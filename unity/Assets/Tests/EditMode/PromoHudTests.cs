#nullable enable
using System.IO;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The promo HUD pass (promo/final/03): the top bar's indicator icons, the round screen buttons and their lit
/// state, the event card's option signs, the info panel's terrain sign (real hex facts, no tile yields) and the panel
/// trim. Behaviour is unchanged; these check the new pieces are wired to the real elements and data.</summary>
public class PromoHudTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void EveryIndicatorHasItsIcon()
    {
        using var ui = new UiTestKit();
        foreach (var (meter, glyph) in new[] { ("meter-treasury", "coin"), ("meter-immigration", "people"), ("meter-security", "shield"),
                                               ("meter-support", "crowd"), ("meter-standing", "globe"), ("meter-fx", "banknote") })
        {
            var icon = ui.Q<VisualElement>(meter).Q<UiIcon>();
            Assert.IsNotNull(icon, meter);
            Assert.AreEqual(glyph, icon.Glyph, meter);
            CollectionAssert.Contains(UiIcon.Glyphs, glyph, meter + ": a glyph the icon can draw");
        }
    }

    [Test]
    public void EveryScreenButtonIsARoundIconWithItsName()
    {
        using var ui = new UiTestKit();
        foreach (var (button, name, glyph) in new[] { ("nav-cabinet", "Cabinet", "columns"), ("nav-budget", "Budget", "scales"),
                                                      ("nav-defence", "Defence", "helmet"), ("nav-why", "Why?", "question"), ("new-game", "New Game", "restart") })
        {
            var b = ui.Q<Button>(button);
            Assert.AreEqual(name, b.text, button + ": the name stays the button's text (legible, and what the event log records)");
            var disc = b.Q<UiIcon>(className: "nav-disc");
            Assert.IsNotNull(disc, button);
            Assert.AreEqual(glyph, disc.Glyph, button);
            Assert.AreEqual(PickingMode.Ignore, disc.pickingMode, button + ": a click on the disc reaches the button");
        }
    }

    [Test]
    public void TheOpenScreensButtonIsLit()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        try
        {
            Assert.IsFalse(ScreenNav.Screens.Any(s => ui.Q<Button>("nav-" + s).ClassListContains(ScreenNav.OpenClass)), "none lit at start");
            nav.Toggle("budget");
            foreach (var s in ScreenNav.Screens) Assert.AreEqual(s == "budget", ui.Q<Button>("nav-" + s).ClassListContains(ScreenNav.OpenClass), s);
            nav.Toggle("budget");
            Assert.IsFalse(ui.Q<Button>("nav-budget").ClassListContains(ScreenNav.OpenClass), "closing unlights it");
        }
        finally { nav.Dispose(); }
    }

    [Test]
    public void EachEventOptionCarriesItsSignAndStaysPressable()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var bridge = new SimBridge(g, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, null);
        var options = ui.Q<VisualElement>("event-options").Query<Button>().ToList();
        Assert.Greater(options.Count, 1);
        foreach (var b in options)
        {
            Assert.IsTrue(b.ClassListContains(EventCardPresenter.OptionClass), b.name);
            Assert.AreEqual(0, b.childCount, b.name + ": the sign is painted, so the button stays a leaf whose wrapped text is measured");
        }
        UiTestKit.Press(ui.Q<Button>("option-sign"));
        Assert.AreNotEqual("armistice_egypt", card.Shown?.Id, "the option still answers the event");
        card.Dispose();
    }

    [Test]
    public void TheInfoPanelShowsTheHexsTerrainSignAndNoYields()
    {
        using var ui = new UiTestKit();
        var towns = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, towns.ToDictionary(t => t.Id, t => t.Name));
        var thumb = ui.Q<UiIcon>("info-thumb");
        Assert.AreEqual(DisplayStyle.None, thumb.style.display.value, "no sign before a hex is chosen");
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        var biome = C.HexAt(tlv.Q, tlv.R)!.Biome;
        Assert.AreEqual(DisplayStyle.Flex, thumb.style.display.value);
        Assert.AreEqual("biome-" + biome, thumb.Glyph);
        CollectionAssert.Contains(UiIcon.Glyphs, thumb.Glyph);
        Assert.AreEqual(MapBuilder.BiomeColours[biome], thumb.style.backgroundColor.value, "the biome's own map colour");
        var words = ui.Q<VisualElement>("info-panel").Query<Label>().ToList().Select(l => l.text).ToList();
        foreach (var yield in new[] { "Food", "Production", "Culture" })
            Assert.IsFalse(words.Any(w => w.Contains(yield)), $"no {yield} yield (owner ruling)");
        info.Dispose();
    }

    [Test]
    public void EveryBiomeHasATerrainSign()
    {
        foreach (var biome in MapBuilder.BiomeColours.Keys)
            CollectionAssert.Contains(UiIcon.Glyphs, "biome-" + biome, biome);
    }

    [Test]
    public void ThePanelsCarryTheTrimAndItTakesNoClicks()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "event-card", "minimap", "info-panel", "screen-cabinet", "screen-budget", "screen-defence", "screen-why" })
        {
            var frame = ui.Q<VisualElement>(name).Children().OfType<UiFrame>().SingleOrDefault();
            Assert.IsNotNull(frame, name);
            Assert.AreEqual(PickingMode.Ignore, frame!.pickingMode, name);
        }
    }

    /// <summary>The icons need a wide bar: a 5:4 window (1280x1024, about 1585 logical px) gets the compact bar without them,
    /// 16:9 keeps them; either way every meter's labels stay inside the meter.</summary>
    [UnityTest]
    public IEnumerator ANarrowWindowDropsTheMeterIcons()
    {
        foreach (var (w, h, compact) in new[] { (1280, 1024, true), (1920, 1080, false), (1280, 720, false) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("CompactTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            HudPresenter? hud = null;
            try
            {
                // As in the game: the bar is laid out (the loading screen) before the HUD is made, and the window never resizes.
                for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
                hud = new HudPresenter(root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text), Text);
                yield return null; yield return null;
                var bar = root.Q("top-bar");
                Assert.AreEqual(compact, bar.ClassListContains(HudPresenter.CompactClass), $"{w}x{h}: bar {bar.worldBound.width:0} px");
                foreach (var icon in root.Q("meters").Query<UiIcon>().ToList())
                    Assert.AreEqual(compact ? DisplayStyle.None : DisplayStyle.Flex, icon.resolvedStyle.display, $"{w}x{h}: {icon.Glyph}");
                foreach (var m in root.Q("meters").Children())
                    foreach (var l in m.Query<Label>().ToList())
                        Assert.LessOrEqual(l.worldBound.xMax, m.worldBound.xMax + 0.5f, $"{w}x{h}: '{l.text}' leaves {m.name}");
            }
            finally { hud?.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }
}
