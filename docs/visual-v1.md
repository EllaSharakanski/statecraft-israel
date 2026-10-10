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
- **Probe (corrected in Task 3 fix round 1).** In WebGL 2, `#extension GL_X : require` compiles only after the page has
  called `gl.getExtension("<WebGL name>")`. The first probe never called it, so it could only ever say FAILS. The procedure
  (page and command in `.superpowers/sdd/2026-10-11-statecraft-visual-v1/task-3-carry.md`):
  1. Run Playwright's `chrome-headless-shell` (Chrome for Testing 149.0.7827.55) with `--use-angle=swiftshader
     --enable-unsafe-swiftshader --dump-dom` under a timeout. Full Chrome's `--headless=new --dump-dom` never exits on this
     Mac. Match the output with `grep -E`, because BSD grep ignores `\|`.
  2. Log `gl.getSupportedExtensions()`.
  3. **Positive control.** For supported extensions that have a GLSL directive, compile a `require` shader before and after
     `gl.getExtension`. It must FAIL before and print OK after. If no control passes, the probe is broken, not the
     extension.
  4. Enable every supported extension, as Unity's loader does, then compile the `require` under test (vertex and
     fragment).
  5. **Unity's loader.** The WebGL framework (`*.framework.js`, emscripten) calls `GLctx.getExtension(ext)` for every
     entry of `getSupportedExtensions()` except names containing `lose_context` or `debug`. So step 4 matches the player,
     and a GLSL extension whose WebGL extension is supported is also enabled in game.
- **Result (2026-10-10, SwiftShader).** Supported: EXT_clip_control, EXT_color_buffer_float, EXT_color_buffer_half_float,
  EXT_conservative_depth, EXT_depth_clamp, EXT_disjoint_timer_query_webgl2, EXT_float_blend, EXT_polygon_offset_clamp,
  EXT_texture_compression_bptc/rgtc, EXT_texture_filter_anisotropic, EXT_texture_mirror_clamp_to_edge,
  NV_shader_noperspective_interpolation, OES_draw_buffers_indexed, OES_sample_variables,
  OES_shader_multisample_interpolation, OES_texture_float_linear, OVR_multiview2, WEBGL_clip_cull_distance,
  WEBGL_compressed_texture_*, WEBGL_multi_draw, WEBGL_polygon_mode, WEBGL_stencil_texturing (plus the debug and
  lose_context ones).
  - Positive controls: `GL_OVR_multiview2`, `GL_EXT_conservative_depth`, `GL_NV_shader_noperspective_interpolation`,
    `GL_OES_sample_variables`, `GL_OES_shader_multisample_interpolation` and `GL_ANGLE_multi_draw` each FAIL before
    `getExtension` and are **OK after** it, in both stages.
  - With `WEBGL_clip_cull_distance` enabled, `GL_ANGLE_clip_cull_distance` is OK but `GL_EXT_clip_cull_distance` FAILS.
    WebGL's GLSL names can differ from GLES's, so the probe must use the exact name that Unity's GLSL requires.
  - Targets: `GL_EXT_shader_non_constant_global_initializers` and `GL_EXT_texture_buffer` still FAIL ("extension is not
    supported") with every supported extension enabled. Neither is a WebGL extension, so the negatives stand.
  - `AllowedRequires` stays empty, because none of our shaders requires anything. An extension is added only when one of
    our shaders needs it and its probe prints OK. Note that SwiftShader's list is not every GPU's list: a desktop-GPU run
    can list more.

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

## Task 2: sun and shadows
Headless, viewport 1920x1080 at DPR 1 (`res=1920x1080 scale=0.80`), web tier (shadow map 2048, one cascade, cascade
border 0.1).

**Shadow fit (fix round 1, review I1).** URP fades shadows by straight-line distance from the camera, from
(1 - cascadeBorder) x shadowDistance out (URP 17.3 `Shadows.hlsl`, `ShadowUtils.GetScaleAndBiasForLinearDistanceFade`).
The first delivery fitted 1.75 x the rig distance from *view depth*, which left the top corners partly or wholly faded
(about 76% faded on the web tier at a 0.08 top inset). `ShadowFit` now takes, on every rig `Apply`, the farthest
straight-line distance from the camera to the ground hits of the four viewport corners (the current aspect and top inset
included; ground plane y = 0, the terrain's lowest point, so the fit is never short) and divides it by
(1 - cascadeBorder). It clamps to 3 km .. 1,000 km (the far clip). `LightingTests.TheShadowDistanceCoversTheWholeView`
checks 6/45/120/250 km x 16:9, 21:9, 4:3 x top insets 0, 0.08, 0.3, 0.5 x borders 0.1 and 0.2.

**Decision: web-tier cascade border 0.2 -> 0.1** (`Mobile_RPAsset` `m_CascadeBorder`). With one cascade, the border is
the fade band at the edge of the fitted range. A tenth keeps the fade off-screen while spending less of the 2048 map past
the view (the fitted distance is 1/0.9 rather than 1/0.8 of the farthest corner). PC keeps 0.108 (4 cascades).

| Run | Lines |
|---|---|
| headless, first delivery (`8b20507`, 1.75 x D view-depth fit, border 0.2) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.11 spread_ms=0.10 median_p95_ms=3.00`<br>`PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.43 spread_ms=0.11 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.12 spread_ms=0.43 median_p95_ms=11.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.39 spread_ms=0.17 median_p95_ms=8.00` |
| headless, fix round 1 (corner fit, border 0.1) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.03 spread_ms=0.09 median_p95_ms=3.00`<br>`PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.42 spread_ms=0.08 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.87 spread_ms=0.46 median_p95_ms=11.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.12 spread_ms=0.11 median_p95_ms=8.00` |

Budget check, fix round 1 (`median(all) - median(-shadows) <= 2.5 ms + the larger spread`):
- gameplay03: -0.39 ms against 2.59 ms: within.
- diorama02: 2.75 ms against 2.96 ms (2.5 + 0.46): within, with 0.21 ms to spare.

**For Task 10 (review M5).** At gameplay03, `all` is faster than `-shadows` by more than both spreads, in both deliveries.
Three runs understate the run-to-run noise, so the 0.2 ms headroom at diorama02 means little. Re-measure diorama02
shadows with more repeats (at least 7) and headed. If the tier review needs headroom, the levers are:
- terrain shadow LOD or pixel error (the whole terrain is drawn into the shadow map at about 400 km);
- a cheaper fit at the far zoom (fit to the map's extent rather than the frustum once the frustum is wider than the map);
- a web-tier shadow map of 1024 at the far zoom only.

**Rendered coverage (review I2, I3).** `ReliefCastsShadowsAtMapScale` runs at both tiers over the Dead Sea escarpment at
60 km. Every other registered visual except `water` is off and the settlement markers are hidden, so only relief casts.
Results: shadowed share **3.06% (web)** and **3.12% (PC)**; the control at the asset's own 50 m is **0.00%** at both.
Hiding the markers moved the web share from 3.03% to 3.06%, which is noise, so the cubes were never a material part of
it. The 2% floor stays: it allows a third of the measured share to be lost to the merge (Task 3's tilt-shift softening
edges, Task 4's grade compressing darks, Track B's haze lifting distant shadow), and the control's 0.00% shows the
separation is clean. If the merge squeezes it further, switch to a relative test (`lit < flat * 0.9`), not a lower floor.

**Ambient (review M4). Superseded by Task 4:** the explicit probe described here was never read. In Trilight mode Unity
replaces it with its own linear conversion of the colours, and Task 4 removed `MapLighting.AmbientProbe`. The evaluated
values below are that conversion, and they match the constants once converted to sRGB; see "Task 4: grade". The original
note follows. `MapLighting.AmbientProbe` builds the trilight from `AddAmbientLight` plus two
`AddDirectionalLight` lobes. The lobes add constant and linear terms in every direction, so the probe evaluates darker
than its constants, which are inputs, not the evaluated colours. Evaluated values, pinned in `LightingTests`: up
(0.216, 0.255, 0.326), horizon (0.184, 0.179, 0.171), down (0.119, 0.103, 0.080); the constants are sky (0.52, 0.60,
0.72), equator (0.48, 0.46, 0.42), ground (0.26, 0.22, 0.17). The images were judged on the evaluated values, and Track
B's thresholds were derived with them, so a least-squares refit would be a look change for Task 4, not a fix here.

## Task 3: tilt-shift
Two bands, blurred by a half-resolution render-graph pass (`TiltShiftFeature` on both renderers, `Statecraft/TiltShift`):
- the blur is a separable 9-tap Gaussian whose radius is `TiltShift.Weight(y) * MaxRadiusUv`;
- the focus sits at the middle of the map area below the top bar;
- the blur weakens to 35% between 60 km and the 250 km overview.

URP's far-only Gaussian DoF is retired. Toggle: `tiltshift`; the URL flag `notiltshift` (desktop: `-notiltshift`) boots with
it off.

**Constants (fix round 1, review I2).** HalfWidth 0.13, Feather 0.17 and MaxRadiusUv 14/1080 (was 0.17, 0.18 and
10/1080). Full blur now starts 0.30 from the focus. That is the bottom and top 20% with no bar; under the web tier's top
bar (8.8% of the screen) it is the bottom 15.5%, and 15.5% between the band and the bar. The promo has about 15% at the
bottom and 20% at the top.

**Edit-mode ratios** (gradient energy shifted / sharp, gameplay pose, 960x540, no bar):

| Tier | top 0.90–0.98 | bottom 0.02–0.10 | middle | top 0.81–0.87 | bottom 0.13–0.19 | top at 1920x1080 |
|---|---|---|---|---|---|---|
| Mobile | 0.16 | 0.24 | 1.00 | 0.17 | 0.22 | 0.14 |
| PC | 0.11 | 0.18 | 1.00 | 0.12 | 0.15 | 0.11 |

TopInset 0.3: focus band 1.00, upper band 0.13. (Before fix round 1: Mobile top 0.22 and bottom 0.31; PC 0.16 and 0.24.)

**WebGL band check, with the real top bar (review m3).** I compared the checkpoint's `gameplay03.png` with a
`?shot&notiltshift` shot from the same build, using columns 30–80% (clear of the info panel and minimap) and rows measured
from the top. The bar ends at 0.088.
- Ratios by row (from the top):

  | Rows | 0.10–0.275 | 0.30 | 0.325 | 0.35 | 0.375 | 0.425–0.675 | 0.70 | 0.725 | 0.75 | 0.775 | 0.80–1.00 |
  |---|---|---|---|---|---|---|---|---|---|---|---|
  | Ratio | 0.01 | 0.13 | 0.32 | 0.60 | 0.86 | 1.00 | 0.74 | 0.45 | 0.22 | 0.07 | 0.01 |

- The sharp band is centred at about 0.54 from the top. The focus is at (1 − 0.088)/2 = 0.456 from the bottom, so this is
  the right way up; a flipped composite would centre at 0.456 from the top.
- The review's check: rows 0.20–0.30 from the top (nearer the bar) keep **0.02** of their detail, and rows 0.70–0.80 keep
  **0.37**.
- **Coverage.** Fully blurred (ratio <= 0.05) from the bar to 0.30 (**21% of the screen at the top**) and from 0.80 to the
  bottom (**20% at the bottom**). The promo has about 20% at the top and 15% at the bottom. By the design weight (weight =
  1), the coverage is 15.6% at each end.

**Profile** (1920x1080, DPR 1, render scale 0.80, 3 interleaved runs per configuration), fix round 1 build:
```
headless (web_check.py)
PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.96 spread_ms=0.13 median_p95_ms=3.00
PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=2.21 spread_ms=0.79 median_p95_ms=4.00
PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.65 spread_ms=0.20 median_p95_ms=10.00
PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=9.72 spread_ms=0.26 median_p95_ms=11.00
headed (headed.py profile --dpr 1)
PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.01 spread_ms=0.39 median_p95_ms=3.00
PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=1.89 spread_ms=0.35 median_p95_ms=3.00
PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.11 spread_ms=0.54 median_p95_ms=11.00
PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=10.55 spread_ms=0.59 median_p95_ms=11.00
```
Cost `median(all) - median(-tiltshift)`, all within the 1.5 ms budget and within the spreads:

| | gameplay03 | diorama02 |
|---|---|---|
| headless | −0.25 ms | −0.07 ms |
| headed | +0.12 ms | −0.44 ms |
| first build: headless | −0.08 ms | +0.48 ms |
| first build: headed | +0.01 ms | +0.07 ms |

## Task 4: grade
`MapGrade` adds the following to the rig's runtime `VolumeProfile`, all behind the `grade` toggle, with LDR grading on both tiers:
- a Neutral tonemapper;
- post exposure +0.15, contrast 14, saturation 16;
- white balance temperature **5** (the plan says 9; see the merge arithmetic below) and tint 2;
- split toning: teal shadows (0.36, 0.52, 0.60), amber highlights (0.96, 0.78, 0.55), balance −15;
- a dark-navy vignette: 0.26, smoothness 0.45.

**What was there before (measured).** The pipeline assets' own volume profile, `Settings/SampleSceneProfile` (the URP template's), already applied the following to every frame:
- Neutral tonemapping;
- bloom 0.25 above 1 (high quality);
- a 0.2 black vignette.

So the build was not "without a tonemapper". The `grade` toggle off falls back to that profile, not to a raw image. Two consequences:
- The control in `TheSunlitDesertDoesNotClip` sets the grade's tonemapper to None. That overrides the template's Neutral, and the graded Negev then clips 2.3% of its pixels, against 0.00% with Neutral.
- URP strips the tonemapper's uber variants unless some VolumeProfile *asset* has a Tonemapping component. The runtime profile is not an asset, so it does not count. `TheGradeIsWarmLandTealShadowsAndAGentleVignette` checks that such an asset exists. Today two assets carry one: the template's `SampleSceneProfile` and the global settings' `DefaultVolumeProfile`. The check fails only if both lose their Tonemapping component. If Task 10 drops the template profile (the bloom decision), it should add a project-owned VolumeProfile with Neutral, so the guarantee no longer rests on URP template leftovers.

Bloom is the template's, not this plan's; Task 10 should decide whether the web tier keeps it.

**Readability (review I9), from edit mode at 960x540, with the tilt-shift off for these detail checks.**
- ΔE76 between the selection band and the land 1 km inside the hex:

  | Pose | Graded | Ungraded |
  |---|---|---|
  | gameplay03 | 32.3 | 24.5 |
  | diorama02 | 48.6 | 36.6 |

- Colour-blind check (Machado 2009, gameplay pose):

  | Simulation | sea vs land | selection vs land | selection vs sea |
  |---|---|---|---|
  | Deuteranopia | 68.6 | 31.8 | 98.6 |
  | Protanopia | 67.8 | 31.5 | 97.6 |

- Saturation rises from 0.438 to 0.564.
- The vignette's corner/centre ratio drops from 0.603 to 0.483.
- In the checkpoint's deuteranopia and protanopia copies, the coast (blue against yellow) and the hex lines stay clear.

**Ambient (review M4, closed by measurement).**
- **How the probe actually works.** `MapLighting.AmbientProbe()` was never what the shaders read. In Trilight mode Unity replaces any assigned probe with its own conversion of the three colours, at once. It does this in the editor, and in the WebGL player too: a diagnostic build logged the getter at Apply, frame 120 and frame 600.
  - Editor renders with our probe, a least-squares refit and a black probe are identical to 3 decimals. Changing the trilight colours changes the image.
- **Why it looked like 40%.** Unity converts the sRGB colours to linear. The "40% of nominal" (horizon 0.18 against 0.48) compared linear values with sRGB constants. In sRGB the horizon is (0.466, 0.460, 0.450) against the equator's (0.48, 0.46, 0.42).
- **Decision: keep the look.** The dead explicit probe is removed, which changes nothing rendered. `LightingTests` now asserts the horizon is within 0.05 of the equator in sRGB.
- **Why no brighter refit.** A refit that brightens the ambient towards the constants (trilight colours raised so the probe's linear values reach the constants) does not help. Measured with the grade, in editor renders:

  | Pose | Warmth | Saturation | Luma | Contrast |
  |---|---|---|---|---|
  | gameplay03 | 0.142 → 0.082 | 0.572 → 0.518 | +0.03 | unchanged |
  | diorama02 | 0.154 → 0.090 | 0.526 → 0.458 | +0.03 | unchanged |

  That works against the merge's warmth target. It would also shift every Track B threshold, which were derived with the current ambient. Nothing Track B measures moves.

**Grade terms at diorama02 (editor, Mobile tier, no UI; each line takes one term out of the full grade).** With the full grade: warmth 0.154, saturation 0.526, contrast 0.110. With the grade off: warmth 0.120, saturation 0.419, contrast 0.088. The temperature here was 9.

| Term | Warmth | Saturation | Other |
|---|---|---|---|
| Saturation 16 | +0.027 | +0.084 | |
| White balance 9 | +0.030 | +0.014 | |
| Split toning | −0.039 (the teal shadows) | −0.041 | |
| Vignette | −0.018 | | |
| Contrast | | | +0.011 contrast |

**Merge arithmetic (diorama02 warmth, WebGL frame).**
- **Track A alone at Temperature 9:** t04 is 0.118 (t03 was 0.088).
- **Track B's contribution:** t06-terrain-r1 0.0384 − t01 0.0171 = **+0.021** before the grade.
- **How the grade scales it:** the grade's saturation (×1.16), contrast and exposure scale chroma by about 1.0–1.3, so Track B's +0.021 becomes +0.021 to +0.027.
- **At Temperature 9:** 0.118 + 0.021…0.027 = **0.139–0.145**. That is already at the promo's 0.145, before Track B's Task 7 haze and palette work.
- **Lowering the temperature:** one unit is about 0.003 of frame warmth: 0.030 for 9 units in the editor, × 0.88 for the WebGL frame's UI share. So Temperature 5 gives 0.118 − 4 × 0.003 = 0.106, and the checkpoint measured **0.106**.
- **Expected merged diorama02 warmth: 0.106 + 0.021…0.027 = 0.127–0.133.** That is at least 0.087 by 0.04, and leaves 0.012–0.018 for Track B's later warming.
- **Contrast:** t03 0.153 → t04 0.170, plus Track B's +0.014 (t06-r1 0.1731 − t01 0.1588), gives about **0.18–0.19** after the merge, moving towards the promo's 0.200.

| WebGL | Saturation | Contrast | Warmth | Promo (sat / contrast / warmth) |
|---|---|---|---|---|
| diorama02, t03 | 0.422 | 0.153 | 0.088 | 0.425 / 0.200 / 0.145 |
| diorama02, t04 (temp 9, `9603700`) | 0.52 | 0.169 | 0.118 | |
| diorama02, t04 (temp 5, `2ac7b6e`) | 0.51 | 0.170 | 0.106 | |
| gameplay03, t03 | 0.444 | 0.155 | 0.082 | 0.382 / 0.203 / 0.064 |
| gameplay03, t04 (temp 5) | 0.55 | 0.174 | 0.107 | |

**Over the promo (owner's call).**
- Saturation is now about 0.09 above the promo at diorama02 and 0.17 above it at gameplay03. The lever is `Saturation` (16 → 8 is about −0.04).
- Gameplay03 warmth is already above its promo (0.064) on Track A alone, and Track B adds about +0.03. The promo's gameplay frame has green fields and orchards, which is the art track's vegetation, and a global grade cannot fix that without cooling the diorama.

**Seam test.** `TheSeaBeyondTheEdgeMatchesTheTerrainSea` measures the seam twice:
- **Before the grade, against 0.012.** This isolates the grade, which is a per-pixel LUT plus a vignette that is flat this close to the centre, so it cannot open a seam.
- **On the shipped, graded frame, against 0.024.** The grade scales the sea's chroma by about 2: the sub-step mismatch between the terrain sea and the skirt shader goes from 0.008 to 0.016.

At the merge, Track B's patch-mean version should keep this graded/ungraded loop.

**Profile** (headless, 1920x1080, DPR 1, scale 0.80, 3 interleaved runs; build `9603700`. Temperature only changes LUT values, so the cost is the same at `2ac7b6e`):
```
PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.95 spread_ms=0.39 median_p95_ms=3.00
PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=1.93 spread_ms=0.37 median_p95_ms=3.00
PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.74 spread_ms=0.06 median_p95_ms=11.00
PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=9.74 spread_ms=0.02 median_p95_ms=11.00
```
The grade costs +0.02 ms at gameplay03 and 0.00 ms at diorama02, inside the noise. The tonemapper and the vignette were already in the uber pass (the template profile), and the LUT pass ran before too.

## Checkpoints
| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
| 2 | t02-light | captures/visual/t02-light/*-compare*.png | (owner) Note: at the new 6 km hero zoom the view is filled by the 600-1,800 m placeholder marker cubes, so judge the zoom range, not the close-up art, until the settlement art track replaces them. |
| 3 | t03-tiltshift | captures/visual/t03-tiltshift/*-compare*.png (re-shot in fix round 1: bands 0.13/0.17, radius 14/1080) | (owner) Note: the map labels are still world-space text, so they blur with the map in the top and bottom bands until Task 9 moves them to UI Toolkit. |
| 4 | t04-grade | captures/visual/t04-grade/*-compare*.png (with -deuteranopia / -protanopia copies) | (owner) Note: saturation is above the promo (0.51/0.43 diorama, 0.55/0.38 gameplay); `MapGrade.Saturation` is the lever. |
