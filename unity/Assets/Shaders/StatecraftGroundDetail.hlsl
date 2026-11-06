#ifndef STATECRAFT_GROUND_DETAIL_INCLUDED
#define STATECRAFT_GROUND_DETAIL_INCLUDED
// What GroundDetail.shader (the pass over the terrain) and GroundScrub.shader (the low 3D bushes) share (#2878): where scrub
// grows, one rule, so the painted bushes and the standing ones gather together. Needs StatecraftMap.hlsl first.
TEXTURE2D(_ScDetailTex);  SAMPLER(sampler_ScDetailTex);    // GroundDetail.BuildTexture
TEXTURE2D(_ScDetailClassTex); SAMPLER(sampler_ScDetailClassTex);   // GroundDetail.ClassOf per mesh step: r, g, b, a
TEXTURE2D(_ScDetailKeepOutTex); SAMPLER(sampler_ScDetailKeepOutTex);   // 1 on the towns, the routes and the units' clearings
TEXTURE2D(_ScDetailLandTex); SAMPLER(sampler_ScDetailLandTex);     // landuse.png
TEXTURE2D(_ScDetailCoastTex); SAMPLER(sampler_ScDetailCoastTex);   // coast.png
TEXTURE2D(_ScDetailBakeTex); SAMPLER(sampler_ScDetailBakeTex);     // terrain_bake.png
float _ScDetailTileM;     // GroundDetail.TileM
float4 _ScDetailFade;     // x, y the pixel footprint (m) where the painted detail starts to fade and is gone

// How much of the beach's width (ScShore's strip, up to 1.4 ScBeachM wide) is kept clear of scrub, as a multiple of ScBeachM.
static const float ScScrubOffBeach = 1.5, ScScrubOffDunes = 1.5;
// The land's luma (linear) under which a texel is a hedge, fading to none at y; the scrub density on a hedge.
static const float2 ScHedgeLuma = float2(0.045, 0.09);
static const float ScHedgeScrub = 0.85;

// 0 on the water and the beach, 1 inland (ScShore draws the sand; as the trees do, the owner: "keep them off the sand"). The dune coasts'
// ground stays sand farther in (DuneCoasts: Jaffa south, Haifa Bay).
float ScOffTheBeach(float coast, float2 mapM)
{
    float landM = saturate(coast - 0.5) * 2.0 * ScCoastRangeM;
    float from = (ScScrubOffBeach + ScScrubOffDunes * ScDuneWeight(mapM)) * ScBeachM;
    return smoothstep(from, from + 0.4 * ScBeachM, landM);
}

// Scrub density 0..1 at a point. The class holds the biomes' shares: r where the land use weighs high (the wet hills: its
// weight comes from rainfall and slope, tools/land_bake.py), g where it weighs low (the plains' ground between the fields).
// Maquis gathers: a broad noise (the grain's coarsest octave, about 2.5 km across this far up its mips) leaves thickets and
// bare ground. It thins into the gullies (the bake's curvature: 0.5 flat, lower a hollow). Every read
// is at one LOD, so the vertex shader can call it too.
half ScScrubDensity(float2 uv, float2 mapM, half4 cls, float4 land, float coast)
{
    // The dry south's trace of land use grows nothing: its weight is rainfall's, and the bushes' ranks would show every trace.
    half scrub = saturate(cls.r * smoothstep(0.08, 0.30, land.a) + cls.g * (1.0 - land.a));
    half broad = SAMPLE_TEXTURE2D_LOD(_ScDetailTex, sampler_ScDetailTex, mapM / (3.3 * _ScDetailTileM), 3.0).r;
    scrub *= smoothstep(0.40, 0.58, broad + 0.20 * scrub);
    scrub *= smoothstep(0.36, 0.50, SAMPLE_TEXTURE2D_LOD(_ScDetailBakeTex, sampler_ScDetailBakeTex, uv, 0.0).g);
    // The land bake's hedges and tree lines along the parcels' edges: its darkest colour, so bushes stand on the painted lines.
    half hedge = (1.0 - smoothstep(ScHedgeLuma.x, ScHedgeLuma.y, dot(land.rgb, half3(0.2126, 0.7152, 0.0722)))) * smoothstep(0.4, 0.7, land.a);
    scrub = max(scrub, ScHedgeScrub * hedge * saturate(cls.r + cls.g));
    scrub *= 1.0 - SAMPLE_TEXTURE2D_LOD(_ScDetailKeepOutTex, sampler_ScDetailKeepOutTex, uv, 0.0).r;
    return scrub;
}

// 0 on a drawn region border's band, 1 clear of it: the stripes stay clean (they are the terrain's, so this pass
// would stain them). The terrain's own lookup (ScBorderApply), one texel a pixel, near a hex edge only.
float ScClearOfBorder(float2 mapM, float footprint)
{
    if (_ScBorders.w < 0.5) return 1.0;
    float2 p = float2(mapM.x, _ScMap.y - mapM.y), v;
    float size = max(_ScMapOrigin.w, 1.0), inradius = 0.8660254 * size;
    float2 qr = ScHexAxial(p, size, v);
    float band = max(_ScBorderShape1.x, max(_ScBorderShape2.x, _ScBorderShape3.x)) + 3.0 * footprint;
    float edge = inradius - max(abs(v.x), max(abs(0.5 * v.x + 0.8660254 * v.y), abs(0.5 * v.x - 0.8660254 * v.y)));
    if (edge > band) return 1.0;
    int mask = (int)ScRegionAt(qr).z;
    float nearest = 1e9;
    for (int k = 0; k < 6; k++)
        if ((mask >> k) & 1) nearest = min(nearest, inradius - dot(v, ScHexNormals[k]));
    return smoothstep(band, 1.2 * band, nearest);
}
#endif
