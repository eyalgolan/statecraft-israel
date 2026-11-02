#nullable enable
using System;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>One token figure's weapon (<see cref="UnitWeapons"/>): it holds the weapon's bundle while the figure lives and
    /// puts the weapon in the hand of each model the figure shows (the far model when it arrives, the near model when the
    /// camera comes close, UnitFigures.Swap and UnitFigureDetail call <see cref="Refresh"/>). It fails open: until the weapon
    /// arrives, and for good when its bundle is missing or damaged, the figure stands as it did.</summary>
    [ExecuteAlways]
    public sealed class UnitFigureWeapon : MonoBehaviour
    {
        string weapon = "";
        GameObject? prefab;
        IDisposable? lease;
        bool asked;

        internal string Weapon => weapon;

        /// <summary>Gives figure <paramref name="piece"/> (dressed as <paramref name="figure"/> on a <paramref name="side"/> token)
        /// its sourced weapon, if it has one; nothing otherwise.</summary>
        internal static void Equip(Transform piece, string side, string? figure)
        {
            var weapon = UnitWeapons.WeaponFor(side, figure);
            if (weapon == null || piece == null || piece.TryGetComponent<UnitFigureWeapon>(out _)) return;
            var held = piece.gameObject.AddComponent<UnitFigureWeapon>();
            held.weapon = weapon;
            held.Ask();
        }

        void Ask()
        {
            if (asked) return;
            asked = true;
            void Answer(GameObject? arrived)
            {
                if (this == null) return;   // the token was torn down meanwhile
                prefab = arrived;           // null: fail open, the figure stays unarmed (the stream has warned once)
                Arm();
            }
            lease = UnitWeapons.Request(weapon, Answer);
        }

        /// <summary>Arms the figure's models that have arrived (each once).</summary>
        internal static void Refresh(Transform piece)
        {
            if (piece != null && piece.TryGetComponent<UnitFigureWeapon>(out var held)) held.Arm();
        }

        void Arm()
        {
            if (prefab == null) return;
            var holder = transform.Find(UnitFigures.ModelName);
            if (holder == null) return;
            bool armed = false;
            for (int i = 0; i < holder.childCount; i++)
                armed |= UnitWeapons.Arm(holder.GetChild(i), weapon, prefab) != null;
            if (!armed) return;
            UnitFigureDetail.Existing(transform)?.Rescan(holder);
            UnitFigures.GeometryChanged();
        }

        void OnDestroy() { lease?.Dispose(); lease = null; }
    }
}
