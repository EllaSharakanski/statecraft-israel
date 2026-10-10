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

    /// <summary>Most consecutive turns of the historical run, before the Egyptian armistice card, that raise no card
    /// (controller ruling, fix round 1, I7).</summary>
    const int MaxEmptyTurns = 4;

    static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

    /// <summary>Id; the historical option; the security level it sets (null: unchanged); the responsible office and its
    /// holder; how many options; and DecidedBy, the date of the historical decision (facts-1948.md), which the card must
    /// reach the player by: the player decides during the turn that starts on the card's date.</summary>
    public static readonly (string Id, string Historical, int? Rung, string Office, string Holder, int Options, DateTime DecidedBy)[] Events =
    {
        ("idf_founding", "one_army", null, "defense_minister", "ben_gurion", 2, D(1948, 5, 26)),         // order signed (§3a)
        ("first_truce", "accept", null, "prime_minister", "ben_gurion", 2, D(1948, 6, 10)),              // acceptance sent (§1)
        ("altalena", "enforce", null, "defense_minister", "ben_gurion", 2, D(1948, 6, 20)),              // force authorised (§2)
        ("ten_days", "central_front", 5, "defense_minister", "ben_gurion", 2, D(1948, 7, 9)),            // Dani begins (§4)
        ("second_truce", "comply", 4, "prime_minister", "ben_gurion", 2, D(1948, 7, 18)),                // truce in force (§5)
        ("jerusalem_military_government", "military_government", null, "prime_minister", "ben_gurion", 2, D(1948, 8, 2)),
        ("bernadotte_assassination", "outlaw", null, "prime_minister", "ben_gurion", 2, D(1948, 9, 20)),  // ultimatum (§6)
        ("latrun_proposal", "accept_vote", null, "prime_minister", "ben_gurion", 2, D(1948, 9, 26)),     // cabinet vote (§13a)
        ("operation_yoav", "stop_at_ceasefire", 5, "defense_minister", "ben_gurion", 2, D(1948, 10, 22)), // Hebron refused, cease-fire (§7)
        ("operation_hiram", "launch", 5, "defense_minister", "ben_gurion", 3, D(1948, 10, 28)),          // launched night of 28-29 Oct (§8)
        ("jerusalem_ceasefire", "sign", null, "prime_minister", "ben_gurion", 2, D(1948, 11, 30)),       // signed (§13b)
        ("operation_horev", "launch", 5, "defense_minister", "ben_gurion", 2, D(1948, 12, 22)),          // launched (§9)
        ("sinai_withdrawal", "withdraw", null, "prime_minister", "ben_gurion", 2, D(1949, 1, 1)),        // orders given (§9, FRUS d341)
        ("rhodes_talks", "send_delegation", null, "foreign_minister", "sharett", 2, D(1949, 1, 13)),     // talks open (§9)
        ("elections_1949", "hold", null, "prime_minister", "ben_gurion", 1, D(1949, 1, 25)),             // the vote (§11)
    };

    /// <summary>Cards allowed to reach the player after DecidedBy, and why.</summary>
    static readonly Dictionary<string, string> LateByDesign = new Dictionary<string, string>
    {
        // The facts that make the ultimatum (Sinai crossing 28 Dec, UN call 29 Dec, US message 30 Dec) come after the
        // Horev card's turn, so the card can only come one turn later, a day after the orders (review M6).
        ["sinai_withdrawal"] = "facts of 28-30 Dec cannot be stated on the 26 Dec turn",
    };

    /// <summary>Follow-up events and the earlier choice they depend on (review I2).</summary>
    public static readonly (string Id, string Event, string Option)[] Chains =
    {
        ("ten_days", "first_truce", "accept"),
        ("latrun_proposal", "second_truce", "comply"),
        ("jerusalem_ceasefire", "jerusalem_military_government", "military_government"),
        ("sinai_withdrawal", "operation_horev", "launch"),
        ("rhodes_talks", "operation_horev", "launch"),
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
        Assert.Contains(e.Status, new[] { "fact-checked", ReleaseGate.ReviewedStatus });   // sensitive ones carry the owner's sign-off (D11)
        Assert.True(e.Sources.Count >= 1, $"{id} has no source");
        Assert.All(e.Sources, s => { Assert.StartsWith("https://", s.Url); Assert.Equal(new DateTime(2026, 10, 10), s.Checked); });
        Assert.Equal(row.Office, e.Office);
        Assert.True(e.To.HasValue, $"{id} needs a closed window");
        Assert.True(e.From >= Start && e.From < EgyptArmistice, $"{id} opens {e.From:yyyy-MM-dd}, outside the war period");
        Assert.Equal(row.Options, e.Options.Count);
        var historical = Assert.Single(e.Options, o => o.Historical);
        Assert.Equal(row.Historical, historical.Id);
        Assert.True(historical.Recommended);
        Assert.Equal(row.Historical, e.Options[0].Id);   // the smoke run and the event card press the first option
        var chain = Chains.Where(x => x.Id == id).ToList();
        if (chain.Count == 0) Assert.Null(e.Requires);
        else { Assert.Equal(chain[0].Event, e.Requires!.Event); Assert.Equal(chain[0].Option, e.Requires.Option); }
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void PlaceholdersResolveOnEveryDayOfTheWindow(string id)
    {
        var e = Get(id);
        var row = Events.Single(x => x.Id == id);
        for (var d = e.From; d < e.To!.Value; d = d.AddDays(1))
        {
            Assert.DoesNotContain("{", Offices.Fill(C, e.Title, d));
            Assert.DoesNotContain("{", Offices.Fill(C, e.Text, d));
            foreach (var o in e.Options) Assert.DoesNotContain("{", Offices.Fill(C, o.Text, d));
        }
        Assert.Equal(row.Holder, Offices.Holder(C, e.Office, e.From)!.Id);
        Assert.Equal(row.Holder, Offices.Holder(C, e.Office, LastDay(e))!.Id);
    }

    /// <summary>Spec section 5.3: office holders are named only through {office} placeholders (review I4).</summary>
    [Theory]
    [MemberData(nameof(Ids))]
    public void NoOfficeHolderIsNamedInTheText(string id)
    {
        var e = Get(id);
        var texts = new[] { e.Title, e.Text }.Concat(e.Options.Select(o => o.Text)).ToList();
        var holders = C.People.Where(p => p.Offices.Any(t => t.From < e.To!.Value && (t.To == null || t.To > e.From))).ToList();
        foreach (var p in holders)
        {
            var surname = p.Name.Split(' ').Last();
            Assert.All(texts, t => Assert.DoesNotContain(surname, t));
        }
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

    sealed class Turn
    {
        public DateTime Start, End;
        public List<string> Raised = new List<string>();
        public List<Rejection> Rejected = new List<Rejection>();
        public int RungAfter;
    }

    /// <summary>Plays to March 1949 with HistoricalAI, except that <paramref name="pick"/> may choose another option.</summary>
    static List<Turn> Play(Func<string, string?>? pick = null)
    {
        var turns = new List<Turn>();
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        while (s.Date < new DateTime(1949, 3, 1) && s.GameOver == null)
        {
            var commands = HistoricalAI.Commands(C, s)
                .Select(c => c is ChooseEventOption o && pick?.Invoke(o.EventId) is string other ? new ChooseEventOption(o.EventId, other) : c)
                .ToList();
            var step = Simulation.Step(C, s, commands);
            turns.Add(new Turn
            {
                Start = s.Date, End = step.State.Date, Rejected = step.Rejected.ToList(), RungAfter = step.State.Rung,
                Raised = step.State.FiredEvents.Except(s.FiredEvents).ToList(),
            });
            s = step.State;
        }
        return turns;
    }

    static readonly List<Turn> Run = Play();

    /// <summary>The date of the card for <paramref name="id"/>: the end of the turn that raised it.</summary>
    static DateTime CardDate(string id) =>
        Run.FirstOrDefault(t => t.Raised.Contains(id))?.End ?? throw new Xunit.Sdk.XunitException($"{id} never fired in the historical run");

    /// <summary>The card reaches the player in a turn that starts by the historical decision (review I1, I7).</summary>
    [Theory]
    [MemberData(nameof(Ids))]
    public void TheCardArrivesBeforeTheHistoricalDecision(string id)
    {
        var row = Events.Single(x => x.Id == id);
        var card = CardDate(id);
        var e = Get(id);
        Assert.True(card >= e.From && card < e.To!.Value.AddDays(31), $"{id}: card {card:yyyy-MM-dd} far from its window");
        if (LateByDesign.TryGetValue(id, out var why))
            Assert.True(card <= row.DecidedBy.AddDays(1), $"{id}: card {card:yyyy-MM-dd}, more than a day after {row.DecidedBy:yyyy-MM-dd} ({why})");
        else
            Assert.True(card <= row.DecidedBy, $"{id}: card {card:yyyy-MM-dd}, after the historical decision of {row.DecidedBy:yyyy-MM-dd}");
    }

    /// <summary>Every card is answered by HistoricalAI on the turn it appears, and nothing is refused for attention
    /// (fix round 1: attention at or under the cap on every turn).</summary>
    [Fact]
    public void NoTurnBeforeTheArmisticeRefusesAnOrder()
    {
        foreach (var t in Run)
            Assert.True(t.Rejected.Count == 0, $"turn {t.Start:yyyy-MM-dd}: refused {string.Join("; ", t.Rejected.Select(r => r.Reason))}");
    }

    [Fact]
    public void TheHistoricalRunMovesThroughTheTruces()
    {
        int After(string id) => Run.Single(t => t.Start == CardDate(id)).RungAfter;
        Assert.Equal(4, After("second_truce"));       // the second truce: monthly turns
        Assert.Equal(5, After("operation_yoav"));     // Yoav ends it
        Assert.Equal(5, After("sinai_withdrawal"));   // full war until the armistice with Egypt
    }

    /// <summary>Two events whose historical options set different security levels must never be answered on the same
    /// turn: the order the player presses them in would decide the level.</summary>
    [Fact]
    public void NoTurnOfTheHistoricalRunAsksForTwoDifferentSecurityLevels()
    {
        foreach (var t in Run)
        {
            var rungs = t.Raised.Where(id => Events.Any(x => x.Id == id)).Select(id => Events.Single(x => x.Id == id).Rung)
                .Where(x => x.HasValue).Distinct().ToList();
            Assert.True(rungs.Count <= 1, $"card {t.End:yyyy-MM-dd} asks for levels {string.Join(", ", rungs)}");
        }
    }

    /// <summary>Playtest 1 E: counted in turns of the historical run, from the start to the Egyptian armistice card, no
    /// more than MaxEmptyTurns consecutive turns raise no card (review I7, I8).</summary>
    [Fact]
    public void NoLongStretchOfTurnsWithoutACard()
    {
        var armistice = Run.FindIndex(t => t.Raised.Contains("armistice_egypt"));
        Assert.True(armistice > 0, "the Egyptian armistice never fired");
        int empty = 0;
        var stretches = new List<string>();
        for (int i = 0; i <= armistice; i++)
        {
            if (Run[i].Raised.Count == 0) { empty++; continue; }
            if (empty > MaxEmptyTurns) stretches.Add($"{empty} turns ending {Run[i - 1].End:yyyy-MM-dd}");
            empty = 0;
        }
        Assert.True(stretches.Count == 0, "empty stretches: " + string.Join("; ", stretches));
    }

    /// <summary>A follow-up card never fires when the player chose otherwise earlier (review I2).</summary>
    [Fact]
    public void AFollowUpNeedsTheChoiceItDescribes()
    {
        foreach (var (id, required, option) in Chains)
        {
            var other = Get(required).Options.First(o => o.Id != option).Id;
            var run = Play(e => e == required ? other : null);
            Assert.True(run.Any(t => t.Raised.Contains(required)), $"{required} never fired");
            Assert.False(run.Any(t => t.Raised.Contains(id)), $"{id} fired although {required} was answered '{other}'");
        }
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
    }

    /// <summary>Notes section 6: the killing costs standing whatever the answer (review I5).</summary>
    [Fact]
    public void BothAnswersToTheAssassinationShareTheStandingHit()
    {
        var e = Get("bernadotte_assassination");
        Assert.All(e.Options, o => Assert.Contains(o.Effects, f => f.Target == "intl_standing" && f.Delta == -0.05));
        Assert.All(e.Options, o => Assert.True(o.Effects.Where(f => f.Target == "intl_standing").Sum(f => f.Delta) < 0, $"{o.Id} raises standing"));
    }
}
