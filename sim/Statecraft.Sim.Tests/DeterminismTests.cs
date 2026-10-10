using System;
using Statecraft.Sim;

/// <summary>Fixed reference values for the generator and for the golden historical playthroughs. See docs/determinism.md.</summary>
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

    /// <summary>Any change to this value is a deliberate rules or content change: update it here, in
    /// GoldenRun.Hash1950 and in docs/determinism.md in the same commit.</summary>
    public const string GoldenHash = "d46ce5ffd442f6b47f515fe2497b1db3e262217b5d01be543b822e5d41a6a8cd";

    /// <summary>Real content, seed 1948, Historical mode, HistoricalAI until the date reaches 1950-01-01.</summary>
    public static GameState GoldenPlaythrough(Content c) => GoldenRun.Play(c, GoldenRun.Until1950);

    [Fact]
    public void GoldenPlaythroughHashIsFixed()
    {
        var s = GoldenPlaythrough(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.Null(s.GameOver);
        Assert.True(s.Date >= GoldenRun.Until1950);
        Assert.Equal(GoldenHash, s.Hash());
    }

    /// <summary>The library constant Unity players compare against is the value this test pins.</summary>
    [Fact]
    public void LibraryGoldenConstantMatchesThisTest() => Assert.Equal(GoldenHash, GoldenRun.Hash1950);
}
