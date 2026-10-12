#ifndef STATECRAFT_TERRAIN_INCLUDED
#define STATECRAFT_TERRAIN_INCLUDED
#include "Assets/Shaders/StatecraftMap.hlsl"
#include "Assets/Shaders/StatecraftLand.hlsl"

// The Custom Function of StatecraftTerrain.shadergraph (written by tools/terrain_graph.py; keep this signature in step with
// its INPUTS and OUTPUTS). UV is the terrain UV (0..1 over the map); PositionWS and NormalWS are absolute world space;
// Bake is terrain_bake.png (g curvature, b sky-view; its r is not read); Coast is coast.png (0.5 = shore, lower = water);
// Ao is ao.png, the ambient occlusion on its own, uncompressed (BC1 banded it: Task 6 fix round 1); Land is landuse.png
// (tools/land_bake.py: rgb the land's colour, sRGB-decoded by the sampler, a its weight), read by ScLandUse
// (StatecraftLand.hlsl).
void StatecraftTerrain_float(float2 UV, float3 PositionWS, float3 NormalWS, UnityTexture2D Bake, UnityTexture2D Coast, UnityTexture2D Ao, UnityTexture2D Land,
    out float3 BaseColor, out float3 NormalOut, out float Occlusion, out float Smoothness, out float Metallic, out float3 Emission)
{
    float3 n = normalize(NormalWS);
    float4 bake = SAMPLE_TEXTURE2D(Bake.tex, Bake.samplerstate, UV);
    bake.r = SAMPLE_TEXTURE2D(Ao.tex, Ao.samplerstate, UV).r;
    float coast = SAMPLE_TEXTURE2D(Coast.tex, Coast.samplerstate, UV).r;
    float2 mapM = UV * _ScMap.xy;
    // The pixel's footprint (m), for the water's chop and breakers, taken here in uniform
    // control flow (derivatives are undefined inside the water branch).
    float footprintM = max(length(ddx(mapM)), length(ddy(mapM)));
    // ScGround with the land use (StatecraftLand.hlsl) laid between its colour and its shading: curvature, AO and breakup
    // shade the fields too. The "land" toggle lerps it away (it does not skip the sample: a visual switch, not a price).
    float4 land = SAMPLE_TEXTURE2D(Land.tex, Land.samplerstate, UV);
    float3 ground = ScGroundBase(UV, mapM, n, PositionWS);
    ground = lerp(ground, ScLandUse(ground, land, coast), _ScLandOn);
    ground = ScGroundShade(ground, mapM, bake);
    float h = _ScHaze.a * ScHazeAmount(PositionWS);                 // land haze, applied after lighting via Emission
    float ao = bake.r * lerp(0.6, 1.0, bake.b);
    float water = ScWater(coast);
    // Water: ScWaterShade's colour (sunlit with its shadows, then the cool water haze) as emission on a black, fully metallic,
    // fully occluded surface, so Lit adds nothing (no diffuse, specular colour 0, indirect light times occlusion 0): exactly
    // what Sea.shader returns past the edge. Land keeps its after-lighting haze through the same Emission.
    float elevM = (PositionWS.y - _ScMap.z) / _ScMap.w;
    // Land pixels (water 0, spatially coherent) skip the water shading: its noise and second shadow lookup would only be
    // multiplied away (Task 7 fix round 1, M3).
    float2 breakers = ScBreakers(coast, mapM);
    float3 waterColour = 0;
    // The branch is taken by whole 2x2 pixel quads (ddx/ddy of water is non-zero when a quad straddles the shore): the water
    // code inside reads derivatives and shadow maps, which are undefined for the lanes a diverged quad leaves out. On a Metal
    // GPU that left a few shoreline pixels one 8-bit step off, differently on every render (flaky BorderTests). Land lanes of a
    // shore quad shade water and discard it: water is 0 for them below.
    [branch] if (water > 0.0 || abs(ddx(water)) + abs(ddy(water)) > 0.0)
        waterColour = ScHazeWater(ScWaterShade(coast, elevM, PositionWS, mapM, footprintM, breakers), PositionWS);
    ground = ScShore(ground, coast, elevM, n, mapM);   // the beach and the Dead Sea's salt flats (promo-sea)
    // The hex grid (visual v1 Task 8), light and faint on land and water, thinner towards the overview: the same lines as
    // Sea.shader past the edge. Computed outside the branch: its fwidth needs uniform control flow.
    float gridLine = ScGridAmount(mapM, PositionWS);
    ground = lerp(ground, SRGBToLinear(ScGridLandSrgb), gridLine * _ScGrid.x);
    waterColour = lerp(waterColour, SRGBToLinear(ScGridWaterSrgb), gridLine * _ScGrid.y);
    BaseColor = ground * (1.0 - h) * (1.0 - water);
    NormalOut = normalize(lerp(n, float3(0, 1, 0), water));
    Occlusion = lerp(lerp(1.0, ao, 0.8), 0.0, water);
    Smoothness = 0.0;
    Metallic = water;
    Emission = lerp(_ScHaze.rgb * h, waterColour, water);
    ScBorderApply(mapM, water, BaseColor, Emission);   // region borders (StatecraftBorders.hlsl), last: unlit, on land only
}

#endif
