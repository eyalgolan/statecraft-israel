#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>The flipbook technique #2490 brought in for the Dakotas' bombs (DakotaBombRun), shared so the combat effects
    /// draw the same way: Unity Labs' two CC0 flipbooks ("Free VFX image sequences and flipbooks", Resources/Effects;
    /// ASSETS.yaml), each frame a quad mesh made once whose UVs cover that frame (the URP particle shaders ignore the
    /// material's tiling), shown on an unlit camera-facing quad; the frame picked from a time, so the same time draws the
    /// same frame. Presentation only.</summary>
    public static class Flipbook
    {
        /// <summary>The explosion (5 x 5 frames: a fireball that burns out into black smoke) and the smoke (8 x 8 frames of a
        /// boiling grey cloud), as Resources paths.</summary>
        public const string ExplosionResource = "Effects/Explosion01_5x5", SmokeResource = "Effects/Cloud02_8x8";
        public const int ExplosionGrid = 5, SmokeGrid = 8;
        /// <summary>The explosion's frames that still burn (0 to 9 have open fire; from 10 it is smoke).</summary>
        public const int ExplosionFireFrames = 10;

        static readonly int BaseMap = Shader.PropertyToID("_BaseMap"), Cull = Shader.PropertyToID("_Cull");

        /// <summary>The flipbook at <paramref name="resource"/>, or null when it is missing (Resources keeps it: not owned).</summary>
        public static Texture2D? Load(string resource) => Resources.Load<Texture2D>(resource);

        /// <summary>Where frame <paramref name="frame"/> of a <paramref name="grid"/> x <paramref name="grid"/> flipbook sits in
        /// its texture (frame 0 top left, then along the rows, as Unity's texture sheet animation): its lower left UV and its
        /// side (1 / grid).</summary>
        public static Vector2 FrameOrigin(int frame, int grid, out float side)
        {
            side = 1f / grid;
            return new Vector2((frame % grid) * side, 1f - (frame / grid + 1) * side);
        }

        /// <summary>The frame of <paramref name="frames"/> a one-shot shows <paramref name="share"/> of the way through its
        /// life (clamped to the first and last).</summary>
        public static int FrameAt(float share, int frames) => Mathf.Clamp((int)(share * frames), 0, frames - 1);

        /// <summary>The frame of <paramref name="frames"/> a looping flipbook shows at frame clock <paramref name="clock"/>
        /// (seconds x frames per second, plus any offset), wrapping round; never negative.</summary>
        public static int Looping(double clock, int frames)
        {
            int m = (int)(Math.Floor(clock) % frames);
            return m < 0 ? m + frames : m;
        }

        /// <summary>One quad mesh per frame of a <paramref name="grid"/> x <paramref name="grid"/> flipbook, its UVs on that
        /// frame, facing -z as the built-in quad, white (the particle shaders multiply the vertex colour), each passed to
        /// <paramref name="own"/> (whoever destroys it) and named <paramref name="name"/>-{grid}x{grid}-{frame}.</summary>
        public static Mesh[] Frames(int grid, Func<Mesh, Mesh> own, string name)
        {
            var frames = new Mesh[grid * grid];
            var v = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            var normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            var white = new Color32[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            var tris = new[] { 0, 3, 1, 3, 0, 2 };
            for (int f = 0; f < frames.Length; f++)
            {
                var o = FrameOrigin(f, grid, out float k);
                var uv = new[] { o, new Vector2(o.x + k, o.y), new Vector2(o.x, o.y + k), new Vector2(o.x + k, o.y + k) };
                var m = new Mesh { name = $"{name}-{grid}x{grid}-{f}", vertices = v, normals = normals, colors32 = white, uv = uv, triangles = tris };
                m.RecalculateBounds();
                frames[f] = own(m);
            }
            return frames;
        }

        /// <summary>A hidden quad renderer under <paramref name="parent"/> drawing <paramref name="mesh"/> with
        /// <paramref name="material"/>: no shadows, no probes.</summary>
        public static (Transform, Renderer, MeshFilter) Part(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var filter = go.GetComponent<MeshFilter>(); filter.sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material; r.enabled = false;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return (go.transform, r, filter);
        }

        /// <summary>A copy of the under-attack template <paramref name="template"/> (HexMaterials) showing
        /// <paramref name="texture"/>, both faces drawn.</summary>
        public static Material NewMaterial(string template, Texture2D texture)
        {
            var m = HexMaterials.New(template); m.SetTexture(BaseMap, texture); m.SetFloat(Cull, (float)CullMode.Off); return m;
        }
    }

    /// <summary>A small fixed pool of one-shot flipbook bursts (an explosion, a puff of dust): each slot one quad made once,
    /// hidden until used, on a material of its own (its colour fades by the material, so no mesh is uploaded), and every
    /// slot's look a pure function of the clock (<see cref="Sample"/>), so a held time repeats exactly and sampling allocates
    /// nothing. A burst starts at a clock time (later than now for a delayed one) and plays its <see cref="Style"/>: it opens
    /// small and swells, rises, plays the flipbook once and fades out at its end, then frees its slot. A full pool takes
    /// the slot whose burst started first. Whoever makes it destroys what it made (<see cref="Owned"/>).</summary>
    public sealed class FlipbookBursts
    {
        /// <summary>How a burst plays: its life (s), the flipbook frames it runs through (from <see cref="FirstFrame"/>,
        /// <see cref="Frames"/> of them), its size when it opens as a share of its full size, how much more it spreads by its
        /// end, how far its centre rises over its life as a share of its size, the share of its life it fades over at the end,
        /// its colour fresh and at its end (alpha included), and how high its centre sits over its foot as a share of its size.</summary>
        public readonly struct Style
        {
            public readonly float LifeS, OpenShare, SpreadShare, RiseShare, FadeShare, CentreShare;
            public readonly int FirstFrame, Frames;
            public readonly Color Fresh, Old;
            public Style(float lifeS, int firstFrame, int frames, float openShare, float spreadShare, float riseShare, float fadeShare,
                Color fresh, Color old, float centreShare)
            {
                LifeS = lifeS; FirstFrame = firstFrame; Frames = frames; OpenShare = openShare; SpreadShare = spreadShare;
                RiseShare = riseShare; FadeShare = fadeShare; Fresh = fresh; Old = old; CentreShare = centreShare;
            }
        }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        readonly Style style;
        readonly Mesh[] frames;
        readonly Transform[] quad; readonly Renderer[] shown; readonly MeshFilter[] filter; readonly Material[] material;
        readonly double[] from;   // each slot's start on the clock, or NaN when free
        readonly Vector3[] foot; readonly float[] size, spin;
        readonly Object[] owned;
        Quaternion view = Quaternion.identity;

        /// <summary>The meshes and materials this pool made (the flipbook frames and one material per slot).</summary>
        public Object[] Owned => owned;
        public int Capacity => quad.Length;
        /// <summary>The slots' renderers (tests).</summary>
        public Renderer[] Renderers => shown;

        /// <summary><paramref name="capacity"/> slots under <paramref name="parent"/>, each a quad of
        /// <paramref name="texture"/>'s <paramref name="grid"/> x <paramref name="grid"/> frames on a copy of
        /// <paramref name="template"/>, named <paramref name="name"/>.</summary>
        public FlipbookBursts(Transform parent, string name, int capacity, Texture2D texture, int grid, string template, Style style)
        {
            this.style = style;
            var made = new System.Collections.Generic.List<Object>();
            frames = Flipbook.Frames(grid, m => { made.Add(m); return m; }, name);
            quad = new Transform[capacity]; shown = new Renderer[capacity]; filter = new MeshFilter[capacity]; material = new Material[capacity];
            from = new double[capacity]; foot = new Vector3[capacity]; size = new float[capacity]; spin = new float[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var m = material[i] = Flipbook.NewMaterial(template, texture); made.Add(m);
                (quad[i], shown[i], filter[i]) = Flipbook.Part(parent, name, frames[Mathf.Clamp(style.FirstFrame, 0, frames.Length - 1)], m);
                from[i] = double.NaN;
            }
            owned = made.ToArray();
        }

        /// <summary>The slots playing or waiting to play.</summary>
        public int Busy
        {
            get { int n = 0; for (int i = 0; i < from.Length; i++) if (!double.IsNaN(from[i])) n++; return n; }
        }

        /// <summary>Slot <paramref name="slot"/>'s burst: where its foot is and its start on the clock (NaN when free).</summary>
        public Vector3 Foot(int slot) => foot[slot];
        public double StartOf(int slot) => from[slot];
        /// <summary>The flipbook frame slot <paramref name="slot"/> shows now (the index of its frame mesh).</summary>
        public int FrameOf(int slot) => Array.IndexOf(frames, filter[slot].sharedMesh);

        /// <summary>Starts a burst <paramref name="sizeM"/> across with its foot at <paramref name="at"/> (world), at clock
        /// time <paramref name="start"/>, turned <paramref name="spinDegrees"/> on the screen. Returns its slot.</summary>
        public int Start(Vector3 at, float sizeM, double start, float spinDegrees = 0f)
        {
            int slot = 0;
            for (int i = 0; i < from.Length; i++)
            {
                if (double.IsNaN(from[i])) { slot = i; break; }
                if (from[i] < from[slot]) slot = i;
            }
            from[slot] = start; foot[slot] = at; size[slot] = sizeM; spin[slot] = spinDegrees;
            return slot;
        }

        /// <summary>Every slot as it is at clock time <paramref name="now"/>: hidden before its start, then playing, and freed
        /// (hidden) at its end. The same time gives the same frame. No allocation.</summary>
        public void Sample(double now)
        {
            for (int i = 0; i < from.Length; i++)
            {
                if (double.IsNaN(from[i])) { shown[i].enabled = false; continue; }
                float b = (float)((now - from[i]) / style.LifeS);
                if (b >= 1f) { from[i] = double.NaN; shown[i].enabled = false; continue; }
                bool on = b >= 0f;
                shown[i].enabled = on;
                if (!on) continue;
                filter[i].sharedMesh = frames[Mathf.Clamp(style.FirstFrame + Flipbook.FrameAt(b, style.Frames), 0, frames.Length - 1)];
                float s = size[i] * Swell(b, style.OpenShare, style.SpreadShare);
                quad[i].SetPositionAndRotation(foot[i] + Vector3.up * (style.CentreShare * s + style.RiseShare * size[i] * b), view * Quaternion.Euler(0f, 0f, spin[i]));
                quad[i].localScale = Vector3.one * s;
                var c = Color.Lerp(style.Fresh, style.Old, b); c.a *= Fade(b, style.FadeShare); material[i].SetColor(BaseColor, c);
            }
        }

        /// <summary>Turns the bursts showing to a camera with rotation <paramref name="camera"/>, and keeps it for the next
        /// samples.</summary>
        public void Face(Quaternion camera)
        {
            view = camera;
            for (int i = 0; i < quad.Length; i++) if (shown[i].enabled) quad[i].rotation = view * Quaternion.Euler(0f, 0f, spin[i]);
        }

        /// <summary>Frees every slot and hides it.</summary>
        public void Clear()
        {
            for (int i = 0; i < from.Length; i++) { from[i] = double.NaN; shown[i].enabled = false; }
        }

        /// <summary>A burst's size over its full size at <paramref name="b"/> of its life: from <paramref name="open"/>, swelling
        /// fast (easing out) to the full size by a quarter of its life, then spreading by <paramref name="spread"/> more, as
        /// DakotaBombRun.BlastSwell.</summary>
        public static float Swell(float b, float open, float spread)
        {
            float o = 1f - Mathf.Clamp01(b / 0.25f);
            return Mathf.Lerp(open, 1f, 1f - o * o) + spread * Mathf.Clamp01((b - 0.25f) / 0.75f);
        }

        /// <summary>A burst's alpha at <paramref name="b"/> of its life: fading in over its first 5 per cent, whole, then fading
        /// over the last <paramref name="fade"/> of it to nothing at its end.</summary>
        public static float Fade(float b, float fade) =>
            Mathf.SmoothStep(0f, 1f, b / 0.05f) * (1f - Mathf.SmoothStep(0f, 1f, (b - (1f - fade)) / fade));
    }
}
