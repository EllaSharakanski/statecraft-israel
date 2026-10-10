using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class TreasuryFlowsTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void FlowsMatchWhatATurnAdds()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EconomySystem.Init(C, s);
        s.Population["veterans"] = 1000; s.Mobilization = 0.5;
        var before = s.Treasury;
        var f = EconomySystem.Flows(C, s, 0.25);
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(before + f.Tax - f.Spending - f.Mobilization, s.Treasury, 12);
        Assert.Equal(C.Rules.TaxPerCapitaPerYear * 1000, EconomySystem.Flows(C, s, 1.0).Tax, 12);
    }
}
