using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EventSystem
    {
        public const int MaxSoftEventsPerTurn = 3;

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        /// <summary>Fires every unfired event whose window [From, To) overlaps the turn just played,
        /// [<paramref name="turnStart"/>, s.Date), or contains s.Date; <paramref name="turnStart"/> defaults to s.Date
        /// (game start). Every eligible hard anchor fires; MaxSoftEventsPerTurn caps only the non-hard events per turn,
        /// which fill the slots that remain (max(0, cap - hard count)) in the usual order (From, then id). The fired
        /// events keep the usual order.
        /// Late fire: an event whose window overlapped the turn just played can fire after its window has ended; its
        /// conditions are evaluated at the end of the turn, and its text is filled at the last in-window day
        /// (<see cref="FillDate"/>).</summary>
        public static void Trigger(Content c, GameState s, List<Cause> why, DateTime? turnStart = null)
        {
            var start = turnStart ?? s.Date;
            var eligible = c.Events
                .Where(e => !s.FiredEvents.Contains(e.Id) && e.From <= s.Date && (e.To == null || start < e.To.Value))
                .Where(e => e.Conditions.All(k => (!k.Min.HasValue || s.Indicators[k.Indicator] >= k.Min.Value)
                                               && (!k.Max.HasValue || s.Indicators[k.Indicator] <= k.Max.Value)))
                .OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
            var hardCount = eligible.Count(e => e.Anchor == "hard");
            var chosen = new HashSet<string>(eligible.Where(e => e.Anchor == "hard")
                .Concat(eligible.Where(e => e.Anchor != "hard").Take(Math.Max(0, MaxSoftEventsPerTurn - hardCount))).Select(e => e.Id));
            foreach (var e in eligible.Where(e => chosen.Contains(e.Id)))
            {
                s.FiredEvents.Add(e.Id);
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Offices.Fill(c, e.Title, FillDate(e, s.Date))}"));
            }
        }

        /// <summary>The date whose office holders an event's text names: the given date, or the last in-window day
        /// once the window has ended (load-time coverage guarantees a holder there).</summary>
        public static DateTime FillDate(GameEvent e, DateTime date) => e.To.HasValue && date >= e.To.Value ? e.To.Value.AddDays(-1) : date;

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
            var fillDate = FillDate(Get(c, eventId), s.Date);
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
                why.Add(new Cause("events", fx.Target, change, FormattableString.Invariant($"{fx.Target} {change:+0.00;-0.00;+0.00} ({eventId}/{o.Id})")));
            }
            foreach (var kv in o.SetPolicy.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Policies[kv.Key] = kv.Value;
                why.Add(new Cause("events", kv.Key, kv.Value, FormattableString.Invariant($"{kv.Key} set to {kv.Value:0.00} ({eventId}/{o.Id})")));
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

        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, FillDate(e, date));
    }
}
