#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>The whole-board capture's framing (HexBoardCapture.BoardDistance): a board framed whole shows all of its
/// rect, the near (southern) edge included, at landscape and portrait sizes; a small board keeps the standard distance.</summary>
public class HexBoardCaptureTests
{
    /// <summary>The shot's aim, as HexBoardCapture.Capture takes it: the mean hex centre, on the ground.</summary>
    static Vector3 Middle(HexBoard board)
    {
        var surface = new HexBoardSurface(board);
        var cs = board.Hexes.Values.Select(h => HexGeometry.Centre(h.q, h.r, HexScale.HexM)).ToList();
        var m = new Vector3(cs.Average(c => c.x), 0f, cs.Average(c => c.z));
        return new Vector3(m.x, Mathf.Max(surface.Height(m), 0f), m.z);
    }

    [TestCase("israel", 3840, 2160)]
    [TestCase("israel", 2160, 3840)]
    public void EveryCornerOfAWholeBoardIsInView(string id, int width, int height)
    {
        var board = HexBoard.Load(id);
        var lookAt = Middle(board);
        float distance = HexBoardCapture.BoardDistance(board, lookAt, width / (float)height);
        Assert.That(distance, Is.GreaterThan(2f * HexBoardCapture.BoardDistanceM), "the board is framed whole");
        // A real camera, placed as Shoot places it, checks the projection independently of BoardDistance's own maths.
        var cam = new GameObject("cam").AddComponent<Camera>();
        try
        {
            cam.fieldOfView = HexBoardCapture.Fov; cam.aspect = width / (float)height; cam.nearClipPlane = 50f; cam.farClipPlane = 1_000_000f;
            cam.transform.rotation = Quaternion.Euler(HexBoardCapture.Pitch, 0f, 0f);
            cam.transform.position = lookAt - cam.transform.forward * distance;
            var b = HexBoardBuilder.Bounds(board);
            foreach (var c in new[] { new Vector3(b.xMin, 0f, b.yMin), new Vector3(b.xMax, 0f, b.yMin), new Vector3(b.xMin, 0f, b.yMax), new Vector3(b.xMax, 0f, b.yMax) })
            {
                var v = cam.WorldToViewportPoint(c);
                Assert.That(v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f && v.z > cam.nearClipPlane && v.z < cam.farClipPlane, $"{id} {width}x{height}: corner {c} at {v}");
            }
        }
        finally { Object.DestroyImmediate(cam.gameObject); }
    }

    [Test]
    public void ASmallBoardKeepsTheStandardDistance()
    {
        var board = HexBoard.Load("tel_aviv");
        Assert.That(HexBoardCapture.BoardDistance(board, Middle(board), 16f / 9f), Is.EqualTo(HexBoardCapture.BoardDistanceM));
    }
}
