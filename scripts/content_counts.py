#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Count the content records (#356): `uv run scripts/content_counts.py [--root DIR]`.

One line per kind: records in each content/model.yaml list with how many carry a `from` or `to` window, then the number
of events, offices, people and parties. Reads YAML only and writes nothing."""
import argparse
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
LISTS = ["indicators", "policies", "effects", "situations", "voter_groups", "actors"]
FILES = [("offices", "offices.yaml", "offices"), ("people", "people.yaml", "people"), ("parties", "parties.yaml", "parties")]


def read(path: Path) -> dict:
    return (yaml.safe_load(path.read_text(encoding="utf-8")) or {}) if path.exists() else {}


def counts(root: Path) -> list[tuple[str, int, int | None]]:
    """(kind, total, windowed) per kind; windowed is None for kinds that have no windows."""
    model = read(root / "content/model.yaml")
    rows: list[tuple[str, int, int | None]] = []
    for key in LISTS:
        recs = [r for r in model.get(key) or [] if isinstance(r, dict)]
        rows.append((key, len(recs), sum(1 for r in recs if "from" in r or "to" in r)))
    rows.append(("events", len(list((root / "content/events").glob("*.yaml"))), None))
    for kind, name, key in FILES:
        rows.append((kind, len(read(root / "content" / name).get(key) or []), None))
    return rows


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--root", type=Path, default=ROOT)
    args = ap.parse_args(argv)
    for kind, total, windowed in counts(args.root):
        print(f"{kind}: {total}" + ("" if windowed is None else f" ({windowed} with a window)"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
