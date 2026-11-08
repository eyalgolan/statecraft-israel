#nullable enable
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEngine;

/// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 4): the camera's eased glide to a
/// beat. It moves the look-at point only (owner, Q5: "Keep the player's zoom"; the GDD camera never orbits), the map's
/// edge holds it as it holds the player's pan (CameraController.Step), and a player's pan or zoom takes the camera back.</summary>
public class CameraGlideTests
{
    GameObject go = null!;
    CameraRig rig = null!;
    static readonly Rect Bounds = new Rect(0, 0, 1000000, 1000000);
    static readonly Vector3 Centre = new Vector3(500000, 0, 500000);

    [SetUp]
    public void Open()
    {
        go = new GameObject("GlideCamera", typeof(Camera), typeof(CameraRig));
        rig = go.GetComponent<CameraRig>();
        rig.LookAt = Centre; rig.Distance = CameraRig.StartDistance; rig.Yaw = 0f;
        CameraController.Step(rig, default, 0f, Bounds);
    }

    [TearDown]
    public void Close() { if (go != null) Object.DestroyImmediate(go); }

    [Test]
    public void TheGlideStartsAndEndsWhereAsked()
    {
        var from = new Vector3(100, 7, 200); var to = new Vector3(5100, 0, -800);
        Assert.AreEqual(from, CameraGlide.At(from, to, 0f));
        Assert.AreEqual(new Vector3(to.x, from.y, to.z), CameraGlide.At(from, to, 1f), "the height is Step's to set");
        Assert.AreEqual(from, CameraGlide.At(from, to, -1f), "t is clamped");
        Assert.AreEqual(new Vector3(to.x, from.y, to.z), CameraGlide.At(from, to, 2f));
    }

    [Test]
    public void ThePointMidwayIsHalfway()
    {
        var from = Vector3.zero; var to = new Vector3(1000, 0, -400);
        var mid = CameraGlide.At(from, to, 0.5f);
        Assert.AreEqual(500f, mid.x, 1e-3f); Assert.AreEqual(-200f, mid.z, 1e-3f);
        var early = CameraGlide.At(from, to, 0.1f);
        Assert.Less(early.x, 100f, "it eases in");
        Assert.Greater(CameraGlide.At(from, to, 0.9f).x, 900f, "and out");
    }

    [Test]
    public void TheGlideTakesLongerForAFartherPoint()
    {
        const float hex = 4400f;
        Assert.AreEqual(UnitBeats.Timing.PanMin, CameraGlide.Seconds(0f, hex), 1e-4f);
        Assert.AreEqual(UnitBeats.Timing.PanMax, CameraGlide.Seconds(1000f * hex, hex), 1e-4f);
        Assert.AreEqual(UnitBeats.Timing.PanPerHex * 10f, CameraGlide.Seconds(10f * hex, hex), 1e-4f);
        Assert.AreEqual(UnitBeats.PanSeconds(10), CameraGlide.Seconds(10f * hex, hex), 1e-4f, "one clamp for the planner and the glide");
    }

    [Test]
    public void APointInTheMiddleOfTheViewNeedsNoPan()
    {
        Assert.IsTrue(CameraGlide.InView(rig, rig.LookAt));
        Assert.IsTrue(CameraGlide.InView(rig, rig.LookAt + new Vector3(2000, 0, 0)));
    }

    [Test]
    public void APointOffScreenNeedsOne()
    {
        Assert.IsFalse(CameraGlide.InView(rig, rig.LookAt + new Vector3(200000, 0, 0)));
        Assert.IsFalse(CameraGlide.InView(rig, rig.LookAt + new Vector3(0, 0, -200000)));
        Assert.IsFalse(CameraGlide.InView(rig, rig.LookAt + new Vector3(0, 0, 200000)));
        // The camera looks north, so the left edge of the view lies west. A point just inside the edge is inside the view
        // but not inside its margin: the glide brings it in, so a unit never moves at the screen's very edge.
        var cam = go.GetComponent<Camera>();
        var nearEdge = PointAt(cam, 0.05f, 0.5f);
        Assert.Less(cam.WorldToViewportPoint(nearEdge).x, 0.15f, "fixture: near the left edge");
        Assert.IsFalse(CameraGlide.InView(rig, nearEdge));
        Assert.IsTrue(CameraGlide.InView(rig, nearEdge, margin: 0f));
    }

    [Test]
    public void TheBarAtTheTopIsNotTheView()
    {
        rig.TopInset = 0.3f;
        CameraController.Step(rig, default, 0f, Bounds);
        var cam = go.GetComponent<Camera>();
        // Viewport y above 1 - TopInset lies under the UI's band.
        var under = PointAt(cam, 0.5f, 0.85f);
        Assert.IsFalse(CameraGlide.InView(rig, under, margin: 0f), "a point under the bar is not seen");
        Assert.IsTrue(CameraGlide.InView(rig, PointAt(cam, 0.5f, 0.4f), margin: 0f));
    }

    /// <summary>The ground point (y = 0) the camera shows at viewport (x, y).</summary>
    static Vector3 PointAt(Camera cam, float x, float y)
    {
        var ray = cam.ViewportPointToRay(new Vector3(x, y, 0f));
        return ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
    }

    [Test]
    public void APlayerMoveHandsTheCameraBack()
    {
        var set = rig.LookAt; float distance = rig.Distance;
        CameraController.Step(rig, default, 0.1f, Bounds);
        Assert.IsFalse(CameraGlide.TookOver(rig, set, distance), "a frame with no input is not the player's");
        CameraController.Step(rig, new CameraInput { Pan = new Vector2(1f, 0f) }, 0.1f, Bounds);
        Assert.IsTrue(CameraGlide.TookOver(rig, set, distance), "a pan");

        rig.LookAt = set; CameraController.Step(rig, default, 0f, Bounds);
        CameraController.Step(rig, new CameraInput { Zoom = 1f }, 0.1f, Bounds);
        Assert.IsTrue(CameraGlide.TookOver(rig, set, distance), "a zoom");
    }

    [Test]
    public void AGlideTargetPastTheMapEdgeStopsWhereSettleHoldsIt()
    {
        var from = rig.LookAt; var past = new Vector3(-50000, 0, Centre.z);
        rig.LookAt = CameraGlide.At(from, past, 1f);
        CameraController.Step(rig, default, 0f, Bounds);   // as CameraController.Settle
        var view = CameraController.ViewFootprint(rig);
        Assert.Greater(rig.LookAt.x, past.x, "it stops short of a point past the edge");
        Assert.AreEqual(Bounds.xMin, rig.LookAt.x + view.xMin, 1f, "with the view's edge on the map's");
        Assert.IsFalse(CameraGlide.TookOver(rig, rig.LookAt, rig.Distance),
            "measured from the held point, the edge's hold is not taken for the player's move");
    }

    [Test]
    public void TheGlideNeverTurnsOrZooms()
    {
        rig.Yaw = 0f; float distance = rig.Distance;
        var from = rig.LookAt; var to = from + new Vector3(30000, 0, -20000);
        for (int i = 0; i <= 10; i++)
        {
            rig.LookAt = CameraGlide.At(from, to, i / 10f);
            CameraController.Step(rig, default, 0.05f, Bounds, fixedNorth: true);
            Assert.AreEqual(0f, rig.Yaw); Assert.AreEqual(distance, rig.Distance);
        }
        Assert.AreEqual(to.x, rig.LookAt.x, 1e-2f); Assert.AreEqual(to.z, rig.LookAt.z, 1e-2f);
    }
}
