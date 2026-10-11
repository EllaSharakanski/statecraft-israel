#ifndef STATECRAFT_LAND_INCLUDED
#define STATECRAFT_LAND_INCLUDED
// Land use on the ground (issue #33, promo-land): the patchwork of the promo's gameplay zoom (promo/final/03, 02), laid
// over the biome colour between ScGroundBase and ScGroundShade, so the relief still shades it. tools/land_bake.py bakes
// it all: landuse.png holds the land's colour (field parcels with soft rims and hedges, block-edge tracks,
// garrigue, terraces, scrub) and its weight in alpha, placed by real data (slope, rainfall, towns); tracks.png holds the
// offset to the nearest dirt track between towns, drawn here as a crisp line that thins out once a pixel is wider than it.
// Kept to a lerp and one line on purpose: the same pattern as procedural shader code (about 150 instructions) cost about
// 5 ms a frame at gameplay03 in WebGL on the development Mac even where a branch skipped it, most likely register
// pressure / occupancy from the shader's size rather than its arithmetic (docs/visual-v1.md, "Promo track: land use").

float _ScLandOn;                                // 1 = land use drawn, 0 = the bare biome ground (the "land" toggle)
static const float ScTrackRangeM = 1000.0;      // tools/land_bake.py TRACK_RANGE_M
static const float ScTrackHalfM = 55.0;         // half-width of a dirt track between towns
static const float3 ScDustLin = float3(0.8478, 0.6762, 0.4136);   // tools/land_bake.py SRGB["dust"], linear

// land: landuse.png (rgb the land's colour, linear once the sampler decodes the sRGB texture; a its weight); track:
// tracks.png (rg the offset to the nearest track, b where that offset may draw one); fp: the pixel footprint (m).
// The land reaches the waterline (coast 0.5): promo-sea's beach band (ScShore, drawn after) covers it where there is a
// beach, and fields run to the water where there is none (the Sea of Galilee, cliffs).
float3 ScLandUse(float3 g, float4 land, float3 track, float coast, float fp)
{
    float shore = smoothstep(0.499, 0.503, coast);
    float3 c = lerp(g, land.rgb, land.a * shore);
    float dTrack = length(track.rg * (255.0 / 127.0) - 1.0) * ScTrackRangeM;   // tools/land_bake.py encode_offsets
    // Box-filtered coverage of a 2 x ScTrackHalfM line, scaled down where a pixel is wider than the line (review M1: it
    // stayed at half opacity at any footprint); faded where filtering mixed in a texel that may not draw a track (b).
    float w = max(fp, 1e-3);
    float cover = saturate((ScTrackHalfM - dTrack) / w + 0.5) * saturate(2.0 * ScTrackHalfM / w) * smoothstep(0.9, 1.0, track.b);
    return lerp(c, ScDustLin, cover * shore * 0.85);
}

#endif
