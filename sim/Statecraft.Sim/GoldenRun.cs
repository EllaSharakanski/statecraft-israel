using System;

namespace Statecraft.Sim
{
    /// <summary>The reference playthroughs that pin determinism (docs/determinism.md): real content, seed 1948,
    /// Historical mode, HistoricalAI until the date reaches the given day. The constants live in the sim library so
    /// Unity players (Golden check) and the editor tests compare against the same values .NET asserts.</summary>
    public static class GoldenRun
    {
        public const ulong Seed = 1948;

        /// <summary>Final hash of Play(content, Until1950). Changing it is a deliberate rules or content change:
        /// update it, DeterminismTests and docs/determinism.md in the same commit and say why in the message.</summary>
        public const string Hash1950 = "ee5d0826cdca5aef578e48c6273bc1453fa7745bd5ba1adf6f4a946ade828590";

        public static readonly DateTime Until1950 = new DateTime(1950, 1, 1);

        /// <summary>Final hash of Play(content, Until1952): the M3 content range (to 31 December 1951).</summary>
        public const string Hash1952 = "91bb6fb5f1295cee033998093651f4f303d1a61881a25a9015a8291798a35d80";

        public static readonly DateTime Until1952 = new DateTime(1952, 1, 1);

        /// <summary>Steps with HistoricalAI until Date >= <paramref name="until"/>, the game ends, or 1000 turns pass.</summary>
        public static GameState Play(Content c, DateTime until)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, Seed, Mode.Historical);
            for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
                s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
            return s;
        }
    }
}
