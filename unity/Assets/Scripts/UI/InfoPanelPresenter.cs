#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel (bottom-left): the selected hex's land, height and the settlements it owns, what the player
    /// built there, and the two map actions (transit camp, housing project) with the reason when one is not possible
    /// (MapActions.Check). Tile yields are deliberately not shown (owner decision, M3 rulings 2). A selected unit
    /// (<see cref="ShowUnit"/>) shows its read-only card instead (<see cref="UnitCard"/>): no orders, only a button to the
    /// Defence screen (owner ruling, 2026-10-11).</summary>
    public sealed class InfoPanelPresenter : System.IDisposable
    {
        readonly VisualElement panel; readonly SimBridge bridge; readonly UiText text;
        readonly Button campButton, housingButton, defenceButton; readonly System.Action onCamp, onHousing, onDefence;
        readonly System.Func<PlacedToken?> selectedUnit;
        bool unitMode;
        readonly Label title, subtitle, region, settlements, placement, reason;
        readonly UiIcon thumb;
        readonly IReadOnlyDictionary<string, string> settlementNames;
        readonly Subdistricts1948? subdistricts;

        /// <param name="nav">Opens the Defence screen from a unit's card (null: the button does nothing).</param>
        /// <param name="selectedUnit">The selected unit's token now (null: none), read at every render so the card follows
        /// the state.</param>
        /// <param name="subdistricts">The 1948 sub-districts the map outlines; the panel names the selected hex's (null: no
        /// region line).</param>
        public InfoPanelPresenter(VisualElement root, SimBridge bridge, UiText text, IReadOnlyDictionary<string, string> settlementNames,
            ScreenNav? nav = null, System.Func<PlacedToken?>? selectedUnit = null, Subdistricts1948? subdistricts = null)
        {
            panel = root.Q<VisualElement>("info-panel"); this.bridge = bridge; this.text = text;
            this.settlementNames = settlementNames; this.subdistricts = subdistricts;
            title = panel.Q<Label>("info-title"); subtitle = panel.Q<Label>("info-subtitle"); region = panel.Q<Label>("info-region"); settlements = panel.Q<Label>("info-settlements");
            placement = panel.Q<Label>("info-placement"); reason = panel.Q<Label>("info-reason");
            thumb = panel.Q<UiIcon>("info-thumb");
            campButton = panel.Q<Button>("build-camp"); housingButton = panel.Q<Button>("build-housing");
            onCamp = () => Build(MapActions.TransitCamp); onHousing = () => Build(MapActions.Housing);
            campButton.clicked += onCamp; housingButton.clicked += onHousing;
            defenceButton = panel.Q<Button>("info-defence");
            this.selectedUnit = selectedUnit ?? (() => null);
            onDefence = () => nav?.Open("defence");
            defenceButton.clicked += onDefence;
            defenceButton.text = text["unit.defence"];
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the buttons (the owner calls this when disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; campButton.clicked -= onCamp; housingButton.clicked -= onHousing; defenceButton.clicked -= onDefence; }

        public (int q, int r)? Selected { get; private set; }

        /// <summary>True while the panel shows a unit's card.</summary>
        public bool ShowsUnit => unitMode && selectedUnit() != null;

        /// <summary>True while a screen covers the panel (a wide screen, or any screen in the two-row HUD): a map click
        /// then changes neither the hex nor the unit shown (#210; units-in-play merge note).</summary>
        public bool Covered
        {
            get
            {
                var hud = panel.parent;
                return hud != null && (hud.ClassListContains(ScreenNav.WideOpenClass)
                    || (hud.ClassListContains(ScreenNav.ScreenOpenClass) && hud.ClassListContains(HudPresenter.TwoRowClass)));
            }
        }

        /// <summary>Shows the hex; ignored while a screen covers the panel, so a map click cannot change a panel the player
        /// cannot see.</summary>
        public void Show(int q, int r)
        {
            if (Covered) return;   // covered (#210)
            unitMode = false; Selected = (q, r); Render();
        }

        /// <summary>Shows the selected unit's card (the unit comes from the selectedUnit callback); ignored while covered.</summary>
        public void ShowUnit()
        {
            if (Covered) return;
            unitMode = true; Selected = null; Render();
        }

        void Build(string action)
        {
            if (Selected == null) return;
            // The bridge reports a refusal itself; the sim enforces legality and the per-turn cap when the turn runs.
            bridge.Queue(new MapAction(action, Selected.Value.q, Selected.Value.r));
        }

        /// <summary>The terrain sign: the biome's glyph on the biome's own map colour (MapBuilder.BiomeColours), hidden when no
        /// hex (or no hex on the map) is selected.</summary>
        void ShowThumb(string? biome)
        {
            var known = biome != null && MapBuilder.BiomeColours.ContainsKey(biome);
            thumb.style.display = known ? DisplayStyle.Flex : DisplayStyle.None;
            thumb.Glyph = known ? "biome-" + biome : "";
            thumb.style.backgroundColor = known ? new StyleColor(MapBuilder.BiomeColours[biome!]) : new StyleColor(StyleKeyword.Null);
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var camp = campButton; var housing = housingButton;
            var unit = unitMode ? selectedUnit() : null;
            defenceButton.style.display = unit != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (unit != null)
            {
                var card = UnitCard.For(unit.Value, s, text);
                panel.EnableInClassList("info-empty", false);
                title.text = card.Title; subtitle.text = card.Subtitle; settlements.text = card.Body;
                region.text = "";   // a unit's card names no sub-district (the last hex's would stay under it)
                placement.text = card.Footer; reason.text = card.Note;
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                ShowThumb(null);
                return;
            }
            panel.EnableInClassList("info-empty", Selected == null);
            if (Selected == null)
            {
                title.text = text["info.hint"];
                subtitle.text = region.text = settlements.text = placement.text = reason.text = "";
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                ShowThumb(null);
                return;
            }
            var (q, r) = Selected.Value;
            var hex = c.HexAt(q, r);
            ShowThumb(hex?.Biome);
            var owned = c.SettlementHexes.Where(x => x.Q == q && x.R == r).Select(x => settlementNames[x.Id]).ToList();   // strict: every settlement hex has a name (final review M4)
            title.text = owned.Count > 0 ? owned[0] : hex == null ? text["info.off_map"] : text["info.biome." + hex.Biome];
            subtitle.text = hex == null ? "" : text.Format("info.hex", text["info.biome." + hex.Biome], hex.ElevM);   // no debug coordinates in player text (playtest 2, P2-6)
            // The sub-district on 15 May 1948, and that its true line is drawn to the nearest hex edge (the owner's ruling).
            var sub = subdistricts != null && subdistricts.Shown ? subdistricts.SubdistrictAt(q, r) : null;   // only while the map shows the layer
            region.text = sub == null ? "" : text.Format("info.region", sub.Name, sub.District);
            settlements.text = owned.Count > 0 ? text.Format("info.owned", string.Join(", ", owned)) : "";
            var here = MapActions.At(s, q, r);
            var queued = bridge.QueuedLike(new MapAction(MapActions.Housing, q, r)) as MapAction;
            placement.text = queued != null ? text.Format("info.queued", PlayerText.Capitalised(PlayerText.MapAction(c, queued.Action)))
                : here != null ? text.Format("info.built", PlayerText.Capitalised(PlayerText.MapAction(c, here.Action)), PlayerText.Month(here.Date)) : "";
            var campWhy = MapActions.Check(c, s, MapActions.TransitCamp, q, r);
            var housingWhy = MapActions.Check(c, s, MapActions.Housing, q, r);
            camp.style.display = DisplayStyle.Flex; housing.style.display = DisplayStyle.Flex;
            camp.SetEnabled(campWhy == null); housing.SetEnabled(housingWhy == null);
            // with no rule for an action there is no cost to show (it is disabled with the sim's reason anyway)
            camp.text = c.Rules.MapActions.TryGetValue(MapActions.TransitCamp, out var cr) ? text.Format("info.build_camp", PlayerText.Money(c, "treasury", cr.Cost)) : text["info.build_camp_plain"];
            housing.text = c.Rules.MapActions.TryGetValue(MapActions.Housing, out var hr) ? text.Format("info.build_housing", PlayerText.Money(c, "treasury", hr.Cost)) : text["info.build_housing_plain"];
            reason.text = string.Join("\n", new[] { campWhy, housingWhy }.Where(x => x != null).Distinct());   // the sim's reasons, each once
        }
    }
}
