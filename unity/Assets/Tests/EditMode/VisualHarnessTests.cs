#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

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
}
