using System;
using Game.Buildings;
using Game.Companies;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace StuntYourGrowth
{
    internal sealed class ResourceStockGuard : IDisposable
    {
        readonly EntityManager manager;
        readonly ResourceSystem resourceSystem;
        readonly EntityQuery companies, services, warehouses;
        readonly long[] targets = new long[EconomyUtils.ResourceCount];
        uint? lastFrame;
        int lastMode;
        ResourcePrefabs resourcePrefabs;

        internal ResourceStockGuard(EntityManager em, ResourceSystem resources)
        {
            manager = em; resourceSystem = resources;
            companies = Query(ComponentType.ReadOnly<CompanyData>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.Exclude<Game.Companies.StorageCompany>());
            services = Query(ComponentType.ReadOnly<CityServiceUpkeep>());
            warehouses = Query(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.Exclude<Game.Objects.OutsideConnection>());
        }
        EntityQuery Query(params ComponentType[] extra)
        {
            var types = new ComponentType[extra.Length + 5];
            types[0] = ComponentType.ReadWrite<Resources>(); types[1] = ComponentType.ReadOnly<PrefabRef>();
            types[2] = ComponentType.Exclude<Temp>(); types[3] = ComponentType.Exclude<Deleted>(); types[4] = ComponentType.Exclude<Destroyed>();
            Array.Copy(extra, 0, types, 5, extra.Length);
            return manager.CreateEntityQuery(types);
        }
        internal void Clear() { lastFrame = null; lastMode = 0; }
        public void Dispose() { companies.Dispose(); services.Dispose(); warehouses.Dispose(); }
        internal void Tick(GPTStuntGrowthSettings settings, uint frame)
        {
            int mode = (settings.MaximumResources ? 1 : 0) | (settings.MaximumStorage ? 2 : 0);
            if (mode == 0) { Clear(); return; }
            if (mode == lastMode && lastFrame.HasValue && unchecked(frame - lastFrame.Value) < 128) return;
            lastMode = mode; lastFrame = frame;
            resourcePrefabs = resourceSystem.GetPrefabs();
            if (settings.MaximumResources) { RefillCompanies(); RefillServices(); }
            if (settings.MaximumStorage) RefillWarehouses();
        }

        internal static bool Stockable(Resource resource) => resource != Resource.NoResource && resource != Resource.Money &&
            (((ulong)resource & ((ulong)resource - 1)) == 0);
        bool Physical(Resource resource)
        {
            if (!Stockable(resource)) return false;
            var prefab = resourcePrefabs[resource];
            return manager.HasComponent<ResourceData>(prefab) && manager.GetComponentData<ResourceData>(prefab).m_Weight > 0f;
        }
        void AddTarget(Resource resource, int weight)
        {
            if (weight > 0 && Physical(resource)) targets[EconomyUtils.GetResourceIndex(resource)] += weight;
        }
        void ResetTargets() => Array.Clear(targets, 0, targets.Length);

        internal static int Share(int capacity, long weight, long total) => capacity <= 0 || weight <= 0 || total <= 0 ? 0 :
            (int)Math.Min(capacity, (decimal)capacity * weight / total);
        internal static int Addition(int current, int target, long free) => (int)Math.Max(0L, Math.Min((long)target - current, Math.Min(int.MaxValue, free)));
        internal static int WarehouseLimit(int perCell, int level, int x, int y)
        {
            if (perCell <= 0 || level <= 0 || x <= 0 || y <= 0) return 0;
            long value = Math.Min(int.MaxValue, (long)perCell * level);
            value = Math.Min(int.MaxValue, value * x);
            return (int)Math.Min(int.MaxValue, value * y);
        }

        void Fill(Entity entity, int capacity, long reservedWeight = 0)
        {
            if (capacity <= 0) return;
            long total = reservedWeight;
            for (int i = 0; i < targets.Length; i++) total += targets[i];
            if (total <= 0) return;
            var buffer = manager.GetBuffer<Resources>(entity);
            long used = 0;
            for (int i = 0; i < buffer.Length; i++) if (Physical(buffer[i].m_Resource)) used += Math.Max(0, buffer[i].m_Amount);
            long free = Math.Max(0L, (long)capacity - used);
            for (int i = 0; i < targets.Length && free > 0; i++)
            {
                if (targets[i] <= 0) continue;
                var resource = EconomyUtils.GetResource(i);
                int add = Addition(EconomyUtils.GetResources(resource, buffer), Share(capacity, targets[i], total), free);
                if (add > 0) { EconomyUtils.AddResources(resource, add, buffer); free -= add; }
            }
        }

        void RefillCompanies()
        {
            using (var entities = companies.ToEntityArray(Allocator.Temp)) foreach (var entity in entities)
            {
                var prefab = manager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!manager.HasComponent<IndustrialProcessData>(prefab) || !manager.HasComponent<StorageLimitData>(prefab)) continue;
                var process = manager.GetComponentData<IndustrialProcessData>(prefab);
                int capacity = manager.GetComponentData<StorageLimitData>(prefab).m_Limit;
                ResetTargets();
                AddTarget(process.m_Input1.m_Resource, process.m_Input1.m_Amount);
                AddTarget(process.m_Input2.m_Resource, process.m_Input2.m_Amount);
                bool commercial = manager.HasComponent<ServiceAvailable>(entity);
                if (commercial && process.m_Output.m_Resource != process.m_Input1.m_Resource && process.m_Output.m_Resource != process.m_Input2.m_Resource)
                    AddTarget(process.m_Output.m_Resource, process.m_Output.m_Amount);
                // Industrial output needs room; do not fill every input slot and block production.
                long reserve = !commercial && Physical(process.m_Output.m_Resource) ? Math.Max(1, process.m_Output.m_Amount) : 0;
                Fill(entity, capacity, reserve);
                RefillVirtualInput(entity, process.m_Input1);
                RefillVirtualInput(entity, process.m_Input2);
            }
        }
        void RefillVirtualInput(Entity entity, ResourceStack input)
        {
            if (input.m_Amount <= 0 || !Stockable(input.m_Resource) || Physical(input.m_Resource)) return;
            var buffer = manager.GetBuffer<Resources>(entity);
            int cap = IndustrialAISystem.kMaxVirtualResourceStorage;
            int add = Addition(EconomyUtils.GetResources(input.m_Resource, buffer), cap, cap);
            if (add > 0) EconomyUtils.AddResources(input.m_Resource, add, buffer);
        }
        void UpkeepTargets(Entity prefab)
        {
            if (!manager.HasBuffer<ServiceUpkeepData>(prefab)) return;
            var upkeep = manager.GetBuffer<ServiceUpkeepData>(prefab, true);
            for (int i = 0; i < upkeep.Length; i++) AddTarget(upkeep[i].m_Upkeep.m_Resource, upkeep[i].m_Upkeep.m_Amount);
        }
        long BaseLimit(Entity prefab) => manager.HasComponent<StorageLimitData>(prefab) ? Math.Max(0, manager.GetComponentData<StorageLimitData>(prefab).m_Limit) : 0;
        void RefillServices()
        {
            using (var entities = services.ToEntityArray(Allocator.Temp)) foreach (var entity in entities)
            {
                var prefab = manager.GetComponentData<PrefabRef>(entity).m_Prefab;
                ResetTargets(); UpkeepTargets(prefab); long capacity = BaseLimit(prefab);
                if (manager.HasBuffer<InstalledUpgrade>(entity))
                {
                    var upgrades = manager.GetBuffer<InstalledUpgrade>(entity, true);
                    for (int i = 0; i < upgrades.Length; i++)
                    {
                        if (BuildingUtils.CheckOption(upgrades[i], BuildingOption.Inactive) || !manager.HasComponent<PrefabRef>(upgrades[i].m_Upgrade)) continue;
                        var upgrade = manager.GetComponentData<PrefabRef>(upgrades[i].m_Upgrade).m_Prefab;
                        UpkeepTargets(upgrade); capacity += BaseLimit(upgrade);
                    }
                }
                if (capacity == 0) for (int i = 0; i < targets.Length; i++) capacity += targets[i];
                Fill(entity, (int)Math.Min(int.MaxValue, capacity));
            }
        }
        void RefillWarehouses()
        {
            using (var entities = warehouses.ToEntityArray(Allocator.Temp)) foreach (var entity in entities)
            {
                var prefab = manager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!manager.HasComponent<StorageCompanyData>(prefab)) continue;
                Resource accepted = manager.GetComponentData<StorageCompanyData>(prefab).m_StoredResources;
                long capacity = BaseLimit(prefab);
                if (manager.HasComponent<PropertyRenter>(entity))
                {
                    var building = manager.GetComponentData<PropertyRenter>(entity).m_Property;
                    if (!manager.HasComponent<PrefabRef>(building)) continue;
                    var buildingPrefab = manager.GetComponentData<PrefabRef>(building).m_Prefab;
                    if (!manager.HasComponent<SpawnableBuildingData>(buildingPrefab) || !manager.HasComponent<BuildingData>(buildingPrefab)) continue;
                    var spawn = manager.GetComponentData<SpawnableBuildingData>(buildingPrefab);
                    var data = manager.GetComponentData<BuildingData>(buildingPrefab);
                    capacity = WarehouseLimit((int)capacity, spawn.m_Level, data.m_LotSize.x, data.m_LotSize.y);
                }
                else if (manager.HasBuffer<InstalledUpgrade>(entity))
                {
                    var upgrades = manager.GetBuffer<InstalledUpgrade>(entity, true);
                    for (int i = 0; i < upgrades.Length; i++)
                    {
                        if (BuildingUtils.CheckOption(upgrades[i], BuildingOption.Inactive) || !manager.HasComponent<PrefabRef>(upgrades[i].m_Upgrade)) continue;
                        var upgrade = manager.GetComponentData<PrefabRef>(upgrades[i].m_Upgrade).m_Prefab;
                        capacity += BaseLimit(upgrade);
                        if (manager.HasComponent<StorageCompanyData>(upgrade)) accepted |= manager.GetComponentData<StorageCompanyData>(upgrade).m_StoredResources;
                    }
                }
                ResetTargets();
                var iterator = ResourceIterator.GetIterator();
                while (iterator.Next()) if ((accepted & iterator.resource) != 0) AddTarget(iterator.resource, 1);
                Fill(entity, (int)Math.Min(int.MaxValue, capacity));
            }
        }
    }
}
