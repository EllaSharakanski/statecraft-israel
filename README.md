Review-only captures for PR #1453 (accessibility, #41, the keyboard-only turn's "Still open" items 1 and 2).

1280x720, Map.unity through `A11yKeyboardShots` (an `[Explicit]` EditMode test in the PR, so the stills can be
remade), English and Hebrew. The game state, camera and zoom are the same in each before/after pair: only the
keyboard focus moves.

- `BEFORE-item1-*` / `AFTER-item1-*`: the Tab after the last Cabinet seat. Before, the seat list's own scroll bar
  takes the stop; after, the focus carries on to the HUD's Cabinet button. The test run logs the focused element:
  `Scroller` before, `nav-cabinet` after.
- `BEFORE-item2-*` / `AFTER-item2-*`: the Assign stepper's press that reaches 0, which disables the minus. Before,
  nothing holds the focus; after, it is on the stepper's plus. Logged: `none` before, `city-workers-more` after.
- `CROP-*`: the same pairs cropped to the control and scaled, with a red box around the pixels that differ.

Kept on a branch rather than in the repository, as CONTRIBUTING.md asks.
