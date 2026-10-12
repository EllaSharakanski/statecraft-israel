#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Who held an office on a date (#365): `uv run scripts/who_held.py OFFICE DATE [--root DIR]`.

Prints each person in content/people.yaml whose term for OFFICE covers DATE, with the term's from and to. Terms are
[from, to): the day a term ends is not covered; a term with no `to` runs on. Exit 1 and "nobody" when no one held it."""
import argparse
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]


def holders(root: Path, office: str, date: str) -> list[tuple[str, str, str]]:
    """(person id, from, to) for every term of `office` covering `date`; to is "open" when the term has none."""
    people = yaml.safe_load((root / "content/people.yaml").read_text(encoding="utf-8")).get("people") or []
    out = []
    for p in people:
        for t in p.get("offices") or []:
            if t.get("office") != office:
                continue
            start, end = str(t["from"]), str(t["to"]) if t.get("to") else None
            if start <= date and (end is None or date < end):
                out.append((p["id"], start, end or "open"))
    return sorted(out, key=lambda r: (r[1], r[0]))


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("office")
    ap.add_argument("date", help="YYYY-MM-DD")
    ap.add_argument("--root", type=Path, default=ROOT)
    a = ap.parse_args(argv)
    rows = holders(a.root, a.office, a.date)
    for pid, start, end in rows:
        print(f"{pid}  {start}  {end}")
    if not rows:
        print("nobody")
    return 0 if rows else 1


if __name__ == "__main__":
    sys.exit(main())
