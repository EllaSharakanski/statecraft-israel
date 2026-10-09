using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The public entry point the Unity layer calls: start a game, then step it one turn at a time.</summary>
    public static class Simulation
    {
        public static GameState NewGame(Content c, ulong seed, Mode mode)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (mode == Mode.Open) throw new NotSupportedException("Open mode arrives with milestone M4.");
            var s = new GameState { Date = c.Rules.Start, RngState = seed, Mode = mode };
            var why = new List<Cause>();
            EffectsSystem.Init(c, s);
            EconomySystem.Init(c, s);
            PopulationSystem.Init(c, s);
            SecuritySystem.Init(c, s, why);
            AttentionSystem.Refill(c, s);
            EventSystem.Trigger(c, s, why);
            return s;
        }

        /// <summary>Applies commands in order (each pays attention first; a rejected command is refunded and carries a
        /// reason), delegates unanswered events, then advances one turn. Never mutates <paramref name="state"/>.</summary>
        public static StepResult Step(Content c, GameState state, IReadOnlyList<Command> commands)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (commands.Any(x => x == null)) throw new ArgumentException("Commands cannot contain null.", nameof(commands));

            var s = state.Clone();
            var why = new List<Cause>();
            var rejected = new List<Rejection>();
            if (s.GameOver != null)
            {
                foreach (var cmd in commands) rejected.Add(new Rejection(cmd, $"The game is over: {s.GameOver}"));
                return new StepResult(s, why, rejected);
            }

            foreach (var cmd in commands)
            {
                if (!AttentionSystem.IsCommandKind(cmd.Kind)) { rejected.Add(new Rejection(cmd, $"Unknown command '{cmd.Kind}'")); continue; }
                var before = s.Attention;
                var error = AttentionSystem.TrySpend(c, s, cmd.Kind) ?? Execute(c, s, cmd, why);
                if (error != null) { s.Attention = before; rejected.Add(new Rejection(cmd, error)); }
            }
            EventSystem.Delegate(c, s, why);

            var len = SecuritySystem.TurnLengthFor(s.Rung);
            var next = SimTime.Advance(s.Date, len);
            var yf = SimTime.YearFraction(len);
            EffectsSystem.Apply(c, s, why);
            PopulationSystem.Apply(c, s, next, yf, why);
            EconomySystem.Apply(c, s, yf, why);
            PoliticsSystem.UpdateStability(c, s, yf, why);
            SecuritySystem.Apply(c, s, len, why);
            PoliticsSystem.ReportChanges(c, s.Date, next, why);
            s.Date = next;
            s.Turn++;
            AttentionSystem.Refill(c, s);
            EventSystem.Trigger(c, s, why);
            return new StepResult(s, why, rejected);
        }

        /// <summary>Returns null on success, or the reason the command was rejected. A rejected command leaves the
        /// state as it found it (attention is refunded by the caller).</summary>
        static string? Execute(Content c, GameState s, Command cmd, List<Cause> why)
        {
            switch (cmd)
            {
                case SetPolicy p:
                    if (c.Policies.All(x => x.Id != p.Policy)) return $"Unknown policy '{p.Policy}'";
                    if (!(p.Level >= 0 && p.Level <= 1)) return "Policy level must be between 0 and 1";
                    if (!PassVote(c, s)) return $"{Offices.Legislature(c, s.Date)?.Body ?? "The legislature"} voted it down";
                    s.Policies[p.Policy] = p.Level;
                    why.Add(new Cause("policy", p.Policy, p.Level, FormattableString.Invariant($"{p.Policy} set to {p.Level:0.00}")));
                    return null;
                case AllocateBudget b:
                {
                    // Validate before voting, so a malformed allocation neither draws from the RNG nor blames the legislature.
                    var old = s.Budget;
                    var invalid = EconomySystem.Allocate(c, s, b.Shares);
                    if (invalid != null) return invalid;
                    if (PoliticsSystem.BudgetNeedsVote(c, s.Date) && !PassVote(c, s))
                    {
                        s.Budget = old;
                        return $"{Offices.Legislature(c, s.Date)!.Body} voted the budget down";
                    }
                    return null;
                }
                case ChooseEventOption e:
                    return EventSystem.Choose(c, s, e.EventId, e.OptionId, why);
                case SetMobilization m:
                    return SecuritySystem.SetMobilization(c, s, m.Level, why);
                case SecurityOrder o:
                    // long arithmetic: an extreme delta clamps to the ladder's end instead of wrapping around.
                    SecuritySystem.SetRung(c, s, (int)Math.Max(1L, Math.Min(5L, (long)s.Rung + o.Delta)), why);
                    return null;
                default:
                    return $"Unknown command '{cmd.Kind}'";
            }
        }

        static bool PassVote(Content c, GameState s)
        {
            var rng = new Rng(s.RngState);
            var passed = PoliticsSystem.Vote(c, s, ref rng);
            s.RngState = rng.State;
            return passed;
        }
    }
}
