using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Sim;

/// <summary>The era golden helper (#292).</summary>
public class EraGoldenTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    [Fact]
    public void TheFoundingEraPlaysTheSameRunAsTheExistingGolden() =>
        Assert.Equal(GoldenRun.Hash1950, EraGolden.Play(Real, "founding", GoldenRun.Until1950).Hash());

    [Fact]
    public void CheckOnTheFoundingEraMatchesThePinnedHash1950AndItsSnapshot() =>
        EraGolden.Check("hash1950", Real, "founding", GoldenRun.Until1950);   // the manifest line and golden/hash1950.txt

    static Content TwoEras(string? secondRules = null)
    {
        var dir = Fixture.Write(new Dictionary<string, string>
        {
            ["eras.yaml"] = "eras:\n  - {id: first, name: One, from: 1948-05-15, to: 1948-12-01}\n  - {id: second, name: Two, from: 1948-12-01, to: 1949-06-01}\n",
        });
        if (secondRules != null)
        {
            Directory.CreateDirectory(Path.Combine(dir, "rules"));
            File.WriteAllText(Path.Combine(dir, "rules", "second.yaml"), secondRules);
        }
        return ContentLoader.Load(dir);
    }

    [Theory]
    [InlineData(1948, 11, 1)]    // the fixture's quarterly turn from 12 September would end on the boundary, 1 December
    [InlineData(1948, 12, 1)]    // the era's end
    public void TheRunStaysInsideTheEraSoTheNextErasRulesDoNotMoveIt(int y, int m, int d)
    {
        var plain = EraGolden.Play(TwoEras(), "first", new DateTime(y, m, d));
        var overridden = EraGolden.Play(TwoEras("attention_per_turn: 7\n"), "first", new DateTime(y, m, d));
        Assert.True(plain.Date < new DateTime(1948, 12, 1), $"the run ended on {plain.Date:yyyy-MM-dd}, in the next era");
        Assert.Equal(plain.Hash(), overridden.Hash());
    }

    [Fact]
    public void TheSeedIsUsed() =>
        Assert.NotEqual(EraGolden.Play(TwoEras(), "first", new DateTime(1948, 11, 1)).RngState, EraGolden.Play(TwoEras(), "first", new DateTime(1948, 11, 1), seed: 7).RngState);

    [Fact]
    public void TheFirstEraOfAFixturePlays()
    {
        var s = EraGolden.Play(TwoEras(), "first", new DateTime(1948, 8, 1));
        Assert.True(s.Date >= new DateTime(1948, 8, 1));
    }

    [Fact]
    public void ALaterEraSaysItsStartValuesAreNotReadYet()
    {
        var e = Assert.Throws<NotSupportedException>(() => EraGolden.Play(TwoEras(), "second", new DateTime(1949, 3, 1)));
        Assert.Contains("start values", e.Message);
        Assert.Contains("1948-12-01", e.Message);
    }

    [Fact]
    public void AnUnknownEraAndADateOutsideTheEraAreRefused()
    {
        Assert.Contains("known: first, second", Assert.Throws<ArgumentException>(() => EraGolden.Play(TwoEras(), "third", new DateTime(1948, 8, 1))).Message);
        Assert.Contains("outside era", Assert.Throws<ArgumentException>(() => EraGolden.Play(TwoEras(), "first", new DateTime(1949, 1, 1))).Message);
    }
}
