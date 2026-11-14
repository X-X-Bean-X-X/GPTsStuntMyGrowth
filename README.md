# GPT's Stunt My Growth by X_Bean_X

A Cities: Skylines II code mod for Windows. Use the full-color **River Glow** skyline-and-pause button in the upper-left toolbar to open **GPT's Stunt My Growth** in Options.

## Buildings

**Protect Vacant Buildings** guards empty rentable buildings against abandonment caused by poor condition. If a property has already been abandoned, the setting holds its countdown to demolition. **Prevent Deterioration** protects condition during building upkeep.

The **Vacancy And Abandonment** section appears at the bottom of the Options page. **Reset Vacant Buildings** and **Reset Abandonment** run once per click and can be used repeatedly in the same city or session. Resetting clears negative condition and restarts eligible demolition timers. Fire, disasters and manual bulldozing have separate effects. A reset does not return an already-abandoned property to service.

## Population and services

Choose independent protections for household growth, resident departures, age, health, well-being, happiness, tenant moves, building levels, comfort, attractiveness, efficiency, garbage production, rents and employment capacity. Schooling has four modes. **Normal Schooling | No Admissions** lets existing students progress and graduate while pausing new admissions.

**Enable Mod** is the first checkbox, followed by **Stop Population Growth**. The master switch suspends all controls while retaining individual selections. Native simulation resumes on its next updates.

On a first installation, all 29 checkboxes are enabled and all six alphabetically ordered dropdowns select **Freeze** (schooling uses **Freeze Schooling As Is**). Existing saved choices are migrated without turning omitted old false settings on. Newly added controls start off for upgrading users until selected. **Enable All Options** turns on every checkbox and selects Freeze in every dropdown. **Disable All Options** clears every checkbox and selects Normal in every dropdown. The reset buttons are not triggered by either bulk action.

Where available, **Prevent Decreases** permits gains. Customer counts, income and profit use the game's tracked statistics. Service staffing and worker assignments use the native simulation; City-Service Staffing has been removed.

## Outside Worker Recruitment

**Boost Outside Worker Recruitment** adds a bounded commuter-recruitment pass while native spawning continues. It checks vacancies across all five education levels against the available workforce. More-qualified available workers can cover lower-level openings; workers cannot cover jobs above their qualifications. Incoming commuters still receive their education through the native initializer. This is not worker training and does not guarantee a particular skill mix or successful hire.

At most 16 extra adult workers are recruited per 256 simulation frames. The boost reserves room for the native planned batch and commuters still waiting for work; it stops adding workers when that waiting/reserved pool reaches 64. It retains the native commuter-to-worker limit and only uses eligible adult-only household prefabs and native outside-connection selection. No suitable prefab, usable connection or remaining room means no extra recruitment. Vanilla recruitment is unchanged by these boost limits.

Stop Population Growth can remain enabled because commuters are separate from resident immigration. The checkbox does not modify residents, schooling, offered job capacity, employee rosters or efficiency factors. Native routing and job matching still decide hires. The removed City-Service Staffing control no longer blocks service hires. Added commuters can increase traffic and simulation work. Disabling the boost stops new extra batches; existing commuters remain in normal simulation. An already scheduled batch can finish.

Fresh defaults and Enable All Options include this checkbox. Existing settings below schema 5 keep it off until selected. Live tests exercised the native spawn job and production recruitment wrapper, timing gates, waiting limits and native service/business hiring. Three eligible native household prefabs were available in the tested playset. Hiring tests supplied successful route results; they do not guarantee usable routes or a particular qualification mix in every city.

## Customers, travel and resources

The Employee Count, Maximum Customers, Hire All Available Jobs, Protect Lifespan, Freeze Abandonment and City-Service Staffing controls have been removed. Old saved keys are ignored. Shopping and ordinary business hiring use their normal game behavior; Daily Customers still controls the reported customer statistic. Worker training and a workforce status panel are not included.

**Prefer Public Transportation** tries available public transport, walking and cycling before driving or taxis for new citizen routes. If that route search fails, the exact original route choices are tried once. Vehicle/service/freight path requests are excluded. Existing trips continue. This does not create routes, vehicles or capacity, and routes with no suitable transit can require a second path search.

**Maximum Resources Available** periodically replenishes business inputs, shop goods and city-service supplies within native inventory limits. Industrial inputs leave capacity for output. Money is excluded. Native extraction uses a temporary resource-map copy so existing oil, ore, fertility and fish deposits are not depleted by extraction; normal production bookkeeping continues. It does not create deposits on barren ground, regrow trees, or remove pollution effects.

**Maximum Storage** periodically refills warehouses and cargo storage with their accepted physical goods up to existing shared capacity, including warehouse level/lot size and active storage upgrades. It does not enlarge storage. Turning either inventory checkbox off stops replenishment; goods already added remain and can be used or sold normally. Artificial supplies can change imports, exports and traffic.

## Automatic Corpse Cleanup

**Automatically Remove Dead & Decaying Citizens** removes deceased citizens awaiting pickup through the game's native citizen-deletion cleanup. It does not transfer bodies to a neighboring city, spawn hearses or change service-import policy. The game has a Dead flag rather than a separate Decaying classification; the control targets that flag together with RequireTransport.

Cleanup runs during simulation, with at most 64 removals and 2,048 candidate checks per 256 simulation frames. It leaves living citizens, active pickups, citizens inside vehicles and bodies already traveling to or held in deathcare alone. Those existing trips continue normally. It does not empty cemetery storage counters. Citizens missing the household/job-seeker components required by native cleanup are skipped. Death statistics are not fabricated or counted again.

Turning this checkbox or Enable Mod off stops future cleanup; already removed bodies cannot be restored by switching it off. Fresh installation/reset and Enable All Options turn it on. Existing settings below schema 4 leave it off until selected; other choices are preserved. Protect Residents can prevent new deaths, but does not prevent cleanup of citizens who were already dead.

## Maintenance Controls

Three independent checkboxes control maintenance. **Protect Road Condition** keeps wear on road segments and car lanes from increasing. **Protect Park Maintenance** holds each park at its current maintenance level or higher. Both allow repairs to improve condition.

**Pause City Upkeep Costs** waives the service upkeep and map tile upkeep entries at the next city budget calculation. Turn it off to resume charges at the next calculation. Maintenance vehicles, resource deliveries, other expenses and service budget settings continue to operate.

## Unlimited utilities

**Unlimited Electricity**, **Unlimited Water**, and **Unlimited Sewer** are independent switches. They satisfy building demand after the native utility dispatch, clear the corresponding shortage timers and notifications, and remove that service's supply efficiency penalty. Unlimited Water also supplies clean water and clears dirty-water penalties. Fresh water and sewage can each operate normally while the other is unlimited.

These switches bypass supply/network limits for building consumers. They do not add power plants, pumping stations, or sewage outlets, raise the capacity figures shown for those facilities, change utility fees, or remove environmental pollution already produced by facilities. Turn off an individual switch or the master switch to let native dispatch determine that service's supply again.

The old city-specific hold option and its automatic configuration-file controller have been retired. Use **Stop Population Growth** in any city.

## Build

Version **1.75.638** targets Cities: Skylines II **1.6.4f1** on Windows. This update replaces the toolbar icon with the approved navy-and-gold River Glow artwork. Clicking it still opens this mod's Options page. Simulation code and saved setting keys are unchanged from 1.75.637. See `validation-1.75.638.json` for icon-update verification and `validation-1.75.637.json` for the preceding gameplay validation. The package includes Harmony 2.2.2 from the official Lib.Harmony NuGet package. Its license is in `dependencies/Harmony-LICENSE.txt`. Time2Work is optional. The personal ChicagoBuildBridge is not a subscriber requirement.

Install the game's code-modding toolchain and run `Build.ps1 -Compiler <path-to-Roslyn-csc.exe>` in PowerShell 7. The game toolchain supplies `CSII_MANAGEDPATH`, `CSII_MODPOSTPROCESSORPATH` and `CSII_UNITYMODPROJECTPATH`. The script compiles and postprocesses the DLL and writes a package manifest in `build`.

Gameplay validation for 1.75.637 included 5,259 offline checks, the maintenance regression and 131 policy cases; native corpse cleanup, efficiency penalties, recruitment/hiring, utilities, resources, transit fallback, building protections and repeatable reset tests; settings migration/persistence and visible Options checks. See `validation-1.75.637.json` for evidence and scope.

Matched simulation runs reloaded the same populated test save for each configuration, warmed up for 20 seconds and measured 45 seconds. With the new cleanup/recruitment controls off/on, normal speed advanced 2,590/2,608 frames; 4x advanced 7,092/6,939 frames. No mod or simulation exceptions appeared in the reviewed logs. One comparison in a small city cannot establish large-city or long-duration performance. The playset also logged an unrelated ChicagoCoverageCache compatibility disable and asset warnings.

## 1.75.632 compatibility update

The assembly version now matches the release and file versions instead of displaying 0.0.0.0. Rebuilt against 1.6.4f1 without changing simulation policies or saved setting keys. Runtime checks on 1.6.4f1 passed within the scope recorded in `validation-1.6.4f1.json`. The update was submitted to the existing Paradox Mods listing on November 13, 2026.

## Efficiency Protection

**Protect Efficiency** now holds individual weather and city-bonus/specialization factors against decreases. It never replaces the entire efficiency-factor list. Electricity, water, sewage, dirty water, staffing, employee health/happiness, budget, fees, garbage, telecom, mail and material/resource shortages retain their current native factors. Unknown future factors are also left alone. Removed factors stay removed; a closed, destroyed, burning or abandoned building resets its bonus baseline. After turning off an unlimited utility, its native shortage penalty can take effect even while Protect Efficiency remains enabled.

This intentionally narrows efficiency protection: total efficiency can fall when an operational requirement is not met. Weather factors retained by this control are wind speed, water depth and sunlight. City-modifier and specialization bonuses are protected only while the factor still exists.

## Remaining Interactions

- City-Service Staffing and its hiring/roster-retention hooks are removed. Native service hiring and departures can proceed; Employment Levels still protects the number of jobs offered.
- Stop Population Growth, frozen age and frozen schooling can limit the future supply of qualified workers. Employment Levels = Freeze holds offered job capacity.
- Protect Residents still pauses death checks and household departures. Removing Protect Lifespan does not disable that broader protection.
- Protect Happiness can raise health or well-being to maintain happiness even if their individual protections are off.
- Hold Automatic Level Changes, Protect Tenants, Prevent Deterioration and Protect Vacant Buildings overlap in avoiding condition-related abandonment. Removing Freeze Abandonment does not remove these separate safeguards. Existing occupied abandoned buildings no longer receive the removed blanket collapse-timer freeze; the repeatable Reset Abandonment button remains.
- Daily Customers, Income and Profit protect reported statistics; Freeze can hide changes in actual shopping and economic activity. Resource/storage replenishment can change production, trade and traffic.
- Enable All Options selects Freeze in every remaining dropdown, including schooling. Disable All Options selects Normal.
