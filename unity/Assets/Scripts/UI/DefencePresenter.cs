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

        void Order(int step)
        {
            var s = bridge.State;
            var target = Math.Max(1, Math.Min(5, s.Rung + QueuedDelta + step));
            if (target == s.Rung) Withdraw(new SecurityOrder(0)); else bridge.Queue(new SecurityOrder(target - s.Rung));
        }

        void Withdraw(Command like)
        {
            var queued = bridge.QueuedLike(like);
            if (queued != null) bridge.Withdraw(queued);
        }

        /// <summary>The call-up minimum at the level the turn will end with (the sim raises a lower call-up to it).</summary>
        double Floor => bridge.Content.Rules.RungMinMobilization[Math.Max(1, Math.Min(5, bridge.State.Rung + QueuedDelta)) - 1];

        void Mobilize(double step)
        {
            var level = Math.Max(Floor, Math.Min(1, Math.Round((Math.Max(ProposedMobilization, Floor) + step) / MobilizationStep) * MobilizationStep));
            if (Math.Abs(level - bridge.State.Mobilization) < 1e-9) Withdraw(new SetMobilization(0)); else bridge.Queue(new SetMobilization(level));
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            for (int rung = 1; rung <= 5; rung++)
            {
                screen.Q<Label>($"rung-{rung}-name").text = text[$"defence.rung{rung}"];
                screen.Q<Label>($"rung-{rung}-turn").text = text["defence.turn_" + SecuritySystem.TurnLengthFor(rung).ToString().ToLowerInvariant()];
                screen.Q<VisualElement>($"rung-{rung}").EnableInClassList("rung-current", rung == s.Rung);
            }
            screen.Q<Label>("defence-command").text = text.Format("defence.command",
                Cabinet.Holder(c, s, "defense_minister")?.Name ?? "-", Cabinet.Holder(c, s, "chief_of_staff")?.Name ?? "-");
            var ceasefire = screen.Q<Label>("ceasefire");
            ceasefire.text = s.Rung == 5 ? text.Format("defence.ceasefire", s.CeasefireWeeksLeft) : "";
            ceasefire.style.display = s.Rung == 5 ? DisplayStyle.Flex : DisplayStyle.None;
            var delta = QueuedDelta;
            screen.Q<Label>("order-pending").text = delta == 0 ? "" : text.Format("defence.order", s.Rung, s.Rung + delta);
            var floor = Floor;
            var proposed = Math.Max(ProposedMobilization, floor);   // what the call-up will be: the sim raises a lower order to the floor
            screen.Q<Label>("mob-level").text = text.Format("defence.mob", proposed * 100, s.Rung + delta, floor * 100);
            screen.Q<Label>("mob-cost").text = text.Format("defence.mob_cost", c.Rules.MobilizationCostPerYear * Math.Max(proposed, floor));
            screen.Q<Button>("escalate").SetEnabled(s.Rung + delta < 5);
            screen.Q<Button>("de-escalate").SetEnabled(s.Rung + delta > 1);
        }
    }
}
