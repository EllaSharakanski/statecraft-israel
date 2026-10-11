# The Arab forces of 1948 as map tokens: research note

Research for the owner's ruling of 2026-10-11 (issue #197): the map shows the Arab forces of 1948 under the same rules as
the Israeli units on branch `units-in-play` (`docs/units.md`, "Units in play";
`docs/superpowers/specs/2026-10-11-units-in-play-design.md`). That means one token per sourced formation, at sourced
positions and dates, with every window's end justified (`end_basis: source | front_armistice`), era-correct figures, a
colour and badge per army, a read-only card, and a sensitivity review before shipping. The owner's addition of the same
day adds the local Palestinian Arab forces as a full category. The content policy is `docs/CONTENT_POLICY.md`: Israeli
perspective, accurate facts.

This is research and a data draft only. No Unity code changes. The draft data is
`research_notes/Units 1948/units_1948_arab.draft.yaml`, in the schema of `content/map/units_1948.yaml` (branch
`units-in-play`) plus `side`, `army`, `echelon`, `commanders`, `conflicts` and `to_verify`, and a top-level `armies`
block for colour, badge and appearance. It passes the units-in-play data rules: every window ends for a stated reason, no
`since` on the game's first day, sources with URL and check date, and `wikipedia_only` matching the sources. All
sources were checked on 2026-10-11.

## How it was researched, and what could not be reached

- **Lorch, *The Edge of the Sword* (1961), and the IDF and Defense Ministry histories were not reachable.** The Internet
  Archive copies of Lorch (`edgeofsword00lorc`, `edgeofsword0000unse`, `bwb_S0-CBG-479`) are lending-library items. They
  have no public text, and search-inside is refused. No IDF History Department text was found online. Lorch appears in
  this note only where Hebrew Wikipedia reports his account (al-Malikiyya, below). **Every row is a draft until checked
  against Lorch** or the IDF and Defense Establishment Archives (IDFA).
- **Two academic histories were checked page by page through Google Books search-inside snippets.** Each snippet is a
  few lines of the printed page, with its page number. Both books rest on the IDFA files and on Arab memoirs.
  - David Tal, *War in Palestine, 1948: Israeli and Arab Strategy and Diplomacy* (Routledge, 2004; Tal is an Israeli
    military historian), Google Books `Vs2PAgAAQBAJ`.
  - Benny Morris, *1948: A History of the First Arab-Israeli War* (Yale, 2008), Google Books `CC7381HrLqcC`.
  - Each book source in the YAML is a `books.google.com` URL with `&pg=PA<page>` and a `cite`. Where a snippet was
    garbled (two columns run together) or cut off, the row says so under `to_verify`.
- **Pollack, *Arabs at War* (2002/2004), was not searchable.** Google Books returned no results for `sSHYdGR_xvoC`, and
  the Internet Archive copy is lending-only. Nothing here rests on Pollack.
- **Wikipedia (English and Hebrew) and Wikimedia Commons** were read through the MediaWiki API: the battle and army
  articles, and photographs for the figures. `wikipedia_only: true` marks a row whose every source is Wikipedia or
  Wikidata.
- **Coordinates** are the place's Wikipedia article (API `prop=coordinates`), Hebrew Wikipedia (Mishmar HaYarden's
  original moshava, al-Majdal) or Wikidata (Ramallah Q158119, Tarshiha village Q134496896).
- **Privacy.** Every request carried the User-Agent `statecraft-israel-research/1.0
  (+https://github.com/eyalgolan/statecraft-israel)` and no personal identifier.

## The formations found

Twenty-eight rows: 21 shown, 7 hidden. The windows are the map's (from inclusive, to exclusive). "Armistice" means the
window ends on the signing of the agreement for its front: Egypt 24 Feb 1949, Lebanon 23 Mar, Jordan 3 Apr, Syria 20 Jul.

| Army | Formation (echelon) | Where and when (shown) | Main sources |
|---|---|---|---|
| Egypt | Main brigade group: 1st, 6th, 9th battalions (brigade group) | Gaza 16-24 May; Majdal 24-29 May; Isdud 29 May-8 Jul | Morris 233, 237, 244; Tal 177, 182, 186 |
| Egypt | 2nd Brigade | Isdud 8 Jul-27 Oct; Majdal 27 Oct-5 Nov | Morris 275, 322, 331; Tal 375, 394 |
| Egypt | 4th Brigade (Sayyid Taha) | Majdal 8 Jul-15 Oct; Faluja 15 Oct-armistice | Morris 276, 322, 332, 335; Tal 286, 375, 382-392; he.wiki |
| Egypt | 3rd Brigade | Gaza 15 Oct-armistice | Morris 322; Tal 375 |
| Egypt | Eastern force, the Light Forces Command from July (Ahmad Abd al-Aziz) | Bethlehem 22 May-armistice | Morris 234, 322, 377; Tal 177, 216, 375, 435 |
| Egypt | Beersheba garrison (battalion) | Beersheba 19 May-21 Oct | Morris 234; Tal 177; he.wiki Yoav |
| Arab Legion | 1st Brigade (Goldie): 1st, 3rd Regiments | Nablus 15-23 May; Bayt Nabala 10-18 Jul | en.wiki table; Tal 205, 216; Morris 287-292 |
| Arab Legion | 3rd Brigade (Ashton): 2nd, 4th Regiments | Latrun 17 May-armistice | Morris 220-221, 287; en.wiki Latrun |
| Arab Legion | 4th Brigade (al-Jundi): 5th, 6th battalions | Ramallah 15 May-armistice | Tal 205-206, 217 |
| Arab Legion | 6th Regiment (Abdullah al-Tall) (regiment) | Old City 18 May-armistice | Tal 213; Morris 218 |
| Arab Legion | 3rd Regiment (Newman) (regiment) | Sheikh Jarrah 19 May-armistice | Morris 216; en.wiki |
| Iraq | The force at Gesher (brigade group; its number conflicts) | Jisr al-Majami 15-22 May | Morris 247; Tal 216; he.wiki |
| Iraq | 4th Brigade | Jenin 31 May-armistice (Jordan) | Tal 233; Morris 250; en.wiki |
| Iraq | 1st Brigade | Baqa al-Gharbiyya 31 May-armistice (Jordan); the snippet is garbled | Tal 233 |
| Syria | 1st Brigade (al-Hakim, then al-Za'im) | Samakh 16-21 May; Mishmar HaYarden 10 Jun-armistice | Morris 251-257, 350; Tal 262, 340, 426; en.wiki |
| Lebanon | 3rd Battalion (battalion) | al-Malikiyya 6 Jun-8 Jul | Morris 258-260; en.wiki; he.wiki |
| ALA | 2nd Yarmuk Battalion (Shishakli) (battalion) | al-Malikiyya 15-29 May; again 6 Jun-31 Oct (second window to verify) | he.wiki; en.wiki; Tal 334 |
| ALA | 3rd Yarmuk Brigade (Mahdi Salah) | Nazareth 11 Jun-16 Jul; Tarshiha 22-31 Oct | Tal 266, 334, 419, 424; Morris 278; he.wiki Hiram |
| Local | Army of the Holy War, Lydda-Ramle (Hasan Salama) | Ramla 15 May-2 Jun | en.wiki; Tal 226-227, 238 |
| Local | Army of the Holy War, Jerusalem | Old City 15 May-3 Oct | he.wiki; en.wiki; Tal 213 |
| Local | Army of the Holy War, Nazareth | Nazareth 15 May-16 Jul (one uncited he.wiki sentence) | he.wiki only |

**Hidden** (listed, not drawn):
- the Saudi contingent: below brigade scale, Gaza-Rafah from July;
- the Iraqi 3rd and 5th brigades, and the 2nd after Gesher: no place;
- the Syrian 2nd Brigade: every place is on the Syrian side;
- the Syrian bridgehead brigade of October: 3rd or 4th, the snippet is ambiguous;
- the ALA 1st Yarmuk Brigade: a road, not a place;
- the two Palestinian battalions with the Iraqi 1st Brigade: unnamed;
- the Lydda and Ramla town militias: unnamed.

### Egypt (the Egyptian Expeditionary Force)

- **Command.** Maj. Gen. Ahmad Ali al-Muwawi commanded, with Col. Mohammed Neguib as deputy (Morris p. 233). He was
  replaced by Gen. Ahmad Fuad Sadiq after Yoav (Morris p. 351).
- **The crossing.** The force crossed on 15-16 May as "a reinforced brigade group, composed of the First, Sixth, and
  Ninth battalions", later joined by the 2nd and 4th brigade groups (Morris p. 233). The advance was held up at the Gaza
  headquarters until 19 May (Tal p. 177). It reached Majdal on 24 May (en.wiki) and stopped just north of Isdud by the
  end of May (Morris p. 244).
- **Brigade designations are sourced from July:**
  - 8 July, Operation An-Far: the 2nd Brigade (7th, 6th and a Sudanese battalion) from the Isdud area (Morris p. 275),
    and the 4th Brigade (2nd and 9th battalions) from Majdal (Morris p. 276).
  - On the eve of Yoav (15 October), three brigades held the coast and the wedge:
    - the 3rd Brigade: El Arish to Gaza (Morris p. 322), or to south of Yad Mordechai (Tal p. 375);
    - the reinforced 4th Brigade (1st, 2nd, 6th, 9th battalions): Yad Mordechai-Majdal-Hamama and the Majdal-Iraq
      al-Manshiya wedge;
    - the 2nd Brigade: Hamama to Isdud.
  - So the main brigade group's windows end on 8 July, and the brigade rows begin then.
- **Isdud and Majdal.** Isdud was evacuated on 26-27 October (Morris p. 331) and Majdal on 5 November (Tal p. 394).
- **The Faluja pocket.** The 4th Brigade, under Col. Sayyid Taha, about 4,000 men (he.wiki), was encircled around
  Faluja-Iraq al-Manshiya (Morris pp. 332-335; Tal pp. 382-392). It was evacuated on 26 February (en.wiki) or
  27 February 1949 (he.wiki), after the 24 February armistice. The window ends on the armistice (rule C1); owner
  question 3.
- **The eastern arm.** Egyptian troops and volunteers under Ahmad Abd al-Aziz occupied Beersheba on 19 May. They passed
  through Hebron to the Bethlehem area and the edge of Jerusalem, and joined the Legion and local irregulars at Ramat
  Rachel on 22-24 May (Morris p. 234; Tal pp. 177, 216).
  - From July they formed the "Light Forces Command" (Tal p. 375), holding the Hebron hills and Bethlehem in October
    (Morris p. 322) and the line from Hebron to Sur Bahir (Tal p. 435).
  - They were still in the Bethlehem area at the armistice talks (Morris p. 377).
- **Conflicts:**
  - Beersheba: 17 May (Tal) or 19 May (Morris, en.wiki).
  - Bethlehem: 20 May (Tal) or about 22 May (Morris).
- **Not covered:**
  - the Egyptian positions at al-Auja, Bir Asluj, Rafah and in Sinai during Horev (December-January): no formation is
    named in the snippets;
  - the Sudanese, Yemeni and Moroccan contingents, inside Egyptian formations.

### The Arab Legion (Transjordan)

- **Organisation.** Four single-battalion "regiments" in two brigades, plus independent companies (en.wiki Arab Legion):
  - the 1st Brigade (Col. Desmond Goldie): the 1st and 3rd Regiments;
  - the 3rd Brigade (Col. T. Ashton): the 2nd and 4th Regiments;
  - a hastily formed 4th Brigade under Col. Ahmad Sidqi al-Jundi, with an all-Arab staff and its headquarters in
    Ramallah: the 5th and 6th battalions, each about 1,000 men (Tal pp. 205-206).
- **Conflict over the 5th and 6th.** en.wiki's table, citing Abu Nawar and Morris, puts the 5th and 6th in a "2nd
  Brigade" (support) and gives al-Jundi's 4th Brigade "support: Ramallah, Hebron and Ramla". The row follows Tal.
- **Latrun.** The 4th Regiment (Habis al-Majali) reached Latrun on 17 May, and the road was cut on 18 May (en.wiki;
  Morris p. 220). The 2nd Regiment (Geoffrey Lockett) joined it: the 3rd Brigade totalled 2,300 men plus 800 auxiliaries
  (Morris p. 221). It still held Latrun in July (Morris p. 287), and Latrun stayed Jordanian at the armistice.
- **Jerusalem:**
  - Abdullah al-Tall's 6th Regiment arrived on 18 May. Al-Tall took command of the Arab forces in the city (Tal p. 213)
    and besieged the Jewish Quarter (Morris p. 218).
  - The 3rd Regiment (Lt Col William Newman) came from Samaria through Sheikh Jarrah to the northern walls (Morris
    p. 216). en.wiki gives 19 May; the Morris snippet shows no date.
- **The 1st Brigade:**
  - Its 1st Battalion held the Nablus area until the Iraqis relieved it after 22 May. It then moved to Latrun (Tal
    pp. 205, 216).
  - In Operation Dani its 1st Regiment fought at Beit Nabala and Deir Tarif (Morris pp. 287-292), and the brigade fought
    at Qula until 18 July (en.wiki).
  - en.wiki has the "4th Regiment" joining at Lydda on 10 July; Morris has the 1st. The row follows Morris.
- **Echelon.** Two Legion rows are regiments (battalion-sized): owner question 1.
- **Not covered:**
  - the 1st Brigade between 23 May and 10 July;
  - the 5th Regiment;
  - the Legion's companies at Hebron (Tal p. 353, autumn 1948) and in Lydda;
  - the Legion's sector after July.

### Iraq (the expeditionary force)

- **Command.** Gen. Nur al-Din Mahmud commanded (Morris p. 188; he.wiki).
- **Gesher.** The force crossed the Jordan on 15 May and attacked Gesher, taking Camel Hill, and was repulsed. It
  withdrew from Jisr al-Majami on 22 May to Nablus (Morris p. 247; Tal p. 216; en.wiki).
- **Which brigade was at Gesher conflicts:**
  - Morris p. 247: Iraq's 2nd Brigade Group;
  - he.wiki: the 1st Brigade, with a mechanised force under Amid Tahir al-Zubaydi, and the 4th later;
  - Tal p. 161: Iraq had the 1st and 3rd brigades, with the 4th forming.
  - The card should carry no number until this is settled.
- **Jenin and the Triangle.** At the IDF's attack on Jenin (31 May-4 June) Tal p. 233 gives the Iraqi line, from the
  Beisan valley to Nir Nazem near Ras al-Ein, in three sectors:
  - northern: the Jordan to Lajjun, the 4th Brigade;
  - western: from Baqa al-Gharbiyya, the 1st Brigade with two Palestinian battalions;
  - southern: one battalion; the Iraqi command counted Kafr Qasim as a Legion sector.
  - The western-sector reading is from a garbled snippet (`to_verify`).
  - Morris p. 248 has the 4th and 5th brigades arriving later (five brigades, 18,000 men by September). That conflict is
    recorded.
- **The end.** The Iraqis left Samaria in April 1949, under the Israel-Jordan agreement (he.wiki; Morris p. 388). Iraq
  signed no armistice, so the rows end on the Jordan armistice.

### Syria

- **The Kinarot valley.** In May only the 1st Brigade (Col. Abd al-Wahab al-Hakim, then Husni al-Za'im) was ready
  (Morris p. 251; Tal p. 162; en.wiki Kinarot).
  - It camped at Tel al-Qasr on 15-16 May and took Samakh on 18 May.
  - On 20 May it was beaten at Degania Alef, and the Haganah returned to Samakh on 21 May (en.wiki).
- **Mishmar HaYarden.** The 1st Brigade, with elements of the 2nd, took Mishmar HaYarden on 10 June (Morris p. 257;
  en.wiki).
  - In July two brigades held the bridgehead (Tal p. 340).
  - In October the 1st Brigade's five battalions held the whole sector (Tal p. 426).
  - The Syrians kept the enclave to the armistice (Morris p. 350).
- **Hidden.** The 2nd Brigade: every place given is on the Syrian side (the Ein Gev diversion, the Quneitra reserve,
  the border to Tall Azaziyat). The October bridgehead brigade under Adib al-Shishakli: the 3rd or 4th, the snippet
  does not say.
- **Not covered.** The Syrian enclaves near Banias and at Samra-Tel al-Qasr at the armistice (Morris p. 350): no
  formation is named.

### Lebanon

- **Strength.** The Lebanese Army had about 3,500 men in four battalions (Morris p. 258). Only the 3rd Battalion fought.
- **al-Malikiyya.** With a Syrian battalion and the ALA, it retook al-Malikiyya and Qadas on 5-6 June, handed them to
  the ALA and withdrew on 8 July (en.wiki, citing Morris p. 260; he.wiki).
- **Conflict over 15 May.** Lorch's account, and the Palmach veterans', has a Lebanese Army counter-attack at
  al-Malikiyya on 15 May. he.wiki reports a "general and rare agreement" in later research, drawing on Guy Maayan's
  translation of the official Lebanese account, that the force on 15 May was the ALA. The draft follows the later
  research; owner question 5.

### The Arab Liberation Army (ALA)

- **al-Malikiyya.** On 15 May the 2nd Yarmuk Battalion (Adib al-Shishakli) held al-Malikiyya against the Palmach.
  Yiftach retook it on the night of 28-29 May, and the Lebanese-ALA attack took it back in June. It fell to the 7th
  Brigade on 31 October (he.wiki, en.wiki). Which ALA battalion held it from June to October needs the printed Tal
  p. 334.
- **The 3rd Yarmuk Brigade.** Under Mahdi Salah (the Hittin Battalion, a reserve battalion and local forces), it had
  its headquarters in Nazareth (Tal p. 334). Al-Qawuqji reached Nazareth as the first truce began on 11 June (Tal
  p. 266), and Nazareth surrendered on 16 July (en.wiki Dekel). In October he.wiki puts the 3rd Yarmuk Brigade in the
  Tarshiha area.
- **After the second truce.** The ALA command was at Aytarun in Lebanon, with three Yarmuk brigades of 2,000-3,000 men
  in all and a brigade headquarters in Tarshiha (Tal p. 419). Morris p. 340 says each "brigade" was in fact a battalion.
- **Operation Hiram.** Hiram (29-31 October) ended the ALA's presence in the Galilee (Tal p. 424; he.wiki).
- **Not covered.** The 1st Yarmuk Brigade: along the Safad-Acre road, no place. The ALA in Samaria and Jerusalem before
  15 May.

### Local Palestinian Arab forces (owner addition, 2026-10-11)

The rule is the same: a token only for a named force at a sourced place and date, from 15 May 1948.

**Shown:**
- **Army of the Holy War, Lydda-Ramle sector (Hasan Salama).**
  - His assignment to the Lydda district was reconfirmed on 5 February 1948, with a headquarters near Ramla from March
    (en.wiki Hasan Salama). Tal p. 227: in late May the forces in Lydda and Ramla were under his command.
  - He was mortally wounded at Ras al-Ein and died on 2 June in Ramla. His forces were left without leadership (Tal
    pp. 226, 238). The window ends on 2 June.
- **Army of the Holy War, Jerusalem.**
  - After Abd al-Qadir al-Husayni's death, Ibrahim Abu Dayyeh was the senior commander in the Jerusalem area (he.wiki).
    Tal p. 213 names Khalid al-Husayni and Munir Abu Fadl as commanders of the local forces in the city on 18 May.
  - Jordan ordered the Legion to disarm the Holy War units on 3 October 1948 (he.wiki; en.wiki: October). he.wiki adds
    that they went on in practice until spring 1949; the window ends on the order.
  - Tal says "local forces", not the Holy War by name (`to_verify`).
- **Army of the Holy War, Nazareth.** One uncited sentence in he.wiki: "another Holy War force operated in the Nazareth
  area until its capture in mid-July". Shown in the draft, but it needs a published source before it ships.

**Hidden:**
- the two Palestinian battalions with the Iraqi 1st Brigade (Tal p. 233): unnamed;
- the Lydda and Ramla town militias, about 1,000 in Lydda with a Legion contingent of 125-300 (en.wiki): unnamed.

**Before the game's window** (recorded, not drawn):
- **Abd al-Qadir al-Husayni's Holy War forces** around Jerusalem and al-Qastal: he was killed on 8 April 1948, and the
  Palmach retook al-Qastal on 8-9 April (en.wiki Army of the Holy War). His headquarters had been at Bir Zeit.
- **Hasan Salama in Jaffa**, 22 January to early February 1948 (en.wiki Hasan Salama).
- **Jaffa's defenders:** a Muslim Brotherhood company and the local National Guard (en.wiki Jaffa). The city
  surrendered on 14 May 1948.
- **Haifa:** taken on 21-22 April 1948 (en.wiki Battle of Haifa).
- **Acre:** surrendered on the night of 17-18 May, inside the window. No source found names its defending force, so it
  has no row (Morris p. 166 and p. 453, note 292; en.wiki Operation Ben-Ami).

## Era-correct markings and figures (proposals for the token design)

The `armies` block in the YAML has each army's colour (hex), badge glyph and appearance, with sources. In short:

| Army | Colour (proposal) | Badge | Figures (sourced) |
|---|---|---|---|
| Egypt | green `#1E6B3A` (the green flag) | crescent and three stars | British-pattern khaki and battledress; peaked caps, berets, side caps, Brodie helmets (Faluja photos, 1948) |
| Arab Legion | crimson `#A3242B` (the red chevron) | seven-pointed star | checked shemagh with black agal and cap badge; khaki shirts, leather bandoliers (Ramallah, 20 Jul 1948; Old City walls, 1948) |
| Iraq | black `#2B2B2B` | two seven-pointed stars | khaki drill, sidara (Faisaliyya) side cap, puttees; Humber armoured car (Jenin, 1948) |
| Syria | slate `#5A6B7A` | three stars | not sourced: neutral khaki; R 35/R 37 tanks (en.wiki) |
| Lebanon | off-white `#C8C8C0` | cedar | not sourced: neutral khaki |
| ALA | sand `#B08D57` | neutral (crossed rifles); not the ALA's emblem | keffiyeh and agal, khaki, bandoliers (ALA on parade, 1948) |
| Local forces | stone `#7A6A58` | neutral (rifle) | mixed civilian dress and khaki, keffiyeh, an occasional steel helmet (Holy War fighters, Jerusalem, 1948, before 15 May) |

Notes:
- **Flags.** The Kingdom of Egypt: white crescent and three stars on green. Transjordan (1928): black-white-green with a
  red triangle and a white seven-pointed star. The Kingdom of Iraq: black-white-green with a red trapezoid and two
  seven-pointed stars. Syria (1946-58): green-white-black with three red stars. Lebanon (1943): red-white-red with a
  cedar. Sources: the en.wiki flag articles.
- **The Legion's red-and-white shemagh is not sourced to 1948.** The 1948 photographs are black and white. en.wiki
  Keffiyeh ties the red-and-white shemagh to Jordan, and to Glubb's Jordanian troops in the 1950s. The colour in 1948 is
  to verify, against Glubb's *A Soldier with the Arabs* or a uniform reference.
- **The Desert Patrol's dress is not the 1948 field dress.** Long khaki robes, a red sash and lanyard, and a silver dagger
  (Glubb, *The Story of the Arab Legion*, 1948, open text on the Internet Archive) belong to the Desert Patrol of the
  1930s. The 1948 field photographs show shirts and trousers.
- **Air roundels.** The Royal Egyptian Air Force roundel of the period carried the crescent and three stars on green
  (en.wiki Egyptian Air Force). No Arab aircraft tokens are proposed here.
- **Unverified.** The Iraqi Jenin photograph's provenance: an uploader's public-domain claim.
- **References only.** The photographs are references; no image file is copied into the repository.

## Sensitivity review: items for the owner

The cards stay military: name, number, army, place, dates, commander where sourced, and sources. No card or sector text
mentions civilians. Each item below is something the owner should rule on before shipping.

1. **The ALA's emblem was a dagger stabbing a Star of David.** It appears on the captured armoured car and on the
   al-Musawwar page of 3 April 1948, both on Commons. It must not be drawn or described on the card. The draft proposes
   a neutral glyph.
2. **The name "Army of the Holy War".** It is the force's own name (Jaysh al-Jihad al-Muqaddas). he.wiki calls it "a
   terrorist organisation and militia". Under the content policy the game uses Israeli framing, but the card's text is
   strictly military. Should the card say "Army of the Holy War (Jaysh al-Jihad al-Muqaddas)", or something else?
   Should the category be labelled "Local Arab forces", the usual Israeli term, or "Palestinian Arab forces" (Tal's
   wording)?
3. **The Muslim Brotherhood volunteers** were part of the Egyptian eastern force. The draft names the force by its
   commander and command, not by the Brotherhood.
4. **Events next to token positions that the cards do not mention.** Listed so the owner can confirm they stay off the
   cards and out of any token text:
   - **Kfar Etzion, 13 May:** a massacre in which Legionnaires took part (Morris p. 405). It is before the window, but
     the 6th Regiment, whose commander planned the bloc's conquest (Morris p. 168), stands in the Old City from 18 May.
   - **The Jewish Quarter, 28 May:** the surrender, the expulsion of its inhabitants and the destruction of the Hurva
     Synagogue (6th Regiment, Old City; en.wiki). The Commons photograph of a Legionnaire in the Hurva ruins with a
     Torah scroll is not to be used as a model reference.
   - **Gezer, 10 June** (Legion, near Latrun).
   - **Nitzanim, 6-7 June:** the surrender and its aftermath (Egyptian 9th Battalion, between Majdal and Isdud; Morris
     p. 243).
   - **Qula, July:** the mutilated bodies of 16 Alexandroni prisoners (Legion 1st Brigade; en.wiki).
   - **Lydda and Ramla, 11-14 July:** the IDF's conquest and the expulsion of the inhabitants (near the Legion 1st
     Brigade and the Holy War tokens; en.wiki; Morris p. 290). Both tokens' windows end before or as it begins.
   - **al-Dawayima, 29 October** (IDF 89th Battalion; Tal p. 393): near the Egyptian eastern force's sector.
   - **Operation Hiram, late October-November:** Safsaf and Jish (en.wiki Operation Hiram), Saliha (Morris, index) and
     Hula (Morris p. 344), near the ALA tokens. Also the bombing of Tarshiha (en.wiki Tarshiha).
   - **Faluja after the evacuation:** the treatment of the remaining villagers (en.wiki Al-Faluja). It falls after the
     4th Brigade's window.
   - **Acre:** the en.wiki article reports an allegation of biological warfare against the town's water. There is no
     token there.
5. **Iraq never signed an armistice.** The Iraqi rows end on the Israel-Jordan agreement, which covered the Iraqi
   front. The card should not say "armistice with Iraq".
6. **al-Malikiyya.** The draft departs from Lorch, the Israeli official account, in favour of the later research
   (ALA, not the Lebanese Army, on 15 May). Owner question 5.
7. **Crowding in Jerusalem.** Within a few kilometres stand the 6th Regiment and the Holy War (Old City, the same
   coordinate), the 3rd Regiment (Sheikh Jarrah) and the Israeli Etzioni Brigade (Jerusalem, 35.22556 E). The placer's
   one-hex offset could put an Arab token on ground Israel held. With no front data yet (rule I2), the owner may prefer
   one Arab token in Jerusalem.
8. **Shared and contested hexes elsewhere:**
   - al-Malikiyya: Lebanese and ALA, 6 June-8 July;
   - Nazareth: ALA and Holy War, 11 June-16 July;
   - Latrun: the Legion 3rd Brigade and the Israeli 7th Brigade on the same coordinate, 24 May-1 June;
   - Lydda: Yiftach, 11-19 July, beside the Legion at Bayt Nabala.
   The token placement needs the front line, or a rule that keeps opposing tokens apart.

## Owner questions

1. **Echelon.** The ruling is brigade-scale tokens. These rows are below brigade:
   - the Legion's 6th and 3rd Regiments (battalion-sized, about 1,000 men each; Tal p. 206): the only way to show the
     Old City and Sheikh Jarrah;
   - the Lebanese 3rd Battalion (Lebanon's only unit in battle);
   - the ALA 2nd Yarmuk Battalion (al-Malikiyya; Morris p. 340 says every ALA "brigade" was a battalion);
   - the Egyptian Beersheba garrison;
   - the hidden Saudi contingent.
   Show them, hide them, or merge them into their brigades?
2. **Figure count.** The Israeli tokens follow Israel's call-up (1-3 figures). No sim state describes the Arab armies.
   The draft assumes a fixed count, perhaps 3 for brigades and 2 for battalions and forces. Owner's choice.
3. **The Faluja pocket.** It held out two to three days past the Egyptian armistice (evacuated 26 or 27 February 1949).
   Rule C1 ends it on 24 February. Keep C1, or show the sourced evacuation?
4. **Windows opened on a source's later description.** Three rows open on the first date a source places the
   formation, even though it was probably there earlier:
   - the Egyptian 3rd Brigade at Gaza from 15 October;
   - the Iraqi brigades from 31 May;
   - the ALA at Tarshiha from 22 October.
   That leaves gaps, for example Gaza from 24 May to 15 October. Acceptable under "no invented dates"?
5. **al-Malikiyya on 15 May:** Lorch (Lebanese Army) or the later research (ALA)?
6. **The Holy War, Nazareth row** rests on one uncited Hebrew Wikipedia sentence. Hide it until sourced?

## Coverage gaps (what a historian should check next)

- **Lorch, *The Edge of the Sword*, and the IDF History Department's *Toldot Milhemet HaKomemiyut*:** every row. In
  particular the Lebanese question at al-Malikiyya, and the Arab orders of battle by date.
- **Avraham Ayalon:** *Givati Brigade against the Egyptian Invader* (Ma'arachot, 1963) for the Egyptian brigades in May
  and June, and "The Iraqi involvement in the War of Independence" (Ma'arachot 246, 1975) for the Iraqi brigade numbers
  and sectors. Both are IDF publications, cited by he.wiki.
- **Pollack, *Arabs at War*:** the Arab orders of battle, which could not be searched.
- **The printed pages of Tal:** p. 233 (the Iraqi sectors), p. 334 (the ALA battalion at al-Malikiyya), p. 426 (the
  Syrian bridgehead brigade). Also Morris p. 216, the date the Legion's 3rd Regiment entered Jerusalem.
- **Not placed at all:**
  - the Legion 1st Brigade between 23 May and 10 July, and the 5th Regiment;
  - the Iraqi 2nd, 3rd and 5th brigades;
  - the Syrian 2nd, 3rd and 4th brigades;
  - the Egyptian forces at al-Auja, Bir Asluj and Rafah (Horev);
  - the ALA's 1st and 2nd Yarmuk brigades from August to October;
  - the Saudi, Sudanese, Yemeni and Moroccan contingents.
- **The Legion's shemagh colour in 1948**, and Syrian and Lebanese field dress: no sourced photograph or text found.

## Sources (all checked 2026-10-11)

Books, through Google Books search-inside (snippets of the printed pages):
- Tal, David. *War in Palestine, 1948: Israeli and Arab Strategy and Diplomacy.* Routledge, 2004.
  https://books.google.com/books?id=Vs2PAgAAQBAJ. Pages used: 161, 162, 177, 182, 186, 205, 206, 213, 216, 217, 226,
  227, 233, 238, 262, 266, 286, 334, 340, 353, 375, 382, 387, 392, 394, 419, 424, 426, 435.
- Morris, Benny. *1948: A History of the First Arab-Israeli War.* Yale University Press, 2008.
  https://books.google.com/books?id=CC7381HrLqcC. Pages used: 166, 168, 188, 194, 205, 216, 218, 220, 221, 233, 234,
  237, 243, 244, 247, 248, 250, 251, 253, 257, 258, 260, 275, 276, 278, 287, 289, 292, 322, 331, 332, 335, 340, 344, 350,
  351, 377, 388, 405, 453.
- Glubb, John Bagot. *The Story of the Arab Legion.* 1948. https://archive.org/details/the-story-of-the-arab-legion-john-bagot-glubb-orig

Not reachable (Internet Archive lending-only):
- Lorch, *The Edge of the Sword*: https://archive.org/details/edgeofsword00lorc
- Pollack, *Arabs at War*: https://archive.org/details/arabsatwarmilita0000poll

English Wikipedia:
- 1948 Arab–Israeli War: https://en.wikipedia.org/wiki/1948_Arab%E2%80%93Israeli_War
- Arab Legion: https://en.wikipedia.org/wiki/Arab_Legion
- Battles of Latrun (1948): https://en.wikipedia.org/wiki/Battles_of_Latrun_(1948)
- Battle for Jerusalem: https://en.wikipedia.org/wiki/Battle_for_Jerusalem
- Battle of Gesher: https://en.wikipedia.org/wiki/Battle_of_Gesher
- Battle of Jenin (1948): https://en.wikipedia.org/wiki/Battle_of_Jenin_(1948)
- Battles of the Kinarot Valley: https://en.wikipedia.org/wiki/Battles_of_the_Kinarot_Valley
- Arab Liberation Army: https://en.wikipedia.org/wiki/Arab_Liberation_Army
- Army of the Holy War: https://en.wikipedia.org/wiki/Army_of_the_Holy_War
- Hasan Salama: https://en.wikipedia.org/wiki/Hasan_Salama
- Operation Dekel: https://en.wikipedia.org/wiki/Operation_Dekel
- Operation Hiram: https://en.wikipedia.org/wiki/Operation_Hiram
- Al-Faluja: https://en.wikipedia.org/wiki/Al-Faluja
- Jaffa: https://en.wikipedia.org/wiki/Jaffa
- Battle of Haifa (1948): https://en.wikipedia.org/wiki/Battle_of_Haifa_(1948)
- Operation Ben-Ami: https://en.wikipedia.org/wiki/Operation_Ben-Ami
- Tarshiha: https://en.wikipedia.org/wiki/Tarshiha
- 1948 Palestinian expulsion from Lydda and Ramle: https://en.wikipedia.org/wiki/1948_Palestinian_expulsion_from_Lydda_and_Ramle
- Keffiyeh: https://en.wikipedia.org/wiki/Keffiyeh
- Sidara: https://en.wikipedia.org/wiki/Sidara
- Flag of Egypt: https://en.wikipedia.org/wiki/Flag_of_Egypt
- Flag of Jordan: https://en.wikipedia.org/wiki/Flag_of_Jordan
- Flag of Iraq: https://en.wikipedia.org/wiki/Flag_of_Iraq
- Flag of Syria: https://en.wikipedia.org/wiki/Flag_of_Syria
- Flag of Lebanon: https://en.wikipedia.org/wiki/Flag_of_Lebanon
- Egyptian Air Force: https://en.wikipedia.org/wiki/Egyptian_Air_Force
- 1949 Armistice Agreements: https://en.wikipedia.org/wiki/1949_Armistice_Agreements

Hebrew Wikipedia:
- the Iraqi expeditionary force in the War of Independence (חיל המשלוח העיראקי במלחמת העצמאות);
- the Battles of Malkiyya (קרבות מלכיה);
- the Faluja pocket (כיס פלוג'ה);
- Sayyid Taha (סייד טאהא);
- Operation Yoav (מבצע יואב);
- Operation Hiram (מבצע חירם);
- the Army of the Holy War (צבא הג'יהאד הקדוש);
- Mishmar HaYarden, the moshava (משמר הירדן (מושבה)).

The full URLs are in the YAML.

Wikimedia Commons photographs (references only):
- `John_Bagot_Glubb_and_soldiers_in_Ramallah_July_20_1948.webp`
- `Arab_Legion_Platoon_on_the_walls_of_Old_Jerusalem_(1948).jpg`
- `Iraqi_army_in_jeneen_palestine_1948.jpg`
- `ALA_i.jpg`
- `Qawuqjis_armored_vehicle.jpg`
- `Arab_Liberation_Army.svg`
- `Arab_fighters_of_the_Holy_War_Army_in_Jerusalem_1948.jpg`
- `Nasser_at_Faluja_Palestine.png`
- `Taha_and_Rabin.jpg`
- `Degania_A.jpg`

Wikidata: Q158119 (Ramallah), Q134496896 (Tarshiha).
