using System;
using System.Linq;
using Game.Citizens;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class TransitHooks
    {
        // Per-request ownership: the native queue owns both the action and this marker.
        // No static city/entity cache survives a load or needs pruning.
        sealed class Request
        {
            internal object System;
            internal PathfindParameters Original;
            internal bool Fallback;
        }

        internal static void Register(Harmony patch)
        {
            var enqueues = typeof(PathfindQueueSystem).GetMethods().Where(m => m.Name == "Enqueue" && m.GetParameters()[0].ParameterType == typeof(PathfindAction)).ToArray();
            if (enqueues.Length != 2) throw new InvalidOperationException("Unsupported citizen path queue API");
            foreach (var method in enqueues)
                patch.Patch(method, prefix: new HarmonyMethod(typeof(TransitHooks), nameof(BeforeEnqueue)));
            var results = AccessTools.Method(typeof(PathfindResultSystem), "ProcessResults", new[] {
                typeof(PathfindQueueSystem.ActionList<PathfindAction>), typeof(JobHandle).MakeByRefType(), typeof(JobHandle) });
            if (results == null) throw new InvalidOperationException("Unsupported citizen path results API");
            patch.Patch(results, prefix: new HarmonyMethod(typeof(TransitHooks), nameof(BeforeResults)));
        }

        internal static bool TryPrefer(PathfindParameters original, out PathfindParameters preferred)
        {
            preferred = original;
            const PathMethod transit = PathMethod.PublicTransportDay | PathMethod.PublicTransportNight;
            const PathMethod driving = PathMethod.Road | PathMethod.MediumRoad | PathMethod.Taxi;
            if ((original.m_Methods & transit) == 0 || (original.m_Methods & PathMethod.Pedestrian) == 0 ||
                (original.m_Methods & driving) == 0 ||
                (original.m_PathfindFlags & (PathfindFlags.IgnorePath | PathfindFlags.SkipPathfind)) != 0) return false;
            // Preserve walking/bicycling connections and the day/night route availability mask.
            preferred.m_Methods &= ~(driving | PathMethod.Parking | PathMethod.SpecialParking);
            return true;
        }

        static void BeforeEnqueue(PathfindQueueSystem __instance, PathfindAction action, Entity owner, ref JobHandle dependencies, ref object system)
        {
            if (system is Request || !Runtime.Playing || !Runtime.Options.PreferPublicTransportation ||
                !__instance.EntityManager.HasComponent<Citizen>(owner)) return;
            dependencies.Complete();
            var original = action.data.m_Parameters;
            if (!TryPrefer(original, out var preferred)) return;
            system = new Request { System = system, Original = original };
            action.data.m_Parameters = preferred;
        }

        internal static bool NeedsFallback(PathfindResult result) => result.m_Duration < 0f || result.m_ErrorCode != ErrorCode.None;

        static void BeforeResults(PathfindResultSystem __instance, PathfindQueueSystem.ActionList<PathfindAction> list)
        {
            var queue = __instance.World.GetExistingSystemManaged<PathfindQueueSystem>();
            int originalCount = list.m_Items.Count;
            for (int i = 0; i < originalCount; i++)
            {
                var item = list.m_Items[i];
                if (!(item.m_System is Request request) || (item.m_Flags & PathFlags.Scheduled) == 0 ||
                    item.m_Action.data.m_State != PathfindActionState.Completed) continue;
                item.m_Dependencies.Complete();
                ref var data = ref item.m_Action.data;
                if (request.Fallback || (data.m_Result.Length > 0 && !NeedsFallback(data.m_Result[0])))
                {
                    item.m_System = request.System;
                    list.m_Items[i] = item;
                    continue;
                }
                // The preferred route failed. Reuse the owned action once with the exact original
                // parameters and targets; native cleanup disposes it after the fallback completes.
                request.Fallback = true;
                data.m_Parameters = request.Original;
                data.m_Result.Clear();
                if (data.m_Path.IsCreated) data.m_Path.Clear();
                data.m_State = PathfindActionState.Pending;
                list.m_Items.RemoveAt(i);
                if (i < list.m_NextIndex) list.m_NextIndex--;
                i--; originalCount--;
                if ((item.m_Flags & PathFlags.WantsEvent) != 0)
                    queue.Enqueue(item.m_Action, item.m_Owner, item.m_Dependencies, item.m_ResultFrame, request, item.m_EventData);
                else queue.Enqueue(item.m_Action, item.m_Owner, item.m_Dependencies, item.m_ResultFrame, request);
            }
        }
    }
}
