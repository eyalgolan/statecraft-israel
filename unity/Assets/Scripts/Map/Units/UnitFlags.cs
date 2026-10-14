#nullable enable
using System.Collections.Generic;
using UnityEngine;
using P = Statecraft.Game.Map.Units.UnitPalette;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The flag a token flies on a pole from its plinth, by its side (<see cref="UnitEntry.Side"/>; demo2 T29,
    /// refs #197). Built from code in the units' palette, as the roundels are: no texture ships. A side gets a flag only
    /// where its 1948 design is sourced here; every other side flies none (never an invented flag).
    /// - Israel: the flag the Provisional Council of State proclaimed on 28 October 1948, "220 cm. long and 160 cm. wide",
    ///   white, with "two stripes of dark sky-blue, 25 cm. broad, over the whole length of the flag" at "15 cm. from the top
    ///   and from the bottom", and a Star of David "composed of six dark sky-blue stripes, 5.5 cm. broad" forming "two
    ///   equilateral triangles, the bases of which are parallel to the two horizontal stripes"
    ///   (https://en.wikipedia.org/wiki/Flag_of_Israel, checked 2026-10-14). The same flag, the Zionist flag, hung behind
    ///   the podium at the declaration on 14 May 1948: "two flags, later to become the official flag of Israel"
    ///   (https://en.wikipedia.org/wiki/Israeli_Declaration_of_Independence, checked 2026-10-14), so it is drawn from the
    ///   game's first day. The law does not give the star's size; it is drawn 69 cm across, as the article's construction
    ///   drawing. The blue is the roundel's.
    /// - Egypt, Jordan, Syria, Iraq, Lebanon: no sourced flag in the repo yet, so none (#226 brings the Arab forces).</summary>
    public static class UnitFlags
    {
        /// <summary>The mesh is in figure heights (the layer scales it by a figure's drawn height), its origin at the
        /// pole's foot, the flag flying towards +x in the XY plane (facing the camera's south).</summary>
        public const float PoleHeight = 1.55f, PoleRadius = 0.022f, FlagWidth = 0.78f;
        public const float FlagLeft = PoleRadius, FlagTop = PoleHeight - 0.03f;
        public const float FlagBottom = FlagTop - FlagWidth * 160f / 220f;
        /// <summary>The star's outer points from its centre, in the specification's centimetres (69 cm across).</summary>
        public const float StarOuterCm = 34.5f;
        const float StripeCm = 5.5f, Layer = 0.004f;

        static readonly HashSet<string> Sourced = new HashSet<string> { UnitData.Israel };

        public static bool Has(string? side) => side != null && Sourced.Contains(side);

        public static string MeshName(string side) => "UnitFlag/" + side;

        /// <summary>The pole and flag of <paramref name="side"/>, or null when the side has no sourced flag.</summary>
        public static Mesh? Build(string? side)
        {
            if (!Has(side)) return null;
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
            return m.Build(MeshName(side!));
        }

        /// <summary>Maps a plate's local XZ outline to the XY plane at depth <paramref name="z"/>.</summary>
        static Matrix4x4 Plane(float z) =>
            new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, z, 1));
    }
}
