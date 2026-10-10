# The Yishuv chapters, 1882–1948: era design

- **Date:** 2026-10-10
- **Status: PROPOSED — owner review needed for §Decisions**
- **Issue:** #49 (roadmap tracker)
- **Builds on:** [the chapters architecture](2026-10-10-chapters-architecture-design.md) (#53; items #56–#67). This
  document uses its words (era, chapter, boundary, window, carry, switch) and changes none of its rules except the two
  amendments in D5.
- **Parent spec:** [2026-10-09-statecraft-israel-design.md](2026-10-09-statecraft-israel-design.md) (§1 perspective and
  owner decisions; §3 systems; §4 loop; §5 content pipeline)

This document says what the player does before the State of Israel exists, and how each system of today's sim serves
that period, by reuse or by a stated new piece. It ends with the hand-off into the existing Founding chapter on
15 May 1948 without moving its goldens. It writes no code and no content; the work items are listed in §12 and drafted
as separate issues.

Facts quoted here are there to size the design. Each one names its source and the day it was checked. A fact marked
*to verify* has no checked source yet; the research issues (§12) settle it before any content uses it.

## Decisions

Each needs the owner's yes or no. Everything else follows from them or is a technical choice with a stated default.

| # | Decision | Recommended default |
|---|---|---|
| D1 | Start date, chapters and turn length (§3) | **Two eras, two chapters.** `ottoman` "The First Aliyot", 31 July 1882 (Rishon LeZion founded) to 1 July 1920 (civil administration replaces military rule, *to verify*), with **yearly** turns. `mandate` "The Mandate", 1 July 1920 to 15 May 1948, with **quarterly** turns. Turns shorten on the security ladder as today |
| D2 | Who the player is (§4) | **The head of the Zionist executive**, a dated chain of real offices: chairman of Hovevei Zion (from the Kattowitz conference, 1884), President of the Zionist Organization (from 29 August 1897), chairman of the Jewish Agency Executive (from 1935). The last one's successor is the Prime Minister. Not a fictional "Yishuv leader", not the Vaad Leumi chair |
| D3 | The legislature (§5.2) | **None in the Ottoman chapter** (policies need no vote; the Zionist Congresses are hard events). **The Assembly of Representatives as a consent body in the Mandate chapter:** a lost vote does not block the policy; it costs national authority. Its elections are sourced (1920, 1925, 1931, 1944) |
| D4 | Historical settlements on the map (§5.7, §7) | **They appear on their founding dates in every game.** The player's map actions add land and settlements on top of history. The player cannot "un-found" Degania |
| D5 | Two amendments to the chapters design for this boundary (§8) | **Yes to both.** (a) A carry rule `rung: start`: at 15 May 1948 the rung is set to the Founding start rung (5), because the invasion of 15 May happens in every game. (b) The budget carries by deviation onto the next era's `budget_start`, exactly, with no rescale when no line moved |
| D6 | Attacks on civilians by Jewish dissident groups, and atrocities against Jews (§10) | **Included as react-only events with sensitive tags.** The player never orders them. The player chooses the Yishuv leadership's response, which history records (for example the Jewish Agency's condemnations). No option rewards the attack |
| D7 | Content scope (§6) | **About 120 events and 10 decision scenes** for the two chapters (about 30 Ottoman, about 90 Mandate), written in four research batches. Later batches may grow it |

## 1. The rules this design keeps

1. **The spec's perspective rule** (spec §1; `docs/CONTENT_POLICY.md` §1): Israeli-perspective narration and terms,
   mainstream Israeli historiography, one sourced value per fact, no opposing-narrative panels. Arab actors and the
   British are AI actors with their own goals.
2. **What holds regardless** (CONTENT_POLICY §2): every fact sourced; no invented quotes; nothing rewards an atrocity;
   the Holocaust is reacted to and never played, shown without swastikas; real people in their public roles.
3. **No reset.** The Ottoman chapter carries into the Mandate chapter, and the Mandate chapter into the Founding
   chapter, by the chapters design's deviation carry (its D1).
4. **The Founding goldens do not move.** `GoldenRun.Hash1950`, `Hash1952` and `SMOKE OK 5 turns` stay the same through
   every item in §12. §8.4 shows how.
5. **Data decides, code implements** (chapters design §1.4, §5). Every pre-state difference is a dated record or an
   era switch with one implementation.
6. **One value, even when the sources give ranges.** The pre-1919 aliyah totals exist only as ranges (§2). The design
   calibrates to single-valued population anchors instead and derives arrivals from them, with the arithmetic in a
   comment, as `rules.yaml` already derives `initial_population` (§5.4).

## 2. The history it rests on

The anchors that shape the systems. Event research (§12) adds the rest.

| Date | Fact | Source (checked 2026-10-10 unless marked) |
|---|---|---|
| 31 Jul 1882 | Rishon LeZion founded by ten Hovevei Zion pioneers from Kharkiv | https://en.wikipedia.org/wiki/Rishon_LeZion |
| 1882–1903 | First Aliyah. The Ottoman authorities moved to limit Jewish entry from November 1882. Baron Edmond de Rothschild funded and took over settlements (Zikhron Ya'akov's assets in 1883). Moshavot founded 1882–1904 listed by year | https://en.wikipedia.org/wiki/First_Aliyah |
| 1882 / 1914 / 1922 / 1931 / 1947 | Jewish population 24,000 / 94,000 / 83,794 (census) / 174,610 (census) / 630,000 (DellaPergola after Bachi) | https://www.jewishvirtuallibrary.org/jewish-and-non-jewish-population-of-israel-palestine-1517-present |
| 1882–1903, 1904–1914 | Immigrants 20,000–30,000 and 35,000–40,000: **ranges only** (§1.6) | https://jewishvirtuallibrary.org/immigration-by-region-1882-2010 |
| 1919 – 14 May 1948 | Immigrants by period and continent (Asia, Africa, Europe, America and Oceania, unknown), the same columns as `content/aliyah.yaml`: 1919–23 35,183; 1924–31 81,613; 1932–38 197,235; 1939–45 81,808; 1946 – 14 May 1948 56,467. The page's total row is 482,857, which these rows do not sum to (*to verify* against the CBS original) | same JVL table |
| 1897 | First Zionist Congress, Basel; the Zionist Organization founded (*to verify* date 29 Aug 1897) | — |
| 1901 | Jewish National Fund founded at the Fifth Zionist Congress. By 1948 it held 936,000 dunams, 54% of Jewish-held land | https://en.wikipedia.org/wiki/Jewish_National_Fund |
| 1903 | Sixth Congress: 295 for, 178 against sending an expedition to East Africa (the Uganda scheme) | https://en.wikipedia.org/wiki/British_Uganda_Programme |
| 1904–1914 | Second Aliyah, about 35,000 (an estimate the research must replace with one sourced value or drop). Degania: 1909 in this source, 1910 in others (*to verify*) | https://en.wikipedia.org/wiki/Second_Aliyah |
| 1908 | Palestine Office in Jaffa, the origin of the Jewish Agency | https://en.wikipedia.org/wiki/Jewish_Agency_for_Israel |
| Apr 1909 | Hashomer (from Bar-Giora) | https://en.wikipedia.org/wiki/Haganah |
| 19 Apr 1920 | First Assembly of Representatives election (314 seats; Ahdut HaAvoda largest, 70). Later elections 6 Dec 1925, 5 Jan 1931, 2 Aug 1944 | https://en.wikipedia.org/wiki/Assembly_of_Representatives_(Mandatory_Palestine) |
| Jun 1920 | Haganah founded after the April 1920 riots | https://en.wikipedia.org/wiki/Haganah |
| 7 Oct 1920 | Jewish National Council (Vaad Leumi) elected. Chairs: Yellin 1920–29, Rutenberg 1929–31, Ben-Zvi 1931–44, Remez 1944–48. Dissolved 14 May 1948; its departments became the basis of the ministries | https://en.wikipedia.org/wiki/Jewish_National_Council |
| Dec 1920 | Histadrut founded in Haifa; Ben-Gurion its secretary from late 1921 to 1935; about 176,000 members in 1947 | https://en.wikipedia.org/wiki/Histadrut |
| 1929 | Jewish Agency formed from the Palestine Zionist Executive. Executive chairs: Kisch 1923–31, Arlosoroff 1931–33, Ruppin 1933–35, Ben-Gurion 1935–48 | https://en.wikipedia.org/wiki/Jewish_Agency_for_Israel |
| 23–29 Aug 1929 | Riots; 133 Jews killed, 67 of them in Hebron (24 Aug) and 18 in Safed (29 Aug) by the source's summary table | https://en.wikipedia.org/wiki/1929_Palestine_riots |
| 25 Aug 1933 | Haavara (transfer) agreement; about 60,000 German Jews reached the Land of Israel 1933–39 under it | https://en.wikipedia.org/wiki/Haavara_Agreement |
| 10 Dec 1936 – Oct 1939 | Tower and stockade: about 57 settlements; Tel Amal first | https://en.wikipedia.org/wiki/Tower_and_stockade |
| 24 Jul 1938 | Kofer HaYishuv proposed by the Vaad Leumi: indirect levies on fares, cigarettes and entertainment, later direct emergency taxes (1940, 1942) | https://he.wikipedia.org/wiki/כופר_היישוב |
| May 1939 | White Paper: 75,000 immigrants over five years, then Arab consent; land transfers restricted by regulations of February 1940 (three zones) | https://en.wikipedia.org/wiki/White_Paper_of_1939 |
| 14 May 1941 | Palmach founded | https://en.wikipedia.org/wiki/Haganah |
| 1934–1948 | Aliyah Bet; Struma sunk about 24 Feb 1942; Patria reached Haifa 1 Nov 1940 and was sunk by the Haganah to stop a deportation (date *to verify*); Exodus 1947 intercepted 18 Jul 1947; up to 50,000 held in Cyprus camps. The source gives conflicting totals (*to verify*) | https://en.wikipedia.org/wiki/Aliyah_Bet |
| 20 Sep 1944 | Jewish Brigade announced, about 5,000 soldiers | https://en.wikipedia.org/wiki/Jewish_Brigade |
| Nov 1944 – Feb 1945 | The Saison | https://en.wikipedia.org/wiki/Haganah |
| 29 Nov 1947 | UN General Assembly Resolution 181: 33 for, 13 against, 10 abstentions. The Jewish Agency accepted | https://en.wikipedia.org/wiki/United_Nations_Partition_Plan_for_Palestine |
| 19 Nov 1947 | Haganah conscription began; 21,000 conscripted by the end of March 1948 | https://en.wikipedia.org/wiki/Haganah |
| 30 Nov 1947 – 14 May 1948 | The civil war phase | https://en.wikipedia.org/wiki/1947–1948_civil_war_in_Mandatory_Palestine |
| 12 Apr 1948 | People's Council (Moetzet HaAm), 37 members; renamed the Provisional State Council at independence | https://en.wikipedia.org/wiki/Moetzet_HaAm |

**Single-value choices the research must make.** Casualty figures for the civil war differ by source; the perspective
rule picks the mainstream Israeli one (for example Gelber over Morris) and records why, as CONTENT_POLICY §3 says.

## 3. The eras

### 3.1 `content/eras.yaml` (D1)

```yaml
eras:
  - id: ottoman
    name: The First Aliyot
    from: 1882-07-31               # Rishon LeZion founded (§2)
    to: 1920-07-01                 # civil administration (to verify); the Assembly, Haganah, Vaad Leumi follow in 1920
    systems:
      legislature: none            # new value (§5.2)
      economy: national_funds      # new switch (§5.3)
      war_mode: false              # new switch: rung capped at 4, no ceasefire clock (§5.6)
      map_actions: [land_purchase, moshava, kibbutz]
    start: start/ottoman.yaml
  - id: mandate
    name: The Mandate
    from: 1920-07-01
    to: 1948-05-15
    systems:
      legislature: assembly
      economy: national_funds
      war_mode: false
      map_actions: [land_purchase, moshava, kibbutz, moshav, tower_and_stockade]
    start: start/mandate.yaml
    carry: carry/mandate.yaml
  - id: founding                   # unchanged, except that it now has a carry file (§8)
    ...
    carry: carry/founding.yaml
```

The chapters design's loader rule holds: eras are contiguous, `systems` keys are strict, and a key is added only with
the code behind it. `rules/ottoman.yaml` and `rules/mandate.yaml` hold the keys these eras change (§3.3). The base
`rules.yaml` stays the Founding chapter's, unchanged.

**Why two eras, not one.** Rules change only at boundaries (chapters design §3.3), and the turn length, the
legislature and the outside ruler all change in 1917–1920. One era would need date checks inside rules. Two eras keep
each calibration test on its own (chapters design §1.2).

**Alternatives for the start:**

| Option | For | Against |
|---|---|---|
| **A. 31 July 1882 (recommended)** | The spec says 1882; the First Aliyah is the start of the story | 15 years before the Zionist Organization; the player's office is thin before 1897 (§4) |
| B. 29 August 1897 | A real institution from the first turn | Drops the First Aliyah; Rishon LeZion and the Rothschild colonies become start conditions |
| C. 1 July 1920 | Every system (Assembly, Haganah, Histadrut) exists | Drops 38 years and the Second Aliyah, where the kibbutz and Hashomer begin |

### 3.2 Parts inside a chapter

A chapter has parts. A part is a heading on the chapter card and in the top bar, with no switch and no rule. Parts are
display text (the era-aware UI item, #66), listed in `eras.yaml` as `parts: [{ from, name }]`.

| Era | Part | From |
|---|---|---|
| ottoman | The Lovers of Zion | 31 Jul 1882 |
| ottoman | The Congress and the Second Aliyah | 29 Aug 1897 (*to verify*) |
| ottoman | War and conquest | Ottoman entry into the war, 1914 (*to verify*) |
| mandate | Building the National Home | 1 Jul 1920 |
| mandate | Riots, revolt and the White Paper | 23 Aug 1929 |
| mandate | The world war and the Holocaust | 1 Sep 1939 |
| mandate | The struggle | 1945 (*to verify*: the end of the war in Europe, 8 May 1945) |
| mandate | The civil war | 30 Nov 1947 |

### 3.3 Turn length

The chapters design keeps quarter, month and week. This era adds a year, set per rung in data:

```yaml
# rules/ottoman.yaml (only the keys it changes)
turn_lengths: [year, year, year, quarter, quarter]   # rungs 1..5; today's default is [quarter, quarter, quarter, month, week]
# rules/mandate.yaml
turn_lengths: [quarter, quarter, quarter, month, month]
```

- New: `TurnLength.Year` in `SimTime` (advance one year; year fraction 1.0) and a `turn_lengths` rules key read by
  `SecuritySystem.TurnLengthFor`. The default equals today's table, so the base `rules.yaml` does not set it.
- The Ottoman chapter is 38 yearly turns plus any quarters on rung 4 (the 1914–1918 war and deportations). The
  Mandate chapter is about 111 quarters plus months on rung 4 (the 1936–39 revolt at its height, the civil war).
- The boundary turn is adjusted by the chapters design's rule (§4.1 there), extended to a year: a turn is never more
  than its normal length plus 14 days, never less than the smaller of its length and 14 days.

## 4. Who the player is (D2)

The spec says "the player leads the Yishuv". There was no head of state. The design follows the office that held the
movement's executive power and that became the Prime Minister's office.

### 4.1 The player's office, by date

`eras.yaml` gains a dated `player_office` list. It replaces the assumption, today implicit in the UI and in
`Cabinet`, that the player is `prime_minister`. The Founding era lists `prime_minister`, so nothing changes there.

| From | Office id | Title | Holders (all *to verify* against a source per term) |
|---|---|---|---|
| 31 Jul 1882 | `hovevei_zion_chair` | Chairman of Hovevei Zion | vacant until the Kattowitz conference (Nov 1884); Leon Pinsker to 1891; later holders to research |
| 29 Aug 1897 | `zo_president` | President of the Zionist Organization | Herzl 1897–1904, Wolffsohn 1905–1911, Warburg 1911–1920, Weizmann 1920–1931, Sokolow 1931–1935 |
| 1935 | `jae_chair` | Chairman of the Jewish Agency Executive | Ben-Gurion 1935–1948 (checked: Jewish Agency article) |

- `jae_chair` has `until: 1948-05-14` and `successor: prime_minister` (chapters design §4.4). Ben-Gurion's Prime
  Minister term already starts on 14 May 1948 in `people.yaml`, so nothing moves.
- `zo_president` continues as a non-player office after 1935 (Weizmann 1935–1946, *to verify*). It answers events on
  diplomacy in London and at the League of Nations.
- **Vacant player office.** An office may be vacant; events that fire then do not use its placeholder (the loader
  already refuses a placeholder with no holder, `docs/WRITING_EVENTS.md`). The UI shows the institution's name in its
  place ("the Hovevei Zion movement").

**Alternatives:**

| Option | For | Against |
|---|---|---|
| **A. The Zionist executive chain (recommended)** | It held money, immigration, settlement and diplomacy, the levers the systems model; it leads to the Prime Minister | Three offices to explain; a vacancy in 1882–84 |
| B. The Vaad Leumi chair | A Yishuv-only body, elected locally | It controlled education, health and welfare, not immigration, land, the Haganah after 1939 or diplomacy |
| C. An unnamed "Yishuv leadership" | No vacancies | Breaks the real-names rule (spec §1) |

### 4.2 The executive (the "cabinet")

Offices that the player appoints and delegates to, each with a successor in the Founding chapter where one exists.
Holders and dates are research (§12); the examples are those the sources above name.

| Office id | Title | Example holder | Successor at 15 May 1948 |
|---|---|---|---|
| `palestine_office_head` | Head of the Palestine Office (1908–) | Arthur Ruppin | `agriculture_minister` (budget line only) |
| `ja_political_head` | Head of the Political Department | Arlosoroff, then Sharett (*to verify*: 1933–1948) | `foreign_minister` |
| `ja_treasurer` | Treasurer of the Jewish Agency | Kaplan (*to verify*: 1933–1948) | `finance_minister` |
| `ja_settlement_head` | Head of the Settlement Department | research | `agriculture_minister` |
| `ja_immigration_head` | Head of the Immigration Department | research | `prime_minister` (the Founding budget already holds absorption there; `rules.yaml` comment) |
| `ja_labour_head` | Head of the Labour Department | research | `labour_minister` |
| `vaad_leumi_chair` | Chairman of the Vaad Leumi | Yellin, Rutenberg, Ben-Zvi, Remez | none |
| `vaad_leumi_education` | Head of the Vaad Leumi Education Department | research | `education_minister` |
| `vaad_leumi_health` | Head of the Vaad Leumi Health Department | research | `health_minister` |
| `haganah_command_head` | Head of the Haganah National Command | research | `defense_minister` |
| `haganah_chief_of_staff` | Haganah Chief of Staff | research (Yaakov Dori, *to verify*) | `chief_of_staff` |

`governments.yaml` holds each Executive as a government record: its from/to (the Zionist Congress or Assembly vote
that formed it) and its coalition parties. `Cabinet.ExpireOnGovernmentChange` then works as today.

## 5. Systems: reuse and new

| System (spec §3) | Pre-state form | Reuse | New (each behind a switch or a window) |
|---|---|---|---|
| Effects web | Indicators, policies and effects of the Yishuv | `EffectsSystem` as is | Windowed records (`to: 1948-05-15`) |
| Legislature | None (Ottoman); the Assembly as a consent body (Mandate) | `Offices.Legislature`, `PoliticsSystem` seats and coalition | `legislature: none` and `assembly` behaviour (§5.2) |
| Ministers | The Executive | `Cabinet`, `people.yaml`, `governments.yaml` | `player_office` by date (§4.1) |
| Budget and money | National funds | Treasury, Debt, Budget lines | `economy: national_funds` (§5.3) |
| Population and aliyah | Waves, certificates, Aliyah Bet, emigration | `PopulationSystem`, `aliyah.yaml` series and regions | Arrival levers and an emigration series (§5.4) |
| Outside powers | Ottoman Empire, the Mandate, Arab leadership, dissidents | `model.yaml` actors (standing only today) | Windowed actors; their behaviour waits for #11 (§5.5) |
| Defence | Hashomer, Haganah, Palmach | The ladder, mobilisation, attention by rung | `war_mode: false`, per-rung turn lengths (§3.3, §5.6) |
| Map | Land purchase and settlement | `MapActions`, `Placements`, dated settlements (#61) | New actions; player settlements count as settlements (§5.7) |
| Events | Hard anchors and dilemmas | `EventSystem`, `requires` across eras | None |

### 5.1 The model (`model.yaml`)

**Kept, windowless:** the five indicators (`morale`, `food_supply`, `housing`, `security`, `intl_standing`) mean the
same before 1948 and stay active in every era. The voter groups (`veterans`, `new_immigrants`, `religious`,
`labour_movement`) stay too: the pre-state public had the same blocs.

**Windowed to the Founding chapter** (`from: 1948-05-15`): the four existing policies (`rationing`,
`housing_construction`, `border_settlement`, `maabarot`) and the effects that read them, because their offices are
state ministries. The offices get `from: 1948-05-14`. This is golden-safe (§8.4).

**New, windowed `to: 1948-05-15`** (design values; names are the player-facing ones):

| Kind | Id | Meaning | Office |
|---|---|---|---|
| Indicator | `national_authority` | How far the Yishuv accepts the national institutions' discipline: levy yield, dissident friction | — |
| Indicator | `employment` | Work for new arrivals; the 1926–27 crisis when emigration passed immigration (*to verify*) | — |
| Policy | `land_purchase` | Pace of JNF and private purchase | `ja_settlement_head` |
| Policy | `hebrew_labour` | Preference for Jewish labour in Jewish enterprises | `ja_labour_head` |
| Policy | `pioneer_selection` | Share of labour certificates kept for trained pioneers (halutzim) against family and rescue cases | `ja_immigration_head` |
| Policy | `aliyah_bet` | Immigration without British certificates (from 1934) | `ja_immigration_head` |
| Policy | `havlagah` | Restraint: defence without reprisal (1936–39) | `haganah_command_head` |
| Policy | `cooperation_with_britain` | Enlistment and intelligence cooperation, or resistance | `ja_political_head` |
| Policy | `hebrew_schools` | Hebrew-language national schooling | `vaad_leumi_education` |
| Policy | `voluntary_levy` | Kofer HaYishuv and emergency taxes (from 24 Jul 1938) | `ja_treasurer` |
| Policy | `settlement_drive` | Settlement for its own sake and for future borders | `ja_settlement_head` |
| Policy | `ottomanization` | Urging Jews to take Ottoman nationality (1914, *to verify*) | player office |

Effects and situations follow the same pattern as today (for example `settlement_drive` raises `security` with
inertia, as `border_settlement` does; low `employment` starts a situation that raises emigration). The full list is
a design item (§12). The Mandate chapter's carry file maps `settlement_drive` to `border_settlement` as a policy
successor (chapters design §4.2).

### 5.2 The legislature (D3)

- **Ottoman: `legislature: none`.** No body sits. `SetPolicy` takes effect without a vote and draws nothing from the
  RNG. Coalition stability still moves with approval and still ends the playthrough below 0.2 ("Your leadership of
  the movement is over"). The Zionist Congresses appear as hard events (the Uganda vote of 1903 is the clearest).
- **Mandate: `legislature: assembly`.** The Assembly of Representatives is a record in `legislatures.yaml` per
  election (1920, 1925, 1931, 1944), with sourced seats by list. The People's Council (37 members, from 12 April 1948)
  follows it and ends on 14 May 1948, the day the existing Provisional State Council record begins. A vote is held
  as today (`PoliticsSystem.Vote`, same RNG draw). **A lost vote does not block**: the policy is set, and
  `national_authority` falls by a rules value, with a Why line ("The Assembly did not endorse ..."). The Assembly had
  no power to compel; consent was the currency.
- `budget_approval` stays a per-record field. For the Assembly records it starts `false` with
  `open_questions: [budget_approval]`, as the Founding records do, until a source says how the Vaad Leumi budget and
  the Jewish Agency budget were approved.
- Parties span eras: `parties.yaml` gains the pre-state lists (Ahdut HaAvoda, Hapoel Hatzair, the Revisionists, the
  Left Bloc and others), with no windows.

| Option for the Mandate body | For | Against |
|---|---|---|
| **A. The Assembly as a consent body (recommended)** | Local, elected, the forerunner of the councils of 1948 (chapters design §4.4) | It did not elect the Jewish Agency Executive |
| B. The Zionist Congress | It elected the Executive | A world body, met every two years; sourcing delegates per list for 13 congresses is large |
| C. Both | Exact | Two seat tables per date and two vote rules; deferred unless play needs it |

The Revisionist and General Zionist boycott of the 1944 election (*to verify*) is the kind of fact this rule turns
into play: a boycott shrinks the body the player can win consent from.

### 5.3 Money: the national funds (`economy: national_funds`)

There was no state, no tax law and no currency of the Yishuv's own.

- **Treasury** is the national funds: the Jewish Agency's budget with Keren Hayesod, the JNF and, from 1938, Kofer
  HaYishuv. **Debt** stays (the institutions borrowed). **Foreign currency** is not tracked: the switch skips trade
  and the reserves line, and the UI hides it. `FxReserves` holds its start value unchanged (§8.2 shows why that
  carries exactly).
- **Income:** donations from abroad (`donations_per_year` in the era rules, sourced and calibrated by period),
  plus voluntary levies per person scaled by `voluntary_levy` and `national_authority`. The founding switch value
  (`economy: state`) is today's `EconomySystem.Apply`, unchanged.
- **Spending:** per person, as today, and the Haganah's cost through mobilisation (`mobilization_cost_per_year` in the
  era rules). The Haganah budget of £400,000 in 1946 and £3.3 million by October 1947 (Perlmutter, cited in the
  Haganah article) is a calibration anchor to verify.
- **Budget lines** are the Executive's departments (§4.2). `Allocate` checks them against the era's `budget_start`,
  so the rule "every ministry, and only them" reads the active era's lines.
- **Units.** The sim keeps one internal unit (chapters design D5). The display follows `currency.yaml` (#66): the
  Ottoman lira and the Egyptian pound before 1927 (*to verify*), the Palestine pound from 1927, the Israeli pound
  after. The Palestine pound passed to the Israeli pound at par (*to verify*), so one internal unit serves both
  sides of the boundary.

### 5.4 Population and aliyah

- **Groups.** `old_yishuv` (the community before 1882) plus the existing region groups (`asia`, `africa`, `europe`,
  `america_oceania`, `unknown`), which the 1919–1948 CBS table already uses. The start population is 24,000 Jews in
  1882 (§2); its split is research.
- **Series, Mandate.** `aliyah.yaml` gains the five CBS periods 1919 – 14 May 1948 by continent (§2). Reuse:
  `PopulationSystem.Arrivals` spreads each period evenly by day, as for 1948–51.
- **Series, Ottoman (§1.6).** No single-valued arrival totals exist. Rows are **derived** from the population
  anchors (24,000 in 1882; 94,000 in 1914; and any intermediate anchor the research finds) with natural increase and
  emigration, the arithmetic in a comment. A derived row says so in its comment and cites the anchors.
- **Emigration (new).** Many left: 50–70% of the First Aliyah by the source above, a range again, so the model takes
  emigration from anchors too. The WWI fall (94,000 in 1914 to 83,794 at the 1922 census) is deportation, hunger and
  flight. New: an `emigration` series of dated rows (people per year) and a rule `emigration_per_year` raised by the
  low-`employment` situation. Default 0, so the Founding chapter is unchanged.
- **The levers (new).** In Historical mode the series is the historical floor. The player moves arrivals around it:
  `aliyah_bet` adds arrivals at a cost (money, British standing, `intl_standing`, the risk of interception events);
  `pioneer_selection` moves morale and employment, not totals; the Mandate's quota, set by events (the 1922 and 1939
  White Papers), scales the legal part. Mechanism: a policy can name `arrivals` as an effect target with a factor on
  the period's rate. The founding content has no such effect, so its arrivals are unchanged.
- **Calibration** to the anchors: 1914, the 1922 and 1931 censuses, 1947, and the Founding start of 649,400 on
  15 May 1948 (`rules.yaml initial_population`), by the chain check of chapters design §12.

### 5.5 Outside powers

Actors are records in `model.yaml` with standing; behaviour comes with the diplomacy design (#11). Until then they
act through hard events and through rules they set (the certificate quota, the 1940 land zones).

| Actor id | Name | Window | Notes |
|---|---|---|---|
| `ottoman_empire` | Ottoman Empire | to 1920-07-01 | restrictions on entry and land purchase; wartime deportations |
| `british_mandate` | The Mandate administration | 1917-12-09 (*to verify*) to 1948-05-15 | the High Commissioner; certificates, land law, police (chapters design §4.4) |
| `uk` | United Kingdom (existing) | windowless | the government in London: the White Papers |
| `league_of_nations` | League of Nations | 1920 to 1946 (*to verify*) | the Permanent Mandates Commission |
| `un` | United Nations (existing) | `from: 1945-10-24` (*to verify*) | UNSCOP, Resolution 181 |
| `arab_leadership` | The Arab leadership in the Land of Israel | 1920-07-01 to 1948-05-15 | the Arab Executive, then the Arab Higher Committee; Israeli-perspective naming |
| `irgun`, `lehi` | Irgun; Lehi | 1931 / 1940 (*to verify*) to 1948-05-15 | the dissidents (porshim): friction, the Saison, the Resistance Movement |
| `usa`, `france` | (existing) | windowless | Biltmore, the 1947 vote |
| `ussr`, `egypt`, `jordan`, `syria`, `lebanon`, `iraq`, `arab_refugees`, `arab_citizens`, `west_germany` | (existing) | `from: 1948-05-15` | state-era actors, golden-safe (§8.4). The Arab states' pre-1948 roles appear through events |

### 5.6 Defence without a state

- **The ladder is always on** (spec §3). Rung meanings carry over: 1 routine (watchmen, theft, field disputes); 2 a
  wave (April 1920, May 1921, *to verify*; August 1929); 3 limited operations; 4 prolonged conflict (1936–39, the
  civil war from 30 November 1947).
- **`war_mode: false`** caps the rung at 4 and turns off the UN ceasefire clock. There was no army and no state to
  impose a ceasefire on. Rung 5 opens on 15 May 1948 by D5(a).
- **Mobilisation** is the call-up of the Haganah (field companies, the Palmach from 14 May 1941, conscription from
  19 November 1947). Its cost falls on the national funds. `rung_min_mobilization` is set in the era rules.
- **The organisations** are offices (§4.2), policies (`havlagah`, `cooperation_with_britain`) and actors (`irgun`,
  `lehi`). Friction lowers `national_authority`. The Haganah's command passes from the Histadrut to the national
  institutions in the 1930s (*to verify*), an event.
- **Hashomer** (Ottoman) is an event chain and the `security` effect of `settlement_drive`; it was not under the
  movement's command.
- The spec's defence depth (#22: command traits, intelligence estimates, procurement) applies here unchanged when it
  lands: the 1947–48 arms purchases in Czechoslovakia (*to verify*) are its first pre-state use.

### 5.7 Map actions

| Action | From | Needs | Effect (design values) |
|---|---|---|---|
| `land_purchase` | 1882 | land hex within reach of a settlement, not already bought | money; enables a settlement on that hex; British standing falls after the 1940 zones |
| `moshava` | 1882 | a bought hex | private farming village; housing, employment |
| `kibbutz` | 1909/1910 (Degania, *to verify*) | a bought hex | housing, security, morale; labour movement support |
| `moshav` | 1921 (Nahalal, *to verify*) | a bought hex | housing, food supply |
| `tower_and_stockade` | 10 Dec 1936 to Oct 1939 | a bought hex; rung 3 or higher | built in one turn; security; the British zone rules do not apply to existing purchases |

- Today `MapActions.Actions` is a fixed list of two. It becomes the era's `map_actions` switch list, each action's
  rule in the era rules. `transit_camp` and `housing` stay the Founding chapter's list.
- **Player settlements count as settlements** for "within one hex of a settlement" when their rule says
  `counts_as_settlement: true`. The two Founding actions do not set it, so the Founding check is unchanged.
- **Land zones of 1940.** The Land Transfer Regulations divided the country into three zones. A content file
  `map/land_zones_1940.yaml` maps hexes to zones, built by a tool from the sourced line (#63); `land_purchase` checks
  it from February 1940 (*to verify* the date). It is the one place a line has a rule, and it is sensitive map data
  (chapters design §13).
- **D4: history is the floor.** Historical settlements appear from the dated settlements file on their dates, in
  every game (#61). The HistoricalAI places nothing, so the reference run has no player placements. A player's
  placements carry into 1948 as extra places on the map.

## 6. Events and content scope (D7)

| Part | Events (about) | Decision scenes | Hard anchors (examples) |
|---|---|---|---|
| The Lovers of Zion, 1882–1897 | 8 | 1 | Rishon LeZion; the Baron's patronage; the first Ottoman restrictions |
| The Congress and the Second Aliyah, 1897–1914 | 12 | 1 | First Congress; JNF; Uganda vote; Palestine Office; Hashomer; Tel Aviv; Degania |
| War and conquest, 1914–1920 | 10 | 1 | deportations; Jaffa evacuation 1917; Balfour Declaration (2 Nov 1917); British conquest |
| Building the National Home, 1920–1929 | 20 | 1 | Tel Hai; Haganah; Histadrut; 1921 riots; 1922 White Paper; Jezreel Valley purchase; 1926–27 crisis |
| Riots, revolt and the White Paper, 1929–1939 | 25 | 2 | 1929 riots; Passfield; Haavara; Arlosoroff; the revolt; Peel; tower and stockade; 1939 White Paper |
| The world war and the Holocaust, 1939–1945 | 15 | 1 | enlistment; Palmach; Struma; Biltmore; the news from Europe; the parachutists; the Brigade; the Saison |
| The struggle, 1945–1947 | 15 | 1 | Resistance Movement; Black Sabbath; Anglo-American Committee; the eleven Negev points; Exodus; UNSCOP; Resolution 181 |
| The civil war, Nov 1947 – May 1948 | 15 | 2 | conscription; the convoys and Jerusalem; Plan Dalet; Nachshon; the People's Council; the decision to declare |
| **Total** | **about 120** | **10** | |

**Key decision events** (each a real choice the record shows; options and texts are the research's job):

1. **The Baron's patronage** (1880s): accept Rothschild's administrators and money, or keep the colonies' autonomy.
2. **The Uganda vote** (1903): send the expedition or refuse any land but the Land of Israel; the movement nearly
   splits.
3. **Hebrew or German** (1913, the Technion language dispute, *to verify*): fund a Hebrew school system.
4. **Ottomanization** (1914–15): urge Ottoman nationality to avoid expulsion, or keep foreign protection.
5. **Tel Hai** (1920): hold or withdraw the Upper Galilee outposts.
6. **Who commands the Haganah** (1920s–30s): the Histadrut or the national institutions.
7. **Haavara** (1933): take the transfer agreement for German Jews and their property, or join the boycott.
8. **Restraint or response** (1936–39): `havlagah`; the Jewish Settlement Police; tower and stockade.
9. **Peel** (1937): negotiate on partition or reject it.
10. **The White Paper** (1939): fight it, enlist against Germany, or both; Aliyah Bet.
11. **The Saison** (1944–45): hand dissidents to the British, act alone, or not at all.
12. **The Resistance Movement** (1945–46): join the Irgun and Lehi against the British, and when to leave.
13. **The eleven points** (October 1946): settle the Negev before the borders are drawn.
14. **Partition** (1947): accept Resolution 181.
15. **Declare the state** (May 1948): declare on 14 May or accept a truce and delay (*to verify*: the People's
    Administration vote of 12 May 1948). Its historical option is the one the Founding chapter's events may
    `require`.

**The Holocaust** is reacted to, never played (spec §1). Events present the Yishuv leadership's real choices: rescue
funds, Aliyah Bet, the parachutists, the response to the news from Europe. They carry the `holocaust` tag. No option
grants treasury. No swastikas in any art.

## 7. The map in these eras

| Layer | Content | Built by |
|---|---|---|
| Settlements | Every Jewish locality founded before 15 May 1948, with `founded` dates, renames (Mas'ha to Kfar Tavor in 1903) and `until` where a place was abandoned (Hartuv, Kfar Etzion; *to verify*). Arab towns and villages are `pre_modern` rows, as today | #61's dated file; research by region |
| Lines | Ottoman district borders (*to verify* which are needed), the 1906 Sinai line, the separation of Transjordan (1922), the Mandate border (1923), the Peel proposal (1937), the 1940 land zones, the UN partition lines (1947) | #63 `lines.yaml`; every line sensitive (chapters design §13) |
| Water | Lake Hula and the Dead Sea at period levels; no modern reservoirs | #16 (one coast mask per water epoch) |
| Places on hexes | `map_action_max_distance` and the cap per turn in the era rules | `rules/<era>.yaml` |

Naming follows the perspective rule. The British official name, Palestine, is used where it is the official name (the
Mandate, the Palestine pound); narration says the Land of Israel.

## 8. The 15 May 1948 boundary

### 8.1 What crosses

The chapters design's table (§4.2 there) applies. For this boundary, concretely:

| Mandate-era thing | At 15 May 1948 | Mechanism |
|---|---|---|
| `jae_chair` and the Executive offices | end; budget lines pass to their successors (§4.2) | `offices.yaml` `until` + `successor`; `carry/founding.yaml` |
| Their holders | continue in their sourced state offices | `people.yaml` terms; nothing moves |
| The People's Council | ends 14 May 1948; the Provisional State Council record (from 14 May 1948) sits | `legislatures.yaml` windows; `legislature: state` |
| National funds (Treasury), Debt | deviation carry onto `treasury_start` (20) and debt 0 | `carry/founding.yaml` |
| `FxReserves` | deviation carry onto `fx_start` (40). The Yishuv never changes it, so player equals reference and the result is exactly 40 | same |
| Population (all groups) | mapped to `veterans`, then ratio carry onto 649,400 | same; chapters design §4.3 |
| Policies | Yishuv policies end with their windows; `settlement_drive` maps to `border_settlement` by deviation; Founding policies start at their defaults | carry file; `ContentWindows.Update` |
| Indicator shifts | deviation carry for the indicators the Founding start lists; persist for the others | carry file |
| Rung | **set to the Founding start rung, 5** (D5a), which starts the ceasefire clock at `ceasefire_weeks`, as a fresh start does | new carry rule `rung: start` |
| Mobilisation | persists, raised to the rung 5 floor (0.8) as `SetRung` does today | `SecuritySystem.RaiseToFloor` |
| Budget | deviation carry per Founding line onto `budget_start` (D5b) | §8.2 |
| Placements | persist; player settlements stay and count | `Placements` |
| `ChosenOptions`, `FiredEvents`, `PendingEvents` | persist; the Founding events may `require` Yishuv choices | event `requires` |
| Actors | Yishuv actors' windows end; `uk`, `usa`, `france`, `un` continue; state-era actors begin | windows |

### 8.2 The two amendments (D5)

**(a) `rung: start`.** The chapters design says the rung persists. At this boundary that would let a player enter
15 May 1948 at rung 4 with the ceasefire clock at 0, which would end the war in the first Founding turn. A carry rule
sets the rung to the next era's `start_rung` when the carry file says `rung: start`, through
`SecuritySystem.SetRung`. From rung 4 that gives exactly the fresh start's state (rung 5, clock 52, mobilisation at
least 0.8). It uses no RNG.

**(b) Budget by deviation.** The chapters design maps the budget through successors and rescales it to sum to 1. A
fresh Founding start has `budget_start` exactly; a rescale of the same shares can differ from it in the last bit, which
breaks the carry invariant (chapters design §6). Rule: for each Founding line, `start + (player − reference)`, where
player and reference are the summed Yishuv lines mapped to it; clamp at 0; rescale only if any deviation is non-zero.
The historical chain then lands on `budget_start` bit for bit.

Both amendments are changes to Chapters 4 (#59) and are listed there as this era's needs.

### 8.3 The boundary turn

The Mandate chapter's last turns are monthly (rung 4). The turn that would pass 15 May 1948 ends on it (chapters
design §4.1). The Founding chapter's first turn then starts on 15 May 1948, the same date a fresh start uses, so both
share the calendar from the boundary on.

### 8.4 Golden safety

Each item in §12 keeps the Founding goldens, by the chapters design's checklist (§8 there):

1. **New records** have windows that end on or before 15 May 1948. They are never active in a run that starts on
   15 May 1948.
2. **Existing records windowed to the Founding chapter** (§5.1, §5.5) get `from: 1948-05-15` (offices
   `from: 1948-05-14`). A fresh Founding start initialises from the active view on 15 May 1948, which holds exactly
   today's keys, so the hashed state is unchanged.
3. **New switches** (`legislature: none | assembly`, `economy: national_funds`, `war_mode`, map-action lists,
   `turn_lengths`, emigration, arrival effects, `counts_as_settlement`) have defaults equal to today's behaviour, and
   the Founding era sets no override file.
4. **New aliyah rows** end on 14 May 1948. `Arrivals` counts days in [from, to), so a run from 15 May 1948 sees none.
5. **New events** close by 15 May 1948. An event may not span a boundary.
6. **The boundary** is the Founding era's `from`. `Simulation.NewGame(content, seed, mode)` still means `founding`;
   `GoldenRun.Play` does not cross it.
7. **No new `GameState` field.** The era and the player office derive from `Date`. Land is held as `Placements`.

`Content.Hash` changes with every new file, as any content change does. Saves warn once and replay unchanged.

## 9. Fresh starts, calibration and goldens

- **Fresh starts** (chapters design D2): `start/ottoman.yaml` (1882) and `start/mandate.yaml` (1920) hold sourced
  start values: population by group, national funds, debt, budget lines, start rung, and the policy levels the
  calibration reads. The Mandate start's population is the 1922 census back-projected to 1 July 1920 (*derived*,
  with the arithmetic in a comment).
- **Calibration per era** (`OttomanCalibrationTests`, `MandateCalibrationTests`), each on a fresh start: population
  against the anchors (§5.4); national funds against the sourced Jewish Agency and Kofer budgets (research); land
  held against the JNF figure for 1948 (§2).
- **Goldens:** `Hash_ottoman_1900`, `Hash_mandate_1930`, and a chain golden from 1882 into the Founding chapter to
  1950. Each pinned in the three places `docs/determinism.md` names. The carry reference files
  `carry/mandate.reference.yaml` and `carry/founding.reference.yaml` are regenerated by the tool and checked by test.
- **The chain invariant:** a historical chain from 1882 into 15 May 1948 equals a fresh Founding start on every
  carried quantity, bit for bit (chapters design §6), with the two amendments of §8.2.

## 10. Perspective and sensitivity

- **Terms.** Aliyah and Aliyah Bet (ha'apala), the Land of Israel in narration, the 1936–39 events as the Arab
  Revolt or the riots (*the research picks one term*), the Irgun and Lehi as the dissidents. Each term choice is
  written in `docs/CONTENT_POLICY.md`'s glossary when it lands (§12).
- **Sensitive tags.** `holocaust` for the Holocaust events; `massacre` or `terror` for Hebron and Safed (1929), the
  attacks of 1936–39 and of the civil war; `war-casualties` for battles with losses. The loader already refuses a
  positive treasury or reserves delta on these.
- **D6, dissident attacks.** Events where Jewish dissident groups killed civilians (the King David Hotel, 22 July 1946;
  Deir Yassin, 9 April 1948; both *to verify*) are react-only: the player is the Executive, which did not order
  them. The options are the Executive's historical responses and their alternatives. They carry a sensitive tag and
  need the owner's sensitivity review.
- **People.** Every person in these chapters is dead; the living-people gate (#62) does not apply. Portraits follow the
  public-domain rule (spec §5.6).
- **Map.** Every `lines.yaml` row and every settlement row with `until` or a rename needs `sensitivity-reviewed`
  before a shareable build (#62).

| Option for D6 | For | Against |
|---|---|---|
| **A. React-only events, tagged (recommended)** | The record is shown; the player faces the Executive's real choice | The owner must review each |
| B. Leave them out | Less review | A gap the record does not have; the Saison and the Resistance Movement make no sense without them |
| C. Let the player order them | — | Breaks the rule that choices never reward atrocities |

## 11. Out of scope

- Arab and British internal politics beyond what an actor does to the Yishuv (spec §1).
- Diaspora politics except through the Zionist Congress events and donations.
- War mode before 15 May 1948 (rung 5 is off; #21 covers 1948 from the Founding chapter).
- Open mode (#10); nothing here depends on it.
- Hebrew text (#55).
- A playable Holocaust (never; spec §1).

## 12. Work items

Drafted as separate issues, each "Part of #49". The systems items need the chapters items #56–#60 first; the map
items need #61–#63.

| # | Item | Needs |
|---|---|---|
| 1 | Window the Founding-only records (policies, effects, offices, actors) | #57 |
| 2 | Yearly turns and turn lengths per rung; `war_mode` | #58 |
| 3 | `legislature: none` and the Assembly as a consent body | #58 |
| 4 | `economy: national_funds` | #58 |
| 5 | Aliyah levers, quotas and emigration | #57, #58 |
| 6 | Map actions for land and settlement; player settlements count | #58, #61 |
| 7 | The player's office by date | #56 |
| 8 | The 15 May 1948 boundary: carry files, the two amendments, the chain golden | #59, #60, 1–7 |
| 9 | Research: offices, executives and people 1882–1948 | — |
| 10 | Research: the Assembly, the People's Council and the parties | — |
| 11 | Research: population and aliyah 1882–1948, with derived Ottoman rows | — |
| 12 | Research: the national funds and the Haganah budget | — |
| 13 | The Yishuv model: indicators, policies, effects, actors | 1, 9 |
| 14 | Events: 1882–1920 | 9, 13, #57 |
| 15 | Events: 1920–1939 | 9, 10, 13 |
| 16 | Events: 1939–1945, the war and the Holocaust | 13 |
| 17 | Events: 1945 – 14 May 1948 | 13 |
| 18 | Map: settlements founded before 15 May 1948 | #61 |
| 19 | Map: Ottoman and Mandate lines, and the 1940 land zones | #62, #63 |
| 20 | UI: the Yishuv chapters on screen | #66, 2–7 |
