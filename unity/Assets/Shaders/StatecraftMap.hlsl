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

// Gradient noise and its derivative, (value, d/dp.x, d/dp.y): random unit gradients at the lattice corners, blended with
// the quintic fade, so the field and its slope are smooth across cell edges (bug B10: the waves' value-noise differences
// jumped at the lattice lines, a checker up close). The value is in about [-0.7, 0.7].
float3 ScGradientNoiseD(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float2 du = 30.0 * f * f * (f * (f - 2.0) + 1.0);
    float ha = ScHash(i) * 6.2831853, hb = ScHash(i + float2(1, 0)) * 6.2831853;
    float hc = ScHash(i + float2(0, 1)) * 6.2831853, hd = ScHash(i + float2(1, 1)) * 6.2831853;
    float2 ga = float2(cos(ha), sin(ha)), gb = float2(cos(hb), sin(hb)), gc = float2(cos(hc), sin(hc)), gd = float2(cos(hd), sin(hd));
    float va = dot(ga, f), vb = dot(gb, f - float2(1, 0)), vc = dot(gc, f - float2(0, 1)), vd = dot(gd, f - float2(1, 1));
    float k = va - vb - vc + vd;
    float v = va + u.x * (vb - va) + u.y * (vc - va) + u.x * u.y * k;
    float2 d = ga + u.x * (gb - ga) + u.y * (gc - ga) + u.x * u.y * (ga - gb - gc + gd) + du * (u.yx * k + float2(vb, vc) - va);
    return float3(v, d);
}

// The slope of a gradient-noise wave layer at mapM: its lattice turned by `angle` (so no two layers, and neither, line up
// with the map's axes or the hex grid) and drifting along `drift` (cells per second), the slope turned back to map axes.
float2 ScWaveSlope(float2 mapM, float cellM, float angle, float2 drift, float time)
{
    float s, c; sincos(angle, s, c);
    float2 p = mapM / cellM;
    float2 d = ScGradientNoiseD(float2(c * p.x - s * p.y, s * p.x + c * p.y) + drift * time).yz;
    return float2(c * d.x + s * d.y, -s * d.x + c * d.y);
}

// Low in the rift the haze grows by up to this share (1.5 saturated the rift at the country-wide zoom: Task 6 review I1).
static const float ScRiftHazeBoost = 0.6;

// Aerial perspective, 0..1 before each surface's strength: none within the start distance, then 1 - exp(-(d - start) /
// scale), and up to 1.6x low in the rift. It is applied after lighting on both surfaces (review I2): land outputs BaseColor x (1 - h) plus Emission =
// haze colour x h, so Lit's result is lerp(lit ground, haze, h); water lerps its final colour (ScHazeWater).
float ScHazeAmount(float3 positionWS, float lead = 0.0)
{
    float dist = length(_WorldSpaceCameraPos - positionWS);
    float aerial = 1.0 - exp(-max(dist - (_ScHazeRange.w - lead), 0.0) / max(_ScHazeRange.z, 1.0));
    float low = saturate((_ScHazeRange.x - positionWS.y) / max(_ScHazeRange.x - _ScHazeRange.y, 1.0));
    return saturate(aerial * (1.0 + ScRiftHazeBoost * low));
}

// Sea round 2 (#388, S4): the water's haze starts ScWaterHazeLeadM nearer than the land's (from 120 km, not 200 km), so
// the overview's sea pales towards the horizon as promo 02's does; the gameplay zoom (under 100 km) still has none.
static const float ScWaterHazeLeadM = 80000.0;
float3 ScHazeWater(float3 colour, float3 positionWS)
{
    return lerp(colour, _ScHazeWater.rgb, _ScHazeWater.a * ScHazeAmount(positionWS, ScWaterHazeLeadM));
}

// 1 in water, 0 on land; coast is the signed distance texture (0.5 at the shore, tools/terrain_bake.py).
float ScWater(float coast) { return 1.0 - smoothstep(0.497, 0.503, coast); }

static const float ScCoastRangeM = 12000.0;   // tools/terrain_bake.py COAST_RANGE_M
float _ScWaterOn;                              // 1 = shaded water and shore, 0 = the flat sea colour, no shore (the "water" toggle)
float4 _ScWaterTime;                           // y = 1: the water's clock is x seconds (tests and frame captures); else _Time.y

// Depth from the shore (promo-sea). The map has no bathymetry: the heightmap is Copernicus GLO-30, a land model whose sea
// texels are 0 m, so the water's depth is read from the distance to the shore (coast.png). To first order that is the
// Israeli shelf: sandy shallows for the first kilometre or two, the 100 m isobath some 10-20 km out. Turquoise shallows
// ScShallowM wide (the edge wanders +-35% with a 3 km noise, so it never reads as a ruled stripe), teal, then the open
// sea's steel blue towards ScCoastRangeM. sRGB, before the grade: a clearly turquoise band (linear weight, so it reads at
// its width) over a muted deep blue, so the graded sea matches the promo in its spread, not only its mean (promo 03 sea
// pixels: saturation 0.49; Task 7's ramp graded to 0.81, open item 3).
static const float ScShallowM = 2600.0;
static const float3 ScShallowSrgb = float3(0.40, 0.72, 0.70);
static const float3 ScMidSrgb = float3(0.29, 0.50, 0.59);
static const float3 ScDeepSrgb = float3(0.27, 0.40, 0.54);
static const float3 ScFlatSeaSrgb = float3(0.20, 0.36, 0.47);    // the "water" toggle's flat sea (x 0.8)
static const float3 ScGalileeSrgb = float3(0.20, 0.45, 0.55);    // fresh water (-300..-100 m): the Sea of Galilee
static const float ScChopGain = 1.4;                             // the chop's share of the wave tilt against the swell's
static const float ScWaveSlopeGain = 0.22;                       // gradient-noise slope -> the old layers' strength
static const float ScWaveTiltFar = 0.10;                         // ... and beyond a 120 m pixel (Task 7: 0.15 everywhere)
static const float ScWaveTilt = 0.6;                             // wave normal tilt at the gameplay zoom (Task 7: 0.15)
// The surf: a swash line along the sand (full to ScSwashM) and breakers rolling shoreward through the surf zone
// (ScSurfM), broken along the shore by noise. The breakers are a squared triangle wave in the distance from the shore,
// box-filtered over the pixel's footprint in closed form, so they never alias and tend to their mean (1/3) as the pixel
// grows; ScBreakerRate cycles per second roll them in at ScBreakerSpacingM x ScBreakerRate = 22.5 m/s (half a pixel per
// frame at the gameplay pose's ~45 m pixels and 60 fps). coast.png's 94 m levels are interpolated linearly between
// texels, so a crest can sit up to ~47 m off its true place: about one gameplay pixel, under the phase noise.
// Sea round 3 (#388): towards promo 03's lace (2-4 broken white lines over the turquoise; SeaTests guarantees the swash
// and at least one breaker, the rest waits for #519's grade). The surf zone from 900 to 1800 m (the
// promo's 29-52 px foam band at gameplay03's ~45 m pixels; at 1500 m the second crest faded under the foam's white before
// it reached the turquoise), the crests from 300 to 450 m apart (about 10 px, so they stand clear of each other;
// ScBreakerRate kept, so they roll in at 22.5 m/s, about half a pixel a frame), and the foam whiter (ScFoamWhite, 0.85
// before: the crests read turquoise-tinted).
// The lace (the "foam" toggle, _ScSeaFoam): the constants alone left whole crests out where the 300 m "broken" noise was
// low, since one blob of it covers two crests 450 m apart (SeaTests.TheSurfShowsABrokenBreakerBeyondTheSwashAtTheGameplayZoom
// at 31.95 N).
// With the toggle on, a finer net (ScLaceCellM cells, kept where it is under ScLaceCover) breaks each crest on its own.
// Its edge is a fixed ScLaceEdge wide in noise units, not the pixel's footprint: an edge that sharpens with resolution
// changes the net's mean between a pixel and its sub-pixels, and TheBreakersAreFilteredToThePixel saw it (p99 0.067 at a
// 140 m net with a footprint edge); a 200 m net with a 0.25 edge stays smooth over a 45 m pixel. The net keeps more than
// "broken" does: its mean is 0.676 (2M samples of ScNoise; P(noise < 0.62) = 0.69), "broken"'s 0.5, so the breakers
// carry about 35% more foam on average at the gameplay zoom. Like "broken", it fades to 0.5 at 75-150 m pixels, so the
// overview's mean foam is unchanged, and zooming out through that range dims the breakers by about a quarter
// (independent review of #660; kept rather than re-centred, which would change the overview).
float _ScSeaFoam;                                                // 1 = on, 0 = off (the "foam" toggle)
static const float ScSwashM = 180.0, ScSurfM = 1800.0, ScBreakerSpacingM = 450.0, ScBreakerRate = 0.05, ScFoamWhite = 0.95;
static const float ScLaceCellM = 200.0, ScLaceCover = 0.62, ScLaceEdge = 0.25;
// Sea round 2 (#388): the far sea's texture, which promo 02 and 03 show at the gameplay and overview zooms where the waves
// above have faded to a smooth surface (sea detail 0.013 against promo 03's 0.060 at gameplay03, 0.019 against 0.063 at
// diorama02). Three terms, all behind the "seatexture" toggle (_ScSeaTexture), so the profile prices them together and
// the seam test (SettlementTests.TheSeaBeyondTheEdgeMatchesTheTerrainSea) pins them off; its step test
// (TheFarSeaTextureHasNoStepAtTheMapEdge) checks the edge with them on.
// - The mottle: wind streaks, a slow gradient-noise brightness variation, ScMottleCellM cells, 3x narrower across the
//   wind (ScMottleStretch: 833 m), with a finer octave (2.7x: about 309 m), +-ScMottleGain, on the open Mediterranean (not
//   the shallows, the surf or the rift lakes). It is what the overview sees. Neither octave is faded by the footprint.
// - The ripple: the waves' slope towards or away from the camera, as their sky reflection. Under a 50 m pixel it uses the
//   tilted normal (swell and chop), +-ScRippleGain, faded out over 50-90 m (the 170 m chop's limit is 85 m). From 90 m it
//   uses the 700 m swell's own slope, +-ScRippleFarGain, faded over 250-450 m, so it is still at half strength at the
//   swell's 350 m limit, and at 84-100% at diorama02's 190-300 m pixels (2.3-3.7 px a swell cell).
//   Review of #402: a slow pan and zoom at diorama02 (motion-compensated, against main) showed no crawl. In focus, the
//   texture moves coherently (a residual of 19% of its fine texture; main 38%). In the far band, under the tilt-shift,
//   what is left is under one 8-bit level.
// - Whitecaps: a fleck in some ScFleckCellM cells on the streaks' bright side (none in the troughs), its brightness the
//   share of the pixel it covers; gone by an 80 m pixel.
float _ScSeaTexture;                                             // 1 = on, 0 = off (the "seatexture" toggle)
static const float ScMottleCellM = 2500.0, ScMottleGain = 0.32, ScMottleStretch = 3.0;
static const float ScRippleGain = 0.54, ScRippleScale = 5.0, ScRippleFarGain = 0.28, ScRippleFarScale = 4.0;
static const float ScFleckCellM = 120.0, ScFleckM = 18.0, ScFleckShare = 0.22;
// Sea boats (#388, the 2026-10-13 gap audit): the open sea calms away from the shore at the gameplay zoom. At gameplay03
// the far sea's fine texture (the ripple, the mottle's fine octave, the whitecaps and the glint) was 4x busier than promo
// 03's open water (edge density 0.048 against 0.012), whose texture is the surf and the near-coast chop, with broad,
// smooth swells beyond. All of it is behind the "seacalm" toggle (_ScSeaCalm), and the calm and swells also behind
// "seatexture", on the open Mediterranean only (not the rift lakes).
// - The zoom, not the pixel: the calm and the swells fade out, and the overview's shelf fades in, with the water's
//   distance from the camera (ScZoomFades), so a 1440p canvas or a device pixel ratio of 2 sees the same sea as 1080p.
//   The gameplay pose's water lies 50-90 km from the camera, the overview's (CameraController.MaxDistance, 250 km)
//   205-360 km.
// - The calm: from ScCalmNearM offshore (past the turquoise shallows, whose edge reaches at most 3.5 km, and the surf)
//   to ScCalmFarM, the ripple, the mottle's fine octave and the glint ease to their ScCalm* shares and the whitecaps go;
//   the coarse streaks grow by ScCalmCoarse. Inside ScCalmNearM, and at the overview, nothing changes: #402's surf,
//   shallows and near-coast chop stay as they were.
// - Broad swells: a ScBroadCellM gradient-noise brightness variation, +-ScBroadGain, on the same far sea and zooms as the
//   calm: promo 03's open water lies in broad lighter and darker reaches. At the gameplay zoom a cell spans some 300
//   pixels; at the overview, where its cells read as blotches, it is off.
// - The coastal shelf at the overview: the water within ScCoastRangeM of the shore lightens towards ScOverviewCoastSrgb
//   (by ScOverviewBand, times how far past the shallows' gradient it lies, fading to none at the range), so diorama02's
//   sea reads as promo 02's lighter shelf over the deep blue. It tints the shallows' outer gradient a little at the
//   overview; the gameplay zoom has none of it.
float _ScSeaCalm;                                                // 1 = on, 0 = off (the "seacalm" toggle)
static const float ScCalmNearM = 3600.0, ScCalmFarM = 8000.0;
static const float ScCalmRipple = 0.3, ScCalmFine = 0.3, ScCalmGlint = 0.4, ScCalmCoarse = 1.3;
static const float ScBroadCellM = 14000.0, ScBroadGain = 0.25;
static const float3 ScOverviewCoastSrgb = float3(0.36, 0.62, 0.66);
static const float ScOverviewBand = 0.6;
// Camera distances (m): the calm and swells fade out over (x, y), the shelf fades in over (z, w).
static const float4 ScZoomFades = float4(110000.0, 190000.0, 150000.0, 220000.0);

// Antiderivative of tri(x)^2, tri(x) = 1 - |2 frac(x) - 1| (period 1, mean 1/3).
float ScTriSqIntegral(float x)
{
    float t = frac(x);
    float g = t < 0.5 ? 4.0 * t * t * t / 3.0 : 1.0 / 3.0 - 4.0 * (1.0 - t) * (1.0 - t) * (1.0 - t) / 3.0;
    return floor(x) / 3.0 + g;
}

// tri(x)^2 averaged over [x - w/2, x + w/2].
float ScTriSqBox(float x, float w)
{
    w = max(w, 1e-3);
    return (ScTriSqIntegral(x + 0.5 * w) - ScTriSqIntegral(x - 0.5 * w)) / w;
}

// The water's clock: Unity's time, or the pinned seconds (MapShaderGlobals.PinWaterTime).
float ScWaterClock() { return _ScWaterTime.y > 0.5 ? _ScWaterTime.x : _Time.y; }

// The breakers' phase (cycles; crests at whole numbers) and its box width: fwidth(phase) x ScBreakerFilterPx. The phase's
// true gradient is not 1 / ScBreakerSpacingM: its noise term adds up to 0.0025 cycles/m and the 8-bit coast's slope varies
// up to 2x, so the width comes from the phase itself (review N2). Call it in uniform control flow (outside any [branch]);
// pass the result to ScWaterShade.
static const float ScBreakerFilterPx = 1.25;
float2 ScBreakers(float coast, float2 mapM)
{
    float offshoreM = saturate(0.5 - coast) * 2.0 * ScCoastRangeM;
    float phase = offshoreM / ScBreakerSpacingM + ScWaterClock() * ScBreakerRate + ScNoise(mapM / 1200.0) * 2.0;
    return float2(phase, fwidth(phase) * ScBreakerFilterPx);
}

// The water's lit colour before haze (callers apply ScHazeWater and output it as is): the depth ramp above; the Dead Sea
// (below -300 m) pale turquoise with a white salt rim, the Sea of Galilee a fresher teal; a long swell and a finer chop
// (scrolling noise, the chop faded by the footprint) tilt a wave normal for Lambert from the sun (with its shadows: cliffs
// shade the water) plus sky fill and a small glint; the surf along the shore. elevM is the real elevation of the water
// surface; mapM metres east and north of the map's south-west corner; footprintM the metres one pixel covers (from fwidth,
// taken by the caller in uniform control flow); breakers ScBreakers' (phase, box width).
float3 ScWaterShade(float coast, float elevM, float3 positionWS, float2 mapM, float footprintM, float2 breakers)
{
    if (_ScWaterOn < 0.5) return SRGBToLinear(ScFlatSeaSrgb) * 0.8;
    float offshoreM = saturate(0.5 - coast) * 2.0 * ScCoastRangeM;
    float shallowEdge = ScShallowM * (0.65 + 0.7 * ScNoise(mapM / 3000.0));
    float shallow = 1.0 - smoothstep(0.0, shallowEdge, offshoreM);
    float deepT = pow(smoothstep(0.0, ScCoastRangeM, offshoreM), 0.6);
    float3 c = lerp(SRGBToLinear(ScMidSrgb), SRGBToLinear(ScDeepSrgb), deepT);
    c = lerp(c, SRGBToLinear(ScShallowSrgb), shallow);
    float salt = step(elevM, -300.0);
    float fresh = step(elevM, -100.0) * (1.0 - salt);
    c = lerp(c, SRGBToLinear(float3(0.45, 0.78, 0.78)), salt * 0.8);
    c = lerp(c, SRGBToLinear(ScGalileeSrgb), fresh * 0.7);
    // Sea boats (#388): the zoom (the water's distance from the camera, not its pixel size) and the overview's shelf.
    float viewM = length(_WorldSpaceCameraPos - positionWS);
    float gameplay = _ScSeaCalm * (1.0 - smoothstep(ScZoomFades.x, ScZoomFades.y, viewM));
    float overview = _ScSeaCalm * smoothstep(ScZoomFades.z, ScZoomFades.w, viewM);
    c = lerp(c, SRGBToLinear(ScOverviewCoastSrgb), overview * ScOverviewBand * (1.0 - deepT) * (1.0 - shallow) * (1.0 - salt) * (1.0 - fresh));

    float time = ScWaterClock();
    // The swell: a 700 m gradient-noise layer turned 0.6 rad. Bug B10: it was value noise differenced along the map's axes
    // (on an axis-aligned lattice), whose slope jumped at every cell edge; ScWaveSlopeGain scales the smooth slope to the
    // old layers' strength.
    float2 g = ScWaveSlopeGain * ScWaveSlope(mapM, 700.0, 0.6, float2(0.05, 0.02), time);
    float2 swellSlope = g;   // the far ripple's (sea round 2)
    // The chop: a 170 m layer turned -0.9 rad and moving the other way, full while a pixel covers under 40 m, gone by
    // 170 m (the gameplay pose sees it, the overview averages it away).
    float chop = 1.0 - smoothstep(40.0, 170.0, footprintM);
    g += ScChopGain * chop * ScWaveSlopeGain * ScWaveSlope(mapM, 170.0, -0.9, float2(-0.11, -0.07), time);
    // The wave tilt: ScWaveTilt near (pixels under 50 m: the gameplay zoom shows the chop and sparkle), easing to 0.10 by
    // 120 m, where a pixel spans a good part of a swell cell: the far sea reads as a calm surface, and patch means either
    // side of the map's edge stay within the seam test's 0.012 (at 0.6 a single 7x7 patch at 120 km carried 0.03; with the
    // gradient-noise swell (sea-fix) Task 7's far 0.15 left single patches up to 0.016 apart).
    float tilt = lerp(ScWaveTilt, ScWaveTiltFar, smoothstep(50.0, 120.0, footprintM));
    float3 n = normalize(float3(g.x * tilt, 1.0, g.y * tilt));
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
    // view-dependent gradient that read as a seam between samples 5 km apart either side of the map's edge. The glint is
    // the near zoom's sparkle: it fades out between 60 and 120 m pixels, where the calmer far sea turned it back into that
    // smooth warm gradient (sea-fix: up to 0.016 between the seam test's +-2.5 km patches with the gradient-noise waves).
    // Sea round 2: 0.12 under a 40 m pixel (the gameplay zoom's sparkle), easing to Task 7's 0.06 by 60 m, so nothing
    // changes from 60 m out (the seam test's 120 km view).
    float glint = lerp(0.12, 0.06, smoothstep(40.0, 60.0, footprintM)) * (1.0 - smoothstep(60.0, 120.0, footprintM));
    // Sea boats (#388): the calm, on the open sea (behind "seatexture" too) at the gameplay zoom, from ScCalmNearM out.
    float farOut = smoothstep(ScCalmNearM, ScCalmFarM, offshoreM) * _ScSeaTexture * (1.0 - salt) * (1.0 - fresh);
    float calm = farOut * gameplay;
    glint *= lerp(1.0, ScCalmGlint, calm);
    lit += pow(saturate(dot(reflect(-l, n), v)), 80.0) * glint * lc * shadow;

    // The far sea's texture (sea round 2, above): on open sea water only.
    float open = _ScSeaTexture * (1.0 - salt) * (1.0 - fresh);
    float2 viewXZ = normalize(v.xz + 1e-5);
    float nearRipple = 1.0 - smoothstep(50.0, 90.0, footprintM);
    float ripple = ScRippleGain * nearRipple * clamp(dot(n.xz, viewXZ) * ScRippleScale, -1.0, 1.0)
        + ScRippleFarGain * (1.0 - nearRipple) * (1.0 - smoothstep(250.0, 450.0, footprintM)) * clamp(dot(swellSlope, viewXZ) * ScRippleFarScale, -1.0, 1.0);
    ripple *= lerp(1.0, ScCalmRipple, calm);
    float ms, mc; sincos(0.3, ms, mc);
    float2 mp = mapM / ScMottleCellM;
    mp = float2(mc * mp.x - ms * mp.y, (ms * mp.x + mc * mp.y) * ScMottleStretch);   // streaks along the wind
    float mottle = lerp(1.0, ScCalmCoarse, calm) * ScGradientNoiseD(mp + float2(0.002, 0.001) * time).x
        + 0.8 * lerp(1.0, ScCalmFine, calm) * ScGradientNoiseD(mp * 2.7 + float2(5.3, 1.7)).x;
    mottle = clamp(mottle / 1.1, -1.0, 1.0);
    float offshore = smoothstep(0.0, shallowEdge, offshoreM);   // 0 in the turquoise shallows, 1 past them
    mottle *= offshore;
    // The shallows keep promo 03's pale turquoise: the ripple runs at 40% there (at full it darkened the band at 25 km).
    float broad = ScGradientNoiseD(mapM / ScBroadCellM + float2(11.3, 4.1)).x * calm;
    lit *= 1.0 + open * (ripple * lerp(0.4, 1.0, offshore) + ScMottleGain * mottle + ScBroadGain * broad);
    float2 cell = floor(mapM / ScFleckCellM);
    float2 at = (cell + 0.35 + 0.3 * float2(ScHash(cell + 3.7), ScHash(cell + 9.1))) * ScFleckCellM;
    float reach = max(ScFleckM, 0.6 * footprintM);
    float fleck = step(ScHash(cell + 17.1), ScFleckShare * saturate(mottle * 1.5)) * (ScFleckM * ScFleckM) / (reach * reach)
        * (1.0 - smoothstep(0.5 * reach, reach, length(mapM - at)))
        * (1.0 - smoothstep(40.0, 80.0, footprintM)) * smoothstep(300.0, 900.0, offshoreM) * (1.0 - calm) * open;

    // Surf: the swash line, then breakers (crests every ScBreakerSpacingM, rolling in: the phase grows with time towards
    // the shore; box-filtered over ScBreakers' width) whose crests break up along the coast (a 300 m noise, faded to its mean once a
    // pixel covers 75-150 m). Neither on the Dead Sea, which keeps its salt rim.
    // Sea round 2: the swash line is never under 1.5 pixels wide, so the white rim reads at the overview as well.
    float swashM = max(ScSwashM, 1.5 * footprintM);
    float swash = 1.0 - smoothstep(swashM, swashM * 1.8, offshoreM);
    float zone = 1.0 - smoothstep(ScSwashM, ScSurfM, offshoreM);
    float crest = ScTriSqBox(breakers.x, breakers.y);
    float broken = smoothstep(0.3, 0.7, ScNoise(mapM / 300.0 + float2(0.0, time * 0.02)));
    float lace = ScNoise(mapM / ScLaceCellM + float2(5.1, 2.3) + float2(time * 0.03, 0.0));
    lace = 1.0 - smoothstep(ScLaceCover - ScLaceEdge, ScLaceCover + ScLaceEdge, lace);   // kept where the net is under ScLaceCover
    broken = lerp(broken, lace, _ScSeaFoam);
    broken = lerp(0.5, broken, 1.0 - smoothstep(75.0, 150.0, footprintM));
    float rim = salt * (1.0 - smoothstep(0.0, 900.0, offshoreM));
    float foam = saturate((swash * (0.8 + 0.2 * broken) + zone * crest * broken) * (1.0 - salt) + rim + fleck);
    return lerp(lit, SRGBToLinear(float3(0.93, 0.95, 0.94)), foam * ScFoamWhite);
}

// The shore on land (promo-sea): a sand strip along every sea-level coast (above -100 m: the Mediterranean and the Gulf
// of Eilat, both sand beaches; not the rift lakes), wet and darker where the swash reaches, ScBeachM wide on average (+-40% with a 4 km noise) - symbolic width, as the promo's
// beach, wider than the real 30-100 m strip but within the coastal dune belt's kilometres; white salt flats along the
// Dead Sea (below -300 m). Both thresholds are 20 m ramps, so a shore slope crossing one leaves no hard contour. Steep
// faces (the Carmel's and the kurkar cliffs) keep their rock. Off with the "water" toggle (so its cost is in the
// toggle's).
static const float ScBeachM = 900.0;   // sea round 2 (#388): 650 m in promo-sea; promo 03's beach reads wider
static const float3 ScSandSrgb = float3(0.90, 0.80, 0.62), ScSaltSrgb = float3(0.92, 0.90, 0.85);   // sea round 2: drier, paler
float3 ScShore(float3 ground, float coast, float elevM, float3 n, float2 mapM)
{
    if (_ScWaterOn < 0.5) return ground;
    float landM = saturate(coast - 0.5) * 2.0 * ScCoastRangeM;
    float width = ScBeachM * (0.6 + 0.8 * ScNoise(mapM / 4000.0 + 7.3));
    float strip = (1.0 - smoothstep(width * 0.55, width, landM)) * (1.0 - smoothstep(0.25, 0.45, 1.0 - saturate(n.y)));
    float salt = 1.0 - smoothstep(-310.0, -290.0, elevM);
    float med = smoothstep(-110.0, -90.0, elevM);
    float3 sand = SRGBToLinear(ScSandSrgb) * lerp(0.94, 1.04, ScNoise(mapM / 350.0));
    sand = lerp(sand, sand * 0.78, 1.0 - smoothstep(60.0, 160.0, landM));          // wet sand at the waterline
    float3 shore = lerp(sand, SRGBToLinear(ScSaltSrgb), salt);
    return lerp(ground, shore, strip * saturate(med + salt));
}

// The painted ground: biome colour, warmer stone high up, a dusty rift floor, rock on steep slopes, two octaves of
// breakup, curvature (ridges light, hollows dark) and the baked occlusion (bake: r AO from ao.png, g curvature, b sky-view).
float3 ScGroundBase(float2 uv, float2 mapM, float3 n, float3 positionWS)
{
    float elevM = (positionWS.y - _ScMap.z) / _ScMap.w;            // real metres above sea level
    float slope = 1.0 - saturate(n.y);                             // 0 flat, 1 vertical
    float3 g = SAMPLE_TEXTURE2D(_ScBiomeTex, sampler_ScBiomeTex, uv).rgb;
    g = lerp(g, g * float3(1.08, 1.00, 0.86), smoothstep(500.0, 1100.0, elevM));
    g = lerp(g, g * float3(1.10, 1.04, 0.92), 1.0 - smoothstep(-350.0, -150.0, elevM));
    float rock = smoothstep(0.22, 0.45, slope + (ScNoise(mapM / 900.0) - 0.5) * 0.12);
    g = lerp(g, SRGBToLinear(float3(0.56, 0.49, 0.41)), rock);
    return g;
}

// ScGround's shading, split from its colour so the terrain can paint land use in between (StatecraftLand.hlsl).
float3 ScGroundShade(float3 g, float2 mapM, float4 bake)
{
    // The fine octave fades to its mean as a pixel's footprint nears its 520 m period, so it never aliases into shimmer.
    float fine = lerp(0.5, ScNoise(mapM / 520.0), 1.0 - saturate(length(fwidth(mapM)) / 520.0));
    g *= 0.93 + 0.14 * (ScNoise(mapM / 2600.0) * 0.6 + fine * 0.4);
    g *= 1.0 + 0.40 * (bake.g - 0.5) * 2.0;
    g *= lerp(1.0, bake.r * lerp(0.6, 1.0, bake.b), 0.45);
    return g;
}

float3 ScGround(float2 uv, float2 mapM, float3 n, float3 positionWS, float4 bake)
{
    return ScGroundShade(ScGroundBase(uv, mapM, n, positionWS), mapM, bake);
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

// Region borders (promo-borders): their globals and ScBorderApply, which the terrain calls last.
#include "Assets/Shaders/StatecraftBorders.hlsl"

#endif
