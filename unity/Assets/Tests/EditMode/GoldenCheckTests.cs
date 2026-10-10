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
    public void TheGoldenFlagIsAQueryParameterNotAPathWord(string url, bool expected) =>
        Assert.AreEqual(expected, UrlFlags.Has(url, "golden"));

    [Test]
    public void IntegerFlagsReadTheirValue()
    {
        Assert.AreEqual(5, UrlFlags.Int("http://x/index.html?golden&smoke=5", "smoke"));
        Assert.AreEqual(0, UrlFlags.Int("http://x/smoke=5/index.html", "smoke"));
    }
}
