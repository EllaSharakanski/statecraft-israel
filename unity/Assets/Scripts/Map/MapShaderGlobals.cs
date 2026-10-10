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
            HazeRange = Shader.PropertyToID("_ScHazeRange"), WaterOn = Shader.PropertyToID("_ScWaterOn"), Grid = Shader.PropertyToID("_ScGrid");

        /// <summary>A warm, dusty haze over land (sRGB): warmer than the ground it covers (R - B 0.26), so the far land reads
        /// as dust, not as a cool milky veil (Task 6 review I1).</summary>
        public static readonly Color HazeColour = new Color(0.84f, 0.74f, 0.58f);
        public const float HazeMax = 0.45f;
        /// <summary>A cooler, weaker haze over water (review I2), so the sea stays the promo's saturated blue towards the
        /// horizon: 0.4 of the land's strength.</summary>
        public static readonly Color WaterHazeColour = new Color(0.62f, 0.74f, 0.86f);
        public const float WaterHazeMax = HazeMax * 0.4f;
        /// <summary>The aerial haze is 1 - exp(-max(d - start, 0) / scale), times HazeMax: none within 200 km (the gameplay
        /// pose, 48-91 km, has none: its depth cue is the tilt-shift), about 2% at the country-wide pose's nearest land
        /// (about 205 km) and 17% at its horizon (about 400 km): a depth cue, not a veil (review I1).</summary>
        public const float HazeStartM = 200000f, HazeDistanceM = 250000f;
        /// <summary>Real elevations between which the haze thickens, low in the Jordan rift.</summary>
        public const float RiftHazeTopM = -100f, RiftHazeFullM = -400f;

        /// <summary>Hex line opacity on land and water, and width in pixels (anti-aliased, the same at every zoom). Light,
        /// faint lines on land as in the promo.</summary>
        public const float GridLandOpacity = 0.25f, GridWaterOpacity = 0.35f, GridWidthPx = 1.5f;

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
            Shader.SetGlobalVector(HazeRange, new Vector4(Y(RiftHazeTopM), Y(RiftHazeFullM), HazeDistanceM, HazeStartM));
            Shader.SetGlobalVector(Grid, new Vector4(GridLandOpacity, GridWaterOpacity, GridWidthPx, 1f));
            assets.OnTeardown(VisualToggles.Register("grid", on => Shader.SetGlobalVector(Grid, new Vector4(GridLandOpacity, GridWaterOpacity, GridWidthPx, on ? 1f : 0f))));
            Shader.SetGlobalFloat(WaterOn, 1f);
            assets.OnTeardown(VisualToggles.Register("water", on => Shader.SetGlobalFloat(WaterOn, on ? 1f : 0f)));
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
        /// <summary>The ambient occlusion on its own, uncompressed (R8): BC1 stepped it on broad slopes (Task 6 fix round 1).</summary>
        public const string Ao = "Assets/Textures/Map/ao.png";
    }
}
