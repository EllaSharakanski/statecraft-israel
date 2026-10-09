using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

public class CultureTests
{
    const string Event = @"
id: levy
title: Emergency levy
text: '{finance_minister} asks for a levy.'
from: 1948-06-01
options:
  - id: pay
    text: Pay it
    effects:
      - target: treasury
        delta: -10.5
";

    static readonly Content C = ContentLoader.Load(Fixture.Write(
        new Dictionary<string, string>
        {
            ["aliyah.yaml"] = Fixture.Files["aliyah.yaml"].Replace("europe: 230", "europe: 23000"),
            ["legislatures.yaml"] = Fixture.Files["legislatures.yaml"].Replace("  - id: house\n", "  - id: house\n    elected: 1949-01-25\n"),
        },
        new Dictionary<string, string> { ["levy.yaml"] = Event }));

    /// <summary>Runs every system that writes numbers or dates into causes or rejection reasons, under one culture.</summary>
    static List<string> Run(CultureInfo culture)
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            var why = new List<Cause>();
            var s = new GameState { Date = new DateTime(1948, 6, 1), RngState = 1 };
            EffectsSystem.Init(C, s); EconomySystem.Init(C, s); PopulationSystem.Init(C, s);
            SecuritySystem.Init(C, s, why);                                    // call-up floor (percent)
            s.Policies["rationing"] = 1.0;
            EffectsSystem.Apply(C, s, why);                                    // indicator deltas
            s.Indicators["food_supply"] = 0.25; EffectsSystem.UpdateSituations(C, s, why);   // situation value
            PopulationSystem.Apply(C, s, new DateTime(1948, 9, 1), 0.25, why);  // thousands
            s.Mobilization = 1.0; s.Treasury = 1;
            EconomySystem.Apply(C, s, 0.25, why);                              // treasury, debt, fx
            PoliticsSystem.UpdateStability(C, s, 0.25, why);
            PoliticsSystem.ReportChanges(C, new DateTime(1949, 2, 1), new DateTime(1949, 5, 1), why);   // election date
            EventSystem.Trigger(C, s, why);
            EventSystem.Choose(C, s, "levy", "pay", why);                      // event delta
            return why.Select(w => w.Message).ToList();
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("th-TH")]
    public void CauseTextsAreCultureInvariant(string culture)
    {
        var expected = Run(CultureInfo.InvariantCulture);
        Assert.Contains(expected, m => m.Contains("9,160 immigrants"));   // grouping is exercised
        Assert.Contains(expected, m => m.Contains("elected 1949-01-25"));  // calendar is exercised
        Assert.Equal(expected, Run(new CultureInfo(culture)));
    }
}
