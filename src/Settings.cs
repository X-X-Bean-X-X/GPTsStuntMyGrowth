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
            void Add(string name, string title, params string[] points)
            {
                labels[s.GetOptionLabelLocaleID(name)] = title;
                labels[s.GetOptionDescLocaleID(name)] = "\u2022 " + string.Join("\n\n\u2022 ", points);
            }
            Add(nameof(GPTStuntGrowthSettings.EnableAllCheckboxes), "Enable All Options",
                "Turn the mod on and set every dropdown to Freeze.",
                "Turn on the other checkboxes, including unlimited utilities.",
                "The two building output options stay off because population growth is frozen.",
                "Keep your target numbers. Do not run either reset button.");
            Add(nameof(GPTStuntGrowthSettings.DisableAllCheckboxes), "Disable All Options",
                "Turn the mod and all checkboxes off.",
                "Set dropdowns to Normal where that choice exists.",
                "City Growth still shows Freeze, but has no effect while the mod is off.",
                "Keep your target numbers. Do not run either reset button.");
            Add(nameof(GPTStuntGrowthSettings.Enabled), "Enable Mod",
                "On: use the settings you choose below.",
                "Off: stop this mod from changing the simulation.",
                "Your choices are saved. The game takes over as it updates.",
                "Turning this off does not undo changes already made.");
            Add(nameof(GPTStuntGrowthSettings.RemoveDead), "Remove the Dead & Decaying",
                "Remove dead citizens who are waiting to be picked up, a few at a time.",
                "Leave living citizens alone.",
                "Let current hearses and deathcare services finish their work.",
                "Do not empty cemeteries or send bodies to another city.",
                "Removed bodies do not come back when you turn this off.");
            Add(nameof(GPTStuntGrowthSettings.BoostOutsideWorkers), "Boost Outside Worker Recruitment",
                "Invite extra adult workers from other cities when jobs are open.",
                "They need an outside connection and a suitable job.",
                "This does not train workers or guarantee that jobs will be filled.",
                "Turning this off stops extra invitations. Existing commuters stay in the simulation.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedElectricity), "Unlimited Electricity",
                "Give buildings all the electricity they need.",
                "Ignore shortages in power supply and connections.",
                "Do not add power plants or raise their listed capacity.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedWater), "Unlimited Water",
                "Give buildings all the clean water they need.",
                "Ignore shortages in water supply and pipe connections.",
                "Sewage is handled by Unlimited Sewer.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedSewer), "Unlimited Sewer",
                "Handle all sewage produced by buildings.",
                "Ignore limits on sewage outlets and pipe connections.",
                "Clean water is handled by Unlimited Water.");
            Add(nameof(GPTStuntGrowthSettings.Growth), "Stop Population Growth",
                "This older checkbox has been replaced by City Growth.",
                "Use Freeze Population Growth to pause new residents and births.");
            Add(nameof(GPTStuntGrowthSettings.MaximumOccupancy), "Maximum Building Occupancy",
                "This older checkbox has been replaced by City Growth.",
                "Choose an occupancy mode to help fill empty homes and business spaces.");
            Add(nameof(GPTStuntGrowthSettings.MaximumPoliceCoverage), "Maximum Police Coverage",
                "Give areas with road coverage the strongest police coverage bonus available in the game.",
                "This does not add officers, police cars or road access.",
                "Crimes can still happen. Police still need to reach them.",
                "Turning this off lets the game update coverage normally.");
            Add(nameof(GPTStuntGrowthSettings.MaximumFireCoverage), "Maximum Fire Coverage",
                "Give areas with road coverage the strongest fire coverage bonus available in the game.",
                "This does not put out fires or add firefighters or fire engines.",
                "Fire crews still need staff, vehicles and road access.",
                "Turning this off lets the game update coverage normally.");
            Add(nameof(GPTStuntGrowthSettings.MaximumCommunications), "Communications & Internet",
                "Set the map's communications and internet signal quality to 100%.",
                "This does not add towers or raise their listed capacity.",
                "Turning this off lets the game update signal quality normally.");
            Add(nameof(GPTStuntGrowthSettings.MaximumResources), "Maximum Resources Available",
                "Refill business materials, shop goods and city-service supplies within their storage limits.",
                "Keep existing oil, ore, fertile land and fish supplies from being used up by extraction.",
                "Do not create new deposits, trees or money.",
                "Extra supplies can change production, trade and traffic.");
            Add(nameof(GPTStuntGrowthSettings.MaximumStorage), "Maximum Storage",
                "Refill warehouses and cargo storage with the goods they accept.",
                "Use their existing storage space. Do not make buildings hold more.",
                "Goods already added stay when you turn this off.",
                "Extra goods can change trade and traffic.");
            Add(nameof(GPTStuntGrowthSettings.CityGrowth), "City Growth",
                "Freeze Population Growth: stop new residents moving in and pause births.",
                "Maximize Population Growth: invite more households and allow births.",
                "Growth and Occupancy: also help fill empty homes and shops.",
                "Occupancy only: invite newcomers to fill existing homes and shops, but pause births.",
                "Newcomers still need suitable buildings and access. Buildings fill gradually.",
                "Freeze turns both building output checkboxes off. Choose another mode to unlock them.");
            Add(nameof(GPTStuntGrowthSettings.MaxIndustrialOutput), "Maximize Occupancy & Production Output of Industrials Buildings",
                "Help fill empty industrial buildings and warehouses, and recruit workers for their normal number of jobs.",
                "Production still needs real workers, materials, storage and access. Full output is not guaranteed.",
                "This takes priority over Employment Freeze for these buildings.",
                "Separate supply and utility options still work.",
                "Freeze Population Growth turns this off and locks it. Other growth modes unlock it.",
                "You can choose this while Enable Mod is off, but it only works when the mod is on.",
                "Frozen schooling can leave too few trained workers.",
                "Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.MaxCivicOutput), "Maximize Occupancy & Services Output of Civic Buildings",
                "Help city-service buildings recruit workers for their normal number of jobs.",
                "Do not add students, patients or prisoners. School staffing still follows enrollment.",
                "Services still need staff, money, supplies, utilities and access. Full output is not guaranteed.",
                "This takes priority over Employment Freeze for these buildings.",
                "Separate supply and utility options still work.",
                "Freeze Population Growth turns this off and locks it. Other modes unlock it.",
                "Enable Mod must be on for it to work.",
                "Frozen schooling can leave too few trained workers.",
                "Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.MaximumShopping), "Maximize Shopping Activity",
                "Give households more chances to go shopping.",
                "They still need goods, money, time and a route to the shop.",
                "Purchases use real goods and payments. Sales are not guaranteed.",
                "More shopping can create more traffic.");
            Add(nameof(GPTStuntGrowthSettings.PreferPublicTransportation), "Prefer Public Transportation",
                "For new trips, try public transport, walking or cycling before cars and taxis.",
                "If no route works, try the usual travel choices.",
                "Existing trips continue. This does not add routes, vehicles or seats.",
                "Service and delivery vehicles keep their normal routes.");
            Add(nameof(GPTStuntGrowthSettings.Residents), "Protect Residents",
                "Stop households from moving away.",
                "Pause the game's checks that cause citizens to die.",
                "This does not revive people who are already dead.");
            Add(nameof(GPTStuntGrowthSettings.Age), "Freeze Citizen Age",
                "Keep citizens at their current age.",
                "Stop them from moving into the next age group.",
                "This can limit the number of children who grow up to become workers.");
            Add(nameof(GPTStuntGrowthSettings.Health), "Protect Health",
                "Keep residents' health from dropping.",
                "Health can still improve.");
            Add(nameof(GPTStuntGrowthSettings.Wellbeing), "Protect Well-Being",
                "Keep residents' well-being from dropping.",
                "Well-being can still improve.");
            Add(nameof(GPTStuntGrowthSettings.Happiness), "Protect Happiness",
                "Keep happiness from dropping by protecting the average of health and well-being.",
                "This may raise health or well-being even when their own checkboxes are off.");
            Add(nameof(GPTStuntGrowthSettings.Schooling), "Schooling",
                "Normal Schooling: students enroll, learn and graduate as usual.",
                "Freeze Schooling As Is: pause new admissions and progress toward graduation.",
                "Enrollment Only: allow new students to join, but pause progress toward graduation.",
                "Normal Schooling | No Admissions: current students can learn and graduate; no new students join.",
                "Frozen schooling can leave too few trained workers.",
                "Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.Tenants), "Protect Tenants",
                "Keep households and businesses from moving away.",
                "Help protect occupied properties from abandonment.",
                "This does not fill empty buildings.");
            Add(nameof(GPTStuntGrowthSettings.Condition), "Prevent Deterioration",
                "Stop a building's condition from getting worse because upkeep was not paid.",
                "Fire, disasters and bulldozing are separate.");
            Add(nameof(GPTStuntGrowthSettings.Levels), "Hold Automatic Level Changes",
                "Stop buildings from automatically changing to a different building level.",
                "This does not fill empty spaces or add workers.");
            Add(nameof(GPTStuntGrowthSettings.Redevelopment), "Hold Automatic Redevelopment",
                "Pause automatic construction on zoned empty lots.",
                "Pause the game's automatic handling of condemned buildings.");
            Add(nameof(GPTStuntGrowthSettings.Comfort), "Protect Comfort",
                "Keep the comfort score at stops, stations and parking facilities from dropping.",
                "This does not add seats or transport vehicles.");
            Add(nameof(GPTStuntGrowthSettings.Attractiveness), "Protect Attractiveness",
                "Keep a building's attractiveness score from dropping.",
                "The score can still improve.");
            Add(nameof(GPTStuntGrowthSettings.LandValue), "Land Value",
                "Normal: let land value change as usual.",
                "Freeze: hold the current value.",
                "Prevent Decreases: allow rises, but block drops.",
                "Maximize: use the Land Value Target below.",
                "Reset to ZERO: keep land value at zero until you change this choice.",
                "Reset to ZERO & Freeze Rent: also hold current rent prices. New leases start at the game's normal price.",
                "Other choices do not freeze rent. Changing land value can affect the city's economy.");
            Add(nameof(GPTStuntGrowthSettings.ParkMaintenance), "Protect Park Maintenance",
                "Keep parks from falling below their current maintenance level.",
                "Repairs can still improve them.");
            Add(nameof(GPTStuntGrowthSettings.RoadWear), "Protect Road Condition",
                "Stop roads and car lanes from wearing out further.",
                "Maintenance vehicles can still repair existing wear.");
            Add(nameof(GPTStuntGrowthSettings.UpkeepCosts), "Pause City Upkeep Costs",
                "Stop city-service and map-tile upkeep charges at the next budget update.",
                "Other expenses continue.",
                "Maintenance services still do their work.");
            Add(nameof(GPTStuntGrowthSettings.Garbage), "Pause New Garbage",
                "Stop buildings from creating new garbage.",
                "Garbage trucks can still collect garbage that is already there.");
            Add(nameof(GPTStuntGrowthSettings.RentAmount), "Freeze Rent Amount",
                "This older checkbox has been replaced by a Land Value choice.",
                "Use Reset to ZERO & Freeze Rent to hold existing rents while keeping land value at zero.");
            Add(nameof(GPTStuntGrowthSettings.RentCollection), "Pause Rent Collection",
                "Stop regular rent payments from being taken from tenants.",
                "This does not change the listed rent price.");
            Add(nameof(GPTStuntGrowthSettings.Jobs), "Employment Levels",
                "This controls the number of jobs offered, not how many people are hired.",
                "Normal: let job numbers change as usual.",
                "Freeze: hold the current number.",
                "Prevent Decreases: allow more jobs, but block cuts.",
                "Boost Outside Worker Recruitment: invite extra commuters to fill open jobs. It does not freeze job numbers.",
                "Workers need an outside connection, a suitable job and the right education.",
                "Frozen schooling can leave too few trained workers.",
                "Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.Customers), "Daily Customers",
                "Normal: let the daily customer count change as usual.",
                "Freeze: hold the count.",
                "Prevent Decreases: allow rises, but block drops.",
                "Maximize: use the Daily Customers Target below.",
                "Reset to ZERO: keep the count at zero.",
                "Those choices change the recorded count. They do not create shoppers, sales or money.",
                "Maximize Shopping Activity: give households more chances to shop, using real goods and money. More traffic is possible.",
                "Shopping Activity lets the customer count change normally. A Profit override can still hide changes in reported profit.");
            Add(nameof(GPTStuntGrowthSettings.Profit), "Profit",
                "This changes the reported profit number, not the money a company actually has.",
                "Normal: let profit change as usual.",
                "Freeze: hold the number.",
                "Prevent Decreases: allow rises, but block drops.",
                "Maximize: use the Profit Target below.",
                "Reset to ZERO: keep the number at zero.",
                "Holding profit can hide gains or losses from real sales.");
            Add(nameof(GPTStuntGrowthSettings.Vacancy), "Protect Vacant Buildings",
                "Help keep empty buildings from becoming abandoned because their condition is too low.",
                "For empty buildings already abandoned, pause the countdown to collapse.",
                "This does not reopen abandoned buildings or protect them from every disaster.");
            Add(nameof(GPTStuntGrowthSettings.ResetVacancy), "Reset Vacant Buildings",
                "Clear poor condition on empty buildings and restart any abandonment-to-collapse countdown.",
                "Run one reset each time you click. You can click again later.",
                "This does not reopen abandoned buildings.",
                "Enable Mod must be on, and the simulation must run to apply the reset.");
            Add(nameof(GPTStuntGrowthSettings.ResetAbandonment), "Reset Abandonment",
                "Clear poor condition on occupied and empty buildings.",
                "Restart the collapse countdown on buildings already abandoned. This does not reopen them.",
                "Run one reset each time you click. You can click again later.",
                "Enable Mod must be on, and the simulation must run to apply the reset.");
            Add(nameof(GPTStuntGrowthSettings.CustomerTarget), "Daily Customers Target",
                "Choose the recorded daily customer count for each company.",
                "Only works when Daily Customers is set to Maximize and the mod is on.",
                "This does not create shoppers or sales.");
            Add(nameof(GPTStuntGrowthSettings.LandValueTarget), "Land Value Target",
                "Choose the land value used by Maximize.",
                "Only works when Land Value is set to Maximize and the mod is on.",
                "This is a number you choose, not an official maximum set by the game.");
            Add(nameof(GPTStuntGrowthSettings.ProfitTarget), "Profit Target",
                "Choose the reported profit number for each company.",
                "Only works when Profit is set to Maximize and the mod is on.",
                "This does not put money into company accounts.");
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
