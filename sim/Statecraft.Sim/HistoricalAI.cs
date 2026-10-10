using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Plays what really happened: picks each pending event's historical option (else its first), then sends
    /// reserves home down to the current security level's minimum call-up (the army demobilised after the 1949
    /// armistices). It sends no budget allocation, so the budget stays at rules.yaml budget_start.</summary>
    public static class HistoricalAI
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var list = s.PendingEvents
                .Select(id => c.Events.First(e => e.Id == id))
                .Select(e => (Command)new ChooseEventOption(e.Id, (e.Options.FirstOrDefault(o => o.Historical) ?? e.Options[0]).Id))
                .ToList();
            var floor = c.Rules.RungMinMobilization[Math.Max(1, Math.Min(5, s.Rung)) - 1];
            if (s.Mobilization > floor) list.Add(new SetMobilization(floor));
            return list;
        }
    }
}
