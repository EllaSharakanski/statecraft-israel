using System;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Player-facing wording shared by the sim (Cause text, refusal reasons) and the UI: the one date format
    /// ("15 May 1948") and the display names content gives its ids. No player-facing string shows a content id or an
    /// ISO date (spec: player-facing text).</summary>
    public static class PlayerText
    {
        /// <summary>"15 May 1948".</summary>
        public static string Date(DateTime d) => d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        /// <summary>"May 1948".</summary>
        public static string Month(DateTime d) => d.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        /// <summary>The display name of an aliyah origin group (aliyah.yaml regions).</summary>
        public static string Region(Content c, string id) => c.Regions.FirstOrDefault(r => r.Id == id)?.Name ?? id;

        /// <summary>The display name of what a cause changes: an indicator or policy (model.yaml names), the treasury or
        /// the foreign currency reserves.</summary>
        public static string Target(Content c, string id) => id switch
        {
            "treasury" => "Treasury",
            "fx_reserves" => "Foreign currency reserves",
            _ => c.Indicators.FirstOrDefault(i => i.Id == id)?.Name ?? c.Policies.FirstOrDefault(p => p.Id == id)?.Name ?? id,
        };
    }
}
