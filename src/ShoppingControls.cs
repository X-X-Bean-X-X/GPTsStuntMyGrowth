using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class ShoppingHooks
    {
        internal static void Register(Harmony h) => h.Patch(AccessTools.DeclaredMethod(typeof(HouseholdBehaviorSystem),"OnUpdate"),
            transpiler:new HarmonyMethod(typeof(ShoppingHooks),nameof(ModifyDispatch)));
        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input)
        {
            int edits=0;
            foreach(var instruction in input)
            {
                var method=instruction.operand as MethodInfo;
                var type=method?.IsGenericMethod==true?method.GetGenericArguments()[0]:null;
                if(method?.Name=="ScheduleParallel" && type?.Name=="HouseholdTickJob")
                {
                    if(AccessTools.Field(type,"m_EconomyParameters")?.FieldType!=typeof(EconomyParameterData))
                        throw new InvalidOperationException("Unsupported native household shopping parameters");
                    instruction.operand=AccessTools.Method(typeof(ShoppingHooks),nameof(Schedule)).MakeGenericMethod(type);
                    edits++;
                }
                yield return instruction;
            }
            if(edits!=1) throw new InvalidOperationException("Unsupported household shopping scheduler: "+edits);
        }
        static class Fields<T>
        {
            internal static readonly FieldInfo Parameters=AccessTools.Field(typeof(T),"m_EconomyParameters");
        }
        internal static EconomyParameterData Maximize(EconomyParameterData source)
        {
            // This field has one use in HouseholdTickJob: the population-based shopping lottery.
            // Only the scheduled job's VALUE COPY changes. The global economy prefab is untouched.
            source.m_TrafficReduction=0;
            return source;
        }
        static JobHandle Schedule<T>(T job,EntityQuery query,JobHandle dependency) where T:struct,IJobChunk
        {
            if(Runtime.Playing && Runtime.Options.ShoppingActivity)
            {
                object copy=job;
                var parameters=(EconomyParameterData)Fields<T>.Parameters.GetValue(copy);
                Fields<T>.Parameters.SetValue(copy,Maximize(parameters));
                job=(T)copy;
            }
            // Keep native frequency, spending checks, existing needs, daily repeat reduction,
            // commodity selection, work/school/sleep priorities, paths and real sale/payment processing.
            return job.ScheduleParallel(query,dependency);
        }
    }
}
