# The map's northern edge to Beirut: Beirut, Jounieh, the Beirut-Damascus highway and the 1982 fronts

Researched 31 October 2026. Every URL below was opened on that date by an agent. **Research only:** a note for issue #164
(1977-93 item 20, part of #51), the research part the owner opened on 22 Oct and freed again on 31 Oct. It gives
coordinates, sources and the distance to candidate northern edges for the places the 1977-93 design names (section 7.1).
It does **not choose the map's northern edge**, and it changes nothing under `content/`, `tools/`, `sim/` or `unity/`: the
DEM and hex-grid rebuild (#129), `grid_version` and the save change stay paused, and the edge stays the owner's decision
(1957-77 and 1977-93 design D9). A person's check of every source is still pending (CONTENT_POLICY section 2, item 6).
The design is [the 1977-1993 era design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md) (section 7.1,
lines 546-565; item 20, lines 1091-1095). The sibling note for the other edges is
[map-extent-1957-77.md](map-extent-1957-77.md).

## Result in one table

Coordinates are WGS84 decimal degrees, north then east. "Margin to 34.0 N" is the distance from the point to a 34.0 N edge
(the design's recommended value), worked out in section 5 (positive = inside the map, negative = outside). Today's box is
34.20-35.90 E and 29.45-33.35 N (`content/map/hexes.json` `bounds` and `tools/dem_to_heightmap.py` `BOUNDS`, both read 31
Oct); the box proposed for #129 reaches 33.5 N.

| Place | Coordinates, every value read | Tag and source | Margin to 34.0 N |
|---|---|---|---|
| Beirut | 33.8933 N, 35.5016 E (GeoNames); 33.8869 N, 35.5131 E (Wikidata); 33.8981 N, 35.5058 E (en.wikipedia, Beirut); 33.8897 N, 35.5003 E (en.wikipedia, Siege of Beirut); 33.8892 N, 35.5026 E (OpenStreetMap) | [S] five values, **[C]**: they differ by 1.2 km in latitude and 1.2 km in longitude (section 1). The design's 33.898 N, 35.506 E is the Wikipedia value | 11.3 to 12.5 km |
| Jounieh | 33.9808 N, 35.6178 E (GeoNames); 33.9697 N, 35.6156 E (Wikidata and en.wikipedia); 33.9742 N, 35.6201 E (OpenStreetMap) | [S] three databases, **[C]** 1.2 km apart in latitude. The design's "about 33.98 N" is the GeoNames value | 2.1 to 3.3 km |
| Ghazir, just inland of Jounieh | 34.0182 N, 35.6661 E | [S] GeoNames only | -2.0 km (outside) |
| Beirut-Damascus highway, Beirut end (Baabda, Hazmiyeh) | Baabda 33.8341 N, 35.5437 E; Hazmiyeh 33.8553 N, 35.5359 E | [S] GeoNames; Wikidata and OpenStreetMap agree for Baabda to 0.1 km | 18.3 km (Baabda) |
| Beirut-Damascus highway, summit and Bekaa end (Dahr el Baidar, Chtaura, Zahle) | Dahr el Baidar village 33.8094 N, 35.7664 E and pass 33.8167 N, 35.7667 E; Chtaura 33.8167 N, 35.8514 E; Zahle 33.8468 N, 35.9020 E | [S] GeoNames; Zahle also Wikidata and OpenStreetMap, within 0.6 km | 16.9 to 21.1 km |
| 1982 front, Sultan Yacoub | 33.6153 N, 35.8528 E (en.wikipedia, the battle); 33.6433 N, 35.8597 E (Wikidata, the village) | [S] **[C]** 3.1 km apart in latitude: the battle's point is south-south-west of the village point (section 4) | 39.4 to 42.5 km |
| Northern Bekaa (Baalbek) | 34.0058 N, 36.2181 E (GeoNames); 34.0061 N, 36.2086 E (Wikidata, first of two `P625` values; the second, 34.0154 N, 36.2006 E, is 1.0 km further north, -1.7 km against 34.0 N); 34.0097 N, 36.2117 E (OpenStreetMap) | [S] three databases | -0.6 to -1.1 km (outside); also 10.0 to 10.9 km east of a 36.1 E edge |

What the numbers say, in short: Beirut and every highway place the sources name are 11 to 21 km inside a 34.0 N edge;
Jounieh is 2 to 3 km inside it and Ghazir, Baalbek are just outside it, so a 34.0 N edge ends at Jounieh, as the design
says (about 11 km for Beirut and "at the edge" for Jounieh). Everything the design lists in Lebanon is north of the 33.5 N edge
proposed for #129, Sultan Yacoub included (section 6, the one place where the design's text and its own edge disagree).

## Conventions

The same as [map-extent-1957-77.md](map-extent-1957-77.md):

- **[V, agent read 2026-10-31; a person's check pending]** read this session on a primary source opened. Here only the two
  United Nations resolutions of 1982 (sections 4 and 9). A UN text carries the tag for what the Council decided, not for the fact behind it.
- **[S]** single or secondary source: press (JTA, even where the original bulletin page was read), Wikipedia and Wikidata
  at the revision given (`[S, Wikipedia]`, `[S, Wikidata]`), GeoNames and OpenStreetMap (databases). Agreement of
  secondary sources is [S]. Every [S] below was read by an agent on 31 Oct 2026; a person's check is pending.
- **Our arithmetic** a value we worked out ourselves (a conversion of degrees, minutes and seconds, or a distance); the
  inputs and the formula are shown. It never carries the [V] tag.
- **[C]** sources conflict; every value is kept.
- **[GAP]** not found in a source that could be opened. Every [GAP] says what was read or searched.
- No figure, name, page or quote was filled from memory. Quotes are copied from the page.
- **JTA dates.** The date on a JTA archive page is the bulletin date. The event date is read from the dateline on the item
  in the original bulletin PDF (OCR of the page image). The item's dateline is given where it was read.
- **Distances** use 110.57 km per degree of latitude (`tools/geo.py` `KM_PER_DEG_LAT`) and 111.32 x cos(latitude) km per
  degree of longitude (`KM_PER_DEG_LON_EQ`), with the cosine taken at the point's own latitude. A hex row is 7.5 km: the grid's
  hexes have a circumradius of 5.0 km (`tools/hexgrid.py` `HEX_SIZE_KM`), and rows are 1.5 x 5.0 km apart.
- **Negative searches** are in the search log (section 8) with the exact query, URL, count and the day run.

## Source caveat

- **GeoNames** is read from its country dump for Lebanon, `https://download.geonames.org/export/dump/LB.zip`, file
  `LB.txt`, downloaded on 31 Oct 2026 (the file's readme states CC BY 4.0). Each record carries its geonameid and a
  modification date; those dates are given where they matter. A GeoNames point is a database point, not a survey: its
  precision is not stated.
- **Wikidata** is read through `wbgetentities` with the property `P625` (coordinate location); the revision id of the item
  on 31 Oct is given. **OpenStreetMap** is read through the public Nominatim search; it returns the point it chooses to
  represent the object (for a city relation, a centre, not a surveyed centre of the city).
- **No official map with a coordinate grid was opened.** The Lebanese, Israeli and United Nations map services tried were not
  reachable from here (section 8); nothing here rests on them. Every coordinate in this note is therefore a database
  value, [S], and the databases disagree by up to 1.2 km on the two cities.
- **Homonyms.** "Baidar" is a common name in Lebanon (63 features of `LB.txt` have "Baidar" or "Baïdar" in their name or ascii name) and Wikidata holds four
  mountains called "Dahr el-Baidar" at other places. The pass on the highway is the one at 33.8167 N, 35.7667 E (GeoNames
  275915, a pass; the village next to it is 275914); the others in section 3 are rejected by their coordinates.

## 1. Beirut

| Item | Status and source |
|---|---|
| 33.89332 N, 35.50157 E; "Beirut", feature class P, code PPLC (capital); population 1,916,100 in the file | [S] GeoNames 276781, record modified 2026-04-04, in `LB.txt` (opened 31 Oct 2026) |
| 33.886944 N, 35.513056 E, "capital and largest city of Lebanon" | [S, Wikidata] Q3820, `P625`, revision 2537922157 (31 Oct 2026). The item for the airport, Q668329, is 33.821111 N, 35.488333 E (section 4) |
| 33 deg 53' 53" N, 35 deg 30' 21" E = 33.8981 N, 35.5058 E | [S, Wikipedia] en.wiki "Beirut", oldid 1381467548 (infobox `coordinates`). Our arithmetic: 33 + 53/60 + 53/3600 = 33.89806; 35 + 30/60 + 21/3600 = 35.50583 |
| 33.8892265 N, 35.5025585 E, a relation (id 5466662) whose display name begins "Beirut, Basta Tahta, Bashura, Beirut Governorate, Lebanon" and whose type is "harbour" | [S] OpenStreetMap through Nominatim, search `q=Beirut, Lebanon`, first result, 31 Oct 2026 |
| 33.8897 N, 35.5003 E | [S, Wikipedia] en.wiki "Siege of Beirut", oldid 1370739371 (`{{coord}}` marked `source:wikidata`) |

**[C]; all kept.** Latitude runs 33.8869 to 33.8981 (0.0112 degrees x 110.57 = 1.2 km) and longitude 35.5003 to 35.5131
(0.0128 degrees x 111.32 x cos 33.89 = 1.2 km). No source read says which part of the city the point is. The design's
Beirut, "33.898 N, 35.506 E (https://en.wikipedia.org/wiki/Beirut, checked 2026-10-10)", is the Wikipedia infobox value
rounded to three decimals, the largest of the latitudes. Beirut's neighbours on the 1982 front are in section 4:
Hazmiyeh (33.85534 N, 35.53589 E, GeoNames 7838998), Baabda, Khalde and the airport.

## 2. Jounieh

| Item | Status and source |
|---|---|
| 33.98083 N, 35.61778 E; "Jounieh", class P, code PPLA2; population 96,315 in the file | [S] GeoNames 273140, record modified 2026-04-04 |
| 33.969722 N, 35.615556 E, "city in Lebanon" | [S, Wikidata] Q26155, `P625`, revision 2558025082 (31 Oct 2026) |
| 33 deg 58' 11" N, 35 deg 36' 56" E = 33.9697 N, 35.6156 E | [S, Wikipedia] en.wiki "Jounieh", oldid 1378456274 (infobox). Our arithmetic: 33 + 58/60 + 11/3600 = 33.96972; 35 + 36/60 + 56/3600 = 35.61556. Equal to the Wikidata point |
| 33.9741506 N, 35.6200633 E, node 258842202, type "city" | [S] OpenStreetMap through Nominatim, search `q=Jounieh, Lebanon`, first result, 31 Oct 2026 |
| Ghazir 34.01823 N, 35.66611 E (class P, code PPL) | [S] GeoNames 274939, record modified 2020-06-10. A place about 6.1 km north-east of the GeoNames Jounieh point: (34.01823 - 33.98083) x 110.57 = 4.1 km north and (35.66611 - 35.61778) x 111.32 x cos 34 = 4.5 km east, so sqrt(4.1^2 + 4.5^2) = 6.1 km |

**[C]; all kept.** Latitude runs 33.9697 to 33.9808 (0.0111 degrees = 1.2 km). The design writes "Jounieh (about 33.98 N, *to
verify*)": the GeoNames value agrees with it, the Wikidata and Wikipedia value is 1.2 km south of it. All three are south of
34.0 N, by 2.1 to 3.3 km (section 5); Ghazir is north of it.

## 3. The Beirut-Damascus highway

The sources read name the road, not a line of coordinates: neither GeoNames, Wikidata nor the Wikipedia pages read gives a
centreline of the 1982 road. The places on the road's route that the sources name are listed; the road itself is a [GAP]
(section 7).

| Place on the road | Coordinates | Tag and source |
|---|---|---|
| Baabda, "which overlooked the airport" | 33.8341 N, 35.54373 E; 33.834167 N, 35.543611 E; 33.8335501 N, 35.5445655 E | [S] GeoNames 277226 (modified 2026-09-11); [S, Wikidata] Q26454, revision 2550089595; [S] OpenStreetMap node 4752379521. Within 0.1 km |
| Aley | 33.80528 N, 35.6 E; 33.805278 N, 35.6 E; 33.8082697 N, 35.6061181 E | [S] GeoNames 280425; [S, Wikidata] Q26338, revision 2561672270; [S] OpenStreetMap relation 10605904 (a centre, 0.7 km north-east of the GeoNames point) |
| Sofar (GeoNames "Saoufar") | 33.80222 N, 35.69889 E; 33.8020548 N, 35.7012197 E | [S] GeoNames 268097 ("Saoufar"); [S] OpenStreetMap node 5348933838 ("Sawfar"), 0.2 km from the GeoNames point |
| Dahr el Baidar, village and pass | village 33.80944 N, 35.76639 E; pass 33.81667 N, 35.76667 E; 33.8108930 N, 35.7660787 E | [S] GeoNames 275914 and 275915 (both modified 2020-06-10); [S] OpenStreetMap node 6516102718 ("Dahr El Baidar, Baabda District"). Wikidata: no item at this place among its first four hits (section 8) |
| Chtaura | 33.81667 N, 35.85139 E | [S] GeoNames 267297 (modified 2018-03-07) |
| Zahle | 33.84675 N, 35.90203 E; 33.843889 N, 35.907222 E; 33.8477069 N, 35.9024955 E | [S] GeoNames 266045; [S, Wikidata] Q26569, revision 2530473887; [S] OpenStreetMap node 564377442. Within 0.6 km |
| Masnaa (GeoNames "Masnaa", class P) | 33.70228 N, 35.92289 E | [S] GeoNames 10124005 (modified 2015-02-27). A place of that name; this note did not confirm that it is the border post on the highway |

What the sources say the road was in June 1982:

| Item | Status and source |
|---|---|
| "He said that although the main Beirut-Damascus highway is blocked, U.S. special envoy Philip Habib would have no trouble shuttling between the two cities on his peace-making mission. 'We are blocking the road to military supplies but not to friendly civilian movement,' Sharon said." Sharon is the Defense Minister | [S] JTA, bulletin of 15 Jun 1982, "Israel Captures Beirut Airport Link Up with Christian Forces, Palestinians Are Surrounded" (https://www.jta.org/archive/israel-captures-beirut-airport-link-up-with-christian-forces-palestinians-are-surrounded). The item's dateline in the bulletin PDF (http://pdfs.jta.org/1982/1982-06-15_113.pdf, page 1, OCR) reads "TEL AVIV, June 14 (JTA)", so "today" is 14 Jun 1982 |
| "Syrian forces and units of the PLO's Palestine Liberation army are fortifying themselves on the eastern section of the Beirut-Damascus road and in the southeastern suburbs of the city", attributed to "Arab sources" | [S] JTA, bulletin of 17 Jun 1982, "Fighting in Lebanon at Standstill" (https://www.jta.org/archive/fighting-in-lebanon-at-standstill). The item's dateline in the bulletin PDF (http://pdfs.jta.org/1982/1982-06-17_115.pdf, page 2, OCR) reads "JERUSALEM, June 16 (JTA)", so "today" is 16 Jun 1982 |
| "After Ain Zhalta fell, the Israelis advanced to the town of Ain Dara, which overlooked the Beirut-Damascus highway, and captured the heights overlooking the town." Ain Dara is 33.78056 N, 35.72472 E in GeoNames (277476, modified 2018-03-07) | [S, Wikipedia] en.wiki "1982 Lebanon War", oldid 1381378524. No date is given in that sentence; Ain Zhalta is 33.74194 N, 35.69972 E (GeoNames 277378) |
| "... after fourteen hours of fierce combat that raged up through Ain Aanoub and Souq el-Gharb, they broke through the Syrian positions and entered Baabda." Then, after a sentence on reinforcements: "From Baabda, the Israeli force split into three columns, one of which struck across the highway and entered the mountainous area to the northeast, one swung west and took up positions in the steep hills west of Beirut, and one turned toward Kahale, which was further down the highway. ... The Israelis had now cut the Beirut-Damascus highway, cutting off all PLO and Syrian forces in the city." GeoNames: Ain Aanoub 33.78843 N, 35.54093 E (277434); Souq el Gharb 33.79226 N, 35.56451 E (267010); Esh Shwayfate (the article's Shuweifat) 33.80886 N, 35.52071 E (278139); Kahhale 33.822 N, 35.584 E (279823, modified 2026-09-22) | [S, Wikipedia] en.wiki "1982 Lebanon War", oldid 1381378524. The sentence has no date; the paragraph before it is dated 13 and 14 June and the one after it 15 June. The article's "Kahale" is matched to GeoNames "Kahhale" by the name only |
| "The Israelis managed to reach the mountain pass near the village of Dahr el-Baidar, which was the last obstacle before Cthaura." (the article's spelling of Chtaura) | [S, Wikipedia] en.wiki "1982 Lebanon War", oldid 1381378524. The date is not given in the sentence |

## 4. The 1982 fronts

| Item | Status and source |
|---|---|
| The strongest northern positions the sources read describe are around Beirut itself. 14 Jun 1982: "Israeli forces captured Beirut international airport at dawn today and seized a crossroad at Khalde south of Beirut, effectively surrounding Palestinian forces still in the city." | [S] JTA, bulletin of 15 Jun 1982 (URL in section 3); dateline "TEL AVIV, June 14 (JTA)" read as above |
| 16 Jun 1982: "IDF units were present in the eastern, Christian quarters of the city but according to the Cabinet decision, no attempt will be made by Israeli forces to enter the Moslem, western section, where the terrorist headquarters are located." The Cabinet decision is "yesterday", so 15 Jun | [S] JTA, bulletin of 17 Jun 1982 (URL in section 3); dateline "JERUSALEM, June 16 (JTA)" |
| "The Israeli advance on Beirut pressed forward in the west, reaching Khalde on 11 June. Six miles south of Beirut, the town was the last PLO position in front of Beirut Airport." Then, "On 13 June, less than twelve hours after the Israeli-PLO ceasefire had gone into effect, it fell apart, and heavy fighting erupted around Khalde. ... an IDF armored unit struck northeast, attempting to bypass Khalde and advance on Baabda" | [S, Wikipedia] en.wiki "1982 Lebanon War", oldid 1381378524. Dates for Khalde are 11 Jun (reached) in this article and 14 Jun (crossroad seized) in JTA; the two are not the same event and are not reconciled here. [C] on the date only |
| Khalde 33.7832 N, 35.47882 E (GeoNames 6274035, modified 2026-10-22; an older GeoNames record 272635 gives 33.78333 N, 35.48333 E); Wikidata Q15236020 33.78333 N, 35.48333 E, revision 2254775108; OpenStreetMap node 5416951814 33.7817608 N, 35.4857182 E | [S] three databases; the four values lie within 0.7 km |
| Beirut airport 33.82093 N, 35.48839 E (GeoNames 276636, modified 2018-04-06); Wikidata Q668329 33.821111 N, 35.488333 E, revision 2548008884; OpenStreetMap relation 13271009 33.8184914 N, 35.4909121 E | [S] three databases; within 0.4 km |
| 15 Sep 1982: "2. Condemns the recent Israeli incursions into Beirut in violation of the cease-fire agreements and of Security Council resolutions; 3. Demands an immediate return to the positions occupied by Israel before 15 September 1982" | [V, agent read 2026-10-31; a person's check pending] UN Security Council resolution 520 (1982), 17 Sep 1982 (https://documents.un.org/api/symbol/access?s=S/RES/520(1982)&l=en&t=pdf, a one-page excerpt of the Council's volume of resolutions and decisions for 1982; the printed page number was not read). For what the Council recorded, not for the places entered |
| 6 Jun 1982: "1. Demands that Israel withdraw all its military forces forthwith and unconditionally to the internationally recognized boundaries of Lebanon" | [V, agent read 2026-10-31; a person's check pending] UN Security Council resolution 509 (1982), same source (https://documents.un.org/api/symbol/access?s=S/RES/509(1982)&l=en&t=pdf) |
| Sultan Yacoub, the battle of 10-11 Jun 1982: 33 deg 36' 55" N, 35 deg 51' 10" E = 33.6153 N, 35.8528 E (Our arithmetic: 33 + 36/60 + 55/3600 = 33.61528; 35 + 51/60 + 10/3600 = 35.85278) | [S, Wikipedia] en.wiki "Battle of Sultan Yacoub", oldid 1370903591; the `{{coord}}` carries `source:kolossus-hewiki`, that is, it was taken from the Hebrew Wikipedia's article |
| Sultan Yacoub, the village: 33.643333 N, 35.859722 E; GeoNames "Soultane Yaaqoub" 277961 33.64472 N, 35.86111 E and "Soultane Yaaqoub et Tahta" 277960 33.64417 N, 35.84333 E; GeoNames hill "Tell Sultan Yakoub" 12557956 33.64441 N, 35.85855 E | [S, Wikidata] Q2896561, revision 2550367531; [S] GeoNames (modified 2023-09-03 for the hill). The battle's point is 3.1 km (0.0280 degrees x 110.57) south of the village, **[C]**; the design uses 33.615 N, 35.853 E (the Wikipedia battle value) |
| Other places on the 1982 axes, GeoNames points: Sidon 33.55751 N, 35.37148 E (268064); the Awali stream "Nahr el Aouali" 33.5881 N, 35.39424 E (277819, a stream: the point's place on its course is not stated); Damour ("Ed Damour") 33.73012 N, 35.45604 E (280789); Jezzine 33.54167 N, 35.58444 E (273360); El Barouk 33.70639 N, 35.68139 E (280341); Ain Zhalta 33.74194 N, 35.69972 E (277378); Bhamdoun 33.795 N, 35.65111 E (276633); Hammana 33.82583 N, 35.73472 E (274652); Dhour ech Choueir 33.90444 N, 35.72 E (275447) | [S] GeoNames, `LB.txt`. All are south of 34.0 N, by 10.6 km (Dhour ech Choueir) to 50.7 km (Jezzine). Wikidata values where read: Sidon Q163490 33.560556 N, 35.375833 E (a second `P625` value: 33.560413 N, 35.375272 E); Awali Q2914997 33.588803 N, 35.38604 E; Damour Q1158931 33.733333 N, 35.45 E; Hammana Q3114122 33.825278 N, 35.733333 E |

What none of the sources read gives is the northernmost point the IDF reached in June 1982 or the line of the front on any
day; they give places and reports. That is a [GAP] (section 7).

## 5. Distances to the candidate northern edges (the arithmetic)

The formula is (edge - latitude) x 110.57 km, so a positive number is the room between the point and the edge, inside the
map, and a negative number says the point is that far north of the edge. Edges: the box proposed for #129 (33.5 N), the
design's recommended edge (34.0 N), and three edges further north that only mark steps (34.05, 34.1 and 34.15 N); no
option is chosen here. Today's edge is 33.35 N.

| Point (source) | Latitude | 33.5 N | 34.0 N | 34.05 N | 34.1 N | 34.15 N |
|---|---|---|---|---|---|---|
| Beirut, GeoNames | 33.89332 | -43.5 | 11.8 | 17.3 | 22.9 | 28.4 |
| Beirut, Wikidata | 33.88694 | -42.8 | 12.5 | 18.0 | 23.6 | 29.1 |
| Beirut, en.wikipedia | 33.89806 | -44.0 | 11.3 | 16.8 | 22.3 | 27.9 |
| Beirut, OpenStreetMap | 33.88923 | -43.0 | 12.2 | 17.8 | 23.3 | 28.8 |
| Jounieh, GeoNames | 33.98083 | -53.2 | 2.1 | 7.6 | 13.2 | 18.7 |
| Jounieh, Wikidata and en.wikipedia | 33.96972 | -51.9 | 3.3 | 8.9 | 14.4 | 19.9 |
| Jounieh, OpenStreetMap | 33.97415 | -52.4 | 2.9 | 8.4 | 13.9 | 19.4 |
| Ghazir, GeoNames | 34.01823 | -57.3 | -2.0 | 3.5 | 9.0 | 14.6 |
| Baalbek, GeoNames | 34.00583 | -55.9 | -0.6 | 4.9 | 10.4 | 15.9 |
| Baalbek, Wikidata | 34.00611 | -56.0 | -0.7 | 4.9 | 10.4 | 15.9 |
| Baalbek, OpenStreetMap | 34.00965 | -56.4 | -1.1 | 4.5 | 10.0 | 15.5 |
| Baabda, GeoNames | 33.8341 | -36.9 | 18.3 | 23.9 | 29.4 | 34.9 |
| Dahr el Baidar pass, GeoNames | 33.81667 | -35.0 | 20.3 | 25.8 | 31.3 | 36.9 |
| Zahle, GeoNames | 33.84675 | -38.3 | 16.9 | 22.5 | 28.0 | 33.5 |
| Sultan Yacoub, Wikipedia battle point | 33.61528 | -12.7 | 42.5 | 48.1 | 53.6 | 59.1 |
| Sultan Yacoub, Wikidata village | 33.64333 | -15.8 | 39.4 | 45.0 | 50.5 | 56.0 |
| Sidon, GeoNames | 33.55751 | -6.4 | 48.9 | 54.5 | 60.0 | 65.5 |

Worked example: Beirut (GeoNames) against 34.0 N is (34.0 - 33.89332) x 110.57 = 11.8 km; against 33.5 N it is (33.5 -
33.89332) x 110.57 = -43.5 km. The values for the other columns are the same product.

What each edge adds to the grid, by the same constants (Our arithmetic): the rows of the hex grid are 7.5 km apart, so an
edge at 33.5 N is (33.5 - 33.35) x 110.57 / 7.5 = 2.2 rows north of today's, at 34.0 N it is 71.9 km or 9.6 rows north of
today's edge and 55.3 km (7.4 rows) north of the 33.5 N edge, at 34.05 N it is 10.3 rows, at 34.1 N 11.1 rows and at 34.15 N
11.8 rows north of today's edge (the three are 77.4, 82.9 and 88.5 km). The design's "about 9% more area than #129's box" is
its own figure, 0.5 degrees on about 5.8 degrees (0.5 / 5.8 = 8.6 percent); it was not recomputed with other edges.

Elevation tiles. `tiles_for_bounds` in `tools/dem_to_heightmap.py` (lines 38-40, read 31 Oct) takes the one-degree Copernicus
tiles from `floor(lat0)` up to `ceil(lat1)`, so the tile row N33 (33 to 34 N) is the top row for every edge up to and
including 34.0 N, and an edge of 34.05 N or more needs the row N34 in addition (Our reading of the formula; no tile was
fetched). This is a fact about the code as it stands, not a recommendation.

East side, for the one northern place that reaches it: Baalbek is (36.1 - 36.2181) x 111.32 x cos 34.006 = -10.9 km
(GeoNames), -10.0 km (Wikidata) and -10.3 km (OpenStreetMap) against a 36.1 E edge, that is 10.0 to 10.9 km east of it. The
east edge belongs to #129's own research (map-extent-1957-77.md) and is not discussed further here.

## 6. The design's own numbers against the sources

| Design statement (section 7.1 and item 20) | What the sources read give |
|---|---|
| Beirut at 33.898 N, 35.506 E (Wikipedia, checked 2026-10-10) | Wikipedia (oldid 1381467548) agrees. Three other databases give 33.8869 to 33.8933 N and 35.5003 to 35.5131 E: [C] by 1.2 km (section 1) |
| "the Beirut-Damascus highway, cut on 11-14 June 1982 (section 2.3), north of Sultan Yacoub (33.615 N, inside the box)" | "Inside the box" does not hold for the box named in the same section (27.7-33.5 N): 33.615 N is 12.7 km north of 33.5 N (section 5), and the Wikidata village point is 15.8 km north. Both values are kept. JTA reports the highway "blocked" on 14 Jun 1982 and Wikipedia has the IDF on the heights above it at Ain Dara (section 3); Wikipedia says the IDF "had now cut the Beirut-Damascus highway" after entering Baabda, in a passage that gives no date for the cut (the article's dates around it are 13 and 15 June); no source read gives the range "11-14 June" for it |
| "Jounieh (about 33.98 N, *to verify*)" and "Beirut is only about 11 km inside a 34.0 N edge" | Jounieh: 33.9697 to 33.9808 N, so 2.1 to 3.3 km inside a 34.0 N edge. Beirut: 11.3 to 12.5 km inside it. Both statements hold within the sources' disagreement |
| "The Shouf and the northern Bekaa battles (to verify their coordinates in item 20)" | The Shouf places read (Hammana, Aley, Sofar, Bhamdoun, Ain Dara, El Barouk; sections 3 and 4) are 19.3 to 32.5 km south of 34.0 N by the GeoNames latitudes ((34.0 - 33.80528) x 110.57 = 21.5 km for Aley); no battle coordinates were found (section 7). Northern Bekaa: only Baalbek (section 5) |

## 7. Still [GAP]

- **A front-line map of 1982 with a coordinate grid.** Searched: Wikimedia Commons (two queries, section 8); the University of
  Texas map library, the UN cartographic section and the Israeli foreign ministry's site were not reachable (403). None was
  read, so no front-line coordinate is "our reading" of a map either. The sibling note read the IDF atlas for 1973 (section 1.2
  there); no atlas of 1982 was found.
- **The centreline of the Beirut-Damascus highway** and the point where the IDF cut it. The sources read give the road's name
  and the places it passes, not coordinates for the cut or any date range "11-14 June" in the text read.
- **The northernmost point the IDF reached in June 1982** (the design's "Beirut's outskirts") and its September 1982 entry:
  the resolution of 17 Sep 1982 records "incursions into Beirut" without naming places.
- **Battle coordinates for the Shouf and the northern Bekaa** (design: "to verify"). Only places, not battles, were read.
- **A survey-grade coordinate for any place.** Every value here is a database point; a printed gazetteer or an official map
  would settle the 1.2 km spread on Beirut and Jounieh.
- **Wikidata for Sofar and for the Dahr el Baidar pass**: no item at the right place among its first four hits (section 8).
- **The border post at Masnaa**: the GeoNames "Masnaa" is a populated place, not shown to be the post.

## 8. Search log

All searches run on 31 Oct 2026. Tools: `curl` and Python with each service's default request headers (nothing was changed or
set), the built-in tools only; no Tavily key on this box and no web search tool was used for the coordinates.

| Search | Result |
|---|---|
| GeoNames, `https://download.geonames.org/export/dump/LB.zip`, `LB.txt` searched by name (exact, ascii) for Beirut, Jounieh, Baabda, Khaldah, Khalde, Damur, Saida, Sidon, Aley, Zahle, Chtaura, Baalbek, Jezzine, Hammana, Bhamdoun, Hazmiyeh, Ain Zhalta, Ain Dara, Ghazir, Antelias, Dbayeh, El Barouk and by name fragment for Baidar, Masnaa, Sultan, Awali | Found: every place in the tables. Not found under the names tried: "Sofar" (found only as "Saoufar" through the alternate names), "Sultan Yaaqoub" (found as "Soultane Yaaqoub"), the Awali river (found as "Nahr el Aouali"). The name fragment "Baidar" or "Baïdar" returns 63 features (a homonym trap) |
| Wikidata, `https://www.wikidata.org/w/api.php?action=wbsearchentities&search=<name>&language=en&format=json&limit=4`, then `wbgetentities&props=claims\|info` for each hit, for Beirut, Jounieh, Baabda, Sultan Yacoub, Dahr el-Baidar, Sofar, Damour, Awali, Baalbek, Zahle, Sidon, Khalde, Hammana, Aley | Coordinates in the tables. "Dahr el-Baidar": 4 hits, all mountains at 33.15 N, 34.14 N, 33.51 N and 33.33 N, none the pass at 33.8167 N. "Sofar": 4 hits, none a place in Lebanon. Beirut was searched with `limit=3`; the first hit is Q3820 |
| OpenStreetMap, `https://nominatim.openstreetmap.org/search?q=<name>&format=json&limit=1` for 15 queries (and an earlier one for Jounieh that returned the same node) | 13 results, two empty: "Sultan Yaacoub, Lebanon" and "Masnaa border crossing" return `[]` |
| JTA wp-json, `https://www.jta.org/wp-json/wp/v2/archive?search=Baabda&after=1982-06-01T00:00:00&before=1982-07-15T00:00:00&per_page=30` | 2 items (15 and 16 Jun 1982 bulletins). Read: the item of the 15 Jun bulletin; the 16 Jun item was not opened |
| The same URL with `search=Beirut-Damascus` | 17 items, 7 Jun to 12 Jul 1982. Read: the items of the 15 Jun bulletin (also in the Baabda search) and the 17 Jun bulletin; the other 15 were not opened |
| Wikimedia Commons, `https://commons.wikimedia.org/w/api.php?action=query&list=search&srsearch=1982+Lebanon+War+map&srnamespace=6&srlimit=20&format=json`, and a second query with the same URL and `srsearch=מלחמת+שלום+הגליל+מפה` (the Hebrew words for "Peace for Galilee war map") | Titles listed, none opened; no front-line map with a graticule was read |
| Not reachable from here, default headers, one try each: `https://maps.lib.utexas.edu/maps/middle_east_and_asia/` (HTTP 403, after a 301 from `legacy.lib.utexas.edu`); `https://www.un.org/geospatial/content/lebanon` (403); `https://peacekeeping.un.org/sites/default/files/unifil.pdf` (403); the Israeli foreign ministry page for the Kahan Commission report under `https://www.mfa.gov.il/mfa/foreignpolicy/mfadocuments/yearbook6/` (403); `https://geonames.nga.mil/` (no connection); `https://overpass-api.de/api/status` (406, not used) | Recorded, not retried and no header changed |

## 9. Sources opened and when

All opened 31 Oct 2026.

| Source | What was used |
|---|---|
| GeoNames Lebanon dump, `LB.zip` / `LB.txt` | Coordinates and record dates in sections 1 to 5 |
| Wikidata (`wbsearchentities`, `wbgetentities`) | Q3820 (rev 2537922157), Q26155 (2558025082), Q26454 (2550089595), Q2896561 (2550367531), Q26569 (2530473887), Q178835 (2560062346), Q26338 (2561672270), Q668329 (2548008884), Q15236020 (2254775108), Q163490 (2548318515), Q2914997 (2545763764), Q1158931 (2550488720), Q3114122 (2493147411) |
| OpenStreetMap through Nominatim | The relation and node ids in the tables |
| en.wikipedia, by oldid | "Beirut" 1381467548, "Jounieh" 1378456274, "Siege of Beirut" 1370739371, "Battle of Sultan Yacoub" 1370903591, "1982 Lebanon War" 1381378524. "Baabda" (oldid 1374957952) was opened: its coordinate is a template with no values, so it is not used |
| JTA, bulletins of 15 and 17 Jun 1982 (archive pages and the PDFs `1982-06-15_113.pdf`, `1982-06-17_115.pdf`) | Sections 3 and 4; datelines by OCR of the page images |
| UN Security Council resolutions 509 and 520 (1982), `documents.un.org` | Section 4 |
| `tools/geo.py`, `tools/hexgrid.py`, `tools/dem_to_heightmap.py`, `content/map/hexes.json` | Constants and bounds in sections 5 and the status line |
