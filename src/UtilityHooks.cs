using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game;
using Game.Buildings;
using Game.Notifications;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    // Append to the native dispatch dependency, before downstream readers and icon playback.
    // Never modify network edges, production capacities, prefabs, or global debug settings.
    internal static class UtilityHooks
    {
        internal static void Register(Harmony patch)
        {
            foreach (var system in new[] { typeof(DispatchElectricitySystem), typeof(DispatchWaterSystem) })
                patch.Patch(AccessTools.DeclaredMethod(system, "OnUpdate"), transpiler:
                    new HarmonyMethod(typeof(UtilityHooks), nameof(ModifyDispatch)));
        }

        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input, MethodBase __originalMethod)
        {
            bool electricity = __originalMethod.DeclaringType == typeof(DispatchElectricitySystem);
            string jobName = electricity ? "DispatchElectricityJob" : "DispatchWaterJob";
            int edits = 0;
            foreach (var instruction in input)
            {
                var method = instruction.operand as MethodInfo;
                var type = method?.IsGenericMethod == true ? method.GetGenericArguments()[0] : null;
                if (method?.Name == "ScheduleParallel" && type?.Name == jobName)
                {
                    // Fail at registration if a game update changes the fields we share with dispatch.
                    CheckField(type, "m_EntityType", typeof(EntityTypeHandle));
                    CheckField(type, "m_ConsumerType", electricity ? typeof(ComponentTypeHandle<ElectricityConsumer>) : typeof(ComponentTypeHandle<WaterConsumer>));
                    CheckField(type, "m_EfficiencyType", typeof(BufferTypeHandle<Efficiency>));
                    CheckField(type, "m_IconCommandBuffer", typeof(IconCommandBuffer));
                    CheckField(type, "m_Parameters", electricity ? typeof(ElectricityParameterData) : typeof(WaterPipeParameterData));
                    var loadSystem = new CodeInstruction(OpCodes.Ldarg_0);
                    loadSystem.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return loadSystem;
                    instruction.operand = AccessTools.Method(typeof(UtilityHooks), electricity ? nameof(ScheduleElectricity) : nameof(ScheduleWater)).MakeGenericMethod(type);
                    edits++;
                }
                yield return instruction;
            }
            if (edits != 1) throw new InvalidOperationException($"Unsupported utility dispatch in {__originalMethod.DeclaringType}: {edits}/1 hooks");
        }

        static void CheckField(Type type, string name, Type expected)
        {
            if (AccessTools.Field(type, name)?.FieldType != expected)
                throw new InvalidOperationException($"Unsupported utility job field: {type.Name}.{name}");
        }

        static class Fields<T>
        {
            internal static readonly FieldInfo Entity = AccessTools.Field(typeof(T), "m_EntityType");
            internal static readonly FieldInfo Consumer = AccessTools.Field(typeof(T), "m_ConsumerType");
            internal static readonly FieldInfo Efficiency = AccessTools.Field(typeof(T), "m_EfficiencyType");
            internal static readonly FieldInfo Parameters = AccessTools.Field(typeof(T), "m_Parameters");
        }

        static JobHandle ScheduleElectricity<T>(T native, EntityQuery query, JobHandle dependency, GameSystemBase system) where T : struct, IJobChunk
        {
            var dispatched = native.ScheduleParallel(query, dependency);
            if (!Runtime.Playing || !Runtime.Options.UnlimitedElectricity) return dispatched;
            object boxed = native;
            return new ElectricityJob
            {
                Entities = (EntityTypeHandle)Fields<T>.Entity.GetValue(boxed),
                Consumers = (ComponentTypeHandle<ElectricityConsumer>)Fields<T>.Consumer.GetValue(boxed),
                Efficiencies = (BufferTypeHandle<Efficiency>)Fields<T>.Efficiency.GetValue(boxed),
                // A later buffer index makes removal sort after native warning additions.
                Icons = system.World.GetExistingSystemManaged<IconCommandSystem>().CreateCommandBuffer(),
                Parameters = (ElectricityParameterData)Fields<T>.Parameters.GetValue(boxed)
            }.ScheduleParallel(query, dispatched);
        }

        static JobHandle ScheduleWater<T>(T native, EntityQuery query, JobHandle dependency, GameSystemBase system) where T : struct, IJobChunk
        {
            var dispatched = native.ScheduleParallel(query, dependency);
            if (!Runtime.Playing || !(Runtime.Options.UnlimitedWater || Runtime.Options.UnlimitedSewer)) return dispatched;
            object boxed = native;
            return new WaterJob
            {
                Entities = (EntityTypeHandle)Fields<T>.Entity.GetValue(boxed),
                Consumers = (ComponentTypeHandle<WaterConsumer>)Fields<T>.Consumer.GetValue(boxed),
                Efficiencies = (BufferTypeHandle<Efficiency>)Fields<T>.Efficiency.GetValue(boxed),
                // A later buffer index makes removal sort after native warning additions.
                Icons = system.World.GetExistingSystemManaged<IconCommandSystem>().CreateCommandBuffer(),
                Parameters = (WaterPipeParameterData)Fields<T>.Parameters.GetValue(boxed),
                Fresh = Runtime.Options.UnlimitedWater,
                Sewer = Runtime.Options.UnlimitedSewer
            }.ScheduleParallel(query, dispatched);
        }

        internal static ElectricityConsumer SupplyElectricity(ElectricityConsumer value)
        {
            value.m_FulfilledConsumption = value.m_WantedConsumption;
            value.m_CooldownCounter = 0;
            value.m_Flags = (value.m_Flags | ElectricityConsumerFlags.Connected) &
                ~(ElectricityConsumerFlags.NoElectricityWarning | ElectricityConsumerFlags.BottleneckWarning);
            return value;
        }

        internal static WaterConsumer SupplyWater(WaterConsumer value, bool fresh, bool sewer)
        {
            if (fresh)
            {
                value.m_FulfilledFresh = value.m_WantedConsumption;
                value.m_FreshCooldownCounter = 0;
                value.m_Pollution = 0;
                value.m_Flags |= WaterConsumerFlags.WaterConnected;
            }
            if (sewer)
            {
                value.m_FulfilledSewage = value.m_WantedConsumption;
                value.m_SewageCooldownCounter = 0;
                value.m_Flags |= WaterConsumerFlags.SewageConnected;
            }
            return value;
        }

        [BurstCompile]
        private struct ElectricityJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle Entities;
            public ComponentTypeHandle<ElectricityConsumer> Consumers;
            public BufferTypeHandle<Efficiency> Efficiencies;
            public IconCommandBuffer Icons;
            public ElectricityParameterData Parameters;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(Entities);
                var consumers = chunk.GetNativeArray(ref Consumers);
                var efficiencies = chunk.GetBufferAccessor(ref Efficiencies);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var old = consumers[i];
                    if ((old.m_Flags & ElectricityConsumerFlags.NoElectricityWarning) != 0)
                        Icons.Remove(entities[i], Parameters.m_ElectricityNotificationPrefab);
                    if ((old.m_Flags & ElectricityConsumerFlags.BottleneckWarning) != 0)
                        Icons.Remove(entities[i], Parameters.m_BuildingBottleneckNotificationPrefab);
                    consumers[i] = SupplyElectricity(old);
                    if (efficiencies.Length != 0)
                        BuildingUtils.SetEfficiencyFactor(efficiencies[i], EfficiencyFactor.ElectricitySupply, 1f);
                }
            }
        }

        [BurstCompile]
        private struct WaterJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle Entities;
            public ComponentTypeHandle<WaterConsumer> Consumers;
            public BufferTypeHandle<Efficiency> Efficiencies;
            public IconCommandBuffer Icons;
            public WaterPipeParameterData Parameters;
            public bool Fresh, Sewer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(Entities);
                var consumers = chunk.GetNativeArray(ref Consumers);
                var efficiencies = chunk.GetBufferAccessor(ref Efficiencies);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var old = consumers[i];
                    if (Fresh)
                    {
                        if (old.m_FreshCooldownCounter >= DispatchWaterSystem.kAlertCooldown)
                            Icons.Remove(entities[i], Parameters.m_WaterNotification);
                        if (old.m_Pollution > Parameters.m_MaxToleratedPollution)
                            Icons.Remove(entities[i], Parameters.m_DirtyWaterNotification);
                    }
                    if (Sewer && old.m_SewageCooldownCounter >= DispatchWaterSystem.kAlertCooldown)
                        Icons.Remove(entities[i], Parameters.m_SewageNotification);
                    consumers[i] = SupplyWater(old, Fresh, Sewer);
                    if (efficiencies.Length != 0)
                    {
                        if (Fresh)
                        {
                            BuildingUtils.SetEfficiencyFactor(efficiencies[i], EfficiencyFactor.WaterSupply, 1f);
                            BuildingUtils.SetEfficiencyFactor(efficiencies[i], EfficiencyFactor.DirtyWater, 1f);
                        }
                        if (Sewer) BuildingUtils.SetEfficiencyFactor(efficiencies[i], EfficiencyFactor.SewageHandling, 1f);
                    }
                }
            }
        }
    }
}
