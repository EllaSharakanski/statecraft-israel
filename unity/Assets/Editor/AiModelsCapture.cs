using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Art spike (branch promo-ai3d): places TRELLIS image-to-3D models (tools/ai3d; none committed yet, missing ones are skipped) from Assets/AI3D/&lt;name&gt;/&lt;name&gt;.obj (+ .png)
/// in a row at PromoPose.Gameplay03, next to the procedural kits, and renders each pose with and without them into
/// -captureDir (default captures/visual/ai3d) on the WebGL tier (quality level 0). The scene is not saved.
/// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod AiModelsCapture.Row</summary>
public static class AiModelsCapture
{
    // name, symbolic height in metres (promo 03 sizes: blocks ~60x real, houses and trees ~100x), yaw offset (deg).
    static readonly (string name, float height, float yaw)[] Models =
    {
        ("bauhaus", 1000f, 0f), ("jaffa", 1500f, 0f), ("moshava", 800f, 0f),
        ("olive", 600f, 0f), ("citrus", 450f, 0f), ("palm", 1100f, 0f), ("figure", 700f, 0f),
    };

    public static void Row()
    {
        var args = System.Environment.GetCommandLineArgs();
        string Arg(string n) { int i = System.Array.IndexOf(args, n); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var dir = Arg("-captureDir") ?? Path.Combine(Paths.RepoRoot, "captures", "visual", "ai3d");
        float yawAll = Arg("-modelYaw") is string y ? float.Parse(y, CultureInfo.InvariantCulture) : 180f;
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(0, true);
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = MapCapture.RequireMapScene(); map.Build();
            var rig = Camera.main.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            var terrain = map.Terrain;
            var cam = Camera.main.transform;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var toward = -Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var centre = rig.LookAt + toward * 6000f;
            var root = new GameObject("AI3D");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            int tris = 0;
            var placed = new List<(string, GameObject)>();
            for (int i = 0; i < Models.Length; i++)
            {
                var (name, height, yaw) = Models[i];
                var mesh = Load(name, out var tex); if (mesh == null) { Debug.LogWarning($"AI3D missing {name}"); continue; }
                var mat = new Material(shader) { enableInstancing = true };
                mat.SetTexture("_BaseMap", tex); mat.SetFloat("_Smoothness", 0.1f); mat.SetFloat("_Metallic", 0f);
                // TRELLIS meshes are open shells with some flipped faces left after post.py's winding fix: draw both sides
                mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); mat.doubleSidedGI = true;
                var p = centre + right * ((i - (Models.Length - 1) / 2f) * 2600f);
                // a few copies: the hero model plus two smaller ones behind it, to read as a cluster
                for (int k = 0; k < 3; k++)
                {
                    var q = p + (k == 0 ? Vector3.zero : (k == 1 ? -toward * 1400f + right * 600f : -toward * 1400f - right * 700f));
                    q.y = terrain.SampleHeight(q) + terrain.transform.position.y;
                    var go = new GameObject($"{name}_{k}");
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                    go.transform.SetParent(root.transform);
                    go.transform.position = q;
                    float s = height * (k == 0 ? 1f : 0.8f);
                    go.transform.localScale = new Vector3(s, s, s);
                    go.transform.rotation = Quaternion.LookRotation(toward, Vector3.up) * Quaternion.Euler(0, yawAll + yaw + k * 35f, 0);
                    tris += mesh.triangles.Length / 3;
                }
                placed.Add((name, root.transform.GetChild(root.transform.childCount - 3).gameObject));
            }
            Debug.Log($"AI3D placed {root.transform.childCount} instances, {tris} triangles");
            var poses = new[]
            {
                PromoPose.Gameplay03, new PromoPose("close30", 34.86, 32.00, 30000f, 10f),
                new PromoPose("hero12", 34.86, 32.00, 12000f, 10f),
            };
            foreach (var pose in poses)
            {
                var pp = new PromoPose(pose.Name, pose.Lon, pose.Lat, pose.Distance, pose.Yaw);
                pp.Apply(rig, map.Frame);
                // aim at the row, not the pose centre
                rig.LookAt = centre; rig.Apply();
                root.SetActive(true); Save(Camera.main, 1920, 1080, Path.Combine(dir, $"ai3d-{pose.Name}.png"));
                root.SetActive(false); Save(Camera.main, 1920, 1080, Path.Combine(dir, $"base-{pose.Name}.png"));
                root.SetActive(true);
            }
            // one 6 km close-up per model (the hero instance), with its procedural neighbours in frame
            foreach (var (name, go) in placed)
            {
                rig.LookAt = go.transform.position; rig.Distance = 6000f; rig.Yaw = PromoPose.Gameplay03.Yaw; rig.Apply();
                Save(Camera.main, 1920, 1080, Path.Combine(dir, $"ai3d-hero06-{name}.png"));
            }
            // straight promo framing (look point unchanged) for the side-by-side with promo 03
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            Save(Camera.main, 1920, 1080, Path.Combine(dir, "ai3d-gameplay03-promoframe.png"));
        }
        finally { QualitySettings.SetQualityLevel(saved, true); }
    }

    static Mesh Load(string name, out Texture2D tex)
    {
        tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/AI3D/{name}/{name}.png");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/AI3D/{name}/{name}.obj");
        if (model == null) return null;
        return model.GetComponentInChildren<MeshFilter>().sharedMesh;
    }

    static void Save(Camera cam, int w, int h, string path)
    {
        var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var t = new Texture2D(w, h, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, t.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(t);
    }
}
