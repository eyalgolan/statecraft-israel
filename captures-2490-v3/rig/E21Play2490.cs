#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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

/// <summary>Throwaway (never committed to a PR): PR #2490 stills IN PLAY. Opens Map.unity with GameBoot switched off, enters
/// Play mode (Application.isPlaying, no -shot, so the Dakotas fly on Time.time), builds the map and calls GameRoot.Begin(inputs)
/// as GameBoot's Ready does (the run passes -nostart, so no start screen), with the collection's prefabs answering the craft
/// requests so GameRoot's own craft layer places the Dakotas. Waits at least 62 s of play after the Dakotas appear, then
/// shoots and logs the shown names; on the after tree also the wide/close Tel Aviv shots and a blast at its peak.</summary>
[Explicit("throwaway stills")]
public class E21Play2490
{
    static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".superpowers", "e21-play-2490"));
    static readonly Vector3 OpeningLookAt = new Vector3(56929.60f, 0f, 289262.20f);
    const float OpeningDistance = 31000f;

    [UnityTearDown] public IEnumerator T()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static bool Shown(Label l) => l.parent != null && l.parent.style.visibility.value == Visibility.Visible;

    static void Log(string s) => Debug.Log("E21PLAY " + s);

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) { yield return null; Pump(); } }

    static RenderTexture? pumpRt;
    /// <summary>Batchmode has no display, so nothing renders the main camera each frame, yet the craft layer asks for its
    /// models from beginCameraRendering (OriginalModelRequests) and the Dakotas face the camera there: render it every frame
    /// into a scratch texture, as a window would.</summary>
    static void Pump()
    {
        var cam = Camera.main; if (cam == null) return;
        if (pumpRt == null) pumpRt = new RenderTexture(CivMovementShots.Width, CivMovementShots.Height, 24, RenderTextureFormat.ARGB32);
        var before = cam.targetTexture; cam.targetTexture = pumpRt; cam.Render(); cam.targetTexture = before;
    }

    static void Aim(CameraRig rig, CameraController cc, Vector3 at, float distance)
    {
        rig.LookAt = at; rig.Distance = distance; rig.Yaw = 0f; rig.Apply();
        cc.Settle();
    }

    /// <summary>Before EnterPlayMode, in the test method itself: the test framework restores only the test method's own
    /// enumerator position after the domain reload, so nothing before the reload may live in a local or a nested enumerator.</summary>
    static void Prepare()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        foreach (var b in Object.FindObjectsByType<GameBoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) b.enabled = false;
    }

    IEnumerator Run(bool hebrew, bool extra, bool seq2 = false)
    {
        var lang = hebrew ? "he" : "en";
        Assert.IsTrue(Application.isPlaying, "in Play mode");
        Assert.IsFalse(VisualShots.Requested(), "no -shot");
        Assert.IsTrue(StartScreenPresenter.Skipped(), "-nostart");
        Directory.CreateDirectory(Dir); Directory.CreateDirectory(CivMovementShots.ShotDir);
        var tmp = Path.Combine(Path.GetTempPath(), "e21-play-2490-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tmp, "save")); Directory.CreateDirectory(Path.Combine(tmp, "settings"));
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(CivMovementShots.Width, CivMovementShots.Height, 24, RenderTextureFormat.ARGB32);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.clear;
        doc.panelSettings = ps;
        var map = Object.FindFirstObjectByType<MapScene>();
        var inputs = MapFocusTests.Inputs;
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(Path.Combine(tmp, "save"));
        root.Settings = new SettingsStore(Path.Combine(tmp, "settings"));
        if (hebrew) root.Language = UiLanguage.Hebrew;
        SeaAirCollection.Request = TalModelsShots.Answer(Array.Empty<string>());
        root.Begin(inputs);   // GameBoot.Ready's call
        float begun = Time.time;
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 300 && !(ui.Q("top-bar").worldBound.height > 0); i++) { yield return null; Pump(); }
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        if (root.Advisor != null && root.Advisor.IsOpen) UiTestKit.Press(ui.Q<Button>("advisor-close"));
        var state = root.Bridge!.State; var content = root.Bridge.Content;
        Assert.AreEqual(new DateTime(1948, 5, 15), state.Date.Date, "date");
        var rig = Camera.main.GetComponent<CameraRig>(); var cc = rig.GetComponent<CameraController>();
        Log($"{lang} begun t={begun:F1} boot lookAt={rig.LookAt} d={rig.Distance:F0} yaw={rig.Yaw:F1} date={state.Date:yyyy-MM-dd} turn={state.Turn}");
        Aim(rig, cc, OpeningLookAt, OpeningDistance);
        float appear = -1f; DakotaLoop[] loops = Array.Empty<DakotaLoop>();
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline)
        {
            loops = Object.FindObjectsByType<DakotaLoop>(FindObjectsSortMode.None);
            if (loops.Length > 0) { appear = Time.time; break; }
            yield return null; Pump();
        }
        Assert.Greater(loops.Length, 0, "the Dakotas appear in play");
        Log($"{lang} dakotas={loops.Length} appear t={appear:F2} still={string.Join(",", loops.Select(l => l.Still))}");
        while (Time.time < appear + 62f) { yield return null; Pump(); }
        var labels = root.Labels!;
        var layer = map.Terrain.transform.Find(SeaAirCollection.Name).GetComponent<SeaAirCollection>();
        var tlv = content.SettlementHexes.First(s => s.Id == "tel_aviv");
        var hex = content.HexAt(tlv.Q, tlv.R)!;
        var city = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        city.y = map.Terrain.SampleHeight(city) + map.Terrain.transform.position.y;
        Log($"{lang} telaviv={city}");

        IEnumerator Take(string what, Vector3 at, float distance, bool listLabels)
        {
            Aim(rig, cc, at, distance);
            yield return Frames(3);
            UnitFlagFacing.FaceAll(Camera.main.transform.position);
            labels.Refresh(force: true); yield return null; Pump();
            labels.Refresh(force: true);
            Shoot(what, listLabels);
        }

        void Shoot(string what, bool listLabels)
        {
            float t = Time.time;
            string where = string.Join(" | ", loops.Select(l => $"{l.name}@{l.transform.position}"));
            if (listLabels)
            {
                var shown = new List<string>();
                for (int i = 0; i < labels.Labels.Count; i++) if (Shown(labels.Labels[i])) shown.Add($"S{i}:{labels.Labels[i].text}");
                for (int i = 0; i < labels.LocalityLabels.Count; i++) if (Shown(labels.LocalityLabels[i])) shown.Add($"L{i}:{labels.LocalityLabels[i].text}");
                for (int i = 0; i < labels.LocationLabels.Count; i++) if (Shown(labels.LocationLabels[i])) shown.Add($"M{i}:{labels.LocationLabels[i].text}");
                Log($"{lang} {what} d={rig.Distance:F0} lookAt={rig.LookAt} t={t:F2} sinceDakotas={t - appear:F2} keepClear={layer.KeepClear.Count} "
                    + $"boxes={string.Join(" | ", layer.KeepClear.SelectMany(b => b).Select(b => $"{b.center} {b.size}"))} shown=[{string.Join(" ; ", shown)}]");
            }
            Log($"{lang} SHOT {what} d={rig.Distance:F0} lookAt={rig.LookAt} t={t:F2} sinceDakotas={t - appear:F2} planes={where}");
            CivMovementShots.Shot(rt, Path.Combine("..", "e21-play-2490", $"{what}-{lang}"));
        }

        if (seq2) { yield return Seq2(lang, rig, cc, loops, city, appear, Shoot, labels); if (Directory.Exists(tmp)) Directory.Delete(tmp, true); yield break; }
        yield return Take("labels-turnmap", OpeningLookAt, OpeningDistance, true);
        if (extra)
        {
            yield return Take("wide-31km", city, 31000f, true);
            yield return Take("close-20km", city, 20000f, true);
            // A blast at its peak: the flipbook's fireball at full size (BlastSwell reaches 1 at a quarter of its life).
            var bombsP = typeof(DakotaLoop).GetProperty("Bombs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(bombsP, "DakotaLoop.Bombs");
            var runs = loops.Select(l => bombsP!.GetValue(l)).Where(r => r != null).ToList();
            Log($"{lang} bomb runs={runs.Count}");
            Assert.Greater(runs.Count, 0, "bombs on 15 May");
            var rType = runs[0]!.GetType();
            var blastsP = rType.GetProperty("Blasts", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
            var frameM = rType.GetMethod("BlastFrame", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
            Aim(rig, cc, city, 20000f);
            var wanted = new[] { 4, 6, 9 }; int got = 0; object? run = null; int slot = -1; int lastFrame = -1;
            float until = Time.realtimeSinceStartup + 200f;
            while (got < wanted.Length && Time.realtimeSinceStartup < until)
            {
                yield return null; Pump();
                if (run == null)
                {
                    foreach (var r in runs)
                    {
                        var bl = (IReadOnlyList<Renderer>)blastsP.GetValue(r);
                        for (int s = 0; s < bl.Count; s++)
                            if (bl[s].enabled && (int)frameM.Invoke(r, new object[] { s })! <= 2) { run = r; slot = s; break; }
                        if (run != null) break;
                    }
                    continue;
                }
                var blasts = (IReadOnlyList<Renderer>)blastsP.GetValue(run);
                if (!blasts[slot].enabled) { run = null; got = 0; continue; }
                int f = (int)frameM.Invoke(run, new object[] { slot })!;
                if (f == lastFrame || f < wanted[got]) continue;
                lastFrame = f;
                var at = blasts[slot].transform.position;
                Log($"{lang} blast frame={f} at={at} scale={blasts[slot].transform.localScale.x:F0}");
                // Same frame, three views: the city at 20 km, then the blast itself at 8 km and 4 km.
                Shoot($"blast-f{f:D2}-20km", false);
                var ground = new Vector3(at.x, city.y, at.z);
                rig.LookAt = ground; rig.Distance = 8000f; rig.Yaw = 0f; rig.Apply(); cc.Settle();
                Shoot($"blast-f{f:D2}-8km", false);
                rig.LookAt = ground; rig.Distance = 4000f; rig.Yaw = 0f; rig.Apply(); cc.Settle();
                Shoot($"blast-f{f:D2}-4km", false);
                Aim(rig, cc, city, 20000f);
                got++;
            }
            Log($"{lang} blast shots={got}");
            Assert.Greater(got, 0, "a blast was caught");

            // 53's sequence: ONE drop of the lead Dakota, one fixed camera, sampled by the loop clock (SpitfireLoop.Clock =
            // Time.time; the lead's Lag is 0): (1) mid-fall = release + 1.0 s, (2) impact = release + fall (2 s) + 0.08 s,
            // (3) release + fall + 1.0 s (blast 2.4 s long). Timings from SeaAirCollection.Dakota.cs:100/107/117 at 5ca573246.
            const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            // By reflection: main (the before tree) has no DakotaBombFallS, and this file compiles on both trees.
            float Const(string n) => (float)typeof(SeaAirCollection).GetField(n, BindingFlags.Static | BindingFlags.Public)!.GetRawConstantValue();
            float fall = Const("DakotaBombFallS"), period = Const("DakotaLoopSeconds");
            Log($"{lang} SEQ consts fall={fall} flash={Const("DakotaFlashS")} blast={Const("DakotaBlastS")} interval={Const("DakotaBombIntervalS")} loop={period}");
            var lagP = typeof(DakotaLoop).GetProperty("Lag", Any)!;
            var lead = loops.OrderBy(l => (float)lagP.GetValue(l)).First();
            float lag = (float)lagP.GetValue(lead);
            var leadRun = bombsP!.GetValue(lead)!;
            var dropsList = ((IEnumerable)rType.GetProperty("Drops", Any)!.GetValue(leadRun)).Cast<object>().ToList();
            var dT = dropsList[0].GetType();
            FieldInfo fAt = dT.GetField("At"), fFrom = dT.GetField("From"), fTo = dT.GetField("To");
            Assert.Greater(dropsList.Count, 0, "the lead has drops");
            float uNow = Time.time - lag;
            // The next release at least 3 s ahead (camera placed and settled before frame 1).
            float best = float.MaxValue; object? pick = null;
            foreach (var dr in dropsList)
            {
                float at0 = (float)fAt.GetValue(dr);
                float ahead = at0 - uNow; ahead -= period * Mathf.Floor(ahead / period);
                if (ahead < 3f) ahead += period;
                if (ahead < best) { best = ahead; pick = dr; }
            }
            float uRel = uNow + best;
            var from = (Vector3)fFrom.GetValue(pick); var to = (Vector3)fTo.GetValue(pick);
            Log($"{lang} SEQ drop At={(float)fAt.GetValue(pick):F2} release u={uRel:F2} impact u={uRel + fall:F2} from={from} to={to} lag={lag:F2} waitS={best:F1} dropsLead={dropsList.Count}");
            var seqAt = new Vector3(0.5f * (from.x + to.x), to.y, 0.5f * (from.z + to.z));
            Aim(rig, cc, seqAt, 10000f);
            var bombsR = (IReadOnlyList<Renderer>)rType.GetProperty("Bombs", Any)!.GetValue(leadRun);
            var flashR = (IReadOnlyList<Renderer>)rType.GetProperty("Flashes", Any)!.GetValue(leadRun);
            var blastR = (IReadOnlyList<Renderer>)blastsP.GetValue(leadRun);
            string Near(IReadOnlyList<Renderer> rs, Vector3 p)
            {
                var on = rs.Where(r => r.enabled).OrderBy(r => Vector2.Distance(new Vector2(r.transform.position.x, r.transform.position.z), new Vector2(p.x, p.z))).ToList();
                if (on.Count == 0) return "none-enabled";
                var q = on[0].transform.position;
                return $"{q} dxzFromImpact={Vector2.Distance(new Vector2(q.x, q.z), new Vector2(to.x, to.z)):F0}m enabled={on.Count}";
            }
            var seq = new[] { (name: "seq1-midfall", u: uRel + 1.0f), (name: "seq2-impact", u: uRel + fall + 0.08f), (name: "seq3-fireball", u: uRel + fall + 1.0f) };
            float seqUntil = Time.realtimeSinceStartup + period + 30f;
            foreach (var (name, u) in seq)
            {
                while (Time.time - lag < u && Time.realtimeSinceStartup < seqUntil) { yield return null; Pump(); }
                float uAt = Time.time - lag;
                Log($"{lang} SEQ {name} target u={u:F2} actual u={uAt:F3} (release+{uAt - uRel:F3}s) camLookAt={rig.LookAt} d={rig.Distance:F0} "
                    + $"lead@{lead.transform.position} bomb={Near(bombsR, from)} flash={Near(flashR, to)} blast={Near(blastR, to)}");
                Shoot(name, false);
            }
        }
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
    }

    [UnityTest, Timeout(1200000)] public IEnumerator English() { Prepare(); yield return new EnterPlayMode(); yield return Run(false, false); }
    [UnityTest, Timeout(1200000)] public IEnumerator Hebrew() { Prepare(); yield return new EnterPlayMode(); yield return Run(true, false); }

    /// <summary>53's sequence, v2: ONE drop of the lead Dakota chosen as far as possible from every smoke column's foot (both
    /// aircraft), so no column hides its fall; one fixed camera per series (12 km and 20 km, both shot in the same frame);
    /// sampled by the loop clock (Time.time, the lead's Lag 0): (1) release + 1.0 s (mid-fall, fall 2 s), (2) release + 2 s
    /// + ~0.08 s (impact, flash 0.5 s), (3) release + 3.0 s (blast 2.4 s). The bomb's expected position is DakotaBombRun.Show's
    /// formula; the enabled bomb nearest it is logged with its viewport point. At (1) also the Tel Aviv wide/close shots.</summary>
    static IEnumerator Seq2(string lang, CameraRig rig, CameraController cc, DakotaLoop[] loops, Vector3 city, float appear,
        Action<string, bool> shoot, MapLabelsPresenter labels)
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        float Const(string n) => (float)typeof(SeaAirCollection).GetField(n, BindingFlags.Static | BindingFlags.Public)!.GetRawConstantValue();
        float fall = Const("DakotaBombFallS"), period = Const("DakotaLoopSeconds");
        Log($"{lang} SEQ2 consts fall={fall} flash={Const("DakotaFlashS")} blast={Const("DakotaBlastS")} interval={Const("DakotaBombIntervalS")} loop={period}");
        var bombsP = typeof(DakotaLoop).GetProperty("Bombs", Any)!; var lagP = typeof(DakotaLoop).GetProperty("Lag", Any)!;
        var lead = loops.OrderBy(l => (float)lagP.GetValue(l)).First(); float lag = (float)lagP.GetValue(lead);
        var run = bombsP.GetValue(lead)!; var rT = run.GetType();
        var sites = loops.Select(l => bombsP.GetValue(l)).Where(r => r != null)
            .SelectMany(r => ((IEnumerable)rT.GetProperty("ColumnSites", Any)!.GetValue(r)).Cast<Vector3>()).ToList();
        var drops = ((IEnumerable)rT.GetProperty("Drops", Any)!.GetValue(run)).Cast<object>().ToList();
        var dT = drops[0].GetType(); FieldInfo fAt = dT.GetField("At"), fFrom = dT.GetField("From"), fTo = dT.GetField("To");
        static float Dxz(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        float Clear(object d) { var f = (Vector3)fFrom.GetValue(d); var t = (Vector3)fTo.GetValue(d); return sites.Min(c => Mathf.Min(Dxz(c, f), Mathf.Min(Dxz(c, t), Dxz(c, 0.5f * (f + t))))); }
        var pick = drops.OrderByDescending(Clear).First();
        float at0 = (float)fAt.GetValue(pick); var from = (Vector3)fFrom.GetValue(pick); var to = (Vector3)fTo.GetValue(pick);
        float uNow = Time.time - lag, ahead = at0 - uNow; ahead -= period * Mathf.Floor(ahead / period); if (ahead < 3f) ahead += period;
        float uRel = uNow + ahead;
        Log($"{lang} SEQ2 sites={string.Join(" ", sites)} drops={drops.Count} pick At={at0:F2} clearM={Clear(pick):F0} release u={uRel:F2} impact u={uRel + fall:F2} from={from} to={to} wait={ahead:F1}s");
        var bombs = (IReadOnlyList<Renderer>)rT.GetProperty("Bombs", Any)!.GetValue(run);
        var flashes = (IReadOnlyList<Renderer>)rT.GetProperty("Flashes", Any)!.GetValue(run);
        var blasts = (IReadOnlyList<Renderer>)rT.GetProperty("Blasts", Any)!.GetValue(run);
        var ground = new Vector3(0.5f * (from.x + to.x), to.y, 0.5f * (from.z + to.z));
        void Pose(float d) { rig.LookAt = ground; rig.Distance = d; rig.Yaw = 0f; rig.Apply(); cc.Settle(); }
        Pose(12000f);
        string NearR(IReadOnlyList<Renderer> rs, Vector3 p)
        {
            var on = rs.Where(r => r.enabled).OrderBy(r => Vector3.Distance(r.transform.position, p)).ToList();
            if (on.Count == 0) return "none";
            var q = on[0].transform.position; var vp = Camera.main.WorldToViewportPoint(q);
            return $"{q} off={Vector3.Distance(q, p):F0}m dxzImpact={Dxz(q, to):F0}m vp=({vp.x:F3},{vp.y:F3},{vp.z:F0}) n={on.Count}";
        }
        var steps = new[] { ("s2-1-midfall", 1.0f), ("s2-2-impact", fall + 0.08f), ("s2-3-fireball", fall + 1.0f) };
        float until = Time.realtimeSinceStartup + period + 60f;
        foreach (var (name, rel) in steps)
        {
            while (Time.time - lag < uRel + rel && Time.realtimeSinceStartup < until) { yield return null; Pump(); }
            float u = Time.time - lag, age = u - uRel, f = Mathf.Clamp01(age / fall);
            var exp = Vector3.Lerp(from, to, f); exp.y = from.y - (from.y - to.y) * f * f;
            foreach (var d in new[] { 12000f, 20000f })
            {
                Pose(d);
                Log($"{lang} SEQ2 {name}-{d / 1000:F0}km u={u:F3} release+{age:F3}s lead@{lead.transform.position} expectedBomb={exp} bomb={NearR(bombs, exp)} flash={NearR(flashes, to)} blast={NearR(blasts, to)}");
                shoot($"{name}-{d / 1000:F0}km", false);
            }
            if (name == "s2-1-midfall")
            {
                foreach (var (w, d) in new[] { ("s2-wide-31km", 31000f), ("s2-close-20km", 20000f) })
                {
                    rig.LookAt = city; rig.Distance = d; rig.Yaw = 0f; rig.Apply(); cc.Settle();
                    UnitFlagFacing.FaceAll(Camera.main.transform.position); labels.Refresh(force: true);
                    shoot(w, true);
                }
            }
            Pose(12000f);
        }
    }

    [UnityTest, Timeout(1800000)] public IEnumerator EnglishSeq() { Prepare(); yield return new EnterPlayMode(); yield return Run(false, false, true); }
    [UnityTest, Timeout(1800000)] public IEnumerator EnglishFull() { Prepare(); yield return new EnterPlayMode(); yield return Run(false, true); }
}
