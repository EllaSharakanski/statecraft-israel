using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>Load-time checks for events, including the sensitive-content rule.</summary>
    static class EventRules
    {
        public static readonly HashSet<string> SensitiveTags = new HashSet<string> { "holocaust", "terror", "massacre", "war-casualties" };
        public static readonly HashSet<string> SpecialTargets = new HashSet<string> { "treasury", "fx_reserves" };

        public static void Validate(Content c, HashSet<string> offices, HashSet<string> indicators)
        {
            var ids = new HashSet<string>();
            foreach (var e in c.Events)
            {
                string F(string field) => $"events[{e.Id}].{field}";
                if (!ids.Add(e.Id)) throw new ContentException(e.File, F("id"), "duplicate event id");
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException(e.File, F("office"), $"unknown office '{e.Office}'");
                CheckPlaceholders(e.File, F("title"), e.Title, offices);
                CheckPlaceholders(e.File, F("text"), e.Text, offices);
                foreach (var cond in e.Conditions)
                    if (!indicators.Contains(cond.Indicator)) throw new ContentException(e.File, F($"conditions.{cond.Indicator}"), "unknown indicator");
                if (e.Options.Count == 0) throw new ContentException(e.File, F("options"), "at least one option required");
                if (e.Options.Count(o => o.Historical) > 1) throw new ContentException(e.File, F("options"), "at most one option can be historical");
                bool sensitive = e.Tags.Any(SensitiveTags.Contains);
                foreach (var o in e.Options)
                {
                    CheckPlaceholders(e.File, F($"options[{o.Id}].text"), o.Text, offices);
                    if (o.SetRung.HasValue && (o.SetRung < 1 || o.SetRung > 5)) throw new ContentException(e.File, F($"options[{o.Id}].set_rung"), "must be 1-5");
                    foreach (var fx in o.Effects)
                    {
                        if (!indicators.Contains(fx.Target) && !SpecialTargets.Contains(fx.Target))
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.{fx.Target}"), "unknown target");
                        if (sensitive && fx.Target == "treasury" && fx.Delta > 0)
                            throw new ContentException(e.File, F($"options[{o.Id}].effects.treasury"), "sensitive events cannot reward the treasury");
                    }
                }
            }
        }

        static void CheckPlaceholders(string file, string field, string text, HashSet<string> offices)
        {
            foreach (Match m in Offices.Placeholder.Matches(text))
                if (!offices.Contains(m.Groups[1].Value)) throw new ContentException(file, field, $"unknown office placeholder '{m.Value}'");
        }
    }
}
