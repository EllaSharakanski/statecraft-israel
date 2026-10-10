#ifndef STATECRAFT_MAP_INCLUDED
#define STATECRAFT_MAP_INCLUDED
// Shared by the terrain graph's Custom Function (StatecraftTerrain.hlsl) and the sea (Sea.shader). The globals are set by
// MapShaderGlobals.Apply (C#) whenever the map is built. Colours are linear; SRGBToLinear converts the authored sRGB values.

float4 _ScMap;        // x width (m, east), y length (m, north), z sea level (world y), w vertical exaggeration
float4 _ScMapOrigin;  // xyz terrain origin (world), w hex circumradius (m)
float4 _ScHaze;       // rgb land haze colour (linear), a strongest land haze (0..1)
float4 _ScHazeWater;  // rgb water haze colour (linear, cooler), a strongest water haze (weaker: the sea keeps its blue)
float4 _ScHazeRange;  // x rift haze begins (world y), y rift haze is full (world y), z aerial distance scale (m), w start (m)
float4 _ScGrid;       // x land line opacity, y water line opacity, z line width (pixels), w 1 = on
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

static const float ScCoastRangeM = 12000.0;   // tools/terrain_bake.py COAST_RANGE_M
float _ScWaterOn;                              // 1 = shaded water, 0 = the flat sea colour (the "water" toggle)

// The water's lit colour before haze (callers apply ScHazeWater and output it as is): a turquoise -> teal -> navy ramp with
// distance from the shore; the Dead Sea (below -300 m) pale turquoise with a white salt rim, the Sea of Galilee a fresher
// teal; two scrolling noise layers tilt a wave normal for Lambert from the sun (with its shadows: cliffs shade the water)
// plus sky fill and a small glint; animated foam bands within 350 m of the shore. elevM is the real elevation of the water
// surface; mapM metres east and north of the map's south-west corner.
float3 ScWaterShade(float coast, float elevM, float3 positionWS, float2 mapM)
{
    if (_ScWaterOn < 0.5) return SRGBToLinear(float3(0.16, 0.42, 0.55)) * 0.8;
    float offshoreM = saturate(0.5 - coast) * 2.0 * ScCoastRangeM;
    float t = pow(saturate(offshoreM / ScCoastRangeM), 0.55);
    float3 shallow = SRGBToLinear(float3(0.38, 0.74, 0.72));
    float3 mid = SRGBToLinear(float3(0.13, 0.47, 0.58));
    float3 deep = SRGBToLinear(float3(0.06, 0.24, 0.40));
    float3 c = t < 0.5 ? lerp(shallow, mid, t * 2.0) : lerp(mid, deep, t * 2.0 - 1.0);
    float salt = step(elevM, -300.0);
    float fresh = step(elevM, -100.0) * (1.0 - salt);
    c = lerp(c, SRGBToLinear(float3(0.45, 0.78, 0.78)), salt * 0.8);
    c = lerp(c, SRGBToLinear(float3(0.15, 0.48, 0.60)), fresh * 0.7);

    float time = _Time.y;
    float2 p = mapM / 700.0;
    float2 g = float2(ScNoise(p + float2(time * 0.05, 0.0)) - ScNoise(p + float2(0.37 + time * 0.05, 0.0)),
                      ScNoise(p.yx + float2(0.0, time * 0.04)) - ScNoise(p.yx + float2(0.0, 0.41 + time * 0.04)));
    float3 n = normalize(float3(g.x * 0.15, 1.0, g.y * 0.15));
#if defined(UNIVERSAL_REALTIME_LIGHTS_INCLUDED) && !defined(SHADERGRAPH_PREVIEW)
    Light sun = GetMainLight(TransformWorldToShadowCoord(positionWS));   // review I6: the water receives the sun's shadows
    float3 l = sun.direction, lc = sun.color;
    float shadow = sun.shadowAttenuation;
#else
    float3 l = normalize(float3(-0.83, 0.5, 0.22)), lc = float3(1.0, 0.84, 0.64);   // passes without lights (depth, preview)
    float shadow = 1.0;
#endif
    float3 v = normalize(_WorldSpaceCameraPos - positionWS);
    float3 lit = c * (0.55 + 0.45 * saturate(dot(n, l)) * lc * shadow);
    // A small glint (Task 7: 0.06, not the plan's 0.25). Where the camera's pitch meets the sun's height (50 deg each with
    // Map.unity's default sun, at yaw 180) the 0.25 lobe lit a warm patch rising 0.08 in sRGB red over 6 km of open sea, a
    // view-dependent gradient that read as a seam between samples 5 km apart either side of the map's edge.
    lit += pow(saturate(dot(reflect(-l, n), v)), 80.0) * 0.06 * lc * shadow;

    float band = 1.0 - smoothstep(0.0, 350.0, offshoreM);
    float waves = 0.7 + 0.3 * sin(offshoreM / 90.0 - time * 1.3);
    float rim = salt * (1.0 - smoothstep(0.0, 900.0, offshoreM));
    float foam = saturate(band * waves * (1.0 - salt) + rim);
    return lerp(lit, SRGBToLinear(float3(0.93, 0.95, 0.94)), foam * 0.85);
}

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

// Distance (m) from p to the nearest edge of its hex: tools/hexgrid.py's pointy-top axial layout, (0, 0) at the north-west
// corner, p.x east and p.y south (HexGrid.EdgeDistance is the C# twin). n is that edge's outward unit normal (constant over
// each sixth of a hex).
float ScHexEdgeDistance(float2 p, float size, out float2 n)
{
    float qf = (0.57735027 * p.x - p.y / 3.0) / size;
    float rf = 0.66666667 * p.y / size;
    float sf = -qf - rf;
    float q = round(qf), r = round(rf), s = round(sf);
    float dq = abs(q - qf), dr = abs(r - rf), ds = abs(s - sf);
    if (dq > dr && dq > ds) q = -r - s; else if (dr > ds) r = -q - s;
    float2 v = p - float2(size * (1.7320508 * q + 0.8660254 * r), size * 1.5 * r);
    float a = v.x, b = 0.5 * v.x + 0.8660254 * v.y, c = 0.5 * v.x - 0.8660254 * v.y;
    float m = abs(a); n = float2(a < 0.0 ? -1.0 : 1.0, 0.0);
    if (abs(b) > m) { m = abs(b); n = (b < 0.0 ? -1.0 : 1.0) * float2(0.5, 0.8660254); }
    if (abs(c) > m) { m = abs(c); n = (c < 0.0 ? -1.0 : 1.0) * float2(0.5, -0.8660254); }
    return 0.8660254 * size - m;
}

float ScHexEdgeDistance(float2 p, float size) { float2 n; return ScHexEdgeDistance(p, size, n); }

// Box-filtered coverage (0..1) of a hex line widthPx pixels wide at mapM (metres east and north of the south-west corner);
// footprintM returns the metres per pixel (the larger screen axis). The pixel's extent across the edge comes from the
// derivatives of the continuous grid coordinate, projected on the edge's normal (Task 8 review I1: fwidth of the V-shaped
// distance collapses on the line itself and broke it into dashes). Call it in uniform control flow (outside any [branch]).
float ScGridLine(float2 mapM, float widthPx, out float footprintM)
{
    float2 p = float2(mapM.x, _ScMap.y - mapM.y);
    float2 px = ddx(p), py = ddy(p);
    footprintM = max(length(px), length(py));
    float2 n;
    float d = ScHexEdgeDistance(p, _ScMapOrigin.w, n);
    float aa = max(length(float2(dot(px, n), dot(py, n))), 1e-3);   // metres per pixel across the edge
    return saturate(0.5 * widthPx + 0.5 - d / aa);
}

// The grid as drawn on both the terrain and the skirt's sea: on/off, thinner towards the overview, and gone where a hex
// is under about 10 px on screen (full from 30 px), so far hexes never form a wash. Light lines on land and water, as in
// the promo (ruling 2: the promo wins over M2's dark lines).
static const float3 ScGridLandSrgb = float3(0.93, 0.90, 0.80);
static const float3 ScGridWaterSrgb = float3(0.82, 0.90, 0.95);
float ScGridAmount(float2 mapM, float3 positionWS)
{
    float footprintM;
    float cover = ScGridLine(mapM, _ScGrid.z, footprintM);
    float fade = lerp(1.0, 0.35, saturate((length(_WorldSpaceCameraPos - positionWS) - 40000.0) / 200000.0));
    fade *= saturate((_ScMapOrigin.w / max(footprintM, 1e-3) - 10.0) / 20.0);
    return cover * _ScGrid.w * fade;
}

#endif
