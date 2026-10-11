"""Checks the committed 1948 routes (content/map/routes_1948.yaml and the traced route_paths_1948.yaml) and the pure parts
of tools/routes_1948.py. The trace itself needs the map tiles (network) and scikit-image, so it runs only where both are
present; CI checks the committed result."""
import json
import math
from pathlib import Path

import numpy as np
import pytest
import yaml

import routes_1948 as rt

ROOT = Path(__file__).resolve().parents[2]
BOUNDS = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())["bounds"]
ROUTES = yaml.safe_load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))
PATHS = yaml.safe_load((ROOT / "content" / "map" / "route_paths_1948.yaml").read_text(encoding="utf-8"))["routes"]
BY_ID = {r["id"]: r for r in PATHS}


def test_routes_file_is_valid():
    rt.validate_routes(ROUTES, BOUNDS)
    assert ROUTES["map_source"]["licence"] == "Public-Domain" and ROUTES["map_source"]["checked"]


def test_the_issue_routes_are_there():
    """Issue #35 names the Jaffa-Jerusalem line, the coastal main line and the 1948 roads to Jerusalem."""
    ids = {r["id"] for r in ROUTES["routes"]}
    assert {"jaffa_jerusalem_railway", "kantara_main_line_south", "eastern_railway", "hadera_haifa_railway",
            "jaffa_jerusalem_road", "burma_road", "tel_aviv_haifa_road", "coastal_road_south"} <= ids


def test_paths_match_the_routes_file():
    assert [r["id"] for r in PATHS] == [r["id"] for r in ROUTES["routes"]]
    for src in ROUTES["routes"]:
        p = BY_ID[src["id"]]
        assert (p["kind"], p["status"]) == (src["kind"], src["status"]), src["id"]


@pytest.mark.parametrize("route", ROUTES["routes"], ids=lambda r: r["id"])
def test_each_path_runs_from_its_first_to_its_last_waypoint(route):
    pts = [tuple(p) for p in BY_ID[route["id"]]["points"]]
    assert len(pts) >= 2
    for lon, lat in pts:
        assert BOUNDS["lon0"] <= lon <= BOUNDS["lon1"] and BOUNDS["lat0"] <= lat <= BOUNDS["lat1"]
    first, last = route["via"][0], route["via"][-1]
    # A waypoint snaps to its symbol within SNAP_M (plus the spur cut at the ends).
    assert rt.km_between(pts[0], (first["lon"], first["lat"])) <= rt.SNAP_M / 1000 + 0.3, route["id"]
    assert rt.km_between(pts[-1], (last["lon"], last["lat"])) <= rt.SNAP_M / 1000 + 0.3, route["id"]
    # No wild detours: the line is at least the waypoints' chord and at most twice it (hill roads wind).
    chord = sum(rt.km_between((a["lon"], a["lat"]), (b["lon"], b["lat"])) for a, b in zip(route["via"], route["via"][1:]))
    length = rt.length_km(pts)
    assert 0.85 * chord <= length <= 2.0 * chord, (route["id"], chord, length)
    assert abs(BY_ID[route["id"]]["length_km"] - length) < 0.06


@pytest.mark.parametrize("route", ROUTES["routes"], ids=lambda r: r["id"])
def test_each_path_passes_near_every_waypoint(route):
    """Every waypoint is near the line (a trace that skipped one would be far from it). Up to 3.5 km: a waypoint at a
    modern town's point off the 1946 road (Kfar Yona, Ashkelon) is reached by a spur, which the tool cuts."""
    pts = np.asarray(BY_ID[route["id"]]["points"], float)
    for w in route["via"]:
        d = min(rt.km_between((w["lon"], w["lat"]), tuple(p)) for p in pts)
        assert d <= 3.5, (route["id"], w["name"], d)


def test_no_segment_jumps():
    """Consecutive points are at most 8 km apart: a longer straight jump would be a trace that left its symbol. (The
    longest real straight is 6.5 km, on the Tel Aviv-Haifa road across the Sharon plain.)"""
    for r in PATHS:
        pts = [tuple(p) for p in r["points"]]
        worst = max(rt.km_between(a, b) for a, b in zip(pts, pts[1:]))
        assert worst < 8.0, (r["id"], worst)


def test_the_jaffa_jerusalem_railway_is_its_real_length():
    """Wikipedia: 86.6 km (Jaffa to Jerusalem). The trace starts at the Jaffa terminus and ends at the Jerusalem
    station, so it must be close (within 10%)."""
    assert 78.0 <= rt.length_km([tuple(p) for p in BY_ID["jaffa_jerusalem_railway"]["points"]]) <= 95.0


def test_mercator_round_trip():
    for lon, lat in [(34.2, 29.45), (35.9, 33.35), (34.78, 32.08)]:
        x, y = rt.lonlat_to_px(lon, lat)
        lo, la = rt.px_to_lonlat(x, y)
        assert abs(lo - lon) < 1e-9 and abs(la - lat) < 1e-9
    assert 15.0 < rt.metres_per_px(32.0) < 17.0          # zoom 13: about 16 m a pixel at 32 N


def test_simplify_keeps_ends_and_tolerance():
    line = [(float(x), 0.0 + (0.3 if x == 5 else 0.0)) for x in range(11)]
    assert rt.simplify(line, 1.0) == [(0.0, 0.0), (10.0, 0.0)]
    bent = [(0.0, 0.0), (5.0, 3.0), (10.0, 0.0)]
    assert rt.simplify(bent, 1.0) == bent


def test_smooth_keeps_the_ends():
    pts = [(0.0, 0.0), (1.0, 1.0), (2.0, 0.0), (3.0, 1.0), (4.0, 0.0)]
    out = rt.smooth(pts, 3)
    assert out[0] == pts[0] and out[-1] == pts[-1] and len(out) == len(pts)
    assert out[2] == pytest.approx((2.0, 2.0 / 3.0))


def test_remove_spurs_drops_an_out_and_back():
    line = [(x, 0) for x in range(30)]
    spur = [(15, y) for y in range(1, 20)] + [(15, y) for y in range(18, 0, -1)]
    chain = line[:16] + spur + line[15:]
    out = rt.remove_spurs(chain, 2.0)
    assert all(y == 0 for _, y in out) and out[0] == (0, 0) and out[-1] == (29, 0)
    assert rt.remove_spurs(line, 2.0) == line


def test_symbol_masks_on_the_map_colours():
    def px(*rgb):
        return np.array([[rgb]], np.uint8)
    road, orange, cream, ink = px(205, 45, 50), px(235, 155, 70), px(252, 232, 182), px(30, 28, 25)
    assert rt.symbol_mask(road, "road")[0, 0]
    assert not rt.symbol_mask(orange, "road")[0, 0]          # the hill tint is not a road
    assert not rt.symbol_mask(cream, "road")[0, 0]
    assert rt.symbol_mask(ink, "track")[0, 0]
    # rail: a 6 px dark stroke survives the opening, a 2 px one (a cart track, a road's casing) does not
    img = np.full((40, 60, 3), 250, np.uint8)
    img[10:16, :] = 25
    img[30:32, :] = 25
    m = rt.symbol_mask(img, "rail")
    assert m[12, 30] and not m[31, 30]


def test_a_trace_follows_a_drawn_road():
    pytest.importorskip("skimage")
    img = np.full((120, 200, 3), (250, 230, 180), np.uint8)
    rr = (60 + 30 * np.sin(np.linspace(0, 3, 200))).astype(int)
    for c, r in enumerate(rr):
        img[r - 2:r + 3, c] = (205, 45, 50)
    cost = rt.cost_raster(img, "road")
    path = rt.least_cost_path(cost, (rr[0], 0), (rr[-1], 199))
    assert max(abs(r - rr[c]) for r, c in path) <= 3


def test_trim_end_hooks_drops_a_hook_at_either_end():
    hook = [(0, 0), (-3, 0), (-3, 1), (0, 1)]
    line = [(x, 1) for x in range(1, 30)]
    out = rt.trim_end_hooks(hook + line, 4.0)
    assert out[0][0] >= 0 and out[-1] == (29, 1)
    assert rt.trim_end_hooks(line, 4.0) == line, "a line that only moves away from its ends is kept whole"
    back = rt.trim_end_hooks((hook + line)[::-1], 4.0)
    assert back[0] == (29, 1) and back[-1] == (0, 1), "the same hook at the end"
