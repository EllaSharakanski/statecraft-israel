using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>A player (or AI) order for one turn. Kind keys rules.yaml attention_cost (see AttentionSystem.CommandKinds).</summary>
    public abstract class Command { public abstract string Kind { get; } }

    public sealed class SetPolicy : Command
    {
        public override string Kind => "set_policy";
        public string Policy { get; } public double Level { get; }
        public SetPolicy(string policy, double level) { Policy = policy; Level = level; }
    }

    /// <summary>Shares must cover exactly the offices in rules.yaml budget_start and sum to 1.</summary>
    public sealed class AllocateBudget : Command
    {
        public override string Kind => "allocate_budget";
        public Dictionary<string, double> Shares { get; }
        public AllocateBudget(Dictionary<string, double> shares) { Shares = shares; }
    }

    public sealed class ChooseEventOption : Command
    {
        public override string Kind => "choose_event_option";
        public string EventId { get; } public string OptionId { get; }
        public ChooseEventOption(string eventId, string optionId) { EventId = eventId; OptionId = optionId; }
    }

    public sealed class SetMobilization : Command
    {
        public override string Kind => "set_mobilization";
        public double Level { get; }
        public SetMobilization(double level) { Level = level; }
    }

    public sealed class SecurityOrder : Command
    {
        public override string Kind => "security_order";
        public int Delta { get; }
        public SecurityOrder(int delta) { Delta = delta; }
    }

    /// <summary>Puts <see cref="Person"/> in <see cref="Office"/> (Cabinet.Appoint has the rules).</summary>
    public sealed class Appoint : Command
    {
        public override string Kind => Cabinet.Kind;
        public string Office { get; } public string Person { get; }
        public Appoint(string office, string person) { Office = office; Person = person; }
    }

    public sealed class Rejection
    {
        public Command Command { get; } public string Reason { get; }
        public Rejection(Command command, string reason) { Command = command; Reason = reason; }
    }

    public sealed class StepResult
    {
        public GameState State { get; }
        public IReadOnlyList<Cause> Why { get; }
        public IReadOnlyList<Rejection> Rejected { get; }
        public StepResult(GameState state, IReadOnlyList<Cause> why, IReadOnlyList<Rejection> rejected) { State = state; Why = why; Rejected = rejected; }
    }
}
