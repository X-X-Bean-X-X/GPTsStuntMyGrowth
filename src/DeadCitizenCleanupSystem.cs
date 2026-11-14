using Game;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace StuntYourGrowth
{
    // Mark eligible corpses for the same native Deleted cleanup used by deathcare
    // facilities. Native systems retain household, occupant, school, creature and
    // seeker cleanup. Never DestroyEntity a citizen or fabricate a death event.
    public sealed class DeadCitizenCleanupSystem : GameSystemBase
    {
        internal const int BatchLimit = 64;
        internal const int ScanLimit = 2048;
        EntityQuery corpses;
        int cursor;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;

        protected override void OnCreate()
        {
            base.OnCreate();
            corpses = GetEntityQuery(ComponentType.ReadOnly<Citizen>(),
                ComponentType.ReadOnly<HouseholdMember>(), ComponentType.ReadWrite<HealthProblem>(),
                ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        }

        internal static bool Eligible(HealthProblemFlags flags, bool activePickup, bool inVehicle,
            bool inDeathcare) =>
            (flags & (HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport)) ==
                (HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport) &&
            !activePickup && !inVehicle && !inDeathcare;

        internal static HealthProblem RetireTransport(HealthProblem health)
        {
            // Invalidate an unassigned request before native dispatch revisits it.
            // Keep Dead and unrelated health flags; never turn death into recovery.
            health.m_Flags &= ~HealthProblemFlags.RequireTransport;
            health.m_HealthcareRequest = Entity.Null;
            return health;
        }

        bool ActivePickup(Entity citizen, HealthProblem health)
        {
            var request = health.m_HealthcareRequest;
            if (!EntityManager.HasComponent<HealthcareRequest>(request) ||
                !EntityManager.HasComponent<Dispatched>(request)) return false;
            var data = EntityManager.GetComponentData<HealthcareRequest>(request);
            var handler = EntityManager.GetComponentData<Dispatched>(request).m_Handler;
            return data.m_Citizen == citizen && EntityManager.Exists(handler) &&
                !EntityManager.HasComponent<Deleted>(handler);
        }

        bool InVehicle(Entity citizen)
        {
            if (!EntityManager.HasComponent<CurrentTransport>(citizen)) return false;
            var transport = EntityManager.GetComponentData<CurrentTransport>(citizen).m_CurrentTransport;
            if (EntityManager.HasComponent<Game.Vehicles.Vehicle>(transport)) return true;
            if (!EntityManager.HasComponent<Game.Creatures.CurrentVehicle>(transport)) return false;
            var vehicle = EntityManager.GetComponentData<Game.Creatures.CurrentVehicle>(transport).m_Vehicle;
            return vehicle != Entity.Null && EntityManager.Exists(vehicle);
        }

        bool InDeathcare(Entity citizen)
        {
            if (EntityManager.HasComponent<TravelPurpose>(citizen))
            {
                var purpose = EntityManager.GetComponentData<TravelPurpose>(citizen).m_Purpose;
                if (purpose == Purpose.Deathcare || purpose == Purpose.InDeathcare) return true;
            }
            if (!EntityManager.HasComponent<CurrentBuilding>(citizen)) return false;
            var building = EntityManager.GetComponentData<CurrentBuilding>(citizen).m_CurrentBuilding;
            return EntityManager.HasComponent<Game.Buildings.DeathcareFacility>(building);
        }

        protected override void OnUpdate()
        {
            if (!Runtime.Playing || !Runtime.Options.RemoveDead) { cursor = 0; return; }
            if (corpses.IsEmptyIgnoreFilter) { cursor = 0; return; }
            Dependency.Complete();
            // Finish producers before a structural-change playback; do not play any
            // other system's command buffer or change the simulation's speed.
            World.GetExistingSystemManaged<EndFrameBarrier>().producerHandle.Complete();
            using (var citizens = corpses.ToEntityArray(Allocator.Temp))
            using (var changes = new EntityCommandBuffer(Allocator.Temp))
            {
                int count = citizens.Length;
                if (count == 0) { cursor = 0; return; }
                cursor %= count;
                int scanned = 0, removed = 0;
                while (scanned < count && scanned < ScanLimit && removed < BatchLimit)
                {
                    var citizen = citizens[(cursor + scanned) % count];
                    scanned++;
                    var health = EntityManager.GetComponentData<HealthProblem>(citizen);
                    // Cheap flag filter before consulting vehicle/request references.
                    if (!Eligible(health.m_Flags, false, false, false)) continue;
                    // Native HouseholdAndCitizenRemoveSystem assumes this enableable
                    // component exists. Skip malformed/custom citizens that lack it;
                    // disabled job seekers are valid and must not be filtered out.
                    if (!EntityManager.HasComponent<HasJobSeeker>(citizen)) continue;
                    if (!Eligible(health.m_Flags, ActivePickup(citizen, health), InVehicle(citizen), InDeathcare(citizen))) continue;
                    changes.SetComponent(citizen, RetireTransport(health));
                    changes.AddComponent<Deleted>(citizen);
                    removed++;
                }
                cursor = (cursor + scanned) % count;
                if (removed != 0) changes.Playback(EntityManager);
            }
        }

        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            cursor = 0;
            base.OnGamePreload(purpose, mode);
        }
    }
}
