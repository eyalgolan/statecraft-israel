#nullable enable
using System;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>The lifetime and deferred refresh of one hex unit's figures, including units whose motion is not ticked
    /// again after placement. Board teardown cancels before Unity's deferred destruction.</summary>
    public sealed class HexUnitFigureRefresh : MonoBehaviour
    {
        HexCollectionFigures? models;
        HexBoardAssets? boardAssets, localAssets;
        Action? refresh, teardown;
        bool dirty, stopped;
        internal void Dirty() { if (!stopped) dirty = true; }
        internal void Initialise(HexCollectionFigures models, Action refresh, HexBoardAssets local, HexBoardAssets? board)
        {
            this.models = models; this.refresh = refresh; localAssets = local; boardAssets = board;
            // The local owner cancels first, then releases this unit's generated assets. The board invokes that same
            // teardown; Stop itself never re-enters it. Collection prefabs/materials are borrowed, not owned here.
            local.OnTeardown(Stop);
            teardown = local.Teardown;
            if (board != null) board.OnTeardown(teardown);
        }
        void LateUpdate() => RefreshNow();
        internal void RefreshNow()
        {
            if (stopped || !dirty) return;
            dirty = false; refresh?.Invoke();
        }
        void Stop()
        {
            if (stopped) return;
            stopped = true; models?.Dispose(); models = null; refresh = null;
            gameObject.SetActive(false);
        }
        void OnDestroy()
        {
            Stop();
            if (boardAssets != null && teardown != null) boardAssets.ForgetTeardown(teardown);
            if (localAssets != null)
            {
                localAssets.ForgetTeardown(Stop);
                localAssets.Teardown();
            }
            boardAssets = null; localAssets = null; teardown = null;
        }
    }
}
