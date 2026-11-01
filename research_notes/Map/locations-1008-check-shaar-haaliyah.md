# Source check of the `shaar_haaliyah` row (#1008, fourth slice)

Status: **draft, needs a person to check**. Research only: an AI agent opened every source below on 28 October 2026.
`docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check each claim against its source. This note
changes nothing in `content/`, `sim/` or `unity/`: the row keeps `status: draft` and `name_he_review: agent-checked`, and no
`reviewed_by` is written (the owner's 22 October rule on #1008: only the person who did the check does that).

Scope: the one row `shaar_haaliyah` of [`content/map/locations_1948.yaml`](../../content/map/locations_1948.yaml) as on `main`
at `33ec5a95`, the last of the ten rows that had no source check. It follows the layout and method of the first two slices,
[`locations-1008-check-negba-hulda-nir-am.md`](locations-1008-check-negba-hulda-nir-am.md) (#1775) and
[`locations-1008-check-latrun-iraq-suwaydan-shaar-hagai.md`](locations-1008-check-latrun-iraq-suwaydan-shaar-hagai.md) (#1827),
and the third slice's note (open PR #1924, not linked because it is not on `main` yet), with their review lessons: quotes
verbatim with their quotation marks, "the agent" and not "I", a review agent's check credited as such, page images read where a
page image settles it. The rows of the other slices are not touched. Field meanings are from
[`docs/locations.md`](../../docs/locations.md).

## Conventions

- **[V, agent read 28 Oct 2026; a person's check pending]**: the printed or served text was opened by an agent on that day,
  as a page image or map tile that the agent looked at, or a computation that the agent re-ran, and says what the cell reports. For a book page or a dissertation page it
  vouches that the page prints the sentence, not that the sentence is right.
- **[S]**: Wikipedia, Wikidata or the web text of a JTA item (a compilation or a transcription, never primary), read by
  revision id or item id. Wikilink brackets and bold markup are removed from quoted wikitext, and square brackets inside a JTA
  quote mark letters lost at the scan edge.
- **[C]**: sources conflict; both values are kept.
- **[GAP]**: not reached or not found; what was tried is written next to it.
- **Result** column: *match* (the source supports the row), *conflict* (the source says something different),
  *not covered* (the source is silent), *not a row field* (the row has no such field; the fact is recorded for the owner).
- Hex conversion is the repo's own: `tools/hexgrid.py` `settlement_hex(lon, lat, grid)` over `content/map/hexes.json`
  (hex circumradius 5 km), run on 28 Oct 2026, plus `point_to_hex` for the hex that geometrically contains the point.
  "Edge" is the distance from the point to the nearest edge of the named hex, as the straight-line distance in the repo's
  equirectangular km plane.
- Distances between two coordinates are spherical (haversine) metres, rounded to 10 m; `pyproj` is not installed on this box.
  Positions read off the map are good to about 30 m.

## Summary

| Row | Row fields checked | match | conflict | not covered | Also recorded (not row fields) |
|---|---|---|---|---|---|
| `shaar_haaliyah` | name, name_he, kind, lon/lat, hex, historical_reference (6) | 6 | 0 | 0 | period names, operating dates, closing (3) |

No field of the row conflicts with a source. What the sources show that the owner may want to know:
(1) **Hex**: the row's stored hex (5, 8) equals what `settlement_hex` returns, but the point itself lies in hex (4, 8), which
`hexes.json` classes as `sea` (`land: false`), 1.64 km from the edge of (5, 8). The rule then places it on the nearest land hex
by centre distance: (5, 8) at 6.05 km, against 6.74 km for the sea hex (5, 7) and 8.62 km for the land hex (4, 9). So the row is
right under the repo's rule, and the camp is drawn on the hex east of the hex that holds the point;
(2) **Position**: Wikidata gives the row's point, and the 1940s Mandate map (sheet 14-24 Haifa) prints neither "camp" nor
"Sha'ar HaAliyah" there: it shows open ground with no printed building closer than about 120 m, a group of building marks about 330 m to the
north under the name "Et Tajadid", and "St. Luke's Sch." about 450 m east-south-east. The sheet is a Mandate-era map, so it predates the
Israeli camp and cannot confirm the name, and it does not say whether the building group is the British barracks that the
dissertation abstract and he.wiki (oldid 42247530) say the camp was built on; it does show that the point is on the Haifa coast plain, near a
printed "St. Luke's" name. No source read gives a coordinate for the camp other than Wikidata's;
(3) **Opening**: the row's `historical_reference: March 1949` matches the dissertation abstract the row cites ("opened in March,
1949 on the Haifa coast") and he.wiki (oldid 42247530), which gives 14 March 1949 for the first group. No item returned by the JTA API searches mentions the
camp (searches below), so the day is [S] only;
(4) **Names**: the row carries one name for a place that was also called "St. Luke's" (the British camp it replaced, per he.wiki oldid 42247530),
"Shaar Haaliya Processing Camp" (the entrance sign, per the dissertation abstract) and "Shaar Aliyah" and "Shaar Haaliya Camp"
(JTA). There is also a second camp, "Sha'ar Aliyah B", from 1951 (he.wiki oldid 42247530), which is not this row;
(5) the camp's role is attested by contemporaneous primary text: JTA (dateline 28 Dec 1949) reports UJA chairman Henry
Morgenthau Jr. as saying in New York that Shaar Aliyah was "the reception camp through which all new immigrants must pass";
The Jerusalem Post of 26 May 1950 reports a Jewish Agency announcement that physically fit unmarried immigrants of 17 to 35
"will be sent first to the Sha'ar Ha'aliya camp in Haifa". That fits the row's `kind: reception_camp`.

## Sources opened

| Id | Source | Day opened | Used for |
|---|---|---|---|
| WD1, WD2 | Wikidata item Q10395086 (label "Shaar HaAliya", Hebrew "שער העלייה"), `https://www.wikidata.org/wiki/Special:EntityData/Q10395086.json`, revision 2246322531; and item Q6004963 (label "Immigrant camps", Hebrew "מחנה עולים"), the value of its P31 | 28 Oct 2026 | the row's own cited coordinate source [S] |
| HW1 | he.wikipedia, `https://he.wikipedia.org/w/index.php?oldid=42247530&action=raw` (שער העלייה, revision of 25 Nov 2025), the row's cited `name_he` source | 28 Oct 2026 | name, kind, period names, dates [S] |
| EW1 | en.wikipedia, `Immigrant camps (Israel)`, `oldid=1368227199`; and the title `Shaar HaAliya`, which the API reports as missing | 28 Oct 2026 | negative record [S] |
| BGU | Rhona D. Seidelman, doctoral abstracts of Ben-Gurion University, July 2009, `https://www.bgu.ac.il/media/gedf3fo2/bguphd2009.pdf` (the row's second source), PDF page 930 of 1,010, her abstract "Shaar Haaliya: Contagion, Aliya and Quarantine during Israel's Mass Immigration 1949-1956" (page image, 100 dpi, whole page read) | 28 Oct 2026 | opening, former use, entrance sign [V, agent read 28 Oct 2026; a person's check pending] |
| MAP | Survey of Palestine 1:20,000 as served by Palestine Open Maps, tile layer `https://palopenmaps.org/tiles/pal20k-1940s/{z}/{x}/{y}.jpg`, zoom 16 (about 2.01 m per pixel at this latitude), 3 x 3 tiles (x 39130 to 39132, y 26436 to 26438) around the row point, row point marked with a red circle on the agent's copy; the agent looked at the whole mosaic and an enlarged crop | 28 Oct 2026 | printed names and positions [V, agent read 28 Oct 2026; a person's check pending] |
| SHEETS | Palestine Open Maps sheet index `raw-data/sheets.csv` at `PalOpenMaps/pom-data` commit `55fe58c45d53f7bce6045659e19286d30c544288` (the commit the earlier slices used) | 28 Oct 2026 | the point is on sheet 14-24 Haifa (listed twice, as `14-24-Haifa.jpg` and `15-24-Haifa.jpg`, same bounding box); also on the 1:100,000 Haifa sheet of 1958 and the 1:250,000 sheets |
| MAP250 | Palestine Open Maps tile layer `isr250k-1951` (Israel 1:250,000, 1951), zoom 12, 3 x 3 tiles around the point | 28 Oct 2026 | negative record: no label at the point [V, agent read 28 Oct 2026; a person's check pending] |
| JTA-A | JTA Daily News Bulletin, Thursday 29 December 1949 (vol. XVI no. 292 per the masthead, the number in the file name), `http://pdfs.jta.org/1949/1949-12-29_292.pdf`, page 3 of 6, item "American Jews Asked for Emergency Aid to Victims of Torrential Rains in Israel" (page image, 260 dpi, five crops read) | 28 Oct 2026 | the camp as the reception camp for all new immigrants, December 1949; the speaker is Henry Morgenthau Jr., "general chairman of the U.J.A.", per the item's opening sentence in the JTA web text (JTA-W, item 737666) [S] |
| JTA-B | JTA Daily News Bulletin, Tuesday 13 March 1951 (vol. XVIII no. 50 per the masthead), `http://pdfs.jta.org/1951/1951-03-13_050.pdf`, page 1, item "Reign of Terror in Bagdad as Mobs Attack Jews and Loot Shops; Emigrants Despoiled" (page image, 260 dpi, three crops read) | 28 Oct 2026 | the camp's name and place, March 1951 |
| JP-A | *The Jerusalem Post*, issue printed "FRIDAY, MAY 26, 1950", "PAGE THREE", `https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1950-05-26_26_7264` (PDF page 3, page image, 200 dpi, crop of the item and of its headline) | 28 Oct 2026 | the camp as first stop for young immigrants, May 1950 |
| JP-B | *The Jerusalem Post*, issue printed "SUNDAY, OCTOBER 29, 1950", "PAGE TWO", `https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1950-10-29_26_7391` (PDF page 2, page image, 200 dpi, crop of the paragraph) | 28 Oct 2026 | Kupat Holim examinations at the camp, August 1950 |
| JP-OCR | The `_djvu.txt` OCR files of 209 *Jerusalem Post* issues of 1950 (details under "Searches run") | 28 Oct 2026 | negative and positive search record; OCR text only [S] |
| JTA-W | JTA wp-json items 737666 (29 Dec 1949) and 742663 (13 Mar 1951), web text only | 28 Oct 2026 | the same two passages as JTA-A and JTA-B, found by search [S] |

Not reached or not used: the National Library of Israel (the *Davar* articles that he.wiki (oldid 42247530) cites for 1950, 1951, 1953 and 1962,
and the 1956 map of west Haifa) answered HTTP 403 with a "Checking your browser" page from this box on 28 Oct 2026, with a
browser User-Agent as well; the two footnotes that he.wiki (oldid 42247530) prints at the end of its paragraph with 14 March 1949 and the 200 dunams, Segev's
*1949: The First Israelis* (p. 124) and Seidelman's *Under Quarantine* (2020), neither opened; the AJS Perspectives article by Seidelman (the connection
failed); the CBS list of boundary changes 1948-2004 that he.wiki (oldid 42247530) cites for 1958; the Israel State Archives and the Israel
Land Administration plan database (not tried). Where a cell quotes a claim that rests on a source the agent did not open, it is
marked as a Wikipedia claim.
On 29 Oct 2026 the agent asked the he.wikipedia API (`prop=revisions`, `rvdir=older`, `rvlimit=1`, `rvstart` 28 Oct 2026 23:59:59 UTC, the day the note was opened and first committed, and again at 29 Oct 2026 00:12:33 UTC, the date of the last commit) for the revision of "שער העלייה" current on those dates. Both return oldid 42247530 (25 Nov 2025; the previous revision is 40918987 of 24 Apr 2025), so every he.wiki claim in this note is read in that one revision. The quoted and cited text of the sections "Summary" and "Sources opened" above (the conversion of the British military camp, "St. Luke's", 200 dunams, 14 March 1949, the Segev and Seidelman footnotes, "Sha'ar Aliyah B" in 1951, the Davar articles of 1950, 1951, 1953 and 1962, the 1956 map, 1958) was found in oldid 42247530. The en.wikipedia claim is pinned at oldid 1368227199 (7 Aug 2026), the revision current on 28 Oct 2026 per the same query.

## `shaar_haaliyah` (row: `Sha'ar HaAliyah`, `שער העלייה`, reception_camp, lon 34.95745, lat 32.8160806, q 5, r 8, `historical_reference: March 1949`)

| Field | Row says | Source says | Result | Tag | Source | Read |
|---|---|---|---|---|---|---|
| name | `Sha'ar HaAliyah` | Wikidata English label "Shaar HaAliya", no English alias. he.wiki: "שמו הרשמי היה "מחנה שער העלייה", אך רבים קראו למקום שנים רבות גם "סנט לוקס"" (its official name was "Camp Sha'ar HaAliyah", but many also called the place "St. Luke's" for years). Dissertation abstract: "The sign at the entrance read "Shaar Haaliya Processing Camp"". JTA-A prints "Shaar Aliyah", JTA-B "Shaar Haaliya Camp", JP-A "Sha'ar Ha'aliya" (the apostrophe after "Ha" is smudged on the scan), JP-B "Sha'ar Ha'aliya reception camp near Haifa". The Mandate map prints no name at the point | match; the row's spelling is one of several transliterations of the same name, all the same place | [V, agent read 28 Oct 2026; a person's check pending] for BGU, JTA-A, JTA-B, JP-A, JP-B; [S] for WD1 and HW1 | WD1, HW1, BGU, JTA-A, JTA-B, JP-A, JP-B | 28 Oct 2026 |
| name_he | `שער העלייה` | Wikidata Hebrew label "שער העלייה". he.wikipedia title "שער העלייה"; its first sentence: "שער העלייה היה יישוב קליטה ומחנה מיון לעולים בכניסה לעיר חיפה" (Sha'ar HaAliyah was a reception settlement and a sorting camp for immigrants at the entrance to the city of Haifa) | match | [S] | WD1, HW1 | 28 Oct 2026 |
| kind | `reception_camp` | Wikidata P31 = Q6004963, label "Immigrant camps" (Hebrew "מחנה עולים"); Q10395086's own Hebrew description "יישוב קליטה ומחנה עולים בישראל" (Q6004963's own reads "מקום ישוב זמני של העולים החדשים לישראל"). he.wiki: "ששימש כמחנה ממיון מרכזי לעולים שהגיעו לארץ בעלייה ההמונית" (which served as a central sorting camp for immigrants who came in the mass immigration). JTA-A (item datelined "NEW YORK, Dec. 28. (JTA)"): "He added that in Shaar Aliyah, the reception camp through which all new immigrants must pass, all tents have collapsed as a result of heavy storms, and the people are being crowded into huts to get some shelter." JP-B: "the Sha'ar Ha'aliya reception camp near Haifa". Dissertation abstract: "an enclosed camp equipped to receive large-scale immigration" | match | [V, agent read 28 Oct 2026; a person's check pending] for BGU, JTA-A, JP-B; [S] for WD1, WD2 and HW1 | WD1, WD2, HW1, BGU, JTA-A, JP-B | 28 Oct 2026 |
| lon/lat | 34.95745, 32.8160806 | Wikidata P625 32.81608056, 34.95745 (stated precision 1e-05 degrees; 0 m from the row, the row rounds the latitude to 7 decimals). Wikidata P131 (located in) = Q12034828, "Haifa West". he.wiki prints no coordinate in its text; its infobox has the map template `{{ממי}}` with no figures, and "עיר = חיפה". en.wiki has no article. Mandate map (sheet 14-24 Haifa): the red row point is in open ground east of the orange main road that runs north-south, with no printed building closer than about 120 m (the nearest blue building mark is at 32.8169, 34.9583, 120 m north-east); a group of about 15 building marks lies about 330 m north (bearing about 356 degrees) (centre about 32.8191, 34.9572), with the name "Et Tajadid" printed at about 32.8182, 34.9566, 250 m north-north-west; "St. Luke's Sch." is printed about 450 m east-south-east (about 32.8154, 34.9622); the printed mark "KM6" lies on the road about 700 m north of the point (bearing about 348 degrees) and "KM7" about 330 m south-south-west of it. 1:250,000 sheet of 1951: no label at the point | match (Wikidata, the row's coordinate source, equals the row; the map is consistent with a point on the Haifa coast plain but is Mandate-era, so it neither names nor contradicts the camp's site) | [S] for Wikidata; [V, agent read 28 Oct 2026; a person's check pending] for the map readings | WD1, HW1, MAP, MAP250 | 28 Oct 2026 |
| hex | q 5, r 8 | `settlement_hex` on the row point gives (5, 8), 6.05 km from that hex's centre. The point lies in hex (4, 8) (centre 34.92915, 32.80736; 2.86 km away, 1.64 km from the edge shared with (5, 8)), which `hexes.json` lists as `land: false`, `biome: sea`, `elev_m: 0.0`. The land neighbours are (5, 8) `coastal_plain` at 6.05 km and (4, 9) `coastal_plain` at 8.62 km; (5, 7) is sea at 6.74 km. The map-read building group (32.8191, 34.9572) gives the same: contained in (4, 8), placed in (5, 8) | match under the placement rule (the stored hex equals the returned one); the point is not inside it | [V, agent read 28 Oct 2026; a person's check pending] (computation) | tools/hexgrid.py | 28 Oct 2026 |
| historical_reference | `March 1949` | Dissertation abstract: "Shaar Haaliya was opened in March, 1949 on the Haifa coast. Established on the foundations of what had formerly been British army barracks, Shaar Haaliya was an enclosed camp equipped to receive large-scale immigration during the mass immigration that came with the establishment of the state of Israel." (the page image prints "March"; the PDF text layer spaces it "M arch"). he.wiki: "במרץ 1949 הוסב המחנה הצבאי הבריטי "סנט לוקס" למחנה לקליטת העולים בשטח של 200 דונם. 353 העולים באונייה "קדמה" שהגיעה ממרסיי היו הראשונים שהגיעו למחנה ב-14 במרץ 1949." (in March 1949 the British military camp "St. Luke's" was converted into an immigrant reception camp on 200 dunams; the 353 immigrants of the ship "Kedma" from Marseille were the first to arrive at the camp on 14 March 1949; he.wiki's two footnotes at the end of that paragraph are Segev's book (p. 124) and Seidelman's *Under Quarantine*; neither opened). Wikidata P571 "+1949-00-00" (year precision). No item returned by the JTA API searches below names the camp | match for the month; the day, 14 March, is [S] only | [V, agent read 28 Oct 2026; a person's check pending] for BGU; [S] for HW1, WD1 | BGU, HW1, WD1, JTA-W | 28 Oct 2026 |
| period / other names | none in the row | The British name was "St. Luke's": he.wiki "הוסב המחנה הצבאי הבריטי "סנט לוקס"" and, in its footnote, the Israel Land Administration plan database lists plan חפ/במ/1948 with the place "סנט לוקס" (the agent did not open that database). The Mandate map prints "St. Luke's Sch." 450 m east-south-east of the row point and no camp name; whether the camp took its name from the school is not stated by any source read. The English entrance-sign name is "Shaar Haaliya Processing Camp" (abstract). he.wiki also names "שער עלייה ב'" (Sha'ar Aliyah B): in early 1951 the families of the Atlit ma'abara were moved out and the Atlit camp became a second reception camp of that name (a different place, not this row). No Arabic name is printed on the map at the point or given by Wikidata | not a row field; no conflict with the row's name | [V, agent read 28 Oct 2026; a person's check pending] for BGU and MAP; [S] for HW1 | HW1, BGU, MAP | 28 Oct 2026 |
| operating dates (other than opening) | none in the row | **1949:** JTA-A, bulletin of 29 Dec 1949, item datelined "NEW YORK, Dec. 28. (JTA)": the camp is "the reception camp through which all new immigrants must pass", its tents collapsed in the storms, and "Eleven hundred new immigrants arrived yesterday" (27 December, by the dateline; that sentence is about the reception center, which is the camp named just before it). **1950:** JP-A, issue of Friday 26 May 1950, item datelined "TEL AVIV, Thursday." (25 May, by the issue date), headline "New Scheme For Young Immigrants": "Physically fit unmarried men and women from 17 to 35 years of age will not be accepted at immigrant camps in future, the Jewish Agency announced today. All will be sent first to the Sha'ar Ha'aliya camp in Haifa, where they will be given the choice of entering a work camp, or joining a kibbutz or moshav, unless they are able to strike out for themselves without assistance." JP-B, issue of Sunday 29 Oct 1950: "During August 11,915 new immigrants were examined by Kupat Holim upon their arrival at the Sha'ar Ha'aliya reception camp near Haifa. Newcomers can be examined at the rate of 1,000 per day. During the past two years 140,000 new immigrants have been examined by the sick fund's T.B. minograph at the camps." (the sentence about two years is about all the camps, not this one). **1951:** JTA-B, item datelined "TEL AVIV, March 12. (JTA)": "The refugees arriving here last night came in six planes. They were immediately taken to the Shaar Haaliya Camp, near Haifa." (Iraqi airlift; "last night" is the night before 12 March, by the dateline). **Later [S]:** he.wiki: from 1952 to 1960 a large centre for treating children with ringworm; in 1958 the area joined Haifa; "בתחילת שנת 1962 נסגר המחנה והעולים האחרונים ששהו בו הועברו לטיפול עיריית חיפה" (at the beginning of 1962 the camp was closed and the last immigrants who stayed there were handed to the Haifa municipality) | not a row field; supports `reception_camp` for 1949 to 1951, and the row's `historical_reference` is only the opening | [V, agent read 28 Oct 2026; a person's check pending] for JTA-A, JP-A, JP-B, JTA-B; [S] for HW1 | JTA-A, JP-A, JP-B, JTA-B, HW1 | 28 Oct 2026 |

## Searches run (so others can repeat them)

JTA: all through `https://www.jta.org/wp-json/wp/v2/archive?search=<query>&after=<from>T00:00:00&before=<to>T23:59:59&per_page=100&page=1`,
run 28 Oct 2026 with a script (the tool is the agent's own Python, not the WebSearch tool). "Items returned" is capped at 100 by
`per_page`; no query returned 100, so none has a second page. "Items whose text matched" counts a case-insensitive pattern on
the full web text of each returned item (title and body). The API matches loosely, so a count of zero is a count of this API, not of the archive.

| Query | Range | Items returned | Items whose text matched | First to last matched | Pattern |
|---|---|---|---|---|---|
| Shaar Haaliyah | 1 Jan to 31 Dec 1949 | 1 | 1 | 29 Dec 1949 (item 737666) | `sha.?ar` or `st.? luke` |
| Shaar Haaliya | 1 Jan to 31 Dec 1949 | 0 | 0 | none | the same |
| Shaar Aliyah | 1 Jan to 31 Dec 1949 | 1 | 1 | 29 Dec 1949 (item 737666) | the same |
| St. Luke's | 1 Jan to 31 Dec 1949 | 0 | 0 | none | the same |
| Haifa | 1 to 31 Mar 1949 | 24 | 0 | none | the same |
| camp | 1 to 31 Mar 1949 | 60 | 0 | none | the same |
| immigrants | 10 to 18 Mar 1949 | 20 | 0 | none | the same |
| Shaar Haaliyah | 1 Jan 1950 to 31 Dec 1952 | 1 | 1 | 13 Mar 1951 (item 742663) | the same |
| Shaar Haaliya | 1 Jan 1950 to 31 Dec 1952 | 1 | 1 | 13 Mar 1951 (item 742663) | the same |
| Shaar Aliyah | 1 Jan 1950 to 31 Dec 1952 | 0 | 0 | none | the same |

Earlier runs of the same day, with the looser pattern `sha.?ar|saar|shar ha|sha'ar` or `reception camp|processing camp|kedma|kadima|st.? luke`, returned the same two items
for "Shaar Haaliyah" (1949, 1950 to 1952) and nothing for "Shaar Aliyah", "Shaar Haaliah", "Shaar Ha'aliyah", "Sha'ar Ha'aliyah" and "Shaar
Hallyah" (each 0 items, 1 Jan to 30 Apr 1949), "Haifa reception camp" (8 items, 0 matched) and "Haifa processing camp" (5 items, 0 matched) for 1 Jan to
30 Apr 1949; "Kedma", "Kadima", "Kadimah" and "immigrants Marseilles Haifa" for 8 to 20 Mar 1949 returned 0 items each. "Kedmah", 1 Jan to
30 Apr 1949, returned 1 item (18 Apr 1949, item 734067), whose text mentions the ship "Kedma" arriving in Haifa with 500 immigrants; the agent read
only the matched passage of it, not the bulletin page, and it does not name the camp. The two matched items were then opened as bulletin
pages (JTA-A, JTA-B).

*Jerusalem Post* OCR: the identifiers came from `https://archive.org/advancedsearch.php?q=identifier:per_jerusalem-post*+AND+date:[1950-01-01+TO+1950-12-31]&rows=500&output=json`
(209 issues, 23 Apr 1950 to 31 Dec 1950); the `_djvu.txt` file of all 209 was downloaded (0 failures) and scanned with
`sha.?ar ?ha.?al|shaar ?ali|sha.ar-ha|st\.? luke` (12 issues matched; the lines the scan printed for the St. Luke pattern are a church, a school or a street) and with a looser pattern
`sha.?.?ar.{0,6}ha.{0,3}ali|sha.?.?ar.{0,3}alij|sha.?ar.?ha.?a|reception camp.{0,60}haifa|haifa.{0,60}reception camp|processing camp`. The looser pattern hit the issues of 26 May,
20 Jun, 17 Jul, 31 Aug, 27 Sep, 29 Oct and 21 Dec 1950 for this camp (the OCR of these columns is noisy and runs lines of neighbouring columns
together) and those of 30 Oct and 8 Nov 1950 for Sha'ar HaGai, a different place. Only the 26 May and 29 Oct issues were opened as page images (JP-A, JP-B);
the others are OCR hits not read on the page, and 1950 before 23 April, and 1951, were not scanned. The camp's opening month, March 1949, is before the first issue in that listing, and the earlier note
(`research_notes/Map/camps-1948-51.md`, source JP5) gives 23 April 1950 as the first issue of the archive.org run it used, so no *Jerusalem Post* text of 1949 was scanned.
