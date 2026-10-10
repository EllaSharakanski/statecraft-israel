#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Budget screen: each budgeted ministry's share of spending, its minister and the effect multiplier the
    /// share gives (the sim's own EffectsSystem.BudgetFactor), the money position, and whether the legislature must approve. +/- moves
    /// a ministry by 5 points and rescales the others so shares always sum to 1. Submit queues AllocateBudget.</summary>
    public sealed class BudgetPresenter : IDisposable
    {
        public const double Step = 0.05;
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;
        readonly Button reset, submit;
        Dictionary<string, double> draft = new Dictionary<string, double>();

        public BudgetPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-budget"); this.bridge = bridge; this.text = text;
            reset = screen.Q<Button>("budget-reset"); submit = screen.Q<Button>("budget-submit");
            reset.clicked += Reset; submit.clicked += Submit;
            draft = Current();
            bridge.Changed += OnChanged;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the buttons and removes the rows (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= OnChanged;
            reset.clicked -= Reset; submit.clicked -= Submit;
            screen.Q<ScrollView>("budget-rows")?.Clear();
        }

        void Reset() { draft = Current(); Render(); }

        // A new game, a loaded save or a finished turn replaces the draft unless the player has unsaved edits.
        void OnChanged()
        {
            if (bridge.QueuedLike(new AllocateBudget(new Dictionary<string, double>())) == null && !Dirty) draft = Current();
            Render();
        }

        /// <summary>The shares the screen proposes.</summary>
        public IReadOnlyDictionary<string, double> Draft => draft;

        Dictionary<string, double> Current() => new Dictionary<string, double>(bridge.State.Budget);

        bool Dirty => draft.Any(kv => Math.Abs(kv.Value - (bridge.State.Budget.TryGetValue(kv.Key, out var v) ? v : 0)) > 1e-9);

        /// <summary>Moves <paramref name="office"/> by <paramref name="delta"/> (clamped to [0, 1]) and rescales the other
        /// shares proportionally; the largest other share absorbs rounding so the total is exactly 1.</summary>
        public static Dictionary<string, double> Adjust(IReadOnlyDictionary<string, double> shares, string office, double delta)
        {
            var target = Math.Max(0, Math.Min(1, shares[office] + delta));
            var others = shares.Keys.Where(k => k != office).OrderBy(k => k, StringComparer.Ordinal).ToList();
            var rest = others.Sum(k => shares[k]);
            var result = new Dictionary<string, double> { [office] = target };
            foreach (var k in others) result[k] = rest > 0 ? shares[k] / rest * (1 - target) : (1 - target) / others.Count;
            if (others.Count > 0)
            {
                var largest = others.OrderByDescending(k => result[k]).ThenBy(k => k, StringComparer.Ordinal).First();
                result[largest] = 1 - result.Where(kv => kv.Key != largest).Sum(kv => kv.Value);
            }
            return result;
        }

        public void Change(string office, double delta) { draft = Adjust(draft, office, delta); Render(); }

        void Submit()
        {
            // The bridge copies the shares and reports a refusal itself; the sim checks coverage and the sum.
            bridge.Queue(new AllocateBudget(new Dictionary<string, double>(draft)));
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var rows = screen.Q<ScrollView>("budget-rows");
            rows.Clear();
            foreach (var office in c.Rules.BudgetStart.Keys)
            {
                var o = c.Offices.First(x => x.Id == office);
                var share = draft.TryGetValue(office, out var v) ? v : 0;
                var row = new VisualElement { name = "budget-" + office };
                row.AddToClassList("row");
                row.Add(new Label(text.Format("budget.office", o.Title, Cabinet.Holder(c, s, office)?.Name ?? "-")) { name = "budget-" + office + "-name" });
                row[0].AddToClassList("grow");
                row.Add(new Button(() => Change(office, -Step)) { name = "budget-" + office + "-down", text = "-" });
                row.Add(new Label(text.Format("budget.share", share * 100)) { name = "budget-" + office + "-share" });
                row.Add(new Button(() => Change(office, +Step)) { name = "budget-" + office + "-up", text = "+" });
                var factor = EffectsSystem.BudgetFactor(draft, office);
                row.Add(new Label(text.Format("budget.factor", factor)) { name = "budget-" + office + "-factor" });
                row[4].AddToClassList("muted");
                rows.Add(row);
            }
            var leg = Offices.Legislature(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            screen.Q<Label>("budget-vote").text = leg == null ? "" : leg.BudgetApproval
                ? text.Format("budget.vote_needed", leg.Body, seats, size) : text.Format("budget.no_vote", leg.Body);
            screen.Q<Label>("budget-money").text = text.Format("budget.money", s.Treasury, s.Debt, s.FxReserves);
            var flows = EconomySystem.Flows(c, s, 1.0);   // the sim's own arithmetic, for a year
            screen.Q<Label>("budget-flow").text = text.Format("budget.flow", flows.Tax, flows.Spending, flows.Mobilization);
            var queued = bridge.QueuedLike(new AllocateBudget(new Dictionary<string, double>())) != null;
            screen.Q<Label>("budget-status").text = queued ? text["budget.queued"] : Dirty ? text["budget.unsaved"] : "";
        }
    }
}
