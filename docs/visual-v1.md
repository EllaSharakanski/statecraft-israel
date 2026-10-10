# Visual v1: measurements, budgets and checkpoints

Plan: `docs/superpowers/plans/2026-10-11-statecraft-visual-v1.md`. Promo targets: `promo/final/03-gameplay-1936.png`,
`promo/final/02-map-diorama.png`. Poses: `PromoPose.Gameplay03` (34.86 E, 32.00 N, 60 km, yaw 10) and
`PromoPose.Diorama02` (35.05 E, 31.55 N, 250 km, yaw 0).

## How to measure
- Frame time: `?vprofile` (WebGL) or `-vprofile` (desktop player, quits when done), always under
  `uv run tools/machine_lock.py --exclusive --wait-unity -- ...` (no other Unity job on the machine). Per pose, every
  configuration (`all`, each toggle off alone as `-<name>`, `none`) runs 3 times in interleaved orders, 300 frames after
  60 warm-up frames; Chrome runs with `--disable-frame-rate-limit --disable-gpu-vsync --force-device-scale-factor=1` (headless through
  `web_check.py`, headed through `headed.py profile`), so times are not capped at the display rate and headed and headless
  runs draw the same pixels. Every line states `res=` and
  the render `scale=`. Read the `PROFILE summary` lines: a feature costs `median(all) - median(-name)`.
- Boot: `uv run tools/headed.py boot --runs 3 [--baseline MS]` (three cold loads in a headed window; the map-build span is
  heightmap decoded to map built; `--cpu 4` adds CPU throttling as a cheap low-end proxy).
- Headed frame time: `uv run tools/headed.py profile --dpr 1` (`--dpr 2` is the fill-rate stress proxy). `--wait-unity`
  also waits for an interactive Unity editor that has any worktree open (it holds `unity/Temp/UnityLockfile`): close it,
  or the profile waits.
- Screenshots: `tools/art_checkpoint.sh [--no-build] <name>` (WebGL `?shot` + editor renders of both tiers + side-by-side
  images and their deuteranopia/protanopia copies under `captures/visual/<name>/`, git-ignored; rows in the main
  checkout's `captures/visual/art-log.tsv`).

## Budgets (proposed; ratified in Task 10)
Measured on the development Mac (Apple silicon, integrated GPU), 1920x1080 at DPR 1, Chrome for Testing:
- headless, all features on: median p95 <= 16.7 ms at both poses, and median(all) - median(none) <= 8 ms;
- each feature: median(all) - median(-feature) <= 2.5 ms (tilt-shift <= 1.5 ms, labels <= 1 ms); a cost counts as over only
  when it exceeds the budget plus the larger of the two spreads;
- headed numbers are recorded, plus a DPR-2 headed run in Task 10 as a fill-rate stress proxy for weaker GPUs; the 30 fps
  floor for an integrated-GPU laptop (research notes 2.3) is 33 ms p95;
- boot: the map-build span grows by at most 1,000 ms over the Task 1 baseline (median of 3).

## Baseline (Task 1, before any visual change)
| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.42 spread_ms=0.68 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.39 spread_ms=0.86 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=5.32 spread_ms=0.02 median_p95_ms=7.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=5.33 spread_ms=0.09 median_p95_ms=7.00`<br>run line: `PROFILE visual=all pose=gameplay03 res=1920x937 scale=0.80 rep=0 frames=300 avg_ms=1.73 p95_ms=2.00` |
| headed (DPR 1) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.45 spread_ms=1.16 median_p95_ms=2.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.46 spread_ms=0.33 median_p95_ms=2.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=4.58 spread_ms=0.04 median_p95_ms=6.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=4.58 spread_ms=0.01 median_p95_ms=7.00`<br>run line: `PROFILE visual=all pose=gameplay03 res=1728x860 scale=0.80 rep=0 frames=300 avg_ms=1.45 p95_ms=2.00` |
| boot | `BOOT map_build_ms median=450 runs=3` |

Notes on the baseline (Task 1, commit base `7e610aa`):
- **Resolution.** Neither run drew 1920x1080. Headless (`--headless=new --window-size=1920,1080`) reports `res=1920x937`:
  the window size is the outer window, and the template's full-viewport canvas loses 143 px to the emulated browser UI.
  The headed window was clamped by the laptop screen to `res=1728x860`. Compare like with like (same tool, same `res=`)
  until this is settled.
- **Headed stall.** The first headed attempt stopped producing frames after 4 of 12 runs (presumably Chrome throttling
  a hidden or occluded window) and timed out at 1,800 s; an unchanged retry (`--timeout 300`) completed. Keep the
  headed window visible while profiling.
- No toggles exist yet, so `all` and `none` are the same scene: their difference (and the spreads) is the noise floor.

## Checkpoints
| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
