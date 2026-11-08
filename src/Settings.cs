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

        [SettingsUISection(ModeGroup)] public SchoolMode Schooling { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode LandValue { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Staff { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Jobs { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Employees { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Customers { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Income { get; set; }
        [SettingsUISection(ModeGroup)] public ValueMode Profit { get; set; }

        [SettingsUIButton, SettingsUIButtonGroup("CheckboxSelection"), SettingsUISection(BulkGroup)]
        public bool EnableAllCheckboxes { set { if (value) ApplyCheckboxSelection(true); } }
        [SettingsUIButton, SettingsUIButtonGroup("CheckboxSelection"), SettingsUISection(BulkGroup)]
        public bool DisableAllCheckboxes { set { if (value) ApplyCheckboxSelection(false); } }

        [SettingsUISection(CheckboxGroup)] public bool Enabled { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Abandonment { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Age { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentAmount { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Levels { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Redevelopment { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Garbage { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool RentCollection { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Condition { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Attractiveness { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Comfort { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Efficiency { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Happiness { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Health { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Lifespan { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Residents { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Tenants { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Wellbeing { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool LegacyHold { get; set; }
        [SettingsUISection(CheckboxGroup)] public bool Growth { get; set; }

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
            labels[s.GetOptionGroupLocaleID(Settings.VacancyGroup)] = "Vacancy and abandonment";
            labels[s.GetOptionGroupLocaleID(Settings.ModeGroup)] = "Dropdown selections";
            labels[s.GetOptionGroupLocaleID(Settings.BulkGroup)] = "Checkbox actions";
            labels[s.GetOptionGroupLocaleID(Settings.CheckboxGroup)] = "Checkbox selections";
            void Add(string name, string title, string description)
            {
                labels[s.GetOptionLabelLocaleID(name)] = title;
                labels[s.GetOptionDescLocaleID(name)] = description;
            }
            Add(nameof(Settings.EnableAllCheckboxes), "Enable all checkboxes", "Check every toggle, including vacancy protection and the city-specific hold. Keeps the selected dropdown modes.");
            Add(nameof(Settings.DisableAllCheckboxes), "Disable all checkboxes", "Uncheck every toggle, including Enable simulation controls. Dropdown selections are kept and resume when simulation controls are enabled.");
            Add(nameof(Settings.Enabled), "Enable simulation controls", "Apply these controls during gameplay.");
            Add(nameof(Settings.LegacyHold), "Respect the original city-specific hold", "Read the existing Chicago Population Hold settings for its named city.");
            Add(nameof(Settings.Growth), "Stop population growth", "Pause household immigration and births.");
            Add(nameof(Settings.Residents), "Protect residents", "Pause household departures and death checks while protected.");
            Add(nameof(Settings.Age), "Freeze citizen age", "Hold age in days and life-stage transitions.");
            Add(nameof(Settings.Lifespan), "Protect lifespan", "Suppress old-age death rolls. Illness and injuries retain their normal checks.");
            Add(nameof(Settings.Health), "Protect health", "Prevent decreases in resident health.");
            Add(nameof(Settings.Wellbeing), "Protect well-being", "Prevent decreases in resident well-being.");
            Add(nameof(Settings.Happiness), "Protect happiness", "Protect the average of resident health and well-being.");
            Add(nameof(Settings.Schooling), "Schooling", "Choose how admissions and graduation checks operate.");
            Add(nameof(Settings.Tenants), "Protect tenants", "Protect occupied properties against relocation and abandonment.");
            Add(nameof(Settings.Condition), "Prevent deterioration", "Prevent loss of condition from unpaid upkeep.");
            Add(nameof(Settings.Levels), "Hold automatic level changes", "Pause automatic transitions to a new building level.");
            Add(nameof(Settings.Redevelopment), "Hold automatic redevelopment", "Suspend automatic zoned construction on vacant lots.");
            Add(nameof(Settings.Abandonment), "Freeze abandonment", "Hold the condition-based abandonment decision and pause the collapse clock for abandoned buildings.");
            Add(nameof(Settings.Comfort), "Protect comfort", "Protect the native comfort factor at stops, stations and parking facilities.");
            Add(nameof(Settings.Attractiveness), "Protect attractiveness", "Prevent a building's attractiveness from decreasing.");
            Add(nameof(Settings.Efficiency), "Protect efficiency", "Hold building efficiency against decreases.");
            Add(nameof(Settings.LandValue), "Land value", "Hold native road-edge values and the land-value map.");
            Add(nameof(Settings.Garbage), "Pause new garbage", "Hold new building garbage generation while collection continues.");
            Add(nameof(Settings.Staff), "City-service staffing", "Protect worker assignments at service buildings.");
            Add(nameof(Settings.RentAmount), "Freeze rent amount", "Keep existing rental prices. New leases begin at their native initial price.");
            Add(nameof(Settings.RentCollection), "Pause rent collection", "Pause the periodic rent deduction while tenant housekeeping continues.");
            Add(nameof(Settings.Jobs), "Employment levels", "Protect the number of jobs offered by employers.");
            Add(nameof(Settings.Employees), "Employee count", "Protect actual worker assignments at workplaces.");
            Add(nameof(Settings.Customers), "Daily customers", "Protect the company's reported daily customer count.");
            Add(nameof(Settings.Income), "Income", "Protect company income and household income statistics.");
            Add(nameof(Settings.Profit), "Profit", "Protect the company's reported profit.");
            Add(nameof(Settings.Vacancy), "Protect vacant buildings", "Prevent condition-triggered abandonment on empty properties and freeze their existing abandonment-to-collapse timer.");
            Add(nameof(Settings.ResetVacancy), "Reset vacant buildings once", "Clear negative condition and restart any abandonment-to-collapse timer on empty properties only.");
            Add(nameof(Settings.ResetAbandonment), "Reset abandonment once", "Clear negative condition on occupied or vacant buildings and restart the delay on already-abandoned buildings.");
            labels[s.GetEnumValueLocaleID(ValueMode.Normal)] = "Normal";
            labels[s.GetEnumValueLocaleID(ValueMode.Freeze)] = "Freeze";
            labels[s.GetEnumValueLocaleID(ValueMode.PreventDecreases)] = "Prevent decreases";
            labels[s.GetEnumValueLocaleID(SchoolMode.Normal)] = "Normal Schooling";
            labels[s.GetEnumValueLocaleID(SchoolMode.Freeze)] = "Freeze Schooling As Is";
            labels[s.GetEnumValueLocaleID(SchoolMode.EnrollmentOnly)] = "Enrollment Only";
            labels[s.GetEnumValueLocaleID(SchoolMode.NoAdmissions)] = "Normal Schooling | No Admissions";
            return labels;
        }
        public void Unload() { }
    }
}
