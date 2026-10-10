#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class HudTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge NewBridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);

    [Test]
    public void TheTopBarShowsTheOpeningOfTheGame()
    {
        var m = HudModel.From(NewBridge(), Text);
        Assert.AreEqual("1948", m.Year);
        Assert.AreEqual("Turn 0", m.Turn);
        Assert.AreEqual("15 May 1948", m.Date);
        Assert.AreEqual("20.0m", m.Treasury.Value);
        Assert.AreEqual("$40.0m", m.Fx.Value);
        Assert.AreEqual("Level 5 of 5", m.SecurityLevel);
        Assert.AreEqual("Attention 4.0 of 4.0", m.Attention);
        Assert.AreEqual("", m.Treasury.Delta);                       // no previous turn yet
    }

    [Test]
    public void EndTurnAdvancesTheGameAndShowsChanges()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new HudPresenter(ui.Root, bridge, Text);
        ui.Press("end-turn");
        Assert.AreEqual(1, bridge.State.Turn);
        Assert.AreEqual("Turn 1", ui.Q<Label>("turn").text);
        Assert.AreEqual("22 May 1948", ui.Q<Label>("date").text);
        var arrivals = bridge.Session.Last!.Why.Where(w => w.System == "population").Sum(w => w.Delta);
        Assert.AreEqual(arrivals.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), ui.Q<Label>("immigration-value").text);
        var s0 = bridge.Session.Previous!; var s1 = bridge.State;
        var d = System.Math.Round((s1.Treasury - s1.Debt) - (s0.Treasury - s0.Debt), 1, System.MidpointRounding.AwayFromZero);
        Assert.AreNotEqual(0.0, d, "the test needs a week with a treasury change");
        Assert.AreEqual(d.ToString("+0.0;-0.0", System.Globalization.CultureInfo.InvariantCulture), ui.Q<Label>("treasury-delta").text);
        Assert.AreEqual(d < 0, ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
        Assert.AreEqual(d > 0, ui.Q<Label>("treasury-delta").ClassListContains("delta-up"));
    }

    [Test]
    public void QueuedOrdersSpendAttentionUpFront()
    {
        var bridge = NewBridge();
        Assert.IsNull(bridge.Queue(new SetMobilization(1.0)));      // at war each order costs 2
        Assert.AreEqual(2.0, bridge.AttentionLeft, 1e-9);
        Assert.IsNull(bridge.Queue(new SetMobilization(0.9)));      // replaces the first: still one order
        Assert.AreEqual(1, bridge.Queued.Count);
        Assert.IsNull(bridge.Queue(new SecurityOrder(-1)));
        Assert.AreEqual("Not enough attention (need 2.0, have 0.0)", bridge.Queue(new SetPolicy("rationing", 1)));
    }

    [Test]
    public void RefusedOrdersAreShownWithTheirReason()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var text = Text;
        new HudPresenter(ui.Root, bridge, text);
        new ToastPresenter(ui.Root, bridge, text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
        bridge.Queue(new SetPolicy(C.Policies[0].Id, 2));     // a known policy at an impossible level: the sim refuses it
        ui.Press("end-turn");
        Assert.AreEqual("Not done: Policy level must be between 0 and 1", ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("toast").style.display.value);
    }

    [Test]
    public void NavigationOpensOneScreenAtATime()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        Assert.IsNull(nav.Current);
        ui.Press("nav-budget");
        Assert.AreEqual("budget", nav.Current);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("screen-budget").style.display.value);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-cabinet").style.display.value);
        ui.Press("nav-cabinet");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-budget").style.display.value);
        ui.Press("nav-cabinet");
        Assert.IsNull(nav.Current);
    }

    [UnityTest]
    public IEnumerator AtStartNoScreenIsShownAndTheMiddleOfTheScreenIsNotUi()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new ScreenNav(ui.Root);
        new ToastPresenter(ui.Root, bridge, Text);
        foreach (var name in new[] { "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>(name).style.display.value, name);
        for (int i = 0; i < 10 && float.IsNaN(ui.Root.worldBound.width); i++) yield return null;
        var panel = ui.Root.panel;
        var middle = ui.Root.worldBound.center;
        Assert.IsNull(panel.Pick(middle), "the middle of the screen must reach the map");
        // screen coordinates are y-up; UiPointer flips them
        var screenH = (float)Screen.height;
        var asScreen = new Vector2(middle.x, screenH - middle.y);
        Assert.IsFalse(UiPointer.IsOverUi(panel, asScreen, screenH));
    }

    [Test]
    public void BadIdsAreRefusedNotThrownAndTheNextTurnRuns()
    {
        var bridge = NewBridge();
        var refusals = new System.Collections.Generic.List<string>();
        bridge.Refused += refusals.Add;
        Assert.IsNotNull(bridge.Queue(new SetPolicy(null!, 1)));
        Assert.IsNotNull(bridge.Queue(new SetPolicy("", 1)));
        Assert.IsNotNull(bridge.Queue(new SetPolicy("curfew", 1)));
        Assert.IsNotNull(bridge.Queue(new Appoint("pm", "free text")));
        Assert.IsNotNull(bridge.Queue(new Appoint(null!, null!)));
        Assert.IsNotNull(bridge.Queue(new ChooseEventOption("nope", "nope")));
        Assert.IsNotNull(bridge.Queue(new ChooseEventOption(C.Events[0].Id, "nope")));
        Assert.IsNotNull(bridge.Queue(new MapAction("moat", 0, 0)));
        Assert.IsNotNull(bridge.Queue(new AllocateBudget(new System.Collections.Generic.Dictionary<string, double> { ["nowhere"] = 1 })));
        Assert.IsNotNull(bridge.Queue(new AllocateBudget(null!)));
        Assert.AreEqual(0, bridge.Queued.Count);
        Assert.AreEqual(10, refusals.Count);
        Assert.DoesNotThrow(() => bridge.EndTurn());
        Assert.AreEqual(1, bridge.State.Turn);
    }

    [Test]
    public void AQueuedBudgetIsACopy()
    {
        var bridge = NewBridge();
        var shares = new System.Collections.Generic.Dictionary<string, double>(C.Rules.BudgetStart);
        Assert.IsNull(bridge.Queue(new AllocateBudget(shares)));
        var key = shares.Keys.First();
        shares[key] = 99;
        shares["nowhere"] = 1;
        var queued = (AllocateBudget)bridge.Queued[0];
        Assert.AreEqual(C.Rules.BudgetStart[key], queued.Shares[key], 1e-12);
        Assert.IsFalse(queued.Shares.ContainsKey("nowhere"));
    }

    [Test]
    public void ADisposedPresenterHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var hud = new HudPresenter(ui.Root, bridge, Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var nav = new ScreenNav(ui.Root);
        hud.Dispose(); toast.Dispose(); nav.Dispose();
        ui.Q<Label>("turn").text = "untouched";
        ui.Q<Label>("toast").text = "untouched";
        bridge.Queue(new SetPolicy("curfew", 1));
        bridge.EndTurn();
        ui.Press("end-turn");
        ui.Press("nav-budget");
        Assert.AreEqual("untouched", ui.Q<Label>("turn").text);
        Assert.AreEqual("untouched", ui.Q<Label>("toast").text);
        Assert.AreEqual(1, bridge.State.Turn, "End Turn button no longer wired (only the direct call ran)");
        Assert.IsNull(nav.Current);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-budget").style.display.value);
    }

    [Test]
    public void ToastShowsQueueRefusalsAndClearsOnTheNextChange()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new ToastPresenter(ui.Root, bridge, Text);
        Assert.IsNotNull(bridge.Queue(new SetPolicy("curfew", 1)));
        Assert.AreEqual("Not done: Unknown policy 'curfew'", ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("toast").style.display.value);
        Assert.IsNull(bridge.Queue(new SecurityOrder(-1)));
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
    }

    [Test]
    public void ChangesShowSignsAndZeroDeltasAreEmpty()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        bridge.EndTurn();
        var hud = new HudPresenter(ui.Root, bridge, Text);
        var prev = bridge.Session.Previous!;
        var now = bridge.State;
        prev.Treasury = now.Treasury - now.Debt + prev.Debt - 5;          // up by 5
        hud.Render();
        Assert.AreEqual("+5.0", ui.Q<Label>("treasury-delta").text);
        Assert.IsTrue(ui.Q<Label>("treasury-delta").ClassListContains("delta-up"));
        Assert.IsFalse(ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
        prev.Treasury = now.Treasury - now.Debt + prev.Debt + 0.04;       // down by 0.04: rounds to zero, never "-0.0"
        hud.Render();
        Assert.AreEqual("", ui.Q<Label>("treasury-delta").text);
        Assert.IsFalse(ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
        prev.Treasury = now.Treasury - now.Debt + prev.Debt - 0.04;
        hud.Render();
        Assert.AreEqual("", ui.Q<Label>("treasury-delta").text);
    }

    [Test]
    public void TheHudIsTheSameUnderAGermanCulture()
    {
        var bridge = NewBridge();
        bridge.EndTurn();
        var inv = HudModel.From(bridge, Text);
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var de = HudModel.From(bridge, Text);
            Assert.AreEqual(JsonUtility.ToJson(Flat(inv)), JsonUtility.ToJson(Flat(de)));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }

    [System.Serializable] class FlatHud { public string[] V = new string[0]; }
    static FlatHud Flat(HudModel m) => new FlatHud
    {
        V = new[] { m.Year, m.Turn, m.Date, m.Attention, m.SecurityLevel,
            m.Treasury.Value, m.Treasury.Delta, m.Immigration.Value, m.Immigration.Delta, m.Security.Value, m.Security.Delta,
            m.Support.Value, m.Support.Delta, m.Standing.Value, m.Standing.Delta, m.Fx.Value, m.Fx.Delta },
    };

    [Test]
    public void ReplaceDropsOrdersAndBeforeLastSpansTwoTurns()
    {
        var bridge = NewBridge();
        Assert.IsNull(bridge.BeforeLast);
        bridge.EndTurn();
        Assert.IsNull(bridge.BeforeLast);                        // only one step has run
        var first = bridge.Session.Last;
        bridge.EndTurn();
        Assert.AreSame(first, bridge.BeforeLast);
        Assert.AreNotSame(first, bridge.Session.Last);
        bridge.Queue(new SecurityOrder(-1));
        int changed = 0; bridge.Changed += () => changed++;
        bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        Assert.AreEqual(1, changed);
        Assert.AreEqual(0, bridge.Queued.Count);
        Assert.IsNull(bridge.BeforeLast);
        Assert.AreEqual(0, bridge.State.Turn);
    }

    [Test]
    public void ABatchTheSimRefusesOutrightKeepsTheStateAndShowsTheReason()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text, (s, cmds) => throw new System.ArgumentException("boom"));
        new ToastPresenter(ui.Root, bridge, Text);
        var order = new SecurityOrder(-1);
        Assert.IsNull(bridge.Queue(order));
        System.Collections.Generic.IReadOnlyList<Rejection>? seen = null;
        bridge.TurnEnded += r => seen = r;
        var hash = bridge.State.Hash();
        Assert.DoesNotThrow(() => bridge.EndTurn());
        Assert.AreEqual(hash, bridge.State.Hash());
        Assert.AreEqual(0, bridge.Queued.Count);
        Assert.IsNull(bridge.BeforeLast);
        Assert.AreSame(order, seen![0].Command);
        Assert.AreEqual("Not done: The turn could not run: boom", ui.Q<Label>("toast").text);
    }
}
