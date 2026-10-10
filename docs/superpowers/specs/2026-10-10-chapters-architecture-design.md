# Chapters architecture: how eras chain without resets

- **Date:** 2026-10-10
- **Status: PROPOSED — owner review needed for the decisions listed in §Decisions**
- **Issue:** #53 (roadmap tracker). Era epics that depend on it: #49 (Yishuv), #50 (1957–1977), #51 (1977–1993), #52 (1993–2026).
- **Parent spec:** [2026-10-09-statecraft-israel-design.md](2026-10-09-statecraft-israel-design.md) (§2 "Eras as chapters, not resets"; §3 saves; §5 content; §7 saves; §8 testing; §9–§11)

This document says how one game runs from one era into the next without a reset, in this codebase. It names the files
and types it changes. It writes no code; the work items are listed in §15 and drafted as separate issues.

**Words used here.** In data, an **era** is one record in `content/eras.yaml`. On screen, the player sees it as a
**chapter**. One era is one chapter. A **boundary** is the date where one era ends and the next begins.

## Decisions

Each needs the owner's yes or no. Everything else in this document follows from them or is a technical choice with a
stated default.

| # | Decision | Recommended default |
|---|---|---|
| D1 | How numbers carry across a boundary (§4.3) | **Deviation carry:** the next chapter starts from its sourced start values, moved by how far the player's game ended from the previous chapter's historical reference run. Not absolute carry, not a reset |
| D2 | Can a player start at a later chapter without playing the earlier ones? (§6) | **Yes.** Every chapter has a sourced start and can be started fresh; playing straight through is the main mode |
| D3 | Chapter dates (§3.1) | **Founding = 15 May 1948 to 1 January 1957.** Later boundaries are set in #49–#52 with one rule: a boundary is a date, and the last turn before it is shortened to end on it |
| D4 | Saves across chapters (§7) | **A checkpoint at each boundary.** A save stores the state at the start of the current chapter, so a content update to an earlier chapter does not change a game that is already past it |
| D5 | Money on screen (§11) | **Each period's own currency**, converted at sourced redenomination dates; the sim converts its stored amounts on that date |
| D6 | Browser build: what it downloads (§9) | **One chapter at a time:** shared data plus the current chapter's content pack at boot, the next pack fetched in the background. Budget: 1 MB compressed per chapter pack |

## 1. The rules this design keeps

1. **No reset.** Player choices made in one chapter still matter in the next: money, population, policies, the
   map, standing, and which options were chosen in past events.
2. **Each chapter is calibrated on its own.** A chapter's calibration test does not depend on how well an earlier
   chapter was calibrated (issue #53: "one tolerance set per era").
3. **Existing goldens do not move.** `GoldenRun.Hash1950` and `GoldenRun.Hash1952` (`sim/Statecraft.Sim/GoldenRun.cs`,
   `docs/determinism.md`) and the browser `SMOKE OK 5 turns` value stay the same through every item in §15. Each item
   says how.
4. **Data decides, code implements.** Eras, dates and which systems are on are content. Code holds one implementation
   of each system and reads the switches.
5. **The content rules apply in every era** (spec §1, §5): Israeli-perspective narration, one sourced value per fact,
   real names, no invented quotes, nothing that rewards an atrocity, the Holocaust reacted to and never played, and
   living people out of release builds until legal review.

## 2. What exists today (checked against the code)

| Area | Today | Gap for chapters |
|---|---|---|
| Game start | `Simulation.NewGame` starts at `Rules.Start` (`content/rules.yaml`: 1948-05-15) | One start only |
| Dated content | Events have `from`/`to`; people have dated office terms; legislatures and governments have `from`/`to`; offices have `until` only (`Content.cs`) | Indicators, policies, effects, situations, voter groups and actors have no dates; offices have no `from` |
| Model in state | `EffectsSystem.Init` puts **every** indicator, policy and effect in the state, and `Apply` loops over all of them | Adding any later-era policy or effect to `model.yaml` today **moves both goldens** (new keys in the hashed `pol{}` and `eff{}` maps) |
| Rules | One `Rules` object for the whole game; economy values are calibrated to 1950–51 | No per-era values |
| Switches by date | `Offices.Legislature(date)` picks the sitting body; `Legislature.BudgetApproval` sets whether the budget is voted; `Rules.MapActionsFrom` | These already are "systems switched by data"; nothing else is |
| State | `GameState` (`sim/Statecraft.Sim/GameState.cs`) has no era field; `Hash()` covers every field | A new field changes the hash text, so it moves the goldens |
| Saves | `SaveGame` schema 1: content hash, seed, mode, command log, end hash; replay from the seed; `MaxTurns = 2000` | No chapter, no checkpoint; one content hash for everything |
| Content loading | `ContentLoader.Load` reads every file; WebGL fetches every file in `manifest.json` in parallel (`GameInputs.FetchAsync`, `ContentManifest`) | One HTTP request per event file; itch.io allows 1,000 files per upload (`docs/m3-release.md`) |
| Validation cost | The office-overlap check compares every pair of terms (`ContentLoader.Validate`) | Quadratic in the number of terms |
| Settlements | `content/map/settlements_1948.yaml` (25 places) with `founded` as a year; `settlement_hexes.yaml` built from it; the Unity map reads the 1948 file by name (`SettlementPlacer.DefaultFileName`) | No dates in use; no end dates or renames |
| Water | `tools/terrain_bake.py` derives sea and lakes from the modern DEM into one `coast.png` (86 KB) | Modern water only (#16) |
| Money display | `model.yaml money` gives one prefix per account (`IL `, `$`) | One currency for all time |
| Office titles | One `title` per office | Titles that change with time |
| UI text | `unity/Assets/UI/strings.en.yaml`; but `Game.uxml` has "Loading the map of 1948…" | Era words in the UI |

**Measured (2026-10-10, .NET 10, Release, this worktree):** loading today's content (31 events) takes 142 ms
including JIT start-up; loading the same content plus 3,000 copies of `hula_drainage.yaml` takes 711 ms; the golden
run to 1952 (43 turns) plays in 5–10 ms with either. Event text is about 4 KB per event and compresses about five
times with gzip. The sim's per-turn cost is not the problem; download size, request count and load time in WebGL are.

## 3. What an era is in data

### 3.1 `content/eras.yaml`

One file lists the eras in order. Eras are contiguous: each era's `from` is the previous era's `to`.

```yaml
eras:
  - id: founding
    name: The Founding           # the chapter name the player sees
    from: 1948-05-15             # = rules.yaml start today
    to: 1957-01-01               # exclusive, like every other window
    systems:                     # switches for code that exists (§5); strict keys
      statehood: true
      legislature: state         # state | assembly (a consent body without force of law)
      war_mode: true
      map_actions: [transit_camp, housing]
    start: start/founding.yaml   # sourced start values for a fresh start (§6)
    carry: carry/founding.yaml   # how the previous era's state maps into this one (§4)
```

- `id` is the key used by saves, packs, goldens and file folders.
- The loader checks that eras are contiguous and non-overlapping, that every dated content record falls in the range
  of the eras, and that `systems` uses only known keys.
- Today the file has one era, `founding`. The first era's `carry` is absent.

**D3:** the founding era runs from 15 May 1948 (the current start) to 1 January 1957, the spec's "Q2 1948 to
Q4 1956". The Yishuv era (#49) would end on 15 May 1948. Each later epic sets its own `to` by the same rule.

### 3.2 Dated content (content windows)

Every content record that can begin or end gets an optional `from`/`to` window, half-open like events. A record
without a window is active in every era. This is the same rule events, terms and legislatures already follow.

| File | Records that gain `from`/`to` |
|---|---|
| `model.yaml` | indicators, policies, effects, situations, voter groups, actors (money is dated in `currency.yaml`, §11) |
| `offices.yaml` | `from` (created), next to the existing `until`; plus `successor` (§4.2) and dated `titles` (§11) |
| `aliyah.yaml` | regions (the series is already dated) |
| `rules.yaml` map actions | each action |

Records that are already dated keep their fields. People span eras (one person's office terms can fall in two or
more chapters), so `people.yaml` stays one file.

The sim reads "what is active on this date" through one view per window state, not by filtering on every call:
`Content.At(date)` returns the active indicators, policies, effects, situations, voter groups and actors. It is cached,
because windows change only on a few dates. `EffectsSystem`, `PoliticsSystem`, `HistoricalAI` and the UI presenters
read the view instead of the raw lists.

**State follows the windows.** When a turn ends past a record's `from`, the record is added to the state as `Init`
would add it (indicator at `base` with shift 0, policy at `default`, effect contribution 0). When it ends past a
record's `to`, the record is removed. This is done in one place, `ContentWindows.Update(c, s, turnStart)`, called in
`Simulation.Step` after the date moves. **Golden safety:** every existing record has no window, so the state of the
1948–52 runs holds the same keys as today.

### 3.3 Rules per era

`rules.yaml` stays the base. An era may add `rules/<era>.yaml` with only the keys it changes. The loader merges at
the YAML-node level (base mapping, then the era's keys), then reads the result with the same strict `Rules` type, so a
typo in an era file still fails. Systems ask `Content.RulesAt(date)` instead of `Content.Rules`.

The founding era has no override file, so its rules are exactly today's. The economy values in `rules.yaml` are
calibrated to 1950–51 (`EconomyCalibrationTests`); a later era sets its own in its own file and its own calibration
test.

**Alternatives considered for era-specific content:**

| Option | How | For | Against |
|---|---|---|---|
| A. Dated records in the existing files (recommended) | `from`/`to` on records, `eras.yaml`, partial `rules/<era>.yaml` | One rule everywhere (windows); people and parties that span eras stay single; no moves of existing files except events (§9) | `model.yaml` grows long; needs the active view |
| B. A folder per era with full copies | `content/eras/<id>/model.yaml`, `rules.yaml`, ... | Each era is self-contained | Records that span eras are duplicated and drift apart; a policy that lives 1948–1990 is written three times |
| C. Era code modules | A C# class per era registers its systems | Full freedom | Rules move out of data; contributors must write code to add an era |

## 4. Crossing a boundary

### 4.1 The boundary turn

The turn grid does not land on boundary dates by itself (quarters from 15 May fall on the 15th; war months and weeks
shift it). So the last turn of an era is **shortened to end exactly on the next era's `from`**:
`next = min(SimTime.Advance(date, len), nextEra.From)`. The year fraction of a shortened turn is its days over 365;
whole turns keep today's fixed fractions.

This makes a game that played into a chapter and a game that started fresh at it share the same calendar from the
boundary on, so per-era goldens and event windows line up. **Golden safety:** there is no boundary inside 1948–1957,
so no turn of the existing runs changes.

Order inside `Simulation.Step` when a turn ends on a boundary:

1. the normal turn, as today, ending on the boundary date;
2. `ContentWindows.Update` (records that ended are removed, records that begin are added);
3. `EraSystem.Cross` applies the next era's carry rules (§4.2, §4.3) and adds Why lines ("A new chapter begins:
   ...");
4. `Cabinet.ExpireOnGovernmentChange`, `AttentionSystem.Refill` and `EventSystem.Trigger`, as today.

A chapter's end also produces the legacy report (#19). The game then goes on in the same session; there is no new
game.

### 4.2 What happens to each part of `GameState`

| Field | At a boundary | Notes |
|---|---|---|
| `Date`, `Turn`, `RngState`, `Mode` | persist | one RNG stream for the whole game |
| `Indicators` | recomputed | level = base + effects + shift, as today |
| `IndicatorShifts` | persist where the indicator continues | the player's lasting marks on morale, housing and so on |
| `EffectContributions` | persist where the effect continues | dropped for effects whose window ended |
| `Policies` | persist where the policy continues; mapped where the carry file names a successor | a new policy starts at its default |
| `ActiveSituations` | persist where the situation continues | hysteresis state is kept |
| `Budget` | mapped through office successors, then rescaled to sum to 1 | e.g. a Yishuv institution's line passes to its state ministry |
| `Treasury`, `Debt`, `FxReserves` | **re-based by deviation** (D1) and converted by any currency factor | §4.3 |
| `Population` | groups mapped by the carry file, then **re-based by deviation** (ratio) | §4.3 |
| `Rung`, `CeasefireWeeksLeft`, `Mobilization` | persist | the ladder is always on; 1948 opens at war |
| `CoalitionStability` | persist | its meaning follows the sitting body (assembly or Knesset) |
| `Attention` | refilled | as every turn |
| `FiredEvents`, `ChosenOptions` | persist | later events may `require` earlier choices |
| `PendingEvents` | persist | the events raised on the boundary turn are answered in the new chapter's first turn; an era pack is kept loaded while any of its events is pending |
| `Appointments`, `AppointedUnder` | persist | `Cabinet.ExpireOnGovernmentChange` ends them when the government changes, at a boundary or not |
| `Placements`, `MapActionsTurnDate`, `MapActionsThisTurn` | persist | what the player built stays on the map |
| `GameOver` | — | a game that ended does not cross |

**Golden safety:** this needs **no new `GameState` field.** The current era is derived from `Date` and
`eras.yaml`. If a later item does need a field, it is written into `Hash()` only when it differs from its default, so
the existing hash text does not change. That convention is documented in `docs/determinism.md` with the field.

### 4.3 How numbers carry (D1)

Each era has a sourced start (§6): treasury, debt, reserves, population by group, budget shares and start rung on its
`from` date. The question is how a game that played the previous chapter enters it.

| Option | Rule at the boundary | For | Against |
|---|---|---|---|
| A. Absolute carry | new value = the player's value, mapped | Simplest; no reference data | Every chapter's calibration error adds up: a small 1948 economy error is still there in 2026. A later chapter cannot be calibrated alone |
| **B. Deviation carry (recommended)** | additive: `start + (player − reference)`; ratio (population): `start × player / reference`, where *reference* is the previous chapter's historical run at the boundary | A historical playthrough enters the next chapter exactly at its sourced start; a player who did better or worse carries the difference. Each chapter is calibrated alone | Needs a pinned reference file per boundary, regenerated when the earlier chapter changes |
| C. Reset to sourced values | new value = start | Simplest for balance | It is the Civ VII reset the spec rejects (§2) |

**Reference file.** `content/carry/<era>.reference.yaml` holds, for the boundary into `<era>`, the carried quantities
of `GoldenRun.Play` of the previous chapter (golden seed, Historical mode) on the boundary date. A tool writes it and a
test regenerates it and compares, as the goldens are checked today. A change to the earlier chapter that moves its
golden also changes this file, in the same commit.

**Rules that keep it sane:**
- The carry file lists only quantities that have a sourced start in the next era; everything else in §4.2 persists.
- A ratio carry needs a reference above zero; additive carry is used for money because it can be negative.
- Each carried quantity has a sourced floor and ceiling in the carry file, so an extreme game cannot enter the next
  chapter with an impossible value (for example negative population). A clamp is reported in the Why panel.
- With another seed, a historical run ends near, not exactly at, the reference. That small difference carries. This is
  wanted: it is the effect of the random draws in that game.

### 4.4 Worked example: Yishuv institutions become state institutions (1948)

This is how #49 would hand over to the founding chapter without a reset. Office ids and dates here are examples;
the real mapping is #49's research and needs sources.

| Yishuv-era thing | At the 15 May 1948 boundary | Mechanism |
|---|---|---|
| Jewish Agency Executive and Vaad Leumi offices (e.g. the chair of the Jewish Agency Executive) | end; their budget lines and policies pass to state offices (e.g. Prime Minister) | `offices.yaml`: `until` plus `successor: prime_minister`; `carry/founding.yaml` maps budget lines |
| The people who held them | continue in their new offices on the sourced term dates | `people.yaml` terms; nothing moves |
| The Assembly of Representatives / People's Council | gives way to the Provisional State Council | `legislatures.yaml` periods with `kind: assembly` or `kind: state` |
| National funds (treasury in the Yishuv) | become the state treasury, re-based by deviation and converted by a sourced currency factor | `carry/founding.yaml` + `currency.yaml` (§11) |
| Population groups (e.g. by aliyah wave) | map to the founding chapter's groups (all pre-state residents become `veterans`), then re-based by ratio | `carry/founding.yaml` group map |
| The Haganah as a policy-led force | the IDF is founded inside the chapter (the existing `idf_founding` event, window 22 May to 1 June 1948); mobilisation and rung persist | Defence state persists |
| Settlements the player founded (e.g. tower-and-stockade) | stay on the map and count as settlements for later map actions | `Placements` persist |
| The British Mandate as an AI actor | its window ends; the United Kingdom actor continues | actor `to` date |
| Choices in Yishuv events | stay in `ChosenOptions`; founding events may `require` them | event `requires` across eras |

## 5. Era-specific systems

Systems are switched on and off by data, in `eras.yaml systems`, and by dated records. Code implements each system
once.

| Option | For | Against |
|---|---|---|
| **A. Switches in `eras.yaml` for code that exists (recommended)** | Readable; tested once per switch; contributors can see what an era uses | A new kind of switch still needs code |
| B. Date checks in code (`if date < 1948-05-15`) | Fast to write | Dates hide in code; already avoided today (`Rules.MapActionsFrom` is data) |
| C. Era plug-in classes | Any rule possible | Moves rules out of data; harder to test and review |

Rules for switches:
- A key is added to `systems` only in the same change that adds the code behind it, with a test for both values.
  The loader refuses unknown keys, as it does everywhere.
- What is already dated stays dated: the sitting legislature, `budget_approval`, office existence, map-action dates.
  A switch is for a whole-era difference: no state (no tax law, no army), a consent assembly instead of a parliament,
  an outside ruler that sets immigration quotas (the Mandate's certificates), whether war mode is available.
- The budget-approval question (#3) stays a field on each legislature record, which is already era-keyed.

## 6. Starting at a chapter (D2)

`Simulation.NewGame(content, seed, mode, era)`; the existing three-argument form means `founding`, so the goldens and
all existing callers keep their behaviour.

A fresh start:
- sets `Date` to the era's `from`, `Turn` to 0 and `RngState` to the seed;
- reads the sourced start values from `start/<era>.yaml`. For the founding era these are today's `rules.yaml` keys
  (`start`, `start_rung`, `treasury_start`, `fx_start`, `initial_population`, `budget_start`), which stay where they
  are, so nothing moves;
- marks as resolved, with their historical option, exactly the earlier-era events that this era's content `requires`.
  A later chapter's opening therefore does not depend on unrelated edits to earlier chapters.

**Invariant, tested:** a historical playthrough of chapter N, carried into N+1, has the same carried quantities as a
fresh start of N+1. The turn number, RNG state and event history differ, which is expected.

## 7. Saves across chapters (D4)

Today a save is the seed and every command from the start; loading replays all of it, and a content change warns and
may diverge (`GameSession.Load`).

| Option | For | Against |
|---|---|---|
| A. Replay from the start, as today | One source of truth; no new format | A game in 1975 replays from 1948 on every load; any patch to 1948 content changes the replay of every later game (the warning fires and the game drifts) |
| **B. A checkpoint at each boundary (recommended)** | Load replays only the current chapter; earlier chapters are frozen, so content patches to them do not change a game past them | Needs a lossless state text and a second check (the checkpoint's hash) |

**Schema 2** (`SaveGame.SchemaVersion = 2`):

```
statecraft-save 2
chapter founding                 era the game started in
seed 1948
mode Historical
content core <hash>              shared files
content founding <hash>          one line per era pack used
checkpoint 1957 <hash>           optional: the state on the boundary into era '1957'
state <key> <value>              ... lossless state lines (doubles as IEEE bit patterns, as today)
turn                             the current chapter's turns, as today
<command> ...
end <hash>
```

- **Lossless state text.** `GameState` gains `Write`/`Read` built on the same canonical form `Hash()` uses. A test
  checks, for every turn of the golden runs, that `Read(Write(s)).Hash() == s.Hash()` and that stepping the restored
  state gives the same next hash.
- **Loading.** With a checkpoint: read the state, check its hash, replay the turns after it. Without one: replay from
  the seed, as today. A content-hash mismatch on the current chapter's pack warns as today; mismatches on earlier packs
  do not matter once a checkpoint exists.
- **Migration.** Schema 1 saves load as `chapter founding`, one content hash, no checkpoint. A test keeps a schema 1
  save fixture and checks it loads to the same hash. Older builds refuse a schema 2 save with the existing "newer than
  this game" message.
- **Length.** `MaxTurns` applies per chapter, not per game.
- **Golden safety:** the save format is not hashed; state hashes do not change.

## 8. Determinism and goldens

The existing method stays (`docs/determinism.md`): SplitMix64, no trigonometry, a canonical hash, goldens pinned in
three places.

**Golden-safe checklist** (each item in §15 shows it holds):
1. Records without windows behave as today; new records have windows outside 1948–1952 or no state effect there.
2. No era override file for `founding`.
3. No boundary inside the existing runs.
4. No new hashed state text unless it is omitted at its default.
5. Moving event files does not change `Content.Events` order (all of today's files move into the same folder, which
   keeps their ordinal order) and nothing in the sim depends on that order (`EventSystem.Trigger` sorts by `from`,
   then id).

**New goldens per era.** `GoldenRun.PlayChapter(content, era, until)` plays a fresh start of an era with the golden
seed and `HistoricalAI`. Each era pins at least one constant, named by era and end year (for example
`Hash1957_1960`), in the same three places. A **chain golden** pins a run that crosses each boundary (for example
founding into 1957). The founding era's goldens are today's `Hash1950` and `Hash1952`, unchanged.

**Rebaselines stay local.** Because a fresh start reads sourced values, a change to the 1950 events moves the founding
goldens, the chain goldens and the carry reference file for 1957, and nothing else. The 1957 fresh-start golden
stays.

## 9. Content at scale (D6)

The spec's full span could hold thousands of events and hundreds of people. The limits are in the browser:

| Limit | Today | At 3,000 events | Design answer |
|---|---|---|---|
| Files per upload (itch.io) | 51 files | over 3,000 | one pack file per era |
| HTTP requests at boot | one per content file | over 3,000 | core + current pack, next pack in the background |
| Event text | 124 KB raw, 24 KB gzip (31 events) | about 12 MB raw, 2.4 MB gzip | 1 MB compressed per era pack (budget, D6) |
| Load time (.NET) | 142 ms | 711 ms | measure IL2CPP WebGL; load one era |
| Office-overlap check | pairwise over all terms | quadratic | sort terms by office and compare neighbours |
| Portraits | 30 in the build, 628 KB | hundreds | load per era (follow-up with #30, #44) |

**Layout.** Events move to `content/events/<era>/` (today's 31 files go to `content/events/founding/`). The loader
checks that an event's window lies inside its folder's era. An event may not span a boundary; it is split. Shared
files (`people.yaml`, `parties.yaml`, `offices.yaml`, `legislatures.yaml`, `governments.yaml`, `model.yaml`,
`rules.yaml`, `eras.yaml`, the map) are the **core**.

**Packs.** The Unity build step (`ContentSync`) writes one `packs/<era>.pack` per era: the era's files joined, each
with its relative path and length, the same framing `ContentLoader.HashOf` already hashes. `manifest.json` lists the
core files and the packs. The pack is transport only: `Content.Hash` stays defined over logical files, so packing
does not change it. A save records the hash of the core and of each pack it used (§7).

**Loading.**

| Option | For | Against |
|---|---|---|
| A. Load everything at boot | One code path; simplest | Boot time and download grow with every chapter; worst on low-end machines (#44) |
| **B. Core + current era, next era prefetched (recommended)** | Boot cost stays about one chapter's worth | `Content` grows during a session; cross-era references must resolve without the old event objects |
| C. Precompiled binary content | Fastest parse | A second format to keep in step with YAML |

With B, the sim only needs events of the current era (and of the previous one while any of its events is pending):
`requires` checks `ChosenOptions` by id and needs no event object. Whole-content validation (cross-era `requires`,
coverage, placeholders) runs in `dotnet test` over all eras; the player runs the per-file checks only. `dotnet test`
and the CLI keep loading everything.

## 10. Map eras

One heightmap (the modern Copernicus DEM, 8 MB) serves every era. What changes by date is drawn on top of it.

| Layer | Data | Change |
|---|---|---|
| Settlements | One `content/map/settlements.yaml` replacing `settlements_1948.yaml`: `founded` as a date (a year alone means 1 January), optional `until` (abandoned or merged) and dated `names` for renames | Unity places what exists on the current date; `MapActions.Check` counts only settlements that exist on the turn's date; `tools/settlement_hexes.py` hexes every row. This answers #27's "agree the file shape first" |
| Water | `content/map/water.yaml`: water bodies (Lake Hula, the Hula swamps, the Dead Sea's southern basin) with `from`/`to`, levels and sources | `tools/terrain_bake.py` bakes one `coast` mask per water epoch (86 KB each); Unity swaps the mask on the epoch's date. Builds on #16 |
| Borders and lines | `content/map/lines.yaml`: dated polylines (armistice lines, later lines), with Israeli-perspective names | drawn as overlays; hex cells keep no political meaning (spec §3) |

| Option for water | For | Against |
|---|---|---|
| **A. One heightmap + dated masks (recommended)** | Small (a mask per epoch); terrain identical in every era | Water under a raised level is drawn flat over today's relief |
| B. A heightmap per era | Exact relief | 8 MB each; the download budget (#44) cannot carry it |
| C. Water polygons drawn at runtime | No bake step | A second water renderer next to the shader's coast mask |

The map changes on its own dates, not only at boundaries: #16 dates the Hula drainage 1951–1958, inside the founding
chapter. A water
epoch may follow an event's outcome later; first version: dates only.

## 11. UI per era (D5)

| Item | Change |
|---|---|
| Office titles | `offices.yaml titles: [{ from, title }]` next to `title`; `PlayerText.OfficeTitle(c, id, date)` |
| Money | `content/currency.yaml`: dated currencies, each with display prefix, `per_previous` factor and sources. On a redenomination date the sim divides stored amounts of that currency by the factor (a dated transform like §4, applied in `EraSystem`). `PlayerText.Money` takes the date. Factor 1 changes only the display. Which currencies and which dates (the pound/lira, the shekel, the new shekel) is content to source; this design only provides the mechanism |
| Units | Land in dunams and population in people stay; amounts stay in millions; the reserves stay in US dollars |
| UI strings | `strings.en.yaml` keys may have era variants, `key@<era>`, used when present; the loading line in `Game.uxml` becomes a string with the chapter's name |
| Chapter name | shown in the top bar and on a chapter card at each boundary, next to the legacy report (#19) |

| Option for money (D5) | For | Against |
|---|---|---|
| **A. Each period's currency, converted on its dates (recommended)** | Historically right; numbers read like the sources | Stored amounts jump on conversion dates; every era's economy values are in its own currency |
| B. One unit throughout (e.g. 1948 pounds) | No conversions | Numbers stop matching any source after 1980 |

## 12. Testing

| Test | What it checks |
|---|---|
| `DeterminismTests` | today's `Hash1950` and `Hash1952`, unchanged through every item in §15; plus each era's fresh-start golden and the chain goldens |
| Carry reference test | regenerates `carry/<era>.reference.yaml` from the previous era's golden run and compares |
| Carry invariant | a historical chain into era N+1 equals a fresh start of N+1 on the carried quantities |
| Era content tests | eras contiguous; every dated record inside the eras' range; event windows inside their folder's era; `requires` only backwards in time; strict `systems` keys |
| Window tests | a record that begins or ends mid-game enters and leaves the state as `Init` would add it |
| Boundary tests | on a two-era test fixture (`sim/Statecraft.Sim.Tests/fixtures/`): the shortened turn, successor mapping, group mapping, clamps, Why lines |
| Save tests | schema 1 fixture still loads; schema 2 checkpoint round-trips; loading after a patch to an earlier pack gives the same game |
| `SliceSmokeTests` per era | the existing smoke, parameterised by era: a fresh start through the era, and a chain across each boundary, each under a minute |
| Calibration per era | one tolerance set per era (`<Era>CalibrationTests`), as `EconomyCalibrationTests` is for 1950–51 |
| Unity | `SimSmokeTests` and `web_check.py` print one `GOLDEN OK` line per pinned run; the pack loader is tested in edit mode |

## 13. Perspective and review across eras

The rules do not change by era; the load gets heavier. The sensitive tags and the "no reward" check apply to every
pack. `ReleaseGate` and `--release-gate` check every pack that ships. Living people (most of #52) stay out of release
builds until legal review, which a pack-level check enforces. Holocaust content in #49 is an event the player reacts
to, never a playable scenario.

## 14. Out of scope

- The content of any era (#49–#52) and their dates beyond the founding era.
- Diplomacy state and actor standing in `GameState` (#11); when it lands, it follows §4.2 (persist, actors by window).
- Open mode (#10); nothing here depends on the mode.
- Hebrew content (#55); era variants of strings would follow #12's keys.
- Coastline changes from ports and land reclamation.

## 15. Work items

Each is buildable on its own, in this order, and keeps the existing goldens. Drafted as separate issues.

| # | Item | Needs |
|---|---|---|
| 1 | `eras.yaml` and era lookup | — |
| 2 | Content windows for the model and offices | 1 |
| 3 | Rules and system switches per era | 1 |
| 4 | Boundary crossing: shortened turn and carry rules | 2, 3 |
| 5 | Fresh chapter starts and per-era goldens | 4 |
| 6 | Save schema 2 with chapter checkpoints | 5 |
| 7 | Event folders per era and content packs | 1 |
| 8 | Era loading in the browser build and the size budget | 7 |
| 9 | Dated settlements on the map | 1 |
| 10 | Water epochs on one heightmap | 9, #16 |
| 11 | Era-aware UI text, office titles and currency | 3 |
| 12 | Per-era smoke and calibration harness | 5 |
