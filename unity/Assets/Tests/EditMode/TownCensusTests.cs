#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Towns round 2 (docs/visual-v1.md, "Towns round 2"): a census of what each town's kit places (buildings,
/// landmarks, the share of its land the buildings cover, heights and wall colours), logged as one CENSUS line per town.</summary>
public class TownCensusTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    internal sealed class Row
    {
        public string Id = ""; public int Buildings, Wings; public List<string> Landmarks = new List<string>();
        public float Radius, Coverage, CoreCoverage; public int LandCells, CoreCells;
        public float HMin, HMedian, HMax, HSpread; public int Colours; public Color MeanWall;
        public override string ToString() =>
            $"CENSUS {Id} R={Radius:0} buildings={Buildings} wings={Wings} landmarks={Landmarks.Count}[{string.Join(",", Landmarks)}] " +
            $"coverage={Coverage:P1} core={CoreCoverage:P1} land_cells={LandCells} h_min={HMin:0} h_med={HMedian:0} h_max={HMax:0} " +
            $"h_cv={HSpread:0.00} colours={Colours} wall=({MeanWall.r:0.00},{MeanWall.g:0.00},{MeanWall.b:0.00})";
    }

    /// <summary>The census of one build: per town, from SettlementKits.Build's list of what it placed. Coverage is the
    /// share of the town's own land (its footprint disc, on land, in its cell: the cells the kit may build in) under a
    /// building's or landmark's footprint, sampled every <paramref name="cellM"/> m; "core" is the inner half radius.</summary>
    internal static List<Row> Census(MapScene map, out List<SettlementKits.Placed> placed, float cellM = 100f) => Census(map, out placed, out _, cellM);

    internal static List<Row> Census(MapScene map, out List<SettlementKits.Placed> placed, out KitGround g, float cellM = 100f)
    {
        g = new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);
        // The corridors MapScene.Build gives the kits: the routes and the units' clearings.
        foreach (var c in map.RouteCorridors) g.AddCorridor(c.line, c.halfWidth);
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        // With the 1948 localities, as the map builds them (their landmarks claim their hearts before any town's houses).
        var localities = map.Localities.Select(p => p.Settlement).ToList();
        var built = SettlementKits.Build(map.Settlements, localities, new List<Localities.Area>(), g, out placed);
        foreach (var k in built.Towns) Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) Object.DestroyImmediate(t.mesh);
        var rows = new List<Row>();
        var towns = new HashSet<string>(map.Settlements.Select(t => t.Id));
        foreach (var s in map.Settlements)
        {
            var w = map.Frame.ToWorld(s.Lon, s.Lat, 0); var centre = new Vector2(w.x, w.z);
            float R = SettlementKits.FootprintRadiusM(s, g.HexSizeM);
            var mine = placed.Where(p => p.Id == s.Id && !Aggregate(p.Kind) && !p.InSea).ToList();
            var row = new Row { Id = s.Id, Radius = R, Buildings = mine.Count(p => p.Kind == "building"), Wings = mine.Count(p => p.Kind == "wing") };
            row.Landmarks = placed.Where(p => p.Id == s.Id && p.Kind != "building" && p.Kind != "wing").Select(p => p.Kind).ToList();
            int n = Mathf.CeilToInt(R / cellM);
            int covered = 0, coreCovered = 0;
            for (int j = -n; j <= n; j++)
                for (int i = -n; i <= n; i++)
                {
                    var p = centre + new Vector2(i, j) * cellM;
                    float d = (p - centre).magnitude / R;
                    if (d > 1f || g.Water(p.x, p.y)) continue;
                    bool own = true;
                    // Its cell among the game settlements (a town's houses ignore the 1948 localities' cells).
                    foreach (var f in g.Footprints) if (f.id != s.Id && towns.Contains(f.id) && (p - f.centre).magnitude / f.radius < d) { own = false; break; }
                    if (!own) continue;
                    bool hit = mine.Any(b => Inside(p, b));
                    row.LandCells++; if (hit) covered++;
                    if (d <= 0.5f) { row.CoreCells++; if (hit) coreCovered++; }
                }
            row.Coverage = covered / (float)Mathf.Max(1, row.LandCells);
            row.CoreCoverage = coreCovered / (float)Mathf.Max(1, row.CoreCells);
            var hs = mine.Where(p => p.Kind == "building").Select(p => p.H).OrderBy(h => h).ToList();
            if (hs.Count > 0)
            {
                row.HMin = hs[0]; row.HMax = hs[hs.Count - 1]; row.HMedian = hs[hs.Count / 2];
                float mean = hs.Average(); row.HSpread = Mathf.Sqrt(hs.Average(h => (h - mean) * (h - mean))) / mean;
                var walls = mine.Where(p => p.Kind == "building").Select(p => p.Wall).ToList();
                row.Colours = walls.Select(c => (c.r >> 4, c.g >> 4, c.b >> 4)).Distinct().Count();
                row.MeanWall = new Color((float)walls.Average(c => c.r) / 255f, (float)walls.Average(c => c.g) / 255f, (float)walls.Average(c => c.b) / 255f);
            }
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>A census entry that names a group (the Old City's walls, the German Colony) rather than one footprint.</summary>
    static bool Aggregate(string kind) => kind == "walls" || kind == "german_colony";

    static bool Inside(Vector2 p, SettlementKits.Placed b)
    {
        float a = b.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var d = p - b.C;
        float u = d.x * ca + d.y * sa, v = -d.x * sa + d.y * ca;
        return Mathf.Abs(u) <= b.W * 0.5f && Mathf.Abs(v) <= b.D * 0.5f;
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    [Test]
    public void TheCensusIsLogged()
    {
        var map = OpenMap();
        var rows = Census(map, out var placed);
        foreach (var r in rows) { TestContext.WriteLine(r); Debug.Log(r); }
        Debug.Log($"CENSUS total buildings={rows.Sum(r => r.Buildings)} landmarks={rows.Sum(r => r.Landmarks.Count)} placed={placed.Count} {map.KitStats}");
        Assert.AreEqual(map.Settlements.Count, rows.Count);
    }

    /// <summary>The same map draws the same towns: every placement, in order, with its size.</summary>
    [Test]
    public void EveryBuildPlacesTheSameTowns()
    {
        var map = OpenMap();
        Census(map, out var a); Census(map, out var b);
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.AreEqual(a[i].Id, b[i].Id, $"#{i}"); Assert.AreEqual(a[i].Kind, b[i].Kind, $"#{i}");
            Assert.AreEqual(a[i].C, b[i].C, $"#{i} {a[i].Id}"); Assert.AreEqual(a[i].W, b[i].W); Assert.AreEqual(a[i].D, b[i].D); Assert.AreEqual(a[i].H, b[i].H);
        }
    }

    /// <summary>Each city's landmarks are built (docs/visual-v1.md, "Towns round 2"); every other Arab or old town has its
    /// mosque, and every moshava, kibbutz and moshav its water tower.</summary>
    [Test]
    public void EveryTownHasItsLandmarks()
    {
        var map = OpenMap();
        Census(map, out var placed);
        var want = new Dictionary<string, string[]>
        {
            ["tel_aviv"] = new[] { "port", "warehouse" },
            ["jaffa"] = new[] { "mosque", "clock_tower", "church", "lighthouse", "port" },
            ["haifa"] = new[] { "port", "warehouse", "german_colony" },
            ["jerusalem"] = new[] { "walls", "gate", "citadel", "haram", "dome_of_the_rock", "al_aqsa" },
            ["ramla"] = new[] { "mosque", "white_tower" },
        };
        var missing = new List<string>();
        foreach (var s in map.Settlements)
        {
            var kinds = new HashSet<string>(placed.Where(p => p.Id == s.Id).Select(p => p.Kind));
            var expect = want.TryGetValue(s.Id, out var w) ? w
                : SettlementKits.ArchetypeOf(s) == Archetype.Moshava ? new[] { "water_tower" }
                : SettlementKits.ArchetypeOf(s) == Archetype.WhiteCity ? new string[0] : new[] { "mosque" };
            foreach (var k in expect) if (!kinds.Contains(k)) missing.Add($"{s.Id}: {k}");
        }
        CollectionAssert.IsEmpty(missing, "landmarks left out");
        Assert.GreaterOrEqual(placed.Count(p => p.Id == "jerusalem" && p.Kind == "gate"), 3, "most of the Old City's gates stand");
    }

    /// <summary>No building or landmark stands on a railway or a road (their corridors, RouteLayer.Corridors, and the units'
    /// clearings), and none stands in the sea: a building's centre and four corners are dry. Only harbour works (the
    /// breakwaters) stand in the water.</summary>
    [Test]
    public void NoBuildingStandsOnARouteOrInTheSea()
    {
        var map = OpenMap();
        Census(map, out var placed, out var g);
        var bad = new List<string>();
        foreach (var p in placed)
        {
            if (p.InSea || Aggregate(p.Kind)) continue;
            bool building = p.Kind == "building" || p.Kind == "wing";
            float reach = building ? 0.5f * Mathf.Sqrt(p.W * p.W + p.D * p.D) : 0.5f * Mathf.Min(p.W, p.D);
            if (p.Kind == "wing") reach = 0.5f * Mathf.Min(p.W, p.D);   // inside its block's plot, which was checked whole
            if (g.OnRoute(p.C, reach)) bad.Add($"{p.Id} {p.Kind} on a route at {p.C}");
            if (g.Water(p.C.x, p.C.y)) bad.Add($"{p.Id} {p.Kind} in the sea at {p.C}");
            if (!building) continue;
            float a = p.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            for (int i = 0; i < 4; i++)
            {
                float u = ((i & 1) == 0 ? -0.5f : 0.5f) * p.W, v = ((i & 2) == 0 ? -0.5f : 0.5f) * p.D;
                if (g.Water(p.C.x + u * ca - v * sa, p.C.y + u * sa + v * ca)) { bad.Add($"{p.Id} {p.Kind} corner in the sea at {p.C}"); break; }
            }
        }
        TestContext.WriteLine($"{placed.Count} placements checked");
        CollectionAssert.IsEmpty(bad.Take(20), $"{bad.Count} placements on a route or in the sea");
    }

    /// <summary>The floors of the four cities' buildings and built cover (share of the town's own land under a building or
    /// landmark, whole footprint and inner half radius). Towns round 2 measured them with the 1948 localities (docs/visual-v1.md,
    /// "Towns round 2"): Tel Aviv 43 / 17.4% / 18.5%, Jaffa 33 / 32.6% / 31.9%, Haifa 45 / 21.0% / 20.6%, Jerusalem 110 /
    /// 22.7% / 24.9%; main before it had 36, 15, 30 and 70 buildings. The building floors sit above main's counts, so a change
    /// that thins a city back to round 1's sparse handful fails here; the cover floors are about 10% under the measure.</summary>
    internal static readonly Dictionary<string, (int buildings, float all, float core)> Floors = new Dictionary<string, (int, float, float)>
    {
        ["tel_aviv"] = (40, 0.155f, 0.165f), ["jaffa"] = (28, 0.29f, 0.285f), ["haifa"] = (40, 0.19f, 0.185f), ["jerusalem"] = (95, 0.20f, 0.22f),
    };

    [Test]
    public void TheCitiesCoverTheirGround()
    {
        var map = OpenMap();
        var rows = Census(map, out _).ToDictionary(r => r.Id);
        foreach (var kv in Floors)
        {
            var r = rows[kv.Key];
            TestContext.WriteLine(r);
            Assert.GreaterOrEqual(r.Buildings, kv.Value.buildings, kv.Key + ": buildings");
            Assert.GreaterOrEqual(r.Coverage, kv.Value.all, kv.Key + ": built cover of the footprint");
            Assert.GreaterOrEqual(r.CoreCoverage, kv.Value.core, kv.Key + ": built cover of the inner half");
        }
    }
}
