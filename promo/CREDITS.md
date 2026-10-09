# Promo concept art: sources

Images 01-08 are AI-generated concept mockups (ElevenLabs Image API: GPT Image 2.5, Nano Banana Pro),
not final game art. Steam requires disclosure of AI-generated marketing assets.
09-m2-flythrough.mp4 and 10-m2-topdown.png are not AI-generated: they are in-engine Unity captures of the M2 map
(`MapCapture.Flythrough` and `MapCapture.TopDown`; see docs/m2-geography-check.md).

## Likeness references (Wikimedia Commons; licence checked by API on 2026-10-09)
Public domain: David Ben-Gurion (D597-087), Eliezer Kaplan, Moshe Sharett (Portrait), Haim-Moshe Shapira
(Moshe_Shapira.jpg), Yitzhak Gruenbaum (1948), Pinchas Rosen, Aharon Zisling, Mordechai Bentov,
Bechor-Shalom Sheetrit (Shitreet), Yehuda Leib Maimon, Peretz Bernstein, David Remez (cropped),
Yitzhak Meir Levin.
CC0: "Moshe Dayan met mgr. A.C. Ramselaar (cropped).jpg" (1962).

## Facts shown (verified 2026-10-09)
- Provisional Government (May 1948): 13 ministers and portfolios; Provisional State Council 37 members,
  coalition 30 / opposition 7, Mapai 10, president Chaim Weizmann (Wikipedia: Provisional government of
  Israel; Provisional State Council).
- Defense, Oct 1955: PM Moshe Sharett (7 Dec 1953 – 3 Nov 1955); Ben-Gurion Minister of Defense after
  Lavon's resignation was accepted 18 Feb 1955; Dayan Chief of Staff from 7 Dec 1953; Egypt–Czechoslovakia
  arms deal announced 27 Sep 1955.
- Geography: promo/ref/geo-reference.png, drawn from gazetteer coordinates by promo/refmap.py.

Readiness/strength bars, competence/loyalty scores and budget bars are illustrative game values.

## Map data (09-m2-flythrough.mp4, 10-m2-topdown.png)
Wording copied verbatim from tools/README.md.

Terrain (Copernicus DEM licence, modified data):
produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018
provided under COPERNICUS by the European Union and ESA; all rights reserved

Biomes (precipitation inputs):
Precipitation: CHELSA V2.1 bio12 (1981-2010), CC0 1.0. Karger, D.N., Conrad, O., Böhner, J., Kawohl, T., Kreft, H.,
Soria-Auza, R.W., Zimmermann, N.E., Linder, H.P. & Kessler, M. (2021) Climatologies at high resolution for the earth's land
surface areas. EnviDat. https://doi.org/10.16904/envidat.228.v2.1 ; Karger et al. (2017) Scientific Data 4, 170122.

Rain-gauge data: Israel Meteorological Service (השירות המטאורולוגי), meteorological data archive, data.gov.il dataset 481
(https://data.gov.il/dataset/481), licence "Other (Open)" as stated on the dataset.

1948 settlements (tools/README.md has no settlement wording; this is the source note of
content/map/settlements_1948.yaml): coordinates from the Wikipedia API (prop=coordinates); founding years from the
cited article's text. Each settlement's source URLs and check dates are listed in that file.
