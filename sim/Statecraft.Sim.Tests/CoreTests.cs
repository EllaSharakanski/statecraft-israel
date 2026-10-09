using System;
using Statecraft.Sim;

public class CoreTests
{
    [Theory]
    [InlineData(TurnLength.Quarter, "1948-08-15")]
    [InlineData(TurnLength.Month, "1948-06-15")]
    [InlineData(TurnLength.Week, "1948-05-22")]
    public void AdvanceMovesDateByTurnLength(TurnLength len, string expected)
    {
        var start = new DateTime(1948, 5, 15);
        Assert.Equal(DateTime.Parse(expected), SimTime.Advance(start, len));
    }

    [Fact]
    public void TurnFractionOfYear()
    {
        Assert.Equal(0.25, SimTime.YearFraction(TurnLength.Quarter));
        Assert.Equal(1.0 / 12, SimTime.YearFraction(TurnLength.Month), 10);
        Assert.Equal(7.0 / 365, SimTime.YearFraction(TurnLength.Week), 10);
    }

    [Fact]
    public void RngIsDeterministicForSameSeed()
    {
        var a = new Rng(42); var b = new Rng(42);
        for (int i = 0; i < 100; i++) Assert.Equal(a.NextDouble(), b.NextDouble());
    }

    [Fact]
    public void RngValuesAreInUnitInterval()
    {
        var r = new Rng(7);
        for (int i = 0; i < 1000; i++) { var v = r.NextDouble(); Assert.True(v >= 0.0 && v < 1.0, $"NextDouble out of [0,1): {v}"); }
    }

    [Fact]
    public void HashIsStableAndSensitiveToState()
    {
        var s1 = new GameState { Date = new DateTime(1948, 5, 15), RngState = 1 };
        s1.Indicators["morale"] = 0.5;
        var s2 = s1.Clone();
        Assert.Equal(s1.Hash(), s2.Hash());
        s2.Indicators["morale"] = 0.6;
        Assert.NotEqual(s1.Hash(), s2.Hash());
        Assert.Equal(0.5, s1.Indicators["morale"]); // clone is deep
    }
    [Fact]
    public void HashTreatsNegativeZeroAsZero()
    {
        var a = new GameState { Date = new DateTime(1948, 5, 15), Treasury = 0.0 };
        a.Indicators["morale"] = 0.0;
        var b = a.Clone(); b.Treasury = -0.0; b.Indicators["morale"] = -0.0;
        Assert.Equal(a.Hash(), b.Hash());
    }

    [Fact]
    public void HashSeesTheLastBitOfADouble()
    {
        var a = new GameState { Date = new DateTime(1948, 5, 15), Treasury = 0.1 };
        var b = a.Clone(); b.Treasury = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(0.1) + 1);
        Assert.NotEqual(a.Treasury, b.Treasury);
        Assert.NotEqual(a.Hash(), b.Hash());
    }
    [Fact]
    public void CloneIsolatesIndicatorShiftsAndHashSeesThem()
    {
        var a = new GameState { Date = new DateTime(1948, 5, 15) };
        a.IndicatorShifts["morale"] = 0.0;
        var b = a.Clone();
        b.IndicatorShifts["morale"] = 0.1;
        Assert.Equal(0.0, a.IndicatorShifts["morale"]);
        Assert.NotEqual(a.Hash(), b.Hash());
    }
}

public class InputParseTests
{
    [Theory]
    [InlineData("0.9", 0.9)] [InlineData("1e0", 1.0)] [InlineData("-2", -2.0)]
    public void ParsesInvariantDoubles(string text, double expected)
    {
        Assert.True(Statecraft.Sim.InputParse.TryDouble(text, out var v)); Assert.Equal(expected, v);
    }

    [Theory]
    [InlineData("abc")] [InlineData("")] [InlineData("0,9")] [InlineData("NaN")] [InlineData("Infinity")]
    public void RejectsBadDoubles(string text) => Assert.False(Statecraft.Sim.InputParse.TryDouble(text, out _));

    [Fact]
    public void IntAndULongRejectJunkAndNegativeSeed()
    {
        Assert.True(Statecraft.Sim.InputParse.TryInt("-1", out var i)); Assert.Equal(-1, i);
        Assert.False(Statecraft.Sim.InputParse.TryInt("x", out _));
        Assert.True(Statecraft.Sim.InputParse.TryULong("1948", out var u)); Assert.Equal(1948UL, u);
        Assert.False(Statecraft.Sim.InputParse.TryULong("-5", out _));
        Assert.False(Statecraft.Sim.InputParse.TryULong("x", out _));
    }

    [Fact]
    public void ParsingIsCultureIndependent()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.True(Statecraft.Sim.InputParse.TryDouble("0.9", out var v)); Assert.Equal(0.9, v);
            Assert.False(Statecraft.Sim.InputParse.TryDouble("0,9", out _));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }
}
