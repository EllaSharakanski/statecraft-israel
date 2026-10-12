using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Scenario goldens (#296): every file in scenarios/ is played from its seed and checked against the manifest
/// (name "scenario-&lt;name&gt;") and a committed snapshot, like the hash goldens (docs/determinism.md).</summary>
public class ScenarioTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    public static System.Collections.Generic.IEnumerable<object[]> Files() =>
        Directory.GetFiles(Scenario.Dir(), "*.yaml").OrderBy(f => f, StringComparer.Ordinal).Select(f => new object[] { Path.GetFileNameWithoutExtension(f) });

    [Theory]
    [MemberData(nameof(Files))]
    public void ScenarioMatchesItsGolden(string file)
    {
        var sc = Scenario.Parse(File.ReadAllText(Path.Combine(Scenario.Dir(), file + ".yaml")));
        Assert.Equal(file, sc.Name);
        var s = sc.Play(C);
        var name = "scenario-" + sc.Name;
        // A new scenario has no manifest line yet: the blessing run (STATECRAFT_UPDATE_GOLDEN=1) adds it with its snapshot.
        var expected = GoldenManifest.UpdateRequested ? GoldenManifest.Read().GetValueOrDefault(name, "") : GoldenManifest.Expected(name);
        GoldenSnapshot.Check(name, s, expected);
    }

    [Fact]
    public void TheScenariosTakeDifferentPathsFromHistory()
    {
        var hashes = Directory.GetFiles(Scenario.Dir(), "*.yaml").Select(f => Scenario.Parse(File.ReadAllText(f)).Play(C).Hash()).ToList();
        Assert.True(hashes.Count >= 2);
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
        Assert.DoesNotContain(GoldenRun.Hash1950, hashes);
    }

    const string Head = "name: t\nuntil: 1948-08-01\nsteps:\n  - on: 1948-05-15\n    commands:\n";

    [Fact]
    public void ARejectedCommandFailsTheRunWithTheDateAndTheReason()
    {
        var sc = Scenario.Parse(Head + "      - { kind: set_policy, policy: no_such_policy, level: 1.0 }\n");
        var e = Assert.Throws<InvalidOperationException>(() => sc.Play(C));
        Assert.Contains("1948-05-15", e.Message);
        Assert.Contains("set_policy", e.Message);
        Assert.Contains("no_such_policy", e.Message);
    }

    [Fact]
    public void AStepAfterTheRunEndsFailsInsteadOfBeingSkipped()
    {
        var sc = Scenario.Parse("name: t\nuntil: 1948-06-01\nsteps:\n  - on: 1949-01-01\n    commands:\n      - { kind: set_mobilization, level: 0.5 }\n");
        Assert.Contains("never ran", Assert.Throws<InvalidOperationException>(() => sc.Play(C)).Message);
    }

    [Theory]
    [InlineData("      - { kind: fly_to_moon }\n", "unknown command kind 'fly_to_moon'")]
    [InlineData("      - { kind: set_policy, policy: rationing }\n", "needs 'level'")]
    [InlineData("      - { kind: set_policy, policy: rationing, level: 1, extra: 2 }\n", "unknown field 'extra'")]
    [InlineData("      - { policy: rationing }\n", "needs 'kind'")]
    [InlineData("      - { kind: set_policy, policy: rationing, level: lots }\n", "must be a number")]
    public void AMalformedCommandIsRefusedWithTheProblemNamed(string command, string expected) =>
        Assert.Contains(expected, Assert.Throws<InvalidDataException>(() => Scenario.Parse(Head + command)).Message);

    [Fact]
    public void ABadDateIsRefused() =>
        Assert.Contains("yyyy-MM-dd", Assert.Throws<InvalidDataException>(() => Scenario.Parse("name: t\nuntil: soon\n")).Message);
}
