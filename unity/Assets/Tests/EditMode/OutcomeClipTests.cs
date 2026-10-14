#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Decision videos, phase 3 (owner rulings 2026-10-13/14; docs/event-videos.md): an outcome clip plays at End
/// Turn only for the historical option the player ordered and the turn applied, captioned as the order; never for a
/// counterfactual option, a delegated answer or Deport; a click, Enter or Space skips it and Esc skips them all; while it
/// plays the card and the panels wait; a sensitive one waits for the owner's review before a shareable build.</summary>
public class OutcomeClipTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static string Yaml => File.ReadAllText(EventVideoMapping.Path);

    [Test]
    public void EveryOutcomeIsAHistoricalOrderWithItsClipAndNoDeport()
    {
        var rows = EventVideoMapping.ParseOutcomes(Yaml);
        Assert.AreEqual(5, rows.Count);
        var problems = EventVideoMapping.OutcomeProblems(rows, C, f => File.Exists(Path.Combine(EventVideoMapping.ClipFolder, f)));
        Assert.IsEmpty(problems, string.Join("\n", problems));
        Assert.IsNotEmpty(EventVideoMapping.LoadOutcomes(), "the game finds them in Resources");
    }

    [Test]
    public void TheMappingRefusesACounterfactualOrderAndDeport()
    {
        var row = EventVideoMapping.ParseOutcomes(Yaml).First(r => r.Event == "law_of_return");
        var quota = new EventVideoMapping.Row { Event = row.Event, Option = "quota", File = row.File, Frames = 84, Source = row.Source, SourceSha256 = row.SourceSha256, Generator = "x", Generated = "x", Alt = row.Alt, AiGenerated = true };
        Assert.IsTrue(EventVideoMapping.OutcomeProblems(new[] { quota }, C, _ => true).Any(p => p.Contains("not the historical option")));
        var c = ContentLoader.Load(Paths.Content);
        c.Events.First(e => e.Id == "law_of_return").Options.Add(new EventOption { Id = "deport", Text = "x" });
        Assert.IsTrue(EventVideoMapping.OutcomeProblems(new[] { row }, c, _ => true).Any(p => p.Contains("Deport never gets any video")));
    }

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui; public readonly SimBridge Bridge; public readonly OutcomePresenter Outcomes; public readonly CardVideo Video;
        public int Closed;
        public Rig(string eventId)
        {
            Ui = new UiTestKit();
            Ui.Root.Q("root").RemoveFromClassList("loading");
            var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            g.State.Date = C.Events.First(e => e.Id == eventId).From;
            g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
            Bridge = new SimBridge(g, Text);
            Video = new CardVideo();
            Outcomes = new OutcomePresenter(Ui.Root, Bridge, Text, Video, EventVideoMapping.ParseOutcomes(Yaml), () => Closed++);
        }
        public bool Open => Ui.Root.Q("root").ClassListContains(OutcomePresenter.OpenClass);
        public VisualElement Layer => Ui.Root.Q("outcome-layer");
        public void Dispose() { Outcomes.Dispose(); Video.Dispose(); Ui.Dispose(); }
    }

    [Test]
    public void TheHistoricalOrderPlaysAtEndTurnCaptionedAsTheOrder()
    {
        LogAssert.ignoreFailingMessages = true;   // the edit-mode VideoPlayer may log
        using var r = new Rig("law_of_return");
        Assert.IsNull(r.Bridge.Queue(new ChooseEventOption("law_of_return", "enact")), "the order is taken");
        Assert.IsFalse(r.Open, "nothing at the click: the order takes effect at End Turn");
        r.Bridge.EndTurn();
        Assert.AreEqual("law_of_return", r.Outcomes.Showing?.Event);
        Assert.AreEqual("Your order: Enact the right of every Jew to immigrate", r.Outcomes.Caption);
        Assert.IsTrue(r.Open);
        Assert.IsFalse(r.Ui.Root.Q("top-bar").enabledSelf, "the bar waits while it plays");
        Assert.IsFalse(r.Ui.Root.Q("info-panel").enabledSelf);
        Assert.AreEqual("AI-generated illustration", r.Ui.Q<Label>("outcome-label").text);
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = r.Layer; r.Layer.SendEvent(e); }   // Enter or Space skips it
        Assert.IsNull(r.Outcomes.Showing);
        Assert.IsFalse(r.Open);
        Assert.IsTrue(r.Ui.Root.Q("top-bar").enabledSelf, "the bar is back");
        Assert.AreEqual(1, r.Closed, "the card is told, to bring its own picture back");
    }

    [Test]
    public void ACounterfactualOrderOrADelegatedAnswerGetsNoClip()
    {
        using (var r = new Rig("law_of_return"))
        {
            r.Bridge.Queue(new ChooseEventOption("law_of_return", "quota"));
            r.Bridge.EndTurn();
            Assert.IsNull(r.Outcomes.Showing, "counterfactual: no clip");
            Assert.IsFalse(r.Open);
        }
        using (var r = new Rig("law_of_return"))
        {
            r.Bridge.EndTurn();   // unanswered: the minister decides; the player gave no order
            Assert.IsTrue(r.Bridge.State.ChosenOptions.ContainsKey("law_of_return"), "precondition: it was delegated");
            Assert.IsNull(r.Outcomes.Showing, "delegated: no clip");
        }
    }

    [Test]
    public void EscapeSkipsThemAllAndAClickSkipsOne()
    {
        LogAssert.ignoreFailingMessages = true;
        using (var r = new Rig("hula_drainage"))
        {
            r.Bridge.Queue(new ChooseEventOption("hula_drainage", "continue_works"));
            r.Bridge.EndTurn();
            Assert.AreEqual("hula_drainage", r.Outcomes.Showing?.Event);
            using (var e = NavigationCancelEvent.GetPooled()) { e.target = r.Layer; r.Layer.SendEvent(e); }
            Assert.IsNull(r.Outcomes.Showing, "Esc skips all");
            Assert.IsFalse(r.Open);
        }
        using (var r = new Rig("first_truce"))
        {
            r.Bridge.Queue(new ChooseEventOption("first_truce", "accept"));
            r.Bridge.EndTurn();
            Assert.AreEqual("first_truce", r.Outcomes.Showing?.Event);
            r.Outcomes.Skip();   // what a click on the backdrop does
            Assert.IsNull(r.Outcomes.Showing);
        }
    }

    [Test]
    public void TheSensitiveHorevClipWaitsForTheOwnersReview()
    {
        var gate = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        var all = EventVideoMapping.Parse(Yaml).Concat(EventVideoMapping.ParseOutcomes(Yaml)).ToList();
        var refusal = (string?)gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, C, null, false, null, all });
        StringAssert.Contains("illustration clips: operation_horev, operation_yoav, ten_days", refusal ?? "");
    }
}
