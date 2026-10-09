using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Load-time validation of malformed content: empty keys, enum-like fields, number ranges and ids.</summary>
public class ValidationTests
{
    internal const string Event = @"
id: drought
title: Drought
text: '{finance_minister} reports a drought.'
anchor: soft
tags: [economy]
office: finance_minister
from: 1948-06-01
to: 1948-09-01
conditions:
  - indicator: food_supply
    max: 0.9
options:
  - id: ration
    text: Ration water
    historical: true
    effects:
      - target: food_supply
        delta: 0.1
  - id: wait
    text: Wait for rain
status: draft
";

    /// <summary>Loads the fixture with one text replacement in <paramref name="file"/> (an events/ file when it
    /// starts with "events/") and returns the ContentException it must raise.</summary>
    internal static ContentException Bad(string file, string from, string to)
    {
        var overrides = new Dictionary<string, string>();
        var events = new Dictionary<string, string> { ["drought.yaml"] = Event };
        string original = file.StartsWith("events/") ? Event : Fixture.Files[file];
        Assert.Contains(from, original);
        var bad = original.Replace(from, to);
        if (file.StartsWith("events/")) events["drought.yaml"] = bad; else overrides[file] = bad;
        return Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(overrides, events)));
    }

    [Fact]
    public void FixtureWithTheTestEventLoads() =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event }));

    // I1: a key written with no value deserialises to null.
    [Theory]
    [InlineData("events/drought.yaml", "tags: [economy]", "tags:", "events[drought].tags", "must be a list")]
    [InlineData("events/drought.yaml", "options:\n  - id: ration\n    text: Ration water\n    historical: true\n    effects:\n      - target: food_supply\n        delta: 0.1\n  - id: wait\n    text: Wait for rain\n", "options:\n", "events[drought].options", "must be a list")]
    [InlineData("events/drought.yaml", "    effects:\n      - target: food_supply\n        delta: 0.1\n", "    effects:\n", "events[drought].options[ration].effects", "must be a list")]
    [InlineData("people.yaml", "    offices:\n      - office: defense_minister\n        from: 1949-01-01\n      - office: finance_minister\n        from: 1948-05-14\n", "    offices:\n", "people[def_two].offices", "must be a list")]
    [InlineData("legislatures.yaml", "seats: { alpha: 4, beta: 6 }", "seats:", "legislatures[house].seats", "must be a map")]
    [InlineData("rules.yaml", "budget_start: { finance_minister: 1.0 }", "budget_start:", "budget_start", "must be a map")]
    [InlineData("parties.yaml", "parties:\n  - id: alpha\n    name: Alpha\n  - id: beta\n    name: Beta\n", "parties:\n", "parties", "must be a list")]
    public void EmptyListOrMapIsAContentError(string file, string from, string to, string field, string message)
    {
        var ex = Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.EndsWith(message, ex.Message);
    }

    // I2: enum-like fields.
    [Theory]
    [InlineData("events/drought.yaml", "anchor: soft", "anchor: hrad", "events[drought].anchor")]
    [InlineData("events/drought.yaml", "anchor: soft", "anchor: Hard", "events[drought].anchor")]
    [InlineData("events/drought.yaml", "status: draft", "status: done", "events[drought].status")]
    [InlineData("people.yaml", "    loyalty: 9\n", "    loyalty: 9\n    status: checked\n", "people[pm_one].status")]
    [InlineData("legislatures.yaml", "    size: 10\n", "    size: 10\n    status: final\n", "legislatures[house].status")]
    [InlineData("governments.yaml", "    coalition: [alpha]\n", "    coalition: [alpha]\n    status: Fact-Checked\n", "governments[gov1].status")]
    [InlineData("aliyah.yaml", "    origins: { europe: 230, asia: 0 }\n", "    origins: { europe: 230, asia: 0 }\n    status: ok\n", "series[0].status")]
    [InlineData("population.yaml", "anchors: []", "anchors:\n  - date: 1948-11-08\n    jews: 700\n    status: ''\n", "anchors[0].status")]
    public void EnumLikeFieldOutsideItsValuesIsAContentError(string file, string from, string to, string field)
    {
        var ex = Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    [Theory]
    [InlineData("hard")]
    [InlineData("soft")]
    public void BothAnchorsLoad(string anchor) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event.Replace("anchor: soft", "anchor: " + anchor) }));

    [Theory]
    [InlineData("draft")]
    [InlineData("fact-checked")]
    [InlineData("historian-reviewed")]
    [InlineData("sensitivity-reviewed")]
    public void EveryStatusLoads(string status) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = Event.Replace("status: draft", "status: " + status) }));
}
