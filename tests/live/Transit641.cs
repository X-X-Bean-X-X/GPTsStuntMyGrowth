using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ChicagoBuildBridge;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Newtonsoft.Json.Linq;
using StuntYourGrowth;
using Unity.Entities;
namespace ChicagoBuildExtensions
{
 public static class Entry
 {
  public static object Run(BridgeSystem b,JObject request)
  {
   try{return Test(b,request);}catch(Exception e){return new{passed=false,error=e.ToString()};}
  }
  static object Test(BridgeSystem b,JObject request)
  {
   var em=b.EntityManager;var sim=b.World.GetExistingSystemManaged<SimulationSystem>();
   if(sim.selectedSpeed!=0 || request.Value<uint>("expectedFrame")!=sim.frameIndex)throw new Exception("Matching paused QA frame required");
   var asm=typeof(GPTStuntGrowthSettings).Assembly;
   if(asm.GetName().Version.ToString()!="1.75.641.0")throw new Exception("Installed 641 required");
   em.CompleteAllTrackedJobs();
   var prepare=asm.GetType("StuntYourGrowth.PublicTransportFleet").GetMethod("Prepare",BindingFlags.Static|BindingFlags.NonPublic);
   var entities=new List<Entity>();var checks=new List<string>();
   Entity New(params ComponentType[] c){var e=em.CreateEntity(c);entities.Add(e);return e;}
   void Check(bool valid,string name){if(!valid)throw new Exception(name);checks.Add(name);}
   // Actual depots are excluded from the fixture through the temporary marker.
   using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),ComponentType.ReadOnly<Temp>()))
   {
    if(query.CalculateEntityCount()!=0)throw new Exception("Existing temporary depots; fixture aborted");
    try
    {
     foreach(var kind in new[]{TransportType.Bus,TransportType.Train,TransportType.Tram,TransportType.Subway,TransportType.Ship,TransportType.Ferry,TransportType.Airplane})
     {
      var prefab=New(typeof(TransportDepotData));
      var original=new TransportDepotData{m_TransportType=kind,m_VehicleCapacity=1,m_ProductionDuration=3,m_MaintenanceDuration=7};em.SetComponentData(prefab,original);
      var depot=New(typeof(Game.Buildings.TransportDepot),typeof(PrefabRef),typeof(Temp));
      em.SetComponentData(depot,new PrefabRef{m_Prefab=prefab});
      var vehicle=New(typeof(Game.Vehicles.PublicTransport));
      em.AddBuffer<Game.Vehicles.OwnedVehicle>(depot).Add(new Game.Vehicles.OwnedVehicle{m_Vehicle=vehicle});
      // Prepare excludes actual Temp depots. Remove the marker, use a single-entity query
      // restricted by a unique shared UpdateFrame, and restore after each call.
      em.AddSharedComponentManaged(depot,new UpdateFrame{m_Index=uint.MaxValue});
      em.RemoveComponent<Temp>(depot);
      using(var one=em.CreateEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),ComponentType.ReadOnly<UpdateFrame>()))
      {
       one.SetSharedComponentFilter(new UpdateFrame{m_Index=uint.MaxValue});
       Check(one.CalculateEntityCount()==1,"Isolated "+kind+" depot");
       var result=(IDictionary)prepare.Invoke(null,new object[]{em,one});
       Check(result.Contains(prefab),kind+" receives fleet reserve");
       var pair=result[prefab];var data=(TransportDepotData)pair.GetType().GetField("Item2").GetValue(pair);
       Check(data.m_VehicleCapacity>=3 && data.m_ProductionDuration==0,kind+" dispatch space without parked manufacturing");
       Check(data.m_MaintenanceDuration==7 && data.m_TransportType==kind,kind+" servicing and transport type preserved");
       Check(em.GetComponentData<TransportDepotData>(prefab).Equals(original),kind+" preparation leaves saved prefab unchanged");
       em.AddComponent<Game.Vehicles.CargoTransport>(vehicle);
       result=(IDictionary)prepare.Invoke(null,new object[]{em,one});Check(result.Count==0,kind+" shared cargo left native");
       em.RemoveComponent<Game.Vehicles.CargoTransport>(vehicle);
       var upgradePrefab=New(typeof(TransportDepotData));em.SetComponentData(upgradePrefab,new TransportDepotData{m_ProductionDuration=1});
       var upgrade=New(typeof(PrefabRef));em.SetComponentData(upgrade,new PrefabRef{m_Prefab=upgradePrefab});
       em.AddBuffer<InstalledUpgrade>(depot).Add(new InstalledUpgrade{m_Upgrade=upgrade});
       result=(IDictionary)prepare.Invoke(null,new object[]{em,one});Check(result.Count==0,kind+" manufacturing upgrade cannot spawn runaway reserve");
      }
      em.DestroyEntity(depot);
     }
     return new{passed=true,checks=checks.Count,assertions=checks,frame=sim.frameIndex};
    }
    finally{foreach(var e in entities)if(em.Exists(e))em.DestroyEntity(e);}
   }
  }
 }
}
