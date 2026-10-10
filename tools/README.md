# tools/

Offline data tools (uv inline-script Python). Run from the repo root.

- `dem_to_heightmap.py` downloads Copernicus GLO-30 tiles into `tools/.cache/dem/` (not committed) and writes
  `unity/Assets/StreamingAssets/map/heightmap.r16` + `heightmap.json`.
- `precip_clip.py` reads the map window of CHELSA V2.1 bio12 (mean annual precipitation 1981-2010) in place over HTTP and
  writes `content/map/precip_bio12.u16` + `precip_bio12.json` (committed).
- `ims_normals.py` downloads Israel Meteorological Service rain-gauge records from data.gov.il into `tools/.cache/ims/`
  (not committed) and writes the 1981-2010 station normals `content/map/precip_stations.json` (committed).
- `hexgrid.py` writes `content/map/hexes.json` from the heightmap and those committed precipitation inputs (no network).
- `settlement_hexes.py` writes `content/map/settlement_hexes.yaml` (committed): the hex that owns each settlement, by the
  `hexgrid.settlement_hex` rule. The sim reads it, so it needs no projection maths.
- `web_check.py` serves a WebGL build on http://localhost and opens it in headless Chrome for Testing (Playwright's
  cache; macOS only, arm64 or x64); it passes when the console shows the expected lines (`GOLDEN OK ...`, `SMOKE OK ...`).
- `package_release.py` zips a Brotli WebGL build for sharing (`unity/Builds/statecraft-m3-web.zip`) and refuses
  (exit 1) until every sensitive event is `sensitivity-reviewed` (docs/m3-release.md).
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
- `terrain_bake.py` writes `unity/Assets/Textures/Map/terrain_bake.png` (AO, curvature, sky-view), `ao.png` (the AO
  alone, imported uncompressed) and `coast.png`
  (exact signed Euclidean distance to the shoreline; lakes under 1 km2 count as land) from the committed heightmap (no
  network). Re-run it whenever the heightmap changes; its PNG bytes are reproducible only on the same toolchain.
- `terrain_graph.py` writes `unity/Assets/Shaders/StatecraftTerrain.shadergraph` from Unity's "Terrain Simple" template
  with one Custom Function node (`StatecraftTerrain.hlsl`); re-run it only when that function's inputs or outputs change.
- `art_compare.py` puts screenshots next to the promo image of their pose, writes deuteranopia and protanopia copies and
  logs rough colour numbers; `art_checkpoint.sh [--no-build] <name>` builds WebGL, takes the screenshots
  (`web_check.py --shots`) and runs it (docs/visual-v1.md).
- `machine_lock.py` serialises frame-time measurements against every other Unity job on the machine (all worktrees,
  batch runs and open editors).
- `headed.py` drives a WebGL build in a headed Chrome for Testing over CDP (real GPU, real mouse events, a temporary
  profile deleted on exit): `boot` (?bootlog phase timings, optional CPU throttling), `navpairs` (same-frame click pairs
  under ?navlog), `loadshot` (screenshots during the load) and `profile` (visual v1's `?vprofile` in a headed window at a
  chosen DPR); `boot --runs N --baseline MS` checks the boot budget. Its browser test runs only with `pytest --headed`.

## Running the tests

Run from the repo root; this installs every pinned dependency (rasterio included) and runs all tools tests:

```
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
```

Expected: `177 passed, 1 skipped` (the skipped one is the headed browser check; add `--headed` and `--with websockets==15.0.1` to run it). Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

## Required attribution (Copernicus DEM licence, modified data)

produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

This notice must also appear in the game credits and the Steam page legal notes.

## Required attribution (precipitation inputs)

Precipitation: CHELSA V2.1 bio12 (1981-2010), CC0 1.0. Karger, D.N., Conrad, O., Böhner, J., Kawohl, T., Kreft, H.,
Soria-Auza, R.W., Zimmermann, N.E., Linder, H.P. & Kessler, M. (2021) Climatologies at high resolution for the earth's land
surface areas. EnviDat. https://doi.org/10.16904/envidat.228.v2.1 ; Karger et al. (2017) Scientific Data 4, 170122.

Rain-gauge data: Israel Meteorological Service (השירות המטאורולוגי), meteorological data archive, data.gov.il dataset 481
(https://data.gov.il/dataset/481), licence "Other (Open)" as stated on the dataset.

CHELSA is CC0 (no attribution required); both are credited here and should be credited in the game credits alongside the
Copernicus notice.
