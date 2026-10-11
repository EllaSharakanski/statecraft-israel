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
        return (string?)t!.GetMethod("ShareableRefusal", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object?[] { c, null });   // event content only
    }

    [Test]
    public void AShareableBuildWaitsForTheSensitivityReview()
    {
        var c = ContentLoader.Load(Paths.Content);
        Assert.IsNull(Refusal(c), "the owner signed off all seven on 2026-10-10 (D11)");
        foreach (var e in c.Events) if (ReleaseGate.SensitiveEvents(c).Contains(e.Id)) e.Status = "fact-checked";   // as before the sign-off
        var refusal = Refusal(c);
        Assert.IsNotNull(refusal, "an unreviewed sensitive event blocks a shareable build");
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
        Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-executeMethod", "Statecraft.Game.EditorTools.Build.Mac" }, c, null }), "a dev build is not gated");
        Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, null }), "all seven are sensitivity-reviewed (D11)");
        foreach (var e in c.Events) if (e.Id == "reparations_claim") e.Status = "fact-checked";
        StringAssert.Contains("reparations_claim", (string?)gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, null }));
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        foreach (var method in new[] { "public static void Web()", "public static void Mac()" })
        {
            var body = source.Substring(source.IndexOf(method, StringComparison.Ordinal), 400);
            StringAssert.Contains("RefuseUnlessShareable()", body, method + " checks the gate first");
        }
    }

    // Owner ruling 2026-10-11: the event photographs of sensitive events (and sinai_withdrawal's aircraft wreck) need the
    // owner's sensitivity review before a shareable build may carry them. The gate reads the catalog the build ships.
    static readonly string[] SensitivePhotos =
        { "altalena", "bernadotte_assassination", "operation_hiram", "operation_horev", "operation_yoav", "sinai_withdrawal", "ten_days" };

    static Statecraft.Game.UI.EventImageCatalog ShippedPhotos()
    {
        var real = UnityEditor.AssetDatabase.LoadAssetAtPath<Statecraft.Game.UI.EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        Assert.IsNotNull(real, "the shipped catalog (run EventImageImport.Run)");
        return UnityEngine.Object.Instantiate(real);
    }

    [Test]
    public void ShareableBuildsWaitForTheSensitivityReviewOfTheEventPhotographs()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!;
        var gate = t.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        var c = ContentLoader.Load(Paths.Content);
        var photos = ShippedPhotos();
        try
        {
            CollectionAssert.AreEqual(SensitivePhotos, photos.SensitiveUnreviewed(), "exactly the owner's list, none reviewed yet");
            var refusal = (string?)gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, photos });
            Assert.IsNotNull(refusal, "unreviewed sensitive photographs block a shareable build");
            foreach (var id in SensitivePhotos) StringAssert.Contains(id, refusal);
            StringAssert.Contains("event photographs", refusal);
            Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-executeMethod", "Statecraft.Game.EditorTools.Build.Web" }, c, photos }),
                "a dev build still carries them (the owner reviews them in one)");
            for (int i = 0; i < photos.entries.Count; i++)                       // as the owner's sign-off would
            {
                var e = photos.entries[i]; e.sensitivityReviewed = e.sensitive; photos.entries[i] = e;
            }
            Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity", "-shareable" }, c, photos }), "all reviewed: the build may be shared");
        }
        finally { UnityEngine.Object.DestroyImmediate(photos); }
    }

    [Test]
    public void TheShareableBuildChecksTheShippedCatalog()
    {
        var source = System.IO.File.ReadAllText("Assets/Editor/Build.cs");
        var body = source.Substring(source.IndexOf("static bool RefuseUnlessShareable()", StringComparison.Ordinal), 700);
        StringAssert.Contains("EventImageImport.CatalogPath", body, "the gate loads the catalog the build ships");
    }
}
