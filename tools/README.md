# tools/

Offline data tools (uv inline-script Python). Run from the repo root.

- `dem_to_heightmap.py` downloads Copernicus GLO-30 tiles into `tools/.cache/dem/` (not committed) and writes
  `unity/Assets/StreamingAssets/map/heightmap.r16` + `heightmap.json`.
- `hexgrid.py` writes `content/map/hexes.json` from that heightmap.
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.

## Running the tests

Run from the repo root; this installs every pinned dependency (rasterio included) and runs all tools tests:

```
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q
```

Expected: `44 passed`. Omitting a dependency (e.g. rasterio) silently drops tests or fails collection.

## Required attribution (Copernicus DEM licence, modified data)

produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

This notice must also appear in the game credits and the Steam page legal notes.
