using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
class Check {
    static object Get(object obj,string name) {return obj.GetType().GetProperty(name).GetValue(obj);}
    static void Main(string[] args) {
        string managed=Environment.GetEnvironmentVariable("CSII_MANAGEDPATH");
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => {
            string file=new AssemblyName(e.Name).Name+".dll";
            foreach(string dir in new[]{managed,Path.GetDirectoryName(args[0])}) {
                string p=Path.Combine(dir,file);
                if(File.Exists(p)) return Assembly.LoadFrom(p);
            }
            return null;
        };
        var asm=Assembly.LoadFrom(args[0]);
        var t=asm.GetType("StuntYourGrowth.GPTStuntGrowthSettings",true);
        object settings=FormatterServices.GetUninitializedObject(t);
        t.GetMethod("SetDefaults").Invoke(settings,null);
        var policies=asm.GetType("StuntYourGrowth.ProtectionSystem",true);
        var game=t.BaseType.Assembly;
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        Func<string,object,object,object> protect=(method,raw,baseline)=>{
            var parameters=new object[]{raw,baseline,settings};
            policies.GetMethod(method,flags).Invoke(null,parameters);
            return parameters[0];
        };
        t.GetMethod("SetCheckboxes",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(settings,new object[]{false});
        foreach(string key in new[]{"ParkMaintenance","RoadWear","UpkeepCosts"}){
            if((bool)t.GetProperty(key).GetValue(settings))throw new Exception("New option default on "+key);
            t.GetProperty(key).SetValue(settings,true);
            if((new[]{"ParkMaintenance","RoadWear","UpkeepCosts"}).Count(x=>(bool)t.GetProperty(x).GetValue(settings))!=1)throw new Exception("Settings coupled");
            t.GetProperty(key).SetValue(settings,false);
        }
        foreach(bool enabled in new[]{true,false}){
            t.GetMethod("SetCheckboxes",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(settings,new object[]{enabled});
            foreach(string key in new[]{"Enabled","ParkMaintenance","RoadWear","UpkeepCosts"})if((bool)t.GetProperty(key).GetValue(settings)!=enabled)throw new Exception("Bulk selection failed");
        }
        var park=game.GetType("Game.Buildings.Park"); var lane=game.GetType("Game.Net.LaneCondition"); var road=game.GetType("Game.Net.NetCondition");
        foreach(var trial in new[]{Tuple.Create(park,"m_Maintenance","ProtectPark"),Tuple.Create(lane,"m_Wear","ProtectLane")}){
            var field=trial.Item1.GetField(trial.Item2);
            foreach(int value in new[]{2,5,8}){
                var current=Activator.CreateInstance(trial.Item1);var previous=Activator.CreateInstance(trial.Item1);
                field.SetValue(current,Convert.ChangeType(value,field.FieldType));field.SetValue(previous,Convert.ChangeType(5,field.FieldType));
                int after=Convert.ToInt32(field.GetValue(protect(trial.Item3,current,previous)));
                if(after!=(trial.Item1==park?Math.Max(value,5):Math.Min(value,5)))throw new Exception("Unexpected protected condition");
            }
        }
        var wear=road.GetField("m_Wear");var a=Activator.CreateInstance(road);var b=Activator.CreateInstance(road);
        wear.SetValue(a,Activator.CreateInstance(wear.FieldType,new object[]{8f,2f}));wear.SetValue(b,Activator.CreateInstance(wear.FieldType,new object[]{5f,5f}));
        var held=wear.GetValue(protect("ProtectRoad",a,b));if((float)wear.FieldType.GetField("x").GetValue(held)!=5f||(float)wear.FieldType.GetField("y").GetValue(held)!=2f)throw new Exception("Road endpoints did not clamp independently");
        var jobType=asm.GetType("StuntYourGrowth.NativeHooks+WaiveUpkeepJob",true);
        object job=Activator.CreateInstance(jobType);
        var expensesField=jobType.GetField("Expenses");var arrayType=expensesField.FieldType;
        var categories=game.GetType("Game.City.ExpenseSource");
        int count=(int)Enum.Parse(categories,"Count");
        var original=Enumerable.Range(1,count).Select(i=>-i*10).ToArray();
        var currentCosts=(int[])original.Clone();
        var pin=System.Runtime.InteropServices.GCHandle.Alloc(currentCosts,System.Runtime.InteropServices.GCHandleType.Pinned);
        try {
            object buffer=Activator.CreateInstance(arrayType);
            unsafe {
              var bytes=arrayType.GetField("m_Buffer",BindingFlags.NonPublic|BindingFlags.Instance);
              bytes.SetValue(buffer,Pointer.Box(pin.AddrOfPinnedObject().ToPointer(),bytes.FieldType));
            }
            arrayType.GetField("m_Length",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(buffer,count);
            expensesField.SetValue(job,buffer);jobType.GetMethod("Execute").Invoke(job,null);
            for(int i=0;i<count;i++){
                string name=Enum.GetName(categories,i);int expected=(name=="MapTileUpkeep"||name=="ServiceUpkeep")?0:original[i];
                if(currentCosts[i]!=expected)throw new Exception("Unexpected expense "+name);
            }
        } finally {pin.Free();}
        Console.WriteLine("PASS: independent settings, bulk enable and disable, park and lane deterioration and repairs, two road endpoints, all "+count+" expense categories");
    }
}
