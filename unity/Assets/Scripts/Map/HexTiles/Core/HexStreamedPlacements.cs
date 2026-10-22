#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>Only the aliased figures of one emitted layer. Coalesces arrivals into one refresh per frame; its own
    /// generated geometry can be replaced without rebuilding terrain, unrelated pieces or picking geometry.</summary>
    public sealed class HexStreamedPlacements : MonoBehaviour
    {
        HexCollectionFigures? models;
        IReadOnlyList<HexPlacement> placements = Array.Empty<HexPlacement>();
        HexBoardAssets? boardAssets;
        Transform? drawn;
        bool dirty, stopped;

        internal void Initialise(IReadOnlyList<HexPlacement> placements, HexBoardAssets assets)
        {
            this.placements = placements; boardAssets = assets;
            models = new HexCollectionFigures(() => dirty = true);
            assets.OnTeardown(Stop);
            Draw(); dirty = false;
        }

        void LateUpdate() => RefreshNow();
        internal void RefreshNow()
        {
            if (stopped || !dirty) return;
            dirty = false; Draw();
        }

        void Draw()
        {
            ReleaseDrawn();
            var holder = new GameObject("Hex figure geometry"); holder.transform.SetParent(transform, false);
            var assets = holder.AddComponent<HexBoardAssets>();
            drawn = holder.transform;
            HexBoardRenderer.Emit(drawn, placements, assets, "figures", models!.Prefab);
        }

        void ReleaseDrawn()
        {
            if (drawn == null) return;
            drawn.gameObject.SetActive(false);
            drawn.GetComponent<HexBoardAssets>().Teardown();
            if (Application.isPlaying) Destroy(drawn.gameObject); else DestroyImmediate(drawn.gameObject);
            drawn = null;
        }

        void Stop()
        {
            if (stopped) return;
            stopped = true; models?.Dispose(); models = null;
            placements = Array.Empty<HexPlacement>(); ReleaseDrawn();
        }

        void OnDestroy()
        {
            Stop();
            if (boardAssets != null) boardAssets.ForgetTeardown(Stop);
            boardAssets = null;
        }
    }
}
