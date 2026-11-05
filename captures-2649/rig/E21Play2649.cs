#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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

/// <summary>Throwaway (never committed): #2649 stills IN PLAY (copy of the e21-cap-2613 rig). Opens Map.unity with GameBoot
/// off, enters Play mode (-nostart), builds the map, GameRoot.Begin (15 May 1948), lets the craft layer place the Dakotas,
/// pins SpitfireLoop.Clock and shoots 15 May t=0; then sets the game state's date to 15 July 1948 and raises the bridge's
/// Changed (GameRoot redraws its layers, SeaAirCollection.ShowAt with the new date) and shoots the PR's shot list by loop
/// time t (the lead's). TelAvivAirDefence is reached by reflection so the rig compiles on origin/main too. The head run
/// writes its July subject cameras to cams.txt; the main run reads them from raw-head so both frame the same spot.</summary>
[Explicit("throwaway stills")]
public class E21Play2649
{
    static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "e21-play-2649"));
    const string HeadCams = "/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-cap-2649/raw-head/cams.txt";
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
        foreach (var r in rs) { if (r == null || !r.enabled) continue; if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; } }
        return b;
    }

    static readonly string[] NotPlane = { "Bomb", "Column", "Flash", "Blast", "Smoke", "Dust", "Shadow", "Puff" };
    static IEnumerable<Renderer> PlaneRenderers(DakotaLoop l)
    {
        var flag = l.Flag != null ? l.Flag.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
        return l.GetComponentsInChildren<Renderer>(false).Where(r => !flag.Contains(r) && !NotPlane.Any(n => r.name.Contains(n)));
    }

    // TelAvivAirDefence by reflection (absent on origin/main).
    static readonly Type? DefT = typeof(SeaAirCollection).Assembly.GetType("Statecraft.Game.Map.Collection.TelAvivAirDefence");
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Component? Defence() => DefT == null ? null : Object.FindObjectsByType(DefT, FindObjectsSortMode.None).FirstOrDefault() as Component;
    static object? Prop(Component d, string n) => DefT!.GetProperty(n, Any)?.GetValue(d);
    static Renderer[] Pool(Component d, string n) => Prop(d, n) as Renderer[] ?? Array.Empty<Renderer>();
    static Transform[] Fighters(Component? d) => d == null ? Array.Empty<Transform>() : Prop(d, "FighterPlanes") as Transform[] ?? Array.Empty<Transform>();
    static Vector3 CrashAt(Component d) => (Vector3)DefT!.GetField("crashAt", Any)!.GetValue(d)!;

    IEnumerator Run(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        Assert.IsTrue(Application.isPlaying, "in Play mode");
        Assert.IsTrue(StartScreenPresenter.Skipped(), "-nostart");
        Directory.CreateDirectory(Dir);
        var tmp = Path.Combine(Path.GetTempPath(), "e21-play-2649-" + Guid.NewGuid().ToString("N"));
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
        DakotaLoop[] loops = Array.Empty<DakotaLoop>();
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline)
        {
            loops = Object.FindObjectsByType<DakotaLoop>(FindObjectsSortMode.None);
            if (loops.Length > 0) break;
            yield return null; Pump();
        }
        Assert.Greater(loops.Length, 0, "the Dakotas appear in play on 15 May");
        loops = loops.OrderBy(l => l.Lag).ToArray();
        savedClock = SpitfireLoop.Clock;
        clk = Base; SpitfireLoop.Clock = () => clk;
        yield return Frames(3);
        var labels = root.Labels!;
        var tlv = content.SettlementHexes.First(s => s.Id == "tel_aviv");
        var hex = content.HexAt(tlv.Q, tlv.R)!;
        var city = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        city.y = map.Terrain.SampleHeight(city) + map.Terrain.transform.position.y;
        Log($"{lang} defenceType={(DefT != null ? "present" : "absent")} telaviv={V(city)}");

        IEnumerator At(float t)
        {
            clk = Base + t; yield return Frames(3);
            UnitFlagFacing.FaceAll(Camera.main.transform.position);
            labels.Refresh(force: true); yield return null; Pump();
            labels.Refresh(force: true);
        }
        void Aim(Vector3 at, float distance) { rig.LookAt = new Vector3(at.x, 0f, at.z); rig.Distance = distance; rig.Yaw = 0f; rig.Apply(); cc.Settle(); }
        void AimOn(Vector3 p, float distance)
        {
            Aim(p, distance);
            var f = Camera.main.transform.forward; float k = (p.y - city.y) / -f.y;
            Aim(p + f * k, distance);
        }
        string TopDate()
        {
            var bar = ui.Q("top-bar"); if (bar == null) return "?";
            return string.Join(" / ", bar.Query<Label>().ToList().Select(l => l.text).Where(s => s != null && (s.Contains("1948") || s.Contains("תש"))).Distinct());
        }

        string month = "may";
        void Shot(string name, string what)
        {
            var cam = Camera.main;
            var frame = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var before = cam.targetTexture; cam.targetTexture = frame; cam.aspect = (float)W / H;
            var parts = new List<string>();
            var live = Object.FindObjectsByType<DakotaLoop>(FindObjectsSortMode.None).OrderBy(l => l.Lag).ToArray();
            foreach (var l in live)
            {
                var pb = BoundsOf(PlaneRenderers(l));
                var vp = pb != null ? cam.WorldToViewportPoint(pb.Value.center) : new Vector3(-9, -9, 0);
                parts.Add($"DAK {l.name} u={F(clk - Base - l.Lag)} vp=({vp.x:F2},{vp.y:F2}) plane=[{(pb != null ? ScreenRect(cam, pb.Value) : "hidden")}]");
            }
            var d = Defence();
            if (d != null)
            {
                foreach (var fp in Fighters(d))
                {
                    var b = BoundsOf(fp.GetComponentsInChildren<Renderer>());
                    var vp = cam.WorldToViewportPoint(fp.position);
                    parts.Add($"FTR {fp.name} vp=({vp.x:F2},{vp.y:F2}) box=[{(b != null ? ScreenRect(cam, b.Value) : "hidden")}]");
                }
                foreach (var pool in new[] { "TracerRenderers", "HitRenderers", "FlakRenderers", "TrailRenderers" })
                {
                    var on = Pool(d, pool).Where(r => r != null && r.enabled).ToList();
                    var b = BoundsOf(on);
                    parts.Add($"POOL {pool} on={on.Count} box=[{(b != null ? ScreenRect(cam, b.Value) : "none")}]");
                }
                var cr = Prop(d, "CrashRenderer") as Renderer;
                parts.Add($"CRASH on={(cr != null && cr.enabled)} box=[{(cr != null && cr.enabled ? ScreenRect(cam, cr.bounds) : "none")}] wingShown={Prop(d, "WingShown")} placedAt={F(((float?)Prop(d, "PlacedAt") ?? 0f) - Base)}");
                // gun-site / burst flashes: any enabled renderer with Flash in its name under the defence
                var flashes = d.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && r.name.Contains("Flash")).ToList();
                parts.Add($"FLASH on={flashes.Count} names={string.Join(",", flashes.Select(r => r.name).Distinct())}");
            }
            else parts.Add("DEFENCE none");
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
            Log($"{lang} SHOT {name} what={what} t={F(clk - Base)} month={month} date={state.Date:yyyy-MM-dd} top=[{TopDate()}] d={rig.Distance:F0} lookAt={V(rig.LookAt)} || {string.Join(" || ", parts)} || shown=[{string.Join(" ; ", shown)}]");
        }

        // ---- 15 May 1948, t = 0: unchanged check (no fighters, no flak).
        var lead = loops[0];
        foreach (var md in new[] { 45000f, 28000f })
        {
            yield return At(0f); AimOn(BoundsOf(PlaneRenderers(lead))!.Value.center, md); yield return At(0f);
            Shot($"may-air-{md / 1000:F0}km-t000.0", "15 May, camera on the lead Dakota");
        }
        Aim(city, 31000f); yield return At(0f);
        Shot("may-tlv-31km-t000.0", "15 May, Tel Aviv 31 km");

        // ---- 15 July 1948: set the state's date and redraw as a turn change does.
        state.Date = new DateTime(1948, 7, 15);
        month = "jul";
        var changed = typeof(SimBridge).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(root.Bridge) as Action;
        Assert.IsNotNull(changed, "bridge Changed handlers");
        changed!.Invoke();
        deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (DefT != null && Defence() != null && Object.FindObjectsByType<DakotaLoop>(FindObjectsSortMode.None).Length == 2) break;
            if (DefT == null && Time.realtimeSinceStartup > deadline - 75f) break;
            yield return null; Pump();
        }
        yield return Frames(10);
        loops = Object.FindObjectsByType<DakotaLoop>(FindObjectsSortMode.None).OrderBy(l => l.Lag).ToArray();
        var def = Defence();
        Log($"{lang} JULY date={state.Date:yyyy-MM-dd} top=[{TopDate()}] dakotas={loops.Length} {string.Join(" ", loops.Select(l => $"{l.name}:lag={F(l.Lag)}"))} defence={(def != null ? "yes" : "no")} still={(def != null ? Prop(def, "Still") : "-")} fighters={Fighters(def).Length}");

        var cams = new Dictionary<string, (float x, float z, float d)>();
        bool computing = def != null && loops.Length == 2;
        if (!computing && File.Exists(HeadCams))
            foreach (var line in File.ReadAllLines(HeadCams))
            {
                var p = line.Split(' ');
                if (p.Length == 4) cams[p[0]] = (float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture));
            }
        var written = new List<string>();

        IEnumerator Subject(string name, float t, string what, string kind, float dist)
        {
            yield return At(t);
            if (computing)
            {
                var ld = loops[0]; var wg = loops[1]; var f = Fighters(def);
                Vector3 p;
                var lb = BoundsOf(PlaneRenderers(ld))!.Value.center;
                var wb = BoundsOf(PlaneRenderers(wg));
                switch (kind)
                {
                    case "f0": p = 0.5f * (lb + f[0].position); break;
                    case "f1": p = 0.5f * (lb + f[1].position); break;
                    case "crash": p = CrashAt(def!); break;
                    default: p = wb?.center ?? CrashAt(def!); break;
                }
                AimOn(p, dist);
                written.Add($"{name} {rig.LookAt.x.ToString("R", CultureInfo.InvariantCulture)} {rig.LookAt.z.ToString("R", CultureInfo.InvariantCulture)} {dist.ToString("R", CultureInfo.InvariantCulture)}");
            }
            else if (cams.TryGetValue(name, out var c)) { rig.LookAt = new Vector3(c.x, 0f, c.z); rig.Distance = c.d; rig.Yaw = 0f; rig.Apply(); cc.Settle(); }
            else { Log($"{lang} NOCAM {name}"); Aim(city, dist); }
            yield return At(t);
            Shot(name, what);
        }

        var list = new (float t, string kind, string what)[]
        {
            (0f, "f0", "item 1: fighter 1 firing on the lead over Tel Aviv, first hit"),
            (2f, "f0", "item 2: fighter 1 at its pass, hits on the lead"),
            (28.9f, "wing", "item 3: first flak burst at the wing (t=28.9)"),
            (29.2f, "wing", "item 3: first flak burst +0.3 s"),
            (31.2f, "wing", "item 3: second burst +0.3 s"),
            (33.2f, "wing", "item 3: third burst +0.3 s"),
            (35.2f, "wing", "item 3: fourth burst +0.3 s"),
            (37.2f, "wing", "item 3: fifth burst +0.3 s"),
            (38.4f, "wing", "item 3: last burst ON the wing (t=38.4)"),
            (38.7f, "wing", "item 3: last burst on the wing +0.3 s"),
            (39.2f, "wing", "item 3 extra (head only): last burst on the wing +0.8 s"),
            (45f, "wing", "item 4: the wing falling, trailing smoke"),
            (54f, "wing", "item 4: the wing falling, trailing smoke"),
            (57f, "crash", "item 5: blast on the ground, the wing gone"),
            (60f, "f1", "item 6: fighter 2 on the northern pass firing on the lead"),
            (107f, "wing", "item 7: the fresh wing back on the loop over the sea"),
        };
        bool full = !hebrew;
        var Close = new[] { 0f, 2f, 33.2f, 38.4f, 38.7f, 45f, 57f, 60f };
        var Closer = new[] { 0f, 2f, 38.4f, 38.7f, 39.2f, 45f, 60f };
        foreach (var (t, kind, what) in list)
        {
            if (!full && t != 0f) continue;
            yield return Subject($"jul-air-45km-t{t:000.0}", t, what, kind, 45000f);
            if (Close.Contains(t)) yield return Subject($"jul-air-28km-t{t:000.0}", t, what, kind, 28000f);
            if (computing && Closer.Contains(t)) yield return Subject($"jul-air-16km-t{t:000.0}", t, what + " (16 km, head only)", kind, 16000f);
            Aim(city, 31000f); yield return At(t);
            Shot($"jul-tlv-31km-t{t:000.0}", what + " | Tel Aviv 31 km");
        }
        if (computing && !hebrew) File.WriteAllLines(Path.Combine(Dir, "cams.txt"), written);
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
    }

    static void Prepare()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        foreach (var b in Object.FindObjectsByType<GameBoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) b.enabled = false;
    }

    [UnityTest, Timeout(1200000)] public IEnumerator English() { Prepare(); yield return new EnterPlayMode(); yield return Run(false); }
    [UnityTest, Timeout(1200000)] public IEnumerator Hebrew() { Prepare(); yield return new EnterPlayMode(); yield return Run(true); }
}
