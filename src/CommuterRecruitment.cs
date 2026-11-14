using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class CommuterPolicy
    {
        internal const uint Interval = 256;
        internal const int MaxExtraWorkers = 16;
        internal const int WaitingLimit = 64;

        // Allocate the existing employable pool from the highest qualification down.
        // More-qualified workers can cover lower-level jobs, never the reverse.
        internal static int Shortage(Workplaces jobs, Workplaces available)
        {
            long spare = 0, missing = 0;
            for (int level = 4; level >= 0; level--)
            {
                spare += Math.Max(0, available[level]);
                long required = Math.Max(0, jobs[level]);
                long filled = Math.Min(spare, required);
                spare -= filled;
                missing += required - filled;
            }
            return (int)Math.Min(int.MaxValue, missing);
        }

        internal static int NativePlanned(Workplaces jobs, Workplaces available, int divisor)
        {
            if (divisor <= 0) return 0;
            long need = 0;
            for (int level = 2; level < 5; level++) need += (long)jobs[level] - available[level];
            return (int)Math.Min(int.MaxValue, Math.Max(0, need / divisor));
        }

        internal static int Budget(int shortage, int waiting, int nativeHouseholds, int maxNativeWorkers)
        {
            if (waiting < 0 || waiting >= WaitingLimit || shortage <= 0 || nativeHouseholds < 0 || maxNativeWorkers <= 0) return 0;
            long reserved = (long)nativeHouseholds * maxNativeWorkers;
            return (int)Math.Max(0, Math.Min(MaxExtraWorkers,
                Math.Min((long)WaitingLimit - waiting - reserved, (long)shortage - waiting - reserved)));
        }

        internal static int HouseholdRoom(int households, int workers, int ratio, int nativePlanned)
        {
            if (households < 0 || workers <= 0 || ratio < 0 || nativePlanned < 0) return 0;
            if (ratio == 0) return MaxExtraWorkers; // Native zero means no ratio ceiling.
            long ceiling = ((long)workers + ratio - 1) / ratio;
            return (int)Math.Max(0, Math.Min(MaxExtraWorkers, ceiling - households - nativePlanned));
        }

        internal static bool AdultOnly(HouseholdData prefab, int budget) =>
            prefab.m_AdultCount > 0 && prefab.m_AdultCount <= budget &&
            // Native child count is Random.NextInt(max): both0and1 produce none.
            (prefab.m_ChildCount == 0 || prefab.m_ChildCount == 1) && prefab.m_StudentCount == 0 && prefab.m_ElderCount == 0;
    }

    internal static class CommuterHooks
    {
        static readonly FieldInfo CommuterQuery = AccessTools.Field(typeof(CommuterSpawnSystem), "m_CommuterQuery");
        static readonly FieldInfo WorkerQuery = AccessTools.Field(typeof(CommuterSpawnSystem), "m_WorkerQuery");
        internal static void Register(Harmony patch) => patch.Patch(AccessTools.DeclaredMethod(typeof(CommuterSpawnSystem), "OnUpdate"),
            transpiler: new HarmonyMethod(typeof(CommuterHooks), nameof(ModifyDispatch)));

        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input)
        {
            int edits = 0;
            foreach (var instruction in input)
            {
                var method = instruction.operand as MethodInfo;
                var native = method?.IsGenericMethod == true ? method.GetGenericArguments()[0] : null;
                if (method?.Name == "Schedule" && native?.Name == "SpawnCommuterHouseholdJob")
                {
                    if (CommuterQuery?.FieldType != typeof(EntityQuery) || WorkerQuery?.FieldType != typeof(EntityQuery))
                        throw new InvalidOperationException("Unsupported commuter count queries");
                    foreach (var target in typeof(BoostCommutersJob).GetFields())
                        if (target.Name.StartsWith("m_", StringComparison.Ordinal) && AccessTools.Field(native, target.Name)?.FieldType != target.FieldType)
                            throw new InvalidOperationException("Unsupported commuter spawn field: " + target.Name);
                    var owner = new CodeInstruction(OpCodes.Ldarg_0);
                    owner.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return owner;
                    instruction.operand = AccessTools.Method(typeof(CommuterHooks), nameof(Schedule)).MakeGenericMethod(native);
                    edits++;
                }
                yield return instruction;
            }
            if (edits != 1) throw new InvalidOperationException("Unsupported commuter scheduler: " + edits);
        }

        static class Fields<T>
        {
            internal static readonly FieldInfo Frame = AccessTools.Field(typeof(T), "m_Frame");
            internal static readonly KeyValuePair<FieldInfo,FieldInfo>[] Copies = Create();
            static KeyValuePair<FieldInfo,FieldInfo>[] Create()
            {
                var list = new List<KeyValuePair<FieldInfo,FieldInfo>>();
                foreach (var target in typeof(BoostCommutersJob).GetFields())
                    if (target.Name.StartsWith("m_", StringComparison.Ordinal))
                        list.Add(new KeyValuePair<FieldInfo,FieldInfo>(AccessTools.Field(typeof(T), target.Name), target));
                return list.ToArray();
            }
        }

        static JobHandle Schedule<T>(T job, JobHandle dependency, CommuterSpawnSystem system) where T : struct, IJob
        {
            if (!Runtime.Playing || !Runtime.Options.RecruitOutsideWorkers) return job.Schedule(dependency);
            object native = job;
            uint frame = (uint)Fields<T>.Frame.GetValue(native);
            if (frame % CommuterPolicy.Interval != 0) return job.Schedule(dependency);
            if (system.World.GetExistingSystemManaged<CountHouseholdDataSystem>().IsCountDataNotReady()) return job.Schedule(dependency);
            object extra = default(BoostCommutersJob);
            foreach (var pair in Fields<T>.Copies) pair.Value.SetValue(extra, pair.Key.GetValue(native));
            var boost = (BoostCommutersJob)extra;
            var commuters = (EntityQuery)CommuterQuery.GetValue(system);
            boost.HouseholdCount = commuters.CalculateEntityCount();
            boost.WorkerCount = ((EntityQuery)WorkerQuery.GetValue(system)).CalculateEntityCount();
            // Bound the new synchronization to one recruitment pass per256 frames.
            // These components are also read by the native household initialization.
            system.EntityManager.CompleteDependencyBeforeRO<Worker>();
            system.EntityManager.CompleteDependencyBeforeRO<Citizen>();
            system.EntityManager.CompleteDependencyBeforeRO<HealthProblem>();
            system.EntityManager.CompleteDependencyBeforeRO<HouseholdCitizen>();
            boost.WaitingHouseholds = commuters.ToEntityListAsync(system.World.UpdateAllocator.ToAllocator, out var pending);
            boost.Members = system.GetBufferLookup<HouseholdCitizen>(true);
            boost.Workers = system.GetComponentLookup<Worker>(true);
            boost.Citizens = system.GetComponentLookup<Citizen>(true);
            boost.Health = system.GetComponentLookup<HealthProblem>(true);
            boost.HouseholdTypes = system.GetComponentLookup<HouseholdData>(true);
            boost.DynamicHouseholds = system.GetComponentLookup<DynamicHousehold>(true);
            // Both jobs append to the SAME native ECB sequentially. Return the entire
            // chain so the original barrier and household-data reader track it.
            var original = job.Schedule(JobHandle.CombineDependencies(dependency, pending));
            return boost.Schedule(original);
        }

        // Native job fields are populated by the validated, cached reflection copy.
#pragma warning disable CS0649
        [BurstCompile]
        struct BoostCommutersJob : IJob
        {
            [ReadOnly] public NativeList<Entity> m_PrefabEntities;
            [ReadOnly] public NativeList<ArchetypeData> m_Archetypes;
            [ReadOnly] public NativeList<HouseholdData> m_HouseholdPrefabs;
            [ReadOnly] public NativeList<Entity> m_OutsideConnectionEntities;
            [ReadOnly] public Workplaces m_FreeWorkplaces;
            [ReadOnly] public NativeArray<int> m_Employables;
            [ReadOnly] public DemandParameterData m_DemandParameterData;
            [ReadOnly] public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefs;
            public uint m_Frame;
            public EntityCommandBuffer m_CommandBuffer;
            public RandomSeed m_RandomSeed;
            [ReadOnly] public NativeList<Entity> WaitingHouseholds;
            [ReadOnly] public BufferLookup<HouseholdCitizen> Members;
            [ReadOnly] public ComponentLookup<Worker> Workers;
            [ReadOnly] public ComponentLookup<Citizen> Citizens;
            [ReadOnly] public ComponentLookup<HealthProblem> Health;
            [ReadOnly] public ComponentLookup<HouseholdData> HouseholdTypes;
            [ReadOnly] public ComponentLookup<DynamicHousehold> DynamicHouseholds;
            public int HouseholdCount, WorkerCount;

            int WaitingWorkers()
            {
                int count = 0;
                foreach (var household in WaitingHouseholds)
                {
                    if (!Members.TryGetBuffer(household, out var members) || members.Length == 0)
                    {
                        int expected = 1;
                        if (m_PrefabRefs.TryGetComponent(household, out var prefab) && HouseholdTypes.TryGetComponent(prefab.m_Prefab, out var data))
                            expected = Math.Max(1, (int)data.m_AdultCount + data.m_StudentCount);
                        count += expected;
                    }
                    else foreach (var member in members)
                    {
                        var entity = member.m_Citizen;
                        if (Workers.HasComponent(entity) || !Citizens.TryGetComponent(entity, out var citizen)) continue;
                        if (Health.TryGetComponent(entity, out var health) && (health.m_Flags & HealthProblemFlags.Dead) != 0) continue;
                        var age = citizen.GetAge();
                        if (age == CitizenAge.Adult || age == CitizenAge.Teen) count++;
                    }
                    if (count >= CommuterPolicy.WaitingLimit) return count;
                }
                return count;
            }

            public void Execute()
            {
                if (m_Employables.Length < 5 || m_HouseholdPrefabs.Length == 0 || m_OutsideConnectionEntities.Length == 0) return;
                var available = new Workplaces();
                for (int i = 0; i < 5; i++) available[i] = m_Employables[i];
                int shortage = CommuterPolicy.Shortage(m_FreeWorkplaces, available);
                if (shortage == 0) return;
                int nativeCount = CommuterPolicy.NativePlanned(m_FreeWorkplaces, available, m_DemandParameterData.m_CommuterSlowSpawnFactor);
                int room = CommuterPolicy.HouseholdRoom(HouseholdCount, WorkerCount, m_DemandParameterData.m_CommuterWorkerRatioLimit, nativeCount);
                if (room == 0) return;
                int maxNativeWorkers = 1;
                foreach (var data in m_HouseholdPrefabs) maxNativeWorkers = Math.Max(maxNativeWorkers, (int)data.m_AdultCount + data.m_StudentCount);
                int budget = CommuterPolicy.Budget(shortage, WaitingWorkers(), nativeCount, maxNativeWorkers);
                var random = m_RandomSeed.GetRandom(unchecked((int)(m_Frame ^ 0x6C8E9CF5u)));
                while (budget > 0 && room > 0)
                {
                    // Reservoir selection avoids temporary lists and never selects a
                    // household whose adults exceed the remaining worker budget.
                    int selected = -1, choices = 0;
                    for (int i = 0; i < m_HouseholdPrefabs.Length; i++)
                        if (!DynamicHouseholds.HasComponent(m_PrefabEntities[i]) &&
                            CommuterPolicy.AdultOnly(m_HouseholdPrefabs[i], budget) && random.NextInt(++choices) == 0) selected = i;
                    if (selected < 0 || !BuildingUtils.GetRandomOutsideConnectionByParameters(ref m_OutsideConnectionEntities,
                        ref m_OutsideConnectionDatas, ref m_PrefabRefs, random, m_DemandParameterData.m_CommuterOCSpawnParameters, out var outside)) return;
                    var household = m_CommandBuffer.CreateEntity(m_Archetypes[selected].m_Archetype);
                    m_CommandBuffer.SetComponent(household, new PrefabRef { m_Prefab = m_PrefabEntities[selected] });
                    m_CommandBuffer.SetComponent(household, new Household { m_Flags = HouseholdFlags.Commuter });
                    m_CommandBuffer.AddComponent(household, new CommuterHousehold { m_OriginalFrom = outside });
                    m_CommandBuffer.AddComponent(household, new CurrentBuilding { m_CurrentBuilding = outside });
                    budget -= m_HouseholdPrefabs[selected].m_AdultCount;
                    room--;
                }
            }
        }
#pragma warning restore CS0649
    }
}
