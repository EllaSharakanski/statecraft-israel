using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>The always-on escalation ladder: 1 routine, 2 wave, 3 limited operation, 4 prolonged conflict, 5 full war.</summary>
    public static class SecuritySystem
    {
        public static TurnLength TurnLengthFor(int rung) =>
            rung >= 5 ? TurnLength.Week : rung == 4 ? TurnLength.Month : TurnLength.Quarter;

        static int Weeks(TurnLength len) => len == TurnLength.Week ? 1 : len == TurnLength.Month ? 4 : 13;

        public static void Init(Content c, GameState s, List<Cause> why) => SetRung(c, s, c.Rules.StartRung, why);

        public static void SetRung(Content c, GameState s, int rung, List<Cause> why)
        {
            rung = Math.Max(1, Math.Min(5, rung));
            if (rung == 5 && s.Rung != 5) s.CeasefireWeeksLeft = c.Rules.CeasefireWeeks;
            if (rung != 5) s.CeasefireWeeksLeft = 0;
            if (rung != s.Rung) why.Add(new Cause("security", "rung", rung - s.Rung, FormattableString.Invariant($"security level {s.Rung} -> {rung}")));
            s.Rung = rung;
            RaiseToFloor(c, s, why);
        }

        /// <summary>Returns null on success, or the reason the order was rejected.</summary>
        public static string? SetMobilization(Content c, GameState s, double level, List<Cause> why)
        {
            if (!(level >= 0 && level <= 1)) return "Mobilization must be between 0 and 1";
            s.Mobilization = level;
            RaiseToFloor(c, s, why);
            return null;
        }

        static void RaiseToFloor(Content c, GameState s, List<Cause> why)
        {
            var rung = Math.Max(1, Math.Min(5, s.Rung));   // a bad save cannot index past the table
            var floor = c.Rules.RungMinMobilization[rung - 1];
            if (s.Mobilization < floor)
            {
                why.Add(new Cause("security", "mobilization", floor - s.Mobilization, FormattableString.Invariant($"reserve call-up raised to {floor * 100:0}%, the minimum for security level {rung}")));
                s.Mobilization = floor;
            }
        }

        public static void Apply(Content c, GameState s, TurnLength len, List<Cause> why)
        {
            if (s.Rung != 5) return;
            s.CeasefireWeeksLeft = Math.Max(0, s.CeasefireWeeksLeft - Weeks(len));
            if (s.CeasefireWeeksLeft == 0)
            {
                why.Add(new Cause("security", "rung", -2, "UN ceasefire imposed: full war ends"));
                SetRung(c, s, 3, why);
            }
        }
    }
}
