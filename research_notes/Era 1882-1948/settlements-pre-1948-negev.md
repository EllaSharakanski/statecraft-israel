# Jewish localities founded in the Negev before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its Negev region. The dated settlements file and `tools/settlement_hexes.py` stay with
#61, and rows with a rename or an abandonment date need the owner's sensitivity review before they
enter the game; none is proposed here.

## How it was done

- Every page was fetched on 23 October 2026 with the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`. A search tool
  was used only to find pages; every page cited was then fetched and read directly.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024,
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23),
  columns `שנת ייסוד` and `קואורדינטות`. The twelve-digit coordinate is read as an Israel
  Transverse Mercator easting and northing of six digits each and converted with `pyproj`
  (EPSG:2039 to EPSG:4326), the same reading `settlements-1949-51.md` uses. That reading is the
  earlier note's, not stated in the file. Coordinates below are the CBS values to five decimals.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, in the Ben-Yehuda
  Project edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Columns: type of
  settlement, name, geographic region, date of founding, affiliation, origin of the name. As
  `settlements-1949-51.md` records, the column does not say whether the date is the going-onto-the
  -land day or a later recognition, and it sometimes differs from other sources by a day or more.
- **Hebrew Wikipedia** was read through the MediaWiki API as the wikitext of the current revision,
  cited by `oldid`. It is a lead and a cross-check, not an authority; facts resting on it say so.
- Hebrew dates are reported as the article gives them. **No Hebrew-date conversion was done here**,
  so where a source gives only a Hebrew date it is quoted and not turned into a civil date.

**Tags**, one per fact, as in `settlements-1949-51.md`
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
  The CBS localities file is treated as that primary source for a founding year and a coordinate.
- **[S] single source**: one source, or several that plainly copy one another. Hebrew Wikipedia is
  a lead and a cross-check, not an authority, so a fact resting on it alone is [S] however plainly
  the article states it. Gvati's appendix alone is also [S]: it is one secondary compilation.
- **[C] conflicting**: sources disagree. The conflict and the sources on each side are stated.
- **[GAP]** not found.
- **computed here** is arithmetic or a conversion done for this note, not a printed figure.
- **our reading** marks an interpretation, and is not attributed to the source.

Every page cited was opened and read, including each Wikipedia revision at the `oldid` given and
each CBS and Gvati row; that is the floor for citing anything here, not what [V] means.

## What counts as the Negev here

The boundary is not clean and the sources do not agree on one, so this note states the rule it used
rather than implying there is a settled answer.

Included: every Jewish locality founded before 15 May 1948 that CBS places in the **Be'er Sheva
sub-district**; the **eleven points of October 1946 as a set**, including the two that CBS places
in the Ashkelon sub-district; and the three northern kibbutzim that the sources themselves call the
first in the Negev.

Excluded, as belonging to a neighbouring region note rather than this one: Negba (CBS 1939), Gat
(CBS 1941), Gevaram (CBS 1942), Yad Mordechai (CBS 1943), Nitzanim (CBS 1943) and Kfar Menachem
(CBS 1937). All are in the Ashkelon sub-district and in the southern coastal plain or the Judean
foothills. **Our reading:** they belong with Lachish and the coastal plain, not the Negev.

Two of the eleven points sit awkwardly on that line and are kept in because the operation defines
the set, not geography: **Gal-On**, which Gvati's appendix puts in **Lachish**, and **Kedma**, which
went up from Kfar Menachem. Both are flagged in their rows.

## 1. The three mitzpim of 1943

The Jewish National Fund established three observation and experiment stations in the Negev in
1943. All three sources agree there were three and that they were the first.

### Gvulot
- CBS: locality code **352**, `שנת ייסוד` **1943**, **31.21084, 34.46549**, Be'er Sheva
  sub-district. [V]
- Gvati, kibbutzim section: `גְּבוּלוֹת | הנגב הצפוני | 12.5.43 | הקיבוץ הארצי`. [S]
- he.wiki `גבולות`, oldid **43752363**: "עלה לקרקע ב-12 במאי 1943 כתחנת ניסיון חקלאית (מצפה) -
  הראשון משלושת המצפים בנגב". [S] Gvati and he.wiki agree on **12 May 1943**, so the date is [V].
- **[C] within the same he.wiki article.** A later paragraph says "הקיבוץ הוקם בשנת 1946 על ידי חברי
  גרעין אלמגור". Both readings are in the article; the 1946 sentence is about the founding group of
  the present kibbutz, not the mitzpe. **Our reading:** 12 May 1943 is the date for the mitzpe and
  the one to use; the article does not reconcile the two itself.
- Relocation: "לאחר מלחמת העצמאות הועתק היישוב למקומו הנוכחי, כקילומטר צפונית מזרחית למצפה". [S]
  No day given. [GAP]
- Departure base for Urim and Nirim in October 1946. [V]

### Revivim
- CBS: code **354**, `שנת ייסוד` **1943**, **31.04397, 34.72178**, Be'er Sheva. [V]
- Gvati: `רְבִיבִים | נגב צפוני | 20.7.43 | הקיבוץ המאוחד`. [S]
- he.wiki `רביבים`, oldid **43556273**: "הקיבוץ עלה לקרקע ב-7 ביולי 1943, בשם תל צופים כאחד משלושת
  המצפים". [S]
- **Rename: founded as תל צופים (Tel Tzofim).** [S]
- **[C] on the day: Gvati 20 July 1943 against he.wiki 7 July 1943.** The article also quotes a
  founding scroll dated "כ"ט במנחם-אב" of 5703, a third value. No conversion was done here, so the
  Hebrew date is left as the article gives it. All three agree on 1943.
- Relocation: "לאחר המלחמה הקיבוץ עבר ממקומו המקורי אל האתר בו הוא נמצא היום. אבן הפינה של היישוב
  החדש הונחה ב-1 ביוני 1949". [S]

### Beit Eshel
- **Not in the CBS localities file.** No row matches `בית אשל` in the 1,490 rows. [V, checked] It was
  not re-established on its site.
- **No row in Gvati's appendix** either. [V, checked]
- he.wiki `בית אשל`, oldid **42228944**. Infobox: `סוג יישוב=יישוב לשעבר`, `תאריך ייסוד=1943`,
  `סיבת נטישה=מלחמת העצמאות`, `תאריך נטישה=אוקטובר 1948`, `יישובים יורשים=באר שבע`, and explicit
  coordinates `רוחב=31.233472 | אורך=34.813272`. [S] These are written in the article, not drawn
  from the CBS bot template, so they are the article's own figure.
- Body: "היישוב עלה לקרקע באוגוסט 1943, כ-2 ק"מ מזרחית לבאר שבע". [S] **Month only, no day.** [GAP]
- **[C] on the abandonment date.** The infobox says `תאריך נטישה = אוקטובר 1948`. The body says the
  place held out "עד כיבוש באר שבע על ידי צה"ל" in October 1948 and that the members decided only
  afterwards to move to the Jezreel valley and found Hayogev. Gvati's narrative agrees with the
  body: the defenders "החזיקו מעמד עד סוף המלחמה, ורק אחריה התיישבו בעמק במושב היוגב", and he dates
  Hayogev to July 1949. **Our reading:** the infobox date is the month Be'er Sheva was taken, not
  the month the site was given up, and the two are being conflated. Both values are given; neither
  is used as the abandonment date here.
- Unlike the other two mitzpim, its people wanted a moshav: "בניגוד לקיבוצים רביבים וגבולות, תושבי
  בית אשל שאפו להקים במקום מושב עובדים". [S]

## 2. The northern kibbutzim that the sources call the first in the Negev

These three are in the Ashkelon sub-district by CBS, but the sources describe them as the Negev's
first, so they are in this note rather than a coastal one.

### Ruhama
- CBS: code **362**, `שנת ייסוד` **1944**, **31.49799, 34.70613**, Ashkelon. [V]
- Gvati: `רוּחָמָה | הנגב הצפוני | 19.3.44 | הקיבוץ הארצי`. [S]
- he.wiki `רוחמה`, oldid **43556283**: "היה זה היישוב היהודי הראשון בנגב בעת החדשה", of the 1911
  venture. [S] **Rename:** "תחילה נקרא היישוב ג'ממה, כשם הכפר הערבי הסמוך", later changed to
  Ruhama. [S]
- **Three abandonments before the present kibbutz**, all in the article: in the First World War
  ("היישוב ננטש ונבזז על ידי בדווים"), in 1929, and a third time in the 1936 to 1939 disturbances
  ("נעזבה רוחמה בשלישית; בתיה נהרסו ומטעיה נעקרו"). [S] No day for any of the three. [GAP]
- **[C] on the founding of the present kibbutz.** The article gives both: "ב-1 בדצמבר 1943 עלו חברי
  השומר הצעיר, חידשו את הבאר והחלו להקים גדר ואת מבני הקיבוץ, טקס העלייה הרשמית לקרקע התבצע במרץ
  1944 ומהווה את ראשיתו של היישוב הנוכחי". [S] So 1 December 1943 is the arrival and March 1944 the
  official ascent. CBS's 1944 and Gvati's 19 March 1944 both fall on the second. **Our reading:**
  Gvati's 19 March 1944 is the best day-level value, and the December 1943 arrival should be
  recorded beside it rather than replaced by it.
- Departure base for Mishmar HaNegev and Shoval in October 1946. [V]

### Dorot
- CBS: code **336**, `שנת ייסוד` **1941**, **31.50672, 34.64506**, Ashkelon. [V]
- Gvati: `דוֹרוֹת | דרום | 18.12.41 | האיחוד`. [S]
- he.wiki `דורות`, oldid **43555697**: "דורות היה הקיבוץ הראשון בנגב בהיווסדו בשנת 1941", and
  "היישוב הוקם בחנוכה, שנת ה'תש"ב (1941) על קרקעות שנרכשו מתושבי הכפר הסמוך הוג'". [S]
- **Day: Gvati only (18 December 1941).** he.wiki gives the Hebrew festival, not a civil day, and no
  conversion was done here. [S] for the day, [V] for the year in all three.

### Nir Am
- CBS: code **348**, `שנת ייסוד` **1943**, **31.51966, 34.57977**, Ashkelon. [V]
- Gvati: `נִיר-עָם | דרום | 12.1.43 | האיחוד`. [S]
- he.wiki `ניר עם`, oldid **44033578**: "הקיבוץ הוקם בט"ו בשבט ה'תש"ג (21 בינואר 1943)". [S]
- **[C] on the day: Gvati 12 January 1943 against he.wiki 21 January 1943.** Both are January 1943.
  The he.wiki value is tied to a Hebrew date the article converts itself; Gvati's is a bare column
  value. Both are given.
- The article notes the Nir Am wells fed the 1947 water line to the eleven points: "קידוחים באזור
  ניר עם שימשו מקור לקו המים שהונח בשנת 1947 וסיפק מים ל-11 הנקודות בנגב". [S]

## 3. The eleven points, the night of 5 to 6 October 1946

he.wiki `11 הנקודות`, oldid **43974883**, opens: "11 הנקודות הן אחד-עשר יישובים יהודיים בנגב
הצפוני, אשר הוקמו במבצע התיישבות גדול במוצאי יום הכיפורים ה'תש"ז", on "הלילה שבין 5 ל-6 באוקטובר
1946", at the initiative of the Jewish Agency. [S]

**A correction worth stating plainly.** The operation is often dated 11 October 1946. The "11" is
the count of points, not a date: the article dates it to the night of 5 to 6 October 1946, and
Gvati's appendix independently gives **6.10.46** for eight of the eleven. Nothing read for this note
supports 11 October.

The article's own list of eleven, with the site name it gives in brackets and the base each party
left from. [S] In the table below, the CBS code, year and coordinate are [V] (CBS is the primary
source for those), and the Gvati date column is [S] on its own but [V] for the eight localities
where it agrees with the article.

| Locality | site name | departure base | CBS code | CBS year | CBS lat, lon | Gvati date |
|---|---|---|---|---|---|---|
| Urim | Grein | Gvulot | 403 | 1946 | 31.30419, 34.52263 | 6.10.46 |
| Be'eri | Nakhabir | Be'erot Yitzhak | 399 | 1946 | 31.42385, 34.49086 | 6.10.46 |
| Gal-On | Khirbet Musajat | Gat | 393 | 1946 | 31.63377, 34.84758 | 6.10.46 |
| Hatzerim | Qalta | Beit Eshel | 397 | 1946 | 31.24010, 34.71344 | 6.10.46 |
| Kfar Darom | Deir al-Balah | Be'erot Yitzhak | not in file | n/a | n/a | no row |
| Mishmar HaNegev | Bir Mansura | Ruhama | 395 | 1946 | 31.36351, 34.71701 | 6.10.46 |
| Nevatim | on Madsus land | Beit Eshel | 396 | 1946 | 31.22316, 34.88160 | 6.10.46 |
| Nirim | on Dangur land | Gvulot | 602 | **1949** | 31.33597, 34.39494 | 6.10.46 |
| Kedma | Tel Turmus | Kfar Menachem | 392 | **blank** | 31.70096, 34.77501 | no row |
| Shoval | Bir Zabala | Ruhama | 394 | 1946 | 31.41449, 34.74384 | 6.10.46 |
| Tkuma | Sumra | Be'erot Yitzhak | 665 | **1949** | 31.45092, 34.57933 | 30.5.49 |

The article also gives the mnemonic "קח משכנתא נג"ב" for the eleven names. [S]

**The CBS coordinates above are the present localities.** Several of the eleven moved after the war,
so for those the CBS point is not the October 1946 point. Where an article gives the original site
explicitly it is recorded below; otherwise the original position is [GAP].

### The ones where CBS disagrees with 1946

- **Nirim.** CBS `שנת ייסוד` **1949**; Gvati and he.wiki **6 October 1946**. [C] he.wiki `נירים`,
  oldid **43556145**: "יצאו חברי נירים במוצאי יום הכיפורים תש"ז, 6 באוקטובר 1946, במסגרת עליית 11
  הנקודות", and "בגלל תקלה לוגיסטית, ההתיישבות לא הייתה במקום המתוכנן", the planned point being
  reached only months later, at **Dangur**. [S] After the war, "בתחילת 1949 עבר הקיבוץ למקומו
  הנוכחי ובשטחו המקורי הוקם הקיבוץ ניר יצחק". [S] **Our reading:** CBS's 1949 is the present site
  and the 1946 date is the Dangur point; they are not in conflict about the same thing. The battle
  of Nirim is dated "בוקר ה־15 במאי 1948" in the article. [S]
  - **[C] on where the first site was.** he.wiki places it "ליד קיבוץ סופה של היום" and also says
    Nir Yitzhak was founded on Nirim's original ground; Gvati's narrative says Nir Yitzhak now sits
    at Dangur. For reference, CBS Nir Yitzhak is code **402**, `שנת ייסוד` 1949, **31.23768,
    34.35765**. [V] Not resolved here.
- **Tkuma.** CBS **1949**, Gvati **30.5.49**, he.wiki **the night of 5 to 6 October 1946**. [C], and
  he.wiki `תקומה`, oldid **42996459**, explains the mechanism: founded as a kibbutz at Sumra, then
  "בתאריך 30 במאי 1949 ... עברו תושבי תקומה אל אזור אדמות הכפר הערבי אל-מוחרקה, שתושביו גורשו על ידי
  כוחות של חטיבת הנגב". [S] **Rename: founded as סומרה**, renamed Tkuma within days. [S] It also
  changed type, founded as a kibbutz and now a moshav, which is why Gvati's row is in the moshavim
  section. **Our reading:** 1946 is the founding and 1949 the move; a row needs both.
  - The 1949 move is onto the land of a depopulated Arab village, which the article states. That is
    recorded here as the source states it and nothing is proposed about it; it is the owner's
    sensitivity review.
- **Kedma.** CBS `שנת ייסוד` is **blank**, and the CBS row is the present youth village, not the
  kibbutz. Gvati's appendix has **no row**; his narrative mentions "קדמה, גלאון, דברת שעלו בשנת
  1946". [S] he.wiki `קדמה`, oldid **43293211**: "קֶדְמָה התקיימה כקיבוץ בין השנים 1946–1962", and
  "מאז 1979 נמצא במקום כפר נוער טיפולי". [S]
  - **An impossible date in the source.** The article's founding sentence reads "במוצאי יום כיפור,
    10.6.1946 יצאו חברי קדמה מכפר מנחם". [S] Motza'ei Yom Kippur 5707 and 10 June 1946 cannot both
    be right, and the `11 הנקודות` article dates the operation to 5 to 6 October 1946. The numeral
    is reported as written. **Our reading:** the day-month digits are transposed or wrong and the
    intended date is the October ascent, but no page read here says so, so no corrected date is
    given.
  - End: "במאי 1962 עזבו אחרוני קדמה את המקום". [S] The article carries
    `קטגוריה:יישובים ישראליים שננטשו`.

### The rest of the eleven

- **Urim.** he.wiki `אורים`, oldid **44069164**: "קבוצת אורים הוקמה במוצאי יום כיפור ה'תש"ז, 1946".
  [S] **Three sites, two with explicit coordinates in the article's own footnotes:** first at Grein
  near Gvulot, `31.185812, 34.485823`, where a marker stone now stands; then "לנקודה הסמוכה לצאלים",
  `31.184178, 34.499766`, where a half-ruined security house stands; then "בשנת 1948 עברו למקום הקבע
  במשטרת עימארה". [S] The CBS point **31.30419, 34.52263** is the third.
- **Be'eri.** he.wiki `בארי (קיבוץ)`, oldid **44086869**: "בארי הוקם במוצאי יום כיפור ה'תש"ז, 1946,
  במסגרת 11 הנקודות". [S] Year only in the article; Gvati gives 6.10.46. Relocation: "לאחר קום
  המדינה הועתק הקיבוץ שלושה קילומטר דרום־מזרחה". [S] No day. [GAP]
- **Gal-On.** he.wiki `גלאון`, oldid **43897874**: "במוצאי יום הכיפורים ה'תש"ז, 5-6 באוקטובר 1946,
  עלה על הקרקע ליד קיבוץ גת, במסגרת 11 הנקודות בנגב". [S] This is the one article that prints the
  two-day form. Gvati puts it in **Lachish**, not the Negev. Not evacuated: it repelled an Egyptian
  attack "ב-14 ביולי 1948". [S]
- **Hatzerim.** he.wiki `חצרים`, oldid **43555845**: "הקיבוץ הוקם במוצאי יום כיפור בשנת 1946 על
  אדמות קלטה כחלק מ־11 הנקודות". [S] No move recorded.
- **Mishmar HaNegev.** he.wiki `משמר הנגב`, oldid **44065142**: "שהוקם כחלק ממבצע י"א הנקודות
  במוצאי יום כיפור תש"ז, 6 באוקטובר 1946", and "לאחר שנה הועתק הקיבוץ למקומו הנוכחי על גבעה שכנה
  כ-500 מטר צפונה", the first site now called "הגבעה הראשונה". [S]
- **Nevatim.** he.wiki `נבטים (מושב)`, oldid **44032217**: "מושב נבטים נוסד ב-6 באוקטובר 1946,
  במוצאי יום כיפור, במבצע 11 הנקודות על ידי עולים יוצאי עיראק". [S] Moved "כ-2 קילומטר" to its
  permanent site; no date for the move. [GAP] Note the bare title `נבטים` is the article on the
  Nabataeans, not the locality.
- **Shoval.** he.wiki `שובל (קיבוץ)`, oldid **44069091**: "נוסד ב-6 באוקטובר 1946", and "עלה לקרקע
  באדמות קרן קיימת לישראל ב'ביר זבאלה' במוצאי יום כיפור, 6 באוקטובר 1946". [S] Gvati gives the
  same day, so the date is [V]. **Intended name:**
  the founders wanted "אילת"; the settling institutions chose Shoval. [S]
- **Kfar Darom.** **Not in the CBS file and no Gvati appendix row.** [V, checked] he.wiki
  `כפר דרום`, oldid **42998094**: "במוצאי יום הכיפורים תש"ז -1946 יושב המקום, על ידי גרעין של הקיבוץ
  הדתי", with explicit coordinates `רוחב=31.401389 | אורך=34.36` and `תאריך נטישה = 18 באוגוסט
  2005`. [S] First attacked "ב-13 במאי 1948, יום לפני הכרזת העצמאות", the first locality the
  Egyptian army attacked. [S] Evacuated on the night of **8 July 1948**. [S]
  - **[C] on the hour of the fall.** Gvati: the defenders left at night "ולמחרת נכבש הישוב
    על־ידי המצרים". he.wiki: "בבוקר 8 ביולי החל הצבא המצרי בהתקפה על היישוב ... וגילה כי היישוב
    ריק", that is the same morning. Both given.
  - Gvati lists it among the localities permanently lost in the war, and its people founded **Bnei
    Darom**: his appendix row reads `בְּנֵי-דָרוֹם | דרום | 23.3.49 | הפועל המזרחי | נוסד בידי חברי
    כפר-דרום שננטש במלחמת השחרור ונקרא על שמו`. [S]
  - The 1970 re-establishment and the 2005 evacuation are outside this note's window and are
    recorded only so the row is not mistaken for a continuous settlement.

## 4. 1947 and 1948

### Sa'ad
- CBS: code **419**, `שנת ייסוד` **1947**, **31.47068, 34.53445**, Be'er Sheva. [V]
- Gvati: `סַעַד | נגב מערבי | 30.6.47 | הפועל המזרחי`. [S]
- he.wiki `סעד`, oldid **42997897**: "היישוב עלה על הקרקע בי"ב בתמוז ה'תש"ז (30 ביוני 1947), במלאת
  שנה ל'שבת השחורה'". [S] **All three agree, and Gvati and he.wiki agree to the day.**
- **Rename:** "השם שנבחר לקיבוץ על ידי מייסדיו היה 'עלומים', אולם בהחלטת המוסדות המיישבים שונה השם
  ל'סעד'". [S]
- Relocation after the war: "העבירו חברי הקיבוץ את יישובם כקילומטר מזרחה". [S] No day. [GAP]

### Tze'elim
- CBS: code **413**, `שנת ייסוד` **1947**, **31.20370, 34.53417**, Be'er Sheva. [V]
- Gvati: `צֶאֱלִים | הנגב המערבי | 7.2.47 | האיחוד`. [S]
- he.wiki `צאלים`, oldid **43801220**: "הקיבוץ נוסד בינואר 1947". [S]
- **[C]: Gvati 7 February 1947 against he.wiki January 1947.** Worth flagging that 7 February 1947
  is also the date he.wiki gives for **Mivtahim**; the two may be the same event recorded against
  different names. Not resolved here.
- **Rename:** "שם הקיבוץ בנגב היה בהתחלה 'שרשים'. לאחר זמן קצר נאלצו לשנות את השם לצאלים". [V]
- **A full change of population**, which a row should probably carry: "לאחר המלחמה עברו במרץ 1949 כל
  חברי צאלים אל הקיבוץ כפר החורש שליד נצרת", and a group of Holocaust survivors from eastern Europe
  took their place. [V]

### Gvim
- CBS: code **424**, `שנת ייסוד` **1947**, **31.50689, 34.59654**, Ashkelon. [V]
- Gvati: `גֵבִים | דרום | 28.8.47 | האיחוד`. [S]
- he.wiki `גבים`, oldid **44057295**: "קיבוץ גבים הוקם בי"ב אלול תש"ז, 28 באוגוסט 1947". [S]
  **Gvati and he.wiki agree to the day.**
- **Rename:** "שמו המקורי של הקיבוץ היה שדה עקיבא ... לאחר זמן מה שונה שמו לגבים". [V]
- **An error in a source, reported rather than dropped.** he.wiki `בתי הביטחון בנגב הצפוני`, oldid
  **42567087**, gives Gvim's coordinate as `31.204928, 34.455571`. That is the same pair it gives
  for Gvulot and lies about 34 km from the CBS Gvim point (computed here). It should not be used.

### Mivtahim, the 1947 kibbutz
- he.wiki `מבטחים (מושב)`, oldid **42987731**: "היישוב מבטחים הוקם לראשונה ב-7 בפברואר 1947 כקיבוץ
  של העובד הציוני". [V] Abandoned: "עם תום מלחמת העצמאות החליטו חברי מבטחים לפנות את המקום" and
  joined the founders of the new Nitzanim. [V] No day for the evacuation. [GAP]
- **The present moshav is not the same founding.** CBS code **829**, `שנת ייסוד` **1950**, **31.24040,
  34.40787**; Gvati, moshavim section, `מִבְטָחִים | הנגב המערבי | 17.12.50`; he.wiki "המושב מבטחים
  הוקם מחדש ב-1950 על ידי עולים מכורדיסטן ליד היישוב הישן הנטוש". [V] Only the 1947 kibbutz is in
  this note's window.

### Brur Hayil
- CBS: code **428**, `שנת ייסוד` **1948**, **31.55787, 34.64673**, Ashkelon. [V]
- Gvati: `בְרוֹר חַיִל | הנגב המערבי | 18.4.48 | האיחוד`. [S] His narrative adds it was built
  "בשיטה של חומה ומגדל" beside the Arab village of Burayr.
- he.wiki `ברור חיל`, oldid **43890748**: "באפריל 1948 הוקם משלט סמוך לכפר ברייר בנגב המערבי על ידי
  הפלמ"ח ותושבים מהאזור", and it was "אחד משני היישובים היחידים שהוקמו בין החלטת החלוקה להכרזת
  העצמאות". [V] **This is the only locality in this note founded between the Partition Resolution
  and 15 May 1948.**
- Relocation: "בינואר 1950 עברו החברים ממיקומו הראשוני של היישוב לנקודה החדשה". [V]

## 5. Be'erot Yitzhak, the original Negev site

The CBS row is the post-war locality in the centre of the country, so the Negev site needs its own
entry or it will be lost.

- CBS: code **450**, `שנת ייסוד` **1948**, **32.04080, 34.91041**, **Petah Tikva sub-district**. [V]
  That is not the Negev site.
- **No Gvati appendix row** for either site. [V, checked] His narrative covers the attack: the
  Egyptians tried to take it "ב-15.7" and "המשק ההרוס נשאר בידי כוחותינו", and its members later
  moved to Wilhelma. [V]
- he.wiki `בארות יצחק`, oldid **43784200**: "בכ"ב בשבט תש"ג (28 בינואר 1943) עלתה קבוצת חלוץ של 12
  חברים והתחילה לבנות את הקיבוץ, כחמישה קילומטר מדרום-מזרח לעזה", with an explicit coordinate
  footnote for the first site, `31.46528, 34.50703` (given as 31 27 54.99 N, 34 30 25.3 E;
  converted to decimal here). [V]
- **Rename:** the group was "רמת השומרון" at Pardes Hanna before the name Be'erot Yitzhak. [V]
- Destroyed "בח' בתמוז תש"ח (15 ביולי 1948)", with 17 defenders killed and the farm "נהרס לחלוטין",
  and the decision was "שלא לחזור" to the site. [V] The members moved to Wilhelma and "בשנת 1952
  עברו חברי הקיבוץ צפונה מווילהלמה והקימו את בארות יצחק החדשה". [V]
- **[C] on the re-establishment year: CBS 1948 against he.wiki 1952.** Both refer to the centre of
  the country, not the Negev. Not resolved here.
- It was the departure base for three of the eleven points, Kfar Darom, Be'eri and Tkuma. [V]

## 6. Checked and excluded

- **Alumim.** CBS code **1146**, `שנת ייסוד` **1966**; Gvati `עֲלוּמִים | הנגב המערבי | 1966`;
  he.wiki `עלומים (קיבוץ)`, oldid **43556197**, "המתיישבים הראשונים עלו לקרקע באלול תשכ"ו, ספטמבר
  1966". [V] All three agree on 1966. **It does not belong in this window.** It stands on Be'erot
  Yitzhak's original land.
- **An unresolved 1947 "Alumim".** The `11 הנקודות` article says that in February 1947 three more
  points went up, "מבטחים, צאלים ועלומים (יישוב בין באר שבע לרביבים שניטש לאחר מלחמת העצמאות)". [V]
  The `צאלים` and `סעד` articles both say the opposite, that this Alumim was the founding name of
  Sa'ad. A search of he.wiki found no article for a 1947 locality between Be'er Sheva and Revivim.
  **[GAP], and a real one:** either there was a third February 1947 point that is now invisible, or
  the `11 הנקודות` article is confusing it with Sa'ad's original name. Nothing read here settles it.
- **Sufa.** he.wiki's settlement chronology, oldid **43572242**, lists Sufa under 1947. The article
  `סופה (קיבוץ)`, oldid **43488608**, says it began as a Nahal outpost "בשנת 1974". [V] The
  chronology is wrong; Sufa is not added.

## 7. Gaps

- The October 1946 position of Be'eri, Hatzerim, Shoval, Mishmar HaNegev and Nevatim, beyond the
  direction and rough distance the articles give. [GAP] Only Urim, Nirim, Beit Eshel, Kfar Darom and
  Be'erot Yitzhak have an explicit original coordinate in a source read here.
- A day for several of the moves: Gvulot, Be'eri, Nevatim, Sa'ad, Mivtahim's evacuation. [GAP]
- Beit Eshel's founding day within August 1943. [GAP]
- Whether a February 1947 "Alumim" existed as a separate locality. [GAP] See section 6.
- Coordinates were taken from CBS and from article footnotes only. None was computed from a map, and
  no hex was computed here, since `tools/settlement_hexes.py` stays with #61.
