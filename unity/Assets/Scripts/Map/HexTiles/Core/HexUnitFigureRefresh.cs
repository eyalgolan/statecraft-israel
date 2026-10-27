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
        bool dirty, stopped, destroying;
        internal void Dirty() { if (!stopped) dirty = true; }
        internal void Initialise(HexCollectionFigures models, Action refresh, HexBoardAssets local, HexBoardAssets? board)
        {
            this.models = models; this.refresh = refresh; localAssets = local; boardAssets = board;
            // The local owner cancels first, then releases this unit's generated assets. The board invokes that same
            // teardown; Stop itself never re-enters it. Acquired originals retire after this consumer and its generated
            // assets are actually destroyed, not merely when it becomes inactive.
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
            stopped = true; refresh = null; gameObject.SetActive(false);
            // A stopped player unit must not leave an inactive, asset-referencing consumer alive indefinitely.
            if (Application.isPlaying && !destroying) Destroy(gameObject);
            var pending = new System.Collections.Generic.List<UnityEngine.Object> { gameObject };
            pending.AddRange(GetComponentsInChildren<Renderer>(true));
            pending.AddRange(GetComponentsInChildren<MeshFilter>(true));
            if (localAssets != null) pending.AddRange(localAssets.PendingRetirementObjects);
            models?.DisposeAfterDestroyed(pending); models = null;
        }
        void OnDestroy()
        {
            destroying = true; Stop();
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
