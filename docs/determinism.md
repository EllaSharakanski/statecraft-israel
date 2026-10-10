# Determinism

The simulation must produce the same state, bit for bit, on .NET, Unity Mono and IL2CPP. Saves, replays and the
"Why did this happen?" report depend on this.

## How it is kept

- **One RNG:** SplitMix64 (`Statecraft.Sim.Rng`). Its whole state is one `ulong` in `GameState.RngState`.
- **Reference vectors:** `DeterminismTests.SplitMix64MatchesTheReferenceVectors` checks the first three outputs for
  seeds 0 and 1234567. The expected values come from Sebastiano Vigna's reference `splitmix64.c`
  (https://prng.di.unimi.it/splitmix64.c), compiled and run separately. They were not copied from this implementation.
- **Canonical hash:** `GameState.Hash()` is SHA-256 over a canonical text. Keys are sorted ordinally. Doubles are
  written as IEEE bit patterns, with -0.0 written as 0.0. Culture is invariant.

## Golden playthroughs

`Statecraft.Sim.GoldenRun.Play(content, until)` starts `Simulation.NewGame(content, GoldenRun.Seed, Mode.Historical)`
with `GoldenRun.Seed = 1948` on the real `content/`, then steps with `HistoricalAI.Commands` until `Date >= until`
(or the game ends, or 1000 turns pass). Each run's final `GameState.Hash()` is pinned:

| Run | Ends | Hash |
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `bf6baf3ff8880b06b82ebf66a01157b29754eed117a7c2bdca528b05fa7959a4` |
| to 1952-01-01 (`Hash1952`) | 1952-02-26, turn 53 | `1753fb88dfeef95d8260b8ffe6250e52b04b71ba10babe8a96d07914047257d0` |

Each constant is written in three places that must agree:
- `sim/Statecraft.Sim/GoldenRun.cs`, which Unity code and players compare against;
- `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, which pins the value on .NET and asserts the library constant equals it;
- this file.

`unity/Assets/Tests/EditMode/SimSmokeTests.cs` runs the same playthroughs inside Unity against `GoldenRun`'s constants
(run `tools/build_sim_for_unity.sh` first so Unity has the current DLLs).

Verified on .NET and Unity editor Mono. IL2CPP and WebGL: pending (M3 Task 22 checks both players).

**Any change to a golden hash is a deliberate rules or content change.** Such a change could be to a rule, a system,
content in `content/`, or the hash format. When it happens, update every place above in the same commit, add a row to
the history below, and say in the commit message why the hash moved. If a hash changes and you did not mean to change
rules or content, treat it as a determinism bug.

## Rebaseline history

| Plan task | Run | New hash (first 8) | Why |
|---|---|---|---|
| M3 Task 2 | 1950 | `d46ce5ff` | the Immigration budget line moves to the Prime Minister (the Ministry of Immigration ends on 1951-10-08), which renames a key in the hashed budget |
| M3 Task 3 | 1950 | `3d360deb` | CBS immigration for 1950 adds January-February 1950 arrivals inside the run |
| M3 Task 4 | 1950 | `e2fa3896` | frumkin_commission (window from 1950-01-01) fires inside the run |
| M3 Task 5 | 1950 | `bf6baf3f` | economy calibrated (rules.yaml) and HistoricalAI sends reserves home after the armistices; capital_inflow_per_year 193.8 acts from 1948, so it moves this hash too |
| M3 Task 5 | 1952 | `1753fb88` | new run covering the M3 content range. Task 4 fix round 1 changed 1951 event windows/effects (hula, forex, bonds, clothing rationing, education, reparations), so capital_inflow_per_year was re-fitted from 192 to 193.8 (first fx crisis in H2 1951) |
