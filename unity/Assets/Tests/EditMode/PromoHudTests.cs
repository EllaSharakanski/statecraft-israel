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
        // Logical widths (1080p reference, match 0.5): 1280x1024 ~1585, 1024x768 ~1662, 1024x1024 ~1440 (the compact bar's
        // floor), 1366x768 and 1280x720 1920, 1920x1080 1920.
        foreach (var (w, h, compact) in new[] { (1280, 1024, true), (1024, 768, true), (1024, 1024, true), (1920, 1080, false), (1280, 720, false), (1366, 768, false) })
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
                // The widest real date line: a three-digit turn and the longest month.
                root.Q<Label>("turn").text = Text.Format("hud.turn", 120);
                root.Q<Label>("date").text = Text.Format("hud.date", PlayerText.Date(new System.DateTime(1956, 9, 30)));
                yield return null; yield return null; yield return null;
                var badge = root.Q(className: "date-box").worldBound;
                foreach (var l in root.Q(className: "date-box").Query<Label>().ToList())
                    Assert.LessOrEqual(l.worldBound.xMax, badge.xMax - 4f, $"{w}x{h}: '{l.text}' {l.worldBound} runs under the badge's trim {badge}");
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

    static void Near(Color e, Color a, string what) =>
        Assert.That(Mathf.Abs(e.r - a.r) + Mathf.Abs(e.g - a.g) + Mathf.Abs(e.b - a.b), Is.LessThan(0.02f), $"{what}: expected {e} got {a}");

    /// <summary>WCAG relative luminance of a colour composited over the navy behind the HUD.</summary>
    static float Luminance(Color c)
    {
        var navy = new Color(9 / 255f, 14 / 255f, 26 / 255f);
        float Ch(float v, float under) { var x = v * c.a + under * (1 - c.a); return x <= 0.04045f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f); }
        return 0.2126f * Ch(c.r, navy.r) + 0.7152f * Ch(c.g, navy.g) + 0.0722f * Ch(c.b, navy.b);
    }

    static float Contrast(Color a, Color b) { float x = Luminance(a), y = Luminance(b); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }

    /// <summary>Review I1: "open" and keyboard focus are separate signals, and a pointer click does not leave a screen
    /// button focused, so a screen just closed with the mouse never looks open.</summary>
    [UnityTest]
    public IEnumerator FocusAndOpenAreDistinctAndAPointerClickClearsFocus()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");   // ready: a hidden (loading) button takes no focus
        try
        {
            VisualElement Disc(string s) => ui.Q<Button>("nav-" + s).Q(className: "nav-disc");
            nav.Toggle("budget");
            var cabinet = ui.Q<Button>("nav-cabinet");
            cabinet.Focus();
            for (int i = 0; i < 10 && cabinet.focusController.focusedElement != cabinet; i++) yield return null;
            yield return null; yield return null;
            Assert.AreSame(cabinet, cabinet.focusController.focusedElement, "precondition: the keyboard focus is on Cabinet");
            var open = Disc("budget").resolvedStyle; var focused = Disc("cabinet").resolvedStyle;
            // Open: a gold-light rim on the raised navy disc. Focus: an ink rim on the warm focus fill, at least 2.5:1 apart.
            Assert.GreaterOrEqual(Contrast(open.backgroundColor, focused.backgroundColor), 2.5f, "focus and open differ in fill");
            Assert.Greater(Contrast(open.borderTopColor, focused.borderTopColor), 1.1f, "focus and open differ in rim colour");

            // Close Budget with the mouse: the click blurs it, and its disc returns to the unlit rim.
            var budget = ui.Q<Button>("nav-budget");
            budget.Focus();
            nav.Toggle("budget");
            using (var e = ClickEvent.GetPooled()) { e.target = budget; budget.SendEvent(e); }
            yield return null; yield return null;
            Assert.AreNotSame(budget, budget.focusController.focusedElement, "a pointer click leaves no focus behind");
            Assert.IsFalse(budget.ClassListContains(ScreenNav.OpenClass));
            Near(new Color(196 / 255f, 160 / 255f, 92 / 255f), Disc("budget").resolvedStyle.borderTopColor, "the closed button's rim is unlit");
            Assert.AreEqual(Disc("defence").resolvedStyle.borderTopWidth, Disc("budget").resolvedStyle.borderTopWidth, "the closed button looks like the others");
        }
        finally { nav.Dispose(); }
    }

    /// <summary>Review I3: keyboard focus changes End Turn, an event option and a screen button by at least 3:1 (WCAG 2.4.13).</summary>
    [UnityTest]
    public IEnumerator FocusChangesEachControlByAtLeastThreeToOne()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var card = new EventCardPresenter(ui.Root, new SimBridge(g, Text), Text, null);
        ui.Q<VisualElement>("root").RemoveFromClassList("loading");
        try
        {
            foreach (var (name, part) in new[] { ("end-turn", ""), ("option-sign", ""), ("nav-why", "nav-disc") })
            {
                var b = ui.Q<Button>(name);
                VisualElement target = part == "" ? b : b.Q(className: part);
                for (int i = 0; i < 5; i++) yield return null;
                var before = target.resolvedStyle;
                Color bgBefore = before.backgroundColor, rimBefore = before.borderTopColor;
                b.Focus();
                for (int i = 0; i < 10 && b.focusController.focusedElement != b; i++) yield return null;
                yield return null; yield return null;
                Assert.AreSame(b, b.focusController.focusedElement, name + ": focused");
                var after = target.resolvedStyle;
                var change = Mathf.Max(Contrast(bgBefore, after.backgroundColor), Contrast(rimBefore, after.borderTopColor));
                Assert.GreaterOrEqual(change, 3f, $"{name}: focus changes it by {change:0.00}:1");
                b.Blur();
            }
        }
        finally { card.Dispose(); }
    }

    /// <summary>Review M1: every glyph paints without an error (each is drawn in a laid-out panel, and a second callback on
    /// the same element proves the repaint ran).</summary>
    [UnityTest]
    public IEnumerator EveryGlyphDraws()
    {
        var rt = new RenderTexture(640, 360, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("GlyphTest");
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        try
        {
            yield return null;
            var drawn = new System.Collections.Generic.HashSet<string>();
            foreach (var glyph in UiIcon.Glyphs)
            {
                var icon = new UiIcon { Glyph = glyph };
                icon.style.width = 32; icon.style.height = 32;
                var name = glyph;
                icon.generateVisualContent += _ => drawn.Add(name);
                doc.rootVisualElement.Add(icon);
            }
            doc.rootVisualElement.style.flexDirection = FlexDirection.Row; doc.rootVisualElement.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < 30 && drawn.Count < UiIcon.Glyphs.Count; i++) { doc.rootVisualElement.MarkDirtyRepaint(); yield return null; }
            CollectionAssert.AreEquivalent(UiIcon.Glyphs, drawn, "every glyph was painted");
            LogAssert.NoUnexpectedReceived();
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
