#nullable enable
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Owner's report on v0.2.0 (22 October 2026): "unit movement is not working, nothing happens". A unit's badge
/// floats over its token "so a player sees it is a unit and can click it" (UnitBadge), but a click there picks the hex of the
/// terrain behind it (MapPointer.HexAt raycasts the terrain only), so the card shows that hex, with no Move.</summary>
public class UnitBadgeClickTests
{
    MapScene map = null!;
    Camera cam = null!;
    CameraRig rig = null!;
    RenderTexture rt = null!;
    GameObject pointerGo = null!;

    [SetUp]
    public void Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        cam = Camera.main; rig = cam.GetComponent<CameraRig>();
        rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt;   // the browser's 1920x1080 at the camera's pixels
        pointerGo = new GameObject("BadgeClickPointer");
    }

    [TearDown]
    public void Close()
    {
        cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(pointerGo);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    /// <summary>With the camera over each Israeli brigade in turn (the rig's default 90 km, yaw 0, its fixed 50 degree pitch), a
    /// click on the brigade's badge picks the brigade's hex, as a click on its figures' base does: the hex
    /// GameRoot.OnHexClicked turns into the unit's card and its Move.</summary>
    [Test]
    public void AClickOnAUnitsBadgePicksTheUnitsHex()
    {
        var content = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var units = UnitData.Parse(File.ReadAllText(Path.Combine(Statecraft.Game.Paths.Content, "map", UnitData.DefaultFileName)));
        var layer = UnitLayer.Create(map, units, content.Hexes, still: true);
        layer.Show(content, Statecraft.Sim.Simulation.NewGame(content, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical));
        var pointer = pointerGo.AddComponent<MapPointer>();
        pointer.Map = map;

        var tokens = GameObject.Find(UnitLayer.RootName).transform.Find("Tokens");
        int clicked = 0;
        var misses = new System.Collections.Generic.List<string>();
        foreach (var t in layer.Tokens.Where(t => t.Token.Unit.Side == UnitData.Israel && t.Token.Unit.Service == UnitData.Army))
        {
            var badge = tokens.Find(t.Token.Unit.Id).Find("Badge");
            rig.LookAt = new Vector3(badge.position.x, 0f, badge.position.z); rig.Distance = 90000f; rig.Yaw = 0f; rig.TopInset = 0; rig.Apply();
            layer.FaceCamera(cam);   // the badges where this camera draws them
            var s = cam.WorldToScreenPoint(badge.position);   // pixels, y up: what MapPointer gets from the mouse
            if (s.z <= 0 || s.x < 0 || s.y < 0 || s.x > cam.pixelWidth || s.y > cam.pixelHeight) continue;   // off screen
            clicked++;
            var hex = pointer.HexAt(new Vector2(s.x, s.y));
            if (hex != (t.Q, t.R)) misses.Add($"{t.Token.Unit.Id} on {t.Q},{t.R}: its badge at ({s.x:0}, {s.y:0}) picks {hex}");
        }
        TestContext.WriteLine($"{clicked} badges on screen; misses:\n" + string.Join("\n", misses));
        Assert.Greater(clicked, 2, "the brigades are drawn");
        Assert.IsEmpty(misses, "a click on a unit's badge must pick the unit's hex");
    }
}
