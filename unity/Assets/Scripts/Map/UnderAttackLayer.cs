#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.HexTiles;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>#1878: the "under attack" look (HexUnderAttack: smoke, fire glow, pulsing border) on the default diorama map.
    /// HexUnderAttack builds on a hex board; this lays the same lattice over the terrain (a root at the map's north-west
    /// corner, as HexRealMap does) so the look sits on the attacked hex and follows the ground. Presentation only: it reads
    /// the turn's Why lines (an engagement's Target is the hex it hit) and writes nothing to the simulation or a save.</summary>
    public sealed class UnderAttackLayer
    {
        public const string RootName = "UnderAttack";

        readonly MapScene map;
        readonly Transform root;
        readonly float sizeM;
        readonly TerrainSurface surface;
        readonly List<(int q, int r)> shown = new List<(int, int)>();

        public IReadOnlyCollection<(int q, int r)> Shown => shown;

        /// <summary>The hexes the turn's engagements hit, each once, in the turn's order.</summary>
        public static IReadOnlyList<(int q, int r)> AttackedHexes(StepResult? turn)
        {
            var hexes = new List<(int q, int r)>();
            if (turn == null) return hexes;
            foreach (var cause in turn.Why)
            {
                if (cause.Key != WhyKeys.Engagement) continue;
                var parts = cause.Target.Split(',');
                if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var q)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)) continue;
                if (!hexes.Contains((q, r))) hexes.Add((q, r));
            }
            return hexes;
        }

        public UnderAttackLayer(MapScene map)
        {
            this.map = map;
            sizeM = map.Hexes.hex_size_km * 1000f;
            var old = map.Terrain.transform.Find(RootName);
            if (old != null) { old.SetParent(null, false); if (Application.isPlaying) Object.Destroy(old.gameObject); else Object.DestroyImmediate(old.gameObject); }
            var go = new GameObject(RootName);
            go.transform.SetParent(map.Terrain.transform, false);
            // Lattice (x east, z = -south of the north-west corner) becomes world by this shift, as HexRealMap's Origin.
            go.transform.position = new Vector3(0f, 0f, map.Frame.LengthM);
            root = go.transform;
            surface = new TerrainSurface(map, root.position);
        }

        /// <summary>Shows the look on every hex of the map the turn attacked, in place of an earlier turn's; none when the
        /// turn is null or attacked nothing. A hex the map does not have shows nothing.</summary>
        public void Show(StepResult? turn)
        {
            Clear();
            if (root == null) return;   // the terrain, and the root under it, went with the scene
            var onMap = new HashSet<(int, int)>(map.Hexes.hexes.Select(h => (h.q, h.r)));
            foreach (var (q, r) in AttackedHexes(turn))
            {
                if (!onMap.Contains((q, r))) continue;
                HexUnderAttack.Show(root, surface, q, r, sizeM);
                shown.Add((q, r));
            }
        }

        public void Clear()
        {
            if (root == null) { shown.Clear(); return; }   // GameRoot's teardown may run after the terrain is gone
            foreach (var (q, r) in shown) HexUnderAttack.Hide(root, q, r);
            shown.Clear();
        }

        /// <summary>The terrain's ground in the root's space (lattice x/z). Heights are world y, which is the root's local y
        /// because the root sits at y = 0 (HexRealMap's board is sunk and sea-based; this layer is not).</summary>
        sealed class TerrainSurface : IHexSurface
        {
            readonly MapScene map; readonly Vector3 origin;
            public TerrainSurface(MapScene map, Vector3 origin) { this.map = map; this.origin = origin; }
            public float Height(Vector3 p) => UnitShowcase.Ground(map, new Vector3(p.x + origin.x, 0f, p.z + origin.z)).y;
            // The look reads only Height; slope and water are for tile placement, which this layer does not do.
            public float SlopeDeg(Vector3 p) => 0f;
            public bool IsWater(Vector3 p) => false;
        }
    }
}
