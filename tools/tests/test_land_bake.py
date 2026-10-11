import math

import numpy as np
import pytest
from PIL import Image

import land_bake as lb

META = {"width_km": 161.53, "length_km": 431.22}
SX, SY = lb.TRACKS_W / META["width_km"], lb.TRACKS_H / META["length_km"]   # track texels per km


def test_offsets_are_affine_across_a_straight_track_so_filtering_keeps_its_line():
    # A north-south track at x = 50.0 km, sampled every 40 m like track_points.
    pts = [(50.0, y) for y in np.arange(100.0, 120.0, 0.04)]
    off, valid = lb.track_offsets(pts, META)
    row = int(110.0 * SY)
    cols = [c for c in range(int(48.5 * SX), int(51.5 * SX)) if valid[row, c]]
    for c in cols:   # east offset = (track x - texel x) in metres: one straight line through the texels on both sides
        assert abs(off[row, c, 0] - (50.0 - (c + 0.5) / SX) * 1000) < 1e-6
    left = max(c for c in cols if (c + 0.5) / SX < 50.0)
    mid_x = ((left + 0.5) + (left + 1.5)) / 2 / SX
    assert abs((off[row, left, 0] + off[row, left + 1, 0]) / 2 - (50.0 - mid_x) * 1000) < 1e-6


def bilinear_drawn(enc: np.ndarray, use_b: bool, scale: int = 4) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """What the shader draws (track coverage > 0.5 at a 30 m footprint) at `scale` x the texture's resolution, filtering
    the encoded texture bilinearly as the GPU does (no mips): (x km, y km south, drawn) of every sample."""
    h, w = enc.shape[:2]
    e = enc.astype(np.float64) / 255.0
    ys = (np.arange(h * scale) + 0.5) / scale - 0.5
    xs = (np.arange(w * scale) + 0.5) / scale - 0.5
    Y, X = np.meshgrid(ys, xs, indexing="ij")
    ch = [lb.bilinear(e[..., k], Y, X) for k in range(3)]
    d = np.hypot(ch[0] * 255 / 127 - 1, ch[1] * 255 / 127 - 1) * lb.TRACK_RANGE_M
    cov = np.clip((55.0 - d) / 30.0 + 0.5, 0, 1)
    if use_b:
        t = np.clip((ch[2] - 0.9) / 0.1, 0, 1)
        cov = cov * t * t * (3 - 2 * t)
    return (X + 0.5) / SX, (Y + 0.5) / SY, cov > 0.5


def test_no_phantom_track_appears_next_to_texels_without_a_track():
    # Review I1: one diagonal SW-NE track. Filtering its offsets with the "no track" texels (255, 255 = +1008, +1008 m)
    # passes through zero about 1 km south-east of it: drawn without the B mask, faded with it.
    pts = [(40.0 + t, 140.0 - t) for t in np.arange(0.0, 12.0, 0.04)]   # x east, y south: going north-east
    off, valid = lb.track_offsets(pts, META)
    r0, r1, c0, c1 = int(136 * SY), int(142 * SY), int(38 * SX), int(54 * SX)
    enc = lb.encode_offsets(off, valid)[r0:r1, c0:c1]

    def far_drawn(use_b: bool) -> int:
        x, y, drawn = bilinear_drawn(enc, use_b)
        x, y = x + c0 / SX, y + r0 / SY
        dist = np.abs((x - 40.0) + (y - 140.0)) / math.sqrt(2) * 1000   # metres from the line x + y = 180
        inside = (x > 41.0) & (x < 51.0)                                # away from the track's two ends
        return int(np.sum(drawn & inside & (dist > 300.0)))
    assert far_drawn(use_b=False) > 0, "the old encoding drew a phantom (reproduced)"
    assert far_drawn(use_b=True) == 0
    x, y, drawn = bilinear_drawn(enc, use_b=True)
    assert drawn.sum() > 0, "the real track still draws"


def test_medial_axes_between_two_tracks_draw_no_phantom():
    # Two parallel tracks 1.6 km apart: halfway between them nothing may draw.
    pts = [(x, y) for y in np.arange(100.0, 120.0, 0.04) for x in (50.0, 51.6)]
    off, valid = lb.track_offsets(pts, META)
    r0, r1, c0, c1 = int(108 * SY), int(112 * SY), int(48 * SX), int(54 * SX)
    x, y, drawn = bilinear_drawn(lb.encode_offsets(off, valid)[r0:r1, c0:c1], use_b=True)
    x = x + c0 / SX
    assert not np.any(drawn & (np.minimum(np.abs(x - 50.0), np.abs(x - 51.6)) > 0.3))


def test_no_track_in_range_is_masked_and_decodes_beyond_the_range():
    off = np.full((1, 2, 2), np.nan)
    off[0, 0] = (0.0, 0.0)
    enc = lb.encode_offsets(off, ~np.isnan(off[..., 0]))
    assert tuple(enc[0, 0]) == (127, 127, 255) and tuple(enc[0, 1]) == (255, 255, 0)
    assert math.hypot(*((enc[0, 1, :2].astype(float) - 127) / 127)) * lb.TRACK_RANGE_M > lb.TRACK_RANGE_M * 1.3


def test_the_links_are_deterministic_and_join_near_neighbours():
    towns = [("a", 0.0, 0.0), ("b", 10.0, 0.0), ("c", 0.0, 12.0), ("d", 200.0, 200.0)]
    assert lb.track_links(towns) == [(0, 1), (0, 2), (1, 2)]
    assert lb.track_points(towns, lb.track_links(towns)) == lb.track_points(towns, lb.track_links(towns))


def test_the_hash_is_deterministic_uniform_and_seeded():
    i = np.arange(-500, 500)
    h = lb.hash01(i, i * 3, 7)
    assert np.array_equal(h, lb.hash01(i, i * 3, 7)) and not np.array_equal(h, lb.hash01(i, i * 3, 8))
    assert h.min() >= 0.0 and h.max() < 1.0 and abs(h.mean() - 0.5) < 0.05


def test_the_painted_land_is_weighted_by_the_masks_and_holds_every_crop():
    x, y = np.meshgrid(np.arange(0, 20000, 50.0), np.arange(0, 20000, 50.0))
    zero, one = np.zeros_like(x), np.ones_like(x)
    _, w = lb.paint_samples(x, y, zero, zero, zero, zero + 50.0, zero)
    assert w.max() == 0.0                                    # no fields, not wet: the biome's ground
    c, w = lb.paint_samples(x, y, one, zero, zero, zero + 50.0, zero)
    assert w.mean() > 0.8 and np.isfinite(c).all()          # farmed: parcels nearly everywhere
    # Each crop covers a share of the ground (its colour, give or take the rows and the parcel's shade).
    for name, share in (("green", 0.10), ("ochre", 0.08), ("plough", 0.06), ("fallow", 0.04)):
        ref = lb.linear(lb.SRGB[name])
        near = np.linalg.norm(c / np.maximum(c.mean(axis=-1, keepdims=True), 1e-6) - ref / ref.mean(), axis=-1) < 0.06
        assert near.mean() > share, name


def test_empty_texels_take_the_neighbouring_land_colour():
    rgb = np.zeros((8, 8, 3)); filled = np.zeros((8, 8), dtype=bool)
    rgb[:, :2] = (0.5, 0.4, 0.2); filled[:, :2] = True
    out = lb.dilate_colour(rgb, filled, passes=8)
    assert np.allclose(out, (0.5, 0.4, 0.2)), "no black spreads into the mips"


def test_the_land_reaches_into_the_water_and_avoids_the_rock():
    x, y = np.meshgrid(np.arange(0, 5000, 50.0), np.arange(0, 5000, 50.0))
    zero, one = np.zeros_like(x), np.ones_like(x)
    _, w_flat = lb.paint_samples(x, y, zero, zero, one, zero + 400.0, zero)
    _, w_cliff = lb.paint_samples(x, y, zero, zero, one, zero + 400.0, zero + 1.0)   # rise 1 per metre: rock
    assert w_flat.mean() > 0.5 and w_cliff.max() < 0.01


@pytest.fixture(scope="module")
def baked():
    return lb.bake()


def test_the_committed_textures_are_the_bake(baked):
    # Review M11: the committed PNGs must be what tools/land_bake.py makes from today's inputs (about 20 s).
    rgba, tracks = baked
    assert np.array_equal(np.asarray(Image.open(lb.OUT)), rgba), "landuse.png is stale: run uv run tools/land_bake.py"
    assert np.array_equal(np.asarray(Image.open(lb.OUT_TRACKS)), tracks), "tracks.png is stale: run uv run tools/land_bake.py"


def test_the_committed_textures_have_their_sizes_and_follow_the_ground():
    land = np.asarray(Image.open(lb.OUT))
    tracks = np.asarray(Image.open(lb.OUT_TRACKS))
    assert land.shape == (lb.COLOUR_H, lb.COLOUR_W, 4) and tracks.shape == (lb.TRACKS_H, lb.TRACKS_W, 3)

    def weight(lon, lat):
        r, c = int((33.35 - lat) / 3.9 * land.shape[0]), int((lon - 34.2) / 1.7 * land.shape[1])
        return land[r - 6:r + 7, c - 6:c + 7, 3].mean() / 255
    assert weight(34.84, 31.92) > 0.8 and weight(35.30, 32.62) > 0.8    # the coastal plain and the Jezreel valley farmed
    assert weight(34.90, 30.60) < 0.02 and weight(35.50, 31.50) < 0.02  # the central Negev and the Dead Sea untouched
    assert weight(35.15, 31.78) > 0.4                                   # garrigue and terraces on the Judean hills
    assert land[..., :3][land[..., 3] == 0].min(axis=0).max() > 0, "the empty texels hold land colour, not black"
