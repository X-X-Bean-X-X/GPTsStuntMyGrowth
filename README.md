# GPT's Stunt My Growth by X_Bean_X

A Cities: Skylines II code mod for Windows. Use the skyline-and-pause button in the upper-left toolbar to open **GPT's Stunt My Growth** in Options.

## Buildings

**Protect vacant buildings** guards empty rentable buildings against abandonment caused by poor condition. If a property has already been abandoned, the setting holds its countdown to demolition. **Freeze abandonment** also covers occupied buildings. **Prevent deterioration** protects condition during building upkeep.

Both reset buttons are one-time actions. Resetting clears negative condition and restarts eligible demolition timers. Fire, disasters and manual bulldozing have separate effects. A reset does not return an already-abandoned property to service.

## Population and services

Choose independent protections for household growth, resident departures, age, old-age mortality, health, well-being, happiness, tenant moves, building levels, comfort, attractiveness, efficiency, garbage production, rents and staffing. Schooling has four modes. **Normal Schooling | No Admissions** lets existing students progress and graduate while pausing new admissions.

The eight dropdowns appear alphabetically by their visible labels and initially select **Freeze**. **Stop population growth** sits directly below **Enable simulation controls**. The remaining 18 checkboxes follow in alphabetical order. Where available, **Prevent decreases** permits gains. Individual checkboxes remain editable after using **Enable all checkboxes** or **Disable all checkboxes**. Disable all also turns off the master switch. Customer counts, income and profit use the game's tracked statistics. Staff protections maintain real worker assignments, subject to the continuing availability of workers and workplaces.

## Original city-specific hold

To use this optional setting, place a file named `population-hold-settings.json` in `ModsSettings/GPTStuntYourGrowth` inside the Cities: Skylines II user-data folder. For example:

```json
{"enabled": true, "cityName": "Chicago GPT"}
```

With the master switch and **Respect the original city-specific hold** checked, the mod pauses household spawning and home searches only in the named city. An existing installation can point to its original configuration by putting its absolute JSON path on one line in `legacy-config-location.txt` in the same directory.

## Build

Version **1.75.629** targets Cities: Skylines II **1.6.2f1** on Windows. The package includes Harmony 2.2.2 from the official Lib.Harmony NuGet package. Its license is in `dependencies/Harmony-LICENSE.txt`. Time2Work is optional. The personal ChicagoBuildBridge is not a subscriber requirement.

Install the game's code-modding toolchain and run `Build.ps1 -Compiler <path-to-Roslyn-csc.exe>` in PowerShell 7. The game toolchain supplies `CSII_MANAGEDPATH`, `CSII_MODPOSTPROCESSORPATH` and `CSII_UNITYMODPROJECTPATH`. The script compiles and postprocesses the DLL and writes a package manifest in `build`.

The core controls have been exercised on a Chicago GPT QA save, with temporary residents, students and workers where needed. In one 45-second 4× comparison, simulation progress was lower with the additional controls enabled. The size and cause of this difference across other cities are unverified.
