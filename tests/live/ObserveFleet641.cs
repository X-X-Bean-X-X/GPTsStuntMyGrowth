using System;
using System.Collections.Generic;
using System.Reflection;
using ChicagoBuildBridge;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Routes;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
namespace ChicagoBuildExtensions {
 public static class Entry {
  public static object Run(BridgeSystem b,JObject request) {
   try {
    var em=b.EntityManager;em.CompleteAllTrackedJobs();
    var sim=b.World.GetExistingSystemManaged<SimulationSystem>();
    var depots=new List<object>();var routes=new List<object>();var prefabs=new List<object>();
    using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<Deleted>()))
    using(var es=q.ToEntityArray(Allocator.Temp))foreach(var e in es){
     var p=em.GetComponentData<PrefabRef>(e).m_Prefab;if(!em.HasComponent<TransportDepotData>(p))continue;
     var d=em.GetComponentData<TransportDepotData>(p);int owned=0,disabled=0,publics=0;
     if(em.HasBuffer<Game.Vehicles.OwnedVehicle>(e))foreach(var v in em.GetBuffer<Game.Vehicles.OwnedVehicle>(e,true)){
      owned++;if(em.HasComponent<Game.Vehicles.PublicTransport>(v.m_Vehicle)){publics++;if((em.GetComponentData<Game.Vehicles.PublicTransport>(v.m_Vehicle).m_State&Game.Vehicles.PublicTransportFlags.Disabled)!=0)disabled++;}
     }
     depots.Add(new{entity=e.ToString(),prefab=p.ToString(),type=d.m_TransportType.ToString(),capacity=d.m_VehicleCapacity,production=d.m_ProductionDuration,owned,publics,disabled,dispatches=em.HasBuffer<ServiceDispatch>(e)?em.GetBuffer<ServiceDispatch>(e,true).Length:0,efficiency=em.HasBuffer<Efficiency>(e)?BuildingUtils.GetEfficiency(em.GetBuffer<Efficiency>(e,true)):1});
    }
    using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<TransportDepotData>()))using(var es=q.ToEntityArray(Allocator.Temp))foreach(var e in es){var d=em.GetComponentData<TransportDepotData>(e);prefabs.Add(new{entity=e.ToString(),type=d.m_TransportType.ToString(),capacity=d.m_VehicleCapacity,production=d.m_ProductionDuration,maintenance=d.m_MaintenanceDuration});}
    using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<Game.Routes.TransportLine>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<Deleted>()))
    using(var es=q.ToEntityArray(Allocator.Temp))foreach(var e in es){
     var p=em.GetComponentData<PrefabRef>(e).m_Prefab;if(!em.HasComponent<TransportLineData>(p))continue;
     var d=em.GetComponentData<TransportLineData>(p);var line=em.GetComponentData<Game.Routes.TransportLine>(e);
     routes.Add(new{entity=e.ToString(),type=d.m_TransportType.ToString(),cargo=d.m_CargoTransport,vehicles=em.HasBuffer<RouteVehicle>(e)?em.GetBuffer<RouteVehicle>(e,true).Length:0,state=JObject.FromObject(line)});
    }
    return new{passed=true,frame=sim.frameIndex,speed=sim.selectedSpeed,depots,routes,prefabs};
   }catch(Exception e){return new{passed=false,error=e.ToString()};}
  }
 }
}
