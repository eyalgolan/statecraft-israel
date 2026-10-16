# Sub-district names in Hebrew and Arabic, and the 1947-48 amendment check (research note, 2026-10-16)

Status: research only. No content file changes. It feeds two of the open questions in
`content/map/subdistricts_1948.geojson` (`statecraft.open_questions` 1 and 2) and the last open items on #196: "the
sub-district names, and the 1947-48 amendment check". The armistice-line sensitivity review is the owner's and is
not touched here.

## Conventions

- **[V]** read on 2026-10-16 on the scanned Gazette page itself (the page image, not the OCR text).
- **[S]** a search snippet (OCR text returned by the search tool) or a single secondary source. Not a page I read.
- **[C]** sources conflict. Every value is given.
- Every page below comes from the **Israel State Archives' scanned Palestine Gazette**, which the Archives publish as
  a Google Pinpoint collection: "העיתון הרשמי ממשלת המנדט 1921-1948- ארכיון המדינה", collection id
  `7ded851a300df5a6`. The collection's own description, read [V]: "האוסף מכיל כ 160,000 עמודים סרוקים / The
  Palestine gazette in Hebrow [sic] / Israel State Archives". Search page:
  https://journaliststudio.google.com/pinpoint/search?collection=7ded851a300df5a6 (checked 2026-10-16).
  A document is cited by its title in the collection (it carries the ISA file number, for example `מ-7138-2`), its
  Pinpoint `docid` and the 1-based scan page `p`, as
  `https://journaliststudio.google.com/pinpoint/document-view?collection=7ded851a300df5a6&docid=<docid>&p=<p>`.
  The printed Gazette page is given as well, so a reader with a paper copy or another scan can find the same page.
- The Archives' own catalogue (https://catalog.archives.gov.il/site/chapter/pghl/) returned HTTP 403 to both a
  scripted request and the fetch tool on 2026-10-16, so the Pinpoint copy is the route used.
- Nothing was logged into or submitted. No scan is committed. The repository takes facts (names and dates) only.

## 1. Summary

1. **The Hebrew names now have a primary source: the Hebrew edition of the Palestine Gazette.** Its schedule of
   administrative divisions (Gazette No. 1415, Supplement No. 2, 7 June 1945, pp. 493-499) gives every district and
   sub-district heading [V]. Twelve of the file's sixteen `name_he` and all six `district_he` agree with it. **Four
   differ** (section 2): Beisan is "נפת בית־שאן" in the Gazette, not "נפת ביסאן"; Ramallah is "נפת ראם־אללה", not
   "נפת רמאללה"; Tulkarm and Beersheba differ only in a space or a maqaf (the Hebrew hyphen). The note proposes changes but makes none.
2. **The 1947 schedule's Hebrew edition was not found.** In the Hebrew Supplement No. 2 volume that holds January
   1947 (ISA file מ-7131-3), the scan skips from No. 1551 (p. 102, scan p=101) to the cover of No. 1553 (scan p=102).
   No. 1552, which holds the 1947 Proclamation, is not in between [V]. Whether it is bound elsewhere in that volume
   was not checked. The headings above are therefore from the 1945 Hebrew schedule. The
   1947 English schedule has the same six districts and sixteen sub-districts (the file's own source, and the ISA copy
   read here, section 3.1).
3. **Arabic: no primary source was reachable.** The Archives' online Gazette is in Hebrew and English only [V, the
   collection's description; every Pinpoint result is tagged עברית or אנגלית]. Birzeit University's Muqtafi
   legislation database, which publishes Mandate legislation in Arabic, refused the connection (section 4). One
   non-Wikipedia secondary source was read: the *Palestinian Encyclopedia* (الموسوعة الفلسطينية), entry "التقسيمات
   الإدارية". All sixteen of the file's sub-districts are in its lists. Fifteen names agree exactly with the file's
   `name_ar`; Tiberias is "طبرية" there and "طبريا" in the file. It **conflicts** with the file on
   the Samaria District: it calls it "لواء نابلس", while the file has "لواء السامرة" [C]. Two of its statements are
   out of date for 1947-48, so it is weak support (section 4).
4. **Amendment check, 1 January 1947 to 28 April 1948: none found.** The Gazette's "Index of Titles for 1947,
   Subsidiary Legislation" lists one entry under "Administrative Divisions": the 1947 (Amendment) Proclamation, at
   p. 125, which is the file's own source [V]. In the index bound into the 1948 Supplement No. 2 volume (Nos.
   1638-1665), the heading "Palestine Orders in Council, 1922 - 1939" (the 1947 Proclamation was made under Article 11
   of those Orders) holds one entry, a land order, and section A has no "Administrative Divisions" [V]. The last 1948
   issue in the Archives' volume, No. 1666 of 28 April 1948, has one Supplement No. 2 item, which is not one either
   [V]. The gaps are in section 3.3. This narrows the geojson's `open_questions` item 1 to those gaps. Whether that is
   enough to close it is the owner's call.

## 2. The names, heading by heading

Source for the Hebrew column: *The Palestine Gazette* (Hebrew edition, העיתון הרשמי) No. 1415, Supplement No. 2,
7 June 1945, "הכרזה על הגלילות האדמיניסטרטיביים (תיקון), 1945", the schedule ("התוספת"), pp. 493-499. ISA document
"פקודות,תקנות,צווים ומודעות תוספת מס 2 של העיתון הרשמי של ממשלת פלשתינה (אי) 04-01-1945 מ-7138-2 (001vkfz) (pages
403 to 608)", `docid=86cb028b55c3baa9_7ded851a300df5a6_2`, scan pages p=122 to p=128. Checked 2026-10-16, read on the
page images [V]. What the proclamation says there (p=122, Gazette p. 493), paraphrased: it is read as one with
"המנשר על הגלילות האדמיניסטרטיביים, 1939", it revokes that proclamation's schedule and puts the new one in its place,
and it counts as in force from 1 January 1945. It uses "מחוז" for district and "נפה" for sub-district.

| id | file `name_he` | Gazette 1945 heading (Hebrew) | Gazette p. (scan p) | Match |
|---|---|---|---|---|
| acre | נפת עכו | א. מחוז הגליל. 1. נפת עכו. | 493 (122) | yes |
| beisan | נפת ביסאן | 2. נפת בית־שאן. | 493 (122) | **no** |
| nazareth | נפת נצרת | 3. נפת נצרת. | 494 (123) | yes |
| safad | נפת צפת | 4. נפת צפת. | 494 (123) | yes |
| tiberias | נפת טבריה | 5. נפת טבריה. | 495 (124) | yes |
| haifa | נפת חיפה | ב. מחוז חיפה. נפת חיפה. | 495 (124) | yes |
| jenin | נפת ג'נין | ג. מחוז שומרון. 1. נפת ג׳נין. | 495 (124) | yes (the Gazette's geresh, the Hebrew apostrophe mark, against the file's apostrophe) |
| nablus | נפת שכם | 2. נפת שכם. | 496 (125) | yes |
| tulkarm | נפת טולכרם | 3. נפת טול כרם. | 496 (125) | **spacing**: two words in the Gazette |
| hebron | נפת חברון | ד. מחוז ירושלים. 1. נפת חברון. | 497 (126) | yes |
| jerusalem | נפת ירושלים | 2. נפת ירושלים. | 497 (126) | yes |
| ramallah | נפת רמאללה | 3. נפת ראם־אללה. | 498 (127) | **no** |
| jaffa | נפת יפו | ה. מחוז לוד. 1. נפת יפו. | 498 (127) | yes |
| ramle | נפת רמלה | 2. נפת רמלה. | 499 (128) | yes |
| beersheba | נפת באר שבע | ו. מחוז עזה. 1. נפת באר־שבע. | 499 (128) | **maqaf**: "באר־שבע" in the Gazette |
| gaza | נפת עזה | 2. נפת עזה. | 499 (128) | yes |

Districts (`district_he`): מחוז הגליל, מחוז חיפה, מחוז שומרון, מחוז ירושלים, מחוז לוד, מחוז עזה. All six are as in the
file [V, the lettered headings above]. The file's "מחוז שומרון" (no article) is what the Gazette prints.

Two more things this schedule shows:
- **The Jerusalem District has three sub-districts** (Hebron, Jerusalem, Ramallah) in 1945 [V]. That supports the
  geojson's fourth source line ("Bethlehem and Jericho merged into Jerusalem"), which today is cited only through
  Wikipedia. The 1942 date of that merger was not checked here.
- Within each district, the 1945 Hebrew schedule numbers the sub-districts in English alphabetical order (Galilee:
  עכו, בית־שאן, נצרת, צפת, טבריה, that is Acre, Beisan, Nazareth, Safad, Tiberias) [V]. Whether the 1947 schedule
  keeps that order was not checked, so nothing should be keyed on the number.

Usage in Hebrew issues of 1947, from search snippets only [S] (not opened; they show the names were still in use):
"מכריז בזה את נפת טבריה לאיזור נגוע בכלבת" and "המכריזה מקומות ידועים בנפת רמאללה" (both in the 1947 Hebrew
Supplement No. 2 volume, ISA file מ-7131-3). The second snippet spells the name "רמאללה", while another snippet from
the same volume has "בנפת ראמאללה". The Gazette was not consistent in running text. The schedule heading is the formal
form.

**Proposal for the owner (not made in this PR).** If the schedule's headings are the rule, the file would change
`beisan.name_he` to "נפת בית־שאן", `ramallah.name_he` to "נפת ראם־אללה", `tulkarm.name_he` to "נפת טול כרם" and
`beersheba.name_he` to "נפת באר־שבע", and `names_source` would cite this Gazette page instead of Wikipedia for the
Hebrew. "נפת בית־שאן" is the one a Hebrew reader will notice: the file's "ביסאן" transliterates the Arabic name, while
the Mandate's own Hebrew edition used the Hebrew name. These strings may reach the player (the region labels), so the
choice is a content decision, and the native-speaker review (#564) may want a say.

## 3. The 1947-48 amendment check

### 3.1 The 1947 Proclamation itself (the file's source), in the Archives' copy

*The Palestine Gazette* No. 1552, Supplement No. 2, 30 January 1947, p. 125 [V]: "This Proclamation may be cited as
the Administrative Divisions (Amendment) Proclamation, 1947, and shall be read as one with the Administrative
Divisions Proclamation, 1939" (section 1); "The Schedule to the principal Proclamation shall be revoked, and the
Schedule hereto shall be substituted therefor" (section 2); "This Proclamation shall be deemed to have come into
force on the 1st January, 1947" (section 3); then "THE SCHEDULE. A. Galilee District. 1. Acre Sub-District." ISA
document "תוספת מס 2 לעיתון הרשמי של ממשלת פלשתינה (אי) 02-01-1947 מ-7126-7 (001vkoo) (pages 1 to 268)",
`docid=a04db5d5ff5010c2_7ded851a300df5a6_0_0`, p=207. Checked 2026-10-16. This matches the geojson
(`in_force_from: 1947-01-01`, and its source line for Gazette No. 1552, pp. 125-133). It is a second, independent copy
of the page the Commons PDF the file cites shows.

### 3.2 What was checked, and what it showed

| Period | Source | What it shows | |
|---|---|---|---|
| 1947, whole year | "INDEX OF TITLES for 1947, SUBSIDIARY LEGISLATION", bound at the front of the English Supplement No. 2 volume (same `docid` as 3.1, p=3) | Under "Administrative Divisions." one entry only: "Administrative Divisions (Amendment) Proclamation, 1947 ... 125". The entries on that page run to p. 1872 (Acre (Control of Hawkers) By-laws, 1947), so the index covers the whole year (an inference: only the A page was opened). Checked 2026-10-16 | [V] |
| 1948, Nos. 1638 (1 Jan) to 1665 (25 Apr) | "INDEX" (typescript) at the end of the English Supplement No. 2 volume "תוספת מספר 2 לעיתון הרשמי של ממשלת פלשתינה (אי) 01-01-1948 מ-7146-1 (001vkcc) (pages 318 to 639)", `docid=3d95e02d3ea51e3d_7ded851a300df5a6_1`, p=315 (P, cont.) and p=303 (A to C). Checked 2026-10-16 | The index groups entries under the parent law. Under "Palestine Orders in Council, 1922 - 1939", the Orders under whose Article 11 the 1947 Proclamation was made, the only entry is "Order No. 4 of 1948 converting 'Miri' land into 'Mulk'" (p. 555). Section A has the Army Act, the Animal Tax Ordinance 1944 and the Animal Diseases Ordinance 1945, and no "Administrative Divisions". The volume's last Supplement No. 2 page is p. 634, No. 1665, 25 April 1948 (p=300) | [V] |
| 1948, No. 1666 (28 Apr) | Contents page, "The Palestine Gazette No. 1666, Wednesday, 28th April, 1948, Gazette Extraordinary", in "עיתון הרשמי של ממשלת פלשתינה (אי) - גיליונות 1638-1666 01-01-1948 מ-7146-8 (001vkc7) (pages 175 to 357)", `docid=42a1f75da4e2d2fe_7ded851a300df5a6_1`, p=150 | Supplement No. 2 holds one item: "Government Officers (Benefits on Termination of Appointment) Order, 1948, under the Palestine Order in Council, 1948 ... 635". Checked 2026-10-16 | [V] |
| 1947-48, full text | Exact-phrase searches of the whole collection's OCR, 2026-10-16: "Administrative Divisions (Amendment) Proclamation, 1947" (8 documents, all the January 1947 issue or its index), "Administrative Divisions (Amendment) Proclamation, 1948" (0), "הגלילות האדמיניסטרטיביים (תיקון), 1947" (0), "הגלילות האדמיניסטרטיביים (תיקון), 1948" (0) | No second 1947 amendment and no 1948 amendment in the OCR. OCR can miss text, so this only supports the index check and does not replace it | [S] |

### 3.3 Gaps (what is still open)

- **29 April to 14 May 1948.** The Archives' main 1948 volume is titled "גיליונות 1638-1666", and No. 1666 is the last
  issue found. Whether any Gazette appeared after 28 April 1948 was not checked.
- **The 1948 index is a typescript of unknown origin** bound into the volume (perhaps made by a library), not a
  printed Government index like the 1947 one. Its "P" section was seen only from "P. (cont.)" onwards. The scan
  pages read in full were p=303 and p=315; the headers only of p=299 to p=322.
- **The 1947 Hebrew schedule** (No. 1552 in Hebrew) is not where expected in the scanned Hebrew volume (section 1, item 2). If
  another ISA file holds it, its headings should be compared with section 2, because the 1947 Schedule replaced the
  1945 one in full.
- **Village-level moves inside the 1947 Schedule** (a village moved between sub-districts) count as amendments only if
  gazetted, and none was gazetted in the period read. This note does not re-check the file's lines against the
  Schedule's village lists. The existing `cross_check` and the Ra'anana note already cover part of that.

## 4. Arabic

Sources tried for an Arabic primary text:
- **The Arabic edition of the Gazette** (الوقائع الفلسطينية): not in the Archives' online collection, which is in
  Hebrew and English only (the collection's description; all results are tagged עברית or אנגלית) [V].
- **Muqtafi**, Birzeit University's legislation database, which publishes Mandate-era legislation in Arabic. A
  search result pointed to a 1945 ordinance there (`https://muqtafi2.birzeit.edu/muqtafi2/transform/fulltext/...`),
  but on 2026-10-16 `muqtafi2.birzeit.edu:443` refused the connection from both networks tried, and
  `muqtafi.birzeit.edu` timed out. Not read.
- The Arabic Wikipedia (the file's current source) is still the only source for the exact strings "قضاء X" and
  "لواء X".

The one non-Wikipedia source read: *الموسوعة الفلسطينية* (the Palestinian Encyclopedia), entry "التقسيمات الإدارية",
https://www.palestinapedia.ps/%D8%A7%D9%84%D8%AA%D9%82%D8%B3%D9%8A%D9%85%D8%A7%D8%AA-%D8%A7%D9%84%D8%A5%D8%AF%D8%A7%D8%B1%D9%8A%D8%A9/,
checked 2026-10-16. In its section "ألوية فلسطين" it says that from July 1939 the Mandate divided the country into six
districts, and it lists them [V]:

| District (encyclopedia) | Sub-districts it lists | File `district_ar` | Agreement |
|---|---|---|---|
| لواء الجليل | عكا، بيسان، الناصرة، صفد، طبرية | لواء الجليل | district yes; four names as in the file; Tiberias is "طبرية" here and "قضاء طبريا" in the file |
| لواء حيفا | حيفا | لواء حيفا | yes |
| لواء نابلس | نابلس، جنين، طولكرم | **لواء السامرة** | **[C]** the district name differs; the three sub-district names agree |
| لواء القدس | القدس، بيت لحم، أريحا، الخليل، رام الله | لواء القدس | district yes; it still lists Bethlehem and Jericho, which the 1945 Gazette no longer has [V for 1945, section 2] |
| لواء اللد | يافا، تل أبيب، الرملة | لواء اللد | district yes; "Tel Aviv" is not a sub-district in the 1945 schedule [V] or in the file's sixteen (the 1947 schedule's Lydda pages were not read here) |
| لواء غزة | غزة، بئر السبع | لواء غزة | yes |

So this source supports the file's sixteen sub-district names in Arabic, with one spelling difference (Tiberias, "ة"
against "ا"). But it is a secondary source, and two of its lines are out of date for 1947-48. On the Samaria District it disagrees with the file, and the file's own `district_ar_note`
already marks "لواء السامرة" as unverified. Settling it needs the Arabic Gazette (the 1945 or 1947 schedule) or the
Muqtafi text. That is the next step if a reader can reach either.

## 5. What a person should check

- Open the scan pages in section 2 and confirm the four Hebrew headings that differ. They are short, clear lines on
  pp. 493, 496, 498 and 499.
- Decide whether `name_he` follows the Gazette's headings (the proposal in section 2) or stays as it is.
- If Muqtafi or an Arabic Gazette copy is reachable from Israel, read the Arabic schedule's six district headings.
  The Samaria District's Arabic name is the one open conflict.
