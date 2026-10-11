#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>How a region is outlined: the promo's blue band with a white inner stripe (Primary), its red-orange band with
    /// a cream inner stripe (Secondary), or not at all (None: a region that only stops its neighbours' bands from running on,
    /// e.g. the land past a border that is not drawn from its side).</summary>
    public enum BorderStyle : byte { None = 0, Primary = 1, Secondary = 2 }

    /// <summary>Which region each hex belongs to, and how each region is outlined: what the border renderer draws. A hex not
    /// listed belongs to no region, and no band is drawn against it (the coast, the edge of the data). Region ids run from 1
    /// to <see cref="MaxId"/>. The renderer draws only what a layer says, so a layer must come from real, sourced data (owner
    /// ruling, 2026-10-11: no invented political boundaries): the 1945-48 districts and sub-districts, the 1949 armistice
    /// lines when dated, fronts. None is in the repository yet (docs/visual-v1.md, "Region borders"), so the game draws no
    /// border; <see cref="BorderProof"/> is a synthetic pattern for tests and captures only.</summary>
    public sealed class RegionLayer
    {
        public const int MaxId = 65535;   // two texel bytes

        public readonly string Name;
        readonly Dictionary<(int q, int r), int> regionOf;
        readonly Dictionary<int, BorderStyle> styleOf;

        /// <summary>Throws ArgumentException naming the layer for an id outside 1..MaxId, a hex's region with no style, or an
        /// undefined style.</summary>
        public RegionLayer(string name, IReadOnlyDictionary<(int q, int r), int> hexRegions, IReadOnlyDictionary<int, BorderStyle> styles)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("a region layer needs a name", nameof(name)) : name;
            if (hexRegions == null) throw new ArgumentNullException(nameof(hexRegions));
            if (styles == null) throw new ArgumentNullException(nameof(styles));
            foreach (var kv in styles)
            {
                if (kv.Key < 1 || kv.Key > MaxId) throw new ArgumentException($"{name}: region id {kv.Key} outside 1..{MaxId}");
                if (!Enum.IsDefined(typeof(BorderStyle), kv.Value)) throw new ArgumentException($"{name}: region {kv.Key} has an unknown style {(int)kv.Value}");
            }
            foreach (var kv in hexRegions)
                if (!styles.ContainsKey(kv.Value)) throw new ArgumentException($"{name}: hex ({kv.Key.q}, {kv.Key.r}) is in region {kv.Value}, which has no style");
            regionOf = new Dictionary<(int q, int r), int>(hexRegions);
            styleOf = new Dictionary<int, BorderStyle>(styles);
        }

        /// <summary>The hex's region, or 0 (none).</summary>
        public int RegionOf(int q, int r) => regionOf.TryGetValue((q, r), out var id) ? id : 0;
        public BorderStyle StyleOf(int id) => styleOf.TryGetValue(id, out var s) ? s : BorderStyle.None;
        public IEnumerable<(int q, int r)> Hexes => regionOf.Keys;
        public int Count => regionOf.Count;
    }

    /// <summary>The region borders (promo-borders): the promo 03 look of coloured, two-tone bands along hex edges, drawn by
    /// the terrain shader (StatecraftBorders.hlsl) from one texel per hex. <see cref="Apply"/> runs on every map build
    /// (MapShaderGlobals.Apply): it allocates the hex texture, draws nothing, and registers the "borders" toggle;
    /// <see cref="Use"/> gives the built map a layer. Visual only: it reads no sim state and changes none.</summary>
    public static class MapBorders
    {
        public const string ToggleName = "borders";

        static readonly int RegionTex = Shader.PropertyToID("_ScRegionTex"), RegionInfo = Shader.PropertyToID("_ScRegionInfo"),
            Borders = Shader.PropertyToID("_ScBorders"),
            Colour1 = Shader.PropertyToID("_ScBorderColour1"), Light1 = Shader.PropertyToID("_ScBorderLight1"), Shape1 = Shader.PropertyToID("_ScBorderShape1"),
            Colour2 = Shader.PropertyToID("_ScBorderColour2"), Light2 = Shader.PropertyToID("_ScBorderLight2"), Shape2 = Shader.PropertyToID("_ScBorderShape2");

        /// <summary>One style's look. Colours are sRGB (above 1: brighter than white before the grade), measured on
        /// promo/final/03-gameplay-1936.png (2752 px wide) and set so the graded game matches them (docs/visual-v1.md, "Region borders"). BandM is the coloured plus light stripe in metres,
        /// so the band grows towards the camera as the promo's does; ColourShare the coloured stripe's part of it; Wash the
        /// wash's width as a multiple of the band, WashStrength its opacity at the band.</summary>
        public readonly struct Style
        {
            public readonly Color Colour, Light; public readonly float BandM, ColourShare, Wash, WashStrength;
            public Style(Color colour, Color light, float bandM, float colourShare, float wash, float washStrength)
            { Colour = colour; Light = light; BandM = bandM; ColourShare = colourShare; Wash = wash; WashStrength = washStrength; }
        }

        /// <summary>The promo's blue line (6 + 6 px of blue and white with a 10 px wash at 2752 px; 125,140,182 and
        /// 251,251,248): the heavier of the two.</summary>
        public static readonly Style Primary = new Style(new Color(0.40f, 0.50f, 0.80f), new Color(1.08f, 1.08f, 1.06f), 300f, 0.5f, 0.9f, 0.40f);
        /// <summary>The promo's red-orange line (4 + 3 px with an 8 px wash; 176,93,61 and 246,188,124), a little darker than
        /// the promo's so it keeps its lightness contrast against dry grass for colour-blind players (GradeTests).</summary>
        public static readonly Style Secondary = new Style(new Color(0.64f, 0.28f, 0.17f), new Color(1.05f, 0.76f, 0.48f), 230f, 0.6f, 1.0f, 0.18f);
        /// <summary>The band never draws thinner than this (the overview), nor, with its wash, wider than this share of a hex's
        /// inradius (so a far hex is never filled by its outline).</summary>
        public const float MinBandPx = 2f, MaxShareOfInradius = 0.5f;

        static Texture2D? texture;
        static bool hasLayer, toggleOn = true;
        static int qMin, rMin;
        static readonly HashSet<(int q, int r)> mapHexes = new HashSet<(int q, int r)>();

        /// <summary>The layer the map draws now (null: none).</summary>
        public static RegionLayer? Current { get; private set; }

        /// <summary>Called by MapShaderGlobals.Apply on every build: a cleared hex texture over hexes.json's (q, r) range (owned
        /// by the map), the styles' globals, no layer, and the "borders" toggle (on), all undone by the map's teardown.</summary>
        public static void Apply(HexFile hexes, MapRuntimeAssets assets)
        {
            if (hexes == null) throw new ArgumentNullException(nameof(hexes));
            if (assets == null) throw new ArgumentNullException(nameof(assets));
            qMin = hexes.hexes.Min(h => h.q); rMin = hexes.hexes.Min(h => h.r);
            int w = hexes.hexes.Max(h => h.q) - qMin + 1, h = hexes.hexes.Max(x => x.r) - rMin + 1;
            mapHexes.Clear();
            foreach (var x in hexes.hexes) mapHexes.Add((x.q, x.r));
            var tex = assets.Own(new Texture2D(w, h, TextureFormat.RGBA32, false, true)
            {
                name = "RegionHexes", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            });
            tex.SetPixels32(new Color32[w * h]); tex.Apply(false, false);
            texture = tex;
            Current = null; hasLayer = false; toggleOn = true;
            Shader.SetGlobalTexture(RegionTex, tex);
            Shader.SetGlobalVector(RegionInfo, new Vector4(qMin, rMin, w, h));
            SetStyle(Colour1, Light1, Shape1, Primary);
            SetStyle(Colour2, Light2, Shape2, Secondary);
            Push();
            assets.OnTeardown(VisualToggles.Register(ToggleName, on => { toggleOn = on; Push(); }));
            assets.OnTeardown(() => { texture = null; Current = null; hasLayer = false; Push(); });
        }

        /// <summary>Draws <paramref name="layer"/> on the built map (null: none). Throws ArgumentException for a hex that is
        /// not on the map, InvalidOperationException before a map is built.</summary>
        public static void Use(RegionLayer? layer)
        {
            if (texture == null) throw new InvalidOperationException("MapBorders.Use needs a built map");
            int w = texture.width, h = texture.height;
            var px = new Color32[w * h];
            if (layer != null)
                foreach (var (q, r) in layer.Hexes)
                {
                    if (!mapHexes.Contains((q, r))) throw new ArgumentException($"{layer.Name}: hex ({q}, {r}) is not on the map");
                    px[(r - rMin) * w + (q - qMin)] = Encode(layer.RegionOf(q, r), layer.StyleOf(layer.RegionOf(q, r)));
                }
            texture.SetPixels32(px); texture.Apply(false, false);
            Current = layer; hasLayer = layer != null && layer.Count > 0;
            Push();
        }

        /// <summary>The texel of a hex: r + 256 g the region id, b the style (StatecraftBorders.hlsl, ScRegionAt).</summary>
        public static Color32 Encode(int id, BorderStyle style) => new Color32((byte)(id & 0xFF), (byte)(id >> 8), (byte)style, 255);

        static void SetStyle(int colour, int light, int shape, Style s)
        {
            var c = s.Colour.linear; var l = s.Light.linear;
            Shader.SetGlobalVector(colour, new Vector4(c.r, c.g, c.b, 1f));
            Shader.SetGlobalVector(light, new Vector4(l.r, l.g, l.b, 1f));
            Shader.SetGlobalVector(shape, new Vector4(s.BandM, s.ColourShare, s.Wash, s.WashStrength));
        }

        static void Push() => Shader.SetGlobalVector(Borders, new Vector4(MinBandPx, MaxShareOfInradius, 0f, toggleOn && hasLayer && texture != null ? 1f : 0f));
    }

    /// <summary>A synthetic region pattern that shows the border renderer at work, for tests, captures and the frame
    /// profile only: concentric hex rings <see cref="RingWidth"/> hexes wide around the hex at a given point, the inner disc
    /// outlined Primary and every ring Secondary; sea and lake hexes in no region (so no band runs along the coast). It is
    /// plainly not geography (hexagonal rings) and appears only when asked for: "borderproof" in the WebGL page URL or
    /// -borderproof on desktop and in the editor's promo renders; normal play never shows it.</summary>
    public static class BorderProof
    {
        public const string Flag = "borderproof";
        public const int RingWidth = 3;
        /// <summary>The rings' centre: gameplay03's look-at, so both styles and their double band are in that frame.</summary>
        public static readonly (double lon, double lat) Centre = (PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat);

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool Requested(string? url, string[] args, bool webgl) => webgl ? UrlFlags.Has(url, Flag) : args.Contains("-" + Flag);

        /// <summary>The pattern on <paramref name="hexes"/>, centred on axial hex <paramref name="centre"/>.</summary>
        public static RegionLayer Build(HexFile hexes, (int q, int r) centre)
        {
            var regions = new Dictionary<(int q, int r), int>(); var styles = new Dictionary<int, BorderStyle>();
            foreach (var h in hexes.hexes)
            {
                if (!h.land) continue;
                int ring = Distance((h.q, h.r), centre) / RingWidth, id = ring + 1;
                regions[(h.q, h.r)] = id;
                styles[id] = ring == 0 ? BorderStyle.Primary : BorderStyle.Secondary;
            }
            return new RegionLayer("border proof (synthetic rings)", regions, styles);
        }

        /// <summary>The pattern centred on the hex under <see cref="Centre"/> of the built map; drawn at once.</summary>
        public static RegionLayer Show(MapScene map)
        {
            if (map == null || map.Terrain == null || map.Hexes == null) throw new InvalidOperationException("BorderProof needs a built map");
            var layer = Build(map.Hexes, HexAt(map, Centre.lon, Centre.lat));
            MapBorders.Use(layer);
            return layer;
        }

        /// <summary>The axial hex under a lon/lat point of the built map.</summary>
        public static (int q, int r) HexAt(MapScene map, double lon, double lat)
        {
            float size = map.Hexes.hex_size_km * 1000f;
            var p = HexGrid.FromWorld(map.Frame.ToWorld(lon, lat, 0), map.Terrain.transform.position, map.Terrain.terrainData.size.z);
            var c = HexGrid.NearestCentre(p, size);
            int r = Mathf.RoundToInt(c.y / (1.5f * size));
            return (Mathf.RoundToInt((c.x / size - 0.8660254f * r) / 1.7320508f), r);
        }

        public static int Distance((int q, int r) a, (int q, int r) b)
        {
            int dq = a.q - b.q, dr = a.r - b.r;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }
    }
}
