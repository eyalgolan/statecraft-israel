#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Throwaway (never committed): #2613 Dakota smoke columns + keep-clear stills IN PLAY (pattern of the #2490 v3 rig
/// and the e21-cap-2691 rig). Opens Map.unity with GameBoot off, enters Play mode (-nostart), builds the map, calls
/// GameRoot.Begin (15 May 1948), lets GameRoot's craft layer place the Dakotas, then pins the map's animation clock
/// (SpitfireLoop.Clock) to fixed times so main and head frames match exactly. Camera aspect pinned to the shot's 16:9.</summary>
[Explicit("throwaway stills")]
public class E21Play2613
{
    static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "e21-play-2613"));
    static readonly Vector3 OpeningLookAt = new Vector3(56929.60f, 0f, 289262.20f);
    const int W = 1920, H = 1080;
    const float P = 120f;      // SeaAirCollection.DakotaLoopSeconds
    const float Base = 4 * P;  // four loops in: every column stands
    static float clk;
    Func<float>? savedClock;

    [UnityTearDown] public IEnumerator T()
    {
        if (savedClock != null) SpitfireLoop.Clock = savedClock;
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void Log(string s) => Debug.Log("E21PLAY " + s);
    static string F(float v) => v.ToString("F1", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => $"({F(v.x)},{F(v.y)},{F(v.z)})";
    static bool Shown(Label l) => l.parent != null && l.parent.style.visibility.value == Visibility.Visible;

    static RenderTexture? pumpRt;
    static void Pump()
    {
        var cam = Camera.main; if (cam == null) return;
        if (pumpRt == null) pumpRt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.aspect = (float)W / H;
        var before = cam.targetTexture; cam.targetTexture = pumpRt; cam.Render(); cam.targetTexture = before;
    }
    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) { yield return null; Pump(); } }

    static Color[] Read(RenderTexture from)
    {
        var prev = RenderTexture.active; RenderTexture.active = from;
        var tex = new Texture2D(from.width, from.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, from.width, from.height), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels(); Object.DestroyImmediate(tex); return px;
    }

    /// <summary>Screen rect (pixels, y down) of a world box; "off" when a corner is behind the camera.</summary>
    static string ScreenRect(Camera cam, Bounds b)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
            var v = cam.WorldToViewportPoint(c);
            if (v.z <= 0f) return "behind";
            float px = v.x * W, py = (1f - v.y) * H;
            x0 = Mathf.Min(x0, px); x1 = Mathf.Max(x1, px); y0 = Mathf.Min(y0, py); y1 = Mathf.Max(y1, py);
        }
        return $"{F(x0)},{F(y0)},{F(x1)},{F(y1)}";
    }

    static Bounds? BoundsOf(IEnumerable<Renderer> rs)
    {
        Bounds? b = null;
        foreach (var r in rs) { if (!r.enabled) continue; if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; } }
        return b;
    }

    static readonly string[] NotPlane = { "Bomb", "Column", "Flash", "Blast", "Smoke", "Dust", "Shadow", "Puff" };
    static IEnumerable<Renderer> PlaneRenderers(DakotaLoop l)
    {
        var flag = l.Flag != null ? l.Flag.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
        return l.GetComponentsInChildren<Renderer>(false).Where(r => !flag.Contains(r) && !NotPlane.Any(n => r.name.Contains(n)));
    }

    IEnumerator Run(bool hebrew, bool full)
    {
        var lang = hebrew ? "he" : "en";
        Assert.IsTrue(Application.isPlaying, "in Play mode");
        Assert.IsTrue(StartScreenPresenter.Skipped(), "-nostart");
        Directory.CreateDirectory(Dir);
        var tmp = Path.Combine(Path.GetTempPath(), "e21-play-2613-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tmp, "save")); Directory.CreateDirectory(Path.Combine(tmp, "settings"));
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        Camera.main.aspect = (float)W / H;
        var map = Object.FindFirstObjectByType<MapScene>();
        var inputs = MapFocusTests.Inputs;
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(Path.Combine(tmp, "save"));
        root.Settings = new SettingsStore(Path.Combine(tmp, "settings"));
        root.Language = hebrew ? UiLanguage.Hebrew : UiLanguage.English;
        SeaAirCollection.Request = TalModelsShots.Answer(Array.Empty<string>());
        root.Begin(inputs);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 300 && !(ui.Q("top-bar").worldBound.height > 0); i++) { yield return null; Pump(); }
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(5);
        if (root.Advisor is { IsOpen: true } && ui.Q<Button>("advisor-close") is Button x) { UiTestKit.Press(x); Log($"{lang} advisor closed via X"); }
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        Assert.AreEqual(new DateTime(1948, 5, 15), state.Date.Date, "date");
        var rig = Camera.main.GetComponent<CameraRig>(); var cc = rig.GetComponent<CameraController>();
        Log($"{lang} begun boot lookAt={V(rig.LookAt)} d={rig.Distance:F0} yaw={rig.Yaw:F1} date={state.Date:yyyy-MM-dd} turn={state.Turn}");
        DakotaLoop[] loops = Array.Empty<DakotaLoop>();
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline)
        {
            loops = Object.FindObjectsByType<DakotaLoop>(FindObjectsSortMode.None);
            if (loops.Length > 0) break;
            yield return null; Pump();
        }
        Assert.Greater(loops.Length, 0, "the Dakotas appear in play");
        loops = loops.OrderBy(l => l.Lag).ToArray();
        savedClock = SpitfireLoop.Clock;
        clk = Base; SpitfireLoop.Clock = () => clk;
        yield return Frames(3);
        var layer = map.Terrain.transform.Find(SeaAirCollection.Name).GetComponent<SeaAirCollection>();
        var keepOfF = typeof(SeaAirCollection).GetField("keepOf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var labels = root.Labels!;
        var tlv = content.SettlementHexes.First(s => s.Id == "tel_aviv");
        var hex = content.HexAt(tlv.Q, tlv.R)!;
        var city = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        city.y = map.Terrain.SampleHeight(city) + map.Terrain.transform.position.y;
        var lead = loops[0]; var run = lead.Bombs!;
        Log($"{lang} consts columns={SeaAirCollection.DakotaColumns} puffs={SeaAirCollection.DakotaColumnPuffs} life={SeaAirCollection.DakotaColumnLifeS} M={SeaAirCollection.DakotaColumnM} foot={SeaAirCollection.DakotaColumnFootM} top={SeaAirCollection.DakotaColumnTopM} loop={SeaAirCollection.DakotaLoopSeconds} fall={SeaAirCollection.DakotaBombFallS}");
        Log($"{lang} dakotas={loops.Length} {string.Join(" ", loops.Select(l => $"{l.name}:lag={F(l.Lag)}:sites={l.Bombs?.ColumnSites.Count}"))} telaviv={V(city)} planeRenderers={string.Join(",", PlaneRenderers(lead).Select(r => r.name))} flag={(lead.Flag != null ? lead.Flag.name : "none")}");
        var sites = new List<(DakotaLoop l, int c, Vector3 at, double from)>();
        foreach (var l in loops) if (l.Bombs != null) for (int c = 0; c < l.Bombs.ColumnSites.Count; c++) sites.Add((l, c, l.Bombs.ColumnSites[c], l.Bombs.ColumnFrom(c)));
        Log($"{lang} SITES {string.Join(" | ", sites.Select(s => $"{s.l.name}#{s.c} at={V(s.at)} from={s.from:F2}+lag{F(s.l.Lag)}"))}");

        IEnumerator At(float t)
        {
            clk = t; yield return Frames(3);
            UnitFlagFacing.FaceAll(Camera.main.transform.position);
            labels.Refresh(force: true); yield return null; Pump();
            labels.Refresh(force: true);
        }
        void Aim(Vector3 at, float distance) { rig.LookAt = new Vector3(at.x, 0f, at.z); rig.Distance = distance; rig.Yaw = 0f; rig.Apply(); cc.Settle(); }
        // Camera on the aircraft: look at the ground point behind it along the view, so it sits mid-frame.
        void AimOn(Vector3 p, float distance)
        {
            Aim(p, distance);
            var f = Camera.main.transform.forward; float k = (p.y - city.y) / -f.y;
            Aim(p + f * k, distance);
        }

        void Shot(string name, string what)
        {
            var cam = Camera.main;
            var frame = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var before = cam.targetTexture; cam.targetTexture = frame; cam.aspect = (float)W / H;
            // Overlays, projected with the frame's camera.
            var keepOf = (System.Collections.IDictionary)keepOfF.GetValue(layer)!;
            var all = layer.KeepClear;
            var parts = new List<string>();
            foreach (var l in loops)
            {
                var key = keepOf.Contains(l.gameObject) ? l.gameObject : null;
                string keep = key != null ? ScreenRect(cam, ((Bounds[])keepOf[key]!)[0]) : "none";
                string ksize = key != null ? V(((Bounds[])keepOf[key]!)[0].size) : "-";
                var pb = BoundsOf(PlaneRenderers(l)); var fb = l.Flag != null ? BoundsOf(l.Flag.GetComponentsInChildren<Renderer>()) : null;
                var vp = cam.WorldToViewportPoint(l.transform.position);
                parts.Add($"{l.name} u={F(clk - l.Lag)} pos={V(l.transform.position)} vp=({vp.x:F2},{vp.y:F2}) keep=[{keep}] keepSize={ksize} plane=[{(pb != null ? ScreenRect(cam, pb.Value) : "none")}] flag=[{(fb != null ? ScreenRect(cam, fb.Value) : "none")}]");
            }
            var shown = new List<string>();
            for (int i = 0; i < labels.Labels.Count; i++) if (Shown(labels.Labels[i])) shown.Add($"S{i}:{labels.Labels[i].text}");
            for (int i = 0; i < labels.LocalityLabels.Count; i++) if (Shown(labels.LocalityLabels[i])) shown.Add($"L{i}:{labels.LocalityLabels[i].text}");
            for (int i = 0; i < labels.LocationLabels.Count; i++) if (Shown(labels.LocationLabels[i])) shown.Add($"M{i}:{labels.LocationLabels[i].text}");
            cam.Render(); cam.targetTexture = before;
            var mapPx = Read(frame); var over = Read(rt);
            frame.Release(); Object.DestroyImmediate(frame);
            var outPx = new Color[mapPx.Length];
            for (int i = 0; i < mapPx.Length; i++)
            {
                var u = over[i]; var m = mapPx[i];
                outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f);
            }
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.SetPixels(outPx); tex.Apply();
            File.WriteAllBytes(Path.Combine(Dir, $"{name}-{lang}.png"), tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            Log($"{lang} SHOT {name} what={what} clock={F(clk)} d={rig.Distance:F0} lookAt={V(rig.LookAt)} keepClear={all.Count} || {string.Join(" || ", parts)} || shown=[{string.Join(" ; ", shown)}]");
        }

        void Columns(string name)
        {
            foreach (var s in sites)
            {
                int NP = SeaAirCollection.DakotaColumnPuffs;
                var on = s.l.Bombs!.Columns.Skip(s.c * NP).Take(NP).Where(r => r.enabled).OrderBy(r => r.transform.position.y).ToList();
                if (on.Count == 0) { Log($"{lang} COLS {name} {s.l.name}#{s.c} none"); continue; }
                string Puff(Renderer r) { var c = DakotaBombRun.Colour(r); return $"{F(r.transform.position.y - s.at.y)}m/w{F(r.transform.localScale.x)}/a{c.a:F2}/r{c.r:F2}"; }
                Log($"{lang} COLS {name} {s.l.name}#{s.c} n={on.Count} age={(clk - s.l.Lag - s.from):F1} shader={on[0].sharedMaterial.shader.name} puffs(low..high h/w/alpha/tint.r)={string.Join(" ", on.Select(Puff))}");
            }
        }

        float U(DakotaLoop l, float u) => Base + u + l.Lag;   // clock for loop time u of plane l
        // 1. Wide 31 km on Tel Aviv + the opening view, the Dakotas ~18 km north (issue #2604's frame, u=63 s).
        yield return At(U(lead, 63f));
        Aim(city, 31000f); yield return At(clk); Shot("wide-31km-u063", "Tel Aviv 31 km, Dakotas off the top edge");
        rig.LookAt = OpeningLookAt; rig.Distance = 31000f; rig.Yaw = 0f; rig.Apply(); cc.Settle(); yield return At(clk);
        Shot("opening-31km-u063", "opening view 31 km, Dakotas off the top edge"); Columns("u063");
        // 2. Close 20 km on Tel Aviv, columns standing, timed over one column life.
        var times = full ? new[] { 63f, 67f, 71f, 75f, 79f } : new[] { 63f };
        foreach (var u in times)
        {
            Aim(city, 20000f); yield return At(U(lead, u));
            Shot($"close-20km-u{u:000}", $"Tel Aviv 20 km, columns standing, lead u={u}"); Columns($"u{u:000}");
        }
        // 3. One column at 8 km (foot on its impact, top tint), same times.
        var s0 = sites.OrderBy(s => (s.at - city).sqrMagnitude).First();
        if (full)
            foreach (var u in times)
            {
                Aim(s0.at + new Vector3(0f, 0f, 1500f), 9000f); yield return At(U(lead, u));
                Shot($"column-9km-u{u:000}", $"nearest column to Tel Aviv ({s0.l.name}#{s0.c}) 9 km, lead u={u}");
            }
        // 4. Growth in the first loop: the first column's first life.
        if (full)
        {
            var first = sites.OrderBy(s => s.from + s.l.Lag).First();
            foreach (var age in new[] { 2f, 6f, 10f, 15f })
            {
                Aim(city, 20000f); yield return At((float)first.from + first.l.Lag + age);
                Shot($"growth-20km-age{age:00}", $"first loop, {age} s after {first.l.name}#{first.c}'s first hit"); Columns($"growth{age:00}");
            }
        }
        // 5. Camera on the lead aircraft, mid-run (names around it), 31 and 20 km.
        foreach (var u in full ? new[] { 30f, 63f, 90f, 110f } : new[] { 30f, 90f })
            foreach (var d in new[] { 31000f, 20000f })
            {
                yield return At(U(lead, u)); AimOn(lead.transform.position, d); yield return At(clk);
                Shot($"plane-{d / 1000:F0}km-u{u:000}", $"camera on {lead.name}, lead u={u}");
            }
        // 6. Bomb / blast / fireball on the pass, 12 km fixed camera (unchanged by the PR; check only).
        if (full)
        {
            var drops = run.Drops.ToList();
            var pick = drops[drops.Count / 2];
            float fall = SeaAirCollection.DakotaBombFallS;
            var ground = 0.5f * (pick.From + pick.To);
            Log($"{lang} SEQ drop At={F(pick.At)} from={V(pick.From)} to={V(pick.To)} drops={drops.Count}");
            foreach (var (n, rel) in new[] { ("seq1-midfall", 1.0f), ("seq2-impact", fall + 0.08f), ("seq3-fireball", fall + 1.0f) })
            {
                Aim(new Vector3(ground.x, pick.To.y, ground.z), 12000f); yield return At(Base + pick.At + lead.Lag + rel);
                Shot($"{n}-12km", $"lead drop At={F(pick.At)} release+{rel:F2}s");
            }
        }
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
    }

    static void Prepare()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        foreach (var b in Object.FindObjectsByType<GameBoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) b.enabled = false;
    }

    [UnityTest, Timeout(1200000)] public IEnumerator English() { Prepare(); yield return new EnterPlayMode(); yield return Run(false, true); }
    [UnityTest, Timeout(1200000)] public IEnumerator Hebrew() { Prepare(); yield return new EnterPlayMode(); yield return Run(true, false); }
}
