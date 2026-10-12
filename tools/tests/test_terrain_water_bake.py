"""The terrain water's baked mesh (unity/Assets/Textures/Map/terrain_water.bytes, issue #195) is for the shipped heightmap.

The game refuses a bake whose header does not match the heightmap it loaded (MapBuilder.TryReadWaterMesh: the resolution and
HeightsCheck, FNV-1a over every 997th height's float bits) and then derives the mesh at boot, which is slower. CI runs no
Unity, so this checks the same header here: a heightmap change without a re-bake (TerrainWaterBake.Bake) fails CI, as
localities_bake's does. Unity's SeaTests.TheBakedWaterMeshMatchesTheHeightmap compares the whole mesh."""
import json
import struct
from pathlib import Path

import numpy as np

import heightmap_codec as hc

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "unity/Assets/StreamingAssets/map"
BAKE = ROOT / "unity/Assets/Textures/Map/terrain_water.bytes"
MAGIC = 0x31574353   # "SCW1"


def heights_check(heights: np.ndarray) -> int:
    """MapBuilder.HeightsCheck over Unity's heights[y, x] (float32, y = 0 at the south)."""
    bits = heights.astype(np.float32).ravel()[::997].view(np.uint32)
    h = 2166136261
    for b in bits.tolist():
        for k in range(4):
            h = ((h ^ ((b >> (8 * k)) & 0xFF)) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def unity_heights() -> np.ndarray:
    """HeightmapLoader.ToUnityHeights: raw / 65535f, file row 0 (north) as the last row."""
    meta = json.loads((MAP / "heightmap.json").read_text())
    raw = hc.decode((MAP / "heightmap.r16z").read_bytes(), meta["width"], meta["height"])
    return (raw.astype(np.float32) / np.float32(65535.0))[::-1]


def test_the_terrain_water_bake_is_for_the_shipped_heightmap():
    magic, res, check, nverts, nindices = struct.unpack_from("<5i", BAKE.read_bytes())
    heights = unity_heights()
    assert magic == MAGIC
    assert res == heights.shape[1]
    assert BAKE.stat().st_size == 20 + 4 * (nverts + nindices)
    assert check == heights_check(heights), "stale: re-run Statecraft.Game.EditorTools.TerrainWaterBake.Bake"


def test_heights_check_reads_every_997th_sample():
    h = np.zeros((40, 50), np.float32)
    base = heights_check(h)
    h[997 // 50, 997 % 50] = 0.5
    assert heights_check(h) != base
    h[997 // 50, 997 % 50] = 0.0
    h[0, 1] = 0.5
    assert heights_check(h) == base
