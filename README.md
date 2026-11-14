# GPT's Puppet Master by X_Bean_X

A Cities: Skylines II code mod for Windows. Use the full-color **Puppet Master riverfront** button in the upper-left toolbar to open **GPT's Puppet Master** in Options.

## Buildings

**Protect Vacant Buildings** guards empty rentable buildings against abandonment caused by poor condition. If a property has already been abandoned, the setting holds its countdown to demolition. **Prevent Deterioration** protects condition during building upkeep.

The **Vacancy And Abandonment** section appears at the bottom of the Options page. **Reset Vacant Buildings** and **Reset Abandonment** run once per click and can be used repeatedly in the same city or session. Resetting clears negative condition and restarts eligible demolition timers. Fire, disasters and manual bulldozing have separate effects. A reset does not return an already-abandoned property to service.

## Population and services

**Enable Mod** is first, followed by six alphabetical dropdowns: City Growth, Daily Customers, Employment Levels, Land Value, Profit and Schooling. Applicable target sliders appear directly below their dropdown. Bulk buttons follow, then checkboxes, with vacancy protections and reset buttons last.

City Growth offers Freeze Population Growth, Maximize Population Growth, Maximize Population Growth and Occupancy of All Commercial & Residential Buildings, and Maximize Occupancy of All Commercial & Residential Buildings. Occupancy-only admits newcomers to fill existing homes and commercial premises but pauses births; combined mode also uses the larger ongoing immigration batch and permits native births. Population-only recruits households without the commercial filling pass. These modes do not construct buildings or bypass housing capacity.

Freeze Population Growth unchecks and disables both industrial and civic output checkboxes, including their runtime functions. Any other growth mode makes them selectable again without automatically rechecking them. Turning Enable Mod off suspends all mod controls and permits native growth.

First-install/reset defaults select Freeze in all six dropdowns. Of the 30 visible checkboxes (including Enable Mod), all are enabled except the two output checkboxes disabled by Freeze Population Growth. Enable All Options applies these same defaults while preserving target values. Disable All Options turns the master and checkboxes off and selects Normal where available; City Growth retains Freeze because it has no Normal choice. Reset buttons are never triggered by bulk actions.

Schooling retains its four modes. Normal Schooling | No Admissions lets existing students progress and graduate while pausing admissions. **Frozen schooling can limit qualified workers. Recruitment cannot guarantee enough workers with the required education.** This warning appears under Schooling, Employment Levels and both output controls.

Existing settings are migrated. Removed Income, Income Target and Protect Efficiency keys are ignored; their runtime overrides are removed. The former separate occupancy, growth and shopping choices migrate into the new dropdowns. An older configuration with occupancy off maps to Freeze Population Growth because the new four-mode dropdown has no Normal setting.

## Numeric Controls

Daily Customers, Land Value and Profit offer Normal, Freeze, Prevent Decreases, Maximize and Reset to ZERO. Daily Customers also offers Maximize Shopping Activity; Land Value also offers Reset to ZERO & Freeze Rent. Employment Levels offers Normal, Freeze, Prevent Decreases and Boost Outside Worker Recruitment. Employment Maximize and Zero are removed. The entire Income dropdown and Income Target are removed; household and company income remain under native simulation.

Daily Customers defaults to target 1,000 (range 1-100,000), Profit to 10,000 (range 1-1,000,000), and Land Value to 100 (range 1-10,000). These are adjustable mod targets, not verified universal game maxima. Each target is editable and applied only while Enable Mod and its corresponding Maximize choice are active. Bulk actions preserve customized targets.

Reset to ZERO holds the selected value at zero continuously, including new eligible entities. Normal or master-off stops the override; native recalculation resumes on its own schedule and does not undo consequences already produced. Land Value updates road-edge values and the map together. Reset to ZERO & Freeze Rent also holds existing lease prices; new leases begin at their native price. Other land-value choices do not freeze rents.

Customer and Profit statistic overrides do not create purchases or cash. Maximize Shopping Activity is an exclusive Daily Customers choice and leaves customer statistics unheld. Profit can still be frozen or overridden independently and conceal changes in actual trading. The historical customer record remains untouched.

## Outside Worker Recruitment

The **Employment Levels > Boost Outside Worker Recruitment** selection adds a bounded commuter-recruitment pass while native spawning continues. It checks vacancies across all five education levels against the available workforce. More-qualified available workers can cover lower-level openings; workers cannot cover jobs above their qualifications. Incoming commuters still receive their education through the native initializer. This is not worker training and does not guarantee a particular skill mix or successful hire.

At most 16 extra adult workers are recruited per 256 simulation frames. The boost reserves room for the native planned batch and commuters still waiting for work; it stops adding workers when that waiting/reserved pool reaches 64. It retains the native commuter-to-worker limit and only uses eligible adult-only household prefabs and native outside-connection selection. No suitable prefab, usable connection or remaining room means no extra recruitment. Vanilla recruitment is unchanged by these boost limits.

Freeze Population Growth can remain selected because commuters are separate from resident immigration. The recruitment mode does not modify residents, schooling, offered job capacity, employee rosters or efficiency factors. Native routing and job matching still decide hires. The removed City-Service Staffing control no longer blocks service hires. Added commuters can increase traffic and simulation work. Disabling the boost stops new extra batches; existing commuters remain in normal simulation. An already scheduled batch can finish.

Fresh defaults and Enable All Options select Freeze for Employment, so recruitment is not enabled at the same time. Native recruitment/hiring was tested in the prior 1.75.637 release; this revised dropdown wiring still requires its own live release check.

## Customers, travel and resources

The Employee Count, Maximum Customers, Hire All Available Jobs, Protect Lifespan, Freeze Abandonment, City-Service Staffing, Protect Efficiency and Income controls have been removed. Old saved keys are ignored. Shopping and ordinary business hiring use their normal game behavior; Daily Customers still controls the reported customer statistic. Worker training and a workforce status panel are not included.

**Prefer Public Transportation** tries available public transport, walking and cycling before driving or taxis for new citizen routes. If that route search fails, the exact original route choices are tried once. Vehicle/service/freight path requests are excluded. Existing trips continue. This does not create routes, vehicles or capacity, and routes with no suitable transit can require a second path search.

**Maximum Resources Available** periodically replenishes business inputs, shop goods and city-service supplies within native inventory limits. Industrial inputs leave capacity for output. Money is excluded. Native extraction uses a temporary resource-map copy so existing oil, ore, fertility and fish deposits are not depleted by extraction; normal production bookkeeping continues. It does not create deposits on barren ground, regrow trees, or remove pollution effects.

**Maximum Storage** periodically refills warehouses and cargo storage with their accepted physical goods up to existing shared capacity, including warehouse level/lot size and active storage upgrades. It does not enlarge storage. Turning either inventory checkbox off stops replenishment; goods already added remain and can be used or sold normally. Artificial supplies can change imports, exports and traffic.

## Citywide coverage benefits

**Maximum Police Coverage** and **Maximum Fire Coverage** provide the strongest finite coverage magnitude found in the loaded service prefab definitions across existing road coverage buffers. They do not inflate provider ranges or add coverage path searches. Higher existing coverage is retained, and unrelated service types are untouched. These are citywide road-coverage benefits, not instant police/fire response, immunity to crime/fire, or added staff/vehicles. Areas without a road coverage buffer are not given fake roads.

**Communications & Internet** sets the native telecommunications map to its maximum sampled quality (100%). Internet and telecom use this same coverage map. It does not fabricate tower capacity or change their reported hardware statistics. All coverage switches resume native recalculation when disabled; restoration occurs on the respective native system's update schedule.

## Maximize Shopping Activity

Select Daily Customers > Maximize Shopping Activity to remove the population-based reduction from the native household shopping lottery. Only the scheduled job's private parameter copy is changed. The global economy configuration stays native. Repeat-purchase reduction, existing needs, available cash, priorities, routing and native transaction processing remain in effect. Successful purchases use goods transfers and real payments; shoppers, sales and money are not fabricated.

This choice cannot run alongside a customer-statistic hold or target. Profit overrides remain independent and can mask changes in reported profit. This can increase shopping traffic and does not guarantee purchases. Fresh defaults and Enable All Options select Freeze, so shopping activity is off until selected.

## Maximum Building Occupancy

Occupancy is now part of City Growth. The occupancy-only and combined modes fill eligible residential and commercial vacancies using real native households and companies. Household initialization and native property searches determine placement. Population, renter lists and employee rosters are not fabricated. Service users, hotel guests and extractor sites are not filled by this residential/commercial setting.

Occupancy-only recruits up to four households per 256 simulation frames; population and combined modes recruit up to 16. Company filling adds at most two eligible companies per 256 frames. Existing unplaced tenants reserve vacancies; backlogs are capped at 64 households and 16 companies. Access, suitability and capacity still apply. These are bounded recruitment attempts, not guaranteed absolute maximum growth or instant full occupancy.

**Maximize Occupancy & Production Output of Industrials Buildings** fills eligible industrial/storage premises and targets native designed job capacity for industrial employers. Offices are excluded. Production still uses actual workers, inputs and storage through native processing.

**Maximize Occupancy & Services Output of Civic Buildings** targets native service staffing capacity, including upgrades and school staffing rules tied to enrollment. It does not create students, patients or detainees, enlarge service-user capacity, change budgets or add response vehicles.

Both output controls enable bounded outside-worker recruitment and take priority over Employment Freeze for the targeted employers. They do not fabricate workers or guarantee hires. **Frozen schooling can limit qualified workers. Recruitment cannot guarantee enough workers with the required education.**

The output controls preserve the game's normal capacity and resource requirements; they do not impose a new arbitrary production cap. Shortages can keep actual output below the target. Separate Maximum Resources, Maximum Storage and Unlimited Utility switches retain their own effects and can satisfy relevant shortages when enabled. Freeze Population Growth clears and disables both output controls.

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

The old city-specific hold option and its automatic configuration-file controller have been retired. Use **City Growth > Freeze Population Growth** in any city.

## Release validation status

The City Growth / Daily Customers reorganization, industrial/civic output controls, and removal of Income and Protect Efficiency are implemented. Offline verification passed 4,830 checks, maintenance regression, 365 policy cases and the toolbar contract. The installed candidate passed 41 live fixture assertions and visible Options checks. Real initialized households and companies acquired leases in the QA city, and native purchases continued with Shopping Activity enabled.

The revised 1.75.639 build is installed locally for QA and is not published. Final matched performance verification remains pending: interrupted runs are excluded. See validation-1.75.639.json for exact scope, measurements and limitations. Compatibility testing against the user's other mods is deferred until after release and a separate handoff.

## Build

Version **1.75.639** targets Cities: Skylines II **1.6.4f1** on Windows. Renamed to GPT's Puppet Master, with the approved riverfront icon and T2 thumbnail. Internal mod identity and saved setting keys are retained. Three numeric dropdowns have Maximize targets; employment has recruitment/hold modes; Income and Protect Efficiency are removed. Land Value adds Reset to ZERO & Freeze Rent. Schooling is unchanged. The package includes Harmony 2.2.2 from the official Lib.Harmony NuGet package. Its license is in `dependencies/Harmony-LICENSE.txt`. Time2Work is optional. The personal ChicagoBuildBridge is not a subscriber requirement.

Install the game's code-modding toolchain and run `Build.ps1 -Compiler <path-to-Roslyn-csc.exe>` in PowerShell 7. The game toolchain supplies `CSII_MANAGEDPATH`, `CSII_MODPOSTPROCESSORPATH` and `CSII_UNITYMODPROJECTPATH`. The script compiles and postprocesses the DLL and writes a package manifest in `build`.

Gameplay validation for 1.75.637 included 5,259 offline checks, the maintenance regression and 131 policy cases; native corpse cleanup, efficiency penalties, recruitment/hiring, utilities, resources, transit fallback, building protections and repeatable reset tests; settings migration/persistence and visible Options checks. See `validation-1.75.637.json` for evidence and scope.

Matched simulation runs reloaded the same populated test save for each configuration, warmed up for 20 seconds and measured 45 seconds. With the new cleanup/recruitment controls off/on, normal speed advanced 2,590/2,608 frames; 4x advanced 7,092/6,939 frames. No mod or simulation exceptions appeared in the reviewed logs. One comparison in a small city cannot establish large-city or long-duration performance. The playset also logged an unrelated ChicagoCoverageCache compatibility disable and asset warnings.

## 1.75.632 compatibility update

The assembly version now matches the release and file versions instead of displaying 0.0.0.0. Rebuilt against 1.6.4f1 without changing simulation policies or saved setting keys. Runtime checks on 1.6.4f1 passed within the scope recorded in `validation-1.6.4f1.json`. The update was submitted to the existing Paradox Mods listing on November 13, 2026.

## Efficiency Protection

Protect Efficiency and its weather/bonus baseline writer have been removed. Efficiency follows native simulation and any separately selected utility, resource or coverage controls. Those independent controls retain their documented effects.

## Remaining Interactions

- Freeze Population Growth clears and disables industrial/civic output; leaving Freeze makes them selectable but leaves them unchecked.
- Industrial/civic native staffing targets take priority over Employment Freeze for their scoped employers. Production and services still depend on workers, qualifications, supplies, budget and access. Separate utility and inventory switches can relieve their corresponding shortages.
- Frozen schooling and age can restrict the future qualified workforce. Recruitment is not training and cannot guarantee the required skill mix.
- Shopping Activity is exclusive with customer-statistic overrides. Profit overrides remain independent and can mask reported trading changes. Income overrides are removed.
- Coverage benefits do not provide response vehicles, staffing or road access.
- Protect Residents pauses death checks and household departures; it does not block cleanup of citizens already dead.
- Protect Happiness can raise health or well-being to maintain happiness even if their individual protections are off.
- Building-level, tenant, deterioration and vacancy safeguards overlap. Repeatable resets remain; they do not restore already-abandoned buildings to operation.
- Resource/storage replenishment can change production, trade and traffic.
- Other mods may write the same values. A separate compatibility audit remains deferred until after publication and a new user handoff; no universal compatibility claim is made.
