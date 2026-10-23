# M4 content research: immigration, emigration and population, 1952 to 1956

Research only. Researched 2026-10-23; all pages were opened on that date. Nothing here is a
decision about `content/aliyah.yaml`, `content/population.yaml`, the emigration code or the
calibration test, which stay paused (#102, maintainer comment of 22 October 2026).

**Conventions**
- **[V]** verified on the cited page, which was opened. The CBS volumes here are PDFs, so each
  table was read both as extracted text and on a rendered image of the page, and the two were
  compared before a figure was accepted.
- **[S]** single source, or a search-result summary that was not opened directly.
- **[C]** sources conflict. Both values are given, each with its source.
- **[GAP]** not found.
- A figure marked **computed here** is plain arithmetic on a cited table, not a printed CBS figure.
- An interpretation is marked **our reading** and is not attributed to CBS.
- No quotes are invented. The short quotes below are copied from the cited page.

**Access notes**
- `economy-series-1952-56.md` records that no volume of the CBS Statistical Abstract for these
  years was found online. Two volumes carrying the 1952 to 1956 series were reached for this note:
  the **Statistical Abstract of Israel 2010**, whose historical tables run back to 1948, and
  **Statistical Abstract of Israel No. 14 (1963)**, which is contemporary with the window and is
  published under `publications/DocLib/shnaton_saruk/`. That directory is the same one the repo
  already cites for `shnaton1950_num1.pdf`, `shnaton1951_num2.pdf` and `shnaton1952_num3.pdf`.
- knesset.gov.il (both `fs.knesset.gov.il` and `main.knesset.gov.il`) returns HTTP 247 and a
  JavaScript bot challenge rather than content, which is the same wall recorded in
  `research_notes/M3 content 1950-51/facts.md`. Nothing in this note depends on it.
- `https://www.cbs.gov.il/en/subjects/Pages/Immigration.aspx` answers HTTP 200 but serves the CBS
  "page not found" page. It is not a source and is not cited below.
- The 1963 volume is an old scan and its text layer is unreliable. Three figures in it were
  misread by text extraction and corrected against the rendered page; they are flagged in place.

**Sources**
- **SA 2010 table 4.2**, immigrants by period of immigration and last continent of residence,
  printed page 230.
  https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_02.pdf
- **SA 2010 table 2.1**, population by population group, printed page 85.
  https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_01.pdf
- **SA No. 14 (1963) table 2**, sources of increase of the population (estimate), printed page 16.
  https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf
- **SA No. 14 (1963) table 1**, marriages, divorces, births, deaths, natural increase, infant
  deaths and stillbirths, by population groups, printed page 65. Same PDF.
- **SA No. 14 (1963) table 13**, emigrants by population groups and year of emigration,
  printed page 121; definitions in the migration introduction, printed page 104. Same PDF.
- **SA 2010 table 4.4**, immigrants by period of immigration, country of birth and last country
  of residence, printed pages 233-234.
  https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_04.pdf

---

## 1. Arrivals by year and continent of last residence

SA 2010 table 4.2, p. 230. Whole population. [V]

| Year | Total | Asia | Africa | Europe | America and Oceania | Not known |
|---|---|---|---|---|---|---|
| 1952 | 24,610 | 6,867 | 10,286 | 6,232 | 950 | 275 |
| 1953 | 11,575 | 3,014 | 5,102 | 2,147 | 930 | 382 |
| 1954 | 18,491 | 3,357 | 12,509 | 1,369 | 1,091 | 165 |
| 1955 | 37,528 | 1,432 | 32,815 | 2,065 | 1,155 | 61 |
| 1956 | 56,330 | 3,139 | 45,284 | 6,739 | 1,067 | 101 |

**The continent rows sum to the printed total for every one of the five years**, exactly, with no
residual. Computed here, from the table's own columns.

The table also prints two percentage columns, Asia and Africa against Europe, America and Oceania.
Those reproduce exactly when the share is taken over the total **less** the "not known" column
(1952: 17,153 of 24,335 is 70.5 per cent, as printed; 1955: 34,247 of 37,467 is 91.4 per cent, as
printed). Computed here. This is what fixes which column is which, independently of the
right-to-left column order, and it holds for all five years.

The table's own period subtotals are 54,676 for 1952 to 1954 and 166,492 for 1955 to 1957. [V]

**The 1955 and 1956 surge is African.** Africa rises from 5,102 in 1953 to 32,815 in 1955 and
45,284 in 1956, while Asia falls to 1,432 in 1955. In 1955 Africa alone is 87.4 per cent of all
arrivals (computed here).

**Our reading:** this African surge is the nearest by-year proxy in these tables for the North
African aliyah that #102 says matters for the 1955 election and the development towns; table 4.2
does not split Africa by country.

**Finer than continent, by year: [GAP].** SA 2010 table 4.4, pp. 233-234, gives country of birth,
but its columns for this window are the period 1952 to 1960 as one bucket, not single years. No per-year
country-of-origin table was found. The repo's 1950 to 1951 note records the same gap for its own
window.

## 2. Immigrants per year: the two CBS series disagree

| Year | SA No. 14 (1963) t.2, p. 16 | SA 2010 t. 4.2, p. 230 | difference |
|---|---|---|---|
| 1952 | 23,408 | 24,610 | 1,202 |
| 1953 | 10,388 | 11,575 | 1,187 |
| 1954 | 17,485 | 18,491 | 1,006 |
| 1955 | 36,327 | 37,528 | 1,201 |
| 1956 | 54,996 | 56,330 | 1,334 |

[C], both CBS. Both values are given above and neither is presented as the figure.

The 2010 table's own footnote 1, p. 231, reads: "Incl. potential immigrants and tourists who
changed their status to immigrants. As of 1970 excl. immigrating citizens". [V]

**Our reading:** the 1963 migration introduction, p. 104, defines immigrants as "persons entering
the country to take up permanent residence" and lists tourists settling separately. [V] SA No. 14
table 3, p. 109, gives tourists settling (Jews) as 994 in 1952, 979 in 1953, 899 in 1954, 1,175
in 1955 and 1,309 in 1956. [V] That is most of each year's gap above, and the rest (25 to 208 a
year) is consistent with the potential immigrants and non-Jews the 2010 footnotes include. CBS
does not reconcile the two series itself. A reader who needs one series should say which,
rather than mixing them across years.

## 3. Emigration by year

SA No. 14 (1963) table 13, p. 121, whole population. [V]

| Year | All emigrants (estimate) | Persons declaring emigration |
|---|---|---|
| 1952 | 13,500 | 11,128 |
| 1953 | 13,000 | 8,644 |
| 1954 | 7,500 | 5,774 |
| 1955 | 6,400 | 3,922 |
| 1956 | 11,400 | 6,245 |

The migration introduction, p. 104, defines emigrants as "residents leaving the country
permanently" and says the number who declare that intention on leaving "is lower than that of
persons emigrating in fact". For 1952 to 1960 the all-emigrants column is "a rough estimate" from
the number of residents leaving and returning and the average stay abroad of returning
residents. [V]

The printed table has no 1951 row, so the year labels were checked: table 13 gives 10,698 Jewish
declaring emigrants for 1952, and less the 202 who returned the same year (table 13, note 3) that
is the 10,496 table 14 prints for 1952 on the same page. Computed here.

SA No. 14 table 2, p. 16, also gives net movement of residents, which balances the population
account. It is a net figure, not a count of emigrants. [V]

| Year | Immigrants | Net movement of residents | Net movement of travellers | Migration balance |
|---|---|---|---|---|
| 1952 | 23,408 | minus 13,793 | plus 1,416 | plus 11,031 |
| 1953 | 10,388 | minus 13,178 | plus 1,255 | minus 1,535 |
| 1954 | 17,485 | minus 8,324 | plus 1,785 | plus 10,946 |
| 1955 | 36,327 | minus 7,114 | plus 1,940 | plus 31,153 |
| 1956 | 54,996 | minus 12,410 | plus 512 | plus 43,098 |

For every year, immigrants plus net movement of residents plus net movement of travellers equals
the printed migration balance, exactly. Computed here.

**1953 is the year the migration balance is negative**, at minus 1,535, and the table prints its
migration share of total growth as minus 3.8 per cent against a natural increase share of 103.8
per cent. [V] This supports the statement in #102 that in 1953 emigration roughly matched
immigration: table 13 estimates 13,000 emigrants that year, against 10,388 immigrants in table 2.

**Two text-extraction corrections.** The extracted text gave the 1953 migration balance as minus
1,533 and the 1955 migration balance as plus 31,133. On the rendered page both read minus 1,535
and plus 31,153, and only those values satisfy the row arithmetic. The printed figures are used.

## 4. Population at the end of each year

Two independent CBS tables, agreeing. [V]

| Year | SA No. 14 (1963) t.2, p. 16 | SA 2010 t. 2.1, p. 85 (thousands) | average population (thousands) |
|---|---|---|---|
| 1952 | 1,629,519 | 1,629.5 | 1,606.2 |
| 1953 | 1,669,417 | 1,669.4 | 1,650.3 |
| 1954 | 1,717,814 | 1,717.8 | 1,689.5 |
| 1955 | 1,789,075 | 1,789.1 | 1,750.4 |
| 1956 | 1,872,390 | 1,872.4 | 1,828.4 |

The 1963 volume's figures round to the 2010 volume's figures in every year. Computed here.

In the 1963 table, each year's population at the beginning of the period equals the previous
year's population at the end, and population at the beginning plus total increase equals
population at the end, in every year of the window. Computed here.

**One text-extraction correction.** The 1952 row of SA No. 14 table 2 prints the population at the end
of the period as 1,629,319 in the extracted text. The rendered page reads **1,629,519**, which is
also what the 1953 row gives as its population at the beginning of the period, what the row's own
arithmetic requires (1,577,825 plus 51,694), and what SA 2010 table 2.1 gives as 1,629.5 thousand.
The note uses 1,629,519.

SA 2010 table 2.1 also splits each year into Jews, Arabs and others. End of 1952 is 1,450.2
thousand Jews and 179.3 thousand Arabs; end of 1956 is 1,667.5 thousand Jews and 204.9 thousand
Arabs. [V]

## 5. Natural increase: the two tables of the 1963 volume disagree

| Year | table 2, p. 16 | table 1, p. 65 | difference | births (p. 65) | deaths (p. 65) |
|---|---|---|---|---|---|
| 1952 | 40,663 | 40,890 | 227 | 52,556 | 11,666 |
| 1953 | 41,433 | 41,636 | 203 | 52,552 | 10,916 |
| 1954 | 37,451 | 37,623 | 172 | 48,951 | 11,328 |
| 1955 | 40,108 | 40,154 | 46 | 50,686 | 10,532 |
| 1956 | 40,217 | 40,262 | 45 | 52,287 | 12,025 |

[C], and both are in the same volume. On p. 65, births minus deaths equals the printed natural
increase in every year, exactly. Computed here.

Table 1 carries footnote 1 on its "Others" rows: "Excluding all data for the Bedouin in the
Negev. As from 1957 marriages and divorces of Negev Bedouin are included. Registration of deaths
among non-Jews is incomplete." [V] Table 2 is headed "Sources of Increase of the Population
(Estimate)", and its footnote 1, p. 17, says its figures differ slightly from chapter C, table 1
and refers to the introduction. [V]

The chapter B introduction, p. 11, says: "Births and deaths which occurred prior to 8 XI 1948 but
were registered after that date were excluded from the calculation of the population estimates."
It gives this as the reason for the discrepancy between natural increase in the vital statistics
(chapter C, table 1) and in the sources of increase table, which it calls table 3; on p. 16 that
table is numbered 2. [V]

**Our reading:** this is why table 2 is the lower figure in every year, and the gap narrowing
from 227 in 1952 to 45 in 1956 fits fewer pre-1948 events being registered late as time went
on. CBS does not say the second part.

## 6. Natural increase rate

SA No. 14 (1963) chapter C, table 2, p. 66, prints the natural increase rate per 1,000 of the
average population, whole population (definition in the chapter C introduction, p. 63). [V]
The two right-hand columns are computed here: natural increase divided by the average population
in SA 2010 table 2.1, p. 85.

| Year | printed, p. 66 | computed from table 2, p. 16 | computed from table 1, p. 65 |
|---|---|---|---|
| 1952 | 25.68 | 25.3 | 25.5 |
| 1953 | 25.44 | 25.1 | 25.2 |
| 1954 | 22.45 | 22.2 | 22.3 |
| 1955 | 23.12 | 22.9 | 22.9 |
| 1956 | 22.18 | 22.0 | 22.0 |

The computed rates run about 0.2 per 1,000 below the printed ones, and the two computed columns
differ by up to 0.2 (1952). **Our reading:** the 1963 volume's own average population differs a
little from the 2010 series. The printed rate is the CBS figure.

**A caution about the other rate column.** SA No. 14 table 2, p. 16, has a column headed "Rate of
Increase", reading 32.8 for 1952, 24.5 for 1953, 29.0 for 1954, 41.5 for 1955 and 46.6 for 1956.
[V] Those are the rate of **total** increase, which includes migration, not the natural increase
rate, and the 1955 and 1956 values are far above the natural rate because those are the heavy
arrival years. They should not be read as the natural increase rate.

The sim's current `natural_increase_per_year` is 0.0169, that is 16.9 per 1,000. Every year in this
window is higher than that, between 22.18 and 25.68 printed. What, if anything, follows for the
rule value belongs to the paused half of #102 and is not proposed here.

## 7. Gaps

- Per-year arrivals by country of origin, finer than continent. [GAP] See section 1.

## 8. Noted while sourcing, outside this issue

`research_notes/M3 content 1950-51/facts.md` records "[GAP] The CBS 'sources of population growth'
table was not located (tried st02_04/05, both 404)" and "No natural-increase figure from CBS". The
table exists at
https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_03.pdf,
though for the 1950s it is grouped by period rather than by year. SA No. 14 (1963) table 2, p. 16,
does carry 1950 and 1951 as single years: natural increase 34,482 in 1950 and 40,322 in 1951, with
migration balance plus 161,741 and plus 167,409. [V] That is a change to another note and is not
made here.
