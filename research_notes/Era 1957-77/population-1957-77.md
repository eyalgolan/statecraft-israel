# Immigration, emigration and population, 1957-1977

Researched 23 October 2026. Every URL below was opened on that date. Research note for issue #115 (1957-77 3, the
research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is drafted
for the game, and no file in `content/` is changed. The `aliyah.yaml` rows, #102's emigration rows and the calibration
checkpoints stay paused.

## Purpose

Fix sourced values for §2.4 and §5.4 of [the 1957-1977 design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md):
CBS arrivals by year and region for 1957-1977, the emigration rows, and the population checkpoints for the calibration
(1961, 1972, 1977). Where sources differ, every value is given and one is named; the design asks for this for the
Soviet aliyah total and for Operation Yachin.

## Conventions

- [V] read this session in a primary source: a CBS table, Bachi's CICRED monograph, or Tolts's paper (all listed
  under Sources). Tables were read from the page image as well as from the text layer; where the two differ, the
  page image is what is reported.
- [S] secondary only (Wikipedia). Usable as a lead, to be confirmed before encoding.
- [C] two sources disagree; every value is given, and the preferred one is named.
- [GAP] not found in any source read. No value is filled in by inference.
- **Derived** marks a number this note computed. The arithmetic is shown next to it.
- "Page" is the printed page number of the source; "PDF p." is the page in the PDF file when it differs.
- No quote is invented. The short quotes below are copied from the cited page. Translations from Hebrew are marked
  "our translation". Readings of a misprint or an unclear cell are marked "our reading". Dashes in quotes (Bachi's
  year ranges, Tolts's en dashes) are written here as hyphens.

## Sources

| Key | Source | Where read |
|---|---|---|
| CBS SA 2010 T4.2 | Central Bureau of Statistics, *Statistical Abstract of Israel 2010*, Table 4.2 "Immigrants, by period of immigration and last continent of residence", pages 230-231 (PDF p. 1-2) | https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_02.pdf |
| CBS SA 2010 T4.4 | same volume, Table 4.4 "Immigrants, by period of immigration, country of birth and last country of residence", pages 233-234 (PDF p. 1-2) | https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_04.pdf |
| CBS SA 2010 T2.1 | same volume, Table 2.1 "Population, by population group", pages 85-86 (PDF p. 1-2) | https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_01.pdf |
| CBS SA 2023 T2.1 | *Statistical Abstract of Israel 2023*, Table 2.1, the same title, page 1 (published 12.09.2023). The file `content/population.yaml` already cites | https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf |
| CBS SA 2010 T2.3 | *Statistical Abstract of Israel 2010*, Table 2.3 "Sources of population growth", pages 89-91 (PDF p. 1-3) | https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_03.pdf |
| CBS 1365 | CBS Publication 1365, *Immigration to Israel 2002-2006* (2009): introduction pages XVI-XVII (PDF p. 7-8); Table 1 "Immigrants, by year of immigration, immigration status and religion", pages 30-32 (PDF p. 21-23); Table 4 "Immigrants, by year of immigration and continent of birth", page 36 (PDF p. 27) | https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/e_print.pdf |
| CBS Statistilite 171 | CBS, *Israel at 70*, Statistilite 171 (2018), page 2, "About the CBS" | https://www.cbs.gov.il/en/Statistical/stat171e.pdf |
| Bachi | Roberto Bachi, *The Population of Israel* (C.I.C.R.E.D. series, World Population Year 1974; the text runs to 1975 and cites work of 1977): §8 (page 92), §9.1 (page 116), Table 9.1 (page 119), Table 9.10 (page 138, printed sideways), Appendix 7.1 note 2 (page 404). PDF p. = page + 19 | https://web.archive.org/web/2015/http://www.cicred.org/Eng/Publications/pdf/c-c26.pdf |
| Tolts 2019 | Mark Tolts, "A Half Century of Jewish Emigration from the Former Soviet Union: Demographic Aspects", paper presented at the Davis Center, Harvard University, 20 November 2019: Table 1 and text, page 2 | https://daviscenter.fas.harvard.edu/sites/default/files/files/2021-04/Tolts%20M.%20A%20Half%20Century%20of%20Jewish%20Emigration%20from%20the%20Former%20Soviet%20Union%20-%20Harvard4%20_0.pdf |
| wiki Soviet | Wikipedia, "1970s Soviet Union aliyah" (the design's §2.4 source) | https://en.wikipedia.org/wiki/1970s_Soviet_Union_aliyah |
| wiki Yachin | Wikipedia, "Operation Yachin" (the design's §2.4 source) | https://en.wikipedia.org/wiki/Operation_Yachin |

Not read: Moshe Sicron's CBS monographs (no copy was reachable), the Statistical Abstracts of the 1960s and 1970s
(no scan was found on the CBS site, so the yearly tables of those editions are not used) and Sabatello's emigration
study, which Bachi cites for his revised estimates. Nothing below is checked against them.

---

## 1. Immigrants by year, 1957-1977

### 1a. By last continent of residence (the CBS original)

[V] CBS SA 2010 T4.2, pages 230 (1957-1974) and 231 (1975-1977). Columns in the order of `content/aliyah.yaml`
(`asia`, `africa`, `europe`, `america_oceania`, `unknown`):

| Year | Total | Asia | Africa | Europe | America and Oceania | Not known |
|---|---|---|---|---|---|---|
| 1957 | 72,634 | 4,230 | 25,747 | 39,812 | 1,410 | 1,435 |
| 1958 | 27,290 | 7,921 | 4,113 | 13,695 | 1,320 | 241 |
| 1959 | 23,988 | 3,544 | 4,429 | 14,731 | 1,147 | 137 |
| 1960 | 24,692 | 1,782 | 5,379 | 16,169 | 1,158 | 204 |
| 1961 | 47,735 | 4,149 | 18,048 | 23,375 | 1,969 | 194 |
| 1962 | 61,533 | 5,355 | 41,816 | 11,825 | 2,187 | 350 |
| 1963 | 64,489 | 4,964 | 38,672 | 14,213 | 6,497 | 143 |
| 1964 | 55,036 | 5,057 | 17,340 | 28,124 | 4,188 | 327 |
| 1965 | 31,115 | 5,223 | 8,535 | 13,879 | 3,096 | 382 |
| 1966 | 15,957 | 3,137 | 3,024 | 7,435 | 2,132 | 229 |
| 1967 | 14,469 | 1,987 | 6,268 | 4,295 | 1,771 | 148 |
| 1968 | 20,703 | 4,671 | 7,567 | 6,029 | 2,275 | 161 |
| 1969 | 38,111 | 7,018 | 5,926 | 15,236 | 9,601 | 330 |
| 1970 | 36,750 | 6,904 | 3,785 | 14,434 | 11,405 | 222 |
| 1971 | 41,930 | 5,778 | 2,354 | 20,888 | 12,885 | 25 |
| 1972 | 55,888 | 3,143 | 2,766 | 39,145 | 10,814 | 20 |
| 1973 | 54,886 | 2,025 | 2,839 | 40,492 | 9,522 | 8 |
| 1974 | 31,979 | 1,177 | 1,216 | 23,126 | 6,439 | 21 |
| 1975 | 20,028 | 927 | 689 | 13,417 | 4,989 | 6 |
| 1976 | 19,754 | 1,135 | 697 | 12,137 | 5,774 | 11 |
| 1977 | 21,429 | 908 | 1,620 | 12,660 | 6,201 | 40 |
| **1957-1977, derived** | **780,396** | **81,035** | **202,830** | **385,117** | **106,780** | **4,634** |

- Checks, derived: each of the 21 rows sums to its total. The table's own subtotals for 1958-1960 (75,970),
  1961-1964 (228,793), 1965-1968 (82,244), 1969-1971 (116,791) and 1972-1974 (142,753) equal the sums of their
  years in every column. 1957 sits in the subtotal 1955-1957 and 1975-1977 in 1975-1979, so those two subtotals do
  not check the 1957-77 rows alone.
- The column sums in the last row are derived; they also add up: 81,035 + 202,830 + 385,117 + 106,780 + 4,634 =
  780,396.
- The yearly totals equal, year by year, the 21 totals in the design's §2.4 (taken there from JVL). The JVL figures
  are therefore the CBS totals.
- Note 1 of the table: "Incl. potential immigrants and tourists who changed their status to immigrants. As of 1970
  excl. immigrating citizens".
- Note 2: "Until 1995 the Asian republics of the USSR (former) were included in Europe". The Soviet arrivals of the
  1970s are therefore all in `europe`, as the design's §5.4 assumes.
- Note 4 (on the "Not known" column): "From 15 V 1948 till end 1969, incl. non-Jews; as of 1970 they are included
  according to last continent of residence." See 1c for what this means for the rows.

### 1b. The same years by continent of birth

[V] CBS 1365 Table 4, page 36. CBS publishes birth in three columns only (Asia, Africa, Europe-America):

| Year | Total | Asia | Africa | Europe-America | Not known |
|---|---|---|---|---|---|
| 1957 | 72,634 | 5,223 | 24,413 | 40,984 | 2,014 |
| 1958 | 27,290 | 8,039 | 4,064 | 14,977 | 210 |
| 1959 | 23,988 | 3,622 | 4,414 | 15,855 | 97 |
| 1960 | 24,692 | 1,890 | 5,340 | 17,274 | 188 |
| 1961 | 47,735 | 4,253 | 18,163 | 25,215 | 104 |
| 1962 | 61,533 | 5,415 | 42,036 | 13,874 | 208 |
| 1963 | 64,489 | 5,073 | 38,834 | 20,453 | 129 |
| 1964 | 55,036 | 5,158 | 17,638 | 31,920 | 320 |
| 1965 | 31,115 | 5,351 | 8,915 | 16,470 | 379 |
| 1966 | 15,957 | 3,263 | 3,387 | 9,080 | 227 |
| 1967 | 14,469 | 2,089 | 6,758 | 5,480 | 142 |
| 1968 | 20,703 | 4,828 | 9,217 | 6,496 | 162 |
| 1969 | 38,111 | 7,376 | 9,187 | 21,059 | 489 |
| 1970 | 36,750 | 7,251 | 6,444 | 22,952 | 103 |
| 1971 | 41,930 | 6,151 | 4,306 | 31,471 | ".." |
| 1972 | 55,888 | 3,481 | 4,211 | 48,170 | 26 |
| 1973 | 54,886 | 2,256 | 3,622 | 48,998 | 10 |
| 1974 | 31,979 | 1,300 | 1,885 | 28,792 | ".." |
| 1975 | 20,028 | 1,041 | 1,449 | 17,533 | 5 |
| 1976 | 19,754 | 1,277 | 1,454 | 17,020 | 3 |
| 1977 | 21,429 | 1,027 | 2,219 | 18,176 | 7 |

- The totals equal 1a in every year. The rows sum to the total except 1971 and 1974, where "Not known" is printed
  ".." and the three continents fall 2 short: 6,151 + 4,306 + 31,471 = 41,928 (against 41,930) and 1,300 + 1,885 +
  28,792 = 31,977 (against 31,979). Our reading: the ".." hides 2 people in each year.
- [C] Residence and birth differ by continent. For 1957-1977, derived: Africa 202,830 by residence against 217,956 by
  birth; Asia 81,035 against 85,364; Europe plus America and Oceania 385,117 + 106,780 = 491,897 against 472,249.
  `content/aliyah.yaml` uses the residence split for 1948-51 (see the 1882-1948 sibling note, 4d), so 1a is the
  consistent one for the rows. Preferred: 1a.

### 1c. Jews and non-Jews, immigrants and potential immigrants

[V] CBS 1365 Table 1, pages 30 (grand total and Jews, 1948-1979) and 31 (other religions and the Jewish potential
immigrants, 1948-1979). "Potential immigrants" are listed from 1969 on.

| Year | Grand total | of which potential immigrants | Jews | Other religions | Not known in 1a minus other religions, derived |
|---|---|---|---|---|---|
| 1957 | 72,634 | - | 71,224 | 1,410 | 1,435 - 1,410 = 25 |
| 1958 | 27,290 | - | 27,082 | 208 | 241 - 208 = 33 |
| 1959 | 23,988 | - | 23,895 | 93 | 137 - 93 = 44 |
| 1960 | 24,692 | - | 24,510 | 182 | 204 - 182 = 22 |
| 1961 | 47,735 | - | 47,638 | 97 | 194 - 97 = 97 |
| 1962 | 61,533 | - | 61,328 | 205 | 350 - 205 = 145 |
| 1963 | 64,489 | - | 64,364 | 125 | 143 - 125 = 18 |
| 1964 | 55,036 | - | 54,716 | 320 | 327 - 320 = 7 |
| 1965 | 31,115 | - | 30,736 | 379 | 382 - 379 = 3 |
| 1966 | 15,957 | - | 15,730 | 227 | 229 - 227 = 2 |
| 1967 | 14,469 | - | 14,327 | 142 | 148 - 142 = 6 |
| 1968 | 20,703 | - | 20,544 | 159 | 161 - 159 = 2 |
| 1969 | 38,111 | 13,186 | 37,804 | 307 | 330 - 307 = 23 |
| 1970 | 36,750 | 15,510 | 35,795 | 955 | (non-Jews spread over continents) |
| 1971 | 41,930 | 15,792 | 41,238 | 692 | |
| 1972 | 55,888 | 12,516 | 54,658 | 1,230 | |
| 1973 | 54,886 | 9,932 | 53,496 | 1,390 | |
| 1974 | 31,979 | 7,756 | 30,854 | 1,125 | |
| 1975 | 20,028 | 6,684 | 19,129 | 899 | |
| 1976 | 19,754 | 7,738 | 18,829 | 925 | |
| 1977 | 21,429 | 8,926 | 20,538 | 891 | |

- Checks, derived: in every year Jews + other religions = grand total, and the grand totals equal 1a. From 1969 the
  table also splits each column into immigrants and potential immigrants, and every split adds up (for example 1977:
  grand total 12,503 + 8,926 = 21,429; Jews 11,874 + 8,664 = 20,538; other religions 629 + 262 = 891).
- Sums for 1957-1977, derived: Jews 768,435; other religions 11,961; 768,435 + 11,961 = 780,396, the 1a total.
  Potential immigrants 1969-1977: 98,040.
- Note (1) of the table (Hebrew, page 32; our translation): includes immigrants and potential immigrants (from 1969)
  who entered with an immigrant visa, and tourists who changed their status from tourist to immigrant or potential
  immigrant; does not include immigrating citizens.
- Note (5) on "Other religions" (page 32; our translation): until 1983 it includes immigrants whose religion was not
  stated.
- Our reading of the last column: up to 1969 the "Not known" continent in 1a is mostly the non-Jewish immigrants
  (T4.2 note 4), and the Jewish immigrants of unknown residence are only the small remainder. 1957 is the exception
  in size: 1,410 immigrants of other religions in one year, against 50 to 380 in every other year of 1952-1969.
  Neither table says who they were. [GAP]
- For the content work, which counts the Jewish population (design §5.4): the 1a rows include non-Jews. The Jewish
  split by continent is [GAP] in the tables read; only the Jewish yearly total is sourced (this table).

### 1d. CBS's own description of the waves

[V] CBS 1365, pages XVI-XVII, short quotes:
- 1958-60: "This wave of immigration was followed by another period of decline, which lasted from 1958 to 1960 (with
  less than 30,000 immigrants arriving per year)."
- 1961-64: "The third wave of immigration, which began in 1961 and lasted until 1964, brought close to 230,000
  immigrants into the country (approximately 60,000 per year). Most of the immigrants arrived from North Africa,
  although a substantial number also came from Romania and Poland." Check, derived: 1a gives 228,793.
- 1965-68: "slightly more than 80,000 immigrants arrived in the country (approximately 20,000 per year)". Check: 1a
  gives 82,244.
- 1969-74: "out of 280,000 immigrants who arrived in Israel during that period (approximately 45,000 per year), over
  50% were from the former USSR, and approximately 30% were from Western Europe, America and Australia." Check,
  derived: 1a for 1969-1974 is 116,791 + 142,753 = 259,544, not 280,000. Our reading: CBS's round number is about
  20,000 higher than its own table. [C] Preferred: the table.
- From 1975: "The longest decline in immigration in the country's history began in 1975."

---

## 2. Named waves

### 2a. The Soviet aliyah

[V] Tolts 2019, Table 1, page 2, "Emigration of Jews and their relatives from the FSU to three main destinations,
thousands", column Israel:

| Year | 1970 | 1971 | 1972 | 1973 | 1974 | 1975 | 1976 | 1977 | 1978 | 1979 |
|---|---|---|---|---|---|---|---|---|---|---|
| To Israel (thousands) | 1.0 | 13.0 | 31.4 | 33.3 | 16.7 | 8.3 | 7.3 | 8.3 | 12.0 | 17.3 |

Tolts names his sources (page 1; our paraphrase) as the Israeli CBS migration publications and some unpublished CBS
data, with FSU, HIAS and German statistics, and on page 2 says: "Between 1970 and 1988, a total of about 291,000 Soviet Jews and their
relatives emigrated from the country, the majority of whom - approximately 164,000 - came to Israel".

The totals on offer:

| Value | Span | Source |
|---|---|---|
| about 106,000 | 1969-1975 | [V] Bachi page 92: "between 1969 and 1975 about 106,000 Jews immigrated from the U.S.S.R. to Israel." |
| 103.7 thousand | 1970-1975 | Derived from Tolts: 1.0 + 13.0 + 31.4 + 33.3 + 16.7 + 8.3 = 103.7 |
| 119.3 thousand | 1970-1977 (to the end of this era) | Derived from Tolts: 103.7 + 7.3 + 8.3 = 119.3 |
| 148.6 thousand | 1970-1979 | Derived from Tolts: 119.3 + 12.0 + 17.3 = 148.6 |
| 137,134 born in the USSR | 1972-1979 | [V] CBS SA 2010 T4.4, page 233 (USSR (former), European republics and not stated) |
| 29,376 born in the USSR | 1961-1971 | [V] the same table and row |
| about 164,000 | 1970-1988 | [V] Tolts 2019, page 2 |
| "More than 150,000" | "this period" (the 1970s) | [S] wiki Soviet, introduction, no citation |
| "some 163,000" | "the late 1960s and the 1970s" | [S] wiki Soviet, section on absorption, no citation |

- Cross-check, derived: Tolts for 1972-1979 is 31.4 + 33.3 + 16.7 + 8.3 + 7.3 + 8.3 + 12.0 + 17.3 = 134.6
  thousand, against CBS's 137,134 born in the USSR in the same years. They differ by about 2,500 (1.8%); CBS counts
  by country of birth, Tolts counts emigrants from the FSU, so the two are close but not the same measure.
- Cross-check, derived: Bachi's 106,000 for 1969-1975 against Tolts's 103.7 thousand for 1970-1975 leaves about
  2,300 for 1969. Tolts has no 1969 figure, so a 1969 value is [GAP] in the sources read; the 2,300 is a remainder
  of two different sources, not a sourced figure.
- Our reading: Wikipedia's 163,000 is close to Tolts's 164,000, which covers 1970-1988, not only the 1970s. The
  1970s sum from Tolts's own table is 148.6 thousand.
- [C] Preferred: Tolts's yearly series, the only yearly one found. For one rounded total in event text: **about
  149,000 in 1970-1979** (derived from Tolts), of which about 119,000 arrived by the end of 1977. "More than
  150,000" and "163,000" are not used: neither is cited on the Wikipedia page, and the second matches a 1970-1988
  span. For the arrivals rows nothing changes: the Soviet arrivals are already inside 1a's `europe` column.
- Tolts, page 3: "in 1976-1977 about half of the total number of emigrants from the Soviet Union with Israeli visas
  changed their destinations, mostly for the United States." His Table 1 gives the USA column as 7.0 (1976) and
  8.5 (1977) thousand against 7.3 and 8.3 to Israel.

### 2b. Operation Yachin (Morocco, 1961-64)

- [S] wiki Yachin: "About 90,000 to 97,000 Moroccan Jews left for Israel by plane and ship from Casablanca and
  Tangier" (cited there to Laskier, 1994), and "From then till 1964, 97,005 Jews left Morocco" (cited to Abecassis).
  The dates "between November 1961 and spring 1964" carry no citation.
- [V] CBS SA 2010 T4.4, page 233, immigrants born in Morocco: 1952-1960 95,945; **1961-1971 130,507**; 1972-1979
  7,780. CBS gives no figure for 1961-1964 alone. [GAP]
- [V] 1a, all of Africa by last residence, 1961-1964, derived: 18,048 + 41,816 + 38,672 + 17,340 = 115,876. By
  birth (1b): 18,163 + 42,036 + 38,834 + 17,638 = 116,671.
- [C] Preferred: 97,005 as the operation's figure if the event text needs one (it is the exact count, the 90,000 is
  the low end of a range), marked [S] until Laskier or Abecassis is read. It fits inside CBS's Africa total for
  1961-64. It does not change the rows: Moroccan arrivals are already in 1a's `africa` column.

### 2c. Romania

[V] CBS SA 2010 T4.4, page 234, immigrants born in Romania: 1952-1960 32,462; 1961-1971 86,184; 1972-1979 18,418. No
yearly split was found. [GAP]

---

## 3. Emigration

### 3a. Bachi's series, 1955-1975

[V] Bachi Table 9.1, page 119, "Emigration of Jews from Palestine and Israel (1920-1975)". Columns 2 and 3 are yearly
averages for each period: column 2 is the "Excess of departures over returns of residents", column 3 the "Revised
estimate". Period totals are **derived** (average x years):

| Period | Yearly average, excess of departures [V] | Yearly average, revised [V] | Per 1000 population, excess / revised [V] | Per 100 immigrants, excess / revised [V] | Period total, excess, derived | Period total, revised, derived |
|---|---|---|---|---|---|---|
| 1955-57 | 10,394 | 9,333 | 6.36 / 5.70 | 18.9 / 17.0 | 10,394 x 3 = 31,182 | 9,333 x 3 = 27,999 |
| 1958-60 | 12,388 | 9,833 | 6.76 / 5.37 | 49.2 / 39.1 | 12,388 x 3 = 37,164 | 9,833 x 3 = 29,499 |
| 1961-64 | 10,292 | 10,134 | 4.97 / 4.90 | 18.1 / 17.8 | 10,292 x 4 = 41,168 | 10,134 x 4 = 40,536 |
| 1965-68 | 10,153 | 8,910 | 4.33 / 3.81 | 49.9 / 43.8 | 10,153 x 4 = 40,612 | 8,910 x 4 = 35,640 |
| 1969-73 | 5,986 | 4,221 | 2.28 / 1.61 | 13.2 / 9.3 | 5,986 x 5 = 29,930 | 4,221 x 5 = 21,105 |
| 1974-75 | 19,463 | 17,473 | 6.17 / 6.02 | 74.9 / 67.2 | 19,463 x 2 = 38,926 | 17,473 x 2 = 34,946 |
| **1958-1975, derived** | | | | | **187,800** | **161,726** |

- Our reading of two cells: the scan prints "10.292" (1961-64, column 2) and, in the 1948-51 row above these, "7.649",
  with a full stop where the other cells have a comma. Both are read as thousands (10,292 and 7,649).
- 1957 alone is [GAP]: it sits inside the 1955-57 average. The average applied to one year gives 10,394 (or 9,333),
  which is a per-year rate for 1955-57, not a sourced 1957 figure.
- 1976 and 1977 are [GAP]: Bachi's series ends in 1975, and no yearly CBS emigration series for the 1970s was
  reachable (see Sources).
- What the columns measure, Bachi §9.1, page 116: the excess of departures over returns is set 2, headed "Very exaggerated" in his summary table; the
  revised estimates come from following up "the number of residents who left in each year and did not return after a
  given span of time"; and "Almost always these estimates of set 3 are lower than those of set 2. However, they have
  also been found to be somewhat exaggerated".
- Bachi's own totals for the whole state period, page 116 [V]: for 15.5.1948-1975, 279,700 (excess of departures),
  250,000 (revised) and 215,000 as the "Final estimate", "a correction of 23.1% between 2) and 4)". He gives no final
  estimate for shorter periods: "For shorter periods no estimates of type 4 are available."
- [C] Preferred for #102's rows: the revised estimate (column 3), Bachi's best figure that exists per period. Both
  columns run high by his own account; if the content work wants Bachi's final level, scaling column 3 by 215,000 /
  250,000 = 0.86 would be a derived rule, not a sourced row, and is not proposed here.

### 3b. A check against immigration and net migration

[V] Bachi Table 9.10, page 138 (printed sideways), panel A, "Jewish population 1919-75", gives the migratory
increase of the Jewish population by period. Set against the Jewish immigrants of CBS 1365 Table 1 (1c above; 1955
and 1956 from the same table, page 30), derived:

| Period | Jewish immigrants (CBS 1365), derived sum | Migratory increase (Bachi 9.10) | Difference, derived | Bachi 9.1, revised, period total (3a) |
|---|---|---|---|---|
| 1955-57 | 37,478 + 56,234 + 71,224 = 164,936 | 136,100 | 28,836 | 27,999 |
| 1958-60 | 27,082 + 23,895 + 24,510 = 75,487 | 46,900 | 28,587 | 29,499 |
| 1961-64 | 47,638 + 61,328 + 64,364 + 54,716 = 228,046 | 193,800 | 34,246 | 40,536 |

The difference (immigrants minus net migration) is of the same size as Bachi's revised emigration for the same
years. Our reading: the two tables are consistent to within a few thousand for 1955-60 and about 6,000 for 1961-64.
The other periods of Table 9.10 (1965-71, 1972-75) do not line up with the periods of Table 9.1, so they are not
compared.

---

## 4. Population checkpoints

### 4a. End-of-year population, 1956-1977

[V] CBS SA 2010 T2.1, pages 85-86, thousands. CBS SA 2023 T2.1 (page 1) carries the same values for every one of
these years (checked cell by cell, Jews and grand total). Note 1 of the table: "Until 1995, before publication of
the Census results "Arabs" also included "others"."

| End of year | Grand total | Jews | Arabs (incl. others) |
|---|---|---|---|
| 1956 | 1,872.4 | 1,667.5 | 204.9 |
| 1957 | 1,976.0 | 1,762.8 | 213.2 |
| 1958 | 2,031.7 | 1,810.2 | 221.5 |
| 1959 | 2,088.7 | 1,858.8 | 229.9 |
| 1960 | 2,150.4 | 1,911.3 | 239.1 |
| 1961 (previous census basis) | 2,233.6 | 1,985.5 | 248.1 |
| **1961 (1961 census basis)** | **2,234.2** | **1,981.7** | **252.5** |
| 1962 | 2,331.8 | 2,068.9 | 262.9 |
| 1963 | 2,430.1 | 2,155.6 | 274.5 |
| 1964 | 2,525.6 | 2,239.2 | 286.4 |
| 1965 | 2,598.4 | 2,299.1 | 299.3 |
| 1966 | 2,657.4 | 2,344.9 | 312.5 |
| 1967 | 2,776.3 | 2,383.6 | 392.7 |
| 1968 | 2,841.1 | 2,434.8 | 406.3 |
| 1969 | 2,929.5 | 2,506.8 | 422.7 |
| 1970 | 3,022.1 | 2,582.0 | 440.1 |
| 1971 | 3,120.7 | 2,662.0 | 458.7 |
| 1972 (previous census basis) | 3,226.6 | 2,749.4 | 477.2 |
| **1972 (1972 census basis)** | **3,225.0** | **2,752.7** | **472.2** |
| 1973 | 3,338.2 | 2,845.0 | 493.2 |
| 1974 | 3,421.6 | 2,906.9 | 514.7 |
| 1975 | 3,493.2 | 2,959.4 | 533.7 |
| 1976 | 3,575.4 | 3,020.4 | 554.9 |
| 1977 | 3,653.2 | 3,077.3 | 575.9 |

- The table prints two rows for 1961 and for 1972. Its notes: "3. Based on the previous Census." and "4. Based on the
  Census of that year." [C] Preferred: the census-based row (bold), which is the base of every later year. For a
  chained run started before the census, the previous-census row is the like-for-like one: the Jewish population
  at the end of 1961 is 1,985.5 on that basis and 1,981.7 on the census basis, derived difference 3.8 thousand; at
  the end of 1972, 2,749.4 against 2,752.7, derived difference -3.3 thousand.
- Checks, derived: Jews + Arabs = grand total in every row, to within 0.1 thousand of rounding (the 1972 census row,
  1975 and 1976 are 0.1 short).
- The Arab jump at the end of 1967 (312.5 to 392.7, derived +80.2 thousand) includes the addition of East Jerusalem's
  Arab residents. [V] CBS SA 2010 T2.3 note 9: "Excl. the addition of the Arab residents from East Jerusalem in 1967
  and from the Golan Sub-District in 1982." The table shows ordinary yearly growth of 13.2 thousand from 1965 to 1966
  and 13.6 thousand from 1967 to 1968. Our reading: the full 80.2 thousand cannot be assigned to East Jerusalem, and
  the source read does not split the jump. The Jewish column has no such break.
- For `content/population.yaml` (which counts Jews, design §5.4), the Jewish values in thousands convert as the file
  already does for 1948-51 (for example 1,762.8 thousand to 1762800). Not written here.

### 4b. The census years and the census counts

- The censuses: [V] CBS Statistilite 171, page 2: "Since the first census, five additional censuses have been held
  (in 1961, 1972, 1983, 1995, and 2008)." So **1977 is not a census year**; the design's *to verify* is answered.
  The end-1977 figure is an estimate on the 1972 census basis; the next census-based figures are 1983's (CBS SA 2010
  T2.3, note 11, "Based on 1983 Census").
- Census days and counts: [V] Bachi page 404, Appendix 7.1 note 2:

| Census | Group | Estimate of population (a) | Census results (b) | (b-a):b, % [V] | Check, derived |
|---|---|---|---|---|---|
| 22 May 1961 | Total | 2,181,159 | 2,179,491 | -0.08 | -1,668 / 2,179,491 = -0.077% |
| | Jewish | 1,938,229 | 1,932,357 | -0.30 | -5,872 / 1,932,357 = -0.304% |
| | Non-Jewish | 242,930 | 247,134 | +1.70 | +4,204 / 247,134 = +1.70% |
| 20 May 1972 | Total | 3,161,400 | 3,147,683 | -0.43 | -13,717 / 3,147,683 = -0.436% |
| | Jewish | 2,695,600 | 2,686,701 | -0.33 | -8,899 / 2,686,701 = -0.331% |
| | Non-Jewish | 465,800 | 460,982 | -1.05 | -4,818 / 460,982 = -1.045% |

- Bachi, on the same page: "reliability of demographic official statistics has been found to be satisfactory",
  and "there was no need ... to correct or recast the official data for 1948-75" (quote shortened at the dots).
- Bachi's census-day check shows that the census count of Jews fell short of the running estimate by 0.30% on 22 May
  1961 and 0.33% on 20 May 1972. These check the estimates but are not the year-end breaks in Table 2.1. The breaks the
  calibration has to allow for are those derived in section 4a: the Jewish year-end row is 3.8 thousand lower on the
  1961 census basis and 3.3 thousand higher on the 1972 census basis.

---

## 5. Sources of growth

### 5a. Bachi, Jewish population by period

[V] Bachi Table 9.10, page 138, panel A:

| Period | Population at beginning | Population at end | Total growth | Natural increase | Migratory increase | % natural / % migratory [V] |
|---|---|---|---|---|---|---|
| 1955-57 | 1,526,000 | 1,762,800 | 236,800 | 100,700 | 136,100 | 42.5 / 57.5 |
| 1958-60 | 1,762,800 | 1,911,200 | 148,400 | 101,500 | 46,900 | 68.4 / 31.6 |
| 1961-64 | 1,911,200 | 2,239,200 | 328,000 | 134,200 | 193,800 | 40.9 / 59.1 |
| 1965-71 | 2,239,200 | 2,662,000 | 422,800 | 277,200 | 145,600 | 65.0 / 34.4 |
| 1972-75 | 2,662,000 | 2,959,400 | 297,400 | 190,100 | 107,200 | 63.9 / 36.1 |

- Checks, derived: end minus beginning equals total growth in every row; natural + migratory equals total growth
  except 1972-75, where 190,100 + 107,200 = 297,300, 100 short (rounding).
- Our reading of a misprint: for 1965-71 the table prints 65.0 and 34.4, which add to 99.4. Derived: 277,200 /
  422,800 = 65.6% and 145,600 / 422,800 = 34.4%. So 65.0 should read 65.6.
- Bachi's end-of-period figures match CBS T2.1 (4a): 1,762,800 (1957), 2,239,200 (1964), 2,662,000 (1971), 2,959,400
  (1975). For 1960 Bachi has 1,911,200 against CBS 1,911.3 thousand.
- Bachi's text, page 137: "Since 1958 the contribution of natural increase to Jewish population growth has become
  dominant and since 1965 it has accounted for about 65%."

### 5b. CBS, 1961-1971

[V] CBS SA 2010 T2.3, page 90 (Jews) and page 89 (total population), thousands:

| 1961-1971 | Population at beginning | Natural increase | Migration balance, total | Thereof: immigrants | Total growth | Population at end |
|---|---|---|---|---|---|---|
| Jews | 1,911.2 | 412.9 | 337.9 | 411.8 | 750.8 | 2,662.0 |
| Total population | 2,150.4 | 562.0 | 339.8 | 414.7 | 901.8 | 3,120.7 |

- Checks, derived: Jews 412.9 + 337.9 = 750.8, and 1,911.2 + 750.8 = 2,662.0. Bachi for 1961-71 (5a), derived:
  natural 134,200 + 277,200 = 411,400 and migratory 193,800 + 145,600 = 339,400, within 1.5 thousand of CBS.
- [C] The immigrants in this table do not match the immigrant tables. Total population: 414.7 thousand here against
  427,828 in T4.2 for 1961-1971 (derived from 1a; T4.4 prints the same 427,828 as its 1961-1971 grand total), a
  difference of 13.1 thousand. Jews: 411.8 thousand here against 424,220 in CBS 1365 Table 1 (derived), 12.4
  thousand apart. T2.3 note 2 says it includes immigrating citizens, which would make it larger, not smaller. The potential immigrants (44,488 in 1969-71, derived from 1c) do not explain it either (our reading: T4.2 counts them, and
  removing all 44,488 would overshoot the 13.1 thousand). The sources read do not explain the gap. [GAP] Preferred for the arrivals rows: T4.2 (1a); T2.3 is a calibration
  cross-check only.
- The next CBS period, 1972-1982, runs past the era and is not used. Yearly sources of growth for 1957-1977 are
  [GAP]: the 2010 table gives long periods only.

---

## 6. Calibration notes for the content work

Not a model, only the arithmetic the sources allow:

- Jewish population, end of 1956 to end of 1977 (4a), derived: 3,077.3 - 1,667.5 = 1,409.8 thousand, with the two
  census breaks inside (census basis minus previous basis: 1961 -3.8 thousand, 1972 +3.3 thousand).
- Jewish immigrants 1957-1977 (1c): 768,435.
- Emigration: Bachi's revised estimate gives 161,726 for 1958-1975 (3a, derived), plus a 1957 share and 1976-77 that
  are [GAP].
- Yearly Jewish change in 4a, derived, on the census-basis rows: smallest +38.7 thousand (1967), largest +95.3
  thousand (1957). The 1962 and 1973 changes depend on which 1961 and 1972 row is used (1973 is +95.6 from the
  previous-census row, +92.3 from the census row). The year-end
  values give the content work one checkpoint per year; the design asks for 1961, 1972 and 1977, and the sources
  give 1961 and 1972 on two bases each and 1977 on the 1972 census basis only.

## 7. Gaps

- Jewish immigrants by continent of residence (1a includes non-Jews; the Jewish split is not published in the tables
  read).
- Who the 1,410 immigrants of other religions in 1957 were.
- Yearly emigration for any year (Bachi gives period averages to 1975); emigration in 1976 and 1977.
- Yearly sources of growth (natural increase, migration balance) for 1957-1977.
- The 12-13 thousand gap between CBS T2.3's immigrants for 1961-1971 and the immigrant tables.
- Soviet arrivals in 1969 (Tolts starts in 1970).
- Moroccan arrivals for 1961-1964 alone; Romanian arrivals by year.
- Sicron, the 1960s-1970s Statistical Abstracts and Sabatello were not reachable; nothing here is checked against
  them.
