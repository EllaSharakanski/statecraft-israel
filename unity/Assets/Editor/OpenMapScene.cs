#nullable enable
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Statecraft.Game.EditorTools
{
    /// <summary>A fresh clone opens on Unity's empty "Untitled" scene, and new contributors could not find the game
    /// (it is Assets/Map.unity, the boot scene). On editor load this opens the map once per session, and only when
    /// nothing the user made could be lost: one untitled, unedited scene, outside play mode and batch mode.</summary>
    [InitializeOnLoad]
    public static class OpenMapScene
    {
        public const string MapScenePath = "Assets/Map.unity";
        const string SessionKey = "Statecraft.OpenMapScene.Done";

        static OpenMapScene() => EditorApplication.delayCall += TryOpen;

        public static bool ShouldOpen(string activeScenePath, bool activeSceneDirty, int loadedScenes, bool isPlaying,
            bool isBatchMode, bool alreadyDoneThisSession) =>
            !alreadyDoneThisSession && !isBatchMode && !isPlaying && loadedScenes == 1
            && string.IsNullOrEmpty(activeScenePath) && !activeSceneDirty;

        static void TryOpen()
        {
            var active = SceneManager.GetActiveScene();
            var open = ShouldOpen(active.path, active.isDirty, SceneManager.sceneCount,
                EditorApplication.isPlayingOrWillChangePlaymode, UnityEngine.Application.isBatchMode,
                SessionState.GetBool(SessionKey, false));
            SessionState.SetBool(SessionKey, true);
            if (open && AssetDatabase.LoadAssetAtPath<SceneAsset>(MapScenePath) != null)
                EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        }
    }
}
