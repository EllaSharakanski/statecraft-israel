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

/// <summary>An idle game must leave the UI alone: once the HUD has settled, no frame may lay anything out again (a
/// GeometryChangedEvent) or regenerate any painted trim or icon (generateVisualContent). A per-frame relayout or repaint
/// would cost every frame. The promo HUD (#144) was suspected of exactly that after a WebGL frame-time rise; this test
/// showed it does not (0 relayouts, 0 repaints over 300 idle frames: the rise came from the promo-sea shader), and keeps
/// it that way.</summary>
public class HudIdleTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    const int SettleFrames = 30, IdleFrames = 300;

    [UnityTest]
    public IEnumerator AnIdleGameNeitherRelaysNorRepaintsTheHud()
    {
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("HudIdleTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        var owned = new List<System.IDisposable>();
        try
        {
            // As in the game: the bar is laid out (the loading screen) before the presenters are made.
            for (int i = 0; i < 20 && !(root.Q("top-bar").worldBound.width > 0); i++) yield return null;
            root.Q("root").RemoveFromClassList("loading");
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
            owned.Add(new HudPresenter(root, bridge, Text));
            owned.Add(new EventCardPresenter(root, bridge, Text, null));
            owned.Add(new ScreenNav(root));
            owned.Add(new PointerBlur(root));

            var geometry = new Dictionary<string, int>();
            var painted = new Dictionary<string, int>();
            string Key(VisualElement e) => $"{e.GetType().Name}#{e.name}.{string.Join(".", e.GetClasses())}@{e.parent?.name}";
            void Count(Dictionary<string, int> d, string k) => d[k] = d.TryGetValue(k, out var n) ? n + 1 : 1;
            foreach (var e in root.Query<VisualElement>().ToList())
            {
                var k = Key(e);
                e.RegisterCallback<GeometryChangedEvent>(_ => Count(geometry, k));
                e.generateVisualContent += _ => Count(painted, k);
            }
            var bar = root.Q("top-bar");
            for (int i = 0; i < SettleFrames; i++) yield return null;   // the bar turns roomy here (one flip, by design)
            geometry.Clear(); painted.Clear();
            int classFlips = 0; var lastClasses = string.Join(" ", bar.GetClasses());
            for (int i = 0; i < IdleFrames; i++)
            {
                yield return null;
                var now = string.Join(" ", bar.GetClasses());
                if (now != lastClasses) { classFlips++; lastClasses = now; }
            }

            string Report(Dictionary<string, int> d) =>
                d.Count == 0 ? "none" : string.Join("; ", d.OrderByDescending(p => p.Value).Take(12).Select(p => $"{p.Key} x{p.Value}"));
            Debug.Log($"HUDIDLE frames={IdleFrames} geometry_events={geometry.Values.Sum()} repaints={painted.Values.Sum()} " +
                      $"bar_class_flips={classFlips} bar_classes=[{lastClasses}]\nGEOMETRY {Report(geometry)}\nPAINT {Report(painted)}");
            Assert.AreEqual(0, classFlips, "the top bar's classes flip while idle");
            Assert.IsEmpty(geometry, "laid out again while idle: " + Report(geometry));
            Assert.IsEmpty(painted, "repainted while idle: " + Report(painted));

            // Control: the harness does see a repaint and a relayout when one happens, so the zeros above are real.
            var frame = root.Q("top-bar").Q<UiFrame>();
            frame.MarkDirtyRepaint();
            root.Q(className: "date-box").style.width = 240;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.Greater(painted.Values.Sum(), 0, "control: a dirtied UiFrame repaints");
            Assert.Greater(geometry.Values.Sum(), 0, "control: a resized date box lays out");
        }
        finally
        {
            foreach (var d in owned) d.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
