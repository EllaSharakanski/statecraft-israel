using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Sim;

/// <summary>turn_lengths (GDD v0.1 Phase 1, refs #81): the turn length on each security rung is data, read in the era the
/// turn starts in. The default is the legacy table, so the goldens do not move.</summary>
public class TurnLengthsTests
{
    static Content Load(string extraRules) => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        { ["rules.yaml"] = Fixture.Files["rules.yaml"] + extraRules }));

    // R-TURN-001
    [Theory]
    [InlineData(1, TurnLength.Quarter)] [InlineData(2, TurnLength.Quarter)] [InlineData(3, TurnLength.Quarter)]
    [InlineData(4, TurnLength.Month)] [InlineData(5, TurnLength.Week)]
    public void TheDefaultReproducesTheLegacyTable(int rung, TurnLength expected)
    {
        var c = ContentLoader.Load(Fixture.Write());   // the fixture has no turn_lengths; the real game sets all month (CutoverTests)
        Assert.Equal(expected, SecuritySystem.TurnLengthFor(c, c.Rules.Start, rung));
        Assert.Equal(SecuritySystem.TurnLengthFor(rung), SecuritySystem.TurnLengthFor(c, c.Rules.Start, rung));
    }

    // R-TURN-001
    [Fact]
    public void AllMonthsGivesMonthlyTurnsOnTheFifteenth()
    {
        var c = Load("turn_lengths: [month, month, month, month, month]\n");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(5, s.Rung);   // the fixture starts at full war: weekly turns in the default table
        var dates = new List<DateTime>();
        for (int i = 0; i < 3; i++) { s = Simulation.Step(c, s, new Command[0]).State; dates.Add(s.Date); }
        Assert.Equal(new[] { new DateTime(1948, 6, 15), new DateTime(1948, 7, 15), new DateTime(1948, 8, 15) }, dates);
    }

    // R-TURN-001
    [Fact]
    public void AnEraOverrideChangesTheTableFromItsFirstDay()
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1948-07-01
  - id: later
    name: A Later Chapter
    from: 1948-07-01
    to: 1957-01-01
" });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "turn_lengths: [month, month, month, month, month]\n");
        var c = ContentLoader.Load(dir);
        Assert.Equal(TurnLength.Week, SecuritySystem.TurnLengthFor(c, new DateTime(1948, 6, 30), 5));
        Assert.Equal(TurnLength.Month, SecuritySystem.TurnLengthFor(c, new DateTime(1948, 7, 1), 5));
        Assert.Equal(TurnLength.Quarter, SecuritySystem.TurnLengthFor(c, new DateTime(1948, 6, 30), 1));
    }

    // R-TURN-001
    [Fact]
    public void ARunReadsTheTableOfTheEraTheTurnStartsIn()
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1948-07-01
  - id: later
    name: A Later Chapter
    from: 1948-07-01
    to: 1957-01-01
" });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "turn_lengths: [month, month, month, month, month]\n");
        var c = ContentLoader.Load(dir);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        while (s.Date < new DateTime(1948, 7, 1)) s = Simulation.Step(c, s, new Command[0]).State;
        Assert.Equal(new DateTime(1948, 7, 1), s.Date);   // the founding era's turns end on the boundary
        s = Simulation.Step(c, s, new Command[0]).State;
        Assert.Equal(new DateTime(1948, 8, 1), s.Date);   // the first turn in the later era is a month
    }

    [Theory]
    [InlineData("turn_lengths: [month, month, month, month]\n", "turn_lengths", "needs exactly 5 values (rungs 1-5)")]
    [InlineData("turn_lengths: [month, month, fortnight, month, month]\n", "turn_lengths[2]", "must be quarter, month or week")]
    [InlineData("turn_lengths: [month, month, Month, month, month]\n", "turn_lengths[2]", "must be quarter, month or week")]
    public void ABadTableFailsNamingTheField(string rules, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(rules));
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void ABadTableInAnEraOverrideFailsNamingTheEraFile()
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1948-07-01
  - id: later
    name: A Later Chapter
    from: 1948-07-01
    to: 1957-01-01
" });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "turn_lengths: [month]\n");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Equal("rules/later.yaml", ex.File);
        Assert.Equal("turn_lengths", ex.Field);
    }

    [Fact]
    public void ParseLengthRefusesAnUnknownName() =>
        Assert.Throws<ArgumentException>(() => SimTime.ParseLength("Month"));   // names are lowercase, as in YAML
}
