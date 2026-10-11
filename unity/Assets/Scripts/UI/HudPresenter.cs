#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Fills the top bar from HudModel and runs End Turn.</summary>
    public sealed class HudPresenter : System.IDisposable
    {
        readonly VisualElement root; readonly SimBridge bridge; readonly UiText text;
        readonly Button endTurn; readonly System.Action onEndTurn;
        readonly VisualElement bar, hudRoot;

        /// <summary>Below this top-bar width (logical px) the meters drop their icons (Statecraft.uss .top-bar--compact): six
        /// meters with icons and their widest changes, the date badge, the screen buttons and End Turn need about this much: 16:9
        /// (1920 logical) keeps the icons, 16:10 (about 1821) does not (PromoHudTests.EveryWindowShapeFitsTheBarAfterATurn).</summary>
        public const float CompactBelowPx = 1900f;
        public const string CompactClass = "top-bar--compact";
        /// <summary>Below this width the HUD's root also gets NarrowClass: a taller bar whose meters stack the change under the
        /// value, and the panels below it move down to match (Statecraft.uss .hud-narrow). Without the icons the bar still
        /// cannot hold six values with their widest changes side by side below about 1900 px (PromoHudTests
        /// .EveryWindowShapeFitsTheBarAfterATurn), so the two tiers share the threshold.</summary>
        public const float NarrowBelowPx = CompactBelowPx;
        public const string NarrowClass = "hud-narrow";
        /// <summary>Below this width (windows narrower than about square, e.g. a browser snapped to half a 1080p screen, about
        /// 1360 logical px) one row cannot hold the badge, six meters, the screen buttons and End Turn: the root also gets
        /// TwoRowClass, and the bar wraps into two rows, the date and the meters above, the screen buttons and End Turn
        /// below (Statecraft.uss .hud-two-row).</summary>
        public const float TwoRowBelowPx = 1500f;
        public const string TwoRowClass = "hud-two-row";

        public HudPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            this.root = root; this.bridge = bridge; this.text = text;
            endTurn = root.Q<Button>("end-turn");
            onEndTurn = () => bridge.EndTurn();
            endTurn.clicked += onEndTurn;
            endTurn.generateVisualContent += PaintHourglass;
            bar = root.Q<VisualElement>("top-bar");
            hudRoot = root.Q<VisualElement>("root");
            bar.RegisterCallback<GeometryChangedEvent>(OnBarGeometry);
            SetCompact(bar.layout.width);   // the bar may already be laid out (the loading screen shows it) and never resize
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when it is disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render; endTurn.clicked -= onEndTurn; endTurn.generateVisualContent -= PaintHourglass;
            bar.UnregisterCallback<GeometryChangedEvent>(OnBarGeometry);
            bar.RemoveFromClassList(CompactClass); hudRoot.RemoveFromClassList(NarrowClass); hudRoot.RemoveFromClassList(TwoRowClass);
        }

        // The bar's width does not depend on the class, so this settles in one pass.
        void OnBarGeometry(GeometryChangedEvent e) => SetCompact(e.newRect.width);

        void SetCompact(float width)
        {
            if (!(width > 0)) return;   // NaN before layout: wait for it
            bar.EnableInClassList(CompactClass, width < CompactBelowPx);
            hudRoot.EnableInClassList(NarrowClass, width < NarrowBelowPx);
            hudRoot.EnableInClassList(TwoRowClass, width < TwoRowBelowPx);
        }

        /// <summary>End Turn's hourglass, in the button's left padding (Statecraft.uss .end-turn), painted into the button so it
        /// stays a leaf sized by its text (UiIcon.Paint).</summary>
        static void PaintHourglass(MeshGenerationContext m)
        {
            if (!m.visualElement.enabledInHierarchy) return;   // no bright sign on a disabled End Turn (game over)
            UiIcon.Paint(m, "hourglass", new UnityEngine.Rect(10f, (m.visualElement.layout.height - 22f) / 2f, 22f, 22f), UiIcon.IsFocused(m.visualElement));
        }

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
