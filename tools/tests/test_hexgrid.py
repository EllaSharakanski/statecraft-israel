import json

import numpy as np
import pytest

import hexgrid as hg


def test_hex_center_round_trips_through_point_to_hex():
    for q, r in [(0, 0), (3, 5), (-2, 7), (10, 40)]:
        x, y = hg.hex_center_km(q, r, 5.0)
        assert hg.point_to_hex(np.array([x]), np.array([y]), 5.0) == ([q], [r])


def test_neighbouring_centres_are_one_hex_width_apart():
    x0, y0 = hg.hex_center_km(0, 0, 5.0)
    x1, y1 = hg.hex_center_km(1, 0, 5.0)
    assert x1 - x0 == pytest.approx(5.0 * 3 ** 0.5) and y1 == y0


@pytest.mark.parametrize("stats, lat, lon, biome", [
    ({"sea_share": 0.6, "median": 0.0, "water_share": 0.0}, 32.0, 34.7, "sea"),
    ({"sea_share": 0.0, "water_share": 0.7, "median": -212.0}, 32.8, 35.6, "lake"),
    ({"sea_share": 0.0, "median": -250.0, "water_share": 0.0}, 32.3, 35.55, "valley"),
    ({"sea_share": 0.0, "median": 780.0, "water_share": 0.0}, 31.78, 35.22, "mountains"),
    ({"sea_share": 0.0, "median": 290.0, "water_share": 0.0}, 31.25 - 0.01, 34.79, "desert"),
    ({"sea_share": 0.0, "median": 300.0, "water_share": 0.0}, 31.6, 35.35, "desert"),
    ({"sea_share": 0.0, "median": 400.0, "water_share": 0.0}, 32.7, 35.3, "hills"),
    ({"sea_share": 0.1, "median": 25.0, "water_share": 0.0}, 32.08, 34.8, "coastal_plain"),
    # Dead Sea basin and northern Arava (below 0 m, south of the Dead Sea's north tip 31.77) are hyper-arid desert
    ({"sea_share": 0.0, "median": -380.0, "water_share": 0.1}, 31.5, 35.45, "desert"),
    ({"sea_share": 0.0, "median": -50.0, "water_share": 0.0}, 31.76, 35.5, "desert"),
    ({"sea_share": 0.0, "median": -300.0, "water_share": 0.0}, 31.78, 35.5, "valley"),
    ({"sea_share": 0.0, "water_share": 0.8, "median": -430.0}, 31.5, 35.5, "lake"),  # lake still wins
])
def test_biome_rules(stats, lat, lon, biome):
    assert hg.classify(stats, lat, lon) == biome


def test_water_share_counts_flat_water_below_minus_150():
    v = np.r_[np.full(60, -430.0), np.linspace(-400, 200, 40)]   # lake surface + cliffs
    assert hg.water_share(v) == pytest.approx(0.6, abs=0.02)
    assert hg.water_share(np.full(10, 300.0)) == 0.0


def test_build_hexes_on_synthetic_map(tmp_path):
    n = 201
    elev = np.zeros((n, n), dtype="float32")
    elev[:, n // 2:] = 300.0                       # west half sea, east half land at 300 m
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0}
    (tmp_path / "heightmap.json").write_text(json.dumps(meta))
    np.round(elev / 300.0 * 65535).astype("<u2").tofile(tmp_path / "heightmap.r16")
    out = hg.build(tmp_path, 5.0)
    assert out["layout"] == "pointy" and out["hex_size_km"] == 5.0
    assert set(out["hexes"][0]) == {"q", "r", "lon", "lat", "elev_m", "land", "biome"}
    west = [h for h in out["hexes"] if h["lon"] < 35.2]
    east = [h for h in out["hexes"] if h["lon"] > 35.3]
    assert west and all(h["biome"] == "sea" and not h["land"] for h in west)
    assert east and all(h["biome"] == "hills" and h["land"] for h in east)
    assert len({(h["q"], h["r"]) for h in out["hexes"]}) == len(out["hexes"])
