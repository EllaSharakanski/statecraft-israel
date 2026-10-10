#ifndef STATECRAFT_LAND_INCLUDED
#define STATECRAFT_LAND_INCLUDED
// Land use on the ground (issue #33, promo-land): the patchwork of the promo's gameplay zoom (promo/final/03, 02) over the
// biome colour, painted between ScGroundBase and ScGroundShade so the relief still shades it. Where comes from data, baked
// by tools/land_bake.py: landuse.png (r arable fields, g citrus belt, b wet enough for terraces and scrub) and tracks.png
// (the offset to the nearest dirt track between towns). What the ground looks like there is procedural, seeded by world
// position only, so every frame and every machine draws the same fields:
// - field parcels: strips 1 km wide and 1.1-2.6 km long (symbolic scale, like the kits) in blocks of about 5 km, each
//   block on its own angle, its edges wobbled by low noise; each strip a crop (green rows, ochre stubble, ploughed brown
//   furrows, pale fallow), orchard earth in the citrus belt, or left as the base grass; soft grassy rims, dark hedges;
// - dirt tracks between the towns and along some edges of the farmed blocks;
// - garrigue: the wet unfarmed hills in patches of stony soil and grass instead of the biome's flat gold;
// - terraces on the wet hill slopes: a stone riser every ScTerraceStepM of real rise, each tread shaded;
// - scrub: bushes of maquis (jittered dots) on the wet unfarmed ground, a faint mottle further out.
// Every pattern fades to its mean as its period nears the pixel footprint, so the overview never shimmers.

float _ScLandOn;                                     // 1 = land use drawn, 0 = the bare biome ground (the "land" toggle)
static const float ScPaletteBrightness = 1.107;     // MapPalette.Brightness (LandUseTests checks they agree)
static const float ScBlockM = 5200.0;          // a farm block (one angle per block)
static const float ScStripM = 1000.0;          // strip width across the block
static const float ScTrackRangeM = 1000.0;     // tools/land_bake.py TRACK_RANGE_M
static const float ScTrackHalfM = 55.0;        // half-width of a data track
static const float ScTerraceStepM = 40.0;      // real metres of rise per terrace (symbolic, like the kits)
static const float ScRowM = 150.0;             // crop row / furrow spacing (symbolic scale, like the kits)

// sRGB colours, scaled by ScPaletteBrightness like the biome palette (MapPalette).
static const float3 ScCropGreen = float3(0.46, 0.50, 0.31);
static const float3 ScCropOchre = float3(0.78, 0.66, 0.44);
static const float3 ScPlough = float3(0.58, 0.47, 0.34);
static const float3 ScFallow = float3(0.72, 0.66, 0.51);
static const float3 ScHillSoil = float3(0.66, 0.57, 0.46);
static const float3 ScHillGrass = float3(0.56, 0.55, 0.37);
static const float3 ScHedge = float3(0.30, 0.33, 0.20);
static const float3 ScDust = float3(0.84, 0.76, 0.61);
static const float3 ScOrchardEarth = float3(0.52, 0.47, 0.31);
static const float3 ScScrub = float3(0.34, 0.36, 0.22);
static const float3 ScStone = float3(0.74, 0.68, 0.56);

float2 ScRotate(float2 p, float a) { float c = cos(a), s = sin(a); return float2(c * p.x - s * p.y, s * p.x + c * p.y); }

// Coverage (0..1) of a line halfM wide at distance dM, box-filtered over the pixel footprint fpM.
float ScLine(float dM, float halfM, float fpM) { return saturate((halfM - dM) / max(fpM, 1e-3) + 0.5); }

// Strength (0..1) of a pattern with period periodM at footprint fpM: full up to 0.15 period per pixel, gone from 0.5.
float ScDetail(float periodM, float fpM) { return 1.0 - smoothstep(periodM * 0.15, periodM * 0.5, fpM); }

// fp is the pixel footprint in metres and terraceFp the terrace steps per pixel, both taken by the caller in uniform
// control flow, so this can run inside a branch (derivatives there are undefined).
float3 ScLandUse(float3 g, float3 land, float2 trackOffset, float2 mapM, float3 n, float elevM, float coast, float fp,
    float terraceFp)
{
    float terraceU = elevM / ScTerraceStepM;
    float shore = smoothstep(0.503, 0.52, coast);                     // the beach band stays the sea's (promo-sea)
    float slope = 1.0 - saturate(n.y);

    // One shared low-frequency noise drives every patch below (fields, hill soil and grass, terraces, mottle): one lookup
    // instead of six (the first cut cost 4.8 ms at gameplay03, WebGL).
    float patch = ScNoise(mapM / 2600.0 + 3.1);
    // Farm blocks: a wobbled grid (cheap sines), each block turned by up to +-20 degrees about its own centre.
    float2 warp = 0.5 * float2(sin(mapM.y / 4100.0 + sin(mapM.x / 6700.0)), sin(mapM.x / 3900.0 + sin(mapM.y / 5300.0)));
    float2 bp = mapM / ScBlockM + warp * 0.55;
    float2 bi = floor(bp), bf = frac(bp);
    float bh = ScHash(bi + 0.37);
    float2 l = ScRotate((bf - 0.5) * ScBlockM, (bh - 0.5) * 0.7 + (bh > 0.8 ? 1.5708 : 0.0));
    // Strips across the block (irregular widths: each strip's own length of parcel along it).
    float sx = l.x / ScStripM + bh * 7.0;
    float ci = floor(sx);
    float ch = ScHash(float2(ci, bi.x * 3.1 + bi.y * 5.7));
    float lenM = lerp(1100.0, 2600.0, ch);
    float sy = l.y / lenM + ch * 5.0;
    float pi_ = floor(sy);
    float ph = ScHash(float2(ci * 1.37 + pi_ * 7.13, bi.y * 2.9 - bi.x * 1.3 + 0.5));
    float pv = ScHash(float2(pi_ * 3.3 + 0.7, ci * 2.1 + bi.x));
    float edgeM = min(min(frac(sx), 1.0 - frac(sx)) * ScStripM, min(frac(sy), 1.0 - frac(sy)) * lenM);
    float blockEdgeM = min(min(bf.x, 1.0 - bf.x), min(bf.y, 1.0 - bf.y)) * ScBlockM;

    // The parcel's crop (0.30 green, 0.20 ochre, 0.17 ploughed, 0.13 fallow, 0.20 left as the base grass), with rows.
    float rowsAmp = ScDetail(ScRowM, fp);
    float rows = sin(l.x * (6.2831853 / ScRowM)) * rowsAmp;
    // Linear colours (constants: the compiler folds the conversions); rows shade them in linear light.
    float3 crop = g; float cropOn = 1.0, rowGain = 0.0;
    if (ph < 0.30)      { crop = SRGBToLinear(ScCropGreen * ScPaletteBrightness); rowGain = 0.20; }
    else if (ph < 0.50) { crop = SRGBToLinear(ScCropOchre * ScPaletteBrightness); rowGain = 0.08; }
    else if (ph < 0.67) { crop = SRGBToLinear(ScPlough * ScPaletteBrightness); rowGain = 0.24; }
    else if (ph < 0.80) crop = SRGBToLinear(ScFallow * ScPaletteBrightness);
    else cropOn = 0.0;
    // In the citrus belt a share of the parcels (up to 55%) are groves: orchard earth (the trees are the kits').
    if (ScHash(float2(pv * 91.7, ph * 37.3)) < land.g * 0.55)
    { crop = SRGBToLinear(ScOrchardEarth * ScPaletteBrightness); rowGain = 0.12; cropOn = 1.0; }
    crop *= (1.0 + rowGain * rows) * (0.92 + 0.16 * pv);
    // The mean of the crop mix (the same weights), for the far zoom where single parcels would shimmer.
    float3 cropMean = SRGBToLinear((ScCropGreen * 0.30 + ScCropOchre * 0.20 + ScPlough * 0.17 + ScFallow * 0.13) / 0.80 * ScPaletteBrightness);
    float parcelDetail = ScDetail(ScStripM * 1.2, fp);
    float rim = smoothstep(20.0, 90.0, edgeM);                        // soft grassy rims (promo 03)
    float3 parcel = lerp(g, crop, cropOn * rim);
    float3 farmed = lerp(lerp(g, cropMean, 0.8 * rim), parcel, parcelDetail);
    float hedge = ScLine(edgeM, 18.0, fp) * ScDetail(220.0, fp) * step(0.45, pv);
    farmed = lerp(farmed, SRGBToLinear(ScHedge), hedge * 0.7);

    float fields = land.r * shore * smoothstep(0.08, 0.22, patch * 0.6 + land.r * 0.5);
    float3 c = lerp(g, farmed, fields);

    // Garrigue: the wet unfarmed hills turn from the biome's gold to patches of stony soil and grass (promo 03's hills).
    float garrigue = land.b * shore * (1.0 - fields) * smoothstep(80.0, 250.0, elevM);
    float3 hillGround = lerp(SRGBToLinear(ScHillSoil * ScPaletteBrightness), SRGBToLinear(ScHillGrass * ScPaletteBrightness),
        smoothstep(0.35, 0.65, patch));
    c = lerp(c, hillGround, garrigue * 0.7);

    // Terraces on the wet hill slopes (in patches): a light stone riser every ScTerraceStepM of real rise, each tread darker
    // at its foot.
    float hill = land.b * shore * smoothstep(0.05, 0.10, slope) * (1.0 - smoothstep(0.45, 0.65, slope))
               * smoothstep(120.0, 320.0, elevM) * (1.0 - fields) * smoothstep(0.55, 0.70, 1.0 - patch);
    float terraceAmp = 1.0 - smoothstep(0.35, 0.8, terraceFp);
    float tf = frac(terraceU);
    float riser = smoothstep(0.72, 0.72 + max(terraceFp, 0.1), tf);
    float3 terraced = c * lerp(0.78, 1.02, tf);
    terraced = lerp(terraced, SRGBToLinear(ScStone * ScPaletteBrightness), riser * 0.35);
    c = lerp(c, lerp(c * 0.97, terraced, terraceAmp), hill);

    // Scrub on the unfarmed wet ground. Bushes: a jittered dot in about half of the 130 m cells, 32-56 m across, faded to
    // their mean cover far out.
    float2 sc = mapM / 130.0, si = floor(sc);
    float sh = ScHash(si + 41.0);
    float bushM = length(frac(sc) - (float2(sh, ScHash(si + 77.0)) * 0.5 + 0.25)) * 130.0;
    float bush = ScLine(bushM, 16.0 + 12.0 * sh, fp) * step(0.45, ScHash(si * 1.7 + 3.0));
    float speck = lerp(0.14, bush, ScDetail(130.0, fp));
    float mottle = smoothstep(0.55, 0.85, patch);
    float scrub = land.b * shore * (1.0 - fields) * (0.8 * speck + 0.2 * mottle) * (1.0 - smoothstep(0.5, 0.8, slope));
    c = lerp(c, SRGBToLinear(ScScrub * ScPaletteBrightness), scrub);

    // Dirt tracks: the baked links between towns, and the edges of farmed blocks (about a third of them).
    float dTrack = length((trackOffset * 255.0 - 127.0) / 127.0) * ScTrackRangeM;   // tools/land_bake.py encode_offsets
    float edgeOn = step(0.65, ScHash(floor(bp + 0.5) + bi.yx * 0.31)) * fields;
    float track = max(ScLine(dTrack, ScTrackHalfM, fp), ScLine(blockEdgeM, 30.0, fp) * edgeOn * ScDetail(300.0, fp)) * shore;
    c = lerp(c, SRGBToLinear(ScDust * ScPaletteBrightness) * (0.96 + 0.06 * pv), track * 0.85);
    return c;
}

#endif
