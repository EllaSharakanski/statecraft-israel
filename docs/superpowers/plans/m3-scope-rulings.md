# M3 scope rulings (controller, 2026-10-10). Binding input for the M3 plan.

The spec is the authority: `docs/superpowers/specs/2026-10-09-statecraft-israel-design.md`.

M3 is defined in §9 as "Playable 1948–51: HUD, Cabinet, Event and Defence screens, delivered as a browser link (or a download if M0(a) fails)". M0(a) passed, so M3 ships as a browser link.

Each ruling below gives what is decided, why, and what it costs if wrong.

1. **Screens in M3**
   - Included: the HUD (top bar of national indicators, date and turn, end-turn), Cabinet (with `Appoint`), Budget, Event/decision card, Defence (escalation ladder, mobilisation, security orders), and the "Why did this happen?" panel.
     - Why: those are the screens the existing sim commands need in order to be playable.
     - Cost if wrong: a little extra UI.
   - Deferred to M4: Diplomacy (no diplomacy system exists yet) and the legacy report (listed under the M4 milestone).
2. **UI look**
   - The visual target is `promo/final/03-gameplay-1936.png`. M3 matches its UI language:
     - dark navy panels with thin gold borders and a serif display face;
     - a top resource bar with value and delta;
     - an event card on the right with image, title, text and option buttons;
     - an info panel at bottom-left;
     - a minimap at bottom-right.
   - Tile yields are OUT. The owner retracted them. The panel at bottom-left shows hex or settlement info, not yields.
   - 3D settlement art (per-era prefabs, vegetation, units) is M4, under its own art-pipeline plan. M3 keeps the M2 markers.
     - Why: the asset pipeline needs its own research and plan, and M3's goal is playability.
     - Cost if wrong: the demo looks sparse until M4.
3. **Content through 31 Dec 1951**
   - Covers:
     - the end of the First Government, the Second Government (1 Nov 1950) and the Third Government (8 Oct 1951), with every minister, portfolio, date and dated party;
     - the Second Knesset (election 30 Jul 1951, 120-seat results);
     - Weizmann's re-election;
     - aliyah for 1950 and 1951 (CBS totals);
     - population calibration at end-1950 and end-1951 (CBS);
     - about 10 sourced events from `research_notes/M3 content 1950-51/facts.md`.
   - Source rules:
     - Every fact is cited.
     - Conflicting facts follow the notes. Where the notes show a conflict, pick the source the notes rank higher, record the other in a comment, and never invent.
     - Israeli framing, real names, no invented quotes.
   - Fix existing content: the Rationing minister's term starts 1949-04-26 (ministry created), not 1949-03-10, and ends 1950-11-01. Check this against the notes.
   - Validation: the loader must reject overlapping legislatures and overlapping governments. This is a Deferred row assigned to "before M4 adds the 1951 Knesset", so it lands in M3.
4. **Sim features**
   - `Appoint` command: change a minister in a portfolio, at an Attention cost, with coalition and party effects. It must be rejected with a reason when illegal.
   - `MapAction`, scoped to ma'abarot (transit camps) and housing from 1950:
     - placing a transit camp or permanent housing on a land hex near settlements;
     - costs budget and Attention;
     - moves immigrant absorption and public support.
     - Hex ownership must use the tools' settlement-hex rule.
   - Saves: seed + command log + content hash + schema version, autosave each turn, and load that replays to the same state hash.
   - Economy calibration moves from M4 to M3.
     - Why: M3 is the first playable build, and an HUD showing foreign-currency reserves of -230 with no consequence reads as broken. The research gives anchors: the 1950/51 budget of IL 60m, GNP growth of about 25% from 1950 to 1951, IL 0.36 per dollar, and the October 1951 foreign-currency crisis.
     - Calibrate so the historical autoplay stays plausible through 1951. Calibration tests in the style of M1 Task 18.
     - Cost if wrong: M4 recalibrates anyway.
   - The golden hash will legitimately change when content extends past 1949. The procedure:
     - keep the existing 1948-to-1950-01-01 golden as is, or rebaseline it with a stated reason;
     - add a second golden that runs to 1952-01-01;
     - keep every place the hash is recorded in sync.
5. **Platform. All M3 rows of the M0–M2 Deferred table close here.** Follow the tech notes in `research_notes/M3 Unity tech/tech.md`:
   - a web map input provider using UnityWebRequest/Awaitable with a manifest, and the sim content copied into StreamingAssets with that manifest;
   - a custom WebGL template: 100% canvas and `autoSyncPersistentDataPath`;
   - PanelSettings set to ScaleWithScreenSize at 1920×1080, match 0.5;
   - the Input System reference added to `Statecraft.Game.asmdef`, with a Camera action map for pan, zoom and orbit, and no camera input while the pointer is over UI;
   - the golden hash checked in a WebGL player through a `GOLDEN OK <hash>` scene read by headless Chrome/Playwright, plus a macOS IL2CPP player (with `-ffp-contract=off` if needed);
   - heightmapPixelError profiled on WebGL;
   - the template-package prune;
   - Map.unity as the boot scene;
   - Brotli compression for itch.io;
   - a headless build script.
6. **Hosting**
   - The plan produces a release-ready WebGL build folder plus a local verification: serve it, open it in headless Chrome, and see the golden check and a scripted 5-turn smoke test.
   - Uploading to itch.io needs the owner's account. That is an OWNER STEP, documented with exact steps, never automated.
   - The itch save-path-per-upload issue gets a documented fix (a fixed `/idbfs` subfolder) and a test plan that runs after the first two uploads.
7. **Process**
   - The plan is a superpowers writing-plans document with Global Constraints, Review Focus and a Deferred table.
   - Tasks are sized for subagent-driven execution, with complete code in each step.
   - Unity UI tests follow the tech notes' edit-mode pattern: UIDocument plus NavigationSubmitEvent, never ClickEvent.
