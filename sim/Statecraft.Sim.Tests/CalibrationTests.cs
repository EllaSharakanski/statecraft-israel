using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>
/// Plays the historical choices and compares the simulation with CBS anchors (content/population.yaml).
///
/// ANCHOR INDEPENDENCE: rules.yaml derives initial_population from the end-1948 CBS figure and
/// natural_increase from end-1948 to end-1949. So the 1948-12-31 and 1949-12-31 anchors are CONSISTENCY
/// checks of that derivation (they should hold by construction). The 1948-11-08 population registration
/// (716,700) is the only INDEPENDENT anchor.
/// </summary>
public class CalibrationTests
{
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

    static string Kind(DateTime d) => d == new DateTime(1948, 11, 8) ? "independent" : "consistency";

    [Fact]
    public void HistoricalPlaythroughMatchesCbsPopulationAnchors()
    {
        var s = Simulation.NewGame(C, 1948, Mode.Historical);
        var history = new List<GameState> { s };
        while (s.Date <= new DateTime(1950, 1, 1))
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            history.Add(s);
        }
        Assert.Null(s.GameOver);
        Assert.Equal(3, C.PopulationAnchors.Count);
        var report = C.PopulationAnchors.Select(a =>
        {
            var sim = PopulationOn(history, a.Date);
            return (a, sim, error: (sim - a.Jews) / a.Jews);
        }).ToList();
        var lines = string.Join("; ", report.Select(r =>
            $"[{Kind(r.a.Date)}] {r.a.Date:yyyy-MM-dd}: sim {r.sim:N0} vs CBS {r.a.Jews:N0} ({r.error * 100:+0.0;-0.0}%)"));
        Assert.True(report.All(r => Math.Abs(r.error) <= Tolerance),
            "Only 1948-11-08 is an independent anchor; the other two are consistency checks of the rules.yaml derivation. " + lines);
    }

    [Fact]
    public void HistoricalPlaythroughSignsAllArmisticesAndEndsTheWar()
    {
        var s = Simulation.NewGame(C, 1948, Mode.Historical);
        while (s.Date <= new DateTime(1950, 1, 1)) s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
        Assert.Subset(s.FiredEvents, new SortedSet<string> { "armistice_egypt", "armistice_lebanon", "armistice_jordan", "armistice_syria" });
        Assert.Equal(2, s.Rung);
    }
}
