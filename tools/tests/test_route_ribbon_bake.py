"""The route ribbons' bake (unity/Assets/Resources/Routes/RouteRibbons.bytes, issue #321) is for the shipped map and routes.

The game refuses a bake whose header does not match what it loaded (RouteRibbonFile.TryRead: the heightmap's resolution and
HeightsCheck, FNV-1a checks of heightmap.json and route_paths_1948.yaml, the terrain, RouteLayer's geometry constants and
the bake's version) and then derives the ribbons at boot, which is slower. CI runs no Unity, so this checks the same
header here, with the constants read from the C# source: a change to any of them without a re-bake
(RouteRibbonBake.Bake) fails CI, as the water's bake does. It also walks the meshes and their route index (route id, vertex
and index range) for the GDD Phase 4 corridors (#384). Unity's RouteTests.TheBakedRibbonsMatchTheRoutes compares the
whole meshes."""
import codecs
import re
import struct
from pathlib import Path

import numpy as np
import yaml

from test_terrain_water_bake import MAP, heights_check, unity_heights

ROOT = Path(__file__).resolve().parents[2]
BAKE = ROOT / "unity/Assets/Resources/Routes/RouteRibbons.bytes"
ROUTES = ROOT / "content/map/route_paths_1948.yaml"
SCRIPTS = ROOT / "unity/Assets/Scripts/Map"
MAGIC = 0x31524353   # "SCR1"
REBAKE = "run Statecraft.Game.EditorTools.RouteRibbonBake.Bake in Unity (docs/routes.md, Baked ribbons)"

# RouteRibbonFile.Floats and .Ints, in the header's order, with the file that defines each.
FLOATS = [("Routes/RouteLayer.cs", n) for n in ("RailWidthM", "RoadWidthM", "TrackWidthM", "StepM", "ClearanceM",
          "FallToleranceM", "RiseToleranceM", "MinWidthShare", "HalfWidthStepShare", "RailSpanShare", "RailClearanceM")]
FLOATS += [("MapBuilder.cs", "VerticalExaggeration")]
INTS = [("Routes/RouteLayer.cs", n) for n in ("Across", "NarrowSamples", "LiftSamples")]


def fnv1a(data: bytes) -> int:
    """RouteRibbonFile.Check: FNV-1a over the bytes, as a signed int32."""
    h = 2166136261
    for b in data:
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def text_check(path: Path) -> int:
    """Check over the file as Unity reads it: File.ReadAllText drops a UTF-8 BOM."""
    return fnv1a(path.read_bytes().removeprefix(codecs.BOM_UTF8))


def csharp_const(rel: str, name: str) -> str:
    m = re.search(rf"\b{name}\s*=\s*([0-9]+(?:\.[0-9]+)?)f?\b", (SCRIPTS / rel).read_text())
    assert m, f"{name} not found in {rel}"
    return m.group(1)


def bake_version() -> int:
    return int(csharp_const("Routes/RouteRibbonFile.cs", "Version"))


class Reader:
    def __init__(self, data: bytes):
        self.data, self.at = data, 0

    def take(self, fmt: str):
        size = struct.calcsize(fmt)
        assert self.at + size <= len(self.data), "truncated: " + REBAKE
        values = struct.unpack_from(fmt, self.data, self.at)
        self.at += size
        return values

    def int(self) -> int:
        return self.take("<i")[0]

    def text(self) -> str:
        n = self.int()
        return self.take(f"<{n}s")[0].decode("utf-8")


def read_header(r: Reader) -> dict:
    head = dict(zip(("magic", "version", "res", "heights", "map", "routes"), r.take("<6i")))
    head["terrain"] = r.take("<6f")
    head["floats"] = r.take(f"<{len(FLOATS)}f")
    head["ints"] = r.take(f"<{len(INTS)}i")
    head["meshes"] = r.int()
    return head


def bake_bytes() -> bytes:
    assert BAKE.exists(), f"{BAKE.relative_to(ROOT)} is missing: {REBAKE}"
    return BAKE.read_bytes()


def test_the_route_ribbon_bake_is_for_the_shipped_map_and_routes():
    head = read_header(Reader(bake_bytes()))
    heights = unity_heights()
    assert head["magic"] == MAGIC
    assert head["version"] == bake_version(), "made by another RouteRibbonFile.Version: " + REBAKE
    assert head["res"] == heights.shape[1]
    assert head["heights"] == heights_check(heights), "stale (heightmap): " + REBAKE
    assert head["map"] == text_check(MAP / "heightmap.json"), "stale (heightmap.json): " + REBAKE
    assert head["routes"] == text_check(ROUTES), "stale (route_paths_1948.yaml): " + REBAKE
    want = [np.float32(float(csharp_const(f, n))) for f, n in FLOATS]
    got = [np.float32(v) for v in head["floats"]]
    changed = [n for (_, n), a, b in zip(FLOATS, want, got) if a != b]
    changed += [n for (f, n), v in zip(INTS, head["ints"]) if int(csharp_const(f, n)) != v]
    assert not changed, f"stale (constants {', '.join(changed)}): " + REBAKE


def test_every_route_is_in_the_index_once_and_its_ranges_tile_its_mesh():
    r = Reader(bake_bytes())
    head = read_header(r)
    ids = []
    for _ in range(head["meshes"]):
        name = r.text()
        nv, ni, nr = r.take("<3i")
        vertex = index = 0
        for _ in range(nr):
            rid = r.text()
            first_v, count_v, first_i, count_i = r.take("<4i")
            assert (first_v, first_i) == (vertex, index), f"{name}: {rid} does not follow the route before it"
            assert count_i % 3 == 0, f"{name}: {rid}"
            vertex, index = first_v + count_v, first_i + count_i
            ids.append(rid)
        assert (vertex, index) == (nv, ni), f"{name}: the routes do not cover the mesh"
        r.take(f"<{8 * nv}f")
        indices = np.array(r.take(f"<{ni}i"), dtype=np.int64)
        assert ni == 0 or (indices.min() >= 0 and indices.max() < nv), name
    assert r.at == len(r.data), "longer than its meshes: " + REBAKE
    shipped = [route["id"] for route in yaml.safe_load(ROUTES.read_text(encoding="utf-8"))["routes"]]
    assert sorted(ids) == sorted(shipped), "stale (route ids): " + REBAKE


def test_the_check_is_fnv1a():
    assert fnv1a(b"") == struct.unpack("<i", struct.pack("<I", 2166136261))[0]
    assert fnv1a(b"a") == struct.unpack("<i", struct.pack("<I", 0xE40C292C))[0]   # the FNV-1a 32-bit test vector
    assert fnv1a(b"ab") != fnv1a(b"ba")
