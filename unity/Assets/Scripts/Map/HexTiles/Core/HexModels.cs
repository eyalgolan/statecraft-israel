#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>Model ids and what they draw. An id is a Resources path, with prefixes for the existing local libraries:
    ///   "tiles:&lt;path&gt;"   Resources/HexTiles/&lt;path&gt; (a contributor's own models, one folder per prototype)
    ///   "sandbox:&lt;name&gt;" Resources/HexSandbox/&lt;name&gt; (EZ-Tree trees, Poly Haven rocks, decimated Tripo building kit)
    ///   "proto:&lt;name&gt;"   Resources/SettlementPrototypes/&lt;name&gt; (Tripo heroes, figures, animals, vehicles)
    /// A trailing "*" picks among &lt;name&gt;, &lt;name&gt;_2, &lt;name&gt;_3 ... per placement. A missing model draws its stand-in, so a
    /// tile works on a machine without the local (git-ignored, pending-rights) models.</summary>
    public static class HexModels
    {
        public readonly struct Part
        {
            public readonly Mesh Mesh; public readonly int SubMesh; public readonly Material Material; public readonly Matrix4x4 Local;
            public Part(Mesh mesh, int subMesh, Material material, Matrix4x4 local) { Mesh = mesh; SubMesh = subMesh; Material = material; Local = local; }
        }

        public const string TownPrefix = "town:";
        /// <summary>Stands a Z-up town mesh up (mesh z to y, mesh y to -z), as SettlementKits.Fit does.</summary>
        public static readonly Matrix4x4 TownTurn = Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f));
        public static readonly (string id, string town, bool same)[] TownAliases = Array.Empty<(string, string, bool)>();

        static readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        static readonly Dictionary<string, string[]> variants = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public static string PathOf(string id) =>
            id.StartsWith("tiles:", StringComparison.Ordinal) ? "HexTiles/" + id.Substring(6)
            : id.StartsWith("sandbox:", StringComparison.Ordinal) ? "HexSandbox/" + id.Substring(8)
            : id.StartsWith("proto:", StringComparison.Ordinal) ? "SettlementPrototypes/" + id.Substring(6)
            : "HexTiles/" + id;

        public static GameObject? Prefab(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (prefabs.TryGetValue(id, out var p)) return p;
            return prefabs[id] = Resources.Load<GameObject>(PathOf(id));
        }

        /// <summary>The concrete id for a placement: a "*" id picks a present variant by the placement's position.</summary>
        public static string Resolve(string id, Vector3 at)
        {
            if (!id.EndsWith("*", StringComparison.Ordinal)) return id;
            if (!variants.TryGetValue(id, out var names))
            {
                var stem = id.Substring(0, id.Length - 1);
                var list = new List<string>();
                for (int v = 1; v < 32; v++) { var name = v == 1 ? stem : $"{stem}_{v}"; if (Prefab(name) == null) break; list.Add(name); }
                variants[id] = names = list.ToArray();
            }
            if (names.Length == 0) return id.Substring(0, id.Length - 1);
            uint h = unchecked((uint)(Mathf.FloorToInt(at.x) * 73856093 ^ Mathf.FloorToInt(at.z) * 19349663));
            return names[h % (uint)names.Length];
        }

        public static bool Skinned(GameObject prefab) => prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;

        /// <summary>The prefab's static meshes in the placement's unit frame (footprint centred, base at y = 0) for
        /// <paramref name="fit"/>: Width and Height scale uniformly by the footprint's larger side or the height, Stretch
        /// by each axis. Skinned prefabs are instantiated instead (HexBoardRenderer).</summary>
        public static Part[] Parts(GameObject prefab, Fit fit)
        {
            var go = Object.Instantiate(prefab);
            try
            {
                var b = Bounds(go);
                var size = new Vector3(Mathf.Max(b.size.x, 1e-4f), Mathf.Max(b.size.y, 1e-4f), Mathf.Max(b.size.z, 1e-4f));
                var s = Normaliser(size, fit);
                var norm = Matrix4x4.Scale(s) * Matrix4x4.Translate(new Vector3(-b.center.x, -b.min.y, -b.center.z));
                return go.GetComponentsInChildren<MeshFilter>()
                    .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null)
                    .SelectMany(f => f.GetComponent<MeshRenderer>().sharedMaterials.Select((m, i) => (f, m, i)))
                    .Where(x => x.m != null && x.i < x.f.sharedMesh.subMeshCount)
                    .Select(x => new Part(x.f.sharedMesh, x.i, x.m, norm * x.f.transform.localToWorldMatrix)).ToArray();
            }
            finally { Object.DestroyImmediate(go); }
        }

        public static Vector3 Normaliser(Vector3 size, Fit fit) => fit switch
        {
            Fit.Width => Vector3.one / Mathf.Max(size.x, size.z),
            Fit.Height => Vector3.one / size.y,
            _ => new Vector3(1f / size.x, 1f / size.y, 1f / size.z),
        };

        public static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Triangles one placement of <paramref name="id"/> draws (its stand-in's when the model is missing).</summary>
        public static int Triangles(string id, StandIn standIn, Vector3 at)
        {
            var prefab = Prefab(Resolve(id, at));
            if (prefab == null) return StandInTriangles(standIn);
            int t = 0;
            foreach (var f in prefab.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh != null) t += IndexTriangles(f.sharedMesh);
            foreach (var s in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.sharedMesh != null) t += IndexTriangles(s.sharedMesh);
            return t;
        }

        // --- stand-ins: HexStandIns' unit shapes, proportioned per kind ---

        static readonly Dictionary<StandIn, (Mesh mesh, string colour, Matrix4x4 local)[]> standIns = new Dictionary<StandIn, (Mesh, string, Matrix4x4)[]>();

        public static (Mesh mesh, string colour, Matrix4x4 local)[] StandInParts(StandIn kind)
        {
            // Editor scene changes can release procedural meshes while this static cache survives.
            if (standIns.TryGetValue(kind, out var p) && p.All(part => part.mesh != null)) return p;
            (Mesh, string, Matrix4x4)[] From(HexStandIns.Shape shape, Vector3 scale) =>
                HexStandIns.Build(shape).Select(part => (part.mesh, part.colour, Matrix4x4.Scale(scale))).ToArray();
            return standIns[kind] = kind switch
            {
                StandIn.Box => From(HexStandIns.Shape.Box, new Vector3(1f, 0.65f, 1f)),
                StandIn.Tree => From(HexStandIns.Shape.Olive, new Vector3(0.8f, 1f, 0.8f)),
                StandIn.Pine => From(HexStandIns.Shape.Cypress, new Vector3(0.45f, 1f, 0.45f)),
                StandIn.Palm => From(HexStandIns.Shape.Palm, new Vector3(0.6f, 1f, 0.6f)),
                StandIn.Rock => From(HexStandIns.Shape.Rock, new Vector3(1f, 0.6f, 1f)),
                StandIn.Figure => new[] { (Resources.GetBuiltinResource<Mesh>("Capsule.fbx"), "hull", Matrix4x4.Translate(new Vector3(0f, 0.5f, 0f)) * Matrix4x4.Scale(new Vector3(0.3f, 0.5f, 0.3f))) },
                _ => From(HexStandIns.Shape.Field, Vector3.one),
            };
        }

        public static int StandInTriangles(StandIn kind) => StandInParts(kind).Sum(p => IndexTriangles(p.mesh));

        /// <summary>Triangles from index counts, which a player can read on any mesh (mesh.triangles needs Read/Write).</summary>
        public static int IndexTriangles(Mesh m)
        {
            // A player refuses even index counts on meshes imported without Read/Write; the counts only feed editor stats.
            if (!m.isReadable && !Application.isEditor) return 0;
            long n = 0;
            for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i);
            return (int)(n / 3);
        }
    }
}
