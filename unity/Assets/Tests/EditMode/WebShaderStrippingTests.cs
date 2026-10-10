#nullable enable
using NUnit.Framework;
using System;
using System.Reflection;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine;

/// <summary>Playtest 1, item 12: the three hidden URP utility shaders that WebGL 2 cannot run, and that this build never
/// draws with, are stripped from WebGL builds only; every other shader and every other platform is left alone.</summary>
public class WebShaderStrippingTests
{
    // Editor code lives in Assembly-CSharp-Editor, which a test assembly cannot reference (as in PortraitImportTests).
    static Type Stripper
    {
        get
        {
            var t = Type.GetType("Statecraft.Game.EditorTools.WebShaderStripping, Assembly-CSharp-Editor");
            Assert.IsNotNull(t, "WebShaderStripping exists");
            return t!;
        }
    }

    static bool Strips(BuildTarget target, string name) =>
        (bool)Stripper.GetMethod("Strips", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { target, name })!;

    [Test]
    public void ItIsAShaderPreprocessor() => Assert.IsTrue(typeof(IPreprocessShaders).IsAssignableFrom(Stripper));

    [TestCase("Hidden/CoreSRP/CoreCopy")]
    [TestCase("Hidden/Universal Render Pipeline/StencilDitherMaskSeed")]
    [TestCase("Hidden/Universal/HDRDebugView")]
    public void TheUnsupportedUtilityShadersAreStrippedForWebGLOnly(string name)
    {
        Assert.IsNotNull(Shader.Find(name), name + " exists (URP renamed it?)");
        Assert.IsTrue(Strips(BuildTarget.WebGL, name));
        Assert.IsFalse(Strips(BuildTarget.StandaloneOSX, name));
    }

    [TestCase("Hidden/Universal Render Pipeline/Blit")]
    [TestCase("Hidden/Universal/CoreBlit")]
    [TestCase("Universal Render Pipeline/Unlit")]
    [TestCase("Universal Render Pipeline/Terrain/Lit")]
    public void EveryOtherShaderIsKept(string name) => Assert.IsFalse(Strips(BuildTarget.WebGL, name));
}
