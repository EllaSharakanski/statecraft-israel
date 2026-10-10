# Licensing

Different parts of this repository are under different licences. Where a file is not listed below, the rule for its folder applies.

| What | Paths | Licence |
|---|---|---|
| Code | `sim/`, `tools/` (Python), `unity/Assets/Scripts/`, `unity/Assets/Editor/`, `unity/Assets/Tests/`, `promo/*.py`, `spikes/` code | [MIT](LICENSE) |
| Game content (text and data) | `content/` (events, people, offices, model, rules, map YAML/JSON), except the third-party map data below | [CC BY-SA 4.0](LICENSES/CC-BY-SA-4.0.txt), credit "Statecraft: Israel contributors". The text paraphrases its cited sources, many from Wikipedia (CC BY-SA), so share-alike applies; each entry lists its sources |
| Original docs, art and UI assets | `docs/`, `reports/` (our own text), `research_notes/` (our own text), `unity/Assets/UI/*.uxml`, `*.uss`, `strings.en.yaml`, `unity/Assets/Materials/`, `unity/Assets/Shaders/`, `unity/Assets/UI/*.asset`, `promo/ref/geo-reference.png` | [CC BY 4.0](LICENSES/CC-BY-4.0.txt), credit "Statecraft: Israel contributors" |
| Unity template-derived settings | `unity/Assets/Settings/` (URP assets, renderers, volume profiles) | Created from Unity's URP project template and edited; under the [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) |
| AI-generated images | `promo/final/01`–`08`, `promo/ref/*-src.jpg`, `docs/images/hero-concept-1936.jpg` | Marked [CC0 1.0](LICENSES/CC0-1.0.txt). No rights are claimed. Purely AI-generated output may not be copyrightable. Made with the ElevenLabs Image API (see `promo/CREDITS.md`). |
| In-engine captures | `promo/final/09`, `10`, `docs/images/build-*.jpg` | CC BY 4.0. They show the Copernicus-derived terrain; see below. |
| Portraits | `unity/Assets/UI/Portraits/`, `promo/ref/people/`, `promo/ref/*-sheet.jpg` | Public-domain photographs from Wikimedia Commons, plus one CC0 file (Moshe Dayan, 1962). Per-file sources are in `content/people.yaml` and `promo/CREDITS.md`. |
| Fonts | `unity/Assets/UI/Fonts/` | EB Garamond, [SIL Open Font License 1.1](unity/Assets/UI/Fonts/OFL.txt) |
| Terrain heightmap | `unity/Assets/StreamingAssets/map/heightmap.*`, `content/map/hexes.json` (elevations) | Derived from Copernicus GLO-30 under the Copernicus DEM licence. Required notice: "produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved" |
| Precipitation grid | `content/map/precip_bio12.*` | CHELSA V2.1, CC0 1.0 (Karger et al. 2017, 2021; see `tools/README.md`) |
| Rain-gauge data | `content/map/precip_stations.json` | Israel Meteorological Service, data.gov.il dataset 481, under the licence stated on the dataset ("Other (Open)") |
| Unity engine and packages | `unity/Packages/`, `unity/ProjectSettings/` | Configuration only. The Unity engine and its packages are under Unity's own terms and are not included. No Asset Store content is in this repository. |

Historical facts are not owned by anyone. The text describing them paraphrases the cited sources. The research notes and reports quote short passages from their sources for commentary; those quotes remain © their authors.

Still being checked by the owner: the ElevenLabs terms for the AI images, the data.gov.il terms for the rain-gauge data, and the US public-domain status of each portrait. If any check fails, the file is replaced or removed.

"Civilization" is a trademark of Take-Two Interactive. It is mentioned only to describe the genre. This project is not affiliated with or endorsed by its owners.
