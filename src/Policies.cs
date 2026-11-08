using System;

namespace StuntYourGrowth
{
    public enum ValueMode { Normal, Freeze, PreventDecreases }
    public enum SchoolMode { Normal, Freeze, EnrollmentOnly, NoAdmissions }

    internal static class Policy
    {
        public static bool Admissions(SchoolMode mode) =>
            mode == SchoolMode.Normal || mode == SchoolMode.EnrollmentOnly;

        public static bool Education(SchoolMode mode) =>
            mode == SchoolMode.Normal || mode == SchoolMode.NoAdmissions;

        public static int Protect(int previous, int current, ValueMode mode) =>
            mode == ValueMode.Freeze ? previous :
            mode == ValueMode.PreventDecreases ? Math.Max(previous, current) : current;
        public static float Protect(float previous, float current, ValueMode mode) =>
            mode == ValueMode.Freeze ? previous :
            mode == ValueMode.PreventDecreases ? Math.Max(previous, current) : current;

        // Native happiness is integer division of health + wellbeing by two.
        public static void ProtectHappiness(int floor, ref byte health, ref byte wellbeing)
        {
            int missing = Math.Max(0, floor * 2 - health - wellbeing);
            int h = Math.Min(100 - health, (missing + 1) / 2);
            int w = Math.Min(100 - wellbeing, missing - h);
            health = (byte)Math.Min(100, health + missing - w);
            wellbeing += (byte)w;
        }
    }
}
