#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

/// <summary>The hex board's ground paint (the israel board's 2048 x 2048 x 11 alphamap) must not allocate the whole
/// alphamap or garbage per pixel: in WebGL the GC runs only between frames and the heap never shrinks, so one build
/// frame's garbage stays reserved for the rest of the game.</summary>
public class HexGroundPaintTests
{
    const string IsraelBoard = "israel";
    GameObject? root;
    HexBoardState state = null!;
    List<(HexPaint paint, int q, int r)> paints = null!;

    [OneTimeSetUp]
    public void BuildTheIsraelBoardPerHex()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = HexBoardBuilder.Build(HexBoard.Load(IsraelBoard), null, options: new HexBoardBuilder.Options { PerHex = true }).root;
        state = root.GetComponent<HexBoardState>();
        // Build's order: hexes by q then r, each tile's paints in its own order (as Repaint rebuilds them).
        paints = state.Paints.OrderBy(kv => kv.Key.q).ThenBy(kv => kv.Key.r).SelectMany(kv => kv.Value.Select(p => (p, kv.Key.q, kv.Key.r))).ToList();
        Assert.That(paints, Is.Not.Empty, "fixture: the israel board has tile paints");
        Assert.That(state.Board.RoadLines(), Is.Not.Empty, "fixture: the israel board has roads");
    }

    [OneTimeTearDown]
    public void Destroy() { if (root != null) Object.DestroyImmediate(root); }

    [Test]
    public void TheGroundIsBitIdenticalToTheOnePieceSplat()
    {
        var data = state.Terrain;
        int n = data.alphamapResolution, layers = data.alphamapLayers, differing = 0;
        Assert.That(n, Is.EqualTo(2048));
        Assert.That(layers, Is.EqualTo(HexGround.Ids.Count));
        var whole = HexBoardBuilder.Splat(state.Board, state.Surface, state.Rect, paints, 0, 0, n, n);
        float maxSeen = 0f;
        for (int z0 = 0; z0 < n; z0 += 128)
        {
            var band = data.GetAlphamaps(0, z0, n, 128);
            for (int z = 0; z < 128; z++)
                for (int x = 0; x < n; x++)
                    for (int l = 0; l < layers; l++)
                    {
                        if (band[z, x, l] != whole[z0 + z, x, l]) differing++;
                        maxSeen = Mathf.Max(maxSeen, band[z, x, l]);
                    }
        }
        Assert.That(maxSeen, Is.GreaterThan(0.5f), "the alphamap was painted");
        Assert.That(differing, Is.EqualTo(0), "alphamap values that differ from the one-piece splat");
    }

    [Test]
    public void PaintingTheGroundAllocatesNoWholeAlphamapAndNoGarbagePerPixel()
    {
        var data = new TerrainData { alphamapResolution = 2048 };
        try
        {
            data.terrainLayers = HexGround.Ids.Select(_ => new TerrainLayer()).ToArray();
            void PaintOnce() => HexBoardBuilder.PaintGround(data, state.Board, state.Surface, state.Rect, paints);
            PaintOnce();   // warm up: the first call JIT-compiles and allocates

            System.GC.Collect();
            long before = Profiler.GetMonoUsedSizeLong();
            PaintOnce();
            long grewMB = (Profiler.GetMonoUsedSizeLong() - before) >> 20;

            Assert.That(() => new int[8], Is.AllocatingGCMemory(), "control: the recorder sees an allocation");
            // Read as Unity's AllocatingGCMemory constraint reads it: this thread only, collected after the call.
            var recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            PaintOnce();
            recorder.enabled = false;
            recorder.CollectFromAllThreads();
            int allocations = recorder.sampleBlockCount;

            Assert.That(grewMB, Is.LessThan(32), $"Mono heap grew {grewMB} MB (the whole alphamap alone is 184 MB)");
            Assert.That(allocations, Is.LessThan(1000), $"{allocations} managed allocations in one ground paint");
        }
        finally
        {
            foreach (var l in data.terrainLayers) Object.DestroyImmediate(l);
            Object.DestroyImmediate(data);
        }
    }
}
