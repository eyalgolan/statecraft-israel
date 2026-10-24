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
- **Not reachable:** `nli.org.il` record pages and `archives.gov.il` answered HTTP 403. [GAP]
- **Not opened:** the IDF archive's catalogue search (`SearchCatalogTz.aspx`), Europeana, the Knesset site itself, and
  any search by the people's names in the Israel State Archives. Anything that depends on them is [GAP], not "none exists".

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

### Israel Rokach (Wikidata Q129676) [V]

- Single-person portrait: `File:Israel_Rokach_1950.jpg`, GPO, Fritz Cohen, 1 October 1950, 2052x3066, "Portrait, Israel
  Rokach, mayor of Tel Aviv (Gen. Zionists)". `{{PD-Israel}}` only. **Does not qualify.**
- He was a Knesset member, but a search of the Knesset Archives pattern (`"Knesset member" Rokach`,
  `insource:"Knesset Archive" Rokach`, and the same in Hebrew) found no file. [GAP]
- 17 Meitar frames (Benno Rothenberg, 1948 to December 1949) sit in his category, headed only "Tel Aviv-Yafo (Israel)
  Photographs" and "Politicians Israel Photographs". None names him, so identity from the archive record fails. [S] The
  rest of the category is GPO or Zoltan Kluger material under PD-Israel and group shots (for example with Weizmann or
  Sharett). The Dutch Nationaal Archief CC0 file in the category (Willem van de Poll, 1949, a Knesset doorman in Tel Aviv)
  does not show him.

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

## Open for a person (not for this note)

- The likeness check of every file above (owner's rule on #6).
- [GAP] The photograph dates of the Knesset portraits. The Knesset Archives record pages, which may hold them, were not
  opened.
- [GAP] Haim Cohn and Israel Rokach: a search of the IDF archive catalogue, Europeana and the Israel State Archives.
- Which of the two or three files per person to choose, and the crop (a crop must stay at least 512 px tall).
