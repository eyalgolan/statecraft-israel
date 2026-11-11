#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Compiles a shader for GLES3 (WebGL 2) and returns the errors. Each pass is compiled for vertex and fragment, in
/// its base variant and with the main-light-shadow keyword (the variant the web tier draws the map with). A pass whose
/// source excludes GLES3 (`#pragma exclude_renderers ... gles3 ...`, or `#pragma only_renderers` without gles3) is skipped:
/// Shader Graph's Terrain Lit target emits DOTS subshaders at target 4.5 that are built for other APIs. The check cannot
/// pass vacuously: at least one subshader must have passes that are built for GLES3 and all compile.
/// Each variant is compiled with forExternalTool, which returns the generated GLSL (the plain overload returns no GLES3
/// output, so only HLSL front-end errors would show), and the GLSL is checked against WebGL 2 (GlslErrors): it must be
/// GLSL ES 3.00 and must not require an extension outside AllowedRequires (default-deny; review I3: a fragment UAV write
/// compiles for Metal and requires GL_EXT_texture_buffer here, which WebGL 2 does not have).</summary>
public static class ShaderChecks
{
    static readonly string[][] Variants = { Array.Empty<string>(), new[] { "_MAIN_LIGHT_SHADOWS" } };
    static readonly Regex Exclude = new Regex(@"#pragma\s+exclude_renderers\s+([^\r\n]*)");
    static readonly Regex Only = new Regex(@"#pragma\s+only_renderers\s+([^\r\n]*)");
    static readonly Regex Version = new Regex(@"#version\s+([^\r\n]*)");
    static readonly Regex Require = new Regex(@"#extension\s+(\w+)\s*:\s*require");

    /// <summary>The `#extension X : require` lines the check accepts. Default-deny: an entry needs a comment saying it was
    /// verified to work in WebGL 2 in headless Chrome (a `compileShader` of a shader carrying that line, see plan Task 3
    /// Step 0). An extension is never added because the check complained, only because the compile succeeded.</summary>
    static readonly HashSet<string> AllowedRequires = new HashSet<string>
    {
        // (empty: add `"GL_..."` with a "verified to work in WebGL 2 in headless Chrome, <date>, <Chrome version>" comment)
    };

    public static List<string> GlslErrors(string glsl) => GlslErrors(glsl, AllowedRequires);

    /// <summary>What in generated GLES3 GLSL WebGL 2 cannot run: no GLSL, a version other than "300 es", or a required
    /// extension that is not in <paramref name="allowedRequires"/>.</summary>
    public static List<string> GlslErrors(string glsl, ISet<string> allowedRequires)
    {
        var errors = new List<string>();
        var versions = Version.Matches(glsl ?? "");
        if (versions.Count == 0) errors.Add("no GLSL was generated");
        foreach (Match v in versions)
            if (v.Groups[1].Value.Trim() != "300 es") errors.Add($"#version {v.Groups[1].Value.Trim()}: WebGL 2 runs GLSL ES 3.00 only");
        foreach (Match r in Require.Matches(glsl ?? ""))
            if (!allowedRequires.Contains(r.Groups[1].Value))
                errors.Add($"requires {r.Groups[1].Value}, which is not in ShaderChecks.AllowedRequires: if WebGL 2 supports it, " +
                           "verify it in headless Chrome (docs/visual-v1.md, \"Probe page and command\") and add it there with a comment saying so");
        return errors;
    }

    public static bool ExcludesGles3(string passSource)
    {
        foreach (Match m in Exclude.Matches(passSource ?? "")) if (Regex.IsMatch(m.Groups[1].Value, @"\bgles3\b")) return true;
        foreach (Match m in Only.Matches(passSource ?? "")) if (!Regex.IsMatch(m.Groups[1].Value, @"\bgles3\b")) return true;
        return false;
    }

    /// <summary>The GLES3 compile errors of every pass, for the default variants and for each of <paramref name="extra"/>
    /// keyword sets (a shader's own multi_compile keywords, which the defaults do not cover: GroundDetail's SC_FURROWS).</summary>
    public static List<string> Gles3Errors(Shader shader, params string[][] extra)
    {
        if (shader == null) throw new ArgumentNullException(nameof(shader));
        var variants = Variants.Concat(extra).ToArray();
        var errors = new List<string>();
        int usable = 0;
        var data = ShaderUtil.GetShaderData(shader);
        // GetSerializedSubshader indexes the serialized subshaders, which can be fewer than SubshaderCount (URP Lit: 2 vs 1).
        for (int s = 0; s < data.SerializedSubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            var subErrors = new List<string>();
            int built = 0;
            for (int p = 0; p < sub.PassCount; p++)
            {
                var pass = sub.GetPass(p);
                if (ExcludesGles3(pass.SourceCode)) continue;
                built++;
                foreach (var keywords in variants)
                    foreach (var stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                    {
                        var info = pass.CompileVariant(stage, keywords, ShaderCompilerPlatform.GLES3x, BuildTarget.WebGL, true);
                        var where = $"{shader.name} subshader {s} pass '{pass.Name}' {stage} [{string.Join(" ", keywords)}]";
                        foreach (var m in info.Messages)
                            if (m.severity == ShaderCompilerMessageSeverity.Error) subErrors.Add($"{where}: {m.message} (line {m.line})");
                        if (!info.Success && info.Messages.Length == 0) subErrors.Add($"{where}: failed without a message");
                        if (info.Success)
                            foreach (var e in GlslErrors(System.Text.Encoding.UTF8.GetString(info.ShaderData ?? Array.Empty<byte>())))
                                subErrors.Add($"{where}: {e}");
                    }
            }
            if (built > 0 && subErrors.Count == 0) usable++;
            errors.AddRange(subErrors);
        }
        if (usable == 0) errors.Add($"{shader.name}: no subshader whose passes are built for GLES3 and all compile");
        return errors;
    }
}
