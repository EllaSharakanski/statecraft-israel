using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

public class SaveUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SaveStore TempStore() => new SaveStore(Path.Combine(Path.GetTempPath(), "statecraft-saves-" + System.Guid.NewGuid().ToString("N")));

    [Test]
    public void EveryTurnIsAutosavedAndTheNextStartContinuesIt()
    {
        var store = TempStore();
        var notes = new List<string>();
        var bridge = new SimBridge(GameStart.Begin(C, store, () => 77UL, notes), Text);
        GameStart.Autosave(bridge, store);
        bridge.Queue(new SetMobilization(1.0));
        bridge.EndTurn(); bridge.EndTurn();
        Assert.IsTrue(File.Exists(store.AutosavePath));
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
        var resumed = GameStart.Begin(C, store, () => 1UL, notes);
        Assert.IsEmpty(notes);
        Assert.AreEqual(bridge.State.Hash(), resumed.State.Hash());
        Assert.AreEqual(2, resumed.State.Turn);
    }

    [Test]
    public void AnUnreadableAutosaveIsSetAsideAndANewGameStarts()
    {
        var store = TempStore();
        store.WriteAutosave("statecraft-save 9\ncontent x\nseed 1\nmode Historical\nend x\n");
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 5UL, notes);
        Assert.AreEqual(0, session.State.Turn);
        StringAssert.StartsWith("Your saved game could not be loaded (line 1: save schema version 9 is newer", notes[0]);
        Assert.IsFalse(File.Exists(store.AutosavePath));
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    [Test]
    public void ASaveWithAnUnsupportedModeIsSetAsideToo()
    {
        var store = TempStore();
        var good = GameSession.New(C, 9, Mode.Historical).Save.Write();
        store.WriteAutosave(good.Replace("mode Historical", "mode Open"));
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 6UL, notes);
        Assert.AreEqual(0, session.State.Turn);
        StringAssert.StartsWith("Your saved game could not be loaded (line 4: mode Open is not supported)", notes[0]);
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    [Test]
    public void AFailureThatIsNotASaveExceptionNeverStopsTheStart()
    {
        // Another process holds the file: reading it throws IOException, not SaveException, and even setting it aside may fail.
        var store = TempStore();
        store.WriteAutosave(GameSession.New(C, 9, Mode.Historical).Save.Write());
        using (new FileStream(store.AutosavePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var notes = new List<string>();
            GameSession? session = null;
            Assert.DoesNotThrow(() => session = GameStart.Begin(C, store, () => 6UL, notes));
            Assert.AreEqual(0, session!.State.Turn);
            StringAssert.StartsWith("Your saved game could not be loaded (", notes[0]);
        }
    }

    [Test]
    public void NewGameNeedsASecondPress()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        GameStart.Autosave(bridge, store);
        bridge.EndTurn();
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("new-game");
        Assert.IsTrue(newGame.Armed);
        Assert.AreEqual("Press New Game again to abandon this game and start over.", ui.Q<UnityEngine.UIElements.Label>("toast").text);
        Assert.AreEqual(1, bridge.State.Turn);
        ui.Press("new-game");
        Assert.AreEqual(0, bridge.State.Turn);
        Assert.IsFalse(File.Exists(store.AutosavePath));
    }

    [Test]
    public void TheWebSaveFolderDoesNotDependOnThePageUrl() =>
        Assert.AreEqual("/idbfs/statecraft-saves", SaveStore.WebDirectory);

    [Test]
    public void AFailingAutosaveWriteNeverStopsATurn()
    {
        var file = Path.Combine(Path.GetTempPath(), "statecraft-not-a-folder-" + System.Guid.NewGuid().ToString("N"));
        File.WriteAllText(file, "x");                                   // a file where the folder should be
        try
        {
            var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
            var errors = new List<string>();
            GameStart.Autosave(bridge, new SaveStore(file), errors.Add);
            Assert.DoesNotThrow(() => bridge.EndTurn());
            Assert.AreEqual(1, bridge.State.Turn);
            Assert.AreEqual(1, errors.Count);
            StringAssert.StartsWith("Your game could not be saved", errors[0]);
        }
        finally { File.Delete(file); }
    }

    [Test]
    public void ANewGameIgnoresTheAutosaveWhenAsked()
    {
        var store = TempStore();
        var played = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        GameStart.Autosave(played, store);
        played.EndTurn();
        var session = GameStart.Begin(C, store, () => 8UL, new List<string>(), fresh: true);
        Assert.AreEqual(0, session.State.Turn);
        Assert.IsTrue(File.Exists(store.AutosavePath), "an ignored save is left alone");
    }

    [Test]
    public void ASaveMadeWithOtherContentLoadsWithANote()
    {
        var store = TempStore();
        var text = GameSession.New(C, 9, Mode.Historical).Save.Write().Replace("content " + C.Hash, "content " + new string('0', C.Hash.Length));
        store.WriteAutosave(text);
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 1UL, notes);
        Assert.AreEqual(1, notes.Count);
        StringAssert.Contains("different game content", notes[0]);
        Assert.IsTrue(File.Exists(store.AutosavePath), "a warning is not a failure: the save is kept");
        Assert.AreEqual(0, session.State.Turn);
    }

    [Test]
    public void ArmingIsCancelledByATurnAndADisposedButtonDoesNothing()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("new-game");
        Assert.IsTrue(newGame.Armed);
        bridge.EndTurn();
        Assert.IsFalse(newGame.Armed);
        ui.Press("new-game");
        Assert.AreEqual(1, bridge.State.Turn, "one press after a turn only arms again");
        newGame.Dispose();
        ui.Press("new-game"); ui.Press("new-game");
        Assert.AreEqual(1, bridge.State.Turn);
    }

    [Test]
    public void NewGameRefreshesTheScreens()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        var hud = new HudPresenter(ui.Root, bridge, Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("end-turn");
        Assert.AreEqual("Turn 1", ui.Q<Label>("turn").text);
        ui.Press("new-game"); ui.Press("new-game");
        Assert.AreEqual("Turn 0", ui.Q<Label>("turn").text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
    }
}
