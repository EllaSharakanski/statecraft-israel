#nullable enable
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The start screen's "Play the recording" button: David Ben-Gurion reading the sentence of the Declaration of
    /// Independence that proclaims the state, from the Kol Yisrael broadcast of 14 May 1948, Israel State Archives item
    /// ISA-Collections-Recordings-00113hj (owner rulings 2026-10-13/14; docs/declaration-recording.md).
    /// The file ships unaltered in StreamingAssets (its SHA-256 is pinned in a test) and is fetched only when the player
    /// presses the button, so it costs the boot nothing and Unity never re-encodes it: the browser decodes it. It plays only
    /// from that press, never by itself, never on Continue or New Game; it stops when the start screen closes. While it
    /// plays, its words show as captions over the painting, in Hebrew (right to left) and in English. The credit line
    /// "Recording: Israel State Archives" sits beside the button. In automated runs the button is not shown (no audio).</summary>
    public sealed class DeclarationRecording : IDisposable
    {
        /// <summary>The recording's path under StreamingAssets, and the SHA-256 of the file as received from the archive's
        /// copy (DeclarationRecordingTests checks the shipped file against it).</summary>
        public const string File = "audio/declaration-1948-isa-00113hj.wav";
        public const string Sha256 = "2b949e12f1d33d6f2dd6509aeceeac318857396174c579286c04fbc8dbf65e1d";

        readonly Button button; readonly VisualElement caption, row; readonly UiText text;
        readonly Action onPress;
        GameObject? go; AudioSource? source; AudioClip? clip; UnityWebRequest? request;
        IVisualElementScheduledItem? watch;

        /// <summary>True from the press until the recording ends or is stopped (including while it is fetched).</summary>
        public bool Playing { get; private set; }

        /// <param name="off">No audio (automated runs, CardVideo.ForcedOff): the button and its credit are not shown.</param>
        public DeclarationRecording(VisualElement root, UiText text, bool off)
        {
            this.text = text;
            row = root.Q<VisualElement>("start-recording");
            button = root.Q<Button>("start-listen");
            caption = root.Q<VisualElement>("start-caption");
            button.text = text["start.listen"];
            onPress = Toggle;
            button.clicked += onPress;
            row.style.display = off ? DisplayStyle.None : DisplayStyle.Flex;
            caption.style.display = DisplayStyle.None;
        }

        /// <summary>The button: play, or stop when playing.</summary>
        public void Toggle()
        {
            if (Playing) { Stop(); return; }
            Playing = true;
            button.text = text["start.stop"];
            caption.style.display = DisplayStyle.Flex;
            if (clip != null) { Begin(); return; }
            var url = Application.streamingAssetsPath + "/" + File;
            if (!url.Contains("://")) url = "file://" + url;
            request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            var sent = request;
            sent.SendWebRequest().completed += _ =>
            {
                if (sent != request) { sent.Dispose(); return; }   // stopped, or pressed again, meanwhile
                request = null;
                if (sent.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"RECORDING failed {sent.url}: {sent.error}");
                    sent.Dispose(); Stop(); return;
                }
                clip = DownloadHandlerAudioClip.GetContent(sent);
                sent.Dispose();
                if (Playing) Begin();
            };
        }

        void Begin()
        {
            if (go == null)
            {
                go = new GameObject("DeclarationRecording");
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
            }
            source!.clip = clip;
            source.Play();
            Debug.Log("RECORDING playing " + File);
            watch?.Pause();
            watch = button.schedule.Execute(() => { if (Playing && source != null && !source.isPlaying) Stop(); }).Every(200).StartingIn(500);
        }

        /// <summary>Stops it (the start screen closing, the button). Safe to call when it is not playing.</summary>
        public void Stop()
        {
            request?.Abort(); request = null;
            watch?.Pause(); watch = null;
            if (source != null) source.Stop();
            Playing = false;
            button.text = text["start.listen"];
            caption.style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            Stop();
            button.clicked -= onPress;
            if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
            go = null; source = null;
        }
    }
}
