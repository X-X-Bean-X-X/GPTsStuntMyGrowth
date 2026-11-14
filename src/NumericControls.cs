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
        EntityQuery numericCompanies, numericLand;
        (ValueMode,ValueMode,ValueMode,int,int,int)? numericSelection;

        void InitNumeric()
        {
            numericCompanies = NumericQuery<CompanyStatisticData>();
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
            var selected = (s.CustomerControl,s.LandControl,s.Profit,s.CustomerTarget,s.LandValueTarget,s.ProfitTarget);
            bool refresh = !numericSelection.HasValue || !numericSelection.Value.Equals(selected) || (sim.frameIndex & 127u)==0;
            numericSelection = selected;
            // Explicit overrides apply immediately, including to newly created entities; no baseline is required.
            if (Policy.IsOverride(s.CustomerControl) || Policy.IsOverride(s.Profit))
                NumericPass<CompanyStatisticData>(numericCompanies,refresh,e =>
                {
                    var v=EntityManager.GetComponentData<CompanyStatisticData>(e); var old=v;
                    v.m_Profit=Policy.Target(v.m_Profit,s.Profit,s.ProfitTarget,1000000);
                    if (Policy.IsOverride(s.CustomerControl))
                    {
                        v.m_MonthlyCustomerCount=Policy.Target(v.m_MonthlyCustomerCount,s.CustomerControl,s.CustomerTarget,100000);
                        v.m_CurrentNumberOfCustomers=v.m_MonthlyCustomerCount;
                    }
                    // m_MaxNumberOfCustomers is a historical record, not a capacity. Leave it untouched.
                    if (!v.Equals(old)) EntityManager.SetComponentData(e,v);
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
        }
        internal static void SetLandMap(LandValueSystem system, GPTStuntGrowthSettings s)
        {
            var values=system.GetMap(false,out var dependency); dependency.Complete();
            float target=Policy.Target(0,s.LandControl,s.LandValueTarget,10000);
            for(int i=0;i<values.Length;i++) if (values[i].m_LandValue!=target)
                values[i]=new LandValueCell { m_LandValue=target };
        }
    }
}
