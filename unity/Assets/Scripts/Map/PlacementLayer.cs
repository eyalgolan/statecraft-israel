#nullable enable
using System.Collections.Generic;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>Draws the player's transit camps and housing projects as marker boxes on their hex centres (placeholder art
    /// like the M2 settlement markers; per-era prefabs are M4). Uses the editor-made marker material with a per-kind
    /// colour through a MaterialPropertyBlock (no runtime Shader.Find). Render replaces what it drew before.
    /// With the map's kits built, a transit camp is drawn as a ma'abara kit instead (SettlementKits.Maabarot, issue #32),
    /// built here when the camp is placed rather than at map build (the boot budget, #321); a camp that finds no free
    /// ground keeps its box. With the "kits" toggle off every camp is its box again, as the settlements are (KitLayer).</summary>
    public sealed class PlacementLayer : MonoBehaviour
    {
        static readonly Dictionary<string, (Color colour, float size)> Look = new Dictionary<string, (Color, float)>
        {
            [MapActions.TransitCamp] = (new Color(0.62f, 0.58f, 0.50f), 700f),
            [MapActions.Housing] = (new Color(0.95f, 0.94f, 0.90f), 900f),
        };
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // The last Render's inputs and the "kits" toggle as it drew them: a flip of the toggle redraws (MapScene.KitsShown is
        // polled, as the map labels poll it), so a camp is always drawn by its kit or its box, never by both or neither.
        List<Placement>? lastPlacements; Content? lastContent; MapScene? lastMap; bool kitsShown;

        void LateUpdate()
        {
            if (lastPlacements != null && lastContent != null && lastMap != null && lastMap.KitsShown != kitsShown)
                Render(lastPlacements, lastContent, lastMap);
        }

        public void Render(IEnumerable<Placement> placements, Content content, MapScene map)
        {
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                // A camp's kit mesh is its own (the boxes share the built-in cube): it goes with its object.
                var filter = old.GetComponent<MeshFilter>();
                var own = filter != null && filter.sharedMesh != cube ? filter.sharedMesh : null;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); if (own != null) Destroy(own); }
                else { DestroyImmediate(old); if (own != null) DestroyImmediate(own); }
            }
            var list = new List<Placement>(placements);
            lastPlacements = list; lastContent = content; lastMap = map; kitsShown = map.KitsShown;
            var holes = new List<(Vector2 centre, float radius)>();
            var kits = kitsShown ? Camps(list, content, map, holes) : new HashSet<string>();
            // The kit trees planted at map build are left out under a camp's kit while it stands (as under the unit figures,
            // issue #249), at draw time; a camp drawn as its box, or gone, gives them back.
            Units.UnitShowcase.HideTrees(map, "placements", holes);
            var block = new MaterialPropertyBlock();
            foreach (var p in list)
            {
                var hex = content.HexAt(p.Q, p.R);
                if (hex == null || !Look.TryGetValue(p.Action, out var look) || kits.Contains($"{p.Action}_{p.Q}_{p.R}")) continue;
                var at = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
                var box = new GameObject($"{p.Action}_{p.Q}_{p.R}");
                box.transform.SetParent(transform, false);
                box.AddComponent<MeshFilter>().sharedMesh = cube;
                var r = box.AddComponent<MeshRenderer>();
                r.sharedMaterial = map.MarkerMaterial;
                block.SetColor(BaseColor, look.colour); r.SetPropertyBlock(block);
                box.transform.position = at + Vector3.up * look.size / 2;
                box.transform.localScale = new Vector3(look.size * 1.6f, look.size, look.size);
            }
        }

        /// <summary>Draws every transit camp of <paramref name="list"/> as a ma'abara kit (one object each, named as its box
        /// would be, drawn with Kit.mat); returns the names of those drawn and adds the ground each tent, hut, communal hut
        /// and water tower stands on (the disc round it) to <paramref name="holes"/>.</summary>
        HashSet<string> Camps(List<Placement> list, Content content, MapScene map, List<(Vector2 centre, float radius)> holes)
        {
            var camps = new List<(string id, Vector2 at, System.DateTime from)>();
            foreach (var p in list)
            {
                var hex = p.Action == MapActions.TransitCamp ? content.HexAt(p.Q, p.R) : null;
                if (hex == null) continue;
                var w = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                camps.Add(($"{p.Action}_{p.Q}_{p.R}", new Vector2(w.x, w.z), p.Date));
            }
            var drawn = new HashSet<string>();
            if (camps.Count == 0) return drawn;
            var built = SettlementKits.Maabarot(camps, CampGround(map), out var placed);
            foreach (var p in placed) holes.Add((p.C, 0.5f * new Vector2(p.W, p.D).magnitude));
            for (int i = 0; i < camps.Count; i++)
            {
                var mesh = built[i].mesh;
                if (mesh == null) continue;
                var go = new GameObject(camps[i].id);
                go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the kit's vertices are in world space
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = map.KitMaterial;
                r.shadowCastingMode = ShadowCastingMode.On;
                drawn.Add(camps[i].id);
            }
            return drawn;
        }

        /// <summary>The ground a camp is built on: the map's terrain with every kit's footprint (towns, 1948 localities,
        /// Bedouin camps: MapScene.KitFootprints), the railways' and roads' corridors and the units' clearings (as KitLayer.Build
        /// is given them), so a camp keeps off all of them, and the units' tokens with their dates, so a camp keeps off those
        /// present on or after its own date (KitGround.DatedTokens; T67, owner 15 October 2026: "Yes, date-aware").</summary>
        internal static KitGround CampGround(MapScene map)
        {
            var ground = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
            foreach (var f in map.KitFootprints) ground.Footprints.Add(f);
            foreach (var (line, half) in map.RouteCorridors) ground.AddCorridor(line, half);
            foreach (var c in map.UnitClearings) ground.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
            ground.DatedTokens.AddRange(map.UnitTokens);
            return ground;
        }
    }
}
