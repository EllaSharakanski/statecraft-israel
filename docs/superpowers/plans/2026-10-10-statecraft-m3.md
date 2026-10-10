# Statecraft: Israel: M3 implementation plan (Playable 1948–51)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task by task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A browser-playable 1948–51 build: HUD, Cabinet (with Appoint), Budget, Event, Defence and "Why did this happen?" screens over the M2 map, with sourced content to 31 December 1951, a calibrated economy, saves, and the golden hash proven in WebGL and macOS IL2CPP players.

**Architecture:**
- **Sim (`sim/`):** new pure-C# systems for appointments (`Cabinet`), map actions (`MapActions`) and saves (`SaveGame`, `GameSession`); content validation for overlapping legislatures/governments, abolished offices and deaths; a calibrated economy with a foreign-currency consequence; reference playthroughs in `GoldenRun`.
- **Content (`content/`):** the Second and Third Governments, the Second Knesset, Weizmann's re-election, CBS immigration and population for 1950–51, eleven sourced events, and the settlement-hex table written by `tools/settlement_hexes.py`.
- **Unity (`unity/`):** a boot scene (`Map.unity`) whose `GameBoot` reads the content (fetched through `UnityWebRequest` + `Awaitable` on WebGL), builds the map, and hands off to `GameRoot`, which runs UI Toolkit presenters over one `SimBridge`. Presenters set element text directly on each change (synchronous, testable in edit mode); no rules live in Unity.
- **Platform:** a custom WebGL template (full-window canvas, `autoSyncPersistentDataPath`), a headless build script (`-compression disabled|gzip|brotli`), and `tools/web_check.py`, which serves a build on `http://localhost` and reads the browser console from headless Chrome for Testing.

**Tech stack:** .NET SDK 10.0.401, YamlDotNet 18.1.0, xUnit; Unity 6000.3.25f1 (URP 17.3.0, UI Toolkit, Input System 1.20.0, test-framework 1.6.0, IL2CPP for WebGL and macOS); Python through uv 0.12.5 (numpy 2.5.3, pyyaml 6.0.3, pytest 9.1.1); Chrome for Testing from the Playwright cache (`~/Library/Caches/ms-playwright/chromium-1228`).

**Spec:** `docs/superpowers/specs/2026-10-09-statecraft-israel-design.md`. **Binding scope rulings:** `docs/superpowers/plans/m3-scope-rulings.md`. **Research inputs:** `research_notes/M3 content 1950-51/facts.md` (history), `research_notes/M3 Unity tech/tech.md` (Unity facts).

**How this plan was verified (2026-10-10, after review amendments round 1):** every task below was run in a scratch clone of this branch (base `5643d4e`), on the branch its track uses, and every code block, total and hash below was taken from that run (observed, not computed).

| Part | Branch it ran on | Observed |
|---|---|---|
| Track A, Tasks 1–9 | `track-a` from the base | sim 318, 320, 320, 321, 335, 352, 358, 377, 391 after each task; Unity 81 (Task 1) and 82 (Task 5); tools 96 (Task 7) |
| Track B, Task 10 | `track-b1` from the base, without Tracks A and C | Unity 88 |
| Track C, Tasks 11–13 | `track-c1` from the base, without Tracks A and B | Unity 86, 90, 93 |
| Merge point 1 | A + B + C | Unity 101, sim 391, tools 96 |
| Track B, Tasks 14–21 | from merge point 1 | Unity 106, 109, 112, 114, 115, 117, 123, 128 |
| Track C, Task 22 | from merge point 1 | Unity 108, tools 98 |
| Merge point 2 | B + C | Unity 135, sim 391, tools 98 |
| Tasks 23–24 | on merge point 2 | Unity 138, 141 |
| Players, at the end | the Task 24 state | WebGL (Disabled) in headless Chrome: `GOLDEN OK 1950 404b1811…`, `GOLDEN OK 1952 36640b59…`, `SMOKE OK 5 turns`, `PROFILE done`; Brotli build (22.3 MB, 36 files) passes the same check over `http://localhost`; macOS IL2CPP `-golden` exits 0 with both `GOLDEN OK` lines |

## Controller questions

Each question has a recommended resolution, and the plan proceeds with it.

1. **Golden check in players: a URL flag on the boot scene, read by Chrome for Testing, instead of a separate "GOLDEN" scene read by Playwright.** Ruling 5 says "a `GOLDEN OK <hash>` scene read by headless Chrome/Playwright". The Playwright npm package is not installed (only its browser cache is), and the tech notes (§4.3) allow "a separate build is not needed if the main boot reads a URL flag". *Recommendation (used):* `GameBoot` runs `GoldenCheck` when the page URL has the query parameter `golden` (WebGL) or the player gets `-golden` (macOS), and `tools/web_check.py` reads the console from the cached Chrome for Testing in `--headless=new` mode (the cached `chrome-headless-shell` reports "Your browser does not support WebGL" for Unity and has no IndexedDB). Cost if wrong: swap the reader for Playwright later; the console lines stay the same.
2. **Resolved by the controller's C2 ruling and research Addendum 2:** `rationing_minister` is "Minister responsible for rationing", with no abolition date: Dov Yosef (Minister of Rationing and Supply) 26 Apr 1949 to 1 Nov 1950; Pinhas Lavon (Agriculture took most rationing and supply functions; he.wiki read directly, Weitz 2014 and Schiffman & Goldstein by search summary; Trade and Industry under Geri took some supply departments) 1 Nov 1950 to 8 Oct 1951; vacant from 8 Oct 1951 (no source names the minister; Deferred). Task 1 (already implemented on `m3-sim`) is unchanged: these changes live in Task 2, and Task 6's abolished-office test now uses the Ministry of War Victims.
3. **Amendment A4 narrows ruling 5.** Ruling 5 says "no camera input while the pointer is over UI". A4 (binding) keeps keyboard pan and orbit working over the UI and blocks only the wheel and the drag. The plan follows A4.
4. **Maimon's resignation date conflicts (4 Jan 1951 vs after the February 1951 vote).** Notes §1d mark it [C] without ranking the sources. *Used:* the footnoted he.wiki account, with the government's resignation (14 Feb 1951) as the date; the other date is in a YAML comment (ruling 3).
5. **Resolved by the research addendum (7391877):** the Third Government had no Immigration or War Victims portfolio, so both offices end on 1951-10-08 (`until`, amendment C1), and the Immigration budget line moves to the Prime Minister (amendment C3); Weizmann's first term starts 17 Feb 1949 (election 16 Feb, oath 17 Feb on two of three sources).

## Global Constraints

- **Sim target:** `netstandard2.1`, `LangVersion 9.0`, `Nullable enable`, warnings as errors (`sim/Directory.Build.props`). No Unity references in `sim/`. Entry point `Statecraft.Sim.Simulation`; never name a type after its namespace (so `GameBoot`, not `Boot`, in `Statecraft.Game.Boot`).
- **Unity:** editor `6000.3.25f1`, `$UNITY` as set in `docs/dev-setup.md`; run `tools/build_sim_for_unity.sh` before any Unity command after sim changes. New Unity C# files start with `#nullable enable`.
- **M0–M2 Unity conventions:** materials are editor-made assets (no runtime `Shader.Find`); runtime-created objects are owned by `MapRuntimeAssets` or destroyed by their owner; the **new Input System only** (no `UnityEngine.Input`, no uGUI `EventSystem`); edit-mode tests in batch mode; capture tools never rewrite `Map.unity` (only `MapCapture.MakeScene` does).
- **UI tests:** a `UIDocument` gives the tree a real panel; buttons are pressed with `NavigationSubmitEvent`, never `ClickEvent` (tech notes §1.6). Shared harness: `unity/Assets/Tests/EditMode/UiTestKit.cs`.
- **Content:** YAML with snake_case keys; unknown keys and broken references are load errors naming file and field. **Every real-world fact carries `sources:` (URL + `checked: 2026-10-10`).** One value per fact; where sources conflict, use the one the research notes rank higher and record the other in a comment; never invent (ruling 3).
- **Narrative:** Israeli perspective and terminology; real names; no quotes attributed to real people; no treasury or foreign-currency reward on options of events tagged `holocaust`, `terror`, `massacre` or `war-casualties`.
- **Determinism:** golden hashes live in `GoldenRun.cs`, `DeterminismTests.cs` and `docs/determinism.md`; any change is deliberate, made in all three in one commit, with the reason in the history table and the commit message.
- **Strings:** UI text lives outside code: static labels in UXML, formatted strings in `unity/Assets/UI/strings.en.yaml` (`UiText`).
- **Look (ruling 2):** dark navy panels, thin gold borders, EB Garamond serif; top resource bar with value and change; event card on the right; info panel bottom-left (no tile yields); minimap bottom-right. M2 markers stay.
- **Commits:** stage explicit paths (never `git add -A`) and include the `.meta` files Unity creates; logs are `unity-*.log` (git-ignored).
- **Web:** WebGL saves go to the fixed folder `/idbfs/statecraft-saves` (not `persistentDataPath`); compression Disabled for local checks, Brotli for itch.io; the itch.io upload is an **OWNER STEP**, never automated.
- **Commits:** end every commit message with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

These are the inputs a player will meet that the spec implies but no feature test would otherwise exercise. Each is pinned by a test in the task that owns the code.

1. **A save made with other content** (the game was updated between two itch.io uploads). Expected: the save still loads, with a warning; a save that cannot be read is set aside and a new game starts, never a crash. *Tests: Task 9 `OtherContentLoadsWithAWarning`, Task 21 `AnUnreadableAutosaveIsSetAsideAndANewGameStarts`.*
2. **Clicking or scrolling on a panel that sits over the map.** Expected: the UI gets the click and the wheel; the camera does not zoom or drag and the map does not select a hex; keys still pan and orbit; the empty middle of the screen still reaches the map. *Tests: Task 10 `PickingFindsButtonsButNotTheTransparentRoot`, Task 11 `PointerInputIsIgnoredOverTheUi` and `KeysStillPanAndOrbitWhileThePointerIsOverTheUi` (amendment A4).*
3. **An appointee when the government changes, and in event text.** Expected: event text names whoever holds the office now; a new government arrives with its own (historical) ministers and the player's appointments end, with a cause line. *Tests: Task 6 `EventTextNamesTheAppointee`, `AppointmentsEndWhenANewGovernmentTakesOffice`.*
4. **Orders the player cannot afford, or that the sim refuses.** Expected: refused before End Turn with the attention arithmetic, or after it with the sim's reason shown on screen; nothing else changes. *Tests: Task 14 `QueuedOrdersSpendAttentionUpFront`, `RefusedOrdersAreShownWithTheirReason`.*
5. **A map action on the wrong hex or at the wrong time** (sea, off the map, far from any settlement, before 1950, an occupied hex). Expected: the button is disabled and the reason is shown; state is untouched. *Tests: Task 8 `IllegalPlacementsAreRejectedWithAReason`, `NothingBeforeTheStartDate`, `AnOccupiedHexIsRejected`; Task 20 `BeforeTheMaabarotTheButtonsSayWhy`.*

## Parallel tracks

Branch every track from `m3` at the commit that adds this plan. Each track works in its own worktree (`superpowers:using-git-worktrees`). The file sets of tracks that run at the same time are disjoint; the controller merges at the merge points and runs every suite there.

| Phase | Track | Tasks | Worktree | Files it owns |
|---|---|---|---|---|
| 1 | **A: sim and content** | 1–9, in order | `m3-sim` | `sim/**`, `content/**`, `tools/settlement_hexes.py`, `tools/tests/test_settlement_hexes.py`, `tools/README.md`, `docs/determinism.md`, `unity/Assets/Tests/EditMode/SimSmokeTests.cs` |
| 1 | **B: UI shell** | 10 | `m3-ui` | `unity/Assets/UI/**`, `unity/Assets/Scripts/UI/{UiText,UiPaths,UiPointer}.cs`, `unity/Assets/Editor/UiAssets.cs`, `unity/Assets/Tests/EditMode/{UiTestKit,UiFoundationTests,UiLayoutTests}.cs`, `unity/Assets/link.xml` |
| 1 | **C: platform** | 11, 12, 13, in order | `m3-web` | `unity/Assets/InputSystem_Actions.inputactions`, both `.asmdef` files, `unity/Assets/Scripts/Map/{CameraRig,CameraController,MapScene}.cs`, `unity/Assets/Scripts/Boot/**`, `unity/Assets/Editor/{ContentSync,MapCapture,ProjectSetup,Build}.cs`, `unity/Assets/Map.unity`, `unity/Assets/WebGLTemplates/**`, `unity/Packages/**`, `unity/ProjectSettings/**`, `.gitignore`, their tests |
| — | **Merge point 1** | merge A, B, C | `m3` | Expected: Unity edit mode `total="101" passed="101" failed="0"`; sim `Passed:   391`; tools `96 passed` |
| 2 | **B: screens** | 14–21, in order | `m3-ui` (rebased on merge point 1) | `unity/Assets/Scripts/UI/**`, `unity/Assets/Scripts/SaveStore.cs`, `unity/Assets/Scripts/Map/{HexPicker,GeoFrame,MapBuilder,MapScene,PlacementLayer,MapPointer}.cs`, `unity/Assets/Editor/PortraitImport.cs`, `unity/Assets/UI/**`, their tests |
| 2 | **C: players** | 22 | `m3-web` (rebased on merge point 1) | `unity/Assets/Scripts/Boot/{GameBoot,GoldenCheck,UrlFlags}.cs`, `unity/Assets/link.xml`, `tools/web_check.py`, `tools/tests/test_web_check.py`, `tools/README.md`, `docs/determinism.md`, `GoldenCheckTests.cs` |
| — | **Merge point 2** | merge B and C | `m3` | Expected: Unity `total="135" passed="135" failed="0"`; tools `98 passed`; sim 391 |
| 3 | **D: integration** | 23, 24, in order | `m3` | `unity/Assets/Scripts/UI/GameRoot.cs`, `unity/Assets/Editor/MapCapture.cs`, `unity/Assets/Map.unity`, `unity/Assets/Scripts/Map/PixelErrorProfile.cs`, `docs/m3-release.md`, `docs/dev-setup.md`, their tests |

Notes for the controller:
- Track B's Phase 2 needs `GameSession` (Task 9), `Cabinet` (Task 6) and `MapActions` (Task 8), and Task 20 needs Task 11's Input System reference and Task 12's `MapScene.Build(MapInputs)`; that is why Phase 2 waits for merge point 1.
- Track A's long chain (9 tasks) is the critical path. Tracks B and C in Phase 1 are short; their implementers can be reused for review.
- Unity locks a project while it runs: two worktrees are two projects, so their Unity runs can overlap.
- Expected Unity totals in each task are the observed totals on that track's branch (Phase 1 branches start at the base's 81 edit-mode tests).
- **Staging (amendment R5):** tasks stage explicit paths, never `git add -A`; build and check logs are named `unity-*.log`, which `.gitignore` already covers.
- **Builds and settings (amendment R1):** a step that builds a player later runs `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset` to drop Unity's build-time rewrites. `git checkout --` restores from the index, so a task that changes real settings stages them first (only Task 13 does).

## File structure (new files)

```
sim/Statecraft.Sim/        GoldenRun.cs  Cabinet.cs  MapActions.cs  SaveGame.cs  GameSession.cs
sim/Statecraft.Sim.Tests/  PeriodValidationTests.cs  EconomyCalibrationTests.cs  EconomyM3Tests.cs  CabinetTests.cs
                           MapContentTests.cs  MapActionTests.cs  SaveGameTests.cs
content/events/            11 events (1950-51)        content/map/settlement_hexes.yaml (generated)
tools/                     settlement_hexes.py  web_check.py  (+ tests)
unity/Assets/Scripts/Boot/ ContentManifest.cs  GameInputs.cs  GameBoot.cs  GoldenCheck.cs  UrlFlags.cs
unity/Assets/Scripts/Map/  CameraController.cs  HexPicker.cs  PlacementLayer.cs  MapPointer.cs  PixelErrorProfile.cs
unity/Assets/Scripts/UI/   UiText  UiPaths  UiPointer  SimBridge  HudModel  HudPresenter  ToastPresenter  ScreenNav
                           PortraitCatalog  EventCardPresenter  DefencePresenter  BudgetPresenter  WhyPresenter
                           CabinetPresenter  InfoPanelPresenter  MinimapPresenter  GameStart  NewGamePresenter  GameRoot
unity/Assets/Scripts/      SaveStore.cs
unity/Assets/Editor/       UiAssets.cs  ContentSync.cs  Build.cs  PortraitImport.cs
unity/Assets/UI/           Game.uxml  Statecraft.uss  StatecraftTheme.tss  strings.en.yaml  Fonts/  Portraits/
unity/Assets/WebGLTemplates/Statecraft/index.html
```

---
## Phase 1, Track A: simulation and content (Tasks 1–9)

Each task here runs `dotnet test` from the repository root. Tasks 1 and 5 also run the Unity edit-mode tests, because they change `SimSmokeTests.cs`.

### Task 1: GoldenRun, and period, office and death validation

*Track A.* Ruling 3 moves the Deferred row "validation that legislature periods do not overlap, and that government periods do not overlap" into M3, before the Second Knesset and two more governments arrive. The same task adds two fields the 1950–51 content needs: an office's abolition date (`until`, for the Ministry of Rationing and Supply) and a person's death date (`died`, for David Remez, who died in office). A budget line for an abolished office would silently stop mattering, so the loader refuses one (plan amendment C3; Task 2 moves the Immigration line). It also moves the golden playthrough into the sim library (`GoldenRun`), so Unity players (Task 22) and the editor tests compare against the constants .NET pins (tech notes §4.3, blocker 6).

**Files:**
- Create: `sim/Statecraft.Sim.Tests/PeriodValidationTests.cs`, `sim/Statecraft.Sim/GoldenRun.cs`
- Modify: `docs/determinism.md`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim/Content.cs`, `sim/Statecraft.Sim/ContentLoader.cs`, `unity/Assets/Tests/EditMode/SimSmokeTests.cs`

**Interfaces:**
- Consumes: `Simulation.NewGame(Content, ulong, Mode)`, `Simulation.Step(Content, GameState, IReadOnlyList<Command>)`, `HistoricalAI.Commands(Content, GameState)`, `ContentLoader.Load(string)`, test helper `ValidationTests.Bad(string file, string from, string to)` (returns the `ContentException`), `Fixture.Files`, `Fixture.Write(overrides, events)`.
- Produces:
  - `public static class GoldenRun { public const ulong Seed = 1948; public const string Hash1950; public static readonly DateTime Until1950; public static GameState Play(Content c, DateTime until); }` (Task 5 adds `Hash1952`, `Until1952`).
  - `Office.Until : DateTime?` (YAML `until`, exclusive like a term's `to`) and `Person.Died : DateTime?` (YAML `died`).
  - Load errors: `legislatures.yaml` / `governments.yaml`, field `legislatures[<id>].from` / `governments[<id>].from`, message `overlaps <id> (<from> to <to|open>)`; `people.yaml`, field `people[<id>].offices.<office>.to`, message `must be on or before <yyyy-MM-dd>, when the office was abolished` or `..., the date of death`; `rules.yaml`, field `budget_start.<office>`, message `office '<office>' is abolished on <yyyy-MM-dd>: give its budget line to the office that took over`.

- [ ] **Step 1: Write the failing tests**

Create `sim/Statecraft.Sim.Tests/PeriodValidationTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Statecraft.Sim;

/// <summary>Load-time checks that keep "who governed / which body sat / who held office on this date" unambiguous:
/// no overlapping legislatures or governments, no term in an abolished office, no term after its holder's death.</summary>
public class PeriodValidationTests
{
    [Fact]
    public void OverlappingLegislaturesAreALoadError()
    {
        var ex = ValidationTests.Bad("legislatures.yaml", "to: 1949-02-14", "to: 1949-03-01");
        Assert.Equal("legislatures.yaml", ex.File);
        Assert.Equal("legislatures[house].from", ex.Field);
        Assert.Contains("overlaps council (1948-05-14 to 1949-03-01)", ex.Message);
    }

    [Fact]
    public void AnOpenEndedLegislatureFollowedByAnotherIsAnOverlap()
    {
        var ex = ValidationTests.Bad("legislatures.yaml", "to: 1949-02-14", "");
        Assert.Equal("legislatures[house].from", ex.Field);
        Assert.Contains("overlaps council (1948-05-14 to open)", ex.Message);
    }

    [Fact]
    public void OverlappingGovernmentsAreALoadError()
    {
        var ex = ValidationTests.Bad("governments.yaml", "to: 1949-03-10", "to: 1949-04-01");
        Assert.Equal("governments.yaml", ex.File);
        Assert.Equal("governments[gov2].from", ex.Field);
        Assert.Contains("overlaps gov1", ex.Message);
    }

    [Fact]
    public void AGapBetweenGovernmentsIsAllowed()
    {
        var gov = Fixture.Files["governments.yaml"].Replace("to: 1949-03-10", "to: 1949-03-01");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["governments.yaml"] = gov }));
        Assert.Null(Offices.Government(c, new DateTime(1949, 3, 5)));
    }

    [Fact]
    public void ATermRunningPastTheOfficesAbolitionIsALoadError()
    {
        var ex = ValidationTests.Bad("offices.yaml", "title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        Assert.Equal("people.yaml", ex.File);
        Assert.Equal("people[def_two].offices.finance_minister.to", ex.Field);
        Assert.Contains("must be on or before 1949-01-01, when the office was abolished", ex.Message);
    }

    [Fact]
    public void AnAbolishedOfficeWhoseTermsEndInTimeLoads()
    {
        var offices = Fixture.Files["offices.yaml"].Replace("title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        var people = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1948-05-14\n        to: 1949-01-01");
        var rules = Fixture.Files["rules.yaml"].Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { prime_minister: 1.0 }");
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices, ["people.yaml"] = people, ["rules.yaml"] = rules }));
        Assert.Equal(new DateTime(1949, 1, 1), c.Offices.Find(o => o.Id == "finance_minister")!.Until);
        Assert.Null(Offices.Holder(c, "finance_minister", new DateTime(1949, 1, 1)));
    }

    [Fact]
    public void ABudgetLineForAnAbolishedOfficeIsALoadError()
    {
        var offices = Fixture.Files["offices.yaml"].Replace("title: Minister of Finance", "title: Minister of Finance\n    until: 1949-01-01");
        var people = Fixture.Files["people.yaml"].Replace("      - office: finance_minister\n        from: 1948-05-14", "      - office: finance_minister\n        from: 1948-05-14\n        to: 1949-01-01");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["offices.yaml"] = offices, ["people.yaml"] = people })));
        Assert.Equal("rules.yaml", ex.File);
        Assert.Equal("budget_start.finance_minister", ex.Field);
        Assert.Contains("is abolished on 1949-01-01: give its budget line to the office that took over", ex.Message);
    }

    [Fact]
    public void ATermRunningPastTheHoldersDeathIsALoadError()
    {
        var ex = ValidationTests.Bad("people.yaml", "    name: Def Two\n", "    name: Def Two\n    died: 1949-06-01\n");
        Assert.Equal("people[def_two].offices.defense_minister.to", ex.Field);
        Assert.Contains("must be on or before 1949-06-01, the date of death", ex.Message);
    }
}
```

Replace the whole of `sim/Statecraft.Sim.Tests/DeterminismTests.cs` with:

```csharp
using System;
using Statecraft.Sim;

/// <summary>Fixed reference values for the generator and for the golden historical playthroughs. See docs/determinism.md.</summary>
public class DeterminismTests
{
    /// <summary>The first three outputs of Vigna's reference splitmix64.c (https://prng.di.unimi.it/splitmix64.c)
    /// for each seed, taken from the reference C code, not from this implementation.</summary>
    [Theory]
    [InlineData(0UL, 0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL)]
    [InlineData(1234567UL, 6457827717110365317UL, 3203168211198807973UL, 9817491932198370423UL)]
    public void SplitMix64MatchesTheReferenceVectors(ulong seed, ulong first, ulong second, ulong third)
    {
        var rng = new Rng(seed);
        Assert.Equal(first, rng.NextULong());
        Assert.Equal(second, rng.NextULong());
        Assert.Equal(third, rng.NextULong());
    }

    /// <summary>Any change to this value is a deliberate rules or content change: update it here, in
    /// GoldenRun.Hash1950 and in docs/determinism.md in the same commit.</summary>
    public const string GoldenHash = "eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9";

    /// <summary>Real content, seed 1948, Historical mode, HistoricalAI until the date reaches 1950-01-01.</summary>
    public static GameState GoldenPlaythrough(Content c) => GoldenRun.Play(c, GoldenRun.Until1950);

    [Fact]
    public void GoldenPlaythroughHashIsFixed()
    {
        var s = GoldenPlaythrough(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.Null(s.GameOver);
        Assert.True(s.Date >= GoldenRun.Until1950);
        Assert.Equal(GoldenHash, s.Hash());
    }

    /// <summary>The library constant Unity players compare against is the value this test pins.</summary>
    [Fact]
    public void LibraryGoldenConstantMatchesThisTest() => Assert.Equal(GoldenHash, GoldenRun.Hash1950);
}
```

Replace the whole of `unity/Assets/Tests/EditMode/SimSmokeTests.cs` with:

```csharp
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
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: the test project does not build: `error CS0103: The name 'GoldenRun' does not exist in the current context` (DeterminismTests.cs) and `error CS1061: 'Office' does not contain a definition for 'Until'` (PeriodValidationTests.cs).

- [ ] **Step 3: Implement**

Create `sim/Statecraft.Sim/GoldenRun.cs`:

```csharp
using System;

namespace Statecraft.Sim
{
    /// <summary>The reference playthroughs that pin determinism (docs/determinism.md): real content, seed 1948,
    /// Historical mode, HistoricalAI until the date reaches the given day. The constants live in the sim library so
    /// Unity players (Golden check) and the editor tests compare against the same values .NET asserts.</summary>
    public static class GoldenRun
    {
        public const ulong Seed = 1948;

        /// <summary>Final hash of Play(content, Until1950). Changing it is a deliberate rules or content change:
        /// update it, DeterminismTests and docs/determinism.md in the same commit and say why in the message.</summary>
        public const string Hash1950 = "eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9";

        public static readonly DateTime Until1950 = new DateTime(1950, 1, 1);

        /// <summary>Steps with HistoricalAI until Date >= <paramref name="until"/>, the game ends, or 1000 turns pass.</summary>
        public static GameState Play(Content c, DateTime until)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, Seed, Mode.Historical);
            for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
                s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
            return s;
        }
    }
}
```

In `sim/Statecraft.Sim/Content.cs`:

1. Replace

```csharp

    public sealed class Office { public string Id { get; set; } = ""; public string Title { get; set; } = ""; }

```

   with

```csharp

    public sealed class Office
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        /// <summary>Day the office was abolished (exclusive end, like a term's `to`); null while it still exists.</summary>
        public DateTime? Until { get; set; }
    }

```

2. Replace

```csharp
        public bool? Living { get; set; }
        public int Competence { get; set; }
```

   with

```csharp
        public bool? Living { get; set; }
        /// <summary>Date of death, when it falls inside the content range: every term must end by then.</summary>
        public DateTime? Died { get; set; }
        public int Competence { get; set; }
```

In `sim/Statecraft.Sim/ContentLoader.cs`:

1. Replace

```csharp

        static void Validate(Content c)
```

   with

```csharp

        /// <summary>Only one legislature sits, and one government holds office, on any date: "which body sat on this
        /// date" must never be ambiguous. Periods are [from, to); sorted by from, each must end on or before the next
        /// one starts (an open-ended period can only be the last).</summary>
        static void NoOverlap(string file, string list, IEnumerable<(string id, DateTime from, DateTime? to)> periods)
        {
            var sorted = periods.OrderBy(p => p.from).ThenBy(p => p.id, StringComparer.Ordinal).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                var (prev, next) = (sorted[i - 1], sorted[i]);
                if (!prev.to.HasValue || prev.to.Value > next.from)
                    throw new ContentException(file, $"{list}[{next.id}].from", FormattableString.Invariant(
                        $"overlaps {prev.id} ({prev.from:yyyy-MM-dd} to {(prev.to.HasValue ? prev.to.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "open")})"));
            }
        }

        static void Validate(Content c)
```

2. Replace

```csharp
            }
            foreach (var p in c.People)
```

   with

```csharp
            }
            foreach (var o in c.Offices.Where(o => o.Until.HasValue))
                foreach (var p in c.People)
                    foreach (var t in p.Offices.Where(t => t.Office == o.Id))
                        if (!t.To.HasValue || t.To.Value > o.Until!.Value)
                            throw new ContentException("people.yaml", $"people[{p.Id}].offices.{o.Id}.to", FormattableString.Invariant($"must be on or before {o.Until:yyyy-MM-dd}, when the office was abolished"));
            foreach (var p in c.People.Where(p => p.Died.HasValue))
                foreach (var t in p.Offices)
                    if (!t.To.HasValue || t.To.Value > p.Died!.Value)
                        throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.to", FormattableString.Invariant($"must be on or before {p.Died:yyyy-MM-dd}, the date of death"));
            foreach (var p in c.People)
```

3. Replace

```csharp
            }
            foreach (var i in c.Indicators)
```

   with

```csharp
            }
            NoOverlap("legislatures.yaml", "legislatures", c.Legislatures.Select(l => (l.Id, l.From, l.To)));
            NoOverlap("governments.yaml", "governments", c.Governments.Select(g => (g.Id, g.From, g.To)));
            foreach (var i in c.Indicators)
```

4. Replace

```csharp
                if (!offices.Contains(kv.Key)) throw new ContentException("rules.yaml", $"budget_start.{kv.Key}", "unknown office");
                Unit("rules.yaml", $"budget_start.{kv.Key}", kv.Value);
```

   with

```csharp
                if (!offices.Contains(kv.Key)) throw new ContentException("rules.yaml", $"budget_start.{kv.Key}", "unknown office");
                // A budget line must not silently stop mattering when its office is abolished: move it to the office that
                // took over (plan amendment C3).
                var until = c.Offices.First(o => o.Id == kv.Key).Until;
                if (until.HasValue)
                    throw new ContentException("rules.yaml", $"budget_start.{kv.Key}", FormattableString.Invariant($"office '{kv.Key}' is abolished on {until:yyyy-MM-dd}: give its budget line to the office that took over"));
                Unit("rules.yaml", $"budget_start.{kv.Key}", kv.Value);
```

Replace the whole of `docs/determinism.md` with:

```markdown
# Determinism

The simulation must produce the same state, bit for bit, on .NET, Unity Mono and IL2CPP. Saves, replays and the
"Why did this happen?" report depend on this.

## How it is kept

- **One RNG:** SplitMix64 (`Statecraft.Sim.Rng`). Its whole state is one `ulong` in `GameState.RngState`.
- **Reference vectors:** `DeterminismTests.SplitMix64MatchesTheReferenceVectors` checks the first three outputs for
  seeds 0 and 1234567. The expected values come from Sebastiano Vigna's reference `splitmix64.c`
  (https://prng.di.unimi.it/splitmix64.c), compiled and run separately. They were not copied from this implementation.
- **Canonical hash:** `GameState.Hash()` is SHA-256 over a canonical text. Keys are sorted ordinally. Doubles are
  written as IEEE bit patterns, with -0.0 written as 0.0. Culture is invariant.

## Golden playthroughs

`Statecraft.Sim.GoldenRun.Play(content, until)` starts `Simulation.NewGame(content, GoldenRun.Seed, Mode.Historical)`
with `GoldenRun.Seed = 1948` on the real `content/`, then steps with `HistoricalAI.Commands` until `Date >= until`
(or the game ends, or 1000 turns pass). Each run's final `GameState.Hash()` is pinned:

| Run | Ends | Hash |
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9` |

Each constant is written in three places that must agree:
- `sim/Statecraft.Sim/GoldenRun.cs`, which Unity code and players compare against;
- `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, which pins the value on .NET and asserts the library constant equals it;
- this file.

`unity/Assets/Tests/EditMode/SimSmokeTests.cs` runs the same playthroughs inside Unity against `GoldenRun`'s constants
(run `tools/build_sim_for_unity.sh` first so Unity has the current DLLs).

Verified on .NET and Unity editor Mono. IL2CPP and WebGL: pending (M3 Task 22 checks both players).

**Any change to a golden hash is a deliberate rules or content change.** Such a change could be to a rule, a system,
content in `content/`, or the hash format. When it happens, update every place above in the same commit, add a row to
the history below, and say in the commit message why the hash moved. If a hash changes and you did not mean to change
rules or content, treat it as a determinism bug.

## Rebaseline history

| Plan task | Run | New hash (first 8) | Why |
|---|---|---|---|
```

- [ ] **Step 4: Run the sim tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   318, Skipped:     0, Total:   318` (309 before; 8 new period-validation tests and 1 new constant test). The golden hash is unchanged: `eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9`.

- [ ] **Step 5: Run the Unity edit-mode tests**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="81" passed="81" failed="0"` (SimSmokeTests still has two tests; the golden one now reads `GoldenRun`).

- [ ] **Step 6: Commit**

```bash
git add docs/determinism.md \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim.Tests/PeriodValidationTests.cs \
  sim/Statecraft.Sim/Content.cs \
  sim/Statecraft.Sim/ContentLoader.cs \
  sim/Statecraft.Sim/GoldenRun.cs \
  unity/Assets/Tests/EditMode/SimSmokeTests.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(sim): GoldenRun; reject overlapping legislatures and governments, terms past abolition or death

Ruling 3 moves the overlap validation to M3. Golden hash unchanged.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 2: 1950–51 governments, Knesset, ministers and President

*Track A.* Ruling 3: the end of the First Government, the Second (1 Nov 1950) and Third (8 Oct 1951) Governments with every minister, portfolio, date and dated party; the Second Knesset (30 Jul 1951, 120 seats); Weizmann's re-election; the minister responsible for rationing (C2 ruling, research Addendum 2): `rationing_minister` is retitled "Minister responsible for rationing" with no abolition date; Dov Yosef holds it as Minister of Rationing and Supply from 26 Apr 1949 (the Rationing fix) to 1 Nov 1950, Pinhas Lavon (Agriculture, which took most rationing functions) from 1 Nov 1950 to 8 Oct 1951, and it is vacant afterwards because no source names the minister; Task 1's validation is unchanged (this office simply has no `until`); and the end of the Ministries of Immigration and of War Victims on 8 Oct 1951 (the research addendum finds no such portfolio in the Third Government). The Immigration budget line moves to the Prime Minister, who with the Jewish Agency handled absorption (addendum (a)); Task 1's loader rule refuses a budget line for an abolished office. Every value comes from `research_notes/M3 content 1950-51/facts.md` §1–3 and §6, with the notes' conflicts resolved in YAML comments. Deputy ministers and Kaplan's Deputy Prime Minister title are not modelled (notes §1c and §1e allow skipping them; Deferred).

**Files:**
- Modify: `content/events/austerity_1949.yaml`, `content/governments.yaml`, `content/legislatures.yaml`, `content/offices.yaml`, `content/parties.yaml`, `content/people.yaml`, `content/rules.yaml`, `docs/determinism.md`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim.Tests/RealContentTests.cs`, `sim/Statecraft.Sim/GoldenRun.cs`

**Interfaces:**
- Consumes: Task 1's `Office.Until`, `Person.Died`, overlap validation; `Offices.Holder(Content, string, DateTime)`, `Offices.Government/Legislature(Content, DateTime)`, `Offices.PartyOf(Content, Person, DateTime)`, `PoliticsSystem.CoalitionSeats(Content, DateTime)`.
- Produces (content ids later tasks use): offices `president`, `minister_without_portfolio`, `rationing_minister` retitled "Minister responsible for rationing" (no `until`; holders dov_yosef 1949-04-26 to 1950-11-01, lavon 1950-11-01 to 1951-10-08, vacant from 1951-10-08), `immigration_minister` and `war_victims_minister` with `until: 1951-10-08`; `rules.yaml` `budget_start.prime_minister: 0.15` (was `immigration_minister`); parties `democratic_list_israeli_arabs`, `progress_and_work`, `agriculture_and_development`; governments `second_government`, `third_government`; legislature `second_knesset`; people `lavon`, `geri`, `eshkol`, `dinur`, `burg`, `pinkas`, `naftali`, `weizmann`; `remez` has `died: 1951-05-19`. `GoldenRun.Hash1950` becomes `d46ce5ffd442f6b47f515fe2497b1db3e262217b5d01be543b822e5d41a6a8cd`.

- [ ] **Step 1: Write the failing tests**

Add the spot checks first; they name people and bodies that do not exist yet.

In `sim/Statecraft.Sim.Tests/RealContentTests.cs`:

1. Replace

```csharp
        var open = C.Legislatures.SelectMany(l => l.OpenQuestions.Select(q => $"{l.Id}.{q}")).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "first_knesset.budget_approval", "provisional_state_council.budget_approval" }, open);
    }
```

   with

```csharp
        var open = C.Legislatures.SelectMany(l => l.OpenQuestions.Select(q => $"{l.Id}.{q}")).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "first_knesset.budget_approval", "provisional_state_council.budget_approval", "second_knesset.budget_approval" }, open);
    }
```

2. Replace

```csharp
        Assert.Equal("dov_yosef", Offices.Holder(C, "rationing_minister", new DateTime(1949, 6, 1))!.Id);
        Assert.Equal((30, 37), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));
```

   with

```csharp
        Assert.Equal("dov_yosef", Offices.Holder(C, "rationing_minister", new DateTime(1949, 6, 1))!.Id);
        Assert.Null(Offices.Holder(C, "rationing_minister", new DateTime(1949, 4, 25)));   // ministry created 26 April 1949
        Assert.Equal((30, 37), PoliticsSystem.CoalitionSeats(C, new DateTime(1948, 6, 1)));
```

3. Replace

```csharp
    }
}
```

   with

```csharp
    }

    [Fact]
    public void GovernmentsAndKnessetsOf1950And1951()
    {
        Assert.Equal("first_government", Offices.Government(C, new DateTime(1950, 10, 31))!.Id);
        Assert.Equal("second_government", Offices.Government(C, new DateTime(1950, 11, 1))!.Id);
        Assert.Equal("third_government", Offices.Government(C, new DateTime(1951, 10, 8))!.Id);
        Assert.Equal("first_knesset", Offices.Legislature(C, new DateTime(1951, 8, 19))!.Id);
        Assert.Equal("second_knesset", Offices.Legislature(C, new DateTime(1951, 8, 20))!.Id);
        Assert.Equal((73, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1951, 1, 1)));
        Assert.Equal((51, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1951, 9, 1)));   // transitional government, new Knesset
        Assert.Equal((65, 120), PoliticsSystem.CoalitionSeats(C, new DateTime(1951, 10, 8)));
    }

    [Fact]
    public void CabinetChangesOf1950And1951()
    {
        string Who(string office, int y, int m, int d) => Offices.Holder(C, office, new DateTime(y, m, d))?.Id ?? "(vacant)";
        Assert.Equal("shazar", Who("education_minister", 1950, 10, 31));
        Assert.Equal("remez", Who("education_minister", 1950, 11, 1));
        Assert.Equal("ben_gurion", Who("education_minister", 1951, 5, 19));
        Assert.Equal("dinur", Who("education_minister", 1951, 10, 8));
        Assert.Equal("lavon", Who("rationing_minister", 1950, 11, 1));
        Assert.Equal("(vacant)", Who("rationing_minister", 1951, 10, 8));
        Assert.Equal("dov_yosef", Who("transportation_minister", 1951, 1, 1));
        Assert.Equal("pinkas", Who("transportation_minister", 1951, 10, 8));
        Assert.Equal("lavon", Who("agriculture_minister", 1951, 1, 1));
        Assert.Equal("eshkol", Who("agriculture_minister", 1951, 10, 8));
        Assert.Equal("geri", Who("trade_minister", 1951, 1, 1));
        Assert.Equal("dov_yosef", Who("trade_minister", 1951, 10, 8));
        Assert.Equal("dov_yosef", Who("justice_minister", 1951, 10, 8));
        Assert.Equal("rosen", Who("justice_minister", 1951, 10, 7));
        Assert.Equal("burg", Who("health_minister", 1951, 10, 8));
        Assert.Equal("weizmann", Who("president", 1951, 11, 19));
        Assert.Equal("yadin", Who("chief_of_staff", 1951, 12, 31));
        Assert.Null(C.Offices.Single(o => o.Id == "rationing_minister").Until);   // the responsibility outlived the ministry (Addendum 2)
        Assert.Equal(new DateTime(1951, 10, 8), C.Offices.Single(o => o.Id == "immigration_minister").Until);
        Assert.Equal(new DateTime(1951, 10, 8), C.Offices.Single(o => o.Id == "war_victims_minister").Until);
        Assert.Equal("shapira", Who("immigration_minister", 1951, 10, 7));
        Assert.Equal(0.15, C.Rules.BudgetStart["prime_minister"]);
        var sheetrit = C.People.Single(p => p.Id == "sheetrit");
        Assert.Equal("sephardim", Offices.PartyOf(C, sheetrit, new DateTime(1951, 1, 1)));
        Assert.Equal("mapai", Offices.PartyOf(C, sheetrit, new DateTime(1951, 10, 8)));
        Assert.Equal("", Offices.PartyOf(C, C.People.Single(p => p.Id == "geri"), new DateTime(1951, 1, 1)));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `RealContentTests.GovernmentsAndKnessetsOf1950And1951` fails (`Expected: "first_government"` is fine, then `second_government` is missing: NullReferenceException on `Offices.Government(...)!`), `CabinetChangesOf1950And1951` fails, and `OpenQuestionsAreExactlyTheKnownOnes` fails (no `second_knesset.budget_approval`).

- [ ] **Step 3: Confirm the death dates**

Confirm a death date on the en.wiki page of each person added below (Lavon, Geri, Eshkol, Dinur, Burg, Pinkas, Naftali, Weizmann): `living: false` is set only after that check (the existing convention at the top of `people.yaml`). Weizmann's first-term start (17 Feb 1949) is sourced in the research addendum, Q3.

- [ ] **Step 4: Write the content**

Replace the whole of `content/offices.yaml` with:

```yaml
offices:
  - { id: prime_minister, title: Prime Minister }
  - { id: defense_minister, title: Minister of Defense }
  - { id: finance_minister, title: Minister of Finance }
  - { id: foreign_minister, title: Minister of Foreign Affairs }
  - { id: health_minister, title: Minister of Health }
  # No Immigration or War Victims portfolio in the Third Government (8 Oct 1951): research_notes/M3 content 1950-51/facts.md,
  # Addendum (2026-10-10) (a) and (b), after en.wiki, he.wiki and JVL. Re-add a term if a source shows the portfolio.
  - { id: immigration_minister, title: Minister of Immigration, until: 1951-10-08 }
  - { id: interior_minister, title: Minister of Internal Affairs }
  - { id: justice_minister, title: Minister of Justice }
  - { id: agriculture_minister, title: Minister of Agriculture }
  - { id: labour_minister, title: Minister of Labour }
  - { id: police_minister, title: Minister of Police }
  - { id: minority_affairs_minister, title: Minister of Minority Affairs }
  - { id: religions_minister, title: Minister of Religions }
  - { id: war_victims_minister, title: Minister of War Victims, until: 1951-10-08 }
  - { id: trade_minister, title: Minister of Trade and Industry }
  - { id: transportation_minister, title: Minister of Transportation }
  - { id: welfare_minister, title: Minister of Welfare }
  - { id: education_minister, title: Minister of Education and Culture }
  # The minister responsible for rationing: the rationing policy's effects and events look this office up. Until
  # 1 November 1950 that was the Minister of Rationing and Supply (Dov Yosef). The ministry was then abolished and its
  # functions split: most went to Agriculture (Lavon), some supply departments to Trade and Industry (Geri) (research
  # notes, Addendum 2). From 8 October 1951 no source names the responsible minister, so the office is vacant (people.yaml).
  - { id: rationing_minister, title: Minister responsible for rationing }
  - { id: chief_of_staff, title: IDF Chief of the General Staff }
  - { id: minister_without_portfolio, title: Minister without Portfolio }
  - { id: president, title: President of Israel }
```

Replace the whole of `content/parties.yaml` with:

```yaml
# Names as listed in the Provisional State Council, 1949 and 1951 election sources (see legislatures.yaml).
parties:
  - { id: mapai, name: Mapai }
  - { id: mapam, name: Mapam }
  - { id: general_zionists, name: General Zionists }
  - { id: new_aliyah, name: New Aliyah }
  - { id: mizrachi, name: Mizrachi }
  - { id: hapoel_hamizrachi, name: Hapoel HaMizrachi }
  - { id: agudat_yisrael, name: Agudat Yisrael }
  - { id: sephardim, name: Sephardim and Oriental Communities }
  - { id: ahdut_haavoda_poalei_tzion, name: Ahdut HaAvoda-Poalei Tzion }
  - { id: poale_zion_left, name: Poale Zion Left }
  - { id: independent, name: Independent }
  - { id: hatzohar, name: Hatzohar }
  - { id: poalei_agudat_yisrael, name: Poalei Agudat Yisrael }
  - { id: maki, name: Maki }
  - { id: wizo, name: WIZO }
  - { id: yemenite_association, name: Yemenite Association }
  - { id: united_religious_front, name: United Religious Front }
  - { id: herut, name: Herut }
  - { id: progressive, name: Progressive Party }
  - { id: democratic_list_nazareth, name: Democratic List of Nazareth }
  - { id: fighters_list, name: Fighters' List }
  - { id: democratic_list_israeli_arabs, name: Democratic List for Israeli Arabs }
  - { id: progress_and_work, name: Progress and Work }
  - { id: agriculture_and_development, name: Agriculture and Development }
```

Replace the whole of `content/governments.yaml` with:

```yaml
# Periods are [from, to): a government's `to` is the day its successor was sworn in.
governments:
  - id: provisional_government
    name: Provisional Government
    from: 1948-05-14
    to: 1949-03-10
    coalition: [mapai, mapam, general_zionists, new_aliyah, mizrachi, hapoel_hamizrachi, agudat_yisrael, sephardim, ahdut_haavoda_poalei_tzion, poale_zion_left, independent]
    sources:
      - { url: "https://en.wikipedia.org/wiki/Provisional_government_of_Israel", checked: 2026-10-09 }
      - { url: "https://en.wikipedia.org/wiki/Provisional_State_Council", checked: 2026-10-09 }
    status: fact-checked
  - id: first_government
    name: First Government
    # Sources differ (8 vs 10 March 1949); 10 March is the Knesset confidence vote recorded in Ben-Gurion's diary.
    from: 1949-03-10
    # Ben-Gurion resigned 15 October 1950 (some pages give 16 October); the en.wiki infobox gives 30 October as the
    # dissolution date. The successor was sworn in on 1 November 1950, which is the handover date used here.
    to: 1950-11-01
    coalition: [mapai, united_religious_front, progressive, sephardim, democratic_list_nazareth]
    sources:
      - { url: "https://en.wikipedia.org/wiki/First_government_of_Israel", checked: 2026-10-10 }
      - { url: "https://bengurionarchive.bgu.ac.il/en/node/14566", checked: 2026-10-09 }
      - { url: "https://en.wikipedia.org/wiki/1950_in_Israel", checked: 2026-10-10 }
    status: fact-checked
  - id: second_government
    name: Second Government
    # Approved 69-42 with 2 abstentions on 1 November 1950. Resigned 14 February 1951 and served as a transitional
    # government until the Third Government was sworn in on 8 October 1951. From 20 August 1951 its parties are measured
    # against the Second Knesset, where the United Religious Front and the Democratic List of Nazareth held no seats.
    from: 1950-11-01
    to: 1951-10-08
    coalition: [mapai, united_religious_front, progressive, sephardim, democratic_list_nazareth]
    sources:
      - { url: "https://en.wikipedia.org/wiki/Second_government_of_Israel", checked: 2026-10-10 }
      - { url: "https://he.wikipedia.org/wiki/ממשלת_ישראל_השנייה", checked: 2026-10-10 }
      - { url: "https://en.wikipedia.org/wiki/1950_in_Israel", checked: 2026-10-10 }
    status: fact-checked
  - id: third_government
    name: Third Government
    # Coalition of 65 seats per he.wiki. Democratic List for Israeli Arabs, Progress and Work and Agriculture and
    # Development were Mapai-affiliated minority lists without ministers. The Progressive Party and the General Zionists
    # were in opposition. Investiture vote 56-40 (he.wiki citing Haaretz, 9 October 1951).
    from: 1951-10-08
    coalition: [mapai, hapoel_hamizrachi, agudat_yisrael, democratic_list_israeli_arabs, mizrachi, poalei_agudat_yisrael, progress_and_work, agriculture_and_development]
    sources:
      - { url: "https://en.wikipedia.org/wiki/Third_government_of_Israel", checked: 2026-10-10 }
      - { url: "https://he.wikipedia.org/wiki/ממשלת_ישראל_השלישית", checked: 2026-10-10 }
    status: fact-checked
```

In `content/legislatures.yaml`:

1. Replace

```yaml
    from: 1949-02-14
    size: 120
```

   with

```yaml
    from: 1949-02-14
    # Sat until the Second Knesset convened. The government resigned on 14 February 1951; in April 1951 the Knesset
    # passed the Transition to the Second Knesset Law and fixed the election for 30 July 1951 (he.wiki First Knesset).
    to: 1951-08-20
    size: 120
```

2. Replace

```yaml
      - { url: "https://en.wikipedia.org/wiki/Knesset", checked: 2026-10-09 }
    status: fact-checked
```

   with

```yaml
      - { url: "https://en.wikipedia.org/wiki/Knesset", checked: 2026-10-09 }
      - { url: "https://he.wikipedia.org/wiki/הכנסת_הראשונה", checked: 2026-10-10 }
    status: fact-checked
  - id: second_knesset
    body: Second Knesset
    elected: 1951-07-30
    # Convening date from he.wiki only (single source; not confirmed elsewhere).
    from: 1951-08-20
    size: 120
    budget_approval: true
    open_questions: [budget_approval]
    # Election results, identical in en.wiki, IDI and JVL. The United Religious Front ran as four lists (Hapoel
    # HaMizrachi, Mizrachi, Agudat Yisrael, Poalei Agudat Yisrael). he.wiki's composition table reflects later mergers.
    seats:
      mapai: 45
      general_zionists: 20
      mapam: 15
      hapoel_hamizrachi: 8
      herut: 8
      maki: 5
      progressive: 4
      democratic_list_israeli_arabs: 3
      agudat_yisrael: 3
      sephardim: 2
      poalei_agudat_yisrael: 2
      mizrachi: 2
      progress_and_work: 1
      yemenite_association: 1
      agriculture_and_development: 1
    sources:
      - { url: "https://en.wikipedia.org/wiki/1951_Israeli_legislative_election", checked: 2026-10-10 }
      - { url: "https://en.idi.org.il/israeli-elections-and-parties/elections/1951/", checked: 2026-10-10 }
      - { url: "https://www.jewishvirtuallibrary.org/elections-to-the-2nd-knesset-july-1951", checked: 2026-10-10 }
      - { url: "https://he.wikipedia.org/wiki/הכנסת_השנייה", checked: 2026-10-10 }
    status: fact-checked
```

Replace the whole of `content/people.yaml` with:

```yaml
# Offices and dates are historical facts (sourced per term). competence/loyalty are game-balance values, not facts.
# A term's `party` (when set) is the list the holder sat for in that government; otherwise `party` of the person applies.
# An empty person `party` means the person sat for no list (non-MK ministers, the President).
# Periods are [from, to). Terms without `to` continue beyond the M3 content range (end of 1951).
# Terms are split per government (Provisional, First 1949-03-10, Second 1950-11-01, Third 1951-10-08).
# living: false is set only after confirming each person's death date (en.wikipedia.org infobox; 2026-10-09 for the
# 1948-49 cabinet, 2026-10-10 for people added in M3).
# Portraits: public-domain files fetched with license check (promo/CREDITS.md).
# Not modelled: deputy ministers (Efrati, Shari, Kahana) and Kaplan's Deputy Prime Minister title (research notes 1c, 1e).
people:
  - id: ben_gurion
    name: David Ben-Gurion
    living: false
    party: mapai
    competence: 9
    loyalty: 10
    portrait: { file: promo/ref/people/David_Ben-Gurion.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:David_Ben-Gurion_(D597-087).jpg" }
    offices:
      - { office: prime_minister, from: 1948-05-14, sources: &prov [{ url: "https://en.wikipedia.org/wiki/Provisional_government_of_Israel", checked: 2026-10-09 }] }
      - { office: defense_minister, from: 1948-05-14, sources: *prov }
      # Held Education after Remez died in office on 19 May 1951.
      - { office: education_minister, from: 1951-05-19, to: 1951-10-08, sources: &second [{ url: "https://en.wikipedia.org/wiki/Second_government_of_Israel", checked: 2026-10-10 }, { url: "https://he.wikipedia.org/wiki/ממשלת_ישראל_השנייה", checked: 2026-10-10 }] }
    status: fact-checked
  - id: zisling
    name: Aharon Zisling
    living: false
    party: mapam
    competence: 6
    loyalty: 5
    portrait: { file: promo/ref/people/Aharon_Zisling.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Aharon_Zisling.jpg" }
    offices:
      - { office: agriculture_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: kaplan
    name: Eliezer Kaplan
    living: false
    party: mapai
    competence: 8
    loyalty: 9
    portrait: { file: promo/ref/people/Eliezer_Kaplan.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Eliezer_Kaplan.jpg" }
    offices:
      - { office: finance_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: finance_minister, from: 1949-03-10, to: 1950-11-01, sources: &first [{ url: "https://en.wikipedia.org/wiki/First_government_of_Israel", checked: 2026-10-10 }, { url: "https://bengurionarchive.bgu.ac.il/en/node/14566", checked: 2026-10-09 }] }
      - { office: trade_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
      - { office: finance_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
      - { office: finance_minister, from: 1951-10-08, sources: &third [{ url: "https://en.wikipedia.org/wiki/Third_government_of_Israel", checked: 2026-10-10 }, { url: "https://he.wikipedia.org/wiki/ממשלת_ישראל_השלישית", checked: 2026-10-10 }] }
    status: fact-checked
  - id: sharett
    name: Moshe Sharett
    living: false
    party: mapai
    competence: 8
    loyalty: 8
    portrait: { file: promo/ref/people/Moshe_Sharett.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Moshe_Sharett_Portrait.jpg" }
    offices:
      - { office: foreign_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: foreign_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
      - { office: foreign_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
      - { office: foreign_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: shapira
    name: Haim-Moshe Shapira
    living: false
    party: hapoel_hamizrachi
    competence: 7
    loyalty: 7
    portrait: { file: promo/ref/people/Haim-Moshe_Shapira.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Moshe_Shapira.jpg" }
    offices:
      - { office: health_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: immigration_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: health_minister, from: 1949-03-10, to: 1950-11-01, party: united_religious_front, sources: *first }
      - { office: immigration_minister, from: 1949-03-10, to: 1950-11-01, party: united_religious_front, sources: *first }
      - { office: interior_minister, from: 1949-03-10, to: 1950-11-01, party: united_religious_front, sources: *first }
      # en.wiki lists all three portfolios in the Second Government; the he.wiki extract did not clearly show Interior
      # and Immigration (research notes 1c). en.wiki is used.
      - { office: health_minister, from: 1950-11-01, to: 1951-10-08, party: united_religious_front, sources: *second }
      - { office: immigration_minister, from: 1950-11-01, to: 1951-10-08, party: united_religious_front, sources: *second }
      - { office: interior_minister, from: 1950-11-01, to: 1951-10-08, party: united_religious_front, sources: *second }
      # The United Religious Front dissolved in 1951; Shapira sat for Hapoel HaMizrachi (the person's party). The Third
      # Government had no Immigration portfolio (notes, Addendum (a)): offices.yaml ends that office on 1951-10-08.
      - { office: interior_minister, from: 1951-10-08, sources: *third }
      - { office: religions_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: gruenbaum
    name: Yitzhak Gruenbaum
    living: false
    party: independent
    competence: 6
    loyalty: 6
    portrait: { file: promo/ref/people/Yitzhak_Gruenbaum.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Yitzhak_Gruenbaum_1948.jpg" }
    offices:
      - { office: interior_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: rosen
    name: Pinchas Rosen
    living: false
    party: new_aliyah
    competence: 8
    loyalty: 7
    portrait: { file: promo/ref/people/Pinchas_Rosen.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Pinchas_Rosen.jpg" }
    offices:
      - { office: justice_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: justice_minister, from: 1949-03-10, to: 1950-11-01, party: progressive, sources: *first }
      # The Progressive Party went into opposition and was not in the Third Government.
      - { office: justice_minister, from: 1950-11-01, to: 1951-10-08, party: progressive, sources: *second }
    status: fact-checked
  - id: bentov
    name: Mordechai Bentov
    living: false
    party: mapam
    competence: 7
    loyalty: 5
    portrait: { file: promo/ref/people/Mordechai_Bentov.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:MordechaiBentov.jpg" }
    offices:
      - { office: labour_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: sheetrit
    name: Bechor-Shalom Sheetrit
    living: false
    party: sephardim
    competence: 7
    loyalty: 7
    portrait: { file: promo/ref/people/Bechor-Shalom_Sheetrit.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Bechor-Shalom_Shitreet.jpg" }
    offices:
      - { office: police_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: minority_affairs_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: police_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
      - { office: police_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
      # en.wiki Third Government lists him under Mapai; he was elected in 1949 for the Sephardim list. Sources conflict
      # (research notes 1e); the cabinet table (en.wiki) is used.
      - { office: police_minister, from: 1951-10-08, party: mapai, sources: *third }
    status: fact-checked
  - id: maimon
    name: Yehuda Leib Maimon
    living: false
    party: mizrachi
    competence: 6
    loyalty: 7
    portrait: { file: promo/ref/people/Yehuda_Leib_Maimon.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Yehuda_Leib_Maimon.jpg" }
    offices:
      - { office: religions_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: war_victims_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: religions_minister, from: 1949-03-10, to: 1950-11-01, party: united_religious_front, sources: *first }
      - { office: war_victims_minister, from: 1949-03-10, to: 1950-11-01, party: united_religious_front, sources: *first }
      # Resignation date conflicts (research notes 1d): he.wiki has him resign after the February 1951 Knesset vote,
      # which brought the government's resignation on 14 February 1951; en.wiki "1951 in Israel" gives 4 January 1951
      # without a footnote. The footnoted he.wiki account is used, with the government's resignation as the date.
      - { office: religions_minister, from: 1950-11-01, to: 1951-02-14, party: united_religious_front, sources: *second }
      - { office: war_victims_minister, from: 1950-11-01, to: 1951-02-14, party: united_religious_front, sources: *second }
    status: fact-checked
  - id: bernstein
    name: Peretz Bernstein
    living: false
    party: general_zionists
    competence: 7
    loyalty: 6
    portrait: { file: promo/ref/people/Peretz_Bernstein.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Peretz_Bernstein.jpg" }
    offices:
      - { office: trade_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
    status: fact-checked
  - id: remez
    name: David Remez
    living: false
    # Died in office on 19 May 1951 (research notes 1c).
    died: 1951-05-19
    party: mapai
    competence: 7
    loyalty: 8
    portrait: { file: promo/ref/people/David_Remez.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:David_Remez_(cropped).jpg" }
    offices:
      - { office: transportation_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: transportation_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
      - { office: education_minister, from: 1950-11-01, to: 1951-05-19, sources: *second }
    status: fact-checked
  - id: levin
    name: Yitzhak-Meir Levin
    living: false
    party: agudat_yisrael
    competence: 6
    loyalty: 6
    portrait: { file: promo/ref/people/Yitzhak-Meir_Levin.jpg, license: Public domain, source: "https://commons.wikimedia.org/wiki/File:Yitzhak_Meir_Levin.jpg" }
    offices:
      - { office: welfare_minister, from: 1948-05-14, to: 1949-03-10, sources: *prov }
      - { office: welfare_minister, from: 1949-03-10, to: 1950-11-01, party: united_religious_front, sources: *first }
      - { office: welfare_minister, from: 1950-11-01, to: 1951-10-08, party: united_religious_front, sources: *second }
      - { office: welfare_minister, from: 1951-10-08, party: agudat_yisrael, sources: *third }
    status: fact-checked
  - id: dov_yosef
    name: Dov Yosef
    living: false
    party: mapai
    competence: 7
    loyalty: 8
    offices:
      - { office: agriculture_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
      # As Minister of Rationing and Supply: the ministry was created on 26 April 1949, the day rationing was decided,
      # and abolished on 1 November 1950.
      - { office: rationing_minister, from: 1949-04-26, to: 1950-11-01, sources: &rationing [{ url: "https://en.wikipedia.org/wiki/Rationing_and_Supply_Minister_of_Israel", checked: 2026-10-10 }, { url: "https://he.wikipedia.org/wiki/תקופת_הצנע", checked: 2026-10-10 }] }
      - { office: transportation_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
      # en.wiki lists Trade and Industry and Justice; he.wiki lists only Trade and Industry clearly (research notes 1e).
      - { office: trade_minister, from: 1951-10-08, sources: *third }
      - { office: justice_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: shazar
    name: Zalman Shazar
    living: false
    party: mapai
    competence: 7
    loyalty: 8
    offices:
      - { office: education_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
    status: fact-checked
  - id: golda_meir
    name: Golda Meir
    living: false
    party: mapai
    competence: 8
    loyalty: 9
    offices:
      - { office: labour_minister, from: 1949-03-10, to: 1950-11-01, sources: *first }
      - { office: labour_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
      - { office: labour_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: lavon
    name: Pinhas Lavon
    living: false
    party: mapai
    competence: 7
    loyalty: 7
    offices:
      - { office: agriculture_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
      # Agriculture took most of the rationing and supply functions of the abolished Ministry of Rationing and Supply;
      # Trade and Industry (Geri) took some supply departments (commodities and stock). Research notes, Addendum 2 (A1):
      # he.wiki (read directly); Weitz 2014 and Schiffman and Goldstein (search summaries).
      - { office: rationing_minister, from: 1950-11-01, to: 1951-10-08, sources: [{ url: "https://he.wikipedia.org/wiki/משרד_האספקה_והקיצוב", checked: 2026-10-10 }, { url: "https://in.bgu.ac.il/bgi/iyunim/24/Yechiam-Weitz.pdf", checked: 2026-10-10 }, { url: "https://papers.ssrn.com/sol3/papers.cfm?abstract_id=6351358", checked: 2026-10-10 }] }
      # From 8 October 1951: no source names the responsible minister (Addendum 2: not found), so the office is vacant.
    status: fact-checked
  - id: geri
    name: Ya'akov Geri
    living: false
    # Not a Knesset member. en.wiki gives no party; he.wiki lists him under Mapai (research notes 1c). He sat for no
    # list, so no party is set.
    party: ""
    competence: 7
    loyalty: 6
    offices:
      - { office: trade_minister, from: 1950-11-01, to: 1951-10-08, sources: *second }
    status: fact-checked
  - id: eshkol
    name: Levi Eshkol
    living: false
    party: mapai
    competence: 8
    loyalty: 8
    offices:
      - { office: agriculture_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: dinur
    name: Ben-Zion Dinur
    living: false
    # Not a Knesset member in the Third Government (a former Mapai MK), so no party is set.
    party: ""
    competence: 7
    loyalty: 7
    offices:
      - { office: education_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: burg
    name: Yosef Burg
    living: false
    party: hapoel_hamizrachi
    competence: 7
    loyalty: 7
    offices:
      # Portfolios conflict: en.wiki (Third Government, Yosef Burg) gives Health; he.wiki's table shows Post, which went
      # to Nurock only on 3 November 1952 (research notes 1e). Health is used.
      - { office: health_minister, from: 1951-10-08, sources: [{ url: "https://en.wikipedia.org/wiki/Third_government_of_Israel", checked: 2026-10-10 }, { url: "https://en.wikipedia.org/wiki/Yosef_Burg", checked: 2026-10-10 }] }
    status: fact-checked
  - id: pinkas
    name: David-Zvi Pinkas
    living: false
    party: mizrachi
    competence: 6
    loyalty: 6
    offices:
      - { office: transportation_minister, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: naftali
    name: Peretz Naftali
    living: false
    party: mapai
    competence: 7
    loyalty: 8
    offices:
      - { office: minister_without_portfolio, from: 1951-10-08, sources: *third }
    status: fact-checked
  - id: weizmann
    name: Chaim Weizmann
    living: false
    party: ""
    competence: 8
    loyalty: 7
    offices:
      # Elected 16 February 1949, declared president and sworn in 17 February 1949 (2 sources; the MFA page gives the oath
      # on 16 February): research notes, Addendum Q3. Re-elected by the Second Knesset on 19 November 1951, 85 for,
      # 11 against, 3 blank; the only candidate.
      - { office: president, from: 1949-02-17, to: 1951-11-19, sources: [{ url: "https://en.wikipedia.org/wiki/Chaim_Weizmann", checked: 2026-10-10 }, { url: "https://en.wikipedia.org/wiki/1949_Israeli_presidential_election", checked: 2026-10-10 }, { url: "https://weizmann.ac.il/WeizmannCompass/node/914", checked: 2026-10-10 }] }
      # The second term starts on the election date (notes section 3); he was sworn in at Rehovot on 25 November 1951
      # (single source, IDI via search), which is not used as a date.
      - { office: president, from: 1951-11-19, sources: [{ url: "https://en.wikipedia.org/wiki/1951_Israeli_presidential_election", checked: 2026-10-10 }, { url: "https://en.wikipedia.org/wiki/1951_in_Israel", checked: 2026-10-10 }] }
    status: fact-checked
  - id: dori
    name: Yaakov Dori
    living: false
    competence: 7
    loyalty: 8
    offices:
      - { office: chief_of_staff, from: 1947-06-01, to: 1949-11-09, sources: &cos [{ url: "https://en.wikipedia.org/wiki/Chief_of_the_General_Staff_(Israel)", checked: 2026-10-09 }] }
    status: fact-checked
  - id: yadin
    name: Yigael Yadin
    living: false
    competence: 8
    loyalty: 8
    offices:
      # In office through the M3 range: resigned 7 December 1952 (research notes 8).
      - { office: chief_of_staff, from: 1949-11-09, sources: *cos }
    status: fact-checked
```

Replace the whole of `content/events/austerity_1949.yaml` with:

```yaml
id: austerity_1949
title: Austerity
text: 'Food and foreign currency cannot keep pace with the immigrants arriving every week. {rationing_minister}, heading the new Ministry of Rationing and Supply, proposes strict rationing.'
anchor: hard
office: rationing_minister
# Rationing was decided on 26 April 1949, the day the ministry was created (he.wiki austerity).
from: 1949-04-26
to: 1950-01-01
options:
  - id: impose
    text: Impose rationing
    historical: true
    recommended: true
    set_policy: { rationing: 1.0 }
    effects:
      - { target: food_supply, delta: 0.15 }
      - { target: morale, delta: -0.05 }
  - id: decline
    text: Keep markets free and import more
    effects:
      - { target: fx_reserves, delta: -8 }
      - { target: food_supply, delta: -0.05 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Austerity_in_Israel", checked: 2026-10-09 }
  - { url: "https://he.wikipedia.org/wiki/תקופת_הצנע", checked: 2026-10-10 }
  - { url: "https://en.wikipedia.org/wiki/First_government_of_Israel", checked: 2026-10-09 }
status: fact-checked
```

Replace the whole of `content/rules.yaml` with:

```yaml
# Game rules. Values are game balance, except where a comment cites a source.
start: 1948-05-15                 # first day covered by the CBS 1948 immigration series (aliyah.yaml)
start_rung: 5                     # the war is under way when the slice opens
attention_per_turn: 4
attention_cost: { set_policy: 1, allocate_budget: 2, choose_event_option: 1, set_mobilization: 1, security_order: 1 }
rung_attention_step: 0.25
rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
ceasefire_weeks: 52               # long enough that the historical armistice events end the war first
treasury_start: 20
fx_start: 40
tax_per_capita_per_year: 0.00012
spending_per_capita_per_year: 0.00016
exports_per_year: 30
imports_per_capita_per_year: 0.00008
mobilization_cost_per_year: 60
# Derived from CBS table 2.1 (1949): (end-1949 1,013.9k - end-1948 758.7k - 1949 immigrants 239,954) / average 1949 population 901.0k.
natural_increase_per_year: 0.0169
# Derived: CBS end-1948 Jewish population 758,700 minus 101,828 immigrants (15 May-31 Dec 1948) minus natural increase over
# 7.5 months at the rate above. Checked against the 8 Nov 1948 census by the calibration test.
# Arithmetic: 758,700 - 101,828 = 656,872. Natural-increase base = average of 656,872 and 758,700 = 707,786.
# Natural increase = 707,786 x 0.0169 x 7.5/12 = 7,476. 656,872 - 7,476 = 649,396, rounded to the nearest hundred = 649,400.
initial_population: { veterans: 649400 }
budget_start:
  defense_minister: 0.45
  finance_minister: 0.05
  # Absorption was handled by the Jewish Agency and the Prime Minister's office (notes, Addendum (a)); the Ministry of
  # Immigration ends on 1951-10-08, so its budget line sits with the Prime Minister (the loader refuses a line for an
  # abolished office).
  prime_minister: 0.15
  labour_minister: 0.10
  health_minister: 0.07
  education_minister: 0.06
  agriculture_minister: 0.07
  interior_minister: 0.05
```

- [ ] **Step 5: Rebaseline the 1948–50 golden hash (deliberate content change)**

Moving `austerity_1949` to 1949-04-26 alone would not move the hash (verified). Renaming the budget key `immigration_minister` to `prime_minister` does, because budget keys are hashed. Run the sim tests: `GoldenPlaythroughHashIsFixed` fails with `Actual: "d46ce5ffd442f6b47f515fe2497b1db3e262217b5"···`; the full value (also `dotnet run --project sim/Statecraft.Cli -- --auto historical --turns 45`, line `State hash ...`) is `d46ce5ffd442f6b47f515fe2497b1db3e262217b5d01be543b822e5d41a6a8cd`. Replace `eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9` with it in `sim/Statecraft.Sim/GoldenRun.cs` (`Hash1950`) and `sim/Statecraft.Sim.Tests/DeterminismTests.cs` (`GoldenHash`), and update `docs/determinism.md`:

In `docs/determinism.md`:

1. Replace

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `eb9756528eeebdc7b078ab90f7fe7a349edcfc94be9543e5f20f258da4e500e9` |

```

   with

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `d46ce5ffd442f6b47f515fe2497b1db3e262217b5d01be543b822e5d41a6a8cd` |

```

2. Replace

```markdown
|---|---|---|---|
```

   with

```markdown
|---|---|---|---|
| M3 Task 2 | 1950 | `d46ce5ff` | the Immigration budget line moves to the Prime Minister (the Ministry of Immigration ends on 1951-10-08), which renames a key in the hashed budget |
```

- [ ] **Step 6: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   320`.

- [ ] **Step 7: Commit**

```bash
git add content/events/austerity_1949.yaml \
  content/governments.yaml \
  content/legislatures.yaml \
  content/offices.yaml \
  content/parties.yaml \
  content/people.yaml \
  content/rules.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim.Tests/RealContentTests.cs \
  sim/Statecraft.Sim/GoldenRun.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "content: 1950-51 governments, Second Knesset, ministers, President; rationing responsibility (Yosef, then Lavon); Immigration ends 1951-10-08

Sources: research_notes/M3 content 1950-51/facts.md sections 1-3 and 6, and its Addendum. Conflicts recorded in YAML comments.
Golden 1950 hash moves to d46ce5ff: the Immigration budget line moves to the Prime Minister.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 3: Immigration and population 1950–51, calibrated to the end of 1951

*Track A.* Ruling 3: aliyah for 1950 and 1951 (CBS totals) and the end-1950 and end-1951 population anchors (CBS Statistical Abstract table 2.1), from notes §4a and §4d. The M1 calibration test (Task 18 of the M0–M2 plan) extends to two new anchors that nothing in `rules.yaml` was derived from, so they are independent checks; tolerance stays 3%.

**Files:**
- Modify: `content/aliyah.yaml`, `content/population.yaml`, `docs/determinism.md`, `sim/Statecraft.Sim.Tests/CalibrationTests.cs`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim/GoldenRun.cs`

**Interfaces:**
- Consumes: `CalibrationTests` (M1), `PopulationSystem.Arrivals`, `GoldenRun` (Task 1).
- Produces: aliyah periods 1950-01-01..1950-12-31 (170,563) and 1951-01-01..1951-12-31 (175,279); population anchors 1950-12-31 (1,203,000) and 1951-12-31 (1,404,400). `GoldenRun.Hash1950` becomes `3d360deb200945776a9ae1ae5faea8cd075900d0b9cd7d2bab715b309e3f0b91`.

- [ ] **Step 1: Write the failing test**

In `sim/Statecraft.Sim.Tests/CalibrationTests.cs`:

1. Replace

```csharp
/// checks of that derivation (they should hold by construction). The 1948-11-08 population registration
/// (716,700) is the only INDEPENDENT anchor.
/// </summary>
```

   with

```csharp
/// checks of that derivation (they should hold by construction). The 1948-11-08 population registration
/// (716,700) and the end-1950 and end-1951 figures (nothing in rules.yaml is derived from them) are INDEPENDENT.
/// </summary>
```

2. Replace

```csharp

    static string Kind(DateTime d) => d == new DateTime(1948, 11, 8) ? "independent" : "consistency";

```

   with

```csharp

    static readonly DateTime End = new DateTime(1952, 1, 1);

    static string Kind(DateTime d) =>
        d == new DateTime(1948, 11, 8) || d.Year >= 1950 ? "independent" : "consistency";

```

3. Replace

```csharp
        var history = new List<GameState> { s };
        while (s.Date <= new DateTime(1950, 1, 1))
        {
```

   with

```csharp
        var history = new List<GameState> { s };
        while (s.Date <= End)
        {
```

4. Replace

```csharp
        Assert.Null(s.GameOver);
        Assert.Equal(3, C.PopulationAnchors.Count);
        var report = C.PopulationAnchors.Select(a =>
```

   with

```csharp
        Assert.Null(s.GameOver);
        Assert.Equal(5, C.PopulationAnchors.Count);
        var report = C.PopulationAnchors.Select(a =>
```

5. Replace

```csharp
        Assert.True(report.All(r => Math.Abs(r.error) <= Tolerance),
            "Only 1948-11-08 is an independent anchor; the other two are consistency checks of the rules.yaml derivation. " + lines);
    }
```

   with

```csharp
        Assert.True(report.All(r => Math.Abs(r.error) <= Tolerance),
            "1948-12-31 and 1949-12-31 are consistency checks of the rules.yaml derivation; the other anchors are independent. " + lines);
    }
```

- [ ] **Step 2: Run it and confirm it fails**

Run:
```bash
dotnet test sim/Statecraft.sln --filter CalibrationTests
```
Expected: `HistoricalPlaythroughMatchesCbsPopulationAnchors` fails with `Assert.Equal() Failure: Values differ, Expected: 5, Actual: 3`.

- [ ] **Step 3: Add the content**

In `content/aliyah.yaml`:

1. Replace

```yaml
    origins: { asia: 71652, africa: 39215, europe: 121963, america_oceania: 1422, unknown: 5702 }
    sources:
      - { url: "https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html", checked: 2026-10-09 }
      - { url: "https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year", checked: 2026-10-09 }
    status: fact-checked
```

   with

```yaml
    origins: { asia: 71652, africa: 39215, europe: 121963, america_oceania: 1422, unknown: 5702 }
    sources:
      - { url: "https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html", checked: 2026-10-09 }
      - { url: "https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year", checked: 2026-10-09 }
    status: fact-checked
  # 1950 and 1951 rows parsed from the raw HTML of the JVL table (right-to-left; the row label follows the data). Each
  # row sums exactly to the CBS total: 170,563 (1950) and 175,279 (1951), as in CBS Publication 1365, Table 1.
  - from: 1950-01-01
    to: 1950-12-31
    origins: { asia: 57565, africa: 26162, europe: 81195, america_oceania: 1954, unknown: 3687 }
    sources:
      - { url: "https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html", checked: 2026-10-10 }
      - { url: "https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year", checked: 2026-10-10 }
      - { url: "https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/e_print.pdf", checked: 2026-10-10 }
    status: fact-checked
  - from: 1951-01-01
    to: 1951-12-31
    origins: { asia: 103396, africa: 20382, europe: 47074, america_oceania: 1286, unknown: 3141 }
    sources:
      - { url: "https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html", checked: 2026-10-10 }
      - { url: "https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year", checked: 2026-10-10 }
      - { url: "https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/e_print.pdf", checked: 2026-10-10 }
    status: fact-checked
```

In `content/population.yaml`:

1. Replace

```yaml
    jews: 1013900       # end of year
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-09 }]
    status: fact-checked
```

   with

```yaml
    jews: 1013900       # end of year
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-09 }]
    status: fact-checked
  - date: 1950-12-31
    jews: 1203000       # end of year (1,203.0 thousand)
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-10 }]
    status: fact-checked
  - date: 1951-12-31
    jews: 1404400       # end of year (1,404.4 thousand)
    sources: [{ url: "https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf", checked: 2026-10-10 }]
    status: fact-checked
```

- [ ] **Step 4: Run the calibration and see the margins**

Temporarily set `const double Tolerance = 0.0;` in `CalibrationTests.cs` and run the filter again. Expected failure text (the margins, verified):
`[independent] 1948-11-08: sim 733,075 vs CBS 716,700 (+2.3%); [consistency] 1948-12-31: sim 758,264 vs CBS 758,700 (-0.1%); [consistency] 1949-12-31: sim 1,008,488 vs CBS 1,013,900 (-0.5%); [independent] 1950-12-31: sim 1,201,851 vs CBS 1,203,000 (-0.1%); [independent] 1951-12-31: sim 1,388,157 vs CBS 1,404,400 (-1.2%)`. Restore `0.03`.

- [ ] **Step 5: Rebaseline the 1948–50 golden hash (deliberate content change)**

The 1950 immigration series adds January–February 1950 arrivals inside the 1948–50 run, so its hash moves. Run the sim tests: `DeterminismTests.GoldenPlaythroughHashIsFixed` fails with `Actual: "3d360deb200945776a9ae1ae5faea8cd075900d0b"···`. The full new value (also printed by `dotnet run --project sim/Statecraft.Cli -- --auto historical --turns 45` as `State hash ...`) is `3d360deb200945776a9ae1ae5faea8cd075900d0b9cd7d2bab715b309e3f0b91`. Replace `d46ce5ffd442f6b47f515fe2497b1db3e262217b5d01be543b822e5d41a6a8cd` with it in `sim/Statecraft.Sim/GoldenRun.cs` (`Hash1950`) and `sim/Statecraft.Sim.Tests/DeterminismTests.cs` (`GoldenHash`), and update `docs/determinism.md`:

In `docs/determinism.md`:

1. Replace

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `d46ce5ffd442f6b47f515fe2497b1db3e262217b5d01be543b822e5d41a6a8cd` |

```

   with

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `3d360deb200945776a9ae1ae5faea8cd075900d0b9cd7d2bab715b309e3f0b91` |

```

2. Replace

```markdown
| M3 Task 2 | 1950 | `d46ce5ff` | the Immigration budget line moves to the Prime Minister (the Ministry of Immigration ends on 1951-10-08), which renames a key in the hashed budget |
```

   with

```markdown
| M3 Task 2 | 1950 | `d46ce5ff` | the Immigration budget line moves to the Prime Minister (the Ministry of Immigration ends on 1951-10-08), which renames a key in the hashed budget |
| M3 Task 3 | 1950 | `3d360deb` | CBS immigration for 1950 adds January-February 1950 arrivals inside the run |
```

- [ ] **Step 6: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   320`.

- [ ] **Step 7: Commit**

```bash
git add content/aliyah.yaml \
  content/population.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/CalibrationTests.cs \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim/GoldenRun.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "content: CBS immigration and population for 1950 and 1951

Golden 1950 hash moves to 3d360deb: the 1950 series adds Jan-Feb 1950 arrivals inside the run.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 4: Eleven sourced events for 1950–51

*Track A.* Ruling 3 asks for about ten sourced events from notes §10. These are the notes' ten candidates plus the strongest-sourced extra (`forex_crisis_1951`, FRUS doc 513). Texts state only what the cited pages say, in Israeli framing, with `{office}` placeholders and no quotes. Option effects are game balance. `reparations_claim` is tagged `holocaust`, so the loader forbids any treasury or foreign-currency reward on it. `maabarot_policy` sets a new policy, `maabarot`, which Task 8 requires for transit camps. Hard anchors are the events that happened as scheduled decisions (the Historical mode guarantees them); the rest are soft.

**Files:**
- Create: `content/events/clothing_rationing_1950.yaml`, `content/events/education_registration_1951.yaml`, `content/events/forex_crisis_1951.yaml`, `content/events/frumkin_commission.yaml`, `content/events/hula_drainage.yaml`, `content/events/iraqi_exodus.yaml`, `content/events/israel_bonds.yaml`, `content/events/law_of_return.yaml`, `content/events/maabarot_policy.yaml`, `content/events/ministry_supply_crisis.yaml`, `content/events/reparations_claim.yaml`
- Modify: `content/model.yaml`, `docs/determinism.md`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim.Tests/RealContentTests.cs`, `sim/Statecraft.Sim/GoldenRun.cs`

**Interfaces:**
- Consumes: event schema (`GameEvent`, `EventOption`, `set_policy`, `effects`), Task 2's office holders (placeholder coverage is checked over each event's window).
- Produces: event ids `frumkin_commission`, `maabarot_policy`, `law_of_return`, `clothing_rationing_1950`, `ministry_supply_crisis`, `education_registration_1951`, `israel_bonds`, `hula_drainage`, `reparations_claim`, `iraqi_exodus`, `forex_crisis_1951`; policy `maabarot` (default 0, office `prime_minister`: the Ministry of Immigration ends on 1951-10-08). `GoldenRun.Hash1950` becomes `e2fa38969b15171a95d6ab7e3d3236d135a6b7af94e0dc822cf391fb3fb898d0`.

- [ ] **Step 1: Write the failing test**

In `sim/Statecraft.Sim.Tests/RealContentTests.cs`:

1. Replace

```csharp
    }
}
```

   with

```csharp
    }

    [Fact]
    public void EveryEventOf1950And1951FiresInTheHistoricalPlaythrough()
    {
        var s = GoldenRun.Play(C, new DateTime(1952, 1, 1));
        Assert.Null(s.GameOver);
        var expected = new[] { "clothing_rationing_1950", "education_registration_1951", "forex_crisis_1951", "frumkin_commission",
            "hula_drainage", "iraqi_exodus", "israel_bonds", "law_of_return", "maabarot_policy", "ministry_supply_crisis", "reparations_claim" };
        var missing = expected.Where(id => !s.FiredEvents.Contains(id)).ToList();
        Assert.True(missing.Count == 0, "Never fired: " + string.Join(", ", missing));
        Assert.Equal(1.0, s.Policies["maabarot"]);   // the historical choice opens transit camps
    }
}
```

- [ ] **Step 2: Run it and confirm it fails**

Run:
```bash
dotnet test sim/Statecraft.sln --filter RealContentTests
```
Expected: `EveryEventOf1950And1951FiresInTheHistoricalPlaythrough` fails with `Never fired: clothing_rationing_1950, education_registration_1951, ...` (all eleven).

- [ ] **Step 3: Write the events and the policy**

In `content/model.yaml`:

1. Replace

```yaml
  - { id: border_settlement, default: 0.2, office: agriculture_minister }
effects:
```

   with

```yaml
  - { id: border_settlement, default: 0.2, office: agriculture_minister }
  # 1 = transit camps (ma'abarot) where residents earn their own living replace the free-board immigrant camps.
  # Transit-camp map actions need it at 0.5 or above (MapActions). The event maabarot_policy sets it. Office: the Prime
  # Minister, who decided it with the Jewish Agency (and the Ministry of Immigration ends on 1951-10-08).
  - { id: maabarot, default: 0.0, office: prime_minister }
effects:
```

Create `content/events/clothing_rationing_1950.yaml`:

```yaml
id: clothing_rationing_1950
title: Rationing Clothes and Shoes
text: 'Prices on the black market are climbing. {rationing_minister} proposes extending rationing to clothing and footwear and fighting the black market.'
anchor: hard
office: rationing_minister
# Extended on 31 July 1950; retailers' strike 1-14 August; no-confidence motion defeated 57-36 on 8 August.
from: 1950-07-01
to: 1950-09-01
options:
  - id: extend
    text: Ration clothing and footwear
    historical: true
    recommended: true
    set_policy: { rationing: 1.0 }
    effects:
      - { target: food_supply, delta: 0.02 }
      - { target: morale, delta: -0.05 }
  - id: relax
    text: Ease rationing and let imports fill the shops
    set_policy: { rationing: 0.5 }
    effects:
      - { target: morale, delta: 0.03 }
      - { target: fx_reserves, delta: -8 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Austerity_in_Israel", checked: 2026-10-10 }
  - { url: "https://he.wikipedia.org/wiki/תקופת_הצנע", checked: 2026-10-10 }
  - { url: "https://en.wikipedia.org/wiki/1950_in_Israel", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/education_registration_1951.yaml`:

```yaml
id: education_registration_1951
title: Schools in the Transit Camps
text: 'The coalition is split over schooling for immigrant children in the transit camps: the religious parties oppose a religious stream run by the labour movement. {education_minister} brings a registration plan to the Knesset.'
anchor: hard
office: education_minister
# Dispute from 29 November 1950; the Knesset rejected the proposals and the government resigned on 14 February 1951.
from: 1950-11-29
to: 1951-02-14
options:
  - id: press_plan
    text: Press the registration plan and go to early elections if it fails
    historical: true
    recommended: true
    effects:
      - { target: morale, delta: -0.02 }
  - id: concede
    text: Leave camp children in the religious stream
    effects:
      - { target: morale, delta: -0.01 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Second_government_of_Israel", checked: 2026-10-10 }
  - { url: "https://he.wikipedia.org/wiki/ממשלת_ישראל_השנייה", checked: 2026-10-10 }
  - { url: "https://he.wikipedia.org/wiki/הכנסת_הראשונה", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/forex_crisis_1951.yaml`:

```yaml
id: forex_crisis_1951
title: Foreign Currency Runs Out
text: 'Drought has forced larger wheat imports, immigration in the first half of the year far exceeded the plan, and about 45 million dollars of short-term credit falls due by December. {finance_minister} proposes asking the United States for about 15 million dollars in immediate aid.'
anchor: soft
office: finance_minister
# US Ambassador Davis's report of 19 October 1951 (FRUS 1951 v.V doc 513).
from: 1951-10-01
to: 1952-01-01
options:
  - id: request_aid
    text: Ask Washington for emergency aid
    historical: true
    recommended: true
    effects:
      - { target: fx_reserves, delta: 15 }
      - { target: intl_standing, delta: -0.01 }
  - id: cut_imports
    text: Cut imports further
    effects:
      - { target: food_supply, delta: -0.05 }
      - { target: morale, delta: -0.04 }
sources:
  - { url: "https://history.state.gov/historicaldocuments/frus1951v05/d513", checked: 2026-10-10 }
  - { url: "https://en.wikipedia.org/wiki/Austerity_in_Israel", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/frumkin_commission.yaml`:

```yaml
id: frumkin_commission
title: Education in the Immigrant Camps
text: 'Religious Yemenite parents report coercion in the immigrant-camp schools, which the uniform system put under the Culture Department, including the cutting of sidelocks. The religious parties demand action. {prime_minister} must decide whether to appoint an inquiry.'
anchor: soft
office: prime_minister
# Commission appointed January 1950 (first session 25 January); report submitted 9 May 1950.
from: 1950-01-01
to: 1950-05-09
options:
  - id: inquiry
    text: Appoint a commission of inquiry and move camp education to the Education Ministry
    historical: true
    recommended: true
    effects:
      - { target: morale, delta: 0.02 }
  - id: dismiss
    text: Dismiss the charges and keep the uniform system
    effects:
      - { target: morale, delta: -0.04 }
sources:
  - { url: "https://he.wikipedia.org/wiki/ועדת_פרומקין", checked: 2026-10-10 }
  - { url: "https://en.wikipedia.org/wiki/Gad_Frumkin", checked: 2026-10-10 }
  - { url: "https://en.wikipedia.org/wiki/1950_in_Israel", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/hula_drainage.yaml`:

```yaml
id: hula_drainage
title: Draining the Hula
text: 'Drainage works south of Lake Hula, under the concession Israel holds, reach into the demilitarised zone with Syria. Syria has complained, and the chairman of the armistice commission asks that work stop pending agreement. {prime_minister} must decide.'
anchor: soft
tags: [security]
office: prime_minister
# Works began about 14 February 1951; Syria complained on 21 February; General Riley recommended a halt on 7 March.
from: 1951-02-14
to: 1951-05-18
options:
  - id: continue_works
    text: Continue the works and assert Israel's rights in the zone
    historical: true
    recommended: true
    effects:
      - { target: security, delta: 0.03 }
      - { target: intl_standing, delta: -0.05 }
  - id: halt
    text: Halt the works until there is agreement
    effects:
      - { target: security, delta: -0.03 }
      - { target: intl_standing, delta: 0.03 }
sources:
  - { url: "https://history.state.gov/historicaldocuments/frus1951v05/d288", checked: 2026-10-10 }
  - { url: "https://www.jta.org/archive/israel-resumes-drainage-of-huleh-swamps-syria-concentrates-troops-situation-tense", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/iraqi_exodus.yaml`:

```yaml
id: iraqi_exodus
title: The Jews of Iraq
text: 'Iraq has frozen the assets of the Jews who gave up their citizenship to leave, and some 64,000 people are still waiting to fly. The transit camps are already crowded. {immigration_minister} asks whether to bring everyone at once.'
anchor: soft
office: immigration_minister
# The asset-freeze law of 10 March 1951; the mass airlift ran through 1951.
from: 1951-03-10
to: 1951-07-01
options:
  - id: airlift
    text: Bring everyone as fast as aircraft allow
    historical: true
    recommended: true
    effects:
      - { target: housing, delta: -0.04 }
      - { target: morale, delta: 0.03 }
  - id: pace
    text: Pace arrivals to the places in the camps
    effects:
      - { target: morale, delta: -0.05 }
      - { target: intl_standing, delta: -0.02 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Operation_Ezra_and_Nehemiah", checked: 2026-10-10 }
  - { url: "https://in.bgu.ac.il/bgi/iyunim/40/Meir-Glitzenstein.pdf", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/israel_bonds.yaml`:

```yaml
id: israel_bonds
title: Israel Bonds
text: '{finance_minister} proposes selling State of Israel bonds to Jews abroad, alongside the United Jewish Appeal donations. The Knesset has authorised the first issue.'
anchor: hard
office: finance_minister
# Knesset authorisation February 1951; launched 10 May 1951 at Madison Square Garden; over 52 million dollars sold in
# the first year against a 25 million goal (en.wiki and JVL; en.wiki "1950 in Israel" gives September 1950, which
# conflicts with both and is not used).
from: 1951-02-01
to: 1951-05-10
options:
  - id: launch
    text: Launch the bond issue in the United States
    historical: true
    recommended: true
    effects:
      - { target: fx_reserves, delta: 52 }
      - { target: intl_standing, delta: 0.02 }
  - id: donations_only
    text: Rely on donations and loans
    effects:
      - { target: fx_reserves, delta: 10 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Israel_Bonds", checked: 2026-10-10 }
  - { url: "https://www.jewishvirtuallibrary.org/state-of-israel-bonds", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/law_of_return.yaml`:

```yaml
id: law_of_return
title: The Law of Return
text: 'The Knesset is ready to vote on the Law of Return, which gives every Jew the right to come to Israel as an oleh. {prime_minister} must decide whether the government backs it.'
anchor: hard
office: prime_minister
# Passed unanimously on 5 July 1950.
from: 1950-07-05
to: 1950-07-06
options:
  - id: enact
    text: Enact the right of every Jew to immigrate
    historical: true
    recommended: true
    effects:
      - { target: morale, delta: 0.03 }
      - { target: intl_standing, delta: 0.01 }
  - id: quota
    text: Regulate immigration by yearly quota
    effects:
      - { target: morale, delta: -0.06 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Law_of_Return", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/maabarot_policy.yaml`:

```yaml
id: maabarot_policy
title: Transit Camps
text: 'The immigrant camps, which house and feed new arrivals free of charge, are overflowing. Levi Eshkol of the Jewish Agency Settlement Department proposes transit camps (ma''abarot) near towns and work, where residents earn their own living. {prime_minister} must decide.'
anchor: hard
office: prime_minister
# Eshkol proposed replacing the camps in March 1950; the first ma'abara, Kesalon, opened in May 1950.
from: 1950-03-01
to: 1950-06-01
options:
  - id: maabarot
    text: Build transit camps where residents work for their living
    historical: true
    recommended: true
    set_policy: { maabarot: 1.0 }
    effects:
      - { target: housing, delta: 0.04 }
      - { target: morale, delta: -0.02 }
      - { target: treasury, delta: 2 }
  - id: keep_camps
    text: Keep the free-board immigrant camps
    effects:
      - { target: housing, delta: -0.03 }
      - { target: treasury, delta: -4 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Ma'abarot", checked: 2026-10-10 }
  - { url: "https://he.wikipedia.org/wiki/מעברה", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/ministry_supply_crisis.yaml`:

```yaml
id: ministry_supply_crisis
title: Closing the Ministry of Rationing
text: 'Mapai is divided over closing the Ministry of Rationing and Supply, and {prime_minister} wants a businessman, Ya''akov Geri, who is not a Knesset member, at Trade and Industry. Under the law of the time, adding a minister means the government must resign and win a new vote of confidence.'
anchor: hard
office: prime_minister
# Ben-Gurion resigned on 15 October 1950; the same coalition was approved 69-42 on 1 November 1950.
from: 1950-10-01
to: 1950-11-01
options:
  - id: restructure
    text: Resign, close the ministry and form a new government
    historical: true
    recommended: true
    effects:
      - { target: morale, delta: 0.02 }
  - id: keep_ministry
    text: Keep the ministry and the cabinet as they are
    effects:
      - { target: morale, delta: -0.03 }
sources:
  - { url: "https://en.wikipedia.org/wiki/1950_in_Israel", checked: 2026-10-10 }
  - { url: "https://en.wikipedia.org/wiki/Rationing_and_Supply_Minister_of_Israel", checked: 2026-10-10 }
  - { url: "https://he.wikipedia.org/wiki/הכנסת_הראשונה", checked: 2026-10-10 }
status: fact-checked
```

Create `content/events/reparations_claim.yaml`:

```yaml
id: reparations_claim
title: A Claim on Germany
text: '{foreign_minister} proposes a note to the four occupying powers claiming 1.5 billion dollars from Germany: the cost of absorbing 500,000 survivors of the Holocaust, at 3,000 dollars each.'
anchor: hard
tags: [holocaust]
office: foreign_minister
# The note is dated 12 March 1951 (en.wiki "1951 in Israel" gives 13 March).
from: 1951-03-01
to: 1951-03-13
options:
  - id: claim
    text: Send the claim to the four powers
    historical: true
    recommended: true
    effects:
      - { target: intl_standing, delta: 0.02 }
  - id: refuse
    text: Refuse any dealings with Germany
    effects:
      - { target: intl_standing, delta: -0.01 }
sources:
  - { url: "https://en.wikipedia.org/wiki/Reparations_Agreement_between_Israel_and_the_Federal_Republic_of_Germany", checked: 2026-10-10 }
  - { url: "https://www.jewishvirtuallibrary.org/german-holocaust-reparations", checked: 2026-10-10 }
status: fact-checked
```

- [ ] **Step 4: Rebaseline the 1948–50 golden hash**

`frumkin_commission` (window from 1950-01-01) fires inside the 1948–50 run. `GoldenPlaythroughHashIsFixed` fails with `Actual: "e2fa38969b15171a95d6ab7e3d3236d135a6b7af9"···`; the full value is `e2fa38969b15171a95d6ab7e3d3236d135a6b7af94e0dc822cf391fb3fb898d0`. Replace `3d360deb200945776a9ae1ae5faea8cd075900d0b9cd7d2bab715b309e3f0b91` with it in `GoldenRun.cs` and `DeterminismTests.cs`, and update `docs/determinism.md`:

In `docs/determinism.md`:

1. Replace

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `3d360deb200945776a9ae1ae5faea8cd075900d0b9cd7d2bab715b309e3f0b91` |

```

   with

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `e2fa38969b15171a95d6ab7e3d3236d135a6b7af94e0dc822cf391fb3fb898d0` |

```

2. Replace

```markdown
| M3 Task 3 | 1950 | `3d360deb` | CBS immigration for 1950 adds January-February 1950 arrivals inside the run |
```

   with

```markdown
| M3 Task 3 | 1950 | `3d360deb` | CBS immigration for 1950 adds January-February 1950 arrivals inside the run |
| M3 Task 4 | 1950 | `e2fa3896` | frumkin_commission (window from 1950-01-01) fires inside the run |
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   321`. A loader error here names the event file and field (for example a placeholder whose office has no holder inside the window); fix the YAML, not the loader.

- [ ] **Step 6: Commit**

```bash
git add content/events/clothing_rationing_1950.yaml \
  content/events/education_registration_1951.yaml \
  content/events/forex_crisis_1951.yaml \
  content/events/frumkin_commission.yaml \
  content/events/hula_drainage.yaml \
  content/events/iraqi_exodus.yaml \
  content/events/israel_bonds.yaml \
  content/events/law_of_return.yaml \
  content/events/maabarot_policy.yaml \
  content/events/ministry_supply_crisis.yaml \
  content/events/reparations_claim.yaml \
  content/model.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim.Tests/RealContentTests.cs \
  sim/Statecraft.Sim/GoldenRun.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "content: eleven sourced events for 1950-51

Golden 1950 hash moves to e2fa3896: frumkin_commission fires inside the run.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 5: Economy calibration, the foreign-currency consequence, and the 1952 golden

*Track A.* Ruling 4 moves economy calibration to M3: today the historical autoplay ends 1953 with reserves at -230 and no consequence. Three changes, each pinned by a test: (1) `rules.yaml` gets units (IL millions; US$ millions for foreign currency) and parameters derived from sourced anchors (the IL 60m 1950/51 budget, Michaely's internal-debt rise of IL 98.4m over 1950–51, exports covering under a third of imports, and reserves running out in autumn 1951); (2) a new `capital_inflow_per_year` (donations and loans) and a consequence: while reserves are exhausted, food imports are cut (`fx_crisis_food_per_year`); (3) `HistoricalAI` sends reserves home to the security level's minimum, as the army demobilised after the armistices (the autoplay kept 80% called up forever). The calibration test labels each anchor the way M1's population test does: the four the parameters were derived or tuned from are consistency checks, so M3 has no fully independent economy anchor; one loose independent check compares the 1950–51 rise in external debt (Michaely: IL 65.7m) with the model's foreign inflow. `EffectsSystem.BudgetFactor` gains an overload over any shares, so the Budget screen (Task 17) uses the sim's rule instead of copying it (amendment A1). The second golden run, to 1952-01-01, is added here, after all historical content exists (ruling 4).

**Files:**
- Create: `sim/Statecraft.Sim.Tests/EconomyCalibrationTests.cs`, `sim/Statecraft.Sim.Tests/EconomyM3Tests.cs`
- Modify: `content/rules.yaml`, `docs/determinism.md`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim/Content.cs`, `sim/Statecraft.Sim/ContentLoader.cs`, `sim/Statecraft.Sim/EconomySystem.cs`, `sim/Statecraft.Sim/EffectsSystem.cs`, `sim/Statecraft.Sim/EventSystem.cs`, `sim/Statecraft.Sim/GoldenRun.cs`, `sim/Statecraft.Sim/HistoricalAI.cs`, `unity/Assets/Tests/EditMode/SimSmokeTests.cs`

**Interfaces:**
- Consumes: `EconomySystem.Apply`, `EffectsSystem.Level/Inner`, `HistoricalAI.Commands`, `GoldenRun.Play`.
- Produces:
  - `Rules.CapitalInflowPerYear` (`capital_inflow_per_year`), `Rules.FxCrisisFoodPerYear` (`fx_crisis_food_per_year`, must be <= 0 and needs a `food_supply` indicator).
  - `public static double EffectsSystem.Shift(Content c, GameState s, string indicator, double delta)` returns the visible change; `EventSystem` uses it (no behaviour change) and Task 8 reuses it.
  - `public static double EffectsSystem.BudgetFactor(IReadOnlyDictionary<string, double> budget, string office)` (the `GameState` overload calls it).
  - `HistoricalAI.Commands` appends `SetMobilization(rung minimum)` when the call-up is above it.
  - `GoldenRun.Hash1952 = "970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a"`, `GoldenRun.Until1952 = 1952-01-01`; `Hash1950` becomes `9cd2f4b5dc01cf4f6159b958eac9e815e7635f4589d2822793c20c527a3333fb`.

- [ ] **Step 1: Write the failing tests**

Create `sim/Statecraft.Sim.Tests/EconomyM3Tests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>M3 economy rules on the fixture: donations and loans, the food cut during a currency crisis, and the
/// historical AI sending reserves home.</summary>
public class EconomyM3Tests
{
    static Content Load(string rulesExtra) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"] + rulesExtra }));

    static GameState Fresh(Content c)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EffectsSystem.Init(c, s);
        EconomySystem.Init(c, s);
        s.Population["veterans"] = 1000;
        return s;
    }

    [Fact]
    public void DonationsAndLoansAddForeignCurrency()
    {
        var c = Load("capital_inflow_per_year: 8\n");
        var s = Fresh(c);
        var why = new List<Cause>();
        EconomySystem.Apply(c, s, 0.25, why);
        Assert.Equal(50 + 5 + 2 - 0.025, s.FxReserves, 9);   // exports 20/4, inflow 8/4, imports 0.0001*1000/4
        Assert.Contains(why, w => w.Target == "fx_reserves" && w.Message.Contains("donations and loans 2.00"));
    }

    [Fact]
    public void ACurrencyCrisisCutsFoodImports()
    {
        var c = Load("fx_crisis_food_per_year: -0.4\n");
        var s = Fresh(c);
        s.FxReserves = -20;                                     // still negative after a quarter of exports (+5)
        var why = new List<Cause>();
        EconomySystem.Apply(c, s, 0.25, why);
        Assert.Contains(EconomySystem.FxCrisis, s.ActiveSituations);
        Assert.Equal(0.4 - 0.1, s.Indicators["food_supply"], 9);
        Assert.Equal(-0.1, s.IndicatorShifts["food_supply"], 9);
        Assert.Contains(why, w => w.Target == "food_supply" && w.Message == "food_supply -0.100 (foreign currency crisis: food imports cut)");
    }

    [Fact]
    public void NoCrisisNoFoodCut()
    {
        var c = Load("fx_crisis_food_per_year: -0.4\n");
        var s = Fresh(c);
        EconomySystem.Apply(c, s, 0.25, new List<Cause>());
        Assert.Equal(0.4, s.Indicators["food_supply"], 9);
    }

    [Fact]
    public void APositiveFoodCutIsALoadError()
    {
        var ex = Assert.Throws<ContentException>(() => Load("fx_crisis_food_per_year: 0.1\n"));
        Assert.Equal("fx_crisis_food_per_year", ex.Field);
    }

    [Fact]
    public void TheBudgetFactorRuleWorksOnAnyShares()
    {
        var draft = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.3, ["c"] = 0.2 };
        Assert.Equal(1.25, EffectsSystem.BudgetFactor(draft, "a"), 9);     // 0.5 + 0.5 * 3 / 2
        Assert.Equal(0.8, EffectsSystem.BudgetFactor(draft, "c"), 9);
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(draft, "z"), 9);      // no line: neutral
        var s = new GameState { Budget = draft };
        Assert.Equal(EffectsSystem.BudgetFactor(draft, "b"), EffectsSystem.BudgetFactor(s, "b"));
    }

    [Fact]
    public void HistoricalAiSendsReservesHomeToTheRungMinimum()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Assert.Empty(HistoricalAI.Commands(c, s));                 // at war: the call-up is already the rung-5 minimum
        s.Rung = 2;
        var order = Assert.IsType<SetMobilization>(HistoricalAI.Commands(c, s).Single());
        Assert.Equal(0.05, order.Level);
    }
}
```

Create `sim/Statecraft.Sim.Tests/EconomyCalibrationTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>
/// Plays the historical choices to 1952 and compares the economy with sourced anchors (rules.yaml cites each).
///
/// ANCHOR INDEPENDENCE (as in CalibrationTests): rules.yaml derives or tunes a parameter from each of the first four
/// anchors, so they are CONSISTENCY checks that the derivation holds in play:
/// - internal debt rose IL 98.4m over 1950-51 (Michaely 1975, Table 5-2: 65.4 + 33.0): spending_per_capita is derived from it;
/// - the FY 1950/51 ordinary budget was IL 60m (en.wiki "1950 in Israel"): tax_per_capita is derived from it;
/// - exports covered under a third of imports (spec section 3): exports and imports were chosen to satisfy it;
/// - foreign currency ran out in autumn 1951 (FRUS 1951 v.V doc 513): capital_inflow_per_year is tuned to it.
/// M3 has NO fully independent economy anchor. The one loose INDEPENDENT check: external debt rose IL 65.7m over 1950-51
/// (Michaely Table 5-2: 22.6 + 43.1), which nothing in rules.yaml was derived from (see its test for the tolerance).
/// M4 adds independent anchors (GNP +25% 1950 to 1951, the FY1951/52 instalments).
/// </summary>
public class EconomyCalibrationTests
{
    const double DebtTolerance = 0.15;
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly List<GameState> History = Play();

    static List<GameState> Play()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var h = new List<GameState> { s };
        while (s.Date <= new DateTime(1952, 1, 1) && s.GameOver == null)
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            h.Add(s);
        }
        return h;
    }

    /// <summary>A state value on a date, linearly interpolated between the two turns around it.</summary>
    static double On(DateTime date, Func<GameState, double> value)
    {
        var after = History.First(s => s.Date >= date);
        var before = History.Last(s => s.Date <= date);
        if (after.Date == before.Date) return value(after);
        var t = (date - before.Date).TotalDays / (after.Date - before.Date).TotalDays;
        return value(before) + t * (value(after) - value(before));
    }

    static string F(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    [Fact]
    public void ThePlaythroughSurvivesTo1952()
    {
        var last = History[History.Count - 1];
        Assert.True(last.GameOver == null, "game over: " + last.GameOver);
        Assert.True(last.CoalitionStability > PoliticsSystem.CollapseBelow);
    }

    [Fact]
    public void BorrowingIn1950And1951MatchesTheRiseInInternalDebt()
    {
        const double michaely = 65.4 + 33.0;
        var rise = On(new DateTime(1952, 1, 1), s => s.Debt) - On(new DateTime(1950, 1, 1), s => s.Debt);
        Assert.True(Math.Abs(rise - michaely) / michaely <= DebtTolerance,
            $"[consistency: spending_per_capita_per_year is derived from this anchor] debt rose {F(rise)}m in 1950-51; Michaely: {F(michaely)}m");
    }

    [Fact]
    public void TaxesAtMidFiscalYear1950_51FundTheIl60mBudget()
    {
        var perYear = C.Rules.TaxPerCapitaPerYear * On(new DateTime(1950, 10, 1), s => s.TotalPopulation);
        Assert.True(Math.Abs(perYear - 60) / 60 <= DebtTolerance,
            $"[consistency: tax_per_capita_per_year is derived from this anchor] tax revenue {F(perYear)}m a year; budget IL 60m");
    }

    [Fact]
    public void ExportsCoverUnderAThirdOfImportsIn1950And1951()
    {
        foreach (var s in History.Where(s => s.Date.Year == 1950 || s.Date.Year == 1951))
        {
            var ratio = C.Rules.ExportsPerYear / (C.Rules.ImportsPerCapitaPerYear * s.TotalPopulation);
            Assert.True(ratio < 1.0 / 3, $"[consistency: exports and imports were chosen to satisfy this] {s.Date:yyyy-MM-dd}: exports cover {ratio:P0} of imports");
        }
    }

    [Fact]
    public void ForeignCurrencyRunsOutInTheLastHalfOf1951()
    {
        var first = History.FirstOrDefault(s => s.ActiveSituations.Contains(EconomySystem.FxCrisis));
        Assert.True(first != null, "[consistency: capital_inflow_per_year is tuned to this anchor] reserves never ran out");
        Assert.True(first!.Date > new DateTime(1951, 7, 1) && first.Date <= new DateTime(1952, 1, 1),
            $"[consistency: capital_inflow_per_year is tuned to this anchor] reserves ran out on {first.Date:yyyy-MM-dd}; historically in October 1951");
        Assert.All(History.Where(s => s.Date < first.Date), s => Assert.True(s.FxReserves >= 0, $"{s.Date:yyyy-MM-dd}: fx {F(s.FxReserves)}"));
    }

    /// <summary>[independent, loose] External debt rose IL 22.6m + 43.1m = 65.7m over 1950-51 (Michaely Table 5-2), which
    /// at the formal IL 0.36 per dollar is $182.5m. The model does not split loans from donations: its foreign currency
    /// from abroad (capital_inflow_per_year plus the event grants and bond sales in the run) is loans + donations, and
    /// donations (the UJA, transfers) are not debt. So Michaely's loans must be covered by the model's inflow
    /// (ratio >= 1), and the inflow must stay within three times the loans (ratio <= 3): the research notes give no
    /// donation total, so the band only guards the order of magnitude. Tightening it needs the donation series (M4).</summary>
    [Fact]
    public void ForeignInflowIsOfTheOrderOfTheRiseInExternalDebt()
    {
        const double externalDebtUsd = (22.6 + 43.1) / 0.36;
        double inflow = 0;
        for (int i = 1; i < History.Count; i++)
        {
            var (a, b) = (History[i - 1], History[i]);
            if (a.Date < new DateTime(1950, 1, 1) || a.Date >= new DateTime(1952, 1, 1)) continue;
            var yf = SimTime.YearFraction(SecuritySystem.TurnLengthFor(a.Rung));
            var trade = C.Rules.ExportsPerYear * yf - C.Rules.ImportsPerCapitaPerYear * a.TotalPopulation * yf;
            inflow += (b.FxReserves - a.FxReserves) - trade;     // everything that reached the reserves except trade
        }
        var ratio = inflow / externalDebtUsd;
        Assert.True(ratio >= 1 && ratio <= 3,
            $"[independent, loose] foreign inflow {F(inflow)} $m in 1950-51 is {ratio:0.00}x the rise in external debt ({F(externalDebtUsd)} $m); expected 1-3x");
    }

    [Fact]
    public void ReservesGoHomeAfterTheArmistices()
    {
        var s = History.First(x => x.Date >= new DateTime(1950, 1, 1));
        Assert.Equal(2, s.Rung);
        Assert.Equal(C.Rules.RungMinMobilization[1], s.Mobilization);
    }
}
```

Replace the whole of `sim/Statecraft.Sim.Tests/DeterminismTests.cs` with:

```csharp
using System;
using Statecraft.Sim;

/// <summary>Fixed reference values for the generator and for the golden historical playthroughs. See docs/determinism.md.</summary>
public class DeterminismTests
{
    /// <summary>The first three outputs of Vigna's reference splitmix64.c (https://prng.di.unimi.it/splitmix64.c)
    /// for each seed, taken from the reference C code, not from this implementation.</summary>
    [Theory]
    [InlineData(0UL, 0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL)]
    [InlineData(1234567UL, 6457827717110365317UL, 3203168211198807973UL, 9817491932198370423UL)]
    public void SplitMix64MatchesTheReferenceVectors(ulong seed, ulong first, ulong second, ulong third)
    {
        var rng = new Rng(seed);
        Assert.Equal(first, rng.NextULong());
        Assert.Equal(second, rng.NextULong());
        Assert.Equal(third, rng.NextULong());
    }

    /// <summary>Any change to this value is a deliberate rules or content change: update it here, in
    /// GoldenRun.Hash1950 and in docs/determinism.md in the same commit.</summary>
    public const string GoldenHash = "9cd2f4b5dc01cf4f6159b958eac9e815e7635f4589d2822793c20c527a3333fb";

    /// <summary>Real content, seed 1948, Historical mode, HistoricalAI until the date reaches 1950-01-01.</summary>
    public static GameState GoldenPlaythrough(Content c) => GoldenRun.Play(c, GoldenRun.Until1950);

    [Fact]
    public void GoldenPlaythroughHashIsFixed()
    {
        var s = GoldenPlaythrough(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.Null(s.GameOver);
        Assert.True(s.Date >= GoldenRun.Until1950);
        Assert.Equal(GoldenHash, s.Hash());
    }

    /// <summary>Same rule for the second golden run, which covers the M3 content range (to 1952-01-01).</summary>
    public const string GoldenHash1952 = "970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a";

    [Fact]
    public void GoldenPlaythroughTo1952HashIsFixed()
    {
        var s = GoldenRun.Play(ContentLoader.Load(RealContentTests.ContentRoot()), GoldenRun.Until1952);
        Assert.Null(s.GameOver);
        Assert.Equal(new DateTime(1952, 2, 26), s.Date);
        Assert.Equal(GoldenHash1952, s.Hash());
    }

    /// <summary>The library constants Unity players compare against are the values these tests pin.</summary>
    [Fact]
    public void LibraryGoldenConstantsMatchThisTest()
    {
        Assert.Equal(GoldenHash, GoldenRun.Hash1950);
        Assert.Equal(GoldenHash1952, GoldenRun.Hash1952);
    }
}
```

Replace the whole of `unity/Assets/Tests/EditMode/SimSmokeTests.cs` with:

```csharp
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

    [Test]
    public void GoldenPlaythroughTo1952MatchesDotNet()
    {
        var s = GoldenRun.Play(ContentLoader.Load(Paths.Content), GoldenRun.Until1952);
        Assert.IsNull(s.GameOver);
        Assert.AreEqual(GoldenRun.Hash1952, s.Hash());
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: the build fails: `error CS0117: 'GoldenRun' does not contain a definition for 'Hash1952'`.

- [ ] **Step 3: Implement the rules**

In `sim/Statecraft.Sim/Content.cs`:

1. Replace

```csharp
        public double ImportsPerCapitaPerYear { get; set; }
        public double MobilizationCostPerYear { get; set; }
```

   with

```csharp
        public double ImportsPerCapitaPerYear { get; set; }
        /// <summary>Foreign currency arriving each year without exports (donations, loans, transfers), in the units of fx_start.</summary>
        public double CapitalInflowPerYear { get; set; }
        /// <summary>Change to food_supply per year while the foreign currency crisis lasts (imports of food are cut); &lt;= 0.</summary>
        public double FxCrisisFoodPerYear { get; set; }
        public double MobilizationCostPerYear { get; set; }
```

In `sim/Statecraft.Sim/ContentLoader.cs`:

1. Replace

```csharp
                ("spending_per_capita_per_year", r.SpendingPerCapitaPerYear), ("exports_per_year", r.ExportsPerYear), ("imports_per_capita_per_year", r.ImportsPerCapitaPerYear),
                ("mobilization_cost_per_year", r.MobilizationCostPerYear), ("natural_increase_per_year", r.NaturalIncreasePerYear) })
                Finite("rules.yaml", key, v);
            foreach (var kv in r.InitialPopulation) Count("rules.yaml", $"initial_population.{kv.Key}", kv.Value);
```

   with

```csharp
                ("spending_per_capita_per_year", r.SpendingPerCapitaPerYear), ("exports_per_year", r.ExportsPerYear), ("imports_per_capita_per_year", r.ImportsPerCapitaPerYear),
                ("mobilization_cost_per_year", r.MobilizationCostPerYear), ("natural_increase_per_year", r.NaturalIncreasePerYear),
                ("capital_inflow_per_year", r.CapitalInflowPerYear), ("fx_crisis_food_per_year", r.FxCrisisFoodPerYear) })
                Finite("rules.yaml", key, v);
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
            if (r.FxCrisisFoodPerYear < 0 && !indicators.Contains("food_supply")) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "needs a food_supply indicator in model.yaml");
            foreach (var kv in r.InitialPopulation) Count("rules.yaml", $"initial_population.{kv.Key}", kv.Value);
```

In `sim/Statecraft.Sim/EffectsSystem.cs`:

1. Replace

```csharp
        /// treating it as unfunded would halve its effects permanently with no way to fix it.</summary>
        public static double BudgetFactor(GameState s, string office)
        {
            if (!s.Budget.TryGetValue(office, out var share)) return 1.0;
            return Math.Min(1.5, 0.5 + share * s.Budget.Count / 2.0);
        }
```

   with

```csharp
        /// treating it as unfunded would halve its effects permanently with no way to fix it.</summary>
        public static double BudgetFactor(GameState s, string office) => BudgetFactor(s.Budget, office);

        /// <summary>The same rule for any set of shares (the Budget screen shows the factor of a draft budget).</summary>
        public static double BudgetFactor(IReadOnlyDictionary<string, double> budget, string office)
        {
            if (!budget.TryGetValue(office, out var share)) return 1.0;
            return Math.Min(1.5, 0.5 + share * budget.Count / 2.0);
        }
```

2. Replace

```csharp

        public static void UpdateSituations(Content c, GameState s, List<Cause> why)
```

   with

```csharp

        /// <summary>Moves an indicator's level by <paramref name="delta"/> through its permanent shift (the layer event
        /// choices and crises use), clamped to [0, 1]. The shift is set so the level lands exactly on the visible result.
        /// Returns the change actually applied (0 when the indicator was already at the clamp).</summary>
        public static double Shift(Content c, GameState s, string indicator, double delta)
        {
            var ind = c.Indicators.First(i => i.Id == indicator);
            var level = Level(c, s, ind);
            var newLevel = Math.Max(0, Math.Min(1, level + delta));
            s.IndicatorShifts[ind.Id] = newLevel - Inner(c, s, ind);
            s.Indicators[ind.Id] = newLevel;
            return newLevel - level;
        }

        public static void UpdateSituations(Content c, GameState s, List<Cause> why)
```

In `sim/Statecraft.Sim/EventSystem.cs`:

1. Replace

```csharp
                else if (fx.Target == "fx_reserves") s.FxReserves += fx.Delta;
                else
                {
                    // The shift is set so the level lands exactly on the visible result; it never exceeds the clamp.
                    var ind = c.Indicators.First(i => i.Id == fx.Target);
                    var level = EffectsSystem.Level(c, s, ind);
                    var newLevel = Math.Max(0, Math.Min(1, level + fx.Delta));
                    s.IndicatorShifts[ind.Id] = newLevel - EffectsSystem.Inner(c, s, ind);
                    s.Indicators[ind.Id] = newLevel;
                    change = newLevel - level;
                }
                why.Add(new Cause("events", fx.Target, change, FormattableString.Invariant($"{fx.Target} {change:+0.00;-0.00;+0.00} ({eventId}/{o.Id})")));
```

   with

```csharp
                else if (fx.Target == "fx_reserves") s.FxReserves += fx.Delta;
                else change = EffectsSystem.Shift(c, s, fx.Target, fx.Delta);
                why.Add(new Cause("events", fx.Target, change, FormattableString.Invariant($"{fx.Target} {change:+0.00;-0.00;+0.00} ({eventId}/{o.Id})")));
```

In `sim/Statecraft.Sim/EconomySystem.cs`:

1. Replace

```csharp
            var exports = r.ExportsPerYear * yearFraction;
            var imports = r.ImportsPerCapitaPerYear * pop * yearFraction;
            s.FxReserves += exports - imports;
            why.Add(new Cause("economy", "fx_reserves", exports - imports, FormattableString.Invariant($"foreign currency {exports - imports:+0.00;-0.00;+0.00} (exports {exports:0.00}, imports {imports:0.00})")));

            bool on = s.ActiveSituations.Contains(FxCrisis);
            if (!on && s.FxReserves < 0) { s.ActiveSituations.Add(FxCrisis); why.Add(new Cause("economy", FxCrisis, 1, "Foreign currency reserves are exhausted")); }
            else if (on && s.FxReserves > 0.25 * r.FxStart) { s.ActiveSituations.Remove(FxCrisis); why.Add(new Cause("economy", FxCrisis, -1, "Foreign currency crisis eased")); }
        }
```

   with

```csharp
            var exports = r.ExportsPerYear * yearFraction;
            var inflow = r.CapitalInflowPerYear * yearFraction;
            var imports = r.ImportsPerCapitaPerYear * pop * yearFraction;
            var net = exports + inflow - imports;
            s.FxReserves += net;
            why.Add(new Cause("economy", "fx_reserves", net, FormattableString.Invariant($"foreign currency {net:+0.00;-0.00;+0.00} (exports {exports:0.00}, donations and loans {inflow:0.00}, imports {imports:0.00})")));

            bool on = s.ActiveSituations.Contains(FxCrisis);
            if (!on && s.FxReserves < 0) { s.ActiveSituations.Add(FxCrisis); why.Add(new Cause("economy", FxCrisis, 1, "Foreign currency reserves are exhausted")); }
            else if (on && s.FxReserves > 0.25 * r.FxStart) { s.ActiveSituations.Remove(FxCrisis); why.Add(new Cause("economy", FxCrisis, -1, "Foreign currency crisis eased")); }

            // The consequence: with no currency to pay for them, food imports are cut while the crisis lasts.
            if (s.ActiveSituations.Contains(FxCrisis) && r.FxCrisisFoodPerYear < 0)
            {
                var change = EffectsSystem.Shift(c, s, "food_supply", r.FxCrisisFoodPerYear * yearFraction);
                if (change != 0)
                    why.Add(new Cause("economy", "food_supply", change, FormattableString.Invariant($"food_supply {change:+0.000;-0.000;+0.000} (foreign currency crisis: food imports cut)")));
            }
        }
```

Replace the whole of `sim/Statecraft.Sim/HistoricalAI.cs` with:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Plays what really happened: picks each pending event's historical option (else its first), then sends
    /// reserves home down to the current security level's minimum call-up (the army demobilised after the 1949
    /// armistices). It sends no budget allocation, so the budget stays at rules.yaml budget_start.</summary>
    public static class HistoricalAI
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var list = s.PendingEvents
                .Select(id => c.Events.First(e => e.Id == id))
                .Select(e => (Command)new ChooseEventOption(e.Id, (e.Options.FirstOrDefault(o => o.Historical) ?? e.Options[0]).Id))
                .ToList();
            var floor = c.Rules.RungMinMobilization[Math.Max(1, Math.Min(5, s.Rung)) - 1];
            if (s.Mobilization > floor) list.Add(new SetMobilization(floor));
            return list;
        }
    }
}
```

- [ ] **Step 4: Calibrate `rules.yaml`**

Replace the whole of `content/rules.yaml` with:

```yaml
# Game rules. Values are game balance, except where a comment cites a source.
start: 1948-05-15                 # first day covered by the CBS 1948 immigration series (aliyah.yaml)
start_rung: 5                     # the war is under way when the slice opens
attention_per_turn: 4
attention_cost: { set_policy: 1, allocate_budget: 2, choose_event_option: 1, set_mobilization: 1, security_order: 1 }
rung_attention_step: 0.25
rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
ceasefire_weeks: 52               # long enough that the historical armistice events end the war first
# ---- Economy (calibrated in M3; checked by sim/Statecraft.Sim.Tests/EconomyCalibrationTests.cs) ----
# Units: treasury and debt in IL millions; fx_start, exports, donations/loans and imports in US$ millions.
# Formal rate 1950-51: IL 0.36 per US$ (IL 1 = $2.80), Michaely, Foreign Trade Regimes and Economic Development: Israel
# (NBER 1975) Table 5-1, https://www.nber.org/system/files/chapters/c4560/c4560.pdf (checked 2026-10-10).
treasury_start: 20
fx_start: 40
# Taxes finance the ordinary budget: FY 1950/51 IL 60 million, approved 29 June 1950
# (https://en.wikipedia.org/wiki/1950_in_Israel, checked 2026-10-10). 60m / ~1.10m Jews (CBS average 1950) = 0.0000545.
tax_per_capita_per_year: 0.0000545
# Spending above taxes is borrowed. The gap is set so 1950-51 borrowing matches the rise in internal debt, IL 65.4m (1950)
# + 33.0m (1951) = 98.4m (Michaely Table 5-2, same chapter). The 1951 fiscal tightening is not modelled, so the model
# matches the two-year total, not each year: (98.4m - 6m reserve call-up) / ~2.40m person-years = 0.0000385 per person.
spending_per_capita_per_year: 0.000093
# Exports cover under a third of imports (spec section 3; EconomyCalibrationTests checks it every turn of 1950-51).
exports_per_year: 40
imports_per_capita_per_year: 0.00025
# Donations, loans and transfers, set so reserves run out in the last quarter of 1951: the October 1951 crisis
# (FRUS 1951 v.V doc 513, https://history.state.gov/historicaldocuments/frus1951v05/d513, checked 2026-10-10).
capital_inflow_per_year: 192
# While reserves are exhausted, food imports are cut: food_supply falls by this much per year.
fx_crisis_food_per_year: -0.2
mobilization_cost_per_year: 60
# Derived from CBS table 2.1 (1949): (end-1949 1,013.9k - end-1948 758.7k - 1949 immigrants 239,954) / average 1949 population 901.0k.
natural_increase_per_year: 0.0169
# Derived: CBS end-1948 Jewish population 758,700 minus 101,828 immigrants (15 May-31 Dec 1948) minus natural increase over
# 7.5 months at the rate above. Checked against the 8 Nov 1948 census by the calibration test.
# Arithmetic: 758,700 - 101,828 = 656,872. Natural-increase base = average of 656,872 and 758,700 = 707,786.
# Natural increase = 707,786 x 0.0169 x 7.5/12 = 7,476. 656,872 - 7,476 = 649,396, rounded to the nearest hundred = 649,400.
initial_population: { veterans: 649400 }
budget_start:
  defense_minister: 0.45
  finance_minister: 0.05
  # Absorption was handled by the Jewish Agency and the Prime Minister's office (notes, Addendum (a)); the Ministry of
  # Immigration ends on 1951-10-08, so its budget line sits with the Prime Minister (the loader refuses a line for an
  # abolished office).
  prime_minister: 0.15
  labour_minister: 0.10
  health_minister: 0.07
  education_minister: 0.06
  agriculture_minister: 0.07
  interior_minister: 0.05
```

- [ ] **Step 5: Set the golden hashes and the history**

In `sim/Statecraft.Sim/GoldenRun.cs` (the whole file after this step):

Replace the whole of `sim/Statecraft.Sim/GoldenRun.cs` with:

```csharp
using System;

namespace Statecraft.Sim
{
    /// <summary>The reference playthroughs that pin determinism (docs/determinism.md): real content, seed 1948,
    /// Historical mode, HistoricalAI until the date reaches the given day. The constants live in the sim library so
    /// Unity players (Golden check) and the editor tests compare against the same values .NET asserts.</summary>
    public static class GoldenRun
    {
        public const ulong Seed = 1948;

        /// <summary>Final hash of Play(content, Until1950). Changing it is a deliberate rules or content change:
        /// update it, DeterminismTests and docs/determinism.md in the same commit and say why in the message.</summary>
        public const string Hash1950 = "9cd2f4b5dc01cf4f6159b958eac9e815e7635f4589d2822793c20c527a3333fb";

        public static readonly DateTime Until1950 = new DateTime(1950, 1, 1);

        /// <summary>Final hash of Play(content, Until1952): the M3 content range (to 31 December 1951).</summary>
        public const string Hash1952 = "970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a";

        public static readonly DateTime Until1952 = new DateTime(1952, 1, 1);

        /// <summary>Steps with HistoricalAI until Date >= <paramref name="until"/>, the game ends, or 1000 turns pass.</summary>
        public static GameState Play(Content c, DateTime until)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, Seed, Mode.Historical);
            for (int turn = 0; turn < 1000 && s.Date < until && s.GameOver == null; turn++)
                s = Simulation.Step(c, s, HistoricalAI.Commands(c, s)).State;
            return s;
        }
    }
}
```

In `docs/determinism.md`:

1. Replace

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `e2fa38969b15171a95d6ab7e3d3236d135a6b7af94e0dc822cf391fb3fb898d0` |

```

   with

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `9cd2f4b5dc01cf4f6159b958eac9e815e7635f4589d2822793c20c527a3333fb` |
| to 1952-01-01 (`Hash1952`) | 1952-02-26, turn 53 | `970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a` |

```

2. Replace

```markdown
| M3 Task 4 | 1950 | `e2fa3896` | frumkin_commission (window from 1950-01-01) fires inside the run |
```

   with

```markdown
| M3 Task 4 | 1950 | `e2fa3896` | frumkin_commission (window from 1950-01-01) fires inside the run |
| M3 Task 5 | 1950 | `9cd2f4b5` | economy calibrated (rules.yaml) and HistoricalAI sends reserves home after the armistices |
| M3 Task 5 | 1952 | `970ef315` | same |
| M3 Task 5 | 1952 | `970ef315` | new run covering the M3 content range |
```

- [ ] **Step 6: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   335`. The calibration margins (verified): debt rises 99m over 1950–51 (Michaely 98.4m), taxes 63m a year at mid FY1950/51 (budget 60m), exports cover 12–15% of imports, reserves first run out in the turn ending 1951-11-26, and foreign inflow over 1950–51 falls inside the 1–3 band against the rise in external debt (about 2.5 times).

If you change a parameter, `dotnet run --project sim/Statecraft.Cli -- --auto historical --turns 53` prints the end state: `End 1952-02-26: population 1,402,478, level 2, treasury 0.0, debt 202.3, fx -21.4, stability 0.48` and `State hash 970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a`.

- [ ] **Step 7: Run the Unity edit-mode tests**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="82" passed="82" failed="0"` (the new 1952 golden passes on editor Mono).

- [ ] **Step 8: Commit**

```bash
git add content/rules.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim.Tests/EconomyCalibrationTests.cs \
  sim/Statecraft.Sim.Tests/EconomyM3Tests.cs \
  sim/Statecraft.Sim/Content.cs \
  sim/Statecraft.Sim/ContentLoader.cs \
  sim/Statecraft.Sim/EconomySystem.cs \
  sim/Statecraft.Sim/EffectsSystem.cs \
  sim/Statecraft.Sim/EventSystem.cs \
  sim/Statecraft.Sim/GoldenRun.cs \
  sim/Statecraft.Sim/HistoricalAI.cs \
  unity/Assets/Tests/EditMode/SimSmokeTests.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(sim): calibrate the 1950-51 economy; currency crisis cuts food imports; reserves go home; 1952 golden

Golden 1950 hash moves to 9cd2f4b5 (rules and HistoricalAI demobilisation); new 1952 golden 970ef315.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 6: Appoint

*Track A.* Ruling 4: change a minister in a portfolio, at an Attention cost, with coalition and party effects, rejected with a reason when illegal. The player's appointments override the historical holder until the government changes (a new government arrives with its own ministers). Everything that asks "who holds this office" during play (effects' competence, delegation, event text) now asks `Cabinet.Holder`, so an appointee's competence and name take effect. Legality, in order: the office exists and is not abolished; it is not the Prime Minister's (the player) or the President's; the person exists, is alive, is not the sitting President, is not the serving Chief of Staff (for any other office), and does not already hold it; a government is in office; a Chief of Staff must have held the post before; a minister's party, if any, must be in the coalition.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/CabinetTests.cs`, `sim/Statecraft.Sim/Cabinet.cs`
- Modify: `content/rules.yaml`, `docs/determinism.md`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim.Tests/Fixture.cs`, `sim/Statecraft.Sim/AttentionSystem.cs`, `sim/Statecraft.Sim/Commands.cs`, `sim/Statecraft.Sim/Content.cs`, `sim/Statecraft.Sim/ContentLoader.cs`, `sim/Statecraft.Sim/EffectsSystem.cs`, `sim/Statecraft.Sim/EventSystem.cs`, `sim/Statecraft.Sim/GameState.cs`, `sim/Statecraft.Sim/GoldenRun.cs`, `sim/Statecraft.Sim/Simulation.cs`

**Interfaces:**
- Consumes: `Offices.Holder/Government/PartyOf/Placeholder`, `AttentionSystem` kinds, `Simulation.Execute`, `EffectsSystem.Multiplier`, `EventSystem.Trigger/Resolve/Delegate`.
- Produces:
  - `public sealed class Appoint : Command { Appoint(string office, string person); string Office; string Person; Kind == "appoint" }`.
  - `public sealed class Appointment { string Person; DateTime From; }`, `public sealed class Seat { Office Office; Person? Holder; bool Appointed; }`.
  - `public static class Cabinet { const string Kind = "appoint"; Person? Holder(Content, GameState, string office, DateTime? date = null); string Fill(Content, GameState, string text, DateTime date); IReadOnlyList<Seat> Lineup(Content, GameState); string? Check(Content, GameState, string office, string person); IReadOnlyList<(Person person, string? reason)> Candidates(Content, GameState, string office); string? Appoint(Content, GameState, string office, string person, List<Cause>); void ExpireOnGovernmentChange(Content, GameState, List<Cause>); }`.
  - `public static string EventSystem.Render(Content c, GameState s, GameEvent e)` (text as the player sees it now).
  - `GameState.Appointments : Dictionary<string, Appointment>`, `GameState.AppointedUnder : string`, both in `Hash()`.
  - Rules: `attention_cost.appoint`, `appoint_stability_cost_other_party`, `appoint_stability_cost_same_party` (0..1).
  - Refusal texts (in check order): `Unknown office '<id>'`, `The office of <title> was abolished on <date>`, `You are the Prime Minister: that office is not yours to fill`, `The President is elected by the Knesset, not appointed`, `Unknown person '<id>'`, `<name> died on <date>`, `<name> is the President`, `<name> is the serving Chief of Staff`, `<name> already holds the office of <title>`, `No government is in office`, `<name> is not an IDF career officer`, `<name>'s party (<party>) is not in the coalition`.
  - An appointee who dies leaves office on the date of death (`Cabinet.Holder` falls back to the historical holder).
  - Hashes: `Hash1950 = 9a546a6f62acdec3d3acc1621d90e9863e8689f9a199a792a458bc8f1b6a3f35`, `Hash1952 = 8db2b892cae1c4462fef93cb6a25096b953db0dd259f18551de496774722cf08` (the hash format gains the appointment fields).

- [ ] **Step 1: Write the failing tests**

Create `sim/Statecraft.Sim.Tests/CabinetTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The Appoint command: who may be appointed, what it costs, and how long it lasts.</summary>
public class CabinetTests
{
    const string ExtraPeople = @"
  - id: outsider
    name: Out Sider
    living: false
    party: beta
    competence: 9
    loyalty: 3
    offices: []
  - id: loyalist
    name: Loy Alist
    living: false
    party: alpha
    competence: 2
    loyalty: 9
    offices: []
  - id: loyal_two
    name: Loyal Two
    living: false
    party: alpha
    competence: 6
    loyalty: 9
    offices: []
  - id: late
    name: Late Comer
    living: false
    died: 1948-07-01
    party: alpha
    competence: 5
    loyalty: 5
    offices: []
  - id: gone
    name: Gone Early
    living: false
    died: 1948-05-01
    competence: 5
    loyalty: 5
    offices: []
";

    static Content Load(Dictionary<string, string>? events = null) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = Fixture.Files["people.yaml"] + ExtraPeople }, events));

    static readonly Content C = Load();
    static readonly IReadOnlyList<Command> None = new Command[0];

    [Fact]
    public void AppointingReplacesTheMinisterAndCostsCoalitionStability()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        var why = new List<Cause>();
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "loyalist", why));
        Assert.Equal("loyalist", Cabinet.Holder(C, s, "finance_minister")!.Id);
        Assert.Equal("def_two", Offices.Holder(C, "finance_minister", s.Date)!.Id);   // history itself is unchanged
        Assert.Equal(0.9, s.CoalitionStability, 9);                                    // Def Two had no party: a seat lost
        Assert.Contains(why, w => w.System == "cabinet" && w.Message == "Loy Alist appointed Minister of Finance, replacing Def Two");
        Assert.Contains(why, w => w.Target == "coalition_stability" && w.Message == "coalition stability -0.10 (Def Two lost the Minister of Finance)");
    }

    [Fact]
    public void AReshuffleWithinAPartyCostsLess()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        var why = new List<Cause>();
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "loyal_two", why));
        Assert.Equal(0.9 - 0.03, s.CoalitionStability, 9);
        Assert.Contains(why, w => w.Message == "coalition stability -0.03 (Alpha resents the reshuffle)");
    }

    [Theory]
    [InlineData("prime_minister", "loyalist", "You are the Prime Minister: that office is not yours to fill")]
    [InlineData("finance_minister", "outsider", "Out Sider's party (Beta) is not in the coalition")]
    [InlineData("finance_minister", "gone", "Gone Early died on 1948-05-01")]
    [InlineData("finance_minister", "def_two", "Def Two already holds the office of Minister of Finance")]
    [InlineData("finance_minister", "nobody", "Unknown person 'nobody'")]
    [InlineData("treasurer", "loyalist", "Unknown office 'treasurer'")]
    public void IllegalAppointmentsAreRejectedWithAReason(string office, string person, string reason)
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        var before = s.Hash();
        Assert.Equal(reason, Cabinet.Appoint(C, s, office, person, new List<Cause>()));
        Assert.Equal(before, s.Hash());
    }

    [Fact]
    public void ARejectedAppointCommandIsRefundedAndReported()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "outsider") });
        Assert.Equal("Out Sider's party (Beta) is not in the coalition", r.Rejected.Single().Reason);
        Assert.Equal("def_two", Cabinet.Holder(C, r.State, "finance_minister")!.Id);
    }

    [Fact]
    public void ThePartyRuleFollowsTheCoalitionOfTheDay()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1949, 3, 10);                                   // Government Two: Alpha and Beta
        Assert.Null(Cabinet.Check(C, s, "finance_minister", "outsider"));
    }

    [Fact]
    public void AppointmentsEndWhenANewGovernmentTakesOffice()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s = Simulation.Step(C, s, new Command[] { new Appoint("finance_minister", "loyalist") }).State;
        Assert.Equal("loyalist", Cabinet.Holder(C, s, "finance_minister")!.Id);
        StepResult r;
        do { r = Simulation.Step(C, s, None); s = r.State; } while (Offices.Government(C, s.Date)!.Id == "gov1");
        Assert.Equal("def_two", Cabinet.Holder(C, s, "finance_minister")!.Id);
        Assert.Empty(s.Appointments);
        Assert.Contains(r.Why, w => w.Message == "A new government took office with its own ministers; your appointments ended");
    }

    [Fact]
    public void AnAppointeeWhoDiesLeavesOfficeOnTheDateOfDeath()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Null(Cabinet.Appoint(C, s, "finance_minister", "late", new List<Cause>()));
        Assert.Equal("late", Cabinet.Holder(C, s, "finance_minister", new DateTime(1948, 6, 30))!.Id);
        Assert.Equal("def_two", Cabinet.Holder(C, s, "finance_minister", new DateTime(1948, 7, 1))!.Id);
    }

    [Fact]
    public void TheAppointeesCompetenceDrivesTheMinistrysEffects()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Equal(0.7, EffectsSystem.Multiplier(C, s, "finance_minister"), 9);   // Def Two, competence 4
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        Assert.Equal(0.6, EffectsSystem.Multiplier(C, s, "finance_minister"), 9);   // Loy Alist, competence 2
    }

    [Fact]
    public void EventTextNamesTheAppointee()
    {
        var c = Load(new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event });
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Cabinet.Appoint(c, s, "finance_minister", "loyalist", new List<Cause>());
        var drought = c.Events.Single(e => e.Id == "drought");
        Assert.Equal("Loy Alist reports a drought.", EventSystem.Render(c, s, drought));
    }

    [Fact]
    public void TheLineupListsEveryExistingOfficeAndMarksAppointees()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Cabinet.Appoint(C, s, "finance_minister", "loyalist", new List<Cause>());
        var lineup = Cabinet.Lineup(C, s);
        Assert.Equal(new[] { "prime_minister", "defense_minister", "finance_minister" }, lineup.Select(x => x.Office.Id));
        Assert.True(lineup.Single(x => x.Office.Id == "finance_minister").Appointed);
        Assert.False(lineup.Single(x => x.Office.Id == "defense_minister").Appointed);
    }

    [Fact]
    public void AppointmentsAreInTheHashAndStepNeverMutatesItsInput()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var plain = Simulation.Step(C, s0, None).State;
        var appointed = Simulation.Step(C, s0, new Command[] { new Appoint("finance_minister", "loyalist") }).State;
        Assert.Empty(s0.Appointments);
        Assert.NotEqual(plain.Hash(), appointed.Hash());
    }

    [Fact]
    public void RealContentRules()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = new GameState { Date = new DateTime(1951, 1, 1) };
        Assert.Equal("Golda Meir is not an IDF career officer", Cabinet.Check(c, s, "chief_of_staff", "golda_meir"));
        Assert.Equal("The office of Minister of War Victims was abolished on 1951-10-08",
            Cabinet.Check(c, new GameState { Date = new DateTime(1951, 11, 1) }, "war_victims_minister", "maimon"));
        Assert.Equal("Aharon Zisling's party (Mapam) is not in the coalition", Cabinet.Check(c, s, "agriculture_minister", "zisling"));
        Assert.Equal("The President is elected by the Knesset, not appointed", Cabinet.Check(c, s, "president", "sharett"));
        Assert.Null(Cabinet.Check(c, s, "agriculture_minister", "eshkol"));
        Assert.Equal("Chaim Weizmann is the President", Cabinet.Check(c, s, "finance_minister", "weizmann"));
        Assert.Equal("Yigael Yadin is the serving Chief of Staff", Cabinet.Check(c, s, "finance_minister", "yadin"));
        s.Date = new DateTime(1951, 6, 1);
        Assert.Equal("David Remez died on 1951-05-19", Cabinet.Check(c, s, "transportation_minister", "remez"));
    }
}
```

In `sim/Statecraft.Sim.Tests/Fixture.cs`:

1. Replace

```csharp
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1 }
rung_attention_step: 0.25
```

   with

```csharp
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 1 }
appoint_stability_cost_other_party: 0.1
appoint_stability_cost_same_party: 0.03
rung_attention_step: 0.25
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: the build fails: `error CS0103: The name 'Cabinet' does not exist in the current context` and `error CS0246: The type or namespace name 'Appoint' could not be found`.

- [ ] **Step 3: Implement**

Create `sim/Statecraft.Sim/Cabinet.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>A player appointment: <see cref="Person"/> holds the office from <see cref="From"/> while the government
    /// that was in office then still is. Immutable, so GameState.Clone can copy the dictionary shallowly.</summary>
    public sealed class Appointment
    {
        public string Person { get; }
        public DateTime From { get; }
        public Appointment(string person, DateTime from) { Person = person; From = from; }
    }

    /// <summary>One office and who holds it now.</summary>
    public sealed class Seat
    {
        public Office Office { get; }
        public Person? Holder { get; }
        /// <summary>True when the holder is the player's appointee, not the historical minister.</summary>
        public bool Appointed { get; }
        public Seat(Office office, Person? holder, bool appointed) { Office = office; Holder = holder; Appointed = appointed; }
    }

    /// <summary>The cabinet as the game sees it: the historical holders from people.yaml, overridden by the player's
    /// appointments until the government changes (a new government takes office with its own, historical, ministers).</summary>
    public static class Cabinet
    {
        public const string Kind = "appoint";

        /// <summary>The player's appointee for the office on the date: appointed by then, same government, and alive (an
        /// appointee who dies leaves office on the date of death, and the historical holder applies again).</summary>
        static Appointment? Active(Content c, GameState s, string office, DateTime date)
        {
            if (!s.Appointments.TryGetValue(office, out var a) || date < a.From) return null;
            var died = c.People.First(p => p.Id == a.Person).Died;
            if (died.HasValue && date >= died.Value) return null;
            return Offices.Government(c, date)?.Id == s.AppointedUnder ? a : null;
        }

        /// <summary>Who holds <paramref name="office"/> on <paramref name="date"/> (default: the state's date).</summary>
        public static Person? Holder(Content c, GameState s, string office, DateTime? date = null)
        {
            var d = date ?? s.Date;
            var a = Active(c, s, office, d);
            return a != null ? c.People.First(p => p.Id == a.Person) : Offices.Holder(c, office, d);
        }

        /// <summary>Offices.Fill with the player's appointments: {office_id} becomes the name of whoever holds it.</summary>
        public static string Fill(Content c, GameState s, string text, DateTime date) =>
            Offices.Placeholder.Replace(text, m =>
                Holder(c, s, m.Groups[1].Value, date)?.Name
                ?? throw new InvalidOperationException(FormattableString.Invariant($"no holder of '{m.Groups[1].Value}' on {date:yyyy-MM-dd}")));

        /// <summary>Every office that exists on the state's date, in offices.yaml order, with its holder (null = vacant).</summary>
        public static IReadOnlyList<Seat> Lineup(Content c, GameState s) =>
            c.Offices.Where(o => !o.Until.HasValue || s.Date < o.Until.Value)
                .Select(o => new Seat(o, Holder(c, s, o.Id), Active(c, s, o.Id, s.Date) != null))
                .ToList();

        /// <summary>Null when <paramref name="personId"/> may be appointed to <paramref name="officeId"/> now, else the
        /// reason shown to the player. Checks, in order: the office exists and has not been abolished; it is not the
        /// Prime Minister's (the player) or the President's (elected by the Knesset); the person exists, is alive, is not
        /// the sitting President, is not the serving Chief of Staff (for any other office) and does not already hold it; a
        /// government is in office; a Chief of Staff must be a career officer (has held the post in people.yaml); a
        /// minister's party, if any, must be in the coalition.</summary>
        public static string? Check(Content c, GameState s, string officeId, string personId)
        {
            var office = c.Offices.FirstOrDefault(o => o.Id == officeId);
            if (office == null) return $"Unknown office '{officeId}'";
            if (office.Until.HasValue && s.Date >= office.Until.Value)
                return FormattableString.Invariant($"The office of {office.Title} was abolished on {office.Until:yyyy-MM-dd}");
            if (officeId == "prime_minister") return "You are the Prime Minister: that office is not yours to fill";
            if (officeId == "president") return "The President is elected by the Knesset, not appointed";
            var person = c.People.FirstOrDefault(p => p.Id == personId);
            if (person == null) return $"Unknown person '{personId}'";
            if (person.Died.HasValue && s.Date >= person.Died.Value)
                return FormattableString.Invariant($"{person.Name} died on {person.Died:yyyy-MM-dd}");
            if (Holder(c, s, "president")?.Id == personId) return $"{person.Name} is the President";
            if (officeId != "chief_of_staff" && Holder(c, s, "chief_of_staff")?.Id == personId) return $"{person.Name} is the serving Chief of Staff";
            if (Holder(c, s, officeId)?.Id == personId) return $"{person.Name} already holds the office of {office.Title}";
            var gov = Offices.Government(c, s.Date);
            if (gov == null) return "No government is in office";
            if (officeId == "chief_of_staff")
                return person.Offices.Any(t => t.Office == "chief_of_staff") ? null : $"{person.Name} is not an IDF career officer";
            var party = Offices.PartyOf(c, person, s.Date);
            if (party != "" && !gov.Coalition.Contains(party))
                return $"{person.Name}'s party ({c.Parties.First(p => p.Id == party).Name}) is not in the coalition";
            return null;
        }

        /// <summary>Every person with the reason they cannot be appointed (null = they can), in people.yaml order.</summary>
        public static IReadOnlyList<(Person person, string? reason)> Candidates(Content c, GameState s, string officeId) =>
            c.People.Select(p => (p, Check(c, s, officeId, p.Id))).ToList();

        /// <summary>Returns null and appoints, or the reason (state untouched). Replacing a minister costs coalition
        /// stability: rules appoint_stability_cost_other_party when the seat leaves the displaced minister's party (a
        /// non-party appointee counts as another party), appoint_stability_cost_same_party otherwise. A vacant seat costs
        /// nothing.</summary>
        public static string? Appoint(Content c, GameState s, string officeId, string personId, List<Cause> why)
        {
            var reason = Check(c, s, officeId, personId);
            if (reason != null) return reason;
            var gov = Offices.Government(c, s.Date)!;
            var office = c.Offices.First(o => o.Id == officeId);
            var person = c.People.First(p => p.Id == personId);
            var displaced = Holder(c, s, officeId);
            if (s.AppointedUnder != gov.Id) { s.Appointments.Clear(); s.AppointedUnder = gov.Id; }
            s.Appointments[officeId] = new Appointment(personId, s.Date);
            why.Add(new Cause("cabinet", officeId, 0, displaced == null
                ? $"{person.Name} appointed {office.Title}"
                : $"{person.Name} appointed {office.Title}, replacing {displaced.Name}"));
            if (displaced == null) return null;
            var oldParty = Offices.PartyOf(c, displaced, s.Date);
            var newParty = Offices.PartyOf(c, person, s.Date);
            bool sameParty = oldParty != "" && oldParty == newParty;
            var cost = sameParty ? c.Rules.AppointStabilityCostSameParty : c.Rules.AppointStabilityCostOtherParty;
            if (cost <= 0) return null;
            var before = s.CoalitionStability;
            s.CoalitionStability = Math.Max(0, before - cost);
            var who = oldParty == "" ? displaced.Name : c.Parties.First(p => p.Id == oldParty).Name;
            why.Add(new Cause("politics", "coalition_stability", s.CoalitionStability - before, FormattableString.Invariant(
                $"coalition stability {s.CoalitionStability - before:+0.00;-0.00;+0.00} ({who} {(sameParty ? "resents the reshuffle" : "lost the " + office.Title)})")));
            return null;
        }

        /// <summary>Called after the date advances: when a new government takes office, the player's appointments end.</summary>
        public static void ExpireOnGovernmentChange(Content c, GameState s, List<Cause> why)
        {
            if (s.AppointedUnder == "" || Offices.Government(c, s.Date)?.Id == s.AppointedUnder) return;
            if (s.Appointments.Count > 0)
                why.Add(new Cause("cabinet", "appointments", -s.Appointments.Count, "A new government took office with its own ministers; your appointments ended"));
            s.Appointments.Clear();
            s.AppointedUnder = "";
        }
    }
}
```

In `sim/Statecraft.Sim/Commands.cs`:

1. Replace

```csharp

    public sealed class Rejection
```

   with

```csharp

    /// <summary>Puts <see cref="Person"/> in <see cref="Office"/> (Cabinet.Appoint has the rules).</summary>
    public sealed class Appoint : Command
    {
        public override string Kind => Cabinet.Kind;
        public string Office { get; } public string Person { get; }
        public Appoint(string office, string person) { Office = office; Person = person; }
    }

    public sealed class Rejection
```

Replace the whole of `sim/Statecraft.Sim/AttentionSystem.cs` with:

```csharp
using System.Collections.Generic;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>The PM's per-turn capacity. Each command costs attention; costs rise with the security level.</summary>
    public static class AttentionSystem
    {
        static readonly HashSet<string> Kinds = new HashSet<string>
            { "set_policy", "allocate_budget", "choose_event_option", "set_mobilization", "security_order", "appoint" };

        public static IReadOnlyCollection<string> CommandKinds => Kinds;

        public static bool IsCommandKind(string kind) => Kinds.Contains(kind);

        public static void Refill(Content c, GameState s) => s.Attention = c.Rules.AttentionPerTurn;

        /// <summary>Cost at <paramref name="rung"/> if given, else at the state's current rung.</summary>
        public static double Cost(Content c, GameState s, string kind, int? rung = null) =>
            c.Rules.AttentionCost[kind] * (1 + c.Rules.RungAttentionStep * ((rung ?? s.Rung) - 1));

        /// <summary>Returns null and deducts on success, or the reason it could not be afforded.
        /// <paramref name="rung"/> prices the command at that security level (the turn's starting rung).</summary>
        public static string? TrySpend(Content c, GameState s, string kind, int? rung = null)
        {
            var cost = Cost(c, s, kind, rung);
            if (cost > s.Attention + 1e-9)
                return string.Format(CultureInfo.InvariantCulture, "Not enough attention (need {0:0.0}, have {1:0.0})", cost, s.Attention);
            s.Attention -= cost;
            return null;
        }
    }
}
```

In `sim/Statecraft.Sim/GameState.cs`:

1. Replace

```csharp
        public List<string> PendingEvents = new List<string>();
        public string? GameOver;
```

   with

```csharp
        public List<string> PendingEvents = new List<string>();
        /// <summary>Player appointments by office id; valid while the government in AppointedUnder is in office (Cabinet).</summary>
        public Dictionary<string, Appointment> Appointments = new Dictionary<string, Appointment>();
        public string AppointedUnder = "";
        public string? GameOver;
```

2. Replace

```csharp
            c.PendingEvents = new List<string>(PendingEvents);
            return c;
```

   with

```csharp
            c.PendingEvents = new List<string>(PendingEvents);
            c.Appointments = new Dictionary<string, Appointment>(Appointments);
            return c;
```

3. Replace

```csharp
            sb.Append("pending[").Append(string.Join(",", PendingEvents)).Append(']');
            sb.Append("over=").Append(GameOver ?? "");
```

   with

```csharp
            sb.Append("pending[").Append(string.Join(",", PendingEvents)).Append(']');
            sb.Append("appt{");
            foreach (var kv in Appointments.OrderBy(k => k.Key, StringComparer.Ordinal))
                sb.Append(kv.Key).Append('=').Append(kv.Value.Person).Append('@').Append(kv.Value.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';');
            sb.Append("}under=").Append(AppointedUnder).Append(';');
            sb.Append("over=").Append(GameOver ?? "");
```

In `sim/Statecraft.Sim/Simulation.cs`:

1. Replace

```csharp
            s.Turn++;
            AttentionSystem.Refill(c, s);
```

   with

```csharp
            s.Turn++;
            Cabinet.ExpireOnGovernmentChange(c, s, why);
            AttentionSystem.Refill(c, s);
```

2. Replace

```csharp
                    return SecuritySystem.SetMobilization(c, s, m.Level, why);
                case SecurityOrder o:
```

   with

```csharp
                    return SecuritySystem.SetMobilization(c, s, m.Level, why);
                case Appoint a:
                    return Cabinet.Appoint(c, s, a.Office, a.Person, why);
                case SecurityOrder o:
```

In `sim/Statecraft.Sim/EffectsSystem.cs`:

1. Replace

```csharp

        /// <summary>Competence (0.5 + competence/20) times budget factor. No office: 1.0.</summary>
        public static double Multiplier(Content c, GameState s, string office)
        {
            if (office == "") return 1.0;
            var holder = Offices.Holder(c, office, s.Date);
            var competence = holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
```

   with

```csharp

        /// <summary>Competence (0.5 + competence/20) of the office holder (player appointments included) times budget
        /// factor. No office: 1.0.</summary>
        public static double Multiplier(Content c, GameState s, string office)
        {
            if (office == "") return 1.0;
            var holder = Cabinet.Holder(c, s, office);
            var competence = holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
```

In `sim/Statecraft.Sim/EventSystem.cs`:

1. Replace

```csharp
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Offices.Fill(c, e.Title, FillDate(e, s.Date))}"));
            }
```

   with

```csharp
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Cabinet.Fill(c, s, e.Title, FillDate(e, s.Date))}"));
            }
```

2. Replace

```csharp
            var fillDate = FillDate(Get(c, eventId), s.Date);
            why.Add(new Cause("events", eventId, 0, $"{how}: {Offices.Fill(c, o.Text, fillDate)}"));
            foreach (var fx in o.Effects)
```

   with

```csharp
            var fillDate = FillDate(Get(c, eventId), s.Date);
            why.Add(new Cause("events", eventId, 0, $"{how}: {Cabinet.Fill(c, s, o.Text, fillDate)}"));
            foreach (var fx in o.Effects)
```

3. Replace

```csharp
                var e = Get(c, id);
                var minister = e.Office == "" ? null : Offices.Holder(c, e.Office, s.Date);
                var competence = (minister?.Competence ?? 5) / 10.0;
```

   with

```csharp
                var e = Get(c, id);
                var minister = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office);
                var competence = (minister?.Competence ?? 5) / 10.0;
```

4. Replace

```csharp
        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, FillDate(e, date));
    }
```

   with

```csharp
        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, FillDate(e, date));

        /// <summary>Event text as the player sees it now: names follow the player's appointments.</summary>
        public static string Render(Content c, GameState s, GameEvent e) => Cabinet.Fill(c, s, e.Text, FillDate(e, s.Date));
    }
```

In `sim/Statecraft.Sim/Content.cs`:

1. Replace

```csharp
        public double MobilizationCostPerYear { get; set; }
        public double NaturalIncreasePerYear { get; set; }
```

   with

```csharp
        public double MobilizationCostPerYear { get; set; }
        /// <summary>Coalition stability lost when an appointment takes a seat from the displaced minister's party.</summary>
        public double AppointStabilityCostOtherParty { get; set; }
        /// <summary>Coalition stability lost when an appointment replaces a minister with one of the same party.</summary>
        public double AppointStabilityCostSameParty { get; set; }
        public double NaturalIncreasePerYear { get; set; }
```

In `sim/Statecraft.Sim/ContentLoader.cs`:

1. Replace

```csharp
                Finite("rules.yaml", key, v);
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
```

   with

```csharp
                Finite("rules.yaml", key, v);
            Unit("rules.yaml", "appoint_stability_cost_other_party", r.AppointStabilityCostOtherParty);
            Unit("rules.yaml", "appoint_stability_cost_same_party", r.AppointStabilityCostSameParty);
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
```

Replace the whole of `content/rules.yaml` with:

```yaml
# Game rules. Values are game balance, except where a comment cites a source.
start: 1948-05-15                 # first day covered by the CBS 1948 immigration series (aliyah.yaml)
start_rung: 5                     # the war is under way when the slice opens
attention_per_turn: 4
attention_cost: { set_policy: 1, allocate_budget: 2, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 2 }
# Coalition stability lost by replacing a minister (Cabinet.Appoint): a seat taken from the minister's party, or a
# reshuffle within it.
appoint_stability_cost_other_party: 0.06
appoint_stability_cost_same_party: 0.02
rung_attention_step: 0.25
rung_min_mobilization: [0.02, 0.05, 0.15, 0.4, 0.8]
ceasefire_weeks: 52               # long enough that the historical armistice events end the war first
# ---- Economy (calibrated in M3; checked by sim/Statecraft.Sim.Tests/EconomyCalibrationTests.cs) ----
# Units: treasury and debt in IL millions; fx_start, exports, donations/loans and imports in US$ millions.
# Formal rate 1950-51: IL 0.36 per US$ (IL 1 = $2.80), Michaely, Foreign Trade Regimes and Economic Development: Israel
# (NBER 1975) Table 5-1, https://www.nber.org/system/files/chapters/c4560/c4560.pdf (checked 2026-10-10).
treasury_start: 20
fx_start: 40
# Taxes finance the ordinary budget: FY 1950/51 IL 60 million, approved 29 June 1950
# (https://en.wikipedia.org/wiki/1950_in_Israel, checked 2026-10-10). 60m / ~1.10m Jews (CBS average 1950) = 0.0000545.
tax_per_capita_per_year: 0.0000545
# Spending above taxes is borrowed. The gap is set so 1950-51 borrowing matches the rise in internal debt, IL 65.4m (1950)
# + 33.0m (1951) = 98.4m (Michaely Table 5-2, same chapter). The 1951 fiscal tightening is not modelled, so the model
# matches the two-year total, not each year: (98.4m - 6m reserve call-up) / ~2.40m person-years = 0.0000385 per person.
spending_per_capita_per_year: 0.000093
# Exports cover under a third of imports (spec section 3; EconomyCalibrationTests checks it every turn of 1950-51).
exports_per_year: 40
imports_per_capita_per_year: 0.00025
# Donations, loans and transfers, set so reserves run out in the last quarter of 1951: the October 1951 crisis
# (FRUS 1951 v.V doc 513, https://history.state.gov/historicaldocuments/frus1951v05/d513, checked 2026-10-10).
capital_inflow_per_year: 192
# While reserves are exhausted, food imports are cut: food_supply falls by this much per year.
fx_crisis_food_per_year: -0.2
mobilization_cost_per_year: 60
# Derived from CBS table 2.1 (1949): (end-1949 1,013.9k - end-1948 758.7k - 1949 immigrants 239,954) / average 1949 population 901.0k.
natural_increase_per_year: 0.0169
# Derived: CBS end-1948 Jewish population 758,700 minus 101,828 immigrants (15 May-31 Dec 1948) minus natural increase over
# 7.5 months at the rate above. Checked against the 8 Nov 1948 census by the calibration test.
# Arithmetic: 758,700 - 101,828 = 656,872. Natural-increase base = average of 656,872 and 758,700 = 707,786.
# Natural increase = 707,786 x 0.0169 x 7.5/12 = 7,476. 656,872 - 7,476 = 649,396, rounded to the nearest hundred = 649,400.
initial_population: { veterans: 649400 }
budget_start:
  defense_minister: 0.45
  finance_minister: 0.05
  # Absorption was handled by the Jewish Agency and the Prime Minister's office (notes, Addendum (a)); the Ministry of
  # Immigration ends on 1951-10-08, so its budget line sits with the Prime Minister (the loader refuses a line for an
  # abolished office).
  prime_minister: 0.15
  labour_minister: 0.10
  health_minister: 0.07
  education_minister: 0.06
  agriculture_minister: 0.07
  interior_minister: 0.05
```

- [ ] **Step 4: Rebaseline both golden hashes (hash format change)**

The canonical hash text gains `appt{...}under=...;`, so both runs move although no appointment is made in them. Replace in `GoldenRun.cs` and `DeterminismTests.cs`: `9cd2f4b5dc01cf4f6159b958eac9e815e7635f4589d2822793c20c527a3333fb` → `9a546a6f62acdec3d3acc1621d90e9863e8689f9a199a792a458bc8f1b6a3f35` and `970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a` → `8db2b892cae1c4462fef93cb6a25096b953db0dd259f18551de496774722cf08`. Then:

In `docs/determinism.md`:

1. Replace

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `9cd2f4b5dc01cf4f6159b958eac9e815e7635f4589d2822793c20c527a3333fb` |
| to 1952-01-01 (`Hash1952`) | 1952-02-26, turn 53 | `970ef315880c5044433c1e5b8334c92741f91b337c6d8746b50a8ef61778a43a` |

```

   with

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `9a546a6f62acdec3d3acc1621d90e9863e8689f9a199a792a458bc8f1b6a3f35` |
| to 1952-01-01 (`Hash1952`) | 1952-02-26, turn 53 | `8db2b892cae1c4462fef93cb6a25096b953db0dd259f18551de496774722cf08` |

```

2. Replace

```markdown
| M3 Task 5 | 1952 | `970ef315` | new run covering the M3 content range |
```

   with

```markdown
| M3 Task 5 | 1952 | `970ef315` | new run covering the M3 content range |
| M3 Task 6 | 1950 | `9a546a6f` | hash format adds the player's appointments (empty in the run) |
| M3 Task 6 | 1952 | `8db2b892` | same |
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   352`.

- [ ] **Step 6: Commit**

```bash
git add content/rules.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/CabinetTests.cs \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim.Tests/Fixture.cs \
  sim/Statecraft.Sim/AttentionSystem.cs \
  sim/Statecraft.Sim/Cabinet.cs \
  sim/Statecraft.Sim/Commands.cs \
  sim/Statecraft.Sim/Content.cs \
  sim/Statecraft.Sim/ContentLoader.cs \
  sim/Statecraft.Sim/EffectsSystem.cs \
  sim/Statecraft.Sim/EventSystem.cs \
  sim/Statecraft.Sim/GameState.cs \
  sim/Statecraft.Sim/GoldenRun.cs \
  sim/Statecraft.Sim/Simulation.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(sim): Appoint command with coalition costs; appointees drive effects, delegation and event text

Both golden hashes move: the hash format gains the appointment fields.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 7: Settlement hexes and the simulation's map

*Track A.* Ruling 4: map-action hex ownership "must use the tools' settlement-hex rule". The rule (`hexgrid.settlement_hex`: the containing hex if land, else the nearest land neighbour, as for Tiberias) needs cos() for the projection, and the sim avoids trigonometry for determinism (tech notes §4.2). So a tool writes the result (`content/map/settlement_hexes.yaml`) and the sim reads it, together with `content/map/hexes.json` (JSON is YAML 1.2, so the strict YamlDotNet loader reads it with every key declared).

**Files:**
- Create: `content/map/settlement_hexes.yaml`, `sim/Statecraft.Sim.Tests/MapContentTests.cs`, `tools/settlement_hexes.py`, `tools/tests/test_settlement_hexes.py`
- Modify: `docs/determinism.md`, `sim/Statecraft.Sim.Tests/Fixture.cs`, `sim/Statecraft.Sim/Content.cs`, `sim/Statecraft.Sim/ContentLoader.cs`, `tools/README.md`
- Generated by a command in this task (commit the output): `content/map/settlement_hexes.yaml`

**Interfaces:**
- Consumes: `tools/hexgrid.py` (`settlement_hex`, `NEIGHBOURS`), `content/map/hexes.json`, `content/map/settlements_1948.yaml`; `ContentLoader.Read<T>`, `ContentLoader.Unique`.
- Produces:
  - `tools/settlement_hexes.py` with `render(hexes: dict, settlements: list[dict]) -> str`; output `content/map/settlement_hexes.yaml` (`settlements: [{id, q, r}]`).
  - `public sealed class MapHex { int Q; int R; double Lon; double Lat; double ElevM; bool Land; string Biome; }`, `public sealed class SettlementHex { string Id; int Q; int R; }`.
  - `Content.Hexes : List<MapHex>`, `Content.SettlementHexes : List<SettlementHex>`, `MapHex? Content.HexAt(int q, int r)`.
  - `ContentLoader.HexesFile = "map/hexes.json"`, `ContentLoader.SettlementHexesFileName = "map/settlement_hexes.yaml"`; both are read through the same `readFile` callback (so a WebGL build fetches them like the rest).
  - The test `Fixture` gains a 5×2 map (`map/hexes.json`, `map/settlement_hexes.yaml`, one settlement `town_a` at (1, 0), sea at (0, 0)).

- [ ] **Step 1: Write the failing tests**

Create `tools/tests/test_settlement_hexes.py`:

```python
"""content/map/settlement_hexes.yaml is the committed output of tools/settlement_hexes.py and must stay in sync."""
import json
from pathlib import Path

import yaml

import hexgrid as hg
import settlement_hexes as sh

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
TOWNS = yaml.safe_load((MAP / "settlements_1948.yaml").read_text())["settlements"]
BY_QR = {(h["q"], h["r"]): h for h in HEXES["hexes"]}


def test_committed_file_is_up_to_date():
    assert (MAP / "settlement_hexes.yaml").read_text() == sh.render(HEXES, TOWNS), "rerun: uv run tools/settlement_hexes.py"


def test_every_settlement_is_on_its_rule_hex_and_that_hex_is_land():
    rows = yaml.safe_load((MAP / "settlement_hexes.yaml").read_text())["settlements"]
    assert [r["id"] for r in rows] == [t["id"] for t in TOWNS]
    for row, town in zip(rows, TOWNS):
        assert (row["q"], row["r"]) == hg.settlement_hex(town["lon"], town["lat"], HEXES), row["id"]
        assert BY_QR[(row["q"], row["r"])]["land"], row["id"]


def test_tiberias_is_moved_off_the_lake():
    rows = {r["id"]: r for r in yaml.safe_load((MAP / "settlement_hexes.yaml").read_text())["settlements"]}
    assert BY_QR[(rows["tiberias"]["q"], rows["tiberias"]["r"])]["biome"] != "lake"
```

Create `sim/Statecraft.Sim.Tests/MapContentTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/map/hexes.json and settlement_hexes.yaml as the sim loads them.</summary>
public class MapContentTests
{
    [Fact]
    public void TheFixtureMapLoads()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(9, c.Hexes.Count);
        Assert.False(c.HexAt(0, 0)!.Land);
        Assert.Equal("hills", c.HexAt(2, 1)!.Biome);
        Assert.Null(c.HexAt(9, 9));
        Assert.Equal(("town_a", 1, 0), (c.SettlementHexes[0].Id, c.SettlementHexes[0].Q, c.SettlementHexes[0].R));
    }

    [Fact]
    public void ASettlementOnASeaHexIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"].Replace("q: 1, r: 0", "q: 0, r: 0");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Equal("map/settlement_hexes.yaml", ex.File);
        Assert.Equal("settlements[town_a]", ex.Field);
        Assert.Contains("hex q=0 r=0 is not a land hex", ex.Message);
    }

    [Fact]
    public void ASettlementOffTheMapIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"].Replace("q: 1, r: 0", "q: 40, r: 0");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Contains("hex q=40 r=0 is not a land hex", ex.Message);
    }

    [Fact]
    public void ADuplicateHexIsALoadError()
    {
        var bad = Fixture.Files["map/hexes.json"].Replace(@"{""q"":4,""r"":0,", @"{""q"":3,""r"":0,");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = bad })));
        Assert.Equal("map/hexes.json", ex.File);
        Assert.Equal("hexes[q=3,r=0]", ex.Field);
    }

    [Fact]
    public void AnUnknownKeyInHexesJsonIsALoadError()
    {
        var bad = Fixture.Files["map/hexes.json"].Replace(@"""layout""", @"""layuot""");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = bad })));
        Assert.Equal("map/hexes.json", ex.File);
    }

    [Fact]
    public void TheRealMapHas1102HexesAndEvery1948Settlement()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(1102, c.Hexes.Count);
        Assert.Equal(870, c.Hexes.Count(h => h.Land));
        Assert.Equal(25, c.SettlementHexes.Count);
        Assert.Contains(c.SettlementHexes, s => s.Id == "tiberias");
    }
}
```

In `sim/Statecraft.Sim.Tests/Fixture.cs`:

1. Replace

```csharp
",
        ["aliyah.yaml"] = @"
```

   with

```csharp
",
        // A 5 x 2 map: one sea hex, the rest land; one settlement at (1, 0).
        ["map/hexes.json"] = @"{""attribution"":[""test""],""bounds"":{""lon0"":34.2,""lon1"":35.9,""lat0"":29.45,""lat1"":33.35},""hex_size_km"":5.0,""layout"":""pointy"",""hexes"":[
{""q"":0,""r"":0,""lon"":34.2,""lat"":33.35,""elev_m"":0.0,""land"":false,""biome"":""sea""},
{""q"":1,""r"":0,""lon"":34.29,""lat"":33.35,""elev_m"":20.0,""land"":true,""biome"":""coastal_plain""},
{""q"":2,""r"":0,""lon"":34.38,""lat"":33.35,""elev_m"":250.0,""land"":true,""biome"":""hills""},
{""q"":3,""r"":0,""lon"":34.47,""lat"":33.35,""elev_m"":300.0,""land"":true,""biome"":""hills""},
{""q"":4,""r"":0,""lon"":34.56,""lat"":33.35,""elev_m"":100.0,""land"":true,""biome"":""desert""},
{""q"":0,""r"":1,""lon"":34.24,""lat"":33.28,""elev_m"":10.0,""land"":true,""biome"":""coastal_plain""},
{""q"":1,""r"":1,""lon"":34.33,""lat"":33.28,""elev_m"":40.0,""land"":true,""biome"":""coastal_plain""},
{""q"":2,""r"":1,""lon"":34.42,""lat"":33.28,""elev_m"":260.0,""land"":true,""biome"":""hills""},
{""q"":3,""r"":1,""lon"":34.51,""lat"":33.28,""elev_m"":120.0,""land"":true,""biome"":""desert""}]}
",
        ["map/settlement_hexes.yaml"] = @"
settlements:
  - { id: town_a, q: 1, r: 0 }
",
        ["aliyah.yaml"] = @"
```

2. Replace

```csharp
        Directory.CreateDirectory(Path.Combine(dir, "events"));
        foreach (var kv in Files)
```

   with

```csharp
        Directory.CreateDirectory(Path.Combine(dir, "events"));
        Directory.CreateDirectory(Path.Combine(dir, "map"));
        foreach (var kv in Files)
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest -q tools/tests/test_settlement_hexes.py
```
Expected: collection error: `ModuleNotFoundError: No module named 'settlement_hexes'`.

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: the build fails: `error CS1061: 'Content' does not contain a definition for 'Hexes'`.

- [ ] **Step 3: Write the tool and generate the table**

Create `tools/settlement_hexes.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pyyaml==6.0.3"]
# ///
"""Settlement -> hex ownership for the simulation.

Usage: uv run tools/settlement_hexes.py   (reads content/map/hexes.json and content/map/settlements_1948.yaml,
writes content/map/settlement_hexes.yaml)
The rule is hexgrid.settlement_hex: the hex containing the settlement if it is land, else the land neighbour with the
nearest centre (Tiberias). The sim reads the result instead of re-implementing the projection, so it needs no
trigonometry (docs/determinism.md: no libm calls in the sim)."""
import json
import sys
from pathlib import Path

import yaml

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hexgrid as hg  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "content" / "map"
OUT = MAP / "settlement_hexes.yaml"
HEADER = ("# Generated by tools/settlement_hexes.py from settlements_1948.yaml and hexes.json (hexgrid.settlement_hex).\n"
          "# Do not edit by hand: rerun the tool after changing either input.\n")


def render(hexes: dict, settlements: list[dict]) -> str:
    """The file text: one flow mapping per settlement, in settlements_1948.yaml order."""
    lines = [HEADER, "settlements:\n"]
    for s in settlements:
        q, r = hg.settlement_hex(s["lon"], s["lat"], hexes)
        lines.append(f"  - {{ id: {s['id']}, q: {q}, r: {r} }}\n")
    return "".join(lines)


def main() -> None:
    hexes = json.loads((MAP / "hexes.json").read_text())
    settlements = yaml.safe_load((MAP / "settlements_1948.yaml").read_text())["settlements"]
    OUT.write_text(render(hexes, settlements))
    print(f"wrote {OUT.relative_to(ROOT)} ({len(settlements)} settlements)")


if __name__ == "__main__":
    main()
```

Run:
```bash
uv run tools/settlement_hexes.py && head -5 content/map/settlement_hexes.yaml
```
Expected: `wrote content/map/settlement_hexes.yaml (25 settlements)`, then the two header comment lines, `settlements:`, `  - { id: jerusalem, q: 0, r: 23 }`, `  - { id: tel_aviv, q: -3, r: 19 }`.

- [ ] **Step 4: Load the map in the sim**

In `sim/Statecraft.Sim/Content.cs`:

1. Replace

```csharp

    public sealed class Content
```

   with

```csharp

    /// <summary>One hex of content/map/hexes.json (written by tools/hexgrid.py; axial q, r, pointy-top).</summary>
    public sealed class MapHex
    {
        public int Q { get; set; }
        public int R { get; set; }
        public double Lon { get; set; }
        public double Lat { get; set; }
        public double ElevM { get; set; }
        public bool Land { get; set; }
        public string Biome { get; set; } = "";
    }

    /// <summary>The hex that owns a settlement (content/map/settlement_hexes.yaml, written by tools/settlement_hexes.py
    /// with the hexgrid.settlement_hex rule).</summary>
    public sealed class SettlementHex
    {
        public string Id { get; set; } = "";
        public int Q { get; set; }
        public int R { get; set; }
    }

    public sealed class Content
```

2. Replace

```csharp
        public List<GameEvent> Events = new List<GameEvent>();
    }
```

   with

```csharp
        public List<GameEvent> Events = new List<GameEvent>();
        public List<MapHex> Hexes = new List<MapHex>();
        public List<SettlementHex> SettlementHexes = new List<SettlementHex>();
        Dictionary<(int, int), MapHex>? _hexIndex;

        /// <summary>The hex at axial (q, r), or null when the map has none there.</summary>
        public MapHex? HexAt(int q, int r)
        {
            if (_hexIndex == null)
            {
                var index = new Dictionary<(int, int), MapHex>();
                foreach (var h in Hexes) index[(h.Q, h.R)] = h;
                _hexIndex = index;
            }
            return _hexIndex.TryGetValue((q, r), out var hex) ? hex : null;
        }
    }
```

In `sim/Statecraft.Sim/ContentLoader.cs`:

1. Replace

```csharp
        sealed class PopulationFile { public List<PopulationAnchor> Anchors { get; set; } = new List<PopulationAnchor>(); }

```

   with

```csharp
        sealed class PopulationFile { public List<PopulationAnchor> Anchors { get; set; } = new List<PopulationAnchor>(); }
        sealed class HexBounds { public double Lon0 { get; set; } public double Lon1 { get; set; } public double Lat0 { get; set; } public double Lat1 { get; set; } }
        /// <summary>hexes.json is JSON, which YAML 1.2 reads as a flow mapping; every key the tool writes is declared.</summary>
        sealed class MapFile
        {
            public List<string> Attribution { get; set; } = new List<string>();
            public HexBounds Bounds { get; set; } = new HexBounds();
            public double HexSizeKm { get; set; }
            public string Layout { get; set; } = "";
            public List<MapHex> Hexes { get; set; } = new List<MapHex>();
        }
        sealed class SettlementHexesFile { public List<SettlementHex> Settlements { get; set; } = new List<SettlementHex>(); }

        public const string HexesFile = "map/hexes.json";
        public const string SettlementHexesFileName = "map/settlement_hexes.yaml";

```

2. Replace

```csharp
            c.PopulationAnchors = Read<PopulationFile>(readFile, "population.yaml").Anchors;
            foreach (var rel in eventFiles.OrderBy(p => p, StringComparer.Ordinal))
```

   with

```csharp
            c.PopulationAnchors = Read<PopulationFile>(readFile, "population.yaml").Anchors;
            c.Hexes = Read<MapFile>(readFile, HexesFile).Hexes;
            c.SettlementHexes = Read<SettlementHexesFile>(readFile, SettlementHexesFileName).Settlements;
            foreach (var rel in eventFiles.OrderBy(p => p, StringComparer.Ordinal))
```

3. Replace

```csharp
            }
            EventRules.Validate(c, offices, indicators);
        }
```

   with

```csharp
            }
            ValidateMap(c);
            EventRules.Validate(c, offices, indicators);
        }

        static void ValidateMap(Content c)
        {
            if (c.Hexes.Count == 0) throw new ContentException(HexesFile, "hexes", "no hexes");
            var seen = new HashSet<(int, int)>();
            foreach (var h in c.Hexes)
            {
                var at = FormattableString.Invariant($"hexes[q={h.Q},r={h.R}]");
                if (!seen.Add((h.Q, h.R))) throw new ContentException(HexesFile, at, "duplicate hex");
                if (h.Biome == "") throw new ContentException(HexesFile, at + ".biome", "required");
            }
            Unique(SettlementHexesFileName, "settlements", c.SettlementHexes.Select(s => s.Id));
            foreach (var s in c.SettlementHexes)
            {
                var hex = c.HexAt(s.Q, s.R);
                if (hex == null || !hex.Land)
                    throw new ContentException(SettlementHexesFileName, $"settlements[{s.Id}]", FormattableString.Invariant($"hex q={s.Q} r={s.R} is not a land hex in {HexesFile}"));
            }
        }
```

In `tools/README.md`:

1. Replace

```markdown
- `hexgrid.py` writes `content/map/hexes.json` from the heightmap and those committed precipitation inputs (no network).
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
```

   with

```markdown
- `hexgrid.py` writes `content/map/hexes.json` from the heightmap and those committed precipitation inputs (no network).
- `settlement_hexes.py` writes `content/map/settlement_hexes.yaml` (committed): the hex that owns each settlement, by the
  `hexgrid.settlement_hex` rule. The sim reads it, so it needs no projection maths.
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
```

2. Replace

```markdown

Expected: `93 passed`. Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

```

   with

```markdown

Expected: `96 passed`. Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

```

In `docs/determinism.md`:

1. Replace

```markdown
  (https://prng.di.unimi.it/splitmix64.c), compiled and run separately. They were not copied from this implementation.
- **Canonical hash:** `GameState.Hash()` is SHA-256 over a canonical text. Keys are sorted ordinally. Doubles are
```

   with

```markdown
  (https://prng.di.unimi.it/splitmix64.c), compiled and run separately. They were not copied from this implementation.
- **No trigonometry in the sim:** the sim calls only `Math.Abs`, `Math.Min` and `Math.Max`. Geometry that needs
  cos/sin (which hex owns a settlement) is precomputed by `tools/settlement_hexes.py` and read as content.
- **Canonical hash:** `GameState.Hash()` is SHA-256 over a canonical text. Keys are sorted ordinally. Doubles are
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
```
Expected: `96 passed` (93 before).

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   358`. Golden hashes unchanged (loading the map adds no state).

- [ ] **Step 6: Commit**

```bash
git add content/map/settlement_hexes.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/Fixture.cs \
  sim/Statecraft.Sim.Tests/MapContentTests.cs \
  sim/Statecraft.Sim/Content.cs \
  sim/Statecraft.Sim/ContentLoader.cs \
  tools/README.md \
  tools/settlement_hexes.py \
  tools/tests/test_settlement_hexes.py
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(tools,sim): settlement hexes by the hexgrid rule; the sim loads the hex map

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 8: MapAction: transit camps and housing

*Track A.* Ruling 4: transit camps (ma'abarot) and permanent housing on a land hex near settlements, from 1950, costing budget and Attention, moving immigrant absorption (the `housing` indicator) and public support (`morale`, which voter groups weigh). Transit camps also need the `maabarot` policy that Task 4's historical decision sets (the first ma'abara opened in May 1950). A shortfall in the treasury is borrowed, as everywhere else in the economy. Housing may replace a transit camp on the same hex; any other second build on a hex is refused. At most two map actions are built per turn (`map_actions_per_turn`, amendment A6); upkeep and camp capacity are M4.

**Files:**
- Create: `sim/Statecraft.Sim.Tests/MapActionTests.cs`, `sim/Statecraft.Sim/MapActions.cs`
- Modify: `content/rules.yaml`, `docs/determinism.md`, `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, `sim/Statecraft.Sim.Tests/Fixture.cs`, `sim/Statecraft.Sim/AttentionSystem.cs`, `sim/Statecraft.Sim/Commands.cs`, `sim/Statecraft.Sim/Content.cs`, `sim/Statecraft.Sim/ContentLoader.cs`, `sim/Statecraft.Sim/GameState.cs`, `sim/Statecraft.Sim/GoldenRun.cs`, `sim/Statecraft.Sim/Simulation.cs`

**Interfaces:**
- Consumes: Task 7's `Content.HexAt`, `Content.SettlementHexes`; Task 5's `EffectsSystem.Shift`; Task 4's `maabarot` policy.
- Produces:
  - `public sealed class MapAction : Command { MapAction(string action, int q, int r); string Action; int Q; int R; Kind == "map_action" }`.
  - `public sealed class Placement { string Action; int Q; int R; DateTime Date; }`; `GameState.Placements : List<Placement>` (in `Hash()`).
  - `public static class MapActions { const string Kind = "map_action", TransitCamp = "transit_camp", Housing = "housing", MaabarotPolicy = "maabarot"; IReadOnlyList<string> Actions; int Distance(int q1, int r1, int q2, int r2); Placement? At(GameState, int q, int r); string? Check(Content, GameState, string action, int q, int r); string? Apply(Content, GameState, string action, int q, int r, List<Cause>); }`.
  - `public sealed class MapActionRule { double Cost; double Housing; double Morale; }`; rules `map_actions_from`, `map_action_max_distance`, `map_actions_per_turn` (refusal `No more than <n> map actions in one turn`), `map_actions: {transit_camp, housing}`, `attention_cost.map_action`.
  - Hashes: `Hash1950 = 404b181105e6a2085ec26d10cff4e9d0ac6f8802a1e20b3a91e8fe554e1d5c88`, `Hash1952 = 36640b59df6535c7f9e9aec94bc41ed62e65505544a91cbbe131bb7b5ca86bea`.

- [ ] **Step 1: Write the failing tests**

Create `sim/Statecraft.Sim.Tests/MapActionTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Transit camps and housing projects on the fixture map (5 x 2 hexes, town_a on (1, 0), sea on (0, 0)).</summary>
public class MapActionTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    /// <summary>A state on 1948-06-01 (map actions open) with the ma'abarot policy on unless told otherwise.</summary>
    static GameState Ready(bool maabarot = true)
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);
        s.Policies["maabarot"] = maabarot ? 1.0 : 0.0;
        return s;
    }

    [Theory]
    [InlineData(1, 0, 0)] [InlineData(2, 0, 1)] [InlineData(0, 1, 1)] [InlineData(3, 1, 3)] [InlineData(4, 0, 3)]
    public void AxialDistanceFromTownA(int q, int r, int expected) => Assert.Equal(expected, MapActions.Distance(1, 0, q, r));

    [Fact]
    public void ATransitCampCostsMoneyAndMovesHousingAndMorale()
    {
        var s = Ready();
        var housing = s.Indicators["housing"]; var morale = s.Indicators["morale"]; var treasury = s.Treasury;
        var why = new List<Cause>();
        Assert.Null(MapActions.Apply(C, s, "transit_camp", 2, 0, why));
        Assert.Equal(treasury - 2, s.Treasury, 9);
        Assert.Equal(housing + 0.05, s.Indicators["housing"], 9);
        Assert.Equal(morale - 0.02, s.Indicators["morale"], 9);
        Assert.Equal("transit_camp", MapActions.At(s, 2, 0)!.Action);
        Assert.Contains(why, w => w.System == "map" && w.Message == "a transit camp at (2, 0): cost 2.0, housing +0.050, morale -0.020");
    }

    [Fact]
    public void AShortfallIsBorrowed()
    {
        var s = Ready();
        s.Treasury = 3;
        var why = new List<Cause>();
        Assert.Null(MapActions.Apply(C, s, "housing", 1, 1, why));
        Assert.Equal(0, s.Treasury);
        Assert.Equal(5, s.Debt, 9);
        Assert.Contains(why, w => w.Message == "borrowed 5.00 to build a housing project");
    }

    [Fact]
    public void HousingReplacesATransitCamp()
    {
        var s = Ready();
        MapActions.Apply(C, s, "transit_camp", 2, 0, new List<Cause>());
        Assert.Null(MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>()));
        Assert.Equal("housing", MapActions.At(s, 2, 0)!.Action);
        Assert.Single(s.Placements);
    }

    [Theory]
    [InlineData("airport", 2, 0, "Unknown map action 'airport'")]
    [InlineData("transit_camp", 0, 0, "There is no land hex at (0, 0)")]
    [InlineData("transit_camp", 7, 7, "There is no land hex at (7, 7)")]
    [InlineData("transit_camp", 4, 0, "(4, 0) is more than 1 hex from a settlement")]
    public void IllegalPlacementsAreRejectedWithAReason(string action, int q, int r, string reason)
    {
        var s = Ready();
        var before = s.Hash();
        Assert.Equal(reason, MapActions.Apply(C, s, action, q, r, new List<Cause>()));
        Assert.Equal(before, s.Hash());
    }

    [Fact]
    public void NothingBeforeTheStartDate()
    {
        var s = Ready();
        s.Date = new DateTime(1948, 5, 31);
        Assert.Equal("Transit camps and housing projects can be placed from 1948-06-01", MapActions.Check(C, s, "housing", 2, 0));
    }

    [Fact]
    public void TransitCampsNeedTheMaabarotPolicy()
    {
        var s = Ready(maabarot: false);
        Assert.Equal("Transit camps need the ma'abarot policy", MapActions.Check(C, s, "transit_camp", 2, 0));
        Assert.Null(MapActions.Check(C, s, "housing", 2, 0));
    }

    [Fact]
    public void AtMostTwoMapActionsInOneTurn()
    {
        var s = Ready();
        Assert.Null(MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>()));
        Assert.Null(MapActions.Apply(C, s, "housing", 1, 1, new List<Cause>()));
        Assert.Equal("No more than 2 map actions in one turn", MapActions.Check(C, s, "housing", 0, 1));
        s.Date = s.Date.AddMonths(3);                                      // the next turn
        Assert.Null(MapActions.Check(C, s, "housing", 0, 1));
    }

    [Fact]
    public void AnOccupiedHexIsRejected()
    {
        var s = Ready();
        MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>());
        Assert.Equal("(2, 0) already has a housing project", MapActions.Check(C, s, "transit_camp", 2, 0));
        Assert.Equal("(2, 0) already has a housing project", MapActions.Check(C, s, "housing", 2, 0));
    }

    [Fact]
    public void TheCommandGoesThroughStepAndCostsAttention()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1948, 6, 1);
        // At war (rung 5) each command costs 2 of the 3 attention: the refused first order is refunded, so the second fits.
        var r = Simulation.Step(C, s0, new Command[] { new MapAction("housing", 4, 0), new MapAction("housing", 2, 0) });
        Assert.Equal("(4, 0) is more than 1 hex from a settlement", r.Rejected.Single().Reason);
        Assert.Equal("housing", MapActions.At(r.State, 2, 0)!.Action);
        Assert.Empty(s0.Placements);                                       // Step never mutates its input
        Assert.NotEqual(Simulation.Step(C, s0, new Command[0]).State.Hash(), r.State.Hash());
    }

    [Fact]
    public void AnUnknownActionInRulesIsALoadError()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("  housing: { cost: 8", "  airport: { cost: 8");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("map_actions.airport", ex.Field);
    }

    [Fact]
    public void RealContentOpensCampsWithTheMaabarotDecision()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = GoldenRun.Play(c, new DateTime(1950, 9, 1));
        var tlv = c.SettlementHexes.Single(x => x.Id == "tel_aviv");
        Assert.Null(MapActions.Check(c, s, "transit_camp", tlv.Q, tlv.R));
        var s1949 = GoldenRun.Play(c, new DateTime(1949, 6, 1));
        Assert.Equal("Transit camps and housing projects can be placed from 1950-01-01", MapActions.Check(c, s1949, "housing", tlv.Q, tlv.R));
    }
}
```

In `sim/Statecraft.Sim.Tests/Fixture.cs`:

1. Replace

```csharp
    base: 0.4
policies:
  - id: rationing
    default: 0.0
```

   with

```csharp
    base: 0.4
  - id: housing
    base: 0.3
policies:
  - id: rationing
    default: 0.0
    office: finance_minister
  - id: maabarot
    default: 0.0
```

2. Replace

```csharp
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 1 }
appoint_stability_cost_other_party: 0.1
appoint_stability_cost_same_party: 0.03
rung_attention_step: 0.25
```

   with

```csharp
attention_per_turn: 3
attention_cost: { set_policy: 1, allocate_budget: 1, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 1, map_action: 1 }
appoint_stability_cost_other_party: 0.1
appoint_stability_cost_same_party: 0.03
map_actions_from: 1948-06-01
map_action_max_distance: 1
map_actions_per_turn: 2
map_actions:
  transit_camp: { cost: 2, housing: 0.05, morale: -0.02 }
  housing: { cost: 8, housing: 0.08, morale: 0.01 }
rung_attention_step: 0.25
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: the build fails: `error CS0103: The name 'MapActions' does not exist in the current context`.

- [ ] **Step 3: Implement**

Create `sim/Statecraft.Sim/MapActions.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Something the player built on a hex. Immutable, so GameState.Clone can copy the list shallowly.</summary>
    public sealed class Placement
    {
        public string Action { get; }
        public int Q { get; }
        public int R { get; }
        public DateTime Date { get; }
        public Placement(string action, int q, int r, DateTime date) { Action = action; Q = q; R = r; Date = date; }
    }

    /// <summary>Map actions (M3 scope: absorption from 1950). A transit camp (ma'abara) or a housing project goes on a land
    /// hex within rules map_action_max_distance of a hex that owns a settlement (content/map/settlement_hexes.yaml). It
    /// costs treasury (a shortfall is borrowed, as in EconomySystem) and moves housing (absorption) and morale (support).
    /// At most rules map_actions_per_turn are built in one turn (upkeep and camp capacity are M4).</summary>
    public static class MapActions
    {
        public const string Kind = "map_action";
        public const string TransitCamp = "transit_camp";
        public const string Housing = "housing";
        public static readonly IReadOnlyList<string> Actions = new[] { TransitCamp, Housing };
        public const string MaabarotPolicy = "maabarot";

        /// <summary>Axial hex distance (Red Blob Games).</summary>
        public static int Distance(int q1, int r1, int q2, int r2) =>
            (Math.Abs(q1 - q2) + Math.Abs(q1 + r1 - q2 - r2) + Math.Abs(r1 - r2)) / 2;

        /// <summary>The placement on (q, r), or null.</summary>
        public static Placement? At(GameState s, int q, int r) => s.Placements.FirstOrDefault(p => p.Q == q && p.R == r);

        static string Name(string action) => action == TransitCamp ? "a transit camp" : "a housing project";

        /// <summary>Null when <paramref name="action"/> may be placed on (q, r) now, else the reason shown to the player.</summary>
        public static string? Check(Content c, GameState s, string action, int q, int r)
        {
            if (!Actions.Contains(action)) return $"Unknown map action '{action}'";
            var rule = c.Rules.MapActions.TryGetValue(action, out var x) ? x : null;
            if (rule == null) return $"Map action '{action}' is not available in this content";
            // Placements carry the date they were built, and the date only advances at the end of a turn.
            var built = s.Placements.Count(p => p.Date == s.Date);
            if (built >= c.Rules.MapActionsPerTurn)
                return FormattableString.Invariant($"No more than {c.Rules.MapActionsPerTurn} map actions in one turn");
            if (s.Date < c.Rules.MapActionsFrom)
                return FormattableString.Invariant($"Transit camps and housing projects can be placed from {c.Rules.MapActionsFrom:yyyy-MM-dd}");
            if (action == TransitCamp && !(s.Policies.TryGetValue(MaabarotPolicy, out var m) && m >= 0.5))
                return "Transit camps need the ma'abarot policy";
            var hex = c.HexAt(q, r);
            if (hex == null || !hex.Land) return FormattableString.Invariant($"There is no land hex at ({q}, {r})");
            var max = c.Rules.MapActionMaxDistance;
            if (!c.SettlementHexes.Any(sh => Distance(q, r, sh.Q, sh.R) <= max))
                return FormattableString.Invariant($"({q}, {r}) is more than {max} {(max == 1 ? "hex" : "hexes")} from a settlement");
            var here = At(s, q, r);
            if (here != null && !(action == Housing && here.Action == TransitCamp))
                return FormattableString.Invariant($"({q}, {r}) already has {Name(here.Action)}");
            return null;
        }

        /// <summary>Returns null and builds, or the reason (state untouched). Housing on a transit camp replaces the camp.</summary>
        public static string? Apply(Content c, GameState s, string action, int q, int r, List<Cause> why)
        {
            var reason = Check(c, s, action, q, r);
            if (reason != null) return reason;
            var rule = c.Rules.MapActions[action];
            s.Placements.RemoveAll(p => p.Q == q && p.R == r);
            s.Placements.Add(new Placement(action, q, r, s.Date));
            s.Treasury -= rule.Cost;
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                why.Add(new Cause("economy", "debt", -s.Treasury, FormattableString.Invariant($"borrowed {-s.Treasury:0.00} to build {Name(action)}")));
                s.Treasury = 0;
            }
            var housing = rule.Housing == 0 ? 0 : EffectsSystem.Shift(c, s, "housing", rule.Housing);
            var morale = rule.Morale == 0 ? 0 : EffectsSystem.Shift(c, s, "morale", rule.Morale);
            why.Add(new Cause("map", action, 1, FormattableString.Invariant(
                $"{Name(action)} at ({q}, {r}): cost {rule.Cost:0.0}, housing {housing:+0.000;-0.000;+0.000}, morale {morale:+0.000;-0.000;+0.000}")));
            return null;
        }
    }
}
```

In `sim/Statecraft.Sim/Commands.cs`:

1. Replace

```csharp

    public sealed class Rejection
```

   with

```csharp

    /// <summary>Builds <see cref="Action"/> (MapActions.TransitCamp or MapActions.Housing) on hex (Q, R).</summary>
    public sealed class MapAction : Command
    {
        public override string Kind => MapActions.Kind;
        public string Action { get; } public int Q { get; } public int R { get; }
        public MapAction(string action, int q, int r) { Action = action; Q = q; R = r; }
    }

    public sealed class Rejection
```

Replace the whole of `sim/Statecraft.Sim/AttentionSystem.cs` with:

```csharp
using System.Collections.Generic;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>The PM's per-turn capacity. Each command costs attention; costs rise with the security level.</summary>
    public static class AttentionSystem
    {
        static readonly HashSet<string> Kinds = new HashSet<string>
            { "set_policy", "allocate_budget", "choose_event_option", "set_mobilization", "security_order", "appoint", "map_action" };

        public static IReadOnlyCollection<string> CommandKinds => Kinds;

        public static bool IsCommandKind(string kind) => Kinds.Contains(kind);

        public static void Refill(Content c, GameState s) => s.Attention = c.Rules.AttentionPerTurn;

        /// <summary>Cost at <paramref name="rung"/> if given, else at the state's current rung.</summary>
        public static double Cost(Content c, GameState s, string kind, int? rung = null) =>
            c.Rules.AttentionCost[kind] * (1 + c.Rules.RungAttentionStep * ((rung ?? s.Rung) - 1));

        /// <summary>Returns null and deducts on success, or the reason it could not be afforded.
        /// <paramref name="rung"/> prices the command at that security level (the turn's starting rung).</summary>
        public static string? TrySpend(Content c, GameState s, string kind, int? rung = null)
        {
            var cost = Cost(c, s, kind, rung);
            if (cost > s.Attention + 1e-9)
                return string.Format(CultureInfo.InvariantCulture, "Not enough attention (need {0:0.0}, have {1:0.0})", cost, s.Attention);
            s.Attention -= cost;
            return null;
        }
    }
}
```

In `sim/Statecraft.Sim/GameState.cs`:

1. Replace

```csharp
        public string AppointedUnder = "";
        public string? GameOver;
```

   with

```csharp
        public string AppointedUnder = "";
        /// <summary>Map actions built so far, in order (MapActions).</summary>
        public List<Placement> Placements = new List<Placement>();
        public string? GameOver;
```

2. Replace

```csharp
            c.Appointments = new Dictionary<string, Appointment>(Appointments);
            return c;
```

   with

```csharp
            c.Appointments = new Dictionary<string, Appointment>(Appointments);
            c.Placements = new List<Placement>(Placements);
            return c;
```

3. Replace

```csharp
            sb.Append("}under=").Append(AppointedUnder).Append(';');
            sb.Append("over=").Append(GameOver ?? "");
```

   with

```csharp
            sb.Append("}under=").Append(AppointedUnder).Append(';');
            sb.Append("map[");
            foreach (var p in Placements)
                sb.Append(p.Action).Append('@').Append(p.Q).Append(',').Append(p.R).Append('@').Append(p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';');
            sb.Append(']');
            sb.Append("over=").Append(GameOver ?? "");
```

In `sim/Statecraft.Sim/Simulation.cs`:

1. Replace

```csharp
                    return Cabinet.Appoint(c, s, a.Office, a.Person, why);
                case SecurityOrder o:
```

   with

```csharp
                    return Cabinet.Appoint(c, s, a.Office, a.Person, why);
                case MapAction m:
                    return MapActions.Apply(c, s, m.Action, m.Q, m.R, why);
                case SecurityOrder o:
```

In `sim/Statecraft.Sim/Content.cs`:

1. Replace

```csharp
        public double AppointStabilityCostSameParty { get; set; }
        public double NaturalIncreasePerYear { get; set; }
        public Dictionary<string, double> InitialPopulation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> BudgetStart { get; set; } = new Dictionary<string, double>();
    }
```

   with

```csharp
        public double AppointStabilityCostSameParty { get; set; }
        /// <summary>First day map actions can be placed.</summary>
        public DateTime MapActionsFrom { get; set; }
        /// <summary>How far (in hexes) a map action may be from a hex that owns a settlement.</summary>
        public int MapActionMaxDistance { get; set; } = 1;
        /// <summary>How many map actions may be built in one turn.</summary>
        public int MapActionsPerTurn { get; set; } = 2;
        public Dictionary<string, MapActionRule> MapActions { get; set; } = new Dictionary<string, MapActionRule>();
        public double NaturalIncreasePerYear { get; set; }
        public Dictionary<string, double> InitialPopulation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> BudgetStart { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>Price and effect of one map action (rules.yaml map_actions).</summary>
    public sealed class MapActionRule
    {
        /// <summary>Treasury cost, in treasury units.</summary>
        public double Cost { get; set; }
        /// <summary>Change to the housing indicator (absorption).</summary>
        public double Housing { get; set; }
        /// <summary>Change to the morale indicator (public support).</summary>
        public double Morale { get; set; }
    }
```

In `sim/Statecraft.Sim/ContentLoader.cs`:

1. Replace

```csharp
            Unit("rules.yaml", "appoint_stability_cost_same_party", r.AppointStabilityCostSameParty);
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
```

   with

```csharp
            Unit("rules.yaml", "appoint_stability_cost_same_party", r.AppointStabilityCostSameParty);
            if (r.MapActionMaxDistance < 0) throw new ContentException("rules.yaml", "map_action_max_distance", "must be >= 0");
            if (r.MapActionsPerTurn < 1) throw new ContentException("rules.yaml", "map_actions_per_turn", "must be >= 1");
            foreach (var kv in r.MapActions)
            {
                if (!MapActions.Actions.Contains(kv.Key)) throw new ContentException("rules.yaml", $"map_actions.{kv.Key}", "unknown map action (transit_camp or housing)");
                if (!(kv.Value.Cost >= 0 && IsFinite(kv.Value.Cost))) throw new ContentException("rules.yaml", $"map_actions.{kv.Key}.cost", "must be a finite number >= 0");
                foreach (var (key, v) in new[] { ("housing", kv.Value.Housing), ("morale", kv.Value.Morale) })
                    if (!(v >= -1 && v <= 1)) throw new ContentException("rules.yaml", $"map_actions.{kv.Key}.{key}", "must be a number from -1 to 1");
            }
            if (r.MapActions.Count > 0 && r.MapActionsFrom == default) throw new ContentException("rules.yaml", "map_actions_from", "required when map_actions are defined");
            if (r.MapActions.Count > 0 && (!indicators.Contains("housing") || !indicators.Contains("morale")))
                throw new ContentException("rules.yaml", "map_actions", "needs housing and morale indicators in model.yaml");
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException("rules.yaml", "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
```

In `content/rules.yaml`:

1. Replace

```yaml
attention_per_turn: 4
attention_cost: { set_policy: 1, allocate_budget: 2, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 2 }
# Coalition stability lost by replacing a minister (Cabinet.Appoint): a seat taken from the minister's party, or a
# reshuffle within it.
appoint_stability_cost_other_party: 0.06
appoint_stability_cost_same_party: 0.02
rung_attention_step: 0.25
```

   with

```yaml
attention_per_turn: 4
attention_cost: { set_policy: 1, allocate_budget: 2, choose_event_option: 1, set_mobilization: 1, security_order: 1, appoint: 2, map_action: 1 }
# Coalition stability lost by replacing a minister (Cabinet.Appoint): a seat taken from the minister's party, or a
# reshuffle within it.
appoint_stability_cost_other_party: 0.06
appoint_stability_cost_same_party: 0.02
# Map actions (MapActions): from 1950, on land within one hex of a settlement. Transit camps also need the ma'abarot
# policy (the first ma'abara, Kesalon, opened in May 1950: https://en.wikipedia.org/wiki/Ma'abarot, checked 2026-10-10).
# Costs (IL millions) and effects are game balance: a camp is cheap and grim, permanent housing dear and welcome.
map_actions_from: 1950-01-01
map_action_max_distance: 1
map_actions_per_turn: 2
map_actions:
  transit_camp: { cost: 1.5, housing: 0.02, morale: -0.005 }
  housing: { cost: 6, housing: 0.03, morale: 0.01 }
rung_attention_step: 0.25
```

- [ ] **Step 4: Rebaseline both golden hashes (hash format change)**

The hash text gains `map[...]`. Replace in `GoldenRun.cs` and `DeterminismTests.cs`: `9a546a6f62acdec3d3acc1621d90e9863e8689f9a199a792a458bc8f1b6a3f35` → `404b181105e6a2085ec26d10cff4e9d0ac6f8802a1e20b3a91e8fe554e1d5c88` and `8db2b892cae1c4462fef93cb6a25096b953db0dd259f18551de496774722cf08` → `36640b59df6535c7f9e9aec94bc41ed62e65505544a91cbbe131bb7b5ca86bea`. Then:

In `docs/determinism.md`:

1. Replace

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `9a546a6f62acdec3d3acc1621d90e9863e8689f9a199a792a458bc8f1b6a3f35` |
| to 1952-01-01 (`Hash1952`) | 1952-02-26, turn 53 | `8db2b892cae1c4462fef93cb6a25096b953db0dd259f18551de496774722cf08` |

```

   with

```markdown
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-26, turn 45 | `404b181105e6a2085ec26d10cff4e9d0ac6f8802a1e20b3a91e8fe554e1d5c88` |
| to 1952-01-01 (`Hash1952`) | 1952-02-26, turn 53 | `36640b59df6535c7f9e9aec94bc41ed62e65505544a91cbbe131bb7b5ca86bea` |

```

2. Replace

```markdown
| M3 Task 6 | 1952 | `8db2b892` | same |
```

   with

```markdown
| M3 Task 6 | 1952 | `8db2b892` | same |
| M3 Task 8 | 1950 | `404b1811` | hash format adds map placements (empty in the run) |
| M3 Task 8 | 1952 | `36640b59` | same |
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   377`.

- [ ] **Step 6: Commit**

```bash
git add content/rules.yaml \
  docs/determinism.md \
  sim/Statecraft.Sim.Tests/DeterminismTests.cs \
  sim/Statecraft.Sim.Tests/Fixture.cs \
  sim/Statecraft.Sim.Tests/MapActionTests.cs \
  sim/Statecraft.Sim/AttentionSystem.cs \
  sim/Statecraft.Sim/Commands.cs \
  sim/Statecraft.Sim/Content.cs \
  sim/Statecraft.Sim/ContentLoader.cs \
  sim/Statecraft.Sim/GameState.cs \
  sim/Statecraft.Sim/GoldenRun.cs \
  sim/Statecraft.Sim/MapActions.cs \
  sim/Statecraft.Sim/Simulation.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(sim): MapAction for transit camps and housing near settlements from 1950

Both golden hashes move: the hash format gains map placements.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 9: Saves: seed, command log, content hash, schema version

*Track A.* Ruling 4 and spec §3/§7: a save is seed + command log + content hash + schema version; loading replays it to the same state hash; a content mismatch shows a warning; autosave every turn (the autosave itself is Task 21). The format is plain text, one item per line, with doubles as IEEE bit patterns so replays are exact, and percent-escaped strings so any command a player could send survives. `Content.Hash` covers every file the loader read (CRLF read as LF). `GameSession` is the one object the Unity bridge wraps: state, previous state, last step, and the save that records every turn. Migrations: v1 is the only schema; a newer version is refused with its number (Deferred: the migration path when v2 exists).

**Files:**
- Create: `sim/Statecraft.Sim.Tests/SaveGameTests.cs`, `sim/Statecraft.Sim/GameSession.cs`, `sim/Statecraft.Sim/SaveGame.cs`
- Modify: `sim/Statecraft.Sim/Content.cs`, `sim/Statecraft.Sim/ContentLoader.cs`

**Interfaces:**
- Consumes: `Simulation.NewGame/Step`, every `Command` type (Tasks 6 and 8 included), `GameState.Hash()`.
- Produces:
  - `Content.Hash : string` (lowercase SHA-256 hex).
  - `public sealed class SaveException : Exception { int Line; }` (message `line N: ...`).
  - `public sealed class SaveGame { const int SchemaVersion = 1; SaveGame(string contentHash, ulong seed, Mode mode, string initialHash); string ContentHash; ulong Seed; Mode Mode; IReadOnlyList<IReadOnlyList<Command>> Turns; string FinalHash; void Record(IReadOnlyList<Command>, GameState after); string Write(); static SaveGame Parse(string text); }`.
  - `public sealed class GameSession { Content Content; GameState State; GameState? Previous; StepResult? Last; SaveGame Save; static GameSession New(Content, ulong seed, Mode); StepResult Step(IReadOnlyList<Command>); static GameSession Load(Content, string saveText, out IReadOnlyList<string> warnings); }`. Warnings: `This save was made with different game content; the replayed game may differ.` and `The replayed game differs from the saved one.`; same content but a different end hash throws `SaveException` (determinism bug).

- [ ] **Step 1: Write the failing tests**

Create `sim/Statecraft.Sim.Tests/SaveGameTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>Saves: seed + command log + content hash + schema version; loading replays to the same state hash.</summary>
public class SaveGameTests
{
    static Content Fixture1() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event }));

    /// <summary>Ten turns using every command kind, including ones the sim rejects.</summary>
    static GameSession Played(Content c)
    {
        var g = GameSession.New(c, 99, Mode.Historical);
        var orders = new List<IReadOnlyList<Command>>
        {
            new Command[] { new SetMobilization(0.9), new SecurityOrder(-2) },
            new Command[] { new ChooseEventOption("drought", "ration"), new SetPolicy("rationing", 1.0 / 3) },
            new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 1.0 }) },
            new Command[] { new Appoint("finance_minister", "pm_one"), new MapAction("housing", 2, 0) },
            new Command[] { new SetPolicy("odd id with spaces%", 0.5), new MapAction("transit_camp", 4, 0) },
            new Command[0], new Command[0], new Command[] { new SecurityOrder(1) }, new Command[0], new Command[0],
        };
        foreach (var o in orders) g.Step(o);
        return g;
    }

    [Fact]
    public void ASaveReplaysToTheSameStateHash()
    {
        var c = Fixture1();
        var played = Played(c);
        var text = played.Save.Write();
        var loaded = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(played.State.Hash(), loaded.State.Hash());
        Assert.Equal(10, loaded.Save.Turns.Count);
        Assert.Equal(text, loaded.Save.Write());                         // writing the loaded save gives the same text
        Assert.Equal(played.Previous!.Hash(), loaded.Previous!.Hash());  // "since last turn" survives a load
    }

    [Fact]
    public void TheSaveRecordsTheHeaderAndExactNumbers()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var lines = text.Split('\n');
        Assert.Equal("statecraft-save 1", lines[0]);
        Assert.Equal("content " + c.Hash, lines[1]);
        Assert.Equal("seed 99", lines[2]);
        Assert.Equal("mode Historical", lines[3]);
        Assert.Contains("policy rationing 3FD5555555555555", lines);       // 1/3, bit for bit
        Assert.Contains("policy odd%20id%20with%20spaces%25 3FE0000000000000", lines);
        Assert.Contains("budget finance_minister=3FF0000000000000", lines);
        Assert.Contains("map housing 2 0", lines);
        Assert.Contains("security -2", lines);
        Assert.StartsWith("end ", lines[lines.Length - 2]);
    }

    [Fact]
    public void TheHistoricalPlaythroughTo1952RoundTrips()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < GoldenRun.Until1952) g.Step(HistoricalAI.Commands(c, g.State));
        Assert.Equal(GoldenRun.Hash1952, g.State.Hash());
        var loaded = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(GoldenRun.Hash1952, loaded.State.Hash());
    }

    [Fact]
    public void OtherContentLoadsWithAWarning()
    {
        var text = Played(Fixture1()).Save.Write();
        var other = ContentLoader.Load(Fixture.Write(
            new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("treasury_start: 100", "treasury_start: 90") },
            new Dictionary<string, string> { ["drought.yaml"] = ValidationTests.Event }));
        GameSession.Load(other, text, out var warnings);
        Assert.Equal(new[] { "This save was made with different game content; the replayed game may differ.", "The replayed game differs from the saved one." }, warnings);
    }

    [Fact]
    public void ADivergingReplayWithTheSameContentIsAnError()
    {
        var c = Fixture1();
        var text = Played(c).Save.Write();
        var lines = text.Split('\n');
        lines[2] = "seed 100";                                                // same content, different game
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Contains("did not reproduce the saved game", ex.Message);
    }

    [Theory]
    [InlineData("statecraft-save 2", 1, "line 1: save schema version 2 is newer than this game (1)")]
    [InlineData("statecraft-save x", 1, "line 1: bad schema version")]
    [InlineData("chess-save 1", 1, "line 1: expected 'statecraft-save'")]
    public void BadHeadersAreRejected(string first, int line, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n');
        lines[0] = first;
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal((line, message), (ex.Line, ex.Message));
    }

    [Theory]
    [InlineData("fly me 1 2", "unknown command 'fly'")]
    [InlineData("policy rationing 0.5", "bad number '0.5'")]
    [InlineData("map housing two 0", "bad integer 'two'")]
    [InlineData("event drought", "'event' needs 2 values")]
    public void BadCommandLinesNameTheLine(string command, string message)
    {
        var c = Fixture1();
        var lines = Played(c).Save.Write().Split('\n').ToList();
        lines.Insert(5, command);                                             // after the first "turn"
        var ex = Assert.Throws<SaveException>(() => GameSession.Load(c, string.Join("\n", lines), out _));
        Assert.Equal(6, ex.Line);
        Assert.Equal("line 6: " + message, ex.Message);
    }

    [Fact]
    public void TruncatedTextIsRejected()
    {
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse("statecraft-save 1\ncontent x\n"));
        Assert.Equal("Not a Statecraft save (too short)", ex.Message);
    }

    [Fact]
    public void TheContentHashCoversEveryFileAndIgnoresLineEndings()
    {
        var a = ContentLoader.Load(Fixture.Write());
        var b = ContentLoader.Load(Fixture.Write());
        Assert.Equal(a.Hash, b.Hash);
        Assert.Equal(64, a.Hash.Length);
        var crlf = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["parties.yaml"] = Fixture.Files["parties.yaml"].Replace("\n", "\r\n") }));
        Assert.Equal(a.Hash, crlf.Hash);
        var map = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = Fixture.Files["map/settlement_hexes.yaml"] + "# note\n" }));
        Assert.NotEqual(a.Hash, map.Hash);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: the build fails: `error CS0246: The type or namespace name 'GameSession' could not be found`.

- [ ] **Step 3: Implement**

Create `sim/Statecraft.Sim/SaveGame.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Statecraft.Sim
{
    public sealed class SaveException : Exception
    {
        /// <summary>1-based line of the save text the error is on (0 when it concerns the whole save).</summary>
        public int Line { get; }
        public SaveException(int line, string message) : base(line > 0 ? FormattableString.Invariant($"line {line}: {message}") : message) { Line = line; }
    }

    /// <summary>
    /// A save is seed + command log + content hash + schema version (spec section 3); loading replays it. Text format,
    /// one item per line:
    ///   statecraft-save 1          schema version
    ///   content &lt;hex&gt;             Content.Hash of the content it was played with
    ///   seed &lt;ulong&gt;  /  mode Historical
    ///   turn                       starts the next turn's command list (one per Simulation.Step)
    ///   &lt;command&gt; ...              policy ID LEVEL | budget OFFICE=SHARE,... | event ID OPTION | mobilize LEVEL |
    ///                              security DELTA | appoint OFFICE PERSON | map ACTION Q R
    ///   end &lt;hex&gt;                 GameState.Hash() after the last turn (checks the replay)
    /// Doubles are IEEE bit patterns in hex, so replays are exact; strings are percent-escaped (Uri.EscapeDataString).
    /// </summary>
    public sealed class SaveGame
    {
        public const int SchemaVersion = 1;
        const string Magic = "statecraft-save";

        readonly List<IReadOnlyList<Command>> turns = new List<IReadOnlyList<Command>>();

        public string ContentHash { get; }
        public ulong Seed { get; }
        public Mode Mode { get; }
        public IReadOnlyList<IReadOnlyList<Command>> Turns => turns;
        /// <summary>GameState.Hash() after the last recorded turn (the new game's hash before any turn).</summary>
        public string FinalHash { get; private set; }

        public SaveGame(string contentHash, ulong seed, Mode mode, string initialHash)
        {
            ContentHash = contentHash ?? throw new ArgumentNullException(nameof(contentHash));
            Seed = seed; Mode = mode;
            FinalHash = initialHash ?? throw new ArgumentNullException(nameof(initialHash));
        }

        /// <summary>Appends one turn: the commands passed to Simulation.Step and the state it returned.</summary>
        public void Record(IReadOnlyList<Command> commands, GameState after)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (after == null) throw new ArgumentNullException(nameof(after));
            turns.Add(commands.ToList());
            FinalHash = after.Hash();
        }

        public string Write()
        {
            var sb = new StringBuilder();
            sb.Append(Magic).Append(' ').Append(SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("content ").Append(ContentHash).Append('\n');
            sb.Append("seed ").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("mode ").Append(Mode.ToString()).Append('\n');
            foreach (var t in turns)
            {
                sb.Append("turn\n");
                foreach (var cmd in t) sb.Append(Format(cmd)).Append('\n');
            }
            sb.Append("end ").Append(FinalHash).Append('\n');
            return sb.ToString();
        }

        static string Esc(string s) => Uri.EscapeDataString(s);
        static string Num(double v) => BitConverter.DoubleToInt64Bits(v).ToString("X16", CultureInfo.InvariantCulture);
        static string Int(int v) => v.ToString(CultureInfo.InvariantCulture);

        static string Format(Command cmd) => cmd switch
        {
            SetPolicy p => $"policy {Esc(p.Policy)} {Num(p.Level)}",
            AllocateBudget b => "budget " + (b.Shares == null || b.Shares.Count == 0 ? "-" :
                string.Join(",", b.Shares.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => Esc(kv.Key) + "=" + Num(kv.Value)))),
            ChooseEventOption e => $"event {Esc(e.EventId)} {Esc(e.OptionId)}",
            SetMobilization m => $"mobilize {Num(m.Level)}",
            SecurityOrder o => $"security {Int(o.Delta)}",
            Appoint a => $"appoint {Esc(a.Office)} {Esc(a.Person)}",
            MapAction m => $"map {Esc(m.Action)} {Int(m.Q)} {Int(m.R)}",
            _ => throw new ArgumentException($"Cannot save command kind '{cmd.Kind}'", nameof(cmd)),
        };

        /// <summary>Parses save text; any malformed line throws SaveException naming the line.</summary>
        public static SaveGame Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var lines = text.Replace("\r\n", "\n").Split('\n');
            int n = lines.Length;
            while (n > 0 && lines[n - 1] == "") n--;
            if (n < 5) throw new SaveException(0, "Not a Statecraft save (too short)");
            string[] Head(int i, string key, int parts)
            {
                var f = lines[i].Split(' ');
                if (f[0] != key || f.Length != parts) throw new SaveException(i + 1, $"expected '{key}'");
                return f;
            }
            var magic = Head(0, Magic, 2);
            if (!int.TryParse(magic[1], NumberStyles.None, CultureInfo.InvariantCulture, out var version)) throw new SaveException(1, "bad schema version");
            if (version > SchemaVersion) throw new SaveException(1, FormattableString.Invariant($"save schema version {version} is newer than this game ({SchemaVersion})"));
            if (version < 1) throw new SaveException(1, FormattableString.Invariant($"unknown save schema version {version}"));
            var content = Head(1, "content", 2)[1];
            if (!ulong.TryParse(Head(2, "seed", 2)[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seed)) throw new SaveException(3, "bad seed");
            if (!Enum.TryParse<Mode>(Head(3, "mode", 2)[1], false, out var mode) || !Enum.IsDefined(typeof(Mode), mode)) throw new SaveException(4, "bad mode");
            var end = Head(n - 1, "end", 2)[1];
            var save = new SaveGame(content, seed, mode, end);
            List<Command>? current = null;
            for (int i = 4; i < n - 1; i++)
            {
                if (lines[i] == "turn") { current = new List<Command>(); save.turns.Add(current); continue; }
                if (current == null) throw new SaveException(i + 1, "command before the first 'turn'");
                current.Add(ParseCommand(i + 1, lines[i]));
            }
            return save;
        }

        static Command ParseCommand(int line, string text)
        {
            var f = text.Split(' ');
            void Arity(int count) { if (f.Length != count) throw new SaveException(line, $"'{f[0]}' needs {count - 1} values"); }
            string Str(string s)
            {
                try { return Uri.UnescapeDataString(s); }
                catch (UriFormatException) { throw new SaveException(line, $"bad text '{s}'"); }
            }
            double Dbl(string s) => long.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var bits) && s.Length == 16
                ? BitConverter.Int64BitsToDouble(bits) : throw new SaveException(line, $"bad number '{s}'");
            int I(string s) => int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v) ? v : throw new SaveException(line, $"bad integer '{s}'");
            switch (f[0])
            {
                case "policy": Arity(3); return new SetPolicy(Str(f[1]), Dbl(f[2]));
                case "budget":
                    Arity(2);
                    var shares = new Dictionary<string, double>();
                    if (f[1] != "-")
                        foreach (var pair in f[1].Split(','))
                        {
                            var kv = pair.Split('=');
                            if (kv.Length != 2) throw new SaveException(line, $"bad budget share '{pair}'");
                            shares[Str(kv[0])] = Dbl(kv[1]);
                        }
                    return new AllocateBudget(shares);
                case "event": Arity(3); return new ChooseEventOption(Str(f[1]), Str(f[2]));
                case "mobilize": Arity(2); return new SetMobilization(Dbl(f[1]));
                case "security": Arity(2); return new SecurityOrder(I(f[1]));
                case "appoint": Arity(3); return new Appoint(Str(f[1]), Str(f[2]));
                case "map": Arity(4); return new MapAction(Str(f[1]), I(f[2]), I(f[3]));
                default: throw new SaveException(line, $"unknown command '{f[0]}'");
            }
        }
    }
}
```

Create `sim/Statecraft.Sim/GameSession.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>One playthrough: the current state, the turn before it, the last step's report, and the save that
    /// records every turn. The Unity SimBridge wraps this; the CLI and tests can use it directly.</summary>
    public sealed class GameSession
    {
        public Content Content { get; }
        public GameState State { get; private set; }
        /// <summary>The state before the last step (null before the first), for "change since last turn".</summary>
        public GameState? Previous { get; private set; }
        /// <summary>The last step's result (null before the first): its Why and Rejected lists.</summary>
        public StepResult? Last { get; private set; }
        public SaveGame Save { get; }

        GameSession(Content c, GameState s, SaveGame save) { Content = c; State = s; Save = save; }

        public static GameSession New(Content c, ulong seed, Mode mode)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, seed, mode);
            return new GameSession(c, s, new SaveGame(c.Hash, seed, mode, s.Hash()));
        }

        /// <summary>Runs one turn and records it in the save.</summary>
        public StepResult Step(IReadOnlyList<Command> commands)
        {
            var r = Simulation.Step(Content, State, commands);
            Save.Record(commands, r.State);
            Previous = State; State = r.State; Last = r;
            return r;
        }

        /// <summary>Parses and replays a save. Throws SaveException when the text is malformed, or when the content is the
        /// same but the replay ends on a different hash (a determinism bug). Content made with other content loads with a
        /// warning (spec section 7), and a second warning if the replay then differs from the saved game.</summary>
        public static GameSession Load(Content c, string saveText, out IReadOnlyList<string> warnings)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var saved = SaveGame.Parse(saveText);
            var list = new List<string>();
            bool sameContent = saved.ContentHash == c.Hash;
            if (!sameContent) list.Add("This save was made with different game content; the replayed game may differ.");
            var session = New(c, saved.Seed, saved.Mode);
            foreach (var turn in saved.Turns) session.Step(turn);
            if (session.State.Hash() != saved.FinalHash)
            {
                if (sameContent) throw new SaveException(0, "The replay did not reproduce the saved game (determinism bug): please report it with the save file.");
                list.Add("The replayed game differs from the saved one.");
            }
            warnings = list;
            return session;
        }
    }
}
```

In `sim/Statecraft.Sim/Content.cs`:

1. Replace

```csharp
        public List<SettlementHex> SettlementHexes = new List<SettlementHex>();
        Dictionary<(int, int), MapHex>? _hexIndex;
```

   with

```csharp
        public List<SettlementHex> SettlementHexes = new List<SettlementHex>();
        /// <summary>SHA-256 (lowercase hex) over every file the loader read, in read order, with CRLF read as LF. A save
        /// records it; loading a save made with other content shows a warning (spec section 7).</summary>
        public string Hash = "";
        Dictionary<(int, int), MapHex>? _hexIndex;
```

In `sim/Statecraft.Sim/ContentLoader.cs`:

1. Replace

```csharp
            if (eventFiles == null) throw new ArgumentNullException(nameof(eventFiles));
            var c = new Content();
```

   with

```csharp
            if (eventFiles == null) throw new ArgumentNullException(nameof(eventFiles));
            var read = new List<(string rel, string text)>();
            var inner = readFile;
            readFile = rel =>
            {
                var text = inner(rel);
                if (text != null) read.Add((rel, text));
                return text;
            };
            var c = new Content();
```

2. Replace

```csharp
            Validate(c);
            return c;
        }
```

   with

```csharp
            Validate(c);
            c.Hash = HashOf(read);
            return c;
        }

        /// <summary>SHA-256 over "path\nlength\ntext" for each file, CRLF normalised to LF (a Windows checkout hashes alike).</summary>
        static string HashOf(List<(string rel, string text)> files)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (rel, text) in files)
            {
                var t = text.Replace("\r\n", "\n");
                sb.Append(rel).Append('\n').Append(t.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n').Append(t);
            }
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
        }
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
dotnet test sim/Statecraft.sln
```
Expected: `Passed!  - Failed:     0, Passed:   391`. Golden hashes unchanged.

- [ ] **Step 5: Commit**

```bash
git add sim/Statecraft.Sim.Tests/SaveGameTests.cs \
  sim/Statecraft.Sim/Content.cs \
  sim/Statecraft.Sim/ContentLoader.cs \
  sim/Statecraft.Sim/GameSession.cs \
  sim/Statecraft.Sim/SaveGame.cs
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(sim): saves (seed, command log, content hash, schema version) that replay to the same hash

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Phase 1, Track B: UI shell (Task 10)

### Task 10: UI foundation: theme, fonts, panel settings, strings, the test harness

*Track B.* Ruling 2's look and ruling 5's panel settings, and the edit-mode harness every later UI task uses (tech notes §1.6, blocker 1: run the UIDocument + NavigationSubmitEvent pattern early). One UXML (`Game.uxml`) holds the whole screen: the top bar now, and empty, named containers for the event card, info panel, minimap and the four screens that Tasks 15–20 fill. Fonts are EB Garamond (OFL), static TTFs pinned to a commit; USS's `-unity-font-definition: url(...ttf)` takes a Font directly (verified: the resolved style is the TTF). The PanelSettings asset is made by an editor script so its theme and shaders ship in builds (tech notes §1.3–1.4). `UiPointer` answers "is the pointer over the UI?" for Task 11's camera and Task 20's map clicks. A layout test renders the panel into 1920×1080 and 1280×720 targets and checks that no region overlaps another and the six meters fit (amendment A5: meters have a 130 px minimum and may shrink; centre screens sit between the info panel and the event card).

**Files:**
- Create: `unity/Assets/Editor/UiAssets.cs`, `unity/Assets/Scripts/UI/UiPaths.cs`, `unity/Assets/Scripts/UI/UiPointer.cs`, `unity/Assets/Scripts/UI/UiText.cs`, `unity/Assets/Tests/EditMode/UiFoundationTests.cs`, `unity/Assets/Tests/EditMode/UiLayoutTests.cs`, `unity/Assets/Tests/EditMode/UiTestKit.cs`, `unity/Assets/UI/Fonts/EBGaramond-Regular.ttf`, `unity/Assets/UI/Fonts/EBGaramond-SemiBold.ttf`, `unity/Assets/UI/Fonts/OFL.txt`, `unity/Assets/UI/Fonts/SOURCE.txt`, `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/Statecraft.uss`, `unity/Assets/UI/StatecraftPanel.asset`, `unity/Assets/UI/StatecraftTheme.tss`, `unity/Assets/UI/strings.en.yaml`
- Modify: `unity/Assets/link.xml`
- Generated by a command in this task (commit the output): `unity/Assets/UI/Fonts/EBGaramond-Regular.ttf`, `unity/Assets/UI/Fonts/EBGaramond-SemiBold.ttf`, `unity/Assets/UI/Fonts/OFL.txt`, `unity/Assets/UI/StatecraftPanel.asset`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Editor/UiAssets.cs.meta`, `unity/Assets/Scripts/UI.meta`, `unity/Assets/Scripts/UI/UiPaths.cs.meta`, `unity/Assets/Scripts/UI/UiPointer.cs.meta`, `unity/Assets/Scripts/UI/UiText.cs.meta`, `unity/Assets/Tests/EditMode/UiFoundationTests.cs.meta`, `unity/Assets/Tests/EditMode/UiLayoutTests.cs.meta`, `unity/Assets/Tests/EditMode/UiTestKit.cs.meta`, `unity/Assets/UI.meta`, `unity/Assets/UI/Fonts.meta`, `unity/Assets/UI/Fonts/EBGaramond-Regular.ttf.meta`, `unity/Assets/UI/Fonts/EBGaramond-SemiBold.ttf.meta`, `unity/Assets/UI/Fonts/OFL.txt.meta`, `unity/Assets/UI/Fonts/SOURCE.txt.meta`, `unity/Assets/UI/Game.uxml.meta`, `unity/Assets/UI/Statecraft.uss.meta`, `unity/Assets/UI/StatecraftPanel.asset.meta`, `unity/Assets/UI/StatecraftTheme.tss.meta`, `unity/Assets/UI/strings.en.yaml.meta`

**Interfaces:**
- Consumes: nothing from other M3 tasks (runs on `main`).
- Produces:
  - `Statecraft.Game.UI.UiPaths` constants: `Panel = "Assets/UI/StatecraftPanel.asset"`, `Theme`, `GameUxml = "Assets/UI/Game.uxml"`, `Strings = "Assets/UI/strings.en.yaml"`, `FontRegular`, `FontSemiBold`.
  - `public sealed class UiText { static UiText Parse(string yaml, string file = "strings.en.yaml"); string this[string key]; string Format(string key, params object[] args); }` (invariant culture; a missing key throws `KeyNotFoundException`).
  - `public static class UiPointer { static Vector2 ToPanel(IPanel, Vector2 screenYUp, float screenHeight); static bool IsOverUi(IPanel?, Vector2 screenYUp, float screenHeight); }`.
  - Editor: `Statecraft.Game.EditorTools.UiAssets.Create()` (idempotent).
  - Tests: `public sealed class UiTestKit : IDisposable { UiTestKit(string uxmlPath = UiPaths.GameUxml); VisualElement Root; T Q<T>(string name); static void Press(Button); void Press(string name); }`.
  - Element names in `Game.uxml`: `year`, `turn`, `date`, `<meter>-value`/`<meter>-delta` for `treasury`, `immigration`, `security`, `support`, `standing`, `fx`; `security-level`; `nav-cabinet`, `nav-budget`, `nav-defence`, `nav-why`; `attention`; `end-turn`; containers `event-card`, `info-panel`, `minimap`, `screen-cabinet`, `screen-budget`, `screen-defence`, `screen-why`; `toast`. USS classes used later: `panel`, `title`, `muted`, `delta-up`, `delta-down`, `row`, `grow`, `rung`, `rung-current`, `draft`, `why-group`, `why-line`, `seat`, `seat-portrait`, `seat-selected`, `appointed`, `candidate`, `reason`, `minimap-image`, `minimap-view`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/UiTestKit.cs`:

```csharp
using System;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Edit-mode UI harness (tech notes 1.6): a UIDocument on a fresh GameObject gives the tree a real runtime
/// panel, so events dispatch. Buttons are pressed with NavigationSubmitEvent, which fires Button.clicked; a synthetic
/// ClickEvent does not.</summary>
public sealed class UiTestKit : IDisposable
{
    public readonly GameObject Go;
    public readonly UIDocument Doc;
    public VisualElement Root => Doc.rootVisualElement;

    public UiTestKit(string uxmlPath = UiPaths.GameUxml)
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
        var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        Assert.IsNotNull(ps, UiPaths.Panel + " (run UiAssets.Create)");
        Assert.IsNotNull(vta, uxmlPath);
        Go = new GameObject("UITest");
        Go.SetActive(false);
        Doc = Go.AddComponent<UIDocument>();
        Doc.panelSettings = ps;
        Doc.visualTreeAsset = vta;
        Go.SetActive(true);                                    // OnEnable -> RecreateUI
        Assert.IsNotNull(Root.panel, "the UIDocument tree has no panel");
    }

    public T Q<T>(string name) where T : VisualElement
    {
        var e = Root.Q<T>(name);
        Assert.IsNotNull(e, $"no {typeof(T).Name} named '{name}'");
        return e;
    }

    /// <summary>Presses a button the way keyboard/gamepad Submit does: Button.clicked fires synchronously.</summary>
    public static void Press(Button b)
    {
        Assert.IsNotNull(b.panel, $"button '{b.name}' is not on a panel");
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
    }

    public void Press(string buttonName) => Press(Q<Button>(buttonName));

    public void Dispose() => Object.DestroyImmediate(Go);
}
```

Create `unity/Assets/Tests/EditMode/UiFoundationTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class UiFoundationTests
{
    [Test]
    public void PanelSettingsScaleWithA1080pReference()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
        Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ps.scaleMode);
        Assert.AreEqual(new Vector2Int(1920, 1080), ps.referenceResolution);
        Assert.AreEqual(PanelScreenMatchMode.MatchWidthOrHeight, ps.screenMatchMode);
        Assert.AreEqual(0.5f, ps.match);
        Assert.IsNotNull(ps.themeStyleSheet, "theme");
    }

    [Test]
    public void ASubmittedButtonFiresClickedSynchronously()
    {
        using var ui = new UiTestKit();
        int clicks = 0;
        ui.Q<Button>("end-turn").clicked += () => clicks++;
        ui.Press("end-turn");
        Assert.AreEqual(1, clicks);
    }

    [Test]
    public void TheLayoutHasEveryRegionOfTheTargetScreen()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "top-bar", "event-card", "info-panel", "minimap", "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name);
        foreach (var name in new[] { "treasury", "immigration", "security", "support", "standing", "fx" })
        {
            ui.Q<Label>(name + "-value");
            ui.Q<Label>(name + "-delta");
        }
        Assert.AreEqual(PickingMode.Ignore, ui.Q<VisualElement>("root").pickingMode, "empty screen areas must let clicks reach the map");
    }

    [UnityTest]
    public IEnumerator TextUsesTheSerifFaces()
    {
        using var ui = new UiTestKit();
        for (int i = 0; i < 10 && ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font == null; i++) yield return null;
        var regular = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontRegular);
        var semibold = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontSemiBold);
        Assert.AreSame(semibold, ui.Q<Label>("year").resolvedStyle.unityFontDefinition.font);
        Assert.AreSame(regular, ui.Q<Label>("turn").resolvedStyle.unityFontDefinition.font);
    }

    [Test]
    public void StringsLoadFromTheYamlFileAndFormatInvariantly()
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        Assert.AreEqual("Turn 3", text.Format("hud.turn", 3));
        Assert.AreEqual("1,234,567", text.Format("hud.people", 1234567.0));
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => { var _ = text["no.such.key"]; });
    }

    [UnityTest]
    public IEnumerator PickingFindsButtonsButNotTheTransparentRoot()
    {
        using var ui = new UiTestKit();
        foreach (var name in new[] { "screen-cabinet", "screen-budget", "screen-defence", "screen-why", "toast" })
            ui.Q<VisualElement>(name).style.display = DisplayStyle.None;   // closed, as at game start
        var button = ui.Q<Button>("end-turn");
        for (int i = 0; i < 10 && float.IsNaN(button.worldBound.width); i++) yield return null;
        var panel = ui.Root.panel;
        Assert.AreSame(button, panel.Pick(button.worldBound.center));
        Assert.IsNull(panel.Pick(ui.Root.worldBound.center), "the empty middle of the screen must reach the map");
        // Input System screen positions are y-up; ToPanel flips y before RuntimePanelUtils.ScreenToPanel (which does not).
        Assert.AreEqual(RuntimePanelUtils.ScreenToPanel(panel, new Vector2(10, 20)), UiPointer.ToPanel(panel, new Vector2(10, 1060), 1080f));
    }
}
```

Create `unity/Assets/Tests/EditMode/UiLayoutTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The screen layout at 1920x1080 and 1280x720 (the panel scales with the screen against a 1080p reference,
/// so both resolve to the same logical layout): no fixed region overlaps another, no centre screen covers a fixed region,
/// and the six top-bar meters fit before the screen buttons at their minimum width.</summary>
public class UiLayoutTests
{
    static readonly string[] Fixed = { "top-bar", "event-card", "info-panel", "minimap" };
    static readonly string[] Screens = { "screen-cabinet", "screen-budget", "screen-defence", "screen-why" };

    [UnityTest]
    public IEnumerator NothingOverlapsAt1080pAnd720p()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("LayoutTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
            try
            {
                Rect R(string name) => root.Q(name).worldBound;
                foreach (var a in Fixed)
                    foreach (var b in Fixed)
                        if (string.CompareOrdinal(a, b) < 0) Assert.IsFalse(R(a).Overlaps(R(b)), $"{w}x{h}: {a} {R(a)} overlaps {b} {R(b)}");
                foreach (var s in Screens)
                    foreach (var f in Fixed)
                        Assert.IsFalse(R(s).Overlaps(R(f)), $"{w}x{h}: {s} {R(s)} overlaps {f} {R(f)}");
                Assert.LessOrEqual(R("meters").xMax, R("nav").xMin + 0.5f, $"{w}x{h}: the meters run into the screen buttons");
                foreach (var m in root.Q("meters").Children())
                    Assert.GreaterOrEqual(m.worldBound.width, 128.5f, $"{w}x{h}: {m.name} is squeezed below its minimum width");   // 130 less pixel rounding at 2/3 scale
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1` and the log has `error CS0246: The type or namespace name 'UiPaths' could not be found` (and `UiText`, `UiPointer`).

- [ ] **Step 3: Add the fonts (pinned, with their licence)**

Run:
```bash
mkdir -p unity/Assets/UI/Fonts && cd unity/Assets/UI/Fonts
C=106a4a6d377987459ae5e68673a4570f13b957fb
curl -sfL -o EBGaramond-Regular.ttf "https://raw.githubusercontent.com/octaviopardo/EBGaramond12/$C/fonts/ttf/EBGaramond-Regular.ttf"
curl -sfL -o EBGaramond-SemiBold.ttf "https://raw.githubusercontent.com/octaviopardo/EBGaramond12/$C/fonts/ttf/EBGaramond-SemiBold.ttf"
curl -sfL -o OFL.txt "https://raw.githubusercontent.com/octaviopardo/EBGaramond12/$C/OFL.txt"
shasum -a 256 *.ttf; cd -
```
Expected: `2028dc06d3c130b4761693481436a32a8e35ed500bf58c25c53de004106125b8  EBGaramond-Regular.ttf` and `ebf827a102983972abb2a1a3964afd5a7b79ca04f2ddd9e53917ce623cefdf2c  EBGaramond-SemiBold.ttf`.

Create `unity/Assets/UI/Fonts/SOURCE.txt`:

```text
EB Garamond (static TTFs), SIL Open Font License 1.1 (OFL.txt in this folder).
Source: https://github.com/octaviopardo/EBGaramond12 at commit 106a4a6d377987459ae5e68673a4570f13b957fb, fonts/ttf/
sha256 2028dc06d3c130b4761693481436a32a8e35ed500bf58c25c53de004106125b8  EBGaramond-Regular.ttf
sha256 ebf827a102983972abb2a1a3964afd5a7b79ca04f2ddd9e53917ce623cefdf2c  EBGaramond-SemiBold.ttf
```

- [ ] **Step 4: Write the theme, layout, strings and code**

Create `unity/Assets/UI/StatecraftTheme.tss`:

```css
@import url("unity-theme://default");
```

Create `unity/Assets/UI/Statecraft.uss`:

```css
/* Statecraft UI language (target: promo/final/03-gameplay-1936.png): dark navy panels, thin gold borders, serif type. */
:root {
    --navy: rgba(14, 22, 38, 0.94);
    --navy-deep: rgb(9, 14, 26);
    --gold: rgb(196, 160, 92);
    --gold-dim: rgba(196, 160, 92, 0.45);
    --ink: rgb(236, 228, 210);
    --ink-dim: rgb(170, 164, 150);
    --up: rgb(126, 196, 120);
    --down: rgb(222, 120, 104);
}

.root {
    flex-grow: 1;
    -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-Regular.ttf");
    color: var(--ink);
    font-size: 20px;
}

.panel {
    background-color: var(--navy);
    border-color: var(--gold-dim);
    border-width: 1px;
    border-radius: 4px;
    padding: 12px;
}

.title {
    -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-SemiBold.ttf");
    font-size: 30px;
    color: var(--ink);
}

.muted { color: var(--ink-dim); }
.delta-up { color: var(--up); }
.delta-down { color: var(--down); }

.button, Button {
    background-color: var(--navy-deep);
    border-color: var(--gold-dim);
    border-width: 1px;
    border-radius: 3px;
    color: var(--ink);
    -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-Regular.ttf");
    font-size: 19px;
    padding: 6px 12px;
    margin: 3px 0;
    -unity-text-align: middle-left;
}
Button:hover { border-color: var(--gold); }
Button:disabled { color: var(--ink-dim); border-color: rgba(120, 120, 120, 0.4); }

/* Top bar: date box, national indicators with value and change, screen buttons, attention and End turn. */
.top-bar {
    position: absolute; left: 0; right: 0; top: 0; height: 96px;
    flex-direction: row; align-items: center;
    border-width: 0 0 1px 0; border-radius: 0; padding: 0 16px;
}
.date-box { width: 190px; border-right-width: 1px; border-color: var(--gold-dim); padding-right: 12px; }
.year { -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-SemiBold.ttf"); font-size: 36px; }
.meters { flex-direction: row; flex-grow: 1; }
.meter { min-width: 130px; flex-shrink: 1; padding: 0 12px; border-right-width: 1px; border-color: var(--gold-dim); }
.meter-name { font-size: 18px; color: var(--ink-dim); }
.meter-row { flex-direction: row; align-items: flex-end; }
.meter-value { font-size: 26px; }
.meter-delta { font-size: 18px; margin-left: 8px; }
.nav { flex-direction: row; }
.nav Button { margin: 0 4px; -unity-text-align: middle-center; }
.turn-box { width: 190px; align-items: flex-end; }
.end-turn { -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-SemiBold.ttf"); font-size: 22px; border-color: var(--gold); }

/* Event card at the right, info panel at the bottom-left, minimap at the bottom-right. */
.event-card { position: absolute; right: 16px; top: 112px; width: 420px; max-height: 560px; overflow: hidden; }
.event-image { height: 200px; margin-bottom: 8px; -unity-background-scale-mode: scale-and-crop; background-color: var(--navy-deep); }
.event-title { -unity-text-align: middle-center; margin-bottom: 6px; }
.event-text { white-space: normal; margin-bottom: 8px; }
.draft { color: var(--down); -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-SemiBold.ttf"); -unity-text-align: middle-center; }
.info-panel { position: absolute; left: 16px; bottom: 16px; width: 520px; min-height: 160px; }
.minimap { position: absolute; right: 16px; bottom: 16px; width: 300px; height: 340px; padding: 6px; }
.minimap-image { flex-grow: 1; -unity-background-scale-mode: scale-to-fit; }
.minimap-view { position: absolute; border-color: var(--ink); border-width: 1px; }

/* Full screens (Cabinet, Budget, Defence, Why) open in the centre, one at a time. */
.screen { position: absolute; left: 552px; right: 452px; top: 112px; bottom: 16px; }
.screen-header { flex-direction: row; justify-content: space-between; align-items: center; border-bottom-width: 1px; border-color: var(--gold-dim); margin-bottom: 8px; }
.row { flex-direction: row; align-items: center; }
.rung { flex-direction: row; padding: 4px 8px; border-left-width: 3px; border-color: rgba(0, 0, 0, 0); }
.rung-current { border-color: var(--gold); background-color: rgba(196, 160, 92, 0.15); }
.grow { flex-grow: 1; }

.toast { position: absolute; left: 30%; right: 30%; top: 112px; -unity-text-align: middle-center; white-space: normal; }

.why-group { -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-SemiBold.ttf"); color: var(--gold); margin-top: 8px; }
.why-line { white-space: normal; padding-left: 12px; }

.seat { flex-direction: row; align-items: center; }
.seat-portrait { width: 40px; height: 48px; margin-right: 8px; -unity-background-scale-mode: scale-and-crop; background-color: var(--navy-deep); }
.seat-selected { border-color: var(--gold); }
.appointed { color: var(--gold); }
.candidate { flex-direction: row; align-items: center; }
.reason { color: var(--ink-dim); font-size: 16px; white-space: normal; flex-shrink: 1; }
```

Create `unity/Assets/UI/Game.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="Statecraft.uss" />
    <ui:VisualElement name="root" class="root" picking-mode="Ignore">
        <ui:VisualElement name="top-bar" class="panel top-bar">
            <ui:VisualElement class="date-box">
                <ui:Label name="year" class="year" />
                <ui:Label name="turn" class="muted" />
                <ui:Label name="date" class="muted" />
            </ui:VisualElement>
            <ui:VisualElement name="meters" class="meters">
                <ui:VisualElement name="meter-treasury" class="meter">
                    <ui:Label text="Treasury" class="meter-name" />
                    <ui:VisualElement class="meter-row"><ui:Label name="treasury-value" class="meter-value" /><ui:Label name="treasury-delta" class="meter-delta" /></ui:VisualElement>
                </ui:VisualElement>
                <ui:VisualElement name="meter-immigration" class="meter">
                    <ui:Label text="Immigration" class="meter-name" />
                    <ui:VisualElement class="meter-row"><ui:Label name="immigration-value" class="meter-value" /><ui:Label name="immigration-delta" class="meter-delta" /></ui:VisualElement>
                </ui:VisualElement>
                <ui:VisualElement name="meter-security" class="meter">
                    <ui:Label text="Security" class="meter-name" />
                    <ui:VisualElement class="meter-row"><ui:Label name="security-value" class="meter-value" /><ui:Label name="security-delta" class="meter-delta" /></ui:VisualElement>
                    <ui:Label name="security-level" class="meter-name" />
                </ui:VisualElement>
                <ui:VisualElement name="meter-support" class="meter">
                    <ui:Label text="Public Support" class="meter-name" />
                    <ui:VisualElement class="meter-row"><ui:Label name="support-value" class="meter-value" /><ui:Label name="support-delta" class="meter-delta" /></ui:VisualElement>
                </ui:VisualElement>
                <ui:VisualElement name="meter-standing" class="meter">
                    <ui:Label text="International Standing" class="meter-name" />
                    <ui:VisualElement class="meter-row"><ui:Label name="standing-value" class="meter-value" /><ui:Label name="standing-delta" class="meter-delta" /></ui:VisualElement>
                </ui:VisualElement>
                <ui:VisualElement name="meter-fx" class="meter">
                    <ui:Label text="Foreign Currency" class="meter-name" />
                    <ui:VisualElement class="meter-row"><ui:Label name="fx-value" class="meter-value" /><ui:Label name="fx-delta" class="meter-delta" /></ui:VisualElement>
                </ui:VisualElement>
            </ui:VisualElement>
            <ui:VisualElement name="nav" class="nav">
                <ui:Button name="nav-cabinet" text="Cabinet" />
                <ui:Button name="nav-budget" text="Budget" />
                <ui:Button name="nav-defence" text="Defence" />
                <ui:Button name="nav-why" text="Why?" />
            </ui:VisualElement>
            <ui:VisualElement class="turn-box">
                <ui:Label name="attention" class="muted" />
                <ui:Button name="end-turn" text="End Turn" class="end-turn" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="event-card" class="panel event-card" />
        <ui:VisualElement name="info-panel" class="panel info-panel" />
        <ui:VisualElement name="minimap" class="panel minimap" />
        <ui:VisualElement name="screen-cabinet" class="panel screen" />
        <ui:VisualElement name="screen-budget" class="panel screen" />
        <ui:VisualElement name="screen-defence" class="panel screen" />
        <ui:VisualElement name="screen-why" class="panel screen" />
        <ui:Label name="toast" class="panel toast" />
    </ui:VisualElement>
</ui:UXML>
```

Create `unity/Assets/UI/strings.en.yaml`:

```yaml
# UI strings (English). Code looks strings up by key (Statecraft.Game.UI.UiText); placeholders are string.Format
# arguments ({0}, {1}) formatted with the invariant culture. Static labels live in the UXML files.
hud.turn: "Turn {0}"
hud.date: "{0:d MMMM yyyy}"
hud.attention: "Attention {0:0.0} of {1:0.0}"
hud.money: "{0:0.0}m"
hud.people: "{0:N0}"
hud.percent: "{0:0}"
hud.fx: "${0:0.0}m"
hud.level: "Level {0} of 5"
hud.delta: "{0:+0;-0}"
hud.delta_money: "{0:+0.0;-0.0}"
toast.refused: "Not done: {0}"
```

Create `unity/Assets/Scripts/UI/UiPaths.cs`:

```csharp
#nullable enable
namespace Statecraft.Game.UI
{
    /// <summary>Project paths of the UI assets, shared by the editor scripts (Assets/Editor) and the edit-mode tests,
    /// which cannot reference the editor assembly.</summary>
    public static class UiPaths
    {
        public const string Folder = "Assets/UI";
        public const string Panel = Folder + "/StatecraftPanel.asset";
        public const string Theme = Folder + "/StatecraftTheme.tss";
        public const string GameUxml = Folder + "/Game.uxml";
        public const string Strings = Folder + "/strings.en.yaml";
        public const string FontRegular = Folder + "/Fonts/EBGaramond-Regular.ttf";
        public const string FontSemiBold = Folder + "/Fonts/EBGaramond-SemiBold.ttf";
    }
}
```

Create `unity/Assets/Scripts/UI/UiText.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using YamlDotNet.Serialization;

namespace Statecraft.Game.UI
{
    /// <summary>UI strings kept outside code (unity/Assets/UI/strings.en.yaml): key -> string.Format pattern.</summary>
    public sealed class UiText
    {
        readonly Dictionary<string, string> map;
        UiText(Dictionary<string, string> map) { this.map = map; }

        /// <summary>Parses the strings file. Throws InvalidOperationException naming the file when it is empty or not a map.</summary>
        public static UiText Parse(string yaml, string file = "strings.en.yaml")
        {
            Dictionary<string, string> m;
            try { m = new DeserializerBuilder().WithDuplicateKeyChecking().Build().Deserialize<Dictionary<string, string>>(yaml ?? ""); }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException($"{file}: {e.Message}", e); }
            if (m == null || m.Count == 0) throw new InvalidOperationException($"{file}: no strings");
            return new UiText(m);
        }

        public string this[string key] =>
            map.TryGetValue(key, out var v) ? v : throw new KeyNotFoundException($"strings.en.yaml has no key '{key}'");

        public string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, this[key], args);
    }
}
```

Create `unity/Assets/Scripts/UI/UiPointer.cs`:

```csharp
#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>"Is the pointer over the UI?" for camera and map clicks (tech notes section 5). Input System positions are
    /// y-up; RuntimePanelUtils.ScreenToPanel does not flip, and panel space is y-down, so y is flipped first. Elements with
    /// picking-mode Ignore (the root) let the pointer through to the map.</summary>
    public static class UiPointer
    {
        public static Vector2 ToPanel(IPanel panel, Vector2 screenYUp, float screenHeight) =>
            RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenYUp.x, screenHeight - screenYUp.y));

        public static bool IsOverUi(IPanel? panel, Vector2 screenYUp, float screenHeight) =>
            panel != null && panel.Pick(ToPanel(panel, screenYUp, screenHeight)) != null;
    }
}
```

Create `unity/Assets/Editor/UiAssets.cs`:

```csharp
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the UI Toolkit PanelSettings asset (tech notes 1.3): scale with screen size against 1920x1080,
    /// matching width and height equally, with the default runtime theme. An asset, not a runtime instance, so its theme
    /// and shader references are included in player builds (the M0 Shader.Find lesson). Idempotent.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.UiAssets.Create</summary>
    public static class UiAssets
    {
        public static void Create()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPaths.Theme);
            if (theme == null) throw new System.IO.FileNotFoundException("theme missing (commit Assets/UI/StatecraftTheme.tss)", UiPaths.Theme);
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, UiPaths.Panel); }
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
        }
    }
}
```

Replace the whole of `unity/Assets/link.xml` with:

```xml
<linker>
  <assembly fullname="YamlDotNet" preserve="all" />
  <assembly fullname="Statecraft.Sim" preserve="all" />
  <assembly fullname="Statecraft.Game"><type fullname="Statecraft.Game.Map.*" preserve="all"/><type fullname="Statecraft.Game.UI.*" preserve="all"/></assembly>
</linker>
```

- [ ] **Step 5: Create the PanelSettings asset**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.UiAssets.Create -logFile unity-ui.log; echo exit=$?; ls unity/Assets/UI/StatecraftPanel.asset
```
Expected: `exit=0` and the asset path is printed.

- [ ] **Step 6: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="88" passed="88" failed="0"` (81 + 7). `TextUsesTheSerifFaces` proves the TTF reaches the resolved style; `ASubmittedButtonFiresClickedSynchronously` proves the harness.

- [ ] **Step 7: Commit**

```bash
git add unity/Assets/Editor/UiAssets.cs \
  unity/Assets/Editor/UiAssets.cs.meta \
  unity/Assets/Scripts/UI.meta \
  unity/Assets/Scripts/UI \
  unity/Assets/Tests/EditMode/UiFoundationTests.cs \
  unity/Assets/Tests/EditMode/UiFoundationTests.cs.meta \
  unity/Assets/Tests/EditMode/UiLayoutTests.cs \
  unity/Assets/Tests/EditMode/UiLayoutTests.cs.meta \
  unity/Assets/Tests/EditMode/UiTestKit.cs \
  unity/Assets/Tests/EditMode/UiTestKit.cs.meta \
  unity/Assets/UI.meta \
  unity/Assets/UI \
  unity/Assets/link.xml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): theme, EB Garamond, 1080p panel settings, strings, layout skeleton, UI test harness

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Phase 1, Track C: platform (Tasks 11–13)

### Task 11: Camera input on the Input System

*Track C.* Ruling 5: add the Input System reference to `Statecraft.Game.asmdef` (tech notes blocker 4), a `Camera` action map (pan, zoom, orbit) in the project-wide actions, and no camera input while the pointer is over UI. Amendment A4 narrows that: over the UI the wheel and the right-drag belong to the UI, while keyboard pan and orbit still move the camera. `CameraController.Step` is a static function of the rig and one frame of input, so it is tested without devices; the controller's `Update` only reads the actions. Orbit is yaw around the look-at point (`CameraRig.Yaw`; pitch stays 50°). Scroll deltas differ by browser (100–120 per notch), so zoom is normalised and clamped (tech notes §5).

**Files:**
- Create: `unity/Assets/Scripts/Map/CameraController.cs`, `unity/Assets/Tests/EditMode/CameraInputTests.cs`
- Modify: `unity/Assets/InputSystem_Actions.inputactions`, `unity/Assets/Scripts/Map/CameraRig.cs`, `unity/Assets/Scripts/Statecraft.Game.asmdef`, `unity/Assets/Tests/EditMode/Statecraft.Tests.EditMode.asmdef`
- Generated by a command in this task (commit the output): `unity/Assets/InputSystem_Actions.inputactions`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/Map/CameraController.cs.meta`, `unity/Assets/Tests/EditMode/CameraInputTests.cs.meta`

**Interfaces:**
- Consumes: `CameraRig` (`LookAt`, `Distance`, `Apply()`), the project-wide `InputSystem.actions`.
- Produces:
  - `CameraRig.Yaw : float` (0 = north; `Apply` uses `Quaternion.Euler(Pitch, Yaw, 0)`).
  - `public struct CameraInput { Vector2 Pan, DragPan; float Zoom, Orbit; bool OverUi; }`.
  - `public sealed class CameraController : MonoBehaviour { const float MinDistance = 20000f, MaxDistance = 250000f; Func<Vector2, bool>? IsOverUi; Rect Bounds; static void Step(CameraRig rig, CameraInput input, float dt, Rect bounds); }` (`[RequireComponent(typeof(CameraRig))]`).
  - Action map `Camera` with actions `Pan` (WASD, arrows), `DragPan` (right-button drag), `Zoom` (scroll y), `Orbit` (Q/E).
  - `Unity.InputSystem` referenced by `Statecraft.Game` and `Statecraft.Tests.EditMode`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/CameraInputTests.cs`:

```csharp
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInputTests
{
    static CameraRig NewRig()
    {
        var go = new GameObject("RigTest", typeof(Camera));
        var rig = go.AddComponent<CameraRig>();
        rig.LookAt = new Vector3(50000, 0, 50000); rig.Distance = 100000f;
        return rig;
    }

    static readonly Rect Map = new Rect(0, 0, 160000, 430000);

    [Test]
    public void TheCameraActionMapIsInTheProjectWideAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var map = asset.FindActionMap("Camera", true);
        foreach (var name in new[] { "Pan", "DragPan", "Zoom", "Orbit" }) Assert.IsNotNull(map.FindAction(name), name);
        Assert.IsNotNull(asset.FindActionMap("UI", true).FindAction("Click"), "UI Toolkit needs the UI map's actions");
    }

    [Test]
    public void PanMovesTheLookAtNorthAtYawZero()
    {
        var rig = NewRig();
        try
        {
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up }, 0.5f, Map);
            Assert.AreEqual(50000f, rig.LookAt.x, 1e-2);
            Assert.AreEqual(50000f + 0.4f * 100000f, rig.LookAt.z, 1e-1);   // 0.8 distances per second for half a second
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void ZoomIsClampedAndOrbitTurnsTheView()
    {
        var rig = NewRig();
        try
        {
            for (int i = 0; i < 100; i++) CameraController.Step(rig, new CameraInput { Zoom = 3 }, 0.016f, Map);
            Assert.AreEqual(CameraController.MinDistance, rig.Distance, 1e-3);
            CameraController.Step(rig, new CameraInput { Orbit = 1 }, 0.5f, Map);
            Assert.AreEqual(30f, rig.Yaw, 1e-3);
            Assert.AreEqual(30f, rig.transform.eulerAngles.y, 1e-3);
            CameraController.Step(rig, new CameraInput { Pan = Vector2.left }, 100f, Map);
            Assert.AreEqual(0f, rig.LookAt.x, 1e-3);                       // clamped to the map edge
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void PointerInputIsIgnoredOverTheUi()
    {
        var rig = NewRig();
        try
        {
            var before = (rig.LookAt, rig.Distance, rig.Yaw);
            CameraController.Step(rig, new CameraInput { Zoom = 1, DragPan = Vector2.one * 50, OverUi = true }, 1f, Map);
            Assert.AreEqual(before, (rig.LookAt, rig.Distance, rig.Yaw));
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void KeysStillPanAndOrbitWhileThePointerIsOverTheUi()
    {
        var rig = NewRig();
        try
        {
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up, Orbit = 1, Zoom = 1, OverUi = true }, 0.5f, Map);
            Assert.AreEqual(100000f, rig.Distance, 1e-3);                 // the wheel was over the UI
            Assert.AreEqual(30f, rig.Yaw, 1e-3);
            Assert.Greater(rig.LookAt.z, 50000f);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }
}
```

Replace the whole of `unity/Assets/Tests/EditMode/Statecraft.Tests.EditMode.asmdef` with:

```json
{ "name": "Statecraft.Tests.EditMode", "references": ["Statecraft.Game", "Unity.RenderPipelines.Core.Runtime", "Unity.RenderPipelines.Universal.Runtime", "Unity.InputSystem", "UnityEngine.TestRunner", "UnityEditor.TestRunner"], "includePlatforms": ["Editor"], "overrideReferences": true, "precompiledReferences": ["nunit.framework.dll", "Statecraft.Sim.dll", "YamlDotNet.dll"], "defineConstraints": ["UNITY_INCLUDE_TESTS"] }
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; the log has `error CS0246: The type or namespace name 'CameraInput' could not be found`.

- [ ] **Step 3: Add the Camera action map**

The actions file is JSON that the Input System editor also writes; add the map with a one-off script (fixed GUIDs, so the result is reproducible), then check it:

Run:
```bash
python3 - <<'EOF'
import json
p = 'unity/Assets/InputSystem_Actions.inputactions'
d = json.load(open(p))
assert not any(m['name'] == 'Camera' for m in d['maps'])
G = '5b1f0c3e-7a8d-4c51-9e2b-3a6d4f8e1c'
def act(name, typ, ctl, n): return {"name": name, "type": typ, "id": G + n, "expectedControlType": ctl, "processors": "", "interactions": "", "initialStateCheck": typ == "Value"}
def b(name, n, path, action, comp=False, part=False):
    return {"name": name, "id": G + n, "path": path, "interactions": "", "processors": "", "groups": "" if comp else ";Keyboard&Mouse",
            "action": action, "isComposite": comp, "isPartOfComposite": part}
d['maps'].append({"name": "Camera", "id": G + "01", "actions": [
    act("Pan", "Value", "Vector2", "02"), act("DragPan", "Value", "Vector2", "03"), act("Zoom", "Value", "Axis", "04"), act("Orbit", "Value", "Axis", "05")],
  "bindings": [
    b("WASD", "10", "2DVector", "Pan", comp=True), b("up", "11", "<Keyboard>/w", "Pan", part=True), b("down", "12", "<Keyboard>/s", "Pan", part=True),
    b("left", "13", "<Keyboard>/a", "Pan", part=True), b("right", "14", "<Keyboard>/d", "Pan", part=True),
    b("Arrows", "15", "2DVector", "Pan", comp=True), b("up", "16", "<Keyboard>/upArrow", "Pan", part=True), b("down", "17", "<Keyboard>/downArrow", "Pan", part=True),
    b("left", "18", "<Keyboard>/leftArrow", "Pan", part=True), b("right", "19", "<Keyboard>/rightArrow", "Pan", part=True),
    b("RightDrag", "20", "OneModifier", "DragPan", comp=True), b("modifier", "21", "<Mouse>/rightButton", "DragPan", part=True),
    b("binding", "22", "<Mouse>/delta", "DragPan", part=True),
    b("", "30", "<Mouse>/scroll/y", "Zoom"),
    b("QE", "40", "1DAxis", "Orbit", comp=True), b("negative", "41", "<Keyboard>/q", "Orbit", part=True), b("positive", "42", "<Keyboard>/e", "Orbit", part=True)]})
open(p, 'w').write(json.dumps(d, indent=4) + "\n")
EOF
python3 -c "import json;print([m['name'] for m in json.load(open('unity/Assets/InputSystem_Actions.inputactions'))['maps']])"
```
Expected: `['Player', 'UI', 'Camera']`. The `UI` map's actions (Point, Click, ScrollWheel, ...) stay as they are: UI Toolkit reads them directly (tech notes §5).

- [ ] **Step 4: Implement**

Replace the whole of `unity/Assets/Scripts/Statecraft.Game.asmdef` with:

```json
{ "name": "Statecraft.Game", "references": ["Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Core.Runtime", "Unity.InputSystem"], "overrideReferences": true, "precompiledReferences": ["Statecraft.Sim.dll", "YamlDotNet.dll"], "autoReferenced": true }
```

Replace the whole of `unity/Assets/Scripts/Map/CameraRig.cs` with:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, Gaussian far blur beyond the look-at point (tilt-shift look).
    /// Yaw turns the view around the look-at point (0 = looking north); CameraController drives LookAt, Distance and Yaw.
    /// Gaussian, not Bokeh: physical Bokeh DoF keeps everything sharp at 100 km focus distances.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float Pitch = 50f, Fov = 35f;
        public Vector3 LookAt; public float Distance = 120000f; public float Yaw;
        DepthOfField _dof;

        /// <summary>The global post-processing volume Init creates. Its runtime VolumeProfile must be owned by the caller
        /// (MapScene registers it with MapRuntimeAssets).</summary>
        public Volume Volume { get; private set; }

        public void Init()
        {
            var cam = GetComponent<Camera>(); cam.fieldOfView = Fov; cam.farClipPlane = 1_000_000f; cam.nearClipPlane = 50f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var vol = new GameObject("PostFX").AddComponent<Volume>(); vol.isGlobal = true;
            vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _dof = vol.profile.Add<DepthOfField>(true); _dof.mode.Override(DepthOfFieldMode.Gaussian);
            _dof.gaussianMaxRadius.Override(1.5f);
            Volume = vol;
            Apply();
        }

        public void Apply()
        {
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.position = LookAt - transform.forward * Distance;
            if (_dof != null) { _dof.gaussianStart.Override(Distance * 1.05f); _dof.gaussianEnd.Override(Distance * 1.8f); }
        }
    }
}
```

Create `unity/Assets/Scripts/Map/CameraController.cs`:

```csharp
#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>One frame of camera input, in the units the actions give: Pan in [-1, 1] per axis (keys), DragPan in
    /// screen pixels (right-button drag), Zoom in scroll units (positive = in), Orbit in [-1, 1] (Q/E).</summary>
    public struct CameraInput
    {
        public Vector2 Pan, DragPan; public float Zoom, Orbit;
        /// <summary>True while the pointer is over the UI: the camera then ignores pointer input (wheel zoom, drag).</summary>
        public bool OverUi;
    }

    /// <summary>Pans, zooms and orbits the diorama camera from the project-wide "Camera" action map (Input System only).
    /// While the pointer is over the UI (IsOverUi, wired by the scene) the wheel and the drag belong to the UI; keyboard pan
    /// and orbit still move the camera (plan amendment A4). Pan speed scales with
    /// distance, zoom is clamped to [MinDistance, MaxDistance], and the look-at point stays on the map.</summary>
    [RequireComponent(typeof(CameraRig))]
    public sealed class CameraController : MonoBehaviour
    {
        public const float MinDistance = 20000f, MaxDistance = 250000f;
        public const float PanPerSecond = 0.8f;        // distances per second at full key press
        public const float DragPanPerPixel = 0.0012f;  // distances per pixel of drag
        public const float ZoomPerStep = 0.1f;         // fraction of the distance per scroll notch (scroll units are normalised to notches)
        public const float OrbitDegreesPerSecond = 60f;

        /// <summary>Answers "is the pointer over the UI?" for a screen position (y up). Null = never.</summary>
        public Func<Vector2, bool>? IsOverUi;
        /// <summary>The map rectangle on the ground (x, z), in world metres; the look-at point is clamped to it.</summary>
        public Rect Bounds = new Rect(0, 0, float.MaxValue, float.MaxValue);

        CameraRig rig = null!;
        InputAction? pan, dragPan, zoom, orbit;

        void Awake() => rig = GetComponent<CameraRig>();

        void OnEnable()
        {
            var map = InputSystem.actions?.FindActionMap("Camera", throwIfNotFound: true);
            if (map == null) return;
            pan = map.FindAction("Pan", true); dragPan = map.FindAction("DragPan", true);
            zoom = map.FindAction("Zoom", true); orbit = map.FindAction("Orbit", true);
            map.Enable();
        }

        void OnDisable() => InputSystem.actions?.FindActionMap("Camera")?.Disable();

        void Update()
        {
            if (pan == null || dragPan == null || zoom == null || orbit == null) return;
            var pointer = Pointer.current?.position.ReadValue() ?? Vector2.zero;
            Step(rig, new CameraInput
            {
                Pan = pan.ReadValue<Vector2>(), DragPan = dragPan.ReadValue<Vector2>(),
                Zoom = Mathf.Clamp(zoom.ReadValue<float>() / 120f, -3f, 3f),   // browsers report 100-120 per notch; clamp bursts
                Orbit = orbit.ReadValue<float>(),
                OverUi = IsOverUi != null && IsOverUi(pointer),
            }, Time.unscaledDeltaTime, Bounds);
        }

        /// <summary>Applies one frame of input to the rig and calls rig.Apply(). Pure apart from the rig, so it is testable.</summary>
        public static void Step(CameraRig rig, CameraInput input, float dt, Rect bounds)
        {
            var zoom = input.OverUi ? 0f : input.Zoom;
            var drag = input.OverUi ? Vector2.zero : input.DragPan;
            rig.Yaw = Mathf.Repeat(rig.Yaw + input.Orbit * OrbitDegreesPerSecond * dt, 360f);
            rig.Distance = Mathf.Clamp(rig.Distance * (1f - zoom * ZoomPerStep), MinDistance, MaxDistance);
            var yaw = Quaternion.Euler(0, rig.Yaw, 0);
            var move = input.Pan * (PanPerSecond * dt) - drag * DragPanPerPixel;
            var delta = yaw * new Vector3(move.x, 0, move.y) * rig.Distance;
            var p = rig.LookAt + delta;
            rig.LookAt = new Vector3(Mathf.Clamp(p.x, bounds.xMin, bounds.xMax), p.y, Mathf.Clamp(p.z, bounds.yMin, bounds.yMax));
            rig.Apply();
        }
    }
}
```

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="86" passed="86" failed="0"` on this branch (81 at the base, + 5).

- [ ] **Step 6: Commit**

```bash
git add unity/Assets/InputSystem_Actions.inputactions \
  unity/Assets/Scripts/Map/CameraController.cs \
  unity/Assets/Scripts/Map/CameraController.cs.meta \
  unity/Assets/Scripts/Map/CameraRig.cs \
  unity/Assets/Scripts/Statecraft.Game.asmdef \
  unity/Assets/Tests/EditMode/CameraInputTests.cs \
  unity/Assets/Tests/EditMode/CameraInputTests.cs.meta \
  unity/Assets/Tests/EditMode/Statecraft.Tests.EditMode.asmdef
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(unity): Input System camera (pan, zoom, orbit) that ignores input over the UI

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 12: Content in StreamingAssets, the web loader, and the boot

*Track C.* Ruling 5 and tech notes §2: WebGL cannot read StreamingAssets with System.IO and an HTTP server gives no directory listing. A build step copies `content/` into `Assets/StreamingAssets/content/` with a `manifest.json`; the player fetches the manifest, starts every download before awaiting any (UnityWebRequest + `Awaitable`, no threads on WebGL), and loads through the existing `ContentLoader.Load(readFile, eventFiles)`. `MapScene` stops reading files itself: `GameBoot` (the first object in `Map.unity`) loads the inputs (fetch on WebGL, disk elsewhere), builds the map and raises `Ready`. Verified: the same fetch path works in edit mode over `file://` URLs.

**Files:**
- Create: `unity/Assets/Editor/ContentSync.cs`, `unity/Assets/Scripts/Boot/ContentManifest.cs`, `unity/Assets/Scripts/Boot/GameBoot.cs`, `unity/Assets/Scripts/Boot/GameInputs.cs`, `unity/Assets/Tests/EditMode/GameInputsTests.cs`
- Modify: `.gitignore`, `unity/Assets/Editor/MapCapture.cs`, `unity/Assets/Map.unity`, `unity/Assets/Scripts/Map/MapScene.cs`
- Generated by a command in this task (commit the output): `unity/Assets/Map.unity`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Editor/ContentSync.cs.meta`, `unity/Assets/Scripts/Boot.meta`, `unity/Assets/Scripts/Boot/ContentManifest.cs.meta`, `unity/Assets/Scripts/Boot/GameBoot.cs.meta`, `unity/Assets/Scripts/Boot/GameInputs.cs.meta`, `unity/Assets/Tests/EditMode/GameInputsTests.cs.meta`

**Interfaces:**
- Consumes: `ContentLoader.Load(Func<string,string?> readFile, IEnumerable<string> eventFiles)`, `HeightmapLoader.Load(string, byte[], string)`, `MapBuilder.LoadHexes(string, string)`, `SettlementPlacer.Parse/DefaultFileName`, `Paths.Content/StreamingMap`.
- Produces:
  - `Statecraft.Game.Boot.ContentManifest { const string FileName = "manifest.json", HexesJson = "map/hexes.json"; static List<string> Collect(string contentDir); static string Write(IEnumerable<string>); static List<string> Parse(string json); static List<string> Copy(string contentDir, string targetDir); }`.
  - `public sealed class MapInputs { string HeightmapJson, HexesJson, SettlementsYaml; byte[] HeightmapR16; }` and `public sealed class GameInputs { Content Content; MapInputs Map; static MapInputs MapFromDisk(string contentDir, string mapDir); static GameInputs FromDisk(string contentDir, string mapDir); static GameInputs FromTexts(IReadOnlyDictionary<string,string>, IEnumerable<string> manifest, string heightmapJson, byte[] r16); static string StreamingUrl(); static Awaitable<GameInputs> FetchAsync(string baseUrl); }` (`IOException` naming the file on a failed download).
  - `public sealed class GameBoot : MonoBehaviour { GameInputs? Inputs; string? Error; event Action<GameInputs>? Ready; void Configure(MapScene); }`; logs `BOOT OK ...` or `BOOT FAIL <message>`.
  - `MapScene.Build(MapInputs)`; `MapScene.Build()` reads the disk (editor tools); `MapScene.MarkerMaterial`; no auto-build in `Awake`.
  - Editor: `ContentSync.Sync()` and a `IPreprocessBuildWithReport` that runs it before every build; `MapCapture.MakeScene()` adds the `Boot` object.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/GameInputsTests.cs`:

```csharp
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.TestTools;

public class GameInputsTests
{
    [Test]
    public void TheManifestListsEveryContentFileThePlayerNeeds()
    {
        var files = ContentManifest.Collect(Paths.Content);
        CollectionAssert.Contains(files, "people.yaml");
        CollectionAssert.Contains(files, "events/armistice_egypt.yaml");
        CollectionAssert.Contains(files, "map/hexes.json");
        CollectionAssert.Contains(files, "map/settlements_1948.yaml");
        CollectionAssert.DoesNotContain(files, "map/precip_bio12.u16");   // tool input only
        CollectionAssert.AreEqual(files.OrderBy(f => f, System.StringComparer.Ordinal).ToList(), files);
        Assert.AreEqual(files, ContentManifest.Parse(ContentManifest.Write(files)));
    }

    [Test]
    public void ABrokenManifestIsAClearError()
    {
        var ex = Assert.Throws<InvalidDataException>(() => ContentManifest.Parse("{\"files\": []}"));
        Assert.AreEqual("manifest.json: no files", ex.Message);
    }

    static string StagedSite()
    {
        var site = Path.Combine(Path.GetTempPath(), "statecraft-site-" + System.Guid.NewGuid().ToString("N"));
        ContentManifest.Copy(Paths.Content, Path.Combine(site, "content"));
        Directory.CreateDirectory(Path.Combine(site, "map"));
        foreach (var f in new[] { "heightmap.json", "heightmap.r16" }) File.Copy(Path.Combine(Paths.StreamingMap, f), Path.Combine(site, "map", f));
        return site;
    }

    [UnityTest]
    public IEnumerator FetchingOverUrlsLoadsTheSameContentAsTheDisk()
    {
        var site = StagedSite();
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        // Wall-clock budget, not a frame count: editor frames in batch mode come at an uneven rate under load.
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
        var fetched = fetch.GetResult();
        var disk = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        Assert.AreEqual(disk.Map.HeightmapR16.Length, fetched.Map.HeightmapR16.Length);
        Assert.AreEqual(disk.Map.HexesJson, fetched.Map.HexesJson);
        Assert.AreEqual(disk.Content.Events.Count, fetched.Content.Events.Count);
        Assert.AreEqual(Simulation.NewGame(disk.Content, 7, Mode.Historical).Hash(), Simulation.NewGame(fetched.Content, 7, Mode.Historical).Hash());
        Directory.Delete(site, true);
    }

    [UnityTest]
    public IEnumerator AMissingFileNamesTheFile()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        // Wall-clock budget, not a frame count: editor frames in batch mode come at an uneven rate under load.
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
        var ex = Assert.Throws<IOException>(() => fetch.GetResult());
        StringAssert.StartsWith("people.yaml: ", ex.Message);
        Directory.Delete(site, true);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0234: The type or namespace name 'Boot' does not exist in the namespace 'Statecraft.Game'`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/Boot/ContentManifest.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The list of content files a player needs, so a WebGL build (no directory listing over HTTP) can fetch
    /// them: content/manifest.json in StreamingAssets, {"files": [...]} with paths relative to content/, '/' separated,
    /// ordinal order. The editor build step (ContentSync) copies the files and writes the manifest.</summary>
    public static class ContentManifest
    {
        public const string FileName = "manifest.json";
        /// <summary>The map files players read: hexes.json and every map/*.yaml (settlements; settlement hexes for the sim).
        /// The precipitation files are tool inputs only.</summary>
        public const string HexesJson = "map/hexes.json";

        [Serializable] sealed class ManifestJson { public string[] files = new string[0]; }

        /// <summary>Every *.yaml in <paramref name="contentDir"/>, events/ and map/, plus map/hexes.json; ordinal order.</summary>
        public static List<string> Collect(string contentDir)
        {
            IEnumerable<string> Yaml(string sub) => Directory.GetFiles(Path.Combine(contentDir, sub), "*.yaml")
                .Select(f => (sub == "" ? "" : sub + "/") + Path.GetFileName(f));
            if (!File.Exists(Path.Combine(contentDir, HexesJson))) throw new FileNotFoundException("content file missing", HexesJson);
            return Yaml("").Concat(Yaml("events")).Concat(Yaml("map")).Append(HexesJson)
                .OrderBy(f => f, StringComparer.Ordinal).ToList();
        }

        public static string Write(IEnumerable<string> files) => JsonUtility.ToJson(new ManifestJson { files = files.ToArray() }, true);

        /// <summary>Parses manifest.json; throws InvalidDataException when it is not a manifest.</summary>
        public static List<string> Parse(string json)
        {
            ManifestJson? m;
            try { m = JsonUtility.FromJson<ManifestJson>(json); }
            catch (ArgumentException e) { throw new InvalidDataException($"{FileName}: malformed JSON ({e.Message})", e); }
            if (m?.files == null || m.files.Length == 0) throw new InvalidDataException($"{FileName}: no files");
            return m.files.ToList();
        }

        /// <summary>Replaces <paramref name="targetDir"/> with a copy of the collected files plus manifest.json.</summary>
        public static List<string> Copy(string contentDir, string targetDir)
        {
            var files = Collect(contentDir);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            foreach (var f in files)
            {
                var to = Path.Combine(targetDir, f);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                File.Copy(Path.Combine(contentDir, f), to);
            }
            File.WriteAllText(Path.Combine(targetDir, FileName), Write(files));
            return files;
        }
    }
}
```

Create `unity/Assets/Scripts/Boot/GameInputs.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Networking;

namespace Statecraft.Game.Boot
{
    /// <summary>The map's raw inputs, already read (no System.IO needed afterwards).</summary>
    public sealed class MapInputs
    {
        public string HeightmapJson = "", HexesJson = "", SettlementsYaml = "";
        public byte[] HeightmapR16 = new byte[0];
    }

    /// <summary>Everything the game loads at start: the sim content and the map inputs. Desktop and the editor read
    /// files (FromDisk); WebGL cannot read StreamingAssets with System.IO, so it fetches them with UnityWebRequest from
    /// the manifest (FetchAsync, tech notes section 2).</summary>
    public sealed class GameInputs
    {
        public Content Content = null!;
        public MapInputs Map = null!;

        public static MapInputs MapFromDisk(string contentDir, string mapDir) => new MapInputs
        {
            HeightmapJson = File.ReadAllText(Path.Combine(mapDir, "heightmap.json")),
            HeightmapR16 = File.ReadAllBytes(Path.Combine(mapDir, "heightmap.r16")),
            HexesJson = File.ReadAllText(Path.Combine(contentDir, "map", "hexes.json")),
            SettlementsYaml = File.ReadAllText(Path.Combine(contentDir, "map", SettlementPlacer.DefaultFileName)),
        };

        public static GameInputs FromDisk(string contentDir, string mapDir) =>
            new GameInputs { Content = ContentLoader.Load(contentDir), Map = MapFromDisk(contentDir, mapDir) };

        /// <summary>Builds the inputs from fetched texts (keys are manifest paths) and heightmap bytes.</summary>
        public static GameInputs FromTexts(IReadOnlyDictionary<string, string> texts, IEnumerable<string> manifest, string heightmapJson, byte[] r16)
        {
            var events = manifest.Where(f => f.StartsWith("events/", StringComparison.Ordinal)).ToList();
            var content = ContentLoader.Load(rel => texts.TryGetValue(rel, out var t) ? t : null, events);
            return new GameInputs
            {
                Content = content,
                Map = new MapInputs
                {
                    HeightmapJson = heightmapJson, HeightmapR16 = r16,
                    HexesJson = texts["map/hexes.json"], SettlementsYaml = texts["map/" + SettlementPlacer.DefaultFileName],
                },
            };
        }

        /// <summary>StreamingAssets as a URL: the page-relative URL on WebGL, a file:// URI elsewhere.</summary>
        public static string StreamingUrl() =>
            Application.streamingAssetsPath.Contains("://") ? Application.streamingAssetsPath : new Uri(Application.streamingAssetsPath).AbsoluteUri;

        static UnityWebRequest Start(string url)
        {
            var r = UnityWebRequest.Get(url);
            r.SendWebRequest();
            return r;
        }

        /// <summary>Waits a frame at a time (no threads on WebGL; an Awaitable is never awaited twice).</summary>
        static async Awaitable<UnityWebRequest> Done(UnityWebRequest r, string name)
        {
            while (!r.isDone) await Awaitable.NextFrameAsync();
            if (r.result != UnityWebRequest.Result.Success)
            {
                var error = $"{name}: {r.error} ({r.url})";
                r.Dispose();
                throw new IOException(error);
            }
            return r;
        }

        static async Awaitable<string> Text(UnityWebRequest r, string name)
        {
            using var done = await Done(r, name);
            return done.downloadHandler.text;
        }

        /// <summary>Fetches the manifest, then starts every download before awaiting any (parallel), then loads.</summary>
        public static async Awaitable<GameInputs> FetchAsync(string baseUrl)
        {
            var manifest = ContentManifest.Parse(await Text(Start(baseUrl + "/content/" + ContentManifest.FileName), ContentManifest.FileName));
            var requests = manifest.Select(f => (file: f, request: Start(baseUrl + "/content/" + f))).ToList();
            var meta = Start(baseUrl + "/map/heightmap.json");
            var r16 = Start(baseUrl + "/map/heightmap.r16");
            var texts = new Dictionary<string, string>();
            foreach (var (file, request) in requests) texts[file] = await Text(request, file);
            var metaText = await Text(meta, "heightmap.json");
            byte[] bytes;
            using (var done = await Done(r16, "heightmap.r16")) bytes = done.downloadHandler.data;
            return FromTexts(texts, manifest, metaText, bytes);
        }
    }
}
```

Create `unity/Assets/Scripts/Boot/GameBoot.cs`:

```csharp
#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The first thing Map.unity runs: loads the game inputs (fetched on WebGL, read from disk elsewhere), builds
    /// the map, then raises Ready for the game (GameRoot) to start. A failure is logged ("BOOT FAIL ...") and kept in Error.</summary>
    public sealed class GameBoot : MonoBehaviour
    {
        [SerializeField] MapScene map = null!;

        public GameInputs? Inputs { get; private set; }
        public string? Error { get; private set; }
        public event Action<GameInputs>? Ready;

        public void Configure(MapScene mapScene) => map = mapScene;

        async void Start()
        {
            try
            {
                Inputs = Application.platform == RuntimePlatform.WebGLPlayer
                    ? await GameInputs.FetchAsync(GameInputs.StreamingUrl())
                    : GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
                map.Build(Inputs.Map);
                Debug.Log($"BOOT OK {Inputs.Content.Events.Count} events");
                Ready?.Invoke(Inputs);
            }
            catch (Exception e)
            {
                Error = e.Message;
                Debug.LogError("BOOT FAIL " + e.Message);
                Debug.LogException(e);
            }
        }
    }
}
```

Replace the whole of `unity/Assets/Scripts/Map/MapScene.cs` with:

```csharp
using System;
using System.IO;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Builds the whole map: terrain, hexes, 1948 settlements, camera. Materials are editor-made assets
    /// (M0 correction 1). Build is idempotent: it destroys what the previous Build made first. In play mode the Boot
    /// component calls Build(inputs) once the inputs are loaded (fetched on WebGL); editor tools call Build().</summary>
    public sealed class MapScene : MonoBehaviour
    {
        [SerializeField] Material terrainMaterial, overlayMaterial, markerMaterial;

        public GeoFrame Frame { get; private set; }
        public Terrain Terrain { get; private set; }
        public Material MarkerMaterial => markerMaterial;

        public void Configure(Material terrain, Material overlay, Material marker)
        { terrainMaterial = terrain; overlayMaterial = overlay; markerMaterial = marker; }

        /// <summary>Builds from the repository files with System.IO (editor and desktop only).</summary>
        public void Build() => Build(GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap));

        public void Build(MapInputs inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (terrainMaterial == null) throw new InvalidOperationException("MapScene: terrainMaterial is not assigned");
            if (overlayMaterial == null) throw new InvalidOperationException("MapScene: overlayMaterial is not assigned");
            if (markerMaterial == null) throw new InvalidOperationException("MapScene: markerMaterial is not assigned");
            var cam = Camera.main;
            if (cam == null) throw new InvalidOperationException("MapScene needs a camera tagged MainCamera");

            Clear();
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16, MetaPath);
            var terrainGo = MapBuilder.Build(meta, heights, MapBuilder.LoadHexes(inputs.HexesJson, HexesPath), terrainMaterial, overlayMaterial);
            terrainGo.transform.SetParent(transform, true);
            Terrain = terrainGo.GetComponent<Terrain>();
            var size = Terrain.terrainData.size;
            Frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
            var list = SettlementPlacer.Parse(inputs.SettlementsYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), SettlementsPath);
            SettlementPlacer.Place(list, Frame, Terrain, markerMaterial).transform.SetParent(transform, true);

            if (!cam.TryGetComponent(out CameraRig rig)) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.LookAt = Frame.ToWorld(34.95, 31.95, 0); rig.Init(); // central Israel: Tel Aviv–Jerusalem corridor
            rig.Volume.transform.SetParent(transform, true);
            // VolumeProfile's own teardown only releases its components, it does not destroy them (Core RP 17.3), so own both.
            var assets = terrainGo.GetComponent<MapRuntimeAssets>();
            foreach (var c in rig.Volume.profile.components) assets.Own(c);
            assets.Own(rig.Volume.profile);
        }

        static string MetaPath => "heightmap.json";
        static string HexesPath => "hexes.json";
        static string SettlementsPath => SettlementPlacer.DefaultFileName;

        /// <summary>Destroys every child: all of them are things Build made (terrain, Settlements, PostFX). Walking the
        /// hierarchy rather than remembered fields keeps Build idempotent after a domain reload (R2). The VolumeProfile is
        /// owned by the terrain's MapRuntimeAssets, so destroying the terrain destroys it.</summary>
        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var go = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { go.transform.SetParent(null); Destroy(go); } else DestroyImmediate(go);
            }
            Terrain = null;
        }
    }
}
```

Create `unity/Assets/Editor/ContentSync.cs`:

```csharp
using System.IO;
using Statecraft.Game.Boot;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Copies the repository's content/ into Assets/StreamingAssets/content with a manifest.json before every
    /// player build (git-ignored output), so players read the same content the editor reads from the repo.
    /// Run alone: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ContentSync.Sync</summary>
    public sealed class ContentSync : IPreprocessBuildWithReport
    {
        public const string Target = "Assets/StreamingAssets/content";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Sync();

        public static void Sync()
        {
            var files = ContentManifest.Copy(Path.Combine(Paths.RepoRoot, "content"), Target);
            UnityEngine.Debug.Log($"ContentSync: {files.Count} files to {Target}");
        }
    }
}
```

In `unity/Assets/Editor/MapCapture.cs`:

1. Replace

```csharp

    /// <summary>Creates and saves Assets/Map.unity: default camera and light plus a MapScene wired to the material assets.</summary>
    public static void MakeScene()
    {
        MapMaterials.Create();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("MapScene").AddComponent<MapScene>().Configure(
            Load(MapMaterials.TerrainPath), Load(MapMaterials.OverlayPath), Load(MapMaterials.MarkerPath));
        EditorSceneManager.SaveScene(scene, ScenePath);
```

   with

```csharp

    /// <summary>Creates and saves Assets/Map.unity: default camera and light, a MapScene wired to the material assets, and
    /// the GameBoot that loads the inputs and builds the map in play mode and in players.</summary>
    public static void MakeScene()
    {
        MapMaterials.Create();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(MapMaterials.TerrainPath), Load(MapMaterials.OverlayPath), Load(MapMaterials.MarkerPath));
        new GameObject("Boot").AddComponent<Statecraft.Game.Boot.GameBoot>().Configure(map);
        EditorSceneManager.SaveScene(scene, ScenePath);
```

In `.gitignore`:

1. Replace

```text
captures/
```

   with

```text
captures/
unity/Assets/StreamingAssets/content/
unity/Assets/StreamingAssets/content.meta
```

- [ ] **Step 4: Regenerate the boot scene and check the copy**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -executeMethod MapCapture.MakeScene -logFile unity-scene.log; echo exit=$?
grep -c "Statecraft.Game.Boot.GameBoot" unity/Assets/Map.unity
"$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ContentSync.Sync -logFile unity-sync.log; grep "ContentSync:" unity-sync.log; git status --short unity/Assets/StreamingAssets
```
Expected: `exit=0`, `1`, `ContentSync: 16 files to Assets/StreamingAssets/content` on this branch (28 once Track A merges: 9 top-level files, 16 events, 3 map files), and **no** `git status` line for StreamingAssets (the copy is git-ignored).

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="90" passed="90" failed="0"` on this branch (+4). The map tests still pass: `MapScene.Build()` reads the same files as before.

- [ ] **Step 6: Commit**

```bash
git add .gitignore \
  unity/Assets/Editor/ContentSync.cs \
  unity/Assets/Editor/ContentSync.cs.meta \
  unity/Assets/Editor/MapCapture.cs \
  unity/Assets/Map.unity \
  unity/Assets/Scripts/Boot.meta \
  unity/Assets/Scripts/Boot \
  unity/Assets/Scripts/Map/MapScene.cs \
  unity/Assets/Tests/EditMode/GameInputsTests.cs \
  unity/Assets/Tests/EditMode/GameInputsTests.cs.meta
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(unity): content manifest in StreamingAssets, UnityWebRequest loader, GameBoot

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 13: WebGL template, headless build script, template-package prune

*Track C.* Ruling 5: a custom WebGL template (full-window canvas, `autoSyncPersistentDataPath`, tech notes §1.3 and §3), Map.unity as the only and first scene, Brotli for itch.io and Disabled for local checks (one script, `-compression` parameter, tech notes §6), IL2CPP for both players, and the template-package prune. The prune removes the packages the Universal 3D template added that nothing uses (AI Navigation, Collab Proxy, Multiplayer Center, Visual Scripting, Timeline, uGUI) and the sample scene. **Keep `Assets/Settings/SampleSceneProfile.asset`:** both URP pipeline assets use it as their volume profile, so deleting it would change the look. A build rewrites some URP and player-setting fields as build-time caches; the build steps discard them.

**Files:**
- Create: `unity/Assets/Editor/Build.cs`, `unity/Assets/Tests/EditMode/ProjectSettingsTests.cs`, `unity/Assets/WebGLTemplates/Statecraft/index.html`
- Modify: `.gitignore`, `unity/Assets/Editor/ProjectSetup.cs`, `unity/Packages/manifest.json`, `unity/Packages/packages-lock.json`, `unity/ProjectSettings/ProjectSettings.asset`
- Delete: `unity/Assets/Scenes/SampleScene.unity`
- Generated by a command in this task (commit the output): `unity/Packages/packages-lock.json`, `unity/ProjectSettings/ProjectSettings.asset`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Editor/Build.cs.meta`, `unity/Assets/Tests/EditMode/ProjectSettingsTests.cs.meta`, `unity/Assets/WebGLTemplates.meta`, `unity/Assets/WebGLTemplates/Statecraft.meta`, `unity/Assets/WebGLTemplates/Statecraft/index.html.meta`

**Interfaces:**
- Consumes: `MapCapture.MakeScene` output `Assets/Map.unity` (Task 12), `ContentSync` (runs before every build).
- Produces:
  - `ProjectSetup.Apply()` (idempotent): IL2CPP for Standalone and WebGL, `PlayerSettings.WebGL.template = "PROJECT:Statecraft"`, compression Disabled, no decompression fallback, build scenes = `["Assets/Map.unity"]`; constants `BootScene`, `WebTemplate`.
  - `Statecraft.Game.EditorTools.Build.Web()` (args `-compression disabled|gzip|brotli`, `-out <dir>`, default `Builds/Web-<compression>`) and `Build.Mac()` (args `-out`, `-ffp-contract-off`); both exit 1 on failure and log `BUILD <result> <target> -> <path>: <MB> MB, ...`.
  - `unity/Assets/WebGLTemplates/Statecraft/index.html`; build output in `unity/Builds/` (git-ignored).

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/ProjectSettingsTests.cs`:

```csharp
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

/// <summary>The M3 player settings (ProjectSetup.Apply) and the custom WebGL template.</summary>
public class ProjectSettingsTests
{
    [Test]
    public void PlayersUseIl2CppTheStatecraftTemplateAndMapUnityAsTheBootScene()
    {
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.WebGL));
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
        Assert.AreEqual("PROJECT:Statecraft", PlayerSettings.WebGL.template);
        Assert.IsFalse(PlayerSettings.WebGL.decompressionFallback);
        Assert.AreEqual(new[] { "Assets/Map.unity" }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
    }

    [Test]
    public void TheTemplateFillsTheWindowAndPersistsSaves()
    {
        var html = File.ReadAllText("Assets/WebGLTemplates/Statecraft/index.html");
        StringAssert.Contains("#unity-canvas { position: fixed; left: 0; top: 0; width: 100%; height: 100%;", html);
        StringAssert.Contains("autoSyncPersistentDataPath: true", html);
        StringAssert.DoesNotContain("alert(", html);
    }

    [Test]
    public void TemplatePackagesAreGone()
    {
        var manifest = File.ReadAllText("Packages/manifest.json");
        foreach (var p in new[] { "com.unity.ai.navigation", "com.unity.collab-proxy", "com.unity.multiplayer.center",
                                  "com.unity.visualscripting", "com.unity.timeline", "com.unity.ugui" })
            StringAssert.DoesNotContain("\"" + p + "\"", manifest);
        Assert.IsFalse(File.Exists("Assets/Scenes/SampleScene.unity"));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=2`; `ProjectSettingsTests` fails three times (template `APPLICATION:Default`, no template file, packages present).

- [ ] **Step 3: Write the template, settings and build script; prune**

Create `unity/Assets/WebGLTemplates/Statecraft/index.html`:

```html
<!DOCTYPE html>
<html lang="en-us">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, user-scalable=no">
    <title>{{{ PRODUCT_NAME }}}</title>
    <style>
      html, body { margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; background: #0e1626; }
      #unity-canvas { position: fixed; left: 0; top: 0; width: 100%; height: 100%; background: #0e1626; display: block; }
      #loading { position: fixed; left: 50%; top: 50%; transform: translate(-50%, -50%); color: #ece4d2;
                 font: 20px Georgia, "Times New Roman", serif; text-align: center; }
      #bar { width: 320px; height: 4px; margin-top: 12px; border: 1px solid #c4a05c; }
      #fill { width: 0; height: 100%; background: #c4a05c; }
      #error { display: none; position: fixed; left: 10%; right: 10%; top: 40%; color: #de7868; background: #0e1626;
               border: 1px solid #c4a05c; padding: 16px; font: 16px Georgia, serif; white-space: pre-wrap; }
    </style>
  </head>
  <body>
    <canvas id="unity-canvas" tabindex="-1"></canvas>
    <div id="loading">Statecraft: Israel<div id="bar"><div id="fill"></div></div></div>
    <div id="error"></div>
    <script src="Build/{{{ LOADER_FILENAME }}}"></script>
    <script>
      // Full-window canvas: Unity keeps the render size matched to the canvas's CSS size, so the UI Toolkit panel
      // (ScaleWithScreenSize, 1920x1080, match 0.5) rescales on every browser resize.
      var config = {
        arguments: [],
        dataUrl: "Build/{{{ DATA_FILENAME }}}",
        frameworkUrl: "Build/{{{ FRAMEWORK_FILENAME }}}",
#if USE_THREADS
        workerUrl: "Build/{{{ WORKER_FILENAME }}}",
#endif
#if USE_WASM
        codeUrl: "Build/{{{ CODE_FILENAME }}}",
#endif
#if SYMBOLS_FILENAME
        symbolsUrl: "Build/{{{ SYMBOLS_FILENAME }}}",
#endif
        streamingAssetsUrl: "StreamingAssets",
        companyName: {{{ JSON.stringify(COMPANY_NAME) }}},
        productName: {{{ JSON.stringify(PRODUCT_NAME) }}},
        productVersion: {{{ JSON.stringify(PRODUCT_VERSION) }}},
        // Persist writes under /idbfs to IndexedDB when a file is closed (tech notes section 3): saves need no manual sync.
        autoSyncPersistentDataPath: true,
      };
      createUnityInstance(document.querySelector("#unity-canvas"), config, function (progress) {
        document.getElementById("fill").style.width = (100 * progress) + "%";
      }).then(function () {
        document.getElementById("loading").style.display = "none";
      }).catch(function (message) {
        var e = document.getElementById("error");
        e.textContent = "Statecraft could not start:\n" + message;
        e.style.display = "block";
        console.error("BOOT FAIL " + message);
      });
    </script>
  </body>
</html>
```

Replace the whole of `unity/Assets/Editor/ProjectSetup.cs` with:

```csharp
using UnityEditor;
using UnityEditor.Build;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Project settings (M0 API corrections and M3 web settings): IL2CPP for Standalone and WebGL, the
    /// Statecraft WebGL template (full-window canvas, autoSyncPersistentDataPath), compression off by default (the build
    /// script sets it per target), Map.unity as the only and first scene.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ProjectSetup.Apply</summary>
    public static class ProjectSetup
    {
        public const string BootScene = "Assets/Map.unity";
        public const string WebTemplate = "PROJECT:Statecraft";

        public static void Apply()
        {
            PlayerSettings.companyName = "Statecraft";
            PlayerSettings.productName = "Statecraft Israel";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.template = WebTemplate;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
```

Create `unity/Assets/Editor/Build.cs`:

```csharp
#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Headless player builds. Map.unity is the boot scene; ContentSync copies content/ into StreamingAssets
    /// first (IPreprocessBuildWithReport). Exits non-zero on failure.
    ///   Web: Unity -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web
    ///        [-compression disabled|gzip|brotli] [-out Builds/Web-disabled]
    ///   Mac: Unity -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac
    ///        [-out Builds/Mac/Statecraft.app] [-ffp-contract-off]</summary>
    public static class Build
    {
        static string? Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static bool Flag(string name) => Environment.GetCommandLineArgs().Contains(name);

        public static WebGLCompressionFormat Compression(string? name) => (name ?? "disabled") switch
        {
            "disabled" => WebGLCompressionFormat.Disabled,
            "gzip" => WebGLCompressionFormat.Gzip,
            "brotli" => WebGLCompressionFormat.Brotli,
            _ => throw new ArgumentException($"-compression must be disabled, gzip or brotli (got '{name}')"),
        };

        public static void Web()
        {
            var name = Arg("-compression") ?? "disabled";
            ProjectSetup.Apply();
            PlayerSettings.WebGL.compressionFormat = Compression(name);
            PlayerSettings.WebGL.decompressionFallback = false;   // itch.io serves .br/.gz with Content-Encoding
            try { Run(BuildTarget.WebGL, Arg("-out") ?? "Builds/Web-" + name); }
            finally { ProjectSetup.Apply(); }                      // back to the committed settings (compression Disabled)
        }

        public static void Mac()
        {
            ProjectSetup.Apply();
            // Apple clang fuses a*b+c into fmadd by default (tech notes 4.2); turn it off if the Mac golden check fails.
            PlayerSettings.SetAdditionalIl2CppArgs(Flag("-ffp-contract-off") ? "--compiler-flags=-ffp-contract=off" : "");
            try { Run(BuildTarget.StandaloneOSX, Arg("-out") ?? "Builds/Mac/Statecraft.app"); }
            finally { PlayerSettings.SetAdditionalIl2CppArgs(""); AssetDatabase.SaveAssets(); }
        }

        static void Run(BuildTarget target, string location)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScene }, locationPathName = location, target = target, options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"BUILD {s.result} {target} -> {location}: {s.totalSize / 1048576.0:0.0} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            if (s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
```

Replace the whole of `unity/Packages/manifest.json` with:

```json
{
  "dependencies": {
    "com.unity.ide.rider": "3.0.40",
    "com.unity.ide.visualstudio": "2.0.26",
    "com.unity.inputsystem": "1.20.0",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.unity.test-framework": "1.6.0",
    "com.unity.modules.accessibility": "1.0.0",
    "com.unity.modules.adaptiveperformance": "1.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vectorgraphics": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

Run:
```bash
git rm -q unity/Assets/Scenes/SampleScene.unity unity/Assets/Scenes/SampleScene.unity.meta unity/Assets/Scenes.meta
printf 'unity/Builds/\n' >> .gitignore
"$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ProjectSetup.Apply -logFile unity-setup.log; echo exit=$?
grep -E "webGLTemplate|webGLCompressionFormat" unity/ProjectSettings/ProjectSettings.asset
git add unity/ProjectSettings/ProjectSettings.asset unity/Packages/manifest.json unity/Packages/packages-lock.json
```
Expected: `exit=0`, `webGLTemplate: PROJECT:Statecraft`, `webGLCompressionFormat: 2`. Unity also rewrites `unity/Packages/packages-lock.json` for the pruned packages. `git checkout --` restores from the index, so stage real settings changes before any build: the build step below discards build-time rewrites with `git checkout --`, which would otherwise also throw away these settings.

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="93" passed="93" failed="0"` on this branch (+3).

- [ ] **Step 5: Build once and check the output**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log; echo exit=$?
grep "BUILD " unity-build-web.log; ls unity/Builds/Web-disabled unity/Builds/Web-disabled/StreamingAssets/content | head
grep -c "autoSyncPersistentDataPath: true" unity/Builds/Web-disabled/index.html
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
```
Expected: `exit=0`; `BUILD Succeeded WebGL -> Builds/Web-disabled: ...` (about 70 MB uncompressed, about 2 minutes); `Build`, `index.html`, `StreamingAssets`, then the content files and `manifest.json`; `1`. The last line drops the build-time rewrites of the URP assets and player settings (prefiltering fields, `m_BuildTargetBatching`), which are caches, not settings.

- [ ] **Step 6: Commit**

```bash
git add .gitignore \
  unity/Assets/Editor/Build.cs \
  unity/Assets/Editor/Build.cs.meta \
  unity/Assets/Editor/ProjectSetup.cs \
  unity/Assets/Tests/EditMode/ProjectSettingsTests.cs \
  unity/Assets/Tests/EditMode/ProjectSettingsTests.cs.meta \
  unity/Assets/WebGLTemplates.meta \
  unity/Assets/WebGLTemplates/Statecraft.meta \
  unity/Assets/WebGLTemplates/Statecraft/index.html \
  unity/Assets/WebGLTemplates/Statecraft/index.html.meta \
  unity/Packages/manifest.json \
  unity/Packages/packages-lock.json \
  unity/ProjectSettings/ProjectSettings.asset
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(unity): Statecraft WebGL template, headless Web/Mac build script, Map.unity boot scene, package prune

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Merge point 1

The controller merges Tracks A, B and C into `m3` (no conflicts expected: the file sets are disjoint) and runs, from the repository root:

Run:
```bash
dotnet test sim/Statecraft.sln
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `Passed:   391`, `96 passed`, `exit=0` and `total="101" passed="101" failed="0"`.

Phase 2 branches start here: Track B (Tasks 14–21) and Track C (Task 22).

## Phase 2, Track B: screens (Tasks 14–21)

Every task in this track adds one presenter: a plain class that takes the UI root, the `SimBridge`, `UiText` and (when it can refuse) the `ToastPresenter`, subscribes to `bridge.Changed`, and sets element text directly. Orders are queued on the bridge and take effect at End Turn. Tests build real panels with `UiTestKit` and real content.

### Task 14: SimBridge, the HUD, refusals and screen navigation

*Track B.* Spec §6 names `SimBridge`: the UI's only door into the sim. It wraps Task 9's `GameSession`, queues this turn's orders (one per key: one answer per event, one call-up level, one budget, one appointee per office, one level per policy, one build per hex), prices them in Attention up front (refusing what cannot be afforded), and runs End Turn. The HUD is the top bar of ruling 2: date and turn, six indicators with value and change since last turn, the security level, Attention, End Turn. `HudModel` computes the values from state alone, so the arithmetic is tested without UI. Treasury shows the net position (cash minus debt): with deficits borrowed, cash alone is always 0. Refused orders show their reason (spec §7).

**Files:**
- Create: `unity/Assets/Scripts/UI/HudModel.cs`, `unity/Assets/Scripts/UI/HudPresenter.cs`, `unity/Assets/Scripts/UI/ScreenNav.cs`, `unity/Assets/Scripts/UI/SimBridge.cs`, `unity/Assets/Scripts/UI/ToastPresenter.cs`, `unity/Assets/Tests/EditMode/HudTests.cs`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/UI/HudModel.cs.meta`, `unity/Assets/Scripts/UI/HudPresenter.cs.meta`, `unity/Assets/Scripts/UI/ScreenNav.cs.meta`, `unity/Assets/Scripts/UI/SimBridge.cs.meta`, `unity/Assets/Scripts/UI/ToastPresenter.cs.meta`, `unity/Assets/Tests/EditMode/HudTests.cs.meta`

**Interfaces:**
- Consumes: `GameSession` (Task 9: `New`, `Step`, `State`, `Previous`, `Last`), `AttentionSystem.Cost/IsCommandKind`, `EffectsSystem.Approval`, `UiText`, `UiTestKit`.
- Produces:
  - `public sealed class SimBridge { SimBridge(GameSession); GameSession Session; Content Content; GameState State; IReadOnlyList<Command> Queued; StepResult? BeforeLast; event Action? Changed; event Action<IReadOnlyList<Rejection>>? TurnEnded; double AttentionLeft; Command? QueuedLike(Command); string? Queue(Command); void Withdraw(Command); StepResult EndTurn(); void Replace(GameSession); }`.
  - `public readonly struct Meter { string Value, Delta; int Sign; }`, `public sealed class HudModel { string Year, Turn, Date, Attention, SecurityLevel; Meter Treasury, Immigration, Security, Support, Standing, Fx; static HudModel From(SimBridge, UiText); }`.
  - `HudPresenter(VisualElement root, SimBridge, UiText)`; `ToastPresenter(VisualElement root, SimBridge, UiText) { void Show(IEnumerable<string>); void Show(string); void Refuse(string reason); void Clear(); }` (`Refuse` prefixes `Not done: `); `ScreenNav(VisualElement root) { static string[] Screens; string? Current; void Toggle(string); void Open(string?); }`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/HudTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class HudTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge NewBridge() => new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical));

    [Test]
    public void TheTopBarShowsTheOpeningOfTheGame()
    {
        var m = HudModel.From(NewBridge(), Text);
        Assert.AreEqual("1948", m.Year);
        Assert.AreEqual("Turn 0", m.Turn);
        Assert.AreEqual("15 May 1948", m.Date);
        Assert.AreEqual("20.0m", m.Treasury.Value);
        Assert.AreEqual("$40.0m", m.Fx.Value);
        Assert.AreEqual("Level 5 of 5", m.SecurityLevel);
        Assert.AreEqual("Attention 4.0 of 4.0", m.Attention);
        Assert.AreEqual("", m.Treasury.Delta);                       // no previous turn yet
    }

    [Test]
    public void EndTurnAdvancesTheGameAndShowsChanges()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        new HudPresenter(ui.Root, bridge, Text);
        ui.Press("end-turn");
        Assert.AreEqual(1, bridge.State.Turn);
        Assert.AreEqual("Turn 1", ui.Q<Label>("turn").text);
        Assert.AreEqual("22 May 1948", ui.Q<Label>("date").text);
        var arrivals = bridge.Session.Last!.Why.Where(w => w.System == "population").Sum(w => w.Delta);
        Assert.AreEqual(arrivals.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), ui.Q<Label>("immigration-value").text);
        Assert.AreEqual("-1.4", ui.Q<Label>("treasury-delta").text);   // a week of war: taxes, spending and the call-up
        Assert.IsTrue(ui.Q<Label>("treasury-delta").ClassListContains("delta-down"));
    }

    [Test]
    public void QueuedOrdersSpendAttentionUpFront()
    {
        var bridge = NewBridge();
        Assert.IsNull(bridge.Queue(new SetMobilization(1.0)));      // at war each order costs 2
        Assert.AreEqual(2.0, bridge.AttentionLeft, 1e-9);
        Assert.IsNull(bridge.Queue(new SetMobilization(0.9)));      // replaces the first: still one order
        Assert.AreEqual(1, bridge.Queued.Count);
        Assert.IsNull(bridge.Queue(new SecurityOrder(-1)));
        Assert.AreEqual("Not enough attention (need 2.0, have 0.0)", bridge.Queue(new SetPolicy("rationing", 1)));
    }

    [Test]
    public void RefusedOrdersAreShownWithTheirReason()
    {
        using var ui = new UiTestKit();
        var bridge = NewBridge();
        var text = Text;
        new HudPresenter(ui.Root, bridge, text);
        new ToastPresenter(ui.Root, bridge, text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
        bridge.Queue(new SetPolicy("curfew", 1));
        ui.Press("end-turn");
        Assert.AreEqual("Not done: Unknown policy 'curfew'", ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<Label>("toast").style.display.value);
    }

    [Test]
    public void NavigationOpensOneScreenAtATime()
    {
        using var ui = new UiTestKit();
        var nav = new ScreenNav(ui.Root);
        Assert.IsNull(nav.Current);
        ui.Press("nav-budget");
        Assert.AreEqual("budget", nav.Current);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("screen-budget").style.display.value);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-cabinet").style.display.value);
        ui.Press("nav-cabinet");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("screen-budget").style.display.value);
        ui.Press("nav-cabinet");
        Assert.IsNull(nav.Current);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'SimBridge' could not be found`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/UI/HudModel.cs`:

```csharp
#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>One top-bar indicator: its value and the change since the last turn (empty when there is none).</summary>
    public readonly struct Meter
    {
        public readonly string Value, Delta;
        /// <summary>+1 up, -1 down, 0 unchanged or unknown.</summary>
        public readonly int Sign;
        public Meter(string value, string delta, int sign) { Value = value; Delta = delta; Sign = sign; }
    }

    /// <summary>What the top bar shows, computed from simulation state only (no UI types), so it is testable on its own.
    /// Indicators (0..1) show as 0-100; Treasury is the net position (cash minus debt, IL millions); Immigration is the
    /// number of immigrants who arrived during the last turn.</summary>
    public sealed class HudModel
    {
        public string Year = "", Turn = "", Date = "", Attention = "", SecurityLevel = "";
        public Meter Treasury, Immigration, Security, Support, Standing, Fx;

        static double Arrivals(StepResult? r) => r == null ? 0 : r.Why.Where(w => w.System == "population").Sum(w => w.Delta);

        public static HudModel From(SimBridge b, UiText t)
        {
            var c = b.Content; var s = b.State; var prev = b.Session.Previous;
            Meter M(double now, double? before, string valueKey, string deltaKey)
            {
                if (before == null) return new Meter(t.Format(valueKey, now), "", 0);
                var d = Math.Round(now - before.Value, deltaKey == "hud.delta_money" ? 1 : 0);
                return d == 0 ? new Meter(t.Format(valueKey, now), "", 0) : new Meter(t.Format(valueKey, now), t.Format(deltaKey, d), Math.Sign(d));
            }
            double Pct(GameState x, string id) => Math.Round(x.Indicators[id] * 100);
            return new HudModel
            {
                Year = s.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Turn = t.Format("hud.turn", s.Turn),
                Date = t.Format("hud.date", s.Date),
                Attention = t.Format("hud.attention", b.AttentionLeft, c.Rules.AttentionPerTurn),
                SecurityLevel = t.Format("hud.level", s.Rung),
                Treasury = M(s.Treasury - s.Debt, prev == null ? (double?)null : prev.Treasury - prev.Debt, "hud.money", "hud.delta_money"),
                Immigration = M(Arrivals(b.Session.Last), b.BeforeLast == null ? (double?)null : Arrivals(b.BeforeLast), "hud.people", "hud.delta"),
                Security = M(Pct(s, "security"), prev == null ? (double?)null : Pct(prev, "security"), "hud.percent", "hud.delta"),
                Support = M(Math.Round(EffectsSystem.Approval(c, s) * 100), prev == null ? (double?)null : Math.Round(EffectsSystem.Approval(c, prev) * 100), "hud.percent", "hud.delta"),
                Standing = M(Pct(s, "intl_standing"), prev == null ? (double?)null : Pct(prev, "intl_standing"), "hud.percent", "hud.delta"),
                Fx = M(s.FxReserves, prev?.FxReserves, "hud.fx", "hud.delta_money"),
            };
        }
    }
}
```

Create `unity/Assets/Scripts/UI/HudPresenter.cs`:

```csharp
#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Fills the top bar from HudModel and runs End Turn.</summary>
    public sealed class HudPresenter
    {
        readonly VisualElement root; readonly SimBridge bridge; readonly UiText text;

        public HudPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            this.root = root; this.bridge = bridge; this.text = text;
            root.Q<Button>("end-turn").clicked += () => bridge.EndTurn();
            bridge.Changed += Render;
            Render();
        }

        void Set(string name, string value) => root.Q<Label>(name).text = value;

        void Set(string meter, Meter m)
        {
            Set(meter + "-value", m.Value);
            var delta = root.Q<Label>(meter + "-delta");
            delta.text = m.Delta;
            delta.EnableInClassList("delta-up", m.Sign > 0);
            delta.EnableInClassList("delta-down", m.Sign < 0);
        }

        public void Render()
        {
            var m = HudModel.From(bridge, text);
            Set("year", m.Year); Set("turn", m.Turn); Set("date", m.Date); Set("attention", m.Attention);
            Set("security-level", m.SecurityLevel);
            Set("treasury", m.Treasury); Set("immigration", m.Immigration); Set("security", m.Security);
            Set("support", m.Support); Set("standing", m.Standing); Set("fx", m.Fx);
            root.Q<Button>("end-turn").SetEnabled(bridge.State.GameOver == null);
        }
    }
}
```

Create `unity/Assets/Scripts/UI/ScreenNav.cs`:

```csharp
#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The top-bar buttons open the Cabinet, Budget, Defence and Why screens, one at a time; pressing the open
    /// screen's button again closes it.</summary>
    public sealed class ScreenNav
    {
        public static readonly string[] Screens = { "cabinet", "budget", "defence", "why" };
        readonly VisualElement root;

        public ScreenNav(VisualElement root)
        {
            this.root = root;
            foreach (var s in Screens)
            {
                var screen = s;
                root.Q<Button>("nav-" + screen).clicked += () => Toggle(screen);
            }
            Open(null);
        }

        /// <summary>The open screen's name, or null.</summary>
        public string? Current { get; private set; }

        public void Toggle(string screen) => Open(Current == screen ? null : screen);

        public void Open(string? screen)
        {
            Current = screen;
            foreach (var s in Screens)
                root.Q<VisualElement>("screen-" + s).style.display = s == screen ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
```

Create `unity/Assets/Scripts/UI/SimBridge.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The UI's only door into the simulation (spec section 6). Holds the GameSession, collects this turn's
    /// orders, runs End Turn and tells the screens to refresh. Orders take effect at End Turn, in the order given; each
    /// order replaces an earlier one with the same key (one choice per event, one call-up level, one budget, one appointee
    /// per office, one policy level per policy, one build per hex).</summary>
    public sealed class SimBridge
    {
        readonly List<Command> queue = new List<Command>();

        public GameSession Session { get; private set; }
        public Content Content => Session.Content;
        public GameState State => Session.State;
        public IReadOnlyList<Command> Queued => queue;
        /// <summary>The step before Session.Last (null until two turns have run), for "change since last turn".</summary>
        public StepResult? BeforeLast { get; private set; }
        /// <summary>Raised after every change the screens show: an order queued or withdrawn, a turn, a load.</summary>
        public event Action? Changed;
        /// <summary>Raised after End Turn with the orders the simulation refused (possibly none).</summary>
        public event Action<IReadOnlyList<Rejection>>? TurnEnded;

        public SimBridge(GameSession session) { Session = session ?? throw new ArgumentNullException(nameof(session)); }

        static string Key(Command c) => c switch
        {
            ChooseEventOption e => "event:" + e.EventId,
            SetPolicy p => "policy:" + p.Policy,
            Appoint a => "appoint:" + a.Office,
            MapAction m => FormattableString.Invariant($"map:{m.Q},{m.R}"),
            _ => c.Kind,
        };

        double Price(Command c) => AttentionSystem.IsCommandKind(c.Kind) ? AttentionSystem.Cost(Content, State, c.Kind) : 0;

        /// <summary>Attention left after paying for the queued orders at this turn's prices.</summary>
        public double AttentionLeft => State.Attention - queue.Sum(Price);

        /// <summary>The queued order with this key, or null.</summary>
        public Command? QueuedLike(Command c) => queue.FirstOrDefault(q => Key(q) == Key(c));

        /// <summary>Queues an order (replacing one with the same key). Returns null, or why it was refused here:
        /// the game is over, or the order costs more attention than is left.</summary>
        public string? Queue(Command c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (State.GameOver != null) return "The game is over";
            var old = QueuedLike(c);
            var left = AttentionLeft + (old != null ? Price(old) : 0);
            if (Price(c) > left + 1e-9)
                return FormattableString.Invariant($"Not enough attention (need {Price(c):0.0}, have {left:0.0})");
            if (old != null) queue[queue.IndexOf(old)] = c; else queue.Add(c);
            Changed?.Invoke();
            return null;
        }

        public void Withdraw(Command c)
        {
            if (queue.Remove(c)) Changed?.Invoke();
        }

        /// <summary>Runs the turn with the queued orders, clears the queue, and notifies.</summary>
        public StepResult EndTurn()
        {
            BeforeLast = Session.Last;
            var r = Session.Step(queue.ToList());
            queue.Clear();
            Changed?.Invoke();
            TurnEnded?.Invoke(r.Rejected);
            return r;
        }

        /// <summary>Switches to another session (a loaded save), dropping queued orders.</summary>
        public void Replace(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            BeforeLast = null;
            queue.Clear();
            Changed?.Invoke();
        }
    }
}
```

Create `unity/Assets/Scripts/UI/ToastPresenter.cs`:

```csharp
#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A short message under the top bar: why orders were refused (spec section 7: the UI shows the stated
    /// reason), or a note such as a save warning. Hidden when there is nothing to say; cleared by the next order.</summary>
    public sealed class ToastPresenter
    {
        readonly Label label; readonly UiText text;

        public ToastPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            label = root.Q<Label>("toast"); this.text = text;
            bridge.TurnEnded += r => Show(r.Select(x => text.Format("toast.refused", x.Reason)));
            Clear();
        }

        /// <summary>Shows the lines as they are (none = hide).</summary>
        public void Show(IEnumerable<string> lines)
        {
            var list = lines.ToList();
            label.text = string.Join("\n", list);
            label.style.display = list.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show(string line) => Show(new[] { line });

        /// <summary>Shows why an order was refused ("Not done: ...").</summary>
        public void Refuse(string reason) => Show(text.Format("toast.refused", reason));

        public void Clear() => Show(new string[0]);
    }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="106" passed="106" failed="0"` (+5).

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/UI/HudModel.cs \
  unity/Assets/Scripts/UI/HudModel.cs.meta \
  unity/Assets/Scripts/UI/HudPresenter.cs \
  unity/Assets/Scripts/UI/HudPresenter.cs.meta \
  unity/Assets/Scripts/UI/ScreenNav.cs \
  unity/Assets/Scripts/UI/ScreenNav.cs.meta \
  unity/Assets/Scripts/UI/SimBridge.cs \
  unity/Assets/Scripts/UI/SimBridge.cs.meta \
  unity/Assets/Scripts/UI/ToastPresenter.cs \
  unity/Assets/Scripts/UI/ToastPresenter.cs.meta \
  unity/Assets/Tests/EditMode/HudTests.cs \
  unity/Assets/Tests/EditMode/HudTests.cs.meta
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): SimBridge, top bar with changes since last turn, refusal messages, screen navigation

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 15: The event card, with public-domain portraits

*Track B.* Ruling 2: an event card on the right with image, title, text and option buttons. The image is the responsible minister's portrait: public-domain photos already recorded in `people.yaml` (`portrait.file`, licence, source), so the card is sourced and needs no AI art. An editor tool copies them into the project and builds a `PortraitCatalog` asset referenced from the scene (tech notes §7: synchronous, in the build's data file). The card shows the first pending event without a queued answer, names today's office holders (appointees included, Task 6), and stamps `DRAFT` on draft content (spec §5.2). People without a recorded public-domain portrait show no image (Deferred: portrait pipeline).

**Files:**
- Create: `unity/Assets/Editor/PortraitImport.cs`, `unity/Assets/Scripts/UI/EventCardPresenter.cs`, `unity/Assets/Scripts/UI/PortraitCatalog.cs`, `unity/Assets/Tests/EditMode/EventCardTests.cs`, `unity/Assets/UI/Portraits/PORTRAITS.txt`, `unity/Assets/UI/Portraits/PortraitCatalog.asset`, `unity/Assets/UI/Portraits/ben_gurion.jpg`, `unity/Assets/UI/Portraits/bentov.jpg`, `unity/Assets/UI/Portraits/bernstein.jpg`, `unity/Assets/UI/Portraits/gruenbaum.jpg`, `unity/Assets/UI/Portraits/kaplan.jpg`, `unity/Assets/UI/Portraits/levin.jpg`, `unity/Assets/UI/Portraits/maimon.jpg`, `unity/Assets/UI/Portraits/remez.jpg`, `unity/Assets/UI/Portraits/rosen.jpg`, `unity/Assets/UI/Portraits/shapira.jpg`, `unity/Assets/UI/Portraits/sharett.jpg`, `unity/Assets/UI/Portraits/sheetrit.jpg`, `unity/Assets/UI/Portraits/zisling.jpg`
- Modify: `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Generated by a command in this task (commit the output): `unity/Assets/UI/Portraits/PORTRAITS.txt`, `unity/Assets/UI/Portraits/PortraitCatalog.asset`, `unity/Assets/UI/Portraits/ben_gurion.jpg`, `unity/Assets/UI/Portraits/bentov.jpg`, `unity/Assets/UI/Portraits/bernstein.jpg`, `unity/Assets/UI/Portraits/gruenbaum.jpg`, `unity/Assets/UI/Portraits/kaplan.jpg`, `unity/Assets/UI/Portraits/levin.jpg`, `unity/Assets/UI/Portraits/maimon.jpg`, `unity/Assets/UI/Portraits/remez.jpg`, `unity/Assets/UI/Portraits/rosen.jpg`, `unity/Assets/UI/Portraits/shapira.jpg`, `unity/Assets/UI/Portraits/sharett.jpg`, `unity/Assets/UI/Portraits/sheetrit.jpg`, `unity/Assets/UI/Portraits/zisling.jpg`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Editor/PortraitImport.cs.meta`, `unity/Assets/Scripts/UI/EventCardPresenter.cs.meta`, `unity/Assets/Scripts/UI/PortraitCatalog.cs.meta`, `unity/Assets/Tests/EditMode/EventCardTests.cs.meta`, `unity/Assets/UI/Portraits.meta`, `unity/Assets/UI/Portraits/PORTRAITS.txt.meta`, `unity/Assets/UI/Portraits/PortraitCatalog.asset.meta`, `unity/Assets/UI/Portraits/ben_gurion.jpg.meta`, `unity/Assets/UI/Portraits/bentov.jpg.meta`, `unity/Assets/UI/Portraits/bernstein.jpg.meta`, `unity/Assets/UI/Portraits/gruenbaum.jpg.meta`, `unity/Assets/UI/Portraits/kaplan.jpg.meta`, `unity/Assets/UI/Portraits/levin.jpg.meta`, `unity/Assets/UI/Portraits/maimon.jpg.meta`, `unity/Assets/UI/Portraits/remez.jpg.meta`, `unity/Assets/UI/Portraits/rosen.jpg.meta`, `unity/Assets/UI/Portraits/shapira.jpg.meta`, `unity/Assets/UI/Portraits/sharett.jpg.meta`, `unity/Assets/UI/Portraits/sheetrit.jpg.meta`, `unity/Assets/UI/Portraits/zisling.jpg.meta`

**Interfaces:**
- Consumes: `SimBridge`, `ToastPresenter.Refuse`, `Cabinet.Holder/Fill`, `EventSystem.FillDate/Render(Content, GameState, GameEvent)`, `Person.Portrait`.
- Produces:
  - `[CreateAssetMenu] public sealed class PortraitCatalog : ScriptableObject { [Serializable] struct Entry { string personId; Texture2D image; } List<Entry> entries; Texture2D? Get(string? personId); }`.
  - Editor `Statecraft.Game.EditorTools.PortraitImport.Run()` (constants `Folder = "Assets/UI/Portraits"`, `CatalogPath = "Assets/UI/Portraits/PortraitCatalog.asset"`); refuses any licence other than "Public domain" or "CC0"; writes `PORTRAITS.txt` (id, licence, source).
  - `EventCardPresenter(VisualElement root, SimBridge, UiText, PortraitCatalog?, ToastPresenter) { GameEvent? Shown; void Render(); }`; option buttons are named `option-<option id>`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/EventCardTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EventCardTests
{
    const string CatalogPath = "Assets/UI/Portraits/PortraitCatalog.asset";
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game on the turn the Egyptian armistice first waits for a decision (February 1949).</summary>
    static SimBridge AtArmistice()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        return new SimBridge(g);
    }

    [Test]
    public void ThePortraitCatalogHoldsEveryPublicDomainPortrait()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, CatalogPath + " (run PortraitImport.Run)");
        var expected = C.People.Where(p => p.Portrait != null && p.Portrait.File != "").Select(p => p.Id).OrderBy(x => x).ToArray();
        Assert.AreEqual(expected, catalog.entries.Select(e => e.personId).OrderBy(x => x).ToArray());
        Assert.IsNotNull(catalog.Get("ben_gurion"));
        Assert.IsNull(catalog.Get("golda_meir"));                     // no public-domain portrait recorded
    }

    [Test]
    public void TheCardShowsThePendingEventWithTheMinisterAndOptions()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, catalog, toast);
        Assert.AreEqual("armistice_egypt", card.Shown!.Id);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q<VisualElement>("event-card").style.display.value);
        Assert.AreEqual("Armistice with Egypt", ui.Q<Label>("event-title").text);
        Assert.AreEqual("Prime Minister: David Ben-Gurion", ui.Q<Label>("event-office").text);
        StringAssert.Contains("David Ben-Gurion must decide whether Israel signs.", ui.Q<Label>("event-text").text);
        Assert.AreEqual(catalog.Get("ben_gurion"), ui.Q<VisualElement>("event-image").style.backgroundImage.value.texture);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("event-draft").style.display.value);
        Assert.AreEqual("Sign the armistice", ui.Q<Button>("option-sign").text);
    }

    [Test]
    public void ChoosingQueuesTheAnswerAndTheTurnAppliesIt()
    {
        using var ui = new UiTestKit();
        var bridge = AtArmistice();
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var card = new EventCardPresenter(ui.Root, bridge, Text, null, toast);
        UiTestKit.Press(ui.Q<Button>("option-sign"));
        var queued = (ChooseEventOption)bridge.Queued.Single();
        Assert.AreEqual(("armistice_egypt", "sign"), (queued.EventId, queued.OptionId));
        Assert.AreNotEqual("armistice_egypt", card.Shown?.Id);           // the card moves on
        bridge.EndTurn();
        Assert.AreEqual(2, bridge.State.Rung);                            // signing ends full war
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "chosen: Sign the armistice"));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'PortraitCatalog' could not be found`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Editor/PortraitImport.cs`:

```csharp
using System.IO;
using System.Linq;
using System.Text;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Copies every portrait people.yaml names (public-domain files under promo/ref/people) into
    /// Assets/UI/Portraits/&lt;person id&gt;.jpg, sets web-friendly import settings, writes PORTRAITS.txt (id, licence,
    /// source) and rebuilds PortraitCatalog.asset. Idempotent.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.PortraitImport.Run</summary>
    public static class PortraitImport
    {
        public const string Folder = "Assets/UI/Portraits";
        public const string CatalogPath = Folder + "/PortraitCatalog.asset";

        public static void Run()
        {
            var content = ContentLoader.Load(Paths.Content);
            Directory.CreateDirectory(Folder);
            var credits = new StringBuilder("# Portraits shipped in the build (copied from people.yaml portrait records; public domain only).\n");
            var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PortraitCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.entries.Clear();
            foreach (var p in content.People.Where(p => p.Portrait != null && p.Portrait.File != ""))
            {
                if (p.Portrait!.License != "Public domain" && p.Portrait.License != "CC0")
                    throw new System.InvalidOperationException($"people.yaml: {p.Id} portrait licence '{p.Portrait.License}' is not public domain or CC0");
                var source = Path.Combine(Paths.RepoRoot, p.Portrait.File);
                if (!File.Exists(source)) throw new FileNotFoundException($"people.yaml: {p.Id} portrait file missing", source);
                var target = $"{Folder}/{p.Id}.jpg";
                File.Copy(source, target, true);
                AssetDatabase.ImportAsset(target);
                var importer = (TextureImporter)AssetImporter.GetAtPath(target);
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = 512;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.crunchedCompression = true;
                importer.compressionQuality = 50;
                importer.SaveAndReimport();
                catalog.entries.Add(new PortraitCatalog.Entry { personId = p.Id, image = AssetDatabase.LoadAssetAtPath<Texture2D>(target) });
                credits.Append(p.Id).Append(" | ").Append(p.Portrait.License).Append(" | ").Append(p.Portrait.Source).Append('\n');
            }
            File.WriteAllText(Folder + "/PORTRAITS.txt", credits.ToString());
            AssetDatabase.ImportAsset(Folder + "/PORTRAITS.txt");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
```

Create `unity/Assets/Scripts/UI/EventCardPresenter.cs`:

```csharp
#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The event card (right of the screen): the first pending event the player has not yet answered, with the
    /// responsible minister's portrait, the text with today's office holders filled in, and one button per option.
    /// A choice is an order: it takes effect at End Turn, and an unanswered event is delegated to the minister.</summary>
    public sealed class EventCardPresenter
    {
        readonly VisualElement card; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly ToastPresenter toast;

        public EventCardPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits, ToastPresenter toast)
        {
            card = root.Q<VisualElement>("event-card"); this.bridge = bridge; this.text = text; this.portraits = portraits; this.toast = toast;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>The event the card shows, or null when every pending event has a queued answer.</summary>
        public GameEvent? Shown { get; private set; }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var pending = s.PendingEvents.Select(id => c.Events.First(e => e.Id == id)).ToList();
            var open = pending.Where(e => bridge.QueuedLike(new ChooseEventOption(e.Id, "")) == null).ToList();
            Shown = open.FirstOrDefault();
            card.style.display = Shown == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Shown == null) return;
            var e = Shown;
            var date = EventSystem.FillDate(e, s.Date);
            var holder = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office, date);
            var image = card.Q<VisualElement>("event-image");
            var portrait = portraits?.Get(holder?.Id);
            image.style.backgroundImage = portrait == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(portrait);
            image.style.display = portrait == null ? DisplayStyle.None : DisplayStyle.Flex;
            card.Q<Label>("event-draft").style.display = e.Status == "draft" ? DisplayStyle.Flex : DisplayStyle.None;
            card.Q<Label>("event-title").text = Cabinet.Fill(c, s, e.Title, date);
            card.Q<Label>("event-office").text = holder == null ? "" : text.Format("event.office", c.Offices.First(o => o.Id == e.Office).Title, holder.Name);
            card.Q<Label>("event-text").text = EventSystem.Render(c, s, e);
            var options = card.Q<VisualElement>("event-options");
            options.Clear();
            foreach (var o in e.Options)
            {
                var option = o;
                var b = new Button(() => Choose(e, option)) { name = "option-" + o.Id, text = Cabinet.Fill(c, s, o.Text, date) };
                options.Add(b);
            }
            card.Q<Label>("event-count").text = open.Count > 1 ? text.Format("event.more", open.Count - 1) : "";
        }

        void Choose(GameEvent e, EventOption o)
        {
            var refused = bridge.Queue(new ChooseEventOption(e.Id, o.Id));
            if (refused != null) toast.Refuse(refused); else toast.Clear();
        }
    }
}
```

Create `unity/Assets/Scripts/UI/PortraitCatalog.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>Person id -> portrait texture (tech notes section 7): imported assets referenced from a scene object, so
    /// they ship in the build's data file and need no runtime loading. Built by the editor tool PortraitImport from the
    /// public-domain files people.yaml names (portrait.file, licence and source recorded there).</summary>
    [CreateAssetMenu(menuName = "Statecraft/Portrait Catalog")]
    public sealed class PortraitCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry { public string personId; public Texture2D image; }

        public List<Entry> entries = new List<Entry>();

        /// <summary>The person's portrait, or null when there is none.</summary>
        public Texture2D? Get(string? personId)
        {
            if (personId == null) return null;
            foreach (var e in entries) if (e.personId == personId) return e.image;
            return null;
        }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
        </ui:VisualElement>
        <ui:VisualElement name="event-card" class="panel event-card" />
        <ui:VisualElement name="info-panel" class="panel info-panel" />
```

   with

```xml
        </ui:VisualElement>
        <ui:VisualElement name="event-card" class="panel event-card">
            <ui:VisualElement name="event-image" class="event-image" />
            <ui:Label name="event-draft" text="DRAFT" class="draft" />
            <ui:Label name="event-title" class="title event-title" />
            <ui:Label name="event-office" class="muted" />
            <ui:Label name="event-text" class="event-text" />
            <ui:VisualElement name="event-options" />
            <ui:Label name="event-count" class="muted" />
        </ui:VisualElement>
        <ui:VisualElement name="info-panel" class="panel info-panel" />
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
toast.refused: "Not done: {0}"
```

   with

```yaml
toast.refused: "Not done: {0}"
event.office: "{0}: {1}"
event.more: "{0} more decision(s) waiting"
```

- [ ] **Step 4: Import the portraits**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.PortraitImport.Run -logFile unity-portraits.log; echo exit=$?
ls unity/Assets/UI/Portraits/*.jpg | wc -l; head -3 unity/Assets/UI/Portraits/PORTRAITS.txt
```
Expected: `exit=0`, `13`, then the header line and `ben_gurion | Public domain | https://commons.wikimedia.org/wiki/File:David_Ben-Gurion_(D597-087).jpg`.

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="109" passed="109" failed="0"`.

- [ ] **Step 6: Commit**

```bash
git add unity/Assets/Editor/PortraitImport.cs \
  unity/Assets/Editor/PortraitImport.cs.meta \
  unity/Assets/Scripts/UI/EventCardPresenter.cs \
  unity/Assets/Scripts/UI/EventCardPresenter.cs.meta \
  unity/Assets/Scripts/UI/PortraitCatalog.cs \
  unity/Assets/Scripts/UI/PortraitCatalog.cs.meta \
  unity/Assets/Tests/EditMode/EventCardTests.cs \
  unity/Assets/Tests/EditMode/EventCardTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/Portraits.meta \
  unity/Assets/UI/Portraits \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): event card with the minister's public-domain portrait and option buttons

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 16: The Defence screen

*Track B.* Ruling 1: the escalation ladder, mobilisation and security orders (spec §3, "always active"). The screen shows the five rungs with the current one marked and each rung's turn length, the UN ceasefire clock in full war, the Defence Minister and Chief of Staff, orders to raise or lower the level, and the reserve call-up (±5%) with its minimum at the ordered level and its yearly cost. Pressing back to today's value withdraws the order.

**Files:**
- Create: `unity/Assets/Scripts/UI/DefencePresenter.cs`, `unity/Assets/Tests/EditMode/DefenceScreenTests.cs`
- Modify: `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/UI/DefencePresenter.cs.meta`, `unity/Assets/Tests/EditMode/DefenceScreenTests.cs.meta`

**Interfaces:**
- Consumes: `SimBridge.Queue/QueuedLike/Withdraw`, `SecurityOrder`, `SetMobilization`, `SecuritySystem.TurnLengthFor`, `Rules.RungMinMobilization/MobilizationCostPerYear`, `Cabinet.Holder`.
- Produces: `DefencePresenter(VisualElement root, SimBridge, UiText, ToastPresenter) { const double MobilizationStep = 0.05; double ProposedMobilization; void Render(); }`; element names `rung-<1..5>`, `rung-<n>-name`, `rung-<n>-turn`, `ceasefire`, `escalate`, `de-escalate`, `order-pending`, `mob-down`, `mob-up`, `mob-level`, `mob-cost`, `defence-command`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/DefenceScreenTests.cs`:

```csharp
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DefenceScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    static (UiTestKit ui, SimBridge bridge, DefencePresenter defence) Open()
    {
        var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        return (ui, bridge, new DefencePresenter(ui.Root, bridge, Text, toast));
    }

    [Test]
    public void TheLadderShowsFullWarWithTheCeasefireClock()
    {
        var (ui, _, _) = Open();
        using (ui)
        {
            Assert.IsTrue(ui.Q<VisualElement>("rung-5").ClassListContains("rung-current"));
            Assert.IsFalse(ui.Q<VisualElement>("rung-2").ClassListContains("rung-current"));
            Assert.AreEqual("turns of a week", ui.Q<Label>("rung-5-turn").text);
            Assert.AreEqual("turns of a quarter", ui.Q<Label>("rung-1-turn").text);
            Assert.AreEqual("A UN ceasefire will be imposed in 52 weeks", ui.Q<Label>("ceasefire").text);
            Assert.AreEqual("Minister of Defense David Ben-Gurion · Chief of Staff Yaakov Dori", ui.Q<Label>("defence-command").text);
            Assert.AreEqual("80% of reserves called up (level 5 needs at least 80%)", ui.Q<Label>("mob-level").text);
            Assert.AreEqual("Costs 48.0m a year in pay and lost work", ui.Q<Label>("mob-cost").text);
            Assert.IsFalse(ui.Q<Button>("escalate").enabledSelf);
        }
    }

    [Test]
    public void OrdersAreQueuedAndTheTurnCarriesThemOut()
    {
        var (ui, bridge, defence) = Open();
        using (ui)
        {
            ui.Press("de-escalate");
            Assert.AreEqual("Ordered: level 5 to 4", ui.Q<Label>("order-pending").text);
            Assert.AreEqual("80% of reserves called up (level 4 needs at least 40%)", ui.Q<Label>("mob-level").text);
            ui.Press("mob-down");
            Assert.AreEqual(0.75, defence.ProposedMobilization, 1e-9);
            bridge.EndTurn();
            Assert.AreEqual(4, bridge.State.Rung);
            Assert.AreEqual(0.75, bridge.State.Mobilization, 1e-9);
            Assert.IsTrue(ui.Q<VisualElement>("rung-4").ClassListContains("rung-current"));
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("ceasefire").style.display.value);
        }
    }

    [Test]
    public void PressingBackToTodaysLevelWithdrawsTheOrder()
    {
        var (ui, bridge, _) = Open();
        using (ui)
        {
            ui.Press("de-escalate");
            ui.Press("escalate");
            Assert.AreEqual(0, bridge.Queued.Count);
            Assert.AreEqual("", ui.Q<Label>("order-pending").text);
        }
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'DefencePresenter' could not be found`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/UI/DefencePresenter.cs`:

```csharp
#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Defence screen: the always-on escalation ladder (spec section 3) with the current level and its turn
    /// length, the UN ceasefire clock in full war, security orders to raise or lower the level, and the reserve call-up
    /// with its yearly cost. Orders are queued for End Turn.</summary>
    public sealed class DefencePresenter
    {
        public const double MobilizationStep = 0.05;
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly ToastPresenter toast;

        public DefencePresenter(VisualElement root, SimBridge bridge, UiText text, ToastPresenter toast)
        {
            screen = root.Q<VisualElement>("screen-defence"); this.bridge = bridge; this.text = text; this.toast = toast;
            screen.Q<Button>("escalate").clicked += () => Order(+1);
            screen.Q<Button>("de-escalate").clicked += () => Order(-1);
            screen.Q<Button>("mob-up").clicked += () => Mobilize(+MobilizationStep);
            screen.Q<Button>("mob-down").clicked += () => Mobilize(-MobilizationStep);
            bridge.Changed += Render;
            Render();
        }

        int QueuedDelta => (bridge.QueuedLike(new SecurityOrder(0)) as SecurityOrder)?.Delta ?? 0;

        /// <summary>The call-up level the turn will end with: the queued order's, else today's.</summary>
        public double ProposedMobilization => (bridge.QueuedLike(new SetMobilization(0)) as SetMobilization)?.Level ?? bridge.State.Mobilization;

        void Order(int step)
        {
            var s = bridge.State;
            var target = Math.Max(1, Math.Min(5, s.Rung + QueuedDelta + step));
            Report(target == s.Rung ? Withdraw(new SecurityOrder(0)) : bridge.Queue(new SecurityOrder(target - s.Rung)));
        }

        string? Withdraw(Command like)
        {
            var queued = bridge.QueuedLike(like);
            if (queued != null) bridge.Withdraw(queued);
            return null;
        }

        void Mobilize(double step)
        {
            var level = Math.Max(0, Math.Min(1, Math.Round((ProposedMobilization + step) / MobilizationStep) * MobilizationStep));
            Report(Math.Abs(level - bridge.State.Mobilization) < 1e-9 ? Withdraw(new SetMobilization(0)) : bridge.Queue(new SetMobilization(level)));
        }

        void Report(string? refused) { if (refused != null) toast.Refuse(refused); else toast.Clear(); }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            for (int rung = 1; rung <= 5; rung++)
            {
                screen.Q<Label>($"rung-{rung}-name").text = text[$"defence.rung{rung}"];
                screen.Q<Label>($"rung-{rung}-turn").text = text["defence.turn_" + SecuritySystem.TurnLengthFor(rung).ToString().ToLowerInvariant()];
                screen.Q<VisualElement>($"rung-{rung}").EnableInClassList("rung-current", rung == s.Rung);
            }
            screen.Q<Label>("defence-command").text = text.Format("defence.command",
                Cabinet.Holder(c, s, "defense_minister")?.Name ?? "-", Cabinet.Holder(c, s, "chief_of_staff")?.Name ?? "-");
            var ceasefire = screen.Q<Label>("ceasefire");
            ceasefire.text = s.Rung == 5 ? text.Format("defence.ceasefire", s.CeasefireWeeksLeft) : "";
            ceasefire.style.display = s.Rung == 5 ? DisplayStyle.Flex : DisplayStyle.None;
            var delta = QueuedDelta;
            screen.Q<Label>("order-pending").text = delta == 0 ? "" : text.Format("defence.order", s.Rung, s.Rung + delta);
            var proposed = ProposedMobilization;
            var floor = c.Rules.RungMinMobilization[Math.Max(1, Math.Min(5, s.Rung + delta)) - 1];
            screen.Q<Label>("mob-level").text = text.Format("defence.mob", proposed * 100, s.Rung + delta, floor * 100);
            screen.Q<Label>("mob-cost").text = text.Format("defence.mob_cost", c.Rules.MobilizationCostPerYear * Math.Max(proposed, floor));
            screen.Q<Button>("escalate").SetEnabled(s.Rung + delta < 5);
            screen.Q<Button>("de-escalate").SetEnabled(s.Rung + delta > 1);
        }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
        <ui:VisualElement name="screen-budget" class="panel screen" />
        <ui:VisualElement name="screen-defence" class="panel screen" />
        <ui:VisualElement name="screen-why" class="panel screen" />
```

   with

```xml
        <ui:VisualElement name="screen-budget" class="panel screen" />
        <ui:VisualElement name="screen-defence" class="panel screen">
            <ui:VisualElement class="screen-header"><ui:Label text="Defence" class="title" /><ui:Label name="defence-command" class="muted" /></ui:VisualElement>
            <ui:Label text="Security level" class="meter-name" />
            <ui:VisualElement name="ladder">
                <ui:VisualElement name="rung-5" class="rung"><ui:Label name="rung-5-name" class="grow" /><ui:Label name="rung-5-turn" class="muted" /></ui:VisualElement>
                <ui:VisualElement name="rung-4" class="rung"><ui:Label name="rung-4-name" class="grow" /><ui:Label name="rung-4-turn" class="muted" /></ui:VisualElement>
                <ui:VisualElement name="rung-3" class="rung"><ui:Label name="rung-3-name" class="grow" /><ui:Label name="rung-3-turn" class="muted" /></ui:VisualElement>
                <ui:VisualElement name="rung-2" class="rung"><ui:Label name="rung-2-name" class="grow" /><ui:Label name="rung-2-turn" class="muted" /></ui:VisualElement>
                <ui:VisualElement name="rung-1" class="rung"><ui:Label name="rung-1-name" class="grow" /><ui:Label name="rung-1-turn" class="muted" /></ui:VisualElement>
            </ui:VisualElement>
            <ui:Label name="ceasefire" class="muted" />
            <ui:VisualElement class="row">
                <ui:Button name="de-escalate" text="Lower the level" />
                <ui:Button name="escalate" text="Raise the level" />
                <ui:Label name="order-pending" class="muted grow" />
            </ui:VisualElement>
            <ui:Label text="Reserve call-up" class="meter-name" />
            <ui:VisualElement class="row">
                <ui:Button name="mob-down" text="-5%" />
                <ui:Label name="mob-level" class="grow" />
                <ui:Button name="mob-up" text="+5%" />
            </ui:VisualElement>
            <ui:Label name="mob-cost" class="muted" />
        </ui:VisualElement>
        <ui:VisualElement name="screen-why" class="panel screen" />
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
event.more: "{0} more decision(s) waiting"
```

   with

```yaml
event.more: "{0} more decision(s) waiting"
defence.rung1: "1  Routine threat: border incidents, deterrence upkeep"
defence.rung2: "2  Wave: terror or infiltration, hostile buildup"
defence.rung3: "3  Limited operation: raids, reprisals"
defence.rung4: "4  Prolonged conflict: attrition, long call-ups"
defence.rung5: "5  Full war"
defence.turn_quarter: "turns of a quarter"
defence.turn_month: "turns of a month"
defence.turn_week: "turns of a week"
defence.command: "Minister of Defense {0} · Chief of Staff {1}"
defence.ceasefire: "A UN ceasefire will be imposed in {0} weeks"
defence.order: "Ordered: level {0} to {1}"
defence.mob: "{0:0}% of reserves called up (level {1} needs at least {2:0}%)"
defence.mob_cost: "Costs {0:0.0}m a year in pay and lost work"
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="112" passed="112" failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/UI/DefencePresenter.cs \
  unity/Assets/Scripts/UI/DefencePresenter.cs.meta \
  unity/Assets/Tests/EditMode/DefenceScreenTests.cs \
  unity/Assets/Tests/EditMode/DefenceScreenTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): Defence screen: escalation ladder, ceasefire clock, security orders, reserve call-up

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 17: The Budget screen

*Track B.* Ruling 1: Budget. Each budgeted ministry's share, its minister, and the effect multiplier the share buys (the sim's own `EffectsSystem.BudgetFactor(shares, office)`, Task 5); the money position; and whether the legislature must approve (per-era `budget_approval`, spec §3). ± moves a ministry by 5 points and rescales the others proportionally; the largest other share absorbs rounding, so the total is exactly 1 and the sim never refuses the allocation for its sum.

**Files:**
- Create: `unity/Assets/Scripts/UI/BudgetPresenter.cs`, `unity/Assets/Tests/EditMode/BudgetScreenTests.cs`
- Modify: `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/UI/BudgetPresenter.cs.meta`, `unity/Assets/Tests/EditMode/BudgetScreenTests.cs.meta`

**Interfaces:**
- Consumes: `AllocateBudget`, `Rules.BudgetStart`, `Offices.Legislature`, `PoliticsSystem.CoalitionSeats`, `Cabinet.Holder`.
- Produces: `BudgetPresenter(VisualElement root, SimBridge, UiText, ToastPresenter) { const double Step = 0.05; IReadOnlyDictionary<string,double> Draft; static Dictionary<string,double> Adjust(IReadOnlyDictionary<string,double>, string office, double delta); void Change(string office, double delta); void Render(); }`; element names `budget-<office>-name/-share/-factor/-up/-down`, `budget-vote`, `budget-money`, `budget-flow`, `budget-reset`, `budget-submit`, `budget-status`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/BudgetScreenTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BudgetScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void AdjustKeepsTheTotalAtExactlyOne()
    {
        var shares = new Dictionary<string, double> { ["a"] = 0.45, ["b"] = 0.35, ["c"] = 0.2 };
        var up = BudgetPresenter.Adjust(shares, "c", 0.05);
        Assert.AreEqual(0.25, up["c"], 1e-12);
        Assert.AreEqual(1.0, up.Values.Sum(), 1e-12);
        Assert.Greater(up["a"] / up["b"], 1.28);                      // the others keep their proportions
        var floor = BudgetPresenter.Adjust(shares, "c", -0.5);
        Assert.AreEqual(0.0, floor["c"]);
        Assert.AreEqual(1.0, floor.Values.Sum(), 1e-12);
    }

    [Test]
    public void TheScreenListsTheMinistriesAndSubmitsTheDraft()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var budget = new BudgetPresenter(ui.Root, bridge, Text, toast);
        Assert.AreEqual("45%", ui.Q<Label>("budget-defense_minister-share").text);
        Assert.AreEqual("Minister of Defense (David Ben-Gurion)", ui.Q<Label>("budget-defense_minister-name").text);
        Assert.AreEqual("effect x1.50", ui.Q<Label>("budget-defense_minister-factor").text);   // capped at 1.5
        Assert.AreEqual("The Provisional State Council does not vote on the budget", ui.Q<Label>("budget-vote").text);
        Assert.AreEqual("Treasury 20.0m · debt 0.0m · foreign currency $40.0m", ui.Q<Label>("budget-money").text);
        UiTestKit.Press(ui.Q<Button>("budget-education_minister-up"));
        Assert.AreEqual("11%", ui.Q<Label>("budget-education_minister-share").text);
        Assert.AreEqual("Changed: submit to put it to the vote", ui.Q<Label>("budget-status").text);
        ui.Press("budget-submit");
        var queued = (AllocateBudget)bridge.Queued.Single();
        Assert.AreEqual(1.0, queued.Shares.Values.Sum(), 1e-9);
        Assert.AreEqual("Submitted: takes effect at End Turn", ui.Q<Label>("budget-status").text);
        bridge.EndTurn();
        Assert.AreEqual(budget.Draft["education_minister"], bridge.State.Budget["education_minister"], 1e-12);
        Assert.IsEmpty(bridge.Session.Last!.Rejected);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0103: The name 'BudgetPresenter' does not exist in the current context`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/UI/BudgetPresenter.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Budget screen: each budgeted ministry's share of spending, its minister and the effect multiplier the
    /// share gives (the sim's own EffectsSystem.BudgetFactor), the money position, and whether the legislature must approve. +/- moves
    /// a ministry by 5 points and rescales the others so shares always sum to 1. Submit queues AllocateBudget.</summary>
    public sealed class BudgetPresenter
    {
        public const double Step = 0.05;
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly ToastPresenter toast;
        Dictionary<string, double> draft = new Dictionary<string, double>();

        public BudgetPresenter(VisualElement root, SimBridge bridge, UiText text, ToastPresenter toast)
        {
            screen = root.Q<VisualElement>("screen-budget"); this.bridge = bridge; this.text = text; this.toast = toast;
            screen.Q<Button>("budget-reset").clicked += () => { draft = Current(); Render(); };
            screen.Q<Button>("budget-submit").clicked += Submit;
            draft = Current();
            bridge.Changed += () => { if (bridge.QueuedLike(new AllocateBudget(new Dictionary<string, double>())) == null && !Dirty) draft = Current(); Render(); };
            Render();
        }

        /// <summary>The shares the screen proposes.</summary>
        public IReadOnlyDictionary<string, double> Draft => draft;

        Dictionary<string, double> Current() => new Dictionary<string, double>(bridge.State.Budget);

        bool Dirty => draft.Any(kv => Math.Abs(kv.Value - (bridge.State.Budget.TryGetValue(kv.Key, out var v) ? v : 0)) > 1e-9);

        /// <summary>Moves <paramref name="office"/> by <paramref name="delta"/> (clamped to [0, 1]) and rescales the other
        /// shares proportionally; the largest other share absorbs rounding so the total is exactly 1.</summary>
        public static Dictionary<string, double> Adjust(IReadOnlyDictionary<string, double> shares, string office, double delta)
        {
            var target = Math.Max(0, Math.Min(1, shares[office] + delta));
            var others = shares.Keys.Where(k => k != office).OrderBy(k => k, StringComparer.Ordinal).ToList();
            var rest = others.Sum(k => shares[k]);
            var result = new Dictionary<string, double> { [office] = target };
            foreach (var k in others) result[k] = rest > 0 ? shares[k] / rest * (1 - target) : (1 - target) / others.Count;
            if (others.Count > 0)
            {
                var largest = others.OrderByDescending(k => result[k]).ThenBy(k => k, StringComparer.Ordinal).First();
                result[largest] = 1 - result.Where(kv => kv.Key != largest).Sum(kv => kv.Value);
            }
            return result;
        }

        public void Change(string office, double delta) { draft = Adjust(draft, office, delta); Render(); }

        void Submit()
        {
            var refused = bridge.Queue(new AllocateBudget(new Dictionary<string, double>(draft)));
            if (refused != null) toast.Refuse(refused); else toast.Clear();
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var rows = screen.Q<ScrollView>("budget-rows");
            rows.Clear();
            foreach (var office in c.Rules.BudgetStart.Keys)
            {
                var o = c.Offices.First(x => x.Id == office);
                var share = draft.TryGetValue(office, out var v) ? v : 0;
                var row = new VisualElement { name = "budget-" + office };
                row.AddToClassList("row");
                row.Add(new Label(text.Format("budget.office", o.Title, Cabinet.Holder(c, s, office)?.Name ?? "-")) { name = "budget-" + office + "-name" });
                row[0].AddToClassList("grow");
                row.Add(new Button(() => Change(office, -Step)) { name = "budget-" + office + "-down", text = "-" });
                row.Add(new Label(text.Format("budget.share", share * 100)) { name = "budget-" + office + "-share" });
                row.Add(new Button(() => Change(office, +Step)) { name = "budget-" + office + "-up", text = "+" });
                var factor = EffectsSystem.BudgetFactor(draft, office);
                row.Add(new Label(text.Format("budget.factor", factor)) { name = "budget-" + office + "-factor" });
                row[4].AddToClassList("muted");
                rows.Add(row);
            }
            var leg = Offices.Legislature(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            screen.Q<Label>("budget-vote").text = leg == null ? "" : leg.BudgetApproval
                ? text.Format("budget.vote_needed", leg.Body, seats, size) : text.Format("budget.no_vote", leg.Body);
            screen.Q<Label>("budget-money").text = text.Format("budget.money", s.Treasury, s.Debt, s.FxReserves);
            screen.Q<Label>("budget-flow").text = text.Format("budget.flow",
                c.Rules.TaxPerCapitaPerYear * s.TotalPopulation, c.Rules.SpendingPerCapitaPerYear * s.TotalPopulation);
            var queued = bridge.QueuedLike(new AllocateBudget(new Dictionary<string, double>())) != null;
            screen.Q<Label>("budget-status").text = queued ? text["budget.queued"] : Dirty ? text["budget.unsaved"] : "";
        }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
        <ui:VisualElement name="screen-cabinet" class="panel screen" />
        <ui:VisualElement name="screen-budget" class="panel screen" />
        <ui:VisualElement name="screen-defence" class="panel screen">
```

   with

```xml
        <ui:VisualElement name="screen-cabinet" class="panel screen" />
        <ui:VisualElement name="screen-budget" class="panel screen">
            <ui:VisualElement class="screen-header"><ui:Label text="Budget" class="title" /><ui:Label name="budget-vote" class="muted" /></ui:VisualElement>
            <ui:Label name="budget-money" />
            <ui:Label name="budget-flow" class="muted" />
            <ui:ScrollView name="budget-rows" class="grow" />
            <ui:VisualElement class="row">
                <ui:Button name="budget-reset" text="Reset" />
                <ui:Button name="budget-submit" text="Submit the budget" />
                <ui:Label name="budget-status" class="muted grow" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="screen-defence" class="panel screen">
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
defence.mob_cost: "Costs {0:0.0}m a year in pay and lost work"
```

   with

```yaml
defence.mob_cost: "Costs {0:0.0}m a year in pay and lost work"
budget.share: "{0:0}%"
budget.office: "{0} ({1})"
budget.factor: "effect x{0:0.00}"
budget.vote_needed: "The {0} must approve it: the coalition holds {1} of {2} seats"
budget.no_vote: "The {0} does not vote on the budget"
budget.money: "Treasury {0:0.0}m · debt {1:0.0}m · foreign currency ${2:0.0}m"
budget.flow: "Taxes {0:0.0}m and spending {1:0.0}m a year"
budget.queued: "Submitted: takes effect at End Turn"
budget.unsaved: "Changed: submit to put it to the vote"
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="114" passed="114" failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/UI/BudgetPresenter.cs \
  unity/Assets/Scripts/UI/BudgetPresenter.cs.meta \
  unity/Assets/Tests/EditMode/BudgetScreenTests.cs \
  unity/Assets/Tests/EditMode/BudgetScreenTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): Budget screen with shares that always sum to one

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 18: The "Why did this happen?" panel

*Track B.* Ruling 1 and spec §4 step 6: every cause the last turn reported, grouped by the system that produced it, in a fixed order (decisions first, then consequences), with the turn's date range. The sim's cause lines are already truthful and culture-invariant (M0–M2 rulings).

**Files:**
- Create: `unity/Assets/Scripts/UI/WhyPresenter.cs`, `unity/Assets/Tests/EditMode/WhyScreenTests.cs`
- Modify: `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/UI/WhyPresenter.cs.meta`, `unity/Assets/Tests/EditMode/WhyScreenTests.cs.meta`

**Interfaces:**
- Consumes: `GameSession.Last.Why` (`Cause.System`, `Cause.Message`), `GameSession.Previous`.
- Produces: `WhyPresenter(VisualElement root, SimBridge, UiText) { static string[] Order; void Render(); }`; element names `why-turn`, `why-rows`, `why-empty`, `why-group-<system>`, lines with class `why-line`.

- [ ] **Step 1: Write the failing test**

Create `unity/Assets/Tests/EditMode/WhyScreenTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class WhyScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void EveryCauseOfTheLastTurnIsListedUnderItsSystem()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        new WhyPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("Nothing has happened yet: end the turn to see its causes.", ui.Q<Label>("why-empty").text);
        bridge.Queue(new SetMobilization(1.0));
        var r = bridge.EndTurn();
        Assert.AreEqual("15 May 1948 to 22 May 1948", ui.Q<Label>("why-turn").text);
        var lines = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
        Assert.AreEqual(r.Why.Count, lines.Count);
        Assert.AreEqual("Immigration and population", ui.Q<Label>("why-group-population").text);
        Assert.IsTrue(lines.Any(l => l.text == "reserve call-up set to 100%"));
        var groups = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-group").ToList().Select(l => l.name).ToList();
        Assert.Less(groups.IndexOf("why-group-security"), groups.IndexOf("why-group-economy"));
    }
}
```

- [ ] **Step 2: Run it and confirm it fails**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'WhyPresenter' could not be found`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/UI/WhyPresenter.cs`:

```csharp
#nullable enable
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>"Why did this happen?" (spec section 4, step 6): every cause the last turn reported, grouped by the system
    /// that produced it, in a fixed order (what was decided first, then its consequences).</summary>
    public sealed class WhyPresenter
    {
        public static readonly string[] Order = { "events", "politics", "cabinet", "security", "economy", "population", "map", "policy", "effects", "situations" };
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;

        public WhyPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-why"); this.bridge = bridge; this.text = text;
            bridge.Changed += Render;
            Render();
        }

        public void Render()
        {
            var rows = screen.Q<ScrollView>("why-rows");
            rows.Clear();
            var last = bridge.Session.Last; var prev = bridge.Session.Previous;
            screen.Q<Label>("why-turn").text = last == null || prev == null ? "" : text.Format("why.turn", prev.Date, bridge.State.Date);
            if (last == null) { rows.Add(new Label(text["why.empty"]) { name = "why-empty" }); return; }
            int i = 0;
            foreach (var system in Order.Concat(last.Why.Select(w => w.System).Distinct().Where(sys => !Order.Contains(sys))))
            {
                var lines = last.Why.Where(w => w.System == system).ToList();
                if (lines.Count == 0) continue;
                var header = new Label(Order.Contains(system) ? text["why." + system] : system) { name = "why-group-" + system };
                header.AddToClassList("why-group");
                rows.Add(header);
                foreach (var w in lines)
                {
                    var line = new Label(w.Message) { name = "why-" + i++ };
                    line.AddToClassList("why-line");
                    rows.Add(line);
                }
            }
        }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
        </ui:VisualElement>
        <ui:VisualElement name="screen-why" class="panel screen" />
        <ui:Label name="toast" class="panel toast" />
```

   with

```xml
        </ui:VisualElement>
        <ui:VisualElement name="screen-why" class="panel screen">
            <ui:VisualElement class="screen-header"><ui:Label text="Why did this happen?" class="title" /><ui:Label name="why-turn" class="muted" /></ui:VisualElement>
            <ui:ScrollView name="why-rows" class="grow" />
        </ui:VisualElement>
        <ui:Label name="toast" class="panel toast" />
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
budget.unsaved: "Changed: submit to put it to the vote"
```

   with

```yaml
budget.unsaved: "Changed: submit to put it to the vote"
why.turn: "{0:d MMMM yyyy} to {1:d MMMM yyyy}"
why.empty: "Nothing has happened yet: end the turn to see its causes."
why.events: "Events"
why.politics: "Politics"
why.cabinet: "Cabinet"
why.security: "Security"
why.economy: "Economy"
why.population: "Immigration and population"
why.map: "Building"
why.policy: "Policies"
why.effects: "Effects of policies"
why.situations: "Situations"
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="115" passed="115" failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/UI/WhyPresenter.cs \
  unity/Assets/Scripts/UI/WhyPresenter.cs.meta \
  unity/Assets/Tests/EditMode/WhyScreenTests.cs \
  unity/Assets/Tests/EditMode/WhyScreenTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): Why did this happen? panel grouped by system

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 19: The Cabinet screen and Appoint

*Track B.* Ruling 1: Cabinet with Appoint. The government of the day, its coalition seats and stability, and every office with its holder (portrait, party on the date, competence, loyalty; appointees marked). Selecting an office lists every person with an Appoint button, or the reason they cannot be appointed (Task 6's `Cabinet.Check`), so illegal appointments are explained before they are tried.

**Files:**
- Create: `unity/Assets/Scripts/UI/CabinetPresenter.cs`, `unity/Assets/Tests/EditMode/CabinetScreenTests.cs`
- Modify: `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/UI/CabinetPresenter.cs.meta`, `unity/Assets/Tests/EditMode/CabinetScreenTests.cs.meta`

**Interfaces:**
- Consumes: `Cabinet.Lineup/Candidates/Holder`, `Appoint` command, `Offices.Government/PartyOf`, `PoliticsSystem.CoalitionSeats`, `PortraitCatalog`.
- Produces: `CabinetPresenter(VisualElement root, SimBridge, UiText, PortraitCatalog?, ToastPresenter) { string? Selected; void Select(string? office); void Render(); }`; element names `cabinet-government`, `cabinet-coalition`, `seat-<office>`, `seat-<office>-holder`, `cand-<person>`, `appoint-<person>`, `cand-<person>-reason`, `cabinet-status`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/CabinetScreenTests.cs`:

```csharp
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CabinetScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>The historical game on 26 February 1950: the First Government, whose next turn ends before it falls.</summary>
    static SimBridge In1950()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g);
    }

    [Test]
    public void TheScreenListsTheGovernmentAndRefusesIllegalAppointmentsWithAReason()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null, new ToastPresenter(ui.Root, bridge, Text));
        Assert.AreEqual("First Government", ui.Q<Label>("cabinet-government").text);
        StringAssert.StartsWith("Coalition: 73 of 120 seats", ui.Q<Label>("cabinet-coalition").text);
        Assert.AreEqual("Minister of Finance: Eliezer Kaplan · Mapai · competence 8, loyalty 9", ui.Q<Label>("seat-finance_minister-holder").text);
        UiTestKit.Press(ui.Q<Button>("seat-finance_minister"));
        Assert.AreEqual("finance_minister", cabinet.Selected);
        Assert.IsFalse(ui.Q<Button>("appoint-zisling").enabledSelf);
        Assert.AreEqual("Aharon Zisling's party (Mapam) is not in the coalition", ui.Q<Label>("cand-zisling-reason").text);
        Assert.IsTrue(ui.Q<Button>("appoint-golda_meir").enabledSelf);
    }

    [Test]
    public void AppointingTakesEffectAtEndTurn()
    {
        using var ui = new UiTestKit();
        var bridge = In1950();
        var cabinet = new CabinetPresenter(ui.Root, bridge, Text, null, new ToastPresenter(ui.Root, bridge, Text));
        cabinet.Select("finance_minister");
        UiTestKit.Press(ui.Q<Button>("appoint-golda_meir"));
        Assert.AreEqual("Ordered: Golda Meir to Minister of Finance, at End Turn", ui.Q<Label>("cabinet-status").text);
        bridge.EndTurn();
        Assert.AreEqual("golda_meir", Cabinet.Holder(C, bridge.State, "finance_minister")!.Id);
        StringAssert.EndsWith("(your appointee)", ui.Q<Label>("seat-finance_minister-holder").text);
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "Golda Meir appointed Minister of Finance, replacing Eliezer Kaplan"));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'CabinetPresenter' could not be found`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/UI/CabinetPresenter.cs`:

```csharp
#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Cabinet screen: the government of the day, its coalition, and every office with its holder (portrait,
    /// party, competence and loyalty). Selecting an office lists every person with Appoint, or the reason they cannot be
    /// appointed (Cabinet.Check). Appointing queues the Appoint order for End Turn.</summary>
    public sealed class CabinetPresenter
    {
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly ToastPresenter toast;

        public CabinetPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits, ToastPresenter toast)
        {
            screen = root.Q<VisualElement>("screen-cabinet"); this.bridge = bridge; this.text = text; this.portraits = portraits; this.toast = toast;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>The office whose candidates are listed, or null.</summary>
        public string? Selected { get; private set; }

        public void Select(string? office) { Selected = office; Render(); }

        string Describe(Person p)
        {
            var c = bridge.Content; var party = Offices.PartyOf(c, p, bridge.State.Date);
            var partyName = party == "" ? text["cabinet.no_party"] : c.Parties.First(x => x.Id == party).Name;
            return text.Format("cabinet.person", p.Name, partyName, p.Competence, p.Loyalty);
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var gov = Offices.Government(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            screen.Q<Label>("cabinet-government").text = gov?.Name ?? "";
            screen.Q<Label>("cabinet-coalition").text = text.Format("cabinet.coalition", seats, size, s.CoalitionStability * 100);
            var list = screen.Q<ScrollView>("cabinet-seats");
            list.Clear();
            foreach (var seat in Cabinet.Lineup(c, s))
            {
                var office = seat.Office.Id;
                var row = new Button(() => Select(office)) { name = "seat-" + office };
                row.AddToClassList("seat");
                row.EnableInClassList("seat-selected", office == Selected);
                var portrait = new VisualElement();
                portrait.AddToClassList("seat-portrait");
                var tex = portraits?.Get(seat.Holder?.Id);
                if (tex != null) portrait.style.backgroundImage = new StyleBackground(tex);
                row.Add(portrait);
                var who = seat.Holder == null ? text["cabinet.vacant"] : Describe(seat.Holder) + (seat.Appointed ? text["cabinet.appointed"] : "");
                var label = new Label(text.Format("cabinet.seat", seat.Office.Title, who)) { name = "seat-" + office + "-holder" };
                label.EnableInClassList("appointed", seat.Appointed);
                row.Add(label);
                list.Add(row);
            }
            var pick = screen.Q<VisualElement>("cabinet-pick");
            var candidates = screen.Q<ScrollView>("cabinet-candidates");
            candidates.Clear();
            pick.style.display = Selected == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Selected != null)
            {
                var office = c.Offices.First(o => o.Id == Selected);
                screen.Q<Label>("cabinet-pick-title").text = text.Format("cabinet.pick", office.Title);
                foreach (var (person, reason) in Cabinet.Candidates(c, s, Selected))
                {
                    var p = person;
                    var row = new VisualElement { name = "cand-" + p.Id };
                    row.AddToClassList("candidate");
                    var appoint = new Button(() => Appoint(office.Id, p.Id)) { name = "appoint-" + p.Id, text = Describe(p) };
                    appoint.SetEnabled(reason == null);
                    row.Add(appoint);
                    if (reason != null) { var why = new Label(reason) { name = "cand-" + p.Id + "-reason" }; why.AddToClassList("reason"); row.Add(why); }
                    candidates.Add(row);
                }
            }
            var queued = bridge.Queued.OfType<Appoint>().Select(a =>
                text.Format("cabinet.queued", c.People.First(p => p.Id == a.Person).Name, c.Offices.First(o => o.Id == a.Office).Title));
            screen.Q<Label>("cabinet-status").text = string.Join("\n", queued);
        }

        void Appoint(string office, string person)
        {
            var refused = bridge.Queue(new Appoint(office, person));
            if (refused != null) toast.Refuse(refused); else toast.Clear();
        }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
        <ui:VisualElement name="minimap" class="panel minimap" />
        <ui:VisualElement name="screen-cabinet" class="panel screen" />
        <ui:VisualElement name="screen-budget" class="panel screen">
```

   with

```xml
        <ui:VisualElement name="minimap" class="panel minimap" />
        <ui:VisualElement name="screen-cabinet" class="panel screen">
            <ui:VisualElement class="screen-header"><ui:Label name="cabinet-government" class="title" /><ui:Label name="cabinet-coalition" class="muted" /></ui:VisualElement>
            <ui:VisualElement class="row grow">
                <ui:ScrollView name="cabinet-seats" class="grow" />
                <ui:VisualElement name="cabinet-pick" class="grow">
                    <ui:Label name="cabinet-pick-title" class="meter-name" />
                    <ui:ScrollView name="cabinet-candidates" class="grow" />
                </ui:VisualElement>
            </ui:VisualElement>
            <ui:Label name="cabinet-status" class="muted" />
        </ui:VisualElement>
        <ui:VisualElement name="screen-budget" class="panel screen">
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
why.situations: "Situations"
```

   with

```yaml
why.situations: "Situations"
cabinet.coalition: "Coalition: {0} of {1} seats · stability {2:0}"
cabinet.seat: "{0}: {1}"
cabinet.vacant: "(vacant)"
cabinet.person: "{0} · {1} · competence {2}, loyalty {3}"
cabinet.no_party: "no party"
cabinet.appointed: " (your appointee)"
cabinet.pick: "Appoint to {0}"
cabinet.queued: "Ordered: {0} to {1}, at End Turn"
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="117" passed="117" failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/UI/CabinetPresenter.cs \
  unity/Assets/Scripts/UI/CabinetPresenter.cs.meta \
  unity/Assets/Tests/EditMode/CabinetScreenTests.cs \
  unity/Assets/Tests/EditMode/CabinetScreenTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): Cabinet screen: lineup, candidates with reasons, Appoint

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 20: Info panel, map actions, minimap, placements and map clicks

*Track B.* Ruling 2: the info panel (bottom-left) shows hex or settlement information, not yields, and the minimap sits bottom-right. Ruling 4's map actions are placed from the info panel. A left click on the map (not on the UI) ray-casts the terrain and converts the hit to lon/lat and then to a hex with `HexPicker`, a C# port of `tools/hexgrid.py`'s projection and rounding (verified against the tool's table for all 24 settlements on their own hex). Placements are drawn as boxes with the editor-made marker material (no runtime `Shader.Find`), replacing what was drawn before. The minimap paints each pixel with the biome of its hex; its texture is created at runtime and destroyed by `Dispose`.

**Files:**
- Create: `unity/Assets/Scripts/Map/HexPicker.cs`, `unity/Assets/Scripts/Map/MapPointer.cs`, `unity/Assets/Scripts/Map/PlacementLayer.cs`, `unity/Assets/Scripts/UI/InfoPanelPresenter.cs`, `unity/Assets/Scripts/UI/MinimapPresenter.cs`, `unity/Assets/Tests/EditMode/MapActionUiTests.cs`
- Modify: `unity/Assets/Scripts/Map/GeoFrame.cs`, `unity/Assets/Scripts/Map/MapBuilder.cs`, `unity/Assets/Scripts/Map/MapScene.cs`, `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/Map/HexPicker.cs.meta`, `unity/Assets/Scripts/Map/MapPointer.cs.meta`, `unity/Assets/Scripts/Map/PlacementLayer.cs.meta`, `unity/Assets/Scripts/UI/InfoPanelPresenter.cs.meta`, `unity/Assets/Scripts/UI/MinimapPresenter.cs.meta`, `unity/Assets/Tests/EditMode/MapActionUiTests.cs.meta`

**Interfaces:**
- Consumes: `MapActions.Check/At/Kind`, `MapAction`, `Content.HexAt/SettlementHexes`, `Rules.MapActions`, `MapScene.Frame/Terrain/MarkerMaterial`, `MapBuilder.LoadHexes`, `SettlementPlacer.Parse`, Task 10's `UiPointer`, Task 11's Input System reference, Task 12's `MapScene.Build(MapInputs)`.
- Produces:
  - `HexPicker { const double KmPerDegLat = 110.57, KmPerDegLonEq = 111.32; static double KmPerDegLon(double lat0, double lat1); static (int q, int r) FromLonLat(double lon, double lat, HexBounds b, double hexSizeKm); }`.
  - `GeoFrame.ToLonLat(Vector3 world)`; `MapBuilder.BiomeColours : IReadOnlyDictionary<string, Color>`; `MapScene.Hexes : HexFile`.
  - `PlacementLayer : MonoBehaviour { void Render(IEnumerable<Placement>, Content, MapScene); }` (children named `<action>_<q>_<r>`).
  - `MapPointer : MonoBehaviour { MapScene? Map; Func<Vector2,bool>? IsOverUi; event Action<int,int>? HexClicked; static (int q, int r) Pick(MapScene, Vector3 world); }`.
  - `InfoPanelPresenter(VisualElement root, SimBridge, UiText, ToastPresenter, IReadOnlyDictionary<string,string> settlementNames) { (int q, int r)? Selected; void Show(int q, int r); void Render(); }`.
  - `MinimapPresenter(VisualElement root, HexFile) : IDisposable { const int Width = 128; static Color Sea; Texture2D Texture; event Action<double,double>? Clicked; static Texture2D Paint(HexFile, int width, int height); (double lon, double lat) ToLonLat(float u, float v); void SetView(GeoFrame, Vector3 lookAt, float distanceM); }`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/MapActionUiTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class MapActionUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly HexFile Hexes = MapBuilder.LoadHexes(Path.Combine(Paths.Content, "map", "hexes.json"));
    static readonly System.Collections.Generic.List<Settlement> Towns =
        SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void HexPickerAgreesWithTheToolsSettlementRule()
    {
        // tools/settlement_hexes.py moves a settlement off a water hex to its nearest land neighbour (Tiberias); every
        // other settlement must land on exactly the hex the C# port computes.
        foreach (var t in Towns)
        {
            var picked = HexPicker.FromLonLat(t.Lon, t.Lat, Hexes.bounds, Hexes.hex_size_km);
            var owner = C.SettlementHexes.Single(s => s.Id == t.Id);
            if (t.Id == "tiberias") { Assert.AreNotEqual((owner.Q, owner.R), picked); continue; }
            Assert.AreEqual((owner.Q, owner.R), picked, t.Id);
        }
    }

    [Test]
    public void GeoFrameRoundTripsLonLat()
    {
        var f = new GeoFrame(34.2, 35.9, 29.45, 33.35, 161530f, 431220f);
        var (lon, lat) = f.ToLonLat(f.ToWorld(34.78, 32.08, 0));
        Assert.AreEqual(34.78, lon, 1e-5); Assert.AreEqual(32.08, lat, 1e-5);
    }

    static SimBridge At(DateTime date)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < date) g.Step(HistoricalAI.Commands(C, g.State));
        return new SimBridge(g);
    }

    static InfoPanelPresenter Panel(UiTestKit ui, SimBridge bridge) =>
        new InfoPanelPresenter(ui.Root, bridge, Text, new ToastPresenter(ui.Root, bridge, Text), Towns.ToDictionary(t => t.Id, t => t.Name));

    [Test]
    public void BeforeTheMaabarotTheButtonsSayWhy()
    {
        using var ui = new UiTestKit();
        var info = Panel(ui, At(new DateTime(1949, 6, 1)));
        Assert.AreEqual("Click the map to inspect a hex", ui.Q<Label>("info-title").text);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text);
        StringAssert.StartsWith("Coastal plain · hex (", ui.Q<Label>("info-subtitle").text);
        Assert.IsFalse(ui.Q<Button>("build-camp").enabledSelf);
        Assert.AreEqual("Transit camps and housing projects can be placed from 1950-01-01", ui.Q<Label>("info-reason").text);
    }

    [Test]
    public void ATransitCampIsOrderedAndBuiltAtEndTurn()
    {
        using var ui = new UiTestKit();
        var bridge = At(new DateTime(1950, 8, 1));                        // after the ma'abarot decision (May 1950)
        var info = Panel(ui, bridge);
        var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
        info.Show(tlv.Q, tlv.R);
        Assert.IsTrue(ui.Q<Button>("build-camp").enabledSelf, ui.Q<Label>("info-reason").text);
        Assert.AreEqual("Place a transit camp (1.5m)", ui.Q<Button>("build-camp").text);
        ui.Press("build-camp");
        Assert.AreEqual("Ordered: A transit camp, at End Turn", ui.Q<Label>("info-placement").text);
        bridge.EndTurn();
        Assert.AreEqual(MapActions.TransitCamp, MapActions.At(bridge.State, tlv.Q, tlv.R)!.Action);
        Assert.AreEqual("A transit camp built here in August 1950", ui.Q<Label>("info-placement").text);
    }

    [Test]
    public void TheMinimapPaintsLandAndSeaNorthUp()
    {
        var tex = MinimapPresenter.Paint(Hexes, 128, 342);
        try
        {
            // RGB24 stores 8 bits per channel, so compare within one step.
            void Is(Color expected, double lon, double lat)
            {
                var c = tex.GetPixel((int)((lon - 34.2) / 1.7 * 128), (int)((lat - 29.45) / 3.9 * 342));
                Assert.AreEqual(expected.r, c.r, 1f / 255); Assert.AreEqual(expected.g, c.g, 1f / 255); Assert.AreEqual(expected.b, c.b, 1f / 255);
            }
            Is(MapBuilder.BiomeColours["coastal_plain"], 34.80, 32.10);   // just inland of Tel Aviv
            Is(MinimapPresenter.Sea, 34.30, 32.60);                       // the Mediterranean
            Is(MapBuilder.BiomeColours["desert"], 34.90, 30.50);          // the Negev
        }
        finally { UnityEngine.Object.DestroyImmediate(tex); }
    }

    [Test]
    public void PlacementsAreDrawnOnTheirHexes()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
            var map = new GameObject("MapScene").AddComponent<MapScene>();
            map.Configure(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexOverlay.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat"));
            map.Build();
            var layer = new GameObject("Placements").AddComponent<PlacementLayer>();
            var tlv = C.SettlementHexes.Single(s => s.Id == "tel_aviv");
            layer.Render(new[] { new Placement(MapActions.TransitCamp, tlv.Q, tlv.R, new DateTime(1950, 8, 26)) }, C, map);
            layer.Render(new[] { new Placement(MapActions.Housing, tlv.Q, tlv.R, new DateTime(1950, 8, 26)) }, C, map);
            Assert.AreEqual(1, layer.transform.childCount);                 // Render replaces what it drew
            var box = layer.transform.GetChild(0);
            Assert.AreEqual($"housing_{tlv.Q}_{tlv.R}", box.name);
            var hex = C.HexAt(tlv.Q, tlv.R)!;
            var expected = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
            Assert.AreEqual(expected.x, box.position.x, 1f); Assert.AreEqual(expected.z, box.position.z, 1f);
            Assert.AreEqual((tlv.Q, tlv.R), MapPointer.Pick(map, box.position));
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0103: The name 'HexPicker' does not exist in the current context`.

- [ ] **Step 3: Implement**

Replace the whole of `unity/Assets/Scripts/Map/GeoFrame.cs` with:

```csharp
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Equirectangular lon/lat -> world metres. x = east, z = north, origin = south-west corner.</summary>
    public readonly struct GeoFrame
    {
        public readonly double Lon0, Lon1, Lat0, Lat1; public readonly float WidthM, LengthM;
        public GeoFrame(double lon0, double lon1, double lat0, double lat1, float widthM, float lengthM)
        { Lon0 = lon0; Lon1 = lon1; Lat0 = lat0; Lat1 = lat1; WidthM = widthM; LengthM = lengthM; }
        public Vector3 ToWorld(double lon, double lat, float y) =>
            new Vector3((float)((lon - Lon0) / (Lon1 - Lon0) * WidthM), y, (float)((lat - Lat0) / (Lat1 - Lat0) * LengthM));

        /// <summary>The inverse of ToWorld on the ground plane (y is ignored).</summary>
        public (double lon, double lat) ToLonLat(Vector3 world) =>
            (Lon0 + world.x / WidthM * (Lon1 - Lon0), Lat0 + world.z / LengthM * (Lat1 - Lat0));
    }
}
```

Create `unity/Assets/Scripts/Map/HexPicker.cs`:

```csharp
#nullable enable
using System;

namespace Statecraft.Game.Map
{
    /// <summary>lon/lat -> axial hex, the same projection and rounding as tools/hexgrid.py (lonlat_to_km + point_to_hex):
    /// x km east of lon0, y km south of lat1, pointy-top hexes, cube rounding. Used for map clicks and the minimap; the sim
    /// never needs it (settlement hexes come precomputed from tools/settlement_hexes.py).</summary>
    public static class HexPicker
    {
        public const double KmPerDegLat = 110.57, KmPerDegLonEq = 111.32;   // tools/geo.py

        public static double KmPerDegLon(double lat0, double lat1) => KmPerDegLonEq * Math.Cos((lat0 + lat1) / 2 * Math.PI / 180);

        public static (int q, int r) FromLonLat(double lon, double lat, HexBounds b, double hexSizeKm)
        {
            var x = (lon - b.lon0) * KmPerDegLon(b.lat0, b.lat1);
            var y = (b.lat1 - lat) * KmPerDegLat;
            var qf = (Math.Sqrt(3) / 3 * x - y / 3) / hexSizeKm;
            var rf = (2.0 / 3 * y) / hexSizeKm;
            var sf = -qf - rf;
            double q = Math.Round(qf, MidpointRounding.ToEven), r = Math.Round(rf, MidpointRounding.ToEven), s = Math.Round(sf, MidpointRounding.ToEven);
            double dq = Math.Abs(q - qf), dr = Math.Abs(r - rf), ds = Math.Abs(s - sf);
            if (dq > dr && dq > ds) q = -r - s;
            else if (dr > ds) r = -q - s;
            return ((int)q, (int)r);
        }
    }
}
```

In `unity/Assets/Scripts/Map/MapBuilder.cs`:

1. Replace

```csharp
        public const float TerrainPixelError = 1f;

```

   with

```csharp
        public const float TerrainPixelError = 1f;

        /// <summary>Biome colours of the terrain layers (the minimap uses the same ones).</summary>
        public static IReadOnlyDictionary<string, Color> BiomeColours => Biome;

```

Create `unity/Assets/Scripts/Map/MapPointer.cs`:

```csharp
#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>Left click on the map (not on the UI) selects the hex under the pointer: a ray from the camera hits the
    /// terrain's collider, the hit becomes lon/lat (GeoFrame) and then a hex (HexPicker).</summary>
    public sealed class MapPointer : MonoBehaviour
    {
        public MapScene? Map;
        public Func<Vector2, bool>? IsOverUi;
        public event Action<int, int>? HexClicked;

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || Map == null || Map.Terrain == null || !mouse.leftButton.wasPressedThisFrame) return;
            var pos = mouse.position.ReadValue();
            if (IsOverUi != null && IsOverUi(pos)) return;
            var cam = Camera.main;
            if (cam == null || !Physics.Raycast(cam.ScreenPointToRay(pos), out var hit, 1_000_000f)) return;
            var hex = Pick(Map, hit.point);
            HexClicked?.Invoke(hex.q, hex.r);
        }

        public static (int q, int r) Pick(MapScene map, Vector3 world)
        {
            var (lon, lat) = map.Frame.ToLonLat(world);
            return HexPicker.FromLonLat(lon, lat, map.Hexes.bounds, map.Hexes.hex_size_km);
        }
    }
}
```

In `unity/Assets/Scripts/Map/MapScene.cs`:

1. Replace

```csharp
        public GeoFrame Frame { get; private set; }
        public Terrain Terrain { get; private set; }
```

   with

```csharp
        public GeoFrame Frame { get; private set; }
        /// <summary>hexes.json as the last Build read it (bounds, hex size, biomes).</summary>
        public HexFile Hexes { get; private set; }
        public Terrain Terrain { get; private set; }
```

2. Replace

```csharp
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16, MetaPath);
            var terrainGo = MapBuilder.Build(meta, heights, MapBuilder.LoadHexes(inputs.HexesJson, HexesPath), terrainMaterial, overlayMaterial);
            terrainGo.transform.SetParent(transform, true);
```

   with

```csharp
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16, MetaPath);
            Hexes = MapBuilder.LoadHexes(inputs.HexesJson, HexesPath);
            var terrainGo = MapBuilder.Build(meta, heights, Hexes, terrainMaterial, overlayMaterial);
            terrainGo.transform.SetParent(transform, true);
```

Create `unity/Assets/Scripts/Map/PlacementLayer.cs`:

```csharp
#nullable enable
using System.Collections.Generic;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Draws the player's transit camps and housing projects as marker boxes on their hex centres (placeholder art
    /// like the M2 settlement markers; per-era prefabs are M4). Uses the editor-made marker material with a per-kind
    /// colour through a MaterialPropertyBlock (no runtime Shader.Find). Render replaces what it drew before.</summary>
    public sealed class PlacementLayer : MonoBehaviour
    {
        static readonly Dictionary<string, (Color colour, float size)> Look = new Dictionary<string, (Color, float)>
        {
            [MapActions.TransitCamp] = (new Color(0.62f, 0.58f, 0.50f), 700f),
            [MapActions.Housing] = (new Color(0.95f, 0.94f, 0.90f), 900f),
        };
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public void Render(IEnumerable<Placement> placements, Content content, MapScene map)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var old = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { old.transform.SetParent(null); Destroy(old); } else DestroyImmediate(old);
            }
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var block = new MaterialPropertyBlock();
            foreach (var p in placements)
            {
                var hex = content.HexAt(p.Q, p.R);
                if (hex == null || !Look.TryGetValue(p.Action, out var look)) continue;
                var at = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                at.y = map.Terrain.SampleHeight(at) + map.Terrain.transform.position.y;
                var box = new GameObject($"{p.Action}_{p.Q}_{p.R}");
                box.transform.SetParent(transform, false);
                box.AddComponent<MeshFilter>().sharedMesh = cube;
                var r = box.AddComponent<MeshRenderer>();
                r.sharedMaterial = map.MarkerMaterial;
                block.SetColor(BaseColor, look.colour); r.SetPropertyBlock(block);
                box.transform.position = at + Vector3.up * look.size / 2;
                box.transform.localScale = new Vector3(look.size * 1.6f, look.size, look.size);
            }
        }
    }
}
```

Create `unity/Assets/Scripts/UI/InfoPanelPresenter.cs`:

```csharp
#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel (bottom-left): the selected hex's land, height and the settlements it owns, what the player
    /// built there, and the two map actions (transit camp, housing project) with the reason when one is not possible
    /// (MapActions.Check). Tile yields are deliberately not shown (owner decision, M3 rulings 2).</summary>
    public sealed class InfoPanelPresenter
    {
        readonly VisualElement panel; readonly SimBridge bridge; readonly UiText text; readonly ToastPresenter toast;
        readonly IReadOnlyDictionary<string, string> settlementNames;

        public InfoPanelPresenter(VisualElement root, SimBridge bridge, UiText text, ToastPresenter toast, IReadOnlyDictionary<string, string> settlementNames)
        {
            panel = root.Q<VisualElement>("info-panel"); this.bridge = bridge; this.text = text; this.toast = toast;
            this.settlementNames = settlementNames;
            panel.Q<Button>("build-camp").clicked += () => Build(MapActions.TransitCamp);
            panel.Q<Button>("build-housing").clicked += () => Build(MapActions.Housing);
            bridge.Changed += Render;
            Render();
        }

        public (int q, int r)? Selected { get; private set; }

        public void Show(int q, int r) { Selected = (q, r); Render(); }

        void Build(string action)
        {
            if (Selected == null) return;
            var refused = bridge.Queue(new MapAction(action, Selected.Value.q, Selected.Value.r));
            if (refused != null) toast.Refuse(refused); else toast.Clear();
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var camp = panel.Q<Button>("build-camp"); var housing = panel.Q<Button>("build-housing");
            if (Selected == null)
            {
                panel.Q<Label>("info-title").text = text["info.hint"];
                foreach (var name in new[] { "info-subtitle", "info-settlements", "info-placement", "info-reason" }) panel.Q<Label>(name).text = "";
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                return;
            }
            var (q, r) = Selected.Value;
            var hex = c.HexAt(q, r);
            var owned = c.SettlementHexes.Where(x => x.Q == q && x.R == r).Select(x => settlementNames.TryGetValue(x.Id, out var n) ? n : x.Id).ToList();
            panel.Q<Label>("info-title").text = owned.Count > 0 ? owned[0] : hex == null ? text["info.off_map"] : text["info.biome." + hex.Biome];
            panel.Q<Label>("info-subtitle").text = hex == null ? "" : text.Format("info.hex", text["info.biome." + hex.Biome], q, r, hex.ElevM);
            panel.Q<Label>("info-settlements").text = owned.Count > 0 ? text.Format("info.owned", string.Join(", ", owned)) : "";
            var here = MapActions.At(s, q, r);
            var queued = bridge.QueuedLike(new MapAction(MapActions.Housing, q, r)) as MapAction;
            panel.Q<Label>("info-placement").text = queued != null ? text.Format("info.queued", text["info." + queued.Action])
                : here != null ? text.Format("info.built", text["info." + here.Action], here.Date) : "";
            var campWhy = MapActions.Check(c, s, MapActions.TransitCamp, q, r);
            var housingWhy = MapActions.Check(c, s, MapActions.Housing, q, r);
            camp.style.display = DisplayStyle.Flex; housing.style.display = DisplayStyle.Flex;
            camp.SetEnabled(campWhy == null); housing.SetEnabled(housingWhy == null);
            camp.text = text.Format("info.build_camp", c.Rules.MapActions.TryGetValue(MapActions.TransitCamp, out var cr) ? cr.Cost : 0);
            housing.text = text.Format("info.build_housing", c.Rules.MapActions.TryGetValue(MapActions.Housing, out var hr) ? hr.Cost : 0);
            panel.Q<Label>("info-reason").text = campWhy ?? housingWhy ?? "";
        }
    }
}
```

Create `unity/Assets/Scripts/UI/MinimapPresenter.cs`:

```csharp
#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Statecraft.Game.UI
{
    /// <summary>The minimap (bottom-right): every hex in its biome colour, north up, the camera's view as a frame, and a
    /// click to move the camera there. The texture is made at runtime and destroyed by Dispose.</summary>
    public sealed class MinimapPresenter : IDisposable
    {
        public const int Width = 128;
        public static readonly Color Sea = MapBuilder.BiomeColours["sea"];
        readonly VisualElement image, view; readonly HexFile hexes;
        readonly double widthKm, lengthKm;

        public Texture2D Texture { get; }
        /// <summary>Raised with the lon/lat the player clicked.</summary>
        public event Action<double, double>? Clicked;

        public MinimapPresenter(VisualElement root, HexFile hexes)
        {
            this.hexes = hexes;
            var b = hexes.bounds;
            widthKm = (b.lon1 - b.lon0) * HexPicker.KmPerDegLon(b.lat0, b.lat1);
            lengthKm = (b.lat1 - b.lat0) * HexPicker.KmPerDegLat;
            Texture = Paint(hexes, Width, (int)Math.Round(Width * lengthKm / widthKm));
            var box = root.Q<VisualElement>("minimap");
            image = box.Q<VisualElement>("minimap-image");
            view = box.Q<VisualElement>("minimap-view");
            image.style.backgroundImage = new StyleBackground(Texture);
            image.RegisterCallback<PointerDownEvent>(e =>
            {
                var (lon, lat) = ToLonLat(e.localPosition.x / image.layout.width, e.localPosition.y / image.layout.height);
                Clicked?.Invoke(lon, lat);
            });
        }

        /// <summary>One pixel per ~1.3 km: each pixel takes the biome of the hex its centre falls in (sea off the grid).
        /// Row 0 of a Texture2D is the bottom, so it is the south edge.</summary>
        public static Texture2D Paint(HexFile hexes, int width, int height)
        {
            var biome = new System.Collections.Generic.Dictionary<(int, int), string>();
            foreach (var h in hexes.hexes) biome[(h.q, h.r)] = h.biome;
            var b = hexes.bounds;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false) { name = "Minimap", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    double lon = b.lon0 + (x + 0.5) / width * (b.lon1 - b.lon0), lat = b.lat0 + (y + 0.5) / height * (b.lat1 - b.lat0);
                    var hex = HexPicker.FromLonLat(lon, lat, b, hexes.hex_size_km);
                    pixels[y * width + x] = biome.TryGetValue(hex, out var name) ? MapBuilder.BiomeColours[name] : Sea;
                }
            tex.SetPixels(pixels); tex.Apply();
            return tex;
        }

        /// <summary>(u, v) in the image, top-left origin, 0..1 -> lon/lat.</summary>
        public (double lon, double lat) ToLonLat(float u, float v)
        {
            var b = hexes.bounds;
            return (b.lon0 + Mathf.Clamp01(u) * (b.lon1 - b.lon0), b.lat1 - Mathf.Clamp01(v) * (b.lat1 - b.lat0));
        }

        /// <summary>Frames the area the camera looks at: about one and a half view distances wide, centred on lookAt.</summary>
        public void SetView(GeoFrame frame, Vector3 lookAt, float distanceM)
        {
            var (lon, lat) = frame.ToLonLat(lookAt);
            var b = hexes.bounds;
            float u = (float)((lon - b.lon0) / (b.lon1 - b.lon0)), v = (float)((b.lat1 - lat) / (b.lat1 - b.lat0));
            float w = (float)(1.5 * distanceM / 1000 / widthKm), h = (float)(1.0 * distanceM / 1000 / lengthKm);
            view.style.left = Length.Percent((u - w / 2) * 100); view.style.top = Length.Percent((v - h / 2) * 100);
            view.style.width = Length.Percent(w * 100); view.style.height = Length.Percent(h * 100);
        }

        public void Dispose() { if (Texture != null) Object.DestroyImmediate(Texture); }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
        </ui:VisualElement>
        <ui:VisualElement name="info-panel" class="panel info-panel" />
        <ui:VisualElement name="minimap" class="panel minimap" />
        <ui:VisualElement name="screen-cabinet" class="panel screen">
```

   with

```xml
        </ui:VisualElement>
        <ui:VisualElement name="info-panel" class="panel info-panel">
            <ui:Label name="info-title" class="title" />
            <ui:Label name="info-subtitle" class="muted" />
            <ui:Label name="info-settlements" />
            <ui:Label name="info-placement" class="muted" />
            <ui:VisualElement class="row">
                <ui:Button name="build-camp" />
                <ui:Button name="build-housing" />
            </ui:VisualElement>
            <ui:Label name="info-reason" class="reason" />
        </ui:VisualElement>
        <ui:VisualElement name="minimap" class="panel minimap">
            <ui:VisualElement name="minimap-image" class="minimap-image">
                <ui:VisualElement name="minimap-view" class="minimap-view" picking-mode="Ignore" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="screen-cabinet" class="panel screen">
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
cabinet.queued: "Ordered: {0} to {1}, at End Turn"
```

   with

```yaml
cabinet.queued: "Ordered: {0} to {1}, at End Turn"
info.hint: "Click the map to inspect a hex"
info.off_map: "Off the map"
info.hex: "{0} · hex ({1}, {2}) · {3:N0} m"
info.owned: "Settlements: {0}"
info.built: "{0} built here in {1:MMMM yyyy}"
info.queued: "Ordered: {0}, at End Turn"
info.transit_camp: "A transit camp"
info.housing: "A housing project"
info.build_camp: "Place a transit camp ({0:0.0}m)"
info.build_housing: "Build housing ({0:0.0}m)"
info.biome.sea: "Sea"
info.biome.lake: "Lake"
info.biome.coastal_plain: "Coastal plain"
info.biome.hills: "Hills"
info.biome.mountains: "Mountains"
info.biome.valley: "Valley"
info.biome.desert: "Desert"
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="123" passed="123" failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/Map/GeoFrame.cs \
  unity/Assets/Scripts/Map/HexPicker.cs \
  unity/Assets/Scripts/Map/HexPicker.cs.meta \
  unity/Assets/Scripts/Map/MapBuilder.cs \
  unity/Assets/Scripts/Map/MapPointer.cs \
  unity/Assets/Scripts/Map/MapPointer.cs.meta \
  unity/Assets/Scripts/Map/MapScene.cs \
  unity/Assets/Scripts/Map/PlacementLayer.cs \
  unity/Assets/Scripts/Map/PlacementLayer.cs.meta \
  unity/Assets/Scripts/UI/InfoPanelPresenter.cs \
  unity/Assets/Scripts/UI/InfoPanelPresenter.cs.meta \
  unity/Assets/Scripts/UI/MinimapPresenter.cs \
  unity/Assets/Scripts/UI/MinimapPresenter.cs.meta \
  unity/Assets/Tests/EditMode/MapActionUiTests.cs \
  unity/Assets/Tests/EditMode/MapActionUiTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui,map): info panel with transit camps and housing, minimap, placement markers, map clicks

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 21: Autosave, continue, and New Game

*Track B.* Ruling 4 and spec §7: autosave every turn, and load that replays to the same state hash. On WebGL the autosave goes to the fixed folder `/idbfs/statecraft-saves`, not `Application.persistentDataPath`, which hashes the page URL and so changes with every itch.io upload (ruling 6, tech notes §3); the template's `autoSyncPersistentDataPath` persists the file when it is closed. Writes go to a `.tmp` file first. The next start continues the autosave; any failure while reading or replaying it (not only `SaveException`) sets it aside (`autosave.bad.txt`) with a message, and a new game starts. New Game asks for a second press (the browser frame shows no confirm dialogs).

**Files:**
- Create: `unity/Assets/Scripts/SaveStore.cs`, `unity/Assets/Scripts/UI/GameStart.cs`, `unity/Assets/Scripts/UI/NewGamePresenter.cs`, `unity/Assets/Tests/EditMode/SaveUiTests.cs`
- Modify: `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/strings.en.yaml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/SaveStore.cs.meta`, `unity/Assets/Scripts/UI/GameStart.cs.meta`, `unity/Assets/Scripts/UI/NewGamePresenter.cs.meta`, `unity/Assets/Tests/EditMode/SaveUiTests.cs.meta`

**Interfaces:**
- Consumes: `GameSession.New/Load`, `SaveGame.Write`, `SaveException`, `SimBridge.TurnEnded/Replace`, `ToastPresenter.Show`.
- Produces:
  - `Statecraft.Game.SaveStore { const string WebDirectory = "/idbfs/statecraft-saves", AutosaveName = "autosave.txt"; SaveStore(string directory); static SaveStore ForPlatform(); string Directory; string AutosavePath; void WriteAutosave(string); string? ReadAutosave(); void SetAsideAutosave(); void DeleteAutosave(); }`.
  - `GameStart { static ulong NewSeed(); static GameSession Begin(Content, SaveStore, Func<ulong> newSeed, List<string> notes, bool fresh = false); static void Autosave(SimBridge, SaveStore); }`.
  - `NewGamePresenter(VisualElement root, SimBridge, SaveStore, ToastPresenter, UiText, Func<ulong> seed) { bool Armed; }`; button `new-game` in the top bar.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/SaveUiTests.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

public class SaveUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SaveStore TempStore() => new SaveStore(Path.Combine(Path.GetTempPath(), "statecraft-saves-" + System.Guid.NewGuid().ToString("N")));

    [Test]
    public void EveryTurnIsAutosavedAndTheNextStartContinuesIt()
    {
        var store = TempStore();
        var notes = new List<string>();
        var bridge = new SimBridge(GameStart.Begin(C, store, () => 77UL, notes));
        GameStart.Autosave(bridge, store);
        bridge.Queue(new SetMobilization(1.0));
        bridge.EndTurn(); bridge.EndTurn();
        Assert.IsTrue(File.Exists(store.AutosavePath));
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
        var resumed = GameStart.Begin(C, store, () => 1UL, notes);
        Assert.IsEmpty(notes);
        Assert.AreEqual(bridge.State.Hash(), resumed.State.Hash());
        Assert.AreEqual(2, resumed.State.Turn);
    }

    [Test]
    public void AnUnreadableAutosaveIsSetAsideAndANewGameStarts()
    {
        var store = TempStore();
        store.WriteAutosave("statecraft-save 9\ncontent x\nseed 1\nmode Historical\nend x\n");
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 5UL, notes);
        Assert.AreEqual(0, session.State.Turn);
        StringAssert.StartsWith("Your saved game could not be loaded (line 1: save schema version 9 is newer", notes[0]);
        Assert.IsFalse(File.Exists(store.AutosavePath));
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    [Test]
    public void ASaveThatThrowsWhileReplayingIsSetAsideToo()
    {
        var store = TempStore();
        // Parses (a valid header), but Open mode makes the replay throw NotSupportedException, not SaveException.
        var good = GameSession.New(C, 9, Mode.Historical).Save.Write();
        store.WriteAutosave(good.Replace("mode Historical", "mode Open"));
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 6UL, notes);
        Assert.AreEqual(0, session.State.Turn);
        StringAssert.StartsWith("Your saved game could not be loaded (Open mode arrives with milestone M4.)", notes[0]);
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    [Test]
    public void NewGameNeedsASecondPress()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical));
        GameStart.Autosave(bridge, store);
        bridge.EndTurn();
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("new-game");
        Assert.IsTrue(newGame.Armed);
        Assert.AreEqual("Press New Game again to abandon this game and start over.", ui.Q<UnityEngine.UIElements.Label>("toast").text);
        Assert.AreEqual(1, bridge.State.Turn);
        ui.Press("new-game");
        Assert.AreEqual(0, bridge.State.Turn);
        Assert.IsFalse(File.Exists(store.AutosavePath));
    }

    [Test]
    public void TheWebSaveFolderDoesNotDependOnThePageUrl() =>
        Assert.AreEqual("/idbfs/statecraft-saves", SaveStore.WebDirectory);
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'SaveStore' could not be found`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/SaveStore.cs`:

```csharp
#nullable enable
using System.IO;
using UnityEngine;

namespace Statecraft.Game
{
    /// <summary>Where the autosave lives (spec section 7: autosave every turn). On WebGL it is a fixed folder under the
    /// /idbfs IndexedDB mount, not Application.persistentDataPath, because that path hashes the page's URL and itch.io
    /// serves every upload from a new URL, which would hide old saves (tech notes section 3). The template sets
    /// autoSyncPersistentDataPath, so closing the file persists it. Writes go to a .tmp file first, then replace.</summary>
    public sealed class SaveStore
    {
        public const string WebDirectory = "/idbfs/statecraft-saves";
        public const string AutosaveName = "autosave.txt";

        public string Directory { get; }
        public SaveStore(string directory) { Directory = directory; }

        public static SaveStore ForPlatform() => new SaveStore(Application.platform == RuntimePlatform.WebGLPlayer
            ? WebDirectory : Path.Combine(Application.persistentDataPath, "saves"));

        public string AutosavePath => Path.Combine(Directory, AutosaveName);

        public void WriteAutosave(string text)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var tmp = AutosavePath + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(AutosavePath)) File.Delete(AutosavePath);
            File.Move(tmp, AutosavePath);
        }

        public string? ReadAutosave() => File.Exists(AutosavePath) ? File.ReadAllText(AutosavePath) : null;

        /// <summary>Moves an unreadable autosave aside (autosave.bad.txt) so it is kept for a bug report.</summary>
        public void SetAsideAutosave()
        {
            if (!File.Exists(AutosavePath)) return;
            var bad = Path.Combine(Directory, "autosave.bad.txt");
            if (File.Exists(bad)) File.Delete(bad);
            File.Move(AutosavePath, bad);
        }

        public void DeleteAutosave() { if (File.Exists(AutosavePath)) File.Delete(AutosavePath); }
    }
}
```

Create `unity/Assets/Scripts/UI/GameStart.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Starting a session: continue the autosave when there is one (replayed to the same state hash), else a new
    /// Historical game. Anything the player should know (a content-change warning, an unreadable save) goes to notes.
    /// No failure while reading or replaying the autosave stops the game from starting: the file is set aside.</summary>
    public static class GameStart
    {
        public static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;

        public static GameSession Begin(Content c, SaveStore store, Func<ulong> newSeed, List<string> notes, bool fresh = false)
        {
            var text = fresh ? null : store.ReadAutosave();
            if (text != null)
            {
                try
                {
                    var loaded = GameSession.Load(c, text, out var warnings);
                    notes.AddRange(warnings);
                    return loaded;
                }
                catch (Exception e)   // any failure (a bad save, a replay that throws): never block the start
                {
                    store.SetAsideAutosave();
                    notes.Add($"Your saved game could not be loaded ({e.Message}). A new game has started.");
                }
            }
            return GameSession.New(c, newSeed(), Mode.Historical);
        }

        /// <summary>Writes the save after every turn.</summary>
        public static void Autosave(SimBridge bridge, SaveStore store) =>
            bridge.TurnEnded += _ => store.WriteAutosave(bridge.Session.Save.Write());
    }
}
```

Create `unity/Assets/Scripts/UI/NewGamePresenter.cs`:

```csharp
#nullable enable
using System;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>New Game asks for a second press (no browser confirm dialogs in WebGL), then deletes the autosave and
    /// starts a new Historical game. Ending a turn in between cancels the request.</summary>
    public sealed class NewGamePresenter
    {
        readonly SimBridge bridge; readonly SaveStore store; readonly ToastPresenter toast; readonly UiText text; readonly Func<ulong> seed;

        public NewGamePresenter(VisualElement root, SimBridge bridge, SaveStore store, ToastPresenter toast, UiText text, Func<ulong> seed)
        {
            this.bridge = bridge; this.store = store; this.toast = toast; this.text = text; this.seed = seed;
            root.Q<Button>("new-game").clicked += Press;
            bridge.TurnEnded += _ => Armed = false;
        }

        public bool Armed { get; private set; }

        void Press()
        {
            if (!Armed) { Armed = true; toast.Show(text["newgame.confirm"]); return; }
            Armed = false;
            store.DeleteAutosave();
            bridge.Replace(GameSession.New(bridge.Content, seed(), Mode.Historical));
            toast.Clear();
        }
    }
}
```

In `unity/Assets/UI/Game.uxml`:

1. Replace

```xml
                <ui:Button name="nav-why" text="Why?" />
            </ui:VisualElement>
```

   with

```xml
                <ui:Button name="nav-why" text="Why?" />
                <ui:Button name="new-game" text="New Game" />
            </ui:VisualElement>
```

In `unity/Assets/UI/strings.en.yaml`:

1. Replace

```yaml
info.biome.desert: "Desert"
```

   with

```yaml
info.biome.desert: "Desert"
newgame.confirm: "Press New Game again to abandon this game and start over."
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="128" passed="128" failed="0"`.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/SaveStore.cs \
  unity/Assets/Scripts/SaveStore.cs.meta \
  unity/Assets/Scripts/UI/GameStart.cs \
  unity/Assets/Scripts/UI/GameStart.cs.meta \
  unity/Assets/Scripts/UI/NewGamePresenter.cs \
  unity/Assets/Scripts/UI/NewGamePresenter.cs.meta \
  unity/Assets/Tests/EditMode/SaveUiTests.cs \
  unity/Assets/Tests/EditMode/SaveUiTests.cs.meta \
  unity/Assets/UI/Game.uxml \
  unity/Assets/UI/strings.en.yaml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(ui): autosave every turn to a URL-independent folder, continue on start, New Game

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Phase 2, Track C: players (Task 22)

### Task 22: The golden hash in WebGL and macOS IL2CPP players

*Track C.* Ruling 5 and the M0–M2 Deferred row "golden hash asserted in a macOS IL2CPP player and a WebGL player". `GameBoot` runs both golden playthroughs when asked (URL query parameter `golden` on WebGL, matched as `[?&]golden` so a path word never turns it on; `-golden` on desktop, which quits with exit code 0 or 1) and logs `GOLDEN OK <year> <hash>` or `GOLDEN FAIL ...`. `tools/web_check.py` serves a build on `http://localhost` (a secure context, so Brotli works) with the right `Content-Encoding` and MIME types, opens it in Chrome for Testing (`--headless=new`), and reads the console from Chrome's stderr; Unity's `Debug.Log` adds a stack trace on following lines, so only each message's first line is matched. See Controller question 1 for why this replaces a separate scene and Playwright. Verified results: WebGL `GOLDEN OK` for both runs; macOS IL2CPP `-golden` exits 0 without `-ffp-contract=off`.

**Files:**
- Create: `tools/tests/test_web_check.py`, `tools/web_check.py`, `unity/Assets/Scripts/Boot/GoldenCheck.cs`, `unity/Assets/Scripts/Boot/UrlFlags.cs`, `unity/Assets/Tests/EditMode/GoldenCheckTests.cs`
- Modify: `docs/determinism.md`, `tools/README.md`, `unity/Assets/Scripts/Boot/GameBoot.cs`, `unity/Assets/link.xml`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/Boot/GoldenCheck.cs.meta`, `unity/Assets/Scripts/Boot/UrlFlags.cs.meta`, `unity/Assets/Tests/EditMode/GoldenCheckTests.cs.meta`

**Interfaces:**
- Consumes: `GoldenRun.Play/Hash1950/Hash1952/Until1950/Until1952` (Tasks 1, 5), `GameBoot` (Task 12), `Build.Web/Mac` (Task 13), `Content.Hash` (Task 9).
- Produces:
  - `Statecraft.Game.Boot.GoldenCheck { static bool Requested(); static (bool ok, List<string> lines) Run(Content); }`.
  - `Statecraft.Game.Boot.UrlFlags { static bool Has(string? url, string name); static int Int(string? url, string name); }` (Tasks 23 and 24 use it for `smoke` and `profile`).
  - `tools/web_check.py BUILD [--query Q] [--expect TEXT]... [--fail REGEX] [--timeout S] [--swiftshader]`: exit 0 when every expected text appears in the console, 1 on a failure line (default `GOLDEN FAIL|SMOKE FAIL|BOOT FAIL`) or timeout; functions `serve(root) -> ThreadingHTTPServer`, `CONSOLE` regex.
  - `link.xml` preserves `Statecraft.Game.Boot.*` (JsonUtility's manifest type).

- [ ] **Step 1: Write the failing tests**

Create `tools/tests/test_web_check.py`:

```python
"""tools/web_check.py: the console-line parser and the static server's headers (no browser needed)."""
import gzip
import urllib.request

import web_check as wc


def test_console_lines_are_read_from_their_first_line():
    one = '[1010/025003.375619:INFO:CONSOLE:2] "GOLDEN OK 1950 abc", source: http://localhost:1/index.html (2)'
    assert wc.CONSOLE.search(one).group(1) == "GOLDEN OK 1950 abc"
    # Unity's Debug.Log adds a stack trace, so the message continues on the next stderr lines.
    first = '[1010/030000.1:INFO:CONSOLE:9] "SMOKE OK 5 turns 1949-01-01'
    assert wc.CONSOLE.search(first).group(1) == "SMOKE OK 5 turns 1949-01-01"


def test_precompressed_files_get_content_encoding_and_wasm_its_type(tmp_path):
    (tmp_path / "Build").mkdir()
    (tmp_path / "Build" / "game.wasm.br").write_bytes(b"\x00")
    (tmp_path / "Build" / "game.data.gz").write_bytes(gzip.compress(b"x"))
    (tmp_path / "index.html").write_text("<html></html>")
    server = wc.serve(tmp_path)
    try:
        base = f"http://localhost:{server.server_address[1]}"
        r = urllib.request.urlopen(base + "/Build/game.wasm.br")
        assert (r.headers["Content-Encoding"], r.headers["Content-Type"]) == ("br", "application/wasm")
        r = urllib.request.Request(base + "/Build/game.data.gz", headers={"Accept-Encoding": "identity"})
        assert urllib.request.urlopen(r).headers["Content-Encoding"] == "gzip"
        assert urllib.request.urlopen(base + "/index.html").headers["Content-Encoding"] is None
    finally:
        server.shutdown()
```

Create `unity/Assets/Tests/EditMode/GoldenCheckTests.cs`:

```csharp
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Sim;

public class GoldenCheckTests
{
    [Test]
    public void TheCheckReportsBothGoldenRunsAsOkInTheEditor()
    {
        var (ok, lines) = GoldenCheck.Run(ContentLoader.Load(Paths.Content));
        Assert.IsTrue(ok, string.Join("\n", lines));
        Assert.AreEqual(new[] { "GOLDEN OK 1950 " + GoldenRun.Hash1950, "GOLDEN OK 1952 " + GoldenRun.Hash1952 }, lines);
    }

    [TestCase("http://localhost:8000/index.html?golden", true)]
    [TestCase("http://localhost:8000/index.html?smoke=5&golden", true)]
    [TestCase("https://html.itch.zone/html/123/golden-project/index.html", false)]
    [TestCase("http://localhost:8000/index.html?goldenrod=1", false)]
    [TestCase("http://localhost:8000/index.html", false)]
    public void TheGoldenFlagIsAQueryParameterNotAPathWord(string url, bool expected) =>
        Assert.AreEqual(expected, UrlFlags.Has(url, "golden"));

    [Test]
    public void IntegerFlagsReadTheirValue()
    {
        Assert.AreEqual(5, UrlFlags.Int("http://x/index.html?golden&smoke=5", "smoke"));
        Assert.AreEqual(0, UrlFlags.Int("http://x/smoke=5/index.html", "smoke"));
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
```
Expected: collection error: `ModuleNotFoundError: No module named 'web_check'`.

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0103: The name 'GoldenCheck' does not exist in the current context`.

- [ ] **Step 3: Implement**

In `docs/determinism.md`:

1. Replace

```markdown

Verified on .NET and Unity editor Mono. IL2CPP and WebGL: pending (M3 Task 22 checks both players).

```

   with

````markdown

Verified on .NET, Unity editor Mono, a macOS IL2CPP player (Apple clang, no `-ffp-contract=off` needed) and a WebGL
IL2CPP player in Chrome (M3 Task 22). Re-check with:

```
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log
uv run tools/web_check.py unity/Builds/Web-disabled --query golden --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952"
"$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile unity-build-mac.log
"unity/Builds/Mac/Statecraft.app/Contents/MacOS/Statecraft Israel" -batchmode -nographics -golden -logFile unity-golden-mac.log; echo exit=$?
```

````

In `tools/README.md`:

1. Replace

```markdown
  `hexgrid.settlement_hex` rule. The sim reads it, so it needs no projection maths.
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
```

   with

```markdown
  `hexgrid.settlement_hex` rule. The sim reads it, so it needs no projection maths.
- `web_check.py` serves a WebGL build on http://localhost and opens it in headless Chrome for Testing (Playwright's
  cache); it passes when the console shows the expected lines (`GOLDEN OK ...`, `SMOKE OK ...`).
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
```

2. Replace

```markdown

Expected: `96 passed`. Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

```

   with

```markdown

Expected: `98 passed`. Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

```

Create `tools/web_check.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Serve a Unity WebGL build on http://localhost and open it in headless Chrome; pass when the console shows every
expected line, fail on a failure line or a timeout. Uses the Playwright-cached chrome-headless-shell (no npm needed):
console messages reach stderr with --enable-logging=stderr --v=1 as  INFO:CONSOLE... "text", source: ...

Usage (from the repo root):
  uv run tools/web_check.py unity/Builds/Web-disabled --query "golden" --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952"
Options: --query Q (page URL query, without '?'), --expect TEXT (repeatable), --fail REGEX (default: GOLDEN FAIL|SMOKE FAIL|BOOT FAIL),
--timeout SECONDS (default 300), --swiftshader (software WebGL when there is no GPU).
Pre-compressed builds: *.br and *.gz files are served with Content-Encoding, *.wasm(.br/.gz) as application/wasm.
Brotli needs a secure context; http://localhost is one."""
import argparse
import functools
import http.server
import os
import re
import subprocess
import sys
import threading
import time
from pathlib import Path

# A console message's first line; Unity's Debug.Log adds a stack trace on later lines, so the closing quote and
# ", source:" may be far below. Only the first line is matched.
CONSOLE = re.compile(r'INFO:CONSOLE[^\]]*\] "(.*?)(?:", source: .*)?$')
CHROME_GLOB = "ms-playwright/chromium-*/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        path = self.path.split("?")[0]
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        base = re.sub(r"\.(br|gz)$", "", str(path))
        if base.endswith(".wasm"):
            return "application/wasm"
        if base.endswith(".js"):
            return "application/javascript"
        if base.endswith((".data", ".r16")):
            return "application/octet-stream"
        return super().guess_type(base)

    def log_message(self, fmt, *args):
        if os.environ.get("WEB_CHECK_VERBOSE"):
            print("http:", fmt % args)


def find_chrome() -> Path:
    found = sorted(Path.home().joinpath("Library", "Caches").glob(CHROME_GLOB))
    if not found:
        sys.exit("Chrome for Testing not found under ~/Library/Caches/ms-playwright (npx playwright install chromium)")
    return found[-1]


def serve(root: Path) -> http.server.ThreadingHTTPServer:
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(Handler, directory=str(root)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("build", type=Path)
    ap.add_argument("--query", default="")
    ap.add_argument("--expect", action="append", default=[])
    ap.add_argument("--fail", default=r"GOLDEN FAIL|SMOKE FAIL|BOOT FAIL")
    ap.add_argument("--timeout", type=float, default=300)
    ap.add_argument("--swiftshader", action="store_true")
    a = ap.parse_args()
    if not (a.build / "index.html").exists():
        sys.exit(f"{a.build}/index.html not found")
    server = serve(a.build)
    url = f"http://localhost:{server.server_address[1]}/index.html" + (f"?{a.query}" if a.query else "")
    flags = ["--headless=new", "--enable-logging=stderr", "--v=1", "--no-first-run", "--no-default-browser-check",
             "--autoplay-policy=no-user-gesture-required", "--window-size=1920,1080",
             "--disable-frame-rate-limit", "--disable-gpu-vsync",
             f"--user-data-dir={a.build.resolve()}/../.web-check-profile"]
    if a.swiftshader:
        flags += ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]
    proc = subprocess.Popen([str(find_chrome()), *flags, url], stderr=subprocess.PIPE, stdout=subprocess.DEVNULL, text=True, errors="replace")
    pending, fail, deadline = list(a.expect), re.compile(a.fail), time.time() + a.timeout
    lines: list[str] = []
    reader = threading.Thread(target=lambda: lines.extend(iter(proc.stderr.readline, "")), daemon=True)
    reader.start()
    seen = 0
    try:
        while time.time() < deadline:
            while seen < len(lines):
                m = CONSOLE.search(lines[seen]); seen += 1
                if not m:
                    continue
                text = m.group(1)
                print("console:", text)
                if fail.search(text):
                    print(f"FAIL: {text}")
                    return 1
                pending = [p for p in pending if p not in text]
            if not pending:
                print("PASS: saw " + ", ".join(a.expect))
                return 0
            time.sleep(0.2)
        print("TIMEOUT: never saw " + ", ".join(pending))
        return 1
    finally:
        proc.terminate()
        server.shutdown()


if __name__ == "__main__":
    sys.exit(main())
```

Replace the whole of `unity/Assets/Scripts/Boot/GameBoot.cs` with:

```csharp
#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The first thing Map.unity runs: loads the game inputs (fetched on WebGL, read from disk elsewhere), runs
    /// the golden check when asked (GoldenCheck), builds the map, then raises Ready for the game (GameRoot) to start.
    /// A failure is logged ("BOOT FAIL ...") and kept in Error.</summary>
    public sealed class GameBoot : MonoBehaviour
    {
        [SerializeField] MapScene map = null!;

        public GameInputs? Inputs { get; private set; }
        public string? Error { get; private set; }
        public event Action<GameInputs>? Ready;

        public void Configure(MapScene mapScene) => map = mapScene;

        async void Start()
        {
            try
            {
                Inputs = Application.platform == RuntimePlatform.WebGLPlayer
                    ? await GameInputs.FetchAsync(GameInputs.StreamingUrl())
                    : GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
                if (GoldenCheck.Requested())
                {
                    var (ok, lines) = GoldenCheck.Run(Inputs.Content);
                    foreach (var line in lines) Debug.Log(line);
                    // Desktop players run the check headless (-batchmode -nographics) and report through the exit code.
                    if (Application.platform != RuntimePlatform.WebGLPlayer) { Application.Quit(ok ? 0 : 1); return; }
                }
                map.Build(Inputs.Map);
                Debug.Log($"BOOT OK content {Inputs.Content.Hash}");
                Ready?.Invoke(Inputs);
            }
            catch (Exception e)
            {
                Error = e.Message;
                Debug.LogError("BOOT FAIL " + e.Message);
                Debug.LogException(e);
            }
        }
    }
}
```

Create `unity/Assets/Scripts/Boot/GoldenCheck.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>Runs both golden playthroughs in whatever runtime this is (IL2CPP in players) and reports
    /// "GOLDEN OK &lt;year&gt; &lt;hash&gt;" or "GOLDEN FAIL &lt;year&gt; &lt;hash&gt; expected &lt;hash&gt;" (docs/determinism.md).
    /// Asked for by the URL query "golden" on WebGL, or the command-line flag -golden on desktop.</summary>
    public static class GoldenCheck
    {
        public static bool Requested() => Application.platform == RuntimePlatform.WebGLPlayer
            ? UrlFlags.Has(Application.absoluteURL, "golden")
            : Environment.GetCommandLineArgs().Contains("-golden");

        public static (bool ok, List<string> lines) Run(Content c)
        {
            var lines = new List<string>();
            bool ok = true;
            foreach (var (until, expected) in new[] { (GoldenRun.Until1950, GoldenRun.Hash1950), (GoldenRun.Until1952, GoldenRun.Hash1952) })
            {
                var hash = GoldenRun.Play(c, until).Hash();
                var year = until.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (hash == expected) lines.Add($"GOLDEN OK {year} {hash}");
                else { ok = false; lines.Add($"GOLDEN FAIL {year} {hash} expected {expected}"); }
            }
            return (ok, lines);
        }
    }
}
```

Create `unity/Assets/Scripts/Boot/UrlFlags.cs`:

```csharp
#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;

namespace Statecraft.Game.Boot
{
    /// <summary>Flags in the WebGL page URL's query: "?golden", "&amp;smoke=5", "?profile". A flag must start a query
    /// parameter and end at a word boundary, so a path such as ".../golden-project/..." never turns one on.</summary>
    public static class UrlFlags
    {
        public static bool Has(string? url, string name) => Regex.IsMatch(url ?? "", @"[?&]" + Regex.Escape(name) + @"\b");

        /// <summary>The flag's integer value ("smoke=5" -> 5), or 0 when absent.</summary>
        public static int Int(string? url, string name)
        {
            var m = Regex.Match(url ?? "", @"[?&]" + Regex.Escape(name) + @"=(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        }
    }
}
```

Replace the whole of `unity/Assets/link.xml` with:

```xml
<linker>
  <assembly fullname="YamlDotNet" preserve="all" />
  <assembly fullname="Statecraft.Sim" preserve="all" />
  <assembly fullname="Statecraft.Game"><type fullname="Statecraft.Game.Map.*" preserve="all"/><type fullname="Statecraft.Game.UI.*" preserve="all"/><type fullname="Statecraft.Game.Boot.*" preserve="all"/></assembly>
</linker>
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
```
Expected: `98 passed`.

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="108" passed="108" failed="0"` on this branch (merge point 1 + 7: one golden check and six flag cases).

- [ ] **Step 5: Check the golden hash in a WebGL player**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log; grep "BUILD " unity-build-web.log
uv run tools/web_check.py unity/Builds/Web-disabled --query golden --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952" --expect "BOOT OK"
```
Expected: `BUILD Succeeded WebGL ...`, then `console: GOLDEN OK 1950 404b181105e6a2085ec26d10cff4e9d0ac6f8802a1e20b3a91e8fe554e1d5c88`, `console: GOLDEN OK 1952 36640b59df6535c7f9e9aec94bc41ed62e65505544a91cbbe131bb7b5ca86bea`, `console: BOOT OK content <64 hex>`, `PASS: saw ...`, exit 0. If the page reports `BOOT FAIL Your browser does not support WebGL`, the machine has no GPU for headless Chrome: add `--swiftshader`. A `GOLDEN FAIL` line is a determinism bug, not a reason to change a constant.

- [ ] **Step 6: Check the golden hash in a macOS IL2CPP player**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile unity-build-mac.log; grep "BUILD " unity-build-mac.log
"unity/Builds/Mac/Statecraft.app/Contents/MacOS/Statecraft Israel" -batchmode -nographics -golden -logFile unity-golden-mac.log; echo exit=$?
grep GOLDEN unity-golden-mac.log
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset   # build caches only: this task stages no settings
```
Expected: `BUILD Succeeded StandaloneOSX ...` (about 2.5 minutes), `exit=0`, and both `GOLDEN OK` lines with the hashes above. If the Mac player prints `GOLDEN FAIL`, rebuild with `-ffp-contract-off` (Apple clang may fuse multiply-adds, tech notes §4.2), rerun, and record the outcome in `docs/determinism.md`.

- [ ] **Step 7: Commit**

```bash
git add docs/determinism.md \
  tools/README.md \
  tools/tests/test_web_check.py \
  tools/web_check.py \
  unity/Assets/Scripts/Boot/GameBoot.cs \
  unity/Assets/Scripts/Boot/GoldenCheck.cs \
  unity/Assets/Scripts/Boot/GoldenCheck.cs.meta \
  unity/Assets/Scripts/Boot/UrlFlags.cs \
  unity/Assets/Scripts/Boot/UrlFlags.cs.meta \
  unity/Assets/Tests/EditMode/GoldenCheckTests.cs \
  unity/Assets/Tests/EditMode/GoldenCheckTests.cs.meta \
  unity/Assets/link.xml
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(unity,tools): golden check in players; web_check.py reads a WebGL build's console in headless Chrome

Verified: WebGL and macOS IL2CPP players reproduce both golden hashes.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Merge point 2

The controller merges Track B (Tasks 14–21) and Track C (Task 22) into `m3` and runs every suite:

Run:
```bash
dotnet test sim/Statecraft.sln
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `Passed:   391`, `98 passed`, `exit=0` and `total="135" passed="135" failed="0"`.

## Phase 3, Track D: integration (Tasks 23–24)

### Task 23: Scene wiring, GameRoot, and the scripted 5-turn smoke test

*Track D.* Everything meets in `Map.unity`: `GameBoot` loads the inputs and builds the map; `GameRoot` then continues the autosave or starts a new game, creates every presenter, autosaves each turn, and wires the camera (`IsOverUi`, map bounds), map clicks, placements and the minimap. The scene is regenerated by `MapCapture.MakeScene` (no hand-edited scene YAML). Ruling 6's "scripted 5-turn smoke test" runs in the real player: with `?smoke=5` (or `-smoke 5`) `GameRoot` plays five turns by pressing the event card's first option and End Turn through `NavigationSubmitEvent`, starting a fresh game with the golden seed, and logs `SMOKE OK 5 turns <date> <hash>`. The smoke loop is a static coroutine, so an edit-mode test also drives it from a turn with a pending event and proves the card's option button was pressed (amendment A2).

**Files:**
- Create: `unity/Assets/Scripts/UI/GameRoot.cs`, `unity/Assets/Tests/EditMode/GameSceneTests.cs`
- Modify: `unity/Assets/Editor/MapCapture.cs`, `unity/Assets/Map.unity`
- Generated by a command in this task (commit the output): `unity/Assets/Map.unity`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/UI/GameRoot.cs.meta`, `unity/Assets/Tests/EditMode/GameSceneTests.cs.meta`

**Interfaces:**
- Consumes: every presenter (Tasks 14–21), `GameBoot.Ready`, `GameInputs`, `CameraController`, `MapPointer`, `PlacementLayer`, `MinimapPresenter`, `UiPointer`, `SaveStore`, `GameStart`, `UiAssets.Create`, `PortraitImport.CatalogPath`.
- Produces:
  - `GameRoot : MonoBehaviour { SimBridge? Bridge; void Configure(GameBoot, UIDocument, TextAsset strings, PortraitCatalog, MapScene, PlacementLayer, MapPointer, CameraController); void Begin(GameInputs); static int SmokeTurns(); static IEnumerator Smoke(VisualElement root, SimBridge, EventCardPresenter, int turns, Action<string> log); }`; logs `GAME OK <date> turn <n>`, `SMOKE turn <n> <date> refused <k>`, `SMOKE OK <n> turns <date> <hash>` or `SMOKE FAIL <message>`.
  - `Map.unity`: Main Camera (+`CameraRig`, `CameraController`), Directional Light, `MapScene`, `Boot` (`GameBoot`), `Placements`, `MapPointer`, `UI` (`UIDocument` with `StatecraftPanel.asset` and `Game.uxml`, `GameRoot`).

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/GameSceneTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class GameSceneTests
{
    [TearDown] public void DiscardScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [Test]
    public void MapUnityIsTheOnlyBuildSceneAndIsFullyWired()
    {
        Assert.AreEqual(new[] { "Assets/Map.unity" }, System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path));
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var root = Object.FindFirstObjectByType<GameRoot>();
        Assert.IsNotNull(root, "GameRoot");
        var so = new SerializedObject(root);
        foreach (var field in new[] { "boot", "document", "strings", "portraits", "map", "placements", "pointer", "cameraController" })
            Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
        Assert.IsNotNull(Object.FindFirstObjectByType<GameBoot>());
        Assert.IsNotNull(Camera.main.GetComponent<CameraController>());
        Assert.IsNull(GameObject.Find("EventSystem"), "UI Toolkit reads the Input System directly; an EventSystem would take over");
    }

    [Test]
    public void TheGameStartsInTheSceneOnceTheInputsAreLoaded()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Begin(inputs);
        Assert.IsNotNull(root.Bridge);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        Assert.AreEqual(root.Bridge!.State.Date.Year.ToString(), doc.rootVisualElement.Q<Label>("year").text);
        Assert.AreEqual("Click the map to inspect a hex", doc.rootVisualElement.Q<Label>("info-title").text);
    }

    [Test]
    public void TheSmokeRunAnswersAPendingEventThroughTheCard()
    {
        using var ui = new UiTestKit();
        var c = ContentLoader.Load(Paths.Content);
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var bridge = new SimBridge(g);
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var toast = new ToastPresenter(ui.Root, bridge, text);
        new HudPresenter(ui.Root, bridge, text);                         // wires the End Turn button
        var card = new EventCardPresenter(ui.Root, bridge, text, null, toast);
        var log = new List<string>();
        var run = GameRoot.Smoke(ui.Root, bridge, card, 1, log.Add);
        while (run.MoveNext()) { }
        StringAssert.StartsWith("SMOKE OK 1 turns", log[log.Count - 1]);
        Assert.IsTrue(bridge.Session.Last!.Why.Any(w => w.Message == "chosen: Sign the armistice"), "the card's option was pressed, not delegated");
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'GameRoot' could not be found`.

- [ ] **Step 3: Implement**

In `unity/Assets/Editor/MapCapture.cs`:

1. Replace

```csharp
using Statecraft.Game.EditorTools;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

```

   with

```csharp
using Statecraft.Game.EditorTools;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

```

2. Replace

```csharp

    /// <summary>Creates and saves Assets/Map.unity: default camera and light, a MapScene wired to the material assets, and
    /// the GameBoot that loads the inputs and builds the map in play mode and in players.</summary>
    public static void MakeScene()
    {
        MapMaterials.Create();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(MapMaterials.TerrainPath), Load(MapMaterials.OverlayPath), Load(MapMaterials.MarkerPath));
        new GameObject("Boot").AddComponent<Statecraft.Game.Boot.GameBoot>().Configure(map);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }
```

   with

```csharp

    /// <summary>Creates and saves Assets/Map.unity, the boot scene: the default camera (with CameraRig and
    /// CameraController) and light, a MapScene wired to the material assets, the GameBoot that loads the inputs and builds
    /// the map, the placement layer, the map pointer, and the UI document with GameRoot wired to all of them.</summary>
    public static void MakeScene()
    {
        MapMaterials.Create();
        UiAssets.Create();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var controller = Camera.main.gameObject.AddComponent<CameraController>();   // adds the CameraRig it requires
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(MapMaterials.TerrainPath), Load(MapMaterials.OverlayPath), Load(MapMaterials.MarkerPath));
        var boot = new GameObject("Boot").AddComponent<GameBoot>();
        boot.Configure(map);
        var placements = new GameObject("Placements").AddComponent<PlacementLayer>();
        var pointer = new GameObject("MapPointer").AddComponent<MapPointer>();
        var ui = new GameObject("UI");
        var doc = ui.AddComponent<UIDocument>();
        doc.panelSettings = Asset<PanelSettings>(UiPaths.Panel);
        doc.visualTreeAsset = Asset<VisualTreeAsset>(UiPaths.GameUxml);
        ui.AddComponent<GameRoot>().Configure(boot, doc, Asset<TextAsset>(UiPaths.Strings),
            Asset<PortraitCatalog>(PortraitImport.CatalogPath), map, placements, pointer, controller);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static T Asset<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) throw new FileNotFoundException($"{typeof(T).Name} asset missing", path);
        return a;
    }
```

Create `unity/Assets/Scripts/UI/GameRoot.cs`:

```csharp
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Starts the game once GameBoot has loaded the inputs and built the map: continues the autosave or starts a
    /// new game, creates every screen, autosaves each turn, and wires the camera, map clicks, placements and minimap.
    /// With "smoke" in the page URL (WebGL, e.g. ?smoke=5) or -smoke N (desktop) it plays N turns through the UI's own
    /// buttons and logs "SMOKE OK ..." (or "SMOKE FAIL ...").</summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] GameBoot boot = null!;
        [SerializeField] UIDocument document = null!;
        [SerializeField] TextAsset strings = null!;
        [SerializeField] PortraitCatalog portraits = null!;
        [SerializeField] MapScene map = null!;
        [SerializeField] PlacementLayer placements = null!;
        [SerializeField] MapPointer pointer = null!;
        [SerializeField] CameraController cameraController = null!;

        MinimapPresenter? minimap;
        EventCardPresenter? card;

        public SimBridge? Bridge { get; private set; }

        public void Configure(GameBoot boot, UIDocument document, TextAsset strings, PortraitCatalog portraits, MapScene map,
            PlacementLayer placements, MapPointer pointer, CameraController cameraController)
        {
            this.boot = boot; this.document = document; this.strings = strings; this.portraits = portraits; this.map = map;
            this.placements = placements; this.pointer = pointer; this.cameraController = cameraController;
        }

        void Awake() { if (boot != null) boot.Ready += Begin; }

        /// <summary>Smoke-test turn count from the URL (?smoke=N) or the command line (-smoke N); 0 when not asked.</summary>
        public static int SmokeTurns()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return UrlFlags.Int(Application.absoluteURL, "smoke");
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-smoke");
            return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var n) ? n : 0;
        }

        public void Begin(GameInputs inputs)
        {
            var text = UiText.Parse(strings.text);
            var store = SaveStore.ForPlatform();
            var notes = new List<string>();
            var smoke = SmokeTurns();
            // A smoke run is reproducible: a fresh game with the golden seed, never the player's autosave.
            var session = GameStart.Begin(inputs.Content, store, smoke > 0 ? () => GoldenRun.Seed : GameStart.NewSeed, notes, fresh: smoke > 0);
            var bridge = Bridge = new SimBridge(session);
            var root = document.rootVisualElement;
            var toast = new ToastPresenter(root, bridge, text);
            new HudPresenter(root, bridge, text);
            new ScreenNav(root);
            var eventCard = card = new EventCardPresenter(root, bridge, text, portraits, toast);
            new DefencePresenter(root, bridge, text, toast);
            new BudgetPresenter(root, bridge, text, toast);
            new WhyPresenter(root, bridge, text);
            new CabinetPresenter(root, bridge, text, portraits, toast);
            var names = SettlementPlacer.Parse(inputs.Map.SettlementsYaml).ToDictionary(s => s.Id, s => s.Name);
            var info = new InfoPanelPresenter(root, bridge, text, toast, names);
            new NewGamePresenter(root, bridge, store, toast, text, GameStart.NewSeed);
            if (smoke == 0) GameStart.Autosave(bridge, store);

            bool OverUi(Vector2 p) => UiPointer.IsOverUi(root.panel, p, Screen.height);
            var size = map.Terrain.terrainData.size;
            cameraController.IsOverUi = OverUi;
            cameraController.Bounds = new Rect(0, 0, size.x, size.z);
            pointer.Map = map;
            pointer.IsOverUi = OverUi;
            pointer.HexClicked += info.Show;
            minimap = new MinimapPresenter(root, map.Hexes);
            var rig = cameraController.GetComponent<CameraRig>();
            minimap.Clicked += (lon, lat) => { rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Apply(); };
            bridge.Changed += () => placements.Render(bridge.State.Placements, bridge.Content, map);
            placements.Render(bridge.State.Placements, bridge.Content, map);
            if (notes.Count > 0) toast.Show(notes);
            Debug.Log($"GAME OK {bridge.State.Date:yyyy-MM-dd} turn {bridge.State.Turn}");
            if (smoke > 0) StartCoroutine(Smoke(root, bridge, eventCard, smoke, Debug.Log));
        }

        void Update()
        {
            if (minimap == null) return;
            var rig = cameraController.GetComponent<CameraRig>();
            minimap.SetView(map.Frame, rig.LookAt, rig.Distance);
        }

        void OnDestroy() => minimap?.Dispose();

        /// <summary>Plays turns the way a player does: answers each event with the card's first option button, then
        /// presses End Turn. Logs one line per turn through <paramref name="log"/>, then "SMOKE OK ..." (or "SMOKE FAIL ...").
        /// Static, so an edit-mode test can drive it step by step.</summary>
        public static IEnumerator Smoke(VisualElement root, SimBridge bridge, EventCardPresenter card, int turns, Action<string> log)
        {
            for (int t = 0; t < turns; t++)
            {
                yield return null;
                for (int guard = 0; guard < 10 && card.Shown != null; guard++)
                {
                    var option = root.Q<VisualElement>("event-options").Q<Button>();
                    if (option == null) break;
                    if (!Press(option, log)) yield break;
                    yield return null;
                }
                if (!Press(root.Q<Button>("end-turn"), log)) yield break;
                var s = bridge.State;
                log($"SMOKE turn {s.Turn} {s.Date:yyyy-MM-dd} refused {bridge.Session.Last!.Rejected.Count}");
            }
            log($"SMOKE OK {turns} turns {bridge.State.Date:yyyy-MM-dd} {bridge.State.Hash()}");
        }

        static bool Press(Button b, Action<string> log)
        {
            try
            {
                using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); }
                return true;
            }
            catch (Exception e) { log("SMOKE FAIL " + e.Message); return false; }
        }
    }
}
```

- [ ] **Step 4: Regenerate the scene**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -executeMethod MapCapture.MakeScene -logFile unity-scene.log; echo exit=$?
grep -c "Statecraft.Game.UI.GameRoot" unity/Assets/Map.unity
```
Expected: `exit=0` and `1`.

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="138" passed="138" failed="0"`.

- [ ] **Step 6: Play five turns in the WebGL player**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log; grep "BUILD " unity-build-web.log
uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&smoke=5" --expect "GOLDEN OK 1952" --expect "GAME OK" --expect "SMOKE OK 5 turns"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset   # build caches only: this task stages no settings
```
Expected: `console: GAME OK 1948-05-15 turn 0`, five `SMOKE turn` lines ending `SMOKE turn 5 1948-06-19 refused 0`, `console: SMOKE OK 5 turns 1948-06-19 85cf952f0d5db0501c749b8b0c61658488760431d09ddb8b35dc5245794ff1d3`, `PASS`. (The smoke game is reproducible: fresh, golden seed; the five opening weekly turns have no events, so End Turn is pressed each time.)

- [ ] **Step 7: Play it yourself in Chrome (manual check)**

Serve the build with `python3 -m http.server 8000 --directory unity/Builds/Web-disabled` and open `http://localhost:8000/` in Chrome. Check by eye against `promo/final/03-gameplay-1936.png`: navy panels with gold borders and serif type; the top bar's six indicators; WASD/arrow keys pan, the scroll wheel zooms, Q/E orbit, and none of them move the camera while the pointer is over a panel; clicking the map fills the info panel; each top-bar button opens its screen; End Turn advances a week while at war. Reload the page: the game continues from the autosave. Note anything wrong in the commit message of the fix, not here.

- [ ] **Step 8: Commit**

```bash
git add unity/Assets/Editor/MapCapture.cs \
  unity/Assets/Map.unity \
  unity/Assets/Scripts/UI/GameRoot.cs \
  unity/Assets/Scripts/UI/GameRoot.cs.meta \
  unity/Assets/Tests/EditMode/GameSceneTests.cs \
  unity/Assets/Tests/EditMode/GameSceneTests.cs.meta
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(unity): wire the boot scene (map, UI, camera, saves) and a scripted 5-turn smoke test

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 24: Web profiling, the release build, local verification and the owner's itch.io steps

*Track D.* Ruling 5: profile `heightmapPixelError` on WebGL (M2 left it at 1 so the hex overlay stays draped, measured only on macOS). Ruling 6: a release-ready WebGL folder (Brotli, for itch.io) verified locally with the golden check and the 5-turn smoke; the itch.io upload as a documented **OWNER STEP**; and the save-path fix with its two-upload test plan. `PixelErrorProfile` measures 300 frames at pixel error 1 and 5 from the diorama camera (`web_check.py` passes `--disable-frame-rate-limit --disable-gpu-vsync`, so frame times are not capped at the display rate). Verified on this Mac: `pixelError=1 avg 4.70 ms, p95 6.00 ms`; `pixelError=5 avg 1.79 ms, p95 4.00 ms`; 1 stays (well under the 33 ms of 30 fps).

**Files:**
- Create: `docs/m3-release.md`, `unity/Assets/Scripts/Map/PixelErrorProfile.cs`, `unity/Assets/Tests/EditMode/PixelErrorProfileTests.cs`
- Modify: `docs/dev-setup.md`, `unity/Assets/Scripts/UI/GameRoot.cs`
- Unity `.meta` files Unity creates on import (stage them with their assets): `unity/Assets/Scripts/Map/PixelErrorProfile.cs.meta`, `unity/Assets/Tests/EditMode/PixelErrorProfileTests.cs.meta`

**Interfaces:**
- Consumes: `GameRoot.Begin` (Task 23), `MapBuilder.TerrainPixelError`, `Build.Web`, `web_check.py`.
- Produces: `PixelErrorProfile { const int WarmupFrames = 60, MeasuredFrames = 300; static bool Requested(); static string Line(float pixelError, List<float> frameMs); static IEnumerator Run(Terrain); }` (`?profile` / `-profile`); logs `PROFILE pixelError=<e> frames=<n> avg_ms=<a> p95_ms=<p>` and `PROFILE done`. Docs `docs/m3-release.md`; `docs/dev-setup.md` gains the build and check commands.

- [ ] **Step 1: Write the failing test**

Create `unity/Assets/Tests/EditMode/PixelErrorProfileTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map;

public class PixelErrorProfileTests
{
    [Test]
    public void TheProfileLineReportsAverageAndP95()
    {
        var frames = new List<float>();
        for (int i = 1; i <= 100; i++) frames.Add(i);
        Assert.AreEqual("PROFILE pixelError=1 frames=100 avg_ms=50.50 p95_ms=95.00", PixelErrorProfile.Line(1f, frames));
    }

    [TestCase("http://localhost:8000/index.html?profile", true)]
    [TestCase("https://html.itch.zone/html/123/profile-pics/index.html", false)]
    public void TheProfileFlagIsAQueryParameter(string url, bool expected) =>
        Assert.AreEqual(expected, Statecraft.Game.Boot.UrlFlags.Has(url, "profile"));
}
```

- [ ] **Step 2: Run it and confirm it fails**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=1`; `error CS0103: The name 'PixelErrorProfile' does not exist in the current context`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/Map/PixelErrorProfile.cs`:

```csharp
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Measures frame time with the terrain's heightmapPixelError at 1 (M2's value, which keeps the hex overlay
    /// draped visibly) and at Unity's default 5, from the diorama camera over central Israel, then restores the M2 value.
    /// Logs "PROFILE pixelError=&lt;e&gt; frames=&lt;n&gt; avg_ms=&lt;a&gt; p95_ms=&lt;p&gt;" per setting and "PROFILE done".
    /// Asked for by "profile" in the WebGL page URL or -profile on desktop.</summary>
    public static class PixelErrorProfile
    {
        public const int WarmupFrames = 60, MeasuredFrames = 300;

        public static bool Requested() => Application.platform == RuntimePlatform.WebGLPlayer
            ? Boot.UrlFlags.Has(Application.absoluteURL, "profile")
            : Environment.GetCommandLineArgs().Contains("-profile");

        public static string Line(float pixelError, List<float> frameMs)
        {
            var sorted = frameMs.OrderBy(x => x).ToList();
            var p95 = sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * 0.95) - 1)];
            return string.Format(CultureInfo.InvariantCulture, "PROFILE pixelError={0:0} frames={1} avg_ms={2:0.00} p95_ms={3:0.00}",
                pixelError, sorted.Count, sorted.Average(), p95);
        }

        public static IEnumerator Run(Terrain terrain)
        {
            foreach (var pixelError in new[] { 1f, 5f })
            {
                terrain.heightmapPixelError = pixelError;
                for (int i = 0; i < WarmupFrames; i++) yield return null;
                var times = new List<float>(MeasuredFrames);
                for (int i = 0; i < MeasuredFrames; i++) { yield return null; times.Add(Time.unscaledDeltaTime * 1000f); }
                Debug.Log(Line(pixelError, times));
            }
            terrain.heightmapPixelError = MapBuilder.TerrainPixelError;
            Debug.Log("PROFILE done");
        }
    }
}
```

In `unity/Assets/Scripts/UI/GameRoot.cs`:

1. Replace

```csharp
            if (smoke > 0) StartCoroutine(Smoke(root, bridge, eventCard, smoke, Debug.Log));
        }
```

   with

```csharp
            if (smoke > 0) StartCoroutine(Smoke(root, bridge, eventCard, smoke, Debug.Log));
            if (PixelErrorProfile.Requested()) StartCoroutine(PixelErrorProfile.Run(map.Terrain));
        }
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
```
Expected: `exit=0` and `total="141" passed="141" failed="0"`.

- [ ] **Step 5: Profile the terrain on WebGL**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log
uv run tools/web_check.py unity/Builds/Web-disabled --query "profile" --expect "PROFILE done"
```
Expected: two `PROFILE pixelError=...` lines and `PROFILE done`. Copy both lines into `docs/m3-release.md` (next step) in place of the verified values if yours differ. Decision rule: keep `TerrainPixelError = 1` while its p95 stays under 33 ms; otherwise raise a controller question (the hex overlay sinks into valleys above 1, M2 ruling K2).

- [ ] **Step 6: Build the release folder and verify it locally**

Run:
```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression brotli -logFile unity-build-web-br.log; grep "BUILD " unity-build-web-br.log
find unity/Builds/Web-brotli -type f | wc -l; du -sh unity/Builds/Web-brotli
uv run tools/web_check.py unity/Builds/Web-brotli --query "golden&smoke=5" --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952" --expect "SMOKE OK 5 turns"
(cd unity/Builds/Web-brotli && rm -f ../statecraft-m3-web.zip && zip -qr ../statecraft-m3-web.zip . && ls -la ../statecraft-m3-web.zip)
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset   # build caches only: this task stages no settings
```
Expected: `BUILD Succeeded WebGL -> Builds/Web-brotli: 22.3 MB` (about); `36` files (itch.io allows 1,000); `23M`; `PASS: saw GOLDEN OK 1950, GOLDEN OK 1952, SMOKE OK 5 turns`; the zip's size. `web_check.py` serves `*.br` with `Content-Encoding: br` over `http://localhost`, as itch.io does over HTTPS.

- [ ] **Step 7: Write the release notes, the owner steps and the build commands**

If your profile lines differ from the verified ones, use yours in the table.

Create `docs/m3-release.md`:

```markdown
# M3 release: the browser build

## What ships
- **Folder:** `unity/Builds/Web-brotli/` (Brotli, no decompression fallback), zipped as `unity/Builds/statecraft-m3-web.zip`
  with `index.html` at the zip root. Both are build output and are not committed.
- **Built by:** `"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression brotli -logFile unity-build-web-br.log`
- **Verified locally by:** `uv run tools/web_check.py unity/Builds/Web-brotli --query "golden&smoke=5" --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952" --expect "SMOKE OK 5 turns"`
  (both golden hashes in the WebGL IL2CPP player, then five turns played through the UI's own buttons).
- **Size:** about 23 MB (22.3 MB reported by the build) in 36 files. `StreamingAssets/map/heightmap.r16` (8 MB) is served as it is, outside Unity's
  compression (Deferred: M5).

## WebGL terrain profile
| `heightmapPixelError` | avg ms | p95 ms |
|---|---|---|
| 1 (shipped; keeps the hex overlay draped) | 4.70 | 6.00 |
| 5 (Unity's default) | 1.79 | 4.00 |

Measured 2026-10-10 with `?profile` in Chrome for Testing (Playwright cache, chromium-1228, `--headless=new
--disable-frame-rate-limit --disable-gpu-vsync`) on the development Mac (arm64): 300 frames each after 60 warm-up frames,
from the diorama camera over central Israel. Decision: keep 1.

## OWNER STEP: upload to itch.io
This needs the owner's itch.io account and is never automated.
1. Sign in at https://itch.io. Dashboard → **Create new project**.
2. Title "Statecraft: Israel (M3 preview)". Kind of project: **HTML**. Classification: Games.
3. Uploads: upload `unity/Builds/statecraft-m3-web.zip` and tick **This file will be played in the browser**.
4. Embed options: viewport **1920 × 1080**; tick **Fullscreen button**; leave **SharedArrayBuffer support** off (the build
   uses no threads).
5. Visibility & access: **Draft** first. Open the page, play a turn, and check the browser console shows `GAME OK`. Then
   choose **Restricted** (password) or **Public**.
6. Record the page URL below under "Published at".

itch.io serves `.br` files with `Content-Encoding: br` (it detects the extension), so no server settings are needed.

Published at: (owner fills in)

## Saves on itch.io: the fix and its test
- **Problem:** on WebGL, `Application.persistentDataPath` is `/idbfs/<MD5 of the page's directory URL>`. itch.io serves
  every upload from a new URL, so a new upload would hide the saves made with the previous one.
- **Fix (Task 21):** the autosave lives at the fixed path `/idbfs/statecraft-saves/autosave.txt`. The template sets
  `autoSyncPersistentDataPath: true`, so closing the file persists it to IndexedDB. IndexedDB is per origin, so saves
  survive re-uploads to the same project, and stay separate per browser profile.
- **Test plan (OWNER, after the first two uploads):**
  1. Upload A as above. Play two turns (press End Turn twice) and note the date in the top bar.
  2. Reload the page. Expected: the game continues on that date (the autosave loaded).
  3. Upload build B to the same project, replacing A (rebuild first if anything changed).
  4. Open the page again with a hard reload. Expected: the game continues on the same date and turn. If the content
     changed between A and B, a message reads "This save was made with different game content; the replayed game may
     differ." **Pass** if date and turn match.
  5. If the game starts again on 15 May 1948, the fixed folder did not persist across uploads: record the browser and
     the page URL here, and use the Deferred fallback (a localStorage bridge, or save export and import).
  6. Record the result (date, browser, pass or fail) here.

Result: (owner fills in)

## Known limits of this build
- Placeholder settlement markers (per-era art is M4); no Diplomacy screen or legacy report (M4); English only.
- The headless smoke plays five turns; a full-slice smoke is M4.
```

In `docs/dev-setup.md`:

1. Replace

```markdown
- Run `tools/build_sim_for_unity.sh` before opening `unity/`. The sim DLLs are build output and are git-ignored.
```

   with

```markdown
- Run `tools/build_sim_for_unity.sh` before opening `unity/`. The sim DLLs are build output and are git-ignored.

## Builds and browser checks (M3)
- Web build: `"$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log`
  (`-compression` is `disabled`, `gzip` or `brotli`; output in `unity/Builds/Web-<compression>/`, about 2 minutes).
- macOS IL2CPP build: `"$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile unity-build-mac.log`
  (output `unity/Builds/Mac/Statecraft.app`).
- Browser check: `uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&smoke=5" --expect "GOLDEN OK 1952" --expect "SMOKE OK 5 turns"`.
  It uses Chrome for Testing from the Playwright cache (`~/Library/Caches/ms-playwright/chromium-1228` on this Mac;
  elsewhere `npx playwright install chromium`).
- Page flags: `?golden` (golden check), `?smoke=N` (play N turns through the UI), `?profile` (terrain profile). Desktop
  players take `-golden`, `-smoke N` and `-profile`.
- A player build rewrites some URP asset and player-setting fields as build-time caches; drop them afterwards with
  `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`.
- Play a build locally: `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.
```

- [ ] **Step 8: Commit**

```bash
git add docs/dev-setup.md \
  docs/m3-release.md \
  unity/Assets/Scripts/Map/PixelErrorProfile.cs \
  unity/Assets/Scripts/Map/PixelErrorProfile.cs.meta \
  unity/Assets/Scripts/UI/GameRoot.cs \
  unity/Assets/Tests/EditMode/PixelErrorProfileTests.cs \
  unity/Assets/Tests/EditMode/PixelErrorProfileTests.cs.meta
git status --short   # nothing else should be staged; build output and logs (unity-*.log) are git-ignored
git commit -m "feat(unity): WebGL terrain profile; release build and the owner's itch.io steps

pixelError 1 vs 5 on WebGL: 4.70 vs 1.79 ms avg, 6.00 vs 4.00 ms p95; 1 stays.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Deferred to later plans

Every spec item M3 does not finish has a home. The M3 rows of the M0–M2 Deferred table all close here (saves, UI screens, content in StreamingAssets, Appoint and MapAction, heightmapPixelError on WebGL, the WebGL map input provider, the golden hash in IL2CPP and WebGL players, overlap validation, economy calibration moved from M4).

| Spec item | Plan | Note |
|---|---|---|
| Diplomacy screen and a diplomacy system (actors' standing changes, treaties) | M4 | Ruling 1: no diplomacy system exists yet |
| Legacy report at the end of a chapter | M4 | Ruling 1; listed under M4 in spec §9 |
| Per-era 3D settlement art, vegetation, units (replacing the M2 markers); art pipeline | M4 (own plan) | Ruling 2 |
| Tile yields in the info panel | Dropped | Owner retracted them (ruling 2) |
| Portraits for people without a recorded public-domain photo (Golda Meir, Dov Yosef, Shazar, Yadin, Dori and the 1950–51 ministers) | M4 | Portrait pipeline: public-domain/CC0 only, licence recorded, likeness checked by a human (spec §5.6) |
| Briefing: only the 3–5 most important items reach the PM, the rest go to an inbox (spec §4 step 1) | M4 | M3 shows every cause in the Why panel |
| Legislation as bills voted by the parties (beyond the M1 policy vote) | M4 |  |
| Delegation that uses minister loyalty (M0–M2 ruling) | M4 | Competence already drives delegation and effects; appointees' competence counts from Task 6 |
| Open history mode | M4 | `Simulation.NewGame` still throws `NotSupportedException` for `Mode.Open` |
| Full Defence system: command traits, intelligence estimates with uncertainty, procurement, manpower policy; war mode on the hex map | M4 |  |
| Map actions beyond absorption: new towns, kibbutzim, water and roads | M4 | Ruling 4 scopes M3 to transit camps and housing |
| Content 1952–56 (New Economic Policy, the reparations Knesset vote of January 1952, the 1955 election, the Czech–Egyptian arms deal, Suez) | M4 | M3 content ends 31 Dec 1951 |
| Deputy ministers (Efrati, Shari, Kahana) and Kaplan's Deputy Prime Minister title | Content work before M4 | Notes §1c/§1e allow skipping them in M3 |
| Ministers of Immigration and of War Victims after 8 Oct 1951 | Content work before M4 | Both offices end on 1951-10-08 (research addendum: no such portfolio in the Third Government); re-add a term if a source shows the portfolio |
| Who held rationing responsibility from 8 Oct 1951 | Content work before M4 | Vacant in M3 (Addendum 2: not found). Dov Yosef as Trade and Industry minister is plausible but unsourced; next sources: Knesset plenary records 1951-52, Orit Rozin, the State Archives |
| Dated budget-line ownership (the Immigration line belongs to `immigration_minister` until 1951-10-08, then to the Prime Minister) | M4 | M3 puts the line on `prime_minister` from game start because budget keys are static (C3); so Shapira's competence does not scale absorption spending in 1948–51 (Task 2 review item 8) |
| Event options that move coalition stability or public support directly (events can only shift indicators today) | M4 | Task 4 review: the 1951 education crisis's historical cost (the government fell) is carried by the scripted government dates, not by the option's effects |
| Economic recovery after 1951: the New Economic Policy (Feb 1952) and its devaluation; without it the historical run stays in the currency crisis after 1951 (reserves about -74 by Aug 1952, food falling) | M4 | Task 5 review item 5; M3 content ends 31 Dec 1951 |
| Economy calibration v2: GNP and national accounts (+25% 1950→51), each year's fiscal stance (the 1951 tightening), unemployment, the official and black-market price index | M4 | M3 matches the two-year debt total, the budget scale, the export/import ratio and the crisis date (all consistency anchors) and checks foreign inflow against external debt loosely; first independent anchors: GNP +25% 1950→51, FY1951/52 instalments |
| Map-action upkeep, camp capacity and occupancy (camps that fill and empty) | M4 | M3 caps map actions at two per turn |
| Military or economic collapse ends the state (spec §4) | M4 | Carried from M0–M2; the currency crisis now cuts food but ends nothing |
| Save migrations between schema versions | When schema v2 appears (M4) | v1 is the only schema; a newer save is refused with its version |
| Save export and import (backstop if the itch.io save folder ever fails) | M5 | Task 24's two-upload test decides whether it is needed sooner |
| Static SDF font assets (fixed size and glyph set) and Hebrew/RTL | M5 / out of scope | Tech notes §1.5; Hebrew is outside this spec (§11) |
| Smaller download: the 8 MB heightmap outside Unity's compression | M5 | Tech notes §6 |
| Screen-by-screen comparison against the approved images (spec §8.5) | M5 | Task 23 has a manual check against promo/final/03 |
| A headless smoke test that plays the whole slice (spec §8.4) | M4 | M3's smoke plays five turns in the WebGL player |
| Release-build gating: only reviewed content, `living: true` people excluded, sensitivity reviews for sensitive tags | M5 | Carried from M0–M2; the event card already stamps DRAFT content |
| Hebrew names and birth/death dates for all people | Content work before M4 | Carried; M3 adds `died` only where a term ends in death (Remez) |
| Open fact: how the budget was approved in 1948–56 (`open_questions: [budget_approval]`, now also on the Second Knesset) | Content work before M4 | Carried |
| Legal review of map-data licences; the Unity MCP check (M0 b) | M5 / owner | Carried |
| itch.io upload and the two-upload save test | M4 (owner ruling, 2026-10-10) | Deferred to the start of M4 as a private Draft; the steps stay in `docs/m3-release.md` ("Deferred to M4") |

## Self-review

Run against the spec, the rulings and the code on 2026-10-10.

**1. Spec and ruling coverage.**
- §9 M3 "HUD, Cabinet, Event and Defence screens, delivered as a browser link": Tasks 14, 19, 15, 16, then 22–24 (browser build, local verification, owner upload steps).
- Ruling 1 screens: HUD 14, Cabinet with Appoint 19 (sim 6), Budget 17, Event card 15, Defence 16, Why 18. Diplomacy and legacy report: Deferred.
- Ruling 2 look: Task 10 (navy, gold, serif, top bar), 15 (event card right), 20 (info panel bottom-left without yields, minimap bottom-right); M2 markers kept.
- Ruling 3 content: governments and ministers 2, Knesset and President 2, aliyah and population 3, events 4, the rationing responsibility 2 (Yosef, Lavon, then vacant: C2 ruling), overlap validation 1; every fact sourced with `checked: 2026-10-10`; conflicts in comments.
- Ruling 4 sim: Appoint 6, MapAction 8 (hex rule 7), saves 9 (autosave 21), economy calibration 5, the two goldens and their sync procedure 1–8.
- Ruling 5 platform: web input provider and manifest 12, template with 100% canvas and autosync 13, PanelSettings 10, Input System and the camera map 11, golden in WebGL and macOS IL2CPP 22, heightmapPixelError profile 24, package prune 13, Map.unity boot scene 13 and 23, Brotli 13 and 24, headless build script 13.
- Ruling 6 hosting: release folder and local verification 24; owner upload and the save-path test plan 24 (fix in 21).
- Spec §7 errors: refused commands show their reason (14 onward); content errors name file and field (1, 7); saves are versioned, a content mismatch warns (9, 21), autosave every turn (21).
- Review amendments round 1: R1 (stage settings before builds, Task 13), R2 (anchor labels and a loose independent check, Task 5), R3 (President and serving Chief of Staff cannot be appointed; a dead appointee leaves office, Task 6), R4 (any autosave failure starts a new game, Task 21), R5 (explicit staging, `.meta` files listed, `unity-*.log` names), R6 (every total observed on its own branch), C1 (offices end with `until`, Task 2), C3 (no budget line for an abolished office, Tasks 1–2), C4 (Weizmann comments, Task 2), A1–A6 (Tasks 5/17, 23, 22–24, 11, 10, 8), and the controller's C2 ruling (rationing responsibility: Yosef, Lavon, then vacant; Task 2, with Task 6's abolished-office test moved to War Victims). Task 1, already implemented, is unchanged.
- Gaps found and closed while writing: the `austerity_1949` window had to follow the Rationing fix (Task 2); deleting `SampleSceneProfile.asset` would have changed the URP look (kept, Task 13); a player build rewrites settings (Build restores compression, and the steps discard build caches).

**2. Placeholder scan.** No TBD/TODO, no "similar to Task N", no step without its code. The only values left to the implementer are measurements recorded as evidence (the profile lines in Task 24, if the machine differs) and the owner's itch.io URL and test result; each has a verified value or an explicit owner field.

**3. Type consistency.** Names were checked against the replay that compiled them (`UrlFlags.Has/Int`, `GameRoot.Smoke`, `EffectsSystem.BudgetFactor(shares, office)` added in round 1): `GoldenRun.Play/Hash1950/Hash1952/Until1950/Until1952`; `Cabinet.Holder/Fill/Check/Candidates/Appoint/Lineup`; `MapActions.Check/Apply/At/Distance`, `MapActions.TransitCamp/Housing`; `GameSession.New/Load/Step/State/Previous/Last/Save`; `SaveGame.Write/Parse/Record`; `SimBridge.Queue/QueuedLike/Withdraw/EndTurn/Replace/AttentionLeft/Changed/TurnEnded`; `ToastPresenter.Show/Refuse/Clear`; `GameInputs.FromDisk/FetchAsync/MapFromDisk`; `MapScene.Build(MapInputs)`; `GameBoot.Ready`; `GameRoot.Begin`. Every Interfaces block uses the same names.

**4. Review Focus.** Five uncovered input classes, each now pinned by a named test in its owning task (see Review Focus).
