#!/usr/bin/env python3
"""Every script named in the dev-setup table exists (#359): `uv run scripts/oss/dev_setup_commands.py [--root DIR]`.

Reads docs/dev-setup.md, takes each backticked `scripts/...` or `tools/...` path ending in .py or .sh, and checks that
the file is there. One line per missing file (line number and path); exit 1 if any."""
import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PATH = re.compile(r"((?:scripts|tools)/[\w./-]+\.(?:py|sh))")


def missing(root: Path) -> list[tuple[int, str]]:
    doc = root / "docs/dev-setup.md"
    bad = []
    for n, line in enumerate(doc.read_text(encoding="utf-8").splitlines(), 1):
        for span in re.findall(r"`([^`]+)`", line):
            for p in PATH.findall(span):
                if not (root / p).is_file():
                    bad.append((n, p))
    return bad


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--root", type=Path, default=ROOT)
    args = ap.parse_args(argv)
    bad = missing(args.root)
    for n, p in bad:
        print(f"docs/dev-setup.md:{n}: {p} does not exist")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
