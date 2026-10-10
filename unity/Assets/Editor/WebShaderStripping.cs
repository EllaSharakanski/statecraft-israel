#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Strips, from WebGL builds only, three hidden URP utility shaders that need shader target 4.5 (WebGL 2 is
    /// ES 3.0) and that this build never draws with. Without them the browser console no longer reports them as "not
    /// supported on this GPU" (playtest 1, item 12). Why each is unused on WebGL (URP 17.3 / Core RP 17.3 sources):
    /// - Hidden/CoreSRP/CoreCopy: Blitter.Initialize creates its material unconditionally, but the render-graph copy
    ///   pass that draws with it needs framebuffer-fetch emulation, which RenderGraphUtils reports unsupported on WebGL
    ///   (IsFramebufferFetchEmulationSupportedOnCurrentPlatform), so URP blits instead.
    /// - Hidden/Universal Render Pipeline/StencilDitherMaskSeed: UniversalRenderer always creates the
    ///   StencilCrossFadeRenderPass's materials, but the pass only runs for LOD cross-fade dithering type Stencil; the
    ///   URP assets use Blue Noise, and the map has no LOD groups.
    /// - Hidden/Universal/HDRDebugView: the Rendering Debugger's HDR view (DebugHandler); release builds have no
    ///   Rendering Debugger and the project's HDR output is off.</summary>
    public sealed class WebShaderStripping : IPreprocessShaders
    {
        public static readonly string[] Unused =
        {
            "Hidden/CoreSRP/CoreCopy",
            "Hidden/Universal Render Pipeline/StencilDitherMaskSeed",
            "Hidden/Universal/HDRDebugView",
        };

        public int callbackOrder => 0;

        /// <summary>True when every variant of <paramref name="shaderName"/> is dropped for <paramref name="target"/>.</summary>
        public static bool Strips(BuildTarget target, string shaderName) =>
            target == BuildTarget.WebGL && Array.IndexOf(Unused, shaderName) >= 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (Strips(EditorUserBuildSettings.activeBuildTarget, shader.name)) data.Clear();
        }
    }
}
