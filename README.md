# GPT's Puppet Master by X_Bean_X

A Cities: Skylines II code mod for Windows. Use the full-color **Puppet Master riverfront** button in the upper-left toolbar to open **GPT's Puppet Master** in Options.

## Buildings

**Protect Vacant Buildings** guards empty rentable buildings against abandonment caused by poor condition. If a property has already been abandoned, the setting holds its countdown to demolition. **Prevent Deterioration** protects condition during building upkeep.

The **Vacancy And Abandonment** section appears at the bottom of the Options page. **Reset Vacant Buildings** and **Reset Abandonment** run once per click and can be used repeatedly in the same city or session. Resetting clears negative condition and restarts eligible demolition timers. Fire, disasters and manual bulldozing have separate effects. A reset does not return an already-abandoned property to service.

## Population and services

Choose independent protections for household growth, resident departures, age, health, well-being, happiness, tenant moves, building levels, comfort, attractiveness, efficiency, garbage production, rents and employment capacity. Schooling has four modes. **Normal Schooling | No Admissions** lets existing students progress and graduate while pausing new admissions.

**Enable Mod** is first. Alphabetical dropdowns, each immediately followed by its applicable target slider, follow, then the bulk buttons. **Stop Population Growth** appears immediately before **Remove the Dead & Decaying** at the start of the checkbox section. The master switch suspends all controls while retaining individual selections. Native simulation resumes on its next updates.

On a first installation, all 32 checkboxes are enabled and all six alphabetically ordered dropdowns select **Freeze** (schooling uses **Freeze Schooling As Is**). Existing saved choices are migrated without turning omitted old false settings on. Newly added controls start off for upgrading users until selected. **Enable All Options** turns on every checkbox and selects Freeze in every dropdown. **Disable All Options** clears every checkbox and selects Normal in every dropdown. The reset buttons are not triggered by either bulk action.

Where available, **Prevent Decreases** permits gains. Customer counts, income and profit use the game's tracked statistics. Service staffing and worker assignments use the native simulation; City-Service Staffing has been removed.

## Numeric Controls

Daily Customers, Income, Land Value and Profit support Normal, Freeze, Prevent Decreases, Maximize and Reset to ZERO. Employment Levels offers Normal, Freeze, Prevent Decreases, Reset to ZERO and Boost Outside Worker Recruitment; its Maximize choice has been removed. Schooling keeps its original four choices. First-load defaults remain Freeze.

Employment controls offered jobs, not people hired. Recruitment and zero job capacity are mutually exclusive selections. Outside-connection job providers and unsupported/malformed employers are left alone. Older independent recruitment selections migrate into the dropdown, except an explicit Zero selection takes priority. A retired Employment Maximize selection migrates to Normal unless recruitment was enabled.

The four target-based values have no verified universal game-defined maximum. Their **Maximize Targets** sliders set explicit mod targets: Daily Customers defaults to 1,000 (range 1â€“100,000), Income and Profit to 10,000 (range 1â€“1,000,000), and Land Value to 100 (range 1â€“10,000). These are adjustable targets, not claims about native limits. Each target appears directly below its dropdown. Both editing and application are disabled unless the master switch is enabled and the corresponding dropdown selects Maximize. Bulk Enable/Disable does not discard customized targets.

**Reset to ZERO holds zero continuously**, including on new eligible entities. It is not a one-click reset. Zero Employment Levels can cause native layoffs, and zero land value can affect the economy. Normal or the master switch stops overrides; the simulation recalculates values on its own schedule, without undoing consequences already produced.

Customers, Income and Profit modify tracked values without directly creating shoppers, sales, or cash deposits. These are not necessarily display-only: household income is used in native disposable-income and shopping-preference calculations. Actual purchase affordability still depends on available cash after native costs. Income Zero can therefore work against Maximize Shopping Activity. Land Value changes road-edge values and the land-value map together. The historical customer record is preserved. Numeric overrides and existing Freeze/Prevent Decreases policies operate separately for each field.

## Outside Worker Recruitment

The **Employment Levels â†’ Boost Outside Worker Recruitment** selection adds a bounded commuter-recruitment pass while native spawning continues. It checks vacancies across all five education levels against the available workforce. More-qualified available workers can cover lower-level openings; workers cannot cover jobs above their qualifications. Incoming commuters still receive their education through the native initializer. This is not worker training and does not guarantee a particular skill mix or successful hire.

At most 16 extra adult workers are recruited per 256 simulation frames. The boost reserves room for the native planned batch and commuters still waiting for work; it stops adding workers when that waiting/reserved pool reaches 64. It retains the native commuter-to-worker limit and only uses eligible adult-only household prefabs and native outside-connection selection. No suitable prefab, usable connection or remaining room means no extra recruitment. Vanilla recruitment is unchanged by these boost limits.

Stop Population Growth can remain enabled because commuters are separate from resident immigration. The recruitment mode does not modify residents, schooling, offered job capacity, employee rosters or efficiency factors. Native routing and job matching still decide hires. The removed City-Service Staffing control no longer blocks service hires. Added commuters can increase traffic and simulation work. Disabling the boost stops new extra batches; existing commuters remain in normal simulation. An already scheduled batch can finish.

Fresh defaults and Enable All Options select Freeze for Employment, so recruitment is not enabled at the same time. Native recruitment/hiring was tested in the prior 1.75.637 release; this revised dropdown wiring still requires its own live release check.

## Customers, travel and resources

The Employee Count, Maximum Customers, Hire All Available Jobs, Protect Lifespan, Freeze Abandonment and City-Service Staffing controls have been removed. Old saved keys are ignored. Shopping and ordinary business hiring use their normal game behavior; Daily Customers still controls the reported customer statistic. Worker training and a workforce status panel are not included.

**Prefer Public Transportation** tries available public transport, walking and cycling before driving or taxis for new citizen routes. If that route search fails, the exact original route choices are tried once. Vehicle/service/freight path requests are excluded. Existing trips continue. This does not create routes, vehicles or capacity, and routes with no suitable transit can require a second path search.

**Maximum Resources Available** periodically replenishes business inputs, shop goods and city-service supplies within native inventory limits. Industrial inputs leave capacity for output. Money is excluded. Native extraction uses a temporary resource-map copy so existing oil, ore, fertility and fish deposits are not depleted by extraction; normal production bookkeeping continues. It does not create deposits on barren ground, regrow trees, or remove pollution effects.

**Maximum Storage** periodically refills warehouses and cargo storage with their accepted physical goods up to existing shared capacity, including warehouse level/lot size and active storage upgrades. It does not enlarge storage. Turning either inventory checkbox off stops replenishment; goods already added remain and can be used or sold normally. Artificial supplies can change imports, exports and traffic.

## Citywide coverage benefits

**Maximum Police Coverage** and **Maximum Fire Coverage** provide the strongest finite coverage magnitude found in the loaded service prefab definitions across existing road coverage buffers. They do not inflate provider ranges or add coverage path searches. Higher existing coverage is retained, and unrelated service types are untouched. These are citywide road-coverage benefits, not instant police/fire response, immunity to crime/fire, or added staff/vehicles. Areas without a road coverage buffer are not given fake roads.

**Communications & Internet** sets the native telecommunications map to its maximum sampled quality (100%). Internet and telecom use this same coverage map. It does not fabricate tower capacity or change their reported hardware statistics. All coverage switches resume native recalculation when disabled; restoration occurs on the respective native system's update schedule.

## Maximize Shopping Activity

Raises the native household shopping lottery to its built-in upper limit by removing the population-based reduction in the scheduled household job's private parameter copy. The global economy configuration is unchanged. Native repeat-purchase reduction, update frequency, existing needs, affordability, commodity/age preferences, work/school/sleep priorities, routing and transaction processing continue normally. It does not spawn shoppers, invent sales, or deposit cash. Successful purchases use native goods transfers and real payments; sales and profit are not guaranteed.

This can increase shopping traffic. Freeze/Zero/Maximize statistic controls still hold their selected reported values and can conceal the increased real activity. Existing-game upgrades leave this checkbox off until selected; fresh defaults and Enable All Options enable it.

## Maximum Building Occupancy

This option creates real native households and companies in small batches to fill eligible vacant properties. Households enter through native outside connections and initialize normally; companies use matching native commercial, industrial/office or storage prefabs. Native property search decides where they settle. Building capacity, renter lists, employment and population counters are not fabricated. Extractor sites, service seats and hotel guest rooms are excluded.

At most four households and two companies are added per 256 simulation frames. Existing unplaced tenants reserve vacancies; household and company backlogs are capped at 64 and 16. Recruitment stops when these limits leave no room. Placement can remain incomplete when native access or suitability requirements fail. The option does not promise instant or guaranteed 100% occupancy in every building.

**Maximum Building Occupancy takes priority over Stop Population Growth for move-ins.** Native household spawning is suppressed while this bounded replacement is active, preventing two independent immigration streams. Births retain their existing growth setting. Disabling occupancy stops new extra tenants; people and companies already created remain.

New shopping, coverage and occupancy switches default off when upgrading existing settings. First-time/reset defaults and Enable All Options turn them on, following the requested all-checkboxes-enabled behavior.

## Automatic Corpse Cleanup

**Remove the Dead & Decaying** removes deceased citizens awaiting pickup through the game's native citizen-deletion cleanup. It does not transfer bodies to a neighboring city, spawn hearses or change service-import policy. The game has a Dead flag rather than a separate Decaying classification; the control targets that flag together with RequireTransport.

Cleanup runs during simulation, with at most 64 removals and 2,048 candidate checks per 256 simulation frames. It leaves living citizens, active pickups, citizens inside vehicles and bodies already traveling to or held in deathcare alone. Those existing trips continue normally. It does not empty cemetery storage counters. Citizens missing the household/job-seeker components required by native cleanup are skipped. Death statistics are not fabricated or counted again.

Turning this checkbox or Enable Mod off stops future cleanup; already removed bodies cannot be restored by switching it off. Fresh installation/reset and Enable All Options turn it on. Existing settings below schema 4 leave it off until selected; other choices are preserved. Protect Residents can prevent new deaths, but does not prevent cleanup of citizens who were already dead.

## Maintenance Controls

Three independent checkboxes control maintenance. **Protect Road Condition** keeps wear on road segments and car lanes from increasing. **Protect Park Maintenance** holds each park at its current maintenance level or higher. Both allow repairs to improve condition.

**Pause City Upkeep Costs** waives the service upkeep and map tile upkeep entries at the next city budget calculation. Turn it off to resume charges at the next calculation. Maintenance vehicles, resource deliveries, other expenses and service budget settings continue to operate.

## Unlimited utilities

**Unlimited Electricity**, **Unlimited Water**, and **Unlimited Sewer** are independent switches. They satisfy building demand after the native utility dispatch, clear the corresponding shortage timers and notifications, and remove that service's supply efficiency penalty. Unlimited Water also supplies clean water and clears dirty-water penalties. Fresh water and sewage can each operate normally while the other is unlimited.

These switches bypass supply/network limits for building consumers. They do not add power plants, pumping stations, or sewage outlets, raise the capacity figures shown for those facilities, change utility fees, or remove environmental pollution already produced by facilities. Turn off an individual switch or the master switch to let native dispatch determine that service's supply again.

The old city-specific hold option and its automatic configuration-file controller have been retired. Use **Stop Population Growth** in any city.

## Release validation status

The revised 1.75.639 build is currently local-only. Offline compilation and automated checks are recorded separately from live gameplay. Shopping, coverage, occupancy, revised UI and updated numeric controls require release testing before upload. Compatibility testing against the user's other mods is deferred.

## Build

Version **1.75.639** targets Cities: Skylines II **1.6.4f1** on Windows. Renamed to GPT's Puppet Master, with the approved riverfront icon and T2 thumbnail. Internal mod identity and saved setting keys are retained. Four numeric dropdowns have Maximize targets; employment has exclusive recruitment/hold/zero modes. Land Value adds Reset to ZERO & Freeze Rent. Schooling is unchanged. The package includes Harmony 2.2.2 from the official Lib.Harmony NuGet package. Its license is in `dependencies/Harmony-LICENSE.txt`. Time2Work is optional. The personal ChicagoBuildBridge is not a subscriber requirement.

Install the game's code-modding toolchain and run `Build.ps1 -Compiler <path-to-Roslyn-csc.exe>` in PowerShell 7. The game toolchain supplies `CSII_MANAGEDPATH`, `CSII_MODPOSTPROCESSORPATH` and `CSII_UNITYMODPROJECTPATH`. The script compiles and postprocesses the DLL and writes a package manifest in `build`.

Gameplay validation for 1.75.637 included 5,259 offline checks, the maintenance regression and 131 policy cases; native corpse cleanup, efficiency penalties, recruitment/hiring, utilities, resources, transit fallback, building protections and repeatable reset tests; settings migration/persistence and visible Options checks. See `validation-1.75.637.json` for evidence and scope.

Matched simulation runs reloaded the same populated test save for each configuration, warmed up for 20 seconds and measured 45 seconds. With the new cleanup/recruitment controls off/on, normal speed advanced 2,590/2,608 frames; 4x advanced 7,092/6,939 frames. No mod or simulation exceptions appeared in the reviewed logs. One comparison in a small city cannot establish large-city or long-duration performance. The playset also logged an unrelated ChicagoCoverageCache compatibility disable and asset warnings.

## 1.75.632 compatibility update

The assembly version now matches the release and file versions instead of displaying 0.0.0.0. Rebuilt against 1.6.4f1 without changing simulation policies or saved setting keys. Runtime checks on 1.6.4f1 passed within the scope recorded in `validation-1.6.4f1.json`. The update was submitted to the existing Paradox Mods listing on November 13, 2026.

## Efficiency Protection

**Protect Efficiency** now holds individual weather and city-bonus/specialization factors against decreases. It never replaces the entire efficiency-factor list. Electricity, water, sewage, dirty water, staffing, employee health/happiness, budget, fees, garbage, telecom, mail and material/resource shortages retain their current native factors. Unknown future factors are also left alone. Removed factors stay removed; a closed, destroyed, burning or abandoned building resets its bonus baseline. After turning off an unlimited utility, its native shortage penalty can take effect even while Protect Efficiency remains enabled.

This intentionally narrows efficiency protection: total efficiency can fall when an operational requirement is not met. Weather factors retained by this control are wind speed, water depth and sunlight. City-modifier and specialization bonuses are protected only while the factor still exists.

## Remaining Interactions

- Employment Levels = Reset to ZERO can cause layoffs. It cannot run together with the recruitment mode. Employment Maximize is removed.
- Land Value = Reset to ZERO & Freeze Rent intentionally combines zero land value with holding existing lease prices. Other land-value modes do not freeze rent. The retired rent checkbox is migration-only; existing land-value choices are never silently changed to Zero.
- Maximum Building Occupancy overrides the resident-immigration hold; births still follow Stop Population Growth. New tenants increase population or business activity and may increase staffing, goods, traffic and service demand.
- Coverage benefits do not provide response vehicles, staffing or road access. Communications coverage affects the native telecom efficiency factor; Protect Efficiency still does not freeze that factor.
- Other mods can write the same fields. A separate playset compatibility audit is deferred until after publication and a new user handoff; no universal compatibility claim is made.

- City-Service Staffing and its hiring/roster-retention hooks are removed. Native service hiring and departures can proceed; Employment Levels still protects the number of jobs offered.
- Stop Population Growth, frozen age and frozen schooling can limit the future supply of qualified workers. Employment Levels = Freeze holds offered job capacity.
- Protect Residents still pauses death checks and household departures. Removing Protect Lifespan does not disable that broader protection.
- Protect Happiness can raise health or well-being to maintain happiness even if their individual protections are off.
- Hold Automatic Level Changes, Protect Tenants, Prevent Deterioration and Protect Vacant Buildings overlap in avoiding condition-related abandonment. Removing Freeze Abandonment does not remove these separate safeguards. Existing occupied abandoned buildings no longer receive the removed blanket collapse-timer freeze; the repeatable Reset Abandonment button remains.
- Daily Customers, Income and Profit protect reported statistics; Freeze can hide changes in actual shopping and economic activity. Resource/storage replenishment can change production, trade and traffic.
- Enable All Options selects Freeze in every remaining dropdown, including schooling. Disable All Options selects Normal.
