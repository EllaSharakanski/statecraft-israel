#ifndef STATECRAFT_LAND_INCLUDED
#define STATECRAFT_LAND_INCLUDED
// Land use on the ground (issue #33, promo-land): the patchwork of the promo's gameplay zoom (promo/final/03, 02), laid
// over the biome colour between ScGroundBase and ScGroundShade, so the relief still shades it. tools/land_bake.py bakes
// it all: landuse.png holds the land's linear colour (field parcels with soft rims and hedges, block-edge tracks,
// garrigue, terraces, scrub) and its weight in alpha, placed by real data (slope, rainfall, towns); tracks.png holds the
// offset to the nearest dirt track between towns, drawn here as a crisp line at any zoom.
// Kept to a lerp and one line on purpose: the same pattern as procedural shader code (about 150 instructions, in or out
// of a branch) cost about 5 ms a frame at gameplay03 in WebGL on the development Mac (docs/visual-v1.md, "Promo track:
// land use").

float _ScLandOn;                                // 1 = land use drawn, 0 = the bare biome ground (the "land" toggle)
static const float ScTrackRangeM = 1000.0;      // tools/land_bake.py TRACK_RANGE_M
static const float ScTrackHalfM = 55.0;         // half-width of a dirt track between towns
static const float3 ScDustLin = float3(0.8478, 0.6762, 0.4136);   // tools/land_bake.py SRGB["dust"], linear

// land: landuse.png (rgb linear colour, a weight); trackOffset: tracks.png rg; fp: the pixel footprint (m), taken by the
// caller. The beach band (coast < 0.52) stays the sea's (promo-sea).
float3 ScLandUse(float3 g, float4 land, float2 trackOffset, float coast, float fp)
{
    float shore = smoothstep(0.503, 0.52, coast);
    float3 c = lerp(g, land.rgb, land.a * shore);
    float dTrack = length(trackOffset * (255.0 / 127.0) - 1.0) * ScTrackRangeM;   // tools/land_bake.py encode_offsets
    return lerp(c, ScDustLin, saturate((ScTrackHalfM - dTrack) / max(fp, 1e-3) + 0.5) * shore * 0.85);
}

#endif
