using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class UiFoundationTests
{
    [Test]
    public void PanelSettingsScaleWithA1080pReference()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
        Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ps.scaleMode);
        Assert.AreEqual(new Vector2Int(1920, 1080), ps.referenceResolution);
        Assert.AreEqual(PanelScreenMatchMode.MatchWidthOrHeight, ps.screenMatchMode);
        Assert.AreEqual(0.5f, ps.match);
        Assert.IsNotNull(ps.themeStyleSheet, "theme");
    }

    [Test]
    public void ASubmittedButtonFiresClickedSynchronously()
    {
        using var ui = new UiTestKit();
        int clicks = 0;
        ui.Q<Button>("end-turn").clicked += () => clicks++;
        ui.Press("end-turn");
        Assert.AreEqual(1, clicks);
    }

    [Test]
    public void TheLayoutHasEveryRegionOfTheTargetScreen()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "top-bar", "event-card", "info-panel", "minimap", "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name);
        foreach (var name in new[] { "treasury", "immigration", "security", "support", "standing", "fx" })
        {
            ui.Q<Label>(name + "-value");
            ui.Q<Label>(name + "-delta");
        }
        Assert.AreEqual(PickingMode.Ignore, ui.Q<VisualElement>("root").pickingMode, "empty screen areas must let clicks reach the map");
    }

    [UnityTest]
    public IEnumerator TextUsesTheSerifFaces()
    {
        using var ui = new UiTestKit();
        for (int i = 0; i < 10 && ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font == null; i++) yield return null;
        var regular = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontRegular);
        var semibold = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontSemiBold);
        Assert.AreSame(semibold, ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font);
        Assert.AreSame(regular, ui.Q<Label>("turn").resolvedStyle.unityFontDefinition.font);
    }

    [Test]
    public void StringsLoadFromTheYamlFileAndFormatInvariantly()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        Assert.AreEqual("Turn 3", text.Format("hud.turn", 3));
        Assert.AreEqual("1,234,567", text.Format("hud.people", 1234567.0));
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { var _ = text["no.such.key"]; });
    }

    [UnityTest]
    public IEnumerator PickingFindsButtonsButNotTheTransparentRoot()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name).style.display = DisplayStyle.None;   // closed, as at game start
        var button = ui.Q<Button>("end-turn");
        for (int i = 0; i < 10 && float.IsNaN(button.worldBound.width); i++) yield return null;
        var panel = ui.Root.panel;
        Assert.AreSame(button, panel.Pick(button.worldBound.center));
        Assert.IsNull(panel.Pick(ui.Root.worldBound.center), "the empty middle of the screen must reach the map");
        // Input System screen positions are y-up; ToPanel flips y before RuntimePanelUtils.ScreenToPanel (which does not).
        Assert.AreEqual(RuntimePanelUtils.ScreenToPanel(panel, new Vector2(10, 20)), UiPointer.ToPanel(panel, new Vector2(10, 1060), 1080f));
    }
}
