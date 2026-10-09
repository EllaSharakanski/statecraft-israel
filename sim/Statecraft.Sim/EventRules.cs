using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>Load-time checks for events, including the sensitive-content rule.</summary>
    static class EventRules
    {
        public static readonly HashSet<string> SensitiveTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "holocaust", "terror", "massacre", "war-casualties" };
        public static readonly HashSet<string> SpecialTargets = new HashSet<string> { "treasury", "fx_reserves" };

        public static void Validate(Content c, HashSet<string> offices, HashSet<string> indicators)
        {
            var ids = new HashSet<string>();
            foreach (var e in c.Events)
            {
                string F(string field) => $"events[{e.Id}].{field}";
                if (!ids.Add(e.Id)) throw new ContentException(e.File, F("id"), "duplicate event id");
                if (e.Anchor != "hard" && e.Anchor != "soft") throw new ContentException(e.File, F("anchor"), $"unknown anchor '{e.Anchor}' (must be hard or soft)");
                ContentLoader.Status(e.File, F("status"), e.Status);
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException(e.File, F("office"), $"unknown office '{e.Office}'");
                CheckPlaceholders(c, e, F("title"), e.Title, offices);
                CheckPlaceholders(c, e, F("text"), e.Text, offices);
                foreach (var cond in e.Conditions)
                    if (!indicators.Contains(cond.Indicator)) throw new ContentException(e.File, F($"conditions.{cond.Indicator}"), "unknown indicator");
                if (e.Options.Count == 0) throw new ContentException(e.File, F("options"), "at least one option required");
                if (e.Options.Count(o => o.Historical) > 1) throw new ContentException(e.File, F("options"), "at most one option can be historical");
                if (e.Anchor == "hard" && !e.Options.Any(o => o.Historical)) throw new ContentException(e.File, F("options"), "hard anchor needs a historical option");
                bool sensitive = e.Tags.Any(SensitiveTags.Contains);
                var optionIds = new HashSet<string>();
                foreach (var o in e.Options)
                {
                    if (!optionIds.Add(o.Id)) throw new ContentException(e.File, F($"options[{o.Id}].id"), "duplicate option id");
                    CheckPlaceholders(c, e, F($"options[{o.Id}].text"), o.Text, offices);
                    if (o.SetRung.HasValue && (o.SetRung < 1 || o.SetRung > 5)) throw new ContentException(e.File, F($"options[{o.Id}].set_rung"), "must be 1-5");
                    foreach (var fx in o.Effects)
                    {
                        if (!indicators.Contains(fx.Target) && !SpecialTargets.Contains(fx.Target))
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), "unknown target");
                        if (sensitive && (fx.Target == "treasury" || fx.Target == "fx_reserves") && fx.Delta > 0)
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), $"sensitive events cannot reward {fx.Target}");
                    }
                }
            }
        }

        static void CheckPlaceholders(Content c, GameEvent e, string field, string text, HashSet<string> offices)
        {
            if (Offices.Placeholder.Replace(text, "").IndexOfAny(new[] { '{', '}' }) >= 0)
                throw new ContentException(e.File, field, "stray '{' or '}' outside an {office_id} placeholder");
            foreach (Match m in Offices.Placeholder.Matches(text))
            {
                var office = m.Groups[1].Value;
                if (!offices.Contains(office)) throw new ContentException(e.File, field, $"unknown office placeholder '{m.Value}'");
                var gap = FirstUncovered(c, office, e.From, e.To);
                if (gap.HasValue)
                    throw new ContentException(e.File, field, FormattableString.Invariant($"office '{office}' has no holder on {gap.Value:yyyy-MM-dd}, inside the event window"));
            }
        }

        /// <summary>First date in [from, to) with nobody holding office, or null. Holders only change at term
        /// starts and ends, so checking those dates (plus from) covers the whole window.</summary>
        static DateTime? FirstUncovered(Content c, string office, DateTime from, DateTime? to)
        {
            bool Inside(DateTime d) => d >= from && (to == null || d < to.Value);
            var terms = c.People.SelectMany(p => p.Offices).Where(t => t.Office == office).ToList();
            var points = new SortedSet<DateTime> { from };
            foreach (var t in terms)
            {
                if (Inside(t.From)) points.Add(t.From);
                if (t.To.HasValue && Inside(t.To.Value)) points.Add(t.To.Value);
            }
            // An open-ended window needs no extra check: a last finite term ends inside [from, ∞) and is caught above.
            foreach (var d in points)
                if (Offices.Holder(c, office, d) == null) return d;
            return null;
        }
    }
}
