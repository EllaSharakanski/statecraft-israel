using System.Collections.Generic;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>The PM's per-turn capacity. Each command costs attention; costs rise with the security level.</summary>
    public static class AttentionSystem
    {
        public static readonly HashSet<string> CommandKinds = new HashSet<string>
            { "set_policy", "allocate_budget", "choose_event_option", "set_mobilization", "security_order" };

        public static void Refill(Content c, GameState s) => s.Attention = c.Rules.AttentionPerTurn;

        public static double Cost(Content c, GameState s, string kind) =>
            c.Rules.AttentionCost[kind] * (1 + c.Rules.RungAttentionStep * (s.Rung - 1));

        /// <summary>Returns null and deducts on success, or the reason it could not be afforded.</summary>
        public static string? TrySpend(Content c, GameState s, string kind)
        {
            var cost = Cost(c, s, kind);
            if (cost > s.Attention + 1e-9)
                return string.Format(CultureInfo.InvariantCulture, "Not enough attention (need {0:0.0}, have {1:0.0})", cost, s.Attention);
            s.Attention -= cost;
            return null;
        }
    }
}
