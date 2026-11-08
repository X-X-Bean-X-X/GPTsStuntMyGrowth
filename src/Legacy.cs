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
  public void OnLoad(UpdateSystem updateSystem){HoldSystem.Register(updateSystem);StuntYourGrowth.Runtime.Start(this,updateSystem);}
  public void OnDispose(){HoldSystem.ReleaseAll();StuntYourGrowth.Runtime.Stop();}
 }
 // A reversible, city-specific immigration gate. Never removes residents automatically.
 public sealed class HoldSystem:GameSystemBase {
  private static string configPath;
  public static string ConfigPath => configPath ?? (configPath = GetConfigPath());

  private static string GetConfigPath()
  {
      string folder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", StuntYourGrowth.Runtime.Id);
      string pointer = Path.Combine(folder, "legacy-config-location.txt");
      if (File.Exists(pointer))
      {
          string existing = File.ReadAllText(pointer).Trim();
          if (Path.IsPathRooted(existing)) return existing;
      }
      return Path.Combine(folder, "population-hold-settings.json");
  }
  public static readonly HashSet<World> Registered=new HashSet<World>();
  bool held,oldSpawn,oldFind;DateTime nextRead;string city;JObject config;
  public string LastError;
  public static void Register(UpdateSystem u){if(!Registered.Add(u.World))return;u.UpdateBefore<HoldSystem>(SystemUpdatePhase.MainLoop);u.UpdateBefore<HoldSystem,HouseholdSpawnSystem>(SystemUpdatePhase.GameSimulation);}
  internal static void ReleaseAll(){foreach(var world in Registered.ToArray())if(world.IsCreated)world.GetExistingSystemManaged<HoldSystem>()?.Restore();}
  void Restore(){if(!held)return;var spawn=World.GetExistingSystemManaged<HouseholdSpawnSystem>();var find=World.GetExistingSystemManaged<HouseholdFindPropertySystem>();if(spawn!=null)spawn.Enabled=oldSpawn;if(find!=null)find.Enabled=oldFind;held=false;}
  public void Evaluate(bool force=false){
   try{
    if(force||DateTime.UtcNow>=nextRead){config=File.Exists(ConfigPath)?JObject.Parse(File.ReadAllText(ConfigPath)):null;nextRead=DateTime.UtcNow.AddSeconds(2);}
    var manager=GameManager.instance;if(manager==null||manager.isGameLoading)return;
    city=World.GetExistingSystemManaged<CityConfigurationSystem>()?.cityName;
    bool apply=!StuntYourGrowth.Runtime.Disposed&&(StuntYourGrowth.Runtime.Options==null || (StuntYourGrowth.Runtime.Options.Enabled && StuntYourGrowth.Runtime.Options.LegacyHold))&&(int)manager.gameMode==2&&config!=null&&(bool?)config["enabled"]==true&&city==(string)config["cityName"];
    if(!apply){Restore();return;}
    var spawn=World.GetExistingSystemManaged<HouseholdSpawnSystem>();var find=World.GetExistingSystemManaged<HouseholdFindPropertySystem>();
    if(spawn==null||find==null){LastError="Native migration systems unavailable";return;}
    if(!held){oldSpawn=spawn.Enabled;oldFind=find.Enabled;held=true;}
    spawn.Enabled=false;find.Enabled=false;LastError=null;
   }catch(Exception ex){LastError=ex.Message;/* Keep an already active gate closed on config-read failure. */}
  }
  public object State()=>new{held,city,configPath=ConfigPath,configured=(bool?)config?["enabled"],spawnEnabled=World.GetExistingSystemManaged<HouseholdSpawnSystem>()?.Enabled,findPropertyEnabled=World.GetExistingSystemManaged<HouseholdFindPropertySystem>()?.Enabled,previousSpawnEnabled=oldSpawn,previousFindPropertyEnabled=oldFind,error=LastError};
  protected override void OnUpdate(){Evaluate();}
  protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose,GameMode mode){Restore();config=null;nextRead=DateTime.MinValue;base.OnGamePreload(purpose,mode);}
  protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose,GameMode mode){base.OnGameLoadingComplete(purpose,mode);Evaluate(true);}
  protected override void OnDestroy(){Restore();Registered.Remove(World);base.OnDestroy();}
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
   if(mode=="enable-and-relocate"){
    Need((bool?)c["apply"]==true,"Explicit apply required");var before=Snapshot(b);Need(Hash(before)==(string)c["expectedSHA256"],"Resident inventory changed");var targets=Residents(em);Need(targets.Length==(int)c["expectedHouseholds"]&&targets.Length<=2048,"Household bound changed");
    var settings=JObject.Parse(File.ReadAllText(ChicagoPopulationHold.HoldSystem.ConfigPath));Need((bool?)settings["enabled"]==true&&(string)settings["cityName"]==city,"Persistent gate not configured");
    ChicagoPopulationHold.HoldSystem.Register(b.World.GetExistingSystemManaged<UpdateSystem>());var hold=b.World.GetExistingSystemManaged<ChicagoPopulationHold.HoldSystem>();hold.Evaluate(true);var state=JObject.FromObject(hold.State());Need((bool)state["held"]&&!(bool)state["spawnEnabled"]&&!(bool)state["findPropertyEnabled"],"Migration gate did not close");
    written=true;foreach(var e in targets)em.AddComponent<Deleted>(e);
    var barrier=b.World.GetExistingSystemManaged<ModificationBarrier2>();barrier.AllowUsage();b.World.GetExistingSystemManaged<HouseholdAndCitizenRemoveSystem>().Update();barrier.Update();em.CompleteAllTrackedJobs();
    return new{status="relocated",written=true,frame=sim.frameIndex,householdsRemoved=targets.Length,hold=hold.State(),after=Snapshot(b),qualification="One-time resident removal through native household cleanup. Existing buildings, visitors and commuters are not deletion targets. Immigration gate remains until explicitly released."};
   }
   if(mode=="refresh-counts"){
    Need(Residents(em).Length==0,"Resident removal incomplete");var barrier=b.World.GetExistingSystemManaged<EndFrameBarrier>();barrier.AllowUsage();b.World.GetExistingSystemManaged<CountHouseholdDataSystem>().Update();barrier.Update();em.CompleteAllTrackedJobs();var snap=Snapshot(b);var cityEntity=b.World.GetExistingSystemManaged<CitySystem>().City;return new{status="verified",frame=sim.frameIndex,snapshot=snap,sha256=Hash(snap),nativePopulation=em.GetComponentData<Population>(cityEntity).m_Population,hold=b.World.GetExistingSystemManaged<ChicagoPopulationHold.HoldSystem>()?.State()};
   }
   throw new Exception("Unknown mode");
  }catch(Exception ex){return new{status="rejected",written,error=ex.ToString()};}}
 }
}
