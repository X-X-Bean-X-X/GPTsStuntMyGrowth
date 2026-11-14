using System;

namespace StuntYourGrowth
{
    public enum ValueMode { Normal = 0, Freeze = 1, PreventDecreases = 2, Maximize = 3, Zero = 4 }
    // Preserve serialized ordinals; 3 was the retired employment Maximize mode.
    public enum EmploymentMode { Normal = 0, Freeze = 1, PreventDecreases = 2, BoostOutsideWorkers = 5 }
    public enum CustomerMode { Normal = 0, Freeze = 1, PreventDecreases = 2, Maximize = 3, Zero = 4, Shopping = 5 }
    public enum CityGrowthMode { Freeze = 0, Population = 1, PopulationAndOccupancy = 2, Occupancy = 3 }
    public enum LandValueMode { Normal = 0, Freeze = 1, PreventDecreases = 2, Maximize = 3, Zero = 4, ZeroFreezeRent = 5 }
    public enum SchoolMode { Normal, Freeze, EnrollmentOnly, NoAdmissions }

    internal static class Policy
    {
        public static bool IsOverride(ValueMode mode) => mode == ValueMode.Maximize || mode == ValueMode.Zero;
        public static ValueMode BaselineMode(ValueMode mode) => IsOverride(mode) ? ValueMode.Normal : mode;
        public static int ModeKey(ValueMode customers, ValueMode income, ValueMode profit) =>
            (int)customers | ((int)income << 3) | ((int)profit << 6);
        public static int Target(int current, ValueMode mode, int target, int limit) =>
            mode == ValueMode.Zero ? 0 : mode == ValueMode.Maximize ? Math.Max(0, Math.Min(limit, target)) : current;

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
