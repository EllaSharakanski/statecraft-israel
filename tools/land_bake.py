# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""Real-data land-use mask for the terrain shader (issue #33): where the ground is farmed, planted or tracked. The shader
(StatecraftLand.hlsl) draws the detail itself (field parcels, rows, hedges, terraces, scrub); this mask only says where.

Usage: uv run tools/land_bake.py   (a few seconds; no network)
Reads the committed heightmap (unity/Assets/StreamingAssets/map/heightmap.*), content/map/hexes.json (biomes),
content/map/precip_bio12.* + precip_stations.json (CHELSA mean annual rainfall, station-corrected exactly as
tools/hexgrid.py does) and content/map/settlements_1948.yaml. Writes, row 0 = north,
unity/Assets/Textures/Map/landuse.png, RGB8, WIDTH x HEIGHT (about 158 m east-west x 211 m north-south per texel):
  R fields:   arable land: gentle slope (< ~7%) with enough rain for dry farming (smoothstep 180..380 mm; the Jordan
              valley floor, irrigated, at least 0.7), outside water
  G orchards: the citrus belt: flat coastal plain or valley floor below 150 m within about 10 km of a town (exp(-d/7 km))
  B wet:      rainfall smoothstep 250..600 mm: the shader's terraces and hill scrub grow where it is high
and unity/Assets/Textures/Map/tracks.png, RGB8, TRACKS_W x TRACKS_H: R, G the offset (east, south) from the texel to
the nearest dirt track (encode_offsets). Each settlement is joined to its nearest neighbours within TRACK_MAX_KM by a
gently meandering track (seeded: the output is deterministic).
The geography is real (slope, rain, towns); the parcels the shader paints over it are procedural."""
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
    rgb = tb.to_u8(np.stack([fields * land, orchards * land, wet * land], axis=-1))
    return rgb, encode_offsets(track_offsets(track_points(towns, track_links(towns)), meta))


def main() -> None:
    rgb, tracks = bake()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgb, "RGB").save(OUT)
    Image.fromarray(tracks, "RGB").save(OUT_TRACKS)
    f = rgb.astype(np.float64) / 255
    print(f"wrote {OUT} ({WIDTH} x {HEIGHT}): fields mean {f[..., 0].mean():.3f}, orchards {f[..., 1].mean():.3f}, "
          f"wet {f[..., 2].mean():.3f}; {OUT_TRACKS} ({TRACKS_W} x {TRACKS_H}): {np.mean(tracks[..., 0] < 255):.2%} of texels near a track")


if __name__ == "__main__":
    main()
