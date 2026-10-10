import math

import numpy as np
from PIL import Image

import land_bake as lb

META = {"width_km": 161.53, "length_km": 431.22}


def test_offsets_are_affine_across_a_straight_track_so_filtering_keeps_its_line():
    # A north-south track at x = 50.0 km, sampled every 40 m like track_points.
    pts = [(50.0, y) for y in np.arange(100.0, 120.0, 0.04)]
    off = lb.track_offsets(pts, META)
    sx, sy = lb.TRACKS_W / META["width_km"], lb.TRACKS_H / META["length_km"]
    row = int(110.0 * sy)
    cols = [c for c in range(int(48.5 * sx), int(51.5 * sx)) if not np.isnan(off[row, c, 0])]
    # East offset = (track x - texel x) in metres: a straight line through the texels on both sides of the track.
    for c in cols:
        assert abs(off[row, c, 0] - (50.0 - (c + 0.5) / sx) * 1000) < 1e-6
    # The midpoint of two neighbours either side of the track lands on the track: the bilinear filter's view.
    left = max(c for c in cols if (c + 0.5) / sx < 50.0)
    mid_x = ((left + 0.5) + (left + 1.5)) / 2 / sx
    assert abs((off[row, left, 0] + off[row, left + 1, 0]) / 2 - (50.0 - mid_x) * 1000) < 1e-6


def test_no_track_in_range_decodes_beyond_the_range():
    off = np.full((1, 2, 2), np.nan)
    off[0, 0] = (0.0, 0.0)
    enc = lb.encode_offsets(off)
    assert tuple(enc[0, 0]) == (127, 127, 0) and tuple(enc[0, 1]) == (255, 255, 0)
    far = math.hypot(*((enc[0, 1, :2].astype(float) - 127) / 127)) * lb.TRACK_RANGE_M
    assert far > lb.TRACK_RANGE_M * 1.3


def test_medial_axes_between_two_tracks_draw_no_phantom():
    # Two parallel tracks 1.6 km apart: halfway between them no texel may claim a track nearer than MEDIAL_KEEP_M.
    pts = [(x, y) for y in np.arange(100.0, 120.0, 0.04) for x in (50.0, 51.6)]
    off = lb.track_offsets(pts, META)
    sx, sy = lb.TRACKS_W / META["width_km"], lb.TRACKS_H / META["length_km"]
    row = int(110.0 * sy)
    d = np.hypot(off[row, :, 0], off[row, :, 1])
    for c in range(len(d) - 1):           # what bilinear filtering sees between each pair of neighbours
        if np.isnan(d[c]) or np.isnan(d[c + 1]):
            continue
        between = np.hypot(*((off[row, c] + off[row, c + 1]) / 2))
        x = (c + 1) / sx
        assert between > 100.0 or min(abs(x - 50.0), abs(x - 51.6)) < 0.4


def test_the_links_are_deterministic_and_join_near_neighbours():
    towns = [("a", 0.0, 0.0), ("b", 10.0, 0.0), ("c", 0.0, 12.0), ("d", 200.0, 200.0)]
    assert lb.track_links(towns) == [(0, 1), (0, 2), (1, 2)]
    assert lb.track_points(towns, lb.track_links(towns)) == lb.track_points(towns, lb.track_links(towns))


def test_the_hash_is_deterministic_uniform_and_seeded():
    i = np.arange(-500, 500)
    h = lb.hash01(i, i * 3, 7)
    assert np.array_equal(h, lb.hash01(i, i * 3, 7)) and not np.array_equal(h, lb.hash01(i, i * 3, 8))
    assert h.min() >= 0.0 and h.max() < 1.0 and abs(h.mean() - 0.5) < 0.05


def test_the_painted_land_is_weighted_by_the_masks():
    x, y = np.meshgrid(np.arange(0, 20000, 50.0), np.arange(0, 20000, 50.0))
    zero, one = np.zeros_like(x), np.ones_like(x)
    _, w = lb.paint_samples(x, y, zero, zero, zero, zero + 50.0, zero)
    assert w.max() == 0.0                                    # no fields, not wet: the biome's ground
    c, w = lb.paint_samples(x, y, one, zero, zero, zero + 50.0, zero)
    assert w.mean() > 0.8 and np.isfinite(c).all()          # farmed: parcels nearly everywhere
    kinds = {tuple(np.round(lb.linear(lb.SRGB[k]), 4)) for k in ("green", "ochre", "plough", "fallow")}
    assert len(kinds) == 4 and c.std(axis=(0, 1)).max() > 0.03   # a patchwork, not one flat colour


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
