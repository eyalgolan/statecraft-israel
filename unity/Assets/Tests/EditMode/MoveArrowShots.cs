#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#2134's pull request stills: Golani's Move pick in the game scene, the camera on Golani, the arrow to a lit hex
/// two hexes away hidden (as main draws it: nothing) and shown, at two zooms, in English and Hebrew; the arrow to a go-to hex
/// two turns away beside the route's turn numbers; and frames of the chevrons' flow for a clip. To ShotDir. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class MoveArrowShots
{
    static string ShotDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "move-arrow-shots"));
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-move-arrow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ShotDir);
        Directory.CreateDirectory(CivMovementShots.ShotDir);   // CivMovementShots.Shot writes there first
    }

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    void Shot(string name)
    {
        CivMovementShots.Shot(rt!, name);
        var from = Path.Combine(CivMovementShots.ShotDir, name + ".png");
        File.Copy(from, Path.Combine(ShotDir, name + ".png"), true);
        File.Delete(from);
    }

    /// <summary>GameRoot.LateUpdate draws the route and arrow to the hex under the batch editor's own pointer whenever that
    /// hex changes (a new camera pose changes it): a few frames let it settle, so what a still draws next stays drawn.</summary>
    static IEnumerator Settle() { for (int i = 0; i < 5; i++) yield return null; }

    IEnumerator Shots(bool hebrew)
    {
        VisualElement ui = null!; GameRoot root = null!;
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        var bridge = root.Bridge!; var units = root.Units!; var info = root.Info!;
        var map = Object.FindFirstObjectByType<MapScene>();
        var c = bridge.Content; var s = bridge.State;
        var golani = (s.Units[0].Q, s.Units[0].R);
        var reach = Movement.Reachable(c, s, 0, bridge.Spent);
        // The lit hex the arrow points at: the first in Reachable's order whose path enters two hexes (MoveArrowTests' pick).
        var lit = reach.Keys.First(h => Movement.Path(c, s, 0, h.Q, h.R, bridge.Spent).Count >= 2);
        var far = c.Hexes.Where(h => h.Land && !reach.ContainsKey((h.Q, h.R)) && (h.Q, h.R) != golani)
            .Select(h => (h.Q, h.R)).First(h => Movement.Route(c, s, 0, h.Q, h.R, bridge.Spent).LastOrDefault().Turn == 2);
        var lang = hebrew ? "he" : "en";
        GameRoot.OnHexClicked(map, units, info, golani.Item1, golani.Item2);
        for (int i = 0; i < 3; i++) yield return null;
        // GameRoot.LateUpdate follows the batch editor's own pointer, so the stills draw what a hover over the hex draws
        // (InfoPanelPresenter.ShowRouteTo) on the map directly, as CivRouteShots does.
        var arrow = InfoPanelPresenter.ArrowHexes(s.Units[0], Movement.Path(c, s, 0, lit.Q, lit.R, bridge.Spent));
        foreach (var zoom in new[] { 45000f, 120000f })
        {
            CivMovementShots.Aim(golani.Item1, golani.Item2, zoom);
            yield return Settle();
            map.ShowMoveArrow(Array.Empty<(int, int)>());
            map.ShowRoute(Array.Empty<(int, int, int)>());
            yield return null;
            Shot($"{lang}-{zoom / 1000:0}km-before");
            map.ShowMoveArrow(arrow);
            yield return null;
            Shot($"{lang}-{zoom / 1000:0}km-after");
        }
        var route = Movement.Route(c, s, 0, far.Item1, far.Item2, bridge.Spent);
        CivMovementShots.Aim(golani.Item1, golani.Item2, 120000f);
        yield return Settle();
        map.ShowRoute(route.Select(h => (h.Q, h.R, h.Turn)).ToList());
        map.ShowMoveArrow(InfoPanelPresenter.ArrowHexes(s.Units[0], route.Select(h => (h.Q, h.R))));
        root.Labels?.Refresh(true);
        for (int i = 0; i < 5; i++) yield return null;
        root.Labels?.Refresh(true);
        yield return null;
        Shot($"{lang}-120km-goto-after");
        if (hebrew) yield break;
        // The clip: the arrow to the lit hex at the closer zoom, a frame about every 80 ms of the editor's clock.
        CivMovementShots.Aim(golani.Item1, golani.Item2, 45000f);
        yield return Settle();
        map.ShowRoute(Array.Empty<(int, int, int)>());
        map.ShowMoveArrow(arrow);
        for (int f = 0; f < 16; f++)
        {
            var until = Time.realtimeSinceStartup + 0.08f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Shot($"clip-{f:00}");
        }
        Debug.Log($"SHOT arrow {string.Join(" ", arrow.Select(h => $"{h.q},{h.r}"))}; goto {far.Item1},{far.Item2}");
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
