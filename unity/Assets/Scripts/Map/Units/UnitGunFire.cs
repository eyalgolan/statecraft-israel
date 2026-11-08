#nullable enable
using System.Collections.Generic;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The 65 mm gun's shot on an attacking token (#2373, part of #1867; owner, 3 November 2026: "do everything with
    /// the 0$ and make it as high quality as possible"). No new art: everything is code on the model the token already draws,
    /// Tal Heuberger's "Napoleonchik" (H02), streamed as one prefab for both details; no file of theirs changes. When the
    /// token's attack plays (UnitTokenMotion.Fire, the same event the figures' fire takes answer), the tube slides back along
    /// its own axis and runs out again on a smooth return, as its hydro-pneumatic recoil did; the carriage hops back a little
    /// and noses up with a damped bounce; a muzzle flash burns for a few frames; and three puffs of grey smoke roll forward
    /// and up, thinning as they go. The whole shot is a pure function of the time since the trigger (<see cref="Sample"/>),
    /// so a rebuilt token goes on from where its predecessor was (UnitTokenMotion.ContinueAttack) and a capture can hold any
    /// frame of it. The muzzle and the recoil axis are read from the model's own barrel node (its mesh bounds' long axis, the
    /// end farther from the body), never from a hand-pinned number, and fail open to the gun's front when the model has no
    /// barrel node. The quads, materials and textures are made on the first shot and destroyed with the piece; sampling a
    /// frame allocates nothing. The gun's share of the attack's length is its recoil (<see cref="RecoilS"/>); the smoke
    /// outlasts the attack on the gun's own clock. A hidden gun (the units' toggle) drops its shot, as a figure drops its take.</summary>
    [ExecuteAlways]
    public sealed class UnitGunFire : MonoBehaviour
    {
        /// <summary>The recoil and return, the gun's share of the attack's length.</summary>
        public const float RecoilS = 0.45f;
        /// <summary>How long the muzzle flash shows.</summary>
        public const float FlashS = 0.12f;
        public const int Puffs = 3;
        /// <summary>When the first puff leaves the muzzle, the gap to the next, and each puff's life.</summary>
        public const float FirstPuffS = 0.02f, PuffGapS = 0.06f, PuffLifeS = 1.3f;
        /// <summary>The whole shot: the last puff has faded.</summary>
        public const float TotalS = FirstPuffS + (Puffs - 1) * PuffGapS + PuffLifeS;
        /// <summary>The tube's recoil as a share of its length, and how fast it goes back before the slow return.</summary>
        public const float TubeTravel = 0.22f, TubeBackS = 0.07f;
        /// <summary>The carriage's hop as a share of the gun's length, its nose-up in degrees, and how fast it goes back.</summary>
        public const float HopShare = 0.035f, NoseUpDeg = 1.6f, HopBackS = 0.05f;
        /// <summary>The flash's width and the puffs' size and travel, as shares of the gun's length.</summary>
        public const float FlashShare = 0.5f, PuffRadiusShare = 0.06f, PuffGrowShare = 0.16f, PuffForwardShare = 0.3f, PuffRiseShare = 0.3f;
        public const float PuffAlpha = 0.85f;
        internal const string BarrelNode = "barrel", BodyNode = "body";

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseMap = Shader.PropertyToID("_BaseMap"), Cull = Shader.PropertyToID("_Cull");
        static readonly Color FlashColour = new Color(1f, 0.85f, 0.5f), SmokeFresh = new Color(0.9f, 0.88f, 0.85f), SmokeThin = new Color(0.72f, 0.72f, 0.72f);
        static readonly float[] PuffSide = { -0.07f, 0.03f, 0.08f }, PuffSpin = { 20f, 140f, 260f };

        // The model's tube and the frame it recoils in, bound on the first shot after the model arrives.
        Transform? model, barrel, holder;
        Vector3 barrelRest, barrelBack, muzzleInBarrel;   // the tube's rest, one holder unit of recoil in its own local units, and its muzzle
        Vector3 holderRest; Quaternion holderRestTurn;
        Vector3 dir, muzzle, noseAxis;           // holder-local: the tube's axis towards the muzzle, the muzzle, the nose-up axis
        float tubeLength, length;                // holder-local: the tube's length and the gun's
        Vector3 pieceDir, pieceMuzzle, pieceRight; float pieceLength;   // the same in the piece's frame, where the effects live
        // The effects, made on the first shot.
        bool built;
        Transform? flashT; Renderer? flash; Material? flashMat;
        readonly Transform?[] puffT = new Transform?[Puffs]; readonly Renderer?[] puff = new Renderer?[Puffs]; readonly Material?[] puffMat = new Material?[Puffs];
        readonly List<Object> owned = new List<Object>();
        float t = -1f;

        /// <summary>A shot is under way (the smoke may still be fading after the attack ended).</summary>
        public bool Playing => t >= 0f;
        /// <summary>Seconds into the shot, or -1 at rest.</summary>
        public float Time => t;
        /// <summary>The tube's axis towards the muzzle and the muzzle, in the model holder's frame (tests).</summary>
        internal Vector3 Direction => dir;
        internal Vector3 Muzzle => muzzle;

        /// <summary>Gives vehicle piece <paramref name="piece"/>, dressed as <paramref name="vehicle"/>, its shot: only the gun.</summary>
        internal static void Equip(Transform piece, string? vehicle)
        {
            if (vehicle != UnitFigures.Napoleonchik || piece == null || piece.TryGetComponent<UnitGunFire>(out _)) return;
            var gun = piece.gameObject.AddComponent<UnitGunFire>();
            gun.enabled = false;   // runs only while a shot plays
        }

        /// <summary>True when the gun's model is there to fire (binding it the first time, which allocates once).</summary>
        public bool Ready => Bind();

        /// <summary>Starts the shot <paramref name="from"/> seconds in (0: the trigger; a continued attack passes what its
        /// predecessor had played). Nothing happens until the model has arrived.</summary>
        public void Fire(float from = 0f)
        {
            if (!Bind()) return;
            if (!built) Build();
            enabled = true;
            Sample(from);
        }

        /// <summary>Advances the shot by <paramref name="seconds"/>; LateUpdate passes the frame's time in a player.</summary>
        internal void Tick(float seconds) { if (t >= 0f) Sample(t + Mathf.Max(0f, seconds)); }

        /// <summary>Puts the gun and its effects as they are <paramref name="seconds"/> into the shot: the same time gives the
        /// same frame. At or past <see cref="TotalS"/> everything is back at rest and the component stops. No allocation.</summary>
        public void Sample(float seconds)
        {
            if (!Bind()) { Stop(); return; }   // the model was retired meanwhile: nothing to move
            t = seconds;
            if (t >= TotalS) { Stop(); return; }
            float tube = Tube(t), hop = Hop(t);
            if (barrel != null) barrel.localPosition = barrelRest + barrelBack * (TubeTravel * tubeLength * tube);
            holder!.localPosition = holderRest - pieceDir * (HopShare * pieceLength * hop);
            holder.localRotation = holderRestTurn * Quaternion.AngleAxis(NoseUpDeg * hop, noseAxis);
            float flashAlpha = Flash(t);
            if (flash != null && flashT != null && flashMat != null)
            {
                flash.enabled = flashAlpha > 0f;
                if (flash.enabled)
                {
                    float grow = 0.7f + 0.3f * Mathf.Clamp01(t / 0.03f);
                    // On the tube's own muzzle, so it rides the recoil; a little ahead of the bore.
                    if (barrel != null) flashT.position = barrel.TransformPoint(muzzleInBarrel); else flashT.localPosition = pieceMuzzle;
                    flashT.localPosition += pieceDir * (0.03f * pieceLength);
                    flashT.localScale = Vector3.one * (FlashShare * pieceLength * grow);
                    var c = FlashColour; c.a = flashAlpha; flashMat.SetColor(BaseColor, c);
                }
            }
            for (int i = 0; i < Puffs; i++)
            {
                var r = puff[i]; var pt = puffT[i]; var m = puffMat[i];
                if (r == null || pt == null || m == null) continue;
                float u = (t - FirstPuffS - i * PuffGapS) / PuffLifeS;
                r.enabled = u > 0f && u < 1f;
                if (!r.enabled) continue;
                Puff(u, out float forward, out float rise, out float radius, out float alpha);
                pt.localPosition = pieceMuzzle + pieceDir * (forward * pieceLength) + Vector3.up * (rise * pieceLength) + pieceRight * (PuffSide[i] * u * pieceLength);
                pt.localScale = Vector3.one * (2.6f * radius * pieceLength);
                var c = Color.Lerp(SmokeFresh, SmokeThin, u); c.a = alpha; m.SetColor(BaseColor, c);
            }
        }

        /// <summary>The tube's recoil at <paramref name="t"/> (0 at rest, 1 fully back): a sharp kick, then a smooth run-out.</summary>
        public static float Tube(float t)
        {
            if (t <= 0f || t >= RecoilS) return 0f;
            if (t < TubeBackS) { float k = 1f - t / TubeBackS; return 1f - k * k; }
            return 1f - Mathf.SmoothStep(0f, 1f, (t - TubeBackS) / (RecoilS - TubeBackS));
        }

        /// <summary>The carriage's hop at <paramref name="t"/> (1 fully back): a kick, then a damped bounce that settles
        /// through a small forward overshoot and is exactly 0 at <see cref="RecoilS"/>.</summary>
        public static float Hop(float t)
        {
            if (t <= 0f || t >= RecoilS) return 0f;
            if (t < HopBackS) { float k = 1f - t / HopBackS; return 1f - k * k; }
            float u = (t - HopBackS) / (RecoilS - HopBackS);
            return Mathf.Exp(-5f * u) * Mathf.Cos(1.5f * Mathf.PI * u);
        }

        /// <summary>The flash's strength at <paramref name="t"/>: full at the shot, gone by <see cref="FlashS"/>.</summary>
        public static float Flash(float t) => t < 0f || t >= FlashS ? 0f : 1f - Mathf.SmoothStep(0f, 1f, t / FlashS);

        /// <summary>A puff <paramref name="u"/> of the way through its life: how far forward of the muzzle and how high it is
        /// and how big, as shares of the gun's length, and how opaque. It leaves the bore fast, slows, rises and thins.</summary>
        public static void Puff(float u, out float forward, out float rise, out float radius, out float alpha)
        {
            float s = Mathf.Sqrt(Mathf.Clamp01(u));
            forward = 0.02f + PuffForwardShare * s;
            radius = PuffRadiusShare + PuffGrowShare * s;
            rise = PuffRiseShare * u + radius * 0.5f;
            alpha = PuffAlpha * Mathf.SmoothStep(0f, 1f, u / 0.08f) * (1f - Mathf.SmoothStep(0f, 1f, (u - 0.4f) / 0.6f));
        }

        /// <summary>Finds the model under the piece and its tube, once: where the muzzle is and which way the recoil goes, from
        /// the barrel node's mesh bounds (its long horizontal axis, up its elevation; the muzzle is the end farther from the
        /// body). A model without a barrel node recoils whole, level along the gun's own length, its muzzle at the front.</summary>
        bool Bind()
        {
            if (model != null) return true;
            var h = transform.Find(UnitFigures.ModelName);
            if (h == null || h.childCount == 0) return false;
            var m = h.GetChild(0);
            if (!(UnitFigures.BoxIn(h, m) is Bounds whole) || whole.size.y <= 1e-6f) return false;
            var barrelNode = UnitWeapons.FindDeep(m, BarrelNode);
            var bodyNode = UnitWeapons.FindDeep(m, BodyNode);
            var tube = barrelNode != null && UnitFigures.BoxIn(h, barrelNode) is Bounds tb ? tb : whole;
            var body = bodyNode != null && UnitFigures.BoxIn(h, bodyNode) is Bounds bb ? bb : whole;
            int axis = tube.size.x > tube.size.z ? 0 : 2, across = axis == 0 ? 2 : 0;   // the tube lies near level: its long axis is never up
            float sign = Mathf.Sign(tube.center[axis] - body.center[axis]);   // Sign(0) is 1: a whole model fires forward
            // An elevated tube's box is taller than the tube is wide by the rise from breech to muzzle; the muzzle is the high
            // end (a gun elevates, never depresses, at rest). A whole model's box is wider than tall, so it fires level.
            float rise = Mathf.Max(0f, tube.size.y - tube.size[across]);
            dir = Vector3.zero; dir[axis] = sign * tube.size[axis]; dir.y = rise; dir.Normalize();
            muzzle = tube.center + dir * (0.5f * tube.size[axis] / Mathf.Abs(dir[axis]));   // the box's far face, up the incline
            tubeLength = Mathf.Sqrt(tube.size[axis] * tube.size[axis] + rise * rise); length = Mathf.Max(whole.size.x, whole.size.z);
            noseAxis = Vector3.Cross(dir, Vector3.up);
            holder = h; model = m; barrel = barrelNode;
            holderRest = h.localPosition; holderRestTurn = h.localRotation;
            if (barrelNode != null)
            {
                barrelRest = barrelNode.localPosition;
                barrelBack = barrelNode.parent.InverseTransformVector(h.TransformVector(-dir));
                muzzleInBarrel = barrelNode.InverseTransformPoint(h.TransformPoint(muzzle));
            }
            pieceDir = holderRestTurn * dir; pieceRight = Vector3.Cross(Vector3.up, pieceDir).normalized;
            pieceMuzzle = holderRest + holderRestTurn * Vector3.Scale(muzzle, h.localScale);
            pieceLength = Vector3.Scale(dir * length, h.localScale).magnitude;
            return true;
        }

        /// <summary>The flash and the puffs: camera-facing quads under the piece (so they turn with the token, not with the
        /// recoiling gun), on the under-attack look's materials (HexMaterials: the additive glow for the flash, the lit smoke
        /// for the puffs, with its soft puff texture) and a flash texture drawn in code. Hidden until a shot.</summary>
        void Build()
        {
            built = true;
            flashMat = Own(HexMaterials.New(HexMaterials.AttackGlow)); flashMat.SetTexture(BaseMap, Own(FlashTexture()));
            (flashT, flash) = Quad("MuzzleFlash", flashMat);
            var puffTexture = Own(HexUnderAttackLook.PuffTexture());
            for (int i = 0; i < Puffs; i++)
            {
                var m = puffMat[i] = Own(HexMaterials.New(HexMaterials.AttackSmoke));
                m.SetTexture(BaseMap, puffTexture); m.SetFloat(Cull, (float)CullMode.Off);
                (puffT[i], puff[i]) = Quad("MuzzleSmoke" + i, m);
            }
        }

        (Transform, Renderer) Quad(string name, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material; r.enabled = false;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return (go.transform, r);
        }

        T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        /// <summary>A muzzle flash: a hot core, eight short rays and a soft warm halo, white at the centre and orange at the rim.
        /// Pure arithmetic, so every gun draws the same texture.</summary>
        internal static Texture2D FlashTexture()
        {
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "muzzle-flash", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N * 2f - 1f, y = (j + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
                    float core = Mathf.Exp(-(r * r) / 0.14f);
                    float rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * a + 0.4f)), 16f) * Mathf.Clamp01(1f - r / 0.95f);
                    float halo = 0.5f * Mathf.Pow(Mathf.Clamp01(1f - r / 0.75f), 2f);
                    float alpha = Mathf.Clamp01(core + 0.9f * rays + halo);
                    var colour = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.15f), Mathf.Clamp01(r / 0.5f));
                    px[j * N + i] = new Color(colour.r, colour.g, colour.b, alpha);
                }
            tex.SetPixels32(px); tex.Apply(true, false);
            return tex;
        }

        /// <summary>Turns the flash and the puffs to face the camera about to render (every camera in turn, as the badges).</summary>
        internal void Face(ScriptableRenderContext _, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            var view = cam.transform.rotation;
            if (flash != null && flash.enabled && flashT != null) flashT.rotation = view * Quaternion.Euler(0f, 0f, 37f);
            for (int i = 0; i < Puffs; i++)
            {
                var r = puff[i]; var pt = puffT[i];
                if (r != null && r.enabled && pt != null) pt.rotation = view * Quaternion.Euler(0f, 0f, PuffSpin[i]);
            }
        }

        /// <summary>Everything back at rest and hidden; the component stops.</summary>
        void Stop()
        {
            t = -1f;
            if (barrel != null) barrel.localPosition = barrelRest;
            if (holder != null) { holder.localPosition = holderRest; holder.localRotation = holderRestTurn; }
            if (flash != null) flash.enabled = false;
            for (int i = 0; i < Puffs; i++) if (puff[i] != null) puff[i]!.enabled = false;
            enabled = false;
        }

        void LateUpdate() { if (Application.isPlaying) Tick(UnityEngine.Time.deltaTime); }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Face;

        /// <summary>A gun hidden mid-shot (the units' toggle deactivates the layer) drops it, as a figure drops its take.</summary>
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Face;
            if (t >= 0f) Stop();
        }

        void OnDestroy()
        {
            foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
            owned.Clear();
        }
    }
}
