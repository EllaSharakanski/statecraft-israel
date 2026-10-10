#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The global shader values StatecraftMap.hlsl reads, set when the map is built (MapBuilder.Build): the map's
    /// size, sea level and exaggeration, its origin and hex size, the biome colour texture and the haze. Globals, not
    /// material values, so the shared editor-made materials are never changed or instanced at run time.</summary>
    public static class MapShaderGlobals
    {
        public static readonly int Map = Shader.PropertyToID("_ScMap"), MapOrigin = Shader.PropertyToID("_ScMapOrigin"),
            Biome = Shader.PropertyToID("_ScBiomeTex"), Haze = Shader.PropertyToID("_ScHaze"), HazeWater = Shader.PropertyToID("_ScHazeWater"),
            HazeRange = Shader.PropertyToID("_ScHazeRange");

        /// <summary>A warm, dusty haze over land (sRGB).</summary>
        public static readonly Color HazeColour = new Color(0.80f, 0.76f, 0.68f);
        public const float HazeMax = 0.6f;
        /// <summary>A cooler, weaker haze over water (review I2), so the sea stays the promo's saturated blue towards the
        /// horizon: 0.4 of the land's strength.</summary>
        public static readonly Color WaterHazeColour = new Color(0.62f, 0.74f, 0.86f);
        public const float WaterHazeMax = HazeMax * 0.4f;
        /// <summary>Distance scale of the aerial haze: 1 - exp(-d / scale); at the gameplay pose's far edge (about 85 km) 19%.</summary>
        public const float HazeDistanceM = 400000f;
        /// <summary>Real elevations between which the haze thickens, low in the Jordan rift.</summary>
        // Whole metres, written as integer literals: HudTests.NoScriptRescalesADisplayedNumberItself would read the float
        // literal of a hundred as a percent rescale.
        public const float RiftHazeTopM = -100, RiftHazeFullM = -400;

        static Vector4 haze, hazeWater;

        public static void Apply(Terrain terrain, HeightmapMeta meta, float hexSizeM, Texture2D biome, MapRuntimeAssets assets)
        {
            var size = terrain.terrainData.size; var o = terrain.transform.position;
            float Y(float elevM) => o.y + (elevM - meta.elev_min_m) * MapBuilder.VerticalExaggeration;
            Shader.SetGlobalVector(Map, new Vector4(size.x, size.z, Y(0f), MapBuilder.VerticalExaggeration));
            Shader.SetGlobalVector(MapOrigin, new Vector4(o.x, o.y, o.z, hexSizeM));
            Shader.SetGlobalTexture(Biome, biome);
            var c = HazeColour.linear;
            haze = new Vector4(c.r, c.g, c.b, HazeMax);
            var w = WaterHazeColour.linear;
            hazeWater = new Vector4(w.r, w.g, w.b, WaterHazeMax);
            Shader.SetGlobalVector(Haze, haze);
            Shader.SetGlobalVector(HazeWater, hazeWater);
            Shader.SetGlobalVector(HazeRange, new Vector4(Y(RiftHazeTopM), Y(RiftHazeFullM), HazeDistanceM, 0f));
            assets.OnTeardown(VisualToggles.Register("haze", on =>
            {
                Shader.SetGlobalVector(Haze, on ? haze : new Vector4(haze.x, haze.y, haze.z, 0f));
                Shader.SetGlobalVector(HazeWater, on ? hazeWater : new Vector4(hazeWater.x, hazeWater.y, hazeWater.z, 0f));
            }));
        }
    }

    /// <summary>The baked map textures (tools/terrain_bake.py), shared by the editor scripts and the tests.</summary>
    public static class MapTexturePaths
    {
        public const string Bake = "Assets/Textures/Map/terrain_bake.png";
        public const string Coast = "Assets/Textures/Map/coast.png";
    }
}
