# The GDD loop switch

## Player story

As the maintainer, I want the GDD v0.1 loop built behind one switch, so that the real game and its goldens do not change until the cutover turns it on, and a save never replays under rules it was not played with [S1].

## Rules

- `systems.loop` in eras.yaml is `legacy` or `gdd`, the same in every era; absent means legacy (R-LOOP-001).
- The GDD state (cities, units, the next unit id, completed objectives, the score) is hashed only when it is set, so the legacy hash text is unchanged (R-LOOP-002).
- Until the cutover, `MvpSlice.Overlay` gave the real content with the switch on and monthly turns. The cutover turned the switch on in the real content and retired the overlay with its rule ID.
- A save names its ruleset, `legacy` or `gdd-0.1`; a game refuses a save from another ruleset (R-SAVE-001, decision D-E4 [S2]), and a save from a ruleset it does not know as made by a newer version (R-SAVE-002).
- Before the cutover, balance values the owner had not set lived only in the overlay or in test fixtures [S3]; since it they are labelled design values in `content/` (R-LOOP-005).

## Data touched

- `content/eras.yaml`: `systems.loop` (`gdd` in the real content since the cutover).
- `GameState`: `Cities`, `Units`, `NextId`, `Objectives`, `Score`.
- Save files: schema 2 and the `ruleset` line.
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: the `mvp-slice` line (until the cutover).

## Done when

- `dotnet test sim/Statecraft.sln` passes, and `hash1950`, `hash1952`, `smoke5`, `scenario-cautious` and `scenario-reckless` do not move.
- The WebGL `web_check` with `golden&smoke=5` passes.

## Out of scope

- Any GDD rule: cities, training, sites, objectives and the score are built once the owner rules on the open decisions.
- Turning the switch on for the real game: the cutover PR, Phase 2 PR 1.

## Sources

- [S1] GDD v0.1 impact analysis and phased plan, section 2.1, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S2] The same plan, section 5, decision D-E4, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, Global constraints, docs/superpowers/plans/2026-10-12-gdd-phase-1.md

## Open questions

- None.
