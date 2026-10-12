using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>The era golden helper (#292).</summary>
public class EraGoldenTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    [Fact]
    public void TheFoundingEraPlaysTheSameRunAsTheExistingGolden() =>
        Assert.Equal(GoldenRun.Hash1950, EraGolden.Play(Real, "founding", GoldenRun.Until1950).Hash());

    static Content TwoEras() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
    {
        ["eras.yaml"] = "eras:\n  - {id: first, name: One, from: 1948-05-15, to: 1948-12-01}\n  - {id: second, name: Two, from: 1948-12-01, to: 1949-06-01}\n",
    }));

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
