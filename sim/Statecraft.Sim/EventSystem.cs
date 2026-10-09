using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EventSystem
    {
        public const int MaxNewPerTurn = 3;

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        public static void Trigger(Content c, GameState s, List<Cause> why)
        {
            var eligible = c.Events
                .Where(e => !s.FiredEvents.Contains(e.Id) && e.From <= s.Date && (e.To == null || s.Date < e.To.Value))
                .Where(e => e.Conditions.All(k => (!k.Min.HasValue || s.Indicators[k.Indicator] >= k.Min.Value)
                                               && (!k.Max.HasValue || s.Indicators[k.Indicator] <= k.Max.Value)))
                .OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
                .Take(MaxNewPerTurn);
            foreach (var e in eligible)
            {
                s.FiredEvents.Add(e.Id);
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Offices.Fill(c, e.Title, s.Date)}"));
            }
        }

        /// <summary>Returns null on success, or the reason the choice was rejected.</summary>
        public static string? Choose(Content c, GameState s, string eventId, string optionId, List<Cause> why)
        {
            if (!s.PendingEvents.Contains(eventId)) return $"Event '{eventId}' is not waiting for a decision";
            var option = Get(c, eventId).Options.FirstOrDefault(o => o.Id == optionId);
            if (option == null) return $"Event '{eventId}' has no option '{optionId}'";
            Resolve(c, s, eventId, option, "chosen", why);
            return null;
        }

        static void Resolve(Content c, GameState s, string eventId, EventOption o, string how, List<Cause> why)
        {
            s.PendingEvents.Remove(eventId);
            // Resolved on or after the window end: name whoever held the office on the last in-window day
            // (load-time coverage guarantees a holder there); otherwise today's holder.
            var e = Get(c, eventId);
            var fillDate = e.To.HasValue && s.Date >= e.To.Value ? e.To.Value.AddDays(-1) : s.Date;
            why.Add(new Cause("events", eventId, 0, $"{how}: {Offices.Fill(c, o.Text, fillDate)}"));
            foreach (var fx in o.Effects)
            {
                var change = fx.Delta;
                if (fx.Target == "treasury") s.Treasury += fx.Delta;
                else if (fx.Target == "fx_reserves") s.FxReserves += fx.Delta;
                else
                {
                    // The shift is set so the level lands exactly on the visible result; it never exceeds the clamp.
                    var ind = c.Indicators.First(i => i.Id == fx.Target);
                    var level = EffectsSystem.Level(c, s, ind);
                    var newLevel = Math.Max(0, Math.Min(1, level + fx.Delta));
                    s.IndicatorShifts[ind.Id] = newLevel - EffectsSystem.Inner(c, s, ind);
                    s.Indicators[ind.Id] = newLevel;
                    change = newLevel - level;
                }
                why.Add(new Cause("events", fx.Target, change, FormattableString.Invariant($"{fx.Target} {change:+0.00;-0.00} ({eventId}/{o.Id})")));
            }
            if (o.SetRung.HasValue) SecuritySystem.SetRung(c, s, o.SetRung.Value, why);
        }

        /// <summary>Unanswered events go to the responsible minister: with probability competence/10 they pick the
        /// recommended option (else the first), otherwise a random option. In Historical mode a hard-anchor event
        /// resolves to its historical option without drawing from the RNG (major anchors happen).</summary>
        public static void Delegate(Content c, GameState s, List<Cause> why)
        {
            var rng = new Rng(s.RngState);
            foreach (var id in s.PendingEvents.ToList())
            {
                var e = Get(c, id);
                var minister = e.Office == "" ? null : Offices.Holder(c, e.Office, s.Date);
                var competence = (minister?.Competence ?? 5) / 10.0;
                var anchored = s.Mode == Mode.Historical && e.Anchor == "hard" ? e.Options.FirstOrDefault(o => o.Historical) : null;
                var option = anchored ?? (rng.NextDouble() < competence
                    ? e.Options.FirstOrDefault(o => o.Recommended) ?? e.Options[0]
                    : e.Options[(int)(rng.NextDouble() * e.Options.Count)]);
                Resolve(c, s, id, option, $"delegated to {minister?.Name ?? "the cabinet"}", why);
            }
            s.RngState = rng.State;
        }

        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, date);
    }
}
