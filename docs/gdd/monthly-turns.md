# Monthly turns as data

## Player story

As a player of the MVP, I want every turn to be one month, so that 15 May 1948 to January 1952 plays in about 44 turns (decision D-B1 [S2]; the GDD targets about 48 [S1]).

## Rules

- The turn length on each security rung comes from `turn_lengths` in the rules of the era the turn starts in (R-TURN-001).
- Without the key, the first three rungs have quarter turns, the fourth month turns and the fifth week turns.
- The MVP sets month on every rung. Turns from the start on 15 May 1948 then end on the 15th of each month [S2].
- A turn that reaches an era boundary ends on it, and a turn that would end fewer than 14 days before it is lengthened to it (`SimTime.AdvanceTo`, D-E5, unchanged).

## Data touched

- `content/rules.yaml` and `content/rules/<era>.yaml`: `turn_lengths`. The real content does not set it until the cutover PR.
- `Rules.TurnLengths`, read by `SecuritySystem.TurnLengthFor(c, date, rung)`.

## Done when

- `dotnet test sim/Statecraft.sln --filter TurnLengthsTests` passes.
- The goldens `hash1950`, `hash1952`, `smoke5`, `scenario-cautious` and `scenario-reckless` do not move.

## Out of scope

- Year turns and the `war_mode` switch of issue #81, paused by the plan's §7.2.
- Turning monthly turns on for the real game, which is the cutover PR (the plan's §3).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, decision D-B1, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md

## Open questions

- None.
