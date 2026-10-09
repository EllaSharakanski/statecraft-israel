using System;
using System.Collections.Generic;
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
        Assert.Contains(why, w => w.Target == "food_supply" && w.Message.Contains("rationing_food"));
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
