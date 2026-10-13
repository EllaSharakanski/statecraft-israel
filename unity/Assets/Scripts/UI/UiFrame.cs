#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The promo's panel trim (promo/final/03 and 05), drawn with Painter2D over a panel: a soft shadow outside its
    /// edge, a thin gold rule inset inside the border, and a gold corner bracket with a small diamond at each corner; with
    /// <see cref="Filigree"/>, also a second rule and a scroll at each corner (HUD round 1, #398). It is
    /// an absolute overlay that fills its parent (Statecraft.uss ".ui-frame"), takes no clicks and lays nothing out, so a
    /// panel's layout is the same with or without it. Inside a panel that clips (overflow: hidden) the shadow is clipped
    /// away and only the trim shows. In UXML: <c>&lt;Statecraft.Game.UI.UiFrame /&gt;</c> as a panel's last child, so the trim is drawn over the content (the
    /// top bar's shadow-only frame is its first child: it draws nothing inside).</summary>
    [UxmlElement]
    public partial class UiFrame : VisualElement
    {
        public const string ClassName = "ui-frame";
        static readonly Color Gold = new Color32(196, 160, 92, 255);

        /// <summary>False drops the corner brackets (a slim bar keeps only the rule and the shadow); UXML "corners".</summary>
        [UxmlAttribute] public bool Corners { get => corners; set { corners = value; MarkDirtyRepaint(); } }
        /// <summary>False drops the outer shadow; UXML "shadow".</summary>
        [UxmlAttribute] public bool Shadow { get => shadow; set { shadow = value; MarkDirtyRepaint(); } }
        /// <summary>False drops the inset gold rule; UXML "rule".</summary>
        [UxmlAttribute] public bool Rule { get => rule; set { rule = value; MarkDirtyRepaint(); } }
        /// <summary>True adds the promo's filigree: a second inset rule and a scroll at each corner; UXML "filigree".</summary>
        [UxmlAttribute] public bool Filigree { get => filigree; set { filigree = value; MarkDirtyRepaint(); } }
        bool corners = true, shadow = true, rule = true, filigree;

        public UiFrame()
        {
            AddToClassList(ClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = new Rect(0, 0, layout.width, layout.height);
            if (!(r.width > 8) || !(r.height > 8)) return;
            var p = mgc.painter2D;
            p.lineJoin = LineJoin.Miter;
            if (shadow)
            {
                // Rings outside the parent's 1 px border, fading out over 12 px and weighted downwards (light from above).
                for (int i = 1; i <= 12; i++)
                {
                    var t = 1f - i / 13f;
                    p.strokeColor = new Color(0f, 0f, 0f, 0.20f * t * t);
                    p.lineWidth = 1.05f;
                    var g = 1f + i;
                    StrokeRect(p, new Rect(r.x - g, r.y - g + i * 0.25f, r.width + 2 * g, r.height + 2 * g + i * 0.25f));
                }
            }
            // The inner rule.
            const float inset = 4f;
            if (rule)
            {
                p.strokeColor = new Color(Gold.r, Gold.g, Gold.b, 0.30f);
                p.lineWidth = 1f;
                StrokeRect(p, new Rect(r.x + inset, r.y + inset, r.width - 2 * inset, r.height - 2 * inset));
                // The promo's double rule: a fainter second line just inside the first.
                if (filigree)
                {
                    const float inner = inset + FiligreeGap;
                    p.strokeColor = new Color(Gold.r, Gold.g, Gold.b, 0.16f);
                    StrokeRect(p, new Rect(r.x + inner, r.y + inner, r.width - 2 * inner, r.height - 2 * inner));
                }
            }
            if (!corners) return;
            // Corner brackets over the rule, with a diamond on each corner.
            float leg = Mathf.Min(16f, Mathf.Min(r.width, r.height) / 4f), d = 2.6f;
            p.strokeColor = Gold; p.lineWidth = 1.6f; p.fillColor = Gold;
            foreach (var (cx, cy, sx, sy) in new[] { (r.xMin + inset, r.yMin + inset, 1f, 1f), (r.xMax - inset, r.yMin + inset, -1f, 1f),
                                                     (r.xMin + inset, r.yMax - inset, 1f, -1f), (r.xMax - inset, r.yMax - inset, -1f, -1f) })
            {
                p.BeginPath(); p.MoveTo(new Vector2(cx + sx * leg, cy)); p.LineTo(new Vector2(cx, cy)); p.LineTo(new Vector2(cx, cy + sy * leg)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(cx + sx * 4f, cy + sy * 2.4f)); p.LineTo(new Vector2(cx + sx * 9f, cy + sy * 2.4f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(cx + sx * 2.4f, cy + sy * 4f)); p.LineTo(new Vector2(cx + sx * 2.4f, cy + sy * 9f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(cx, cy - d)); p.LineTo(new Vector2(cx + d, cy)); p.LineTo(new Vector2(cx, cy + d)); p.LineTo(new Vector2(cx - d, cy)); p.ClosePath(); p.Fill();
                if (filigree) Scroll(p, cx, cy, sx, sy, leg, r.height >= TallPanel);
            }
        }

        /// <summary>Below this height a panel (the date plate) gets no scroll down its sides: there the side scrolls would
        /// reach its lines of text.</summary>
        public const float TallPanel = 120f;
        /// <summary>The space between the rule and the filigree's second rule.</summary>
        public const float FiligreeGap = 3f;

        /// <summary>The promo's corner filigree (promo/final/03): from the end of each bracket leg a scroll curls back
        /// towards the panel's inside (down the sides only when <paramref name="down"/>), and a leaf lies on the corner's
        /// diagonal. Drawn in the corner's own frame (sx, sy point inwards) and scaled with the bracket's leg, so a small
        /// panel gets a small scroll. Painter2D only: no texture, and the mesh is rebuilt only when the frame repaints (a
        /// layout change), never per frame.</summary>
        static void Scroll(Painter2D p, float cx, float cy, float sx, float sy, float leg, bool down)
        {
            float f = leg / 16f;
            Vector2 P(float x, float y) => new Vector2(cx + sx * x * f, cy + sy * y * f);
            p.strokeColor = Gold; p.lineWidth = 1.1f; p.fillColor = Gold;
            // along the horizontal leg, then the same mirrored along the vertical one
            p.BeginPath(); p.MoveTo(P(16, 0)); p.BezierCurveTo(P(22, 0), P(26, 3.6f), P(23, 6.4f));
            p.BezierCurveTo(P(21, 8.2f), P(18.6f, 6.6f), P(20.2f, 4.6f)); p.Stroke();
            if (down)
            {
                p.BeginPath(); p.MoveTo(P(0, 16)); p.BezierCurveTo(P(0, 22), P(3.6f, 26), P(6.4f, 23));
                p.BezierCurveTo(P(8.2f, 21), P(6.6f, 18.6f), P(4.6f, 20.2f)); p.Stroke();
            }
            // the leaf on the diagonal, inside the bracket's ticks
            p.BeginPath(); p.MoveTo(P(5.2f, 5.2f)); p.BezierCurveTo(P(8.8f, 5.8f), P(10.6f, 7.6f), P(11.2f, 11.2f));
            p.BezierCurveTo(P(7.6f, 10.6f), P(5.8f, 8.8f), P(5.2f, 5.2f)); p.ClosePath(); p.Fill();
        }

        static void StrokeRect(Painter2D p, Rect r)
        {
            p.BeginPath(); p.MoveTo(new Vector2(r.xMin, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMax));
            p.LineTo(new Vector2(r.xMin, r.yMax)); p.ClosePath(); p.Stroke();
        }
    }
}
