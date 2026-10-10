#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The ground colours the terrain shader paints per biome (sRGB; MapBuilder bakes them, blended between hexes,
    /// into the biome texture). Warmer and more saturated than MapBuilder.BiomeColours, which the minimap keeps: dry-grass
    /// gold on the coastal plain, olive-ochre hills, sand-amber desert (research notes §1, promo 03). "sea" and "lake" are
    /// only seen where a lake or sea hex's blend reaches the shore; the water itself is shaded from the coast texture.</summary>
    public static class MapPalette
    {
        public static IReadOnlyDictionary<string, Color> Ground { get; } = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.62f, 0.60f, 0.42f), ["lake"] = new Color(0.60f, 0.58f, 0.42f),
            ["coastal_plain"] = new Color(0.64f, 0.62f, 0.36f), ["hills"] = new Color(0.66f, 0.58f, 0.38f),
            ["mountains"] = new Color(0.62f, 0.53f, 0.40f), ["valley"] = new Color(0.58f, 0.58f, 0.37f),
            ["desert"] = new Color(0.84f, 0.70f, 0.48f),
        };
    }
}
