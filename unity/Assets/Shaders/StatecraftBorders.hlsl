#ifndef STATECRAFT_BORDERS_INCLUDED
#define STATECRAFT_BORDERS_INCLUDED
// Region borders (promo-borders): the promo's two-tone outlines along hex edges, drawn inside each region along every edge
// it shares with a different region (as Civ draws them, so two outlined regions meet as a double band): a coloured stripe on
// the edge, a light stripe inside it, then a soft wash of the colour fading inwards. Data driven: MapBorders (C#) bakes one
// texel per hex (_ScRegionTex) and sets the globals; with no region data, or the "borders" toggle off, _ScBorders.w is 0 and
// nothing is drawn. Included at the end of StatecraftMap.hlsl; the terrain calls ScBorderApply last.

TEXTURE2D(_ScRegionTex);  // one texel per hex, linear RGBA8: r + 256 g the region id (0 = none), b's low two bits the style
                          // (0 = none, 1, 2, 3) and its upper six which border edges the neighbour also draws in the same style (a
                          // shared edge: each side draws ScSharedBand of the band, so the two make one band centred on the edge), a's low
                          // six bits which edges border another region (MapBorders.Use bakes them; issue #195)
float4 _ScRegionInfo;     // x q of texel column 0, y r of texel row 0, z width, w height (texels)
float4 _ScBorders;        // x least band width (px), y widest band (+ wash) as a share of the hex's inradius,
                          // z 1 = no early-out (tests prove the early-out exact), w 1 = on
// Per style (1 primary: the Israeli side, 2 secondary: the other side, 3 neutral: contested, no-man's land, a
// demilitarized zone, or the 1947 sub-districts before any side is sourced): Colour rgb the edge stripe (linear); Light rgb
// the inner stripe; Shape x the band's width (m; colour + light stripes), y the colour stripe's share of it, z the wash's
// width (times the band's), w its strength.
float4 _ScBorderColour1, _ScBorderLight1, _ScBorderShape1;
float4 _ScBorderColour2, _ScBorderLight2, _ScBorderShape2;
float4 _ScBorderColour3, _ScBorderLight3, _ScBorderShape3;

// The outward unit normals of a hex's edges (p space: x east, y south), in tools/hexgrid.py's NEIGHBOURS order (axial
// steps (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1): MapBorders.NeighbourSteps). Bit i of a hex's edge mask is edge i.
// The share of its band each side of a shared edge draws. At exactly half, each half loses its partly covered outer pixel
// and the pair measured 6 px at gameplay03 against a full band's 8 (promo 03: about 8); at 0.6 the two measure as one band.
static const float ScSharedBand = 0.6;
// The neutral style is dashed along its edge (a cue that is not colour, for colour-blind players: review S9 of #555):
// ScNeutralDashM metres a period, the first ScNeutralDashOn of it drawn.
static const float ScNeutralDashM = 2000.0, ScNeutralDashOn = 0.65;

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

// Adds the borders to the terrain's outputs (unlit: the stripes and wash replace BaseColor and go out as Emission, so they
// read the same in sun and shadow, as the promo's glowing lines do). mapM: metres east and north of the map's south-west
// corner; water: ScWater's 0 (land) .. 1 (water): bands stay on land, so a boundary that reaches the coast stops at the
// shore instead of running up to 4 km out to its hex's edge (review M7). Call it in uniform control flow (outside any
// [branch]): it takes derivatives.
void ScBorderApply(float2 mapM, float water, inout float3 baseColor, inout float3 emission)
{
    float2 p = float2(mapM.x, _ScMap.y - mapM.y);
    float2 px = ddx(p), py = ddy(p);
    [branch] if (_ScBorders.w > 0.5)
    {
        float size = _ScMapOrigin.w, inradius = 0.8660254 * size;
        float2 v;
        float2 qr = ScHexAxial(p, size, v);
        // How far a band and its wash can reach from an edge (m): the wider style's band (or the least width in pixels, at
        // up to 1.5x the footprint: the gradient's metres per pixel never exceed sqrt 2 of it) times 1 + its wash, plus a
        // pixel for the filter. A pixel farther than that from every edge of its hex is in no band (the distance to an edge
        // segment is never less than to its line), so most pixels fetch no texel at all (review: 7 fetches on every land
        // pixel cost 3.3 ms at gameplay03).
        float footprint = max(length(px), length(py));
        float washMul = 1.0 + max(max(_ScBorderShape1.z, _ScBorderShape2.z), _ScBorderShape3.z);
        float reach = max(max(max(_ScBorderShape1.x, _ScBorderShape2.x), _ScBorderShape3.x), 1.5 * _ScBorders.x * footprint) * washMul + 1.5 * footprint;
        reach = _ScBorders.z > 0.5 ? 1e9 : reach;
        float edge = inradius - max(abs(v.x), max(abs(0.5 * v.x + 0.8660254 * v.y), abs(0.5 * v.x - 0.8660254 * v.y)));
        float4 own = edge < reach && water < 1.0 ? ScRegionAt(qr) : float4(0.0, 0.0, 0.0, 0.0);
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
                float ph = frac(bestAlong / ScNeutralDashM), w = max(footprint / ScNeutralDashM, 1e-4);
                float on = saturate((ScNeutralDashOn - ph) / w + 0.5) * saturate(ph / w + 0.5);
                a *= lerp(on, ScNeutralDashOn, saturate(w * 4.0 - 1.0));   // far off, where dashes are sub-pixel: their mean
            }
            baseColor *= 1.0 - a;
            emission = lerp(emission, c, a);
        }
    }
}

#endif
