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

/// <summary>#1398's go-to (U3), which main has no "before" for: Golani's Move pick with the route to a hex two turns away
/// shown, and the turn numbers on each turn's last hex, in English and Hebrew, to CivMovementShots' folder. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class CivRouteShots
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-civ-route-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(CivMovementShots.ShotDir);
    }

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    IEnumerator Shots(bool hebrew)
    {
        VisualElement ui = null!; GameRoot root = null!;
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; ui = u; root = g; });
        var bridge = root.Bridge!; var units = root.Units!; var info = root.Info!;
        var map = Object.FindFirstObjectByType<MapScene>();
        var c = bridge.Content;
        var golani = (bridge.State.Units[0].Q, bridge.State.Units[0].R);
        var lit = Movement.Reachable(c, bridge.State, 0).Keys.ToHashSet();
        var none = new System.Collections.Generic.Dictionary<int, int>();
        // A land hex two turns away, the nearest such in sim order (as UnitOrdersTests.AClickBeyondTheLitHexesGivesAGoTo).
        var far = c.Hexes.Where(h => h.Land && !lit.Contains((h.Q, h.R)) && (h.Q, h.R) != golani)
            .Select(h => (h.Q, h.R)).First(h => Movement.Route(c, bridge.State, 0, h.Q, h.R, none).LastOrDefault().Turn == 2);
        CivMovementShots.Aim(golani.Item1, golani.Item2, 60000f);
        GameRoot.OnHexClicked(map, units, info, golani.Item1, golani.Item2);
        yield return null;
        UiTestKit.Press(ui.Q<Button>("info-move"));
        yield return null;
        // GameRoot.Update follows the batch editor's own pointer every frame; once it has settled (its hover cache then calls
        // nothing more), the route a hover over the far hex shows is drawn on the map directly.
        for (int i = 0; i < 3; i++) yield return null;
        map.ShowRoute(Movement.Route(c, bridge.State, 0, far.Q, far.R, bridge.Spent).Select(h => (h.Q, h.R, h.Turn)).ToList());
        root.Labels?.Refresh(true);
        for (int i = 0; i < 5; i++) yield return null;
        root.Labels?.Refresh(true);
        yield return null;
        Debug.Log($"SHOT route to {far.Q},{far.R}: {map.ShownRoute.Count} hexes");
        CivMovementShots.Shot(rt!, $"5-route-{(hebrew ? "he" : "en")}");
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
