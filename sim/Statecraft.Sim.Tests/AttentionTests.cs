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
}
