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
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Throwaway (never committed): #2751 before/after stills of the PARKED 101 Squadron Spitfire on 20 October 1948
/// (rung 1), the same three camera views as e21-cap-spitfire-blender (min 6 km, close 12 km, normal 31 km, aimed on the
/// plane's centre), in EN and HE. Logs the plane / badge / tail screen rects, the drawn tree crowns and town houses that
/// reach the parked span. When fixed.txt exists, also shoots the same distances aimed at that world point.</summary>
[Explicit("throwaway stills")]
public class E21Park2751
{
    static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "e21-park-2751"));
    const string FixedFile = "/private/tmp/claude-501/-Users-eyalgolan/3c0fa202-b087-43f0-838d-8402482cee84/scratchpad/e21-cap-2751/fixed.txt";
    const int W = 1920, H = 1080;
    const float SpanR = 11.23f * UnitShowcase.AircraftScale * 0.5f;

    [UnityTearDown] public IEnumerator T()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static void Log(string s) => Debug.Log("E21PLAY " + s);
    static string F(float v) => v.ToString("F1", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => $"({F(v.x)},{F(v.y)},{F(v.z)})";

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

    static string Rect(Camera cam, IEnumerable<Vector3> pts)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (var c in pts)
        {
            var v = cam.WorldToViewportPoint(c);
            if (v.z <= 0f) return "behind";
            float px = v.x * W, py = (1f - v.y) * H;
            x0 = Mathf.Min(x0, px); x1 = Mathf.Max(x1, px); y0 = Mathf.Min(y0, py); y1 = Mathf.Max(y1, py);
        }
        return $"{F(x0)},{F(y0)},{F(x1)},{F(y1)}";
    }
    static IEnumerable<Vector3> Corners(Bounds b)
    {
        for (int c = 0; c < 8; c++)
            yield return new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
    }
    static Bounds? BoundsOf(IEnumerable<Renderer> rs)
    {
        Bounds? b = null;
        foreach (var r in rs) { if (r == null || !r.enabled) continue; if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; } }
        return b;
    }
    /// <summary>The aircraft's mesh box (badge excluded), world or the aircraft's local space (as UnitsInPlayTests).</summary>
    static Bounds MeshBounds(Transform root, bool local)
    {
        Bounds? all = null;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null || f.GetComponentInParent<UnitBadge>(true) != null || f.name == "Shadow") continue;
            foreach (var c in Corners(f.sharedMesh.bounds))
            {
                var p = f.transform.TransformPoint(c);
                if (local) p = root.InverseTransformPoint(p);
                if (all is Bounds a) { a.Encapsulate(p); all = a; } else all = new Bounds(p, Vector3.zero);
            }
        }
        return all!.Value;
    }

    IEnumerator Run(bool hebrew)
    {
        string lang = hebrew ? "he" : "en";
        Assert.IsTrue(Application.isPlaying, "in Play mode");
        Assert.IsTrue(StartScreenPresenter.Skipped(), "-nostart");
        Directory.CreateDirectory(Dir);
        var tmp = Path.Combine(Path.GetTempPath(), "e21-park-2751-" + Guid.NewGuid().ToString("N"));
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
        root.Begin(inputs);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 300 && !(ui.Q("top-bar").worldBound.height > 0); i++) { yield return null; Pump(); }
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(5);
        if (root.Advisor is { IsOpen: true } && ui.Q<Button>("advisor-close") is Button x) { UiTestKit.Press(x); Log($"{lang} advisor closed via X"); }
        var hud = ui.Q("root") ?? ui;
        Log($"{lang} begun lang-he={hud.ClassListContains("lang-he")} rootClasses=[{string.Join(" ", hud.GetClasses())}] dir={ui.Q("top-bar").resolvedStyle.flexDirection}");
        var state = root.Bridge!.State;
        var rig = Camera.main.GetComponent<CameraRig>(); var cc = rig.GetComponent<CameraController>();
        var labels = root.Labels!;
        var terr = map.Terrain;
        float Ground(Vector3 p) => terr.SampleHeight(p) + terr.transform.position.y;
        void Aim(Vector3 at, float distance) { rig.LookAt = new Vector3(at.x, 0f, at.z); rig.Distance = distance; rig.Yaw = 0f; rig.Apply(); cc.Settle(); }
        float RayK(Vector3 p) => p.y / -(Quaternion.Euler(CameraRig.Pitch, 0f, 0f) * Vector3.forward).y;
        void AimOn(Vector3 p, float distance) { var f = Quaternion.Euler(CameraRig.Pitch, 0f, 0f) * Vector3.forward; Aim(p + f * RayK(p), distance); }
        string TopDate()
        {
            var bar = ui.Q("top-bar"); if (bar == null) return "?";
            return string.Join(" / ", bar.Query<Label>().ToList().Select(l => l.text).Where(s => s != null && s.Contains("194")).Distinct());
        }

        state.Date = new DateTime(1948, 10, 20);
        var changed = typeof(SimBridge).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(root.Bridge) as Action;
        Assert.IsNotNull(changed, "bridge Changed handlers");
        var ulayer0 = Object.FindFirstObjectByType<UnitLayer>();
        typeof(UnitLayer).GetField("still", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ulayer0, true);
        int rung = state.Rung < UnitRoster.FlyingFromRung ? state.Rung : 1;
        state.Rung = rung; changed!.Invoke();
        Transform? Token() => Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(tr => tr.name == "squadron_101" && tr.Find("Aircraft") != null);
        Transform? tok = null;
        float deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline) { tok = Token(); if (tok != null && tok.GetComponentInChildren<ShowcaseFlight>() == null) break; yield return null; Pump(); }
        yield return Frames(10);
        tok = Token();
        Assert.IsNotNull(tok, "squadron_101 token");
        var layer = Object.FindFirstObjectByType<UnitLayer>();
        var placed = layer.Tokens.Where(p => p.Token.Unit.Id == "squadron_101").Select(p => $"{p.Token}@{p.Q},{p.R} spot=({p.Spot.X:F0},{p.Spot.Z:F0})").ToList();
        var plane = tok!.Find("Aircraft");
        var badge = tok.Find("Badge");
        IEnumerable<Renderer> PR() => plane.GetComponentsInChildren<Renderer>(false).Where(r => r.name != "Shadow");
        var wb = MeshBounds(plane, local: false);
        var flat = new Vector2(wb.center.x, wb.center.z);
        float footR = new Vector2(wb.extents.x, wb.extents.z).magnitude;
        Log($"{lang} SQN rung={rung} date={state.Date:yyyy-MM-dd} top=[{TopDate()}] token={tok.name} placed=[{string.Join(";", placed)}] flying={plane.GetComponent<ShowcaseFlight>() != null} planeCentre={V(wb.center)} planeSize={V(wb.size)} footR={F(footR)} spanR={F(SpanR)} rot={V(plane.rotation.eulerAngles)}");
        // Tree holes and drawn crowns in reach of the parked footprint.
        var holes = layer.TreeHoles.Where(h => (h.centre - flat).magnitude < 3000f).Select(h => $"({F(h.centre.x)},{F(h.centre.y)} r={F(h.radius)} d={F((h.centre - flat).magnitude)})");
        Log($"{lang} TREEHOLES near plane: {string.Join(" ", holes)}");
        var veg = map.GetComponentInChildren<VegetationLayer>(true);
        if (veg != null)
        {
            int Reach(IEnumerable<(TreeKind kind, Matrix4x4 m)> ts, float r) => ts.Count(t => { var p = t.m.GetColumn(3); return (new Vector2(p.x, p.z) - flat).magnitude < r + VegetationLayer.CrownRadius(t.kind, t.m); });
            var drawn = veg.Drawable().ToList(); var planted = veg.Planted().ToList();
            Log($"{lang} TREES planted={planted.Count} drawn={drawn.Count} | crowns reaching the plane footprint (r={F(footR)}): planted={Reach(planted, footR)} drawn={Reach(drawn, footR)} | reaching the span disc (r={F(SpanR)}): planted={Reach(planted, SpanR)} drawn={Reach(drawn, SpanR)}");
        }
        else Log($"{lang} TREES no VegetationLayer");
        var towns = map.GetComponentInChildren<TownModelLayer>(true);
        if (towns != null)
        {
            var near = new List<(float d, string what)>();
            foreach (var f in map.KitFootprints)
                foreach (var house in towns.InstancesOf(f.id))
                {
                    var local = Quaternion.Euler(0f, -house.Yaw, 0f) * new Vector3(flat.x - house.C.x, 0f, flat.y - house.C.y);
                    var n = new Vector2(Mathf.Clamp(local.x, -0.5f * house.W, 0.5f * house.W), Mathf.Clamp(local.z, -0.5f * house.D, 0.5f * house.D));
                    near.Add(((new Vector2(local.x, local.z) - n).magnitude, $"{f.id}:{house.Model}@({F(house.C.x)},{F(house.C.y)})"));
                }
            var close = near.OrderBy(t => t.d).Take(5).Select(t => $"{t.what} gap={F(t.d)}");
            Log($"{lang} HOUSES examined={near.Count} withinSpan={near.Count(t => t.d < SpanR)} withinFoot={near.Count(t => t.d < footR)} nearest: {string.Join(" ; ", close)}");
        }
        var lb = MeshBounds(plane, local: true);
        lb.max = new Vector3(lb.max.x, lb.max.y, Mathf.Lerp(lb.min.z, lb.max.z, 0.4f));
        var tail = Corners(lb).Select(plane.TransformPoint).ToList();

        void Shot(string name, string what)
        {
            var cam = Camera.main;
            var frame = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var before = cam.targetTexture; cam.targetTexture = frame; cam.aspect = (float)W / H;
            cam.Render(); cam.targetTexture = before;   // FaceBadges places the badge for this camera
            var sb = BoundsOf(PR());
            var rect = sb != null ? Rect(cam, Corners(sb.Value)) : "none";
            float depth = sb != null ? Vector3.Dot(sb.Value.center - cam.transform.position, cam.transform.forward) : -1f;
            float half = badge.lossyScale.x * 0.5f;
            var bRect = Rect(cam, new[] { badge.position - cam.transform.right * half - cam.transform.up * half, badge.position - cam.transform.right * half + cam.transform.up * half,
                badge.position + cam.transform.right * half - cam.transform.up * half, badge.position + cam.transform.right * half + cam.transform.up * half });
            var tRect = Rect(cam, tail);
            var fp = new List<Vector3>(); for (int k = 0; k < 32; k++) { float a = k * Mathf.PI * 2f / 32f; var q = new Vector3(flat.x + footR * Mathf.Cos(a), 0f, flat.y + footR * Mathf.Sin(a)); q.y = Ground(q); fp.Add(q); }
            var fRect = Rect(cam, fp);
            var sp = new List<Vector3>(); for (int k = 0; k < 32; k++) { float a = k * Mathf.PI * 2f / 32f; var q = new Vector3(flat.x + SpanR * Mathf.Cos(a), 0f, flat.y + SpanR * Mathf.Sin(a)); q.y = Ground(q); sp.Add(q); }
            var sRect = Rect(cam, sp);
            var mapPx = Read(frame); var over = Read(rt);
            frame.Release(); Object.DestroyImmediate(frame);
            var outPx = new Color[mapPx.Length];
            for (int i = 0; i < mapPx.Length; i++) { var u = over[i]; var m = mapPx[i]; outPx[i] = new Color(u.r + m.r * (1f - u.a), u.g + m.g * (1f - u.a), u.b + m.b * (1f - u.a), 1f); }
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.SetPixels(outPx); tex.Apply();
            File.WriteAllBytes(Path.Combine(Dir, $"{name}-{lang}.png"), tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            Log($"{lang} SHOT {name} what={what} date={state.Date:yyyy-MM-dd} top=[{TopDate()}] d={rig.Distance:F0} depth={depth:F0} lookAt={V(rig.LookAt)} rect=[{rect}] badge=[{bRect}] badgePos={V(badge.position)} badgeSize={F(badge.lossyScale.x)} tail=[{tRect}] foot=[{fRect}] span=[{sRect}]");
        }
        IEnumerator ShootAt(Vector3 p, string name, float dist, string what)
        {
            AimOn(p, dist); yield return Frames(2);
            UnitFlagFacing.FaceAll(Camera.main.transform.position); labels.Refresh(force: true); yield return null; Pump();
            AimOn(p, dist); labels.Refresh(force: true);
            Shot(name, what);
        }
        var dists = new[] { ("min", CameraController.MinDistance), ("close", 12000f), ("normal", CameraRig.StartDistance) };
        foreach (var (tag, dist) in dists)
            yield return ShootAt(BoundsOf(PR())!.Value.center, $"sqn101-parked-{tag}-{dist / 1000:F0}km", dist, $"101 Squadron Spitfire token, parked (rung {rung}), camera on the plane");
        if (File.Exists(FixedFile))
        {
            var v = File.ReadAllText(FixedFile).Trim().Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            var fixedAt = new Vector3(v[0], v[1], v[2]);
            Log($"{lang} FIXED point={V(fixedAt)} planeOffset={F(new Vector2(fixedAt.x - wb.center.x, fixedAt.z - wb.center.z).magnitude)}");
            foreach (var (tag, dist) in dists)
                yield return ShootAt(fixedAt, $"sqn101-fixed-{tag}-{dist / 1000:F0}km", dist, "camera on the BEFORE plane centre (same world view as BEFORE)");
        }
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
    }

    static void Prepare()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        foreach (var b in Object.FindObjectsByType<GameBoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) b.enabled = false;
    }

    [UnityTest, Timeout(900000)] public IEnumerator English() { Prepare(); yield return new EnterPlayMode(); yield return Run(false); }
    [UnityTest, Timeout(900000)] public IEnumerator Hebrew() { Prepare(); yield return new EnterPlayMode(); yield return Run(true); }
}
