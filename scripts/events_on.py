#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Which events are in play on a date (#366): `uv run scripts/events_on.py DATE [--root DIR]`.

Lists every event in content/events/*.yaml whose window [from, to) contains DATE (a missing `to` is open) with its
window, anchor and review status. Exit 1 and "no events" when none. Reads YAML only."""
import argparse
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_ANCHOR = "soft"    # the sim's defaults for a missing key (as in event_timeline.py)
DEFAULT_STATUS = "draft"


def events_on(root: Path, date: str) -> list[dict]:
    out = []
    for f in sorted((root / "content/events").glob("*.yaml")):
        e = yaml.safe_load(f.read_text(encoding="utf-8"))
        start, end = str(e["from"]), str(e["to"]) if e.get("to") else None
        if start <= date and (end is None or date < end):
            out.append({"id": e["id"], "from": start, "to": end or "open",
                        "anchor": e.get("anchor", DEFAULT_ANCHOR), "status": e.get("status", DEFAULT_STATUS)})
    return sorted(out, key=lambda r: (r["from"], r["id"]))


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("date", help="YYYY-MM-DD")
    ap.add_argument("--root", type=Path, default=ROOT)
    a = ap.parse_args(argv)
    rows = events_on(a.root, a.date)
    for r in rows:
        print(f"{r['from']}  {r['to']:<10}  {r['anchor']:<4}  {r['status']:<20}  {r['id']}")
    if not rows:
        print("no events")
    return 0 if rows else 1


if __name__ == "__main__":
    sys.exit(main())
