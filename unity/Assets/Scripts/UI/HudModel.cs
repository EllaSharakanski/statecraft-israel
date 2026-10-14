#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>One top-bar indicator: its value and the change since the last turn (empty when there is none), and the
    /// two again in words for screen readers (HudModel.Spoken): SayValue, and SayChange without its sign (empty when there
    /// is no change).</summary>
    public readonly struct Meter
    {
        public readonly string Value, Delta, SayValue, SayChange;
        /// <summary>+1 up, -1 down, 0 unchanged or unknown.</summary>
        public readonly int Sign;
        public Meter(string value, string delta, int sign, string sayValue, string sayChange)
        { Value = value; Delta = delta; Sign = sign; SayValue = sayValue; SayChange = sayChange; }
    }

    /// <summary>What the top bar shows, computed from simulation state only (no UI types), so it is testable on its own.
    /// Indicators (0..1) show as 0-100; Treasury is the net position (cash minus debt, IL millions); Immigration is the
    /// number of immigrants who arrived during the last turn.</summary>
    public sealed class HudModel
    {
        public string Year = "", Turn = "", Date = "", Attention = "", SecurityLevel = "";
        public Meter Treasury, Immigration, Security, Support, Standing, Fx;

        static double Arrivals(StepResult? r) => r == null ? 0 : r.Why.Where(w => w.System == "population").Sum(w => w.Delta);

        /// <summary>The meter in words, for screen readers (the meter's tooltip, as the event card's alt text, #398):
        /// "Treasury IL 18.6 million, down IL 1.4 million this turn"; with no change, the name and the value only.</summary>
        public static string Spoken(UiText t, string name, Meter m) =>
            m.SayChange.Length == 0 ? t.Format("hud.say", name, m.SayValue)
            : t.Format(m.Sign > 0 ? "hud.say_up" : "hud.say_down", name, m.SayValue, m.SayChange);

        public static HudModel From(SimBridge b, UiText t)
        {
            var c = b.Content; var s = b.State;
            // No change until a turn has run here: a loaded save's replay leaves Session.Previous set, but that change is not
            // the player's last turn, and Immigration's needs the step before it (SimBridge.BeforeLast), which a load lacks.
            // The save does not keep them (#398).
            var prev = b.TurnPlayed ? b.Session.Previous : null;
            // Each meter: its value and, after the first turn, the change rounded as shown (a change that rounds to zero is
            // not shown). Money comes with its currency (PlayerText.Money: IL or $ from content); the 0..1 values are on the
            // 0-100 scale with "%" (PlayerText.Scale), as everywhere the player sees them (fix round 1, I2).
            // In words: money in millions ("IL 18.6 million", "-IL 3.0 million"), the change without its sign.
            string Millions(string id, double millions, bool signed)
            {
                var r = Math.Round(millions, 1, MidpointRounding.AwayFromZero);
                var prefix = c.Money.First(x => x.Id == id).Prefix;
                return t.Format("hud.say_million", (signed && r < 0 ? "-" : "") + prefix, Math.Abs(r));
            }
            Meter Money(string id, double now, double? before)
            {
                var value = PlayerText.Money(c, id, now); var say = Millions(id, now, true);
                if (before == null) return new Meter(value, "", 0, say, "");
                var d = Math.Round(now - before.Value, 1, MidpointRounding.AwayFromZero);
                return d == 0 ? new Meter(value, "", 0, say, "") : new Meter(value, PlayerText.MoneyChange(c, id, d), Math.Sign(d), say, Millions(id, d, false));
            }
            Meter Points(double now, double? before)
            {
                var value = PlayerText.Percent(now);                   // the one formatter (fix round 2, N4)
                if (before == null) return new Meter(value, "", 0, value, "");
                // The difference of the two shown values, so the change always agrees with them (fix round 3, m-1).
                var points = PlayerText.ShownPoints(now) - PlayerText.ShownPoints(before.Value);
                var change = PlayerText.PercentChangeShown(before.Value, now);
                return points == 0 ? new Meter(value, "", 0, value, "") : new Meter(value, change, Math.Sign(points), value, change.TrimStart('+', '-'));
            }
            Meter People(double now, double? before)
            {
                var value = t.Format("hud.people", now);
                if (before == null) return new Meter(value, "", 0, value, "");
                var d = Math.Round(now - before.Value, 0, MidpointRounding.AwayFromZero);
                return d == 0 ? new Meter(value, "", 0, value, "") : new Meter(value, t.Format("hud.delta_people", d), Math.Sign(d), value, t.Format("hud.people", Math.Abs(d)));
            }
            return new HudModel
            {
                Year = s.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Turn = t.Format("hud.turn", s.Turn),
                Date = t.Format("hud.date", PlayerText.Date(s.Date)),
                Attention = t.Format("hud.attention", b.AttentionLeft, c.RulesAt(s.Date).AttentionPerTurn),
                SecurityLevel = t.Format("hud.level", s.Rung),
                Treasury = Money("treasury", s.Treasury - s.Debt, prev == null ? (double?)null : prev.Treasury - prev.Debt),
                Immigration = People(Arrivals(b.Session.Last), prev == null || b.BeforeLast == null ? (double?)null : Arrivals(b.BeforeLast)),
                Security = Points(s.Indicators["security"], prev?.Indicators["security"]),
                Support = Points(EffectsSystem.Approval(c, s), prev == null ? (double?)null : EffectsSystem.Approval(c, prev)),
                Standing = Points(s.Indicators["intl_standing"], prev?.Indicators["intl_standing"]),
                Fx = Money("fx_reserves", s.FxReserves, prev?.FxReserves),
            };
        }
    }
}
