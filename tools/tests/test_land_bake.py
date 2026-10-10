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


def test_the_committed_masks_have_their_sizes_and_follow_the_ground():
    land = np.asarray(Image.open(lb.OUT))
    tracks = np.asarray(Image.open(lb.OUT_TRACKS))
    assert land.shape == (lb.HEIGHT, lb.WIDTH, 3) and tracks.shape == (lb.TRACKS_H, lb.TRACKS_W, 3)

    def at(a, lon, lat):
        return a[int((33.35 - lat) / 3.9 * a.shape[0]), int((lon - 34.2) / 1.7 * a.shape[1])]
    assert at(land, 34.84, 31.92)[0] > 150 and at(land, 34.90, 30.60)[0] < 10   # plain farmed, central Negev not
    assert at(land, 35.15, 31.78)[2] > 128 and at(land, 35.50, 31.50).sum() == 0  # Judea wet; the Dead Sea is water
