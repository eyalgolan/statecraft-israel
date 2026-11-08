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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the before and after stills for #2744 (refs #2740), in English and Hebrew, to
/// `.superpowers/shots-2744/&lt;tag&gt;/` (local, not committed): Assets/Map.unity, its UI drawn through a copy of its
/// PanelSettings into a transparent 1280x720 texture and laid over the main camera's own 1280x720 frame, as CivMovementShots
/// does, so the map shows under the UI. Each state is the one the PR's own tests build, on a golden-seed
/// game the root goes on with (GameRoot.Begin keep:), so the shot is the real screen over the real map.
///
/// Shots (`{lang}` is `en` or `he`):
/// - `dollars-refused-{lang}`: the Defence screen's dollar panel at the war rung with reserves 10 and nothing converted yet,
///   after the queued `austerity_1949` "decline" (fx_reserves -8), with the box at 4
///   (DollarConversionTests.AQueuedChoiceThatSpendsReserves...). The golden game is played by HistoricalAI to the first turn
///   on or after 10 March 1949, the day the card's office, rationing_minister, first has a holder (content/people.yaml,
///   Dov Yosef from 1949-03-10; content/offices.yaml, the office from 1949-03-10): the card cannot be drawn on 15 May 1948.
///   The war rung (EconomySystem.WarRung) is set, as the test sets it, in case the run has left it by then.
///   After: the reserves refusal line under Convert. Before: no refusal line, as the panel read the state without the queue.
/// - `dollars-refused-pressed-{lang}`: the same panel after the player presses Convert. After: nothing more queued, the refusal
///   still shown. Before: the conversion shows as queued, which End turn then refuses.
/// - `reinforce-after-reassign-{lang}`: Golani's unit card, one strength short, in Tel Aviv, whose only idle civilian a queued
///   ReassignCraftsmen takes (UnitOrdersTests.AQueuedCraftsmanReassignmentHidesReinforce...). After: no Reinforce button, the
///   sim's refusal ("0 civilians without work") in the card's reason line (info-reason). Before: Reinforce is offered and
///   enabled. On 15 May 1948; the pending events (the opening idf_founding card) are cleared before the root draws the game,
///   so no event card covers the map, and the button or the reason line is scrolled into the info panel's view. The SHOT
///   line logs their world bounds and whether each lies inside the 1280x720 texture.
///
/// Run (Unity closed on the project; the same file, unchanged, on both trees, at the same resolution):
///   AFTER, with the PR head checked out:
///     SHOTS_TAG=after  &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter PanelPreviewShots -testResults /tmp/shots-2744-after.xml
///   BEFORE, with this file and its .meta copied onto main:
///     SHOTS_TAG=before &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter PanelPreviewShots -testResults /tmp/shots-2744-before.xml
/// Uses only APIs that exist on main as well (checked against origin/main when written). Explicit, so CI never runs it.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class PanelPreviewShots
{
    const int Width = 1280, Height = 720;
    string saveDir = "";
    string shotDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-shots-" + Guid.NewGuid().ToString("N"));
        var tag = Environment.GetEnvironmentVariable("SHOTS_TAG");
        shotDir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "shots-2744", string.IsNullOrEmpty(tag) ? "untagged" : tag);
        Directory.CreateDirectory(shotDir);
    }

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    SimBridge Bridge => root.Bridge!;
    static Content C => MapFocusTests.Inputs.Content;
    static UiText Text(bool he) => UiText.Parse(UiPaths.StringsText(he));

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>A golden-seed historical game, shaped by <paramref name="shape"/> before the root draws it.</summary>
    IEnumerator Boot(bool hebrew, Action<GameSession> shape)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        ShotCamera.Configure(Camera.main, Width, Height);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        // The UI goes into a transparent texture and Shot puts it over the map camera's frame, as CivMovementShots does.
        rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        if (hebrew) root.Language = UiLanguage.Hebrew;
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        shape(g);
        root.Begin(MapFocusTests.Inputs, 0, keep: new SimBridge(g, Text(hebrew)));
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
    }

    /// <summary>Puts any open event card away, as its Hide button does, and closes the advisor, so the screen under it can be seen.</summary>
    IEnumerator PutCardsAway()
    {
        var hide = ui.Q<Button>("event-hide");
        if (hide != null && hide.resolvedStyle.display != DisplayStyle.None && hide.enabledInHierarchy) UiTestKit.Press(hide);
        root.Card?.Collapse(takeFocus: false);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));   // as CivMovementShots.Boot
        yield return Frames(3);
    }

    static void IntoView(VisualElement? e)
    {
        if (e == null) return;
        e.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(e);
    }

    /// <summary>An element's world bound and whether it lies inside the panel, which the 1280x720 texture shows whole.</summary>
    string Bound(VisualElement? e)
    {
        if (e == null) return "(missing)";
        var b = e.worldBound; var all = ui.worldBound;
        var inside = b.width > 0 && b.height > 0 && b.xMin >= all.xMin && b.yMin >= all.yMin && b.xMax <= all.xMax && b.yMax <= all.yMax;
        return $"at {b} inside={inside} (panel {all})";
    }

    static Color[] Read(RenderTexture from)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = from;
        var tex = new Texture2D(from.width, from.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, from.width, from.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }

    /// <summary>The map camera's frame with the UI texture over it (the UI's pixels are premultiplied by their alpha), copied
    /// from CivMovementShots.Shot, which writes to its own folder.</summary>
    void Shot(string name)
    {
        var cam = Camera.main;
        ShotCamera.Require(cam, Width, Height);
        var frame = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture;
        cam.targetTexture = frame; cam.Render(); cam.targetTexture = before;
        var map = Read(frame); var over = Read(rt!);
        frame.Release(); Object.DestroyImmediate(frame);
        var outPx = new Color[map.Length];
        for (int i = 0; i < map.Length; i++)
        {
            var u = over[i]; var m = map[i];
            outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f);
        }
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.SetPixels(outPx); tex.Apply();
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"SHOT {name} -> {shotDir}");
    }

    IEnumerator DollarPanel(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew, g =>
        {
            // The first turn with a holder of rationing_minister (from 10 March 1949), as CabinetScreenTests.In1950 plays to 1950.
            while (g.State.Date < new DateTime(1949, 3, 10)) g.Step(HistoricalAI.Commands(C, g.State));
            Debug.Log($"SHOT dollars: game on {g.State.Date:yyyy-MM-dd}, rung {g.State.Rung}, reserves {g.State.FxReserves}, converted {g.State.DollarsConverted}");
            g.State.Rung = EconomySystem.WarRung;
            g.State.FxReserves = 10;
            g.State.DollarsConverted = 0;
            g.State.PendingEvents.Clear();
            g.State.PendingEvents.Add("austerity_1949");
        });
        Debug.Log($"SHOT dollars: queue decline -> {Bridge.Queue(new ChooseEventOption("austerity_1949", "decline")) ?? "queued"}");
        yield return PutCardsAway();
        UiTestKit.Press(ui.Q<Button>("nav-defence"));
        yield return Frames(5);
        var amount = ui.Q<IntegerField>("dollars-amount");
        amount.value = 4;
        IntoView(ui.Q("defence-dollars"));
        yield return Frames(5);
        Debug.Log($"SHOT dollars-refused-{lang}: refusal \"{ui.Q<Label>("dollars-refusal")?.text}\" {Bound(ui.Q("dollars-refusal"))}; convert {Bound(ui.Q("convert-dollars"))}");
        Shot($"dollars-refused-{lang}");

        UiTestKit.Press(ui.Q<Button>("convert-dollars"));
        yield return Frames(5);
        Debug.Log($"SHOT dollars-refused-pressed-{lang}: queued [{string.Join(", ", Bridge.Queued.Select(x => x.GetType().Name))}]");
        Shot($"dollars-refused-pressed-{lang}");
    }

    IEnumerator ReinforceCard(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        var id = C.StartingForces.ToList().FindIndex(f => f.Row == "golani");
        Assert.GreaterOrEqual(id, 0, "Golani is a starting force");
        var city = C.SettlementHexes.Single(h => h.Id == "tel_aviv");
        yield return Boot(hebrew, g =>
        {
            var u = g.State.Units[id];
            g.State.Units[id] = new Unit(u.Id, u.Type, city.Q, city.R, u.Service, u.Side, u.Strength - 1, u.Order, u.OrderTarget, u.HomeCity, u.Strength);
            var telAviv = g.State.Cities[city.Id];
            g.State.Cities[city.Id] = telAviv.WithCounts(1, 0, 0).WithCraftsmen(0).WithQualified(new Qualified(1, 0, Array.Empty<int>()));
            g.State.Arms = 100;
            // The opening decision (idf_founding) and any other waiting card would cover the map and the unit card.
            g.State.PendingEvents.Clear();
        });
        yield return PutCardsAway();
        Debug.Log($"SHOT reinforce: queue reassign -> {Bridge.Queue(new ReassignCraftsmen(city.Id, 1)) ?? "queued"}");
        yield return Frames(3);
        root.FocusUnit("golani");
        yield return Frames(8);
        var button = ui.Q<Button>("info-reinforce");
        var reason = ui.Q<Label>("info-reason");
        var offered = button != null && button.style.display.value != DisplayStyle.None;
        IntoView(offered ? button : reason);
        yield return Frames(5);
        Debug.Log($"SHOT reinforce-after-reassign-{lang}: Reinforce {(button == null ? "missing" : button.style.display.value.ToString())}"
            + $" enabled={button?.enabledInHierarchy} {Bound(button)}; reason \"{reason?.text}\" {Bound(reason)}");
        Shot($"reinforce-after-reassign-{lang}");
    }

    [UnityTest] public IEnumerator DollarsEnglish() { yield return DollarPanel(false); }
    [UnityTest] public IEnumerator DollarsHebrew() { yield return DollarPanel(true); }
    [UnityTest] public IEnumerator ReinforceEnglish() { yield return ReinforceCard(false); }
    [UnityTest] public IEnumerator ReinforceHebrew() { yield return ReinforceCard(true); }
}
