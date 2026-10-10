using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class WhyScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void EveryCauseOfTheLastTurnIsListedUnderItsSystem()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        new WhyPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("Nothing has happened yet: end the turn to see its causes.", ui.Q<Label>("why-empty").text);
        bridge.Queue(new SetMobilization(1.0));
        var r = bridge.EndTurn();
        Assert.AreEqual("15 May 1948 to 22 May 1948", ui.Q<Label>("why-turn").text);
        var lines = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
        Assert.AreEqual(r.Why.Count, lines.Count);
        Assert.AreEqual("Immigration and population", ui.Q<Label>("why-group-population").text);
        Assert.IsTrue(lines.Any(l => l.text == "reserve call-up set to 100%"));
        var groups = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-group").ToList().Select(l => l.name).ToList();
        Assert.Less(groups.IndexOf("why-group-security"), groups.IndexOf("why-group-economy"));
    }

    [Test]
    public void TheLinesAreTheSimsOwnMessagesInGroupOrder()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        new WhyPresenter(ui.Root, bridge, Text);
        var r = bridge.EndTurn();
        var expected = WhyPresenter.Order.SelectMany(sys => r.Why.Where(w => w.System == sys).Select(w => w.Message)).ToList();
        var shown = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Select(l => l.text).ToList();
        Assert.AreEqual(expected, shown);
    }

    [Test]
    public void EverySystemTheSimReportsHasAGroupName()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 80 && g.State.GameOver == null; i++)
        {
            g.Step(HistoricalAI.Commands(C, g.State));
            foreach (var w in g.Last!.Why) seen.Add(w.System);
        }
        CollectionAssert.IsSubsetOf(seen, WhyPresenter.Order, "a system with no group would show its raw id to players");
        var text = Text;
        foreach (var sys in WhyPresenter.Order) Assert.DoesNotThrow(() => { var _ = text["why." + sys]; }, sys);
    }

    [Test]
    public void ADisposedPanelHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var why = new WhyPresenter(ui.Root, bridge, Text);
        why.Dispose();
        bridge.EndTurn();
        Assert.IsNotNull(ui.Root.Q<Label>("why-empty"), "still the empty note: the panel did not re-render");
    }
}
