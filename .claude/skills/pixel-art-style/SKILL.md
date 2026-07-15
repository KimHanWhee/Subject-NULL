---
name: pixel-art-style
description: Structured methodology for drawing game-ready pixel art (sprites, tiles, icons) using Aseprite MCP tools (create_canvas, draw_rectangle_at, draw_ellipse_at, fill_area_at, draw_pixels_at, export_frame, generate_color_ramp, apply_palette_preset, adjust_hsl, apply_dither_gradient, outline_cel, etc). ALWAYS use this skill whenever the user asks to draw, create, or generate pixel art, a sprite, a character, a tile, an icon, or any 2D game asset via Aseprite/aseprite-mcp — even if they just say "draw X" or "make a sprite of X" without mentioning pixel art explicitly. This skill exists specifically to fix low-quality, inconsistent, or messy pixel art output.
---

# Pixel Art Style Guide

Pixel art made by guessing coordinates freehand looks messy: banding, inconsistent light source, muddy colors, no clean silhouette. This skill forces a structured, professional workflow instead. Follow every phase in order — do not skip to detailed drawing before the silhouette and palette are locked.

## Core principle: silhouette → palette → block-in → shade → clean up → verify

Never try to draw a finished sprite in one shot. Each phase below is a separate round of tool calls, and **you must export and visually inspect your own work after every phase** before moving to the next one.

---

## Phase 0 — Plan before touching pixels

Decide and state explicitly, before drawing anything:

- **Canvas size**: 16x16 = simple icon/prop only. 32x32 = standard small character/item (default choice for most requests). 64x64 = detailed character or hero sprite. Never go below 24x24 for anything with a face or recognizable silhouette — smaller reads as noise.
- **View/angle**: top-down, side-view, or 3/4 — match whatever the rest of the project uses. If unknown, ask or default to side-view.
- **Light source**: pick one direction (default: top-left) and never change it mid-sprite. Every shadow in the piece must be consistent with this.
- **Palette size**: 4–8 colors for a simple object, up to 12–16 for a detailed character. More colors than this is almost always a mistake for pixel art — it stops reading as pixel art and starts reading as a compressed photo.

## Phase 1 — Lock the palette first

Before any drawing, generate or select the palette and commit to it:

- If the project has an established style, use `get_color_stats` on a reference image to extract it, or reuse a previously used palette.
- Otherwise use `apply_palette_preset` with a known-good retro preset (PICO-8, Sweetie-16, Endesga-32, Resurrect-64, Arne-16) rather than inventing colors from scratch.
- For shading, don't just darken a color's brightness — use `generate_color_ramp` or `adjust_hsl` to shift **hue** toward blue/purple for shadows and toward yellow for highlights. Flat-darkened shadows look muddy; hue-shifted shadows look professional.
- Write down the ramp you're using (base → shadow → highlight, per material) before drawing so every part of the sprite stays consistent.

## Phase 2 — Block in the silhouette only

Using only large primitives — `draw_rectangle_at`, `draw_ellipse_at`, `fill_area_at` — build the basic shape in **one flat mid-tone color per major part** (body, head, weapon, etc). No shading, no outline detail yet.

Then immediately:
```
export_frame at 8x–10x scale
```
Look at it. Ask yourself:
- Is the silhouette readable at a glance, even blurred/small?
- Are proportions right (heads too big/small, limbs too thin)?
- Does it look like the intended subject with zero color detail?

If the silhouette isn't working, fix it now with more primitives/fills. **Do not proceed to shading on a broken silhouette** — shading cannot save bad proportions, and re-shading after fixing a silhouette wastes the shading work.

## Phase 3 — Outline

Use `outline_cel` (or manual `draw_pixels_at` for hand corrections) to add a clean, consistent outline. Keep outline thickness at 1px unless the art style explicitly calls for chunkier lines. A clean outline does more for perceived quality than almost anything else in pixel art — don't skip it.

## Phase 4 — Shade with the locked ramp

Working from your Phase 1 ramp:
- Apply base-tone fills first across each region.
- Add shadow tones on the side away from the light source, highlight tones on the side facing it. Keep shadow/highlight shapes simple and geometric at this size — don't try to render soft gradients by hand.
- For actual gradients/transitions (e.g. a rounded surface, a glow), use `apply_dither_gradient` (ordered dithering) instead of hand-placing intermediate pixels. Diffusion-style hand blending looks wrong at low resolution; dithering is the correct pixel-art technique.
- Re-check the light source direction on every shaded region — the #1 amateur mistake is inconsistent lighting across different parts of the same sprite.

## Phase 5 — Export, zoom, and self-critique (mandatory, every time)

```
export_frame at 8x scale
```

Look specifically for:
- **Stray pixels** (single off-color pixels that don't belong — very common artifact, easy to miss at 1x)
- **Color banding** or too many near-identical shades (check with `get_color_stats` — if you're using more colors than your Phase 0 budget, you drifted, go clean it up)
- **Broken outline** (gaps, inconsistent thickness)
- **Inconsistent light source**
- **Pixel grid misalignment** — verify all edges land on whole pixels, no anti-aliased/blurry edges (pixel art tools shouldn't produce this, but double-check after any freehand `draw_pixels_at` corrections)

Fix what you find, then re-export and check again. Do not declare the sprite finished on the first export — treat the first export as a draft review, not a final delivery.

## Animation (if requested)

- Block in only the **extreme poses first** (e.g. contact and passing poses for a walk cycle), not every frame in order.
- Use `render_onion_skin` to check motion continuity between frames before filling in the in-between frames.
- Use `compare_frames` to catch a shape/proportion that accidentally drifted between frames (very common failure mode — the character subtly changes size or shape frame to frame).
- Export with `export_tag` / `export_spritesheet` once all frames pass the Phase 5 checklist individually.

## Quick reference: common failure modes and their fix

| Symptom | Cause | Fix |
|---|---|---|
| Looks "AI-generated"/muddy | Too many colors, shadows made by darkening only | Cut palette to Phase 0 budget, hue-shift shadows |
| Unreadable shape | Skipped Phase 2 silhouette check | Go back, fix silhouette in flat color before any shading |
| Looks flat/2D | No hue shift in ramp, or single light source not applied | Regenerate ramp with `generate_color_ramp`, re-check light direction per region |
| Rough/dirty edges | No outline pass, or `draw_pixels_at` corrections not grid-aligned | Run `outline_cel`, re-export and zoom-check |
| Animation "swims" | Frames drawn independently without checking | Use `render_onion_skin` + `compare_frames` before finalizing |

## When the user gives a reference image or existing project style

Extract its palette and proportions with `get_color_stats` before drawing anything new, and match canvas size, outline weight, and palette to it — don't introduce a new style per request. Consistency across a project's assets matters more than any single sprite being individually "better."
