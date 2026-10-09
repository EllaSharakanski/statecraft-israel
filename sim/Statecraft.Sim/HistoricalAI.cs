using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Plays what really happened: picks each pending event's historical option (else its first).
    /// It sends no budget allocation, so the budget stays at rules.yaml budget_start.</summary>
    public static class HistoricalAI
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s) =>
            s.PendingEvents
                .Select(id => c.Events.First(e => e.Id == id))
                .Select(e => (Command)new ChooseEventOption(e.Id, (e.Options.FirstOrDefault(o => o.Historical) ?? e.Options[0]).Id))
                .ToList();
    }
}
