#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Sim;

/// <summary>Final review I4: a shareable build (Build.Web -shareable) is refused while any sensitive event lacks the owner's
/// sensitivity review, and the refusal names each one.</summary>
public class BuildGateTests
{
    static string? Refusal(Content c)
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "Build exists");
        return (string?)t!.GetMethod("ShareableRefusal", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { c });
    }

    [Test]
    public void AShareableBuildWaitsForTheSensitivityReview()
    {
        var c = ContentLoader.Load(Paths.Content);
        var refusal = Refusal(c);
        Assert.IsNotNull(refusal, "the seven sensitive events are not reviewed yet");
        foreach (var id in ReleaseGate.SensitiveUnreviewed(c)) StringAssert.Contains(id, refusal);
        foreach (var e in c.Events) if (ReleaseGate.SensitiveEvents(c).Contains(e.Id)) e.Status = ReleaseGate.ReviewedStatus;   // as the owner's sign-off would
        Assert.IsNull(Refusal(c), "all reviewed: the build may be shared");
    }

    /// <summary>Rereview: the gate applies to every player build made with -shareable (Build.Web and Build.Mac call the same
    /// check); without the flag, dev builds stay ungated (web_check needs them).</summary>
    [Test]
    public void TheShareableFlagGatesEveryPlayerBuild()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        var gate = t.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        var c = ContentLoader.Load(Paths.Content);
        Assert.IsNull(gate.Invoke(null, new object[] { new[] { "Unity", "-executeMethod", "Statecraft.Game.EditorTools.Build.Mac" }, c }), "a dev build is not gated");
        StringAssert.Contains("reparations_claim", (string?)gate.Invoke(null, new object[] { new[] { "Unity", "-shareable" }, c }));
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        foreach (var method in new[] { "public static void Web()", "public static void Mac()" })
        {
            var body = source.Substring(source.IndexOf(method, StringComparison.Ordinal), 400);
            StringAssert.Contains("RefuseUnlessShareable()", body, method + " checks the gate first");
        }
    }
}
