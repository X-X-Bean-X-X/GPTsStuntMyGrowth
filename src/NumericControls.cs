using System;
using Game.Buildings;
using Game.Citizens;
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
        EntityQuery numericCompanies, numericHouseholds, numericJobs, numericLand;
        (ValueMode,ValueMode,ValueMode,ValueMode,ValueMode,int,int,int,int)? numericSelection;

        void InitNumeric()
        {
            numericCompanies = NumericQuery<CompanyStatisticData>();
            numericHouseholds = NumericQuery<Household>();
            numericJobs = NumericQuery<WorkProvider>();
            numericLand = NumericQuery<Game.Net.LandValue>();
        }
        EntityQuery NumericQuery<T>() where T : unmanaged,IComponentData => GetEntityQuery(
            ComponentType.ReadWrite<T>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(),
            ComponentType.Exclude<Destroyed>());
        void ClearNumeric() { numericSelection = null; }
        void NumericPass<T>(EntityQuery query, bool refresh, Action<Entity> visit) where T : unmanaged,IComponentData
        {
            if (refresh) query.ResetFilter();
            else
            {
                query.SetChangedVersionFilter(ComponentType.ReadWrite<T>());

            }
            using (var entities = query.ToEntityArray(Allocator.Temp)) foreach (var entity in entities) visit(entity);
        }
        void UpdateNumeric(GPTStuntGrowthSettings s)
        {
            var selected = (s.Customers,s.JobControl,s.Income,s.LandControl,s.Profit,s.CustomerTarget,s.IncomeTarget,s.LandValueTarget,s.ProfitTarget);
            bool refresh = !numericSelection.HasValue || !numericSelection.Value.Equals(selected) || (sim.frameIndex & 127u)==0;
            numericSelection = selected;
            // Explicit overrides apply immediately, including to newly created entities; no baseline is required.
            if (Policy.IsOverride(s.Customers) || Policy.IsOverride(s.Income) || Policy.IsOverride(s.Profit))
                NumericPass<CompanyStatisticData>(numericCompanies,refresh,e =>
                {
                    var v=EntityManager.GetComponentData<CompanyStatisticData>(e); var old=v;
                    v.m_Income=Policy.Target(v.m_Income,s.Income,s.IncomeTarget,1000000);
                    v.m_Profit=Policy.Target(v.m_Profit,s.Profit,s.ProfitTarget,1000000);
                    if (Policy.IsOverride(s.Customers))
                    {
                        v.m_MonthlyCustomerCount=Policy.Target(v.m_MonthlyCustomerCount,s.Customers,s.CustomerTarget,100000);
                        v.m_CurrentNumberOfCustomers=v.m_MonthlyCustomerCount;
                    }
                    // m_MaxNumberOfCustomers is a historical record, not a capacity. Leave it untouched.
                    if (!v.Equals(old)) EntityManager.SetComponentData(e,v);
                });
            if (Policy.IsOverride(s.Income)) NumericPass<Household>(numericHouseholds,refresh,e =>
            {
                var v=EntityManager.GetComponentData<Household>(e);
                int target=Policy.Target(v.m_Income,s.Income,s.IncomeTarget,1000000);
                if (v.m_Income!=target) { v.m_Income=target; EntityManager.SetComponentData(e,v); }
            });
            if (Policy.IsOverride(s.LandControl))
            {
                float target=Policy.Target(0,s.LandControl,s.LandValueTarget,10000);
                NumericPass<Game.Net.LandValue>(numericLand,refresh,e =>
                {
                    var v=EntityManager.GetComponentData<Game.Net.LandValue>(e);
                    if (v.m_LandValue!=target) { v.m_LandValue=target; EntityManager.SetComponentData(e,v); }
                });
                if (refresh) SetLandMap(World.GetOrCreateSystemManaged<LandValueSystem>(),s);
            }
            if (Policy.IsOverride(s.JobControl)) UpdateJobTargets(s,refresh);
        }
        internal static void SetLandMap(LandValueSystem system, GPTStuntGrowthSettings s)
        {
            var values=system.GetMap(false,out var dependency); dependency.Complete();
            float target=Policy.Target(0,s.LandControl,s.LandValueTarget,10000);
            for(int i=0;i<values.Length;i++) if (values[i].m_LandValue!=target)
                values[i]=new LandValueCell { m_LandValue=target };
        }
        void UpdateJobTargets(GPTStuntGrowthSettings s,bool refresh)
        {
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
            var workplaces=GetComponentLookup<WorkplaceData>(true);
            var schools=GetComponentLookup<SchoolData>(true);
            var students=GetBufferLookup<Game.Buildings.Student>(true);
            NumericPass<WorkProvider>(numericJobs,refresh,e =>
            {
                // Do not change external cities or hidden entities with no native workplace definition.
                if (EntityManager.HasComponent<Game.Objects.OutsideConnection>(e) || !prefabs.HasComponent(e)) return;
                var prefab=prefabs[e].m_Prefab;
                int capacity;
                if (EntityManager.HasComponent<CompanyData>(e) && EntityManager.HasComponent<PropertyRenter>(e))
                {
                    var property=EntityManager.GetComponentData<PropertyRenter>(e).m_Property;
                    if (!prefabs.HasComponent(property) || deleted.HasComponent(property) || EntityManager.HasComponent<Destroyed>(property)) return;
                    var bp=prefabs[property].m_Prefab;
                    bool extractor=extractors.HasComponent(prefab);
                    if (extractor && !processes.HasComponent(prefab)) return;
                    if (!extractor && (!buildings.HasComponent(bp) || !properties.HasComponent(bp) ||
                        (!services.HasComponent(prefab) && !processes.HasComponent(prefab)))) return;
                    capacity=CompanyUtils.GetCompanyMaxFittingWorkers(e,property,ref prefabs,ref services,ref buildings,
                        ref properties,ref levels,ref processes,ref extractors,ref attached,ref subAreas,ref upgrades,ref lots,ref geometries);
                }
                else if (workplaces.HasComponent(prefab))
                {
                    // Native school staffing may scale with enrollment; no enrollment or education is modified.
                    if (schools.HasComponent(prefab) && upgrades.HasBuffer(e) &&
                        (!students.HasBuffer(e) || schools[prefab].m_StudentCapacity<=0)) return;
                    capacity=Game.City.CityUtils.GetCityServiceWorkplaceMaxWorkers(e,ref prefabs,ref upgrades,
                        ref deleted,ref workplaces,ref schools,ref students);
                }
                else return;
                if (capacity<0) return;
                var v=EntityManager.GetComponentData<WorkProvider>(e);
                int target=s.JobControl==ValueMode.Zero ? 0 : capacity;
                if (v.m_MaxWorkers!=target) { v.m_MaxWorkers=target; EntityManager.SetComponentData(e,v); }
            });
        }
    }
}
