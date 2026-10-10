#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Starting a session: continue the autosave when there is one (replayed to the same state hash), else a new
    /// Historical game. Anything the player should know (a content-change warning, an unreadable save) goes to notes.
    /// No failure while reading or replaying the autosave stops the game from starting: the file is set aside.</summary>
    public static class GameStart
    {
        public static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;

        public static GameSession Begin(Content c, SaveStore store, Func<ulong> newSeed, List<string> notes, bool fresh = false)
        {
            if (!fresh)
            {
                try
                {
                    var text = store.ReadAutosave();
                    if (text == null) return GameSession.New(c, newSeed(), Mode.Historical);
                    var loaded = GameSession.Load(c, text, out var warnings);
                    notes.AddRange(warnings);
                    return loaded;
                }
                catch (Exception e)   // any failure (a bad save, a replay that throws): never block the start
                {
                    try { store.SetAsideAutosave(); } catch (Exception) { /* even this must not stop the start */ }
                    notes.Add($"Your saved game could not be loaded ({e.Message}). A new game has started.");
                }
            }
            return GameSession.New(c, newSeed(), Mode.Historical);
        }

        /// <summary>Writes the save after every turn. A write that fails (a full or read-only disk) never stops the game:
        /// the reason goes to <paramref name="onError"/>. Dispose the result to stop autosaving.</summary>
        public static IDisposable Autosave(SimBridge bridge, SaveStore store, Action<string>? onError = null)
        {
            void Save(IReadOnlyList<Rejection> _)
            {
                try { store.WriteAutosave(bridge.Session.Save.Write()); }
                catch (Exception e) { onError?.Invoke($"Your game could not be saved ({e.Message})."); }
            }
            bridge.TurnEnded += Save;
            return new Unsubscribe(() => bridge.TurnEnded -= Save);
        }

        sealed class Unsubscribe : IDisposable
        {
            readonly Action undo;
            public Unsubscribe(Action undo) { this.undo = undo; }
            public void Dispose() => undo();
        }
    }
}
