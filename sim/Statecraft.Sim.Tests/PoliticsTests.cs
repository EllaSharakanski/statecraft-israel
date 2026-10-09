using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class PoliticsTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void CoalitionSeatsFollowGovernmentAndLegislatureOnDate()
    {
        Assert.Equal((3, 5), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));   // gov1 [alpha] in council
        Assert.Equal((4, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 3, 1)));  // gov1 [alpha] in house
        Assert.Equal((10, 10), PoliticsSystem.CoalitionSeats(C, new DateTime(1949, 3, 10))); // gov2 [alpha,beta]
        Assert.False(PoliticsSystem.HasMajority(C, new DateTime(1949, 3, 1)));
        Assert.True(PoliticsSystem.HasMajority(C, new DateTime(1948, 6, 1)));
    }

    [Fact]
    public void BudgetVoteRequirementIsPerLegislature()
    {
        Assert.False(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1948, 6, 1)));
        Assert.True(PoliticsSystem.BudgetNeedsVote(C, new DateTime(1949, 6, 1)));
    }

    [Fact]
    public void VoteFailsWithoutMajorityAndPassesWithStableMajority()
    {
        var rng = new Rng(1);
        var s = new GameState { Date = new DateTime(1949, 3, 1), CoalitionStability = 1.0 };
        Assert.False(PoliticsSystem.Vote(C, s, ref rng));
        s.Date = new DateTime(1949, 6, 1);
        Assert.True(PoliticsSystem.Vote(C, s, ref rng));
    }

    [Fact]
    public void StabilityMovesTowardApprovalAndCollapseEndsGame()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1), CoalitionStability = 1.0 };
        EffectsSystem.Init(C, s); // approval 0.5
        var why = new List<Cause>();
        PoliticsSystem.UpdateStability(C, s, 0.25, why);
        Assert.Equal(0.75, s.CoalitionStability, 6);
        s.CoalitionStability = 0.1; s.Indicators["morale"] = 0.0;
        PoliticsSystem.UpdateStability(C, s, 0.25, why);
        Assert.Equal("coalition_collapse", s.GameOver);
    }

    [Fact]
    public void ReportsNewLegislatureAndGovernment()
    {
        var why = new List<Cause>();
        PoliticsSystem.ReportChanges(C, new DateTime(1949, 2, 1), new DateTime(1949, 5, 1), why);
        Assert.Contains(why, w => w.Target == "house");
        Assert.Contains(why, w => w.Target == "gov2");
    }
}
