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
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9` |

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
