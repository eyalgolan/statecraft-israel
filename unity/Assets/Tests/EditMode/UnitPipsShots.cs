#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Writes the stills #2432 asks for (#2960 PR 2): a full, a damaged and a badly damaged unit's badge pips, on both
/// sides, at the gameplay zoom (31 km, yaw 10 as PromoPose.Gameplay03) and from 60 km (nearer, the badge leaves the top of
/// the frame at this look-at point), to `.superpowers/shots-2960/&lt;SHOTS_TAG&gt;/`
/// at the repository root (local, not committed; "untagged" when SHOTS_TAG is unset). Each still is the map camera at
/// 1920x1080 without the UI, plus a 4x crop round the unit's badge. The figures come from the AssetDatabase. Explicit, so it
/// does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class UnitPipsShots
{
    const int Width = 1920, Height = 1080, Crop = 120, Zoom = 4;
    static readonly Content C = ContentLoader.Load(Paths.Content);

    [Test]
    public void FullDamagedAndBadlyDamaged()
    {
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "shots-2960", string.IsNullOrEmpty(tag) ? "untagged" : tag));
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        UnitFigures.Forget();
        UnitFigures.Source = (id, detail, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail)));
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        ShotCamera.Configure(cam, Width, Height);
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        try
        {
            cam.targetTexture = rt; rig.TopInset = 0f;
            var units = UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
            var cases = new List<(string name, string row, int strength, int full)>
            {
                ("israel-full", "golani", 0, 0), ("israel-damaged", "golani", 3, 4), ("israel-badly", "golani", 1, 4),
                ("egypt-full", "egyptian_expeditionary_force", 0, 0), ("egypt-damaged", "egyptian_expeditionary_force", 7, 10),
                ("egypt-badly", "egyptian_expeditionary_force", 2, 10),
            };
            foreach (var (name, row, strength, full) in cases)
            {
                int id = C.StartingForces.FindIndex(f => f.Row == row);
                var u = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Units[id];
                // An enemy's unit keeps no Full (UnitDamage): its starting strength is its full strength.
                int setFull = u.Side == UnitData.Israel ? full : 0;
                s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, strength > 0 ? strength : u.Strength, u.Order, u.OrderTarget, u.HomeCity, setFull, u.CombatXp);
                layer.Show(C, s, slide: false);
                var badge = layer.transform.Find("Tokens/" + row + "/Badge");
                Assert.IsNotNull(badge, $"{row} is drawn");
                var at = badge.position; at.y = 0f;
                foreach (var km in new[] { 31f, 60f })
                {
                    rig.LookAt = at; rig.Distance = km * 1000f; rig.Yaw = 10f; rig.Apply();
                    for (int i = 0; i < 4; i++) { layer.FaceCamera(cam); UnitFlagFacing.FaceAll(cam.transform.position); cam.Render(); }
                    var px = Read(rt);
                    var still = FormattableString.Invariant($"{name}-{km:0}km");
                    Write(Path.Combine(dir, still + ".png"), px, Width, Height);
                    var sp = cam.WorldToScreenPoint(badge.position);
                    WriteCrop(Path.Combine(dir, still + "-badge.png"), px, Mathf.RoundToInt(sp.x * Width / cam.pixelWidth), Mathf.RoundToInt(sp.y * Height / cam.pixelHeight));
                    Debug.Log(FormattableString.Invariant($"SHOT {still}: {row} {s.Units[id].Strength} of {UnitDamage.FullStrength(C, s.Units[id])}, badge {badge.lossyScale.x:F0} m at ({sp.x:F0}, {sp.y:F0})"));
                }
            }
        }
        finally
        {
            cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt);
            UnitFigures.Source = null; UnitFigures.Forget();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    static Color32[] Read(RenderTexture from)
    {
        var prev = RenderTexture.active; RenderTexture.active = from;
        var tex = new Texture2D(from.width, from.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, from.width, from.height), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels32(); Object.DestroyImmediate(tex);
        return px;
    }

    static void Write(string path, Color32[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
    }

    /// <summary>A <see cref="Crop"/>-pixel square round (<paramref name="cx"/>, <paramref name="cy"/>) drawn <see cref="Zoom"/>
    /// times larger, nearest pixel, so the pips can be counted.</summary>
    static void WriteCrop(string path, Color32[] px, int cx, int cy)
    {
        int size = Crop * Zoom;
        var outPx = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int sx = Mathf.Clamp(cx - Crop / 2 + x / Zoom, 0, Width - 1), sy = Mathf.Clamp(cy - Crop / 2 + y / Zoom, 0, Height - 1);
                outPx[y * size + x] = px[sy * Width + sx];
            }
        Write(path, outPx, size, size);
    }
}
