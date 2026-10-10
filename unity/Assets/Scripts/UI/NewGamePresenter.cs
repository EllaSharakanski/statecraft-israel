#nullable enable
using System;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>New Game asks for a second press (no browser confirm dialogs in WebGL), then deletes the autosave and
    /// starts a new Historical game. Ending a turn in between cancels the request.</summary>
    public sealed class NewGamePresenter : IDisposable
    {
        readonly VisualElement root; readonly SimBridge bridge; readonly SaveStore store; readonly ToastPresenter toast; readonly UiText text; readonly Func<ulong> seed;

        public NewGamePresenter(VisualElement root, SimBridge bridge, SaveStore store, ToastPresenter toast, UiText text, Func<ulong> seed)
        {
            this.root = root; this.bridge = bridge; this.store = store; this.toast = toast; this.text = text; this.seed = seed;
            button = root.Q<Button>("new-game");
            button.clicked += Press;
            bridge.Changed += Disarm;
        }

        readonly Button button;

        // Any change (an order, a turn, a new session) means the prompt no longer describes what is on screen.
        void Disarm() => Armed = false;

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when disabled).</summary>
        public void Dispose() { button.clicked -= Press; bridge.Changed -= Disarm; }

        public bool Armed { get; private set; }

        void Press()
        {
            if (!Armed) { Armed = true; toast.Show(text["newgame.confirm"]); return; }
            Armed = false;
            bool deleted = true;
            // An old autosave that cannot be deleted is overwritten by the next turn's autosave anyway, so the new game still starts.
            try { store.DeleteAutosave(); } catch (Exception e) { deleted = false; UnityEngine.Debug.LogWarning("Autosave could not be deleted: " + e); }
            bridge.Replace(GameSession.New(bridge.Content, seed(), Mode.Historical));
            if (deleted) toast.Clear(); else toast.Show(text["newgame.delete_failed"]);
        }
    }
}
