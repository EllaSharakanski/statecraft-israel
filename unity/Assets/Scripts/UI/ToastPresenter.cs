#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A short message under the top bar: why orders were refused (spec section 7: the UI shows the stated
    /// reason), or a note such as a save warning. Hidden when there is nothing to say; cleared by the next order.</summary>
    public sealed class ToastPresenter : System.IDisposable
    {
        readonly Label label; readonly UiText text; readonly SimBridge bridge;
        readonly System.Action<IReadOnlyList<Statecraft.Sim.Rejection>> onTurnEnded;

        public ToastPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            label = root.Q<Label>("toast"); this.text = text; this.bridge = bridge;
            onTurnEnded = r => Show(r.Select(x => text.Format("toast.refused", x.Reason)));
            bridge.TurnEnded += onTurnEnded;
            bridge.Refused += Refuse;
            bridge.Changed += Clear;
            Clear();
        }

        public void Dispose() { bridge.TurnEnded -= onTurnEnded; bridge.Refused -= Refuse; bridge.Changed -= Clear; }

        /// <summary>Shows the lines as they are (none = hide).</summary>
        public void Show(IEnumerable<string> lines)
        {
            var list = lines.ToList();
            label.text = string.Join("\n", list);
            label.style.display = list.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show(string line) => Show(new[] { line });

        /// <summary>Shows why an order was refused ("Not done: ...").</summary>
        public void Refuse(string reason) => Show(text.Format("toast.refused", reason));

        public void Clear() => Show(new string[0]);
    }
}
