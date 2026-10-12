using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Dated windows for model records (#57, chapters design 3.2): the active view, the state following it, the command
/// check and the loader's reference checks. Records with no window behave as before (the goldens pin that).</summary>
public class ContentWindowsTests
{
    static readonly DateTime D0 = new DateTime(1948, 5, 15);
    static readonly DateTime Cut = new DateTime(1948, 8, 1);

    static string Model(Func<string, string>? edit = null) => edit == null ? Fixture.Files["model.yaml"] : edit(Fixture.Files["model.yaml"]);
    static string Offices(Func<string, string> edit) => edit(Fixture.Files["offices.yaml"]);

    static Content Load(string model, string? offices = null, Dictionary<string, string>? events = null)
    {
        var o = new Dictionary<string, string> { ["model.yaml"] = model };
        if (offices != null) o["offices.yaml"] = offices;
        return ContentLoader.Load(Fixture.Write(o, events));
    }

    static string Window(string text, string anchor, string window) => text.Replace(anchor, anchor + "\n    " + window);

    static void Refused(string model, string expected, string? offices = null, Dictionary<string, string>? events = null)
    {
        var e = Assert.Throws<ContentException>(() => Load(model, offices, events));
        Assert.Contains(expected, e.Message);
    }

    // ---- the active view

    [Fact]
    public void WithNoWindowEveryDateHasTheSameFullView()
    {
        var c = Load(Model());
        Assert.Same(c.At(D0), c.At(new DateTime(1956, 1, 1)));
        Assert.Equal(c.Policies.Count, c.At(D0).Policies.Count);
        Assert.Equal(c.Indicators.Count, c.At(D0).Indicators.Count);
    }

    [Fact]
    public void ARecordIsActiveOnItsFromAndNotOnItsTo()
    {
        var c = Load(Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-08-01\n    to: 1948-11-01")));
        Assert.DoesNotContain(c.At(Cut.AddDays(-1)).Policies, p => p.Id == "maabarot");
        Assert.Contains(c.At(Cut).Policies, p => p.Id == "maabarot");
        Assert.Contains(c.At(new DateTime(1948, 10, 31)).Policies, p => p.Id == "maabarot");
        Assert.DoesNotContain(c.At(new DateTime(1948, 11, 1)).Policies, p => p.Id == "maabarot");
    }

    [Fact]
    public void TheActiveListsAreBuiltOncePerSegmentAtLoadNotPerCall()
    {
        var c = Load(Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-08-01")));
        Assert.Same(c.At(D0), c.At(Cut.AddDays(-30)));          // one segment before the cut
        Assert.Same(c.At(Cut), c.At(new DateTime(1950, 1, 1))); // one segment after it
        Assert.NotSame(c.At(D0), c.At(Cut));
        var first = c.At(D0);
        for (int i = 0; i < 100; i++) Assert.Same(first, c.At(D0.AddDays(i % 60)));
    }

    // ---- the state follows the windows

    // The fixture's turns run weekly to 12 June, then every quarter (the ceasefire ends the war rung): 5/15 ... 6/12, 9/12.
    static Content Windowed() => Load(Model(m => Window(Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-06-20"), "  - id: housing\n    base: 0.3", "to: 1948-08-01")));

    [Fact]
    public void ANewGameHoldsOnlyTheRecordsActiveOnTheStartDate()
    {
        var c = Windowed();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.DoesNotContain("maabarot", s.Policies.Keys);
        Assert.Contains("housing", s.Indicators.Keys);
    }

    static GameState Play(Content c, DateTime until)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        while (s.Date < until && s.GameOver == null) s = Simulation.Step(c, s, new Command[0]).State;
        return s;
    }

    [Fact]
    public void ARecordThatBeginsIsAddedAtItsDefaultAndOneThatEndsIsRemoved()
    {
        var c = Windowed();
        var early = Play(c, new DateTime(1948, 6, 10));
        Assert.DoesNotContain("maabarot", early.Policies.Keys);
        Assert.Contains("housing", early.Indicators.Keys);
        var later = Play(c, new DateTime(1948, 9, 10));
        Assert.Equal(0.0, later.Policies["maabarot"]);             // added as Init would add it
        Assert.DoesNotContain("housing", later.Indicators.Keys);   // removed with its shift
        Assert.DoesNotContain("housing", later.IndicatorShifts.Keys);
        Assert.NotEmpty(later.Hash());                              // the hash never reads a missing key
        Assert.NotEmpty(later.Describe());
    }

    [Fact]
    public void ARecordThatBeginsEntersWithAZeroContributionAndTheStateStaysConsistent()
    {
        var m = Model(x => x.Replace("  - id: rationing_food\n    source: rationing", "  - id: rationing_food\n    from: 1948-08-01\n    source: rationing"));
        var c = Load(m);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.DoesNotContain("rationing_food", s.EffectContributions.Keys);
        s = Play(c, new DateTime(1948, 8, 10));
        Assert.Equal(0.0, s.EffectContributions["rationing_food"]);
        Assert.Equal(c.Effects.Count, s.EffectContributions.Count);
    }

    [Fact]
    public void ASituationThatEndsLeavesTheActiveSet()
    {
        var m = Model(x => x.Replace("    on_below: 0.3", "    to: 1948-09-01\n    on_below: 0.3").Replace("base: 0.4\n    name: Food supply", "base: 0.1\n    name: Food supply"));
        var c = Load(m);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s = Simulation.Step(c, s, new Command[0]).State;
        Assert.Contains("hunger", s.ActiveSituations);
        s = Play(c, new DateTime(1948, 9, 10));
        Assert.DoesNotContain("hunger", s.ActiveSituations);
    }

    // ---- commands

    [Fact]
    public void SetPolicyOutsideItsWindowIsRefusedAndChangesNothing()
    {
        var c = Windowed();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var before = s.Hash();
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("maabarot", 1.0) });
        Assert.Contains("not available", r.Rejected.Single().Reason);
        Assert.DoesNotContain("maabarot", r.State.Policies.Keys);
        Assert.Equal(s.Attention, Simulation.NewGame(c, 1, Mode.Historical).Attention);
        Assert.Equal(before, s.Hash());   // Step never mutates its input
    }

    [Fact]
    public void SetPolicyInsideItsWindowWorksAndAnUnknownPolicyKeepsItsOwnReason()
    {
        var c = Windowed();
        var s = Play(c, new DateTime(1948, 8, 10));
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("maabarot", 1.0), new SetPolicy("nope", 1.0) });
        Assert.Equal(1.0, r.State.Policies["maabarot"]);
        Assert.Contains("does not exist", r.Rejected.Single().Reason);
    }

    // ---- the loader refuses a reference to a record that is not active for the whole window of the record using it

    [Fact]
    public void AWindowMustRunForward() =>
        Refused(Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-09-01\n    to: 1948-08-01")), "from must be before to");

    [Fact]
    public void AnEffectSourceThatIsNotActiveThroughTheEffectIsRefused() =>
        Refused(Model(m => Window(m, "  - id: rationing\n    default: 0.0", "from: 1948-08-01")), "source 'rationing' is not active");

    [Fact]
    public void AnEffectTargetThatIsNotActiveThroughTheEffectIsRefused() =>
        Refused(Model(m => Window(m, "  - id: food_supply\n    base: 0.4", "to: 1948-09-01")), "target 'food_supply' is not active");

    [Fact]
    public void ASituationOnAnIndicatorThatEndsEarlyIsRefused() =>
        Refused(Model(m => Window(m, "  - id: food_supply\n    base: 0.4", "to: 1948-09-01")
            .Replace("  - id: rationing_food\n", "  - id: rationing_food\n    to: 1948-09-01\n")), "indicator 'food_supply' is not active");

    [Fact]
    public void AVoterGroupWeightOnAnIndicatorThatBeginsLateIsRefused() =>
        Refused(Model(m => Window(m, "  - id: morale\n    base: 0.5", "from: 1948-08-01")), "is not active for the whole window");

    [Fact]
    public void AWindowedRecordMayUseARecordWithAWiderWindow()
    {
        var m = Model(x => Window(x, "  - id: rationing\n    default: 0.0", "from: 1948-06-01")
            .Replace("  - id: rationing_food\n", "  - id: rationing_food\n    from: 1948-07-01\n").Replace("  - id: rationing_morale\n", "  - id: rationing_morale\n    from: 1948-07-01\n"));
        Assert.NotNull(Load(m));
    }

    const string Event = "id: probe\ntitle: Probe\ntext: A probe.\nanchor: soft\nfrom: 1948-06-01\nto: 1948-12-01\n{0}options:\n  - id: a\n    text: Do a\n{1}";

    [Fact]
    public void AnEventConditionOnAnIndicatorThatBeginsInsideItsWindowIsRefused() =>
        Refused(Model(m => Window(m, "  - id: housing\n    base: 0.3", "from: 1948-07-01")), "not active for the whole window of this event",
            events: new Dictionary<string, string> { ["probe.yaml"] = string.Format(Event, "conditions: [{ indicator: housing, min: 0.1 }]\n", "") });

    [Fact]
    public void AnEventOptionThatSetsAPolicyOrMovesAnIndicatorOutsideItsWindowIsRefused()
    {
        var model = Model(m => Window(m, "  - id: maabarot\n    default: 0.0", "from: 1948-07-01"));
        Refused(model, "not active for the whole window of this event",
            events: new Dictionary<string, string> { ["probe.yaml"] = string.Format(Event, "", "    set_policy: { maabarot: 1.0 }\n") });
        Refused(Model(m => Window(m, "  - id: housing\n    base: 0.3", "to: 1948-09-01")), "not active for the whole window of this event",
            events: new Dictionary<string, string> { ["probe.yaml"] = string.Format(Event, "", "    effects: [{ target: housing, delta: 0.1 }]\n") });
    }

    [Fact]
    public void ARecordWithAWindowNeedsItsOfficeForThatWholeWindow()
    {
        var offices = Offices(o => o + "  - id: trade_minister\n    title: Trade\n    from: 1948-09-01\n");
        string OnTrade(string m) => m.Replace("  - id: maabarot\n    default: 0.0\n    office: finance_minister", "  - id: maabarot\n    default: 0.0\n    office: trade_minister");
        Refused(Window(OnTrade(Model()), "  - id: maabarot\n    default: 0.0", "from: 1948-07-01"), "office 'trade_minister' does not exist for the whole window", offices);
        Assert.NotNull(Load(OnTrade(Model()), offices));   // with no window the existing rule stands: the office may be created later
    }
}
