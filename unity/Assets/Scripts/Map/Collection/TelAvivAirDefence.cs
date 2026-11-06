#nullable enable
using System;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Draws Tel Aviv's air defence on the second bombing period's turns (SeaAirCollection.AirDefence.cs; owner, "do
    /// 1+2"): (1) two of 101 Squadron's Avia S-199s, each diving on the lead Dakota once a loop, firing (the gun's flash and
    /// a tracer) and hitting it (puffs of smoke left where it was struck), then pulling away, never bringing it down: an
    /// illustration, as no source read shows a July interception over Tel Aviv; and (2) the anti-aircraft fire of 17 July
    /// 1948 (JTA 19 July 1948): flashes from gun sites on Tel Aviv's hex, flak bursting round the wing Dakota after its bombs,
    /// the last burst on it; it trails smoke, falls out of the loop and is gone in a blast on the ground; a fresh wing joins
    /// the loop out over the sea before the next pass (SeaAirCollection.WingRejoinS), so one loop is one raid.
    ///
    /// Everything is a pure function of the Dakotas' loop clock (SpitfireLoop.Clock, the lead's time; the wing's is
    /// SeaAirCollection.DakotaTrailSeconds behind), so a held shot repeats exactly. It runs after the Dakotas' own loops
    /// (DefaultExecutionOrder), and while the wing falls it moves the wing (and its flag and shadow) off the loop where
    /// DakotaLoop put it; DakotaLoop itself is unchanged. All the effects are camera-facing quads made once, hidden until used,
    /// their materials and frame meshes owned by this object; placing a frame allocates nothing. Editor renders and
    /// screenshots hold the loop's start, as the Dakotas do. Presentation only: no sim, content or hash change.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class TelAvivAirDefence : MonoBehaviour
    {
        /// <summary>One S-199: its airframe (nose +z, at the tokens' aircraft scale), its propeller (turns about its +z), its
        /// shadow and flag (beside it), half its length, height and span, and its shadow's half size across and along.</summary>
        internal sealed class Fighter
        {
            public readonly Transform Plane, Prop; public readonly Transform? Shadow, Flag; public readonly Vector3 Half; public readonly Vector2 ShadowHalf;
            /// <summary>Half the flag's height (m), or 0.</summary>
            public readonly float FlagHalf;
            public Fighter(Transform plane, Transform prop, Transform? shadow, Transform? flag, Vector3 half, Vector2 shadowHalf)
            {
                Plane = plane; Prop = prop; Shadow = shadow; Flag = flag; Half = half; ShadowHalf = shadowHalf;
                FlagHalf = flag != null ? flag.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * flag.localScale.y : 0f;
            }
        }

        /// <summary>The textures: the gun's flash, the blast flipbook and the smoke flipbook (grids of frames each way; 1 for a
        /// single picture).</summary>
        internal readonly struct Looks
        {
            public readonly Texture2D Flash, Blast, Smoke; public readonly int BlastGrid, SmokeGrid;
            public Looks(Texture2D flash, Texture2D blast, int blastGrid, Texture2D smoke, int smokeGrid)
            { Flash = flash; Blast = blast; BlastGrid = blastGrid; Smoke = smoke; SmokeGrid = smokeGrid; }
        }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseMap = Shader.PropertyToID("_BaseMap"), Cull = Shader.PropertyToID("_Cull");
        /// <summary>DESIGN VALUES: the gun's and the guns' flashes the muzzle flash's colour (UnitGunFire); the tracer a hot
        /// orange; the hits' and the trail's smoke dark grey, as burning oil; the flak's bursts near black, as flak's
        /// smoke reads in photographs, thinning paler.</summary>
        static readonly Color FlashColour = new Color(1f, 0.85f, 0.5f), TracerColour = new Color(1f, 0.7f, 0.3f);
        static readonly Color HitSmoke = new Color(0.36f, 0.33f, 0.30f), TrailSmoke = new Color(0.30f, 0.28f, 0.26f), FlakSmoke = new Color(0.16f, 0.15f, 0.14f), SmokeThin = new Color(0.7f, 0.68f, 0.65f);
        /// <summary>DESIGN VALUES: where on the lead the hits land, as shares of its half length (forward), of a fifth of its span
        /// (up from its centre) and of its half span (to its right), one a hit.</summary>
        static readonly Vector3[] HitSpot = { new Vector3(0.10f, 0.25f, 0.30f), new Vector3(-0.15f, -0.35f, 0.25f), new Vector3(0.25f, 0.05f, 0.35f), new Vector3(-0.30f, 0.45f, 0.30f) };
        /// <summary>DESIGN VALUE: a C-47's length over its span, read off the Dakota mesh's bounds when drawn
        /// (SeaAirCollection.FlyAirDefence passes it); this is the fallback.</summary>
        float dakotaLengthShare = 0.667f;

        Vector3 centre; bool still; Fighter[] fighters = Array.Empty<Fighter>();
        DakotaLoop? lead, wing; Transform? wingFlag, wingShadow; Renderer[] wingRenderers = Array.Empty<Renderer>(); Renderer? wingFlagR, wingShadowR;
        Quaternion wingLevel = Quaternion.identity; Vector3 wingOffset; float wingFlagUp; Vector2 wingShadowHalf;
        Vector3 wingHitAt; float wingHitHeading, wingHitSpeed, wingDrop; Vector3 crashAt; bool wingShown = true;
        Terrain? terrain; float seaY;
        Vector2[] sites = Array.Empty<Vector2>(); Vector3[] siteAt = Array.Empty<Vector3>(); Vector3[] burstAt = Array.Empty<Vector3>();
        Mesh[] blastFrames = Array.Empty<Mesh>(), smokeFrames = Array.Empty<Mesh>();
        Quaternion view = Quaternion.identity;

        // The pools: per fighter its tracers, its muzzle flash, its hits (a flash and a puff each); per burst a gun flash on
        // the ground, a flash and a puff in the air; the falling wing's trail and its blast on the ground.
        Part[] tracer = Array.Empty<Part>(), muzzle = Array.Empty<Part>(), hitFlash = Array.Empty<Part>(), hitPuff = Array.Empty<Part>();
        Part[] gunFlash = Array.Empty<Part>(), burstFlash = Array.Empty<Part>(), burstPuff = Array.Empty<Part>(), trail = Array.Empty<Part>();
        Part crash;

        /// <summary>One pooled quad: its transform, renderer, frame filter and own material.</summary>
        internal struct Part
        {
            public Transform T; public Renderer R; public MeshFilter F; public Material M;
        }

        /// <summary>The loop clock's seconds where <see cref="Place"/> last put it (the fighters' keep-clear boxes follow it).</summary>
        public float PlacedAt { get; private set; }
        /// <summary>True when everything holds the loop's start (edit mode, screenshots).</summary>
        public bool Still => still;
        /// <summary>The fighters' airframes (tests).</summary>
        public Transform[] FighterPlanes { get; private set; } = Array.Empty<Transform>();
        /// <summary>The pools' renderers (tests).</summary>
        internal Renderer[] TracerRenderers => Renderers(tracer);
        internal Renderer[] HitRenderers => Renderers(hitPuff);
        internal Renderer[] FlakRenderers => Renderers(burstPuff);
        internal Renderer[] TrailRenderers => Renderers(trail);
        internal Renderer CrashRenderer => crash.R;
        /// <summary>The falling wing's drawn centre where <see cref="Place"/> last put it while it fell (tests).</summary>
        internal Vector3 WingAt { get; private set; }
        /// <summary>True while the wing Dakota is drawn (on the loop or falling), false once shot down until the fresh wing joins.</summary>
        public bool WingShown => wingShown;
        static Renderer[] Renderers(Part[] parts) { var r = new Renderer[parts.Length]; for (int i = 0; i < parts.Length; i++) r[i] = parts[i].R; return r; }

        /// <summary><paramref name="centre"/>: the Dakotas' loop centre in the air. The fighters, the two Dakotas' loops, the
        /// wing's shadow (beside it, DakotaLoop lays it), the anti-aircraft gun sites (world x, z, on Tel Aviv's hex), the
        /// ground, the terrain and the looks. <paramref name="lengthShare"/>: the Dakota's length over its span.</summary>
        internal void Configure(Vector3 centre, Fighter[] fighters, DakotaLoop lead, DakotaLoop wing, Transform? wingShadow, Vector2[] sites,
            KitGround ground, Terrain? terrain, Looks looks, bool still, float lengthShare = 0.667f)
        {
            this.centre = centre; this.fighters = fighters; this.lead = lead; this.wing = wing; this.wingShadow = wingShadow; this.sites = sites;
            this.terrain = terrain; seaY = ground.SeaLevelY; this.still = still; dakotaLengthShare = lengthShare;
            FighterPlanes = new Transform[fighters.Length];
            for (int i = 0; i < fighters.Length; i++) FighterPlanes[i] = fighters[i].Plane;
            wingFlag = wing.Flag; wingFlagR = wingFlag != null ? wingFlag.GetComponent<Renderer>() : null; wingShadowR = wingShadow != null ? wingShadow.GetComponent<Renderer>() : null;
            wingRenderers = wing.GetComponentsInChildren<Renderer>(true);

            // The wing's model rotation and drawn-centre offset, read back from where its loop placed it (DakotaLoop.Place: the
            // rotation is the loop's turn times the model's level rotation; the position its drawn centre less the turn of
            // the offset), so the fall below draws it exactly as its loop does.
            float u0 = wing.PlacedAt - wing.Lag;
            var turn0 = LoopTurn(u0);
            wingLevel = Quaternion.Inverse(turn0) * wing.transform.localRotation;
            var drawn0 = centre + Flat(SeaAirCollection.DakotaLoopOffset(u0));
            wingOffset = Quaternion.Inverse(turn0) * (drawn0 - wing.transform.position);
            wingFlagUp = wingFlag != null ? wingFlag.position.y - drawn0.y : 0f;
            float span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota);
            wingShadowHalf = 0.5f * SeaAirCollection.ShadowShare * new Vector2(span, span * dakotaLengthShare);

            // Where it is hit, and its fall: from the loop at WingHitS, at its speed and heading there, down to the highest
            // ground under its path (so it never flies into a hill before its end), its blast on the ground under its end.
            var h = SeaAirCollection.DakotaLoopHeading(SeaAirCollection.WingHitS);
            wingHitAt = centre + Flat(SeaAirCollection.DakotaLoopOffset(SeaAirCollection.WingHitS));
            wingHitHeading = Mathf.Atan2(h.y, h.x);
            wingHitSpeed = (SeaAirCollection.DakotaLoopOffset(SeaAirCollection.WingHitS + 0.05f) - SeaAirCollection.DakotaLoopOffset(SeaAirCollection.WingHitS - 0.05f)).magnitude / 0.1f;
            float floor = float.NegativeInfinity;
            for (int i = 0; i <= 16; i++)
            {
                var p = SeaAirCollection.WingFall(i * SeaAirCollection.WingFallS / 16f, wingHitSpeed, wingHitHeading, out _, out _);
                floor = Mathf.Max(floor, ground.Y(wingHitAt.x + p.x, wingHitAt.z + p.y));
            }
            // Its centre comes down to WingFallEndShare of its span over that ground: it levels its wings as it goes in
            // (FallPoint), so at the end only its nose, pitched down, reaches the ground, where the blast goes up.
            float clear = SeaAirCollection.WingFallEndShare * span;
            wingDrop = Mathf.Max(0f, centre.y - (floor + clear));
            var end = SeaAirCollection.WingFall(SeaAirCollection.WingFallS, wingHitSpeed, wingHitHeading, out _, out _);
            crashAt = new Vector3(wingHitAt.x + end.x, ground.Y(wingHitAt.x + end.x, wingHitAt.z + end.y), wingHitAt.z + end.y);

            // The guns on the ground and the bursts in the air (each at a set miss from the wing where it is then, on its loop).
            siteAt = new Vector3[sites.Length];
            for (int i = 0; i < sites.Length; i++) siteAt[i] = new Vector3(sites[i].x, ground.Y(sites[i].x, sites[i].y), sites[i].y);
            burstAt = new Vector3[SeaAirCollection.FlakAtS.Length];
            for (int i = 0; i < burstAt.Length; i++) burstAt[i] = centre + Flat(SeaAirCollection.DakotaLoopOffset(SeaAirCollection.FlakAtS[i])) + SeaAirCollection.FlakMissM[i];

            Build(looks);
            var cam = Camera.main;
            if (cam != null) view = cam.transform.rotation;
            Place(still ? 0f : SpitfireLoop.Clock());
        }

        void Build(Looks looks)
        {
            var owned = GetComponent<MapRuntimeAssets>();   // not ??: a missing component is Unity's fake null in the editor
            if (owned == null) owned = gameObject.AddComponent<MapRuntimeAssets>();
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            blastFrames = DakotaBombRun.Frames(owned, looks.BlastGrid); smokeFrames = DakotaBombRun.Frames(owned, looks.SmokeGrid);
            Part Make(string name, Mesh mesh, string template, Texture2D texture)
            {
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                var f = go.GetComponent<MeshFilter>(); f.sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                var m = owned.Own(HexMaterials.New(template)); m.SetTexture(BaseMap, texture); m.SetFloat(Cull, (float)CullMode.Off);
                r.sharedMaterial = m; r.enabled = false;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                return new Part { T = go.transform, R = r, F = f, M = m };
            }
            int n = fighters.Length;
            tracer = new Part[n * SeaAirCollection.FighterTracers]; muzzle = new Part[n];
            hitFlash = new Part[n * SeaAirCollection.FighterHits]; hitPuff = new Part[n * SeaAirCollection.FighterHits];
            for (int i = 0; i < tracer.Length; i++) tracer[i] = Make("Tracer", quad, HexMaterials.AttackGlow, looks.Flash);
            for (int i = 0; i < n; i++) muzzle[i] = Make("GunFlash", quad, HexMaterials.AttackGlow, looks.Flash);
            for (int i = 0; i < hitPuff.Length; i++)
            {
                hitFlash[i] = Make("HitFlash", quad, HexMaterials.AttackGlow, looks.Flash);
                hitPuff[i] = Make("HitSmoke", smokeFrames[0], HexMaterials.AttackSmoke, looks.Smoke);
            }
            int bursts = SeaAirCollection.FlakAtS.Length;
            gunFlash = new Part[bursts]; burstFlash = new Part[bursts]; burstPuff = new Part[bursts];
            for (int i = 0; i < bursts; i++)
            {
                gunFlash[i] = Make("FlakGunFlash", quad, HexMaterials.AttackGlow, looks.Flash);
                burstFlash[i] = Make("FlakFlash", quad, HexMaterials.AttackGlow, looks.Flash);
                burstPuff[i] = Make("FlakBurst", smokeFrames[0], HexMaterials.AttackSmoke, looks.Smoke);
            }
            trail = new Part[SeaAirCollection.TrailPuffs];
            for (int i = 0; i < trail.Length; i++) trail[i] = Make("WingSmoke", smokeFrames[0], HexMaterials.AttackSmoke, looks.Smoke);
            // The blast is alpha-blended, as the bombs' (DakotaBombRun): its fire glows by its own colours, its smoke stays dark.
            crash = Make("WingCrash", blastFrames[0], HexMaterials.AttackBorder, looks.Blast);
        }

        void Update()
        {
            if (!still) Place(SpitfireLoop.Clock());
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Face;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Face;

        /// <summary>Keeps the camera's rotation for the quads and turns those showing to it, as DakotaLoop does the bombs'.</summary>
        void Face(ScriptableRenderContext _, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            view = cam.transform.rotation;
            Place(PlacedAt);
        }

        static Vector3 Flat(Vector2 v) => new Vector3(v.x, 0f, v.y);
        static int Mod(int a, int n) { int m = a % n; return m < 0 ? m + n : m; }

        /// <summary>DakotaLoop's turn at the loop time <paramref name="u"/>: the yaw from east to the heading, then the bank about it.</summary>
        static Quaternion LoopTurn(float u)
        {
            var h = SeaAirCollection.DakotaLoopHeading(u);
            var yaw = Quaternion.Euler(0f, -Mathf.Atan2(h.y, h.x) * Mathf.Rad2Deg, 0f);
            return Quaternion.AngleAxis(SeaAirCollection.DakotaBankDegrees(u), new Vector3(h.x, 0f, h.y)) * yaw;
        }

        /// <summary>Puts everything where it is <paramref name="seconds"/> into the loop's clock (the lead's time).</summary>
        public void Place(float seconds)
        {
            PlacedAt = seconds;
            for (int i = 0; i < fighters.Length; i++) PlaceFighter(i, seconds);
            float u = seconds - SeaAirCollection.DakotaTrailSeconds;   // the wing's own time
            PlaceFlak(u);
            PlaceWing(u);
        }

        void PlaceFighter(int i, float seconds)
        {
            var f = fighters[i];
            var drawn = centre + SeaAirCollection.FighterOffset(i, seconds);
            var v = SeaAirCollection.FighterVelocity(i, seconds);
            float speed = new Vector2(v.x, v.z).magnitude;
            var flat = speed > 1f ? new Vector3(v.x / speed, 0f, v.z / speed) : Vector3.right;
            float pitch = Mathf.Clamp(Mathf.Atan2(v.y, Mathf.Max(speed, 1f)) * Mathf.Rad2Deg, -SeaAirCollection.FighterPitchMaxDeg, SeaAirCollection.FighterPitchMaxDeg);
            var nose = flat * Mathf.Cos(pitch * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad);
            float bank = SeaAirCollection.FighterBankDegrees(i, seconds);
            f.Plane.SetPositionAndRotation(drawn, Quaternion.AngleAxis(bank, nose) * Quaternion.LookRotation(nose, Vector3.up));
            f.Prop.localRotation = Quaternion.AngleAxis(-(float)((double)seconds * SpitfireLoop.PropDegreesPerSecond % 360.0), Vector3.forward);
            if (f.Flag != null)
            {
                f.Flag.position = drawn - flat * f.Half.x + Vector3.up * (SpitfireLoop.TopOver(f.Half, bank) + f.Half.x * Mathf.Abs(Mathf.Sin(pitch * Mathf.Deg2Rad))
                    + DakotaLoop.FlagClearM + f.FlagHalf);
            }
            if (f.Shadow != null && terrain != null) SpitfireLoop.LayShadow(f.Shadow, terrain, seaY, f.ShadowHalf, drawn, flat, bank, converge: true);

            // The gun: flashes and tracers from the nose to the lead while it fires; hits on the lead, left where they struck.
            float share = SeaAirCollection.FighterAttackShare(i, seconds);
            bool firing = share >= SeaAirCollection.FighterFireFrom && share < SeaAirCollection.FighterFireTo;
            var gun = drawn + nose * f.Half.x;
            var aim = centre + Flat(SeaAirCollection.DakotaLoopOffset(seconds));   // the lead's drawn centre (its lag is 0)
            var line = aim - gun; float length = line.magnitude;
            var dir = length > 1f ? line / length : nose;
            var camForward = view * Vector3.forward;
            var face = camForward - dir * Vector3.Dot(camForward, dir);
            if (face.sqrMagnitude < 1e-6f) face = Vector3.Cross(dir, Vector3.up);
            face.Normalize();
            var along = Quaternion.LookRotation(face, Vector3.Cross(face, dir));   // its local x along the line, its face to the camera
            float fireStart = SeaAirCollection.FighterAttackStartS[i] + SeaAirCollection.FighterFireFrom * SeaAirCollection.FighterAttackS;
            for (int k = 0; k < SeaAirCollection.FighterTracers; k++)
            {
                ref var t = ref tracer[i * SeaAirCollection.FighterTracers + k];
                t.R.enabled = firing;
                if (!firing) continue;
                float run = (float)(((double)seconds - fireStart) * SeaAirCollection.TracerRate + (double)k / SeaAirCollection.FighterTracers);
                run -= Mathf.Floor(run);
                t.T.SetPositionAndRotation(gun + dir * (run * length), along);
                t.T.localScale = new Vector3(SeaAirCollection.TracerLengthM, SeaAirCollection.TracerWidthM, 1f);
                var c = TracerColour; c.a = 1f - 0.6f * run; t.M.SetColor(BaseColor, c);
            }
            ref var mz = ref muzzle[i];
            float flash = firing ? UnitGunFire.Flash(Mathf.Repeat(seconds - fireStart, SeaAirCollection.MuzzleFlashGapS)) : 0f;
            mz.R.enabled = flash > 0f;
            if (flash > 0f)
            {
                mz.T.SetPositionAndRotation(gun, view * Quaternion.Euler(0f, 0f, 37f));
                mz.T.localScale = Vector3.one * SeaAirCollection.MuzzleFlashM;
                var c = FlashColour; c.a = flash; mz.M.SetColor(BaseColor, c);
            }

            float phase = Mathf.Repeat(seconds - SeaAirCollection.FighterAttackStartS[i], SeaAirCollection.DakotaLoopSeconds);
            double cycle = (double)seconds - phase;   // this loop's attack start
            float span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota);
            for (int j = 0; j < SeaAirCollection.FighterHits; j++)
            {
                ref var hf = ref hitFlash[i * SeaAirCollection.FighterHits + j]; ref var hp = ref hitPuff[i * SeaAirCollection.FighterHits + j];
                float at = SeaAirCollection.FighterHitFrom + (SeaAirCollection.FighterHitTo - SeaAirCollection.FighterHitFrom) * j / (SeaAirCollection.FighterHits - 1f);
                double born = cycle + at * SeaAirCollection.FighterAttackS;
                float age = (float)((double)seconds - born);
                bool on = age >= 0f && age < SeaAirCollection.HitPuffS;
                hp.R.enabled = on;
                hf.R.enabled = on && age < UnitGunFire.FlashS * 2f;
                if (!on) continue;
                float b = (float)born;
                var hd = SeaAirCollection.DakotaLoopHeading(b);
                var fwd = Flat(hd); var right = new Vector3(hd.y, 0f, -hd.x);
                var s = HitSpot[j % HitSpot.Length];
                var spot = centre + Flat(SeaAirCollection.DakotaLoopOffset(b)) + fwd * (s.x * 0.5f * span * dakotaLengthShare) + right * (s.z * 0.5f * span)
                    + Vector3.up * (s.y * 0.2f * span);
                if (hf.R.enabled)
                {
                    hf.T.SetPositionAndRotation(spot, view * Quaternion.Euler(0f, 0f, 37f + 50f * j));
                    hf.T.localScale = Vector3.one * SeaAirCollection.HitFlashM;
                    var c = FlashColour; c.a = UnitGunFire.Flash(0.5f * age); hf.M.SetColor(BaseColor, c);
                }
                float life = age / SeaAirCollection.HitPuffS;
                UnitGunFire.Puff(life, out _, out float rise, out float radius, out float alpha);
                hp.F.sharedMesh = smokeFrames[Mathf.Min((int)(life * smokeFrames.Length), smokeFrames.Length - 1)];
                hp.T.SetPositionAndRotation(spot + Vector3.up * (rise * SeaAirCollection.HitPuffM), view * Quaternion.Euler(0f, 0f, 70f * j));
                hp.T.localScale = Vector3.one * (SeaAirCollection.HitPuffM * radius / (UnitGunFire.PuffRadiusShare + UnitGunFire.PuffGrowShare));   // grows to HitPuffM
                var sc = Color.Lerp(HitSmoke, SmokeThin, life); sc.a = alpha; hp.M.SetColor(BaseColor, sc);
            }
        }

        /// <summary>The flak at the wing's own loop time <paramref name="u"/>: each burst's gun flashes on the ground its flight
        /// time before; the burst flashes and leaves a dark puff that swells and thins.</summary>
        void PlaceFlak(float u)
        {
            float flight = SeaAirCollection.FlakFlightS;
            for (int i = 0; i < burstAt.Length; i++)
            {
                float since = Mathf.Repeat(u - SeaAirCollection.FlakAtS[i] + flight, SeaAirCollection.DakotaLoopSeconds) - flight;
                ref var g = ref gunFlash[i]; ref var bf = ref burstFlash[i]; ref var bp = ref burstPuff[i];
                float fromGun = since + flight;
                g.R.enabled = fromGun < UnitGunFire.FlashS * 2f && siteAt.Length > 0;
                if (g.R.enabled)
                {
                    g.T.SetPositionAndRotation(siteAt[i % siteAt.Length] + Vector3.up * (0.4f * SeaAirCollection.GunFlashM), view * Quaternion.Euler(0f, 0f, 20f * i));
                    g.T.localScale = Vector3.one * SeaAirCollection.GunFlashM;
                    var c = FlashColour; c.a = UnitGunFire.Flash(0.5f * fromGun); g.M.SetColor(BaseColor, c);
                }
                bf.R.enabled = since >= 0f && since < UnitGunFire.FlashS * 2.5f;
                if (bf.R.enabled)
                {
                    bf.T.SetPositionAndRotation(burstAt[i], view * Quaternion.Euler(0f, 0f, 37f + 40f * i));
                    bf.T.localScale = Vector3.one * (0.8f * SeaAirCollection.FlakBurstM);
                    var c = FlashColour; c.a = UnitGunFire.Flash(since / 2.5f); bf.M.SetColor(BaseColor, c);
                }
                float life = since / SeaAirCollection.FlakBurstS;
                bp.R.enabled = life >= 0f && life < 1f;
                if (!bp.R.enabled) continue;
                bp.F.sharedMesh = smokeFrames[Mathf.Min((int)(life * smokeFrames.Length), smokeFrames.Length - 1)];
                bp.T.SetPositionAndRotation(burstAt[i] + Vector3.up * (0.15f * life * SeaAirCollection.FlakBurstM), view * Quaternion.Euler(0f, 0f, 55f * i));
                bp.T.localScale = Vector3.one * (SeaAirCollection.FlakBurstM * (0.45f + 0.55f * Mathf.Sqrt(life)));
                var sc = Color.Lerp(FlakSmoke, SmokeThin, life * life);
                sc.a = Mathf.SmoothStep(0f, 1f, life / 0.06f) * (1f - Mathf.SmoothStep(0f, 1f, (life - 0.45f) / 0.55f));
                bp.M.SetColor(BaseColor, sc);
            }
        }

        /// <summary>The wing at its own loop time <paramref name="u"/>: on its loop (DakotaLoop placed it), falling (moved here,
        /// with its flag and shadow, its smoke trailing), or gone (hidden, the blast on the ground, the trail thinning).</summary>
        void PlaceWing(float u)
        {
            int state = SeaAirCollection.WingState(u, out float since);
            bool show = state != 2;
            if (show != wingShown)
            {
                wingShown = show;
                for (int i = 0; i < wingRenderers.Length; i++) if (wingRenderers[i] != null) wingRenderers[i].enabled = show;
                if (wingFlagR != null) wingFlagR.enabled = show;
                if (wingShadowR != null) wingShadowR.enabled = show;
            }
            if (state == 1 && wing != null)
            {
                var drawn = WingAt = FallPoint(since, out var flat, out float pitch, out float bank);
                var nose = flat * Mathf.Cos(pitch * Mathf.Deg2Rad) - Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad);
                var yaw = Quaternion.Euler(0f, -Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg, 0f);
                var turn = Quaternion.AngleAxis(bank, nose) * Quaternion.AngleAxis(-pitch, Vector3.Cross(flat, Vector3.up)) * yaw;
                wing.transform.localRotation = turn * wingLevel;
                wing.transform.position = drawn - turn * wingOffset;
                if (wingFlag != null) wingFlag.position = drawn + Vector3.up * wingFlagUp;
                if (wingShadow != null && terrain != null) SpitfireLoop.LayShadow(wingShadow, terrain, seaY, wingShadowHalf, drawn, flat, bank, converge: true);
            }

            // The smoke it trails from the hit, left in the air where it was let out.
            int newest = state == 0 ? -1 : (int)Mathf.Floor(Mathf.Min(since, SeaAirCollection.WingFallS) / SeaAirCollection.TrailGapS);
            for (int slot = 0; slot < trail.Length; slot++)
            {
                ref var p = ref trail[slot];
                int j = newest - Mod(newest - slot, trail.Length);
                float born = j * SeaAirCollection.TrailGapS, age = since - born;
                bool on = newest >= 0 && j >= 0 && age >= 0f && age < SeaAirCollection.TrailPuffS;
                p.R.enabled = on;
                if (!on) continue;
                var at = FallPoint(born, out var flat, out _, out _);
                float life = age / SeaAirCollection.TrailPuffS;
                float span = SeaAirCollection.SizeOf(SeaAirCollection.Dakota);
                p.F.sharedMesh = smokeFrames[Mod((int)Mathf.Floor(age * SeaAirCollection.DakotaColumnFps + slot * 7), smokeFrames.Length)];
                p.T.SetPositionAndRotation(at - flat * (0.15f * span * dakotaLengthShare) + Vector3.up * (0.25f * life * SeaAirCollection.TrailPuffM),
                    view * Quaternion.Euler(0f, 0f, 47f * slot));
                p.T.localScale = Vector3.one * (SeaAirCollection.TrailPuffM * (0.5f + 0.8f * Mathf.Sqrt(life)));
                var c = Color.Lerp(TrailSmoke, SmokeThin, life);
                c.a = 0.9f * Mathf.SmoothStep(0f, 1f, life / 0.05f) * (1f - Mathf.SmoothStep(0f, 1f, (life - 0.4f) / 0.6f));
                p.M.SetColor(BaseColor, c);
            }

            // Its blast where it comes down.
            float b = (since - SeaAirCollection.WingFallS) / SeaAirCollection.DakotaBlastS;
            crash.R.enabled = state == 2 && b >= 0f && b < 1f;
            if (crash.R.enabled)
            {
                crash.F.sharedMesh = blastFrames[Mathf.Min((int)(b * blastFrames.Length), blastFrames.Length - 1)];
                float size = SeaAirCollection.DakotaBlastM * DakotaBombRun.BlastSwell(b);
                crash.T.SetPositionAndRotation(crashAt + Vector3.up * (0.4f * size), view);
                crash.T.localScale = Vector3.one * size;
                var c = Color.white; c.a = DakotaBombRun.BlastAlpha(b); crash.M.SetColor(BaseColor, c);
            }
        }

        /// <summary>The falling wing's drawn centre <paramref name="since"/> seconds after the hit, its heading seen from above,
        /// its nose-down pitch and its roll (degrees).</summary>
        Vector3 FallPoint(float since, out Vector3 flat, out float pitch, out float bank)
        {
            float t = Mathf.Clamp(since, 0f, SeaAirCollection.WingFallS);
            var xz = SeaAirCollection.WingFall(t, wingHitSpeed, wingHitHeading, out float heading, out float drop);
            flat = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            float speed = wingHitSpeed * (1f - (1f - SeaAirCollection.WingFallSpeedShare) * t / SeaAirCollection.WingFallS);
            float sink = wingDrop * 2f * t / (SeaAirCollection.WingFallS * SeaAirCollection.WingFallS);
            pitch = Mathf.Min(Mathf.Atan2(sink, Mathf.Max(speed, 1f)) * Mathf.Rad2Deg, SeaAirCollection.WingFallPitchDeg);
            bank = SeaAirCollection.WingFallBankDeg * Mathf.SmoothStep(0f, 1f, t / 3f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f * SeaAirCollection.WingFallS) / (0.4f * SeaAirCollection.WingFallS)));
            return new Vector3(wingHitAt.x + xz.x, centre.y - wingDrop * drop, wingHitAt.z + xz.y);
        }
    }
}
