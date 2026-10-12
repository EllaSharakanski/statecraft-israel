using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Aliyah region windows (#57): the loader checks a period against the windows of the regions it counts.</summary>
public class RegionWindowTests
{
    static Content Load(string regions, string series = "")
    {
        var a = Fixture.Files["aliyah.yaml"].Replace("  - { id: asia, name: Asia }\n", regions);
        if (series != "") a = a.Replace("series:\n", "series:\n" + series);
        return ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["aliyah.yaml"] = a }));
    }

    static void Refused(string regions, string expected, string series = "") =>
        Assert.Contains(expected, Assert.Throws<ContentException>(() => Load(regions, series)).Message);

    [Fact]
    public void ARegionWithNoWindowBehavesAsBefore() => Assert.Equal(2, Load("  - { id: asia, name: Asia }\n").Regions.Count);

    [Fact]
    public void AWindowThatCoversThePeriodIsAccepted()
    {
        var c = Load("  - { id: asia, name: Asia, from: 1948-05-15, to: 1949-01-01 }\n");
        Assert.Equal(new System.DateTime(1949, 1, 1), c.Regions[1].To);
    }

    [Theory]
    [InlineData("1949-01-01")]   // backwards
    [InlineData("1950-01-01")]   // empty: to is exclusive
    public void AWindowThatRunsBackwardsOrIsEmptyIsRefused(string to) =>
        // A region no period counts, so only the window's own check can refuse it.
        Refused($"  - {{ id: asia, name: Asia }}\n  - {{ id: africa, name: Africa, from: 1950-01-01, to: {to} }}\n", "aliyah.yaml: regions[africa].to");

    [Fact] public void ARegionThatEndsInsideThePeriodIsRefused() =>
        Refused("  - { id: asia, name: Asia, to: 1948-12-31 }\n", "aliyah.yaml: series[0].origins.asia: region 'asia' does not exist for the whole period");

    [Fact] public void ARegionThatBeginsInsideThePeriodIsRefused() =>
        Refused("  - { id: asia, name: Asia, from: 1948-06-01 }\n", "does not exist for the whole period");

    [Fact] public void ThePeriodsLastDayIsInclusiveSoToMayBeTheDayAfter() =>
        Assert.NotNull(Load("  - { id: asia, name: Asia, to: 1949-01-01 }\n"));
}
