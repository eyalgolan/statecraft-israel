# New kibbutzim and moshavim of 1957-1977 inside the pre-1967 lines (research note, 2026-10-25)

Status: research only. Nothing here is in `content/`, `sim/`, `tools/` or `unity/`; the dated settlements file (#61)
stays paused until after the MVP. Refs #130 (1957-77 18), the "Settlements" row of [the 1957-1977
design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md) section 7.3: "new kibbutzim and moshavim".
One topic of #130 only. **Development towns (PR #1643), settlements in the territories and the lines are not in this
note.** Which rows enter the game, and their names, wait for the owner's sensitivity list (#118); this note makes no
include or omit choice and no naming choice.

It follows the format of [`settlements-1949-51.md`](../GDD%20objectives/settlements-1949-51.md). Every page below was
opened on **25 October 2026** (UTC) unless a row says otherwise.

The question: which kibbutzim and moshavim were founded between 1 January 1957 and 31 December 1977 inside Israel's pre-1967 lines, and for each: the founding date (and what it marks), coordinates from a named source, and the sources.

## Short answer

- **The list.** The Central Bureau of Statistics (CBS) localities file 2024 has 89 rows with a year of founding of
  1957 to 1977 and a form-of-settlement code that I read as kibbutz, moshav or moshav shitufi. 21 are in the Judea and
  Samaria district and 19 in the Golan subdistrict and are left out (topic of another part of #130). **49 remain**: 31
  moshavim, 4 moshavim shitufiyim and 14 kibbutzim.
- **Four of the 49 are flagged**, not removed: Kefar Rut, Shilat, Senir and Sufa (section "Rows whose site may be
  outside the pre-1967 lines"). The note does not decide whether they belong.
- **29 of the 49 rows carry a disagreement [C]** on the year, the month or the day among CBS, Gvati and Wikipedia; 20
  rows agree on the year across the sources read, and for those the day, where there is one, rests on Gvati's list
  alone.
- **No contemporaneous source was read for any row** (no press report, no government record of the day of settling).
  Every founding date below is [S] or [C]; the only verified items are what the CBS file and the CBS intro print. That
  is the main [GAP] of the note, listed in "What was not done".

## Conventions and tags

- **[V, agent read 2026-10-25; a person's check pending]**: what a CBS publication prints, read today. It says nothing about whether the printed value is right.
- **[S]** secondary: one source, or several that plainly copy one another. Gvati (below) and Wikipedia are [S]. Where Wikipedia cites a newspaper, a book or a government publication, I do not count that source as read; I did not open any Wikipedia citation.
- **[C]** conflicting: sources disagree; every value is given and what each marks, as far as the source says.
- **[GAP]** not closed, with what was tried.
- **Derived** marks a number or a date this note computed; the arithmetic or the library is named.
- Quotes are copied from the page read; Hebrew quotes carry a translation. Translations from Hebrew are mine.

## How it was done

- **The list and the CBS year and coordinates.** CBS, localities file 2024 ("קובץ יישובים 2024"),
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (downloaded and opened 2026-10-25; 1,490
  locality rows, one sheet). Filter: column "שנת ייסוד" (year of founding) from 1957 to 1977 **and** column "צורת
  יישוב שוטפת" (form of settlement) equal to 310, 320 or 330. I excluded, by the file's own columns, district "אזור
  יהודה ושומרון" (Judea and Samaria) and subdistrict "גולן". The CBS year is its "year of Jewish settlement or
  founding" (its intro, section 12: the year of founding or of settling by Jews; the last settling year after a long
  break, the original year after a short one, the older community's year when localities merged).
- **What the codes mean.** The 2024 file does not list the code meanings and its intro
  (https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/intro.pdf, December 2025, 14 pages) defines the forms in
  words, not by code. I cross-tabulated the 2024 codes against the form printed in the CBS publication "Local
  authorities in Israel 2009", list 03, settlements in regional councils
  (https://www.cbs.gov.il/he/publications/doclib/2011/local_authorities09/pdf/list03_h.pdf), matching on the 4-digit
  locality symbol. **Derived** result: 310 = moshav (394 of 398 matched symbols print "מושב"; 2 print kibbutz, 2 other
  rural), 320 = moshav shitufi (39 of 41 print "מושב שיתופי", 1 prints moshav and 1 community), 330 = kibbutz (263 of
  264 print "קיבוץ"), 340 = institutional settlement (27 of 27), 350 = other rural settlement (52 of 52), 370 =
  community settlement (87 of 88). The few mismatches are places whose form changed after 2009. The mapping is mine,
  read from that table; the CBS does not state it for the 2024 file. The 2009 list covers regional-council settlements
  only.
- **Coordinates.** The CBS field "קואורדינטות" has twelve digits; as in `settlements-1949-51.md` I read it as an
  Israel Transverse Mercator easting and northing in metres (six digits each) and converted it with `pyproj`
  (EPSG:2039 to EPSG:4326), rounded to five decimals. That reading is mine, not stated in the file (the intro's
  section 14 describes a different format, ten digits in hundreds of metres, which the 12-digit field does not match).
  **Derived** check against Wikipedia's own coordinates (haversine, my computation): Hebrew Wikipedia has a coordinate
  for 49 of the 49 rows (median 16 m, largest 439 m, Ne'ot HaKikkar); English Wikipedia for 49 (median 98 m, largest
  506 m, Giv'at Yesha'yahu). Raw CBS fields are in the table.
- **Founding days: Gvati.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural
  settlements in Israel", Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (opened 2026-10-25; 756
  appendix rows parsed by script from the page). Columns: name, region, date of founding, affiliation, origin of the
  name. It is one secondary compilation; it does not say whether its date is the day of going onto the land. It has
  misprints I can see: Zerachia is given "32.8.50", an impossible date. For the rows below I quote its appendix row;
  where the appendix has no row I searched the page text (see Searches) and say so. Gvati's narrative chapters are
  cited where they date a row.
- **Wikipedia** (Hebrew and English) was fetched through the MediaWiki API as wikitext of the current revision; each
  revision is named in the last table (`oldid`). It is a lead and a cross-check, always [S]. Hebrew Wikipedia's
  village infobox is a template filled by a bot and carries no founding date; the Hebrew date is in its prose. English
  infoboxes often give a founding date that sometimes disagrees with the article's own text; both are given.
- Hebrew dates were converted with the `convertdate` library (Python), month numbers as that library numbers them.
- All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.

## Summary table

Order: by CBS year, then name. "CBS year" and "CBS code" are [V, agent read 2026-10-25; a person's check pending] as to what the file prints. Coordinates are **derived** from the CBS field (method above). Gvati is the appendix row quoted as printed, "-" where the appendix has none. "Tag" is the note's own tag for the founding date: [C] sources disagree, [S] they agree on the year (the day, where given, is Gvati's).

| # | Locality (CBS spelling) | Type | CBS year | CBS code | Lat, lon (derived) | CBS raw field | Gvati date | Tag | Flag |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Ammi'oz | moshav | 1957 | 310 | 31.24846, 34.41295 | 144060573257 | - | [C] |  |
| 2 | Or HaNer | kibbutz | 1957 | 330 | 31.55711, 34.59989 | 162056607367 | 1.10.57 | [S] |  |
| 3 | Shoqeda | moshav | 1957 | 310 | 31.42200, 34.52471 | 154825592428 | 4.7.57 | [S] |  |
| 4 | Yesha | moshav | 1957 | 310 | 31.24755, 34.40255 | 143069573163 | 10.3.57 | [S] |  |
| 5 | Zimrat | moshav | 1957 | 310 | 31.44804, 34.55258 | 157492595299 | 3.3.51 | [C] |  |
| 6 | Adamit | kibbutz | 1958 | 330 | 33.07929, 35.20855 | 219840776009 | 29.5.58 | [C] |  |
| 7 | Avi'ezer | moshav | 1958 | 310 | 31.68179, 35.01679 | 201663621049 | 8.4.58 | [S] |  |
| 8 | Giv'at Shappira | moshav | 1958 | 310 | 32.35744, 34.87665 | 188603696003 | 1958 | [C] |  |
| 9 | Giv'at Yesha'yahu | moshav | 1958 | 310 | 31.67114, 34.94548 | 194899619882 | 31.5.58 | [S] |  |
| 10 | Newe Mikha'el | moshav | 1958 | 310 | 31.67294, 35.00618 | 200656620069 | 29.7.58 | [C] |  |
| 11 | Zafririm | moshav | 1958 | 310 | 31.66008, 34.94393 | 194749618656 | 12.5.58 | [S] |  |
| 12 | Kefar Maymon | moshav | 1959 | 310 | 31.43218, 34.53590 | 155896593550 | 20.8.59 | [S] |  |
| 13 | Avivim | moshav | 1960 | 310 | 33.08862, 35.47180 | 244417777076 | 2.9.60 | [C] |  |
| 14 | En Hazeva | moshav shitufi | 1960 | 320 | 30.79853, 35.24591 | 223425523103 | - | [C] |  |
| 15 | Even Menahem | moshav | 1960 | 310 | 33.07423, 35.29393 | 227812775451 | 13.9.60 | [S] |  |
| 16 | Ram-On | moshav | 1960 | 310 | 32.52690, 35.25971 | 224649714749 | 14.5.60 | [C] |  |
| 17 | Yodefat | moshav shitufi | 1960 | 320 | 32.83761, 35.27191 | 225773749208 | 1960 | [S] |  |
| 18 | Adderet | moshav | 1961 | 310 | 31.66044, 34.99489 | 199582618685 | 12.7.61 | [C] |  |
| 19 | Almagor | moshav | 1961 | 310 | 32.91210, 35.60239 | 256683757537 | 29.11.61 | [C] |  |
| 20 | Ne'ot HaKikkar | moshav | 1961 | 310 | 30.93372, 35.37824 | 236067538103 | 2.59 | [C] |  |
| 21 | Elot | kibbutz | 1962 | 330 | 29.58148, 34.96283 | 196047388208 | 21.12.62 | [S] |  |
| 22 | Ma'ale Gilboa | kibbutz | 1962 | 330 | 32.47695, 35.41875 | 239602709229 | 25.1.52 | [C] |  |
| 23 | Dovev | moshav | 1963 | 310 | 33.05177, 35.40722 | 238396772975 | 11.9.58 | [C] |  |
| 24 | Gerofit | kibbutz | 1963 | 330 | 29.94121, 35.06450 | 205946428068 | 25.11.63 | [C] |  |
| 25 | Me Ammi | moshav shitufi | 1963 | 320 | 32.50474, 35.14767 | 214121712292 | 29.11.63 | [S] |  |
| 26 | Hazeva | moshav | 1965 | 310 | 30.76843, 35.27988 | 226679519767 | 9.12.65 | [C] |  |
| 27 | Alumim | kibbutz | 1966 | 330 | 31.45228, 34.51334 | 153765595792 | 1966 | [S] |  |
| 28 | Netu'a | moshav | 1966 | 310 | 33.06470, 35.32350 | 230575774397 | 8.12.66 | [S] |  |
| 29 | Kefar Rozenwald(Zarit) | moshav | 1967 | 310 | 33.10007, 35.28836 | 227290778317 | 1969 | [C] |  |
| 30 | Senir | kibbutz | 1967 | 330 | 33.24222, 35.67798 | 263589794179 | 26.9.67 | [S] | site, see below |
| 31 | Hazon | moshav | 1969 | 310 | 32.90649, 35.39489 | 237273756861 | 69. | [S] |  |
| 32 | Ohad | moshav | 1969 | 310 | 31.23718, 34.43139 | 145808571993 | 1969 | [C] |  |
| 33 | Shetula | moshav | 1969 | 310 | 33.08525, 35.31417 | 229701776676 | 1969 | [C] |  |
| 34 | Qetura | kibbutz | 1970 | 330 | 29.96997, 35.06106 | 205618431256 | 1970 | [C] |  |
| 35 | Talme Eliyyahu | moshav | 1970 | 310 | 31.22901, 34.42903 | 145577571089 | 1970 | [S] |  |
| 36 | Zofar | moshav | 1970 | 310 | 30.56033, 35.18066 | 217175496694 | - | [C] |  |
| 37 | Zur Natan | moshav shitufi | 1970 | 320 | 32.24077, 35.01318 | 201431683034 | 9.6.66 | [C] |  |
| 38 | Paran | moshav | 1972 | 310 | 30.36319, 35.15462 | 214667474840 | 1971 | [C] |  |
| 39 | Sede Nizzan | moshav | 1973 | 310 | 31.23015, 34.41932 | 144653571222 | 1973 | [S] |  |
| 40 | Sufa | kibbutz | 1975 | 330 | 31.23732, 34.34215 | 137306572072 | 1975 | [C] | site, see below |
| 41 | Ya'ad | moshav | 1975 | 310 | 32.87931, 35.24240 | 223009753831 | 1975 | [C] |  |
| 42 | Kishor | kibbutz | 1976 | 330 | 32.94607, 35.25003 | 223719761235 | 1976 | [C] |  |
| 43 | Magen Sha'ul | moshav | 1976 | 310 | 32.52123, 35.30760 | 229149714124 | 1976 | [C] |  |
| 44 | Samar | kibbutz | 1976 | 330 | 29.83233, 35.02252 | 201874416004 | 1976 | [C] |  |
| 45 | Yahel | kibbutz | 1976 | 330 | 30.08356, 35.12824 | 212110443842 | 18.11.76 | [C] |  |
| 46 | Bet Rimmon | kibbutz | 1977 | 330 | 32.78203, 35.32813 | 231044743048 | 1977 | [S] |  |
| 47 | Kefar Rut | moshav | 1977 | 310 | 31.90989, 35.03070 | 203023646340 | 77. | [S] | site, see below |
| 48 | Moran | kibbutz | 1977 | 330 | 32.92162, 35.39372 | 237161758538 | 1977 | [S] |  |
| 49 | Shilat | moshav | 1977 | 310 | 31.91883, 35.01890 | 201908647333 | 1977 | [C] | site, see below |

## The rows

Each row: what the sources say about the founding, in the order CBS, Gvati, Hebrew Wikipedia, English Wikipedia, and what each date marks where a source says. The CBS year, code and coordinate for the row are in the summary table.

1. Ammi'oz (moshav, CBS 1957) [C]. CBS 1957. Not an appendix row in Gvati (searched, see Searches); Gvati's narrative
   for 5717 lists it among four Western Negev moshavim. Hebrew WP: first established in 1956 by Jews from Egypt and
   Romania; a 1961 try by Vizhnitz Hasidim failed. English WP: first established 1956, "permanently established in
   1962" by Moroccan immigrants. Years 1956, 1957 and 1962 each mark a different step; no source gives the day.

2. Or HaNer (kibbutz, CBS 1957) [S]. Gvati: 1 October 1957 (Lachish region); its narrative says the Givot Zaid members
   moved "at the end of 5717" (the year ends 25 Sep 1957, derived). Hebrew WP: founded 1957 by former Givot Zaid
   members and the Nahal gar'in Tzal-Tzach, on a site where the Yitzur u-Pituach company ran a farm in 1955-1957.
   English WP: infobox 1957, text "established between 1955 and 1957 as a farm". The year agrees; what the date marks
   differs (the group's arrival, not the farm).

3. Shoqeda (moshav, CBS 1957) [S]. Gvati: 4 July 1957. Hebrew WP: set up in 1957 by the Jewish Agency Settlement
   Department, Moroccan immigrants. English WP: 1957, the same. Year agrees; the day rests on Gvati alone.

4. Yesha (moshav, CBS 1957) [S]. Gvati: 10 March 1957 (Western Negev). Hebrew WP: founded in 1957 by Jews from Egypt
   expelled after the 1956 Sinai campaign. English WP: 1957, the same group. CBS, Gvati and both articles agree on
   1957; the day rests on Gvati alone.

5. Zimrat (moshav, CBS 1957) [C]. Gvati's appendix row says 3 March 1951 (affiliation Hapoel HaMizrahi); CBS says
   1957. Gvati's own narrative for the Hebrew year 5717 (6 Sep 1956 to 25 Sep 1957, derived) names Zimrat among four
   moshavim set up in the Western Negev, so the appendix day disagrees with Gvati's text; I did not establish which is
   right, and I do not know whether the appendix has a misprint (it prints the impossible day "32.8.50" for Zerachia).
   Hebrew WP: founded in 1957 by immigrants from Tunisia after the neighbouring Shuva split (first called "Shuva B").
   English WP: 1957, the same.

6. Adamit (kibbutz, CBS 1958) [C]. Gvati: 29 May 1958. English WP infobox: "August 1958", by Hashomer Hatzair members.
   Hebrew WP: the settlers "הגיעו למקום למחרת יום הכיפורים, בספטמבר 1958" (came to the place the day after Yom Kippur,
   September 1958); its infobox gives the core group as 1958; resettled in 1971 by immigrants from England, the US and
   Canada. May (Gvati), August (English WP) and September (Hebrew WP); I did not open any citation.

7. Avi'ezer (moshav, CBS 1958) [S]. Gvati: 8 April 1958. Hebrew WP: April 1958, on land of the abandoned Arab village
   Bayt Nattif. English WP: 8 April 1958, "initially named Adulam". The three agree; the English day may come from
   Gvati, which I cannot tell.

8. Giv'at Shappira (moshav, CBS 1958) [C]. Gvati: 1958 (year only). English WP: 1958. Hebrew WP (article "Giv'at
   Shappira (moshav)"): the land group decided in 1932; the name was fixed in 1934; a 1954 agreement with RASKO led to
   houses; "at the start of 1959" the committee members broke into six empty houses and housed new families (a date
   "22.3.1959" stands in the sentence); the first 30 houses were finished in 1957. 1954, 1957, 1958 and 1959 mark
   different steps.

9. Giv'at Yesha'yahu (moshav, CBS 1958) [S]. Gvati: 31 May 1958 (Adullam region). Hebrew WP: 1958, immigrants from
   Hungary. English WP: 1958, members of HaNoar HaTzioni. Day rests on Gvati alone.

10. Newe Mikha'el (moshav, CBS 1958) [C]. CBS names Newe Mikha'el; the facts below are for Roglit. Hebrew WP says the
   settlement formed from the union of the moshav Roglit and the regional centre Neve Michael, and the CBS record is
   for the merged settlement (my reading). Gvati row "Roglit": 29 July 1958. English WP: 29 July 1958 "(12 Av 5718)",
   which converts to 29 July 1958 (derived). Hebrew WP: "31 July 1958". Day differs by two.

11. Zafririm (moshav, CBS 1958) [S]. Gvati: 12 May 1958. Hebrew WP: works in 1957, settled in May 1958 by immigrants
   from North Africa and India of the "From City to Village" movement. English WP: 1958. The day rests on Gvati alone.

12. Kefar Maymon (moshav, CBS 1959) [S]. Gvati: 20 August 1959 (Northern Negev). Hebrew WP: founded in the Hebrew year
   5719 (1959) with the help of the Jewish Colonization Association by a Bnei Akiva gar'in, the first moshav set up by
   Bnei Akiva. English WP: 1959, the same. Day rests on Gvati alone.

13. Avivim (moshav, CBS 1960) [C]. Gvati: 2 September 1960 (Upper Galilee). Hebrew WP says "המושב הוקם בשנת 1958" (the
   moshav was founded in 1958), "ובשנת 1960 הוא יושב מחדש" (in 1960 it was resettled) by new immigrants, and in
   another sentence resettled in 1963 by the Moshavim Movement with North African immigrants. English WP: founded
   1958, abandoned, re-established 1963. 1958, 1960 and 1963 all appear.

14. En Hazeva (moshav shitufi, CBS 1960) [C]. CBS code 320. Gvati's appendix has no row (searched, see Searches); its
   narrative says that in 1970 two settlements were set up in the Arava and names En Hazeva, an agricultural
   settlement set up by a small group of farmers from the Sharon (my translation), so Gvati says 1970. English WP:
   1960, an unaffiliated farm "not recognized by the government". Hebrew WP: founded 1962 as an agricultural farm; in
   August 1964 the Jewish Agency decided on a Nahal outpost nearby (it became Hazeva). CBS and English WP 1960, Hebrew
   WP 1962, Gvati 1970.

15. Even Menahem (moshav, CBS 1960) [S]. Gvati: 13 September 1960. Hebrew WP: 1960, North African immigrants. English
   WP: 13 September 1960. Agree; the day may trace to Gvati.

16. Ram-On (moshav, CBS 1960) [C]. Gvati: 14 May 1960. Hebrew WP: the core group went up to Nurit on Mount Gilboa in
   June 1958; in 1961 the members came down to the permanent outpost. English WP infobox 1953: the group first settled
   at Nurit and moved in 1961 to the present site. 1953, 1958, 1960 and 1961 mark different steps.

17. Yodefat (moshav shitufi, CBS 1960) [S]. CBS code 320. Gvati: 1960. Hebrew WP: a core group formed in 1958 (pupils
   of Dr Yosef Schechter, Haifa Reali School). English WP: established 1960 by Reali graduates. Year agrees; 1958
   marks the group.

18. Adderet (moshav, CBS 1961) [C]. Gvati: 12 July 1961 (Judean Foothills). English WP infobox: 1961, Moroccan Jews;
   its text: "founded in the early 1959" by Jewish immigrants from Romania, re-established in 1963. Hebrew WP (article
   "Adderet (moshav)"): a first state attempt in 1959 with immigrants from Romania; the families left; a Ministry of
   Agriculture committee found in early 1961 no point in continuing as an agricultural moshav; resettled in 1963 by
   immigrants from Morocco. 1959, 1961 and 1963.

19. Almagor (moshav, CBS 1961) [C]. Gvati: 29 November 1961 (Upper Galilee). Hebrew WP: went onto the land "in
   December 1961" as a Nahal outpost opposite the Syrian border. English WP: 1961, Nahal. 29 November against
   December.

20. Ne'ot HaKikkar (moshav, CBS 1961) [C]. Gvati appendix row "Ne'ot HaKikkar" (spelled with a doubled kaf), Arava,
   "2.59" (my reading: February 1959; the column gives day.month.year elsewhere); its narrative puts the moshav in
   1974. Hebrew WP: first settled in 1959 as a cattle ranch by young people who went up against the position of the
   settling bodies; a desalination unit in August 1961. English WP: moshav "established in 1970". CBS 1961. 1959,
   1961, 1970 and 1974.

21. Elot (kibbutz, CBS 1962) [S]. Gvati: 21 December 1962 (Arava). Hebrew WP: a group of 15 of the United Kibbutz went
   down to Eilat on 16 June 1955; the members voted on 14 July 1962 for the name Eilot; the names committee decided on
   23 September 1962 for Nir Eilat (names in my translation); the going-up ceremony was in December 1962. English WP
   infobox 1962 (its category says 1963). Gvati's day and Hebrew WP's month agree.

22. Ma'ale Gilboa (kibbutz, CBS 1962) [C]. Gvati row "Ma'ale Gilboa": 25 January 1952 (Gilboa); CBS says 1962. Hebrew
   WP: Nahal outpost on 29 November 1962; civilianised in 1968 by Religious Kibbutz members. English WP: text 1962,
   infobox 1963; settled 1967 by the Religious Kibbutz Movement. Gvati's 1952 is not explained (a misprint is
   possible, not shown).

23. Dovev (moshav, CBS 1963) [C]. CBS 1963. Gvati: appendix 11 September 1958 (name spelled Dovev); its narrative says
   that in 1963 the moshav Dovev was set up (my translation). Hebrew WP: founded 1958 by immigrants from Morocco and
   Persia of the "From City to Village" movement, and resettled in November 1963 (a cited Al HaMishmar item of 12
   November 1963, not opened). English WP: text 1958, infobox 1963. On Hebrew WP's account 1958 marks the first
   settling and 1963 the resettling.

24. Gerofit (kibbutz, CBS 1963) [C]. Gvati: 25 November 1963. Hebrew WP: Nahal outpost of 1963; the going-up ceremony
   was on 26 November 1963. English WP: 1963. 25 against 26 November.

25. Me Ammi (moshav shitufi, CBS 1963) [S]. CBS code 320. Gvati: 29 November 1963 (it lists the region as Mount
   Samaria). Hebrew and English WP: 1963 as a Nahal outpost, civilianised 1969. The day rests on Gvati alone.

26. Hazeva (moshav, CBS 1965) [C]. Gvati: 9 December 1965 (Arava). Hebrew WP: the Nahal outpost went onto the land on
   9 December 1965 (the 33rd); civilianised 21 November 1967. English WP: founded 1965 as a Nahal settlement, a moshav
   in 1968 (infobox 1968). CBS, Gvati and Hebrew WP agree on 1965 and the day; English WP's infobox says 1968, the
   year it became a moshav.

27. Alumim (kibbutz, CBS 1966) [S]. Gvati: 1966. Hebrew WP: the first settlers went up in Elul 5726 (17 Aug to 14 Sep
   1966, derived); the cornerstone was laid on 29 Kislev 5725 (4 December 1964, derived). English WP: 1966. Year
   agrees.

28. Netu'a (moshav, CBS 1966) [S]. Gvati: 8 December 1966. Hebrew WP: 1966, Operation "Sof Sof". English WP: 1966. Day
   rests on Gvati alone.

29. Kefar Rozenwald(Zarit) (moshav, CBS 1967) [C]. CBS names it Kefar Rozenwald (Zarit). Gvati row "Zar'it (Kefar
   Rosenwald)": 1969 in the appendix, but its narrative says Netu'a and Zarit were set up in 1966 and 1967 (Gvati
   disagrees with itself). Hebrew WP: construction began 30 January 1966; inaugurated in 1967; first named Zarit,
   renamed Kefar Rosenwald two years later. English WP: 1967. CBS and both articles say 1967; Gvati says 1969 and
   1966-1967.

30. Senir (kibbutz, CBS 1967) [S]. Flag: the site lay in the Banias area. Gvati: 26 September 1967. Hebrew WP: 26
   September 1967 (21 Elul 5727, which converts to the same day, derived), a Nahal outpost on the Banias plateau,
   "אזור מפורז" (a demilitarised zone) since the 1949 armistice, held in practice by Syria until the Six-Day War.
   English WP: the same date, a former demilitarised zone. CBS lists it in the Safed subdistrict.

31. Hazon (moshav, CBS 1969) [S]. Gvati: "69." in the appendix and 1969 in the narrative. Hebrew WP: part of Operation
   Sof Sof; the founding was approved in June 1965; "אוכלס לבסוף ביולי 1969" (finally settled in July 1969). English
   WP: 1969. Gvati gives only the year; no source read gives the day.

32. Ohad (moshav, CBS 1969) [C]. Gvati: 1969. Hebrew WP: built in 1966, occupation frozen after the Six-Day War;
   occupation renewed on 15 September 1969; the first 15 families came through the Moshavim Movement agency. English
   WP: 1969. 1966 marks construction; 1969 the settling.

33. Shetula (moshav, CBS 1969) [C]. Gvati: 1969 (narrative). Hebrew WP: houses built from the end of 1966; the
   settlers entered in March 1969. English WP: 1967. CBS 1969.

34. Qetura (kibbutz, CBS 1970) [C]. Gvati: 1970. Hebrew WP: Nahal soldiers settled in December 1970; the ceremony was
   on 31 March 1971; civilianised in November 1973 by Young Judaea members. English WP infobox: November 1973. 1970,
   1971 and 1973.

35. Talme Eliyyahu (moshav, CBS 1970) [S]. Gvati: 1970. Hebrew WP: 1970, settled by the "Arnon" gar'in of young
   immigrants from France. English WP: 1970, a collective moshav that became a workers' moshav in 1974. Agree.

36. Zofar (moshav, CBS 1970) [C]. Not an appendix row in Gvati (searched); its narrative says a Nahal outpost was set
   up in the Arava in 1968 and a moshav after civilianisation. Hebrew WP: Nahal outpost Zofar, 3 September 1968.
   English WP: Nahal in 1968, a moshav in 1975 after moving north; infobox 1970. CBS 1970. 1968, 1970 and 1975.

37. Zur Natan (moshav shitufi, CBS 1970) [C]. CBS code 320. Gvati: 9 June 1966. Hebrew WP: the Nahal outpost went onto
   the land on 9 July 1966; civilianised on 15 Av 5727 (21 August 1967, derived). English WP: 1966. 9 June against 9
   July; CBS 1970 is not explained by any source read.

38. Paran (moshav, CBS 1972) [C]. CBS 1972. Gvati's appendix: 1971; Gvati's narrative: "ב־1972 נוסף בערבה המושב
   פארן" (in 1972 the moshav Paran was added in the Arava). Hebrew WP: November 1971, Nahal gar'ins of moshav sons; a
   moshav of workers from 1975. English WP: 1971. CBS and Gvati's narrative 1972; Gvati's appendix, Hebrew and English
   WP 1971.

39. Sede Nizzan (moshav, CBS 1973) [S]. Gvati: 1973. Hebrew WP: 1973, immigrants from England and the US. English WP:
   1973. Agree.

40. Sufa (kibbutz, CBS 1975) [C]. Flag: the 1975 date belongs to an outpost in the Rafah Approaches. Gvati row "Sufa":
   1975, region "Rafah Approaches" (פתחת רפיח). Hebrew WP: began as the Nahal outpost "Nahal Sukkot", which went up in
   the Rafah Approaches in 1974; civilianised on 17 January 1977; evacuated in 1982 after the peace treaty and moved.
   English WP: "founded in 1982 by former residents of Sufa, an Israeli settlement in Sinai". CBS 1975 lists the
   present place in the Be'er Sheva subdistrict.

41. Ya'ad (moshav, CBS 1975) [C]. Gvati: appendix 1975; its narrative puts Ya'ad among the three settlements of 1974.
   Hebrew WP: core group formed in the early 1970s; went to a temporary site at the end of 1974; in June 1975 17
   families lived at the Yavor caravan site; the permanent site was reached in November 1978. English WP: infobox
   1974; its text says founded in 1974 and adds Walid Khalidi's 1975. 1974, 1975 and 1978.

42. Kishor (kibbutz, CBS 1976) [C]. Gvati's appendix has "Nahal Kishor" (Western Galilee) 1976; its narrative says a
   Nahal gar'in went up in 1975 and set up Kishor. Hebrew WP: Nahal outpost on 8 January 1976 by a Hashomer Hatzair
   gar'in. English WP (article Kishorit): founded "in the late 1970s", abandoned, refounded in 1997 as Kishorit. 1975,
   8 January 1976 and "late 1970s".

43. Magen Sha'ul (moshav, CBS 1976) [C]. Gvati: 1976. Hebrew WP: founded 1975 by sons of neighbouring moshavim
   (Ta'anach region); 45 families in a temporary camp until the permanent moshav of 1977. English WP: text 1976,
   infobox 1953 (its own text and the infobox disagree).

44. Samar (kibbutz, CBS 1976) [C]. Gvati: appendix 1976; its narrative puts the foundation of Samar in the 1975
   paragraph. Hebrew WP: 1976 by a gar'in of kibbutz sons wanting another framework. English WP: 1976, the same. CBS
   1976. 1975 against 1976.

45. Yahel (kibbutz, CBS 1976) [C]. Gvati: 18 November 1976. Hebrew WP: went onto the land in November 1976, the first
   Reform Jewish settlement. English WP: "Established in 1977". Late 1976 against 1977.

46. Bet Rimmon (kibbutz, CBS 1977) [S]. Gvati's appendix row "Nahal Beit Rimon": 1977 (Hapoel HaMizrahi). Hebrew WP:
   the going-up ceremony of the Nahal outpost was on 8 March 1977, with a Bnei Akiva gar'in. English WP: 1977; British
   immigrants established the kibbutz there in 1979. Year agrees.

47. Kefar Rut (moshav, CBS 1977) [S]. Flag: no-man's land before 1967. Gvati: "77." Hebrew WP: "הוקם על גבול הקו
   הירוק, במה שנחשב לשטח ההפקר בין ישראל לירדן עד 1967" (on the Green Line, in what was considered no man's land
   between Israel and Jordan until 1967), by decision of the first Rabin government. English WP: 1977, in a no-man's
   land after 1948 that came under Israeli control in the Six-Day War.

48. Moran (kibbutz, CBS 1977) [S]. Gvati: 1977. Hebrew WP: founded 1977 by the United Kibbutz; a pioneer unit came in
   September 1977. English WP: 1977, a kvutza trained at Ginosar. Agree.

49. Shilat (moshav, CBS 1977) [C]. Flag: whether the site was in Israeli-held land before June 1967 is [GAP] (no map
   source read). Gvati: appendix 1977; its narrative puts it in 1976. Hebrew WP: founded 1977 where the Arab village
   Shilta stood until 1948; the Ministerial Committee on Settlement under Israel Galili decided, after the UN General
   Assembly's December 1975 resolution, to thicken settlements on the Green Line; the first 25 families went up on 16
   August 1977. English WP: "July 1977", 25 families; its lead describes the place as partly in Israel and partly in
   the Latrun salient of the Green Line (my paraphrase). 16 August against July; 1976 against 1977.

## Rows whose site may be outside the pre-1967 lines

The filter above is the CBS district and subdistrict, which place all 49 in Israel proper today. For four rows,
Wikipedia's own text suggests the founding site, or the 1975 outpost behind the CBS year, was outside the area Israel
held before June 1967. They stay in the table, flagged, and the owner decides.

- **Kefar Rut (row in the table, 1977).** Hebrew and English Wikipedia both place it in the no-man's land between
  Israel and Jordan until 1967 (quoted in its row). English Wikipedia also cites ARIJ for dunams expropriated after
  1967, which I did not weigh. [S] Gvati's narrative chapter introduces its 1968-77 list as settlements built "בגבולות
  הקו הירוק" (within the Green Line) and names Kefar Rut in it (founded in the Ayalon valley by sons of the moshavim of
  the Jerusalem corridor). [S] The same list names Shilat, two
  paragraphs before Kefar Rut: "בעמק אילון, דרומית־מזרחית למבוא־מודיעים, נוסד מושב שילת (חורון ב׳) על־ידי משפחות העובדים במפעלי־תעשייה באזור לוד־רמלה" (in the Ayalon valley,
  south-east of Mevo Modi'im, the moshav Shilat (Horon B) was founded by families of workers in industrial plants in
  the Lod-Ramla area). [S]
- **Senir (1967).** Hebrew Wikipedia places it on the Banias plateau, a demilitarised zone held in practice by Syria
  until June 1967; English Wikipedia says only that it was built in what had been a demilitarised zone until the Six-
  Day War (both in its row). [S]
- **Sufa (1975).** The CBS year follows an outpost in the Rafah Approaches (Gvati, Hebrew Wikipedia); English
  Wikipedia calls the old Sufa "an Israeli settlement in Sinai" and dates the present place to 1982. [C]
- **Shilat (1977).** Hebrew Wikipedia says the settlement was meant to thicken the Green Line; I read no map source
  that says which side of the 1949 line the site was on. **[GAP]** Gvati lists it among the settlements built within
  the Green Line (quoted under Kefar Rut above). [S]

## Rows left out of this note

- **Territories and the Golan** (21 + 19 rows, CBS codes 310, 320 or 330, year 1957-1977): left out by topic. Judea
  and Samaria district: Kefar Ezyon (1967), Argaman (1968), Mehola (1968), Qalya (1968), Rosh Zurim (1969), Gilgal
  (1970), Massu'a (1970), Yitav (1970), Hamra (1971), Mizpe Shalem (1971), Beqa'ot (1972), Gittit (1973), Mekhora
  (1973), Peza'el (1975), Netiv HaGedud (1976), Ro'i (1976), Almog (1977), Migdal Oz (1977), Na'aran (1977), Rehan
  (1977), Sal'it (1977). Golan subdistrict: Afiq (1967), Merom Golan (1967), Ne'ot Golan (1967), Eli-Ad (1968), En
  Ziwan (1968), Giv'at Yo'av (1968), Mevo Hamma (1968), Ramat Magshimim (1968), Ramot (1970), El-Rom (1971), Geshur
  (1971), Newe Ativ (1972), Nov (1973), Kefar Haruv (1974), Qeshet (1974), Ma'ale Gamla (1976), Odem (1976), Sha'al
  (1976), Yonatan (1976). Counts from the CBS file, opened 2026-10-25; no judgement is made here.
- **Other rural codes in the same window, inside the district and subdistrict filter** (27 rows). By the 2009-list
  cross-tab above they are not kibbutz, moshav or moshav shitufi, so they are not in the table; I did not research
  them and make no claim about any of them. Name (CBS year, code meaning): Kefar Silver (1957, institutional), Hodayot
  (1959, institutional), Kerem Beyavne (1963, institutional), Yedida (1964, institutional), Metal (1976,
  institutional), Ganne Hadar (1958, other rural), Hever (1958, other rural), Kerem Ben Shemen (1958, other rural),
  Mabbu'im (1958, other rural), Omen (1958, other rural), Tushiyya (1958, other rural), Sarigim (Li-On) (1960, other
  rural), Gannot Hadar (1964, other rural), Nes Ammim (1964, other rural), Alumma (1965, other rural), Midreshet Ben
  Gurion (1965, other rural), Ezer (1966, other rural), Bat Hen (1967, other rural), Gizo (1968, other rural), Wardon
  (1968, other rural), Ye'af (1968, other rural), Newe Zohar (1972, other rural), Zohar (1973, other rural), Ahawa
  (1976, other rural), Ma'galim (1958, community), Mevo Modi'im (1964, community), Ashalim (1976, community).
  **[GAP]** whether any of these is a kibbutz or moshav in common usage was not looked at.

## What was not done

- **No contemporaneous source for any founding day.** I did not search or read the Jewish Telegraphic Agency archive
  for any row, nor Davar, Maariv, Hamashkif, the Official Gazette or Jewish Agency records. The only look at the JTA
  archive was a listing of its item titles for 15 to 19 August 1977 (65 items, via `https://www.jta.org/wp-
  json/wp/v2/archive?after=1977-08-15T00:00:00&before=1977-08-20T00:00:00&per_page=100`, run 2026-10-25): no title
  names Shilat or a new moshav; that is titles only, no bulletin text, and records nothing about the content.
  **[GAP]**
- **The day of going onto the land versus other steps** (a decision, a cornerstone, a Nahal outpost, a civilian
  settling) is stated only where a Wikipedia article or Gvati says so. Where neither does, the row says the date's
  meaning is not given. **[GAP]**
- **Wikipedia citations unopened.** The Hebrew articles cite Davar, Maariv, the Nahal publications and Walid Khalidi;
  none was opened.
- **Membership rests on the CBS year.** A kibbutz or moshav whose CBS year is outside 1957-1977 but whose first
  outpost or core group falls inside the window (or the reverse) is not in the table; I did not look for such cases
  except where a row's sources named one (Zimrat, Ammi'oz, Dovev). **[GAP]**

## Searches (every negative record)

All run 2026-10-25.

| Row | Query / method | Where | Result |
|---|---|---|---|
| S1 | Name of each of the 49 rows, nikud and punctuation stripped, against the name column of the 756 appendix rows parsed from https://benyehuda.org/read/14684 | Gvati appendix | 41 matched on the CBS spelling; 5 more matched on another spelling I chose by hand (Roglit for Newe Mikha'el, Nahal Kishor, Nahal Beit Rimon, Ne'ot HaKikkar with a doubled kaf, Zar'it); 3 not matched: Ammi'oz, En Hazeva, Zofar. |
| S2 | Page text of the same chapter, nikud stripped, patterns: עמיעוז, צופר, עין חצבה, עין-חצבה | Gvati page | "עמיעוז": 1 hit (narrative, 5717 list); "צופר": 1 hit (narrative, 1968 Nahal outpost, inside the word "בארצופר"); "עין חצבה" and the hyphenated form: 0 hits, but this search was defective (stripping vowel signs also stripped the maqaf); a rerun keeping the maqaf ("עין־חצבה", U+05BE) found 2 hits in the narrative: the 1970 line (see its row) and a mention of Idan. |
| S3 | `titles=<Hebrew name>` with `redirects=1` for each row, then by hand `titles=` with a disambiguator where the first page was wrong | he.wikipedia.org MediaWiki API | First fetch needed a hand-chosen title for 10 rows (a missing page, a page on another topic or an unclear one: Zimrat, Moran, Sufa, En Hazeva, Zofar, Zarit, Hazon, Kishor, Giv'at Shappira, Alumim) and a disambiguation page for Adderet; each was refetched under the title in the last table. No row is left without a Hebrew article; Kefar Rozenwald is covered by the article "זרעית". |
| S4 | Same, `langlinks` to English | en.wikipedia.org | 49 of 49 rows have an English article: 48 via the Hebrew language link (Kishor under the title Kishorit, which also describes the later community), Adderet by title (Aderet, Israel). |

## Sources and revisions

CBS localities file 2024; CBS intro (December 2025); CBS "Local authorities in Israel 2009" list 03; Gvati vol. 2 appendix. All opened 2026-10-25 at the URLs under "How it was done". Wikipedia revisions (current at the fetch; URL form `https://he.wikipedia.org/w/index.php?oldid=<n>`, `https://en.wikipedia.org/w/index.php?oldid=<n>`):

| # | Locality | Hebrew article (oldid) | English article (oldid) |
|---|---|---|---|
| 1 | Ammi'oz | עמיעוז (42987738) | Ami'oz (1375608038) |
| 2 | Or HaNer | אור הנר (44113777) | Or HaNer (1375608194) |
| 3 | Shoqeda | שוקדה (42996449) | Shokeda (1376017476) |
| 4 | Yesha | ישע (44112888) | Yesha, Israel (1376013713) |
| 5 | Zimrat | זמרת (מושב) (44027084) | Zimrat (1376019203) |
| 6 | Adamit | אדמית (43555341) | Adamit (1374527355) |
| 7 | Avi'ezer | אביעזר (43944557) | Aviezer (1374528100) |
| 8 | Giv'at Shappira | גבעת שפירא (מושב) (43302050) | Givat Shapira (1374530889) |
| 9 | Giv'at Yesha'yahu | גבעת ישעיהו (43518112) | Givat Yeshayahu (1374530919) |
| 10 | Newe Mikha'el | נווה מיכאל (44057811) | Neve Michael (1374770622) |
| 11 | Zafririm | צפרירים (44026900) | Tzafririm (1374537674) |
| 12 | Kefar Maymon | כפר מימון (43999867) | Kfar Maimon (1376016067) |
| 13 | Avivim | אביבים (43992981) | Avivim (1374898912) |
| 14 | En Hazeva | עין חצבה (מושב) (44057301) | Ein Hatzeva (1378223786) |
| 15 | Even Menahem | אבן מנחם (43759266) | Even Menachem (1374530036) |
| 16 | Ram-On | רם און (43786935) | Ram-On (1374536041) |
| 17 | Yodefat | יודפת (42326348) | Yodfat (1374538335) |
| 18 | Adderet | אדרת (מושב) (43518117) | Aderet, Israel (1374527434) |
| 19 | Almagor | אלמגור (43945990) | Almagor (1374527672) |
| 20 | Ne'ot HaKikkar | נאות הכיכר (43794281) | Neot HaKikar (1376033957) |
| 21 | Elot | אילות (44069090) | Eilot (1376332467) |
| 22 | Ma'ale Gilboa | מעלה גלבוע (43417875) | Ma'ale Gilboa (1374533439) |
| 23 | Dovev | דוב"ב (43987009) | Dovev (1374529336) |
| 24 | Gerofit | גרופית (43890259) | Grofit (1376330363) |
| 25 | Me Ammi | מי עמי (43692372) | Mei Ami (1374533963) |
| 26 | Hazeva | חצבה (43846012) | Hatzeva (1376047531) |
| 27 | Alumim | עלומים (קיבוץ) (43556197) | Alumim (1377921791) |
| 28 | Netu'a | נטועה (42329582) | Netu'a (1374535050) |
| 29 | Kefar Rozenwald(Zarit) | זרעית (43794329) | Zar'it (1374538404) |
| 30 | Senir | שניר (44069098) | Snir (1374892695) |
| 31 | Hazon | חזון (מושב) (43124802) | Hazon, Israel (1379065682) |
| 32 | Ohad | אוהד (43944558) | Ohad, Israel (1376006172) |
| 33 | Shetula | שתולה (43555028) | Shtula (1374537202) |
| 34 | Qetura | קטורה (43643549) | Ketura, Israel (1376330090) |
| 35 | Talme Eliyyahu | תלמי אליהו (43669567) | Talmei Eliyahu (1376009501) |
| 36 | Zofar | צופר (מושב) (43827181) | Tzofar (1376049650) |
| 37 | Zur Natan | צור נתן (43747599) | Tzur Natan (1374537814) |
| 38 | Paran | פארן (43790177) | Paran, Israel (1376050298) |
| 39 | Sede Nizzan | שדה ניצן (42987756) | Sde Nitzan (1376007792) |
| 40 | Sufa | סופה (קיבוץ) (43488608) | Sufa, Israel (1376009255) |
| 41 | Ya'ad | יעד (43438445) | Ya'ad, Israel (1374537950) |
| 42 | Kishor | כישור (קיבוץ) (42319145) | Kishorit (1374533067) |
| 43 | Magen Sha'ul | מגן שאול (42326915) | Magen Shaul (1374533602) |
| 44 | Samar | סמר (43920907) | Samar, Israel (1377164420) |
| 45 | Yahel | יהל (43799991) | Yahel (1376328542) |
| 46 | Bet Rimmon | בית רימון (43685462) | Beit Rimon (1374528763) |
| 47 | Kefar Rut | כפר רות (43968102) | Kfar Ruth (1380204185) |
| 48 | Moran | מורן (קיבוץ) (43761740) | Moran, Israel (1374534639) |
| 49 | Shilat | שילת (43725343) | Shilat (1380467739) |
