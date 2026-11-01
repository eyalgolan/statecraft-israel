using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The owner, 31 October 2026, asked "Should badges shrink when you're close?": "Shrink up close (Recommended)". A
/// unit badge's plate keeps its world size (UnitBadge.SizeM) from a 30 km view out, and a nearer camera draws it no taller
/// than UnitBadge.MaxScreenShare of the screen (at 12 km over Gaza the Egyptian force's plate was about 700 of 1080 px).
/// Each view renders the units of the first turn at 1920x1080 with no top bar (the inset only moves the view's lower edge
/// further out, where badges are smaller), the look-at point swept from the token past the view's lower edge.</summary>
public class UnitBadgeScreenCapTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    const string Egypt = "egyptian_expeditionary_force";
    static readonly float[] Yaws = { 0f, 90f, 180f, 270f };
    /// <summary>How far (m) the look-at point sits beyond each token, along the view: from a little behind it (the token
    /// high on the screen) to past the view's lower edge (its badge leaves the screen at the bottom).</summary>
    static readonly float[] Ahead = { -4000f, 0f, 2000f, 4000f, 6000f, 8000f, 10000f, 12000f, 14000f, 16000f, 18000f, 20000f };

    [Test]
    public void BadgesKeepTheir30KmSizeAndNearerViewsCapTheirHeightOnScreen()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var rt = new RenderTexture(1920, 1080, 24);
        // An Arab force's token waits for its Collection figure, which edit mode never streams: answer at once with a
        // stand-in, as CollectionOtherLeaveTests does (the badge's place comes from the figure's set height, not its mesh).
        var standIn = GameObject.CreatePrimitive(PrimitiveType.Cube); standIn.transform.position = Vector3.one * -100000f;
        UnitFigures.Source = (_, __, done) => done(standIn); UnitFigures.Forget();
        try
        {
            Assert.AreEqual(CameraRig.Fov, cam.fieldOfView, 1e-3f, "the game camera's field of view");
            cam.targetTexture = rt; rig.TopInset = 0f;
            var units = UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
            var layer = UnitLayer.Create(map, units, C.Hexes, still: true);
            layer.Show(C, Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical));
            var tokens = map.Terrain.transform.Find(UnitLayer.RootName).Find("Tokens");
            var badges = layer.Tokens.Select(t => tokens.Find(t.Token.Unit.Id).Find("Badge")).ToList();
            Assert.That(badges.Count, Is.GreaterThan(5), "the first turn's tokens");
            // Where each token stands (its root sits at the origin: the pieces are placed in world space).
            var spots = badges.Select(b => { var p = b.position; p.y = 0f; return p; }).ToList();
            int egypt = layer.Tokens.ToList().FindIndex(t => t.Token.Unit.Id == Egypt);
            Assert.That(egypt, Is.GreaterThanOrEqualTo(0), $"the Egyptian force stands at Gaza on the first turn (drawn: {string.Join(", ", layer.Tokens.Select(t => t.Token.Unit.Id))})");

            // The plate's height on the screen (a share of it) and whether all of it is on the screen.
            (float share, bool shown) Drawn(Transform badge)
            {
                float half = 0.5f * badge.lossyScale.y;
                var corners = new[] { new Vector3(-1, -1), new Vector3(1, -1), new Vector3(-1, 1), new Vector3(1, 1) }
                    .Select(c => cam.WorldToViewportPoint(badge.position + badge.rotation * (c * half))).ToList();
                bool shown = corners.All(v => v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f);
                return (corners.Max(v => v.y) - corners.Min(v => v.y), shown);
            }
            // The look-at point (on y = 0, as the game's and the captures' are) the given distance beyond a token.
            void Look(int token, float distance, float yaw, float ahead)
            {
                rig.LookAt = spots[token] + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * ahead; rig.Distance = distance; rig.Yaw = yaw; rig.Apply();
                layer.FaceCamera(cam);
            }
            Vector3 LowerEdge(Transform badge) => badge.position - badge.up * (0.5f * badge.lossyScale.y);

            // From 30 km out every plate on the screen is today's: SizeM wide, and so no taller than the cap.
            float largest = 0f; int seen = 0; var changed = new List<string>();
            var lowerAt30 = new Dictionary<(int, float), Vector3>();
            foreach (var distance in new[] { 30000f, 45000f, 60000f })
                foreach (var yaw in Yaws)
                    for (int i = 0; i < badges.Count; i++)
                        foreach (var ahead in Ahead)
                        {
                            Look(i, distance, yaw, ahead);
                            if (distance == 30000f && ahead == 0f) lowerAt30[(i, yaw)] = LowerEdge(badges[i]);
                            for (int j = 0; j < badges.Count; j++)
                            {
                                var (share, shown) = Drawn(badges[j]);
                                if (!shown) continue;
                                seen++; largest = Mathf.Max(largest, share);
                                if (Mathf.Abs(badges[j].lossyScale.x - UnitBadge.SizeM) > 1e-2f)
                                    changed.Add($"{distance / 1000f:0} km, yaw {yaw}, {ahead / 1000f:0} km beyond {badges[i].parent.name}: {badges[j].parent.name}'s plate is {badges[j].lossyScale.x:0} m ({share * 1080f:0} px of 1080)");
                            }
                        }
            Debug.Log($"BADGECAP from 30 km out: the largest plate on the screen is {largest:0.0000} of its height ({largest * 1080f:0} px of 1080), over {seen} plates; the cap is {UnitBadge.MaxScreenShare}");
            Assert.Greater(seen, 1000, "plates seen from 30 km out");
            Assert.IsEmpty(changed, $"from 30 km out every plate keeps its size; {changed.Count} did not, first: {string.Join("; ", changed.Take(5))}");

            // Nearer, no plate on the screen is taller than the cap; it shrinks about its lower edge, which stays where it
            // was at 30 km (the camera's yaw alone places it), clear of the flag and the figures; clicks follow the plate.
            int capped = 0;
            foreach (var distance in new[] { CameraController.MinDistance, 8000f, 12000f, 20000f })
                foreach (var yaw in Yaws)
                    for (int i = 0; i < badges.Count; i++)
                        foreach (var ahead in Ahead)
                        {
                            Look(i, distance, yaw, ahead);
                            for (int j = 0; j < badges.Count; j++)
                            {
                                var (share, shown) = Drawn(badges[j]);
                                if (!shown) continue;
                                Assert.LessOrEqual(share, UnitBadge.MaxScreenShare + 1e-4f,
                                    $"{distance / 1000f:0} km, yaw {yaw}, {ahead / 1000f:0} km beyond {badges[i].parent.name}: {badges[j].parent.name}'s plate is {share * 1080f:0} px of 1080");
                                if (badges[j].lossyScale.x < UnitBadge.SizeM - 1f) capped++;
                            }
                            if (ahead != 0f) continue;
                            var b = badges[i];
                            Assert.Less((LowerEdge(b) - lowerAt30[(i, yaw)]).magnitude, 1f, $"{distance / 1000f:0} km, yaw {yaw}, {b.parent.name}: the plate's lower edge stays put");
                            var box = layer.KeepClear[i][0];
                            Assert.Less((box.center - b.position).magnitude, 1f, $"{b.parent.name}: the click box is round the plate as drawn");
                            Assert.AreEqual(b.lossyScale.x, box.size.x, 1e-2f, $"{b.parent.name}: the click box is the plate's own width");
                            var centre = cam.WorldToScreenPoint(b.position);
                            if (centre.z > 0f)
                                Assert.AreEqual(b.parent.name, layer.TokenAtScreen(cam, centre)?.Token.Unit.Id, $"{distance / 1000f:0} km, yaw {yaw}: a click on {b.parent.name}'s plate picks its unit");
                        }
            Assert.Greater(capped, 100, "nearer views shrink the plates");

            // The QA view of 31 October (v0.3.2, turn1-gaza-12km): the Egyptian plate low on the screen at 12 km, as low as
            // it stays whole.
            float low = new[] { 6000f, 5000f, 4000f, 3000f, 2000f, 0f }.First(a => { Look(egypt, 12000f, 0f, a); return Drawn(badges[egypt]).shown; });
            Look(egypt, 12000f, 0f, low);
            var (gaza, _) = Drawn(badges[egypt]);
            Debug.Log($"BADGECAP 12 km, {low / 1000f:0} km beyond Gaza: the Egyptian plate is {gaza * 1080f:0} px of 1080, {badges[egypt].lossyScale.x:0} m wide");
            Assert.Less(badges[egypt].lossyScale.x, 0.5f * UnitBadge.SizeM, "at 12 km the Egyptian plate is drawn smaller");
            Assert.LessOrEqual(gaza, UnitBadge.MaxScreenShare + 1e-4f, "and no taller than the cap");
        }
        finally
        {
            UnitFigures.Source = null; UnitFigures.Forget();
            cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(map.gameObject); Object.DestroyImmediate(standIn);
        }
    }
}
