using System;
using Statecraft.Sim;

public class OfficesTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void ResolvesHolderOnDate()
    {
        Assert.Equal("pm_one", Offices.Holder(C, "defense_minister", new DateTime(1948, 12, 31))!.Id);
        Assert.Equal("def_two", Offices.Holder(C, "defense_minister", new DateTime(1949, 1, 1))!.Id); // 'to' is exclusive
    }

    [Fact]
    public void NoHolderBeforeFirstTerm() => Assert.Null(Offices.Holder(C, "prime_minister", new DateTime(1948, 1, 1)));

    [Fact]
    public void CurrentLegislatureAndGovernment()
    {
        Assert.Equal("council", Offices.Legislature(C, new DateTime(1948, 6, 1))!.Id);
        Assert.Equal("house", Offices.Legislature(C, new DateTime(1949, 2, 14))!.Id);
        Assert.Equal("gov2", Offices.Government(C, new DateTime(1949, 3, 10))!.Id);
    }

    [Fact]
    public void FillsOfficePlaceholders()
    {
        var text = Offices.Fill(C, "{defense_minister} briefs {prime_minister}.", new DateTime(1949, 6, 1));
        Assert.Equal("Def Two briefs Pm One.", text);
    }

    [Fact]
    public void UnfilledPlaceholderThrows() =>
        Assert.Throws<InvalidOperationException>(() => Offices.Fill(C, "{prime_minister}", new DateTime(1947, 1, 1)));

    [Fact]
    public void TwoPeopleCannotHoldOneOfficeOnOverlappingDates()
    {
        var bad = Fixture.Files["people.yaml"].Replace("to: 1949-01-01", "to: 1949-06-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new System.Collections.Generic.Dictionary<string, string> { ["people.yaml"] = bad })));
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("offices.defense_minister", ex.Field);
        Assert.Contains("pm_one", ex.Message);
        Assert.Contains("def_two", ex.Message);
    }
}
