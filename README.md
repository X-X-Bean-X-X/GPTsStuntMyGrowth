# GPT's Stunt My Growth by X_Bean_X

A Cities: Skylines II code mod for Windows. Use the skyline-and-pause button in the upper-left toolbar to open **GPT's Stunt My Growth** in Options.

## Buildings

**Protect Vacant Buildings** guards empty rentable buildings against abandonment caused by poor condition. If a property has already been abandoned, the setting holds its countdown to demolition. **Freeze Abandonment** also covers occupied buildings. **Prevent Deterioration** protects condition during building upkeep.

The **Vacancy And Abandonment** section appears at the bottom of the Options page. **Reset Vacant Buildings** and **Reset Abandonment** run once per click and can be used repeatedly in the same city or session. Resetting clears negative condition and restarts eligible demolition timers. Fire, disasters and manual bulldozing have separate effects. A reset does not return an already-abandoned property to service.

## Population and services

Choose independent protections for household growth, resident departures, age, old-age mortality, health, well-being, happiness, tenant moves, building levels, comfort, attractiveness, efficiency, garbage production, rents and staffing. Schooling has four modes. **Normal Schooling | No Admissions** lets existing students progress and graduate while pausing new admissions.

**Enable Mod** is the first checkbox, followed by **Stop Population Growth**. The master switch suspends all controls while retaining individual selections. Native simulation resumes on its next updates.

On a first installation, all 30 checkboxes are enabled and all eight alphabetically ordered dropdowns select **Freeze** (schooling uses **Freeze Schooling As Is**). Existing saved choices are migrated without turning omitted old false settings on. Newly added controls start off for upgrading users until selected. **Enable All Options** turns on every checkbox and selects Freeze in every dropdown. **Disable All Options** clears every checkbox and selects Normal in every dropdown. The reset buttons are not triggered by either bulk action.

Where available, **Prevent Decreases** permits gains. Customer counts, income and profit use the game's tracked statistics. Staff protections maintain real worker assignments, subject to the continuing availability of workers and workplaces.

## Customers, travel and resources

**Maximum Customers** removes population-based reductions from native household shopping chances. Supported Time2Work shopping trip gates and household cooldowns are bypassed while enabled. Existing citizens make real shopping requests: money, household needs, work/school schedules, sleep, shop access and native purchase rules still apply. It does not spawn people or guarantee every shop is full. It overrides the **Daily Customers** dropdown while enabled so real purchases can update the reported count; the dropdown selection is retained for when this checkbox is turned off.

**Prefer Public Transportation** tries available public transport, walking and cycling before driving or taxis for new citizen routes. If that route search fails, the exact original route choices are tried once. Vehicle/service/freight path requests are excluded. Existing trips continue. This does not create routes, vehicles or capacity, and routes with no suitable transit can require a second path search.

**Maximum Resources Available** periodically replenishes business inputs, shop goods and city-service supplies within native inventory limits. Industrial inputs leave capacity for output. Money is excluded. Native extraction uses a temporary resource-map copy so existing oil, ore, fertility and fish deposits are not depleted by extraction; normal production bookkeeping continues. It does not create deposits on barren ground, regrow trees, or remove pollution effects.

**Maximum Storage** periodically refills warehouses and cargo storage with their accepted physical goods up to existing shared capacity, including warehouse level/lot size and active storage upgrades. It does not enlarge storage. Turning either inventory checkbox off stops replenishment; goods already added remain and can be used or sold normally. Artificial supplies can change imports, exports and traffic.

## Maintenance

Three independent checkboxes control maintenance. **Protect Road Condition** keeps wear on road segments and car lanes from increasing. **Protect Park Maintenance** holds each park at its current maintenance level or higher. Both allow repairs to improve condition.

**Pause City Upkeep Costs** waives the service upkeep and map tile upkeep entries at the next city budget calculation. Turn it off to resume charges at the next calculation. Maintenance vehicles, resource deliveries, other expenses and service budget settings continue to operate.

## Unlimited utilities

**Unlimited Electricity**, **Unlimited Water**, and **Unlimited Sewer** are independent switches. They satisfy building demand after the native utility dispatch, clear the corresponding shortage timers and notifications, and remove that service's supply efficiency penalty. Unlimited Water also supplies clean water and clears dirty-water penalties. Fresh water and sewage can each operate normally while the other is unlimited.

These switches bypass supply/network limits for building consumers. They do not add power plants, pumping stations, or sewage outlets, raise the capacity figures shown for those facilities, change utility fees, or remove environmental pollution already produced by facilities. Turn off an individual switch or the master switch to let native dispatch determine that service's supply again.

The old city-specific hold option and its automatic configuration-file controller have been retired. Use **Stop Population Growth** in any city.

## Build

Version **1.75.633** targets Cities: Skylines II **1.6.4f1** on Windows. The package includes Harmony 2.2.2 from the official Lib.Harmony NuGet package. Its license is in `dependencies/Harmony-LICENSE.txt`. Time2Work is optional. The personal ChicagoBuildBridge is not a subscriber requirement.

Install the game's code-modding toolchain and run `Build.ps1 -Compiler <path-to-Roslyn-csc.exe>` in PowerShell 7. The game toolchain supplies `CSII_MANAGEDPATH`, `CSII_MODPOSTPROCESSORPATH` and `CSII_UNITYMODPROJECTPATH`. The script compiles and postprocesses the DLL and writes a package manifest in `build`.

In the previous release, the core controls were exercised on a Chicago GPT QA save, with temporary residents, students and workers where needed. In one 45-second 4x comparison, simulation progress was lower with the additional controls enabled. The size and cause of this difference across other cities are unverified.

## 1.75.632 compatibility update

The assembly version now matches the release and file versions instead of displaying 0.0.0.0. Rebuilt against 1.6.4f1 without changing simulation policies or saved setting keys. Runtime checks on 1.6.4f1 passed within the scope recorded in `validation-1.6.4f1.json`. The update was submitted to the existing Paradox Mods listing on November 13, 2026.

## Validation - 1.75.633

The C# build and official Windows/Burst postprocessing pass against the installed 1.6.4f1 game assemblies. Run `tests/Test.ps1` after `Build.ps1` for settings, utility, shopping, routing, inventory-boundary and native scheduler checks, plus maintenance regression and 131 policy cases. The local run passed 951 checks with `STUNT_TIME2WORK_PATH` set to the installed Time2Work DLL; the optional Time2Work checks are skipped when that environment variable is absent. The settings type has a unique name so the game's immediate-save lookup does not select another mod with a generic `Settings` class.

In a separate Chicago GPT QA save, native dispatch passed 27 utility cases covering disconnected, insufficient and sufficient supply, independent switches and master bypass. Live stock fixtures verified native capacities, industrial output room, money exclusion and normal consumption after disabling replenishment. The actual native ore-extraction job completed its bookkeeping while preserving the deposit only when enabled. The other deposit channels passed a scheduled isolation probe and offline dispatch checks.

The live transit run completed 243 preferred searches and 22 successful fallbacks to original routing. Other searches remained unreachable after fallback; this control cannot repair an inaccessible destination. The test does not establish a citywide public-transport ridership percentage. Core resident, building, rent, health, staffing and all four schooling modes passed regression fixtures. Maximum Customers retains native eligibility and schedules; these checks do not promise full shops or a particular sales increase. See `validation-1.75.633.json` for the recorded scope and performance measurements.
