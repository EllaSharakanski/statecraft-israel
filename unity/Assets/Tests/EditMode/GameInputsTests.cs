using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.TestTools;

public class GameInputsTests
{
    [Test]
    public void TheManifestListsEveryContentFileThePlayerNeeds()
    {
        var files = ContentManifest.Collect(Paths.Content);
        CollectionAssert.Contains(files, "people.yaml");
        CollectionAssert.Contains(files, "events/armistice_egypt.yaml");
        CollectionAssert.Contains(files, "map/hexes.json");
        CollectionAssert.Contains(files, "map/settlements_1948.yaml");
        CollectionAssert.DoesNotContain(files, "map/precip_bio12.u16");   // tool input only
        CollectionAssert.AreEqual(files.OrderBy(f => f, System.StringComparer.Ordinal).ToList(), files);
        Assert.AreEqual(files, ContentManifest.Parse(ContentManifest.Write(files)));
    }

    [Test]
    public void ABrokenManifestIsAClearError()
    {
        var ex = Assert.Throws<InvalidDataException>(() => ContentManifest.Parse("{\"files\": []}"));
        Assert.AreEqual("manifest.json: no files", ex.Message);
    }

    static string StagedSite()
    {
        var site = Path.Combine(Path.GetTempPath(), "statecraft-site-" + System.Guid.NewGuid().ToString("N"));
        ContentManifest.Copy(Paths.Content, Path.Combine(site, "content"));
        Directory.CreateDirectory(Path.Combine(site, "map"));
        foreach (var f in new[] { "heightmap.json", "heightmap.r16" }) File.Copy(Path.Combine(Paths.StreamingMap, f), Path.Combine(site, "map", f));
        return site;
    }

    [UnityTest]
    public IEnumerator FetchingOverUrlsLoadsTheSameContentAsTheDisk()
    {
        var site = StagedSite();
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
        var fetched = fetch.GetResult();
        var disk = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        CollectionAssert.AreEqual(disk.Map.HeightmapR16, fetched.Map.HeightmapR16);
        Assert.AreEqual(disk.Map.HeightmapJson, fetched.Map.HeightmapJson);
        Assert.AreEqual(disk.Map.SettlementsYaml, fetched.Map.SettlementsYaml);
        Assert.AreEqual(disk.Map.HexesJson, fetched.Map.HexesJson);
        Assert.AreEqual(disk.Content.Events.Count, fetched.Content.Events.Count);
        Assert.AreEqual(Simulation.NewGame(disk.Content, 7, Mode.Historical).Hash(), Simulation.NewGame(fetched.Content, 7, Mode.Historical).Hash());
        Directory.Delete(site, true);
    }

    [UnityTest]
    public IEnumerator AMissingFileNamesTheFile()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
        var ex = Assert.Throws<IOException>(() => fetch.GetResult());
        StringAssert.StartsWith("people.yaml: ", ex.Message);
        Directory.Delete(site, true);
    }

    [UnityTest]
    public IEnumerator AFailedFetchLeavesNoRequestOpen()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
        Assert.Throws<IOException>(() => fetch.GetResult());
        Assert.AreEqual(0, GameInputs.OpenRequests, "requests still in flight after a failed fetch were not disposed");
        Directory.Delete(site, true);
    }

    [Test]
    public void VerifyAcceptsACopyAndRejectsAMissingFileOrManifestMismatch()
    {
        var target = Path.Combine(Path.GetTempPath(), "statecraft-verify-" + System.Guid.NewGuid().ToString("N"));
        try
        {
            ContentManifest.Copy(Paths.Content, target);
            ContentManifest.Verify(Paths.Content, target);
            File.Delete(Path.Combine(target, "people.yaml"));
            StringAssert.Contains("people.yaml", Assert.Throws<InvalidDataException>(() => ContentManifest.Verify(Paths.Content, target)).Message);
            ContentManifest.Copy(Paths.Content, target);
            File.WriteAllText(Path.Combine(target, ContentManifest.FileName), ContentManifest.Write(new[] { "people.yaml" }));
            StringAssert.Contains("does not match", Assert.Throws<InvalidDataException>(() => ContentManifest.Verify(Paths.Content, target)).Message);
        }
        finally { if (Directory.Exists(target)) Directory.Delete(target, true); }
    }
}
