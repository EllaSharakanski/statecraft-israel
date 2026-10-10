#nullable enable
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>Frames a portrait in a box with background-size cover so its focal point (the face) is in view: the picture is
    /// scaled to cover the box, and its vertical position puts the focal point at the box's middle where it can, clamped so
    /// no empty edge shows (playtest 2, P2-5).</summary>
    public static class PortraitFraming
    {
        /// <summary>The picture's top offset under cover scaling, in picture pixels scaled to the box, and the scaled height.</summary>
        static (float offset, float height) Offset(float texW, float texH, float boxW, float boxH, float focusY)
        {
            var scale = Mathf.Max(boxW / texW, boxH / texH);
            var h = texH * scale;
            var offset = Mathf.Clamp(focusY * h - boxH / 2, 0f, Mathf.Max(0f, h - boxH));
            return (offset, h);
        }

        /// <summary>background-position-y in percent: p% puts the picture's p% line on the box's p% line.</summary>
        public static float PositionYPercent(float texW, float texH, float boxW, float boxH, float focusY)
        {
            var (offset, h) = Offset(texW, texH, boxW, boxH, focusY);
            return h - boxH <= 0.5f ? 50f : offset / (h - boxH) * 100f;
        }

        /// <summary>The share of the picture's height in view, from top to bottom.</summary>
        public static (float top, float bottom) VisibleBand(float texW, float texH, float boxW, float boxH, float focusY)
        {
            var (offset, h) = Offset(texW, texH, boxW, boxH, focusY);
            return (offset / h, Mathf.Min(1f, (offset + boxH) / h));
        }
    }
}
