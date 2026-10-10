using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EventCardTests
{
    const string CatalogPath = "Assets/UI/Portraits/PortraitCatalog.asset";
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game on the turn the Egyptian armistice first waits for a decision (February 1949).</summary>
    static SimBridge AtArmistice()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        return new SimBridge(g, Text);
    }

    [Test]
    public void ThePortraitCatalogHoldsEveryPublicDomainPortrait()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, CatalogPath + " (run PortraitImport.Run)");
        var expected = C.People.Where(p => p.Portrait != null && p.Portrait.File != "").Select(p => p.Id).OrderBy(x => x).ToArray();
        Assert.AreEqual(expected, catalog.entries.Select(e => e.personId).OrderBy(x => x).ToArray());
        Assert.IsNotNull(catalog.Get("ben_gurion"));
        Assert.IsNull(catalog.Get("golda_meir"));                     // no public-domain portrait recorded
    }

    [Test]
    public void TheCardShowsThePendingEventWithTheMinisterAndOptions()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, catalog, toast);
        Assert.AreEqual("armistice_egypt", card.Shown!.Id);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("event-card").style.display.value);
        Assert.AreEqual("Armistice with Egypt", ui.Q<Label>("event-title").text);
        Assert.AreEqual("Prime Minister: David Ben-Gurion", ui.Q<Label>("event-office").text);
        StringAssert.Contains("David Ben-Gurion must decide whether Israel signs.", ui.Q<Label>("event-text").text);
        Assert.AreEqual(catalog.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-draft").style.display.value);
        Assert.AreEqual("Sign the armistice", ui.Q<Button>("option-sign").text);
    }

    [Test]
    public void AMinisterWithoutAPortraitShowsNoImage()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        new EventCardPresenter(ui.Root, bridge, Text, null, new ToastPresenter(ui.Root, bridge, Text));
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("event-image").style.display.value);
    }

    [Test]
    public void DraftContentIsStamped()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var ev = C.Events.First(e => e.Id == "armistice_egypt");
        var old = ev.Status;
        try
        {
            ev.Status = "draft";
            new EventCardPresenter(ui.Root, bridge, Text, null, new ToastPresenter(ui.Root, bridge, Text));
            Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("event-draft").style.display.value);
            Assert.AreEqual("DRAFT", ui.Q<Label>("event-draft").text);
        }
        finally { ev.Status = old; }
    }

    [Test]
    public void ADisposedCardHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var card = new EventCardPresenter(ui.Root, bridge, Text, null, new ToastPresenter(ui.Root, bridge, Text));
        Assert.IsNotNull(card.Shown);
        card.Dispose();
        ui.Q<Label>("event-title").text = "untouched";
        bridge.Queue(new SecurityOrder(-1));
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("event-title").text);
        Assert.IsNull(ui.Root.Q<Button>("option-sign"), "option buttons are removed with the card");
    }

    [Test]
    public void ChoosingQueuesTheAnswerAndTheTurnAppliesIt()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, null, toast);
        UiTestKit.Press(ui.Q<Button>("option-sign"));
        var queued = (ChooseEventOption)bridge.Queued.Single();
        Assert.AreEqual(("armistice_egypt", "sign"), (queued.EventId, queued.OptionId));
        Assert.AreNotEqual("armistice_egypt", card.Shown?.Id);           // the card moves on
        bridge.EndTurn();
        Assert.AreEqual(2, bridge.State.Rung);                            // signing ends full war
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "chosen: Sign the armistice"));
    }
}
