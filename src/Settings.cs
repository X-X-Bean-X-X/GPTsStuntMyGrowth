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
        [SettingsUIHidden] public bool UnlimitedPublicTransportVehicles { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedAirlines { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedBuses { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool MaximumCommunications { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedElectricity { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedFerries { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedSewer { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedShips { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedSubways { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedTrains { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedTrams { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool UnlimitedWater { get; set; }

        internal bool AnyTransportFleet => UnlimitedAirlines || UnlimitedBuses || UnlimitedFerries ||
            UnlimitedShips || UnlimitedSubways || UnlimitedTrains || UnlimitedTrams;
        private void SetTransportFleets(bool enabled) => UnlimitedAirlines = UnlimitedBuses = UnlimitedFerries =
            UnlimitedShips = UnlimitedSubways = UnlimitedTrains = UnlimitedTrams = enabled;

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
            SetTransportFleets(enabled);
            UnlimitedPublicTransportVehicles = false; // Retired combined switch; migration input only.
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
            DefaultsVersion = 9;
        }

        internal void PrepareUpgrade(int savedVersion)
        {
            if (savedVersion == 0) UseLegacyDefaults();
            // Schema 8 omitted true because the combined switch defaulted to true.
            // Earlier versions did not have fleet control. Read the legacy value before splitting it.
            if (savedVersion < 9) UnlimitedPublicTransportVehicles = savedVersion == 8;
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
            if (savedVersion < 9) SetTransportFleets(savedVersion == 8 && UnlimitedPublicTransportVehicles);
            UnlimitedPublicTransportVehicles = false;
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
            DefaultsVersion = 9;
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
            Add(nameof(GPTStuntGrowthSettings.EnableAllCheckboxes), "Enable All Options", "\u2022 Turn the mod on and set every dropdown to Freeze.\n\n\u2022 Turn on the other checkboxes, including unlimited utilities.\n\n\u2022 The two building output options stay off because population growth is frozen.\n\n\u2022 Keep your target numbers. Do not run either reset button.");
            Add(nameof(GPTStuntGrowthSettings.DisableAllCheckboxes), "Disable All Options", "\u2022 Turn the mod and all checkboxes off.\n\n\u2022 Set dropdowns to Normal where that choice exists.\n\n\u2022 City Growth still shows Freeze, but has no effect while the mod is off.\n\n\u2022 Keep your target numbers. Do not run either reset button.");
            Add(nameof(GPTStuntGrowthSettings.Enabled), "Enable Mod", "\u2022 On: use the settings you choose below.\n\n\u2022 Off: stop this mod from changing the simulation.\n\n\u2022 Your choices are saved. The game takes over as it updates.\n\n\u2022 Turning this off does not undo changes already made.");
            Add(nameof(GPTStuntGrowthSettings.RemoveDead), "Remove the Dead & Decaying", "\u2022 Remove dead citizens who are waiting to be picked up, a few at a time.\n\n\u2022 Leave living citizens alone.\n\n\u2022 Let current hearses and deathcare services finish their work.\n\n\u2022 Do not empty cemeteries or send bodies to another city.\n\n\u2022 Removed bodies do not come back when you turn this off.");
            Add(nameof(GPTStuntGrowthSettings.BoostOutsideWorkers), "Boost Outside Worker Recruitment", "\u2022 Invite extra adult workers from other cities when jobs are open.\n\n\u2022 They need an outside connection and a suitable job.\n\n\u2022 This does not train workers or guarantee that jobs will be filled.\n\n\u2022 Turning this off stops extra invitations. Existing commuters stay in the simulation.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedElectricity), "Unlimited Electricity", "\u2022 Give buildings all the electricity they need.\n\n\u2022 Ignore shortages in power supply and connections.\n\n\u2022 Do not add power plants or raise their listed capacity.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedAirlines), "Unlimited Airline Vehicles", "\u2022 Supply the vehicles requested by your airline lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedBuses), "Unlimited Buses", "\u2022 Supply the vehicles requested by your bus lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedFerries), "Unlimited Ferries", "\u2022 Supply the vehicles requested by your ferry lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedShips), "Unlimited Ships", "\u2022 Supply the vehicles requested by your passenger ship lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedSubways), "Unlimited Subways", "\u2022 Supply the vehicles requested by your subway lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedTrains), "Unlimited Trains", "\u2022 Supply the vehicles requested by your passenger train lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedTrams), "Unlimited Trams", "\u2022 Supply the vehicles requested by your tram lines.\n\n\u2022 Only affects this transport type. Other transport switches work separately.\n\n\u2022 Keep line targets and schedules. Supply requested vehicles, not endless extras.\n\n\u2022 Needs working providers, suitable vehicles and reachable stops. Servicing continues.\n\n\u2022 Frozen schooling or job counts can limit staffing.\n\n\u2022 Shared cargo providers and vehicle-making upgrades keep normal limits.\n\n\u2022 Extra vehicles can increase traffic and slow the game.\n\n\u2022 Turn off to restore normal limits. Surplus vehicles may be withdrawn.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedWater), "Unlimited Water", "\u2022 Give buildings all the clean water they need.\n\n\u2022 Ignore shortages in water supply and pipe connections.\n\n\u2022 Sewage is handled by Unlimited Sewer.");
            Add(nameof(GPTStuntGrowthSettings.UnlimitedSewer), "Unlimited Sewer", "\u2022 Handle all sewage produced by buildings.\n\n\u2022 Ignore limits on sewage outlets and pipe connections.\n\n\u2022 Clean water is handled by Unlimited Water.");
            Add(nameof(GPTStuntGrowthSettings.Growth), "Stop Population Growth", "\u2022 This older checkbox has been replaced by City Growth.\n\n\u2022 Use Freeze Population Growth to pause new residents and births.");
            Add(nameof(GPTStuntGrowthSettings.MaximumOccupancy), "Maximum Building Occupancy", "\u2022 This older checkbox has been replaced by City Growth.\n\n\u2022 Choose an occupancy mode to help fill empty homes and business spaces.");
            Add(nameof(GPTStuntGrowthSettings.MaximumPoliceCoverage), "Maximum Police Coverage", "\u2022 Give areas with road coverage the strongest police coverage bonus available in the game.\n\n\u2022 This does not add officers, police cars or road access.\n\n\u2022 Crimes can still happen. Police still need to reach them.\n\n\u2022 Turning this off lets the game update coverage normally.");
            Add(nameof(GPTStuntGrowthSettings.MaximumFireCoverage), "Maximum Fire Coverage", "\u2022 Give areas with road coverage the strongest fire coverage bonus available in the game.\n\n\u2022 This does not put out fires or add firefighters or fire engines.\n\n\u2022 Fire crews still need staff, vehicles and road access.\n\n\u2022 Turning this off lets the game update coverage normally.");
            Add(nameof(GPTStuntGrowthSettings.MaximumCommunications), "Unlimited Communications & Internet", "\u2022 Set the map's communications and internet signal quality to 100%.\n\n\u2022 This does not add towers or raise their listed capacity.\n\n\u2022 Turning this off lets the game update signal quality normally.");
            Add(nameof(GPTStuntGrowthSettings.MaximumResources), "Maximum Resources Available", "\u2022 Refill business materials, shop goods and city-service supplies within their storage limits.\n\n\u2022 Keep existing oil, ore, fertile land and fish supplies from being used up by extraction.\n\n\u2022 Do not create new deposits, trees or money.\n\n\u2022 Extra supplies can change production, trade and traffic.");
            Add(nameof(GPTStuntGrowthSettings.MaximumStorage), "Maximum Storage", "\u2022 Refill warehouses and cargo storage with the goods they accept.\n\n\u2022 Use their existing storage space. Do not make buildings hold more.\n\n\u2022 Goods already added stay when you turn this off.\n\n\u2022 Extra goods can change trade and traffic.");
            Add(nameof(GPTStuntGrowthSettings.CityGrowth), "City Growth", "\u2022 Freeze Population Growth: stop new residents moving in and pause births.\n\n\u2022 Maximize Population Growth: invite more households and allow births.\n\n\u2022 Growth and Occupancy: also help fill empty homes and shops.\n\n\u2022 Occupancy only: invite newcomers to fill existing homes and shops, but pause births.\n\n\u2022 Newcomers still need suitable buildings and access. Buildings fill gradually.\n\n\u2022 Freeze turns both building output checkboxes off. Choose another mode to unlock them.");
            Add(nameof(GPTStuntGrowthSettings.MaxIndustrialOutput), "Maximize Occupancy & Production Output of Industrials Buildings", "\u2022 Help fill empty industrial buildings and warehouses, and recruit workers for their normal number of jobs.\n\n\u2022 Production still needs real workers, materials, storage and access. Full output is not guaranteed.\n\n\u2022 This takes priority over Employment Freeze for these buildings.\n\n\u2022 Separate supply and utility options still work.\n\n\u2022 Freeze Population Growth turns this off and locks it. Other growth modes unlock it.\n\n\u2022 You can choose this while Enable Mod is off, but it only works when the mod is on.\n\n\u2022 Custom job counts from Change Company take priority.\n\n\u2022 Frozen schooling can leave too few trained workers.\n\n\u2022 Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.MaxCivicOutput), "Maximize Occupancy & Services Output of Civic Buildings", "\u2022 Help city-service buildings recruit workers for their normal number of jobs.\n\n\u2022 Do not add students, patients or prisoners. School staffing still follows enrollment.\n\n\u2022 Services still need staff, money, supplies, utilities and access. Full output is not guaranteed.\n\n\u2022 This takes priority over Employment Freeze for these buildings.\n\n\u2022 Separate supply and utility options still work.\n\n\u2022 Freeze Population Growth turns this off and locks it. Other modes unlock it.\n\n\u2022 Enable Mod must be on for it to work.\n\n\u2022 Frozen schooling can leave too few trained workers.\n\n\u2022 Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.MaximumShopping), "Maximize Shopping Activity", "\u2022 Give households more chances to go shopping.\n\n\u2022 They still need goods, money, time and a route to the shop.\n\n\u2022 Purchases use real goods and payments. Sales are not guaranteed.\n\n\u2022 More shopping can create more traffic.");
            Add(nameof(GPTStuntGrowthSettings.PreferPublicTransportation), "Prefer Public Transportation", "\u2022 For new trips, try public transport, walking or cycling before cars and taxis.\n\n\u2022 If no route works, try the usual travel choices.\n\n\u2022 Existing trips continue. This does not add routes, vehicles or seats.\n\n\u2022 Service and delivery vehicles keep their normal routes.");
            Add(nameof(GPTStuntGrowthSettings.Residents), "Protect Residents", "\u2022 Stop households from moving away.\n\n\u2022 Pause the game's checks that cause citizens to die.\n\n\u2022 This does not revive people who are already dead.");
            Add(nameof(GPTStuntGrowthSettings.Age), "Freeze Citizen Age", "\u2022 Keep citizens at their current age.\n\n\u2022 Stop them from moving into the next age group.\n\n\u2022 This can limit the number of children who grow up to become workers.");
            Add(nameof(GPTStuntGrowthSettings.Health), "Protect Health", "\u2022 Keep residents' health from dropping.\n\n\u2022 Health can still improve.");
            Add(nameof(GPTStuntGrowthSettings.Wellbeing), "Protect Well-Being", "\u2022 Keep residents' well-being from dropping.\n\n\u2022 Well-being can still improve.");
            Add(nameof(GPTStuntGrowthSettings.Happiness), "Protect Happiness", "\u2022 Keep happiness from dropping by protecting the average of health and well-being.\n\n\u2022 This may raise health or well-being even when their own checkboxes are off.");
            Add(nameof(GPTStuntGrowthSettings.Schooling), "Schooling", "\u2022 Normal Schooling: students enroll, learn and graduate as usual.\n\n\u2022 Freeze Schooling As Is: pause new admissions and progress toward graduation.\n\n\u2022 Enrollment Only: allow new students to join, but pause progress toward graduation.\n\n\u2022 Normal Schooling | No Admissions: current students can learn and graduate; no new students join.\n\n\u2022 Frozen schooling can leave too few trained workers.\n\n\u2022 Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.Tenants), "Protect Tenants", "\u2022 Keep households and businesses from moving away.\n\n\u2022 Help protect occupied properties from abandonment.\n\n\u2022 This does not fill empty buildings.");
            Add(nameof(GPTStuntGrowthSettings.Condition), "Prevent Deterioration", "\u2022 Stop a building's condition from getting worse because upkeep was not paid.\n\n\u2022 Fire, disasters and bulldozing are separate.");
            Add(nameof(GPTStuntGrowthSettings.Levels), "Hold Automatic Level Changes", "\u2022 Stop buildings from automatically changing to a different building level.\n\n\u2022 This does not fill empty spaces or add workers.");
            Add(nameof(GPTStuntGrowthSettings.Redevelopment), "Hold Automatic Redevelopment", "\u2022 Pause automatic construction on zoned empty lots.\n\n\u2022 Pause the game's automatic handling of condemned buildings.");
            Add(nameof(GPTStuntGrowthSettings.Comfort), "Protect Comfort", "\u2022 Keep the comfort score at stops, stations and parking facilities from dropping.\n\n\u2022 This does not add seats or transport vehicles.");
            Add(nameof(GPTStuntGrowthSettings.Attractiveness), "Protect Attractiveness", "\u2022 Keep a building's attractiveness score from dropping.\n\n\u2022 The score can still improve.");
            Add(nameof(GPTStuntGrowthSettings.LandValue), "Land Value", "\u2022 Normal: let land value change as usual.\n\n\u2022 Freeze: hold the current value.\n\n\u2022 Prevent Decreases: allow rises, but block drops.\n\n\u2022 Maximize: use the Land Value Target below.\n\n\u2022 Reset to ZERO: keep land value at zero until you change this choice.\n\n\u2022 Reset to ZERO & Freeze Rent: also hold current rent prices. New leases start at the game's normal price.\n\n\u2022 Other choices do not freeze rent. Changing land value can affect the city's economy.");
            Add(nameof(GPTStuntGrowthSettings.ParkMaintenance), "Protect Park Maintenance", "\u2022 Keep parks from falling below their current maintenance level.\n\n\u2022 Repairs can still improve them.");
            Add(nameof(GPTStuntGrowthSettings.RoadWear), "Protect Road Condition", "\u2022 Stop roads and car lanes from wearing out further.\n\n\u2022 Maintenance vehicles can still repair existing wear.");
            Add(nameof(GPTStuntGrowthSettings.UpkeepCosts), "Pause City Upkeep Costs", "\u2022 Stop city-service and map-tile upkeep charges at the next budget update.\n\n\u2022 Other expenses continue.\n\n\u2022 Maintenance services still do their work.");
            Add(nameof(GPTStuntGrowthSettings.Garbage), "Pause New Garbage", "\u2022 Stop buildings from creating new garbage.\n\n\u2022 Garbage trucks can still collect garbage that is already there.");
            Add(nameof(GPTStuntGrowthSettings.RentAmount), "Freeze Rent Amount", "\u2022 This older checkbox has been replaced by a Land Value choice.\n\n\u2022 Use Reset to ZERO & Freeze Rent to hold existing rents while keeping land value at zero.");
            Add(nameof(GPTStuntGrowthSettings.RentCollection), "Pause Rent Collection", "\u2022 Stop regular rent payments from being taken from tenants.\n\n\u2022 This does not change the listed rent price.");
            Add(nameof(GPTStuntGrowthSettings.Jobs), "Employment Levels", "\u2022 This controls the number of jobs offered, not how many people are hired.\n\n\u2022 Normal: let job numbers change as usual.\n\n\u2022 Freeze: hold the current number.\n\n\u2022 Prevent Decreases: allow more jobs, but block cuts.\n\n\u2022 Boost Outside Worker Recruitment: invite extra commuters to fill open jobs. It does not freeze job numbers.\n\n\u2022 Workers need an outside connection, a suitable job and the right education.\n\n\u2022 Custom job counts from Change Company take priority.\n\n\u2022 Frozen schooling can leave too few trained workers.\n\n\u2022 Recruitment cannot promise workers with the right education.");
            Add(nameof(GPTStuntGrowthSettings.Customers), "Daily Customers", "\u2022 Normal: let the daily customer count change as usual.\n\n\u2022 Freeze: hold the count.\n\n\u2022 Prevent Decreases: allow rises, but block drops.\n\n\u2022 Maximize: use the Daily Customers Target below.\n\n\u2022 Reset to ZERO: keep the count at zero.\n\n\u2022 Those choices change the recorded count. They do not create shoppers, sales or money.\n\n\u2022 Maximize Shopping Activity: give households more chances to shop, using real goods and money. More traffic is possible.\n\n\u2022 Shopping Activity lets the customer count change normally. A Profit override can still hide changes in reported profit.");
            Add(nameof(GPTStuntGrowthSettings.Profit), "Profit", "\u2022 This changes the reported profit number, not the money a company actually has.\n\n\u2022 Normal: let profit change as usual.\n\n\u2022 Freeze: hold the number.\n\n\u2022 Prevent Decreases: allow rises, but block drops.\n\n\u2022 Maximize: use the Profit Target below.\n\n\u2022 Reset to ZERO: keep the number at zero.\n\n\u2022 Holding profit can hide gains or losses from real sales.");
            Add(nameof(GPTStuntGrowthSettings.Vacancy), "Protect Vacant Buildings", "\u2022 Help keep empty buildings from becoming abandoned because their condition is too low.\n\n\u2022 For empty buildings already abandoned, pause the countdown to collapse.\n\n\u2022 This does not reopen abandoned buildings or protect them from every disaster.");
            Add(nameof(GPTStuntGrowthSettings.ResetVacancy), "Reset Vacant Buildings", "\u2022 Clear poor condition on empty buildings and restart any abandonment-to-collapse countdown.\n\n\u2022 Run one reset each time you click. You can click again later.\n\n\u2022 This does not reopen abandoned buildings.\n\n\u2022 Enable Mod must be on, and the simulation must run to apply the reset.");
            Add(nameof(GPTStuntGrowthSettings.ResetAbandonment), "Reset Abandonment", "\u2022 Clear poor condition on occupied and empty buildings.\n\n\u2022 Restart the collapse countdown on buildings already abandoned. This does not reopen them.\n\n\u2022 Run one reset each time you click. You can click again later.\n\n\u2022 Enable Mod must be on, and the simulation must run to apply the reset.");
            Add(nameof(GPTStuntGrowthSettings.CustomerTarget), "Daily Customers Target", "\u2022 Choose the recorded daily customer count for each company.\n\n\u2022 Only works when Daily Customers is set to Maximize and the mod is on.\n\n\u2022 This does not create shoppers or sales.");
            Add(nameof(GPTStuntGrowthSettings.LandValueTarget), "Land Value Target", "\u2022 Choose the land value used by Maximize.\n\n\u2022 Only works when Land Value is set to Maximize and the mod is on.\n\n\u2022 This is a number you choose, not an official maximum set by the game.");
            Add(nameof(GPTStuntGrowthSettings.ProfitTarget), "Profit Target", "\u2022 Choose the reported profit number for each company.\n\n\u2022 Only works when Profit is set to Maximize and the mod is on.\n\n\u2022 This does not put money into company accounts.");
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
