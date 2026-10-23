### Fixed

- Keyboard: the focus is no longer dropped when the only candidates for it sit inside a hidden parent. A control
  inside a closed screen keeps its own `display: flex`, so the rule that decides where the focus may go accepted it,
  the focus then failed to land, and nothing had it. The rule now looks at the whole line up to the panel, so such a
  control is skipped and the fallback takes the focus instead.
