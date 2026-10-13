# Rules of the GDD v0.1 MVP

The rules of the 1948–1951 MVP in [docs/GDD.md](../GDD.md), as Phase 1 builds them behind the `systems.loop` switch
([the approved plan](../superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md), §6). Balance values are the owner's.
Until the owner sets them, they are placeholders in the dev and test overlay (`MvpSlice.Overlay`) or in test fixtures, never in `content/`.

## Turns

R-TURN-001: The turn length on each security rung (1 to 5) is read from the rules key `turn_lengths` of the era the turn starts in. Without the key it is quarter, quarter, quarter, month, week, today's table. The GDD's monthly turns set all five to month.

## The loop switch

R-LOOP-001: `systems.loop` in eras.yaml is `legacy` (the default) or `gdd`, and every era has the same value. Its ruleset id, which a save records, is `legacy` or `gdd-0.1`.

R-LOOP-003: `MvpSlice.Overlay` loads the real content with `systems.loop: gdd` in every era and `turn_lengths` all month, and changes nothing else. Tests, the MVP goldens and the dev flag use it until cutover.

R-LOOP-002: The GDD state (cities, units, the next unit id, completed objectives, the score) is written into the state hash only when it is set. With the legacy loop the hash text is unchanged, byte for byte.

## Saves

R-SAVE-001: A save names the ruleset it was played under (`ruleset legacy` or `ruleset gdd-0.1`, save schema 2). A game refuses a save made under another ruleset, with a message to start a new game. A schema 1 save has no ruleset line and is legacy.
