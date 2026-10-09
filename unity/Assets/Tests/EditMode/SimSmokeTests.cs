using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Sim;

public class SimSmokeTests
{
    [Test]
    public void ContentLoads_AndStepIsDeterministicInsideUnity()
    {
        var content = ContentLoader.Load(Paths.Content);
        var a = Simulation.NewGame(content, 42UL, Mode.Historical);
        var b = Simulation.NewGame(content, 42UL, Mode.Historical);
        var ra = Simulation.Step(content, a, new List<Command>());
        var rb = Simulation.Step(content, b, new List<Command>());
        Assert.AreEqual(ra.State.Hash(), rb.State.Hash());
        Assert.IsNotEmpty(ra.State.Hash());
    }

    /// <summary>The same playthrough and constant as sim/Statecraft.Sim.Tests/DeterminismTests.cs (see docs/determinism.md).
    /// It proves Unity's runtime computes, bit for bit, what .NET computes. Update all three places together.</summary>
    public const string GoldenHash = "eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9";

    [Test]
    public void GoldenPlaythroughMatchesDotNet()
    {
        var c = ContentLoader.Load(Paths.Content);
        var s = Simulation.NewGame(c, 1948UL, Mode.Historical);
        for (int turn = 0; turn < 1000 && s.Date < new System.DateTime(1950, 1, 1) && s.GameOver == null; turn++)
            s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
        Assert.IsNull(s.GameOver);
        Assert.GreaterOrEqual(s.Date, new System.DateTime(1950, 1, 1));
        Assert.AreEqual(GoldenHash, s.Hash());
    }
}
