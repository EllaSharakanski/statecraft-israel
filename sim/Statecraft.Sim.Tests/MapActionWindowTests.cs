using System;
using System.Collections.Generic;
using System.IO;
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
        return MapActions.Check(c, s, MapActions.Housing, 2, 0);   // a land hex next to a settlement: null means placeable
    }

    const string Before = "A housing project can be placed from 1 January 1950";
    const string After = "A housing project could be placed until 31 December 1951";

    [Fact]
    public void WithNoWindowTheActionIsNeverRefusedForTime()
    {
        var c = Load(Housing);
        Assert.Null(Check(c, new DateTime(1949, 1, 1)));
        Assert.Null(Check(c, new DateTime(1955, 1, 1)));
    }

    [Fact]
    public void AnActionIsRefusedBeforeFromAndOnTo()
    {
        var c = Load("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project, from: 1950-01-01, to: 1952-01-01 }\n");
        Assert.Equal(Before, Check(c, new DateTime(1949, 12, 31)));
        Assert.Null(Check(c, new DateTime(1950, 1, 1)));
        Assert.Null(Check(c, new DateTime(1951, 12, 31)));
        Assert.Equal(After, Check(c, new DateTime(1952, 1, 1)));
    }

    [Theory]
    [InlineData("1950-01-01")]   // backwards
    [InlineData("1952-01-01")]   // empty: to is exclusive, so from = to is never open
    public void AWindowThatRunsBackwardsOrIsEmptyIsRefused(string to)
    {
        var e = Assert.Throws<ContentException>(() => Load($"  housing: {{ cost: 8, housing: 0.08, morale: 0.01, name: a housing project, from: 1952-01-01, to: {to} }}\n"));
        Assert.Contains("rules.yaml", e.Message);
        Assert.Contains("map_actions.housing.to", e.Message);
    }

    [Fact]
    public void AnOpenEndedWindowNamesItsOneBound()
    {
        Assert.Equal(Before, Check(Load("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project, from: 1950-01-01 }\n"), new DateTime(1949, 6, 1)));
        Assert.Equal(After, Check(Load("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project, to: 1952-01-01 }\n"), new DateTime(1953, 6, 1)));
    }

    /// <summary>The window is read from RulesAt (#290): an era's rules/&lt;era&gt;.yaml can close an action the base keeps open,
    /// and a backwards window there is reported against that file.</summary>
    [Fact]
    public void AnErasRulesFileCanCloseTheActionAndItsErrorsNameThatFile()
    {
        const string eras = "eras:\n  - { id: founding, name: The Founding, from: 1948-05-15, to: 1952-01-01 }\n  - { id: later, name: Later, from: 1952-01-01, to: 1957-01-01 }\n";
        static Content WithLater(string later)
        {
            var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = eras });
            Directory.CreateDirectory(Path.Combine(dir, "rules"));
            File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), later);
            return ContentLoader.Load(dir);
        }
        var c = WithLater("map_actions:\n  housing: { to: 1953-01-01 }\n");
        Assert.Null(Check(c, new DateTime(1951, 6, 1)));                 // founding: no window
        Assert.Null(Check(c, new DateTime(1952, 6, 1)));                 // later: open until 1953
        Assert.Equal("A housing project could be placed until 31 December 1952", Check(c, new DateTime(1953, 6, 1)));
        var e = Assert.Throws<ContentException>(() => WithLater("map_actions:\n  housing: { from: 1954-01-01, to: 1953-01-01 }\n"));
        Assert.Contains("rules/later.yaml", e.Message);
        Assert.Contains("map_actions.housing.to", e.Message);
    }
}
