using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EffectsSystem
    {
        public static void Init(Content c, GameState s)
        {
            foreach (var i in c.Indicators) s.Indicators[i.Id] = i.Base;
            foreach (var p in c.Policies) s.Policies[p.Id] = p.Default;
            foreach (var e in c.Effects) s.EffectContributions[e.Id] = 0;
        }

        /// <summary>0.5 + competence/20: a 10-competence minister delivers the full effect, a 0 delivers half.</summary>
        public static double Multiplier(Content c, string office, DateTime date)
        {
            if (office == "") return 1.0;
            var holder = Offices.Holder(c, office, date);
            return holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
        }

        public static void Apply(Content c, GameState s, List<Cause> why)
        {
            // Read every source before writing anything, so effect order cannot change results.
            var targets = c.Effects.ToDictionary(e => e.Id, e =>
                e.K * (s.Policies.TryGetValue(e.Source, out var p) ? p : s.Indicators[e.Source]) * Multiplier(c, e.Office, s.Date));
            foreach (var e in c.Effects)
            {
                var old = s.EffectContributions[e.Id];
                var now = old + (targets[e.Id] - old) / e.Inertia;
                s.EffectContributions[e.Id] = now;
                if (Math.Abs(now - old) > 1e-9)
                    why.Add(new Cause("effects", e.Target, now - old, $"{e.Target} {now - old:+0.000;-0.000} ({e.Id})"));
            }
            foreach (var i in c.Indicators)
            {
                var sum = i.Base + c.Effects.Where(e => e.Target == i.Id).Sum(e => s.EffectContributions[e.Id]);
                s.Indicators[i.Id] = Math.Max(0, Math.Min(1, sum));
            }
            UpdateSituations(c, s, why);
        }

        public static void UpdateSituations(Content c, GameState s, List<Cause> why)
        {
            foreach (var sit in c.Situations)
            {
                var v = s.Indicators[sit.Indicator];
                bool on = s.ActiveSituations.Contains(sit.Id);
                bool turnOn = sit.OnBelow.HasValue ? v < sit.OnBelow.Value : v > sit.OnAbove!.Value;
                bool turnOff = sit.OffAbove.HasValue ? v > sit.OffAbove.Value : v < sit.OffBelow!.Value;
                if (!on && turnOn) { s.ActiveSituations.Add(sit.Id); why.Add(new Cause("situations", sit.Id, 1, $"{sit.Id} began ({sit.Indicator} = {v:0.00})")); }
                else if (on && turnOff) { s.ActiveSituations.Remove(sit.Id); why.Add(new Cause("situations", sit.Id, -1, $"{sit.Id} ended ({sit.Indicator} = {v:0.00})")); }
            }
        }

        public static double Approval(Content c, GameState s) =>
            c.VoterGroups.Count == 0 ? 0.5 :
            c.VoterGroups.Average(g => Math.Max(0, Math.Min(1, g.Weights.Sum(w => w.Value * s.Indicators[w.Key]))));
    }
}
