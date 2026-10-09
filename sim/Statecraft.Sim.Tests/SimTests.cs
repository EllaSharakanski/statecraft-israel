using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class SimTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());
    static readonly IReadOnlyList<Command> None = new Command[0];

    [Fact]
    public void NewGameStartsAtRulesStartAtWar()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Equal(new DateTime(1948, 5, 15), s.Date);
        Assert.Equal(5, s.Rung);
        Assert.Equal(3, s.Attention);
        Assert.Equal(1000, s.TotalPopulation);
    }

    [Fact]
    public void OpenModeIsNotAvailableYet() =>
        Assert.Throws<NotSupportedException>(() => Simulation.NewGame(C, 1, Mode.Open));

    [Fact]
    public void StepAdvancesByRungTurnLengthAndExplainsItself()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, None);
        Assert.Equal(new DateTime(1948, 5, 22), r.State.Date);   // rung 5 = weekly
        Assert.Equal(1, r.State.Turn);
        Assert.Contains(r.Why, w => w.System == "population");
        Assert.Contains(r.Why, w => w.System == "economy");
    }

    [Fact]
    public void StepDoesNotMutateItsInput()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var before = s0.Hash();
        Simulation.Step(C, s0, new Command[] { new SetMobilization(1.0) });
        Assert.Equal(before, s0.Hash());
    }

    [Fact]
    public void InvalidCommandsAreRejectedWithReasonsAndAttentionRefunded()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("curfew", 1), new SetMobilization(2) });
        Assert.Equal(new[] { "Unknown policy 'curfew'", "Mobilization must be between 0 and 1" }, r.Rejected.Select(x => x.Reason));
    }

    [Fact]
    public void AttentionLimitsCommandsPerTurn()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);   // attention 3, rung 5 -> each command costs 2
        var r = Simulation.Step(C, s0, new Command[] { new SetMobilization(0.9), new SetMobilization(1.0) });
        Assert.Single(r.Rejected);
        Assert.StartsWith("Not enough attention", r.Rejected[0].Reason);
    }

    [Fact]
    public void SecurityOrderChangesRung()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-2) });
        Assert.Equal(3, r.State.Rung);
        Assert.Equal(new DateTime(1948, 8, 15), r.State.Date);  // now quarterly
    }

    [Fact]
    public void GameOverRejectsEverything()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical); s0.GameOver = "coalition_collapse";
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-1) });
        Assert.Equal("The game is over: coalition_collapse", r.Rejected[0].Reason);
        Assert.Equal(s0.Hash(), r.State.Hash());
    }

    [Fact]
    public void SameSeedAndCommandsGiveIdenticalHashes()
    {
        string Play(ulong seed)
        {
            var s = Simulation.NewGame(C, seed, Mode.Historical);
            for (int i = 0; i < 20; i++)
                s = Simulation.Step(C, s, i == 3 ? new Command[] { new SecurityOrder(-3) } : HistoricalAI.Commands(C, s)).State;
            return s.Hash();
        }
        Assert.Equal(Play(7), Play(7));
        Assert.NotEqual(Play(7), Play(8));
    }

    // ---- Beyond the brief: post-H1 rules and robustness ----

    const string Shortage = @"
id: shortage
title: Bread lines
text: '{finance_minister} reports shortages.'
office: finance_minister
from: 1948-05-15
options:
  - id: ration
    text: Ration food
    recommended: true
    effects:
      - target: food_supply
        delta: 0.2
  - id: import
    text: Buy abroad
    historical: true
    effects:
      - target: fx_reserves
        delta: -10
";

    const string Raid = @"
id: raid
title: Border raid
text: 'A raid on the border.'
office: defense_minister
from: 1948-05-15
options:
  - id: hold
    text: Hold the line
    effects:
      - target: morale
        delta: -0.05
  - id: strike
    text: Strike back
    effects:
      - target: morale
        delta: 0.05
";

    static Content WithEvents() =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["shortage.yaml"] = Shortage, ["raid.yaml"] = Raid }));

    sealed class Bogus : Command { public override string Kind => "bogus"; }

    [Fact]
    public void UnknownCommandKindIsRejectedNotThrown()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new Bogus(), new SetMobilization(1.0) });
        Assert.Equal(new[] { "Unknown command 'bogus'" }, r.Rejected.Select(x => x.Reason));
        Assert.Equal(1.0, r.State.Mobilization);   // the bogus command cost no attention
    }

    [Fact]
    public void PolicyLevelMustBeFinite()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("rationing", double.NaN) });
        Assert.Equal("Policy level must be between 0 and 1", r.Rejected.Single().Reason);
        Assert.Equal(0.0, r.State.Policies["rationing"]);
    }

    [Fact]
    public void PolicyPassesWithAMajority()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);   // council: alpha holds 3 of 5
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("rationing", 0.5) });
        Assert.Empty(r.Rejected);
        Assert.Equal(0.5, r.State.Policies["rationing"]);
        Assert.Contains(r.Why, w => w.System == "policy" && w.Message == "rationing set to 0.50");
    }

    [Fact]
    public void PartialBudgetIsRejectedWithEconomyReason()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["defense_minister"] = 1.0 }) });
        Assert.Equal("Budget shares must cover exactly: finance_minister", r.Rejected.Single().Reason);
        Assert.Equal(1.0, r.State.Budget["finance_minister"]);
    }

    [Fact]
    public void MalformedBudgetIsRejectedBeforeAnyVoteIsCast()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1949, 4, 1);   // House needs to approve budgets; gov2 holds 10/10
        s0.CoalitionStability = 0.3;           // so a vote would draw from the RNG
        var r = Simulation.Step(C, s0, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 0.7 }) });
        Assert.Equal("Budget shares must sum to 1", r.Rejected.Single().Reason);
        Assert.Equal(s0.RngState, r.State.RngState);
    }

    [Fact]
    public void BudgetVoteFailsWithoutAMajority()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1949, 3, 1);   // House sits, gov1 (alpha, 4 of 10) has no majority
        var r = Simulation.Step(C, s0, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 1.0 }) });
        Assert.Equal("House voted the budget down", r.Rejected.Single().Reason);
    }

    [Fact]
    public void SecurityOrderDoesNotOverflow()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(int.MaxValue) });
        Assert.Empty(r.Rejected);
        Assert.Equal(5, r.State.Rung);
    }

    [Fact]
    public void HistoricalAIChoosesTheHistoricalOption()
    {
        var c = WithEvents();
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Assert.Equal(new[] { "raid", "shortage" }, s.PendingEvents);
        var cmds = HistoricalAI.Commands(c, s).Cast<ChooseEventOption>().ToList();
        Assert.Equal(new[] { ("raid", "hold"), ("shortage", "import") }, cmds.Select(x => (x.EventId, x.OptionId)));
    }

    [Fact]
    public void UnaffordableEventChoicesAreDelegated()
    {
        var c = WithEvents();
        var s0 = Simulation.NewGame(c, 42, Mode.Historical);
        var r = Simulation.Step(c, s0, HistoricalAI.Commands(c, s0));   // 3 attention, each choice costs 2
        Assert.StartsWith("Not enough attention", r.Rejected.Single().Reason);
        Assert.Empty(r.State.PendingEvents);
        Assert.Contains(r.Why, w => w.System == "events" && w.Message.StartsWith("chosen: "));
        Assert.Contains(r.Why, w => w.System == "events" && w.Message.StartsWith("delegated to Def Two"));
    }

    [Fact]
    public void DeterministicWithEventsAndDelegation()
    {
        var c = WithEvents();
        (string hash, string why) Play(ulong seed)
        {
            var s = Simulation.NewGame(c, seed, Mode.Historical);
            var lines = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                var r = Simulation.Step(c, s, i % 2 == 0 ? None : HistoricalAI.Commands(c, s));
                lines.AddRange(r.Why.Select(w => w.ToString()));
                s = r.State;
            }
            return (s.Hash(), string.Join("\n", lines));
        }
        Assert.Equal(Play(11), Play(11));
        Assert.NotEqual(Play(11).hash, Play(12).hash);
    }
}
