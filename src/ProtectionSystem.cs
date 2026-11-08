using System;
using System.Collections.Generic;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace StuntYourGrowth
{
    internal delegate void ProtectValue<T>(ref T current, T previous, Settings settings);
    internal interface ITrackedFields { void Tick(Settings s, bool prune); void Clear(); }

    // Baselines use the full Entity, including its generation. Only changed chunks are revisited.
    internal sealed class TrackedFields<T> : ITrackedFields where T : unmanaged, IComponentData
    {
        readonly EntityManager manager;
        readonly EntityQuery query;
        readonly Func<Settings,int> selection;
        readonly ProtectValue<T> protect;
        readonly Dictionary<Entity,T> previous = new Dictionary<Entity,T>();
        int mode;
        public TrackedFields(EntityManager manager, EntityQuery query, Func<Settings,int> selection, ProtectValue<T> protect)
        {
            this.manager = manager;
            this.query = query;
            this.selection = selection;
            this.protect = protect;
        }
        public void Clear() { mode = 0; previous.Clear(); }
        public void Tick(Settings s, bool prune)
        {
            int selected = selection(s);
            if (selected == 0) { if (mode != 0) Clear(); return; }
            bool first = mode != selected;
            mode = selected;
            if (first) { previous.Clear(); query.ResetFilter(); }
            else query.SetChangedVersionFilter(ComponentType.ReadWrite<T>());
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var e in entities)
                {
                    T raw = manager.GetComponentData<T>(e), value = raw;
                    if (previous.TryGetValue(e,out T baseline))
                    {
                        protect(ref value, baseline, s);
                        if (!EqualityComparer<T>.Default.Equals(raw,value)) manager.SetComponentData(e,value);
                    }
                    previous[e] = value;
                }
            }
            if (prune)
            {
                var expired = new List<Entity>();
                foreach (var e in previous.Keys) if (!manager.Exists(e) || !manager.HasComponent<T>(e)) expired.Add(e);
                foreach (var e in expired) previous.Remove(e);
            }
        }
    }

    public sealed class ProtectionSystem : GameSystemBase
    {
        internal static ProtectionSystem Current;
        readonly List<ITrackedFields> fields = new List<ITrackedFields>();
        SimulationSystem sim;
        EntityQuery time, citizens, abandoned, conditions, garbage, rentalBuildings, departingResidents, departingTenants;
        RosterGuard roster;
        EfficiencyGuard efficiency;
        int? lastAgeDay;
        bool resetAll, resetVacant, wasActive;
        uint? lastClock;
        protected override void OnCreate()
        {
            base.OnCreate();
            sim = World.GetOrCreateSystemManaged<SimulationSystem>();
            time = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            citizens = Query<Citizen>();
            conditions = Query<BuildingCondition>();
            abandoned = Query<Abandoned>();
            garbage = FrameQuery<GarbageProducer>();
            rentalBuildings = FrameQuery<Renter>();
            Track<Citizen>(s => (s.Happiness?4:0) | (s.Health?2:0) | (s.Wellbeing?1:0), ProtectCitizen);
            Track<AttractivenessProvider>(s => s.Attractiveness?1:0, (ref AttractivenessProvider v, AttractivenessProvider p, Settings s) => v.m_Attractiveness = Math.Max(v.m_Attractiveness,p.m_Attractiveness));
            Track<TransportStop>(s => s.Comfort?1:0, (ref TransportStop v, TransportStop p, Settings s) => v.m_ComfortFactor = Math.Max(v.m_ComfortFactor,p.m_ComfortFactor));
            Track<TransportStation>(s => s.Comfort?1:0, (ref TransportStation v, TransportStation p, Settings s) => v.m_ComfortFactor = Math.Max(v.m_ComfortFactor,p.m_ComfortFactor));
            Track<ParkingFacility>(s => s.Comfort?1:0, (ref ParkingFacility v, ParkingFacility p, Settings s) => v.m_ComfortFactor = Math.Max(v.m_ComfortFactor,p.m_ComfortFactor));
            Track<Game.Net.LandValue>(s => (int)s.LandValue, (ref Game.Net.LandValue v, Game.Net.LandValue p, Settings s) => v.m_LandValue = Policy.Protect(p.m_LandValue,v.m_LandValue,s.LandValue));
            Track<WorkProvider>(s => (int)s.Jobs, (ref WorkProvider v, WorkProvider p, Settings s) => v.m_MaxWorkers = Policy.Protect(p.m_MaxWorkers,v.m_MaxWorkers,s.Jobs));
            Track<Household>(s => (int)s.Income, (ref Household v, Household p, Settings s) => v.m_Income = Policy.Protect(p.m_Income,v.m_Income,s.Income));
            Track<CompanyStatisticData>(s => (int)s.Customers + 4*(int)s.Income + 16*(int)s.Profit, ProtectCompany);
            Track<Game.Buildings.Park>(s => s.ParkMaintenance ? 1 : 0, ProtectPark);
            var carLanes = GetEntityQuery(ComponentType.ReadWrite<Game.Net.LaneCondition>(), ComponentType.ReadOnly<Game.Net.CarLane>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            fields.Add(new TrackedFields<Game.Net.LaneCondition>(EntityManager, carLanes, s => s.RoadWear ? 1 : 0, ProtectLane));
            var roads = GetEntityQuery(ComponentType.ReadWrite<Game.Net.NetCondition>(), ComponentType.ReadOnly<Game.Net.Road>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            fields.Add(new TrackedFields<Game.Net.NetCondition>(EntityManager, roads, s => s.RoadWear ? 1 : 0, ProtectRoad));
            roster = new RosterGuard(EntityManager, QueryBuffer<Employee>());
            efficiency = new EfficiencyGuard(EntityManager, QueryBuffer<Efficiency>());
            departingResidents = GetEntityQuery(ComponentType.ReadOnly<Game.Agents.MovingAway>(),ComponentType.ReadOnly<Household>(),ComponentType.Exclude<TouristHousehold>(),ComponentType.Exclude<CommuterHousehold>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Deleted>());
            departingTenants = GetEntityQuery(ComponentType.ReadOnly<Game.Agents.MovingAway>(),ComponentType.ReadOnly<PropertyRenter>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Deleted>());
            Current = this;
        }
        EntityQuery Query<T>() where T : unmanaged,IComponentData => GetEntityQuery(ComponentType.ReadWrite<T>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Deleted>());
        EntityQuery QueryBuffer<T>() where T : unmanaged,IBufferElementData => GetEntityQuery(ComponentType.ReadWrite<T>(),ComponentType.Exclude<Temp>(),ComponentType.Exclude<Deleted>());
        EntityQuery FrameQuery<T>() => GetEntityQuery(ComponentType.ReadOnly<T>(),ComponentType.ReadOnly<UpdateFrame>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>());
        void Track<T>(Func<Settings,int> selection, ProtectValue<T> protect) where T : unmanaged,IComponentData =>
            fields.Add(new TrackedFields<T>(EntityManager, Query<T>(), selection, protect));

        static void ProtectPark(ref Game.Buildings.Park current, Game.Buildings.Park previous, Settings settings) =>
            current.m_Maintenance = Math.Max(current.m_Maintenance, previous.m_Maintenance);

        static void ProtectLane(ref Game.Net.LaneCondition current, Game.Net.LaneCondition previous, Settings settings) =>
            current.m_Wear = Math.Min(current.m_Wear, previous.m_Wear);

        static void ProtectRoad(ref Game.Net.NetCondition current, Game.Net.NetCondition previous, Settings settings) =>
            current.m_Wear = Unity.Mathematics.math.min(current.m_Wear, previous.m_Wear);

        static void ProtectCitizen(ref Citizen v, Citizen p, Settings s)
        {
            if ((v.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0) return;
            if (s.Health) v.m_Health = Math.Max(p.m_Health,v.m_Health);
            if (s.Wellbeing) v.m_WellBeing = Math.Max(p.m_WellBeing,v.m_WellBeing);
            if (s.Happiness) Policy.ProtectHappiness(p.Happiness,ref v.m_Health,ref v.m_WellBeing);
        }
        static void ProtectCompany(ref CompanyStatisticData v, CompanyStatisticData p, Settings s)
        {
            v.m_Income = Policy.Protect(p.m_Income, v.m_Income, s.Income);
            v.m_Profit = Policy.Protect(p.m_Profit, v.m_Profit, s.Profit);
            if (s.Customers != ValueMode.Normal)
            {
                var clock = Current.World.GetExistingSystemManaged<TimeSystem>();
                float t = clock.normalizedTime;
                int reported(CompanyStatisticData data) => (int)Math.Round(data.m_MonthlyCustomerCount*(1.0-t)+data.m_CurrentNumberOfCustomers*t);
                int held = Policy.Protect(reported(p),reported(v),s.Customers);
                // The displayed count interpolates these counters; keep the protected result time-independent.
                v.m_MonthlyCustomerCount = held;
                v.m_CurrentNumberOfCustomers = held;
            }
        }
        protected override void OnUpdate()
        {
            if (!Runtime.Playing) { if (wasActive) ClearState(); return; }
            wasActive = true;
            var s = Runtime.Options;
            bool prune = (sim.frameIndex & 4095u) == 0;
            foreach (var group in fields) group.Tick(s,prune);
            if (s.Residents && !departingResidents.IsEmptyIgnoreFilter)
                EntityManager.RemoveComponent<Game.Agents.MovingAway>(departingResidents);
            if (s.Tenants && !departingTenants.IsEmptyIgnoreFilter)
                EntityManager.RemoveComponent<Game.Agents.MovingAway>(departingTenants);
            roster.Tick(s,prune);
            efficiency.Tick(s.Efficiency,prune);
            UpdateBirthdays(s);
            UpdateClocks(s);
        }
        void UpdateBirthdays(Settings s)
        {
            if (!s.Age || time.IsEmptyIgnoreFilter) { lastAgeDay = null; return; }
            int day = TimeSystem.GetDay(sim.frameIndex,time.GetSingleton<TimeData>());
            int delta = day - (lastAgeDay ?? day);
            lastAgeDay = day;
            if (delta <= 0) return;
            // Citizen shares its system query with health protection; reset for the once-daily pass.
            citizens.ResetFilter();
            using (var list=citizens.ToEntityArray(Allocator.Temp)) foreach (var e in list)
            {
                Citizen v = EntityManager.GetComponentData<Citizen>(e);
                v.m_BirthDay = (short)Math.Min(short.MaxValue, v.m_BirthDay + delta);
                EntityManager.SetComponentData(e,v);
            }
        }
        internal void QueueReset(bool emptyOnly) { if (emptyOnly) resetVacant = true; else resetAll = true; }
        internal static bool Empty(EntityManager em, Entity building)
        {
            if (!em.HasBuffer<Renter>(building)) return false;
            var tenants = em.GetBuffer<Renter>(building,true);
            for(int i=0;i<tenants.Length;i++)
            {
                Entity e = tenants[i].m_Renter;
                if (em.HasComponent<PropertyRenter>(e) && !em.HasComponent<Deleted>(e) &&
                    em.GetComponentData<PropertyRenter>(e).m_Property == building) return false;
            }
            return true;
        }
        void UpdateClocks(Settings s)
        {
            uint now=sim.frameIndex;
            if (!(s.Abandonment || s.Vacancy || resetAll || resetVacant)) { lastClock=null; return; }
            uint prior = lastClock ?? now;
            lastClock = s.Abandonment || s.Vacancy ? now : (uint?)null;
            using (var list=abandoned.ToEntityArray(Allocator.Temp)) foreach (var e in list)
            {
                var a = EntityManager.GetComponentData<Abandoned>(e);
                bool empty = (s.Vacancy || resetVacant) && Empty(EntityManager,e);
                uint start = a.m_AbandonmentTime;
                if (resetAll || (resetVacant && empty)) a.m_AbandonmentTime = now;
                else if (s.Abandonment || (s.Vacancy && empty))
                    a.m_AbandonmentTime = start + Math.Min(now - start, now - prior);
                if (a.m_AbandonmentTime != start) EntityManager.SetComponentData(e,a);
            }
            if (resetAll || resetVacant)
                using (var list=conditions.ToEntityArray(Allocator.Temp)) foreach (var e in list)
                {
                    var c=EntityManager.GetComponentData<BuildingCondition>(e);
                    if (c.m_Condition < 0 && (resetAll || Empty(EntityManager,e)))
                    { c.m_Condition=0; EntityManager.SetComponentData(e,c); }
                }
            resetAll = resetVacant = false;
        }
        void ClearState()
        {
            foreach (var f in fields) f.Clear();
            roster?.Clear(); efficiency?.Clear();
            lastClock=null; lastAgeDay=null; resetAll=resetVacant=wasActive=false;
        }
        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        { ClearState(); base.OnGamePreload(purpose,mode); }
        protected override void OnDestroy()
        { ClearState(); if (Current==this) Current=null; base.OnDestroy(); }

        static ProtectionSystem Owner(GameSystemBase system) => system.World.GetExistingSystemManaged<ProtectionSystem>();
        static void SetFrame(EntityQuery q, GameSystemBase system, int updates)
        {
            uint frame=system.World.GetExistingSystemManaged<SimulationSystem>().frameIndex;
            q.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrame(frame,updates,16)));
        }
        internal static Dictionary<Entity,int> Leases(GameSystemBase system,bool charge)
        {
            var q=Owner(system).rentalBuildings;
            SetFrame(q,system,charge ? PropertyRenterSystem.kUpdatesPerDay : RentAdjustSystem.kUpdatesPerDay);
            var values=new Dictionary<Entity,int>();
            var em=system.EntityManager;
            using(var list=q.ToEntityArray(Allocator.Temp)) foreach(var e in list)
            {
                var renters=em.GetBuffer<Renter>(e,true);
                for(int i=0;i<renters.Length;i++)
                {
                    var t=renters[i].m_Renter;
                    if (!em.HasComponent<PropertyRenter>(t)) continue;
                    if (!charge) values[t]=em.GetComponentData<PropertyRenter>(t).m_Rent;
                    else if(em.HasBuffer<Resources>(t)) values[t]=EconomyUtils.GetResources(Resource.Money,em.GetBuffer<Resources>(t,true));
                }
            }
            return values;
        }
        internal static void RestoreLeases(GameSystemBase system,Dictionary<Entity,int> values,bool charge)
        {
            if (values==null) return;
            system.World.GetExistingSystemManaged<EndFrameBarrier>().producerHandle.Complete();
            var em=system.EntityManager;
            foreach(var pair in values)
            {
                if(charge)
                {
                    if (!em.HasBuffer<Resources>(pair.Key)) continue;
                    var buffer=em.GetBuffer<Resources>(pair.Key);
                    if (EconomyUtils.GetResources(Resource.Money,buffer)!=pair.Value)
                        EconomyUtils.SetResources(Resource.Money,buffer,pair.Value);
                }
                else if(em.HasComponent<PropertyRenter>(pair.Key))
                {
                    var rent=em.GetComponentData<PropertyRenter>(pair.Key);
                    if(rent.m_Rent!=pair.Value){rent.m_Rent=pair.Value; em.SetComponentData(pair.Key,rent);}
                }
            }
        }
        internal static Dictionary<Entity,int> GarbageBefore(GameSystemBase system)
        {
            var q=Owner(system).garbage;
            SetFrame(q,system,GarbageAccumulationSystem.kUpdatesPerDay);
            var values=new Dictionary<Entity,int>();
            using(var list=q.ToEntityArray(Allocator.Temp)) foreach(var e in list)
                values[e]=system.EntityManager.GetComponentData<GarbageProducer>(e).m_Garbage;
            return values;
        }
        internal static void RestoreGarbage(GameSystemBase system,Dictionary<Entity,int> values)
        {
            if(values==null) return;
            system.World.GetExistingSystemManaged<EndFrameBarrier>().producerHandle.Complete();
            foreach(var pair in values)
            {
                var g=system.EntityManager.GetComponentData<GarbageProducer>(pair.Key);
                if (g.m_Garbage>pair.Value) {g.m_Garbage=pair.Value; system.EntityManager.SetComponentData(pair.Key,g);}
            }
        }
    }
}
