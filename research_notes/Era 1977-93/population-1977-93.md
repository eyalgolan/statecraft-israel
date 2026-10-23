# Immigration, emigration and population, 1977-1993

Researched 23 October 2026. Every URL below was opened on that date. Research note for issue #147 (1977-93, the
research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is drafted
for the game, and no file in `content/` is changed. The `aliyah.yaml` rows, #102's emigration rows and the calibration
checkpoints stay paused.

## Purpose

Fix sourced values for §2.5 and §5.4 of [the 1977-1993 design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md):
CBS arrivals by year and region for 1977-1993, emigration, the population checkpoints for the calibration (the design
names 1983 and 1993, *to verify*), and the dates of Operations Moses, Joshua and Solomon. Where sources differ, every
value is given and one is named. The design also asks how CBS classifies the Asian republics of the USSR; see 1a.

## Conventions

- [V] read this session in a primary source: a CBS table or publication, or Tolts's paper (all listed under Sources).
  Tables were read from the page image as well as from the text layer; where the two differ, the page image is what
  is reported.
- [S] secondary only (Wikipedia, the Jewish Agency's history page, the World Jewish Congress). Usable as a lead, to be
  confirmed before encoding.
- [C] two sources disagree; every value is given, and the preferred one is named where the sources allow it.
- [GAP] not found in any source read. No value is filled in by inference.
- **Derived** marks a number this note computed. The arithmetic is shown next to it.
- "Page" is the printed page number of the source; "PDF p." is the page in the PDF file when it differs.
- No quote is invented. The short quotes below are copied from the cited page. Translations from Hebrew are marked
  "our translation". Readings of a misprint or an unclear cell are marked "our reading". Dashes in quotes (Tolts's
  en dashes) are written here as hyphens.

## Sources

| Key | Source | Where read |
|---|---|---|
| CBS SA 2010 T4.2 | Central Bureau of Statistics, *Statistical Abstract of Israel 2010*, Table 4.2 "Immigrants, by period of immigration and last continent of residence", pages 230-231 (PDF p. 1-2); 1977-1993 are on page 231 | https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_02.pdf |
| CBS SA 2010 T4.4 | same volume, Table 4.4 "Immigrants, by period of immigration, country of birth and last country of residence", pages 233-234 (PDF p. 1-2) | https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_04.pdf |
| CBS SA 2010 T2.1 | same volume, Table 2.1 "Population, by population group", page 85 (PDF p. 1) for 1977-1993; notes on page 86 (PDF p. 2) | https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_01.pdf |
| CBS SA 2023 T2.1 | *Statistical Abstract of Israel 2023*, Table 2.1, the same title, page 1. The file `content/population.yaml` already cites it | https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf |
| CBS SA 2010 T2.3 | *Statistical Abstract of Israel 2010*, Table 2.3 "Sources of population growth", pages 89-91 (PDF p. 1-3) | https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_03.pdf |
| CBS 1365 | CBS Publication 1365, *Immigration to Israel 2002-2006* (2009): introduction pages XVII-XVIII (PDF p. 8-9); Table 1 "Immigrants, by year of immigration, immigration status and religion", pages 30-33 (PDF p. 21-24); Table 2 "Immigrants, by period and year of immigration, last continent and country of residence", page 34 (PDF p. 25); Table 4 "Immigrants, by year of immigration and continent of birth", pages 36-37 (PDF p. 27-28) | https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/e_print.pdf |
| CBS Statistilite 171 | CBS, *Israel at 70*, Statistilite 171 (2018), page 2 | https://www.cbs.gov.il/en/Statistical/stat171e.pdf |
| Tolts 2019 | Mark Tolts, "A Half Century of Jewish Emigration from the Former Soviet Union: Demographic Aspects", paper presented at the Davis Center, Harvard University, 20 November 2019: Table 1 and text, pages 2-3 | https://daviscenter.fas.harvard.edu/sites/default/files/files/2021-04/Tolts%20M.%20A%20Half%20Century%20of%20Jewish%20Emigration%20from%20the%20Former%20Soviet%20Union%20-%20Harvard4%20_0.pdf |
| wiki Moses | Wikipedia, "Operation Moses" (the design's §2.5 source) | https://en.wikipedia.org/wiki/Operation_Moses |
| wiki Joshua | Wikipedia, "Operation Joshua" | https://en.wikipedia.org/wiki/Operation_Joshua |
| wiki Solomon | Wikipedia, "Operation Solomon" (the design's §2.5 source) | https://en.wikipedia.org/wiki/Operation_Solomon |
| JA | The Jewish Agency, "Ethiopian Jewish Aliyot: Operation Moses [1984], Operation Solomon [1991]" (Internet Archive copy of 4 June 2019; the live page returns 404) | http://web.archive.org/web/20190604081933/http://www.jewishagency.org:80/historical-aliyah/content/28831 |
| WJC | World Jewish Congress, "This week in Jewish history: Operation Moses" (17 November 2020; the page credits the Israel Ministry of Foreign Affairs) | https://www.worldjewishcongress.org/en/news/this-week-in-jewish-history--operation-moses-israel-airlifts-thousands-of-ethiopian-jews-to-safety-11-2-2020 |

Also opened, not usable for this era: *Statistical Abstract of Israel 2010*, Table 4.10, "Departures and returns of
Israelis staying abroad continuously for one year or more" (st04_10.pdf in the same folder as T4.2); it covers 2005-2008
only. Not read: the Statistical Abstracts of the 1980s and 1990s (no scan was found on the CBS site), CBS's own
emigration studies, the sources Wikipedia cites for the Ethiopian operations (Kaplan and Salamon, the *New York Times*
and *Boston Globe* reports), and the IDF and Foreign Ministry pages on the operations. Nothing below is checked against
them.

---

## 1. Immigrants by year, 1977-1993

### 1a. By last continent of residence (the CBS original)

[V] CBS SA 2010 T4.2, page 231. Columns in the order of `content/aliyah.yaml` (`asia`, `africa`, `europe`,
`america_oceania`, `unknown`):

| Year | Total | Asia | Africa | Europe | America and Oceania | Not known |
|---|---|---|---|---|---|---|
| 1977 | 21,429 | 908 | 1,620 | 12,660 | 6,201 | 40 |
| 1978 | 26,394 | 1,736 | 1,683 | 16,549 | 6,305 | 121 |
| 1979 | 37,222 | 7,087 | 1,340 | 22,404 | 6,024 | 367 |
| 1980 | 20,428 | 3,202 | 1,007 | 11,792 | 4,350 | 77 |
| 1981 | 12,599 | 1,215 | 1,170 | 5,909 | 4,243 | 62 |
| 1982 | 13,723 | 951 | 1,555 | 6,168 | 5,003 | 46 |
| 1983 | 16,906 | 844 | 3,094 | 6,154 | 6,758 | 56 |
| 1984 | 19,981 | 700 | 8,885 | 5,485 | 4,876 | 35 |
| 1985 | 10,642 | 607 | 2,318 | 3,964 | 3,739 | 14 |
| 1986 | 9,505 | 1,183 | 982 | 3,675 | 3,634 | 31 |
| 1987 | 12,965 | 1,888 | 1,205 | 6,044 | 3,812 | 16 |
| 1988 | 13,034 | 1,700 | 1,334 | 6,012 | 3,969 | 19 |
| 1989 | 24,050 | 1,185 | 1,861 | 16,766 | 4,147 | 91 |
| 1990 | 199,516 | 940 | 4,472 | 189,650 | 4,315 | 139 |
| 1991 | 176,100 | 622 | 20,251 | 152,142 | 3,023 | 62 |
| 1992 | 77,057 | 891 | 4,075 | 68,962 | 3,006 | 123 |
| 1993 | 76,805 | 1,728 | 1,431 | 70,315 | 3,283 | 48 |
| **1977-1993, derived** | **768,356** | **27,387** | **58,283** | **604,651** | **76,688** | **1,347** |

- Checks, derived: each of the 17 rows sums to its total. The table's own subtotals for 1980-1984 (83,637) and
  1985-1989 (70,196) equal the sums of their years in every column. 1977-1979 sit in the subtotal 1975-1979 (124,827)
  and 1990-1993 in 1990-1994 (609,322); both also check with the 1975, 1976 and 1994 rows of the same page.
- The column sums in the last row are derived; they also add up: 27,387 + 58,283 + 604,651 + 76,688 + 1,347 =
  768,356.
- The yearly totals equal, year by year, the 17 totals in the design's §2.5 (taken there from JVL). The JVL figures
  are therefore the CBS totals.
- The table's two percentage columns ("Europe-America and Oceania" and "Asia-Africa", % of total) are shares of the
  immigrants whose continent is known (1977: (12,660 + 6,201) / (21,429 - 40) = 88.2%, as printed). **1982 is
  misprinted**: the page image prints 85.7 and 18.3, which add to 104.0. Derived: (6,168 + 5,003) / (13,723 - 46) =
  81.7% and (951 + 1,555) / 13,677 = 18.3%. Our reading: 85.7 should read 81.7. The absolute numbers are unaffected.
- Note 1 of the table: "Incl. potential immigrants and tourists who changed their status to immigrants. As of 1970
  excl. immigrating citizens".
- Note 2: "Until 1995 the Asian republics of the USSR (former) were included in Europe; as of 1996 they are included in
  Asia." This answers the design's *to verify* for this table: every Soviet arrival of 1977-1993, from the Asian
  republics too, is in `europe`. The small Asia column of 1990-1993 (940, 622, 891, 1,728) confirms it.
- [C] CBS 1365 Table 2, page 34, note 2, classifies by last residence differently: "As of 1990, including immigrants
  from the Asian republics of USSR (former)" in Asia, and its 1990-2001 column has 168,581 from the Asian republics
  inside Asia's 180,086. It gives no yearly split for 1990-1993, so it cannot be used for the rows. Preferred for the
  rows: T4.2, which keeps one classification for the whole era.
- Note 4 (on the "Not known" column): from 1970 non-Jews "are included according to last continent of residence", so
  every row here includes immigrants of other religions (see 1c).

### 1b. The same years by continent of birth

[V] CBS 1365 Table 4, pages 36 (1977-1984) and 37 (1985-1993). CBS publishes birth in three columns only (Asia,
Africa, Europe-America):

| Year | Total | Asia | Africa | Europe-America | Not known |
|---|---|---|---|---|---|
| 1977 | 21,429 | 1,027 | 2,219 | 18,176 | 7 |
| 1978 | 26,394 | 1,879 | 2,292 | 22,210 | 13 |
| 1979 | 37,222 | 7,195 | 2,141 | 27,560 | 326 |
| 1980 | 20,428 | 3,322 | 1,686 | 15,320 | 100 |
| 1981 | 12,599 | 1,286 | 1,764 | 9,483 | 66 |
| 1982 | 13,723 | 1,123 | 2,331 | 10,236 | 33 |
| 1983 | 16,906 | 1,012 | 4,039 | 11,810 | 45 |
| 1984 | 19,981 | 812 | 9,536 | 9,578 | 55 |
| 1985 | 10,642 | 692 | 2,721 | 7,225 | 4 |
| 1986 | 9,505 | 1,250 | 1,353 | 6,900 | ".." |
| 1987 | 12,965 | 1,927 | 1,494 | 9,518 | 26 |
| 1988 | 13,034 | 1,747 | 1,600 | 9,651 | 36 |
| 1989 | 24,050 | 1,262 | 2,140 | 20,546 | 102 |
| 1990 | 199,516 | 1,058 | 4,758 | 193,583 | 117 |
| 1991 | 176,100 | 766 | 20,538 | 154,726 | 70 |
| 1992 | 77,057 | 1,048 | 4,473 | 71,426 | 110 |
| 1993 | 76,805 | 1,931 | 1,837 | 73,006 | 31 |

- The totals equal 1a in every year. The rows sum to the total except 1986, where "Not known" is printed ".." and
  the three continents fall 2 short: 1,250 + 1,353 + 6,900 = 9,503 (against 9,505). Our reading: the ".." hides 2
  people.
- Notes 2 and 3 of the table: the Asian republics of the USSR are in Asia, and the European republics and "USSR
  (former)" with the republic not known in Europe-America, "As of 1995". Before 1995 the table does not say where
  Soviet-born immigrants with a known Asian republic were put. [GAP]
- Residence and birth are different measures and differ by continent. For 1977-1993, derived: Africa 58,283 by residence against 66,922 by
  birth; Asia 27,387 against 29,337; Europe plus America and Oceania 604,651 + 76,688 = 681,339 against 670,954.
  `content/aliyah.yaml` uses the residence split (see the 1957-77 sibling note, 1b), so 1a is the consistent one for
  the rows. Preferred: 1a.

### 1c. Jews and non-Jews, immigrants and potential immigrants

[V] CBS 1365 Table 1, pages 30 and 32 (grand total and Jews), 31 and 33 (other religions and the Jewish potential
immigrants).

| Year | Grand total | Immigrants | Potential immigrants | Jews | Other religions |
|---|---|---|---|---|---|
| 1977 | 21,429 | 12,503 | 8,926 | 20,538 | 891 |
| 1978 | 26,394 | 16,439 | 9,955 | 25,276 | 1,118 |
| 1979 | 37,222 | 22,160 | 15,062 | 35,662 | 1,560 |
| 1980 | 20,428 | 11,788 | 8,640 | 19,332 | 1,096 |
| 1981 | 12,599 | 5,861 | 6,738 | 11,454 | 1,145 |
| 1982 | 13,723 | 6,030 | 7,693 | 12,519 | 1,204 |
| 1983 | 16,906 | 7,227 | 9,679 | 15,738 | 1,168 |
| 1984 | 19,981 | 13,044 | 6,937 | 18,776 | 868 |
| 1985 | 10,642 | 5,467 | 5,175 | ".." | ".." |
| 1986 | 9,505 | 3,942 | 5,563 | ".." | ".." |
| 1987 | 12,965 | 7,155 | 5,810 | ".." | ".." |
| 1988 | 13,034 | 7,599 | 5,435 | ".." | ".." |
| 1989 | 24,050 | 18,960 | 5,090 | ".." | ".." |
| 1990 | 199,516 | 194,941 | 4,575 | ".." | ".." |
| 1991 | 176,100 | 172,932 | 3,168 | ".." | ".." |
| 1992 | 77,057 | 76,432 | 625 | ".." | ".." |
| 1993 | 76,805 | 76,274 | 531 | ".." | ".." |

- Checks, derived: immigrants + potential immigrants = grand total in every year, and the grand totals equal 1a. Sums
  for 1977-1993: 658,754 + 109,602 = 768,356. For 1977-1983, Jews + other religions = grand total; each column also
  splits into immigrants and potential immigrants that add up (for example 1980: Jews 10,955 + 8,377 = 19,332; other
  religions 833 + 263 = 1,096).
- 1984: Jews 18,776 + other religions 868 = 19,644, 337 short of 19,981. Note (5), page 33: "In 1984, excl. 337
  immigrants whose religion was not stated or was not known, but are included in the total."
- From 1985 the split by religion is not printed. Note (5): "In 1985, the registration of religion at the time of
  immigration ceased." So **the Jewish share of the immigrants is [GAP] for 1985-1993** in the tables read; the
  table points to "Table D in the Introduction" of the same publication, which is not in the English edition.
- **A misprint on page 31**: the Jewish potential immigrants of 1979 are printed 4,715 (page image). Derived: Jews
  35,662 - Jewish immigrants 20,947 = 14,715, and grand-total potential 15,062 - other religions' potential 347 =
  14,715. Our reading: 4,715 should read 14,715.
- Sums for 1977-1984, derived: Jews 159,295; other religions 9,050. Note (5) also says that "Up to 1983" other
  religions include immigrants whose religion was not stated.
- Note (1), page 33: "Including immigrants and potential immigrants (as of 1969) who had an immigrant visa at arrival
  and tourists who changed their status to immigrants or potential immigrants. Excluding immigrating citizens."

### 1d. CBS's own description of the period

[V] CBS 1365, pages XVII-XVIII, short quotes, each checked against 1a:
- "For 15 years - from 1975 to 1989 - the average number of immigrants per year was less than 20,000." Derived: 1a's
  subtotals 124,827 + 83,637 + 70,196 = 278,660, or 18,577 a year.
- "In 1986, immigration reached an all-time low: this was the only year in the nation's history when the number of
  immigrants was even lower than 10,000." 1a: 9,505.
- "in 1978 and 1979 over 60,000 immigrants arrived in the country, a large proportion of whom were from the former
  USSR". Derived: 26,394 + 37,222 = 63,616.
- "1984 witnessed the first major operation that brought to Israel immigrants from Ethiopia ("Operation Moses"): over
  8,000 Ethiopian immigrants arrived in the country in that operation."
- "Altogether, close to 280,000 immigrants arrived in Israel over those 15 years. The vast majority came from Eastern
  Europe, and only one-fifth were from Asia and Africa." Derived from the T4.2 subtotals, Asia + Africa by residence
  for 1975-1989: (11,793 + 6,029) + (6,912 + 15,711) + (6,563 + 7,700) = 54,708, or 19.6% of 278,660.
- "The low period in immigration to Israel ended suddenly during the second half of 1989, with the fall of the Iron
  Curtain." And: "In 1990, there were close to 200,000 immigrants, and in 1991 there were over 175,000." 1a: 199,516
  and 176,100.
- [C] The same paragraph says that in 1990-2001 "906,500 immigrants arrived in Israel. The vast majority (86%)
  arrived from countries that were formerly part of the USSR, and approximately 40,000 arrived from Ethiopia." The
  publication's own Table 2 (page 34) gives 1,060,091 for 1990-2001. Derived: its former-USSR rows, 739,614 + 168,581 =
  908,195, are 85.7% of 1,060,091, so the 86% fits the table's total and the 906,500 does not. Our reading: 906,500 is
  an error in the text. Preferred: the table. The span runs past 1993, so this touches the era only as context.

---

## 2. Named waves

### 2a. The Soviet aliyah

[V] Tolts 2019, Table 1, page 2, "Emigration of Jews and their relatives from the FSU to three main destinations,
thousands":

| Year | 1977 | 1978 | 1979 | 1980 | 1981 | 1982 | 1983 | 1984 | 1985 | 1986 | 1987 | 1988 | 1989 | 1990 | 1991 | 1992 | 1993 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Israel | 8.3 | 12.0 | 17.3 | 7.4 | 1.8 | 0.7 | 0.4 | 0.3 | 0.35 | 0.2 | 2.1 | 2.2 | 12.9 | 185.2 | 147.8 | 65.1 | 66.1 |
| USA (note a) | 8.5 | 16.9 | 34.1 | 14.1 | 7.7 | 2.0 | 0.9 | 0.55 | 0.8 | 0.7 | 6.1 | 16.8 | 56 (b) | 6.5 (b) | 35.2 | 45.9 | 35.9 |
| Germany | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | 0.6 | 8.5 | 8.0 | 4.0 | 16.6 |

- Table notes: "a Data for 1970-1988 include all destinations other than Israel for those who emigrated with Israeli
  visas; since 1991 annual data cover only those immigrants who were assisted by the Hebrew Immigrant Aid Society
  (HIAS)." "b Departures from the Soviet Union."
- Derived from the Israel row: 1977-1988, 53.05 thousand; 1989-1993, 12.9 + 185.2 + 147.8 + 65.1 + 66.1 = 477.1
  thousand; 1977-1993, 530.15 thousand.
- Tolts, page 3: "This emigration peaked in 1990-1991 ... when about 400,000 Soviet Jews and their relatives left
  the USSR. Of these, 333,000 (83%) went to Israel." (quote shortened at the dots). Check, derived: 185.2 + 147.8 =
  333.0.
- Tolts, page 3: "By 1979 only one-third of these emigrants were arriving in Israel." Derived for 1979: 17.3 / (17.3 +
  34.1) = 33.7%.
- Cross-check against CBS by residence, [V] CBS 1365 Table 2, page 34, "USSR (former)": 1980-1984 10,888 and
  1985-1989 17,875. Note 3 limits that row to the European republics and an unknown republic only "As of 1990", and
  the Asian-republics row is empty before 1990, so these two cells are the whole USSR. Tolts, derived: 7.4 + 1.8 +
  0.7 + 0.4 + 0.3 = 10.6 thousand and 0.35 + 0.2 + 2.1 + 2.2 + 12.9 = 17.75 thousand. Within 300 and 130 of CBS.
- [V] CBS SA 2010 T4.4, page 233, immigrants born in "USSR (former)" European republics and not stated: 1972-1979
  137,134; 1980-1989 29,754; 1990-1999 772,239. Asian republics (a separate row, filled only from 1990-1999): 49,524.
  The periods do not match the era's bounds, so these are context only.
- [C] The design's §2.5 quotes the Wikipedia article on the 1990s aliyah for 1990 185,227, 1992 65,093 and 1993
  66,145. They agree with Tolts's 185.2, 65.1 and 66.1 thousand to his rounding; our reading: they count FSU emigrants to Israel, as Tolts does; CBS's
  `europe` column (189,650, 68,962, 70,315) also holds every other European country. Preferred, as the design says
  (§5.4): the CBS series for the rows; Tolts for any event text on the Soviet wave.

### 2b. Ethiopia: Operations Moses, Joshua and Solomon

CBS does not date the operations. What it counts:
- [V] CBS 1365 Table 2, page 34, immigrants whose last residence was Ethiopia: 1970-1979 326; **1980-1984 12,579**;
  1985-1989 4,396; 1990-2001 45,102. No yearly figure for Ethiopia is printed for these years. [GAP]
- [V] CBS SA 2010 T4.4, page 233, born in Ethiopia: 1972-1979 306; 1980-1989 16,965; 1990-1999 39,651.
- [V] 1a, all of Africa by last residence: 1984 8,885 (against 3,094 in 1983 and 2,318 in 1985); 1991 20,251 (against
  4,472 in 1990 and 4,075 in 1992). Our reading: the two Ethiopian airlifts are the spikes in the `africa` column, as
  the design's §5.4 assumes (Ethiopia is `africa`).
- [V] CBS 1365, page XVII: Operation Moses in 1984, "over 8,000 Ethiopian immigrants".

**Operation Moses**

| Item | Value | Source |
|---|---|---|
| Start | 21 November 1984 | [S] wiki Moses, "Beginning November 21, 1984" (no citation on the sentence) |
| Start | 18 November 1984 | [S] WJC: "On 18 November 1984, the State of Israel launched Operation Moses, a seven-week covert operation" |
| Start | "Mid-1984" | [S] JA |
| End | 5 January 1985 | [S] wiki Moses ("ending January 5, 1985"); [S] WJC ("The operation came to a halt on 5 January 1985, when news of the activity leaked to the international media") |
| End | "November 1985" (the leak) | [S] JA: "News of the rescue leaked out to the foreign media in November 1985" |
| Number | about 8,000 | [S] wiki Moses ("some 8,000"); [S] WJC ("approximately 8,000 ... aboard 30 flights"); [S] JA ("8,000 Jews were flown from Khartoum"); [V] CBS 1365 page XVII ("over 8,000") |
| Why it stopped | Peres's press conference confirming the airlift | [S] wiki Moses, cited there to a source Wikipedia itself tags as needing a better one |

- [C] The end date: two sources say 5 January 1985; JA's "November 1985" cannot be right, since the follow-up airlift
  it describes next is dated March 1985 by Wikipedia (below; JA itself gives it no date). Preferred: **5 January 1985**.
- [C] The start: 18 or 21 November 1984, neither from a primary source. Not settled here. [GAP] for a primary source.
- The number: about 8,000, and CBS's own "over 8,000" agrees. It fits inside CBS's 12,579 Ethiopian arrivals of
  1980-1984.

**Operation Joshua**

- [S] wiki Joshua: "On March 22, 1985" (no citation on the sentence); "six United States Air Force C-130 Hercules
  transport aircraft" (no citation); the number is given three ways: "Around 500" (cited to an IDF blog post), "more
  than 500" (cited to the *Los Angeles Times*, 26 March 1985) and "around 650" (cited to the *Jewish Telegraph*). The
  article says Vice President George H. W. Bush arranged it (cited to the IDF blog).
- [S] wiki Moses: "approximately 500 of whom were evacuated later in the U.S.-led Operation Joshua" (cited there to
  Kaplan and Salamon, 2004).
- [S] JA, undated: "After mediation by the US, Numeiri allowed six American Hercules planes to airlift the last
  remaining Ethiopian Jews in Sudan".
- [C] Preferred for event text: **22 March 1985, about 500**, marked [S] until a primary source is read. The 650 is a
  single newspaper figure.

**Operation Solomon**

| Item | Value | Source |
|---|---|---|
| Dates | 24-25 May 1991 | [S] wiki Solomon ("from May 24 to 25, 1991", cited there to the Jewish Virtual Library) |
| Date | 24 May 1991 | [S] JA ("on May 24 1991, over the Shavuot festival") |
| Number | 14,325 | [S] wiki Solomon ("transported 14,325 Ethiopian Jews to Israel in 36 hours", cited there to the *New York Times*, 26 May 1991) |
| Number | 14,000 | [S] JA ("14,000 people were flown overnight to Israel") |
| Duration | 36 hours | [S] wiki Solomon |
| Duration | 48 hours | [S] JA ("a procedure which took all of 48 hours") |
| Aircraft | 35, IAF C-130s and El Al 747s | [S] wiki Solomon |
| Payment | $35 million, "raised by the greater Jewish community" | [S] wiki Solomon (cited there to the *Boston Globe*, 30 June 1991) |
| Payment | "forty million US dollars" | [S] JA |

- [C] Preferred for event text, all [S]: **24-25 May 1991, 14,325 people in 36 hours, $35 million**. The exact
  count and the 36 hours come with a contemporary newspaper citation on Wikipedia; JA's 14,000 is a round number, and
  its 48 hours and $40 million carry no source.
- Check, derived: the 14,325 fit inside CBS's 1991 `africa` total of 20,251 (1a).

---

## 3. Emigration

### 3a. Yearly emigration

No yearly series for 1977-1993 was reachable. [GAP] CBS's departures-and-returns table in the 2010 Abstract (T4.10)
starts in 2005; the Statistical Abstracts of the period are not on the CBS site; and the 1957-77 note's source (Bachi)
ends in 1975.

### 3b. What CBS's sources of growth imply

[V] CBS SA 2010 T2.3, pages 89 (total population) and 90 (Jews), thousands. The table gives the migration balance and,
inside it, the immigrants. The difference is **derived**, not a CBS emigration figure:

| Period | Group | Immigrants (incl. immigrating citizens) | Migration balance | Immigrants minus balance, derived |
|---|---|---|---|---|
| 1972-1982 | Jews | 311.9 | 178.6 | 133.3 |
| 1983-1989 | Jews | 114.5 | 27.7 | 86.8 |
| 1990-1995 | Jews | 659.8 | 540.7 | 119.1 |
| 1972-1982 | Total population | 324.9 | 183.5 | 141.4 |
| 1983-1989 | Total population | 119.6 | 31.1 | 88.5 |
| 1990-1995 | Total population | 702.5 | 593.5 | 109.0 |

- What the difference holds, from the table's notes (page 91): note 2, the immigrants column is "Here also incl. immigrating
  citizens"; note 1, "Until 1995, the balance of migration also included the balance of changes and corrections of
  religion." So the derived column is residents leaving net of residents returning, minus the group's net gain from changes and corrections of religion (for the Jewish rows; the table does not
  separate it). Our reading: it is the closest CBS-based net emigration figure for these years,
  but it is not labelled as emigration anywhere, and none of the three periods matches the era's bounds (the first
  starts in 1972, the last ends in 1995).
- Cross-check, derived, that the immigrants here are larger than T4.2 by the immigrating citizens, as note 2 says:
  1983-1989, T4.2 16,906 + 19,981 + 70,196 = 107,083 against 119.6 thousand (12.5 more); 1990-1995, T4.2 609,322 +
  1995's 76,361 (CBS 1365 Table 1, page 32) = 685,683 against 702.5 thousand (16.8 more); 1972-1982, 142,753 + 124,827
  + 20,428 + 12,599 + 13,723 = 314,330 against 324.9 thousand (10.6 more). The 1972-1974 subtotal is T4.2 page 230,
  as in the 1957-77 note.
- Per year, derived (only as a scale for #102, not a series): Jews 133.3 / 11 = 12.1 thousand a year in 1972-1982,
  86.8 / 7 = 12.4 in 1983-1989, 119.1 / 6 = 19.9 in 1990-1995.

---

## 4. Population checkpoints

### 4a. End-of-year population, 1976-1993

[V] CBS SA 2010 T2.1, page 85, thousands. CBS SA 2023 T2.1 (page 1) carries the same values for every one of these
years, both 1983 rows included (checked cell by cell, total, Jews and Arabs, end of year and average). Note 1 of the
table (page 86): "Until 1995, before publication of the Census results "Arabs" also included "others"."

| End of year | Grand total | Jews | Arabs (incl. others) | Jewish change on the year, derived |
|---|---|---|---|---|
| 1976 | 3,575.4 | 3,020.4 | 554.9 | |
| 1977 | 3,653.2 | 3,077.3 | 575.9 | +56.9 |
| 1978 | 3,737.6 | 3,141.2 | 596.3 | +63.9 |
| 1979 | 3,836.2 | 3,218.4 | 617.8 | +77.2 |
| 1980 | 3,921.7 | 3,282.7 | 638.9 | +64.3 |
| 1981 | 3,977.7 | 3,320.3 | 657.5 | +37.6 |
| 1982 | 4,063.6 | 3,373.2 | 690.4 | +52.9 |
| 1983 (previous census basis) | 4,148.5 | 3,436.1 | 712.5 | +62.9 |
| **1983 (1983 census basis)** | **4,118.6** | **3,412.5** | **706.1** | |
| 1984 | 4,199.7 | 3,471.7 | 727.9 | +59.2 |
| 1985 | 4,266.2 | 3,517.2 | 749.0 | +45.5 |
| 1986 | 4,331.3 | 3,561.4 | 769.9 | +44.2 |
| 1987 | 4,406.5 | 3,612.9 | 793.6 | +51.5 |
| 1988 | 4,476.8 | 3,659.0 | 817.7 | +46.1 |
| 1989 | 4,559.6 | 3,717.1 | 842.5 | +58.1 |
| 1990 | 4,821.7 | 3,946.7 | 875.0 | +229.6 |
| 1991 | 5,058.8 | 4,144.6 | 914.2 | +197.9 |
| 1992 | 5,195.9 | 4,242.5 | 953.4 | +97.9 |
| **1993** | **5,327.6** | **4,335.2** | **992.5** | +92.7 |

- The table prints two rows for 1983. Its notes (page 86): "3. Based on the previous Census." and "4. Based on the Census of
  that year." These are different measures, not a [C]. Preferred: the census-based row (bold), which is the base of every later year. For a chained run
  started before the census, the previous-census row is the like-for-like one. Derived differences, census basis
  minus previous basis: Jews -23.6 thousand, Arabs -6.4, total -29.9. The 1984 change is taken from the census row.
- Checks, derived: Jews + Arabs = grand total in every row to within 0.1 thousand of rounding (0.1 off in 1976, 1978,
  1980, 1981, the previous-basis 1983 row, 1984, 1988 and 1993).
- The Arab column rises 32.9 thousand in 1982 against 18.6 to 21.5 in each of 1978-1981 (derived). [V] CBS SA 2010
  T2.3 note 9 (page 91): "Excl. the addition of the Arab residents from East Jerusalem in 1967 and from the Golan Sub-District
  in 1982." Our reading: the 1982 jump includes the Golan addition on top of ordinary growth; the excess over 1978-1981,
  derived 32.9 - 21.5 = 11.4 to 32.9 - 18.6 = 14.3 thousand, is the order of its size. The Jewish column has no such break.
- Jewish population, end of 1977 to end of 1993, derived: 4,335.2 - 3,077.3 = 1,257.9 thousand, with the 1983 census
  break (-23.6) inside. 1990 and 1991 together, derived: 229.6 + 197.9 = 427.5 thousand.
- For `content/population.yaml` (which counts Jews, design §5.4), the values convert as the file already does (for
  example 3,412.5 thousand to 3412500). Not written here.

### 4b. The census years

- [V] CBS Statistilite 171, page 2: "Since the first census, five additional censuses have been held (in 1961, 1972,
  1983, 1995, and 2008)." So **1983 is a census year and 1993 is not**; the design's *to verify* is answered. The
  end-1993 figure is an estimate on the 1983 census basis ([V] CBS SA 2010 T2.3, note 11, "Based on 1983 Census",
  on the end-1995 figure of the 1990-1995 period); the next census basis is 1995's.
- The 1983 census day and its raw count against the running estimate (the analogue of Bachi's table for 1961 and
  1972 in the 1957-77 note) are [GAP]: no source read gives them.

---

## 5. Sources of growth

[V] CBS SA 2010 T2.3, pages 89 (total population) and 90 (Jews), thousands:

| Period | Group | Population at beginning | Natural increase | Migration balance | Thereof: immigrants | Total growth | Population at end | % annual growth | Migration balance, % of growth |
|---|---|---|---|---|---|---|---|---|---|
| 1972-1982 | Jews | 2,662.0 | 532.5 | 178.6 | 311.9 | 711.0 | 3,373.2 (note 10) | 2.2 | 25.1 |
| 1983-1989 | Jews | 3,349.6 (note 8) | 339.7 | 27.7 | 114.5 | 367.4 | 3,717.1 | 1.5 | 7.5 |
| 1990-1995 | Jews | 3,717.1 | 291.7 | 540.7 | 659.8 | 832.4 | 4,549.5 (note 11) | 3.4 | 65.0 |
| 1972-1982 | Total | 3,115.6 (note 8) | 752.7 | 183.5 | 324.9 | 936.2 (note 9) | 4,063.6 | 2.4 (note 9) | 19.6 |
| 1983-1989 | Total | 4,033.7 (note 8) | 494.8 | 31.1 | 119.6 | 525.9 | 4,559.6 | 1.8 | 5.9 |
| 1990-1995 | Total | 4,559.6 | 465.9 | 593.5 | 702.5 | 1,059.4 | 5,619.0 | 3.5 | 56.0 |

- The notes (page 91): 8, "Discontinuity is due to census results"; 9, "Excl. the addition of the Arab residents from East
  Jerusalem in 1967 and from the Golan Sub-District in 1982"; 10, "Incl. census adjustments. The Arab population
  includes the addition of the Arab population in East Jerusalem and in the Golan"; 11, "Based on 1983 Census".
- Checks, derived: natural + migration = total growth in every row, except Jews 1972-1982 (532.5 + 178.6 = 711.1,
  0.1 over). Beginning + growth = end: Jews 1983-1989 3,717.0 (0.1 under) and 1990-1995 exact; total 1983-1989 and
  1990-1995 exact. Jews 1972-1982: 2,662.0 + 711.0 = 3,373.0 against 3,373.2 (note 10). Total 1972-1982: 3,115.6 +
  936.2 = 4,051.8 against 4,063.6, 11.8 apart, which note 9 puts down to the Golan addition being left out of growth.
- The 1983-1989 rows begin on the 1983 census basis: Jews 3,349.6 against 3,373.2 at the end of 1982 in T2.1,
  derived -23.6, the same break as the two 1983 rows of 4a; total 4,033.7 against 4,063.6, -29.9.
- Yearly sources of growth for 1977-1993 are [GAP]: the 2010 table gives long periods only, and none starts in 1977
  or ends in 1993.

---

## 6. Calibration notes for the content work

Not a model, only the arithmetic the sources allow:

- Checkpoints (4a): end of 1977, 3,077.3 thousand Jews (1972 census basis); end of 1983, 3,412.5 (1983 census basis;
  3,436.1 on the previous basis); end of 1993, 4,335.2 (1983 census basis). 1993 is not a census year (4b).
- Arrivals 1977-1993 (1a): 768,356, of which 529,478 in 1990-1993 (derived from 1a: 199,516 + 176,100 + 77,057 +
  76,805). The rows include non-Jews; the Jewish count is printed only to 1984 (1c).
- Emigration: no yearly figure (3a). The derived Jewish figure in 3b (net outflow less the religion balance) is about
  12 thousand a year in 1972-1989 and about 20 thousand a year in 1990-1995, period averages only.
- The 1983 census break: -23.6 thousand Jews (4a, 5).

## 7. Gaps

- Yearly emigration for any year of 1977-1993; a labelled CBS emigration estimate for the period.
- Jewish immigrants for 1985-1993 (religion not registered from 1985; Table D of CBS 1365 not in the English edition), and the Jewish
  split by continent for any year.
- Yearly immigrants from Ethiopia and from the USSR in CBS (only periods); the CBS birth-table placement of Asian-
  republic Soviet immigrants before 1995.
- A primary source for the dates and numbers of Operations Moses, Joshua and Solomon; the start of Moses (18 or 21
  November 1984) is unsettled.
- The 1983 census day and its count against the running estimate.
- Yearly sources of growth (natural increase, migration balance) for 1977-1993.
