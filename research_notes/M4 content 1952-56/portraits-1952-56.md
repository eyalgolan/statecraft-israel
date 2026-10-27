# Portrait candidates for the 20 people of 1952-56 who have none (research note, 2026-10-24)

Status: research only, for a person's likeness check. No image is in the repository, and nothing here is in `content/` or
`unity/`. Refs #6 (the research part opened by the owner's comment of 24 October 2026). Adding the images, the `portrait:`
entries and the tests stays paused until after the MVP.

**Scope.** The 20 people in that comment: Haim Cohn, Mordechai Nurock, Israel Rokach, Yosef Serlin, Yosef Sapir, Zalman Aran,
Kadish Luz, Pinchas Sapir, Yisrael Barzilai, Yisrael Bar-Yehuda, Moshe Carmel, Mordechai Namir, Mordechai Maklef, Moshe Dayan,
Zorach Warhaftig, Shlomo-Yisrael Ben-Meir, Zalman Susayeff, Ze'ev Tzur, Moshe Unna and Ami Assaf. Not Geri, Pinkas or Yadin.
The ids are those of `content/people.yaml`; none of the 20 has a `portrait:` entry there (checked on main `55ee875b`).

## Result

- **18 of 20 have at least one file that meets the licence rule** and is a single-person portrait whose identity is named by
  the archive's record as copied onto Commons: Nurock, Maklef, Serlin, Yosef Sapir, Aran, Luz, Pinchas Sapir, Barzilai,
  Bar-Yehuda, Carmel, Namir, Warhaftig, Ben-Meir, Susayeff, Tzur, Unna, Assaf and Dayan.
- **2 of 20 have none: Haim Cohn and Israel Rokach.** Their only single-person portraits are Government Press Office (GPO)
  files under PD-Israel alone, and the CC BY 4.0 frames I found of them are group shots or do not name them (details below).
  (second pass, 27 Oct: Rokach does have one qualifying file, a Meitar frame whose NLI title names him; see "Second pass, 27 October" below. Haim Cohn still has none.)
- Sixteen of the 18 rest only on Knesset Archives portraits (CC BY-SA 4.0); Nurock has both kinds and Maklef only a Meitar frame. Those are **member portraits with no photograph
  date**: Commons gives only the span of the person's Knesset membership. For Carmel (member from 1958), Pinchas Sapir and
  Dayan (both from 1959) the photograph is probably later than 1956 [S]. Maklef and Nurock have dated Meitar frames
  (November 1950 and December 1952).
- **Share-alike applies** to a crop of a CC BY-SA 4.0 file (`docs/CONTENT_POLICY.md` section 1, rule 5), and the
  existing portrait test refuses a crop under 512 px tall (`tools/tests/test_portraits_1948_51.py`, `h >= 512`). Files
  whose whole frame is under 512 px tall cannot pass it: **Serlin (480), Barzilai (402) and the second Dayan file (480)**.
  Files under about 600 px tall allow only a near-whole-frame crop.
- Each licence is the template the file page carries; a US status is not needed for the CC licences. No candidate carries a
  PD-Israel-only template, and no file here is an upscale or a crop made by Commons users.

## Conventions (the same as `powers-1948-56.md`)

- **[V]** read on the cited page or API record in this session and it says it. Every [V] here is "agent read 2026-10-24; a
  person's check pending".
- **[S]** single source or a copy of an archive's record on another site (for example an NLI title as reproduced on Commons,
  because the NLI page itself returned HTTP 403 to this machine). Treat it as "probably".
- **[C]** sources conflict. Every version is given.
- **[GAP]** not found, or the page could not be opened. A gap stays a gap.
- (second pass, 27 Oct) A claim added on 27 October is tagged `[V, agent read 27 Oct; a person's check pending]`; the other tags are used as above.

## What was opened, and when (all on 2026-10-24)

- **Wikimedia Commons API** (`https://commons.wikimedia.org/w/api.php`): for every person, a search of the File namespace
  by English name, by Hebrew name, by `haswbstatement:P180=<Wikidata id>`, the person's own category, and the file named
  by Wikidata's image property (P18). For each hit: `imageinfo` (size, extmetadata) and the file page's wikitext (source,
  date, permission, licence template). Result counts are in "Method". [V]
- **Wikidata API**: the item of each person, matched on the birth date in `content/people.yaml`, and its P18 and P373. [V]
- **Thumbnails of the 26 shortlisted files** were looked at, to see whether each shows one person. [V]
- **Opened, no use for the rule:** the IDF and Defence Establishment Archive photographs page
  (`archives.mod.gov.il/sites/english/record/pages/photographs.aspx`) says the archive "allows use for a fee of photographs, of
  which it owns the copyrights". That is a fee licence, so it does not meet the rule. [V]
- **Opened but empty:** the GPO terms URL from the owner's starter list returned a gov.il "page not found" error body
  (HTTP 200). [GAP]
  (second pass, 27 Oct: the same URL now opens and carries the GPO licence terms; read, see below. [V, agent read 27 Oct; a person's check pending])
- **Not reachable:** `nli.org.il` record pages and `archives.gov.il` answered HTTP 403. [GAP]
  (second pass, 27 Oct: again HTTP 403 from `nli.org.il` and `archives.gov.il`, and from Europeana's item pages; still [GAP]. See below.)
- **Not opened:** the IDF archive's catalogue search (`SearchCatalogTz.aspx`), Europeana, the Knesset site itself, and
  any search by the people's names in the Israel State Archives. Anything that depends on them is [GAP], not "none exists".
  (second pass, 27 Oct: Europeana was searched through its API and the Knesset site was tried; see below. The IDF catalogue and
  the Israel State Archives are still not searched.)

## The 18 with a qualifying file

Columns: Commons file page, licence template as the page's wikitext shows it, size (whole frame), the date as the page
gives it, creator, identity as the archive record gives it. All Knesset files carry `{{Cc-by-sa-4.0}}`, the permission
ticket VRTS 2024112910006311, the credit "donated to Wikimedia Commons by the Knesset Archives", and a
`{{Knesset Archive|1=<member code>}}` source; "code" below is that Knesset member code (the Knesset's own id for the person).
All Meitar files carry `{{cc-by-4.0}}` with the Meitar credit line (photographer Benno Rothenberg, Meitar Collection,
National Library of Israel) and VRTS 2021092910005788. All creator fields read "Knesset photographer" for the Knesset files. [V]

| Person | File page (https://commons.wikimedia.org/wiki/File:...) | Licence | Size | Date as the page gives it | Identity | Note |
|---|---|---|---|---|---|---|
| Mordechai Nurock | `Mordechai_Nurock_(997008136227405171).jpg` | CC BY 4.0 | 1322x1881 | 1952-12 | NLI title "ח"כ הרב מרדכי נורוק מסיעת המזרחי" [S] | single portrait; best fit for the period |
| Mordechai Nurock | `Mordechai_Nurock_(00340379).jpg` | CC BY-SA 4.0 | 596x767 | Knesset 1949-02-14 to 1962-11-08 | Knesset code 517 [S] | single portrait |
| Mordechai Nurock | `Mordechai_Nurock_(00340378).jpg` | CC BY-SA 4.0 | 394x562 | same span | code 517 [S] | small |
| Mordechai Maklef | `Mordechai_Maklef_(997008136532505171).jpg` | CC BY 4.0 | 1354x1905 | 1950-11 | NLI title "האלוף מרדכי מקלף", description "לימים הרמטכ"ל" [S] | single man in uniform; before he was Chief of Staff (Dec 1952) |
| Yosef Serlin | `Joseph_Serlin_(serlin_yosef).jpg` | CC BY-SA 4.0 | 316x480 | Knesset 1949-02-14 to 1974-01-15 | code 548 [S] | under 512 px tall: fails the size test |
| Yosef Sapir | `Joseph_Sapir_(00340227).jpg` | CC BY-SA 4.0 | 762x1065 | Knesset 1949-02-14 to 1972-02-26 | code 544 [S] | single portrait |
| Zalman Aran | `Zalman_Aran_(00010021).jpg` | CC BY-SA 4.0 | 2852x4134 | Knesset 1949-02-14 to 1969-11-17 | code 277 [S] | largest of the set |
| Kadish Luz | `Kadish_Luz_(00360701).jpg` | CC BY-SA 4.0 | 486x768 | Knesset 1951-08-20 to 1969-11-17 | code 473 [S] | single portrait |
| Pinchas Sapir | `Pinchas_Sapir_(00340230).jpg` | CC BY-SA 4.0 | 549x741 | Knesset 1959-11-30 to 1975-08-12 | code 545 [S] | membership starts after 1956 |
| Yisrael Barzilai | `Israel_Barzilai_(barzilai_israel).jpg` | CC BY-SA 4.0 | 266x402 | Knesset 1955-08-15 to 1965-11-22 | code 315 [S] | far too small: fails the size test |
| Yisrael Bar-Yehuda | `Israel_Bar-Yehuda_(00370387).jpg` | CC BY-SA 4.0 | 530x796 | Knesset 1949-02-14 to 1965-05-04 | code 310 [S] | a scan of a print with a white border |
| Moshe Carmel | `Moshe_Carmel_(00360631).jpg` | CC BY-SA 4.0 | 506x745 | Knesset 1958-06-09 to 1977-06-13 | code 467 [S] | membership starts after 1956 |
| Mordechai Namir | `Mordechai_Namir_(00340247).jpg` | CC BY-SA 4.0 | 660x812 | Knesset 1951-08-20 to 1969-11-17 | code 525 [S] | single portrait |
| Zorach Warhaftig | `Zerach_Warhaftig_(00360246).jpg` | CC BY-SA 4.0 | 404x551 | Knesset 1949-02-14 to 1981-07-20 | code 398 [S] | small |
| Shlomo-Yisrael Ben-Meir | `Shlomo-Israel_Ben-Meir_(00330056).jpg` | CC BY-SA 4.0 | 1426x2025 | Knesset 1952-08-14 to 1971-04-04 | code 301 [S] | single portrait; Wikidata's own image |
| Zalman Susayeff | `Zalman_Susayeff_(00340346).jpg` | CC BY-SA 4.0 | 1557x2109 | Knesset 1951-08-20 to 1959-11-30 | code 535 [S] | single portrait |
| Ze'ev Tzur | `Ze'ev_Tzur_(00310272).jpg` | CC BY-SA 4.0 | 480x703 | Knesset 1955-08-15 to 1969-11-17 | code 586 [S] | single portrait |
| Ze'ev Tzur | `Ze'ev_Tzur_(00310270).jpg` | CC BY-SA 4.0 | 420x589 | same span | code 586 [S] | small |
| Moshe Unna | `Moshe_Unna_(00330749).jpg` | CC BY-SA 4.0 | 418x588 | Knesset 1949-02-14 to 1969-11-17 | code 245 [S] | small |
| Ami Assaf | `Ami_Assaf_(00330283).jpg` | CC BY-SA 4.0 | 1536x2115 | Knesset 1949-02-14 to 1963-05-17 | code 266 [S] | single portrait |
| Moshe Dayan | `Moshe_Dayan_(00370233).jpg` | CC BY-SA 4.0 | 404x581 | Knesset 1959-11-30 to 1981-10-16 | code 688 [S] | eye patch; membership starts after 1956 |
| Moshe Dayan | `Moshe_Dayan_(dayan_moshe).jpg` | CC BY-SA 4.0 | 383x480 | same span | code 688 [S] | speaking at a microphone; under 512 px tall |

Seen in the thumbnails: each of these 22 files shows one person, head and shoulders (Dayan's second file and Maklef's
show more of the body). The Knesset Archives' own record pages were not opened, so "identity" is the Commons page's copy of
the Knesset member code and name [S]; the Commons page's `{{Knesset Archive}}` template is the archive's own link to the
record. Whether the face is the right man is the likeness check a person does.

## The 2 with no qualifying file

### Haim Cohn (Wikidata Q1058964) [V]

- The Commons category holds one single-person portrait: `File:Cohen_haim.jpg`, GPO, Teddy Brauner, 10 June 1952,
  2052x3066, "Supreme court justice dr. Haim Cohen". Template `{{PD-Israel}}` only, no US status. **Does not qualify.**
- Meitar frames of "Judges" (Boris Carmi, June 1965) are in his category: `Zvi Berenzon, Moshe Zilberg and Haim Cohn
  (997009326707205171)` and `...(997009326707305171)` are CC BY 4.0 but name three men; five more frames titled "Meitar
  Collection (997009452288305171", "...288405171", "...288505171", "...288905171" and "...289005171" carry only the subject heading
  "Judges Israel". **Group shots, or no name: do not qualify.** They are also June 1965.
- Four IPPA frames (CC BY 4.0, 5 March 1980, FL45815251, FL45815269, FL45815294, FL62833475) show new Supreme Court judges; two
  name him with Moshe Landau, one is a group, one names nobody, and all are 1980. Do not qualify.
- He was not a Knesset member (so there is no Knesset Archives file) and I found no Nationaal Archief, US-government or
  other explicit-US-status photograph of him. [GAP: the IDF archive catalogue and Europeana were not searched.]
  (second pass, 27 Oct: Europeana was searched, see below, and found no photograph of him from before 1980; the IDF archive
  catalogue is still not searched. Still [GAP] for that catalogue.)

### Israel Rokach (Wikidata Q129676) [V]

- Single-person portrait: `File:Israel_Rokach_1950.jpg`, GPO, Fritz Cohen, 1 October 1950, 2052x3066, "Portrait, Israel
  Rokach, mayor of Tel Aviv (Gen. Zionists)". `{{PD-Israel}}` only. **Does not qualify.**
- He was a Knesset member, but a search of the Knesset Archives pattern (`"Knesset member" Rokach`,
  `insource:"Knesset Archive" Rokach`, and the same in Hebrew) found no file. [GAP]
  (second pass, 27 Oct: searched again by the Knesset member code, with a control that finds Lavon; no Knesset Archives file
  for Rokach, see below. Closed as "none found by these searches". [V, agent read 27 Oct; a person's check pending])
- 17 Meitar frames (Benno Rothenberg, 1948 to December 1949) sit in his category, headed only "Tel Aviv-Yafo (Israel)
  Photographs" and "Politicians Israel Photographs". None names him, so identity from the archive record fails. [S] The
  rest of the category is GPO or Zoltan Kluger material under PD-Israel and group shots (for example with Weizmann or
  Sharett). The Dutch Nationaal Archief CC0 file in the category (Willem van de Poll, 1949, a Knesset doorman in Tel Aviv)
  does not show him.
  (second pass, 27 Oct: this is wrong for ten of the 17. On the file pages the Hebrew title or the Hebrew caption names him in
  ten of them; one frame, titled with his name and office, shows him alone. See "Second pass, 27 October" below. [V, agent read 27 Oct; a person's check pending])

## Files that look usable and are not

Each was read on its file page on 2026-10-24. [V]

- **GPO portraits, PD-Israel only, no US status.** Wikidata's image for 17 of the 20 is such a file: Cohn, Nurock
  (`Mordechai_Nurok,_1951.jpg`, 30 Nov 1951), Rokach, Serlin (1 Sep 1951), Yosef Sapir (1946, Zoltan Kluger), Aran (4 Sep 1954,
  David Eldan), Luz (30 Nov 1955, Moshe Pridan), Pinchas Sapir (1970), Barzilai (1 Aug 1955), Bar-Yehuda (1 Dec 1951),
  Carmel (8 Aug 1955), Namir (1 May 1947), Maklef (4 Dec 1952, David Eldan, `Mordechai_Maklef_1952_(cropped).jpg`,
  579x898), Warhaftig (30 Aug 1964), Susayeff (1 Dec 1951), Unna (1 Dec 1951) and Assaf (1 Nov 1959); Tzur's
  (`Zeev_Tzur_1966-01-12.jpg`) carries `{{PD-Israel}}` alone, without the GPO source template. These are the files
  to refuse (rule: no PD-Israel-only). Ben-Meir's and Dayan's Wikidata images are not GPO (see next).
- **IDF Spokesperson's Unit portraits, CC BY-SA 3.0.** `Moshe_Dayan,_Chief_of_General_Staff.jpg` (566x800, no date on the
  page) and `Mordechai_Maklef,_Chief_of_General_Staff.jpg` (1560x2014, "circa 1952", VRTS 2020020310000194) carry
  `{{cc-by-sa-3.0}}`. Version 3.0 is not one of the four licences, so they do not qualify, although they fit the period
  best. The Maklef one is the better photograph than the qualifying Meitar frame in size; the licence decides.
- **Meitar group shots.** `Mordechai_Namir,_Jerusalem_(997009857447005171).jpg` (1947-1949, 1285x2000; its title is about the Tel Aviv-Jerusalem railway and does not name him as the subject, and the thumbnail shows a group),
  `Moshe_Carmel,_Wadi_Ara_(997008137551105171).jpg` (May 1949, 1889x1297; the title says he is in conversation, and the thumbnail shows two men), `Moshe_Dayan_(997008136760605171).jpg`
  (October 1951, 1930x1322; a farewell party, men at a meal table in the thumbnail). All CC BY 4.0; all fail "no group shots".
- **Dayan, 13 November 1956** (`Moshe_Dayan_(997009157311805171).jpg`, Benno Rothenberg, CC BY 4.0, 4983x5064): the NLI
  title is "עיתונאים עם הרמטכ"ל משה דיין" (journalists with the Chief of Staff Moshe Dayan). The frame shows one man at a
  wall map, small in the picture; the title does not say which person is Dayan. **[C] likeness and identity need a person;
  not counted as qualifying.** It is the only in-period frame of him I found.
- **Dayan on Nationaal Archief and US files.** The CC0 Anefo files are from 18 June 1962 (a group in a church) and 25 July
  1979 (arrival at Schiphol, with a minister and bodyguards). `Moshe_Dayan_1967.jpg` is `{{PD-US-no notice}}` but shows Dayan
  with a US officer, a two-person frame of 909x987. The `PD-USGov` files are a four-person group at a rally on 27 April 1958 (NARA, with
  Eleanor Roosevelt) and frames from 1970 to 1979. None is a usable portrait of the period.
- **Dan Hadani / IPPA collection (CC BY 4.0, National Library of Israel):** many frames of Dayan, Pinchas Sapir, Luz, Aran
  and others, but all are events or later than 1960 (for example Luz at an exhibition in 1969). None was kept.
- **Not him:** a search for "Joseph Serlin" returns the French politician (1868-1944): CC0 busts and a CC0 portrait. A
  search for "Tzur" returns the sculptor Ze'ev Tsurnamal. Neither is the person.

## Method and counts

- Candidate pool per person (file pages read through the API): Cohn 37, Nurock 9, Rokach 78, Serlin 58, Yosef Sapir 40,
  Aran 35, Luz 45, Pinchas Sapir 168, Barzilai 30, Bar-Yehuda 50, Carmel 38, Namir 43, Maklef 49, Dayan 245, Warhaftig 34,
  Ben-Meir 30, Susayeff 4, Tzur 30, Unna 6, Assaf 30. A second round for Cohn, Rokach and Maklef (seven or eight more
  search forms each, English and Hebrew) added 33, 21 and 9 files and no qualifying one.
- Shortlisting rule: a file stayed if its licence template is CC0, CC BY 4.0, CC BY-SA 4.0 or a US public-domain template,
  it shows one person, and the archive's title or the Knesset code names the person. PD-Israel-only, CC BY 2.5, CC BY-SA 3.0,
  CC BY 3.0 and unnamed Meitar frames were dropped and are listed above where they matter.
- "None found" means "not found by these searches". A frame whose page has none of the words searched can be missed.
- Wikidata ids: Cohn Q1058964, Nurock Q1365144, Rokach Q129676, Serlin Q2613074, Yosef Sapir Q2614158, Aran Q145162,
  Luz Q1720966, Pinchas Sapir Q578459, Barzilai Q2904686, Bar-Yehuda Q2079765, Carmel Q2660910, Namir Q2909456,
  Maklef Q1391515, Dayan Q188783, Warhaftig Q2890045, Ben-Meir Q2906644, Susayeff Q2902132, Tzur Q2902009,
  Unna Q2909985, Assaf Q470672. [V]

## Second pass, 27 October 2026

Added after review of the first pass: this section adds to the note above and changes none of it. Where the first pass is
wrong, a dated correction sits right after the line, marked "second pass, 27 Oct". Refs #6. Research only: no image is added.

**Result of the second pass.**

- **Israel Rokach now has one qualifying file.** The first pass said no CC BY 4.0 frame names him. That was wrong: on the file
  pages, ten of the 17 Meitar frames in his category name him in the Hebrew title or the Hebrew caption, and one is titled with
  his name and office alone. I opened that frame's thumbnail and it shows one man, head and shoulders, with a sliver of a second
  face at the right edge and part of a sleeve at the left edge. Details in the table below. The likeness is for a person to check.
- **Haim Cohn still has no qualifying file.** Europeana (searched through its API) adds nothing from before 1980.
- **Knesset Archives file for Rokach: none found.** His Knesset member code is 620 (Wikidata P9770); no Commons file carries it.
- Still [GAP]: the Knesset portraits' photograph dates (the Knesset site is not reachable from here), the IDF archive catalogue
  search, the Israel State Archives, and the NLI record pages.

**Opened on 27 October 2026, and what each returned**

- **Wikimedia Commons API**, `https://commons.wikimedia.org/w/api.php`: file-page wikitext, `imageinfo` size and licence,
  the category lists of Israel Rokach (61 files) and Haim Cohn (10 files), the thumbnails of two frames.
  [V, agent read 27 Oct; a person's check pending]
- **Wikidata API** (`wbgetentities`): the item of Israel Rokach, Q129676, has P9770 (Knesset member ID) = 620 and P18 =
  `Israel Rokach 1950.jpg`. [V, agent read 27 Oct; a person's check pending]
- **Europeana search API** (`https://api.europeana.eu/record/v2/search.json`, the public demo key): titles, years and rights of
  the records returned. The Europeana item pages answered HTTP 403 (a browser check), so only the API's record fields were read.
  [V, agent read 27 Oct; a person's check pending]
- **GPO photo collection terms**, `https://gpophoto.gov.il/haetonot/Eng_ShowPage.aspx?id=30`: opens now (HTTP 200, a page headed
  "Terms"). Its summary says "You may: Use any photograph that appears on the Site, except photographs which bear the notice
  '© Copyright held by the photographer'", and "You are prohibited from: ... Creating derivative works". A crop is a derivative,
  and the licence also bars "commercial advertising or political activity", so it is not one of the four licences the project
  accepts. [V, agent read 27 Oct; a person's check pending] The gov.il photography page returned HTTP 403 (Cloudflare "Attention
  Required"). [GAP]
- **IDF and Defence Establishment Archive**, `https://archives.mod.gov.il/Pages/SearchCatalogTz.aspx`: the page opens (HTTP 200)
  but is a search form that runs in a browser; no search was run. [GAP] Its photographs carry a fee licence (first pass), so this
  source could add identity and date only, not a qualifying licence.
- **Knesset site**, `https://main.knesset.gov.il/en/mk/pages/mk.aspx?mkid=620` and the older `knesset.gov.il/mk/eng/mk_eng.asp`
  form: both answered HTTP 303 and then a page at `https://www.knesset.gov.il/maintenance-page-geo` whose text says "The site is
  temporarily inaccessible from your location". [V, agent read 27 Oct; a person's check pending] Not spoofed. [GAP]
- **NLI** (`https://www.nli.org.il/en/archives/NNL_ARCHIVE_AL997008137543605171/NLI`): HTTP 403, "Checking your browser...".
  **Israel State Archives** (`https://www.archives.gov.il/`): HTTP 403. [GAP]

### Israel Rokach: one file that meets the rule

| Person | File page (https://commons.wikimedia.org/wiki/File:...) | Licence | Size | Date as the page gives it | Identity as the page gives it | Note |
|---|---|---|---|---|---|---|
| Israel Rokach | `Tel_Aviv-Yafo_(997008137543605171).jpg` | CC BY 4.0 | 920x657 | 1949-02 | NLI title "ראש עיריית תל אביב, ישראל רוקח" (my translation: Mayor of Tel Aviv, Israel Rokach), depicted person Q129676 only [S] | single man in the thumbnail; a crop at least 512 px tall fits the 657 px frame |

- The page's wikitext, read 27 Oct: photographer Benno Rothenberg, institution National Library of Israel, call number "ARC. 4*
  2068 / 269 ארכיון בנו רותנברג", `{{cc-by-4.0|1={{Meitar Collection/license credit line|...}}}}`, permission ticket VRTS
  2021092910005788 (the same ticket as the first pass's Meitar files), categories "1949 in Tel Aviv-Yafo", "Israel Rokach",
  "Photographs by Benno Rothenberg". The category "Meitar Collection (review needed)" is not on it. [V, agent read 27 Oct; a
  person's check pending]
- Identity is the NLI title as copied onto Commons [S]; the NLI record page itself returned HTTP 403. The photograph is dated
  February 1949 and its title gives his office as mayor of Tel Aviv, so it is earlier than the 1952-56 period of this note.
- **Frame size.** The whole frame is 920x657 (so the 512 px height limit of `tools/tests/test_portraits_1948_51.py` leaves
  room). Part of a second face is at the right edge, so the crop has to keep clear of it; whether the result is large enough is for
  a person with the file.
- **Attribution.** CC BY 4.0 needs an attribution entry in NOTICE; the owner's comment on #6 after the Lavon, Dov Yosef, Yadin and
  Naftali candidates accepts Meitar CC BY 4.0 portraits on that condition. [V, agent read 27 Oct; a person's check pending]

### Why the other Rokach frames were not taken

The 17 Meitar frames in his category are all `{{cc-by-4.0|...}}`, by Benno Rothenberg, dated on the pages from 1948 to December 1949.
By the wikitext's depicted-person field, six show only Rokach (`...652905171`, `...653005171`, `...653105171`, `...160805171`,
`...543505171`, `...543605171`) and eleven name two or three people. The Hebrew title or caption names him in ten of the 17
(the title for `...543605171`, the caption for nine). Two were opened as images:

- `Tel_Aviv-Yafo_(997008137543505171).jpg` (February 1949, 936x673, welcoming the President): three men. Group shot, does not
  qualify. [V, agent read 27 Oct; a person's check pending]
- `Tel_Aviv-Yafo_(997008137543605171).jpg`: the one in the table above.

The other 15 were not opened as images. Of the six with only Rokach in the depicted-person field, three (`...652905171`,
`...653005171`, `...653105171`, 1948, 1451x949, 1459x932 and 1451x924) have a caption saying the mayor accompanies "them" on a visit, so they are not portraits by their own record; `...160805171` (936x653, 1949) has no
caption. [S] A person could open these. The 1948 to 1949 frames with two or more depicted people (Weizmann, the Health Minister,
the Interior Minister) are group frames by their own record and were not taken.

### Knesset Archives file for Rokach: searched again, none

Commons API, `list=search`, namespace 6 (File), 50 results asked, run 27 Oct 2026. URL form:
`https://commons.wikimedia.org/w/api.php?action=query&list=search&srnamespace=6&srlimit=50&format=json&srsearch=<query>`.
[V, agent read 27 Oct; a person's check pending]

| Query | Total hits | Note |
|---|---|---|
| `insource:"Knesset Archive\|1=620\|"` | 0 | code 620 is Rokach's (Wikidata P9770) |
| `insource:"Knesset Archive\|1=620"` | 0 | same without the closing bar |
| `insource:"Knesset Archive\|1=468\|"` | 2 | control: both are Pinchas Lavon files, so the pattern works |
| `insource:"Knesset Archive" "Rokach"` | 0 | |
| `insource:"Knesset Archive" "Rokah"` | 0 | |
| `insource:"Knesset Archive" "רוקח"` | 2 | two PDF documents, no photograph |
| `haswbstatement:P180=Q129676` | 1 | `Israel Rokach 1950.jpg`, the GPO file already refused |
| `insource:"Israel Rokach" insource:"Cc-by-4.0"` | 57 | includes the IPPA frames of a bridge opening named after him; with `-intitle:Opening` 17, the 17 Meitar frames |

Category "Israel Rokach", 61 files, by licence as the API reports it: Public domain 40, CC BY 4.0 17, CC BY-SA 3.0 2, CC BY-SA 4.0 1
(a graphic of the 1951 election, not a photograph), CC0 1 (the 1949 Nationaal Archief doorman frame of the first pass).

Europeana API, `https://api.europeana.eu/record/v2/search.json?wskey=<demo key>&query=<query>&rows=100&profile=standard`, 27 Oct:
`Israel Rokach` 38 records, titles read, none a photograph of him as the subject (a tennis-courts record, a bridge, unrelated
items); `ישראל רוקח` 4 records, none a photograph of him; `"Israel Rokach"` 0; `Rokach Tel Aviv mayor` 3 (a bridge dig of 1974
and a 1932 conference). [V, agent read 27 Oct; a person's check pending]

### Haim Cohn: still none

- Category "Haim Cohn", 10 files: 8 CC BY 4.0 (the five June 1965 Meitar frames titled "שופטי בית המשפט העליון." (Supreme Court
  judges) with no depicted person, the two June 1965 frames naming three judges, and one IPPA frame of 1980) and 2 Public domain
  (`Cohen haim.jpg`, PD-Israel only, 10 June 1952, refused in the first pass, and `6 - MekorotOilAgreement.jpg`).
- New in this pass: `6 - MekorotOilAgreement.jpg`, 4 March 1952, source Yad Levi Eshkol, description "Signature of the Ampal Contract
  ... Standing behind: Moshe Sharett, Levi Eshkol, Haim Cohen", licence `{{PD-Israel}}` only. A group shot and PD-Israel only: does
  not qualify. [V, agent read 27 Oct; a person's check pending]
- `haswbstatement:P180=Q1058964`: 13 hits, the same files as the category plus three IPPA frames of 1980 (FL45815251,
  FL45815269, FL45815294), all already in the first pass. Other Commons queries: `insource:"חיים כהן" insource:"היועץ המשפטי"` 0;
  `Chaim Cohn Israel justice` 1 (a PDF). [V, agent read 27 Oct; a person's check pending]
- Europeana, 27 Oct: `Haim Cohn` 48 records. Where a year is given it is between 1980 and 1986 (Hadani collection items such as
  "Movement tolerance with Judge Haim Cohen" of 1982, conference and prize-giving frames); one more, "Eli Rothschild Collection.,
  1986", carries CC BY-NC-ND 4.0. `Haim Cohen attorney general`, `חיים כהן היועץ המשפטי` and `Attorney General Cohen Israel`: 0 each.
  [V, agent read 27 Oct; a person's check pending]
- So the second pass found no qualifying file for Haim Cohn. "None found by these searches": the IDF catalogue and the Israel
  State Archives were not searched.

## Open for a person (not for this note)

- The likeness check of every file above (owner's rule on #6).
- [GAP] The photograph dates of the Knesset portraits. The Knesset Archives record pages, which may hold them, were not
  opened.
  (second pass, 27 Oct: the Knesset site answered "temporarily inaccessible from your location"; still [GAP]. [V, agent read 27 Oct; a person's check pending])
- [GAP] Haim Cohn and Israel Rokach: a search of the IDF archive catalogue, Europeana and the Israel State Archives.
  (second pass, 27 Oct: Europeana done, see below; the IDF catalogue, the Israel State Archives and the Knesset record pages
  stay [GAP].)
- Which of the two or three files per person to choose, and the crop (a crop must stay at least 512 px tall).
