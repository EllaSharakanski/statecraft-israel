#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# ///
"""Write a valid skeleton for a new event (#323): `uv run scripts/new_event.py <id> [--from YYYY-MM-DD] [--to YYYY-MM-DD]`.

The file has the schema line, every required field of content/schema/event.schema.json and an empty `sources` list the
writer must fill (docs/CONTENT_POLICY.md). It refuses an existing file and an id that is not lower_snake_case. It never
overwrites and writes nothing else."""
import argparse
import re
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

TEMPLATE = """# yaml-language-server: $schema=../schema/event.schema.json
id: @ID@
title: TODO title
text: TODO what the player reads. Say who must decide, e.g. {defense_minister}.
anchor: soft
# tags: [security]   # holocaust, terror, massacre and war-casualties are sensitive (docs/CONTENT_POLICY.md)
# office: prime_minister
# Say in a comment what happened and when, with the source.
from: @FROM@
@TO@options:
  - id: TODO_option
    text: TODO what the player does
    effects:
      - { target: morale, delta: 0.0 }
sources: []   # each { url: "https://...", checked: YYYY-MM-DD }
status: draft
"""


def build(event_id: str, start: str, end: str | None) -> str:
    return TEMPLATE.replace("@ID@", event_id).replace("@FROM@", start).replace("@TO@", f"to: {end}\n" if end else "")


def main(argv: list[str] | None = None, root: Path = ROOT) -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    p.add_argument("id")
    p.add_argument("--from", dest="start", default="1948-05-15")
    p.add_argument("--to", dest="end")
    a = p.parse_args(argv)
    if not re.fullmatch(r"[a-z][a-z0-9_]*", a.id):
        print(f"bad id {a.id!r}: use lower_snake_case", file=sys.stderr)
        return 2
    for d in filter(None, (a.start, a.end)):
        try:
            date.fromisoformat(d)
        except ValueError:
            print(f"bad date {d!r}: use YYYY-MM-DD", file=sys.stderr)
            return 2
    if a.end and a.end <= a.start:
        print("--to must be after --from", file=sys.stderr)
        return 2
    path = root / "content" / "events" / f"{a.id}.yaml"
    if path.exists():
        print(f"{path} exists; not overwriting", file=sys.stderr)
        return 1
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(build(a.id, a.start, a.end), encoding="utf-8")
    print(f"wrote {path.relative_to(root)}; fill the TODOs and sources, then run scripts/check.sh")
    return 0


if __name__ == "__main__":
    sys.exit(main())
