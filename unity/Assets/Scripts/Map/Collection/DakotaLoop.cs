#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Units;
using Statecraft.HexTiles;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Flies one of Tal Heuberger's two Egyptian Dakotas round their loop over Tel Aviv (SeaAirCollection.DakotaLoopOffset;
    /// owner, 4 November 2026, #2482: "1 + make the visual 2 slightly smaller planes and not 0ne big one", option 1: "On the
    /// bombing turns it flies over Tel Aviv (not just off the coast) and drops bombs with flashes and smoke on the city"), its
    /// own clock <see cref="Lag"/> behind the loop's (the wing trails the lead), nose along the path, banked into the turns
    /// (SeaAirCollection.DakotaBankDegrees), a propeller turning at each engine, its shadow on the ground, Egypt's flag
    /// floating over its tail (owner on #2260, "Smaller flag, no pole") and its bombs falling on Tel Aviv
    /// (<see cref="DakotaBombRun"/>), as the Spitfire flies (SpitfireLoop; owner, before the ruling: "it also doesnt look like
    /// its flying"). Presentation only: it reads the map's animation clock (SpitfireLoop.Clock, Time.time), sets a few
    /// transforms a frame and allocates nothing. It lives on the aircraft's own object, so it stops when the layer drops the
    /// aircraft. Editor renders and screenshots (`?shot` / `-shot`) hold the loop's start, as the train and the Spitfire do
    /// (RouteLayer.ModeFor). On the first bombing period's turns (<see cref="Raids"/>, #3140) it flies New game's raid again
    /// and again instead of its loop (DakotaRaidFlight.Cycle), and a held shot holds the raid at DakotaRaidFlight.HoldS.</summary>
    public sealed class DakotaLoop : MonoBehaviour
    {
        /// <summary>DESIGN VALUE: the gap (m) between the aircraft's box top and the flag's lower edge, as #2260's
        /// SpitfireLoop.FlagClearM.</summary>
        public const float FlagClearM = 150f;

        Vector3 centre, offset, half; Quaternion level; bool still, raids, hidden; float lag;
        Renderer[] parts = Array.Empty<Renderer>();
        Transform[] props = Array.Empty<Transform>(); Transform? shadow, flag; Terrain? ground; Vector2 shadowHalf; float flagHalf, seaY;
        DakotaBombRun? bombs;

        /// <summary>True when the aircraft holds the loop's start (edit mode, screenshots).</summary>
        public bool Still => still;

        /// <summary>True when it repeats the raid instead of flying its loop (SeaAirCollection.DakotaRaidsWanted, #3140).</summary>
        public bool Raids => raids;

        /// <summary>True while it is hidden, off its raid path (DakotaRaidFlight.OnPath): the aircraft, its propellers, its flag
        /// and its shadow. Its bombs and smoke show as they are.</summary>
        public bool Hidden => hidden;

        /// <summary>True when <see cref="PlacedAt"/> is the seconds into a raid (<see cref="PlaceRaid"/>), not on the loop's clock.</summary>
        public bool PlacedRaid { get; private set; }

        /// <summary>How far (s) this aircraft's clock is behind the loop's: 0 for the lead, SeaAirCollection.DakotaTrailSeconds
        /// for its wing.</summary>
        public float Lag => lag;

        /// <summary>The seconds where <see cref="Place"/> (on the loop's clock) or <see cref="PlaceRaid"/> (into a raid,
        /// <see cref="PlacedRaid"/>) last put it (the labels' keep-clear box follows it, SeaAirCollection.DakotaKeepBox).</summary>
        public float PlacedAt { get; private set; }

        /// <summary>The flag that floats over the tail, or null.</summary>
        public Transform? Flag => flag;

        /// <summary>Half the flag's height (m), its centre's rise over its lower edge (0 without a flag).</summary>
        public float FlagHalf => flagHalf;

        /// <summary>The aircraft's bombs, or null.</summary>
        internal DakotaBombRun? Bombs => bombs;

        /// <summary><paramref name="centre"/>: the loop's centre in the air. <paramref name="level"/>: the model's rotation with
        /// its nose east. <paramref name="offset"/>: its drawn centre from its pivot at that rotation. <paramref name="half"/>: half
        /// its box at that rotation (half its length, height and span). <paramref name="lag"/>: its clock behind the loop's. The
        /// propeller discs (children of its mesh), the shadow and the flag (beside it), the terrain the shadow lies on and the
        /// shadow's half size across and along the heading, the sea's surface (world y), its bombs, and whether it repeats the
        /// raid (<see cref="Raids"/>).</summary>
        internal void Configure(Vector3 centre, Quaternion level, Vector3 offset, Vector3 half, float lag, Transform[] props, Transform? shadow,
            Transform? flag, Terrain? ground, Vector2 shadowHalf, float seaY, DakotaBombRun? bombs, bool still, bool raids = false)
        {
            this.centre = centre; this.level = level; this.offset = offset; this.half = half; this.lag = lag; this.props = props;
            this.shadow = shadow; this.flag = flag; this.ground = ground; this.shadowHalf = shadowHalf; this.seaY = seaY; this.bombs = bombs; this.still = still;
            this.raids = raids;
            flagHalf = flag != null ? flag.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * flag.localScale.y : 0f;
            var own = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
            if (flag != null) own.AddRange(flag.GetComponentsInChildren<Renderer>(true));
            if (shadow != null) own.AddRange(shadow.GetComponentsInChildren<Renderer>(true));
            parts = own.ToArray();
            var cam = Camera.main;
            if (cam != null) bombs?.Face(cam.transform.rotation);
            // Where the clock puts it now: a component added mid-frame gets no Update that frame. A held raid holds both in sight,
            // the lead on its pass over Tel Aviv (DakotaRaidFlight.HoldS).
            if (!still) Fly();
            else if (raids) PlaceRaid(DakotaRaidFlight.HoldS);
            else Place(0f);
        }

        void Update()
        {
            if (!still) Fly();
        }

        /// <summary>Puts it where the clock has it now: on the first bombing period's turns, in the repeat's raid
        /// (DakotaRaidFlight.Cycle, #3140, which New game's raid starts); otherwise on New game's raid (#3000) while it flies,
        /// then on the loop's clock.</summary>
        internal void Fly()
        {
            if (raids) { float seconds = DakotaRaidFlight.Cycle(out int raid); PlaceRaid(seconds, raid); }
            else if (DakotaRaidFlight.Since(out float seconds)) PlaceRaid(seconds);
            else Place(SpitfireLoop.Clock());
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Face;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Face;

        /// <summary>Turns the flashes and the smoke to the camera about to render, as UnitGunFire.Face does the gun's.</summary>
        void Face(ScriptableRenderContext _, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            bombs?.Face(cam.transform.rotation);
        }

        /// <summary>Puts the aircraft where it is <paramref name="seconds"/> into the loop's clock (its own, <see cref="Lag"/>
        /// less): its drawn centre on the path, its nose along the path's tangent, rolled into the turn about that tangent; the
        /// propellers at their angle then (by the loop's clock, so a held shot holds them); the flag over its tail, its lower edge
        /// <see cref="FlagClearM"/> over the top of its box at this bank; the shadow where the sun's ray through it meets the
        /// ground; and its bombs, flashes and smoke as they are then.</summary>
        public void Place(float seconds)
        {
            PlacedAt = seconds; PlacedRaid = false;
            Show(true);
            float u = seconds - lag;
            var turn = SeaAirCollection.DakotaTurn(u, out var forward, out float bank);
            Put(SeaAirCollection.DakotaLoopOffset(u), turn, forward, bank, seconds, u);
        }

        /// <summary>Puts the aircraft where New game's raid has it <paramref name="seconds"/> after the raid started
        /// (DakotaRaidFlight, #3000), the repeat's raid number <paramref name="raid"/> (#3140): on its path at its raid time (the
        /// wing DakotaRaidFlight.TrailS behind the lead), as <see cref="Place"/> does on the loop, its bombs at that raid time,
        /// which is their loop time on the pass over Tel Aviv. Off its path (DakotaRaidFlight.OnPath) it is hidden and only its
        /// bombs and smoke move on. Its smoke columns count from the first raid's hits, so the city burns on from raid to raid.</summary>
        internal void PlaceRaid(float seconds, int raid = 0)
        {
            PlacedAt = seconds; PlacedRaid = true;
            int plane = SeaAirCollection.DakotaPlaneOf(lag);
            float u = DakotaRaidFlight.U(seconds, plane);
            double burning = u + (double)raid * DakotaRaidFlight.CycleS;
            bool on = DakotaRaidFlight.OnPath(seconds, plane);
            Show(on);
            if (!on) { bombs?.Sample(u, burning); return; }
            float tau = DakotaRaidFlight.PathTime(u);
            var turn = DakotaRaidFlight.Turn(tau, out var forward, out float bank);
            Put(DakotaRaidFlight.Offset(tau), turn, forward, bank, seconds, u, burning);
        }

        /// <summary>Shows or hides the aircraft, its propellers, its flag and its shadow (<see cref="Hidden"/>); nothing when
        /// that is already so.</summary>
        void Show(bool on)
        {
            if (hidden != on) return;
            hidden = !on;
            for (int i = 0; i < parts.Length; i++) if (parts[i] != null) parts[i].enabled = on;
        }

        /// <summary>Puts the aircraft at <paramref name="at"/> from its loop's centre, turned by <paramref name="turn"/> (heading
        /// <paramref name="forward"/>, <paramref name="bank"/> degrees), its propellers at their angle <paramref name="seconds"/>
        /// into the clock, and its bombs as they are at <paramref name="u"/> into its loop, its columns at
        /// <paramref name="burning"/> (u on the loop).</summary>
        void Put(Vector2 at, Quaternion turn, Vector3 forward, float bank, float seconds, float u, double? burning = null)
        {
            transform.localRotation = turn * level;
            var drawn = centre + new Vector3(at.x, 0f, at.y);
            transform.position = drawn - turn * offset;
            // The angle in double, as the Spitfire's (review of #2260: a float product with the clock loses whole degrees).
            var spin = Quaternion.AngleAxis(-(float)((double)seconds * SpitfireLoop.PropDegreesPerSecond % 360.0), Vector3.up);
            for (int i = 0; i < props.Length; i++) props[i].localRotation = spin;
            if (flag != null) flag.position = drawn - forward * half.x + Vector3.up * (SpitfireLoop.TopOver(half, bank) + FlagClearM + flagHalf);
            // Cut the sun's ray to convergence: the shadow falls up the rising ground east of Tel Aviv, where the Spitfire's two
            // fixed-point steps miss by about 200 m (SpitfireLoop.LayShadow).
            if (shadow != null && ground != null) SpitfireLoop.LayShadow(shadow, ground, seaY, shadowHalf, drawn, forward, bank, converge: true);
            bombs?.Sample(u, burning ?? u);
        }
    }

    /// <summary>One Dakota's bombs on Tel Aviv (#2482, option 1: "drops bombs with flashes and smoke on the city. Looks only; no
    /// change to the game's numbers."): its releases (<see cref="Drops"/>, found once by SeaAirCollection.DakotaBombs: every
    /// SeaAirCollection.DakotaBombIntervalS of the loop whose release and impact are both on Tel Aviv's hex), a small pool
    /// of bombs, flashes, blasts and dust puffs, as many as can show at once, and its share of the smoke columns. The whole
    /// run is a pure function of the aircraft's loop time (<see cref="Sample"/>), as UnitGunFire's shot is of its time, so a
    /// held shot repeats exactly. A bomb falls under the aircraft's track (its horizontal speed is the aircraft's), faster as
    /// it falls (its drop grows with the square of the time). At the impact the gun's flash (UnitGunFire.Flash, its texture,
    /// HexMaterials.AttackGlow) marks the instant, while the blast, Unity Labs' CC0 explosion flipbook (owner, 4 November
    /// 2026, after "the explosions look bad in the screenshot and there is no smoke columns coming up" and "it needs to look
    /// top quality"), swells from the ground, burns out into black smoke and fades; dust puffs (the smoke flipbook, the
    /// muzzle smoke's curves, UnitGunFire.Puff) rise and thin after it; and from the first hit on each of its column sites a
    /// smoke column rises and leans downwind for as long as the aircraft flies (SeaAirCollection.DakotaColumns). All of it is
    /// drawn on camera-facing quads (no lights, no particle system): a flipbook frame is a quad mesh whose UVs cover that
    /// frame (the URP particle shaders ignore the material's tiling), made once per run. Every renderer is made once, hidden
    /// until used, and its materials and meshes are owned by the run's object, so they go with the aircraft; sampling
    /// allocates nothing.</summary>
    internal sealed class DakotaBombRun
    {
        /// <summary>A release: <see cref="At"/> seconds into the aircraft's loop, from <see cref="From"/> (its belly) to
        /// <see cref="To"/> (the ground).</summary>
        internal readonly struct Drop
        {
            public readonly float At; public readonly Vector3 From, To;
            public Drop(float at, Vector3 from, Vector3 to) { At = at; From = from; To = to; }
        }

        /// <summary>The run's textures: the gun's flash, the blast flipbook (<see cref="BlastGrid"/> frames each way) and the
        /// smoke flipbook (<see cref="SmokeGrid"/> each way). A grid of 1 is a single picture.</summary>
        internal readonly struct Looks
        {
            public readonly Texture2D Flash, Blast, Smoke; public readonly int BlastGrid, SmokeGrid;
            public Looks(Texture2D flash, Texture2D blast, int blastGrid, Texture2D smoke, int smokeGrid)
            { Flash = flash; Blast = blast; BlastGrid = blastGrid; Smoke = smoke; SmokeGrid = smokeGrid; }
        }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseMap = Shader.PropertyToID("_BaseMap"), Cull = Shader.PropertyToID("_Cull");
        /// <summary>DESIGN VALUES: the flash the gun's colour; the dust a dusty grey-brown (a bomb's burst raises dust and
        /// rubble), thinning paler as it rises; the puffs drift a little east, each its own way. The blast keeps the
        /// flipbook's own fire and smoke colours, its smoke a touch warmer (dust). The smoke flipbook's grey averages 0.44
        /// where it is opaque (measured on its pixels), so a tint is about 2.3 times the shade drawn: the dust runs from
        /// about 0.27 to 0.42.</summary>
        static readonly Color FlashColour = new Color(1f, 0.85f, 0.5f), SmokeFresh = new Color(0.62f, 0.56f, 0.48f), SmokeThin = new Color(0.95f, 0.92f, 0.86f);
        static readonly Color BlastTint = new Color(1f, 0.95f, 0.88f);
        /// <summary>DESIGN VALUES (#2604, the columns read as "two flat, dark-brown blobs"): a column is near-black and
        /// brownish at its foot (burning rubble, about 0.13 drawn) and light grey aloft (about 0.68 drawn), the shade
        /// changing over the lower 70% of its height. The columns are drawn unlit, as the blast (the flipbook carries its own
        /// light and shade; #2490's lit columns came out uniformly dark), so the tint times the flipbook's 0.44 is the shade
        /// on screen; the top tint is over 1 for that reason (the shader multiplies, nothing clamps it before blending).</summary>
        internal static readonly Color ColumnFoot = new Color(0.30f, 0.26f, 0.22f), ColumnTop = new Color(1.55f, 1.53f, 1.50f);
        static readonly float[] PuffSide = { -0.07f, 0.03f, 0.08f }, PuffSpin = { 20f, 140f, 260f };
        /// <summary>DESIGN VALUES: a column puff's alpha at the foot and at the top of the column (#2604: "partly transparent
        /// at the top"). The puffs overlap more aloft, where they are wider, so the top's alpha is low: about five puffs
        /// overlap there, which still leaves it about half see-through. A newborn puff fades in over the first
        /// <see cref="ColumnFadeInShare"/> of the column's height (40 m), and an old one fades out over its top
        /// <see cref="ColumnFadeOutShare"/>.</summary>
        internal const float ColumnAlphaFoot = 0.95f, ColumnAlphaTop = 0.12f, ColumnFadeInShare = 0.01f, ColumnFadeOutShare = 0.3f;

        readonly List<Drop> drops;
        readonly int[] entry;   // the drop at each release slot of the loop, or -1
        readonly int slots;
        readonly Transform[] bombT, flashT, blastT, puffT, columnT;
        readonly Renderer[] bomb, flash, blast, puff, column;
        readonly MeshFilter[] blastF, puffF, columnF;
        readonly Material[] flashMat, blastMat, puffMat, columnMat;
        readonly Mesh[] blastFrames, smokeFrames;
        readonly Vector3[] columnAt;    // each column's foot (an impact point)
        readonly double[] columnFrom;   // the loop time of its first hit
        Quaternion view = Quaternion.identity;

        /// <summary>The releases, in loop order.</summary>
        internal IReadOnlyList<Drop> Drops => drops;
        /// <summary>The pool's renderers (tests): the bombs, the flashes, the blasts, the dust puffs and the column puffs
        /// (<see cref="ColumnSites"/> x SeaAirCollection.DakotaColumnPuffs, column by column).</summary>
        internal IReadOnlyList<Renderer> Bombs => bomb;
        internal IReadOnlyList<Renderer> Flashes => flash;
        internal IReadOnlyList<Renderer> Blasts => blast;
        internal IReadOnlyList<Renderer> Smoke => puff;
        internal IReadOnlyList<Renderer> Columns => column;
        /// <summary>Each of this aircraft's columns' foot and the loop time (s) of its first hit (tests).</summary>
        internal IReadOnlyList<Vector3> ColumnSites => columnAt;
        internal double ColumnFrom(int c) => columnFrom[c];
        /// <summary>The flipbook frame a blast or column renderer shows (tests): the index of its frame mesh.</summary>
        internal int BlastFrame(int s) => Array.IndexOf(blastFrames, blastF[s].sharedMesh);
        internal int ColumnFrame(int i) => Array.IndexOf(smokeFrames, columnF[i].sharedMesh);
        /// <summary>A pool renderer's own material colour now (tests).</summary>
        internal static Color Colour(Renderer r) => r.sharedMaterial.GetColor(BaseColor);

        /// <summary>The run of <paramref name="drops"/> among the loop's <paramref name="releases"/> release slots, drawn under
        /// <paramref name="parent"/>: the bombs with <paramref name="bombMesh"/> and <paramref name="bombMaterial"/> (shared), the
        /// flashes, blasts, puffs and columns with <paramref name="looks"/> (shared) on materials of their own, owned by
        /// <paramref name="parent"/>. Aircraft <paramref name="plane"/> of <paramref name="planes"/> takes every
        /// <paramref name="planes"/>-th of the pair's evenly spaced column sites, starting at its own index.</summary>
        internal DakotaBombRun(Transform parent, List<Drop> drops, int releases, Mesh bombMesh, Material bombMaterial, Looks looks, int plane, int planes)
        {
            this.drops = drops;
            entry = new int[releases];
            for (int i = 0; i < releases; i++) entry[i] = -1;
            for (int k = 0; k < drops.Count; k++) entry[Mathf.RoundToInt(drops[k].At / SeaAirCollection.DakotaBombIntervalS) % releases] = k;
            // Every release whose bomb, flash, blast or dust can still show: its fall, then the longer of the blast and the
            // last puff's start and life.
            float life = SeaAirCollection.DakotaBombFallS + Mathf.Max(SeaAirCollection.DakotaBlastS,
                (SeaAirCollection.DakotaPuffs - 1) * SeaAirCollection.DakotaPuffGapS + SeaAirCollection.DakotaSmokeS);
            slots = Mathf.CeilToInt(life / SeaAirCollection.DakotaBombIntervalS) + 1;
            var owned = parent.GetComponent<MapRuntimeAssets>();   // not ??: a missing component is Unity's fake null in the editor
            if (owned == null) owned = parent.gameObject.AddComponent<MapRuntimeAssets>();
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            blastFrames = Frames(owned, looks.BlastGrid); smokeFrames = Frames(owned, looks.SmokeGrid);
            Material Mat(string template, Texture2D texture)
            {
                var m = owned.Own(HexMaterials.New(template)); m.SetTexture(BaseMap, texture); m.SetFloat(Cull, (float)CullMode.Off); return m;
            }
            bombT = new Transform[slots]; bomb = new Renderer[slots]; flashT = new Transform[slots]; flash = new Renderer[slots]; flashMat = new Material[slots];
            blastT = new Transform[slots]; blast = new Renderer[slots]; blastF = new MeshFilter[slots]; blastMat = new Material[slots];
            int puffs = slots * SeaAirCollection.DakotaPuffs;
            puffT = new Transform[puffs]; puff = new Renderer[puffs]; puffF = new MeshFilter[puffs]; puffMat = new Material[puffs];
            for (int s = 0; s < slots; s++)
            {
                (bombT[s], bomb[s], _) = Part(parent, "Bomb", bombMesh, bombMaterial);
                bombT[s].localScale = SeaAirCollection.DakotaBombLengthM * Vector3.one;
                var fm = flashMat[s] = owned.Own(HexMaterials.New(HexMaterials.AttackGlow)); fm.SetTexture(BaseMap, looks.Flash);
                (flashT[s], flash[s], _) = Part(parent, "BombFlash", quad, fm);
                // The blast is unlit and alpha-blended (the border's material): its fire glows by its own colours, its smoke
                // stays dark, where an additive glow would wash out over the light sand.
                (blastT[s], blast[s], blastF[s]) = Part(parent, "BombBlast", blastFrames[0], blastMat[s] = Mat(HexMaterials.AttackBorder, looks.Blast));
                for (int p = 0; p < SeaAirCollection.DakotaPuffs; p++)
                {
                    int i = s * SeaAirCollection.DakotaPuffs + p;
                    (puffT[i], puff[i], puffF[i]) = Part(parent, "BombSmoke", smokeFrames[0], puffMat[i] = Mat(HexMaterials.AttackSmoke, looks.Smoke));
                }
            }
            (columnAt, columnFrom) = ColumnSitesOf(drops, releases * SeaAirCollection.DakotaBombIntervalS, plane, planes);
            int n = columnAt.Length * SeaAirCollection.DakotaColumnPuffs;
            columnT = new Transform[n]; column = new Renderer[n]; columnF = new MeshFilter[n]; columnMat = new Material[n];
            // The columns unlit, as the blast (ColumnFoot, ColumnTop): their shade is the tint times the flipbook's own.
            for (int i = 0; i < n; i++) (columnT[i], column[i], columnF[i]) = Part(parent, "BombColumn", smokeFrames[0], columnMat[i] = Mat(HexMaterials.AttackBorder, looks.Smoke));
        }

        /// <summary>Aircraft <paramref name="plane"/>'s column sites among <paramref name="drops"/> on a loop of
        /// <paramref name="period"/> seconds: the impacts in the order of the pass over Tel Aviv (each impact's time put in the
        /// half loop either side of the loop's start, where the pass is), the pair's SeaAirCollection.DakotaColumns x
        /// <paramref name="planes"/> (or every impact, if fewer) evenly spaced along it, and of those every
        /// <paramref name="planes"/>-th from <paramref name="plane"/>. Each site's first hit is its impact's time in that
        /// pass (negative for the impacts before the loop's start: an editor shot, held at the start, shows those columns rising).</summary>
        static (Vector3[] at, double[] from) ColumnSitesOf(List<Drop> drops, float period, int plane, int planes)
        {
            int n = drops.Count;
            var hit = new double[n]; var order = new int[n];
            for (int k = 0; k < n; k++)
            {
                double t = drops[k].At + SeaAirCollection.DakotaBombFallS;
                hit[k] = t - period * Math.Floor(t / period + 0.5); order[k] = k;
            }
            Array.Sort(order, (a, b) => hit[a].CompareTo(hit[b]));
            int total = Mathf.Min(SeaAirCollection.DakotaColumns * planes, n);
            var at = new List<Vector3>(); var from = new List<double>();
            for (int i = plane; i < total; i += planes)
            {
                int k = order[total == 1 ? 0 : Mathf.RoundToInt(i * (n - 1f) / (total - 1f))];
                at.Add(drops[k].To); from.Add(hit[k]);
            }
            return (at.ToArray(), from.ToArray());
        }

        /// <summary>One quad mesh per frame of a <paramref name="grid"/> x <paramref name="grid"/> flipbook, owned by the run's
        /// object (Flipbook.Frames, shared with the combat effects).</summary>
        internal static Mesh[] Frames(MapRuntimeAssets owned, int grid) => Flipbook.Frames(grid, owned.Own, "dakota-frame");

        static (Transform, Renderer, MeshFilter) Part(Transform parent, string name, Mesh mesh, Material material) => Flipbook.Part(parent, name, mesh, material);

        static int Mod(int a, int n) { int m = a % n; return m < 0 ? m + n : m; }

        /// <summary>A pseudo-random share in [0, 1) for <paramref name="seed"/>, as HexUnderAttackLook's per-puff seed.</summary>
        static float Own(float seed) => HexUnderAttack.Frac(Mathf.Sin(seed * 12.9898f) * 43758.547f);

        /// <summary>DESIGN VALUE: a column puff's turn (degrees) for its share <paramref name="own"/>: within 25 degrees either
        /// way, so the flipbook's sunlit side stays roughly up.</summary>
        static float Spin(float own) => (own - 0.5f) * 50f;

        /// <summary>The run as it is <paramref name="u"/> seconds into the aircraft's loop: each slot shows the latest release that
        /// maps to it (release j at j x the interval, the loop's slot j mod its releases), if that slot drops a bomb; each
        /// column as it is <paramref name="burning"/> (u, unless given: a repeated raid's columns count from the first raid,
        /// DakotaLoop.PlaceRaid) less its first hit. The same times give the same frame. No allocation.</summary>
        internal void Sample(float u, double? burning = null)
        {
            float every = SeaAirCollection.DakotaBombIntervalS;
            int newest = (int)Math.Floor((double)u / every);
            for (int k = 0; k < slots; k++)
            {
                int j = newest - k;
                int d = entry[Mod(j, entry.Length)];
                Show(Mod(j, slots), d < 0 ? (Drop?)null : drops[d], (float)((double)u - (double)j * every));
            }
            double at = burning ?? u;
            for (int c = 0; c < columnAt.Length; c++) ShowColumn(c, at - columnFrom[c]);
        }

        void Show(int s, Drop? drop, float age)
        {
            float fall = SeaAirCollection.DakotaBombFallS, after = age - fall;
            bool falling = drop.HasValue && age >= 0f && age < fall;
            bomb[s].enabled = falling;
            if (falling)
            {
                var d = drop!.Value; float f = age / fall, drop2 = d.From.y - d.To.y;
                var at = Vector3.Lerp(d.From, d.To, f); at.y = d.From.y - drop2 * f * f;
                var velocity = new Vector3((d.To.x - d.From.x) / fall, -2f * drop2 * f / fall, (d.To.z - d.From.z) / fall);
                bombT[s].SetPositionAndRotation(at, Quaternion.FromToRotation(Vector3.up, velocity));
            }
            bool flashing = drop.HasValue && after >= 0f && after < SeaAirCollection.DakotaFlashS;
            flash[s].enabled = flashing;
            if (flashing)
            {
                var d = drop!.Value;
                float grow = 0.7f + 0.3f * Mathf.Clamp01(after / (0.25f * SeaAirCollection.DakotaFlashS));
                flashT[s].SetPositionAndRotation(d.To + Vector3.up * (0.3f * SeaAirCollection.DakotaFlashM), view * Quaternion.Euler(0f, 0f, 37f));
                flashT[s].localScale = Vector3.one * (SeaAirCollection.DakotaFlashM * grow);
                var c = FlashColour; c.a = UnitGunFire.Flash(after * UnitGunFire.FlashS / SeaAirCollection.DakotaFlashS); flashMat[s].SetColor(BaseColor, c);
            }
            bool blasting = drop.HasValue && after >= 0f && after < SeaAirCollection.DakotaBlastS;
            blast[s].enabled = blasting;
            if (blasting)
            {
                float b = after / SeaAirCollection.DakotaBlastS;
                blastF[s].sharedMesh = blastFrames[Mathf.Min((int)(b * blastFrames.Length), blastFrames.Length - 1)];
                float size = SeaAirCollection.DakotaBlastM * BlastSwell(b);
                // Its foot on the ground: the flipbook's cloud fills about the middle four fifths of its frame.
                blastT[s].SetPositionAndRotation(drop!.Value.To + Vector3.up * (0.4f * size), view);
                blastT[s].localScale = Vector3.one * size;
                var c = BlastTint; c.a = BlastAlpha(b); blastMat[s].SetColor(BaseColor, c);
            }
            for (int p = 0; p < SeaAirCollection.DakotaPuffs; p++)
            {
                int i = s * SeaAirCollection.DakotaPuffs + p;
                float v = (after - p * SeaAirCollection.DakotaPuffGapS) / SeaAirCollection.DakotaSmokeS;
                bool on = drop.HasValue && v > 0f && v < 1f;
                puff[i].enabled = on;
                if (!on) continue;
                UnitGunFire.Puff(v, out _, out float rise, out float radius, out float alpha);
                float size = SeaAirCollection.DakotaSmokeM;
                puffF[i].sharedMesh = smokeFrames[Mathf.Min((int)(v * smokeFrames.Length), smokeFrames.Length - 1)];
                puffT[i].SetPositionAndRotation(drop!.Value.To + Vector3.up * (rise * size) + Vector3.right * (PuffSide[p] * v * size),
                    view * Quaternion.Euler(0f, 0f, PuffSpin[p]));
                puffT[i].localScale = Vector3.one * (2.6f * radius * size);
                var c = Color.Lerp(SmokeFresh, SmokeThin, v); c.a = alpha; puffMat[i].SetColor(BaseColor, c);
            }
        }

        /// <summary>The blast's size over SeaAirCollection.DakotaBlastM at <paramref name="b"/> of its life: from
        /// SeaAirCollection.DakotaBlastOpenShare, swelling fast (easing out) to the full size by a quarter of its life, then
        /// spreading a little more as its smoke drifts.</summary>
        internal static float BlastSwell(float b)
        {
            float open = 1f - Mathf.Clamp01(b / 0.25f);
            return Mathf.Lerp(SeaAirCollection.DakotaBlastOpenShare, 1f, 1f - open * open) + 0.12f * Mathf.Clamp01((b - 0.25f) / 0.75f);
        }

        /// <summary>The blast's alpha at <paramref name="b"/> of its life: whole, then fading over the last
        /// SeaAirCollection.DakotaBlastFadeShare of it to nothing at its end.</summary>
        internal static float BlastAlpha(float b)
        {
            float fade = SeaAirCollection.DakotaBlastFadeShare;
            return 1f - Mathf.SmoothStep(0f, 1f, (b - (1f - fade)) / fade);
        }

        /// <summary>A column puff's height share (0 at the impact, 1 at the column's top) at <paramref name="v"/> of its life:
        /// v to the power SeaAirCollection.DakotaColumnRisePower, so a puff leaves the impact slowly and speeds up as it rises.</summary>
        internal static float ColumnHeightShare(float v) => Mathf.Pow(Mathf.Clamp01(v), SeaAirCollection.DakotaColumnRisePower);

        /// <summary>A column puff's width (m) at height share <paramref name="h"/> (<see cref="ColumnHeightShare"/>), before
        /// its own jitter: from SeaAirCollection.DakotaColumnFootM at the impact, widening steadily to
        /// SeaAirCollection.DakotaColumnTopM at the top, as a plume spreads (#2604: "start thin at each
        /// impact, widen as they rise"). #2490 widened with the square root, so the column was nearly full width a quarter of the way up.</summary>
        internal static float ColumnWidth(float h) => Mathf.Lerp(SeaAirCollection.DakotaColumnFootM, SeaAirCollection.DakotaColumnTopM, h);

        /// <summary>A column puff's alpha at height share <paramref name="h"/>: fading in over <see cref="ColumnFadeInShare"/>,
        /// then falling with height from <see cref="ColumnAlphaFoot"/> toward <see cref="ColumnAlphaTop"/> (fast at first: the
        /// foot stays dense, the upper half thin), and fading out to nothing over the last <see cref="ColumnFadeOutShare"/>,
        /// so the top dissolves. Above the fade-in it only falls.</summary>
        internal static float ColumnAlphaAt(float h)
        {
            float fadeIn = Mathf.SmoothStep(0f, 1f, h / ColumnFadeInShare);
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, (h - (1f - ColumnFadeOutShare)) / ColumnFadeOutShare);
            return fadeIn * Mathf.Lerp(ColumnAlphaFoot, ColumnAlphaTop, Mathf.Pow(Mathf.Clamp01(h), 0.6f)) * fadeOut;
        }

        /// <summary>A column puff's tint (alpha aside) at height share <paramref name="h"/>: <see cref="ColumnFoot"/> at the
        /// impact to <see cref="ColumnTop"/> by 70% of the way up.</summary>
        internal static Color ColumnTint(float h) => Color.Lerp(ColumnFoot, ColumnTop, Mathf.SmoothStep(0f, 1f, h / 0.7f));

        /// <summary>Column <paramref name="c"/>, <paramref name="age"/> seconds after its first hit (hidden before it): a stream
        /// of SeaAirCollection.DakotaColumnPuffs puffs, each born every SeaAirCollection.DakotaColumnLifeS, a puff's share of
        /// a life apart, and only those born since the first hit, so the column grows from the ground over its first life and
        /// then stands. A puff rises SeaAirCollection.DakotaColumnM over its life, slowly at first
        /// (<see cref="ColumnHeightShare"/>: the puffs crowd at the foot, so the thin stem stands unbroken on the impact, and
        /// spread aloft, where they are wide), widening (<see cref="ColumnWidth"/>), leaning downwind, its
        /// shade (<see cref="ColumnTint"/>) and alpha (<see cref="ColumnAlphaAt"/>) going from dark and dense at the foot to
        /// light grey and thin aloft, its smoke boiling through the flipbook.</summary>
        void ShowColumn(int c, double age)
        {
            int P = SeaAirCollection.DakotaColumnPuffs;
            float life = SeaAirCollection.DakotaColumnLifeS;
            for (int i = 0; i < P; i++)
            {
                int k = c * P + i;
                double lives = age / life - (double)i / P;
                float v = (float)(lives - Math.Floor(lives));
                bool on = age >= 0.0 && v * life <= age;
                column[k].enabled = on;
                if (!on) continue;
                float own = Own(c * 7.31f + i * 1.93f + 0.5f), h = ColumnHeightShare(v);
                float size = ColumnWidth(h) * (0.85f + 0.3f * own);
                var wobble = new Vector3(Mathf.Sin(own * 19f + v * 5f), 0f, Mathf.Cos(own * 13f + v * 4f)) * (60f * Mathf.Sqrt(h));
                // Its centre 0.45 of its size up: tilted to the board camera (50 degrees down), its lower edge stays over the
                // ground, so the column's foot stands on the impact. It leans downwind more the higher it is.
                var at = columnAt[c] + Vector3.up * (0.45f * size + h * SeaAirCollection.DakotaColumnM)
                    + HexUnderAttack.Smoke.Wind * (SeaAirCollection.DakotaColumnLeanM * Mathf.Pow(h, 1.3f)) + wobble;
                float spin = Spin(own);
                columnT[k].SetPositionAndRotation(at, view * Quaternion.Euler(0f, 0f, spin));
                columnT[k].localScale = Vector3.one * size;
                int frames = smokeFrames.Length;
                columnF[k].sharedMesh = smokeFrames[Mod((int)Math.Floor(((double)v * life * SeaAirCollection.DakotaColumnFps) + own * frames), frames)];
                var colour = ColumnTint(h); colour.a = ColumnAlphaAt(h);
                columnMat[k].SetColor(BaseColor, colour);
            }
        }

        /// <summary>Turns the flashes, blasts and puffs showing to a camera with rotation <paramref name="camera"/>, and keeps it
        /// for the next samples (edit mode and shots face the main camera from the start).</summary>
        internal void Face(Quaternion camera)
        {
            view = camera;
            for (int s = 0; s < flash.Length; s++) if (flash[s].enabled) flashT[s].rotation = view * Quaternion.Euler(0f, 0f, 37f);
            for (int s = 0; s < blast.Length; s++) if (blast[s].enabled) blastT[s].rotation = view;
            for (int i = 0; i < puff.Length; i++) if (puff[i].enabled) puffT[i].rotation = view * Quaternion.Euler(0f, 0f, PuffSpin[i % SeaAirCollection.DakotaPuffs]);
            int P = SeaAirCollection.DakotaColumnPuffs;
            for (int i = 0; i < column.Length; i++)
                if (column[i].enabled) columnT[i].rotation = view * Quaternion.Euler(0f, 0f, Spin(Own(i / P * 7.31f + i % P * 1.93f + 0.5f)));
        }
    }
}
