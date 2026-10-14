#nullable enable
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Ben-Gurion's 1948 recording on the start screen (docs/declaration-recording.md): the shipped file is the one
/// received from the archive, byte for byte; it plays only from its button, never in automated runs; its captions are the
/// sentence, in Hebrew right to left and in English, and the credit is on screen.</summary>
public class DeclarationRecordingTests
{
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static string FilePath => Path.Combine(Application.streamingAssetsPath, DeclarationRecording.File);

    [Test]
    public void TheShippedFileIsTheArchivesExcerptByteForByte()
    {
        Assert.IsTrue(File.Exists(FilePath), FilePath);
        using var sha = SHA256.Create();
        var hash = string.Concat(sha.ComputeHash(File.ReadAllBytes(FilePath)).Select(b => b.ToString("x2")));
        Assert.AreEqual(DeclarationRecording.Sha256, hash, "the recording must ship unaltered (ISA terms)");
        Assert.AreEqual(2675622, new FileInfo(FilePath).Length);
        Assert.IsNull(AssetImporter.GetAtPath("Assets/StreamingAssets/" + DeclarationRecording.File) as AudioImporter,
            "StreamingAssets: Unity does not import or re-encode it");
    }

    sealed class Rig : System.IDisposable
    {
        public readonly UiTestKit Ui; public readonly DeclarationRecording Recording;
        public Rig(bool off) { Ui = new UiTestKit(); Recording = new DeclarationRecording(Ui.Root, Text, off); }
        public bool Shows(string name) => Ui.Root.Q(name).style.display.value == DisplayStyle.Flex;
        public void Dispose() { Recording.Dispose(); Ui.Dispose(); }
    }

    [Test]
    public void TheButtonPlaysAndStopsAndShowsTheCaptionsAndTheCredit()
    {
        LogAssert.ignoreFailingMessages = true;   // the edit-mode audio fetch may log; the screen is what is checked
        using var r = new Rig(off: false);
        var listen = r.Ui.Q<Button>("start-listen");
        Assert.IsTrue(r.Shows("start-recording"));
        Assert.AreEqual("Play the recording", listen.text);
        Assert.AreEqual("Recording: Israel State Archives", r.Ui.Q<Label>("start-recording-credit").text);
        Assert.IsFalse(r.Shows("start-caption"), "no captions before the press");
        Assert.IsFalse(r.Recording.Playing, "nothing plays by itself");
        UiTestKit.Press(listen);
        Assert.IsTrue(r.Recording.Playing);
        Assert.IsTrue(r.Shows("start-caption"), "captions while it plays");
        Assert.AreEqual("Stop the recording", listen.text);
        UiTestKit.Press(listen);
        Assert.IsFalse(r.Recording.Playing);
        Assert.IsFalse(r.Shows("start-caption"));
        Assert.AreEqual("Play the recording", listen.text);
    }

    [Test]
    public void AutomatedRunsHaveNoButton()
    {
        using var r = new Rig(off: true);
        Assert.IsFalse(r.Shows("start-recording"), "no audio in automated runs");
        Assert.IsFalse(r.Recording.Playing);
    }

    [Test]
    public void TheCaptionsAreTheSentenceInHebrewAndEnglish()
    {
        using var ui = new UiTestKit();
        var he = ui.Q<Label>("start-caption-he"); var en = ui.Q<Label>("start-caption-en");
        Assert.AreEqual("אנו מכריזים בזאת על הקמת מדינה יהודית בארץ ישראל, היא מדינת ישראל", he.text);
        Assert.AreEqual("We hereby declare the establishment of a Jewish state in Eretz-Israel, to be known as the State of Israel.", en.text);
        Assert.AreEqual(LanguageDirection.RTL, he.languageDirection, "Hebrew right to left");
        Assert.IsTrue(he.ClassListContains("start-he"), "Frank Ruhl Libre through the advanced text generator, as the panel's Hebrew");
    }
}
