using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Saves: seed + command log + content hash + schema version; loading replays to the same state hash.</summary>
public class SaveGameTests
{
    static Content Fixture1() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event }));

    /// <summary>Ten turns using every command kind, including ones the sim rejects.</summary>
    static GameSession Played(Content c)
    {
        var g = GameSession.New(c, 99, Mode.Historical);
        var orders = new List<IReadOnlyList<Command>>
        {
            new Command[] { new SetMobilization(0.9), new SecurityOrder(-2) },
            new Command[] { new ChooseEventOption("drought", "ration"), new SetPolicy("rationing", 1.0 / 3) },
            new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 1.0 }) },
            new Command[] { new Appoint("finance_minister", "pm_one"), new MapAction("housing", 2, 0) },
            new Command[] { new SetPolicy("odd id with spaces%", 0.5), new MapAction("transit_camp", 4, 0) },
            new Command[0], new Command[0], new Command[] { new SecurityOrder(1) }, new Command[0], new Command[0],
        };
        foreach (var o in orders) g.Step(o);
        return g;
    }

    [Fact]
    public void ASaveReplaysToTheSameStateHash()
    {
        var c = Fixture1();
        var played = Played(c);
        var text = played.Save.Write();
        var loaded = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(played.State.Hash(), loaded.State.Hash());
        Assert.Equal(10, loaded.Save.Turns.Count);
        Assert.Equal(text, loaded.Save.Write());                         // writing the loaded save gives the same text
        Assert.Equal(played.Previous!.Hash(), loaded.Previous!.Hash());  // "since last turn" survives a load
    }

    [Fact]
    public void TheSaveRecordsTheHeaderAndExactNumbers()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var lines = text.Split('\n');
        Assert.Equal("statecraft-save 1", lines[0]);
        Assert.Equal("content " + c.Hash, lines[1]);
        Assert.Equal("seed 99", lines[2]);
        Assert.Equal("mode Historical", lines[3]);
        Assert.Contains("policy rationing 3FD5555555555555", lines);       // 1/3, bit for bit
        Assert.Contains("policy odd%20id%20with%20spaces%25 3FE0000000000000", lines);
        Assert.Contains("budget finance_minister=3FF0000000000000", lines);
        Assert.Contains("map housing 2 0", lines);
        Assert.Contains("security -2", lines);
        Assert.StartsWith("end ", lines[lines.Length - 2]);
    }

    [Fact]
    public void TheHistoricalPlaythroughTo1952RoundTrips()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < GoldenRun.Until1952) g.Step(HistoricalAI.Commands(c, g.State));
        Assert.Equal(GoldenRun.Hash1952, g.State.Hash());
        var loaded = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(GoldenRun.Hash1952, loaded.State.Hash());
    }

    [Fact]
    public void OtherContentLoadsWithAWarning()
    {
        var text = Played(Fixture1()).Save.Write();
        var other = ContentLoader.Load(Fixture.Write(
            new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("treasury_start: 100", "treasury_start: 90") },
            new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event }));
        GameSession.Load(other, text, out var warnings);
        Assert.Equal(new[] { "This save was made with different game content; the replayed game may differ.", "The replayed game differs from the saved one." }, warnings);
    }

    [Fact]
    public void ADivergingReplayWithTheSameContentIsAnError()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var lines = text.Split('\n');
        lines[2] = "seed 100";                                                // same content, different game
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Contains("did not reproduce the saved game", ex.Message);
    }

    [Theory]
    [InlineData("statecraft-save 2", 1, "line 1: save schema version 2 is newer than this game (1)")]
    [InlineData("statecraft-save x", 1, "line 1: bad schema version")]
    [InlineData("chess-save 1", 1, "line 1: expected 'statecraft-save'")]
    public void BadHeadersAreRejected(string first, int line, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n');
        lines[0] = first;
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal((line, message), (ex.Line, ex.Message));
    }

    [Theory]
    [InlineData("fly me 1 2", "unknown command 'fly'")]
    [InlineData("policy rationing 0.5", "bad number '0.5'")]
    [InlineData("map housing two 0", "bad integer 'two'")]
    [InlineData("event drought", "'event' needs 2 values")]
    public void BadCommandLinesNameTheLine(string command, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n').ToList();
        lines.Insert(5, command);                                             // after the first "turn"
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal(6, ex.Line);
        Assert.Equal("line 6: " + message, ex.Message);
    }

    [Fact]
    public void TruncatedTextIsRejected()
    {
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse("statecraft-save 1\ncontent x\n"));
        Assert.Equal("Not a Statecraft save (too short)", ex.Message);
    }

    [Fact]
    public void TheContentHashCoversEveryFileAndIgnoresLineEndings()
    {
        var a = ContentLoader.Load(Fixture.Write());
        var b = ContentLoader.Load(Fixture.Write());
        Assert.Equal(a.Hash, b.Hash);
        Assert.Equal(64, a.Hash.Length);
        var crlf = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["parties.yaml"] = Fixture.Files["parties.yaml"].Replace("\n", "\r\n") }));
        Assert.Equal(a.Hash, crlf.Hash);
        var map = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = Fixture.Files["map/settlement_hexes.yaml"] + "# note\n" }));
        Assert.NotEqual(a.Hash, map.Hash);
    }

    /// <summary>Every command type the sim has must be savable: the played session uses each one, and a new Command type
    /// that the save format cannot write fails here.</summary>
    [Fact]
    public void EveryCommandTypeIsInTheSavedSession()
    {
        var all = typeof(Command).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract).Select(t => t.Name).OrderBy(x => x).ToList();
        var used = Played(Fixture1()).Save.Turns.SelectMany(x => x).Select(x => x.GetType().Name).Distinct().OrderBy(x => x).ToList();
        Assert.Equal(all, used);
    }
}
