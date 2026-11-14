using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace StuntYourGrowth
{
    internal static class PublicTransportFleet
    {
        internal static void Register(Harmony patch) => patch.Patch(
            AccessTools.DeclaredMethod(typeof(TransportDepotAISystem),"OnUpdate"),
            transpiler:new HarmonyMethod(typeof(PublicTransportFleet),nameof(ModifyDispatch)));

        static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input)
        {
            int edits=0;
            foreach(var instruction in input)
            {
                var method=instruction.operand as MethodInfo;
                var type=method?.IsGenericMethod==true ? method.GetGenericArguments()[0] : null;
                if(method?.Name=="ScheduleParallel" && type?.Name=="TransportDepotTickJob")
                {
                    if(AccessTools.Field(type,"m_PrefabTransportDepotData")?.FieldType!=typeof(ComponentLookup<TransportDepotData>))
                        throw new InvalidOperationException("Native transport depot API changed");
                    var load=new CodeInstruction(OpCodes.Ldarg_0);
                    load.labels.AddRange(instruction.labels);instruction.labels.Clear();yield return load;
                    instruction.operand=AccessTools.Method(typeof(PublicTransportFleet),nameof(ScheduleDepots)).MakeGenericMethod(type);
                    edits++;
                }
                yield return instruction;
            }
            if(edits!=1)throw new InvalidOperationException("Unsupported transport depot scheduler: "+edits);
        }

        internal static bool Supported(TransportType type) => type==TransportType.Bus || type==TransportType.Train ||
            type==TransportType.Tram || type==TransportType.Subway || type==TransportType.Ship ||
            type==TransportType.Ferry || type==TransportType.Airplane;

        // Keep one dispatch slot beyond existing vehicles and the native maintenance reserve.
        // Actual line requests, not this number, decide how many vehicles are spawned.
        internal static int ReserveCapacity(int original,int owned,float maintenance,float efficiency)
        {
            if(original<0 || owned<0 || float.IsNaN(efficiency) || float.IsInfinity(efficiency) ||
                efficiency<=0.001f || float.IsNaN(maintenance) || float.IsInfinity(maintenance))return original;
            double reserve=(double)owned+Math.Ceiling(Math.Max(0,maintenance))+2;
            double needed=Math.Ceiling(reserve/Math.Min(1,efficiency));
            if(needed>100000000)return original; // Reject corrupt/unbounded arithmetic; never use int.MaxValue.
            return Math.Max(original,(int)needed);
        }

        static JobHandle ScheduleDepots<T>(T job,EntityQuery query,JobHandle dependency,GameSystemBase system) where T:struct,IJobChunk
        {
            if(!Runtime.Playing || !Runtime.Options.UnlimitedPublicTransportVehicles)return job.ScheduleParallel(query,dependency);
            var em=system.EntityManager;
            // Native Burst code reads shared depot data. Complete readers before changing it,
            // complete this one native job, then restore it before any other system can run.
            // No save, UI, another system, or later frame may observe the temporary values.
            dependency.Complete();em.CompleteAllTrackedJobs();
            var changes=Prepare(em,query);
            JobHandle dispatched=default;
            try
            {
                foreach(var pair in changes)
                    em.SetComponentData(pair.Key,pair.Value.modified);
                dispatched=job.ScheduleParallel(query,dependency);
                dispatched.Complete();
                return dispatched;
            }
            finally
            {
                try { dispatched.Complete(); }
                finally
                {
                    foreach(var pair in changes)
                        if(em.HasComponent<TransportDepotData>(pair.Key))em.SetComponentData(pair.Key,pair.Value.original);
                }
            }
        }

        internal static Dictionary<Entity,(TransportDepotData original,TransportDepotData modified)> Prepare(EntityManager em,EntityQuery query)
        {
            var changes=new Dictionary<Entity,(TransportDepotData original,TransportDepotData modified)>();
            var blocked=new HashSet<Entity>();
            using(var depots=query.ToEntityArray(Allocator.Temp))foreach(var depot in depots)
            {
                if(!em.HasComponent<PrefabRef>(depot))continue;
                var prefab=em.GetComponentData<PrefabRef>(depot).m_Prefab;
                if(!em.HasComponent<TransportDepotData>(prefab))continue;
                var data=em.GetComponentData<TransportDepotData>(prefab);
                if(!Supported(data.m_TransportType))continue;
                // An upgrade can add its own manufacturing loop after the base values combine.
                // Keep that provider native rather than create an expanding parked fleet.
                bool manufactures=false;
                if(em.HasBuffer<InstalledUpgrade>(depot))foreach(var upgrade in em.GetBuffer<InstalledUpgrade>(depot,true))
                {
                    if(!em.HasComponent<PrefabRef>(upgrade.m_Upgrade))continue;
                    var upgradePrefab=em.GetComponentData<PrefabRef>(upgrade.m_Upgrade).m_Prefab;
                    if(em.HasComponent<TransportDepotData>(upgradePrefab) && em.GetComponentData<TransportDepotData>(upgradePrefab).m_ProductionDuration!=0)manufactures=true;
                }
                bool cargo=false;
                int owned=0;
                if(em.HasBuffer<Game.Vehicles.OwnedVehicle>(depot))
                {
                    var vehicles=em.GetBuffer<Game.Vehicles.OwnedVehicle>(depot,true);owned=vehicles.Length;
                    foreach(var entry in vehicles)if(em.HasComponent<Game.Vehicles.CargoTransport>(entry.m_Vehicle)){cargo=true;break;}
                }
                if(em.HasBuffer<ServiceDispatch>(depot))foreach(var dispatch in em.GetBuffer<ServiceDispatch>(depot,true))
                {
                    if(!em.HasComponent<TransportVehicleRequest>(dispatch.m_Request))continue;
                    var route=em.GetComponentData<TransportVehicleRequest>(dispatch.m_Request).m_Route;
                    if(!em.HasComponent<PrefabRef>(route))continue;
                    var routePrefab=em.GetComponentData<PrefabRef>(route).m_Prefab;
                    if(em.HasComponent<TransportLineData>(routePrefab) && em.GetComponentData<TransportLineData>(routePrefab).m_CargoTransport)cargo=true;
                }
                // Shared cargo/passenger providers retain native dispatch. Do not expand cargo fleets.
                if(cargo || manufactures){blocked.Add(prefab);continue;}
                if(em.HasComponent<Deleted>(depot) || em.HasComponent<Temp>(depot) || em.HasComponent<Abandoned>(depot) || em.HasComponent<Destroyed>(depot))continue;
                float efficiency=1;
                if(em.HasBuffer<Efficiency>(depot))
                {
                    var factors=em.GetBuffer<Efficiency>(depot,true);
                    efficiency=Math.Min(BuildingUtils.GetEfficiency(factors),BuildingUtils.GetImmediateEfficiency(factors));
                }
                if(efficiency<=0.001f)continue;
                var state=em.GetComponentData<Game.Buildings.TransportDepot>(depot);
                int target=ReserveCapacity(data.m_VehicleCapacity,owned,state.m_MaintenanceRequirement,efficiency);
                if(changes.TryGetValue(prefab,out var previous))target=Math.Max(target,previous.modified.m_VehicleCapacity);
                var modified=data;modified.m_VehicleCapacity=target;
                // Fulfil genuine dispatch requests directly; never manufacture a growing parked reserve.
                modified.m_ProductionDuration=0;
                changes[prefab]=(data,modified);
            }
            foreach(var prefab in blocked)changes.Remove(prefab);
            return changes;
        }
    }
}
