using System;
using System.Collections.Generic;
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

    /// <summary>Any change to a golden hash is a deliberate rules or content change. The pinned values are in
    /// golden/manifest.txt (see GoldenManifest and docs/determinism.md).</summary>
    static string GoldenHash => GoldenManifest.Expected("hash1950");

    /// <summary>Real content, seed 1948, Historical mode, the reference player until the date reaches 1950-01-01.</summary>
    public static GameState GoldenPlaythrough(Content c) => GoldenRun.Play(c, GoldenRun.Until1950);

    [Fact]
    public void GoldenPlaythroughHashIsFixed()
    {
        var s = GoldenPlaythrough(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.Null(s.GameOver);
        Assert.Equal((new DateTime(1950, 1, 15), 20), (s.Date, s.Turn));   // the first monthly turn on or after 1 January 1950
        GoldenSnapshot.Check("hash1950", s, GoldenHash);
    }

    /// <summary>Same rule for the second golden run, which covers the M3 content range (to 1952-01-01).</summary>
    static string GoldenHash1952 => GoldenManifest.Expected("hash1952");

    [Fact]
    public void GoldenPlaythroughTo1952HashIsFixed()
    {
        var s = GoldenRun.Play(ContentLoader.Load(RealContentTests.ContentRoot()), GoldenRun.Until1952);
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);   // the game's last turn ends on 15 January 1952 (R-LOOP-004)
        Assert.Equal((new DateTime(1952, 1, 15), 44), (s.Date, s.Turn));
        GoldenSnapshot.Check("hash1952", s, GoldenHash1952);
    }

    /// <summary>mvp-end (spec 2026-10-12 section 3): the reference player over the whole GDD game, to its end. With the end
    /// on 15 January 1952 it is the same run as hash1952, which stops at the first turn on or after 1 January 1952; both
    /// names are kept (spec section 3 step 6), so a later end date moves only this one.</summary>
    // R-LOOP-004
    [Fact]
    public void TheWholeGameToItsEndIsFixed()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = GoldenRun.PlayToEnd(c);
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.Equal((c.End, 44), (s.Date, s.Turn));
        var expected = GoldenManifest.UpdateRequested ? GoldenManifest.Read().GetValueOrDefault("mvp-end", "") : GoldenManifest.Expected("mvp-end");
        GoldenSnapshot.Check("mvp-end", s, expected);
        Assert.Equal(GoldenRun.Play(c, GoldenRun.Until1952).Hash(), s.Hash());
    }

    /// <summary>The library constants Unity players compare against are the values these tests pin.</summary>
    [Fact]
    public void LibraryGoldenConstantsMatchThisTest()
    {
        Assert.Equal(GoldenRun.Hash1950, GoldenHash);
        Assert.Equal(GoldenRun.Hash1952, GoldenHash1952);
    }
}
