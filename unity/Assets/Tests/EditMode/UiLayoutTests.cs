using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
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
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720), (1280, 1024) })
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
}
