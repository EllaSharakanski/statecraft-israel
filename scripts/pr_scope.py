#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""What does my diff touch? (#352): `uv run scripts/pr_scope.py [BASE]` (BASE defaults to origin/main; the branch is HEAD).

Compares committed changes (BASE...HEAD), like the changelog check, and prints one line each for: user-visible paths and the
CHANGELOG, Unity files, and the golden files. It only reports; it never fails the build."""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts" / "oss"))
import changelog_check  # noqa: E402  (the same path rule the changelog job uses)

GOLDEN = "sim/Statecraft.Sim.Tests/golden/"


def scope(paths: list[str]) -> list[str]:
    lines = []
    visible = changelog_check.user_visible(paths)
    if not visible:
        lines.append("User-visible paths: none changed, no CHANGELOG line needed.")
    elif "CHANGELOG.md" in paths:
        lines.append(f"User-visible paths: {len(visible)} changed, CHANGELOG.md is updated.")
    else:
        lines.append(f"User-visible paths: {len(visible)} changed, CHANGELOG.md is NOT updated (add a line under Unreleased).")
    unity = [p for p in paths if p.startswith("unity/")]
    if unity:
        lines.append(f"Unity: {len(unity)} file(s) under unity/ changed. Say in the PR whether Unity EditMode was run; "
                     "if not, a maintainer must run it before merge.")
    else:
        lines.append("Unity: nothing under unity/ changed.")
    golden = [p for p in paths if p.startswith(GOLDEN)]
    if golden:
        lines.append("Goldens: CHANGED (" + ", ".join(Path(p).name for p in golden) + "). Explain why in the PR.")
    else:
        lines.append("Goldens: unchanged.")
    return lines


def main(argv: list[str]) -> int:
    base = argv[0] if argv else "origin/main"
    try:
        paths = changelog_check.changed_paths(base, "HEAD")
    except subprocess.CalledProcessError:
        print(f"Cannot diff against {base} (try: git fetch origin).", file=sys.stderr)
        return 2
    print("\n".join(scope(paths)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
