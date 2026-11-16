# Focused QA: demand and households, 1.75.644

Status: focused checks passed. See validation-1.75.644.json for observed results and limitations.

The user authorized this QA session and publication after passing. The recovery checkpoint was reloaded and user settings restored. Future live runs still require a fresh game-control handoff.

## One-session scope

1. Record the loaded city, current settings and Demand+ master state. Verify pause and make a separate recovery checkpoint. Install the candidate only after a graceful game exit, then restart into a disposable QA copy. Verify the loaded assembly is 1.75.644 and matches the offline candidate hash.
2. Check all seven new labels, alphabetical ordering and help layout. Verify upgrade from schema 9 starts them off and retains existing choices. Do not reset the user's settings to test new-install defaults; those passed offline checks.
3. With Demand+ off, verify each density and business demand switch affects its own category. Include both a mixed selection and all six on. Allow native demand updates between observations. Check actual commercial/industrial resource demand arrays as well as the six displayed values; full bars alone do not prove simulation behavior. Storage/extractor and unselected resource categories must remain native.
4. Exercise master-off and individual-off transitions. Confirm native calculation resumes on subsequent demand updates, without job-safety errors. Do not expect the old value to reappear immediately or already-built buildings to disappear.
5. In the QA copy only, briefly enable Demand+ and confirm our demand controls become unavailable and our overrides stop. Disable Demand+ and verify the saved Puppet Master selections become available again. Restore the user's original Demand+ state before handoff.
6. Check Maximum Households in occupancy-only mode with suitable vacant homes and an outside connection. Confirm recruitment uses the same queue, respects available homes and the 64-household backlog, and does not exceed the 16-household batch. Existing City Growth modes must not receive a second spawn batch. Compare household capacity before/after; capacity must not increase. Homes are filled gradually, so instant occupancy is not an acceptance criterion.
7. Select Freeze Population Growth. Maximum Households must clear, lock and stop recruitment. Leaving Freeze must unlock it without selecting it. Check that redevelopment hold still stops zoned construction; demand must not bypass that setting. Verify no new recurring errors or obvious simulation stall during the focused run. Investigate any new performance concern before broadening timing tests.
8. Reload the QA baseline and restore user settings. Confirm a stable paused frame, report the tested scope and return control. Preserve the original city. Keep publication pending until live results pass and publication is authorized.

## Record only observed results

Capture candidate hash, game version, save/session, before/after settings, selected demand masks, native demand values, household counts/capacities, any errors and the final restored state. Carry prior unrelated transit and maintenance results forward with their original version labels; do not rerun them without a new concern.

## Recorded scope

- 9,935 live fixture checks passed, including demand/resource isolation, master-off, native restoration, household freeze controls and unchanged home capacities.
- The real waiting queue correctly prevented added households. A temporary empty-queue fixture created four households normally and five with the boost; native outside-connection selection can stop below the sixteen-household budget. Created fixture households were removed and the checkpoint was reloaded afterward.
- All seven new controls were enabled during a 630-frame normal-speed run. All six native demand results were 100, and the redevelopment hold still blocked zone spawning.
- Demand+ was absent. Its enabled/disabled ownership policy passed offline tests; live co-operation with an active Demand+ instance was not exercised.
- One test-report serialization error was fixed in the helper. No subsequent errors were found during the focused run.
