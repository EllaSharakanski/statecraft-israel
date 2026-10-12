#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""All office terms of one person (#369): `uv run scripts/person_terms.py PERSON_ID [--root DIR]`.

Prints each term of that person in content/people.yaml (office, from, to; `open` when there is no `to`), oldest first.
Exit 1 and "unknown person" when the id is not there. Reads YAML only."""
import argparse
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]


def terms(root: Path, person: str) -> list[tuple[str, str, str]] | None:
    """(office, from, to) for the person, or None when the id is unknown."""
    people = yaml.safe_load((root / "content/people.yaml").read_text(encoding="utf-8")).get("people") or []
    for p in people:
        if p.get("id") == person:
            rows = [(t["office"], str(t["from"]), str(t["to"]) if t.get("to") else "open") for t in p.get("offices") or []]
            return sorted(rows, key=lambda r: (r[1], r[0]))
    return None


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("person")
    ap.add_argument("--root", type=Path, default=ROOT)
    a = ap.parse_args(argv)
    rows = terms(a.root, a.person)
    if rows is None:
        print("unknown person")
        return 1
    for office, start, end in rows:
        print(f"{start}  {end:<10}  {office}")
    if not rows:
        print("no office terms")
    return 0


if __name__ == "__main__":
    sys.exit(main())
