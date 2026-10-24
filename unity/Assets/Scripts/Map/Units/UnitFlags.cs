#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using P = Statecraft.Game.Map.Units.UnitPalette;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The flag a token flies on a pole from its base, by its side (<see cref="UnitEntry.Side"/>; demo2 T29,
    /// refs #197). Built from code in the units' palette, as the roundels are: no texture ships. A side gets a flag only
    /// where its 1948 design is sourced here; every other side flies none (never an invented flag).
    /// - Israel: the flag the Provisional Council of State proclaimed on 25 Tishrei 5709 (28 October 1948), "Proclamation
    ///   on the Flag of the State of Israel", Iton Rishmi No. 32, 10 Heshvan 5709 (12 November 1948), p. 62
    ///   (https://fs.knesset.gov.il/0/law/0_lsr_340940.pdf, read 2026-10-14): 220 cm long and 160 cm wide; white, with two
    ///   dark sky-blue (tkhelet) stripes 25 cm broad along its whole length, 15 cm from the top and from the bottom edge;
    ///   in the middle of the white, equally far from both stripes, a Star of David of six sky-blue stripes 5.5 cm broad
    ///   forming two equilateral triangles whose bases are parallel to the stripes. The Flag and Emblem Law, 5709-1949
    ///   (Sefer HaChukim No. 8, 28 Iyar 5709 / 24 May 1949, p. 37, s. 1(a); https://fs.knesset.gov.il/1/law/1_lsr_208225.pdf,
    ///   read 2026-10-14) makes "the State flag" that proclaimed flag, "in any size". Neither text gives the star's size: it
    ///   is drawn 69 cm across, as the construction drawing at https://en.wikipedia.org/wiki/Flag_of_Israel (checked
    ///   2026-10-14), which fits the gazette's own drawing (the star fills most of the white between the stripes). The same
    ///   design, the Zionist flag, hung at the declaration on 14 May 1948 ("two flags, later to become the official flag of
    ///   Israel", https://en.wikipedia.org/wiki/Israeli_Declaration_of_Independence, checked 2026-10-14), so it flies from
    ///   the game's first day. The gazette says only "dark sky-blue" (tkhelet kehe); the cloth's blue
    ///   (<see cref="BlueColour"/>) is a choice, a little brighter than the roundel's so it reads unlit.
    /// - Transjordan (the Arab Legion's tokens; owner, 22 October 2026, #756: each Arab side flies "the sourced 1948 flag ...
    ///   Where no source exists, a neutral pennant"): the Organic Law of 16 April 1928, Art. 3, French translation on the
    ///   Digithèque MJP (https://mjp.univ-perp.fr/constit/jo1928.htm, read 2026-10-22 through the Wayback Machine; source
    ///   research_notes/GDD phase 5/unit-art-sourcing-1948.md, section 1): the length twice the width; three equal bands,
    ///   black, white and green from the top; at the hoist a red triangle whose longest side is the flag's width and whose
    ///   height is a quarter of its length; at the meeting of the triangle's bisectors a white seven-pointed star that fits a
    ///   circle a twenty-fourth of the length across, the axis through one of its points parallel to the staff
    ///   (<see cref="BuildTransjordan"/>). The note's open points, kept as the law has them: that the flag was unchanged on
    ///   15 May 1948 rests on one search snippet [S], and a later, longer triangle is reported [C]. The law names the colours
    ///   only: their values (<see cref="BlackColour"/>, <see cref="GreenColour"/>, <see cref="RedColour"/>) are a choice, as
    ///   are the star's point up (the law fixes the axis, not which end) and the depth of its points.
    /// - Egypt, Syria, Iraq, Lebanon: no figure on the map yet, so no flag drawn (#756 follows the same code once they have
    ///   one). A side the map draws without a sourced flag (the Arab Liberation Army, issue #665) flies a neutral pennant
    ///   instead (<see cref="BuildPennant"/>):
    ///   Q-ART-1, decided by the owner on 15 October 2026 ("2+3"): "a neutral pennant now, replaced by a sourced emblem when
    ///   one is found" (docs/superpowers/plans/2026-10-15-gdd-phase-5.md, PR 17). Never the ALA's dagger emblem (#226,
    ///   ruling 8).
    /// Proportions on the token follow T29: the cloth about half a figure's height, the pole about one and a half.</summary>
    public static class UnitFlags
    {
        /// <summary>The mesh is in figure heights (the layer scales it by a figure's drawn height), its origin at the
        /// pole's foot, the flag flying towards +x in the XY plane (facing the camera's south). T29's flags, measured on three
        /// tokens: the cloth 0.47-0.60 of a figure's height tall and 0.65-0.70 wide, the pole about 1.5; here 0.52, 0.72 and
        /// 1.5.</summary>
        public const float PoleHeight = 1.5f, PoleRadius = 0.022f, FlagWidth = 0.72f;
        public const float FlagLeft = PoleRadius, FlagTop = PoleHeight - 0.03f;
        public const float FlagBottom = FlagTop - FlagWidth * 160f / 220f;
        /// <summary>The star's outer points from its centre, in the specification's centimetres (69 cm across).</summary>
        public const float StarOuterCm = 34.5f;
        const float StripeCm = 5.5f, Layer = 0.02f;   // the blue a fiftieth of a figure off the white: enough depth for WebGL at 60 km

        /// <summary>The Arab Legion's side (<see cref="UnitFigures.BySide"/>).</summary>
        public const string Jordan = "jordan";

        static readonly HashSet<string> Sourced = new HashSet<string> { UnitData.Israel, Jordan };

        public static bool Has(string? side) => side != null && Sourced.Contains(side);

        /// <summary>True when <paramref name="side"/>'s tokens fly the neutral pennant: a side the map draws with no sourced flag.</summary>
        public static bool HasPennant(string? side) => side != null && !Has(side) && UnitFigures.SideFigure(side) != null;

        public const string PennantMeshName = "UnitFlag/pennant";
        /// <summary>The pennant's cloth: a plain triangle from the pole's top, <see cref="FlagWidth"/> long and
        /// <see cref="PennantHeight"/> at the pole (figure heights), in the palette's neutral grey (<see cref="UnitPalette.HullGrey"/>):
        /// no colour of any side, and not white.</summary>
        public const float PennantHeight = 0.36f;

        /// <summary>The pole and the neutral pennant, one submesh in the units' palette.</summary>
        public static Mesh BuildPennant()
        {
            var m = new MiniatureMesh();
            m.Rod(Vector3.zero, new Vector3(0, PoleHeight, 0), PoleRadius, PoleRadius * 0.8f, P.RifleWood, segments: 8, shade: 0.7f);
            m.Ellipsoid(Matrix4x4.TRS(new Vector3(0, PoleHeight + 0.02f, 0), Quaternion.identity, Vector3.one * 0.035f), P.RifleWood, 8, 4, shade: 0.9f);
            var cloth = new List<Vector2> { new Vector2(FlagLeft, FlagTop - PennantHeight), new Vector2(FlagLeft + FlagWidth, FlagTop - PennantHeight / 2f), new Vector2(FlagLeft, FlagTop) };
            m.Plate(Plane(0f), cloth, P.HullGrey, shade: 0.9f);
            return m.Build(PennantMeshName);
        }

        public static string MeshName(string side) => "UnitFlag/" + side;

        /// <summary>The pole and flag of <paramref name="side"/>, or null when the side has no sourced flag.</summary>
        public static Mesh? Build(string? side)
        {
            if (!Has(side)) return null;
            if (side == Jordan) return BuildTransjordan();
            var m = new MiniatureMesh();
            m.Rod(Vector3.zero, new Vector3(0, PoleHeight, 0), PoleRadius, PoleRadius * 0.8f, P.RifleWood, segments: 8, shade: 0.7f);
            m.Ellipsoid(Matrix4x4.TRS(new Vector3(0, PoleHeight + 0.02f, 0), Quaternion.identity, Vector3.one * 0.035f), P.RifleWood, 8, 4, shade: 0.9f);
            Vector2 F(float xCm, float yCm) => new Vector2(FlagLeft + xCm * FlagWidth / 220f, FlagBottom + yCm * FlagWidth / 220f);
            m.Plate(Plane(0f), new List<Vector2> { F(0, 0), F(220, 0), F(220, 160), F(0, 160) }, P.RoundelWhite, shade: 0.9f);
            foreach (float z in new[] { Layer, -Layer })   // the blue on both faces, a hair off the white
            {
                var at = Plane(z);
                foreach (float y in new[] { 15f, 120f })
                    m.Plate(at, new List<Vector2> { F(0, y), F(220, y), F(220, y + 25f), F(0, y + 25f) }, P.RoundelBlue, shade: 0.85f);
                foreach (float turn in new[] { 90f, 270f })   // the upward and the downward triangle, bases horizontal
                    for (int k = 0; k < 3; k++)
                    {
                        Vector2 Corner(int i, float r) { float a = (turn + 120f * i) * Mathf.Deg2Rad; return F(110f + r * Mathf.Cos(a), 80f + r * Mathf.Sin(a)); }
                        float inner = StarOuterCm - 2f * StripeCm;   // an equilateral triangle's edges moved in by d: its corners by 2d
                        m.Plate(at, new List<Vector2> { Corner(k, StarOuterCm), Corner(k + 1, StarOuterCm), Corner(k + 1, inner), Corner(k, inner) }, P.RoundelBlue, shade: 0.85f);
                    }
            }
            return Split(m.Build(MeshName(side!)), IsraelSlots);
        }

        static readonly int[] IsraelSlots = { P.RoundelWhite, P.RoundelBlue }, TransjordanSlots = { P.PropBlack, P.CamoGreen, P.MarkingRed, P.RoundelWhite };

        /// <summary>Transjordan's flag (the class note), in the law's units: the length 2 along +x from the pole, the width 1,
        /// the cloth's top at <see cref="FlagTop"/>. The bands are cut around the triangle (0,0), (0,1), (1/2,1/2), so the cloth
        /// is one layer; only the star lies on the red, a hair off it on both faces.</summary>
        static Mesh BuildTransjordan()
        {
            var m = new MiniatureMesh();
            m.Rod(Vector3.zero, new Vector3(0, PoleHeight, 0), PoleRadius, PoleRadius * 0.8f, P.RifleWood, segments: 8, shade: 0.7f);
            m.Ellipsoid(Matrix4x4.TRS(new Vector3(0, PoleHeight + 0.02f, 0), Quaternion.identity, Vector3.one * 0.035f), P.RifleWood, 8, 4, shade: 0.9f);
            float s = FlagWidth / 2f;
            Vector2 F(float x, float y) => new Vector2(FlagLeft + x * s, FlagTop - s + y * s);
            const float third = 1f / 3f, twoThirds = 2f / 3f;
            var at = Plane(0f);
            m.Plate(at, new List<Vector2> { F(0, 1), F(2, 1), F(2, twoThirds), F(third, twoThirds) }, P.PropBlack, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(third, twoThirds), F(2, twoThirds), F(2, 0.5f), F(0.5f, 0.5f) }, P.RoundelWhite, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0.5f, 0.5f), F(2, 0.5f), F(2, third), F(third, third) }, P.RoundelWhite, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, 0), F(third, third), F(2, third), F(2, 0) }, P.CamoGreen, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, 0), F(0, 1), F(0.5f, 0.5f) }, P.MarkingRed, shade: 0.9f);
            // The star at the incentre of the triangle (sides 1, sqrt 1/2, sqrt 1/2): (1/(2+2 sqrt 2), 1/2), its inradius from
            // the hoist; its points on a circle of radius 1/24 (a diameter of a twenty-fourth of the length 2), one point up.
            // The inner corners at the regular {7/2} star's depth: the law gives none.
            float cx = 1f / (2f + 2f * Mathf.Sqrt(2f)), outer = 1f / 24f, inner = outer * Mathf.Cos(2f * Mathf.PI / 7f) / Mathf.Cos(Mathf.PI / 7f);
            var star = new List<Vector2> { F(cx, 0.5f) };   // a fan from the centre
            for (int i = 0; i <= 14; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 7f, r = i % 2 == 0 ? outer : inner;
                star.Add(F(cx + r * Mathf.Cos(a), 0.5f + r * Mathf.Sin(a)));
            }
            foreach (float z in new[] { Layer, -Layer }) m.Plate(Plane(z), star, P.RoundelWhite, shade: 0.9f);
            return Split(m.Build(MeshName(Jordan)), TransjordanSlots);
        }

        /// <summary>The cloth's colours, drawn unlit (the selection ring's shader) so the flag reads white and blue under
        /// the map's light and grade, as T29's (its white 0.83-0.91 luma). The blue stands for the proclamation's "dark
        /// sky-blue", which gives no value: a choice.</summary>
        public static readonly Color WhiteColour = new Color(1f, 1f, 1f), BlueColour = new Color(0.0f, 0.22f, 0.62f);
        /// <summary>Transjordan's black, green and red: the 1928 law names the colours, not their values, so these are a choice
        /// (plain, saturated cloth colours that read unlit).</summary>
        public static readonly Color BlackColour = new Color(0.05f, 0.05f, 0.05f), GreenColour = new Color(0.0f, 0.45f, 0.22f), RedColour = new Color(0.78f, 0.08f, 0.10f);

        /// <summary>The cloth's colours of <paramref name="side"/>'s flag, one per submesh after the pole's, in drawing order
        /// (a later colour lies over an earlier one).</summary>
        public static IReadOnlyList<Color> Colours(string side) =>
            side == Jordan ? new[] { BlackColour, GreenColour, RedColour, WhiteColour } : new[] { WhiteColour, BlueColour };

        /// <summary>The unlit cloth materials for <paramref name="side"/>'s flag, one per <see cref="Colours"/>, each drawn after
        /// the one before (the blue over the white, the star over the red).</summary>
        public static Material[] ClothMaterials(string side, Material ringTemplate, Func<Material, Material> own)
        {
            var colours = Colours(side); var mats = new Material[colours.Count];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = own(new Material(ringTemplate) { name = $"UnitFlag ({side} {i})", color = colours[i] });
                if (i > 0) mats[i].renderQueue = mats[0].renderQueue + i;
            }
            return mats;
        }

        /// <summary>Puts the pole in submesh 0 and the cloth of palette slot <paramref name="slots"/>[k] in submesh k + 1 (the
        /// palette, then the cloth materials).</summary>
        static Mesh Split(Mesh mesh, int[] slots)
        {
            var uv = mesh.uv; var t = mesh.triangles;
            var parts = new List<int>[slots.Length + 1];
            for (int k = 0; k < parts.Length; k++) parts[k] = new List<int>();
            for (int i = 0; i < t.Length; i += 3)
            {
                float u = uv[t[i]].x;
                int k = 1 + Array.FindIndex(slots, slot => Mathf.Abs(u - P.U(slot)) < 1e-4f);
                parts[k].Add(t[i]); parts[k].Add(t[i + 1]); parts[k].Add(t[i + 2]);
            }
            mesh.subMeshCount = parts.Length;
            for (int k = 0; k < parts.Length; k++) mesh.SetTriangles(parts[k], k);
            return mesh;
        }

        /// <summary>Maps a plate's local XZ outline to the XY plane at depth <paramref name="z"/>.</summary>
        static Matrix4x4 Plane(float z) =>
            new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, z, 1));
    }
}
