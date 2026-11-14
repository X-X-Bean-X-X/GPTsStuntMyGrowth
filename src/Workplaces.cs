using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace StuntYourGrowth
{
    // Only supporting weather/bonus factors are held. Operational requirements and
    // unknown future factors always retain the current native value.
    internal static class EfficiencyPolicy
    {
        internal static bool Protected(EfficiencyFactor factor)
        {
            switch(factor)
            {
                case EfficiencyFactor.WindSpeed:
                case EfficiencyFactor.WaterDepth:
                case EfficiencyFactor.SunIntensity:
                case EfficiencyFactor.CityModifierSoftware:
                case EfficiencyFactor.CityModifierElectronics:
                case EfficiencyFactor.CityModifierIndustrialEfficiency:
                case EfficiencyFactor.CityModifierOfficeEfficiency:
                case EfficiencyFactor.CityModifierHospitalEfficiency:
                case EfficiencyFactor.SpecializationBonus:
                case EfficiencyFactor.CityModifierFishInput:
                case EfficiencyFactor.CityModifierFishHub: return true;
                default: return false;
            }
        }
        static bool OutOfService(Efficiency[] values)
        {
            foreach(var value in values)
                if((value.m_Factor==EfficiencyFactor.Disabled || value.m_Factor==EfficiencyFactor.Destroyed ||
                    value.m_Factor==EfficiencyFactor.Fire || value.m_Factor==EfficiencyFactor.Abandoned) && value.m_Efficiency<=0) return true;
            return false;
        }
        internal static Efficiency[] Reconcile(Efficiency[] previous,Efficiency[] current)
        {
            var result=(Efficiency[])current.Clone();
            if(OutOfService(previous) || OutOfService(current))return result;
            for(int i=0;i<result.Length;i++)
            {
                if(!Protected(result[i].m_Factor))continue;
                foreach(var old in previous)
                    if(old.m_Factor==result[i].m_Factor && !float.IsNaN(old.m_Efficiency) && !float.IsInfinity(old.m_Efficiency))
                    {
                        result[i].m_Efficiency=Math.Max(result[i].m_Efficiency,old.m_Efficiency);
                        break;
                    }
            }
            // Never recreate factors removed by the native simulation or another mod.
            return result;
        }
    }

    internal sealed class EfficiencyGuard
    {
        readonly EntityManager em;
        readonly EntityQuery buildings;
        readonly Dictionary<Entity,Efficiency[]> baseline=new Dictionary<Entity,Efficiency[]>();
        bool enabled;
        public EfficiencyGuard(EntityManager em,EntityQuery buildings){this.em=em;this.buildings=buildings;}
        public void Clear(){baseline.Clear();enabled=false;}
        public void Tick(bool hold,bool prune)
        {
            if(!hold){if(enabled)Clear();return;}
            if(enabled)buildings.SetChangedVersionFilter(ComponentType.ReadWrite<Efficiency>());
            else buildings.ResetFilter();
            enabled=true;
            using(var list=buildings.ToEntityArray(Allocator.Temp)) foreach(var e in list)
            {
                var current=em.GetBuffer<Efficiency>(e,true).AsNativeArray().ToArray();
                var held=baseline.TryGetValue(e,out var old) ? EfficiencyPolicy.Reconcile(old,current) : current;
                DynamicBuffer<Efficiency> write=default;
                bool writable=false;
                for(int i=0;i<current.Length;i++)
                    if(!held[i].m_Efficiency.Equals(current[i].m_Efficiency))
                    {
                        if(!writable) { write=em.GetBuffer<Efficiency>(e);writable=true; }
                        write[i]=held[i];
                    }
                baseline[e]=held;
            }
            if(prune)
            {
                var gone=new List<Entity>();foreach(var e in baseline.Keys)if(!em.HasBuffer<Efficiency>(e))gone.Add(e);
                foreach(var e in gone)baseline.Remove(e);
            }
        }
    }
}
