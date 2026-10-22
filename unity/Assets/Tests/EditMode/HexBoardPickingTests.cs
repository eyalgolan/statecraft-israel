#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.HexTiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Clicks must use the visible opt-in board's ground, not the real terrain lowered beneath it.</summary>
public class HexBoardPickingTests
{
    MapScene map = null!;
    Camera cam = null!;
    CameraRig rig = null!;
    MapPointer pointer = null!;
    Terrain board = null!;
    RenderTexture target = null!;
    readonly List<(int q, int r)> clicked = new List<(int, int)>();

    [OneTimeSetUp]
    public void BuildTheShippedBoardOnTheRealMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        cam = new GameObject("PickingCamera") { tag = "MainCamera" }.AddComponent<Camera>();
        target = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = target;
        map = new GameObject("PickingMap").AddComponent<MapScene>();
        map.Configure(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSelection.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sea.mat"),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MapFrame.mat"));
        map.Build();
        rig = cam.GetComponent<CameraRig>();
        HexTilesLook.Apply(map, true);
        board = map.transform.Find(HexTilesLook.RootName).GetComponentsInChildren<Terrain>().Single();
        Assert.AreNotSame(map.Terrain, board);
        pointer = map.gameObject.AddComponent<MapPointer>();
        pointer.Map = map;
        pointer.IsOverUi = _ => false;
        pointer.HexClicked += (q, r) => clicked.Add((q, r));
        Physics.SyncTransforms();
    }

    [OneTimeTearDown]
    public void ReleaseTheMap()
    {
        if (cam != null) cam.targetTexture = null;
        if (target != null) Object.DestroyImmediate(target);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShadowFit.Restore(); VisualToggles.Clear(); Statecraft.Game.Map.Kits.KitScale.Reset();
    }

    [TestCase(1, 13, "flat")]
    [TestCase(4, 13, "hills")]
    [TestCase(3, 17, "mountain")]
    public void ObliqueClicksAgreeWithTheDisplayedGround(int q, int r, string terrain)
    {
        var definition = HexBoard.Load(HexTilesLook.Board);
        Assert.AreEqual(terrain, definition.Hexes[(q, r)].terrain);
        Assert.AreEqual(terrain, definition.Hexes[(q + 1, r)].terrain);
        var origin = new Vector3(0f, Shader.GetGlobalVector(MapShaderGlobals.Map).z, map.Frame.LengthM);
        var a = origin + HexGeometry.Centre(q, r, HexScale.HexM);
        var b = origin + HexGeometry.Centre(q + 1, r, HexScale.HexM);
        var edge = (a + b) * 0.5f;
        var failures = new List<string>();
        int checkedClicks = 0;
        foreach (float distance in new[] { CameraController.MinDistance, 20000f, 60000f })
        foreach (float yaw in new[] { 90f, 270f })
        foreach (float inset in new[] { 0f, 0.35f })
        foreach (float offset in new[] { -1000f, -15f, 15f, 1000f })
        {
            var point = edge + Vector3.right * offset;
            point.y = board.transform.position.y + board.SampleHeight(point);
            rig.LookAt = point;
            rig.Distance = distance;
            rig.Yaw = yaw;
            rig.TopInset = inset;
            rig.Apply();
            var screen = cam.WorldToScreenPoint(point);
            Assert.That(screen.z, Is.GreaterThan(cam.nearClipPlane));
            Assert.That(screen.x, Is.InRange(0f, (float)cam.pixelWidth));
            Assert.That(screen.y, Is.InRange(0f, (float)cam.pixelHeight));
            var ray = cam.ScreenPointToRay(screen);
            Assert.IsTrue(board.GetComponent<TerrainCollider>().Raycast(ray, out var visible, 1000000f));
            Assert.IsTrue(map.Terrain.GetComponent<TerrainCollider>().Raycast(ray, out var lowered, 1000000f));
            var expected = MapPointer.Pick(map, visible.point);
            // At the actual displayed-ground hit, independent of any projection round-trip drift.
            Assert.AreEqual(offset < 0 ? (q, r) : (q + 1, r), expected, "visible-ground precondition");
            clicked.Clear();
            Assert.IsTrue(pointer.HandleClick(screen));
            Assert.AreEqual(1, clicked.Count);
            checkedClicks++;
            if (clicked[0] != expected)
            {
                var detail = $"{terrain} distance={distance} yaw={yaw} inset={inset} offset={offset} " +
                    $"visible={visible.point:R} lowered={lowered.point:R} expected={expected} clicked={clicked[0]}";
                Debug.Log("HEX_PICK_MISMATCH " + detail);
                failures.Add(detail);
            }
        }
        Debug.Log($"HEX_PICK_RESULT {terrain} checked={checkedClicks} mismatches={failures.Count}");
        Assert.IsEmpty(failures, string.Join("\n", failures));
    }
    [TestCase(0, 13)] // a sea hex cut out of the board
    [TestCase(-8, 32)] // real map outside the central board
    public void BoardHolesAndOutsideGroundStillPickTheRealMap(int q, int r)
    {
        var point = new Vector3(0, 0, map.Frame.LengthM) + HexGeometry.Centre(q, r, HexScale.HexM);
        point.y = map.Terrain.transform.position.y + map.Terrain.SampleHeight(point);
        rig.LookAt = point; rig.Distance = 20000f; rig.Yaw = 0f; rig.TopInset = 0f; rig.Apply();
        var screen = cam.WorldToScreenPoint(point);
        var ray = cam.ScreenPointToRay(screen);
        Assert.IsFalse(board.GetComponent<TerrainCollider>().Raycast(ray, out _, 1000000f));
        Assert.IsTrue(map.Terrain.GetComponent<TerrainCollider>().Raycast(ray, out var real, 1000000f));
        clicked.Clear();
        Assert.IsTrue(pointer.HandleClick(screen));
        CollectionAssert.AreEqual(new[] { MapPointer.Pick(map, real.point) }, clicked);
    }

    [Test]
    public void NearerPropCollidersDoNotChangeTheClickedHex()
    {
        var point = new Vector3(0, 0, map.Frame.LengthM) + HexGeometry.Centre(1, 13, HexScale.HexM);
        point.y = board.transform.position.y + board.SampleHeight(point);
        rig.LookAt = point; rig.Distance = 20000f; rig.Yaw = 90f; rig.TopInset = 0f; rig.Apply();
        var screen = cam.WorldToScreenPoint(point);
        var ray = cam.ScreenPointToRay(screen);
        Assert.IsTrue(board.GetComponent<TerrainCollider>().Raycast(ray, out var visible, 1000000f));
        var prop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            prop.transform.SetParent(map.transform);
            prop.transform.position = ray.GetPoint(visible.distance - 8000f);
            prop.transform.localScale = Vector3.one * 1000f;
            Physics.SyncTransforms();
            Assert.IsTrue(prop.GetComponent<Collider>().Raycast(ray, out var obstruction, visible.distance));
            Assert.AreNotEqual(MapPointer.Pick(map, visible.point), MapPointer.Pick(map, obstruction.point),
                "the prop would change the picked hex if arbitrary colliders were allowed");
            clicked.Clear(); Assert.IsTrue(pointer.HandleClick(screen));
            CollectionAssert.AreEqual(new[] { MapPointer.Pick(map, visible.point) }, clicked);
        }
        finally { Object.DestroyImmediate(prop); }
    }

    [Test]
    public void RemovingAndReplacingTheBoardDoesNotKeepAStalePickSurface()
    {
        var point = new Vector3(0, 0, map.Frame.LengthM) + HexGeometry.Centre(1, 13, HexScale.HexM);
        try
        {
            HexTilesLook.Apply(map, false);
            Physics.SyncTransforms();
            point.y = map.Terrain.transform.position.y + map.Terrain.SampleHeight(point);
            rig.LookAt = point; rig.Distance = 20000f; rig.Yaw = 90f; rig.TopInset = 0f; rig.Apply();
            var screen = cam.WorldToScreenPoint(point);
            Assert.IsTrue(map.Terrain.GetComponent<TerrainCollider>().Raycast(cam.ScreenPointToRay(screen), out var real, 1000000f));
            clicked.Clear(); Assert.IsTrue(pointer.HandleClick(screen));
            CollectionAssert.AreEqual(new[] { MapPointer.Pick(map, real.point) }, clicked);
            HexTilesLook.Apply(map, true);
            HexTilesLook.Apply(map, true);
            board = map.transform.Find(HexTilesLook.RootName).GetComponentsInChildren<Terrain>().Single();
            Physics.SyncTransforms();
            point.y = board.transform.position.y + board.SampleHeight(point);
            rig.LookAt = point; rig.Apply(); screen = cam.WorldToScreenPoint(point);
            Assert.IsTrue(board.GetComponent<TerrainCollider>().Raycast(cam.ScreenPointToRay(screen), out var visible, 1000000f));
            clicked.Clear(); Assert.IsTrue(pointer.HandleClick(screen));
            CollectionAssert.AreEqual(new[] { MapPointer.Pick(map, visible.point) }, clicked);
        }
        finally
        {
            if (map.transform.Find(HexTilesLook.RootName) == null) HexTilesLook.Apply(map, true);
            board = map.transform.Find(HexTilesLook.RootName).GetComponentsInChildren<Terrain>().Single();
        }
    }

}
