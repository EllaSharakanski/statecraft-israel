#ifndef STATECRAFT_MAP_INCLUDED
#define STATECRAFT_MAP_INCLUDED
// Shared by the terrain graph's Custom Function (StatecraftTerrain.hlsl) and the sea (Sea.shader). The globals are set by
// MapShaderGlobals.Apply (C#) whenever the map is built. Colours are linear; SRGBToLinear converts the authored sRGB values.

float4 _ScMap;        // x width (m, east), y length (m, north), z sea level (world y), w vertical exaggeration
float4 _ScMapOrigin;  // xyz terrain origin (world), w hex circumradius (m)
float4 _ScHaze;       // rgb land haze colour (linear), a strongest land haze (0..1)
float4 _ScHazeWater;  // rgb water haze colour (linear, cooler), a strongest water haze (weaker: the sea keeps its blue)
float4 _ScHazeRange;  // x rift haze begins (world y), y rift haze is full (world y), z aerial distance scale (m), w start (m)
TEXTURE2D(_ScBiomeTex); SAMPLER(sampler_ScBiomeTex);

float ScHash(float2 p)
{
    p = frac(p * float2(0.1031, 0.1030));
    p += dot(p, p.yx + 33.33);
    return frac((p.x + p.y) * p.x);
}

float ScNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(ScHash(i), ScHash(i + float2(1, 0)), u.x), lerp(ScHash(i + float2(0, 1)), ScHash(i + float2(1, 1)), u.x), u.y);
}

// Low in the rift the haze grows by up to this share (1.5 saturated the rift at the country-wide zoom: Task 6 review I1).
static const float ScRiftHazeBoost = 0.6;

// Aerial perspective, 0..1 before each surface's strength: none within the start distance, then 1 - exp(-(d - start) /
// scale), and up to 1.6x low in the rift. It is applied after lighting on both surfaces (review I2): land outputs BaseColor x (1 - h) plus Emission =
// haze colour x h, so Lit's result is lerp(lit ground, haze, h); water lerps its final colour (ScHazeWater).
float ScHazeAmount(float3 positionWS)
{
    float dist = length(_WorldSpaceCameraPos - positionWS);
    float aerial = 1.0 - exp(-max(dist - _ScHazeRange.w, 0.0) / max(_ScHazeRange.z, 1.0));
    float low = saturate((_ScHazeRange.x - positionWS.y) / max(_ScHazeRange.x - _ScHazeRange.y, 1.0));
    return saturate(aerial * (1.0 + ScRiftHazeBoost * low));
}

float3 ScHazeWater(float3 colour, float3 positionWS)
{
    return lerp(colour, _ScHazeWater.rgb, _ScHazeWater.a * ScHazeAmount(positionWS));
}

// 1 in water, 0 on land; coast is the signed distance texture (0.5 at the shore, tools/terrain_bake.py).
float ScWater(float coast) { return 1.0 - smoothstep(0.497, 0.503, coast); }

// The painted ground: biome colour, warmer stone high up, a dusty rift floor, rock on steep slopes, two octaves of
// breakup, curvature (ridges light, hollows dark) and the baked occlusion (bake: r AO from ao.png, g curvature, b sky-view).
float3 ScGround(float2 uv, float2 mapM, float3 n, float3 positionWS, float4 bake)
{
    float elevM = (positionWS.y - _ScMap.z) / _ScMap.w;            // real metres above sea level
    float slope = 1.0 - saturate(n.y);                             // 0 flat, 1 vertical
    float3 g = SAMPLE_TEXTURE2D(_ScBiomeTex, sampler_ScBiomeTex, uv).rgb;
    g = lerp(g, g * float3(1.08, 1.00, 0.86), smoothstep(500.0, 1100.0, elevM));
    g = lerp(g, g * float3(1.10, 1.04, 0.92), 1.0 - smoothstep(-350.0, -150.0, elevM));
    float rock = smoothstep(0.22, 0.45, slope + (ScNoise(mapM / 900.0) - 0.5) * 0.12);
    g = lerp(g, SRGBToLinear(float3(0.56, 0.49, 0.41)), rock);
    // The fine octave fades to its mean as a pixel's footprint nears its 520 m period, so it never aliases into shimmer.
    float fine = lerp(0.5, ScNoise(mapM / 520.0), 1.0 - saturate(length(fwidth(mapM)) / 520.0));
    g *= 0.93 + 0.14 * (ScNoise(mapM / 2600.0) * 0.6 + fine * 0.4);
    g *= 1.0 + 0.40 * (bake.g - 0.5) * 2.0;
    g *= lerp(1.0, bake.r * lerp(0.6, 1.0, bake.b), 0.45);
    return g;
}

#endif
