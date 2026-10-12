using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>`successor` on an office (#57): read and validated at load; nothing reads it yet (Chapters 4).</summary>
public class OfficeSuccessorTests
{
    static Content Load(string extra)
    {
        var offices = Fixture.Files["offices.yaml"] + extra;
        return ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices }));
    }

    static void Refused(string extra, string expected) =>
        Assert.Contains(expected, Assert.Throws<ContentException>(() => Load(extra)).Message);

    [Fact]
    public void ASuccessorIsReadAndTheOldOfficeKeepsItsOwnWindow()
    {
        var c = Load("  - id: old_office\n    title: Old\n    until: 1950-01-01\n    successor: new_office\n  - id: new_office\n    title: New\n    from: 1949-06-01\n");
        Assert.Equal("new_office", c.Offices.Single(o => o.Id == "old_office").Successor);
        Assert.Equal("", c.Offices.Single(o => o.Id == "new_office").Successor);
    }

    [Fact] public void AnUnknownSuccessorIsRefused() => Refused("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: ghost\n", "offices.yaml: offices[o].successor: unknown office 'ghost'");
    [Fact] public void AnOfficeCannotSucceedItself() => Refused("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: o\n", "cannot succeed itself");
    [Fact] public void ASuccessorNeedsUntil() => Refused("  - id: o\n    title: O\n    successor: finance_minister\n", "needs until");
    [Fact] public void ASuccessorEstablishedAfterTheHandoverIsRefused() =>
        Refused("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: n\n  - id: n\n    title: N\n    from: 1951-01-01\n", "after o is abolished");
    [Fact] public void ASuccessorAbolishedBeforeTheHandoverIsRefused() =>
        Refused("  - id: o\n    title: O\n    until: 1951-01-01\n    successor: n\n  - id: n\n    title: N\n    until: 1950-01-01\n", "before it can take over");

    // The handover day is the old office's `until` (exclusive): the successor may start on it and must still exist on it.
    [Fact] public void ASuccessorEstablishedOnTheHandoverDayIsAccepted() =>
        Assert.Equal("n", Load("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: n\n  - id: n\n    title: N\n    from: 1950-01-01\n").Offices.Single(x => x.Id == "o").Successor);
    [Fact] public void ASuccessorAbolishedOnTheHandoverDayIsRefused() =>
        Refused("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: n\n  - id: n\n    title: N\n    until: 1950-01-01\n", "before it can take over");

    // Spec 4.2: budget lines and policies pass to the successor, so it must be able to own them.
    [Fact] public void ASuccessorWithSeveralHoldersIsRefused() =>
        Refused("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: n\n  - id: n\n    title: N\n    several_holders: true\n", "cannot take over budget lines");
    [Fact] public void AHistoricalOnlySuccessorIsRefused() =>
        Refused("  - id: o\n    title: O\n    until: 1950-01-01\n    successor: n\n  - id: n\n    title: N\n    historical_only: true\n", "is a historical title");
    [Fact] public void AHistoricalTitleMayPassToAnother() =>
        Assert.Equal("n", Load("  - id: o\n    title: O\n    until: 1950-01-01\n    historical_only: true\n    successor: n\n  - id: n\n    title: N\n    historical_only: true\n").Offices.Single(x => x.Id == "o").Successor);
}
