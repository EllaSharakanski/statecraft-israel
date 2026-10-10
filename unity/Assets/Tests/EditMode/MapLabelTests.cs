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
using UnityEngine.UIElements;

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
        Assert.AreEqual(DisplayStyle.Flex, container.style.display.value);
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

    /// <summary>Behavioural, not text (review N4/N6): with the real theme, a laid-out pill resolves to EB Garamond SemiBold on
    /// the navy fill with the gold edge, and the whole layer is display:none while the root carries "loading" (the slots'
    /// inline visibility cannot override that), and back once it is removed.</summary>
    [UnityTest]
    public IEnumerator ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading()
    {
        // A 1920x1080 panel (scale 1), as in the game: UiTestKit's panel follows the batch-mode screen (about 0.39 points per
        // pixel there), and UI Toolkit rounds a 1 px border up to one physical pixel, 2.57 points.
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
        var go = new GameObject("LabelPanel"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var labels = new MapLabelsPresenter(doc.rootVisualElement, map, cam);
        try
        {
            var root = doc.rootVisualElement.Q<VisualElement>("root");
            var container = doc.rootVisualElement.Q<VisualElement>(MapLabelsPresenter.ContainerName);
            Assert.IsTrue(root.ClassListContains("loading"), "Game.uxml starts loading");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.None, container.resolvedStyle.display, "hidden while the game loads");
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
            Assert.IsNull(GameObject.Find("tel_aviv_label"), "no world-space TextMesh labels remain");
        }
        finally { labels.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    static IEnumerator Settle(MapLabelsPresenter labels)
    {
        for (int i = 0; i < 20 && labels.Labels.Any(l => float.IsNaN(l.resolvedStyle.width) || l.resolvedStyle.width <= 0); i++) yield return null;
        labels.Refresh(force: true); yield return null;   // the first refresh with sizes declutters
        labels.Refresh(force: true); yield return null;
    }

    /// <summary>Independent of the presenter's own projection call: the Tel Aviv pill's bottom centre is within 2 px of its
    /// marker box's top, projected with Camera.WorldToScreenPoint and the panel's scale, LiftPx above it.</summary>
    [UnityTest]
    public IEnumerator APillSitsJustAboveItsMarkerTop()
    {
        var rt = new RenderTexture(1920, 1080, 0); var camRt = new RenderTexture(1920, 1080, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
        var go = new GameObject("LabelPanel"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        cam.targetTexture = camRt;
        var root = doc.rootVisualElement; root.Q("root")?.RemoveFromClassList("loading");
        var labels = new MapLabelsPresenter(root, map, cam);
        try
        {
            yield return Settle(labels);
            var box = GameObject.Find("Settlements").transform.Find("tel_aviv");
            var top = box.position + Vector3.up * box.lossyScale.y / 2;
            var p = cam.WorldToScreenPoint(top);                       // pixels of the 1920x1080 camera target, y up
            float scale = root.worldBound.width / 1920f;
            var expected = new Vector2(p.x * scale, (1080f - p.y) * scale - MapLabelsPresenter.LiftPx);
            var wb = labels.Labels.Single(l => l.name == "label-tel_aviv").worldBound;
            TestContext.WriteLine($"Tel Aviv pill {wb}, expected bottom centre {expected}");
            Assert.AreEqual(expected.x, wb.center.x, 2f, "centred on the marker");
            Assert.AreEqual(expected.y, wb.yMax, 2f, "LiftPx above the marker's top");
        }
        finally
        {
            labels.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            cam.targetTexture = null; camRt.Release(); Object.DestroyImmediate(camRt);
        }
    }

    static readonly string[] TelAvivCluster = { "tel_aviv", "jaffa", "petah_tikva", "rishon_lezion", "lod", "ramla", "rehovot" };

    static float Gap(Rect r, Vector2 p) => new Vector2(Mathf.Max(0, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;

    /// <summary>M2's cluster test, on layout bounds (review I5): at 90 km (the flythrough's frame 120) and at the overview,
    /// no two shown pills overlap, Tel Aviv is always shown, and every shown pill of the Tel Aviv - Lod cluster is at most
    /// half as far from its own marker top as from any other cluster marker's top, so none reads as a neighbour's.</summary>
    [UnityTest]
    public IEnumerator ShownPillsNeverOverlapAndEachReadsAsItsOwn()
    {
        var rt = new RenderTexture(1920, 1080, 0); var camRt = new RenderTexture(1920, 1080, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
        var go = new GameObject("LabelPanel"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        cam.targetTexture = camRt;
        var root = doc.rootVisualElement; root.Q("root")?.RemoveFromClassList("loading");
        var labels = new MapLabelsPresenter(root, map, cam);
        try
        {
            foreach (var (name, lon, lat, distance) in new[] { ("90 km", 34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 90000f), ("overview", PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, CameraController.MaxDistance) })
            {
                Pose(lon, lat, distance);
                yield return Settle(labels);
                float scale = root.worldBound.width / 1920f;
                Vector2 MarkerTop(string id)
                {
                    var box = GameObject.Find("Settlements").transform.Find(id);
                    var p = cam.WorldToScreenPoint(box.position + Vector3.up * box.lossyScale.y / 2);
                    return new Vector2(p.x * scale, (1080f - p.y) * scale);
                }
                var shown = labels.Slots.Where(Shown).Select(s => (id: s.name.Substring("slot-".Length), rect: s.Q<Label>().worldBound)).ToList();
                TestContext.WriteLine($"{name}: {shown.Count} of {labels.Slots.Count} pills shown");
                Assert.IsTrue(shown.Any(s => s.id == "tel_aviv"), $"{name}: Tel Aviv is shown");
                for (int i = 0; i < shown.Count; i++)
                    for (int j = i + 1; j < shown.Count; j++)
                        Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{name}: {shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
                foreach (var (id, rect) in shown.Where(s => TelAvivCluster.Contains(s.id)))
                {
                    float own = Gap(rect, MarkerTop(id));
                    foreach (var other in TelAvivCluster.Where(o => o != id))
                        Assert.Greater(Gap(rect, MarkerTop(other)), 2f * own + 4f, $"{name}: {id} pill is {own:F0} px from its marker but closer to {other}'s");
                }
                if (distance > MapLabelsPresenter.DeclutterDistanceM)
                    foreach (var s in map.Settlements.Where(s => MapLabelsPresenter.HiddenByZoom(s.Type, distance)))
                        Assert.IsFalse(Shown(labels.Slots.Single(x => x.name == "slot-" + s.Id)), $"{name}: {s.Id} ({s.Type}) is decluttered");
            }
        }
        finally
        {
            labels.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            cam.targetTexture = null; camRt.Release(); Object.DestroyImmediate(camRt);
        }
    }
}
