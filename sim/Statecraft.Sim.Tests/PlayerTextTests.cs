using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>Player-facing text (spec: player-facing text): Cause messages and refusal reasons use display names from
/// content and the one date format, never content ids or ISO dates (playtest 1, items 4 and 5).</summary>
public class PlayerTextTests
{
    static readonly Regex SnakeId = new Regex("[a-z]+_[a-z]+");
    static readonly Regex IsoDate = new Regex(@"\d{4}-\d{2}-\d{2}");

    static List<string> HistoricalCauseMessages()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var messages = new List<string>();
        for (int turn = 0; turn < 1000 && s.Date < GoldenRun.Until1952 && s.GameOver == null; turn++)
        {
            var r = Simulation.Step(c, s, HistoricalAI.Commands(c, s));
            messages.AddRange(r.Why.Select(w => w.Message));
            s = r.State;
        }
        return messages;
    }

    [Fact]
    public void NoCauseInTheHistoricalRunShowsAContentIdOrAnIsoDate()
    {
        var messages = HistoricalCauseMessages();
        Assert.NotEmpty(messages);
        var bad = messages.Where(m => SnakeId.IsMatch(m) || IsoDate.IsMatch(m)).Distinct().ToList();
        Assert.True(bad.Count == 0, "Cause text shows ids or ISO dates:\n" + string.Join("\n", bad.Take(40)));
    }

    [Fact]
    public void CausesNameRegionsEffectsAndEventsByTheirDisplayNames()
    {
        var messages = HistoricalCauseMessages();
        Assert.Contains(messages, m => m.EndsWith("immigrants arrived (the Americas and Oceania)", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.EndsWith("immigrants arrived (country of origin not recorded)", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.StartsWith("Morale ", StringComparison.Ordinal) && m.Contains("Homes lift morale"));
        Assert.Contains(messages, m => m.StartsWith("International standing +5.0% (Armistice with Egypt)", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.Contains("elected 25 January 1949"));
    }

    [Fact]
    public void DatesUseTheDayMonthYearFormat()
    {
        Assert.Equal("15 May 1948", PlayerText.Date(new DateTime(1948, 5, 15)));
        Assert.Equal("1 January 1950", PlayerText.Date(new DateTime(1950, 1, 1)));
        Assert.Equal("May 1948", PlayerText.Month(new DateTime(1948, 5, 15)));
    }

    [Fact]
    public void TheMapActionStartDateIsShownInTheDayMonthYearFormat()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var reason = MapActions.Check(c, s, MapActions.Housing, 0, 0);
        Assert.Equal("Transit camps and housing projects can be placed from 1 January 1950", reason);
    }

    /// <summary>Display names are required, non-empty text; an origin needs a named region (file and field named).</summary>
    [Theory]
    [InlineData("model.yaml", "    name: Morale\n", "", "indicators[morale].name")]
    [InlineData("model.yaml", "    name: Rationing\n", "    name: ' '\n", "policies[rationing].name")]
    [InlineData("model.yaml", "    name: Rations feed people\n", "", "effects[rationing_food].name")]
    [InlineData("model.yaml", "    name: Hunger\n", "", "situations[hunger].name")]
    [InlineData("aliyah.yaml", "  - { id: europe, name: Europe }\n", "  - { id: europe }\n", "regions[europe].name")]
    [InlineData("aliyah.yaml", "  - { id: europe, name: Europe }\n", "  - { id: europe, name: '' }\n", "regions[europe].name")]
    [InlineData("aliyah.yaml", "  - { id: asia, name: Asia }\n", "", "series[0].origins.asia")]
    [InlineData("aliyah.yaml", "  - { id: asia, name: Asia }\n", "  - { id: europe, name: Asia }\n", "regions[europe].id")]
    public void DisplayNamesAreRequired(string file, string from, string to, string field)
    {
        var ex = ValidationTests.Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    // ---- Fix round 1 (review I1, I2, M3) ----

    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Every content id a Cause could leak: indicators, policies, effects, situations, money, regions, offices
    /// and events.</summary>
    static IEnumerable<string> ContentIds(Content c) =>
        c.Indicators.Select(x => x.Id).Concat(c.Policies.Select(x => x.Id)).Concat(c.Effects.Select(x => x.Id))
            .Concat(c.Situations.Select(x => x.Id)).Concat(c.Money.Select(x => x.Id)).Concat(c.Regions.Select(x => x.Id))
            .Concat(c.Offices.Select(x => x.Id)).Concat(c.Events.Select(x => x.Id)).Concat(c.Rules.MapActions.Keys).Distinct();

    /// <summary>Where an id would appear in a Cause: a snake_case id anywhere as a whole word; a one-word id ("morale",
    /// "treasury", "asia") at the start, alone in parentheses or a list, before a signed number, "set to", "=",
    /// "began"/"ended", or after "from" (case-sensitive: the display names are capitalised or phrased).</summary>
    static Regex IdPattern(string id)
    {
        var e = Regex.Escape(id);
        return id.Contains("_")
            ? new Regex($@"\b{e}\b")
            : new Regex($@"(^|\(|, |/|from ){e}(\)|,|/| [+-]\d| set to| =| began| ended|$)");
    }

    static List<string> Leaks(Content c, IEnumerable<string> messages)
    {
        var patterns = ContentIds(c).Select(id => (id, re: IdPattern(id))).ToList();
        return messages.Distinct().SelectMany(m => patterns.Where(p => p.re.IsMatch(m)).Select(p => $"'{p.id}' in: {m}")).ToList();
    }

    /// <summary>The player's own commands from mid-1950 on (historical play up to then): policy votes (passed and lost),
    /// an appointment, a budget, both map actions, and every option of every event, each from a state where the event
    /// is pending. Returns every Cause.</summary>
    internal static List<Cause> ScriptedCauses(Content c)
    {
        var causes = new List<Cause>();
        foreach (var e in c.Events)
            foreach (var o in e.Options)
            {
                var g = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
                g.Date = e.From; g.PendingEvents.Clear(); g.PendingEvents.Add(e.Id);
                var r = Simulation.Step(c, g, new Command[] { new ChooseEventOption(e.Id, o.Id) });
                Assert.Empty(r.Rejected);
                causes.AddRange(r.Why);
            }
        var s = GoldenRun.Play(c, new DateTime(1950, 6, 15));
        bool appointed = false, budget = false, camp = false, housing = false;
        for (int turn = 0; turn < 60 && s.GameOver == null; turn++)
        {
            var cmds = new List<Command>(HistoricalAI.Commands(c, s));
            cmds.Add(new SetPolicy("rationing", turn % 2 == 0 ? 0.9 : 0.2));
            if (!appointed)
            {
                var pick = c.Offices.SelectMany(o => Cabinet.Candidates(c, s, o.Id).Where(x => x.reason == null).Select(x => (o.Id, x.person.Id))).FirstOrDefault();
                if (pick.Item1 != null) cmds.Add(new Appoint(pick.Item1, pick.Item2));
            }
            if (!budget)
            {
                var first = s.Budget.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
                cmds.Add(new AllocateBudget(new Dictionary<string, double>(s.Budget.ToDictionary(k => k.Key, k => k.Value)) { [first] = s.Budget[first] }));
            }
            foreach (var (action, done) in new[] { (MapActions.TransitCamp, camp), (MapActions.Housing, housing) })
            {
                if (done) continue;
                var hex = c.Hexes.FirstOrDefault(h => MapActions.Check(c, s, action, h.Q, h.R) == null);
                if (hex != null) cmds.Add(new MapAction(action, hex.Q, hex.R));
            }
            var r = Simulation.Step(c, s, cmds);
            causes.AddRange(r.Why);
            appointed |= r.Why.Any(w => w.System == "cabinet");
            budget |= cmds.OfType<AllocateBudget>().Any() && !r.Rejected.Any(x => x.Command is AllocateBudget);
            camp |= r.Why.Any(w => w.System == "map" && w.Target == MapActions.TransitCamp);
            housing |= r.Why.Any(w => w.System == "map" && w.Target == MapActions.Housing);
            s = r.State;
        }
        Assert.True(appointed && budget && camp && housing, $"the script ran every command (appointed {appointed}, budget {budget}, camp {camp}, housing {housing})");
        Assert.Contains(causes, w => w.System == "policy");
        Assert.Contains(causes, w => w.System == "politics" && w.Message.Contains(" voted down "));
        return causes;
    }

    static List<Cause> AllCauses(Content c)
    {
        var all = new List<Cause>();
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        for (int turn = 0; turn < 1000 && s.Date < GoldenRun.Until1952 && s.GameOver == null; turn++)
        {
            var r = Simulation.Step(c, s, HistoricalAI.Commands(c, s));
            all.AddRange(r.Why);
            s = r.State;
        }
        all.AddRange(ScriptedCauses(c));
        return all;
    }

    [Fact]
    public void NoCauseFromAnyCommandShowsAContentId()
    {
        var leaks = Leaks(Real, AllCauses(Real).Select(w => w.Message));
        Assert.True(leaks.Count == 0, "Cause text shows content ids:\n" + string.Join("\n", leaks.Take(60)));
    }

    /// <summary>The guard itself: a one-word id in an id position is caught (the old snake_case regex missed these).</summary>
    [Theory]
    [InlineData("rationing set to 0.50", "rationing")]
    [InlineData("treasury +1.23 (tax 2.00)", "treasury")]
    [InlineData("14 immigrants arrived from asia", "asia")]
    [InlineData("Morale +4.1% (homes_lift_morale)", "homes_lift_morale")]
    [InlineData("Food supply +5.0% (austerity_1949)", "austerity_1949")]
    [InlineData("transit_camp at (3, 4): cost IL 1.5m", "transit_camp")]
    public void TheGuardCatchesOneWordIds(string message, string id) =>
        Assert.Contains(Leaks(Real, new[] { message }), l => l.StartsWith($"'{id}'", StringComparison.Ordinal));

    [Theory]
    [InlineData("Impose rationing")]
    [InlineData("A housing project at (3, 4): cost IL 1.5m")]
    [InlineData("reserve call-up raised to 80%, the minimum for security level 5")]
    public void TheGuardLeavesPlainEnglishAlone(string message) => Assert.Empty(Leaks(Real, new[] { message }));

    /// <summary>Review I2: money Causes carry their currency (IL or $, in millions); indicator Causes use the top bar's
    /// 0-100 scale with "%".</summary>
    [Fact]
    public void EveryCauseNumberCarriesItsUnit()
    {
        var money = Real.Money.Select(m => m.Id).ToHashSet();
        var indicators = Real.Indicators.Select(i => i.Id).ToHashSet();
        var bad = new List<string>();
        foreach (var w in AllCauses(Real))
        {
            if (money.Contains(w.Target) && !Regex.IsMatch(w.Message, @"[+-]?(IL |\$)\d+\.\dm"))
                bad.Add("money without a unit: " + w.Message);
            if (indicators.Contains(w.Target) && w.System != "situations" && !Regex.IsMatch(w.Message, @"[+-]\d+\.\d%"))
                bad.Add("indicator change not on the 0-100 % scale: " + w.Message);
            if (w.Message.Contains("stability") && !w.Message.Contains("%"))
                bad.Add("stability without %: " + w.Message);
        }
        Assert.True(bad.Count == 0, string.Join("\n", bad.Distinct().Take(40)));
    }

    [Fact]
    public void PlayerTextFormatsMoneyAndTheIndicatorScale()
    {
        Assert.Equal("IL 18.6m", PlayerText.Money(Real, "treasury", 18.6));
        Assert.Equal("$41.5m", PlayerText.Money(Real, "fx_reserves", 41.5));
        Assert.Equal("+IL 1.2m", PlayerText.MoneyChange(Real, "treasury", 1.23));
        Assert.Equal("-$0.4m", PlayerText.MoneyChange(Real, "fx_reserves", -0.4));
        Assert.Equal("+$0.0m", PlayerText.MoneyChange(Real, "fx_reserves", -0.004));
        Assert.Equal(30.0, PlayerText.Scale(0.3), 9);
        Assert.Equal("55%", PlayerText.Percent(0.55));
        Assert.Equal("+4.1%", PlayerText.PercentChange(0.041));
        Assert.Equal("Treasury -IL 2.0m", PlayerText.Change(Real, "treasury", -2));
        Assert.Equal("Morale +5.0%", PlayerText.Change(Real, "morale", 0.05));
        Assert.Equal("Foreign currency", PlayerText.Target(Real, "fx_reserves"));
    }

    /// <summary>Review M3: no silent fallback to the id.</summary>
    [Fact]
    public void AnUnknownIdIsAnError()
    {
        Assert.Throws<InvalidOperationException>(() => PlayerText.Target(Real, "no_such_thing"));
        Assert.Throws<InvalidOperationException>(() => PlayerText.Region(Real, "atlantis"));
        Assert.Throws<InvalidOperationException>(() => PlayerText.Money(Real, "morale", 1));
    }

    /// <summary>Review M3: the money names and currency prefixes come from content and are validated at load.</summary>
    [Theory]
    [InlineData("  - { id: treasury, name: Treasury, prefix: 'IL ' }\n", "  - { id: treasury, prefix: 'IL ' }\n", "money[treasury].name")]
    [InlineData("  - { id: fx_reserves, name: Foreign currency, prefix: '$' }\n", "  - { id: fx_reserves, name: Foreign currency }\n", "money[fx_reserves].prefix")]
    [InlineData("  - { id: debt, name: Debt, prefix: 'IL ' }\n", "", "money")]
    [InlineData("  - { id: debt, name: Debt, prefix: 'IL ' }\n", "  - { id: debt, name: Debt, prefix: 'IL ' }\n  - { id: gold, name: Gold, prefix: 'g' }\n", "money[gold].id")]
    public void MoneyNamesAreRequired(string from, string to, string field)
    {
        var ex = ValidationTests.Bad("model.yaml", from, to);
        Assert.Equal("model.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    // ---- Fix round 2 (R2-2): map-action phrases come from content ----

    [Theory]
    [InlineData("transit_camp: { cost: 2, housing: 0.05, morale: -0.02, name: a transit camp }", "transit_camp: { cost: 2, housing: 0.05, morale: -0.02 }", "map_actions.transit_camp.name")]
    [InlineData("housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project }", "housing: { cost: 8, housing: 0.08, morale: 0.01, name: '  ' }", "map_actions.housing.name")]
    public void MapActionNamesAreRequired(string from, string to, string field)
    {
        var ex = ValidationTests.Bad("rules.yaml", from, to);
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void MapActionCausesAndReasonsUseTheContentPhrase()
    {
        var rules = Fixture.Files["rules.yaml"].Replace("name: a transit camp", "name: a ma'abara");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = rules }));
        Assert.Equal("a ma'abara", PlayerText.MapAction(c, MapActions.TransitCamp));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Policies[MapActions.MaabarotPolicy] = 1;
        s.Date = c.Rules.MapActionsFrom;
        var hex = c.Hexes.First(h => MapActions.Check(c, s, MapActions.TransitCamp, h.Q, h.R) == null);
        var why = new List<Cause>();
        Assert.Null(MapActions.Apply(c, s, MapActions.TransitCamp, hex.Q, hex.R, why));
        Assert.Contains(why, w => w.System == "map" && w.Message.StartsWith($"A ma'abara at ({hex.Q}, {hex.R})", StringComparison.Ordinal));
        Assert.Equal($"({hex.Q}, {hex.R}) already has a ma'abara", MapActions.Check(c, s, MapActions.TransitCamp, hex.Q, hex.R));
        Assert.Throws<InvalidOperationException>(() => PlayerText.MapAction(c, "factory"));
    }
}
