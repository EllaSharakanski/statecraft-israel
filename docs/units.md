# Unit miniatures: the art slice

The owner's target for units (2026-10-11) is the promo: `promo/final/03-gameplay-1936.png` and the demo trailer
(`promo/demo/out/demo-16x9.mp4`, 0:06–0:30). It shows Civ-style miniatures standing on hexes at symbolic scale: a trio of
era-correct riflemen with a selection ring, a mounted rider, and aircraft with Israeli roundels flying low with a shadow.
This page records the slice that matches it, the pipeline choice behind it, the scale, and the sources for every uniform,
aircraft and ship.

The slice is a **showcase only**. It reads nothing from the sim and changes no game state. Where units stand, move and
fight is war mode's job (#104, with the sim side in #21). The golden hashes do not move.

## How to see it

- **WebGL:** add `units` to the page URL (`index.html?units`). The art checkpoint's `?shot` shows it too, with the aircraft
  held at a fixed point so screenshots repeat. Normal play never shows it.
- **Desktop player:** run with `-units`.
- **Editor:** `MapCapture.Promo` (run by `tools/art_checkpoint.sh`) renders it.
- **Frame cost:** `?vprofile&units` prices it as the `units` toggle (`median(all) - median(-units)`).

Code: `unity/Assets/Scripts/Map/Units/`. The materials are made by `UnitMaterialAssets.Create` in
`unity/Assets/Resources/Units/`. Tests: `UnitShowcaseTests`.

## What is in the slice

| Piece | Model | Where (PromoPose.Gameplay03) |
|---|---|---|
| Rifle trio | three 1948 IDF riflemen: bush hat with rifle held ready, Mk II steel helmet with rifle grounded, Palmach stocking cap with rifle slung | on the coastal plain south-east of Jaffa (34.84 E, 31.93 N), inside a white selection ring |
| Aircraft | Supermarine Spitfire LF Mk IX in IAF service, 1948 | a 16 km loop over Tel Aviv and the coast, 4.2 km above the highest ground under it, banked 28 degrees, its shadow cast on the ground by the sun |
| Navy | modified Flower-class corvette INS Haganah (K-20), 1948 | off Jaffa (34.63 E, 32.02 N) |

Not in the slice:
- **The mounted rider.** No 1948 IDF mounted unit was sourced for this slice. The promo's rider belongs to 1936, so it
  waits for the Yishuv era (#49) and a source.
- **Civilian boats.** The owner ruled them out of scope; navy ships are units.

## Pipeline choice: code-built miniatures

Three pipelines were tried on this Mac (Apple M5 Pro, 48 GB) on 2026-10-11. Each was placed on the map at the same scale
and rendered at the gameplay zoom and close up. The evidence is in the spike report (agent scratchpad, `units-report.md`).

| Pipeline | Result | Licence |
|---|---|---|
| (a) Image-to-3D, run locally: TripoSR (MIT code and weights), CPU, about 3.5 s per mesh | Inputs were a promo crop and a 1948 photograph. Out came a blurry blob of about 45,700 triangles with baked vertex colours: the head and rifle were lost to the background cut-out, the side view was a flat card for the rider, and there was no rig. It would need clean, sharp turnaround references, which we cannot make locally, plus manual retopology. | MIT; fine. |
| (a) Hunyuan3D-2, TRELLIS | Not run. Hunyuan3D-2's community licence excludes the EU, the UK and South Korea, which is not compatible with an open project. TRELLIS needs an NVIDIA GPU with CUDA. | Hunyuan3D-2: not compatible. TRELLIS: MIT. |
| (b) CC0 base meshes: Quaternius Universal Base Characters (free "Standard" kit, CC0) | A clean, rigged body, but a stylised "superhero" figure in underwear, in a T-pose. Every uniform, hat, belt and rifle would have to be modelled by hand in Blender. Kenney's characters are blocky, low-poly figures, which the spec rules out (§6, "no low-poly packs mixed in"). | CC0; fine. |
| **(c) Code-built from primitives (chosen)** | Lathed limbs and torsos, ellipsoids, boxes and plates, painted from one palette texture with a dark wash at the feet and a dry-brushed highlight on top. One URP Lit material, made at runtime, with no texture file. About 3,600 triangles per figure. Real silhouettes for each headgear and rifle pose. The figures cast and receive shadows. | Our own code, MIT. |

**Why (c):** at the gameplay zoom a figure is about 100 px tall. What reads at that size is the silhouette (hat brim,
rifle, stance), the colour blocks (khaki shirt, dark trousers, pale webbing) and the shadow, and the code-built figures
match the promo on all three. They are exact to the sources: each part is a sourced item, and a change is a reviewed line
of code. Era variants (1956 helmets, 1967 Uzis, 1973 flak vests) are parameters. Builds are deterministic, there is no
licence question, and the build-size cost is code only.

**Their limit:** close up (the 6 km hero zoom) they look like wooden mannequins, with faceted limbs and no faces or
folds. The promo's close shots (0:21) show sculpted miniatures. The path past that limit, when the art track gets to it:
hand-sculpted figures in Blender over a CC0 base (b), in the same palette and silhouettes, with the code-built figures kept
as the far LOD.

## Scale

Measured on the promo at the gameplay zoom, a rifleman stands about 0.38 of a hex's screen width tall. Our gameplay03 pose
frames the same hex count as the promo (about 9 across: see `PromoPose`). With 5 km hexes, a 50-degree camera pitch and a
35-degree field of view at 60 km, that is a figure about 5.5 km tall: `UnitShowcase.FigureScale = 2600` times a real man.
The aircraft (x520) and the ship (x105) are sized against the figures, as Civ sizes them, not against each other.

**Open question for the owner.** This is far above the "10–25x real" ruling for buildings and trees. At 10–25x a man is
18–45 m tall, which is under one pixel at 60 km. The promo (which wins over strict scale, by the owner's ruling) draws
units as tokens. Options:
1. Keep units as tokens at about 2,600x, as now.
2. Scale units with the zoom, Civ-style, so they keep a constant size on screen.
3. Make them smaller and accept that they only read at the hero zoom.

The slice does (1), and the scale is one constant.

## Sources (checked 2026-10-11)

Uniform and equipment, 1948:
- Palmach (Negev Brigade) soldiers briefed before an operation, 1948: khaki shirts and trousers, web belts with pouches,
  knitted stocking caps, rifles. Public domain.
  https://commons.wikimedia.org/wiki/File:Palmach-negev-instructed2operation.jpg
- Negev Brigade soldiers, 20 October 1948: Mk II steel helmets, khaki drill, field gun. Public domain.
  https://commons.wikimedia.org/wiki/File:Negev_Brigade_soldiers_1948.jpg
- Beersheba, 21 October 1948: brimmed bush hats, khaki shirts and shorts, belts. CC BY 4.0 (National Library of Israel).
  https://commons.wikimedia.org/wiki/File:1948_Arab-Israeli_War_997008137085905171.jpg
- An IDF soldier, about March 1948: khaki shirt and trousers, web belt, a Lee-Enfield rifle. CC BY 2.5 (PikiWiki).
  https://commons.wikimedia.org/wiki/File:PikiWiki_Israel_38816_Israel_Defense_Forces.jpg

Aircraft:
- Israel bought 59 Spitfire Mk IXs from Czechoslovakia in 1948–49; IAF Spitfires fought in the 1948 war (the last air
  combat was on 7 January 1949). https://en.wikipedia.org/wiki/Supermarine_Spitfire_operational_history
- Ferry flights of the Czech Spitfires to Israel (Operation Velvetta, 1948). https://en.wikipedia.org/wiki/Operation_Velvetta
- The Avia S-199 flew first, on 29 May 1948 (Operation Pleshet). https://en.wikipedia.org/wiki/Avia_S-199
  The Spitfire was chosen over it because the promo shows a Spitfire (promo beats era, and both are 1948-correct).
- Spitfire Mk IX shape (an elliptical wing, a four-blade propeller): the IAF Museum airframe at Hatzerim. CC BY 2.5.
  https://commons.wikimedia.org/wiki/File:Spitfire-MkIX-hatzerim-1-1.jpg
- Early IAF roundel: a blue Star of David on a white disc ringed in blue. Public domain.
  https://commons.wikimedia.org/wiki/File:IAF_roundel_(early_variant).svg
- **Unverified (single source: the promo):** the two-tone camouflage. The Czech Mk IXs were ex-RAF airframes, but no
  source for the scheme they flew in Israel in 1948 has been checked yet. The museum airframe is bare metal in post-war
  markings.

Navy:
- INS Haganah (K-20), ex-HMCS Norsyd, a modified Flower-class corvette, taken over and rearmed by the Israeli Navy in 1948.
  The modified Flowers had an upright funnel. https://en.wikipedia.org/wiki/HMCS_Norsyd
- Photograph of K-20 in Haifa harbour, 1948: light grey, mast at the bridge, funnel aft of it. Public domain.
  https://commons.wikimedia.org/wiki/File:Hagana_K-20_ship_Israel.jpg
- Photograph of INS Haganah, about 1949. Public domain. https://commons.wikimedia.org/wiki/File:INSHaganah.jpg

The photographs are references only: no image file is copied into the repository.
