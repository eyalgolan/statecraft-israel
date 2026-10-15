#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The four procedural archetypes of the settlement art spike (issues #31, #32).</summary>
    public enum Archetype { WhiteCity, OldTown, ArabTown, Moshava }

    /// <summary>Procedural settlement kits at the owner's symbolic scale (ruling 2026-10-11): buildings about
    /// <see cref="KitScale"/> times their real size (per class), a town's footprint spread over neighbouring hexes in
    /// proportion to its population, geography real (towns stay on land, at their sourced coordinates). Built at load time
    /// from the settlement id's seed (<see cref="KitRandom"/>), so every load of a build draws the same towns; nothing here reads or
    /// writes the sim.
    /// Towns round 2 (docs/visual-v1.md, "Towns round 2"): every town is laid out on plot lattices (streets of plots at the
    /// town's grid angle; organic towns jittered), each plot built with a probability that falls from the core to the edge,
    /// instead of random spots tried against every lot. The landmarks of the four cities live in SettlementKits.Landmarks.cs.</summary>
    public static partial class SettlementKits
    {
        /// <summary>Settled population per hex of footprint: a town of this many people covers one hex (about 65 km^2).</summary>
        public const float PeoplePerHex = 60000f;
        /// <summary>The smallest footprint, in hexes, so a kibbutz of 240 still reads as a place at the gameplay zoom.</summary>
        public const float MinHexes = 0.08f;
        /// <summary>How far past its footprint radius a town's plots reach (UnitGround.FootprintMargin: the units assume no
        /// building beyond it; PaintGround tints the same disc).</summary>
        public const float PlotReach = 1.1f;

        public static Archetype ArchetypeOf(Settlement s)
        {
            switch (s.Id)
            {
                case "tel_aviv": case "haifa": return Archetype.WhiteCity;
                case "jaffa": case "acre": case "gaza": case "jerusalem": return Archetype.OldTown;
            }
            switch (s.Type)
            {
                case "city": return Archetype.WhiteCity;
                case "arab_town": case "village": return Archetype.ArabTown;
                case "town": return s.Id == "tiberias" || s.Id == "safed" ? Archetype.ArabTown : Archetype.Moshava;
                default: return Archetype.Moshava;   // kibbutz, moshav
            }
        }

        /// <summary>Footprint radius (m) of a settlement: the radius of a disc of (population / PeoplePerHex) hexes.</summary>
        public static float FootprintRadiusM(Settlement s, float hexSizeM)
        {
            float hexArea = 1.5f * 1.7320508f * hexSizeM * hexSizeM;   // a pointy hex of circumradius hexSizeM
            float hexes = Mathf.Max(MinHexes, KitPopulation.Of(s.Id) / PeoplePerHex);
            return Mathf.Sqrt(hexes * hexArea / Mathf.PI);
        }

        /// <summary>Claims every settlement's footprint on <paramref name="ground"/> (largest first, so a big town's disc is
        /// whole and its small neighbour fits around it) and returns one mesh per settlement, keyed by id.</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground) =>
            Build(list, ground, out _);

        /// <summary>As Build, also returning every claimed lot (centre, radius, owning settlement id): buildings and landmarks
        /// of all towns, none overlapping another (tests); and, when <paramref name="columns"/> is given, filling it with each
        /// non-empty kit's columns by settlement id (KitMesh.Columns, read before the mesh's CPU copy is dropped).</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground,
            out List<(Vector2 centre, float radius, string id)> claimed, Dictionary<string, Bounds[]>? columns = null) =>
            Build(list, ground, out claimed, out _, columns);

        /// <summary>One thing a kit placed, for the tests' and the docs' town census: a building (<see cref="Placed.Kind"/>
        /// "building", or "wing" for a block's lower part), a street palm ("palm": its crown's square, its height) or a
        /// landmark (its name: "mosque", "clock_tower", ...), with its
        /// world footprint (centre, yaw in degrees, width and depth in m), its wall height (m) and wall colour.</summary>
        public readonly struct Placed
        {
            public readonly string Id, Kind; public readonly Vector2 C; public readonly float Yaw, W, D, H; public readonly Color32 Wall;
            public Placed(string id, string kind, Vector2 c, float yaw, float w, float d, float h, Color32 wall)
            { Id = id; Kind = kind; C = c; Yaw = yaw; W = w; D = d; H = h; Wall = wall; }
            /// <summary>Harbour works stand in the sea by design; everything else stands on land.</summary>
            public bool InSea => Kind == "port" || Kind == "breakwater";
        }

        [System.ThreadStatic] static List<Placed>? census;
        static void Record(string id, string kind, Vector2 c, float yaw, float w, float d, float h, Color32 wall) =>
            census?.Add(new Placed(id, kind, c, yaw, w, d, h, wall));

        /// <summary>As Build, also listing everything placed (<see cref="Placed"/>), in placement order.</summary>
        public static List<(Settlement s, Mesh mesh, int buildings)> Build(IReadOnlyList<Settlement> list, KitGround ground,
            out List<(Vector2 centre, float radius, string id)> claimed, out List<Placed> placed, Dictionary<string, Bounds[]>? columns = null)
        {
            var all = Build(list, new List<Settlement>(), new List<Localities.Area>(), ground, out placed, columns);
            claimed = all.Claimed;
            return all.Towns;
        }

        /// <summary>As the full Build, also listing everything placed (<see cref="Placed"/>), in placement order.</summary>
        public static Result Build(IReadOnlyList<Settlement> towns, IReadOnlyList<Settlement> localities, IReadOnlyList<Localities.Area> camps,
            KitGround ground, out List<Placed> placed, Dictionary<string, Bounds[]>? columns = null)
        {
            census = placed = new List<Placed>();
            try { return Build(towns, localities, camps, ground, columns); }
            finally { census = null; }
        }

        /// <summary>What <see cref="Build(IReadOnlyList{Settlement}, IReadOnlyList{Settlement}, IReadOnlyList{Localities.Area}, KitGround)"/>
        /// made: one mesh per game settlement, and the 1948 localities and Bedouin camps merged into one mesh per
        /// <see cref="TileM"/> tile (a few dozen draws for some three hundred places).</summary>
        public sealed class Result
        {
            public List<(Settlement s, Mesh mesh, int buildings)> Towns = new List<(Settlement, Mesh, int)>();
            public List<(string name, Mesh mesh)> Tiles = new List<(string, Mesh)>();
            public List<(Vector2 centre, float radius, string id)> Claimed = new List<(Vector2, float, string)>();
            public int LocalityBuildings, Camps, Tents;
            /// <summary>Landmarks each game settlement built (SettlementKits.Landmarks.cs: a city's own, else its archetype's).</summary>
            public Dictionary<string, int> TownLandmarks = new Dictionary<string, int>();
        }

        /// <summary>Side of the square tiles the localities' and camps' buildings are merged into (the trees' tile size).</summary>
        public const float TileM = VegetationLayer.TileM;

        /// <summary>Builds the game settlements (<paramref name="towns"/>) and the 1948 localities (<paramref name="localities"/>)
        /// together on one lot list, largest first by population, then scatters the Bedouin camps of <paramref name="camps"/>
        /// into the ground left free. Seeded: the same inputs draw the same map.</summary>
        public static Result Build(IReadOnlyList<Settlement> towns, IReadOnlyList<Settlement> localities, IReadOnlyList<Localities.Area> camps,
            KitGround ground, Dictionary<string, Bounds[]>? columns = null)
        {
            var merged = new HashSet<string>();
            foreach (var s in localities) merged.Add(s.Id);
            var order = new List<Settlement>(towns); order.AddRange(localities);
            order.Sort((a, b) => KitPopulation.Of(b.Id).CompareTo(KitPopulation.Of(a.Id)) != 0
                ? KitPopulation.Of(b.Id).CompareTo(KitPopulation.Of(a.Id)) : string.CompareOrdinal(a.Id, b.Id));
            var centres = new Dictionary<string, Vector2>();
            foreach (var s in order)
            {
                var w = ground.Frame.ToWorld(s.Lon, s.Lat, 0);
                centres[s.Id] = new Vector2(w.x, w.z);
                ground.Footprints.Add((centres[s.Id], FootprintRadiusM(s, ground.HexSizeM), s.Id));
            }
            var result = new Result();
            var tiles = new SortedDictionary<(int, int), KitMesh>();
            KitMesh Tile(Vector2 p)
            {
                var key = (Mathf.FloorToInt(p.x / TileM), Mathf.FloorToInt(p.y / TileM));
                if (!tiles.TryGetValue(key, out var km)) tiles[key] = km = new KitMesh();
                return km;
            }
            // One lot list for every town: a big town's buildings are placed first, and a neighbour's never overlap them.
            var lots = new Lots();
            var streams = new Dictionary<string, KitRandom>(); var landmarks = new Dictionary<string, int>();
            var spans = new Dictionary<string, List<(KitMesh km, int from, int to)>>();   // each locality's vertices in its tile
            var townMeshes = new Dictionary<string, KitMesh>();
            var gameTowns = new List<(Settlement s, KitMesh km, int n, System.Func<int>? scatter, System.Func<int>? palms)>();
            // Landmarks first: every game settlement's (largest first), then every locality's, each at its heart, before any
            // houses. A village at a city's edge (Jammasin by Tel Aviv) keeps its heart, and no village takes a game town's
            // landmark spot (review M8).
            foreach (var pass in new[] { false, true })
                foreach (var s in order)
                {
                    if (merged.Contains(s.Id) != pass) continue;
                    var rnd = new KitRandom(s.Id, 1);
                    var km = pass ? Tile(centres[s.Id]) : (townMeshes[s.Id] = new KitMesh());
                    int from = km.VertexCount;
                    // A game settlement's own landmarks (towns round 2: TownLandmarks), a locality's its archetype's.
                    int n = pass ? BuildOne(s, centres[s.Id], ground, km, lots, Phase.Landmark, ref rnd, 0, null, PackedIn(s, towns, centres, ground))
                        : TownLandmarks(s, centres[s.Id], ground, km, lots, ref rnd);
                    if (pass) { spans[s.Id] = new List<(KitMesh, int, int)> { (km, from, km.VertexCount) }; result.LocalityBuildings += n; }
                    else result.TownLandmarks[s.Id] = n;
                    landmarks[s.Id] = n; streams[s.Id] = rnd;
                }
            // A village's houses keep out of every smaller game settlement's plot reach, plus the lot of its largest
            // fallback house (S1): that settlement places its houses, and its small-town fallback (#317), after the
            // village, and the 25 towns keep the ground they had (Yad Mordechai beside Hiribya).
            var townReach = new List<(Vector2 c, float r, int pop)>();
            foreach (var t in towns)
                townReach.Add((centres[t.Id], FootprintRadiusM(t, ground.HexSizeM) * PlotReach + 0.5f * Mathf.Sqrt(12f * 12f + 9f * 9f) * KitScale.For(ArchetypeOf(t)),
                    KitPopulation.Of(t.Id)));
            System.Func<Vector2, bool> SmallerTowns(Settlement loc)
            {
                int pop = KitPopulation.Of(loc.Id);
                return q =>
                {
                    foreach (var (c, r, tp) in townReach) if (tp < pop && (q - c).sqrMagnitude < r * r) return true;
                    return false;
                };
            }
            // Then the houses, largest place first. A game settlement's houses ignore the localities' cells (their landmarks'
            // lots still stand): the 25 towns keep the ground they had before the villages came, and a village next to one
            // (Fajja by Petah Tikva) fills in around its houses.
            foreach (var s in order)
            {
                var rnd = streams[s.Id];
                if (merged.Contains(s.Id))
                {
                    var tile = Tile(centres[s.Id]); int from = tile.VertexCount;
                    // An Arab village or town (S1): a dense pack of small stone houses on the village lattice, as Lod's and
                    // Ramla's. The Jewish localities and mixed Nesher keep their scatter.
                    result.LocalityBuildings += ArchetypeOf(s) == Archetype.ArabTown && !Localities.IsMixed(s.Id)
                        ? PlaceHouses(s, centres[s.Id], ground, tile, lots, ref rnd, null, out _, out _, locality: true, keepOut: SmallerTowns(s))
                        : BuildOne(s, centres[s.Id], ground, tile, lots, Phase.Houses, ref rnd, landmarks[s.Id], null);
                    spans[s.Id].Add((tile, from, tile.VertexCount));
                    continue;
                }
                var km = townMeshes[s.Id];
                int n = landmarks[s.Id] + PlaceHouses(s, centres[s.Id], ground, km, lots, ref rnd, merged, out var scatter, out var palms);   // towns round 2: plot lattices
                gameTowns.Add((s, km, n, scatter, palms));
            }
            foreach (var area in camps)
            {
                var (c, t) = Encampments(area, ground, lots, Tile);
                result.Camps += c; result.Tents += t;
            }
            // Last, the small towns' fallback houses (#317), largest place first: after every place's own houses and the camps,
            // so they take only ground no other place builds on, and every other place draws as it would without them.
            // Before them, Tel Aviv's street palms (SettlementKits.TelAviv.cs), on ground no place's house or camp took, so they
            // move none of those; the fallback then still moves nothing else (#317).
            foreach (var t in gameTowns) t.palms?.Invoke();
            var totals = new List<int>();
            foreach (var t in gameTowns) totals.Add(t.n + (t.scatter?.Invoke() ?? 0));
            for (int i = 0; i < gameTowns.Count; i++)
            {
                var (s, km, _, _, _) = gameTowns[i];
                if (columns != null && km.VertexCount > 0) columns[s.Id] = km.Columns();
                result.Towns.Add((s, km.ToMesh(s.Id + "_kit"), totals[i]));
            }
            // A locality's columns, from its own vertices in its tile (read before ToMesh drops the CPU copy).
            if (columns != null)
                foreach (var kv in spans)
                {
                    var pts = new List<Vector3>();
                    foreach (var (km, from, to) in kv.Value) km.Positions(from, to, pts);
                    if (pts.Count > 0) columns[kv.Key] = KitMesh.Columns(pts);
                }
            foreach (var kv in tiles) result.Tiles.Add(($"Localities {kv.Key.Item1},{kv.Key.Item2}", kv.Value.ToMesh($"localities_{kv.Key.Item1}_{kv.Key.Item2}")));
            result.Claimed = lots.List.ConvertAll(l => (centre: l.c, radius: l.r, id: l.id));
            return result;
        }

        /// <summary>Tints the ground of each settlement's footprint in the baked biome colour texels (sRGB, row 0 = south):
        /// pale paving and sand in the towns, as in promo 03, fading out over the outer half of the footprint. Review I6 (boot
        /// budget, ~300 places): each row is clipped to the circle, the inner half (a constant weight) skips the square root
        /// and the smoothstep, and the blend is in integers. Same weights as before, to within a 1/256 step.</summary>
        public static void PaintGround(Color32[] px, int res, GeoFrame frame, IReadOnlyList<Settlement> list, float hexSizeM)
        {
            float sx = frame.WidthM / res, sz = frame.LengthM / res;
            foreach (var s in list)
            {
                var arch = ArchetypeOf(s);
                var (sandF, strength) = arch switch
                {
                    Archetype.WhiteCity => (new Color(0.80f, 0.75f, 0.64f), 0.62f),
                    Archetype.OldTown => (new Color(0.78f, 0.68f, 0.52f), 0.62f),
                    Archetype.ArabTown => (new Color(0.74f, 0.64f, 0.48f), 0.5f),
                    _ => (new Color(0.70f, 0.63f, 0.47f), 0.32f),
                };
                Color32 sand = sandF;
                var c = frame.ToWorld(s.Lon, s.Lat, 0);
                float R = FootprintRadiusM(s, hexSizeM) * 1.1f, R2 = R * R, inner2 = 0.25f * R2;
                int full = Mathf.RoundToInt(strength * 256f);
                int y0 = Mathf.Max(0, Mathf.FloorToInt((c.z - R) / sz)), y1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.z + R) / sz));
                for (int y = y0; y <= y1; y++)
                {
                    float wz = (y + 0.5f) * sz - c.z, rest = R2 - wz * wz;
                    if (rest <= 0f) continue;
                    float half = Mathf.Sqrt(rest);   // the circle's half-width on this row
                    int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - half) / sx)), x1 = Mathf.Min(res - 1, Mathf.CeilToInt((c.x + half) / sx));
                    int row = y * res;
                    for (int x = x0; x <= x1; x++)
                    {
                        float wx = (x + 0.5f) * sx - c.x, d2 = wx * wx + wz * wz;
                        if (d2 >= R2) continue;
                        int w = d2 <= inner2 ? full
                            : Mathf.RoundToInt(256f * strength * (1f - Mathf.SmoothStep(0f, 1f, (Mathf.Sqrt(d2) / R - 0.5f) * 2f)));
                        if (w <= 0) continue;
                        ref var p = ref px[row + x];
                        p.r = (byte)(p.r + (((sand.r - p.r) * w) >> 8)); p.g = (byte)(p.g + (((sand.g - p.g) * w) >> 8));
                        p.b = (byte)(p.b + (((sand.b - p.b) * w) >> 8));
                    }
                }
            }
        }

        /// <summary>A claimed lot: its disc, its town and, for a plot, its lattice (<see cref="group"/>; -1 for a landmark).
        /// Plots of one lattice never overlap by construction, so they do not test each other's discs.</summary>
        sealed class Lot { public Vector2 c; public float r; public string id = ""; public int group = -1; }

        /// <summary>The shared lot list, in claim order, with a grid index (DiscIndex, as KitGround's) for the free-spot test.</summary>
        sealed class Lots
        {
            public readonly List<Lot> List = new List<Lot>();
            readonly DiscIndex index = new DiscIndex(1f, KitGround.LotQueryM);
            public void Add(Lot l) { List.Add(l); index.Add((l.c, l.r, l.id)); }

            /// <summary>No lot within reach of a disc of radius r at p, except the plots of <paramref name="id"/>'s lattice
            /// <paramref name="group"/> (pass group -1 to test against every lot).</summary>
            public bool Free(Vector2 p, float r, string id = "", int group = -1)
            {
                if (r > KitGround.LotQueryM)
                {
                    foreach (var l in List) if (!Same(l, id, group) && (l.c - p).sqrMagnitude < (l.r + r) * (l.r + r)) return false;
                    return true;
                }
                var near = index.Near(p);
                for (int k = 0; k < near.Count; k++)
                {
                    var l = List[near[k]];
                    if (!Same(l, id, group) && (l.c - p).sqrMagnitude < (l.r + r) * (l.r + r)) return false;
                }
                return true;
            }

            static bool Same(Lot l, string id, int group) => group >= 0 && l.group == group && l.id == id;
        }

        static bool FreeAt(Lots lots, Vector2 p, float r) => lots.Free(p, r);

        /// <summary>A landmark's spot (review N2): the dry spot nearest <paramref name="want"/> (rings out to four radii) that no
        /// lot of any town already holds; claimed for <paramref name="owner"/>. False, and nothing claimed, when there is none:
        /// the landmark is then left out rather than overlapping a bigger neighbour.</summary>
        static bool Claim(Lots lots, Vector2 want, float r, string owner, KitGround ground, out Vector2 at)
        {
            for (int ring = 0; ring <= 8; ring++)
            {
                int around = ring == 0 ? 1 : 8 * ring;
                for (int k = 0; k < around; k++)
                {
                    float a = 2f * Mathf.PI * k / around, d = ring * r * 0.5f;
                    var p = want + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                    if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !FreeAt(lots, p, r) || ground.OnRoute(p, r)) continue;
                    lots.Add(new Lot { c = p, r = r, id = owner }); at = p; return true;
                }
            }
            at = want; return false;
        }

        // ---- Plots ----------------------------------------------------------------------------------------------------

        /// <summary>The kinds of building a plot lattice lays out. Real sizes (m, before the symbolic scale) and storeys are
        /// in <see cref="Pick"/>; lattice pitch, density and scale in <see cref="Grid"/>.</summary>
        enum Style
        {
            /// <summary>Hadar HaCarmel's International Style blocks: white plaster, flat roofs (60x).</summary>
            WhiteBlock,
            /// <summary>Tel Aviv's White City blocks (60x; SettlementKits.TelAviv.cs, visual gap S2).</summary>
            TelAviv,
            /// <summary>The plastered two- and three-storey centre of a large moshava (Petah Tikva, Rehovot) (60x).</summary>
            TownBlock,
            /// <summary>Jerusalem's new city: stone-faced blocks, Storrs' 1918 ordinance (60x).</summary>
            StoneBlock,
            /// <summary>An Ottoman old town's stone house: Jaffa, Acre, Gaza, Jerusalem within the walls, Haifa's lower town (100x).</summary>
            OldHouse,
            /// <summary>An Arab town's or village's stone or mud-brick house (100x).</summary>
            Village,
            /// <summary>A moshava's, kibbutz's or moshav's red-roofed house (100x).</summary>
            Moshava,
            /// <summary>A Templer house of Haifa's German Colony: stone, red roof (100x; placed along its avenue, not on a lattice).</summary>
            Templer,
        }

        /// <summary>A plot lattice: rows of plots at its style's pitch (<see cref="Grid"/>), turned <see cref="Yaw"/> degrees;
        /// <see cref="Group"/> tells a town's lattices apart (plots of one lattice never overlap, so they skip each other's lot test).</summary>
        readonly struct Lattice
        {
            public readonly Style Style; public readonly float Yaw; public readonly int Group;
            public Lattice(Style style, float yaw, int group) { Style = style; Yaw = yaw; Group = group; }
        }

        /// <summary>Plot pitch along and across the street (real m), jitter (share of the pitch), the symbolic scale, and
        /// the share of plots built in the core (out to t0 of the radius), at the radius, and past it (falling to none at
        /// <see cref="PlotReach"/>).</summary>
        static (float pu, float pv, float jitter, float scale, float core, float t0, float edge) Grid(Style st) => st switch
        {
            Style.WhiteBlock => (18.5f, 13.5f, 0f, KitScale.CityBlocks, 1.00f, 0.60f, 0.70f),
            Style.TelAviv => (TelAvivPitchU, TelAvivPitchV, 0f, KitScale.CityBlocks, 1.00f, 0.60f, TelAvivEdge),
            Style.TownBlock => (20f, 16f, 0.03f, KitScale.CityBlocks, 1.00f, 0.30f, 0.80f),
            Style.StoneBlock => (23f, 17.5f, 0.03f, KitScale.CityBlocks, 0.97f, 0.50f, 0.60f),
            Style.OldHouse => (7.2f, 6.8f, 0.05f, KitScale.Houses, 1.00f, 0.60f, 0.75f),
            // S1: a compact pack of small houses, lanes of about two metres between them, built solid to three quarters of
            // the radius (houses "clustered around the yards ... in a compact and convergent manner": Abuarkub 2018). The pitch
            // leaves a lane when two full-size neighbours' jitter and yaw meet (TownCensusTests.NoTwoHousesOfATownOverlap).
            Style.Village => (6.6f, 6.2f, 0.04f, KitScale.Houses, 1.00f, 0.75f, 0.85f),
            _ => (11.5f, 9.8f, 0.04f, KitScale.Houses, 0.95f, 0.40f, 0.60f),
        };

        static float Density(Style st, float t)
        {
            var g = Grid(st);
            if (t <= g.t0) return g.core;
            if (t <= 1f) return Mathf.Lerp(g.core, g.edge, (t - g.t0) / (1f - g.t0));
            return Mathf.Lerp(g.edge, 0f, (t - 1f) / (PlotReach - 1f));
        }

        /// <summary>The lattices a town is laid out on: one per archetype, two or three where a town has quarters of different
        /// building (Jerusalem: the walled Old City, the stone new city, the Arab quarters and villages round it; Haifa: the
        /// lower town by the port and Hadar on the slope; a large moshava's plastered centre).</summary>
        static List<Lattice> Lattices(Settlement s, Archetype arch)
        {
            float yaw = GridYaw(s.Id);
            switch (s.Id)
            {
                case "jerusalem": return new List<Lattice> { new Lattice(Style.OldHouse, 0f, 0), new Lattice(Style.StoneBlock, yaw, 1), new Lattice(Style.Village, yaw, 2) };
                case "haifa": return new List<Lattice> { new Lattice(Style.WhiteBlock, yaw, 0), new Lattice(Style.OldHouse, yaw, 1) };
                case "tel_aviv": return new List<Lattice> { new Lattice(Style.TelAviv, yaw, 0) };
            }
            switch (arch)
            {
                case Archetype.WhiteCity: return new List<Lattice> { new Lattice(Style.WhiteBlock, yaw, 0) };
                case Archetype.OldTown: return new List<Lattice> { new Lattice(Style.OldHouse, yaw, 0) };
                case Archetype.ArabTown: return new List<Lattice> { new Lattice(Style.Village, yaw, 0) };
                default:
                    return s.Type == "town" && KitPopulation.Of(s.Id) >= LargeMoshava
                        ? new List<Lattice> { new Lattice(Style.TownBlock, yaw, 0), new Lattice(Style.Moshava, yaw, 1) }
                        : new List<Lattice> { new Lattice(Style.Moshava, yaw, 0) };
            }
        }

        /// <summary>A moshava of at least this many people (1945) gets a plastered town centre (Petah Tikva, Rehovot, Rishon
        /// LeZion, Hadera).</summary>
        const int LargeMoshava = 7500;

        /// <summary>Whether lattice <paramref name="L"/> builds at <paramref name="p"/> (t: distance over the footprint radius).</summary>
        static bool Allows(Settlement s, Lattice L, Vector2 p, float t, KitGround ground)
        {
            switch (s.Id)
            {
                case "jerusalem":
                {
                    var (oc, half) = OldCity(ground);
                    var d = p - oc;
                    bool inside = Mathf.Abs(d.x) < half - OldCityInset && Mathf.Abs(d.y) < half - OldCityInset;
                    bool outside = Mathf.Abs(d.x) > half + OldCityInset || Mathf.Abs(d.y) > half + OldCityInset;
                    // West of the Old City: the Jewish new city (the city centre, Rehavia); north, east and south: the Arab
                    // quarters and villages. (The Old City's symbolic square covers the ground of its nearest neighbourhoods.)
                    bool west = d.x < -half * 0.6f;
                    return L.Style == Style.OldHouse ? inside : outside && (L.Style == Style.StoneBlock) == west;
                }
                case "haifa":
                    // The lower town by the bay (old Arab Haifa, below about 35 m) and Hadar HaCarmel up the slope.
                    return (L.Style == Style.OldHouse) == (ground.ElevM(p.x, p.y) < 35f);
                case "nahalal":
                    // Nahalal's ring of farmsteads round the public buildings, out to the footprint's edge: its lattice's
                    // first ring of plots stands at 0.76 and 0.89 of the radius.
                    return t > 0.42f && t <= 1f;
            }
            if (L.Style == Style.TownBlock) return t < 0.42f;
            if (L.Style == Style.Moshava && L.Group == 1) return t > 0.36f;
            return true;
        }

        /// <summary>Places a town's houses on its lattices, nearest the centre first; returns how many. When the lattice
        /// places fewer than <see cref="SmallTownFloor"/>, <paramref name="scatter"/> is the town's fallback (#317), which
        /// Build runs after every other place's houses; else null. <paramref name="palms"/> is Tel Aviv's street palms
        /// (SettlementKits.TelAviv.cs), which Build runs after every place's houses and the camps; else null. A 1948
        /// <paramref name="locality"/> (an Arab village or town, S1) keeps its houses off the grid's edge midpoints, as its
        /// scatter did (review M10), and out of <paramref name="keepOut"/> (the smaller game settlements' ground), keeps no
        /// beach setback, and fills up to its own floor (<see cref="VillageFloor"/>) at once instead of the deferred fallback.</summary>
        static int PlaceHouses(Settlement s, Vector2 centre, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd, HashSet<string>? ignoreCellsOf,
            out System.Func<int>? scatter, out System.Func<int>? palms, bool locality = false, System.Func<Vector2, bool>? keepOut = null)
        {
            var arch = ArchetypeOf(s);
            float R = FootprintRadiusM(s, ground.HexSizeM), reach = R * PlotReach;
            var lattices = Lattices(s, arch);

            // A point belongs to the settlement whose footprint it is deepest inside (distance over radius), so Jaffa and Tel
            // Aviv, or Lod and Ramla, meet along a seam instead of overlapping. The 1948 localities' cells (ignoreCellsOf) do
            // not count: a game settlement keeps the ground it had before the villages came (their landmarks' lots still stand).
            bool Mine(Vector2 p)
            {
                float mine = (p - centre).magnitude / R;
                var fp = ground.Footprints;
                var near = fp.Near(p);
                for (int k = 0; k < near.Count; k++)
                {
                    int i = near[k];
                    if (fp[i].id != s.Id && (ignoreCellsOf == null || !ignoreCellsOf.Contains(fp[i].id))
                        && (p - fp[i].centre).magnitude / fp[i].radius < mine) return false;
                }
                return true;
            }

            // Only a town with water near its footprint tests the beach setback per plot (eight samples each: the boot budget).
            // A 1948 village keeps none: the coast's villages (Tantura, az-Zeeb) met the shore, as their scatter drew them.
            bool shore = !locality && WaterWithin(centre, reach + BeachSetbackM + 1000f, ground);
            var plots = new List<(float d, int order, Vector2 p, int lattice)>();
            for (int li = 0; li < lattices.Count; li++)
            {
                var g = Grid(lattices[li].Style);
                float pu = g.pu * g.scale, pv = g.pv * g.scale, a = lattices[li].Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                int nu = Mathf.CeilToInt(reach / pu) + 1, nv = Mathf.CeilToInt(reach / pv) + 1;
                for (int j = -nv; j <= nv; j++)
                    for (int i = -nu; i <= nu; i++)
                    {
                        // Organic towns stagger alternate rows by half a plot: no straight cross streets.
                        float u = (i + (g.jitter > 0f && (j & 1) != 0 ? 0.5f : 0f)) * pu, v = j * pv;
                        var p = centre + new Vector2(u * ca - v * sa, u * sa + v * ca);
                        float d = (p - centre).magnitude;
                        if (d <= reach) plots.Add((d, plots.Count, p, li));
                    }
            }
            plots.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : x.order.CompareTo(y.order));

            // Whether lattice L's building fits at p: the full building, else a smaller one on the plot or nudged within it,
            // so a plot by a road or a neighbour's lot keeps a smaller building instead of standing empty. A nudged, shrunk
            // building still stays inside its plot, so it never reaches a neighbouring plot's building. group -1 tests
            // against every lot, this town's plots included (the scatter below).
            bool Fit(Lattice L, Vector2 p, float yaw, float t, Spec spec, int group, out Spec b, out Vector2 q)
            {
                var g = Grid(L.Style);
                float a = L.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                bool hillTown = L.Style == Style.OldHouse || L.Style == Style.Village;
                foreach (var (shrink, du, dv) in Fits)
                {
                    b = Shrink(spec, shrink);
                    float u = du * g.pu * g.scale, v = dv * g.pv * g.scale;
                    q = p + new Vector2(u * ca - v * sa, u * sa + v * ca);
                    if ((du != 0f || dv != 0f) && (!ground.Inside(q.x, q.y) || !Mine(q) || !Allows(s, L, q, t, ground) || ground.Water(q.x, q.y))) continue;
                    if (!lots.Free(q, b.lotR, s.Id, group) || ground.OnRoute(q, b.lotR)) continue;   // off the railways and roads too
                    if (locality && (ground.OnGridMark(q, b.lotR) || (keepOut != null && keepOut(q)))) continue;
                    if (ground.Slope(q.x, q.y, b.w) > (hillTown ? 0.6f : 0.35f)) continue;
                    if (CornerInWater(q, yaw, b.w, b.d, ground)) continue;   // the whole footprint is dry
                    if (shore && L.Style != Style.OldHouse && NearSea(q, yaw, b.w + 2f * BeachSetbackM, b.d + 2f * BeachSetbackM, ground)) continue;
                    return true;
                }
                b = spec; q = p; return false;
            }

            int placed = 0;
            var blocks = new List<(Vector2 c, float hw, float hd)>();   // Tel Aviv's blocks in its lattice's frame, for its street palms
            foreach (var (d, _, p0, li) in plots)
            {
                var L = lattices[li]; var g = Grid(L.Style);
                float t = d / R;
                // Every plot draws the same numbers whether or not it is built, so one plot's fate never shifts another's.
                bool build = rnd.Chance(Density(L.Style, t));
                float ju = rnd.Range(-1f, 1f) * g.jitter * g.pu * g.scale, jv = rnd.Range(-1f, 1f) * g.jitter * g.pv * g.scale;
                var spec = Pick(L.Style, t, ref rnd, g.scale);
                // Inside Jerusalem's walls, one or two storeys: the walls and the Dome of the Rock stand above the roofs.
                if (s.Id == "jerusalem" && L.Style == Style.OldHouse && spec.storeys > 2) { spec.storeys = 2; spec.h = 2f * 3.3f * g.scale; }
                float yaw = L.Yaw + (g.jitter > 0f ? rnd.Range(-4f, 4f) : 0f);
                ulong detail = rnd.Next();
                if (!build) continue;
                float a = L.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                var p = p0 + new Vector2(ju * ca - jv * sa, ju * sa + jv * ca);
                // The cheap tests first (the boot budget): the cell and the quarter, then the terrain samples.
                if (!ground.Inside(p.x, p.y) || !Mine(p) || !Allows(s, L, p, t, ground) || ground.Water(p.x, p.y)) continue;
                if (!Fit(L, p, yaw, t, spec, L.Group, out var b, out var q)) continue;
                lots.Add(new Lot { c = q, r = b.lotR, id = s.Id, group = L.Group });
                var drnd = new KitRandom(detail);
                Emit(s.Id, L.Style, q, yaw, b, t, ground, km, ref drnd);
                placed++;
                if (L.Style == Style.TelAviv)
                {
                    var dq = q - centre;
                    blocks.Add((new Vector2(dq.x * ca + dq.y * sa, -dq.x * sa + dq.y * ca), 0.5f * b.w, 0.5f * b.d + BalconyM * g.scale));
                }
            }
            // Tel Aviv's street palms: Build runs them after every place's houses and the camps.
            palms = blocks.Count == 0 ? null : () => StreetPalms(s, centre, R, lattices[0].Yaw, blocks, ground, km, lots, Mine, shore);

            // A 1948 village's floor (S1): where the routes, a bigger neighbour's lots or the cell's seam take most of its
            // plots, small village houses on free spots over its plot reach, nearest its heart first (KitRandom(id, 3)), at
            // full size and then 0.72, until it has VillageFloor houses.
            if (locality && placed < VillageFloor)
            {
                var srnd = new KitRandom(s.Id, 3);
                float m = KitScale.Houses;
                var spots = new List<(float r, int i, Vector2 p, float yaw, Spec b, ulong detail)>();
                for (int i = 0; i < ScatterTries; i++)
                {
                    float r = reach * Mathf.Sqrt(srnd.Value()), ang = srnd.Range(0f, 2f * Mathf.PI);
                    var b = Pick(Style.Village, r / R, ref srnd, m);
                    spots.Add((r, i, centre + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r, lattices[0].Yaw + srnd.Range(-4f, 4f), b, srnd.Next()));
                }
                spots.Sort((x, y) => x.r != y.r ? x.r.CompareTo(y.r) : x.i.CompareTo(y.i));
                foreach (var shrink in new[] { 1f, 0.72f })
                    for (int k = 0; k < spots.Count && placed < VillageFloor; k++)
                    {
                        var (r, _, p, yaw, full, detail) = spots[k];
                        var b = Shrink(full, shrink);
                        if (!ground.Inside(p.x, p.y) || !Mine(p) || ground.Water(p.x, p.y)) continue;
                        if (!lots.Free(p, b.lotR) || ground.OnRoute(p, b.lotR) || ground.OnGridMark(p, b.lotR) || (keepOut != null && keepOut(p))) continue;
                        if (ground.Slope(p.x, p.y, b.w) > 0.6f || CornerInWater(p, yaw, b.w, b.d, ground)) continue;
                        lots.Add(new Lot { c = p, r = b.lotR, id = s.Id });
                        var drnd = new KitRandom(detail);
                        Emit(s.Id, Style.Village, p, yaw, b, r / R, ground, km, ref drnd);
                        placed++;
                    }
            }

            // A small town's floor (#317): at the houses' 100x scale a plot is about 1 km, so a footprint of 1.3-2 km radius
            // holds only some 7 plots, and the routes, the sea or a neighbour's lots can take most of them.
            // Below SmallTownFloor houses the town is filled up as round 1 drew it: its archetype's full-size building (the
            // size main drew, never shrunk), on random spots over the footprint tried nearest the heart first, from its own
            // stream (KitRandom(id, 3)). Build runs it after every place's own houses and the camps, so it takes only ground
            // no other place builds on.
            scatter = null;
            if (placed < SmallTownFloor && SmallTownScatter && !locality)
            {
                int want = SmallTownFloor - placed;
                scatter = () =>
                {
                    var srnd = new KitRandom(s.Id, 3);
                    float m = KitScale.For(arch);
                    var spots = new List<(float r, int i, Vector2 p, float yaw, Spec b, ulong detail)>();
                    for (int i = 0; i < ScatterTries; i++)
                    {
                        float r = reach * Mathf.Sqrt(srnd.Value()), ang = srnd.Range(0f, 2f * Mathf.PI);
                        var b = Pick(arch, r / R, ref srnd, m);
                        b.lotR = 0.5f * Mathf.Sqrt(b.w * b.w + b.d * b.d);   // the whole footprint, as a plot's (round 1's lot let a corner reach a road)
                        float yaw = arch == Archetype.Moshava || arch == Archetype.WhiteCity ? GridYaw(s.Id) + srnd.Range(-4f, 4f) : srnd.Range(0f, 90f);
                        spots.Add((r, i, centre + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r, yaw, b, srnd.Next()));
                    }
                    spots.Sort((x, y) => x.r != y.r ? x.r.CompareTo(y.r) : x.i.CompareTo(y.i));
                    // The tests that do not depend on the house's size, once per spot (the boot budget): the cell, the water, the
                    // quarter's rule (Nahalal's ring).
                    var site = new bool[spots.Count];
                    for (int k = 0; k < spots.Count; k++)
                    {
                        var (r, _, p, _, _, _) = spots[k]; float t = r / R;
                        site[k] = ground.Inside(p.x, p.y) && Mine(p) && !ground.Water(p.x, p.y) && lattices.Exists(x => Allows(s, x, p, t, ground));
                    }
                    int added = 0;
                    bool hillTown = arch == Archetype.ArabTown || arch == Archetype.OldTown;
                    // Full size first over every spot; only then 0.72 size (never the half-size fit: Acre's stray beach house).
                    foreach (var shrink in new[] { 1f, 0.72f })
                    for (int k = 0; k < spots.Count && added < want; k++)
                    {
                        if (!site[k]) continue;
                        var (r, _, p, yaw, full, detail) = spots[k];
                        var b = Shrink(full, shrink);
                        if (!lots.Free(p, b.lotR) || ground.OnRoute(p, b.lotR)) continue;
                        if (ground.Slope(p.x, p.y, b.w) > (hillTown ? 0.6f : 0.35f)) continue;
                        if (CornerInWater(p, yaw, b.w, b.d, ground)) continue;
                        // Every fallback house keeps off the beach strip, an old town's too (Acre's stood alone on the sand).
                        if (shore && NearSea(p, yaw, b.w + 2f * BeachSetbackM, b.d + 2f * BeachSetbackM, ground)) continue;
                        lots.Add(new Lot { c = p, r = b.lotR, id = s.Id });
                        var drnd = new KitRandom(detail);
                        var wall = Emit(arch, p, yaw, b, ground, km, ref drnd);
                        Record(s.Id, "building", p, yaw, b.w, b.d, b.h, wall);
                        added++;
                    }
                    return added;
                };
            }
            return placed;
        }

        /// <summary>The fewest houses a town keeps (#317): round 1's floor per town (TargetCount's minimum of 6). Fifteen of the
        /// 25 towns' lattices fall short of it (the moshavot and kibbutzim, Tiberias, Netanya, Beersheba and Acre); their
        /// fallback houses join the lattice's where free ground is left (13 of them).</summary>
        const int SmallTownFloor = 6;
        /// <summary>The fewest houses a 1948 Arab village or town keeps (S1): the lattice's median village draws about a dozen.</summary>
        const int VillageFloor = 10;
        /// <summary>Random spots a small town's fallback tries before it settles for fewer than SmallTownFloor houses (round 1
        /// tried about 30 per building).</summary>
        const int ScatterTries = 240;
        /// <summary>Whether the small towns' fallback runs (on; tests switch it off to show it moves no other place).</summary>
        public static bool SmallTownScatter = true;

        /// <summary>The fits a plot tries in turn: (size share, nudge along and across the street as a share of the pitch).</summary>
        static readonly (float shrink, float du, float dv)[] Fits =
        {
            (1f, 0f, 0f), (0.72f, 0f, 0f), (0.72f, 0.15f, 0f), (0.72f, -0.15f, 0f), (0.72f, 0f, 0.15f), (0.72f, 0f, -0.15f), (0.5f, 0f, 0f),
        };

        static Spec Shrink(Spec b, float k) =>
            k >= 1f ? b : new Spec { w = b.w * k, d = b.d * k, h = b.h, storeys = b.storeys, lotR = b.lotR * k };


        /// <summary>How far (m) a new town's buildings keep back from the sea: the beach strip (promo-sea's ScBeachM, 650 m on
        /// average) stays sand, with its palms, as in promo 03's Tel Aviv. The old towns (Jaffa, Acre, Gaza, Haifa's lower
        /// town by the port) meet the water.</summary>
        const float BeachSetbackM = 700f;

        /// <summary>Water that is not a lake: the beach strip is the Mediterranean's. The Sea of Galilee's and the Hula's towns
        /// (Tiberias, Degania) meet the water, as the old towns do.</summary>
        static bool Sea(float x, float z, KitGround ground) => ground.Water(x, z) && !(ground.Inside(x, z) && ground.Biome(x, z) == "lake");

        /// <summary>Any sea on 24 rays from <paramref name="c"/>, sampled every kilometre out to <paramref name="radius"/>: whether a
        /// town's plots need the beach setback test at all.</summary>
        static bool WaterWithin(Vector2 c, float radius, KitGround ground)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (float d = 1000f; d <= radius; d += 1000f)
                {
                    var p = c + dir * d;
                    if (Sea(p.x, p.y, ground)) return true;
                }
            }
            return false;
        }

        /// <summary>Any sea (not a lake) at the corners or edge midpoints of a w x d rectangle at <paramref name="p"/>.</summary>
        static bool NearSea(Vector2 p, float yawDeg, float w, float d, KitGround ground)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            foreach (var (u, v) in new[] { (-0.5f * w, -0.5f * d), (0.5f * w, -0.5f * d), (-0.5f * w, 0.5f * d), (0.5f * w, 0.5f * d),
                         (0.5f * w, 0f), (-0.5f * w, 0f), (0f, 0.5f * d), (0f, -0.5f * d) })
                if (Sea(p.x + u * ca - v * sa, p.y + u * sa + v * ca, ground)) return true;
            return false;
        }

        static bool CornerInWater(Vector2 p, float yawDeg, float w, float d, KitGround ground)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            for (int i = 0; i < 4; i++)
            {
                float u = ((i & 1) == 0 ? -0.5f : 0.5f) * w, v = ((i & 2) == 0 ? -0.5f : 0.5f) * d;
                if (ground.Water(p.x + u * ca - v * sa, p.y + u * sa + v * ca)) return true;
            }
            return false;
        }

        /// <summary>The street grid's angle per town (Tel Aviv's Geddes plan runs about 20 degrees off north, parallel to the
        /// shore; the others get a seeded angle).</summary>
        static float GridYaw(string id) => id == "tel_aviv" ? 20f : id == "haifa" ? 35f : new KitRandom(id, 7).Range(0f, 90f);

        struct Spec { public float w, d, h, lotR; public int storeys; }

        /// <summary>A plot's building: real size and storeys by style and by t (0 at the heart, 1 at the footprint's edge),
        /// times the scale <paramref name="m"/>. Every size fits its lattice's plot with a street to spare.</summary>
        static Spec Pick(Style st, float t, ref KitRandom rnd, float m)
        {
            float w, d; int storeys;
            switch (st)
            {
                case Style.TelAviv: return PickTelAviv(t, ref rnd, m);
                case Style.WhiteBlock:
                    w = rnd.Range(12f, 16f); d = rnd.Range(9f, 11.5f);
                    // Taller near the centre, a few taller still: promo 03's five- to eight-storey skyline, above the real White
                    // City's three or four storeys (owner ruling 2026-10-10: where the promo departs from the era, the promo wins).
                    storeys = Mathf.RoundToInt(Mathf.Lerp(7f, 3f, Mathf.Clamp01(t)) + rnd.Range(-1.5f, 1.5f));
                    if (t < 0.5f && rnd.Chance(0.1f)) storeys = rnd.Range(7, 9);
                    storeys = Mathf.Clamp(storeys, 2, 8);   // promo 03's tallest block is eight storeys; nothing reads as a later tower
                    break;
                case Style.TownBlock:
                    w = rnd.Range(12f, 17f); d = rnd.Range(9f, 13f);
                    storeys = rnd.Chance(Mathf.Lerp(0.6f, 0.2f, t / 0.42f)) ? 3 : 2;
                    break;
                case Style.StoneBlock:
                    w = rnd.Range(13f, 20f); d = rnd.Range(10f, 14.5f);
                    storeys = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3.6f, 2f, Mathf.Clamp01(t)) + rnd.Range(-0.8f, 0.8f)), 2, 4);
                    break;
                case Style.OldHouse:
                    w = rnd.Range(4.6f, 6.6f); d = rnd.Range(4.4f, 6.2f);
                    storeys = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3.2f, 1.3f, Mathf.Clamp01(t)) + rnd.Range(-0.8f, 0.8f)), 1, 4);
                    break;
                case Style.Village:
                    // S1: the village's one-room house, smaller than a town's (the one-room house of the villages "ranges from
                    // three meters in small houses" up: Abuarkub 2018; was 4.4-6.6 x 4.2-6.0 m).
                    w = rnd.Range(3.8f, 5.4f); d = rnd.Range(3.6f, 5.0f);
                    // More two-storey houses in the core (was 45% falling to 10%): the layered heart promo 03's Qalqilya shows.
                    storeys = rnd.Chance(Mathf.Lerp(0.6f, 0.15f, Mathf.Clamp01(t))) ? 2 : 1;
                    break;
                case Style.Templer:
                    w = rnd.Range(8.5f, 10.5f); d = rnd.Range(7.5f, 9f);
                    storeys = 2;
                    break;
                default:
                    w = rnd.Range(7.5f, 10f); d = rnd.Range(6f, 7.8f);
                    storeys = rnd.Chance(0.15f) ? 2 : 1;
                    break;
            }
            // A village house's lot also covers the terrace it may stand on (Emit, Style.Village).
            float pad = st == Style.Village ? 2f * TerraceMarginM : 0f;
            return new Spec { w = w * m, d = d * m, h = storeys * 3.3f * m, storeys = storeys, lotR = 0.5f * Mathf.Sqrt((w + pad) * (w + pad) + (d + pad) * (d + pad)) * m };
        }

        // sRGB palettes (promo 03: white and cream city, honey stone Jaffa, tan villages, terracotta roofs).
        static Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
        static Color32 Jitter(Color32 c, ref KitRandom rnd, float amount)
        {
            float k = 1f + rnd.Range(-amount, amount), warm = rnd.Range(-amount, amount) * 0.5f;
            return C(Mathf.Clamp01(c.r / 255f * k * (1 + warm)), Mathf.Clamp01(c.g / 255f * k), Mathf.Clamp01(c.b / 255f * k * (1 - warm)));
        }

        /// <summary>Draws one plot's building and records it in the census.</summary>
        static void Emit(string id, Style st, Vector2 p, float yaw, Spec b, float t, KitGround ground, KitMesh km, ref KitRandom rnd)
        {
            float m = Grid(st).scale;
            var (bottom, top) = GroundSpan(p, yaw, b.w, b.d, ground, m);
            float storey = 3.3f * m;
            switch (st)
            {
                case Style.TelAviv: TelAvivBlock(id, p, yaw, b, bottom, top, km, ref rnd); break;
                case Style.WhiteBlock:
                case Style.TownBlock:
                {
                    bool white = st == Style.WhiteBlock;
                    // The White City's lime-white plaster (settlements quick wins, #31: promo 03's Tel Aviv reads white, not cream; a
                    // few blocks keep the cream render). The moshava centres keep their warmer plaster.
                    var wall = white
                        ? (rnd.Chance(0.08f) ? Jitter(C(0.93f, 0.90f, 0.83f), ref rnd, 0.03f) : Jitter(WhitePlaster, ref rnd, 0.025f))
                        : rnd.Chance(0.3f) ? Jitter(C(0.90f, 0.84f, 0.72f), ref rnd, 0.04f) : Jitter(C(0.94f, 0.92f, 0.87f), ref rnd, 0.035f);
                    var roof = white ? Jitter(C(0.90f, 0.89f, 0.86f), ref rnd, 0.03f) : Jitter(C(0.80f, 0.78f, 0.74f), ref rnd, 0.05f);
                    // A third of the blocks step down at one end: a lower wing within the plot (L and stepped massing).
                    bool stepped = b.storeys >= 3 && rnd.Chance(0.33f);
                    if (stepped)
                    {
                        float a = yaw * Mathf.Deg2Rad, share = rnd.Range(0.58f, 0.72f), side = rnd.Chance(0.5f) ? 1f : -1f;
                        float w1 = b.w * share, w2 = b.w - w1, d2 = b.d * rnd.Range(0.7f, 1f), h2 = b.h - storey * rnd.Range(1, 3);
                        float u1 = side * (b.w - w1) * 0.5f, u2 = -side * (b.w - w2) * 0.5f, v2 = (b.d - d2) * 0.5f;
                        var c1 = p + new Vector2(u1 * Mathf.Cos(a), u1 * Mathf.Sin(a));
                        var c2 = p + new Vector2(u2 * Mathf.Cos(a) - v2 * Mathf.Sin(a), u2 * Mathf.Sin(a) + v2 * Mathf.Cos(a));
                        km.Box(c1.x, c1.y, yaw, w1, b.d, bottom, top, b.h, wall, roof, Facade.WhiteCity, m);
                        km.Box(c2.x, c2.y, yaw, w2 * 1.01f, d2, bottom, top, h2, wall, roof, Facade.WhiteCity, m);
                        if (white) WhiteBlockDetail(km, c1, u1, b.lotR, yaw, w1, b.d, top, b.h, wall, roof, m, ref rnd);
                        Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                        Record(id, "wing", c2, yaw, w2, d2, h2, wall);
                    }
                    else
                    {
                        km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.WhiteCity, m);
                        if (white) WhiteBlockDetail(km, p, 0f, b.lotR, yaw, b.w, b.d, top, b.h, wall, roof, m, ref rnd);
                        Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    }
                    // A stair tower or roof room on many Bauhaus blocks (the White City's roofs: most have one).
                    if (rnd.Chance(white ? 0.8f : 0.5f))
                    {
                        float rw = b.w * rnd.Range(0.22f, 0.36f), rd = b.d * rnd.Range(0.3f, 0.45f);
                        km.Box(p.x + b.w * rnd.Range(-0.15f, 0.15f), p.y + b.d * rnd.Range(-0.12f, 0.12f), yaw, rw, rd, top + b.h - 2f, top + b.h, 2.8f * m, wall, roof, Facade.Plain, m);
                    }
                    // Many White City roofs carry a second room (the shared laundry room beside the stair head), at the
                    // block's other end, inside its footprint (not on a stepped block, whose ends differ in height).
                    if (white && !stepped && rnd.Chance(0.45f))
                    {
                        float rw = b.w * rnd.Range(0.16f, 0.24f), rd = b.d * rnd.Range(0.25f, 0.35f), a = yaw * Mathf.Deg2Rad;
                        float u = (rnd.Chance(0.5f) ? 1f : -1f) * (0.5f * b.w - 0.6f * rw), v = rnd.Range(-0.2f, 0.2f) * b.d;
                        km.Box(p.x + u * Mathf.Cos(a) - v * Mathf.Sin(a), p.y + u * Mathf.Sin(a) + v * Mathf.Cos(a), yaw, rw, rd, top + b.h - 2f, top + b.h,
                            2.4f * m, wall, roof, Facade.Plain, m);
                    }
                    break;
                }
                case Style.StoneBlock:
                {
                    var wall = Jitter(C(0.86f, 0.79f, 0.64f), ref rnd, 0.05f);
                    var roof = Jitter(C(0.82f, 0.77f, 0.66f), ref rnd, 0.04f);
                    bool tiled = rnd.Chance(0.15f);   // the red-roofed villas of the new city's garden suburbs
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: tiled);
                    if (tiled) km.Pyramid(p.x, p.y, yaw, b.w * 1.04f, b.d * 1.04f, top + b.h, 2.6f * m, Jitter(C(0.70f, 0.33f, 0.22f), ref rnd, 0.06f));
                    else if (rnd.Chance(0.35f))
                        km.Box(p.x, p.y, yaw, b.w * 0.3f, b.d * 0.35f, top + b.h - 2f, top + b.h, 2.6f * m, wall, roof, Facade.Plain, m);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                case Style.OldHouse:
                {
                    var wall = StoneWall(ref rnd);
                    var roof = Jitter(LimeRoof, ref rnd, 0.04f);
                    // Outside the core, some Ottoman-era houses have red tiled roofs (Jaffa's Ajami, Haifa's lower town).
                    bool tiled = rnd.Chance(t > 0.6f ? 0.14f : 0.03f);
                    if (tiled)
                    {
                        km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: true);
                        km.Pyramid(p.x, p.y, yaw, b.w * 1.04f, b.d * 1.04f, top + b.h, 2.2f * m, Jitter(C(0.70f, 0.33f, 0.22f), ref rnd, 0.06f));
                    }
                    else StoneHouse(km, p, yaw, b, bottom, top, wall, roof, Facade.Stone, m, rnd.Chance(OldHouseDome), ref rnd);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                case Style.Village:
                {
                    bool hill = ground.ElevM(p.x, p.y) >= HillVillageM;
                    var wall = hill ? StoneWall(ref rnd) : Jitter(MudBrick, ref rnd, 0.06f);
                    var roof = Jitter(LimeRoof, ref rnd, 0.05f);
                    bool dome = hill && rnd.Chance(VillageDome);
                    // S1: a hill house on a slope stands on its terrace, a dry-stone retaining wall up to the house's
                    // uphill ground with a flat yard round the house, so a village on a hillside steps down in terraces.
                    if (hill && top - bottom > TerraceRiseM * m)
                        km.Box(p.x, p.y, yaw, b.w + 2f * TerraceMarginM * m, b.d + 2f * TerraceMarginM * m, bottom, top, 0.15f * m,
                            Jitter(TerraceStone, ref rnd, 0.05f), Jitter(TerraceYard, ref rnd, 0.04f), Facade.Plain, m);
                    StoneHouse(km, p, yaw, b, bottom, top, wall, roof, Facade.Village, m, dome, ref rnd);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                case Style.Templer:
                {
                    var wall = Jitter(C(0.86f, 0.79f, 0.66f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.72f, 0.31f, 0.20f), ref rnd, 0.06f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.55f, roof, wall);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
                default:
                {
                    var wall = Jitter(C(0.92f, 0.89f, 0.82f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.74f, 0.31f, 0.20f), ref rnd, 0.08f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.42f, roof, wall);
                    Record(id, "building", p, yaw, b.w, b.d, b.h, wall);
                    break;
                }
            }
        }

        // ---- Stone houses: the old towns and the Arab towns and villages (settlements quick wins, #31) -------------------
        // Promo 03's Qalqilya and Jaffa are layered stone houses: stepped storeys, roof rooms, few domes, grey-buff stone. The
        // 1948 basis: the hill country built in limestone and the coastal plain and valleys in mud brick (Palestine Ministry of
        // Tourism, "Traditional Palestinian House - Al Hosh", https://virtualmuseum.tourism.ps/en/article/72, checked
        // 2026-10-13); the hill house's roof was a stone vault or cupola, the plain's a flat roof of beams and earth (Abuarkub,
        // "Traditional Building Materials and Techniques in Palestine", Prostor 25 (2017) 140-153,
        // https://doi.org/10.31522/p.25.1(53).9). So domes stay, but only on hill houses, fewer and lower (a vault's low
        // cap, not a white hemisphere), and every flat roof is lime plaster.

        /// <summary>A village above this elevation (m) is a hill village: limestone walls, some domed roofs. Below it, the coastal
        /// plain's and the valleys' mud-brick houses with flat roofs.</summary>
        const float HillVillageM = 120f;
        /// <summary>Share of an old town's flat-roofed houses with a dome (was 22%: promo 03's Jaffa shows few).</summary>
        const float OldHouseDome = 0.08f;
        /// <summary>Share of a hill village's houses with a dome (was 10% of every village's, then 7%; S1: 3%, as a village now
        /// draws about twice the houses, so it keeps about the domes it had).</summary>
        const float VillageDome = 0.03f;
        /// <summary>A hill village's terraces (S1): a house whose ground falls more than this (real m, plinth included: 2 m)
        /// across its footprint stands on a terrace reaching this far (real m) past its walls. Hill villages such as Battir
        /// stood among stone field terraces ("on the precipitous slope of a deep valley", its core "surrounded by agricultural
        /// terraces": https://en.wikipedia.org/wiki/Battir, checked 2026-10-15); the terrace under a house is a modelling
        /// choice that carries that stepped look into the village.</summary>
        const float TerraceRiseM = 2.8f, TerraceMarginM = 0.3f;
        /// <summary>The terrace's dry-stone retaining wall and its beaten-earth yard (sRGB).</summary>
        static readonly Color32 TerraceStone = C(0.66f, 0.60f, 0.50f), TerraceYard = C(0.74f, 0.66f, 0.53f);
        /// <summary>Grey-buff limestone, lime-plastered roofs and the plain's mud brick (sRGB).</summary>
        static readonly Color32 LimeRoof = C(0.86f, 0.83f, 0.77f), MudBrick = C(0.78f, 0.69f, 0.56f);

        /// <summary>A limestone wall colour, from grey to buff (exactly two draws, as one Jitter: the villages' streams rely on it).</summary>
        static Color32 StoneWall(ref KitRandom rnd) => Jitter(C(0.77f, 0.70f, 0.60f), ref rnd, 0.07f);

        /// <summary>A stone or mud-brick house of <paramref name="b"/>'s footprint: of two storeys or more, most step back, the top
        /// storey a smaller block on one corner of the lower roof (the lower roof its terrace); a one-storey house often has a
        /// roof room (an upper room). <paramref name="dome"/> puts a low dome on the topmost roof. All of it stands
        /// inside the footprint, so the lot is unchanged.</summary>
        static void StoneHouse(KitMesh km, Vector2 p, float yaw, Spec b, float bottom, float top, Color32 wall, Color32 roof, Facade facade,
            float m, bool dome, ref KitRandom rnd)
        {
            float storey = 3.3f * m, a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector2 At(float u, float v) => p + new Vector2(u * ca - v * sa, u * sa + v * ca);
            // The upper block: 50-75% of the footprint each way, pushed to a seeded corner.
            float uw = b.w * rnd.Range(0.5f, 0.75f), ud = b.d * rnd.Range(0.5f, 0.75f);
            float su = rnd.Chance(0.5f) ? 1f : -1f, sv = rnd.Chance(0.5f) ? 1f : -1f;
            var uc = At(su * 0.5f * (b.w - uw), sv * 0.5f * (b.d - ud));
            if (b.storeys >= 2 && rnd.Chance(0.75f))
            {
                float low = b.h - storey;
                km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, low, wall, roof, facade, m);
                km.Box(uc.x, uc.y, yaw, uw, ud, top + low - 0.3f * m, top + low, storey, wall, roof, facade, m);
                if (dome) km.Dome(uc.x, uc.y, top + b.h - 0.2f * m, Mathf.Min(uw, ud) * 0.4f, roof, rise: 0.6f);
                return;
            }
            km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, facade, m);
            if (dome) km.Dome(p.x, p.y, top + b.h - 0.2f * m, Mathf.Min(b.w, b.d) * 0.33f, roof, rise: 0.6f);
            else if (rnd.Chance(0.35f))   // a roof room
                km.Box(uc.x, uc.y, yaw, uw * 0.8f, ud * 0.8f, top + b.h - 0.3f * m, top + b.h, storey * 0.85f, wall, roof, facade, m);
        }

        /// <summary>The White City's wall plaster (sRGB): lime white, a touch cool, so the warm sun leaves it white rather than cream.</summary>
        static readonly Color32 WhitePlaster = C(0.94f, 0.945f, 0.94f);

        /// <summary>How far (real m) a White City balcony stands out from its wall.</summary>
        const float BalconyM = 1.0f;

        /// <summary>A White City block's balconies (settlements quick wins, #31): the International Style's continuous balconies
        /// with solid plastered parapets, one per upper storey along a long side (most blocks on both), each a white band with
        /// its shadow under it, as promo 03's Tel Aviv shows. The box is <paramref name="w"/> x <paramref name="d"/> at
        /// <paramref name="c"/> (w along the yaw, the long side), walls from <paramref name="ground"/> up <paramref name="h"/>.
        /// Every balcony stays inside the plot's lot (radius <paramref name="lotR"/> round the plot's centre; c lies
        /// <paramref name="du"/> from it along the yaw), so trees and neighbours, which test the lot, never meet one.</summary>
        static void WhiteBlockDetail(KitMesh km, Vector2 c, float du, float lotR, float yaw, float w, float d, float ground, float h,
            Color32 wall, Color32 roof, float m, ref KitRandom rnd)
        {
            float storey = 3.3f * m, proj = BalconyM * m, parapet = 1.05f * m, slab = 0.2f * m;
            int storeys = Mathf.RoundToInt(h / storey);
            float a = yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float reachV = 0.5f * d + proj;
            var rail = Jitter(wall, ref rnd, 0.015f);
            for (int side = -1; side <= 1; side += 2)
            {
                // The street side always, the back on most blocks.
                if (side > 0 && !rnd.Chance(0.75f)) continue;
                float share = rnd.Range(0.5f, 0.85f), u0 = rnd.Range(-0.5f, 0.5f) * (1f - share) * w;
                float bw = Mathf.Min(share * w, 2f * (Mathf.Sqrt(Mathf.Max(0f, lotR * lotR - reachV * reachV)) - Mathf.Abs(du + u0)));
                if (bw < 0.25f * w) continue;
                float v = side * (0.5f * d + 0.5f * proj);
                float x = c.x + u0 * ca - v * sa, z = c.y + u0 * sa + v * ca;
                for (int k = 1; k < storeys; k++)
                {
                    float floor = ground + k * storey;
                    km.Box(x, z, yaw, bw, proj, floor - slab, floor - slab, slab + parapet, rail, roof, Facade.Plain, m);
                }
            }
        }

        /// <summary>Wall bottom (sunk 2 m below the lowest corner, so a house on a slope stands on a plinth, not on air) and the
        /// ground level the walls rise from (the highest corner, so the slope never buries a storey).</summary>
        static (float bottom, float ground) GroundSpan(Vector2 p, float yawDeg, float w, float d, KitGround ground, float m)
        {
            float a = yawDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 5; i++)
            {
                float u = i == 4 ? 0 : ((i & 1) == 0 ? -0.5f : 0.5f) * w, v = i == 4 ? 0 : ((i & 2) == 0 ? -0.5f : 0.5f) * d;
                float y = ground.Y(p.x + u * ca - v * sa, p.y + u * sa + v * ca);
                lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            return (lo - 2f * m, hi);
        }

        static Vector2 DryNear(Vector2 c, KitGround ground, float within, ref KitRandom rnd)
        {
            if (ground.Inside(c.x, c.y) && !ground.Water(c.x, c.y)) return c;
            for (int i = 0; i < 64; i++)
            {
                float a = rnd.Range(0f, 2f * Mathf.PI), r = within * (i + 1) / 64f;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (ground.Inside(p.x, p.y) && !ground.Water(p.x, p.y)) return p;
            }
            return c;
        }

        // ---- The 1948 localities (promo-villages) ---------------------------------------------------------------------

        /// <summary>Which part of a settlement BuildOne places: only its landmark (the first passes: every game settlement's,
        /// then every locality's, claims its heart before any houses), or only its houses (continuing the same random stream,
        /// so a place draws as it would in one pass).</summary>
        enum Phase { Landmark, Houses }

        /// <summary>A 1948 locality's landmark or houses (promo-villages): random spots over its disc, its archetype's buildings,
        /// clear of the grid's edge midpoints. The 25 game settlements use TownLandmarks and PlaceHouses (towns round 2).</summary>
        static int BuildOne(Settlement s, Vector2 centre, KitGround ground, KitMesh km, Lots lots, Phase phase, ref KitRandom rnd, int landmarksBefore,
            HashSet<string>? ignoreCellsOf, CityPlots? city = null)
        {
            var arch = ArchetypeOf(s);
            float R = FootprintRadiusM(s, ground.HexSizeM);
            float m = KitScale.For(arch);
            int placed = 0;

            // This settlement's cell: a point belongs to the settlement whose footprint it is deepest inside (distance over
            // radius), so Jaffa and Tel Aviv, or Lod and Ramla, meet along a seam instead of overlapping.
            bool Mine(Vector2 p)
            {
                float mine = (p - centre).magnitude / R;
                var fp = ground.Footprints;
                var near = fp.Near(p);
                for (int k = 0; k < near.Count; k++)
                {
                    int i = near[k];
                    if (fp[i].id != s.Id && (ignoreCellsOf == null || !ignoreCellsOf.Contains(fp[i].id))
                        && (p - fp[i].centre).magnitude / fp[i].radius < mine) return false;
                }
                return true;
            }
            bool Free(Vector2 p, float r) => FreeAt(lots, p, r);

            // Landmarks first, at the centre (or the nearest dry spot to it).
            if (phase != Phase.Houses) placed += PlaceLandmark(s, arch, centre, R, ground, km, lots, ref rnd, city);
            if (phase == Phase.Landmark) return placed;

            bool mixed = Localities.IsMixed(s.Id);
            int target = mixed ? Mathf.Max(TargetCount(Archetype.ArabTown, R), TargetCount(Archetype.Moshava, R)) : TargetCount(arch, R);
            if (phase == Phase.Houses) target -= landmarksBefore;
            int attempts = target * 30;
            bool ring = s.Type == "moshav";   // Nahalal's ring of farmsteads round the public buildings
            for (int i = 0; i < attempts && placed < target; i++)
            {
                // The disc grows slowly when the sea or a neighbour eats into it, so the count stays tied to the population.
                float reach = R * (1f + 0.6f * i / attempts);
                float u = rnd.Value(), a = rnd.Range(0f, 2f * Mathf.PI);
                float rr = ring ? reach * rnd.Range(0.55f, 0.75f) : reach * Mathf.Pow(u, arch == Archetype.Moshava ? 0.5f : 0.62f);
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                if (!ground.Inside(p.x, p.y) || ground.Water(p.x, p.y) || !Mine(p)) continue;
                float t = rr / R;   // 0 at the heart, 1 at the edge
                // A mixed locality (Nesher, owner ruling 2026-10-12): Arab village houses on its west side, Jewish ones on its
                // east, side by side.
                var a2 = mixed ? (p.x < centre.x ? Archetype.ArabTown : Archetype.Moshava) : arch;
                var b = Pick(a2, t, ref rnd, m);
                if (!Free(p, b.lotR) || ground.OnRoute(p, b.lotR)) continue;   // off the railways and roads too
                // A 1948 locality's house (only localities build houses with no ignoreCellsOf) keeps off the grid's edge midpoints.
                if (phase == Phase.Houses && ignoreCellsOf == null && ground.OnGridMark(p, b.lotR)) continue;
                if (ground.Slope(p.x, p.y, b.w) > (a2 == Archetype.ArabTown || a2 == Archetype.OldTown ? 0.6f : 0.35f)) continue;
                // Corner and seaward checks: the whole footprint is dry.
                if (ground.Water(p.x + b.w * 0.5f, p.y) || ground.Water(p.x - b.w * 0.5f, p.y) || ground.Water(p.x, p.y + b.d * 0.5f) || ground.Water(p.x, p.y - b.d * 0.5f)) continue;
                float yaw = a2 == Archetype.WhiteCity || a2 == Archetype.Moshava
                    ? GridYaw(s.Id) + (rnd.Chance(0.15f) ? 90f : 0f) + rnd.Range(-4f, 4f)
                    : rnd.Range(0f, 90f);
                lots.Add(new Lot { c = p, r = b.lotR, id = s.Id });
                Emit(a2, p, yaw, b, ground, km, ref rnd);
                placed++;
            }
            return placed;
        }

        /// <summary>A settlement's landmark at its heart (the dry spot nearest its centre): Jaffa's and the old towns', an Arab
        /// town's or village's mosque (a 1948 locality's follows its Village Statistics religion columns: a church in
        /// Christian Bethlehem, none in the Druze villages; a village's is smaller than a town's), a Jewish place's water
        /// tower. Returns how many were built.</summary>
        static int PlaceLandmark(Settlement s, Archetype arch, Vector2 centre, float R, KitGround ground, KitMesh km, Lots lots, ref KitRandom rnd,
            CityPlots? city = null)
        {
            var heart = DryNear(centre, ground, R * 0.3f, ref rnd);
            // Inside Tel Aviv's footprint (#318) the landmark stands at the street crossing nearest its spot, drawn small enough
            // that the four blocks round the crossing keep their plots. Elsewhere it stands where it wants, at full size.
            Vector2 At(Vector2 want) => city?.NearestCrossing(want) ?? want;
            float mosqueK = city == null ? 1f : InCityMosque, towerK = city == null ? 1f : InCityTower;
            if (Localities.LandmarkOf(s.Id) == Landmark.Mixed)   // a mixed locality: a mosque on its west side, a water tower on its east
                return Mosque(s.Id, At(heart - new Vector2(R * 0.3f, 0f)), ground, km, lots, ref rnd, 0.8f * mosqueK)
                    + WaterTower(s.Id, At(heart + new Vector2(R * 0.35f, 0f)), ground, km, lots, towerK);
            switch (arch)
            {
                case Archetype.OldTown: return Mosque(s.Id, At(heart), ground, km, lots, ref rnd, 1.2f * mosqueK);   // no locality is an old town today
                case Archetype.ArabTown:
                    float size = (s.Type == "village" ? 0.8f : 1f) * mosqueK;
                    switch (Localities.LandmarkOf(s.Id) ?? Landmark.Mosque)
                    {
                        // S1: a village keeps its mosque, as a village mosque (no dome, a short square minaret); the domed
                        // mosque with its tall minaret is the Arab towns'.
                        case Landmark.Mosque:
                            return s.Type == "village" ? VillageMosque(s.Id, At(heart), ground, km, lots, ref rnd, size)
                                : Mosque(s.Id, At(heart), ground, km, lots, ref rnd, size);
                        case Landmark.Church: return Church(s.Id, At(heart), ground, km, lots, size) ? 1 : 0;
                        default: return 0;
                    }
                case Archetype.Moshava:
                    return (Localities.LandmarkOf(s.Id) ?? Landmark.WaterTower) == Landmark.WaterTower
                        ? WaterTower(s.Id, At(heart + new Vector2(R * 0.35f, -R * 0.2f)), ground, km, lots, towerK) : 0;
                default: return 0;
            }
        }

        /// <summary>A 1948 locality's mosque and water tower inside Tel Aviv's footprint, as a share of their size elsewhere
        /// (#318). Their lots (a village mosque's 132 m, a water tower's 120 m) then fit a street crossing with the blocks of
        /// all four plots round it at full size: Tel Aviv's core cover is 25.6%, as without the localities, against 18.5%
        /// with full-size landmarks. At 0.3 for the mosques it falls to 24.3%.</summary>
        const float InCityMosque = 0.25f, InCityTower = 0.3f;

        /// <summary>A white city's block lattice (Tel Aviv's: one lattice, no jitter), to stand a 1948 locality's landmark at
        /// one of its street crossings (#318).</summary>
        sealed class CityPlots
        {
            readonly Vector2 c; readonly float pu, pv, ca, sa;
            public CityPlots(Vector2 centre, float yawDeg)
            {
                var g = Grid(Style.TelAviv);
                c = centre; pu = g.pu * g.scale; pv = g.pv * g.scale;
                ca = Mathf.Cos(yawDeg * Mathf.Deg2Rad); sa = Mathf.Sin(yawDeg * Mathf.Deg2Rad);
            }
            /// <summary>The street crossing nearest <paramref name="p"/>: the corner shared by four plots, half a pitch off
            /// every plot's centre.</summary>
            public Vector2 NearestCrossing(Vector2 p)
            {
                var d = p - c;
                float u = (Mathf.Floor((d.x * ca + d.y * sa) / pu) + 0.5f) * pu, v = (Mathf.Floor((-d.x * sa + d.y * ca) / pv) + 0.5f) * pv;
                return c + new Vector2(u * ca - v * sa, u * sa + v * ca);
            }
        }

        /// <summary>The city whose plots a 1948 locality's landmark stands on (#318, owner ruling 2026-10-12): Tel Aviv's, when the
        /// locality's centre lies inside its footprint; else null. Its villages (Sheikh Muwannis, Jammasin, Salama) and its
        /// neighbours (Ramat Gan, Givatayim, Bnei Brak) keep their landmarks, smaller, at the street crossing nearest their hearts.</summary>
        static CityPlots? PackedIn(Settlement loc, IReadOnlyList<Settlement> towns, Dictionary<string, Vector2> centres, KitGround ground)
        {
            foreach (var t in towns)
            {
                if (t.Id != "tel_aviv") continue;
                if ((centres[loc.Id] - centres[t.Id]).magnitude <= FootprintRadiusM(t, ground.HexSizeM)) return new CityPlots(centres[t.Id], GridYaw(t.Id));
            }
            return null;
        }

        /// <summary>Buildings per settlement: the footprint area times the archetype's built-up share over its mean lot.</summary>
        static int TargetCount(Archetype arch, float R)
        {
            float area = Mathf.PI * R * R, m = KitScale.For(arch);
            float lot = arch switch
            {
                Archetype.WhiteCity => 21f * 15f, Archetype.OldTown => 11f * 10f, Archetype.ArabTown => 10f * 9f, _ => 10f * 8f,
            } * m * m;
            float share = arch switch { Archetype.WhiteCity => 0.36f, Archetype.OldTown => 0.46f, Archetype.ArabTown => 0.28f, _ => 0.09f };
            return Mathf.Clamp(Mathf.RoundToInt(area * share / lot), 6, 900);
        }

        /// <summary>A locality's building, by archetype (promo-villages).</summary>
        static Spec Pick(Archetype arch, float t, ref KitRandom rnd, float m)
        {
            float w, d; int storeys;
            switch (arch)
            {
                case Archetype.WhiteCity:
                    w = rnd.Range(15f, 26f); d = rnd.Range(11f, 18f);
                    // Taller near the centre; a few landmarks taller still (the promo's skyline, owner ruling: promo wins).
                    storeys = Mathf.RoundToInt(Mathf.Lerp(5.2f, 2.2f, t) + rnd.Range(-1.2f, 1.2f));
                    if (t < 0.45f && rnd.Chance(0.07f)) storeys = rnd.Range(7, 10);
                    storeys = Mathf.Clamp(storeys, 2, 9);
                    break;
                case Archetype.OldTown:
                    w = rnd.Range(7f, 14f); d = rnd.Range(7f, 12f);
                    storeys = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3.2f, 1.4f, t) + rnd.Range(-0.8f, 0.8f)), 1, 4);
                    break;
                case Archetype.ArabTown:
                    w = rnd.Range(6.5f, 12f); d = rnd.Range(6f, 10f);
                    storeys = rnd.Chance(Mathf.Lerp(0.45f, 0.1f, t)) ? 2 : 1;
                    break;
                default:
                    w = rnd.Range(8f, 12f); d = rnd.Range(6.5f, 9f);
                    storeys = rnd.Chance(0.15f) ? 2 : 1;
                    if (rnd.Chance(0.06f)) { w = rnd.Range(18f, 28f); d = rnd.Range(8f, 10f); }   // a barn, a dining hall, a packing house
                    break;
            }
            float storeyM = 3.3f;
            float gap = arch switch { Archetype.WhiteCity => 1.25f, Archetype.OldTown => 0.92f, Archetype.ArabTown => 1.05f, _ => 2.4f };
            return new Spec
            {
                w = w * m, d = d * m, h = storeys * storeyM * m, storeys = storeys,
                lotR = 0.5f * Mathf.Sqrt(w * w + d * d) * m * gap * (arch == Archetype.OldTown ? 0.8f : 0.72f),
            };
        }

        /// <summary>Draws a locality's building (promo-villages).</summary>
        /// <returns>The wall colour (the census records it for a town's fallback houses, #317).</returns>
        static Color32 Emit(Archetype arch, Vector2 p, float yaw, Spec b, KitGround ground, KitMesh km, ref KitRandom rnd)
        {
            float m = KitScale.For(arch);
            var (bottom, top) = GroundSpan(p, yaw, b.w, b.d, ground, m);
            switch (arch)
            {
                case Archetype.WhiteCity:
                {
                    var wall = rnd.Chance(0.12f) ? Jitter(C(0.90f, 0.84f, 0.72f), ref rnd, 0.04f) : Jitter(C(0.94f, 0.92f, 0.87f), ref rnd, 0.035f);
                    var roof = Jitter(C(0.80f, 0.78f, 0.74f), ref rnd, 0.05f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.WhiteCity, m);
                    // Some blocks are L-shaped: a lower wing at one end, back from the street.
                    if (rnd.Chance(0.3f))
                    {
                        float a = yaw * Mathf.Deg2Rad, side = rnd.Chance(0.5f) ? 1f : -1f;
                        float ww = b.w * 0.42f, wd = b.d * 0.8f;
                        float u = side * (b.w - ww) * 0.5f, v = (b.d + wd) * 0.5f * 0.92f;
                        var wc = new Vector2(p.x + u * Mathf.Cos(a) - v * Mathf.Sin(a), p.y + u * Mathf.Sin(a) + v * Mathf.Cos(a));
                        if (!ground.Water(wc.x, wc.y))
                            km.Box(wc.x, wc.y, yaw, ww, wd, bottom, top, Mathf.Max(2f * 3.3f * m, b.h - 3.3f * m), wall, roof, Facade.WhiteCity, m);
                    }
                    // A stair tower or roof room on many Bauhaus blocks, and a parapet's shadow line.
                    if (rnd.Chance(0.55f))
                    {
                        float rw = b.w * rnd.Range(0.25f, 0.4f), rd = b.d * rnd.Range(0.3f, 0.45f);
                        km.Box(p.x + b.w * rnd.Range(-0.2f, 0.2f), p.y + b.d * rnd.Range(-0.15f, 0.15f), yaw, rw, rd, top + b.h - 2f, top + b.h, 2.8f * m, wall, roof, Facade.Plain, m);
                    }
                    return wall;
                }
                case Archetype.OldTown:
                {
                    // A town's fallback house (#317), on its own stream: drawn as the old town's lattice houses are.
                    var wall = StoneWall(ref rnd);
                    var roof = Jitter(LimeRoof, ref rnd, 0.04f);
                    if (rnd.Chance(0.03f))
                    {
                        km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Stone, m, roofed: true);
                        km.Pyramid(p.x, p.y, yaw, b.w * 1.04f, b.d * 1.04f, top + b.h, 2.2f * m, Jitter(C(0.70f, 0.33f, 0.22f), ref rnd, 0.06f));
                    }
                    else StoneHouse(km, p, yaw, b, bottom, top, wall, roof, Facade.Stone, m, rnd.Chance(OldHouseDome), ref rnd);
                    return wall;
                }
                case Archetype.ArabTown:
                {
                    // A 1948 locality's houses share the locality's stream with its spots, so this draws exactly the five numbers
                    // it always has (wall, roof, the dome roll), and the house's own detail comes from a stream seeded off it: the
                    // villages keep every house where it was (layout, lots and trees unchanged).
                    var peek = rnd; var detail = new KitRandom(peek.Next() ^ 0xA5C3F1E2D4B69788UL);
                    bool hill = ground.ElevM(p.x, p.y) >= HillVillageM;
                    var wall = hill ? StoneWall(ref rnd) : Jitter(MudBrick, ref rnd, 0.06f);
                    var roof = Jitter(LimeRoof, ref rnd, 0.05f);
                    bool dome = rnd.Value() < VillageDome && hill;
                    StoneHouse(km, p, yaw, b, bottom, top, wall, roof, Facade.Village, m, dome, ref detail);
                    return wall;
                }
                default:
                {
                    var wall = Jitter(C(0.92f, 0.89f, 0.82f), ref rnd, 0.04f);
                    var roof = Jitter(C(0.74f, 0.31f, 0.20f), ref rnd, 0.08f);
                    km.Box(p.x, p.y, yaw, b.w, b.d, bottom, top, b.h, wall, roof, Facade.Village, m, roofed: true);
                    km.Gable(p.x, p.y, yaw, b.w, b.d, top + b.h, Mathf.Min(b.w, b.d) * 0.42f, roof, wall);
                    return wall;
                }
            }
        }

        /// <summary>A sub-district's Bedouin camps (owner ruling 2026-10-11: a light scatter sized by the sourced total, no named
        /// points): each camp, seeded by the sub-district and its number, takes a random spot in one of the area's hexes that
        /// is dry, gentle and outside every town's footprint, and pitches two to four black goat-hair tents in a loose row
        /// (each tent its own lot, so nothing overlaps). Returns the camps and tents built.</summary>
        /// <summary>How far (in house-scale metres) a camp's tents and its footprint reach from its centre: the longest row
        /// (four tents, 28 apart at most, offset up to 9 to a side) and its footprint's 8 of margin, rounded up. A camp keeps
        /// this clear of every unit token (KitGround.Tokens).</summary>
        public const float CampReach = 64f;

        static (int camps, int tents) Encampments(Localities.Area area, KitGround ground, Lots lots, System.Func<Vector2, KitMesh> tile)
        {
            if (area.Hexes.Count == 0) return (0, 0);
            float m = KitScale.Houses, hex = ground.HexSizeM;
            int want = Localities.CampsOf(area), camps = 0, tents = 0;
            var origin = ground.TerrainOrigin;
            for (int i = 0; i < want; i++)
            {
                var rnd = new KitRandom("camp:" + area.Subdistrict, (ulong)i + 1);
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    var (q, r) = area.Hexes[rnd.Range(0, area.Hexes.Count)];
                    var w = HexGrid.ToWorld(HexGrid.Centre(q, r, hex), origin, ground.Length);
                    float a = rnd.Range(0f, 2f * Mathf.PI), d = hex * 0.7f * Mathf.Sqrt(rnd.Value());
                    var c = new Vector2(w.x + Mathf.Cos(a) * d, w.z + Mathf.Sin(a) * d);
                    if (!ground.Inside(c.x, c.y) || ground.Water(c.x, c.y) || ground.InTown(c.x, c.y, 1.2f) || ground.Slope(c.x, c.y, 600f) > 0.12f
                        || ground.OnToken(c, CampReach * m)) continue;
                    float yaw = rnd.Range(-25f, 25f), ca = Mathf.Cos(yaw * Mathf.Deg2Rad), sa = Mathf.Sin(yaw * Mathf.Deg2Rad);
                    int n = rnd.Range(2, 5), pitched = 0;
                    var km = tile(c);
                    float along = -(n - 1) * 11f * m;
                    for (int k = 0; k < n; k++)
                    {
                        // A loose, uneven line (a neat row of equal boxes read as a train): each tent its own length, gap,
                        // offset and angle.
                        float tw = rnd.Range(7f, 12f) * m, td = rnd.Range(4f, 5.5f) * m;
                        var p = c + new Vector2(along * ca, along * sa) + new Vector2(-sa, ca) * rnd.Range(-9f, 9f) * m;
                        along += rnd.Range(16f, 28f) * m;
                        float tyaw = yaw + rnd.Range(-20f, 20f);
                        float lotR = 0.55f * Mathf.Sqrt(tw * tw + td * td);
                        if (ground.Water(p.x, p.y) || !FreeAt(lots, p, lotR) || ground.OnRoute(p, lotR) || ground.OnGridMark(p, lotR)) continue;
                        lots.Add(new Lot { c = p, r = lotR, id = "camp:" + area.Subdistrict });
                        var (bottom, top) = GroundSpan(p, tyaw, tw, td, ground, m);
                        // Goat-hair cloth, a warm dark brown rather than black: at the overview a black row read as a scratch.
                        // Low side walls under a peaked roof, so it reads as a tent rather than a shed.
                        var cloth = Jitter(C(0.33f, 0.27f, 0.21f), ref rnd, 0.08f);
                        km.Box(p.x, p.y, tyaw, tw, td, bottom, top, 1.1f * m, Jitter(C(0.42f, 0.35f, 0.27f), ref rnd, 0.06f), cloth, Facade.Plain, m, roofed: true);
                        km.Gable(p.x, p.y, tyaw, tw, td, top + 1.1f * m, 1.4f * m, cloth, cloth);
                        pitched++;
                    }
                    if (pitched == 0) continue;
                    ground.Footprints.Add((c, Mathf.Abs(along) * 0.5f + 8f * m, "camp:" + area.Subdistrict));   // about the row's half length: groves stay out
                    camps++; tents += pitched;
                    break;
                }
            }
            return (camps, tents);
        }
    }

    /// <summary>The symbolic scale per class (owner ruling 2026-10-11, superseding the 10-25x ruling: match promo 03's
    /// measured scale per class): city blocks about 60x their real size, houses and trees about 100x; geography stays real.
    /// Landmarks (a clock tower, a mosque, a harbour) take the city-block scale. Fields, not constants, so the capture tool
    /// and the player's ?kitscale / ?housescale / ?treescale flags (clamped to <see cref="Min"/>..<see cref="Max"/>) can show
    /// another scale. Counts are tuned at <see cref="CountReference"/> and fall with the square of the scale.</summary>
    public static class KitScale
    {
        public const float DefaultCityBlocks = 60f, DefaultHouses = 100f, DefaultTrees = 100f;
        public const float Min = 5f, Max = 200f;
        /// <summary>The scale the building and tree counts were tuned at: a class drawn at k times this has 1/k^2 as many.</summary>
        public const float CountReference = 25f;
        public static float CityBlocks = DefaultCityBlocks;
        public static float Houses = DefaultHouses;
        public static float Trees = DefaultTrees;

        public static float Landmarks => CityBlocks;
        public static float For(Archetype a) => a == Archetype.WhiteCity ? CityBlocks : Houses;

        public static float Clamp(float v) => Mathf.Clamp(v, Min, Max);

        public static void Reset() { CityBlocks = DefaultCityBlocks; Houses = DefaultHouses; Trees = DefaultTrees; }
    }
}
