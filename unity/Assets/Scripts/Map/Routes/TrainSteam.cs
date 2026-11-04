#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>The steam train's exhaust (owner, 4 November 2026: "Train steam"): a short trail of smoke and steam from the
    /// locomotive's chimney while the train runs, drifting back along the track. Palestine Railways burned oil, not coal,
    /// from 1942-43 (research_notes/Map/train-steam-oil-burning-2026-11-05.md), so the smoke leaves the chimney dark
    /// grey-black and thins to grey, with white steam puffs among it.
    /// Drawn as #2490's Dakota bombs are: Unity Labs' CC0 smoke flipbook (Flipbook) on unlit camera-facing quads, from
    /// <see cref="Puffs"/> slots made once. Each puff is a pure function of the clock: puff k leaves the chimney at
    /// k x <see cref="GapS"/> from where the chimney was then (RouteTrain.ChimneyAt), swells, rises, drifts back and fades
    /// over <see cref="LifeS"/>; so the same time draws the same trail, and sampling allocates nothing. Nothing shows while
    /// the train stands at either end (a puff left while it stood is never drawn), and nothing while it is hidden (the
    /// train's toggle or its date: this lives under the train, so it goes with it). Runs in play only, as the train does.
    /// Presentation only: it reads no game state.</summary>
    public sealed class TrainSteam : MonoBehaviour
    {
        public const string Name = "Steam";

        /// <summary>DESIGN VALUES: the puffs on screen at once, one leaving every <see cref="GapS"/> s, each living
        /// <see cref="LifeS"/> (so the trail is always the same few puffs); every <see cref="SteamEvery"/>th puff is white
        /// steam, the others smoke.</summary>
        public const int Puffs = 10, SteamEvery = 3;
        public const float GapS = 0.3f, LifeS = Puffs * GapS;

        /// <summary>DESIGN VALUES for the sizes, as shares of a puff's size (<see cref="PuffWidthShare"/> of the locomotive's
        /// width, so both trains steam to their own size): a puff opens at <see cref="OpenShare"/> and spreads by
        /// <see cref="SpreadShare"/> more by its end; it drifts back along the track <see cref="DriftShare"/> and rises
        /// <see cref="RiseShare"/> a second; its centre sits <see cref="CentreShare"/> over the chimney. A steam puff is
        /// <see cref="SteamSizeShare"/> of a smoke puff and gone by <see cref="SteamLifeShare"/> of the life.</summary>
        public const float PuffWidthShare = 1.6f, OpenShare = 0.45f, SpreadShare = 1.4f, DriftShare = 0.35f, RiseShare = 0.3f,
            CentreShare = 0.4f, SteamSizeShare = 0.75f, SteamLifeShare = 0.6f, FadeShare = 0.55f;

        /// <summary>DESIGN VALUES: the flipbook's frames a second (it boils as it goes, each puff from its own frame).</summary>
        public const float FramesPerSecond = 20f;

        /// <summary>DESIGN VALUES for the colours: the smoke flipbook's grey averages about 0.44 where opaque, so each tint is
        /// about twice the shade drawn. Oil-burning smoke leaves the chimney dark grey-black (about 0.14) and thins to grey
        /// (about 0.5); steam is white.</summary>
        public static readonly Color SmokeFresh = new Color(0.32f, 0.31f, 0.3f, 0.92f), SmokeOld = new Color(1.15f, 1.15f, 1.15f, 0.55f),
            SteamFresh = new Color(2.2f, 2.2f, 2.2f, 0.85f), SteamOld = new Color(2.25f, 2.25f, 2.25f, 0.5f);

        /// <summary>The clock (s), the train's own (RouteTrain runs on Time.time). Test seam (internal; the tests see it
        /// through InternalsVisibleTo).</summary>
        internal static Func<float> Clock = () => Time.time;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        RouteTrain train = null!;
        float sizeM;
        Mesh[] frames = Array.Empty<Mesh>();
        Transform[] quad = Array.Empty<Transform>();
        Renderer[] shown = Array.Empty<Renderer>();
        MeshFilter[] filter = Array.Empty<MeshFilter>();
        Material[] material = Array.Empty<Material>();
        float[] spin = Array.Empty<float>();   // each slot's puff's turn on the screen
        readonly List<Object> owned = new List<Object>();
        Quaternion view = Quaternion.identity;

        /// <summary>The meshes and materials this made (tests: freed with the train).</summary>
        internal IReadOnlyList<Object> Owned => owned;
        /// <summary>The puffs' renderers, one per slot (tests).</summary>
        internal Renderer[] Renderers => shown;
        public float SizeM => sizeM;

        /// <summary>Adds the steam to <paramref name="train"/>, its puffs <paramref name="sizeM"/> across. Null when the smoke
        /// flipbook is missing from Resources (a warning says so): the train runs without steam.</summary>
        public static TrainSteam? Attach(RouteTrain train, float sizeM)
        {
            var cloud = Flipbook.Load(Flipbook.SmokeResource);
            if (cloud == null)
            {
                Debug.LogWarning("TRAIN: the smoke flipbook is missing from Resources/Effects; the train runs without steam.");
                return null;
            }
            var go = new GameObject(Name);
            go.transform.SetParent(train.transform, false);
            var s = go.AddComponent<TrainSteam>();
            s.train = train; s.sizeM = sizeM;
            s.frames = Flipbook.Frames(Flipbook.SmokeGrid, m => { s.owned.Add(m); return m; }, "TrainSteam");
            s.quad = new Transform[Puffs]; s.shown = new Renderer[Puffs]; s.filter = new MeshFilter[Puffs]; s.material = new Material[Puffs]; s.spin = new float[Puffs];
            for (int i = 0; i < Puffs; i++)
            {
                var m = s.material[i] = Flipbook.NewMaterial(HexMaterials.AttackBorder, cloud); s.owned.Add(m);
                (s.quad[i], s.shown[i], s.filter[i]) = Flipbook.Part(go.transform, "Puff", s.frames[0], m);
            }
            var cam = Camera.main;
            if (cam != null) s.view = cam.transform.rotation;
            return s;
        }

        /// <summary>The slot puff <paramref name="k"/> draws in (each puff keeps its slot for its whole life).</summary>
        public static int SlotOf(long k) { int m = (int)(k % Puffs); return m < 0 ? m + Puffs : m; }

        /// <summary>Whether puff <paramref name="k"/> is white steam rather than smoke.</summary>
        public static bool IsSteam(long k) => k % SteamEvery == 0;

        /// <summary>The trail as it is at clock time <paramref name="now"/>: nothing while the train stands; else the last
        /// <see cref="Puffs"/> puffs, each from where the chimney was when it left (none for a puff that left while the train
        /// stood). The same time gives the same trail. No allocation.</summary>
        internal void Sample(float now)
        {
            if (train == null) return;
            if (!train.MovingAt(now)) { Hide(); return; }
            long newest = (long)Math.Floor(now / (double)GapS);
            for (int j = 0; j < Puffs; j++)
            {
                long k = newest - j;
                int i = SlotOf(k);
                float born = (float)(k * (double)GapS), age = now - born, b = age / LifeS;
                if (b < 0f || b >= 1f || !train.MovingAt(born)) { shown[i].enabled = false; continue; }
                var (head, dir) = train.HeadAt(born);
                var (chimney, back) = train.ChimneyAt(head, dir);
                bool steam = IsSteam(k);
                float size = sizeM * (steam ? SteamSizeShare : 1f);
                float s = size * FlipbookBursts.Swell(b, OpenShare, SpreadShare);
                var centre = chimney + back * (DriftShare * size * age) + Vector3.up * (CentreShare * s + RiseShare * size * age);
                spin[i] = Spin(k);
                quad[i].SetPositionAndRotation(centre, view * Quaternion.Euler(0f, 0f, spin[i]));
                quad[i].localScale = Vector3.one * s;
                filter[i].sharedMesh = frames[Flipbook.Looping(age * FramesPerSecond + SlotOf(k * 7) * 6, frames.Length)];
                Color c;
                if (steam)
                {
                    float sb = Mathf.Clamp01(b / SteamLifeShare);
                    c = Color.Lerp(SteamFresh, SteamOld, sb); c.a *= sb >= 1f ? 0f : FlipbookBursts.Fade(sb, FadeShare);
                }
                else { c = Color.Lerp(SmokeFresh, SmokeOld, b); c.a *= FlipbookBursts.Fade(b, FadeShare); }
                material[i].SetColor(BaseColor, c);
                shown[i].enabled = c.a > 0f;
            }
        }

        /// <summary>Puff <paramref name="k"/>'s turn on the screen (degrees, within 25 either way so the flipbook's lit side
        /// stays up).</summary>
        static float Spin(long k) { int m = (int)((k * 37) % 51); return (m < 0 ? m + 51 : m) - 25; }

        void Hide() { for (int i = 0; i < shown.Length; i++) shown[i].enabled = false; }

        void Update() => Sample(Clock());

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Face;

        /// <summary>Hidden (the train's toggle, its date) or stopped: every puff goes, so none is left over when it shows
        /// again.</summary>
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Face;
            Hide();
        }

        /// <summary>Turns the puffs to the camera about to render, as CombatBursts does.</summary>
        void Face(ScriptableRenderContext _, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            view = cam.transform.rotation;
            for (int i = 0; i < quad.Length; i++)
                if (shown[i].enabled) quad[i].rotation = view * Quaternion.Euler(0f, 0f, spin[i]);
        }

        void OnDestroy()
        {
            foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
            owned.Clear();
        }
    }
}
