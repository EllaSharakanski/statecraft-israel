# M2 geography check (captures/topdown.png vs promo/ref/geo-reference.png)

Redone on 2026-10-10 against the merged m0-m2 tree, which has the climatology biomes, the K2 overlay fix and the K1 labels. Images checked:
- `captures/topdown.png` (749×2000, `MapCapture.TopDown`, batch mode with graphics)
- `promo/ref/geo-reference.png` (a schematic, not to scale)
- `tools/preview_map.py`'s image
- flythrough frames 0, 60, 120, 180 and 239

Durable copies (captures/ is git-ignored and goes away with the worktree):
- `promo/final/09-m2-flythrough.mp4`: byte-identical copy of `captures/flythrough.mp4`.
- `promo/final/10-m2-topdown.png`: `captures/topdown.png` downscaled to 374×1000.

- [x] Coast on the west; Carmel ridge reaching the coast at Haifa — PASS. The sea fills the west edge. The Haifa marker sits on the headland, with Haifa Bay and Acre to its north. With the fine terrain LOD, the Carmel relief can be seen running south-east from Haifa (clearest in flythrough frame 239).
- [x] Sea of Galilee north-east; Jordan valley running south to the Dead Sea — PASS. The Galilee lake is at about 35.6E 32.8N. A low valley runs south from it to the Dead Sea along the east side of the land.
- [x] Dead Sea's north tip level with Jerusalem; Jerusalem south-east of Tel Aviv — PASS. In the top-down image the Dead Sea's north tip and Jerusalem's marker are both at row ≈ 400/1000. Jerusalem is east and south of Tel Aviv. The same layout shows in flythrough frame 60.
- [x] Negev widening to the south; Beersheba inland, south of the coastal plain — PASS (partly checkable). These parts can be checked:
  - The coast turns west and leaves the frame near Rafah (31.3N).
  - Beersheba sits inland, at the southern edge of the green coastal plain, where the desert begins (31.25–31.3N in the climatology biomes).

  This part cannot be checked: the Negev's own triangle. The frame includes Sinai and Jordan, and the map draws no borders, so the desert runs the full width of the frame. The widening can be judged only from the coastline, not from the Negev's outline.
- [x] Every 1948 settlement marker sits on its correct place (spot-check Tel Aviv, Jaffa, Haifa, Jerusalem, Beersheba, Tiberias) — PASS.
  - Edit-mode tests check all 25 markers against `GeoFrame.ToWorld` (within 1 m). They also check the frame against the heightmap meta, and Jerusalem's position computed independently.
  - By eye and in the labelled flythrough:
    - Tel Aviv and Jaffa are on the coast, Jaffa just south.
    - Haifa is at the Carmel tip.
    - Jerusalem is in the hills.
    - Beersheba is inland in the northern Negev.
    - Tiberias is on the Sea of Galilee's west shore.
- [x] No settlement in the sea; no hex outline over open sea — PASS.
  - Markers: "no marker on water" rests on a DEM check. The DEM elevation at all 25 coordinates is above sea level, or above the Galilee lake surface (−210 m) for Tiberias (−135 m) and Degania (−197 m).
  - In the image, the Tiberias marker appears inside the lake's blue tint. That is because biome colours blend over about a hex near the shore; it is not a placement error.
  - Outlines are drawn only for land hexes. Coastal hexes overhang the shoreline by part of a hex; none sits over open sea.

Unity's render and `tools/preview_map.py`'s preview agree on:
- the coastline, the lakes and the Jordan valley;
- the desert edge (both follow `hexes.json`);
- every settlement position.

They differ only in biome palette.

Result: PASS (geography).

Presentation, not blocking:
- The labels are sized for the 1080p flythrough (see the task 25 report for measured sizes).
- In the top-down image they are small and foreshortened, because they face the 50-degree flythrough camera.
