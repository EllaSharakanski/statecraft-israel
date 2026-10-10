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
}
