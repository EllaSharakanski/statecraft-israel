#!/usr/bin/env python3
"""Markdown link check (#333): every relative link in a tracked *.md file must point at a file that is tracked in git.

`uv run scripts/oss/md_links.py [--root DIR]`. Ignores code fences, absolute URLs (any `scheme:`), `#anchor` and
`mailto:` links, and targets with brackets (reference-style leftovers). Anchors after `#` are not checked, only the file.
Handles a link title (`[a](b "t")`), `<angle>` targets, `%20` escapes and the outer link of a badge (`[![x](img)](page)`).
A target must match a tracked file or directory exactly, so a wrong letter case fails on macOS too (CI's Linux would).
One line per dead link (file: link); exit 1 if any."""
import argparse
import os
import re
import subprocess
import sys
from urllib.parse import unquote
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LINK = re.compile(r"\]\(<?([^)\s>]+)>?(?:\s+(?:\"[^\"]*\"|'[^']*'))?\)")
FENCE = re.compile(r"```.*?```", re.S)


def tracked_markdown(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z", "*.md"], capture_output=True, text=True, check=True).stdout
    return [f for f in out.split("\0") if f]


def tracked_paths(root: Path) -> set[str]:
    """Every tracked file and every directory above one, with the exact letter case git stores."""
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, text=True, check=True).stdout
    paths = set()
    for f in filter(None, out.split("\0")):
        parts = f.split("/")
        paths.update("/".join(parts[:i]) for i in range(1, len(parts) + 1))
    return paths


def dead_links(root: Path) -> list[tuple[str, str]]:
    bad = []
    known = tracked_paths(root)
    for f in tracked_markdown(root):
        text = FENCE.sub("", (root / f).read_text(encoding="utf-8"))
        for m in LINK.finditer(text):
            target = m.group(1)
            if re.match(r"[A-Za-z][A-Za-z0-9+.-]*:|#", target) or "[" in target:
                continue
            path = unquote(target.split("#")[0].split("?")[0])
            if path and os.path.normpath(os.path.join(os.path.dirname(f), path)).replace(os.sep, "/").rstrip("/") not in known:
                bad.append((f, target))
    return bad


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    p.add_argument("--root", type=Path, default=ROOT)
    bad = dead_links(p.parse_args(argv).root)
    for f, t in bad:
        print(f"{f}: {t}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
