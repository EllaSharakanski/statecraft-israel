# Changelog

All notable changes to Statecraft: Israel. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Every pull request that changes the game (`sim/`, `content/`, `unity/`)
adds a line under **Unreleased**; CI's `Changelog` job checks this (see `CONTRIBUTING.md`).

## [Unreleased]

### Added
- Region borders in the promo look (promo-borders): two-tone outlines along hex edges (a blue band with a white inner
  stripe, a red-orange band with a cream one), drawn inside each region by the terrain shader from one texel per hex, with
  a "borders" visual toggle. Visual only. No invented boundaries: the first real data is the 1948 sub-districts (below);
  the 1949 armistice lines and fronts are research items.
- A local multi-size favicon for the WebGL page.
- Hebrew names and sourced birth and death dates for all 29 people (issue #1).
- Successors for offices left by deaths in 1952: Ben-Gurion takes Transportation after Pinkas (14 August 1952) and
  Yitzhak Ben-Zvi becomes President on 16 December 1952 after Weizmann; the presidency is vacant in between, while
  the Speaker was acting President (issue #1).
- A portrait of Golda Meir: a 1964 Nationaal Archief photo by Willem van de Poll (CC0; #6).
- Historical-only deputy titles and sourced 1951 terms; Kaplan's June 1952 Deputy Prime Minister title and the linked Finance/Agriculture succession (issue #4).
- People can carry a birth date and a source for each biographical value (Hebrew name, birth and death dates) in
  `people.yaml`; the loader rejects unknown source keys, sources without a `url` or `checked` date, and impossible birth
  dates.
- Browser-playable 1948–1951 (milestone M3): HUD, Cabinet with appointments, Budget, Events, Defence and "Why did this
  happen?" screens over the 3D hex map; sourced content to 31 December 1951; saves; a calibrated economy; the golden
  hash checked in WebGL and macOS players.
- The map (milestone M2): terrain from Copernicus elevation data, biomes from rainfall, 1948 settlements on hexes.
- The 1948 sub-districts on the map (promo-borderdata): the sixteen Mandate sub-districts in force on 15 May 1948
  (Palestine Gazette No. 1552, 1947), digitised from the Survey of Palestine's "Administrative Divisions" map and stored
  as true lines (`content/map/subdistricts_1948.geojson`, status draft); the map shows them snapped to hex edges in the
  blue border style, labelled "Mandate sub-districts, 1947" from the 1948 start until the 1949 armistice lines replace
  them; the info panel names a hex's sub-district and district. Display only: the sim does not read them.
- The 1949 armistice lines on the map (armistice-lines, #196): from each agreement's date the Mandate sub-district
  layer gives way on that front, where the line exists. Egypt (24 Feb 1949): the El Auja Demilitarized Zone and the
  Gaza-Rafah line from the Wadi Hasi to Gaza; Lebanon (23 Mar 1949): the international boundary; Syria (20 Jul 1949):
  the line through Annex I's points and the three-part Demilitarized Zone, from the annexed 1:50,000 map. Stored as true
  lines (`content/map/armistice_lines_1949.geojson`, status draft), drawn snapped to hex edges in the red-orange style;
  the info panel names the line or zone and its agreement. Jordan has no line data yet (no usable scan of the signed
  maps): the Mandate layer stays there and the map label says so. Display only: the sim does not read them.
- Open-source release (public since 2026-10-10): licences (MIT code, CC BY-SA 4.0 content, CC BY 4.0 original docs
  and art, CC0 for AI-only images), asset provenance (`ASSETS.yaml`), contributor
  and content policies, CI for the sim, tools and repository policy.
- Unity-MCP 0.93.3 (`com.ivanmurzak.unity.mcp`, from OpenUPM) as the standard AI-agent bridge in the editor, local
  Custom mode with token auth only (held by `UnityMcpGuard`). It stays offline in batch mode, opens no window there, and is
  left out of player builds.
- A runtime quality fallback for slow machines in the browser: when frames stay slower than 30 fps, the game lowers its
  render scale (to 0.9, then 0.8) and then turns the tilt-shift off, and steps back up when frames are comfortably fast
  again. Presentation only; `?nofallback` turns it off (#44).
- The sea and coast in the promo look: turquoise shallows that deepen to a muted blue offshore, rolling surf filtered
  so it does not shimmer, a fine chop, and a sand strip with wet sand along every sea-level coast (salt flats at the
  Dead Sea). Presentation only.
- The unit art slice (a showcase for the art track, not gameplay): a 1948 IDF rifle trio with a selection ring, an IAF
  Spitfire flying a loop with its shadow, and the corvette INS Haganah, built from code as painted miniatures at the
  promo's symbolic scale. Shown only with `?units` (desktop `-units`) and in `tools/art_checkpoint.sh --units`; an
  authored model can replace a code-built one per unit type (docs/units.md).

### Changed
- The four armistice events end with 1949 (`to: 1950-01-01`). They still fire on their 1949 dates; a finite window
  means their Prime Minister placeholder doesn't need a holder after his death in 1973.
- Ben-Gurion's in-game portrait is now a crop of Abbie Rowe's 8 May 1951 White House photo (NARA 200318,
  PD-USGov) instead of a Government Press Office photo with no clear US status (#30, decision D-P).
- Later death dates can be recorded for people with open-ended terms in the latest authored government; explicit term ends still cannot pass the holder's death.
- Source Dov Yosef's food-supply and rationing responsibility in the Third Government from 8 October 1951,
  replacing the unsupported vacancy (#2).
- The interface is restyled to the promo art (`promo/final/03`, `04`, `05`, `08`):
  - navy panels with gold trim, corner brackets and a soft shadow;
  - an icon for each indicator in the top bar, with its value and its change since the last turn;
  - a framed date badge;
  - round icon buttons for the screens, with their names under them;
  - signs on the event options;
  - a terrain sign in the hex panel (still no tile yields);
  - the Cabinet as a grid of portrait cards.

  Narrower windows get a compact top bar, and windows narrower than about square get a taller one that puts each change
  under its value. Keyboard focus has a fill at least 3:1 against the unfocused button, and a mouse click leaves no
  focus behind. Presentation only.
- The historical AI no longer sends an order the turn's attention cannot cover (the rule the UI's order queue applies),
  so the sim never has to refuse one; the golden playthroughs are unchanged.
- The browser build downloads a smaller heightmap: `heightmap.r16z` (4.1 MB, packed losslessly: row deltas in byte
  planes, one zlib stream) replaces the raw 8.4 MB `heightmap.r16`, which was served outside Unity's build compression.
  It decodes to the same bytes, so the terrain is unchanged (#44).
