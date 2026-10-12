#nullable enable
using System.Collections.Generic;
using UnityEngine;
using P = Statecraft.Game.Map.Units.UnitPalette;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The unit miniatures, built in real metres (a man 1.78 m tall, the Spitfire's 11.2 m span, the corvette's
    /// 63.4 m hull) and scaled up to the map's symbolic scale by <see cref="UnitShowcase"/>. Each model faces +Z with its
    /// origin on the ground (the aircraft: its centre of gravity). References and their check dates: docs/units.md.
    /// Rifles, straps and hat brims are thicker than real so they survive at 60 km (a painted miniature's exaggeration).</summary>
    public static class UnitModels
    {
        public enum Headgear { SteelHelmet, SlouchHat, WoolCap }
        public enum RiflePose { Ready, Slung, Grounded }

        static Matrix4x4 T(float x, float y, float z) => Matrix4x4.Translate(new Vector3(x, y, z));
        static Matrix4x4 TRS(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>A 1948 IDF rifleman (sources in docs/units.md): khaki drill shirt and trousers, a 37-pattern web belt with
        /// two basic pouches and a brace, short anklets over boots, a No. 1 / No. 4 Lee-Enfield or a Czech Mauser (one long
        /// rifle shape serves both at this scale), and one of the three headgear the 1948 photographs show.</summary>
        public static Mesh Rifleman(Headgear hat, RiflePose pose, bool fadedShirt, string name)
        {
            var m = new MiniatureMesh();
            int shirt = fadedShirt ? P.ShirtFaded : P.Shirt;
            // Legs (trousers), anklets and boots; a slight stance.
            foreach (var side in new[] { -1f, 1f })
            {
                float x = side * 0.105f;
                m.Rod(V(x, 0.93f, 0f), V(x * 1.15f, 0.16f, 0.01f), 0.085f, 0.06f, P.Trousers);
                m.Rod(V(x * 1.15f, 0.20f, 0.01f), V(x * 1.15f, 0.09f, 0.01f), 0.065f, 0.062f, P.Webbing, rounded: false);
                m.Ellipsoid(TRS(V(x * 1.15f, 0.05f, 0.05f), Vector3.zero, V(0.06f, 0.05f, 0.13f)), P.Boots);
            }
            // Hips and torso (shirt), flattened front to back.
            m.Lathe(Matrix4x4.Scale(V(1f, 1f, 0.68f)), new List<Vector2> { new Vector2(0.86f, 0.17f), new Vector2(1.00f, 0.165f),
                new Vector2(1.22f, 0.175f), new Vector2(1.40f, 0.19f), new Vector2(1.47f, 0.13f), new Vector2(1.50f, 0.05f) }, shirt, 12);
            // Web belt with two pouches, and a brace across the chest.
            m.Lathe(Matrix4x4.Scale(V(1f, 1f, 0.70f)), new List<Vector2> { new Vector2(0.97f, 0.178f), new Vector2(1.04f, 0.178f) }, P.Webbing, 12);
            foreach (var side in new[] { -1f, 1f })
                m.Box(TRS(V(side * 0.085f, 1.06f, 0.125f), V(0, side * 12f, 0), V(0.10f, 0.11f, 0.05f)), P.Webbing);
            m.Box(TRS(V(0f, 1.24f, 0.0f), V(0, 0, 38f), V(0.05f, 0.62f, 0.27f)), P.Webbing);
            // Neck, head, hair.
            m.Rod(V(0, 1.47f, 0), V(0, 1.56f, 0.01f), 0.05f, 0.05f, P.Skin, rounded: false);
            m.Ellipsoid(TRS(V(0, 1.65f, 0.01f), Vector3.zero, V(0.105f, 0.125f, 0.115f)), P.Skin);   // a miniature's slightly large head
            switch (hat)
            {
                case Headgear.SteelHelmet:   // British Mk II: a shallow bowl with a wide flat brim
                    m.Ellipsoid(TRS(V(0, 1.72f, 0.01f), Vector3.zero, V(0.13f, 0.08f, 0.14f)), P.Helmet);
                    m.Lathe(T(0, 1.70f, 0.01f), new List<Vector2> { new Vector2(0f, 0.20f), new Vector2(0.018f, 0.19f) }, P.Helmet, 14);
                    break;
                case Headgear.SlouchHat:     // the brimmed bush hat of the 1948 Negev photographs
                    m.Lathe(T(0, 1.71f, 0.01f), new List<Vector2> { new Vector2(0f, 0.11f), new Vector2(0.08f, 0.10f), new Vector2(0.12f, 0.07f) }, P.SlouchHat, 12);
                    m.Lathe(TRS(V(0, 1.715f, 0.01f), V(0, 0, -8f), Vector3.one), new List<Vector2> { new Vector2(0f, 0.22f), new Vector2(0.016f, 0.215f) }, P.SlouchHat, 14);
                    break;
                default:                     // the Palmach's knitted stocking cap
                    m.Ellipsoid(TRS(V(0, 1.71f, 0.0f), V(-10f, 0, 0), V(0.105f, 0.085f, 0.115f)), P.WoolCap);
                    break;
            }
            m.Ellipsoid(TRS(V(0, 1.66f, -0.035f), Vector3.zero, V(0.09f, 0.08f, 0.08f)), P.Hair);
            // Arms and the rifle, by pose.
            var shoulderL = V(-0.20f, 1.40f, 0f); var shoulderR = V(0.20f, 1.40f, 0f);
            switch (pose)
            {
                case RiflePose.Ready:        // held across the body, muzzle up and forward to the left (the promo's lead figure)
                {
                    var handR = V(0.08f, 1.08f, 0.24f); var handL = V(-0.16f, 1.30f, 0.28f);
                    Arm(m, shoulderR, V(0.22f, 1.16f, 0.10f), handR, shirt);
                    Arm(m, shoulderL, V(-0.26f, 1.16f, 0.16f), handL, shirt);
                    Rifle(m, V(0.20f, 0.92f, 0.20f), V(-0.42f, 1.62f, 0.36f));
                    break;
                }
                case RiflePose.Slung:        // rifle on the right shoulder by its sling, arms relaxed
                {
                    Arm(m, shoulderL, V(-0.25f, 1.12f, 0.02f), V(-0.24f, 0.88f, 0.06f), shirt);
                    Arm(m, shoulderR, V(0.25f, 1.15f, 0.06f), V(0.21f, 1.30f, 0.14f), shirt);
                    Rifle(m, V(0.24f, 0.72f, -0.10f), V(0.24f, 1.82f, -0.02f));
                    break;
                }
                default:                     // butt on the ground at the right foot, hand at the muzzle
                {
                    Arm(m, shoulderL, V(-0.25f, 1.12f, 0.02f), V(-0.24f, 0.88f, 0.05f), shirt);
                    Arm(m, shoulderR, V(0.27f, 1.14f, 0.04f), V(0.30f, 1.02f, 0.12f), shirt);
                    Rifle(m, V(0.31f, 0.05f, 0.16f), V(0.30f, 1.14f, 0.13f));
                    break;
                }
            }
            return m.Build(name);
        }

        static void Arm(MiniatureMesh m, Vector3 shoulder, Vector3 elbow, Vector3 hand, int sleeve)
        {
            m.Ellipsoid(TRS(shoulder, Vector3.zero, V(0.08f, 0.08f, 0.08f)), sleeve);
            m.Rod(shoulder, elbow, 0.062f, 0.052f, sleeve);
            m.Rod(elbow, hand, 0.05f, 0.04f, P.Skin);
            m.Ellipsoid(TRS(hand, Vector3.zero, V(0.045f, 0.05f, 0.045f)), P.Skin);
        }

        /// <summary>A long service rifle from butt to muzzle: a wooden stock and fore-end with the barrel's last part bare.</summary>
        static void Rifle(MiniatureMesh m, Vector3 butt, Vector3 muzzle)
        {
            var wood = Vector3.Lerp(butt, muzzle, 0.86f);
            m.Rod(butt, Vector3.Lerp(butt, muzzle, 0.30f), 0.05f, 0.035f, P.RifleWood, segments: 6);
            m.Rod(Vector3.Lerp(butt, muzzle, 0.28f), wood, 0.032f, 0.028f, P.RifleWood, segments: 6);
            m.Rod(Vector3.Lerp(butt, muzzle, 0.34f), muzzle, 0.016f, 0.014f, P.RifleMetal, segments: 5);
        }

        /// <summary>A Supermarine Spitfire LF Mk IXe of 101 Squadron, IAF, from October 1948 (Yoav Efrati, Colors &amp;
        /// Markings of the Israeli Air Force, IAF, 2005, pp. 12-14):
        /// - the clipped wing every Spitfire flown to Israel in the war had (p. 14), about 10.2 m span;
        /// - RAF Ocean Grey and Dark Green over Medium Sea Grey (p. 14);
        /// - from early October 1948 the squadron's red spinner and diagonal red and white rudder stripes (p. 12);
        /// - filled Star of David roundels with no ring on the fuselage sides and on both wing surfaces; the Velvetta
        ///   Spitfires arrived with over-wing roundels (p. 14).
        /// The squadron badge on the left cowling is too small to read at map scale and is left out. The propeller is a
        /// separate mesh (<see cref="SpitfirePropeller"/>) so it can turn. Faces +Z, centre of gravity at the origin.
        /// Built with one mid shade (<see cref="AircraftShade"/>), not by height.</summary>
        public static Mesh Spitfire(string name)
        {
            var m = new MiniatureMesh();
            // Fuselage: a lathe along Z, taller than wide.
            var body = new List<Vector2> { new Vector2(-6.2f, 0.06f), new Vector2(-5.2f, 0.28f), new Vector2(-3.0f, 0.52f), new Vector2(-1.0f, 0.68f),
                new Vector2(1.0f, 0.72f), new Vector2(2.4f, 0.66f), new Vector2(3.0f, 0.50f) };
            m.Lathe(TRS(Vector3.zero, V(90f, 0, 0), V(0.78f, 1f, 1.05f)), body, P.CamoGreen, 12);
            m.Lathe(TRS(V(0, -0.22f, 0), V(90f, 0, 0), V(0.62f, 1f, 0.72f)), new List<Vector2> { new Vector2(-2.6f, 0.2f), new Vector2(-1.0f, 0.62f),
                new Vector2(1.6f, 0.62f), new Vector2(2.6f, 0.42f) }, P.Underside, 12);
            // Spinner: 101 Squadron red, from early October 1948 (Efrati p. 12).
            m.Lathe(TRS(V(0, 0.02f, 3.0f), V(90f, 0, 0), Vector3.one), new List<Vector2> { new Vector2(0f, 0.36f), new Vector2(0.45f, 0.26f), new Vector2(0.75f, 0f) }, P.MarkingRed, 10);
            // Canopy.
            m.Ellipsoid(TRS(V(0, 0.68f, -0.4f), Vector3.zero, V(0.34f, 0.32f, 1.0f)), P.Glass);
            // The clipped wing: every Spitfire flown to Israel in the 1948 war was a clipped-wing Mk IXe (Efrati p. 14); the
            // tips were replaced by fairings that cut the span by 3 ft 6 in (Supermarine Spitfire article). So the elliptical
            // planform stops at 84% of the full semi-span, square-ended: semi-span 4.7 m beyond the root, low on the fuselage,
            // with a 6 degree dihedral.
            float semi = 4.7f; const float clip = 0.84f;
            System.Func<float, float> lead = s => 1.05f * Mathf.Sqrt(Mathf.Max(0f, 1f - (s * clip) * (s * clip))) + 0.04f;
            System.Func<float, float> trail = s => 1.45f * Mathf.Sqrt(Mathf.Max(0f, 1f - (s * clip) * (s * clip))) + 0.04f;
            // Disruptive camouflage: two smooth waves across span and chord give irregular patches, not a chequerboard.
            System.Func<int, int, int> camo = (i, j) => Mathf.Sin(i * 0.9f + j * 1.7f) + Mathf.Sin(i * 0.37f - j * 2.3f + 1.1f) > 0.2f ? P.CamoGreen : P.CamoGrey;
            foreach (var side in new[] { 1f, -1f })
            {
                var w = TRS(V(side * 0.4f, -0.38f, 0.3f), V(0, 0, side * 6f), V(side, 1f, 1f));
                m.Wing(w, lead, trail, semi, 0.06f, camo, (i, j) => P.Underside, spanSteps: 12, chordSteps: 4);
                // Wing roundel near the tip, on top and below.
                var rp = w.MultiplyPoint3x4(V(semi * 0.62f, 0.16f, -0.15f));
                Roundel(m, TRS(rp, V(0, 0, side * 6f), Vector3.one), 0.62f);
                Roundel(m, TRS(rp - V(0, 0.32f, 0), V(0, 0, side * 6f), V(1f, -1f, 1f)), 0.62f);   // underneath: stacked downwards
                // Tailplane: small ellipses.
                var tp = TRS(V(side * 0.2f, 0.1f, -5.6f), Vector3.zero, V(side, 1f, 1f));
                m.Wing(tp, s => 0.45f * Mathf.Sqrt(Mathf.Max(0f, 1f - s * s)) + 0.02f, s => 0.55f * Mathf.Sqrt(Mathf.Max(0f, 1f - s * s)) + 0.02f,
                    1.55f, 0.05f, (i, j) => (i + j) % 3 == 0 ? P.CamoGreen : P.CamoGrey, (i, j) => P.Underside, spanSteps: 5, chordSteps: 2);
                // Fuselage roundel.
                Roundel(m, TRS(V(side * 0.58f, 0.15f, -2.6f), V(0, 0, -side * 90f), Vector3.one), 0.48f, turnDegrees: 30f);   // its face (local +Y) outwards
                // Radiator under each wing.
                m.Box(TRS(V(side * 1.7f, -0.62f, 0.0f), Vector3.zero, V(0.42f, 0.28f, 1.3f)), P.Underside);
            }
            // Fin and rudder: the elliptic Spitfire fin, a vertical surface; the rear half (the rudder) in DIAGONAL red and white
            // stripes on both sides, as 101 Squadron painted them from early October 1948 (Efrati p. 12: "rudders with diagonal
            // red and white stripes"; the photographs of Spitfires 14 and 17, p. 12).
            System.Func<int, int, int> fin = (i, j) => j < 2 ? (i % 2 == 0 ? P.CamoGreen : P.CamoGrey) : ((i + j) / 2 % 2 == 0 ? P.MarkingRed : P.RoundelWhite);
            m.Wing(TRS(V(0, 0.4f, -5.5f), V(0, 0, 90f), Vector3.one), s => 0.55f * Mathf.Sqrt(Mathf.Max(0f, 1f - s * s)) + 0.05f,
                s => 0.75f * Mathf.Sqrt(Mathf.Max(0f, 1f - s * s)) + 0.05f, 1.55f, 0.05f, fin, fin, spanSteps: 10, chordSteps: 4);
            return m.Build(name, AircraftShade);
        }

        /// <summary>An Avia S-199 of 101 Squadron, IAF, 1948 (sources in docs/units.md): the Bf 109G airframe with the Jumo 211F
        /// and its wide paddle-bladed propeller (Avia S-199 article), 8.94 m long with a 9.92 m span, one khaki green on all
        /// surfaces (about RAL 6013: Efrati, Colors &amp; Markings of the Israeli Air Force, 2005, pp. 8 and 11; Eduard Info 11/2025,
        /// pp. 53-54), the white-blue-white identification band round the rear fuselage, filled Star of David roundels on the
        /// fuselage sides and under the wings, none on top (Efrati pp. 6, 8, 11, 14: over-wing roundels came only with the
        /// Velvetta Spitfires in October 1948), and the MG 151 cannon pods under the wings: its
        /// scheme until mid-October 1948. The red spinner, rudder stripes and upper-wing roundels the squadron's fighters got
        /// for Operation Ten Plagues (from 15 October, Eduard p. 53) are not drawn: the map shows the Spitfire from that day
        /// (docs/units.md). The 101 Squadron skull badge on the nose is too small to read at map scale and is left out. The
        /// propeller is a separate mesh (<see cref="S199Propeller"/>). Faces +Z, centre of gravity at the origin.</summary>
        public static Mesh AviaS199(string name)
        {
            var m = new MiniatureMesh();
            var shape = TRS(Vector3.zero, V(90f, 0, 0), V(0.70f, 1f, 1.05f));
            var body = new List<Vector2> { new Vector2(-5.5f, 0.06f), new Vector2(-4.6f, 0.26f), new Vector2(-2.6f, 0.46f), new Vector2(-0.8f, 0.56f),
                new Vector2(0.9f, 0.58f), new Vector2(2.2f, 0.55f), new Vector2(2.95f, 0.46f) };
            m.Lathe(shape, body, P.AviaGreen, 12);
            // The identification band round the rear fuselage: white, blue, white (Efrati p. 11: "the fuselage identification
            // white-blue-white blue center stripe"; the 1948 photographs show the white bands either side).
            foreach (var (z, slot) in new[] { (-3.40f, P.RoundelWhite), (-3.62f, P.RoundelBlue), (-3.84f, P.RoundelWhite) })
            {
                float r = Mathf.Lerp(0.26f, 0.46f, (z + 4.6f) / 2.0f) + 0.015f;
                m.Lathe(shape, new List<Vector2> { new Vector2(z - 0.11f, r), new Vector2(z + 0.11f, r) }, slot, 12);
            }
            // A roundel on each fuselage side, behind the cockpit (Efrati p. 11: D-120's "left fuselage identification
            // roundel", D-123's photograph; the Sherut Avir aircraft had roundels "on the sides of the fuselage", p. 6).
            foreach (var side in new[] { 1f, -1f })
                Roundel(m, TRS(V(side * 0.42f, 0.10f, -2.15f), V(0, 0, -side * 90f), Vector3.one), 0.42f, turnDegrees: 30f);   // its face (local +Y) outwards
            // Spinner: the big Jumo spinner, in the airframe's green.
            m.Lathe(TRS(V(0, 0.02f, 2.95f), V(90f, 0, 0), Vector3.one), new List<Vector2> { new Vector2(0f, 0.40f), new Vector2(0.30f, 0.30f), new Vector2(0.55f, 0f) },
                P.AviaGreen, 10);
            m.Ellipsoid(TRS(V(0, 0.58f, -0.6f), Vector3.zero, V(0.30f, 0.28f, 0.85f)), P.Glass);
            // Straight tapered wing with rounded tips: semi-span 4.6 m beyond the fuselage, low on it, 6.5 degree dihedral.
            float semi = 4.6f;
            System.Func<float, float> tip = s => s < 0.8f ? 1f : 0.15f + 0.85f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((s - 0.8f) / 0.2f, 2f)));
            foreach (var side in new[] { 1f, -1f })
            {
                var w = TRS(V(side * 0.35f, -0.40f, 0.25f), V(0, 0, side * 6.5f), V(side, 1f, 1f));
                m.Wing(w, s => (0.70f - 0.20f * s) * tip(s), s => (1.30f - 0.60f * s) * tip(s), semi, 0.06f, (i, j) => P.AviaGreen, (i, j) => P.AviaGreen,
                    spanSteps: 10, chordSteps: 3);
                var rp = w.MultiplyPoint3x4(V(semi * 0.62f, 0f, -0.25f));
                Roundel(m, TRS(rp - V(0, 0.11f, 0), V(0, 0, side * 6.5f), V(1f, -1f, 1f)), 0.48f);   // underneath
                // The MG 151 pod and the radiator under each wing.
                var pod = w.MultiplyPoint3x4(V(1.65f, -0.12f, 0f));
                m.Rod(pod + V(0, 0, -0.3f), pod + V(0, 0, 1.0f), 0.10f, 0.07f, P.AviaGreen, segments: 6);
                m.Box(TRS(w.MultiplyPoint3x4(V(1.0f, -0.14f, -0.5f)), V(0, 0, side * 6.5f), V(0.55f, 0.18f, 0.9f)), P.AviaGreen);
                // Tailplane.
                var tp = TRS(V(side * 0.12f, 0.25f, -4.9f), Vector3.zero, V(side, 1f, 1f));
                m.Wing(tp, s => (0.45f - 0.15f * s) * tip(s), s => (0.45f - 0.10f * s) * tip(s), 1.45f, 0.05f, (i, j) => P.AviaGreen, (i, j) => P.AviaGreen,
                    spanSteps: 5, chordSteps: 2);
            }
            // Fin and rudder, in the airframe's green.
            System.Func<int, int, int> fin = (i, j) => P.AviaGreen;
            m.Wing(TRS(V(0, 0.35f, -5.05f), V(0, 0, 90f), Vector3.one), s => (0.55f - 0.15f * s) * tip(s), s => (0.55f - 0.10f * s) * tip(s),
                1.35f, 0.05f, fin, fin, spanSteps: 5, chordSteps: 2);
            return m.Build(name, AircraftShade);
        }

        /// <summary>The S-199's three paddle blades (the Jumo 211F's propeller: Avia S-199 article), centred on its hub; turns
        /// about +Z. Placed at <see cref="S199PropellerHub"/> in the aircraft's frame.</summary>
        public static Mesh S199Propeller(string name)
        {
            var m = new MiniatureMesh();
            for (int b = 0; b < 3; b++)
            {
                var dir = Quaternion.Euler(0, 0, 120f * b + 30f) * Vector3.up;
                m.Rod(dir * 0.25f, dir * 1.55f, 0.20f, 0.17f, P.PropBlack, segments: 5);
            }
            return m.Build(name, AircraftShade);
        }

        public static readonly Vector3 S199PropellerHub = new Vector3(0f, 0.02f, 3.5f);

        /// <summary>The Spitfire's four-blade propeller, centred on its hub; turns about +Z. Placed at
        /// <see cref="SpitfirePropellerHub"/> in the aircraft's frame.</summary>
        public static Mesh SpitfirePropeller(string name)
        {
            var m = new MiniatureMesh();
            for (int b = 0; b < 4; b++)
            {
                var dir = Quaternion.Euler(0, 0, 90f * b + 20f) * Vector3.up;
                m.Rod(dir * 0.2f, dir * 1.6f, 0.13f, 0.08f, P.PropBlack, segments: 5);
            }
            return m.Build(name, AircraftShade);
        }

        public static readonly Vector3 SpitfirePropellerHub = new Vector3(0f, 0.02f, 3.55f);

        /// <summary>The aircraft's one shade row: the palette's base colour, a touch under its top highlight.</summary>
        public const float AircraftShade = 0.72f;

        /// <summary>The star's points as a share of the roundel's radius: clear of the disc's edge (FOTW, citing Born in
        /// Battle; the 1948 photographs in Efrati, Colors &amp; Markings of the Israeli Air Force, 2005, pp. 11-14).</summary>
        public const float RoundelStarShare = 0.74f;

        /// <summary>The two triangles of the roundel's star (radius 1, centred on the origin), each a filled plate.</summary>
        public static List<Vector2>[] RoundelStar(float turnDegrees = 0f)
        {
            var tris = new List<Vector2>[2];
            for (int k = 0; k < 2; k++)
            {
                tris[k] = new List<Vector2>();
                for (int i = 0; i < 3; i++)
                {
                    float a = Mathf.Deg2Rad * (90f + turnDegrees + 60f * k + 120f * i);
                    tris[k].Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RoundelStarShare);
                }
            }
            return tris;
        }

        /// <summary>The IAF roundel of 1948-49: a white disc with a FILLED blue Star of David, its points clear of the
        /// disc's edge, and no outer ring.
        /// - Filled, not an outline: "Ground visibility of the open star was poor so he [Boris Senior] suggested that the
        ///   Star of David be filled resulting in the IAF insignia as applied on IAF aircraft ever since" (Yoav Efrati,
        ///   Colors &amp; Markings of the Israeli Air Force, IAF, 2005, p. 6, on the Bonanzas after 15 May 1948). The 1948
        ///   photographs of 101 Squadron's S-199s and Spitfires (Efrati pp. 11-14; Commons, public domain) show the star
        ///   filled.
        /// - No ring: a thin blue ring appears only on 1950s Mosquitos and Meteors (FOTW, citing Born in Battle).
        /// The plates are stacked a hair apart so they never z-fight; the plane is the local XZ, facing +Y.
        /// <paramref name="turnDegrees"/> 30 sets a point straight up on a fuselage side, as the photographs show.</summary>
        static void Roundel(MiniatureMesh m, Matrix4x4 at, float radius, float turnDegrees = 0f)
        {
            m.Plate(at, Circle(radius, 20), P.RoundelWhite, shade: 0.8f);
            var star = RoundelStar(turnDegrees);
            for (int k = 0; k < 2; k++)
                m.Plate(at * T(0, 0.012f + 0.002f * k, 0), star[k].ConvertAll(v => v * radius), P.RoundelBlue, shade: 0.8f);
        }

        static List<Vector2> Circle(float r, int n)
        {
            var pts = new List<Vector2>(n);
            for (int i = 0; i < n; i++) { float a = 2f * Mathf.PI * i / n; pts.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return pts;
        }

        /// <summary>A modified Flower-class corvette as the Israeli Navy commissioned it in 1948 (INS Haganah, K-20, ex-HMCS
        /// Norsyd; sources in docs/units.md): a 63.4 m (208 ft) hull with a raised forecastle, a bridge, one tall funnel, a mast and a
        /// single gun forward. Origin at the waterline amidships, bow towards +Z.</summary>
        public static Mesh Corvette(string name)
        {
            var m = new MiniatureMesh();
            // Hull: a lathe along Z, flattened into a hull section, its keel below the waterline (hidden by the sea).
            var hull = new List<Vector2> { new Vector2(-31.7f, 1.2f), new Vector2(-26f, 4.2f), new Vector2(-10f, 5.0f), new Vector2(8f, 5.0f),
                new Vector2(20f, 4.0f), new Vector2(28.5f, 2.0f), new Vector2(31.7f, 0.2f) };
            m.Lathe(TRS(V(0, 0.6f, 0), V(90f, 0, 0), V(1f, 1f, 0.95f)), hull, P.HullGrey, 14, shade: 0.75f);
            m.Lathe(TRS(V(0, -0.2f, 0), V(90f, 0, 0), V(1.01f, 1f, 0.55f)), hull, P.Waterline, 14, shade: 0.5f);
            // Decks: main deck and raised forecastle.
            m.Box(TRS(V(0, 4.5f, -4f), Vector3.zero, V(8.6f, 0.4f, 46f)), P.Deck, shade: 0.8f);
            m.Box(TRS(V(0, 5.6f, 17f), Vector3.zero, V(8.0f, 2.2f, 18f)), P.HullGrey, shade: 0.8f);
            // Bridge and wheelhouse.
            m.Box(TRS(V(0, 8.2f, 5.5f), Vector3.zero, V(6.2f, 4.0f, 6.0f)), P.HullGrey, shade: 0.9f);
            m.Box(TRS(V(0, 10.8f, 6.0f), Vector3.zero, V(7.2f, 1.2f, 4.0f)), P.HullGrey, shade: 1f);
            // Funnel, with a dark cap: upright, as the modified Flowers had it (the 1948 photograph shows it so).
            m.Lathe(TRS(V(0, 5f, -3.5f), Vector3.zero, V(1f, 1f, 1.35f)), new List<Vector2> { new Vector2(0f, 1.9f), new Vector2(7.0f, 1.8f) }, P.HullGrey, 12, shade: 0.95f);
            m.Lathe(TRS(V(0, 5f, -3.5f), Vector3.zero, V(1f, 1f, 1.35f)), new List<Vector2> { new Vector2(7.0f, 1.82f), new Vector2(7.8f, 1.82f) }, P.Funnel, 12, shade: 1f);
            // Mast with a yard.
            m.Rod(V(0, 11.4f, 4.0f), V(0, 21f, 3.2f), 0.30f, 0.18f, P.Funnel, segments: 6, shade: 1f);
            m.Rod(V(-3.2f, 18f, 3.4f), V(3.2f, 18f, 3.4f), 0.14f, 0.14f, P.Funnel, segments: 5, shade: 1f);
            // The forward gun: a shield and a barrel.
            m.Box(TRS(V(0, 7.6f, 20f), Vector3.zero, V(2.4f, 1.8f, 2.0f)), P.HullGrey, shade: 0.95f);
            m.Rod(V(0, 7.8f, 20.8f), V(0, 8.4f, 25.5f), 0.22f, 0.18f, P.Funnel, segments: 6, shade: 0.9f);
            // Boats and depth-charge rails aft (small boxes, they read as deck clutter).
            foreach (var side in new[] { -1f, 1f })
                m.Box(TRS(V(side * 3.2f, 6.2f, -10f), Vector3.zero, V(1.4f, 1.0f, 5.5f)), P.RoundelWhite, shade: 0.85f);
            m.Box(TRS(V(0, 5.2f, -26f), Vector3.zero, V(5f, 0.8f, 3f)), P.Funnel, shade: 0.8f);
            return m.Build(name);
        }
    }
}
