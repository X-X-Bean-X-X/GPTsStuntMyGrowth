using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Colossal.Json;
using Game.Buildings;
using Game.Simulation;
using Game.Pathfind;
using Game.Prefabs;
using Game.Economy;
using Game.Companies;
using HarmonyLib;
using StuntYourGrowth;

class Offline633
{
    static int checks;
    static BindingFlags hidden = BindingFlags.Static | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static GPTStuntGrowthSettings NewSettings() { var s=(GPTStuntGrowthSettings)FormatterServices.GetUninitializedObject(typeof(GPTStuntGrowthSettings));s.SetDefaults();return s; }
    static void SetAll(GPTStuntGrowthSettings s,bool enabled) { typeof(GPTStuntGrowthSettings).GetMethod("SetAllOptions",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,new object[]{enabled}); }
    static IEnumerable<PropertyInfo> Controls => typeof(GPTStuntGrowthSettings).GetProperties().Where(p=>p.DeclaringType==typeof(GPTStuntGrowthSettings) && p.CustomAttributes.Any(a=>a.AttributeType.Name=="SettingsUISectionAttribute"));
    static List<CodeInstruction> ReadIL(MethodInfo method)
    {
        // Decode without activating Harmony's Mono-specific detour runtime in this .NET host.
        var opcodes=typeof(System.Reflection.Emit.OpCodes).GetFields().Where(f=>f.FieldType==typeof(System.Reflection.Emit.OpCode)).Select(f=>(System.Reflection.Emit.OpCode)f.GetValue(null)).ToDictionary(o=>(ushort)o.Value);
        var bytes=method.GetMethodBody().GetILAsByteArray();var result=new List<CodeInstruction>();
        for(int offset=0;offset<bytes.Length;)
        {
            ushort key=bytes[offset++];if(key==0xfe)key=(ushort)(0xfe00|bytes[offset++]);
            var op=opcodes[key];object operand=null;int length=0;
            switch(op.OperandType)
            {
                case System.Reflection.Emit.OperandType.InlineMethod: operand=method.Module.ResolveMethod(BitConverter.ToInt32(bytes,offset));length=4;break;
                case System.Reflection.Emit.OperandType.InlineNone:break;
                case System.Reflection.Emit.OperandType.ShortInlineBrTarget:
                case System.Reflection.Emit.OperandType.ShortInlineI:
                case System.Reflection.Emit.OperandType.ShortInlineVar:length=1;break;
                case System.Reflection.Emit.OperandType.InlineVar:length=2;break;
                case System.Reflection.Emit.OperandType.InlineI8:
                case System.Reflection.Emit.OperandType.InlineR:length=8;break;
                case System.Reflection.Emit.OperandType.InlineSwitch:length=4+4*BitConverter.ToInt32(bytes,offset);break;
                default:length=4;break;
            }
            result.Add(new CodeInstruction(op,operand));offset+=length;
        }
        return result;
    }
    public static void Run()
    {
        var asm=typeof(GPTStuntGrowthSettings).Assembly;
        Check(typeof(GPTStuntGrowthSettings).Name=="GPTStuntGrowthSettings","Unique native settings save lookup key");
        Check(asm.GetName().Version.ToString()=="1.75.639.0","Assembly version");
        foreach(string removed in new[]{"Employees","MaximumCustomers","HireAllAvailableJobs","Lifespan","Abandonment","Staff"})
            Check(typeof(GPTStuntGrowthSettings).GetProperty(removed)==null,"Retired setting absent: "+removed);
        Check(asm.GetType("StuntYourGrowth.CustomerHooks")==null && asm.GetType("StuntYourGrowth.HiringGuard")==null,"Retired simulation hooks absent");
        Check(asm.GetType("StuntYourGrowth.RosterGuard")==null,"Service roster retention removed");
        foreach(var key in new[]{"Levels","Tenants","Condition","Vacancy","ResetVacancy","ResetAbandonment"})
            Check(typeof(GPTStuntGrowthSettings).GetProperty(key)!=null,"Requested building protection/reset retained: "+key);
        var ep=asm.GetType("StuntYourGrowth.EfficiencyPolicy",true);
        var reconcile=ep.GetMethod("Reconcile",hidden);
        Func<Efficiency[],Efficiency[],Efficiency[]> hold=(old,current)=>(Efficiency[])reconcile.Invoke(null,new object[]{old,current});
        Func<EfficiencyFactor,float,Efficiency> factor=(kind,value)=>new Efficiency {m_Factor=kind,m_Efficiency=value};
        var protectedKinds=new[]{EfficiencyFactor.WindSpeed,EfficiencyFactor.WaterDepth,EfficiencyFactor.SunIntensity,
            EfficiencyFactor.CityModifierSoftware,EfficiencyFactor.CityModifierElectronics,EfficiencyFactor.CityModifierIndustrialEfficiency,
            EfficiencyFactor.CityModifierOfficeEfficiency,EfficiencyFactor.CityModifierHospitalEfficiency,EfficiencyFactor.SpecializationBonus,
            EfficiencyFactor.CityModifierFishInput,EfficiencyFactor.CityModifierFishHub};
        foreach(EfficiencyFactor kind in Enum.GetValues(typeof(EfficiencyFactor)))
            foreach(float previous in new[]{0f,0.5f,1f,1.5f})foreach(float current in new[]{0f,0.25f,1f,2f})
            {
                var before=new[]{factor(kind,previous)};var now=new[]{factor(kind,current)};
                var result=hold(before,now);
                float expected=protectedKinds.Contains(kind)?Math.Max(previous,current):current;
                Check(result.Length==1 && result[0].m_Factor==kind && result[0].m_Efficiency==expected,"Efficiency factor isolation: "+kind);
                Check(now[0].m_Efficiency==current && before[0].m_Efficiency==previous,"Input snapshots unchanged");
            }
        var oldMix=new[]{factor(EfficiencyFactor.ElectricitySupply,1),factor(EfficiencyFactor.NotEnoughEmployees,1),factor(EfficiencyFactor.SpecializationBonus,1.5f)};
        var newMix=new[]{factor(EfficiencyFactor.SpecializationBonus,1),factor(EfficiencyFactor.NotEnoughEmployees,0.4f),factor(EfficiencyFactor.ElectricitySupply,0)};
        var mix=hold(oldMix,newMix);
        Check(mix[0].m_Efficiency==1.5f && mix[1].m_Efficiency==0.4f && mix[2].m_Efficiency==0,"Bonus protection cannot mask staffing/electricity failure after factor reorder");
        var restored=hold(mix,new[]{factor(EfficiencyFactor.ElectricitySupply,1),factor(EfficiencyFactor.NotEnoughEmployees,0.8f)});
        Check(restored.Length==2 && restored[0].m_Efficiency==1 && restored[1].m_Efficiency==0.8f,"Recovered supplies and removed bonus remain native");
        Check(hold(oldMix,Array.Empty<Efficiency>()).Length==0,"Never recreate removed factors");
        foreach(var gate in new[]{EfficiencyFactor.Disabled,EfficiencyFactor.Destroyed,EfficiencyFactor.Fire,EfficiencyFactor.Abandoned})
        {
            var closed=new[]{factor(gate,0),factor(EfficiencyFactor.SpecializationBonus,0.5f)};
            Check(hold(oldMix,closed)[1].m_Efficiency==0.5f,"Closed building resets bonus hold");
            Check(hold(closed,new[]{factor(EfficiencyFactor.SpecializationBonus,0.2f)})[0].m_Efficiency==0.2f,"Reopened building starts fresh");
        }
        Check(hold(new[]{factor(EfficiencyFactor.SpecializationBonus,float.PositiveInfinity)},new[]{factor(EfficiencyFactor.SpecializationBonus,1)})[0].m_Efficiency==1,"Invalid previous factor cannot poison new value");
        Check(hold(new[]{factor((EfficiencyFactor)250,2)},new[]{factor((EfficiencyFactor)250,0.1f)})[0].m_Efficiency==0.1f,"Unknown factors stay native");
        var hooks=asm.GetType("StuntYourGrowth.NativeHooks",true);
        Check(hooks.GetMethod("DeathRates",hidden)==null,"Removed lifespan has no mortality-rate modifier");
        foreach(var type in new[]{typeof(BuildingUpkeepSystem),typeof(PropertyProcessingSystem)})
        {
            var method=AccessTools.DeclaredMethod(type,"OnUpdate");
            var changed=((IEnumerable<CodeInstruction>)hooks.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{ReadIL(method),method})).ToArray();
            Check(changed.Count(x=>(x.operand as MethodInfo)?.DeclaringType==hooks)==(type==typeof(BuildingUpkeepSystem)?3:1),"Remaining native scheduler hooks match: "+type.Name);
        }
        Check(hooks.GetMethod("BeforeHiring",hidden)==null && hooks.GetMethod("RestoreHiring",hidden)==null,"Service hiring blocker and vacancy rewrites removed");
        var s=NewSettings();
        var toggles=Controls.Where(p=>p.CanRead && p.PropertyType==typeof(bool)).ToArray();
        var modes=Controls.Where(p=>p.PropertyType.IsEnum).ToArray();
        Check(modes.Length==6,"Six dropdowns");
        Check(toggles.Length==32,"32 checkboxes including occupancy, shopping and three coverage controls");
        foreach(var p in toggles) Check((bool)p.GetValue(s),"First-load toggle "+p.Name);
        foreach(var p in modes) Check(p.GetValue(s).ToString()=="Freeze","First-load dropdown "+p.Name);
        Check(typeof(GPTStuntGrowthSettings).GetProperty("LegacyHold")==null,"Retired city hold still exposed");
        var ordered=Controls.ToArray();
        Check(ordered[0].Name=="Enabled" && ordered[1].Name=="Customers","Master then dropdown order");
        Check(ordered[0].CustomAttributes.Any(a=>a.AttributeType.Name=="SettingsUISectionAttribute" && (string)a.ConstructorArguments[0].Value=="MasterControls"),"Master section");
        Check(Array.FindIndex(ordered,p=>p.Name=="RemoveDead")==Array.FindIndex(ordered,p=>p.Name=="Growth")+1,"Growth immediately before deathcare");
        var groups=typeof(GPTStuntGrowthSettings).CustomAttributes.Single(a=>a.AttributeType.Name=="SettingsUIGroupOrderAttribute");
        var groupList=(IEnumerable<CustomAttributeTypedArgument>)groups.ConstructorArguments[0].Value;
        Check((string)groupList.First().Value=="MasterControls","First section");
        Check((string)groupList.Last().Value=="VacancyControls","Vacancy section last");
        foreach(bool enabled in new[]{false,true,false,true})
        {
            SetAll(s,enabled);
            foreach(var p in toggles) Check((bool)p.GetValue(s)==enabled,"Bulk toggle "+p.Name);
            foreach(var p in modes) Check(p.GetValue(s).ToString()==(enabled?"Freeze":"Normal"),"Bulk mode "+p.Name);
        }
        var values=Controls.Where(p=>p.CanRead && p.Name!="Enabled").ToDictionary(p=>p.Name,p=>p.GetValue(s));
        s.Enabled=false;
        foreach(var p in Controls.Where(p=>p.CanRead && p.Name!="Enabled")) Check(Equals(values[p.Name],p.GetValue(s)),"Master changed selection");
        s.Enabled=true;
        // Exercise the real Colossal JSON reader with omitted false values from old .coc fragments.
        typeof(GPTStuntGrowthSettings).GetMethod("UseLegacyDefaults",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,null);
        JSON.WriteInto(JSON.Load("{\"Growth\":true,\"Vacancy\":true,\"Staff\":0,\"LegacyHold\":true}"),ref s);
        Check(s.Enabled && s.Growth && s.Vacancy,"Legacy saved true values");
        Check(!s.ParkMaintenance && !s.RoadWear && !s.UpkeepCosts && !s.Age,"Legacy omitted false values");
        Check(s.Schooling==SchoolMode.Freeze,"Legacy modes");
        Check(!s.UnlimitedElectricity && !s.UnlimitedWater && !s.UnlimitedSewer,"Upgrade utilities default off");
        Check(!s.PreferPublicTransportation,"Upgrade travel controls default off");
        Check(!s.MaximumResources && !s.MaximumStorage,"Upgrade inventory controls default off");
        for(int version=0;version<=5;version++)
        {
            var migrated=NewSettings();
            typeof(GPTStuntGrowthSettings).GetMethod("PrepareUpgrade",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(migrated,new object[]{version});
            JSON.WriteInto(JSON.Load("{\"Age\":false,\"Staff\":0}"),ref migrated);
            Check(migrated.PreferPublicTransportation==(version>=2),"Existing schema travel migration");
            Check(!migrated.Age,"Upgrade preserves old selections");
            Check(!migrated.MaximumOccupancy && !migrated.MaximumCommunications && !migrated.MaximumPoliceCoverage && !migrated.MaximumFireCoverage && !migrated.MaximumShopping,"New simulation features stay off for existing settings");
            Check(migrated.RemoveDead==(version>=4),"Existing settings do not silently enable corpse cleanup");
            Check(migrated.BoostOutsideWorkers==(version>=5),"Existing settings do not silently enable commuter recruitment");
            JSON.WriteInto(JSON.Load("{\"BoostOutsideWorkers\":true}"),ref migrated);
            Check(migrated.BoostOutsideWorkers,"Explicit commuter preference survives migration");
            JSON.WriteInto(JSON.Load("{\"RemoveDead\":true}"),ref migrated);
            Check(migrated.RemoveDead,"Explicit saved deathcare choice survives migration");
            JSON.WriteInto(JSON.Load("{\"HireAllAvailableJobs\":true,\"Employees\":1,\"MaximumCustomers\":true,\"Lifespan\":true,\"Abandonment\":true}"),ref migrated);
            Check(!migrated.Age,"Retired saved keys do not change remaining settings");
        }
        s=NewSettings(); JSON.WriteInto(JSON.Load("{\"Enabled\":false,\"Age\":false,\"Schooling\":3,\"DefaultsVersion\":1}"),ref s);
        Check(!s.Enabled && !s.Age && s.Schooling==SchoolMode.NoAdmissions && s.UnlimitedWater,"New schema explicit overrides");
        foreach(int oldStaff in new[]{0,1,2})
        {
            var migrated=NewSettings();
            JSON.WriteInto(JSON.Load("{\"Staff\":"+oldStaff+",\"Levels\":false,\"Tenants\":true,\"Condition\":false,\"Vacancy\":true,\"Jobs\":0}"),ref migrated);
            Check(!migrated.Levels && migrated.Tenants && !migrated.Condition && migrated.Vacancy && migrated.Jobs==EmploymentMode.Normal,"Retired staffing values preserve building choices and employment capacity");
        }
        var numericSettings=NewSettings();
        JSON.WriteInto(JSON.Load("{\"Customers\":4,\"Jobs\":3,\"IncomeTarget\":23400,\"Schooling\":3}"),ref numericSettings);
        Check(numericSettings.Customers==ValueMode.Zero && (int)numericSettings.Jobs==3 && numericSettings.IncomeTarget==23400 && numericSettings.Schooling==SchoolMode.NoAdmissions,"New numeric selections persist without changing schooling");
        var finishUpgrade=typeof(GPTStuntGrowthSettings).GetMethod("FinishUpgrade",BindingFlags.Instance|BindingFlags.NonPublic);
        finishUpgrade.Invoke(numericSettings,new object[]{5});
        Check(numericSettings.Jobs==EmploymentMode.Normal,"Retired employment Maximize migrates to Normal");
        Check(!Enum.GetNames(typeof(EmploymentMode)).Contains("Maximize"),"Employment offers no Maximize choice");
        Check(!Controls.Any(p=>p.Name=="BoostOutsideWorkers" || p.Name=="RentAmount"),"Retired independent switches are not visible");
        foreach (int jobs in new[]{0,1,2,3,4}) foreach(bool boost in new[]{false,true})
        foreach(int land in new[]{0,1,2,3,4}) foreach(bool rent in new[]{false,true})
        {
            var old=NewSettings();
            JSON.WriteInto(JSON.Load("{\"Jobs\":"+jobs+",\"BoostOutsideWorkers\":"+boost.ToString().ToLowerInvariant()+",\"LandValue\":"+land+",\"RentAmount\":"+rent.ToString().ToLowerInvariant()+"}"),ref old);
            finishUpgrade.Invoke(old,new object[]{5});
            var expectedJob=jobs==4?EmploymentMode.Zero:boost?EmploymentMode.BoostOutsideWorkers:jobs==3?EmploymentMode.Normal:(EmploymentMode)jobs;
            Check(old.Jobs==expectedJob,"Migration cannot combine recruitment and Zero");
            Check(old.LandValue==(rent && land==4?LandValueMode.ZeroFreezeRent:(LandValueMode)land),"Rent migration preserves land choice without silently zeroing it");
            Check(!old.BoostOutsideWorkers && !old.RentAmount && old.DefaultsVersion==6,"Legacy switches cannot remain active");
        }
        foreach(var mode in Enum.GetValues(typeof(ValueMode)).Cast<ValueMode>())
        {
            numericSettings.Customers=numericSettings.Income=numericSettings.Profit=mode;
            numericSettings.LandValue=(LandValueMode)(int)mode;
            foreach(bool enabled in new[]{false,true})
            {
                numericSettings.Enabled=enabled;
                bool disabled=!enabled || mode!=ValueMode.Maximize;
                Check(numericSettings.CustomerTargetDisabled()==disabled && numericSettings.IncomeTargetDisabled()==disabled && numericSettings.LandValueTargetDisabled()==disabled && numericSettings.ProfitTargetDisabled()==disabled,"Targets enabled only with active Maximize");
            }
        }
        foreach(var pair in new[]{new[]{"Customers","CustomerTarget"},new[]{"Income","IncomeTarget"},new[]{"LandValue","LandValueTarget"},new[]{"Profit","ProfitTarget"}})
            Check(Array.FindIndex(ordered,p=>p.Name==pair[1])==Array.FindIndex(ordered,p=>p.Name==pair[0])+1,"Target follows corresponding dropdown");
        SetAll(numericSettings,true);
        Check(numericSettings.IncomeTarget==23400 && numericSettings.Customers==ValueMode.Freeze,"Enable all preserves custom targets and uses Freeze");
        var localeType=asm.GetType("StuntYourGrowth.Locale");
        var labels=((IEnumerable<KeyValuePair<string,string>>)localeType.GetMethod("ReadEntries").Invoke(Activator.CreateInstance(localeType,new object[]{s}),new object[]{null,null})).ToDictionary(x=>x.Key,x=>x.Value);
        Func<string,string> label=n=>labels[s.GetOptionLabelLocaleID(n)];
        Check(label("Enabled")=="Enable Mod","Master label");
        Check(label("ResetVacancy")=="Reset Vacant Buildings" && label("ResetAbandonment")=="Reset Abandonment","Repeatable reset labels");
        Check(label("EnableAllCheckboxes")=="Enable All Options" && label("DisableAllCheckboxes")=="Disable All Options","Bulk labels");
        Check(label("RemoveDead")=="Remove the Dead & Decaying","Deathcare checkbox label");
        Check(label("BoostOutsideWorkers")=="Boost Outside Worker Recruitment","Commuter checkbox label");
        var occupancy=asm.GetType("StuntYourGrowth.OccupancyPolicy",true);
        Func<int,int,int,int,int> occupancyBudget=(free,pending,batch,limit)=>(int)occupancy.GetMethod("Budget",hidden).Invoke(null,new object[]{free,pending,batch,limit});
        foreach(int free in new[]{0,1,3,20,1000,int.MaxValue})
        foreach(int pending in new[]{-1,0,1,15,16,63,64,int.MaxValue})
        foreach(var pair in new[]{new[]{4,64},new[]{2,16}})
        {
            int count=occupancyBudget(free,pending,pair[0],pair[1]);
            Check(count>=0 && count<=pair[0],"Occupancy bounded batch");
            Check(pending>=0 && pending<pair[1] && free>pending ? count<=free-pending && count<=pair[1]-pending : count==0,"Occupancy reserves pending tenants and stops at capacity/backlog");
        }
        foreach(bool stop in new[]{false,true}) foreach(bool maximum in new[]{false,true})
            Check((bool)occupancy.GetMethod("AllowNativeImmigration",hidden).Invoke(null,new object[]{stop,maximum})==(!stop && !maximum),"Occupancy owns immigration while enabled; no duplicate native batch");
        var fullTelecom=new TelecomCoverage{m_SignalStrength=255,m_NetworkLoad=127};
        Check(fullTelecom.networkQuality==255,"Communications uses maximum native quality without overflow");
        var shopping=asm.GetType("StuntYourGrowth.ShoppingHooks",true);
        var originalEconomy=new EconomyParameterData{m_TrafficReduction=0.75f};
        var shoppingEconomy=(EconomyParameterData)shopping.GetMethod("Maximize",hidden).Invoke(null,new object[]{originalEconomy});
        Check(shoppingEconomy.m_TrafficReduction==0 && originalEconomy.m_TrafficReduction==0.75f,"Shopping changes the scheduled copy only");
        foreach(var f in typeof(EconomyParameterData).GetFields())
            if(f.Name!="m_TrafficReduction" && (f.FieldType.IsPrimitive || f.FieldType.IsEnum))Check(Equals(f.GetValue(originalEconomy),f.GetValue(shoppingEconomy)),"Other scalar economy parameters preserved: "+f.Name);
        var householdUpdate=AccessTools.DeclaredMethod(typeof(HouseholdBehaviorSystem),"OnUpdate");
        var shoppingIL=((IEnumerable<CodeInstruction>)shopping.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{ReadIL(householdUpdate)})).ToArray();
        Check(shoppingIL.Count(c=>(c.operand as MethodInfo)?.DeclaringType==shopping)==1,"Exactly one native household job scheduler wrapped");
        var commuter=asm.GetType("StuntYourGrowth.CommuterPolicy",true);
        Func<Workplaces,Workplaces,int> shortage=(jobs,available)=>(int)commuter.GetMethod("Shortage",hidden).Invoke(null,new object[]{jobs,available});
        Func<int,int,int,int,int> budget=(need,waiting,native,max)=>(int)commuter.GetMethod("Budget",hidden).Invoke(null,new object[]{need,waiting,native,max});
        Func<int,int,int,int,int> room=(households,workers,ratio,native)=>(int)commuter.GetMethod("HouseholdRoom",hidden).Invoke(null,new object[]{households,workers,ratio,native});
        Workplaces levels(params int[] amounts){var v=new Workplaces();for(int i=0;i<amounts.Length;i++)v[i]=amounts[i];return v;}
        Check(shortage(levels(),levels(9,9,9,9,9))==0,"No vacancies means no commuter demand");
        Check(shortage(levels(5),levels(0,0,0,0,5))==0,"Qualified workers can fill lower-tier vacancies");
        Check(shortage(levels(0,0,0,0,5),levels(5))==5,"Lower qualifications cannot cover higher-tier shortage");
        Check(shortage(levels(5,0,0,0,5),levels(0,0,0,0,5))==5,"Each available worker counted only once");
        Check(shortage(levels(10,10,10,10,10),levels(10,10,10,10,10))==0,"Exact qualification match has no shortage");
        Check(shortage(levels(int.MaxValue,int.MaxValue,int.MaxValue,int.MaxValue,int.MaxValue),levels())==int.MaxValue,"Large demand saturates without overflow");
        Check(shortage(levels(-5,0,0,0,2),levels(-9))==2,"Invalid negative counts cannot create extra supply");
        Check(budget(100,0,0,2)==16 && budget(5,0,0,2)==5,"Batch respects shortage and16worker limit");
        Check(budget(100,64,0,2)==0 && budget(100,63,0,2)==1,"Waiting commuter backlog caps new recruitment");
        Check(budget(10,4,2,3)==0,"Pending and already planned native workers reserve shortage");
        Check(budget(int.MaxValue,0,int.MaxValue,int.MaxValue)==0,"Large native batch cannot overflow reservation");
        Check(budget(0,0,0,1)==0 && budget(20,-1,0,1)==0 && budget(20,0,0,0)==0,"Invalid or absent demand cannot recruit");
        Check(room(9,100,10,0)==1 && room(9,100,10,1)==0,"Native commuter ceiling includes planned households");
        Check(room(10,100,10,0)==0 && room(0,0,10,0)==0,"No boost at native cap or without workers");
        Check(room(0,int.MaxValue,int.MaxValue,0)==1,"Large ratio arithmetic cannot overflow");
        Check(room(0,100,0,0)==16 && room(0,100,-1,0)==0,"Native zero ratio and invalid ratio handled separately");
        var planned=commuter.GetMethod("NativePlanned",hidden);
        Check((int)planned.Invoke(null,new object[]{levels(99,99,30,30,30),levels(99,99,10,10,10),10})==6,"Reserve native educated-worker recruitment");
        Check((int)planned.Invoke(null,new object[]{levels(99,99),levels(),10})==0,"Low-tier shortages are additional to native spawn formula");
        var adultOnly=commuter.GetMethod("AdultOnly",hidden);
        Check((bool)adultOnly.Invoke(null,new object[]{new HouseholdData{m_AdultCount=2},2}),"Adult-only household fits worker budget");
        Check((bool)adultOnly.Invoke(null,new object[]{new HouseholdData{m_AdultCount=1,m_ChildCount=1},2}),"Native exclusive child-count upper bound still produces adults only");
        foreach(var excluded in new[]{new HouseholdData{m_AdultCount=3},new HouseholdData{m_AdultCount=1,m_ChildCount=2},new HouseholdData{m_AdultCount=1,m_StudentCount=1},new HouseholdData{m_AdultCount=1,m_ElderCount=1},new HouseholdData()})
            Check(!(bool)adultOnly.Invoke(null,new object[]{excluded,2}),"Reject unsuitable commuter household");
        var random=new Random(636);
        for(int trial=0;trial<256;trial++)
        {
            var jobs=levels();var available=levels();int total=0;
            for(int i=0;i<5;i++){jobs[i]=random.Next(100);available[i]=random.Next(100);total+=jobs[i];}
            int missing=shortage(jobs,available);
            Check(missing>=0 && missing<=total,"Shortage bounded by actual vacancies");
            var increased=available;increased[random.Next(5)]+=10;
            Check(shortage(jobs,increased)<=missing,"Adding eligible labor never increases shortage");
            int waiting=random.Next(80),native=random.Next(20),capacity=random.Next(1,5);
            int extra=budget(missing,waiting,native,capacity);
            Check(extra>=0 && extra<=16 && extra<=missing,"Recruitment remains bounded under mixed shortages");
            Check(budget(missing,waiting+1,native,capacity)<=extra,"More pending commuters never increases recruitment");
        }
        var commuterHooks=asm.GetType("StuntYourGrowth.CommuterHooks",true);
        var commuterUpdate=AccessTools.DeclaredMethod(typeof(CommuterSpawnSystem),"OnUpdate");
        var commuterIL=((IEnumerable<CodeInstruction>)commuterHooks.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{ReadIL(commuterUpdate)})).ToArray();
        int commuterCall=Array.FindIndex(commuterIL,x=>(x.operand as MethodInfo)?.DeclaringType==commuterHooks);
        Check(commuterCall>0 && commuterIL[commuterCall-1].opcode==System.Reflection.Emit.OpCodes.Ldarg_0,"Native commuter scheduler and all copied job fields match installed game");
        Check(asm.GetType("StuntYourGrowth.DeathcareHooks")==null,"Outside-hearse routing hooks excluded");
        var deathcare=typeof(DeadCitizenCleanupSystem);
        var eligible=deathcare.GetMethod("Eligible",hidden);
        var retire=deathcare.GetMethod("RetireTransport",hidden);
        for(int flags=0;flags<256;flags++)
        {
            foreach(bool pickup in new[]{false,true})foreach(bool vehicle in new[]{false,true})foreach(bool facility in new[]{false,true})
            {
                bool expected=(flags&10)==10 && !pickup && !vehicle && !facility;
                Check((bool)eligible.Invoke(null,new object[]{(Game.Citizens.HealthProblemFlags)flags,pickup,vehicle,facility})==expected,"Cleanup excludes living citizens, pickups, vehicles and deathcare");
            }
            var before=new Game.Citizens.HealthProblem { m_Flags=(Game.Citizens.HealthProblemFlags)flags, m_Timer=17, m_HealthcareRequest=new Unity.Entities.Entity{Index=123,Version=4} };
            var after=(Game.Citizens.HealthProblem)retire.Invoke(null,new object[]{before});
            Check((int)after.m_Flags==(flags&~8) && after.m_Timer==17 && after.m_HealthcareRequest==Unity.Entities.Entity.Null,"Retired corpse request preserves death, health flags and timer");
        }
        foreach(var p in Controls) Check(labels.ContainsKey(s.GetOptionLabelLocaleID(p.Name)),"Missing label "+p.Name);
        var dropdownLabels=modes.Select(p=>label(p.Name)).ToArray();
        Check(dropdownLabels.SequenceEqual(dropdownLabels.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)),"Alphabetical dropdowns");
        var checkboxLabels=toggles.Where(p=>p.Name!="Growth" && p.Name!="RemoveDead").Where(p=>p.CustomAttributes.Any(a=>a.AttributeType.Name=="SettingsUISectionAttribute" && (string)a.ConstructorArguments[0].Value=="Checkboxes")).Select(p=>label(p.Name)).ToArray();
        Check(checkboxLabels.SequenceEqual(checkboxLabels.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)),"Alphabetical checkboxes");
        var utility=asm.GetType("StuntYourGrowth.UtilityHooks");
        var power=(Func<ElectricityConsumer,ElectricityConsumer>)Delegate.CreateDelegate(typeof(Func<ElectricityConsumer,ElectricityConsumer>),utility.GetMethod("SupplyElectricity",hidden));
        var water=(Func<WaterConsumer,bool,bool,WaterConsumer>)Delegate.CreateDelegate(typeof(Func<WaterConsumer,bool,bool,WaterConsumer>),utility.GetMethod("SupplyWater",hidden));
        foreach(int demand in new[]{0,1,10000,int.MaxValue})
        foreach(var flags in new[]{ElectricityConsumerFlags.None,ElectricityConsumerFlags.NoElectricityWarning,ElectricityConsumerFlags.BottleneckWarning,(ElectricityConsumerFlags)255})
        {
            var e=power(new ElectricityConsumer{m_WantedConsumption=demand,m_CooldownCounter=42,m_Flags=flags});
            Check(e.m_WantedConsumption==demand && e.m_FulfilledConsumption==demand && e.m_CooldownCounter==0 && e.electricityConnected,"Power fulfillment");
            Check((e.m_Flags & (ElectricityConsumerFlags.NoElectricityWarning|ElectricityConsumerFlags.BottleneckWarning))==0,"Power warnings");
            Check((e.m_Flags & (ElectricityConsumerFlags)248)==(flags & (ElectricityConsumerFlags)248),"Unrelated power flags");
            Check(power(e).Equals(e),"Repeated power supply");
        }
        foreach(int demand in new[]{0,1,10000,int.MaxValue})
        foreach(bool fresh in new[]{false,true})
        foreach(bool sewer in new[]{false,true})
        foreach(var flags in new[]{WaterConsumerFlags.None,WaterConsumerFlags.WaterConnected,WaterConsumerFlags.SewageConnected,(WaterConsumerFlags)255})
        {
            var old=new WaterConsumer{m_WantedConsumption=demand,m_FulfilledFresh=2,m_FulfilledSewage=3,m_Pollution=0.75f,m_FreshCooldownCounter=255,m_SewageCooldownCounter=42,m_Flags=flags};
            var w=water(old,fresh,sewer);
            Check(w.m_WantedConsumption==demand,"Demand preserved");
            Check(w.m_FulfilledFresh==(fresh?demand:2) && w.m_FreshCooldownCounter==(fresh?0:255) && w.m_Pollution==(fresh?0f:0.75f),"Fresh isolated");
            Check(w.m_FulfilledSewage==(sewer?demand:3) && w.m_SewageCooldownCounter==(sewer?0:42),"Sewer isolated");
            Check(w.m_Flags==(flags | (fresh?WaterConsumerFlags.WaterConnected:0) | (sewer?WaterConsumerFlags.SewageConnected:0)),"Water flags isolated");
            Check(water(w,fresh,sewer).Equals(w),"Repeated water supply");
        }
        // Read the installed game's actual managed scheduler IL, not a hand-written imitation.
        foreach(var type in new[]{typeof(DispatchElectricitySystem),typeof(DispatchWaterSystem)})
        {
            var original=AccessTools.DeclaredMethod(type,"OnUpdate");
            var il=ReadIL(original);
            var changed=((IEnumerable<CodeInstruction>)utility.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{il,original})).ToArray();
            var indexes=changed.Select((x,i)=>new{x,i}).Where(x=>(x.x.operand as MethodInfo)?.DeclaringType==utility).ToArray();
            Check(indexes.Length==1,"One utility scheduler replacement");
            Check(changed[indexes[0].i-1].opcode==System.Reflection.Emit.OpCodes.Ldarg_0,"Scheduler receives owning system");
            var replaced=(MethodInfo)indexes[0].x.operand;
            Check(replaced.GetParameters().Length==4 && replaced.ReturnType.FullName=="Unity.Jobs.JobHandle","Scheduler stack signature");
        }
        var transit=asm.GetType("StuntYourGrowth.TransitHooks");
        var prefer=transit.GetMethod("TryPrefer",hidden);
        foreach(PathMethod time in new[]{PathMethod.PublicTransportDay,PathMethod.PublicTransportNight})
        {
            var original=new PathfindParameters{m_Methods=time|PathMethod.Pedestrian|PathMethod.Road|PathMethod.Parking|PathMethod.Taxi|PathMethod.Bicycle,m_MaxCost=1234,m_PathfindFlags=PathfindFlags.Stable};
            object[] args={original,null}; Check((bool)prefer.Invoke(null,args),"Transit preference eligible");
            var preferred=(PathfindParameters)args[1];
            Check(preferred.m_Methods==(time|PathMethod.Pedestrian|PathMethod.Bicycle),"Transit methods, day/night and bicycle preserved");
            foreach(var field in typeof(PathfindParameters).GetFields()) if(field.Name!="m_Methods") Check(Equals(field.GetValue(original),field.GetValue(preferred)),"Preserve route parameter "+field.Name);
            foreach(var noPreference in new[]{new PathfindParameters{m_Methods=PathMethod.Road},new PathfindParameters{m_Methods=time|PathMethod.Pedestrian},new PathfindParameters{m_Methods=original.m_Methods,m_PathfindFlags=PathfindFlags.IgnorePath},new PathfindParameters{m_Methods=original.m_Methods,m_PathfindFlags=PathfindFlags.SkipPathfind}})
            {
                object[] unchanged={noPreference,null};Check(!(bool)prefer.Invoke(null,unchanged) && noPreference.Equals(unchanged[1]),"Ineligible route unchanged");
            }
        }
        var fallback=transit.GetMethod("NeedsFallback",hidden);
        Check((bool)fallback.Invoke(null,new object[]{new PathfindResult{m_Duration=-1}}),"Missing transit route falls back");
        Check(!(bool)fallback.Invoke(null,new object[]{new PathfindResult{m_Duration=0}}),"Valid zero-distance route succeeds");
        Check((bool)fallback.Invoke(null,new object[]{new PathfindResult{m_Duration=10,m_ErrorCode=ErrorCode.TooManyEndAccessRequirements}}),"Native path error falls back");
        var stock=asm.GetType("StuntYourGrowth.ResourceStockGuard");
        var stockable=(Func<Resource,bool>)Delegate.CreateDelegate(typeof(Func<Resource,bool>),stock.GetMethod("Stockable",hidden));
        Check(!stockable(Resource.Money) && !stockable(Resource.NoResource) && !stockable(Resource.Oil|Resource.Ore),"Money and combined flags cannot be refilled");
        Check(stockable(Resource.Oil) && stockable(Resource.Ore),"Native goods accepted");
        var share=(Func<int,long,long,int>)Delegate.CreateDelegate(typeof(Func<int,long,long,int>),stock.GetMethod("Share",hidden));
        var addition=(Func<int,int,long,int>)Delegate.CreateDelegate(typeof(Func<int,int,long,int>),stock.GetMethod("Addition",hidden));
        var limit=(Func<int,int,int,int,int>)Delegate.CreateDelegate(typeof(Func<int,int,int,int,int>),stock.GetMethod("WarehouseLimit",hidden));
        Check(limit(100,3,4,5)==6000,"Native warehouse level and footprint capacity");
        Check(limit(int.MaxValue,int.MaxValue,int.MaxValue,int.MaxValue)==int.MaxValue,"Warehouse arithmetic saturates without overflow");
        Check(limit(100,0,4,5)==0 && limit(-100,3,4,5)==0,"Invalid capacities do not refill");
        foreach(int capacity in new[]{0,1,100,5000,int.MaxValue})
        foreach(int count in new[]{1,2,3,17})
        {
            int target=share(capacity,1,count);
            Check((long)target*count<=capacity,"Shared storage cap");
            int current=capacity/4;
            int add=addition(current,target,capacity-current);
            Check(add>=0 && (long)current+add<=capacity,"Refill stays within capacity");
            Check(addition(capacity+0,target,0)==0,"No space means no refill");
        }
        Check(share(int.MaxValue,long.MaxValue,long.MaxValue)==int.MaxValue,"Large resource weights do not overflow");
        Check(addition(200,100,500)==0,"Existing excess stock is not destroyed");
        Check(addition(0,200,50)==50,"Shared free space limits a refill");
        Check(share(100,1,2)==50,"Industrial inputs leave output room");
        var resourceHooks=asm.GetType("StuntYourGrowth.ResourceHooks");
        var areaUpdate=AccessTools.DeclaredMethod(typeof(AreaLotSimulationSystem),"OnUpdate");
        var extraction=((IEnumerable<CodeInstruction>)resourceHooks.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{ReadIL(areaUpdate)})).ToArray();
        Check(extraction.Count(x=>(x.operand as MethodInfo)?.DeclaringType==resourceHooks)==1,"Native extraction scheduler hook");
        Console.WriteLine("PASS: "+checks+" checks; "+toggles.Length+" checkboxes, 6 dropdowns; defaults, migration, removed controls, efficiency isolation, utilities, transit policies and native scheduler hooks.");
    }
}
class Launcher
{
    static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
            string file=new AssemblyName(e.Name).Name+".dll";
            foreach(string dir in new[]{args[0],Environment.GetEnvironmentVariable("CSII_MANAGEDPATH")}){
                string path=System.IO.Path.Combine(dir,file);if(System.IO.File.Exists(path))return Assembly.LoadFrom(path);
            }
            return null;
        };
        try {Offline633.Run();return 0;} catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
