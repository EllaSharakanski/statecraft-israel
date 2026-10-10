#ifndef STATECRAFT_TERRAIN_INCLUDED
#define STATECRAFT_TERRAIN_INCLUDED
#include "Assets/Shaders/StatecraftMap.hlsl"

// The Custom Function of StatecraftTerrain.shadergraph (written by tools/terrain_graph.py; keep this signature in step with
// its INPUTS and OUTPUTS). UV is the terrain UV (0..1 over the map); PositionWS and NormalWS are absolute world space;
// Bake is terrain_bake.png (r AO, g curvature, b sky-view); Coast is coast.png (0.5 = shore, lower = water).
void StatecraftTerrain_float(float2 UV, float3 PositionWS, float3 NormalWS, UnityTexture2D Bake, UnityTexture2D Coast,
    out float3 BaseColor, out float3 NormalOut, out float Occlusion, out float Smoothness, out float Metallic, out float3 Emission)
{
    float3 n = normalize(NormalWS);
    float4 bake = SAMPLE_TEXTURE2D(Bake.tex, Bake.samplerstate, UV);
    float coast = SAMPLE_TEXTURE2D(Coast.tex, Coast.samplerstate, UV).r;
    float2 mapM = UV * _ScMap.xy;
    float3 ground = ScGround(UV, mapM, n, PositionWS, bake);
    float h = _ScHaze.a * ScHazeAmount(PositionWS);                 // land haze, applied after lighting via Emission
    float ao = bake.r * lerp(0.6, 1.0, bake.b);
    float water = ScWater(coast);
    // Water: the flat sea colour, lit like the skirt's URP Lit Sea.mat so the map's edge shows no seam (Task 7 replaces
    // both sides with ScWaterShade together). No haze on water yet: the skirt's URP Lit sea has none either.
    BaseColor = lerp(ground * (1.0 - h), SRGBToLinear(float3(0.16, 0.42, 0.55)), water);
    NormalOut = normalize(lerp(n, float3(0, 1, 0), water));
    Occlusion = lerp(lerp(1.0, ao, 0.8), 1.0, water);
    Smoothness = 0.0;
    Metallic = 0.0;
    Emission = _ScHaze.rgb * h * (1.0 - water);
}

#endif
