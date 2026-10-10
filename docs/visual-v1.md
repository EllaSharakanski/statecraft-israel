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

**Ambient (review M4).** `MapLighting.AmbientProbe` builds the trilight from `AddAmbientLight` plus two
`AddDirectionalLight` lobes. The lobes add constant and linear terms in every direction, so the probe evaluates darker
than its constants, which are inputs, not the evaluated colours. Evaluated values, pinned in `LightingTests`: up
(0.216, 0.255, 0.326), horizon (0.184, 0.179, 0.171), down (0.119, 0.103, 0.080); the constants are sky (0.52, 0.60,
0.72), equator (0.48, 0.46, 0.42), ground (0.26, 0.22, 0.17). The images were judged on the evaluated values, and Track
B's thresholds were derived with them, so a least-squares refit would be a look change for Task 4, not a fix here.

## Checkpoints
| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
| 2 | t02-light | captures/visual/t02-light/*-compare*.png | (owner) Note: at the new 6 km hero zoom the view is filled by the 600-1,800 m placeholder marker cubes, so judge the zoom range, not the close-up art, until the settlement art track replaces them. |
