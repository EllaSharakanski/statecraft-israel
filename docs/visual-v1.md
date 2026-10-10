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

### Task 7 fix round 1
- **Haze at full width (review I1, the Task 6 carry).**
  - At diorama02, the haze toggle moves the far band 0.047 and the near band 0.002 over the full width. Task 6 measured
    0.023 / 0.026, with the sea and skirt unhazed. Restricted to the land column it was 0.069 / 0.026.
  - So `TheHazeThickensWithDistance` and `TheHazeStartsBeyondTheNearestDioramaLandAndGrowsWithDistance` sample x 0..1
    again. By distance: near 0.0019, mid 0.0212, far 0.0473.
  - `TheOpenSeaStaysBlueUnderTheHaze` now asserts that the water haze applies. The open sea at about 300 km moves 0.059
    in luma with the haze on, and 0.000 with `ScHazeWater` dropped on both sides (the red check).
- **Seam test close pair (M1).**
  - The pair sits at +-600 m across the edge. Each side is the mean of five 7x7 patches, 2 km apart along the edge,
    because a single 1 km patch carries one wave cell or glint sparkle on its own (up to 0.012).
  - A step of 5% on the skirt sea only within 1.5 km of the edge passes the +-2.5 km pairs (0.000) and fails the close
    pair (0.015). All 16 lines are <= 0.004.
- **Skirt hold (M4).**
  - The hold is now a running mean over the edge's own samples (prefix sums, +-8 km). It replaces the 17 taps 1 km apart.
  - East edge, spoke-to-spoke step 20 km out: 8.5 -> 5.2 m. What remains is the plateau's own trend along the edge.
  - The test bound is under 0.18 x the edge's step.
  - The faint grazing-angle banding in diorama02 is gone.
- **Land skips the water shading (M3).** `[branch]` on `water > 0`.
  - gameplay03 `all` minus `-water`: 0.03 ms before, 0.01 ms after. Both are inside the spreads, so there is no
    measurable change on this GPU.
  - Before, on the build at `6757168`: `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.49 spread_ms=0.17`
    and `visual=-water ... median_avg_ms=2.46 spread_ms=0.06`.
  - After: `visual=all ... median_avg_ms=2.55 spread_ms=0.07`, `visual=-water ... median_avg_ms=2.54 spread_ms=0.02`;
    diorama02 `all` 7.34 ms (before 7.38 ms).
  - The earlier 0.68 ms water cost was run-to-run noise.
- **Build and boot.** WebGL 77.1 MB, no `Shader error`. Boot `--cpu 1`: `map_build_ms` 469, `first_frame_ms` 3598
  (budgets 743 / 5321).

## Task 8: grid
The hex grid is drawn by the terrain graph and by the skirt's `Sea.shader` from one distance field (`ScHexEdgeDistance`,
tools/hexgrid.py's layout; `HexGrid` is the C# twin), anti-aliased with `fwidth`, 1.5 px wide, light on land (0.25) and
water (0.35), fading to 0.6 of that from 40 km to 240 km camera distance. The `MeshTopology.Lines` overlay, its material,
`OverlayClearanceM` and its boot phase are gone; the selected-hex band sits 30 m above the ground (was 140 m). Toggle
`grid`, registered in `MapShaderGlobals.Apply` through `assets.OnTeardown`. All numbers here are pre-grade (Track A's grade
and tilt-shift are not in this worktree).

- **Rendered tests (EditMode).** Edge luma change 0.059 (hills, 30 km). Grid coverage: 3.08% at 20 km, 18.15% at
  250 km (bound 0.05%-25%). Rift hex lines: Beit She'an 0.062, Jericho 0.058, Dead Sea shore / Arava 0.016 (light lines
  on the bright desert are faint; that test's bound is 0.012, the plan's 0.03 holds only off the desert).
- **Seam.** Task 7's pairs and close strip, grid off: 16 lines, max 0.004. Grid on: 12 asserted lines, max 0.008; the
  close strip at yaw 90/270 is logged only (0.014 at yaw 270: the x = 0 hex line falls unequally into the two
  foreshortened patches). The plan's hex-centre pairs at x = +-4330 m were dropped: the middle-row pair differs by 0.027
  at yaw 180 with the grid off too (a warm sun glint on the terrain side), a pre-existing water effect.
- **Profile (WebGL, 1920x1080).**
  - `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.42 spread_ms=0.74 median_p95_ms=4.00`
  - `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=2.41 spread_ms=0.22 median_p95_ms=4.00`
  - `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.30 spread_ms=0.12 median_p95_ms=8.00`
  - `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=7.28 spread_ms=0.34 median_p95_ms=8.00`
  - The grid costs 0.01-0.02 ms, inside the spreads; with the line mesh's draw call gone, gameplay03 `all` is 2.42 ms
    (Task 7 fix round 1: 2.55 ms).
- **Build and boot.** WebGL 77 MB, no `Shader error`; no GLES derivative error (no `#pragma require derivatives`
  needed). Boot `--cpu 1`: `BOOT map_build_ms median=419`, `BOOT first_frame_ms median=3442` (Task 7: 469 / 3598).
- **Look (t08-grid).** The sea lattice runs on past the map's edge with no step. At the 250 km overview the sea lattice is
  dense and reads strongly; land lines are faint, nearly invisible on the desert.

### Task 8 fix round 1
- **Line anti-aliasing (review I1).** `fwidth` of the V-shaped edge distance collapsed on the line itself. The fix projects
  the derivatives of the continuous grid coordinate on the nearest edge's normal (metres per pixel across the edge) and
  box-filters: `saturate(0.5 * widthPx + 0.5 - d / aa)`.
  - New rendered test `GridLinesAreUnbrokenAndAboutTheirWidthAtGameplay03` (1920x1080, opacities forced to 1, linear luma).
    - Red at b409955: sea peak ratio 0.41 along one edge, FWHM 2.74 px.
    - Green: sea ratio 0.85, FWHM 1.66 px (1.51-1.95); land FWHM 1.62 px (1.37-1.97).
- **Weights (review I2).** `GridWaterOpacity` 0.35 -> 0.10. The distance fade is now lerp(1, 0.35). Lines also fade out as a hex
  shrinks from 30 to 10 px on screen (circumradius over the pixel footprint). Land stays 0.25 until the grade merges.
  - Sea line in `gameplay03` (x 120-170, y 480-560): background 49, peak 83, +34, ratio 1.69, 2 px wide. Before: +86,
    2.75, 4 px. Promo (x 470-520, y 550-580): background 109, peak 151, +43, ratio 1.39. Our ratio is higher only
    because our sea is darker (pre-grade).
  - diorama02 near sea line: +6 over 73 (before +50). Grid coverage at 1920x1080: 0.74% at 20 km, 0.76% at 250 km
    (was 3.1% / 18.2% at 960x540).
- **Seam (M1).** The close strips sit on even hex rows (x = 0 a hex centre); all 32 lines (grid off and on, four yaws)
  are asserted, max 0.004.
- **Rift.** Arava line change 0.015 (bound 0.012, land opacity unchanged).
- **Profile** (machine load average about 7, spreads up to 1 ms):
  - `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.71 spread_ms=0.97 median_p95_ms=3.00`
  - `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=2.22 spread_ms=0.99 median_p95_ms=4.00`
  - `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.23 spread_ms=0.67 median_p95_ms=8.00`
  - `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=7.22 spread_ms=0.54 median_p95_ms=8.00`
- **Build.** WebGL, no `Shader error`; golden values OK in the checkpoint run.

## Checkpoints
| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
| 2 | t02-light | captures/visual/t02-light/*-compare*.png | (owner) Note: at the new 6 km hero zoom the view is filled by the 600-1,800 m placeholder marker cubes, so judge the zoom range, not the close-up art, until the settlement art track replaces them. |
| 3 | t03-tiltshift | captures/visual/t03-tiltshift/*-compare*.png (re-shot in fix round 1: bands 0.13/0.17, radius 14/1080) | (owner) Note: the map labels are still world-space text, so they blur with the map in the top and bottom bands until Task 9 moves them to UI Toolkit. |
| 4 | t04-grade | captures/visual/t04-grade/*-compare*.png (with -deuteranopia / -protanopia copies) | (owner) Note: saturation is above the promo (0.51/0.43 diorama, 0.55/0.38 gameplay); `MapGrade.Saturation` is the lever. |
| 6 | t06-terrain | captures/visual/t06-terrain/*-compare*.png, editor/*-compare*.png | (owner) |
| 6 (fix 1) | t06-terrain-r1 | captures/visual/t06-terrain-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 7 | t07-sea | captures/visual/t07-sea/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 7 (fix 1) | t07-sea-r1 | captures/visual/t07-sea-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 8 | t08-grid | captures/visual/t08-grid/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 8 (fix 1) | t08-grid-r1 | captures/visual/t08-grid-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 9 | t09-labels | captures/visual/t09-labels/*-compare*.png | (owner) |
| merge | t-merge | captures/visual/t-merge/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png (before the grade re-tune: t-merge-pre, t-merge-r1) | (owner) Note: the land is one dry-grass gold with no fields, orchards or cities yet; that gap is content (art track), not the grade. |
| merge (fix 1) | t-merge-r3 | captures/visual/t-merge-r3/*-compare*.png (t-merge-r2 = with the rejected border colour) | (owner) Note: olive coastal plain and valleys; the selected hex is now the promo's ivory ring colour, gold stays the UI chrome's. |

## Task 9: labels

Settlement names are UI Toolkit pills (`MapLabelsPresenter`, EB Garamond SemiBold on navy with a gold edge) in the
click-through `map-labels` layer, placed in `GameRoot.LateUpdate` with an inline pixel `translate` and hidden while the
game loads. The world-space `TextMesh` labels are gone.

Frame time, headless, DPR 1, `res=1920x1080 scale=0.80` (`?vprofile`, `machine_lock.py --exclusive --wait-unity`):

| Pose | all | -labels | none | all - (-labels) | budget |
|---|---|---|---|---|---|
| gameplay03 | 2.30 (spread 0.15) | 2.36 (0.06) | 2.34 (0.04) | -0.06 ms | <= 1 + 0.15 ms: within |
| diorama02 | 6.82 (0.06) | 6.55 (0.02) | 6.61 (0.14) | +0.27 ms | <= 1 + 0.06 ms: within |

The profile holds the camera still, so after the first frames `Refresh` returns early (unchanged view-projection,
panel size and zoom); the cost measured is the drawing of the pills, not the per-frame placement while the camera moves.

Deviations from the plan text, each forced by a test:
- Projection: `RuntimePanelUtils.CameraTransformWorldToPanel` flips y with `Screen.height`, which is wrong when the camera
  draws to a texture of another size (edit-mode tests: 600 px off). The presenter maps the camera's viewport point
  through `camera.rect` onto the panel's own size instead.
- Declutter at the overview: the Tel Aviv - Lod cluster's markers sit 10-60 px apart there, and a town's pill could not
  avoid a neighbour's marker with any anchor (Rishon LeZion's pill ended 10 px under Jaffa's marker). A pill other than
  a city's is now also hidden when it comes within `NeighbourClearPx` (2 x LiftPx + MarginPx = 16 px) of another
  settlement's marker top. At 90 km this hides nothing (11 of 25 shown, as before); at the overview 10 of 25 are shown.
- City pills: 20 px text and 8 px side padding at first. Fix round 1 restores the plan's 22 px and 10 px (see below).
- `ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading` uses a 1920x1080 panel instead of `UiTestKit`'s: the
  batch-mode screen gives that panel about 0.39 px per point, and UI Toolkit rounds the 1 px border up to one physical
  pixel (2.57 points).

### Task 9 fix round 1 (review I1-I5, M1-M7)

- Panels: pills are culled against the cached rects of the top bar, the info panel, the minimap and the event card,
  marked dirty by GeometryChangedEvent; a pill must lie wholly on screen, and a marker under a panel is no one's
  neighbour. A container resize re-places the pills before the repaint (M4).
- Anchors: the USS anchor class is tried first, then lower right, lower left, lower centre and centred below the marker,
  as a pixel shift on the slot's translate. The legibility rule (more than 2 x own gap + MarginPx from every other
  visible marker top) applies to every pill; a city whose anchors all crowd a neighbour keeps the first that is clear of
  panels and pills. A pill hidden in the last refresh needs HysteresisPx = 2 more (M7).
- Shown sets (MapLabelTests): 90 km: tel_aviv jaffa lod ramla petah_tikva rishon_lezion rehovot netanya (every visible
  marker); overview: jerusalem tel_aviv lod gaza beersheba petah_tikva netanya; diorama02 with the top-bar inset also
  labels Hadera (checkpoint).
- City pills are back to the plan's 22 px and 10 px padding (towns 19 px), so cities read larger (M5). At the
  overview Tel Aviv's pill takes the lower-right anchor, over the sea.
- Refresh allocates nothing (`Is.Not.AllocatingGCMemory`). Editor Mono, 240 refreshes while turning and zooming:
  `LABELS refresh moving avg_us=27 p95_us=38`.
- `?vprofile&vmoving` (FrameProfile, additive): every run restarts at its pose and turns 0.5 degrees per frame.
  Headless, DPR 1, `res=1920x1080 scale=0.80`:

| Pose | all | -labels | none | all - (-labels) | budget |
|---|---|---|---|---|---|
| gameplay03-moving | 2.24 (spread 0.65) | 2.28 (0.78) | 2.44 (0.63) | -0.04 ms | <= 1 + 0.78 ms: within |
| diorama02-moving | 10.21 (0.10) | 10.20 (0.16) | 10.10 (0.34) | +0.01 ms | <= 1 + 0.16 ms: within |
| gameplay03 (still) | 2.47 (0.51) | 2.44 (0.04) | 2.35 (0.34) | +0.03 ms | within |
| diorama02 (still) | 6.76 (0.38) | 6.53 (0.29) | 6.58 (0.41) | +0.23 ms | within |

  diorama02 costs about 3.4 ms more while turning than still, with or without labels (the rest of the scene).

### Task 9 fix round 2 (re-review N1-N3, shown-set robustness)

- A GeometryChangedEvent on a panel (event card, info panel, minimap, top bar) re-places the pills at once, inside the
  layout pass, so a card that opens never covers a pill for a frame (N1). The pills' summed sizes are part of the
  early-out key, so a pill that grows with the camera still is re-placed (N2). `LABELS refresh moving avg_us=28 p95_us=38`.
- N3: WebGL `?smoke=5&navlog` (`SMOKE OK 5 turns 1948-06-19 d3fdd459...`) shows no layout or style warning in the
  console; the only non-game lines are the existing `WebGL: INVALID_ENUM: getInternalformatParameter` driver notices.
- `ShownPillsNeverOverlapAndEachReadsAsItsOwn` pins only unambiguous names (90 km: Tel Aviv, Petah Tikva; overview: Tel
  Aviv, Jerusalem, Gaza, Beersheba), at least 8 and 7 pills, Hadera or Netanya at the overview, and completeness: every
  visible marker not decluttered by zoom with no other visible marker within 2 pill heights + MarginPx is labelled.
  Disabling the anchor fallback turns it red (`overview: shown pills Expected: greater than or equal to 7 But was: 6`).

## Merge point (Tracks A, B, C and main on `visual-v1`)
Merged in order vis-light, vis-labels, main, then vis-ground (`d5037af`). Conflicts: `SettlementTests.cs` (the seam test
now runs Track A's graded/ungraded loop around Track B's pairs and even-row strips, grid off and on, four yaws: 64
asserted checks, max 0.006 ungraded against 0.012 and 0.005 graded against 0.024), `docs/visual-v1.md`,
`tools/README.md`. Carries closed: the stale `_label` filter (Task 9 M8) is gone; Track B's colour and detail render
tests (`TerrainShaderTests`, `SeaTests`, the seam and rift tests) run with the tilt-shift off (task-3 carry m6).

**Counts.** Unity edit mode: base `0a2a391` 346; Track A `5fa3fa9` 382 (+36); Track B `ee555ac` 371 (+25); Track C
`ebfdb73` 361 (+15); main since the base +15 (`UnityMcpGuardTests` 10, `GameInputsTests` 5); minus 1 for
`VisualHarnessTests.AnAllowedRequireIsAcceptedAndAnUnknownOneFailsWithHowToAddIt`, which both A and B carry (the Task 3
Step 0 cherry-pick). 346 + 36 + 25 + 15 + 15 - 1 = **436**, measured `total="436" passed="436" failed="0"`, no CS
warning from `Assets/(Scripts|Editor|Tests)`. Tools 143 + 1 (main) + 29 (Track B) = **173 passed, 1 skipped**. Sim 625
(`git diff 0a2a391 -- sim content` shows only main's owner sign-off `25d2ef6`; no visual track touches either).

**Cross-track interactions found by the render tests.**
- `GradeTests.TheSelectedHexStillStandsOutOnGradedLand` failed at gameplay03 ungraded: deltaE76 18.2 (< 20; graded
  23.1; Track A alone 24.5 / 32.3). Track B's coastal plain is dry-grass gold, the hue of the gold selection band. The
  selection is now warm ivory (1, 0.95, 0.80): 31.9 / 34.1 at gameplay03, 30.7 / 32.2 at diorama02.
- Shadows (`ReliefCastsShadowsAtMapScale`, water on): 4.53% web, 6.99% PC (floor 2%; Track A alone 3.05 / 3.23).
  Haze tests: near 0.004, mid 0.028, far 0.054. Zoom extremes at MinDistance 6 km: grid covers 0.22% (1.57% at
  250 km). Rift line change (bound 0.012): Beit She'an 0.096, Jericho 0.085, Arava 0.026.

**Grade re-tune (Task 4 review I1).** WebGL headless, 1920x1080 at DPR 1:

| Shot | Saturation | Contrast | Warmth |
|---|---|---|---|
| promo 02 / promo 03 | 0.425 / 0.382 | 0.200 / 0.203 | 0.145 / 0.064 |
| diorama02, `Saturation` 16 (t-merge-pre) | 0.5345 | 0.2058 | 0.1405 |
| gameplay03, `Saturation` 16 | 0.6629 | 0.2147 | 0.1509 |
| diorama02, `Saturation` 4 (t-merge-r1) | 0.4860 | 0.2070 | 0.1181 |
| diorama02, **`Saturation` 0 (t-merge)** | **0.4729** | **0.2074** | **0.1099** |
| gameplay03, **`Saturation` 0** | **0.5822** | **0.2168** | **0.1078** |

Diorama02 now meets both bars (warmth in [0.087, 0.145], saturation <= 0.48). `TheGradeLiftsSaturation`: 0.568 ->
0.598 (+5.3%, bar 5%; replaced in fix round 1). Gameplay03 stays 0.20 over its promo in saturation and 0.04 in warmth: the promo's frame is
green fields and orchards, which no global grade can supply.

**Grid (Task 8 N2).** Land opacity 0.25 -> 0.35: at 0.25 the graded land lines were invisible on the plain at both
poses. At 0.35 gameplay03 shows a faint land lattice and diorama02 a faint near-field one, as in the promos. Fade
thresholds unchanged.

**Web.** `BUILD Succeeded WebGL -> Builds/Web-disabled: 77.1 MB on disk, 0 errors`; `?golden&smoke=5` PASS with all
three values from docs/determinism.md. Boot (headed, `machine_lock --exclusive`, median of 3): `--cpu 1` map_build 456 /
first_frame 3585 ms (budgets 743 / 5321); `--cpu 4` 1863 / 6952 ms (budgets 2720 / 10445). Both within.

### Merge point fix round 1 (merge review)
- **I1.** `TheGradeLiftsSaturation` is replaced by `TheGradeKeepsSaturationNearTheUngradedFrame`: graded/ungraded
  saturation at gameplay03 in [0.98, 1.12], and `MapGrade.Saturation` in [-10, 4]. Measured x1.042.
- **Palette (art gap a).** `coastal_plain` Lit(0.52, 0.54, 0.33), `valley` Lit(0.50, 0.53, 0.33). WebGL:

| Shot | Saturation | Contrast | Warmth |
|---|---|---|---|
| promo 02 / promo 03 | 0.425 / 0.382 | 0.200 / 0.203 | 0.145 / 0.064 |
| diorama02, t-merge -> t-merge-r3 | 0.4729 -> 0.4694 | 0.2074 -> 0.2034 | 0.1099 -> 0.1045 |
| gameplay03, t-merge -> t-merge-r3 | 0.5822 -> 0.5646 | 0.2168 -> 0.1959 | 0.1078 -> 0.0774 |

  EditMode 436/436 with no threshold change: selection vs land 33.4 / 35.5 (gameplay03), colour-blind selection-land
  34.5 / 36.4, rift lines 0.121 / 0.104 / 0.026.
- **Border land colour (art gap b): not taken.** `BorderLandColour` (0.76, 0.66, 0.47) (t-merge-r2) rendered the
  south-west slab at (0.69, 0.58, 0.37), against (0.56, 0.50, 0.37) before. The terrain next to it renders (0.82, 0.69,
  0.43), so the step shrank. But the skirt is 17% of diorama02, and its saturation rose 0.34 -> 0.47. That added +0.021
  to the frame, which reached 0.488 against the 0.48 bar (warmth 0.129). Reverted. A lower-chroma colour of the same
  luma is the next candidate.
