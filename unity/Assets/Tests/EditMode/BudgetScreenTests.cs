using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BudgetScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void AdjustKeepsTheTotalAtExactlyOne()
    {
        var shares = new Dictionary<string, double> { ["a"] = 0.45, ["b"] = 0.35, ["c"] = 0.2 };
        var up = BudgetPresenter.Adjust(shares, "c", 0.05);
        Assert.AreEqual(0.25, up["c"], 1e-12);
        Assert.AreEqual(1.0, up.Values.Sum(), 1e-12);
        Assert.Greater(up["a"] / up["b"], 1.28);                      // the others keep their proportions
        var floor = BudgetPresenter.Adjust(shares, "c", -0.5);
        Assert.AreEqual(0.0, floor["c"]);
        Assert.AreEqual(1.0, floor.Values.Sum(), 1e-12);
    }

    [Test]
    public void TheScreenListsTheMinistriesAndSubmitsTheDraft()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("45%", ui.Q<Label>("budget-defense_minister-share").text);
        Assert.AreEqual("Minister of Defense (David Ben-Gurion)", ui.Q<Label>("budget-defense_minister-name").text);
        Assert.AreEqual("effect x1.50", ui.Q<Label>("budget-defense_minister-factor").text);   // capped at 1.5
        Assert.AreEqual("The Provisional State Council does not vote on the budget", ui.Q<Label>("budget-vote").text);
        Assert.AreEqual("Treasury 20.0m · debt 0.0m · foreign currency $40.0m", ui.Q<Label>("budget-money").text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        Assert.AreEqual("11%", ui.Q<Label>("budget-education_minister-share").text);
        Assert.AreEqual("Changed: submit to put it to the vote", ui.Q<Label>("budget-status").text);
        ui.Press("budget-submit");
        var queued = (AllocateBudget)bridge.Queued.Single();
        Assert.AreEqual(1.0, queued.Shares.Values.Sum(), 1e-9);
        Assert.AreEqual("Submitted: takes effect at End Turn", ui.Q<Label>("budget-status").text);
        bridge.EndTurn();
        Assert.AreEqual(budget.Draft["education_minister"], bridge.State.Budget["education_minister"], 1e-12);
        Assert.IsEmpty(bridge.Session.Last!.Rejected);
    }

    [Test]
    public void AdjustHandlesEdges()
    {
        var one = BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 1.0, ["b"] = 0.0 }, "a", -0.05);
        Assert.AreEqual(0.95, one["a"], 1e-12); Assert.AreEqual(0.05, one["b"], 1e-12);     // nothing to rescale: split evenly
        var cap = BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 0.9, ["b"] = 0.1 }, "a", 0.5);
        Assert.AreEqual(1.0, cap["a"]); Assert.AreEqual(0.0, cap["b"], 1e-12);
        var lone = BudgetPresenter.Adjust(new Dictionary<string, double> { ["a"] = 1.0 }, "a", -0.5);
        Assert.AreEqual(0.5, lone["a"], 1e-12);                                           // a single line cannot leave 1: the sim would refuse a sum of 0.5
    }

    [Test]
    public void FactorsAndFlowsComeFromTheSim()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        foreach (var office in C.Rules.BudgetStart.Keys)
            Assert.AreEqual($"effect x{EffectsSystem.BudgetFactor(budget.Draft, office):0.00}", ui.Q<Label>($"budget-{office}-factor").text, office);
        var f = EconomySystem.Flows(C, bridge.State, 1.0);
        Assert.AreEqual($"Taxes {f.Tax:0.0}m, spending {f.Spending:0.0}m and reserve call-up {f.Mobilization:0.0}m a year", ui.Q<Label>("budget-flow").text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        Assert.AreEqual($"effect x{EffectsSystem.BudgetFactor(budget.Draft, "education_minister"):0.00}", ui.Q<Label>("budget-education_minister-factor").text);
    }

    [Test]
    public void TheDraftSurvivesOtherChangesAndResetsToTheBudget()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        bridge.Queue(new SecurityOrder(-1));                                  // another order changes the bridge; the draft stays
        Assert.AreEqual("11%", ui.Q<Label>("budget-education_minister-share").text);
        ui.Press("budget-reset");
        Assert.AreEqual("6%", ui.Q<Label>("budget-education_minister-share").text);
        Assert.AreEqual("", ui.Q<Label>("budget-status").text);
    }

    [Test]
    public void ALegislatureThatMustApproveIsNamed()
    {
        using var ui = new UiTestKit();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!(Offices.Legislature(C, g.State.Date)?.BudgetApproval ?? false)) g.Step(new Command[0]);
        var bridge = new SimBridge(g, Text);
        new BudgetPresenter(ui.Root, bridge, Text);
        var leg = Offices.Legislature(C, g.State.Date)!;
        var (seats, size) = PoliticsSystem.CoalitionSeats(C, g.State.Date);
        Assert.AreEqual($"The {leg.Body} must approve it: the coalition holds {seats} of {size} seats", ui.Q<Label>("budget-vote").text);
    }

    [Test]
    public void ADisposedScreenHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text);
        budget.Dispose();
        ui.Q<Label>("budget-money").text = "untouched";
        ui.Press("budget-submit");
        Assert.AreEqual(0, bridge.Queued.Count);
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("budget-money").text);
    }
}
