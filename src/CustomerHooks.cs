using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class CustomerHooks
    {
        internal static void Register(Harmony patch) => Patch(patch, typeof(HouseholdBehaviorSystem));

        static void Patch(Harmony patch, Type system)
        {
            var method = AccessTools.DeclaredMethod(system, "OnUpdate");
            if (Harmony.GetPatchInfo(method)?.Owners.Contains(patch.Id) == true) return;
            patch.Patch(method, transpiler: new HarmonyMethod(typeof(CustomerHooks), nameof(ModifyDispatch)));
        }

        internal static void RegisterIntegrations(Harmony patch)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Time2Work.Time2WorkCitizenBehaviorSystem", false);
                if (type == null) continue;
                // Optional integrations must not stop the base game controls if their API changes.
                try { Patch(patch, type); }
                catch (Exception error) { Runtime.Log.Warn("Maximum Customers: Time2Work shopping integration unavailable: " + error.Message); }
            }
        }

        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input, MethodBase __originalMethod)
        {
            bool household = __originalMethod.DeclaringType == typeof(HouseholdBehaviorSystem);
            int edits = 0;
            foreach (var instruction in input)
            {
                var method = instruction.operand as MethodInfo;
                var job = method?.IsGenericMethod == true ? method.GetGenericArguments()[0] : null;
                if (method?.Name == "ScheduleParallel" && job?.Name == (household ? "HouseholdTickJob" : "CitizenAITickJob"))
                {
                    ValidateJob(job, household);
                    instruction.operand = AccessTools.Method(typeof(CustomerHooks), household ? nameof(ScheduleHousehold) : nameof(ScheduleCitizen)).MakeGenericMethod(job);
                    edits++;
                }
                yield return instruction;
            }
            if (edits != 1) throw new InvalidOperationException("Unsupported shopping scheduler: " + __originalMethod.DeclaringType);
        }

        static void ValidateJob(Type type, bool household)
        {
            if (household)
            {
                if (AccessTools.Field(type, "m_EconomyParameters")?.FieldType != typeof(EconomyParameterData))
                    throw new InvalidOperationException("Household economy parameters changed");
            }
            else foreach (var name in new[] { "shoppingTripGatesEnabled", "householdShoppingCooldownEnabled" })
                if (AccessTools.Field(type, name)?.FieldType != typeof(bool))
                    throw new InvalidOperationException("Time2Work shopping controls changed: " + name);
        }

        static class Fields<T>
        {
            internal static readonly FieldInfo Economy = AccessTools.Field(typeof(T), "m_EconomyParameters");
            internal static readonly FieldInfo Gate = AccessTools.Field(typeof(T), "shoppingTripGatesEnabled");
            internal static readonly FieldInfo Cooldown = AccessTools.Field(typeof(T), "householdShoppingCooldownEnabled");
        }

        // Only job-local copies change. No global prefab, saved household needs or wallets are edited.
        internal static T PrepareHousehold<T>(T job, bool enabled) where T : struct
        {
            if (!enabled) return job;
            object boxed = job;
            var economy = (EconomyParameterData)Fields<T>.Economy.GetValue(boxed);
            economy.m_TrafficReduction = 0f;
            Fields<T>.Economy.SetValue(boxed, economy);
            return (T)boxed;
        }

        internal static T PrepareCitizen<T>(T job, bool enabled) where T : struct
        {
            if (!enabled) return job;
            object boxed = job;
            Fields<T>.Gate.SetValue(boxed, false);
            Fields<T>.Cooldown.SetValue(boxed, false);
            return (T)boxed;
        }

        static JobHandle ScheduleHousehold<T>(T job, EntityQuery query, JobHandle dependency) where T : struct, IJobChunk =>
            PrepareHousehold(job, Runtime.Playing && Runtime.Options.MaximumCustomers).ScheduleParallel(query, dependency);

        static JobHandle ScheduleCitizen<T>(T job, EntityQuery query, JobHandle dependency) where T : struct, IJobChunk =>
            PrepareCitizen(job, Runtime.Playing && Runtime.Options.MaximumCustomers).ScheduleParallel(query, dependency);
    }
}
