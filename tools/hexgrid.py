# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3"]
# ///
"""Heightmap -> hex grid (Red Blob Games axial, pointy-top) with elevation and biome per hex.

Usage: uv run tools/hexgrid.py   (reads unity/Assets/StreamingAssets/map/, writes content/map/hexes.json)
Projection: equirectangular km, x east from lon0, y south from lat1 (so r grows southward).
Biome rules, first match wins (per hex, over the heightmap pixels inside it):
  1 sea           >= 50% of pixels within 0.5 m of 0 m (Copernicus sea is exactly 0 m)
  2 lake          >= 40% of pixels are flat water: below -150 m and within 1.5 m of the hex's most common
                  sub -150 m level (Sea of Galilee ~-210 m, Dead Sea ~-430 m, evaporation ponds)
  3 valley        median < 0 m (Jordan rift / Arava floor)
  4 mountains     median >= 700 m
  5 desert        lat < 31.25 (Negev, Sinai, Arava, Edom foothills) or the Judean Desert strip
                  31.25 <= lat < 32.0 and 35.28 <= lon < 35.60
  6 hills         median >= 200 m
  7 coastal_plain everything else
"""
import json
import math
from pathlib import Path

import numpy as np

from geo import KM_PER_DEG_LAT, KM_PER_DEG_LON_EQ

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
OUT = ROOT / "content" / "map" / "hexes.json"
HEX_SIZE_KM = 5.0  # circumradius (centre to corner)
SQRT3 = math.sqrt(3)


def km_per_deg_lon(meta: dict) -> float:
    return KM_PER_DEG_LON_EQ * math.cos(math.radians((meta["lat0"] + meta["lat1"]) / 2))


def hex_center_km(q: int, r: int, size: float) -> tuple[float, float]:
    return size * (SQRT3 * q + SQRT3 / 2 * r), size * 1.5 * r


def point_to_hex(x: np.ndarray, y: np.ndarray, size: float) -> tuple[list[int], list[int]]:
    """Vectorised axial rounding (cube-coordinate round), Red Blob Games 'pixel to hex'."""
    qf = (SQRT3 / 3 * x - y / 3) / size
    rf = (2 / 3 * y) / size
    sf = -qf - rf
    q, r, s = np.round(qf), np.round(rf), np.round(sf)
    dq, dr, ds = np.abs(q - qf), np.abs(r - rf), np.abs(s - sf)
    fix_q = (dq > dr) & (dq > ds)
    fix_r = ~fix_q & (dr > ds)
    q = np.where(fix_q, -r - s, q)
    r = np.where(fix_r, -q - s, r)
    return q.astype(int).tolist(), r.astype(int).tolist()


def water_share(v: np.ndarray) -> float:
    low = np.round(v[v < -150])
    if low.size == 0:
        return 0.0
    vals, counts = np.unique(low, return_counts=True)
    level = vals[np.argmax(counts)]
    return float(np.mean((v < -150) & (np.abs(v - level) < 1.5)))


def classify(st: dict, lat: float, lon: float) -> str:
    if st["sea_share"] >= 0.5:
        return "sea"
    if st["water_share"] >= 0.4:
        return "lake"
    if st["median"] < 0:
        return "valley"
    if st["median"] >= 700:
        return "mountains"
    if lat < 31.25 or (31.25 <= lat < 32.0 and 35.28 <= lon < 35.60):
        return "desert"
    if st["median"] >= 200:
        return "hills"
    return "coastal_plain"


def load_elevation(map_dir: Path) -> tuple[np.ndarray, dict]:
    meta = json.loads((map_dir / "heightmap.json").read_text())
    raw = np.fromfile(map_dir / "heightmap.r16", dtype="<u2").reshape(meta["height"], meta["width"])
    elev = meta["elev_min_m"] + raw.astype("float64") / 65535 * (meta["elev_max_m"] - meta["elev_min_m"])
    return elev, meta


def build(map_dir: Path, size: float = HEX_SIZE_KM) -> dict:
    elev, meta = load_elevation(map_dir)
    h, w = elev.shape
    width_km = (meta["lon1"] - meta["lon0"]) * km_per_deg_lon(meta)
    length_km = (meta["lat1"] - meta["lat0"]) * KM_PER_DEG_LAT
    ys, xs = np.mgrid[0:h, 0:w]
    q, r = point_to_hex(xs.ravel() / (w - 1) * width_km, ys.ravel() / (h - 1) * length_km, size)
    q, r = np.array(q), np.array(r)
    values = elev.ravel()
    order = np.lexsort((q, r))
    keys = np.stack([r[order], q[order]], axis=1)
    starts = np.flatnonzero(np.r_[True, np.any(keys[1:] != keys[:-1], axis=1)])
    hexes = []
    for a, b in zip(starts, np.r_[starts[1:], len(order)]):
        hr, hq = (int(v) for v in keys[a])
        cx, cy = hex_center_km(hq, hr, size)
        if not (0 <= cx <= width_km and 0 <= cy <= length_km):
            continue  # partial hex on the map edge: centre outside the bounds
        v = values[order[a:b]]
        st = {"sea_share": float(np.mean(np.abs(v) < 0.5)), "water_share": water_share(v),
              "median": float(np.median(v))}
        lon = meta["lon0"] + cx / km_per_deg_lon(meta)
        lat = meta["lat1"] - cy / KM_PER_DEG_LAT
        biome = classify(st, lat, lon)
        hexes.append({"q": hq, "r": hr, "lon": round(lon, 5), "lat": round(lat, 5), "elev_m": round(st["median"], 1),
                      "land": biome not in ("sea", "lake"), "biome": biome})
    bounds = {k: meta[k] for k in ("lon0", "lon1", "lat0", "lat1")}
    return {"bounds": bounds, "hex_size_km": size, "layout": "pointy", "hexes": hexes}


def main() -> None:
    out = build(MAP_DIR)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(out, separators=(",", ":")))
    counts = {}
    for hx in out["hexes"]:
        counts[hx["biome"]] = counts.get(hx["biome"], 0) + 1
    print(f"wrote {OUT}: {len(out['hexes'])} hexes {dict(sorted(counts.items()))}")


if __name__ == "__main__":
    main()
