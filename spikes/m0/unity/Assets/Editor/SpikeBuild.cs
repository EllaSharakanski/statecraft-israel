using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class SpikeBuild
{
    const string ScenePath = "Assets/Spike/Spike.unity";

    public static void MakeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        AssetDatabase.CreateAsset(ps, "Assets/Spike/SpikePanel.asset");
        var boot = new GameObject("SpikeBoot").AddComponent<SpikeBoot>();
        boot.panelSettings = ps;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
        AssetDatabase.CreateAsset(mat, "Assets/Spike/HexMat.mat");
        boot.hexMaterial = mat;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    public static void BuildWeb()
    {
        MakeScene();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // plain http.server can serve it
        Report(BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "../build/web", target = BuildTarget.WebGL }));
    }

    public static void BuildMac()
    {
        MakeScene();
        // Standalone defaults to Mono; (c) must be proven under IL2CPP stripping on Mac too.
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        Report(BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "../build/mac/Spike.app", target = BuildTarget.StandaloneOSX }));
    }

    static void Report(UnityEditor.Build.Reporting.BuildReport r)
    {
        Debug.Log($"SPIKE build {r.summary.platform}: {r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize}");
        if (r.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
