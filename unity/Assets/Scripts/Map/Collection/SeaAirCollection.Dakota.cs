#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's Egyptian Dakota C-47 (owner, 2 November 2026, "Over Tel Aviv, 15 May-11 Jun (Recommended)": "A
    /// slow loop over Tel Aviv on those turns, like the Spitfire's loop, with the Egyptian flag; purely visual"; on #2274, "Off the coast is fine (Recommended)": its loop is just off the city, over the sea). It follows the
    /// Spitfire's path in this layer: the far mesh streamed after the first frame, drawn in flight at the unit tokens' aircraft
    /// scale and altitude, never on a hex or an airfield, with Egypt's flag as the owner ruled for the Spitfire on #2260
    /// ("Smaller flag, no pole": a small flag floating over the tail).</summary>
    public sealed partial class SeaAirCollection
    {
        /// <summary>The Dakota's object key.</summary>
        public const string DakotaKey = "tel-aviv-dakota";

        /// <summary>The first Egyptian bombing period over Tel Aviv: "תקופת ההפצצות הראשונה נמשכה מ-15 במאי ועד כניסת ההפוגה
        /// הראשונה לתוקפה ב-11 ביוני. שיטת התקיפה המצרית הייתה בדרך כלל גיחה של שני מטוסי הפצצה, בדרך כלל מטוסי דקוטה" (he.wiki
        /// "הפצצת תל אביב במלחמת העצמאות", revision 43856594; agent-read 2 November 2026, a person's check pending; the paragraph
        /// has no footnote of its own). On 3 June 1948 two Egyptian C-47s that bombed Tel Aviv were shot down (en.wiki "Modi
        /// Alon", revision 1375428559; he.wiki "מודי אלון", revision 43744773; both agent-read 2 November 2026). On the game's
        /// monthly turns (the 15th) it shows on the 15 May 1948 turn only.</summary>
        public static readonly DateTime DakotaFrom = new DateTime(1948, 5, 15), DakotaTo = new DateTime(1948, 6, 11);

        /// <summary>The C-47's wingspan, 95 ft 6 in (29.11 m): en.wiki "Douglas C-47 Skytrain", Specifications (C-47B-DK),
        /// revision 1383378030, agent-read 2 November 2026. At the unit tokens' aircraft scale (420x, as the Spitfire, #2014)
        /// it is 12.2 km across.</summary>
        public const float DakotaSpanM = 95.5f * FootM;

        /// <summary>The settlement it loops by: Tel Aviv's row in content/map/settlements_1948.yaml.</summary>
        public const string DakotaTown = "tel_aviv";

        /// <summary>DESIGN VALUES: the Dakota's loop, the Spitfire's ellipse (<see cref="SpitfireLoopHalfLengthM"/> by
        /// <see cref="SpitfireLoopHalfWidthM"/>, "like the Spitfire's loop"), flown once every 60 s, half again the Spitfire's
        /// period: a slower aircraft, about 430 m/s of map. In east along its southern side, then anticlockwise, as the
        /// Spitfire's.</summary>
        public const float DakotaLoopSeconds = 60f;

        /// <summary>DESIGN VALUE: how far apart (m) the candidate loop centres are, in rings round Tel Aviv and in bearings of 10
        /// degrees, nearest first (<see cref="DakotaSpot"/>).</summary>
        public const float DakotaSearchStepM = 1000f;
        const int DakotaSearchRings = 90, DakotaSearchBearings = 36;

        /// <summary>DESIGN VALUE: the flag's size over the tokens' flag size, as #2260's SpitfireFlagScale (0.5).</summary>
        public const float DakotaFlagScale = 0.5f;

        /// <summary>The flag object beside the aircraft (it does not turn with it): the aircraft's key and this.</summary>
        public const string DakotaMarksSuffix = "-marks", DakotaFlagName = "Flag";

        /// <summary>Where the Dakota is from its loop's centre (world x, z, m), <paramref name="seconds"/> into its loop: at 0 due
        /// south of the centre flying east, then anticlockwise seen from above.</summary>
        public static Vector2 DakotaLoopOffset(float seconds)
        {
            float a = DakotaLoopAngle(seconds);
            return new Vector2(SpitfireLoopHalfLengthM * Mathf.Sin(a), -SpitfireLoopHalfWidthM * Mathf.Cos(a));
        }

        /// <summary>The Dakota's direction of travel there (world x, z; unit length): the loop's tangent.</summary>
        public static Vector2 DakotaLoopHeading(float seconds)
        {
            float a = DakotaLoopAngle(seconds);
            return new Vector2(SpitfireLoopHalfLengthM * Mathf.Cos(a), SpitfireLoopHalfWidthM * Mathf.Sin(a)).normalized;
        }

        static float DakotaLoopAngle(float seconds) => 2f * Mathf.PI * Mathf.Repeat(seconds, DakotaLoopSeconds) / DakotaLoopSeconds;

        /// <summary>The Dakota's farthest point from its centre, seen from above, at any heading: half the diagonal of a square
        /// as wide as its wingspan (its fuselage is shorter than its span).</summary>
        public static float DakotaReach => 0.5f * Mathf.Sqrt(2f) * SizeOf(Dakota);

        /// <summary>Half the box (world x, z, m) the Dakota flies through about its loop's centre: the loop and its reach.</summary>
        public static Vector2 DakotaSweptHalf => new Vector2(SpitfireLoopHalfLengthM + DakotaReach, SpitfireLoopHalfWidthM + DakotaReach);

        /// <summary>The Dakota's loop centre (on the ground, world x, z): the nearest to Tel Aviv whose whole swept box
        /// (<see cref="DakotaSweptHalf"/>) is over the board and clear of every unit token present in its window (their rings
        /// and town clearings), every settlement's and town's kit footprint, and every map label's point (the settlements, the
        /// 1948 localities and the map locations), so it hides no token and no name. Over the city itself it cannot be: Kiryati's
        /// token stands on Tel Aviv from 15 May 1948 (content/map/units_1948.yaml), and at 420x the aircraft is 12 km across.
        /// Rings of <see cref="DakotaSearchStepM"/> round Tel Aviv, nearest first, each from due west (the sea) turning both ways.
        /// On the map that is 22 km due west of Tel Aviv's point, over the sea; the owner reviewed it on #2274 (2 November 2026):
        /// "Off the coast is fine (Recommended)". Null when none is clear.</summary>
        internal static Vector2? DakotaSpot(MapScene map, Statecraft.Sim.Content? content)
        {
            var town = map.Settlements.FirstOrDefault(s => s.Id == DakotaTown);
            if (town == null) return null;
            var w = map.Frame.ToWorld(town.Lon, town.Lat, 0); var origin = new Vector2(w.x, w.z);
            var ground = PlacementLayer.CampGround(map);
            var half = DakotaSweptHalf;
            var discs = new List<(Vector2 centre, float radius)>();
            foreach (var kv in map.UnitSpots)
            {
                if (UnitData.Day(kv.Key.From) > DakotaTo || UnitData.Day(kv.Key.To) <= DakotaFrom) continue;
                var c = new Vector2((float)kv.Value.X, (float)kv.Value.Z);
                discs.Add((c, TokenRadius(map, c, kv.Value.InTown)));
            }
            foreach (var (c, r, _) in map.KitFootprints) discs.Add((c, r));
            var points = new List<Vector2>();
            void Point(double lon, double lat) { var p = map.Frame.ToWorld(lon, lat, 0); points.Add(new Vector2(p.x, p.z)); }
            foreach (var s in map.Settlements) Point(s.Lon, s.Lat);
            foreach (var l in map.Localities) Point(l.Settlement.Lon, l.Settlement.Lat);
            if (content != null) foreach (var l in content.MapLocations) Point(l.Lon, l.Lat);
            bool Free(Vector2 c)
            {
                if (!ground.Inside(c.x - half.x, c.y - half.y) || !ground.Inside(c.x + half.x, c.y + half.y)) return false;
                foreach (var p in points) if (Mathf.Abs(p.x - c.x) <= half.x && Mathf.Abs(p.y - c.y) <= half.y) return false;
                foreach (var (d, r) in discs)
                {
                    float dx = Mathf.Max(Mathf.Abs(d.x - c.x) - half.x, 0f), dz = Mathf.Max(Mathf.Abs(d.y - c.y) - half.y, 0f);
                    if (dx * dx + dz * dz <= r * r) return false;
                }
                return true;
            }
            for (int k = 0; k <= DakotaSearchRings; k++)
                for (int i = 0; i < (k == 0 ? 1 : DakotaSearchBearings); i++)
                {
                    int step = (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);   // 0, +1, -1, +2, -2 ... bearings from due west
                    float a = Mathf.PI + 2f * Mathf.PI * step / DakotaSearchBearings;
                    var c = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (k * DakotaSearchStepM);
                    if (Free(c)) return c;
                }
            return null;
        }

        /// <summary>The ring (out of town) or clearing (in a town) a unit token holds at <paramref name="c"/>, as MapScene placed
        /// it; the largest of either when none matches.</summary>
        static float TokenRadius(MapScene map, Vector2 c, bool inTown)
        {
            if (inTown) { foreach (var (k, r) in map.UnitClearings) if ((k - c).sqrMagnitude < 1f) return r; }
            else foreach (var (k, r, _) in map.UnitTokens) if ((k - c).sqrMagnitude < 1f) return r;
            float most = 0f;
            foreach (var (_, r) in map.UnitClearings) most = Mathf.Max(most, r);
            foreach (var (_, r, _) in map.UnitTokens) most = Mathf.Max(most, r);
            return most;
        }

        bool dakotaSearched; Vector2? dakotaCentre;
        /// <summary><see cref="DakotaSpot"/> for this map, found once (the map's tokens and labels do not move).</summary>
        Vector2? DakotaCentre()
        {
            if (!dakotaSearched && map != null) { dakotaSearched = true; dakotaCentre = DakotaSpot(map, content); }
            return dakotaCentre;
        }

        /// <summary>The Dakota in level flight round its loop about <paramref name="at"/> (<see cref="DakotaLoop"/>), wingspan at
        /// its map size, UnitShowcase.AltitudeM (4,200 m) above the highest ground under its loop, as the Spitfire (#2014). Its
        /// model's nose is its mesh's -y, as the Spitfire's (the FBX's wings lie forward of its middle, its tailplane at +y), so
        /// the same yaw points it east. Egypt's flag floats over its tail. The labels keep clear of the whole box it and its flag
        /// fly through.</summary>
        void FlyDakota(GameObject go, GameObject prefab, Vector2 at)
        {
            var level = Quaternion.Euler(0f, SpitfireNoseEastYaw, 0f) * prefab.transform.localRotation;
            go.transform.localRotation = level;
            var b = Bounds(go); float span = b.size.z;
            go.transform.localScale *= span > 1e-3f ? SizeOf(Dakota) / span : 1f;
            var centre = new Vector3(at.x, UnitShowcase.LoopFloor(map!, new Vector3(at.x, 0f, at.y), SpitfireLoopHalfLengthM + SizeOf(Dakota))
                + UnitShowcase.AltitudeM, at.y);
            go.transform.position = centre; b = Bounds(go);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
            // The flag stands beside the aircraft in this layer, so it does not turn with it, and goes with it (its model's
            // originals) when its dates end.
            var marks = new GameObject(go.name + DakotaMarksSuffix);
            marks.transform.SetParent(transform, false);
            originals[Dakota].Add(marks);
            var flag = DakotaFlag(marks.transform);
            var half = 0.5f * b.size;   // nose east: half its length (x), its height and its span (z)
            go.AddComponent<DakotaLoop>().Configure(centre, level, b.center - go.transform.position, half, flag,
                !Application.isPlaying || VisualShots.Requested());
            var cam = Camera.main;
            if (flag != null && cam != null) UnitFlagFacing.Face(flag, cam.transform.position);
            Placed.Add((go.name, Dakota, centre));
            float reach = 0.5f * new Vector2(b.size.x, b.size.z).magnitude + 1f, up = half.y + 1f;
            if (flag != null)
            {
                var f = flag.GetComponent<MeshFilter>().sharedMesh.bounds; float k = flag.lossyScale.y;
                up = Mathf.Max(up, half.y + DakotaLoop.FlagClearM + 2f * f.extents.y * k + 1f);
            }
            var keep = new Bounds();
            keep.SetMinMax(centre - new Vector3(SpitfireLoopHalfLengthM + reach, half.y + 1f, SpitfireLoopHalfWidthM + reach),
                centre + new Vector3(SpitfireLoopHalfLengthM + reach, up, SpitfireLoopHalfWidthM + reach));
            Keep(go, keep);
        }

        /// <summary>Egypt's flag as the owner ruled for the Spitfire on #2260 (2 November 2026, "Smaller flag, no pole"): the cloth
        /// alone (UnitFlags.BuildCloth, the sourced 1948 flag, UnitFlags), in the tokens' unlit cloth colours, at
        /// <see cref="DakotaFlagScale"/> of the tokens' flag size, turned to the camera (UnitFlagFacing). Its mesh and materials
        /// are owned by the flag and destroyed with it. Null, with a warning, when the units' materials are missing: the
        /// aircraft flies on without it.</summary>
        static Transform? DakotaFlag(Transform parent)
        {
            var ring = Resources.Load<Material>(UnitMaterials.RingResource);
            if (ring == null)
            {
                Debug.LogWarning("SEA/AIR: the unit materials are missing (run UnitMaterialAssets.Create); the Dakota flies without its flag.");
                return null;
            }
            var go = new GameObject(DakotaFlagName);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * (DakotaFlagScale * UnitBadge.FigureHeightM * UnitShowcase.FigureScale);
            var owned = go.AddComponent<MapRuntimeAssets>();
            go.AddComponent<MeshFilter>().sharedMesh = UnitPlinth.Upload(owned.Own(UnitFlags.BuildCloth(UnitFlags.Egypt)!));
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = UnitFlags.ClothMaterials(UnitFlags.Egypt, ring, m => owned.Own(m));
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<UnitFlagFacing>();
            return go.transform;
        }
    }
}
