#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Defence screen: the always-on escalation ladder (spec section 3) with the current level and its turn
    /// length, the UN ceasefire clock in full war, security orders to raise or lower the level, and the reserve call-up
    /// with its yearly cost. Orders are queued for End Turn.</summary>
    public sealed class DefencePresenter : IDisposable
    {
        public const double MobilizationStep = 0.05;
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;
        readonly Button escalate, deEscalate, mobUp, mobDown; readonly Action onEscalate, onDeEscalate, onMobUp, onMobDown;

        public DefencePresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-defence"); this.bridge = bridge; this.text = text;
            escalate = screen.Q<Button>("escalate"); deEscalate = screen.Q<Button>("de-escalate");
            mobUp = screen.Q<Button>("mob-up"); mobDown = screen.Q<Button>("mob-down");
            onEscalate = () => Order(+1); onDeEscalate = () => Order(-1);
            onMobUp = () => Mobilize(+MobilizationStep); onMobDown = () => Mobilize(-MobilizationStep);
            escalate.clicked += onEscalate; deEscalate.clicked += onDeEscalate; mobUp.clicked += onMobUp; mobDown.clicked += onMobDown;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the buttons (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render;
            escalate.clicked -= onEscalate; deEscalate.clicked -= onDeEscalate; mobUp.clicked -= onMobUp; mobDown.clicked -= onMobDown;
        }

        int QueuedDelta => (bridge.QueuedLike(new SecurityOrder(0)) as SecurityOrder)?.Delta ?? 0;

        /// <summary>The call-up level the turn will end with: the queued order's, else today's.</summary>
        public double ProposedMobilization => (bridge.QueuedLike(new SetMobilization(0)) as SetMobilization)?.Level ?? bridge.State.Mobilization;

        /// <summary>The security level the turn will end with: today's, moved by the queued order.</summary>
        int TargetRung => SecuritySystem.ClampRung(bridge.State.Rung + QueuedDelta);

        void Order(int step)
        {
            var s = bridge.State;
            var target = SecuritySystem.ClampRung(s.Rung + QueuedDelta + step);
            if (target == s.Rung) Withdraw(new SecurityOrder(0)); else bridge.Queue(new SecurityOrder(target - s.Rung));
            DropNoOpCallUp();
        }

        void Withdraw(Command like)
        {
            var queued = bridge.QueuedLike(like);
            if (queued != null) bridge.Withdraw(queued);
        }

        /// <summary>The call-up the turn will end with if <paramref name="level"/> is ordered (the sim raises a lower one to the minimum).</summary>
        double EffectiveAt(double level) => SecuritySystem.EffectiveMobilization(bridge.Content, TargetRung, level);

        bool SameAsToday(double level) => Math.Abs(EffectiveAt(level) - EffectiveAt(bridge.State.Mobilization)) < 1e-9;

        /// <summary>A queued call-up order that would change nothing (after the minimum is applied) is withdrawn, so it
        /// neither costs attention nor shows a stale proposed level.</summary>
        void DropNoOpCallUp()
        {
            if (bridge.QueuedLike(new SetMobilization(0)) is SetMobilization q && SameAsToday(q.Level)) bridge.Withdraw(q);
        }

        void Mobilize(double step)
        {
            var floor = SecuritySystem.MinMobilization(bridge.Content, TargetRung);
            var level = Math.Max(floor, Math.Min(1, Math.Round((EffectiveAt(ProposedMobilization) + step) / MobilizationStep) * MobilizationStep));
            if (SameAsToday(level)) Withdraw(new SetMobilization(0)); else bridge.Queue(new SetMobilization(level));
        }

        /// <summary>The string key for a turn length. A switch, so a new TurnLength value fails loudly (and in a test).</summary>
        public static string TurnKey(TurnLength length)
        {
            switch (length)
            {
                case TurnLength.Quarter: return "defence.turn_quarter";
                case TurnLength.Month: return "defence.turn_month";
                case TurnLength.Week: return "defence.turn_week";
                default: throw new ArgumentOutOfRangeException(nameof(length), length, "no string for this turn length");
            }
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            for (int rung = 1; rung <= 5; rung++)
            {
                screen.Q<Label>($"rung-{rung}-name").text = text[$"defence.rung{rung}"];
                screen.Q<Label>($"rung-{rung}-turn").text = text[TurnKey(SecuritySystem.TurnLengthFor(rung))];
                screen.Q<VisualElement>($"rung-{rung}").EnableInClassList("rung-current", rung == s.Rung);
            }
            screen.Q<Label>("defence-command").text = text.Format("defence.command",
                Cabinet.Holder(c, s, "defense_minister")?.Name ?? "-", Cabinet.Holder(c, s, "chief_of_staff")?.Name ?? "-");
            var delta = QueuedDelta;
            var target = TargetRung;
            var ceasefire = screen.Q<Label>("ceasefire");
            if (s.Rung == 5 && target == 5) ceasefire.text = text.Format("defence.ceasefire", s.CeasefireWeeksLeft);
            else if (s.Rung == 5) ceasefire.text = text["defence.ceasefire_stops"];                       // a lowering is queued
            else if (target == 5) ceasefire.text = text.Format("defence.ceasefire_starts", SecuritySystem.CeasefireWeeksAfter(c, s.Rung, s.CeasefireWeeksLeft, target));
            else ceasefire.text = "";
            ceasefire.style.display = ceasefire.text == "" ? DisplayStyle.None : DisplayStyle.Flex;
            screen.Q<Label>("order-pending").text = delta == 0 ? "" : text.Format("defence.order", s.Rung, target);
            var floor = SecuritySystem.MinMobilization(c, target);
            var proposed = EffectiveAt(ProposedMobilization);   // what the call-up will be: the sim raises a lower order to the minimum
            screen.Q<Label>("mob-level").text = text.Format("defence.mob", PlayerText.Percent(proposed), target, PlayerText.Percent(floor));
            screen.Q<Label>("mob-cost").text = text.Format("defence.mob_cost", PlayerText.Money(c, "treasury", c.Rules.MobilizationCostPerYear * proposed));
            screen.Q<Button>("escalate").SetEnabled(target < 5);
            screen.Q<Button>("de-escalate").SetEnabled(target > 1);
        }
    }
}
