using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Map-action windows (#57): from/to on a rules.yaml map action.</summary>
public class MapActionWindowTests
{
    const string Housing = "  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project }\n";

    static Content Load(string housingLine)
    {
        var rules = Fixture.Files["rules.yaml"].Replace(Housing, housingLine);
        return ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = rules }));
    }

    static string? Check(Content c, DateTime date)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical); s.Date = date;
        return MapActions.Check(c, s, MapActions.Housing, 0, 0);
    }

    const string Gone = "That map action is not available at this time";

    [Fact]
    public void WithNoWindowTheActionIsNeverRefusedForTime()
    {
        var c = Load(Housing);
        Assert.NotEqual(Gone, Check(c, new DateTime(1949, 1, 1)));
        Assert.NotEqual(Gone, Check(c, new DateTime(1955, 1, 1)));
    }

    [Fact]
    public void AnActionIsRefusedBeforeFromAndOnTo()
    {
        var c = Load("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project, from: 1950-01-01, to: 1952-01-01 }\n");
        Assert.Equal(Gone, Check(c, new DateTime(1949, 12, 31)));
        Assert.NotEqual(Gone, Check(c, new DateTime(1950, 1, 1)));
        Assert.NotEqual(Gone, Check(c, new DateTime(1951, 12, 31)));
        Assert.Equal(Gone, Check(c, new DateTime(1952, 1, 1)));
    }

    [Fact]
    public void AWindowThatRunsBackwardsIsRefused()
    {
        var e = Assert.Throws<ContentException>(() => Load("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project, from: 1952-01-01, to: 1950-01-01 }\n"));
        Assert.Contains("map_actions.housing.to", e.Message);
    }
}
