#ifndef STATECRAFT_LAND_INCLUDED
#define STATECRAFT_LAND_INCLUDED
// Land use on the ground (issue #33, promo-land): the patchwork of the promo's gameplay zoom (promo/final/03, 02), laid
// over the biome colour between ScGroundBase and ScGroundShade, so the relief still shades it. tools/land_bake.py bakes
// it all into landuse.png: the land's colour (field parcels with soft rims and hedges, farm tracks along the blocks'
// edges, garrigue, terraces, scrub) and its weight in alpha, placed by real data (slope, rainfall, towns). The roads and
// railways are not here: they are the real 1948 network, drawn by the roads track.
// Kept to a lerp on purpose: the same pattern as procedural shader code (about 150 instructions) cost about 5 ms a
// frame at gameplay03 in WebGL on the development Mac even where a branch skipped it, most likely register pressure /
// occupancy from the shader's size rather than its arithmetic (docs/visual-v1.md, "Promo track: land use").

float _ScLandOn;                                // 1 = land use drawn, 0 = the bare biome ground (the "land" toggle)

// land: landuse.png (rgb the land's colour, linear once the sampler decodes the sRGB texture; a its weight). The land
// reaches the waterline (coast 0.5): promo-sea's beach band (ScShore, drawn after) covers it where there is a beach, and
// fields run to the water where there is none (the Sea of Galilee, cliffs).
float3 ScLandUse(float3 g, float4 land, float coast)
{
    return lerp(g, land.rgb, land.a * smoothstep(0.499, 0.503, coast));
}

#endif
