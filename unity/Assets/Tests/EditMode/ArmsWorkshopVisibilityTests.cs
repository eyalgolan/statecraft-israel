#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Review of #2281 (coordinator, 3 November 2026: the workshop must be VISIBLE): in a gdd-0.3 game, a b21 built on a
/// city's own hex is not hidden behind a unit's figures in the map's oblique view. The camera frames the b21 from 9 km, as the
/// PR's stills do; no unit renderer nearer the camera covers half or more of the b21's screen rectangle. The figures are the
/// ones GameRoot draws for the game's units (UnitLayer), not a model of them.</summary>
public class ArmsWorkshopVisibilityTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    Action<string, CollectionDetail, Action<GameObject?>>? savedSpecial;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-b21-visible-" + Guid.NewGuid().ToString("N"));
        savedSpecial = SpecialBuildingModels.Request;
    }

    [TearDown]
    public void Discard()
    {
        SpecialBuildingModels.Request = savedSpecial!;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static Rect ScreenRect(Camera cam, Bounds b)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int c = 0; c < 8; c++)
        {
            var corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
            var q = cam.WorldToScreenPoint(corner);
            x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    /// <summary>Whether every corner of <paramref name="b"/> is in front of the camera: a box reaching behind it does not project
    /// to a rectangle on screen.</summary>
    static bool InFront(Camera cam, Bounds b)
    {
        for (int c = 0; c < 8; c++)
        {
            var corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
            if (cam.WorldToScreenPoint(corner).z <= cam.nearClipPlane) return false;
        }
        return true;
    }

    /// <summary>The view depth of <paramref name="b"/>'s nearest corner (review of #2281, N1: by view depth, not by the
    /// distance to its centre, so a tall renderer whose centre lies beyond the b21 but whose front reaches over it counts).</summary>
    static float NearestDepth(Camera cam, Bounds b)
    {
        float near = float.MaxValue;
        for (int c = 0; c < 8; c++)
            near = Mathf.Min(near, cam.WorldToScreenPoint(new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z)).z);
        return near;
    }

    static float Covered(Rect over, Rect model)
    {
        float w = Mathf.Min(over.xMax, model.xMax) - Mathf.Max(over.xMin, model.xMin), h = Mathf.Min(over.yMax, model.yMax) - Mathf.Max(over.yMin, model.yMin);
        return w <= 0f || h <= 0f ? 0f : w * h / (model.width * model.height);
    }

    IEnumerator Visible(string city)
    {
        var inputs = Statecraft.Game.Boot.GameInputs.FromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        foreach (var era in inputs.Content.Eras) { era.Systems.Ruleset = SaveGame.Gdd03Ruleset; era.Systems.Loop = EraSystems.Gdd; }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (ps != null) Object.DestroyImmediate(ps);
        rt = new RenderTexture(CivMovementShots.Width, CivMovementShots.Height, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new Statecraft.Game.SaveStore(saveDir);
        root.Begin(inputs, 0);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        for (int i = 0; i < 5; i++) yield return null;
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        Assert.AreEqual(SaveGame.Gdd03Ruleset, content.Ruleset);
        Assert.IsNotNull(root.Units, "the game's units are drawn");
        var home = content.SettlementHexes.Single(h => h.Id == city);
        int id = state.NextId++;
        state.CityProjects.Add(id, new CityProject(id, Workshops.ArmsWorkshop, city, home.Q, home.R, 0, state.Date));
        SpecialBuildingModels.Request = TalModelsShots.Answer(Array.Empty<string>());
        var old = map.Terrain.transform.Find(SpecialBuildingModels.Name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var layer = CollectionGeometryFixture.Special(map, state.Date, content, state.CityProjects.Values)!;
        var model = layer.transform.Find(SpecialBuildingModels.WorkshopKey(id));
        Assert.IsNotNull(model, city + " has its b21");
        var mb = model!.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        var at = mb.center; at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        TalModelsShots.Look(at, 9000f);
        for (int i = 0; i < 5; i++) yield return null;
        var cam = Camera.main!;
        var modelRect = ScreenRect(cam, mb);
        float modelDepth = cam.WorldToScreenPoint(mb.center).z;   // view depth
        float worst = 0f; string worstName = "none";
        foreach (var r in root.Units!.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.forceRenderingOff || !r.gameObject.activeInHierarchy) continue;
            if (!InFront(cam, r.bounds)) continue;   // behind the camera (or across its plane): not drawn over the b21
            if (NearestDepth(cam, r.bounds) >= modelDepth) continue;   // wholly beyond the b21's centre: it cannot hide it
            float covered = Covered(ScreenRect(cam, r.bounds), modelRect);
            if (covered > worst) { worst = covered; worstName = r.name + " of " + (r.transform.parent != null ? r.transform.parent.name : "") + " at " + r.bounds.center; }
        }
        TestContext.WriteLine($"{city}: b21 at {mb.center}, screen {modelRect}; the most a nearer unit renderer covers: {worst:P0} ({worstName})");
        Assert.Less(worst, 0.5f, $"{city}'s b21 is hidden behind a unit's figures ({worstName} covers {worst:P0} of it)");
    }

    [UnityTest] public IEnumerator TheJerusalemWorkshopIsNotHiddenBehindAUnit() { yield return Visible("jerusalem"); }
    [UnityTest] public IEnumerator TheTelAvivWorkshopIsNotHiddenBehindAUnit() { yield return Visible("tel_aviv"); }
    [UnityTest] public IEnumerator TheRehovotWorkshopIsNotHiddenBehindAUnit() { yield return Visible("rehovot"); }
}

/// <summary>Review of #2281 (S3): GameRoot's merge of the names' keep-clear boxes (GameRoot.KeepClearLists): the units' tokens,
/// the craft and the workshops' b21; one list alone as it is; several merged into one list kept until one of them is a new
/// list (a growing workshop list arrives as a new list each time); back to one list.</summary>
public class KeepClearListsTests
{
    static List<Bounds[]> Boxes(int n) => Enumerable.Range(0, n).Select(i => new[] { new Bounds(new Vector3(i, 0, 0), Vector3.one) }).ToList();

    [Test]
    public void TokensCraftAndWorkshopsMergeAndFollowNewLists()
    {
        var merge = new GameRoot.KeepClearLists();
        var tokens = Boxes(2); var craft = Boxes(1); var none = new List<Bounds[]>();
        Assert.AreSame(tokens, merge.Merge(tokens, null, null), "one list alone, as it is");
        Assert.AreSame(tokens, merge.Merge(tokens, none, none), "empty lists do not count");
        var w1 = Boxes(1);
        var all = merge.Merge(tokens, craft, w1)!;
        Assert.AreEqual(4, all.Count, "tokens, craft and workshops together");
        Assert.AreSame(all, merge.Merge(tokens, craft, w1), "the same lists: the same merged list (the labels do not refresh)");
        var w2 = Boxes(2);   // a second b21: the workshops publish a new list
        var grown = merge.Merge(tokens, craft, w2)!;
        Assert.AreNotSame(all, grown, "a new workshop list: a new merged list");
        Assert.AreEqual(5, grown.Count);
        Assert.AreSame(w2, merge.Merge(null, none, w2), "back to one list: that list");
        var again = merge.Merge(tokens, null, w2)!;
        Assert.AreEqual(4, again.Count, "one to several again");
        Assert.AreSame(tokens, merge.Merge(tokens, null, none), "and back to one");
    }
}
