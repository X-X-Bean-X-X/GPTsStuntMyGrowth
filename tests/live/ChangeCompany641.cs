// Private QA bridge fixture. Never shipped in the subscriber DLL.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ChicagoBuildBridge;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Newtonsoft.Json.Linq;
using StuntYourGrowth;
using Unity.Entities;
using Unity.Mathematics;

namespace ChicagoBuildExtensions
{
    public static class Entry
    {
        public static object Run(BridgeSystem bridge,JObject request)
        {
            try { return Test(bridge,request); }
            catch(Exception error) { return new { passed=false,error=error.ToString() }; }
        }
        static object Test(BridgeSystem bridge,JObject request)
        {
            var em=bridge.EntityManager;
            var sim=bridge.World.GetExistingSystemManaged<SimulationSystem>();
            if(sim==null || sim.selectedSpeed!=0 || request.Value<string>("qaCheckpoint")==null ||
                !request.Value<string>("qaCheckpoint").StartsWith("Chicago GPT - Stunt Growth QA ") ||
                request.Value<uint>("expectedFrame")!=sim.frameIndex)
                throw new InvalidOperationException("Paused QA checkpoint and matching frame required");
            if(typeof(GPTStuntGrowthSettings).Assembly.GetName().Version.ToString()!="1.75.641.0")
                throw new InvalidOperationException("Installed candidate 1.75.641 required");
            em.CompleteAllTrackedJobs();
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic;
            var type=typeof(ProtectionSystem);
            var controller=type.GetField("Current",hidden).GetValue(null);
            var queryField=type.GetField("workplaces",hidden);
            var modeField=type.GetField("workplaceMode",hidden);
            var baseline=(IDictionary)type.GetField("workplaceBaseline",hidden).GetValue(controller);
            var integrationField=type.GetField("changeCompanyWorkplacesOverride",hidden);
            if(integrationField.GetValue(controller)==null)throw new Exception("Automatic Change Company detection failed");
            var savedQuery=queryField.GetValue(controller);
            var savedMode=modeField.GetValue(controller);
            var savedBaseline=new Hashtable(baseline);
            // These temporary employers cannot be created by the fixture until the query is empty.
            // Swapping only the workplace query keeps all real employers outside the tested writer.
            var isolated=em.CreateEntityQuery(ComponentType.ReadWrite<WorkProvider>(),ComponentType.ReadOnly<Temp>());
            if(isolated.CalculateEntityCount()!=0){isolated.Dispose();throw new Exception("Temporary employers already exist; abort fixture");}
            var update=type.GetMethod("UpdateWorkplaces",hidden);
            var clear=type.GetMethod("ClearWorkplaces",hidden);
            var cc=bridge.World.GetExistingSystemManaged<ChangeCompany.CommercialWorkplacesSystem>();
            var ccUpdate=typeof(ChangeCompany.CompanyWorkplacesSystem).GetMethod("CompanyAISystemOnUpdatePostfixImpl",hidden);
            if(cc==null){isolated.Dispose();throw new Exception("Change Company system unavailable");}
            var settings=new GPTStuntGrowthSettings(new FixtureMod());
            settings.CityGrowth=CityGrowthMode.Population;
            settings.MaxIndustrialOutput=false;settings.MaxCivicOutput=false;
            var entities=new List<Entity>();var passed=new List<string>();
            Entity New(params ComponentType[] components){var e=em.CreateEntity(components);entities.Add(e);return e;}
            int Count(Entity e)=>em.GetComponentData<WorkProvider>(e).m_MaxWorkers;
            void SetCount(Entity e,int n)=>em.SetComponentData(e,new WorkProvider{m_MaxWorkers=n});
            void Tick(){update.Invoke(controller,new object[]{settings,false});em.CompleteAllTrackedJobs();}
            void Check(bool valid,string name){if(!valid)throw new Exception(name);passed.Add(name);}
            void Other(Entity e,int n)
            {
                if(!em.HasComponent<ChangeCompany.WorkplacesOverride>(e))em.AddComponent<ChangeCompany.WorkplacesOverride>(e);
                em.SetComponentData(e,new ChangeCompany.WorkplacesOverride{Value=n});
                ccUpdate.Invoke(cc,null);em.CompleteAllTrackedJobs();
                Check(Count(e)==n,"Change Company applied "+n);
            }
            try
            {
                queryField.SetValue(controller,isolated);clear.Invoke(controller,null);
                var employer=New(typeof(Temp),typeof(WorkProvider),typeof(Game.Companies.ProcessingCompany),typeof(UpdateFrame),typeof(PrefabRef),typeof(ServiceAvailable));
                em.AddBuffer<Game.Economy.Resources>(employer);em.AddBuffer<TradeCost>(employer);
                em.SetSharedComponentManaged(employer,new UpdateFrame{m_Index=SimulationUtils.GetUpdateFrame(sim.frameIndex,CommercialAISystem.kUpdatesPerDay,16)});
                foreach(var mode in new[]{EmploymentMode.Freeze,EmploymentMode.PreventDecreases})
                {
                    if(em.HasComponent<ChangeCompany.WorkplacesOverride>(employer))em.RemoveComponent<ChangeCompany.WorkplacesOverride>(employer);
                    clear.Invoke(controller,null);settings.Jobs=mode;SetCount(employer,10);Tick();
                    SetCount(employer,5);Tick();Check(Count(employer)==10,mode+" still protects ordinary employers");
                    foreach(int n in new[]{25,5,0,40}){Other(employer,n);Tick();Check(Count(employer)==n,mode+" yields to custom count "+n);}
                    em.RemoveComponent<ChangeCompany.WorkplacesOverride>(employer);Tick();
                    SetCount(employer,7);Tick();Check(Count(employer)==40,mode+" starts fresh after override removal");
                }
                settings.Jobs=EmploymentMode.Normal;SetCount(employer,19);Tick();Check(Count(employer)==19,"Normal leaves jobs alone");
                var prefab=New(typeof(IndustrialProcessData));
                em.SetComponentData(prefab,new IndustrialProcessData{m_MaxWorkersPerCell=3});
                var buildingPrefab=New(typeof(BuildingData),typeof(BuildingPropertyData),typeof(SpawnableBuildingData));
                em.SetComponentData(buildingPrefab,new BuildingData{m_LotSize=new int2(2,3)});
                em.SetComponentData(buildingPrefab,new BuildingPropertyData{m_SpaceMultiplier=1});
                em.SetComponentData(buildingPrefab,new SpawnableBuildingData{m_Level=1});
                var building=New(typeof(Building),typeof(PrefabRef));em.SetComponentData(building,new PrefabRef{m_Prefab=buildingPrefab});
                em.AddComponent<CompanyData>(employer);em.AddComponent<IndustrialCompany>(employer);
                em.AddComponentData(employer,new PropertyRenter{m_Property=building});
                em.SetComponentData(employer,new PrefabRef{m_Prefab=prefab});
                settings.Jobs=EmploymentMode.Freeze;settings.MaxIndustrialOutput=true;
                object[] native={employer,0};
                Check((bool)type.GetMethod("TryNativeCapacity",hidden).Invoke(controller,native),"Native industrial capacity available");
                int capacity=(int)native[1];
                SetCount(employer,1);Tick();Check(Count(employer)==capacity && capacity>1,"Industrial target still works without override");
                foreach(int n in new[]{capacity+20,1,0}){Other(employer,n);Tick();Check(Count(employer)==n,"Industrial output yields to custom count "+n);}
                em.RemoveComponent<ChangeCompany.WorkplacesOverride>(employer);Tick();Check(Count(employer)==capacity,"Industrial targeting resumes after override removal");
                return new{passed=true,checks=passed.Count,assertions=passed,frame=sim.frameIndex};
            }
            finally
            {
                em.CompleteAllTrackedJobs();foreach(var entity in entities)if(em.Exists(entity))em.DestroyEntity(entity);
                queryField.SetValue(controller,savedQuery);modeField.SetValue(controller,savedMode);
                baseline.Clear();foreach(DictionaryEntry entry in savedBaseline)baseline.Add(entry.Key,entry.Value);
                isolated.Dispose();
            }
        }
    }
    sealed class FixtureMod : Game.Modding.IMod
    {
        public void OnLoad(Game.UpdateSystem updateSystem) {}
        public void OnDispose() {}
    }
}
