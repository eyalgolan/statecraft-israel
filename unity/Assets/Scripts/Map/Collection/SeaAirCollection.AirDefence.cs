#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tel Aviv's air defence against the two Egyptian Dakotas on the turns of the second bombing period, 8 to 17 July
    /// 1948 (<see cref="DakotaSecondFrom"/>; on the game's monthly turns, the 15 July 1948 turn). The owner asked: "we will need
    /// an animation of the 101 attacking the dakotas", chose "(a) Add the Dakotas to the 15 July 1948 turn, the second bombing
    /// period, with 101 Squadron's fighters attacking them over Tel Aviv. Both sides are historically present that turn.", and
    /// then answered "do 1+2" to two options: (1) 101 Squadron's Avia S-199s attack the Dakotas with hits but no 101 shoot-down,
    /// shown as an illustration of that turn's air defence; (2) one Dakota goes down to anti-aircraft fire, the sourced event of
    /// 17 July 1948.
    ///
    /// What is sourced, and what is not (research_notes/GDD phase 7/tel-aviv-bombing-july-1948.md, agent-read, a person's
    /// check pending): the second period itself, 8 to 17 July (he.wiki "הפצצת תל אביב במלחמת העצמאות", rev 43856594); Egyptian
    /// raids on Tel Aviv by Dakotas with a Spitfire escort after the Israeli B-17 raid of 15 July, "One Egyptian Dakota was
    /// lost" (en.wiki "1948 Arab–Israeli War", rev 1375001749, citing Morris 2008, pp. 294-295; the same article says elsewhere
    /// there were no more bomber raids after 3 June, so [C]); one Egyptian Dakota shot down by anti-aircraft fire over Tel Aviv
    /// on 17 July (JTA 19 July 1948, row R11 of research_notes/GDD phase 7/egyptian-air-bases-and-raids-1948-1872.md). No
    /// source read shows 101 Squadron intercepting the Dakotas over Tel Aviv in July: the fighters' attack is an illustration,
    /// so they hit but never bring a Dakota down. The only sourced 101-against-Dakota fight is Modi Alon's, 3 June 1948.
    ///
    /// The fighters are the Avia S-199, 101 Squadron's type from 20 May to mid-October 1948 (content/map/units_1948.yaml,
    /// squadron_101), the map's own code-built model (UnitModels.AviaS199, sourced in docs/units.md), at the unit tokens'
    /// aircraft scale. No model file is added or changed. Everything here is drawn by <see cref="TelAvivAirDefence"/> on the
    /// Dakotas' own loop clock, a pure function of it. Presentation only: no sim, content, save or hash change.</summary>
    public sealed partial class SeaAirCollection
    {
        /// <summary>The air defence's object key: it is drawn beside the Dakotas and goes with them.</summary>
        public const string AirDefenceKey = DakotaKey + "-air-defence";

        /// <summary>The two fighters' object names, under the air defence's object.</summary>
        public static readonly string[] FighterKeys = { "s199-1", "s199-2" };

        /// <summary>True on the dates the S-199s and the anti-aircraft fire show: the second bombing period (the Dakotas' second
        /// window, <see cref="DakotaSecondFrom"/>). Never in the first period: the 15 May turn shows the Dakotas alone.</summary>
        public static bool AirDefenceWanted(DateTime date) => date >= DakotaSecondFrom && date <= DakotaSecondTo;

        /// <summary>DESIGN VALUES for the fighters' choreography, in the lead Dakota's loop time (s): fighter i dives on the
        /// lead from <see cref="FighterAttackStartS"/>[i] for <see cref="FighterAttackS"/>, the first as the pair bombs Tel Aviv
        /// (on the loop's southern pass), the second on the pass back out to sea (the northern pass), both where the Dakotas fly
        /// level. The heights keep every fighter's box, at any bank and pitch, over the box of a Dakota under it, banked in its
        /// turn or not (sampled with the two models' sizes, with a margin of about 500 m). On the lead's track the fighter starts <see cref="FighterBehindS"/> of the loop behind it and
        /// <see cref="FighterTopM"/> above, closes, passes <see cref="FighterPassSideM"/> to the inside and
        /// <see cref="FighterPassUpM"/> above at the middle of the attack, and ends <see cref="FighterAheadS"/> ahead, climbing
        /// back to <see cref="FighterTopM"/> (it dives in, passes and pulls away). For the rest of the loop it falls back to its
        /// start, flying up to <see cref="FighterOutsideM"/> outside the Dakotas' track, slower than they are. Smoothsteps join the
        /// pieces, so the path and its speed are continuous, and the path closes every loop.</summary>
        public static readonly float[] FighterAttackStartS = { -6f, 54f };
        public const float FighterAttackS = 16f, FighterBehindS = 12.7f, FighterAheadS = 7.6f, FighterPassSideM = 2500f, FighterOutsideM = 3000f,
            FighterPassUpM = 3900f, FighterTopM = 5400f;

        /// <summary>DESIGN VALUES: the share of its attack over which a fighter fires (its gun's flash and tracer), and over which
        /// its rounds hit the lead (puffs of smoke on the Dakota, <see cref="FighterHits"/> of them). Hits, never a kill: no source
        /// shows 101 Squadron bringing down a Dakota over Tel Aviv in July.</summary>
        public const float FighterFireFrom = 0.28f, FighterFireTo = 0.5f, FighterHitFrom = 0.34f, FighterHitTo = 0.56f;
        public const int FighterHits = 4, FighterTracers = 3;

        /// <summary>DESIGN VALUES for the gun: a tracer dash <see cref="TracerLengthM"/> long and <see cref="TracerWidthM"/>
        /// wide runs from the muzzle to the Dakota <see cref="TracerRate"/> times a second; the muzzle flash, the gun's
        /// (UnitGunFire.Flash), <see cref="MuzzleFlashM"/> across, every <see cref="MuzzleFlashGapS"/>; a hit is a flash
        /// <see cref="HitFlashM"/> across and a puff of the smoke flipbook <see cref="HitPuffM"/> across, living
        /// <see cref="HitPuffS"/>, left behind where it was struck.</summary>
        public const float TracerLengthM = 1400f, TracerWidthM = 260f, TracerRate = 2.5f, MuzzleFlashM = 900f, MuzzleFlashGapS = 0.15f,
            HitFlashM = 900f, HitPuffM = 1300f, HitPuffS = 2.2f;

        /// <summary>DESIGN VALUES for the anti-aircraft fire (option 2, the sourced 17 July shoot-down): in the wing Dakota's own
        /// loop time, after its bombs, <see cref="FlakAtS"/> are the bursts, each fired from one of <see cref="FlakSites"/> gun
        /// sites on Tel Aviv's hex (a flash on the ground <see cref="FlakFlightS"/> before the burst), bursting near the wing:
        /// a flash and a dark puff, <see cref="FlakBurstM"/> across, living <see cref="FlakBurstS"/>. The last burst, at
        /// <see cref="WingHitS"/>, is on it.</summary>
        public static readonly float[] FlakAtS = { 2.5f, 4.5f, 6.5f, 8.5f, 10.5f, 12f };
        public static readonly Vector3[] FlakMissM =
        {
            new Vector3(-2600f, 700f, 1900f), new Vector3(2200f, -500f, -2400f), new Vector3(-1500f, 900f, -2000f),
            new Vector3(1800f, 400f, 2300f), new Vector3(-900f, -300f, 1500f), Vector3.zero,
        };
        public const int FlakSites = 3;
        public const float FlakFlightS = 1.2f, FlakBurstM = 1500f, FlakBurstS = 2.6f, FlakSiteM = 1100f, GunFlashM = 700f;

        /// <summary>DESIGN VALUES for the wing's fall: hit at <see cref="WingHitS"/> of its own loop, it trails smoke and goes down
        /// over <see cref="WingFallS"/>, turning left by <see cref="WingFallTurnDeg"/> (back toward the sea), its nose down at
        /// most <see cref="WingFallPitchDeg"/>, rolling to <see cref="WingFallBankDeg"/> and level again, its speed easing to
        /// <see cref="WingFallSpeedShare"/>; where it reaches the ground a blast of the bombs' flipbook goes up, and it is gone.
        /// Where it fell is not sourced: the path is a design value. The loop repeats every <see cref="DakotaLoopSeconds"/>, so
        /// one loop is one raid: a fresh wing joins the loop out over the sea at <see cref="WingRejoinS"/>, before the next
        /// pass, and the same raid plays again. One anti-aircraft kill a loop, as on 17 July; the lead is never brought down.</summary>
        public const float WingHitS = 12f, WingFallS = 18f, WingFallTurnDeg = 150f, WingFallPitchDeg = 25f, WingFallBankDeg = 35f, WingFallSpeedShare = 0.75f,
            WingRejoinS = 80f;

        /// <summary>DESIGN VALUE: the falling wing's centre ends this share of its span over the highest ground under its fall,
        /// its wings levelled again over the last 40 per cent of the fall, so that with its nose down at
        /// <see cref="WingFallPitchDeg"/> it reaches the ground (the aircraft is drawn 336 times its size, so a deeper fall would
        /// bury a wing in the hills first).</summary>
        public const float WingFallEndShare = 0.1f;

        /// <summary>DESIGN VALUES for the smoke the falling wing trails: a puff of the smoke flipbook every
        /// <see cref="TrailGapS"/>, <see cref="TrailPuffM"/> across, living <see cref="TrailPuffS"/>, at most
        /// <see cref="TrailPuffs"/> showing.</summary>
        public const float TrailGapS = 0.6f, TrailPuffM = 1500f, TrailPuffS = 6f;
        public static readonly int TrailPuffs = Mathf.CeilToInt(TrailPuffS / TrailGapS) + 1;

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>Fighter <paramref name="fighter"/>'s share of its attack at the lead's loop time <paramref name="seconds"/>:
        /// in [0, 1) during the attack, negative otherwise.</summary>
        public static float FighterAttackShare(int fighter, float seconds)
        {
            float phase = Mathf.Repeat(seconds - FighterAttackStartS[fighter], DakotaLoopSeconds);
            return phase < FighterAttackS ? phase / FighterAttackS : -1f;
        }

        /// <summary>Fighter <paramref name="fighter"/>'s drawn centre from the Dakotas' loop centre in the air (world x, height, z;
        /// m) at the lead's loop time <paramref name="seconds"/> (<see cref="FighterAttackStartS"/>): on the lead's track, a
        /// lag behind or ahead of it, to one side of it, above it. A pure function of the time, closed every loop.</summary>
        public static Vector3 FighterOffset(int fighter, float seconds)
        {
            float phase = Mathf.Repeat(seconds - FighterAttackStartS[fighter], DakotaLoopSeconds);
            float lag, side, up;
            if (phase < FighterAttackS)
            {
                float p = phase / FighterAttackS, c = Mathf.Cos(Mathf.PI * p);
                lag = FighterBehindS - (FighterBehindS + FighterAheadS) * Smooth(p);
                side = FighterPassSideM * Mathf.Sin(Mathf.PI * p);
                up = FighterPassUpM + (FighterTopM - FighterPassUpM) * c * c;
            }
            else
            {
                float q = (phase - FighterAttackS) / (DakotaLoopSeconds - FighterAttackS);
                lag = -FighterAheadS + (FighterBehindS + FighterAheadS) * Smooth(q);
                side = -FighterOutsideM * Mathf.Sin(Mathf.PI * q);
                up = FighterTopM;
            }
            float track = seconds - lag;
            var at = DakotaLoopOffset(track); var h = DakotaLoopHeading(track);
            var inside = new Vector2(-h.y, h.x);   // the loop turns left: its inside is to the left of the heading
            var xz = at + inside * side;
            return new Vector3(xz.x, up, xz.y);
        }

        /// <summary>The step (s) of the finite differences that give a fighter's velocity and turn.</summary>
        internal const float FighterStepS = 0.05f;

        /// <summary>Fighter <paramref name="fighter"/>'s velocity (world, m/s) at <paramref name="seconds"/>: a central difference
        /// of <see cref="FighterOffset"/>.</summary>
        public static Vector3 FighterVelocity(int fighter, float seconds) =>
            (FighterOffset(fighter, seconds + FighterStepS) - FighterOffset(fighter, seconds - FighterStepS)) / (2f * FighterStepS);

        /// <summary>Fighter <paramref name="fighter"/>'s bank (degrees, positive for its left wing down in a left turn, as
        /// DakotaLoop's): <see cref="BankFor"/> its path's curvature seen from above at the Dakotas' loop scale, as the Dakotas'
        /// own (<see cref="DakotaBankDegrees"/>), signed by the turn's side.</summary>
        public static float FighterBankDegrees(int fighter, float seconds)
        {
            var a = FighterOffset(fighter, seconds - FighterStepS); var b = FighterOffset(fighter, seconds); var c = FighterOffset(fighter, seconds + FighterStepS);
            var v = new Vector2(c.x - a.x, c.z - a.z) / (2f * FighterStepS);
            var acc = new Vector2(c.x - 2f * b.x + a.x, c.z - 2f * b.z + a.z) / (FighterStepS * FighterStepS);
            float speed = v.magnitude;
            if (speed < 1f) return 0f;
            float cross = v.x * acc.y - v.y * acc.x, curvature = cross / (speed * speed * speed);
            return Mathf.Sign(curvature) * BankFor(DakotaLoopScale * Mathf.Abs(curvature));
        }

        /// <summary>DESIGN VALUE: the steepest a fighter's nose points down in its dive or up in its pull-out (degrees).</summary>
        public const float FighterPitchMaxDeg = 20f;

        /// <summary>The falling wing's drawn centre from where it was hit (world x, z; m), its heading's angle (radians from east,
        /// anticlockwise from above) and its height lost (as a share of the whole drop), <paramref name="s"/> seconds after the
        /// hit, for a wing that was flying at <paramref name="speed"/> (m/s) on <paramref name="heading0"/> (radians) when hit:
        /// a left turn of <see cref="WingFallTurnDeg"/> at a speed easing to <see cref="WingFallSpeedShare"/> of it, the height
        /// lost growing with the square of the time. Its turn radius is its speed over its turn rate.</summary>
        public static Vector2 WingFall(float s, float speed, float heading0, out float heading, out float dropShare)
        {
            float t = Mathf.Clamp(s, 0f, WingFallS), w = WingFallTurnDeg * Mathf.Deg2Rad / WingFallS;
            // The distance flown with the speed easing linearly to WingFallSpeedShare, expressed as an angle turned: the turn
            // rate eases with the speed, so the radius stays the speed at the hit over the full turn rate.
            float k = 1f - WingFallSpeedShare, eased = t - 0.5f * k * t * t / WingFallS;
            heading = heading0 + w * eased;
            float r = speed / w;
            dropShare = (t / WingFallS) * (t / WingFallS);
            return new Vector2(r * (Mathf.Sin(heading) - Mathf.Sin(heading0)), -r * (Mathf.Cos(heading) - Mathf.Cos(heading0)));
        }

        /// <summary>The wing's state at its own loop time <paramref name="u"/>: 0 on the loop, 1 falling (its seconds since the
        /// hit in <paramref name="since"/>), 2 gone (shot down, until the fresh wing joins at <see cref="WingRejoinS"/>).</summary>
        public static int WingState(float u, out float since)
        {
            since = Mathf.Repeat(u - WingHitS, DakotaLoopSeconds);
            if (since < WingFallS) return 1;
            return since < WingRejoinS - WingHitS ? 2 : 0;
        }

        /// <summary>The ground points (world x, z) of the anti-aircraft guns: <see cref="FlakSites"/> spots
        /// <see cref="FlakSiteM"/> from the middle of Tel Aviv's hex (its first hex), inside its edge.</summary>
        internal static Vector2[] FlakSitesAt(Vector2 hex)
        {
            var sites = new Vector2[FlakSites];
            for (int i = 0; i < FlakSites; i++)
            {
                float a = (90f + 120f * i) * Mathf.Deg2Rad;
                sites[i] = hex + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * FlakSiteM;
            }
            return sites;
        }

        /// <summary>The air defence's director, or null when it is not drawn (a test aid).</summary>
        public TelAvivAirDefence? AirDefence { get; private set; }

        /// <summary>On the second bombing period's turns, once both Dakotas are drawn: the air defence beside them
        /// (<see cref="TelAvivAirDefence"/>), with the two S-199s built from the map's own model (UnitModels.AviaS199, its
        /// propeller UnitModels.S199Propeller) at the unit tokens' aircraft scale, each with its propeller, its shadow and
        /// Israel's flag over its tail, as the Dakotas carry Egypt's. It is one of the Dakotas' originals, so it goes when they
        /// go. The labels keep clear of each fighter where it is now, a <see cref="DakotaKeepStepS"/> step at a time
        /// (<see cref="FollowAirDefence"/>).</summary>
        void FlyAirDefence(KitGround ground)
        {
            AirDefence = null; airDefenceKeeps.Clear();
            if (!AirDefenceWanted(date) || map == null || content == null) return;
            var lead = transform.Find(DakotaKeys[0]); var wing = transform.Find(DakotaKeys[1]);
            var leadLoop = lead != null ? lead.GetComponent<DakotaLoop>() : null; var wingLoop = wing != null ? wing.GetComponent<DakotaLoop>() : null;
            if (leadLoop == null || wingLoop == null || DakotaCentre() is not Vector2 c) return;
            var targets = DakotaTargets(ground, content);
            if (targets.Count == 0) return;
            float size = SizeOf(Dakota);
            var centre = new Vector3(c.x, UnitShowcase.LoopFloor(map, new Vector3(c.x, 0f, c.y), DakotaLoopHalfLengthM + size) + UnitShowcase.AltitudeM, c.y);

            var go = new GameObject(AirDefenceKey);
            go.transform.SetParent(transform, false);
            originals[Dakota].Add(go);
            var owned = go.AddComponent<MapRuntimeAssets>();
            var template = Resources.Load<Material>(UnitMaterials.MiniatureResource);
            if (template == null) { Debug.LogWarning("SEA/AIR: the unit materials are missing (run UnitMaterialAssets.Create); the S-199s are not drawn."); return; }
            if (fighterPaint == null)
            {
                fighterPaint = Own(new Material(template) { name = "UnitMiniature (palette, S-199)" });
                fighterPaint.SetTexture("_BaseMap", Own(UnitPalette.MakeTexture(readable: false)));
            }
            if (s199Mesh == null) s199Mesh = UnitPlinth.Upload(Own(UnitModels.AviaS199("S199-1948-air-defence")));
            if (s199PropMesh == null) s199PropMesh = UnitPlinth.Upload(Own(UnitModels.S199Propeller("S199-propeller-air-defence")));

            var fighters = new TelAvivAirDefence.Fighter[FighterKeys.Length];
            for (int i = 0; i < FighterKeys.Length; i++)
            {
                var plane = UnitShowcase.Piece(go.transform, FighterKeys[i], s199Mesh, fighterPaint, centre, Quaternion.identity, UnitShowcase.AircraftScale);
                plane.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                var prop = UnitShowcase.Piece(plane.transform, "Propeller", s199PropMesh, fighterPaint, Vector3.zero, Quaternion.identity, 1f);
                prop.transform.localPosition = UnitModels.S199PropellerHub;
                var b = Bounds(plane);   // nose north (+z) at identity: its length along z, its span along x
                var half = 0.5f * new Vector3(b.size.z, b.size.y, b.size.x);   // half its length, height and span, as DakotaLoop's
                var marks = new GameObject(FighterKeys[i] + DakotaMarksSuffix);
                marks.transform.SetParent(go.transform, false);
                var flag = FighterFlag(marks.transform);
                fighters[i] = new TelAvivAirDefence.Fighter(plane.transform, prop.transform, Shadow(marks.transform), flag, half,
                    0.5f * ShadowShare * new Vector2(b.size.x, b.size.z));
            }

            var looks = AirDefenceLooks();
            var defence = go.AddComponent<TelAvivAirDefence>();
            var wingShadow = transform.Find(DakotaKeys[1] + DakotaMarksSuffix)?.Find(DakotaShadowName);
            // The Dakota's length over its span, off its mesh (its -y the nose, x across), for where the hits and the trail sit.
            var dm = wing!.GetComponentInChildren<MeshFilter>();
            float lengthShare = dm != null && dm.sharedMesh != null && dm.sharedMesh.bounds.size.x > 1e-3f ? dm.sharedMesh.bounds.size.y / dm.sharedMesh.bounds.size.x : 0.667f;
            defence.Configure(centre, fighters, leadLoop, wingLoop, wingShadow, FlakSitesAt(targets[0]), ground, map.Terrain, looks,
                !Application.isPlaying || VisualShots.Requested(), lengthShare);
            AirDefence = defence;
            var cam = Camera.main;
            if (cam != null) foreach (var f in fighters) if (f.Flag != null) UnitFlagFacing.Face(f.Flag, cam.transform.position);

            // The labels keep clear of each fighter where it is now, as of the Dakotas (owner, 4 November 2026, on #2490: "Shrink
            // the keep-clear area (Recommended)").
            for (int i = 0; i < fighters.Length; i++)
            {
                var f = fighters[i];
                float reach = FighterReach(f.Plane, f.Prop) + 1f, up = reach;
                if (f.Flag != null)
                {
                    var fb = f.Flag.GetComponent<MeshFilter>().sharedMesh.bounds; float k = f.Flag.lossyScale.y;
                    up = Mathf.Max(up, f.Half.magnitude + DakotaLoop.FlagClearM + 2f * fb.extents.y * k + 1f);
                }
                var keep = new FighterKeep(f.Plane.gameObject, defence, i, centre, reach, up);
                keep.Step = DakotaKeepStep(defence.PlacedAt);
                airDefenceKeeps.Add(keep);
                Keep(f.Plane.gameObject, FighterKeepBox(i, centre, keep.Step, reach, up));
            }
        }

        Material? fighterPaint; Mesh? s199Mesh, s199PropMesh;

        /// <summary>The farthest any part of a fighter can be from its pivot (world m), at any attitude and any turn of its
        /// propeller: the airframe mesh's farthest bounds corner from its origin (the centre of gravity, UnitModels.AviaS199),
        /// and the propeller hub's distance plus the propeller mesh's farthest corner from its own origin (it turns about it),
        /// times the aircraft's scale. The fighter turns about its pivot, not about its bounds' centre, which lies about a metre
        /// of the model aft of it (the tail is longer than the nose), so half the bounds' diagonal fell short at some attitudes.</summary>
        internal static float FighterReach(Transform plane, Transform prop)
        {
            static float Far(Bounds b) => new Vector3(Mathf.Abs(b.center.x) + b.extents.x, Mathf.Abs(b.center.y) + b.extents.y, Mathf.Abs(b.center.z) + b.extents.z).magnitude;
            static float Largest(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));
            float airframe = Far(plane.GetComponent<MeshFilter>().sharedMesh.bounds) * Largest(plane.lossyScale);
            float blades = Far(prop.GetComponent<MeshFilter>().sharedMesh.bounds) * Largest(prop.lossyScale);
            float hub = Vector3.Scale(prop.localPosition, plane.lossyScale).magnitude;
            return Mathf.Max(airframe, hub + blades);
        }

        /// <summary>The box (world) the names keep clear of for fighter <paramref name="fighter"/> through the clock step
        /// <paramref name="step"/> (<see cref="DakotaKeepStepS"/>): its path in that step, sampled, with <paramref name="reach"/>
        /// round it and <paramref name="up"/> above it (its flag), as <see cref="DakotaKeepBox"/>.</summary>
        internal static Bounds FighterKeepBox(int fighter, Vector3 centre, long step, float reach, float up)
        {
            const int Samples = 8; const float Pad = 10f;
            var box = new Bounds();
            for (int i = 0; i <= Samples; i++)
            {
                var at = centre + FighterOffset(fighter, (float)(step * (double)DakotaKeepStepS + i * (double)DakotaKeepStepS / Samples));
                var b = new Bounds(); b.SetMinMax(at - new Vector3(reach, reach, reach), at + new Vector3(reach, up, reach));
                if (i == 0) box = b; else box.Encapsulate(b);
            }
            box.Expand(2f * Pad);
            return box;
        }

        sealed class FighterKeep
        {
            public readonly GameObject Go; public readonly TelAvivAirDefence Defence; public readonly int Fighter; public readonly Vector3 Centre; public readonly float Reach, Up;
            public long Step;
            public FighterKeep(GameObject go, TelAvivAirDefence defence, int fighter, Vector3 centre, float reach, float up)
            { Go = go; Defence = defence; Fighter = fighter; Centre = centre; Reach = reach; Up = up; }
        }
        readonly List<FighterKeep> airDefenceKeeps = new List<FighterKeep>();

        /// <summary>Moves each fighter's keep-clear box to the clock step where the air defence last placed it, as
        /// FollowDakotas does the Dakotas'. Cheap: a comparison a fighter a call, and a box and a list once a step.</summary>
        void FollowAirDefence()
        {
            bool moved = false;
            for (int i = 0; i < airDefenceKeeps.Count; i++)
            {
                var d = airDefenceKeeps[i];
                if (d.Go == null || d.Defence == null || !keepOf.ContainsKey(d.Go)) continue;
                long step = DakotaKeepStep(d.Defence.PlacedAt);
                if (step == d.Step) continue;
                d.Step = step;
                keepOf[d.Go] = new[] { FighterKeepBox(d.Fighter, d.Centre, step, d.Reach, d.Up) };
                moved = true;
            }
            if (moved) RebuildKeepAll();
        }

        /// <summary>The textures the air defence draws with: the gun's flash (made once for the layer, as the bombs'), and the
        /// bombs' two Unity Labs flipbooks (the blast and the smoke), or the drawn flash and puff when they are missing.</summary>
        TelAvivAirDefence.Looks AirDefenceLooks()
        {
            if (flashTexture == null) flashTexture = Own(UnitGunFire.FlashTexture());
            if (puffTexture == null) puffTexture = Own(Statecraft.HexTiles.HexUnderAttackLook.PuffTexture());
            var blast = Resources.Load<Texture2D>(DakotaBlastTexture); var smoke = Resources.Load<Texture2D>(DakotaSmokeTexture);
            return new TelAvivAirDefence.Looks(flashTexture, blast != null ? blast : flashTexture, blast != null ? DakotaBlastGrid : 1,
                smoke != null ? smoke : puffTexture, smoke != null ? DakotaSmokeGrid : 1);
        }

        /// <summary>Israel's flag over a fighter's tail, as Egypt's floats over each Dakota's (<see cref="DakotaFlag"/>; owner on
        /// #2260, "Smaller flag, no pole"): the cloth alone (UnitFlags.BuildCloth), the flag the map's Israeli tokens fly
        /// (UnitFlags, UnitData.Israel), at <see cref="DakotaFlagScale"/> of the tokens' size, turned to the camera. Null, with a
        /// warning, when the units' materials are missing.</summary>
        static Transform? FighterFlag(Transform parent)
        {
            var ring = Resources.Load<Material>(UnitMaterials.RingResource);
            if (ring == null)
            {
                Debug.LogWarning("SEA/AIR: the unit materials are missing (run UnitMaterialAssets.Create); the S-199 flies without its flag.");
                return null;
            }
            var go = new GameObject(DakotaFlagName);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * (DakotaFlagScale * UnitBadge.FigureHeightM * UnitShowcase.FigureScale);
            var owned = go.AddComponent<MapRuntimeAssets>();
            go.AddComponent<MeshFilter>().sharedMesh = UnitPlinth.Upload(owned.Own(UnitFlags.BuildCloth(UnitData.Israel)!));
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = UnitFlags.ClothMaterials(UnitData.Israel, ring, m => owned.Own(m));
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<UnitFlagFacing>();
            return go.transform;
        }
    }
}
