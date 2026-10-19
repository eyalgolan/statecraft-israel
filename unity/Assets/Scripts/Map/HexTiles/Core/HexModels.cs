#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>Model ids and what they draw. An id is a Resources path, with prefixes for the existing local libraries:
    ///   "tiles:&lt;path&gt;"   Resources/HexTiles/&lt;path&gt; (a contributor's own models, one folder per prototype)
    ///   "sandbox:&lt;name&gt;" Resources/HexSandbox/&lt;name&gt; (EZ-Tree trees, Poly Haven rocks, decimated Tripo building kit)
    ///   "proto:&lt;name&gt;"   Resources/SettlementPrototypes/&lt;name&gt; (Tripo heroes, figures, animals, vehicles)
    ///   "town:&lt;folder&gt;[@mid]" a shipped b*-v2 building (TownModels.Ids; TownModelSet's full mesh, or its baked mid
    ///                     level), which the sandbox building ids draw instead while the set is found (TownAliases)
    /// A trailing "*" picks among &lt;name&gt;, &lt;name&gt;_2, &lt;name&gt;_3 ... per placement. A missing model draws its stand-in, so a
    /// tile works on a machine without the local (git-ignored, pending-rights) models.</summary>
    public static class HexModels
    {
        public readonly struct Part
        {
            public readonly Mesh Mesh; public readonly int SubMesh; public readonly Material Material; public readonly Matrix4x4 Local;
            public Part(Mesh mesh, int subMesh, Material material, Matrix4x4 local) { Mesh = mesh; SubMesh = subMesh; Material = material; Local = local; }
        }

        public const string TownPrefix = "town:", MidSuffix = "@mid";
        /// <summary>Stands a Z-up town mesh up (mesh z to y, mesh y to -z), as SettlementKits.Fit does.</summary>
        public static readonly Matrix4x4 TownTurn = Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f));

        const string B01 = TownPrefix + "b01-white-city-v2", B02a = TownPrefix + "b02a-tel-aviv-corner-block-v2",
            B02b = TownPrefix + "b02b-tel-aviv-narrow-block-v2", B03a = TownPrefix + "b03a-jerusalem-arches-v2",
            B03b = TownPrefix + "b03b-jerusalem-external-stair-v2", B03c = TownPrefix + "b03c-jerusalem-plain-v2",
            B04b = TownPrefix + "b04b-haifa-loading-doors-v2",
            B05a = TownPrefix + "b05a-moshava-house-v2", B07 = TownPrefix + "b07-kibbutz-dining-hall-v2",
            B09a = TownPrefix + "b09a-kibbutz-communal-building-v2", B13a = TownPrefix + "b13a-netanya-two-storey-v2",
            B16 = TownPrefix + "b16-farmhouse-v2", B17 = TownPrefix + "b17-stone-courtyard-house-v2",
            B24 = TownPrefix + "b24-agricultural-barn-v2", B28 = TownPrefix + "b28-water-tower-v2", B32 = TownPrefix + "b32-port-cargo-shed-v2";

        /// <summary>The sandbox building ids drawn as the shipped b*-v2 town models when the set is found (Tal, 19 October
        /// 2026: the decimated sandbox kit had lost faces, so the ground showed through walls and windows). Each names its
        /// "town:" id and whether it is the same Tripo model; false is a remap, because its own model (b11, b21) is not in
        /// the set and is not pulled into a build. All take the full mesh (Tal, 19 October 2026): most baked mid levels have lost faces too,
        /// and the generator's in-between levels smear their textures (HexModelMeshTests' report), so the city and village
        /// tiles' budgets were raised to the full meshes (about 6,000 triangles each). A lighter level that keeps its UV
        /// seams is a follow-up.</summary>
        public static readonly (string id, string town, bool same)[] TownAliases =
        {
            ("sandbox:hq_white_city_hero", B01, true),
            ("sandbox:hq_white_city", B02a, true),
            ("sandbox:hq_white_city_2", B02b, true),
            ("sandbox:hq_white_city_3", B13a, true),
            ("sandbox:kit_white_city", B02a, true),
            ("sandbox:kit_white_city_2", B02b, true),
            ("sandbox:kit_white_city_3", B13a, true),
            ("sandbox:kit_white_city_4", B13a, false),   // b11 shikun block
            ("sandbox:kit_jaffa", B17, true),
            ("sandbox:kit_jaffa_2", B03a, false),   // b21 artisan workshop
            ("sandbox:kit_jaffa_3", B03c, true),
            ("sandbox:kit_haifa", B17, false),   // b04a: its full mesh has open wall triangles (1.33% see-through)
            ("sandbox:kit_haifa_2", B04b, true),
            ("sandbox:kit_haifa_3", B05a, true),
            ("sandbox:kit_jerusalem", B03a, true),
            ("sandbox:kit_jerusalem_2", B03b, true),
            ("sandbox:kit_jerusalem_3", B03c, true),
            ("sandbox:kit_jerusalem_4", B17, true),
            ("sandbox:kit_negev", B17, true),
            ("sandbox:kit_negev_2", B03c, true),
            ("sandbox:kit_negev_3", B03a, false),   // b21 artisan workshop
            ("sandbox:kit_moshava", B05a, true),
            ("sandbox:kit_moshava_2", B16, true),
            ("sandbox:kit_moshava_3", B24, true),
            ("sandbox:kit_kibbutz", B09a, true),
            ("sandbox:kit_kibbutz_2", B07, true),
            ("sandbox:kit_kibbutz_3", B24, true),
            ("sandbox:kit_kibbutz_4", B28, true),
            ("sandbox:kit_stone_village", B17, true),
            ("sandbox:kit_stone_village_2", B03c, true),
            ("sandbox:kit_port", B32, true),
        };

        /// <summary>Off draws the sandbox prefabs again (a before shot, or a test of the fallback); call
        /// <see cref="Clear"/> after switching.</summary>
        public static bool DrawTownModels = true;

        /// <summary>The cleared, tracked figure library (the game's Collection), one folder per figure with its -near (rigged,
        /// idle clip) and -far (static) prefabs.</summary>
        public const string CollectionFolder = "Assets/Art/Collection";

        /// <summary>The local proto figures the hex tiles and units place, drawn in the editor as their figure in the
        /// Collection (<see cref="CollectionFolder"/>) at the level given (Tal, 19 October 2026: the proto figures are our
        /// own decimations and show the ground through clothes, boots and faces, 2.7-6.5% of view rays; the Collection's
        /// are whole, HexUnitTests' report). The soldiers take the rigged near prefab (about 7,200 triangles, idle clip);
        /// the Tel Aviv townspeople the static far one (about 1,350, so the tile keeps its budget), but c01, whose far mesh
        /// is holed (1.8%), the near one. A player keeps the proto prefab, then the stand-in: the Collection streams from
        /// AssetBundles there, not Resources.</summary>
        public static readonly (string id, string figure, string level)[] FigureAliases =
        {
            ("proto:figure-palmach-scout-c12f-1948-v2", "figure-c12f-palmach-scout-v2", "near"),
            ("proto:figure-veteran-fighter-c07-1948-v2", "figure-c07-veteran-fighter-v2", "near"),
            ("proto:figure-young-recruit-c06-1948-v2", "figure-c06-young-recruit-v2", "near"),
            ("proto:figure-townsman-c02-1948-v2", "figure-c02-townsman-v2", "far"),
            ("proto:figure-woman-blue-dress-c10-1948-v2", "figure-c10-woman-blue-dress-v2", "far"),
            ("proto:figure-nurse-c13-1948-v2", "figure-c13-nurse-v2", "far"),
            ("proto:figure-arriving-man-c01-1948-v2", "figure-c01-arriving-man-v2", "near"),
            ("proto:figure-sabra-pioneer-c04-1948-v2", "figure-c04-sabra-pioneer-v3", "far"),
        };

        /// <summary>Off draws the proto figures again (a before shot, or a test of the fallback); call <see cref="Clear"/>
        /// after switching.</summary>
        public static bool DrawCollectionFigures = true;

        static readonly Dictionary<string, (string figure, string level)> figureOf =
            FigureAliases.ToDictionary(a => a.id, a => (a.figure, a.level), StringComparer.Ordinal);

        /// <summary>The Collection prefab a figure id draws in the editor (AssetDatabase), or null: in a player, without
        /// the Collection folder, or switched off.</summary>
        static GameObject? CollectionFigure(string id)
        {
#if UNITY_EDITOR
            if (DrawCollectionFigures && figureOf.TryGetValue(id, out var f))
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FigurePath(f.figure, f.level));
#endif
            return null;
        }

        /// <summary>A Collection figure's prefab path at a level ("near" or "far").</summary>
        public static string FigurePath(string figure, string level) => $"{CollectionFolder}/{figure}/{figure}-{level}.prefab";

        static readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        static readonly Dictionary<string, string[]> variants = new Dictionary<string, string[]>(StringComparer.Ordinal);
        static readonly Dictionary<string, string> townOf = TownAliases.ToDictionary(a => a.id, a => a.town, StringComparer.Ordinal);
        static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        static TownModelSet? townSet;
        static Transform? templateRoot;

        public static string PathOf(string id) =>
            id.StartsWith("tiles:", StringComparison.Ordinal) ? "HexTiles/" + id.Substring(6)
            : id.StartsWith("sandbox:", StringComparison.Ordinal) ? "HexSandbox/" + id.Substring(8)
            : id.StartsWith("proto:", StringComparison.Ordinal) ? "SettlementPrototypes/" + id.Substring(6)
            : "HexTiles/" + id;

        /// <summary>What <paramref name="id"/> draws: a town model's template (a "town:" id, or a sandbox id in
        /// <see cref="TownAliases"/>, while the set is found), else its Resources prefab, else null (its stand-in).</summary>
        public static GameObject? Prefab(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            // A cached template is made again if it was destroyed (Unity's fake null); a cached miss stays a miss.
            if (prefabs.TryGetValue(id, out var p) && (ReferenceEquals(p, null) || p != null)) return p;
            bool town = id.StartsWith(TownPrefix, StringComparison.Ordinal), alias = townOf.ContainsKey(id);
            var made = town || alias ? Town(town ? id : townOf[id]) : null;
            if (made != null) return prefabs[id] = made;
            var figure = CollectionFigure(id);
            if (figure != null) return prefabs[id] = figure;
            var prefab = town ? null : Resources.Load<GameObject>(PathOf(id));
            // Without the set (a player before the map scene has loaded it), not remembered, so the set is looked for again.
            if (!(DrawTownModels && (town || alias))) prefabs[id] = prefab;
            return prefab;
        }

        /// <summary>Forgets every model looked up, the town set and the "*" variants.</summary>
        public static void Clear() { prefabs.Clear(); variants.Clear(); townSet = null; }

        /// <summary>The town models' list (TownModelSet, complete): in the editor the asset itself, in a player the one
        /// the map scene loaded (MapScene references it; no Resources folder, so it adds nothing to a build). Null until
        /// found; a miss is not remembered.</summary>
        public static TownModelSet? TownSet()
        {
            if (townSet != null) return townSet;
#if UNITY_EDITOR
            townSet = UnityEditor.AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
#endif
            if (townSet == null) townSet = Resources.FindObjectsOfTypeAll<TownModelSet>().FirstOrDefault();
            if (townSet != null && townSet.Missing() != null) townSet = null;
            return townSet;
        }

        /// <summary>The template a "town:" id draws: an object under a hidden, inactive root holding the set's mesh (full,
        /// or the baked mid level for "@mid") and material, stood up from Z-up. The template's own object is active, so a
        /// copy of it (Parts) has renderer bounds; the inactive root keeps the template itself from drawing. Parts puts it
        /// in the unit frame (footprint centred, base at y = 0).</summary>
        static GameObject? Town(string town)
        {
            if (!DrawTownModels) return null;
            if (templates.TryGetValue(town, out var made) && made != null) return made;
            var set = TownSet();
            if (set == null) return null;
            bool mid = town.EndsWith(MidSuffix, StringComparison.Ordinal);
            int i = Array.IndexOf(TownModels.Ids, town.Substring(TownPrefix.Length, town.Length - TownPrefix.Length - (mid ? MidSuffix.Length : 0)));
            if (i < 0) return null;
            if (templateRoot == null)
            {
                var root = new GameObject("HexModels town templates") { hideFlags = HideFlags.HideAndDontSave };
                root.SetActive(false);
                templateRoot = root.transform;
            }
            var go = new GameObject(town.Substring(TownPrefix.Length)) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(templateRoot, false);
            var child = new GameObject("mesh", typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.HideAndDontSave };
            child.transform.SetParent(go.transform, false);
            child.transform.localRotation = TownTurn.rotation;
            child.GetComponent<MeshFilter>().sharedMesh = mid ? set.mid[i] : set.meshes[i];
            child.GetComponent<MeshRenderer>().sharedMaterial = set.materials[i];
            return templates[town] = go;
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
            var b = RendererBounds(rs[0]); foreach (var r in rs) b.Encapsulate(RendererBounds(r));
            return b;
        }

        /// <summary>A renderer's world bounds. In the editor a skinned renderer without exposed bones (the Collection's near
        /// figures: their rig hides its transforms) reports nearly empty bounds until it has drawn, which scaled a squad to
        /// hundreds of kilometres: measure its baked vertices instead (BakeMesh already applies the scale, so only the
        /// renderer's position and rotation place them).</summary>
        static Bounds RendererBounds(Renderer r)
        {
#if UNITY_EDITOR
            if (r is SkinnedMeshRenderer s && s.sharedMesh != null && s.bones.Length == 0)
            {
                var baked = new Mesh();
                try
                {
                    s.BakeMesh(baked, false);
                    var v = baked.vertices;
                    if (v.Length > 0)
                    {
                        var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
                        var b = new Bounds(m.MultiplyPoint3x4(v[0]), Vector3.zero);
                        foreach (var p in v) b.Encapsulate(m.MultiplyPoint3x4(p));
                        return b;
                    }
                }
                finally { Object.DestroyImmediate(baked); }
            }
#endif
            return r.bounds;
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
