using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The Appoint command: who may be appointed, what it costs, and how long it lasts.</summary>
public class CabinetTests
{
    const string ExtraPeople = @"
  - id: outsider
    name: Out Sider
    living: false
    party: beta
    competence: 9
    loyalty: 3
    offices: []
  - id: loyalist
    name: Loy Alist
    living: false
    party: alpha
    competence: 2
    loyalty: 9
    offices: []
  - id: loyal_two
    name: Loyal Two
    living: false
    party: alpha
    competence: 6
    loyalty: 9
    offices: []
  - id: late
    name: Late Comer
    living: false
    died: 1948-07-01
    party: alpha
    competence: 5
    loyalty: 5
    offices: []
  - id: gone
    name: Gone Early
    living: false
    died: 1948-05-01
    competence: 5
    loyalty: 5
    offices: []
";

    static Content Load(Dictionary<string, string>? events = null) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = Fixture.Files["people.yaml"] + ExtraPeople }, events));

    static readonly Content C = Load();
    static readonly IReadOnlyList<Command> None = new Command[0];

    [Fact]
    public void AppointingReplacesTheMinisterAndCostsCoalitionStability()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        var why = new List<Cause>();
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "loyalist", why));
        Assert.Equal("loyalist", Cabinet.Holder(C, s, "finance_minister")!.Id);
        Assert.Equal("def_two", Offices.Holder(C, "finance_minister", s.Date)!.Id);   // history itself is unchanged
        Assert.Equal(0.9, s.CoalitionStability, 9);                                    // Def Two had no party: a seat lost
        Assert.Contains(why, w => w.System == "cabinet" && w.Message == "Loy Alist appointed Minister of Finance, replacing Def Two");
        Assert.Contains(why, w => w.Target == "coalition_stability" && w.Message == "coalition stability -0.10 (Def Two lost the Minister of Finance)");
    }

    [Fact]
    public void AReshuffleWithinAPartyCostsLess()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        var why = new List<Cause>();
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "loyal_two", why));
        Assert.Equal(0.9 - 0.03, s.CoalitionStability, 9);
        Assert.Contains(why, w => w.Message == "coalition stability -0.03 (Alpha resents the reshuffle)");
    }

    [Theory]
    [InlineData("prime_minister", "loyalist", "You are the Prime Minister: that office is not yours to fill")]
    [InlineData("finance_minister", "outsider", "Out Sider's party (Beta) is not in the coalition")]
    [InlineData("finance_minister", "gone", "Gone Early died on 1948-05-01")]
    [InlineData("finance_minister", "def_two", "Def Two already holds the office of Minister of Finance")]
    [InlineData("finance_minister", "nobody", "Unknown person 'nobody'")]
    [InlineData("treasurer", "loyalist", "Unknown office 'treasurer'")]
    public void IllegalAppointmentsAreRejectedWithAReason(string office, string person, string reason)
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        var before = s.Hash();
        Assert.Equal(reason, Cabinet.Appoint(C, s, office, person, new List<Cause>()));
        Assert.Equal(before, s.Hash());
    }

    [Fact]
    public void ARejectedAppointCommandIsRefundedAndReported()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "outsider") });
        Assert.Equal("Out Sider's party (Beta) is not in the coalition", r.Rejected.Single().Reason);
        Assert.Equal("def_two", Cabinet.Holder(C, r.State, "finance_minister")!.Id);
    }

    [Fact]
    public void ThePartyRuleFollowsTheCoalitionOfTheDay()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1949, 3, 10);                                   // Government Two: Alpha and Beta
        Assert.Null(Cabinet.Check(C, s, "finance_minister", "outsider"));
    }

    [Fact]
    public void AppointmentsEndWhenANewGovernmentTakesOffice()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s = Simulation.Step(C, s, new Command[] { new Appoint("finance_minister", "loyalist") }).State;
        Assert.Equal("loyalist", Cabinet.Holder(C, s, "finance_minister")!.Id);
        StepResult r;
        do { r = Simulation.Step(C, s, None); s = r.State; } while (Offices.Government(C, s.Date)!.Id == "gov1");
        Assert.Equal("def_two", Cabinet.Holder(C, s, "finance_minister")!.Id);
        Assert.Empty(s.Appointments);
        Assert.Contains(r.Why, w => w.Message == "A new government took office with its own ministers; your appointments ended");
    }

    [Fact]
    public void AnAppointeeWhoDiesLeavesOfficeOnTheDateOfDeath()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "late", new List<Cause>()));
        Assert.Equal("late", Cabinet.Holder(C, s, "finance_minister", new DateTime(1948, 6, 30))!.Id);
        Assert.Equal("def_two", Cabinet.Holder(C, s, "finance_minister", new DateTime(1948, 7, 1))!.Id);
    }

    [Fact]
    public void TheAppointeesCompetenceDrivesTheMinistrysEffects()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Equal(0.7, EffectsSystem.Multiplier(C, s, "finance_minister"), 9);   // Def Two, competence 4
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        Assert.Equal(0.6, EffectsSystem.Multiplier(C, s, "finance_minister"), 9);   // Loy Alist, competence 2
    }

    [Fact]
    public void EventTextNamesTheAppointee()
    {
        var c = Load(new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event });
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Cabinet.Appoint(c, s, "finance_minister", "loyalist", new List<Cause>());
        var drought = c.Events.Single(e => e.Id == "drought");
        Assert.Equal("Loy Alist reports a drought.", EventSystem.Render(c, s, drought));
    }

    [Fact]
    public void TheLineupListsEveryExistingOfficeAndMarksAppointees()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        var lineup = Cabinet.Lineup(C, s);
        Assert.Equal(new[] { "prime_minister", "defense_minister", "finance_minister" }, lineup.Select(x => x.Office.Id));
        Assert.True(lineup.Single(x => x.Office.Id == "finance_minister").Appointed);
        Assert.False(lineup.Single(x => x.Office.Id == "defense_minister").Appointed);
    }

    [Fact]
    public void AppointmentsAreInTheHashAndStepNeverMutatesItsInput()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var plain = Simulation.Step(C, s0, None).State;
        var appointed = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "loyalist") }).State;
        Assert.Empty(s0.Appointments);
        Assert.NotEqual(plain.Hash(), appointed.Hash());
    }

    [Fact]
    public void RealContentRules()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = new GameState { Date = new DateTime(1951, 1, 1) };
        Assert.Equal("Golda Meir is not an IDF career officer", Cabinet.Check(c, s, "chief_of_staff", "golda_meir"));
        Assert.Equal("The office of Minister of War Victims was abolished on 1951-10-08",
            Cabinet.Check(c, new GameState { Date = new DateTime(1951, 11, 1) }, "war_victims_minister", "maimon"));
        Assert.Equal("Aharon Zisling's party (Mapam) is not in the coalition", Cabinet.Check(c, s, "agriculture_minister", "zisling"));
        Assert.Equal("The President is elected by the Knesset, not appointed", Cabinet.Check(c, s, "president", "sharett"));
        Assert.Null(Cabinet.Check(c, s, "agriculture_minister", "eshkol"));
        Assert.Equal("Chaim Weizmann is the President", Cabinet.Check(c, s, "finance_minister", "weizmann"));
        Assert.Equal("Yigael Yadin is the serving Chief of Staff", Cabinet.Check(c, s, "finance_minister", "yadin"));
        s.Date = new DateTime(1951, 6, 1);
        Assert.Equal("David Remez died on 1951-05-19", Cabinet.Check(c, s, "transportation_minister", "remez"));
    }
}
