using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class MapActionUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly HexFile Hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
    static readonly System.Collections.Generic.List<Settlement> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void HexPickerAgreesWithTheToolsSettlementRule()
    {
        // tools/settlement_hexes.py moves a settlement off a water hex to its nearest land neighbour (Tiberias); every
        // other settlement must land on exactly the hex the C# port computes.
        foreach (var t in Towns)
        {
            var picked = HexPicker.FromLonLat(t.Lon, t.Lat, Hexes.bounds, Hexes.hex_size_km);
            var owner = C.SettlementHexes.Single(s => s.Id == t.Id);
            if (t.Id == "tiberias") { Assert.AreNotEqual((owner.Q, owner.R), picked); continue; }
            Assert.AreEqual((owner.Q, owner.R), picked, t.Id);
        }
    }

    [Test]
    public void GeoFrameRoundTripsLonLat()
    {
        var f = new GeoFrame(34.2, 35.9, 29.45, 33.35, 161530f, 431220f);
        var (lon, lat) = f.ToLonLat(f.ToWorld(34.78, 32.08, 0));
        Assert.AreEqual(34.78, lon, 1e-5); Assert.AreEqual(32.08, lat, 1e-5);
    }

    static SimBridge At(DateTime date)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < date) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g, Text);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge) =>
        new InfoPanelPresenter(ui.Root, bridge, Text, Towns.ToDictionary(t => t.Id, t => t.Name));

    [Test]
    public void BeforeTheMaabarotTheButtonsSayWhy()
    {
        using var ui = new UiTestKit();
        var info = Panel(ui, At(new DateTime(1949, 6, 1)));
        Assert.AreEqual("Click the map to inspect a hex", ui.Q<Label>("info-title").text);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text);
        StringAssert.StartsWith("Coastal plain · hex (", ui.Q<Label>("info-subtitle").text);
        Assert.IsFalse(ui.Q<Button>("build-camp").enabledSelf);
        Assert.AreEqual("Transit camps and housing projects can be placed from 1950-01-01", ui.Q<Label>("info-reason").text);
    }

    [Test]
    public void ATransitCampIsOrderedAndBuiltAtEndTurn()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));                        // after the ma'abarot decision (May 1950)
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        Assert.IsTrue(ui.Q<Button>("build-camp").enabledSelf, ui.Q<Label>("info-reason").text);
        Assert.AreEqual("Place a transit camp (1.5m)", ui.Q<Button>("build-camp").text);
        ui.Press("build-camp");
        Assert.AreEqual("Ordered: A transit camp, at End Turn", ui.Q<Label>("info-placement").text);
        bridge.EndTurn();
        Assert.AreEqual(MapActions.TransitCamp, MapActions.At(bridge.State, tlv.Q, tlv.R)!.Action);
        Assert.AreEqual("A transit camp built here in August 1950", ui.Q<Label>("info-placement").text);
    }

    [Test]
    public void TheMinimapPaintsLandAndSeaNorthUp()
    {
        var tex = MinimapPresenter.Paint(Hexes, 128, 342);
        try
        {
            // RGB24 stores 8 bits per channel, so compare within one step.
            void Is(Color expected, double lon, double lat)
            {
                var c = tex.GetPixel((int)((lon - 34.2) / 1.7 * 128), (int)((lat - 29.45) / 3.9 * 342));
                Assert.AreEqual(expected.r, c.r, 1f / 255); Assert.AreEqual(expected.g, c.g, 1f / 255); Assert.AreEqual(expected.b, c.b, 1f / 255);
            }
            Is(MapBuilder.BiomeColours["coastal_plain"], 34.80, 32.10);   // just inland of Tel Aviv
            Is(MinimapPresenter.Sea, 34.30, 32.60);                       // the Mediterranean
            Is(MapBuilder.BiomeColours["desert"], 34.90, 30.50);          // the Negev
        }
        finally { UnityEngine.Object.DestroyImmediate(tex); }
    }

    [Test]
    public void PlacementsAreDrawnOnTheirHexes()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
            var map = new GameObject("MapScene").AddComponent<MapScene>();
            map.Configure(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexOverlay.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"));
            map.Build();
            var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
            var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
            layer.Render(new[] { new Placement(MapActions.TransitCamp, tlv.Q, tlv.R, new DateTime(1950, 8, 26)) }, C, map);
            layer.Render(new[] { new Placement(MapActions.Housing, tlv.Q, tlv.R, new DateTime(1950, 8, 26)) }, C, map);
            Assert.AreEqual(1, layer.transform.childCount);                 // Render replaces what it drew
            var box = layer.transform.GetChild(0);
            Assert.AreEqual($"housing_{tlv.Q}_{tlv.R}", box.name);
            var hex = C.HexAt(tlv.Q, tlv.R)!;
            var expected = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
            Assert.AreEqual(expected.x, box.position.x, 1f); Assert.AreEqual(expected.z, box.position.z, 1f);
            Assert.AreEqual((tlv.Q, tlv.R), MapPointer.Pick(map, box.position));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }

    [Test]
    public void TheInfoPanelShowsNoTileYields()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        var shown = string.Join(" | ", new[] { "info-title", "info-subtitle", "info-settlements", "info-placement", "info-reason" }.Select(n => ui.Q<Label>(n).text)).ToLowerInvariant();
        foreach (var word in new[] { "yield", "food", "production", "gold", "income" }) StringAssert.DoesNotContain(word, shown);
    }

    [Test]
    public void ThePerTurnCapAndRefusalsComeFromTheSim()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        new ToastPresenter(ui.Root, bridge, Text);
        var info = Panel(ui, bridge);
        var hexes = C.SettlementHexes.Select(h => (h.Q, h.R)).Distinct().Take(3).ToList();
        foreach (var (q, r) in hexes) { info.Show(q, r); ui.Press("build-housing"); }
        Assert.AreEqual(3, bridge.Queued.OfType<MapAction>().Count());      // the sim, not the UI, enforces the cap at End Turn
        bridge.EndTurn();
        Assert.AreEqual(1, bridge.Session.Last!.Rejected.Count);
        StringAssert.Contains($"No more than {C.Rules.MapActionsPerTurn} map actions in one turn", ui.Q<Label>("toast").text);
    }

    [Test]
    public void ABuiltHexCannotBeBuiltOnAgainAndSaysWhy()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        ui.Press("build-housing");
        bridge.EndTurn();
        Assert.IsFalse(ui.Q<Button>("build-housing").enabledSelf);
        Assert.AreEqual(MapActions.Check(C, bridge.State, MapActions.Housing, tlv.Q, tlv.R), ui.Q<Label>("info-reason").text);
    }

    [Test]
    public void TheMinimapClicksAndFramesTheView()
    {
        using var ui = new UiTestKit();
        using var mini = new MinimapPresenter(ui.Root, Hexes);
        (double lon, double lat)? clicked = null;
        mini.Clicked += (lon, lat) => clicked = (lon, lat);
        var (lon0, lat0) = mini.ToLonLat(0, 0);
        Assert.AreEqual(Hexes.bounds.lon0, lon0, 1e-9); Assert.AreEqual(Hexes.bounds.lat1, lat0, 1e-9);   // top-left is north-west
        var (lon1, lat1) = mini.ToLonLat(1, 1);
        Assert.AreEqual(Hexes.bounds.lon1, lon1, 1e-9); Assert.AreEqual(Hexes.bounds.lat0, lat1, 1e-9);
        var f = new GeoFrame(Hexes.bounds.lon0, Hexes.bounds.lon1, Hexes.bounds.lat0, Hexes.bounds.lat1, 161530f, 431220f);
        mini.SetView(f, f.ToWorld(34.95, 31.95, 0), 20000f);
        Assert.AreEqual(LengthUnit.Percent, ui.Q<VisualElement>("minimap-view").style.left.value.unit);
        Assert.IsNull(clicked);
    }

    [Test]
    public void ADisposedMinimapDestroysItsTextureAndADisposedPanelHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        info.Dispose();
        ui.Q<Label>("info-title").text = "untouched";
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("info-title").text);
        var mini = new MinimapPresenter(ui.Root, Hexes);
        var tex = mini.Texture;
        mini.Dispose();
        Assert.IsTrue(tex == null, "the texture is destroyed");
    }

    [Test]
    public void AClickOverTheUiNeverReachesTheMap()
    {
        var go = new GameObject("PointerTest");
        try
        {
            var pointer = go.AddComponent<MapPointer>();
            int hexClicks = 0;
            pointer.HexClicked += (q, r) => hexClicks++;
            pointer.IsOverUi = _ => true;
            Assert.IsFalse(pointer.HandleClick(new Vector2(10, 10)));
            Assert.AreEqual(0, hexClicks);
            pointer.IsOverUi = _ => false;
            Assert.IsFalse(pointer.HandleClick(new Vector2(10, 10)), "no map, camera or terrain: nothing is picked");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
