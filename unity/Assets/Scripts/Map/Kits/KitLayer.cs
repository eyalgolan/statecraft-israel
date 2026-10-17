#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Builds the art spike's procedural layer under a returned "Kits" root: one static mesh per settlement (its
    /// buildings, merged: one draw each, culled by its bounds) drawn with the editor-made Kit.mat, the boats
    /// (<see cref="SeaCraft"/>, one more mesh with the same material), and the instanced trees
    /// (<see cref="VegetationLayer"/>) drawn with Tree.mat. Meshes are owned by the map's MapRuntimeAssets, so a rebuild
    /// frees them. Hides the marker boxes under <paramref name="markers"/> while the kits show. Registers the "kits" and
        /// "trees" visual toggles, so the frame-time profile prices each ("kits" off shows the markers again). Fills
    /// <paramref name="columns"/>, when given, with each kit's columns (world space: the kits sit at the root's origin), the
    /// 1948 localities' included.</summary>
    public static class KitLayer
    {
        public const string RootName = "Kits", VegetationName = "Vegetation", SeaCraftName = "SeaCraft", TownModelsName = "TownModels",
            CloseSuffix = " close";

        public sealed class Stats
        {
            public int Buildings, Vertices, Triangles, Trees, CorridorSegments, Localities, LocalityBuildings, Camps, Tents, Draws, Boats, Models, CloseVertices; public float BuildMs, PlantMs;
            /// <summary>Every footprint the build claimed (towns, localities, camps), in claim order (MapScene.KitFootprints).</summary>
            public IReadOnlyList<(Vector2 centre, float radius, string id)> Footprints = System.Array.Empty<(Vector2, float, string)>();
            public override string ToString() =>
                $"KITS buildings={Buildings} vertices={Vertices} triangles={Triangles} trees={Trees} route_segments={CorridorSegments} build_ms={BuildMs:0} plant_ms={PlantMs:0} " +
                $"localities={Localities} locality_buildings={LocalityBuildings} camps={Camps} tents={Tents} boats={Boats} draws={Draws} models={Models} close_vertices={CloseVertices}";
        }

        public static GameObject Build(IReadOnlyList<Settlement> settlements, Terrain terrain, GeoFrame frame, HexFile hexes, float elevMinM,
            Material kitMaterial, Material treeMaterial, TownModelSet? townModels, MapRuntimeAssets assets, GameObject markers, out Stats stats,
            IReadOnlyList<(List<Vector2> line, float halfWidth)>? corridors = null, Dictionary<string, Bounds[]>? columns = null) =>
            Build(settlements, new List<Settlement>(), new List<Localities.Area>(), terrain, frame, hexes, elevMinM, kitMaterial, treeMaterial,
                townModels, assets, markers, out stats, corridors, columns);

        /// <summary>As Build, with the 1948 localities and the Bedouin camp areas (content/map/localities_1948.yaml) drawn
        /// beside the game settlements: their buildings merged per tile (SettlementKits.Result.Tiles), one renderer each.</summary>
        public static GameObject Build(IReadOnlyList<Settlement> settlements, IReadOnlyList<Settlement> localities, IReadOnlyList<Localities.Area> camps,
            Terrain terrain, GeoFrame frame, HexFile hexes, float elevMinM,
            Material kitMaterial, Material treeMaterial, TownModelSet? townModels, MapRuntimeAssets assets, GameObject markers, out Stats stats,
            IReadOnlyList<(List<Vector2> line, float halfWidth)>? corridors = null, Dictionary<string, Bounds[]>? columns = null,
            IReadOnlyList<(Vector2 centre, float radius)>? tokens = null)
        {
            var st = stats = new Stats();
            var root = new GameObject(RootName);
            var ground = new KitGround(terrain, frame, hexes, elevMinM);
            // Buildings and trees stay off the railways and roads (RouteLayer.Corridors): every route, whatever its date,
            // since the kits are not rebuilt when a dated route (the Burma Road) appears.
            if (corridors != null) foreach (var (line, half) in corridors) ground.AddCorridor(line, half);
            stats.CorridorSegments = ground.CorridorSegments;
            if (tokens != null) ground.Tokens.AddRange(tokens);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var built = SettlementKits.Build(settlements, localities, camps, ground, out var placed, columns);   // columns: the labels' kit footprints (issue #214)
            foreach (var l in built.Claimed) ground.Lots.Add((l.centre, l.radius));
            stats.Footprints = new List<(Vector2, float, string)>(ground.Footprints);
            MeshRenderer Add(string name, Mesh mesh, bool count = true)
            {
                assets.Own(mesh);
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = kitMaterial;
                r.shadowCastingMode = ShadowCastingMode.On;
                if (count) { st.Vertices += mesh.vertexCount; st.Triangles += (int)mesh.GetIndexCount(0) / 3; st.Draws++; }
                return r;
            }
            // Issue #659 (owner ruling on #700: "Models up close only"): each kit mesh, and beside it its close mesh (without
            // its buildings, with the models' plinths), which TownModelLayer swaps in when the camera is close.
            var swaps = new List<(Renderer kit, Renderer? close)>();
            void AddKit(string name, Mesh mesh)
            {
                var kit = Add(name, mesh);
                Renderer? close = null;
                if (built.Close.TryGetValue(mesh.name, out var cm)) { close = Add(name + CloseSuffix, cm, count: false); st.CloseVertices += cm.vertexCount; }
                swaps.Add((kit, close));
            }
            foreach (var (s, mesh, n) in built.Towns) { AddKit(s.Id, mesh); stats.Buildings += n; }
            foreach (var (name, mesh) in built.Tiles) AddKit(name, mesh);
            // The boats (SeaCraft): off the coast and moored in the harbours the kits recorded.
            var crafts = SeaCraft.Place(ground, placed);
            if (crafts.Count > 0)
            {
                var km = new KitMesh();
                SeaCraft.Draw(crafts, ground, km);
                Add(SeaCraftName, km.ToMesh(SeaCraftName));
            }
            stats.Boats = crafts.Count;
            // Issue #659: the buildings' models, instanced up close (without a complete model list, the kits at every zoom).
            var townGo = new GameObject(TownModelsName);
            townGo.transform.SetParent(root.transform, false);
            var towns = townGo.AddComponent<TownModelLayer>();
            towns.Init(built.Models, townModels, swaps);
            stats.Models = built.Models.Count;
            stats.Localities = localities.Count; stats.LocalityBuildings = built.LocalityBuildings; stats.Camps = built.Camps; stats.Tents = built.Tents;
            stats.Buildings += built.LocalityBuildings + built.Tents;
            stats.BuildMs = (float)clock.Elapsed.TotalMilliseconds;
            Boot.DiagLog.BootPhase("kit buildings built");   // issue #321: the buildings, apart from the trees
            clock.Restart();
            var all = new List<Settlement>(settlements); all.AddRange(localities);
            var trees = Vegetation.Plant(all, ground);
            var veg = new GameObject(VegetationName);
            veg.transform.SetParent(root.transform, false);
            var layer = veg.AddComponent<VegetationLayer>();
            layer.Init(trees, treeMaterial, assets);
            stats.Trees = trees.Count;
            stats.PlantMs = (float)clock.Elapsed.TotalMilliseconds;
            // "kits" off shows the placeholder marker boxes again, so a settlement is always drawn by one or the other.
            void ShowKits(bool on)
            {
                if (root != null) foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = on;
                if (towns != null) towns.Draw = on;
                if (markers != null) foreach (var mr in markers.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = !on;
            }
            ShowKits(true);
            assets.OnTeardown(VisualToggles.Register("kits", ShowKits));
            assets.OnTeardown(VisualToggles.Register("trees", on => { if (layer != null) layer.Draw = on; }));
            return root;
        }
    }
}
