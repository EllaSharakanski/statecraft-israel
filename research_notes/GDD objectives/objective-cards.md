# Objective cards: dated events, sources and photographs (research note, 2026-10-13)

Status: research only, for the owner's choice. Nothing here is in `content/` or `unity/`. Refs #380, #375 (D-B2,
D-B3, D-B4) and #378 (the Yeruham photograph question, ruling 4).

The GDD (docs/GDD.md, "Historical events on objective completion" and "MVP completion rules") asks for each
objective: a real event, its sourced reference date, an authentic image and a short account. For immigrant
absorption it asks for a defined milestone with a sourced date. This note gives candidates for the four cards the
GDD's three objectives need: end of the war, absorption, the Tel Aviv–Haifa railway and the first new yishuv.

How it was done:
- Every web page was read on 13 October 2026 unless marked otherwise. Wikipedia pages were read through the
  MediaWiki API (plain-text extract of the current revision).
- Every photograph was checked on its Commons file page through the Commons API: the licence template in the page
  source, the artist, the date, the NLI title, the VRTS ticket, the original URL and its SHA-1.
- Photographs follow the event-photograph rule in docs/CONTENT_POLICY.md §4 and docs/event-images.md: public domain
  or CC0 with a valid US basis, or CC BY 4.0 with a credit line. GPO, PD-Israel-only, PD-India-only and share-alike
  (CC BY-SA) files are refused. Under that rule only the Meitar Collection files below are usable. The others are
  listed so nobody searches for them again.
- [C] marks a conflict between sources. "Unverified" means I found the claim but could not read the primary source.

## Summary

| Card | Recommended event and date | Photograph | Open point |
|---|---|---|---|
| End of the war | Israel–Syria armistice signed at Hill 232 near Mahanayim, 20 July 1949 | None of the signing. Use an alternative (below) or the Operation Uvda photo | Whether the card is the armistice (date) with a picture of another event, or Uvda (10 March 1949) with its own picture |
| Absorption | Jewish population passes 1,000,000, celebrated as "Ingathering of the Exiles Day", 20 December 1949 [C on the day] | A ma'abara on the road to Jerusalem, June–August 1950 (Rothenberg), or none fitting the date | The GDD objective is housing and work; a population milestone measures arrival, not absorption. Owner to choose |
| Railway | Haifa–Tel Aviv passenger line opens via Rosh HaAyin to the new Tel Aviv North station, 20 September 1949 | Boris Carmi, Israel Railways 1949 (not of the opening day) | The coastal line itself opened in 1953, outside the MVP |
| First new yishuv | Tifrah, founded 27 September 1949 (one source to the day) [C on the year] | Benno Rothenberg, "the founding of moshav Tifrah in the Negev", 2 December 1949, CC BY 4.0 | A second source for the day. Yeruham's 1951 photos are not usable (below) |

## 1. End of the war: the last armistice

### The date

The Israel–Syria General Armistice Agreement was signed on 20 July 1949. The signing date and the entry into force are
the same day: Article VIII.1 says the agreement "shall come into force immediately upon being signed". The text ends
"DONE at Hill 232 near Mahanayim on the twentieth of July nineteen forty-nine".
- Avalon Project, Yale Law School, "Israeli-Syrian General Armistice Agreement, July 20, 1949" (UN doc. S/1353/Rev.1),
  https://avalon.law.yale.edu/20th_century/arm04.asp, accessed 2026-10-13.
- United Nations Treaty Series vol. 42 (1949), No. 657, p. 327, as already recorded in
  `research_notes/Map borders/armistice-lines-1949.md` (read there 2026-10-11).
- English Wikipedia, "1949 Armistice Agreements": talks began at Gesher B'not Yaacov in April 1949; signed 20 July
  1949. https://en.wikipedia.org/wiki/1949_Armistice_Agreements, accessed 2026-10-13.
- Hebrew Wikipedia, "מלחמת העצמאות": after the Syrian armistice of 20 July 1949 the Syrian army left Mishmar HaYarden
  and the area became demilitarised; "so the War of Independence formally ended".
  https://he.wikipedia.org/wiki/מלחמת_העצמאות, accessed 2026-10-13.
- The existing event `content/events/armistice_syria.yaml` already uses 1949-07-20.

No source I read gives a different date for the signing. I did not find an official Israeli definition of the war's
end date (for example the Ministry of Defence's period for the fallen); Hamichlol's article on the fallen gives the
war as 30 November 1947 to 20 July 1949 (search result only, unverified).

### Alternatives

| Event | Date | Source | Note |
|---|---|---|---|
| End of Operation Horev; large-scale fighting ends | 7 January 1949 [C] | he.wiki "מלחמת העצמאות" gives 7 January in the narrative and 8 January in the phase summary | The repo already has `operation_horev` with a photograph |
| Israel–Egypt armistice, Rhodes | 24 February 1949 | en.wiki "1949 Armistice Agreements"; UNTS vol. 42 No. 654 | Has a photograph in the repo (`armistice_egypt`) |
| End of Operation Uvda, flag raised at Umm Rashrash; the IDF's last operation of the war | 10 March 1949 | en.wiki "Operation Uvda" (5–10 March 1949; "considered to have ended the war"); he.wiki "מלחמת העצמאות" ("מבצע עובדה סיים את הפעילות הקרבית") | Good photograph available (below) |
| Israel–Lebanon armistice, Ras en Naqura | 23 March 1949 | en.wiki; UNTS No. 655 | |
| Israel–Jordan armistice, Rhodes | 3 April 1949 | en.wiki; UNTS No. 656 | |

https://en.wikipedia.org/wiki/Operation_Uvda, accessed 2026-10-13.

Recommendation: keep 20 July 1949 as the reference date (it is the plan's D-B2 recommendation and the formal end). If
the owner wants the card's picture to show its own event, Uvda (10 March 1949) is the only end-of-war candidate with a
usable photograph of the day's events.

### Photographs

Usable:
- **Operation Uvda, parade on the Red Sea shore, March 1949.** Benno Rothenberg. NLI title "מסדר על חוף ים סוף"
  (a parade on the Red Sea shore). Soldiers lined up on the beach under a flag pole, the Gulf and the Edom
  mountains behind.
  - File page: https://commons.wikimedia.org/wiki/File:Eilat_(997008136794105171).jpg
  - Original: https://upload.wikimedia.org/wikipedia/commons/2/2f/Eilat_%28997008136794105171%29.jpg (661 x 953,
    SHA-1 a892acf21fdcd9cbe3efda6dfbcb22819a6ef65f)
  - Licence: CC BY 4.0 (`{{cc-by-4.0|1={{Meitar Collection/license credit line}}}}`), VRTS ticket 2021092910005788.
    NLI record 997008136794105171, ARC. 4* 2068 / 418.
  - Credit: "Benno Rothenberg / Meitar Collection / National Library of Israel / The Pritzker Family National
    Photography Collection · CC BY 4.0 · graded and cropped".
  - Portrait original: a 9:4 crop of a 661 px wide frame needs upscaling, as `idf_founding` did. NLI dates it only
    "March 1949", so the alt text should say "March 1949", not 10 March.
  - Others in the same set (Eilat, March 1949, Rothenberg, CC BY 4.0): 997008136794005171 (two soldiers at the police
    post), 997008137356105171 (the flag over the post).

Not usable under the policy:
- "Final Armistice Agreement being signed by representatives of Syria and Israel, in a tent at Mahayanim", Photo
  Division, Government of India, August 1949. Licence `{{PD-India}}` only: refused.
  https://commons.wikimedia.org/wiki/File:Final_Armistice_Agreement_being_signed_by_representatives_of_Syria_and_Israel,_in_a_tent_at_Mahayanim.jpg
- "Raising the Ink Flag at Umm Rashrash (Eilat)", Micha Perry, 10 March 1949: CC BY-SA 3.0, refused. A second copy
  credited to an unknown author is tagged public domain without a US basis: refused.
- The return to Mishmar HaYarden, November 1949 (PikiWiki, Rudolf Jonas): tagged `{{PD-author|Israel Preker}}` by the
  uploader, who is not the photographer. Same problem as Yeruham (section 4).

No Meitar photograph of the Syrian signing or of Mahanayim in July 1949 was found (Commons searches for
"Mahanayim 1949", "Syria armistice 1949", "Mishmar HaYarden 1949").

## 2. Absorption: a dated milestone

The GDD objective is "Provide immigrants with housing and employment". No single dated event marks that as done in
1948–51: the ma'abarot peaked at the end of 1951 and the last closed around 1963 (below). The options are a
population milestone (clear date, but it measures arrival) or a housing milestone (fits the objective, weaker date).

### Option A: Jewish population passes one million (recommended for a clean date)

- JTA, "Jewish Population of Israel Peaches [sic] 1,000,000 with Arrival of New Tripoli Immigrants", daily bulletin
  of 22 November 1949: with the arrival "yesterday" of 663 immigrants from Tripoli the Jewish population was 1,000,000,
  said Yaacov Blomberg, acting director of the Ministry of Immigration; 321,991 newcomers since 15 May 1948.
  https://www.jta.org/archive/jewish-population-of-israel-peaches-1000000-with-arrival-of-new-tripoli-immigrants,
  accessed 2026-10-13.
- JTA, "Israel Marks 'Ingathering of Exiles Day'; Arrival of Second Million Jews Anticipated", 20 December 1949: "Yom
  Kibbutz Galuyoth" was "celebrated today throughout Israel" for the Jewish population passing 1,000,000.
  https://www.jta.org/archive/israel-marks-ingathering-of-exiles-day-arrival-of-second-million-jews-anticipated,
  accessed 2026-10-13.
- The New York Times, 21 November 1949, p. 5, "Israel Jewish Population Increases to 1,000,000": known only from a
  dealer's listing (https://www.rarenewspapers.com/view/566565, accessed 2026-10-13). Unverified.
- [C] The day: the announcement is dated 21 or 22 November 1949 (JTA bulletin date vs NYT date), and the celebration
  20 December 1949 (JTA). A poster-archive note found in search gives 19 December 1949 (unverified, not read).
  Recommendation: use the celebration, 20 December 1949, citing JTA; or the announcement, "November 1949".
- Hebrew Wikipedia ("מלחמת העצמאות") also says the Jewish population reached about 1,000,000 by the end of the
  armistice signings in July 1949. That is a rounder statement than the JTA report and does not give a day.

Photograph: none found of the celebration or of the Tripoli arrival under a usable licence (Commons searches for
"Aliyah" and "Immigrants" with November–December 1949 dates). A generic arrival photograph would need the owner's
approval.

### Option B: the first ma'abara (fits "housing and employment")

- Hebrew Wikipedia "מעברה" quotes Davar of 23 May 1950: "last week" the first ma'abara was set up on the slopes of
  southern Kesalon, its residents taken out of the immigrant camps for forestry work, living in tents with their
  families. It also gives Eshkol's letter of 21 April 1950 as the first use of the word.
  https://he.wikipedia.org/wiki/מעברה, accessed 2026-10-13.
- English Wikipedia "Ma'abarot": the first ma'abara, at Kesalon, "became functional in May 1950 when 150 families were
  settled there"; by the end of 1951, 227,000 people lived in 123 or 125 ma'abarot; the last closed around 1963.
  https://en.wikipedia.org/wiki/Ma'abarot, accessed 2026-10-13.
- Date: only "the week before 23 May 1950" (about 14–20 May 1950). No day found. The existing event
  `maabarot_policy` already covers this decision (window 1 March to 1 June 1950).
- Sensitivity: en.wiki says Kesalon was "built atop the emptied Palestinian village of Kasla". Under D-F1 the card
  would need to state that factually or avoid it.

Photographs (usable): Benno Rothenberg's series "מעברה בדרך לירושלים" (a ma'abara on the road to Jerusalem), NLI date
June–August 1950, CC BY 4.0, VRTS ticket 2021092910005788. The repo already uses 997008136927805171 for
`maabarot_policy`. Two others from the series:
- Families arriving by lorry among the tents:
  https://commons.wikimedia.org/wiki/File:Ma%27abara_(997008136927205171).jpg, original
  https://upload.wikimedia.org/wikipedia/commons/d/d0/Ma%27abara_%28997008136927205171%29.jpg (1322 x 1305, SHA-1
  ff457454a83c3ca0d8ef20934db1dc8ea1fec176).
- Residents laying out timber for huts:
  https://commons.wikimedia.org/wiki/File:Ma%27abara_(997008136928005171.jpg, original
  https://upload.wikimedia.org/wikipedia/commons/a/a9/Ma%27abara_%28997008136928005171.jpg (1314 x 1297, SHA-1
  5e994e17b1554fa0fb536e0b72ebd79ed772fae5).
- Credit for both: "Benno Rothenberg / Meitar Collection / National Library of Israel / The Pritzker Family National
  Photography Collection · CC BY 4.0 · graded and cropped". The NLI does not say which ma'abara, so the alt text must
  not say Kesalon.

### Option C: end of Operation Magic Carpet

- English Wikipedia "Operation Magic Carpet (Yemen)": the airlift ran from 16 December 1948 to 24 September 1950,
  "when the last two aircraft reached Lydda Airport with 177 passengers"; about 49,000 people.
  https://en.wikipedia.org/wiki/Operation_Magic_Carpet_(Yemen), accessed 2026-10-13.
- A clean date, but it marks arrival from one country, not housing or work. Photograph not searched in depth.

Not found: a dated closure of the last immigrant camp (machane olim). He.wiki "מחנה עולים" says the camps were turned
into ma'abarot from March 1950 and gives no closing day.

## 3. The Tel Aviv–Haifa railway

### Finding

The coastal railway between Tel Aviv and Haifa opened in 1953, after the MVP window. Inside the window, the Haifa–Tel
Aviv rail link was restored on 20 September 1949 over the old inland (Eastern) line, by a new bypass west of
Tulkarm, a reversal at Rosh HaAyin and a 7 km extension of the Yarkon line to a new Tel Aviv North station.

### Timeline

| Date | Event | Sources |
|---|---|---|
| 1948 | The inland line from Haifa south ran through Tulkarm, which stayed outside Israel; Tel Aviv had no rail link north, only to Lydda | he.wiki "תחנת הרכבת בני ברק"; en.wiki "Eastern Railway (Israel)" |
| about January 1949 | Work starts on a new coastal line from Remez junction (near Pardes Hanna) to Tel Aviv; after the armistice it is slowed in favour of reopening the inland line | he.wiki "מסילת החוף" |
| 4 January 1949 | Combined rail and bus service: train Haifa–Hadera, bus Hadera–Tel Aviv; opened in the presence of the transport minister David Remez | he.wiki "רכבת ישראל". Supporting: Commons file "IL steam engine.jpg" is captioned the first train from Haifa to Hadera, 4 January 1949 (photo not usable, see below) |
| 7 August 1949 | Tel Aviv–Jerusalem line reopened (ceremonial train) | he.wiki "רכבת ישראל" |
| **20 September 1949** | **Haifa–Tel Aviv passenger line opens**: the bypass west of Tulkarm, the Eastern line to Rosh HaAyin (trains reverse there), the Yarkon line extended 7 km to the new Tel Aviv North station (now Bnei Brak). Tel Aviv's old station is renamed Tel Aviv South the same day | he.wiki "המסילה המזרחית ההיסטורית", "מסילת הירקון", "תחנת הרכבת בני ברק"; en.wiki "Yarkon Railway", "Bnei Brak–Ramat HaHayal railway station" |
| 25 September 1949 | Carmel Newsreel II-029: "Inauguration of Tel Aviv-Haifa Train Route in the North Tel Aviv Station", with David Remez, Golda Meir, Yosef Sprinzak; the newsreel's issue date, not the event date | Steven Spielberg Jewish Film Archive, https://jfc.org.il/en/news_journal/27569-2/92813-2/ |
| late 1951 | Track laid from Remez junction to Hadera on the new coastal line | he.wiki "מסילת החוף" |
| April 1953 (unofficial), **14 May 1953** (official) | Coastal line opens; it replaces the inland route between Hadera and Tel Aviv. en.wiki: "opened in May 1953" | he.wiki "מסילת החוף"; en.wiki "Coastal railway line, Israel" |
| November 1954 | Tel Aviv Central station opens | en.wiki "Coastal railway line, Israel" |

Further details and conflicts:
- JTA, 12 August 1949 (seen only as a search summary of a reprint in the Detroit Jewish News; the page is behind a
  bot check): regular Tel Aviv–Haifa service was to resume on 21 August for freight, with passenger trains "about the
  middle of September" routed via Rosh HaAyin. Unverified, and a plan, not a report. It agrees with 20 September.
- Carmel Newsreel II-014, 12 March 1949, "New Israel Railways Passenger Train Line from Tel Aviv to Haifa": the
  catalogue describes an animated map of a route "between Tel Aviv and Hadera via Kfar Vitkin" and a train running
  north to Haifa. [C] This does not match the 20 September 1949 opening. It may show the 4 January rail-and-bus service
  or the planned coastal route. Unresolved; the film is behind a login.
  https://jfc.org.il/en/news_journal/27554-2/105753-2, accessed 2026-10-13.
- [C] The coastal line's official day (14 May 1953) is from he.wiki only. en.wiki says May 1953 without a day.

URLs, all accessed 2026-10-13:
- https://he.wikipedia.org/wiki/מסילת_החוף
- https://he.wikipedia.org/wiki/המסילה_המזרחית_ההיסטורית
- https://he.wikipedia.org/wiki/מסילת_הירקון
- https://he.wikipedia.org/wiki/תחנת_הרכבת_בני_ברק
- https://he.wikipedia.org/wiki/רכבת_ישראל
- https://en.wikipedia.org/wiki/Coastal_railway_line,_Israel
- https://en.wikipedia.org/wiki/Eastern_Railway_(Israel)
- https://en.wikipedia.org/wiki/Yarkon_Railway
- https://en.wikipedia.org/wiki/Bnei_Brak–Ramat_HaHayal_railway_station

Recommendation for D-B3: if the objective must be completable "earlier than history" inside the MVP, use 20 September
1949, worded as "Haifa–Tel Aviv rail service restored" (not "coastal railway completed"). If the owner wants the
coastal line itself, its date (14 May 1953) is after the MVP ends, and the timing bonus would compare against a date
the game never reaches. A primary source for 20 September 1949 (Davar or HaBoker of 21 September 1949 in the NLI's
historical newspapers) should be added before the card is built: all the sources above are Wikipedia, plus the
newsreel catalogue.

### Photographs

Usable (not of the opening day): Boris Carmi's "רכבת ישראל" (Israel Railways) series, NLI date 1949, CC BY 4.0, VRTS
ticket 2021092910005788. 20 frames: locomotives, crews, conductors, a view from a train on the Jerusalem line.
- A conductor with a flag beside the coaches at a platform:
  https://commons.wikimedia.org/wiki/File:Israel_Railways_(997009453600005171.jpg, original
  https://upload.wikimedia.org/wikipedia/commons/4/47/Israel_Railways_%28997009453600005171.jpg (2000 x 1975, SHA-1
  3cd1c8ad099c2976db9c49054cce0442be57b38b).
- A tender lettered "מדינת ישראל" over "P R" (Palestine Railways), a picture of the handover:
  https://commons.wikimedia.org/wiki/File:Israel_Railways_(997009453599305171.jpg, original
  https://upload.wikimedia.org/wikipedia/commons/9/99/Israel_Railways_%28997009453599305171.jpg (1975 x 2000, SHA-1
  b5b546c2a4cf358b29bd47802ecf71f70483d8ac).
- Credit: "Boris Carmi / Meitar Collection / National Library of Israel / The Pritzker Family National Photography
  Collection · CC BY 4.0 · graded and cropped".
- The NLI gives no place or day, so the alt text must say "Israel Railways, 1949" and not tie the frame to 20
  September.

Not usable under the policy:
- "Tel Aviv North Railway Station.jpg", Ernest Talhoffer, taken 20 September 1949, Israel Railway Museum archive:
  `{{PD-Israel}}` only. It is the exact event; the State Archives hold a copy (linked from the file page). If the owner
  wants it, the State Archives' terms for that file would need checking. I have not checked them.
  https://commons.wikimedia.org/wiki/File:Tel_Aviv_North_Railway_Station.jpg
- "The Haifa train leaving for Tel Aviv from Atlit railway station 1949 D846-029.jpg", Teddy Brauner, 20 September
  1949: a GPO photograph (`{{GPO Israel-image|id=D846-029}}`), `{{PD-Israel}}`. Refused twice over.
- "IL steam engine.jpg", 4 January 1949, unknown author, scanned from Cotterell's *The Railways of Palestine and
  Israel*: `{{PD-Israel}}` only.

## 4. First new yishuv

### Yeruham (current lead): the photograph question

Founding date [C]:
- English Wikipedia "Yeruham": "founded on 9 January 1951 as Kfar Yeruham".
  https://en.wikipedia.org/wiki/Yeruham, accessed 2026-10-13.
- Hebrew Wikipedia "ירוחם": the Jewish settlement began on ד' בשבט ה'תשי"א (4 Shevat 5711), when Amnon Zair of Gan
  Shmuel set up a ma'abara for immigrants from Romania called Tel Yeruham, later Kfar Yeruham.
  https://he.wikipedia.org/wiki/ירוחם, accessed 2026-10-13. 4 Shevat 5711 is Thursday 11 January 1951 (converted with
  the `convertdate` library; 9 January 1951 is 2 Shevat).
- Srugim, "היום בהיסטוריה: מעברת ירוחם עולה על הקרקע", 6 January 2022 (4 Shevat 5782), the same Hebrew date.
  https://www.srugim.co.il/634849, accessed 2026-10-13.
- A Shimur (Society for Preservation of Israel Heritage Sites) presentation, seen only in a search summary, gives
  "Thursday 4 Shevat 5711 (4.1.1951)". 4 January 1951 was 26 Tevet, so its Gregorian date looks miscomputed.
  Unverified.
- So: 9, 11 or 4 January 1951. The Hebrew date in two Hebrew sources gives 11 January 1951.
- Note for the card: Yeruham began as a ma'abara, a transit camp, rather than a planned yishuv. He.wiki also says the
  site was an area where Bedouin of the Azazma moved (no depopulated village is named).

The 1951 photographs: three on Commons, all by Werner Braun, uploaded through PikiWiki by Israel Preker, source "the
Yeruham archive via the KKL archive":
- "128639 yeruham PikiWiki Israel.png", June 1951, installing an electric line.
- "129769 yeruham PikiWiki Israel.png", July 1951, installing an electric line.
- "129770 yeruham PikiWiki Israel.png", July 1951, paving the road from Beersheba to Tel Yeruham.
- All three carry `{{PD-author|Israel Preker}}`. The template's text is "I, the copyright holder of this work,
  release this work into the public domain". The person releasing the photographs is the uploader, not the
  photographer. Nothing on the pages shows that Preker holds the copyright, or that the KKL archive or Braun's heirs
  released them.

Is a 1951 photograph public domain anyway?
- In Israel, yes. Under the Copyright Act 2007, s. 78(i), photographs made before the Act came into force (25 May
  2008) keep the former law's term (the Mandate-era Copyright Ordinance, which applied the UK Copyright Act 1911):
  per Commons, protection "lasts until 1 January of the 51st year after the creation of the photograph". I did not
  re-read the 1911 Act itself. A 1951 photograph went out of copyright in Israel on 1 January 2002.
- In the US, no. The URAA restored US copyright, from 1 January 1996, in foreign works still protected in their
  source country on that day. A 1951 photograph was still protected in Israel on 1 January 1996, so its US
  copyright was restored: 95 years from publication if it was published (to the end of 2046), longer if not. This is
  the reason the repo refuses PD-Israel-only files (decision D-P in docs/event-images.md).
- Source for the Israeli terms: Wikimedia Commons, "Copyright rules by territory/Israel" (the 2007 Act ss. 38, 42,
  43, 78(i); URAA date 1 January 1996),
  https://commons.wikimedia.org/wiki/Commons:Copyright_rules_by_territory/Israel, accessed 2026-10-13. The Act itself:
  WIPO Lex, http://www.wipo.int/edocs/lexdocs/laws/en/il/il033en.pdf (cited by that page; not re-read today).
- Werner Braun lived 12 June 1918 – 25 December 2018 (en.wiki "Werner Braun (photojournalist)"; Wikidata Q18120775),
  so life + 70 does not help either.
- The only route to "public domain with a US basis" is a valid release by the copyright holder: Braun's heirs, or the
  KKL (JNF) if Braun took them as its employee or on its commission (2007 Act ss. 34–35). Whether he did is
  unverified.

Conclusion: under CONTENT_POLICY §4 the three Yeruham photographs are not usable. They are effectively PD-Israel files
with an unsupported PD-author tag. For the owner (ruling 4, #378): refuse them, unless the KKL archive or the
Braun estate confirms a release in writing. GPO is not an alternative: GPO photographs are refused by the same
section.

Usable Yeruham photographs exist only from June 1953 onward (Benno Rothenberg, Meitar, CC BY 4.0, e.g.
https://commons.wikimedia.org/wiki/File:Yeruham_(997008136362205171).jpg). They would show the place two years after
its founding.

### Alternatives with clear licences

**Tifrah (recommended).** A moshav in the north-western Negev.
- Founded 27 September 1949 ("אור לה' בתשרי ה'תש"י", the eve of 5 Tishrei 5710; 5 Tishrei 5710 is 28 September 1949,
  checked with `convertdate`) by Holocaust survivors from Romania and Hungary: he.wiki "תפרח",
  https://he.wikipedia.org/wiki/תפרח, accessed 2026-10-13.
- [C] en.wiki "Tifrah": "established in 1950". https://en.wikipedia.org/wiki/Tifrah, accessed 2026-10-13. Hamichlol
  (search summary only) says 1949. The NLI's own title for the photographs below is "the founding of moshav Tifrah in
  the Negev" on 2 December 1949, which supports 1949.
- A second independent source for the day is still needed (Phase 1 plan, Task 5.0, criterion 2): try the Merhavim
  regional council, the Poalei Agudat Israel moshav movement, or Davar/HaTzofe of late September 1949.
- Neither article names a depopulated Arab locality at the site. Not checked against a gazetteer.
- Photograph: Benno Rothenberg, NLI title "הקמת מושב תפרח בנגב" (the founding of moshav Tifrah in the Negev), taken
  2 December 1949. A new block house with washing on the line and tents behind.
  - https://commons.wikimedia.org/wiki/File:Tifrah_997008137656705171.jpg
  - Original https://upload.wikimedia.org/wikipedia/commons/f/f7/Tifrah_997008137656705171.jpg (653 x 681, SHA-1
    e592a5bea461b5e1c36b81de9d332e9a3bfe5149). Small: a 9:4 header crop needs upscaling.
  - CC BY 4.0, VRTS ticket 2021092910005788, NLI ARC. 4* 2068 / 1342. Credit: "Benno Rothenberg / Meitar Collection
    / National Library of Israel / The Pritzker Family National Photography Collection · CC BY 4.0 · graded and
    cropped".
  - Four more frames from the same day: 997008137656605171 and 997008137656805171 (families outside the new houses;
    close groups, so check the portrait rule), 997008137656905171 (a tractor and the line of tents), 997008137657205171
    (two men in a doorway).

**Mefalsim.** A kibbutz in the western Negev founded in 1949 by Habonim Dror immigrants, mostly from Argentina and
Uruguay; about a year later it moved from its first site, a hill facing the Gaza Strip border, after frequent fire
from Beit Hanoun.
- en.wiki "Mefalsim" (https://en.wikipedia.org/wiki/Mefalsim) and he.wiki "מפלסים"
  (https://he.wikipedia.org/wiki/מפלסים), accessed 2026-10-13: year only.
- Day: 12 June 1949, from the Hebrew Wikipedia article on a founder, Yosef Perlmutter, seen only in a search summary
  via Hamichlol. Unverified.
- Photograph: Boris Carmi, NLI title "ימיו הראשונים של קיבוץ מפלסים" (the first days of kibbutz Mefalsim), June 1949.
  Two men putting up a barbed-wire fence on open ground.
  - https://commons.wikimedia.org/wiki/File:Mefalsim_(997009452698205171.jpg
  - Original https://upload.wikimedia.org/wikipedia/commons/e/e3/Mefalsim_%28997009452698205171.jpg (1528 x 1566,
    SHA-1 f4d82b8f08f810081145eec13d24ac09f672873f). CC BY 4.0, VRTS ticket 2021092910005788, NLI ARC. 4* 2069 /
    32475.
  - Credit: "Boris Carmi / Meitar Collection / National Library of Israel / The Pritzker Family National Photography
    Collection · CC BY 4.0 · graded and cropped".
  - Eleven frames in the series (997009452697705171 to 997009452698705171): huts, unpacking crates, a crawler
    tractor, laying pipe.
- Sensitivity: Mefalsim was among the communities attacked on 7 October 2023. The owner should know before choosing
  it.

**Nahal Oz.** The first Nahal outpost, founded 26 July 1951 ("נחלאים א' – מול עזה") on the land of the old Be'erot
Yitzhak, with Moshe Dayan at the ceremony (he.wiki "נחל עוז", https://he.wikipedia.org/wiki/נחל_עוז, accessed
2026-10-13; en.wiki: 1951).
- Photographs: 30 Boris Carmi frames on Commons, CC BY 4.0, with the NLI date "1950". [C] 1950 is before the founding.
  The frames show gardens, finished buildings and later dress, so "1950" looks like a decade estimate. Not usable as
  founding-era pictures without the NLI record's correction.
- Sensitivity: Nahal Oz was attacked on 7 October 2023.

Rejected for now:
- Yotvata (Nahal outpost at Ein Radian, ceremony 30 October 1951, he.wiki "יטבתה"): Boris Carmi frames on Commons are
  dated "1950" [C], before the founding, and show a developed settlement. Same problem as Nahal Oz.
- Kissufim (1951): PikiWiki photographs by an unknown author, tagged public domain without a US basis.
- Ein Yahav (Nahal outpost, 1951): one PikiWiki photograph by Werner Braun, April 1952, tagged PD-author by the
  uploader. Same problem as Yeruham.
- HaOn (August 1949; moved October 1949 "near the depopulated Palestinian village of As-Samra", en.wiki), Ma'agan
  (1949, "near the Arab village of Samakh, which was depopulated", en.wiki), and the 1949 Meitar sets for Yas'ur,
  Tirat Yehuda, Elkosh, Kidron and Masmiyya: sites tied to depopulated Arab localities. They fail the plan's criterion
  4 unless the owner accepts a card that says so.

Recommendation for D-A4 / #378: Tifrah, 27 September 1949, with the 2 December 1949 Rothenberg photograph, once a
second source for the day is found. Mefalsim is the fallback, with a larger photograph but a weaker date.

## Licence notes for the reviewer

- **"Meitar Collection (review needed)".** Most Meitar files carry this Commons category, among them the Uvda and
  railway picks and 8 of the 24 photographs the game already ships (checked through the API today). It has no
  description page. A Commons administrators' noticeboard archive (Archive 96) discusses it in the context of
  renaming about 50,000 files. It reads as a maintenance category, not a licence doubt: every file above carries the
  same `{{cc-by-4.0}}` template and VRTS ticket 2021092910005788. docs/event-images.md records that the `ten_days`
  pick avoided one such series. The tag has not blocked the files already shipped.
- **NLI dates.** The NLI dates some sets to the decade or to a period (Nahal Oz, Yotvata "1950"; the ma'abara
  "June–August 1950"). Alt text should use only the month or year the NLI gives, as docs/event-images.md asks.
- **GPO.** GPO photographs are refused by CONTENT_POLICY §4 whatever the GPO's own terms are, so I did not look into
  those terms.
- **Next steps if a card is chosen:** add the `event-images.yaml` row, the `ASSETS.yaml` row (`status: owner-verify`)
  and the `NOTICE` credit, as docs/event-images.md describes. Phase 1 PR 7 (#380) covers this for the yishuv card.

## Open questions for the owner

1. War card (D-B2): 20 July 1949 as the date with a picture from another event (Uvda, March 1949), or the Uvda date
   (10 March 1949) with its own picture?
2. Absorption (D-B4): a population milestone (20 December 1949, clear date, measures arrival), the first ma'abara (May
   1950, matches "housing and employment", no exact day, Kesalon site sensitivity), or Magic Carpet (24 September
   1950)?
3. Railway (D-B3): accept 20 September 1949 ("Haifa–Tel Aviv service restored") as the in-window event, or keep the
   coastal line (14 May 1953) and accept that it falls after the MVP?
4. Yeruham photographs (ruling 4): refuse, as this note recommends, or ask the KKL archive for a written release?
5. Yishuv (D-A4): Tifrah, Mefalsim or another site? Both alternatives are 1949 Negev settlements; Mefalsim also has
   a 7 October 2023 association.
