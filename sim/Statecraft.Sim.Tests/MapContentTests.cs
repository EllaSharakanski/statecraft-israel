using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/map/hexes.json and settlement_hexes.yaml as the sim loads them.</summary>
public class MapContentTests
{
    [Fact]
    public void TheFixtureMapLoads()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(9, c.Hexes.Count);
        Assert.False(c.HexAt(0, 0)!.Land);
        Assert.Equal("hills", c.HexAt(2, 1)!.Biome);
        Assert.Null(c.HexAt(9, 9));
        Assert.Equal(("town_a", 1, 0), (c.SettlementHexes[0].Id, c.SettlementHexes[0].Q, c.SettlementHexes[0].R));
    }

    [Fact]
    public void ASettlementOnASeaHexIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"].Replace("q: 1, r: 0", "q: 0, r: 0");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Equal("map/settlement_hexes.yaml", ex.File);
        Assert.Equal("settlements[town_a]", ex.Field);
        Assert.Contains("hex q=0 r=0 is not a land hex", ex.Message);
    }

    [Fact]
    public void ASettlementOffTheMapIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"].Replace("q: 1, r: 0", "q: 40, r: 0");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Contains("hex q=40 r=0 is not a land hex", ex.Message);
    }

    [Fact]
    public void ADuplicateHexIsALoadError()
    {
        var bad = Fixture.Files["map/hexes.json"].Replace(@"{""q"":4,""r"":0,", @"{""q"":3,""r"":0,");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = bad })));
        Assert.Equal("map/hexes.json", ex.File);
        Assert.Equal("hexes[q=3,r=0]", ex.Field);
    }

    [Fact]
    public void AnUnknownKeyInHexesJsonIsALoadError()
    {
        var bad = Fixture.Files["map/hexes.json"].Replace(@"""layout""", @"""layuot""");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = bad })));
        Assert.Equal("map/hexes.json", ex.File);
    }

    [Fact]
    public void TheRealMapHas1102HexesAndEvery1948Settlement()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(1102, c.Hexes.Count);
        Assert.Equal(870, c.Hexes.Count(h => h.Land));
        Assert.Equal(25, c.SettlementHexes.Count);
        Assert.Contains(c.SettlementHexes, s => s.Id == "tiberias");
    }
}
