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
    /// - Egypt (owner, 31 October 2026: "we need to have the flags of all arab forces like we have for the israeli ones"; the
    ///   sources for the four flags below are .superpowers/coordination/arab-flags-1948-research.md, read 2026-10-31): Law
    ///   No. 47 of 1923 on the national flag, al-Waqa'i' al-Misriyya No. 118, 13 December 1923, pp. 1-2 (scan at
    ///   https://manshurat.org/node/73466): green, a white crescent and three white five-pointed stars, and its annex's
    ///   construction: the hoist two thirds of the length; the stars' centres on an equilateral triangle whose side is a
    ///   seventh of the length; the side nearest the staff parallel to it, two thirds of the hoist from it; each star an eighth
    ///   of the hoist across; the crescent's centre on that side's line; its outer radius a quarter of the hoist; its breadth
    ///   at the middle three quarters of a star's diameter (<see cref="BuildEgypt"/>, in the Flaggenbuch 1939's units, 672 by
    ///   1008, which match the annex: https://www.crwflags.com/fotw/flags/eg-kingd.html). The law leaves open, and the
    ///   drawing here takes from Wikimedia Commons' File:Flag_of_Egypt_(1922-1958).svg (2012 revision): the crescent's centre
    ///   at mid-hoist, its inner circle (centre 485 from the staff, radius 142: the breadth 63 the annex asks), its horns
    ///   towards the fly with the stars between them, and the stars' points (one up, one down, one towards the fly). The
    ///   annex's model drawing is not in the scan. The green is "Hooker No. 1", a named pigment with no value:
    ///   <see cref="GreenColour"/> is a choice. Law 47, Art. 2 lets the War Minister fix a separate army flag; that decision
    ///   was not found, so the tokens fly the national flag, the one design with a primary text.
    /// - Iraq: the Organic Law of 21 March 1925 (as amended 29 July 1925), Art. 4, British and Foreign State Papers vol.
    ///   CXXIII (https://constitution.org/1-Constitution/cons/iraq/iraqiconst19250321.html): the length twice the width;
    ///   three equal bands, black, white and green from the top; at the staff a red trapezoid whose greater base is the
    ///   flag's width, its lesser base the white band's and its height a quarter of the length; on it two white
    ///   seven-pointed stars, one above the other, parallel to the staff (<see cref="BuildIraq"/>). The law fixes no star size
    ///   or centre: they follow the construction on https://www.fotw.info/flags/iq_kingd.html (Heimer, estimated from an
    ///   image): each inside a circle a ninth of the length across, centred midway along the perpendiculars from the
    ///   trapezoid's inner corners to the staff. One point up and the regular {7/2} depth, as Transjordan's, are a choice.
    /// - Syria: the Constitution of 14 May 1930 (High Commissioner's Decree No. 3111), Art. 4, League of Nations Official
    ///   Journal vol. 11 (1930) p. 1113, read as transcribed on Wikimedia Commons' File:Flag_of_Syria_(1930-1958,_1961-1963).svg
    ///   (the League's own PDF was not opened): the length twice the height; three equal bands, green, white and black from
    ///   the top; three red stars in line on the white (<see cref="BuildSyria"/>). In force in 1948 (restored 1943, revised
    ///   20 March 1948). The law fixes no star size, spacing or orientation, and FOTW reports both orientations: they follow
    ///   that Commons drawing (after FOTW): five-pointed, regular, a quarter of the length apart, the middle one at the
    ///   centre, each in a circle 0.24 of the height across, one point up (up, as National Geographic 1934 shows them).
    /// - Lebanon: the Constitution of 23 May 1926, Art. 5 as amended by the Constitutional Law of 7 December 1943 (Arabic
    ///   text, https://ar.wikisource.org/wiki/دستور_لبنان; English, https://www.wipo.int/wipolex/en/text/565753): red, white,
    ///   red, horizontal, the white as broad as both reds together; in the middle of the white a green cedar touching both
    ///   reds, and "one third" in size (<see cref="BuildLebanon"/>). Which measure that third applies to is not settled by the
    ///   texts, so the cedar's size, a third of the flag's length wide, follows the drawings: a deliberate choice. The 2:3 cloth is practice, not law (Britannica, FOTW). The law gives no outline: the
    ///   cedar is traced from Wikimedia Commons' File:Flag_of_Lebanon.svg (public domain; revision of 21 August 2026; itself
    ///   traced from the CIA World Factbook), its row-by-row outline simplified to 24 levels, the gaps in its foliage left out.
    ///   The cedar's colour in 1948 is in conflict (the 1943 Arabic text and FOTW say green; English Wikipedia says the
    ///   article named no colour before 1990 and depictions had a brown trunk): the owner chose all green on 31 October 2026
    ///   ("All green (Recommended)"; <see cref="LebanonCedarColour"/>).
    /// - Where a 1948 army flew a separate war flag in the field, no source was found for any of the four, so each flies its
    ///   national flag. A side the map draws without a sourced flag (the Arab Liberation Army, issue #665) flies a neutral
    ///   pennant instead (<see cref="BuildPennant"/>):
    ///   Q-ART-1, decided by the owner on 15 October 2026 ("2+3"): "a neutral pennant now, replaced by a sourced emblem when
    ///   one is found" (docs/superpowers/plans/2026-10-15-gdd-phase-5.md, PR 17). Never the ALA's dagger emblem (#226,
    ///   ruling 8).
    /// Every flag is <see cref="FlagWidth"/> long on the token, its top at <see cref="FlagTop"/>; its height follows its own
    /// law's proportion, so the 2:3 flags (Egypt, Lebanon) are 0.48 of a figure tall, the 1:2 flags (Transjordan, Iraq,
    /// Syria) 0.36 and Israel's 8:11 0.52.
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

        /// <summary>The Arab Legion's side, and the other Arab states' (<see cref="UnitFigures.BySide"/>).</summary>
        public const string Jordan = "jordan", Egypt = "egypt", Iraq = "iraq", Syria = "syria", Lebanon = "lebanon";

        static readonly HashSet<string> Sourced = new HashSet<string> { UnitData.Israel, Jordan, Egypt, Iraq, Syria, Lebanon };

        public static bool Has(string? side) => side != null && Sourced.Contains(side);

        /// <summary>Test seam (internal; the tests see it through InternalsVisibleTo): the sides with a sourced flag, so a test
        /// can check each has its own builder and colours without keeping a second list.</summary>
        internal static IReadOnlyCollection<string> SourcedSides => Sourced;

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
            var m = Pole();
            var cloth = new List<Vector2> { new Vector2(FlagLeft, FlagTop - PennantHeight), new Vector2(FlagLeft + FlagWidth, FlagTop - PennantHeight / 2f), new Vector2(FlagLeft, FlagTop) };
            m.Plate(Plane(0f), cloth, P.HullGrey, shade: 0.9f);
            return m.Build(PennantMeshName);
        }

        public static string MeshName(string side) => "UnitFlag/" + side;

        /// <summary>The pole and flag of <paramref name="side"/>, or null when the side has no sourced flag.</summary>
        public static Mesh? Build(string? side)
        {
            if (!Has(side)) return null;
            switch (side)
            {
                case Jordan: return BuildTransjordan();
                case Egypt: return BuildEgypt();
                case Iraq: return BuildIraq();
                case Syria: return BuildSyria();
                case Lebanon: return BuildLebanon();
                case UnitData.Israel: break;
                default: throw new InvalidOperationException($"UnitFlags: '{side}' is sourced but has no builder");   // never Israel's flag on another side
            }
            var m = Pole();
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

        /// <summary>Each flag's palette slots, one per cloth colour in <see cref="Colours"/>'s order: a slot only tags a plate's
        /// submesh (<see cref="Split"/>); the cloth's colour is the unlit material's.</summary>
        static readonly int[] IsraelSlots = { P.RoundelWhite, P.RoundelBlue }, TransjordanSlots = { P.PropBlack, P.CamoGreen, P.MarkingRed, P.RoundelWhite },
            EgyptSlots = { P.CamoGreen, P.RoundelWhite }, SyriaSlots = { P.CamoGreen, P.RoundelWhite, P.PropBlack, P.MarkingRed },
            LebanonSlots = { P.MarkingRed, P.RoundelWhite, P.CamoGreen };

        /// <summary>Transjordan's flag (the class note), in the law's units: the length 2 along +x from the pole, the width 1,
        /// the cloth's top at <see cref="FlagTop"/>. The bands are cut around the triangle (0,0), (0,1), (1/2,1/2), so the cloth
        /// is one layer; only the star lies on the red, a hair off it on both faces.</summary>
        static Mesh BuildTransjordan()
        {
            var m = Pole();
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
            float cx = 1f / (2f + 2f * Mathf.Sqrt(2f));
            var star = Star(F, cx, 0.5f, 7, 1f / 24f, 90f);
            foreach (float z in new[] { Layer, -Layer }) m.Plate(Plane(z), star, P.RoundelWhite, shade: 0.9f);
            return Split(m.Build(MeshName(Jordan)), TransjordanSlots);
        }

        /// <summary>Iraq's flag (the class note), in the law's units: the length 2 along +x from the pole, the width 1, the
        /// cloth's top at <see cref="FlagTop"/>. Transjordan's cut with the trapezoid (0,0), (0,1), (1/2,2/3), (1/2,1/3) for its
        /// triangle; the two stars at (1/4, 1/3) and (1/4, 2/3), each a ninth of the length across, a hair off the red.</summary>
        static Mesh BuildIraq()
        {
            var m = Pole();
            float s = FlagWidth / 2f;
            Vector2 F(float x, float y) => new Vector2(FlagLeft + x * s, FlagTop - s + y * s);
            const float third = 1f / 3f, twoThirds = 2f / 3f;
            var at = Plane(0f);
            m.Plate(at, new List<Vector2> { F(0, 1), F(2, 1), F(2, twoThirds), F(0.5f, twoThirds) }, P.PropBlack, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0.5f, twoThirds), F(2, twoThirds), F(2, third), F(0.5f, third) }, P.RoundelWhite, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, 0), F(0.5f, third), F(2, third), F(2, 0) }, P.CamoGreen, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, 0), F(0, 1), F(0.5f, twoThirds), F(0.5f, third) }, P.MarkingRed, shade: 0.9f);
            foreach (float y in new[] { third, twoThirds })
            {
                var star = Star(F, 0.25f, y, 7, 1f / 9f, 90f);
                foreach (float z in new[] { Layer, -Layer }) m.Plate(Plane(z), star, P.RoundelWhite, shade: 0.9f);
            }
            return Split(m.Build(MeshName(Iraq)), TransjordanSlots);
        }

        /// <summary>Syria's flag (the class note), in the law's units: the length 2, the height 1. Three equal bands; three red
        /// stars on the white, centred at a quarter, a half and three quarters of the length, each in a circle 0.24 across, one
        /// point up, a hair off the white.</summary>
        static Mesh BuildSyria()
        {
            var m = Pole();
            float s = FlagWidth / 2f;
            Vector2 F(float x, float y) => new Vector2(FlagLeft + x * s, FlagTop - s + y * s);
            const float third = 1f / 3f, twoThirds = 2f / 3f;
            var at = Plane(0f);
            m.Plate(at, new List<Vector2> { F(0, 1), F(2, 1), F(2, twoThirds), F(0, twoThirds) }, P.CamoGreen, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, twoThirds), F(2, twoThirds), F(2, third), F(0, third) }, P.RoundelWhite, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, third), F(2, third), F(2, 0), F(0, 0) }, P.PropBlack, shade: 0.9f);
            foreach (float x in new[] { 0.5f, 1f, 1.5f })
            {
                var star = Star(F, x, 0.5f, 5, 0.12f, 90f);
                foreach (float z in new[] { Layer, -Layer }) m.Plate(Plane(z), star, P.MarkingRed, shade: 0.9f);
            }
            return Split(m.Build(MeshName(Syria)), SyriaSlots);
        }

        /// <summary>Egypt's flag (the class note), in the Flaggenbuch's units that match the 1923 annex: 1008 long, 672 high.
        /// The crescent is the outer disc (centre (448, 336), radius 168) less the inner one (<see cref="EgyptInnerCentre"/>,
        /// <see cref="EgyptInnerRadius"/>), drawn as quads between the two arcs; the stars on a triangle of side 144 whose
        /// side nearest the staff stands at x = 448, each 84 across. Crescent and stars lie a hair off the green.</summary>
        static Mesh BuildEgypt()
        {
            var m = Pole();
            float s = FlagWidth / 1008f;
            Vector2 F(float x, float y) => new Vector2(FlagLeft + x * s, FlagTop - 672f * s + y * s);
            m.Plate(Plane(0f), new List<Vector2> { F(0, 0), F(1008, 0), F(1008, 672), F(0, 672) }, P.CamoGreen, shade: 0.9f);
            const float cx = 448f, cy = 336f, outerR = 168f, side = 1008f / 7f, starR = 42f;
            // The horns: where the circles meet, at a from the outer centre along the axis, h off it.
            float d = EgyptInnerCentre - cx, a = (d * d + outerR * outerR - EgyptInnerRadius * EgyptInnerRadius) / (2f * d);
            float h = Mathf.Sqrt(outerR * outerR - a * a), tipOuter = Mathf.Atan2(h, a), tipInner = Mathf.Atan2(h, a - d);
            const int arc = 24;
            Vector2 Outer(int k) { float t = tipOuter + k * (2f * Mathf.PI - 2f * tipOuter) / arc; return F(cx + outerR * Mathf.Cos(t), cy + outerR * Mathf.Sin(t)); }
            Vector2 Inner(int k) { float t = tipInner + k * (2f * Mathf.PI - 2f * tipInner) / arc; return F(EgyptInnerCentre + EgyptInnerRadius * Mathf.Cos(t), cy + EgyptInnerRadius * Mathf.Sin(t)); }
            var stars = new[]
            {
                Star(F, cx, cy + side / 2f, 5, starR, 90f), Star(F, cx, cy - side / 2f, 5, starR, 270f),
                Star(F, cx + side * Mathf.Sqrt(3f) / 2f, cy, 5, starR, 0f),
            };
            foreach (float z in new[] { Layer, -Layer })
            {
                var at = Plane(z);
                for (int k = 0; k < arc; k++) m.Plate(at, new List<Vector2> { Outer(k), Outer(k + 1), Inner(k + 1), Inner(k) }, P.RoundelWhite, shade: 0.9f);
                foreach (var star in stars) m.Plate(at, star, P.RoundelWhite, shade: 0.9f);
            }
            return Split(m.Build(MeshName(Egypt)), EgyptSlots);
        }

        /// <summary>The crescent's inner circle in <see cref="BuildEgypt"/>'s units: not fixed by the 1923 annex (only the
        /// breadth at the middle, 63, is), so taken from the Commons drawing (the class note): 280 + 63 = 485 - 142.</summary>
        public const float EgyptInnerCentre = 485f, EgyptInnerRadius = 142f;

        /// <summary>Lebanon's cedar, traced (the class note) in the flag's units (the length 3 along +x from the pole, the white
        /// band from y = 0 to 1): each row its height and its left and right edge. Consecutive rows bound a convex strip, so the
        /// cedar is drawn as a stack of strips; its width spans x = 1 to 2, a third of the length.</summary>
        public static readonly (float y, float left, float right)[] CedarRows =
        {
            (1.000f, 1.485f, 1.488f), (0.811f, 1.324f, 1.743f), (0.738f, 1.289f, 1.786f), (0.735f, 1.286f, 1.653f),
            (0.632f, 1.154f, 1.790f), (0.586f, 1.257f, 1.867f), (0.574f, 1.249f, 1.881f), (0.558f, 1.236f, 1.768f),
            (0.507f, 1.149f, 1.917f), (0.474f, 1.114f, 1.764f), (0.439f, 1.075f, 1.856f), (0.415f, 1.206f, 1.878f),
            (0.346f, 1.103f, 1.962f), (0.338f, 1.092f, 1.940f), (0.335f, 1.089f, 1.812f), (0.283f, 1.000f, 1.890f),
            (0.261f, 1.031f, 1.931f), (0.260f, 1.182f, 1.933f), (0.199f, 1.068f, 2.000f), (0.194f, 1.081f, 2.000f),
            (0.193f, 1.374f, 2.000f), (0.182f, 1.371f, 1.990f), (0.181f, 1.371f, 1.603f), (0.000f, 1.182f, 1.732f),
        };

        /// <summary>Lebanon's flag (the class note): the length 3, the width 2 (2:3, practice); red bands half a unit broad at
        /// the top and the bottom, the white between them; the cedar (<see cref="CedarRows"/>) on the white, a hair off it.</summary>
        static Mesh BuildLebanon()
        {
            var m = Pole();
            float s = FlagWidth / 3f;
            Vector2 F(float x, float y) => new Vector2(FlagLeft + x * s, FlagTop - 2f * s + y * s);
            var at = Plane(0f);
            m.Plate(at, new List<Vector2> { F(0, 2), F(3, 2), F(3, 1.5f), F(0, 1.5f) }, P.MarkingRed, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, 0.5f), F(3, 0.5f), F(3, 0), F(0, 0) }, P.MarkingRed, shade: 0.9f);
            m.Plate(at, new List<Vector2> { F(0, 1.5f), F(3, 1.5f), F(3, 0.5f), F(0, 0.5f) }, P.RoundelWhite, shade: 0.9f);
            foreach (float z in new[] { Layer, -Layer })
                for (int i = 0; i + 1 < CedarRows.Length; i++)
                {
                    var (y0, l0, r0) = CedarRows[i]; var (y1, l1, r1) = CedarRows[i + 1];
                    m.Plate(Plane(z), new List<Vector2> { F(l0, 0.5f + y0), F(r0, 0.5f + y0), F(r1, 0.5f + y1), F(l1, 0.5f + y1) }, P.CamoGreen, shade: 0.9f);
                }
            return Split(m.Build(MeshName(Lebanon)), LebanonSlots);
        }

        /// <summary>A flag's pole and its finial, in the palette's wood.</summary>
        static MiniatureMesh Pole()
        {
            var m = new MiniatureMesh();
            m.Rod(Vector3.zero, new Vector3(0, PoleHeight, 0), PoleRadius, PoleRadius * 0.8f, P.RifleWood, segments: 8, shade: 0.7f);
            m.Ellipsoid(Matrix4x4.TRS(new Vector3(0, PoleHeight + 0.02f, 0), Quaternion.identity, Vector3.one * 0.035f), P.RifleWood, 8, 4, shade: 0.9f);
            return m;
        }

        /// <summary>A regular {n/2} star as a fan from its centre (<paramref name="cx"/>, <paramref name="cy"/>) in a flag's own
        /// units (<paramref name="F"/> maps them to the mesh): <paramref name="points"/> points on a circle of radius
        /// <paramref name="outer"/>, one at <paramref name="pointDeg"/> degrees (90 is up); the inner corners at the regular
        /// star's depth, outer x cos(2 pi / n) / cos(pi / n).</summary>
        static List<Vector2> Star(Func<float, float, Vector2> F, float cx, float cy, int points, float outer, float pointDeg)
        {
            float inner = outer * Mathf.Cos(2f * Mathf.PI / points) / Mathf.Cos(Mathf.PI / points);
            var star = new List<Vector2> { F(cx, cy) };
            for (int i = 0; i <= 2 * points; i++)
            {
                float a = pointDeg * Mathf.Deg2Rad + i * Mathf.PI / points, r = i % 2 == 0 ? outer : inner;
                star.Add(F(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a)));
            }
            return star;
        }

        /// <summary>The cloth's colours, drawn unlit (the selection ring's shader) so the flag reads white and blue under
        /// the map's light and grade, as T29's (its white 0.83-0.91 luma). The blue stands for the proclamation's "dark
        /// sky-blue", which gives no value: a choice.</summary>
        public static readonly Color WhiteColour = new Color(1f, 1f, 1f), BlueColour = new Color(0.0f, 0.22f, 0.62f);
        /// <summary>Transjordan's black, green and red: the 1928 law names the colours, not their values, so these are a choice
        /// (plain, saturated cloth colours that read unlit).
        /// Egypt's, Iraq's, Syria's and Lebanon's laws name their colours with no value either (Egypt's green a pigment, "Hooker
        /// No. 1"), so their cloth shares these.</summary>
        public static readonly Color BlackColour = new Color(0.05f, 0.05f, 0.05f), GreenColour = new Color(0.0f, 0.45f, 0.22f), RedColour = new Color(0.78f, 0.08f, 0.10f);

        /// <summary>The colour of Lebanon's whole cedar: all green, the owner's choice (31 October 2026, "All green
        /// (Recommended)") where the sources conflict (the 1943 Arabic text and FOTW say green; English Wikipedia says no colour
        /// was fixed before 1990 and depictions had a brown trunk; the class note).</summary>
        public static readonly Color LebanonCedarColour = GreenColour;

        /// <summary>The cloth's colours of <paramref name="side"/>'s flag, one per submesh after the pole's, in drawing order
        /// (a later colour lies over an earlier one).</summary>
        public static IReadOnlyList<Color> Colours(string side) => side switch
        {
            Jordan or Iraq => new[] { BlackColour, GreenColour, RedColour, WhiteColour },
            Egypt => new[] { GreenColour, WhiteColour },
            Syria => new[] { GreenColour, WhiteColour, BlackColour, RedColour },
            Lebanon => new[] { RedColour, WhiteColour, LebanonCedarColour },
            UnitData.Israel => new[] { WhiteColour, BlueColour },
            _ => throw new ArgumentException($"UnitFlags: '{side}' has no sourced flag, so no cloth colours", nameof(side)),
        };

        /// <summary>The unlit cloth materials for <paramref name="side"/>'s flag, one per <see cref="Colours"/>, each drawn after
        /// the one before (the blue over the white, a star over its field).</summary>
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
