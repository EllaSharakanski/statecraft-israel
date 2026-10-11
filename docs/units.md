# Unit miniatures: the art slice

The owner's target for units (2026-10-11) is the promo: `promo/final/03-gameplay-1936.png` and the demo trailer
(`promo/demo/out/demo-16x9.mp4`, 0:06–0:30). It shows Civ-style miniatures standing on hexes at symbolic scale: a trio of
era-correct riflemen with a selection ring, a mounted rider, and aircraft with Israeli roundels flying low with a shadow.
This page records the slice that matches it, the pipeline choice behind it, the scale, and the sources for every uniform,
aircraft and ship.

The slice is a **showcase only**. It reads nothing from the sim and changes no game state. Normal play draws the units
from the sim's state and sourced map-only data with the same models: see "Units in play" below. Where units move and
fight is war mode's job (#104, with the sim side in #21). The golden hashes do not move.

## How to see it

- **WebGL:** add `units` to the page URL (`index.html?units`). Normal play never shows it, and neither does a plain
  `?shot`: every track's art-checkpoint metrics stay comparable (controller ruling, 2026-10-11).
- **Art checkpoint:** `tools/art_checkpoint.sh --units <name>` adds `&units` to the WebGL screenshots and `-units` to the
  editor renders. With `shot` the aircraft holds a fixed point, so the screenshots repeat.
- **Desktop player:** run with `-units`.
- **Editor:** `MapCapture.Promo` renders it when given `-units`.
- **Frame cost:** `?vprofile&units` prices it as the `unitshowcase` toggle (`median(all) - median(-unitshowcase)`). The
  `units` toggle is the units in play.

Code: `unity/Assets/Scripts/Map/Units/`. The materials are made by `UnitMaterialAssets.Create` in
`unity/Assets/Resources/Units/`. Tests: `UnitShowcaseTests`.

## Owner rulings (2026-10-11)

1. **Units are Civ-style tokens at about 2,000x real size** (see "Scale").
2. **Promo-quality close-zoom models come from a separate track:** a cloud-GPU AI-3D pipeline (TRELLIS, MIT). The
   code-built miniatures here stay as the far-zoom and fallback version.

**The art hook.** An authored model replaces the code-built one for its unit type when a prefab exists at
`unity/Assets/Resources/Units/Authored/<unit type>`. The unit types are the `*Type` constants in `UnitShowcase`, for
example `rifleman-1948-steel-helmet` and `spitfire-mk9e-101sqn-1948`.

An authored model follows the code-built convention:
- real metres, facing +Z;
- origin on the ground (an aircraft: its centre of gravity);
- its own materials;
- an authored aircraft brings its own propeller.

The showcase places and scales it as it does the code-built model. That is the whole hook: there is no registry beyond
it, and there are no level-of-detail switches yet.

## What is in the slice

| Piece | Model | Where (PromoPose.Gameplay03) |
|---|---|---|
| Rifle trio | three 1948 IDF riflemen: bush hat with rifle held ready, Mk II steel helmet with rifle grounded, Palmach stocking cap with rifle slung | on the centre of the hex nearest 34.88 E, 32.01 N (east of Tel Aviv, clear of the settlement labels), inside a white selection ring that fits within the hex |
| Aircraft | Supermarine Spitfire LF Mk IXe of 101 Squadron, late 1948 – January 1949: red spinner and red-and-white rudder (attested on Spitfires for January 1949), roundel without an outer ring | a 16 km loop over Tel Aviv and the coast, 4.2 km above the highest ground under it, banked 28 degrees, its shadow cast on the ground by the sun |
| Navy | modified Flower-class corvette INS Haganah (K-20), 1948, 63.4 m | off Jaffa (34.63 E, 32.02 N) |

Not in the slice:
- **The mounted rider.** No 1948 IDF mounted unit was sourced for this slice. The promo's rider belongs to 1936, so it
  waits for the Yishuv era (#49) and a source.
- **Civilian boats.** The owner ruled them out of scope; navy ships are units.

## Units in play

Normal play draws one token per unit from two inputs: the sim's state and `content/map/units_1948.yaml`.
- **The state** gives the date, the security level and the reserve call-up (`GameState.Date`, `Rung`, `Mobilization`).
  Nothing else in the sim describes the military: it has no formations and no positions (design:
  `docs/superpowers/specs/2026-10-11-units-in-play-design.md`).
- **The data file** is map-only. `ContentLoader` never reads it, so the content hash and the goldens do not move
  (`UnitsInPlayTests.TheUnitFileDoesNotMoveTheContentHash`). The WebGL manifest ships it.

Code: `UnitData` (the file), `UnitRoster` (which units now), `UnitPlacer` (which hex), `UnitLayer` (the tokens and the
ring), `UnitCard` and `InfoPanelPresenter` (the card). Tests: `UnitsInPlayTests`.

### Owner rulings (2026-10-11)

1. One token per brigade.
2. The number of figures follows the call-up: 3 at or above the security-level-4 floor (`rung_min_mobilization`,
   0.4), 2 at or above the level-3 floor (0.15), else 1.
3. 101 Squadron shows an Avia S-199 until the Spitfire's sourced date (21 October 1948), then the Spitfire.
4. The four brigades with no sourced position (Carmeli, Alexandroni, the 8th, Oded) are listed in the file and hidden.

The controller adopted the design's defaults for the rest:
- tokens stand only at dated, sourced positions and do not move with operation events;
- after the armistices only what a source says continued is shown (no source gives a later position yet, so no token
  shows after 20 July 1949);
- there are no orders: the unit's card links to the Defence screen.

### What the player sees

| Unit | Positions (from the sources, dated) |
|---|---|
| Golani (1st) | Yavne'el, May–November 1948; the Julis camp from December 1948 |
| Kiryati (4th) | Tel Aviv |
| Givati (5th) | Gedera (the first of the three places its source names) |
| Etzioni (6th) | Jerusalem |
| 7th Brigade | Latrun, 24 May–31 May 1948; Ein Shemer, June 1948 |
| Harel (10th) | Sha'ar HaGai, to 21 December 1948 (Operation Horev, in the south, gives no place for it) |
| Yiftach (11th) | Lydda, 9–19 July 1948 (Operation Dani) |
| Negev (12th) | the Mekorot camp near Nir Am |
| 101 Squadron | Ekron, 20–29 May 1948; the Herzliya strip, 30 May–September 1948; Hatzor from October 1948 |
| INS Haganah, INS Wedgwood | Haifa harbour |

A position with no stated end runs to 20 July 1949, the last armistice. Every row, date and coordinate has its source
and check date in the file. Rows marked `wikipedia_only` (all but 101 Squadron, whose markings come from Eduard) are
drafts until checked against a published history (Netanel Lorch, *The Edge of the Sword*, 1961) or the IDF and Defense
Establishment Archives.

**Placement.** A token stands on the nearest hex to its anchor that is clear (`UnitPlacer`, at most 3 hexes away):
- the ring's disc stays off every settlement footprint (the kits' discs, times 1.1);
- an army token's three figure spots (always three, so the hex does not change with the call-up) and a parked
  aircraft's centre are on land and at least 700 m beyond every railway and road corridor;
- a ship needs a sea hex, and no two tokens share a hex.

So Kiryati stands east of Tel Aviv, not inside it: the offset is presentation, not a claim about the position.
`UnitsInPlayTests.EveryTokenOfTheWarStandsClearOfTheTownsAndTheRoutes` checks every week of the war.

**Look.** The models and the authored-prefab hook are the slice's.
- An army token is 1 to 3 riflemen.
- The squadron flies the slice's loop over its base at security level 4 or 5 and stands parked below that.
- A ship is the K-20 corvette model; Wedgwood was the same modified Flower class.

Clicking a token's hex selects it: the white ring goes round the hex and the info panel shows a read-only card.
- The card gives the name, the number and the service, where the unit stands and since when, its formation and first
  commander where sourced, and the sector in the source's words.
- It also shows the national values the token reflects ("Reserve call-up 80% · security level 5 of 5") and the source
  hosts, with a draft note.
- It has no orders; a button opens the Defence screen.

**The Avia S-199** (`UnitModels.AviaS199`; types `avia-s199-101sqn-1948` and, from 15 October 1948,
`avia-s199-101sqn-1948-10`):
- The Bf 109G airframe, 8.94 m long with a 9.92 m span, with the Jumo 211F's wide three-blade paddle propeller and the
  MG 151 pods under the wings (Avia S-199 article).
- One green on all surfaces: Avia's standard finish, and "the same shade of green" on D-123 in Israel (Eduard Info
  11/2025, pp. 53–54). The exact shade is unverified.
- Two white bands round the rear fuselage, and Star of David roundels under the wings. These come from the 101 Squadron
  photographs of 1948 on Wikimedia Commons: the roundels show from a low camera in front of a nose-up aircraft, so they
  are on the lower surface.
- From Operation Ten Plagues (later Yoav, 15–22 October 1948) it adds the red spinner, the red and white rudder stripes
  and roundels atop the wings, "for the first time" (Eduard p. 53).
- The squadron's skull badge on the nose is too small to read at map scale and is left out.

**Cost.** The budget is 2.5 ms or less for the `units` toggle at gameplay03 (docs/visual-v1.md). The measurement was
headless `?vprofile` on the WebGL build of 4640ca6, 1920x1080 at DPR 1, 2026-10-11.

The machine was busy: the load average was 7–10 and other sessions were running Unity. The exclusive lock waited more
than 20 minutes without getting the machine, so this run took a **shared** lock. The numbers are an upper bound, not a
clean measurement.

| Pose | all | -units | units cost | none |
|---|---|---|---|---|
| gameplay03 | 10.42 ms (spread 4.44, p95 12) | 9.33 ms (spread 0.11, p95 11) | 1.09 ms | 7.47 ms |
| diorama02 | 21.46 ms (spread 8.20, p95 29) | 17.69 ms (spread 8.26, p95 20) | 3.77 ms, inside the noise | 13.81 ms |

- **gameplay03** is within the budget even at this upper bound. The new-game state draws 6 brigades (up to 18
  riflemen) and 2 ships.
- **diorama02** cannot be read here. Its spreads (8 ms) are larger than the difference, and on this run other toggles
  also come out above "all" (for example -routes at gameplay03).
- A clean idle-machine re-measure is left open.
- The layer rebuilds only when the roster changes (a new turn, a call-up or security order). Between those it does no
  per-frame work beyond a flying aircraft's transform.

**Left open.**
- The rows are drafts from Wikipedia (and Eduard for the markings) until a historian checks them against Lorch or the
  archives.
- 101 Squadron's move from Herzliya to Hatzor is October 1948 in en.wiki, and November 1948 in he.wiki and Eduard
  p. 54. The map follows en.wiki.
- Wedgwood's Israeli name and pennant number conflict between its two articles. The card shows "INS Wedgwood" and no
  number.
- No source yet places any unit after 20 July 1949, so the map shows no units after the armistices.
- The Spitfire's red spinner and striped rudder are attested on the Spitfires for January 1949. Eduard dates the
  squadron's markings to October 1948, so the October–December 1948 Spitfire markings rest on that.
- In the historical playthrough the call-up stays at or above 0.4 until the armistices, so the tokens keep three
  figures.

## Pipeline choice: code-built miniatures

Three pipelines were tried on a development Mac (Apple M5 Pro, 48 GB) on 2026-10-11. Each was placed on the map at the
same scale and rendered at the gameplay zoom and close up, next to the promo's trio; the table summarises what each
produced. The renders are not in the repository.

| Pipeline | Result | Licence |
|---|---|---|
| (a) Image-to-3D, run locally: TripoSR (MIT code and weights), CPU, about 3.5 s per mesh | Inputs were a promo crop and a 1948 photograph. Out came a blurry blob of about 45,700 triangles with baked vertex colours: the head and rifle were lost to the background cut-out, the side view was a flat card for the rider, and there was no rig. It would need clean, sharp turnaround references, which we cannot make locally, plus manual retopology. | MIT; fine. |
| (a) Hunyuan3D-2, TRELLIS | Not run locally. Hunyuan3D-2's community licence excludes the EU, the UK and South Korea, which is not compatible with an open project. TRELLIS needs an NVIDIA GPU with CUDA; the owner chose it for the cloud-GPU close-zoom track (rulings above). | Hunyuan3D-2: not compatible. TRELLIS: MIT. |
| (b) CC0 base meshes: Quaternius Universal Base Characters (free "Standard" kit, CC0) | A clean, rigged body, but a stylised "superhero" figure in underwear, in a T-pose. Every uniform, hat, belt and rifle would have to be modelled by hand in Blender. Kenney's characters are blocky, low-poly figures, which the spec rules out (§6, "no low-poly packs mixed in"). | CC0; fine. |
| **(c) Code-built from primitives (chosen)** | Lathed limbs and torsos, ellipsoids, boxes and plates, painted from one palette texture with a dark wash at the feet and a dry-brushed highlight on top. One URP Lit material, made at runtime, with no texture file. About 3,600 triangles per figure. Real silhouettes for each headgear and rifle pose. The figures cast and receive shadows. | Our own code, MIT. |

**Why (c):** at the gameplay zoom a figure is about 90 px tall. What reads at that size is the silhouette (hat brim,
rifle, stance), the colour blocks (khaki shirt, dark trousers, pale webbing) and the shadow. In the gameplay03
checkpoint the code-built trio matches the promo's trio in size (87 px) and reads with all three. They are exact to the
sources: each part is a sourced item, and a change is a reviewed line of code. Era variants (1956 helmets, 1967 Uzis, 1973 flak vests) are parameters. Builds are deterministic, there is no
licence question, and the build-size cost is code only.

**Their limit:** close up (the 6 km hero zoom) they look like wooden mannequins, with faceted limbs and no faces or
folds. The promo's close shots (0:21) show sculpted miniatures. The way past that limit is the owner's close-zoom track,
which plugs authored models in through the art hook above.

## Scale

Measured on the promo scaled to 1920 px wide: a rifleman is about 87 px tall, inside a ring about 190 px across. Our
gameplay03 pose frames the same hex count as the promo (about 9 across: see `PromoPose`). The first checkpoint, at 2600x,
drew our riflemen 113 px tall. So the slice uses `UnitShowcase.FigureScale = 2000`: a man about 3.6 km tall on 5 km
hexes, with the ring at 0.66 of a hex's circumradius. The aircraft (x420, a 4.7 km span) and the ship (x95, 6.0 km) are
sized against the figures, as Civ sizes them, not against each other.

**Owner ruling (2026-10-11): units stay Civ-style tokens at about 2,000x.** This is far above the 10–25x ruling for
buildings and trees. At 10–25x a man is 18–45 m tall, which is under one pixel at 60 km. The promo, which wins over strict
scale by the owner's ruling, draws units as tokens. The scale is one constant.

## Sources (checked 2026-10-11)

Uniform and equipment, 1948:
- Palmach (Negev Brigade) soldiers briefed before an operation, 1948: khaki shirts and trousers, web belts with pouches,
  knitted stocking caps, rifles. Public domain.
  https://commons.wikimedia.org/wiki/File:Palmach-negev-instructed2operation.jpg
- Negev Brigade soldiers, 20 October 1948: Mk II steel helmets, khaki drill, field gun. Public domain.
  https://commons.wikimedia.org/wiki/File:Negev_Brigade_soldiers_1948.jpg
- Beersheba, 21 October 1948: brimmed bush hats, khaki shirts and shorts, belts. CC BY 4.0 (National Library of Israel).
  https://commons.wikimedia.org/wiki/File:1948_Arab-Israeli_War_997008137085905171.jpg
- A woman fighter in Jerusalem, about March 1948 (photograph by Yehuda Aizenshtark): khaki shirt and trousers, web
  belt, a grenade, and a Lee-Enfield No. 4 rifle (per the Commons category). CC BY 2.5 (PikiWiki).
  https://commons.wikimedia.org/wiki/File:PikiWiki_Israel_38816_Israel_Defense_Forces.jpg

Aircraft:
- Israel bought 59 Spitfire Mk IXs from Czechoslovakia in 1948–49; IAF Spitfires fought in the 1948 war (the last air
  combat was on 7 January 1949). https://en.wikipedia.org/wiki/Supermarine_Spitfire_operational_history
- Ferry flights of the Czech Spitfires to Israel (Operation Velvetta, 1948). https://en.wikipedia.org/wiki/Operation_Velvetta
- The Avia S-199 flew first, on 29 May 1948 (Operation Pleshet). https://en.wikipedia.org/wiki/Avia_S-199
  The Spitfire was chosen over it because the promo shows a Spitfire (promo beats era, and both are 1948-correct).
- Spitfire Mk IX shape (an elliptical wing, a four-blade propeller): the IAF Museum airframe at Hatzerim. CC BY 2.5.
  https://commons.wikimedia.org/wiki/File:Spitfire-MkIX-hatzerim-1-1.jpg
  Many IAF IXes had clipped wing tips (Fundekals review, below). The slice keeps the full elliptical wing that the promo
  shows.
- Variant **LF.IXe**, and 101 Squadron's **red spinner** and **red-and-white striped rudder**. On the Spitfires these are
  attested for **January 1949** (IsraDecal); the slice is dated "late 1948 – January 1949".
  - IsraDecal Studio, "Spitfire Mk.IX, Israeli Air Force" (IAF-123, 1:24) marking instructions, May 2023. It lists
    "Spitfire LF.IXe 2011/26, 101 Squadron, January 1949", painted in RAF colours, with a red spinner and the rudder
    painted "in White. Paint Red stripes".
    https://www.isradecal.com/_files/ugd/ddb4b4_42f20e2bc83f4ba895c816e97ad03151.pdf
  - Eduard Info 11/2025: "In preparation for Operation Ten Plagues (October 15-22, 1948), later renamed Operation Yoav,
    101 Tayeset fighters obtained high visibility markings consisting of red spinners and red and white stripes on the
    rudders". https://info.eduard.com/en/11-2025-1/strana-53
    The page shows the squadron's Avia S-199s ("Messers"), not its Spitfires. So it dates the squadron's markings to
    October 1948, but **on the Spitfires the October 1948 date is unverified**.
  - A Fundekals decal review also says 101 Squadron "had the white and red stripes on the rudder".
    https://mail.modelingmadness.com/scott/decals/fundekals/fd48idfspit.htm
  - **Unverified:** the stripes' direction. The slice draws them across the rudder.
- Roundel of 1948–49: a blue Star of David on a white disc, the star clear of the disc's edge, **with no outer blue ring**.
  - Flags of the World, citing a 2002 survey of *Born in Battle*: "almost all the roundels show the star within, but not
    reaching the edges of, the white disk ... This includes early planes from the 1948/49 war of independence (like
    Spitfire ...)". It adds: "the only aircraft showing a roundel with a thin blue ring around are Mosquitos and Meteors
    from the 1950ies". https://crwflags.com/fotw/flags/il%5Eairfo.html
  - The Commons "early variant" SVG with the ring is drawn from a Mosquito photograph, a 1950s type, so it is not used.
- **Unverified (one hobby source, IsraDecal):** the camouflage, RAF ocean grey and dark green over medium sea grey. A
  forum report of brown overpaint in theatre is an open question. The museum airframe is bare metal in post-war markings.
  The standard reference, Alex Yofe's *Spitfire Mk. IX in Israeli Air Force Service 1948–1956*, has not been checked.

Navy:
- INS Haganah (K-20), ex-HMCS Norsyd, a modified Flower-class corvette, 208 ft (63.4 m) overall, with an upright
  funnel. https://en.wikipedia.org/wiki/HMCS_Norsyd
  - In 1946 it was sold to the Mossad for Aliyah Bet as the *Balboa*, renamed *Haganah*, and carried Holocaust survivors
    towards Mandate Palestine.
  - The Israeli Navy took it over and rearmed it in 1948.
- Photograph of K-20 in Haifa harbour, 1948: light grey, mast at the bridge, funnel aft of it. Public domain.
  https://commons.wikimedia.org/wiki/File:Hagana_K-20_ship_Israel.jpg
- Photograph of INS Haganah, about 1949. Public domain. https://commons.wikimedia.org/wiki/File:INSHaganah.jpg

Avia S-199 (units in play):
- Type, dimensions, the Jumo 211F with its paddle-bladed propeller, the underwing MG 151 kit; the first four arrived on
  20 May 1948 and flew first on 29 May; mostly withheld by the end of October 1948.
  https://en.wikipedia.org/wiki/Avia_S-199
- Eduard Info 11/2025, p. 53: 101 Squadron at Herzliya (July–August 1948) and Hatzor (October 1948); D-123 in "the same
  shade of green"; for Operation Ten Plagues (15–22 October 1948) red spinners, red and white rudder stripes, and Star
  of David roundels "applied atop the wings for the first time". https://info.eduard.com/en/11-2025-1/strana-53
- Eduard Info 11/2025, p. 54: Avia-built aircraft "bore the standard MNO Smalt Avion 2036.02 paint scheme on all
  surfaces"; 101 Squadron moved to Qastina (Hatzor) on 21 November 1948. https://info.eduard.com/en/11-2025-1/strana-54
- Photographs of 101 Squadron S-199s in 1948 (public domain): the white rear-fuselage bands, the roundels on the lower
  wing surfaces, the paddle propeller.
  https://commons.wikimedia.org/wiki/File:Avia-S-199-IAF-101Sqn-Tel-Nof-Israel-1948-01.jpg and
  https://commons.wikimedia.org/wiki/File:Avia_S-199_in_June_1948_(Israeli_Air_Force).png
- The Spitfire from 21 October 1948: IAF Spitfires fought Egyptian Spitfires that day, and a 101 Squadron pilot claimed
  a victory. https://en.wikipedia.org/wiki/Supermarine_Spitfire_operational_history

Brigades, the squadron's bases and the ships: every position, date and coordinate has its own sources and check date
in `content/map/units_1948.yaml`.

The photographs are references only: no image file is copied into the repository.
