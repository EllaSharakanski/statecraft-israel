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


def test_boot_phases_and_the_map_build_span():
    lines = ["BOOT phase=player started at=900 ms dt=900 ms", "BOOT phase=heightmap decoded at=5200 ms dt=1300 ms",
             "GAME OK 1948-05-15 turn 0 hash 00", "BOOT phase=map built at=9400 ms dt=300 ms"]
    phases = headed.boot_phases(lines)
    assert phases[0] == ("player started", 900, 900)
    assert headed.map_build_ms(phases) == 9400 - 5200 + 1300
    with pytest.raises(ValueError, match="map built"):
        headed.map_build_ms(phases[:2])


def test_the_boot_budget_compares_the_median_with_the_baseline():
    assert headed.over_budget([5500, 5700, 5900], 4800, 1000) == (5700, False)   # 5700 <= 5800
    assert headed.over_budget([5500, 5700, 5900], 4600, 1000) == (5700, True)    # 5700 > 5600
    assert headed.over_budget([5500], None, 1000) == (5500, False)


def test_profile_windows_are_uncapped_at_the_requested_dpr():
    plain = headed.browser_flags("/tmp/p", 1568, 752)
    assert "--use-angle=metal" in plain and "--window-size=1568,752" in plain and plain[-1] == "about:blank"
    assert not any("scale-factor" in f or "frame-rate" in f for f in plain), "navpairs and boot keep their old flags"
    prof = headed.browser_flags("/tmp/p", 1920, 1080, dpr=2, uncapped=True)
    for flag in ("--force-device-scale-factor=2", "--disable-frame-rate-limit", "--disable-gpu-vsync", "--window-size=1920,1080"):
        assert flag in prof
    assert prof[-1] == "about:blank"
