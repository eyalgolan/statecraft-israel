#ifndef STATECRAFT_BORDERS_INCLUDED
#define STATECRAFT_BORDERS_INCLUDED
// Region borders (promo-borders): the promo's two-tone outlines along hex edges, drawn inside each region along every edge
// it shares with a different region (as Civ draws them, so two outlined regions meet as a double band): a coloured stripe on
// the edge, a light stripe inside it, then a soft wash of the colour fading inwards, over a light tint of the whole region
// that grows as the camera pulls out (the Civ-like prototype for the owner, #196: MapBorders.Variant). Data driven: MapBorders (C#) bakes one
// texel per hex (_ScRegionTex) and sets the globals; with no region data, or the "borders" toggle off, _ScBorders.w is 0 and
// nothing is drawn. Included at the end of StatecraftMap.hlsl; the terrain calls ScBorderApply last.

TEXTURE2D(_ScRegionTex);  // one texel per hex, linear RGBA8: r + 256 g the region id (0 = none), b's low two bits the style
                          // (0 = none, 1, 2, 3) and its upper six which border edges the neighbour also draws in the same style (a
                          // shared edge: each side draws ScSharedBand of the band, so the two make one band centred on the edge), a's low
                          // six bits which edges border another region (MapBorders.Use bakes them; issue #195)
float4 _ScRegionInfo;     // x q of texel column 0, y r of texel row 0, z width, w height (texels)
float4 _ScBorders;        // x least colour stripe (px), y widest band (+ wash) as a share of the hex's inradius,
                          // z 1 = no early-out (tests prove the early-out exact), w 1 = on
float4 _ScBorderMin;      // x least light stripe (px), y least wash (px), z the wash's hold: 0 a smoothstep fall, 1 a 1 - x^2 one
float4 _ScBorderDash;     // the neutral style's dashes (a cue that is not colour, for colour-blind players: review S9 of #555):
                          // x metres a period, y the drawn share of it, z the least drawn dash (px)
float4 _ScBorderTint;     // x the territory tint near, y far; z the hex inradius (px) where it starts rising, w where it is full
float4 _ScBorderRim;      // the dark outer rim (variant B): x width (m), y least width (px), z opacity (0: none), w value (times the colour)
// Per style (1 primary: the Israeli side, 2 secondary: the other side, 3 neutral: contested, no-man's land, a
// demilitarized zone, or the 1947 sub-districts before any side is sourced): Colour rgb the edge stripe (linear); Light rgb
// the inner stripe; Shape x the band's width (m; colour + light stripes), y the colour stripe's share of it, z how far the
// wash reaches from the edge (a share of the hex's inradius), w its strength.
float4 _ScBorderColour1, _ScBorderLight1, _ScBorderShape1;
float4 _ScBorderColour2, _ScBorderLight2, _ScBorderShape2;
float4 _ScBorderColour3, _ScBorderLight3, _ScBorderShape3;

// The outward unit normals of a hex's edges (p space: x east, y south), in tools/hexgrid.py's NEIGHBOURS order (axial
// steps (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1): MapBorders.NeighbourSteps). Bit i of a hex's edge mask is edge i.
// The share of its band each side of a shared edge draws. At exactly half, each half loses its partly covered outer pixel
// and the pair measured 6 px at gameplay03 against a full band's 8 (promo 03: about 8); at 0.6 the two measure as one band.
static const float ScSharedBand = 0.6;

static const float2 ScHexNormals[6] = { float2(1, 0), float2(0.5, -0.8660254), float2(-0.5, -0.8660254), float2(-1, 0),
                                        float2(-0.5, 0.8660254), float2(0.5, 0.8660254) };

// Region id, style, border-edge mask and shared-edge mask of axial hex (q, r); 0 off the texture.
float4 ScRegionAt(float2 qr)
{
    int2 t = int2(qr - _ScRegionInfo.xy);
    if (t.x < 0 || t.y < 0 || t.x >= (int)_ScRegionInfo.z || t.y >= (int)_ScRegionInfo.w) return float4(0.0, 0.0, 0.0, 0.0);
    float4 v = LOAD_TEXTURE2D(_ScRegionTex, t) * 255.0;
    int b = (int)round(v.b);
    return float4(round(v.r) + 256.0 * round(v.g), (float)(b & 3), (float)((int)round(v.a) & 63), (float)((b >> 2) & 63));
}

// The axial hex containing p (cube rounding: ScHexEdgeDistance's layout, p.x east and p.y south of the north-west corner)
// and p's offset from that hex's centre.
float2 ScHexAxial(float2 p, float size, out float2 v)
{
    float qf = (0.57735027 * p.x - p.y / 3.0) / size;
    float rf = 0.66666667 * p.y / size;
    float sf = -qf - rf;
    float q = round(qf), r = round(rf), s = round(sf);
    float dq = abs(q - qf), dr = abs(r - rf), ds = abs(s - sf);
    if (dq > dr && dq > ds) q = -r - s; else if (dr > ds) r = -q - s;
    v = p - float2(size * (1.7320508 * q + 0.8660254 * r), size * 1.5 * r);
    return float2(q, r);
}


// ---- Legacy: main's borders, unchanged (the default until the owner picks a variant; MapBorders.Variant) ----
// The neutral style's dashes as main draws them: ScLegacyDashM metres a period, the first ScLegacyDashOn of it drawn.
static const float ScLegacyDashM = 2000.0, ScLegacyDashOn = 0.65;

// How far main's band and wash can reach from an edge (m): the wider style's band (or the least width in pixels, at up to
// 1.5x the footprint: the gradient's metres per pixel never exceed sqrt 2 of it) times 1 + its wash, plus a pixel for the
// filter. A pixel farther than that from every edge of its hex is in no band, so it fetches no texel (review: 7 fetches on
// every land pixel cost 3.3 ms at gameplay03).
float ScLegacyReach(float footprint)
{
    float washMul = 1.0 + max(max(_ScBorderShape1.z, _ScBorderShape2.z), _ScBorderShape3.z);
    return max(max(max(_ScBorderShape1.x, _ScBorderShape2.x), _ScBorderShape3.x), 1.5 * _ScBorders.x * footprint) * washMul + 1.5 * footprint;
}

// Main's drawing of one pixel (_ScBorderShape.z the wash's width times the band's, _ScBorders.x the least band width).
void ScBorderLegacy(float4 own, float2 v, float2 p, float2 px, float2 py, float size, float inradius, float reach,
                    float footprint, float water, inout float3 baseColor, inout float3 emission)
{
    int edges = (int)own.z, sharedEdges = (int)own.w;
    // A hex with no border edge draws nothing (the loop below would leave cover 0), so it skips the work.
    [branch] if (own.x > 0.5 && own.y > 0.5 && edges != 0)
    {
        // bestPx: the distance (px) to the nearest band, measured in units of that edge's band (a shared edge draws
        // ScSharedBand of it on each side), so where a shared and an unshared edge meet at a corner each keeps its own width.
        float bestPx = 1e9, bestAa = 1.0, bestHalf = 1.0, bestAlong = 0.0;
        [unroll] for (int i = 0; i < 6; i++)
        {
            float2 n = ScHexNormals[i];
            // Only the edges within reach count (one, or two near a corner). Whether edge i borders another region is
            // baked into the hex's own texel: the neighbours' six fetches here had pushed the terrain's fragment shader
            // past a WebGL cost cliff (about 5 ms a frame at gameplay03, borders on or off: issue #195).
            if (inradius - dot(v, n) >= reach) continue;
            if (((edges >> i) & 1) != 0)
            {
                // Distance to the edge segment (corners rounded, so two border edges join without a notch), in pixels
                // along the direction it grows (Task 8's anisotropic filter: metres per pixel across the line).
                float2 t = float2(-n.y, n.x);
                float2 closest = n * inradius + t * clamp(dot(v, t), -0.5 * size, 0.5 * size);
                float2 e = v - closest;
                float d = length(e);
                float2 g = d > 1e-3 ? e / d : -n;
                float aa = max(length(float2(dot(px, g), dot(py, g))), 1e-3);
                float share = ((sharedEdges >> i) & 1) != 0 ? ScSharedBand : 1.0;
                if (d / aa / share < bestPx / bestHalf) { bestPx = d / aa; bestAa = aa; bestHalf = share; bestAlong = dot(p, t); }
            }
        }
        bool primary = own.y < 1.5, neutral = own.y > 2.5;
        float4 colour = primary ? _ScBorderColour1 : (neutral ? _ScBorderColour3 : _ScBorderColour2);
        float4 light = primary ? _ScBorderLight1 : (neutral ? _ScBorderLight3 : _ScBorderLight2);
        float4 shape = primary ? _ScBorderShape1 : (neutral ? _ScBorderShape3 : _ScBorderShape2);
        // The band is shape.x metres wide (it grows towards the camera, like the promo's), never thinner than the least
        // width, and the band plus its wash never wider than _ScBorders.y of the inradius (far zooms: a hex stays a hex).
        float widest = _ScBorders.y * inradius / bestAa;
        float bandPx = min(max(shape.x / bestAa, _ScBorders.x), widest / (1.0 + shape.z));
        // Far off (a band under about 4 px: the overview), the light stripe would read as a white line, so it turns
        // into the colour and the wash fades: a far band is a thin line of the region's colour.
        float near = smoothstep(2.5, 4.5, bandPx);
        // A shared edge: this side draws ScSharedBand of the band (and of the wash), the neighbour as much again.
        bandPx *= bestHalf;
        float washPx = bandPx * shape.z;
        // Box-filtered stripes: the colour from the edge to shape.y of the band, the light stripe to its end.
        float cColour = saturate(shape.y * bandPx - bestPx + 0.5);
        float cBand = saturate(bandPx - bestPx + 0.5);
        // max(): a style with no wash (shape.z 0) must not make smoothstep divide by zero (review M3).
        float wash = near * shape.w * (1.0 - smoothstep(bandPx, bandPx + max(washPx, 1e-3), bestPx)) * (1.0 - cBand);
        float3 stripe = lerp(lerp(colour.rgb, light.rgb, near), colour.rgb, cBand > 0.0 ? cColour / cBand : 0.0);
        float cover = saturate(cBand + wash);
        float3 c = cBand > 0.0 || wash > 0.0 ? (stripe * cBand + colour.rgb * wash) / max(cover, 1e-4) : colour.rgb;
        float a = cover * (1.0 - water);
        if (neutral)
        {
            // box-filtered dash: the share of this pixel's footprint along the edge that falls on a drawn dash
            float ph = frac(bestAlong / ScLegacyDashM), w = max(footprint / ScLegacyDashM, 1e-4);
            float on = saturate((ScLegacyDashOn - ph) / w + 0.5) * saturate(ph / w + 0.5);
            a *= lerp(on, ScLegacyDashOn, saturate(w * 4.0 - 1.0));   // far off, where dashes are sub-pixel: their mean
        }
        baseColor *= 1.0 - a;
        emission = lerp(emission, c, a);
    }
}

// ---- The Civ-like prototype (variants A, B, C, A2, B2, C2; #196) ----
// A band's on-screen widths (px) across an edge at aa metres a pixel: x the rim, y the colour stripe, z the light stripe,
// w the wash, each held to its floor (_ScBorders.x, _ScBorderMin, _ScBorderRim.y) so the band keeps its weight at every
// zoom. The wash runs to shape.z of the inradius from the edge. Rim, band and wash together never pass _ScBorders.y of the
// inradius (a far hex is never filled by its outline): the wash gives way first, then both stripes shrink together, and
// the rim last. MapBorders.BandWidths mirrors it.
float4 ScBandWidths(float4 shape, float aa, float inradius)
{
    float widest = _ScBorders.y * inradius / aa;
    float rim = _ScBorderRim.z > 0.0 ? min(max(_ScBorderRim.x / aa, _ScBorderRim.y), widest) : 0.0;
    float room = widest - rim;
    float colourPx = max(shape.x * shape.y / aa, _ScBorders.x);
    float lightPx = max(shape.x * (1.0 - shape.y) / aa, _ScBorderMin.x);
    float washPx = max(shape.z * inradius / aa - colourPx - lightPx, _ScBorderMin.y);
    washPx = min(washPx, max(room - colourPx - lightPx, 0.0));
    float k = min(1.0, room / max(colourPx + lightPx, 1e-4));
    return float4(rim, colourPx * k, lightPx * k, washPx);
}

// How far the prototype's rim, band and wash can reach from an edge (m), for any style (ScBandWidths): the stripes' metres
// plus their pixel floors (at up to 1.5x the footprint) and the wash's floor, or the wash's reach if farther, never past
// the cap; plus a pixel for the filter. Only the edge loop is skipped beyond it: the tint needs every region pixel's own
// texel (one fetch: issue #195's rule still holds).
float ScCivReach(float footprint, float inradius)
{
    float fp15 = 1.5 * footprint;
    float bandMaxM = max(max(_ScBorderShape1.x, _ScBorderShape2.x), _ScBorderShape3.x);
    float washShare = max(max(_ScBorderShape1.z, _ScBorderShape2.z), _ScBorderShape3.z);
    float floorsPx = _ScBorders.x + _ScBorderMin.x + _ScBorderMin.y;
    float rimM = _ScBorderRim.z > 0.0 ? max(_ScBorderRim.x, _ScBorderRim.y * fp15) : 0.0;
    return min(_ScBorders.y * inradius, rimM + max(washShare * inradius, bandMaxM + floorsPx * fp15)) + fp15;
}

void ScBorderCiv(float4 own, float2 v, float2 p, float2 px, float2 py, float size, float inradius, float edge, float reach,
                 float footprint, float water, inout float3 baseColor, inout float3 emission)
{
    int edges = (int)own.z, sharedEdges = (int)own.w;
    [branch] if (own.x > 0.5 && own.y > 0.5)
    {
        bool primary = own.y < 1.5, neutral = own.y > 2.5;
        float4 colour = primary ? _ScBorderColour1 : (neutral ? _ScBorderColour3 : _ScBorderColour2);
        float4 light = primary ? _ScBorderLight1 : (neutral ? _ScBorderLight3 : _ScBorderLight2);
        float4 shape = primary ? _ScBorderShape1 : (neutral ? _ScBorderShape3 : _ScBorderShape2);
        // The territory tint: _ScBorderTint.x of the region's colour while a hex is large on screen, rising to .y as it
        // shrinks (the overview reads as coloured territory with a firm edge, not a hairline).
        float tint = lerp(_ScBorderTint.x, _ScBorderTint.y,
                          saturate((_ScBorderTint.z - inradius / footprint) / max(_ScBorderTint.z - _ScBorderTint.w, 1e-3)));
        float bandA = 0.0, washA = 0.0;
        float3 stripe = colour.rgb;
        // A hex with no border edge, or a pixel beyond every band's reach, gets the tint alone.
        [branch] if (edges != 0 && edge < reach)
        {
            // bestPx: the distance (px) to the nearest band, measured in units of that edge's band (a shared edge draws
            // ScSharedBand of it on each side), so where a shared and an unshared edge meet at a corner each keeps its own
            // width. Only an edge whose rim, band or wash reaches the pixel competes: under the camera's tilt an edge's pixels
            // can be wider than another's, so otherwise a farther edge with nothing to draw here could win the pixel, and the
            // early-out would not be exact (TheEarlyOutIsExact).
            float bestPx = 1e9, bestHalf = 1.0, bestAlong = 0.0;
            float4 bestW = float4(0.0, 0.0, 0.0, 0.0);
            [unroll] for (int i = 0; i < 6; i++)
            {
                float2 n = ScHexNormals[i];
                if (inradius - dot(v, n) >= reach) continue;
                if (((edges >> i) & 1) != 0)
                {
                    // Distance to the edge segment (corners rounded), in pixels across the edge (Task 8's anisotropic
                    // filter: metres per pixel along the direction it grows, so the pixel floors hold under the tilt).
                    float2 t = float2(-n.y, n.x);
                    float2 closest = n * inradius + t * clamp(dot(v, t), -0.5 * size, 0.5 * size);
                    float2 e = v - closest;
                    float d = length(e);
                    float2 g = d > 1e-3 ? e / d : -n;
                    float aa = max(length(float2(dot(px, g), dot(py, g))), 1e-3);
                    float share = ((sharedEdges >> i) & 1) != 0 ? ScSharedBand : 1.0;
                    // A shared edge draws ScSharedBand of the band and wash each side, and no rim (one band, no seam).
                    float4 w = ScBandWidths(shape, aa, inradius) * float4(share < 1.0 ? 0.0 : 1.0, share, share, share);
                    if (d / aa < w.x + w.y + w.z + w.w + 0.5 && d / aa / share < bestPx / bestHalf)
                    {
                        // The dash phase runs along the edge in one direction for both hexes (edges i and i + 3 are
                        // opposite: their tangents are flipped back), so the two halves of a shared neutral edge line up.
                        bestPx = d / aa; bestHalf = share; bestAlong = dot(p, i < 3 ? t : -t); bestW = w;
                    }
                }
            }
            float rimEnd = bestW.x, colourEnd = rimEnd + bestW.y, bandEnd = colourEnd + bestW.z;
            // Box-filtered, from the edge in: the rim, the colour stripe, the light stripe (coverage up to each end).
            float cRim = rimEnd > 0.0 ? saturate(rimEnd - bestPx + 0.5) : 0.0;
            float cColour = saturate(colourEnd - bestPx + 0.5), cBand = saturate(bandEnd - bestPx + 0.5);
            float rimA = cRim * _ScBorderRim.z;
            bandA = cBand - cRim + rimA;
            stripe = bandA > 0.0 ? (colour.rgb * (cColour - cRim) + light.rgb * (cBand - cColour) + colour.rgb * _ScBorderRim.w * rimA) / bandA : colour.rgb;
            // How far through the wash (0 at the band, 1 at its reach; max(): no division by zero for a style with no
            // wash, review M3). It falls as a smoothstep, or (the stronger tier) as x^2, holding its strength farther in.
            float x = saturate((bestPx - bandEnd) / max(bestW.w, 1e-3));
            washA = shape.w * (1.0 - lerp(x * x * (3.0 - 2.0 * x), x * x, _ScBorderMin.z));
            if (neutral)
            {
                // box-filtered dash: the share of this pixel's footprint along the edge that falls on a drawn dash. The
                // period doubles from _ScBorderDash.x as the camera pulls out, so a drawn dash is never under
                // _ScBorderDash.z px (doubling keeps every other dash in place, so zooming does not make them swim).
                float period = _ScBorderDash.x * exp2(max(0.0, ceil(log2(max(_ScBorderDash.z / _ScBorderDash.y * footprint / _ScBorderDash.x, 1e-6)))));
                float ph = frac(bestAlong / period), fw = max(footprint / period, 1e-4);
                float on = saturate((_ScBorderDash.y - ph) / fw + 0.5) * saturate(ph / fw + 0.5);
                bandA *= on; washA *= on;
            }
        }
        // The wash and the tint are the region's colour under the band: the stronger of the two.
        float fill = max(washA, tint) * (1.0 - bandA);
        float cover = bandA + fill;
        float3 c = cover > 0.0 ? (stripe * bandA + colour.rgb * fill) / cover : colour.rgb;
        float a = cover * (1.0 - water);
        baseColor *= 1.0 - a;
        emission = lerp(emission, c, a);
    }
}

// Adds the borders to the terrain's outputs (unlit: the stripes, wash and tint replace BaseColor and go out as Emission,
// so they read the same in sun and shadow, as the promo's glowing lines do). mapM: metres east and north of the map's
// south-west corner; water: ScWater's 0 (land) .. 1 (water): bands stay on land, so a boundary that reaches the coast stops
// at the shore instead of running up to 4 km out to its hex's edge (review M7). _ScBorderMin.w picks main's drawing
// (legacy, the default) or the prototype's. Call it in uniform control flow (outside any [branch]): it takes derivatives.
void ScBorderApply(float2 mapM, float water, inout float3 baseColor, inout float3 emission)
{
    float2 p = float2(mapM.x, _ScMap.y - mapM.y);
    float2 px = ddx(p), py = ddy(p);
    [branch] if (_ScBorders.w > 0.5)
    {
        float size = _ScMapOrigin.w, inradius = 0.8660254 * size;
        float2 v;
        float2 qr = ScHexAxial(p, size, v);
        bool legacy = _ScBorderMin.w > 0.5;
        float footprint = legacy ? max(length(px), length(py)) : max(max(length(px), length(py)), 1e-4);
        float reach = legacy ? ScLegacyReach(footprint) : ScCivReach(footprint, inradius);
        reach = _ScBorders.z > 0.5 ? 1e9 : reach;
        float edge = inradius - max(abs(v.x), max(abs(0.5 * v.x + 0.8660254 * v.y), abs(0.5 * v.x - 0.8660254 * v.y)));
        // One texel a pixel at most (issue #195): main's drawing fetches only within reach; the prototype's tint needs it on
        // every region pixel.
        float4 own = (!legacy || edge < reach) && water < 1.0 ? ScRegionAt(qr) : float4(0.0, 0.0, 0.0, 0.0);
        [branch] if (legacy) ScBorderLegacy(own, v, p, px, py, size, inradius, reach, footprint, water, baseColor, emission);
        else ScBorderCiv(own, v, p, px, py, size, inradius, edge, reach, footprint, water, baseColor, emission);
    }
}

#endif
