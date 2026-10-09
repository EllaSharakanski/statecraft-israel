using System;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Checks the real content/ folder at the repository root.</summary>
public class RealContentTests
{
    public static string ContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "content", "people.yaml"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("content/people.yaml not found above test dir"), "content");
    }

    static readonly Content C = ContentLoader.Load(ContentRoot());
    static readonly DateTime Checked = new DateTime(2026, 10, 9);

    [Fact]
    public void EveryHistoricalRecordIsSourcedAndFactChecked()
    {
        foreach (var p in C.People)
        {
            Assert.True(p.Status == "fact-checked", $"people[{p.Id}] status {p.Status}");
            foreach (var t in p.Offices) Assert.True(t.Sources.Count > 0, $"people[{p.Id}].offices.{t.Office} has no source");
        }
        foreach (var l in C.Legislatures) { Assert.Equal("fact-checked", l.Status); Assert.NotEmpty(l.Sources); }
        foreach (var g in C.Governments) { Assert.Equal("fact-checked", g.Status); Assert.NotEmpty(g.Sources); }
        foreach (var a in C.Aliyah) { Assert.Equal("fact-checked", a.Status); Assert.NotEmpty(a.Sources); }
        foreach (var a in C.PopulationAnchors) { Assert.Equal("fact-checked", a.Status); Assert.NotEmpty(a.Sources); }
        foreach (var e in C.Events) { Assert.Equal("fact-checked", e.Status); Assert.NotEmpty(e.Sources); }
        var all = C.People.SelectMany(p => p.Offices.SelectMany(t => t.Sources))
            .Concat(C.Legislatures.SelectMany(l => l.Sources)).Concat(C.Governments.SelectMany(g => g.Sources))
            .Concat(C.Aliyah.SelectMany(a => a.Sources)).Concat(C.PopulationAnchors.SelectMany(a => a.Sources))
            .Concat(C.Events.SelectMany(e => e.Sources));
        Assert.All(all, s => { Assert.StartsWith("https://", s.Url); Assert.Equal(Checked, s.Checked); });
    }

    [Fact]
    public void OpenQuestionsAreExactlyTheKnownOnes()
    {
        var open = C.Legislatures.SelectMany(l => l.OpenQuestions.Select(q => $"{l.Id}.{q}")).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "first_knesset.budget_approval", "provisional_state_council.budget_approval" }, open);
    }

    [Fact]
    public void EveryEventTextResolvesOnItsStartDate()
    {
        foreach (var e in C.Events)
        {
            Offices.Fill(C, e.Title, e.From);
            Offices.Fill(C, e.Text, e.From);
            foreach (var o in e.Options) Offices.Fill(C, o.Text, e.From);
        }
    }

    [Fact]
    public void SpotChecksAgainstVerifiedFacts()
    {
        Assert.Equal("ben_gurion", Offices.Holder(C, "defense_minister", new DateTime(1948, 6, 1))!.Id);
        Assert.Equal("dori", Offices.Holder(C, "chief_of_staff", new DateTime(1949, 11, 8))!.Id);
        Assert.Equal("yadin", Offices.Holder(C, "chief_of_staff", new DateTime(1949, 11, 9))!.Id);
        Assert.Equal("dov_yosef", Offices.Holder(C, "rationing_minister", new DateTime(1949, 6, 1))!.Id);
        Assert.Equal((30, 37), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));
        Assert.Equal((73, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 6, 1)));
        Assert.Equal(101828, C.Aliyah[0].Origins.Values.Sum());
        Assert.Equal(239954, C.Aliyah[1].Origins.Values.Sum());
        Assert.Equal(13, C.Actors.Count);
    }
}
