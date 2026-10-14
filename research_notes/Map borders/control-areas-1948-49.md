# Who held what, 1948-49: sources and a data model for side-coloured borders (research note, 2026-10-14)

Status: research only. No code and no content file is changed. Feeds the owner decision of 2026-10-14: colour the map's
borders by side (deep blue for the Israeli side, orange-red for the other side, as in `promo/final/03-gameplay-1936.png`)
once sourced, dated data says which side held which areas. Companions: `armistice-lines-1949.md` (D-A) and the 1947
sub-districts (D-B), both in `docs/oss/DECISIONS.md`.

Framing (`docs/CONTENT_POLICY.md` section 1): Israeli perspective; each fact takes one value from Israeli official or
mainstream sources where they exist; UN and US government records are used for lines the parties agreed to or that
observers recorded. The colours mean **military control on a date**, nothing else (section 5 below).

## Conventions

- **[V]** read on 2026-10-14 in the cited document or image (page text, catalogue record or the map itself).
- **[S]** single source, or only a search snippet, a dealer or auction listing, or a catalogue entry that could not be
  opened (several sites returned HTTP 403 or a JavaScript check to the project's User-Agent).
- **[C]** sources conflict; each value is given.
- **not found**: searched for and not found online.
- Copyright notes are factual, not legal advice, and follow D-B and D-A: the repository takes **facts** (where a line
  ran, which side held a place on a date) and redraws them by its own method. No map image is copied or committed.
  US copyright does not protect facts (Feist). Israeli state works: 50 years (Copyright Act 2007 s. 42; earlier the 1911
  Act as applied in 1924), so a state map of 1949 or 1953 is out of copyright in Israel. As for D-B, a work still
  protected in Israel on 1996-01-01 may carry a URAA-restored US copyright in its drawing; that does not reach the data.
- All requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
  Nothing was logged into or submitted. Downloaded scans stayed in the session scratchpad and are not in the repository.

## 1. Summary of findings

1. **The best single source found is an Israeli government map**: Israel State Archives (ISA), Prime Minister's Office,
   Government Secretariat, **map 6158/1**, file "פלשתינה - קווי החלוקה וקווי החזית" (Palestine: the partition lines
   and the front lines), dated 1949, marked "סודי!" (Secret) [V]. It is a Survey of Palestine 1:250,000 "Palestine:
   Index to Villages & Settlements" sheet with lines drawn by hand in colour. Its legend (read on the scan) [V]:
   - teal: "גבולות החלוקה לפי החלטת או"ם, 29.11.1947" (the partition boundaries);
   - red: "קו החזית עם גמר ההפוגה הראשונה, 9.7.1948" (the front line at the end of the first truce);
   - blue: "קו החזית עם התחלת ההפוגה השנייה, 18.7.1948" (the front line at the start of the second truce);
   - green: "קו החזית עם פתיחת הדרך לנגב ושחרור הגליל" (the front line at the opening of the road to the Negev and
     the taking of the Galilee; **no date on the map**: by its wording, after Yoav and Hiram, end of October 1948);
   - yellow: "קו הנסיגה לפי דרישת המתווך בפועל" (the withdrawal line as the Mediator in fact demanded it: the Negev
     provisional lines of November 1948, a demanded line, **not** a control line).
   Catalogue: NYU Faculty Digital Archive, Jewish Settlements Archival Project, http://hdl.handle.net/2451/72921
   (record title "מפת קווי הגבול השונים בין החלטת האו"מ ב-1947 לתחילת שיחות שביתת הנשק ב-1949"; ISA id
   ISA-PMO-GovernmentSecretary-R0005czg; ISA link https://www.archives.gov.il/archives/Archive/0b071706800171a1/File/0b07170687cca62a).
   The PDF (2 pages: the folder cover, then the map at 2368 x 4078 px, about 65 m a pixel on the map, pen lines about
   0.5-0.7 km wide) was opened and read [V]. Expected accuracy about +-1-2 km, the same class as the Jordan map 1 trace
   (D-A). Copyright: a 1949 Israeli state work (out of copyright in Israel; possible URAA term in the US for the
   drawing; facts only); its base is a Survey of Palestine sheet (D-B status). The NYU archive's page carries a generic
   "all rights reserved unless otherwise indicated" line, which is the host's notice, not a claim by the State.
2. **No one source covers every date.** The UN did not keep a single truce-line map for the first truce: the Mediator's
   report S/1025 describes lines "as established at the beginning of the truce" that were disputed and fixed locally
   (a truce line and a 2 km no-man's land decided at Mishmar HaYarden, Annex B; a Jerusalem agreement of 16 June with
   "an agreed line" and "a no-man's land ... between the two lines", shown on "the Jerusalem map") [V,
   https://www.un.org/unispal/document/auto-insert-184863/].
3. **15 May 1948 has no line source** (not found). On that date control was a patchwork of Jewish and Arab localities,
   many of them islands (the Negev settlements, the Etzion bloc until 13-14 May, the Jewish Quarter). Area colouring
   at 15 May would be invented. Recommendation: no side colouring before 11 June 1948 (section 3).
4. **Today's repository data cannot separate the sides, confirmed by test.** Run on 2026-10-14 against
   `content/map/subdistricts_1948.geojson` and `armistice_lines_1949.geojson` with the border agent's `close.py`
   (polygonise the Mandate land boundary with every line and zone in force on the date): on 1949-04-03, 1949-07-20 and
   1950-02-22 one face holds 98 % of the land and contains Tel Aviv, Nablus, Hebron, Ramallah, Gaza City, Rafah,
   Beersheba and Tulkarm together. With lines buffered by 0.05 degrees (`close2.py`), 1949-07-20 and 1950-02-22 split
   Tel Aviv/Rafah/Beersheba from Nablus/Hebron/Ramallah, but Rafah and Khan Yunis still land on the Israeli side
   (no 1949 Egypt line south of Gaza) and Jerusalem is not separated (no map 2). [V, run in the session scratchpad]

## 2. Sources by date

| Date (snapshot) | Source | What it gives | Licence | Tag |
|---|---|---|---|---|
| 15 May 1948 | none found | US Military Academy map "May 15 - June 10" (arrows and fronts at small scale; Commons file "1948 arab israeli war - May15-June10.jpg", Krasnoborski and Martini, Department of History, USMA) is a campaign map, not a control map | US federal work, public domain in the US (Commons {{PD-US-Army}}) | not found (as a control line) |
| 11 Jun 1948, first truce | (a) "Military Situation On Effective Date of Cease-Fire (June 11, 1948)", credited to the Harry S. Truman Library, image on JVL https://www.jewishvirtuallibrary.org/map-of-military-situation-on-effective-date-of-cease-fire-june-1948; (b) ISA 6158/1 red line, which is dated 9 Jul 1948 (the end of the same truce); (c) S/1025 for the Jerusalem and Mishmar HaYarden local lines | (a) a US government map, author not given on JVL; (b) the closest Israeli line; truce violations moved some local positions between 11 Jun and 9 Jul | (a) probably a US federal work, to verify at the Truman Library before use; JVL's page is (c) AICE, so take the map from the Truman Library, not JVL; (b) as item 1; (c) UN document, not copyrighted (ST/AI/189/Add.9/Rev.2) | (a) [S]; (b) [V]; (c) [V] |
| 18 Jul 1948, second truce | (a) ISA 6158/1 blue line; (b) CIA, ORE 38-48, "Possible Developments From the Palestine Truce", 27 Jul 1948, Enclosure B "The Military Situation in Palestine at the Beginning of the Second Truce, 18 July 1948", "(See map.)", reprinted in FRUS 1948 vol. V part 2, doc. 486, https://history.state.gov/historicaldocuments/frus1948v05p2/d486; the map is on JVL credited "Source: CIA", https://www.jewishvirtuallibrary.org/1948-war-military-situation-at-beginning-of-second-truce; CIA reading room CIA-RDP78-01617A003200140001-0 (the reading room PDF was not reachable: JavaScript check) | (a) Israeli front line; (b) "approximate lines between Arab-controlled and Jewish-controlled areas", partition boundary, gains between the truces; text names Lydda, Ramle, Ras el Ein, Nazareth, the Isdud thrust, the Jerusalem corridor | (a) as item 1; (b) US federal work, public domain in the US (17 USC 105) | (a) [V]; (b) text [V], map [S] (seen only as a JVL copy) |
| Syrian front, 18 Jul 1948 to 20 Jul 1949 | The Syria armistice map S/1353/Rev.1 (already georeferenced for D-A) draws the **Israeli truce line (black)** and the **Syrian truce line (brown)** | Both sides' truce positions, so control and the ground between them | UN document, public domain | legend [V] (D-A note); that these lines held from July 1948 is an inference [S] |
| End Oct 1948, after Yoav (15-22 Oct) and Hiram (29-31 Oct) | (a) ISA 6158/1 green line; (b) USMA map "1948 arab israeli war - Oct.jpg" (Commons) | (a) one line for both operations; in the north it is not drawn separately: after Hiram the line is the Lebanese frontier (and beyond it, below); (b) small-scale cross-check | (a) as item 1; (b) public domain (US) | (a) [V] legend, date by inference [S]; (b) [S] |
| Lebanon, Nov 1948 to 23 Mar 1949 | Israeli forces held villages inside Lebanon from Hiram until the armistice, withdrawing under it: 13 villages (en.wiki "1949 Armistice Agreements"); other accounts give other numbers, and one says Carmeli reached the Litani | the area lies outside the Mandate frontier | Wikipedia CC BY-SA (facts) | [C] count; not drawn (section 4) |
| Nov 1948, Mediator's Negev lines | ISA 6158/1 yellow line; S/1086 (20 Nov 1948, Acting Mediator on provisional demarcation lines in the Negeb); the Memorandum of 13 Nov 1948 (UN Archives AG-022, not online; D-A note) | a line demanded, **not** a control line | UN / Israeli state | [V] (yellow legend), memorandum not found |
| Jan 1949, after Horev (22 Dec - 7 Jan) | USMA map "1948 arab israeli war - Dec22-Jan07.jpg" (Commons, {{PD-US-Army}}); the Faluja pocket (Egyptian-held until the Egypt agreement's Art. III withdrawal); the Gaza-Rafah strip | small-scale only; **no Israeli or UN line found for January 1949** | public domain (US) | [S]; needs more sources |
| 24 Feb 1949, Egypt | D-A (Art. VI.1 line, El Auja DMZ); south of Gaza 1949 still missing (D-A gap) | as built | UN | [V] (D-A) |
| 10 Mar 1949, the southern Negev | Operation Uvda reached Umm Rashrash (Eilat) on 10 Mar 1949 (PR #396 war note, [V] MFA there). Before that date the southern Negev was held by neither side in strength | grey ("not held") until then, then blue | n/a | [S]; needs a source for what Jordan held in the Arava before March |
| 23 Mar 1949, Lebanon | D-A (the frontier) | as built | UN | [V] |
| 3 Apr 1949 and the Triangle stages | D-A (Jordan map 1, low accuracy; Art. VI line A by stages); the 23 Mar 1949 Wadi Ara agreement (Ben-Gurion diary, 23 Mar 1949, https://bengurionarchive.bgu.ac.il/en/node/6928); ECF reference map of the transferred localities, https://ecf.org.il/issues/issue/1348 | as built; the actual handover dates are not researched (D-A convention: Art. VI.3 deadlines) | UN; ECF page licence not checked (not used) | [V] (D-A); diary and ECF [S] |
| Jerusalem, 30 Nov 1948 to 1949 | (a) Jordan agreement map 2 (the 30 Nov 1948 cease-fire map, 1:20,000; Dayan and al-Tall): not online (D-A gap); (b) **lead:** NLI holds a Survey of Israel map, 1949, 1:10,000, central Jerusalem, "showing the Israel - Jordan armistice line of 3 April 1949", base map Survey of Palestine, catalogued as public domain in Israel (https://www.nli.org.il/en/maps/NNL_ALEPH002366948/NLI; the page needs JavaScript, seen only as a search snippet); (c) FRUS 1948 vol. V part 2 doc. 800 (editorial note on the 30 Nov agreement and the Mount Scopus annex) | (b) could close the Jerusalem gap | (b) per NLI, PD in Israel; Israeli state work 1949 | (a) not found; (b) [S]; (c) [S] |

Other sources checked:

- **IDF, Doctrine and Training (Guidance) Brigade, "מפת קרבות מלחמת העצמאות (כוחות קרקע)"**, 29 sheets at 1:100,000
  on Survey of Palestine bases, printed by the Survey of Israel government press, 1953-54, 100 copies; NLI Eran Laor
  Cartographic Collection (system no. 990025555220205171); on Wikimedia Commons, "Category:Maps by Doctrine and
  Guidance Brigade (1950s)", scans about 16,000 x 11,500 px, tagged {{PD-UKGov}}{{PD-IsraelGov}} [V]. It is **not** a
  front-line map: each box is one action, coloured by brigade, with its type (attack to capture, defence, raid, ambush,
  temporary capture, capture without battle), result (+ achieved, - not achieved, x partly) and date [V, legend read on
  sheet 9 "Ramle"]. Use: a dated Israeli official cross-check of where control changed between two snapshots. The 2018
  Ministry of Defence book "קרבות מלחמת העצמאות, מפות ומבצעים" analyses these sheets (copyright, not read). Sheet 11
  "Gaza" shades a strip along the coast whose meaning the legend does not give (unverified; possibly the Gaza Strip).
- **IDF History Department training-aid map set, 1957**, Hebrew, nine maps, two indexes, "divided into stages by the
  three truces" (dealer listing, geographicus.com; the page blocked the request) [S]. Israeli state work; lead for the
  NLI or the IDF Archives.
- **Signed truce maps, 12 Aug 1948**, Jordan Valley and the Galilee panhandle, signed by IDF and Arab commanders and UN
  representatives (Kedem auction listing, https://www.kedem-auctions.com/en/node/114671, blocked) [S]. Private sale;
  location unknown.
- **"מפת החזיתות ביום הפסקת האש 18/7/1948"**, a lithograph by Dr. Y. Shapiro, 1948 (Winner's auction 103, 2017) [S].
  Private publisher; copyright status unknown; not usable.
- **Atlases and books (copyright; facts only, for cross-checks; none read this session, none online):** Carta,
  Jehuda Wallach (ed.), "Carta's Atlas of Israel" / "אטלס כרטא לתולדות מדינת ישראל" and Wallach, "Carta's Guide to the
  War of Independence" (titles per VIAF, https://viaf.org/viaf/87962078) [S]; Martin Gilbert, "The Routledge Atlas of
  the Arab-Israeli Conflict" (10th ed., 2012) [S]; Benny Morris, "1948" (Yale, 2008); Yoav Gelber, "Palestine 1948"
  (Sussex Academic, 2001/2006); David Tal, "War in Palestine 1948" (Routledge, 2004); Netanel Lorch, "The Edge of the
  Sword" (1961). The question did not name a "Carta Atlas of the War of Independence" by that title, and none was found.
- **Survey of Israel:** no 1948-49 control map found online; its 1961/1967 "map of the cease-fire lines" (1:250,000) is
  later [S].
- **Third-party digitisations** (Commons "own work" maps such as "Israel and Palestine 1st June 1948", OpenHistoricalMap):
  never a source, as in D-A; at most a named cross-check.

## 3. Recommended data model

**Polygons per key date, baked per hex like the sub-districts; a per-locality "held by" list only as a check, never as
the drawn source.**

Why not per-locality as the source:

- **The renderer** (border agent, 2026-10-14) colours each region from one per-hex texel with no spare bits. Polygons
  per date that bake to per-hex regions, each region with a side, need no shader change. A per-locality field would
  have to be turned into areas (Voronoi or nearest-locality), which invents boundaries (against the D-A ruling "no
  invented boundaries").
- **The sources are lines, not lists.** ISA 6158/1, the CIA map, the armistice maps and the Syrian truce lines all
  draw lines; none lists which side held each village.
- **Sensitivity.** A dated "held by" per Arab locality is a capture timeline for each village, which the repository
  has chosen not to record: `localities_1948.yaml` is "start of game only" (test
  `test_start_of_game_only_no_later_fate`; `docs/visual-v1.md` defers a dated map, #61, to an owner framing
  decision), and the M4 plan's D4 says war mode never models the capture or depopulation of Arab localities (PR #396
  note). Area colour at five-ish dates says less than that and matches what the sources say.

Proposed shape (for a later `content/map/control_1948_49.geojson`, status draft; **not built**):

- One FeatureCollection; each feature is a polygon with `snapshot` (the id below), `side` (`israel`, `arab`,
  `neutral`), `holder` for the info panel only (`israel`, `egypt`, `jordan`, `iraq`, `syria`, `lebanon`, `ala`,
  `mixed`; `neutral` features take `kind`: `no_mans_land`, `dmz`, `not_held`, `contested`, `un_enclave`),
  `shown_from` / `shown_until`, `sources` with `checked`, `source_quality` (`low_res_scan`, `small_scale`), and the
  line ids it was closed from.
- Each snapshot covers the whole Mandate land with no gaps and no overlaps (the bake fails otherwise).
- Built by the same method as D-A: digitise each dated line (true lines kept), then close it against the Mandate
  frontier and the coast (D-B), polygonise, and give each face a side from the source (the side a line faces is read
  from the map: e.g. ISA's loops around Ijzim and Ein Ghazal enclose Arab-held enclaves on 9 Jul 1948).
- Bake per hex by the sub-district rule (the hex takes the face under its centre), plus: a hex whose centre falls in a
  `neutral` face is grey; no rule moves a hex to a side the sources do not give it.

Snapshots:

| id | shown from | shown until | source | can it be built now? |
|---|---|---|---|---|
| (none) | 15 May 1948 | 11 Jun 1948 | none | no: show the 1947 sub-districts, as today, with no side colour |
| `truce1` | 11 Jun 1948 | 9 Jul 1948 | ISA red (dated 9 Jul) + S/1025 local lines; Truman Library map as cross-check | yes, from ISA, with the caveat that the line is the truce's end |
| `ten_days` | 9 Jul | 18 Jul 1948 | none: fighting (the Ten Days) | grey as `contested` only where ISA red and blue differ; elsewhere keep `truce1` |
| `truce2` | 18 Jul 1948 | 15 Oct 1948 | ISA blue + CIA/FRUS map + Syrian truce lines (S/1353/Rev.1) | yes |
| `yoav_hiram` | 15 Oct 1948 (south) / 29 Oct (north), the area changes from the operation's end (22 Oct / 31 Oct) | 22 Dec 1948 | ISA green + the Lebanese frontier in the north + Syrian truce lines | yes, the date by inference; the south needs the Faluja pocket drawn as an Egyptian enclave |
| `horev` | 7 Jan 1949 | the armistice of each front | **missing**: needs a January 1949 source (Faluja, Gaza-Rafah, the Hebron hills, the southern Negev) | not yet |
| armistice fronts | 24 Feb / 23 Mar / 3 Apr (with the Art. VI stages) / 20 Jul 1949 | 1967 (out of scope) | D-A lines | not until the Jerusalem map 2 and the Egypt 1949 south-of-Gaza gaps are closed |

**Closure check (how to prove a snapshot separates the sides).** For each snapshot: polygonise the Mandate land with
that snapshot's lines (`close.py`'s method), then require that (1) every face has a side, (2) a list of anchor places
with a sourced side on that date lands in faces of that side, and (3) no face holds anchors of both sides. Anchors:
the main towns and the Jewish settlements whose side on each date is in the sources (Tel Aviv, Haifa, West Jerusalem,
Nablus, Hebron, Ramallah, Jenin, Tulkarm, Gaza, Majdal, Beersheba, Nazareth from 16 Jul 1948, Lydda and Ramle from
12 Jul 1948, the Negev settlements); keep the anchor list in the tool, cited, not as a content layer. Today's data
fails test (3) (section 1, item 4).

## 4. Contested areas, no-man's land and zones

- **One grey style** (the renderer's third style) for everything that is neither side's: no-man's land (Jerusalem,
  Latrun after the agreements), the demilitarised zones (El Auja; the three Syrian DMZ parts, Ein Gev and Dardara on
  the Israeli side of the line but inside the zone), the Mount Scopus enclave (Israeli-held under UN arrangement, and
  surrounded: grey with an info-panel note), areas held by neither side in strength (the southern Negev before
  10 Mar 1949), and areas where a snapshot's sources disagree. The info panel names which (`kind`).
- **Change within a period:** no animation and no interpolation between snapshots. A snapshot is shown from its date
  until the next; the stretch between two lines while fighting runs (the Ten Days, Yoav, Hiram, Horev) is grey
  `contested` only where the two lines differ, and only for the days the operation ran.
- **Sub-hex features** (Jerusalem's two sides, the Faluja pocket, Mount Scopus, the northern and southern Syrian DMZ
  parts) are smaller than a 5 km hex. They are kept in the true polygons; the hex bake shows the hex's centre, and a
  hex that holds both sides of the Jerusalem line is grey. The info panel says so.
- **Outside the Mandate frontier** (the Lebanese villages, Oct 1948 - Mar 1949; the Sinai incursion, 28 Dec 1948 -
  early Jan 1949): not coloured. The map's land outside the frontier stays `outside`, as D-B does.

## 5. Sensitivity (for the owner)

1. **What the colour claims.** Military control on a date. Not sovereignty, not ownership, not population. The
   armistice texts say so for their own lines: "The Armistice Demarcation Line is not to be construed in any sense as
   a political or territorial boundary" (Egypt, Art. V.2) and "without prejudice to future territorial settlements or
   boundary lines" (Jordan, Art. VI.9) [V, https://avalon.law.yale.edu/20th_century/arm01.asp and arm03.asp]. The
   legend changes at the armistice from "held by" to "side of the armistice line" (section 6).
2. **No per-village dates.** Area colour must not become a per-village capture or depopulation record. The localities
   layer stays "start of game only" until the owner's #61 framing decision. **Owner to review:** once the colour
   changes, Arab localities that were depopulated will sit inside blue areas with their 1948 names and kits still
   drawn. That is the #61 question and is not settled by this note.
3. **Words.** ISA 6158/1's own legend says "שחרור הגליל"; the IDF battle map says "כיבוש". Those are the sources' words
   and stay in citations; the on-screen text uses neutral "held by" / "בשליטת". "Arab forces" (not "the enemy", not
   "Palestinians") because the side was several armies plus the Arab Liberation Army and local forces; the info panel
   can name the holder (Egypt, Jordan's Arab Legion, Iraq, Syria, Lebanon, the ALA).
4. **Colour.** Orange-red is the promo's colour for the other side. It is a side colour, not a judgement; the legend
   says what it means.
5. **The Triangle.** The localities transferred under the Jordan agreement change colour by the Art. VI stages, which
   follow a negotiated transfer, not a battle; the info panel says "transferred under the Israel-Jordan armistice".
6. **Sources marked secret in 1949.** ISA 6158/1 was "Secret" then and is open now in the State Archives; cite it with
   its ISA identifier.
7. **Owner to review:** the legend wording (section 6); whether `ten_days`-style grey is wanted at all; the
   `holder` names in the info panel; whether the 11 Jun snapshot may use a line dated 9 Jul.

## 6. Legend wording (for the owner to approve)

The wording follows what the data claims. Two versions, switching per front on its armistice date.

**1948 snapshots (front lines; "held by")**

- EN: "Blue: held by Israel · Orange-red: held by Arab forces · Grey: contested, no-man's land or not held"
- HE: "כחול: בשליטת ישראל · כתום-אדום: בשליטת כוחות ערביים · אפור: שטח שבמחלוקת, שטח הפקר או שטח שאינו מוחזק"
- Caption under it: EN "Front lines, [18 July 1948], approximate (Israel State Archives)" / HE "קווי החזית,
  [18 ביולי 1948], בקירוב (ארכיון המדינה)".

**From each armistice ("side of the line")**

- EN: "Blue: Israeli side of the armistice line · Orange-red: Arab side · Grey: no-man's land or demilitarised zone"
- HE: "כחול: הצד הישראלי של קו שביתת הנשק · כתום-אדום: הצד הערבי · אפור: שטח הפקר או אזור מפורז"

Notes: "כוחות ערביים" rather than "צבאות ערב", because the ALA and local forces were not state armies. "שטח הפקר" is
the usual Hebrew for no-man's land; "אזור מפורז" for a DMZ. The Hebrew is a draft for the owner; Arabic is not proposed.

## 7. What can be built now, and what needs more sources

Can be built now (status draft, owner sensitivity review before any shared build):

- `truce1` (from the 9 Jul line), `truce2` and `yoav_hiram` from ISA 6158/1, georeferenced on the sheet's Palestine
  grid (ticks every 10 km on the scan), closed against the D-B frontier and coast, with the Syrian truce lines from
  the S/1353/Rev.1 scan already georeferenced for D-A. Accuracy about +-1-2 km (`source_quality: low_res_scan`).
  One reading risk: the teal partition line and the green front line are close in colour on the scan; trace green
  only where it departs from the 1947 partition line, and record each such stretch.

Needs more sources:

- **A higher-resolution copy of ISA 6158/1** (the State Archives' own viewer, or a request: an outward action for the
  owner to approve).
- **January 1949 (after Horev)**: an Israeli or UN line. Leads: the 1957 IDF History Department set (NLI), the IDF and
  Defense Establishment Archives, UN S/ documents of January 1949 (the Egypt General Cease-Fire, S/1225 per PR #396).
- **Jerusalem**: the NLI 1949 Survey of Israel 1:10,000 map (lead above) or map 2 itself; until then Jerusalem's hexes
  are grey.
- **The Egypt 1949 line south of Gaza** (D-A gap; the 13 Nov 1948 Memorandum's map, UN Archives AG-022).
- **What Jordan held in the Arava and the southern Negev before 10 Mar 1949.**
- **The Truman Library 11 June map** (to confirm its author and status) and the **CIA ORE 38-48 map** from the CIA
  reading room or FRUS print (to cross-check `truce2`).

## Sources opened (2026-10-14)

- ISA 6158/1 via NYU FDA: http://hdl.handle.net/2451/72921 (record and PDF) [V].
- UN S/1025, Mediator's report on the truce of 11 June - 9 July 1948: https://www.un.org/unispal/document/auto-insert-184863/ [V].
- FRUS 1948 vol. V part 2, doc. 486 (CIA ORE 38-48): https://history.state.gov/historicaldocuments/frus1948v05p2/d486 [V].
- JVL map pages (CIA, Truman Library credits): URLs above [V for the credit lines only].
- Wikimedia Commons: the IDF battle map sheets (sheet 9 Ramle and sheet 11 Gaza viewed), file pages and licence tags;
  the USMA maps' metadata [V].
- Egypt and Jordan armistice texts, Avalon Project: https://avalon.law.yale.edu/20th_century/arm01.asp, arm03.asp [V].
- ECF, Wadi Ara reference map page: https://ecf.org.il/issues/issue/1348 [V for the description].
- Search snippets only [S]: geographicus (1957 IDF set), Kedem (12 Aug 1948 truce maps), Winner's (1948 lithograph),
  NLI (1949 Jerusalem map), VIAF (Wallach), Routledge/Blackwell's (Gilbert), Ben-Gurion Archive diary of 23 Mar 1949.
