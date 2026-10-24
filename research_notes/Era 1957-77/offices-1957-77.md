# Offices, governments and office holders, 1957-1977

Researched 23 October 2026. Every URL below was read on that date. Research note for issue #113 (1957-77 item 1,
the research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is
drafted for the game, and no file in `content/` is changed. `content/offices.yaml`, `content/people.yaml` and
`content/governments.yaml` stay paused.

Second pass 24 October 2026, on the [GAP] rows the owner lists in their "Research part open" comment on #113 (the
vacancies of 1960, 1961, 1962 and 1965, Zeira's days, the blank party cells). What it opened and what it found is in
"Second pass" under Sources and in the bullets it adds to §1, §4, §5 and §6, each marked "(24 Oct)". Every page named
there was read by an agent on 24 October 2026; a person's check is pending.

## Purpose

Fix one sourced value for every government, minister and senior office holder in office on or after 1 January 1957
(the era's start) up to 20 June 1977, when [offices-1977-93.md](../Era%201977-93/offices-1977-93.md) takes over, so
that `governments.yaml`, `offices.yaml` and `people.yaml` can be filled later without new research. It answers the
*to verify* marks in §2.1 of [the 1957-1977 design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md)
(Dayan's start, Begin's government, the unity government's date, Zeira's days) in §5, and gives `living` for every
person in §8.

## Conventions

- [V] read this session in a primary source: the Knesset's position table (K) or the Knesset's government pages (W)
  (see Sources). Every [V] here is an agent read; a person's check is pending. For Presidents and the Chief of Staff, "[V] years" means the primary source gives the year only.
- [S] secondary only: English or Hebrew Wikipedia, or Wikidata.
- [C] two sources disagree; every value is given.
- [GAP] not found in any source read. A gap stays a gap: no holder or date is filled in by inference.
- Periods are [from, to), as in `people.yaml`: `to` is the day the next holder or government took office.
- "Knesset row(s)" are `PersonToPositionID` values in K. Two ids mean the Knesset split one term where a new Knesset
  convened (30 November 1959, 4 September 1961, 22 November 1965, 17 November 1969, 21 January 1974, 13 June 1977);
  they are joined here, as in offices-1977-93.md.
- "W agrees" means W shows the same holder with the same two dates. W's own note explains most of its gaps: "Note:
  The Prime Minister automatically assumes any ministerial position vacated until the official appointment of
  another minister or acting minister." K records those holdings as terms of the Prime Minister in the vacated office; W leaves them out.
- "Our reading" marks a conclusion drawn here from the sources, not stated by any of them.
- Quotes keep the source's wording, except that its en dashes are written here as hyphens. Quotes from Hebrew
  sources are this note's translation; the Hebrew is given beside them where the wording carries the fact.
- "en.wiki *title*" and "he.wiki *title*" mean the raw wikitext of that page at the revision (oldid) given in Sources.

## Sources

Primary:

- **K**: the Knesset position table, `KNS_PersonToPosition`, the source `people.yaml` already uses for 1948-1956 and
  offices-1977-93.md for 1977-1993. Row *n* is `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition(n)`;
  the per-government query is
  `https://knesset.gov.il/Odata/ParliamentInfo.svc/KNS_PersonToPosition?$filter=GovernmentNum%20eq%20N` for N = 7 to
  17. Names from `KNS_Person`, position types from `KNS_Position`. From this machine every knesset.gov.il URL
  (including fs.knesset.gov.il) answers with a redirect to `https://www.knesset.gov.il/maintenance-page-geo`, so the
  rows were read from the Public Knowledge Workshop's mirror of the same tables, which keeps the Knesset's ids and
  fields unchanged:
  `https://production.oknesset.org/pipelines/data/members/kns_persontoposition/kns_persontoposition.csv`
  (23,690 rows, last `LastUpdatedDate` 18 October 2026), with `.../members/kns_person/kns_person.csv` and
  `.../members/kns_position/kns_position.csv`. Ministers are position types 39 and 57 (שר, שרה), the Prime Minister
  45, the Deputy Prime Minister 50, the interim Prime Minister 45 with `DutyDesc` "ראש הממשלה בפועל", the Speaker 122.
- **W**: the Knesset's English government pages, `http://knesset.gov.il/govt/eng/GovtByNumber_eng.asp?govt=N`,
  read in the Wayback Machine's snapshots of 17 November 2014 (one URL per government, given at the head of each
  table in §2; government 18: `https://web.archive.org/web/20141117051013/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=18`).
  Each page lists every minister with dates and a faction, and says: "** Faction name represents the Knesset
  parliamentary group in which the minister was a member on the date of his/her entry into the government."
- The Ministry of Foreign Affairs page "The Presidents of the State of Israel", Wayback snapshot of 17 December 2015:
  `https://web.archive.org/web/20151217052607/http://mfa.gov.il/MFA/AboutIsrael/State/Democracy/Pages/The%20Presidents%20of%20the%20State%20of%20Israel.aspx`
  (years only).
- Encyclopaedia Judaica, 2nd edition, on encyclopedia.com: EJ *Elazar, David*
  (`https://www.encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/elazar-david`) and EJ *Laskov,
  Hayyim* (`.../laskov-hayyim`).

Not reached: knesset.gov.il and fs.knesset.gov.il directly (the geo redirect above), so no plenum record (Divrei
HaKnesset) was read for 1957-1977; the live embassies.gov.il copy of the MFA page (an empty shell; the archived copy
was read). No IDF, Mossad or Israel Government Year Book page was read. Encyclopaedia Judaica entries for Shazar,
Katzir, Ben-Zvi, Zeira, Yariv, Amit and Harel were not found at the encyclopedia.com addresses tried (404).

Second pass, read on 24 October 2026 (agent read; a person's check pending). JTA = the Jewish Telegraphic Agency's
daily bulletins, `https://www.jta.org/archive/<slug>`, contemporary press, [S]. The bulletin date is the date JTA
printed (the page's `datePublished`). A bulletin's items carry their own dateline ("JERUSALEM, Oct. 16. (JTA)"), which
is one day before the bulletin date, and the items' "yesterday" and "last night" are read against that dateline, not
against the bulletin date. The datelines were read on the bulletin pages (the "See Original Daily Bulletin" PDFs on
`pdfs.jta.org`, scanned images), 24 October 2026:

- JTA `israel-mourns-death-of-rabbi-toledano-minister-for-religious-affairs` (17 October 1960),
  `ramat-gan-chief-rabbi-advanced-as-successor-to-religious-ministry-post` (25 October 1960),
  `binyamin-mintz-israels-minister-of-posts-dies-in-tel-aviv-was-58` (1 June 1961),
  `dr-josephtal-israel-minister-dies-of-heart-attack-in-switzerland` (23 August 1962) and
  `dr-josephtal-funeral-today-will-be-buried-in-mapai-settlement` (24 August 1962),
  `israel-bar-yehuda-israel-minister-of-transport-dead-was-69` (6 May 1965),
  `moshe-carmiel-coopted-as-israel-cabinet-member-succeeds-bar-yehuda` (1 June 1965),
  `agranat-recommendations-released` (3 April 1974), `nation-stunned-by-agranat-committee-report-many-see-elazar-as-scapegoat`
  and `new-appointments-announced` (both 4 April 1974).
- The Knesset's lexicon page "Agranat Commission", Wayback snapshot of 9 April 2019:
  `https://web.archive.org/web/20190409175502/https://www.knesset.gov.il/lexicon/eng/agranat_eng.htm` [V].
- Ran Edelist, "Kakh nirkma ha-hafta'a she-lo hayta be-milkhemet Yom Kippur", Maariv, 1 October 2022:
  `https://www.maariv.co.il/news/military/article-948243` [S].
- K again, for the empty windows and the party cells: the same three mirror files as above (`kns_persontoposition`,
  `kns_person`, `kns_position`), position type 54 ("חבר/ת סיעה", faction member, with `FactionName`), read 24 October.
  W again: the Wayback snapshots of 17 November 2014 for governments 7 to 17, to list the rows whose faction cell is
  empty.
- he.wiki *ממשלת ישראל התשיעית* (oldid 42592809), for the dates only.

Searched on 24 October 2026 and not usable: the Detroit Jewish News and a Georgia newspaper's reprints of the JTA
Toledano story (snippets only, the pages answered 403 to this machine); JPress / the National Library newspaper
pages (Davar, Maariv, Hatzofe; 403 from this machine); knesset.gov.il (geo redirect, as above).

Secondary, read as raw wikitext through the MediaWiki API at these revisions:

- en.wiki *List of presidents of Israel* (oldid 1371184272), *Chief of the General Staff (Israel)* (oldid
  1374697957), *Military Intelligence Directorate (Israel)* (oldid 1367978116), *Mossad* (oldid 1379341978), *Eli
  Zeira* (oldid 1373692513), *Moshe Dayan* (oldid 1379174185), *Thirteenth government of Israel* (oldid 1333303933).
- he.wiki *ראש המטה הכללי* (oldid 43910926), *אגף המודיעין* (oldid 44053769), *המוסד למודיעין ולתפקידים מיוחדים*
  (oldid 44101791), *אלי זעירא* (oldid 43993235), *משה דיין* (oldid 44078398), *נשיא מדינת ישראל* (oldid 43897250).
- Wikidata, birth (P569) and death (P570) dates, one item per person (Q-ids in §8), read through
  `https://www.wikidata.org/w/api.php?action=wbgetentities`.

URLs: `https://en.wikipedia.org/w/index.php?oldid=<oldid>`, `https://he.wikipedia.org/w/index.php?oldid=<oldid>`,
`https://www.wikidata.org/wiki/<Q-id>`.

## 1. Governments

Every `from` and `to` below is the date in K's Prime Minister rows; W's page header gives the same two dates for
every government. K and W do not give the hour, the confidence vote or the swearing-in, and no plenum record was
read (Sources), so these are the dates of office as the Knesset records them.

| Government | Prime Minister | From | To | K rows (Prime Minister) | Mark |
|---|---|---|---|---|---|
| 7th | David Ben-Gurion | 3 November 1955 | 7 January 1958 | 4023 | [V] K and W agree |
| 8th | David Ben-Gurion | 7 January 1958 | 17 December 1959 | 4024, 22297 | [V] K and W agree |
| 9th | David Ben-Gurion | 17 December 1959 | 2 November 1961 | 4025, 22347 | [V] K and W agree |
| 10th | David Ben-Gurion | 2 November 1961 | 26 June 1963 | 4026 | [V] K and W agree |
| 11th | Levi Eshkol | 26 June 1963 | 22 December 1964 | 4560 | [V] K and W agree |
| 12th | Levi Eshkol | 22 December 1964 | 12 January 1966 | 4053, 22391 | [V] K and W agree |
| 13th | Levi Eshkol, died in office; then Yigal Allon, interim | 12 January 1966 | 17 March 1969 | Eshkol 4054 (to 26 February 1969); Allon 21297 (26 February to 17 March 1969) | [V] K and W agree. K: Allon "ראש הממשלה בפועל"; W: "Interim Prime Minister Yigal Allon 26/02/1969 - 17/03/1969"; W: Eshkol "12/01/1966 - 26/02/1969 (Died)" |
| 14th | Golda Meir | 17 March 1969 | 15 December 1969 | 3929, 22430 | [V] K and W agree |
| 15th | Golda Meir | 15 December 1969 | 10 March 1974 | 3930, 22544 | [V] K and W agree |
| 16th | Golda Meir | 10 March 1974 | 3 June 1974 | 3570 | [V] K and W agree |
| 17th | Yitzhak Rabin | 3 June 1974 | 20 June 1977 | 3453, 22727 | [V] K and W agree. No Peres row as Prime Minister or acting Prime Minister in K or W for 1977 (the design's §4: not a term) |
| 18th | Menachem Begin | 20 June 1977 | 5 August 1981 | see offices-1977-93.md §1 | [V] W header "20/06/1977 - 05/08/1981"; the plenum record of the vote is in offices-1977-93.md |

Changes of office that K and W fix, in date order (all [V] unless marked):

- **1 July 1958 (8th government).** The National Religious Party ministers leave: W ends Burg (Posts) and Shapira
  (Religions, Welfare) on 01/07/1958, and K gives their offices to Ben-Gurion from 1 July 1958 (rows 22293, 22294,
  22295) until Barzilai (Posts, 24 November 1958), Toledano (Religions, 3 December 1958) and Naftali (Welfare, 26
  January 1959) take them.
- **30 November 1959.** K ends Luz at Agriculture on 30 November 1959, the day his Speaker row 1274 starts, and gives
  Agriculture to Ben-Gurion to 17 December 1959; W keeps Luz at Agriculture to 17 December. [C], §3.
- **Toledano's death, 15 October 1960.** W: "15/10/1960 (Died)"; K has no holder of Religions from 15 to 24 October
  1960, then Ben-Gurion (row 22360). [V]; the holder for those nine days is a [GAP] (W's note would give it to the
  Prime Minister; K does not record it).
  (24 Oct) JTA, 17 October 1960: Toledano "died suddenly yesterday, at his home here, aged 79", and the funeral was
  attended by "President Izhak Ben-Zvi and Prime Minister David Ben-Gurion"; 15 October 1960 was a Saturday, and
  the item is datelined "JERUSALEM, Oct. 16. (JTA)", so "yesterday" is 15 October, which is W's date [S]. JTA, bulletin of 25 October 1960, item datelined "JERUSALEM, Oct. 24. (JTA)", the day K starts Ben-Gurion: Rabbi Eliahu Pardess "was reported
  today to be the most likely candidate for the office of the Minister for Religious Affairs ... The office was vacated
  earlier this month by the death of Rabbi Yaacov Toledano." It names no acting minister. Searched in the K mirror: no
  row of any person at the Ministry of Religions between 15 and 24 October 1960 (rows with a ministry name or a
  `DutyDesc` over the window, Knessets 3 to 6). Still [GAP] on the holder; K's Ben-Gurion row 22360 starts the day
  JTA still speaks of a vacancy, which does not contradict W's note, since the Prime Minister holds a vacated
  office until another is named.
- **Mintz's death, 30 May 1961.** W: "30/05/1961 (Died)". Neither K nor W names a Minister of Posts from 30 May to 2
  November 1961. [GAP] on the holder; vacant on both sources' rows.
  (24 Oct) JTA, 1 June 1961: Mintz "died in Tel Aviv last night at the age of 58"; the item is datelined "JERUSALEM, May 31. (JTA)", so "last night" is
  30 May, which is W's date [S]. It names no one for the Posts. The K mirror has no row at the Ministry of Posts
  (משרד הדואר) between Mintz's end (30 May) and Sasson (2 November 1961), and no `DutyDesc` of an acting minister over
  the window. Our reading, from he.wiki *ממשלת ישראל התשיעית*, which says the government "collapsed after" Ben-Gurion's
  resignation of 31 January 1961: the 9th government may have sat as a continuing government through the vacancy (he.wiki, same page, "והממשלה הוסיפה לכהן כממשלת מעבר במשך כ-10 חודשים": "and the government continued to serve as a
  transitional government for about 10 months"; K
  carries its ministers again from 4 September to 2 November 1961, rows such as 22347 to 22372). This is a reading,
  not a source on the Posts.
  Still [GAP] on the holder.
- **Yoseftal's death, 23 August 1962.** W: "02/11/1961 - 23/08/1962 (Died)" at Development and Housing. Neither source
  names a holder until Almogi (K 29 October, W 30 October 1962). [GAP] on the holder.
  (24 Oct) JTA, 23 August 1962: Dr. Josephtal, "Minister of Development and Housing", "died in Switzerland this
  morning after a heart attack", which confirms the day [S]. Same bulletin: the Cabinet's memorial session was led by
  "Acting Premier Levi Eshkol" while Ben-Gurion was on a state visit to Scandinavia; that is an acting Prime Minister,
  not a holder of the two ministries. JTA 24 August 1962 (funeral) names no holder either. The K mirror has no row at
  the Ministry of Development (משרד הפיתוח) or Housing (משרד השיכון) from 23 August to 29 October 1962 except Almogi's
  from 29 October (rows 21341, 21342). Still [GAP] on the holder.
- **Bar-Yehuda's death, 4 May 1965.** W: "22/12/1964 - 04/05/1965 (Died)" at Transportation. No holder in either
  source until Carmel (K 31 May, W 30 May 1965). [GAP] on the holder.
  (24 Oct) JTA, 6 May 1965: Bar-Yehuda, "Minister of Transport and Communications", "died here last night after a
  brief illness", the item is datelined "JERUSALEM, May 5. (JTA)", so "last night" is 4 May, which is W's date [S]. JTA, 1 June 1965: "The Cabinet yesterday decided to coopt Moshe Carmel,
  Achdut Avoda member of the Knesset, to serve as Minister of Transport, in place of Israel Bar-Yehuda who died earlier
  this month". The item is datelined "JERUSALEM, May 31. (JTA)", so "yesterday" is 30 May: JTA gives 30 May [S],
  W's date, against K's 31 May (item 8 of §3 stays [C], with JTA added on W's side). The same bulletin: Zadok takes Development and
  Eshkol takes Housing; the persons match K and W, the dates are those of §1 (K Zadok at Development from 25 May and
  Eshkol at Housing from 31 May, W Zadok at Development from 31 May). The K mirror has no row at the Ministry of Transport (משרד התחבורה)
  between Bar-Yehuda's end (4 May) and Carmel (row 3916, 31 May 1965). Still [GAP] on the holder.
- **What the second pass closed in §1's four vacancies (24 Oct).** Nothing about the holders: no contemporary source
  read (JTA; K) names an acting minister for 15-24 October 1960, 30 May-2 November 1961, 23 August-29 October 1962
  or 4-30 May 1965, and K, which does record the Prime Minister as holder of other vacated offices (rows 22293,
  22295, 22364), has no row for these four. All four stay [GAP] on the holder. JTA confirms the four death dates and
  gives 30 May for Carmel, with W against K. What is not tried: the Divrei HaKnesset, the Government's decisions and
  Yalkut HaPirsumim (knesset.gov.il and the State Archives catalogue were not reachable), and Davar / Hatzofe on JPress
  (403 from this machine). A person with access to those is the next step.
- **5 June 1967.** Gahal (Begin, Yosef Sapir) and Rafi (Dayan) enter the 13th government: §5.
- **Eshkol's death, 26 February 1969**, and Allon's interim term: the table above.
- **6 August 1970.** Gahal leaves the 15th government: K and W end Begin, Dulzin (W: Dolchin), Landau, Rimalt, Yosef
  Sapir and Weizman on 6 August 1970; K gives Development, Communications, Trade and Transportation to Meir from 6
  August to 1 September 1970 (rows 22785 to 22788). This matches the design's "6 Aug 1970".
- **Shapira's death, 16 July 1970.** W: "15/12/1969 - 16/07/1970 (Died)"; K: Meir as "ממלאת מקום שר הפנים" (acting
  Minister of the Interior) 16 July to 1 September 1970 (row 22547), then Burg.
- **Communications.** K keeps the Posts ministry's title "שר הדואר" for Peres to 3 January 1971 and gives "שר
  התקשורת" from that day (rows 3595, 22569); W calls Peres "Minister of Communications" from 01/09/1970.
- **The 1976 crisis (17th government).** The National Religious Party ministers leave: K ends Burg (Interior),
  Rafael (Religions) and Hammer (Welfare) on 20 December 1976; W gives 22/12/1976. K gives their offices to Rabin
  until 18 January 1977 (W: the new holders from 16/01/1977). [C], §3.

## 2. Ministers, by government

All terms in K that run on or after 1 January 1957, joined across Knesset splits, with W's value beside K's. Office
ids are those in `content/offices.yaml`; "(to add)" and "[other]" are offices the content does not have (see §7).
Terms that started before 1957 show their real start. Deputy ministers are not in this note (#113 asks for ministers);
W lists them under "Deputy Ministers" on each page.

### Government 7

W: https://web.archive.org/web/20141117051204/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=7

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | David Ben-Gurion | 3 November 1955 | 7 January 1958 | 4023 | [V] K and W agree |
| defense_minister | David Ben-Gurion | 3 November 1955 | 7 January 1958 | 22291 | [V] K and W agree |
| finance_minister | Levi Eshkol | 3 November 1955 | 7 January 1958 | 4059 | [V] K and W agree |
| foreign_minister | Golda Meir | 19 June 1956 | 7 January 1958 | 22188 | [V] K and W agree |
| agriculture_minister | Kadish Luz | 3 November 1955 | 7 January 1958 | 3471 | [V] K and W agree |
| education_minister | Zalman Aran | 3 November 1955 | 7 January 1958 | 4049 | [V] K and W agree |
| health_minister | Yisrael Barzilai | 3 November 1955 | 7 January 1958 | 3992 | [V] K and W agree |
| interior_minister | Yisrael Bar-Yehuda | 3 November 1955 | 7 January 1958 | 3996 | [V] K and W agree |
| justice_minister | Pinchas Rosen | 28 February 1956 | 7 January 1958 | 22189 | [V] K and W agree |
| labour_minister | Mordechai Namir | 19 June 1956 | 7 January 1958 | 3905 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 3 November 1955 | 7 January 1958 | 3818 | [V] K and W agree |
| posts_minister | Yosef Burg | 3 November 1955 | 7 January 1958 | 4241 | [V] K and W agree |
| religions_minister | Haim-Moshe Shapira | 3 November 1955 | 7 January 1958 | 3802 | [V] K and W agree |
| trade_minister | Pinchas Sapir | 3 November 1955 | 7 January 1958 | 3502 | [V] K and W agree |
| transportation_minister | Moshe Carmel | 3 November 1955 | 7 January 1958 | 3469 | [V] K and W agree |
| welfare_minister | Haim-Moshe Shapira | 3 November 1955 | 7 January 1958 | 3797 | [V] K and W agree |
| development_minister | Mordechai Bentov | 3 November 1955 | 7 January 1958 | 4572 | [V] K and W agree |
| minister_without_portfolio | Peretz Naftali | 3 November 1955 | 7 January 1958 | 3908 | [V] K and W agree |

### Government 8

W: https://web.archive.org/web/20141117051208/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=8

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | David Ben-Gurion | 7 January 1958 | 17 December 1959 | 4024, 22297 | [V] K and W agree |
| defense_minister | David Ben-Gurion | 7 January 1958 | 17 December 1959 | 22292, 22344 | [V] K and W agree |
| finance_minister | Levi Eshkol | 7 January 1958 | 17 December 1959 | 4060, 22306 | [V] K and W agree |
| foreign_minister | Golda Meir | 7 January 1958 | 17 December 1959 | 3409, 22311 | [V] K and W agree |
| agriculture_minister | Kadish Luz | 7 January 1958 | 30 November 1959 | 3472 | [C] W: to 17 December 1959; K: to 30 November 1959 (see the next row) |
| agriculture_minister | David Ben-Gurion | 30 November 1959 | 17 December 1959 | 22296 | [C] W: Luz Agriculture to 17 December 1959, no Ben-Gurion row; K: Luz left on 30 November 1959, the day his Speaker row 1274 starts (§4) |
| education_minister | Zalman Aran | 7 January 1958 | 17 December 1959 | 4555, 22305 | [V] K and W agree |
| health_minister | Yisrael Barzilai | 7 January 1958 | 17 December 1959 | 3993, 22308 | [V] K and W agree |
| interior_minister | Yisrael Bar-Yehuda | 7 January 1958 | 17 December 1959 | 3997, 22309 | [V] K and W agree |
| justice_minister | Pinchas Rosen | 7 January 1958 | 17 December 1959 | 3854, 22315 | [V] K and W agree |
| labour_minister | Mordechai Namir | 7 January 1958 | 17 December 1959 | 3906, 22312 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 7 January 1958 | 17 December 1959 | 3819, 22316 | [V] K and W agree |
| posts_minister | Yosef Burg | 7 January 1958 | 1 July 1958 | 4242 | [V] K and W agree |
| posts_minister | David Ben-Gurion | 1 July 1958 | 24 November 1958 | 22293 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| posts_minister | Yisrael Barzilai | 24 November 1958 | 17 December 1959 | 22302, 22345 | [V] K and W agree |
| religions_minister | Haim-Moshe Shapira | 7 January 1958 | 1 July 1958 | 3383 | [V] K and W agree |
| religions_minister | David Ben-Gurion | 1 July 1958 | 3 December 1958 | 22295 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| religions_minister | Ya'akov Moshe Toledano | 3 December 1958 | 17 December 1959 | 4428, 22317 | [C] W: 3 December 1958 to 30 November 1959; K: to 17 December 1959 |
| trade_minister | Pinchas Sapir | 7 January 1958 | 17 December 1959 | 3592, 22313 | [V] K and W agree |
| transportation_minister | Moshe Carmel | 7 January 1958 | 17 December 1959 | 3470, 22310 | [V] K and W agree |
| welfare_minister | Haim-Moshe Shapira | 7 January 1958 | 1 July 1958 | 3798 | [V] K and W agree |
| welfare_minister | David Ben-Gurion | 1 July 1958 | 26 January 1959 | 22294 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| welfare_minister | Peretz Naftali | 26 January 1959 | 17 December 1959 | 22303, 23448 | [C] W: from 25 January 1959; K: 26 January 1959 |
| development_minister | Mordechai Bentov | 7 January 1958 | 17 December 1959 | 4035, 22307 | [V] K and W agree |
| minister_without_portfolio | Peretz Naftali | 7 January 1958 | 26 January 1959 | 3909 | [C] W: to 25 January 1959; K: 26 January 1959 |
| minister_without_portfolio | Kadish Luz | 30 November 1959 | 17 December 1959 | 22304 | [V] K only: W has no row; Luz was Speaker from 30 November 1959 (K row 1274) |

### Government 9

W: https://web.archive.org/web/20141117051214/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=9

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | David Ben-Gurion | 17 December 1959 | 2 November 1961 | 4025, 22347 | [V] K and W agree |
| defense_minister | David Ben-Gurion | 17 December 1959 | 2 November 1961 | 22348, 22349 | [V] K and W agree |
| finance_minister | Levi Eshkol | 17 December 1959 | 2 November 1961 | 4036, 22354 | [V] K and W agree |
| foreign_minister | Golda Meir | 17 December 1959 | 2 November 1961 | 3891, 22363 | [V] K and W agree |
| agriculture_minister | Moshe Dayan | 17 December 1959 | 2 November 1961 | 3982, 22359 | [V] K and W agree |
| education_minister | Zalman Aran | 17 December 1959 | 10 May 1960 | 4556 | [V] K and W agree |
| education_minister | David Ben-Gurion | 10 May 1960 | 3 August 1960 | 4032 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| education_minister | Abba Eban | 3 August 1960 | 2 November 1961 | 4403, 22350 | [V] K and W agree |
| health_minister | Yisrael Barzilai | 17 December 1959 | 2 November 1961 | 3994, 22358 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 17 December 1959 | 2 November 1961 | 3384, 22370 | [V] K and W agree |
| justice_minister | Pinchas Rosen | 17 December 1959 | 2 November 1961 | 3855, 22368 | [V] K and W agree |
| labour_minister | Giora Yoseftal | 17 December 1959 | 2 November 1961 | 3931, 22362 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 17 December 1959 | 2 November 1961 | 3820, 22369 | [V] K and W agree |
| posts_minister | David Ben-Gurion | 17 December 1959 | 5 July 1960 | 22364 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| posts_minister | Bechor-Shalom Sheetrit | 5 July 1960 | 18 July 1960 | 22365 | [V] K only: W has no row for Sheetrit at Posts |
| posts_minister | Binyamin Mintz | 18 July 1960 | 30 May 1961 | 4441 | [C] W: from 17 July 1960; K: 18 July 1960. Mintz died in office on 30 May 1961 (W: "(Died)") |
| religions_minister | Ya'akov Moshe Toledano | 17 December 1959 | 15 October 1960 | 4429 | [V] K and W agree |
| religions_minister | David Ben-Gurion | 24 October 1960 | 2 November 1961 | 22360, 22361 | [V] W: no row; the PM held the vacated office (W's note, §1); K has no holder 15-24 October 1960 |
| trade_minister | Pinchas Sapir | 17 December 1959 | 2 November 1961 | 3874, 22366 | [V] K and W agree |
| transportation_minister | Yitzhak Ben-Aharon | 17 December 1959 | 2 November 1961 | 4018, 22356 | [V] K and W agree |
| welfare_minister | Yosef Burg | 17 December 1959 | 2 November 1961 | 4234, 22355 | [V] K and W agree |
| development_minister | Mordechai Bentov | 17 December 1959 | 2 November 1961 | 4010, 22357 | [V] K and W agree |
| minister_without_portfolio | Abba Eban | 17 December 1959 | 3 August 1960 | 4399 | [V] K and W agree |

### Government 10

W: https://web.archive.org/web/20141117043003/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=10

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | David Ben-Gurion | 2 November 1961 | 26 June 1963 | 4026 | [V] K and W agree |
| defense_minister | David Ben-Gurion | 2 November 1961 | 26 June 1963 | 4571 | [V] K and W agree |
| finance_minister | Levi Eshkol | 2 November 1961 | 26 June 1963 | 4559 | [V] K and W agree |
| foreign_minister | Golda Meir | 2 November 1961 | 26 June 1963 | 3892 | [V] K and W agree |
| agriculture_minister | Moshe Dayan | 2 November 1961 | 26 June 1963 | 3983 | [V] K and W agree |
| education_minister | Abba Eban | 2 November 1961 | 26 June 1963 | 4552 | [V] K and W agree |
| health_minister | Haim-Moshe Shapira | 2 November 1961 | 26 June 1963 | 3792 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 2 November 1961 | 26 June 1963 | 3810 | [V] K and W agree |
| justice_minister | Dov Yosef | 2 November 1961 | 26 June 1963 | 3953 | [V] K and W agree |
| labour_minister | Yigal Allon | 2 November 1961 | 26 June 1963 | 4085 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 2 November 1961 | 26 June 1963 | 3821 | [V] K and W agree |
| posts_minister | Eliyahu Sasson | 2 November 1961 | 26 June 1963 | 3773 | [V] K and W agree |
| religions_minister | Zorach Warhaftig | 2 November 1961 | 26 June 1963 | 3933 | [V] K and W agree |
| trade_minister | Pinchas Sapir | 2 November 1961 | 26 June 1963 | 3875 | [V] K and W agree |
| transportation_minister | Yitzhak Ben-Aharon | 2 November 1961 | 28 May 1962 | 4567 | [V] K and W agree |
| transportation_minister | Yisrael Bar-Yehuda | 28 May 1962 | 26 June 1963 | 3998 | [V] K and W agree |
| welfare_minister | Yosef Burg | 2 November 1961 | 26 June 1963 | 4235 | [V] K and W agree |
| development_minister | Giora Yoseftal | 2 November 1961 | 23 August 1962 | 3963 | [V] K and W agree |
| development_minister | Yosef Almogi | 29 October 1962 | 26 June 1963 | 21341 | [C] W: from 30 October 1962; K: 29 October 1962 |
| minister_without_portfolio | Yosef Almogi | 2 November 1961 | 29 October 1962 | 4062 | [C] W: to 30 October 1962; K: 29 October 1962 |
| [other] housing | Giora Yoseftal | 2 November 1961 | 23 August 1962 | 3932 | [V] K and W agree |
| [other] housing | Yosef Almogi | 29 October 1962 | 26 June 1963 | 21342 | [C] W: from 30 October 1962; K: 29 October 1962 |

### Government 11

W: https://web.archive.org/web/20141117043009/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=11

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Levi Eshkol | 26 June 1963 | 22 December 1964 | 4560 | [V] K and W agree |
| deputy_prime_minister | Abba Eban | 26 June 1963 | 22 December 1964 | 21265 | [V] K and W agree |
| defense_minister | Levi Eshkol | 26 June 1963 | 22 December 1964 | 4012 | [V] K and W agree |
| finance_minister | Pinchas Sapir | 26 June 1963 | 22 December 1964 | 3870 | [V] K and W agree |
| foreign_minister | Golda Meir | 26 June 1963 | 22 December 1964 | 3893 | [V] K and W agree |
| agriculture_minister | Moshe Dayan | 26 June 1963 | 9 November 1964 | 3984 | [C] W: to 4 November 1964; K: 9 November 1964 (Gvati from 9 November in both) |
| agriculture_minister | Haim Gvati | 9 November 1964 | 22 December 1964 | 4004 | [V] K and W agree |
| education_minister | Zalman Aran | 26 June 1963 | 22 December 1964 | 4050 | [V] K and W agree |
| health_minister | Haim-Moshe Shapira | 26 June 1963 | 22 December 1964 | 3793 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 26 June 1963 | 22 December 1964 | 3786 | [V] K and W agree |
| justice_minister | Dov Yosef | 26 June 1963 | 22 December 1964 | 3954 | [V] K and W agree |
| labour_minister | Yigal Allon | 26 June 1963 | 22 December 1964 | 4086 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 26 June 1963 | 22 December 1964 | 3822 | [V] K and W agree |
| posts_minister | Eliyahu Sasson | 26 June 1963 | 22 December 1964 | 3774 | [V] K and W agree |
| religions_minister | Zorach Warhaftig | 26 June 1963 | 22 December 1964 | 3934 | [V] K and W agree |
| trade_minister | Pinchas Sapir | 26 June 1963 | 22 December 1964 | 3876 | [V] K and W agree |
| transportation_minister | Yisrael Bar-Yehuda | 26 June 1963 | 22 December 1964 | 3999 | [V] K and W agree |
| welfare_minister | Yosef Burg | 26 June 1963 | 22 December 1964 | 22374 | [V] K and W agree |
| development_minister | Yosef Almogi | 26 June 1963 | 22 December 1964 | 4037 | [V] K and W agree |
| [other] minister in the PM's office | Akiva Govrin | 2 December 1963 | 22 December 1964 | 4009 | [C] W: Minister Without Portfolio from 1 December 1963; K: "שר במשרד ראש הממשלה" from 2 December 1963 |
| [other] housing | Yosef Almogi | 26 June 1963 | 22 December 1964 | 4041 | [V] K and W agree |

### Government 12

W: https://web.archive.org/web/20141117043013/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=12

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Levi Eshkol | 22 December 1964 | 12 January 1966 | 4053, 22391 | [V] K and W agree |
| deputy_prime_minister | Abba Eban | 22 December 1964 | 12 January 1966 | 21266, 21267 | [V] K and W agree |
| defense_minister | Levi Eshkol | 22 December 1964 | 12 January 1966 | 22392, 22393 | [V] K and W agree |
| finance_minister | Pinchas Sapir | 22 December 1964 | 12 January 1966 | 3871, 22405 | [V] K and W agree |
| foreign_minister | Golda Meir | 22 December 1964 | 12 January 1966 | 3894, 22402 | [V] K and W agree |
| agriculture_minister | Haim Gvati | 22 December 1964 | 12 January 1966 | 22410, 22411 | [V] K and W agree |
| education_minister | Zalman Aran | 22 December 1964 | 12 January 1966 | 4051, 22395 | [V] K and W agree |
| health_minister | Haim-Moshe Shapira | 22 December 1964 | 12 January 1966 | 3794, 22407 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 22 December 1964 | 12 January 1966 | 3787, 22408 | [V] K and W agree |
| justice_minister | Dov Yosef | 22 December 1964 | 12 January 1966 | 3955, 22398 | [V] K and W agree |
| labour_minister | Yigal Allon | 22 December 1964 | 12 January 1966 | 4087, 21294 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 22 December 1964 | 12 January 1966 | 3823, 22406 | [V] K and W agree |
| posts_minister | Eliyahu Sasson | 22 December 1964 | 12 January 1966 | 3775, 22409 | [V] K and W agree |
| religions_minister | Zorach Warhaftig | 22 December 1964 | 12 January 1966 | 3935, 22400 | [V] K and W agree |
| trade_minister | Pinchas Sapir | 22 December 1964 | 25 May 1965 | 3593 | [V] K and W agree |
| trade_minister | Haim Yosef Zadok | 25 May 1965 | 12 January 1966 | 3884, 22404 | [V] K and W agree |
| transportation_minister | Yisrael Bar-Yehuda | 22 December 1964 | 4 May 1965 | 4000 | [V] K and W agree |
| transportation_minister | Moshe Carmel | 31 May 1965 | 12 January 1966 | 3916, 22401 | [C] W: from 30 May 1965; K: 31 May 1965 |
| welfare_minister | Yosef Burg | 22 December 1964 | 12 January 1966 | 22375, 22376 | [V] K and W agree |
| development_minister | Yosef Almogi | 22 December 1964 | 25 May 1965 | 4038 | [V] K and W agree |
| development_minister | Haim Yosef Zadok | 25 May 1965 | 12 January 1966 | 3886, 22403 | [C] W: from 31 May 1965; K: 25 May 1965 |
| [other] housing | Yosef Almogi | 22 December 1964 | 25 May 1965 | 4042 | [C] W: to 23 May 1965; K: 25 May 1965 |
| [other] housing | Levi Eshkol | 31 May 1965 | 12 January 1966 | 4017, 22394 | [V] K and W agree |
| [other] tourism | Akiva Govrin | 22 December 1964 | 12 January 1966 | 22412, 22413 | [V] K and W agree |

### Government 13

W: https://web.archive.org/web/20141117043018/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=13

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Levi Eshkol | 12 January 1966 | 26 February 1969 | 4054 | [V] K and W agree |
| prime_minister (interim) | Yigal Allon | 26 February 1969 | 17 March 1969 | 21297 | [V] K and W agree |
| deputy_prime_minister | Yigal Allon | 1 July 1968 | 17 March 1969 | 21295 | [V] K and W agree |
| defense_minister | Levi Eshkol | 12 January 1966 | 5 June 1967 | 4013 | [V] K and W agree |
| defense_minister | Moshe Dayan | 5 June 1967 | 17 March 1969 | 3977 | [V] K and W agree |
| finance_minister | Pinchas Sapir | 12 January 1966 | 5 August 1968 | 3872 | [V] K and W agree |
| finance_minister | Ze'ev Sherf | 5 August 1968 | 17 March 1969 | 3803 | [V] K and W agree |
| foreign_minister | Abba Eban | 12 January 1966 | 17 March 1969 | 4400 | [V] K and W agree |
| agriculture_minister | Haim Gvati | 12 January 1966 | 17 March 1969 | 4005 | [V] K and W agree |
| education_minister | Zalman Aran | 12 January 1966 | 17 March 1969 | 4557 | [V] K and W agree |
| health_minister | Yisrael Barzilai | 12 January 1966 | 17 March 1969 | 3995 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 12 January 1966 | 17 March 1969 | 3788 | [V] K and W agree |
| justice_minister | Ya'akov Shimshon Shapira | 12 January 1966 | 17 March 1969 | 3412 | [V] K and W agree |
| labour_minister | Yigal Allon | 12 January 1966 | 8 July 1968 | 4061 | [C] W: to 1 July 1968; K: 8 July 1968 (Almogi from 8 July in both) |
| labour_minister | Yosef Almogi | 8 July 1968 | 17 March 1969 | 22417 | [V] K and W agree |
| police_minister | Bechor-Shalom Sheetrit | 12 January 1966 | 2 January 1967 | 3824 | [V] K and W agree |
| police_minister | Eliyahu Sasson | 2 January 1967 | 17 March 1969 | 3772 | [V] K and W agree |
| posts_minister | Eliyahu Sasson | 12 January 1966 | 2 January 1967 | 3776 | [V] K and W agree |
| posts_minister | Yisrael Yeshayahu | 2 January 1967 | 17 March 1969 | 3914 | [V] K and W agree |
| religions_minister | Zorach Warhaftig | 12 January 1966 | 17 March 1969 | 3936 | [V] K and W agree |
| trade_minister | Haim Yosef Zadok | 12 January 1966 | 22 November 1966 | 3885 | [V] K and W agree |
| trade_minister | Ze'ev Sherf | 22 November 1966 | 17 March 1969 | 3805 | [V] K and W agree |
| transportation_minister | Moshe Carmel | 12 January 1966 | 17 March 1969 | 3917 | [V] K and W agree |
| welfare_minister | Yosef Burg | 12 January 1966 | 17 March 1969 | 4236 | [V] K and W agree |
| development_minister | Moshe Kol | 12 January 1966 | 17 March 1969 | 3838 | [V] K and W agree |
| absorption_minister (to add) | Yigal Allon | 1 July 1968 | 17 March 1969 | 21296 | [V] K and W agree |
| minister_without_portfolio | Yisrael Galili | 12 January 1966 | 24 October 1966 | 3972 | [C] W: to 5 June 1967; K: 24 October 1966 |
| minister_without_portfolio | Menachem Begin | 5 June 1967 | 17 March 1969 | 4067 | [V] K and W agree |
| minister_without_portfolio | Pinchas Sapir | 5 August 1968 | 17 March 1969 | 3869 | [V] K and W agree |
| [other] minister in the PM's office | Yosef Sapir | 5 June 1967 | 17 March 1969 | 3889 | [V] K and W agree |
| [other] housing | Mordechai Bentov | 12 January 1966 | 17 March 1969 | 3990 | [V] K and W agree |
| [other] tourism | Moshe Kol | 12 January 1966 | 17 March 1969 | 3834 | [V] K and W agree |
| [other] information | Yisrael Galili | 24 October 1966 | 17 March 1969 | 3976 | [C] W: Minister of Information from 5 June 1967 and without portfolio to that day; K: Information from 24 October 1966 |

### Government 14

W: https://web.archive.org/web/20141117043024/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=14

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Golda Meir | 17 March 1969 | 15 December 1969 | 3929, 22430 | [V] K and W agree |
| deputy_prime_minister | Yigal Allon | 17 March 1969 | 15 December 1969 | 21298, 21299 | [V] K and W agree |
| defense_minister | Moshe Dayan | 17 March 1969 | 15 December 1969 | 3978, 22446 | [V] K and W agree |
| finance_minister | Ze'ev Sherf | 17 March 1969 | 15 December 1969 | 3804, 22456 | [V] K and W agree |
| foreign_minister | Abba Eban | 17 March 1969 | 15 December 1969 | 21268, 21269 | [V] K and W agree |
| agriculture_minister | Haim Gvati | 17 March 1969 | 15 December 1969 | 4575, 22444 | [V] K and W agree |
| education_minister | Zalman Aran | 17 March 1969 | 15 December 1969 | 4558, 22439 | [V] K and W agree |
| health_minister | Yisrael Barzilai | 17 March 1969 | 15 December 1969 | 22421, 22443 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 17 March 1969 | 15 December 1969 | 22428, 22454 | [V] K and W agree |
| justice_minister | Ya'akov Shimshon Shapira | 17 March 1969 | 15 December 1969 | 22429, 22455 | [V] K and W agree |
| labour_minister | Yosef Almogi | 17 March 1969 | 15 December 1969 | 4039, 21343 | [V] K and W agree |
| police_minister | Eliyahu Sasson | 17 March 1969 | 15 December 1969 | 3549, 22458 | [V] K and W agree |
| posts_minister | Yisrael Yeshayahu | 17 March 1969 | 15 December 1969 | 3915, 22448 | [V] K and W agree |
| religions_minister | Zorach Warhaftig | 17 March 1969 | 15 December 1969 | 22423, 22447 | [V] K and W agree |
| trade_minister | Ze'ev Sherf | 17 March 1969 | 15 December 1969 | 3806, 22457 | [V] K and W agree |
| transportation_minister | Moshe Carmel | 17 March 1969 | 15 December 1969 | 22424, 22449 | [V] K and W agree |
| welfare_minister | Yosef Burg | 17 March 1969 | 15 December 1969 | 22419, 22441 | [V] K and W agree |
| development_minister | Moshe Kol | 17 March 1969 | 15 December 1969 | 22426, 22452 | [V] K and W agree |
| absorption_minister (to add) | Yigal Allon | 17 March 1969 | 15 December 1969 | 21300, 21301 | [V] K and W agree |
| minister_without_portfolio | Menachem Begin | 17 March 1969 | 17 November 1969 | 4440 | [C] W: Minister Without Portfolio for the whole government; K: without portfolio to 17 November 1969, then "שר במשרד ראש הממשלה" (next row) |
| [other] minister in the PM's office | Yisrael Galili | 17 March 1969 | 15 December 1969 | 22422, 22445 | [V] K and W agree |
| [other] minister in the PM's office | Yosef Sapir | 17 March 1969 | 15 December 1969 | 3890, 22450 | [V] K and W agree |
| [other] minister in the PM's office | Pinchas Sapir | 17 March 1969 | 15 December 1969 | 22425 | [C] K: row 22425 starts and ends on 17 March 1969, row 22451 runs 17 November to 15 December 1969; W: Minister Without Portfolio 17 March to 15 December 1969. Use W's span |
| [other] minister in the PM's office | Menachem Begin | 17 November 1969 | 15 December 1969 | 22440 | [C] see the row above |
| [other] minister in the PM's office | Pinchas Sapir | 17 November 1969 | 15 December 1969 | 22451 | [C] see the row above |
| [other] housing | Mordechai Bentov | 17 March 1969 | 15 December 1969 | 22420, 22442 | [V] K and W agree |
| [other] tourism | Moshe Kol | 17 March 1969 | 15 December 1969 | 22427, 22453 | [V] K and W agree |

### Government 15

W: https://web.archive.org/web/20141117043028/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=15

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Golda Meir | 15 December 1969 | 10 March 1974 | 3930, 22544 | [V] K and W agree |
| deputy_prime_minister | Yigal Allon | 15 December 1969 | 10 March 1974 | 4082, 21302 | [V] K and W agree |
| defense_minister | Moshe Dayan | 15 December 1969 | 10 March 1974 | 3979, 22563 | [V] K and W agree |
| finance_minister | Pinchas Sapir | 15 December 1969 | 10 March 1974 | 3873, 22567 | [V] K and W agree |
| foreign_minister | Abba Eban | 15 December 1969 | 10 March 1974 | 4401, 21270 | [V] K and W agree |
| agriculture_minister | Haim Gvati | 15 December 1969 | 10 March 1974 | 4576, 22560 | [V] K and W agree |
| education_minister | Yigal Allon | 15 December 1969 | 10 March 1974 | 4080, 21303 | [V] K and W agree |
| health_minister | Golda Meir | 15 December 1969 | 22 December 1969 | 22545 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| health_minister | Haim Gvati | 22 December 1969 | 27 July 1970 | 4007 | [V] K and W agree |
| health_minister | Victor Shem-Tov | 27 July 1970 | 10 March 1974 | 4336, 22573 | [V] K and W agree |
| interior_minister | Haim-Moshe Shapira | 15 December 1969 | 16 July 1970 | 4594 | [V] K and W agree |
| interior_minister | Golda Meir | 16 July 1970 | 1 September 1970 | 22547 | [V] K: "ממלאת מקום שר הפנים" (acting Minister of Interior); W: no row; the PM held the vacated office (W's note, §1) |
| interior_minister | Yosef Burg | 1 September 1970 | 10 March 1974 | 4565, 22558 | [V] K and W agree |
| justice_minister | Ya'akov Shimshon Shapira | 15 December 1969 | 13 June 1972 | 3827 | [V] K and W agree |
| justice_minister | Golda Meir | 13 June 1972 | 12 September 1972 | 22784 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| justice_minister | Ya'akov Shimshon Shapira | 12 September 1972 | 10 March 1974 | 22574, 22575 | [C] W: 12 September 1972 to 1 November 1973; K: to 10 March 1974. Neither source names a holder for 1 November 1973 to 10 March 1974 on W's dates |
| labour_minister | Yosef Almogi | 15 December 1969 | 10 March 1974 | 4040, 22557 | [V] K and W agree |
| police_minister | Shlomo Hillel | 15 December 1969 | 10 March 1974 | 4292, 22564 | [V] K and W agree |
| posts_minister | Elimelekh Rimalt | 15 December 1969 | 6 August 1970 | 4140 | [V] K and W agree |
| posts_minister | Golda Meir | 6 August 1970 | 1 September 1970 | 22788 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| posts_minister | Shimon Peres | 1 September 1970 | 10 March 1974 | 3595, 22569, 22570 | [V] K and W agree |
| religions_minister | Zorach Warhaftig | 15 December 1969 | 10 March 1974 | 3937, 22565 | [V] K and W agree |
| trade_minister | Yosef Sapir | 15 December 1969 | 6 August 1970 | 3868 | [V] K and W agree |
| trade_minister | Golda Meir | 6 August 1970 | 1 September 1970 | 22786 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| trade_minister | Pinchas Sapir | 1 September 1970 | 6 March 1972 | 3877 | [C] W: to 5 March 1972; K: 6 March 1972 |
| trade_minister | Haim Bar-Lev | 6 March 1972 | 10 March 1974 | 4287, 22559 | [C] W: from 5 March 1972; K: 6 March 1972 |
| transportation_minister | Ezer Weizman | 15 December 1969 | 6 August 1970 | 4169 | [V] K and W agree |
| transportation_minister | Golda Meir | 6 August 1970 | 1 September 1970 | 22785 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| transportation_minister | Shimon Peres | 1 September 1970 | 10 March 1974 | 4277, 22571 | [V] K and W agree |
| welfare_minister | Yosef Burg | 15 December 1969 | 1 September 1970 | 4237 | [V] K and W agree |
| welfare_minister | Michael Hasani | 1 September 1970 | 10 March 1974 | 3940, 22566 | [V] W spells him Chasani |
| development_minister | Haim Landau | 15 December 1969 | 6 August 1970 | 3928 | [V] K and W agree |
| development_minister | Golda Meir | 6 August 1970 | 1 September 1970 | 22787 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| development_minister | Haim Gvati | 1 September 1970 | 10 March 1974 | 4008, 22561 | [V] K and W agree |
| absorption_minister (to add) | Golda Meir | 15 December 1969 | 22 December 1969 | 22546 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| absorption_minister (to add) | Shimon Peres | 22 December 1969 | 27 July 1970 | 4320 | [V] K and W agree |
| absorption_minister (to add) | Natan Peled | 27 July 1970 | 10 March 1974 | 3880, 22568 | [V] K and W agree |
| minister_without_portfolio | Menachem Begin | 15 December 1969 | 6 August 1970 | 4068 | [V] K and W agree |
| minister_without_portfolio | Yisrael Barzilai | 15 December 1969 | 12 June 1970 | 3991 | [V] K and W agree |
| minister_without_portfolio | Yisrael Galili | 15 December 1969 | 10 March 1974 | 3973, 22562 | [V] K and W agree |
| minister_without_portfolio | Victor Shem-Tov | 15 December 1969 | 27 July 1970 | 4335 | [V] K and W agree |
| minister_without_portfolio | Shimon Peres | 15 December 1969 | 22 December 1969 | 4319 | [V] K and W agree |
| [other] minister in the PM's office | Arieh Dulzin | 15 December 1969 | 6 August 1970 | 3755 | [C] W: Aryeh Dolchin, Minister Without Portfolio; K: "שר במשרד ראש הממשלה" |
| [other] housing | Ze'ev Sherf | 15 December 1969 | 10 March 1974 | 3612, 22576 | [V] K and W agree |
| [other] tourism | Moshe Kol | 15 December 1969 | 10 March 1974 | 3835, 22572 | [V] K and W agree |

### Government 16

W: https://web.archive.org/web/20141117043033/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=16

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Golda Meir | 10 March 1974 | 3 June 1974 | 3570 | [V] K and W agree |
| deputy_prime_minister | Yigal Allon | 10 March 1974 | 3 June 1974 | 4083 | [V] K and W agree |
| defense_minister | Moshe Dayan | 10 March 1974 | 3 June 1974 | 3980 | [V] K and W agree |
| finance_minister | Pinchas Sapir | 10 March 1974 | 3 June 1974 | 3594 | [V] K and W agree |
| foreign_minister | Abba Eban | 10 March 1974 | 3 June 1974 | 4402 | [V] K and W agree |
| agriculture_minister | Haim Gvati | 10 March 1974 | 3 June 1974 | 4006 | [V] K and W agree |
| education_minister | Yigal Allon | 10 March 1974 | 3 June 1974 | 4081 | [V] K and W agree |
| health_minister | Victor Shem-Tov | 10 March 1974 | 3 June 1974 | 4337 | [V] K and W agree |
| interior_minister | Yosef Burg | 10 March 1974 | 3 June 1974 | 4566 | [V] K and W agree |
| justice_minister | Haim Yosef Zadok | 10 March 1974 | 3 June 1974 | 3882 | [V] K and W agree |
| labour_minister | Yitzhak Rabin | 10 March 1974 | 3 June 1974 | 3452 | [V] K and W agree |
| police_minister | Shlomo Hillel | 10 March 1974 | 3 June 1974 | 4293 | [V] K and W agree |
| posts_minister (Communications) | Aharon Uzan | 10 March 1974 | 3 June 1974 | 4200 | [V] K and W agree |
| religions_minister | Yitzhak Rafael | 10 March 1974 | 3 June 1974 | 3863 | [V] W spells him Raphael |
| trade_minister | Haim Bar-Lev | 10 March 1974 | 3 June 1974 | 4288 | [V] K and W agree |
| transportation_minister | Aharon Yariv | 10 March 1974 | 3 June 1974 | 3912 | [V] K and W agree |
| welfare_minister | Michael Hasani | 10 March 1974 | 3 June 1974 | 3941 | [C] W (Chasani): to 4 April 1974; K: to 3 June 1974 |
| development_minister | Haim Bar-Lev | 10 March 1974 | 1 April 1974 | 4290 | [C] W: to 3 June 1974; K: 1 April 1974 |
| absorption_minister (to add) | Shlomo Rosen | 10 March 1974 | 3 June 1974 | 3856 | [V] K and W agree |
| minister_without_portfolio | Yisrael Galili | 10 March 1974 | 3 June 1974 | 3974 | [V] K and W agree |
| minister_without_portfolio | Gideon Hausner | 10 March 1974 | 3 June 1974 | 3964 | [V] K and W agree |
| [other] housing | Yehoshua Rabinovitz | 10 March 1974 | 3 June 1974 | 3847 | [V] W spells him Rabinowitz |
| [other] tourism | Moshe Kol | 10 March 1974 | 3 June 1974 | 3836 | [V] K and W agree |
| [other] information | Shimon Peres | 10 March 1974 | 1 April 1974 | 4280 | [C] see the row above |
| [other] information | Shimon Peres | 1 April 1974 | 3 June 1974 | 3596 | [C] W: one row, Minister of Information 10 March to 3 June 1974; K: two rows, in the Prime Minister's Office to 1 April, then the Ministry of Information |

### Government 17

W: https://web.archive.org/web/20141117043040/http://knesset.gov.il:80/govt/eng/GovtByNumber_eng.asp?govt=17

| Office id | Holder | From | To | Knesset row(s) | Mark |
|---|---|---|---|---|---|
| prime_minister | Yitzhak Rabin | 3 June 1974 | 20 June 1977 | 3453, 22727 | [V] K and W agree |
| deputy_prime_minister | Yigal Allon | 3 June 1974 | 20 June 1977 | 21305, 21306 | [V] K and W agree |
| defense_minister | Shimon Peres | 3 June 1974 | 20 June 1977 | 4300, 22724 | [V] K and W agree |
| finance_minister | Yehoshua Rabinovitz | 3 June 1974 | 20 June 1977 | 3846, 22728 | [V] W spells him Rabinowitz |
| foreign_minister | Yigal Allon | 3 June 1974 | 20 June 1977 | 4079, 21307 | [V] K and W agree |
| agriculture_minister | Aharon Uzan | 3 June 1974 | 20 June 1977 | 3407, 21279 | [V] K and W agree |
| education_minister | Aharon Yadlin | 3 June 1974 | 20 June 1977 | 3945, 22721 | [V] K and W agree |
| health_minister | Victor Shem-Tov | 3 June 1974 | 20 June 1977 | 4338, 22730 | [V] K and W agree |
| interior_minister | Shlomo Hillel | 3 June 1974 | 30 October 1974 | 4295 | [C] W: to 29 October 1974; K: 30 October 1974 |
| interior_minister | Yosef Burg | 30 October 1974 | 20 December 1976 | 4231 | [C] W: 29 October 1974 to 22 December 1976; K: 30 October 1974 to 20 December 1976 |
| interior_minister | Yitzhak Rabin | 20 December 1976 | 18 January 1977 | 22782 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| interior_minister | Shlomo Hillel | 18 January 1977 | 20 June 1977 | 22738, 22739 | [C] W: from 16 January 1977; K: 18 January 1977 |
| justice_minister | Haim Yosef Zadok | 3 June 1974 | 20 June 1977 | 3883, 22725 | [V] K and W agree |
| labour_minister | Moshe Baram | 3 June 1974 | 20 June 1977 | 4003, 22717 | [V] K and W agree |
| police_minister | Shlomo Hillel | 3 June 1974 | 20 June 1977 | 4294, 22720 | [V] K and W agree |
| posts_minister (Communications) | Yitzhak Rabin | 3 June 1974 | 20 March 1975 | 4157 | [V] K and W agree |
| posts_minister (Communications) | Aharon Uzan | 20 March 1975 | 20 June 1977 | 4201, 22715 | [V] K and W agree |
| religions_minister | Haim Yosef Zadok | 3 June 1974 | 30 October 1974 | 3887 | [C] W: to 29 October 1974; K: 30 October 1974 |
| religions_minister | Yitzhak Rafael | 30 October 1974 | 20 December 1976 | 22731 | [C] W (Raphael): 30 October 1974 to 22 December 1976; K: to 20 December 1976 |
| religions_minister | Yitzhak Rabin | 20 December 1976 | 18 January 1977 | 22780 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| religions_minister | Haim Yosef Zadok | 18 January 1977 | 20 June 1977 | 22734, 22735 | [C] W: from 16 January 1977; K: 18 January 1977 |
| trade_minister | Haim Bar-Lev | 3 June 1974 | 20 June 1977 | 4289, 22716 | [V] K and W agree |
| transportation_minister | Gad Yaacobi | 3 June 1974 | 20 June 1977 | 4262, 22722 | [V] K and W agree |
| welfare_minister | Victor Shem-Tov | 3 June 1974 | 30 October 1974 | 4339 | [C] W: to 29 October 1974; K: 30 October 1974 |
| welfare_minister | Michael Hasani | 30 October 1974 | 2 July 1975 | 3942 | [C] W (Chasani): 30 October 1974 to 2 July 1975, "(Died)"; K: from 30 October 1974 |
| welfare_minister | Yitzhak Rabin | 2 July 1975 | 29 July 1975 | 22779 | [C] W: Rabin from 7 July 1975; K: 2 July 1975 |
| welfare_minister | Yosef Burg | 29 July 1975 | 4 November 1975 | 4238 | [V] K and W agree |
| welfare_minister | Zevulun Hammer | 4 November 1975 | 20 December 1976 | 4322 | [C] W: to 22 December 1976; K: 20 December 1976 |
| welfare_minister | Yitzhak Rabin | 20 December 1976 | 18 January 1977 | 22781 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| welfare_minister | Moshe Baram | 18 January 1977 | 20 June 1977 | 22736, 22737 | [C] W: from 16 January 1977; K: 18 January 1977 |
| absorption_minister (to add) | Shlomo Rosen | 3 June 1974 | 20 June 1977 | 3857, 22729 | [V] K and W agree |
| minister_without_portfolio | Shulamit Aloni | 3 June 1974 | 5 November 1974 | 4392 | [C] W: to 6 November 1974; K: 5 November 1974 |
| minister_without_portfolio | Yisrael Galili | 3 June 1974 | 20 June 1977 | 3975, 22718 | [V] K and W agree |
| minister_without_portfolio | Gideon Hausner | 3 June 1974 | 20 June 1977 | 3965, 22719 | [V] K and W agree |
| [other] housing | Avraham Ofer | 3 June 1974 | 3 January 1977 | 3365 | [V] K and W agree |
| [other] housing | Yitzhak Rabin | 3 January 1977 | 18 January 1977 | 22783 | [V] W: no row; the PM held the vacated office (W's note, §1) |
| [other] housing | Shlomo Rosen | 18 January 1977 | 20 June 1977 | 22740, 22741 | [C] W: from 16 January 1977; K: 18 January 1977 |
| [other] tourism | Moshe Kol | 3 June 1974 | 20 June 1977 | 3837, 22726 | [V] K and W agree |
| [other] information | Aharon Yariv | 3 June 1974 | 4 February 1975 | 3913 | [V] K and W agree |

## 3. Where the sources disagree

Every [C] in §1 and §2 in one list, then the design's §2.1 rows against K. "K" is the position table, "W" the
government pages.

K against W:

1. Agriculture, end of the 8th government: K Luz to 30 November 1959, Ben-Gurion to 17 December, and Luz without
   portfolio 30 November to 17 December (row 22304); W Luz at Agriculture to 17 December and nothing else.
2. Welfare and without portfolio, January 1959: K Naftali moves on 26 January 1959; W 25 January.
3. Religions, end of the 8th government: K Toledano to 17 December 1959; W to 30 November 1959.
4. Posts, July 1960: K Sheetrit 5 to 18 July and Mintz from 18 July 1960; W Mintz from 17 July and no Sheetrit row.
5. Development and Housing, October 1962: K Almogi from 29 October; W 30 October 1962.
6. Govrin, 11th government: K "שר במשרד ראש הממשלה" (minister in the Prime Minister's Office) from 2 December 1963;
   W "Minister Without Portfolio" from 1 December 1963.
7. Agriculture, November 1964: K Dayan to 9 November; W to 4 November (Gvati from 9 November in both).
8. Transportation, May 1965: K Carmel from 31 May; W 30 May 1965; JTA (24 Oct) "The Cabinet yesterday decided" under a 31 May dateline, so 30 May, with W.
9. Housing and Development, May 1965: K Almogi to 25 May at both, Zadok at Development from 25 May, Eshkol at
   Housing from 31 May; W Almogi to 25 May at Development and to 23 May at Housing, Zadok at Development from 31 May.
   (W's Zadok row at Trade starts 25 May, K's too.)
10. Labour, July 1968: K Allon to 8 July; W to 1 July (Almogi from 8 July in both). On W's dates Labour is empty for
    a week.
11. Information, 13th government: K Galili at the Ministry of Information from 24 October 1966; W Galili without
    portfolio to 5 June 1967 and Minister of Information from that day.
12. Begin, 14th government: K without portfolio to 17 November 1969, then "שר במשרד ראש הממשלה"; W without portfolio
    throughout.
13. Pinchas Sapir, 14th government: K row 22425 starts and ends on 17 March 1969 (a zero-length row) and row 22451
    runs 17 November to 15 December 1969; W "Minister Without Portfolio" 17 March to 15 December 1969. Our reading:
    row 22425 is a data-entry error, and W's span is the one to use.
14. Trade, March 1972: K Bar-Lev from 6 March; W 5 March 1972.
15. Justice, 15th government: K Y. S. Shapira 12 September 1972 to 10 March 1974; W to 1 November 1973. Neither
    source names anyone else at Justice for 1 November 1973 to 10 March 1974.
16. Dulzin, 15th government: K "שר במשרד ראש הממשלה"; W "Minister Without Portfolio".
17. Development, 16th government: K Bar-Lev to 1 April 1974; W to 3 June 1974.
18. Welfare, 16th government: K Hasani to 3 June 1974; W to 4 April 1974.
19. Information, 16th government: K Peres in the Prime Minister's Office to 1 April 1974, then at the Ministry of
    Information; W one row, Minister of Information from 10 March.
20. Aloni without portfolio: K to 5 November 1974; W 6 November 1974.
21. Interior, Religions, Welfare, October 1974: K changes on 30 October 1974 (Hillel to Burg, Zadok to Rafael, Shem-Tov
    to Hasani); W ends Hillel, Zadok and Shem-Tov on 29 October, starts Burg on 29 October and Rafael and Hasani on
    30 October.
22. Welfare, July 1975: Hasani died on 2 July 1975 (W "(Died)"); K Rabin from 2 July; W Rabin from 7 July.
23. The 1976 crisis: K ends Burg, Rafael and Hammer on 20 December 1976 and starts Hillel, Zadok, Baram and Rosen on
    18 January 1977; W 22 December 1976 and 16 January 1977.

The design's §2.1 rows against K (the design cites en.wiki pages, so these are en.wiki against the Knesset):

24. Rabin's end and the 17th government: design "21 Jun 1977" (en.wiki list of Prime Ministers); K and W 20 June
    1977.
25. Foreign Affairs: design Meir from 18 June 1956, Eban 13 January 1966 to 2 June 1974, Allon 3 June 1974 to 19 June
    1977; K Meir from 19 June 1956 (row 22188), Eban 12 January 1966 to 3 June 1974, Allon to 20 June 1977. W agrees
    with K. On the design's dates Foreign Affairs is empty on 12 January 1966 and 2 June 1974; on K's it is not.
26. Defence: design "Eshkol to June 1967"; K and W 5 June 1967 (see §5 for Dayan's start).
27. Chief of Staff, Elazar: design and both wikis 1 January 1972; EJ *Elazar, David*: "in November 1971, chief of
    staff". Our reading: November 1971 is the appointment and 1 January 1972 the start of the term.

Of these, 10, 15, 22 and 25 touch a vacancy: on W's dates Labour (1 to 8 July 1968) and Justice (1 November 1973 to
10 March 1974) are empty and Welfare is empty for 2 to 7 July 1975; on the design's dates Foreign Affairs is. On K's
dates none of these offices is empty. The vacancies both sources show are in §1 (Religions 15 to 24 October 1960,
Posts from 30 May 1961, Development and Housing from 23 August 1962, Transportation from 4 May 1965, Housing 25 to 31
May 1965).

## 4. Non-ministerial offices

| Office id | Holder | From | To | Source | Mark |
|---|---|---|---|---|---|
| president | Yitzhak Ben-Zvi | 16 December 1952 | 23 April 1963 (died in office) | MFA page: "Yitzhak Ben-Zvi (1952-1963)"; en.wiki list: terms from 16 December 1952, 30 October 1957 and 30 October 1962, to 23 April 1963 | [V] years; days [S] |
| president (acting) | Kadish Luz, Speaker | 23 April 1963 | 21 May 1963 | en.wiki list, note 2: "Upon Ben-Zvi's death on 23 April 1963, Knesset Speaker Kadish Luz took over as Acting President of Israel until the appointment of Zalman Shazar."; he.wiki *נשיא מדינת ישראל*: he served "until Zalman Shazar was sworn in, on 21 May" ("הוא שימש בתפקיד זה עד השבעתו של זלמן שזר, ב-21 במאי") | [S] |
| president | Zalman Shazar | 21 May 1963 | 24 May 1973 | MFA page: "Zalman Shazar (1963-1973)"; en.wiki list: 21 May 1963 to 26 March 1968, then 26 March 1968 to 24 May 1973; he.wiki timeline 21/05/1963 to 24/05/1973 | [V] years; days [S] |
| president | Ephraim Katzir | 24 May 1973 | 29 May 1978 | MFA page: "Ephraim Katzir (1973-1978)"; en.wiki list and he.wiki timeline 24 May 1973; the end is [V] in offices-1977-93.md §4 (plenum of 29 May 1978) | [V] years and end; start [S] |
| chief_of_staff | Moshe Dayan | 6 December 1953 | 29 January 1958 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Haim Laskov | 29 January 1958 | 1 January 1961 | en.wiki and he.wiki agree; EJ *Laskov*: "in 1958, fifth chief of general staff. In 1961 he retired from the army" | [V] years; days [S] |
| chief_of_staff | Tzvi Tzur | 1 January 1961 | 1 January 1964 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Yitzhak Rabin | 1 January 1964 | 1 January 1968 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | Haim Bar-Lev | 1 January 1968 | 1 January 1972 | en.wiki and he.wiki agree | [S] |
| chief_of_staff | David Elazar | 1 January 1972 | 3 April 1974 | en.wiki and he.wiki agree; EJ *Elazar*: "in November 1971, chief of staff" (§3, item 27) | [C] |
| chief_of_staff (acting) | Yitzhak Hofi | 3 April 1974 | 16 April 1974 | en.wiki (acting = y) and he.wiki agree | [S] |
| chief_of_staff | Mordechai Gur | 16 April 1974 | 16 April 1978 | en.wiki and he.wiki agree (and offices-1977-93.md §4) | [S] |
| intelligence_chief (Aman) | Yehoshafat Harkabi | 1955 (he.wiki May 1955) | 1959 (he.wiki April 1959) | en.wiki 1955-1959; he.wiki "מאי 1955 - אפריל 1959" | [S] |
| intelligence_chief (Aman) | Chaim Herzog | 1959 (he.wiki April 1959) | 1962 (he.wiki January 1962) | en.wiki 1959-1962 | [S] |
| intelligence_chief (Aman) | Meir Amit | 1962 (he.wiki January 1962) | 1963 (en.wiki) / January 1964 (he.wiki) | | [C] [S]; he.wiki's own Mossad list starts Amit at the Mossad on 26 March 1963, our reading: he.wiki has him in both posts for nine months |
| intelligence_chief (Aman) | Aharon Yariv | 1964 (he.wiki January 1964) | 1972 (he.wiki September 1972) | en.wiki 1964-1972 | [S] |
| intelligence_chief (Aman) | Eli Zeira | 15 October 1972 (Maariv, 24 Oct) / 1 October 1972 (Jewish Chronicle, 24 Oct) / October 1972 (en.wiki, he.wiki biography) / September 1972 (he.wiki Aman list) | April 1974 (he.wiki Aman list); between 1 and 3 April 1974 (our reading from the Knesset lexicon and JTA, 24 Oct) | §5 | [C] [S]; start day [C] between two press sources, end day [GAP] |
| intelligence_chief (Aman) | Shlomo Gazit | 1974 (he.wiki April 1974) | 1978 (en.wiki) / February 1979 (he.wiki) | | [C] [S] (as in offices-1977-93.md §4) |
| mossad_director | Isser Harel | 22 September 1952 (he.wiki) | 25 March 1963 (he.wiki) | en.wiki 1953-1963 | [C] [S] on the start |
| mossad_director | Meir Amit | 26 March 1963 (he.wiki) | 1 September 1968 (he.wiki) | en.wiki 1963-1968 | [S] |
| mossad_director | Zvi Zamir | 1 September 1968 (he.wiki) | 1 September 1974 (he.wiki) / 1973 (en.wiki) | | [C] [S] |
| mossad_director | Yitzhak Hofi | 1 September 1974 (he.wiki) / 1973 (en.wiki) | 12 September 1982 (he.wiki) | | [C] [S] (as in offices-1977-93.md §4) |
| speaker | Yosef Sprinzak | 15 August 1955 | 28 January 1959 | K row 3358 | [V] |
| speaker | Nahum Nir | 2 March 1959 | 30 November 1959 | K row 3361 | [V]; no Speaker 28 January to 2 March 1959 in K |
| speaker | Kadish Luz | 30 November 1959 | 17 November 1969 | K rows 1274, 1275, 1276 | [V] |
| speaker | Reuven Barkat | 17 November 1969 | 4 April 1972 | K row 1277 | [C] K ends the row on 4 April 1972; Wikidata gives his death as 5 April 1972 |
| speaker | Yisrael Yeshayahu | 9 May 1972 | 13 June 1977 | K rows 1137, 1138 | [V]; no Speaker 4 April to 9 May 1972 in K |
| speaker | Yitzhak Shamir | 13 June 1977 | 12 March 1980 (after this era) | K row 831 | [V] |

The Speaker rows are given because the Speaker acted as President in 1963 and `offices.yaml` has no Speaker office
yet (§7). The Aman and Mossad dates stay unverified until an IDF or Mossad page, or a contemporary report, is read;
the Chief of Staff days likewise, as the Knesset table does not cover the post.

## 5. The design's *to verify* marks

- **Dayan's start as Minister of Defence.** K row 3977 and W both give 5 June 1967 ("Moshe Dayan 05/06/1967 -
  17/03/1969 Rafi"), and K ends Eshkol at Defence the same day (row 4013); en.wiki *Thirteenth government of Israel*
  agrees ("Moshe Dayan (5 June 1967 - 17 March 1969)"). he.wiki *משה דיין* gives 2 June 1967 in its infobox ("התחלת
  כהונה = 2 ביוני 1967") and in its text says the appointment went to Dayan on 1 June 1967 ("והמינוי נמסר לדיין,
  ב-1 ביוני 1967"). The design's three values are all in the sources. **Value: 5 June 1967** [V] for the term; 1
  June 1967 [S] as the day the appointment was decided. Our reading: K and W date terms from when the Knesset
  recorded the minister in office, which for these additions is the first day of the war; the 1-5 June difference is
  between decision and office, not an error in either.
- **Begin's government.** K ends every 17th-government row on 20 June 1977 and W's 18th-government page starts on
  "20/06/1977"; offices-1977-93.md §1 fixes the confidence vote in the plenum record of 20 June 1977. **Value: 20 June
  1977** [V]. en.wiki's 21 June (design §2.1) is the next day; see offices-1977-93.md §3, item 1, on the sitting that
  ran past midnight.
- **The unity government's date.** K and W start Begin (without portfolio, row 4067) and Yosef Sapir (row 3889) of
  Gahal, and Dayan of Rafi, on 5 June 1967, and W gives Galili's move to Information the same day. en.wiki *Thirteenth
  government of Israel*: "On 5 June 1967, the first day of the Six-Day War, Gahal and Rafi joined the government". The
  design's 1 June (from en.wiki *Six-Day War*) is the day he.wiki gives for Dayan's appointment above; the 1 June for
  the whole unity government is in neither Knesset source. **Value: 5 June 1967** [V].
- **Zeira's days.** No primary source read gives a day. en.wiki *Eli Zeira*: "In October 1972, Zeira was appointed as
  the director of Aman."; he.wiki *אלי זעירא*: "In September 1972 he returned to Israel, and in October was appointed
  head of Aman" ("בספטמבר 1972 שב ארצה, ובאוקטובר התמנה לראש אמ"ן", citing Yossi Goldstein, *Golda: Biography*, p.
  550); he.wiki *אגף המודיעין* list: "ספטמבר 1972 - אפריל 1974" (September 1972 to April 1974). en.wiki's list on
  *Military Intelligence Directorate (Israel)*: 1972-1974. **Value: October 1972 to April 1974** [C] [S], days [GAP].
  The design's own anchor for the end, the Agranat interim report of 1 April 1974 (design §2.1, en.wiki *Agranat
  Commission*), was not checked in this note. Our reading: a contemporary newspaper report (Davar or Maariv on JPress,
  or the JTA archive) is the likely place to find both days; none was read here.
  (24 Oct) **Start.** Maariv, 1 October 2022 (Ran Edelist): "ב־15 באוקטובר 1972 החליף אלי זעירא את אהרון יריב כראש
  אמ"ן וכמעריך הלאומי" ("on 15 October 1972 Eli Zeira replaced Aharon Yariv as head of Aman and as the national
  assessor") [S]; a newspaper article; the sentence cites no source for the day. It agrees with the October of en.wiki and the
  he.wiki biography, not with the September of the he.wiki Aman list. JTA's archive search returned no 1972 bulletin on
  the appointment. The Jewish Chronicle's obituary of Zeira
  (`https://www.thejc.com/news/obituaries/obituary-major-general-eli-zeira-dv7dccma`, read 24 October 2026): "On
  October 1, 1972 he was appointed IDF Intelligence Chief." [S]. Two retrospective press sources, two different days.
  **Value: 15 October 1972 (Maariv) [C] 1 October 1972 (Jewish Chronicle) [C] September 1972 (he.wiki Aman list)**,
  all [S]; the day is unsettled and needs a contemporary or IDF source.
  **End.** The Knesset's lexicon [V]: "Its Intermediate Report was published on April 1st 1974 ... Its other
  recommendations included the dismissal of Eliahu Zeira, Head of the Intelligence Directorate". JTA 4 April 1974:
  "Maj. Gen. Shlomo Gazit ... was appointed chief of the army's intelligence branch, replacing Gen. Eliahu Zeira who
  also resigned in the aftermath of the Agranat report" (the item is datelined "TEL AVIV, April 3. (JTA)" and says Dayan named Hofi acting Chief of
  Staff "last night", that is the night of 2 April; this note's §4 has 3 April for Hofi from en.wiki and he.wiki). The
  3 April bulletin's Agranat item is datelined "JERUSALEM, April 2. (JTA)" and says the report was "released late
  tonight" and Elazar "resigned late tonight", so JTA puts the release on the night of 2 April where the Knesset's
  lexicon says 1 April [C]. **Value, our reading: Zeira's term ended between 1 and 3 April 1974; the day is [GAP]** (neither a resignation letter nor a Gazit appointment date in a primary source was
  read). The design's 1 April anchor is the day of the report, not the day he left.
- **Presidents' days** (design §2.1: "years only, days *to verify*"): §4. Ben-Zvi 16 December 1952 to 23 April 1963,
  Shazar 21 May 1963 to 24 May 1973, Katzir from 24 May 1973; the MFA page confirms the years [V], the days are
  [S].
- **`living`**: §8. All 79 people in this note have a death date in Wikidata, so every one is `living: false` [S].

## 6. Coalition factions (W)

W gives each minister's faction, defined on each page as "the Knesset parliamentary group in which the minister was a
member on the date of his/her entry into the government". The factions W names, per government, with W's own
spelling [V]:

| Government | Factions W names |
|---|---|
| 7th | Worker's Party of Eretz Yisrael; United Workers` Party; Ahdut Ha`avoda - Poalei Zion; Progressive Party; Poel Mizrahi - Mizrahi |
| 8th | Worker's Party of Eretz Yisrael; United Workers` Party; Ahdut Ha`avoda - Poalei Zion; Progressive Party; National Religious Party (to 1 July 1958, §1) |
| 9th | Worker's Party of Eretz Yisrael; United Workers` Party; Ahdut Ha`avoda - Poalei Zion; Progressive Party; National Religious Party; Religious Torah Front (Mintz) |
| 10th | Worker's Party of Eretz Yisrael; Ahdut Ha`avoda - Poalei Zion; National Religious Party |
| 11th | Worker's Party of Eretz Yisrael; Ahdut Ha`avoda - Poalei Zion; National Religious Party |
| 12th | Worker's Party of Eretz Yisrael; Ahdut Ha`avoda - Poalei Zion; National Religious Party |
| 13th | Alignment; Israel Labor Party (ministers entering from 1968); National Religious Party; Rafi and Herut-Liberal Bloc (from 5 June 1967) |
| 14th | Alignment; National Religious Party; Herut-Liberal Bloc |
| 15th | Alignment; National Religious Party; Herut-Liberal Bloc (to 6 August 1970); Independent Liberals |
| 16th | Alignment; National Religious Party; Independent Liberals |
| 17th | Alignment; National Religious Party (to 20 December 1976 in K, 22 December in W); Civil Rights Movement (Aloni, to 5 November 1974 in K) |
| 18th | Likud; Shlomtzion; Democratic Movement for Change; National Religious Party; Single MK - Moshe Dayan |

W leaves the faction blank on some rows (for example Dov Yosef and Eliyahu Sasson in the 10th to 12th governments,
Moshe Kol in the 13th and 14th, Haim Gvati, Victor Shem-Tov, Yehoshua Rabinowitz and Haim Bar-Lev in the 16th). W's
legend has a "Not a Knesset Member" mark that does not survive in the archived text, so a blank is not read here as
a party or as no party. [GAP] for those rows. Mapping W's faction names to party ids belongs to 1957-77 item 2
(Knessets and parties).

**Second pass (24 Oct): what the blank cells are.** Every blank row on W's pages for governments 7 to 17 was listed
(52 rows, 48 distinct person-and-entry-dates, 21 people; W's own spelling of names below). For each, K's faction-member rows
(position type 54, `FactionName`, `StartDate`, `FinishDate`) were searched for a row covering the entry date. **None
of the 52 has one**: where W shows no faction, K shows no Knesset faction membership on that day. Two independent
tables agree, so our reading is that a blank is W's "Not a Knesset Member", not a missing party. It is **not a party
and not "no party" either**: the party of these people on the day they entered is not given by W or K, and stays
[GAP] as a stated party. K's faction rows nearest each person are given for context only (gloss of the Hebrew faction
names is ours; first dates are the rows' `StartDate`):

| Person (W) | W blank rows (government, entry date) | K faction rows of that person (merged) |
|---|---|---|
| Pinhas Sapir | 7th 3 Nov 1955; 8th 7 Jan 1958 | 1959-11-30 to 1965-11-22 Mapai; to 1968-01-23 Alignment; to 1969-01-28 Israel Labor Party; to 1975-08-12 Alignment |
| Moshe Carmel | 7th 3 Nov 1955; 8th 7 Jan 1958; 14th 17 Mar 1969 | 1958-06-09 to 1965-11-22 Ahdut Ha`avoda; to 1966-01-17 Alignment; 1969-11-17 to 1977-06-13 Alignment |
| Dov Yosef | 10th 2 Nov 1961; 11th 26 Jun 1963; 12th 22 Dec 1964 | 1949-02-14 to 1959-11-30 Mapai |
| Eliyahu Sasson | 10th, 11th, 12th (same dates as Yosef) | 1965-11-22 to 1968-01-23 Alignment; to 1969-01-28 Israel Labor Party; to 1974-01-21 Alignment |
| Haim Gvati | 11th 9 Nov 1964; 12th 22 Dec 1964; 14th 17 Mar 1969; 16th 10 Mar 1974 | 1965-11-22 to 1966-01-17 Alignment; 1969-11-17 to 1974-01-21 Alignment |
| Moshe Kol | 13th 12 Jan 1966; 14th 17 Mar 1969; 17th 3 Jun 1974 | Progressive Party 1948-49, 1951, 1959-11-30 to 1961-05-08; Liberal Party of Israel to 1965-03-16; Independent Liberals to 1966-01-11, 1969-11-17 to 1969-12-15, 1974-01-21 to 1974-03-10 |
| Israel Barzilai | 13th 12 Jan 1966; 14th 17 Mar 1969; 15th 15 Dec 1969 | 1955-08-15 to 1965-11-22 Mapam |
| Mordechai Bentov | 13th 12 Jan 1966; 14th 17 Mar 1969 | 1948-05-04 to 1965-11-22 Mapam |
| Yaakov-Shimshon Shapira | 13th 12 Jan 1966; 14th 17 Mar 1969 | 1951-08-20 to 1955-11-14 Mapai; 1969-11-17 to 1974-01-21 Alignment |
| Victor Shem-Tov | 15th 15 Dec 1969, 27 Jul 1970; 16th 10 Mar 1974; 17th 3 Jun 1974 | 1961-09-04 to 1969-01-28 Mapam; to 1969-11-17 Alignment; 1981-07-20 to 1984-10-22 Alignment; to 1988-03-15 Mapam |
| Natan Peled | 15th 27 Jul 1970 | 1965-11-22 to 1969-01-28 Mapam; to 1969-11-17 Alignment |
| Yosef Burg | 15th 1 Sep 1970 | NRP to 1969-12-15, then 1974-01-21 to 1981-07-20 NRP (the K gap, 15 Dec 1969 to 21 Jan 1974, matches W's blank; a person's check against the Knesset's own page for Burg is pending) |
| Shlomo Hillel | 15th 15 Dec 1969 | 1952-12-21 to 1959-07-06 Mapai; 1974-01-21 to 1991-10-07 Alignment |
| Haim Bar-Lev | 15th 5 Mar 1972; 16th 10 Mar 1974; 17th 3 Jun 1974 | 1977-06-13 to 1991-10-07 Alignment (no earlier row) |
| Ezer Weizman | 15th 15 Dec 1969 | 1977-06-13 to 1981-07-20 Likud (no earlier row) |
| Aharon Uzan | 16th 10 Mar 1974; 17th 3 Jun 1974, 20 Mar 1975 | 1965-11-22 to 1969-11-17 Alignment / Israel Labor Party; 1981-07-20 to 1984-08-13 Tami |
| Yehoshua Rabinowitz | 16th 10 Mar 1974; 17th 3 Jun 1974 | 1977-06-13 to 1979-08-14 Alignment (no earlier row) |
| Shlomo Rosen | 16th 10 Mar 1974; 17th 3 Jun 1974, 16 Jan 1977 | 1965-11-22 to 1969-01-28 Mapam; to 1974-01-21 Alignment |
| Gideon Hausner | 17th 3 Jun 1974 | 1965-11-22 to 1974-03-10 Independent Liberals; 1977-06-13 to 1981-07-20 Independent Liberals |
| Aryeh Dolchin (W: Dolchin) | 15th 15 Dec 1969 | no K faction row at all |
| Yaakov Moshe Toledano | 8th 3 Dec 1958; 9th 17 Dec 1959 | no K faction row at all |

The party cells therefore stay [GAP] as stated parties, but the reason is now known: these people were not Knesset
members on the entry date. One case shows the edge: Moshe Kol's last K row ends on 11 January 1966, the day before his
entry on 12 January 1966. Mapping W's faction names to party ids belongs to 1957-77 item 2 (Knessets and parties).

## 7. Mapping this note into content later, and what `content/` already says

No content is changed here. When the content part of #113 opens:

- **`governments.yaml`**: `seventh_government` exists (from 1955-11-03, no end date; its comment says it "served
  until 7 January 1958, after the content range"); §1 gives that end. A new record per later row of §1, ids in the
  existing style (`eighth_government` ... `seventeenth_government`); sources: the K query for that `GovernmentNum` and W's page. The 8th (1 July 1958), 15th
  (6 August 1970) and 17th (December 1976) need coalition changes; the 13th needs its 5 June 1967 entry and Allon's
  interim term.
- **`people.yaml`**: one term per row of §2, split per government as today, `sources` = the K row URL(s) and W's
  page. Where a row is [C], the K value with the W value in a YAML comment, as offices-1977-93.md proposes.
- **Office questions for the maintainer** (each needs a decision before encoding):
  - `absorption_minister` does not exist yet; K starts the Ministry of Immigrant Absorption (המשרד לקליטת עלייה) on 1
    July 1968 (row 21296, Allon). offices-1977-93.md §5 already expects #113 to add it from that day.
  - Housing (from 2 November 1961), Tourism (from 22 December 1964) and Information (24 October 1966 to 17 March 1969
    and 10 March 1974 to 4 February 1975) have no office id; they are "[other]" in §2. So is "שר במשרד ראש הממשלה"
    (minister in the Prime Minister's Office), which W calls Minister Without Portfolio; `minister_without_portfolio`
    may be the right home for it.
  - `development_minister` ends with Bar-Lev on 1 April 1974 in K (3 June 1974 in W); no 17th-government row.
  - `posts_minister`: K's title becomes "שר התקשורת" (Communications) on 3 January 1971 (§1); the title may want an
    era-dated rename, as offices-1977-93.md §5 also says.
  - `prime_minister`: Allon's interim term (K "ראש הממשלה בפועל", W "Interim Prime Minister") is either a
    `prime_minister` term with a note or a new historical-only title.
  - `deputy_prime_minister` has one holder at a time throughout 1957-1977 in K (Eban 1963-1966, Allon 1968-1977).
  - Chief of Staff, Aman and Mossad, and the Speaker: `offices.yaml` has `chief_of_staff` but no intelligence or
    Speaker office; the ids in §4 (`intelligence_chief`, `mossad_director`, `speaker`) are the same proposal as
    offices-1977-93.md §4.
- **Mismatches with `content/people.yaml` on main** (as of `3c8f3098`, unchanged at `af61f20d`), reported, not edited:
  - `ben_zvi` born 1884-12-06; Wikidata Q128894 gives 1884-11-24, en.wiki's list "24 November 1884". [C]. Our
    reading: 24 November (Julian) is 6 December (Gregorian), so both may be the same day in two calendars.
  - `shazar` born 1889-11-24, as en.wiki's list gives; Wikidata Q299100 gives 1889-12-22. [C], not explained by the
    calendar difference (12 days in 1889).
  - `golda_meir` born 1898-05-03; Wikidata Q42992 holds two values, 1898-04-21 and 1898-05-03 [C] (the same Julian
    and Gregorian day).
  - `naftali` born 1888-03-19; Wikidata Q72124 holds 1888-03-19 and 1888-03-29 [C].
  - Open terms: the 7th-government terms of `ben_gurion`, `shapira`, `rosen`, `bentov`, `sheetrit`, `golda_meir`,
    `eshkol`, `burg`, `naftali`, `aran`, `luz`, `pinchas_sapir`, `barzilai`, `bar_yehuda`, `carmel` and `namir` have
    no `to`; K and W end them on 7 January 1958 (§2). `ben_zvi` (president) and `dayan` (chief_of_staff) are open too;
    §4 gives 23 April 1963 and 29 January 1958.
  - Deputy-minister terms of `warhaftig`, `ben_meir`, `tzur`, `unna` and `assaf` are open; deputies are outside this
    note, but W's 7th-government page ends Tzur on 07/01/1958, Unna on 31/12/1957 and Warhaftig on 28/12/1957.

## 8. People and `living`

Every person who holds an office in §2 or §4. Born and died from Wikidata [S]; where an item holds two values, both
are given and marked [C]. Wikidata's year-only or month-only values are shown as the year or month. "none" in the
PersonID column: not in KNS_Person. "(exists)" marks an id already in `people.yaml`. New ids reuse the ones offices-1977-93.md
§7 proposes for the same people (`begin`, `peres`, `rabin`, `shamir`, `hammer`, `uzan`, `yaacobi`, `ezer_weizman`,
`mordechai_gur`, `hofi`, `gazit`, `chaim_herzog`, `amit`, `katzir`, `landau`, `bar_lev`, `aloni`), and
avoid the existing `tzur` (Ze'ev Tzur), `shapira` (Haim-Moshe Shapira) and `rosen` (Pinchas Rosen).

Every person here has a death date in Wikidata, so `living` is `false` for all 79 [S]; the existing rule in
`people.yaml` is to confirm each against the en.wiki infobox before setting it.

| Suggested id | Name (en.wiki title) | Role in this note | Knesset PersonID | Born | Died | living | Wikidata |
|---|---|---|---|---|---|---|---|
| allon | Yigal Allon | minister; interim Prime Minister 1969 | 2142 | 1918 / 1918-10-10 | 1980-02-29 | false | Q319015 |
| almogi | Yosef Almogi | minister | 2150 | 1910 / 1910-05-05 | 1991-11-02 | false | Q74696 |
| aloni | Shulamit Aloni | minister | 2143 | 1928-12-27 | 2014-01-24 | false | Q267335 |
| amit | Meir Amit | Aman; Mossad | 2527 | 1921-03-17 | 2009-07-17 | false | Q449095 |
| aran (exists) | Zalman Aran | minister | 2171 | 1899-03-01 | 1970-09-06 | false | Q145162 |
| bar_lev | Haim Bar-Lev | minister (Chief of Staff 1968-1972) | 2216 | 1924-11-16 | 1994-05-07 | false | Q467150 |
| bar_yehuda (exists) | Yisrael Bar-Yehuda | minister | 2215 | 1895-11-15 | 1965-05-04 | false | Q2079765 |
| baram | Moshe Baram | minister | 2224 | 1911-03-17 | 1986-12-05 | false | Q2908256 |
| barkat | Reuven Barkat | Speaker | 2227 | 1906-10-25 | 1972-04-05 | false | Q519944 |
| barzilai (exists) | Yisrael Barzilai | minister | 2220 | 1913-10-01 | 1970-06-12 | false | Q2904686 |
| begin | Menachem Begin | minister | 2179 | 1913-08-16 | 1992-03-09 | false | Q130873 |
| ben_aharon | Yitzhak Ben-Aharon | minister | 2194 | 1906-07-17 | 2006-05-19 | false | Q2226775 |
| ben_gurion (exists) | David Ben-Gurion | minister; Prime Minister | 2198 | 1886-10-16 | 1973-12-01 | false | Q37610 |
| ben_zvi (exists) | Yitzhak Ben-Zvi | President | 2207 | 1884-11-24 | 1963-04-23 | false | Q128894 |
| bentov (exists) | Mordechai Bentov | minister | 2210 | 1900-03-28 | 1985-01-18 | false | Q2091327 |
| burg (exists) | Yosef Burg | minister | 2187 | 1909-01-31 | 1999-10-15 | false | Q216021 |
| carmel (exists) | Moshe Carmel | minister | 2415 | 1911-01-17 | 2003-08-14 | false | Q2660910 |
| chaim_herzog | Chaim Herzog | Aman | 2312 | 1918-09-17 | 1997-04-17 | false | Q295141 |
| dayan (exists) | Moshe Dayan | minister (Chief of Staff to 29 January 1958) | 2277 | 1915-05-20 | 1981-10-16 | false | Q188783 |
| dov_yosef (exists) | Dov Yosef | minister | 2375 | 1899-05-27 | 1980-01-07 | false | Q936528 |
| dulzin | Arieh Dulzin | minister | 3257 | 1913-03-31 | 1989-09-13 | false | Q4790361 |
| eban | Abba Eban | minister | 2122 | 1915-02-02 | 2002-11-17 | false | Q305776 |
| elazar | David Elazar | Chief of Staff | none | 1925-08-27 | 1976-04-15 | false | Q467177 |
| eshkol (exists) | Levi Eshkol | minister; Prime Minister | 2173 | 1895-10-25 | 1969-02-26 | false | Q191123 |
| ezer_weizman | Ezer Weizman | minister | 2320 | 1924-06-15 | 2005-04-24 | false | Q128911 |
| galili | Yisrael Galili | minister | 2257 | 1911-02-10 | 1986-02-08 | false | Q1386714 |
| gazit | Shlomo Gazit | Aman | none | 1926-10-22 | 2020-10-08 | false | Q2902267 |
| golda_meir (exists) | Golda Meir | minister; Prime Minister | 2448 | 1898-04-21 / 1898-05-03 | 1978-12-08 | false [C] | Q42992 |
| govrin | Akiva Govrin | minister | 2235 | 1902-08-12 | 1980-06-26 | false | Q2906720 |
| gvati | Haim Gvati | minister | 2233 | 1901-01-29 | 1990-10-19 | false | Q2904362 |
| hammer | Zevulun Hammer | minister | 2300 | 1936-05-31 | 1998-01-20 | false | Q2910400 |
| harel | Isser Harel | Mossad | 2305 | 1912-01 / 1912 | 2003-02-18 | false | Q350744 |
| harkabi | Yehoshafat Harkabi | Aman | none | 1921 / 1921-09-21 | 1994-08-26 | false | Q2561928 |
| hasani | Michael Hasani | minister | 2349 | 1913-06-27 | 1975-07-02 | false | Q2046022 |
| hausner | Gideon Hausner | minister | 2292 | 1915-09-26 | 1990-11-15 | false | Q1391626 |
| hillel | Shlomo Hillel | minister | 2299 | 1923-04-23 | 2021-02-08 | false | Q1719051 |
| hofi | Yitzhak Hofi | Chief of Staff (acting); Mossad | none | 1927-01-25 | 2014-09-15 | false | Q946803 |
| katzir | Ephraim Katzir | President | none | 1916-05-03 / 1916-05-16 | 2009-05-30 | false [C] | Q110330 |
| kol | Moshe Kol | minister | 2575 | 1911-05-28 | 1989-07-07 | false | Q2907940 |
| landau | Haim Landau | minister | 2442 | 1916-09-10 | 1981-10-06 | false | Q2613376 |
| laskov | Haim Laskov | Chief of Staff | none | 1919 / 1919-04 | 1982-12-08 | false | Q1058998 |
| luz (exists) | Kadish Luz | minister, Speaker, acting President 1963 | 2423 | 1895-01-10 | 1972-12-04 | false | Q1720966 |
| mintz | Binyamin Mintz | minister | 2462 | 1903-01-12 | 1961-05-30 | false | Q2904692 |
| mordechai_gur | Mordechai Gur | Chief of Staff | 2247 | 1930-05-06 | 1995-07-16 | false | Q1335169 |
| naftali (exists) | Peretz Naftali | minister | 2493 | 1888-03-19 / 1888-03-29 | 1961-04-30 | false [C] | Q72124 |
| namir (exists) | Mordechai Namir | minister | 2488 | 1897-02-23 | 1975-02-22 | false | Q2909456 |
| nir | Nahum Nir | Speaker | 2486 | 1884-03-17 | 1968-07-10 | false | Q519963 |
| ofer | Avraham Ofer | minister | 2520 | 1922-01-28 | 1977-01-03 | false | Q2909729 |
| peled | Natan Peled | minister | 2540 | 1913-06-03 | 1992-01-08 | false | Q2044141 |
| peres | Shimon Peres | minister | 456 | 1923-08-02 | 2016-09-28 | false | Q57410 |
| pinchas_sapir (exists) | Pinchas Sapir | minister | 2513 | 1906-10-15 | 1975-08-12 | false | Q578459 |
| rabin | Yitzhak Rabin | minister; Prime Minister (Chief of Staff 1964-1968) | 2595 | 1922-03-01 | 1995-11-04 | false | Q34060 |
| rabinovitz | Yehoshua Rabinovitz | minister | 2596 | 1911-11-12 | 1979-08-14 | false | Q434179 |
| rafael | Yitzhak Rafael | minister | 2615 | 1914-07-05 | 1999-08-03 | false | Q2904325 |
| rimalt | Elimelekh Rimalt | minister | 2609 | 1907-11-01 | 1987-11-05 | false | Q2334860 |
| rosen (exists) | Pinchas Rosen | minister | 2600 | 1887-05-01 | 1978-05-03 | false | Q1389085 |
| sasson | Eliyahu Sasson | minister | 2669 | 1902-02-02 | 1978-10-08 | false | Q674248 |
| shamir | Yitzhak Shamir | Speaker | 2652 | 1915-10-22 | 2012-06-30 | false | Q184351 |
| shapira (exists) | Haim-Moshe Shapira | minister | 2661 | 1902-03-26 | 1970-07-16 | false | Q2079775 |
| shazar (exists) | Zalman Shazar | President | 2631 | 1889-12-22 | 1974-10-05 | false | Q299100 |
| sheetrit (exists) | Bechor-Shalom Sheetrit | minister | 2639 | 1895-01-20 | 1967-01-28 | false | Q2909380 |
| shem_tov | Victor Shem-Tov | minister | 2648 | 1915-02-01 | 2014-03-07 | false | Q2891007 |
| sherf | Ze'ev Sherf | minister | 2668 | 1906-04-21 | 1984-04-18 | false | Q184431 |
| shlomo_rosen | Shlomo Rosen | minister | 2601 | 1905-06-21 | 1985-12-07 | false | Q2904443 |
| sprinzak | Yosef Sprinzak | Speaker | 2665 | 1885-12-08 | 1959-01-28 | false | Q974766 |
| toledano | Ya'akov Moshe Toledano | minister | 3258 | 1880-08-18 | 1960-10-15 | false | Q2906690 |
| tzvi_tzur | Tzvi Tzur | Chief of Staff | 2569 | 1923-04-17 | 2004-12-28 | false | Q1385464 |
| uzan | Aharon Uzan | minister | 2129 | 1924-11-01 | 2007-01-23 | false | Q2906629 |
| warhaftig (exists) | Zorach Warhaftig | minister | 2325 | 1906-02-02 | 2002-09-26 | false | Q2890045 |
| yaacobi | Gad Yaacobi | minister | 2380 | 1935-01-18 | 2007-08-27 | false | Q612860 |
| yaakov_shimshon_shapira | Ya'akov Shimshon Shapira | minister | 2662 | 1902-11-04 | 1993-11-14 | false | Q937011 |
| yadlin | Aharon Yadlin | minister | 2370 | 1926-04-17 | 2022-08-12 | false | Q266713 |
| yariv | Aharon Yariv | minister (Aman 1964-1972) | 2388 | 1920-12-20 | 1994-05-07 | false | Q2886487 |
| yeshayahu | Yisrael Yeshayahu | minister, Speaker | 2389 | 1908-04-20 | 1979-06-20 | false | Q983232 |
| yosef_sapir (exists) | Yosef Sapir | minister | 2512 | 1902-01-27 | 1972-02-26 | false | Q2614158 |
| yoseftal | Giora Yoseftal | minister | 2377 | 1912-08-09 | 1962-08-23 | false | Q216050 |
| zadok | Haim Yosef Zadok | minister | 2565 | 1913-10-02 | 2002-08-15 | false | Q434951 |
| zamir | Zvi Zamir | Mossad | none | 1925-03-03 | 2024-01-02 | false | Q727529 |
| zeira | Eli Zeira | Aman | none | 1928-04-04 | 2025-11-21 | false | Q2910612 |
