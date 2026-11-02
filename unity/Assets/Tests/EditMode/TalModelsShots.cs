#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Before/after stills of Tal Heuberger's signed-off models in the real game scene (owner, 27 October 2026, #756: "Sign
/// off all 19"), in English and Hebrew: the game booted as CivMovementShots boots it, the map camera with the UI over it.
/// "Before" withholds the new models (main's map: every layer adds them last, so nothing else moves); "after" answers them.
/// The editor streams nothing, so the scenery layers are rebuilt with the geometry fixture's prefab answers. Explicit.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class TalModelsShots
{
    internal static string ShotDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "tal-models-shots"));
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    GameRoot root = null!;
    Action<string, CollectionDetail, Action<GameObject?>>? savedRural, savedPeople, savedSpecial, savedUnits;
    Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? savedTown;

    [SetUp]
    public void Prepare()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-tal-shots-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ShotDir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        savedRural = RuralLife.Request; savedPeople = CivilianPeople.Request; savedSpecial = SpecialBuildingModels.Request;
        savedUnits = UnitFigures.Source; savedTown = TownModelLayer.ModelAcquire;
    }

    [TearDown]
    public void DiscardScene()
    {
        RuralLife.Request = savedRural!; CivilianPeople.Request = savedPeople!; SpecialBuildingModels.Request = savedSpecial!;
        UnitFigures.Source = savedUnits; TownModelLayer.ModelAcquire = savedTown!;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameObject? Prefab(string id, CollectionDetail detail) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));

    internal static Action<string, CollectionDetail, Action<GameObject?>> Answer(ICollection<string> withheld) =>
        (id, detail, done) => done(withheld.Contains(id) ? null : Prefab(id, detail));

    static void Drop(MapScene map, string name)
    {
        var old = map.Terrain.transform.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
    }

    internal static void Look(Vector3 at, float distance)
    {
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = at; rig.Distance = distance; rig.Yaw = 0f;
        rig.Apply();
    }

    IEnumerator Boot(bool hebrew)
    {
        yield return CivMovementShots.Boot(hebrew, saveDir, (t, p, u, g) => { rt = t; ps = p; root = g; });
    }

    IEnumerator Bedouin(bool hebrew, bool after)
    {
        yield return Boot(hebrew);
        var map = Object.FindFirstObjectByType<MapScene>();
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        void Layers(ICollection<string> withheld)
        {
            RuralLife.Request = CivilianPeople.Request = Answer(withheld);
            Drop(map, RuralLife.Name); Drop(map, CivilianPeople.Name);
            // GameRoot's order and inputs: the figures first, then the rural scenes, which keep clear of them.
            CollectionGeometryFixture.People(map, state.Date, state.Placements, content, state.Settlements, state.CityProjects.Values);
            CollectionGeometryFixture.Rural(map);
        }
        // The frames come from the map with the new models, so they are the same before and after: where the tent and the
        // figure stand. Both layers add them last, so nothing else moves when they are withheld.
        Layers(Array.Empty<string>());
        var rural = map.Terrain.transform.Find(RuralLife.Name).GetComponent<RuralLife>();
        var people = map.Terrain.transform.Find(CivilianPeople.Name).GetComponent<CivilianPeople>();
        var tent = rural.Placed.First(p => p.Model == RuralLife.BedouinTent);
        var figure = people.Placed.First(p => p.Model == CivilianPeople.BedouinCivilian);
        // The tent's frame holds the tent and the nearest code-built camp (coordinator decision on #1893: the tent stands in the
        // camp area, apart from the camps' plots): the midpoint of the tent and that camp's footprint, from the closest zoom the
        // player can reach or as far as needed to hold both. Every placed tent's distance to its nearest camp is logged.
        Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
        var camps = map.KitFootprints.Where(f => f.id.StartsWith("camp:", StringComparison.Ordinal)).ToList();
        (Vector2 centre, float radius, float edge) Nearest(Vector3 at) => camps.Select(f => (f.centre, f.radius, Vector2.Distance(Flat(at), f.centre) - f.radius)).OrderBy(c => c.Item3).First();
        foreach (var placed in rural.Placed.Where(p => p.Model == RuralLife.BedouinTent && p.Detail == CollectionDetail.Near))
            Debug.Log($"SHOT bedouin tent {placed.Key}: nearest code-built camp footprint edge {Nearest(placed.At).edge:F0} m away");
        var camp = Nearest(tent.At);
        var campAt = new Vector3(camp.centre.x, 0f, camp.centre.y);
        float span = Vector2.Distance(Flat(tent.At), camp.centre) + camp.radius;
        var frames = new[] { ("tent", tent.Key, (tent.At + campAt) * 0.5f, Mathf.Max(CameraController.MinDistance, 2.5f * span)),
                             ("figure", figure.Key, figure.At, CameraController.MinDistance) };
        if (!after)
        {
            Layers(new[] { CollectionCatalog.BedouinTent, CollectionCatalog.BedouinCivilian });
            rural = map.Terrain.transform.Find(RuralLife.Name).GetComponent<RuralLife>();
            people = map.Terrain.transform.Find(CivilianPeople.Name).GetComponent<CivilianPeople>();
        }
        Debug.Log($"SHOT bedouin {(after ? "after" : "before")}: rural tents {rural.Placed.Count(p => p.Model == RuralLife.BedouinTent)}, " +
                  $"figures {people.Placed.Count(p => p.Model == CivilianPeople.BedouinCivilian)}, tent size {RuralLife.SizeOf(RuralLife.BedouinTent)} m");
        foreach (var (what, key, flat, distance) in frames)
        {
            var at = new Vector3(flat.x, 0f, flat.z);
            at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
            Look(at, distance);
            for (int i = 0; i < 5; i++) yield return null;
            CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"bedouin-{what}-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
            Debug.Log($"SHOT bedouin {what} at {key} {at} from {distance:F0} m");
        }
    }

    /// <summary>Degania Alef after the first turn (15 June 1948: the tank stands from 20 May), the camera on the kibbutz's own point,
    /// so the frame is the same with and without the tank.</summary>
    IEnumerator Degania(bool hebrew, bool after)
    {
        yield return Boot(hebrew);
        root.Bridge!.EndTurn();
        for (int i = 0; i < 3; i++) yield return null;
        var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        var close = ui.Q<Button>("turn-report-close");
        if (close != null && close.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(close);
        for (int i = 0; i < 3; i++) yield return null;
        // The new turn opens a decision card over the map; the player's Hide button puts it away, and the card shrinks away
        // (ScreenNav.Show). The frame is taken only once the card itself is undisplayed.
        var card = ui.Q("event-card");
        Assert.IsNotNull(card, "the event card");
        if (card.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(ui.Q<Button>("event-hide"));
        for (int i = 0; i < 120 && card.resolvedStyle.display != DisplayStyle.None; i++) yield return null;
        Assert.AreEqual(DisplayStyle.None, card.resolvedStyle.display, "the decision card is hidden before the frame");
        var map = Object.FindFirstObjectByType<MapScene>();
        SpecialBuildingModels Layer(ICollection<string> withheld)
        {
            SpecialBuildingModels.Request = Answer(withheld);
            Drop(map, SpecialBuildingModels.Name);
            return CollectionGeometryFixture.Special(map, root.Bridge.State.Date)!;
        }
        // The frame is where the tank stands on this date with the model answered, the same before and after.
        var layer = Layer(Array.Empty<string>());
        var tank = layer.Placed.First(p => p.Model == CollectionCatalog.SyrianR35);
        if (!after) layer = Layer(new[] { CollectionCatalog.SyrianR35 });
        var point = tank.At;
        point.y = map.Terrain.SampleHeight(point) + map.Terrain.transform.position.y;
        // The closest the player's camera comes (CameraController.MinDistance).
        Look(point, CameraController.MinDistance);
        for (int i = 0; i < 5; i++) yield return null;
        CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"degania-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
        Debug.Log($"SHOT degania on {root.Bridge.State.Date:yyyy-MM-dd} at {point} from {CameraController.MinDistance:F0} m, event card display {card.resolvedStyle.display}: {string.Join(", ", layer.Placed.Select(p => p.Key))}");
    }

    sealed class NoLease : IDisposable { public void Dispose() { } }

    /// <summary>A town's houses from the closest zoom the player can reach, the camera on the centre of the town's placed
    /// houses (the plots are the same on main and on this branch: the model choice draws no random number). The town layer's
    /// building requests are answered with the editor's prefabs, as the player's stream would answer them. Run on main and on
    /// this branch for the before and after.</summary>
    IEnumerator Houses(bool hebrew, string? town)
    {
        yield return Boot(hebrew);
        TownModelLayer.ModelAcquire = (id, detail, done) => { done(Prefab(id, detail)); return new NoLease(); };
        var layer = Object.FindFirstObjectByType<TownModelLayer>();
        Assert.IsNotNull(layer, "the town layer");
        // The village: the first locality (by id) on the stone-village lattice whose houses include the plastered courtyard house.
        town ??= ((IEnumerable<(string id, Bounds bounds)>)typeof(TownModelLayer)
                .GetField("buildingPlaces", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(layer)!)
            .Select(p => p.id).Where(id => layer.InstancesOf(id).Any(i => i.Model == TownModel.G05CourtyardHouse))
            .OrderBy(id => id, StringComparer.Ordinal).First();
        var placed = layer.InstancesOf(town);
        Assert.IsNotEmpty(placed, town);
        var map = Object.FindFirstObjectByType<MapScene>();
        var sum = Vector3.zero; foreach (var i in placed) sum += (Vector3)i.M.GetColumn(3);
        var at = sum / placed.Count;
        at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
        Look(at, CameraController.MinDistance);
        for (int i = 0; i < 3; i++) { Camera.main.Render(); yield return null; }
        string models = string.Join(", ", placed.GroupBy(i => i.Model).OrderBy(g => g.Key).Select(g => $"{g.Key} x{g.Count()}"));
        CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"houses-{town}-{(hebrew ? "he" : "en")}"));
        Debug.Log($"SHOT houses {town} at {at} from {CameraController.MinDistance:F0} m: {models}");
    }

    [UnityTest] public IEnumerator HousesNetanyaEnglish() { yield return Houses(false, "netanya"); }
    [UnityTest] public IEnumerator HousesNetanyaHebrew() { yield return Houses(true, "netanya"); }
    [UnityTest] public IEnumerator HousesTiberiasEnglish() { yield return Houses(false, "tiberias"); }
    [UnityTest] public IEnumerator HousesTiberiasHebrew() { yield return Houses(true, "tiberias"); }
    [UnityTest] public IEnumerator HousesVillageEnglish() { yield return Houses(false, null); }
    [UnityTest] public IEnumerator HousesVillageHebrew() { yield return Houses(true, null); }

    [UnityTest] public IEnumerator DeganiaBeforeEnglish() { yield return Degania(false, false); }
    [UnityTest] public IEnumerator DeganiaAfterEnglish() { yield return Degania(false, true); }
    [UnityTest] public IEnumerator DeganiaBeforeHebrew() { yield return Degania(true, false); }
    [UnityTest] public IEnumerator DeganiaAfterHebrew() { yield return Degania(true, true); }

    /// <summary>The Lebanese token's actual placed spot near Al-Malikiyya after the first turn (15 June
    /// 1948, inside the token's 6 June to 8 July window), from the unit zoom and from the closest zoom. The unit figures are
    /// answered with the editor's prefabs, as the player's stream would answer them. The admitted frame is a candidate-only diagnostic; a matched before requires this identical fixture and state.</summary>
    static void ObserveSubject(MapScene map, UnitLayer units, string stage, Vector3 target)
    {
        var camera = Camera.main;
        var policy = map.Residency.Units;
        bool ground = CollectionResidencyView.TryGroundFocus(camera, out var focus);
        var token = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/lebanese_army");
        Debug.Log($"MODEL-OBS {stage} frame={Time.frameCount} token={token != null} active={units.gameObject.activeInHierarchy} arrived={UnitFigures.Arrived(UnitFigures.LebaneseRegular)} version={UnitFigures.AvailabilityVersion} targetVP={camera.WorldToViewportPoint(target).ToString("R")} ground={ground}:{focus.ToString("R")} view={Statecraft.Game.Map.Kits.TownModelLayer.ViewDistance(camera)} policy={policy.EnterM}/{policy.ExitM}/{policy.ViewEnterM}/{policy.ViewExitM} mask={camera.cullingMask} clip={camera.nearClipPlane}/{camera.farClipPlane}");
        foreach (var demand in units.GetComponentsInChildren<UnitFigureResidency>(true).Where(o => o.name.StartsWith("Original demand lebanese_army|", StringComparison.Ordinal)))
            Debug.Log($"MODEL-OBS {stage} prefetch={demand.name} position={demand.transform.position.ToString("R")} focusDistance={Vector2.Distance(new Vector2(focus.x, focus.z), new Vector2(demand.transform.position.x, demand.transform.position.z))} snapshot={JsonUtility.ToJson(demand.ResidencySnapshot())}");
        if (token == null) return;
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        foreach (var owner in token.GetComponentsInChildren<UnitFigureResidency>(true))
        {
            var piece = owner.transform;
            var model = piece.Find(UnitFigures.ModelName);
            Debug.Log($"MODEL-OBS {stage} piece={piece.name} position={piece.position.ToString("R")} scale={piece.lossyScale.ToString("R")} dressed={UnitFigures.Dressed(piece)} model={model != null}");
            if (model == null) continue;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                Debug.Log($"MODEL-OBS {stage} renderer={renderer.name} enabled={renderer.enabled} off={renderer.forceRenderingOff} active={renderer.gameObject.activeInHierarchy} layer={renderer.gameObject.layer} inMask={(camera.cullingMask & (1 << renderer.gameObject.layer)) != 0} bounds={renderer.bounds.center.ToString("R")}/{renderer.bounds.size.ToString("R")} frustum={GeometryUtility.TestPlanesAABB(planes, renderer.bounds)} visible={renderer.isVisible} materials={string.Join(",", renderer.sharedMaterials.Select(m => m == null ? "null" : m.name + ":" + m.shader.name))}");
        }
    }

    IEnumerator Malikiyya(bool hebrew)
    {
        UnitFigures.Source = (id, detail, done) => done(Prefab(id, detail));
        yield return Boot(hebrew);
        // Keep the real rig's projection and the saved RenderTexture on the same aspect throughout these frames.
        Camera.main.aspect = (float)CivMovementShots.Width / CivMovementShots.Height;
        root.Bridge!.EndTurn();
        for (int i = 0; i < 3; i++) yield return null;
        var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        var close = ui.Q<Button>("turn-report-close");
        if (close != null && close.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(close);
        // The new turn opens a decision card over the map; the player's Hide button puts it away, and the card shrinks away.
        var card = ui.Q("event-card");
        Assert.IsNotNull(card, "the event card");
        if (card.resolvedStyle.display == DisplayStyle.Flex) UiTestKit.Press(ui.Q<Button>("event-hide"));
        for (int i = 0; i < 120 && card.resolvedStyle.display != DisplayStyle.None; i++) yield return null;
        Assert.AreEqual(DisplayStyle.None, card.resolvedStyle.display, "the decision card is hidden before the frame");
        var subjects = root.Units!.Tokens.Where(t => t.Token.Unit.Id == "lebanese_army").ToArray();
        Assert.AreEqual(1, subjects.Length, "capture precondition: exactly one actual Lebanese army token");
        Assert.IsTrue(UnitFigures.Arrived(UnitFigures.LebaneseRegular), "capture precondition: the Lebanese original has arrived");
        var subject = subjects[0];
        int q = subject.Q, r = subject.R;
        var map = Object.FindFirstObjectByType<MapScene>();
        var target = new Vector3((float)subject.Spot.X, 0f, (float)subject.Spot.Z);
        target.y = map.Terrain.SampleHeight(target) + map.Terrain.transform.position.y;
        var demands = root.Units!.GetComponentsInChildren<UnitFigureResidency>(true)
            .Where(o => o.name.StartsWith("Original demand lebanese_army|", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(1, demands.Length, "capture precondition: one real Lebanese source-prefetch holder");
        var prefetch = demands[0];
        var here = root.Units!.Tokens.Where(t => t.Q == q && t.R == r).Select(t => $"{t.Token.Unit.Id} ({t.Token.ArtType})").ToList();
        foreach (var (what, distance) in new[] { ("unit", 40000f), ("admitted", 24000f), ("close", CameraController.MinDistance) })
        {
            if (what == "admitted")
            {
                var anchor = prefetch.transform.position;
                Assert.Less(Vector2.Distance(new Vector2(anchor.x, anchor.z), new Vector2(target.x, target.z)),
                    2f * map.Residency.Units.EnterM, "one real focus must be able to admit both anchors");
                var middle = new Vector3((anchor.x + target.x) * .5f, 0f, (anchor.z + target.z) * .5f);
                Look(middle, distance);
                // This south-facing admitted frame is checked against all original figure corners below the top bar.
                Camera.main.GetComponent<CameraRig>().Yaw = 180f;
                Camera.main.GetComponent<CameraRig>().Apply();
                Assert.IsTrue(CollectionResidencyView.TryGroundFocus(Camera.main, out var measured));
                var rig = Camera.main.GetComponent<CameraRig>();
                // The UI inset shifts the centre ray. Pan the real rig so its ground focus, not just LookAt, is the midpoint.
                rig.LookAt += new Vector3(middle.x - measured.x, 0f, middle.z - measured.z);
                rig.Apply();
            }
            else Look(target, distance);
            for (int i = 0; i < 5; i++) yield return null;
            if (what == "admitted")
            {
                Assert.Less(Statecraft.Game.Map.Kits.TownModelLayer.ViewDistance(Camera.main), map.Residency.Units.ViewEnterM,
                    "capture control requires a view inside the approved enter band");
                Assert.IsTrue(CollectionResidencyView.TryGroundFocus(Camera.main, out var focus));
                Assert.Less(Vector2.Distance(new Vector2(focus.x, focus.z), new Vector2(target.x, target.z)), map.Residency.Units.EnterM,
                    "capture control requires the actual placed subject inside the approved enter radius");
                Assert.Less(Vector2.Distance(new Vector2(focus.x, focus.z), new Vector2(prefetch.transform.position.x, prefetch.transform.position.z)), map.Residency.Units.EnterM,
                    "capture control requires the source-prefetch anchor inside the approved enter radius");
                var camera = Camera.main;
                var frame = new RenderTexture(CivMovementShots.Width, CivMovementShots.Height, 24, RenderTextureFormat.ARGB32);
                var previous = camera.targetTexture;
                bool Ready() => prefetch != null && prefetch.ResidencySnapshot().ownedLeases > 0
                    && UnitFigures.Arrived(UnitFigures.LebaneseRegular)
                    && map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/lebanese_army") != null;
                int renders = 0;
                try
                {
                    // Use the real render callbacks to re-admit the original after the deliberately out-of-band frame.
                    for (; renders < 30; renders++)
                    {
                        camera.targetTexture = frame; camera.Render(); camera.targetTexture = previous;
                        yield return null;
                        if (Ready()) break;
                    }
                    ObserveSubject(map, root.Units, "admitted primed", target);
                    Assert.IsTrue(Ready(), $"source, lease and token must be ready after at most 30 real renders ({renders + 1})");
                    camera.targetTexture = frame; camera.Render(); camera.targetTexture = previous;
                    yield return null;
                    Assert.IsTrue(Ready(), "admitted readiness must survive another real render");
                }
                finally { camera.targetTexture = previous; frame.Release(); Object.DestroyImmediate(frame); }
                var viewport = camera.WorldToViewportPoint(target);
                Assert.IsTrue(viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f
                    && viewport.y < 1f - Mathf.Clamp(camera.GetComponent<CameraRig>().TopInset, 0f, CameraRig.MaxTopInset),
                    "the admitted subject is inside the map viewport below the top bar");
                Assert.AreEqual((float)CivMovementShots.Width / CivMovementShots.Height, camera.aspect, 0.0001f,
                    "viewport checks and saved capture must use the same aspect ratio");
                var live = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/lebanese_army");
                Assert.IsNotNull(live, "the admitted token remains live before whole-model projection");
                var owners = live.GetComponentsInChildren<UnitFigureResidency>(true);
                Assert.GreaterOrEqual(owners.Length, 3, "three distinct original figure owners must be framed");
                foreach (var owner in owners)
                {
                    Assert.Greater(owner.ResidencySnapshot().ownedLeases, 0, "each visible figure owns its admitted original lease");
                    var model = owner.transform.Find(UnitFigures.ModelName);
                    Assert.IsNotNull(model, "each original is dressed");
                    Assert.Greater(model.childCount, 0);
                    StringAssert.StartsWith(UnitFigures.LebaneseRegular, model.GetChild(0).name, "the actual Lebanese original");
                    Assert.IsTrue(model.GetComponentsInChildren<Renderer>(true)
                        .Any(r => r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy),
                        "each distinct figure has a live visible original renderer");
                }
                var visible = owners.Select(o => o.transform.Find(UnitFigures.ModelName))
                    .SelectMany(m => m.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy).ToArray();
                foreach (var renderer in visible)
                {
                    var box = renderer.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var point = camera.WorldToViewportPoint(new Vector3((corner & 1) == 0 ? box.min.x : box.max.x,
                            (corner & 2) == 0 ? box.min.y : box.max.y, (corner & 4) == 0 ? box.min.z : box.max.z));
                        Assert.IsTrue(point.z > 0f && point.x > 0f && point.x < 1f && point.y > 0f
                            && point.y < 1f - Mathf.Clamp(camera.GetComponent<CameraRig>().TopInset, 0f, CameraRig.MaxTopInset),
                            $"whole original figure {renderer.name} corner {corner} must be visible below the top bar: {point}");
                    }
                }
                Debug.Log($"MODEL-OBS admitted readiness renders={renders + 2} viewport={viewport.ToString("R")}");
                Assert.IsTrue(UnitFigures.Arrived(UnitFigures.LebaneseRegular), "the admitted source is ready before the frame");
                Assert.Greater(prefetch.ResidencySnapshot().ownedLeases, 0, "the source-prefetch holder owns its admitted lease");
                Assert.IsNotNull(map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/lebanese_army"),
                    "the admitted token exists before the frame");
            }
            ObserveSubject(map, root.Units, what + " before", target);
            CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"malikiyya-{what}-{(hebrew ? "he" : "en")}"));
            ObserveSubject(map, root.Units, what + " after", target);
            if (what == "admitted")
            {
                var live = map.Terrain.transform.Find(UnitLayer.RootName + "/Tokens/lebanese_army");
                Assert.IsNotNull(live, "admitted model capture must retain its actual Lebanese token after render");
                var models = live!.GetComponentsInChildren<UnitFigureResidency>(true)
                    .Where(o => o.transform.Find(UnitFigures.ModelName) != null).ToArray();
                Assert.GreaterOrEqual(models.Length, 3, "admitted model capture must retain all three original figures after render");
            }
            Debug.Log($"SHOT malikiyya {what} on {root.Bridge.State.Date:yyyy-MM-dd} hex {q},{r} from {distance:F0} m, target {target.ToString("R")} camera {Camera.main.transform.position.ToString("R")} rotation {Camera.main.transform.rotation.ToString("R")}, event card display {card.resolvedStyle.display}: tokens [{string.Join(", ", here)}]");
        }
    }

    /// <summary>#1932 (c): the Egyptian Spitfire in flight near the western board edge (30 km in) toward el-Arish on the opening turn (15 May
    /// 1948, the first sourced Spitfire raid), close (the closest zoom), at a gameplay zoom and in an overview of the southern
    /// coast. "Before" withholds the Spitfire. Each shot is the player's camera (CameraRig.Pitch, yaw 0) aimed at the ground
    /// under the plane and held on the map as CameraController.Step holds it, so it shows what a player sees (#2014).</summary>
    IEnumerator Spitfire(bool hebrew, bool after)
    {
        var savedCraft = SeaAirCollection.Request;
        try
        {
            yield return Boot(hebrew);
            var map = Object.FindFirstObjectByType<MapScene>();
            var state = root.Bridge!.State; var content = root.Bridge.Content;
            SeaAirCollection.Request = Answer(Array.Empty<string>());
            Drop(map, SeaAirCollection.Name);
            var layer = CollectionGeometryFixture.Craft(map, state.Date, state.Placements, content)!;
            var plane = layer.Placed.Single(p => p.Model == SeaAirCollection.Spitfire);
            if (!after)
            {
                SeaAirCollection.Request = Answer(new[] { SeaAirCollection.Spitfire });
                Drop(map, SeaAirCollection.Name);
                layer = CollectionGeometryFixture.Craft(map, state.Date, state.Placements, content)!;
            }
            Debug.Log($"SHOT spitfire {(after ? "after" : "before")} on {state.Date:yyyy-MM-dd}: drawn {layer.Placed.Count(p => p.Model == SeaAirCollection.Spitfire)}, at {plane.At}, span {SeaAirCollection.SizeOf(SeaAirCollection.Spitfire)} m");
            var rig = Camera.main.GetComponent<CameraRig>(); var cc = rig.GetComponent<CameraController>();
            foreach (var (what, distance) in new[] { ("close", CameraController.MinDistance), ("gameplay", 40000f), ("overview", 150000f) })
            {
                var under = new Vector3(plane.At.x, 0f, plane.At.z);   // the player's look-at is on y = 0, lifted by Step
                Look(under, distance);
                CameraController.Step(rig, default, 0f, cc.Bounds, cc.Ground, cc.GroundTop, true);
                for (int i = 0; i < 5; i++) yield return null;
                CivMovementShots.Shot(rt!, Path.Combine("..", "tal-models-shots", $"spitfire-{what}-{(after ? "after" : "before")}-{(hebrew ? "he" : "en")}"));
                var vp = Camera.main.WorldToViewportPoint(plane.At);
                Debug.Log($"SHOT spitfire {what} from {distance:F0} m, look-at {rig.LookAt} (asked {under}), camera {Camera.main.transform.position}, plane at viewport {vp}");
            }
        }
        finally { SeaAirCollection.Request = savedCraft; }
    }

    [UnityTest] public IEnumerator SpitfireBeforeEnglish() { yield return Spitfire(false, false); }
    [UnityTest] public IEnumerator SpitfireAfterEnglish() { yield return Spitfire(false, true); }

    [UnityTest] public IEnumerator MalikiyyaEnglish() { yield return Malikiyya(false); }
    [UnityTest] public IEnumerator MalikiyyaHebrew() { yield return Malikiyya(true); }

    [UnityTest] public IEnumerator BedouinBeforeEnglish() { yield return Bedouin(false, false); }
    [UnityTest] public IEnumerator BedouinAfterEnglish() { yield return Bedouin(false, true); }
    [UnityTest] public IEnumerator BedouinBeforeHebrew() { yield return Bedouin(true, false); }
    [UnityTest] public IEnumerator BedouinAfterHebrew() { yield return Bedouin(true, true); }
}
