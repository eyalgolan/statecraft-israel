using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Builds the whole map: terrain, hexes, 1948 settlements, railways and roads, camera. Materials are editor-made assets
    /// (M0 correction 1). Build is idempotent: it destroys what the previous Build made first. In play mode the Boot
    /// component calls Build(inputs) once the inputs are loaded (fetched on WebGL); editor tools call Build().</summary>
    public sealed class MapScene : MonoBehaviour
    {
        [SerializeField] Material terrainMaterial, markerMaterial, selectionMaterial, seaMaterial, frameMaterial;
        /// <summary>The art spike's procedural settlement kits and trees (Kit.mat, Tree.mat: Statecraft/Kit). Optional: without
        /// them the map keeps the placeholder marker boxes.</summary>
        [SerializeField] Material kitMaterial, treeMaterial;
        /// <summary>The terrain water's mesh, baked (MapTexturePaths.WaterMesh, issue #195). Optional: without it, or when it
        /// is for other heights, the map derives the mesh at load (about 90 ms at the boot budget's CPU throttling).</summary>
        [SerializeField] TextAsset terrainWaterMesh;

        public GeoFrame Frame { get; private set; }
        /// <summary>hexes.json as the last Build read it (bounds, hex size, biomes).</summary>
        public HexFile Hexes { get; private set; }
        public Terrain Terrain { get; private set; }
        public Material MarkerMaterial => markerMaterial;
        /// <summary>Kit.mat (null without the kits), for the transit camps' kits PlacementLayer builds when a camp is placed.</summary>
        public Material KitMaterial => kitMaterial;
        /// <summary>The heightmap's lowest elevation (m) as the last Build read it: a KitGround built after the map needs it.</summary>
        public float ElevMinM { get; private set; }
        /// <summary>The settlements the last Build placed (settlements_1948.yaml), for the UI's map labels and, through
        /// SettlementsFor, the info panel's names. Read only: the markers, the kits, the labels and GameRoot share this one
        /// list and its objects; nothing may write to them.</summary>
        public IReadOnlyList<Settlement> Settlements { get; private set; } = new List<Settlement>();
        string settlementsYamlRead;   // null before a build

        /// <summary>The settlements for <paramref name="yaml"/>: the list the last Build parsed when it read the same text
        /// (issue #321: GameRoot parsed the file again at boot only for the names), else a parse of it.</summary>
        public IReadOnlyList<Settlement> SettlementsFor(string yaml) =>
            settlementsYamlRead != null && string.Equals(yaml, settlementsYamlRead, StringComparison.Ordinal)
                ? Settlements : SettlementPlacer.Parse(yaml);
        /// <summary>The 1948 localities the last Build drew with the kits (content/map/localities_1948.yaml; empty without the
        /// kits), for the map labels: 1948 name, today's name. Presentation only, never in the sim.</summary>
        public IReadOnlyList<Kits.Localities.Place> Localities { get; private set; } = new List<Kits.Localities.Place>();

        /// <summary>Every kit's ground after the last Build (world x, z centre; radius; owner id): the 25 game settlements', the
        /// 1948 localities' and the Bedouin camps' footprints (empty without the kits). Anything placed on the map "off the
        /// towns" (unit tokens: review I5) keeps clear of these, not of the 25 settlements alone.</summary>
        public IReadOnlyList<(Vector2 centre, float radius, string id)> KitFootprints => KitStats?.Footprints ?? (IReadOnlyList<(Vector2, float, string)>)System.Array.Empty<(Vector2, float, string)>();
        /// <summary>The game date the routes were last shown at (RouteLayer.ShowAt), so a rebuild draws them as at that date;
        /// null before the game has set one (the routes then show as at the game's start).</summary>
        public DateTime? RouteDate { get; set; }

        /// <summary>The selected hex's outline: a child of the terrain (so a rebuild destroys it).</summary>
        public const string SelectionName = "HexSelection";
        /// <summary>The frame round the map and the table beyond it (issue #36): a child of the terrain (so a rebuild destroys it).</summary>
        public const string FrameName = "MapFrame";
        /// <summary>The water inside the map: a mesh over the water and its shore, a child of the terrain (issue #195).</summary>
        public const string TerrainWaterName = "TerrainWater";
        /// <summary>The terrain water's render queue: after the terrain (Terrain.mat, one queue earlier: MapMaterials) and
        /// before every other opaque, so boats, harbours and units drawn on the water cover it despite its depth pull.</summary>
        public const int TerrainWaterQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry - 1;
        /// <summary>Highlight colour of the selected hex, to read against every biome; MapMaterials bakes it into the
        /// editor-made HexSelection.mat. Warm ivory since the visual v1 merge point: the earlier gold (1, 0.84, 0.38) shares
        /// the hue of the terrain shader's dry-grass coastal plain (MapPalette), so at gameplay03 it stood out from the land
        /// only by lightness (deltaE76 18.2 ungraded, under GradeTests' 20). Ivory differs in chroma too, like the promo's
        /// white rings and lines.</summary>
        public static readonly Color SelectionColour = new Color(1f, 0.95f, 0.80f);
        /// <summary>Width of the outline band, as a fraction of the hex circumradius (about 600 m on a 5 km hex).</summary>
        public const float SelectionWidth = 0.12f;
        /// <summary>Height of the band above the highest ground under it. 30 m since visual v1 Task 8 (it was the line mesh's
        /// 120 m plus 20 m, to cover the lines; at the 6 km hero zoom that floated); the material's depth offset keeps it
        /// from z-fighting the terrain.</summary>
        public const float SelectionClearanceM = 30f;

        /// <summary>Hides the selected hex's outline (a selected unit has the ring instead: UnitLayer).</summary>
        public void ClearSelection()
        {
            var found = Terrain != null ? Terrain.transform.Find(SelectionName) : null;
            if (found != null) found.GetComponent<MeshRenderer>().enabled = false;
        }

        /// <summary>Outlines hex (q, r) with a band draped on the terrain (MapBuilder.DrapedRing, HexMath corners) drawn with
        /// the editor-made HexSelection.mat, whose depth offset keeps it from z-fighting the ground (no runtime material). The band is sampled at its outer, middle and inner rings and each index
        /// takes the highest, so terrain inside the band cannot rise through it. A hex that is not on the map hides it.</summary>
        public void Select(int q, int r)
        {
            if (Terrain == null || Hexes == null) return;
            var found = Terrain.transform.Find(SelectionName);
            var go = found != null ? found.gameObject : null;
            if (go == null)
            {
                go = new GameObject(SelectionName);
                go.transform.SetParent(Terrain.transform, false);
                var mesh = Terrain.GetComponent<MapRuntimeAssets>().Own(new Mesh { name = SelectionName });
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = selectionMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var mr = go.GetComponent<MeshRenderer>();
            HexJson hex = null;
            foreach (var h in Hexes.hexes) if (h.q == q && h.r == r) { hex = h; break; }
            if (hex == null) { mr.enabled = false; return; }
            var c = Frame.ToWorld(hex.lon, hex.lat, 0);
            var centre = new Vector2(c.x, c.z);
            float size = Hexes.hex_size_km * 1000f, clearance = SelectionClearanceM;
            var verts = new List<Vector3>(2 * MapBuilder.RingVertices);
            var middle = new List<Vector3>(MapBuilder.RingVertices);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size, clearance, verts);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size * (1f - SelectionWidth), clearance, verts);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size * (1f - SelectionWidth / 2), clearance, middle);
            int n = MapBuilder.RingVertices;
            var tris = new List<int>(6 * n);
            for (int j = 0; j < n; j++)
            {
                // Each index at the highest of the outer, middle and inner ring, so the band clears the ground across it.
                float y = Mathf.Max(verts[j].y, Mathf.Max(verts[n + j].y, middle[j].y));
                verts[j] = new Vector3(verts[j].x, y, verts[j].z); verts[n + j] = new Vector3(verts[n + j].x, y, verts[n + j].z);
            }
            for (int j = 0; j < n; j++)
            {
                int a = j, b = (j + 1) % n, ai = n + j, bi = n + (j + 1) % n;
                tris.AddRange(new[] { a, ai, b, b, ai, bi });   // the shader draws both faces (Cull Off)
            }
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            m.Clear();
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            mr.enabled = true;
        }

        public void Configure(Material terrain, Material marker, Material selection, Material sea, Material frame)
        {
            terrainMaterial = terrain; markerMaterial = marker; selectionMaterial = selection;
            seaMaterial = sea; frameMaterial = frame;
        }

        /// <summary>The settlement kits' and trees' materials (null, null: placeholder markers only).</summary>
        public void ConfigureKits(Material kit, Material tree) { kitMaterial = kit; treeMaterial = tree; }

        /// <summary>Each settlement's footprint on the ground plane (centre x, z; radius m): the kits' discs
        /// (SettlementKits.FootprintRadiusM) of the game settlements and, with the kits, of the 1948 localities, so the units
        /// keep their tokens off the buildings (UnitLayer). The Bedouin camps are not here: they are placed after the units and
        /// keep off them (KitGround.Tokens). Empty before Build.</summary>
        public IReadOnlyList<(Vector2 centre, float radius)> SettlementFootprints { get; private set; } = new List<(Vector2, float)>();

        /// <summary>Whether the next Build keeps a kit clearing for the units' showcase trio (Units.UnitShowcase.Clearing), as
        /// for a unit in a town: null means only when the run asks for the showcase (?units, -units), so a plain screenshot
        /// keeps every kit (review of #559).</summary>
        public bool? ShowcaseClearing { get; set; }

        /// <summary>The railways' and roads' corridors (RouteLayer.Corridors), every route whatever its date, so the units keep
        /// their figures off them. Empty before Build.</summary>
        public IReadOnlyList<(List<Vector2> line, float halfWidth)> RouteCorridors { get; private set; } = new List<(List<Vector2>, float)>();

        /// <summary>The units of content/map/units_1948.yaml as the last Build read them (empty before a build, or when the
        /// file is missing or malformed). Read only: the units layer draws from this same list (UnitEntriesFor).</summary>
        public IReadOnlyList<Units.UnitEntry> UnitEntries { get; private set; } = Array.Empty<Units.UnitEntry>();
        string unitsYamlRead;   // null before a build

        /// <summary>The 1948 sub-districts as the last Build read them for the Bedouin camps' hexes (null when it did not:
        /// no kits, or no sub-district file). Read only: GameRoot's region layers draw from this same object.</summary>
        public Subdistricts1948 Subdistricts { get; private set; }
        string regionsYamlRead;   // null before a build, or when Build did not read the file

        /// <summary>The sub-districts for <paramref name="yaml"/>: the object the last Build read when it read the same text
        /// (issue #321: the file was read twice at boot), else read from <paramref name="tsv"/> or the YAML
        /// (Subdistricts1948.ReadRuntimeOrYaml, warning through <paramref name="warn"/>).</summary>
        public Subdistricts1948 SubdistrictsFor(string tsv, string yaml, Action<string> warn) =>
            regionsYamlRead != null && Subdistricts != null && string.Equals(yaml, regionsYamlRead, StringComparison.Ordinal)
                ? Subdistricts : Subdistricts1948.ReadRuntimeOrYaml(tsv, yaml, warn);

        /// <summary>The unit spots the last Build placed (UnitGround.Place over <see cref="UnitEntries"/>, keyed by their
        /// anchors; an anchor it could not place has none) and the hexes it placed them on. The units layer takes them instead
        /// of placing again when it draws the same units on the same hexes (issue #321; UnitSpotsFor).</summary>
        public IReadOnlyDictionary<Units.UnitAnchor, Units.AnchorSpot> UnitSpots { get; private set; } = new Dictionary<Units.UnitAnchor, Units.AnchorSpot>();
        /// <remarks>Kept for the map's life, not released once a layer has taken the spots: GameRoot.Begin may run again on
        /// the same map (it shuts the old layer down and makes a new one; the editor tests do), and each layer checks its
        /// hexes against these. 1,102 small objects; nothing writes to them after Build.</remarks>
        public IReadOnlyList<Statecraft.Sim.MapHex> UnitHexes { get; private set; } = Array.Empty<Statecraft.Sim.MapHex>();

        /// <summary>The map's unit spots when <paramref name="units"/> is the list the map
        /// placed and <paramref name="hexes"/> match the hexes it placed them on (q, r, lon, lat and land, in order): the same
        /// ground, so the same spots. Null otherwise: the caller places them itself.</summary>
        public IReadOnlyDictionary<Units.UnitAnchor, Units.AnchorSpot> UnitSpotsFor(
            IReadOnlyList<Units.UnitEntry> units, IReadOnlyList<Statecraft.Sim.MapHex> hexes)
        {
            if (unitsYamlRead == null || !ReferenceEquals(units, UnitEntries) || hexes == null || hexes.Count != UnitHexes.Count) return null;
            for (int i = 0; i < hexes.Count; i++)
            {
                var (a, b) = (hexes[i], UnitHexes[i]);
                if (a.Q != b.Q || a.R != b.R || a.Land != b.Land
                    || BitConverter.DoubleToInt64Bits(a.Lon) != BitConverter.DoubleToInt64Bits(b.Lon)
                    || BitConverter.DoubleToInt64Bits(a.Lat) != BitConverter.DoubleToInt64Bits(b.Lat)) return null;
            }
            return UnitSpots;
        }

        /// <summary>The units for <paramref name="yaml"/>: the list the last Build parsed when it read the same text (issue
        /// #321: the file was parsed twice at boot, here and for the units layer), else a parse of it (UnitData.ReadOrNone,
        /// warning through <paramref name="warn"/>).</summary>
        public IReadOnlyList<Units.UnitEntry> UnitEntriesFor(string yaml, Action<string> warn) =>
            unitsYamlRead != null && string.Equals(yaml, unitsYamlRead, StringComparison.Ordinal)
                ? UnitEntries : Units.UnitData.ReadOrNone(yaml, warn);

        /// <summary>Where the kits keep their buildings and trees off the units (centre x, z; radius m): the spot of a unit that
        /// stands in a town (UnitPlacer's last round, UnitGround.ClearingRadius), so no token stands in a building. Empty before
        /// Build or without the unit file.</summary>
        public IReadOnlyList<(Vector2 centre, float radius)> UnitClearings { get; private set; } = new List<(Vector2, float)>();

        /// <summary>The unit tokens that stand outside the towns (centre x, z; ring radius m), as the kits get them
        /// (KitGround.Tokens), with the day the token's window ends (its anchor's `to`, exclusive): the transit camps
        /// PlacementLayer builds later keep off those still present on the camp's date (KitGround.DatedTokens). Empty before
        /// Build.</summary>
        public IReadOnlyList<(Vector2 centre, float radius, DateTime until)> UnitTokens { get; private set; } = new List<(Vector2, float, DateTime)>();

        /// <summary>The last Build's kit numbers (buildings, vertices, trees, build time), or null without kits.</summary>
        public Kits.KitLayer.Stats KitStats { get; private set; }

        readonly Dictionary<string, Bounds[]> kitColumns = new Dictionary<string, Bounds[]>();

        /// <summary>A settlement's kit (its buildings' merged mesh, as the last Build made it) as a few world boxes that hold
        /// all of it: the occupied cells of a KitMesh.ColumnGrid-square grid over its footprint, each from the ground
        /// under the kit to its tallest vertex in the cell. Much tighter on screen than the kit's one bounding box under a
        /// tilted camera (a water tower at the back no longer lifts the whole box). False without kits or for a kit with no
        /// buildings. The map labels keep clear of it (issue #214).</summary>
        public bool TryKitColumns(string id, out Bounds[] columns) => kitColumns.TryGetValue(id, out columns);

        MeshRenderer kitProbe;   // one kit's renderer: the "kits" toggle enables or disables them all together

        /// <summary>Whether the kits are drawn (built, and the "kits" toggle on); when not, the marker boxes are, and the map
        /// labels keep clear of those instead.</summary>
        public bool KitsShown => kitProbe != null && kitProbe.enabled;

        /// <summary>Builds from the repository files with System.IO (editor and desktop only).</summary>
        public void Build() => Build(GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap));

        public void Build(MapInputs inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (terrainMaterial == null) throw new InvalidOperationException("MapScene: terrainMaterial is not assigned");
            if (markerMaterial == null) throw new InvalidOperationException("MapScene: markerMaterial is not assigned");
            if (selectionMaterial == null) throw new InvalidOperationException("MapScene: selectionMaterial is not assigned");
            if (seaMaterial == null) throw new InvalidOperationException("MapScene: seaMaterial is not assigned");
            if (frameMaterial == null) throw new InvalidOperationException("MapScene: frameMaterial is not assigned");
            var cam = Camera.main;
            if (cam == null) throw new InvalidOperationException("MapScene needs a camera tagged MainCamera");

            Clear();
            kitColumns.Clear(); kitProbe = null;
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16z, MetaPath);
            ElevMinM = meta.elev_min_m;
            DiagLog.BootPhase("heightmap decoded");
            Hexes = MapBuilder.LoadHexes(inputs.HexesJson, HexesPath);
            DiagLog.BootPhase("hexes parsed");
            var list = SettlementPlacer.Parse(inputs.SettlementsYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), SettlementsPath);
            Settlements = list; settlementsYamlRead = inputs.SettlementsYaml;
            bool kitsOn = kitMaterial != null && treeMaterial != null;
            var localities = new Kits.Localities.Doc();
            if (kitsOn)
            {
                if (string.IsNullOrWhiteSpace(inputs.PopulationYaml))
                {
                    // Review N1: without the file every town would quietly take the smallest footprint. The editor (and its
                    // tests) refuses; a player warns and builds the small towns rather than no map.
                    var missing = $"map/{Kits.KitPopulation.FileName} is missing or empty: the settlement kits need it to size the towns";
                    if (Application.isEditor) throw new InvalidDataException(missing);
                    Debug.LogWarning("KITS " + missing + "; every town takes the smallest footprint");
                }
                var table = string.IsNullOrWhiteSpace(inputs.PopulationYaml)
                    ? new Dictionary<string, int>() : Kits.KitPopulation.Parse(inputs.PopulationYaml, "map/" + Kits.KitPopulation.FileName);
                if (string.IsNullOrWhiteSpace(inputs.LocalitiesTsv))
                {
                    // As the population file: the editor refuses, a player warns and draws the 25 game settlements only.
                    var missing = $"map/{Kits.Localities.RuntimeFileName} is missing or empty: the kits draw the 1948 localities from it (tools/localities_bake.py)";
                    if (Application.isEditor) throw new InvalidDataException(missing);
                    Debug.LogWarning("KITS " + missing);
                    localities = new Kits.Localities.Doc();
                }
                else
                {
                    var reserved = new HashSet<string>(); foreach (var s in list) reserved.Add(s.Id);
                    // The baked runtime form, not the 176k-character YAML (boot budget; pytest fails a stale bake).
                    localities = Kits.Localities.ParseRuntime(inputs.LocalitiesTsv, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), reserved,
                        "map/" + Kits.Localities.RuntimeFileName);
                }
                foreach (var p in localities.Places) table[p.Settlement.Id] = p.Population;
                Kits.KitPopulation.Use(table);
                // The camps' areas are the sub-districts as the region layer draws them (review I4: no copy in the
                // localities file).
                if (string.IsNullOrWhiteSpace(inputs.RegionsYaml)) { localities.Encampments.Clear(); Debug.LogWarning($"KITS map/{Subdistricts1948.FileName} is missing: no Bedouin camps"); }
                else
                {
                    // The camps' hexes from the sub-districts, read once from their runtime form (issue #321) and handed on
                    // to GameRoot's region layers (SubdistrictsFor).
                    Subdistricts = Subdistricts1948.ReadRuntimeOrYaml(inputs.RegionsTsv, inputs.RegionsYaml, Debug.LogWarning);
                    regionsYamlRead = inputs.RegionsYaml;
                    Kits.Localities.FillHexes(localities, Subdistricts, Hexes, "map/" + Kits.Localities.FileName);
                }
            }
            Kits.Localities.Use(localities.Places);   // on every build, so a map without the kits inherits nothing (review M13)
            if (kitsOn) DiagLog.BootPhase("localities parsed");
            Localities = localities.Places;
            var localitySettlements = localities.Places.ConvertAll(p => p.Settlement);
            var painted = new List<Settlement>(list); painted.AddRange(localitySettlements);
            float hexSizeM = Hexes.hex_size_km * 1000f;
            var terrainGo = MapBuilder.Build(meta, heights, Hexes, terrainMaterial,
                kitsOn ? (px, res, frame) =>
                {
                    // The ground tint under every town and locality (review I6: timed, as the boot budget is tight).
                    var tint = System.Diagnostics.Stopwatch.StartNew();
                    Kits.SettlementKits.PaintGround(px, res, frame, painted, hexSizeM);
                    Debug.Log(FormattableString.Invariant($"KITS ground_tint_ms={tint.Elapsed.TotalMilliseconds:0.0} places={painted.Count}"));
                } : null, bakedBiome: true);   // the biome colours from their bake (issue #321: boot time)
            terrainGo.transform.SetParent(transform, true);
            Terrain = terrainGo.GetComponent<Terrain>();
            var size = Terrain.terrainData.size;
            AddFrame(terrainGo, meta.elev_min_m);
            DiagLog.BootPhase("frame");
            AddTerrainWater(terrainGo, heights, meta.elev_min_m);
            DiagLog.BootPhase("terrain water");
            Frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
            var markers = SettlementPlacer.Place(list, Frame, Terrain, markerMaterial);
            markers.transform.SetParent(transform, true);
            DiagLog.BootPhase("settlements placed");
            KitStats = null;
            // The 1948 railways and roads (issue #35), read before the kits so buildings and trees keep off them.
            // From the baked runtime form when it matches the YAML (issue #321: boot time), else from the YAML with a warning.
            var routes = Routes.RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), Debug.LogWarning);
            DiagLog.BootPhase("route paths read");   // issue #321: the splits of the units' phase
            RouteCorridors = Routes.RouteLayer.Corridors(Frame, routes);
            DiagLog.BootPhase("route corridors");
            // The game settlements' and the 1948 localities' discs, as the kits will claim them (review I5: a token placed "off
            // the towns" keeps off the villages too). The camps are placed after the units and keep off them instead.
            SettlementFootprints = painted.Select(s => { var w = Frame.ToWorld(s.Lon, s.Lat, 0); return (new Vector2(w.x, w.z), Kits.SettlementKits.FootprintRadiusM(s, hexSizeM)); }).ToList();
            // The units' spots depend only on the footprints, the routes and the land, so they are known before the kits:
            // a unit placed in a town gets a clearing the kits keep free (UnitLayer places it the same way).
            DiagLog.BootPhase("footprints");
            var unitData = Units.UnitData.ReadOrNone(inputs.UnitsYaml, Debug.LogWarning);
            UnitEntries = unitData; unitsYamlRead = inputs.UnitsYaml ?? "";
            DiagLog.BootPhase("unit data read");
            var unitHexes = Hexes.hexes.Select(h => new Statecraft.Sim.MapHex { Q = h.q, R = h.r, Lon = h.lon, Lat = h.lat, ElevM = h.elev_m, Land = h.land, Biome = h.biome }).ToList();
            UnitHexes = unitHexes;
            var unitGround = new Units.UnitGround(SettlementFootprints, RouteCorridors, unitHexes,
                (lon, lat) => { var w = Frame.ToWorld(lon, lat, 0); return new Vector2(w.x, w.z); }, hexSizeM);
            var clearings = new List<(Vector2 centre, float radius)>();
            var unitRings = new List<(Vector2 centre, float radius)>();
            var datedRings = new List<(Vector2 centre, float radius, DateTime until)>();
            // The units' showcase trio gets the same clearing at its spot, off the railways and roads, so its base stands
            // clear of the kits (review of #559).
            if (ShowcaseClearing ?? Units.UnitShowcase.Requested()) clearings.Add(Units.UnitShowcase.Clearing(this));
            if (unitData.Count > 0)
            {
                var spots = unitGround.Place(unitData, (lon, lat) => { var w = Frame.ToWorld(lon, lat, 0); return new Vector2(w.x, w.z); }, null);
                UnitSpots = spots;
                foreach (var kv in spots.OrderBy(k => k.Value.X).ThenBy(k => k.Value.Z))
                {
                    var c = new Vector2((float)kv.Value.X, (float)kv.Value.Z);
                    if (kv.Value.InTown && !clearings.Exists(x => x.centre == c)) clearings.Add((c, unitGround.ClearingRadius));
                    else if (!kv.Value.InTown)
                    {
                        unitRings.Add((c, unitGround.RingRadius(kv.Value.Compact)));
                        datedRings.Add((c, unitGround.RingRadius(kv.Value.Compact), Units.UnitData.Day(kv.Key.To)));
                    }
                }
            }
            UnitClearings = clearings;
            DiagLog.BootPhase("units placed");   // issue #321: the routes' corridors, the footprints and the unit spots
            UnitTokens = datedRings;
            if (kitsOn)
            {
                // ?kitscale=, ?housescale=, ?treescale= in the WebGL player: the same layout at another symbolic scale (city
                // blocks, houses, trees), clamped to KitScale.Min..Max; docs/dev-setup.md lists the flags.
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    var url = Application.absoluteURL;
                    int ks = UrlFlags.Int(url, "kitscale"), hs = UrlFlags.Int(url, "housescale"), ts = UrlFlags.Int(url, "treescale");
                    if (ks > 0) Kits.KitScale.CityBlocks = Kits.KitScale.Clamp(ks);
                    if (hs > 0) Kits.KitScale.Houses = Kits.KitScale.Clamp(hs);
                    if (ts > 0) Kits.KitScale.Trees = Kits.KitScale.Clamp(ts);
                }
                // The kits replace the marker boxes; the boxes stay as the labels' and tests' anchors, and come back when the
                // "kits" toggle turns the kits off (so settlements are never left undrawn).
                // The units' clearings go in as corridors (village houses and tents keep off them too); the tokens outside
                // the towns are kept off by the Bedouin camps, the one kit placed where no footprint is (review I5).
                var kits = Kits.KitLayer.Build(list, localitySettlements, localities.Encampments, Terrain, Frame, Hexes, meta.elev_min_m, kitMaterial, treeMaterial,
                    terrainGo.GetComponent<MapRuntimeAssets>(), markers, out var stats,
                    RouteCorridors.Concat(UnitClearings.Select(c => (new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius))).ToList(), kitColumns,
                    unitRings);
                kits.transform.SetParent(transform, true);
                kitProbe = kits.GetComponentInChildren<MeshRenderer>(true);
                KitStats = stats;
                DiagLog.BootPhase("kits built");
                Debug.Log(stats.ToString());
            }
            // The routes are drawn map-only, as at the game's date (the start until the game sets one); a missing or malformed
            // file means no routes and a warning; the train only in screenshots, editor renders and ?train. The ribbons are
            // placed from their bake when it was made from these inputs (issue #321), else derived.
            Routes.RouteLayer.Build(Terrain, Frame, routes, Routes.RouteLayer.ModeForThisRun(), RouteDate ?? Routes.RouteLayer.DefaultDate,
                Routes.RouteRibbonFile.Source.Of(heights, inputs.HeightmapJson, inputs.RoutesYaml));
            DiagLog.BootPhase("routes built");

            if (!cam.TryGetComponent(out CameraRig rig)) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.LookAt = Frame.ToWorld(34.95, 31.95, 0); rig.Init(); // central Israel: Tel Aviv–Jerusalem corridor
            rig.Volume.transform.SetParent(transform, true);
            // VolumeProfile's own teardown only releases its components, it does not destroy them (Core RP 17.3), so own both.
            var assets = terrainGo.GetComponent<MapRuntimeAssets>();
            foreach (var c in rig.Volume.profile.components) assets.Own(c);
            assets.Own(rig.Volume.profile);
            foreach (var undo in rig.TeardownActions) assets.OnTeardown(undo);
            rig.TeardownActions.Clear();
        }

        /// <summary>The frame round the map and the table beyond it (MapBuilder.BuildFrame), drawn with the editor-made
        /// MapFrame.mat; no collider, so clicks still pick only the terrain, and no shadow. It lies outside the terrain's
        /// footprint only.</summary>
        void AddFrame(GameObject terrainGo, float elevMinM)
        {
            var frame = new GameObject(FrameName);
            frame.transform.SetParent(terrainGo.transform, false);
            frame.AddComponent<MeshFilter>().sharedMesh =
                MapBuilder.BuildFrame(Terrain.terrainData, elevMinM, terrainGo.GetComponent<MapRuntimeAssets>());
            var renderer = frame.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = frameMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>The water inside the map (issue #195): MapBuilder.BuildWaterMesh's copy of the terrain's surface over the
        /// water and its shore, drawn with a copy of Sea.mat in its terrain-water mode (Sea.shader _ScTerrainWater), which
        /// adds the water's colour times its coverage on top of the terrain graph (black there); the land adds 0 (no discard: whole quads, #306). Drawn
        /// right after the terrain, before the other opaques (TerrainWaterQueue); no depth write, no shadow, no collider
        /// (clicks still pick only the terrain). The water's shading thus stays out of the terrain's fragment shader, whose
        /// size it had pushed past a WebGL cost cliff (about 3 ms a frame at gameplay03, even with the water toggled
        /// off).</summary>
        void AddTerrainWater(GameObject terrainGo, float[,] heights, float elevMinM)
        {
            var assets = terrainGo.GetComponent<MapRuntimeAssets>();
            var material = assets.Own(new Material(seaMaterial) { name = TerrainWaterName });
            material.SetFloat("_ScTerrainWater", 1f);
            material.SetFloat("_ScSrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ScDstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ScZWrite", 0f);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("DepthNormals", false);
            material.renderQueue = TerrainWaterQueue;
            var go = new GameObject(TerrainWaterName);
            go.transform.SetParent(terrainGo.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = WaterMesh(heights, elevMinM, assets);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>The baked water mesh when it matches these heights, else the one derived from them.</summary>
        Mesh WaterMesh(float[,] heights, float elevMinM, MapRuntimeAssets assets)
        {
            var data = Terrain.terrainData;
            if (terrainWaterMesh != null)
            {
                if (MapBuilder.TryReadWaterMesh(terrainWaterMesh.bytes, data.heightmapResolution, heights, out var grid, out var indices, out var why))
                    return assets.Own(MapBuilder.WaterMesh(data.heightmapResolution, data.size, heights, grid, indices));
                Debug.LogWarning($"MAP {MapTexturePaths.WaterMesh}: {why}; deriving the water mesh at load");
            }
            return MapBuilder.BuildWaterMesh(data, heights, elevMinM, assets);
        }

        /// <summary>Test and editor seam: the baked water mesh (null: derived at load).</summary>
        public void ConfigureWater(TextAsset baked) => terrainWaterMesh = baked;

        static string MetaPath => "heightmap.json";
        static string HexesPath => "hexes.json";
        static string SettlementsPath => SettlementPlacer.DefaultFileName;

        /// <summary>Destroys every child: all of them are things Build made (terrain, Settlements, PostFX). Walking the
        /// hierarchy rather than remembered fields keeps Build idempotent after a domain reload (R2). The VolumeProfile is
        /// owned by the terrain's MapRuntimeAssets, so destroying the terrain destroys it.</summary>
        void Clear()
        {
            if (Application.isPlaying) Clear(go => { go.transform.SetParent(null); Destroy(go); });
            else Clear(go => DestroyImmediate(go));
        }

        /// <summary>Clear with the destroy step given (test seam for the play-mode path, whose Destroy is deferred): each
        /// child's MapRuntimeAssets teardown runs first, so it has run by the time Clear returns (review I1).</summary>
        internal void Clear(Action<GameObject> destroy)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var go = transform.GetChild(i).gameObject;
                foreach (var assets in go.GetComponentsInChildren<MapRuntimeAssets>(true)) assets.RunTeardown();
                destroy(go);
            }
            Terrain = null;
            Hexes = null;
            settlementsYamlRead = null;
            UnitEntries = Array.Empty<Units.UnitEntry>(); unitsYamlRead = null;
            Subdistricts = null; regionsYamlRead = null;
            UnitSpots = new Dictionary<Units.UnitAnchor, Units.AnchorSpot>(); UnitHexes = Array.Empty<Statecraft.Sim.MapHex>();
        }
    }
}
