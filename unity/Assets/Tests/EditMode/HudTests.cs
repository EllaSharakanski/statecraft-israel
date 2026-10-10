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
    static SimBridge NewBridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical));

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
        Assert.AreEqual("-1.4", ui.Q<Label>("treasury-delta").text);   // a week of war: taxes, spending and the call-up
        Assert.IsTrue(ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
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
        bridge.Queue(new SetPolicy("curfew", 1));
        ui.Press("end-turn");
        Assert.AreEqual("Not done: Unknown policy 'curfew'", ui.Q<Label>("toast").text);
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
}
