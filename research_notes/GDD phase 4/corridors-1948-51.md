# Road and rail corridors, 1948-51 (research note, 2026-10-13)

Status: research only, for issue #384 (GDD Phase 4, D-D7 and D-B3). Nothing here is in `content/` yet. The corridor
list and the railway objective are the owner's calls; this note gives the sourced facts and the gaps.

What the GDD needs (`docs/GDD.md`, lines 11, 17, 121-122, 164): a list of designated corridors, each with route
geometry and construction states, and a Tel Aviv-Haifa railway the player completes, compared against its historical
date.

## How to read this note

- **Sources.** Most claims come from English and Hebrew Wikipedia, read through the Wikipedia API on 2026-10-13, at the
  revision given in the source list. Where an article cites a contemporary newspaper or book for a claim, the citation
  is given as "(cited: ...)". Those newspapers were **not** read for this note; they are where a fact-checker should go
  next (most are on the National Library's Historical Jewish Press site).
- **[C]** marks a conflict between sources. **[U]** marks a claim that rests on one secondary source, or one that
  could not be checked. Nothing is invented: where no source gives a date or a route, the note says so.
- **Coordinates** are approximate: the point the place's Wikipedia article gives (API `prop=coordinates`, read
  2026-10-13), rounded to 4 decimals, as `routes_1948.yaml` does. A junction or station point is not a surveyed route
  point. "(no point)" means the article has none.
- Dates are as the source gives them. A month alone means the source gives no day.
- Article abbreviations used below are listed in [Sources](#sources).

## Summary

1. **The GDD's Tel Aviv-Haifa railway was finished after 1951.** The new coastal line (Remez Junction near Pardes
   Hanna to Tel Aviv North, 47 km) opened unofficially in April 1953 and officially on 14 May 1953 [C on the April
   date]. Any 48-turn game that starts in 1948 ends before that. In-window milestones that are sourced:
   - 4 January 1949: a joint rail-bus service Haifa-Hadera by train, Hadera-Tel Aviv by bus.
   - about August 1949: the bypass of Tulkarm on the Eastern Railway finished [C: one book says August 1948].
   - 20 September 1949: the first Haifa-Tel Aviv passenger line, over the Eastern Railway and a new 7 km link to a
     new Tel Aviv North station.
   - end of 1951: track laid on the first stretch of the coastal line from Remez Junction [U].
2. **The Jerusalem railway reopened on 7 August 1949** (a ceremonial first train); regular passenger trains started on
   2 March 1950. `routes_1948.yaml` gives only the March 1950 date.
3. **Routes the game would plausibly offer that `routes_1948.yaml` does not have:** the Road of Valour (built 4
   September to 30 October 1948, opened 7 December 1948), its Nahshon-Ramla link (opened September 1950), the Tulkarm
   rail bypass, the Rosh HaAyin-Tel Aviv North rail link, the coastal railway, the Tel Aviv-Netanya road (opened July
   1950), the Wadi Ara road (reopened May 1949), the Acre-Safed road (reopened early 1949), the Gesher-Menahamia bypass
   (opened November 1950), the Malachi-Kama road to Beersheba (July 1949 to June or July 1951), the Beersheba-Kurnub-Ma'ale
   Akrabim-Arava track to Eilat (taken November 1948 and March 1949, improved by the Engineering Corps in 1950), and
   the Kurnub-Sodom road (January 1951 to 26 March 1953).
4. **Existing entries need dates for when a line was cut, not only when it opened.** The Jaffa-Jerusalem road through
   Latrun was closed to Israeli traffic from 18 May 1948 and never reopened before 1967; the Eastern Railway as drawn
   runs through Tulkarm, which stayed on the Jordanian side; the Haifa-Nahariya line was cut at the Na'aman bridge
   until 1950 [C].

## Railways

### R1. Haifa-Tel Aviv by the Eastern Railway (Haifa, Hadera, Rosh HaAyin, Tel Aviv North), 1948-53

The Mandate main line ran Haifa - Hadera - Qaqun - Tulkarm - Qalqilya - Rosh HaAyin - Lydda (EN Eastern). Tel Aviv
had no rail link to the north in 1948; the nearest line was the Yarkon branch from the Eastern Railway near Rosh HaAyin
to Petah Tikva (HE TA North).

| Date | Status | Source |
|---|---|---|
| 29 April 1948 | First freight trains Haifa-Hadera under the agreement of 23 April 1948 with the British army | HE IR |
| rest of 1948 | Service confined to the Haifa area: south to Hadera, north to Kiryat Motzkin and later Nahariya | EN PR, citing Cotterell 1984, p. 84 |
| 4 January 1949 | Regular rail-bus service Haifa-Tel Aviv: train Haifa-Hadera, bus Hadera-Tel Aviv; two trains a day each way; Transport Minister David Remez at the opening | HE IR (cited: Al HaMishmar, 16 January 1949) |
| 3 April 1949 | Armistice with Jordan; the Triangle passes to Israel and almost all of the Eastern Railway with it, running tens to hundreds of metres from the new line near Qalqilya and Tulkarm | HE IR (cited: Davar, 21 September 1949) |
| August 1948 or 1949 | Bypass of Tulkarm, "a short stretch of new track just west of what was to become the Armistice Line" | **[C]** EN PR says August 1948 (citing Cotterell 1984, p. 86); EN Eastern says 1949; HE Coastal cites Haaretz, 4 August 1949: "Train traffic to Haifa in September - the section bypassing Tulkarm completed". The 1949 date fits the armistice; Cotterell's page should be checked. |
| 1949 | Yarkon branch extended about 7 km west along the Yarkon to a new Tel Aviv North station (later Bnei Brak station) | HE TA North |
| 20 September 1949 | Haifa-Tel Aviv passenger line inaugurated with Tel Aviv North station. Trains ran to Rosh HaAyin South and reversed there towards Hadera and Haifa | HE TA North (cited: Ma'ariv, 20 September 1949); HE Yarkon |
| 14 May 1953 | Replaced as the main line by the coastal railway (R2); the Eastern Railway kept some passenger and freight service | HE Coastal; EN Eastern |

Waypoints (approximate): Haifa East station (32.8119, 35.0047, from `routes_1948.yaml`); Hadera East station (32.4461,
34.95); Tulkarm (32.3115, 35.027); Rosh HaAyin South station (32.1042, 34.9347); Tel Aviv North / Bnei Brak station
(32.1029, 34.831). The bypass's own alignment west of Tulkarm is not given by any source read [U].

### R2. The coastal railway, Remez Junction - Netanya - Herzliya - Tel Aviv North (the GDD's railway)

| Date | Status | Source |
|---|---|---|
| soon after May 1948 | Decided, as a replacement for the longer Eastern Railway | HE Coastal (cited: Ma'ariv, 3 October 1948) |
| about January 1949 | Work in the field begins, with the hope of finishing by summer 1949 | HE Coastal (cited: Al HaMishmar, 6 October 1948; Davar and Al HaMishmar, 19 January 1949); HE IR says "early 1949" |
| after April 1949 | Most effort moved to reopening the Eastern Railway; later delays from late rails and sleepers and from flooding | HE Coastal (cited: Haaretz, 4 August 1949; Haaretz, 9 March 1951; HaBoker, 16 February 1950) |
| end of 1951 | Track laid "from Remez Junction to Hadera" | HE Coastal (cited: Davar, 29 November 1951) **[U]**: the cited headline is about the Tel Aviv-Herzliya line, so what was laid by the end of 1951 needs the article itself |
| early 1953 | Tracklaying complete; more work needed before trains could run | HE Coastal (cited: Davar, 25 January 1953) |
| April 1953 | Opened unofficially | **[C]** HE Coastal: April 1953 (cited: Al HaMishmar, 20 April 1953; Herut, 26 April 1953). HE IR: a ceremony marked completion on 15 April 1953 (cited: Israel Hayom, 20 September 2019) |
| 14 May 1953 | Official opening, ceremony at Kfar Vitkin; 47 km from Remez Junction to Tel Aviv North | HE Coastal (cited: Haaretz, 14 May 1953); HE Remez; HE IR (47 km). EN Eastern gives only "1953" |
| 4 November 1954 | Extended about 4 km to Tel Aviv Central station | HE IR; HE Coastal gives "1954" |

Builder: Solel Boneh (HE Coastal, cited: HaTzofe, 21 November 1949). Built with American aid (HE IR) [U: no citation
given there].

Waypoints (approximate): Remez Junction (32.4656, 34.9442); Kfar Vitkin (32.3817, 34.8769); Netanya (32.3286,
34.8567); Herzliya (32.1653, 34.8458); Tel Aviv North / Bnei Brak station (32.1029, 34.831). The places are those the
cited headlines and the ceremony name; the line between them is not traced from any source [U].

**For D-B3.** The historical completion date (14 May 1953, or April 1953) is after the MVP window whatever the starting
month. Options, all sourced above: (a) 20 September 1949, the first Haifa-Tel Aviv passenger line (R1), which is a
reopening of an existing line rather than a new build; (b) the coastal line as the build, with its historical date
shown as after the game's end; (c) 4 January 1949, the rail-bus link. Which one the card uses is the owner's call.

### R3. Jaffa (Tel Aviv South) - Lod - Jerusalem railway

Route: the existing `jaffa_jerusalem_railway` line, Jaffa - Lod - Ramla - Artuf (Beit Shemesh) - Battir - Jerusalem.

| Date | Status | Source |
|---|---|---|
| 1948 | Service stopped during the war; sections of the line (Battir) ended up under the Arab Legion | EN JJR; HE JLM rail |
| 1948 | Jaffa station abandoned; the coastal terminus is Tel Aviv South (by Beit Hadar) from 1949 | HE JLM rail |
| 3 April 1949 | Armistice with Jordan returns the whole route to Israel | EN JJR; HE JLM rail |
| 7 August 1949 | Reopened: first Israeli train to Jerusalem, with a symbolic load of flour, cement and Torah scrolls; it left Tel Aviv South at 08:00 with Ben-Gurion and Remez on board | EN JJR; HE JLM rail; HE IR |
| 2 March 1950 | Regular passenger service, Haifa (and Tel Aviv North) - Rosh HaAyin - Lod - Jerusalem, with a connection at Lod from Tel Aviv South | EN JJR; HE IR (cited: Al HaMishmar, 1 March 1950; Israel Railways, "50 Years of Israel Railways", September 1998); EN PR, citing Cotterell 1984, p. 88, "March 1950" |

[C, minor] HE IR's external links list a Carmel newsreel titled "Israel Railways travels to Jerusalem for the first
time", dated February 1949. That date does not fit the armistice; it may be a mislabelled release date. Not used.

Waypoints: as `routes_1948.yaml` lines 50-55.

### R4. Haifa - Acre - Nahariya

| Date | Status | Source |
|---|---|---|
| late February 1948 | Rosh HaNikra railway bridges blown, cutting the line to Lebanon | HE Coastal; `routes_1948.yaml` line 142 |
| June 1948 | Haifa-Krayot suburban passenger service reopened | HE IR |
| 1948 (war in the Western Galilee) | Railway bridge over the Na'aman stream south of Acre blown; no trains north of Acre | HE Coastal |
| 4 January 1949 | Five passenger trains a day Haifa-Acre begin | HE IR |
| 1950 | Na'aman bridge rebuilt; Haifa-Acre-Nahariya line reopened | HE Coastal (cited: Ma'ariv, 24 May 1950, "The Haifa-Nahariya line will be renewed") |

**[C]** HE Coastal says trains stopped from Acre north until 1950, and the bridge is south of Acre; HE IR has trains
to Acre from 4 January 1949; EN PR (Cotterell p. 84) has trains in 1948 to Kiryat Motzkin "and later Nahariya". Which
stretch was open, and when, needs Cotterell and the 1950 Ma'ariv piece.

Waypoints: Haifa East (32.8119, 35.0047); Na'aman stream (32.8771, 35.1066, the stream's article point, not the
bridge); Acre station (32.9282, 35.0828); Nahariya station (33.0053, 35.0989).

### R5. Jezreel Valley railway (Haifa - Afula)

Already in `routes_1948.yaml` (lines 160-183). HE IR adds that it ran again for special trips to Afula at the end of
June 1948 and was shut "at the start of summer 1951" for lack of traffic. The existing note says service ended in 1949
with occasional use until September 1951 (citing EN Jezreel Valley railway). [C, minor] on the end date. Not a corridor
the player would build.

### R6. Lod - Majdal (the Kantara main line, south)

Already in `routes_1948.yaml` (lines 61-89). EN Lod-Ashkelon: severed at the Gaza Strip border in 1948; Dayr Sunayd
the southernmost station Israel could reach; passenger service stopped, freight ran Lod-Majdal. No date for the freight
restart was found [U].

### Not in the window: the railway to Beersheba

The rebuilt standard-gauge line to Beersheba was completed in 1956; passenger service started on 29 September 1956
(EN Beersheba railway). Nothing was running or being built on it in 1948-51 in the sources read.

## Roads

### D1. Tel Aviv - Jerusalem by Latrun and Bab al-Wad (the old main road)

| Date | Status | Source |
|---|---|---|
| from the outbreak of fighting (winter 1947-48) | Jewish traffic stops passing through Arab Ramla; Jerusalem traffic goes by Rehovot and Hulda to Latrun, then Sha'ar HaGai | HE R424; HE R1 |
| after February 1948 | British traffic moved to the line of today's Route 443; the Yishuv guards its own convoys | HE R1 |
| 15 May 1948 | British leave the Latrun monastery and police fort; the Harel Brigade occupies Latrun | EN Burma |
| night of 18 May 1948 | Arab Legion takes Latrun and closes the main road | EN Burma; HE R424; HE Gvura |
| 7 October 1948 | An agreement for Israeli buses to use the road with Israeli and Jordanian armoured cars and UN jeeps is reported signed | HE R1 **[U]**: no citation, and no source says it was carried out |
| 3 April 1949 | Article 8 of the armistice with Jordan provides for opening the Latrun road to Israeli traffic | HE R424 |
| 1949-67 | Never opened; the Road of Valour made it matter less | HE R424 |

Waypoints: as `routes_1948.yaml` lines 193-199. The Rehovot-Hulda detour (HE R1, HE R424) has no route data in the
repo: Rehovot, the Masmiya junction, Hulda (31.8322, 34.8833). Masmiya has no article point.

### D2. Burma Road (Hulda area - Bayt Susin - Bayt Mahsir - Saris)

| Date | Status | Source |
|---|---|---|
| late May - early June 1948 | Track found and scouted; porters and mules carry loads over the 4.8 km vehicles cannot pass | EN Burma |
| night of 30-31 May 1948 | First jeep attempt fails | EN Burma |
| night of 31 May - 1 June 1948 | Second attempt gets through | EN Burma |
| 9 June 1948 | An attack kills eight Israelis on the road | EN Burma |
| 10 June 1948 | Steepest section open to vehicles, with tractor help | EN Burma |
| 14 June 1948 | Completed; water and fuel pipes laid along it | EN Burma (cited: Isseroff, "Burma Road", zionism-israel.com, 2008; Rosenbloom, "The Road to Jerusalem", 2001) |
| until the Road of Valour | Jerusalem's sole supply route; civilians not allowed free passage | EN Burma; HE Gvura |

Endpoints [C, minor]: EN Burma's lead says "between Kibbutz Hulda and Jerusalem"; its construction section starts it
"just east of Dayr Muhaysin (today Moshav Beko'a)"; HE R1 starts it near Kibbutz Harel. These are all points on one
route described from different ends, not a real disagreement about the line. The existing entry follows the
construction section.

### D3. Road of Valour (Kvish HaGvura): Nahshon Junction - Shimshon Junction - Sha'ar HaGai

The permanent Latrun bypass, south of and parallel to the Burma Road.

| Date | Status | Source |
|---|---|---|
| 12 August 1948 | First attempt at a civilian convoy through Latrun fails; a permanent road on a southern line is announced | HE Gvura |
| 4 September - 30 October 1948 | Built by the Engineering Corps in about eight weeks; 40.5 km, about 5 km a week, mostly 5 m wide | HE Gvura (cited: Ma'ariv, 7 December 1948, p. 10; HaTzofe, 7 December 1948) |
| 7 December 1948 | Official opening and cornerstone of the memorial, with Ben-Gurion and Chief of Staff Ya'akov Dori. The name "Road of Valour" was given here | HE Gvura (cited: Yehuda Ziv, "Derekh Burma", yad-leyuval.org.il) |
| winter 1948-49 | Much of it damaged by rain (no drainage) | HE Gvura |
| summer 1949 | Re-laid with drainage ditches | HE Gvura (cited: HaTzofe, 20 October 1949) |

**[C]** On the start: HE Gvura gives 4 September 1948. HE R1 says paving began after Israel widened its hold south of
Latrun in Operation HaHar, which ran from 19 to 22 October 1948 (HE HaHar). If HaHar came first, work could not have
started on 4 September. The two are not reconciled in the sources read.

**Route.** HE Gvura: the central part, Nahshon Junction (by Hulda, "Gvura Junction") to Shimshon Junction, is today's
Route 44; from Shimshon Junction it ran up today's Route 38 to Sha'ar HaGai and rejoined the old road (HE R1, EN R38,
EN R44, HE R44). At the opening the name covered more: Mazkeret Batya to al-Qastal by Routes 411, part of 3, 44 and
395 (HE Gvura). Route 395 was called the "Malaya road" in Ma'ariv and linked al-Qastal to the Road of Valour (HE Gvura)
[U on 395's 1948 line].

Waypoints (approximate): Mazkeret Batya (31.8497, 34.8431); Hulda (31.8322, 34.8833); Mishmar David (31.8228, 34.9022,
the memorial is near it); Nahshon Junction (no point); Shimshon Junction (no point; it lies by Beit Shemesh, 31.7456,
34.9867); Sha'ar HaGai (31.8167, 35.0333); al-Qastal (31.7957, 35.1442).

### D4. Ramla - Nahshon Junction link (Route 44, central part)

| Date | Status | Source |
|---|---|---|
| mid-1949 or October 1949 | Construction begins | **[C]** HE Gvura: "mid-1949" (cited: Al HaMishmar, 20 October 1949). HE R44: "from October 1949" |
| September 1950 | Opened to traffic; 12.5 km, 6.5 m wide, 5 m of it asphalt. Saves the detour through Hulda and Rehovot | HE Gvura (cited: Davar, 30 August 1950, "The Ramla-Gvura road will open on Friday"); HE R44 |

Waypoints: Ramla (31.9275, 34.8625, `routes_1948.yaml` line 195); Nahshon Junction (no point).

### D5. Tel Aviv - Haifa road (old inland road by Ra'anana, Kfar Yona and Hadera)

Already in `routes_1948.yaml` (lines 227-251). No source read for this note gives a 1948-51 cut or closure. Related
builds:
- **Coastal road, Tel Aviv - Netanya (Route 2).** Announced August 1949; work from November 1949 at both ends; about
  half paved by February 1950; opened to traffic July 1950 (HE R2, cited: Davar, 17 February 1950; Ma'ariv, 27 March
  1950; Al HaMishmar, 30 July 1950). A Carmel newsreel of August 1950 shows Golda Meir cutting the ribbon (HE R2). EN
  R2 says only "early 1950s". The Netanya - Hadera section: tender May 1951, opened January 1959 (HE R2): out of the
  window.
  Waypoints: Tel Aviv (32.08, 34.78); Herzliya (32.1653, 34.8458); Netanya (32.3286, 34.8567).
- **Geha road (Route 4, Ra'anana/Petah Tikva - Azor).** Begun 1951, opened December 1953 by Golda Meir (HE R4; EN R4
  says completed 1968 for the full road). Begun in the window, finished after it.

### D6. Wadi Ara road (Hadera - Iron Pass - Megiddo Junction - Afula)

| Date | Status | Source |
|---|---|---|
| 1947 | Partition plan puts the road in the Arab state | HE R65 |
| end of the war | Wadi Ara section held by the Iraqi army | HE R65 |
| 3 April 1949 | Armistice with Jordan; the road and the Triangle pass to Israel | HE R65; EN R65 |
| May 1949 | Reopened to traffic with a ceremony | HE R65 (cited: Al HaMishmar, 26 May 1949) |

The Iraqi sector was handed over in stages; `research_notes/Map borders/armistice-lines-1949.md` gives the stage
deadlines (8 May, 22 May, 17 July 1949). Waypoints: Hadera (32.45, 34.9167); Wadi Ara (32.4941, 35.0545); Megiddo
Junction (32.5731, 35.1919); Afula (32.6064, 35.2881). **Not in `routes_1948.yaml` at all**, though it is on the 1946
map's road network [U: not checked on the sheet for this note].

### D7. Acre - Safed road (Route 85)

The central section was taken in Operation Hiram in autumn 1948; reopened to Jewish civilian traffic early in 1949;
renovated in 1950 (HE R85, cited: Herut, 3 January 1949, "Safed-Acre road open to civilian traffic"; Ma'ariv, 27 June
1950). Waypoints: Acre (32.9278, 35.0817); Majd al-Krum (32.9206, 35.2528); Safed (32.9658, 35.4983). Not in
`routes_1948.yaml`.

### D8. Tiberias - Beit She'an (Route 90) and the Gesher - Menahamia bypass

A few kilometres of the Tiberias - Beit She'an road near Naharayim were held by Jordan after the war, blocking it (HE
R90). The bypass, Route 7188, Gesher to Menahamia: construction begun by 25 June 1950; opened to traffic in November
1950 (HE R90, cited: Davar, 25 June 1950; Herut, 6 November 1950; Davar, 13 November 1950). It was hard going and
loaded lorries could not use it (HE R90). Waypoints: Gesher (32.6214, 35.5522); Naharayim (32.6333, 35.5683);
Menahamia (32.6678, 35.5539). Neither road is in `routes_1948.yaml`; the Tiberias - Beit She'an road itself is not
there either.

### D9. Roads to the Negev and Beersheba

- **Into the Negev, 1948.** The Egyptian line Majdal - Fallujah - Bayt Jibrin cut the Negev settlements off; supply was
  by air (Operation Avak) and infiltration (HE Yoav). Operation Yoav, 15-22 October 1948, broke the road open and took
  Beersheba (HE Yoav). The sources read do not give the road the convoys then used as a named route [U].
- **Malachi Junction - Kama Junction (with Route 264).** Built July 1949 to June 1951 by Solel Boneh with new
  immigrant labour, to ease unemployment around Lod and Ramla. It joined Malachi Junction to the old Beersheba road
  (Route 25) near Mishmar HaNegev and shortened Tel Aviv - Beersheba by about 25 km (HE R40, cited: Ma'ariv, 25
  November 1951; Davar, 27 July 1951, "Fallujah-Beersheba road opened"). [C, minor] June 1951 in the text, a July 1951
  opening in the cited headline. Waypoints: Malachi Junction (31.7319, 34.7568); Kama Junction (no point); Mishmar
  HaNegev (31.3642, 34.7186).
- **Beersheba - Auja (Nitzana).** Already in `routes_1948.yaml` (lines 400-413). Cleared in Operation Horev (HE Uvda,
  background section). No 1949-51 works found.

### D10. Beersheba - Kurnub - Ma'ale Akrabim - Arava - Eilat (the first road to Eilat)

| Date | Status | Source |
|---|---|---|
| 1927 | Mandate vehicle road cut on a spur parallel to the Roman ascent, between the Camel Corps police posts of Kurnub (Mamshit) and Ein Husub (Ein Hatzeva) | HE Akrabim; HE R227 |
| 23-27 November 1948 | Operation Lot: the Negev Brigade takes Kurnub police post (23 November) and Ein Husub (the next morning) without a fight; a convoy runs Beersheba - Kurnub - Ma'ale Akrabim - Ein Husub; the land link to Sodom, cut for seven months, is restored | HE Lot; EN Lot |
| 6 March 1949 | Golani uses the road on its way to Operation Uvda; Alexandroni the next day, towards Ein Gedi | HE Akrabim |
| 5 or 6 - 10 March 1949 | Operation Uvda [C on the start: EN Uvda 5 March, HE Uvda 6 March]. Golani goes on down the Arava: Ein Husub - Ein Gharandal - Ein Ghadyan (Yotvata), reaching Umm Rashrash (Eilat) on 10 March, two hours after the Negev Brigade | EN Uvda |
| 1949-50 or 1950 | Operation Arava: the Engineering Corps turns the tracks into roads and paves the ascent | **[C]** HE Akrabim and EN R227: paved 1950. EN Lot: "Operation Arava in 1949-1950". HE Eilat: improved in 1950 (cited: HaTzofe, 9 March 1951) |
| 1949-53/54 | The only road to Eilat: "the oil road", Route 227 (Ma'ale Akrabim) and the Arava road. Beersheba to Eilat took ten hours by jeep | HE Eilat (cited: Ma'ariv, 22 September 1967, a retrospective) |
| until 1954 or 1956 | Main Beersheba - Eilat route | **[C]** HE Akrabim: until 1954 (the Ma'ale HaAtzmaut road). EN R227: "prior to 1956" (citing Israel Traveller, a dead link) |

"The oil road" (כביש הנפט, Negev) is a separate Hebrew article not read for this note; which road it was in 1949 is
[U].

The Negev Brigade's own route in Uvda (Beersheba - Bir Asluj - the Ramon crater - Sde Avraham airstrip - the Valley of
Fingers - Ras al-Naqb - Umm Rashrash, EN Uvda) was a desert approach, not a road; it should not be drawn as one.

Waypoints (approximate): Beersheba (31.2522, 34.7867); Kurnub / Mamshit (31.025, 35.064); Ma'ale Akrabim (30.9067,
35.1317); Ein Husub / Metzad Hatzeva (30.8087, 35.2459); Ein Yahav (30.6586, 35.2367); Yotvata / Ein Ghadyan (29.8956,
35.06); Umm Rashrash (29.5492, 34.9534). The line between Ein Husub and Yotvata is not described in any source read
[U]. Eilat airfield opened on 28 February 1950 (HE Eilat); Arkia (first "Ilata") was founded in December 1950 for the
Eilat route (HE Eilat). Neither is a corridor, but both bear on how cut off the road left Eilat.

**After the window:** the road to Eilat by Yeruham and the Ramon crater (Route 40 south and Route 204) was cut by the
IDF from 1951 and opened officially in February 1954, 245 km (HE R204, cited: Al HaMishmar, 21 February 1954; HE R40,
cited: Davar, 21 February 1954). **[C]** HE Eilat says "early 1953".

### D11. Kurnub (Dimona) - Sodom road, and the Yeruham link

The "Sodom road" from Sodom up to the Dimona area: built from January 1951, opened with a ceremony on 26 March 1953
after 28 months, climbing about 800 m (HE R25, cited: Davar, 24 November 1952; Davar, 27 March 1953). The Yeruham link
(Route 204 north) was built in 1952 and opened in February 1953 as a cheap connection from the Sodom road to "the oil
road" (HE R204). Beersheba - Dimona opened at the end of 1955 (HE R25). Only the start of work (January 1951) falls in
the window. Waypoints: Mamshit (31.025, 35.064); Dimona (31.0667, 35.0333); Neve Zohar (31.1522, 35.3653, a modern
village on the descent; Sodom has no article point).

## Other candidates checked and left out

- **Jerusalem - Jericho, Jerusalem - Hebron, Jerusalem - Nablus, Tel Aviv - Tulkarm - Nablus.** On the Jordanian side
  of the 1949 lines for most of their length (EN R1 for the Jericho road). The player cannot build them.
- **Gaza - Beersheba, the coastal road south of Majdal, the Kantara line south of Dayr Sunayd.** Cut at the Gaza Strip
  line (EN Lod-Ashkelon for the railway). The sources read give no 1949-51 work on them.
- **Haifa - Nazareth - Tiberias, Tiberias - Metula.** In Israeli hands; no 1948-51 works found in the sources read.

## Comparison with the repo

Today the corridors exist only as map data: `content/map/routes_1948.yaml` (hand-written, 413 lines) and the generated
`content/map/route_paths_1948.yaml`. Each route has an opening `from` and an optional `to`, and the map draws it inside
that window (`routes_1948.yaml` lines 18-22). There is no state for "cut" or "under construction", and nothing in the
sim reads either file (impact plan §1.5). No `corridors.yaml` exists yet.

| # | Route or gap | Repo today (file:line) | Finding | Kind |
|---|---|---|---|---|
| 1 | Jaffa-Jerusalem railway reopening | `routes_1948.yaml:42-46`: "regular trains between Ramla and Jerusalem resumed in March 1950"; `from: '1920-10-05'` (line 40), no `to` | The line was out of use from 1948; the ceremonial reopening was 7 August 1949; regular passengers 2 March 1950 (R3). The map draws the line as running throughout | Missing date; no cut state |
| 2 | Jaffa terminus | `routes_1948.yaml:47-48` to_verify: closing date of Jaffa station not in the cited text | HE JLM rail: Jaffa station abandoned in 1948, Tel Aviv South the terminus from 1949. Month still not found | Partly answered |
| 3 | Tulkarm bypass | `routes_1948.yaml:96-101`: bypass "not drawn"; to_verify gives August 1948 vs 1949 | Haaretz, 4 August 1949 (cited by HE Coastal) reports the bypass section finished; trains to Haifa from September 1949 (R1). Favours 1949; Cotterell p. 86 still to check | Conflict narrowed; route missing |
| 4 | Eastern Railway as drawn | `routes_1948.yaml:90-113`, via Qalqilya and Tulkarm (lines 105-106), `from: '1915'`, no `to` | The stretch through Tulkarm stayed Jordanian; from 1949 Israel's trains used the bypass. The drawn line through Tulkarm needs a `to` and the bypass its own entry | Wrong route after 1949 |
| 5 | Rosh HaAyin - Tel Aviv North link | not present | Yarkon branch extended about 7 km to Tel Aviv North, opened with the Haifa-Tel Aviv line on 20 September 1949 (R1) | Missing corridor |
| 6 | Coastal railway Remez Jct - Tel Aviv North | not present | Built about January 1949 to 1953; opened 14 May 1953 [C April] (R2). This is the GDD's railway; it was not in service before 1953 | Missing corridor (needed for D-B3) |
| 7 | Haifa - Acre - Nahariya railway | `routes_1948.yaml:139-144`: "in 1948 Israel's trains ran north from Haifa to Kiryat Motzkin and later Nahariya"; `from: '1942'` | Na'aman bridge south of Acre blown in 1948, rebuilt 1950, line reopened 1950 [C with Haifa-Acre trains from 4 January 1949] (R4) | Missing cut; conflict |
| 8 | Jezreel Valley railway end | `routes_1948.yaml:170-171`: service ended 1949, occasional use to September 1951 | HE IR: shut at the start of summer 1951 (R5) | Minor conflict |
| 9 | Jaffa-Jerusalem road through Latrun | `routes_1948.yaml:185-204`, `from: '1946'`, no `to`; note (190-191) says the Legion held Latrun from 18 May 1948 | Closed to Israeli traffic from 18 May 1948 to 1967; the armistice clause to open it was never carried out (D1). Needs a cut state or a `to` for the Latrun stretch | Missing cut |
| 10 | Burma Road | `routes_1948.yaml:205-226`, `from: '1948-06-14'` | Matches EN Burma. Could start at 10 June 1948 (first vehicle passage) or 1 June (first jeeps) if the game wants "usable" rather than "complete"; 14 June is the sourced completion | Correct |
| 11 | Road of Valour | not present | Built 4 September - 30 October 1948 [C on start], opened 7 December 1948; Nahshon Jct - Shimshon Jct - Sha'ar HaGai (D3) | Missing corridor |
| 12 | Ramla - Nahshon Jct link | not present | Opened September 1950 (D4) | Missing corridor |
| 13 | Tel Aviv - Haifa road | `routes_1948.yaml:227-251`, `from: '1937'` | No change found. The Tel Aviv-Netanya coastal road (opened July 1950) is missing (D5) | Missing corridor (Route 2) |
| 14 | Wadi Ara road | not present | Iraqi-held to the armistice; reopened May 1949 (D6) | Missing corridor |
| 15 | Acre - Safed road | not present | Reopened early 1949 (D7) | Missing corridor |
| 16 | Tiberias - Beit She'an road and Gesher - Menahamia bypass | not present | Road cut at Naharayim; bypass opened November 1950 (D8) | Missing corridors |
| 17 | Malachi - Kama road to Beersheba | not present | Built July 1949 - June/July 1951 (D9) | Missing corridor |
| 18 | Beersheba - Kurnub - Ma'ale Akrabim - Arava - Eilat | not present | Track from 1927; taken November 1948 and March 1949; improved 1950 [C] (D10) | Missing corridor |
| 19 | Kurnub/Dimona - Sodom road | not present | Built from January 1951, opened 26 March 1953 (D11) | Missing corridor (completes after the window) |
| 20 | Beersheba - Auja road | `routes_1948.yaml:400-413`, `from: '1946'` | No 1949-51 change found | Correct as far as checked |
| 21 | Routes in Jordanian or Egyptian hands after 1949 | `routes_1948.yaml:287-344` (Hebron, Nablus, Tulkarm roads), 360-372 (Jericho), 252-269 and 386-399 (south of Majdal, Gaza-Beersheba) | Drawn with no `to`. Right for a map of what lay on the ground; a corridor list should leave them out | Not corridors |
| 22 | Traced geometry for new corridors | `route_paths_1948.yaml` (generated, ids at lines 8-1619) traces only on the 1946 map | Items 5, 6, 11, 12, 14 (partly on the 1946 road), 16, 17, 18, 19 did not exist or differ from the 1946 sheet; impact plan §6 Phase 4 lists this risk | Tooling gap |

## Open questions for the owner

1. **D-B3:** which railway milestone the objective card uses (R2, options a-c).
2. **D-D7:** which of the missing corridors become designated corridors. The impact plan proposes the Tel Aviv-Haifa
   rail plus 2-3 roads. Candidates with good in-window dates: the Road of Valour (D3), the Tel Aviv-Netanya road (D5),
   the Malachi-Kama road (D9) and the road to Eilat (D10).
3. Whether existing routes get a "cut" state (Latrun road, Eastern Railway through Tulkarm, Haifa-Nahariya), or only a
   `to` date with a separate bypass route.

## Sources

Wikipedia articles, read through the API on 2026-10-13 (revision id in brackets). Licence CC BY-SA 4.0.

- EN Burma: "Burma Road (Israel)", https://en.wikipedia.org/wiki/Burma_Road_(Israel) [1355307500]
- EN JJR: "Jaffa–Jerusalem railway", https://en.wikipedia.org/wiki/Jaffa%E2%80%93Jerusalem_railway [1372529038]
- EN Eastern: "Eastern Railway (Israel)", https://en.wikipedia.org/wiki/Eastern_Railway_(Israel) [1357861699]
- EN PR: "Palestine Railways", https://en.wikipedia.org/wiki/Palestine_Railways [1379014541]. It cites Paul Cotterell,
  *The Railways of Palestine and Israel*, Abingdon: Tourret Publishing, 1984, ISBN 0-905878-04-3, pp. 84, 86, 88 (not
  read for this note).
- EN Lod-Ashkelon: "Lod–Ashkelon railway", https://en.wikipedia.org/wiki/Lod%E2%80%93Ashkelon_railway
- EN Beersheba railway: "Railway to Beersheba", https://en.wikipedia.org/wiki/Railway_to_Beersheba
- EN Uvda: "Operation Uvda", https://en.wikipedia.org/wiki/Operation_Uvda (it cites Jehuda Wallach (ed.), *Battle Sites
  in the Land of Israel* (Hebrew), Carta, 2003, pp. 15-16)
- EN Lot: "Operation Lot", https://en.wikipedia.org/wiki/Operation_Lot
- EN R1, EN R2, EN R4, EN R38, EN R44, EN R65: "Highway 1 (Israel)", "Highway 2 (Israel)", "Highway 4 (Israel)",
  "Highway 38 (Israel)", "Highway 44 (Israel)", "Highway 65 (Israel)", https://en.wikipedia.org/wiki/Highway_1_(Israel)
  and so on
- EN R227: "Route 227 (Israel)" (Ma'ale Akrabim redirects here), https://en.wikipedia.org/wiki/Route_227_(Israel)
  [1361872666]
- HE Gvura: "כביש הגבורה", https://he.wikipedia.org/wiki/כביש_הגבורה [43319595]
- HE Coastal: "מסילת החוף", https://he.wikipedia.org/wiki/מסילת_החוף [42711693]
- HE IR: "רכבת ישראל", https://he.wikipedia.org/wiki/רכבת_ישראל [43876266]
- HE TA North: "תחנת הרכבת תל אביב צפון" (redirects to the Bnei Brak station article),
  https://he.wikipedia.org/wiki/תחנת_הרכבת_תל_אביב_צפון [43883317]
- HE Yarkon: "מסילת הירקון", https://he.wikipedia.org/wiki/מסילת_הירקון
- HE Remez: "צומת רמז", https://he.wikipedia.org/wiki/צומת_רמז
- HE JLM rail: "מסילת הרכבת לירושלים", https://he.wikipedia.org/wiki/מסילת_הרכבת_לירושלים
- HE R1, HE R2, HE R4, HE R25, HE R40, HE R44, HE R65, HE R85, HE R90, HE R204, HE R424: "כביש 1", "כביש 2", "כביש 4",
  "כביש 25", "כביש 40", "כביש 44", "כביש 65", "כביש 85", "כביש 90", "כביש 204", "כביש 424",
  https://he.wikipedia.org/wiki/כביש_1 and so on. Revisions: R2 [43966837], R25 [42981794], R40 [43957522], R44
  [43555948], R65 [43959856], R85 [43959865], R90 [43960345], R204 [42371469]
- HE R227: "כביש 227", https://he.wikipedia.org/wiki/כביש_227
- HE Akrabim: "מעלה עקרבים", https://he.wikipedia.org/wiki/מעלה_עקרבים [43643617]
- HE Lot: "מבצע לוט", https://he.wikipedia.org/wiki/מבצע_לוט [43218671]
- HE Yoav: "מבצע יואב", https://he.wikipedia.org/wiki/מבצע_יואב
- HE Uvda: "מבצע עובדה", https://he.wikipedia.org/wiki/מבצע_עובדה
- HE HaHar: "מבצע ההר", https://he.wikipedia.org/wiki/מבצע_ההר
- HE Eilat: "אילת", https://he.wikipedia.org/wiki/אילת [44022047]

Not reachable on 2026-10-13: the Israel Railways history pages on rail.co.il and gov.il (HTTP 403 to a scripted
request). Israel Railways' 1998 booklet "50 Years of Israel Railways", cited by HE IR, is archived at
https://web.archive.org/web/20140327112217/http://www.rail.co.il/HE/Fun/Museum/Documents/50Years.pdf (not read).

The contemporary newspapers named "(cited: ...)" above (Davar, Ma'ariv, Haaretz, Al HaMishmar, HaTzofe, Herut,
HaBoker) are the next step for fact-checking each date before it goes into `content/`.
