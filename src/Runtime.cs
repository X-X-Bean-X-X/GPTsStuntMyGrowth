using System;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using HarmonyLib;
using Unity.Entities;

namespace StuntYourGrowth
{
    internal static class Runtime
    {
        internal const string Id = "GPTStuntYourGrowth";
        internal const string Title = "GPT's Stunt My Growth by X_Bean_X";
        internal const string OptionsTitle = "GPT's Stunt My Growth";
        internal static GPTStuntGrowthSettings Options;
        internal static bool Disposed { get; private set; }
        internal static readonly ILog Log = LogManager.GetLogger(Id);
        static Harmony patches;
        static Locale locale;
        internal static bool Playing => Options?.Enabled == true && GameManager.instance != null &&
            GameManager.instance.gameMode == GameMode.Game && !GameManager.instance.isGameLoading;

        public sealed class SavedSettingsSchema
        {
            public int DefaultsVersion { get; set; }
        }

        internal static void Start(IMod mod, UpdateSystem update)
        {
            Disposed = false;
            Options = new GPTStuntGrowthSettings(mod);
            // CS2 stores differences from defaults, so omitted old false toggles must stay false.
            bool existing = false;
            int savedVersion = 0;
            AssetDatabase.global.LoadSettings<SavedSettingsSchema>(Id, (saved, source) =>
            {
                existing = true;
                savedVersion = saved.DefaultsVersion;
            });
            if (existing) Options.PrepareUpgrade(savedVersion);
            // A zero baseline forces the version marker to be saved even with default selections.
            AssetDatabase.global.LoadSettings(Id, Options, new GPTStuntGrowthSettings(mod) { DefaultsVersion = 0 });
            Options.DefaultsVersion = 5;
            Options.RegisterInOptionsUI();
            locale = new Locale(Options);
            GameManager.instance.localizationManager.AddSource("en-US", locale);
            try
            {
                patches = new Harmony(Id);
                RegisterGate<HouseholdSpawnSystem>();
                RegisterGate<BirthSystem>();
                RegisterGate<FindSchoolSystem>();
                RegisterGate<ApplyToSchoolSystem>();
                RegisterGate<GraduationSystem>();
                RegisterGate<AgingSystem>();
                RegisterGate<DeathCheckSystem>();
                RegisterGate<HouseholdMoveAwaySystem>();
                RegisterGate<ZoneSpawnSystem>();
                RegisterGate<CondemnedBuildingSystem>();
                RegisterGate<CompanyMoveAwaySystem>();
                NativeHooks.Register(patches);
                UtilityHooks.Register(patches);
                TransitHooks.Register(patches);
                ResourceHooks.Register(patches);
                CommuterHooks.Register(patches);
                GameManager.instance.onWorldReady += OnWorldReady;
                update.UpdateBefore<ProtectionSystem>(SystemUpdatePhase.GameSimulation);
                update.UpdateBefore<DeadCitizenCleanupSystem>(SystemUpdatePhase.GameSimulation);
                update.UpdateAt<ToolbarSystem>(SystemUpdatePhase.UIUpdate);
                Log.Info(Title + " registered native settings and simulation controls.");
            }
            catch
            {
                Stop();
                throw;
            }
        }
        static void OnWorldReady()
        {
            if(patches!=null) NativeHooks.RegisterIntegrations(patches);
        }
        static void RegisterGate<T>() where T : GameSystemBase
        {
            patches.Patch(AccessTools.DeclaredMethod(typeof(T), "OnUpdate"),
                prefix: new HarmonyMethod(typeof(Runtime), nameof(AllowUpdate)));
        }
        static bool AllowUpdate(GameSystemBase __instance)
        {
            if (!Playing) return true;
            var s = Options;
            if (__instance is HouseholdSpawnSystem || __instance is BirthSystem) return !s.Growth;
            if (__instance is FindSchoolSystem || __instance is ApplyToSchoolSystem) return Policy.Admissions(s.Schooling);
            if (__instance is GraduationSystem) return Policy.Education(s.Schooling);
            if (__instance is AgingSystem) return !s.Age;
            if (__instance is DeathCheckSystem) return !s.Residents;
            if (__instance is HouseholdMoveAwaySystem) return !(s.Residents || s.Tenants);
            if (__instance is ZoneSpawnSystem || __instance is CondemnedBuildingSystem) return !s.Redevelopment;
            if (__instance is CompanyMoveAwaySystem) return !s.Tenants;
            return true;
        }
        internal static void RequestReset(bool emptyOnly = false)
        {
            if (!Playing) return;
            ProtectionSystem.Current?.QueueReset(emptyOnly);
        }
        internal static void Stop()
        {
            Disposed = true;
            if(GameManager.instance!=null) GameManager.instance.onWorldReady -= OnWorldReady;
            patches?.UnpatchAll(Id);
            patches = null;
            Options?.UnregisterInOptionsUI();
            if (locale != null) GameManager.instance?.localizationManager?.RemoveSource("en-US", locale);
            Options = null;
            locale = null;
        }
    }
}
