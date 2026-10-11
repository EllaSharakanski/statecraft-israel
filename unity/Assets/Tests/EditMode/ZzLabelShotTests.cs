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

/// <summary>Temporary (not committed): before/after shots of the map labels over the kits.</summary>
public class ZzLabelShotTests
{
    [UnityTest]
    public IEnumerator ZzShootLabels()
    {
        string dir = System.Environment.GetEnvironmentVariable("LABEL_SHOTS") ?? "/tmp/labelshots";
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 0); var camRt = new RenderTexture(W, H, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
        var go = new GameObject("LabelPanel"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        cam.targetTexture = camRt;
        var root = doc.rootVisualElement; root.Q("root")?.RemoveFromClassList("loading");
        foreach (var n in new[] { "top-bar", "info-panel", "minimap", "event-card" }) { var e = root.Q(n); if (e != null) e.style.display = DisplayStyle.None; }
        var labels = new MapLabelsPresenter(root, map, cam);
        var deg = map.Settlements.Single(s => s.Id == "degania_alef"); var zik = map.Settlements.Single(s => s.Id == "zikhron_yaakov");
        foreach (var (name, lon, lat, d, yaw) in new[] { ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance, PromoPose.Gameplay03.Yaw),
                     ("degania10km", deg.Lon, deg.Lat, 10000f, 0f), ("zikhron25km", zik.Lon, zik.Lat, 25000f, 0f) })
        {
            rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = d; rig.Yaw = yaw; rig.TopInset = 0; rig.Apply();
            for (int i = 0; i < 20; i++) { yield return null; labels.Refresh(force: true); }
            cam.Render();
            RenderTexture.active = camRt;
            var img = new Texture2D(W, H, TextureFormat.RGBA32, false); img.ReadPixels(new Rect(0, 0, W, H), 0, 0); img.Apply();
            RenderTexture.active = null;
            var navy = new Color(14 / 255f, 22 / 255f, 38 / 255f); var gold = new Color(196 / 255f, 160 / 255f, 92 / 255f);
            float scale = W / root.worldBound.width;
            foreach (var slot in labels.Slots.Where(s => s.style.visibility.value == Visibility.Visible))
            {
                var r = slot.Q<Label>().worldBound;
                int x0 = Mathf.RoundToInt(r.xMin * scale), x1 = Mathf.RoundToInt(r.xMax * scale), y0 = Mathf.RoundToInt(r.yMin * scale), y1 = Mathf.RoundToInt(r.yMax * scale);
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    bool edge = x == x0 || x == x1 - 1 || y == y0 || y == y1 - 1;
                    int py = H - 1 - y;
                    img.SetPixel(x, py, edge ? gold : Color.Lerp(img.GetPixel(x, py), navy, 0.82f));
                }
                TestContext.WriteLine($"{name} {slot.name} {r}");
            }
            img.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), img.EncodeToPNG());
            Object.DestroyImmediate(img);
        }
        labels.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); cam.targetTexture = null; rt.Release(); camRt.Release();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear();
    }
}
