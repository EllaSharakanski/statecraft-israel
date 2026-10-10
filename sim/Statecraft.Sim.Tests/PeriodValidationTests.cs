using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Load-time checks that keep "who governed / which body sat / who held office on this date" unambiguous:
/// no overlapping legislatures or governments, no term in an abolished office, no term after its holder's death.</summary>
public class PeriodValidationTests
{
    [Fact]
    public void OverlappingLegislaturesAreALoadError()
    {
        var ex = ValidationTests.Bad("legislatures.yaml", "to: 1949-02-14", "to: 1949-03-01");
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.Equal("legislatures[house].from", ex.Field);
        Assert.Contains("overlaps council (1948-05-14 to 1949-03-01)", ex.Message);
    }

    [Fact]
    public void AnOpenEndedLegislatureFollowedByAnotherIsAnOverlap()
    {
        var ex = ValidationTests.Bad("legislatures.yaml", "to: 1949-02-14", "");
        Assert.Equal("legislatures[house].from", ex.Field);
        Assert.Contains("overlaps council (1948-05-14 to open)", ex.Message);
    }

    [Fact]
    public void OverlappingGovernmentsAreALoadError()
    {
        var ex = ValidationTests.Bad("governments.yaml", "to: 1949-03-10", "to: 1949-04-01");
        Assert.Equal("governments.yaml", ex.File);
        Assert.Equal("governments[gov2].from", ex.Field);
        Assert.Contains("overlaps gov1", ex.Message);
    }

    [Fact]
    public void AGapBetweenGovernmentsIsAllowed()
    {
        var gov = Fixture.Files["governments.yaml"].Replace("to: 1949-03-10", "to: 1949-03-01");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["governments.yaml"] = gov }));
        Assert.Null(Offices.Government(c, new DateTime(1949, 3, 5)));
    }

    [Fact]
    public void ATermRunningPastTheOfficesAbolitionIsALoadError()
    {
        var ex = ValidationTests.Bad("offices.yaml", "title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.finance_minister.to", ex.Field);
        Assert.Contains("must be on or before 1949-01-01, when the office was abolished", ex.Message);
    }

    [Fact]
    public void AnAbolishedOfficeWhoseTermsEndInTimeLoads()
    {
        var offices = Fixture.Files["offices.yaml"].Replace("title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        var people = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1948-05-14\n        to: 1949-01-01");
        var rules = Fixture.Files["rules.yaml"].Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { prime_minister: 1.0 }");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices, ["people.yaml"] = people, ["rules.yaml"] = rules }));
        Assert.Equal(new DateTime(1949, 1, 1), c.Offices.Find(o => o.Id == "finance_minister")!.Until);
        Assert.Null(Offices.Holder(c, "finance_minister", new DateTime(1949, 1, 1)));
    }

    [Fact]
    public void ABudgetLineForAnAbolishedOfficeIsALoadError()
    {
        var offices = Fixture.Files["offices.yaml"].Replace("title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        var people = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1948-05-14\n        to: 1949-01-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices, ["people.yaml"] = people })));
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal("budget_start.finance_minister", ex.Field);
        Assert.Contains("is abolished on 1949-01-01: give its budget line to the office that took over", ex.Message);
    }

    [Fact]
    public void ATermRunningPastTheHoldersDeathIsALoadError()
    {
        var ex = ValidationTests.Bad("people.yaml", "    name: Def Two\n", "    name: Def Two\n    died: 1949-06-01\n");
        Assert.Equal("people[def_two].offices.defense_minister.to", ex.Field);
        Assert.Contains("must be on or before 1949-06-01, the date of death", ex.Message);
    }
}
