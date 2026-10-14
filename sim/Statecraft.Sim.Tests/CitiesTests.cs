using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD slice's city (Phase 1 PR 3): AssignWorkers (R-CITY-001), and the turn's production, housing, Social
/// cohesion and arrivals in TurnResolver (R-CITY-002 to R-CITY-005, in the order of spec 2026-10-12 section 2.3, D-A6).</summary>
public class CitiesTests
{
    static readonly Content Mvp = MvpSlice.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture with one city: 20 jobs, 10 civilians, 6 of them employed, housing for 12.</summary>
    static Content Town(string city = "  - { id: town_a, housing_capacity: 12, job_slots: 20, civilians: 10, employed: 6, facilities: [] }\n") =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["cities.yaml"] = "cities:\n" + city })));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    [Fact]
    public void ANewGddGameStartsWithItsCities()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var city = Assert.Single(s.Cities.Values);
        Assert.Equal(("tel_aviv", 120, 90, 100, 80, 0), (city.Id, city.HousingCapacity, city.JobSlots, city.Civilians, city.Employed, city.Refugees));
    }

    [Fact]
    public void ALegacyGameHasNoCities() =>
        Assert.Empty(Simulation.NewGame(ContentLoader.Load(RealContentTests.ContentRoot()), GoldenRun.Seed, Mode.Historical).Cities);

    // R-CITY-001
    [Fact]
    public void AssigningWorkersSetsEmployed()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var r = Step(Mvp, s, new AssignWorkers("tel_aviv", "job", 90));
        Assert.Empty(r.Rejected);
        Assert.Equal(90, r.State.Cities["tel_aviv"].Employed);
        Assert.Contains(r.Why, x => x.System == "city" && x.Message == "Tel Aviv: 90 groups in work");
    }

    // R-CITY-001
    [Theory]
    [InlineData("town_a", "job", -1, "The number of workers cannot be negative")]
    [InlineData("town_a", "job", 21, "Town A has 20 jobs")]
    [InlineData("town_a", "job", 11, "Town A has 10 civilians")]
    [InlineData("nowhere", "job", 1, "There is no such city")]
    [InlineData("town_a", "craftsman", 1, "There is no such role")]
    public void TheLimitsAreRefusedAndChangeNothing(string city, string role, int count, string reason)
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new AssignWorkers(city, role, count));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());   // as if the order had not been given: Attention refunded too
    }

    // R-CITY-001
    [Fact]
    public void TheLegacyLoopRefusesTheOrderAndRefundsAttention()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var r = Step(c, s, new AssignWorkers("tel_aviv", "job", 1));
        Assert.Equal("That order is not available in this game", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-CITY-001
    [Fact]
    public void AnAssignOrderSurvivesASave()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        g.Step(new Command[] { new AssignWorkers("tel_aviv", "job", 85) });
        var text = g.Save.Write();
        Assert.Contains("\nwork tel_aviv job 85\n", text);
        Assert.Equal(g.State.Hash(), GameSession.Load(Mvp, text, out _).State.Hash());
    }
}
