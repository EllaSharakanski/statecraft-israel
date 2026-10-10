#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Fills the top bar from HudModel and runs End Turn.</summary>
    public sealed class HudPresenter : System.IDisposable
    {
        readonly VisualElement root; readonly SimBridge bridge; readonly UiText text;
        readonly Button endTurn; readonly System.Action onEndTurn;

        public HudPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            this.root = root; this.bridge = bridge; this.text = text;
            endTurn = root.Q<Button>("end-turn");
            onEndTurn = () => bridge.EndTurn();
            endTurn.clicked += onEndTurn;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when it is disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; endTurn.clicked -= onEndTurn; }

        void Set(string name, string value) => root.Q<Label>(name).text = value;

        void Set(string meter, Meter m)
        {
            Set(meter + "-value", m.Value);
            var delta = root.Q<Label>(meter + "-delta");
            delta.text = m.Delta;
            delta.EnableInClassList("delta-up", m.Sign > 0);
            delta.EnableInClassList("delta-down", m.Sign < 0);
        }

        public void Render()
        {
            var m = HudModel.From(bridge, text);
            Set("year", m.Year); Set("turn", m.Turn); Set("date", m.Date); Set("attention", m.Attention);
            Set("security-level", m.SecurityLevel);
            Set("treasury", m.Treasury); Set("immigration", m.Immigration); Set("security", m.Security);
            Set("support", m.Support); Set("standing", m.Standing); Set("fx", m.Fx);
            root.Q<Button>("end-turn").SetEnabled(bridge.State.GameOver == null);
        }
    }
}
