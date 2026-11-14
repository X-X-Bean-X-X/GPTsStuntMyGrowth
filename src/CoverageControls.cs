using System;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using HarmonyLib;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Mathematics;
using ServiceCoverage = Game.Net.ServiceCoverage;

namespace StuntYourGrowth
{
    // Modify coverage results, never enlarge providers' pathfinding searches.
    internal static class CoverageHooks
    {
        internal static void Register(Harmony h)
        {
            h.Patch(AccessTools.DeclaredMethod(typeof(ServiceCoverageSystem), "OnUpdate"),
                postfix: new HarmonyMethod(typeof(CoverageHooks), nameof(AfterRoads)));
            h.Patch(AccessTools.DeclaredMethod(typeof(TelecomCoverageSystem), "OnUpdate"),
                postfix: new HarmonyMethod(typeof(CoverageHooks), nameof(AfterTelecom)));
        }
        static void AfterRoads(ServiceCoverageSystem __instance)
        {
            if (Runtime.Playing) __instance.World.GetExistingSystemManaged<CoverageControlSystem>()?.ApplyRoads();
        }
        static void AfterTelecom(TelecomCoverageSystem __instance)
        {
            if (Runtime.Playing) __instance.World.GetExistingSystemManaged<CoverageControlSystem>()?.ApplyTelecom();
        }
    }

    public sealed class CoverageControlSystem : GameSystemBase
    {
        EntityQuery roads, definitions;
        TelecomCoverageSystem telecom;
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 128;
        protected override void OnCreate()
        {
            base.OnCreate();
            roads=GetEntityQuery(ComponentType.ReadOnly<Edge>(),ComponentType.ReadWrite<ServiceCoverage>(),ComponentType.Exclude<Deleted>(),ComponentType.Exclude<Temp>());
            definitions=GetEntityQuery(ComponentType.ReadOnly<CoverageData>());
            telecom=World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
        }
        protected override void OnUpdate()
        {
            if (!Runtime.Playing) return;
            ApplyRoads();
            ApplyTelecom();
        }
        internal void ApplyRoads()
        {
            var s=Runtime.Options;
            if (s == null || (!s.MaximumPoliceCoverage && !s.MaximumFireCoverage)) return;
            // Strongest finite prefab-defined coverage, without inventing an infinite magnitude.
            float police=0, fire=0;
            using(var values=definitions.ToComponentDataArray<CoverageData>(Allocator.Temp))
                foreach(var value in values)
                {
                    if (!math.isfinite(value.m_Magnitude) || value.m_Magnitude<=0) continue;
                    if (value.m_Service==CoverageService.Police) police=math.max(police,value.m_Magnitude);
                    if (value.m_Service==CoverageService.FireRescue) fire=math.max(fire,value.m_Magnitude);
                }
            if (!s.MaximumPoliceCoverage) police=0;
            if (!s.MaximumFireCoverage) fire=0;
            if (police<=0 && fire<=0) return;
            // The native coverage worker uses unsafe pointers. Join all its buffer work first.
            EntityManager.CompleteDependencyBeforeRW<ServiceCoverage>();
            Dependency=new FillRoadCoverage
            {
                Coverage=GetBufferTypeHandle<ServiceCoverage>(), Police=police, Fire=fire
            }.ScheduleParallel(roads,Dependency);
            // Postfix may run between native systems: finish before any consumer schedules.
            Dependency.Complete();
        }
        internal void ApplyTelecom()
        {
            if (Runtime.Options?.MaximumCommunications != true) return;
            var map=telecom.GetMap(false,out var pending);
            var writer=new FillTelecom{Values=map}.Schedule(JobHandle.CombineDependencies(Dependency,pending));
            telecom.AddWriter(writer);
            Dependency=writer;
        }
        [BurstCompile]
        struct FillRoadCoverage : IJobChunk
        {
            public BufferTypeHandle<ServiceCoverage> Coverage;
            public float Police,Fire;
            public void Execute(in ArchetypeChunk chunk,int index,bool mask,in v128 enabled)
            {
                var buffers=chunk.GetBufferAccessor(ref Coverage);
                for(int i=0;i<chunk.Count;i++)
                {
                    var b=buffers[i];
                    if(Police>0 && b.Length>(int)CoverageService.Police)
                    {
                        var v=b[(int)CoverageService.Police];v.m_Coverage=math.max(v.m_Coverage,new float2(Police));b[(int)CoverageService.Police]=v;
                    }
                    if(Fire>0 && b.Length>(int)CoverageService.FireRescue)
                    {
                        var v=b[(int)CoverageService.FireRescue];v.m_Coverage=math.max(v.m_Coverage,new float2(Fire));b[(int)CoverageService.FireRescue]=v;
                    }
                }
            }
        }
        [BurstCompile]
        struct FillTelecom : IJob
        {
            public NativeArray<TelecomCoverage> Values;
            public void Execute()
            {
                // This pair yields the native maximum sampled quality of 1 (100%).
                var full=new TelecomCoverage{m_SignalStrength=255,m_NetworkLoad=127};
                for(int i=0;i<Values.Length;i++) Values[i]=full;
            }
        }
    }
}
