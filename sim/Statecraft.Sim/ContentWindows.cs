using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Keeps the state's model records in step with their windows (chapters design 3.2). Called once per turn, after the
    /// date moves. A record that begins is added as <see cref="EffectsSystem.Init"/> would add it (an indicator at its base with
    /// shift 0, a policy at its default, an effect's contribution at 0); one that ends is removed. Records go in and out in
    /// ordinal id order. With no window anywhere the two views are the same object and nothing happens.</summary>
    public static class ContentWindows
    {
        public static void Update(Content c, GameState s, DateTime turnStart)
        {
            var before = c.At(turnStart);
            var after = c.At(s.Date);
            if (ReferenceEquals(before, after)) return;

            var gone = new HashSet<string>(before.Indicators.Select(x => x.Id).Except(after.Indicators.Select(x => x.Id)), StringComparer.Ordinal);
            foreach (var id in gone.OrderBy(x => x, StringComparer.Ordinal)) { s.Indicators.Remove(id); s.IndicatorShifts.Remove(id); }
            foreach (var id in before.Policies.Select(x => x.Id).Except(after.Policies.Select(x => x.Id)).OrderBy(x => x, StringComparer.Ordinal)) s.Policies.Remove(id);
            foreach (var id in before.Effects.Select(x => x.Id).Except(after.Effects.Select(x => x.Id)).OrderBy(x => x, StringComparer.Ordinal)) s.EffectContributions.Remove(id);
            foreach (var id in before.Situations.Select(x => x.Id).Except(after.Situations.Select(x => x.Id)).OrderBy(x => x, StringComparer.Ordinal)) s.ActiveSituations.Remove(id);

            var had = new HashSet<string>(before.Indicators.Select(x => x.Id), StringComparer.Ordinal);
            foreach (var i in after.Indicators.Where(x => !had.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal)) { s.Indicators[i.Id] = i.Base; s.IndicatorShifts[i.Id] = 0.0; }
            var hadPolicies = new HashSet<string>(before.Policies.Select(x => x.Id), StringComparer.Ordinal);
            foreach (var p in after.Policies.Where(x => !hadPolicies.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal)) s.Policies[p.Id] = p.Default;
            var hadEffects = new HashSet<string>(before.Effects.Select(x => x.Id), StringComparer.Ordinal);
            foreach (var e in after.Effects.Where(x => !hadEffects.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal)) s.EffectContributions[e.Id] = 0;
        }
    }
}
