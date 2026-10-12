using System;
using Statecraft.Sim;

/// <summary>The readable golden failure (#256): Describe() lists what Hash() hashes, and Diff names what moved.</summary>
public class GoldenSnapshotTests
{
    static GameState Fresh() => Simulation.NewGame(ContentLoader.Load(RealContentTests.ContentRoot()), GoldenRun.Seed, Mode.Historical);

    [Fact]
    public void ChangingOneIndicatorShowsThatIndicatorInTheDiff()
    {
        var a = Fresh(); var b = a.Clone();
        var key = a.Indicators.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        b.Indicators[key] += 0.125;
        Assert.NotEqual(a.Hash(), b.Hash());
        var diff = GoldenSnapshot.Diff(a.Describe(), b.Describe());
        Assert.Contains("[ind]", diff);
        Assert.Contains(key + "=", diff);
        Assert.DoesNotContain("[pol]", diff);
        Assert.DoesNotContain("[bud]", diff);
    }

    [Fact]
    public void ChangingTheTreasuryAndAnAppointmentShowsBothSections()
    {
        var a = Fresh(); var b = a.Clone();
        b.Treasury += 1;
        b.FiredEvents.Add("zzz_probe_event");
        var diff = GoldenSnapshot.Diff(a.Describe(), b.Describe());
        Assert.Contains("treasury=", diff);
        Assert.Contains("zzz_probe_event", diff);
    }

    [Fact]
    public void IdenticalStatesHaveNoDiffAndTheSameHash()
    {
        var a = Fresh(); var b = a.Clone();
        Assert.Equal(a.Hash(), b.Hash());
        Assert.Equal(a.Describe(), b.Describe());
        Assert.Contains("identical", GoldenSnapshot.Diff(a.Describe(), b.Describe()));
    }

    [Fact]
    public void DescribeIsTheSameInEveryCulture()
    {
        var a = Fresh();
        var before = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var invariant = a.Describe();
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal(invariant, a.Describe());
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = before; }
    }
}
