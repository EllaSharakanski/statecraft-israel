using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

/// <summary>The M3 player settings (ProjectSetup.Apply) and the custom WebGL template.</summary>
public class ProjectSettingsTests
{
    [Test]
    public void PlayersUseIl2CppTheStatecraftTemplateAndMapUnityAsTheBootScene()
    {
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.WebGL));
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
        Assert.AreEqual("PROJECT:Statecraft", PlayerSettings.WebGL.template);
        Assert.IsFalse(PlayerSettings.WebGL.decompressionFallback);
        Assert.AreEqual(WebGLCompressionFormat.Disabled, PlayerSettings.WebGL.compressionFormat);
        Assert.AreEqual(new[] { "Assets/Map.unity" }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
    }

    [Test]
    public void TheNewSceneTemplateDoesNotPointAtTheDeletedSampleScene()
    {
        var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        Assert.AreEqual("Assets/Map.unity", player.FindProperty("templateDefaultScene").stringValue);
    }

    [Test]
    public void TheTemplateFillsTheWindowAndPersistsSaves()
    {
        var html = File.ReadAllText("Assets/WebGLTemplates/Statecraft/index.html");
        StringAssert.Contains("#unity-canvas { position: fixed; left: 0; top: 0; width: 100%; height: 100%;", html);
        StringAssert.Contains("autoSyncPersistentDataPath: true", html);
        StringAssert.DoesNotContain("alert(", html);
    }

    [Test]
    public void TemplatePackagesAreGone()
    {
        var manifest = File.ReadAllText("Packages/manifest.json");
        foreach (var p in new[] { "com.unity.ai.navigation", "com.unity.collab-proxy", "com.unity.multiplayer.center",
                                  "com.unity.visualscripting", "com.unity.timeline", "com.unity.ugui" })
            StringAssert.DoesNotContain("\"" + p + "\"", manifest);
        Assert.IsFalse(File.Exists("Assets/Scenes/SampleScene.unity"));
    }
}
