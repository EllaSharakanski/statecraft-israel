#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Statecraft.Game.Boot;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The web tier's runtime quality fallback (issue #44; visual v1 Deferred row "Runtime quality fallback"):
    /// when the frame-time p95 stays over the 30 fps floor, lower the render scale, then turn the tilt-shift off; when it
    /// stays comfortably under, step back up in reverse. Pure logic (frame times in, a step out), so tests drive it with
    /// synthetic frames; QualityFallbackDriver feeds it the player's frames and applies its step. Presentation only: it
    /// reads nothing from the sim and changes no sim state, so turns, hashes and saves are the same on every step.
    ///
    /// Why these numbers (docs/visual-v1.md "Runtime quality fallback"):
    /// - Window 3 s: the p95 of a 3 s window needs at least 5% of the frames slow (5 of 90 at 30 fps), so a one-off hitch
    ///   (an End Turn redraw, a GC, a first shader compile: 1-2 frames) never trips it, while a real shortfall (every frame
    ///   slow) does within 3 s. Windows are back to back; one decision per window.
    /// - Step down when a window's p95 is over the 30 fps floor (33 ms, research notes 2.3) by more than the WebGL clock's
    ///   resolution: p95 > OverMs = 34 ms. The WebGL frame clock is quantised to about 1 ms, so exactly 30 fps (a display
    ///   or a battery saver capping the page at 30 Hz, or vsync halving 60 Hz) reads as 33 and 34 ms frames: that meets
    ///   the floor, and lowering the quality could not raise a capped rate anyway.
    /// - Step up only after RecoverWindows windows in a row with p95 &lt; 20 ms. 20 ms is the hysteresis: one step up
    ///   (0.8 -> 0.9 or 0.9 -> 1.0) draws at most 1.27 times the pixels, so a fill-bound frame at 20 ms lands at about 25 ms,
    ///   under the floor; the tilt-shift costs under 1.5 ms. Between 20 and 34 ms nothing changes (the dead band).
    /// - Settle: frames in the first 0.5 s after a change are not measured (the change reallocates render targets), nor
    ///   the first 3 s after start (scene load and shader compiles).
    /// - A frame over 1 s is a pause (a hidden tab, a stalled load), not load: it drops the window.
    /// - Anti-oscillation: a step up that is undone within two windows doubles the windows the next step up needs
    ///   (3, 6, 12 ... at most 48, 2.4 minutes), so a device on the edge settles instead of flipping every 10 s.</summary>
    public sealed class QualityFallback
    {
        public readonly struct Step
        {
            public readonly float RenderScale; public readonly bool TiltShift;
            public Step(float renderScale, bool tiltShift) { RenderScale = renderScale; TiltShift = tiltShift; }
            public override string ToString() => string.Format(CultureInfo.InvariantCulture, "scale={0:0.00} tiltshift={1}", RenderScale, TiltShift ? "on" : "off");
        }

        /// <summary>Level 0 is the tier's own look; each level down costs less. Render scale first, within the research's
        /// 0.8-1.0 dynamic range (notes 2.3), then the tilt-shift.</summary>
        public static readonly IReadOnlyList<Step> Steps = new[] { new Step(1f, true), new Step(0.9f, true), new Step(0.8f, true), new Step(0.8f, false) };

        public const float FloorMs = 33f, OverMs = 34f, RecoverMs = 20f, WindowSeconds = 3f, SettleSeconds = 0.5f, StartupSeconds = 3f, PauseMs = 1000f;
        public const int MinWindowFrames = 10, RecoverWindows = 3, MaxRecoverWindows = 48, FailedRecoveryWindows = 2;

        public int Level { get; private set; }
        public Step Current => Steps[Level];
        /// <summary>The p95 of the last complete window (NaN before the first).</summary>
        public float LastP95 { get; private set; } = float.NaN;
        /// <summary>Windows measured so far (diagnostics and tests).</summary>
        public int Windows { get; private set; }
        /// <summary>Windows in a row under RecoverMs that the next step up needs (doubles after a failed step up).</summary>
        public int RecoverNeeded { get; private set; } = RecoverWindows;

        readonly List<float> window = new List<float>();
        float windowMs, settleMs = StartupSeconds * 1000f;
        int goodWindows, windowsSinceStepUp = int.MaxValue;

        /// <summary>Feeds one frame's time in ms; true when it moved Level (the caller then applies Current).</summary>
        public bool Feed(float frameMs)
        {
            if (float.IsNaN(frameMs) || frameMs < 0f) return false;
            if (frameMs > PauseMs) { window.Clear(); windowMs = 0f; return false; }
            if (settleMs > 0f) { settleMs -= frameMs; return false; }
            window.Add(frameMs); windowMs += frameMs;
            if (windowMs < WindowSeconds * 1000f) return false;
            bool enough = window.Count >= MinWindowFrames;
            var p95 = enough ? FrameStats.Summary(window).p95 : float.NaN;
            window.Clear(); windowMs = 0f;
            if (!enough) return false;
            LastP95 = p95; Windows++;
            if (windowsSinceStepUp < int.MaxValue) windowsSinceStepUp++;
            if (p95 > OverMs)
            {
                goodWindows = 0;
                if (Level == Steps.Count - 1) return false;
                if (windowsSinceStepUp <= FailedRecoveryWindows) RecoverNeeded = Math.Min(RecoverNeeded * 2, MaxRecoverWindows);
                windowsSinceStepUp = int.MaxValue;
                return Move(+1);
            }
            if (p95 >= RecoverMs || Level == 0) { goodWindows = 0; return false; }
            if (++goodWindows < RecoverNeeded) return false;
            goodWindows = 0; windowsSinceStepUp = 0;
            return Move(-1);
        }

        bool Move(int by) { Level += by; settleMs = SettleSeconds * 1000f; return true; }

        /// <summary>Whether the fallback runs: the WebGL tier only (the desktop tier is the development and capture target, and
        /// in the editor a render-scale change would write the URP asset), and never under an instrument that must see the
        /// tier's own look (?vprofile, ?profile, ?shot) or with ?nofallback.</summary>
        public static bool Wanted(string? url, bool webgl) =>
            webgl && !UrlFlags.Has(url, "vprofile") && !UrlFlags.Has(url, "profile") && !UrlFlags.Has(url, "shot") && !UrlFlags.Has(url, "nofallback");

        public static bool Wanted() => Wanted(Application.absoluteURL, Application.platform == RuntimePlatform.WebGLPlayer);

        public string Line() => string.Format(CultureInfo.InvariantCulture, "QUALITY level={0} {1} p95_ms={2:0.00}", Level, Current, LastP95);
    }

    /// <summary>Feeds QualityFallback the player's frame times (unscaled, so a paused game still counts) and applies its step:
    /// the active URP asset's render scale (never above the tier's own) and TiltShift.FallbackOff. CameraRig.Init attaches it
    /// to the map camera through Attach, whose undo (run by the map's teardown) removes it and puts both back.</summary>
    public sealed class QualityFallbackDriver : MonoBehaviour
    {
        public QualityFallback Controller { get; } = new QualityFallback();
        float tierScale = 1f;
        Action<float> setScale = _ => { };

        /// <summary>Adds the driver to <paramref name="camera"/> when <paramref name="wanted"/> (QualityFallback.Wanted());
        /// returns the undo for the map's teardown (a no-op when not wanted).</summary>
        public static Action Attach(GameObject camera, bool wanted)
        {
            if (!wanted) return () => { };
            var asset = UniversalRenderPipeline.asset;
            var driver = camera.AddComponent<QualityFallbackDriver>();
            driver.tierScale = asset != null ? asset.renderScale : 1f;
            driver.setScale = s => { if (asset != null) asset.renderScale = s; };
            var tierScale = driver.tierScale;
            return () =>
            {
                if (asset != null) asset.renderScale = tierScale;
                TiltShift.FallbackOff = false;
                if (driver != null) { if (Application.isPlaying) Destroy(driver); else DestroyImmediate(driver); }
            };
        }

        /// <summary>Feeds one frame (tests call it; Update feeds the real frames).</summary>
        public void Feed(float frameMs)
        {
            if (!Controller.Feed(frameMs)) return;
            var step = Controller.Current;
            setScale(Mathf.Min(tierScale, step.RenderScale));
            TiltShift.FallbackOff = !step.TiltShift;
            Debug.Log(Controller.Line());
        }

        void Update() => Feed(Time.unscaledDeltaTime * 1000f);
    }
}
