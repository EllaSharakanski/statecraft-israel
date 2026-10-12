#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Suggest a `dotnet test --filter` for the changed sim files (#353): `uv run scripts/test_filter.py [BASE]`
(BASE defaults to origin/main; the branch is HEAD).

`sim/Statecraft.Sim/FooSystem.cs` maps to the test class `FooSystemTests`; a changed file in `sim/Statecraft.Sim.Tests/`
maps to the test classes it declares. Only classes that exist are named. When nothing maps, it prints the full-suite command.
This is a fast loop only: the full suite is still required before you open the PR."""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = "sim/Statecraft.Sim/"
TESTS = "sim/Statecraft.Sim.Tests/"
FULL = "dotnet test sim/Statecraft.sln -c Release -warnaserror"
CLASS = re.compile(r"\bclass\s+(\w+)")


def test_classes(root: Path) -> dict[str, str]:
    """{class name: file path} for every class declared in the test project."""
    out = {}
    for f in sorted((root / TESTS).glob("*.cs")):
        for name in CLASS.findall(f.read_text(encoding="utf-8")):
            out[name] = f"{TESTS}{f.name}"
    return out


def classes_for(paths: list[str], known: dict[str, str]) -> list[str]:
    wanted = set()
    for p in paths:
        if p.startswith(SRC) and p.endswith(".cs"):
            stem = Path(p).stem
            for cand in (stem + "Tests",):
                if cand in known:
                    wanted.add(cand)
        elif p.startswith(TESTS) and p.endswith(".cs"):
            wanted.update(n for n, f in known.items() if f == p)
    return sorted(wanted)


def command(classes: list[str]) -> str:
    if not classes:
        return FULL
    return "dotnet test sim/Statecraft.sln -c Release --filter \"" + "|".join(f"FullyQualifiedName~{c}" for c in classes) + "\""


def main(argv: list[str]) -> int:
    base = argv[0] if argv else "origin/main"
    r = subprocess.run(["git", "-C", str(ROOT), "diff", "--name-only", f"{base}...HEAD"], capture_output=True, text=True)
    if r.returncode != 0:
        print(f"Cannot diff against {base} (try: git fetch origin).", file=sys.stderr)
        return 2
    classes = classes_for(r.stdout.split(), test_classes(ROOT))
    print(command(classes))
    if classes:
        print(f"# Matched by file name: {', '.join(classes)}. Run the full suite before you open the PR: {FULL}")
    else:
        print("# No sim file with a matching test class changed, so this is the full suite.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
