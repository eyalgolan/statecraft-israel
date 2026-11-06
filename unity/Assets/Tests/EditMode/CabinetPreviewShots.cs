#nullable enable
using System;
using System.Collections;
using System.IO;
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

/// <summary>Writes the before and after stills for #2745 (refs #2743 item 1), in English and Hebrew, to
/// `.superpowers/shots-2745/&lt;tag&gt;/` (local, not committed): Assets/Map.unity, its UI drawn through a copy of its
/// PanelSettings into a transparent 1280x720 texture and laid over the main camera's own 1280x720 frame, as CivMovementShots
/// does, so the map shows under the UI. Each state is the one the PR's own tests build
/// (CabinetScreenTests.In1950: the historical game on the golden seed, played by HistoricalAI to 1 February 1950), which the
/// root goes on with (GameRoot.Begin keep:); the Cabinet opens from the top bar's Cabinet button, and the office is picked with
/// CabinetPresenter.Select, as the tests do. The candidate row is scrolled into view.
///
/// Shots (`{lang}` is `en` or `he`):
/// - `maklef-finance-after-cos-queued-{lang}`: Appoint(chief_of_staff, maklef) queued, Minister of Finance selected
///   (AQueuedChiefOfStaffAppointmentDisablesThatPersonsMinisterButton). After: Maklef's button off, with End turn's reason in
///   the row. Before: the button on, no reason.
/// - `maklef-cos-replacing-dayan-{lang}`: Dayan queued as Chief of Staff and Maklef for Finance, Chief of Staff selected
///   (ReplacingAChiefOfStaffOrderCannotNewlyBreakALaterAppointment). After: Maklef's button off, with the reason the later
///   Finance order would be refused. Before: the button on, no reason.
///
/// Run (Unity closed on the project; the same file, unchanged, on both trees, at the same resolution):
///   AFTER, with the PR head checked out:
///     SHOTS_TAG=after  &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter CabinetPreviewShots -testResults /tmp/shots-2745-after.xml
///   BEFORE, with this file and its .meta copied onto main:
///     SHOTS_TAG=before &lt;Unity&gt; -batchmode -projectPath unity -runTests -testPlatform EditMode -testFilter CabinetPreviewShots -testResults /tmp/shots-2745-before.xml
/// Uses only APIs that exist on main as well (checked against origin/main when written). Explicit, so CI never runs it.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class CabinetPreviewShots
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
        shotDir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "shots-2745", string.IsNullOrEmpty(tag) ? "untagged" : tag);
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

    /// <summary>CabinetScreenTests.In1950's game, drawn by the root.</summary>
    IEnumerator Boot(bool hebrew)
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
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        root.Begin(MapFocusTests.Inputs, 0, keep: new SimBridge(g, Text(hebrew)));
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        // Any waiting event card is put away, as its Hide button does, so the Cabinet can open over the map.
        var hide = ui.Q<Button>("event-hide");
        if (hide != null && hide.resolvedStyle.display != DisplayStyle.None && hide.enabledInHierarchy) UiTestKit.Press(hide);
        root.Card?.Collapse(takeFocus: false);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));   // as CivMovementShots.Boot
        yield return Frames(3);
    }

    CabinetPresenter CabinetScreen()
    {
        var field = typeof(GameRoot).GetField("cabinet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (CabinetPresenter)field!.GetValue(root)!;
    }

    IEnumerator OpenAt(string office, string person)
    {
        UiTestKit.Press(ui.Q<Button>("nav-cabinet"));
        yield return Frames(5);
        CabinetScreen().Select(office);
        yield return Frames(5);
        var row = ui.Q("cand-" + person);
        if (row != null) ui.Q<ScrollView>("cabinet-candidates")?.ScrollTo(row);
        yield return Frames(5);
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
    void Shot(string name, string person)
    {
        var cam = Camera.main;
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
        var button = ui.Q<Button>("appoint-" + person);
        Debug.Log($"SHOT {name} -> {shotDir}: appoint-{person} enabled={button?.enabledSelf}, reason \"{ui.Q<Label>("cand-" + person + "-reason")?.text}\"");
    }

    IEnumerator FinanceAfterChiefOfStaff(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew);
        Debug.Log($"SHOT queue chief_of_staff maklef -> {Bridge.Queue(new Appoint("chief_of_staff", "maklef")) ?? "queued"}");
        yield return OpenAt("finance_minister", "maklef");
        Shot($"maklef-finance-after-cos-queued-{lang}", "maklef");
    }

    IEnumerator ChiefOfStaffReplacingDayan(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew);
        // The 1950 game pays for one appointment (1.5 Attention left after it, 2.5 for the next), and this state needs two
        // queued, as CabinetScreenTests.ReplacingAChiefOfStaffOrderCannotNewlyBreakALaterAppointment gives itself.
        Bridge.TurnStart.Attention += 5;
        Debug.Log($"SHOT queue chief_of_staff dayan -> {Bridge.Queue(new Appoint("chief_of_staff", "dayan")) ?? "queued"}");
        Debug.Log($"SHOT queue finance_minister maklef -> {Bridge.Queue(new Appoint("finance_minister", "maklef")) ?? "queued"}");
        yield return OpenAt("chief_of_staff", "maklef");
        Shot($"maklef-cos-replacing-dayan-{lang}", "maklef");
    }

    [UnityTest] public IEnumerator FinanceEnglish() { yield return FinanceAfterChiefOfStaff(false); }
    [UnityTest] public IEnumerator FinanceHebrew() { yield return FinanceAfterChiefOfStaff(true); }
    [UnityTest] public IEnumerator ChiefOfStaffEnglish() { yield return ChiefOfStaffReplacingDayan(false); }
    [UnityTest] public IEnumerator ChiefOfStaffHebrew() { yield return ChiefOfStaffReplacingDayan(true); }
}
