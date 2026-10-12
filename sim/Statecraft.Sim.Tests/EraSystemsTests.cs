using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>eras.yaml `systems` (#58): the legislature and map_actions switches, each tested both ways.</summary>
public class EraSystemsTests
{
    static string Eras(string systems) => $@"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1957-01-01
{systems}";

    static Content Load(string systems, bool noMajority = false)
    {
        var o = new Dictionary<string, string> { ["eras.yaml"] = Eras(systems) };
        // Government One has no majority in the council (beta holds 2 of 5), so every vote fails.
        if (noMajority) o["governments.yaml"] = Fixture.Files["governments.yaml"].Replace("coalition: [alpha]", "coalition: [beta]");
        return ContentLoader.Load(Fixture.Write(o));
    }

    [Fact]
    public void RealContentUsesTheDefaultSwitches()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal("state", c.SystemsAt(c.Rules.Start).Legislature);
        Assert.Equal(new[] { "transit_camp", "housing" }, c.SystemsAt(c.Rules.Start).MapActions);
        Assert.Equal("state", c.SystemsAt(new DateTime(1960, 1, 1)).Legislature);   // outside every era: defaults
    }

    [Theory]
    [InlineData("    systems: { legislature: parliament }", "eras[founding].systems.legislature", "must be state or assembly")]
    [InlineData("    systems: { map_actions: [bulldozer] }", "eras[founding].systems.map_actions", "unknown map action 'bulldozer'")]
    [InlineData("    systems: { map_actions: [housing, housing] }", "eras[founding].systems.map_actions", "lists an action twice")]
    public void BadSwitchesFailNamingTheField(string systems, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(systems));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void AnUnknownSwitchKeyFails() =>
        Assert.Throws<ContentException>(() => Load("    systems: { war_mode: true }"));

    [Fact]
    public void AStateLegislatureVotesDownAPolicy()
    {
        var c = Load("    systems: { legislature: state }", noMajority: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("rationing", 1.0) });
        Assert.Equal(0.0, r.State.Policies["rationing"]);
        Assert.Contains(r.Why, w => w.Message.Contains("voted down"));
    }

    [Fact]
    public void AnAssemblyVotesAgainstAPolicyButItStillTakesEffect()
    {
        var c = Load("    systems: { legislature: assembly }", noMajority: true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Simulation.Step(c, s, new Command[] { new SetPolicy("rationing", 1.0) });
        Assert.Equal(1.0, r.State.Policies["rationing"]);
        Assert.Contains(r.Why, w => w.Message.Contains("its vote does not bind"));
        Assert.DoesNotContain(r.Why, w => w.Message.Contains("voted down"));
    }

    [Fact]
    public void AnEmptyMapActionsListAllowsNone()
    {
        var c = Load("    systems: { map_actions: [] }");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal("That map action is not available in this chapter", MapActions.Check(c, s, MapActions.Housing, 0, 0));
    }

    [Fact]
    public void MapActionsAreLimitedToTheErasList()
    {
        var c = Load("    systems: { map_actions: [housing] }");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal("That map action is not available in this chapter", MapActions.Check(c, s, MapActions.TransitCamp, 0, 0));
        Assert.NotEqual("That map action is not available in this chapter", MapActions.Check(c, s, MapActions.Housing, 0, 0));
        var all = Load("    systems: { map_actions: [transit_camp, housing] }");
        var s2 = Simulation.NewGame(all, 1, Mode.Historical);
        Assert.NotEqual("That map action is not available in this chapter", MapActions.Check(all, s2, MapActions.TransitCamp, 0, 0));
    }
}
