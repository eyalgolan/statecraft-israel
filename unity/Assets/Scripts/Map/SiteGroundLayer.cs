#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Routes;
using Statecraft.HexTiles;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>#2877: a plowed field or a drained swamp shows on its hex's ground (the owner on #2812, "Ground change only": no
    /// kit and no pill, SiteLayer). A patch of parcels draped on the terrain, drawn by Statecraft/SiteGround over the baked ground,
    /// redrawn from State.Sites as <see cref="Collection.HulaPapyrusLayer"/> is, so a loaded save shows its own sites.
    /// Why a layer over the terrain and not a repaint of <c>_ScBiomeTex</c> (MapBuilder.BakeBiomeTexture): that texture is one
    /// map-wide upload, so a repaint would stall End turn; this builds one small mesh, for the site that changed only.
    /// A site's first draw would compile the shader, on the End turn that finishes it, so a zero-area mesh draws from the first
    /// frame (<see cref="WarmName"/>); it costs one draw call and shows nothing.
    /// The look is a design value, no field layout or crop is sourced: the mesh carries the site's own seeded axes, the shader
    /// paints the parcels. A hex with a field and the swamp it was drained from (hula, hula_field) shows the field. The patch
    /// keeps off water, steep ground and the heart of every town's footprint and fades out short of the hex's edge, so the grid and the region borders stay
    /// on top. Presentation only: what is built, and when, is the sim's (R-SITE-001 to R-SITE-003, R-SWAMP-001).</summary>
    public sealed class SiteGroundLayer : MonoBehaviour
    {
        public const string Name = "SiteGround", WarmName = "Warm", ToggleName = "siteground", MaterialResource = "Map/SiteGround";
        /// <summary>The look, in the mesh's vertex colour's red (Statecraft/SiteGround): a plowed field, or the drained swamp.</summary>
        public const int Field = 0, Drained = 1;
        // Design values. StepM: the mesh's spacing, a few of the heightmap's samples, enough to follow the ground and the mask.
        // LiftM: over the highest ground round a vertex, low enough not to bury a house's foot (a road's 20 m would).
        // EdgeInsetM, EdgeFadeM: the patch starts this far inside the hex's edge and is whole this far in, so the parcels
        // the shader drops along the way give it a field's outline, not a hexagon's. ClearShare, ClearFadeM: the same round a
        // town: from this share of its footprint (a footprint's rim is sparse: the whole disc left a round hole in the fields).
        // SteepM: no field on ground that rises this much (world m, exaggerated) round a vertex, about a 28% drawn slope.
        public const float StepM = 160f, LiftM = 6f, EdgeInsetM = 250f, EdgeFadeM = 1500f, ClearShare = 0.7f, ClearFadeM = 1200f, SteepM = 45f;
        const float AxisStepDeg = 15f, OffsetRangeM = 8000f;
        /// <summary>The budget of a site's mesh: a 4.4 km hex at <see cref="StepM"/>, with its rim, comes to about 2,100 vertices.</summary>
        public const int MaxVertices = 2500;

        readonly Dictionary<string, (GameObject Go, Mesh Mesh, int Look)> shown = new Dictionary<string, (GameObject, Mesh, int)>();
        Mesh? warm;
        KitGround? ground;
        MapScene map = null!;

        /// <summary>The site ids drawn now.</summary>
        public IReadOnlyCollection<string> Shown => shown.Keys;
        /// <summary>How many meshes this layer has built (tests: an unchanged turn builds none).</summary>
        public int MeshesBuilt { get; private set; }
        public Mesh? MeshOf(string site) => shown.TryGetValue(site, out var s) ? s.Mesh : null;

        public static Material MaterialFor() => Resources.Load<Material>(MaterialResource);

        /// <summary>The built field and drained swamp sites, one per hex (the field over the swamp it was drained from), in
        /// site-id order.</summary>
        public static List<(Site Site, int Look)> Wanted(Content content, IEnumerable<SiteWork> sites)
        {
            var byHex = new Dictionary<(int, int), (Site Site, int Look)>();
            foreach (var w in sites.Where(w => w.BuiltOn != null).OrderBy(w => w.Site, StringComparer.Ordinal))
            {
                var site = content.Sites.FirstOrDefault(s => s.Id == w.Site);
                if (site == null) continue;
                int look = site.Kinds.Contains(Projects.FieldKind) ? Field : site.Kinds.Contains(Projects.SwampKind) ? Drained : -1;
                if (look < 0) continue;
                var hex = (site.Q, site.R);
                if (!byHex.TryGetValue(hex, out var had) || look == Field) byHex[hex] = (site, look);
            }
            return byHex.Values.OrderBy(v => v.Site.Id, StringComparer.Ordinal).ToList();
        }

        /// <summary>The layer on <paramref name="map"/>, made on the first call with its warm mesh, showing what
        /// <paramref name="sites"/> ask for. Builds only what changed, so it can run on every Changed. Null on the hex-tile map,
        /// whose boards have no such ground to draw on.</summary>
        public static SiteGroundLayer? ShowAt(MapScene map, Content content, IEnumerable<SiteWork> sites)
        {
            if (map == null || map.Terrain == null || map.Hexes == null || content == null || HexTilesLook.Requested()) return null;
            var layer = map.Terrain.transform.Find(Name)?.GetComponent<SiteGroundLayer>();
            if (layer == null)
            {
                var go = new GameObject(Name);
                go.transform.SetParent(map.Terrain.transform, false);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the meshes' vertices are in world space
                layer = go.AddComponent<SiteGroundLayer>();
                layer.map = map;
                layer.AddWarm();
                var made = layer;
                map.Terrain.GetComponent<MapRuntimeAssets>().OnTeardown(VisualToggles.Register(ToggleName, on => { if (made != null) made.gameObject.SetActive(on); }));
            }
            layer.Show(Wanted(content, sites));
            return layer;
        }

        void AddWarm()
        {
            warm = new Mesh { name = WarmName };
            var centre = new Vector3(0.5f * map.Frame.WidthM, 0f, 0.5f * map.Frame.LengthM);
            warm.vertices = new[] { centre, centre, centre };
            warm.normals = new[] { Vector3.up, Vector3.up, Vector3.up };
            warm.triangles = new[] { 0, 1, 2 };
            warm.bounds = new Bounds(centre, new Vector3(map.Frame.WidthM, 1e6f, map.Frame.LengthM));   // never culled
            Add(WarmName, warm);
        }

        GameObject Add(string name, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialFor();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
            return go;
        }

        void Show(List<(Site Site, int Look)> wanted)
        {
            foreach (var id in shown.Where(s => !wanted.Any(w => w.Site.Id == s.Key && w.Look == s.Value.Look)).Select(s => s.Key).ToList())
            {
                var (go, mesh, _) = shown[id];
                shown.Remove(id);
                Drop(go); Drop(mesh);
            }
            foreach (var (site, look) in wanted)
            {
                if (shown.ContainsKey(site.Id)) continue;
                ground ??= PlacementLayer.CampGround(map);
                var mesh = BuildMesh(map, ground, site, look);
                MeshesBuilt++;
                shown[site.Id] = (Add(site.Id, mesh), mesh, look);
            }
        }

        static void Drop(UnityEngine.Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

        void OnDestroy()
        {
            foreach (var s in shown.Values) if (s.Mesh != null) Drop(s.Mesh);
            shown.Clear();
            if (warm != null) Drop(warm);
        }

        /// <summary>A stable hash of a site's id (never string.GetHashCode: it differs between runs on some runtimes).</summary>
        static uint Hash(string id)
        {
            uint h = 2166136261u;
            foreach (char c in id) h = (h ^ c) * 16777619u;
            return h;
        }

        /// <summary>The distance (m) from <paramref name="p"/>, relative to the hex's centre, to the edge of a pointy-top hex
        /// (HexMath) of corner radius <paramref name="sizeM"/>; negative outside.</summary>
        public static float EdgeDistance(Vector2 p, float sizeM)
        {
            float a = Mathf.Abs(p.x), b = Mathf.Abs(0.5f * p.x + 0.8660254f * p.y), c = Mathf.Abs(0.5f * p.x - 0.8660254f * p.y);
            return 0.8660254f * sizeM - Mathf.Max(a, Mathf.Max(b, c));
        }

        /// <summary>The site's patch: a grid over its hex in world space, each vertex on the ground. Its colour's alpha is the
        /// mask (0 off the hex's inside, on water and on a town), red the look; its uv is the site's own seeded axes, in metres.</summary>
        static Mesh BuildMesh(MapScene map, KitGround ground, Site site, int look)
        {
            var hex = map.Hexes.hexes.First(h => h.q == site.Q && h.r == site.R);
            var c3 = map.Frame.ToWorld(hex.lon, hex.lat, 0f);
            var centre = new Vector2(c3.x, c3.z);
            float sizeM = map.Hexes.hex_size_km * 1000f;
            uint seed = Hash(site.Id);
            float axis = Mathf.Deg2Rad * (seed % 12u) * AxisStepDeg;
            var along = new Vector2(Mathf.Cos(axis), Mathf.Sin(axis));
            var across = new Vector2(-along.y, along.x);
            var offset = new Vector2(((seed >> 8) & 0xffu) / 255f, ((seed >> 16) & 0xffu) / 255f) * OffsetRangeM;
            var terrain = map.Terrain; var data = terrain.terrainData; var origin = terrain.transform.position;
            int n = Mathf.CeilToInt(sizeM / StepM), side = 2 * n + 1;
            var index = new int[side * side];
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var colours = new List<Color32>();
            for (int j = 0; j < side; j++)
                for (int i = 0; i < side; i++)
                {
                    index[j * side + i] = -1;
                    var rel = new Vector2((i - n) * StepM, (j - n) * StepM);
                    if (EdgeDistance(rel, sizeM) < -StepM) continue;
                    var p = centre + rel;
                    if (!ground.Inside(p.x, p.y)) continue;
                    index[j * side + i] = verts.Count;
                    float y = RouteLayer.Ground(data, origin, p), low = y;
                    foreach (var d in new[] { new Vector2(StepM, 0f), new Vector2(-StepM, 0f), new Vector2(0f, StepM), new Vector2(0f, -StepM) })
                    {
                        float around = RouteLayer.Ground(data, origin, p + 0.5f * d);
                        y = Mathf.Max(y, around); low = Mathf.Min(low, around);
                    }
                    verts.Add(new Vector3(p.x, y + LiftM, p.y));
                    normals.Add(data.GetInterpolatedNormal((p.x - origin.x) / data.size.x, (p.y - origin.z) / data.size.z));
                    uvs.Add(new Vector2(Vector2.Dot(rel, along), Vector2.Dot(rel, across)) + offset);
                    float mask = Mathf.Clamp01((EdgeDistance(rel, sizeM) - EdgeInsetM) / EdgeFadeM);
                    if (mask > 0f && (y - low > SteepM || ground.Hex(p.x, p.y) != (site.Q, site.R) || ground.Water(p.x, p.y))) mask = 0f;
                    if (mask > 0f)
                        foreach (var f in map.KitFootprints)
                            mask = Mathf.Min(mask, Mathf.Clamp01(((f.centre - p).magnitude - ClearShare * f.radius) / ClearFadeM));
                    colours.Add(new Color32((byte)(look == Drained ? 255 : 0), 0, 0, (byte)Mathf.RoundToInt(255f * mask)));
                }
            var tris = new List<int>();
            for (int j = 0; j + 1 < side; j++)
                for (int i = 0; i + 1 < side; i++)
                {
                    int a = index[j * side + i], b = index[j * side + i + 1], c = index[(j + 1) * side + i], d = index[(j + 1) * side + i + 1];
                    if (a < 0 || b < 0 || c < 0 || d < 0) continue;
                    if (colours[a].a == 0 && colours[b].a == 0 && colours[c].a == 0 && colours[d].a == 0) continue;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { name = Name + " " + site.Id };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetColors(colours);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
