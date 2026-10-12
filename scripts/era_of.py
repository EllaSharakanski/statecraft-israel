#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Which era a date is in (#370): `uv run scripts/era_of.py DATE [--root DIR]`.

Prints the era id and its [from, to) range from content/eras.yaml. `to` is the first day after the era, so the last
day of an era is the day before it. Exit 1 and "no era" when the date is outside every era. Reads YAML only."""
import argparse
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]


def era_of(root: Path, date: str) -> tuple[str, str, str] | None:
    eras = yaml.safe_load((root / "content/eras.yaml").read_text(encoding="utf-8")).get("eras") or []
    for e in eras:
        start, end = str(e["from"]), str(e["to"]) if e.get("to") else None
        if start <= date and (end is None or date < end):
            return e["id"], start, end or "open"
    return None


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("date", help="YYYY-MM-DD")
    ap.add_argument("--root", type=Path, default=ROOT)
    a = ap.parse_args(argv)
    hit = era_of(a.root, a.date)
    if hit is None:
        print("no era")
        return 1
    print(f"{hit[0]}  {hit[1]}  {hit[2]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
