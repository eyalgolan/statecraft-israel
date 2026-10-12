#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Builds the art spike's procedural layer under a returned "Kits" root: one static mesh per settlement (its
    /// buildings, merged: one draw each, culled by its bounds) drawn with the editor-made Kit.mat, and the instanced trees
    /// (<see cref="VegetationLayer"/>) drawn with Tree.mat. Meshes are owned by the map's MapRuntimeAssets, so a rebuild
    /// frees them. Hides the marker boxes under <paramref name="markers"/> while the kits show. Registers the "kits" and
        /// "trees" visual toggles, so the frame-time profile prices each ("kits" off shows the markers again). Fills
    /// <paramref name="columns"/>, when given, with each kit's columns (world space: the kits sit at the root's origin).</summary>
    public static class KitLayer
    {
        public const string RootName = "Kits", VegetationName = "Vegetation";

        public sealed class Stats
        {
            public int Buildings, Vertices, Triangles, Trees, CorridorSegments; public float BuildMs, PlantMs;
            public override string ToString() =>
                $"KITS buildings={Buildings} vertices={Vertices} triangles={Triangles} trees={Trees} route_segments={CorridorSegments} build_ms={BuildMs:0} plant_ms={PlantMs:0}";
        }

        public static GameObject Build(IReadOnlyList<Settlement> settlements, Terrain terrain, GeoFrame frame, HexFile hexes, float elevMinM,
            Material kitMaterial, Material treeMaterial, MapRuntimeAssets assets, GameObject markers, out Stats stats,
            IReadOnlyList<(List<Vector2> line, float halfWidth)>? corridors = null, Dictionary<string, Bounds[]>? columns = null)
        {
            stats = new Stats();
            var root = new GameObject(RootName);
            var ground = new KitGround(terrain, frame, hexes, elevMinM);
            // Buildings and trees stay off the railways and roads (RouteLayer.Corridors): every route, whatever its date,
            // since the kits are not rebuilt when a dated route (the Burma Road) appears.
            if (corridors != null) foreach (var (line, half) in corridors) ground.AddCorridor(line, half);
            stats.CorridorSegments = ground.CorridorSegments;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var built = SettlementKits.Build(settlements, ground, out var lots, columns);   // the labels' kit footprints (issue #214)
            foreach (var l in lots) ground.Lots.Add((l.centre, l.radius));
            foreach (var (s, mesh, n) in built)
            {
                assets.Own(mesh);
                var go = new GameObject(s.Id);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = kitMaterial;
                r.shadowCastingMode = ShadowCastingMode.On;
                stats.Buildings += n; stats.Vertices += mesh.vertexCount; stats.Triangles += (int)mesh.GetIndexCount(0) / 3;
            }
            stats.BuildMs = (float)clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var trees = Vegetation.Plant(settlements, ground);
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
                if (markers != null) foreach (var mr in markers.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = !on;
            }
            ShowKits(true);
            assets.OnTeardown(VisualToggles.Register("kits", ShowKits));
            assets.OnTeardown(VisualToggles.Register("trees", on => { if (layer != null) layer.Draw = on; }));
            return root;
        }
    }
}
