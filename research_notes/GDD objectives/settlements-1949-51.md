# Settlements founded in 1949-1951: candidates for designated sites (research note, 2026-10-14)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`. Refs #27 (its
1949-51 rows), #384 (GDD Phase 4, the MVP's full site list) and #378 (Phase 1, PR 5, Task 5.0). It continues
[`designated-site-1.md`](designated-site-1.md) (merged in PR #459), which covers Mefalsim, Tifrah and Yeruham; those
three are not repeated here.

The question: which settlements founded between 1 January 1949 and 31 December 1951 are good candidates for the GDD's
designated sites, where a pioneer unit can "Build yishuv" (`docs/GDD.md`, "Pioneer work")? For each: name, founding
date and group, location and hex, fit with Task 5.0's criteria, and factual flags for the owner's sensitivity review.

## How it was done

- Every page was fetched on 14 October 2026 with `curl` and the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`. No request carried a personal
  identifier. A web search tool was used only to find pages; each page cited was then fetched and read directly.
- **The candidate pool** is Haim Gvati's founding-date list (below, "Gvati"): 218 of its rows carry a 1949, 1950 or
  1951 date. Each was matched by name to the Central Bureau of Statistics localities file, and its hex and flags were
  computed (method below). The 30 rows chosen here spread over the regions and types, and were preferred where a
  second source gives the day. Towns that grew from transit camps are not in Gvati's list (it covers rural
  settlements) and were added from the CBS file.
- **Founding dates.** Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural settlements in
  Israel", read in the Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (checked 2026-10-14). The
  appendix has three sections (kibbutzim, moshavim, cooperative moshavim), which give the type used below, and the
  columns name, region, "date of founding", affiliation, origin of the name. The column does not say whether the date
  is the going-onto-the-land day or a later recognition. This is the source used for Mefalsim in PR #459. It is one
  secondary compilation; as `designated-site-1.md` showed for Tifrah, Nahal Oz and Yotvata, it sometimes differs from
  other sources by a day or more.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024 ("קובץ יישובים
  2024"), https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-14), columns
  "שנת ייסוד" (year of founding) and "קואורדינטות". The coordinate field is twelve digits; I read it as an Israel
  Transverse Mercator easting and northing in metres (six digits each) and converted it with `pyproj` (EPSG:2039 to
  EPSG:4326). That reading is mine, not stated in the file; it is borne out by the result: for the 29 rows whose
  Hebrew Wikipedia article has coordinates, the converted point lies 2 to 562 m from them (median about 40 m), and
  for Netiv HaLamed-He 89 m from the English article's; every row gives the same hex either way. Coordinates below are the CBS values, rounded to five decimals.
- **Hexes** were computed with the repo's own rule, `tools/hexgrid.py` `settlement_hex(lon, lat, grid)`, against
  `content/map/hexes.json`, exactly as in `designated-site-1.md` (it reproduces Mefalsim's (-10, 27)). For each hex:
  land and biome from `hexes.json`; any 1948 settlement in it or a neighbour from `settlement_hexes.yaml`; armistice
  line or zone hexes in it or a neighbour from `armistice_hexes_1949.yaml` (the "within one hex" test); localities of
  `localities_1948.tsv` in it or a neighbour, with the straight-line distance from the CBS point.
- **Wikipedia** (Hebrew and English) was read through the MediaWiki API (wikitext of the current revision; the
  revision is cited as `oldid`). It is a lead and a coordinate check. Facts resting on it are tagged and say so.
- Hebrew dates were converted with the `convertdate` library.

Tags, one per fact, as in `designated-site-1.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. The conflict and the sources on each side are stated.

### How the flags are recorded

Recorded factually, for the owner's sensitivity review (`docs/CONTENT_POLICY.md`; Task 5.0, criterion 4). No
judgement is made here.

- **Armistice line.** "On a line hex": the settlement's hex is listed under `line_hexes` in
  `armistice_hexes_1949.yaml`. "Next to a line hex": a neighbouring hex is. The file's own caveats apply: lines are
  snapped to hexes (up to about 4 km from the true line), the Jordan lines are marked low accuracy, and the Jerusalem
  sector is not drawn. A flag says only that the site is near a line on the game map, not that it was outside the
  lines; every settlement here is an Israeli locality in the CBS file.
- **Depopulated Arab localities.** Two kinds of evidence, kept apart:
  1. What sources say about the site itself (Wikipedia articles, Yad Ben-Zvi, the Palmach database). Their wording
     is reported as they give it; where Hebrew and English sources word it differently ("abandoned", "depopulated",
     "emptied"), both are given.
  2. `localities_1948.tsv`: places of 1,000 or more people in 1945 that have no `today` entry, in the same hex or a
     neighbour, with distance. The file does not record what happened to a place; a missing `today` means only that
     its Wikidata item is not a living locality. It also leaves out every place under 1,000 people, so villages named
     by sources (for example Kafr Lam, Ishwa', al-Qabu, Faruna) are not in it. One row is a data gap, not a fate:
     Binyamina, a Jewish locality (type `town`), has no `today` value; it is not counted as a flag.
- **Same hex as another site.** Some candidates share a hex with each other or with a `designated-site-1.md`
  candidate. The plan does not say whether a hex may hold two sites; noted where it happens.

### What the criteria columns mean

Task 5.0 (`docs/superpowers/plans/2026-10-12-gdd-phase-1.md`, PR 5): (1) founded 15 May 1948 to 31 December 1951,
inside the lines, on a land hex with no 1948 settlement; (2) founding date to the day from two independent sources;
(3) a photograph inside the policy; (4) not on the site of a depopulated Arab locality unless stated factually and
seen by the owner. **Criterion 3 was not searched for in this note.** "Hex check" is the plan's Task 5.1 validation
(land, no 1948 settlement).

### Sources not reached

- **CBS localities file 2023**, https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2023.xlsx: HTTP 200
  with an HTML page titled "Oops, Something is wrong", not a spreadsheet. The 2024 and 2022 files were reached; the
  2024 file is used.
- **Kibbutz Lohamei HaGeta'ot timeline**, https://www.loh.org.il/viewpage_mobile.asp?pagesCatID=1588: HTTP 404 ("page
  not found").
- **Council for Conservation of Heritage Sites, Beit Shemesh page**,
  https://shimur.org/%D7%94%D7%A2%D7%99%D7%A8-%D7%91%D7%99%D7%AA-%D7%A9%D7%9E%D7%A9-%D7%90%D7%AA%D7%A8%D7%99%D7%9D-%D7%A9%D7%94%D7%95%D7%9B%D7%A8%D7%96%D7%95-%D7%9C%D7%A9%D7%99%D7%9E%D7%95%D7%A8/:
  HTTP 404.
- **Mevo Beitar "About" page** cited by English Wikipedia for the day,
  http://www.mevobeitar.co.il/index.php/en/about/about-mevo-beitar: HTTP 404. The village's home page was reached.
- **Not tried, known blocked** (`designated-site-1.md`): the NLI historical press (JPress) and the Israel State
  Archives catalogue. So **no contemporaneous newspaper was read for any row**. Several Hebrew Wikipedia dates rest
  on newspaper reports it cites (Al HaMishmar, HaTzofe, HaBoker, Sha'arim, Davar); I give the citation as Wikipedia
  gives it and do not count the newspaper as read.
- **Not tried**: Knesset records, the Jewish Agency Settlement Department's publications, KKL archive catalogues, and
  most regional council sites (four were read: Gan Rave, Merhavim, Mateh Asher, Hevel Eilot; plus the Galilee
  Development Authority). Books cited by Wikipedia and not read: Benny Morris, *The Birth of the Palestinian Refugee
  Problem* (Hebrew edition, pp. 250, 253, 594); Walid Khalidi, *All That Remains* (1992); Ze'ev Vilnai, *Ariel
  Encyclopedia*, vol. 7; the Mapa concise gazetteer of Israel.

## Summary table

Coordinates: CBS 2024 (lat N, lon E). "Day" is the Gvati date unless the row says otherwise. Hex status: all 30 hexes
are land.

| # | Name | Type | Founded | Region | Lat, lon (CBS) | Hex (q, r) | Hex check | Crit. 1 / 2 / 4 | Flags |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Sasa / סאסא | Kibbutz | 13 Jan 1949 **[V]** | Upper Galilee | 33.02731, 35.39415 | (11, 5) mountains | Pass | Yes / Yes / No | Next to Lebanon line; Sa'sa' 0.2 km |
| 2 | Yas'ur / יסעור | Kibbutz | 6 Jan 1949 **[V]** | Western Galilee | 32.90113, 35.16539 | (7, 7) coastal plain | Pass | Yes / Yes / No | al-Birwa 1.5 km |
| 3 | Gesher HaZiv / גשר הזיו | Kibbutz | 27 Jan 1949 **[V]** | Western Galilee | 33.03859, 35.11166 | (8, 4) coastal plain | Pass | Yes / Yes / No | On Lebanon line hex; az-Zib 1.4 km |
| 4 | Lohamei HaGeta'ot / לוחמי הגטאות | Kibbutz | 27 Jan, 3 Feb or Feb 1949 **[C]** | Western Galilee | 32.96373, 35.09800 | (7, 6) coastal plain | **Fails: Acre's hex** | No / No / No | al-Sumayriyya (sources) |
| 5 | HaSolelim / הסוללים | Kibbutz | 21 Jul 1949 **[S]**; July **[V]** | Lower Galilee | 32.75067, 35.23758 | (7, 9) hills | Pass | Yes / No / No | Saffuriya 3.9 km |
| 6 | Meron / מירון | Moshav | 22 May 1949 **[S]**; 1949 **[V]** | Upper Galilee | 32.98738, 35.43982 | (11, 5) mountains | Pass | Yes / No / No | Next to Lebanon line; Meron village (sources); hex shared with Sasa |
| 7 | Kiryat Shmona / קריית שמונה | Transit camp, then town | 18 Jul 1949 first families; 26 Dec 1949 camp; CBS 1950 **[C]** | Hula valley | 33.21316, 35.57461 | (14, 2) hills | Pass | Yes / No / No | On Lebanon line hex; al-Khalisa 0.7 km |
| 8 | Megiddo / מגידו | Kibbutz | 2 Feb 1949 (kibbutz's own text; its Hebrew date reads as 1 Feb, our reading) vs 1 Feb (Gvati list) **[C]** | Jezreel valley | 32.58041, 35.17968 | (5, 11) coastal plain | Pass | Yes / No / No | Next to Jordan lines; Lajjun 600 m (sources) |
| 9 | Rehov / רחוב | Moshav | 13 Sep 1951 **[V]** | Beit She'an valley | 32.45080, 35.48915 | (8, 13) valley | Pass | Yes / Yes / No | Next to Jordan line; Faruna (source) |
| 10 | Ma'agan Michael / מעגן מיכאל | Kibbutz | 25 Aug 1949 **[V]** | Carmel coast | 32.55597, 34.91571 | (2, 12) coastal plain | Pass | Yes / Yes / See note | Area depopulated except one tribe (en Wikipedia); hex shared with Or Akiva |
| 11 | HaBonim / הבונים | Cooperative moshav | 18 Jul 1949 **[S]** | Carmel coast | 32.63663, 34.93271 | (3, 10) coastal plain | Pass | Yes / No / No | Kafr Lam (sources) |
| 12 | Beit Zayit / בית זית | Moshav | 24 Oct 1949 **[S]** | Jerusalem corridor | 31.78211, 35.16187 | (-1, 23) mountains | Pass | Yes / No / No | On Jordan line hex; next to Jerusalem's hex; Ein Karim 1.7 km |
| 13 | Eshtaol / אשתאול | Moshav | 11 Dec 1949 **[S]**; Dec **[V]** | Jerusalem corridor | 31.78113, 35.00914 | (-3, 23) hills | Pass | Yes / No / No | On Jordan line hex; Ishwa' and Islin (sources) |
| 14 | Mevo Beitar / מבוא ביתר | Cooperative moshav | 24 Apr 1950 **[S]** | Jerusalem corridor | 31.72247, 35.10656 | (-2, 24) hills | Pass | Yes / No / No | On Jordan line hex; al-Qabu (source) |
| 15 | Netiv HaLamed-He / נתיב הל"ה | Kibbutz | 16 Aug 1949 **[V]** | Elah valley | 31.68832, 34.98292 | (-4, 25) hills | Pass | Yes / Yes / No | On Jordan line hex; Beit Nattif 1.5 km |
| 16 | Beit Shemesh / בית שמש | Transit camp, then town | 6 Dec 1950 **[S, Wikipedia]** | Jerusalem corridor | 31.73178, 34.98828 | (-3, 24) hills | Pass | Yes / No / See note | Next to Jordan lines; Deiraban 2.4 km |
| 17 | Palmachim / פלמחים | Kibbutz | 11 Apr 1949 **[V]** | Coastal plain | 31.93357, 34.70625 | (-5, 21) coastal plain | Pass | Yes / Yes / No | Nabi Rubin 2.6 km |
| 18 | Kfar Truman / כפר טרומן | Moshav | 18 Jul 1949 **[V]** | Lod area | 31.98049, 34.92423 | (-2, 20) coastal plain | Pass | Yes / Yes / No | Next to Jordan lines; Beit Nabala 3.1 km |
| 19 | Nir Galim / ניר גלים | Cooperative moshav | 19 Nov 1949 **[S]** | Coastal plain | 31.82415, 34.68206 | (-6, 23) coastal plain | Pass | Yes / No / No | Arab Suqrir (source) |
| 20 | Rosh HaAyin / ראש העין | Immigrant camp, then town | 1949 or 1950 **[C]** | Sharon edge | 32.09448, 34.95867 | (-1, 19) coastal plain | Pass | Yes / No / No | On Jordan line hex; Majdal Yaba 1.5 km |
| 21 | Or Akiva / אור עקיבא | Transit camp, then town | 17 Apr 1951 **[S, Wikipedia]** | Carmel coast | 32.50776, 34.91984 | (2, 12) coastal plain | Pass | Yes / No / **[C]** | Barrat Qisarya (contested in source); hex shared with Ma'agan Michael |
| 22 | Beit Kama / בית קמה | Kibbutz | 18 Apr 1949 **[V]** | Northern Negev | 31.44853, 34.76108 | (-8, 28) hills | Pass | Yes / Yes / Yes | None |
| 23 | Gilat / גילת | Moshav | 4 Dec 1949 **[V]** | Northern Negev | 31.32798, 34.65203 | (-10, 30) lowland | Pass | Yes / Yes / Yes | None; hex shared with Tifrah |
| 24 | Patish / פטיש | Moshav | 3 or 4 Mar 1950 **[C]** | Northern Negev | 31.32739, 34.56004 | (-11, 30) lowland | Pass | Yes / No / Yes | None |
| 25 | Re'im / רעים | Kibbutz | 20 Oct 1949 **[V]** | Western Negev | 31.38552, 34.45932 | (-12, 29) coastal plain | Pass | Yes / Yes / Yes | Next to 1950 Gaza lines |
| 26 | Nahal Oz / נחל עוז | Nahal outpost, then kibbutz | 15, 25 or 26 Jul 1951 **[C]** | Gaza border | 31.47277, 34.49746 | (-11, 28) coastal plain | Pass | Yes / No / Yes | Next to Gaza lines; next to Gaza's hex |
| 27 | Sderot / שדרות | Transit camp, then town | April 1951 **[S]** | Western Negev | 31.52901, 34.59434 | (-9, 27) coastal plain | Pass | Yes / No / No | On Gaza line hex; Najd (source) |
| 28 | Beit Guvrin / בית גוברין | Kibbutz | 23 May or 2 Jun 1949 **[C]** | Lachish area | 31.61302, 34.89595 | (-5, 26) hills | Pass | Yes / No / No | On Jordan line hex; Bayt Jibrin 0.9 km |
| 29 | Ahuzam / אחוזם | Moshav | 2 or 30 Oct 1950 **[C]** | Lachish area | 31.55340, 34.77020 | (-7, 26) coastal plain | Pass | Yes / No / See note | Abu Jabir (source); 'Iraq el Manshiya 5.8 km |
| 30 | Yotvata / יטבתה | Nahal outpost, then kibbutz | 30 or 31 Oct 1951 **[C]** | Arava | 29.89552, 35.05929 | (-16, 51) desert | Pass | Yes / No / Yes | On Jordan (Arava) line hex |

"Crit. 4: Yes" means no source read places the settlement at or next to a depopulated locality and no
`localities_1948.tsv` place without a `today` entry is in its hex or a neighbour. It is absence of evidence in the
sources read, not a gazetteer check.

## Northern regions

### 1. Sasa (סאסא), kibbutz

- **Founded 13 January 1949. [V]** Gvati: Sasa, Upper Galilee, 13.1.49, HaKibbutz HaArtzi. Hebrew Wikipedia: founded
  13 January 1949, footnoted to Benny Morris, *The Birth of the Palestinian Refugee Problem*, p. 253 (not read).
  English Wikipedia: January 1949. CBS: 1949. Galilee Development Authority page: "year of founding: 1949".
- **Founders. [V]** A garin of Hashomer Hatzair from North America: Hebrew Wikipedia ("Garin Aliyah 5" of Hashomer
  Hatzair, from North America) and English Wikipedia. Gvati: HaKibbutz HaArtzi.
- **Location and hex.** CBS 33.02731, 35.39415 (Hebrew Wikipedia 52 m away). Hex (11, 5), mountains. No 1948
  settlement in it; Safed's hex is a neighbour.
- **Suitability.** Criteria 1 and 2 met; criterion 4 not (below). Hex shared with Meron (row 6).
- **Flags.** Next to a line hex: `lebanon_adl`. English Wikipedia: founded "on the land of the depopulated
  Palestinian village of Sa'sa'". `localities_1948.tsv`: Sa'sa' 0.2 km, same hex, no `today`. Note for content: that
  file already uses the id `sasa` for the Arab village, so a site id `sasa` would collide.
- **Sources.** https://benyehuda.org/read/14684 ; https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx ;
  https://he.wikipedia.org/w/index.php?oldid=43769424 ; https://en.wikipedia.org/w/index.php?oldid=1380144019 ;
  http://www.romgalil.org.il/cds/105/ . All checked 2026-10-14.

### 2. Yas'ur (יסעור), kibbutz

- **Founded 6 January 1949. [V]** Gvati: Yas'ur, Western Galilee, 6.1.49, HaKibbutz HaArtzi. Hebrew Wikipedia: "the
  kibbutz was founded on 6 January 1949". English Wikipedia and the Mateh Asher Regional Council page: 1949. CBS: 1949.
- **Founders. [V for movement, S for origin]** Hashomer Hatzair (council page; Gvati's HaKibbutz HaArtzi; both
  Wikipedias). English Wikipedia: immigrants from Hungary, joined in 1951 by a group from England.
- **Location and hex.** CBS 32.90113, 35.16539 (Hebrew Wikipedia 58 m). Hex (7, 7), coastal plain. No 1948
  settlement; Acre's hex is a neighbour.
- **Suitability.** Criteria 1 and 2 met; 4 not.
- **Flags.** No line within one hex. Hebrew Wikipedia: founded "on the lands of the abandoned Arab village al-Birwa",
  citing Morris, pp. 250, 253, 594. English Wikipedia: "on the land of the depopulated former Palestinian village of
  Al-Birwa", citing Khalidi. `localities_1948.tsv`: Birwa 1.5 km and Damin 3.1 km, same hex, no `today`.
- **Sources.** Gvati and CBS (as row 1); https://he.wikipedia.org/w/index.php?oldid=42959937 ;
  https://en.wikipedia.org/w/index.php?oldid=1374538155 ; https://mta.org.il/he/340/ . All checked 2026-10-14.

### 3. Gesher HaZiv (גשר הזיו), kibbutz

- **Founded 27 January 1949. [V]** Gvati: Gesher HaZiv, Western Galilee, 27.1.49, HaIhud. Hebrew Wikipedia: founded
  27 January 1949. CBS: 1949. **[C, minor]** English Wikipedia's text says "founded in 1948" while its infobox and
  category say 1949.
- **Founders. [V]** Part of the evacuated members of kibbutz Beit HaArava (north of the Dead Sea) together with
  Habonim immigrants from North America: Hebrew and English Wikipedia. English Wikipedia: 120 people of the first
  Habonim garin from North America **[S]**.
- **Location and hex.** CBS 33.03859, 35.11166 (Hebrew Wikipedia 61 m). Hex (8, 4), coastal plain. No 1948
  settlement; Hanita's hex is a neighbour.
- **Suitability.** Criteria 1 and 2 met; 4 not; the line hex is a design question (as for Mefalsim).
- **Flags.** **On a line hex**: `lebanon_adl` (the international boundary). Hebrew Wikipedia: founded on an abandoned
  British army base and "on the lands of the emptied Arab village a-Zib". English Wikipedia: "on land near the former
  Palestinian village of Az-Zeeb, close to the village site". `localities_1948.tsv`: Zib 1.4 km and Bassa 5.0 km,
  same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43555677 ;
  https://en.wikipedia.org/w/index.php?oldid=1374530444 . All checked 2026-10-14.

### 4. Lohamei HaGeta'ot (לוחמי הגטאות), kibbutz

- **Founded: [C].** Gvati: 3.2.49, HaKibbutz HaMeuhad. Hebrew Wikipedia: "on 27 January 1949 the kibbutz was
  founded", and "the first members went onto the land in February 1949". English Wikipedia and CBS: 1949. A web
  search summary mentioned a ceremony on 18 April 1949; I did not find that in a page I read, so it is not used.
- **Founders. [V]** Holocaust survivors, including ghetto fighters: both Wikipedias.
- **Location and hex.** CBS 32.96373, 35.09800 (Hebrew Wikipedia 49 m). Hex (7, 6), coastal plain.
- **Suitability. Unsuitable as the plan stands: the hex holds Acre**, a 1948 settlement (`settlement_hexes.yaml`), so
  Task 5.1's validation fails. Kept in the table because it is one of the best-documented foundings of 1949.
- **Flags.** No line within one hex. Hebrew Wikipedia: land allocated in 1948 next to the British army camp and "on
  the lands of the Arab village a-Sumeiriya". English Wikipedia: on the site of an abandoned British Army base "and
  the depopulated Palestinian village of al-Sumayriyya". al-Sumayriyya is not in `localities_1948.tsv`; Manshiya
  3.2 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43828032 ;
  https://en.wikipedia.org/w/index.php?oldid=1379780578 . All checked 2026-10-14.

### 5. HaSolelim (הסוללים), kibbutz

- **Founded 21 July 1949. [S for the day, V for July 1949]** Gvati: 21.7.49, HaOved HaZioni. English Wikipedia:
  "established in July 1949". CBS: 1949.
- **Founders. [S]** English Wikipedia: members of Maccabi Hatzair and of the General Zionist movement in America;
  first named Tzippori B.
- **Location and hex.** CBS 32.75067, 35.23758 (Hebrew Wikipedia 8 m). Hex (7, 9), hills. No 1948 settlement;
  Nahalal's and Nazareth's hexes are neighbours.
- **Suitability.** Criterion 1 met; 2 not (one source for the day); 4 not.
- **Flags.** No line within one hex. English Wikipedia: from February 1949 land near Saffuriya was distributed to
  neighbouring Jewish villages, HaSolelim among them. `localities_1948.tsv`: Saffuriya 3.9 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43975880 ;
  https://en.wikipedia.org/w/index.php?oldid=1374531153 . All checked 2026-10-14.

### 6. Meron (מירון), moshav

- **Founded 22 May 1949. [S for the day, V for 1949]** Gvati: 22.5.49, Hapoel HaMizrachi. Both Wikipedias and CBS:
  1949.
- **Founders. [V]** Hapoel HaMizrachi (Gvati, English Wikipedia); English Wikipedia: Orthodox soldiers discharged
  after the war **[S]**.
- **Location and hex.** CBS 32.98738, 35.43982 (Hebrew Wikipedia 38 m). Hex (11, 5), mountains, shared with Sasa.
  Safed's hex is a neighbour.
- **Suitability.** Criterion 1 met; 2 not; 4 not.
- **Flags.** Next to a line hex: `lebanon_adl`. Hebrew Wikipedia: founded "on the lands of the Arab village Meron,
  destroyed in the War of Independence"; the moshav's name keeps the ancient name preserved in the village's. English
  Wikipedia: "at the location of the former depopulated Palestinian village of Meiron". Not in
  `localities_1948.tsv` (under 1,000 people); Sa'sa' 6.2 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43575980 ;
  https://en.wikipedia.org/w/index.php?oldid=1375017312 . All checked 2026-10-14.

### 7. Kiryat Shmona (קריית שמונה), transit camp, then town

- **Founded: [C].** Three dates for three things, plus a CBS year:
  - 18 July 1949: 14 families of immigrants from Yemen arrived (Hebrew and English Wikipedia; one line of evidence).
  - July 1949: "Kiryat Shmona was founded in July 1949" (Yad Ben-Zvi, "Israel Revealed to the Eye", Kiryat Shmona
    page). Agrees with the month above.
  - 26 December 1949 (6 Tevet 5710): Hebrew Wikipedia says Kiryat Shmona was set up as a ma'abara on that day. The
    two calendars agree.
  - CBS year of founding: 1950.
- **Names. [S]** First called Kiryat Yosef, renamed Kiryat Shmona in June 1950 (Hebrew and English Wikipedia).
- **Location and hex.** CBS 33.21316, 35.57461 (Hebrew Wikipedia 214 m). Hex (14, 2), hills. No 1948 settlement.
- **Suitability.** Criterion 1 met (a town that grew from a camp, which the plan allows as a "development town"); 2
  not; 4 not. Like Yeruham, it was set up by agencies for immigrants, not built by a pioneer garin.
- **Flags.** **On a line hex**: `lebanon_adl`; next to `syria_adl`. Yad Ben-Zvi: founded "on the remains of the Arab
  village Halsa". Hebrew Wikipedia: "on the ruins of the abandoned Arab village al-Khalisa (Halsa), whose residents
  fled in the War of Independence up the mountain to southern Lebanon". English Wikipedia: "on the site of the former
  depopulated Palestinian Arab village of Al-Khalisa", citing Vilnai's *Ariel Encyclopedia*. `localities_1948.tsv`:
  Khalisa 0.7 km, Zuq et Tahtani 2.5 km, Hunin 2.8 km and Na'ima 3.4 km, all same hex, no `today`.
- **Sources.** CBS; https://israelalbum.org.il/mercazim/%D7%A7%D7%A8%D7%99%D7%99%D7%AA-%D7%A9%D7%9E%D7%95%D7%A0%D7%94/ ;
  https://he.wikipedia.org/w/index.php?oldid=44043359 ; https://en.wikipedia.org/w/index.php?oldid=1376419614 . All
  checked 2026-10-14.

### 8. Megiddo (מגידו), kibbutz

- **Founded: [C, one day; the kibbutz's own quoted line may be internally inconsistent, on our reading].** Tags below are [S] unless stated;
  "agent read 2026-10-23; a person's check pending" applies to every page added here that was opened.
  - Gvati: 1.2.49, HaKibbutz HaArtzi (the appendix list, as the note already cites it); his prose, on the
    benyehuda.org page above, says four kibbutzim "went onto the land in Shvat 5709 (February 1949)" and names
    Megiddo first but gives no day. [S]
  - The kibbutz's own pages. (a) An excerpt of its founding scroll, shown on the settlement list at ofra.org.il
    (page "אודות קיבוץ מגידו"): "היום, יום ב' בשבט תש"ט (02.02.1949), שנה ראשונה למדינת ישראל, עלינו כאן חברי הקיבוץ
    'השומר הצעיר' על שם יוסף קפלן ואנו מניחים יסוד לישוב חדש במולדת" ("Today, 'yom bet' of Shvat 5709 (02.02.1949),
    the first year of the State of Israel, we members of the kibbutz 'HaShomer HaTza'ir' named after Yosef Kaplan
    came up here and lay the foundation of a new settlement in the homeland"), marked "(from the founding scroll)".
    The scroll itself was not seen, only this quotation on a third-party list. [S]
    (b) Two member memorial pages on megiddo.co.il (objDoc OID 889907 and OID 889911)
    carry the line "2.2.1949 יום העלייה על הקרקע" (2.2.1949, the day of going onto the land); the first page's life story
    says he "עלה ב-2 בפברואר 1949 עם קבוצת המייסדים על הקרקע במגידו", the second's "בשניים בפברואר 1949 עלה עם חברי
    הגרעין להקים את מגידו". These were written decades later; they show what the kibbutz records, not an event
    report. [S]
  - **Our reading of the scroll's Hebrew date.** The quotation has no day-of-month number: "יום ב' בשבט" can
    mean "the 2nd of Shvat" or "Monday in Shvat". We read it as the 2nd of Shvat, which is our interpretation. On
    that reading it falls on Tuesday 1 February 1949, while Wednesday 2 February 1949 is 3 Shvat (our reading,
    `convertdate`; 1 Shvat 5709 = Monday 31 January 1949); the Hebrew day would then agree with Gvati's 1.2.49 and
    not with the Gregorian date in the same line. Read as "Monday", it matches neither 1 nor 2 February (Mondays
    in Shvat 5709 are 31 January and 7 February). The reading is ours and does not show which date is right. [C]
  - Hebrew Wikipedia (oldid 43556011): "on 2 February 1949 kibbutz Megiddo was founded". The reference attached to
    that sentence is not a newspaper but a note on a 1951 survey map (NLI, Eran Laor map collection). The Al HaMishmar
    item of 2 February 1949, p. 15, is cited elsewhere in the article, after a sentence about the group's members
    coming from Mishmar HaEmek, under the headline "קיבוץ השוה"צ ע"ש יוסף קפלן עלה על הקרקע בעמק המערבי" ("the
    Hashomer Hatzair kibbutz named after Yosef Kaplan went onto the land in the western valley"). So the article's
    own citation gives us the headline only, in a paper dated 2 February; what day the headline's event fell on is
    not stated in it. The
    article's sentence that in "February 1949" they went onto the land opposite the mouth of Nahal Iron is cited
    to Al HaMishmar of 4 May 1949, p. 44 (not read by us). Wikipedia is a citation here, not a source we read
    through. [S]
  - English Wikipedia (oldid 1374533944): 2 February 1949. CBS: 1949. The kibbutz archive's "historical background"
    page repeats the Hebrew Wikipedia text, so it is not independent.
  - **[GAP]** The Al HaMishmar item of 2 February 1949 (and 4 May 1949) was not opened: nli.org.il returns a
    Cloudflare "Checking your browser" page (HTTP 403) to curl, and the Jpress address returns 403. A JTA archive
    search for Megiddo in February 1949 found nothing. So no contemporary record has been read, and the one-day gap
    stays open: the sources that say 2.2 are the kibbutz's later text (the scroll excerpt, the member memorial
    pages written long after) or Wikipedia, and on our reading the scroll excerpt's Hebrew date points to
    1 February.
- **Founders. [V]** A Hashomer Hatzair group named after Yosef Kaplan, Holocaust survivors from Poland and Hungary:
  both Wikipedias, the archive page.
- **Location and hex.** CBS 32.58041, 35.17968 (Hebrew Wikipedia 37 m). Hex (5, 11), coastal plain. Nahalal's hex is
  a neighbour.
- **Suitability.** Criterion 1 met; 2 not (one day apart); 4 not.
- **Flags.** Next to line hexes: `jordan_adl_iraqi_b_wadi_ara`, `jordan_adl_iraqi_c_east`. Hebrew Wikipedia: founded
  "on the lands of the destroyed village" (al-Lajjun). English Wikipedia: built 600 m north-east of the site of "the
  depopulated Arab village of Lajjun"; the members first settled on its ruins. Lajjun is not in
  `localities_1948.tsv`; Mansi 1.7 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43556011 ;
  https://en.wikipedia.org/w/index.php?oldid=1374533944 ;
  https://archive.megiddo.co.il/archiyon_http/ViewPage.asp?pagesCatID=28306&siteName=megido . All checked 2026-10-14.
  Added 2026-10-23 (agent read; a person's check pending):
  https://www.ofra.org.il/adminML/ajaxXML/getXMLGlobalSetelmentsList.asp (the founding-scroll excerpt, in the
  "אודות קיבוץ מגידו" entry); https://www.megiddo.co.il/objDoc.asp?PID=877627&OID=889907&DivID=1 and
  https://www.megiddo.co.il/objDoc.asp?DivID=1&OID=889911&PID=877627 ; Gvati as above
  (https://benyehuda.org/read/14684).

### 9. Rehov (רחוב), moshav

- **Founded 13 September 1951. [V]** Gvati: Rehov, Beit She'an valley, 13.9.51, Hapoel HaMizrachi. Hebrew Wikipedia:
  September 1951, citing a HaTzofe item of 14 September 1951 whose title (as cited) says a stake for three Hapoel
  HaMizrachi moshavim was driven in the Beit She'an valley "yesterday", that is 13 September. I count the HaTzofe item
  as a second line of evidence only through its cited title; the article itself was not read. CBS: 1951.
- **Founders. [V]** Immigrants from Kurdistan and Morocco (both Wikipedias); Hapoel HaMizrachi (Gvati).
- **Location and hex.** CBS 32.45080, 35.48915 (Hebrew Wikipedia 60 m). Hex (8, 13), valley. No 1948 settlement.
- **Suitability.** Criteria 1 and 2 met (with the caveat above); 4 not.
- **Flags.** Next to a line hex: `jordan_adl_iraqi_c_east`. Hebrew Wikipedia: founded "on the lands of the abandoned
  Arab village Faruna". Faruna is not in `localities_1948.tsv`; Ghazawiya 7.5 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=42329764 ;
  https://en.wikipedia.org/w/index.php?oldid=1375326564 . All checked 2026-10-14.

### 10. Ma'agan Michael (מעגן מיכאל), kibbutz

- **Founded 25 August 1949, the day of going onto the land. [V]** Gvati: 25.8.49, HaKibbutz HaMeuhad. The kibbutz's
  own site: on 25.8.1949 the members, 154 adults and 44 children, went up to settle on the drained Kabara marsh land.
  Hebrew and English Wikipedia: 25 August 1949. CBS: 1949.
- **Founders. [V]** A garin of the Hebrew Scouts (kibbutz site, both Wikipedias), formed in 1942 (kibbutz site
  **[S]**). Several members worked before 1948 at the Ayalon Institute, the Haganah's underground ammunition plant
  (Hebrew Wikipedia **[S]**).
- **Location and hex.** CBS 32.55597, 34.91571 (Hebrew Wikipedia 33 m). Hex (2, 12), coastal plain, shared with Or
  Akiva (row 21). No 1948 settlement; Hadera's and Zikhron Ya'akov's hexes are neighbours.
- **Suitability.** Criteria 1 and 2 met. Criterion 4: no source read names a village on the site, but see the flag.
- **Flags.** No line within one hex. English Wikipedia: the Kabara area "was depopulated of its Arab residents during
  the 1948 Palestine war, with the exception of the 'Arab al-Ghawarneh tribe", and Ma'agan Michael was the first of
  three Jewish villages set up there in 1949-51. Hebrew Wikipedia names the Arab village Jisr az-Zarqa (today a
  living town) as the kibbutz's southern neighbour. `localities_1948.tsv`: Tantura 5.9 km, neighbouring hex, no
  `today`; nothing without `today` in the same hex (Binyamina's missing value is a data gap, see above).
- **Sources.** Gvati and CBS;
  https://www.maaganmk.co.il/http_new/ViewPage.asp?pagesCatID=31237&siteName=maaganm ;
  https://he.wikipedia.org/w/index.php?oldid=43192204 ; https://en.wikipedia.org/w/index.php?oldid=1377294253 . All
  checked 2026-10-14.

### 11. HaBonim (הבונים), cooperative moshav

- **Founded 18 July 1949. [S for the day, V for 1949]** Gvati (cooperative moshavim section): 18.7.49, HaIhud. Both
  Wikipedias and CBS: 1949.
- **Founders. [S]** English Wikipedia: the Habonim movement; its founders brought an industrial process from South
  Africa.
- **Location and hex.** CBS 32.63663, 34.93271 (Hebrew Wikipedia 58 m). Hex (3, 10), coastal plain.
- **Suitability.** Criterion 1 met; 2 not; 4 not.
- **Flags.** No line within one hex. Hebrew Wikipedia: "built on the remains of the abandoned Arab village Kafr Lam".
  English Wikipedia: "on land near the depopulated Arab village of Kafr Lam". Not in `localities_1948.tsv`; Jaba'
  3.2 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=42331948 ;
  https://en.wikipedia.org/w/index.php?oldid=1374531075 . All checked 2026-10-14.

## Jerusalem corridor

### 12. Beit Zayit (בית זית), moshav

- **Founded 24 October 1949. [S for the day, V for 1949]** Gvati: 24.10.49, Tnuat HaMoshavim. Both Wikipedias and
  CBS: 1949.
- **Founders. [C, minor]** Immigrants from Yugoslavia and Egypt (Hebrew Wikipedia); from Egypt, Romania and Yugoslavia
  (English Wikipedia). Part of the Jerusalem corridor settlements (Hebrew Wikipedia).
- **Location and hex.** CBS 31.78211, 35.16187 (Hebrew Wikipedia 7 m). Hex (-1, 23), mountains. No 1948 settlement;
  Jerusalem's hex is a neighbour.
- **Suitability.** Criterion 1 met; 2 not; 4 not.
- **Flags.** **On a line hex**: `jordan_adl_a_latrun`; next to `jordan_adl_bc_south` (the Jerusalem sector itself is
  not drawn in the repo). English Wikipedia: "on land that had formerly belonged to the depopulated Palestinian village
  of 'Ayn Karim". `localities_1948.tsv`: Ein Karim 1.7 km and Lifta 3.6 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43888232 ;
  https://en.wikipedia.org/w/index.php?oldid=1377941420 . All checked 2026-10-14.

### 13. Eshtaol (אשתאול), moshav

- **Founded 11 December 1949. [S for the day, V for the month]** Gvati: 11.12.49, Tnuat HaMoshavim. Hebrew Wikipedia:
  the first garin of 80 families went onto the land in December 1949. CBS: 1949.
- **Founders. [S]** Almost all immigrants from Yemen (Hebrew Wikipedia).
- **Location and hex.** CBS 31.78113, 35.00914 (Hebrew Wikipedia 3 m). Hex (-3, 23), hills.
- **Suitability.** Criterion 1 met; 2 not; 4 not.
- **Flags.** **On a line hex**: `jordan_adl_a_latrun`. Hebrew Wikipedia: founded "on the lands of the abandoned Arab
  villages Ishwa' and Islin". English Wikipedia: "on the lands of the depopulated Arab villages of Ishwa' and Islin".
  Neither is in `localities_1948.tsv`; Beit Mahsir 2.8 km, neighbouring hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43555477 ;
  https://en.wikipedia.org/w/index.php?oldid=1378096050 . All checked 2026-10-14.

### 14. Mevo Beitar (מבוא ביתר), cooperative moshav

- **Founded 24 April 1950. [S]** Gvati (cooperative moshavim section): 24.4.50, Moshvei Herut. English Wikipedia:
  24 April 1950, citing the village's "About" page, which now returns 404. The village's home page and CBS: 1950.
- **Founders. [V]** Young members of the Beitar movement from Argentina (village home page; English Wikipedia, which
  adds native Israelis).
- **Location and hex.** CBS 31.72247, 35.10656 (English Wikipedia 11 m). Hex (-2, 24), hills.
- **Suitability.** Criterion 1 met; 2 not (the second source is Wikipedia citing a page that cannot be read); 4 not.
- **Flags.** **On a line hex**: `jordan_adl_bc_south`; next to `jordan_adl_a_latrun`. English Wikipedia: "founded on
  the land of the depopulated Arab village of al-Qabu". Not in `localities_1948.tsv`; Battir 2.9 km, same hex, has a
  `today` (a living village).
- **Sources.** Gvati and CBS; https://www.mevobeitar.co.il/ ; https://he.wikipedia.org/w/index.php?oldid=42331038 ;
  https://en.wikipedia.org/w/index.php?oldid=1374534222 . All checked 2026-10-14.

### 15. Netiv HaLamed-He (נתיב הל"ה), kibbutz

- **Founded 16 August 1949. [V]** Gvati: 16.8.49, HaKibbutz HaMeuhad. Palmach database (Beit HaShita hachshara
  entry): on 16 August 1949, 60 members went up to the abandoned village Beit Nattif in the Elah valley and set up the
  kibbutz, named for the 35 fighters of the Mountain Platoon killed in January 1948. English Wikipedia: 16 August
  1949. CBS: 1949.
- **Permanent site. [S]** Hebrew Wikipedia: the ceremony of going onto the permanent site and naming the kibbutz
  Netiv HaLamed-He was held on 11 January 1951; the group first called itself "Kvutzat Peled". So "founded 16 August
  1949" is the first site; the map pin is the present one.
- **Founders. [V]** Demobilised Palmach soldiers of the Beit HaShita hachshara (Palmach database; English Wikipedia).
- **Location and hex.** CBS 31.68832, 34.98292 (English Wikipedia 89 m; Hebrew Wikipedia gives no coordinates). Hex
  (-4, 25), hills.
- **Suitability.** Criteria 1 and 2 met; 4 not.
- **Flags.** **On a line hex**: `jordan_adl_bc_south`. Palmach database: "the abandoned village Beit Nattif". English
  Wikipedia: "on land that had previously belonged to the village of Bayt Nattif". Hebrew Wikipedia: the name "Netiv"
  keeps the village's name. `localities_1948.tsv`: Beit Nattif 1.5 km, neighbouring hex, no `today`.
- **Sources.** Gvati and CBS; https://www.palmach.org.il/history/database/?itemId=5400 ;
  https://he.wikipedia.org/w/index.php?oldid=43924144 ; https://en.wikipedia.org/w/index.php?oldid=1374535027 . All
  checked 2026-10-14.

### 16. Beit Shemesh (בית שמש), transit camp, then town

- **Founded 6 December 1950 (27 Kislev 5711). [S, Wikipedia]** Hebrew Wikipedia gives both calendars, which agree;
  English Wikipedia: on 6 December 1950 the camp "Ma'abarat Har-Tuv" was set up on the site of today's moshav Naham.
  CBS: 1950. The heritage-council page that would have been a second source returned 404.
- **Names. [S]** First called Ma'abarat Har-Tuv after the moshava Har-Tuv (Hebrew Wikipedia).
- **Location and hex.** CBS 31.73178, 34.98828 (Hebrew Wikipedia 562 m, the largest spread in this note; same hex).
  Hex (-3, 24), hills.
- **Suitability.** Criterion 1 met; 2 not; 4: no source read places it on a village, but see the flags.
- **Flags.** Next to line hexes: `jordan_adl_a_latrun`, `jordan_adl_bc_south`. `localities_1948.tsv`: Deiraban
  2.4 km and Beit Nattif 4.1 km, same hex, no `today`.
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43925345 ;
  https://en.wikipedia.org/w/index.php?oldid=1375111051 . All checked 2026-10-14.

## Coastal plain and centre

### 17. Palmachim (פלמחים), kibbutz

- **Founded 11 April 1949 (12 Nisan 5709). [V]** Gvati: 11.4.49, HaKibbutz HaMeuhad. Palmach database: founded on
  11 April 1949 by Hebrew Scouts garin Dalet (the fourth), who had joined the Palmach in October 1946; on 11.4.1949, 80 members (35
  women) founded it. Hebrew Wikipedia: 11 April 1949, 12 Nisan 5709 (the calendars agree). Gan Rave Regional Council:
  founded 1949 by veterans of the Palmach's Yiftach Brigade, Scouts garin Dalet. CBS: 1949.
- **Location and hex.** CBS 31.93357, 34.70625 (Hebrew Wikipedia 17 m). Hex (-5, 21), coastal plain. Rishon LeZion's
  and Rehovot's hex is a neighbour.
- **Suitability.** Criteria 1 and 2 met; 4 not. The council page says the settling bodies and the garin chose a
  shore point with no Jewish settlement south of it as far as Gaza (paraphrase).
- **Flags.** No line within one hex. English Wikipedia: "on land of the depopulated Palestinian village of Nabi
  Rubin", citing the Mapa gazetteer. `localities_1948.tsv`: Nabi Rubin 2.6 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://palmach.org.il/history/database/?itemId=7951 ; https://www.ganrave.org.il/28/ ;
  https://he.wikipedia.org/w/index.php?oldid=43773921 ; https://en.wikipedia.org/w/index.php?oldid=1374535809 . All
  checked 2026-10-14.

### 18. Kfar Truman (כפר טרומן), moshav

- **Founded 18 July 1949. [V]** Gvati: 18.7.49, Tnuat HaMoshavim. Palmach database: members of the Harel Brigade's
  veterans' organisation founded the moshav "Bnei HaRael" on 18 July 1949; today Kfar Truman. Both Wikipedias and CBS:
  1949.
- **Founders. [V]** Demobilised Palmach soldiers of the Harel Brigade (Palmach database; English Wikipedia).
- **Location and hex.** CBS 31.98049, 34.92423 (Hebrew Wikipedia 27 m). Hex (-2, 20), coastal plain. Lod's, Ramla's
  and Petah Tikva's hexes are neighbours.
- **Suitability.** Criteria 1 and 2 met; 4 not.
- **Flags.** Next to line hexes: `jordan_adl_a_latrun`, `jordan_adl_iraqi_c_south`. Hebrew Wikipedia: founded "on
  the lands of the abandoned Arab village Beit Nabala". English Wikipedia: "on land located near the depopulated
  Palestinian village of Bayt Nabala". `localities_1948.tsv`: Deir Tarif 1.8 km, Beit Nabala 3.1 km and Tira 4.4 km,
  same hex, no `today`.
- **Sources.** Gvati and CBS; https://palmach.org.il/history/database/?itemId=7983 ;
  https://he.wikipedia.org/w/index.php?oldid=43956995 ; https://en.wikipedia.org/w/index.php?oldid=1374532849 . All
  checked 2026-10-14.

### 19. Nir Galim (ניר גלים), cooperative moshav

- **Founded 19 November 1949. [S for the day, V for 1949]** Gvati (cooperative moshavim section): 19.11.49, Hapoel
  HaMizrachi. Both Wikipedias and CBS: 1949.
- **Founders. [S]** Mostly Holocaust survivors from Hungary and Central Europe (Hebrew Wikipedia).
- **Location and hex.** CBS 31.82415, 34.68206 (Hebrew Wikipedia 82 m). Hex (-6, 23), coastal plain.
- **Suitability.** Criterion 1 met; 2 not; 4 not.
- **Flags.** No line within one hex. English Wikipedia: "on land near the Palestinian village of Arab Suqrir, which
  was depopulated in the 1948 Arab-Israeli War". Not in `localities_1948.tsv`; nothing without `today` in the same
  hex.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43119756 ;
  https://en.wikipedia.org/w/index.php?oldid=1374535415 . All checked 2026-10-14.

### 20. Rosh HaAyin (ראש העין), immigrant camp, then town

- **Founded: [C] year, no day.** Yad Ben-Zvi: in 1949-1950 the Jewish Agency set up a camp here for immigrants from
  Yemen, and in 1951 it became a permanent settlement. Hebrew Wikipedia: in 1949-1950 immigrants from Yemen settled
  near the British camp. English Wikipedia: founded 1949. CBS: 1950.
- **Location and hex.** CBS 32.09448, 34.95867 (Hebrew Wikipedia 227 m). Hex (-1, 19), coastal plain. Petah Tikva's
  hex is a neighbour.
- **Suitability.** Criterion 1 met; 2 not (no day); 4 not. A camp, not a pioneer foundation.
- **Flags.** **On a line hex**: `jordan_adl_iraqi_c_south`. English Wikipedia: "founded in 1949 near the site of the
  Palestinian village of Majdal Yaba, which was captured by Israeli forces in July 1948". Hebrew Wikipedia: the British
  camp was built near the Arab village Majdal Yaba. `localities_1948.tsv`: Majdal Yaba 1.5 km, same hex, no `today`.
- **Sources.** CBS; https://israelalbum.org.il/mercazim/rosh-haayin/ ;
  https://he.wikipedia.org/w/index.php?oldid=43949863 ; https://en.wikipedia.org/w/index.php?oldid=1380043985 . All
  checked 2026-10-14.

### 21. Or Akiva (אור עקיבא), transit camp, then town

- **Founded 17 April 1951 (first families). [S, Wikipedia]** Hebrew Wikipedia: on 17 April 1951 the Jewish Agency
  moved two families from the Brandes ma'abara in Hadera to the dunes at Caesarea; more came on 24 and 29 April, 38
  families of immigrants from Romania in all (citing Al HaMishmar, 2 May 1951, not read). English Wikipedia and CBS:
  1951.
- **Founders. [C, minor]** Immigrants from Romania first (Hebrew Wikipedia); English Wikipedia says the majority came
  from Morocco (it describes the camp's population, not the first families).
- **Location and hex.** CBS 32.50776, 34.91984 (Hebrew Wikipedia 42 m). Hex (2, 12), shared with Ma'agan Michael.
- **Suitability.** Criterion 1 met; 2 not; 4 contested in the source (below).
- **Flags.** No line within one hex. **[C]** English Wikipedia: "According to Walid Khalidi, it was built on the land
  of the depopulated Palestinian village Barrat Qisarya", followed in the same sentence by a contrary note based on
  the British Village Statistics. Hebrew Wikipedia: unlike most development towns, Or Akiva was built on private land
  owned by ICA (PICA). Barrat Qisarya is not in `localities_1948.tsv`; Qisariya (2.1 km, neighbouring hex) has a
  `today` (Caesarea).
- **Sources.** CBS; https://he.wikipedia.org/w/index.php?oldid=43428526 ;
  https://en.wikipedia.org/w/index.php?oldid=1377544116 . All checked 2026-10-14.

## Northern and western Negev

### 22. Beit Kama (בית קמה), kibbutz

- **Founded 18 April 1949 (19 Nisan 5709), going onto the land. [V]** Gvati: 18.4.49, HaKibbutz HaArtzi. Kibbutz
  mynet (local news, on the founding scroll found in a founder's papers): the scroll of "Hashomer Hatzair kibbutz
  Sefiah", which went onto the land of Tel Maliha on 19 Nisan 5709. 19 Nisan 5709 is 18 April 1949. **[C, minor]**
  Hebrew Wikipedia gives 19 April 1949 (citing an Al HaMishmar item of 19 April 1949, not read, which would report the
  previous day); English Wikipedia gives 18 April 1949. The Beit Kama heritage museum and archive page: April 1949.
  CBS: 1949.
- **Founders. [V]** Hashomer Hatzair immigrants from Hungary, who had lived at Hadar before going onto the land
  (archive page; Hebrew Wikipedia); first named Sefiah, renamed Beit Kama in June 1951 (archive page) **[S]**.
- **Location and hex.** CBS 31.44853, 34.76108 (Hebrew Wikipedia 39 m). Hex (-8, 28), hills. No 1948 settlement in it
  or next to it.
- **Suitability.** Criteria 1, 2 and 4 met. The cleanest row in this note.
- **Flags.** None: no line or zone hex within one hex; no `localities_1948.tsv` place within one hex; no source read
  names a village at the site. English Wikipedia places it north of the Bedouin town of Rahat (founded later).
- **Sources.** Gvati and CBS; https://kibbutz.mynet.co.il/local_news/article/m_154712 ;
  https://www.israeliana.org/post/%D7%90%D7%A8%D7%9B%D7%99%D7%95%D7%9F-%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%91%D7%99%D7%AA-%D7%A7%D7%9E%D7%94 ;
  https://he.wikipedia.org/w/index.php?oldid=43790184 ; https://en.wikipedia.org/w/index.php?oldid=1375255214 . All
  checked 2026-10-14.

### 23. Gilat (גילת), moshav

- **Founded 4 December 1949 (13 Kislev 5710), going onto the land. [V]** Gvati: 4.12.49, Tnuat HaMoshavim. Hebrew
  Wikipedia: went onto the land on 13 Kislev 5710; 13 Kislev 5710 is 4 December 1949. Merhavim Regional Council,
  English Wikipedia and CBS: 1949. (A web search summary gave 3 December from another page; I did not read such a
  page.)
- **Founders. [V]** Immigrants from Tunisia (council page; both Wikipedias). Hebrew Wikipedia: the second immigrants'
  moshav set up in the Negev **[S]**.
- **Location and hex.** CBS 31.32798, 34.65203 (Hebrew Wikipedia 3 m). Hex (-10, 30), lowland. Same hex as Tifrah
  (`designated-site-1.md`).
- **Suitability.** Criteria 1, 2 and 4 met.
- **Flags.** None (no line, no `localities_1948.tsv` place within one hex, no village named by a source).
- **Sources.** Gvati and CBS; https://www.merchavim.org.il/%D7%92%D7%99%D7%9C%D7%AA/ ;
  https://he.wikipedia.org/w/index.php?oldid=43560845 ; https://en.wikipedia.org/w/index.php?oldid=1376021022 . All
  checked 2026-10-14.

### 24. Patish (פטיש), moshav

- **Founded: [C, one day].** Gvati: 4.3.50, Tnuat HaMoshavim. English Wikipedia: 3 March 1950. CBS: 1950.
- **Founders. [S]** Hebrew Wikipedia: the site was first called Imara C; immigrants from Egypt, then from Tunisia,
  left; the Jewish Agency then settled immigrants from Kurdistan.
- **Location and hex.** CBS 31.32739, 34.56004 (Hebrew Wikipedia 19 m). Hex (-11, 30), lowland.
- **Suitability.** Criteria 1 and 4 met; 2 not (one day apart).
- **Flags.** None on lines or localities. For the owner: English Wikipedia records a fedayeen attack on a wedding in
  the village in March 1955, and Hebrew Wikipedia has a section on 7 October 2023.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43958443 ;
  https://en.wikipedia.org/w/index.php?oldid=1376022818 . All checked 2026-10-14.

### 25. Re'im (רעים), kibbutz

- **Founded 20 October 1949. [V]** Gvati: 20.10.49, HaKibbutz HaMeuhad. Palmach database (two entries): founded on
  20 October 1949 by the Na'an-Schiller hachshara (Hebrew Scouts garin Vav, the sixth), as a lookout settlement on Tel Jemmeh in
  the Negev. Yad Ben-Zvi: founded 1949 by a Scouts garin Vav that served in the Palmach, 100 members at going onto the
  land, first called Tel Re'im. CBS: 1949.
- **Location and hex.** CBS 31.38552, 34.45932 (Hebrew Wikipedia 7 m). Hex (-12, 29), coastal plain.
- **Suitability.** Criteria 1, 2 and 4 met.
- **Flags.** Next to line hexes: `egypt_mv1950_demarcation` and `egypt_mv1950_zone_lines` (shown from 22 February
  1950). No `localities_1948.tsv` place without `today` within one hex. For the owner: Hebrew Wikipedia describes the
  attack on the kibbutz on 7 October 2023.
- **Sources.** Gvati and CBS; https://palmach.org.il/history/database/?itemId=7953 ;
  https://palmach.org.il/history/database/?itemId=5312 ;
  https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%A8%D7%A2%D7%99%D7%9D/ ;
  https://he.wikipedia.org/w/index.php?oldid=43899506 ; https://en.wikipedia.org/w/index.php?oldid=1376276233 . All
  checked 2026-10-14.

### 26. Nahal Oz (נחל עוז), Nahal outpost, then kibbutz

- **Founded: [C].**
  - 15 July 1951 (11 Tammuz 5711): the Nahal veterans' association list ("Nahal Oz ... date founded"). The
    calendars agree.
  - 25 July 1951 (21 Tammuz 5711): Gvati (25.7.51, HaIhud); and Kibbutz Story quotes the outpost's founding scroll,
    dated 21 Tammuz 5711, which is 25 July 1951.
  - 26 July 1951: Hebrew Wikipedia ("founded on 26 July 1951 as the first Nahal outpost", citing HaBoker; it also
    cites a Sha'arim report of 26 July 1951 on the ceremony; neither read).
  - CBS and English Wikipedia: 1951. 25 July has two lines of evidence (Gvati; the scroll as quoted).
- **Founders. [V]** The first Nahal outpost, "Nahala'im A", facing Gaza (Nahal association; Kibbutz Story; English
  Wikipedia); turned over to a civilian garin on 16 September 1953 (Nahal association) **[S]**.
- **Location and hex.** CBS 31.47277, 34.49746 (Hebrew Wikipedia 7 m). Hex (-11, 28), coastal plain. Gaza's hex is a
  neighbour.
- **Suitability.** Criteria 1 and 4 met; 2 contested. A military outpost at founding, not a civilian yishuv until
  1953.
- **Flags.** Next to line hexes: `egypt_adl_gaza`, `egypt_mv1950_demarcation`, `egypt_mv1950_zone_lines`. No
  `localities_1948.tsv` place without `today` within one hex. For the owner: Hebrew Wikipedia describes the capture
  of the kibbutz on 7 October 2023.
- **Sources.** Gvati and CBS; https://www.nahal.co.il/%D7%94%D7%99%D7%90%D7%97%D7%96%D7%95%D7%99%D7%95%D7%AA/ ;
  https://www.kibbutz-story.com/post/nahal-oz ; https://he.wikipedia.org/w/index.php?oldid=44023010 ;
  https://en.wikipedia.org/w/index.php?oldid=1379237702 . All checked 2026-10-14.

### 27. Sderot (שדרות), transit camp, then town

- **Founded April 1951 (the Gevim-Dorot ma'abara). [S for the month]** Yad Ben-Zvi: the Gevim-Dorot ma'abara, the
  basis of Sderot, was set up in the Sha'ar HaNegev Regional Council area in April 1951; the permanent town was built
  from 1954. Hebrew Wikipedia: first half of 1951. English Wikipedia and CBS: 1951.
- **Location and hex.** CBS 31.52901, 34.59434 (Hebrew Wikipedia 138 m). Hex (-9, 27), coastal plain. Yad Mordechai's
  hex is a neighbour. Mefalsim is in the next hex west, (-10, 27).
- **Suitability.** Criterion 1 met; 2 not; 4 not.
- **Flags.** **On a line hex**: `egypt_adl_gaza`, `egypt_mv1950_zone_lines`; next to `egypt_mv1950_demarcation`.
  English Wikipedia: the area was depopulated in May 1948 and the farmers of the Palestinian village of Najd were
  expelled (citing Morris and Khalidi). Najd is not in `localities_1948.tsv`; Sumsum 4.4 km, neighbouring hex, no
  `today`. For the owner: Hebrew Wikipedia describes the attack on the town on 7 October 2023.
- **Sources.** CBS; https://israelalbum.org.il/mercazim/%D7%A9%D7%93%D7%A8%D7%95%D7%AA/ ;
  https://he.wikipedia.org/w/index.php?oldid=43855949 ; https://en.wikipedia.org/w/index.php?oldid=1376842336 . All
  checked 2026-10-14.

## Lachish area

### 28. Beit Guvrin (בית גוברין), kibbutz

- **Founded: [C].**
  - 23 May 1949: Gvati (23.5.49, HaKibbutz HaMeuhad, region "Lachish"); Palmach database, Beit Guvrin entry.
  - 2 June 1949: Palmach database, "Shalhevet" hachshara entry. English Wikipedia: founded "on the eve of Shavuot"
    1949; Shavuot 5709 (6 Sivan) was 3 June 1949, so its eve is 2 June.
  - CBS: 1949. The Palmach site gives both dates on two pages.
- **Founders. [V]** Three Palmach hachsharot, "Yetzivim", "Shalhevet" and "Bnei Horin" (Palmach database, both
  entries); former Palmach members (English Wikipedia).
- **Location and hex.** CBS 31.61302, 34.89595 (Hebrew Wikipedia 8 m). Hex (-5, 26), hills.
- **Suitability.** Criterion 1 met; 2 contested; 4 not.
- **Flags.** **On a line hex**: `jordan_adl_bc_south`. Hebrew Wikipedia: founded "on the lands of the Arab village
  Beit Jibrin, captured by the 8th Brigade on 27 October 1948"; a building left from the village served as the
  dining hall. English Wikipedia: "on the fields to the north of the Arab village of Beit Jibrin", after its residents
  fled the assault. `localities_1948.tsv`: Beit Jibrin 0.9 km, same hex, no `today`.
- **Sources.** Gvati and CBS; https://palmach.org.il/history/database/?itemId=7976 ;
  https://palmach.org.il/history/database/?itemId=5401 ; https://he.wikipedia.org/w/index.php?oldid=43555500 ;
  https://en.wikipedia.org/w/index.php?oldid=1375241236 . All checked 2026-10-14.

### 29. Ahuzam (אחוזם), moshav

- **Founded: [C].** Gvati: 2.10.50, HaOved HaZioni, region "Lachish". English Wikipedia: 30 October 1950. Hebrew
  Wikipedia and CBS: 1950.
- **Founders. [V]** Immigrants from Morocco, first named "Ma'agalim" (both Wikipedias).
- **Location and hex.** CBS 31.55340, 34.77020 (Hebrew Wikipedia 2 m). Hex (-7, 26), coastal plain.
- **Suitability.** Criterion 1 met; 2 contested; 4: see the flag.
- **Flags.** No line within one hex. Hebrew Wikipedia: the settlement began in a ma'abara that stood "near the
  destroyed Arab village Abu Jabir". Not in `localities_1948.tsv`; 'Iraq el Manshiya 5.8 km and Faluja 8.2 km, same
  hex, no `today`.
- **Sources.** Gvati and CBS; https://he.wikipedia.org/w/index.php?oldid=43968833 ;
  https://en.wikipedia.org/w/index.php?oldid=1375067074 . All checked 2026-10-14.

## Arava

### 30. Yotvata (יטבתה), Nahal outpost, then kibbutz

- **Founded: [C, one day].** Gvati: 31.10.51, HaIhud, region "Arava". Hebrew Wikipedia: the ceremony of going onto the
  land was on 30 October 1951 (citing HaBoker, 1 November 1951, not read). The Hevel Eilot Regional Council page
  quotes Yehiel Bentov's memoir for 30 October 1951, but marks the passage as taken from Wikipedia, so it is not
  independent. CBS: 1951.
- **Founders. [S]** The third Nahal outpost, near the well that supplied Eilat (Hebrew Wikipedia); the Moroccan garin
  of the Nahal platoon from Ein Harod as its advance force (council page); a civilian kibbutz from 1957 (council
  page).
- **Location and hex.** CBS 29.89552, 35.05929 (Hebrew Wikipedia 277 m). Hex (-16, 51), desert.
- **Suitability.** Criteria 1 and 4 met; 2 not. The only Arava row: a military outpost at founding.
- **Flags.** **On a line hex**: `jordan_adl_d_arava` (marked low accuracy in the repo). No localities within one hex.
- **Sources.** Gvati and CBS; https://www.eilot.org.il/yotvata/ ;
  https://he.wikipedia.org/w/index.php?oldid=43944113 ; https://en.wikipedia.org/w/index.php?oldid=1378616030 . All
  checked 2026-10-14.

## Considered and left out

- **Tkuma** (Gvati 30.5.49): Yad Ben-Zvi says it was founded on 5-6 October 1946 as one of the 11 Negev points and
  moved to its present site on 30 May 1949. Gvati's date is the move, so it is not a 1949 founding.
  https://israelalbum.org.il/mercazim/%D7%9E%D7%95%D7%A9%D7%91-%D7%AA%D7%A7%D7%95%D7%9E%D7%94/ (checked 2026-10-14).
- **Ein Gedi** (Gvati 26.2.50): the CBS year is 1953 and the Nahal association dates the outpost to 15 January 1953.
  **[C]**; probably outside the range.
- **Eilat**: the sources read give 1949 (the name given to Umm al-Rashrash), 1950 and 1951 for different events; no
  founding day. Not pursued.
- The other 1949-51 rows of Gvati's list (218 in all) were mapped but not researched. Many moshavim of 1949-50 in the
  Judaean foothills and the Jerusalem corridor are on or next to a Jordan line hex and have one or more
  `localities_1948.tsv` places without `today` in their hex.

## The best five

Ranked on Task 5.0's criteria 1, 2 and 4 and on fit with "Build yishuv" (a pioneer group going onto bare land on a
known day). Criterion 3 (a photograph) was not checked for any of them.

1. **Beit Kama, 18 April 1949.** The day is supported by Gvati and the founding scroll's Hebrew date; the hex is clean
   (no line, no locality, no 1948 settlement within one hex); founded by a Hashomer Hatzair garin of Holocaust
   survivors from Hungary. Only flaw: Hebrew Wikipedia says 19 April.
2. **Gilat, 4 December 1949.** Two sources agree on the day (Gvati; Hebrew Wikipedia's Hebrew date); clean hex. An
   immigrants' moshav from Tunisia, so a different settlement story from the kibbutzim. Shares hex (-10, 30) with
   Tifrah.
3. **Ma'agan Michael, 25 August 1949.** Three sources agree, one of them the kibbutz's own; no line nearby; a Hebrew
   Scouts garin onto drained marsh land. The owner should see English Wikipedia's line on the area's 1948
   depopulation; no source read names a village on the site.
4. **Re'im, 20 October 1949.** Gvati and two Palmach database entries agree; Yad Ben-Zvi confirms the year and the
   garin; no locality nearby. Next to the 1950 Gaza lines on the map, and associated with 7 October 2023.
5. **Palmachim, 11 April 1949.** Gvati, the Palmach database and the Hebrew calendar date agree; a Palmach Scouts garin
   on an empty shore. Flagged: English Wikipedia places it on land of Nabi Rubin, 2.6 km away in the same hex.

Next in line: Patish (clean, but the day is 3 or 4 March 1950), Rehov (day agreed, but on the lands of Faruna per
Hebrew Wikipedia), Netiv HaLamed-He and Kfar Truman (days verified, both flagged).

## All flagged rows

| # | Settlement | Line (within one hex) | Depopulated locality (sources; `localities_1948.tsv` same hex) | Other |
|---|---|---|---|---|
| 1 | Sasa | Next to `lebanon_adl` | Sa'sa' (en Wikipedia); TSV 0.2 km | Id `sasa` already used for the Arab village; hex shared with Meron |
| 2 | Yas'ur | None | al-Birwa (he and en Wikipedia); TSV Birwa 1.5 km, Damin 3.1 km | |
| 3 | Gesher HaZiv | **On** `lebanon_adl` | a-Zib (he, en Wikipedia); TSV Zib 1.4 km, Bassa 5.0 km | |
| 4 | Lohamei HaGeta'ot | None | al-Sumayriyya (he, en Wikipedia); TSV Manshiya 3.2 km | Hex holds Acre (1948) |
| 5 | HaSolelim | None | Saffuriya land (en Wikipedia); TSV Saffuriya 3.9 km | |
| 6 | Meron | Next to `lebanon_adl` | Meron village (he, en Wikipedia); TSV Sa'sa' 6.2 km | Hex shared with Sasa |
| 7 | Kiryat Shmona | **On** `lebanon_adl`; next to `syria_adl` | al-Khalisa (Yad Ben-Zvi, he, en Wikipedia); TSV Khalisa 0.7 km, Zuq et Tahtani, Hunin, Na'ima | |
| 8 | Megiddo | Next to two Jordan lines | Lajjun (he, en Wikipedia); TSV Mansi 1.7 km | |
| 9 | Rehov | Next to `jordan_adl_iraqi_c_east` | Faruna (he Wikipedia); TSV Ghazawiya 7.5 km | |
| 10 | Ma'agan Michael | None | Area depopulated except 'Arab al-Ghawarneh (en Wikipedia); TSV none | Hex shared with Or Akiva |
| 11 | HaBonim | None | Kafr Lam (he, en Wikipedia); TSV Jaba' 3.2 km | |
| 12 | Beit Zayit | **On** `jordan_adl_a_latrun` | 'Ayn Karim land (en Wikipedia); TSV Ein Karim 1.7 km, Lifta 3.6 km | Next to Jerusalem's hex |
| 13 | Eshtaol | **On** `jordan_adl_a_latrun` | Ishwa', Islin (he, en Wikipedia); TSV none | |
| 14 | Mevo Beitar | **On** `jordan_adl_bc_south` | al-Qabu (en Wikipedia); TSV none | |
| 15 | Netiv HaLamed-He | **On** `jordan_adl_bc_south` | Beit Nattif (Palmach database, en Wikipedia); TSV none (Beit Nattif 1.5 km, next hex) | First site 1949, permanent site 1951 |
| 16 | Beit Shemesh | Next to two Jordan lines | None named; TSV Deiraban 2.4 km, Beit Nattif 4.1 km | |
| 17 | Palmachim | None | Nabi Rubin (en Wikipedia); TSV 2.6 km | |
| 18 | Kfar Truman | Next to two Jordan lines | Beit Nabala (he, en Wikipedia); TSV Deir Tarif 1.8 km, Beit Nabala 3.1 km, Tira 4.4 km | |
| 19 | Nir Galim | None | Arab Suqrir (en Wikipedia); TSV none | |
| 20 | Rosh HaAyin | **On** `jordan_adl_iraqi_c_south` | Majdal Yaba (he, en Wikipedia); TSV 1.5 km | |
| 21 | Or Akiva | None | Barrat Qisarya per Khalidi, contested in the same source **[C]**; TSV none | Hex shared with Ma'agan Michael |
| 24 | Patish | None | None | 1955 attack; 7 October 2023 section (Wikipedia) |
| 25 | Re'im | Next to the 1950 Gaza lines | None | 7 October 2023 (he Wikipedia) |
| 26 | Nahal Oz | Next to three Gaza lines | None | Next to Gaza's hex; 7 October 2023 (he Wikipedia) |
| 27 | Sderot | **On** `egypt_adl_gaza` | Najd (en Wikipedia); TSV none (Sumsum 4.4 km, next hex) | 7 October 2023 (he Wikipedia) |
| 28 | Beit Guvrin | **On** `jordan_adl_bc_south` | Beit Jibrin (he, en Wikipedia); TSV 0.9 km | |
| 29 | Ahuzam | None | Abu Jabir (he Wikipedia); TSV 'Iraq el Manshiya 5.8 km, Faluja 8.2 km | |
| 30 | Yotvata | **On** `jordan_adl_d_arava` | None | |

Unflagged: Beit Kama (22) and Gilat (23, which shares Tifrah's hex).

## Open questions for the owner

1. Whether a designated site may sit on or next to an armistice line hex (10 of the 30 are on one, 8 more next to one). The plan's Task
   5.1 validation does not test it; `designated-site-1.md` raised the same question for Mefalsim.
2. Whether two sites may share a hex (Sasa and Meron; Ma'agan Michael and Or Akiva; Gilat, Tifrah and other moshavim
   in (-10, 30)).
3. For sites with a first and a permanent site (Netiv HaLamed-He; Mefalsim), whether the card's date is the first
   going onto the land.
4. Someone with access to the NLI press archive could settle the one-day conflicts here (Megiddo, Patish, Beit Kama,
   Yotvata, Nahal Oz, Ahuzam, Beit Guvrin) from the newspaper items Hebrew Wikipedia cites.
