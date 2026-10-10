# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""Land use for the terrain shader (issue #33): where the ground is farmed, planted, terraced or tracked, from real data,
and what it looks like there, painted procedurally and baked (the shader only lays it over the biome ground:
StatecraftLand.hlsl).

Usage: uv run tools/land_bake.py   (about 15 seconds; no network)
Reads the committed heightmap (unity/Assets/StreamingAssets/map/heightmap.*), content/map/hexes.json (biomes),
content/map/precip_bio12.* + precip_stations.json (CHELSA mean annual rainfall, station-corrected exactly as
tools/hexgrid.py does) and content/map/settlements_1948.yaml. Where, on a WIDTH x HEIGHT grid (about 158 x 211 m):
  fields:   arable land: gentle slope (< ~7%) with enough rain for dry farming (smoothstep 180..380 mm; the Jordan
            valley floor, irrigated, at least 0.7), outside water
  orchards: the citrus belt: flat coastal plain or valley floor below 150 m within about 10 km of a town (exp(-d/7 km))
  wet:      rainfall smoothstep 250..600 mm: garrigue, terraces and scrub grow where it is high
What (paint_land, seeded by map position only): field parcels in rotated blocks (green, ochre, ploughed, fallow, grass,
groves in the citrus belt) with soft rims and hedges, some block edges as tracks; on the wet unfarmed hills patches of
stony soil and grass, terraces along the real contours, scrub. Writes, row 0 = north:
  unity/Assets/Textures/Map/landuse.png  RGBA8, COLOUR_W x COLOUR_H (about 79 x 105 m): rgb the land's linear colour, a
      its weight over the biome ground (2 x 2 samples per texel)
  unity/Assets/Textures/Map/tracks.png   RGB8, TRACKS_W x TRACKS_H: R, G the offset (east, south) from the texel to the
      nearest dirt track (encode_offsets); each settlement is joined to its nearest neighbours within TRACK_MAX_KM by a
      gently meandering track
The geography is real (slope, rain, towns); the parcels are procedural. Deterministic: the same PNGs on every run."""
import json
import math
import warnings
from pathlib import Path

import numpy as np
import yaml
from PIL import Image

import hexgrid
import terrain_bake as tb

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
CONTENT = ROOT / "content" / "map"
OUT = ROOT / "unity" / "Assets" / "Textures" / "Map" / "landuse.png"
OUT_TRACKS = ROOT / "unity" / "Assets" / "Textures" / "Map" / "tracks.png"
WIDTH, HEIGHT = 1024, 2048
TRACKS_W, TRACKS_H = 512, 1024   # about 316 x 421 m per texel: the offsets are affine across a track, so this is enough
SLOPE_SMOOTH_M = 300.0           # the slope that decides "flat" is averaged over about this radius
FLAT_SLOPE = (0.035, 0.075)      # rise/run: fully arable below the first, none above the second
RAIN_FIELDS_MM = (180.0, 380.0)  # dry farming (barley in the northern Negev at about 200-250 mm)
RAIN_WET_MM = (250.0, 600.0)
VALLEY_IRRIGATED = 0.7
ORCHARD_BELOW_M = 150.0
ORCHARD_DECAY_KM = 7.0
TRACK_NEIGHBOURS = 3
TRACK_MAX_KM = 40.0
TRACK_RANGE_M = 1000.0           # StatecraftLand.hlsl ScTrackRangeM (more than a texel diagonal)
TRACK_SEED = 1936
MEDIAL_JUMP_M = 900.0            # neighbouring texels whose nearest track points are farther apart sit on a medial axis
MEDIAL_KEEP_M = 250.0            # ... and keep their offset only this close to a track
# The painted land (paint_land): field parcels, rims, hedges, block-edge tracks, garrigue, terraces and scrub, baked here
# rather than drawn in the shader: the same pattern as procedural shader code cost about 5 ms a frame at gameplay03 on
# WebGL even where no pixel ran it (docs/visual-v1.md, "Promo track: land use").
COLOUR_W, COLOUR_H = 2048, 4096  # about 79 m east-west x 105 m north-south per texel
TEXEL_M = 92.0                   # mean texel size (m), for the terrace fade
SUPERSAMPLE = 2                  # samples per texel per axis, box-filtered (the parcels' edges stay soft, not jagged)
BLOCK_M = 5200.0                 # a farm block, one angle per block
STRIP_M = 1000.0                 # strip width across the block (symbolic scale, like the kits)
TERRACE_STEP_M = 40.0            # real metres of rise per terrace
VERTICAL_EXAGGERATION = tb.VERTICAL_EXAGGERATION
PALETTE_BRIGHTNESS, PALETTE_MAX = 1.107, 0.95   # MapPalette.Brightness and MaxChannel: the land sits in the biome palette
SRGB = {  # the land's colours (sRGB, before PALETTE_BRIGHTNESS), sampled from promo/final/03's ground and tuned on renders
    "green": (0.46, 0.50, 0.31), "ochre": (0.78, 0.66, 0.44), "plough": (0.58, 0.47, 0.34), "fallow": (0.72, 0.66, 0.51),
    "grass": (0.52, 0.54, 0.33), "orchard": (0.52, 0.47, 0.31), "hedge": (0.27, 0.30, 0.18), "dust": (0.84, 0.76, 0.61),
    "soil": (0.66, 0.57, 0.46), "hillgrass": (0.56, 0.55, 0.37), "stone": (0.74, 0.68, 0.56), "scrub": (0.34, 0.36, 0.22),
}


def smoothstep(e0: float, e1: float, x: np.ndarray) -> np.ndarray:
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def bilinear(a: np.ndarray, fy: np.ndarray, fx: np.ndarray) -> np.ndarray:
    """a sampled at fractional (row, col) positions, clamped to the edges."""
    h, w = a.shape
    fy = np.clip(fy, 0, h - 1); fx = np.clip(fx, 0, w - 1)
    y0 = np.minimum(np.floor(fy).astype(int), h - 2); x0 = np.minimum(np.floor(fx).astype(int), w - 2)
    ty, tx = fy - y0, fx - x0
    return ((a[y0, x0] * (1 - tx) + a[y0, x0 + 1] * tx) * (1 - ty) + (a[y0 + 1, x0] * (1 - tx) + a[y0 + 1, x0 + 1] * tx) * ty)


def texel_km(meta: dict) -> tuple[np.ndarray, np.ndarray]:
    """(x km east of lon0, y km south of lat1) of every output texel centre, shape (HEIGHT, WIDTH)."""
    ys, xs = np.mgrid[0:HEIGHT, 0:WIDTH]
    return (xs + 0.5) / WIDTH * meta["width_km"], (ys + 0.5) / HEIGHT * meta["length_km"]


def slope_map(elev: np.ndarray, meta: dict) -> np.ndarray:
    dx, dy = tb.spacing_m(meta)
    gy, gx = np.gradient(elev.astype(np.float64), dy, dx)
    s = np.hypot(gx, gy)
    return tb.box_mean(s, max(1, round(SLOPE_SMOOTH_M / dy)), max(1, round(SLOPE_SMOOTH_M / dx)))


def rain_at(xk: np.ndarray, yk: np.ndarray, meta: dict) -> np.ndarray:
    pr = hexgrid.load_corrected_precip(CONTENT)
    m = pr["meta"]
    mm = pr["mm"].copy()
    # Sea cells are NaN: fill each with the mean of its finite neighbours, repeatedly, so the coast's rain reaches the shore.
    for _ in range(4):
        nan = np.isnan(mm)
        if not nan.any():
            break
        p = np.pad(mm, 1, mode="edge")
        stack = np.stack([p[1 + oy:1 + oy + mm.shape[0], 1 + ox:1 + ox + mm.shape[1]] for oy in (-1, 0, 1) for ox in (-1, 0, 1)])
        with np.errstate(invalid="ignore"), warnings.catch_warnings():
            warnings.simplefilter("ignore", RuntimeWarning)
            fill = np.nanmean(stack, axis=0)
        mm = np.where(nan, fill, mm)
    mm = np.nan_to_num(mm, nan=0.0)
    lon = meta["lon0"] + xk / hexgrid.km_per_deg_lon(meta)
    lat = meta["lat1"] - yk / hexgrid.KM_PER_DEG_LAT
    return bilinear(mm, (m["lat1"] - lat) / m["cell_deg"] - 0.5, (lon - m["lon0"]) / m["cell_deg"] - 0.5)


def biome_at(xk: np.ndarray, yk: np.ndarray, hexes: dict) -> np.ndarray:
    lookup = {(h["q"], h["r"]): h["biome"] for h in hexes["hexes"]}
    q, r = hexgrid.point_to_hex(xk.ravel(), yk.ravel(), hexes["hex_size_km"])
    return np.array([lookup.get(k, "sea") for k in zip(q, r)]).reshape(xk.shape)


def settlements_km(meta: dict) -> list[tuple[str, float, float]]:
    doc = yaml.safe_load((CONTENT / "settlements_1948.yaml").read_text())
    return [(s["id"], *hexgrid.lonlat_to_km(s["lon"], s["lat"], meta)) for s in doc["settlements"]]


def track_links(towns: list[tuple[str, float, float]]) -> list[tuple[int, int]]:
    links = set()
    for i, (_, x, y) in enumerate(towns):
        near = sorted((math.dist((x, y), (u, v)), j) for j, (_, u, v) in enumerate(towns) if j != i)
        for d, j in near[:TRACK_NEIGHBOURS]:
            if d <= TRACK_MAX_KM:
                links.add((min(i, j), max(i, j)))
    return sorted(links)


def track_points(towns: list[tuple[str, float, float]], links: list[tuple[int, int]]) -> list[tuple[float, float]]:
    """Points (km) every 40 m along each track: the straight line bent by two seeded sine meanders, tapered to 0 at the
    towns (amplitude up to 4% of the link's length, at most 900 m)."""
    rng = np.random.default_rng(TRACK_SEED)
    pts = []
    for i, j in links:
        (_, x0, y0), (_, x1, y1) = towns[i], towns[j]
        length = math.dist((x0, y0), (x1, y1))
        n = max(2, int(length / 0.04))
        t = np.linspace(0.0, 1.0, n)
        a1, a2 = rng.uniform(-1, 1, 2) * min(0.04 * length, 0.9)
        k1, k2 = rng.uniform(1.0, 2.0), rng.uniform(2.5, 4.5)
        ph = rng.uniform(0, math.pi)
        off = (a1 * np.sin(math.pi * k1 * t) + 0.35 * a2 * np.sin(math.pi * k2 * t + ph)) * np.sin(math.pi * t)
        nx, ny = -(y1 - y0) / length, (x1 - x0) / length
        pts += list(zip(x0 + (x1 - x0) * t + nx * off, y0 + (y1 - y0) * t + ny * off))
    return pts


def track_offsets(points: list[tuple[float, float]], meta: dict) -> np.ndarray:
    """(east, south) metres from every TRACKS_W x TRACKS_H texel centre to its nearest track point, NaN beyond
    TRACK_RANGE_M. An offset, not a distance: across a straight track it is affine in position, so the GPU's bilinear
    filter reproduces it exactly between texels and the shader's line keeps its width (a distance field, V-shaped across the
    line, under-reads it between texels: the first try drew the tracks as dashes)."""
    sx, sy = TRACKS_W / meta["width_km"], TRACKS_H / meta["length_km"]   # texels per km
    d2 = np.full((TRACKS_H, TRACKS_W), np.inf)
    off = np.full((TRACKS_H, TRACKS_W, 2), np.nan)
    rx, ry = math.ceil(TRACK_RANGE_M / 1000 * sx) + 1, math.ceil(TRACK_RANGE_M / 1000 * sy) + 1
    for px, py in points:
        c, r = int(px * sx), int(py * sy)
        r0, r1, c0, c1 = max(r - ry, 0), min(r + ry + 1, TRACKS_H), max(c - rx, 0), min(c + rx + 1, TRACKS_W)
        if r0 >= r1 or c0 >= c1:
            continue
        ey = np.broadcast_to((py - (np.arange(r0, r1) + 0.5) / sy)[:, None] * 1000, (r1 - r0, c1 - c0))
        ex = np.broadcast_to((px - (np.arange(c0, c1) + 0.5) / sx)[None, :] * 1000, (r1 - r0, c1 - c0))
        dd = ex * ex + ey * ey
        win = dd < d2[r0:r1, c0:c1]
        d2[r0:r1, c0:c1][win] = dd[win]
        off[r0:r1, c0:c1, 0][win] = ex[win]
        off[r0:r1, c0:c1, 1][win] = ey[win]
    off[d2 > TRACK_RANGE_M ** 2] = np.nan
    # Medial axes: where two neighbouring texels' nearest points lie on different tracks, filtering between their offsets
    # (pointing opposite ways) passes near zero and would draw a phantom track. Away from the tracks themselves such texels
    # become "no track in range".
    ys, xs = np.mgrid[0:TRACKS_H, 0:TRACKS_W]
    qx = (xs + 0.5) / sx * 1000 + off[..., 0]                    # nearest track point (m east, m south)
    qy = (ys + 0.5) / sy * 1000 + off[..., 1]
    split = np.zeros((TRACKS_H, TRACKS_W), dtype=bool)
    for a, b in ((np.s_[:, :-1], np.s_[:, 1:]), (np.s_[:-1, :], np.s_[1:, :])):
        jump = np.hypot(qx[a] - qx[b], qy[a] - qy[b]) > MEDIAL_JUMP_M   # NaN (no track) compares False
        split[a] |= jump; split[b] |= jump
    off[split & (np.hypot(off[..., 0], off[..., 1]) > MEDIAL_KEEP_M)] = np.nan
    return off


def encode_offsets(off: np.ndarray) -> np.ndarray:
    """RGB8: R, G = round(127 + 127 offset / TRACK_RANGE_M) (east, south), B 0; no track in range = (255, 255), which
    decodes to about 1.4 TRACK_RANGE_M away, so filtering towards it never fakes a track."""
    enc = np.clip(np.round(127 + 127 * off / TRACK_RANGE_M), 0, 254)
    enc = np.where(np.isnan(off), 255, enc).astype(np.uint8)
    return np.concatenate([enc, np.zeros(enc.shape[:2] + (1,), dtype=np.uint8)], axis=-1)


def linear(srgb: tuple[float, float, float]) -> np.ndarray:
    """An SRGB colour at the palette's brightness, in linear light (the texture holds linear values, like the biome
    texture's sampled colour)."""
    c = np.minimum(np.array(srgb) * PALETTE_BRIGHTNESS, PALETTE_MAX)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def hash01(*ints: np.ndarray) -> np.ndarray:
    """A deterministic hash of integer lattice coordinates to [0, 1) (uint64 arithmetic: the same on every platform)."""
    h = np.uint64(0x9E3779B97F4A7C15)
    for i, v in enumerate(ints):
        k = np.asarray(v).astype(np.int64).astype(np.uint64) * np.uint64(0xBF58476D1CE4E5B9 + 2 * i)
        h = (h ^ k) * np.uint64(0x94D049BB133111EB)
        h ^= h >> np.uint64(31)
    return (h >> np.uint64(11)).astype(np.float64) / float(1 << 53)


def value_noise(x: np.ndarray, y: np.ndarray, seed: int) -> np.ndarray:
    xi, yi = np.floor(x), np.floor(y)
    fx, fy = x - xi, y - yi
    ux, uy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    xi, yi = xi.astype(np.int64), yi.astype(np.int64)
    a, b = hash01(xi, yi, seed), hash01(xi + 1, yi, seed)
    c, d = hash01(xi, yi + 1, seed), hash01(xi + 1, yi + 1, seed)
    return (a + (b - a) * ux) * (1 - uy) + (c + (d - c) * ux) * uy


def paint_samples(x: np.ndarray, y: np.ndarray, fields: np.ndarray, orchards: np.ndarray, wet: np.ndarray,
                  elev: np.ndarray, grad: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Linear colour (..., 3) and weight (...) of the land at map points (x m east, y m north; elev real metres, grad its
    rise per metre). Weight 0 leaves the biome's own ground."""
    col = {k: linear(v) for k, v in SRGB.items()}
    patch = value_noise(x / 2600.0 + 3.1, y / 2600.0 + 3.1, 1)          # one patch noise for every patchy choice
    # Farm blocks: a wobbled grid, each block turned by up to +-20 degrees (a few by 90) about its own centre.
    wx = 0.275 * np.sin(y / 4100.0 + np.sin(x / 6700.0))
    wy = 0.275 * np.sin(x / 3900.0 + np.sin(y / 5300.0))
    bpx, bpy = x / BLOCK_M + wx, y / BLOCK_M + wy
    bix, biy = np.floor(bpx), np.floor(bpy)
    bfx, bfy = bpx - bix, bpy - biy
    bh = hash01(bix, biy, 2)
    ang = (bh - 0.5) * 0.7 + np.where(bh > 0.8, math.pi / 2, 0.0)
    qx, qy = (bfx - 0.5) * BLOCK_M, (bfy - 0.5) * BLOCK_M
    lx, ly = np.cos(ang) * qx - np.sin(ang) * qy, np.sin(ang) * qx + np.cos(ang) * qy
    # Strips across the block, each cut into parcels of its own length (1.1-2.6 km).
    sx = lx / STRIP_M + bh * 7.0
    ci = np.floor(sx)
    ch = hash01(ci, bix, biy, 3)
    len_m = 1100.0 + 1500.0 * ch
    sy = ly / len_m + ch * 5.0
    pj = np.floor(sy)
    ph, pv, po = hash01(ci, pj, bix, biy, 4), hash01(ci, pj, bix, biy, 5), hash01(ci, pj, bix, biy, 6)
    edge_m = np.minimum(np.minimum(sx - ci, ci + 1 - sx) * STRIP_M, np.minimum(sy - pj, pj + 1 - sy) * len_m)
    # The crop: 0.30 green, 0.20 ochre, 0.17 ploughed, 0.13 fallow, 0.20 grass; in the citrus belt up to 55% groves.
    kinds = [(0.30, "green", 0.20), (0.50, "ochre", 0.08), (0.67, "plough", 0.24), (0.80, "fallow", 0.0), (1.01, "grass", 0.0)]
    crop = np.zeros(x.shape + (3,)); gain = np.zeros(x.shape); done = np.zeros(x.shape, dtype=bool)
    for upto, name, g in kinds:
        m = ~done & (ph < upto)
        crop[m] = col[name]; gain[m] = g; done |= m
    grove = po < orchards * 0.55
    crop[grove] = col["orchard"]; gain[grove] = 0.12
    rows = 0.6 * np.sin(lx * (2 * math.pi / 320.0))                     # rows (symbolic: 320 m apart, about 3 texels)
    crop *= ((1 + gain * rows) * (0.92 + 0.16 * pv))[..., None]
    rim = smoothstep(20.0, 90.0, edge_m)[..., None]                     # soft grassy rims (promo 03)
    farmed = col["grass"] * (1 - rim) + crop * rim
    hedge = ((edge_m < 18.0) & (pv > 0.45))[..., None]
    farmed = np.where(hedge, farmed * 0.3 + col["hedge"] * 0.7, farmed)
    block_edge_m = np.minimum(np.minimum(bfx, 1 - bfx), np.minimum(bfy, 1 - bfy)) * BLOCK_M
    edge_track = ((block_edge_m < 30.0) & (hash01(np.floor(bpx + 0.5), np.floor(bpy + 0.5), bix, biy, 7) > 0.65))[..., None]
    farmed = np.where(edge_track, col["dust"] * 0.85 + farmed * 0.15, farmed)
    farm_w = fields * smoothstep(0.08, 0.22, patch * 0.6 + fields * 0.5)

    # The wet unfarmed hills: patches of stony soil and grass (garrigue), terraces on the slopes, scrub.
    slope = 1.0 - 1.0 / np.sqrt(1.0 + (VERTICAL_EXAGGERATION * grad) ** 2)   # the shader's 1 - n.y on the drawn relief
    wild = wet * (1 - farm_w)
    hill = (col["soil"] * (1 - smoothstep(0.35, 0.65, patch))[..., None] + col["hillgrass"] * smoothstep(0.35, 0.65, patch)[..., None])
    tf = np.mod(elev / TERRACE_STEP_M, 1.0)
    steps_per_texel = grad * TEXEL_M / TERRACE_STEP_M
    terrace = (smoothstep(0.05, 0.10, slope) * (1 - smoothstep(0.45, 0.65, slope)) * smoothstep(120.0, 320.0, elev)
               * smoothstep(0.55, 0.70, 1 - patch) * (1 - smoothstep(0.35, 0.8, steps_per_texel)))[..., None]
    terraced = hill * (0.78 + 0.24 * tf)[..., None]
    terraced = np.where((tf > 0.75)[..., None], terraced * 0.65 + col["stone"] * 0.35, terraced)
    hill = hill * (1 - terrace) + terraced * terrace
    scrub = (0.14 + 0.2 * smoothstep(0.55, 0.85, patch)) * (1 - smoothstep(0.5, 0.8, slope))
    speck = (hash01(np.floor(x / 160.0), np.floor(y / 160.0), 8) < 0.30) * (1 - smoothstep(0.5, 0.8, slope)) * 0.7   # bushes
    hill = hill * (1 - np.maximum(scrub, speck))[..., None] + col["scrub"] * np.maximum(scrub, speck)[..., None]
    wild_w = wild * smoothstep(80.0, 250.0, elev) * 0.7

    w = farm_w + wild_w
    colour = (farmed * farm_w[..., None] + hill * wild_w[..., None]) / np.maximum(w, 1e-6)[..., None]
    return colour, np.clip(w, 0.0, 1.0)


def paint_land(fields: np.ndarray, orchards: np.ndarray, wet: np.ndarray, elev: np.ndarray, meta: dict) -> np.ndarray:
    """RGBA8 COLOUR_W x COLOUR_H (row 0 = north): rgb the land's linear colour, a its weight, from SUPERSAMPLE^2 samples per
    texel. The masks are WIDTH x HEIGHT, the elevation the heightmap's (row 0 = north)."""
    width_m, length_m = meta["width_km"] * 1000, meta["length_km"] * 1000
    dx, dy = tb.spacing_m(meta)
    gy, gx = np.gradient(elev.astype(np.float64), dy, dx)
    grad = np.hypot(gx, gy)
    out = np.zeros((COLOUR_H, COLOUR_W, 4))
    ss = SUPERSAMPLE
    offs = (np.arange(ss) + 0.5) / ss
    chunk = 256
    for r0 in range(0, COLOUR_H, chunk):
        rows = np.arange(r0, min(r0 + chunk, COLOUR_H))
        fr = (rows[:, None] + offs[None, :]).ravel()                    # sample rows (texel units, 0 = north edge)
        fc = (np.arange(COLOUR_W)[:, None] + offs[None, :]).ravel()
        FR, FC = np.meshgrid(fr, fc, indexing="ij")
        x = FC / COLOUR_W * width_m
        y = length_m - FR / COLOUR_H * length_m
        my, mx = FR / COLOUR_H * HEIGHT - 0.5, FC / COLOUR_W * WIDTH - 0.5
        hy, hx = FR / COLOUR_H * (meta["height"] - 1), FC / COLOUR_W * (meta["width"] - 1)
        c, w = paint_samples(x, y, bilinear(fields, my, mx), bilinear(orchards, my, mx), bilinear(wet, my, mx),
                             bilinear(elev.astype(np.float64), hy, hx), bilinear(grad, hy, hx))
        rgbw = np.concatenate([c * w[..., None], w[..., None]], axis=-1)   # premultiplied, so the box filter weighs colour
        n = len(rows)
        rgbw = rgbw.reshape(n, ss, COLOUR_W, ss, 4).mean(axis=(1, 3))
        out[rows] = np.concatenate([rgbw[..., :3] / np.maximum(rgbw[..., 3:], 1e-6), rgbw[..., 3:]], axis=-1)
    return tb.to_u8(out)


def bake() -> tuple[np.ndarray, np.ndarray]:
    elev, meta = tb.load_heightmap(MAP_DIR)
    hexes = json.loads((CONTENT / "hexes.json").read_text())
    xk, yk = texel_km(meta)
    hy = yk / meta["length_km"] * (meta["height"] - 1)       # heightmap row (0 = north) and column of each texel
    hx = xk / meta["width_km"] * (meta["width"] - 1)
    e = bilinear(elev.astype(np.float64), hy, hx)
    slope = bilinear(slope_map(elev, meta), hy, hx)
    dx, dy = tb.spacing_m(meta)
    water_hm = tb.water_mask(elev, dx, dy).astype(np.float64)
    water = bilinear(water_hm, hy, hx) > 0.5
    rain = rain_at(xk, yk, meta)
    biome = biome_at(xk, yk, hexes)
    valley = biome == "valley"

    flat = 1.0 - smoothstep(*FLAT_SLOPE, slope)
    farm_rain = np.maximum(smoothstep(*RAIN_FIELDS_MM, rain), np.where(valley, VALLEY_IRRIGATED, 0.0))
    fields = flat * farm_rain

    towns = settlements_km(meta)
    d_town = np.full(xk.shape, np.inf)
    for _, x, y in towns:
        d_town = np.minimum(d_town, np.hypot(xk - x, yk - y))
    lowland = (e < ORCHARD_BELOW_M) & np.isin(biome, ("coastal_plain", "valley"))
    orchards = np.where(lowland, flat * farm_rain * np.clip(1.4 * np.exp(-d_town / ORCHARD_DECAY_KM) - 0.15, 0, 1), 0.0)

    wet = smoothstep(*RAIN_WET_MM, rain)

    land = ~water
    rgba = paint_land(fields * land, orchards * land, wet * land, elev, meta)
    return rgba, encode_offsets(track_offsets(track_points(towns, track_links(towns)), meta))


def main() -> None:
    rgba, tracks = bake()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(OUT)
    Image.fromarray(tracks, "RGB").save(OUT_TRACKS)
    print(f"wrote {OUT} ({COLOUR_W} x {COLOUR_H}): land weight mean {rgba[..., 3].mean() / 255:.3f}; "
          f"{OUT_TRACKS} ({TRACKS_W} x {TRACKS_H}): {np.mean(tracks[..., 0] < 255):.2%} of texels near a track")


if __name__ == "__main__":
    main()
