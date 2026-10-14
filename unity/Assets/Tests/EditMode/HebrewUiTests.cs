#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The Hebrew UI (#193): asked for by hand only ("?lang=he", "-lang he"), English otherwise; when asked, GameRoot
/// loads strings.he.yaml and marks the root, and every text is set in the Hebrew face through the advanced text
/// generator.</summary>
public class HebrewUiTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    string saveDir = "";

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-hebrew-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    // --- The flag ---------------------------------------------------------------------------------------------------

    [Test]
    public void HebrewIsOnlyAskedForByHand()
    {
        var none = new string[0];
        foreach (var q in new[] { "?lang=he", "?golden&lang=he", "?lang=he&smoke=5", "?lang=he#top" })
            Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.Requested("http://localhost:8000/index.html" + q, none, webgl: true), q);
        foreach (var q in new[] { "", "?lang", "?lang=", "?lang=en", "?lang=hex", "?lang=HE", "?slang=he", "?lang=fr", "?golden" })
            Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost:8000/index.html" + q, none, webgl: true), q);
        Assert.AreEqual(UiLanguage.English, UiLanguage.Requested(null, none, webgl: true));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.Requested(null, new[] { "-batchmode", "-lang", "he" }, webgl: false));
        foreach (var args in new[] { none, new[] { "-lang" }, new[] { "-lang", "en" }, new[] { "-lang", "-smoke" }, new[] { "he" }, new[] { "-language", "he" } })
            Assert.AreEqual(UiLanguage.English, UiLanguage.Requested(null, args, webgl: false), string.Join(" ", args));
        // A flag on the desktop does not count on the web and the other way round (each instrument's own rule).
        Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost/index.html", new[] { "-lang", "he" }, webgl: true));
        Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost/index.html?lang=he", none, webgl: false));
    }

    // --- The game ---------------------------------------------------------------------------------------------------

    GameRoot Boot(string? language)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Language = language;
        root.Begin(Inputs, 0);
        return root;
    }

    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    [Test]
    public void TheGameIsEnglishUnlessHebrewIsAskedFor()
    {
        var game = Boot(null);                                         // the test run's command line has no "-lang he"
        Assert.IsFalse(Ui.ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(LanguageDirection.Inherit, Ui.languageDirection);
        Assert.AreEqual(En.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
    }

    [Test]
    public void AskedForHebrewTheGameLoadsTheHebrewStringsAndMarksTheRoot()
    {
        var game = Boot(UiLanguage.Hebrew);
        Assert.IsTrue(Ui.ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(LanguageDirection.RTL, Ui.languageDirection);
        Assert.AreEqual(He.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        StringAssert.Contains("תור", Ui.Q<Label>("turn").text);
        // Starting again in English takes the mark off (New Game restarts through Begin).
        game.Language = UiLanguage.English;
        game.Begin(Inputs, 0);
        Assert.IsFalse(Ui.ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(LanguageDirection.Inherit, Ui.languageDirection);
    }

    // --- The house ---------------------------------------------------------------------------------------------------

    /// <summary>In Hebrew the half circle is the same picture mirrored (#199): the first seat, the coalition's, is at the
    /// right end and the last at the left, at the same height. English keeps the first seat on the left.</summary>
    [Test]
    public void TheHemicycleIsMirroredInHebrewOnly()
    {
        var centre = new Vector2(160, 150);
        const float Scale = 100f;
        var seats = Hemicycle.Layout(120);
        Vector2 first = seats[0], last = seats[seats.Count - 1];
        Assert.Less(Hemicycle.ToBox(first, centre, Scale, mirrored: false).x, centre.x, "English: the first seat on the left");
        Assert.Greater(Hemicycle.ToBox(first, centre, Scale, mirrored: true).x, centre.x, "Hebrew: the first seat on the right");
        Assert.Less(Hemicycle.ToBox(last, centre, Scale, mirrored: true).x, centre.x);
        foreach (var seat in seats)
        {
            Vector2 ltr = Hemicycle.ToBox(seat, centre, Scale, false), rtl = Hemicycle.ToBox(seat, centre, Scale, true);
            Assert.AreEqual(2 * centre.x - ltr.x, rtl.x, 1e-3f, "mirrored about the centre line");
            Assert.AreEqual(ltr.y, rtl.y, "the same height");
        }

        using var ui = new UiTestKit();
        var c = Inputs.Content;
        SimBridge Game(UiText t) => new SimBridge(Statecraft.Sim.GameSession.New(c, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical), t);
        using (new CabinetPresenter(ui.Root, Game(En), En, null))
            Assert.IsFalse(ui.Q<Hemicycle>("cabinet-hemicycle").Mirrored, "English");
        using (new CabinetPresenter(ui.Root, Game(He), He, null))
            Assert.IsTrue(ui.Q<Hemicycle>("cabinet-hemicycle").Mirrored, "Hebrew");
        using (new CabinetPresenter(ui.Root, Game(En), En, null))
            Assert.IsFalse(ui.Q<Hemicycle>("cabinet-hemicycle").Mirrored, "English again: the mark does not stick");
    }

    // --- The face ---------------------------------------------------------------------------------------------------

    /// <summary>With .lang-he on the root every text element of Game.uxml resolves Frank Ruhl Libre and the advanced
    /// generator, whatever rule styles it; without it (the control) the same elements resolve EB Garamond.</summary>
    [UnityTest]
    public IEnumerator InHebrewEveryTextIsSetInTheHebrewFaceThroughTheAdvancedGenerator()
    {
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("HebrewUiTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        try
        {
            var hebrew = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontHebrew);
            Assert.IsNotNull(hebrew, UiPaths.FontHebrew);
            var root = doc.rootVisualElement.Q("root");
            static Font? FontOf(TextElement e)
            {
                var d = e.resolvedStyle.unityFontDefinition;
                return d.fontAsset != null ? d.fontAsset.sourceFontFile : d.font;
            }
            // The start screen's Hebrew lines are Hebrew in both languages (the Declaration's own text).
            var texts = root.Query<TextElement>().ToList().Where(e => !e.ClassListContains("start-he")).ToList();
            Assert.Greater(texts.Count, 30, "Game.uxml's labels and buttons");
            for (int i = 0; i < 3; i++) yield return null;             // resolved styles need a style pass
            foreach (var e in texts)
                Assert.AreNotSame(hebrew, FontOf(e), $"English: '{e.name}' ({string.Join(" ", e.GetClasses())})");
            root.AddToClassList(UiLanguage.HebrewClass);
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var e in texts)
            {
                var what = $"'{e.name}' ({string.Join(" ", e.GetClasses())})";
                Assert.AreSame(hebrew, FontOf(e), what);
                Assert.AreEqual("Advanced", e.resolvedStyle.unityTextGenerator.ToString(), what);
            }
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
