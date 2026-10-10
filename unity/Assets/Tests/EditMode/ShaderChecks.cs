#nullable enable
using System;
using System.Collections.Generic;
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
/// GLSL ES 3.00 and must not require an extension, which WebGL 2 does not have for anything the translator requires
/// (review I3: a fragment UAV write compiles for Metal and requires GL_EXT_texture_buffer here).</summary>
public static class ShaderChecks
{
    static readonly string[][] Variants = { Array.Empty<string>(), new[] { "_MAIN_LIGHT_SHADOWS" } };
    static readonly Regex Exclude = new Regex(@"#pragma\s+exclude_renderers\s+([^\r\n]*)");
    static readonly Regex Only = new Regex(@"#pragma\s+only_renderers\s+([^\r\n]*)");
    static readonly Regex Version = new Regex(@"#version\s+([^\r\n]*)");
    static readonly Regex Require = new Regex(@"#extension\s+(\w+)\s*:\s*require");

    /// <summary>What in generated GLES3 GLSL WebGL 2 cannot run: no GLSL, a version other than "300 es", or an extension
    /// the code requires.</summary>
    public static List<string> GlslErrors(string glsl)
    {
        var errors = new List<string>();
        var versions = Version.Matches(glsl ?? "");
        if (versions.Count == 0) errors.Add("no GLSL was generated");
        foreach (Match v in versions)
            if (v.Groups[1].Value.Trim() != "300 es") errors.Add($"#version {v.Groups[1].Value.Trim()}: WebGL 2 runs GLSL ES 3.00 only");
        foreach (Match r in Require.Matches(glsl ?? "")) errors.Add($"requires {r.Groups[1].Value}, which WebGL 2 does not have");
        return errors;
    }

    public static bool ExcludesGles3(string passSource)
    {
        foreach (Match m in Exclude.Matches(passSource ?? "")) if (Regex.IsMatch(m.Groups[1].Value, @"\bgles3\b")) return true;
        foreach (Match m in Only.Matches(passSource ?? "")) if (!Regex.IsMatch(m.Groups[1].Value, @"\bgles3\b")) return true;
        return false;
    }

    public static List<string> Gles3Errors(Shader shader)
    {
        if (shader == null) throw new ArgumentNullException(nameof(shader));
        var errors = new List<string>();
        int usable = 0;
        var data = ShaderUtil.GetShaderData(shader);
        for (int s = 0; s < data.SubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            var subErrors = new List<string>();
            int built = 0;
            for (int p = 0; p < sub.PassCount; p++)
            {
                var pass = sub.GetPass(p);
                if (ExcludesGles3(pass.SourceCode)) continue;
                built++;
                foreach (var keywords in Variants)
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
