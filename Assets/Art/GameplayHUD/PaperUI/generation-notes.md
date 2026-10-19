# Paper UI Material Pack

> Generated: 2026-09-06  
> Status: Imported and verified in Unity; ready for art-direction review

This pack follows the approved archival survival-notebook direction: warm grey-beige paper, restrained graphite wear, clean readable centres and controlled irregular edges. All production images contain no baked text or icons.

## Assets

| File | Size | Alpha | Intended use |
|---|---:|---:|---|
| `ui_background_archival_paper_01.png` | 1920×1080 | No | Event, exploration, codex and notebook-like full-screen backgrounds |
| `ui_panel_paper_light_9slice_01.png` | 2048×1024 | Yes | Light details, event copy and large modal panels |
| `ui_panel_paper_dark_9slice_01.png` | 1536×512 | Yes | Permanent HUD strips, notifications and dark information panels |
| `ui_texture_paper_fiber_tile_01.png` | 1024×1024 | No | Low-opacity tiled fibre variation inside UI surfaces |
| `ui_overlay_graphite_wear_01.png` | 1920×1080 | Yes | Optional corner/perimeter wear overlay above a paper background |

The fibre tile was post-processed into a mirrored 2×2 repeat. Its opposite edges have an exact zero-pixel difference, so Unity `Repeat` wrapping will not expose a seam.

## Recommended Unity import

- Background and overlay: `Sprite (2D and UI)`, Single, Bilinear, Mip Maps off, Clamp, max size 2048.
- Light panel: `Sprite (2D and UI)`, Single, alpha transparency on, Full Rect, suggested border `160 / 120 / 160 / 120`.
- Dark panel: `Sprite (2D and UI)`, Single, alpha transparency on, Full Rect, suggested border `96 / 72 / 96 / 72`.
- Fibre tile: `Sprite (2D and UI)` or Default Texture according to the consumer, Bilinear, Mip Maps off, Wrap Mode `Repeat`, max size 1024.
- Keep the fibre tile and wear overlay between roughly 5–18% opacity. They should support the paper material without competing with TMP text.
- Do not tint light paper with saturated colours. Semantic colour belongs to icons, values and state marks.

## Generation prompts

### Full-screen background

`16:9 Unity UI background; blank front-facing archival paper; desaturated warm grey-beige; fine fibres, soft graphite dust, restrained hairline scratches and faint diagonal construction-line ghosts near corners; calm low-contrast centre; slightly darker worn perimeter; no text, symbols, UI, characters, blood, logos or watermark; avoid heavy dirt, fuzzy edges and a dark centre.`

### Light 9-slice panel

`Isolated 2:1 archival-paper panel on genuine transparent alpha; warm grey-beige matte fibres; broad calm interior; controlled irregular torn contour and consistent border thickness for Unity 9-slicing; no shadow, text, symbols, holes, strong stains, fuzzy edges or perspective.`

### Seamless fibre tile

`Uniform square archival-paper fibre texture; warm grey-beige, very low-contrast mottling and graphite dust; no border, vignette, focal point, directional light, text, scratches, stains or objects. Post-process as a mirrored tile for exact edge continuity.`

### Graphite wear overlay

`REAL transparent-alpha 16:9 decal containing only sparse graphite scratches, faint diagonal drafting-line remnants and light perimeter abrasion; centre mostly alpha zero; no paper substrate, checkerboard, text, symbols, blood or dense grime.`

### Dark 9-slice panel

`Isolated 3:1 dark charcoal-grey paper HUD panel on genuine transparent alpha; matte fibres, faint graphite rubbing and subtly lighter worn edges; calm even interior for light TMP text; consistent border thickness; no metal, rivets, text, shadow, holes, pitch-black fill or glossy bevel.`

Generated with the built-in image generation tool. The user-provided reference was used only for material, ageing balance and mood; its text and exact marks were not copied.

## Unity preview

- Preview scene: `Assets/Scenes/PaperUI_MaterialPreview.unity`
- Rebuild menu: `Tools > Zombie Card Survive > Gameplay HUD > Build Paper UI Material Preview`
- The rebuild command reapplies the Sprite, alpha, clamp/repeat, mipmap, max-size and 9-slice border settings before creating the scene.
- Runtime capture: `Assets/Temp/CodexPaperUI/paper_ui_material_preview_runtime.png`
- Unity scene validation: 0 issues; Console: 0 errors on 2026-09-06.
