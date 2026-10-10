#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

/// <summary>Plan Task 9: settlement labels as UI Toolkit pills in EB Garamond, decluttered and on their markers.</summary>
public class MapLabelTests
{
    MapScene map = null!;
    Camera cam = null!;
    CameraRig rig = null!;

    [SetUp]
    public void Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        cam = Camera.main; rig = cam.GetComponent<CameraRig>();
        Pose(34.85, 32.0, 90000f);
    }

    [TearDown]
    public void Close() { cam.targetTexture = null; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    void Pose(double lon, double lat, float distance)
    {
        rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
    }

    static bool Shown(VisualElement slot) => slot.style.visibility.value == Visibility.Visible;

    /// <summary>A UIDocument with Game.uxml on a panel of its own size (scale-with-screen-size against 1920x1080, so a
    /// 2880x1620 target is a scale of 1.5), the camera drawing to a texture of the same size, "loading" removed.</summary>
    sealed class HdPanel : System.IDisposable
    {
        public readonly int Width, Height; public readonly VisualElement Root;
        readonly RenderTexture rt, camRt; readonly PanelSettings ps; readonly GameObject go; readonly Camera cam;

        public HdPanel(Camera cam, int width = 1920, int height = 1080, bool loading = false)
        {
            this.cam = cam; Width = width; Height = height;
            rt = new RenderTexture(width, height, 0); camRt = new RenderTexture(width, height, 24);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
            go = new GameObject("LabelPanel"); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            cam.targetTexture = camRt;
            Root = doc.rootVisualElement;
            if (!loading) Root.Q("root")?.RemoveFromClassList("loading");
        }

        /// <summary>A world point in panel coordinates, independently of the presenter: the camera's pixels, y flipped, times
        /// the panel's scale.</summary>
        public Vector2 ToPanel(Vector3 world, out bool inFront)
        {
            var p = cam.WorldToScreenPoint(world); inFront = p.z > 0;
            float scale = Root.worldBound.width / Width;
            return new Vector2(p.x * scale, (Height - p.y) * scale);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            cam.targetTexture = null; camRt.Release(); Object.DestroyImmediate(camRt);
        }
    }

    static Vector3 MarkerTopOf(string id)
    {
        var box = GameObject.Find("Settlements").transform.Find(id);
        return box.position + Vector3.up * box.lossyScale.y / 2;
    }

    [Test]
    public void EverySettlementHasOneGaramondPillThatIgnoresThePointer()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var names = map.Settlements.Select(s => s.Name).ToArray();
        CollectionAssert.AreEqual(names, labels.Labels.Select(l => l.text).ToArray());
        var container = ui.Q<VisualElement>(MapLabelsPresenter.ContainerName);
        Assert.AreEqual(names.Length, container.childCount);
        foreach (var slot in labels.Slots) Assert.AreEqual(PickingMode.Ignore, slot.pickingMode);
        foreach (var l in labels.Labels)
        {
            Assert.IsTrue(l.ClassListContains(MapLabelsPresenter.LabelClass), l.text);
            Assert.AreEqual(PickingMode.Ignore, l.pickingMode, l.text + " must not catch map clicks");
        }
        Assert.AreEqual(PickingMode.Ignore, container.pickingMode);
        VisualToggles.Set("labels", false);
        Assert.AreEqual(DisplayStyle.None, container.style.display.value, "the labels toggle hides the layer");
        VisualToggles.Set("labels", true);
        Assert.AreEqual(StyleKeyword.Null, container.style.display.keyword, "on clears the inline display, so the loading rule still wins (review M1)");
    }

    [Test]
    public void LabelsOffScreenAreHiddenAndComeBack()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
        Pose(35.5, 33.2, CameraController.MinDistance);   // the far north, close up
        labels.Refresh(force: true);
        Assert.IsFalse(Shown(tlv), "Tel Aviv is off screen");
        Pose(34.85, 32.0, 90000f);
        labels.Refresh(force: true);
        Assert.IsTrue(Shown(tlv));
    }

    [Test]
    public void TheDenseClusterKeepsItsAnchorsAndMinorPlacesGiveWayAtTheOverview()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        string AnchorOf(string id) => labels.Labels.Single(l => l.name == "label-" + id).GetClasses().Single(c => c.StartsWith("anchor-"));
        Assert.AreEqual("anchor-lower-right", AnchorOf("jaffa")); Assert.AreEqual("anchor-lower-right", AnchorOf("rishon_lezion"));
        Assert.AreEqual("anchor-lower-left", AnchorOf("lod")); Assert.AreEqual("anchor-lower-left", AnchorOf("petah_tikva"));
        Assert.AreEqual("anchor-lower-centre", AnchorOf("tel_aviv"));
        var ramla = map.Settlements.Single(s => s.Id == "ramla");
        Assert.AreEqual(map.Frame.ToWorld(ramla.Lon, ramla.Lat, 0).x - 1500f, MapLabelsPresenter.Anchor(map, ramla).x, 1f, "Ramla's label nudged west");
        Assert.Less(MapLabelsPresenter.Priority("city"), MapLabelsPresenter.Priority("town"));
        Assert.AreEqual(MapLabelsPresenter.Priority("town"), MapLabelsPresenter.Priority("arab_town"));
        Assert.Less(MapLabelsPresenter.Priority("arab_town"), MapLabelsPresenter.Priority("kibbutz"));
        foreach (var minor in new[] { "village", "kibbutz", "moshav" })
        {
            Assert.IsFalse(MapLabelsPresenter.HiddenByZoom(minor, 90000f), minor + " at 90 km");
            Assert.IsTrue(MapLabelsPresenter.HiddenByZoom(minor, CameraController.MaxDistance), minor + " at the overview");
        }
        Assert.IsFalse(MapLabelsPresenter.HiddenByZoom("town", CameraController.MaxDistance));
        Assert.AreEqual(new Rect(90, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-centre"));
        Assert.AreEqual(new Rect(100, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-left"));
        Assert.AreEqual(new Rect(80, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-right"));
    }

    [Test]
    public void RebuildingKeepsOneLabelPerSettlement()
    {
        using var ui = new UiTestKit();
        var first = new MapLabelsPresenter(ui.Root, map, cam);
        first.Dispose();
        Assert.AreEqual(0, ui.Q<VisualElement>(MapLabelsPresenter.ContainerName).childCount, "dispose clears");
        map.Build();
        using var second = new MapLabelsPresenter(ui.Root, map, cam);
        using var third = new MapLabelsPresenter(ui.Root, map, cam);
        Assert.AreEqual(map.Settlements.Count, ui.Q<VisualElement>(MapLabelsPresenter.ContainerName).childCount);
    }

    /// <summary>Review M2: an older presenter disposed after a newer one was made removes only its own pills.</summary>
    [Test]
    public void DisposingAnOlderPresenterLeavesTheNewerOnesPills()
    {
        using var ui = new UiTestKit();
        var older = new MapLabelsPresenter(ui.Root, map, cam);
        using var newer = new MapLabelsPresenter(ui.Root, map, cam);
        older.Dispose();
        var container = ui.Q<VisualElement>(MapLabelsPresenter.ContainerName);
        Assert.AreEqual(map.Settlements.Count, container.childCount);
        foreach (var slot in newer.Slots) Assert.AreSame(container, slot.parent, slot.name);
    }

    /// <summary>Behavioural, not text (review N4/N6): with the real theme, a laid-out pill resolves to EB Garamond SemiBold on
    /// the navy fill with the gold edge, and the whole layer is display:none while the root carries "loading" (neither the
    /// slots' inline visibility nor the labels switch turned on can override that, review M1), and back once it is
    /// removed. A 1920x1080 panel (scale 1), as in the game: UiTestKit's panel follows the batch-mode screen (about 0.39
    /// points per pixel there), and UI Toolkit rounds a 1 px border up to one physical pixel, 2.57 points.</summary>
    [UnityTest]
    public IEnumerator ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading()
    {
        var hd = new HdPanel(cam, loading: true);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            var root = hd.Root.Q<VisualElement>("root");
            var container = hd.Root.Q<VisualElement>(MapLabelsPresenter.ContainerName);
            Assert.IsTrue(root.ClassListContains("loading"), "Game.uxml starts loading");
            VisualToggles.Set("labels", true);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.None, container.resolvedStyle.display, "hidden while the game loads, the switch on or not");
            root.RemoveFromClassList("loading");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, container.resolvedStyle.display, "shown once ready");
            var pill = labels.Labels.Single(l => l.name == "label-tel_aviv");
            var font = pill.resolvedStyle.unityFontDefinition;
            var fontName = font.font != null ? font.font.name : font.fontAsset != null ? font.fontAsset.name : "";
            StringAssert.Contains("EBGaramond-SemiBold", fontName);
            var bg = pill.resolvedStyle.backgroundColor;
            Assert.AreEqual(14f / 255f, bg.r, 0.01f); Assert.AreEqual(22f / 255f, bg.g, 0.01f); Assert.AreEqual(38f / 255f, bg.b, 0.01f);
            Assert.AreEqual(0.82f, bg.a, 0.01f);
            var gold = pill.resolvedStyle.borderTopColor;
            Assert.AreEqual(196f / 255f, gold.r, 0.01f); Assert.AreEqual(160f / 255f, gold.g, 0.01f); Assert.AreEqual(92f / 255f, gold.b, 0.01f);
            Assert.AreEqual(1f, pill.resolvedStyle.borderTopWidth, 0.01f);
            Assert.GreaterOrEqual(pill.resolvedStyle.fontSize, labels.Labels.Single(l => l.name == "label-petah_tikva").resolvedStyle.fontSize + 2f,
                "a city's name is visibly larger than a town's (review M5)");
            Assert.IsNull(GameObject.Find("tel_aviv_label"), "no world-space TextMesh labels remain");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    static IEnumerator Settle(MapLabelsPresenter labels)
    {
        for (int i = 0; i < 20 && labels.Labels.Any(l => float.IsNaN(l.resolvedStyle.width) || l.resolvedStyle.width <= 0); i++) yield return null;
        labels.Refresh(force: true); yield return null;   // the first refresh with sizes declutters
        labels.Refresh(force: true); yield return null;
    }

    /// <summary>Independent of the presenter's own projection call: the Tel Aviv pill's bottom centre is within 2 px of its
    /// marker box's top, projected with Camera.WorldToScreenPoint and the panel's scale, LiftPx above it. Also on a 2880x1620
    /// target (panel scale 1.5, Retina-like) with the game's top-bar inset (review I5).</summary>
    [UnityTest]
    public IEnumerator APillSitsJustAboveItsMarkerTop() => PillOnMarker(1920, 1080, 0f);

    [UnityTest]
    public IEnumerator APillSitsJustAboveItsMarkerTopAtScaleOneAndAHalfWithTheTopInset() => PillOnMarker(2880, 1620, 0.088f);

    IEnumerator PillOnMarker(int width, int height, float topInset)
    {
        var hd = new HdPanel(cam, width, height);
        rig.TopInset = topInset; rig.ApplyProjection();
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            yield return Settle(labels);
            var expected = hd.ToPanel(MarkerTopOf("tel_aviv"), out _) - new Vector2(0, MapLabelsPresenter.LiftPx);
            var wb = labels.Labels.Single(l => l.name == "label-tel_aviv").worldBound;
            TestContext.WriteLine($"{width}x{height} inset {topInset}: Tel Aviv pill {wb}, expected bottom centre {expected}, panel {hd.Root.worldBound.size}");
            Assert.AreEqual(1920f, hd.Root.worldBound.width, 1f, $"the panel is 1920 points wide: a scale of {width / 1920f}");
            Assert.AreEqual(expected.x, wb.center.x, 2f, "centred on the marker");
            Assert.AreEqual(expected.y, wb.yMax, 2f, "LiftPx above the marker's top");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    static float Gap(Rect r, Vector2 p) => new Vector2(Mathf.Max(0, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;

    static readonly string[] Occluders = { "top-bar", "info-panel", "minimap" };

    /// <summary>Marker tops on screen and not under a panel, in panel coordinates (independent of the presenter).</summary>
    static System.Collections.Generic.Dictionary<string, Vector2> VisibleMarkerTops(HdPanel hd, MapScene map)
    {
        var panels = Occluders.Select(n => hd.Root.Q<VisualElement>(n).worldBound).ToList();
        var size = hd.Root.worldBound.size;
        var tops = new System.Collections.Generic.Dictionary<string, Vector2>();
        foreach (var s in map.Settlements)
        {
            var p = hd.ToPanel(MarkerTopOf(s.Id), out bool inFront);
            if (inFront && p.x >= 0 && p.x <= size.x && p.y >= 0 && p.y <= size.y && !panels.Any(r => r.Contains(p))) tops[s.Id] = p;
        }
        return tops;
    }

    /// <summary>M2's cluster test, on layout bounds (review I5), widened by reviews I2/I4 and made robust to terrain changes
    /// (re-review, task 3). At 90 km (the flythrough's frame 120) and at the overview: no two shown pills overlap; every
    /// shown pill's marker is visible and the pill is more than twice as far (plus 4 px) from every other visible marker's
    /// top as from its own, so none reads as a neighbour's; completeness: every visible marker of a place not decluttered
    /// by zoom with no other visible marker within 2 pill heights + MarginPx is shown; only unambiguous names are pinned
    /// (well apart from any other marker), with a lower bound on the count; and at the overview a coastal town between
    /// Haifa and Tel Aviv is labelled. The full shown set is logged, for a new baseline.</summary>
    [UnityTest]
    public IEnumerator ShownPillsNeverOverlapAndEachReadsAsItsOwn()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            foreach (var (name, lon, lat, distance, pinned, atLeast) in new[]
            {
                ("90 km", 34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 90000f, new[] { "tel_aviv", "petah_tikva" }, 8),
                ("overview", PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, CameraController.MaxDistance, new[] { "tel_aviv", "jerusalem", "gaza", "beersheba" }, 7),
            })
            {
                Pose(lon, lat, distance);
                yield return Settle(labels);
                var tops = VisibleMarkerTops(hd, map);
                var shown = labels.Slots.Where(Shown).Select(s => (id: s.name.Substring("slot-".Length), rect: s.Q<Label>().worldBound)).ToList();
                TestContext.WriteLine($"{name}: {shown.Count} of {labels.Slots.Count} pills shown: {string.Join(" ", shown.Select(s => s.id))}");
                TestContext.WriteLine($"{name}: visible markers {string.Join(" ", tops.Select(t => $"{t.Key}@{t.Value.x:F0},{t.Value.y:F0}"))}");
                foreach (var id in pinned) Assert.IsTrue(shown.Any(s => s.id == id), $"{name}: {id} is shown");
                Assert.GreaterOrEqual(shown.Count, atLeast, $"{name}: shown pills");
                if (name == "overview") Assert.IsTrue(shown.Any(s => s.id == "hadera" || s.id == "netanya"), "overview: a coastal town between Haifa and Tel Aviv is labelled");
                for (int i = 0; i < shown.Count; i++)
                    for (int j = i + 1; j < shown.Count; j++)
                        Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{name}: {shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
                foreach (var (id, rect) in shown)
                {
                    Assert.IsTrue(tops.ContainsKey(id), $"{name}: {id} is shown but its marker is not visible");
                    float own = Gap(rect, tops[id]);
                    foreach (var other in tops.Where(o => o.Key != id))
                        Assert.Greater(Gap(rect, other.Value), 2f * own + 4f, $"{name}: {id} pill is {own:F0} px from its marker but closer to {other.Key}'s");
                }
                foreach (var s in map.Settlements.Where(s => tops.ContainsKey(s.Id) && !MapLabelsPresenter.HiddenByZoom(s.Type, distance)))
                {
                    float clear = 2f * labels.Labels.Single(l => l.name == "label-" + s.Id).worldBound.height + MapLabelsPresenter.MarginPx;
                    if (tops.Any(o => o.Key != s.Id && Vector2.Distance(o.Value, tops[s.Id]) <= clear)) continue;
                    Assert.IsTrue(shown.Any(x => x.id == s.Id), $"{name}: {s.Id} has no visible marker within {clear:F0} px, so it must be labelled");
                }
                if (distance > MapLabelsPresenter.DeclutterDistanceM)
                    foreach (var s in map.Settlements.Where(s => MapLabelsPresenter.HiddenByZoom(s.Type, distance)))
                        Assert.IsFalse(Shown(labels.Slots.Single(x => x.name == "slot-" + s.Id)), $"{name}: {s.Id} ({s.Type}) is decluttered");
            }
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Re-review N1: a panel that appears (the event card) hides the pill it covers when its layout lands, from the
    /// geometry event itself, with no other refresh (in the game that is before the frame is drawn, not one frame later).</summary>
    [UnityTest]
    public IEnumerator TheEventCardAppearingHidesAnOverlappedPillAtOnce()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            Pose(34.43, 31.95, 90000f);   // Tel Aviv on the right, where the event card opens
            yield return Settle(labels);
            var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
            var before = tlv.Q<Label>().worldBound;
            Assert.IsTrue(Shown(tlv), "Tel Aviv is shown before the card opens");
            var card = hd.Root.Q<VisualElement>("event-card");
            card.style.minHeight = 500; card.style.display = DisplayStyle.Flex;
            for (int i = 0; i < 10 && (float.IsNaN(card.worldBound.width) || card.worldBound.width <= 0); i++) yield return null;
            TestContext.WriteLine($"Tel Aviv {before}, the card {card.worldBound}");
            Assert.IsTrue(card.worldBound.Overlaps(before), "precondition: the card covers Tel Aviv's pill");
            var after = tlv.Q<Label>().worldBound;
            Assert.IsTrue(!Shown(tlv) || !after.Overlaps(card.worldBound), $"Tel Aviv's pill {after} is still under the card {card.worldBound}");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Re-review N2: a pill that changes size with the camera still (a late font, a new text) is re-placed by the
    /// next plain refresh: label sizes are part of what Refresh checks before returning early.</summary>
    [UnityTest]
    public IEnumerator APillThatGrowsIsRePlacedWithoutACameraChange()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            Pose(34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 90000f);
            yield return Settle(labels);
            var tlv = labels.Labels.Single(l => l.name == "label-tel_aviv");
            float width = tlv.resolvedStyle.width;
            tlv.text = "Tel Aviv, the first Hebrew city, with Jaffa to the south";
            for (int i = 0; i < 10 && tlv.resolvedStyle.width <= width + 1f; i++) yield return null;
            Assert.Greater(tlv.resolvedStyle.width, width + 200f, "the pill grew");
            labels.Refresh();
            var shown = labels.Slots.Where(Shown).Select(s => (id: s.name, rect: s.Q<Label>().worldBound)).ToList();
            for (int i = 0; i < shown.Count; i++)
                for (int j = i + 1; j < shown.Count; j++)
                    Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Review I1: with the game's top-bar inset, no shown pill reaches under the top bar, the info panel or the
    /// minimap, at both promo poses.</summary>
    [UnityTest]
    public IEnumerator NoPillSitsUnderAPanel()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        var inset = new TopBarInset(hd.Root, rig);
        try
        {
            foreach (var pose in new[] { PromoPose.Diorama02, PromoPose.Gameplay03 })
            {
                pose.Apply(rig, map.Frame);
                yield return null;
                inset.Update();
                Assert.Greater(rig.TopInset, 0.05f, "the top bar's inset applies");
                yield return Settle(labels);
                var bar = hd.Root.Q<VisualElement>("top-bar").worldBound;
                var info = hd.Root.Q<VisualElement>("info-panel").worldBound;
                var mini = hd.Root.Q<VisualElement>("minimap").worldBound;
                int count = 0;
                foreach (var slot in labels.Slots.Where(Shown))
                {
                    var r = slot.Q<Label>().worldBound; count++;
                    Assert.GreaterOrEqual(r.yMin, bar.yMax - 0.5f, $"{pose.Name}: {slot.name} {r} reaches under the top bar {bar}");
                    Assert.IsFalse(r.Overlaps(info), $"{pose.Name}: {slot.name} {r} under the info panel {info}");
                    Assert.IsFalse(r.Overlaps(mini), $"{pose.Name}: {slot.name} {r} under the minimap {mini}");
                }
                var zikhron = labels.Slots.Single(s => s.name == "slot-zikhron_yaakov");
                TestContext.WriteLine($"{pose.Name}: {count} shown; Zikhron Ya'akov shown={Shown(zikhron)} {zikhron.Q<Label>().worldBound}; bar {bar}");
                Assert.Greater(count, 4, $"{pose.Name}: pills are still shown");
            }
        }
        finally { inset.Dispose(); labels.Dispose(); hd.Dispose(); }
    }

    /// <summary>Review I3: a refresh allocates nothing, and placing every pill while the camera turns and zooms is cheap
    /// (240 refreshes, each with a new view).</summary>
    [UnityTest]
    public IEnumerator RefreshWhileTheCameraMovesIsCheapAndAllocationFree()
    {
        var hd = new HdPanel(cam);
        var labels = new MapLabelsPresenter(hd.Root, map, cam);
        try
        {
            yield return Settle(labels);
            Assert.That(() => labels.Refresh(force: true), Is.Not.AllocatingGCMemory());
            var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
            var times = new System.Collections.Generic.List<double>();
            var watch = new System.Diagnostics.Stopwatch();
            int moved = 0; var last = tlv.style.translate.value;
            for (int i = 0; i < 240; i++)
            {
                rig.Yaw += 1.5f; rig.Distance = Mathf.Lerp(60000f, CameraController.MaxDistance, i / 239f); rig.Apply();
                watch.Restart(); labels.Refresh(); watch.Stop();
                times.Add(watch.Elapsed.TotalMilliseconds * 1000.0);
                var now = tlv.style.translate.value;
                if (now.x != last.x || now.y != last.y) moved++;
                last = now;
            }
            times.Sort();
            double avg = times.Average(), p95 = times[(int)(0.95 * (times.Count - 1))];
            TestContext.WriteLine(System.FormattableString.Invariant($"LABELS refresh moving avg_us={avg:0} p95_us={p95:0} moved={moved}"));
            Assert.Greater(moved, 120, "the refreshes did work: Tel Aviv's pill moved");
            Assert.Less(avg, 200.0, "average refresh under 200 us (editor Mono)");
        }
        finally { labels.Dispose(); hd.Dispose(); }
    }
}
