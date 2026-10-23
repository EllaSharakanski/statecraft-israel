Review-only captures for the #41 screen-entry fix (K5): opening a screen from the side rail now puts the keyboard
focus in the screen instead of leaving it on the rail button.

1280x720, Map.unity through `ScreenEntryShots`, an `[Explicit]` EditMode test in the PR, so the stills can be remade.
English and Hebrew. The game state, camera and zoom are the same in each pair: only the keyboard focus moves. The
"before" state is the old path called directly, the focus put back on the rail button that opened the screen, which
is where `ScreenNav.Open` left it.

- `BEFORE-cabinet-entry-*` / `AFTER-cabinet-entry-*`: the Cabinet opened with Enter from its rail button. Before, the
  rail button keeps the focus (and shows its tip); after, the focus is on `seat-prime_minister`, the screen's first
  control. The test run logs the focused element: `nav-cabinet` before, `seat-prime_minister` after.
- `CROP-*`: the same pairs cropped to the changed area and scaled, with a red box around the pixels that differ.

Kept on a branch rather than in the repository, as CONTRIBUTING.md asks.
