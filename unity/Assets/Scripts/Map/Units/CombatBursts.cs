#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Map.Units
{
    /// <summary>What an attack hits (owner, 4 November 2026: "Hit on target hex"), from an engagement's structured line
    /// (UnitAttackPresenter.HitOf): the hex it hit, whether guns fired (a shell bursts there) and whether other units did
    /// (small-arms fire kicks up dust), and the defenders by sim id, whose tokens the hit lands on.</summary>
    public readonly struct CombatHit
    {
        public readonly int Q, R;
        public readonly bool Guns, SmallArms;
        /// <summary>The engagement's aggregate defender loss, or zero when the structured value is absent or malformed.</summary>
        public readonly int DefenderLoss;
        public readonly IReadOnlyList<int> Defenders;
        public CombatHit(int q, int r, bool guns, bool smallArms, IReadOnlyList<int> defenders)
            : this(q, r, guns, smallArms, 0, defenders) { }
        public CombatHit(int q, int r, bool guns, bool smallArms, int defenderLoss, IReadOnlyList<int> defenders)
        { Q = q; R = r; Guns = guns; SmallArms = smallArms; DefenderLoss = defenderLoss; Defenders = defenders; }
    }

    /// <summary>The combat effects on the map's ground (owner, 4 November 2026: "Hit on target hex" and "Dust when a unit
    /// dies"): a shell's burst where the guns' fire lands, a few puffs of dust where small arms hit, and a cloud of dust where
    /// a destroyed unit's token stood, and a lighter trail behind a moving token. All are drawn as #2490's Dakota bombs are:
    /// Unity Labs' CC0 flipbooks (Flipbook) on unlit camera-facing quads, from three small pools made once
    /// (<see cref="FlipbookBursts"/>), each burst a pure function of the
    /// clock, so sampling allocates nothing. Presentation only: it reads no state and writes none. It lives under the unit
    /// layer, so the units' toggle hides it (and drops what plays), and it goes with the layer. It runs only while a burst
    /// plays (in play: an edit-mode build, such as the promo captures, never advances it).</summary>
    [ExecuteAlways]
    public sealed class CombatBursts : MonoBehaviour
    {
        public const string Name = "Combat effects";

        /// <summary>DESIGN VALUES for the timing, from the attack's start (UnitTokenMotion.PlayAttack, the gun's flash at 0,
        /// UnitGunFire): the shell bursts on the target <see cref="ShellFlightS"/> after the flash; the first puff of the
        /// small-arms volley rises <see cref="VolleyS"/> after the figures' fire take starts, then <see cref="VolleyPuffs"/>
        /// in all, <see cref="VolleyGapS"/> apart. A destroyed unit's dust rises with the hit that destroyed it
        /// (<see cref="ImpactS"/>), in <see cref="DeathPuffs"/> puffs <see cref="DeathGapS"/> apart.</summary>
        public const float ShellFlightS = 0.3f, VolleyS = 0.12f, VolleyGapS = 0.22f, DeathGapS = 0.15f;
        public const int VolleyPuffs = 3, DeathPuffs = 3;

        /// <summary>When <paramref name="h"/>'s first hit shows on its target, from the fire take's start: the small-arms
        /// volley's first puff (<see cref="VolleyS"/>) when any small arms fired, else the shell's burst
        /// (<see cref="ShellFlightS"/>); 0 for neither. The defenders' shake and flash and the dust of the units it destroyed
        /// start then, so they land with it (e21's capture of #2614, 5 November 2026: they came with the shot, before it).</summary>
        public static float ImpactS(CombatHit h) => h.SmallArms ? VolleyS : h.Guns ? ShellFlightS : 0f;

        /// <summary>DESIGN VALUES for the sizes (m across), beside a 4.4 km hex and the Dakotas' 2.2 km bomb blast
        /// (SeaAirCollection.DakotaBlastM): a field gun's shell is smaller than an aerial bomb; small arms raise small puffs;
        /// a unit's end raises a cloud about as wide as its token's base. Each puff stands <see cref="SpreadShare"/> of its
        /// size from the spot, each its own way, so the puffs read as several.</summary>
        public const float BlastM = 1400f, HitDustM = 800f, DeathDustM = 1500f, MoveDustM = 520f, SpreadShare = 0.45f;

        /// <summary>DESIGN VALUES: the pools' sizes, as many as can show at once with a few attacks in one turn (an older burst
        /// gives up its slot to a newer one).</summary>
        public const int BlastSlots = 4, DustSlots = 12, MoveDustSlots = 8;

        /// <summary>DESIGN VALUES: the shell's burst plays the whole explosion flipbook (fire, then black smoke) over 1.8 s,
        /// opening at 0.45 of its size and swelling, as the Dakotas' blast does (BlastTint, DakotaBlastOpenShare and
        /// DakotaBlastFadeShare). The dust is the smoke flipbook in a dusty brown (its grey averages 0.44 where opaque, so
        /// the tint is about twice the shade drawn), puffing out from a third of its size, rising and thinning paler.</summary>
        public static readonly FlipbookBursts.Style BlastStyle = new FlipbookBursts.Style(1.8f, 0, Flipbook.ExplosionGrid * Flipbook.ExplosionGrid,
            0.45f, 0.12f, 0.1f, 0.35f, new Color(1f, 0.95f, 0.88f, 1f), new Color(1f, 0.95f, 0.88f, 1f), 0.4f);
        public static readonly FlipbookBursts.Style DustStyle = new FlipbookBursts.Style(1.6f, 0, 24,
            0.35f, 0.5f, 0.35f, 0.6f, new Color(1.25f, 1.1f, 0.88f, 0.85f), new Color(1.6f, 1.5f, 1.35f, 0.6f), 0.4f);
        public static readonly FlipbookBursts.Style MoveDustStyle = new FlipbookBursts.Style(0.7f, 0, 18,
            0.25f, 0.3f, 0.12f, 0.55f, new Color(1.18f, 1.05f, 0.84f, 0.48f), new Color(1.45f, 1.35f, 1.18f, 0f), 0.3f);

        /// <summary>Each puff's direction from the spot (x east, z north) and its turn on the screen (degrees, within 25 either
        /// way so the flipbook's lit side stays up).</summary>
        static readonly Vector2[] PuffSide = { new Vector2(-0.8f, 0.2f), new Vector2(0.7f, -0.3f), new Vector2(0.1f, 0.9f) };
        static readonly float[] PuffSpin = { -18f, 12f, 24f };

        /// <summary>The clock (s). Test seam (internal; the tests see it through InternalsVisibleTo): an EditMode test has no
        /// frame clock.</summary>
        internal static Func<double> Clock = () => Time.timeAsDouble;

        FlipbookBursts? blasts, dust, moveDust;
        readonly List<Object> owned = new List<Object>();

        /// <summary>The pools (tests): the shells' bursts, combat dust and movement dust.</summary>
        internal FlipbookBursts? Blasts => blasts;
        internal FlipbookBursts? Dust => dust;
        internal FlipbookBursts? MoveDust => moveDust;

        /// <summary>Makes the effects under <paramref name="parent"/>, hidden until a burst. Null when the flipbooks are
        /// missing from Resources (a warning says so): nothing plays then.</summary>
        public static CombatBursts? Create(Transform parent)
        {
            var blast = Flipbook.Load(Flipbook.ExplosionResource); var cloud = Flipbook.Load(Flipbook.SmokeResource);
            if (blast == null || cloud == null)
            {
                Debug.LogWarning("UNITS: the explosion or smoke flipbook is missing from Resources/Effects; attacks and losses play no burst.");
                return null;
            }
            var go = new GameObject(Name);
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<CombatBursts>();
            c.blasts = new FlipbookBursts(go.transform, "ShellBurst", BlastSlots, blast, Flipbook.ExplosionGrid, HexMaterials.AttackBorder, BlastStyle);
            c.dust = new FlipbookBursts(go.transform, "Dust", DustSlots, cloud, Flipbook.SmokeGrid, HexMaterials.AttackBorder, DustStyle);
            c.moveDust = new FlipbookBursts(go.transform, "MoveDust", MoveDustSlots, cloud, Flipbook.SmokeGrid, HexMaterials.AttackBorder, MoveDustStyle);
            c.owned.AddRange(c.blasts.Owned); c.owned.AddRange(c.dust.Owned); c.owned.AddRange(c.moveDust.Owned);
            var cam = Camera.main;
            if (cam != null) c.Face(cam.transform.rotation);
            c.enabled = false;   // runs only while a burst plays
            // Keep the already-created callback registered for this short-lived map child. Re-subscribing on every first
            // emission would allocate on that frame; OnDestroy removes it, and Face returns immediately while hidden.
            RenderPipelineManager.beginCameraRendering += c.Face;
            return c;
        }

        /// <summary>Plays an attack's hit on the spot <paramref name="at"/> (world, on the ground), from now: a shell's burst
        /// when <paramref name="guns"/> fired, the small-arms volley's dust when <paramref name="smallArms"/> did. False when
        /// neither, or the effects are hidden.</summary>
        public bool Hit(Vector3 at, bool guns, bool smallArms)
        {
            if (!gameObject.activeInHierarchy || blasts == null || dust == null || !(guns || smallArms)) return false;
            double now = Clock();
            if (guns) blasts.Start(at, BlastM, now + ShellFlightS);
            if (smallArms) Puffs(at, HitDustM, now + VolleyS, VolleyPuffs, VolleyGapS);
            Run(now);
            return true;
        }

        /// <summary>Raises the dust of a destroyed unit where its token stood, <paramref name="at"/> (world, on the ground),
        /// <paramref name="delayS"/> from now (the hit's <see cref="ImpactS"/>). False when the effects are hidden.</summary>
        public bool Destroyed(Vector3 at, float delayS = 0f)
        {
            if (!gameObject.activeInHierarchy || dust == null) return false;
            double now = Clock();
            Puffs(at, DeathDustM, now + Mathf.Max(0f, delayS), DeathPuffs, DeathGapS);
            Run(now);
            return true;
        }

        /// <summary>Raises one light ground-dust puff behind a moving token. False while the unit layer is hidden.</summary>
        public bool March(Vector3 at)
        {
            if (!gameObject.activeInHierarchy || moveDust == null) return false;
            double now = Clock();
            moveDust.Start(at, MoveDustM, now);
            Run(now);
            return true;
        }

        void Puffs(Vector3 at, float size, double start, int count, float gap)
        {
            for (int i = 0; i < count; i++)
            {
                var side = PuffSide[i % PuffSide.Length] * (SpreadShare * size * (i == 0 ? 0.4f : 1f));
                dust!.Start(at + new Vector3(side.x, 0f, side.y), size, start + i * gap, PuffSpin[i % PuffSpin.Length]);
            }
        }

        void Run(double now) { enabled = true; Sample(now); }

        /// <summary>Every burst as it is at clock time <paramref name="now"/>; the component stops when none is left. No
        /// allocation.</summary>
        internal void Sample(double now)
        {
            if (blasts == null || dust == null || moveDust == null) return;
            blasts.Sample(now); dust.Sample(now); moveDust.Sample(now);
            if (blasts.Busy == 0 && dust.Busy == 0 && moveDust.Busy == 0) enabled = false;
        }

        void Update() { if (Application.isPlaying) Sample(Clock()); }

        /// <summary>Hidden (the units' toggle) or stopped: whatever played is dropped, as a gun drops its shot.</summary>
        void OnDisable()
        {
            blasts?.Clear(); dust?.Clear(); moveDust?.Clear();
        }

        /// <summary>Turns the bursts to the camera about to render, as UnitGunFire.Face does the gun's.</summary>
        void Face(ScriptableRenderContext _, Camera cam)
        {
            if (!enabled || !gameObject.activeInHierarchy || cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            Face(cam.transform.rotation);
        }

        void Face(Quaternion view) { blasts?.Face(view); dust?.Face(view); moveDust?.Face(view); }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= Face;
            foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
            owned.Clear();
        }
    }
}
