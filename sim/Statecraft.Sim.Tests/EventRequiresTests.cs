using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>`requires: { event, option }`: an event is eligible only after another event was resolved with that option
/// (fix round 1, I2: a follow-up card must not contradict the player's earlier choice).</summary>
public class EventRequiresTests
{
    const string First = @"
id: first
title: First
text: 'A choice.'
from: 1948-06-01
options:
  - id: go
    text: Go
    historical: true
  - id: stay
    text: Stay
";

    const string Follow = @"
id: follow
title: Follow-up
text: 'After going.'
from: 1948-06-01
requires: { event: first, option: go }
options:
  - id: ok
    text: Fine
";

    static Content Load(string follow) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["first.yaml"] = First, ["follow.yaml"] = follow }));

    static GameState State(Content c)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1), RngState = 3 };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    [Fact]
    public void TheRequirementIsReadFromYaml()
    {
        var e = Load(Follow).Events.Find(x => x.Id == "follow")!;
        Assert.Equal("first", e.Requires!.Event);
        Assert.Equal("go", e.Requires.Option);
        Assert.Null(Load(Follow).Events.Find(x => x.Id == "first")!.Requires);
    }

    [Fact]
    public void ItWaitsUntilTheRequiredOptionIsChosen()
    {
        var c = Load(Follow);
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Equal(new[] { "first" }, s.PendingEvents);           // the follow-up is not eligible yet
        Assert.Null(EventSystem.Choose(c, s, "first", "go", why));
        Assert.Equal("go", s.ChosenOptions["first"]);
        EventSystem.Trigger(c, s, why);
        Assert.Equal(new[] { "follow" }, s.PendingEvents);
    }

    [Fact]
    public void AnotherOptionKeepsTheFollowUpAway()
    {
        var c = Load(Follow);
        var s = State(c);
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "first", "stay", why));
        EventSystem.Trigger(c, s, why);
        Assert.Empty(s.PendingEvents);
        Assert.DoesNotContain("follow", s.FiredEvents);
    }

    [Fact]
    public void ADelegatedChoiceCountsToo()
    {
        var c = Load(Follow);
        var s = State(c);
        s.Mode = Mode.Historical;
        var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        EventSystem.Delegate(c, s, why);
        Assert.True(s.ChosenOptions.ContainsKey("first"));
    }

    [Theory]
    [InlineData("requires: { event: nope, option: go }", "events[follow].requires.event", "unknown event 'nope'")]
    [InlineData("requires: { event: first, option: nope }", "events[follow].requires.option", "event 'first' has no option 'nope'")]
    [InlineData("requires: { event: follow, option: ok }", "events[follow].requires.event", "an event cannot require itself")]
    [InlineData("requires: { event: '', option: go }", "events[follow].requires.event", "unknown event ''")]
    public void ABrokenRequirementIsALoadErrorNamingTheFile(string line, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Follow.Replace("requires: { event: first, option: go }", line)));
        Assert.Equal("events/follow.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void ChosenOptionsAreClonedAndHashed()
    {
        var a = new GameState { Date = new DateTime(1948, 6, 1) };
        var b = a.Clone();
        b.ChosenOptions["first"] = "go";
        Assert.Empty(a.ChosenOptions);
        Assert.NotEqual(a.Hash(), b.Hash());
        var c = a.Clone();
        c.ChosenOptions["first"] = "stay";
        Assert.NotEqual(b.Hash(), c.Hash());
    }

    static string Requiring(string id, string other) => $@"
id: {id}
title: {id}
text: 'Waits.'
from: 1948-06-01
requires: {{ event: {other}, option: ok }}
options:
  - id: ok
    text: Fine
";

    /// <summary>Re-review N4: a cycle of requirements could never fire; the loader names every file in it.</summary>
    [Fact]
    public void ACycleOfRequirementsIsALoadErrorNamingItsFiles()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["first.yaml"] = First, ["a.yaml"] = Requiring("a", "b"), ["b.yaml"] = Requiring("b", "c"), ["c.yaml"] = Requiring("c", "a"),
        })));
        Assert.Equal("events/a.yaml", ex.File);
        Assert.Equal("events[a].requires", ex.Field);
        Assert.Contains("requirements form a cycle: events/a.yaml -> events/b.yaml -> events/c.yaml -> events/a.yaml", ex.Message);
    }

    [Fact]
    public void AChainWithoutACycleLoads()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["first.yaml"] = First, ["follow.yaml"] = Follow, ["a.yaml"] = Requiring("a", "follow"), ["b.yaml"] = Requiring("b", "a"),
        }));
        Assert.Equal(4, c.Events.Count);
    }
}
