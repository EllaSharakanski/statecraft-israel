using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>#58, second piece: the systems read RulesAt(the turn's starting date) instead of Rules. Each test gives the
/// "later" era (1952-01-01 to 1957-01-01) an override and checks the value changes with the state's date, in the founding era
/// and in the later one.</summary>
public class EraRulesSwitchTests
{
    const string Eras = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1952-01-01
  - id: later
    name: A Later Chapter
    from: 1952-01-01
    to: 1957-01-01
";
    static readonly DateTime Early = new DateTime(1950, 6, 1), Late = new DateTime(1953, 6, 1);

    static Content Load(string laterRules)
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = Eras });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), laterRules);
        return ContentLoader.Load(dir);
    }

    static GameState At(Content c, DateTime date) { var s = Simulation.NewGame(c, 1, Mode.Historical); s.Date = date; return s; }

    [Fact]
    public void AttentionRefillsFromTheTurnsEra()
    {
        var c = Load("attention_per_turn: 7.5\n");
        var s = At(c, Early); AttentionSystem.Refill(c, s); Assert.Equal(c.Rules.AttentionPerTurn, s.Attention);
        s = At(c, Late); AttentionSystem.Refill(c, s); Assert.Equal(7.5, s.Attention);
    }

    [Fact]
    public void AttentionCostComesFromTheTurnsEra()
    {
        var c = Load("attention_cost:\n  set_policy: 9\n");
        Assert.NotEqual(9, c.Rules.AttentionCost["set_policy"]);
        Assert.Equal(c.Rules.AttentionCost["set_policy"] * (1 + c.Rules.RungAttentionStep * (At(c, Early).Rung - 1)),
            AttentionSystem.Cost(c, At(c, Early), "set_policy"));
        var late = At(c, Late);
        Assert.Equal(9 * (1 + c.Rules.RungAttentionStep * (late.Rung - 1)), AttentionSystem.Cost(c, late, "set_policy"));
    }

    [Fact]
    public void TreasuryFlowsAndMobilizationCostComeFromTheTurnsEra()
    {
        var c = Load("tax_per_capita_per_year: 2\nspending_per_capita_per_year: 3\nmobilization_cost_per_year: 5\n");
        var early = At(c, Early); var late = At(c, Late); early.Mobilization = late.Mobilization = 0.5;
        var fe = EconomySystem.Flows(c, early, 1.0); var fl = EconomySystem.Flows(c, late, 1.0);
        Assert.Equal(c.Rules.TaxPerCapitaPerYear * early.TotalPopulation, fe.Tax, 9);
        Assert.Equal(2 * late.TotalPopulation, fl.Tax, 9);
        Assert.Equal(3 * late.TotalPopulation, fl.Spending, 9);
        Assert.Equal(c.Rules.MobilizationCostPerYear * 0.5, fe.Mobilization, 9);
        Assert.Equal(5 * 0.5, fl.Mobilization, 9);
        Assert.Equal(fl.Mobilization, EconomySystem.MobilizationCost(c, Late, 0.5), 12);
    }

    [Fact]
    public void SecurityFloorAndCeasefireComeFromTheTurnsEra()
    {
        var c = Load("rung_min_mobilization: [0.1, 0.2, 0.3, 0.4, 0.5]\nceasefire_weeks: 9\n");
        Assert.Equal(c.Rules.RungMinMobilization[4], SecuritySystem.MinMobilization(c, Early, 5));
        Assert.Equal(0.5, SecuritySystem.MinMobilization(c, Late, 5));
        Assert.Equal(0.5, SecuritySystem.EffectiveMobilization(c, Late, 5, 0.1));
        Assert.Equal(c.Rules.CeasefireWeeks, SecuritySystem.CeasefireWeeksAfter(c, Early, 3, 0, 5));
        Assert.Equal(9, SecuritySystem.CeasefireWeeksAfter(c, Late, 3, 0, 5));
        var s = At(c, Late); s.Mobilization = 0; s.Rung = 3; s.CeasefireWeeksLeft = 0;
        SecuritySystem.SetRung(c, s, 5, new List<Cause>());
        Assert.Equal(9, s.CeasefireWeeksLeft);
        Assert.True(s.Mobilization >= 0.5);   // raised to the later era's floor
    }

    [Fact]
    public void MapActionAvailabilityComesFromTheTurnsEra()
    {
        var c = Load("map_actions_from: 1960-01-01\n");
        var early = MapActions.Check(c, At(c, Early), MapActions.Housing, 0, 0);
        var late = MapActions.Check(c, At(c, Late), MapActions.Housing, 0, 0);
        Assert.DoesNotContain("1960", early ?? "");
        Assert.Contains("1960", late);
    }

    [Fact]
    public void ThePopulationGrowsByTheTurnsEraRate()
    {
        var c = Load("natural_increase_per_year: 0.5\n");
        double Grow(DateTime d)
        {
            var s = At(c, d); var before = s.TotalPopulation;
            PopulationSystem.Apply(c, s, d.AddDays(7), 0.1, new List<Cause>());
            return s.TotalPopulation / before;
        }
        Assert.True(Grow(Late) > Grow(Early));
    }

    /// <summary>The only places the sim reads the base rules directly are the values <c>NewGame</c> reads once (an era
    /// override cannot set them) and a map action's display name (an override cannot remove an action). Everything else
    /// reads <c>RulesAt(date)</c>, so a new <c>c.Rules.X</c> read in a system fails here until it is deliberate.</summary>
    [Fact]
    public void OnlyStartValuesAndMapActionNamesReadTheBaseRulesDirectly()
    {
        var simDir = Path.Combine(Directory.GetParent(RealContentTests.ContentRoot())!.FullName, "sim", "Statecraft.Sim");
        var allowed = new HashSet<string> { "Start", "TreasuryStart", "FxStart", "BudgetStart", "InitialPopulation", "StartRung", "MapActions" };
        var bad = new List<string>();
        foreach (var f in Directory.GetFiles(simDir, "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(f);
            if (name == "ContentLoader.cs" || name == "Content.cs") continue;
            var lines = File.ReadAllLines(f);
            for (int i = 0; i < lines.Length; i++)
                foreach (Match m in Regex.Matches(lines[i], @"\bc\.Rules\.(\w+)"))
                    if (!allowed.Contains(m.Groups[1].Value)) bad.Add($"{name}:{i + 1} {m.Value}");
        }
        Assert.True(bad.Count == 0, "read RulesAt(date) instead: " + string.Join(", ", bad));
    }
}
