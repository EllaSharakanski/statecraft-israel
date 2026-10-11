using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The units in play (docs/units.md, "Units in play"): the sourced map-only data, the roster read from the
/// sim's state, the placement clear of the towns and routes, the layer, the selection and its read-only card.</summary>
public class UnitsInPlayTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName));
    static List<UnitEntry> Units => UnitData.Parse(CommittedYaml);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static readonly DateTime WarEnd = new DateTime(1949, 7, 20);

    static IEnumerable<string> Ids(IEnumerable<UnitToken> tokens) => tokens.Select(t => t.Unit.Id);

    // ---- The data -----------------------------------------------------------------------------------------------

    /// <summary>Map-only: the sim's content hash is the same with and without the unit file (so it cannot move the
    /// goldens), and the WebGL manifest ships it (the player reads it).</summary>
    [Test]
    public void TheUnitFileDoesNotMoveTheContentHash()
    {
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-nounits-" + Guid.NewGuid().ToString("N"));
        try
        {
            Statecraft.Game.Boot.ContentManifest.Copy(Paths.Content, copy);
            var file = Path.Combine(copy, "map", UnitData.DefaultFileName);
            Assert.IsTrue(File.Exists(file), "the manifest copies the unit file");
            File.Delete(file);
            Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, ContentLoader.Load(copy).Hash);
            CollectionAssert.Contains(Statecraft.Game.Boot.ContentManifest.Collect(Paths.Content), "map/" + UnitData.DefaultFileName);
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }
    }

    [Test]
    public void EveryRowIsSourcedDatedAndOnTheMap()
    {
        var units = Units;
        Assert.AreEqual(12, units.Count(u => u.Service == UnitData.Army), "the twelve brigades of 1948");
        Assert.AreEqual(1, units.Count(u => u.Service == UnitData.Air));
        Assert.AreEqual(2, units.Count(u => u.Service == UnitData.Navy));
        CollectionAssert.AreEquivalent(new[] { "carmeli", "alexandroni", "brigade_8", "oded" }, units.Where(u => u.Hidden != null).Select(u => u.Id),
            "owner ruling: the four brigades with no sourced position are listed and hidden");
        double lon0 = C.Hexes.Min(h => h.Lon), lon1 = C.Hexes.Max(h => h.Lon), lat0 = C.Hexes.Min(h => h.Lat), lat1 = C.Hexes.Max(h => h.Lat);
        foreach (var u in units)
        {
            Assert.AreEqual("draft", u.Status, u.Id);
            Assert.AreEqual(UnitData.IsWikipediaOnly(u), u.WikipediaOnly, $"{u.Id}: wikipedia_only must say whether every source is Wikipedia or Wikidata");
            Assert.IsNotEmpty(u.AllSources().ToList(), u.Id);
            foreach (var s in u.AllSources())
                Assert.LessOrEqual(UnitData.Day(s.Checked), new DateTime(2026, 10, 11), $"{u.Id}: {s.Url} checked in the future");
            foreach (var a in u.Anchors)
            {
                Assert.That(a.Lon, Is.InRange(lon0, lon1), $"{u.Id} {a.Place}");
                Assert.That(a.Lat, Is.InRange(lat0, lat1), $"{u.Id} {a.Place}");
                Assert.LessOrEqual(UnitData.Day(a.To), WarEnd, $"{u.Id} {a.Place}: no position after the armistices without a source");
                Assert.GreaterOrEqual(UnitData.Day(a.From), C.Rules.Start, $"{u.Id} {a.Place}");
            }
            for (int i = 1; i < u.Anchors.Count; i++)
                Assert.LessOrEqual(UnitData.Day(u.Anchors[i - 1].To), UnitData.Day(u.Anchors[i].From), $"{u.Id}: anchor windows in order, not overlapping");
        }
    }

    [TestCase("units: []", "no units")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: cavalry, sector: s, status: draft, wikipedia_only: true, anchors: []}", "unknown service 'cavalry'")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: []}", "'a' has no anchors")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: [{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', sources: []}]}", "has no source")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: [{place: P, lon: 34.8, lat: 32.0, from: '1948-06-01', to: '1948-05-15', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "to must be after from")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: [{place: P, lon: 34.8, lat: 32.0, from: '1948-5-15', to: '1948-06-01', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "from/to")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: [{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', sources: [{url: 'https://x.org'}]}]}", "checked")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: air, sector: s, status: draft, wikipedia_only: true, anchors: [{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "without aircraft")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, colour: red, sector: s, status: draft, wikipedia_only: true, anchors: []}", "colour")]
    public void BadUnitFilesNameTheFileAndTheProblem(string yaml, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => UnitData.Parse(yaml, "u.yaml"));
        StringAssert.StartsWith("u.yaml:", e.Message);
        StringAssert.Contains(expected, e.Message);
    }

    [Test]
    public void AMissingOrBrokenFileDrawsNoUnitsAndWarns()
    {
        var warnings = new List<string>();
        Assert.IsEmpty(UnitData.ReadOrNone("", warnings.Add));
        Assert.IsEmpty(UnitData.ReadOrNone("units: [", warnings.Add));
        Assert.AreEqual(2, warnings.Count);
    }

    // ---- The roster ----------------------------------------------------------------------------------------------

    [Test]
    public void ANewGameShowsTheSourcedUnitsOfMay1948()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var tokens = UnitRoster.Build(Units, C, s);
        CollectionAssert.AreEqual(new[] { "golani", "kiryati", "givati", "etzioni", "harel", "negev", "ins_haganah", "ins_wedgwood" }, Ids(tokens),
            "on 15 May 1948: no aircraft yet (101 Squadron from 20 May), the 7th Brigade from 24 May, Yiftach only in Operation Dani");
        Assert.AreEqual(5, s.Rung);
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Army).All(t => t.Figures == 3), "full war: call-up at least 0.8");
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Navy).All(t => t.ArtType == UnitShowcase.CorvetteType && t.Anchor.Place == "Haifa harbour"));
    }

    [TestCase(0.80, 3)]
    [TestCase(0.40, 3)]
    [TestCase(0.39, 2)]
    [TestCase(0.15, 2)]
    [TestCase(0.10, 1)]
    [TestCase(0.02, 1)]
    public void TheFiguresFollowTheCallUp(double mobilization, int figures)
    {
        var tokens = UnitRoster.Build(Units, C.Rules, new DateTime(1948, 6, 5), 5, mobilization);
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Army).All(t => t.Figures == figures));
        Assert.IsTrue(tokens.Where(t => t.Unit.Service != UnitData.Army).All(t => t.Figures == 0));
    }

    [Test]
    public void TheSquadronFliesInWarAndItsAircraftAndBaseFollowTheSources()
    {
        UnitToken Squadron(DateTime d, int rung) => UnitRoster.Build(Units, C.Rules, d, rung, 0.8).Single(t => t.Unit.Id == "squadron_101");
        var may = Squadron(new DateTime(1948, 5, 22), 5);
        Assert.AreEqual((UnitShowcase.S199Type, "Ekron (RAF Aqir)", true), (may.ArtType, may.Anchor.Place, may.Flying));
        Assert.IsTrue(Squadron(new DateTime(1948, 7, 1), 4).Flying);
        Assert.IsFalse(Squadron(new DateTime(1948, 7, 1), 3).Flying, "below security level 4 it stands parked");
        Assert.AreEqual("Herzliya strip", Squadron(new DateTime(1948, 7, 1), 5).Anchor.Place);
        Assert.AreEqual(UnitShowcase.S199YoavType, Squadron(new DateTime(1948, 10, 16), 5).ArtType, "Operation Yoav markings from 15 October 1948");
        Assert.AreEqual(UnitShowcase.S199Type, Squadron(new DateTime(1948, 10, 14), 5).ArtType);
        var spit = Squadron(new DateTime(1948, 10, 21), 5);
        Assert.AreEqual((UnitShowcase.SpitfireType, "Hatzor"), (spit.ArtType, spit.Anchor.Place), "the Spitfire from its sourced date, 21 October 1948");
    }

    [Test]
    public void AfterTheArmisticesOnlyWhatASourceSaysContinuedShows()
    {
        Assert.IsEmpty(UnitRoster.Build(Units, C.Rules, WarEnd, 2, 0.05), "no source gives a position after 20 July 1949");
        Assert.IsEmpty(UnitRoster.Build(Units, C.Rules, new DateTime(1951, 1, 1), 1, 0.02));
        Assert.IsNotEmpty(UnitRoster.Build(Units, C.Rules, WarEnd.AddDays(-1), 2, 0.05));
    }

    [Test]
    public void TheRosterNeverChangesTheStateAndTheGoldenHolds()
    {
        var units = Units;
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var digests = new List<string>();
        for (int turn = 0; turn < 1000 && s.Date < GoldenRun.Until1950 && s.GameOver == null; turn++)
        {
            var before = s.Hash();
            var tokens = UnitRoster.Build(units, C, s);
            Assert.AreEqual(before, s.Hash(), $"turn {turn}: building the roster changed the state");
            digests.Add($"{s.Date:yyyy-MM-dd} {string.Join(",", tokens)}");
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
        }
        Assert.AreEqual(GoldenRun.Hash1950, s.Hash(), "the golden does not move");
        // The presentation golden: the roster of every turn of the historical playthrough, pinned. A deliberate change to
        // the unit data or the roster rules updates it (and says why in the commit).
        var all = string.Join("\n", digests);
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(all)).Select(b => b.ToString("x2")));
        Debug.Log("UNITS roster golden " + hash + "\n" + all);
        Assert.AreEqual(RosterGolden1950, hash, "the roster of the historical playthrough changed (see the log for the turns)");
    }

    const string RosterGolden1950 = "94f32d7e7acf3ab116346b47633266bf41b1b6d73a84a8e9ad3a4bc6b1015874";

    // ---- Placement, the layer and the card (a built map) ---------------------------------------------------------

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("BorderLand"));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    [Test]
    public void EveryTokenOfTheWarStandsClearOfTheTownsAndTheRoutes()
    {
        var map = BuildMap();
        try
        {
            Assert.IsNotEmpty(map.SettlementFootprints);
            Assert.IsNotEmpty(map.RouteCorridors);
            var units = Units;
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            float hex = map.Hexes.hex_size_km * 1000f;
            var hexAt = C.Hexes.ToDictionary(h => (h.Q, h.R));
            int checkedTokens = 0;
            for (var d = C.Rules.Start; d < WarEnd; d = d.AddDays(7))
            {
                var tokens = UnitRoster.Build(units, C.Rules, d, 5, 0.8);
                var skipped = new List<string>();
                var placed = UnitPlacer.Place(tokens, C.Hexes, (lon, lat) => { var w = map.Frame.ToWorld(lon, lat, 0); return (w.x, w.z); }, layer.IsClear, skipped.Add);
                CollectionAssert.IsEmpty(skipped, $"{d:yyyy-MM-dd}: every sourced unit finds a clear hex");
                Assert.AreEqual(placed.Count, placed.Select(p => (p.Q, p.R)).Distinct().Count(), $"{d:yyyy-MM-dd}: one token per hex");
                foreach (var p in placed)
                {
                    var h = hexAt[(p.Q, p.R)];
                    Assert.AreEqual(p.Token.Unit.Service != UnitData.Navy, h.Land, $"{p}: land for the army and the air force, sea for ships");
                    var w = map.Frame.ToWorld(h.Lon, h.Lat, 0); var c = new Vector2(w.x, w.z);
                    foreach (var (fc, fr) in map.SettlementFootprints)
                        Assert.GreaterOrEqual((fc - c).magnitude, fr * UnitLayer.FootprintMargin + UnitShowcase.RingRadius * hex, $"{p}: the ring reaches a town's buildings");
                    if (p.Token.Unit.Service == UnitData.Army)
                        foreach (var o in UnitShowcase.FigureOffsets(3))
                        {
                            var f = c + o * UnitShowcase.TrioSpread * hex;
                            foreach (var (line, half) in map.RouteCorridors)
                                for (int i = 0; i + 1 < line.Count; i++)
                                    Assert.GreaterOrEqual(UnitLayer.DistanceToSegment(f, line[i], line[i + 1]), half, $"{p}: a figure stands on a railway or road");
                        }
                    var anchor = C.Hexes.OrderBy(x => { var a = map.Frame.ToWorld(x.Lon, x.Lat, 0); var b = map.Frame.ToWorld(p.Token.Anchor.Lon, p.Token.Anchor.Lat, 0); return (a - b).sqrMagnitude; }).First();
                    Assert.LessOrEqual(MapActions.Distance(anchor.Q, anchor.R, p.Q, p.R), UnitPlacer.MaxShift, $"{p}: within {UnitPlacer.MaxShift} hexes of its anchor");
                    checkedTokens++;
                }
            }
            Assert.Greater(checkedTokens, 300);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheLayerReadsTheStateNeverWritesItAndRebuildsOnlyOnChange()
    {
        var map = BuildMap();
        try
        {
            var session = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            var before = session.State.Hash();
            Assert.IsTrue(layer.Show(C, session.State));
            Assert.AreEqual(before, session.State.Hash(), "drawing the units changed the state");
            Assert.IsFalse(layer.Show(C, session.State), "the same roster draws nothing new");
            Assert.AreEqual(8, layer.Tokens.Count);
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            Assert.AreEqual(8, tokens.childCount);
            Assert.AreEqual(3, tokens.Find("kiryati").GetComponentsInChildren<MeshRenderer>().Length, "three riflemen at full call-up");
            for (int i = 0; i < 3; i++)
            {
                var b = session.State.Hash();
                session.Step(HistoricalAI.Commands(C, session.State));
                var after = session.State.Hash();
                layer.Show(C, session.State);
                Assert.AreEqual(after, session.State.Hash(), $"turn {i}: drawing changed the state");
                Assert.AreNotEqual(b, after);
            }
            Assert.IsNotNull(layer.Tokens.FirstOrDefault(p => p.Token.Unit.Id == "squadron_101").Token, "101 Squadron from 20 May 1948");
            CollectionAssert.Contains(VisualToggles.Names, UnitLayer.ToggleName);
            VisualToggles.Set(UnitLayer.ToggleName, false);
            Assert.IsFalse(map.Terrain.transform.Find(UnitLayer.RootName).gameObject.activeSelf);
            VisualToggles.Set(UnitLayer.ToggleName, true);
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
        CollectionAssert.DoesNotContain(VisualToggles.Names, UnitLayer.ToggleName, "the map's teardown removes the switch");
    }

    [Test]
    public void SelectingAUnitDrawsTheRingAndTheCardLinksToDefence()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            var nav = new ScreenNav(ui.Root);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), nav,
                () => layer.Selected is string id ? layer.Tokens.First(p => p.Token.Unit.Id == id) : (PlacedToken?)null);
            var root = map.Terrain.transform.Find(UnitLayer.RootName);
            var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
            Assert.AreEqual(golani, layer.At(golani.Q, golani.R));
            Assert.AreEqual("golani", layer.UnitToSelectAt(golani.Q, golani.R), "a click on its hex selects the unit");
            layer.Select("golani");
            Assert.IsNull(layer.UnitToSelectAt(golani.Q, golani.R), "a second click shows the hex and its map actions");
            info.ShowUnit();
            Assert.IsNotNull(root.Find("SelectionRing"), "the promo's white ring round the selected token");
            Assert.IsTrue(info.ShowsUnit);
            var words = ui.Q<VisualElement>("info-panel").Query<Label>().ToList().Select(l => l.text).ToList();
            Assert.AreEqual("Golani Brigade", ui.Q<Label>("info-title").text);
            Assert.IsTrue(words.Any(w => w.Contains("Yavne'el")), "where it stands");
            Assert.IsTrue(words.Any(w => w.Contains("Reserve call-up 80%") && w.Contains("security level 5")), string.Join(" | ", words));
            Assert.IsTrue(words.Any(w => w.Contains("Wikipedia only")), "a Wikipedia-only row says it is a draft");
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("build-camp").style.display.value, "no orders on a unit");
            var defence = ui.Q<Button>("info-defence");
            Assert.AreEqual(DisplayStyle.Flex, defence.style.display.value);
            UiTestKit.Press(defence);
            Assert.AreEqual("defence", nav.Current);
            layer.Select(null);
            Assert.IsNull(root.Find("SelectionRing"));
            info.Show(golani.Q, golani.R);
            Assert.IsFalse(info.ShowsUnit);
            Assert.AreEqual(DisplayStyle.None, defence.style.display.value);
            info.Dispose(); nav.Dispose();
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheS199IsRepeatableToScaleAndCarriesItsDatedMarkings()
    {
        var plain = UnitModels.AviaS199(false, "a"); var again = UnitModels.AviaS199(false, "b"); var yoav = UnitModels.AviaS199(true, "c");
        var prop = UnitModels.S199Propeller("p");
        try
        {
            CollectionAssert.AreEqual(plain.vertices, again.vertices);
            CollectionAssert.AreEqual(plain.uv, again.uv);
            var b = plain.bounds;
            Assert.AreEqual(9.92f, b.size.x, 0.25f, "span (Avia S-199 specifications)");
            Assert.AreEqual(8.94f, b.size.z, 0.25f, "length");
            Assert.Less(plain.triangles.Length / 3 + prop.triangles.Length / 3, 6000);
            int Red(Mesh m) => m.uv.Count(uv => Mathf.Abs(uv.x - UnitPalette.U(UnitPalette.MarkingRed)) < 1e-4f);
            int Blue(Mesh m) => m.uv.Count(uv => Mathf.Abs(uv.x - UnitPalette.U(UnitPalette.RoundelBlue)) < 1e-4f);
            Assert.AreEqual(0, Red(plain), "no red before October 1948");
            Assert.Greater(Red(yoav), 0, "red spinner and rudder stripes from 15 October 1948");
            Assert.Greater(Blue(plain), 0, "roundels under the wings");
            Assert.AreEqual(2 * Blue(plain), Blue(yoav), "and on top of the wings from October 1948");
        }
        finally { foreach (var m in new[] { plain, again, yoav, prop }) UnityEngine.Object.DestroyImmediate(m); }
    }
}
