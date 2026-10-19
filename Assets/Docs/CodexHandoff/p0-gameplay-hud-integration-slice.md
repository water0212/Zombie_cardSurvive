# P0 Gameplay HUD Integration Slice

> Updated: 2026-09-06  
> Status: Runtime binding and first functional review complete; full P0 acceptance gate remains open.

## Deliverable

- Validation scene: `Scenes/GameplayHUD_P0_Integration.unity`
- Rebuild command: `Tools > Zombie Card Survive > Gameplay HUD > Build P0 Integration Slice`
- Latest visual evidence: `Temp/CodexP0/p0_runtime_final.png`
- Generated source assets and prompts: `Art/GameplayHUD/P0/`

The scene is deliberately separate from `SampleScene`. It proves the SpriteRenderer-world / Canvas-HUD boundary while preserving the current playable scene as the rollback path.

## Implemented in this slice

- Fixed orthographic world camera and 1920×1080 Scale With Screen Size HUD.
- SpriteRenderer parchment backdrop, charcoal ground, Intelligence Center, Workbench and one Wandering Zombie.
- URP 2D Global Light, campfire Point Light, ember particles and an Intelligence Center ShadowCaster2D.
- Permanent HUD regions: time-of-day, morale and threshold, horizontal resources, left shortcut rail, five-card hand, draw/discard, notification, details and input-lock tester.
- Shared `WorldLabelController` projecting the zombie label to Canvas.
- `WorldInteractionRouter` with UI-first input priority, Physics2D non-alloc hit testing and flow/input lock rejection.
- Zombie maximum-health and attack display; clicking allocates or cancels the whole kill cost instead of showing partial HP.
- Separate `BuildingWorldVisualProfile` and `ZombieWorldVisualProfile` data types so card artwork does not become the world-art authority.
- Fixed right-side 3×4 threat-slot controller foundation.
- Event-driven `GameplayHudRuntimePresenter` binding for morale, food, resources, combat, energy, hand, draw pile and discard pile.
- P0 scene builder now creates a real `RunPhaseController` with the fixed Demo morale Profile, so day and time-of-day no longer rely on prefilled labels after the scene is rebuilt.
- Reserved food/resource are shown separately; the resource main number remains the freely available amount required by the HUD spec.

## Verified

- Unity script compile: pass.
- Play Mode boot: pass.
- Console after Play Mode: 0 errors, 0 warnings.
- Explicit world lock toggle: `LOCKED`, then restored to `UNLOCKED`.
- Kill allocation: combat value 6 → 2 for a 4-health zombie; runtime kill mark became true.
- Existing `SampleScene` was not migrated or removed.
- Runtime mutation check: food `23/30 +4 temporary`, resource `19`, energy `4/5`, morale `32/100`, Stage `Brink`; all visible values refreshed from state events.
- External low-concurrency C# build on 2026-09-06: pass, 0 warnings and 0 errors. This did not start Unity.

## Static review and corrections 2026-09-06

- Resource reservation commits now finish deduction and reservation removal before publishing state events. Subscribers can no longer observe the previous half-committed state, and reentrant release cannot decrement another reservation.
- Food/resource turn-start production now publishes one final notification after all production effects tick and expire.
- `MoraleSystem.Reset()` now publishes a final reset snapshot so an enabled HUD does not retain stale morale.
- Reconfiguring an enabled HUD presenter now unsubscribes from the old controller and subscribes to the new controller.
- Missing card runtime now hides existing hand roots instead of leaving mock or stale cards visible.
- Added targeted tests for reservation transaction consistency, callback reentry, production notification batching, morale reset notification and presenter rebinding.

The generated Unity project files had not yet discovered the two newly added test files while Unity was closed. The existing edited tests and all current runtime/editor scripts compile externally. After Unity restarts and imports the files, run only the targeted tests first; do not immediately run the full EditMode suite.

## Unity memory crash note

The last full EditMode test attempt on 2026-09-04 did not complete. `Editor.log` showed system/pagefile use reaching 97%, followed by profiler-frame drops and an editor crash. This is recorded as a memory-environment failure, not as a passed suite and not as a confirmed gameplay-code failure.

## Still required before ADR-0001 acceptance

- 1280×720 capture and complete safe-area verification.
- Seven-building / twelve-zombie stress and clickability pass.
- Greyscale, red-weak and green-weak accessibility captures.
- Production normal-map pairs and transparent-fringe checks.
- Final icon art, TMP fallback audit, reduced-motion controls and 60 FPS profiling.
- Connect the parallel SpriteRenderer views to the full SampleScene building and zombie spawning flows, then retire the Canvas world views only after regression checks.
- Rebuild `GameplayHUD_P0_Integration.unity` once after Unity restarts so the new `RunPhaseController` and Profile references are serialized into the scene.
- Run only `ValueSystemStateChangedTests`, the two new reservation tests in `BuildingRuntimeTests`, and `GameplayHudRuntimePresenterTests`; inspect Console and memory before expanding test scope.
- Close remaining HUD-spec gaps: 10-card presentation, clickable zombie health target, shared Canvas/world flow lock, threat-slot safe area, 1280×720 font/contrast pass and explicit uninitialized/loading snapshot.

## Visual revision 2026-08-28

- Replaced the bright full-screen parchment with `world_backdrop_parchment_02_dark_fractured_alpha.png`.
- Added `ui_panel_torn_gray_03_readable.png` as the shared sliced panel texture; two darker attempts blended into the background and are retained only as source.
- Darkened the camera underlay and panel palette while changing primary text to a light parchment colour.
- Kept HUD positions and Gameplay interactions unchanged.
- The scene was rebuilt and captured before the Unity crash; `p0_runtime_final.png` is the latest verified image. The 2026-09-06 static corrections require one further rebuild through the same menu after Unity restarts.
