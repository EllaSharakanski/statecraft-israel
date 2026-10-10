#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel (bottom-left): the selected hex's land, height and the settlements it owns, what the player
    /// built there, and the two map actions (transit camp, housing project) with the reason when one is not possible
    /// (MapActions.Check). Tile yields are deliberately not shown (owner decision, M3 rulings 2).</summary>
    public sealed class InfoPanelPresenter : System.IDisposable
    {
        readonly VisualElement panel; readonly SimBridge bridge; readonly UiText text;
        readonly Button campButton, housingButton; readonly System.Action onCamp, onHousing;
        readonly IReadOnlyDictionary<string, string> settlementNames;

        public InfoPanelPresenter(VisualElement root, SimBridge bridge, UiText text, IReadOnlyDictionary<string, string> settlementNames)
        {
            panel = root.Q<VisualElement>("info-panel"); this.bridge = bridge; this.text = text;
            this.settlementNames = settlementNames;
            campButton = panel.Q<Button>("build-camp"); housingButton = panel.Q<Button>("build-housing");
            onCamp = () => Build(MapActions.TransitCamp); onHousing = () => Build(MapActions.Housing);
            campButton.clicked += onCamp; housingButton.clicked += onHousing;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the buttons (the owner calls this when disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; campButton.clicked -= onCamp; housingButton.clicked -= onHousing; }

        public (int q, int r)? Selected { get; private set; }

        public void Show(int q, int r) { Selected = (q, r); Render(); }

        void Build(string action)
        {
            if (Selected == null) return;
            // The bridge reports a refusal itself; the sim enforces legality and the per-turn cap when the turn runs.
            bridge.Queue(new MapAction(action, Selected.Value.q, Selected.Value.r));
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var camp = panel.Q<Button>("build-camp"); var housing = panel.Q<Button>("build-housing");
            if (Selected == null)
            {
                panel.Q<Label>("info-title").text = text["info.hint"];
                foreach (var name in new[] { "info-subtitle", "info-settlements", "info-placement", "info-reason" }) panel.Q<Label>(name).text = "";
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                return;
            }
            var (q, r) = Selected.Value;
            var hex = c.HexAt(q, r);
            var owned = c.SettlementHexes.Where(x => x.Q == q && x.R == r).Select(x => settlementNames.TryGetValue(x.Id, out var n) ? n : x.Id).ToList();
            panel.Q<Label>("info-title").text = owned.Count > 0 ? owned[0] : hex == null ? text["info.off_map"] : text["info.biome." + hex.Biome];
            panel.Q<Label>("info-subtitle").text = hex == null ? "" : text.Format("info.hex", text["info.biome." + hex.Biome], q, r, hex.ElevM);
            panel.Q<Label>("info-settlements").text = owned.Count > 0 ? text.Format("info.owned", string.Join(", ", owned)) : "";
            var here = MapActions.At(s, q, r);
            var queued = bridge.QueuedLike(new MapAction(MapActions.Housing, q, r)) as MapAction;
            panel.Q<Label>("info-placement").text = queued != null ? text.Format("info.queued", text["info." + queued.Action])
                : here != null ? text.Format("info.built", text["info." + here.Action], here.Date) : "";
            var campWhy = MapActions.Check(c, s, MapActions.TransitCamp, q, r);
            var housingWhy = MapActions.Check(c, s, MapActions.Housing, q, r);
            camp.style.display = DisplayStyle.Flex; housing.style.display = DisplayStyle.Flex;
            camp.SetEnabled(campWhy == null); housing.SetEnabled(housingWhy == null);
            camp.text = text.Format("info.build_camp", c.Rules.MapActions.TryGetValue(MapActions.TransitCamp, out var cr) ? cr.Cost : 0);
            housing.text = text.Format("info.build_housing", c.Rules.MapActions.TryGetValue(MapActions.Housing, out var hr) ? hr.Cost : 0);
            panel.Q<Label>("info-reason").text = string.Join("\n", new[] { campWhy, housingWhy }.Where(x => x != null).Distinct());   // the sim's reasons, each once
        }
    }
}
