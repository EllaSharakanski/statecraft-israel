using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EconomySystem
    {
        public const string FxCrisis = "fx_crisis";

        public static void Init(Content c, GameState s)
        {
            s.Treasury = c.Rules.TreasuryStart;
            s.FxReserves = c.Rules.FxStart;
            s.Budget = new Dictionary<string, double>(c.Rules.BudgetStart);
        }

        public static void Apply(Content c, GameState s, double yearFraction, List<Cause> why)
        {
            var r = c.Rules; var pop = s.TotalPopulation;
            var revenue = r.TaxPerCapitaPerYear * pop * yearFraction;
            var spending = r.SpendingPerCapitaPerYear * pop * yearFraction;
            var mobilization = r.MobilizationCostPerYear * s.Mobilization * yearFraction;
            s.Treasury += revenue - spending - mobilization;
            why.Add(new Cause("economy", "treasury", revenue - spending - mobilization,
                FormattableString.Invariant($"treasury {revenue - spending - mobilization:+0.00;-0.00;+0.00} (tax {revenue:0.00}, spending {spending:0.00}, reserve call-up {mobilization:0.00})")));
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                why.Add(new Cause("economy", "debt", -s.Treasury, FormattableString.Invariant($"borrowed {-s.Treasury:0.00} to cover the deficit")));
                s.Treasury = 0;
            }

            var exports = r.ExportsPerYear * yearFraction;
            var inflow = r.CapitalInflowPerYear * yearFraction;
            var imports = r.ImportsPerCapitaPerYear * pop * yearFraction;
            var net = exports + inflow - imports;
            s.FxReserves += net;
            why.Add(new Cause("economy", "fx_reserves", net, FormattableString.Invariant($"foreign currency {net:+0.00;-0.00;+0.00} (exports {exports:0.00}, donations and loans {inflow:0.00}, imports {imports:0.00})")));

            bool on = s.ActiveSituations.Contains(FxCrisis);
            if (!on && s.FxReserves < 0) { s.ActiveSituations.Add(FxCrisis); why.Add(new Cause("economy", FxCrisis, 1, "Foreign currency reserves are exhausted")); }
            else if (on && s.FxReserves > 0.25 * r.FxStart) { s.ActiveSituations.Remove(FxCrisis); why.Add(new Cause("economy", FxCrisis, -1, "Foreign currency crisis eased")); }

            // The consequence: with no currency to pay for them, food imports are cut while the crisis lasts.
            if (s.ActiveSituations.Contains(FxCrisis) && r.FxCrisisFoodPerYear < 0)
            {
                var change = EffectsSystem.Shift(c, s, "food_supply", r.FxCrisisFoodPerYear * yearFraction);
                if (change != 0)
                    why.Add(new Cause("economy", "food_supply", change, FormattableString.Invariant($"food_supply {change:+0.000;-0.000;+0.000} (foreign currency crisis: food imports cut)")));
            }
        }

        /// <summary>Returns null on success, or the reason the allocation was rejected.</summary>
        public static string? Allocate(Content c, GameState s, Dictionary<string, double> shares)
        {
            if (shares == null) return "Budget shares are required";
            var unknown = shares.Keys.FirstOrDefault(k => c.Offices.All(o => o.Id != k));
            if (unknown != null) return $"Unknown office '{unknown}'";
            if (shares.Values.Any(v => double.IsNaN(v) || double.IsInfinity(v))) return "Budget shares must be finite numbers";
            if (!new HashSet<string>(shares.Keys).SetEquals(c.Rules.BudgetStart.Keys))
                return "Budget shares must cover exactly: " + string.Join(", ", c.Rules.BudgetStart.Keys.OrderBy(k => k, StringComparer.Ordinal));
            if (Math.Abs(shares.Values.Sum() - 1.0) > 1e-6) return "Budget shares must sum to 1";
            if (shares.Values.Any(v => v < 0)) return "Budget shares cannot be negative";
            s.Budget = new Dictionary<string, double>(shares);
            return null;
        }
    }
}
