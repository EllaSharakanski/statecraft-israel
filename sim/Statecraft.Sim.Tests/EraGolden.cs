using System;
using System.Linq;
using Statecraft.Sim;

/// <summary>Adds a golden for one era in a few lines (#292): <c>EraGolden.Check("hash_founding_1950", c, "founding", new DateTime(1950, 1, 1))</c>
/// plays a fresh start of that era (golden seed, Historical mode, the golden orders, as <see cref="GoldenRun.Play"/> does) and
/// checks its hash against the manifest line and its snapshot in golden/&lt;name&gt;.txt, with the readable diff on a mismatch.
/// Bless a new one with STATECRAFT_UPDATE_GOLDEN=1 (docs/determinism.md).
/// The run stays inside the era: it stops before the turn that would reach the era's <c>to</c>. That turn ends on the boundary
/// (spec 4.1), and its final state already holds the next era's attention and events, so the next era's rules would move
/// this era's golden.
/// A fresh start of an era later than the first needs that era's sourced start values, which NewGame does not read yet
/// (it reads the base rules' start), so such an era fails here with a message saying so, not with a quiet wrong run.</summary>
public static class EraGolden
{
    public static GameState Play(Content c, string eraId, DateTime until, ulong seed = GoldenRun.Seed)
    {
        var era = c.Eras.FirstOrDefault(e => e.Id == eraId) ?? throw new ArgumentException($"no era '{eraId}' (known: {string.Join(", ", c.Eras.Select(e => e.Id))})");
        if (era.From != c.Rules.Start)
            throw new NotSupportedException($"era '{eraId}' starts {era.From:yyyy-MM-dd}, but a new game starts {c.Rules.Start:yyyy-MM-dd}: a fresh start of a later era needs that era's start values, which are not read yet (chapters design section 6).");
        if (until <= era.From || until > era.To) throw new ArgumentException($"until {until:yyyy-MM-dd} is outside era '{eraId}' ({era.From:yyyy-MM-dd} to {era.To:yyyy-MM-dd})");
        // GoldenRun.Play's loop, except that a turn ending on or after the era's end is not taken.
        var s = Simulation.NewGame(c, seed, Mode.Historical);
        for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
        {
            var next = Simulation.Step(c, s, GoldenRun.Orders(c, s)).State;
            if (next.Date >= era.To) break;
            s = next;
        }
        return s;
    }

    public static void Check(string name, Content c, string eraId, DateTime until, ulong seed = GoldenRun.Seed)
    {
        var s = Play(c, eraId, until, seed);
        var expected = GoldenManifest.UpdateRequested ? GoldenManifest.Read().GetValueOrDefault(name, "") : GoldenManifest.Expected(name);
        GoldenSnapshot.Check(name, s, expected);
    }
}
