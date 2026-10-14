"""The biome colours' bake (unity/Assets/Resources/Map/BiomeColours.bytes, issue #321) is for the shipped map.

The game refuses a bake whose header does not match what it loaded (BiomeColourFile.TryRead: the bake's version, the
texture's resolution, and BiomeColourFile.Check, FNV-1a over the frame, the hex size, the blend radius, the palette and
every hex's centre and biome) and then blends the colours at boot, which is slower. CI runs no Unity, so this checks the
same header here, with the constants read from the C# source: a change to hexes.json, the heightmap's bounds, the palette
or the blend radius without a re-bake (BiomeColourBake.Bake) fails CI, as the water's and the route ribbons' bakes do.
Unity's BiomeColourBakeTests.TheBakedBiomeColoursAreTheOnesTheMapWouldBlend compares every texel."""
import json
import re
import struct
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
BAKE = ROOT / "unity/Assets/Resources/Map/BiomeColours.bytes"
MAP = ROOT / "unity/Assets/StreamingAssets/map"
HEXES = ROOT / "content/map/hexes.json"
SCRIPTS = ROOT / "unity/Assets/Scripts/Map"
MAGIC = 0x31424353   # "SCB1"
REBAKE = "run Statecraft.Game.EditorTools.BiomeColourBake.Bake in Unity (BiomeColourFile)"


def source(name: str) -> str:
    return (SCRIPTS / name).read_text()


def const(name: str, rel: str) -> str:
    m = re.search(rf"\b{name}\s*=\s*([0-9]+(?:\.[0-9]+)?)f?\b", source(rel))
    assert m, f"{name} not found in {rel}"
    return m.group(1)


def f32(v) -> np.float32:
    return np.float32(v)


def palette() -> list[tuple[np.float32, ...]]:
    """MapPalette.Ground in MapBuilder.Biome's key order: Lit(r, g, b) = min(c * Brightness, MaxChannel), alpha 1."""
    order = re.findall(r'\["(\w+)"\]\s*=\s*new Color', source("MapBuilder.cs"))
    lit = dict(re.findall(r'\["(\w+)"\]\s*=\s*Lit\(([^)]*)\)', source("MapPalette.cs")))
    assert set(order) == set(lit), (order, lit)
    bright, top = f32(const("Brightness", "MapPalette.cs")), f32(const("MaxChannel", "MapPalette.cs"))
    out = []
    for name in order:
        rgb = [f32(v.strip().rstrip("f")) for v in lit[name].split(",")]
        out.append(tuple(min(f32(c * bright), top) for c in rgb) + (f32(1.0),))
    return out


def check() -> int:
    """BiomeColourFile.Check, as a signed int32."""
    h = 2166136261

    def bits(v: int):
        nonlocal h
        v &= 0xFFFFFFFFFFFFFFFF
        for k in range(8):
            h = ((h ^ ((v >> (8 * k)) & 0xFF)) * 16777619) & 0xFFFFFFFF

    def single(x: np.float32):
        bits(struct.unpack("<i", struct.pack("<f", float(x)))[0])

    def double(x: float):
        bits(struct.unpack("<q", struct.pack("<d", x))[0])

    meta = json.loads((MAP / "heightmap.json").read_text())
    hexes = json.loads(HEXES.read_text())
    bits(int(const("BiomeTextureResolution", "MapBuilder.cs")))
    for k in ("lon0", "lon1", "lat0", "lat1"):
        double(float(meta[k]))
    single(f32(f32(meta["width_km"]) * f32(1000)))       # MapBuilder.TerrainSize: width_km * 1000f
    single(f32(f32(meta["length_km"]) * f32(1000)))
    single(f32(hexes["hex_size_km"]))
    single(f32(const("BlendRadiusHexes", "BiomeBlender.cs")))
    for colour in palette():
        for c in colour:
            single(c)
    for hx in hexes["hexes"]:
        double(float(hx["lon"]))
        double(float(hx["lat"]))
        for ch in hx["biome"]:
            bits(ord(ch))
        bits(-1)
    return struct.unpack("<i", struct.pack("<I", h))[0]


def test_the_biome_colour_bake_is_for_the_shipped_map():
    data = BAKE.read_bytes()
    magic, version, res, baked = struct.unpack_from("<4i", data)
    assert magic == MAGIC, f"{BAKE.name} is not a biome colour bake"
    assert version == int(const("Version", "BiomeColourFile.cs")), f"made by another version: {REBAKE}"
    assert res == int(const("BiomeTextureResolution", "MapBuilder.cs")), f"made at {res} texels a side: {REBAKE}"
    assert len(data) == 16 + 3 * res * res, "truncated"
    assert baked == check(), f"made from other hexes, heightmap bounds, palette or blend radius: {REBAKE}"
