using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class EconomyTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    static GameState Fresh()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EconomySystem.Init(C, s);
        s.Population["veterans"] = 1000;
        return s;
    }

    [Fact]
    public void InitCopiesStartValues()
    {
        var s = Fresh();
        Assert.Equal(100, s.Treasury); Assert.Equal(50, s.FxReserves); Assert.Equal(1.0, s.Budget["finance_minister"]);
    }

    [Fact]
    public void QuarterOfTaxSpendingAndTrade()
    {
        var s = Fresh();
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(100 + 0.1 - 0.125, s.Treasury, 9);   // tax 0.0004*1000/4, spend 0.0005*1000/4
        Assert.Equal(50 + 5 - 0.025, s.FxReserves, 9);   // exports 20/4, imports 0.0001*1000/4
    }

    [Fact]
    public void MobilizationCostsTreasuryAndDeficitBecomesDebt()
    {
        var s = Fresh(); s.Treasury = 1; s.Mobilization = 1.0;
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(0, s.Treasury);
        Assert.Equal(10 + 0.025 - 1, s.Debt, 9);
    }

    [Fact]
    public void FxCrisisHasHysteresis()
    {
        var s = Fresh(); var why = new List<Cause>();
        s.FxReserves = -10; EconomySystem.Apply(C, s, 0.25, why);
        Assert.Contains("fx_crisis", s.ActiveSituations);
        s.FxReserves = 5; EconomySystem.Apply(C, s, 0.25, why);   // 5 + 4.975 < 12.5 (quarter of start): still on
        Assert.Contains("fx_crisis", s.ActiveSituations);
        s.FxReserves = 20; EconomySystem.Apply(C, s, 0.25, why);
        Assert.DoesNotContain("fx_crisis", s.ActiveSituations);
    }

    [Theory]
    [InlineData("finance_minister", 1.0, null)]
    [InlineData("finance_minister", 0.9, "Budget shares must sum to 1")]
    [InlineData("chief_rabbi", 1.0, "Unknown office 'chief_rabbi'")]
    [InlineData("finance_minister", -1.0, "Budget shares must sum to 1")]
    public void AllocateValidates(string office, double share, string? error)
    {
        var s = Fresh();
        Assert.Equal(error, EconomySystem.Allocate(C, s, new Dictionary<string, double> { [office] = share }));
    }

    [Fact]
    public void BudgetShareScalesEffects()
    {
        var s = Fresh();
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(s, "finance_minister"));   // equal split is neutral
        s.Budget = new Dictionary<string, double> { ["finance_minister"] = 0.0, ["defense_minister"] = 1.0 };
        Assert.Equal(0.5, EffectsSystem.BudgetFactor(s, "finance_minister"));
        Assert.Equal(1.5, EffectsSystem.BudgetFactor(s, "defense_minister"));
    }
}

public class EconomyNegativeShareTests
{
    [Fact]
    public void NegativeShareRejectedEvenIfSumIsOne()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = new GameState();
        EconomySystem.Init(c, s);
        var err = EconomySystem.Allocate(c, s, new System.Collections.Generic.Dictionary<string, double> { ["finance_minister"] = 1.5, ["defense_minister"] = -0.5 });
        Assert.Equal("Budget shares cannot be negative", err);
    }
}
