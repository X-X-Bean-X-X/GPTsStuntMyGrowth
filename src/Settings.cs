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
        internal const string TargetGroup = "MaximizeTargets";
        internal const string BulkGroup = "BulkActions";
        internal const string CheckboxGroup = "Checkboxes";

        [SettingsUIHidden] public int DefaultsVersion { get; set; }

        public GPTStuntGrowthSettings(IMod mod) : base(mod) { SetDefaults(); }

        [SettingsUISection(MasterGroup)] public bool Enabled { get; set; }

        private CityGrowthMode cityGrowth;
        private bool industrialOutput, civicOutput;
        [SettingsUISection(ModeGroup)]
        public CityGrowthMode CityGrowth
        {
            get => cityGrowth;
            set
            {
                cityGrowth = (uint)(int)value <= 3u ? value : CityGrowthMode.Freeze;
                if (cityGrowth == CityGrowthMode.Freeze) industrialOutput = civicOutput = false;
            }
        }
        [SettingsUISection(ModeGroup)] public CustomerMode Customers { get; set; }
        [SettingsUISection(ModeGroup), SettingsUISlider(min=1, max=100000, step=1), SettingsUIDisableByCondition(typeof(GPTStuntGrowthSettings), nameof(CustomerTargetDisabled))]
        public int CustomerTarget { get; set; }
        [SettingsUISection(ModeGroup)] public EmploymentMode Jobs { get; set; }
        [SettingsUISection(ModeGroup)] public LandValueMode LandValue { get; set; }
        [SettingsUISection(ModeGroup), SettingsUISlider(min=1, max=10000, step=1), SettingsUIDisableByCondition(typeof(GPTStuntGrowthSettings), nameof(LandValueTargetDisabled))]
        public int LandValueTarget { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Profit { get; set; }
        [SettingsUISection(ModeGroup), SettingsUISlider(min=1, max=1000000, step=100), SettingsUIDisableByCondition(typeof(GPTStuntGrowthSettings), nameof(ProfitTargetDisabled))]
        public int ProfitTarget { get; set; }
        [SettingsUISection(ModeGroup)] public SchoolMode Schooling { get; set; }

        public bool CustomerTargetDisabled() => !Enabled || Customers != CustomerMode.Maximize;
        public bool LandValueTargetDisabled() => !Enabled || LandValue != LandValueMode.Maximize;
        public bool ProfitTargetDisabled() => !Enabled || Profit != ValueMode.Maximize;
        public bool OutputControlsDisabled() => CityGrowth == CityGrowthMode.Freeze;
        internal bool FreezeGrowth => CityGrowth == CityGrowthMode.Freeze;
        internal bool GrowPopulation => CityGrowth == CityGrowthMode.Population || CityGrowth == CityGrowthMode.PopulationAndOccupancy;
        internal bool FillCommercial => CityGrowth == CityGrowthMode.PopulationAndOccupancy || CityGrowth == CityGrowthMode.Occupancy;
        internal bool ShoppingActivity => Customers == CustomerMode.Shopping;
        internal ValueMode CustomerControl => (uint)(int)Customers <= 4u ? (ValueMode)(int)Customers : ValueMode.Normal;
        internal ValueMode JobControl => Jobs == EmploymentMode.BoostOutsideWorkers ? ValueMode.Normal : (ValueMode)(int)Jobs;
        internal ValueMode LandControl => LandValue == LandValueMode.ZeroFreezeRent ? ValueMode.Zero : (ValueMode)(int)LandValue;
        internal bool RecruitOutsideWorkers => Jobs == EmploymentMode.BoostOutsideWorkers || MaxIndustrialOutput || MaxCivicOutput;
        internal bool FreezeRent => LandValue == LandValueMode.ZeroFreezeRent;

        [SettingsUIButton, SettingsUIButtonGroup("CheckboxSelection"), SettingsUISection(BulkGroup)]
        public bool EnableAllCheckboxes { set { if (value) ApplyCheckboxSelection(true); } }
        [SettingsUIButton, SettingsUIButtonGroup("CheckboxSelection"), SettingsUISection(BulkGroup)]
        public bool DisableAllCheckboxes { set { if (value) ApplyCheckboxSelection(false); } }

        [SettingsUIHidden] public bool Growth { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RemoveDead { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumCommunications { get; set; }
        [SettingsUIHidden] public bool BoostOutsideWorkers { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Age { get; set; }
        [SettingsUIHidden] public bool RentAmount { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Levels { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Redevelopment { get; set; }
        [SettingsUISection(CheckboxGroup), SettingsUIDisableByCondition(typeof(GPTStuntGrowthSettings), nameof(OutputControlsDisabled))]
        public bool MaxIndustrialOutput { get => !FreezeGrowth && industrialOutput; set => industrialOutput = !FreezeGrowth && value; }
        [SettingsUISection(CheckboxGroup), SettingsUIDisableByCondition(typeof(GPTStuntGrowthSettings), nameof(OutputControlsDisabled))]
        public bool MaxCivicOutput { get => !FreezeGrowth && civicOutput; set => civicOutput = !FreezeGrowth && value; }
        [SettingsUIHidden] public bool MaximumShopping { get; set; }
        [SettingsUIHidden] public bool MaximumOccupancy { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumFireCoverage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumPoliceCoverage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumResources { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumStorage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UpkeepCosts { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Garbage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentCollection { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool PreferPublicTransportation { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Condition { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Attractiveness { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Comfort { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Happiness { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Health { get; set; }
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
            PreferPublicTransportation = enabled;
            MaximumResources = MaximumStorage = enabled;
            MaximumShopping = false;
            MaxIndustrialOutput = MaxCivicOutput = enabled;
            MaximumCommunications = MaximumFireCoverage = MaximumPoliceCoverage = enabled;
            MaximumOccupancy = false;
            Growth = enabled;
            Residents = enabled;
            RemoveDead = enabled;
            BoostOutsideWorkers = false; // Retired checkbox, migration input only.
            Age = enabled;
            Health = enabled;
            Wellbeing = enabled;
            Happiness = enabled;
            Tenants = enabled;
            Condition = enabled;
            Levels = enabled;
            Redevelopment = enabled;
            Comfort = enabled;
            Attractiveness = enabled;
            ParkMaintenance = enabled;
            RoadWear = enabled;
            UpkeepCosts = enabled;
            Garbage = enabled;
            RentAmount = false; // Rent is controlled exclusively by the land-value selection.
            RentCollection = enabled;
        }

        // Preserve existing serialized property names; only first-load/reset defaults change.
        private void SetAllOptions(bool enabled)
        {
            CityGrowth = CityGrowthMode.Freeze;
            SetCheckboxes(enabled);
            Schooling = enabled ? SchoolMode.Freeze : SchoolMode.Normal;
            Customers = enabled ? CustomerMode.Freeze : CustomerMode.Normal;
            Profit = enabled ? ValueMode.Freeze : ValueMode.Normal;
            Jobs = enabled ? EmploymentMode.Freeze : EmploymentMode.Normal;
            LandValue = enabled ? LandValueMode.Freeze : LandValueMode.Normal;
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
            CustomerTarget = 1000;
            ProfitTarget = 10000;
            LandValueTarget = 100;
            DefaultsVersion = 7;
        }

        internal void PrepareUpgrade(int savedVersion)
        {
            if (savedVersion == 0) UseLegacyDefaults();
            if (savedVersion == 6) MaximumOccupancy = MaximumShopping = true;
            if (savedVersion < 7) CityGrowth = CityGrowthMode.Freeze;
            if (savedVersion < 6) MaximumCommunications = MaximumFireCoverage = MaximumPoliceCoverage = MaximumOccupancy = false;
            if (savedVersion < 6) MaximumShopping = false;
            // Older files omit values equal to their former defaults.
            if (savedVersion > 0 && savedVersion < 6) RentAmount = true;
            if (savedVersion == 5) BoostOutsideWorkers = true;
            if (savedVersion < 4) RemoveDead = false;
            if (savedVersion < 5) BoostOutsideWorkers = false;
            if (savedVersion < 2) PreferPublicTransportation = MaximumResources = MaximumStorage = false;
        }

        internal void FinishUpgrade(int savedVersion)
        {
            if (savedVersion < 6)
            {
                // Explicit Zero wins over the retired independent recruitment flag.
                if (BoostOutsideWorkers && (int)Jobs != 4) Jobs = EmploymentMode.BoostOutsideWorkers;
                if (RentAmount && LandValue == LandValueMode.Zero) LandValue = LandValueMode.ZeroFreezeRent;
            }
            if (savedVersion < 7)
            {
                CityGrowth = MaximumOccupancy ? (Growth ? CityGrowthMode.Occupancy : CityGrowthMode.PopulationAndOccupancy) : CityGrowthMode.Freeze;
                if (MaximumShopping) Customers = CustomerMode.Shopping;
            }
            if (!Enum.IsDefined(typeof(CustomerMode), Customers)) Customers = CustomerMode.Normal;
            if (FreezeGrowth) industrialOutput = civicOutput = false;
            MaximumOccupancy = MaximumShopping = false;
            if (!Enum.IsDefined(typeof(EmploymentMode), Jobs)) Jobs = EmploymentMode.Normal;
            if (!Enum.IsDefined(typeof(LandValueMode), LandValue)) LandValue = LandValueMode.Normal;
            BoostOutsideWorkers = RentAmount = false;
            DefaultsVersion = 7;
        }

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
            labels[s.GetOptionGroupLocaleID(GPTStuntGrowthSettings.TargetGroup)] = "Maximize Targets";
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
            Add(nameof(GPTStuntGrowthSettings.RemoveDead), "Remove the Dead & Decaying", "Automatically Remove Deceased Citizens Awaiting Pickup In Small Batches Through Native Citizen Cleanup. Living Citizens Are Untouched. Active Pickups And Citizens In Vehicles Or Deathcare Finish Normally. Does Not Empty Cemeteries Or Transfer Bodies To Another City. Removed Bodies Cannot Be Restored By Turning This Off.");
            Add(nameof(GPTStuntGrowthSettings.BoostOutsideWorkers), "Boost Outside Worker Recruitment", "Recruit Extra Adult Commuters In Small Batches When Vacancies Exceed The Available Workforce. Keeps Native Commuter Limits, Education And Job Matching; Does Not Enable Resident Immigration. Requires Usable Outside Connections. Existing Commuters Remain When Disabled.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedElectricity), "Unlimited Electricity", "Satisfy Building Electricity Demand Without Supply Or Network Limits. Clears Electricity Shortage Penalties. Does Not Add Power Plants Or Change Their Reported Capacity.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedWater), "Unlimited Water", "Satisfy Building Fresh Water Demand With Clean Water Without Supply Or Pipe Limits. Sewage Is Controlled Separately.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedSewer), "Unlimited Sewer", "Satisfy Building Sewage Disposal Demand Without Outlet Or Pipe Limits. Fresh Water Is Controlled Separately.");
            Add(nameof(GPTStuntGrowthSettings.Growth), "Stop Population Growth", "Pause Household Immigration And Births. Maximum Building Occupancy Takes Priority For Move-Ins While Enabled; Births Remain Paused.");
            Add(nameof(GPTStuntGrowthSettings.MaximumOccupancy), "Maximum Building Occupancy", "Gradually Fill Eligible Empty Homes And Commercial, Industrial, Office And Warehouse Spaces With Real Native Households And Businesses. Overrides Stop Population Growth For Move-Ins. Uses Native Placement, Access And Capacity Rules; Does Not Fill Extractor Sites, Service Seats Or Hotel Guest Rooms. Pauses Recruitment When The Placement Backlog Is Full. Disabling Does Not Remove New Tenants.");
            Add(nameof(GPTStuntGrowthSettings.MaximumPoliceCoverage), "Maximum Police Coverage", "Provide The Strongest Loaded Prefab-Defined Police Coverage Across Road-Connected Areas, Without Enlarging Coverage Searches. Does Not Add Police Crews, Vehicles Or Reachability. Normal Coverage Recalculates After Disabling.");
            Add(nameof(GPTStuntGrowthSettings.MaximumFireCoverage), "Maximum Fire Coverage", "Provide The Strongest Loaded Prefab-Defined Fire Coverage Across Road-Connected Areas, Without Enlarging Coverage Searches. Does Not Extinguish Fires Or Add Fire Engines. Normal Coverage Recalculates After Disabling.");
            Add(nameof(GPTStuntGrowthSettings.MaximumCommunications), "Communications & Internet", "Provide Maximum Native Sampled Communications Quality Across The Map. Does Not Add Towers Or Change Their Reported Capacity. Normal Signal And Network Load Recalculate After Disabling.");
            Add(nameof(GPTStuntGrowthSettings.MaximumResources), "Maximum Resources Available", "Replenish Business Inputs, Shop Goods And City-Service Supplies Within Native Inventory Limits. Prevent Extraction From Depleting Existing Oil, Ore, Fertility And Fish Deposits. Money Is Unchanged; Does Not Create New Deposits Or Trees.");
            Add(nameof(GPTStuntGrowthSettings.MaximumStorage), "Maximum Storage", "Refill Warehouses And Cargo Storage With Their Accepted Goods Up To Their Existing Shared Capacity. Does Not Increase Capacity. Stored Goods Remain When Disabled And Resume Normal Use.");
            Add(nameof(GPTStuntGrowthSettings.CityGrowth), "City Growth", "Freeze Pauses Immigration And Births And Unchecks The Industrial/Civic Output Controls. Population Maximizes Bounded Household Recruitment And Allows Native Births. Population And Occupancy Also Fills Commercial Premises. Occupancy Fills Existing Homes And Commercial Premises With Newcomers But Pauses Births. Native Access, Housing Capacity And Placement Backlogs Still Apply. Turn Enable Mod Off For Unmodified Growth.");
            Add(nameof(GPTStuntGrowthSettings.MaxIndustrialOutput), "Maximize Occupancy & Production Output of Industrials Buildings", "Target Industrial Buildings' Normal Designed Staffing Capacity, Recruit Real Workers And Fill Eligible Vacant Industrial Premises. Production Uses Real Workers, Inputs, Storage And Native Processing. Does Not Invent Employees, Supplies Or Output; Staffing And Deliveries Can Delay Full Production. Uses Normal Designed Capacity, Not An Extra Arbitrary Production Cap. Separate Inventory And Utility Options Retain Their Own Effects. Takes Priority Over Employment Freeze For Targeted Workplaces. Freeze Population Growth Unchecks And Disables This Control. Choose Any Other City Growth Mode To Make It Selectable, Even With Enable Mod Off. Enable Mod Must Be On For Its Simulation Effects. Frozen schooling can limit qualified workers. Recruitment cannot guarantee enough workers with the required education.");
            Add(nameof(GPTStuntGrowthSettings.MaxCivicOutput), "Maximize Occupancy & Services Output of Civic Buildings", "Target Civic Buildings' Native Staffing Capacity, Including Upgrades And Native School Enrollment Scaling, And Recruit Real Workers. Students, Patients And Detainees Stay Normal. Services Still Require Their Real Budget, Utilities, Supplies And Access. Does Not Create Staff Or Bypass Shortages. Separate Inventory And Utility Options Retain Their Own Effects. Takes Priority Over Employment Freeze For Targeted Workplaces. Freeze Population Growth Unchecks And Disables This Control. Choose Any Other City Growth Mode To Make It Selectable, Even With Enable Mod Off. Enable Mod Must Be On For Its Simulation Effects. Frozen schooling can limit qualified workers. Recruitment cannot guarantee enough workers with the required education.");
            Add(nameof(GPTStuntGrowthSettings.MaximumShopping), "Maximize Shopping Activity", "Use The Native Maximum Shopping Chance Without Its Population-Based Traffic Reduction. Keeps Native Spending Limits, Existing Shopping Needs, Repeat-Purchase Reduction, Work/School/Sleep Priorities, Routing, Goods And Real Payments. Does Not Spawn Shoppers Or Money Or Guarantee Sales. Can Increase Shopping Traffic; Statistic Overrides Can Hide The Result.");
            Add(nameof(GPTStuntGrowthSettings.PreferPublicTransportation), "Prefer Public Transportation", "Try Walking, Cycling And Available Public Transportation Before Driving Or Taxis For New Citizen Routes. If No Route Is Found, Retry With Normal Travel Choices. Existing Trips Continue.");
            Add(nameof(GPTStuntGrowthSettings.Residents), "Protect Residents", "Pause Household Departures And Death Checks While Protected.");
            Add(nameof(GPTStuntGrowthSettings.Age), "Freeze Citizen Age", "Hold Age In Days And Life-Stage Transitions.");
            Add(nameof(GPTStuntGrowthSettings.Health), "Protect Health", "Prevent Decreases In Resident Health.");
            Add(nameof(GPTStuntGrowthSettings.Wellbeing), "Protect Well-Being", "Prevent Decreases In Resident Well-Being.");
            Add(nameof(GPTStuntGrowthSettings.Happiness), "Protect Happiness", "Protect The Average Of Resident Health And Well-Being.");
            Add(nameof(GPTStuntGrowthSettings.Schooling), "Schooling", "Choose How Admissions And Graduation Checks Operate. Frozen schooling can limit qualified workers. Recruitment cannot guarantee enough workers with the required education.");
            Add(nameof(GPTStuntGrowthSettings.Tenants), "Protect Tenants", "Protect Occupied Properties Against Relocation And Abandonment.");
            Add(nameof(GPTStuntGrowthSettings.Condition), "Prevent Deterioration", "Prevent Loss Of Condition From Unpaid Upkeep.");
            Add(nameof(GPTStuntGrowthSettings.Levels), "Hold Automatic Level Changes", "Pause Automatic Transitions To A New Building Level.");
            Add(nameof(GPTStuntGrowthSettings.Redevelopment), "Hold Automatic Redevelopment", "Suspend Automatic Zoned Construction On Vacant Lots.");
            Add(nameof(GPTStuntGrowthSettings.Comfort), "Protect Comfort", "Protect The Native Comfort Factor At Stops, Stations And Parking Facilities.");
            Add(nameof(GPTStuntGrowthSettings.Attractiveness), "Protect Attractiveness", "Prevent A Building's Attractiveness From Decreasing.");
            Add(nameof(GPTStuntGrowthSettings.LandValue), "Land Value", "Hold Native Road-Edge Values And The Land-Value Map. Maximize Uses The Target Below. Reset to ZERO & Freeze Rent Also Holds Existing Lease Prices; New Leases Start At Their Native Price. Other Modes Do Not Freeze Rent.");
            Add(nameof(GPTStuntGrowthSettings.ParkMaintenance), "Protect Park Maintenance", "Keep Each Park At Its Current Maintenance Level Or Higher. Repairs Can Still Improve It.");
            Add(nameof(GPTStuntGrowthSettings.RoadWear), "Protect Road Condition", "Prevent Wear From Increasing On Road Segments And Car Lanes. Maintenance Vehicles Can Still Repair Them.");
            Add(nameof(GPTStuntGrowthSettings.UpkeepCosts), "Pause City Upkeep Costs", "Waive Service Upkeep And Map Tile Upkeep At The Next City Budget Calculation. Other Expenses And Maintenance Services Continue.");
            Add(nameof(GPTStuntGrowthSettings.Garbage), "Pause New Garbage", "Hold New Building Garbage Generation While Collection Continues.");
            Add(nameof(GPTStuntGrowthSettings.RentAmount), "Freeze Rent Amount", "Keep Existing Rental Prices. New Leases Begin At Their Native Initial Price.");
            Add(nameof(GPTStuntGrowthSettings.RentCollection), "Pause Rent Collection", "Pause The Periodic Rent Deduction While Tenant Housekeeping Continues.");
            Add(nameof(GPTStuntGrowthSettings.Jobs), "Employment Levels", "Normal Lets Native Job Capacity Change. Freeze Holds Captured Capacity; Prevent Decreases Allows Increases. Boost Outside Worker Recruitment Recruits Qualified Commuters Through Native Matching Without Holding Job Capacity. Maximize And Reset to ZERO Are Removed. Frozen schooling can limit qualified workers. Recruitment cannot guarantee enough workers with the required education.");
            Add(nameof(GPTStuntGrowthSettings.Customers), "Daily Customers", "Choose Normal Customer Statistics, Freeze, Prevent Decreases, A Maximize Statistic Target, Reset to ZERO, Or Maximize Shopping Activity. Shopping Activity Uses Real Native Purchases And Leaves The Customer Counter Unheld. It Cannot Run With A Customer Statistic Hold. Profit Controls Remain Independent And Can Mask Reported Results. Income Follows The Native Simulation.");
            Add(nameof(GPTStuntGrowthSettings.Profit), "Profit", "Protect The Company's Reported Profit. Maximize Uses The Adjustable Target Below. Reset to ZERO Holds Zero Until You Select Another Mode.");
            Add(nameof(GPTStuntGrowthSettings.Vacancy), "Protect Vacant Buildings", "Prevent Condition-Triggered Abandonment On Empty Properties And Freeze Their Existing Abandonment-To-Collapse Timer.");
            Add(nameof(GPTStuntGrowthSettings.ResetVacancy), "Reset Vacant Buildings", "Clear Negative Condition And Restart Any Abandonment-To-Collapse Timer On Empty Properties Only. Runs Once Per Click; You Can Use This Button Again Whenever Needed.");
            Add(nameof(GPTStuntGrowthSettings.ResetAbandonment), "Reset Abandonment", "Clear Negative Condition On Occupied Or Vacant Buildings And Restart The Delay On Already-Abandoned Buildings. Runs Once Per Click; You Can Use This Button Again Whenever Needed.");
            Add(nameof(GPTStuntGrowthSettings.CustomerTarget), "Daily Customers Target", "Used Only By Maximize. Holds Each Company's Reported Customer Statistic At This Target; Does Not Generate Trips Or Sales.");
            Add(nameof(GPTStuntGrowthSettings.LandValueTarget), "Land Value Target", "Used Only By Maximize. Sets Road-Edge Values And The Land-Value Map To This Target. A Mod Target, Not A Game-Defined Maximum.");
            Add(nameof(GPTStuntGrowthSettings.ProfitTarget), "Profit Target", "Used Only By Maximize. Holds Company Profit Statistics At This Target; Does Not Deposit Money.");
            labels[s.GetEnumValueLocaleID(CityGrowthMode.Freeze)] = "Freeze Population Growth";
            labels[s.GetEnumValueLocaleID(CityGrowthMode.Population)] = "Maximize Population Growth";
            labels[s.GetEnumValueLocaleID(CityGrowthMode.PopulationAndOccupancy)] = "Maximize Population Growth and Occupancy of All Commercial & Residential Buildings";
            labels[s.GetEnumValueLocaleID(CityGrowthMode.Occupancy)] = "Maximize Occupancy of All Commercial & Residential Buildings";
            foreach (CustomerMode mode in Enum.GetValues(typeof(CustomerMode)))
                labels[s.GetEnumValueLocaleID(mode)] = mode == CustomerMode.Shopping ? "Maximize Shopping Activity" : mode == CustomerMode.Zero ? "Reset to ZERO" : mode == CustomerMode.PreventDecreases ? "Prevent Decreases" : mode.ToString();
            foreach (EmploymentMode mode in Enum.GetValues(typeof(EmploymentMode)))
                labels[s.GetEnumValueLocaleID(mode)] = mode == EmploymentMode.BoostOutsideWorkers ? "Boost Outside Worker Recruitment" : mode == EmploymentMode.PreventDecreases ? "Prevent Decreases" : mode.ToString();
            foreach (LandValueMode mode in Enum.GetValues(typeof(LandValueMode)))
                labels[s.GetEnumValueLocaleID(mode)] = mode == LandValueMode.ZeroFreezeRent ? "Reset to ZERO & Freeze Rent" : mode == LandValueMode.Zero ? "Reset to ZERO" : mode == LandValueMode.PreventDecreases ? "Prevent Decreases" : mode.ToString();
            labels[s.GetEnumValueLocaleID(ValueMode.Maximize)] = "Maximize";
            labels[s.GetEnumValueLocaleID(ValueMode.Zero)] = "Reset to ZERO";
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
