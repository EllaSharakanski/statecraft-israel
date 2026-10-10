#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Settlement names as UI Toolkit pills (EB Garamond SemiBold on navy with a gold edge, Statecraft.uss
    /// ".map-label") in the click-through "map-labels" layer under the panels. Each pill sits in a zero-size absolute slot;
    /// Refresh (GameRoot.LateUpdate, after the camera has moved) moves each slot with an inline pixel translate to its
    /// marker's top as the camera sees it, LiftPx above it, so no layout pass runs per frame, and returns early when the
    /// camera's view-projection, the panel size and the zoom are unchanged. Pills are placed in priority order (city, then
    /// town and Arab town, then the rest, each in file order); a pill behind the camera or off screen, a minor place
    /// (village, kibbutz, moshav) beyond DeclutterDistanceM, or a pill that would overlap an already placed one (its last
    /// laid-out size plus MarginPx) is hidden with visibility, so it keeps its size and the choice does not flicker. A pill
    /// other than a city's is also hidden when it would come within NeighbourClearPx of another settlement's marker top, so
    /// no shown name reads as a neighbour's (the dense Tel Aviv - Lod cluster at the overview); cities always keep theirs.
    /// Screen-space, so the tilt-shift never blurs them. The dense Tel Aviv - Lod cluster keeps M2's anchors and Ramla's
    /// nudge west. The anchors are read once, when the presenter is created (a later MapScene.Build needs a new presenter;
    /// GameRoot makes one per Begin).</summary>
    public sealed class MapLabelsPresenter : IDisposable
    {
        public const string ContainerName = "map-labels", SlotClass = "map-label-slot", LabelClass = "map-label";
        public const float LiftPx = 6f, MarginPx = 4f, DeclutterDistanceM = 150000f;
        /// <summary>A pill's least gap to any other marker's top: twice its own gap (LiftPx) plus MarginPx.</summary>
        public const float NeighbourClearPx = 2f * LiftPx + MarginPx;

        static readonly HashSet<string> MinorTypes = new HashSet<string> { "village", "kibbutz", "moshav" };
        static readonly Dictionary<string, string> AnchorClass = new Dictionary<string, string>
        {
            ["jaffa"] = "anchor-lower-right", ["rishon_lezion"] = "anchor-lower-right", ["lod"] = "anchor-lower-left",
            ["petah_tikva"] = "anchor-lower-left", ["safed"] = "anchor-lower-right", ["rosh_pinna"] = "anchor-lower-left",
        };
        static readonly Dictionary<string, float> OffsetEastM = new Dictionary<string, float> { ["ramla"] = -1500f };

        sealed class Item
        {
            public Settlement Settlement = null!; public Vector3 Anchor, MarkerTop; public VisualElement Slot = null!; public Label Label = null!;
            public string AnchorClass = ""; public bool TopOnScreen; public Vector2 TopPanel;
        }

        readonly VisualElement container;
        readonly Camera camera;
        readonly Func<float> zoom;
        readonly List<Item> items = new List<Item>();   // in placing (priority) order
        Matrix4x4 lastViewProjection; Vector2 lastPanelSize; float lastZoom = float.NaN; bool sizesKnown;

        public IReadOnlyList<Label> Labels { get; }
        public IReadOnlyList<VisualElement> Slots { get; }

        public MapLabelsPresenter(VisualElement root, MapScene map, Camera camera, Func<float>? zoomDistance = null)
        {
            container = root.Q<VisualElement>(ContainerName) ?? throw new InvalidOperationException($"Game.uxml has no '{ContainerName}' element");
            this.camera = camera;
            zoom = zoomDistance ?? (() => camera.TryGetComponent(out CameraRig rig) ? rig.Distance : 0f);
            container.Clear();
            var labels = new List<Label>(); var slots = new List<VisualElement>();
            foreach (var s in map.Settlements)
            {
                var slot = new VisualElement { name = "slot-" + s.Id, pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                var anchorClass = AnchorClass.TryGetValue(s.Id, out var a) ? a : "anchor-lower-centre";
                var label = new Label(s.Name) { name = "label-" + s.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass);
                label.AddToClassList("map-label-" + s.Type);
                label.AddToClassList(anchorClass);
                slot.Add(label);
                container.Add(slot);
                labels.Add(label); slots.Add(slot);
                var anchor = Anchor(map, s); var top = map.Frame.ToWorld(s.Lon, s.Lat, 0); top.y = anchor.y;
                items.Add(new Item { Settlement = s, Anchor = anchor, MarkerTop = top, Slot = slot, Label = label, AnchorClass = anchorClass });
            }
            // Stable sort: priority, then file order.
            var ordered = items.Select((it, i) => (it, i)).OrderBy(x => Priority(x.it.Settlement.Type)).ThenBy(x => x.i).Select(x => x.it).ToList();
            items.Clear(); items.AddRange(ordered);
            Labels = labels; Slots = slots;
            removeToggle?.Invoke();
            removeToggle = VisualToggles.Register("labels", on => container.style.display = on ? DisplayStyle.Flex : DisplayStyle.None);
            Refresh(force: true);
        }

        /// <summary>The world point a settlement's label hangs from: its marker's top, nudged east or west for a few crowded names.</summary>
        public static Vector3 Anchor(MapScene map, Settlement s)
        {
            var p = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            p.x += OffsetEastM.TryGetValue(s.Id, out var dx) ? dx : 0f;
            p.y = map.Terrain.SampleHeight(p) + map.Terrain.transform.position.y + SettlementPlacer.MarkerHeightM(s.Type);
            return p;
        }

        /// <summary>Placing order: lower first. Cities, then towns and Arab towns, then villages, kibbutzim and moshavim.</summary>
        public static int Priority(string type) => type == "city" ? 0 : type == "town" || type == "arab_town" ? 1 : 2;

        public static bool HiddenByZoom(string type, float distance) => MinorTypes.Contains(type) && distance > DeclutterDistanceM;

        /// <summary>The pill's panel rectangle when its anchor point is <paramref name="at"/> (the anchor class says which corner
        /// of the pill sits there, USS translate).</summary>
        public static Rect PillRect(Vector2 at, Vector2 size, string anchorClass) => anchorClass switch
        {
            "anchor-lower-left" => new Rect(at.x, at.y - size.y, size.x, size.y),
            "anchor-lower-right" => new Rect(at.x - size.x, at.y - size.y, size.x, size.y),
            _ => new Rect(at.x - size.x / 2f, at.y - size.y, size.x, size.y),
        };

        public void Refresh(bool force = false)
        {
            var panel = container.panel;
            if (panel == null || camera == null) return;
            var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            var panelSize = container.layout.size;
            float distance = zoom();
            if (!force && sizesKnown && viewProjection == lastViewProjection && panelSize == lastPanelSize && distance == lastZoom) return;
            lastViewProjection = viewProjection; lastPanelSize = panelSize; lastZoom = distance;
            sizesKnown = true;
            var screen = panel.visualTree.layout.size;                    // the whole panel, which the camera's view fills
            var origin = container.worldBound.position;                   // slots translate from the layer's top-left
            bool laidOut = !(float.IsNaN(screen.x) || screen.x <= 0f || float.IsNaN(origin.x) || float.IsNaN(origin.y));
            if (!laidOut) { sizesKnown = false; origin = Vector2.zero; }   // before the first layout: cull by view only, place later
            var r0 = camera.rect;
            Vector2 ToPanel(Vector3 v) => new Vector2((r0.x + v.x * r0.width) * screen.x, (1f - (r0.y + v.y * r0.height)) * screen.y) - origin;
            foreach (var it in items)
            {
                var t = camera.WorldToViewportPoint(it.MarkerTop);
                it.TopOnScreen = t.z > 0f && t.x > -0.05f && t.x < 1.05f && t.y > -0.05f && t.y < 1.05f;
                it.TopPanel = ToPanel(t);
            }
            var placed = new List<Rect>();
            foreach (var it in items)
            {
                var v = camera.WorldToViewportPoint(it.Anchor);
                bool shown = v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f && !HiddenByZoom(it.Settlement.Type, distance);
                var at = Vector2.zero;
                if (shown)
                {
                    // Viewport to panel through the camera's own rect, not RuntimePanelUtils.CameraTransformWorldToPanel: that
                    // flips y with Screen.height, which is wrong whenever the camera draws to a texture (edit-mode tests,
                    // captures) and the screen is a different size.
                    at = ToPanel(v);
                    at.y -= LiftPx;
                    var size = new Vector2(it.Label.resolvedStyle.width, it.Label.resolvedStyle.height);
                    if (!laidOut || float.IsNaN(size.x) || size.x <= 0f) sizesKnown = false;   // not laid out yet: show it, cull on a later refresh
                    else
                    {
                        var r = PillRect(at, size, it.AnchorClass);
                        var grown = new Rect(r.x - MarginPx, r.y - MarginPx, r.width + 2f * MarginPx, r.height + 2f * MarginPx);
                        if (placed.Any(p => p.Overlaps(grown)) || it.Settlement.Type != "city" && CrowdsANeighbour(it, r)) shown = false;
                        else placed.Add(r);
                    }
                }
                it.Slot.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
                if (shown && laidOut) it.Slot.style.translate = new Translate(at.x, at.y);
            }
        }

        bool CrowdsANeighbour(Item it, Rect r)
        {
            foreach (var other in items)
                if (other != it && other.TopOnScreen && Gap(r, other.TopPanel) <= NeighbourClearPx) return true;
            return false;
        }

        static float Gap(Rect r, Vector2 p) =>
            new Vector2(Mathf.Max(0f, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0f, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;

        Action? removeToggle;   // the labels switch's removal token (VisualToggles.Register): a disposed presenter never removes a newer one

        public void Dispose() { removeToggle?.Invoke(); removeToggle = null; container.Clear(); items.Clear(); }
    }
}
