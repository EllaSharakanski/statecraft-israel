using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EffectsSystem
    {
        public static void Init(Content c, GameState s)
        {
            foreach (var i in c.Indicators) { s.Indicators[i.Id] = i.Base; s.IndicatorShifts[i.Id] = 0.0; }
            foreach (var p in c.Policies) s.Policies[p.Id] = p.Default;
            foreach (var e in c.Effects) s.EffectContributions[e.Id] = 0;
        }

        /// <summary>Competence (0.5 + competence/20) times budget factor. No office: 1.0.</summary>
        public static double Multiplier(Content c, GameState s, string office)
        {
            if (office == "") return 1.0;
            var holder = Offices.Holder(c, office, s.Date);
            var competence = holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
            return competence * BudgetFactor(s, office);
        }

        /// <summary>An equal split of the budget is neutral (1.0); a ministry's factor runs from 0.5 (no money) to 1.5.
        /// An office with no budget line (not in budget_start, so the player can never fund it) is neutral too:
        /// treating it as unfunded would halve its effects permanently with no way to fix it.</summary>
        public static double BudgetFactor(GameState s, string office)
        {
            if (!s.Budget.TryGetValue(office, out var share)) return 1.0;
            return Math.Min(1.5, 0.5 + share * s.Budget.Count / 2.0);
        }

        public static void Apply(Content c, GameState s, List<Cause> why)
        {
            // Read every source before writing anything, so effect order cannot change results.
            var targets = c.Effects.ToDictionary(e => e.Id, e =>
                e.K * (s.Policies.TryGetValue(e.Source, out var p) ? p : s.Indicators[e.Source]) * Multiplier(c, s, e.Office));
            var moved = new HashSet<string>();
            foreach (var e in c.Effects)
            {
                var old = s.EffectContributions[e.Id];
                var now = old + (targets[e.Id] - old) / e.Inertia;
                s.EffectContributions[e.Id] = now;
                if (Math.Abs(now - old) > 1e-9) moved.Add(e.Id);
            }
            var before = c.Indicators.ToDictionary(i => i.Id, i => s.Indicators[i.Id]);
            foreach (var i in c.Indicators) s.Indicators[i.Id] = Level(c, s, i);
            // One cause per indicator that actually changed (a clamped indicator reports nothing).
            foreach (var i in c.Indicators)
            {
                var delta = s.Indicators[i.Id] - before[i.Id];
                if (Math.Abs(delta) <= 1e-9) continue;
                var ids = c.Effects.Where(e => e.Target == i.Id && moved.Contains(e.Id)).Select(e => e.Id).ToList();
                var from = ids.Count > 0 ? $" ({string.Join(", ", ids)})" : "";
                why.Add(new Cause("effects", i.Id, delta, FormattableString.Invariant($"{i.Id} {delta:+0.000;-0.000;+0.000}{from}")));
            }
            UpdateSituations(c, s, why);
        }

        /// <summary>clamp(base + effect contributions) to [0, 1], before event shifts.</summary>
        internal static double Inner(Content c, GameState s, Indicator i) =>
            Math.Max(0, Math.Min(1, i.Base + c.Effects.Where(e => e.Target == i.Id).Sum(e => s.EffectContributions[e.Id])));

        /// <summary>clamp(inner + event shift) to [0, 1].</summary>
        internal static double Level(Content c, GameState s, Indicator i) =>
            Math.Max(0, Math.Min(1, Inner(c, s, i) + s.IndicatorShifts[i.Id]));

        public static void UpdateSituations(Content c, GameState s, List<Cause> why)
        {
            foreach (var sit in c.Situations)
            {
                var v = s.Indicators[sit.Indicator];
                bool on = s.ActiveSituations.Contains(sit.Id);
                bool turnOn = sit.OnBelow.HasValue ? v < sit.OnBelow.Value : v > sit.OnAbove!.Value;
                bool turnOff = sit.OffAbove.HasValue ? v > sit.OffAbove.Value : v < sit.OffBelow!.Value;
                if (!on && turnOn) { s.ActiveSituations.Add(sit.Id); why.Add(new Cause("situations", sit.Id, 1, FormattableString.Invariant($"{sit.Id} began ({sit.Indicator} = {v:0.00})"))); }
                else if (on && turnOff) { s.ActiveSituations.Remove(sit.Id); why.Add(new Cause("situations", sit.Id, -1, FormattableString.Invariant($"{sit.Id} ended ({sit.Indicator} = {v:0.00})"))); }
            }
        }

        public static double Approval(Content c, GameState s) =>
            c.VoterGroups.Count == 0 ? 0.5 :
            c.VoterGroups.Average(g => Math.Max(0, Math.Min(1, g.Weights.Sum(w => w.Value * s.Indicators[w.Key]))));
    }
}
