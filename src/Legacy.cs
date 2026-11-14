using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Colossal.PSI.Environment;
using Colossal.Serialization.Entities;
using Game;
using Game.City;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

namespace ChicagoPopulationHold {
 public sealed class Mod:IMod {
  public void OnLoad(UpdateSystem updateSystem){StuntYourGrowth.Runtime.Start(this,updateSystem);}
  public void OnDispose(){HoldSystem.ReleaseAll();StuntYourGrowth.Runtime.Stop();}
 }
 // Retired compatibility surface. No city-specific config is read or applied.
 public sealed class HoldSystem:GameSystemBase {
  public static string ConfigPath => Path.Combine(EnvPath.kUserDataPath, "ModsSettings", StuntYourGrowth.Runtime.Id, "population-hold-settings.json");
  public static readonly HashSet<World> Registered=new HashSet<World>();
  public string LastError = "City-specific hold retired; use Stop Population Growth.";
  public static void Register(UpdateSystem u) { }
  internal static void ReleaseAll() { }
  public void Evaluate(bool force=false) { }
  public object State()=>new { held=false, retired=true, error=LastError };
  protected override void OnUpdate() { }
 }

}
namespace ChicagoBuildExtensions {
 public static class Entry {
  static void Need(bool b,string s){if(!b)throw new Exception(s);}
  static int[] Id(Entity e)=>new[]{e.Index,e.Version};
  static string Hash(object x){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x)))).Replace("-","").ToLowerInvariant();}
  static Entity[] Residents(EntityManager em){using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<Household>(),ComponentType.Exclude<TouristHousehold>(),ComponentType.Exclude<CommuterHousehold>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>()))using(var a=q.ToEntityArray(Allocator.Temp))return a.ToArray().OrderBy(e=>e.Index).ToArray();}
  static object Snapshot(GameSystemBase b){var em=b.EntityManager;var households=Residents(em);var ids=new HashSet<Entity>(households);int people=0;using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<Citizen>(),ComponentType.ReadOnly<HouseholdMember>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>()))using(var es=q.ToEntityArray(Allocator.Temp))foreach(var e in es)if(ids.Contains(em.GetComponentData<HouseholdMember>(e).m_Household))people++;
   var renters=new List<object>();using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<Game.Buildings.Building>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<Owner>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>()))using(var es=q.ToEntityArray(Allocator.Temp))foreach(var e in es){var p=em.GetComponentData<PrefabRef>(e).m_Prefab;if(!em.HasComponent<BuildingPropertyData>(p)||em.GetComponentData<BuildingPropertyData>(p).m_ResidentialProperties==0)continue;var buf=em.HasBuffer<Game.Buildings.Renter>(e)?em.GetBuffer<Game.Buildings.Renter>(e,true).ToNativeArray(Allocator.Temp):default(NativeArray<Game.Buildings.Renter>);var members=new List<int[]>();if(buf.IsCreated){foreach(var x in buf)if(ids.Contains(x.m_Renter))members.Add(Id(x.m_Renter));buf.Dispose();}renters.Add(new{entity=Id(e),prefab=b.World.GetExistingSystemManaged<PrefabSystem>().GetPrefabName(p),residentRenters=members.Count});}
   var rows=households.Select(e=>new{entity=Id(e),members=em.HasBuffer<HouseholdCitizen>(e)?em.GetBuffer<HouseholdCitizen>(e,true).Length:0,renter=em.HasComponent<Game.Buildings.PropertyRenter>(e)?Id(em.GetComponentData<Game.Buildings.PropertyRenter>(e).m_Property):null,flags=em.GetComponentData<Household>(e).m_Flags.ToString()}).ToArray();
   return new{residentHouseholds=households.Length,residentCitizens=people,households=rows,residentialBuildings=renters};
  }
  public static object Run(GameSystemBase b,JObject c){bool written=false;try{
   var em=b.EntityManager;var sim=b.World.GetExistingSystemManaged<SimulationSystem>();Need(sim.selectedSpeed==0&&sim.frameIndex==(uint)c["expectedFrame"],"Paused matching frame required");Need(b.World.GetExistingSystemManaged<ToolSystem>().activeTool.toolID=="Default Tool","Default tool required");em.CompleteAllTrackedJobs();string city=b.World.GetExistingSystemManaged<CityConfigurationSystem>().cityName;string mode=(string)c["mode"];
   if(mode=="inspect"){var snap=Snapshot(b);return new{status="inspected",city,frame=sim.frameIndex,snapshot=snap,sha256=Hash(snap),hold=b.World.GetExistingSystemManaged<ChicagoPopulationHold.HoldSystem>()?.State()};}
   Need(city==(string)c["expectedCity"],"City changed");using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<Temp>(),ComponentType.Exclude<Deleted>()))Need(q.CalculateEntityCount()==0,"Active preview");using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<CreationDefinition>()))Need(q.CalculateEntityCount()==0,"Pending construction");
   if(mode=="enable-and-relocate") throw new InvalidOperationException("Legacy resident relocation is retired. Use the mod Options controls.");
   if(mode=="refresh-counts"){
    Need(Residents(em).Length==0,"Resident removal incomplete");var barrier=b.World.GetExistingSystemManaged<EndFrameBarrier>();barrier.AllowUsage();b.World.GetExistingSystemManaged<CountHouseholdDataSystem>().Update();barrier.Update();em.CompleteAllTrackedJobs();var snap=Snapshot(b);var cityEntity=b.World.GetExistingSystemManaged<CitySystem>().City;return new{status="verified",frame=sim.frameIndex,snapshot=snap,sha256=Hash(snap),nativePopulation=em.GetComponentData<Population>(cityEntity).m_Population,hold=b.World.GetExistingSystemManaged<ChicagoPopulationHold.HoldSystem>()?.State()};
   }
   throw new Exception("Unknown mode");
  }catch(Exception ex){return new{status="rejected",written,error=ex.ToString()};}}
 }
}
