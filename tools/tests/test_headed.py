"""tools/headed.py: the pure part always; the real headed browser run only with `pytest --headed` (it opens a window)."""
import asyncio
import argparse
from pathlib import Path

import pytest

import headed

BUILD = Path(headed.REPO) / "unity/Builds/Web-disabled"


def test_a_burst_is_move_press_release_per_click_in_order():
    events = headed.burst_events([(10, 20), (30, 40)])
    assert [(e["type"], e["x"], e["y"]) for e in events] == [
        ("mouseMoved", 10, 20), ("mousePressed", 10, 20), ("mouseReleased", 10, 20),
        ("mouseMoved", 30, 40), ("mousePressed", 30, 40), ("mouseReleased", 30, 40)]
    assert all(e["button"] == "left" and e["clickCount"] == 1 for e in events)


@pytest.mark.headed
@pytest.mark.skipif(not BUILD.exists(), reason="needs unity/Builds/Web-disabled")
def test_every_click_in_one_frame_counts_once():
    """Playtest 2, P2-4, in a real browser: two clicks in one frame each toggle Why exactly once."""
    toggles = asyncio.run(headed.navpairs(argparse.Namespace(build=str(BUILD), cpu=1.0)))
    assert toggles == {"Why+Why": 2, "Why+map-under-panel": 1, "Why+option": 1}
