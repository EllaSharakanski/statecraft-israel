#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The first thing Map.unity runs: loads the game inputs (fetched on WebGL, read from disk elsewhere), builds
    /// the map, then raises Ready for the game (GameRoot) to start. A failure is logged ("BOOT FAIL ...") and kept in Error.</summary>
    public sealed class GameBoot : MonoBehaviour
    {
        [SerializeField] MapScene map = null!;

        public GameInputs? Inputs { get; private set; }
        public string? Error { get; private set; }
        public event Action<GameInputs>? Ready;

        public void Configure(MapScene mapScene) => map = mapScene;

        async void Start()
        {
            try
            {
                Inputs = Application.platform == RuntimePlatform.WebGLPlayer
                    ? await GameInputs.FetchAsync(GameInputs.StreamingUrl())
                    : GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
                map.Build(Inputs.Map);
                Debug.Log($"BOOT OK {Inputs.Content.Events.Count} events");
                Ready?.Invoke(Inputs);
            }
            catch (Exception e)
            {
                Error = e.Message;
                Debug.LogError("BOOT FAIL " + e.Message);
                Debug.LogException(e);
            }
        }
    }
}
