# Who held what on 14/15 May 1948: sources and a proposed opening period (research note, 2026-10-15)

Status: research only, for the maintainer's review (#674, step 1). No code and no `content/` file is changed. Nothing here
is to be drawn until the maintainer approves the sources, the period table and the polygons. Companion:
`control-areas-1948-49.md` (D-C), whose section 1 item 3 said "15 May 1948 has no line source (not found)". This note
corrects that: a dated US government line source for 18 May 1948 was found (section 1). It is not Israeli, it is three
days after the game's start, and it is a low-resolution greyscale scan, so the owner has to rule on it (section 7).

Framing (`docs/CONTENT_POLICY.md` section 1): Israeli perspective; Israeli official or mainstream sources first; UN and
US government records for lines observers recorded. The colours mean **military control on a date**, nothing else (D-C,
owner ruling 2026-10-14). Not sovereignty, ownership or population.

## Conventions

- **[V]** read on 2026-10-15 in the cited document or image (page text, catalogue record or the map itself).
- **[S]** single source, a search snippet, a catalogue entry or dealer listing that could not be opened, or a reading
  of the scan at overview resolution that the digitising run must confirm.
- **[C]** sources conflict; each value is given.
- Copyright notes are factual, not legal advice, and follow D-A, D-B and D-C: the repository takes **facts** (where a
  line ran, which side held a place on a date) and redraws them by its own method. No map image was copied into the
  repository; downloads stayed in the session scratchpad, with their SHA-256 below.
- All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
  Nothing was logged into or submitted.
- Wikipedia is cited by article and the revision read (en.wiki API, `prop=extracts|revisions`, 2026-10-15).

## 1. Summary of findings

1. **The best line source found for the opening weeks is a CIA map series, not an Israeli one.** The CIA's Near
   East/Africa Branch "Intelligence Summary" (weekly, declassified and released 2013-07-22) carries a map titled
   "Military Situation" with a date, drawn by the CIA Map Branch on a Palestine base [V]:
   - **18 May 1948** (Vol. III No. 19, week ending 19 May 1948; map no. 10981, base "Map Branch, CIA, 4-48"). Legend:
     "Border of strongly-held Jewish area" (heavy solid line), "Border of tenuously-held Jewish area" (heavy dashed
     line), "Arab troop concentration", "Arab advance", "Jewish advance" [V].
   - **25 May 1948** (Vol. III No. 20; map no. 10984, "4-48, 5-48"). Legend: "Border of Jewish held area", "Jewish
     attack", "Arab troop concentration", "Arab troop movement", "British evacuation area" (stippled) [V].
   - **2 June 1948** (Vol. III No. 21). Legend as 25 May, with "Arab attack" [V title and legend at contact-sheet
     size only].
   - The **11 June 1948** map that D-C cites as "Truman Library" is the next edition of the same series: its imprint
     reads "11010 Map Branch, CIA, 6-48 (First Revision 6-48)" [V, Commons copy]. Its legend: "Jewish held area",
     "Jewish controlled area", "Jewish area (as accepted by U.N. General Assembly, November 1947)" [V].
   All are US federal government works, not protected by US copyright (17 USC 105). The weekly for 9 June 1948
   (Vol. III No. 22, CIA-RDP78-01617A004700010006-3, Wayback copy of 2025-03-02, SHA-256
   `b1068eed9edcdc2020a3e59bc6768eeec88983b79eb5bd3ee51d3653eed76116`) has no map [V].
2. **No Israeli line or area map for 14/15 May 1948 was found online.** Searched: the Israel State Archives catalogue
   (HTTP 403 to the project's client again on 2026-10-15), Wikimedia Commons (the 1948 war map categories, file
   searches), the NLI and the Haganah Archive (search snippets only), the IDF History Branch's 1959 "תולדות מלחמת
   הקוממיות" (about 70 maps per a description; not online), the 1957 IDF History Department training-map set (dealer
   listing only, as in D-C). The one dated IDF map on Commons for the day, "מערך חטיבות צה"ל ב-15.5.1948" (published by
   the General Staff, Chief Education Officer, Information Branch), shows **brigade deployment only**: brigade names at
   places, no held areas, no lines [V]. It is not a control source.
3. **The 15 May picture is mostly a coastal and valley core plus islands.** The CIA's own text of 5 May 1948 [V]: "The
   beginning of the unrestricted campaign will find the Jews in substantial possession of the areas allotted to them
   under the partition scheme, except in southern Palestine, where no more than 3,000 Jews are scattered in isolated
   settlements. They strongly hold the coastal area from newly captured Haifa to south of Tel Aviv, including a small
   area in the Arab port of Jaffa. They have consolidated their positions in the Lake Tiberias area of the northeast
   ... They do not, however, hold a firm route of supply from the coast to either Tiberias or Jerusalem."
4. **The south cannot be coloured by side on 15 May.** The 18 May and 25 May CIA maps draw no Jewish-held border in the
   Gaza and Beersheba sub-districts beyond the partition boundary [V at overview resolution; S until digitised]; the
   11 June CIA map labels the Negev "Arab controlled" [V], while ISA 6158/1 (9 July) draws an Israeli-held Negev block
   [V, D-C]. The settlements there were islands among Arab localities, with the Egyptian army moving north from 15 May
   (section 3). Sources disagree **[C]**: grey or no colour, never a side.
5. **The British still held an enclave at Haifa.** "By 14 May 1948, the only British forces remaining in Palestine were
   in the Haifa area and in Jerusalem"; RAF Ramat David was handed over on 26 May and the last British troops left Haifa
   on 30 June (en.wiki "Mandatory Palestine", rev. 1380057030) [V]. S/1025 para. 28 calls it "the Haifa enclave" and
   records the camps dispute settled on 18 June and "all British forces were peacefully evacuated from Haifa by 30 June
   1948" [V]. The 25 May CIA map stipples a "British evacuation area" around Haifa and inland to an "(Air field)" with
   "(7,000)" [V]. No source found gives its boundary to better than that sketch (section 6).
6. **Hex scale.** At the game's 5 km hexes (`content/map/hexes.json`, `hex_size_km: 5.0`) a hex takes the code at its
   centre (D-C bake rule). The Jerusalem corridor, the Etzion bloc, the Jewish Quarter, Mount Scopus, Atarot, Neve
   Yaakov, the north Dead Sea works, Yehiam and each Negev settlement are a hex or less. Today's `truce1` period already
   shows only one Israeli hex in the Jerusalem sub-district (counted on `control_hexes_1948_49.yaml`, 2026-10-15). A
   15 May layer will show the core and the large Arab-held areas; it cannot show the islands as areas.

## 2. Sources

| # | Source | Date shown | What it shows (exactly) | Link (checked 2026-10-15) | Licence / reuse | Tag |
|---|---|---|---|---|---|---|
| 1 | CIA, Near East/Africa Branch, Intelligence Summary Vol. III No. 19, week ending 19 May 1948, page 2: "Military Situation - 18 May 1948", map no. 10981, CONFIDENTIAL | 18 May 1948 | Borders of the strongly-held (solid) and tenuously-held (dashed) Jewish areas; Arab troop concentrations; Arab and Jewish advances (Lebanese troops toward Nabi Yusha; Syrian forces at Banias, toward the Hula and Samakh; Iraqi forces at Naharayim; Arab Legion from the Allenby Bridge to Jerusalem; Egyptian forces up the coast from Khan Yunis and north from Beersheba). Base sources printed: Palestine 1:500,000 (Geo. Philip and Son, 1942), Survey of Palestine Motor Map 1:500,000 (1939), Palestine 1:250,000 (M.D.R. 1509/12375, 1946). The "ARAB" and "JEWISH" area labels are the partition allocation, not control | Record CIA-RDP78-01617A004700010003-6: https://www.cia.gov/readingroom/document/cia-rdp78-01617a004700010003-6 (redirected the project's client to the reading room home page); copy used: https://web.archive.org/web/20250302131758/https://www.cia.gov/readingroom/docs/CIA-RDP78-01617A004700010003-6.pdf (5 pages, page images about 1600 x 2100 px greyscale; scale bar 30 miles = about 346 px, so about 0.14 km a pixel). SHA-256 `56915d81ac0f6f69ecef301f12c71362abb34ba5d6fa7d377b9853bb0cf69cbf` | US federal work, public domain in the US (17 USC 105); its 1:250,000 base is a UK military sheet (facts only, as D-B) | legend, title, imprint [V]; line courses [S] (overview only) |
| 2 | CIA, same series, Vol. III No. 20, week ending 26 May 1948: "Military Situation - 25 May 1948", map no. 10984 | 25 May 1948 | "Border of Jewish held area" (one class, no tenuous class); Jewish attack arrows (one from Acre north to Ras en Naqura; one toward Latrun); Arab troop concentrations (Latrun, Jerusalem, Lydda-Ramle "Arab Legion", Nablus-Tubas, the Lebanese frontier); "Irregulars" in the Galilee and by Qalqilya; the British evacuation area at Haifa | CIA-RDP78-01617A004700010004-5; copy: https://web.archive.org/web/20250302135039/https://www.cia.gov/readingroom/docs/CIA-RDP78-01617A004700010004-5.pdf. SHA-256 `f0d33847131a66a79ead41c7993db6269dcdb88e1b0bb89fa2f8d61ffc70aa8b` | as 1 | legend [V]; lines [S] |
| 3 | CIA, same series, Vol. III No. 21, week ending 2 June 1948: "Military Situation - 2 June 1948" | 2 June 1948 | as 2, with "Arab attack" | CIA-RDP78-01617A004700010005-4; copy: https://web.archive.org/web/20250302122323/https://www.cia.gov/readingroom/docs/CIA-RDP78-01617A004700010005-4.pdf. SHA-256 `b65d3acbcee74c8e33305a8ea17cb3562dc7de37e799234ecc4f1495710c3861` | as 1 | title [V]; content [S] |
| 4 | CIA, same series, Vol. III No. 18, week ending 5 May 1948, text "The situation in Palestine" (no map) | early May 1948 (forecast of the position at the Mandate's end) | the paragraph quoted in section 1 item 3 | CIA-RDP78-01617A004700010002-7; copy: https://web.archive.org/web/20240413190033/https://www.cia.gov/readingroom/docs/CIA-RDP78-01617A004700010002-7.pdf. SHA-256 `6ea52c34b1115c5a48f21b766ded7f6652b2a2a37764f5c4bc072dc59a8fd397` | as 1 | [V] |
| 5 | CIA Map Branch 11010 (6-48, first revision), "Military Situation - 11 June 1948 (On effective date of U.N. cease-fire order)", in the Truman Papers | 11 June 1948 | "Jewish held area" (dense hatch), "Jewish controlled area" (light hatch: the western Galilee and the Acre plain), the partition "Jewish area"; labels "ARAB HELD" (Samaria, Judea) and "ARAB CONTROLLED" (the Negev and the south) | https://commons.wikimedia.org/wiki/File:Palestine_Military_Situation,_June_11,_1948,_Truman_Papers.jpg (685 x 902 px; {{PD-USGov}}, "Source: Truman Library") | US federal work, public domain | [V] |
| 6 | Israel State Archives map 6158/1, red line "קו החזית עם גמר ההפוגה הראשונה, 9.7.1948" | 9 July 1948 | the D-C `truce1` source: the front line at the end of the first truce | as D-C: http://hdl.handle.net/2451/72921 | as D-C (1949 Israeli state work) | [V] (D-C) |
| 7 | UN S/1025, the Mediator's report on the first truce, para. 28 | 11 June - 30 June 1948 | "the Haifa enclave"; British camps; evacuation complete by 30 June | https://www.un.org/unispal/document/auto-insert-184863/ | UN document, not copyrighted | [V] |
| 8 | IDF, Doctrine and Training (Guidance) Brigade, "מפת קרבות מלחמת העצמאות (כוחות קרקע)", 29 sheets, 1:100,000, 1953-54 | each action dated | one box per action, coloured by brigade, with type, result and date (D-C read the legend on sheet 9) | https://commons.wikimedia.org/wiki/Category:Maps_by_Doctrine_and_Guidance_Brigade_(1950s) | Commons {{PD-UKGov}}{{PD-IsraelGov}} | legend [V] (D-C); May 1948 boxes not read [S] |
| 9 | "מפת אתרי מלחמת העצמאות", moreshet-map.org.il: battle sites by phase ("נטילת היוזמה", "בלימת הפלישה"), "as mapped by the IDF History Department in 1954" | per site | sites and dates, not areas | https://moreshet-map.org.il/about-the-war/ | site terms not checked; facts only | about page [V]; site data not read [S] |
| 10 | IDF Spokesperson's Unit / Commons: "מערך חטיבות צה"ל ב-15.5.1948" | 15 May 1948 | brigade deployment (Yiftach, Carmeli, Golani, Alexandroni, Kiryati, Givati, Harel, Etzioni, Sheva, Negev); no areas | https://commons.wikimedia.org/wiki/File:Map_of_IDF_units_in_May_15,_1948.jpg | CC BY-SA 3.0 (IDF permission ticket 2020071210005207) | [V]; not a control source. Its "מג'דל (אשקלון)" label suggests a print after 1948 [S] |
| 11 | US Military Academy, "Israel - War of Independence - Arab Attacks, 15 May - 10 June 1948" (map 36) | 15 May - 10 June 1948 | the partition boundary ("Jewish and Arab communities were on both sides of it"), brigade symbols, Arab attack arrows; notes "Arab Liberation Army Entered 7 June" (in the north). **No control line** | https://commons.wikimedia.org/wiki/File:1948_arab_israeli_war_-_May15-June10.jpg | Commons "Public domain" (US Army) | [V] |
| 12 | Wikipedia articles (dated events, section 3) | per event | see section 3 | en.wiki, revisions in section 3 | CC BY-SA (facts) | [V] for the sentences quoted |
| 13 | Martin Gilbert, "The Routledge Atlas of the Arab-Israeli Conflict" (2002 ed.), map "The Arab invasion of the State of Israel, 15 May 1948" | 15 May 1948 | not seen | https://archive.org/details/routledgeatlasof0000gilb (access-restricted: borrowing needs a login, not used) | copyright | title [S]; not read |
| 14 | Third-party maps: Commons "Israel 15 mai 1948.GIF" (Ceedjee, own work, 2008), "Zones controlled by Yishuv by the 20may48.GIF" (derived), Palestine Remembered "Israeli Controlled Areas As Of May 14 1948", omniatlas | 14-20 May 1948 | the Ceedjee map colours the whole partition-plan Negev as "under Israeli control" on 15 May [V]; the Palestine Remembered page carries its own commenters disputing the map's extent [S] | Commons file pages; https://www.palestineremembered.com/Acre/Maps/Story565.html | CC BY-SA / unknown | never a source (D-A rule); recorded because they **disagree** with sources 1 and 4 in the south |
| 15 | Israel State Archives catalogue: the War of Independence chapter (lists a map "השטחים שכבש צה"ל, 2 בספטמבר 1948", per a search snippet) | 2 Sept 1948 | not 15 May | https://catalog.archives.gov.il/site/chapter/ndependence-war/ (HTTP 403 on 2026-10-15) | Israeli state work | [S]; a lead for later periods only |

Searched and not found (2026-10-15): a Haganah, IDF or ISA map of the areas held on 14 or 15 May 1948; a UN map for
May 1948 (the Truce Commission's reports of May 1948 were not searched map by map); a colour copy of CIA maps 10981 and
10984 (the Truman Library online map pages found give only the 11 June and 6 April 1949 maps).

## 3. Dated events between 14 May and 11 June 1948 (for the overlays and the closure check)

Read 2026-10-15 in en.wiki (revision in brackets). Operation-level facts only; this list is a check on the lines and
must not become a per-village layer (section 5).

| Date | Event (as the source says) | Sub-district | Source [V] |
|---|---|---|---|
| 12-13 May | Givati takes and leaves the detention camp on the Latrun road; 14-15 May its 52nd battalion takes Abu Shusha, al-Na'ani and al-Qubab "thus cutting off the zone from Ramla" | Ramle | Battles of Latrun (1948) (1370603666) |
| 13 May | Jaffa "would eventually fall on 13 May, when Haganah forces entered the city" | Jaffa | Operation Hametz (1370632906) |
| 13 May | Kfar Etzion falls (the Kfar Etzion massacre); the other three Etzion kibbutzim surrender on 14 May | Hebron | Kfar Etzion massacre (1368442104); Gush Etzion (1365397258) |
| 13 May | Operation Ben-Ami launched "with the capture of villages east of Acre cutting the town off from the interior"; Acre surrenders on the night of 17/18 May; second phase from 20 May | Acre | Operation Ben-Ami (1369980718); Acre, Israel (1380244094) |
| 13-18 May | Operation Pitchfork (Kilshon) in Jerusalem as the British leave; "At midnight on Friday 14 May, the British declared their civil and military authority in Jerusalem to be at an end" | Jerusalem | Operation Pitchfork (1372901311) |
| 14 May | Naharayim taken by the Arab Legion and Iraqi forces at 18:00; the Gesher battle runs to 22 May | Beisan | Battle of Gesher (1370110846) |
| 15 May | British forces withdraw from the Latrun monastery and fort; the Legion's first 40 men arrive that night, the regiment by 17 May; "On 18 May ... the road was blocked again" | Ramle / Jerusalem | Burma Road (Israel) (1355307500); Battles of Latrun (1948) |
| 15 May | Egypt invades; Nirim attacked | Gaza | Nirim (1377065151) |
| 16 May | Nabi Yusha depopulated in Operation Yiftach (which ran 28 April - 23 May) | Safad | Al-Nabi Yusha' (1374585152); Operation Yiftach (1369980534) |
| 17 / 18 May | Atarot evacuated (17 May) and Neve Yaakov (18 May) | Jerusalem | Battle for Jerusalem (1374854522); Neve Yaakov (1359170806) gives Atarot 17 May too |
| 18-22 May | Kinarot valley: Syria takes Samakh (18 May), attacks Degania (20 May), occupies Masada and Sha'ar HaGolan; Haganah back in Samakh on 21 May, villagers back on 22 May | Tiberias | Battles of the Kinarot Valley (1373488223) |
| 20 May | Kalia and Beit HaArava (north Dead Sea) abandoned | Jerusalem | Beit HaArava (1374528553); Battle for Jerusalem |
| 24 May | Yad Mordechai falls to the Egyptians | Gaza | Battle of Yad Mordechai (1375608270) |
| 26 May | RAF Ramat David handed to the Israelis | Haifa / Nazareth | Mandatory Palestine (1380057030) |
| 28 May | The Jewish Quarter of the Old City surrenders to the Arab Legion | Jerusalem | Jewish Quarter (Jerusalem) (1371809949) |
| 28 May | Golani occupies Megiddo and Lajjun; Zir'in captured | Jenin | Battle of Jenin (1948) (1370266333); Zir'in (1378510912) |
| 29 May | Isdud occupied by the Egyptian army, "the Egyptians' northernmost position" | Gaza | Isdud (1374880650); Operation Pleshet (1377859533) |
| 31 May - 4 June | Battle of Jenin: Israeli attack, Iraqi counter-attack; Jenin stays Arab-held | Jenin | Battle of Jenin (1948) |
| 5 June | Qaqun attacked by Alexandroni; Yibna occupied | Tulkarm; Ramle | Qaqun (1379771080); Yibna (1375084946) |
| 5-6 June | Battle at al-Malkiyya, which "changed hands no fewer than five times between May and October 1948" | Safad | Al-Malkiyya (1377919542) |
| 7 June | Nitzanim falls to the Egyptian army | Gaza | Battle of Nitzanim (1370270842) |
| 7 June | "Arab Liberation Army Entered 7 June" (north) | Acre / Safad | USMA map 36 (source 11) |
| 10 June | Mishmar HaYarden taken by the Syrian army; Gezer attacked by the Arab Legion | Safad; Ramle | Mishmar HaYarden (moshava) (1345121435); Gezer (kibbutz) (1374530537) |

## 4. Proposed period: 15 May 1948 - 10 June 1948, region by region

Proposed data (for step 2, **not built**): a snapshot `may18` (the CIA 18 May line) in `content/map/control_1948_49.geojson`,
shown by a period `opening` from 1948-05-15 until 1948-06-11 (replacing today's `sub_districts` period, which has no
side colour). Codes as today: `I` Israel; `A` the other side, holder not named; `c` contested; `m` no-man's land; `-`
no colour. Holder on the other side: **`A` (not named) everywhere**. On 15 May the Arab side was local forces and
irregulars with the state armies only entering; the ALA's re-entry in the north is dated 7 June (source 11), so `V`
(ALA, used from 11 June today) is not proposed before then.

The "today at 11 June" column is the D-C `truce1` hex count (codes counted on `control_hexes_1948_49.yaml`, 2026-10-15),
to show what changes.

| Sub-district (D-B) | `truce1` hexes today (11 June) | Proposed for 15 May - 10 June | Basis | Tag |
|---|---|---|---|---|
| Haifa | I 13, V 2, A 1 | **I** on the coast and the Haifa-Jezreel plain inside the 18 May strongly-held border; the Carmel enclaves that ISA 6158/1 draws as Arab-held on 9 July (Tira; Ijzim, Jaba, Ein Ghazal) stay **A** (they were taken on 16 July and 24-26 July: D-C anchors), drawn from the ISA red loops since the CIA map is too coarse for them. The British enclave: section 6 | sources 1, 4, 6, 7 | [V] text; lines [S] |
| Acre | I 3, V 10 | Acre-Nahariya coastal strip: **c** for 15-18 May (Operation Ben-Ami running; Acre surrenders 17/18 May), shown **I** from the 18 May line onward if sub-periods are used (section 4.2); the central Galilee **A**. Yehiam: an island, below the hex scale | sources 1, 2 (the 25 May arrow north to Ras en Naqura), 12 | [S] until traced |
| Safad | I 7, V 4, S 1 | Safad town and the Hula panhandle: inside the 18 May **tenuously-held** border (dashed loop around Safad and east of the Hula) [S]. Proposed: **I** with the info panel saying "tenuously held (CIA, 18 May 1948)", or **c**: owner ruling Q2. The frontier strip (Malkiya, Nabi Yusha) **c** for the whole period (changed hands repeatedly). Mishmar HaYarden: Israeli until 10 June (the existing pin starts 11 June) | sources 1, 12 | [S] |
| Tiberias | I 6, V 2 | Tiberias and the west and south shores of the lake **I** (the 5 May text: "consolidated their positions in the Lake Tiberias area"); the lower Galilee hills west of it **A**; the Kinarot valley (Samakh to Degania) **c** 18-22 May if sub-periods are used, else **I** with a staleness note. Ein Gev: an island | sources 1, 4, 12 | [V] text; lines [S] |
| Beisan | I 6 | **I** (Operation Gideon, Beisan taken in May 1948: en.wiki "Beit She'an" says "captured by the Haganah in May 1948", no day given [S]); Naharayim **A** from 14 May (sub-hex); Gesher **c** 15-22 May | sources 1, 12 | [S] |
| Nazareth | I 4, V 4 | Afula and the Jezreel valley **I**; Nazareth and the hills **A** (Nazareth taken 16 July: D-C anchor). Follow the 18 May line; ISA red as cross-check | sources 1, 6 | [S] |
| Jenin | M 11, I 1 | **A**, except the strip of the Jezreel valley inside the 18 May line. The Megiddo-Lajjun-Zir'in edge, taken 28 May, is **A** on 15 May; `truce1` already has it Israeli | sources 1, 12 | [S] |
| Tulkarm | M 10, I 5 | The coastal plain (Natanya, Hadera) **I** inside the strongly-held border; the hill edge (Tulkarm, Qalqilya, Qaqun until 5 June) **A** | sources 1, 4, 12 | [S] |
| Nablus | M 24 | **A** | sources 1, 2 | [V] (no Jewish-held area drawn) |
| Ramallah | M 10 | **A** | sources 1, 2 | [V] (as above) |
| Jaffa | I 6 | **I** (Tel Aviv; Jaffa from 13 May) | sources 1, 4, 12 | [V] text |
| Ramle | M 6, I 8 | Lydda and Ramle **A** (until 11-12 July: D-C anchors); the Rishon-Sarafand area inside a dashed (tenuous) border on the 18 May map [S]; the Latrun area **c** 15-18 May (the British leave the fort on 15 May, the Legion blocks the road on 18 May), then **A**. Yibna **A** until 5 June | sources 1, 2, 12 | [S] |
| Jerusalem | M 24, I 1 | The city: the existing rule (hexes within 6 km holding both sides become **m**) applies, with the line from the 18 May map (Operation Pitchfork 13-18 May: the city was being fought over). The corridor (Bab el Wad - El Qastal - Jerusalem) inside a dashed (tenuous) border [S]; at 5 km hexes it may show as one or two hexes or none. The Jewish Quarter (to 28 May), Mount Scopus, Atarot (to 17 May), Neve Yaakov (to 18 May): sub-hex, not drawn; info panel only. The north Dead Sea works (Kalia, Beit HaArava, the north Potash works, a dashed loop on the 18 May map [S]) **c** until 20 May, then **A** | sources 1, 7, 12 | [S] |
| Hebron | M 32, I 1 | **A** (the Etzion bloc fell 13-14 May) | source 12 | [V] |
| Gaza | E 11, I 4, M 3 | **[C] grey (c) or no colour (-)**: owner ruling Q3. The coast road and its towns (Gaza, Majdal, Isdud) were Arab localities with the Egyptian army advancing (Isdud on 29 May); the Jewish settlements (Yad Mordechai to 24 May, Nitzanim to 7 June, Negba, Nir Am, Kfar Darom, Nirim and others) were islands. The CIA maps draw no Jewish-held area here; ISA's 9 July line gives an Israeli Negev block. Not a side | sources 1, 2, 4, 5, 6, 12 | [C] |
| Beersheba | I 27, M 20, E 4, - 139 | North of the 60-km northing: **[C] grey or no colour**, as Gaza (Beersheba town Arab-held, Egyptian "since May 1948"; settlements such as Revivim and Nevatim islands). South of it: **-** as today (no sourced line) | sources 1, 5, 12; D-C `SOUTH_N` | [C] |

### 4.1 What the proposal does at the start

From the first turn the map shows the sides as the promo does in the north and centre (the coastal plain, the Jezreel
and Beisan valleys, the Tiberias and Safad areas, Haifa, Tel Aviv-Jaffa on one side; Samaria, Judea, Lydda-Ramle and the
central Galilee on the other), Jerusalem grey, and the south grey or uncoloured. It does not show the Negev settlements,
the corridor or the Jewish Quarter as areas.

### 4.2 Sub-periods (owner ruling Q4)

- **Option A (minimum):** one period, 15 May - 11 June, from the 18 May line, with `c` overlays only for the 15-18 May
  events (Ben-Ami at Acre, Latrun, Jerusalem, Gesher) and the regions in section 4 marked `c` for the whole period.
  Later changes within the period (Yad Mordechai 24 May, the Jewish Quarter 28 May, Lajjun-Zir'in 28 May, Qaqun and Yibna
  5 June, Nitzanim 7 June, Mishmar HaYarden 10 June) are listed as `known_staleness`, as D-C does for October 1948 -
  February 1949. One digitisation.
- **Option B (closer to the sources):** three snapshots from the same CIA series, each shown from its date: 15 May (the
  18 May line, with the 15-18 May overlays), 25 May (map 10984), 2 June (the 2 June map), then `truce1` from 11 June.
  Overlays `c` where consecutive lines differ, for the days between them. Three digitisations of the same kind.

Recommendation: A first (one scan, the sensitive ruling taken once); B only if the owner wants the movement in May shown.

## 5. Uncertainty list

1. **Not an Israeli source [policy].** The line is a US intelligence sketch. `CONTENT_POLICY.md` section 1 prefers
   Israeli official sources; D-C used US maps only as cross-checks. No Israeli line for these dates was found online.
   Owner ruling Q1.
2. **Date offset.** The map is dated 18 May, three days after the game's start; Acre, Latrun, Jerusalem (Pitchfork),
   Atarot, Neve Yaakov, Nabi Yusha and Gesher changed or were fought over in between (section 3).
3. **Symbol clash on the scan (confirmed 2026-10-16, section 9).** The legend's "Border of strongly-held Jewish area" and the base map's "Partition
   Boundary" are both heavy solid black lines on this greyscale scan (the original was presumably in colour; no colour
   copy found). Where they coincide the scan cannot say whether the held border runs there. The CIA text of 5 May
   ("substantial possession of the areas allotted to them under the partition scheme, except in southern Palestine")
   supports reading the coincident stretches as held in the north and centre, but not in the south.
4. **Resolution and generalisation.** About 0.14 km a pixel on the scan, a base compiled at 1:250,000-1:500,000 and
   printed at roughly 1:1,000,000 (the 30-mile scale bar): expected accuracy about +-3-5 km (estimate, not measured),
   worse than ISA 6158/1's +-1-2 km. A hex is 5 km.
5. **"Tenuously held".** The source's own class; whether it is drawn blue or grey is a ruling (Q2).
6. **The south [C].** CIA (no Jewish-held area; "Arab controlled" on 11 June) against ISA 9 July (an Israeli block) and
   third-party maps (the whole partition Negev Israeli). Grey or no colour only.
7. **What lies outside the Jewish-held borders.** The CIA maps draw only Jewish-held borders; they do not say who held
   the rest. Calling it the other side (`A`) is the D-C parity rule. In the Negev and on the Hebron hills' southern
   fringe much of it was held by nobody in strength; hence grey or no colour there.
8. **Readings at overview resolution.** Every line course in section 4 marked [S] was read on the page image at
   overview size only (the dashed loops around Safad and the Hula, the corridor, the north Dead Sea, Rishon-Sarafand).
   The digitising run must confirm each one and list any that cannot be read.
9. **The British enclave's extent** is a stippled sketch on the 25 May map (no line); no other boundary found.
10. **Beisan's capture date** is "May 1948" in the article read; the day (12-13 May, Operation Gideon) was not confirmed
    in a source read today.
11. **Sources not read:** the Gilbert atlas, Morris's "1948", the Carta atlas (Wallach), the IDF 1959 history and the
    2018 Ministry of Defence book "קרבות מלחמת העצמאות, מפות ומבצעים" (copyright; not online); the Doctrine Brigade
    sheets' May 1948 boxes. Any of them could disagree with the CIA line; none was used.

## 6. What can and cannot be drawn honestly

Can be drawn (after approval), from sourced lines only:
- The Israeli core (coast, Haifa, the Jezreel and Beisan valleys, the Tiberias area) and the large Arab-held areas
  (Samaria, Judea, Lydda-Ramle, the central Galilee), from the 18 May CIA borders, closed against the D-B outline by the
  D-C method, side by parity from Tel Aviv.
- The Carmel enclaves from ISA 6158/1's red loops (dated 9 July, held since before 15 May by the D-C anchors).
- Jerusalem as no-man's land (`m`) by the existing rule.
- `c` overlays where the 18 May line and `truce1` differ, limited to the events and days in section 3.

Cannot be drawn honestly:
- The Negev and the southern coastal plain by side (sources disagree; islands among Arab localities).
- The isolated settlements and enclaves as areas (Etzion bloc before its fall, Jewish Quarter, Mount Scopus, Atarot,
  Neve Yaakov, Yehiam, Ein Gev, the Negev settlements): sub-hex; info panel at most. Pinning their hexes (the D-C
  `pinned_towns` mechanism) is possible in code but is an owner decision (Q3), not proposed.
- The British Haifa enclave as a third side: there is no `british` code, its boundary is a sketch, and it lay inside
  the Israeli-held area. Proposed: an info-panel note only ("British forces held an evacuation enclave at Haifa until
  30 June 1948"), no colour.
- A per-village "held by" or capture date for Arab localities: excluded, as D-C section 5 and
  `localities_1948.yaml` ("start of game only") require. Section 3 is a check list for the lines, not a layer.
- Anything from the third-party maps (source 14).

## 7. Rulings needed from the maintainer

- **Q1.** Accept the CIA "Military Situation" map of 18 May 1948 (US government, public domain) as the line source for
  15 May - 10 June, with ISA 6158/1 and the dated events as cross-checks? Or keep no side colour before 11 June (today's
  state) until an Israeli source is found (a request to the IDF Archives or the State Archives would be an outward action
  for the owner)?
- **Q2.** "Tenuously-held Jewish area": blue with an info-panel note, or grey?
- **Q3.** The Gaza and northern Beersheba sub-districts: grey (`c`, "contested") or no colour (`-`, "no sourced line")?
  Pin the Jewish settlement hexes blue there, or not?
- **Q4.** Option A (one period, staleness listed) or option B (three CIA snapshots)?
- **Q5.** The British Haifa enclave: info-panel note only (proposed), or a grey overlay 15 May - 30 June?
- **Q6.** The caption for the period: proposed EN "Front lines, 18 May 1948, approximate (CIA map, 1948)" / HE "קווי
  החזית, 18 במאי 1948, בקירוב (מפת CIA, 1948)"; the D-C legend ("held by") otherwise unchanged.

## 8. Digitising method (step 2, with `tools/control_digitise.py` and `tools/control_bake.py`)

The tool is today ISA-specific (a colour scan with a printed 10-km Palestine grid). The CIA scan is greyscale with only a
lat/long graticule, so step 2 adds a second source to the same tool rather than a new tool:

1. **Source block.** A `CIA_MAY18` dict beside `ISA_MAP` (cite, record URL, the Wayback copy URL, page 2, SHA-256
   `56915d81...cf69cbf`, checked date, licence "US federal work, 17 USC 105; facts only"), and a `--cia-map` argument
   like `--isa-map`. The PDF stays outside the repository; the run checks its SHA-256.
2. **Georeference.** Fit a 2nd-order polynomial from pixel to lon/lat on (a) the neatline graticule ticks printed on
   the map (34°30', 35°, 35°30' E; 31°, 31°30', 32°, 32°30', 33° N) and (b) about 15 town symbols named on the map
   (Haifa, Acre, Safad, Tiberias, Beisan, Jenin, Nablus, Tulkarm, Ramallah, Jerusalem, Hebron, Gaza, Beersheba, Majdal,
   Jaffa) at their `settlements_1948.yaml` / `localities_1948.tsv` coordinates. Print the residuals; fail the run if the
   RMS is over 2 km. Record the fit in `statecraft.georeference`, as D-C does.
3. **Ink mask.** Replace `colour_masks` for this source with an ink-weight mask: dark pixels, opened with a structuring
   element wider than the thin base lines, so that only the heavy lines (the held borders, the partition boundary, the
   frame, arrows) remain. Heavy dashed (tenuous) and heavy solid (strong) are told apart by gap runs along the traced
   path. Lettering and arrows are excluded by the waypoints, not by the mask.
4. **Strokes.** `STROKES` entries `m_strong_*` and `m_tenuous_*` with waypoints read off the scan by hand, snapped and
   routed along the mask as `trace_all` does (record `on_ink` per stroke). Stretches that run on the partition boundary
   are named and listed (`statecraft.not_traced` or a new `on_partition` list), with the CIA 5 May text as their basis
   north of the Gaza sub-district and **no** stroke south of it.
5. **Snapshot.** `SNAPSHOTS["may18"]` built by `build_snapshot` on the Mandate land north of `SOUTH_N`, sides by parity
   from Tel Aviv. The Carmel enclaves are taken from the ISA red strokes `r_carmel_tira` and `r_carmel_ijzim` (already
   traced). The Gaza and northern Beersheba sub-districts become an overlay (`c` or `-` per Q3), cut by the D-B
   sub-district polygons (no new line drawn).
6. **Closure check.** `ANCHORS` rows with windows from 1948-05-15, each sourced: Israel - Tel Aviv, Jaffa (13 May),
   Haifa, Hadera, Natanya, Rehovot, Afula, Beisan, Tiberias, Safed, Nahariya; the other side - Nablus, Jenin, Tulkarm,
   Qalqilya, Ramallah, Lydda, Ramle, Hebron, Bethlehem, Nazareth, Shefa-Amr, Tira and Ijzim (Haifa), Acre until 17 May,
   Gaza, Majdal. A failed anchor fails the run (tolerance 4 km for this source, item 5.4).
7. **Overlays.** `contested_*` for the 15-18 May events (option A) or between consecutive CIA snapshots (option B), each
   as `xor_israel` of two snapshots intersected with a named km box (the method of `contested_ten_days`), each with its
   dates and section 3 sources in `OPERATIONS`.
8. **Periods.** Replace the `sub_districts` period in `build()` by `opening` (1948-05-15 to 1948-06-11, snapshot
   `may18`, legend `held`, `line_date` "1948-05-18"); `control_bake.py` then writes the extra character per hex. The
   `shown_from` in `control_hexes_1948_49.yaml` becomes 1948-05-15. `Control1948.cs` already reads dated periods; its
   `shown_from == periods[0].From` check holds.
9. **Tests.** Extend `tools/tests/test_control.py` (the anchors, the new period, no `I` in the Gaza/Beersheba overlay,
   the SHA-256 check) and `unity/Assets/Tests/EditMode/ControlTests.cs` (the period list starting with `opening`).
   Display only: the sim never reads the file, so the goldens do not move.

Status of every new feature: `draft`, `review: owner-verify (sensitivity review before a shareable build)`,
`source_quality: low_res_scan`.

## 9. Step 2 check: the scan cannot carry the strongly-held border (2026-10-16)

The owner ruled on 15/16 October 2026 (#674): Q1 "Use the CIA map, cited (Recommended)"; Q2 "Contested (Recommended)";
Q3 "No colour + settlement hexes blue (Recommended)"; Q4-Q6 as this note recommends. Before tracing, the scan was
tested for whether the line can be read reliably. **It cannot, outside the dashed (tenuous) borders.** What was done, in
the session scratchpad (nothing committed):

1. **Georeference: good.** The map's own graticule ticks, found by pixel search, were used: 35° and 35°30' E on the top
   neatline (x 742.5 and 1075.5); 34°30', 35° and 35°30' E on the bottom one (x 380.5, 724.5, about 1060); and 33°, 32°30', 32° and
   31°30' N on the right one (y 440, 837.5, 1235.5, 1631). A linear fit leaves residuals under 0.005° and gives about 0.139
   km a pixel. It predicts 26 town symbols to within about 1-21 px (0.1-3 km); most are within 10 px. The D-B sub-district
   boundaries overlaid on the scan fall on the map's own district boundaries.
2. **Line weight does not separate the classes.** On this binarised scan the legend's "Border of strongly-held Jewish
   area" and "Partition Boundary" bars are both about 4 px thick. The first-class roads and the standard-gauge railway are
   2-4 px, and where they run together they merge into wider strokes. A stroke-width map (skeleton width, thresholds 4.5
   and 6 px) marks the road and rail bundles as heavy as often as any border. So no mask picks out the held border.
3. **Agreement cannot be measured.** The ink is too dense. For each ISA 6158/1 9 July red stroke, the share of its
   length within 4 px (0.6 km) of CIA ink was compared with the same stroke shifted about 4 km in six directions:
   r_galilee 0.67 against 0.63 shifted; r_jenin 0.72 against 0.67; r_south_east 0.50 against 0.52; r_south_west 0.51 against
   0.48; r_carmel_ijzim 0.53 against 0.45. Only r_east (it runs on the frontier and the Jordan) stands out (0.89 against
   0.61). A line drawn anywhere lands on ink about as often as the true one, so an on-ink check of a traced border would
   prove nothing.
4. **Read by eye.** The heavy dashed (tenuous) borders are legible: the loop from the frontier near Nabi Yusha down to
   Safad and east to the Jordan, enclosing the Hula; the Latrun - Bab el Wad - El Qastal - Jerusalem corridor; the loop by
   the north Dead Sea works; the Rishon-Sarafand area. The solid (strong) border cannot be told from the partition
   boundary, the roads and the railway in the western Galilee (Acre - Nahariya), the Jezreel - Wadi Ara edge, or the Lydda
   - Ramle area. Those are exactly the places where the 18 May position differs from 9 July.

So a faithful trace of the 18 May strongly-held border is not possible from this copy. Tracing it anyway would mean
choosing among candidate lines by expectation, which is a guess. Options for the owner:

- **(a) A better copy, then trace (recommended for the CIA route).** A colour or full-resolution copy of CIA maps
  10981 / 10984 is needed: NARA RG 263 (CIA records, CREST), or the Truman Library (which holds the colour 11 June
  edition of the same series). Requesting one is an outward action for the owner. Until then, keep today's state: no
  side colour before 11 June.
- **(b) Draw only what is sourced and legible, without a 15 May line.** Show the `truce1` geometry (ISA 6158/1, 9 July)
  from 15 May, with these changes. Contested (`c`) where the CIA 18 May map's dashed borders are legible (Safad-Hula, the
  corridor, the north Dead Sea; traced from the CIA scan). Contested where a sourced change between 15 May and 11 June lies
  inside a `truce1` Israeli area (the Acre-Nahariya coast, the Lajjun-Zir'in edge, Yibna), cut by the D-B sub-district
  polygons so no new line is drawn. No colour in the south, with settlement hexes blue (Q3). Jerusalem `m`. The caption
  would have to say the lines are those of 9 July 1948, adjusted. This is a different source from the one Q1 approved,
  so it needs the owner's ruling.
- **(c) Leave 15 May - 10 June uncoloured** (today's state), and record the CIA series as a cross-check only.

## Sources opened (2026-10-15)

- CIA Near East/Africa Branch Intelligence Summaries, Vol. III Nos. 18-22 (weeks ending 5 May - 9 June 1948), via the
  Wayback Machine copies listed in section 2 (the CIA reading room redirected the project's client) [V].
- Wikimedia Commons: the Truman Papers 11 June 1948 map; the USMA map 36; the IDF 15 May brigade map; the Ceedjee maps;
  category listings "Maps of the 1948 Arab-Israeli War" and "Maps by Doctrine and Guidance Brigade (1950s)" [V].
- UN S/1025 para. 28 [V].
- en.wiki articles and revisions in section 3 [V].
- moreshet-map.org.il about page [V].
- archive.org metadata for the Gilbert atlas (access-restricted) [V for the metadata].
- Search snippets only [S]: Palestine Remembered, omniatlas, the ISA catalogue chapter page (HTTP 403), the NLI and
  Haganah Archive catalogue entries for the 1959 IDF history.
