#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The visual track's instruments (plan Task 1): profile lines, flags, toggles, screenshot chunks, poses,
/// teardown hand-off, the colour metrics and the GLES3 compile check.</summary>
public class VisualHarnessTests
{
    [TearDown] public void ClearToggles() => VisualToggles.Clear();

    [Test]
    public void ProfileLinesShareTheAverageAndP95AndStateTheResolution()
    {
        var frames = Enumerable.Range(1, 100).Select(i => (float)i).ToList();
        Assert.AreEqual("PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=2 frames=100 avg_ms=50.50 p95_ms=95.00",
            FrameProfile.Line("all", "gameplay03", "1920x1080", 0.8f, 2, frames));
        Assert.AreEqual("PROFILE pixelError=1 frames=100 avg_ms=50.50 p95_ms=95.00", PixelErrorProfile.Line(1f, frames));
        Assert.Throws<System.ArgumentException>(() => FrameStats.Summary(new List<float>()));
    }

    [Test]
    public void TheSummaryReportsTheMedianAndTheSpreadOfTheRepeats()
    {
        Assert.AreEqual("PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=6.00 spread_ms=2.00 median_p95_ms=9.00",
            FrameProfile.Summary("-grid", "diorama02", new[] { (5f, 8f), (7f, 10f), (6f, 9f) }));
    }

    [Test]
    public void RepeatsRunTheConfigurationsInInterleavedOrders()
    {
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, FrameProfile.Order(4, 0).ToArray());
        CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, FrameProfile.Order(4, 1).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 3, 0, 1 }, FrameProfile.Order(4, 2).ToArray());
        for (int rep = 0; rep < 6; rep++)
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 7), FrameProfile.Order(7, rep), "each repeat runs every configuration once");
    }

    [TestCase("http://localhost:8000/index.html?vprofile", true)]
    [TestCase("http://localhost:8000/index.html?profile", false)]
    [TestCase("https://html.itch.zone/html/1/vprofile-x/index.html", false)]
    public void TheVisualProfileFlagIsAWholeQueryParameter(string url, bool expected) =>
        Assert.AreEqual(expected, FrameProfile.Requested(url, new string[0], true));

    [Test]
    public void DesktopPlayersTakeDashFlags()
    {
        Assert.IsTrue(FrameProfile.Requested(null, new[] { "Statecraft", "-vprofile" }, false));
        Assert.IsTrue(VisualShots.Requested(null, new[] { "Statecraft", "-shot" }, false));
        Assert.IsFalse(VisualShots.Requested("http://localhost/index.html?shots", new string[0], true));
        Assert.IsTrue(VisualShots.Requested("http://localhost/index.html?golden&shot", new string[0], true));
    }

    [Test]
    public void TogglesRunInNameOrderAndUnknownNamesThrow()
    {
        var log = new List<string>();
        VisualToggles.Register("tiltshift", on => log.Add("tiltshift " + on));
        VisualToggles.Register("grade", on => log.Add("grade " + on));
        CollectionAssert.AreEqual(new[] { "grade", "tiltshift" }, VisualToggles.Names.ToArray());
        VisualToggles.SetAll(false);
        CollectionAssert.AreEqual(new[] { "grade False", "tiltshift False" }, log);
        Assert.Throws<KeyNotFoundException>(() => VisualToggles.Set("fog", true));
        VisualToggles.Register("grade", on => log.Add("grade again " + on));   // re-registering replaces
        VisualToggles.Set("grade", true);
        Assert.AreEqual("grade again True", log.Last());
    }

    /// <summary>Audit 2026-10-13: the close checks look at the ground, so the shots turn the haze and the settlement trees
    /// off for them (a kit tree's crown had filled hero06judea) and back on after; other toggles and unregistered names are
    /// left alone.</summary>
    [Test]
    public void TheCloseChecksAreShotWithoutHazeOrTrees()
    {
        var state = new Dictionary<string, bool>();
        foreach (var n in new[] { "haze", "trees", "kits", "grade" }) { var name = n; state[name] = true; VisualToggles.Register(name, on => state[name] = on); }
        CollectionAssert.AreEquivalent(new[] { "haze", "trees" }, VisualShots.OffForCloseChecks);
        var off = VisualShots.TurnOff(VisualShots.OffForCloseChecks.Append("fog"));
        CollectionAssert.AreEquivalent(new[] { "haze", "trees" }, off, "only registered toggles are turned off");
        Assert.IsFalse(state["haze"]); Assert.IsFalse(state["trees"]);
        Assert.IsTrue(state["kits"], "the kits stay"); Assert.IsTrue(state["grade"]);
        VisualShots.TurnOn(off);
        Assert.IsTrue(state.Values.All(on => on), "all back on after the close checks");
    }

    [Test]
    public void ShotsSplitIntoNumberedChunksAfterAHashedHeader()
    {
        var png = new byte[20000]; for (int i = 0; i < png.Length; i++) png[i] = (byte)(i * 7);
        var lines = VisualShots.Lines("gameplay03", png).ToList();
        var head = lines[0].Split(' ');
        Assert.AreEqual(new[] { "SHOT", "gameplay03", "begin" }, head.Take(3).ToArray());
        int n = int.Parse(head[3]);
        Assert.AreEqual(64, head[4].Length, "SHA-256 in hex");
        Assert.AreEqual(n + 1, lines.Count);
        StringAssert.StartsWith($"SHOT gameplay03 1/{n} ", lines[1]);
        Assert.IsTrue(lines.Skip(1).All(l => l.Split(' ')[3].Length <= VisualShots.ChunkChars));
        CollectionAssert.AreEqual(png, System.Convert.FromBase64String(string.Concat(lines.Skip(1).Select(l => l.Split(' ')[3]))));
    }

    [Test]
    public void PromoPosesAreInsideTheMapAndTheZoomRange()
    {
        Assert.AreEqual(new[] { "gameplay03", "diorama02" }, PromoPose.All.Select(p => p.Name).ToArray());
        foreach (var p in PromoPose.All)
        {
            Assert.That(p.Distance, Is.InRange(CameraController.MinDistance, CameraController.MaxDistance), p.Name);
            Assert.That(p.Lon, Is.InRange(34.2, 35.9), p.Name); Assert.That(p.Lat, Is.InRange(29.45, 33.35), p.Name);
        }
    }

    [Test]
    public void TheMapRunsItsTeardownActionsNewestFirstWhenDestroyed()
    {
        var go = new GameObject("assets"); var assets = go.AddComponent<MapRuntimeAssets>();
        var order = new List<int>();
        assets.OnTeardown(() => order.Add(1)); assets.OnTeardown(() => order.Add(2));
        Object.DestroyImmediate(go);
        CollectionAssert.AreEqual(new[] { 2, 1 }, order);
    }

    /// <summary>Review I1: in play mode Clear destroys the old map with a deferred Destroy, so its teardown must run before
    /// Clear returns (not at the end of the frame, after the new map's Init has saved state); running it again on
    /// OnDestroy does nothing (idempotent).</summary>
    [Test]
    public void ClearRunsTheOldMapsTeardownBeforeDestroyingItAndOnlyOnce()
    {
        var root = new GameObject("map"); var scene = root.AddComponent<MapScene>();
        var child = new GameObject("terrain"); child.transform.SetParent(root.transform);
        var assets = child.AddComponent<MapRuntimeAssets>();
        var log = new List<string>();
        assets.OnTeardown(() => log.Add("teardown"));
        try
        {
            scene.Clear(go => log.Add("destroy " + go.name));   // the play-mode path's deferred destroy, recorded
            CollectionAssert.AreEqual(new[] { "teardown", "destroy terrain" }, log);
            assets.RunTeardown();
        }
        finally { Object.DestroyImmediate(root); }
        CollectionAssert.AreEqual(new[] { "teardown", "destroy terrain" }, log, "RunTeardown and OnDestroy do not run it again");
    }

    /// <summary>Review M2: a throwing teardown action is logged, and the others and the owned objects are still released.</summary>
    [Test]
    public void AThrowingTeardownActionDoesNotStopTheOthers()
    {
        var go = new GameObject("assets"); var assets = go.AddComponent<MapRuntimeAssets>();
        var owned = assets.Own(new Texture2D(1, 1));
        var order = new List<int>();
        assets.OnTeardown(() => order.Add(1));
        assets.OnTeardown(() => throw new System.InvalidOperationException("boom"));
        assets.OnTeardown(() => order.Add(3));
        LogAssert.Expect(LogType.Exception, new Regex("boom"));
        Object.DestroyImmediate(go);
        CollectionAssert.AreEqual(new[] { 3, 1 }, order);
        Assert.IsTrue(owned == null, "the owned texture is destroyed");
    }

    /// <summary>Review I2: a map's token removes only its own switch, so an old map's late teardown cannot unregister the
    /// switch a rebuilt map registered under the same name.</summary>
    [Test]
    public void ARemovalTokenRemovesOnlyItsOwnSwitch()
    {
        var log = new List<string>();
        var oldToken = VisualToggles.Register("grid", on => log.Add("old " + on));
        var newToken = VisualToggles.Register("grid", on => log.Add("new " + on));
        oldToken();
        CollectionAssert.AreEqual(new[] { "grid" }, VisualToggles.Names.ToArray());
        VisualToggles.Set("grid", false);
        Assert.AreEqual("new False", log.Last());
        newToken(); newToken();
        CollectionAssert.IsEmpty(VisualToggles.Names);
    }

    [Test]
    public void TheMapOwnsItsTogglesThroughItsTeardown()
    {
        var go = new GameObject("assets"); var assets = go.AddComponent<MapRuntimeAssets>();
        assets.OnTeardown(VisualToggles.Register("haze", _ => { }));
        Object.DestroyImmediate(go);
        CollectionAssert.IsEmpty(VisualToggles.Names);
    }

    /// <summary>Review M8: undo actions an earlier Init left behind (Init called outside MapScene.Build) run on the next
    /// Init instead of piling up. (From Task 2 on, Init registers its own undo actions: the shadow fit and the toggle.)</summary>
    [Test]
    public void InitRunsAndClearsUndoActionsLeftByAnEarlierInit()
    {
        var go = new GameObject("cam", typeof(Camera)); var rig = go.AddComponent<CameraRig>();
        int ran = 0;
        System.Action stale = () => ran++;
        UnityEngine.Rendering.Volume? first = null;
        try
        {
            rig.Init(); first = rig.Volume;
            int own = rig.TeardownActions.Count;   // Init's own undo actions (Task 2: the lighting, the toggle, the shadow fit)
            rig.TeardownActions.Add(stale);
            rig.Init();
            Assert.AreEqual(1, ran);
            CollectionAssert.DoesNotContain(rig.TeardownActions, stale);
            Assert.AreEqual(own, rig.TeardownActions.Count, "a second Init does not pile up undo actions (review M3)");
        }
        finally
        {
            for (int i = rig.TeardownActions.Count - 1; i >= 0; i--) rig.TeardownActions[i]();   // the shadow fit, the toggle
            rig.TeardownActions.Clear();
            foreach (var vol in new[] { first, rig.Volume })
                if (vol != null) { Object.DestroyImmediate(vol.profile); Object.DestroyImmediate(vol.gameObject); }
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ColourMetricsBehave()
    {
        Assert.AreEqual(0f, RenderSample.DeltaE(Color.grey, Color.grey), 1e-4f);
        Assert.AreEqual(100f, RenderSample.Lab(Color.white).x, 0.1f, "white is L* 100");
        Assert.Greater(RenderSample.DeltaE(Color.red, Color.green), 80f);
        var grey = new Color(0.5f, 0.5f, 0.5f);
        foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
            Assert.Less(RenderSample.DeltaE(grey, RenderSample.SimulateCvd(grey, kind)), 1f, $"{kind} keeps greys");
        var red = RenderSample.SimulateCvd(new Color(0.8f, 0.1f, 0.1f), RenderSample.Cvd.Deuteranopia);
        var green = RenderSample.SimulateCvd(new Color(0.1f, 0.6f, 0.1f), RenderSample.Cvd.Deuteranopia);
        Assert.Less(RenderSample.DeltaE(red, green), RenderSample.DeltaE(new Color(0.8f, 0.1f, 0.1f), new Color(0.1f, 0.6f, 0.1f)) * 0.5f,
            "red and green move together for a deuteranope");
    }

    static Shader Make(string source) => ShaderUtil.CreateShaderAsset(source, false);
    const string Good = "Pass { HLSLPROGRAM\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nfloat4 f() : SV_Target { return 1; }\nENDHLSL }";
    const string Broken = "Pass { HLSLPROGRAM\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nfloat4 f() : SV_Target { return undefined_symbol; }\nENDHLSL }";
    const string ExcludedBroken = "Pass { HLSLPROGRAM\n#pragma exclude_renderers gles3 glcore\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nfloat4 f() : SV_Target { return undefined_symbol; }\nENDHLSL }";

    [Test]
    public void TheGles3CheckPassesAGoodShaderAndCatchesABrokenOne()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/HexSelection")));
        var broken = Make("Shader \"Test/Broken\" { SubShader { " + Broken + " } }");
        try { CollectionAssert.IsNotEmpty(ShaderChecks.Gles3Errors(broken)); }
        finally { Object.DestroyImmediate(broken); }
    }

    /// <summary>Negative controls (review C2): a subshader built for other APIs is not this check's business, but a shader
    /// with nothing built for GLES3 fails.</summary>
    [Test]
    public void SubshadersThatExcludeGles3AreSkippedButOneMustCompile()
    {
        Assert.IsTrue(ShaderChecks.ExcludesGles3("#pragma target 4.5\n#pragma exclude_renderers gles3 glcore\n"));
        Assert.IsTrue(ShaderChecks.ExcludesGles3("#pragma only_renderers metal vulkan d3d11\n"));
        Assert.IsFalse(ShaderChecks.ExcludesGles3("#pragma only_renderers gles3 metal\n#pragma exclude_renderers d3d11\n"));
        var mixed = Make("Shader \"Test/Mixed\" { SubShader { " + ExcludedBroken + " } SubShader { " + Good + " } }");
        var none = Make("Shader \"Test/None\" { SubShader { " + ExcludedBroken + " } }");
        try
        {
            CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(mixed), "the excluded subshader is skipped, the other compiles");
            var errors = ShaderChecks.Gles3Errors(none);
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("no subshader", errors[0]);
        }
        finally { Object.DestroyImmediate(mixed); Object.DestroyImmediate(none); }
    }

    // A fragment-stage UAV write: Metal compiles it; the GLES3 translation needs GL_EXT_texture_buffer, which WebGL 2 lacks.
    const string MetalOnly = "Pass { HLSLPROGRAM\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nRWTexture2D<float4> _U : register(u1);\nfloat4 f(float4 p : SV_POSITION) : SV_Target { _U[int2(p.xy)] = 1; return 1; }\nENDHLSL }";

    /// <summary>Review I3: a GLES3-specific negative control. The pass compiles for Metal, but its GLES3 (WebGL 2) GLSL
    /// requires an extension WebGL 2 does not have, so the check fails it; a generic HLSL error would not prove that the
    /// check looks at the GLES3 output at all.</summary>
    [Test]
    public void AShaderThatCompilesForMetalButNotForWebGL2Fails()
    {
        Assert.IsEmpty(ShaderChecks.GlslErrors("#version 300 es\n#extension GL_EXT_shader_framebuffer_fetch : enable\nvoid main() {}"));
        Assert.IsNotEmpty(ShaderChecks.GlslErrors("#version 310 es\nvoid main() {}"), "WebGL 2 is GLSL ES 3.00");
        Assert.IsNotEmpty(ShaderChecks.GlslErrors("#version 300 es\n#extension GL_EXT_texture_buffer : require\nvoid main() {}"));
        Assert.IsNotEmpty(ShaderChecks.GlslErrors(""), "no GLSL at all is not a pass");
        var shader = Make("Shader \"Test/MetalOnly\" { SubShader { " + MetalOnly + " } }");
        try
        {
            var pass = ShaderUtil.GetShaderData(shader).GetSerializedSubshader(0).GetPass(0);
            var metal = pass.CompileVariant(UnityEditor.Rendering.ShaderType.Fragment, new string[0],
                UnityEditor.Rendering.ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX);
            Assert.IsTrue(metal.Success, "compiles for Metal: " + string.Join("; ", metal.Messages.Select(m => m.message)));
            var errors = ShaderChecks.Gles3Errors(shader);
            Assert.That(errors, Has.Some.Contains("GL_EXT_texture_buffer"), string.Join("\n", errors));
        }
        finally { Object.DestroyImmediate(shader); }
    }

    [Test]
    public void AnAllowedRequireIsAcceptedAndAnUnknownOneFailsWithHowToAddIt()
    {
        const string glsl = "#version 300 es\n#extension GL_EXT_demo_allowed : require\nvoid main() {}";
        Assert.IsEmpty(ShaderChecks.GlslErrors(glsl, new HashSet<string> { "GL_EXT_demo_allowed" }), "an allowed require passes");
        var errors = ShaderChecks.GlslErrors(glsl, new HashSet<string>());
        Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
        StringAssert.Contains("GL_EXT_demo_allowed", errors[0]);
        StringAssert.Contains("AllowedRequires", errors[0], "the message says where to add it");
        Assert.IsNotEmpty(ShaderChecks.GlslErrors("#version 300 es\n#extension GL_EXT_other : require\nvoid main() {}",
            new HashSet<string> { "GL_EXT_demo_allowed" }), "allowing one extension allows no other");
        Assert.IsEmpty(ShaderChecks.GlslErrors("#version 300 es\n#extension GL_EXT_demo_allowed : enable\nvoid main() {}", new HashSet<string>()),
            "enable and warn never fail");
    }

    /// <summary>Review M6: the first pose's first runs were noisier (a cold start), so before the first pose every
    /// configuration runs once for WarmupFrames, unmeasured; then each pose runs every configuration Repeats times.</summary>
    [Test]
    public void TheProfileStartsWithOneDiscardedPassOfEveryConfiguration()
    {
        var go = new GameObject("cam", typeof(Camera)); var rig = go.AddComponent<CameraRig>();
        var frame = new GeoFrame(34.2, 35.9, 29.45, 33.35, 161529f, 431223f);
        VisualToggles.Register("grade", _ => { });
        var log = new List<string>();
        int frames = 0;
        try
        {
            var run = FrameProfile.Run(rig, frame, log.Add, false);
            while (run.MoveNext()) frames++;
        }
        finally { Object.DestroyImmediate(go); }
        int configs = 3;   // all, -grade, none
        int perRun = FrameProfile.WarmupFrames + FrameProfile.MeasuredFrames;
        Assert.AreEqual(configs * FrameProfile.WarmupFrames + PromoPose.All.Count * FrameProfile.Repeats * configs * perRun, frames);
        Assert.AreEqual(PromoPose.All.Count * FrameProfile.Repeats * configs, log.Count(l => l.StartsWith("PROFILE visual=")));
        Assert.AreEqual("PROFILE visual done", log.Last());
    }
}
