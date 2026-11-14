using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class NativeHooks
    {
        static readonly FieldInfo DownQueue = AccessTools.Field(typeof(BuildingUpkeepSystem), "m_LeveldownQueue");
        internal static void Register(Harmony patch)
        {
            foreach (var owner in new[] { typeof(BuildingUpkeepSystem), typeof(PropertyProcessingSystem) })
                patch.Patch(AccessTools.DeclaredMethod(owner, "OnUpdate"), transpiler:
                    new HarmonyMethod(typeof(NativeHooks), nameof(ModifyDispatch)));
            patch.Patch(AccessTools.DeclaredMethod(typeof(CityServiceBudgetSystem), "OnUpdate"),
                postfix: new HarmonyMethod(typeof(NativeHooks), nameof(AfterCityBudget)));
            Around<RentAdjustSystem>(patch, nameof(BeforePrice), nameof(AfterPrice));
            Around<PropertyRenterSystem>(patch, nameof(BeforeCharge), nameof(AfterCharge));
            Around<GarbageAccumulationSystem>(patch, nameof(BeforeGarbage), nameof(AfterGarbage));
            Around<LandValueSystem>(patch, nameof(BeforeLand), nameof(AfterLand));
            patch.Patch(AccessTools.DeclaredMethod(typeof(DestroyAbandonedSystem), "OnUpdate"),
                prefix: new HarmonyMethod(typeof(NativeHooks), nameof(BeforeCollapse)),
                finalizer: new HarmonyMethod(typeof(NativeHooks), nameof(AfterCollapse)));
        }
        [Unity.Burst.BurstCompile]
        private struct WaiveUpkeepJob : IJob
        {
            public NativeArray<int> Expenses;

            public void Execute()
            {
                Expenses[(int)ExpenseSource.ServiceUpkeep] = 0;
                Expenses[(int)ExpenseSource.MapTileUpkeep] = 0;
            }
        }

        static void AfterCityBudget(CityServiceBudgetSystem __instance)
        {
            if (!Runtime.Playing || !Runtime.Options.UpkeepCosts) return;
            // Join the budget pipeline before its readers calculate the treasury debit and expense statistics.
            var expenses = __instance.GetExpenseArray(out var budgetDependencies);
            var waiver = new WaiveUpkeepJob { Expenses = expenses }.Schedule(budgetDependencies);
            __instance.AddArrayReader(waiver);
        }

        internal static void RegisterIntegrations(Harmony patch)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var replacement = assembly.GetType("Time2Work.Systems.Time2WorkDeathCheckSystem",false);
                if (replacement == null) continue;
                var method=AccessTools.DeclaredMethod(replacement,"OnUpdate");
                if (Harmony.GetPatchInfo(method)?.Owners.Contains(patch.Id)==true) return;
                patch.Patch(method, prefix:new HarmonyMethod(typeof(NativeHooks),nameof(AllowResidentMortality)));
                Runtime.Log.Info("Registered Time2Work resident controls.");
                return;
            }
        }
        static void Around<T>(Harmony h, string before, string after)
        {
            h.Patch(AccessTools.DeclaredMethod(typeof(T), "OnUpdate"), new HarmonyMethod(typeof(NativeHooks), before),
                new HarmonyMethod(typeof(NativeHooks), after));
        }

        // Only managed scheduling is patched. Burst jobs still run through the game's scheduler.
        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input, MethodBase __originalMethod)
        {
            int edits = 0;
            foreach (var instruction in input)
            {
                var m = instruction.operand as MethodInfo;
                var t = m?.IsGenericMethod == true ? m.GetGenericArguments()[0] : null;
                if (m?.Name == "Schedule" && t?.Name == "LeveldownJob")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(NativeHooks), nameof(FilterAbandonment));
                    edits++;
                }
                if (m?.Name == "Schedule" && t == typeof(PropertyProcessingSystem.PropertyRentJob))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(NativeHooks), nameof(FilterMoves));
                    edits++;
                }
                if (m?.Name == "ScheduleParallel" && t?.Name == "ResourceNeedingUpkeepJob")
                {
                    instruction.operand = AccessTools.Method(typeof(NativeHooks), nameof(ScheduleLevelMaterials)).MakeGenericMethod(t);
                    edits++;
                }
                yield return instruction;
                if (m?.Name == "GetSingleton" && t == typeof(BuildingConfigurationData))
                {
                    yield return CodeInstruction.Call(typeof(NativeHooks), nameof(ConditionRates));
                    edits++;
                }
            }
            int expected = __originalMethod.DeclaringType == typeof(BuildingUpkeepSystem) ? 3 : 1;
            if (edits != expected) throw new InvalidOperationException($"Unsupported native dispatch in {__originalMethod.DeclaringType}: {edits}/{expected} hooks");
        }

        static BuildingConfigurationData ConditionRates(BuildingConfigurationData value)
        {
            if (Runtime.Playing && Runtime.Options.Condition) value.m_BuildingConditionDecrement = 0;
            return value;
        }
        static bool AllowResidentMortality() => !Runtime.Playing || !Runtime.Options.Residents;
        static JobHandle ScheduleLevelMaterials<T>(T job, EntityQuery query, JobHandle dependency) where T : struct, IJobChunk
        {
            return Runtime.Playing && Runtime.Options.Levels ? dependency : job.ScheduleParallel(query, dependency);
        }
        static JobHandle FilterAbandonment(JobHandle dependency, BuildingUpkeepSystem system)
        {
            var s = Runtime.Options;
            if (!Runtime.Playing || !(s.Levels || s.Vacancy || s.Tenants)) return dependency;
            dependency.Complete();
            var queue = (NativeQueue<Entity>)DownQueue.GetValue(system);
            int count = queue.Count;
            while (count-- > 0 && queue.TryDequeue(out var building))
            {
                bool empty = ProtectionSystem.Empty(system.EntityManager, building);
                if (!(s.Levels || (empty ? s.Vacancy : s.Tenants))) queue.Enqueue(building);
            }
            return dependency;
        }
        static JobHandle FilterMoves(JobHandle dependency, PropertyProcessingSystem system)
        {
            if (!Runtime.Playing || !Runtime.Options.Tenants) return dependency;
            var moves = system.GetRentActionQueue(out var writers);
            JobHandle.CombineDependencies(dependency, writers).Complete();
            int count = moves.Count;
            while (count-- > 0 && moves.TryDequeue(out var action))
            {
                var em = system.EntityManager;
                if (em.HasComponent<PropertyRenter>(action.m_Renter))
                {
                    Entity home = em.GetComponentData<PropertyRenter>(action.m_Renter).m_Property;
                    if (em.Exists(home) && !em.HasComponent<Deleted>(home) && !em.HasComponent<Destroyed>(home) &&
                        !em.HasComponent<Abandoned>(home) && (home != action.m_Property || (action.m_Flags & RentActionFlags.Remove) != 0))
                        continue;
                }
                moves.Enqueue(action);
            }
            return dependency;
        }
        static void BeforePrice(RentAdjustSystem __instance, out Dictionary<Entity,int> __state) =>
            __state = Runtime.Playing && Runtime.Options.RentAmount ? ProtectionSystem.Leases(__instance, false) : null;
        static void AfterPrice(RentAdjustSystem __instance, Dictionary<Entity,int> __state) =>
            ProtectionSystem.RestoreLeases(__instance, __state, false);
        static void BeforeCharge(PropertyRenterSystem __instance, out Dictionary<Entity,int> __state) =>
            __state = Runtime.Playing && Runtime.Options.RentCollection ? ProtectionSystem.Leases(__instance, true) : null;
        static void AfterCharge(PropertyRenterSystem __instance, Dictionary<Entity,int> __state) =>
            ProtectionSystem.RestoreLeases(__instance, __state, true);
        static void BeforeGarbage(GarbageAccumulationSystem __instance, out Dictionary<Entity,int> __state) =>
            __state = Runtime.Playing && Runtime.Options.Garbage ? ProtectionSystem.GarbageBefore(__instance) : null;
        static void AfterGarbage(GarbageAccumulationSystem __instance, Dictionary<Entity,int> __state) =>
            FinishGarbageTick(__instance,__state);
        static readonly FieldInfo GarbageBuckets = AccessTools.Field(typeof(GarbageAccumulationSystem),"m_GarbageAccumulation");
        static readonly FieldInfo GarbageTotal = AccessTools.Field(typeof(GarbageAccumulationSystem),"m_Accumulation");
        static readonly FieldInfo GarbageJobs = AccessTools.Field(typeof(GarbageAccumulationSystem),"m_AccumulationDeps");
        static void FinishGarbageTick(GarbageAccumulationSystem system,Dictionary<Entity,int> before)
        {
            if (before == null) return;
            ProtectionSystem.RestoreGarbage(system,before);
            ((JobHandle)GarbageJobs.GetValue(system)).Complete();
            var buckets=(NativeArray<long>)GarbageBuckets.GetValue(system);
            for(int i=0;i<buckets.Length;i++) buckets[i]=0L;
            GarbageTotal.SetValue(system,0L);
        }
        static bool BeforeCollapse(DestroyAbandonedSystem __instance, out Dictionary<Entity,Abandoned> __state)
        {
            __state=null;
            if (!Runtime.Playing) return true;
            if (!Runtime.Options.Vacancy) return true;
            var em=__instance.EntityManager;
            uint frame=__instance.World.GetExistingSystemManaged<SimulationSystem>().frameIndex;
            __state=new Dictionary<Entity,Abandoned>();
            using (var query=em.CreateEntityQuery(ComponentType.ReadWrite<Abandoned>(),ComponentType.ReadOnly<Renter>(),ComponentType.Exclude<Deleted>()))
            using (var entries=query.ToEntityArray(Allocator.Temp))
                foreach(var building in entries)
                {
                    if (!ProtectionSystem.Empty(em,building)) continue;
                    __state.Add(building,em.GetComponentData<Abandoned>(building));
                    em.SetComponentData(building,new Abandoned {m_AbandonmentTime=frame});
                }
            return true;
        }
        static void AfterCollapse(DestroyAbandonedSystem __instance, Dictionary<Entity,Abandoned> __state)
        {
            if (__state==null) return;
            __instance.World.GetExistingSystemManaged<EndFrameBarrier>().producerHandle.Complete();
            foreach(var pair in __state)
                if (__instance.EntityManager.HasComponent<Abandoned>(pair.Key))
                    __instance.EntityManager.SetComponentData(pair.Key,pair.Value);
        }
        static bool BeforeLand(LandValueSystem __instance, out float[] __state)
        {
            __state = null;
            if (!Runtime.Playing || Runtime.Options.LandValue == ValueMode.Normal) return true;
            if (Runtime.Options.LandValue == ValueMode.Freeze) return false;
            var values = __instance.GetMap(true, out var dep);
            dep.Complete();
            __state = new float[values.Length];
            for (int i=0;i<values.Length;i++) __state[i] = values[i].m_LandValue;
            return true;
        }
        static void AfterLand(LandValueSystem __instance, float[] __state)
        {
            if (__state == null) return;
            var values = __instance.GetMap(false, out var dep);
            dep.Complete();
            for (int i=0;i<values.Length;i++)
                if (values[i].m_LandValue < __state[i]) values[i] = new LandValueCell { m_LandValue=__state[i] };
        }
    }
}
