using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class EventTests
{
    const string Shortage = @"
id: shortage
title: Bread lines
text: '{finance_minister} reports shortages.'
office: finance_minister
from: 1948-06-01
conditions:
  - indicator: food_supply
    max: 0.45
options:
  - id: ration
    text: Ration food
    historical: true
    recommended: true
    effects:
      - target: food_supply
        delta: 0.2
  - id: import
    text: Buy abroad
    effects:
      - target: fx_reserves
        delta: -10
  - id: escalate
    text: Blame the border
    set_rung: 3
";

    static Content Load(Dictionary<string, string> events) => ContentLoader.Load(Fixture.Write(events: events));

    static GameState State(Content c, DateTime d)
    {
        var s = new GameState { Date = d, RngState = 3 };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        return s;
    }

    [Fact]
    public void TriggersWhenDateAndConditionsHoldOnlyOnce()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var why = new List<Cause>();
        var s = State(c, new DateTime(1948, 5, 20));
        EventSystem.Trigger(c, s, why); Assert.Empty(s.PendingEvents);           // too early
        s.Date = new DateTime(1948, 6, 1);
        EventSystem.Trigger(c, s, why); Assert.Equal(new[] { "shortage" }, s.PendingEvents);
        s.PendingEvents.Clear();
        EventSystem.Trigger(c, s, why); Assert.Empty(s.PendingEvents);           // fires once
    }

    [Fact]
    public void ConditionBlocksTrigger()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); s.Indicators["food_supply"] = 0.9;
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Empty(s.PendingEvents);
    }

    [Fact]
    public void ChooseAppliesEffectsAndClearsPending()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Null(EventSystem.Choose(c, s, "shortage", "import", why));
        Assert.Equal(40, s.FxReserves);
        Assert.Empty(s.PendingEvents);
        Assert.Equal("Event 'shortage' is not waiting for a decision", EventSystem.Choose(c, s, "shortage", "ration", why));
    }

    [Fact]
    public void UnknownOptionIsRejected()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
        EventSystem.Trigger(c, s, why);
        Assert.Equal("Event 'shortage' has no option 'nap'", EventSystem.Choose(c, s, "shortage", "nap", why));
    }

    [Fact]
    public void DelegationIsDeterministicAndClearsPending()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        GameState Run()
        {
            var s = State(c, new DateTime(1948, 6, 1)); var why = new List<Cause>();
            EventSystem.Trigger(c, s, why); EventSystem.Delegate(c, s, why);
            return s;
        }
        var a = Run(); var b = Run();
        Assert.Empty(a.PendingEvents);
        Assert.Equal(a.Hash(), b.Hash());
    }

    [Fact]
    public void RenderFillsOfficeHolder()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        Assert.Equal("Def Two reports shortages.", EventSystem.Render(c, c.Events[0], new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void SensitiveEventCannotRewardTreasury()
    {
        var bad = Shortage.Replace("title: Bread lines", "title: Bread lines\ntags: [massacre]")
                          .Replace("target: fx_reserves\n        delta: -10", "target: treasury\n        delta: 50");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.treasury", ex.Field);
    }

    [Fact]
    public void PlaceholderMustBeAnOffice()
    {
        var bad = Shortage.Replace("{finance_minister}", "{king}");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].text", ex.Field);
    }

    // def_two takes defense only on 1949-02-01: nobody holds it in [1949-01-01, 1949-02-01).
    static readonly string GapPeople = Fixture.Files["people.yaml"].Replace(
        "office: defense_minister\n        from: 1949-01-01", "office: defense_minister\n        from: 1949-02-01");

    static Content LoadWithGap(string ev) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = GapPeople },
                                         new Dictionary<string, string> { ["shortage.yaml"] = ev }));

    [Fact]
    public void PlaceholderOfficeWithoutHolderForPartOfWindowFailsToLoad()
    {
        var bad = Shortage.Replace("{finance_minister}", "{defense_minister}");
        var ex = Assert.Throws<ContentException>(() => LoadWithGap(bad));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].text", ex.Field);
        Assert.Contains("defense_minister", ex.Message);
        Assert.Contains("1949-01-01", ex.Message);
    }

    [Fact]
    public void PlaceholderUncoveredAtWindowStartNamesThatDate()
    {
        var bad = Shortage.Replace("text: Ration food", "text: '{defense_minister} rations food'")
                          .Replace("from: 1948-06-01", "from: 1949-01-15\nto: 1949-03-01");
        var ex = Assert.Throws<ContentException>(() => LoadWithGap(bad));
        Assert.Equal("events[shortage].options[ration].text", ex.Field);
        Assert.Contains("1949-01-15", ex.Message);
    }

    [Fact]
    public void PlaceholderFullyCoveredWindowLoads()
    {
        var ok = Shortage.Replace("{finance_minister}", "{defense_minister}")
                         .Replace("from: 1948-06-01", "from: 1948-06-01\nto: 1949-01-01");   // window ends where the gap starts
        Assert.Single(LoadWithGap(ok).Events);
    }

    [Fact]
    public void PlaceholderCoveredAcrossAHandoverLoads()
    {
        // Fixture: pm_one holds defense until 1949-01-01, def_two from then on (open-ended).
        var ok = Shortage.Replace("{finance_minister}", "{defense_minister}");
        Assert.Single(Load(new Dictionary<string, string> { ["shortage.yaml"] = ok }).Events);
    }

    [Theory]
    [InlineData("'{Finance} reports shortages.'", "text: Ration food", "events[shortage].text")]
    [InlineData("'{finance_minister} reports { shortages.'", "text: Ration food", "events[shortage].text")]
    [InlineData("'{finance_minister} reports shortages.'", "text: 'Ration } food'", "events[shortage].options[ration].text")]
    public void StrayBracesAreRejected(string text, string optionText, string field)
    {
        var bad = Shortage.Replace("'{finance_minister} reports shortages.'", text).Replace("text: Ration food", optionText);
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void StrayBraceInTitleIsRejected()
    {
        var bad = Shortage.Replace("title: Bread lines", "title: 'Bread {lines'");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].title", ex.Field);
    }

    [Fact]
    public void EffectTargetMustExist()
    {
        var bad = Shortage.Replace("target: food_supply", "target: bread");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events[shortage].options[ration].effects.bread", ex.Field);
    }

    [Fact]
    public void DuplicateEventIdIsAnError()
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["a.yaml"] = Shortage, ["b.yaml"] = Shortage }));
        Assert.Equal("events/b.yaml", ex.File);
        Assert.Equal("events[shortage].id", ex.Field);
    }

    static string Sensitive(string tag, string target, string delta) =>
        Shortage.Replace("title: Bread lines", "title: Bread lines\ntags: [" + tag + "]")
                .Replace("target: fx_reserves\n        delta: -10", "target: " + target + "\n        delta: " + delta);

    [Theory]
    [InlineData("Massacre")]
    [InlineData("War-Casualties")]
    [InlineData("HOLOCAUST")]
    [InlineData("Terror")]
    public void SensitiveTagMatchingIgnoresCase(string tag)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = Sensitive(tag, "treasury", "50") }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.treasury", ex.Field);
    }

    [Fact]
    public void SensitiveEventCannotRewardFxReserves()
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = Sensitive("terror", "fx_reserves", "50") }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[import].effects.fx_reserves", ex.Field);
    }

    [Theory]
    [InlineData("treasury", "0")]
    [InlineData("treasury", "-20")]
    [InlineData("fx_reserves", "0")]
    [InlineData("fx_reserves", "-20")]
    public void SensitiveEventMayCostOrNotChangeMoney(string target, string delta)
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Sensitive("massacre", target, delta) });
        Assert.Single(c.Events);
    }

    [Fact]
    public void DuplicateOptionIdIsAnError()
    {
        var bad = Shortage.Replace("id: import", "id: ration");
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["shortage.yaml"] = bad }));
        Assert.Equal("events/shortage.yaml", ex.File);
        Assert.Equal("events[shortage].options[ration].id", ex.Field);
    }

    static string DelegatedText(Content c, int competence, ulong seed)
    {
        c.People.First(p => p.Id == "def_two").Competence = competence;
        var s = State(c, new DateTime(1948, 6, 1)); s.RngState = seed; var why = new List<Cause>();
        EventSystem.Trigger(c, s, why); why.Clear();
        EventSystem.Delegate(c, s, why);
        return why.First(w => w.Message.StartsWith("delegated")).Message;
    }

    [Fact]
    public void MaximalCompetenceAlwaysPicksRecommendedOption()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        for (ulong seed = 1; seed <= 30; seed++)
            Assert.EndsWith("Ration food", DelegatedText(c, 10, seed));
    }

    [Fact]
    public void MinimalCompetenceDelegateCanPickOtherOptions()
    {
        var c = Load(new Dictionary<string, string> { ["shortage.yaml"] = Shortage });
        var picks = new HashSet<string>();
        for (ulong seed = 1; seed <= 60; seed++) picks.Add(DelegatedText(c, 0, seed));
        Assert.True(picks.Count > 1, "competence 0 should choose at random among options");
    }
}
