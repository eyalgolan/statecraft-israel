# City start values, 1948-51 (research note, 2026-10-14)

Status: research only, for issue #383 (GDD Phase 3, "all Jewish cities with start values"). Nothing here is in `content/`. Which cities
exist in the MVP, and every balance value, are the owner's calls (D-D1 to D-D4, D-D8 in #375). This note gives the sourced
facts, the conflicts and the gaps, so that `cities.yaml` can label each value "historical, sourced" or "design".

## How to read this note

- **[V]** read on the page named, on 2026-10-14. **[S]** a figure from a Wikipedia infobox or table that I read, not checked
  against its own source. **[C]** two sources disagree. **[G]** gap: no source reached.
- Dates are as the source gives them. Figures are residents unless said otherwise.
- I did not open the CBS Statistical Abstracts of 1949/50 to 1952 (not found as readable text) or the 1948 census volume
  "Towns, Villages, Regions" (Global Census Archive, paywalled). Those are where the per-city series should come from.

## Summary

1. **Only four city figures for the census day are from the CBS.** On 8 November 1948: Tel Aviv-Yafo 248,500, Jerusalem 84,000,
   Haifa 98,600, Rishon LeZiyyon 11,100 [V, CBS 2006 release]. Tel Aviv was the only city above 100,000.
2. **Nine more 1948 town figures are Wikipedia-only [S]:** Rehovot 12,500, Hadera 11,800, Netanya 11,600, Bnei Brak 9,300,
   Ramat Gan 17,200, Herzliya about 5,300 (at 14 May 1948), Kfar Saba about 5,500 (May 1948), Bat Yam 2,300, Nahariya 1,700.
3. **Growth is fast and the sources differ in date.** A 1952 US Census Bureau report puts the Jewish population at the end of
   1950 at about 330,000 for Tel Aviv-Jaffa, 122,000 for Haifa and 120,000 for Jerusalem [V]. Against the census-day figures
   that is +33% in Tel Aviv, +24% in Haifa and +43% in Jerusalem in two years [C on boundaries: see 4].
4. **Boundary and population-type caveat.** The 1948 census counted the de facto population, Arabs included; the 1952 report
   counts Jews. Tel Aviv-Yafo in 1948 includes Jaffa. A game value must say which it is.
5. **Housing capacity per city is not in any source I reached [G].** What exists is national: transit housing.

## Sourced anchors

### National, for the calibration check

`content/population.yaml` already carries the CBS series (Jews 716,700 on the registration date). The city values must sum
below it, minus rural and kibbutz residents (about 6% of Israel's population lived on kibbutzim at the founding of the state,
CBS 2006 release [V]).

### Four CBS city figures, 8 November 1948

| City | Residents, 8 Nov 1948 | Source |
|---|---|---|
| Tel Aviv-Yafo | 248,500 | [V] https://www.cbs.gov.il/he/mediarelease/DocLib/2006/092/11_06_092e.pdf, "Residents of Large Localities" table and footnote 3 |
| Jerusalem | 84,000 | same |
| Haifa | 98,600 | same |
| Rishon LeZiyyon | 11,100 | same |

The footnote says the figures are as of 8 November 1948, the census date.

### End of 1950, the three largest cities

"The largest city in Israel is Tel Aviv-Jaffa, with 330,000 population ... The two other large cities, Haifa and Jerusalem,
have 122,000 and 120,000 Jewish inhabitants, respectively." About half the Jewish population lived in three cities of 100,000
or more at the end of 1950. 572,000 in cities of 100,000+ (49.2%), 249,000 in settlements of 10,000-100,000 (21.4%),
118,000 in smaller ones (10.1%). [V] US Census Bureau, "Israel: Jewish population and immigration" (1952), section "Geographic distribution":
https://digitalcommons.fau.edu/cgi/viewcontent.cgi?article=1307&context=prism . It cites the CBS Statistical Bulletin of Israel
(April-September 1951). The text of its table 6 did not extract, so no per-city figures beyond the sentence above [G].

### Town figures for 1948 [S]

Read from the Wikipedia infobox history table of each article on 2026-10-14. They have not been checked against the cited
source of each table.

| Town | 1948 | Page |
|---|---|---|
| Ramat Gan | 17,200 | https://en.wikipedia.org/wiki/Ramat_Gan |
| Rehovot | 12,500 | https://en.wikipedia.org/wiki/Rehovot |
| Hadera | 11,800 | https://en.wikipedia.org/wiki/Hadera |
| Netanya | 11,600 | https://en.wikipedia.org/wiki/Netanya |
| Bnei Brak | 9,300 | https://en.wikipedia.org/wiki/Bnei_Brak |
| Kfar Saba | about 5,500 (May 1948) | https://en.wikipedia.org/wiki/Kfar_Saba |
| Herzliya | about 5,300 (14 May 1948) | https://en.wikipedia.org/wiki/Herzliya |
| Bat Yam | 2,300 | https://en.wikipedia.org/wiki/Bat_Yam |
| Nahariya | 1,700 | https://en.wikipedia.org/wiki/Nahariya |

Petah Tikva, Holon, Beersheba, Nazareth, Acre, Tiberias, Safed and Afula had no 1948 figure I could read in their articles [G].
Lod, Ramla, Ashkelon (Majdal) and Beersheba were Arab towns until 1948; their Jewish populations start in 1948-49 and
cannot be given as a 1948 value [G]. Sensitivity: `docs/CONTENT_POLICY.md` and the plan §4 apply to any card about them.

## Housing: what is sourced

All Wikipedia unless noted [S], read 2026-10-14 from https://en.wikipedia.org/wiki/Ma%27abarot :

- The first ma'abara (Kesalon) became functional in May 1950 with 150 families. This matches the date in `content/rules.yaml`
  (lines 10-11), which cites the same page.
- In July 1950 about 70,000 immigrants were moved into ma'abarot, and a further 50,000 the next year.
- "In 1951 there were 127 Ma'abarot housing 250,000 Jews" [C]: the same article says the numbers began to fall from 1952,
  and the US report gives 26,039 residents of immigrant camps and reception centres at the end of 1951 [V, same report]. These count
  different things (ma'abarot versus reception camps) and I did not find the date of the 250,000 figure [G].
- The batei olim (immigrant huts) were already at capacity when ma'abarot opened.

For game use this supports D-D3 (a transit camp from May 1950, a cheap low-condition type). No per-city count of ma'abara
residents was found [G].

## Conflicts

1. Jerusalem: 84,000 (CBS, 8 Nov 1948) against 120,000 Jewish (end of 1950). Part of the gap is two years of growth; the
   boundary of Israeli Jerusalem is not given in either source [C].
2. Ma'abarot: 250,000 (1951) against 26,039 in camps and centres (end of 1951). Different categories; unresolved.

## Open questions for the owner

1. Which cities does the MVP include? The issue says "all Jewish cities" and the plan warns against micromanaging 25 cities. The
   sourced start values above would cover about 13 towns, of which only four are CBS.
2. Population type: residents (the census count) or Jews only, for a city's start value?
3. Is a start value allowed to be a rounded Wikipedia figure, or must every city value be CBS? If CBS only, the 1949/50 and 1951
   Statistical Abstracts must be read first.
4. Housing capacity per city has no source. Should it be labelled a design value (derived from population) in `cities.yaml`?

## Not reached

- CBS Statistical Abstract of Israel No. 1 (1949/50) to No. 3 (1952): catalogue records only (HathiTrust, CiNii), no text.
- 1948 census "Towns, Villages, Regions": Global Census Archive, paywalled.
- CBS "Atlas 70" chapter 1 (the 1948 census): the PDF returned only a language table, no population by locality.
- Hebrew Wikipedia and the Hebrew press (National Library): not tried.

## Sources

- CBS, "On the eve of Israel's 58th Independence Day" (2006): https://www.cbs.gov.il/he/mediarelease/DocLib/2006/092/11_06_092e.pdf
- US Bureau of the Census, "Israel: Jewish population and immigration" (1952): https://digitalcommons.fau.edu/cgi/viewcontent.cgi?article=1307&context=prism
- Wikipedia pages listed above, read 2026-10-14.
