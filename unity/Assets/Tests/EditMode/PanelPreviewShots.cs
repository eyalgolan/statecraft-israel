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
/// `.superpowers/shots-2744/&lt;tag&gt;/` (local, not committed), as A11yKeyboardShots does: Assets/Map.unity drawn into a
/// 1280x720 RenderTexture through a copy of its PanelSettings. Each state is the one the PR's own tests build, on a golden-seed
/// game the root goes on with (GameRoot.Begin keep:), so the shot is the real screen over the real map.
///
/// Shots (`{lang}` is `en` or `he`):
/// - `dollars-refused-{lang}`: the Defence screen's dollar panel at the war rung with reserves 10, after the queued
///   `austerity_1949` "decline" (fx_reserves -8), with the box at 4 (DollarConversionTests.AQueuedChoiceThatSpendsReserves...).
///   After: the reserves refusal line under Convert. Before: no refusal line, as the panel read the state without the queue.
/// - `dollars-refused-pressed-{lang}`: the same panel after the player presses Convert. After: nothing more queued, the refusal
///   still shown. Before: the conversion shows as queued, which End turn then refuses.
/// - `reinforce-after-reassign-{lang}`: Golani's unit card, one strength short, in Tel Aviv, whose only idle civilian a queued
///   ReassignCraftsmen takes (UnitOrdersTests.AQueuedCraftsmanReassignmentHidesReinforce...). After: no Reinforce button, the
///   sim's refusal ("0 civilians without work") among the card's lines. Before: Reinforce is offered.
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
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>A golden-seed historical game, shaped by <paramref name="shape"/> before the root draws it.</summary>
    IEnumerator Boot(bool hebrew, Action<GameSession> shape)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 24);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
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

    /// <summary>Puts any open event card away, as its Hide button does, so the screen under it can be seen.</summary>
    IEnumerator PutCardsAway()
    {
        var hide = ui.Q<Button>("event-hide");
        if (hide != null && hide.resolvedStyle.display != DisplayStyle.None && hide.enabledInHierarchy) UiTestKit.Press(hide);
        root.Card?.Collapse(takeFocus: false);
        yield return Frames(3);
    }

    static void IntoView(VisualElement? e)
    {
        if (e == null) return;
        e.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(e);
    }

    void Shot(string name)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt!.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"SHOT {name} -> {shotDir}");
    }

    IEnumerator DollarPanel(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew, g =>
        {
            g.State.Rung = EconomySystem.WarRung;
            g.State.FxReserves = 10;
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
        Debug.Log($"SHOT dollars-refused-{lang}: refusal \"{ui.Q<Label>("dollars-refusal")?.text}\"");
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
        });
        yield return PutCardsAway();
        Debug.Log($"SHOT reinforce: queue reassign -> {Bridge.Queue(new ReassignCraftsmen(city.Id, 1)) ?? "queued"}");
        yield return Frames(3);
        root.FocusUnit("golani");
        yield return Frames(8);
        var button = ui.Q<Button>("info-reinforce");
        Debug.Log($"SHOT reinforce-after-reassign-{lang}: Reinforce {(button == null ? "missing" : button.style.display.value.ToString())}");
        Shot($"reinforce-after-reassign-{lang}");
    }

    [UnityTest] public IEnumerator DollarsEnglish() { yield return DollarPanel(false); }
    [UnityTest] public IEnumerator DollarsHebrew() { yield return DollarPanel(true); }
    [UnityTest] public IEnumerator ReinforceEnglish() { yield return ReinforceCard(false); }
    [UnityTest] public IEnumerator ReinforceHebrew() { yield return ReinforceCard(true); }
}
