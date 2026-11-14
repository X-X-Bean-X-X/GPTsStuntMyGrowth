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
        Check(asm.GetName().Version.ToString()=="1.75.633.0","Assembly version");
        var s=NewSettings();
        var toggles=Controls.Where(p=>p.CanRead && p.PropertyType==typeof(bool)).ToArray();
        var modes=Controls.Where(p=>p.PropertyType.IsEnum).ToArray();
        Check(modes.Length==8,"Eight dropdowns");
        foreach(var p in toggles) Check((bool)p.GetValue(s),"First-load toggle "+p.Name);
        foreach(var p in modes) Check(p.GetValue(s).ToString()=="Freeze","First-load dropdown "+p.Name);
        Check(typeof(GPTStuntGrowthSettings).GetProperty("LegacyHold")==null,"Retired city hold still exposed");
        var ordered=Controls.ToArray();
        Check(ordered[0].Name=="Enabled" && ordered[1].Name=="Growth","Master and growth order");
        Check(ordered[0].CustomAttributes.Any(a=>a.AttributeType.Name=="SettingsUISectionAttribute" && (string)a.ConstructorArguments[0].Value=="MasterControls"),"Master section");
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
        Check(s.Staff==ValueMode.Normal && s.Schooling==SchoolMode.Freeze,"Legacy modes");
        Check(!s.UnlimitedElectricity && !s.UnlimitedWater && !s.UnlimitedSewer,"Upgrade utilities default off");
        Check(!s.MaximumCustomers && !s.PreferPublicTransportation,"Upgrade travel controls default off");
        Check(!s.MaximumResources && !s.MaximumStorage,"Upgrade inventory controls default off");
        s=NewSettings(); JSON.WriteInto(JSON.Load("{\"Enabled\":false,\"Age\":false,\"Schooling\":3,\"DefaultsVersion\":1}"),ref s);
        Check(!s.Enabled && !s.Age && s.Schooling==SchoolMode.NoAdmissions && s.UnlimitedWater,"New schema explicit overrides");
        var localeType=asm.GetType("StuntYourGrowth.Locale");
        var labels=((IEnumerable<KeyValuePair<string,string>>)localeType.GetMethod("ReadEntries").Invoke(Activator.CreateInstance(localeType,new object[]{s}),new object[]{null,null})).ToDictionary(x=>x.Key,x=>x.Value);
        Func<string,string> label=n=>labels[s.GetOptionLabelLocaleID(n)];
        Check(label("Enabled")=="Enable Mod","Master label");
        Check(label("ResetVacancy")=="Reset Vacant Buildings" && label("ResetAbandonment")=="Reset Abandonment","Repeatable reset labels");
        Check(label("EnableAllCheckboxes")=="Enable All Options" && label("DisableAllCheckboxes")=="Disable All Options","Bulk labels");
        foreach(var p in Controls) Check(labels.ContainsKey(s.GetOptionLabelLocaleID(p.Name)),"Missing label "+p.Name);
        var dropdownLabels=modes.Select(p=>label(p.Name)).ToArray();
        Check(dropdownLabels.SequenceEqual(dropdownLabels.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)),"Alphabetical dropdowns");
        var checkboxLabels=toggles.Where(p=>p.CustomAttributes.Any(a=>a.AttributeType.Name=="SettingsUISectionAttribute" && (string)a.ConstructorArguments[0].Value=="Checkboxes")).Select(p=>label(p.Name)).ToArray();
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
        var customers=asm.GetType("StuntYourGrowth.CustomerHooks");
        var nativeJob=typeof(HouseholdBehaviorSystem).GetNestedType("HouseholdTickJob",BindingFlags.NonPublic);
        object inputJob=Activator.CreateInstance(nativeJob);
        var economyField=nativeJob.GetField("m_EconomyParameters");
        var economy=new EconomyParameterData(); economy.m_TrafficReduction=0.75f;
        economyField.SetValue(inputJob,economy);
        var prepare=customers.GetMethod("PrepareHousehold",hidden).MakeGenericMethod(nativeJob);
        foreach(bool enabled in new[]{false,true})
        {
            var adjusted=prepare.Invoke(null,new[]{inputJob,(object)enabled});
            // Native containers require Unity's runtime; compare scalar scheduling and policy inputs offline.
            foreach(var field in nativeJob.GetFields().Where(f=>f.FieldType.IsPrimitive))
                Check(Equals(field.GetValue(inputJob),field.GetValue(adjusted)),"Preserve household job "+field.Name);
            var adjustedEconomy=(EconomyParameterData)economyField.GetValue(adjusted);
            foreach(var field in typeof(EconomyParameterData).GetFields().Where(f=>f.FieldType.IsPrimitive))
                Check(Equals(field.GetValue(adjustedEconomy),enabled && field.Name=="m_TrafficReduction"?(object)0f:field.GetValue(economy)),"Preserve economy "+field.Name);
        }
        Check(((EconomyParameterData)economyField.GetValue(inputJob)).m_TrafficReduction==0.75f,"Original economy copy unchanged");
        var nativeUpdate=AccessTools.DeclaredMethod(typeof(HouseholdBehaviorSystem),"OnUpdate");
        var edited=((IEnumerable<CodeInstruction>)customers.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{ReadIL(nativeUpdate),nativeUpdate})).ToArray();
        Check(edited.Count(x=>(x.operand as MethodInfo)?.DeclaringType==customers)==1,"Native shopping scheduler hook");
        string time2WorkPath=Environment.GetEnvironmentVariable("STUNT_TIME2WORK_PATH");
        if(!string.IsNullOrEmpty(time2WorkPath))
        {
            var time2Work=Assembly.LoadFrom(time2WorkPath).GetType("Time2Work.Time2WorkCitizenBehaviorSystem",true);
            var tick=time2Work.GetNestedType("CitizenAITickJob",BindingFlags.NonPublic);
            object original=Activator.CreateInstance(tick);
            tick.GetField("shoppingTripGatesEnabled").SetValue(original,true);
            tick.GetField("householdShoppingCooldownEnabled").SetValue(original,true);
            var prepareCitizen=customers.GetMethod("PrepareCitizen",hidden).MakeGenericMethod(tick);
            foreach(bool enabled in new[]{false,true})
            {
                var adjusted=prepareCitizen.Invoke(null,new[]{original,(object)enabled});
                foreach(var field in tick.GetFields().Where(f=>f.FieldType.IsPrimitive))
                {
                    bool gate=field.Name=="shoppingTripGatesEnabled" || field.Name=="householdShoppingCooldownEnabled";
                    Check(Equals(field.GetValue(adjusted),enabled && gate?(object)false:field.GetValue(original)),"Time2Work isolated shopping gates "+field.Name);
                }
            }
            var update=AccessTools.DeclaredMethod(time2Work,"OnUpdate");
            var patched=((IEnumerable<CodeInstruction>)customers.GetMethod("ModifyDispatch",hidden).Invoke(null,new object[]{ReadIL(update),update})).ToArray();
            Check(patched.Count(x=>(x.operand as MethodInfo)?.DeclaringType==customers)==1,"Time2Work scheduler hook");
        }
        var customerMode=typeof(GPTStuntGrowthSettings).GetProperty("CustomerMode",BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(ValueMode mode in Enum.GetValues(typeof(ValueMode)))
        {
            s.Customers=mode; s.MaximumCustomers=true;
            Check((ValueMode)customerMode.GetValue(s)==ValueMode.Normal,"Real customer counts advance");
            s.MaximumCustomers=false;
            Check((ValueMode)customerMode.GetValue(s)==mode,"Saved customer mode restored");
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
        Console.WriteLine("PASS: "+checks+" checks; "+toggles.Length+" checkboxes, 8 dropdowns; defaults, migration, bulk actions, layout, utilities, shopping, transit policies and native scheduler hooks.");
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
