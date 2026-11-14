using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace StuntYourGrowth
{
    public sealed partial class ProtectionSystem
    {
        EntityQuery workplaces;
        readonly Dictionary<Entity,(int mode,int count)> workplaceBaseline=new Dictionary<Entity,(int,int)>();
        int workplaceMode=-1;
        void InitWorkplaces() => workplaces=GetEntityQuery(ComponentType.ReadWrite<WorkProvider>(),
            ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Destroyed>(),ComponentType.Exclude<Game.Objects.OutsideConnection>());
        void ClearWorkplaces() { workplaceBaseline.Clear();workplaceMode=-1; }
        void UpdateWorkplaces(GPTStuntGrowthSettings s,bool prune)
        {
            int mode=(int)s.JobControl | (s.MaxIndustrialOutput?8:0) | (s.MaxCivicOutput?16:0);
            if(mode==0) {if(workplaceMode!=0)ClearWorkplaces();workplaceMode=0;return;}
            bool refresh=mode!=workplaceMode || (sim.frameIndex & 127u)==0;
            if(mode!=workplaceMode)workplaceBaseline.Clear();
            workplaceMode=mode;
            if(refresh)workplaces.ResetFilter();else workplaces.SetChangedVersionFilter(ComponentType.ReadWrite<WorkProvider>());
            using(var entities=workplaces.ToEntityArray(Allocator.Temp)) foreach(var e in entities)
            {
                var current=EntityManager.GetComponentData<WorkProvider>(e);
                int target=current.m_MaxWorkers;
                if(TargetedWorkplace(e,s) && TryNativeCapacity(e,out var capacity))
                {
                    // Scoped output targets are the sole owner for this employer, even when Jobs is Freeze.
                    target=capacity;
                    workplaceBaseline.Remove(e);
                }
                else if(s.JobControl==ValueMode.Freeze || s.JobControl==ValueMode.PreventDecreases)
                {
                    if(workplaceBaseline.TryGetValue(e,out var previous))target=Policy.Protect(previous.count,target,s.JobControl);
                    workplaceBaseline[e]=(mode,target);
                }
                if(target!=current.m_MaxWorkers) {current.m_MaxWorkers=target;EntityManager.SetComponentData(e,current);}
            }
            if(prune)
            {
                var expired=new List<Entity>();foreach(var e in workplaceBaseline.Keys)if(!EntityManager.HasComponent<WorkProvider>(e))expired.Add(e);
                foreach(var e in expired)workplaceBaseline.Remove(e);
            }
        }
        bool TargetedWorkplace(Entity e,GPTStuntGrowthSettings s)
        {
            if(!EntityManager.HasComponent<PrefabRef>(e))return false;
            if(EntityManager.HasComponent<CompanyData>(e))
            {
                if(!s.MaxIndustrialOutput || !EntityManager.HasComponent<IndustrialCompany>(e) || !EntityManager.HasComponent<PropertyRenter>(e))return false;
                var property=EntityManager.GetComponentData<PropertyRenter>(e).m_Property;
                return EntityManager.HasComponent<Building>(property) && !EntityManager.HasComponent<OfficeProperty>(property) && Operational(property);
            }
            return s.MaxCivicOutput && EntityManager.HasComponent<Building>(e) && Operational(e);
        }
        bool Operational(Entity building)
        {
            if(EntityManager.HasComponent<Deleted>(building) || EntityManager.HasComponent<Destroyed>(building) || EntityManager.HasComponent<Abandoned>(building))return false;
            if(EntityManager.HasBuffer<Efficiency>(building))
                foreach(var factor in EntityManager.GetBuffer<Efficiency>(building,true))
                    if((factor.m_Factor==EfficiencyFactor.Disabled || factor.m_Factor==EfficiencyFactor.Destroyed || factor.m_Factor==EfficiencyFactor.Fire || factor.m_Factor==EfficiencyFactor.Abandoned) && factor.m_Efficiency<=0)return false;
            return true;
        }
        bool TryNativeCapacity(Entity e,out int capacity)
        {
            capacity=0;
            var prefabs=GetComponentLookup<PrefabRef>(true);
            var services=GetComponentLookup<ServiceCompanyData>(true);
            var buildings=GetComponentLookup<BuildingData>(true);
            var properties=GetComponentLookup<BuildingPropertyData>(true);
            var levels=GetComponentLookup<SpawnableBuildingData>(true);
            var processes=GetComponentLookup<IndustrialProcessData>(true);
            var extractors=GetComponentLookup<ExtractorCompanyData>(true);
            var attached=GetComponentLookup<Game.Objects.Attached>(true);
            var subAreas=GetBufferLookup<Game.Areas.SubArea>(true);
            var upgrades=GetBufferLookup<InstalledUpgrade>(true);
            var lots=GetComponentLookup<Game.Areas.Lot>(true);
            var geometries=GetComponentLookup<Game.Areas.Geometry>(true);
            var deleted=GetComponentLookup<Deleted>(true);
            var workplace=GetComponentLookup<WorkplaceData>(true);
            var schools=GetComponentLookup<SchoolData>(true);
            var students=GetBufferLookup<Game.Buildings.Student>(true);
            if(!prefabs.HasComponent(e))return false;
            var prefab=prefabs[e].m_Prefab;
            if(EntityManager.HasComponent<CompanyData>(e) && EntityManager.HasComponent<PropertyRenter>(e))
            {
                var property=EntityManager.GetComponentData<PropertyRenter>(e).m_Property;
                if(!prefabs.HasComponent(property))return false;
                var bp=prefabs[property].m_Prefab;
                if(extractors.HasComponent(prefab)) {if(!processes.HasComponent(prefab))return false;}
                else if(!buildings.HasComponent(bp) || !properties.HasComponent(bp) || (!services.HasComponent(prefab) && !processes.HasComponent(prefab)))return false;
                capacity=CompanyUtils.GetCompanyMaxFittingWorkers(e,property,ref prefabs,ref services,ref buildings,ref properties,ref levels,ref processes,ref extractors,ref attached,ref subAreas,ref upgrades,ref lots,ref geometries);
            }
            else if(workplace.HasComponent(prefab))
            {
                if(schools.HasComponent(prefab) && upgrades.HasBuffer(e) && (!students.HasBuffer(e) || schools[prefab].m_StudentCapacity<=0))return false;
                capacity=Game.City.CityUtils.GetCityServiceWorkplaceMaxWorkers(e,ref prefabs,ref upgrades,ref deleted,ref workplace,ref schools,ref students);
            }
            else return false;
            return capacity>=0;
        }
    }
}
