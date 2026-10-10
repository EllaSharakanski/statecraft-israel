using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The decision events of the war period, 15 May 1948 to the Egyptian armistice (24 February 1949), sourced
/// from research_notes/M3 content 1950-51/facts-1948.md (playtest 1 E: about 40 weekly turns had no event).</summary>
public class WarEvents1948Tests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly DateTime Start = new DateTime(1948, 5, 15);
    static readonly DateTime EgyptArmistice = new DateTime(1949, 2, 24);

    /// <summary>Id, the historical option, the security level it sets (null: unchanged) and the responsible office.</summary>
    public static readonly (string Id, string Historical, int? Rung, string Office)[] Events =
    {
        ("idf_founding", "one_army", null, "defense_minister"),
        ("first_truce", "accept", null, "prime_minister"),
        ("altalena", "enforce", null, "defense_minister"),
        ("ten_days", "central_front", 5, "defense_minister"),
        ("second_truce", "comply", 4, "prime_minister"),
        ("jerusalem_military_government", "military_government", null, "prime_minister"),
        ("bernadotte_assassination", "outlaw", null, "prime_minister"),
        ("latrun_proposal", "accept_vote", null, "prime_minister"),
        ("operation_yoav", "launch", 5, "defense_minister"),
        ("operation_hiram", "launch", 5, "defense_minister"),
        ("elections_1949", "hold", null, "prime_minister"),
        ("operation_horev", "launch", 5, "defense_minister"),
        ("sinai_withdrawal", "withdraw", null, "prime_minister"),
    };

    public static IEnumerable<object[]> Ids() => Events.Select(e => new object[] { e.Id });

    static GameEvent Get(string id) => C.Events.Single(e => e.Id == id);

    static DateTime LastDay(GameEvent e) => e.To!.Value.AddDays(-1);

    [Theory]
    [MemberData(nameof(Ids))]
    public void EachEventLoadsWithSourcesAndOneHistoricalOption(string id)
    {
        var e = Get(id);
        var row = Events.Single(x => x.Id == id);
        Assert.Equal("fact-checked", e.Status);
        Assert.True(e.Sources.Count >= 1, $"{id} has no source");
        Assert.All(e.Sources, s => { Assert.StartsWith("https://", s.Url); Assert.Equal(new DateTime(2026, 10, 10), s.Checked); });
        Assert.Equal(row.Office, e.Office);
        Assert.True(e.To.HasValue, $"{id} needs a closed window");
        Assert.True(e.From >= Start && e.From < EgyptArmistice, $"{id} opens {e.From:yyyy-MM-dd}, outside the war period");
        Assert.True(e.Options.Count >= 2, $"{id} offers no choice");
        var historical = Assert.Single(e.Options, o => o.Historical);
        Assert.Equal(row.Historical, historical.Id);
        Assert.True(historical.Recommended);
        Assert.Equal(row.Historical, e.Options[0].Id);   // the smoke run and the event card press the first option
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void PlaceholdersResolveOnEveryDayOfTheWindow(string id)
    {
        var e = Get(id);
        for (var d = e.From; d < e.To!.Value; d = d.AddDays(1))
        {
            Offices.Fill(C, e.Title, d);
            Assert.DoesNotContain("{", Offices.Fill(C, e.Text, d));
            foreach (var o in e.Options) Offices.Fill(C, o.Text, d);
        }
        Assert.Equal("ben_gurion", Offices.Holder(C, e.Office, e.From)!.Id);      // PM and Defence Minister in 1948-49
        Assert.Equal("ben_gurion", Offices.Holder(C, e.Office, LastDay(e))!.Id);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheHistoricalOptionAppliesItsEffectsAndSecurityLevel(string id)
    {
        var e = Get(id);
        var row = Events.Single(x => x.Id == id);
        var option = e.Options.Single(o => o.Historical);
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        s.Date = e.From;
        SecuritySystem.SetRung(C, s, 3, new List<Cause>());   // a level no option sets, so any change is visible
        s.PendingEvents.Add(id);
        var before = C.Indicators.ToDictionary(i => i.Id, i => s.Indicators[i.Id]);
        Assert.Null(EventSystem.Choose(C, s, id, option.Id, new List<Cause>()));
        Assert.Equal(row.Rung ?? 3, s.Rung);
        Assert.Empty(option.SetPolicy);   // no policy fits these events (notes, schema reminders)
        foreach (var i in C.Indicators)
        {
            var expected = option.Effects.Where(f => f.Target == i.Id).Sum(f => f.Delta);
            Assert.Equal(before[i.Id] + expected, s.Indicators[i.Id], 9);
        }
    }

    /// <summary>Plays the historical run and records, for each event, the turn it fired on ([turn start, turn end]) and
    /// whether HistoricalAI's choice of the historical option was carried out (not refused, then delegated).</summary>
    static readonly Dictionary<string, (DateTime TurnStart, DateTime TurnEnd, bool Chosen, int RungAfter)> Run = PlayHistorical();

    static Dictionary<string, (DateTime, DateTime, bool, int)> PlayHistorical()
    {
        var fired = new Dictionary<string, (DateTime, DateTime)>();
        var result = new Dictionary<string, (DateTime, DateTime, bool, int)>();
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        foreach (var id in s.PendingEvents) fired[id] = (s.Date, s.Date);
        while (s.Date < new DateTime(1949, 3, 1) && s.GameOver == null)
        {
            var commands = HistoricalAI.Commands(C, s);
            var step = Simulation.Step(C, s, commands);
            foreach (var cmd in commands.OfType<ChooseEventOption>())
                if (fired.TryGetValue(cmd.EventId, out var when))
                    result[cmd.EventId] = (when.Item1, when.Item2, !step.Rejected.Any(r => r.Command == cmd), step.State.Rung);
            foreach (var id in step.State.FiredEvents.Except(s.FiredEvents)) fired[id] = (s.Date, step.State.Date);
            s = step.State;
        }
        return result;
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void ItFiresInItsWindowAndTheHistoricalChoiceIsMade(string id)
    {
        var e = Get(id);
        Assert.True(Run.TryGetValue(id, out var r), $"{id} never fired (or was never answered) in the historical run");
        // The turn [TurnStart, TurnEnd] overlaps the window [From, To): the sim's own firing rule, late fire included.
        Assert.True(e.From <= r.TurnEnd && r.TurnStart < e.To!.Value,
            $"{id} fired on the turn {r.TurnStart:yyyy-MM-dd} to {r.TurnEnd:yyyy-MM-dd}, outside its window {e.From:yyyy-MM-dd} to {e.To:yyyy-MM-dd}");
        Assert.True(r.Chosen, $"{id}: HistoricalAI's choice was refused (attention?) and the event was delegated");
    }

    [Fact]
    public void TheHistoricalRunMovesThroughTheTruces()
    {
        Assert.Equal(4, Run["second_truce"].RungAfter);   // the second truce: monthly turns
        Assert.Equal(5, Run["operation_yoav"].RungAfter); // Yoav ends it
        Assert.Equal(5, Run["sinai_withdrawal"].RungAfter); // full war until the armistice with Egypt
    }

    /// <summary>Two events whose historical options set different security levels must never be answered on the same
    /// turn: the order the player presses them in would decide the level.</summary>
    [Fact]
    public void NoTurnOfTheHistoricalRunAsksForTwoDifferentSecurityLevels()
    {
        foreach (var turn in Run.Where(kv => Events.Any(x => x.Id == kv.Key)).GroupBy(kv => kv.Value.TurnEnd))
        {
            var rungs = turn.Select(kv => Events.Single(x => x.Id == kv.Key).Rung).Where(x => x.HasValue).Distinct().ToList();
            Assert.True(rungs.Count <= 1, $"turn ending {turn.Key:yyyy-MM-dd} asks for levels {string.Join(", ", rungs)} ({string.Join(", ", turn.Select(kv => kv.Key))})");
        }
    }

    /// <summary>Playtest 1 E: no stretch of more than eight weeks between 15 May 1948 and the Egyptian armistice
    /// (24 February 1949) without an event window opening.</summary>
    [Fact]
    public void NoEightWeekStretchWithoutAnEventWindow()
    {
        var opens = C.Events.Select(e => e.From).Where(d => d >= Start && d <= EgyptArmistice)
            .Concat(new[] { Start, EgyptArmistice }).Distinct().OrderBy(d => d).ToList();
        Assert.Contains(EgyptArmistice, C.Events.Select(e => e.From));
        for (int i = 1; i < opens.Count; i++)
            Assert.True((opens[i] - opens[i - 1]).TotalDays <= 56, $"no event window opens between {opens[i - 1]:yyyy-MM-dd} and {opens[i]:yyyy-MM-dd}");
    }

    /// <summary>EventRules.SensitiveTags (internal to the sim): the loader refuses a money reward on these.</summary>
    static readonly HashSet<string> Sensitive = new HashSet<string> { "holocaust", "terror", "massacre", "war-casualties" };

    /// <summary>Events with fighting or terror carry the sensitive tags EventRules already enforces (no money reward).</summary>
    [Fact]
    public void CombatAndTerrorEventsAreTaggedSensitive()
    {
        foreach (var id in new[] { "altalena", "ten_days", "operation_yoav", "operation_hiram", "operation_horev" })
            Assert.Contains("war-casualties", Get(id).Tags);
        Assert.Contains("terror", Get("bernadotte_assassination").Tags);
        var tags = C.Events.SelectMany(e => e.Tags).Distinct().ToList();
        Assert.All(tags, t => Assert.True(t == "security" || Sensitive.Contains(t), $"tag '{t}' is neither 'security' nor a sensitive tag the loader enforces"));
        foreach (var e in Events.Select(x => Get(x.Id)).Where(e => e.Tags.Any(Sensitive.Contains)))
            Assert.All(e.Options.SelectMany(o => o.Effects), f => Assert.False((f.Target == "treasury" || f.Target == "fx_reserves") && f.Delta > 0));
    }
}
