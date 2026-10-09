# tools/

Offline data tools (uv inline-script Python). Run from the repo root.

- `dem_to_heightmap.py` downloads Copernicus GLO-30 tiles into `tools/.cache/dem/` (not committed) and writes
  `unity/Assets/StreamingAssets/map/heightmap.r16` + `heightmap.json`.
- `precip_clip.py` reads the map window of CHELSA V2.1 bio12 (mean annual precipitation 1981-2010) in place over HTTP and
  writes `content/map/precip_bio12.u16` + `precip_bio12.json` (committed).
- `ims_normals.py` downloads Israel Meteorological Service rain-gauge records from data.gov.il into `tools/.cache/ims/`
  (not committed) and writes the 1981-2010 station normals `content/map/precip_stations.json` (committed).
- `hexgrid.py` writes `content/map/hexes.json` from the heightmap and those committed precipitation inputs (no network).
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.

## Running the tests

Run from the repo root; this installs every pinned dependency (rasterio included) and runs all tools tests:

```
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
```

Expected: `88 passed`. Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

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
