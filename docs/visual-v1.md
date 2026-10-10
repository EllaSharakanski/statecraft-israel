# Visual v1: measurements, budgets and checkpoints

Plan: `docs/superpowers/plans/2026-10-11-statecraft-visual-v1.md`. Promo targets: `promo/final/03-gameplay-1936.png`,
`promo/final/02-map-diorama.png`. Poses: `PromoPose.Gameplay03` (34.86 E, 32.00 N, 60 km, yaw 10) and
`PromoPose.Diorama02` (35.05 E, 31.55 N, 250 km, yaw 0).

## How to measure
- Frame time: `?vprofile` (WebGL) or `-vprofile` (desktop player, quits when done), always under
  `uv run tools/machine_lock.py --exclusive --wait-unity -- ...` (no other Unity job on the machine). Per pose, every
  configuration (`all`, each toggle off alone as `-<name>`, `none`) runs 3 times in interleaved orders, 300 frames after
  60 warm-up frames (and, before the first pose, one discarded warm-up pass of every configuration); Chrome runs with
  `--disable-frame-rate-limit --disable-gpu-vsync` and the anti-throttling flags (`--disable-backgrounding-occluded-windows
  --disable-renderer-backgrounding --disable-background-timer-throttling`), headless through `web_check.py` (add
  `--stall 120`), headed through `headed.py profile`. Both fix the page's viewport to exactly 1920x1080 CSS pixels at the
  DPR over CDP (`Emulation.setDeviceMetricsOverride`, before the page loads), so headed and headless runs draw the same
  pixels even when the laptop screen clamps the headed window (its final composite is then scaled; the backbuffer is not).
  Every line states `res=` and the render `scale=`, and both tools FAIL on a `res=` other than 1920x1080 times the DPR
  (and on a stall: no console line for 120 s). Read the `PROFILE summary` lines: a feature costs
  `median(all) - median(-name)`. The WebGL frame clock is quantised to about 1 ms: averages are fine, but every p95 is a
  whole millisecond, so a p95 budget is only good to about 1 ms.
- Boot: `uv run tools/headed.py boot --runs 3 [--baseline MS] [--baseline-first-frame MS] [--cpu R]` (three cold loads in
  a kept-awake headed window with a 1920x1080 viewport). Two spans, each the median of the runs: `map_build_ms`
  (heightmap decoded to map built) and `first_frame_ms` (navigation start to the second animation frame after `GAME OK`:
  download, scene load and texture uploads, map build and the first draw with its shader compiles). `--cpu 4` adds CPU
  throttling as a cheap low-end proxy; every line states `cpu=`.
- Headed frame time: `uv run tools/headed.py profile --dpr 1` (`--dpr 2` is the fill-rate stress proxy). `--wait-unity`
  also waits for an interactive Unity editor that has any worktree open (it holds `unity/Temp/UnityLockfile`): close it,
  or the profile waits.
- Screenshots: `tools/art_checkpoint.sh [--no-build] <name>` (WebGL `?shot` + editor renders of both tiers + side-by-side
  images and their deuteranopia/protanopia copies under `captures/visual/<name>/`, git-ignored; rows in the main
  checkout's `captures/visual/art-log.tsv` with the branch, short commit and the image's absolute path). It refuses to run
  while `QualitySettings.asset` (and, when it builds, `unity/Assets/Settings` or `ProjectSettings.asset`) has unstaged
  changes, restores them on every exit, and takes the pose list from `PromoPose.cs`. Each checkpoint switches the
  worktree's build target WebGL -> OSXUniversal (editor renders) and back at the next build, a reimport each way: budget
  a few minutes per checkpoint.

## Budgets (proposed; ratified in Task 10)
Measured on the development Mac (Apple silicon, integrated GPU), 1920x1080 at DPR 1, Chrome for Testing:
- headless, all features on: median p95 <= 16.7 ms at both poses, and median(all) - median(none) <= 8 ms;
- each feature: median(all) - median(-feature) <= 2.5 ms (tilt-shift <= 1.5 ms, labels <= 1 ms); a cost counts as over only
  when it exceeds the budget plus the larger of the two spreads;
- headed numbers are recorded, plus a DPR-2 headed run in Task 10 as a fill-rate stress proxy for weaker GPUs; the 30 fps
  floor for an integrated-GPU laptop (research notes 2.3) is 33 ms p95;
- boot: each span (`map_build_ms` and `first_frame_ms`, median of 3, at `--cpu 1` and at `--cpu 4`) grows by at most
  max(300 ms, 50% of its Task 1 baseline): the tool's default when `--budget` is not given (controller ruling 3).

## GLES3 check: the `require` allowlist (Task 3 Step 0)
`ShaderChecks.GlslErrors` fails any `#extension X : require` not in `ShaderChecks.AllowedRequires` (default-deny). An
entry is added only after a WebGL 2 `compileShader` of a shader carrying that line succeeds in headless Chrome.
- Report (`-executeMethod Gles3Report.Run`, 2026-10-10): URP Lit, URP Terrain Lit and `Statecraft/HexSelection` show
  **0 errors and no `require` line**, so `AllowedRequires` stays **empty**. Running the report exposed a harness bug, fixed
  here: `Gles3Errors` looped over `SubshaderCount` but indexed `GetSerializedSubshader`. URP Lit has 2 subshaders and 1
  serialized one, so the report threw. The loop now uses `SerializedSubshaderCount`.
- Probe: full Chrome 153's `--headless=new --dump-dom` never exits on this Mac, so the probe runs in Playwright's
  `chrome-headless-shell` (Chrome for Testing 149.0.7827.55, `--use-angle=swiftshader`). Two references both returned
  `REQUIRE FAILS ... extension is not supported`: `GL_EXT_shader_non_constant_global_initializers` (the plan's example)
  and `GL_EXT_texture_buffer`. So a shader that requires either cannot run on the web tier.

## Baseline (Task 1, before any visual change; re-recorded in fix round 1)
Viewport fixed to 1920x1080 at DPR 1 over CDP; Chrome kept awake; build from `0b6058e` (harness only).

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.29 spread_ms=0.17 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.35 spread_ms=0.14 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=6.84 spread_ms=0.06 median_p95_ms=7.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=6.92 spread_ms=0.05 median_p95_ms=8.00`<br>run line: `PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=0 frames=300 avg_ms=2.43 p95_ms=4.00` |
| headed (DPR 1) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.31 spread_ms=0.57 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.83 spread_ms=0.76 median_p95_ms=3.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.18 spread_ms=0.29 median_p95_ms=8.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.15 spread_ms=0.40 median_p95_ms=8.00`<br>run line: `PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=0 frames=300 avg_ms=1.78 p95_ms=3.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=443 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3547 runs=3 cpu=1` |
| boot, `--cpu 4` | `BOOT map_build_ms median=1813 runs=3 cpu=4`<br>`BOOT first_frame_ms median=6963 runs=3 cpu=4` |

Boot budgets from these (ruling 3, baseline + max(300 ms, 50%)): `--cpu 1` map build 443 -> 743 ms, first frame
3547 -> 5321 ms; `--cpu 4` map build 1813 -> 2720 ms, first frame 6963 -> 10445 ms.

Notes:
- The headed window is clamped by the laptop screen (1728 px wide); the emulated 1920x1080 viewport still gives a
  1920x1080 backbuffer (`res=1920x1080`), only the final composite to the window is scaled.
- No toggles exist yet, so `all` and `none` are the same scene: their difference and the spreads are the noise floor.
  Headless spreads are 0.05-0.17 ms; headed gameplay03 is noisier (spreads 0.57-0.76 ms, `all` vs `none` 0.48 ms apart).
- Superseded first recording (commit `33fb072`, before the CDP viewport): headless ran at `res=1920x937` and headed at
  `res=1728x860`, with a first-pose spread of 0.68 ms (headless) and one headed run that stalled; do not compare with it.

## Baked textures (Task 5)
`tools/terrain_bake.py` regenerates `unity/Assets/Textures/Map/terrain_bake.png` and `coast.png` from the committed
heightmap in about 10 s. The output is deterministic per toolchain: re-running it with the pinned numpy 2.5.3 and
pillow 12.3.0 on the same machine gives byte-identical PNGs. Another platform may differ in its zlib build or in
1-ulp `sin`/`arctan` results. Compare decoded pixels (`np.array_equal`), not file bytes; R and B may then differ by
one level here and there.

## Task 6: terrain
The Statecraft Terrain Lit Shader Graph (`tools/terrain_graph.py` + `StatecraftTerrain.hlsl`): one terrain layer (one
pass) instead of seven, no basemap fallback. WebGL build (`-compression disabled`): `BUILD Succeeded WebGL ->
Builds/Web-disabled: 73.4 MB on disk, 0 errors` (Task 1: 70.9 MB; +2.5 MB, the two baked textures), no `Shader error`.
GLES3 check: `ShaderChecks.Gles3Errors` is empty for `Statecraft/StatecraftTerrain` (no `#extension ... : require`;
`AllowedRequires` not needed).

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.58 spread_ms=0.93 median_p95_ms=2.00`<br>`PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=1.88 spread_ms=0.87 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.67 spread_ms=0.40 median_p95_ms=3.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.25 spread_ms=0.47 median_p95_ms=8.00`<br>`PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=7.13 spread_ms=0.11 median_p95_ms=8.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.16 spread_ms=0.20 median_p95_ms=8.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=509 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3851 runs=3 cpu=1` (budgets 743 / 5321 ms: within) |

Against the Task 1 baseline (`all`): gameplay03 2.29 -> 1.58 ms, diorama02 6.84 -> 7.25 ms (+0.41 ms, inside 2.5 ms plus
the spread). Haze costs nothing measurable (`all` - `-haze`: -0.30 / +0.12 ms, both inside the spreads). The boot phase
`biome texture baked` takes 94-106 ms (it replaces Task 1's `biome colours blended`).

### Task 6 fix round 1
Haze retuned (review I1: start 200 km, scale 250 km, max 0.45, rift boost 0.6, colour sRGB (0.84, 0.74, 0.58)), ground
palette x1.107 in sRGB, curvature gain 0.40, graph and nodes at Single precision, HDRP target dropped, trilinear map
textures (bake aniso 2), the fine noise octave faded by its pixel footprint. The AO is its own uncompressed R8 texture
(`ao.png`): measured against the source PNG, BC1 stepped it by 2+ levels on 404 of 681 broad Judean texels (59%); R8
gives 0 of 681 (Judean slopes) and 0 of 6206 (Negev plateau). WebGL build: 76.9 MB (+3.5 MB for `ao.png`), no
`Shader error`. GLES3 report (with vis-light `e8b1c48`'s serialized-subshader loop): 0 errors and no `require` for
`Statecraft/StatecraftTerrain`, `Statecraft/HexSelection`, URP Lit (the sea on this branch) and URP Terrain Lit.

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.41 spread_ms=0.88 median_p95_ms=4.00`<br>`PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=2.34 spread_ms=0.48 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.46 spread_ms=0.07 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.29 spread_ms=0.07 median_p95_ms=8.00`<br>`PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=7.31 spread_ms=0.10 median_p95_ms=8.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.34 spread_ms=0.09 median_p95_ms=8.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=452 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3559 runs=3 cpu=1` (budgets 743 / 5321 ms: within) |

Art (WebGL, whole frame; vis-ground has no Task 2 light, so compare with t01): diorama02 warmth 0.017 (t01) / 0.007
(t06) -> 0.038, saturation 0.36 -> 0.41, contrast 0.171 -> 0.173, palette distance 0.85 -> 0.78; map land alone (column
x 0.45-0.6) R-B 0.158 -> 0.229, saturation 0.254 -> 0.363, luma 0.576 -> 0.572. gameplay03 warmth 0.009 (t01) / 0.017
-> 0.040, contrast 0.163 -> 0.173, palette distance 0.69 -> 0.66. The checkpoint also shoots the 6 km hero zoom over the
Judean slopes and the Negev (`PromoPose.CloseChecks`, haze off): no AO steps.

## Task 7: sea
One water function (`StatecraftMap.hlsl` `ScWaterShade`) on both sides of the map's edge: the terrain graph outputs it as
emission on a black, fully metallic, fully occluded surface, and the border skirt's `Sea.mat` (`Statecraft/Sea`, passes
`Sea`, `DepthOnly`, `DepthNormals`) returns it directly. It is driven by Task 5's coast distance: a turquoise -> teal -> navy
ramp over 12 km, foam within 350 m, the Dead Sea pale turquoise with a white salt rim. It samples the main light's shadows
itself, takes the cooler water haze (`ScHazeWater`), and falls back to the flat sea colour under the `water` toggle. WebGL
build: 77.1 MB, no `Shader error`. GLES3: `ShaderChecks.Gles3Errors` is empty for `Statecraft/Sea` and the terrain (no
`require`).

Deviations from the plan:
- **Glint 0.06, not 0.25.** With `Map.unity`'s default 50-degree sun, the camera's 50-degree pitch at 120 km meets the
  reflection at yaw 180. There the 0.25 lobe lit a warm patch rising 0.08 in sRGB red over 6 km of open sea, and the seam
  test (samples 5 km apart) read it as a 0.031 seam. A profile across the edge showed no step at the edge itself, and the
  difference scaled with the glint. At 0.06, every seam line is <= 0.004.
- **The held skirt is averaged along the edge.** Holding each edge vertex's own height for 20 km
  (`SkirtFall = {0, 0.05, 0.15, 0.45, 1}`) drew the edge's ridges out as long stripes across the Jordan skirt (checkpoint
  t07-sea, first run). Rings 1-4 now hold the edge height averaged over +-8 km (`MapBuilder.SkirtHoldSmoothingM`). Ring 0
  still meets the terrain exactly. New test `TheHeldSkirtCarriesThePlateauNotTheEdgesRidges`: the mean spoke-to-spoke step
  20 km out is 8.5 m against 36.7 m at the edge (31.2 m before).

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.38 spread_ms=0.42 median_p95_ms=4.00`<br>`PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=2.35 spread_ms=0.08 median_p95_ms=4.00`<br>`PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=1.70 spread_ms=0.61 median_p95_ms=3.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.72 spread_ms=0.27 median_p95_ms=2.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.64 spread_ms=1.56 median_p95_ms=9.00`<br>`PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=7.67 spread_ms=0.29 median_p95_ms=8.00`<br>`PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=7.78 spread_ms=0.35 median_p95_ms=9.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.48 spread_ms=0.02 median_p95_ms=8.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=472 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3592 runs=3 cpu=1` (budgets 743 / 5321 ms: within) |

Against Task 6 fix round 1 (`all`): gameplay03 2.41 -> 2.38 ms, diorama02 7.29 -> 7.64 ms (+0.35 ms, inside 2.5 ms plus the
spread). Water costs 0.68 ms at gameplay03 (`all` - `-water`, against a 0.61 ms spread) and nothing measurable at
diorama02 (-0.14 ms), well inside 2.5 ms. The profile ran on the build before the skirt averaging, which changes no
shader. The boot lines are from the final build.

Art (WebGL): diorama02 saturation 0.41 -> 0.39, contrast 0.173 -> 0.186, warmth 0.038 -> 0.058, palette distance 0.78 ->
0.76. gameplay03 contrast 0.173 -> 0.194, palette distance 0.66 -> 0.65. The sea grades from turquoise shallows with a
foam line to navy, and the Dead Sea is pale turquoise with white salt rims. Coast streaks: none visible. A 12x high-pass of
the gameplay03 coast shows only the foam band's waves and single-level output steps in the deep water, so the bake's
distance needs no smoothing (Task 5 carry). Plateau edge (diorama02): the Jordanian plateau now continues past the east
edge at the edge's broad height instead of sagging at once, as a flat muted band level with the map's eastern rim. A
faint low-contrast banding remains there at grazing angles.

## Checkpoints
| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
| 6 | t06-terrain | captures/visual/t06-terrain/*-compare*.png, editor/*-compare*.png | (owner) |
| 6 (fix 1) | t06-terrain-r1 | captures/visual/t06-terrain-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 7 | t07-sea | captures/visual/t07-sea/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
