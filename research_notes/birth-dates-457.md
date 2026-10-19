# Exact birth dates for Kaplan, Pinkas, Geri, Remez and Maimon: primary-source search (AI draft, research note, 2026-10-19)

Status: **AI draft, research only.** Claude (an AI assistant) ran this search for Beri, a contributor, on 2026-10-19. Nothing here changes `content/`: no birth value, no review flag, no office term, no golden. Refs #457 (the owner's "Available now" comment of 19 October 2026, which asks for the five `biography_sources.born` fields that #720 left on Wikipedia).

**No `checked:` date is earned by anything below.** Claude read these pages; a person has not. Where a page supports a day, a person still has to open it and record their own check date before it goes into `content/`.

## Result in one table

Checked against `origin/main` as fetched on 2026-10-19. All five `born` fields cite only Wikipedia (`checked: 2026-10-11`).

| Person | Current `born` | Current source | Primary found? | Supports the exact day? |
| --- | --- | --- | --- | --- |
| kaplan | 1891-01-27 | en.wiki Eliezer_Kaplan | No | No. Knesset page gives year only (1891) |
| pinkas | 1895-12-05 | en.wiki David-Zvi_Pinkas | No | No. Knesset page gives year only (1895) |
| geri | 1901-09-18 | en.wiki Yaakov_Geri | No | No. Knesset page has no birth field at all, death only |
| remez | 1886-05-23 | en.wiki David_Remez | No | No. Knesset page gives year only (1886) |
| maimon | 1875-12-11 | he.wiki יהודה_לייב_פישמן_מימון | A lead, not opened | Not shown. A 1925 jubilee volume titled for "12 Kislev 686" points to a birthday of 12 Kislev, but its catalogue record was reached only through a relay and it converts to **10 December**, not 11 (see below) |

Recommendation: keep the named Wikipedia citation on all five. No source tested here is good enough to replace it.

## What could not be reached, and why that matters

The planned first step, a JPress pass over contemporary biographical notices and obituaries, **was not possible from this machine**. This is the main finding, so the note is "not found" because the source was closed, not because the search came back empty.

| Source | Result from this server (AWS, outside Israel) |
| --- | --- |
| JPress (`jpress.org.il`, now redirecting to `https://www.nli.org.il/he/discover/newspapers/jpress`) | HTTP 403, Cloudflare "Checking your browser" challenge, for both curl and the fetch tool. The search itself is JavaScript-driven. A Wayback copy of the landing page exists, but page images and search results do not archive. |
| NLI catalogue and authority records (`nli.org.il`) | Same Cloudflare 403. |
| Knesset OData `KNS_Person` and the new member pages (`main.knesset.gov.il/.../mk-print/<id>`) | HTTP 200 but the body is a geo-block notice ("The site is temporarily inaccessible from your location"). Wayback copies of the new pages from 2024 are the same notice. |
| HathiTrust catalogue record 103189517 | HTTP 403. |
| VIAF | Cloudflare challenge. |

So the next person with an Israeli network or a browser that passes the challenge can search JPress directly. Suggested terms, from the dates in `content/`: for each person, the Hebrew name plus "יום הולדת" or "נולד" in the paper of the day around the date, and the 1951 and 1952 obituaries (Remez died 19 May 1951, Kaplan 13 July 1952, Pinkas 14 August 1952, Maimon 10 July 1962, Geri 18 December 1974). Birthday notices for sitting ministers (1948 to 1952) are the likeliest exact-day hit.

## What was opened

Wayback copies of the old Knesset member pages, the same URLs that already carry `died` on these rows (Claude opened each; the page text is windows-1255 and is rebuilt by script, so a person should open it in a browser). The "birth" row is the Knesset's `לידה:` label, which in the page's markup is the birth **year** field.

| Person | URL | What the page prints for birth | Printed death |
| --- | --- | --- | --- |
| kaplan | https://web.archive.org/web/20170930230605/http://www.knesset.gov.il:80/mk/heb/mk.asp?mk_individual_id_t=606 | `1891` (year only) | `13/07/1952` |
| pinkas | https://web.archive.org/web/20170930193308/http://www.knesset.gov.il:80/mk/heb/mk.asp?mk_individual_id_t=571 | `1895` (year only) | `14/08/1952` |
| remez | https://web.archive.org/web/20170930225823/http://www.knesset.gov.il:80/mk/heb/mk.asp?mk_individual_id_t=628 | `1886` (year only) | `19/05/1951` |
| maimon | https://web.archive.org/web/20170930193508/http://www.knesset.gov.il:80/mk/heb/mk.asp?mk_individual_id_t=503 | `1875` (year only) | `10/07/1962` |
| geri | https://web.archive.org/web/20160303225240/http://www.knesset.gov.il/mk/heb/mk.asp?mk_individual_id_t=698 | no birth field printed | `18/12/1974` |

This agrees with the coverage finding in #720: the old Knesset pages give an exact death date and, at most, a birth year. A year alone does not support an exact day, so these rows are all "No".

The current English Wikipedia article on Kaplan cites one source for his birth sentence, the Knesset profile (`https://main.knesset.gov.il/en/MK/APPS/mk/mk-print/606`). The infobox date, 27 January 1891, has no citation marker next to it. That page could not be opened here (geo-block above), so whether it prints a full date is **not reached**, and is worth one look from Israel. The same check applies to the equivalent pages for the other four (ids 571, 628, 503, 698).

## Per person

### kaplan (1891-01-27)
- Primary for the day: not found. Knesset Wayback page gives `1891` only.
- Searched and seen, none primary: Bizportal (https://www.bizportal.co.il/people/news/article/20042442, its headline is tagged "גרוק", so it was not used), Jewish Virtual Library and Encyclopaedia Judaica (give the year and place, not the day), the AIM25 authority record (year range 1891-1952). Nothing contradicts 27 January.

### pinkas (1895-12-05)
- Primary: not found. Knesset page `1895`. #720 already records that the 15 August 1952 JTA bulletin gives an age, not a date.
- Seen, secondary only: Wikidata Q2587814, Austria-Forum (derives from Wikipedia), Prabook. They repeat en.wiki and are not independent.

### geri (1901-09-18)
- Primary: not found. He was never a Knesset member and the old page has no birth line. Other candidate records (Israel State Archives, Histadrut or Haifa municipal records, JPress notices from his period as Minister of Trade and Industry, 1950 to 1951) were not reachable.
- Related: en.wiki puts the birthplace at Seda, Russian Empire, and says he studied in South Africa. Neither detail has been checked.

### remez (1886-05-23)
- Primary: not found. Knesset page `1886`.
- Seen: Jewish Virtual Library (Encyclopaedia Judaica text) gives the year only; the BnF authority record gives 1886-1951; Times of Israel / Israel Story give the year. None gives the day.

### maimon (1875-12-11): conflict, flagged
Existing record (`research_notes/person-biographies.md`, lines 42-43): en.wiki 1 January 1875; he.wiki 11 December 1875 (12 Kislev 5636); Wikidata 11 December; Congress for Jewish Culture dictionary 12 December 1875; JTA obituary says age 87 only.

New in this pass:

1. **A contemporary title that dates a 50th birthday.** The University of Pennsylvania Online Books Page (https://onlinebooks.library.upenn.edu/webbin/who/Maimon%2c%20Judah%20Leib%2c%201875%2d1962) lists, under him, *Sefer ha-yovel le-Rabi Yehudah Leb ha-Kohen Fishman : bi-melʾet lo ḥamishim shanah (12 Kislev, 686)*, Histadrut ha-Mizraḥi ha-ṣaʻir, 1925, and links https://catalog.hathitrust.org/Record/103189517. A jubilee volume "on his reaching fifty" dated 12 Kislev 5686 implies he was born on 12 Kislev 5636. **Not opened:** the HathiTrust record returned 403 here, so the title is a transliteration relayed by the Penn index, not the book or its catalogue entry. A person should open the HathiTrust record and, if possible, the title page.
2. **The conversion does not give 11 December.** Hebcal's converter (https://www.hebcal.com/converter, queried 2026-10-19, Gregorian dates) gives 12 Kislev 5636 = **10 December 1875**, and 11 December 1875 = 13 Kislev. (Hebcal returns Gregorian dates; a Julian-calendar reading would move the day by twelve days and fits none of the candidates.) So the Hebrew date in he.wiki and the Gregorian day in `content/` do not match each other by this converter. That is Claude's calculation, not a source's statement. It leaves 10 December, 11 December and 12 December (the Congress for Jewish Culture value) all unsettled.
3. Not a primary record: Hamichlol (the Hebrew wiki mirror) repeats the he.wiki line "12 Kislev 5636, 11 December 1875".

Flag for the owner, not a change: the one contemporary item found points to **12 Kislev**, which agrees with the Hebrew date in the current source but not with the Gregorian day. Do not change `born` on this basis.

Secondary items named above (Jewish Virtual Library, AIM25, BnF, Wikidata, Austria-Forum, Prabook, Hamichlol, Times of Israel / Israel Story, and the en.wiki details for Geri and Kaplan) were seen in search results by the author and not opened in full or re-checked by the reviewer; no conclusion rests on them.

## What a person must check

1. Open JPress from a network and browser that passes the Cloudflare challenge. Search each name for an exact-day birthday notice or obituary, and record newspaper, date and page for any hit. A hit that gives only an age does not count.
2. Open the Knesset's current member page for each of ids 606, 571, 628, 503 and 698 (`https://main.knesset.gov.il/en/MK/APPS/mk/mk-print/<id>`, geo-blocked from this server) and see whether it prints a full birth date. If it does, that is the primary the audit wanted, and it can replace the Wikipedia citation with a person's own `checked:` date.
3. Open the HathiTrust record 103189517 for the Maimon jubilee volume and confirm the title reads "12 Kislev 686" (5686).
4. Decide the Maimon Gregorian day (10, 11 or 12 December 1875) once step 3 and a Hebrew-to-Gregorian check are done by a person.
5. Confirm that every cited Wayback URL above shows the quoted text in a browser (the page text here was decoded from windows-1255 by script).

## Wording note

Everything above was read by Claude, an AI assistant. Nothing here is a first-person claim by a human reviewer, and no source has had a human `checked:` date applied.
