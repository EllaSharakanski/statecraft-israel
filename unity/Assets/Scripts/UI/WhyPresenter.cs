#nullable enable
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>"Why did this happen?" (spec section 4, step 6): every cause the last turn reported, grouped by the system
    /// that produced it, in a fixed order (what was decided first, then its consequences).</summary>
    public sealed class WhyPresenter : System.IDisposable
    {
        public static readonly string[] Order = { "events", "politics", "cabinet", "security", "economy", "population", "map", "policy", "effects", "situations" };
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;

        public WhyPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-why"); this.bridge = bridge; this.text = text;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge (the owner calls this when disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; }

        public void Render()
        {
            var rows = screen.Q<ScrollView>("why-rows");
            rows.Clear();
            var last = bridge.Session.Last; var prev = bridge.Session.Previous;
            screen.Q<Label>("why-turn").text = last == null || prev == null ? "" : text.Format("why.turn", prev.Date, bridge.State.Date);
            if (last == null) { rows.Add(new Label(text["why.empty"]) { name = "why-empty" }); return; }
            int i = 0;
            foreach (var system in Order.Concat(last.Why.Select(w => w.System).Distinct().Where(sys => !Order.Contains(sys))))
            {
                var lines = last.Why.Where(w => w.System == system).ToList();
                if (lines.Count == 0) continue;
                var header = new Label(Order.Contains(system) ? text["why." + system] : system) { name = "why-group-" + system };
                header.AddToClassList("why-group");
                rows.Add(header);
                foreach (var w in lines)
                {
                    var line = new Label(w.Message) { name = "why-" + i++ };
                    line.AddToClassList("why-line");
                    rows.Add(line);
                }
            }
        }
    }
}
