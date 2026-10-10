using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class EffectsTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    static GameState Fresh(DateTime date)
    {
        var s = new GameState { Date = date };
        EffectsSystem.Init(C, s);
        return s;
    }

    [Fact]
    public void InitSetsBasesAndPolicyDefaults()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        Assert.Equal(0.5, s.Indicators["morale"]);
        Assert.Equal(0.0, s.Policies["rationing"]);
    }

    [Fact]
    public void EffectMovesTowardTargetOverInertiaAndScalesByMinisterCompetence()
    {
        // finance_minister on 1948-06-01 is def_two, competence 4 -> multiplier 0.5 + 4/20 = 0.7
        var s = Fresh(new DateTime(1948, 6, 1));
        s.Policies["rationing"] = 1.0;
        var why = new List<Cause>();
        EffectsSystem.Apply(C, s, why);
        // target contribution 0.4 * 1.0 * 0.7 = 0.28, inertia 2 -> half way = 0.14
        Assert.Equal(0.54, s.Indicators["food_supply"], 6);
        EffectsSystem.Apply(C, s, why);
        Assert.Equal(0.61, s.Indicators["food_supply"], 6);
        var food = why.Where(w => w.System == "effects" && w.Target == "food_supply").ToList();
        Assert.Equal(2, food.Count);   // one truthful cause per turn the indicator moved
        Assert.Equal(0.14, food[0].Delta, 9);
        Assert.Equal(0.07, food[1].Delta, 9);
        Assert.All(food, w => Assert.Contains("Rations feed people", w.Message));
    }

    [Fact]
    public void MovingIndicatorGetsExactlyOneCauseWithItsRealChange()
    {
        // Two effects on food_supply: one cause for the indicator, naming both, with the real change.
        var model = Fixture.Files["model.yaml"].Replace("situations:",
            "  - id: rationing_food2\n    source: rationing\n    target: food_supply\n    k: 0.1\n    inertia: 1\n    name: More rations\nsituations:");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = model }));
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EffectsSystem.Init(c, s);
        s.Policies["rationing"] = 1.0;
        var before = s.Indicators["food_supply"];
        var why = new List<Cause>();
        EffectsSystem.Apply(c, s, why);
        Assert.Equal(0.64, s.Indicators["food_supply"], 9);   // 0.4 + 0.14 + 0.1
        var cause = Assert.Single(why, w => w.System == "effects" && w.Target == "food_supply");
        Assert.Equal(s.Indicators["food_supply"] - before, cause.Delta, 12);
        Assert.Equal("Food supply +0.240 (Rations feed people, More rations)", cause.Message);
    }

    [Fact]
    public void ClampedIndicatorWithMovingContributionHasNoCause()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        s.Policies["rationing"] = 5.0;   // morale contribution -1.0: morale clamps to 0
        EffectsSystem.Apply(C, s, new List<Cause>());
        Assert.Equal(0.0, s.Indicators["morale"]);
        s.Policies["rationing"] = 6.0;   // contribution moves to -1.2, morale stays at 0
        var why = new List<Cause>();
        EffectsSystem.Apply(C, s, why);
        Assert.Equal(0.0, s.Indicators["morale"]);
        Assert.Equal(-1.2, s.EffectContributions["rationing_morale"], 9);
        Assert.DoesNotContain(why, w => w.System == "effects" && w.Target == "morale");
    }

    [Fact]
    public void EffectWithoutOfficeHasNoMultiplierAndIndicatorsClampToUnitRange()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        s.Policies["rationing"] = 5.0; // morale target 0.5 + (-0.2 * 5) = -0.5 -> clamped to 0
        EffectsSystem.Apply(C, s, new List<Cause>());
        Assert.Equal(0.0, s.Indicators["morale"]);
    }

    [Fact]
    public void SituationUsesHysteresis()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        var why = new List<Cause>();
        s.Indicators["food_supply"] = 0.29; EffectsSystem.UpdateSituations(C, s, why);
        Assert.Contains("hunger", s.ActiveSituations);
        s.Indicators["food_supply"] = 0.40; EffectsSystem.UpdateSituations(C, s, why); // between thresholds: stays on
        Assert.Contains("hunger", s.ActiveSituations);
        s.Indicators["food_supply"] = 0.46; EffectsSystem.UpdateSituations(C, s, why);
        Assert.DoesNotContain("hunger", s.ActiveSituations);
        Assert.Contains(why, w => w.Target == "hunger");
    }

    [Fact]
    public void ApprovalIsMeanOfClampedVoterGroupSupport()
    {
        var s = Fresh(new DateTime(1948, 6, 1));
        Assert.Equal(0.5, EffectsSystem.Approval(C, s), 6);
    }
}
