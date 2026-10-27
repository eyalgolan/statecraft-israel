#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly List<UnityEngine.Object> retiringObjects = new List<UnityEngine.Object>();

        internal void Initialise(IReadOnlyList<HexPlacement> placements, HexBoardAssets assets)
        {
            this.placements = placements; boardAssets = assets;
            models = new HexCollectionFigures(() => dirty = true);
            models.SetInstanceCounter(() => placements.Count(p => models != null && models.IsOriginalCached(HexModels.Resolve(p.Model, p.Position))));
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
            retiringObjects.RemoveAll(instance => instance == null);
            retiringObjects.Add(drawn.gameObject);
            retiringObjects.AddRange(drawn.GetComponentsInChildren<Renderer>(true));
            retiringObjects.AddRange(drawn.GetComponentsInChildren<MeshFilter>(true));
            var assets = drawn.GetComponent<HexBoardAssets>();
            retiringObjects.AddRange(assets.PendingRetirementObjects);
            drawn.gameObject.SetActive(false);
            assets.Teardown();
            if (Application.isPlaying) Destroy(drawn.gameObject); else DestroyImmediate(drawn.gameObject);
            drawn = null;
        }

        void Stop()
        {
            if (stopped) return;
            stopped = true;
            placements = Array.Empty<HexPlacement>(); ReleaseDrawn();
            models?.DisposeAfterDestroyed(retiringObjects); models = null; retiringObjects.Clear();
        }

        void OnDestroy()
        {
            Stop();
            if (boardAssets != null) boardAssets.ForgetTeardown(Stop);
            boardAssets = null;
        }
    }
}
