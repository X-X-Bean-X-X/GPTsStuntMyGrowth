using System;
using System.Collections.Generic;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace StuntYourGrowth
{
    internal static class OccupancyPolicy
    {
        internal const int HouseholdBatch=4, CompanyBatch=2, HouseholdBacklog=64, CompanyBacklog=16;
        internal static int Budget(int vacant,int pending,int batch,int backlog) =>
            vacant<=0 || pending<0 || pending>=backlog ? 0 : (int)Math.Max(0,Math.Min(batch,Math.Min((long)vacant-pending,(long)backlog-pending)));
        internal static bool AllowNativeImmigration(bool stopGrowth,bool maximumOccupancy) => !stopGrowth && !maximumOccupancy;
    }

    // Create real native tenants, then let native initialization and property search place them.
    // Never fabricate renter buffers, population counters, jobs or building capacity.
    public sealed class OccupancyControlSystem : GameSystemBase
    {
        EntityQuery houses, businesses, waitingHomes, waitingBusinesses, connections, demands, properties;
        EndFrameBarrier barrier;
        CountResidentialPropertySystem residentialCount;
        SimulationSystem simulation;
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;
        protected override void OnCreate()
        {
            base.OnCreate();
            houses=GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(),ComponentType.ReadOnly<HouseholdData>(),ComponentType.Exclude<DynamicHousehold>());
            businesses=GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(),ComponentType.ReadOnly<IndustrialProcessData>());
            waitingHomes=GetEntityQuery(ComponentType.ReadOnly<Household>(),ComponentType.Exclude<PropertyRenter>(),ComponentType.Exclude<TouristHousehold>(),ComponentType.Exclude<CommuterHousehold>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>());
            waitingBusinesses=GetEntityQuery(ComponentType.ReadOnly<CompanyData>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<PropertyRenter>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>());
            connections=GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(),ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>());
            demands=GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            properties=GetEntityQuery(ComponentType.ReadOnly<Building>(),ComponentType.ReadOnly<PropertyOnMarket>(),ComponentType.ReadOnly<PrefabRef>(),ComponentType.ReadOnly<Renter>(),ComponentType.Exclude<Abandoned>(),ComponentType.Exclude<Condemned>(),ComponentType.Exclude<Destroyed>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>());
            barrier=World.GetOrCreateSystemManaged<EndFrameBarrier>();
            residentialCount=World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
            simulation=World.GetOrCreateSystemManaged<SimulationSystem>();
        }
        protected override void OnUpdate()
        {
            if (!Runtime.Playing || !Runtime.Options.MaximumOccupancy) return;
            Dependency.Complete();
            var random=new Unity.Mathematics.Random(math.max(1u,simulation.frameIndex));
            var commands=barrier.CreateCommandBuffer();
            AddHouseholds(commands,ref random);
            AddCompanies(commands,ref random);
        }
        void AddHouseholds(EntityCommandBuffer commands,ref Unity.Mathematics.Random random)
        {
            if(demands.CalculateEntityCount()!=1 || houses.IsEmptyIgnoreFilter || connections.IsEmptyIgnoreFilter) return;
            int3 free=residentialCount.FreeProperties;
            int vacant=(int)Math.Min(int.MaxValue,(long)Math.Max(0,free.x)+Math.Max(0,free.y)+Math.Max(0,free.z));
            int budget=OccupancyPolicy.Budget(vacant,waitingHomes.CalculateEntityCount(),OccupancyPolicy.HouseholdBatch,OccupancyPolicy.HouseholdBacklog);
            if(budget==0) return;
            using(var choices=houses.ToEntityArray(Allocator.Temp))
            using(var external=new NativeList<Entity>(Allocator.Temp))
            {
                using(var list=connections.ToEntityArray(Allocator.Temp)) external.AddRange(list);
                var outside=GetComponentLookup<OutsideConnectionData>(true);
                var prefabs=GetComponentLookup<PrefabRef>(true);
                var parameters=demands.GetSingleton<DemandParameterData>();
                // Native weighted household profiles, with adult residents rather than tourists/commuters.
                long total=0;
                foreach(var prefab in choices)
                {
                    var data=EntityManager.GetComponentData<HouseholdData>(prefab);
                    if(data.m_AdultCount>0 && data.m_Weight>0) total+=data.m_Weight;
                }
                if(total<=0 || total>int.MaxValue) return;
                for(int n=0;n<budget;n++)
                {
                    var externalCopy=external;
                    if(!BuildingUtils.GetRandomOutsideConnectionByParameters(ref externalCopy,ref outside,ref prefabs,random,parameters.m_CitizenOCSpawnParameters,out var entry)) break;
                    int pick=random.NextInt((int)total);
                    foreach(var prefab in choices)
                    {
                        var data=EntityManager.GetComponentData<HouseholdData>(prefab);
                        if(data.m_AdultCount<=0 || data.m_Weight<=0) continue;
                        pick-=data.m_Weight;
                        if(pick>=0) continue;
                        var e=commands.CreateEntity(EntityManager.GetComponentData<ArchetypeData>(prefab).m_Archetype);
                        commands.SetComponent(e,new PrefabRef{m_Prefab=prefab});
                        commands.AddComponent(e,new CurrentBuilding{m_CurrentBuilding=entry});
                        break;
                    }
                }
            }
        }
        void AddCompanies(EntityCommandBuffer commands,ref Unity.Mathematics.Random random)
        {
            int pending=waitingBusinesses.CalculateEntityCount();
            if(pending>=OccupancyPolicy.CompanyBacklog || businesses.IsEmptyIgnoreFilter || properties.IsEmptyIgnoreFilter) return;
            var waiting=new HashSet<Entity>();
            using(var current=waitingBusinesses.ToComponentDataArray<PrefabRef>(Allocator.Temp))
                foreach(var p in current) waiting.Add(p.m_Prefab);
            using(var available=properties.ToEntityArray(Allocator.Temp))
            using(var prefabs=businesses.ToEntityArray(Allocator.Temp))
            {
                int vacant=0;
                foreach(var property in available) if(CompanySpace(property,out var ignored)) vacant++;
                int budget=OccupancyPolicy.Budget(vacant,pending,OccupancyPolicy.CompanyBatch,OccupancyPolicy.CompanyBacklog);
                int start=random.NextInt(available.Length);
                for(int offset=0;offset<available.Length && budget>0;offset++)
                {
                    var property=available[(start+offset)%available.Length];
                    if(!CompanySpace(property,out var data)) continue;
                    int first=random.NextInt(prefabs.Length);
                    for(int k=0;k<prefabs.Length;k++)
                    {
                        var prefab=prefabs[(first+k)%prefabs.Length];
                        if(waiting.Contains(prefab) || EntityManager.HasComponent<ExtractorCompanyData>(prefab)) continue;
                        var output=EntityManager.GetComponentData<IndustrialProcessData>(prefab).m_Output.m_Resource;
                        bool commercial=EntityManager.HasComponent<CommercialCompanyData>(prefab);
                        bool storage=EntityManager.HasComponent<StorageCompanyData>(prefab);
                        bool industrial=EntityManager.HasComponent<IndustrialCompanyData>(prefab);
                        var allowed=commercial?data.m_AllowedSold:storage?data.m_AllowedStored:industrial?data.m_AllowedManufactured:Resource.NoResource;
                        if((allowed & output)==Resource.NoResource) continue;
                        var e=commands.CreateEntity(EntityManager.GetComponentData<ArchetypeData>(prefab).m_Archetype);
                        commands.SetComponent(e,new PrefabRef{m_Prefab=prefab});
                        waiting.Add(prefab);budget--;break;
                    }
                }
            }
        }
        bool CompanySpace(Entity property,out BuildingPropertyData data)
        {
            data=default;
            if(EntityManager.HasComponent<ExtractorProperty>(property)) return false;
            var prefab=EntityManager.GetComponentData<PrefabRef>(property).m_Prefab;
            if(!EntityManager.HasComponent<BuildingPropertyData>(prefab)) return false;
            data=EntityManager.GetComponentData<BuildingPropertyData>(prefab);
            if((data.m_AllowedSold|data.m_AllowedManufactured|data.m_AllowedStored)==Resource.NoResource) return false;
            var tenants=EntityManager.GetBuffer<Renter>(property,true);
            foreach(var tenant in tenants)
                if(EntityManager.HasComponent<CompanyData>(tenant.m_Renter) && !EntityManager.HasComponent<Deleted>(tenant.m_Renter)) return false;
            return true;
        }
    }
}
