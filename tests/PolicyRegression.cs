using System;
using System.Linq;
using StuntYourGrowth;

class PolicyTests
{
    static int count;
    static void Check(bool ok) { if(!ok) throw new Exception("Failed policy case "+count); count++; }
    public static void Main()
    {
        foreach (int baseline in new[]{-100,0,50,100})
        foreach (int current in new[]{baseline-1,baseline,baseline+1})
        {
            Check(Policy.Protect(baseline,current,ValueMode.Normal)==current);
            Check(Policy.Protect(baseline,current,ValueMode.Freeze)==baseline);
            Check(Policy.Protect(baseline,current,ValueMode.PreventDecreases)==Math.Max(baseline,current));
            Check(Policy.Protect(baseline+0.25f,current+0.25f,ValueMode.PreventDecreases)==Math.Max(baseline,current)+0.25f);
        }
        Check((int)ValueMode.Normal==0 && (int)ValueMode.Freeze==1 && (int)ValueMode.PreventDecreases==2);
        var keys = new System.Collections.Generic.HashSet<int>();
        foreach(ValueMode a in Enum.GetValues(typeof(ValueMode)))
        foreach(ValueMode b in Enum.GetValues(typeof(ValueMode)))
        foreach(ValueMode c in Enum.GetValues(typeof(ValueMode))) Check(keys.Add(Policy.ModeKey(a,b,c)));
        foreach(int value in new[]{int.MinValue,-1,0,1,100,int.MaxValue})
        foreach(int target in new[]{int.MinValue,-1,0,1,100,int.MaxValue})
        {
            Check(Policy.Target(value,ValueMode.Zero,target,1000000)==0);
            Check(Policy.Target(value,ValueMode.Maximize,target,1000000)==Math.Max(0,Math.Min(target,1000000)));
            Check(Policy.Target(value,ValueMode.Normal,target,1000000)==value);
        }
        foreach(SchoolMode mode in Enum.GetValues(typeof(SchoolMode)))
        {
            Check(Policy.Admissions(mode)==(mode==SchoolMode.Normal || mode==SchoolMode.EnrollmentOnly));
            Check(Policy.Education(mode)==(mode==SchoolMode.Normal || mode==SchoolMode.NoAdmissions));
        }
        foreach(int h in new[]{0,1,50,99,100})
        foreach(int w in new[]{0,1,50,99,100})
        foreach(int floor in new[]{0,55,100})
        {
            byte health=(byte)h, wellbeing=(byte)w;
            Policy.ProtectHappiness(floor,ref health,ref wellbeing);
            Check(health>=h && wellbeing>=w && health<=100 && wellbeing<=100 && (health+wellbeing)/2>=floor);
        }
        Console.WriteLine(count+" policy cases passed");
    }
}
