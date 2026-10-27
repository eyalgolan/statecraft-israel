#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.HexTiles.Painter
{
    /// <summary>The Hex Painter's transient looks: the time of day (HexTimeOfDay) and the under-attack preview
    /// (HexUnderAttack). Neither is written to a board or its overrides. It keeps the time preset's undo and runs it before
    /// the next preset and on Clear; it remembers which hexes show the attack look, re-shows one after its hex is rebuilt,
    /// and hides them all on Clear. The window calls Clear on unload, board reload, close and script reload, so nothing is
    /// left changed. The looks are passed in (the real ones by default) so tests can watch the calls.</summary>
    public sealed class HexPainterSwitches
    {
        readonly Func<Transform, IHexSurface, int, int, GameObject> show;
        readonly Func<Transform, int, int, bool> hide;
        readonly HashSet<(int q, int r)> attacked = new HashSet<(int q, int r)>();
        Action? undoTime;
        Func<HexTime, Action>? applyTime;
        Transform? board;
        IHexSurface? surface;

        public HexPainterSwitches(Func<Transform, IHexSurface, int, int, GameObject>? show = null, Func<Transform, int, int, bool>? hide = null)
        {
            this.show = show ?? ((b, s, q, r) => HexUnderAttack.Show(b, s, q, r));
            this.hide = hide ?? HexUnderAttack.Hide;
        }

        /// <summary>The preset applied now (Day when none is).</summary>
        public HexTime Time { get; private set; } = HexTime.Day;

        /// <summary>The hexes showing the under-attack look, in (q, r) order.</summary>
        public IReadOnlyList<(int q, int r)> Attacked => attacked.OrderBy(h => h.q).ThenBy(h => h.r).ToList();

        /// <summary>Undoes the preset applied now, then applies <paramref name="time"/> through <paramref name="apply"/>
        /// (HexTimeOfDay.Apply on the scene's light, grade and board) and keeps its undo.</summary>
        public void SetTime(HexTime time, Func<HexTime, Action> apply)
        {
            UndoTime();
            undoTime = apply(time);
            applyTime = apply;
            Time = time;
        }

        /// <summary>Undoes the preset applied now and applies it again: night's lit windows (HexNightWindows) are made
        /// from the board's pieces, so they go stale when a hex is rebuilt.</summary>
        public void ReapplyTime()
        {
            var (time, apply) = (Time, applyTime);
            if (apply == null) return;
            SetTime(time, apply);
        }

        /// <summary>Restores what the preset changed; the look is the day look again.</summary>
        public void UndoTime()
        {
            var undo = undoTime;
            undoTime = null; applyTime = null; Time = HexTime.Day;
            undo?.Invoke();
        }

        public bool IsAttacked(int q, int r) => attacked.Contains((q, r));

        /// <summary>Shows or hides the under-attack look on hex (q, r) of the board under <paramref name="boardRoot"/>.
        /// Hexes of another board are hidden first (a switch belongs to one board).</summary>
        public void SetAttack(Transform boardRoot, IHexSurface boardSurface, int q, int r, bool on)
        {
            if (board != boardRoot) ClearAttacks();
            board = boardRoot; surface = boardSurface;
            if (on) { show(boardRoot, boardSurface, q, r); attacked.Add((q, r)); }
            else if (attacked.Remove((q, r))) hide(boardRoot, q, r);
        }

        /// <summary>After hex (q, r) was rebuilt: applies the time preset again (so its look matches the rebuilt hex), and
        /// shows the hex's attack look again if it was on and the rebuild removed it.</summary>
        public void Rebuilt(int q, int r)
        {
            ReapplyTime();
            if (board == null || surface == null || !attacked.Contains((q, r))) return;
            if (!HexUnderAttack.IsShown(board, q, r)) show(board, surface, q, r);
        }

        /// <summary>Hides every attack look this switch showed (on the board while it still exists).</summary>
        public void ClearAttacks()
        {
            if (board != null) foreach (var (q, r) in Attacked) hide(board, q, r);
            attacked.Clear(); board = null; surface = null;
        }

        /// <summary>Undoes the time preset and hides every attack look. Call it before the board is torn down: a night
        /// preset may have changed the board's own objects.</summary>
        public void Clear()
        {
            ClearAttacks();
            UndoTime();
        }
    }
}
