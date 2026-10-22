# Fact-check sheet for #1244: the three draft corridor rows

For the person doing #1244's check: open each source and record what it supports, with the page and the day you read
it. Built on 22 October 2026 from `origin/main` at `ece85d71` (`content/corridors.yaml`, `content/map/routes_1948.yaml`,
`research_notes/gdd-phase-4-sourcing.md` and `docs/rules/mvp.md` are unchanged since `e41b0b8f`).

**Nothing here is checked.** An AI agent opened the sources it could reach and copied the sentences below. The
"Agent reading" column is its reading, for the person to confirm or reject. Only the person fills **Checked by / date /
result**, changes `status` and writes `reviewed_by` and `reviewed_on` (#1244, "How").

How the quotes were taken (2026-10-22):
- ***The Jerusalem Post*, 30 July 1950:** the Internet Archive's OCR of this issue is badly garbled ("Rishpon", "26" and
  "eight kilometre" are not in it at all). Every quote from it below was read on the **page image** (the issue's PDF,
  rendered at 4x) and typed from the image. Bracketed notes mark a word the scan blurs.
- ***The Jerusalem Post*, 28 July 1950:** quoted from the archive's **OCR text**, whose lines here are clean; not
  compared with the image. The OCR's line-break hyphens ("Min- ister", "tomor- row") are joined in the quote.
- **Israel Railways booklet (1998):** the PDF is scanned pages with **no text layer**. The Hebrew below was **transcribed
  by the agent from the page image** (printed p. 15, the left half of PDF page 6); the person should compare it with the
  scan, including maqaf and hyphen marks.
- **Wikipedia:** read through the MediaWiki API (plain text), revision given.

Rules the rows serve (`docs/rules/mvp.md`): R-COR-001 (line 169), R-COR-002 (line 171), R-COR-003 (line 173). They
govern how segments are built, not the historical facts below.

## Sources and whether they were reached

| Key | Source | URL | Reached 2026-10-22 |
|---|---|---|---|
| EN-BURMA | en.wiki, "Burma Road (Israel)", revision 1355307500 (2026-05-21; the current revision, and the one `gdd-phase-4-sourcing.md` read) | https://en.wikipedia.org/wiki/Burma_Road_(Israel) | yes, 200 |
| ISSEROFF | Ami Isseroff, "Burma Road", zionism-israel.com, archived 15 April 2021 (cited by EN-BURMA) | https://web.archive.org/web/20210415133543/https://zionism-israel.com/dic/Burma_Road.htm | yes, 200 |
| ROSENBLOOM | Michael Rosenbloom, "The Road to Jerusalem", ohav.org (cited by EN-BURMA), archived 2 December 2008 | https://web.archive.org/web/20081202020201/http://www.ohav.org:80/columns/survival/solomon3.html | archived copy yes; the live page answered 406 |
| IR98 | Israel Railways, *50 Years of Israel Railways* (Hebrew booklet, September 1998), printed p. 15 = left half of PDF page 6 | https://web.archive.org/web/20140327112217/http://www.rail.co.il/HE/Fun/Museum/Documents/50Years.pdf | yes, 200, 4,074,133 bytes, 12 PDF pages, image-only |
| IR98-live | the same booklet on Israel Railways' site | http://www.rail.co.il/HE/Fun/Museum/Documents/50Years.pdf | **not reached**: HTTP 403 |
| EN-BNEI | en.wiki, "Bnei Brak–Ramat HaHayal railway station" (the routes file's "Bnei_Brak_railway_station" redirects here), revision 1370812274 (2026-08-23) | https://en.wikipedia.org/wiki/Bnei_Brak_railway_station | yes, 200 |
| JP-0730 | *The Jerusalem Post*, Sunday 30 July 1950 (vol. XXVI no. 7318), pp. 1 and 3 | https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1950-07-30_26_7318 | yes, 200 (PDF, 4 pages) |
| JP-0728 | *The Jerusalem Post*, 28 July 1950 (no. 7317), p. 3 (hOCR page 3) | https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1950-07-28_26_7317 | yes, 200 (OCR text) |
| JPRESS | the National Library's JPress copy of *The Jerusalem Post* | https://www.nli.org.il/he/newspapers/jpost/1950/07/30/01/article/1 (tried) | **not reached**: HTTP 403 to scripted requests; a person can open it in a browser |

## 1. `burma_road` (`content/corridors.yaml` lines 19-33)

The corridor cites one source, EN-BURMA (line 32, `checked: 2026-10-11`). Its waypoints and dates come from the route
`burma_road` in `content/map/routes_1948.yaml` (lines 263-283), which cites the same article.

| # | Claim as content states it | file:line | Cited source | Agent reading: what the source says (verbatim, URL, read 2026-10-22) | Agent reading | Checked by / date / result |
|---|---|---|---|---|---|---|
| B1 | The road ran "from just east of Dayr Muhaysin by Bayt Jiz and Bayt Susin, up to Bayt Mahsir and Saris, joining the old Jerusalem road" | routes_1948.yaml:269-270; waypoints :277-281; segments corridors.yaml:25-28 | EN-BURMA | "The road ran from just east of Dayr Muhaysin (today Moshav Beko'a), by way of Bayt Jiz and Bayt Susin (near kibbutz Harel), and then crossed the road that is now known as Highway 38." / "From there it ascended to Bayt Mahsir (Beit Meir), Saris (Shoresh and Sho'eva), and then connected with the old Jerusalem road." (EN-BURMA) | matches | |
| B2 | Built "in May-June 1948" | routes_1948.yaml:269 | EN-BURMA | the dates in B3 to B5 fall in May and June 1948; no single sentence says "May-June" | consistent, not stated as such | |
| B3 | "The first jeep attempt failed on the night of 30-31 May and a second got through the next night (31 May - 1 June)" | routes_1948.yaml:270-271 | EN-BURMA | "On the night of May 30–31, an attempt failed when the lead jeep overturned." / "A second attempt on the following night succeeded." (EN-BURMA) | matches | |
| B4 | "the steepest section opened to vehicles on 10 June" | routes_1948.yaml:271-272 | EN-BURMA | "Three weeks later, 10 June, the steepest section was opened to vehicles, though they needed assistance from tractors to get up it." (EN-BURMA) | matches; see tension T2 | |
| B5 | "the road was completed on 14 June 1948"; the map shows it from that day (`from: '1948-06-14'`); the corridor's comment "its completion on 14 June 1948" | routes_1948.yaml:267, :272; corridors.yaml:30 | EN-BURMA | "The road was finally completed on June 14, and water and fuel pipes were laid alongside it." (EN-BURMA; it cites ISSEROFF and ROSENBLOOM). ISSEROFF: "...it was not really complete until June 14, when UN inspectors pronounced the road paved." ROSENBLOOM (2008 copy): no date of completion; it says "The Burma Road was not paved nor was it easily passable." | matches (EN-BURMA, ISSEROFF); ROSENBLOOM does not mention it | |
| B6 | Four segments, one between each pair of consecutive waypoints | corridors.yaml:25-28 | owner's Q-COR-2 (corridors.yaml:3-4) | design, not a historical claim | n/a | |
| B7 | Name "Burma Road" | corridors.yaml:21 | EN-BURMA | "Burma Road (Hebrew: ... Derekh Burma) in Israel was a makeshift bypass road between Kibbutz Hulda and Jerusalem..." (EN-BURMA, its first words; the Hebrew is in the article) | matches | |

## 2. `tlv_haifa_rail` (`content/corridors.yaml` lines 34-46)

The corridor cites IR98 (line 45, `checked: 2026-10-15`); the route `yarkon_rail_link` (`routes_1948.yaml` lines
476-497) cites IR98 and EN-BNEI.

| # | Claim as content states it | file:line | Cited source | Agent reading: what the source says (verbatim, URL, read 2026-10-22) | Agent reading | Checked by / date / result |
|---|---|---|---|---|---|---|
| R1 | "the two sections laid in the summer of 1949" | corridors.yaml:43-44; routes_1948.yaml:482 | IR98, p. 15 | Transcribed from the scan (no text layer): "במארס 1949, עם חתימת חוזה שביתת הנשק, מעבירה ירדן לשליטת ישראל קטעים ממסילות בית־שמש-ירושלים ולוד-חדרה־מזרח. בקיץ של אותה שנה סוללת רכבת ישראל שני קטעי מסילה חדשים: האחד עוקף את תחנת טול־כרם, שנותרה בידי הירדנים, ומאפשר את חידוש הרצף המסילתי בין חיפה למרכז הארץ; האחר מחבר את קו ראש־העין-פתח־תקוה עם תחנת תל־אביב־צפון הישנה (כיום תחנת בני־ברק, ליד איצטדיון הכדורגל ברמת־גן)." | matches ("the summer of that year" follows "March 1949") | |
| R2 | "the service from 20 September 1949"; the map shows it from that day (`from: '1949-09-20'`) | corridors.yaml:44; routes_1948.yaml:480, :484 | IR98, p. 15; EN-BNEI | IR98, transcribed: "ב־20 בספטמבר 1949 נחנך שירות סדיר לנוסעים בקו חיפה-בנימינה-חדרה־מזרח-ראש־העין-תל־אביב־צפון (הישנה). משך הנסיעה שעתיים ו־20 דקות." EN-BNEI: "The station was originally opened on September 20, 1949, for the purpose of serving the residents of Tel Aviv, and named Tel Aviv North." | matches | |
| R3 | The new track ran "from the Rosh HaAyin–Petah Tikva branch ... to a new Tel Aviv North station" | routes_1948.yaml:482-483; segment end corridors.yaml:41 | IR98, p. 15; EN-BNEI | IR98: "...האחר מחבר את קו ראש־העין-פתח־תקוה עם תחנת תל־אביב־צפון הישנה..." (R1). EN-BNEI: "In 1949, Israel Railways decided to lengthen this railroad branch further to the west, and thus created this station." | matches the ends. IR98 calls it "the old Tel Aviv North station" (old as against today's name; EN-BNEI says it was created in 1949) | |
| R4 | "west along the Yarkon" | routes_1948.yaml:482-483 | IR98; EN-BNEI | IR98 does not mention the Yarkon. EN-BNEI: "It is located on the Yarkon Railway near the Bnei Brak–Ramat Gan and Bnei Brak–Tel Aviv borders next to the Ayalon Mall and Ramat Gan Stadium." (today's location, not the 1949 alignment) | not stated for 1949 | |
| R5 | "about 7 km" | routes_1948.yaml:483 | IR98; EN-BNEI | neither gives a length. `gdd-phase-4-sourcing.md` (§2, "The Rosh HaAyin-Tel Aviv North (Yarkon) link") sources "about 7 km" to he.wiki (HE Bnei Brak station, HE Yarkon), which the row does not cite | **not in the cited sources** (U1) | |
| R6 | "(renamed Bnei Brak after 1954)" | routes_1948.yaml:483 | EN-BNEI | "With the opening of Tel Aviv Center in November 1954 by extending the Coastal railway further into Tel Aviv, the station's relevance was again put into question and its name was changed to Bnei Brak, with only a few trains on the Haifa–Jerusalem service continuing to use it." | matches | |
| R7 | The corridor is the Yarkon link alone; the bypass west of Tulkarm is not drawn because "no source read gives its line" | corridors.yaml:4-6; routes_1948.yaml:487 | IR98 | IR98 names the Tulkarm bypass (R1) but gives no line or end points | matches (a gap, stated as one) | |
| R8 | The trace "follows the tracks the map shows between the two stations, not the 1949 alignment" | routes_1948.yaml:486 | none | no source read gives the 1949 alignment; #1244 asks the person to check the trace against it | **no source** (U2) | |
| R9 | One segment | corridors.yaml:41 | design (Q-COR-2) | n/a | n/a | |

## 3. `tlv_netanya_road` (`content/corridors.yaml` lines 47-62)

The corridor cites JP-0730 (line 61, `checked: 2026-10-15`); the route `tlv_netanya_road` (`routes_1948.yaml` lines
503-527) cites JP-0730 and JP-0728.

| # | Claim as content states it | file:line | Cited source | Agent reading: what the source says (verbatim, URL, read 2026-10-22) | Agent reading | Checked by / date / result |
|---|---|---|---|---|---|---|
| N1 | Opened on 28 July 1950 (`from: '1950-07-28'`; "the opening on 28 July 1950") | corridors.yaml:50, :59; routes_1948.yaml:507, :509-510 | JP-0730 p. 1; JP-0728 p. 3 | JP-0730 p. 1 (image; masthead "SUNDAY July 30, 1950"; dateline Saturday, so "yesterday" is 28 July): "TEL AVIV, Saturday. — Israel's newest highway, the 26-kilometre road bringing Natanya within half an hour's driving distance of Tel Aviv, and Herzlia [the scan blurs this word] within ten minutes, was officially declared open to traffic yesterday." JP-0728 (OCR, datelined Thursday): "The new Tel Aviv-Natanya coast road will be officially opened by Mrs. Golda Myerson, Minister of Labour, at 11 o'clock tomorrow morning. The road will be opened to regular traffic from 2 o'clock tomorrow afternoon." | matches | |
| N2 | "opened by Golda Myerson" | routes_1948.yaml:509-510 | JP-0730 p. 1 | (image) "The Minister of Labour and National Insurance, Mrs. Golda Myerson, cut a blue-and-white tape stretched across the highway just north of the Yarkon bridge at midday." | matches | |
| N3 | "26 km" | routes_1948.yaml:509 | JP-0730 p. 1 | "the 26-kilometre road" (N1) | matches | |
| N4 | Starts at "the northern end of the Yarkon bridge in Tel Aviv" (first waypoint "Yarkon bridge (Tel Aviv)") | routes_1948.yaml:509, :518 | JP-0730 p. 1 | (image) "Mayor I. Rokach of Tel Aviv, also in a reminiscent mood, recalled the time when an Arab melon vendor had his stall at the northern end of the Yarkon bridge where the new road now starts." | matches. The waypoint's coordinates are the Yarkon River article's point, not a bridge's (routes_1948.yaml:518; U3) | |
| N5 | Passes Herzliya (segment 1 ends there) | corridors.yaml:54; routes_1948.yaml:509, :519 | JP-0730 p. 3 | (image) "When the convoy of some scores of cars reached the entrance to Herzlia less than ten minutes after leaving Tel Aviv it was greeted by the Chairman of the Local Council, Mr. B.Z. Michaeli..." | matches | |
| N6 | Passes Rishpon (segment 2 ends there) | corridors.yaml:55; routes_1948.yaml:520 | JP-0730 p. 1 | (image, box "Safety First — For Three Minutes") "Although the road is closed to horse traffic, a horse-drawn cart was seen on the road near Rishpon two hours after the official opening." | mentions Rishpon as near the road; does not say the road passes it (T1) | |
| N7 | Wadi Falik (Nahal Poleg) (segment 3 ends there); "an 8 km stretch at Wadi Falik was still unfinished, with a detour" | corridors.yaml:56; routes_1948.yaml:510-511, :521 | JP-0730 pp. 1 and 3 | p. 3 (image): "An eight kilometre stretch of the seven metre wide highway at Wadi Falik is still to be completed. Until it is ready vehicles are forced to make a detour on a side track." p. 1 (image, same box): "...when Air Force tender slid ... the ditch at the Wadi Falik detour." [one word blurred] | matches. That Wadi Falik is Nahal Poleg is the route file's identification (its EN Poleg link), not the newspaper's | |
| N8 | Ends at Netanya (segment 4) | corridors.yaml:57; routes_1948.yaml:522 | JP-0730 p. 3 | (image) "Our Natanya correspondent reports that Mayor O. Ben Ami, of Natanya, drove out to Wadi Falik to greet Mrs. Myerson and the convoy, opening the road, which enters the township between the area set aside for the new industrial zone and immigrants' quarters." | matches | |
| N9 | "the five places the road passed" | corridors.yaml:59-60 | JP-0730 pp. 1 and 3 | the five places are all in JP-0730 (N4 to N8); the paper names no other place on the road. Its fare notice (OCR only) may name Kfar Shmaryahu and Shefayim (`gdd-phase-4-sourcing.md` §3, [U: OCR]) | matches, with T1 | |
| N10 | "the coastal road" | corridors.yaml:50 | JP-0728; JP-0730 p. 1 | JP-0728 (OCR): "The new Tel Aviv-Natanya coast road..." JP-0730 p. 1 (image): Rokach "paid tribute to the late Mayor Dizengoff, who had dreamed of such a coastal road" | matches | |
| N11 | Not finished when opened (implied by the detour) | routes_1948.yaml:510-511 | JP-0730 p. 3 | (image) "Although now open to traffic, the road was completed neither in its length nor in its width, she pointed out." | matches | |
| N12 | Four segments, drawn as straight lines between the five places | corridors.yaml:6-8, :54-57; routes_1948.yaml:505, :513-514 | owner, 18 October 2026, #786 | design, not a historical claim; no source gives the 1950 line | n/a (U4) | |

## Claims with no source among the rows' cited sources

- **U1 (R5):** the Yarkon link's "about 7 km" (`routes_1948.yaml:483`). Neither IR98 nor EN-BNEI gives a length. The
  sourcing note takes it from Hebrew Wikipedia, which the row does not cite.
- **U2 (R8):** the rail link's 1949 alignment. The trace follows the 1946 map's tracks; no source read gives the built
  line (the row says so itself).
- **U3 (N4):** the Yarkon bridge waypoint's coordinates are the river article's point; which bridge is not given.
- **U4 (N12, R8, B6):** the segment lines and counts are design (Q-COR-2; the owner's straight-line draft for the road),
  not sourced facts.

## Tensions and points for the person

- **T1 (N6):** JP-0730 places a cart "on the road near Rishpon"; it does not say the road passes Rishpon. The content
  makes Rishpon a waypoint between Herzliya and Wadi Falik. Nothing read contradicts that; the paper just doesn't state
  it.
- **T2 (B4):** EN-BURMA has vehicles on the steepest section on 10 June "though they needed assistance from tractors";
  ISSEROFF: "...until June 11, about four kilometers of the road had to be traversed by donkeys and porters. On June
  11, the first day of the cease fire, the road was used for the first time by completely motorized vehicle transport."
  Both can hold; the row uses EN-BURMA's 10 June for the steep section only.
- **T3 (B5):** EN-BURMA cites two sources for "finally completed on June 14". ISSEROFF supports the date; the archived
  ROSENBLOOM page does not mention it.
- **T4 (R1):** IR98 has a known error elsewhere on the same page: it dates the first train to Jerusalem "ב־17 באוגוסט 1948"
  (17 August 1948), where `gdd-phase-4-sourcing.md` ("Unverified and conflicting", item 9) gives 7 August 1949 from the
  press. Its 1949 facts used here agree with EN-BNEI.
- **Not a contradiction, wording only (R3):** IR98's "the old Tel Aviv North station" against the route note's "a new
  Tel Aviv North station"; EN-BNEI says the station was created in 1949.

No source read contradicts a date, place or figure in the three rows.

## Not reached

- **JPRESS** (the National Library's *Jerusalem Post*): HTTP 403 to scripted requests. JP-0730 on the Internet Archive was
  read instead. A person can open JPress in a browser to compare.
- **IR98-live** (rail.co.il): HTTP 403. The Wayback copy was read.
- **The Hebrew names** (`דרך בורמה`, `קישור הרכבת חיפה–תל אביב`, `כביש תל אביב–נתניה`) are for the native read on
  #564, not this check.
