using System.Collections.Generic;
using Statecraft.Sim;

public class AttentionTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void CostRisesWithSecurityLevel()
    {
        var s = new GameState { Rung = 1 };
        Assert.Equal(1.0, AttentionSystem.Cost(C, s, "set_policy"));
        s.Rung = 5;
        Assert.Equal(2.0, AttentionSystem.Cost(C, s, "set_policy")); // 1 * (1 + 0.25 * 4)
    }

    [Fact]
    public void SpendingStopsWhenAttentionRunsOut()
    {
        var s = new GameState { Rung = 5 };
        AttentionSystem.Refill(C, s);                 // 3
        Assert.Null(AttentionSystem.TrySpend(C, s, "set_policy"));   // 3 - 2 = 1
        Assert.Equal("Not enough attention (need 2.0, have 1.0)", AttentionSystem.TrySpend(C, s, "set_policy"));
        Assert.Equal(1.0, s.Attention);
    }

    [Fact]
    public void LoaderRejectsUnknownCommandCost()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("set_policy: 1,", "set_polcy: 1,");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("attention_cost.set_polcy", ex.Field);
    }

    static string LoadField(string from, string to)
    {
        var bad = Fixture.Files["rules.yaml"].Replace(from, to);
        Assert.NotEqual(Fixture.Files["rules.yaml"], bad);
        return Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad }))).Field;
    }

    [Fact]
    public void LoaderRejectsNegativeCost() =>
        Assert.Equal("attention_cost.set_policy", LoadField("set_policy: 1,", "set_policy: -1,"));

    [Fact]
    public void LoaderRejectsNaNStep() =>
        Assert.Equal("rung_attention_step", LoadField("rung_attention_step: 0.25", "rung_attention_step: .nan"));

    [Fact]
    public void LoaderRejectsNegativePerTurn() =>
        Assert.Equal("attention_per_turn", LoadField("attention_per_turn: 3", "attention_per_turn: -3"));
}
