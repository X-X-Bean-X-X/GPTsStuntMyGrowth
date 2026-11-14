using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace StuntYourGrowth
{
    [FileLocation(Runtime.Id)]
    [SettingsUIGroupOrder(GPTStuntGrowthSettings.MasterGroup, GPTStuntGrowthSettings.ModeGroup, GPTStuntGrowthSettings.BulkGroup, GPTStuntGrowthSettings.CheckboxGroup, GPTStuntGrowthSettings.VacancyGroup)]
    public sealed class GPTStuntGrowthSettings : ModSetting
    {
        internal const string MasterGroup = "MasterControls";
        internal const string VacancyGroup = "VacancyControls";
        internal const string ModeGroup = "Modes";
        internal const string BulkGroup = "BulkActions";
        internal const string CheckboxGroup = "Checkboxes";

        [SettingsUIHidden] public int DefaultsVersion { get; set; }

        public GPTStuntGrowthSettings(IMod mod) : base(mod) { SetDefaults(); }

        [SettingsUISection(MasterGroup)] public bool Enabled { get; set; }
        [SettingsUISection(MasterGroup)] public bool Growth { get; set; }

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

        [SettingsUISection(CheckboxGroup)] public bool Abandonment { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Age { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentAmount { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Levels { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Redevelopment { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumCustomers { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumResources { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumStorage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UpkeepCosts { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Garbage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentCollection { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool PreferPublicTransportation { get; set; }
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
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedElectricity { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedSewer { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedWater { get; set; }

        [SettingsUISection(VacancyGroup)] public bool Vacancy { get; set; }
        [SettingsUIButton, SettingsUISection(VacancyGroup)]
        public bool ResetVacancy { set { if (value && Enabled) Runtime.RequestReset(true); } }
        [SettingsUIButton, SettingsUISection(VacancyGroup)]
        public bool ResetAbandonment { set { if (value && Enabled) Runtime.RequestReset(); } }

        private void ApplyCheckboxSelection(bool enabled)
        {
            SetAllOptions(enabled);
            ApplyAndSave();
        }

        private void SetCheckboxes(bool enabled)
        {
            Vacancy = enabled;
            Enabled = enabled;
            UnlimitedElectricity = UnlimitedSewer = UnlimitedWater = enabled;
            MaximumCustomers = PreferPublicTransportation = enabled;
            MaximumResources = MaximumStorage = enabled;
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

        // Preserve existing serialized property names; only first-load/reset defaults change.
        private void SetAllOptions(bool enabled)
        {
            SetCheckboxes(enabled);
            Schooling = enabled ? SchoolMode.Freeze : SchoolMode.Normal;
            LandValue = Staff = Jobs = Employees = Customers = Income = Profit =
                enabled ? ValueMode.Freeze : ValueMode.Normal;
        }

        internal void UseLegacyDefaults()
        {
            SetAllOptions(true);
            SetCheckboxes(false);
            Enabled = true;
        }

        public override void SetDefaults()
        {
            SetAllOptions(true);
            DefaultsVersion = 2;
        }

        // Real shopping must be allowed to update the reported customer counters.
        internal ValueMode CustomerMode => MaximumCustomers ? ValueMode.Normal : Customers;

    }

    internal sealed class Locale : IDictionarySource
    {
        private readonly GPTStuntGrowthSettings s;
        public Locale(GPTStuntGrowthSettings settings) { s = settings; }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> counts)
        {
            var labels = new Dictionary<string, string>();
            labels[s.GetSettingsLocaleID()] = Runtime.OptionsTitle;
            labels[s.GetOptionGroupLocaleID(GPTStuntGrowthSettings.MasterGroup)] = "Mod Controls";
            labels[s.GetOptionGroupLocaleID(GPTStuntGrowthSettings.VacancyGroup)] = "Vacancy And Abandonment";
            labels[s.GetOptionGroupLocaleID(GPTStuntGrowthSettings.ModeGroup)] = "Dropdown Selections";
            labels[s.GetOptionGroupLocaleID(GPTStuntGrowthSettings.BulkGroup)] = "All Options";
            labels[s.GetOptionGroupLocaleID(GPTStuntGrowthSettings.CheckboxGroup)] = "Checkbox Selections";
            void Add(string name, string title, string description)
            {
                labels[s.GetOptionLabelLocaleID(name)] = title;
                labels[s.GetOptionDescLocaleID(name)] = description;
            }
            Add(nameof(GPTStuntGrowthSettings.EnableAllCheckboxes), "Enable All Options", "Enable Every Checkbox And Set Every Dropdown To Freeze. Includes Unlimited Utilities.");
            Add(nameof(GPTStuntGrowthSettings.DisableAllCheckboxes), "Disable All Options", "Disable Every Checkbox And Set Every Dropdown To Normal Simulation.");
            Add(nameof(GPTStuntGrowthSettings.Enabled), "Enable Mod", "Master Switch For All Simulation Controls. Turning This Off Preserves Your Individual Selections And Returns To Normal Simulation On Its Next Updates.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedElectricity), "Unlimited Electricity", "Satisfy Building Electricity Demand Without Supply Or Network Limits. Clears Electricity Shortage Penalties. Does Not Add Power Plants Or Change Their Reported Capacity.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedWater), "Unlimited Water", "Satisfy Building Fresh Water Demand With Clean Water Without Supply Or Pipe Limits. Sewage Is Controlled Separately.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedSewer), "Unlimited Sewer", "Satisfy Building Sewage Disposal Demand Without Outlet Or Pipe Limits. Fresh Water Is Controlled Separately.");
            Add(nameof(GPTStuntGrowthSettings.Growth), "Stop Population Growth", "Pause Household Immigration And Births.");
            Add(nameof(GPTStuntGrowthSettings.MaximumCustomers), "Maximum Customers", "Maximize Native Shopping Chances By Removing Population-Based Shopping Reductions And Supported Time2Work Trip Gates. Uses Real Shoppers, Normal Budgets And Travel; Does Not Guarantee Full Shops. Overrides Daily Customers Freeze While Enabled. Can Increase Traffic.");
            Add(nameof(GPTStuntGrowthSettings.MaximumResources), "Maximum Resources Available", "Replenish Business Inputs, Shop Goods And City-Service Supplies Within Native Inventory Limits. Prevent Extraction From Depleting Existing Oil, Ore, Fertility And Fish Deposits. Money Is Unchanged; Does Not Create New Deposits Or Trees.");
            Add(nameof(GPTStuntGrowthSettings.MaximumStorage), "Maximum Storage", "Refill Warehouses And Cargo Storage With Their Accepted Goods Up To Their Existing Shared Capacity. Does Not Increase Capacity. Stored Goods Remain When Disabled And Resume Normal Use.");
            Add(nameof(GPTStuntGrowthSettings.PreferPublicTransportation), "Prefer Public Transportation", "Try Walking, Cycling And Available Public Transportation Before Driving Or Taxis For New Citizen Routes. If No Route Is Found, Retry With Normal Travel Choices. Existing Trips Continue.");
            Add(nameof(GPTStuntGrowthSettings.Residents), "Protect Residents", "Pause Household Departures And Death Checks While Protected.");
            Add(nameof(GPTStuntGrowthSettings.Age), "Freeze Citizen Age", "Hold Age In Days And Life-Stage Transitions.");
            Add(nameof(GPTStuntGrowthSettings.Lifespan), "Protect Lifespan", "Suppress Old-Age Death Rolls. Illness And Injuries Retain Their Normal Checks.");
            Add(nameof(GPTStuntGrowthSettings.Health), "Protect Health", "Prevent Decreases In Resident Health.");
            Add(nameof(GPTStuntGrowthSettings.Wellbeing), "Protect Well-Being", "Prevent Decreases In Resident Well-Being.");
            Add(nameof(GPTStuntGrowthSettings.Happiness), "Protect Happiness", "Protect The Average Of Resident Health And Well-Being.");
            Add(nameof(GPTStuntGrowthSettings.Schooling), "Schooling", "Choose How Admissions And Graduation Checks Operate.");
            Add(nameof(GPTStuntGrowthSettings.Tenants), "Protect Tenants", "Protect Occupied Properties Against Relocation And Abandonment.");
            Add(nameof(GPTStuntGrowthSettings.Condition), "Prevent Deterioration", "Prevent Loss Of Condition From Unpaid Upkeep.");
            Add(nameof(GPTStuntGrowthSettings.Levels), "Hold Automatic Level Changes", "Pause Automatic Transitions To A New Building Level.");
            Add(nameof(GPTStuntGrowthSettings.Redevelopment), "Hold Automatic Redevelopment", "Suspend Automatic Zoned Construction On Vacant Lots.");
            Add(nameof(GPTStuntGrowthSettings.Abandonment), "Freeze Abandonment", "Hold The Condition-Based Abandonment Decision And Pause The Collapse Clock For Abandoned Buildings.");
            Add(nameof(GPTStuntGrowthSettings.Comfort), "Protect Comfort", "Protect The Native Comfort Factor At Stops, Stations And Parking Facilities.");
            Add(nameof(GPTStuntGrowthSettings.Attractiveness), "Protect Attractiveness", "Prevent A Building's Attractiveness From Decreasing.");
            Add(nameof(GPTStuntGrowthSettings.Efficiency), "Protect Efficiency", "Hold Building Efficiency Against Decreases.");
            Add(nameof(GPTStuntGrowthSettings.LandValue), "Land Value", "Hold Native Road-Edge Values And The Land-Value Map.");
            Add(nameof(GPTStuntGrowthSettings.ParkMaintenance), "Protect Park Maintenance", "Keep Each Park At Its Current Maintenance Level Or Higher. Repairs Can Still Improve It.");
            Add(nameof(GPTStuntGrowthSettings.RoadWear), "Protect Road Condition", "Prevent Wear From Increasing On Road Segments And Car Lanes. Maintenance Vehicles Can Still Repair Them.");
            Add(nameof(GPTStuntGrowthSettings.UpkeepCosts), "Pause City Upkeep Costs", "Waive Service Upkeep And Map Tile Upkeep At The Next City Budget Calculation. Other Expenses And Maintenance Services Continue.");
            Add(nameof(GPTStuntGrowthSettings.Garbage), "Pause New Garbage", "Hold New Building Garbage Generation While Collection Continues.");
            Add(nameof(GPTStuntGrowthSettings.Staff), "City-Service Staffing", "Protect Worker Assignments At Service Buildings.");
            Add(nameof(GPTStuntGrowthSettings.RentAmount), "Freeze Rent Amount", "Keep Existing Rental Prices. New Leases Begin At Their Native Initial Price.");
            Add(nameof(GPTStuntGrowthSettings.RentCollection), "Pause Rent Collection", "Pause The Periodic Rent Deduction While Tenant Housekeeping Continues.");
            Add(nameof(GPTStuntGrowthSettings.Jobs), "Employment Levels", "Protect The Number Of Jobs Offered By Employers.");
            Add(nameof(GPTStuntGrowthSettings.Employees), "Employee Count", "Protect Actual Worker Assignments At Workplaces.");
            Add(nameof(GPTStuntGrowthSettings.Customers), "Daily Customers", "Protect The Company's Reported Daily Customer Count. Maximum Customers Overrides This Selection While Enabled So Real Purchases Can Update The Count.");
            Add(nameof(GPTStuntGrowthSettings.Income), "Income", "Protect Company Income And Household Income Statistics.");
            Add(nameof(GPTStuntGrowthSettings.Profit), "Profit", "Protect The Company's Reported Profit.");
            Add(nameof(GPTStuntGrowthSettings.Vacancy), "Protect Vacant Buildings", "Prevent Condition-Triggered Abandonment On Empty Properties And Freeze Their Existing Abandonment-To-Collapse Timer.");
            Add(nameof(GPTStuntGrowthSettings.ResetVacancy), "Reset Vacant Buildings", "Clear Negative Condition And Restart Any Abandonment-To-Collapse Timer On Empty Properties Only. Runs Once Per Click; You Can Use This Button Again Whenever Needed.");
            Add(nameof(GPTStuntGrowthSettings.ResetAbandonment), "Reset Abandonment", "Clear Negative Condition On Occupied Or Vacant Buildings And Restart The Delay On Already-Abandoned Buildings. Runs Once Per Click; You Can Use This Button Again Whenever Needed.");
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
