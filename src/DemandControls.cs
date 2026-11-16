using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.Collections;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace StuntYourGrowth
{
    internal static class DemandPolicy
    {
        internal const int Maximum = 100;
        internal static int Mask(GPTStuntGrowthSettings s, bool externalOwner)
        {
            if(s == null || !s.Enabled || externalOwner) return 0;
            return (s.MaximumLowResidentialDemand?1:0) | (s.MaximumMediumResidentialDemand?2:0) |
                (s.MaximumHighResidentialDemand?4:0) | (s.MaximumCommercialDemand?8:0) |
                (s.MaximumIndustrialDemand?16:0) | (s.MaximumOfficeDemand?32:0);
        }
        internal static int3 Residential(int3 native, int mask) => new int3(
            (mask&1)!=0?Maximum:native.x, (mask&2)!=0?Maximum:native.y, (mask&4)!=0?Maximum:native.z);
        internal static bool IndustrialResource(ResourceData data, int mask) =>
            data.m_IsProduceable && !data.m_IsMaterial && ((mask & (data.m_Weight==0f?32:16))!=0);
    }

    // Demand+ owns the same results. Yield the entire demand feature while its master switch is on.
    // Do not write its settings or infer ownership from a stale .coc file.
    internal static class DemandCompatibility
    {
        static PropertyInfo settings, enabled;
        static bool unsupported;
        internal static void Resolve()
        {
            settings=enabled=null; unsupported=false;
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type=assembly.GetType("DemandPlus.Mod",false);
                if(type==null) continue;
                settings=AccessTools.Property(type,"Settings");
                enabled=settings==null?null:AccessTools.Property(settings.PropertyType,"Enabled");
                unsupported=settings==null || enabled?.PropertyType!=typeof(bool);
                if(unsupported) Runtime.Log.Warn("Demand+ API not recognized; Puppet Master demand overrides will stay inactive.");
                break;
            }
        }
        internal static bool Active
        {
            get
            {
                if(unsupported) return true;
                if(settings==null) return false;
                try { var value=settings.GetValue(null); return value!=null && (bool)enabled.GetValue(value); }
                catch { return true; } // Unknown ownership must not become two competing writers.
            }
        }
    }

    internal static class DemandHooks
    {
        static int ActiveMask => Runtime.Playing ? DemandPolicy.Mask(Runtime.Options,DemandCompatibility.Active) : 0;
        internal static void Register(Harmony h)
        {
            DemandCompatibility.Resolve();
            foreach(var type in new[]{typeof(ResidentialDemandSystem),typeof(CommercialDemandSystem),typeof(IndustrialDemandSystem)})
                h.Patch(AccessTools.DeclaredMethod(type,"OnUpdate"),transpiler:new HarmonyMethod(typeof(DemandHooks),nameof(ModifyDispatch)));
            h.Patch(AccessTools.PropertyGetter(typeof(ResidentialDemandSystem),"buildingDemand"),postfix:new HarmonyMethod(typeof(DemandHooks),nameof(ResidentialDisplay)));
            h.Patch(AccessTools.PropertyGetter(typeof(CommercialDemandSystem),"buildingDemand"),postfix:new HarmonyMethod(typeof(DemandHooks),nameof(CommercialDisplay)));
            h.Patch(AccessTools.PropertyGetter(typeof(IndustrialDemandSystem),"industrialBuildingDemand"),postfix:new HarmonyMethod(typeof(DemandHooks),nameof(IndustrialDisplay)));
            h.Patch(AccessTools.PropertyGetter(typeof(IndustrialDemandSystem),"officeBuildingDemand"),postfix:new HarmonyMethod(typeof(DemandHooks),nameof(OfficeDisplay)));
        }
        static void ResidentialDisplay(ref int3 __result) => __result=DemandPolicy.Residential(__result,ActiveMask);
        static void CommercialDisplay(ref int __result) { if((ActiveMask&8)!=0) __result=DemandPolicy.Maximum; }
        static void IndustrialDisplay(ref int __result) { if((ActiveMask&16)!=0) __result=DemandPolicy.Maximum; }
        static void OfficeDisplay(ref int __result) { if((ActiveMask&32)!=0) __result=DemandPolicy.Maximum; }

        internal static IEnumerable<CodeInstruction> ModifyDispatch(IEnumerable<CodeInstruction> input)
        {
            int edits=0;
            foreach(var instruction in input)
            {
                var method=instruction.operand as MethodInfo;
                var type=method?.IsGenericMethod==true?method.GetGenericArguments()[0]:null;
                if(method?.Name=="Schedule" && type!=null && (type.Name=="UpdateResidentialDemandJob" || type.Name=="UpdateCommercialDemandJob" || type.Name=="UpdateIndustrialDemandJob"))
                {
                    Validate(type);
                    instruction.operand=AccessTools.Method(typeof(DemandHooks),nameof(Schedule)).MakeGenericMethod(type);
                    edits++;
                }
                yield return instruction;
            }
            if(edits!=1) throw new InvalidOperationException("Unsupported demand scheduler: "+edits);
        }
        static void Require(Type t,string name,Type expected)
        {
            if(AccessTools.Field(t,name)?.FieldType!=expected) throw new InvalidOperationException("Unsupported demand field: "+t.Name+"."+name);
        }
        static void Validate(Type t)
        {
            if(t.Name=="UpdateResidentialDemandJob") Require(t,"m_BuildingDemand",typeof(NativeValue<int3>));
            else
            {
                bool commercial=t.Name=="UpdateCommercialDemandJob";
                Require(t,commercial?"m_BuildingDemand":"m_IndustrialBuildingDemand",typeof(NativeValue<int>));
                Require(t,commercial?"m_BuildingDemands":"m_IndustrialBuildingDemands",typeof(NativeArray<int>));
                Require(t,"m_ResourcePrefabs",typeof(ResourcePrefabs));
                Require(t,"m_ResourceDatas",typeof(ComponentLookup<ResourceData>));
                if(!commercial) Require(t,"m_OfficeBuildingDemand",typeof(NativeValue<int>));
            }
        }
        static class Fields<T>
        {
            internal static readonly bool Residential=typeof(T).Name=="UpdateResidentialDemandJob", Commercial=typeof(T).Name=="UpdateCommercialDemandJob";
            internal static readonly FieldInfo Demand=AccessTools.Field(typeof(T),Residential||Commercial?"m_BuildingDemand":"m_IndustrialBuildingDemand");
            internal static readonly FieldInfo Office=AccessTools.Field(typeof(T),"m_OfficeBuildingDemand");
            internal static readonly FieldInfo Values=AccessTools.Field(typeof(T),Commercial?"m_BuildingDemands":"m_IndustrialBuildingDemands");
            internal static readonly FieldInfo Prefabs=AccessTools.Field(typeof(T),"m_ResourcePrefabs"), Data=AccessTools.Field(typeof(T),"m_ResourceDatas");
        }
        static JobHandle Schedule<T>(T job,JobHandle dependency) where T:struct,IJob
        {
            int mask=ActiveMask;
            int selected=mask & (Fields<T>.Residential?7:Fields<T>.Commercial?8:48);
            if(selected==0) return job.Schedule(dependency);
            object copy=job;
            // Append a small writer to the native chain. The returned dependency is used by
            // the native system and all of its readers; no global job completion or entity scan.
            if(Fields<T>.Residential)
                return new ResidentialOverride{Values=(NativeValue<int3>)Fields<T>.Demand.GetValue(copy),Mask=selected}.Schedule(job.Schedule(dependency));
            var values=(NativeArray<int>)Fields<T>.Values.GetValue(copy);
            var prefabs=(ResourcePrefabs)Fields<T>.Prefabs.GetValue(copy);
            var data=(ComponentLookup<ResourceData>)Fields<T>.Data.GetValue(copy);
            var demand=(NativeValue<int>)Fields<T>.Demand.GetValue(copy);
            if(Fields<T>.Commercial)
                return new CommercialOverride{Demand=demand,Values=values,Prefabs=prefabs,Data=data}.Schedule(job.Schedule(dependency));
            return new IndustrialOverride{Industry=demand,Office=(NativeValue<int>)Fields<T>.Office.GetValue(copy),Values=values,Prefabs=prefabs,Data=data,Mask=selected}.Schedule(job.Schedule(dependency));
        }
        struct ResidentialOverride:IJob
        {
            public NativeValue<int3> Values; public int Mask;
            public void Execute() { Values.value=DemandPolicy.Residential(Values.value,Mask); }
        }
        struct CommercialOverride:IJob
        {
            public NativeValue<int> Demand; public NativeArray<int> Values;
            [ReadOnly] public ResourcePrefabs Prefabs;
            [ReadOnly] public ComponentLookup<ResourceData> Data;
            public void Execute()
            {
                Demand.value=DemandPolicy.Maximum;
                var iterator=ResourceIterator.GetIterator();
                while(iterator.Next())
                {
                    int i=EconomyUtils.GetResourceIndex(iterator.resource);
                    if(i>=0 && i<Values.Length && EconomyUtils.IsCommercialResource(iterator.resource) && Data.HasComponent(Prefabs[iterator.resource]))
                        Values[i]=DemandPolicy.Maximum;
                }
            }
        }
        struct IndustrialOverride:IJob
        {
            public NativeValue<int> Industry,Office; public NativeArray<int> Values; public int Mask;
            [ReadOnly] public ResourcePrefabs Prefabs;
            [ReadOnly] public ComponentLookup<ResourceData> Data;
            public void Execute()
            {
                if((Mask&16)!=0) Industry.value=DemandPolicy.Maximum;
                if((Mask&32)!=0) Office.value=DemandPolicy.Maximum;
                var iterator=ResourceIterator.GetIterator();
                while(iterator.Next())
                {
                    int i=EconomyUtils.GetResourceIndex(iterator.resource);
                    if(i>=0 && i<Values.Length && Data.TryGetComponent(Prefabs[iterator.resource],out var data) && DemandPolicy.IndustrialResource(data,Mask))
                        Values[i]=DemandPolicy.Maximum;
                }
            }
        }
    }
}
