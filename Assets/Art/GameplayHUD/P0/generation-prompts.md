# P0 Gameplay HUD Generation Prompts

> Generated: 2026-08-26  
> Status: In Progress — integration placeholders, not art-direction approval.

All prompts used the approved clean parchment-and-charcoal Gameplay HUD mockup as the visual reference. Shared constraints: warm parchment, black charcoal, clean edges, restrained semantic colour only in UI, no dirty grunge, no baked labels or HUD, no checkerboard, and no imitation of a specific living artist.

## Gameplay parchment backdrop

Create a clean 16:9 game-screen backdrop made from a single pale warm parchment sheet. Subtle fibrous paper texture and softly irregular deckled outer edge, almost no stains, no drawings, no writing, no UI, no objects, no vignette, no black border. Even value and generous negative space, suitable behind a charcoal survival-game interface.

Output: `world_backdrop_parchment_01.png`

## Charcoal base ground

Create an isolated top-down irregular oval patch of compacted earth for a survival base, drawn in clean black charcoal and pencil on a transparent background. Soft grey value masses, sparse stones, faint paths and small grass marks, readable at game scale, no building, no character, no text, no shadow outside the patch, no paper background, clean alpha edge, no checkerboard baked into the image.

Output: `world_ground_base_charcoal_01.png`

## Intelligence Center

Create an isolated top-down three-quarter game building sprite: a small improvised post-apocalyptic intelligence center with timber walls, antenna mast, radio dish, pinned map board and supply crates. Clean hand-drawn charcoal and pencil linework, controlled grey shading, crisp silhouette, no label, no UI, no ground tile, transparent background, clean alpha edge, no checkerboard.

Output: `world_building_intel_lv1_01.png`

## Workbench

Create an isolated top-down three-quarter game building sprite: a compact post-apocalyptic workbench shed with tools, repair table, timber roof and a few neatly arranged material crates. Clean hand-drawn charcoal and pencil linework, controlled grey shading, crisp silhouette distinct from a radio building, no label, no UI, no ground tile, transparent background, clean alpha edge, no checkerboard.

Output: `world_building_workbench_lv1_01.png`

## Wandering Zombie

Create one isolated full-body wandering zombie game sprite seen from a fixed top-down three-quarter camera. Thin shambling silhouette, damaged civilian clothes, arms hanging forward, readable head and limbs, clean black charcoal and pencil drawing with restrained grey shading, no gore emphasis, no text, no icon, no ground shadow, transparent background, clean alpha edge, no checkerboard.

Output: `world_zombie_wanderer_idle_00.png`

## Dark fractured backdrop — revision 2

Edit the existing pale parchment backdrop only. Convert it to a desaturated charcoal-gray and gray-brown paper surface, approximately 30–40% darker. Add visible torn-away corners, uneven ripped perimeter sections, layered paper seams and restrained folds near the edges while keeping the center calm for gameplay readability. Preserve 16:9 framing. No text, drawings, UI, blood, stains, dirt splatter, heavy grunge or vignette.

Outputs:

- `world_backdrop_parchment_02_dark_fractured.png`
- `world_backdrop_parchment_02_dark_fractured_alpha.png` — white exterior removed to expose the dark camera background through torn edges.

## Torn gray UI panel

Create one reusable wide horizontal torn-paper patch for Unity uGUI. Medium charcoal-gray and gray-brown paper, calm center, obvious ripped edges, missing bites and layered corner fibers. Keep it suitable for 9-slice scaling with genuine transparency outside the paper. No text, icons, blood, stains, dirt splatter, heavy grunge or watermark.

Outputs:

- `ui_panel_torn_gray_01.png`
- `ui_panel_torn_gray_01_trimmed.png` — mechanically trimmed and padded integration copy.
- `ui_panel_torn_gray_02_lifted.png` — mechanically lifted midtone version used in Unity so panels remain distinct from the dark backdrop.
- `ui_panel_torn_gray_03_readable.png` — final P0 contrast pass; medium-light gray paper paired with dark ink text.
