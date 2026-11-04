#nullable enable
using System;
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Flies Tal Heuberger's Egyptian Spitfire round its loop (SeaAirCollection.LoopOffset, owner, 2 November 2026:
    /// "Fly a slow loop"), nose along the path, banked into the turns (SeaAirCollection.LoopBankDegrees), its propeller
    /// turning, its shadow on the ground and its side's flag over its tail (owner's report, 2 November 2026: "the gyptian plane's
    /// propelor isn't moving and it doesnt look fliying, also it doesnt show its an egyptian unit"). Presentation only: it
    /// reads the map's animation clock (Time.time, as the steam train and the showcase flight do), sets a few transforms per
    /// frame and allocates nothing. It lives on the aircraft's own object, so it stops when the layer drops the aircraft (its
    /// dates end, or the map is torn down) and does not run while the layer is inactive.
    /// The model is one fused mesh, so the turning propeller is a separate translucent disc at its nose (<see cref="Attach"/>,
    /// SeaAirCollection.Fly). Editor renders and screenshots (`?shot` / `-shot`) hold the loop's start, propeller included, so
    /// they repeat exactly, as the train does (RouteLayer.ModeFor).</summary>
    public sealed class SpitfireLoop : MonoBehaviour
    {
        /// <summary>DESIGN VALUE: the propeller's turn (degrees a second), as ShowcaseFlight turns the unit tokens' propellers:
        /// about 4 turns a second, so its three blurred blades move a sixth of a turn or less between two frames at 30 fps
        /// and never seem to turn backwards.</summary>
        public const float PropDegreesPerSecond = 1500f;

        /// <summary>DESIGN VALUE: the shadow's lift (m) over the highest of the five ground points sampled under it, so the flat
        /// disc lies over the dunes rather than in them.</summary>
        public const float ShadowLiftM = 30f;

        /// <summary>The direction the map's sun shines (MapLighting.Apply), pointing down: the shadow's ray when the scene has
        /// no directional sun, or one under 10 degrees.</summary>
        public static readonly Vector3 SunRay = Quaternion.Euler(MapLighting.SunElevation, MapLighting.SunAzimuth - 180f, 0f) * Vector3.forward;

        /// <summary>The ray the scene's sun casts now (RenderSettings.sun: MapLighting's, or the opt-in hex map's dusk or night
        /// light, HexTimeOfDay), so the shadow lies with the real ones; <see cref="SunRay"/> without one, or with one under
        /// 10 degrees, whose shadow would run off the board.</summary>
        static Vector3 SunNow()
        {
            var sun = RenderSettings.sun;
            if (sun != null && sun.type == LightType.Directional)
            {
                var ray = sun.transform.forward;
                if (ray.y < -0.17f) return ray;
            }
            return SunRay;
        }

        Vector3 centre, offset; Quaternion level; bool still;
        Transform? prop, shadow, flag; Terrain? ground; Vector2 shadowHalf; Vector3 half; float flagHalf, seaY;

        /// <summary>DESIGN VALUE: the gap (m) between the aircraft's box top and the flag's lower edge.</summary>
        public const float FlagClearM = 150f;

        /// <summary>How far the top of the aircraft's box stands over its centre at <paramref name="bankDegrees"/>: its half box
        /// <paramref name="half"/> (half its length, height and span) rolled about its length.</summary>
        public static float TopOver(Vector3 half, float bankDegrees)
        {
            float b = bankDegrees * Mathf.Deg2Rad;
            return half.z * Mathf.Abs(Mathf.Sin(b)) + half.y * Mathf.Cos(b);
        }

        /// <summary>The animation clock, Time.time. Test seam (internal; the tests see it through InternalsVisibleTo): an
        /// EditMode test has no frame clock.</summary>
        internal static Func<float> Clock = () => Time.time;

        /// <summary>True when the aircraft holds the loop's start (edit mode, screenshots).</summary>
        public bool Still => still;

        /// <summary>The turning propeller disc (a child of the aircraft's mesh), the shadow and the flag (each in world space,
        /// beside the aircraft), the terrain the shadow lies on, the shadow's half size across and along the heading (m), and
        /// the aircraft's half box at level (half its length, height and span, m), and the sea's surface (world y: the shadow
        /// never lies under it). Before <see cref="Configure"/>, which places them all.</summary>
        internal void Attach(Transform? prop, Transform? shadow, Transform? flag, Terrain? ground, Vector2 shadowHalf, Vector3 airframeHalf = default,
            float seaY = float.NegativeInfinity)
        {
            this.prop = prop; this.shadow = shadow; this.flag = flag; this.ground = ground; this.shadowHalf = shadowHalf; half = airframeHalf; this.seaY = seaY;
            flagHalf = flag != null ? flag.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y : 0f;
        }

        /// <summary><paramref name="centre"/>: the loop's centre in the air. <paramref name="level"/>: the model's rotation
        /// with its nose east. <paramref name="offset"/>: its drawn centre from its pivot at that rotation.</summary>
        internal void Configure(Vector3 centre, Quaternion level, Vector3 offset, bool still)
        {
            this.centre = centre; this.level = level; this.offset = offset; this.still = still;
            // Where the clock puts it now: a component added mid-frame gets no Update that frame (review k2 C1).
            Place(still ? 0f : Clock());
        }

        void Update()
        {
            if (!still) Place(Clock());
        }

        /// <summary>Puts the aircraft where it is <paramref name="seconds"/> into its loop: its drawn centre on the path, its
        /// nose along the path's tangent, rolled into the turn about that tangent; the propeller at its angle then; the flag
        /// over its tail; the shadow where the sun's ray through the drawn centre meets the ground.</summary>
        public void Place(float seconds)
        {
            var at = SeaAirCollection.LoopOffset(seconds); var heading = SeaAirCollection.LoopHeading(seconds);
            var forward = new Vector3(heading.x, 0f, heading.y);
            // A turn about the vertical from east (+x) to the heading; Unity's yaw turns +x toward -z.
            var yaw = Quaternion.Euler(0f, -Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg, 0f);
            // The loop turns left (anticlockwise seen from above): a positive turn about the heading lowers the left wing.
            float bank = SeaAirCollection.LoopBankDegrees(seconds);
            var turn = Quaternion.AngleAxis(bank, forward) * yaw;
            transform.localRotation = turn * level;
            var drawn = centre + new Vector3(at.x, 0f, at.y);
            transform.position = drawn - turn * offset;
            // The angle in double: a float product with the clock loses whole degrees after hours (review of #2260).
            if (prop != null) prop.localRotation = Quaternion.AngleAxis(-(float)((double)seconds * PropDegreesPerSecond % 360.0), Vector3.up);
            // The flag over the tail, its lower edge FlagClearM over the top of the aircraft's box at this bank: clear of the
            // airframe at every bank and heading.
            if (flag != null) flag.position = drawn - forward * half.x + Vector3.up * (TopOver(half, bank) + FlagClearM + flagHalf * flag.lossyScale.y);
            if (shadow != null && ground != null) LayShadow(shadow, ground, seaY, shadowHalf, drawn, forward, bank);
        }

        /// <summary>Lays <paramref name="shadow"/> (half <paramref name="shadowHalf"/> across and along the heading) for an
        /// aircraft drawn at <paramref name="drawn"/>, heading <paramref name="forward"/>, banked <paramref name="bank"/> degrees,
        /// on <paramref name="ground"/> or the sea's surface <paramref name="seaY"/>: the scene sun's ray from the aircraft to the
        /// ground (by default in two steps, the ground's height where the ray lands setting how far it travels); the disc flat, turned
        /// with the heading, narrowed across by the bank, lifted over the highest ground under its middle and its four ends. The
        /// Spitfire's, and the Dakotas' (DakotaLoop, #2482).
        ///
        /// The two steps are a fixed-point guess: each takes the ground's height where the last ray landed. Their miss is the
        /// height left between the last two guesses times the ray's run per metre of fall (cot of the sun's elevation, 1.73 at
        /// MapLighting's 30 degrees), and the guess only closes in where the ground under the ray rises more slowly than the ray
        /// falls (slope x cot(elevation) under 1). That holds on the flat coast by el-Arish where the Spitfire flies, so its
        /// shadow keeps the two steps (the default). The Dakotas over Tel Aviv cast theirs about 8 km east-south-east, up the
        /// rising ground toward the hills, where two steps left it about 200 m off the ray (gate H4 on #2490). With
        /// <paramref name="converge"/> the ray is cut instead by halving: between the aircraft (above the ground) and the point
        /// where the ray falls to the lowest surface there can be (the sea's, or the terrain's base), so the ground is crossed
        /// between them whatever its slope or the sun's height; a march in <see cref="ShadowMarchM"/> steps finds the first
        /// crossing, and each halving halves that step, to under
        /// <see cref="ShadowConvergeM"/> along the ray, at most <see cref="ShadowConvergeSteps"/> halvings.</summary>
        internal static void LayShadow(Transform shadow, Terrain ground, float seaY, Vector2 shadowHalf, Vector3 drawn, Vector3 forward, float bank,
            bool converge = false)
        {
            var ray = SunNow();
            var hit = drawn;
            float floor;
            if (converge)
            {
                // At s = 0 the ray is at the aircraft, over the ground; at hi it is at the lowest surface there can be, at or
                // under the ground (Surface is never below the sea's surface or the terrain's base).
                float lo = 0f, hi = (drawn.y - Mathf.Max(seaY, ground.transform.position.y)) / -ray.y;
                // The first crossing, not any: march out in ShadowMarchM steps until the ray is at or under the ground, so a
                // ridge the ray clips puts the shadow on the ridge, not in the valley beyond it (review of #2490).
                for (float s = ShadowMarchM; s < hi; s += ShadowMarchM)
                {
                    var q = drawn + ray * s;
                    if (q.y > Surface(ground, seaY, q)) lo = s; else { hi = s; break; }
                }
                for (int i = 0; i < ShadowConvergeSteps && hi - lo > ShadowConvergeM; i++)
                {
                    float mid = 0.5f * (lo + hi);
                    var q = drawn + ray * mid;
                    if (q.y > Surface(ground, seaY, q)) lo = mid; else hi = mid;
                }
                hit = drawn + ray * hi;
                floor = Surface(ground, seaY, hit);
            }
            else
            {
                floor = Surface(ground, seaY, drawn);
                for (int i = 0; i < 2; i++)
                {
                    hit = drawn + ray * ((drawn.y - floor) / -ray.y);
                    floor = Surface(ground, seaY, hit);
                }
            }
            var across = new Vector3(forward.z, 0f, -forward.x);
            float half = shadowHalf.x * Mathf.Cos(bank * Mathf.Deg2Rad);
            float top = floor;
            top = Mathf.Max(top, Surface(ground, seaY, hit + across * half));
            top = Mathf.Max(top, Surface(ground, seaY, hit - across * half));
            top = Mathf.Max(top, Surface(ground, seaY, hit + forward * shadowHalf.y));
            top = Mathf.Max(top, Surface(ground, seaY, hit - forward * shadowHalf.y));
            shadow.SetPositionAndRotation(new Vector3(hit.x, top + ShadowLiftM, hit.z), Quaternion.LookRotation(forward, Vector3.up));
            shadow.localScale = new Vector3(half, 1f, shadowHalf.y);
        }

        /// <summary>The converging shadow's halvings (<see cref="LayShadow"/>): stop once the bracket along the ray is under
        /// <see cref="ShadowConvergeM"/> (m), and after <see cref="ShadowConvergeSteps"/> at most. From 4,200 m at a 30 degree
        /// sun the bracket starts under 10 km; the march (<see cref="ShadowMarchM"/>) leaves one step of it, so about 8 halvings.</summary>
        internal const float ShadowConvergeM = 1f, ShadowMarchM = 200f;
        internal const int ShadowConvergeSteps = 24;

        /// <summary>The ground's height under <paramref name="p"/>, or the sea's surface over the sea.</summary>
        static float Surface(Terrain ground, float seaY, Vector3 p) => Mathf.Max(ground.SampleHeight(p) + ground.transform.position.y, seaY);
    }
}
