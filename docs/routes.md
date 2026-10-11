# Railways and roads of 1948 (issue #35)

The map draws the railways and main roads of May 1948 and a steam train, after the promo (`promo/final/03-gameplay-1936.png`,
`02-map-diorama.png`, and the 0:21 close-up of `promo/demo/out/demo-16x9.mp4`). Civilian boats are out of scope (owner).

## Files

| What | Where |
|---|---|
| Routes, sources, waypoints, status (hand-written) | `content/map/routes_1948.yaml` |
| Traced lines (generated) | `content/map/route_paths_1948.yaml` |
| Tracer | `tools/routes_1948.py` (tests: `tools/tests/test_routes_1948.py`) |
| Map layer | `unity/Assets/Scripts/Map/Routes/` (`RoutePaths`, `RouteLayer`, `RouteTrain`, `TrainModels`) |
| Shader and materials | `unity/Assets/Shaders/Route.shader`; `Resources/Routes/Rail.mat`, `Road.mat` (`RouteMaterialAssets.Create`) |
| Tests | `unity/Assets/Tests/EditMode/RouteTests.cs` |

Both route files are map-only. `ContentLoader` never reads them, so the content hash and the goldens do not move
(`RouteTests.TheRoutesDoNotMoveTheContentHash` loads the content with and without them). The WebGL manifest ships both,
as it ships every `map/*.yaml`. The game reads only the traced file: 69 KB, 32 KB gzipped.

## The data

**Source of every line:** the Survey of Palestine 1:250,000 map of 1946 (three sheets).
- It is public domain: a Mandate government survey published in 1946, whose Crown copyright (50 years) has expired.
  Wikimedia Commons files these sheets as public domain ("PD Israel Government", "UK Government artistic works"; for
  example `File:Gaza_sh._04,_1946.jpg`, checked 2026-10-11).
- The scans and georeferencing are Palestine Open Maps' (credited in `NOTICE`). Station coordinates fall on their map
  symbols to about 100 m.

**How a line is made.** `tools/routes_1948.py` traces each route from waypoint to waypoint.
- The waypoints are towns and stations that the cited sources name on the route. Their coordinates come from the
  Wikipedia API (`prop=coordinates`, as `settlements_1948.yaml`), or from Wikidata (CC0) where the article has none.
- Between two waypoints the line is the least-cost path over the scanned map. A pixel costs 1 on the route's own
  symbol: the red main road, or the black railway (thick, with white dashes). A thin dark line costs 4 for a railway
  (curves drawn thinner, as in the Sorek canyon). Any other pixel costs 40.
- The 1,050 mm narrow-gauge Jezreel Valley line is drawn thin, so it traces as a `track`.
- A waypoint first snaps to its symbol within 1.5 km. A waypoint off the line (a town circle, its name, a modern
  town's point such as Ashkelon's for al-Majdal) is reached by an out-and-back spur, which the tool cuts.
- The chain is smoothed over 5 pixels and simplified at 35 m.
- `--preview DIR` writes every traced segment over the map, which is how each was checked by eye.

**The routes.** Every route has `status: draft`: traced and checked by eye against the 1946 map, not yet reviewed by a
historian. `to_verify` lines say what the sources leave open.

| Kind | Route | Opened (source) | Notes |
|---|---|---|---|
| rail | Jaffa–Jerusalem railway | 1892 | The promo's line and the train's. Ramla–Jerusalem trains stopped in the 1948 war and resumed in March 1950. |
| rail | Main line, Lydda to Rafah | 1920 | Severed at the Gaza front in 1948. |
| rail | Eastern Railway, Lydda to Hadera | 1915 | Israel's August 1948 bypass west of Tulkarm is not drawn. |
| rail | Main line, Hadera to Haifa | 1918 | |
| rail | Haifa–Acre and the Haifa–Beirut–Tripoli line | 1913 | to-verify: the Haifa–Acre alignment is the 1946 sheet's. |
| rail | Jezreel Valley railway, Haifa to al-Hamma | 1905 | Used irregularly by Israel until the early 1950s. |
| road | Jaffa–Jerusalem by Latrun and Bab al-Wad, Tel Aviv–Haifa, the coastal road south to Rafah, Haifa–Ras an-Naqura, Jerusalem–Hebron–Beersheba, Jerusalem–Nablus–Jenin–Nazareth, Tel Aviv–Tulkarm–Nablus, Haifa–Nazareth–Tiberias, Jerusalem–Jericho, Tiberias–Metula, Gaza–Beersheba, Beersheba–Auja al-Hafir | Only where a source gives a year | All are red main roads on the 1946 map, so each ran by 1946. |
| track | Burma Road (1948) | 1948 | to-verify: built after the map, so it follows the 1946 tracks between the places its source names. |

**Left out, on purpose.**
- The Afula–Nablus–Tulkarm branch: its service ended in 1932 and 1938 (Palestine Railways article).
- The Ottoman Beersheba line: dismantled after the First World War.
- Branches with no 1948 source here: the Petah Tikva branch, and the port and quarry sidings.

**Accuracy, measured but nothing copied.**
- OpenHistoricalMap (CC0) has the Jaffa–Jerusalem railway with its dates.
- OpenStreetMap has today's lines and pieces of the abandoned ones. It is ODbL, which this repository's licence policy
  does not allow, so its geometry is used only to measure ours.

| Route | Reference | Median distance | Within 300 m |
|---|---|---|---|
| Jaffa–Jerusalem railway | OHM, 1890 ways | 70 m | 96% |
| Eastern Railway | OSM rail, current and abandoned | 108 m | 87% |
| Hadera–Haifa railway | OSM | 216 m | 85% |
| Jezreel Valley railway | OSM | 153 m | 82% |
| Haifa–Beirut railway | OSM | 238 m | 72% |
| Main line south | OSM | 140 m | 58% |
| Beersheba–Auja road | OHM roads valid in 1948 | 20 m | 100% |
| Gaza–Beersheba road | OHM | 205 m | 93% |

- The misses on the railways are the reference's, not the trace's. Today's lines were rebuilt off the old alignment:
  near Rosh HaAyin the 1946 line runs past Rantiya and Wilhelma, and south of Ashkelon it differs too. OSM has almost
  nothing in the Gaza Strip.
- The rest is the 1:250,000 sheet's generalisation of tight curves.
- OHM has 1948 roads only in patches, so most roads have no reference.

## How it is drawn

**Ribbons, not a texture.**
- The roads and railways are meshes draped on the terrain: one mesh per look, so two draw calls.
- The other way was to bake them into a texture the terrain shader samples, as promo-land does for its tracks
  (`tracks.png`). It was not taken, for three reasons:
  - A railway needs a coordinate along its line for the sleepers. A distance-field texture has only the distance across.
  - At promo-land's 316 x 421 m texel, a 560 m wide railway is under two texels.
  - The terrain shader and its bakes are being changed by promo-land and promo-sea at the same time, so a texture
    would mean shared-shader edits on three branches. The ribbons need no terrain change.
- The train needs the polyline anyway.

**Symbolic widths**, measured on promo 03 at gameplay03 (about 35 m a pixel at 1920 px), after the units' scale
measurement in `docs/units.md`:

| Feature | Promo | Here |
|---|---|---|
| Railway bed | about 16 px | 560 m |
| Sleeper spacing | about 4 px | 150 m |
| Dirt road | about 8 px | 300 m |
| Burma Road | (not in the promo) | 200 m |
| Train (six cars) | about 250 px, about 8.8 km | 7.1 km (locomotive, tender, four coaches; 65x real size) |

**The mesh** (`RouteLayer.Ribbon`).
- A cross-section every 120 m, square to the line's direction averaged over two steps each way.
- Five vertices across each section. Each sits 20 m above the highest ground at its own point and half a step or half a
  spacing around it, and takes the terrain's normal.
- u runs across the ribbon and v along it, in metres.

**The shader** (`Statecraft/Route`). It is transparent and has no new keywords; the shadow keywords are the terrain's.
- The road is pale dirt with faint wheel ruts. The railway is a grey-brown ballast bed with dark sleepers and two dark
  rails.
- Every pattern is box-filtered to the pixel, so it fades to its mean at the overview instead of shimmering.
- It is lit like the ground: the sun with its shadows and the ambient probe. Then come the hex grid and the land haze
  from `StatecraftMap.hlsl`.
- Edges are soft. A stencil bit lets each pixel take one ribbon, so joins never double-blend. The rail material draws
  first, so a railway stays on top of a road it crosses.

**The train** (`RouteTrain`, `TrainModels`).
- Code-built painted miniatures, made with the units' `MiniatureMesh` and palette. These are generic period shapes; the
  close zoom's authored models are the AI-3D track's.
- It shuttles on the Jaffa–Jerusalem line between Jaffa and Lydda junction at 300 m/s of map, with a 6 s stop at each
  end and the locomotive leading each way.
- Tel Aviv's trains used this stretch through 1948–56 (Tel Aviv South station, 1920–1970). The Lydda–Jerusalem section
  saw no regular trains between the 1948 war and March 1950.
- A screenshot (`?shot` / `-shot`) or an editor render holds it still, its middle by Mikve Israel and heading inland, as
  in the promo.

**Toggles.** `routes` (the ribbons) and `train` are visual toggles, so `?vprofile` prices each one.

## Measurements

(filled in below by the measurement runs)

## promo-land's tracks

See "Integration with promo-land" below.

## What still differs from the promo

(filled in below)
