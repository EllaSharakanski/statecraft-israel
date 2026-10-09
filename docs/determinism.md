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

## Golden playthrough

The playthrough loads the real `content/` and calls `Simulation.NewGame(content, 1948, Mode.Historical)`. It then
steps with `HistoricalAI.Commands` until `Date >= 1950-01-01`; with the current rules this ends on 1950-02-26 at
turn 45. The final `GameState.Hash()` must equal:

```
eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9
```

The constant is asserted in two places:
- `sim/Statecraft.Sim.Tests/DeterminismTests.cs` (`GoldenHash`), on .NET.
- `unity/Assets/Tests/EditMode/SimSmokeTests.cs` (`GoldenHash`), inside Unity (plan Task 23).

**Any change to this hash is a deliberate rules or content change.** Such a change could be to a rule, a system,
content in `content/`, or the hash format. When it happens, update the constant here, in `DeterminismTests.cs` and in
`SimSmokeTests.cs`, all in the same commit, and say in the commit message why the hash moved. If the hash changes and
you did not mean to change rules or content, treat it as a determinism bug.
