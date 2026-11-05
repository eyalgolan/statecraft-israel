#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's Egyptian Dakota C-47, two of them, bombing Tel Aviv on the turns of the first bombing period. The
    /// owner, 2 November 2026, "Over Tel Aviv, 15 May-11 Jun (Recommended)": "A slow loop over Tel Aviv on those turns, like the
    /// Spitfire's loop, with the Egyptian flag; purely visual"; on #2274, "Off the coast is fine (Recommended)", so #2274 drew one
    /// Dakota looping just off the city, over the sea. On #2482 (4 November 2026), asked how far the bombing should go, the owner
    /// chose option 1 and added: "1 + make the visual 2 slightly smaller planes and not 0ne big one". Option 1 read: "On the
    /// bombing turns it flies over Tel Aviv (not just off the coast) and drops bombs with flashes and smoke on the city. Looks
    /// only; no change to the game's numbers." Before that the owner said: "we need the egyptian dakota to bomb tel aviv. it
    /// also doesnt look like its flying". So the 2 November off-the-coast placement gives way: two Dakotas, each
    /// <see cref="DakotaScale"/> of #2274's size, fly one loop as a pair, its pass in from the sea over Tel Aviv's hex, dropping
    /// bombs that flash and smoke on that hex only; and they fly as the Spitfire does (#2260): turning propellers, here on both
    /// engines, a bank into their turns (<see cref="SeaAirCollection.BankFor"/>), a shadow on the ground and Egypt's flag over
    /// each tail (owner on #2260, "Smaller flag, no pole"). Never on a hex or an airfield: in flight at the unit tokens' aircraft
    /// scale and altitude, the far mesh streamed after the first frame. Presentation only: no sim, content or hash change.</summary>
    public sealed partial class SeaAirCollection
    {
        /// <summary>The Dakotas' object keys are made from this.</summary>
        public const string DakotaKey = "tel-aviv-dakota";

        /// <summary>The two Dakotas' keys, the lead first, then its wing, which trails it round the loop
        /// (<see cref="DakotaTrailSeconds"/>). Two, for the owner's "2 slightly smaller planes" (#2482) and the sourced sortie of
        /// two bombers (<see cref="DakotaFrom"/>).</summary>
        public static readonly string[] DakotaKeys = { DakotaKey + "-lead", DakotaKey + "-wing" };

        /// <summary>The first Egyptian bombing period over Tel Aviv, he.wiki "הפצצת תל אביב במלחמת העצמאות", revision 43856594,
        /// section "תקופת ההפצצות הראשונה", two paragraphs, neither with a footnote of its own: "תקופת ההפצצות הראשונה נמשכה
        /// מ-15 במאי ועד כניסת ההפוגה הראשונה לתוקפה ב-11 ביוני." and "שיטת התקיפה המצרית הייתה בדרך כלל גיחה של שני מטוסי
        /// הפצצה, בדרך כלל מטוסי דקוטה, שלוו במטוס קרב אחד או שניים מדגם ספיטפייר." (agent-read 2 November 2026 and re-read 4
        /// November 2026, still the current revision; a person's check pending). The same article: "ב-3 ביוני אחר הצהריים פגעו
        /// הפצצות המצריות בכיכר מסריק" and Modi Alon shot down "שני מטוסי דקוטה מצרים ששימשו כמפציצים". he.wiki "מודי אלון",
        /// revision 43744773, calls them "זוג מטוסי DC-3 דקוטה מצריים"; en.wiki "Modi Alon", revision 1375428559, "a pair of
        /// Egyptian C-47s and their two Spitfire escorts" (all agent-read 4 November 2026). On the game's monthly turns (the
        /// 15th) the Dakotas show on the 15 May 1948 turn only in this period (and again in the second, DakotaSecondFrom).</summary>
        public static readonly DateTime DakotaFrom = new DateTime(1948, 5, 15), DakotaTo = new DateTime(1948, 6, 11);

        /// <summary>The second Egyptian bombing period over Tel Aviv, the Ten Days, 8 to 17 July 1948: the same he.wiki article and
        /// revision, section "תקופת ההפצצות השנייה": "תקופת ההפצצות השנייה על תל אביב התרחשה במהלך קרבות עשרת הימים (8 עד 17
        /// ביולי)" and "ב-17 ביולי הופצצה תל אביב בפעם האחרונה". That the bombers were Dakotas this time: en.wiki "1948
        /// Arab–Israeli War", revision 1375001749, section on the Ten Days, citing Morris 2008, pp. 294-295: "The Egyptians
        /// responded with raids on Tel Aviv by Dakota aircraft accompanied by a Spitfire fighter escort"; he.wiki's 13 July
        /// paragraph has "שני מפציצים ושני מטוסי קרב מצרים", the type not named (all agent-read 5 November 2026, a person's
        /// check pending; research_notes/GDD phase 7/tel-aviv-bombing-july-1948.md). On the game's monthly turns the Dakotas
        /// show again on the 15 July 1948 turn, as on 15 May, with Tel Aviv's air defence (SeaAirCollection.AirDefence.cs).</summary>
        public static readonly DateTime DakotaSecondFrom = new DateTime(1948, 7, 8), DakotaSecondTo = new DateTime(1948, 7, 17);

        /// <summary>True in either bombing period (<see cref="DakotaFrom"/>, <see cref="DakotaSecondFrom"/>).</summary>
        public static bool DakotaWanted(DateTime date) => (date >= DakotaFrom && date <= DakotaTo) || (date >= DakotaSecondFrom && date <= DakotaSecondTo);

        /// <summary>The C-47's wingspan, 95 ft 6 in (29.11 m): en.wiki "Douglas C-47 Skytrain", Specifications (C-47B-DK),
        /// revision 1383378030, agent-read 2 November 2026. At the unit tokens' aircraft scale (420x, as the Spitfire, #2014)
        /// it is 12.2 km across; each of the two is drawn at <see cref="DakotaScale"/> of that.</summary>
        public const float DakotaSpanM = 95.5f * FootM;

        /// <summary>DESIGN VALUE: each Dakota's size over #2274's one (the unit tokens' aircraft scale): the owner's "2 slightly
        /// smaller planes and not 0ne big one" (#2482). 0.8 makes each 9.8 km across, still more than twice the Spitfire's 4.7 km.</summary>
        public const float DakotaScale = 0.8f;

        /// <summary>The settlement they bomb: Tel Aviv's row in content/map/settlements_1948.yaml, and its hex in
        /// content/map/settlement_hexes.yaml.</summary>
        public const string DakotaTown = "tel_aviv";

        /// <summary>DESIGN VALUES: the Dakotas' loop is the Spitfire's ellipse ("like the Spitfire's loop", 2 November) at
        /// <see cref="DakotaLoopScale"/> times its size, 15 km each way east-west and 9 km each way north-south, flown once every
        /// <see cref="DakotaLoopSeconds"/>, about 640 m/s of map, the Spitfire's own speed. #2274's one Dakota flew the
        /// Spitfire's own 10 by 6 km loop, smaller than the aircraft itself; two 9.8 km aircraft on one track need room to fly it
        /// apart (<see cref="DakotaTrailSeconds"/>). The unit tokens' own aircraft fly a 16 km radius (UnitShowcase.LoopRadiusM),
        /// so this is their size of loop. In east along its southern side, then anticlockwise, as the Spitfire's.</summary>
        public const float DakotaLoopScale = 3f, DakotaLoopSeconds = 120f;
        public const float DakotaLoopHalfLengthM = DakotaLoopScale * SpitfireLoopHalfLengthM, DakotaLoopHalfWidthM = DakotaLoopScale * SpitfireLoopHalfWidthM;

        /// <summary>DESIGN VALUE: the wing trails the lead by this share of the loop (26.4 s), on the same track. Seen from above,
        /// every point of a Dakota lies within <see cref="DakotaReachShare"/> of its span of its centre, so two never meet while
        /// their centres are more than 2 x 0.54 x 9.8 = 10.6 km apart; at 0.22 of the loop the closest they come, in the turns at
        /// the ends, is 11.5 km (sampled along this ellipse), and on the passes they fly about 15 km apart, the wing's nose 7 km
        /// behind the lead's tail: close enough to read as a pair, never across the loop from each other.</summary>
        public const float DakotaTrailShare = 0.22f, DakotaTrailSeconds = DakotaTrailShare * DakotaLoopSeconds;

        /// <summary>DESIGN VALUE, from the model's FBX vertices (generated-1948-16-egyptian-dakota-c47-compatibility.fbx, read 4
        /// November 2026): seen from above, the farthest points from the model's middle are its wingtips, at 0.53 of its span.</summary>
        public const float DakotaReachShare = 0.54f;

        /// <summary>DESIGN VALUE: the flag's size over the tokens' flag size, as #2260's SpitfireFlagScale (0.5).</summary>
        public const float DakotaFlagScale = 0.5f;

        /// <summary>The object beside each aircraft (it neither banks nor turns with it) that holds its flag, its shadow and its
        /// bombs: the aircraft's key and this. The propeller discs, children of the aircraft's mesh.</summary>
        public const string DakotaMarksSuffix = "-marks", DakotaFlagName = "Flag", DakotaShadowName = SpitfireShadowName, DakotaPropellerName = SpitfirePropellerName;

        /// <summary>DESIGN VALUES for the two propeller discs on the fused mesh, from the model's FBX vertices (read 4 November
        /// 2026; the mesh's -y is its nose, +z up, x across): the fuselage's nose is the front-most point, on the centre line;
        /// each engine nacelle's front (its spinner) stands 0.131 of the length behind it, its vertices centred 0.128 of the span
        /// either side of the centre line and 0.40 of the height up, and its still blades reach about 0.06 of the span round it
        /// (the nacelle's front vertices run 0.07 to 0.20 of the span out). Each disc's centre stands
        /// <see cref="DakotaPropBehindNoseShare"/> of the length behind the front, just behind its spinner over the blades' roots,
        /// <see cref="DakotaPropRadiusShare"/> of the span in radius, inside the airframe's box at every angle.</summary>
        public const float DakotaEngineAcrossShare = 0.128f, DakotaPropBehindNoseShare = 0.145f, DakotaPropUpShare = 0.40f, DakotaPropRadiusShare = 0.06f;

        /// <summary>DESIGN VALUES for the bombs (option 1: "drops bombs with flashes and smoke on the city"). Each Dakota releases
        /// one every <see cref="DakotaBombIntervalS"/> of its loop while over Tel Aviv's hex, where it also lands on that hex at
        /// least <see cref="DakotaBombMarginM"/> inside its edge (so the smoke's drift stays on it); each falls for
        /// <see cref="DakotaBombFallS"/> (a real fall from 4,200 m takes about 29 s: shortened, as the map's speeds are), under
        /// the aircraft's track, faster as it falls. The interval divides the loop, so the same releases repeat every loop.</summary>
        public const float DakotaBombIntervalS = 1f, DakotaBombFallS = 2f, DakotaBombMarginM = 600f;

        /// <summary>DESIGN VALUES for the impact, sized to read at the gameplay zoom beside a 9.8 km aircraft and a 4.4 km hex: a
        /// flash <see cref="DakotaFlashM"/> across for <see cref="DakotaFlashS"/>, then <see cref="DakotaPuffs"/> puffs of smoke
        /// <see cref="DakotaPuffGapS"/> apart, each rising and thinning for <see cref="DakotaSmokeS"/>, the muzzle smoke's curves
        /// (UnitGunFire.Flash, UnitGunFire.Puff) at <see cref="DakotaSmokeM"/> for the gun's length. A bomb is a dark spindle
        /// <see cref="DakotaBombLengthM"/> long.</summary>
        public const float DakotaFlashS = 0.5f, DakotaFlashM = 1500f, DakotaSmokeS = 4f, DakotaPuffGapS = 0.25f, DakotaSmokeM = 1600f, DakotaBombLengthM = 400f;
        public const int DakotaPuffs = 3;

        /// <summary>DESIGN VALUES for the blast (owner, 4 November 2026, on the 15 May captures: "the explosions look bad in
        /// the screenshot and there is no smoke columns coming up", then "it needs to look top quality"): Unity Labs' CC0
        /// explosion flipbook (<see cref="DakotaBlastTexture"/>, 5 x 5 frames: a fireball that burns out into black smoke) on a
        /// camera-facing quad, played once over <see cref="DakotaBlastS"/> from the impact. It opens at
        /// <see cref="DakotaBlastOpenShare"/> of <see cref="DakotaBlastM"/> across and swells to the full size in the first
        /// quarter of its life, its foot on the ground; it fades over the last <see cref="DakotaBlastFadeShare"/> of it. The
        /// gun's flash (<see cref="DakotaFlashM"/>) still marks the first instant.</summary>
        public const float DakotaBlastS = 2.4f, DakotaBlastM = 2200f, DakotaBlastOpenShare = 0.45f, DakotaBlastFadeShare = 0.35f;
        public const int DakotaBlastGrid = 5;

        /// <summary>DESIGN VALUES for the smoke columns (owner, 4 November 2026: "there is no smoke columns coming up"): each
        /// Dakota lights <see cref="DakotaColumns"/> columns, at evenly spaced impacts of its pass (the lead and its wing take
        /// turns, so the pair lights at most 2 x <see cref="DakotaColumns"/> over Tel Aviv). From its first hit a column is a
        /// stream of <see cref="DakotaColumnPuffs"/> puffs of Unity Labs' CC0 smoke flipbook (<see cref="DakotaSmokeTexture"/>,
        /// 8 x 8 frames, boiling at <see cref="DakotaColumnFps"/>), each rising <see cref="DakotaColumnM"/> over
        /// <see cref="DakotaColumnLifeS"/>, widening from <see cref="DakotaColumnFootM"/> to <see cref="DakotaColumnTopM"/>
        /// across and leaning <see cref="DakotaColumnLeanM"/> downwind (east, a little north, as the under-attack look's smoke,
        /// HexUnderAttack.Smoke.Wind), less than <see cref="DakotaBombMarginM"/>, so a column stays over Tel Aviv's hex. It
        /// grows from the ground over its first <see cref="DakotaColumnLifeS"/> and then burns for as long as the Dakotas fly.</summary>
        public const int DakotaColumns = 3, DakotaColumnPuffs = 10, DakotaSmokeGrid = 8;
        public const float DakotaColumnLifeS = 14f, DakotaColumnM = 3600f, DakotaColumnFootM = 700f, DakotaColumnTopM = 2300f,
            DakotaColumnLeanM = 450f, DakotaColumnFps = 6f;

        /// <summary>The two Unity Labs flipbooks (Resources, "Free VFX image sequences and flipbooks", CC0; ASSETS.yaml).</summary>
        public const string DakotaBlastTexture = "Effects/Explosion01_5x5", DakotaSmokeTexture = "Effects/Cloud02_8x8";

        /// <summary>Where a Dakota is from its loop's centre (world x, z, m), <paramref name="seconds"/> into its loop: at 0 due
        /// south of the centre flying east (over Tel Aviv, <see cref="DakotaSpot"/>), then anticlockwise seen from above.</summary>
        public static Vector2 DakotaLoopOffset(float seconds)
        {
            float a = DakotaLoopAngle(seconds);
            return new Vector2(DakotaLoopHalfLengthM * Mathf.Sin(a), -DakotaLoopHalfWidthM * Mathf.Cos(a));
        }

        /// <summary>A Dakota's direction of travel there (world x, z; unit length): the loop's tangent.</summary>
        public static Vector2 DakotaLoopHeading(float seconds)
        {
            float a = DakotaLoopAngle(seconds);
            return new Vector2(DakotaLoopHalfLengthM * Mathf.Cos(a), DakotaLoopHalfWidthM * Mathf.Sin(a)).normalized;
        }

        static float DakotaLoopAngle(float seconds) => 2f * Mathf.PI * Mathf.Repeat(seconds, DakotaLoopSeconds) / DakotaLoopSeconds;

        /// <summary>The loop's curvature (1/m) <paramref name="seconds"/> into it, as <see cref="LoopCurvature"/> for this ellipse.</summary>
        public static float DakotaLoopCurvature(float seconds)
        {
            float t = DakotaLoopAngle(seconds), a = DakotaLoopHalfLengthM, b = DakotaLoopHalfWidthM;
            float c = Mathf.Cos(t), s = Mathf.Sin(t);
            return a * b / Mathf.Pow(a * a * c * c + b * b * s * s, 1.5f);
        }

        /// <summary>A Dakota's bank <paramref name="seconds"/> into its loop (degrees, its inner wing down; #2482 "a bank into their
        /// turns"): <see cref="BankFor"/> the curvature at the Spitfire's scale. The loop is the Spitfire's shape at
        /// <see cref="DakotaLoopScale"/>, so its curvature there is the Spitfire's over that scale, and this is the Spitfire's own
        /// bank at the same point of its loop: level on the passes (the bomb run among them), the tokens' 28 degrees at the ends.</summary>
        public static float DakotaBankDegrees(float seconds) => BankFor(DakotaLoopScale * DakotaLoopCurvature(seconds));

        /// <summary>A Dakota's farthest point from its centre, seen from above, at any heading, on the safe side for the board:
        /// half the diagonal of a square as wide as its wingspan.</summary>
        public static float DakotaReach => 0.5f * Mathf.Sqrt(2f) * SizeOf(Dakota);

        /// <summary>Half the box (world x, z, m) the Dakotas fly through about their loop's centre: the loop and their reach.</summary>
        public static Vector2 DakotaSweptHalf => new Vector2(DakotaLoopHalfLengthM + DakotaReach, DakotaLoopHalfWidthM + DakotaReach);

        /// <summary>The Dakotas' loop centre (on the ground, world x, z): <see cref="DakotaLoopHalfWidthM"/> due north of Tel Aviv's
        /// hex centre, so the loop's southern pass, flown level and due east in from the sea, crosses the middle of Tel Aviv's
        /// hex at the loop's start (#2482, option 1: "it flies over Tel Aviv (not just off the coast)"); edit mode and shots hold
        /// the lead there. #2274 searched instead for the nearest centre whose whole loop cleared every unit token, kit and map
        /// name, which put it 22 km out to sea (owner on #2274, 2 November 2026: "Off the coast is fine (Recommended)"); over the
        /// city no loop can clear Kiryati's token on Tel Aviv (content/map/units_1948.yaml), so those ground clearances go. The
        /// clearance of the aircraft at altitude stays: the map names keep clear of each aircraft where it is now
        /// (<see cref="FlyDakota"/>, <see cref="DakotaKeepBox"/>; owner, 4 November 2026, on #2490, "Shrink the keep-clear area
        /// (Recommended)", where they had kept clear of its whole loop as of the Spitfire's), and the whole loop is over the board. Null when Tel Aviv has no hex
        /// or the loop would leave the board.</summary>
        internal static Vector2? DakotaSpot(MapScene map, Statecraft.Sim.Content? content)
        {
            var ground = PlacementLayer.CampGround(map);
            var targets = DakotaTargets(ground, content);
            if (targets.Count == 0) return null;
            var c = targets[0] - DakotaLoopOffset(0f);
            var half = DakotaSweptHalf;
            return ground.Inside(c.x - half.x, c.y - half.y) && ground.Inside(c.x + half.x, c.y + half.y) ? c : (Vector2?)null;
        }

        /// <summary>The centres (world x, z) of Tel Aviv's hexes (content/map/settlement_hexes.yaml), on the hex grid the board
        /// draws (HexGrid, as KitGround reads it).</summary>
        internal static List<Vector2> DakotaTargets(KitGround ground, Statecraft.Sim.Content? content)
        {
            var list = new List<Vector2>();
            if (content == null) return list;
            foreach (var h in content.SettlementHexes)
                if (h.Id == DakotaTown)
                {
                    var w = HexGrid.ToWorld(HexGrid.Centre(h.Q, h.R, ground.HexSizeM), ground.TerrainOrigin, ground.Length);
                    list.Add(new Vector2(w.x, w.z));
                }
            return list;
        }

        /// <summary>True when <paramref name="p"/> (world x, z) is on one of <paramref name="targets"/>' hexes, at least
        /// <see cref="DakotaBombMarginM"/> inside its edge.</summary>
        internal static bool OnDakotaTarget(KitGround ground, List<Vector2> targets, Vector2 p)
        {
            var g = HexGrid.FromWorld(new Vector3(p.x, 0f, p.y), ground.TerrainOrigin, ground.Length);
            if (HexGrid.EdgeDistance(g, ground.HexSizeM) < DakotaBombMarginM) return false;
            var w = HexGrid.ToWorld(HexGrid.NearestCentre(g, ground.HexSizeM), ground.TerrainOrigin, ground.Length);
            foreach (var t in targets) if ((new Vector2(w.x, w.z) - t).sqrMagnitude < 1f) return true;
            return false;
        }

        bool dakotaSearched; Vector2? dakotaCentre;
        /// <summary><see cref="DakotaSpot"/> for this map, found once the content is known (the map's hexes do not move).</summary>
        Vector2? DakotaCentre()
        {
            if (!dakotaSearched && map != null && content != null) { dakotaSearched = true; dakotaCentre = DakotaSpot(map, content); }
            return dakotaCentre;
        }

        /// <summary>Dakota number <paramref name="plane"/> (0 the lead, 1 its wing, <see cref="DakotaKeys"/>) in flight round the
        /// loop about <paramref name="at"/> (<see cref="DakotaLoop"/>), the wing <see cref="DakotaTrailSeconds"/> behind, wingspan
        /// at its map size, UnitShowcase.AltitudeM (4,200 m) above the highest ground under the loop, as the Spitfire (#2014).
        /// Its model's nose is its mesh's -y, as the Spitfire's, so the same yaw points it east. Beside it, in its marks object
        /// (they go with it when its dates end): Egypt's flag over its tail, its shadow and its bombs. A turning disc at each
        /// engine. The labels keep clear of the box it and its flag fill now, banked or not (<see cref="DakotaKeepBox"/>), not of its
        /// whole loop (owner, 4 November 2026, on #2490: "Shrink the keep-clear area (Recommended)").</summary>
        void FlyDakota(GameObject go, GameObject prefab, Vector2 at, KitGround ground, int plane)
        {
            var level = Quaternion.Euler(0f, SpitfireNoseEastYaw, 0f) * prefab.transform.localRotation;
            go.transform.localRotation = level;
            var b = Bounds(go); float span = b.size.z, size = SizeOf(Dakota);
            go.transform.localScale *= span > 1e-3f ? size / span : 1f;
            var centre = new Vector3(at.x, UnitShowcase.LoopFloor(map!, new Vector3(at.x, 0f, at.y), DakotaLoopHalfLengthM + size)
                + UnitShowcase.AltitudeM, at.y);
            go.transform.position = centre; b = Bounds(go);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
            var marks = new GameObject(go.name + DakotaMarksSuffix);
            marks.transform.SetParent(transform, false);
            originals[Dakota].Add(marks);
            var flag = DakotaFlag(marks.transform);
            var half = 0.5f * b.size;   // nose east: half its length (x), its height and its span (z)
            var props = DakotaPropellers(go);
            var bombs = DakotaBombs(marks.transform, centre, half, ground, plane);
            var loop = go.AddComponent<DakotaLoop>();
            loop.Configure(centre, level, b.center - go.transform.position, half, plane * DakotaTrailSeconds, props, Shadow(marks.transform), flag,
                map!.Terrain, 0.5f * ShadowShare * new Vector2(size, b.size.x), ground.SeaLevelY, bombs, !Application.isPlaying || VisualShots.Requested());
            var cam = Camera.main;
            if (flag != null && cam != null) UnitFlagFacing.Face(flag, cam.transform.position);
            Placed.Add((go.name, Dakota, centre));
            // At any heading and bank the aircraft lies within half its box's diagonal of its centre; the flag floats over its
            // tail, at most the steepest bank's box top over the centre (SpitfireLoop.TopOver), as the Spitfire's (Fly).
            float reach = 0.5f * b.size.magnitude + 1f, up = reach, round = reach;
            if (flag != null)
            {
                var f = flag.GetComponent<MeshFilter>().sharedMesh.bounds; float k = flag.lossyScale.y;
                up = Mathf.Max(up, SpitfireLoop.TopOver(half, UnitShowcase.BankDegrees) + DakotaLoop.FlagClearM + 2f * f.extents.y * k + 1f);
                round = Mathf.Max(round, half.x + new Vector2(f.extents.x, f.extents.z).magnitude * k + 1f);
            }
            // The labels keep clear of where it is now, not of its whole loop (owner, 4 November 2026, on #2490: "Shrink the
            // keep-clear area (Recommended)"): its box follows it a DakotaKeepStepS step of the loop at a time (FollowDakotas).
            var follow = new DakotaKeep(go, loop, centre, reach, up, round);
            dakotaKeeps.Add(follow);
            follow.Step = DakotaKeepStep(loop.PlacedAt);
            Keep(go, DakotaKeepBox(centre, loop.Lag, follow.Step, reach, up, round));
        }

        /// <summary>DESIGN VALUE: the labels' keep-clear box for a Dakota covers its flight over one step of this many seconds of
        /// the loop's clock (about 650 m of map at its speed, against a 9.8 km aircraft), and moves on a step at a time: the
        /// labels re-place themselves at most once a step while the Dakotas fly, not every frame.</summary>
        public const float DakotaKeepStepS = 1f;

        /// <summary>The step of the loop's clock that <paramref name="seconds"/> falls in (<see cref="DakotaKeepStepS"/>).</summary>
        internal static long DakotaKeepStep(float seconds) => (long)Math.Floor((double)seconds / DakotaKeepStepS);

        /// <summary>The box (world) the map names keep clear of for a Dakota whose loop is centred at <paramref name="centre"/> (in
        /// the air), its clock <paramref name="lag"/> behind the loop's, through the loop's clock step <paramref name="step"/>
        /// (<see cref="DakotaKeepStepS"/>): the aircraft at every heading and bank (<paramref name="reach"/> round its drawn
        /// centre, <paramref name="round"/> across with its flag over the tail, <paramref name="up"/> above with its flag) at
        /// every point of its path in that step, sampled finely enough that the path's bow between samples (under a metre on
        /// this ellipse) is inside the pad. Owner, 4 November 2026, on #2490: "Shrink the keep-clear area (Recommended)": a name
        /// hides only where an aircraft and its flag are, not over the whole loop as #2274's box did.</summary>
        internal static Bounds DakotaKeepBox(Vector3 centre, float lag, long step, float reach, float up, float round)
        {
            const int Samples = 4; const float Pad = 10f;
            var box = new Bounds();
            for (int i = 0; i <= Samples; i++)
            {
                var o = DakotaLoopOffset((float)(step * (double)DakotaKeepStepS + i * (double)DakotaKeepStepS / Samples - lag));
                var at = centre + new Vector3(o.x, 0f, o.y);
                var b = new Bounds(); b.SetMinMax(at - new Vector3(round, reach, round), at + new Vector3(round, up, round));
                if (i == 0) box = b; else box.Encapsulate(b);
            }
            box.Expand(2f * Pad);
            return box;
        }

        /// <summary>A drawn Dakota whose keep-clear box follows it: its loop, its loop's centre in the air, the box's sizes
        /// (<see cref="DakotaKeepBox"/>) and the clock step its box now covers.</summary>
        sealed class DakotaKeep
        {
            public readonly GameObject Go; public readonly DakotaLoop Loop; public readonly Vector3 Centre; public readonly float Reach, Up, Round;
            public long Step;
            public DakotaKeep(GameObject go, DakotaLoop loop, Vector3 centre, float reach, float up, float round)
            { Go = go; Loop = loop; Centre = centre; Reach = reach; Up = up; Round = round; }
        }
        readonly List<DakotaKeep> dakotaKeeps = new List<DakotaKeep>();

        /// <summary>Moves each drawn Dakota's keep-clear box to the clock step where its loop last placed it
        /// (DakotaLoop.PlacedAt); when one moves, <see cref="KeepClear"/> becomes a new list, so the labels re-place themselves
        /// (MapLabelsPresenter re-reads a list only when it is a new one). A held loop (edit mode, shots) never moves. Cheap: a
        /// comparison an aircraft a call, and a box and a list once a step.</summary>
        void FollowDakotas()
        {
            bool moved = false;
            for (int i = 0; i < dakotaKeeps.Count; i++)
            {
                var d = dakotaKeeps[i];
                if (d.Go == null || d.Loop == null || !keepOf.ContainsKey(d.Go)) continue;
                long step = DakotaKeepStep(d.Loop.PlacedAt);
                if (step == d.Step) continue;
                d.Step = step;
                keepOf[d.Go] = new[] { DakotaKeepBox(d.Centre, d.Loop.Lag, step, d.Reach, d.Up, d.Round) };
                moved = true;
            }
            if (moved) RebuildKeepAll();
        }

        /// <summary>The turning propellers on both engines (#2482: "turning propellers on both engines"; the model is one fused
        /// mesh, so it cannot turn its own): a disc as the Spitfire's (<see cref="Propeller"/>: the same blur and mesh, made once
        /// for the layer) at each nacelle's front (<see cref="DakotaEngineAcrossShare"/>, <see cref="DakotaPropBehindNoseShare"/>,
        /// <see cref="DakotaPropUpShare"/>, <see cref="DakotaPropRadiusShare"/>), a child of the aircraft's mesh, turned by
        /// DakotaLoop about the mesh's y, the flight axis. Each disc's renderer bounds are the square inside its circle, so its
        /// box stays inside the airframe's at every angle.</summary>
        Transform[] DakotaPropellers(GameObject go)
        {
            var airframe = go.GetComponentInChildren<MeshFilter>();
            var mesh = airframe.sharedMesh.bounds;
            float radius = DakotaPropRadiusShare * mesh.size.x;
            var discs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var disc = new GameObject(DakotaPropellerName);
                disc.transform.SetParent(airframe.transform, false);
                disc.transform.localPosition = new Vector3(mesh.center.x + (i == 0 ? -1f : 1f) * DakotaEngineAcrossShare * mesh.size.x,
                    mesh.min.y + DakotaPropBehindNoseShare * mesh.size.y, mesh.min.z + DakotaPropUpShare * mesh.size.z);
                disc.transform.localScale = Vector3.one * radius;
                disc.AddComponent<MeshFilter>().sharedMesh = propMesh != null ? propMesh : propMesh = Own(PropellerDisc());
                var r = disc.AddComponent<MeshRenderer>();
                r.sharedMaterial = Blur(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                r.localBounds = new Bounds(Vector3.zero, new Vector3(Mathf.Sqrt(2f), 2f * PropLayer, Mathf.Sqrt(2f)));
                discs[i] = disc.transform;
            }
            return discs;
        }

        /// <summary>The bomb run of a Dakota whose loop is centred at <paramref name="centre"/> (in the air) and whose half box is
        /// <paramref name="half"/>: every <see cref="DakotaBombIntervalS"/> of the loop whose release point (the ground under the
        /// aircraft) and impact point (the ground under it <see cref="DakotaBombFallS"/> later) are both on Tel Aviv's hex
        /// (<see cref="OnDakotaTarget"/>), a bomb from its belly. Its bombs, flashes and smoke are drawn under
        /// <paramref name="parent"/> (DakotaBombRun); the flash and puff textures and the bomb mesh are made once for the layer.</summary>
        DakotaBombRun DakotaBombs(Transform parent, Vector3 centre, Vector3 half, KitGround ground, int plane)
        {
            var targets = DakotaTargets(ground, content);
            var drops = new List<DakotaBombRun.Drop>();
            int n = Mathf.RoundToInt(DakotaLoopSeconds / DakotaBombIntervalS);
            var c = new Vector2(centre.x, centre.z);
            for (int j = 0; j < n; j++)
            {
                float at = j * DakotaBombIntervalS;
                Vector2 from = c + DakotaLoopOffset(at), to = c + DakotaLoopOffset(at + DakotaBombFallS);
                if (!OnDakotaTarget(ground, targets, from) || !OnDakotaTarget(ground, targets, to)) continue;
                drops.Add(new DakotaBombRun.Drop(at, new Vector3(from.x, centre.y - half.y, from.y), new Vector3(to.x, ground.Y(to.x, to.y), to.y)));
            }
            if (flashTexture == null) flashTexture = Own(UnitGunFire.FlashTexture());
            if (puffTexture == null) puffTexture = Own(Statecraft.HexTiles.HexUnderAttackLook.PuffTexture());
            if (bombMesh == null) bombMesh = Own(BombMesh());
            // The flipbooks are assets (not owned: Resources keeps them); without them the blast and smoke fall back to the
            // drawn flash and puff, one frame each.
            var blast = Resources.Load<Texture2D>(DakotaBlastTexture); var smoke = Resources.Load<Texture2D>(DakotaSmokeTexture);
            if (blast == null || smoke == null) Debug.LogWarning("SEA/AIR: the Dakotas' explosion or smoke flipbook is missing from Resources/Effects; drawn textures stand in.");
            var looks = new DakotaBombRun.Looks(flashTexture, blast != null ? blast : flashTexture, blast != null ? DakotaBlastGrid : 1,
                smoke != null ? smoke : puffTexture, smoke != null ? DakotaSmokeGrid : 1);
            return new DakotaBombRun(parent, drops, n, bombMesh, Blur(), looks, plane, DakotaKeys.Length);
        }

        Texture2D? flashTexture, puffTexture; Mesh? bombMesh;

        /// <summary>DESIGN VALUES: a bomb, a dark olive-grey spindle (two cones base to base, six sides), one unit long along y and
        /// a third of that across, coloured by its vertices for the propeller's blur material (<see cref="Blur"/>).</summary>
        static Mesh BombMesh()
        {
            const int Sides = 6; const float R = 1f / 6f;
            var v = new Vector3[Sides + 2]; var col = new Color32[v.Length]; var tris = new int[Sides * 6];
            v[0] = new Vector3(0f, 0.5f, 0f); v[1] = new Vector3(0f, -0.5f, 0f);
            for (int i = 0; i < Sides; i++) { float a = 2f * Mathf.PI * i / Sides; v[2 + i] = new Vector3(Mathf.Cos(a) * R, 0.1f, Mathf.Sin(a) * R); }
            for (int i = 0; i < v.Length; i++) col[i] = new Color(0.12f, 0.12f, 0.10f, 1f);
            for (int i = 0; i < Sides; i++)
            {
                int a = 2 + i, b = 2 + (i + 1) % Sides;
                tris[i * 6] = 0; tris[i * 6 + 1] = b; tris[i * 6 + 2] = a;
                tris[i * 6 + 3] = 1; tris[i * 6 + 4] = a; tris[i * 6 + 5] = b;
            }
            var mesh = new Mesh { name = "DakotaBomb", vertices = v, colors32 = col, triangles = tris };
            mesh.RecalculateBounds();
            return UnitPlinth.Upload(mesh);
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
