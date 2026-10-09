# Civilization VII art direction and technical art, and how a small team could reproduce a "Civ 7-like" look for a historical Israel / Middle East map game

Research date: 2026-10-09. About 15 tool calls. Primary-source art-direction material on Civ VII is thin online. Most of it is in interviews with Firaxis artists and engineers, plus a middleware vendor's case study. There is also an official art book (Jan 2026) that I could not read. Anything I did not find a source for is labelled as an inference or listed as a gap.

## 1. Firaxis' stated art direction (diorama/tabletop, realism, cities, rivers, eras, lighting)

### Takeaway
Firaxis calls Civ VII's look **"readable realism."** It was inspired by **dioramas, model railroads and elaborate tabletop-gaming tables**, and it moves toward realism compared with Civ VI's cartoon style. The governing rule is that terrain and art should be readable without UI icons. Cities sprawl organically across adjacent hexes, and their architecture changes with each Age. Rivers are now simulated and navigable. I found no official statement that the in-game camera uses tilt-shift or depth of field. The "miniature" framing is explicit only in the CG teaser.

### Cited Findings
- A Gamereactor developer interview credits the visual direction to **Jason Johnson's "readable realism."** It says Johnson drew inspiration from **dioramas and model railroads**, and that the environment artists draw on tabletop gaming, making "the most impressive tabletop gaming tables they've seen." Search-result summary of the [Gamereactor interview](https://www.gamereactor.eu/video/698313/Changing+History+One+Age+at+a+Time+-+Civilization+VII+Interview/). The page itself returned 403, so the wording is from the search snippet.
- The same interview says lighting and fog of war set the mood, and that early ages are "very dark, it's imposing." Same source, search snippet only.
- Art Director Jason Johnson keeps "the design of the game and player experience at the forefront." [Player2 interview with FitzGerald, Quickel and Andrade (PAX Aus, Oct 2024)](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- Ryan Andrade (Lead Concept Artist): "Civ 7 has shifted to a more realistic style when compared to previous versions." [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- Matthew Quickel (Senior Lead Environment Artist) on the readability goal: "Can we make it so you never have to look at resource icons?" To make cocoa tiles differ from jungle, the team changed bean size, tree spacing and hue. Assets are checked for readability so the UI can be turned off "as much as possible." [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- Quickel: "We also wanted to ratchet up the fidelity on this game more than any of the previous games." John FitzGerald (Lead Unit Artist): "We have to provide a continent's worth of information every time you zoom out." [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- John Kloetzli (lead graphics programmer) called it "a huge increase in visual fidelity from the previous game." He said each building, unit and leader is full of historically inspired detail, and the game keeps the series' established presentation. [Intel Gaming story](https://game.intel.com/stories/sid-meiers-civilization-vii-next-level-empire-building/)
- **Rivers:** navigable rivers fill a whole tile, and some naval units can sail them. Each tile has elevation and rainfall, including rain shadows from mountains, used to "simulate water flow all the way to the sea." This produces "navigable rivers connecting multiple bodies of water." [Intel Gaming](https://game.intel.com/stories/sid-meiers-civilization-vii-next-level-empire-building/). Both small and navigable rivers run through tiles, not along tile edges. [Civilization Wiki (fandom)](https://civilization.fandom.com/wiki/Civilization_VII). Patch 1.0.1 (Feb 5, 2025) let players build ports on navigable rivers. [Official patch notes](https://civilization.2k.com/civ-vii/game-update-notes/2025-feb-05-patch-1-0-1/)
- **Cities:** a preview says cities "gradually sprawl out through hexes adjacent to where they were founded," and that architecture changes when the Age changes. [Kowatek preview](https://blog.kowatek.com/?p=92609). A fan review says city tiles blend into "an absolutely stunning city." [Erindromeda review](https://erindromeda.pika.page/posts/a-civ-fans-thoughts-on-civ-vii-its-pretty-ok)
- Civs carry over "unique quarters," traditions and wonders across Ages. [Intel Gaming](https://game.intel.com/stories/sid-meiers-civilization-vii-next-level-empire-building/). Settlements start as Towns, which can be upgraded into Cities (the capital starts as a City). [Civ Wiki](https://civilization.fandom.com/wiki/Civilization_VII)
- **Downside of realism:** without Civ VI's colour-coded district rooftops, "There is _no_ identifying buildings from a glance here." [Erindromeda review](https://erindromeda.pika.page/posts/a-civ-fans-thoughts-on-civ-vii-its-pretty-ok)
- **Reception:** "There is a conscious effort to make the map look like a detailed diorama in a museum." The same review places the style between Civ V's realism and Civ VI's cartoon look. [Erindromeda](https://erindromeda.pika.page/posts/a-civ-fans-thoughts-on-civ-vii-its-pretty-ok). A Checkpoint review says the map reads as clear and tasteful from a distance and has small animated details up close, such as fish around reefs and embers at campsites. [Checkpoint Gaming](https://checkpointgaming.net/reviews/2025/02/civilization-vii-review-reinforcing-a-powerful-legacy/). Some players found the palette too desaturated, "smeared in clay muddy grounds," and felt leaders blended into backgrounds. [Steam discussion](https://steamcommunity.com/app/1295660/discussions/2/591762729907275087); [Steam discussion](https://steamcommunity.com/app/1295660/discussions/0/591762563949166307)
- **CG teaser (Halon):** "The entire world was designed to feel as though it could exist on a tabletop." It is meant to look "carved by human hands throughout the ages." It was shot as a single continuous camera move across a miniature world, using forced perspective, controlled scale changes and choreographed lighting. [Halon project page](https://halon.com/work/civilization)
- **Official art book:** *The Art and Making of Sid Meier's Civilization VII* by Rick Barba (Insight Editions, Jan 6, 2026, 256 pp.) has developer interviews, concept art and historical research. [Insight Editions](https://insighteditions.com/products/the-art-and-making-of-sid-meiers-civilization-vii)

### Inferences
- In practice the "living diorama" idea comes down to five things:
  1. Realistic PBR materials with slightly heightened saturation and contrast.
  2. Very dense, hand-placed-looking micro-detail: crop rows, walls, tiny props, animated life.
  3. Terrain that reads at a glance, so resources show through the models themselves.
  4. Cities built as tile-sized urban modules that merge into one continuous townscape.
  5. Lighting that sets the mood by Age.

  "Tilt-shift" is the community's word for it, not Firaxis'. From footage, any depth of field in game looks mild. That is my observation, not sourced.
- The trade-off for Israel/Middle East: realism costs glance-readability. Plan readability aids from day one, such as roof colour accents, banners, lens overlays and silhouettes.

### Gaps
- I found no GDC talk, 80.lv breakdown or ArtStation post by Firaxis artists on Civ VII's terrain or cities, and no named interview where Jason Johnson describes the style himself.
- No official details on time-of-day, a colour-grading LUT, or depth-of-field settings.
- The art book's contents could not be read.

## 2. UI: style, leader/diplomacy screens, narrative events, criticism and patches

### Takeaway
The UI is built in **HTML/CSS/JS on Coherent Gameface**, with one layout for PC, console and handheld. At launch it was the most-criticised part of the game ("feels incomplete or unfinished"). Firaxis promised polish in 1.1.0 and 1.1.1 (March 2025). Patch 1.2.4 later rebuilt the UI framework and broke many mods. Leader presentation was also criticised for losing personality.

### Cited Findings
- Coherent says Civ VII's UI runs on Gameface (HTML5/CSS) inside Firaxis' custom AAA engine. The goal was a "consistent interface tailored for multiple platforms" that shipped at the same time on PC, consoles and handhelds. Update 1.2.2 added more UI polish plus Steam Workshop support. [Coherent Labs case study](https://coherent-labs.com/blog/casestudies/firaxis-case-study/)
- Side (an outside co-dev studio) published a case study titled "Shaping Civilization VII: From Multi-Platform UI to Immersive Storytelling." It describes Gameface as built on standard web technologies and more flexible than Firaxis' earlier proprietary UI. The page text was not readable in full. [Side case study](https://side.inc/lab/sid-meiers-civilization-vii-case-study)
- Modders report the UI is scripted in JavaScript, running on V8 with separate contexts. This is community analysis, not official. [CivFanatics thread](https://forums.civfanatics.com/threads/scripting-runtime-information.695101/)
- **Launch criticism:** Steam reviews "almost universally criticise the game's UI." Notifications are annoying to click through and also disappear without reason. [TheSixthAxis](https://www.thesixthaxis.com/2025/02/07/firaxis-highlights-three-areas-theyll-improve-civilization-7-after-mixed-launch-reviews/). Reviewers called it "incomplete or unfinished" and compared it to a console port. Firaxis responded: "We are aware of and looking into feedback on the game's UI." [80.lv](https://80.lv/articles/less-than-half-of-civilization-vii-s-players-on-steam-are-happy-with-the-game/); [Gamereactor](https://www.gamereactor.eu/civilization-vii-early-access-opens-to-mostly-negative-steam-reviews-1493163/). One reviewer: "The state of the UI in Civilization VII is not acceptable." [Erindromeda](https://erindromeda.pika.page/posts/a-civ-fans-thoughts-on-civ-vii-its-pretty-ok)
- **Patches:**
  - 1.1.0 (Mar 4, 2025): UI polish, and the army unpacking action "refined."
  - 1.1.1 (Mar 25, 2025): Firaxis "aims to improve the UI over the next several months."
  
  [PCGamesN](https://www.pcgamesn.com/civilization-vii/ui-fixes); [CGMagazine](https://www.cgmagonline.com/news/civilization-vii-quality-of-life-updates)
  - 1.2.4: the underlying UI architecture changed to be more responsive and easier to extend. It broke many mods, so a "Legacy" Steam beta branch was offered. [PC Games (DE)](https://www.pcgames.de/Civilization-7-Spiel-74716/News/Neues-Update-Mods-1480362/)
  - 1.2.5: fixed the Steam Deck "Next" button. [SteamDeckHQ](https://steamdeckhq.com/news/civilization-7-fixes-ui-issue-steam-deck/)
- **Leaders:** Andrade says leaders are always close to the camera, so they can be "more detailed and realistic" than buildings and units, which must read at many scales. [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/). Critics say leaders feel less distinct, give generic dialogue ("hmm!"), and don't face the player, like "selecting a character in Mortal Kombat." [Erindromeda](https://erindromeda.pika.page/posts/a-civ-fans-thoughts-on-civ-vii-its-pretty-ok)
- **Typography:** the series logo is a custom all-caps serif that was never released as a retail font. The guide's note on UI sans-serifs refers to Civ VI. [MadeGoodDesigns](https://madegooddesigns.com/civilization-font/)

### Inferences
- Gameface (or any HTML/CSS UI layer) is directly reusable by an indie team. Gameface is licensed middleware. Free equivalents are web UI in Godot or Tauri overlays, Unity UI Toolkit (USS, CSS-like), or Noesis. Lesson from Civ VII: build the UI for information density and glanceable notifications first, and add ornament second.
- I could not verify the "gold/parchment ornamentation" in the brief. From screenshots, my impression is dark navy and teal panels with thin gold filigree frames and serif headings, with narrative events as illustrated vignette cards. Treat that as unverified description.

### Gaps
- No source found on the narrative-event illustration pipeline (artist count, style guide), the exact UI fonts, or a Firaxis UI art lead interview.
- I did not get the post-1.2.x UI state as of late 2026 (e.g., 1.3.x patches).

## 3. Technical art: terrain, blending, decals, foliage, water, fog of war, camera, post-processing, engine

### Takeaway
Officially confirmed:
- A proprietary Firaxis engine.
- Coherent Gameface for UI.
- Intel XeSS upscaling.
- Tools that flag assets over performance budget.
- Development for multiple platforms from the start.
- Simulated hydrology for rivers.
- Fog of war as an outlined or stylised version of the map.

The shader and post-processing internals are not published. Indie teams can rebuild the techniques from public tutorials.

### Cited Findings
- Firaxis' engine is a "custom AAA engine." [Coherent Labs](https://coherent-labs.com/blog/casestudies/firaxis-case-study/)
- Intel XeSS upscaling is integrated. Kloetzli: it gives significant performance improvements "without sacrificing image quality." [Intel Gaming](https://game.intel.com/stories/sid-meiers-civilization-vii-next-level-empire-building/)
- Tools flag assets that miss performance targets. The game was built for multiple platforms from the start. An autoplay build runs on office TVs to judge how it looks from a couch. [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- VFX for disasters builds on *Gathering Storm*, but navigable rivers created new flooding challenges. [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- **Fog of war:** pre-launch players described it as "the same map, but with features outlined," with edges "a little too shredded-up." Some preferred the older cloud style. [Steam](https://steamcommunity.com/app/1295660/discussions/2/591762729907275087); [Steam](https://steamcommunity.com/app/1295660/discussions/0/4431065858194331767)
- **Public techniques for hex terrain blending:**
  - Splat maps across hex cells, at four or five textures per splat. [Catlike Coding Hex Map part 14](https://catlikecoding.com/unity/tutorials/hex-map/part-14/)
  - Hex fog of war and exploration. [Catlike Coding part 20](https://catlikecoding.com/unity/tutorials/hex-map/part-20/)
  - Godot hex-tile shader with neighbour blending through a biome texture array, a per-tile ID texture and noise-broken edges. [Godot Shaders](https://godotshaders.com/shader/hexagonal-tilemap-with-blending/)
  - Baking hex colours into a whole-map splat mask. [Polycount](https://polycount.com/discussion/comment/2682018)
  - Blurred masks used as a cheap flow map for shoreline waves. [MonoGame forum](https://community.monogame.net/t/rounded-hexagon-terrain-how-to/14806)

### Inferences
These are reproduction recipes, not confirmed Firaxis internals.
- **Terrain:** one continuous heightmap mesh. The hex grid lives in data, not geometry. A per-hex biome ID texture plus a noise-warped blend gives organic, non-hex borders. Lay a faint hex-grid overlay on top that can be toggled. Use 4–8 PBR terrain layers (sand, loess, terra rossa, limestone rock, wadi gravel, irrigated field, marsh, snow on Hermon). Add triplanar mapping on slopes.
- **Decals:** roads, field patterns, ruins, burn marks and erosion streaks as projected decals. Crop and orchard patterns (olive, citrus, date palm, wheat) as tile-sized decals plus instanced props.
- **Foliage:** GPU-instanced scatter, driven by biome and a per-tile density mask. Per Quickel, tune spacing, size and hue per resource so each tile reads on its own.
- **Water:** a sea and lake shader with depth-based colour, a shoreline foam band from a distance field, and a flow-mapped river ribbon. Coastal reef details are cheap wins. For the Jordan River, Sea of Galilee and Dead Sea, add salt-flat shading for the Dead Sea.
- **Fog of war:** two-level. Unexplored is a parchment or desaturated map-paper look with a hand-drawn contour edge. Explored-but-not-visible is the desaturated terrain. A ragged noise mask at the boundary handles the edge. Civ VII's own fog was criticised as hard to parse, so keep biome colour visible in "explored" fog.
- **Camera:** a steep perspective camera, roughly 45–60° pitch, narrow-to-moderate FOV (about 30–40°) for a miniature feel. Add mild, distance-based depth of field only when zoomed in, plus a strategic zoom-out that switches to a flatter map or lens view.
- **Post-processing:** ACES or filmic tonemapping, a mild warm grade, subtle vignette and bloom, a small amount of saturation lift and SSAO. Per-Age LUTs: darker and moodier in antiquity, crisper and cooler in modern times, matching Firaxis' "early ages are very dark" note.

### Gaps
- No public Firaxis talk on terrain shading, foliage system, water or post-processing. Camera FOV and DoF values are not published.

## 4. Comparable games and what an indie team can reach

### Takeaway
- **Ara: History Untold** (Oxide engine) leans into a living, animated realistic world with no-grid cities.
- **Endless Legend 2** goes for colourful, stylised science-fantasy.
- **Civ VII** is realistic diorama.

For a small team, a stylised-realistic middle ground is reachable. It needs strong terrain shaders, a limited set of modular buildings and heavy instanced scatter. Full Civ VII per-civ, per-era bespoke architecture is not.

### Cited Findings
- Ara's main visual difference from Civ is its "living world." Reviewers note beautiful shorelines and animals moving across the map, and compare its realism to Civ V. [Gamereactor review](https://www.gamereactor.eu/ara-history-untold-1436193/); [GameStar](https://www.gamestar.de/artikel/ara-history-untold-civilization-gefaehrlich-werden-will,3407103.html). Ara has "nicely populated cities without a hex grid." [CivFanatics](https://civfanatics.com/?p=9693). It uses the engine from *Ashes of the Singularity*. [mein-mmo](https://mein-mmo.de/civilization-vii-nicht-erschienen-alternative-wenigen-wochen-seam/)
- Endless Legend 2 (Amplitude, Hooded Horse): "striking art style," "a fantasy explosion of colours." Its oceanic world is reshaped by seasons and tidefalls. [Shacknews](https://shacknews.com/article/143855/endless-legend-2-steam-early-access-release-window); [GameStar](https://www.gamestar.de/artikel/endless-legend-2-test-early-access,3439849.html); [Game Developer](https://www.gamedeveloper.com/press-release/endless-legend-2-revealed-a-new-chapter-in-the-endless-universe-begins-soon-in-early-access)
- A German preview compared Civ VII to the "charm of Anno 117." [PCWorld](https://www.pcworld.com/article/2457668/civilisation-vii-in-preview-the-old-civ-love-with-the-charm-of-anno-117.html)

### Inferences
- **Indie feasibility ranking** (my judgement):
  1. Clean stylised low-poly diorama, like Synty, easiest.
  2. Stylised-realistic with PBR terrain, simple buildings and heavy scatter, best value.
  3. Civ VII-level realistic micro-detail across eras, needs a AAA-sized art team.
- Old World looks closer to Civ V-era realism with painterly terrain, and Humankind has hand-painted-feeling terrain with an ink-map look. These are from general knowledge and were not verified this session.

### Gaps
- No sourced visual comparisons for Humankind, Old World or Millennia were gathered, and no Endless Legend 2 release-status confirmation as of Oct 2026.

## 5. Asset count and scale, kitbash packs, Middle-East architecture

### Takeaway
Firaxis says scale and readability are the hard problems: a "continent's worth of information" at zoom-out, and assets with a "range of sizes." No Synty or POLYGON pack is Middle-East specific. POLYGON Ancient Egypt (870+ prefabs) and Ancient Empire (700+) are the nearest. Mixing them with modular kits and custom hero assets (Jerusalem, Bauhaus, kibbutz) is the realistic route.

### Cited Findings
- Buildings and units are "a bit more complicated because we have such a range of sizes to consider." [Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)
- POLYGON Ancient Egypt has 870+ prefabs, including modular temples, houses, pergolas, tents and obelisks, and supports Unity Built-in and URP. [Synty Store](https://syntystore.com/products/polygon-ancient-egypt)
- POLYGON Ancient Empire (Greco-Roman) has 700+ prefabs and works with other POLYGON packs. [Unity Asset Store](https://assetstore-fallback.unity.com/packages/3d/environments/historic/ancient-empire-synty-polygon-stylized-low-poly-art-assets-224020)
- POLYGON Prototype has modular walls, doors and roofs, 444 assets. [Search summary, Synty](https://syntystore.com/5b848)
- I found no dedicated Synty Middle East or Arabian pack. Search summary; see [Fab listing](https://www.fab.com/listings/08c843dc-696d-450e-971a-c4c9a2ce781f?lang=ar) for a possibly relevant Fab item, contents unverified.

### Inferences
- Synty's flat-shaded low-poly look clashes with a Civ VII realistic target. Pick one lane:
  - **Stylised lane:** Synty or POLYGON plus custom flat-shaded pieces, with a strong grade.
  - **Realistic lane:** Fab/Quixel/Megascans-style scanned rock, sand and foliage for terrain, plus custom mid-poly buildings using trim sheets.

  Don't mix the two.
- **Minimum set** (rough estimate):
  - About 8–12 terrain layers.
  - About 40–60 scatter props.
  - Per era, about 6–10 modular urban tile variants (residential low and high, market or souk, religious, industrial, port, farm or kibbutz).
  - About 10–15 hero landmarks (Western Wall/Temple Mount area as a landmark silhouette, Tower of David, Acre walls, Caesarea, Masada, Bahá'í gardens, Azrieli-type towers).
  - Units as small squads of 3–5 figures.
- **Era architecture palette** for prompting and kitbash:
  - **Antiquity:** mudbrick and fieldstone, flat roofs, terraces, Herodian ashlar.
  - **Ottoman / late Ottoman:** domed stone houses, arched windows, khans, minarets, red-tile Templer roofs in Haifa.
  - **Mandate / 1930s:** Bauhaus "White City" Tel Aviv, with white stucco, horizontal ribbon balconies, rounded corners, pilotis and thermometer stairwell windows.
  - **Kibbutz:** low white blocks with red-tile roofs, water tower, dining hall, orchards and cypress windbreaks.
  - **Jerusalem:** mandatory Jerusalem limestone, a warm golden-cream colour.
  - **Modern:** glass towers in Tel Aviv and Ramat Gan, and highways.

  For the "city merges across tiles" effect, build each urban tile with roads that line up at the edges, and pick variants by neighbour count.

### Gaps
- No official Civ VII asset counts (building, unit or leader totals, polycounts) were found.
- Specific Fab or Asset Store Middle-East packs (e.g., "Arabian/Middle East town" kits) were not verified.

## 6. Reference image descriptions for prompting concept art

### Takeaway
Prompts should combine "museum diorama / model railroad / tabletop miniature" with "readable realism," a high three-quarter top-down camera, and era- and region-specific materials.

### Cited Findings
- The style anchors are diorama, model railroad and tabletop table, plus readable realism. [Gamereactor](https://www.gamereactor.eu/video/698313/Changing+History+One+Age+at+a+Time+-+Civilization+VII+Interview/). The teaser is "carved by human hands," shot as a miniature on a tabletop. [Halon](https://halon.com/work/civilization)

### Inferences (prompt drafts)
- **Map overview:** "Top-down 50-degree view of a hex-based strategy map of the southern Levant, museum diorama, model-railroad miniature realism, golden limestone hills of Judea, green coastal plain, Jordan River ribbon winding to the Dead Sea with white salt flats, terraced olive groves, warm late-afternoon light, subtle hex grid overlay, soft depth of field at frame edges, high detail, readable terrain."
- **Ottoman-era town:** "Miniature diorama of a late-Ottoman Levantine walled port town spread across several hex tiles, domed stone houses, minaret, caravanserai courtyard, fishing boats, Mediterranean coast, warm grade, tabletop scale."
- **Bauhaus Tel Aviv:** "Miniature diorama of 1930s White City Tel Aviv, white Bauhaus apartment blocks with rounded balconies, palm-lined boulevard, sea in background, bright noon light, model-railroad realism."
- **Kibbutz tile:** "Single hex tile diorama of a 1950s kibbutz, red-roofed white bungalows, water tower, cypress windbreak, citrus orchard rows, dirt roads aligning to tile edges."
- **Jerusalem landmark:** "Hero landmark miniature, walled Old City in pale golden Jerusalem stone, domes and towers silhouetted, readable at strategic zoom, tabletop diorama."
- **Narrative event card:** "Painterly illustrated vignette, 3:2, muted ochre and teal palette, framed by thin gold filigree, historical scene of [event], digital oil painting."

### Gaps
- No official Civ VII prompt or style guide exists publicly. These prompts are my synthesis.
