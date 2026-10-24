# Immigration, emigration and population, 1977-1993

Researched 23 October 2026. Every URL below not marked "(opened 24 Oct)" was opened on 23 October. Research note for issue #147 (1977-93, the
research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is drafted
for the game, and no file in `content/` is changed. The `aliyah.yaml` rows, #102's emigration rows and the calibration
checkpoints stay paused.

Gaps revisited 24 October 2026, after the owner's "Research part open" comment on #147 of that day. Every source added
on 24 October was opened that day and is marked "(opened 24 Oct)" in the Sources table. Of the five rows the owner
listed, two are closed (1c-bis, and the Ethiopia yearly arrivals series inside 2b), one is closed for the natural-increase half and open for the migration half
(5b), and two stay [GAP] with what was read this time (the start of Operation Moses in 2b, yearly emigration in 3a).
Where this pass added a derived series, the arithmetic is shown. An agent opened these sources; a person's check of them is still pending.

## Purpose

Fix sourced values for §2.5 and §5.4 of [the 1977-1993 design](../../docs/superpowers/specs/2026-10-10-era-1977-1993-design.md):
CBS arrivals by year and region for 1977-1993, emigration, the population checkpoints for the calibration (the design
names 1983 and 1993, *to verify*), and the dates of Operations Moses, Joshua and Solomon. Where sources differ, every
value is given and one is named. The design also asks how CBS classifies the Asian republics of the USSR; see 1a.

## Conventions

- [V] read this session in a primary source: a CBS table or publication, or Tolts's paper (all listed under Sources).
  Tables were read from the page image as well as from the text layer; where the two differ, the page image is what
  is reported.
- From 24 October the tag [V] is also used for what a contemporaneous newspaper or wire report (JTA bulletins of the
  day, the *Los Angeles Times* of 1985) says, and each such item is marked as press. It is a record of what was
  reported then, not a government count.
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
| CBS 1365 HE | CBS Publication 1365, Hebrew edition of the same publication (opened 24 Oct): Table D "Immigrants by religion, year of immigration and age", printed page 24 (PDF p. 16); definitions section 3, paragraph "Religion", printed page 22 (PDF p. 14) | https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/h_print.pdf |
| CBS SA 2010 T3.1 | *Statistical Abstract of Israel 2010*, Table 3.1 "Marriages, divorces, live births, deaths, natural increase, infant deaths and stillbirths, by religion", absolute numbers, printed page 171 (total population) and 172 (Jews) (PDF p. 1-2) (opened 24 Oct) | https://www.cbs.gov.il/he/publications/doclib/2010/3.%20shnatonvitalstatistics/st03_01.pdf |
| JVL country-year | Jewish Virtual Library, "Total Immigration to Israel by Select Country by Year (1948 - Present)", column Ethiopia; the page's source line reads "Israel Central Bureau of Statistics" (opened 24 Oct) | https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-country-per-year |
| JVL Ethiopia | Jewish Virtual Library, "Total Immigration from Ethiopia (1948 - Present)", same source line (opened 24 Oct) | https://www.jewishvirtuallibrary.org/total-immigration-from-ethiopia-1948-present |
| LAT 1985 | *Los Angeles Times*, "Operation Moses: Chronology of an Exodus", 8 July 1985 (opened 24 Oct) | https://www.latimes.com/archives/la-xpm-1985-07-08-mn-9858-story.html |
| JTA 1984-11-28 | JTA Daily News Bulletin, 28 November 1984, "Dulzing: Jewish Agency is Preparing for 'a Sudden Jump in Immigration'" (opened 24 Oct) | https://www.jta.org/archive/dulzing-jewish-agency-is-preparing-for-a-sudden-jump-in-immigration (bulletin PDF: http://pdfs.jta.org/1984/1984-11-28_220.pdf) |
| JTA 1985-01-04 | JTA, 4 January 1985, "Curtain Lifted on Ethiopian Jewry Rescue" (the archive truncates the title) (opened 24 Oct) | https://www.jta.org/archive/curtain-lifted-on-ethiopian-jewry-rescue-vast-majority-of-ethiopian-jews-are-now-in-israel-about-1 (PDF: http://pdfs.jta.org/1985/1985-01-04_002.pdf) |
| JTA 1985-01-07 | JTA, 7 January 1985, "Airlift of Ethiopian Jews Has Been Halted, Apparently Because of Its Premature Disclosure" (opened 24 Oct) | https://www.jta.org/archive/airlift-of-ethiopian-jews-has-been-halted-apparently-because-of-its-premature-disclosure (PDF: http://pdfs.jta.org/1985/1985-01-07_003.pdf) |
| JTA 1985-01-08 | JTA, 8 January 1985, "The Airlift Story" (opened 24 Oct) | https://www.jta.org/archive/the-airlift-story (PDF: http://pdfs.jta.org/1985/1985-01-08_004.pdf) |
| JTA 1985-03-26 | JTA, 26 March 1985, "U.S. Praised for Its Role in Weekend Airlift of Ethiopian Jews from Sudan" (opened 24 Oct) | https://www.jta.org/archive/u-s-praised-for-its-role-in-weekend-airlift-of-ethiopian-jews-from-sudan (PDF: http://pdfs.jta.org/1985/1985-03-26_058.pdf) |
| JTA 1998-06-24 | JTA, 24 June 1998, "Ethiopian Exodus: a Timeline" (opened 24 Oct) | https://www.jta.org/archive/ethiopian-exodus-a-timeline-3 |
| JTA 1991-05-28 | JTA, 28 May 1991, "Airlift of 14,000 Ethiopian Jews Accomplished in Just 40 Flights" (opened 24 Oct) | https://www.jta.org/archive/airlift-of-14000-ethiopian-jews-accomplished-in-just-40-flights (PDF: http://pdfs.jta.org/1991/1991-05-28_100.pdf) |
| JTA 1980-12-31 | JTA, 31 December 1980, "The Urgent Problem of Yordim" (opened 24 Oct) | https://www.jta.org/archive/the-urgent-problem-of-yordim (PDF: http://pdfs.jta.org/1980/1980-12-31_249.pdf) |
| JTA 1985-12-12 | JTA, 12 December 1985, "News Brief" on the CBS report to the Cabinet (opened 24 Oct) | https://www.jta.org/archive/three-hundred-and-thirty-thousand-jews-have-emigrated (PDF: http://pdfs.jta.org/1985/1985-12-12_231.pdf) |
| JTA 1988-11-21 | JTA, 21 November 1988, "Israel's Population at 4.46 Million; Arabs Still Growing Twice As Fast", on the *Statistical Abstract of Israel 1988* (opened 24 Oct) | https://www.jta.org/archive/israels-population-at-4-46-million-arabs-still-growing-twice-as-fast (PDF: http://pdfs.jta.org/1988/1988-11-21_212.pdf) |
| CBS SA 2012 T2.3 | *Statistical Abstract of Israel 2012*, Table 2.3 "Sources of population growth" (opened 24 Oct; periods only, none starting in 1977 or ending in 1993) | https://www.cbs.gov.il/he/publications/doclib/2012/2.%20shnatonpopulation/st02_03.pdf |
| CBS SA 2012 T4.13 | same volume, Table 4.13 "Israelis" (departures and returns of Israelis staying abroad continuously for one year or more, 2007-2010) (opened 24 Oct) | https://www.cbs.gov.il/he/publications/doclib/2012/4.%20shnatonimmigration/st04_13.pdf |
| CBS SA 2018 T4.10 | *Statistical Abstract of Israel 2018*, Table 4.10, the same title for 2013-2016 (opened 24 Oct) | https://www.cbs.gov.il/he/publications/doclib/2018/4.%20shnatonimmigration/st04_10.pdf |
| CBS SA 2012 ch. 4 list | *Statistical Abstract of Israel 2012*, chapter 4 "Immigration and emigration" table list (opened 24 Oct) | https://www.cbs.gov.il/he/publications/doclib/2012/4.%20shnatonimmigration/st04_00.pdf |
| CBS SA 2018 ch. 4 list | *Statistical Abstract of Israel 2018*, chapter 4 table list (opened 24 Oct) | https://www.cbs.gov.il/he/publications/doclib/2018/4.%20shnatonimmigration/st04_00.pdf |
| JPR Staetsky | Daniel Staetsky, *Israelis abroad: The transformation of the Jewish Diaspora?*, Jewish Policy Research, March 2025, 35 pages (opened 24 Oct) | https://www.jpr.org.uk/sites/default/files/attachments/Israelis%20abrod%20-%20transformation%20of%20the%20Jewish%20Diaspora_0.pdf |
| CIA Reading Room | "In Ethiopians' Exodus, Moses Was American" (a reprint of a Los Angeles Times piece of 30 March 1985; record CIA-RDP90-00965R000605190003-1) (tried, not readable: the site returned a maintenance page on 24 Oct) | https://www.cia.gov/readingroom/node/1868581 |
| JVL Operation Moses | Jewish Virtual Library, "Operation Moses" (tried, not readable: the text loads by script and the page returned no body on 24 Oct) | https://www.jewishvirtuallibrary.org/operation-moses |
| ISA catalogue | Israel State Archives, "The immigration of the Jews of Ethiopia to Israel, 1948-1985" (tried, not readable: HTTP 403 on 24 Oct) | https://catalog.archives.gov.il/site/en/publication/the-immigration-of-the-jews-of-ethiopia-to-israel-1948-1985/ |

Also opened, not usable for this era: *Statistical Abstract of Israel 2010*, Table 4.10, "Departures and returns of
Israelis staying abroad continuously for one year or more" (st04_10.pdf in the same folder as T4.2); it covers 2005-2008
only. Not read: the Statistical Abstracts of the 1980s and 1990s (no scan was found on the CBS site; searched again on
24 October, see 3a), CBS's own emigration studies, the sources Wikipedia cites for the Ethiopian operations (Kaplan and Salamon, the *New York Times*
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
- From 1985 the split by religion is not printed in the English edition. Note (5): "In 1985, the registration of
  religion at the time of immigration ceased." The table points to "Table D in the Introduction", which the English
  edition does not contain. **Closed on 24 October from the Hebrew edition of the same publication: see 1c-bis.**
- **A misprint on page 31**: the Jewish potential immigrants of 1979 are printed 4,715 (page image). Derived: Jews
  35,662 - Jewish immigrants 20,947 = 14,715, and grand-total potential 15,062 - other religions' potential 347 =
  14,715. Our reading: 4,715 should read 14,715.
- Sums for 1977-1984, derived: Jews 159,295; other religions 9,050. Note (5) also says that "Up to 1983" other
  religions include immigrants whose religion was not stated.
- Note (1), page 33: "Including immigrants and potential immigrants (as of 1969) who had an immigrant visa at arrival
  and tourists who changed their status to immigrants or potential immigrants. Excluding immigrating citizens."

### 1c-bis. The Jewish share of the immigrants, 1985-1993 (closed 24 Oct)

[V] CBS 1365 Hebrew edition, Table D, printed page 24 (PDF p. 16), thousands, read from the page image as well as the
text layer (they agree). The columns are total, Jews, other religions, not known, and "% Jews", which the page's footnote (2)
gives as the percentage of those whose religion is known (the column header's footnote (3) points to the definitions).
Footnote (1) sends to note (1) of Table 1, so the
population is the same as in 1c: immigrants and potential immigrants, tourists who changed status included, immigrating
citizens excluded.

| Year | Total | Jews | Other religions | Not known | % Jews, as printed |
|---|---|---|---|---|---|
| 1985 | 10.6 | 8.9 | 0.5 | 1.3 | 94.9 |
| 1986 | 9.5 | 8.0 | 0.5 | 1.1 | 94.3 |
| 1987 | 13.0 | 11.1 | 0.6 | 1.3 | 95.1 |
| 1988 | 13.0 | 11.3 | 0.6 | 1.2 | 95.3 |
| 1989 | 24.1 | 21.5 | 1.3 | 1.3 | 94.0 |
| 1990 | 199.5 | 184.6 | 7.6 | 7.3 | 96.0 |
| 1991 | 176.1 | 152.0 | 14.2 | 9.9 | 91.4 |
| 1992 | 77.1 | 60.6 | 10.4 | 6.0 | 85.3 |
| 1993 | 76.8 | 59.0 | 11.7 | 6.2 | 83.5 |

- Checks, derived: the totals equal 1a rounded to thousands in all nine years (for example 1990, 199,516 to 199.5;
  1993, 76,805 to 76.8). Jews + other religions + not known comes within 0.1 of the total in every year (1985:
  8.9 + 0.5 + 1.3 = 10.7 against 10.6; 1992: 60.6 + 10.4 + 6.0 = 77.0 against 77.1). The printed percentage matches
  Jews / (Jews + other religions) to within 0.3 points, which is the rounding of the thousands (1985: 8.9 / 9.4 =
  94.7 against 94.9; 1989: 21.5 / 22.8 = 94.3 against 94.0; 1993: 59.0 / 70.7 = 83.5, exact).
- Sums, derived: Jews 1985-1993, 8.9 + 8.0 + 11.1 + 11.3 + 21.5 + 184.6 + 152.0 + 60.6 + 59.0 = 517.0 thousand. All
  arrivals of the same years, 10.6 + 9.5 + 13.0 + 13.0 + 24.1 + 199.5 + 176.1 + 77.1 + 76.8 = 599.7 thousand, so the
  Jews are 517.0 / 599.7 = 86.2% of all arrivals. Other religions, 0.5 + 0.5 + 0.6 + 0.6 + 1.3 + 7.6 + 14.2 + 10.4 +
  11.7 = 47.4 thousand, so among those whose religion is known the share is 517.0 / (517.0 + 47.4) = 91.6%. Our
  reading: 91.6% answers "share of Jews among immigrants whose religion is known" for the whole period, 86.2% answers
  "Jews as a share of all arrivals"; the two differ because 1985-1993 leaves 35.6 thousand unknown (1.3 + 1.1 + 1.3 +
  1.2 + 1.3 + 7.3 + 9.9 + 6.0 + 6.2).
- What "not known" means (definitions section, "Religion", page 22, our translation): from 1985 the religion is
  recorded after the immigrant applies at the Interior Ministry's population registry office for an identity card
  (before 1985 it was recorded at entry), and until then the entry reads "not registered". Up to 1989 Table D put
  both the "not registered" and those registered "no religion" under "not known"; from 1990 those registered "no
  religion" are under "other religions" and "not known" holds only the not yet registered. That is a break in the
  series at 1989-1990: the "other religions" column of 1990-1993 holds people the earlier years left in "not known".
- Why the unknowns do not shrink (page 22, our translation): "Experience shows that the share of Jews among the
  immigrants changes to a small degree." Reasons given: some leave the country or die before their religion is
  registered, and children under 16 need no identity card and so stay unregistered.
- Together with 1c this gives the Jewish immigrants for the whole era: 1977-1984 from Table 1 (159,295, derived in 1c)
  and 1985-1993 from Table D (517.0 thousand, rounded to a hundred). The two tables share the population, but 1984 and
  1985 sit on different registration rules (religion at entry against religion at the registry), so the join is a
  break, not a continuous series.
- Still open in this row: the Jewish split by continent for any year (see 7).

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
  1985-1989 4,396; 1990-2001 45,102. CBS 1365 prints no yearly figure for Ethiopia for these years; the yearly series
  is under "Yearly arrivals from Ethiopia" below (24 Oct).
- [V] CBS SA 2010 T4.4, page 233, born in Ethiopia: 1972-1979 306; 1980-1989 16,965; 1990-1999 39,651.
- [V] 1a, all of Africa by last residence: 1984 8,885 (against 3,094 in 1983 and 2,318 in 1985); 1991 20,251 (against
  4,472 in 1990 and 4,075 in 1992). Our reading: the two Ethiopian airlifts are the spikes in the `africa` column, as
  the design's §5.4 assumes (Ethiopia is `africa`).
- [V] CBS 1365, page XVII: Operation Moses in 1984, "over 8,000 Ethiopian immigrants".


**Yearly arrivals from Ethiopia, 1977-1993 (closed 24 Oct, tagged [S])**

[S] JVL country-year, column Ethiopia, which the page credits to the CBS; the CBS yearly table itself was not found.
Last country of residence, persons:

| Year | Ethiopia | Africa, all (1a) | Ethiopia as % of Africa, derived |
|---|---|---|---|
| 1977 | 90 | 1,620 | 5.6% |
| 1978 | 37 | 1,683 | 2.2% |
| 1979 | 45 | 1,340 | 3.4% |
| 1980 | 259 | 1,007 | 25.7% |
| 1981 | 650 | 1,170 | 55.6% |
| 1982 | 950 | 1,555 | 61.1% |
| 1983 | 2,393 | 3,094 | 77.3% |
| 1984 | 8,327 | 8,885 | 93.7% |
| 1985 | 1,886 | 2,318 | 81.4% |
| 1986 | 236 | 982 | 24.0% |
| 1987 | 231 | 1,205 | 19.2% |
| 1988 | 595 | 1,334 | 44.6% |
| 1989 | 1,448 | 1,861 | 77.8% |
| 1990 | 4,121 | 4,472 | 92.2% |
| 1991 | 20,014 | 20,251 | 98.8% |
| 1992 | 3,648 | 4,075 | 89.5% |
| 1993 | 863 | 1,431 | 60.3% |
| **1977-1993, derived** | **45,793** | **58,283** | **78.6%** |

- [C] The two JVL pages disagree on 1984 and 1985. The country-year page prints 8,327 for 1984 and 1,886 for 1985; the
  Ethiopia page prints 1,886 for 1984 and 8,327 for 1985. Kept: 8,327 for 1984 and 1,886 for 1985, because of the
  checks below, and because CBS 1365 (page XVII) places Operation Moses in 1984.
  The Ethiopia page's own overview line ("Up to Operation Moses (1948-1984) 6,720", "Operation Moses (1985) ~ 7,500")
  fits its yearly table, that is 1984 = 1,886, so that page contradicts CBS 1365 Table 2; this note follows CBS.
- [V] Checks, derived, against the four Ethiopia cells of CBS 1365 Table 2 (page 34): 1970-1979, 13 + 7 + 40 + 41 + 24 +
  19 + 10 + 90 + 37 + 45 = 326, exact; 1980-1984, 259 + 650 + 950 + 2,393 + 8,327 = 12,579, exact; 1985-1989, 1,886 +
  236 + 231 + 595 + 1,448 = 4,396, exact; 1990-2001, 4,121 + 20,014 + 3,648 + 863 + 1,197 + 1,312 + 1,411 + 1,661 +
  3,110 + 2,290 + 2,201 + 3,274 = 45,102, exact. Swapping 1984 and 1985 would break the 1980-1984 and 1985-1989 sums,
  so the series is anchored to CBS on all four periods, though each year inside a period is [S].
- Every yearly value is at or below the `africa` total of 1a for the same year (1984, 8,327 of 8,885; 1991, 20,014 of
  20,251), as it must be.
- Two readings, both ours. 1984 holds 8,327 although Operation Moses ran from late November into January: the
  arrivals of the first days of January 1985 are in 1985's 1,886 together with the later Operation Joshua arrivals.
  CBS 1365's own text says "over 8,000" for the operation, so the year column and the text are the same order.
  Second, 1991's 20,014 is the Operation Solomon year; 14,325 or 14,087 of them (see below) arrived in the two days of
  Solomon, so about 5,700 to 5,900 arrived in the other eleven months (20,014 - 14,325 = 5,689; 20,014 - 14,087 =
  5,927).

**Operation Moses**

| Item | Value | Source |
|---|---|---|
| Start | 21 November 1984 | [S] wiki Moses, "Beginning November 21, 1984" (no citation on the sentence) |
| Start | 18 November 1984 | [S] WJC: "On 18 November 1984, the State of Israel launched Operation Moses, a seven-week covert operation" |
| Start | "Mid-1984" | [S] JA |
| First flight | 20 November 1984, "The first flight under Operation Moses leaves Khartoum airport, bound for Europe and then Israel." | [V] LAT 1985, a chronology compiled in July 1985 and printed 8 July 1985 (a 1985 press chronology, not a government record) |
| End | 5 January 1985 | [S] wiki Moses ("ending January 5, 1985"); [S] WJC ("The operation came to a halt on 5 January 1985, when news of the activity leaked to the international media") |
| End | 6 January 1985 | [V] LAT 1985 ("Jan. 4, 1985--The Israeli government announces that it has been receiving Ethiopian Jews through a secret airlift from Sudan. Two days later, Sudanese officials halt the flights"); [V] JTA 1985-03-26 ("Operation Moses was halted January 6 due to premature disclosure of its details") |
| End | last flight left Brussels "at midnight Saturday" | [V] JTA 1985-01-08 ("The last flight, before TEA withdrew from the operation yesterday, left Brussels at midnight Saturday"); the item is datelined Brussels, Monday 7 January 1985 (printed in the bulletin of Tuesday 8 January), so the Saturday is 5 January and TEA's "yesterday" is Sunday 6 January, which also backs the 6 January halt in the row above |
| End | "November 1985" (the leak) | [S] JA: "News of the rescue leaked out to the foreign media in November 1985" |
| Number | about 10,000, 35 flights | [V] JTA 1985-01-04 (Levinsky, Jewish Agency: "about 10,000 Ethiopian Jews arrived in Israel recently"); JTA 1985-01-07 ("There were 35 flights since last November, by a Belgian charter airline") |
| Number | 40 flights, some 10,000 | [V] JTA 1985-01-08 ("TEA made 40 flights in that period, bringing some 10,000 Ethiopian Jewish refugees from Khartoum, Sudan to Tel Aviv") |
| Number | 6,000 to 8,000 | [S] JTA 1998-06-24, a retrospective timeline, which also dates the operation to "December 1984" |
| Number | about 8,000 | [V] JTA 1985-03-26 ("Some 8,000 Jews were reportedly brought to Israel during that operation") |
| Number | about 8,000 | [S] wiki Moses ("some 8,000"); [S] WJC ("approximately 8,000 ... aboard 30 flights"); [S] JA ("8,000 Jews were flown from Khartoum"); [V] CBS 1365 page XVII ("over 8,000") |
| Why it stopped | Peres's press conference confirming the airlift | [S] wiki Moses, cited there to a source Wikipedia itself tags as needing a better one |

- [C] The end date: two sources say 5 January 1985; JA's "November 1985" cannot be right, since the follow-up airlift
  it describes next is dated March 1985 by Wikipedia (below; JA itself gives it no date). Two contemporaneous JTA and
  LAT items add 6 January for the halt and Saturday 5 January for the last flight out of Brussels. Our reading: the
  last plane left around midnight between 5 and 6 January and the halt is dated by the day after. Preferred, as
  before: **5 January 1985** for the end of the flights, with 6 January as the halt.
- [C] The start: 18 November (WJC), 20 November (LAT 1985, the first flight leaving Khartoum) or 21 November 1984
  (Wikipedia). Our reading, not a source statement: a plane that left Khartoum on the 20th and stopped over in Brussels
  at about midnight (JTA 1985-01-08 describes the midnight stopovers) would reach Tel Aviv on the 21st, which would
  make the 20th and the 21st the same flight counted at two ends. The 18th stays unexplained. [GAP] for a government
  or archive record of the first flight: (24 Oct) Searched again and not found. Read: LAT 1985; JTA 1984-11-28 (Dulzin
  says only that "the Jewish Agency is preparing for a sudden jump in immigration", no flight date); JTA 1985-01-04,
  01-07, 01-08 and 1985-03-26; JTA 1998-06-24, which gives "December 1984". Tried and not readable: the CIA Reading
  Room item "In Ethiopians' Exodus, Moses Was American" (the site returned a maintenance page), the Jewish Virtual
  Library "Operation Moses" page (the text loads by script and returned no body), and the Israel State Archives
  catalogue entry for "The immigration of the Jews of Ethiopia to Israel 1948-1985" (HTTP 403).
- The number: about 8,000, and CBS's own "over 8,000" agrees; the contemporaneous JTA reports range from about 8,000
  to about 10,000. It fits inside CBS's 12,579 Ethiopian arrivals of 1980-1984 and inside the 8,327 of 1984 above.

**Operation Joshua**

- [S] wiki Joshua: "On March 22, 1985" (no citation on the sentence); "six United States Air Force C-130 Hercules
  transport aircraft" (no citation); the number is given three ways: "Around 500" (cited to an IDF blog post), "more
  than 500" (cited to the *Los Angeles Times*, 26 March 1985) and "around 650" (cited to the *Jewish Telegraph*). The
  article says Vice President George H. W. Bush arranged it (cited to the IDF blog).
- [S] wiki Moses: "approximately 500 of whom were evacuated later in the U.S.-led Operation Joshua" (cited there to
  Kaplan and Salamon, 2004).
- [S] JA, undated: "After mediation by the US, Numeiri allowed six American Hercules planes to airlift the last
  remaining Ethiopian Jews in Sudan".
- [V] JTA 1985-03-26: a "three-day evacuation" "over the weekend" before Monday 25 March (the item's dateline; printed in the bulletin of Tuesday 26 March), with the operation, reported
  from the LAT, beginning "at dawn last Friday" (22 March 1985); "there are no precise figures"; "about 900 Ethiopian
  Jews lived as refugees in Sudan for about a year". LAT 1985's chronology: "March 22, 1985--Under CIA direction, the
  last of the Ethiopian Jews in Sudan are airlifted to Israel."
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
- [C] JTA 1991-05-28, an item datelined Tel Aviv, 27 May 1991, three days after the airlift began: "14,087 Ethiopian Jews, comprising over 3,000 families,
  were airlifted from Addis Ababa to Israel over the weekend within some 30 hours", "accomplished in a mere 40 flights",
  "The first flight reportedly arrived in Israel around 6 p.m. Friday local time" (Friday 24 May 1991). That is a third
  count (14,087, against 14,325 and 14,000), 30 hours against 36 and 48, and 40 flights against 35 aircraft. The
  contemporaneous 14,087 is the figure nearest the event; the 14,325 is the one Wikipedia cites to the *New York Times*
  of 26 May, which was not opened. Preferred for event text: **24-25 May 1991, 14,087 (JTA) or 14,325 (NYT as cited)**,
  to be settled from a primary count.
- Check, derived: the 14,325 fit inside CBS's 1991 `africa` total of 20,251 (1a), and so do the 14,087 and the
  JVL country-year figure for Ethiopia, 20,014.

---

## 3. Emigration

### 3a. Yearly emigration

No CBS-labelled yearly emigration series for 1977-1993 was reachable. [GAP] (24 Oct) Searched again and not found.
Read: CBS SA 2010 T4.10 (starts 2005), CBS SA 2012 Table 4.13 (departures and returns for 2007-2010, in thousands,
by age and sex) and CBS SA 2018 Table 4.10 (2013-2016); CBS SA 2012 and 2018 chapter 4 table lists (thirteen and twelve
tables, none a yearly series before 1990); the JPR report by Staetsky, *Israelis abroad* (March 2025), whose CBS
series starts in 1990 and which lists the CBS emigration papers of the period without reproducing them (Rabi 1978,
Lamdany 1983, Paltiel 1986 and 1990); JTA daily bulletins found by search. The Statistical Abstracts of the period are
not on the CBS site. The 1957-77 note's source (Bachi) ends in 1975. What was found instead:

- [V] (contemporaneous press, reporting the CBS) JTA 1980-12-31: the director of the Central Bureau of Statistics "acknowledged, however, that a
  record. 30,000 Israelis left the country permanently this year compared to fewer than 7000 in each of the two
  preceding years" (as printed, with its typographical slips). One year, 1980, against 1978 and 1979; the article does
  not say whether gross or net; no series.
- [V] (same kind) JTA 1988-11-21, on the *Statistical Abstract of Israel 1988*: "The number of Israelis living abroad
  for more than one year increased by 9,200 in 1987, compared with 13,900 in 1986. An additional 1,000 persons were
  deducted from the population to account for the difference between those departing and returning among potential
  immigrants and others." Two years, a net increase of the stock abroad, not gross departures.
- [V] (same kind) JTA 1985-12-12, on a CBS report to the Cabinet: "Three hundred and thirty thousand Jews have emigrated from
  Israel since the establishment of the State", "a further 120,000 non-Jews had left during this period". A cumulative
  figure to 1985; no year.
- These three measure different things (permanent departures in 1980, not stated whether gross or net; the net increase of those abroad for a year or more in
  1986-87, a cumulative count), so they are not combined here. They are the only CBS emigration numbers found for
  1977-1993 outside the period balances of 3b.

### 3a-bis. A derived yearly net outflow (not a CBS series)

[V] inputs, derived output. Immigrants are CBS SA 2010 T4.2 (1a). "Change minus natural" is the year's change in the
population (4a) less the natural increase (5b), both CBS; it is what the population gained from immigration, less
what it lost to emigration, plus corrections. The last column is then immigrants less that residual:
the net number of residents the population lost to departures and corrections in the year. Thousands.

| Year | Immigrants, T4.2 | Total population: change minus natural | Immigrants minus that residual |
|---|---|---|---|
| 1977 | 21,429 | +7.4 | 14.0 |
| 1978 | 26,394 | +17.0 | 9.4 |
| 1979 | 37,222 | +30.6 | 6.6 |
| 1980 | 20,428 | +17.5 | 2.9 |
| 1981 | 12,599 | -11.2 | 23.8 |
| 1982 | 13,723 | +17.0 | -3.3 |
| 1983 | 16,906 | +13.9 | 3.0 |
| 1984 | 19,981 | +10.4 | 9.6 |
| 1985 | 10,642 | -4.8 | 15.4 |
| 1986 | 9,505 | -4.8 | 14.3 |
| 1987 | 12,965 | +5.4 | 7.6 |
| 1988 | 13,034 | -1.0 | 14.0 |
| 1989 | 24,050 | +10.6 | 13.5 |
| 1990 | 199,516 | +187.5 | 12.0 |
| 1991 | 176,100 | +162.6 | 13.5 |
| 1992 | 77,057 | +60.4 | 16.7 |
| 1993 | 76,805 | +52.4 | 24.4 |

- Reading the column: it understates net departures by the number of immigrating citizens, who are not in T4.2 (T2.3,
  note 2; the 1983-1989 gap between T2.3 and T4.2 is 12.5 thousand, 1.8 a year, derived in 3b). It also carries the
  1983 census correction and any religion-registry corrections, and it is the net of departures and returns, not gross
  departures.
- Break at 1983: the 1983 row uses the previous-census population of both years (4,148.5 - 4,063.6 = 84.9); the 1984
  row starts from the census-based 1983 population (4,199.7 - 4,118.6 = 81.1). The census correction itself (-29.9
  thousand, 4a) is left out of both rows.
- 1982 is not usable: the Arab population of the Golan was added that year (T2.3 note 9, 4a), so its residual of
  17.0 and the -3.3 are inflated by roughly the 11 to 14 thousand of 4a.
- 1981 (23.8) and 1993 (24.4) are the two high values that no source read explains. They are shown as derived, not
  as findings.
- Cross-check against the press figures above, derived, same order and not equal: 1986, 14.3 (plus about 1.8 of
  immigrating citizens, 16.1) against 13.9 + 1.0 = 14.9; 1987, 7.6 (9.4) against 9.2 + 1.0 = 10.2. The CBS stock figure
  counts only those abroad a year or more, so it is not the same quantity.
- Not comparable to the 30,000 of 1980: the article does not say whether that is gross or net, and this column is a net (1980: 2.9).

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
- Yearly sources of growth, 24 Oct: the natural-increase half is closed from CBS SA 2010 T3.1 (5b); the migration balance
  as CBS labels it is still [GAP] by year: (24 Oct) Searched again and not found. Read: T2.3 in the 2010 and 2012
  Abstracts (periods only, none starting in 1977 or ending in 1993), T3.1, T4.2, T4.10 and the other tables listed in 3a.
  The derived residual in 5b stands in for it and is marked as derived.

### 5b. Yearly natural increase (CBS) and the derived residual

[V] CBS SA 2010 T3.1, printed pages 171 (total population) and 172 (Jews), absolute numbers. For every year 1975-1994
the printed live births less deaths equals the printed natural increase (checked cell by cell for both groups, 40
rows). The population change is from 4a (thousands). "Change minus natural" is derived and is the migration balance
plus corrections; it is not a CBS figure.

| Year | Natural increase, Jews | Jewish population change | Change minus natural, Jews | Natural increase, total | Total population change | Change minus natural, total |
|---|---|---|---|---|---|---|
| 1977 | 49,815 | +56.9 | +7.1 | 70,364 | +77.8 | +7.4 |
| 1978 | 47,028 | +63.9 | +16.9 | 67,449 | +84.4 | +17.0 |
| 1979 | 47,001 | +77.2 | +30.2 | 68,010 | +98.6 | +30.6 |
| 1980 | 47,853 | +64.3 | +16.4 | 67,957 | +85.5 | +17.5 |
| 1981 | 47,347 | +37.6 | -9.7 | 67,223 | +56.0 | -11.2 |
| 1982 | 48,001 | +52.9 | +4.9 | 68,915 | +85.9 | +17.0 |
| 1983 | 50,011 | +62.9 | +12.9 | 70,993 | +84.9 | +13.9 |
| 1984 | 49,421 | +59.2 | +9.8 | 70,673 | +81.1 | +10.4 |
| 1985 | 49,921 | +45.5 | -4.4 | 71,283 | +66.5 | -4.8 |
| 1986 | 48,425 | +44.2 | -4.2 | 69,926 | +65.1 | -4.8 |
| 1987 | 47,264 | +51.5 | +4.2 | 69,778 | +75.2 | +5.4 |
| 1988 | 47,408 | +46.1 | -1.3 | 71,278 | +70.3 | -1.0 |
| 1989 | 46,862 | +58.1 | +11.2 | 72,157 | +82.8 | +10.6 |
| 1990 | 48,092 | +229.6 | +181.5 | 74,615 | +262.1 | +187.5 |
| 1991 | 47,027 | +197.9 | +150.9 | 74,459 | +237.1 | +162.6 |
| 1992 | 48,210 | +97.9 | +49.7 | 76,735 | +137.1 | +60.4 |
| 1993 | 49,149 | +92.7 | +43.6 | 79,303 | +131.7 | +52.4 |

- T3.1 notes (page 177): note 1, "Excl. soldiers killed in war"; note 5, "Incl. births and deaths in East Jerusalem as of
  1970, and in the Golan as of 1982 ..."; note 6, "Incl. religion unknown". The total-population column has the Golan
  Arab addition in 1982 inside its population change (4a), so the 1982 residual is inflated; the Jewish column has no
  such break.
- 1983 and 1984: as in 3a-bis. The 1983 row uses the previous-census populations, the 1984 row starts from the census
  base, and the census correction (Jews -23.6, total -29.9 thousand, 4a) is outside both.
- [C] The yearly natural increase does not add up to the period natural increase of T2.3 (5): Jews 1983-1989, T3.1 sum
  339,312 against 339.7 thousand; Jews 1990-1995, 290,746 against 291.7; Jews 1972-1982, 532,730 against 532.5; total
  1983-1989, 496,088 against 494.8; total 1990-1995, 467,658 against 465.9; total 1972-1982, 753,044 against 752.7.
  The gaps are 0.2 to 1.8 thousand (0.4, 1.0 and 0.2 for Jews; 1.3, 1.8 and 0.3 for the total). Neither table's notes
  explain it. Both values are kept; preferred: T3.1 for a yearly value, T2.3 for a period total.
- Cross-check, derived: the Jewish residual of 1983-1989 sums to 12.9 + 9.8 - 4.4 - 4.2 + 4.2 - 1.3 + 11.2 = 28.2
  thousand, against the T2.3 migration balance for Jews 1983-1989 of 27.7 (5).
- The yearly Jewish residual is 4.9 to 30.2 thousand in 1977-1980 and 1982-1984, below zero in 1981 (-9.7), 1985 (-4.4),
  1986 (-4.2) and 1988 (-1.3), and 181.5, 150.9, 49.7 and 43.6 in 1990-1993. Those sign changes are the years in which
  the net of immigration and emigration was a loss, on this derivation.

---

## 6. Calibration notes for the content work

Not a model, only the arithmetic the sources allow:

- Checkpoints (4a): end of 1977, 3,077.3 thousand Jews (1972 census basis); end of 1983, 3,412.5 (1983 census basis;
  3,436.1 on the previous basis); end of 1993, 4,335.2 (1983 census basis). 1993 is not a census year (4b).
- Arrivals 1977-1993 (1a): 768,356, of which 529,478 in 1990-1993 (derived from 1a: 199,516 + 176,100 + 77,057 +
  76,805). The rows include non-Jews; the Jewish count is printed in CBS 1365 Table 1 to 1984 and, from the Hebrew edition's Table D, for 1985-1993 (1c-bis).
- Emigration: no CBS-labelled yearly figure (3a); a derived yearly net outflow is in 3a-bis. The derived Jewish figure in 3b (net outflow less the religion balance) is about
  12 thousand a year in 1972-1989 and about 20 thousand a year in 1990-1995, period averages only.
- The 1983 census break: -23.6 thousand Jews (4a, 5).

## 7. Gaps

Closed on 24 October: the Jewish share of the immigrants for 1985-1993 (1c-bis); yearly arrivals from Ethiopia (2b,
tagged [S] with an exact reconciliation to CBS); the yearly natural increase (5b).

Still open:
- A CBS-labelled yearly emigration series for any year of 1977-1993 (3a); the derived net outflow in 3a-bis and the
  three press-reported figures stand in.
- The yearly migration balance as CBS labels it (5b); the derived residual stands in.
- The Jewish split by continent for any year.
- Yearly immigrants from the USSR in CBS (only periods; Tolts gives yearly FSU emigrants to Israel, 2a); the CBS
  birth-table placement of Asian-republic Soviet immigrants before 1995.
- A government or archive record of the first flight of Operation Moses (18, 20 or 21 November 1984, 2b), and primary
  counts for Moses, Joshua and Solomon; this pass added contemporaneous press only.
- The 1983 census day and its count against the running estimate.
