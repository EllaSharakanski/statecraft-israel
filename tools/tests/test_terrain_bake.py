import json
from pathlib import Path

import numpy as np
import pytest
from PIL import Image

import terrain_bake as tb

ROOT = Path(__file__).resolve().parents[2]


def meta_for(h: int, w: int, dx: float, dy: float) -> dict:
    return {"width": w, "height": h, "width_km": (w - 1) * dx / 1000, "length_km": (h - 1) * dy / 1000,
            "lon0": 34.2, "lon1": 35.9, "lat0": 29.45, "lat1": 33.35}


def test_flat_land_is_open_and_has_no_curvature():
    z = np.full((41, 41), 300.0, dtype=np.float32)
    assert np.allclose(tb.openness(z, 100, 100, 2000), 1.0)
    assert np.allclose(tb.curvature(z, 100, 100), 0.5)


def test_a_pit_sees_less_sky_than_its_rim():
    y, x = np.mgrid[0:65, 0:65]
    z = (100 + 2.0 * ((x - 32) ** 2 + (y - 32) ** 2)).astype(np.float32)   # a bowl: 0 at the centre, ~2 km at the corners
    sky = tb.openness(z, 100, 100, 2000)
    assert sky[32, 32] < 0.9
    assert sky[32, 32] < sky[0, 0]


def test_ridges_are_convex_and_valleys_concave():
    x = np.arange(65)
    z = np.tile(500 + 300 * np.cos(2 * np.pi * x / 32), (65, 1)).astype(np.float32)   # crests at x = 0, 32, 64
    c = tb.curvature(z, 100, 100)
    assert c[32, 32] > 0.55 and c[32, 16] < 0.45


def test_sea_is_border_connected_zero_and_an_inland_zero_pocket_is_land():
    elev = np.full((40, 40), 50.0, dtype=np.float32)
    elev[:, :10] = 0.0                      # the sea along the west border
    elev[19:22, 29:32] = 0.0                # an enclosed 0 m pocket
    water = tb.water_mask(elev)
    assert water[:, :10].all()
    assert not water[19:22, 29:32].any()
    assert not water[:, 10:].any()


def test_flat_water_below_minus_150_is_a_lake_and_a_slope_is_not():
    elev = np.full((40, 40), 300.0, dtype=np.float32)
    elev[5:17, 5:17] = -420.0                                                 # the Dead Sea's surface: flat
    elev[22:34, 5:17] = (-200.0 - 10.0 * np.arange(12))[None, :]              # the rift floor: sloping
    water = tb.water_mask(elev)
    assert water[8:14, 8:14].all()
    assert not water[22:34, 5:17].any()


def test_coast_distance_is_signed_metres_across_a_straight_coast():
    elev = np.full((30, 60), 50.0, dtype=np.float32)
    elev[:, :20] = 0.0                                                        # sea columns 0..19
    water = tb.water_mask(elev)
    to_water = tb.distance_to(water, 100, 100, 12000)
    to_land = tb.distance_to(~water, 100, 100, 12000)
    assert to_water[15, 30] == pytest.approx(1100)                           # 11 columns east of the last sea column
    assert to_land[15, 9] == pytest.approx(1100)                             # 11 columns west of the first land column
    assert to_water[15, 59] == pytest.approx(4000)


def test_distance_uses_each_axis_own_spacing():
    elev = np.full((40, 30), 50.0, dtype=np.float32)
    elev[:20, :] = 0.0                                                        # sea in the north rows (row 0 = north)
    d = tb.distance_to(tb.water_mask(elev), 79.0, 211.0, 12000)
    assert d[25, 15] == pytest.approx(6 * 211.0)


def test_texels_average_four_samples_and_coast_encoding_is_centred():
    a = np.arange(9, dtype=np.float32).reshape(3, 3)
    assert np.array_equal(tb.to_texels(a), np.array([[2, 3], [5, 6]], dtype=np.float32))
    enc = tb.encode_coast(np.array([0.0, -12000.0, 12000.0, -50000.0, 120.0], dtype=np.float32))
    assert enc.tolist() == [128, 0, 255, 0, 129]


def test_main_writes_both_textures_one_texel_smaller_and_deterministically(tmp_path):
    n = 33
    y, x = np.mgrid[0:n, 0:n]
    elev = np.where(x < 8, 0.0, 40.0 + 30.0 * np.sin(y / 3.0) + 20.0 * x).astype(np.float32)
    lo, hi = float(elev.min()), float(elev.max())
    map_dir = tmp_path / "map"; map_dir.mkdir()
    np.round((elev - lo) / (hi - lo) * 65535).astype("<u2").tofile(map_dir / "heightmap.r16")
    (map_dir / "heightmap.json").write_text(json.dumps({**meta_for(n, n, 79.0, 211.0), "elev_min_m": lo, "elev_max_m": hi}))
    tb.main(map_dir, tmp_path / "a"); tb.main(map_dir, tmp_path / "b")
    bake = Image.open(tmp_path / "a" / "terrain_bake.png"); coast = Image.open(tmp_path / "a" / "coast.png")
    assert (bake.mode, bake.size) == ("RGB", (n - 1, n - 1))
    assert (coast.mode, coast.size) == ("L", (n - 1, n - 1))
    for name in ("terrain_bake.png", "coast.png"):
        assert (tmp_path / "a" / name).read_bytes() == (tmp_path / "b" / name).read_bytes()
    c = np.asarray(coast)
    assert c[:, :5].max() < 128 and c[:, 12:].min() > 128                   # water west, land east


def texel(lon: float, lat: float) -> tuple[int, int]:
    meta = json.loads((ROOT / "unity/Assets/StreamingAssets/map/heightmap.json").read_text())
    col = int((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * 2048)
    row = int((meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * 2048)
    return row, col


def test_the_committed_coast_texture_marks_the_seas_and_lakes():
    c = np.asarray(Image.open(ROOT / "unity/Assets/Textures/Map/coast.png"))
    assert c.shape == (2048, 2048)
    assert c[texel(34.40, 32.00)] < 64, "the Mediterranean off Tel Aviv, well offshore"
    assert c[texel(35.59, 32.82)] < 128, "the Sea of Galilee"
    assert c[texel(35.50, 31.50)] < 128, "the Dead Sea"
    assert c[texel(34.95, 29.50)] < 128, "the Gulf of Aqaba off Eilat"
    assert c[texel(35.21, 31.78)] > 192, "Jerusalem, far inland"
    b = np.asarray(Image.open(ROOT / "unity/Assets/Textures/Map/terrain_bake.png"))
    assert b.shape == (2048, 2048, 3)
