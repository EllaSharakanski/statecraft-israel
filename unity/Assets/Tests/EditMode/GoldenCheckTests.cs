using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Sim;

public class GoldenCheckTests
{
    [Test]
    public void TheCheckReportsBothGoldenRunsAsOkInTheEditor()
    {
        var (ok, lines) = GoldenCheck.Run(ContentLoader.Load(Paths.Content));
        Assert.IsTrue(ok, string.Join("\n", lines));
        Assert.AreEqual(new[] { "GOLDEN OK 1950 " + GoldenRun.Hash1950, "GOLDEN OK 1952 " + GoldenRun.Hash1952 }, lines);
    }

    [TestCase("http://localhost:8000/index.html?golden", true)]
    [TestCase("http://localhost:8000/index.html?smoke=5&golden", true)]
    [TestCase("https://html.itch.zone/html/123/golden-project/index.html", false)]
    [TestCase("http://localhost:8000/index.html?goldenrod=1", false)]
    [TestCase("http://localhost:8000/index.html", false)]
    [TestCase("http://localhost:8000/index.html?golden.y", false)]          // Task 24 review M2: a whole query parameter
    [TestCase("http://localhost:8000/index.html?golden-x&smoke=5", false)]
    [TestCase("http://localhost:8000/index.html?golden#top", true)]
    [TestCase("http://localhost:8000/index.html?golden=1", true)]
    public void TheGoldenFlagIsAQueryParameterNotAPathWord(string url, bool expected) =>
        Assert.AreEqual(expected, UrlFlags.Has(url, "golden"));

    [Test]
    public void IntegerFlagsReadTheirValue()
    {
        Assert.AreEqual(5, UrlFlags.Int("http://x/index.html?golden&smoke=5", "smoke"));
        Assert.AreEqual(0, UrlFlags.Int("http://x/smoke=5/index.html", "smoke"));
    }

    [Test]
    public void AnOverflowingIntegerFlagReadsAsAbsent() =>
        Assert.AreEqual(0, UrlFlags.Int("http://x/index.html?smoke=99999999999999999999", "smoke"));

    [Test]
    public void ABootErrorQuitsWithCode1OnlyForADesktopGoldenRun()
    {
        Assert.AreEqual(1, GoldenCheck.QuitCodeOnError(requested: true, webgl: false));
        Assert.IsNull(GoldenCheck.QuitCodeOnError(requested: true, webgl: true));
        Assert.IsNull(GoldenCheck.QuitCodeOnError(requested: false, webgl: false));
    }

    /// <summary>Task 24 review M2: golden, profile and camlog share one rule (UrlFlags.Has): the flag is a whole query
    /// parameter, bare or with a value, ending at '&amp;', '#' or the end.</summary>
    [TestCase("profile")]
    [TestCase("camlog")]
    [TestCase("golden")]
    public void EveryBooleanFlagIsAWholeQueryParameter(string flag)
    {
        Assert.IsTrue(UrlFlags.Has($"http://x/index.html?{flag}", flag));
        Assert.IsTrue(UrlFlags.Has($"http://x/index.html?smoke=5&{flag}&y", flag));
        Assert.IsFalse(UrlFlags.Has($"http://x/index.html?{flag}-x", flag));
        Assert.IsFalse(UrlFlags.Has($"http://x/index.html?{flag}s", flag));
        Assert.IsFalse(UrlFlags.Has($"http://x/{flag}/index.html", flag));
    }
}
