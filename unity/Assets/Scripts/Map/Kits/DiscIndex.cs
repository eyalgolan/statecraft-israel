#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Discs (centre, radius, owner id) in a uniform grid, so the kits' "is this spot free / inside a town" questions
    /// look at the few discs near a point instead of all of them (some 300 places, 2,000 lots and 10,000 trees: the linear
    /// scans took the kit build and the planting from about 20 ms to 350 ms each). A disc is filed in every cell within its
    /// radius times <see cref="reachScale"/> plus <see cref="reachPad"/>, which bounds how far from it a query may still
    /// need to find it; <see cref="Near"/> then returns only the point's own cell. Insertion order is kept (enumeration is
    /// the list), so results do not depend on the grid.</summary>
    public sealed class DiscIndex : IEnumerable<(Vector2 centre, float radius, string id)>
    {
        public const float CellM = 4000f;
        readonly float reachScale, reachPad;
        readonly List<(Vector2 centre, float radius, string id)> all = new List<(Vector2, float, string)>();
        readonly Dictionary<(int, int), List<int>> cells = new Dictionary<(int, int), List<int>>();
        static readonly int[] None = System.Array.Empty<int>();

        /// <param name="reachScale">A query finds a disc up to its radius times this from its centre ...</param>
        /// <param name="reachPad">... plus this many metres.</param>
        public DiscIndex(float reachScale, float reachPad) { this.reachScale = reachScale; this.reachPad = reachPad; }

        public int Count => all.Count;
        public (Vector2 centre, float radius, string id) this[int i] => all[i];

        /// <summary>The farthest from a disc's edge a query may look (beyond this <see cref="Near"/> may miss the disc).</summary>
        public float Reach(float radius) => radius * reachScale + reachPad;

        public void Add((Vector2 centre, float radius, string id) d)
        {
            int i = all.Count; all.Add(d);
            float reach = Reach(d.radius);
            int x0 = Cell(d.centre.x - reach), x1 = Cell(d.centre.x + reach), z0 = Cell(d.centre.y - reach), z1 = Cell(d.centre.y + reach);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!cells.TryGetValue((x, z), out var list)) cells[(x, z)] = list = new List<int>();
                    list.Add(i);
                }
        }

        public void Add((Vector2 centre, float radius) d) => Add((d.centre, d.radius, ""));

        /// <summary>Indices of the discs filed in <paramref name="p"/>'s cell, in insertion order.</summary>
        public IReadOnlyList<int> Near(Vector2 p) => cells.TryGetValue((Cell(p.x), Cell(p.y)), out var list) ? list : (IReadOnlyList<int>)None;

        static int Cell(float v) => Mathf.FloorToInt(v / CellM);

        public IEnumerator<(Vector2 centre, float radius, string id)> GetEnumerator() => all.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
