using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>Player-facing text (spec: player-facing text): Cause messages and refusal reasons use display names from
/// content and the one date format, never content ids or ISO dates (playtest 1, items 4 and 5).</summary>
public class PlayerTextTests
{
    static readonly Regex SnakeId = new Regex("[a-z]+_[a-z]+");
    static readonly Regex IsoDate = new Regex(@"\d{4}-\d{2}-\d{2}");

    static List<string> HistoricalCauseMessages()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var messages = new List<string>();
        for (int turn = 0; turn < 1000 && s.Date < GoldenRun.Until1952 && s.GameOver == null; turn++)
        {
            var r = Simulation.Step(c, s, HistoricalAI.Commands(c, s));
            messages.AddRange(r.Why.Select(w => w.Message));
            s = r.State;
        }
        return messages;
    }

    [Fact]
    public void NoCauseInTheHistoricalRunShowsAContentIdOrAnIsoDate()
    {
        var messages = HistoricalCauseMessages();
        Assert.NotEmpty(messages);
        var bad = messages.Where(m => SnakeId.IsMatch(m) || IsoDate.IsMatch(m)).Distinct().ToList();
        Assert.True(bad.Count == 0, "Cause text shows ids or ISO dates:\n" + string.Join("\n", bad.Take(40)));
    }

    [Fact]
    public void CausesNameRegionsEffectsAndEventsByTheirDisplayNames()
    {
        var messages = HistoricalCauseMessages();
        Assert.Contains(messages, m => m.EndsWith("immigrants arrived (the Americas and Oceania)", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.EndsWith("immigrants arrived (country of origin not recorded)", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.StartsWith("Morale ", StringComparison.Ordinal) && m.Contains("Homes lift morale"));
        Assert.Contains(messages, m => m.StartsWith("International standing +0.05 (Armistice with Egypt)", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.Contains("elected 25 January 1949"));
    }

    [Fact]
    public void DatesUseTheDayMonthYearFormat()
    {
        Assert.Equal("15 May 1948", PlayerText.Date(new DateTime(1948, 5, 15)));
        Assert.Equal("1 January 1950", PlayerText.Date(new DateTime(1950, 1, 1)));
        Assert.Equal("May 1948", PlayerText.Month(new DateTime(1948, 5, 15)));
    }

    [Fact]
    public void TheMapActionStartDateIsShownInTheDayMonthYearFormat()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var reason = MapActions.Check(c, s, MapActions.Housing, 0, 0);
        Assert.Equal("Transit camps and housing projects can be placed from 1 January 1950", reason);
    }

    /// <summary>Display names are required, non-empty text; an origin needs a named region (file and field named).</summary>
    [Theory]
    [InlineData("model.yaml", "    name: Morale\n", "", "indicators[morale].name")]
    [InlineData("model.yaml", "    name: Rationing\n", "    name: ' '\n", "policies[rationing].name")]
    [InlineData("model.yaml", "    name: Rations feed people\n", "", "effects[rationing_food].name")]
    [InlineData("model.yaml", "    name: Hunger\n", "", "situations[hunger].name")]
    [InlineData("aliyah.yaml", "  - { id: europe, name: Europe }\n", "  - { id: europe }\n", "regions[europe].name")]
    [InlineData("aliyah.yaml", "  - { id: europe, name: Europe }\n", "  - { id: europe, name: '' }\n", "regions[europe].name")]
    [InlineData("aliyah.yaml", "  - { id: asia, name: Asia }\n", "", "series[0].origins.asia")]
    [InlineData("aliyah.yaml", "  - { id: asia, name: Asia }\n", "  - { id: europe, name: Asia }\n", "regions[europe].id")]
    public void DisplayNamesAreRequired(string file, string from, string to, string field)
    {
        var ex = ValidationTests.Bad(file, from, to);
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }
}
