using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>golden/manifest.txt is the one place a golden hash is pinned (#293). These tests keep every other place that
/// shows a hash in step with it, and name the place that is out of date.</summary>
public class GoldenManifestTests
{
    static string Repo => Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
    static string Text(params string[] path) => File.ReadAllText(Path.Combine(new[] { Repo }.Concat(path).ToArray()));

    [Fact]
    public void TheManifestHoldsExactlyTheKnownGoldens()
    {
        var m = GoldenManifest.Read();
        var scenarios = System.IO.Directory.GetFiles(ScenarioTests.Dir(), "*.yaml").Select(f => "scenario-" + System.IO.Path.GetFileNameWithoutExtension(f));
        var known = new[] { "hash1950", "hash1952", "smoke5", "mvp-end" }.Concat(scenarios).OrderBy(k => k, StringComparer.Ordinal).ToArray();   // one line per scenario file (#296)
        Assert.Equal(known, m.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        Assert.All(m.Values, h => Assert.Matches("^[0-9a-f]{64}$", h));
    }

    [Fact]
    public void EveryScenarioSnapshotHasItsScenarioFile()
    {
        var names = System.IO.Directory.GetFiles(ScenarioTests.Dir(), "*.yaml").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToHashSet();
        var snapshots = System.IO.Directory.GetFiles(System.IO.Path.Combine(Repo, "sim", "Statecraft.Sim.Tests", "golden"), "scenario-*.txt");
        Assert.NotEmpty(snapshots);
        foreach (var f in snapshots)
        {
            var n = System.IO.Path.GetFileNameWithoutExtension(f).Substring("scenario-".Length);
            Assert.True(names.Contains(n), $"golden/{System.IO.Path.GetFileName(f)} has no scenarios/{n}.yaml: delete the snapshot with its scenario");
        }
    }

    [Fact]
    public void TheLibraryConstantsUnityComparesAgainstEqualTheManifest()
    {
        Assert.Equal(GoldenRun.Hash1950, GoldenManifest.Expected("hash1950"));
        Assert.Equal(GoldenRun.Hash1952, GoldenManifest.Expected("hash1952"));
    }

    [Theory]
    [InlineData("hash1950")]
    [InlineData("hash1952")]
    [InlineData("smoke5")]
    public void TheDocsAndTheWebCheckShowTheManifestHash(string name)
    {
        var hash = GoldenManifest.Expected(name);
        Assert.True(Text("docs", "determinism.md").Contains(hash), $"docs/determinism.md does not show the current {name} hash {hash}");
        Assert.True(Text("docs", "RELEASING.md").Contains(hash), $"docs/RELEASING.md does not show the current {name} hash {hash}");
        if (name != "smoke5")
            Assert.True(Text("tools", "web_check.py").Contains(hash), $"tools/web_check.py does not show the current {name} hash {hash}");
    }

    [Fact]
    public void TheArtCheckpointReadsTheManifestInsteadOfPinningAHash()
    {
        var script = Text("tools", "art_checkpoint.sh");
        Assert.Contains("sim/Statecraft.Sim.Tests/golden/manifest.txt", script);
        Assert.DoesNotMatch("[0-9a-f]{64}", script);
    }

    [Fact]
    public void RewritingAConstantChangesOnlyThatConstant()
    {
        var src = Text("sim", "Statecraft.Sim", "GoldenRun.cs");
        var new1950 = new string('a', 64);
        var after = GoldenManifest.RewriteConstant(src, "Hash1950", new1950);
        Assert.Contains($"Hash1950 = \"{new1950}\"", after);
        Assert.Contains($"Hash1952 = \"{GoldenRun.Hash1952}\"", after);
        Assert.NotEqual(src, after);
    }
}
