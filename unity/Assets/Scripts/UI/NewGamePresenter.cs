#nullable enable
using System;
using System.Collections.Generic;
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
            bridge.TurnEnded += Disarm;
        }

        readonly Button button;

        void Disarm(IReadOnlyList<Rejection> _) => Armed = false;

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when disabled).</summary>
        public void Dispose() { button.clicked -= Press; bridge.TurnEnded -= Disarm; }

        public bool Armed { get; private set; }

        void Press()
        {
            if (!Armed) { Armed = true; toast.Show(text["newgame.confirm"]); return; }
            Armed = false;
            store.DeleteAutosave();
            bridge.Replace(GameSession.New(bridge.Content, seed(), Mode.Historical));
            toast.Clear();
        }
    }
}
