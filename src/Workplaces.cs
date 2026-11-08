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
    internal sealed class RosterGuard
    {
        readonly EntityManager em;
        readonly EntityQuery providers;
        readonly Dictionary<Entity,Dictionary<Entity,Worker>> members = new Dictionary<Entity,Dictionary<Entity,Worker>>();
        int selected;
        public RosterGuard(EntityManager em, EntityQuery providers) { this.em=em; this.providers=providers; }
        public void Clear() { members.Clear(); selected=0; }
        internal static ValueMode Mode(EntityManager em, Entity employer, Settings s)
        {
            bool service=em.HasComponent<PrefabRef>(employer) && em.HasComponent<ServiceObjectData>(em.GetComponentData<PrefabRef>(employer).m_Prefab);
            return service && s.Staff!=ValueMode.Normal ? s.Staff : s.Employees;
        }
        bool Available(Entity e)
        {
            if (!em.HasComponent<Citizen>(e) || em.HasComponent<Deleted>(e)) return false;
            if (em.HasComponent<HealthProblem>(e) && (em.GetComponentData<HealthProblem>(e).m_Flags & HealthProblemFlags.Dead)!=0) return false;
            return em.GetComponentData<Citizen>(e).GetAge()!=CitizenAge.Elderly;
        }
        public void Tick(Settings s,bool prune)
        {
            int code=(int)s.Employees+4*(int)s.Staff;
            if(code==0) {if(selected!=0) Clear(); return;}
            if(code!=selected){members.Clear();providers.ResetFilter();selected=code;}
            else providers.SetChangedVersionFilter(ComponentType.ReadWrite<Employee>());
            using(var list=providers.ToEntityArray(Allocator.Temp)) foreach(var employer in list)
            {
                if(Mode(em,employer,s)==ValueMode.Normal || em.HasComponent<Destroyed>(employer) || em.HasComponent<Abandoned>(employer)) continue;
                if(!members.TryGetValue(employer,out var roster))
                    members.Add(employer,roster=new Dictionary<Entity,Worker>());
                var buffer=em.GetBuffer<Employee>(employer,true);
                var current=new HashSet<Entity>();
                for(int i=0;i<buffer.Length;i++)
                {
                    var citizen=buffer[i].m_Worker;
                    if(em.HasComponent<Worker>(citizen) && Available(citizen))
                    {
                        var job=em.GetComponentData<Worker>(citizen);
                        if(job.m_Workplace==employer){current.Add(citizen);roster[citizen]=job;}
                    }
                }
                var drop=new List<Entity>();
                foreach(var pair in roster)
                {
                    if(current.Contains(pair.Key)) continue;
                    if(!Available(pair.Key) || (em.HasComponent<Worker>(pair.Key) && em.GetComponentData<Worker>(pair.Key).m_Workplace!=employer))
                    {drop.Add(pair.Key);continue;}
                    if(!em.HasComponent<Worker>(pair.Key)) em.AddComponentData(pair.Key,pair.Value);
                    // Structural changes invalidate buffer handles. Reacquire after restoring Worker.
                    var active=em.GetBuffer<Employee>(employer);
                    bool present=false;
                    for(int j=0;j<active.Length;j++) if(active[j].m_Worker==pair.Key){present=true;break;}
                    if(!present) active.Add(new Employee {m_Worker=pair.Key,m_Level=pair.Value.m_Level});
                }
                foreach(var e in drop) roster.Remove(e);
            }
            if(prune)
            {
                var gone=new List<Entity>();
                foreach(var e in members.Keys) if(!em.HasBuffer<Employee>(e)) gone.Add(e);
                foreach(var e in gone) members.Remove(e);
            }
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
        static float Product(Efficiency[] factors)
        {
            float product=1;
            foreach(var factor in factors) product*=Math.Max(0,factor.m_Efficiency);
            return product;
        }
        public void Tick(bool hold,bool prune)
        {
            if(!hold){if(enabled)Clear();return;}
            if(enabled)buildings.SetChangedVersionFilter(ComponentType.ReadWrite<Efficiency>());
            else buildings.ResetFilter();
            enabled=true;
            using(var list=buildings.ToEntityArray(Allocator.Temp)) foreach(var e in list)
            {
                var buffer=em.GetBuffer<Efficiency>(e,true);
                bool outOfService=false;
                foreach(var factor in buffer)
                    if((factor.m_Factor==EfficiencyFactor.Disabled || factor.m_Factor==EfficiencyFactor.Destroyed ||
                        factor.m_Factor==EfficiencyFactor.Fire || factor.m_Factor==EfficiencyFactor.Abandoned) && factor.m_Efficiency<=0)
                        outOfService=true;
                if(outOfService){baseline.Remove(e);continue;}
                float current=1;foreach(var factor in buffer)current*=Math.Max(0,factor.m_Efficiency);
                if(baseline.TryGetValue(e,out var old) && current < Product(old))
                {
                    var write=em.GetBuffer<Efficiency>(e);
                    write.Clear();
                    foreach(var factor in old)write.Add(factor);
                }
                else baseline[e]=buffer.AsNativeArray().ToArray();
            }
            if(prune)
            {
                var gone=new List<Entity>();foreach(var e in baseline.Keys)if(!em.HasBuffer<Efficiency>(e))gone.Add(e);
                foreach(var e in gone)baseline.Remove(e);
            }
        }
    }
}
