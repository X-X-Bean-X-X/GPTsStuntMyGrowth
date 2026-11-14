using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Simulation;
using HarmonyLib;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class ResourceHooks
    {
        internal static void Register(Harmony patch) => patch.Patch(AccessTools.DeclaredMethod(typeof(AreaLotSimulationSystem), "OnUpdate"),
            transpiler: new HarmonyMethod(typeof(ResourceHooks), nameof(ModifyDispatch)));

        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input)
        {
            int edits = 0;
            foreach (var instruction in input)
            {
                var method = instruction.operand as MethodInfo;
                var type = method?.IsGenericMethod == true ? method.GetGenericArguments()[0] : null;
                if (method?.Name == "Schedule" && type?.Name == "ExtractResourcesJob")
                {
                    if (AccessTools.Field(type,"m_NaturalResourceData")?.FieldType != typeof(CellMapData<NaturalResourceCell>))
                        throw new InvalidOperationException("Native extraction map API changed");
                    instruction.operand = AccessTools.Method(typeof(ResourceHooks), nameof(ScheduleExtraction)).MakeGenericMethod(type);
                    edits++;
                }
                yield return instruction;
            }
            if (edits != 1) throw new InvalidOperationException("Unsupported natural resource extraction scheduler: " + edits);
        }

        static class Fields<T> { internal static readonly FieldInfo Map = AccessTools.Field(typeof(T), "m_NaturalResourceData"); }

        [BurstCompile]
        struct CopyMap : IJob
        {
            [ReadOnly] public NativeArray<NaturalResourceCell> Source;
            public NativeArray<NaturalResourceCell> Destination;
            public void Execute() => NativeArray<NaturalResourceCell>.Copy(Source, Destination);
        }

        static JobHandle ScheduleExtraction<T>(T job, JobHandle dependency) where T : struct, IJob
        {
            if (!Runtime.Playing || !Runtime.Options.MaximumResources) return job.Schedule(dependency);
            object boxed = job;
            var map = (CellMapData<NaturalResourceCell>)Fields<T>.Map.GetValue(boxed);
            // Run the real extraction bookkeeping against a temporary map copy. Production,
            // extraction debt, area notifications and work continue; actual deposits stay intact.
            var copy = new NativeArray<NaturalResourceCell>(map.m_Buffer.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var copied = new CopyMap { Source = map.m_Buffer, Destination = copy }.Schedule(dependency);
            map.m_Buffer = copy;
            Fields<T>.Map.SetValue(boxed, map);
            var extracted = ((T)boxed).Schedule(copied);
            return copy.Dispose(extracted);
        }
    }
}
