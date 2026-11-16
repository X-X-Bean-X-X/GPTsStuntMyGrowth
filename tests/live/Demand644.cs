using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChicagoBuildBridge;
using Colossal.Collections;
using Game;
using Game.Citizens;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Newtonsoft.Json.Linq;
using StuntYourGrowth;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ChicagoBuildExtensions
{
 public static class Entry
 {
  const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static readonly string[] Names={"MaximumLowResidentialDemand","MaximumMediumResidentialDemand","MaximumHighResidentialDemand","MaximumCommercialDemand","MaximumIndustrialDemand","MaximumOfficeDemand"};
  static object Field(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
  static int Value(object o,string n)=>((NativeValue<int>)Field(o,n)).value;
  static int[] ArrayOf(object o,string n)=>((NativeArray<int>)Field(o,n)).ToArray();
  static void Require(bool good,string text){if(!good)throw new InvalidOperationException(text);}
  public static object Run(BridgeSystem b,JObject request)
  {
   try{return Test(b,request);}catch(Exception e){return new{passed=false,error=e.ToString()};}
  }
  static object Test(BridgeSystem b,JObject request)
  {
   var sim=b.World.GetExistingSystemManaged<SimulationSystem>();
   Require(sim.selectedSpeed==0 && sim.frameIndex==request.Value<uint>("expectedFrame"),"Matching paused QA frame required");
   Require(request.Value<string>("qaCheckpoint")?.StartsWith("Chicago GPT - Stunt Growth QA ")==true,"QA checkpoint required");
   var asm=typeof(GPTStuntGrowthSettings).Assembly;
   Require(asm.GetName().Version.ToString()=="1.75.644.0","Loaded 644 required");
   var runtime=asm.GetType("StuntYourGrowth.Runtime");
   var s=(GPTStuntGrowthSettings)runtime.GetField("Options",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
   var saved=Names.ToDictionary(n=>n,n=>(bool)typeof(GPTStuntGrowthSettings).GetProperty(n).GetValue(s));
   bool master=s.Enabled, household=s.MaximumHouseholds;var growth=s.CityGrowth;
   object external=null;PropertyInfo externalEnabled=null;bool externalBefore=false;
   foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
   {
    var t=a.GetType("DemandPlus.Mod",false);if(t==null)continue;
    external=t.GetProperty("Settings",BindingFlags.Static|BindingFlags.Public).GetValue(null);
    if(external!=null){externalEnabled=external.GetType().GetProperty("Enabled");externalBefore=(bool)externalEnabled.GetValue(external);}break;
   }
   var em=b.EntityManager;
   var res=b.World.GetExistingSystemManaged<ResidentialDemandSystem>();
   var com=b.World.GetExistingSystemManaged<CommercialDemandSystem>();
   var ind=b.World.GetExistingSystemManaged<IndustrialDemandSystem>();
   if(request.Value<string>("mode")=="observe")
   {
    em.CompleteAllTrackedJobs();
    var d=((NativeValue<int3>)Field(res,"m_BuildingDemand")).value;
    var zone=b.World.GetExistingSystemManaged<ZoneSpawnSystem>();
    bool allowZone=(bool)runtime.GetMethod("AllowUpdate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{zone});
    return new{frame=sim.frameIndex,speed=sim.selectedSpeed,version=asm.GetName().Version.ToString(),assembly=asm.Location,residential=new[]{d.x,d.y,d.z},commercial=Value(com,"m_BuildingDemand"),industry=Value(ind,"m_IndustrialBuildingDemand"),office=Value(ind,"m_OfficeBuildingDemand"),redevelopmentHeld=s.Redevelopment,zoneSpawnAllowed=allowZone,demandPlusLoaded=external!=null};
   }
   var results=new List<object>();int checks=0;
   void Check(bool ok,string name){Require(ok,name);checks++;}
   void Set(int mask){for(int i=0;i<6;i++)typeof(GPTStuntGrowthSettings).GetProperty(Names[i]).SetValue(s,(mask&(1<<i))!=0);}
   void Tick(){em.CompleteAllTrackedJobs();res.Update();com.Update();ind.Update();em.CompleteAllTrackedJobs();}
   Snapshot Read()=>new Snapshot{Residential=((NativeValue<int3>)Field(res,"m_BuildingDemand")).value,Commercial=Value(com,"m_BuildingDemand"),Industry=Value(ind,"m_IndustrialBuildingDemand"),Office=Value(ind,"m_OfficeBuildingDemand"),CommercialResources=ArrayOf(com,"m_BuildingDemands"),IndustrialResources=ArrayOf(ind,"m_IndustrialBuildingDemands"),Storage=ArrayOf(ind,"m_StorageBuildingDemands"),Companies=ArrayOf(ind,"m_IndustrialCompanyDemands"),CommercialCompanies=ArrayOf(com,"m_ResourceDemands")};
   try
   {
    if(external!=null)externalEnabled.SetValue(external,false);
    s.Enabled=true;Set(0);Tick();Tick();var baseline=Read();
    var resources=b.World.GetExistingSystemManaged<ResourceSystem>().GetPrefabs();
    foreach(int mask in new[]{1,2,4,8,16,32,21,42,63})
    {
     Set(mask);Tick();var current=Read();
     for(int j=0;j<3;j++)Check(current.Residential[j]==((mask&(1<<j))!=0?100:baseline.Residential[j]),"Density isolation "+mask+"/"+j);
     Check(current.Commercial==((mask&8)!=0?100:baseline.Commercial),"Commercial scalar "+mask);
     Check(current.Industry==((mask&16)!=0?100:baseline.Industry),"Industrial scalar "+mask);
     Check(current.Office==((mask&32)!=0?100:baseline.Office),"Office scalar "+mask);
     var iterator=ResourceIterator.GetIterator();
     while(iterator.Next())
     {
      int i=EconomyUtils.GetResourceIndex(iterator.resource);var prefab=resources[iterator.resource];
      bool dataExists=em.HasComponent<ResourceData>(prefab);
      bool selectedCommercial=(mask&8)!=0 && EconomyUtils.IsCommercialResource(iterator.resource) && dataExists;
      Check(current.CommercialResources[i]==(selectedCommercial?100:baseline.CommercialResources[i]),"Commercial resource "+mask+"/"+iterator.resource);
      bool selectedIndustrial=false;
      if(dataExists){var data=em.GetComponentData<ResourceData>(prefab);selectedIndustrial=data.m_IsProduceable&&!data.m_IsMaterial&&(mask&(data.m_Weight==0?32:16))!=0;}
      Check(current.IndustrialResources[i]==(selectedIndustrial?100:baseline.IndustrialResources[i]),"Industrial resource "+mask+"/"+iterator.resource);
     }
     Check(current.Storage.SequenceEqual(baseline.Storage),"Storage demand untouched "+mask);
     Check(current.Companies.SequenceEqual(baseline.Companies)&&current.CommercialCompanies.SequenceEqual(baseline.CommercialCompanies),"Company demand untouched "+mask);
     results.Add(new{mask,residential=new[]{current.Residential.x,current.Residential.y,current.Residential.z},commercial=current.Commercial,industry=current.Industry,office=current.Office});
    }
    Set(0);Tick();Check(Read().Same(baseline),"All-off returns native demand");
    Set(63);s.Enabled=false;Tick();Check(Read().Same(baseline),"Master-off returns native demand");s.Enabled=true;
    if(external!=null)
    {
     externalEnabled.SetValue(external,true);Check(s.DemandControlsDisabled(),"Demand+ locks demand controls");
     Tick();Check(Read().Same(baseline),"Demand+ ownership suppresses Puppet demand writer");
     externalEnabled.SetValue(external,false);Check(!s.DemandControlsDisabled(),"Demand+ off unlocks demand controls");
     Tick();Check(Read().Commercial==100&&Read().Office==100,"Saved selection resumes after Demand+ off");
    }
    else Check(!s.DemandControlsDisabled(),"No Demand+ dependency when absent");
    var householdResults=new List<object>();
    using(var waiting=em.CreateEntityQuery(ComponentType.ReadOnly<Household>(),ComponentType.Exclude<PropertyRenter>(),ComponentType.Exclude<TouristHousehold>(),ComponentType.Exclude<CommuterHousehold>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>()))
    using(var properties=em.CreateEntityQuery(ComponentType.ReadOnly<BuildingPropertyData>()))
    {
     var capacityBefore=properties.ToComponentDataArray<BuildingPropertyData>(Allocator.Temp);
     try
     {
      var occupancy=b.World.GetExistingSystemManaged<OccupancyControlSystem>();
      var add=typeof(OccupancyControlSystem).GetMethod("AddHouseholds",F);
      var counts=b.World.GetExistingSystemManaged<CountResidentialPropertySystem>();
      int3 free=counts.FreeProperties;long vacant=(long)Math.Max(0,free.x)+Math.Max(0,free.y)+Math.Max(0,free.z);
      foreach(bool boost in new[]{false,true})
      {
       s.CityGrowth=CityGrowthMode.Occupancy;s.MaximumHouseholds=boost;
       var old=new HashSet<Entity>();using(var entities=waiting.ToEntityArray(Allocator.Temp))foreach(var e in entities)old.Add(e);
       int expected=(int)Math.Max(0,Math.Min(boost?16:4,Math.Min(vacant-old.Count,64-old.Count)));
       try
       {
        using(var commands=new EntityCommandBuffer(Allocator.TempJob))
        {
         object[] args={commands,new Unity.Mathematics.Random(123456)};add.Invoke(occupancy,args);((EntityCommandBuffer)args[0]).Playback(em);
        }
        int added=waiting.CalculateEntityCount()-old.Count;
        Check(added<=expected&&added>=0,"Household batch and vacancy/backlog limits");
        householdResults.Add(new{boost,available=vacant,waiting=old.Count,expectedMaximum=expected,added});
       }
       finally{using(var entities=waiting.ToEntityArray(Allocator.Temp))foreach(var e in entities)if(!old.Contains(e))em.DestroyEntity(e);}
      }
      // Isolated positive fixture: represent an empty recruitment queue without changing
      // existing households. No simulation frame runs while the query is substituted.
      var queueField=typeof(OccupancyControlSystem).GetField("waitingHomes",F);
      var originalQueue=(EntityQuery)queueField.GetValue(occupancy);
      using(var emptyQueue=em.CreateEntityQuery(ComponentType.ReadOnly<Household>(),ComponentType.ReadOnly<Game.Objects.OutsideConnection>()))
      {
       Check(emptyQueue.CalculateEntityCount()==0,"Isolated queue is empty");
       try
       {
        queueField.SetValue(occupancy,emptyQueue);
        foreach(bool boost in new[]{false,true})
        {
         s.CityGrowth=CityGrowthMode.Occupancy;s.MaximumHouseholds=boost;
         var old=new HashSet<Entity>();using(var entities=waiting.ToEntityArray(Allocator.Temp))foreach(var e in entities)old.Add(e);
         try
         {
          using(var commands=new EntityCommandBuffer(Allocator.TempJob))
          {
           object[] args={commands,new Unity.Mathematics.Random(123456)};add.Invoke(occupancy,args);((EntityCommandBuffer)args[0]).Playback(em);
          }
          int added=waiting.CalculateEntityCount()-old.Count;
          // Native outside-connection selection may stop recruitment before its budget.
          Check(added>0 && added<=Math.Min(boost?16:4,vacant),"Positive household fixture respects native connection selection and batch limit");
          if(boost) Check(added>4,"Household boost can exceed the ordinary occupancy batch");
          householdResults.Add(new{boost,isolatedEmptyQueue=true,added});
         }
         finally{using(var entities=waiting.ToEntityArray(Allocator.Temp))foreach(var e in entities)if(!old.Contains(e))em.DestroyEntity(e);}
        }
       }
       finally{queueField.SetValue(occupancy,originalQueue);}
      }
      s.CityGrowth=CityGrowthMode.Freeze;Check(!s.MaximumHouseholds&&s.OutputControlsDisabled(),"Freeze clears and locks household control");
      s.MaximumHouseholds=true;Check(!s.MaximumHouseholds,"Cannot enable households under Freeze");
      s.CityGrowth=CityGrowthMode.Population;Check(!s.MaximumHouseholds&&!s.OutputControlsDisabled(),"Leaving Freeze unlocks without rechecking");
      using(var after=properties.ToComponentDataArray<BuildingPropertyData>(Allocator.Temp))
      {
       Check(after.Length==capacityBefore.Length,"Property prefab count preserved");
       for(int i=0;i<after.Length;i++)Check(after[i].Equals(capacityBefore[i]),"Residential capacity unchanged");
      }
     }
     finally{capacityBefore.Dispose();}
    }
    Check(sim.selectedSpeed==0&&sim.frameIndex==request.Value<uint>("expectedFrame"),"QA frame stayed paused");
    return new{passed=true,checks,demands=results,households=householdResults,demandPlusLoaded=external!=null,demandPlusOriginalEnabled=externalBefore,frame=sim.frameIndex};
   }
   finally
   {
    s.Enabled=master;s.CityGrowth=growth;s.MaximumHouseholds=household;
    foreach(var pair in saved)typeof(GPTStuntGrowthSettings).GetProperty(pair.Key).SetValue(s,pair.Value);
    if(external!=null)externalEnabled.SetValue(external,externalBefore);
    Tick();
   }
  }
  sealed class Snapshot
  {
   public int3 Residential;public int Commercial,Industry,Office;
   public int[] CommercialResources,IndustrialResources,Storage,Companies,CommercialCompanies;
   public bool Same(Snapshot o)=>Residential.Equals(o.Residential)&&Commercial==o.Commercial&&Industry==o.Industry&&Office==o.Office&&CommercialResources.SequenceEqual(o.CommercialResources)&&IndustrialResources.SequenceEqual(o.IndustrialResources)&&Storage.SequenceEqual(o.Storage)&&Companies.SequenceEqual(o.Companies)&&CommercialCompanies.SequenceEqual(o.CommercialCompanies);
  }
 }
}
