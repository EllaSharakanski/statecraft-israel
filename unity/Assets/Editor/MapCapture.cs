using System.IO;
using Statecraft.Game;
using Statecraft.Game.EditorTools;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor capture tools. Run headless (with graphics, not -nographics):
/// Unity -batchmode -quit -projectPath unity -executeMethod MapCapture.TopDown (or MapCapture.Flythrough).
/// Captures go to captures/ at the repo root, which git ignores.</summary>
public static class MapCapture
{
    const string ScenePath = "Assets/Map.unity";

    static string CaptureDir => Path.Combine(Paths.RepoRoot, "captures");

    /// <summary>Creates and saves Assets/Map.unity: default camera and light plus a MapScene wired to the material assets.</summary>
    public static void MakeScene()
    {
        MapMaterials.Create();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("MapScene").AddComponent<MapScene>().Configure(
            Load(MapMaterials.TerrainPath), Load(MapMaterials.OverlayPath), Load(MapMaterials.MarkerPath));
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    /// <summary>Opens Assets/Map.unity (creating it only when missing) and builds the map in memory. The scene is not
    /// saved, so captures leave it unchanged.</summary>
    static MapScene OpenAndBuild()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) MakeScene();
        else EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var map = RequireMapScene(); map.Build();
        return map;
    }

    /// <summary>The open scene's MapScene; throws naming the scene and the fix when there is none.</summary>
    public static MapScene RequireMapScene()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        if (map == null)
            throw new System.InvalidOperationException($"{ScenePath} has no MapScene component. Delete {ScenePath} so the next " +
                "capture recreates it, or run MapCapture.MakeScene.");
        return map;
    }

    static Material Load(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) throw new FileNotFoundException("material asset missing", path);
        return m;
    }

    /// <summary>Orthographic straight-down render of the whole map, for comparison with promo/ref/geo-reference.png.</summary>
    public static void TopDown()
    {
        var map = OpenAndBuild();
        var cam = new GameObject("TopCam").AddComponent<Camera>();
        var size = map.Terrain.terrainData.size;
        cam.orthographic = true; cam.orthographicSize = size.z / 2; cam.aspect = size.x / size.z;
        cam.transform.position = new Vector3(size.x / 2, 100000, size.z / 2); cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        cam.farClipPlane = 300000;
        Save(cam, Mathf.RoundToInt(2000 * size.x / size.z), 2000, Path.Combine(CaptureDir, "topdown.png"));
    }

    /// <summary>240 frames flying north along the coast at diorama pitch; encode with ffmpeg.</summary>
    public static void Flythrough()
    {
        var map = OpenAndBuild();
        var rig = Camera.main.GetComponent<CameraRig>();
        var dir = Path.Combine(CaptureDir, "fly"); Directory.CreateDirectory(dir);
        for (int i = 0; i < 240; i++)
        {
            double lat = 31.3 + (32.9 - 31.3) * i / 239.0; // from the northern Negev to the Carmel
            rig.LookAt = map.Frame.ToWorld(34.95, lat, 0); rig.Distance = 90000f; rig.Apply();
            Save(Camera.main, 1920, 1080, Path.Combine(dir, $"{i:D4}.png"));
        }
    }

    static void Save(Camera cam, int w, int h, string path)
    {
        var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
    }
}
