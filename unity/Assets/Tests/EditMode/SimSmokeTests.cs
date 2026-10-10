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

    /// <summary>The golden playthroughs of docs/determinism.md, run on the Unity editor's Mono runtime. The constants are
    /// GoldenRun's, which sim/Statecraft.Sim.Tests/DeterminismTests.cs pins on .NET; rebuild the DLLs
    /// (tools/build_sim_for_unity.sh) after changing them.</summary>
    [Test]
    public void GoldenPlaythroughTo1950MatchesDotNet()
    {
        var s = GoldenRun.Play(ContentLoader.Load(Paths.Content), GoldenRun.Until1950);
        Assert.IsNull(s.GameOver);
        Assert.AreEqual(GoldenRun.Hash1950, s.Hash());
    }
}
