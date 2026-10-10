using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>One playthrough: the current state, the turn before it, the last step's report, and the save that
    /// records every turn. The Unity SimBridge wraps this; the CLI and tests can use it directly.</summary>
    public sealed class GameSession
    {
        public Content Content { get; }
        public GameState State { get; private set; }
        /// <summary>The state before the last step (null before the first), for "change since last turn".</summary>
        public GameState? Previous { get; private set; }
        /// <summary>The last step's result (null before the first): its Why and Rejected lists.</summary>
        public StepResult? Last { get; private set; }
        public SaveGame Save { get; }

        GameSession(Content c, GameState s, SaveGame save) { Content = c; State = s; Save = save; }

        public static GameSession New(Content c, ulong seed, Mode mode)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, seed, mode);
            return new GameSession(c, s, new SaveGame(c.Hash, seed, mode, s.Hash()));
        }

        /// <summary>Runs one turn and records it in the save.</summary>
        public StepResult Step(IReadOnlyList<Command> commands)
        {
            var r = Simulation.Step(Content, State, commands);
            Save.Record(commands, r.State);
            Previous = State; State = r.State; Last = r;
            return r;
        }

        /// <summary>Parses and replays a save. Throws SaveException when the text is malformed, or when the content is the
        /// same but the replay ends on a different hash (a determinism bug). Content made with other content loads with a
        /// warning (spec section 7), and a second warning if the replay then differs from the saved game.</summary>
        public static GameSession Load(Content c, string saveText, out IReadOnlyList<string> warnings)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var saved = SaveGame.Parse(saveText);
            var list = new List<string>();
            bool sameContent = saved.ContentHash == c.Hash;
            if (!sameContent) list.Add("This save was made with different game content; the replayed game may differ.");
            var session = New(c, saved.Seed, saved.Mode);
            foreach (var turn in saved.Turns) session.Step(turn);
            if (session.State.Hash() != saved.FinalHash)
            {
                if (sameContent) throw new SaveException(0, "The replay did not reproduce the saved game (determinism bug): please report it with the save file.");
                list.Add("The replayed game differs from the saved one.");
            }
            warnings = list;
            return session;
        }
    }
}
