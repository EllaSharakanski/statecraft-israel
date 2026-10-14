#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The End Turn outcome clips (decision videos, phase 3; owner rulings 2026-10-13/14; docs/event-videos.md). When
    /// a turn has run, each event the player answered with its historical option, in an order the turn applied (it is in
    /// GameState.ChosenOptions with that option and was not refused), and that has an outcome clip, plays once: a 3.5 s
    /// silent clip in the middle of the screen, captioned as the order ("Your order: ..."), never as a result. A delegated
    /// answer, a counterfactual option and Deport get none (EventVideoMapping.OutcomeProblems refuses such rows). Several
    /// play one after the other. A click, Enter or Space skips one; Esc skips them all. While one plays the decision card
    /// and the panels under it wait (ScreenNav.DisableCovered, OpenClass). Not made at all when video is off
    /// (CardVideo.ForcedOff: every automated run).</summary>
    public sealed class OutcomePresenter : IDisposable
    {
        /// <summary>On the "root" element while an outcome clip shows.</summary>
        public const string OpenClass = "outcome-open";
        /// <summary>How long past the clip's own length the overlay waits for it before moving on (a stalled stream).</summary>
        public const long GraceMs = 6000;

        readonly VisualElement hudRoot, layer, media; readonly Label caption;
        readonly SimBridge bridge; readonly UiText text; readonly CardVideo video;
        readonly Dictionary<(string ev, string option), EventVideoMapping.Row> clips;
        readonly Action? closed;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly EventCallback<PointerDownEvent> onPointer;
        readonly EventCallback<NavigationSubmitEvent> onSubmit;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly Queue<(EventVideoMapping.Row row, string order)> queue = new Queue<(EventVideoMapping.Row, string)>();
        IVisualElementScheduledItem? guard;

        /// <summary>The clip on screen now, or null.</summary>
        public EventVideoMapping.Row? Showing { get; private set; }

        int closedAt = -1;
        /// <summary>True in the frame the overlay closed: GameRoot's Escape poll then leaves the card alone (the same Escape
        /// key already skipped the clips through the Cancel event).</summary>
        public bool ClosedThisFrame => closedAt == UnityEngine.Time.frameCount;

        /// <summary>The caption of the clip on screen ("Your order: ..."), or "".</summary>
        public string Caption => caption.text;

        /// <param name="closed">Told when the last clip of a turn is done (GameRoot re-renders the decision card).</param>
        public OutcomePresenter(VisualElement root, SimBridge bridge, UiText text, CardVideo video, IEnumerable<EventVideoMapping.Row> rows, Action? closed = null)
        {
            this.bridge = bridge; this.text = text; this.video = video; this.closed = closed;
            clips = rows.ToDictionary(r => (r.Event, r.Option));
            hudRoot = root.Q("root") ?? root;
            layer = root.Q<VisualElement>("outcome-layer");
            media = layer.Q<VisualElement>("outcome-media");
            caption = layer.Q<Label>("outcome-caption");
            layer.Q<Label>("outcome-hint").text = text["outcome.skip"];
            layer.Q<Label>("outcome-label").text = text["event.ai_label"];
            layer.focusable = true; layer.tabIndex = 0;
            onTurn = _ => Turn();
            onPointer = _ => Skip();
            onSubmit = e => { Skip(); e.StopPropagation(); };
            onCancel = e => { SkipAll(); e.StopPropagation(); };
            media.RegisterCallback<GeometryChangedEvent>(_ =>   // 16:9 of its width (USS has no aspect ratio)
            {
                var w = media.layout.width;
                if (w > 0 && UnityEngine.Mathf.Abs(media.layout.height - w / EventCardPresenter.MediaAspect) > 0.5f)
                    media.style.height = UnityEngine.Mathf.Round(w / EventCardPresenter.MediaAspect);
            });
            bridge.TurnEnded += onTurn;
            layer.RegisterCallback(onPointer);
            layer.RegisterCallback(onSubmit);
            layer.RegisterCallback(onCancel);
        }

        /// <summary>The turn just run: queue the clip of every historical order it applied, then play the first.</summary>
        void Turn()
        {
            var c = bridge.Content; var s = bridge.State;
            foreach (var order in bridge.LastBatch.OfType<ChooseEventOption>())
            {
                if (!clips.TryGetValue((order.EventId, order.OptionId), out var row)) continue;
                if (!s.ChosenOptions.TryGetValue(order.EventId, out var applied) || applied != order.OptionId) continue;   // refused or replaced
                var ev = c.Events.FirstOrDefault(e => e.Id == order.EventId);
                var option = ev?.Options.FirstOrDefault(o => o.Id == order.OptionId);
                if (option == null || !option.Historical) continue;   // the mapping refuses this too
                queue.Enqueue((row, text.Format("outcome.order", Cabinet.Fill(c, s, option.Text, s.Date))));
            }
            if (Showing == null && queue.Count > 0) Next();
        }

        void Next()
        {
            guard?.Pause(); guard = null;
            if (queue.Count == 0) { Close(); return; }
            var (row, order) = queue.Dequeue();
            Showing = row;
            caption.text = order;
            media.tooltip = row.Alt;
            media.style.backgroundImage = StyleKeyword.None;
            hudRoot.AddToClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
            layer.Focus();
            var shown = row;
            video.Play(CardVideo.Url(row.File), media, _ => { if (Showing == shown) Next(); }, loop: false, ended: () => { if (Showing == shown) Next(); });
            guard = layer.schedule.Execute(() => { if (Showing == shown) Next(); }).StartingIn(row.Frames * 1000L / 24 + GraceMs);
        }

        /// <summary>Skips the clip on screen (a click, Enter, Space).</summary>
        public void Skip() { if (Showing != null) { video.Stop(); Next(); } }

        /// <summary>Skips every clip of this turn (Esc). True when one was showing.</summary>
        public bool SkipAll()
        {
            if (Showing == null) return false;
            queue.Clear();
            video.Stop();
            Close();
            return true;
        }

        void Close()
        {
            guard?.Pause(); guard = null;
            var was = Showing != null;
            Showing = null; caption.text = "";
            hudRoot.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
            if (was) { closedAt = UnityEngine.Time.frameCount; layer.Blur(); closed?.Invoke(); }
        }

        public void Dispose()
        {
            bridge.TurnEnded -= onTurn;
            layer.UnregisterCallback(onPointer);
            layer.UnregisterCallback(onSubmit);
            layer.UnregisterCallback(onCancel);
            queue.Clear();
            if (Showing != null) video.Stop();
            Close();
        }
    }
}
