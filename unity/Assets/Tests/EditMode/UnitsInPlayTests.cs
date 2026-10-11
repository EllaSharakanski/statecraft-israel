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
/// sim's state, the placement clear of the towns and routes, the layer, the badge, the selection and its read-only card.</summary>
public class UnitsInPlayTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName));
    static List<UnitEntry> Units => UnitData.Parse(CommittedYaml);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static readonly DateTime LastArmistice = new DateTime(1949, 7, 20);

    static IEnumerable<string> Ids(IEnumerable<UnitToken> tokens) => tokens.Select(t => t.Unit.Id);

    static string Sha256(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
    }

    /// <summary>A copy of the new-game state at <paramref name="date"/> with the given security level and call-up (a
    /// fixture for the presentation; the sim never sees it).</summary>
    static GameState StateAt(DateTime date, int rung, double mobilization)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        s.Date = date; s.Rung = rung; s.Mobilization = mobilization;
        return s;
    }

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

    /// <summary>Review C1: every window ends for a reason. A source-based end cites its source and says which; any other
    /// end is the armistice of the anchor's front (the owner's "after the armistices, only what a source says continued").
    /// No window runs past its unit's sourced disbandment.</summary>
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
            foreach (var s in u.AllSources())
                Assert.LessOrEqual(UnitData.Day(s.Checked), new DateTime(2026, 10, 11), $"{u.Id}: {s.Url} checked in the future");
            foreach (var a in u.Anchors)
            {
                var at = $"{u.Id} {a.Place}";
                Assert.That(a.Lon, Is.InRange(lon0, lon1), at);
                Assert.That(a.Lat, Is.InRange(lat0, lat1), at);
                Assert.GreaterOrEqual(UnitData.Day(a.From), C.Rules.Start, at);
                Assert.IsNotEmpty(a.Sources, at);
                CollectionAssert.Contains(new[] { UnitData.EndBySource, UnitData.EndByFrontArmistice }, a.EndBasis, $"{at}: every window has an end_basis");
                if (a.EndBasis == UnitData.EndBySource)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(a.EndNote), $"{at}: a source-based end says which source");
                else
                    Assert.AreEqual(UnitData.FrontArmistice[a.Front], UnitData.Day(a.To), $"{at}: ends on its front's armistice");
                Assert.LessOrEqual(UnitData.Day(a.To), LastArmistice, at);
                if (u.Disbanded != null) Assert.LessOrEqual(UnitData.Day(a.To), UnitData.Day(u.Disbanded.Date), $"{at}: after its disbandment");
                Assert.AreNotEqual("15 May 1948", a.Since, $"{at}: the game's first day is not a sourced arrival");
            }
            for (int i = 1; i < u.Anchors.Count; i++)
                Assert.LessOrEqual(UnitData.Day(u.Anchors[i - 1].To), UnitData.Day(u.Anchors[i].From), $"{u.Id}: anchor windows in order, not overlapping");
        }
        Assert.AreEqual(new DateTime(1949, 2, 24), UnitData.FrontArmistice["egypt"]);
        Assert.AreEqual(new DateTime(1949, 3, 23), UnitData.FrontArmistice["lebanon"]);
        Assert.AreEqual(new DateTime(1949, 4, 3), UnitData.FrontArmistice["jordan"]);
        Assert.AreEqual(LastArmistice, UnitData.FrontArmistice["syria"]);
    }

    const string Anchor = "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}";
    const string Unit = "units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, anchors: [";

    [TestCase("units: []", "no units")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: cavalry, sector: s, status: draft, wikipedia_only: true, anchors: []}", "unknown service 'cavalry'")]
    [TestCase(Unit + "]}", "'a' has no anchors")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: []}]}", "has no source")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-06-01', to: '1948-05-15', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "to must be after from")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-5-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "from/to")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, sources: [{url: 'https://x.org'}]}]}", "checked")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "needs end_basis")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: ~, end_basis: source, end_note: n, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "to is empty")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, end_note: n, toward: {place: T, lon: 34.8, lat: 32.0, note: n, sources: []}, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "toward needs a note and a source")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1948-06-01', end_basis: source, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "no end_note")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1949-07-20', end_basis: front_armistice, front: egypt, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "must end on the egypt armistice, 1949-02-24")]
    [TestCase(Unit + "{place: P, lon: 34.8, lat: 32.0, from: '1948-05-15', to: '1949-02-24', end_basis: front_armistice, front: iraq, sources: [{url: 'https://x.org', checked: '2026-10-11'}]}]}", "needs a front")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, disbanded: {date: '1948-05-20', sources: [{url: 'https://x.org', checked: '2026-10-11'}]}, anchors: [" + Anchor + "]}", "runs past the unit's disbandment")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: army, sector: s, status: draft, wikipedia_only: true, headgear: [wool_cap, wool_cap, wool_cap], anchors: [" + Anchor + "]}", "headgear has no source")]
    [TestCase("units:\n- {id: a, name: A, number: '1', service: air, sector: s, status: draft, wikipedia_only: true, anchors: [" + Anchor + "]}", "without aircraft")]
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
        CollectionAssert.AreEqual(new[] { UnitData.WoolCap, UnitData.SteelHelmet, UnitData.WoolCap }, tokens.Single(t => t.Unit.Id == "negev").Headgear,
            "the Negev Brigade's headgear is sourced to its 1948 photographs");
        CollectionAssert.AreEqual(UnitData.DefaultHeadgear, tokens.Single(t => t.Unit.Id == "givati").Headgear, "no source: the uniform default");
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
        Assert.IsTrue(tokens.Where(t => t.Unit.Service == UnitData.Army).All(t => t.Figures == figures && t.Headgear.Count == figures));
        Assert.IsTrue(tokens.Where(t => t.Unit.Service != UnitData.Army).All(t => t.Figures == 0));
    }

    /// <summary>Review I3 and I4: the bases where two sources agree (no base from 30 May to mid-June, where they do not),
    /// and the Spitfire from its first sourced sortie, 15 October 1948.</summary>
    [Test]
    public void TheSquadronFliesInWarAndItsAircraftAndBaseFollowTheSources()
    {
        UnitToken Squadron(DateTime d, int rung) => UnitRoster.Build(Units, C.Rules, d, rung, 0.8).SingleOrDefault(t => t.Unit.Id == "squadron_101");
        var may = Squadron(new DateTime(1948, 5, 22), 5);
        Assert.AreEqual((UnitShowcase.S199Type, "Ekron (RAF Aqir)", true), (may.ArtType, may.Anchor.Place, may.Flying));
        Assert.IsNull(Squadron(new DateTime(1948, 6, 5), 5), "the sources disagree on its base from 30 May to mid-June");
        Assert.IsTrue(Squadron(new DateTime(1948, 7, 1), 4).Flying);
        Assert.IsFalse(Squadron(new DateTime(1948, 7, 1), 3).Flying, "below security level 4 it stands parked");
        Assert.AreEqual("Herzliya strip", Squadron(new DateTime(1948, 7, 1), 5).Anchor.Place);
        Assert.AreEqual(UnitShowcase.S199Type, Squadron(new DateTime(1948, 10, 14), 5).ArtType);
        var oct = Squadron(new DateTime(1948, 10, 16), 5);
        Assert.AreEqual((UnitShowcase.SpitfireType, "Herzliya strip"), (oct.ArtType, oct.Anchor.Place),
            "the Spitfire from 15 October 1948; Modi Alon landed at Herzliya on 16 October");
        Assert.AreEqual("Hatzor", Squadron(new DateTime(1948, 11, 21), 5).Anchor.Place, "Hatzor from 21 November 1948 (Eduard p. 54, he.wiki)");
        Assert.IsNull(Squadron(new DateTime(1949, 6, 1), 2), "it left Hatzor in June 1949");
    }

    [Test]
    public void AfterTheArmisticesOnlyWhatASourceSaysContinuedShows()
    {
        CollectionAssert.AreEqual(new[] { "kiryati", "etzioni", "squadron_101" }, Ids(UnitRoster.Build(Units, C.Rules, new DateTime(1949, 2, 24), 4, 0.4)),
            "the southern brigades and the ships end on the Egypt armistice");
        CollectionAssert.AreEqual(new[] { "squadron_101" }, Ids(UnitRoster.Build(Units, C.Rules, new DateTime(1949, 4, 3), 2, 0.05)),
            "the central and Jerusalem brigades end on the Jordan armistice");
        Assert.IsEmpty(UnitRoster.Build(Units, C.Rules, LastArmistice, 2, 0.05));
        Assert.IsEmpty(UnitRoster.Build(Units, C.Rules, new DateTime(1951, 1, 1), 1, 0.02));
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
        Debug.Log("UNITS roster golden " + Sha256(all) + "\n" + all);
        Assert.AreEqual(RosterGolden1950, Sha256(all), "the roster of the historical playthrough changed (see the log for the turns)");
    }

    /// <summary>Review M4: the historical run's turns are monthly or quarterly in places, so a weekly sweep of the war (at
    /// full war and full call-up) pins every window too.</summary>
    [Test]
    public void AWeeklySweepOfTheWarIsPinned()
    {
        var units = Units;
        var lines = new List<string>();
        for (var d = C.Rules.Start; d < LastArmistice; d = d.AddDays(7))
            lines.Add($"{d:yyyy-MM-dd} {string.Join(",", UnitRoster.Build(units, C.Rules, d, 5, 0.8))}");
        var all = string.Join("\n", lines);
        Debug.Log("UNITS weekly golden " + Sha256(all) + "\n" + all);
        Assert.AreEqual(WeeklyGolden, Sha256(all));
    }

    const string RosterGolden1950 = "f1efa90389934c3c088dc9ea3e6639d87e67b5937d894f3a3686b38cd1f5b35e";
    const string WeeklyGolden = "d2216af24b56e44179eb0b8aca601a93e6a98ce9a2db764e5b026a4ff1e02103";

    // ---- Placement, the layer and the card (a built map) ---------------------------------------------------------

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("BorderLand"));
        // The towns' footprints as the game sizes them (its kits read the 1945 populations), whatever an earlier test left.
        Statecraft.Game.Map.Kits.KitPopulation.Use(Statecraft.Game.Map.Kits.KitPopulation.Parse(
            File.ReadAllText(Path.Combine(Paths.Content, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    /// <summary>Review I2 and M9: every anchor gets one spot, from the data alone, within one hex step of its sourced
    /// place, clear of the towns' footprints and, for the figures, of the railways and roads by the full clearance; no two
    /// anchors whose windows overlap share a hex. Each spot and its distance is logged.</summary>
    [Test]
    public void EveryAnchorStandsWithinOneHexClearOfTheTownsAndTheRoutes()
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
            var anchors = units.Where(u => u.Hidden == null).SelectMany(u => u.Anchors.Select(a => (u, a))).ToList();
            var log = new List<string>();
            foreach (var (u, a) in anchors)
            {
                var at = $"{u.Id} {a.Place}";
                Assert.IsTrue(layer.Spots.TryGetValue(a, out var s), $"{at}: no spot");
                var w = map.Frame.ToWorld(a.Lon, a.Lat, 0);
                var c = new Vector2((float)s.X, (float)s.Z);
                float km = (c - new Vector2(w.x, w.z)).magnitude / 1000f;
                log.Add($"{at} -> {s} {km:0.0} km");
                Assert.LessOrEqual(km * 1000f, Mathf.Sqrt(3f) * hex + 1f, $"{at}: more than one hex step from its anchor");
                Assert.AreEqual(u.Service != UnitData.Navy, hexAt[(s.Q, s.R)].Land, $"{at}: land for the army and the air force, sea for ships");
                if (u.Service == UnitData.Navy) continue;
                if (s.InTown)
                {
                    Assert.IsTrue(s.Compact, at);
                    CollectionAssert.Contains(map.UnitClearings, c, $"{at}: a token in a town has the kits' clearing, so it stands in no building");
                }
                else
                {
                    float disc = UnitShowcase.RingRadius * hex * (s.Compact ? UnitGround.CompactScale : 1f);
                    foreach (var (fc, fr) in map.SettlementFootprints)
                        Assert.GreaterOrEqual((fc - c).magnitude, fr * UnitGround.FootprintMargin + disc, $"{at}: the ring reaches a town's buildings");
                }
                var points = u.Service == UnitData.Army ? layer.PlacementGround.FigurePoints(c, s.Compact) : new[] { c };
                foreach (var f in points)
                    foreach (var (line, half) in map.RouteCorridors)
                        for (int i = 0; i + 1 < line.Count; i++)
                            Assert.GreaterOrEqual(UnitGround.DistanceToSegment(f, line[i], line[i + 1]), half + UnitGround.FigureClearanceM, $"{at}: a figure stands on a railway or road");
            }
            for (int i = 0; i < anchors.Count; i++)
                for (int j = i + 1; j < anchors.Count; j++)
                {
                    var (a, b) = (anchors[i].a, anchors[j].a);
                    bool overlap = UnitData.Day(a.From) < UnitData.Day(b.To) && UnitData.Day(b.From) < UnitData.Day(a.To);
                    if (overlap) Assert.AreNotEqual((layer.Spots[a].Q, layer.Spots[a].R), (layer.Spots[b].Q, layer.Spots[b].R), $"{anchors[i].u.Id} and {anchors[j].u.Id} share a hex");
                }
            Debug.Log("UNITS spots\n" + string.Join("\n", log));
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheTrioIsSpreadSoEachFigureReadsOnItsOwn()
    {
        float step = 5000f * UnitShowcase.TrioSpread;   // a 5 km hex
        var o = UnitShowcase.FigureOffsets(3);
        for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 3; j++)
                Assert.GreaterOrEqual((o[i] - o[j]).magnitude * step, 1800f, "a figure is about 1.2 km wide at 2,000x: keep a gap");
        Assert.Less(o.Max(v => v.magnitude) * step + 600f, UnitShowcase.RingRadius * 5000f, "the figures stay inside the ring");
    }

    [Test]
    public void ScreenshotsShowUnitsOnlyWhenAsked()
    {
        Assert.IsTrue(UnitLayer.HiddenInCaptures("https://x/?golden&shot", new string[0], true), "plain checkpoints stay comparable");
        Assert.IsFalse(UnitLayer.HiddenInCaptures("https://x/?golden&shot&units", new string[0], true));
        Assert.IsFalse(UnitLayer.HiddenInCaptures("https://x/?vprofile", new string[0], true), "the frame profile prices them");
        Assert.IsFalse(UnitLayer.HiddenInCaptures("https://x/", new string[0], true), "normal play shows them");
        Assert.IsTrue(UnitLayer.HiddenInCaptures(null, new[] { "player", "-shot" }, false));
        Assert.IsFalse(UnitLayer.HiddenInCaptures(null, new[] { "player", "-shot", "-units" }, false));
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
            var kiryati = tokens.Find("kiryati");
            Assert.AreEqual(3, kiryati.Cast<Transform>().Count(t => t.name.StartsWith("Rifleman")), "three riflemen at full call-up");
            var badge = kiryati.Find("Badge");
            Assert.IsNotNull(badge, "the promo's badge over every token");
            Assert.AreEqual(2, badge.GetComponentsInChildren<MeshRenderer>().Length, "a plate and a glyph");
            Assert.Greater(badge.position.y, kiryati.Find("Rifleman0").position.y + UnitBadge.FigureHeightM * UnitShowcase.FigureScale, "above the figures");
            for (int i = 0; i < 3; i++)
            {
                var b = session.State.Hash();
                session.Step(HistoricalAI.Commands(C, session.State));
                var after = session.State.Hash();
                layer.Show(C, session.State);
                Assert.AreEqual(after, session.State.Hash(), $"turn {i}: drawing changed the state");
                Assert.AreNotEqual(b, after);
                if (i == 0) Assert.IsNotNull(layer.Tokens.FirstOrDefault(p => p.Token.Unit.Id == "squadron_101").Token, "101 Squadron from 20 May 1948");
            }
            CollectionAssert.Contains(VisualToggles.Names, UnitLayer.ToggleName);
            var any = layer.Tokens[0];
            VisualToggles.Set(UnitLayer.ToggleName, false);
            Assert.IsFalse(map.Terrain.transform.Find(UnitLayer.RootName).gameObject.activeSelf);
            Assert.IsNull(layer.UnitToSelectAt(any.Q, any.R), "a hidden unit cannot be selected (review M1)");
            VisualToggles.Set(UnitLayer.ToggleName, true);
            Assert.AreEqual(any.Token.Unit.Id, layer.UnitToSelectAt(any.Q, any.R));
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
        CollectionAssert.DoesNotContain(VisualToggles.Names, UnitLayer.ToggleName, "the map's teardown removes the switch");
    }

    /// <summary>Review I1: "since" only from a sourced start; never the game's first day.</summary>
    [Test]
    public void TheCardGivesASinceOnlyFromASourcedStart()
    {
        var text = Text;
        PlacedToken Token(string id, GameState s) => new PlacedToken(UnitRoster.Build(Units, C, s).Single(t => t.Unit.Id == id), default);
        var start = StateAt(C.Rules.Start, 5, 0.8);
        var kiryati = UnitCard.For(Token("kiryati", start), start, text);
        StringAssert.Contains("At Tel Aviv since February 1948", kiryati.Body);
        StringAssert.DoesNotContain("15 May 1948", kiryati.Body);
        var givati = UnitCard.For(Token("givati", start), start, text);
        StringAssert.Contains("At Gedera\n", givati.Body + "\n");
        StringAssert.DoesNotContain("since", givati.Body.Split('\n').First(l => l.StartsWith("At ")), "its move south is not dated");
        var latrun = StateAt(new DateTime(1948, 5, 29), 5, 0.8);
        StringAssert.Contains("At Latrun since 24 May 1948", UnitCard.For(Token("brigade_7", latrun), latrun, text).Body);
        var ship = UnitCard.For(Token("ins_haganah", start), start, text);
        StringAssert.Contains("commissioned 1948", ship.Subtitle);
        StringAssert.Contains("Wikipedia only", kiryati.Note);
        var hatzor = StateAt(new DateTime(1948, 12, 1), 5, 0.8);
        StringAssert.DoesNotContain("Wikipedia only", UnitCard.For(Token("squadron_101", hatzor), hatzor, text).Note,
            "Hatzor's position cites Eduard (review M8: the note is per position)");
        var july = StateAt(new DateTime(1948, 7, 1), 5, 0.8);
        StringAssert.Contains("Wikipedia only", UnitCard.For(Token("squadron_101", july), july, text).Note, "Herzliya's position is Wikipedia only");
    }

    [Test]
    public void ClickingAUnitSelectsItWithOneHighlightAndTheCardLinksToDefence()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            layer.Show(bridge.Content, bridge.State);
            var nav = new ScreenNav(ui.Root);
            var subdistricts = Subdistricts1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Subdistricts1948.FileName)));
            var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.Name);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, towns, nav: nav,
                selectedUnit: () => layer.Selected is string id ? layer.Tokens.First(p => p.Token.Unit.Id == id) : (PlacedToken?)null,
                subdistricts: subdistricts);
            var root = map.Terrain.transform.Find(UnitLayer.RootName);
            var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
            var tlv = C.SettlementHexes.Single(t => t.Id == "tel_aviv");
            info.Show(tlv.Q, tlv.R);
            Assert.IsNotEmpty(ui.Q<Label>("info-region").text, "a hex names its 1948 sub-district");
            map.Select(golani.Q, golani.R);   // MapPointer outlines the hex before it raises HexClicked
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            Assert.AreEqual("golani", layer.Selected);
            Assert.IsNotNull(root.Find("SelectionRing"), "the promo's white ring round the selected token");
            Assert.IsFalse(map.Terrain.transform.Find(MapScene.SelectionName).GetComponent<MeshRenderer>().enabled, "one highlight: the ring, not the hex outline (review M2)");
            Assert.IsTrue(info.ShowsUnit);
            Assert.AreEqual("", ui.Q<Label>("info-region").text, "a unit's card names no sub-district (review MF2)");
            var words = ui.Q<VisualElement>("info-panel").Query<Label>().ToList().Select(l => l.text).ToList();
            Assert.AreEqual("Golani Brigade", ui.Q<Label>("info-title").text);
            Assert.IsTrue(words.Any(w => w.Contains("At Yavne'el since February 1948")), string.Join(" | ", words));
            Assert.IsTrue(words.Any(w => w.Contains("Reserve call-up 80%") && w.Contains("security level 5")), string.Join(" | ", words));
            Assert.IsTrue(words.Any(w => w.Contains("Wikipedia only")), "a Wikipedia-only position says it is a draft");
            Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("build-camp").style.display.value, "no orders on a unit");
            var defence = ui.Q<Button>("info-defence");
            Assert.AreEqual(DisplayStyle.Flex, defence.style.display.value);
            UiTestKit.Press(defence);
            Assert.AreEqual("defence", nav.Current);
            GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
            Assert.IsNull(layer.Selected, "a second click shows the hex and its map actions");
            Assert.IsNull(root.Find("SelectionRing"));
            Assert.IsFalse(info.ShowsUnit);
            Assert.AreEqual(DisplayStyle.None, defence.style.display.value);
            info.Dispose(); nav.Dispose();
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Review M5: a selected unit that leaves the map on a turn is deselected and the card falls back to the hint.</summary>
    [Test]
    public void ASelectedUnitThatLeavesTheMapClearsItsCard()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), null,
                () => layer.Selected is string id ? layer.Tokens.First(p => p.Token.Unit.Id == id) : (PlacedToken?)null);
            layer.Show(C, StateAt(new DateTime(1948, 7, 12), 5, 0.8));
            var yiftach = layer.Tokens.Single(p => p.Token.Unit.Id == "yiftach");
            GameRoot.OnHexClicked(map, layer, info, yiftach.Q, yiftach.R);
            Assert.IsTrue(info.ShowsUnit);
            layer.Show(C, StateAt(new DateTime(1948, 7, 24), 5, 0.8));   // Operation Dani is over
            Assert.IsNull(layer.Selected);
            Assert.IsNull(map.Terrain.transform.Find(UnitLayer.RootName).Find("SelectionRing"));
            info.Render();
            Assert.IsFalse(info.ShowsUnit);
            Assert.AreEqual(Text["info.hint"], ui.Q<Label>("info-title").text);
            info.Dispose();
        }
        finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheS199IsRepeatableAndToScale()
    {
        var plain = UnitModels.AviaS199("a"); var again = UnitModels.AviaS199("b");
        var prop = UnitModels.S199Propeller("p");
        try
        {
            CollectionAssert.AreEqual(plain.vertices, again.vertices);
            CollectionAssert.AreEqual(plain.uv, again.uv);
            var b = plain.bounds;
            Assert.AreEqual(9.92f, b.size.x, 0.25f, "span (Avia S-199 specifications)");
            Assert.AreEqual(8.94f, b.size.z, 0.25f, "length");
            Assert.Less(plain.triangles.Length / 3 + prop.triangles.Length / 3, 6000);
            int Slot(Mesh m, int slot) => m.uv.Count(uv => Mathf.Abs(uv.x - UnitPalette.U(slot)) < 1e-4f);
            Assert.AreEqual(0, Slot(plain, UnitPalette.MarkingRed), "no Operation Yoav markings: the map shows the Spitfire from 15 October 1948");
            Assert.Greater(Slot(plain, UnitPalette.RoundelBlue), 0, "roundels under the wings");
            Assert.Greater(Slot(plain, UnitPalette.RoundelWhite), 0, "and the white rear-fuselage bands");
        }
        finally { foreach (var m in new[] { plain, again, prop }) UnityEngine.Object.DestroyImmediate(m); }
    }
}
