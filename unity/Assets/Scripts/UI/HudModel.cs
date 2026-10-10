#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>One top-bar indicator: its value and the change since the last turn (empty when there is none).</summary>
    public readonly struct Meter
    {
        public readonly string Value, Delta;
        /// <summary>+1 up, -1 down, 0 unchanged or unknown.</summary>
        public readonly int Sign;
        public Meter(string value, string delta, int sign) { Value = value; Delta = delta; Sign = sign; }
    }

    /// <summary>What the top bar shows, computed from simulation state only (no UI types), so it is testable on its own.
    /// Indicators (0..1) show as 0-100; Treasury is the net position (cash minus debt, IL millions); Immigration is the
    /// number of immigrants who arrived during the last turn.</summary>
    public sealed class HudModel
    {
        public string Year = "", Turn = "", Date = "", Attention = "", SecurityLevel = "";
        public Meter Treasury, Immigration, Security, Support, Standing, Fx;

        static double Arrivals(StepResult? r) => r == null ? 0 : r.Why.Where(w => w.System == "population").Sum(w => w.Delta);

        public static HudModel From(SimBridge b, UiText t)
        {
            var c = b.Content; var s = b.State; var prev = b.Session.Previous;
            Meter M(double now, double? before, string valueKey, string deltaKey)
            {
                if (before == null) return new Meter(t.Format(valueKey, now), "", 0);
                var d = Math.Round(now - before.Value, deltaKey == "hud.delta_money" ? 1 : 0, MidpointRounding.AwayFromZero);
                return d == 0 ? new Meter(t.Format(valueKey, now), "", 0) : new Meter(t.Format(valueKey, now), t.Format(deltaKey, d), Math.Sign(d));
            }
            double Pct(GameState x, string id) => Math.Round(x.Indicators[id] * 100, MidpointRounding.AwayFromZero);
            return new HudModel
            {
                Year = s.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Turn = t.Format("hud.turn", s.Turn),
                Date = t.Format("hud.date", s.Date),
                Attention = t.Format("hud.attention", b.AttentionLeft, c.Rules.AttentionPerTurn),
                SecurityLevel = t.Format("hud.level", s.Rung),
                Treasury = M(s.Treasury - s.Debt, prev == null ? (double?)null : prev.Treasury - prev.Debt, "hud.money", "hud.delta_money"),
                Immigration = M(Arrivals(b.Session.Last), b.BeforeLast == null ? (double?)null : Arrivals(b.BeforeLast), "hud.people", "hud.delta_people"),
                Security = M(Pct(s, "security"), prev == null ? (double?)null : Pct(prev, "security"), "hud.percent", "hud.delta"),
                Support = M(Math.Round(EffectsSystem.Approval(c, s) * 100, MidpointRounding.AwayFromZero), prev == null ? (double?)null : Math.Round(EffectsSystem.Approval(c, prev) * 100, MidpointRounding.AwayFromZero), "hud.percent", "hud.delta"),
                Standing = M(Pct(s, "intl_standing"), prev == null ? (double?)null : Pct(prev, "intl_standing"), "hud.percent", "hud.delta"),
                Fx = M(s.FxReserves, prev?.FxReserves, "hud.fx", "hud.delta_money"),
            };
        }
    }
}
