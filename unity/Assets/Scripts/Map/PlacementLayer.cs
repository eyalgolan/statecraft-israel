#nullable enable
using System.Collections.Generic;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Draws the player's transit camps and housing projects as marker boxes on their hex centres (placeholder art
    /// like the M2 settlement markers; per-era prefabs are M4). Uses the editor-made marker material with a per-kind
    /// colour through a MaterialPropertyBlock (no runtime Shader.Find). Render replaces what it drew before.</summary>
    public sealed class PlacementLayer : MonoBehaviour
    {
        static readonly Dictionary<string, (Color colour, float size)> Look = new Dictionary<string, (Color, float)>
        {
            [MapActions.TransitCamp] = (new Color(0.62f, 0.58f, 0.50f), 700f),
            [MapActions.Housing] = (new Color(0.95f, 0.94f, 0.90f), 900f),
        };
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public void Render(IEnumerable<Placement> placements, Content content, MapScene map)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var block = new MaterialPropertyBlock();
            foreach (var p in placements)
            {
                var hex = content.HexAt(p.Q, p.R);
                if (hex == null || !Look.TryGetValue(p.Action, out var look)) continue;
                var at = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
                if (BuildingPreview.DressPlacement(transform, map, p, at, look.size)) continue;   // the local Tripo model, when present
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
    }
}
