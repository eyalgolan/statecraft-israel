# Population and aliyah, 1882-1948

Researched 22 October 2026. Every URL below was opened on that date. Research note for issue #90 (Yishuv 11, the
research part only, opened by the owner's ruling of 22 October 2026 on #49). Research only: nothing here is drafted
for the game, and no file in `content/` is changed. `content/aliyah.yaml`, `content/population.yaml` and the rules rows
stay paused.

**Follow-up of 24 October 2026** (the [GAP] rows listed in the owner's comment on #90 of that day). Every passage
added that day is marked "(24 Oct)". Every source in the table below marked "(24 Oct)" was opened on 24 October 2026
by an AI agent reading the file or page image; a person's own check of those sources is still pending. Result: the
total population in 1882 and at the end of 1918 are now filled as [C] with two secondary values each, the 1915-1918
arrivals stay [GAP] with a sourced qualitative statement, the 1882 origin split stays [GAP], and a partial gross
series for the 1920s and 1930s is added. Details are in 1b, 2, 3 and 5.

## Purpose

Fix sourced values for §2 and §5.4 of [the Yishuv design](../../docs/superpowers/specs/2026-10-10-era-yishuv-design.md):
the population anchors (1882, 1914, the 1922 and 1931 censuses, 1947), the CBS aliyah periods from 1919 to 14 May 1948
by continent, an emigration series, and the start populations for 1882 and 1918. It also answers the design's *to
verify* mark on the Jewish Virtual Library (JVL) total of 482,857 for 1919-1948, which is not the sum of its periods.

## Conventions

- [V] read this session in a primary source: a CBS table, a Mandate census report, the *Survey of Palestine*, or
  Bachi's CICRED monograph (all listed under Sources). Scanned tables were read from the page image, not only from
  the OCR text.
- [S] secondary only (JVL, Wikipedia). Usable as a lead, to be confirmed before encoding.
- [C] two sources disagree; every value is given, and the preferred one is named.
- [GAP] not found in any source read. No value is filled in by inference.
- A [C] row names no preferred value where the choice belongs to the paused content part; the row says so.
- **Derived** marks a number this note computed. The arithmetic is shown next to it.
- "Page" is the printed page number of the source; "PDF p." is the page in the PDF file when it differs.
- No quote is invented. The short quotes below are copied from the cited page.

## Sources

| Key | Source | Where read |
|---|---|---|
| CBS SA 2010 | Central Bureau of Statistics, *Statistical Abstract of Israel 2010*, Table 4.2 "Immigrants, by period of immigration and last continent of residence", pages 230-231 (notes on p. 231) | https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_02.pdf |
| CBS 1365 | CBS Publication 1365, *Immigration to Israel 2002-2006* (2009), Table 3 "Immigrants, by period of immigration and continent of birth", page 35 | https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/e_print.pdf |
| Barron 1922 | J. B. Barron, *Palestine: Report and General Abstracts of the Census of 1922* (Jerusalem, 1923), page 3 and the erratum before Table I | https://archive.org/details/PalestineCensus1922 |
| Mills 1931 | E. Mills, *Census of Palestine 1931: Population of Villages, Towns and Administrative Areas* (Jerusalem, 1932), "Summary for Palestine" (PDF p. 11) | https://archive.org/details/CensusOfPalestine1931.PopulationOfVillagesTownsAndAdministrativeAreas |
| Survey | *A Survey of Palestine*, prepared December 1945 to January 1946 for the Anglo-American Committee of Inquiry, Vol. I: chapter VI Tables 1 and 3 (pages 141-142) and paragraphs 8-9 (page 144); chapter VII Table 1 (page 185) | https://www.bjpa.org/content/upload/bjpa/a_su/A%20SURVEY%20OF%20PALESTINE%20DEC%201945-JAN%201946%20VOL%20I.pdf |
| Bachi | Roberto Bachi, *The Population of Israel* (C.I.C.R.E.D. series, World Population Year 1974; DellaPergola cites it as 1977): Table 1.1 (page 5), §4.6 (page 36), §5.2 (page 40), §8.4 and Table 8.1 (pages 79, 87), §9.2 and §9.3 (pages 117-118), Table 9.1 (page 119) | https://web.archive.org/web/2015/http://www.cicred.org/Eng/Publications/pdf/c-c26.pdf |
| DellaPergola | Sergio DellaPergola, "Demography in Israel/Palestine: Trends, Prospects, Policy Implications", IUSSP XXIV General Population Conference, Salvador de Bahia, 2001, Table 2 (PDF p. 5) | https://iussp.org/sites/default/files/Brazil2001/s60/S64_02_dellapergola.pdf |
| JVL period table | JVL, "Total Immigration to Israel, by Continent per Year" (the table `content/aliyah.yaml` cites) | https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html |
| JVL population table | JVL, "Jewish and Non-Jewish Population of Israel/Palestine (1517-Present)" (the design's §2 source) | https://www.jewishvirtuallibrary.org/jewish-and-non-jewish-population-of-israel-palestine-1517-present |
| McCarthy (24 Oct) | Justin McCarthy, *The Population of Palestine: Population History and Statistics of the Late Ottoman Period and the Mandate* (Columbia University Press, 1990), a modern scholarly work, so [S]. Read on 24 Oct 2026: Table 2.1, 2.2, 2.3 (page 26), Table 2.18 (page 37), Table A9-14 and A9-15 (page 179), Table A10-1 (page 219). Tables read from the page image | https://yplus.ps/wp-content/uploads/2021/01/McCarthy-Justin-The-Population-of-Palestine.pdf |
| Bachi, pages re-read (24 Oct) | Bachi page 77, §8.3 (page 81) and Appendix 4A (pages 367-368), re-opened on 24 Oct 2026 in the same file as the Bachi row above | https://web.archive.org/web/2015/http://www.cicred.org/Eng/Publications/pdf/c-c26.pdf |
| JTA 1924 (24 Oct) | Jewish Telegraphic Agency, "Palestine Immigration Figures Published", 30 November 1924 (contemporaneous press, relaying official figures) | https://www.jta.org/archive/palestine-immigration-figures-published |
| JTA 1927 (24 Oct) | Jewish Telegraphic Agency, "Palestine Immigration Figures for December", 1 February 1927 | https://www.jta.org/archive/palestine-immigration-figures-for-december |
| JTA 1930 (24 Oct) | Jewish Telegraphic Agency, "521 Jews Entered Palestine in April", 4 June 1930 | https://www.jta.org/archive/521-jews-entered-palestine-in-april |

Not read: Moshe Sicron, *Immigration to Israel 1948-1953* (CBS Special Series 60, 1957). Only library records were
found and no copy was reachable, so it is cited nowhere below. The JVL table names its source as a 2015 CBS release
(`http://www1.cbs.gov.il/hodaot2015n/21_15_129t2.pdf`); that host did not answer, so CBS SA 2010 is used as the CBS
original instead. Its figures for 1919-1948 match the JVL table cell for cell.

---

## 1. Population anchors

### 1a. Summary

Jewish population and total population (all religions). The preferred value is in **bold**.

| Date | Jews | Total | Basis |
|---|---|---|---|
| 1882 | **24,000** [V] | [C] 468,089 [S] (1882-83, McCarthy) / 450,000 [S] (1880, Hirschberg via Bachi) / 300,000 [S] (JVL population table) | Survey p. 144; see 1b |
| around 1890 | 42,900 [V] | 532,060 [V] | Bachi Table 1.1 |
| 1914 | [C] **94,000** (Bachi) / 84,660 (Barron, Survey text) / 85,000 (Survey table) | [C] **689,275** (Barron) / 689,300 (Bachi) / 689,000 (Survey) | see 1c |
| end of 1918 | **56,000** [V] | [C] 748,128 [S] (McCarthy) / 648,000 [V] (March 1919, Bachi) | Bachi p. 36; Survey p. 144 ("1916-1918"); see 2b |
| March 1919 | 65,300 [V] | 648,000 [V] | Bachi p. 36, British estimate |
| 23 Oct 1922 (census) | [C] **83,794** (census) / 83,790 (excl. British forces) | [C] **757,182** (census) / 752,048 (excl. British forces) | see 1d |
| 18 Nov 1931 (census) | [C] **174,610** (census) / 174,606 (excl. British forces) | [C] **1,035,821** (census) / 1,033,314 (excl. British forces) | see 1e |
| 31 Dec 1936 | 384,078 [V] | 1,366,692 [V] | Survey Table 1 (excl. HM forces) |
| 31 Dec 1944 | [C] **528,702** (Survey Table 1) / 553,600 ("revised de facto estimate") | 1,739,624 [V] | Survey pages 141-143 |
| 31 Dec 1947 | [C] **630,000** (Bachi table) / "about 650,000" (DellaPergola text) | 1,970,400 [V] | see 1f |
| 15 May 1948 | 649,600 [V] | 805,600 [V] (Israel's later territory only) | Bachi Table 1.1 |

### 1b. 1882

- **24,000** Jews. [V] Survey Vol. I, chapter VI, page 144, table "Estimated Jewish population of Palestine": 1882
  24,000; 1895 47,000; 1900 50,000; 1910 81,000; 1914 85,000; 1916-1918 56,000; 1922 83,794. The table cites no
  source of its own.
- Total population in 1882: no census exists. The Survey (Vol. I, chapter VI, page 144, paragraph 8) says of the high
  natural increase that "no data are available on this point nor on the changes in the size of the population before
  the British occupation" [V]; that is about rates and changes, not a statement on the 1882 total. Bachi Table 1.1 gives 275,000 for
  1800 and 532,060 for "around 1890" and nothing between. The JVL population table gives 300,000 for 1882 [S], with no
  source per row. (24 Oct) Two further values found, both secondary and neither a count:
  - [S] **468,089** for 1882-83. McCarthy (1990), Table 2.18 "The Population of Palestine by Religion, 1870 to 1946",
    page 37, row "1882-83": Muslim 408,318, Jewish 15,300, Christian 44,471, total 468,089. Check, derived:
    408,318 + 15,300 + 44,471 = 468,089. Table 1.4 of the same book projects the Ottoman registers forward, corrected
    for the undercounting of women and children (chapter 1 text, introducing Table 1.4; the 1882-83 total itself is in Table 2.18). It counts Ottoman citizens only: its 1914 row has
    Jews at 61,000 with the footnote "Including noncitizen Jews", and the 1914-15 row has 38,754, so its 15,300 for
    1882-83 leaves out the non-citizen Jews (inference from that footnote) and is not comparable with the Survey's 24,000.
  - [S] **450,000** for 1880. Bachi, Appendix 4A, page 368: "An estimate quoted by H.Z. Hirschberg for 1880 (totalling
    450,000, of whom 45,000 were Christians and 25,000, Jews)." Hirschberg's own text was not opened.
  - [C] The two disagree by 18,089 (468,089 - 450,000), and they are two years apart and built differently (McCarthy
    corrected, citizens only; Hirschberg an older estimate). Neither is a primary source, so the row stays secondary.
    Which one to use is a content-part decision. McCarthy's own series runs 468,089 (1882-83), 548,854 (1895-96),
    722,143 (1914-15, citizens only); Bachi's runs 450,000 (1880, via Hirschberg), 532,060 (around 1890), 689,300
    (1914). Do not mix the two series.
  - A primary Ottoman count (the 1881-82 register, Karpat or Ottoman archive) was not reachable: the FamilySearch
    collection "Palestine, Ottoman Census and Population Registers, 1876-1917" was listed in a search result but not
    opened, and its images were not read.
- The design's 1882 start population (24,000) is therefore confirmed. Its split by origin is still [GAP]
  (24 Oct) "Searched again and not found." Read: Bachi Table 1.1 and Appendix 4; McCarthy Table A10-1 (page 219),
  which gives for Jerusalem alone 13,920 in 1881 (Luncz) and has no row dated 1882 or near it with a split by origin (its community classification columns are 1845, 1916-1918 and a 1928 estimate, below);
  Survey Vol. I chapter VI. No source read splits the 1882 population by origin. What does exist, kept unmerged:
  - [S] McCarthy Table A10-1, lower block "Classification by Communities" (also a 1928 Palestine Zionist Executive estimate, Ashkenazic 114,000, 71%, total 160,000): 1845 (Schwarz) Ashkenazic 3,800 (32%), Sephardic and
    Oriental 8,000 (68%), total 11,800; 1916-1918 census Ashkenazic 33,029 (59%), Sephardic (Spanioli) 12,575, Yemenite
    4,356, Persian 1,786, Bukharian 790, other Oriental 3,464, total 56,000. Check, derived: 12,575 + 4,356 + 1,786 +
    790 + 3,464 = 22,971, and 33,029 + 22,971 = 56,000; 33,029 / 56,000 = 59.0%. The table cites "Handbook" as its source.
  - [V] Bachi, page 77: among 472 immigrants who arrived in 1842-1881 and survived in Palestine in 1916-1918, 37% came
    from Asia and North Africa, 29.4% from Russia and 33.6% from other European countries. This is the origin of the
    arrivals of 1842-1881 that were still alive in 1916-18, not of the population of 1882.

### 1c. 1914

- **94,000** Jews, total 689,300. [V] Bachi Table 1.1, page 5, under "C) Estimates" (Jews 94,000; Christians 70,000;
  Moslems 525,150). DellaPergola Table 2 repeats 94 (thousands) and 689 for the total, citing Bachi. [V]
- **84,660** Jews. [V] Barron 1922, page 3: "the Jewish population in 1914 was calculated to be 84,660 of whom 11,660
  were in Jewish Colonies and the remainder in towns", citing Ruppin, *Syrien als Wirtschaftsgebiet* (1916). The
  Survey, page 144, paragraph 8, repeats 84,660.
- **85,000** Jews. [V] Survey page 144, the table in 1b. The second digit is not sharp on the scan; it reads as 85,000
  and could be 86,000. Either way it differs from the 84,660 in the paragraph above it on the same page.
- Total 1914: **689,275**. [V] Barron 1922, page 3, "record taken in 1914" from Turkish sources: Mutessariflik of
  Jerusalem 398,362; Nablus 153,749; Acre 137,164; total 689,275. The Survey (page 144) rounds it to 689,000 and Bachi
  to 689,300.
- Preferred: 94,000, as the design (D8) already chose, because Bachi's monograph is the later demographic
  reconstruction and the design names it. The lower value (84,660) is the Mandate's own figure and is the D8
  alternative. [C]

### 1d. The 1922 census (23 October 1922)

- Date: the census was held "on the night of the twenty-second-twenty-third of October, 1922" (Barron, page 1); the
  title page says "taken on the 23rd of October, 1922". [V]

- **Census figures:** Jews 83,794; total 757,182 (Moslems 590,890; Christians 73,024; Others 9,474). [V] Barron 1922,
  page 3, "A comparison of religions". The Jewish figure prints as 83,694 in Table I, and the erratum before Table I
  says: "For Total Jews 83,694 read 83,794".
- **Excluding British forces:** Jews 83,790; total 752,048. [V] Survey Table 1, page 141 ("Excluding His Majesty's
  Forces"); Bachi Table 1.1, page 5, which notes "Incl. nomads, excluding British Forces. The data for 1922 are the
  official ones."
- Difference, derived: total 757,182 - 752,048 = 5,134; Jews 83,794 - 83,790 = 4. Neither source names the
  5,134 directly. Barron (paragraph 3, page 1) says the military authorities "completed the particulars for all
  persons, both civilians and military, resident in military areas", so the census figure includes the forces.
- Preferred: the census figures (83,794; 757,182), as the design already has. The 83,790 series is the one to use
  where a figure must be consistent with the Survey's later annual estimates, which all exclude the forces. [C]
- Earlier official estimates on the same page: Jews 66,574 (1920) and 81,263 (1921). [V] Barron 1922, page 3.

### 1e. The 1931 census (18 November 1931)

- **Census figures:** Jews 174,610 (88,100 men, 86,510 women); total 1,035,821 (settled 969,268, nomadic 66,553).
  [V] Mills 1931, "Summary for Palestine", PDF p. 11. The census date is "midnight on the 18th of November 1931"
  (Mills, preface, PDF p. 3).
- **Excluding British forces:** Jews 174,606; total 1,033,314. [V] Survey Table 1, page 141; Bachi Table 1.1, page 5.
- Difference, derived: total 1,035,821 - 1,033,314 = 2,507; Jews 174,610 - 174,606 = 4.
- Preferred: the census figures (174,610; 1,035,821), as the design already has. [C]

### 1f. 1947

- **630,000** Jews on 31 December 1947, total 1,970,400. [V] Bachi Table 1.1, page 5 (Christians 143,300; Moslems
  1,180,500). DellaPergola Table 2 gives 630 and 1,970 (thousands) for 1947, citing Bachi. [V]
- [C] DellaPergola's own text on PDF p. 4 says that in 1947, "out of an estimated total population of about 2 million",
  "about 650,000 (32%) were Jewish". His table on the next page gives 630. Bachi (§5.2, page 40) gives the same 650,000
  for May 1948: "growing from about 56,000 at the beginning of 1919 to 650,000 in May 1948".
- Preferred: 630,000 for the end of 1947 (the tabulated value, and the design's), 650,000 being the round May 1948
  figure.
- For the Founding chain: Bachi Table 1.1 gives 649,600 Jews on 15 May 1948. `content/rules.yaml`
  (`initial_population`) derives 649,400 from the CBS end-1948 figure. The two differ by 200; this is reported, not
  changed.

### 1g. How the Jewish population grew, 1922-1944

[V] Survey Table 3, page 142 ("Not including members of His Majesty's Forces; inclusive of nomads"):

| Jews | Value |
|---|---|
| Census 1922 | 83,790 |
| Estimate, end of 1944 | 528,702 |
| Total increase | 444,912 |
| Natural increase | 117,226 (26%) |
| Migratory increase | 327,686 (74%) |

Check, derived: 528,702 - 83,790 = 444,912, and 117,226 + 327,686 = 444,912. Both hold.

---

## 2. Start populations

### 2a. 1882

24,000 Jews (1b). Split by origin: [GAP]; (24 Oct) searched again and not found, the nearest partial data are in 1b.

### 2b. 1918

- **56,000** Jews at the end of 1918. [V] Bachi §4.6, page 36, "b) Jewish population (end of 1918)", based on the
  Jewish population censuses of 1916-18 and a back-projection from the 1922 census. The Survey (page 144) gives
  56,000 for "1916-1918" and says the Jews "declined from 84,660 to 56,000 in consequence of epidemics, emigration and
  deportations by the Turks".
- [C] The British estimate for March 1919 is 65,300 Jews, total 648,000 [V] (Bachi, page 36). Bachi says it "possibly
  somewhat overstates the number of Jews". The JVL population table gives 60,000 for 1918 [S], with no source per
  row.
- Preferred: 56,000 (Bachi and the Survey agree).
- War loss, derived: 94,000 - 56,000 = 38,000, which is 40.4% of 94,000. Bachi (page 36): the Jewish population
  "presumably declined by some 40%".
- Total population at the end of 1918: no count exists. (24 Oct) Two values, both short of a primary count:
  - [V] 648,000 for 31 March 1919 (Bachi, page 36, from the Palestine Royal Commission Report, Cmd. 5479, 1937,
    page 156, as Bachi's Appendix 4A cites it). The Royal Commission report itself was not opened.
  - [S] **748,128** for 1918. McCarthy (1990), Table 2.2 "Population of Palestine in 1918, Projected Back from 1922
    Census Figures, As Corrected", page 26: Muslim 611,098, Jewish 58,728, Christian 70,429, Druze 7,268, Shii 162,
    Other 443. Check, derived: 611,098 + 58,728 + 70,429 + 7,268 + 162 + 443 = 748,128. His method: project the
    corrected 1922 population back at the 1922-1931 growth rate (.0186189 per year) and subtract the immigrants of
    1919-1922 (his Table 2.1: 1,806 + 8,223 + 8,294 + 8,685 = 27,008; his footnote to Table 2.1, page 26, first multiplies the 27,008 by the 1922 correction factor 1.114164, which gives 30,091).
  - [C] The two differ by 100,128 (748,128 - 648,000). The reason is the base: McCarthy's 1914 total is 798,389 (his
    Table 2.3, corrected for undercount), Bachi's is 689,300 (the uncorrected Ottoman figure, Barron). Both show a
    war loss of about 6%: McCarthy 798,389 to 748,128 is 50,261 (6.3%, his table prints (.063)); Barron's 689,275 (Bachi's base) to
    648,000 is 41,275, derived, 6.0%. Choose by the 1914 anchor used: with 689,300 use 648,000, with 798,389 use
    748,128. McCarthy's Jewish 58,728 sits near the preferred 56,000 above (a difference of 2,728).

---

## 3. Arrivals before 1919

| Period | Jewish immigrants | Source |
|---|---|---|
| 1850-80 | 25,000 | [V] Bachi Table 8.1, page 79 |
| 1881-1903 | 20,000-30,000 (range) | [V] Bachi Table 8.1; CBS SA 2010 gives "20,000-30,000" for 1882-1903 |
| 1904-10 | 20,300 | [V] Bachi Table 8.1 |
| 1911-14 | 14,000 | [V] Bachi Table 8.1 |
| 1850-1914 | 84,300 | [V] Bachi Table 8.1 |
| 1915-1918 | ".." (no figure) | [V] CBS SA 2010 |

- **Second Aliyah as one value, derived:** 20,300 + 14,000 = **34,300** for 1904-1914 (Bachi's two rows). This
  replaces the design's "about 35,000" estimate. CBS SA 2010 gives only the range 35,000-40,000 for 1904-1914. [C]
- **First Aliyah:** no single value exists in any source read. Bachi's own 1850-1914 total implies he used 25,000
  for 1881-1903, derived: 84,300 - 25,000 - 20,300 - 14,000 = 25,000, the middle of his range. Bachi does not state
  this; it is an inference from his total and is marked so.
- Bachi's prose (§8.4, page 87) gives "during 1880-1914 some 55,000-65,000 persons", again a range.
- 1915-1918 arrivals: [GAP]. CBS shows no figure. A derived zero is a design decision, not a sourced value.
  (24 Oct) Searched again and not found as a number. Read: Bachi §8.3, Table 8.1 (no 1915-1918 row), McCarthy Table 2.1
  (starts in 1919), the Survey chapter VII, the JVL period table (has a 1915-1918 row with no figures). What is sourced is qualitative:
  [V] Bachi, page 81, "Only during 1915-1918 there has been an almost complete standstill, due to the First World
  War." "Almost complete" is not zero, so a derived zero stays a design decision. McCarthy Table 2.1 (his source,
  the Jewish Agency) gives 1919 as 1,806 [S], the first sourced year.

---

## 4. Aliyah 1919 to 14 May 1948, by period and continent

### 4a. The CBS original

[V] CBS SA 2010, Table 4.2, page 230 (notes 1 and 3 on page 231), "Immigrants, by period of immigration and last continent of residence".
Columns as in `content/aliyah.yaml`:

| Period | Total | Asia | Africa | Europe | America and Oceania | Not known |
|---|---|---|---|---|---|---|
| 1919-23 | 35,183 | 1,181 | 230 | 27,872 | 678 | 5,222 |
| 1924-31 | 81,613 | 9,182 | 621 | 66,917 | 2,241 | 2,652 |
| 1932-38 | 197,235 | 16,272 | 1,212 | 171,173 | 4,589 | 3,989 |
| 1939-45 | 81,808 | 13,116 | 1,072 | 62,968 | 108 | 4,544 |
| 1946 - 14 May 1948 | 56,467 | 1,144 | 906 | 48,451 | 138 | 5,828 |
| **1919 - 14 May 1948** | **482,857** | **40,895** | **4,041** | **377,381** | **7,754** | **52,786** (note 3) |

- Note 1 to the table: "Incl. potential immigrants and tourists who changed their status to immigrants."
- Note 3 (on 52,786): "Incl. about 11,000 illegal immigrants and about 19,500 tourists who remained in Israel, and
  whose continent of residence and period of immigration are not known."
- Every row sums to its total (checked, derived). The JVL period table carries the same 30 numbers.

### 4b. Why 482,857 is not the sum of the periods

Derived:

- Sum of the five period totals: 35,183 + 81,613 + 197,235 + 81,808 + 56,467 = **452,306**.
- Gap: 482,857 - 452,306 = **30,551**.
- The four named-continent columns do sum to their totals: Asia 1,181 + 9,182 + 16,272 + 13,116 + 1,144 = 40,895;
  Africa 230 + 621 + 1,212 + 1,072 + 906 = 4,041; Europe 27,872 + 66,917 + 171,173 + 62,968 + 48,451 = 377,381;
  America and Oceania 678 + 2,241 + 4,589 + 108 + 138 = 7,754.
- The "not known" column does not: 5,222 + 2,652 + 3,989 + 4,544 + 5,828 = 22,235, against a total of 52,786.
  52,786 - 22,235 = **30,551**, the whole gap.
- Note 3 explains it: about 11,000 illegal immigrants and about 19,500 tourists who stayed, 11,000 + 19,500 = 30,500,
  whose "period of immigration [is] not known". CBS puts them in the 1919-1948 total but in no period row.

So the JVL table is not wrong: it copies the CBS table, which has these 30,551 people in the total row only. JVL drops
the note's link to the gap. [V] The JVL footnote (3) carries the same sentence as the CBS note.

### 4c. A cross-check: Bachi spreads the 30,551 over periods

[V] Bachi Table 8.1, page 79, uses finer periods whose rows do sum to 482,857:

| Bachi period | Immigrants |
|---|---|
| 1919-23 | 35,183 |
| 1924-26 | 62,133 |
| 1927-31 | 19,480 |
| 1932-36 | 191,224 |
| 1937-39 | 56,499 |
| 1940-45 | 60,315 |
| 1946-48 (to 14 May) | 58,023 |
| 1919-48 | 482,857 |

Derived:
- 35,183 + 62,133 + 19,480 + 191,224 + 56,499 + 60,315 + 58,023 = 482,857.
- 1919-23 equals CBS. 1924-26 + 1927-31 = 62,133 + 19,480 = 81,613, which equals CBS 1924-31.
- 1932 to 14 May 1948: Bachi 191,224 + 56,499 + 60,315 + 58,023 = 366,061; CBS 197,235 + 81,808 + 56,467 = 335,510.
  366,061 - 335,510 = 30,551, the unallocated CBS remainder.

So Bachi put the illegal immigrants and staying tourists into the periods
from 1932 on. His table does not say how they were split, and his period boundaries (1936/37, 1939/40) differ from
CBS's, so his split cannot be carried into the five CBS rows. That split is [GAP].

### 4d. Choice for the series

- Preferred: the CBS rows of 4a, as the issue asks. They sum to 452,306, not 482,857.
- [C] The 30,551 with no period are a modelling choice for the content work: leave them out (the series then falls
  short of the anchors by about 30,500), or spread them by a stated rule as a derived row. Bachi's table shows they
  belong after 1931, but gives no rule. No split is proposed here.
- The JVL "continent" label: CBS SA 2010 is by **last continent of residence**. CBS 1365 Table 3, page 35 [V], splits
  the same 482,857 by **continent of birth** instead: Asia 40,776; Africa 4,033; Europe-America 385,066; not known
  52,982 (note 6, the same 11,000 and 19,500). Check, derived: 40,776 + 4,033 + 385,066 + 52,982 = 482,857.
  `content/aliyah.yaml` uses the residence split for 1948-51, so the residence table (4a) is the consistent one.

### 4e. Registered immigration in the Mandate's own statistics

- [V] Survey Vol. I, chapter VII, Table 1, page 185, "Total number of persons registered as immigrants (Including
  persons who entered as travellers and subsequently registered as immigrants)": Jews, September 1920 to November 1945,
  **367,845** (all races 401,149).
- This counts legal registrations only, so it is lower than CBS. It is not comparable period by period with 4a
  (different start and end, no illegal immigration). Reported for the calibration note, not as a competing value.

---

## 5. Emigration

### 5a. 1882-1918

No series exists. Bachi §9.2, page 117: "As far as we know, no statistical reconstruction of the size of emigration in
this period has been undertaken." [V] So Ottoman-era emigration can only be derived from the anchors. [GAP] for any
sourced row.

Two sourced facts for the war years [V]:
- Bachi, page 118: "From the 1922 census it appears that at least 6,264 Jews of Palestinian nationality resided
  abroad".
- Barron 1922, page 3, citing Ruppin: Jerusalem's Jews fell from 45,000-50,000 in 1914 to 26,605 in 1916, and
  Jaffa's from 12,000 to 6,309.

### 5b. 1920 to 14 May 1948

[V] Bachi Table 9.1, page 119, "Emigration of Jews from Palestine and Israel (1920-1975)", column 2, "Yearly number of
emigrants: excess of departures over returns of residents". These are yearly averages for each period. The period
totals are **derived** (average x years):

| Period | Yearly average [V] | Per 1000 population [V] | Per 100 immigrants [V] | Period total, derived |
|---|---|---|---|---|
| 1920-23 | 2,277 | 30.93 | 27.3 | 2,277 x 4 = 9,108 |
| 1924-25 | 2,094 | 28.48 | 8.7 | 2,094 x 2 = 4,188 |
| 1926-27 | 6,218 | 61.20 | 73.6 | 6,218 x 2 = 12,436 |
| 1928-31 | 1,575 | 14.30 | 38.1 | 1,575 x 4 = 6,300 |
| 1932-35 | 360 | 1.48 | 0.9 | 360 x 4 = 1,440 |
| 1936-38 | 5,712 | 19.67 | 31.2 | 5,712 x 3 = 17,136 |
| 1939-44 | 333 | 0.68 | 2.6 | 333 x 6 = 1,998 |
| 1945 - 14 May 1948 | 3,677 | 6.38 | 16.9 | 3,677 x 3.3676 = 12,383 |

- Length of the last period, derived: 1 January 1945 to 14 May 1948 inclusive is 1,230 days; 1,230 / 365.25 = 3.3676
  years.
- Bachi's averages row [V]: 1932-48 2,013; 1920-48 2,289. The 1920-31 average is not legible on the scan (it starts
  "2,6"). Derived from the rows: (9,108 + 4,188 + 12,436 + 6,300) / 12 = 32,032 / 12 = 2,669.
- Check, derived: 1932-48 = (1,440 + 17,136 + 1,998 + 12,383) / 16.368 = 32,957 / 16.368 = 2,013, which matches
  Bachi's 2,013 and confirms the 3.368-year reading of the last period. 1920-48 = (32,032 + 32,957) / 28.368 = 2,291,
  against Bachi's 2,289; the 2 is rounding in his averages.
- Total Jewish emigration 1920 to 14 May 1948, derived: 32,032 + 32,957 = **64,989** (about 65,000).
- Bachi, page 118, on the size: emigration "offset about 28% of immigration in the first part of the Mandatory era
  (1920-31)". His table gives 27.8 per 100 immigrants for 1920-31, 9.0 for 1932-48 and 13.5 for 1920-48.
- These are net figures (departures minus returning residents), not gross departures. A gross series is partly open:
  (24 Oct) a full gross series of permanent Jewish emigration was not found. Three kinds of gross figures were found,
  none of them the same thing, so they are listed apart and not summed:
  - [S] Residents' movement, Jews, McCarthy Table A9-15 "Movement of Residents of Palestine, 1926 to 1937", page 179
    (the table cites *Migration*, 1937 as its source). By the column headings (the author's reading, the table has no definition) it counts every departure of a
    resident, temporary trips included, and every return. Jews departing: 1933 9,282; 1934 14,787; 1935 25,207; 1936 24,611; 1937 36,716.
    Jews returning: 1933 10,236; 1934 14,552; 1935 23,050; 1936 17,895; 1937 31,153. Excess of departures over
    returns, derived: 1933 9,282 - 10,236 = -954; 1934 14,787 - 14,552 = 235; 1935 25,207 - 23,050 = 2,157; 1936
    24,611 - 17,895 = 6,716; 1937 36,716 - 31,153 = 5,563. The table has no Jewish split for 1926-1932 (only totals
    for all residents) and none for 1920-1925. These excesses are the same kind of figure as Bachi's net series;
    they are of the same order as Bachi's yearly averages (360 for 1932-35, 5,712 for 1936-38), but no match
    is claimed, because the periods differ.
  - [S] Same book, Table A9-14 "Arrivals and Departures, 1923 to 1946", page 179: all religions together, with
    no split by religion. Only 1923-1925 is a combined column; from 1926 on the table has separate "Residents Departing" and
    "Travellers Departing" columns. Not usable as Jewish emigration, because there is no split by religion.
  - [S] contemporaneous press, partial (the tag is [S] because the Conventions list no press source as [V]): JTA 30 Nov 1924: "8,191 Jews enterer [sic] Palestine in the first nine months of the
    year 1924 ... During the same period 1,701 emigrated." JTA 1 Feb 1927: "During last month 513 Jews immigrated to
    Palestine, while 642 left the country" (December 1926). JTA 4 Jun 1930: "Of the 118 who left, 57 were Moslems, 50
    Christians and 11 Jews" (April 1930). Each is one official period figure reported by a news agency; the
    underlying government return was not opened.
  - Still [GAP]: a sourced gross series of Jewish departures for 1920-1932 as a whole, and any gross series before
    1920. Not read: the Palestine Blue Books and the Government of Palestine's annual *Migration* reports (no copy found).

---

## 6. Calibration notes for the content work

Not a model, only the arithmetic the sources allow:

- 1922 to 1944: the Survey splits the growth into natural increase 117,226 and migration 327,686 (1g).
- 1914 to 1918: 94,000 to 56,000 (or 84,660 to 56,000 on the Mandate's own figure), a fall of 38,000 (or 28,660,
  derived: 84,660 - 56,000).
- 1919 to 14 May 1948: CBS immigration 482,857 (452,306 in periods plus 30,551 without a period), net emigration about
  65,000 (5b), against a growth from 56,000 (end of 1918) to 649,600 (15 May 1948), derived: 649,600 - 56,000 =
  593,600. The remainder, 593,600 - 482,857 + 64,989 = 175,732, is what natural increase would have to supply. That is
  a derived residual, not a sourced natural-increase figure, and it carries no emigration for 1919 (Bachi's series
  starts in 1920); Survey Table 6 (page 144) gives the Jewish rates of
  natural increase per 1000 by five-year period (20.44 in 1922/25 down to 17.83 in 1941/44) if the content work wants
  to check it.

## 7. Gaps

- Total population of Palestine in 1882: now [C] with two [S] values (1b); no primary count.
- Origin split of the 1882 and 1918 start populations (the 1918 split is in the 1916-18 census, 1b; the 1882 split is [GAP]).
- A single sourced First Aliyah figure (only ranges; Bachi's implied 25,000 is an inference).
- Arrivals 1915-1918 (CBS shows no figure).
- How the 30,551 immigrants with no period divide over the CBS periods.
- Emigration before 1920, and a complete gross (not net) series after it (partial figures in 5b).
- Total population at the end of 1918: now [C] with two values (2b); no primary count.
- Sicron (1957) was not reachable; nothing here is checked against it.
