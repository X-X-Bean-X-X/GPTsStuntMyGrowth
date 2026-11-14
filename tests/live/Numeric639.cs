using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChicagoBuildBridge;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Prefabs;
using CoverageService = Game.Net.CoverageService;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace ChicagoBuildExtensions
{
    internal static class Numeric639
    {
        internal static object Run(BridgeSystem b, object settings)
        {
            var em=b.EntityManager; var asm=settings.GetType().Assembly;
            if(asm.GetName().Version.ToString()!="1.75.639.0") throw new Exception("Wrong numeric candidate");
            var type=asm.GetType("StuntYourGrowth.ProtectionSystem",true);
            var controller=(ComponentSystemBase)type.GetField("Current",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var props=settings.GetType().GetProperties().Where(p=>p.CanRead && p.CanWrite && p.DeclaringType==settings.GetType()).ToArray();
            var original=props.ToDictionary(p=>p.Name,p=>p.GetValue(settings));
            var entities=new List<Entity>(); var checks=new List<string>();
            Entity New(params ComponentType[] types){var e=em.CreateEntity(types);entities.Add(e);return e;}
            void Set(string name,object value){var p=settings.GetType().GetProperty(name);p.SetValue(settings,p.PropertyType.IsEnum?Enum.Parse(p.PropertyType,(string)value):value);}
            void Check(bool ok,string label){checks.Add((ok ? "PASS: " : "FAIL: ")+label);}
            void Modes(string mode){foreach(var name in new[]{"Customers","Jobs","LandValue","Profit"})Set(name,name=="Jobs" && (mode=="Maximize" || mode=="Zero")?"Normal":mode);}
            try
            {
                foreach(var p in props)if(p.PropertyType==typeof(bool))p.SetValue(settings,false);
                Set("Enabled",true); Modes("Normal"); controller.Update();
                var house=New(typeof(Household));em.SetComponentData(house,new Household{m_Income=17});
                var edge=New(typeof(Game.Net.LandValue));em.SetComponentData(edge,new Game.Net.LandValue{m_LandValue=23});
                var companyPrefab=New(typeof(ServiceCompanyData));em.SetComponentData(companyPrefab,new ServiceCompanyData{m_MaxWorkersPerCell=2});
                var buildingPrefab=New(typeof(BuildingData),typeof(BuildingPropertyData),typeof(SpawnableBuildingData));
                em.SetComponentData(buildingPrefab,new BuildingData{m_LotSize=new int2(2,3)});
                em.SetComponentData(buildingPrefab,new BuildingPropertyData{m_SpaceMultiplier=1});
                em.SetComponentData(buildingPrefab,new SpawnableBuildingData{m_Level=1});
                var building=New(typeof(PrefabRef),typeof(Building));em.SetComponentData(building,new PrefabRef{m_Prefab=buildingPrefab});
                var company=New(typeof(CompanyData),typeof(WorkProvider),typeof(PrefabRef),typeof(PropertyRenter),typeof(CompanyStatisticData));
                em.SetComponentData(company,new PrefabRef{m_Prefab=companyPrefab});em.SetComponentData(company,new PropertyRenter{m_Property=building});
                em.SetComponentData(company,new WorkProvider{m_MaxWorkers=7});
                em.SetComponentData(company,new CompanyStatisticData{m_Income=19,m_Profit=11,m_MonthlyCustomerCount=8,m_CurrentNumberOfCustomers=6,m_MaxNumberOfCustomers=432});
                var servicePrefab=New(typeof(WorkplaceData));em.SetComponentData(servicePrefab,new WorkplaceData{m_MaxWorkers=25});
                var upgradePrefab=New(typeof(WorkplaceData));em.SetComponentData(upgradePrefab,new WorkplaceData{m_MaxWorkers=10});
                var upgrade=New(typeof(PrefabRef));em.SetComponentData(upgrade,new PrefabRef{m_Prefab=upgradePrefab});
                var service=New(typeof(PrefabRef),typeof(WorkProvider),typeof(Building));em.SetComponentData(service,new PrefabRef{m_Prefab=servicePrefab});
                em.AddBuffer<InstalledUpgrade>(service).Add(new InstalledUpgrade{m_Upgrade=upgrade});
                var outside=New(typeof(PrefabRef),typeof(WorkProvider),typeof(Game.Objects.OutsideConnection));
                em.SetComponentData(outside,new PrefabRef{m_Prefab=servicePrefab});em.SetComponentData(outside,new WorkProvider{m_MaxWorkers=600});
                var industryPrefab=New(typeof(IndustrialProcessData));em.SetComponentData(industryPrefab,new IndustrialProcessData{m_MaxWorkersPerCell=3});
                var industry=New(typeof(IndustrialCompany),typeof(CompanyData),typeof(WorkProvider),typeof(PrefabRef),typeof(PropertyRenter));
                em.SetComponentData(industry,new PrefabRef{m_Prefab=industryPrefab});em.SetComponentData(industry,new PropertyRenter{m_Property=building});
                var extractorPrefab=New(typeof(IndustrialProcessData),typeof(ExtractorCompanyData));em.SetComponentData(extractorPrefab,new IndustrialProcessData{m_MaxWorkersPerCell=1});
                var extractor=New(typeof(CompanyData),typeof(WorkProvider),typeof(PrefabRef),typeof(PropertyRenter));
                em.SetComponentData(extractor,new PrefabRef{m_Prefab=extractorPrefab});em.SetComponentData(extractor,new PropertyRenter{m_Property=building});
                var schoolPrefab=New(typeof(WorkplaceData),typeof(SchoolData));
                em.SetComponentData(schoolPrefab,new WorkplaceData{m_MaxWorkers=20,m_MinimumWorkersLimit=5});em.SetComponentData(schoolPrefab,new SchoolData{m_StudentCapacity=200});
                var school=New(typeof(PrefabRef),typeof(WorkProvider),typeof(Building));em.SetComponentData(school,new PrefabRef{m_Prefab=schoolPrefab});
                em.AddBuffer<InstalledUpgrade>(school);em.AddBuffer<Game.Buildings.Student>(school).ResizeUninitialized(100);
                Modes("Zero"); controller.Update();
                Check(em.GetComponentData<Household>(house).m_Income==17,"Household income remains native during Zero overrides");
                Check(em.GetComponentData<WorkProvider>(company).m_MaxWorkers==7 && em.GetComponentData<WorkProvider>(service).m_MaxWorkers==0,"Zero numeric modes do not rewrite jobs");
                Check(em.GetComponentData<WorkProvider>(outside).m_MaxWorkers==600,"Outside cities untouched");
                Check(em.GetComponentData<Game.Net.LandValue>(edge).m_LandValue==0,"Zero road edge");
                var map=b.World.GetExistingSystemManaged<LandValueSystem>().GetMap(true,out var dependency);dependency.Complete();
                Check(map.Length>0 && map[0].m_LandValue==0 && map[map.Length-1].m_LandValue==0,"Zero land map");
                var v=em.GetComponentData<CompanyStatisticData>(company);
                Check(v.m_Income==19 && v.m_Profit==0 && v.m_MonthlyCustomerCount==0 && v.m_CurrentNumberOfCustomers==0,"Zero company statistics");
                Check(v.m_MaxNumberOfCustomers==432,"Historical record preserved");
                v.m_Income=21;v.m_Profit=27;v.m_MonthlyCustomerCount=11;v.m_CurrentNumberOfCustomers=13;em.SetComponentData(company,v);
                controller.Update();v=em.GetComponentData<CompanyStatisticData>(company);
                Check(v.m_Income==21 && v.m_Profit==0 && v.m_CurrentNumberOfCustomers==0,"Zero persists after native writes");
                em.SetComponentData(company,new WorkProvider{m_MaxWorkers=7});
                em.SetComponentData(service,new WorkProvider{m_MaxWorkers=25});
                em.SetComponentData(industry,new WorkProvider{m_MaxWorkers=9});
                em.SetComponentData(extractor,new WorkProvider{m_MaxWorkers=1});
                em.SetComponentData(school,new WorkProvider{m_MaxWorkers=10});
                Set("CustomerTarget",123);Set("LandValueTarget",78);Set("ProfitTarget",321);Modes("Maximize");controller.Update();
                v=em.GetComponentData<CompanyStatisticData>(company);
                Check(v.m_Income==21 && v.m_Profit==321 && v.m_MonthlyCustomerCount==123 && v.m_CurrentNumberOfCustomers==123,"Independent adjustable targets");
                Check(em.GetComponentData<Household>(house).m_Income==17,"Household income remains native during Maximize");
                Check(em.GetComponentData<WorkProvider>(company).m_MaxWorkers==7,"Other Maximize modes leave commercial jobs alone");
                Check(em.GetComponentData<WorkProvider>(service).m_MaxWorkers==25,"Other Maximize modes leave service jobs alone");
                Check(em.GetComponentData<WorkProvider>(industry).m_MaxWorkers==9,"Other Maximize modes leave industrial jobs alone");
                Check(em.GetComponentData<WorkProvider>(extractor).m_MaxWorkers==1,"Native extractor minimum capacity");
                Check(em.GetComponentData<WorkProvider>(school).m_MaxWorkers==10,"Other Maximize modes leave school staffing alone");
                Check(em.GetBuffer<Game.Buildings.Student>(school).Length==100,"No students enrolled or removed");
                Check(em.GetComponentData<Game.Net.LandValue>(edge).m_LandValue==78,"Land edge target");
                map=b.World.GetExistingSystemManaged<LandValueSystem>().GetMap(true,out dependency);dependency.Complete();
                Check(map[0].m_LandValue==78 && map[map.Length-1].m_LandValue==78,"Land map target");
                Set("CustomerTarget",234);controller.Update();Check(em.GetComponentData<CompanyStatisticData>(company).m_CurrentNumberOfCustomers==234,"Target change applies immediately");
                var fresh=New(typeof(CompanyStatisticData));controller.Update();Check(em.GetComponentData<CompanyStatisticData>(fresh).m_Profit==321,"New entity gets target without baseline");
                v=em.GetComponentData<CompanyStatisticData>(company);v.m_Income=29;v.m_Profit=31;em.SetComponentData(company,v);controller.Update();v=em.GetComponentData<CompanyStatisticData>(company);
                Check(v.m_Income==29 && v.m_Profit==321,"Independent Normal and Maximize modes");
                Set("Enabled",false);v.m_Profit=41;em.SetComponentData(company,v);controller.Update();Check(em.GetComponentData<CompanyStatisticData>(company).m_Profit==41,"Master disabled stops writes");
                Set("Enabled",true);controller.Update();Check(em.GetComponentData<CompanyStatisticData>(company).m_Profit==321,"Master reenable applies target");
                Modes("Normal");controller.Update();v=em.GetComponentData<CompanyStatisticData>(company);v.m_Profit=51;em.SetComponentData(company,v);controller.Update();Check(em.GetComponentData<CompanyStatisticData>(company).m_Profit==51,"Normal resumes unmodified statistics");
                Set("CityGrowth","Population");Set("Jobs","Freeze");controller.Update();
                Set("MaxIndustrialOutput",true);Set("MaxCivicOutput",true);controller.Update();
                Check(em.GetComponentData<WorkProvider>(industry).m_MaxWorkers==27,"Industrial native staffing capacity takes priority over Freeze");
                Check(em.GetComponentData<WorkProvider>(service).m_MaxWorkers==35,"Civic native capacity includes upgrade");
                Check(em.GetComponentData<WorkProvider>(school).m_MaxWorkers==10 && em.GetBuffer<Game.Buildings.Student>(school).Length==100,"Civic school target preserves enrollment and native staffing scaling");
                Check(em.GetComponentData<WorkProvider>(outside).m_MaxWorkers==600,"Scoped output leaves outside providers untouched");
                Set("CityGrowth","Freeze");controller.Update();
                Check(!(bool)settings.GetType().GetProperty("MaxIndustrialOutput").GetValue(settings) && !(bool)settings.GetType().GetProperty("MaxCivicOutput").GetValue(settings),"Freeze clears both output controls in live settings");
                Check((bool)settings.GetType().GetMethod("OutputControlsDisabled").Invoke(settings,null),"Freeze disables output selections");
                Set("CityGrowth","Occupancy");
                Check(!(bool)settings.GetType().GetMethod("OutputControlsDisabled").Invoke(settings,null) && !(bool)settings.GetType().GetProperty("MaxCivicOutput").GetValue(settings),"Leaving Freeze enables selection without rechecking");
                Set("Customers","Shopping");
                Check((int)settings.GetType().GetProperty("CustomerControl",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(settings)==0,"Shopping leaves customer statistics native");
                Check(settings.GetType().GetProperty("Income")==null && settings.GetType().GetProperty("Efficiency")==null,"Removed controls absent in loaded assembly");
                em.AddComponent<Game.Simulation.UpdateFrame>(building);
                em.SetSharedComponent(building,new Game.Simulation.UpdateFrame(SimulationUtils.GetUpdateFrame(b.World.GetExistingSystemManaged<SimulationSystem>().frameIndex,RentAdjustSystem.kUpdatesPerDay,16)));
                em.AddBuffer<Renter>(building).Add(new Renter{m_Renter=company});
                em.SetComponentData(company,new PropertyRenter{m_Property=building,m_Rent=123});
                var hooks=asm.GetType("StuntYourGrowth.NativeHooks",true);
                var priceSystem=b.World.GetExistingSystemManaged<RentAdjustSystem>();
                var beforePrice=hooks.GetMethod("BeforePrice",BindingFlags.Static|BindingFlags.NonPublic);
                var afterPrice=hooks.GetMethod("AfterPrice",BindingFlags.Static|BindingFlags.NonPublic);
                Set("LandValue","ZeroFreezeRent");controller.Update();
                object[] priceArgs={priceSystem,null};beforePrice.Invoke(null,priceArgs);
                em.SetComponentData(company,new PropertyRenter{m_Property=building,m_Rent=456});
                afterPrice.Invoke(null,priceArgs);
                Check(em.GetComponentData<PropertyRenter>(company).m_Rent==123 && em.GetComponentData<Game.Net.LandValue>(edge).m_LandValue==0,"Combined Zero and Freeze Rent holds existing lease price");
                Set("LandValue","Zero");priceArgs=new object[]{priceSystem,null};beforePrice.Invoke(null,priceArgs);
                Check(priceArgs[1]==null,"Plain Zero does not freeze rent");
                var road=New(typeof(Game.Net.Edge));
                var cover=em.AddBuffer<Game.Net.ServiceCoverage>(road);
                int slots=Enum.GetValues(typeof(CoverageService)).Cast<CoverageService>().Select(c=>(int)c).Max()+1;
                for(int i=0;i<slots;i++)cover.Add(new Game.Net.ServiceCoverage{m_Coverage=new float2(0.125f)});
                var coverage=b.World.GetExistingSystemManaged(asm.GetType("StuntYourGrowth.CoverageControlSystem",true));
                float police=0,fire=0;
                using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<CoverageData>()))
                using(var defs=q.ToComponentDataArray<CoverageData>(Unity.Collections.Allocator.Temp))
                    foreach(var d in defs)if(math.isfinite(d.m_Magnitude))
                    {if(d.m_Service==CoverageService.Police)police=math.max(police,d.m_Magnitude);if(d.m_Service==CoverageService.FireRescue)fire=math.max(fire,d.m_Magnitude);}
                Check(police>0 && fire>0,"Native police/fire coverage definitions available");
                Set("MaximumPoliceCoverage",true);Set("MaximumFireCoverage",false);Set("MaximumCommunications",false);coverage.Update();
                cover=em.GetBuffer<Game.Net.ServiceCoverage>(road);
                Check(math.all(cover[(int)CoverageService.Police].m_Coverage>=police) && math.all(cover[(int)CoverageService.FireRescue].m_Coverage==0.125f),"Police coverage independent from fire");
                for(int i=0;i<slots;i++)cover[i]=new Game.Net.ServiceCoverage{m_Coverage=new float2(0.125f)};
                Set("MaximumPoliceCoverage",false);Set("MaximumFireCoverage",true);coverage.Update();cover=em.GetBuffer<Game.Net.ServiceCoverage>(road);
                Check(math.all(cover[(int)CoverageService.FireRescue].m_Coverage>=fire) && math.all(cover[(int)CoverageService.Police].m_Coverage==0.125f),"Fire coverage independent from police");
                Set("MaximumFireCoverage",false);for(int i=0;i<slots;i++)cover[i]=new Game.Net.ServiceCoverage{m_Coverage=new float2(0.25f)};
                coverage.Update();cover=em.GetBuffer<Game.Net.ServiceCoverage>(road);
                bool unchanged=true;foreach(var c in cover)unchanged &= math.all(c.m_Coverage==0.25f);Check(unchanged,"Disabled road coverage leaves native values alone");
                Set("MaximumCommunications",true);coverage.Update();
                var telecom=b.World.GetExistingSystemManaged<TelecomCoverageSystem>().GetMap(true,out var telecomPending);telecomPending.Complete();
                Check(telecom.Length>0 && telecom[0].m_SignalStrength==255 && telecom[0].m_NetworkLoad==127 && telecom[telecom.Length-1].m_SignalStrength==255,"Maximum communications covers map endpoints with native full quality");
                Set("MaximumCommunications",false);telecom[0]=default;coverage.Update();
                telecom=b.World.GetExistingSystemManaged<TelecomCoverageSystem>().GetMap(true,out telecomPending);telecomPending.Complete();
                Check(telecom[0].m_SignalStrength==0,"Disabled communications leaves native writes alone");
                return new{passed=checks.All(c=>c.StartsWith("PASS: ")),checks};
            }
            finally
            {
                foreach(var pair in original)settings.GetType().GetProperty(pair.Key).SetValue(settings,pair.Value);
                foreach(var e in entities)if(em.Exists(e))em.DestroyEntity(e);
                controller.Update();
            }
        }
    }
}
