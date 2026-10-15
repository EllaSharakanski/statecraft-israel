using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>
/// Plays the historical choices and compares the simulation with CBS anchors (content/population.yaml).
///
/// ANCHOR INDEPENDENCE: rules.yaml derives initial_population from the end-1948 CBS figure and
/// natural_increase from end-1948 to end-1949. So the 1948-12-31 and 1949-12-31 anchors are CONSISTENCY
/// checks of that derivation (they should hold by construction). The 1948-11-08 population registration
/// (716,700) and the end-1950 and end-1951 figures (nothing in rules.yaml is derived from them) are INDEPENDENT.
/// </summary>
public class CalibrationTests
{
    // The M1 value. The largest observed margin is 2.3%, at the 1948-11-08 anchor. The 1951 undershoot (-1.2%) is a known
    // limit of a constant natural-increase rate with no emigration.
    const double Tolerance = 0.03;
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Jewish population on a date, linearly interpolated between the two turns around it.</summary>
    static double PopulationOn(List<GameState> history, DateTime date)
    {
        var after = history.First(s => s.Date >= date);
        var before = history.Last(s => s.Date <= date);
        if (after.Date == before.Date) return after.TotalPopulation;
        var t = (date - before.Date).TotalDays / (after.Date - before.Date).TotalDays;
        return before.TotalPopulation + t * (after.TotalPopulation - before.TotalPopulation);
    }

    static readonly DateTime End = new DateTime(1952, 1, 1);

    static readonly DateTime[] IndependentAnchors = { new DateTime(1948, 11, 8), new DateTime(1950, 12, 31), new DateTime(1951, 12, 31) };

    static string Kind(DateTime d) => IndependentAnchors.Contains(d) ? "independent" : "consistency";

    [Fact]
    public void HistoricalPlaythroughMatchesCbsPopulationAnchors()
    {
        var s = Simulation.NewGame(C, 1948, Mode.Historical);
        var history = new List<GameState> { s };
        while (s.Date <= End && s.GameOver == null)
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            history.Add(s);
        }
        Assert.True(s.GameOver == null || s.GameOver == GameOverCodes.MvpEnded, "game over: " + s.GameOver);   // the game ends on 15 January 1952 (R-LOOP-004)
        Assert.Equal(5, C.PopulationAnchors.Count);
        var report = C.PopulationAnchors.Select(a =>
        {
            var sim = PopulationOn(history, a.Date);
            return (a, sim, error: (sim - a.Jews) / a.Jews);
        }).ToList();
        var lines = string.Join("; ", report.Select(r =>
            string.Format(CultureInfo.InvariantCulture, "[{0}] {1:yyyy-MM-dd}: sim {2:N0} vs CBS {3:N0} ({4:+0.0;-0.0;+0.0}%)",
                Kind(r.a.Date), r.a.Date, r.sim, r.a.Jews, r.error * 100)));
        Assert.True(report.All(r => Math.Abs(r.error) <= Tolerance),
            "1948-12-31 and 1949-12-31 are consistency checks of the rules.yaml derivation; the other anchors are independent. " + lines);
    }

    [Fact]
    public void HistoricalPlaythroughSignsAllArmisticesAndEndsTheWar()
    {
        var s = Simulation.NewGame(C, 1948, Mode.Historical);
        while (s.Date <= new DateTime(1950, 1, 1)) s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
        var final = s.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.True(s.GameOver == null, "Game ended early with GameOver = " + s.GameOver + " at " + final);
        var required = new[] { "armistice_egypt", "armistice_lebanon", "armistice_jordan", "armistice_syria" };
        var missing = required.Where(id => !s.FiredEvents.Contains(id)).ToList();
        Assert.True(missing.Count == 0, "Missing armistice events: " + string.Join(", ", missing) + " (final date " + final + ")");
        Assert.True(s.Rung == 2, string.Format(CultureInfo.InvariantCulture, "Expected war to end at rung 2 but rung is {0} at {1}", s.Rung, final));
    }
}
