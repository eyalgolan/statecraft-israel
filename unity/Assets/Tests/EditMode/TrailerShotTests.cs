#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;

/// <summary>Issue #45: the scripted trailer capture's shot list (TrailerShot) and its editor entry point (MapCapture.Trailer).</summary>
public class TrailerShotTests
{
    /// <summary>Every shot starts exactly on its From pose and ends exactly on its To pose, at 60 fps, the shots join up
    /// (no jump between them), and the default flight is the 840 frames docs/RELEASING.md states.</summary>
    [Test]
    public void TheShotsStartAndEndOnTheirPosesAndJoinUp()
    {
        var shots = TrailerShot.Default;
        Assert.That(shots.Count, Is.GreaterThan(0));
        foreach (var s in shots)
        {
            Assert.AreEqual(UnityEngine.Mathf.RoundToInt(s.Seconds * 60), s.Frames, s.Name);
            Assert.That(s.Frames, Is.GreaterThan(1), s.Name);
            AssertSamePlace(s.From, s.PoseAt(0), s.Name + " first frame");
            AssertSamePlace(s.To, s.PoseAt(s.Frames - 1), s.Name + " last frame");
        }
        for (int i = 1; i < shots.Count; i++) AssertSamePlace(shots[i - 1].To, shots[i].From, shots[i].Name + " joins the shot before");
        Assert.AreEqual(840, shots.Sum(x => x.Frames), "docs/RELEASING.md and the CHANGELOG say 840 frames");
    }

    /// <summary>The frames only use framings already approved for the art checkpoint (PromoPose.All and CloseChecks), and the
    /// zoom moves one way inside a shot, within the camera's own limits: no overshoot past either end.</summary>
    [Test]
    public void TheShotsUseThePromoPosesAndNeverOvershoot()
    {
        var approved = PromoPose.All.Concat(PromoPose.CloseChecks).Select(p => p.Name).ToList();
        foreach (var s in TrailerShot.Default)
        {
            CollectionAssert.Contains(approved, s.From.Name, s.Name);
            CollectionAssert.Contains(approved, s.To.Name, s.Name);
            float lo = System.Math.Min(s.From.Distance, s.To.Distance), hi = System.Math.Max(s.From.Distance, s.To.Distance);
            float last = s.PoseAt(0).Distance;
            for (int f = 0; f < s.Frames; f++)
            {
                var p = s.PoseAt(f);
                Assert.That(p.Distance, Is.InRange(lo - 1f, hi + 1f), $"{s.Name} frame {f}");
                Assert.That(p.Distance, Is.InRange(CameraController.MinDistance, CameraController.MaxDistance), $"{s.Name} frame {f}");
                Assert.That(s.To.Distance >= s.From.Distance ? p.Distance >= last - 1e-3f : p.Distance <= last + 1e-3f, $"{s.Name} frame {f} zooms one way");
                last = p.Distance;
            }
        }
    }

    /// <summary>The headless entry point exists: -executeMethod MapCapture.Trailer (docs/RELEASING.md).</summary>
    [Test]
    public void TheCaptureHasAnEditorEntryPoint()
    {
        var capture = System.Type.GetType("MapCapture, Assembly-CSharp-Editor");
        Assert.IsNotNull(capture);
        Assert.IsNotNull(capture!.GetMethod("Trailer", System.Type.EmptyTypes), "MapCapture.Trailer()");
    }

    static void AssertSamePlace(PromoPose want, PromoPose got, string what)
    {
        Assert.AreEqual(want.Lon, got.Lon, 1e-9, what + " lon");
        Assert.AreEqual(want.Lat, got.Lat, 1e-9, what + " lat");
        Assert.AreEqual(want.Distance, got.Distance, 1e-2f, what + " distance");
        Assert.AreEqual(want.Yaw, got.Yaw, 1e-4f, what + " yaw");
    }
}
