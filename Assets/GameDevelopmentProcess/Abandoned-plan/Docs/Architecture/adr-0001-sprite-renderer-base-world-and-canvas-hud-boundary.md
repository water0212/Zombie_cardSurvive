# ADR-0001: SpriteRenderer 基地世界與 Canvas HUD 邊界

## Status

Proposed

## Date

2026-08-26

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 2022.3.62f3 |
| **Render Pipeline** | Universal Render Pipeline 14.0.12 |
| **Renderer** | URP 2D Renderer (`Settings/Renderer2D.asset`) |
| **Domain** | Rendering / UI / Input / Animation |
| **Knowledge Risk** | LOW — pinned LTS version and local project settings verified |
| **References Consulted** | `../ProjectSettings/ProjectVersion.txt`, `../Packages/manifest.json`, `Settings/UniversalRP.asset`, `Settings/Renderer2D.asset`, `../ProjectSettings/GraphicsSettings.asset` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | P0 integration slice at 1280x720 and 1920x1080; Light2D, normal-map, shadow, input-lock and particle profiling |

The project does not yet contain `docs/engine-reference/unity/`, `.agents/docs/technical-preferences.md`, or an architecture registry. Engine compatibility is therefore based on the pinned local project configuration and must be rechecked if the engine or URP package changes.

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | None |
| **Enables** | Gameplay HUD world-art production and the central-world migration story |
| **Blocks** | P0 HUD integration slice cannot be accepted until this ADR is accepted |
| **Ordering Note** | Produce and validate one integration slice before migrating all buildings and zombies |

## Context

### Problem Statement

The central base is a fixed, non-scrollable play space, but it must feel like a living place rather than a collection of static UI images. Buildings, zombies and the environment need dynamic 2D lighting, restrained shadows, smoke, embers, dust, animated functional parts and low-frame idle motion. At the same time, permanent HUD information, cards, labels and flow overlays must remain resolution-stable, readable and compatible with the existing Canvas-based interaction flows.

### Constraints

- The player cannot pan or zoom the base camera in the current design.
- The 16:9 core safe frame must show the same interactive information at every supported aspect ratio.
- The Demo supports up to seven buildings and twelve zombies at once.
- Zombies must always render in front of buildings.
- Event and exploration flows can reveal the base while allowing only Card Warehouse and Return controls.
- Runtime systems remain the only authority for game state; rendering components only present and forward input.
- The existing card, event, exploration and menu UI remains Canvas-based.
- The visual language is clean parchment, soft charcoal and restrained semantic colour rather than photorealistic or glossy rendering.

### Requirements

- Support URP 2D Light, Sprite-Lit materials, secondary normal textures and controlled ShadowCaster2D usage.
- Support pooled ParticleSystem effects and independently animated building parts.
- Keep TMP labels and critical interaction targets sharp at 1280x720.
- Keep world interaction lockable by the same flow state that locks HUD interaction.
- Separate card artwork from world artwork.
- Preserve the established Gameplay runtime data and state-change events during migration.

## Decision

The central base will be rendered as a SpriteRenderer world under a fixed orthographic camera using the project's existing URP 2D Renderer. Permanent HUD, cards, notifications, details and flow overlays remain Canvas UI. World labels are projected into one shared `WorldLabelCanvas` so text and interaction targets do not inherit world lighting or become blurry.

### Architecture Diagram

```text
Orthographic World Camera
└─ WorldRoot
   ├─ Backdrop                 SpriteRenderer / Unlit
   ├─ Ground                   SpriteRenderer / Lit
   ├─ Props and Buildings      SpriteRenderer / Lit
   ├─ Zombies                  SpriteRenderer / Lit
   ├─ World Lights             Global + local Light2D
   ├─ World Shadows            ShadowCaster2D + blob shadows
   └─ World VFX                pooled ParticleSystem

WorldLabelCanvas
└─ pooled labels projected from world anchors
   ├─ maximum health / attack
   ├─ effect-type icons
   └─ kill allocation button

HUDCanvas
├─ morale, date, resources and shortcuts
├─ hand, draw pile and discard pile
├─ notifications and details
└─ event, exploration, menu and failure overlays
```

### Rendering Boundary

- `World_Backdrop`, `World_Ground`, `World_Decal`, `World_PropBack`, `World_Building`, `World_Zombie`, `World_FX` and `World_Overlay` are Sprite sorting layers.
- Buildings and zombies use `SortingGroup`; main artwork, animated parts, shadows and local VFX use consistent local orders.
- Buildings use required low-strength normal maps. Zombies may use lower-strength normal maps when they improve form without creating a plastic appearance.
- UI paper, text, icons and labels are not affected by Light2D.
- One Global Light2D provides stable base illumination. Local lights communicate campfire, windows and functional activity.
- Only large structures use simplified ShadowCaster2D by default; zombies use blob shadows unless profiling permits otherwise.

### Key Interfaces

The exact serialized shape can change during implementation, but the responsibility boundary must remain:

```text
BuildingWorldSpriteView.Bind(BuildingRuntime, BuildingWorldVisualProfile)
ZombieThreatSpriteView.Bind(ZombieThreatRuntime, ZombieWorldVisualProfile)
WorldLabelController.Bind(runtimeId, worldAnchor)
WorldInteractionRouter.SetInputLocked(bool)
```

- `BuildingWorldVisualProfile` and `ZombieWorldVisualProfile` own world prefabs, visual states, label anchors and effect anchors.
- Card artwork fields are not reused as world artwork fields.
- `WorldInteractionRouter` checks the authoritative flow lock before every Physics2D hover, click or card-target query.
- `WorldLabelController` uses `Camera.WorldToScreenPoint()` and one shared Canvas; individual entities do not create their own Canvas.
- Labels use the same runtime ID as their world object so kill allocation and details cannot bind to the wrong threat after pooling or rearrangement.

### Input Priority

```text
forced flow lock
→ HUD GraphicRaycaster
→ WorldLabelCanvas actions
→ card drag target query
→ zombie Physics2D collider
→ building Physics2D collider
→ ground / clear selection
```

An opaque or transparent Canvas overlay is not sufficient by itself. When Gameplay is locked, the shared flow state also disables Physics2D world interaction.

## Alternatives Considered

### Alternative 1: Canvas-only central world

- **Description**: Keep buildings and zombies as `Image` components and imitate life with UI shaders, RectTransform animation and overlay particles.
- **Pros**: Smallest migration and one input system.
- **Cons**: Light2D and ShadowCaster2D do not naturally affect Canvas Images; spatial particles, sorting and world-material behaviour require custom workarounds.
- **Rejection Reason**: It cannot deliver the desired dynamic world cleanly and would turn each new visual feature into a Canvas-specific exception.

### Alternative 2: SpriteRenderer world plus Canvas HUD

- **Description**: Use the existing URP 2D Renderer for the central world, while keeping screen information and flow UI on Canvas.
- **Pros**: Native 2D lighting, shadows, particles and world sorting; clear visual and responsibility boundary.
- **Cons**: Requires a parallel world-view migration, Physics2D input and projected labels.
- **Selection Reason**: Best matches the requested living-base presentation without sacrificing HUD readability or changing Gameplay rules.

### Alternative 3: Full World Space Canvas or 3D scene

- **Description**: Move both the world and most HUD presentation into World Space Canvas or rebuild the base with 3D meshes.
- **Pros**: A single spatial composition and potential future camera motion.
- **Cons**: Unnecessary complexity, text scaling problems, larger art and code migration, and no current gameplay need for free camera movement.
- **Rejection Reason**: Exceeds the fixed-view Demo scope and adds cost without improving current player decisions.

## Consequences

### Positive

- The base can communicate time, function and threat through native dynamic lighting and world-space effects.
- Buildings and zombies remain separate interactive objects instead of being baked into a flat composition.
- HUD text and controls stay crisp and predictable across supported resolutions.
- Art production gains explicit albedo, normal, pivot, shadow and animation requirements.
- Camera movement can be reconsidered later without forcing it into the current design.

### Negative

- Existing `BuildingWorldView` and `ZombieThreatView` Canvas prefabs cannot be used as the final world views.
- Input is split between UI raycasting and Physics2D and therefore needs one routing policy.
- Each lit world asset needs additional normal-map, prefab and effect-anchor work.
- Sorting layers, light target layers and pooling must be configured deliberately.

### Risks

- **Excessive normal strength** could make charcoal art look plastic. Mitigation: exclude paper grain and line work from normals and validate at 0.15–0.35 visual strength.
- **Lighting cost** could exceed the Demo budget. Mitigation: maximum six local lights, one or two shadow-casting local lights, simplified casters and a profiled P0 slice.
- **Input leakage** could allow Gameplay interaction behind overlays. Mitigation: shared flow lock checked by both Canvas and `WorldInteractionRouter`.
- **Label drift** could detach UI from pooled world objects. Mitigation: runtime-ID binding, shared label pool and frame-by-frame projection only while visible.
- **Sorting mistakes** could place zombies behind buildings. Mitigation: separate locked sorting layers plus Bottom Center pivots and Y sorting within each layer.

## GDD Requirements Addressed

| Source | Requirement | How This ADR Addresses It |
|---|---|---|
| `design/ux/hud.md` | Fixed central base with all buildings and zombies visible and clickable | Uses a fixed orthographic camera and a bounded world safe frame |
| `design/ux/hud.md` | Zombies appear in front of buildings | Uses separate `World_Building` and `World_Zombie` sorting layers |
| `design/ux/hud.md` | HUD remains readable and stable across resolutions | Keeps HUD and labels on Canvas with a 1920x1080 reference layout |
| `design/ux/hud.md` | Event/exploration temporary view locks all Gameplay except Card Warehouse and Return | A shared flow lock disables both Canvas and Physics2D world input |
| `Docs/GameDesign/ZombieSurvivalCard_GDD.md` | Runtime-generated buildings and field zombies remain interactive Gameplay objects | Separates runtime authority from reusable world-view prefabs and profiles |

## Performance Implications

- **Target**: 60 FPS at 1280x720 and 1920x1080 on the Demo PC target.
- **CPU**: Physics2D queries use layer masks and non-allocating overlap queries; labels and particles use pools.
- **GPU**: approximately 60–80 visible SpriteRenderers, up to 20 simplified ShadowCaster2D objects and no more than six local Light2D objects.
- **Particles**: approximately 200–250 active particles, up to six looping and four simultaneous one-shot emitters; collision and trails disabled by default.
- **Draw calls**: world target below 100 and total frame target below approximately 150 during the integration slice.
- **Memory**: paired albedo and normal textures increase texture memory; building atlases are split rather than forcing a single 4096 RGBA atlas.
- **Network**: none.

These are initial Demo budgets, not promises for every target device. The P0 integration slice must measure them before ADR acceptance.

## Migration Plan

1. Preserve current runtime systems and Canvas views as a rollback reference.
2. Add world sorting layers, Physics2D layers and shared Sprite-Lit/Unlit/particle materials.
3. Create `WorldRoot`, fixed orthographic composition and one `WorldLabelCanvas`.
4. Implement parallel `BuildingWorldSpriteView`, `ZombieThreatSpriteView`, visual profiles and `WorldInteractionRouter`.
5. Build a P0 slice with backdrop, ground, Intelligence Center, Workbench, one Wandering Zombie, labels, one local light and core particles.
6. Verify input lock, card targeting, 1280x720 readability, greyscale readability and reduced-motion behaviour.
7. Profile 7 buildings, 12 zombies, lights, shadows and particles.
8. Accept or revise this ADR before migrating the remaining buildings and zombies.
9. Remove obsolete Canvas world views only after feature parity and regression checks.

## Validation Criteria

- The P0 slice maintains 60 FPS at 1280x720 and 1920x1080 under the defined stress case.
- Seven buildings and twelve zombies remain visible, correctly sorted and individually clickable.
- Zombies always render in front of buildings without losing building access.
- Event/exploration temporary viewing cannot interact with world Physics2D objects.
- World labels remain aligned and readable at both target resolutions.
- Campfire and building lights affect only world Sprite-Lit assets, not HUD or labels.
- Reduced-motion mode lowers particle and flicker intensity without hiding state information.
- Albedo and normal pairs contain no visible fringe, atlas mismatch or plastic paper texture.
- Existing card, event, exploration, combat and building runtime behaviour remains unchanged.

## Related Decisions

- `design/ux/hud.md`
- `design/art/art-bible.md`
- `design/assets/specs/gameplay-hud-assets.md`
- `design/ux/morale-feedback-flow.md`
