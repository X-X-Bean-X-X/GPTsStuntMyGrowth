using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace StuntYourGrowth
{
    [FileLocation(Runtime.Id)]
    [SettingsUIGroupOrder(Settings.VacancyGroup, Settings.ModeGroup, Settings.BulkGroup, Settings.CheckboxGroup)]
    public sealed class Settings : ModSetting
    {
        internal const string VacancyGroup = "VacancyControls";
        internal const string ModeGroup = "Modes";
        internal const string BulkGroup = "BulkActions";
        internal const string CheckboxGroup = "Checkboxes";

        public Settings(IMod mod) : base(mod) { SetDefaults(); }

        [SettingsUISection(VacancyGroup)] public bool Vacancy { get; set; }
        [SettingsUIButton, SettingsUISection(VacancyGroup)]
        public bool ResetVacancy { set { if (value && Enabled) Runtime.RequestReset(true); } }
        [SettingsUIButton, SettingsUISection(VacancyGroup)]
        public bool ResetAbandonment { set { if (value && Enabled) Runtime.RequestReset(); } }

        [SettingsUISection(ModeGroup)] public ValueMode Staff { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Customers { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Employees { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Jobs { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Income { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode LandValue { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Profit { get; set; }
        [SettingsUISection(ModeGroup)] public SchoolMode Schooling { get; set; }

        [SettingsUIButton, SettingsUIButtonGroup("CheckboxSelection"), SettingsUISection(BulkGroup)]
        public bool EnableAllCheckboxes { set { if (value) ApplyCheckboxSelection(true); } }
        [SettingsUIButton, SettingsUIButtonGroup("CheckboxSelection"), SettingsUISection(BulkGroup)]
        public bool DisableAllCheckboxes { set { if (value) ApplyCheckboxSelection(false); } }

        [SettingsUISection(CheckboxGroup)] public bool Enabled { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Growth { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Abandonment { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Age { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentAmount { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Levels { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Redevelopment { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UpkeepCosts { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Garbage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentCollection { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Condition { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Attractiveness { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Comfort { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Efficiency { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Happiness { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Health { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Lifespan { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool ParkMaintenance { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Residents { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RoadWear { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Tenants { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Wellbeing { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool LegacyHold { get; set; }

        private void ApplyCheckboxSelection(bool enabled)
        {
            SetCheckboxes(enabled);
            ApplyAndSave();
        }

        private void SetCheckboxes(bool enabled)
        {
            Vacancy = enabled;
            Enabled = enabled;
            LegacyHold = enabled;
            Growth = enabled;
            Residents = enabled;
            Age = enabled;
            Lifespan = enabled;
            Health = enabled;
            Wellbeing = enabled;
            Happiness = enabled;
            Tenants = enabled;
            Condition = enabled;
            Levels = enabled;
            Redevelopment = enabled;
            Abandonment = enabled;
            Comfort = enabled;
            Attractiveness = enabled;
            Efficiency = enabled;
            ParkMaintenance = enabled;
            RoadWear = enabled;
            UpkeepCosts = enabled;
            Garbage = enabled;
            RentAmount = enabled;
            RentCollection = enabled;
        }

        public override void SetDefaults()
        {
            SetCheckboxes(false);
            Enabled = true;
            LegacyHold = true;
            Schooling = SchoolMode.Freeze;
            LandValue = ValueMode.Freeze;
            Staff = ValueMode.Freeze;
            Jobs = ValueMode.Freeze;
            Employees = ValueMode.Freeze;
            Customers = ValueMode.Freeze;
            Income = ValueMode.Freeze;
            Profit = ValueMode.Freeze;
        }
    }

    internal sealed class Locale : IDictionarySource
    {
        private readonly Settings s;
        public Locale(Settings settings) { s = settings; }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> counts)
        {
            var labels = new Dictionary<string, string>();
            labels[s.GetSettingsLocaleID()] = Runtime.OptionsTitle;
            labels[s.GetOptionGroupLocaleID(Settings.VacancyGroup)] = "Vacancy And Abandonment";
            labels[s.GetOptionGroupLocaleID(Settings.ModeGroup)] = "Dropdown Selections";
            labels[s.GetOptionGroupLocaleID(Settings.BulkGroup)] = "Checkbox Actions";
            labels[s.GetOptionGroupLocaleID(Settings.CheckboxGroup)] = "Checkbox Selections";
            void Add(string name, string title, string description)
            {
                labels[s.GetOptionLabelLocaleID(name)] = title;
                labels[s.GetOptionDescLocaleID(name)] = description;
            }
            Add(nameof(Settings.EnableAllCheckboxes), "Enable All Checkboxes", "Check Every Toggle, Including Vacancy Protection And The City-Specific Hold. Keeps The Selected Dropdown Modes.");
            Add(nameof(Settings.DisableAllCheckboxes), "Disable All Checkboxes", "Uncheck Every Toggle, Including Enable Simulation Controls. Dropdown Selections Are Kept And Resume When Simulation Controls Are Enabled.");
            Add(nameof(Settings.Enabled), "Enable Simulation Controls", "Apply These Controls During Gameplay.");
            Add(nameof(Settings.LegacyHold), "Respect The Original City-Specific Hold", "Read The Existing Chicago Population Hold Settings For Its Named City.");
            Add(nameof(Settings.Growth), "Stop Population Growth", "Pause Household Immigration And Births.");
            Add(nameof(Settings.Residents), "Protect Residents", "Pause Household Departures And Death Checks While Protected.");
            Add(nameof(Settings.Age), "Freeze Citizen Age", "Hold Age In Days And Life-Stage Transitions.");
            Add(nameof(Settings.Lifespan), "Protect Lifespan", "Suppress Old-Age Death Rolls. Illness And Injuries Retain Their Normal Checks.");
            Add(nameof(Settings.Health), "Protect Health", "Prevent Decreases In Resident Health.");
            Add(nameof(Settings.Wellbeing), "Protect Well-Being", "Prevent Decreases In Resident Well-Being.");
            Add(nameof(Settings.Happiness), "Protect Happiness", "Protect The Average Of Resident Health And Well-Being.");
            Add(nameof(Settings.Schooling), "Schooling", "Choose How Admissions And Graduation Checks Operate.");
            Add(nameof(Settings.Tenants), "Protect Tenants", "Protect Occupied Properties Against Relocation And Abandonment.");
            Add(nameof(Settings.Condition), "Prevent Deterioration", "Prevent Loss Of Condition From Unpaid Upkeep.");
            Add(nameof(Settings.Levels), "Hold Automatic Level Changes", "Pause Automatic Transitions To A New Building Level.");
            Add(nameof(Settings.Redevelopment), "Hold Automatic Redevelopment", "Suspend Automatic Zoned Construction On Vacant Lots.");
            Add(nameof(Settings.Abandonment), "Freeze Abandonment", "Hold The Condition-Based Abandonment Decision And Pause The Collapse Clock For Abandoned Buildings.");
            Add(nameof(Settings.Comfort), "Protect Comfort", "Protect The Native Comfort Factor At Stops, Stations And Parking Facilities.");
            Add(nameof(Settings.Attractiveness), "Protect Attractiveness", "Prevent A Building's Attractiveness From Decreasing.");
            Add(nameof(Settings.Efficiency), "Protect Efficiency", "Hold Building Efficiency Against Decreases.");
            Add(nameof(Settings.LandValue), "Land Value", "Hold Native Road-Edge Values And The Land-Value Map.");
            Add(nameof(Settings.ParkMaintenance), "Protect Park Maintenance", "Keep Each Park At Its Current Maintenance Level Or Higher. Repairs Can Still Improve It.");
            Add(nameof(Settings.RoadWear), "Protect Road Condition", "Prevent Wear From Increasing On Road Segments And Car Lanes. Maintenance Vehicles Can Still Repair Them.");
            Add(nameof(Settings.UpkeepCosts), "Pause City Upkeep Costs", "Waive Service Upkeep And Map Tile Upkeep At The Next City Budget Calculation. Other Expenses And Maintenance Services Continue.");
            Add(nameof(Settings.Garbage), "Pause New Garbage", "Hold New Building Garbage Generation While Collection Continues.");
            Add(nameof(Settings.Staff), "City-Service Staffing", "Protect Worker Assignments At Service Buildings.");
            Add(nameof(Settings.RentAmount), "Freeze Rent Amount", "Keep Existing Rental Prices. New Leases Begin At Their Native Initial Price.");
            Add(nameof(Settings.RentCollection), "Pause Rent Collection", "Pause The Periodic Rent Deduction While Tenant Housekeeping Continues.");
            Add(nameof(Settings.Jobs), "Employment Levels", "Protect The Number Of Jobs Offered By Employers.");
            Add(nameof(Settings.Employees), "Employee Count", "Protect Actual Worker Assignments At Workplaces.");
            Add(nameof(Settings.Customers), "Daily Customers", "Protect The Company's Reported Daily Customer Count.");
            Add(nameof(Settings.Income), "Income", "Protect Company Income And Household Income Statistics.");
            Add(nameof(Settings.Profit), "Profit", "Protect The Company's Reported Profit.");
            Add(nameof(Settings.Vacancy), "Protect Vacant Buildings", "Prevent Condition-Triggered Abandonment On Empty Properties And Freeze Their Existing Abandonment-To-Collapse Timer.");
            Add(nameof(Settings.ResetVacancy), "Reset Vacant Buildings Once", "Clear Negative Condition And Restart Any Abandonment-To-Collapse Timer On Empty Properties Only.");
            Add(nameof(Settings.ResetAbandonment), "Reset Abandonment Once", "Clear Negative Condition On Occupied Or Vacant Buildings And Restart The Delay On Already-Abandoned Buildings.");
            labels[s.GetEnumValueLocaleID(ValueMode.Normal)] = "Normal";
            labels[s.GetEnumValueLocaleID(ValueMode.Freeze)] = "Freeze";
            labels[s.GetEnumValueLocaleID(ValueMode.PreventDecreases)] = "Prevent Decreases";
            labels[s.GetEnumValueLocaleID(SchoolMode.Normal)] = "Normal Schooling";
            labels[s.GetEnumValueLocaleID(SchoolMode.Freeze)] = "Freeze Schooling As Is";
            labels[s.GetEnumValueLocaleID(SchoolMode.EnrollmentOnly)] = "Enrollment Only";
            labels[s.GetEnumValueLocaleID(SchoolMode.NoAdmissions)] = "Normal Schooling | No Admissions";
            return labels;
        }
        public void Unload() { }
    }
}
