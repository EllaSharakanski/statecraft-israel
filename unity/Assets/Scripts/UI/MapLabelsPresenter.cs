#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Settlement names as UI Toolkit pills (EB Garamond SemiBold on navy with a gold edge, Statecraft.uss
    /// ".map-label") in the click-through "map-labels" layer under the panels. Each pill sits in a zero-size absolute slot;
    /// Refresh (GameRoot.LateUpdate, after the camera has moved) moves each slot with an inline pixel translate to its
    /// marker's top as the camera sees it, LiftPx above it, so no layout pass runs per frame, and returns early when
    /// nothing it reads has changed (the camera's view-projection and rect, the panel's size, the layer's origin, the zoom,
    /// which panels are shown, the pills' sizes, and no GeometryChangedEvent since). A GeometryChangedEvent on the layer or
    /// on a panel re-places the pills at once, inside the layout pass and before the repaint. The mapping assumes the camera's view fills the panel,
    /// as it does in the game (both span the canvas), so DPR, render scale and the panel's scale cancel out.
    ///
    /// Declutter: pills are placed in priority order (city, then town and Arab town, then the rest, each in file order). A
    /// pill is hidden (visibility, so it keeps its size and the choice does not flicker) when its marker is behind the
    /// camera, off screen or under a panel (the top bar, the info panel, the minimap, the event card), when it is a minor
    /// place (village, kibbutz, moshav) beyond DeclutterDistanceM, or when no anchor fits. Its anchor class (USS) is tried
    /// first, then the others (lower right, lower left, lower centre, then centred below the marker), applied as a pixel
    /// shift on the slot's translate (no class change, so no style pass). An anchor fits when the pill lies wholly on
    /// screen and clear of every panel, does not overlap an already placed pill (its last laid-out size plus MarginPx), and is more than twice
    /// as far from every other visible marker's top as from its own, plus MarginPx, so no name reads as a neighbour's. A
    /// city whose anchors all crowd a neighbour keeps the first that is clear of panels and pills. A pill hidden in the
    /// last refresh needs HysteresisPx more room to come back, so it does not blink at a threshold. Refresh allocates
    /// nothing. The dense Tel Aviv - Lod cluster keeps M2's anchor classes and Ramla's nudge west. The anchors are read
    /// once, when the presenter is created (a later MapScene.Build needs a new presenter; GameRoot makes one per Begin).</summary>
    public sealed class MapLabelsPresenter : IDisposable
    {
        public const string ContainerName = "map-labels", SlotClass = "map-label-slot", LabelClass = "map-label";
        public const float LiftPx = 6f, MarginPx = 4f, DeclutterDistanceM = 150000f;
        /// <summary>Extra room a pill hidden in the last refresh needs before it is shown again (review M7).</summary>
        public const float HysteresisPx = 2f;
        /// <summary>The panels a pill never sits under (Game.uxml names).</summary>
        public static readonly string[] OccluderNames = { "top-bar", "info-panel", "minimap", "event-card" };

        const string Centre = "anchor-lower-centre", Left = "anchor-lower-left", Right = "anchor-lower-right", Below = "below";
        static readonly string[] Fallbacks = { Right, Left, Centre, Below };
        static readonly HashSet<string> MinorTypes = new HashSet<string> { "village", "kibbutz", "moshav" };
        static readonly Dictionary<string, string> AnchorClass = new Dictionary<string, string>
        {
            ["jaffa"] = Right, ["rishon_lezion"] = Right, ["lod"] = Left,
            ["petah_tikva"] = Left, ["safed"] = Right, ["rosh_pinna"] = Left,
        };
        static readonly Dictionary<string, float> OffsetEastM = new Dictionary<string, float> { ["ramla"] = -1500f };

        sealed class Item
        {
            public Settlement Settlement = null!; public Vector3 Anchor, MarkerTop, MarkerBase; public VisualElement Slot = null!;
            public Label Label = null!; public string AnchorClass = ""; public bool IsCity, TopVisible, AnchorInFront, ShownLast = true;
            public Vector2 TopPanel, AnchorPanel, BasePanel;
        }

        readonly VisualElement container;
        readonly Camera camera;
        readonly CameraRig? rig;
        readonly Func<float>? zoomDistance;
        readonly List<Item> items = new List<Item>();        // in placing (priority) order
        readonly List<Item> settlementOrder = new List<Item>();
        readonly List<VisualElement> occluders = new List<VisualElement>();
        readonly List<Rect> occluderRects = new List<Rect>();  // in the layer's coordinates
        readonly List<Rect> placed = new List<Rect>();
        Rect layerRect;                                        // the panel in the layer's coordinates: a pill stays inside it
        readonly EventCallback<GeometryChangedEvent> onOccluderGeometry, onContainerGeometry;
        Matrix4x4 lastViewProjection; Rect lastCameraRect; Vector2 lastPanelSize, lastOrigin; float lastZoom = float.NaN;
        int lastOccluderMask = -1; float lastLabelSizes = float.NaN; bool sizesKnown, geometryDirty = true;
        Action? removeToggle;   // the labels switch's removal token (VisualToggles.Register): a disposed presenter never removes a newer one

        public IReadOnlyList<Label> Labels { get; }
        public IReadOnlyList<VisualElement> Slots { get; }

        public MapLabelsPresenter(VisualElement root, MapScene map, Camera camera, Func<float>? zoomDistance = null)
        {
            container = root.Q<VisualElement>(ContainerName) ?? throw new InvalidOperationException($"Game.uxml has no '{ContainerName}' element");
            this.camera = camera;
            this.zoomDistance = zoomDistance;
            rig = camera.TryGetComponent(out CameraRig r) ? r : null;
            container.Clear();   // one layer, one owner: the newest presenter (GameRoot makes one per Begin)
            var labels = new List<Label>(); var slots = new List<VisualElement>();
            foreach (var s in map.Settlements)
            {
                var slot = new VisualElement { name = "slot-" + s.Id, pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                var anchorClass = AnchorClass.TryGetValue(s.Id, out var a) ? a : Centre;
                var label = new Label(s.Name) { name = "label-" + s.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass);
                label.AddToClassList("map-label-" + s.Type);
                label.AddToClassList(anchorClass);
                slot.Add(label);
                container.Add(slot);
                labels.Add(label); slots.Add(slot);
                var anchor = Anchor(map, s);
                var top = map.Frame.ToWorld(s.Lon, s.Lat, 0); top.y = anchor.y;
                settlementOrder.Add(new Item
                {
                    Settlement = s, Anchor = anchor, MarkerTop = top, MarkerBase = top - Vector3.up * SettlementPlacer.MarkerHeightM(s.Type),
                    Slot = slot, Label = label, AnchorClass = anchorClass, IsCity = s.Type == "city",
                });
            }
            // Stable sort: priority, then file order.
            for (int p = 0; p <= 2; p++)
                foreach (var it in settlementOrder) if (Priority(it.Settlement.Type) == p) items.Add(it);
            Labels = labels; Slots = slots;

            onOccluderGeometry = _ => { geometryDirty = true; Refresh(force: true); };   // a panel that appears hides the pills it covers before the repaint (re-review N1)
            onContainerGeometry = _ => { geometryDirty = true; Refresh(force: true); };   // a resize is placed before the repaint (review M4)
            foreach (var name in OccluderNames)
            {
                var e = root.Q<VisualElement>(name);
                if (e == null) continue;
                occluders.Add(e);
                e.RegisterCallback(onOccluderGeometry);
            }
            container.RegisterCallback(onContainerGeometry);
            // On clears the inline display, so the loading rule (".loading .map-labels") still wins (review M1).
            removeToggle = VisualToggles.Register("labels", on => container.style.display = on ? new StyleEnum<DisplayStyle>(StyleKeyword.Null) : DisplayStyle.None);
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
            Left => new Rect(at.x, at.y - size.y, size.x, size.y),
            Right => new Rect(at.x - size.x, at.y - size.y, size.x, size.y),
            _ => new Rect(at.x - size.x / 2f, at.y - size.y, size.x, size.y),
        };

        float Zoom() => zoomDistance != null ? zoomDistance() : rig != null ? rig.Distance : 0f;

        int OccluderMask()
        {
            int mask = 0;
            for (int i = 0; i < occluders.Count; i++)
            {
                var s = occluders[i].resolvedStyle;
                if (s.display != DisplayStyle.None && s.visibility == Visibility.Visible) mask |= 1 << i;
            }
            return mask;
        }

        public void Refresh(bool force = false)
        {
            var panel = container.panel;
            if (panel == null || camera == null) return;
            var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            var cameraRect = camera.rect;
            var screen = panel.visualTree.layout.size;    // the whole panel, which the camera's view fills
            var origin = container.worldBound.position;   // slots translate from the layer's top-left
            float distance = Zoom();
            int mask = OccluderMask();
            float labelSizes = 0f;   // a pill that grows or shrinks (a late font, a new text) is re-placed (re-review N2)
            for (int i = 0; i < items.Count; i++) { var st = items[i].Label.resolvedStyle; if (!float.IsNaN(st.width) && !float.IsNaN(st.height)) labelSizes += st.width + st.height; }
            if (!force && sizesKnown && !geometryDirty && labelSizes == lastLabelSizes && viewProjection == lastViewProjection && cameraRect == lastCameraRect
                && screen == lastPanelSize && origin == lastOrigin && distance == lastZoom && mask == lastOccluderMask) return;
            lastViewProjection = viewProjection; lastCameraRect = cameraRect; lastPanelSize = screen; lastOrigin = origin;
            lastZoom = distance; lastOccluderMask = mask; lastLabelSizes = labelSizes; geometryDirty = false;
            sizesKnown = true;
            bool laidOut = !(float.IsNaN(screen.x) || screen.x <= 0f || float.IsNaN(origin.x) || float.IsNaN(origin.y));
            if (!laidOut) { sizesKnown = false; origin = Vector2.zero; }   // before the first layout: cull by view only, place later

            layerRect = new Rect(-origin, screen);
            occluderRects.Clear();
            if (laidOut)
                for (int i = 0; i < occluders.Count; i++)
                    if ((mask & (1 << i)) != 0)
                    {
                        var b = occluders[i].worldBound;
                        if (!float.IsNaN(b.x) && b.width > 0f) occluderRects.Add(new Rect(b.position - origin, b.size));
                    }

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var t = camera.WorldToViewportPoint(it.MarkerTop);
                it.TopPanel = ToPanel(t, cameraRect, screen, origin);
                it.TopVisible = t.z > 0f && t.x >= 0f && t.x <= 1f && t.y >= 0f && t.y <= 1f && !UnderAPanel(it.TopPanel);   // a marker under a panel is no one's neighbour
                var v = camera.WorldToViewportPoint(it.Anchor);
                it.AnchorInFront = OnScreen(v);
                it.AnchorPanel = ToPanel(v, cameraRect, screen, origin);
                it.BasePanel = ToPanel(camera.WorldToViewportPoint(it.MarkerBase), cameraRect, screen, origin);
            }

            placed.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                bool shown = it.AnchorInFront && it.TopVisible && !HiddenByZoom(it.Settlement.Type, distance);
                var at = new Vector2(it.AnchorPanel.x, it.AnchorPanel.y - LiftPx);
                var shift = Vector2.zero;
                if (shown)
                {
                    var size = new Vector2(it.Label.resolvedStyle.width, it.Label.resolvedStyle.height);
                    if (!laidOut || float.IsNaN(size.x) || size.x <= 0f) sizesKnown = false;   // not laid out yet: show it, place on a later refresh
                    else
                    {
                        float extra = it.ShownLast ? 0f : HysteresisPx;
                        var home = PillRect(at, size, it.AnchorClass);
                        bool found = TryPlace(it, at, size, home, it.AnchorClass, extra, true, ref shift);
                        for (int k = 0; !found && k < Fallbacks.Length; k++)
                            if (Fallbacks[k] != it.AnchorClass) found = TryPlace(it, at, size, home, Fallbacks[k], extra, true, ref shift);
                        if (!found && it.IsCity)
                        {
                            found = TryPlace(it, at, size, home, it.AnchorClass, extra, false, ref shift);
                            for (int k = 0; !found && k < Fallbacks.Length; k++)
                                if (Fallbacks[k] != it.AnchorClass) found = TryPlace(it, at, size, home, Fallbacks[k], extra, false, ref shift);
                        }
                        shown = found;
                    }
                }
                it.ShownLast = shown;
                it.Slot.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
                if (shown && laidOut) it.Slot.style.translate = new Translate(at.x + shift.x, at.y + shift.y);
            }
        }

        /// <summary>Whether the pill fits with <paramref name="anchor"/>; if so records it as placed and sets the shift from
        /// its USS anchor's rectangle <paramref name="home"/>.</summary>
        bool TryPlace(Item it, Vector2 at, Vector2 size, Rect home, string anchor, float extra, bool clearOfNeighbours, ref Vector2 shift)
        {
            var r = anchor == Below
                ? new Rect(it.BasePanel.x - size.x / 2f, it.BasePanel.y + LiftPx, size.x, size.y)
                : PillRect(at, size, anchor);
            float m = MarginPx + extra;
            var grown = new Rect(r.x - m, r.y - m, r.width + 2f * m, r.height + 2f * m);
            if (r.xMin < layerRect.xMin || r.yMin < layerRect.yMin || r.xMax > layerRect.xMax || r.yMax > layerRect.yMax) return false;
            for (int i = 0; i < occluderRects.Count; i++) if (occluderRects[i].Overlaps(r)) return false;
            for (int i = 0; i < placed.Count; i++) if (placed[i].Overlaps(grown)) return false;
            if (clearOfNeighbours)
            {
                float need = 2f * Gap(r, it.TopPanel) + MarginPx + extra;
                for (int i = 0; i < items.Count; i++)
                {
                    var other = items[i];
                    if (other != it && other.TopVisible && Gap(r, other.TopPanel) <= need) return false;
                }
            }
            placed.Add(r);
            shift = r.position - home.position;
            return true;
        }

        bool UnderAPanel(Vector2 p)
        {
            for (int i = 0; i < occluderRects.Count; i++) if (occluderRects[i].Contains(p)) return true;
            return false;
        }

        static bool OnScreen(Vector3 v) => v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;

        /// <summary>Viewport to the layer's coordinates through the camera's own rect, not
        /// RuntimePanelUtils.CameraTransformWorldToPanel: that flips y with Screen.height, which is wrong whenever the camera
        /// draws to a texture of another size (edit-mode tests, captures).</summary>
        static Vector2 ToPanel(Vector3 v, Rect cameraRect, Vector2 screen, Vector2 origin) =>
            new Vector2((cameraRect.x + v.x * cameraRect.width) * screen.x, (1f - (cameraRect.y + v.y * cameraRect.height)) * screen.y) - origin;

        static float Gap(Rect r, Vector2 p) =>
            new Vector2(Mathf.Max(0f, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0f, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;

        /// <summary>Removes only this presenter's own pills and callbacks, so an older presenter disposed after a newer one
        /// was made leaves the newer one's pills (review M2).</summary>
        public void Dispose()
        {
            removeToggle?.Invoke(); removeToggle = null;
            foreach (var e in occluders) e.UnregisterCallback(onOccluderGeometry);
            container.UnregisterCallback(onContainerGeometry);
            occluders.Clear();
            foreach (var slot in Slots) slot.RemoveFromHierarchy();
            items.Clear(); settlementOrder.Clear();
        }
    }
}
