# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0"]
# ///
"""Heightmap -> the baked shading textures of the terrain and sea shaders (no network: reads only the committed heightmap).

Usage: uv run tools/terrain_bake.py   (about 45 seconds)
Reads unity/Assets/StreamingAssets/map/heightmap.r16 + heightmap.json; writes, 2048 x 2048, row 0 = north:
  unity/Assets/Textures/Map/terrain_bake.png  RGB8: R ambient occlusion (horizon search to AO_RADIUS_M; 1 = open),
      G curvature (0.5 flat, > 0.5 convex), B sky-view factor (horizon search to SKY_RADIUS_M)
  unity/Assets/Textures/Map/coast.png         L8: 0.5 + d / (2 COAST_RANGE_M), clamped; d = signed distance (m) to the
      nearest shoreline, < 0 in water (open sea: |elevation| <= SEA_EPS_M and connected to the map's border; lakes: flat
      water below LAKE_BELOW_M), the rules tools/hexgrid.py uses for its sea and lake biomes
Heights are multiplied by VERTICAL_EXAGGERATION (MapBuilder.VerticalExaggeration) so the shading matches the relief the
game draws. Texel (j, i) is the mean of heightmap samples j..j+1, i..i+1, so its centre sits at terrain UV (j + 0.5) / 2048.
Distances use each axis' own sample spacing (about 79 m east-west, 211 m north-south)."""
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
OUT_DIR = ROOT / "unity" / "Assets" / "Textures" / "Map"
VERTICAL_EXAGGERATION = 3.0
AO_RADIUS_M = 2000.0
SKY_RADIUS_M = 12000.0
DIRECTIONS = 16
STEPS = 16
CURVATURE_RADIUS_M = 600.0
CURVATURE_SCALE_M = 150.0      # exaggerated metres above or below the local mean that reach 1 or 0
SEA_EPS_M = 0.5
LAKE_BELOW_M = -150.0
LAKE_FLAT_M = 0.25             # largest height range in a 3x3 window that is still a water surface
COAST_RANGE_M = 12000.0        # StatecraftMap.hlsl ScCoastRangeM


def load_heightmap(map_dir: Path) -> tuple[np.ndarray, dict]:
    meta = json.loads((map_dir / "heightmap.json").read_text())
    raw = np.fromfile(map_dir / "heightmap.r16", dtype="<u2").reshape(meta["height"], meta["width"])
    elev = meta["elev_min_m"] + raw.astype(np.float64) / 65535.0 * (meta["elev_max_m"] - meta["elev_min_m"])
    return elev.astype(np.float32), meta


def spacing_m(meta: dict) -> tuple[float, float]:
    return meta["width_km"] * 1000 / (meta["width"] - 1), meta["length_km"] * 1000 / (meta["height"] - 1)


def shifted(a: np.ndarray, oy: int, ox: int) -> np.ndarray:
    """out[y, x] = a[y + oy, x + ox], the border's values repeated beyond it."""
    h, w = a.shape
    ys = np.clip(np.arange(h) + oy, 0, h - 1)
    xs = np.clip(np.arange(w) + ox, 0, w - 1)
    return a[ys[:, None], xs[None, :]]


def openness(z: np.ndarray, dx: float, dy: float, radius_m: float, directions: int = DIRECTIONS, steps: int = STEPS) -> np.ndarray:
    """Sky-view factor: 1 - the mean over directions of sin(horizon elevation), the horizon searched out to radius_m and
    never below 0 (a peak sees the whole upper hemisphere: 1)."""
    acc = np.zeros(z.shape, dtype=np.float32)
    for k in range(directions):
        theta = 2 * math.pi * k / directions
        best = np.zeros(z.shape, dtype=np.float32)      # tan of the highest horizon so far
        seen: set[tuple[int, int]] = set()
        for s in range(1, steps + 1):
            r = radius_m * (s / steps) ** 1.5             # denser near the point
            ox, oy = int(round(r * math.cos(theta) / dx)), int(round(-r * math.sin(theta) / dy))
            if (ox, oy) == (0, 0) or (ox, oy) in seen:
                continue
            seen.add((ox, oy))
            np.maximum(best, (shifted(z, oy, ox) - z) / math.hypot(ox * dx, oy * dy), out=best)
        acc += np.sin(np.arctan(best))
    return (1.0 - acc / directions).astype(np.float32)


def box_mean(a: np.ndarray, ry: int, rx: int) -> np.ndarray:
    """Mean over the (2 ry + 1) x (2 rx + 1) window, the border repeated (integral image)."""
    p = np.pad(a.astype(np.float64), ((ry, ry), (rx, rx)), mode="edge")
    c = np.zeros((p.shape[0] + 1, p.shape[1] + 1))
    c[1:, 1:] = p.cumsum(0).cumsum(1)
    h, w = a.shape
    ky, kx = 2 * ry + 1, 2 * rx + 1
    s = c[ky:ky + h, kx:kx + w] - c[0:h, kx:kx + w] - c[ky:ky + h, 0:w] + c[0:h, 0:w]
    return (s / (ky * kx)).astype(np.float32)


def curvature(z: np.ndarray, dx: float, dy: float) -> np.ndarray:
    """0.5 + (z - local mean) / (2 CURVATURE_SCALE_M), clamped to [0, 1]; the local mean is two box passes (a tent)."""
    rx, ry = max(1, round(CURVATURE_RADIUS_M / dx)), max(1, round(CURVATURE_RADIUS_M / dy))
    mean = box_mean(box_mean(z, ry, rx), ry, rx)
    return np.clip(0.5 + (z - mean) / (2 * CURVATURE_SCALE_M), 0.0, 1.0).astype(np.float32)


def grow(mask: np.ndarray) -> np.ndarray:
    out = mask.copy()
    out[1:, :] |= mask[:-1, :]; out[:-1, :] |= mask[1:, :]
    out[:, 1:] |= mask[:, :-1]; out[:, :-1] |= mask[:, 1:]
    return out


def shrink(mask: np.ndarray) -> np.ndarray:
    return ~grow(~mask)


def border_connected(candidate: np.ndarray) -> np.ndarray:
    """The pixels of candidate connected (4-neighbour) to the map's border through candidate pixels."""
    reached = np.zeros_like(candidate)
    for edge in (np.s_[0, :], np.s_[-1, :], np.s_[:, 0], np.s_[:, -1]):
        reached[edge] = candidate[edge]
    while True:
        before = reached
        for _ in range(16):                          # 16 growth steps per convergence check
            reached = grow(reached) & candidate
        if np.array_equal(before, reached):
            return reached


def lake_mask(elev: np.ndarray) -> np.ndarray:
    hi, lo = elev.copy(), elev.copy()
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            s = shifted(elev, oy, ox)
            np.maximum(hi, s, out=hi); np.minimum(lo, s, out=lo)
    flat = (elev < LAKE_BELOW_M) & (hi - lo <= LAKE_FLAT_M)
    return grow(grow(shrink(shrink(flat)))) & flat      # an opening: specks of flat rift floor drop out


def water_mask(elev: np.ndarray) -> np.ndarray:
    return border_connected(np.abs(elev) <= SEA_EPS_M) | lake_mask(elev)


def distance_to(target: np.ndarray, dx: float, dy: float, cap: float) -> np.ndarray:
    """Chamfer distance (m, 8 neighbours, each axis its own spacing) to the nearest target pixel, capped at cap."""
    d = np.where(target, 0.0, cap).astype(np.float32)
    diag = math.hypot(dx, dy)
    moves = [(0, 1, dx), (0, -1, dx), (1, 0, dy), (-1, 0, dy), (1, 1, diag), (1, -1, diag), (-1, 1, diag), (-1, -1, diag)]
    while True:
        new = d.copy()
        for oy, ox, step in moves:
            np.minimum(new, shifted(d, oy, ox) + np.float32(step), out=new)
        if np.array_equal(new, d):
            return d
        d = new


def to_texels(a: np.ndarray) -> np.ndarray:
    return (0.25 * (a[:-1, :-1] + a[1:, :-1] + a[:-1, 1:] + a[1:, 1:])).astype(np.float32)


def to_u8(a: np.ndarray) -> np.ndarray:
    return np.clip(np.round(a * 255), 0, 255).astype(np.uint8)


def encode_coast(signed_m: np.ndarray, range_m: float = COAST_RANGE_M) -> np.ndarray:
    return to_u8(0.5 + signed_m / (2 * range_m))


def bake(elev: np.ndarray, meta: dict) -> tuple[np.ndarray, np.ndarray]:
    dx, dy = spacing_m(meta)
    z = elev * np.float32(VERTICAL_EXAGGERATION)
    ao, sky, curv = openness(z, dx, dy, AO_RADIUS_M), openness(z, dx, dy, SKY_RADIUS_M), curvature(z, dx, dy)
    water = water_mask(elev)
    signed = np.where(water, -distance_to(~water, dx, dy, COAST_RANGE_M), distance_to(water, dx, dy, COAST_RANGE_M))
    rgb = np.stack([to_u8(to_texels(ao)), to_u8(to_texels(curv)), to_u8(to_texels(sky))], axis=-1)
    return rgb, encode_coast(to_texels(signed.astype(np.float32)))


def main(map_dir: Path = MAP_DIR, out_dir: Path = OUT_DIR) -> None:
    elev, meta = load_heightmap(map_dir)
    rgb, coast = bake(elev, meta)
    out_dir.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgb, "RGB").save(out_dir / "terrain_bake.png")
    Image.fromarray(coast, "L").save(out_dir / "coast.png")
    print(f"wrote {out_dir / 'terrain_bake.png'} and {out_dir / 'coast.png'} ({coast.shape[1]} x {coast.shape[0]}): "
          f"water {np.mean(coast < 128):.1%} of texels; AO mean {rgb[..., 0].mean() / 255:.3f}, sky-view mean {rgb[..., 2].mean() / 255:.3f}")


if __name__ == "__main__":
    main()
