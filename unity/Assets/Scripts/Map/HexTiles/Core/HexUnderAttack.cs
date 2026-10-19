#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>The "under attack" look for a hex (a city or the unit on it): presentation only. It is a visual state
    /// the simulation can drive later (show it on a hex, hide it); nothing here reads or writes the simulation, and it is
    /// never saved in a board or its overrides. Movement and combat rules are the owner's (docs/gdd/military-combat.md).
    /// Kept calm, not cinematic: a few grey smoke columns drifting east on the prevailing west wind, a low warm glow
    /// where each rises, and a muted red border that pulses slowly. Three meshes and three materials, all made by and
    /// destroyed with the look's object (HexUnderAttackLook).</summary>
    public static class HexUnderAttack
    {
        public static string Name(int q, int r) => $"under attack {q},{r}";

        /// <summary>The look's material templates (Resources/HexTiles/Materials): smoke, fire glow, border.</summary>
        public static readonly string[] Materials = { HexMaterials.AttackSmoke, HexMaterials.AttackGlow, HexMaterials.AttackBorder };

        /// <summary>The border's pulse: slow, a steady signal rather than a flash.</summary>
        public const float PulsePeriodS = 2.4f;
        /// <summary>Smoke columns per hex: "a few points", outside the unit spot.</summary>
        public const int Columns = 3;

        /// <summary>Shows the look on hex (q, r) of the board under <paramref name="boardRoot"/> (board space, as
        /// HexBoardBuilder builds it), on <paramref name="surface"/>. Showing it again replaces it. Returns its object;
        /// Hide (or destroying the object) removes it and everything it made.</summary>
        public static GameObject Show(Transform boardRoot, IHexSurface surface, int q, int r, float sizeM = HexScale.HexM)
        {
            Hide(boardRoot, q, r);
            var go = new GameObject(Name(q, r));
            go.transform.SetParent(boardRoot, false);
            var c = HexGeometry.Centre(q, r, sizeM);
            go.transform.localPosition = new Vector3(c.x, surface.Height(c), c.z);
            go.AddComponent<HexUnderAttackLook>().Build(surface, q, r, sizeM);
            return go;
        }

        /// <summary>Removes the look from hex (q, r); false when it was not shown.</summary>
        public static bool Hide(Transform boardRoot, int q, int r)
        {
            var t = boardRoot.Find(Name(q, r));
            if (t == null) return false;
            t.SetParent(null, false);
            if (Application.isPlaying) Object.Destroy(t.gameObject); else Object.DestroyImmediate(t.gameObject);
            return true;
        }

        public static bool IsShown(Transform boardRoot, int q, int r) => boardRoot.Find(Name(q, r)) != null;

        /// <summary>The border's strength at time <paramref name="t"/> (seconds), 0 to 1: a smooth sine, the same for
        /// every hex so several attacked hexes pulse together.</summary>
        public static float Pulse(float t) => 0.5f - 0.5f * Mathf.Cos(Frac(t / PulsePeriodS) * 2f * Mathf.PI);

        /// <summary>Where the smoke columns rise, relative to the hex centre (y = 0), seeded by (q, r). They keep out of the
        /// unit spot and out of the hex's south side, so a column (seen from the south, as the board camera sees it)
        /// does not stand in front of the unit or the city's centre; and far enough in that the drifting top stays inside.</summary>
        public static Vector3[] SmokeBases(int q, int r, float sizeM)
        {
            var rng = new System.Random(unchecked(q * 73856093 ^ r * 19349663 ^ 0x5EED));
            var bases = new Vector3[Columns];
            // Spread over the north half and the flanks: 200 degrees from east-south-east round to west-south-west.
            float a0 = -10f + (float)rng.NextDouble() * 40f;
            for (int i = 0; i < Columns; i++)
            {
                float a = (a0 + i * 80f + ((float)rng.NextDouble() - 0.5f) * 30f) * Mathf.Deg2Rad;
                float d = sizeM * (0.36f + (float)rng.NextDouble() * 0.14f);
                var p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                // Pull in until the whole column (glow at the foot, the top drifted downwind) fits the hex.
                while (d > sizeM * 0.32f && !(HexGeometry.InHex(p, Vector3.zero, sizeM, Smoke.GlowM * sizeM * 1.2f + 60f)
                    && HexGeometry.InHex(p + Smoke.Wind * Smoke.DriftM * sizeM, Vector3.zero, sizeM, (Smoke.TopRadius * 1.2f * Smoke.QuadScale * 1.09f + 0.02f) * sizeM + 60f)))
                { d -= sizeM * 0.02f; p = p.normalized * d; }
                bases[i] = p;
            }
            return bases;
        }

        internal static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>Smoke and glow shape, in shares of the hex size S (units are ~0.2 S tall, buildings ~0.15 S across).</summary>
        internal static class Smoke
        {
            /// <summary>Prevailing west wind: the smoke leans east, a little north.</summary>
            public static readonly Vector3 Wind = new Vector3(0.94f, 0f, 0.34f);
            public const int Puffs = 14;
            public const float LifeS = 13f, HeightShare = 0.62f, DriftM = 0.2f, BaseRadius = 0.04f, TopRadius = 0.11f;
            /// <summary>A puff's drawn size over its radius: the soft texture fades well inside its rim.</summary>
            public const float QuadScale = 1.5f;
            /// <summary>Where the smoke starts: above the flames, so they show at its foot.</summary>
            public const float FootShare = 0.04f;
            public const float Alpha = 0.92f, GlowM = 0.11f;
        }
    }

    /// <summary>Animates one shown look, in edit mode and in play: Time(t) samples the smoke, the glow's flicker and the
    /// border's pulse at t seconds, as a pure function of t and the hex (seeded by q, r), so captures and tests are
    /// deterministic. Set Hold to keep a sampled time (Update then leaves it). Destroys its meshes and materials with
    /// its object.</summary>
    [ExecuteAlways]
    public sealed class HexUnderAttackLook : MonoBehaviour
    {
        public int Q, R;
        /// <summary>Keep the last sampled time: Update does not advance it.</summary>
        public bool Hold;
        public float SampledT { get; private set; } = float.NaN;

        readonly List<Object> owned = new List<Object>();
        Mesh smoke = null!, glow = null!;
        Material border = null!;
        float sizeM;
        Vector3[] bases = Array.Empty<Vector3>();
        float[] phase = Array.Empty<float>();
        // Smoke: one soft camera-facing octagon per puff. Time(t) samples each puff's centre, size, spin and colour;
        // Face(right, up) writes them for a view (the board camera's by default, each rendering camera's in turn).
        Vector3[] puffAt = Array.Empty<Vector3>(); float[] puffRadius = Array.Empty<float>(), puffSpin = Array.Empty<float>();
        Vector3[] smokeV = Array.Empty<Vector3>(), smokeN = Array.Empty<Vector3>(); Color32[] smokeC = Array.Empty<Color32>();
        /// <summary>A puff is an octagon round the soft disc (its corners hug the disc, unlike a quad's).</summary>
        const int PuffSides = 8;
        /// <summary>The octagon's rim in the puff's (right, up) plane, circumscribing the unit disc.</summary>
        static readonly Vector2[] PuffRim = Rim();
        static Vector2[] Rim()
        {
            var rim = new Vector2[PuffSides];
            float r = 1f / Mathf.Cos(Mathf.PI / PuffSides);
            for (int k = 0; k < PuffSides; k++) { float a = (k + 0.5f) * 2f * Mathf.PI / PuffSides; rim[k] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            return rim;
        }
        /// <summary>The board camera's view (HexBoardCapture and the game rig: pitch 50 degrees, facing north).</summary>
        static readonly Quaternion BoardView = Quaternion.Euler(50f, 0f, 0f);
        // Glow: fixed vertices (discs on the ground, small flame cones), per-vertex base colour scaled by the flicker.
        Vector3[] glowRest = Array.Empty<Vector3>(), glowV = Array.Empty<Vector3>(); Color[] glowBase = Array.Empty<Color>(); Color32[] glowC = Array.Empty<Color32>();
        int[] glowColumn = Array.Empty<int>(); bool[] glowFlameTip = Array.Empty<bool>();

        static readonly Color SmokeLow = new Color(0.13f, 0.12f, 0.115f), SmokeHigh = new Color(0.40f, 0.395f, 0.385f);
        static readonly Color GlowCore = new Color(1f, 0.34f, 0.08f), FlameCore = new Color(1f, 0.62f, 0.22f);
        static readonly Color BorderColour = new Color(0.78f, 0.13f, 0.10f);
        const float BorderLowAlpha = 0.3f, BorderHighAlpha = 0.88f;

        internal void Build(IHexSurface surface, int q, int r, float size)
        {
            Q = q; R = r; sizeM = size;
            var centre = transform.localPosition;
            float Ground(Vector3 local) { var p = centre + local; return Mathf.Max(surface.Height(new Vector3(p.x, 0f, p.z)), 0f) - centre.y; }
            bases = HexUnderAttack.SmokeBases(q, r, size);
            phase = new float[bases.Length];
            var rng = new System.Random(unchecked(q * 83492791 ^ r * 2654435 ^ 0xF17E));
            for (int i = 0; i < bases.Length; i++) { bases[i].y = Ground(bases[i]); phase[i] = (float)rng.NextDouble(); }
            Array.Sort(bases, phase, Comparer<Vector3>.Create((a, b) => b.z.CompareTo(a.z)));   // far (north) first

            smoke = Own(new Mesh { name = "under-attack-smoke" }); smoke.MarkDynamic();
            int n = bases.Length * HexUnderAttack.Smoke.Puffs;
            puffAt = new Vector3[n]; puffRadius = new float[n]; puffSpin = new float[n];
            const int V = PuffSides + 1;
            smokeV = new Vector3[n * V]; smokeN = new Vector3[n * V]; smokeC = new Color32[n * V];
            var uv = new Vector2[n * V]; var tris = new int[n * PuffSides * 3];
            for (int p = 0; p < n; p++)
            {
                int o = p * V;
                uv[o] = new Vector2(0.5f, 0.5f);
                for (int k = 0; k < PuffSides; k++)
                {
                    uv[o + 1 + k] = new Vector2(0.5f + 0.5f * PuffRim[k].x, 0.5f + 0.5f * PuffRim[k].y);
                    // Clockwise as the viewer sees it (rim counter-clockwise in right/up): centre, next, this.
                    tris[(p * PuffSides + k) * 3] = o; tris[(p * PuffSides + k) * 3 + 1] = o + 1 + (k + 1) % PuffSides; tris[(p * PuffSides + k) * 3 + 2] = o + 1 + k;
                }
            }
            smoke.vertices = smokeV; smoke.normals = smokeN; smoke.colors32 = smokeC; smoke.uv = uv; smoke.triangles = tris;
            Part("Smoke", smoke, HexMaterials.AttackSmoke).SetTexture("_BaseMap", Own(PuffTexture()));

            glow = Own(BuildGlow(Ground)); glow.MarkDynamic();
            Part("Glow", glow, HexMaterials.AttackGlow);

            var ring = Own(BuildBorder(Ground));
            border = Part("Border", ring, HexMaterials.AttackBorder);
            Time(0f);
        }

        T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        Material Part(string name, Mesh mesh, string material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var m = Own(HexMaterials.New(material));
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return m;
        }

        /// <summary>Samples the look at <paramref name="t"/> seconds: the same t gives the same meshes and colours.</summary>
        public void Time(float t)
        {
            SampledT = t;
            int P = HexUnderAttack.Smoke.Puffs;
            float S = sizeM, top = HexUnderAttack.Smoke.HeightShare * S;
            int slot = 0;
            var order = new (float u, int i)[P];
            for (int c = 0; c < bases.Length; c++)
            {
                for (int i = 0; i < P; i++) order[i] = (HexUnderAttack.Frac(t / HexUnderAttack.Smoke.LifeS + (i + phase[c]) / P), i);
                Array.Sort(order, (a, b) => a.u.CompareTo(b.u));   // low first: the camera is above, so higher puffs are nearer
                foreach (var (u, i) in order)
                {
                    float seed = c * 7.31f + i * 1.93f, own = HexUnderAttack.Frac(Mathf.Sin(seed * 12.9898f) * 43758.547f);
                    // Each puff its own size and offset, so a column reads as smoke rather than a stack of beads.
                    float radius = Mathf.Lerp(HexUnderAttack.Smoke.BaseRadius, HexUnderAttack.Smoke.TopRadius, Mathf.Sqrt(u)) * S * (0.8f + 0.4f * own);
                    // Fade in just above the fire, thin out towards the top; the top puffs also shrink as they disperse.
                    float alpha = HexUnderAttack.Smoke.Alpha * Mathf.SmoothStep(0f, 1f, u / 0.06f) * (1f - Mathf.SmoothStep(0f, 1f, (u - 0.55f) / 0.45f));
                    radius *= 1f - 0.25f * Mathf.SmoothStep(0f, 1f, (u - 0.75f) / 0.25f);
                    var wobble = new Vector3(Mathf.Sin(seed * 3.1f + u * 5f), 0f, Mathf.Cos(seed * 2.3f + u * 4f)) * (0.02f * S * Mathf.Sqrt(u));
                    var at = bases[c] + new Vector3(0f, HexUnderAttack.Smoke.FootShare * S + radius * 0.5f + u * top, 0f)
                        + HexUnderAttack.Smoke.Wind * (HexUnderAttack.Smoke.DriftM * S * Mathf.Pow(u, 1.4f)) + wobble;
                    var colour = Color.Lerp(SmokeLow, SmokeHigh, Mathf.SmoothStep(0f, 1f, u / 0.7f)); colour.a = alpha;
                    Color32 c32 = colour;
                    puffAt[slot] = at; puffRadius[slot] = radius; puffSpin[slot] = (own - 0.5f) * 0.7f + u * 0.6f;
                    for (int v = 0; v <= PuffSides; v++) smokeC[slot * (PuffSides + 1) + v] = c32;
                    slot++;
                }
            }
            smoke.colors32 = smokeC;
            Face(BoardView * Vector3.right, BoardView * Vector3.up);

            for (int v = 0; v < glowRest.Length; v++)
            {
                float f = Flicker(t, phase[glowColumn[v]]);
                var col = glowBase[v]; col.a *= f;
                glowC[v] = col;
                var p = glowRest[v];
                if (glowFlameTip[v]) p.y = bases[glowColumn[v]].y + (p.y - bases[glowColumn[v]].y) * (0.8f + 0.35f * Flicker(t * 1.7f, phase[glowColumn[v]] + v * 0.13f));
                glowV[v] = p;
            }
            glow.vertices = glowV; glow.colors32 = glowC; glow.RecalculateBounds();

            var b = BorderColour; b.a = Mathf.Lerp(BorderLowAlpha, BorderHighAlpha, HexUnderAttack.Pulse(t));
            border.SetColor("_BaseColor", b);
        }

        /// <summary>Writes the smoke puffs to face a view with these world right and up axes (the puffs are round, so
        /// only the view matters, not the time).</summary>
        void Face(Vector3 right, Vector3 up)
        {
            right = transform.InverseTransformDirection(right); up = transform.InverseTransformDirection(up);
            var normal = Vector3.Cross(up, right).normalized;   // towards the viewer (Unity's cross is left-handed)
            for (int p = 0; p < puffAt.Length; p++)
            {
                float cs = Mathf.Cos(puffSpin[p]), sn = Mathf.Sin(puffSpin[p]);
                float h = puffRadius[p] * HexUnderAttack.Smoke.QuadScale;
                Vector3 x = (right * cs + up * sn) * h, y = (up * cs - right * sn) * h;
                var c = puffAt[p];
                int o = p * (PuffSides + 1);
                smokeV[o] = c; smokeN[o] = normal;
                for (int k = 0; k < PuffSides; k++) { smokeV[o + 1 + k] = c + x * PuffRim[k].x + y * PuffRim[k].y; smokeN[o + 1 + k] = normal; }
            }
            smoke.vertices = smokeV; smoke.normals = smokeN; smoke.RecalculateBounds();
        }

        /// <summary>A soft, slightly lumpy puff (white, alpha falling to the rim; a little darker below, as smoke
        /// shades itself). Pure arithmetic, so every look draws the same texture.</summary>
        static Texture2D PuffTexture()
        {
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "under-attack-puff", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N * 2f - 1f, y = (j + 0.5f) / N * 2f - 1f, a = Mathf.Atan2(y, x);
                    float rim = 0.86f + 0.07f * Mathf.Sin(a * 3f + 0.6f) + 0.05f * Mathf.Sin(a * 5f + 2.1f);
                    float d = Mathf.Sqrt(x * x + y * y) / rim;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - d * d), 0.7f);
                    float shade = Mathf.Lerp(0.72f, 1f, (y + 1f) * 0.5f);
                    px[j * N + i] = new Color(shade, shade, shade, alpha);
                }
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += FaceCamera;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= FaceCamera;
        void FaceCamera(ScriptableRenderContext context, Camera cam)
        {
            if (smoke != null && puffAt.Length > 0) Face(cam.transform.right, cam.transform.up);
        }

        /// <summary>A slow, irregular flicker between about 0.65 and 1 (two sines), offset per column.</summary>
        static float Flicker(float t, float phase) =>
            0.82f + 0.11f * Mathf.Sin(2f * Mathf.PI * (t * 0.7f + phase)) + 0.07f * Mathf.Sin(2f * Mathf.PI * (t * 1.9f + phase * 2.3f));

        /// <summary>Per column: a soft warm disc on the ground (centre bright, rim clear) and a few small flame cones.</summary>
        Mesh BuildGlow(Func<Vector3, float> ground)
        {
            const int Rim = 18, Flames = 3, Sides = 5; const float LiftM = 22f;
            var v = new List<Vector3>(); var col = new List<Color>(); var cols = new List<int>(); var tip = new List<bool>(); var tris = new List<int>();
            var rng = new System.Random(unchecked(Q * 19349663 ^ R * 83492791 ^ 0x6A0));
            for (int c = 0; c < bases.Length; c++)
            {
                var b = bases[c]; float rad = HexUnderAttack.Smoke.GlowM * sizeM;
                int o = v.Count;
                v.Add(new Vector3(b.x, b.y + LiftM, b.z)); col.Add(new Color(GlowCore.r, GlowCore.g, GlowCore.b, 0.85f));
                for (int k = 0; k < Rim; k++)
                {
                    float a = k * 2f * Mathf.PI / Rim;
                    // An irregular rim, so the patch reads as burning ground rather than a decal.
                    float rr = rad * (0.8f + 0.35f * (float)rng.NextDouble());
                    var p = b + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr; p.y = ground(p) + LiftM;
                    v.Add(p); col.Add(new Color(GlowCore.r, GlowCore.g * 0.8f, GlowCore.b, 0f));
                    tris.AddRange(new[] { o, o + 1 + (k + 1) % Rim, o + 1 + k });
                }
                for (int k = 0; k <= Rim; k++) { cols.Add(c); tip.Add(false); }
                for (int f = 0; f < Flames; f++)
                {
                    float a = (float)rng.NextDouble() * 2f * Mathf.PI, d = (float)rng.NextDouble() * rad * 0.4f;
                    var foot = b + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d; foot.y = ground(foot) + LiftM * 0.5f;
                    float h = sizeM * (0.045f + 0.03f * (float)rng.NextDouble()), w = sizeM * (0.016f + 0.008f * (float)rng.NextDouble());
                    int o2 = v.Count;
                    for (int s = 0; s < Sides; s++)
                    {
                        float sa = s * 2f * Mathf.PI / Sides + a;
                        v.Add(foot + new Vector3(Mathf.Cos(sa), 0f, Mathf.Sin(sa)) * w); col.Add(new Color(GlowCore.r, GlowCore.g, GlowCore.b, 0.8f));
                        cols.Add(c); tip.Add(false);
                    }
                    v.Add(foot + Vector3.up * h); col.Add(new Color(FlameCore.r, FlameCore.g, FlameCore.b, 0.15f)); cols.Add(c); tip.Add(true);
                    for (int s = 0; s < Sides; s++) tris.AddRange(new[] { o2 + s, o2 + Sides, o2 + (s + 1) % Sides });
                }
            }
            glowRest = v.ToArray(); glowV = (Vector3[])glowRest.Clone(); glowBase = col.ToArray(); glowC = new Color32[glowRest.Length];
            glowColumn = cols.ToArray(); glowFlameTip = tip.ToArray();
            var mesh = new Mesh { name = "under-attack-glow" };
            mesh.vertices = glowV; mesh.colors32 = glowC; mesh.triangles = tris.ToArray(); mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>A band just inside the hex edge: a solid outer strip, then a soft inner fade, following the ground.
        /// Its colour and pulse come from the material (_BaseColor), so pulsing costs no mesh upload.</summary>
        Mesh BuildBorder(Func<Vector3, float> ground)
        {
            const int Segments = 12; const float LiftM = 24f;
            float outer = 0.975f, solid = 0.035f, fade = 0.07f;   // corner-radius shares of S
            var rings = new[] { (outer, 1f), (outer - solid, 0.8f), (outer - solid - fade, 0f) };
            var corners = HexGeometry.Corners(Vector3.zero, sizeM);
            var v = new List<Vector3>(); var col = new List<Color>(); var tris = new List<int>();
            int perRing = 6 * Segments;
            foreach (var (share, alpha) in rings)
                for (int e = 0; e < 6; e++)
                    for (int s = 0; s < Segments; s++)
                    {
                        var p = Vector3.Lerp(corners[e], corners[(e + 1) % 6], s / (float)Segments) * share;
                        p.y = ground(p) + LiftM;
                        v.Add(p); col.Add(new Color(1f, 1f, 1f, alpha));
                    }
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < perRing; i++)
                {
                    int a = ring * perRing + i, b = ring * perRing + (i + 1) % perRing, c = a + perRing, d = b + perRing;
                    tris.AddRange(new[] { a, b, c, b, d, c });
                }
            var mesh = new Mesh { name = "under-attack-border" };
            mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        void Update()
        {
            if (Hold || smoke == null) return;
            Time(Application.isPlaying ? UnityEngine.Time.time : (float)UnityEngine.Time.realtimeSinceStartupAsDouble);
#if UNITY_EDITOR
            // Edit mode only updates on a change: ask for the next frame so the smoke and the pulse keep moving.
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        void OnDestroy()
        {
            foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
            owned.Clear();
        }
    }
}
