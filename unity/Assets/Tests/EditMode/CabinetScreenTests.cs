using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CabinetScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game on 26 February 1950: the First Government, whose next turn ends before it falls.</summary>
    static SimBridge In1950()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g, Text);
    }

    [Test]
    public void TheScreenListsTheGovernmentAndRefusesIllegalAppointmentsWithAReason()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        Assert.AreEqual("First Government", ui.Q<Label>("cabinet-government").text);
        StringAssert.StartsWith("Coalition: 73 of 120 seats", ui.Q<Label>("cabinet-coalition").text);
        Assert.AreEqual("Minister of Finance: Eliezer Kaplan · Mapai · competence 8, loyalty 9", ui.Q<Label>("seat-finance_minister-holder").text);
        UiTestKit.Press(ui.Q<Button>("seat-finance_minister"));
        Assert.AreEqual("finance_minister", cabinet.Selected);
        Assert.IsFalse(ui.Q<Button>("appoint-zisling").enabledSelf);
        Assert.AreEqual("Aharon Zisling's party (Mapam) is not in the coalition", ui.Q<Label>("cand-zisling-reason").text);
        Assert.IsTrue(ui.Q<Button>("appoint-golda_meir").enabledSelf);
    }

    [Test]
    public void AppointingTakesEffectAtEndTurn()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        Assert.AreEqual("Ordered: Golda Meir to Minister of Finance, at End Turn", ui.Q<Label>("cabinet-status").text);
        bridge.EndTurn();
        Assert.AreEqual("golda_meir", Cabinet.Holder(C, bridge.State, "finance_minister")!.Id);
        StringAssert.EndsWith("(your appointee)", ui.Q<Label>("seat-finance_minister-holder").text);
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "Golda Meir appointed Minister of Finance, replacing Eliezer Kaplan"));
    }

    [Test]
    public void APortraitShowsOnlyWhenTheCatalogHasOne()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset");
        new CabinetPresenter(ui.Root, bridge, Text, catalog);
        var kaplan = ui.Q<Button>("seat-finance_minister").Q<VisualElement>(className: "seat-portrait");
        Assert.AreEqual(catalog.Get("kaplan"), kaplan.style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.Flex, kaplan.style.display.value);
        var noPortrait = C.Offices.Select(o => (office: o, holder: Cabinet.Holder(C, bridge.State, o.Id))).First(x => x.holder != null && catalog.Get(x.holder.Id) == null);
        var shown = ui.Q<Button>("seat-" + noPortrait.office.Id).Q<VisualElement>(className: "seat-portrait");
        Assert.AreEqual(DisplayStyle.None, shown.style.display.value, "no invented portrait");
    }

    [Test]
    public void EveryCandidateIsListedAndOnlyCheckDecidesWhoCanBeAppointed()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        foreach (var office in new[] { "finance_minister", "chief_of_staff", "president", "prime_minister" })
        {
            cabinet.Select(office);
            foreach (var (person, reason) in Cabinet.Candidates(C, bridge.State, office))
            {
                Assert.AreEqual(reason == null, ui.Q<Button>("appoint-" + person.Id).enabledSelf, office + " " + person.Id);
                if (reason != null) Assert.AreEqual(reason, ui.Q<Label>("cand-" + person.Id + "-reason").text);
            }
        }
    }

    [Test]
    public void ARefusalAtQueueTimeIsShown()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        new ToastPresenter(ui.Root, bridge, Text);
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[0].Id, 0.5)));
        Assert.IsNull(bridge.Queue(new SetPolicy(C.Policies[1].Id, 0.5)));
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        StringAssert.StartsWith("Not done: Not enough attention", ui.Q<Label>("toast").text);
        Assert.AreEqual(2, bridge.Queued.Count);
    }

    [Test]
    public void SelectingNothingClosesTheListAndADisposedScreenHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null);
        cabinet.Select("finance_minister");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("cabinet-pick").style.display.value);
        Assert.IsTrue(ui.Q<Button>("seat-finance_minister").ClassListContains("seat-selected"));
        cabinet.Select(null);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("cabinet-pick").style.display.value);
        cabinet.Dispose();
        ui.Q<Label>("cabinet-government").text = "untouched";
        bridge.EndTurn();
        Assert.AreEqual("untouched", ui.Q<Label>("cabinet-government").text);
    }
}
