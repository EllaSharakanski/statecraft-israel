#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The screen layout at 1920x1080 and 1280x720 (the panel scales with the screen against a 1080p reference,
/// so both resolve to the same logical layout): no fixed region overlaps another, no centre screen covers a fixed region,
/// and the six top-bar meters fit before the screen buttons at their minimum width.</summary>
public class UiLayoutTests
{
    static readonly string[] Fixed = { "top-bar", "event-card", "info-panel", "minimap" };
    static readonly string[] Screens = { "screen-cabinet", "screen-budget", "screen-defence", "screen-why" };

    [UnityTest]
    public IEnumerator NothingOverlapsAt1080pAnd720p()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1280, 1024), (1531, 784), (2560, 1080), (1920, 900) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
            try
            {
                Rect R(string name) => root.Q(name).worldBound;
                foreach (var a in Fixed)
                    foreach (var b in Fixed)
                        if (string.CompareOrdinal(a, b) < 0) Assert.IsFalse(R(a).Overlaps(R(b)), $"{w}x{h}: {a} {R(a)} overlaps {b} {R(b)}");
                foreach (var s in Screens)
                    foreach (var f in Fixed)
                        Assert.IsFalse(R(s).Overlaps(R(f)), $"{w}x{h}: {s} {R(s)} overlaps {f} {R(f)}");
                Assert.LessOrEqual(R("meters").xMax, R("nav").xMin + 0.5f, $"{w}x{h}: the meters run into the screen buttons");
                Assert.LessOrEqual(R("top-bar").xMin, root.Q(className: "date-box").worldBound.xMin, $"{w}x{h}: date box starts outside the top bar");
                Assert.LessOrEqual(root.Q(className: "date-box").worldBound.xMax, R("meters").xMin + 0.5f, $"{w}x{h}: date box runs into the meters");
                Assert.LessOrEqual(R("nav").xMax, root.Q(className: "turn-box").worldBound.xMin + 0.5f, $"{w}x{h}: the screen buttons run into the End Turn box");
                Assert.LessOrEqual(root.Q(className: "turn-box").worldBound.xMax, R("top-bar").xMax + 0.5f, $"{w}x{h}: End Turn box overflows the top bar");
                foreach (var m in root.Q("meters").Children())   // text may wrap in a narrow meter but must stay inside it and inside the bar
                {
                    Assert.LessOrEqual(m.worldBound.yMax, R("top-bar").yMax + 0.5f, $"{w}x{h}: {m.name} overflows the top bar vertically");
                    foreach (var l in m.Query<Label>().ToList())
                    {
                        Assert.GreaterOrEqual(l.worldBound.xMin + 0.5f, m.worldBound.xMin, $"{w}x{h}: '{l.text}' leaves {m.name} on the left");
                        Assert.LessOrEqual(l.worldBound.xMax, m.worldBound.xMax + 0.5f, $"{w}x{h}: '{l.text}' leaves {m.name} on the right");
                        if (l.ClassListContains("meter-name") && !string.IsNullOrEmpty(l.text))
                            Assert.GreaterOrEqual(l.worldBound.height + 0.5f, l.resolvedStyle.fontSize, $"{w}x{h}: '{l.text}' collapsed");
                    }
                }
                foreach (var m in root.Q("meters").Children())
                    Assert.GreaterOrEqual(m.worldBound.width, 128.5f, $"{w}x{h}: {m.name} is squeezed below its minimum width");   // 130 less pixel rounding at 2/3 scale
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }

    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The window sizes a player is likely to have: 16:9, 5:4, the playtest's wide-and-short browser window
    /// (1531x784, about 1.95) and 21:9.</summary>
    static readonly (int w, int h)[] Windows = { (1531, 784), (2560, 1080), (1920, 1080), (1280, 1024), (1920, 900) };

    /// <summary>The real Game.uxml on the real PanelSettings (and so the real theme), rendered at <paramref name="w"/> x
    /// <paramref name="h"/>, with the HUD and the Cabinet screen filled from the opening of the historical game.</summary>
    static IEnumerator Filled(int w, int h, string? screen, System.Action<VisualElement> check)
    {
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("LayoutTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var hud = new HudPresenter(root, bridge, Text);
        var nav = new ScreenNav(root);
        var cabinet = new CabinetPresenter(root, bridge, Text, null);
        nav.Open(screen);
        try
        {
            for (int i = 0; i < 20 && (float.IsNaN(root.Q("minimap").worldBound.width) || root.Q("year").worldBound.height <= 0); i++) yield return null;
            yield return null;   // one more pass, so text measured after the first layout settles
            check(root);
        }
        finally
        {
            cabinet.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>Playtest 1, item 1: every top-bar label (the year, the date, the security level) lies inside the bar,
    /// clear of the window's top edge, and nothing else starts above the bar's bottom edge.</summary>
    [UnityTest]
    public IEnumerator TheTopBarHoldsItsContentInWideAndShortWindows()
    {
        foreach (var (w, h) in Windows)
            yield return Filled(w, h, null, root =>
            {
                var bar = root.Q("top-bar").worldBound;
                Assert.GreaterOrEqual(bar.yMin, -0.5f, $"{w}x{h}: the top bar starts above the window");
                var labels = root.Q("top-bar").Query<Label>().ToList().Where(l => !string.IsNullOrEmpty(l.text)).ToList();
                Assert.Greater(labels.Count, 10, "the HUD filled the bar");
                foreach (var l in labels)
                {
                    Assert.IsTrue(Inside(l.worldBound, bar), $"{w}x{h}: '{l.text}' {l.worldBound} is outside the top bar {bar}");
                    Assert.GreaterOrEqual(l.worldBound.yMin, bar.yMin + 2f, $"{w}x{h}: '{l.text}' sits flush against the top edge");
                }
                foreach (var name in new[] { "event-card", "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
                    if (root.Q(name).resolvedStyle.display != DisplayStyle.None)
                        Assert.GreaterOrEqual(root.Q(name).worldBound.yMin, bar.yMax - 0.5f, $"{w}x{h}: {name} starts under the top bar");
            });
    }

    /// <summary>Playtest 1, item 2: the Cabinet lineup is a list inside the screen panel, below its header, filling the
    /// panel; rows that do not fit scroll instead of spilling over the top bar.</summary>
    [UnityTest]
    public IEnumerator TheCabinetListStaysInsideItsPanel()
    {
        foreach (var (w, h) in Windows)
            yield return Filled(w, h, "cabinet", root =>
            {
                var screen = root.Q("screen-cabinet").worldBound;
                var header = root.Q("screen-cabinet").Q(className: "screen-header").worldBound;
                var list = root.Q<ScrollView>("cabinet-seats");
                var view = list.contentViewport.worldBound;
                Assert.IsTrue(Inside(header, screen), $"{w}x{h}: the header {header} is outside the screen {screen}");
                Assert.IsTrue(Inside(view, screen), $"{w}x{h}: the list {view} is outside the screen {screen}");
                Assert.GreaterOrEqual(view.yMin, header.yMax - 0.5f, $"{w}x{h}: the list starts above the screen header");
                Assert.GreaterOrEqual(view.yMax, root.Q("cabinet-status").worldBound.yMin - 16f, $"{w}x{h}: the list leaves the panel empty below it");
                var rows = list.Query<Button>(className: "seat").ToList();
                Assert.Greater(rows.Count, 5, "the lineup was rendered");
                foreach (var r in rows)
                {
                    Assert.GreaterOrEqual(r.worldBound.yMin, view.yMin - 0.5f, $"{w}x{h}: {r.name} sits above the list");
                    Assert.GreaterOrEqual(r.worldBound.xMin, screen.xMin - 0.5f, $"{w}x{h}: {r.name} leaves the panel on the left");
                    Assert.LessOrEqual(r.worldBound.xMax, screen.xMax + 0.5f, $"{w}x{h}: {r.name} leaves the panel on the right");
                    if (r.worldBound.yMax > view.yMax + 0.5f)
                        Assert.Greater(list.verticalScroller.highValue, 0f, $"{w}x{h}: {r.name} runs past the panel and the list does not scroll");
                }
            });
    }

    /// <summary>Playtest 1, item 3: the camera learns how much of the window the top bar covers, at every window shape,
    /// so the map is framed below it; disposing gives the whole screen back.</summary>
    [UnityTest]
    public IEnumerator TheCameraIsToldWhatTheTopBarCovers()
    {
        var camGo = new GameObject("InsetCamera");
        camGo.AddComponent<Camera>();
        var rig = camGo.AddComponent<Statecraft.Game.Map.CameraRig>();
        try
        {
            foreach (var (w, h) in Windows)
            {
                yield return Filled(w, h, null, root =>
                {
                    var inset = new TopBarInset(root, rig);
                    var bar = root.Q("top-bar").worldBound.height;
                    var screen = root.panel.visualTree.worldBound.height;
                    Assert.Greater(bar, 90f, $"{w}x{h}: the bar is laid out");
                    Assert.AreEqual(bar / screen, rig.TopInset, 1e-3f, $"{w}x{h}: the inset is the bar's share of the window");
                    inset.Dispose();
                    Assert.AreEqual(0f, rig.TopInset, $"{w}x{h}: disposing gives the screen back");
                });
            }
        }
        finally { Object.DestroyImmediate(camGo); }
    }

    // Task 24, seen in headed Chrome: the top bar's labels were taller than the bar and, centred, ran off the top of the
    // screen at every size; the Cabinet list grew out of its screen over the top bar. Checked with every screen filled by
    // its presenter (an event open on the card), at 16:9 and at the wider viewports a browser window leaves.
    [UnityTest]
    public IEnumerator FilledScreensStayInsideTheirPanelsAndTheTopBarFits()
    {
        var c = ContentLoader.Load(Paths.Content);
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        // Bounded (Task 24 review M5): content that no longer raises these events fails the test instead of hanging the run.
        for (int t = 0; !g.State.PendingEvents.Contains("armistice_egypt"); t++)
        {
            if (t >= 500) Assert.Fail("armistice_egypt was not raised within 500 turns");
            g.Step(new Command[0]);
        }
        g.Step(new Command[0]);                                         // a Why report to show
        for (int t = 0; g.State.PendingEvents.Count == 0; t++)
        {
            if (t >= 500) Assert.Fail("no event was raised within 500 turns of the armistice");
            g.Step(new Command[0]);
        }
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1920, 960), (1600, 757), (1366, 657) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var bridge = new SimBridge(g, text);
            var disposables = new List<System.IDisposable>
            {
                new HudPresenter(root, bridge, text), new EventCardPresenter(root, bridge, text, null),
                new CabinetPresenter(root, bridge, text, null), new BudgetPresenter(root, bridge, text),
                new DefencePresenter(root, bridge, text), new WhyPresenter(root, bridge, text),
            };
            var nav = new ScreenNav(root);
            try
            {
                for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
                yield return null;
                Rect R(VisualElement e) => e.worldBound;
                bool Inside(Rect inner, Rect outer) => inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;
                var bar = root.Q("top-bar");
                foreach (var l in bar.Query<VisualElement>().ToList().Where(e => e != bar && e.resolvedStyle.display == DisplayStyle.Flex))
                    if (!Inside(R(l), R(bar))) problems.Add($"{w}x{h}: top bar child {l.name}/{(l as Label)?.text ?? l.GetClasses().FirstOrDefault()} {R(l)} leaves the bar {R(bar)}");
                var card = root.Q("event-card");
                if (R(card).Overlaps(R(root.Q("minimap")))) problems.Add($"{w}x{h}: event card {R(card)} overlaps the minimap {R(root.Q("minimap"))}");
                foreach (var s in ScreenNav.Screens)
                {
                    nav.Open(s);
                    for (int i = 0; i < 3; i++) yield return null;
                    var screen = root.Q("screen-" + s);
                    foreach (var e in screen.Children().Concat(screen.Query<ScrollView>().ToList()).Where(e => e.resolvedStyle.display == DisplayStyle.Flex))
                        if (!Inside(R(e), R(screen))) problems.Add($"{w}x{h}: {s} child {e.name}/{e.GetClasses().FirstOrDefault()} {R(e)} leaves the screen {R(screen)}");
                }
                nav.Open(null);
            }
            finally
            {
                nav.Dispose(); foreach (var d in disposables) d.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>A button's text fits its content box: on one line, or wrapped (white-space normal) within its height.</summary>
    static bool TextFits(TextElement b)
    {
        var box = b.contentRect;
        var natural = b.MeasureTextSize(b.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
        if (natural.x <= box.width + 1f) return true;
        if (b.resolvedStyle.whiteSpace != WhiteSpace.Normal) return false;
        var wrapped = b.MeasureTextSize(b.text, box.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
        return wrapped.y <= box.height + 1f;
    }

    /// <summary>Task 24 review I2: every real event, the tallest included, fits its card above the minimap at every window
    /// shape, wide and short ones included: the card never overlaps the minimap and its option buttons stay inside it.</summary>
    [UnityTest]
    public IEnumerator EveryEventCardStaysAboveTheMinimap()
    {
        var problems = new List<string>();
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1531, 784), (1568, 752), (2560, 1080), (1920, 900), (1366, 657), (1280, 1024) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            // The real portraits: a minister's portrait adds the 200 px image row, so the tallest card is a portrait card.
            var portraits = AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset");
            Assert.IsNotNull(portraits);
            // And the real event photographs: a photograph card adds the 200 px header band and its credit line.
            var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
            Assert.IsNotNull(photos);
            var card = new EventCardPresenter(root, bridge, Text, portraits, photos);
            float tallest = 0; string tallestId = "";
            try
            {
                for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
                foreach (var e in C.Events)
                {
                    var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
                    g.State.Date = e.From;                       // the text names the office holders of the event's own time
                    g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
                    bridge.Replace(g);
                    for (int i = 0; i < 3; i++) yield return null;
                    var c = root.Q("event-card").worldBound;
                    var mini = root.Q("minimap").worldBound;
                    if (c.height > tallest) { tallest = c.height; tallestId = e.Id; }
                    if (c.yMax > mini.yMin + 0.5f && c.xMax > mini.xMin && c.xMin < mini.xMax)
                        problems.Add($"{w}x{h}: {e.Id}: card {c} overlaps the minimap {mini}");
                    foreach (var b in root.Q("event-options").Query<Button>().ToList())
                    {
                        if (b.worldBound.yMax > c.yMax + 0.5f || b.worldBound.yMin < c.yMin - 0.5f || b.worldBound.xMax > c.xMax + 0.5f || b.worldBound.xMin < c.xMin - 0.5f)
                            problems.Add($"{w}x{h}: {e.Id}: option '{b.text}' {b.worldBound} is cut off by the card {c}");
                        // Playtest 2, P2-2: the option's text lies inside its button (it wraps; it is never cut at the edge).
                        if (!TextFits(b))
                            problems.Add($"{w}x{h}: {e.Id}: option text '{b.text}' does not fit its button {b.contentRect}");
                    }
                    // Playtest 2, P2-3: in a window at least as tall (logically) as 1366x657, the body and the options fit
                    // without scrolling; shorter windows may scroll the body as the fallback.
                    var scroll = root.Q<ScrollView>("event-scroll");
                    var logicalHeight = root.panel.visualTree.layout.height;
                    if (logicalHeight >= 990f && scroll.contentContainer.layout.height > scroll.contentViewport.layout.height + 1f)
                        problems.Add($"{w}x{h} (logical height {logicalHeight:0}): {e.Id}: the body scrolls ({scroll.contentContainer.layout.height:0} px of text in {scroll.contentViewport.layout.height:0} px)");
                }
                Debug.Log($"I2 {w}x{h}: tallest card {tallestId} {tallest:0} px");
                Assert.Greater(tallest, 400f, $"{w}x{h}: the cards were laid out (a short window shrinks the tallest to fit)");
            }
            finally
            {
                card.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
