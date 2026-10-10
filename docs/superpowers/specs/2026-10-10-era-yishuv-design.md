# The Yishuv chapters, 1882–1948: era design

- **Date:** 2026-10-10 (revised after staff review the same day)
- **Status: PROPOSED — owner review needed for §Decisions**
- **Issue:** #49 (roadmap tracker)
- **Builds on:** [the chapters architecture](2026-10-10-chapters-architecture-design.md) (#53; items #56–#67). This
  document uses its words (era, chapter, boundary, window, carry, switch). §8.5 lists every change it asks of that
  design, item by item.
- **Parent spec:** [2026-10-09-statecraft-israel-design.md](2026-10-09-statecraft-israel-design.md) (§1 perspective and
  owner decisions; §3 systems; §4 loop; §5 content pipeline)

This document says what the player does before the State of Israel exists, and how each system of today's sim serves
that period, by reuse or by a stated new piece. It ends with the hand-off into the existing Founding chapter on
15 May 1948 without moving its goldens. It writes no code and no content. The work items are listed in §12 and drafted
as separate issues.

Facts quoted here size the design. Each one names its source and the day it was checked. A fact marked *to verify* has
no checked source yet. The research issues (§12) settle it before any content uses it.

## Decisions

Each needs the owner's yes or no. Everything else follows from them or is a technical choice with a stated default.

| # | Decision | Recommended default |
|---|---|---|
| D1 | Start date, chapters and turn length (§3) | **Two eras, two chapters.** `ottoman` "The First Aliyot" runs from 31 July 1882 (Rishon LeZion founded) to 31 October 1918 (the Armistice of Mudros takes effect, the end of Ottoman rule), with **yearly** turns. `mandate` "The Mandate" runs from 31 October 1918 to 15 May 1948, with **quarterly** turns, and opens under British military rule. Turns shorten on the security ladder as today |
| D2 | Who the player is (§4) | **The head of the Zionist executive**, a dated chain of real offices: chairman of Hovevei Zion (from the Kattowitz conference, 1884), President of the Zionist Organization (from 29 August 1897), chairman of the Jewish Agency Executive (from 1935). The last office's successor is the Prime Minister. Not a fictional "Yishuv leader", not the Vaad Leumi chair |
| D3 | The legislature (§5.2) | **None in the Ottoman chapter:** policies need no vote, and the Zionist Congresses are hard events. **In the Mandate chapter the Assembly of Representatives is a consent body.** It sits from its first session, 7 October 1920. Its coalition is the parties of the Vaad Leumi executive it elected. A lost vote does not block a policy; it costs national authority |
| D4 | Historical settlements on the map (§5.7, §7) | **They appear on their founding dates in every game.** The player's map actions add land and settlements on top of history. The player cannot "un-found" Degania |
| D5 | Two carry rules for this boundary (§8.2) | **Yes to both.** (a) `rung: start`: at 15 May 1948 the rung is set to the Founding start rung (5). (b) The budget carries by deviation onto the Founding `budget_start`, with no rescale when no line moved |
| D6 | Violence against civilians in these chapters (§10.2) | **Three rules.** Attacks by the Irgun and Lehi are **react-only** events, and they state the Haganah's part where mainstream Israeli sources record one. **Haganah and Palmach operations** are decided at the level of the plan or operation, carry `war-casualties` or `massacre` where civilians were killed, and never reward civilian harm. **Atrocities against Jews** are react-only and tagged. Every such event needs the owner's sensitivity review |
| D7 | Content scope (§6) | **About 120 events and 10 decision scenes** for the two chapters (about 30 Ottoman, about 90 Mandate), written in four research batches. The last batch (1945–1948) is written first, so the boundary is tested by real content early |
| D8 | Contested terms and single values (§10.3) | **The perspective rule's choice for each,** written into the content policy's glossary: "the 1936–1939 riots (Me'oraot)" in narration; "the Deir Yassin affair", with one death toll from a mainstream Israeli historian; civil-war casualties from Gelber; the 1914 population as 94,000 (DellaPergola after Bachi). The owner confirms or changes each |

## 1. The rules this design keeps

1. **The spec's perspective rule** (spec §1; `docs/CONTENT_POLICY.md` §1). Narration and terms are Israeli-perspective,
   from mainstream Israeli historiography, with one sourced value per fact and no opposing-narrative panels. Arab
   actors and the British are AI actors with their own goals.
2. **What holds regardless** (CONTENT_POLICY §2):
   - every fact is sourced;
   - no quotes are invented;
   - nothing rewards an atrocity;
   - the Holocaust is reacted to and never played, and is shown without swastikas;
   - real people appear in their public roles.
3. **No reset.** The Ottoman chapter carries into the Mandate chapter, and the Mandate chapter into the Founding
   chapter, by the chapters design's deviation carry (its D1).
4. **The Founding goldens do not move.** `GoldenRun.Hash1950`, `Hash1952` and `SMOKE OK 5 turns` stay the same
   through every item in §12. §8.4 shows how.
5. **Data decides, code implements** (chapters design §1.4, §5). Every pre-state difference is a dated record or an era
   switch with one implementation. Where code needs a content id (the levy policy, the authority indicator), an era
   rules key names it, so no id is written into code.
6. **One value, even when sources give ranges.** The pre-1919 aliyah totals exist only as ranges (§2). The design
   calibrates to single-valued population anchors instead and derives arrivals from them, with the arithmetic in a
   comment, as `rules.yaml` already derives `initial_population` (§5.4).
7. **The reference run is history.** A policy's `default` is its historical level at the start of its window. Each
   later historical change is a hard event's `set_policy` on a sourced date. The `HistoricalAI` therefore reproduces
   history without sending policy orders, as it does today.

## 2. The history it rests on

These are the anchors that shape the systems. Event research (§12) adds the rest.

| Date | Fact | Source (checked 2026-10-10 unless marked) |
|---|---|---|
| 31 Jul 1882 | Rishon LeZion founded by ten Hovevei Zion pioneers from Kharkiv | https://en.wikipedia.org/wiki/Rishon_LeZion |
| 1882–1903 | First Aliyah. From November 1882 the Ottoman authorities moved to limit Jewish entry. Baron Edmond de Rothschild funded and took over settlements (Zikhron Ya'akov's assets in 1883). The source lists the moshavot founded 1882–1904 by year | https://en.wikipedia.org/wiki/First_Aliyah |
| 1882 / 1914 / 1922 / 1931 / 1947 | Jewish population 24,000 / 94,000 / 83,794 (census) / 174,610 (census) / 630,000 (DellaPergola after Bachi) | https://www.jewishvirtuallibrary.org/jewish-and-non-jewish-population-of-israel-palestine-1517-present |
| 1882–1903, 1904–1914 | Immigrants 20,000–30,000 and 35,000–40,000. **These are ranges only** (§1.6) | https://jewishvirtuallibrary.org/immigration-by-region-1882-2010 |
| 1919 – 14 May 1948 | Immigrants by period and continent (Asia, Africa, Europe, America and Oceania, unknown), the same columns as `content/aliyah.yaml`: 1919–23 35,183; 1924–31 81,613; 1932–38 197,235; 1939–45 81,808; 1946 – 14 May 1948 56,467. The page's total row is 482,857, which these rows do not sum to (*to verify* against the CBS original). The totals include immigrants who came without certificates | same JVL table |
| 29 Aug 1897 | First Zionist Congress in Basel; the Zionist Organization founded (*to verify*) | — |
| 1901 | Jewish National Fund founded at the Fifth Zionist Congress. By 1948 it held 936,000 dunams, 54% of the land Jews held | https://en.wikipedia.org/wiki/Jewish_National_Fund |
| 1903 | Sixth Congress: 295 for, 178 against sending an expedition to East Africa (the Uganda scheme) | https://en.wikipedia.org/wiki/British_Uganda_Programme |
| 1904–1914 | Second Aliyah, about 35,000. This is an estimate: the research must find one sourced value or drop it | https://en.wikipedia.org/wiki/Second_Aliyah |
| 1910 | Degania founded: 1910 in the infobox and lead, "after first beginnings in 1909" at Umm Juni | https://en.wikipedia.org/wiki/Degania_Alef |
| 1908 | Palestine Office in Jaffa, the origin of the Jewish Agency | https://en.wikipedia.org/wiki/Jewish_Agency_for_Israel |
| Apr 1909 | Hashomer (from Bar-Giora) | https://en.wikipedia.org/wiki/Haganah |
| 30–31 Oct 1918 | Armistice of Mudros signed 30 October, in effect from noon on 31 October 1918 | https://en.wikipedia.org/wiki/Armistice_of_Mudros |
| 19 Apr 1920 | First Assembly of Representatives election (314 members). The riots of 1920 postponed voting by eight days; en.wiki gives voting from 19 April to 3 May. Later Assemblies: 221 (from 6 Dec 1925), 71 (from 5 Jan 1931), 171 (from Aug 1944) | https://he.wikipedia.org/wiki/אספת_הנבחרים ; https://en.wikipedia.org/wiki/Assembly_of_Representatives_(Mandatory_Palestine) |
| 2 Aug 1944 | Fourth Assembly election (173 seats in the election article; Mapai 64). he.wiki dates the Fourth Assembly from 1 August, with 171 members. The design uses 2 August as the `elected` date, from the dedicated election article, and leaves the size to Yishuv research | https://en.wikipedia.org/wiki/1944_Assembly_of_Representatives_election |
| Jun 1920 | Haganah founded after the April 1920 riots | https://en.wikipedia.org/wiki/Haganah |
| 7 Oct 1920 | Jewish National Council (Vaad Leumi) elected at the Assembly's session. Chairs: Yellin 1920–29, Rutenberg 1929–31, Ben-Zvi 1931–44, Remez 1944–48. Dissolved 14 May 1948; its departments became the basis of the ministries | https://en.wikipedia.org/wiki/Jewish_National_Council |
| Dec 1920 | Histadrut founded in Haifa. Ben-Gurion was its secretary from late 1921 to 1935. About 176,000 members in 1947 | https://en.wikipedia.org/wiki/Histadrut |
| 1929 | Jewish Agency formed from the Palestine Zionist Executive. Executive chairs: Kisch 1923–31, Arlosoroff 1931–33, Ruppin 1933–35, Ben-Gurion 1935–48 | https://en.wikipedia.org/wiki/Jewish_Agency_for_Israel |
| 23–29 Aug 1929 | Riots: 133 Jews killed, 67 of them in Hebron (24 Aug) and 18 in Safed (29 Aug), by the source's summary table | https://en.wikipedia.org/wiki/1929_Palestine_riots |
| 25 Aug 1933 | Haavara (transfer) agreement. About 60,000 German Jews reached the Land of Israel 1933–39 under it | https://en.wikipedia.org/wiki/Haavara_Agreement |
| 10 Dec 1936 – Oct 1939 | Tower and stockade: about 57 settlements; Tel Amal was the first | https://en.wikipedia.org/wiki/Tower_and_stockade |
| 24 Jul 1938 | Kofer HaYishuv proposed by the Vaad Leumi: indirect levies on fares, cigarettes and entertainment, with direct emergency taxes added later (1940, 1942) | https://he.wikipedia.org/wiki/כופר_היישוב |
| May 1939 | White Paper: 75,000 immigrants over five years, then only with Arab consent. Land transfers restricted by regulations of February 1940 (three zones) | https://en.wikipedia.org/wiki/White_Paper_of_1939 |
| 15 May 1941 | Palmach founded (Lag BaOmer). en.wiki gives 14 May; he.wiki's date is used | https://he.wikipedia.org/wiki/הפלמ"ח |
| 1934–1948 | Aliyah Bet. Patria reached Haifa on 1 Nov 1940 and was sunk by the Haganah to stop a deportation (date *to verify*). Struma sunk about 24 Feb 1942. Exodus 1947 intercepted 18 Jul 1947. Up to 50,000 held in the Cyprus camps. The source gives conflicting totals (*to verify*) | https://en.wikipedia.org/wiki/Aliyah_Bet |
| 20 Sep 1944 | Jewish Brigade announced, about 5,000 soldiers | https://en.wikipedia.org/wiki/Jewish_Brigade |
| Nov 1944 – Feb 1945 | The Saison | https://en.wikipedia.org/wiki/Haganah |
| 29 Nov 1947 | UN General Assembly Resolution 181: 33 for, 13 against, 10 abstentions. The Jewish Agency accepted | https://en.wikipedia.org/wiki/United_Nations_Partition_Plan_for_Palestine |
| 19 Nov 1947 | Haganah conscription began; 21,000 conscripted by the end of March 1948 | https://en.wikipedia.org/wiki/Haganah |
| 30 Nov 1947 – 14 May 1948 | The civil war phase | https://en.wikipedia.org/wiki/1947–1948_civil_war_in_Mandatory_Palestine |
| 12 Apr 1948 | People's Council (Moetzet HaAm), 37 members; renamed the Provisional State Council at independence | https://en.wikipedia.org/wiki/Moetzet_HaAm |

## 3. The eras

### 3.1 `content/eras.yaml` (D1)

```yaml
eras:
  - id: ottoman
    name: The First Aliyot
    from: 1882-07-31               # Rishon LeZion founded (§2)
    to: 1918-10-31                 # the Armistice of Mudros takes effect: Ottoman rule ends (§2)
    player_office:                 # new key (§4.1, §8.5)
      - { from: 1882-07-31, office: hovevei_zion_chair }
      - { from: 1897-08-29, office: zo_president }
    systems:
      legislature: none            # new value (§5.2)
      economy: national_funds      # new switch (§5.3)
      war_mode: false              # new switch: rung capped at 4, no ceasefire clock (§5.6)
      map_actions: [land_purchase, moshava, kibbutz]
    start: start/ottoman.yaml
  - id: mandate
    name: The Mandate
    from: 1918-10-31
    to: 1948-05-15
    player_office:
      - { from: 1918-10-31, office: zo_president }
      - { from: 1935-08-01, office: jae_chair }     # date to verify (§4.1)
    systems:
      legislature: assembly        # behaves as `none` until the first Assembly sits, 7 Oct 1920 (§5.2)
      economy: national_funds
      war_mode: false
      map_actions: [land_purchase, moshava, kibbutz, moshav, tower_and_stockade]
    start: start/mandate.yaml
    carry: carry/mandate.yaml
  - id: founding                   # unchanged, except that it gains player_office and a carry file (§8)
    player_office: [{ from: 1948-05-15, office: prime_minister }]
    ...
    carry: carry/founding.yaml
```

The chapters design's loader rules hold: eras are contiguous, `systems` keys are strict, and a key is added only with
the code behind it. `rules/ottoman.yaml` and `rules/mandate.yaml` hold only the keys these eras change (§3.3). The base
`rules.yaml` stays the Founding chapter's, unchanged.

**Why two eras, not one.** Rules change only at boundaries (chapters design §3.3), and the turn length, the outside
ruler and the legislature all change between 1917 and 1920. One era would need date checks inside rules. Two eras keep
each calibration test on its own (chapters design §1.2).

**Why Mudros (31 October 1918), not 1 July 1920.** The events that open the Mandate story come before July 1920:
Tel Hai (1 March 1920), the April 1920 riots, the first Assembly election (19 April 1920), San Remo (April 1920) and
the Haganah (June 1920). With a July 1920 boundary they would fall in the Ottoman chapter, under Ottoman rules.
Mudros is the end of Ottoman rule in the whole country and a single sourced date. The Mandate chapter then opens under
British military rule. The civil administration of 1 July 1920 and the Mandate's approval in 1922 are hard events
inside it.

| Option for the start | For | Against |
|---|---|---|
| **A. 31 July 1882 (recommended)** | The spec says 1882; the First Aliyah is the start of the story | 15 years before the Zionist Organization; the player's office is thin before 1897 (§4) |
| B. 29 August 1897 | A real institution from the first turn | Drops the First Aliyah. Rishon LeZion and the Rothschild colonies become start conditions |
| C. 31 October 1918 | The Mandate systems are close at hand | Drops 36 years, and with them the Second Aliyah, where the kibbutz and Hashomer begin |

### 3.2 Parts inside a chapter

A chapter has parts. A part is a heading in the top bar and on the chapter card, with no rule. Parts are UI strings
keyed by date in the era-aware UI item (#66). They are not `eras.yaml` keys, so the schema does not grow for display
text.

| Era | Part | From |
|---|---|---|
| ottoman | The Lovers of Zion | 31 Jul 1882 |
| ottoman | The Congress and the Second Aliyah | 29 Aug 1897 |
| ottoman | War | Ottoman entry into the war, 1914 (*to verify*) |
| mandate | British rule and the National Home | 31 Oct 1918 |
| mandate | Riots, revolt and the White Paper | 23 Aug 1929 |
| mandate | The world war and the Holocaust | 1 Sep 1939 |
| mandate | The struggle | 8 May 1945 (*to verify*) |
| mandate | The civil war | 30 Nov 1947 |

### 3.3 Turn length

The chapters design keeps quarter, month and week. This era adds a year, and the turn length on each rung is set in
data:

```yaml
# rules/ottoman.yaml (only the keys it changes)
turn_lengths: [year, year, year, quarter, quarter]   # rungs 1..5; today's default is [quarter, quarter, quarter, month, week]
# rules/mandate.yaml
turn_lengths: [quarter, quarter, quarter, month, month]
```

- **New code.**
  - `TurnLength.Year` in `SimTime`: advance one calendar year, with year fraction 1.0.
  - A `turn_lengths` rules key, read by `SecuritySystem.TurnLengthFor`. That method's signature changes from
    `(rung)` to `(c, date, rung)`. It has a Unity caller (`DefencePresenter`) and test callers.
  - `SecuritySystem.Weeks` gets an explicit year case (52), even though the clock never runs when `war_mode` is false.
  - The UI gets a "year" string.
- **The default.** It equals today's table, so the base `rules.yaml` does not set the key.
- **Turn counts.** The Ottoman chapter is about 36 yearly turns, plus quarters on rung 4 (the 1914–1918 war and the
  deportations). The Mandate chapter is about 118 quarters, plus months on rung 4 (the 1936–39 riots at their height
  and the civil war).
- **The adjusted turn** at a boundary (chapters design §4.1) is extended to a year. A turn is never more than its
  normal length plus 14 days, and never less than the smaller of its length and 14 days.
- **Grid drift.** Quarter turns from 31 July clamp the day on `AddMonths` (31 to 30 to 28), so the grid drifts by a
  few days. This is harmless. Yearly turns from 31 July do not drift.
- **Calibration note.** Effect inertia is counted in turns. Windowless effects such as `homes_lift_morale` respond
  about four times faster in real time under yearly turns. The Ottoman calibration sets inertia for its own effects
  with this in mind.

## 4. Who the player is (D2)

The spec says "the player leads the Yishuv". There was no head of state. The design follows the office that held the
movement's executive power and that became the Prime Minister's office.

### 4.1 The player's office, by date

Each era gets a dated `player_office` list (§3.1). It replaces the assumption, implicit today in the UI, in `Cabinet`
and in the game-over text, that the player is `prime_minister`. The Founding era lists `prime_minister`, so nothing
changes there.

| From | Office id | Title | Holders (all *to verify* against a source per term) |
|---|---|---|---|
| 31 Jul 1882 | `hovevei_zion_chair` | Chairman of Hovevei Zion | vacant until the Kattowitz conference (Nov 1884); Leon Pinsker to 1891; later holders to research |
| 29 Aug 1897 | `zo_president` | President of the Zionist Organization | Herzl 1897–1904, Wolffsohn 1905–1911, Warburg 1911–1920, Weizmann 1920–1931, Sokolow 1931–1935 |
| 1935 | `jae_chair` | Chairman of the Jewish Agency Executive | Ben-Gurion 1935–1948 (checked: the Jewish Agency article) |

- **Hand-over to the Prime Minister.** `jae_chair` has `until: 1948-05-14` and `successor: prime_minister` (chapters
  design §4.4). Ben-Gurion's Prime Minister term already starts on 14 May 1948 in `people.yaml`, so nothing moves.
- **`zo_president` after 1935** continues as an office the player does not hold (Weizmann 1935–1946, *to verify*). It
  answers events on diplomacy in London and at the League of Nations.
- **Titles are exact.** Weizmann was President of the Zionist Organization and of the Jewish Agency. Ben-Gurion
  chaired the Jewish Agency's Executive. The office titles keep that distinction.
- **A vacant player office.** The office may be vacant. Events that fire then do not use its placeholder: the loader
  already refuses a placeholder with no holder (`docs/WRITING_EVENTS.md`). The UI shows the institution's name in its
  place ("the Hovevei Zion movement").
- **Game-over text** follows the player's office: "Your leadership of the movement is over", not "Your premiership is
  over" (`PoliticsSystem.UpdateStability`).

| Option | For | Against |
|---|---|---|
| **A. The Zionist executive chain (recommended)** | It held money, immigration, settlement and diplomacy, the levers the systems model, and it leads to the Prime Minister | Three offices to explain; a vacancy in 1882–84 |
| B. The Vaad Leumi chair | A Yishuv-only body, elected locally | It controlled education, health and welfare. It did not control immigration, land, the Haganah after the 1930s or diplomacy |
| C. An unnamed "Yishuv leadership" | No vacancies | Breaks the real-names rule (spec §1) |

### 4.2 The executive (the "cabinet")

These are the offices the player appoints and delegates to, each with a successor in the Founding chapter where one
exists. Holders and dates are research (§12). The examples are the ones the sources above name.

| Office id | Title | Example holder | Successor at 15 May 1948 | Budget line carries to |
|---|---|---|---|---|
| `palestine_office_head` | Head of the Palestine Office (1908–) | Arthur Ruppin | none (ends before 1948) | — |
| `ja_political_head` | Head of the Political Department | Arlosoroff, then Sharett (*to verify*: 1933–1948) | `foreign_minister` | **dropped** (no Founding line) |
| `ja_treasurer` | Treasurer of the Jewish Agency | Kaplan (*to verify*: 1933–1948) | `finance_minister` | `finance_minister` |
| `ja_settlement_head` | Head of the Settlement Department | research | `agriculture_minister` | `agriculture_minister` |
| `ja_immigration_head` | Head of the Immigration Department | research | `prime_minister` | `prime_minister` (the `rules.yaml` comment already puts absorption there) |
| `ja_labour_head` | Head of the Labour Department | research | `labour_minister` | `labour_minister` |
| `vaad_leumi_chair` | Chairman of the Vaad Leumi | Yellin, Rutenberg, Ben-Zvi, Remez | none | **dropped** |
| `vaad_leumi_education` | Head of the Vaad Leumi Education Department | research | `education_minister` | `education_minister` |
| `vaad_leumi_health` | Head of the Vaad Leumi Health Department | research | `health_minister` | `health_minister` |
| `vaad_leumi_local` | Head of the Vaad Leumi Local Communities Department (*to verify* the name) | research | `interior_minister` | `interior_minister` |
| `haganah_command_head` | Head of the Haganah National Command | research | `defense_minister` | `defense_minister` |
| `haganah_chief_of_staff` | Haganah Chief of Staff | Yaakov Dori (the existing term from 1 June 1947, split at 14 May 1948; §8.4) | `chief_of_staff` | **dropped** (no Founding line) |

**The coalition records.** In the Ottoman chapter there is no coalition record. In the Mandate chapter, each
`governments.yaml` record is the coalition of the national institutions for one period: the parties of the Vaad Leumi
executive, which the Assembly elected (§5.2). Its from/to are the dates that executive was formed and replaced.
`Cabinet.ExpireOnGovernmentChange` then ends player appointments when that coalition changes, as today. Jewish Agency
Executives that change between Assembly elections (after a Zionist Congress) are hard events that set or end terms in
`people.yaml`, not separate government records.

## 5. Systems: reuse and new

| System (spec §3) | Pre-state form | Reuse | New (each behind a switch or a window) |
|---|---|---|---|
| Effects web | Indicators, policies and effects of the Yishuv | `EffectsSystem` as is | Windowed records (`to: 1948-05-15`) |
| Legislature | None (Ottoman); the Assembly as a consent body (Mandate) | `Offices.Legislature`, `PoliticsSystem` seats and coalition; #58's `assembly` value | `legislature: none`; the consent cost (§5.2) |
| Ministers | The Executive | `Cabinet`, `people.yaml`, `governments.yaml` | `player_office` by date (§4.1) |
| Budget and money | National funds | Treasury, Debt, budget lines | `economy: national_funds` (§5.3) |
| Population and aliyah | Waves, Aliyah Bet, emigration | `PopulationSystem`, the `aliyah.yaml` series and regions | One arrivals lever and an emigration series (§5.4) |
| Outside powers | Ottoman Empire, the British administration, Arab leadership, dissidents | `model.yaml` actors (standing only, not in state) | Windowed actors; behaviour waits for #11 (§5.5) |
| Defence | Hashomer, Haganah, Palmach | The ladder, mobilisation, attention by rung | `war_mode: false`; turn lengths per rung (§3.3, §5.6) |
| Map | Land purchase and settlement | `MapActions`, `Placements`, dated settlements (#61), #58's `map_actions` list | New actions; player settlements count as settlements (§5.7) |
| Events | Hard anchors and dilemmas | `EventSystem`, `requires` across eras | The boundary rule for pending events (§8.3) |

### 5.1 The model (`model.yaml`)

**Kept, windowless.** The five indicators (`morale`, `food_supply`, `housing`, `security`, `intl_standing`) mean the
same before 1948 and stay active in every era. The four voter groups (`veterans`, `new_immigrants`, `religious`,
`labour_movement`) also stay: the pre-state public had the same blocs.

**Windowed to the Founding chapter** (`from: 1948-05-15`). The four existing policies (`rationing`,
`housing_construction`, `border_settlement`, `maabarot`) and the four effects that read them, because their offices
are state ministries. The offices get `from: 1948-05-14`. This is golden-safe (§8.4).

**New, windowed `to: 1948-05-15`.** These are design values. The names are the player-facing ones.

| Kind | Id | Meaning | Office |
|---|---|---|---|
| Indicator | `national_authority` | How far the Yishuv accepts the national institutions' discipline: levy yield, friction with the dissidents | — |
| Indicator | `employment` | Work for new arrivals; the 1926–27 crisis, when emigration passed immigration (*to verify*) | — |
| Policy | `land_purchase` | Pace of JNF and private purchase | `ja_settlement_head` |
| Policy | `hebrew_labour` | Preference for Jewish labour in Jewish enterprises | `ja_labour_head` |
| Policy | `pioneer_selection` | Share of labour certificates kept for trained pioneers (halutzim), against family and rescue cases | `ja_immigration_head` |
| Policy | `aliyah_bet` | Immigration without British certificates (from 1934) | `ja_immigration_head` |
| Policy | `havlagah` | Restraint: defence without reprisal (1936–39) | `haganah_command_head` |
| Policy | `cooperation_with_britain` | Enlistment and intelligence cooperation, or resistance | `ja_political_head` |
| Policy | `hebrew_schools` | Hebrew-language national schooling | `vaad_leumi_education` |
| Policy | `voluntary_levy` | Kofer HaYishuv and the emergency taxes (from 24 Jul 1938) | `ja_treasurer` |
| Policy | `settlement_drive` | Settlement for its own sake and for future borders | `ja_settlement_head` |

- **Ottomanization** (urging Jews to take Ottoman nationality, 1914) is an event, not a policy. It was one decision.
- **Defaults and history (§1.7).** Each policy's `default` is its historical level when its window opens. For example,
  `aliyah_bet` opens in 1934 at its 1934 level. Changes such as the pause of 1942–44, the surge of 1945–48, `havlagah`
  in 1936, `cooperation_with_britain` in 1939 and 1945, and `voluntary_levy` in 1938 are hard events' `set_policy` on
  sourced dates.
- **Effects and situations** follow the same pattern as today. For example, `settlement_drive` raises `security` with
  inertia, as `border_settlement` does; low `employment` starts a situation that raises emigration. The full list is a
  design item (§12).
- **Policy successor.** The Founding carry file maps `settlement_drive` to `border_settlement` as a policy successor
  (chapters design §4.2).

### 5.2 The legislature (D3)

- **Ottoman: `legislature: none`.** No body sits. `SetPolicy` takes effect without a vote and draws nothing from the
  RNG; today it refuses when no body sits (`Simulation.Execute`). Coalition stability still moves with approval, and it
  still ends the playthrough below 0.2. The Zionist Congresses are hard events; the Uganda vote of 1903 is the clearest.
- **Mandate: `legislature: assembly`.** #58 builds this switch value ("an assembly's votes do not bind"). This design
  adds what a lost vote costs.
  - Each Assembly is a `legislatures.yaml` record: `elected` is the election date, and the window runs from the first
    session. The first Assembly is `elected: 1920-04-19` and sits `from: 1920-10-07`. Seats are by list, from sources.
  - From 31 October 1918 to 7 October 1920 no body sits, and `assembly` behaves exactly as `none`.
  - The People's Council (37 members, from 12 April 1948) follows the last Assembly. It ends on 14 May 1948, the day
    the existing Provisional State Council record begins.
- **The vote.** A vote is held as today (`PoliticsSystem.Vote`). The coalition counted is the Vaad Leumi executive's
  parties (§4.2), the body the Assembly elected and could refuse. With a majority and stability at or above 0.5, the
  vote passes. Below 0.5 it passes with probability 2 × stability, using the same RNG draw as `state`.
  - **A lost vote does not block.** The policy is set, and `national_authority` falls by a rules value
    (`assembly_dissent_cost`), with a Why line ("The Assembly did not endorse ..."). The Assembly had no power to
    compel; consent was the currency.
  - **No majority.** If a period's coalition has no majority, the vote is lost with no RNG draw, as today, and every
    policy change pays the dissent cost. That is a real state of affairs, not a bug: a test checks that each Assembly
    period's majority status matches the sourced composition (Yishuv research).
- **`budget_approval`** stays a field on each record. For the Assembly records it starts `false` with
  `open_questions: [budget_approval]`, as the Founding records do, until a source says how the Vaad Leumi and Jewish
  Agency budgets were approved.
- **Boycotts.** The Fourth Assembly election of 1944 was boycotted by several lists (*to verify* which). A boycott
  shrinks the body whose consent the player can win.
- **Parties span eras.** `parties.yaml` gains the pre-state lists (Ahdut HaAvoda, Hapoel Hatzair, the Revisionists,
  the Left Bloc and others) with no windows.

| Option for the Mandate body | For | Against |
|---|---|---|
| **A. The Assembly as a consent body (recommended)** | Local and elected; the forerunner of the councils of 1948 (chapters design §4.4) | It elected the Vaad Leumi, not the Jewish Agency Executive |
| B. The Zionist Congress | It elected the Executive | A world body that met every two years; sourcing delegates per list for 13 congresses is large |
| C. Both | Exact | Two seat tables per date and two vote rules; deferred unless play needs it |

### 5.3 Money: the national funds (`economy: national_funds`)

There was no state, no tax law and no currency of the Yishuv's own.

- **Accounts.**
  - **Treasury** is the national funds: the Jewish Agency's budget, with Keren Hayesod, the JNF and, from 1938, Kofer
    HaYishuv.
  - **Debt** stays (the institutions borrowed).
  - **Foreign currency** is not tracked. The switch skips trade and the reserves line, and the UI hides it.
    `FxReserves` keeps its start value (§8.1 shows why that carries exactly).
- **Income:**
  - donations from abroad (`donations_per_year` in the era rules, sourced and calibrated by period);
  - voluntary levies per person, scaled by the policy and indicator that the era rules name (`levy_policy:
    voluntary_levy`, `authority_indicator: national_authority`).

  The Founding switch value (`economy: state`) is today's `EconomySystem.Apply`, unchanged.
- **Spending** is per person, as today, plus the Haganah's cost through mobilisation (`mobilization_cost_per_year` in
  the era rules). A calibration anchor to verify: the Haganah budget of £400,000 in 1946 and £3.3 million by October
  1947 (Perlmutter, cited in the Haganah article).
- **Budget lines** are the Executive's departments (§4.2). `Allocate` checks them against the active era's
  `budget_start`, so the rule "every ministry, and only them" reads the active era's lines.
- **Units.** The sim keeps one internal unit (chapters design D5). The display follows `currency.yaml` (#66): the
  Ottoman lira and the Egyptian pound before 1927 (*to verify*), the Palestine pound from 1927, the Israeli pound
  after. The Palestine pound passed to the Israeli pound at par (*to verify*), so one internal unit serves both sides
  of the boundary.

### 5.4 Population and aliyah

- **Groups.**
  - `old_yishuv` is the community before 1882. It needs a region record with a display name ("the old Yishuv"), so
    `PlayerText.Region` can name it.
  - The existing region groups (`asia`, `africa`, `europe`, `america_oceania`, `unknown`) are kept; the 1919–1948 CBS
    table already uses them.
  - The start population is 24,000 Jews in 1882 (§2). Its split is research.
- **Series, Mandate.** `aliyah.yaml` gains the five CBS periods from 1919 to 14 May 1948, by continent (§2). This is
  reuse: `PopulationSystem.Arrivals` spreads each period evenly by day, as for 1948–51. The 1918 months before the
  first period need a row or a derived zero (research).
- **Series, Ottoman (§1.6).** No single-valued arrival totals exist, so rows are **derived** from the population
  anchors (24,000 in 1882; 94,000 in 1914; any intermediate anchor the research finds) with natural increase and
  emigration. Each derived row says so in its comment, gives the arithmetic and cites the anchors.
- **Emigration (new).**
  - Many left: 50–70% of the First Aliyah by the source above, which is a range again, so the model takes emigration
    from anchors too.
  - The fall during the war, from 94,000 in 1914 to 83,794 at the 1922 census, came from deportation, hunger and
    flight.
  - The mechanism is an `emigration` series of dated rows (people per year) and a rule `emigration_per_year` that the
    low-`employment` situation raises. The default is 0, so the Founding chapter is unchanged.
- **One lever, no legal/illegal split.**
  - The CBS period totals already include the people who came without certificates (Aliyah Bet), and no source in §2
    splits them. So the design does not model the certificate quota and Aliyah Bet as separate streams.
  - The factor reads the game's own policy history, not a value on the series row. A row's period (for example
    1939–45) spans lever changes (the 1942–44 pause), so one level per row could not match the history inside it.
  - **The historical level** of `aliyah_bet` on a turn is computed from the state: the policy's `default`, then the
    `set_policy` value of the `historical` option of each event that sets it and has been resolved in this game
    (`ChosenOptions`), applied in the order `EventSystem.Trigger` uses (`from`, then id). It is derived each turn and
    needs no new state field. The loader refuses two events with overlapping windows that both set `aliyah_bet`, so the
    order in which a player answers them never matters.
  - Arrivals over the turn are multiplied by `1 + k × (level − historical level)`, where `level` is the policy's
    current value and `k` is a rules value. Before the policy's window opens (1934) the factor is 1.
  - **Exactly 1.0 on the reference run.** The `HistoricalAI` sends no `SetPolicy` and picks each historical option, so
    the policy's value is always the result of the same assignments as the historical level: the difference is 0.0
    and the factor is exactly 1.0 on every turn. The reference run therefore reproduces the series exactly, whatever
    turn an event fires on. A player who picks a non-historical option or sets the policy directly moves the factor.
  - The White Papers of 1922, 1930 and 1939 are hard events. Their historical options set `aliyah_bet`, and their
    options cost `intl_standing` and `national_authority` according to the response the player chooses.
  - `pioneer_selection` moves morale and employment, not totals.
  - Test: on the golden-seed reference run the factor is exactly 1.0 on every turn, and the arrivals equal
    `PopulationSystem.Arrivals` of the series.
- **Calibration** is to the anchors: 1914, the 1922 and 1931 censuses, 1947, and the Founding start of 649,400 on
  15 May 1948 (`rules.yaml initial_population`), by the chain check of chapters design §12.

### 5.5 Outside powers

Actors are records in `model.yaml` with a starting standing. **`GameState` has no actor standing.** That state belongs
to the diplomacy design (#11). Until #11 lands:
- costs that would fall on "British standing" fall on `intl_standing`, and on `national_authority` where the cost is
  internal;
- actors act through hard events and through the rules they set, such as the White Papers' immigration settings (§5.4).

| Actor id | Name | Window | Notes |
|---|---|---|---|
| `ottoman_empire` | Ottoman Empire | to 1918-10-31 | restrictions on entry and land purchase; wartime deportations |
| `british_administration` | The British administration | 1918-10-31 to 1948-05-15 | military rule, then the High Commissioner from 1 Jul 1920 (*to verify*): certificates, land law, police (chapters design §4.4) |
| `uk` | United Kingdom (existing) | windowless | the government in London: the Balfour Declaration and the White Papers |
| `league_of_nations` | League of Nations | 1920 to 1946 (*to verify*) | the Permanent Mandates Commission |
| `un` | United Nations (existing) | `from: 1945-10-24` (*to verify*) | UNSCOP, Resolution 181 |
| `arab_leadership` | The Arab leadership in the Land of Israel | 1918-10-31 to 1948-05-15 | the Arab Executive, then the Arab Higher Committee; Israeli-perspective naming |
| `irgun`, `lehi` | Irgun; Lehi | 1931 and 1940 (*to verify*) to 1948-05-15 | the dissidents (porshim): friction, the Saison, the Resistance Movement |
| `usa`, `france` | (existing) | windowless | Biltmore, the 1947 vote |
| `ussr`, `egypt`, `jordan`, `syria`, `lebanon`, `iraq`, `arab_refugees`, `arab_citizens`, `west_germany` | (existing) | `from: 1948-05-15` | state-era actors, golden-safe (§8.4). The Arab states' roles before 1948 appear through events |

### 5.6 Defence without a state

- **The ladder is always on** (spec §3). The rung meanings carry over:
  1. routine (watchmen, theft, field disputes);
  2. a wave (April 1920, May 1921 *to verify*, August 1929);
  3. limited operations;
  4. prolonged conflict (1936–39, and the civil war from 30 November 1947).
- **`war_mode: false`** caps the rung at 4 and turns off the UN ceasefire clock. There was no army, and no state on
  which to impose a ceasefire. Rung 5 opens on 15 May 1948 by D5(a).
- **Mobilisation** is the call-up of the Haganah: the field companies, the Palmach from 15 May 1941, and conscription
  from 19 November 1947. Its cost falls on the national funds. `rung_min_mobilization` is set in the era rules.
- **The organisations** are offices (§4.2), policies (`havlagah`, `cooperation_with_britain`) and actors (`irgun`,
  `lehi`). Friction with the dissidents lowers `national_authority`. The Haganah's command passed from the Histadrut to
  the national institutions in the 1930s (*to verify*); that is an event.
- **Hashomer** (Ottoman period) is an event chain plus the `security` effect of `settlement_drive`. It was not under
  the movement's command.
- **Defence depth** (#22: command traits, intelligence estimates, procurement) applies here unchanged when it lands.
  The arms purchases in Czechoslovakia in 1947–48 (*to verify*) are its first use before 1948.

### 5.7 Map actions

| Action | From | Needs | Effect (design values) |
|---|---|---|---|
| `land_purchase` | 1882 | a land hex within reach of a settlement, with no placement | money; marks the hex as bought; costs `intl_standing` after the 1939 White Paper |
| `moshava` | 1882 | a bought hex | private farming village; housing, employment |
| `kibbutz` | 1910 (Degania) | a bought hex | housing, security, morale; labour movement support |
| `moshav` | 1921 (Nahalal, *to verify*) | a bought hex | housing, food supply |
| `tower_and_stockade` | 10 Dec 1936 to Oct 1939 | a bought hex; rung 3 or higher | built in one turn; security |

- **How a bought hex is recorded.** A `land_purchase` placement is the hex's purchase. A settlement action replaces
  that placement on the same hex (`MapActions.Apply` removes what was there, as housing replaces a camp today). A
  settlement placement therefore also means the hex was bought. No new state is needed.
- **The action list** is #58's `map_actions` switch. This design adds the five actions and their rules in the era rule
  files. `transit_camp` and `housing` stay the Founding chapter's list. #23 adds state-era kibbutz and moshav actions;
  the two share rule fields where they can.
- **Player settlements count as settlements** for the "within one hex of a settlement" check when their rule says
  `counts_as_settlement: true`. The two Founding actions do not set it, so the Founding check is unchanged.
- **D4: history is the floor.**
  - Historical settlements appear from the dated settlements file on their dates, in every game (#61). The
    `HistoricalAI` places nothing, so the reference run has no player placements. A player's placements carry into
    1948 as extra places on the map.
  - If a player buys or settles a hex where history later founds a settlement, both stand: the historical place appears
    on its date next to the player's placement, with no refund and no conflict.
- **Deferred: the 1940 land zones.** The Land Transfer Regulations' three zones would make `land_purchase` harder in
  zones A and B. That needs a hex-to-zone file and a tool for one rule. It is deferred until play shows the purchase
  lever needs it. The zone line itself is drawn as a dated line (§7).

## 6. Events and content scope (D7)

| Part | Events (about) | Decision scenes | Hard anchors (examples) |
|---|---|---|---|
| The Lovers of Zion, 1882–1897 | 8 | 1 | Rishon LeZion; the Baron's patronage; the first Ottoman restrictions |
| The Congress and the Second Aliyah, 1897–1914 | 12 | 1 | First Congress; JNF; Uganda vote; Palestine Office; Hashomer; Tel Aviv; Degania |
| War, 1914–1918 | 10 | 0 | Ottomanization; deportations; the Jaffa evacuation of 1917; the Balfour Declaration (2 Nov 1917); the British conquest; the Jewish Legion |
| British rule and the National Home, 1918–1929 | 20 | 1 | Tel Hai; the 1920 riots; the Assembly election; San Remo; the Haganah; the Histadrut; the civil administration; 1921 riots; 1922 White Paper; the Jezreel Valley purchase; the 1926–27 crisis |
| Riots, revolt and the White Paper, 1929–1939 | 25 | 2 | 1929 riots; Passfield; Haavara; Arlosoroff; the 1936–39 riots; Peel; tower and stockade; Kofer HaYishuv; the 1939 White Paper |
| The world war and the Holocaust, 1939–1945 | 15 | 2 | enlistment; Palmach; Patria; Struma; Biltmore; the news from Europe; the parachutists; the Brigade; the Saison |
| The struggle, 1945 – 29 Nov 1947 | 15 | 2 | Resistance Movement; Black Sabbath; Anglo-American Committee; the eleven Negev points; Exodus; UNSCOP; Resolution 181 |
| The civil war, 30 Nov 1947 – 14 May 1948 | 15 | 1 | conscription; the convoys and Jerusalem; Plan Dalet; Nachshon; the People's Council; the decision to declare |
| **Total** | **about 120** | **10** | |

**The ten decision scenes** are each a real choice the record shows. Options and texts are the research's job.

1. **The Baron's patronage** (1880s): accept Rothschild's administrators and money, or keep the colonies' autonomy.
2. **The Uganda vote** (1903): send the expedition, or refuse any land but the Land of Israel; the movement nearly
   splits.
3. **Tel Hai** (1920): hold or withdraw the Upper Galilee outposts.
4. **Restraint or response** (1936–39): `havlagah`, the Jewish Settlement Police, tower and stockade.
5. **Peel** (1937): negotiate on partition, or reject it.
6. **The White Paper and the war** (1939): fight the White Paper, enlist against Germany, or both; Aliyah Bet.
7. **The Saison** (1944–45): hand dissidents to the British, act alone, or not at all.
8. **The Resistance Movement** (1945–46): join the Irgun and Lehi against the British, and when to leave.
9. **Partition** (1947): accept Resolution 181.
10. **Declare the state** (May 1948): declare on 14 May, or accept a truce and delay (*to verify*: the People's
    Administration vote of 12 May 1948). Its historical option is the one the Founding chapter's events may `require`.

These choices are ordinary events with options, not scenes: the language dispute of 1913 (*to verify*),
Ottomanization (1914), the command of the Haganah, Haavara (1933) and the eleven points (1946).

**The Holocaust** is reacted to, never played (spec §1). Events present the Yishuv leadership's real choices: rescue
funds, Aliyah Bet, the parachutists, and the response to the news from Europe. They carry the `holocaust` tag. No option
grants treasury, and no art shows a swastika.

**Writing order.** The batches are written from the last backward: 1945–1948 first, so the boundary (§8) is exercised
by real content early. Then 1882–1918, 1918–1939 and 1939–1945.

## 7. The map in these eras

| Layer | Content | Built by |
|---|---|---|
| Settlements | Every Jewish locality founded before 15 May 1948, with `founded` dates, renames (Mas'ha to Kfar Tavor, 1903) and `until` where a place was abandoned (Hartuv, Kfar Etzion; *to verify*). Arab towns and villages are `pre_modern` rows, as today | #61's dated file; research by region |
| Lines | the 1906 Sinai line, the separation of Transjordan (1922), the Mandate border (1923), the Peel proposal (1937), the 1940 land zones (drawn only, §5.7), the UN partition lines (1947). Ottoman district borders only if an event needs them | #63 `lines.yaml`; every line is sensitive (chapters design §13) |
| Water | Lake Hula and the Dead Sea at period levels; no modern reservoirs | #16 (one coast mask per water epoch) |
| Placement limits | `map_action_max_distance` and the cap per turn | `rules/<era>.yaml` |

Naming follows the perspective rule. The British official name, Palestine, is used where it is the official name (the
Mandate, the Palestine pound). Narration says the Land of Israel.

## 8. The 15 May 1948 boundary

### 8.1 What crosses

The chapters design's table (§4.2 there) applies. For this boundary, concretely:

| Mandate-era thing | At 15 May 1948 | Mechanism |
|---|---|---|
| `jae_chair` and the Executive offices | end; budget lines pass to their successors (§4.2); lines with no Founding budget line are dropped and listed in the carry file | `offices.yaml` `until` + `successor`; `carry/founding.yaml` |
| Their holders | continue in their sourced state offices | `people.yaml` terms; nothing moves |
| The People's Council | ends 14 May 1948; the Provisional State Council record (from 14 May 1948) sits | `legislatures.yaml` windows; `legislature: state` |
| National funds (Treasury), Debt | deviation carry onto `treasury_start` (20) and debt 0 | `carry/founding.yaml` |
| `FxReserves` | deviation carry onto `fx_start` (40). The Yishuv never changes it, so player equals reference and the result is exactly 40 | same |
| Population (all groups) | mapped to `veterans`, then ratio carry onto 649,400 | same; chapters design §4.3 |
| Policies | Yishuv policies end with their windows; `settlement_drive` maps to `border_settlement` by deviation; Founding policies start at their defaults | carry file; `ContentWindows.Update` |
| **Indicator shifts** | **deviation carry for all five windowless indicators, onto 0.** Without it, 66 years of event shifts would reach 1948 in the historical chain, and the played-through Founding chapter would not be the calibrated one (chapters design §1.2) | the Founding carry file lists the five shifts with start 0 |
| **Coalition stability** | **deviation carry onto 1.0** (a fresh start's value), clamped to 0–1 | carry file |
| Rung | **set to the Founding start rung, 5** (D5a) | new carry rule `rung: start` |
| Mobilisation | persists, then raised to the rung 5 floor (0.8) by `SecuritySystem.SetRung` | as today |
| Ceasefire clock | started at `ceasefire_weeks` (52) by `SetRung` from rung 4 to 5, as in a fresh start | as today |
| Budget | deviation carry per Founding line onto `budget_start` (D5b) | §8.2 |
| Placements | persist; player settlements stay and count | `Placements` |
| `ChosenOptions`, `FiredEvents` | persist; the Founding events may `require` Yishuv choices | event `requires` |
| `PendingEvents` | no Mandate event crosses (§8.3) | new boundary rule |
| Actors | Yishuv actors' windows end; `uk`, `usa`, `france` and `un` continue; state-era actors begin | windows |

### 8.2 The two carry rules (D5)

**(a) `rung: start`.** The chapters design says the rung persists. Here it may not:
- The chain must equal a fresh Founding start bit for bit (chapters design §6), and a fresh start opens at rung 5.
- The alternative, a 15 May "invasion" event that sets rung 5, would also fire in a fresh Founding start and move
  `Hash1950`.

The rule: when the carry file says `rung: start`, `EraSystem.Cross` calls `SecuritySystem.SetRung` with the next era's
`start_rung`. From rung 4 this gives exactly the fresh start's state: rung 5, a ceasefire clock of 52 weeks
(`CeasefireWeeksAfter(4, 0, 5)` equals `(1, 0, 5)`), and mobilisation raised to 0.8. The `HistoricalAI` holds
mobilisation at the rung floor, so the historical chain lands on 0.8 exactly. The rule uses no RNG.

**(b) Budget by deviation.** Under the chapters design (§4.2), the budget is mapped through successors and rescaled to
sum to 1. A historical chain would then enter 1948 with the mapped Yishuv shares, not with `budget_start`. That breaks
the carry invariant by whole shares, not only in the last bit.

The rule, for each Founding line:
1. Sum the player's Yishuv lines that map to it, and sum the reference run's lines that map to it.
2. The new share is `start + (player − reference)`, clamped at 0.
3. Rescale only if any deviation is non-zero.

Lines with no Founding successor line are dropped before step 1, and the carry file lists them. The historical chain
then lands on `budget_start` bit for bit.

### 8.3 The boundary turn and pending events

The Mandate chapter's last turns are monthly (rung 4) on the historical path. The turn that would pass 15 May 1948 ends
on that date (chapters design §4.1). The Founding chapter's first turn then starts on 15 May 1948, the date a fresh
start uses, so both share the calendar from the boundary on.

**Pending events never cross into an era where their records are gone.** Today `EventSystem.Trigger` runs after the
date moves and raises every event whose window overlaps the turn just played. On the boundary turn it would raise
Mandate events (`to: 1948-05-15`) that are answered in the Founding chapter, after `ContentWindows.Update` has removed
`national_authority`, `aliyah_bet` and the rest. `EffectsSystem.Shift` would then fail on a missing key. The rule, in
`Simulation.Step` on a turn that ends on a boundary:

1. the normal turn, ending on the boundary;
2. `EventSystem.Trigger` for the **ending** era's events only (their windows overlap the turn just played);
3. every pending event of the ending era is resolved with its `historical` option. An event without one takes its
   `recommended` option, else its first. This uses no RNG. A Why line says "Decided as history records: ...";
4. `ContentWindows.Update`, then `EraSystem.Cross` (the carry);
5. `Cabinet.ExpireOnGovernmentChange`, `AttentionSystem.Refill`, and `EventSystem.Trigger` for the **new** era's
   events.

**Scenes must not be auto-resolved on the historical grid.** The "Declare the state" scene opens on 12 April 1948. On
the historical monthly grid it is therefore raised by a turn that ends before the boundary, and the player answers it in
the Mandate chapter. A player still on quarterly turns may see it decided as history records. Tests:
- after every boundary crossing, `PendingEvents` holds no event outside the new era;
- on the historical chain, the declaration scene is answered by a choice, not auto-resolved;
- the auto-resolve draws no RNG.

### 8.4 Golden safety

Each item in §12 keeps the Founding goldens, by the chapters design's checklist (§8 there):

1. **New records** have windows that end on or before 15 May 1948. They are never active in a run that starts on
   15 May 1948.
2. **Existing records windowed to the Founding chapter** (§5.1, §5.5) get `from: 1948-05-15` (offices
   `from: 1948-05-14`).
   - A fresh Founding start initialises from the active view on 15 May 1948, which holds exactly today's keys.
   - The hashed `ind{}`, `pol{}`, `eff{}` and `shift{}` maps are therefore unchanged.
   - Actors are not in `GameState`.
3. **Dori's term.** `people.yaml` gives Yaakov Dori `chief_of_staff` from 1 June 1947. Once the office has
   `from: 1948-05-14`, that term would fail the Chapters 2 check that a term lies inside its office's window. The term
   is split:
   - `haganah_chief_of_staff` from 1947-06-01 to 1948-05-14;
   - `chief_of_staff` from 1948-05-14 to 1949-11-09, with the same sources.

   The `&cos` YAML anchor stays on the first of the two entries, because Yadin's term reuses it (`*cos`).

   From 15 May 1948 `Cabinet` finds the same holder, so nothing hashed moves.
4. **New switches** (`legislature: none`, the consent cost, `economy: national_funds`, `war_mode`, `turn_lengths`,
   emigration, the arrivals factor, `counts_as_settlement`) default to today's behaviour. The Founding era sets no
   override file.
5. **New aliyah rows** end on 14 May 1948. `Arrivals` treats `to` as inclusive, so a run from 15 May 1948 sees none of
   them.
6. **New events** close by 15 May 1948. An event may not span a boundary.
7. **The boundary** is the Founding era's `from`. `Simulation.NewGame(content, seed, mode)` still means `founding`, and
   `GoldenRun.Play` does not cross the boundary. The pending-event rule (§8.3) runs only on a turn that ends on a
   boundary.
8. **No new `GameState` field.** The era and the player office come from `Date`. Land is held as `Placements`.

`Content.Hash` changes with every new file, as it does for any content change. Saves warn once and replay unchanged.

### 8.5 Changes this design asks of the chapters design

Every change, and the chapters item it belongs to:

| Change | Chapters item |
|---|---|
| `eras.yaml` gains a dated `player_office` list (strict key) | #56 (Chapters 1) |
| `TurnLength.Year`, the `turn_lengths` rules key, the new `TurnLengthFor(c, date, rung)` signature, and the adjusted-turn rule extended to years | #58 (Chapters 3) for the key, #59 (Chapters 4) for the adjusted turn; built by Yishuv 2 |
| New switches `war_mode` and `economy`, the value `legislature: none`, and the consent cost on #58's `assembly` | #58 builds the switch mechanism, `assembly` and `map_actions`; Yishuv 2–4 add the rest, each with its code (chapters design §5) |
| `counts_as_settlement` on a map-action rule; new actions on #58's `map_actions` list | #58 (the list); built by Yishuv 6 |
| D5(a) carry rule `rung: start` | #59 |
| D5(b) budget by deviation per next-era line, with no rescale when nothing moved, and a list of dropped lines | #59 (replaces "budget lines move to successors and sum to 1") |
| Pending events of the ending era resolved before the crossing; a split `Trigger` on the boundary turn (§8.3) | #59 (replaces "pending events stay answerable") |
| Coalition stability carried by deviation | #59 |
| Era goldens are pinned once the eras exist as data, and later content PRs name the goldens they move (§9) | #60 (Chapters 5) |

Parts (§3.2) are UI strings in #66, not an `eras.yaml` key. The chapters design §4.2 row "Budget: mapped ... rescaled"
and row "`PendingEvents`: persist" become "per the carry file's rules". These are proposals; #59 takes them only on the
owner's yes to D5.

## 9. Fresh starts, calibration and goldens

- **Fresh starts** (chapters design D2). `start/ottoman.yaml` (1882) and `start/mandate.yaml` (31 October 1918) hold
  sourced start values: population by group, national funds, debt, budget lines, start rung, and the policy levels and
  indicator shifts the calibration reads. The Mandate start's population is derived from the anchors, with the
  arithmetic in a comment.
- **Calibration per era** (`OttomanCalibrationTests`, `MandateCalibrationTests`), each on a fresh start:
  - population against the anchors (§5.4);
  - national funds against the sourced Jewish Agency and Kofer budgets (research);
  - land held against the JNF figure for 1948 (§2).
- **Goldens.** `Hash_ottoman_1900`, `Hash_mandate_1930`, and a chain golden from 1882 into the Founding chapter to
  1950. Each is pinned in the three places `docs/determinism.md` names. The carry reference files
  `carry/mandate.reference.yaml` and `carry/founding.reference.yaml` are regenerated by the tool and checked by a test.
- **Rebaselines.** Every later change to Yishuv content or era rules moves some of these: the era goldens, the chain
  golden and the reference files. As `docs/WRITING_EVENTS.md` says, the contributor names in the PR which ones moved,
  and the maintainer rebaselines them in the same merge. **The Founding goldens never move from Yishuv work.**
- **The chain invariant.** A historical chain from 1882 into 15 May 1948 equals a fresh Founding start, bit for bit,
  on every carried quantity: treasury, debt, reserves, population, budget, the five indicator shifts, coalition
  stability, rung, ceasefire clock and mobilisation (chapters design §6, with §8.2 here).

## 10. Perspective and sensitivity

### 10.1 Terms and tags

- **Terms.** Aliyah and Aliyah Bet (ha'apala); the Land of Israel in narration; the Irgun and Lehi as the dissidents.
  The contested ones are D8 (§10.3). Each choice goes into `docs/CONTENT_POLICY.md`'s glossary when it lands.
- **Sensitive tags.**
  - `holocaust` for the Holocaust events.
  - `massacre` or `terror` for Hebron and Safed (1929), the attacks of 1936–39 and those of the civil war.
  - `war-casualties` for battles with losses.

  The loader already refuses a positive treasury or reserves delta on these.
- **People.** Every person planned for these chapters is dead. The living-people gate (#62) still runs over them, as
  over all content.
- **Map.** Every `lines.yaml` row, and every settlement row with `until` or a rename, needs `sensitivity-reviewed`
  before a shareable build (#62).

### 10.2 Violence against civilians (D6)

1. **Attacks by the Irgun and Lehi are react-only.** The player is the Executive, and the options are its historical
   responses and their alternatives.
   - Where mainstream Israeli sources record a Haganah link, the event states it. Two such links, both *to verify*:
     - the King David Hotel (22 July 1946) was carried out within the Jewish Resistance Movement, whose joint command
       the Haganah sat on;
     - Deir Yassin (9 April 1948) took place with the Haganah's Jerusalem commander's consent, during the fighting
       for the Jerusalem road.
   - The Resistance Movement scene (decision 8) is a choice about joining the movement, not about any single attack.
2. **Haganah and Palmach operations** (Plan Dalet, Nachshon and the operations of the civil war) are decided at the
   level of the plan or operation. Where civilians were killed, the event carries `war-casualties` or `massacre`. No
   option rewards civilian harm: no treasury, standing or score for it.
3. **The Arab flight of 1947–48** appears in events and through the `arab_refugees` actor (from 15 May 1948). It is
   framed as mainstream Israeli historiography records it, and it is never a player objective or a reward.
4. **Atrocities against Jews** (Hebron and Safed in 1929, the attacks of 1936–39, the Hadassah convoy and Kfar Etzion
   in 1948, *to verify*) are react-only and tagged.

Each of these events needs the owner's sensitivity review before a shareable build.

| Option for D6 | For | Against |
|---|---|---|
| **A. The three rules above (recommended)** | The record is shown; the player faces the Executive's real choices at the level it made them | Many events to review |
| B. Leave the attacks out | Less review | A gap the record does not have; the Saison and the Resistance Movement make no sense without them |
| C. Let the player order attacks | — | Breaks the rule that choices never reward atrocities |

### 10.3 Contested terms and single values (D8)

CONTENT_POLICY §1 and §3 make these owner choices, not research defaults. For each, the recommended default follows the
perspective rule:

| Item | Recommended default | Alternative |
|---|---|---|
| The 1936–1939 events in narration | "the 1936–1939 riots (Me'oraot)", the Israeli official term; "the Arab Revolt" once, as the name others use | "the Arab Revolt" throughout |
| Deir Yassin, name | "the Deir Yassin affair" | "the battle of Deir Yassin" |
| Deir Yassin, death toll | one value from a mainstream Israeli historian, chosen by research and recorded with the reason | no number in the text |
| Civil-war casualties | Gelber (2,000 Jewish dead by the end of March 1948, *to verify* in the original) | the Ministry of Defence memorial count |
| Jewish population, 1914 | 94,000 (DellaPergola after Bachi, §2) | the lower Ottoman-era count |
| The 1947–48 Arab flight | the mainstream Israeli account the research names, one source | — |

## 11. Out of scope

- Arab and British internal politics beyond what an actor does to the Yishuv (spec §1).
- Diaspora politics, except through the Zionist Congress events and donations.
- Actor standing in state (#11); war mode before 15 May 1948 (rung 5 is off; #21 covers 1948 in the Founding chapter).
- The 1940 land zones as a rule (§5.7, deferred).
- Open mode (#10); nothing here depends on it.
- Hebrew text (#55).
- A playable Holocaust (never; spec §1).

## 12. Work items

These are drafted as separate issues, each "Part of #49". The systems items need the chapters items #56–#60 first. The
map items need #61–#63.

| # | Item | Needs |
|---|---|---|
| 1 | Window the Founding-only records and split Dori's term | #57 |
| 2 | Yearly turns, turn lengths per rung, `war_mode` | #58, #59 |
| 3 | Politics without a state: the player's office by date, no legislature, the Assembly's consent cost | #56, #58 |
| 4 | `economy: national_funds` | #58 |
| 5 | The arrivals lever and emigration | #57, #58 |
| 6 | Map actions for land and settlement; player settlements count | #58, #61 |
| 7 | The two eras as data: `eras.yaml` entries, start files, rule files, calibration tests, era goldens | #60, 1–6, 9–13 |
| 8 | The 15 May 1948 boundary: the carry files, the carry and pending-event rules, the chain golden | #59, #60, 7 |
| 9 | Research: offices, executives and people, 1882–1948 | — |
| 10 | Research: the Assemblies, the People's Council, the parties and the coalitions | — |
| 11 | Research: population and aliyah, 1882–1948, with derived Ottoman rows | — |
| 12 | Research: the national funds and the Haganah budget | — |
| 13 | The Yishuv model: indicators, policies, effects, actors | 1, 9 |
| 14 | Events: 1945 – 14 May 1948 (written first) | 13 |
| 15 | Events: 1882–1918 | 9, 13 |
| 16 | Events: 1918–1939 | 9, 10, 13 |
| 17 | Events: 1939–1945, the war and the Holocaust | 13 |
| 18 | Map: settlements founded before 15 May 1948 | #61 |
| 19 | Map: Ottoman and Mandate lines | #62, #63 |
| 20 | UI: the Yishuv chapters on screen | #66, 2–8 |
