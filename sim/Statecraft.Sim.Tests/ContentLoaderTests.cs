using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Sim;

public class ContentLoaderTests
{
    [Fact]
    public void LoadsValidFixture()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(2, c.Parties.Count);
        Assert.Equal(2, c.People.Count);
        Assert.Equal(10, c.Legislatures[1].Size);
        Assert.Equal(2, c.Effects.Count);
        Assert.Equal(5, c.Rules.StartRung);
    }

    [Fact]
    public void UnknownPartyInSeatsNamesFileAndField()
    {
        var bad = Fixture.Files["legislatures.yaml"].Replace("beta: 6", "gamma: 6");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["legislatures.yaml"] = bad })));
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.Equal("legislatures[house].seats.gamma", ex.Field);
    }

    [Fact]
    public void SeatsMustSumToSize()
    {
        var bad = Fixture.Files["legislatures.yaml"].Replace("size: 10", "size: 11");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["legislatures.yaml"] = bad })));
        Assert.Equal("legislatures[house].size", ex.Field);
    }

    [Fact]
    public void UnknownYamlPropertyIsAnError()
    {
        var bad = Fixture.Files["parties.yaml"].Replace("name: Beta", "nmae: Beta");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["parties.yaml"] = bad })));
        Assert.Equal("parties.yaml", ex.File);
    }

    [Fact]
    public void EffectTargetMustBeAnIndicator()
    {
        var bad = Fixture.Files["model.yaml"].Replace("target: morale", "target: moral");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = bad })));
        Assert.Equal("effects[rationing_morale].target", ex.Field);
    }

    [Fact]
    public void PersonOfficeMustExist()
    {
        var bad = Fixture.Files["people.yaml"].Replace("office: finance_minister", "office: treasury_boss");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people[def_two].offices.treasury_boss", ex.Field);
    }

    [Fact]
    public void OfficeTermEndingBeforeItStartsIsAnError()
    {
        var bad = Fixture.Files["people.yaml"].Replace("to: 1949-01-01", "to: 1947-01-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[pm_one].offices.defense_minister", ex.Field);
    }

    [Theory]
    [InlineData("offices.yaml", "id: finance_minister", "id: defense_minister", "offices[defense_minister].id")]
    [InlineData("parties.yaml", "id: beta", "id: alpha", "parties[alpha].id")]
    [InlineData("people.yaml", "id: def_two", "id: pm_one", "people[pm_one].id")]
    [InlineData("legislatures.yaml", "id: house", "id: council", "legislatures[council].id")]
    [InlineData("governments.yaml", "id: gov2", "id: gov1", "governments[gov1].id")]
    [InlineData("model.yaml", "id: food_supply", "id: morale", "indicators[morale].id")]
    [InlineData("model.yaml", "id: rationing\n    default", "id: morale\n    default", "policies[morale].id")]
    public void DuplicateIdIsAnError(string file, string original, string duplicate, string field)
    {
        var bad = Fixture.Files[file].Replace(original, duplicate);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { [file] = bad })));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }
    [Fact]
    public void UnreadableFileIsAContentError()
    {
        if (OperatingSystem.IsWindows()) return;   // unix permissions only
        var dir = Fixture.Write();
        var path = Path.Combine(dir, "parties.yaml");
        File.SetUnixFileMode(path, UnixFileMode.None);
        try
        {
            var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
            Assert.Equal("parties.yaml", ex.File);
            Assert.Equal("(file)", ex.Field);
        }
        finally { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
    }

    [Fact]
    public void LockedFileIsAContentError()
    {
        var dir = Fixture.Write();
        using (new FileStream(Path.Combine(dir, "parties.yaml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
            Assert.Equal("parties.yaml", ex.File);
            Assert.Equal("(file)", ex.Field);
        }
    }

    [Fact]
    public void RulesStartIsRequired()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("start: 1948-05-15\n", "");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal("start", ex.Field);
        Assert.EndsWith("required", ex.Message);
    }

    [Theory]
    [InlineData("office:")]
    [InlineData("office: ''")]
    public void OfficeTermNeedsAnOffice(string replacement)
    {
        var bad = Fixture.Files["people.yaml"].Replace("office: finance_minister", replacement);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices", ex.Field);
    }
}
