# Asset Specs — System: Gameplay HUD

> **Source**: `design/ux/hud.md`  
> **Architecture**: `Docs/Architecture/adr-0001-sprite-renderer-base-world-and-canvas-hud-boundary.md`  
> **Art Bible**: `design/art/art-bible.md`  
> **Generated**: 2026-08-26  
> **Status**: 45 new asset families approved / 1 existing asset family referenced / 0 in production / 0 done

## Scope

This specification covers the permanent Gameplay HUD, the fixed central base world, world labels, five implemented building types, three implemented zombie types, card-pile presentation, feedback VFX, dynamic 2D lighting and the particle assets needed for the first playable visual slice. Full event, exploration, encyclopedia, settings, card-warehouse and failure screens remain separate contexts; only their HUD entry points and shared chrome are included here.

`ASSET-004 — Workbench` keeps its existing ID and is redesigned under the world-building standards in this file. Card fronts and card illustrations are not redrawn here.

## Approved Visual Direction

- A clean post-apocalyptic field notebook rather than a dirty scrapbook.
- Paper keeps fibre, slight folds and natural edges; no oil stains, black blotches, blood smears or heavy soot in reading areas.
- The central base uses soft charcoal grey rather than a solid black field.
- Buildings and zombies are predominantly charcoal and greyscale world illustrations.
- Functional HUD icons use one restrained semantic colour over a charcoal base; colour coverage should normally remain below 25%.
- Colour is never the only signal. Shape, crack, notch, outline, icon or pattern must provide the same meaning in greyscale.

### Palette

| Role | Colour |
|---|---|
| Paper | `#E6DDC5` |
| Charcoal ink | `#252622` |
| Structural grey | `#5E625D` |
| Central world charcoal | `#45423C` |
| Building light plane | `#B4AD9F` |
| Stage 0 | `#5E7650` |
| Stage 1 | `#C79A24` |
| Stage 2 / danger | `#B53A32` |
| Combat | restrained iron blue-grey |

## Shared Generation Prompt

Use this prefix for every image-generation prompt in this file, followed by the asset-specific prompt:

> clean post-apocalyptic survival field-notebook asset, restrained charcoal drawing and fine ink contour, clear silhouette, controlled paper fibre, soft structural greys, palette anchored to paper #E6DDC5, charcoal #252622 and structural grey #5E625D, near-top-down view where applicable, low-detail readable forms, no readable text or numbers

Shared negative prompt:

> no watermark, no fake lettering, no named artist imitation, no photorealism, no glossy fantasy GUI, no neon magic glow, no heavy grime, no oil stains, no gore or exposed organs, no dense scratch noise, no complex background, no baked UI state, no baked values, no broken anatomy, no unintended colour

## Unity Production Standard

### UI assets

- Texture Type: Sprite (2D and UI)
- PPU: 100
- sRGB: On
- Mipmap: Off
- Wrap: Clamp
- Filter: Bilinear
- Read/Write: Off
- Generate Physics Shape: Off
- Alpha Is Transparency: On for RGBA assets
- 9-slice sprites: Full Rect
- Atlas padding: at least 8 px; Extrude: 2 px

### World assets

- Rendered by SpriteRenderer under the fixed orthographic world camera.
- World PPU: 200; Bottom Center pivot for buildings, zombies and tall props.
- Albedo: RGBA 8-bit sRGB PNG.
- Normal: same dimensions and crop; Texture Type Normal Map; sRGB Off; secondary texture name `_NormalMap`.
- World sprites: Mipmap Off, Wrap Clamp, Read/Write Off, Generate Physics Shape Off.
- Lit art uses shared materials; do not create a material instance per entity.
- Normal maps describe only large planes and form changes. Paper grain, charcoal noise and line work do not become normal detail.
- Colliders and ShadowCaster2D paths are authored in Prefabs, not generated from alpha.

### Atlas groups

- `atlas_hud_chrome_2048`
- `atlas_hud_icons_2048`
- `atlas_hud_pyramid_2048`
- `atlas_world_buildings_demo_a_2048`
- `atlas_world_buildings_demo_b_2048`
- `atlas_world_zombies_2048`
- `atlas_world_props_2048`
- `atlas_cards_hud_2048`
- `atlas_hud_vfx_1024`

Full-screen backdrops and particle texture sheets load independently.

---

## ASSET-007 — Gameplay Parchment Backdrop

| Field | Value |
|---|---|
| Category | Environment / Backdrop |
| Dimensions | 3840x2160 px |
| Format | RGB PNG |
| Naming | `world_backdrop_parchment_01.png` |
| Render | SpriteRenderer, Unlit, PPU 200, Center pivot |
| Status | Approved / Needed |

**Visual Description:** Clean full-page parchment with low-contrast fibres, slight folds and one natural outer edge treatment. Reading zones remain almost texture-free; non-interactive side extensions may continue beyond the 16:9 core frame.

**Art Bible Anchors:** §4 colour system and clean-paper amendment in §7–8.

**Generation Prompt:** full-screen clean parchment sheet, broad quiet centre, subtle fibres and slight edge irregularity, no objects, no writing, no stains, 16:9.

## ASSET-008 — Lit Charcoal Base Ground

| Field | Value |
|---|---|
| Category | Environment / Ground |
| Dimensions | 2560x1120 px |
| Format | RGBA PNG + paired normal PNG |
| Naming | `world_ground_base_charcoal_01.png`, `world_ground_base_charcoal_01_n.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Center pivot |
| Status | Approved / Needed |

**Visual Description:** A soft medium-dark charcoal ground plate clearly separated from the light HUD paper. It uses three readable value bands—ground, paths and shallow terrain—without a solid black centre.

**Art Bible Anchors:** §3 grounded base shape; §4 charcoal and structural grey.

**Generation Prompt:** wide fixed-base ground, soft charcoal grey field, subtle dirt paths and shallow terrain patches, clear quiet centre, near-top-down, no buildings, no text, transparent outer edge.

## ASSET-009 — Parchment Panel Kit

| Field | Value |
|---|---|
| Category | UI / 9-slice Chrome |
| Dimensions | 256x256 px master pieces |
| Format | RGBA PNG |
| Naming | `ui_panel_parchment_{fill|frame|critical}_01.png` |
| Border | 32 / 32 / 32 / 32 |
| Status | Approved / Needed |

**Visual Description:** Separate repeatable paper fill and restrained hand-cut edge frame. The critical variant adds one missing corner or archive-stamp silhouette without baking colour, labels or warnings into the frame.

**Art Bible Anchors:** §3 complete square state versus broken risk state; §7 clean paper rule.

**Generation Prompt:** modular parchment UI panel components, quiet repeatable centre, restrained hand-cut edge, separate normal and missing-corner critical frame, orthographic front view, no shadow, no text.

## ASSET-010 — Left Shortcut Rail Kit

| Field | Value |
|---|---|
| Category | UI / Navigation Chrome |
| Dimensions | 128x128 px button; scalable rail frame |
| Format | RGBA PNG |
| Naming | `ui_shortcut_slot_01.png`, `ui_shortcut_rail_01.png` |
| Border | Button 16 / 16 / 16 / 16 |
| Status | Approved / Needed |

**Visual Description:** A vertical paper-tab rail with consistent square cells and generous icon padding. Game-outside entries and base-function entries are divided by spacing or a simple ink rule; selection uses an offset outline or tab displacement.

**Art Bible Anchors:** §3 complete square and open planning shapes; §7 interaction-state rule.

**Generation Prompt:** vertical notebook index rail with separate square paper-tab slot, clean charcoal contour, quiet paper centre, visible group divider, no icons, no text.

## ASSET-011 — Resource Strip and Badge Kit

| Field | Value |
|---|---|
| Category | UI / HUD Chrome |
| Dimensions | 512x128 px strip; 64x64 px badge |
| Format | RGBA PNG |
| Naming | `ui_resource_strip_01.png`, `ui_badge_count_01.png` |
| Border | Strip 48/32/48/32; badge 12/12/12/12 |
| Status | Approved / Needed |

**Visual Description:** A horizontal paper strip for top-right resources and a small matching numeric badge. Both retain clean centres and cannot include baked icons, separators or values.

**Art Bible Anchors:** §7 permanent-information hierarchy and contrast rules.

**Generation Prompt:** clean horizontal parchment HUD strip and matching small count badge, fine charcoal edge, neutral centre, no icons, no numbers, isolated transparent assets.

## ASSET-012 — Notification and Detail Kit

| Field | Value |
|---|---|
| Category | UI / Information Chrome |
| Dimensions | 512x256 px master; 64x64 px controls |
| Format | RGBA PNG |
| Naming | `ui_message_{normal|irreversible}_01.png`, `ui_detail_pin_{on|off}_01.png` |
| Border | 40 / 40 / 40 / 40 |
| Status | Approved / Needed |

**Visual Description:** Shared paper family for right-side notifications, pinned details, hover names and notification history. Ordinary messages remain complete and rectangular; irreversible messages use a broken corner or sealed mark, not colour alone.

**Art Bible Anchors:** §3 threat asymmetry; §7 notification hierarchy and accessibility.

**Generation Prompt:** modular notebook notification and detail-window frame family, normal intact version and irreversible missing-corner version, separate pin control silhouettes, no text, no colour-only state.

## ASSET-013 — Flow Control Kit

| Field | Value |
|---|---|
| Category | UI / Flow Controls |
| Dimensions | 192x256 px side tab; scalable primary button and modal frame |
| Format | RGBA PNG |
| Naming | `ui_flow_{view_base|return|advance|lock|confirm}_01.png` |
| Status | Approved / Needed |

**Visual Description:** Contains View Base, Return, Advance Time, high-risk confirmation and minimum-resolution blocker chrome. View and Return share a location but use opposing direction symbols; locked states add a lock or diagonal cut overlay rather than only dimming.

**Art Bible Anchors:** §3 planning direction; §7 input lock and non-colour state rules.

**Generation Prompt:** notebook flow-control tabs and primary button frames, forward, return and locked shape variants, restrained charcoal outlines, blank centres, no text.

## ASSET-014 — Morale Pyramid Kit

| Field | Value |
|---|---|
| Category | UI / State Illustration |
| Dimensions | 1024x1024 px shared work canvas |
| Format | Multiple RGBA PNG sprites |
| Naming | `ui_morale_pyramid_{top|middle|bottom}_{intact|lost}_01.png` |
| Atlas | `atlas_hud_pyramid_2048` |
| Status | Approved / Needed |

**Visual Description:** Three independent layers aligned to one coordinate system. Every layer has intact and permanently lost variants; cracks, current-layer outline and warning pulse remain separate overlays so the same art supports both corner display and central transition.

**Art Bible Anchors:** §3 complete/cracked/broken progression; §4 Stage semantic colours.

**Generation Prompt:** three-part morale pyramid diagram, independent top middle bottom paper-and-charcoal layers, intact and clearly broken grey-white lost versions, large readable silhouette, no lettering, no numbers.

## ASSET-015 — Shortcut Icon Set

| Field | Value |
|---|---|
| Category | UI / Icon Set |
| Dimensions | 128x128 px each; 16 px clear margin |
| Format | RGBA PNG |
| Naming | `icon_shortcut_{encyclopedia|settings|warehouse|base|build|foodplan|combat}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Encyclopedia, Settings, Card Warehouse, Base Overview, Construction, Food Plan and Combat share one grid, stroke and negative-space ratio. Each uses charcoal plus one restrained functional colour; combat uses iron blue-grey rather than danger red.

**Art Bible Anchors:** §3 functional silhouette; §4 semantic colour; §7 icon consistency.

**Generation Prompt:** coherent seven-icon survival notebook set, book, gear, card archive box, base, hammer, food crop, crossed weapons, bold charcoal silhouettes with restrained low-saturation accent, isolated transparent background.

## ASSET-016 — Time-of-Day Icon Set

| Field | Value |
|---|---|
| Category | UI / Icon Set |
| Dimensions | 128x128 px each |
| Format | RGBA PNG |
| Naming | `icon_time_{day|afternoon|night}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Day, Afternoon and Night are distinguishable through sun height and silhouette, not only colour. They remain readable at 64 px and in greyscale.

**Art Bible Anchors:** §7 time readability and accessibility.

**Generation Prompt:** three cohesive notebook time icons, high sun for day, low angled sun for afternoon, crescent moon for night, charcoal outline, restrained accent, no labels.

## ASSET-017 — Core Resource Icon Set

| Field | Value |
|---|---|
| Category | UI / Icon Set |
| Dimensions | 128x128 px each |
| Format | RGBA PNG |
| Naming | `icon_resource_{morale|food|parts|combat|energy|capacity}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Morale, Food, Parts, Combat, Energy and Capacity share consistent visual weight. Combat is iron blue-grey; red is reserved for actual loss and threat warnings.

**Art Bible Anchors:** §4 resource semantics; §7 permanent-information hierarchy.

**Generation Prompt:** six-icon survival resource set, morale community, canned food, mechanical parts, combat readiness, energy, capacity limit, clean charcoal pictograms with restrained semantic accents, transparent background.

## ASSET-018 — Semantic State Icon Set

| Field | Value |
|---|---|
| Category | UI / Icon Set |
| Dimensions | 64x64 px each; 8 px clear margin |
| Format | RGBA PNG |
| Naming | `icon_state_{shortage|overflow|reserved|temporary|pending|permanent|collapse|warning|unknown}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Shortage uses a downward notch, overflow an upward spill, reservation a lock, temporary value an hourglass, pending effect a clock, permanent damage a non-repairable break, collapse a terminal emblem and unknown threat a concealed silhouette. Every meaning survives greyscale.

**Art Bible Anchors:** §3 state shape language; §7 no colour-only rule.

**Generation Prompt:** compact nine-icon status set, downward shortage notch, upward overflow, lock, hourglass, clock, permanent broken mark, collapse, warning, hidden threat silhouette, strong distinct shapes, charcoal with minimal accent.

## ASSET-019 — PC Input Hint Set

| Field | Value |
|---|---|
| Category | UI / Input Icons |
| Dimensions | 128x128 px each |
| Format | RGBA PNG |
| Naming | `icon_input_{mouse_left|mouse_right|space|escape}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Mouse-left, mouse-right, Space and Escape hints share one keycap and line system. No controller or touch icon is included in the PC Demo set.

**Art Bible Anchors:** §7 PC input and legibility rules.

**Generation Prompt:** four clean PC input hint pictograms, mouse left button, mouse right button, space bar and escape key, notebook ink style, isolated, no words except simple key symbols added later in TMP.

## ASSET-020 — Base Terrain and Path Kit

| Field | Value |
|---|---|
| Category | Environment / Modular Ground |
| Dimensions | 256x256 and 512x512 px modules |
| Format | RGBA PNG + optional paired normal PNG |
| Naming | `world_ground_{dirt|path|patch|extension}_{variant}.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Center pivot |
| Status | Approved / Needed |

**Visual Description:** Seam-compatible dirt, path and ground-patch modules extend the central charcoal field without adding playable information. The same 16:9 interactive core remains visible on 16:10 and ultrawide screens.

**Art Bible Anchors:** §4 Rust Belt environment cues; §7 fixed-world fairness.

**Generation Prompt:** modular near-top-down charcoal dirt paths and ground patches, clean readable shapes, subtle texture, seamless edges, no buildings, no text, transparent or tileable boundaries.

## ASSET-021 — Perimeter and Base Prop Kit

| Field | Value |
|---|---|
| Category | Environment / Props |
| Dimensions | 256x256 or 512x512 px each |
| Format | RGBA PNG + paired normal PNG where lit form matters |
| Naming | `world_prop_{fence|gate|watchtower|campfire|well|crate|tree|wreck}_{variant}.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center for tall props |
| Status | Approved / Needed |

**Visual Description:** Fence segments, corners, gate, watchtower, campfire base, well, crates, material piles, trees and wreckage form a readable Rust Belt survivor base. Props support the base but never obscure buildings, zombies or their labels.

**Art Bible Anchors:** §4 world-setting cues; §3 grounded base shapes.

**Generation Prompt:** coherent modular survivor-base prop set, rough timber fence, gate, watchtower, stone fire ring, well, crates, material piles, sparse trees and abandoned vehicle, near-top-down charcoal illustration, isolated sprites.

## ASSET-022 — World Target and Ground Feedback Kit

| Field | Value |
|---|---|
| Category | World UI / Overlay Sprites |
| Dimensions | 256x256 and 512x512 px |
| Format | RGBA PNG |
| Naming | `world_overlay_{anchor|valid|invalid|hover|selected|shadow}_01.png` |
| Render | SpriteRenderer, Unlit or dedicated overlay material, PPU 200 |
| Status | Approved / Needed |

**Visual Description:** Contains hidden building anchors, construction foundations, legal and illegal target marks, hover/selection outlines and shared contact shadows. Planning marks are dashed and open; invalid marks use a crossed or broken shape in addition to colour.

**Art Bible Anchors:** §3 planning/construction language; §7 non-colour feedback.

**Generation Prompt:** near-top-down world interaction overlay set, dashed foundation anchor, valid open blueprint outline, invalid crossed outline, selection ring and soft charcoal blob shadow, transparent background, no text.

## ASSET-023 — Intelligence Center World Family

| Field | Value |
|---|---|
| Category | Environment / Building Sprite Family |
| Dimensions | 640x512 px per state |
| Format | RGBA albedo PNG + paired normal PNG |
| Naming | `world_building_intel_{foundation|construction|lv1|lv2}_01.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** The central building family progresses from a marked foundation to scaffolding, a compact communications office and an expanded level-two structure. A radio mast, map board or antenna is the functional top element; food-plan availability is a separate status overlay.

**Art Bible Anchors:** §3 building base/core/top grammar and construction-state rules; §4 blueprint blue accent.

**Generation Prompt:** four-state survivor intelligence centre, empty marked foundation, open scaffold construction, compact operating radio-planning building, expanded level-two version, near-top-down charcoal drawing, clear antenna top element, isolated transparent background.

## ASSET-004 — Workbench World Family (Shared Existing ID)

| Field | Value |
|---|---|
| Category | Environment / Building Sprite Family |
| Dimensions | 512x512 px per state |
| Format | RGBA albedo PNG + paired normal PNG |
| Naming | `world_building_workbench_{construction|lv1}_01.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Needs Redesign / Shared from Visual Pilot |

**Visual Description:** A compact improvised workshop with a stable base, enclosed work shelter and unmistakable bench/tool-rack top element. The old Visual Pilot image remains reference-only and cannot be imported as final world art.

**Art Bible Anchors:** §3 building grammar; §4 resource and construction semantics.

**Generation Prompt:** two-state improvised survivor workshop, scaffold construction and operating workbench shelter, near-top-down charcoal illustration, clear bench and hanging tool silhouette, isolated transparent background.

## ASSET-024 — Storage World Family

| Field | Value |
|---|---|
| Category | Environment / Building Sprite Family |
| Dimensions | 512x512 px per state |
| Format | RGBA albedo PNG + paired normal PNG |
| Naming | `world_building_storage_{construction|lv1|lv2}_01.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** Storage is identified by stacked crates, a reinforced dry platform and a covered loading side. Level two expands volume and shelving without changing its ground scale or losing the original silhouette.

**Art Bible Anchors:** §3 stable resource shape and building upgrade language.

**Generation Prompt:** three-state survivor storage building, scaffold construction, small covered crate warehouse, expanded reinforced level-two storage, near-top-down charcoal art, clear stacked-crate function, isolated.

## ASSET-025 — Kitchen World Family

| Field | Value |
|---|---|
| Category | Environment / Building Sprite Family |
| Dimensions | 512x512 px per state |
| Format | RGBA albedo PNG + paired normal PNG |
| Naming | `world_building_kitchen_{construction|lv1}_01.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** A practical cooking shelter with stove, preparation surface and chimney. Smoke, firelight and any moving cloth are separate animated parts or particles rather than baked into the building.

**Art Bible Anchors:** §3 building grammar; §4 food accent and low-detail function cues.

**Generation Prompt:** two-state survivor kitchen shelter, scaffold construction and operating stove building, near-top-down charcoal drawing, clear chimney and preparation counter, no smoke baked into sprite, isolated.

## ASSET-026 — Fortification World Family

| Field | Value |
|---|---|
| Category | Environment / Building Sprite Family |
| Dimensions | 512x512 px per state |
| Format | RGBA albedo PNG + paired normal PNG |
| Naming | `world_building_fortification_{construction|lv1|lv2}_01.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** A defensive position built from barricade walls, firing cover and one clear guard or weapon platform. Level two strengthens and extends the same outward-facing defensive silhouette.

**Art Bible Anchors:** §3 outward threat-facing shapes and building upgrade language; §4 danger red only for active warning.

**Generation Prompt:** three-state survivor fortification, scaffold construction, barricaded firing position, reinforced level-two defence post, near-top-down charcoal illustration, strong outward defensive silhouette, isolated.

## ASSET-027 — Building Status Icon Set

| Field | Value |
|---|---|
| Category | UI / World Status Icons |
| Dimensions | 64x64 px each |
| Format | RGBA PNG |
| Naming | `icon_building_{construct|upgrade|available|affected|locked|foodplan}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Construction, upgrade, function available, event affected, locked and Food Plan available states use distinct silhouettes and the same compact badge weight. Normal operation shows no badge.

**Art Bible Anchors:** §3 planning and broken-state shapes; §7 contextual-only status rule.

**Generation Prompt:** six compact building-status icons, scaffold, outward upgrade arrows, available function, event impact, lock, food-plan marker, charcoal line with restrained accents, transparent background.

## ASSET-028 — Wandering Zombie World Family

| Field | Value |
|---|---|
| Category | Character / World Sprite and Idle Sheet |
| Dimensions | 384x512 px per frame; 3–4 frames |
| Format | RGBA albedo PNG + optional low-strength normal PNG |
| Naming | `world_zombie_wanderer_idle_{00-03}.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** Slow forward lean, dragging limb and narrow unstable silhouette. Idle motion shifts shoulders, head and centre of mass without moving the foot contact point.

**Art Bible Anchors:** §3 cracked/asymmetric threat language; §4 low-to-medium gore limit.

**Generation Prompt:** wandering zombie idle sequence, slow forward lean, dragging limb, torn clothing, narrow unstable silhouette, near-top-down charcoal illustration, 4 coherent frames, fixed feet, isolated, low gore.

## ASSET-029 — Damaged Zombie World Family

| Field | Value |
|---|---|
| Category | Character / World Sprite and Idle Sheet |
| Dimensions | 384x512 px per frame; 3–4 frames |
| Format | RGBA albedo PNG + optional low-strength normal PNG |
| Naming | `world_zombie_damaged_idle_{00-03}.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** Broader, lower and heavier than the Wanderer, with a visibly damaged but stubborn body mass. The silhouette communicates higher health without exposed anatomy.

**Art Bible Anchors:** §3 asymmetric threat language; §4 gore limit and silhouette-first identification.

**Generation Prompt:** durable damaged zombie idle sequence, broad low centre of gravity, broken coat and heavy stubborn posture, near-top-down charcoal illustration, 4 coherent frames, fixed feet, no exposed organs, isolated.

## ASSET-030 — Berserk Zombie World Family

| Field | Value |
|---|---|
| Category | Character / World Sprite and Idle Sheet |
| Dimensions | 384x512 px per frame; 3–4 frames |
| Format | RGBA albedo PNG + optional low-strength normal PNG |
| Naming | `world_zombie_berserk_idle_{00-03}.png` |
| Render | SpriteRenderer, Sprite-Lit, PPU 200, Bottom Center pivot |
| Status | Approved / Needed |

**Visual Description:** Sharp forward-thrust posture, aggressive arm angles and high tension distinguish it from the other two types even as a pure black silhouette. Infection red is limited to a small threat cue.

**Art Bible Anchors:** §3 pointed aggressive threat shape; §4 infection red and gore limits.

**Generation Prompt:** berserk zombie idle sequence, sharp forward lunge posture, aggressive angular limbs, tense asymmetric silhouette, near-top-down charcoal illustration, tiny infection-red accent, 4 coherent frames, fixed feet, isolated.

## ASSET-031 — Zombie World Label Kit

| Field | Value |
|---|---|
| Category | UI / World Labels |
| Dimensions | 256x96 px plate; 64x64 px icon cells |
| Format | RGBA PNG |
| Naming | `ui_zombie_label_plate_01.png`, `icon_zombie_{health|attack}_01.png` |
| Render | Shared `WorldLabelCanvas`, PPU 100 |
| Status | Approved / Needed |

**Visual Description:** One compact plate supports maximum-health and attack icons plus TMP numbers. The plate stays visually detached from the lit character and remains legible over both dark ground and bright effects.

**Art Bible Anchors:** §7 world-label clarity and minimum hit-target rules.

**Generation Prompt:** compact parchment zombie status plate with separate health and attack icon cells, clean charcoal border, empty number areas, no text, no baked values.

## ASSET-032 — Zombie Effect Icon Set

| Field | Value |
|---|---|
| Category | UI / World Status Icons |
| Dimensions | 64x64 px each |
| Format | RGBA PNG |
| Naming | `icon_zombie_effect_{morale|max_morale|next_round|food_demand|special}_01.png` |
| Atlas | `atlas_hud_icons_2048` |
| Status | Approved / Needed |

**Visual Description:** Icons cover current morale damage, permanent maximum-morale loss, next-round effect, food-demand increase and special/unknown effect. Permanent loss uses a non-repairable broken boundary rather than merely a red tint.

**Art Bible Anchors:** §3 permanent broken-state grammar; §7 non-colour semantics.

**Generation Prompt:** five compact zombie consequence icons, morale impact, permanently broken maximum, next-round clock, increased food demand, special unknown effect, strong unique charcoal silhouettes with restrained warning accents.

## ASSET-033 — Zombie Interaction Feedback Kit

| Field | Value |
|---|---|
| Category | UI / World Interaction Overlay |
| Dimensions | 64x64 kill marks; 384x512 outline masks |
| Format | RGBA PNG |
| Naming | `world_zombie_feedback_{kill|cancel|insufficient|hover|selected}_01.png` |
| Status | Approved / Needed |

**Visual Description:** Kill allocation uses a decisive crossed or checked mark above health, while insufficient combat uses a brief rebound/flash shape and no persistent mark. Hover and selected outlines remain distinct in greyscale.

**Art Bible Anchors:** §3 decisive threat cancellation; §7 hover/selected accessibility.

**Generation Prompt:** zombie interaction overlay set, decisive kill mark, cancel mark, insufficient-combat pulse, hover outline and selected outline, charcoal notebook symbols, transparent background, no numbers.

## ASSET-034 — Card Back and Pile Kit

| Field | Value |
|---|---|
| Category | UI / Card Piles |
| Dimensions | 320x440 px each |
| Format | RGBA PNG |
| Naming | `ui_card_{back|draw|draw_empty|discard|discard_empty}_01.png` |
| Atlas | `atlas_cards_hud_2048` |
| Status | Approved / Needed |

**Visual Description:** Draw and discard piles use fixed thickness; count is always TMP. Discard is face-up or marked with a recycling/wear silhouette, while both empty states retain an explicit empty base and zero badge location.

**Art Bible Anchors:** §7 fixed pile semantics and card ratio.

**Generation Prompt:** coherent survival notebook card back, fixed draw pile, empty draw base, fixed face-up discard pile and empty discard tray, 160:220 ratio, charcoal paper style, no text or numbers.

## ASSET-035 — Card Interaction and Hand Kit

| Field | Value |
|---|---|
| Category | UI / Card Interaction Overlay |
| Dimensions | 320x440 px overlays; 512x256 px hand shadow |
| Format | RGBA PNG |
| Naming | `ui_card_state_{hover|selected|locked|valid|invalid}_01.png`, `ui_hand_shadow_01.png` |
| Status | Approved / Needed |

**Visual Description:** Cards remain a natural fan without visible slots. Hover, selected, locked, valid and invalid overlays use outline, displacement or pattern; the broad hand shadow remains soft and neutral.

**Art Bible Anchors:** §7 natural hand and interaction-state rules.

**Generation Prompt:** transparent card-state overlay family and soft fan-shaped hand shadow, hover lift outline, selected border, lock pattern, valid and invalid target marks, no card face art, no text.

## ASSET-036 — Numeric Feedback VFX Kit

| Field | Value |
|---|---|
| Category | VFX / UI Feedback |
| Dimensions | 256x256 and 512x512 px |
| Format | RGBA PNG |
| Naming | `vfx_ui_value_{increase|decrease|shortage|overflow|warning}_01.png` |
| Atlas | `atlas_hud_vfx_1024` |
| Status | Approved / Needed |

**Visual Description:** Arrows, shortage notch, overflow spill and warning pulse support TMP value changes without replacing the actual number. Motion remains short and local to the affected HUD area.

**Art Bible Anchors:** §7 distributed prediction and reduced-motion rules.

**Generation Prompt:** compact notebook value-feedback marks, upward increase, downward decrease, shortage notch, overflow spill and warning pulse, clean charcoal with restrained accent, transparent background, no numbers.

## ASSET-037 — Morale Pyramid Transition VFX

| Field | Value |
|---|---|
| Category | VFX / UI Transition |
| Dimensions | 512x512 and 1024x1024 px overlays |
| Format | RGBA PNG |
| Naming | `vfx_morale_{crack|fragment|fade|sealed}_01.png` |
| Atlas | `atlas_hud_pyramid_2048` |
| Status | Approved / Needed |

**Visual Description:** Crack, fragment, fade and permanent-seal overlays make the lost layer feel truly damaged while leaving its outline visible. The reduced-motion version uses sprite swap and brief fades without long travel or repeated scaling.

**Art Bible Anchors:** §3 irreversible broken-state language; §7 Stage transition rules.

**Generation Prompt:** morale pyramid transition overlays, spreading crack, charcoal-paper fragments, grey-white fade and permanently sealed broken outline, dramatic but clean, transparent, no text.

## ASSET-038 — Building Completion VFX

| Field | Value |
|---|---|
| Category | VFX / World Feedback |
| Dimensions | 512x512 px sheet and overlays |
| Format | RGBA PNG |
| Naming | `vfx_world_build_{blueprint|dust|paper}_01.png` |
| Render | World ParticleSystem / overlay SpriteRenderer |
| Status | Approved / Needed |

**Visual Description:** Dashed blueprint lines settle into the completed structure with restrained dust and paper fragments. The effect does not obscure the building state or imply magical construction.

**Art Bible Anchors:** §3 planning-to-complete transition; §4 blueprint blue.

**Generation Prompt:** world construction-complete effect sheet, dashed blueprint lines, restrained charcoal dust and a few paper fragments, near-top-down, no magic glow, transparent background.

## ASSET-039 — Zombie Entry and Removal VFX

| Field | Value |
|---|---|
| Category | VFX / World Feedback |
| Dimensions | 512x512 px sheet |
| Format | RGBA PNG |
| Naming | `vfx_world_zombie_{entry|kill}_01.png` |
| Render | Pooled World ParticleSystem / overlay SpriteRenderer |
| Status | Approved / Needed |

**Visual Description:** Entry uses a short edgeward ink-dust arrival; removal uses a decisive ink brush or erasure. Neither effect communicates damage before the Runtime confirms spawn or kill.

**Art Bible Anchors:** §3 threat asymmetry and cancellation; §7 truthful result feedback.

**Generation Prompt:** two-part zombie world effect sheet, short charcoal ink-dust entry and decisive ink-brush removal, near-top-down, restrained fragments, no gore, transparent background.

## ASSET-040 — UI Body TMP Font Configuration

| Field | Value |
|---|---|
| Category | UI / Typography |
| Source | Existing Noto Sans TC fonts |
| Naming | `UI_Regular_Font`, `UI_Bold_Font` |
| Configuration | Dynamic SDF, Multi Atlas On, verified fallback chain |
| Status | Existing / Needs Configuration |

**Visual Description:** Readability-first Traditional Chinese body type for HUD labels, messages and details. At 1280x720, ordinary text remains at least equivalent to 16 px.

**Art Bible Anchors:** §7 text contrast and minimum-size rules.

**Generation Prompt:** Not applicable; typography uses licensed existing source fonts.

## ASSET-041 — Notebook Display TMP Font Configuration

| Field | Value |
|---|---|
| Category | UI / Typography |
| Source | Existing LXGW WenKai Mono TC fonts |
| Naming | `Note_Regular_Font` |
| Configuration | Dynamic SDF; headings, dates and short labels only |
| Status | Existing / Needs Configuration |

**Visual Description:** Provides controlled handwritten character for dates, headings and short notebook labels without reducing paragraph readability.

**Art Bible Anchors:** §7 field-notebook identity and typography hierarchy.

**Generation Prompt:** Not applicable; typography uses a licensed existing source font.

## ASSET-042 — Traditional Chinese Fallback Chain

| Field | Value |
|---|---|
| Category | UI / Typography Configuration |
| Sources | Noto Sans TC → TW-Sung / Ext-B → symbol font |
| Naming | `TMP_Fallback_TraditionalChinese` |
| Configuration | Multi Atlas or validated Demo character set; aligned baseline |
| Status | Approved / Needs Configuration |

**Visual Description:** Prevents missing glyph squares and obvious baseline jumps across supported Traditional Chinese text and symbols. Liberation Sans cannot remain the only project fallback.

**Art Bible Anchors:** §7 supported-language and fallback requirements.

**Generation Prompt:** Not applicable.

## ASSET-043 — Gameplay HUD Integration Validation Board

| Field | Value |
|---|---|
| Category | Production Reference / QA |
| Dimensions | 1920x1080 and 1280x720 captures |
| Format | PNG evidence + checklist |
| Naming | `validation_hud_{resolution}_{variant}.png` |
| Status | Approved / Needed |

**Visual Description:** One controlled composition verifies all P0 families together, then repeats in greyscale, colour-vision simulations and 1/8/12-zombie density states. A separate glyph page exercises every Demo Traditional Chinese character.

**Art Bible Anchors:** §4 pilot consistency gate; §7 accessibility gate.

**Generation Prompt:** Not applicable; this is assembled from production assets in Unity.

## ASSET-044 — World Material Kit

| Field | Value |
|---|---|
| Category | Technical Art / Materials |
| Engine Assets | Unity Materials |
| Naming | `mat_world_sprite_lit`, `mat_world_sprite_unlit`, `mat_world_shadow_blob`, `mat_world_particle_lit`, `mat_world_particle_additive` |
| Status | Approved / Needed |

**Visual Description:** A minimal shared material set supports lit world art, unlit backdrop/overlays, neutral contact shadows, lit smoke and restrained additive fire. No building or zombie receives a private material instance.

**Art Bible Anchors:** §7 world/HUD separation; §8 shared-material rule.

**Generation Prompt:** Not applicable; authored in Unity from URP 2D shaders.

## ASSET-045 — Time Lighting and Light Cookie Kit

| Field | Value |
|---|---|
| Category | Technical Art / Lighting |
| Cookie Dimensions | 256x256 px greyscale each |
| Format | Greyscale PNG + Unity Light2D profiles/prefabs |
| Naming | `light_profile_{day|afternoon|night}`, `light_cookie_{campfire|window}_01.png` |
| Status | Approved / Needed |

**Visual Description:** Day, afternoon and night primarily change Global Light colour and intensity. Campfire and window cookies remain soft, irregular and low-contrast, supporting the charcoal drawing rather than creating photorealistic beams.

**Art Bible Anchors:** §4 time colours; §7 living-world lighting restraint.

**Generation Prompt:** soft greyscale 2D light-cookie masks for irregular campfire glow and simple window glow, feathered organic edges, no scene, no colour, square texture.

## ASSET-046 — World Shadow Kit

| Field | Value |
|---|---|
| Category | Technical Art / World Shadows |
| Blob Dimensions | 128x64 px variants |
| Format | RGBA PNG + simplified ShadowCaster2D paths |
| Naming | `world_shadow_blob_{small|medium|large}_01.png` |
| Status | Approved / Needed |

**Visual Description:** Neutral soft blob shadows anchor zombies and small objects. Buildings, walls, towers and large trees use manually simplified 4–16 point ShadowCaster2D paths; no alpha-traced caster is allowed.

**Art Bible Anchors:** §3 grounded base language; §8 shadow budget.

**Generation Prompt:** three soft charcoal contact-shadow ellipses, small medium large, transparent background, low contrast, no object, no directional dramatic lighting.

## ASSET-047 — Campfire Animation and Ember VFX

| Field | Value |
|---|---|
| Category | VFX / World Activity |
| Dimensions | 4–6 flame frames plus 512x512 ember sheet |
| Format | RGBA PNG |
| Naming | `world_campfire_flame_{00-05}.png`, `vfx_particle_ember_sheet_01.png` |
| Render | SpriteRenderer animation + pooled ParticleSystem + Point Light2D |
| Status | Approved / Needed |

**Visual Description:** A low-frame flame and sparse embers provide the base's central living pulse. Point-light intensity and radius vary in code; individual embers never carry lights.

**Art Bible Anchors:** §4 supply yellow and infection-red limits; §7 restrained world activity.

**Generation Prompt:** low-frame campfire flame animation and sparse ember particle sheet, charcoal outline with restrained supply-yellow and warm brick accent, clean transparent background, no photoreal fire.

## ASSET-048 — Shared Smoke VFX

| Field | Value |
|---|---|
| Category | VFX / World Activity |
| Dimensions | 512x512 px texture sheet |
| Format | RGBA PNG |
| Naming | `vfx_particle_smoke_sheet_01.png` |
| Render | Pooled ParticleSystem, World Simulation Space |
| Status | Approved / Needed |

**Visual Description:** One restrained smoke family serves campfire, Kitchen and future chimneys through scale, speed and tint changes. Forms are broad charcoal washes, not dense black clouds.

**Art Bible Anchors:** §4 charcoal limits; §7 world-particle readability.

**Generation Prompt:** soft hand-drawn charcoal smoke particle sheet, several broad fading puffs, low contrast, clean transparent background, no fire, no dense black cloud.

## ASSET-049 — Ambient Particle Kit

| Field | Value |
|---|---|
| Category | VFX / Environment Activity |
| Dimensions | 512x512 px texture sheet |
| Format | RGBA PNG |
| Naming | `vfx_particle_ambient_sheet_01.png` |
| Render | Pooled ParticleSystem |
| Status | Approved / Needed |

**Visual Description:** Dust, ash and a few paper scraps move slowly through non-interactive world space. Ambient particles remain sparse and cannot pass over critical labels long enough to reduce readability.

**Art Bible Anchors:** §7 clean-world and accessibility rules; §8 particle budget.

**Generation Prompt:** sparse hand-drawn ambient particle sheet, dust mote, ash flake and small torn paper shapes, charcoal and paper tones, transparent background, simple readable silhouettes.

## ASSET-050 — Building Animated Part Kit

| Field | Value |
|---|---|
| Category | Environment / Animated Parts |
| Dimensions | 128–512 px per isolated part |
| Format | RGBA albedo PNG + normal PNG where lit |
| Naming | `world_building_part_{antenna|flag|cloth|window|lamp}_{variant}.png` |
| Render | Child SpriteRenderer with Animator or transform motion |
| Status | Approved / Needed |

**Visual Description:** Antennas, flags, hanging cloth, window light and function lamps animate independently from building bodies. Motion is small, asynchronous and functional rather than constant decorative bouncing.

**Art Bible Anchors:** §3 functional top element; §7 living-world restraint.

**Generation Prompt:** isolated survivor-building animated parts, radio antenna, torn flag, hanging cloth, window-light mask and small work lamp, near-top-down charcoal style, transparent background, no building body.

## ASSET-051 — Zombie Idle Animation Sheets

| Field | Value |
|---|---|
| Category | Character / Animation Deliverable |
| Dimensions | 384x512 px per frame; 3–4 frames per zombie type |
| Format | RGBA albedo PNG; paired normal frames only if required by final rig |
| Naming | `world_zombie_{wanderer|damaged|berserk}_idle_{00-03}.png` |
| Playback | 6–8 FPS, random starting frame, fixed foot contact |
| Status | Approved / Needed |

**Visual Description:** Low-amplitude shoulder, head and weight shifts prevent static-card presentation while keeping the character's collider and world-label anchor stable. Each archetype retains its distinct silhouette throughout the loop.

**Art Bible Anchors:** §3 threat silhouette; §7 living-world motion and reduced-motion rule.

**Generation Prompt:** three coherent low-frame zombie idle animation sets, wanderer, durable damaged zombie and aggressive berserker, near-top-down charcoal style, subtle shoulder and head shifts, fixed feet, isolated, low gore.

---

## World Runtime and Prefab Requirements

These are implementation requirements attached to the assets, not additional image assets:

- Create shared `BuildingWorldVisualProfile` and `ZombieWorldVisualProfile` data or equivalent world-prefab references.
- Card artwork and world artwork must be separate fields.
- World prefabs use SpriteRenderer, SortingGroup, Collider2D, optional ShadowCaster2D, animated-part anchors, VFX anchors and one label anchor.
- Use one `WorldLabelCanvas` and a pooled label system; never one Canvas per entity.
- Use one `WorldInteractionRouter` or equivalent authority to apply flow lock before Physics2D queries.
- Add sorting layers `World_Backdrop`, `World_Ground`, `World_Decal`, `World_PropBack`, `World_Building`, `World_Zombie`, `World_FX` and `World_Overlay`.
- Configure Global Light 2D target sorting layers after the layers are created.
- Use Custom Transparency Sort Axis `(0, 1, 0)` and Bottom Center sort points for near-top-down objects.

## Dynamic World Budget

- One Global Light2D.
- Up to six local Light2D objects; no more than one or two local shadow-casting lights.
- Up to approximately twenty simplified ShadowCaster2D objects.
- Up to six looping and four simultaneous one-shot ParticleSystems.
- Approximately 200–250 active particles; no particle collision or trails by default.
- Approximately 60–80 visible SpriteRenderers in the Demo stress composition.
- Use pooling for zombie labels, zombie views and one-shot effects.
- Target 60 FPS at both 1280x720 and 1920x1080 with seven buildings, twelve zombies and the complete HUD.

## Production Priority

### P0 — Integration Slice

- ASSET-007, 008, 009–017, 020, 022, 023, 004, 028, 031, 033–035, 040–047 and 050.
- Compose one complete 1920x1080 Gameplay screen with five cards, one notification, one detail window and Default/Hover/Selected/Locked states.
- Include one Global Light2D, campfire light, Intelligence Center shadow, one Wandering Zombie, label projection and the flow-input lock.

### P1 — Playable Demo

- Remaining buildings, all three zombie families, complete status/effect icons, Smoke, Ambient Particles, zombie idle sheets and all Gameplay feedback.

### P2 — World Polish

- Additional fence/path/prop variants, rare animation parts, advanced dust and optional high-quality zombie shadow casters.

## Acceptance Gate

P1 batch production cannot start until the P0 integration slice passes:

1. 1920x1080 and 1280x720 layouts with no clipped critical content.
2. Seven buildings and a right-side 3x4 grid of twelve zombies remain individually readable and clickable.
3. Greyscale, red-weak and green-weak captures preserve all decision-critical states.
4. The three zombie silhouettes remain distinct without colour or labels.
5. Five building families remain distinct without baked names.
6. HUD and WorldLabelCanvas are unaffected by Light2D.
7. Event/exploration temporary view disables Physics2D world input as well as Canvas Gameplay input.
8. Albedo/normal pairs show no fringe, mismatch or plastic paper texture.
9. Transparent sprites pass both light-background and dark-background fringe tests.
10. TMP fallback validation contains no missing glyph squares or baseline jumps.
11. The stress composition maintains the initial 60 FPS budget and stays within the agreed light, shadow and particle limits.
12. Reduced-motion mode lowers particle density, light flicker and large animation without hiding required state information.
