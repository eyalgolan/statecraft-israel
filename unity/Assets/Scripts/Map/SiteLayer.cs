#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>GDD Phase 1 PR 8 (#381): the designated sites the player builds on (sites.yaml, State.Sites).
    /// <list type="bullet">
    /// <item>A built site is drawn as a rural kit on its hex (SettlementKits, as a kibbutz: the one archetype a new yishuv has
    /// in Phase 1), named <see cref="Prefix"/> + its id. With the map's "kits" toggle off, it is the kibbutz marker box, as the
    /// settlements are (KitLayer).</item>
    /// <item>A site being built is a scaffold marker: an open frame of raw timber on its hex, named Prefix + id +
    /// <see cref="ScaffoldSuffix"/>.</item>
    /// <item>An available site, with no record, draws nothing.</item>
    /// </list>
    /// Render replaces what it drew before, as PlacementLayer does, and keeps each kit's columns (<see cref="Columns"/>) for
    /// the map's name pills. Presentation only: what is built, and when, is the sim's (R-SITE-001 to R-SITE-003).</summary>
    public sealed class SiteLayer : MonoBehaviour
    {
        public const string Prefix = "site_", ScaffoldSuffix = "_scaffold";
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        /// <summary>The scaffold's light raw timber, and its size (a post's height, the frame's side and a post's thickness, m):
        /// an open frame as wide as a small kibbutz kit and thick enough to read at the play distance (the first captures
        /// showed 70 m posts as a speck), but open, so a site under way never reads as a founded place.</summary>
        static readonly Color Timber = new Color(0.86f, 0.72f, 0.48f);
        const float PostM = 450f, SideM = 1100f, PostThickM = 140f;

        readonly Dictionary<string, Bounds[]> columns = new Dictionary<string, Bounds[]>();
        /// <summary>Each built site's kit columns (KitMesh.Columns), by site id, as MapScene.TryKitColumns gives a town's.</summary>
        public IReadOnlyDictionary<string, Bounds[]> Columns => columns;
        readonly List<Settlement> places = new List<Settlement>();
        /// <summary>The built sites as places (Prefix + id, the site's name, a kibbutz on its hex), for their name pills
        /// (MapLabelsPresenter.SetSites).</summary>
        public IReadOnlyList<Settlement> Places => places;

        List<SiteWork>? lastSites; Content? lastContent; MapScene? lastMap; bool kitsShown;

        void LateUpdate()
        {
            // A flip of the "kits" toggle redraws (as PlacementLayer does), so a built site is its kit or its box, never both.
            if (lastSites != null && lastContent != null && lastMap != null && lastMap.KitsShown != kitsShown)
                Render(lastSites, lastContent, lastMap);
        }

        public void Render(IEnumerable<SiteWork> sites, Content content, MapScene map)
        {
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                var filter = old.GetComponent<MeshFilter>();
                var own = filter != null && filter.sharedMesh != cube ? filter.sharedMesh : null;   // a kit's mesh is its own
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); if (own != null) Destroy(own); }
                else { DestroyImmediate(old); if (own != null) DestroyImmediate(own); }
            }
            columns.Clear(); places.Clear();
            var list = sites.ToList();
            lastSites = list; lastContent = content; lastMap = map; kitsShown = map.KitsShown;
            var block = new MaterialPropertyBlock();
            var built = new List<Settlement>();
            foreach (var w in list)
            {
                var site = content.Sites.FirstOrDefault(x => x.Id == w.Site);
                var hex = site == null ? null : content.HexAt(site.Q, site.R);
                if (site == null || hex == null) continue;
                var at = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
                if (w.BuiltOn == null) { Scaffold(Prefix + w.Site + ScaffoldSuffix, at, cube, map, block); continue; }
                built.Add(new Settlement { Id = Prefix + w.Site, Name = site.Name, NameHe = site.NameHe, Type = "kibbutz", Lon = hex.Lon, Lat = hex.Lat });
            }
            places.AddRange(built);
            var drawn = kitsShown && built.Count > 0 ? Kits(built, map) : new HashSet<string>();
            foreach (var s in built.Where(s => !drawn.Contains(s.Id)))
            {
                // No kit (toggle off, or no free ground): the kibbutz marker box, as SettlementPlacer draws the settlements.
                var at = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
                var h = SettlementPlacer.MarkerHeightM(s.Type);
                var box = Part(s.Id, cube, map, block, SettlementPlacer.MarkerColour(s.Type));
                box.transform.position = at + Vector3.up * h / 2;
                box.transform.localScale = new Vector3(h, h, h);
            }
        }

        /// <summary>Draws the built sites as kits (one object each, Kit.mat), keeps their columns, and returns the ids drawn.</summary>
        HashSet<string> Kits(List<Settlement> built, MapScene map)
        {
            var kits = SettlementKits.Build(built, PlacementLayer.CampGround(map), out _, columns);
            var drawn = new HashSet<string>();
            foreach (var (s, mesh, _) in kits)
            {
                if (mesh == null) continue;
                var go = new GameObject(s.Id);
                go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the kit's vertices are in world space
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = map.KitMaterial;
                r.shadowCastingMode = ShadowCastingMode.On;
                drawn.Add(s.Id);
            }
            return drawn;
        }

        /// <summary>The scaffold: four timber posts at the corners of a square, under one parent named <paramref name="name"/>.</summary>
        void Scaffold(string name, Vector3 at, Mesh cube, MapScene map, MaterialPropertyBlock block)
        {
            var frame = new GameObject(name);
            frame.transform.SetParent(transform, false);
            frame.transform.position = at;
            foreach (var (dx, dz) in new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) })
            {
                var post = Part(name + "_post", cube, map, block, Timber);
                post.transform.SetParent(frame.transform, false);
                post.transform.localPosition = new Vector3(dx * SideM / 2, PostM / 2, dz * SideM / 2);
                post.transform.localScale = new Vector3(PostThickM, PostM, PostThickM);
            }
            // a beam along each top edge, so it reads as a frame from any side
            foreach (var (x, z, alongX) in new[] { (0f, -SideM / 2, true), (0f, SideM / 2, true), (-SideM / 2, 0f, false), (SideM / 2, 0f, false) })
            {
                var beam = Part(name + "_beam", cube, map, block, Timber);
                beam.transform.SetParent(frame.transform, false);
                beam.transform.localPosition = new Vector3(x, PostM, z);
                beam.transform.localScale = alongX ? new Vector3(SideM + PostThickM, PostThickM, PostThickM) : new Vector3(PostThickM, PostThickM, SideM + PostThickM);
            }
        }

        GameObject Part(string name, Mesh cube, MapScene map, MaterialPropertyBlock block, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = cube;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = map.MarkerMaterial;
            block.SetColor(BaseColor, colour); r.SetPropertyBlock(block);
            return go;
        }
    }
}
