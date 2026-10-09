using System;
using Statecraft.Sim;

/// <summary>Fixed reference values for the generator and for one full historical playthrough. See docs/determinism.md.</summary>
public class DeterminismTests
{
    /// <summary>The first three outputs of Vigna's reference splitmix64.c (https://prng.di.unimi.it/splitmix64.c)
    /// for each seed, taken from the reference C code, not from this implementation.</summary>
    [Theory]
    [InlineData(0UL, 0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL)]
    [InlineData(1234567UL, 6457827717110365317UL, 3203168211198807973UL, 9817491932198370423UL)]
    public void SplitMix64MatchesTheReferenceVectors(ulong seed, ulong first, ulong second, ulong third)
    {
        var rng = new Rng(seed);
        Assert.Equal(first, rng.NextULong());
        Assert.Equal(second, rng.NextULong());
        Assert.Equal(third, rng.NextULong());
    }

    /// <summary>Any change to this value is a deliberate rules or content change: update it here and in
    /// docs/determinism.md (and the Unity edit-mode SimSmokeTests) in the same commit.</summary>
    public const string GoldenHash = "eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9";

    /// <summary>Real content, seed 1948, Historical mode, HistoricalAI until the date reaches 1950-01-01.</summary>
    public static GameState GoldenPlaythrough(Content c)
    {
        var s = Simulation.NewGame(c, 1948UL, Mode.Historical);
        for (int turn = 0; turn < 1000 && s.Date < new DateTime(1950, 1, 1) && s.GameOver == null; turn++)
            s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
        return s;
    }

    [Fact]
    public void GoldenPlaythroughHashIsFixed()
    {
        var s = GoldenPlaythrough(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.Null(s.GameOver);
        Assert.True(s.Date >= new DateTime(1950, 1, 1));
        Assert.Equal(GoldenHash, s.Hash());
    }
}
