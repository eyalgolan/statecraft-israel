#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>The "under attack" look for a hex (a city or the unit on it): presentation only. It is a visual state
    /// the simulation can drive later (show it on a hex, hide it); nothing here reads or writes the simulation, and it is
    /// never saved in a board or its overrides. Movement and combat rules are the owner's (docs/gdd/military-combat.md).
    /// STUB for round 2: the signatures are fixed so the painter can be wired; lane E fills in the look.</summary>
    public static class HexUnderAttack
    {
        public static string Name(int q, int r) => $"under attack {q},{r}";

        /// <summary>Shows the look on hex (q, r) of the board under <paramref name="boardRoot"/> (board space, as
        /// HexBoardBuilder builds it), on <paramref name="surface"/>. Showing it again replaces it. Returns its object;
        /// Hide (or destroying the object) removes it and everything it made.</summary>
        public static GameObject Show(Transform boardRoot, IHexSurface surface, int q, int r, float sizeM = HexScale.HexM)
        {
            Hide(boardRoot, q, r);
            var go = new GameObject(Name(q, r));
            go.transform.SetParent(boardRoot, false);
            var c = HexGeometry.Centre(q, r, sizeM);
            go.transform.localPosition = new Vector3(c.x, surface.Height(c), c.z);
            return go;
        }

        /// <summary>Removes the look from hex (q, r); false when it was not shown.</summary>
        public static bool Hide(Transform boardRoot, int q, int r)
        {
            var t = boardRoot.Find(Name(q, r));
            if (t == null) return false;
            t.SetParent(null, false);
            if (Application.isPlaying) Object.Destroy(t.gameObject); else Object.DestroyImmediate(t.gameObject);
            return true;
        }

        public static bool IsShown(Transform boardRoot, int q, int r) => boardRoot.Find(Name(q, r)) != null;
    }
}
