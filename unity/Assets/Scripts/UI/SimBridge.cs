#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The UI's only door into the simulation (spec section 6). Holds the GameSession, collects this turn's
    /// orders, runs End Turn and tells the screens to refresh. Orders take effect at End Turn, in the order given; each
    /// order replaces an earlier one with the same key (one choice per event, one call-up level, one budget, one appointee
    /// per office, one policy level per policy, one build per hex).</summary>
    public sealed class SimBridge
    {
        readonly List<Command> queue = new List<Command>();

        public GameSession Session { get; private set; }
        public Content Content => Session.Content;
        public GameState State => Session.State;
        public IReadOnlyList<Command> Queued => queue;
        /// <summary>The step before Session.Last (null until two turns have run), for "change since last turn".</summary>
        public StepResult? BeforeLast { get; private set; }
        /// <summary>Raised after every change the screens show: an order queued or withdrawn, a turn, a load.</summary>
        public event Action? Changed;
        /// <summary>Raised after End Turn with the orders the simulation refused (possibly none).</summary>
        public event Action<IReadOnlyList<Rejection>>? TurnEnded;

        public SimBridge(GameSession session) { Session = session ?? throw new ArgumentNullException(nameof(session)); }

        static string Key(Command c) => c switch
        {
            ChooseEventOption e => "event:" + e.EventId,
            SetPolicy p => "policy:" + p.Policy,
            Appoint a => "appoint:" + a.Office,
            MapAction m => FormattableString.Invariant($"map:{m.Q},{m.R}"),
            _ => c.Kind,
        };

        double Price(Command c) => AttentionSystem.IsCommandKind(c.Kind) ? AttentionSystem.Cost(Content, State, c.Kind) : 0;

        /// <summary>Attention left after paying for the queued orders at this turn's prices.</summary>
        public double AttentionLeft => State.Attention - queue.Sum(Price);

        /// <summary>The queued order with this key, or null.</summary>
        public Command? QueuedLike(Command c) => queue.FirstOrDefault(q => Key(q) == Key(c));

        /// <summary>Queues an order (replacing one with the same key). Returns null, or why it was refused here:
        /// the game is over, or the order costs more attention than is left.</summary>
        public string? Queue(Command c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (State.GameOver != null) return "The game is over";
            var old = QueuedLike(c);
            var left = AttentionLeft + (old != null ? Price(old) : 0);
            if (Price(c) > left + 1e-9)
                return FormattableString.Invariant($"Not enough attention (need {Price(c):0.0}, have {left:0.0})");
            if (old != null) queue[queue.IndexOf(old)] = c; else queue.Add(c);
            Changed?.Invoke();
            return null;
        }

        public void Withdraw(Command c)
        {
            if (queue.Remove(c)) Changed?.Invoke();
        }

        /// <summary>Runs the turn with the queued orders, clears the queue, and notifies.</summary>
        public StepResult EndTurn()
        {
            BeforeLast = Session.Last;
            var r = Session.Step(queue.ToList());
            queue.Clear();
            Changed?.Invoke();
            TurnEnded?.Invoke(r.Rejected);
            return r;
        }

        /// <summary>Switches to another session (a loaded save), dropping queued orders.</summary>
        public void Replace(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            BeforeLast = null;
            queue.Clear();
            Changed?.Invoke();
        }
    }
}
