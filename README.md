Review-only captures for #41 K6: no scroll bar takes a Tab stop, not only the Cabinet's.

1280x420, Map.unity through `ScrollBarShots`, an `[Explicit]` EditMode test in the PR, so the stills can be remade.
The short window is deliberate: `budget-rows` overflows there (content 394 against a 364 viewport), so its bar is
shown and really in the Tab order. At 1280x720 the bar is hidden and there is nothing to show.

Both frames are the same walk: focus the Budget's rail button, then 33 Tab presses.

- `BEFORE-*`: the slider put back in the Tab order, exactly as it was before this change. The 33rd press lands on
  the `Scroller`. Note how little there is to see: the focus ring on a thin dark bar is nearly invisible, which is
  part of why the stop is no use from the keyboard.
- `AFTER-*`: the same 33 presses reach `budget-reset`, a real control, lit gold.
- `CROP-*`: the same pairs cropped to the changed area and scaled, with a red box around the pixels that differ.

Kept on a branch rather than in the repository, as CONTRIBUTING.md asks.
