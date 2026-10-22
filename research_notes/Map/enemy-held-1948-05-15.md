# Enemy-held localities on 15 May 1948

**Status: draft research note; AI-read. A person must open every cited page and check every line and quotation
before use. Checked by the AI on 2026-10-21. Refs #1096 and #1073.**

This note is a recommendation only. Adding a place to `mvp.enemy_held_localities` or dropping one is the owner's
decision; this note changes no file in `content/`.

## Question and method

The list under check is `content/rules.yaml:159`: `enemy_held_localities: [acre, nazareth, lod, ramla, gaza,
beersheba]`, accepted by the owner as a draft on 19 October 2026 (`content/rules.yaml:156-158`). "Held" means
military control on a date, not sovereignty, ownership or population (the map's own `meaning`,
`content/map/control_hexes_1948_49.yaml:6`). The question is only whether each place was on the non-Israeli side at
the game's start, 15 May 1948.

The map check reads two things on main (commit `65ea0e6a4`):

1. **The hex code.** Each place's hex in `content/map/settlement_hexes.yaml`, then the first character (the `opening`
   period, 15 May to 11 June 1948, `content/map/control_hexes_1948_49.yaml:11`) of that hex's row in
   `content/map/control_hexes_1948_49.yaml`. Codes (line 9): `I` Israel, `E` Egypt, `V` Arab Liberation Army, `M`
   several, `A` the other side with no holder named, `c` contested, `-` no colour (no sourced line).
2. **The code at the town's own point.** The bake gives each hex the code at the hex's centre
   (`tools/control_bake.py:13`), which can lie up to half a hex (about 4 km) from the town. So this note also reads
   the opening code at each town's own coordinates (`content/map/settlements_1948.yaml`), with the bake's own
   `code_at` over the opening snapshot and its overlays (a read-only script; it writes nothing). This point code does
   not include the bake's per-hex `contested_places` grey (`tools/control_bake.py:117`).

What the opening layer is: the 9 July 1948 lines, adjusted (`content/map/control_hexes_1948_49.yaml:8`, owner
decision of 16 October 2026 on #674), not a traced 15 May front line. Its sources are ISA map 6158/1 (checked
2026-10-14) and the CIA "Military Situation - 18 May 1948" map (checked 2026-10-16), recorded in
`content/map/control_1948_49.geojson:13` and `:32`.

## Findings

| Place | Hex and opening hex code | Opening code at the town's point | One more opened source (checked 2026-10-21) | Draft finding |
|---|---|---|---|---|
| Acre | `(7, 6)` (`settlement_hexes.yaml:8`); `c`, contested (`control_hexes_1948_49.yaml:43`). The grey comes from the GeoJSON's `contested_places` row for Acre: "attacked on 16 May, the town surrendered on the night of 17/18 May 1948" (`control_1948_49.geojson:99-109`). | `I`: the 9 July polygon, which the hex's grey overrides for the opening period. | "On the night of 16/17 May, a mortar barrage was unleashed on the town and the following night it surrendered." [Operation Ben-Ami, en.wiki, revision 1369980718](https://en.wikipedia.org/wiki/Operation_Ben-Ami). Not an Israeli official source: none naming Acre was found (the Knesset summary below does not name Acre). | Not Israeli-held on 15 May; fell on the night of 17/18 May, three days into the first period. Keep, with the owner flag below. |
| Nazareth | `(7, 10)` (`settlement_hexes.yaml:11`); `A` (`control_hexes_1948_49.yaml:72`). | `A` | Knesset, *מלחמת העצמאות*, PDF p. 5: "במבצע "דקל" נכבש הגליל התחתון, ובו העיר נצרת, אולם במבצע "ברוש" צומצם רק חלקית ראש הגשר הסורי ליד משמר הירדן." [Knesset](https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf). Also: "On the evening of 16 July, Nazareth surrendered to the Israelis after a light fight which left one Israeli dead and one wounded." [Operation Dekel, en.wiki, revision 1370631934](https://en.wikipedia.org/wiki/Operation_Dekel). | Not Israeli-held on 15 May; taken in Operation Dekel, July 1948. Keep. Do not name the Arab Liberation Army as its 15 May holder: see "Corrections" below. |
| Lod (Lydda) | `(-3, 21)` (`settlement_hexes.yaml:12`); `A` (`control_hexes_1948_49.yaml:158`). Lod and Ramla share this one hex. | `A` | IDF: "ביום ה-11 ביולי 1948, במסגרת מבצע "דני" נכבשה העיר לוד על ידי כוחות חטיבת "יפתח"." [IDF, ענף אית"ן, לוד](https://www.idf.il/%D7%90%D7%AA%D7%A8%D7%99-%D7%99%D7%97%D7%99%D7%93%D7%95%D7%AA/%D7%A2%D7%A0%D7%A3-%D7%90%D7%99%D7%AA-%D7%9F/%D7%90%D7%99%D7%A8%D7%95%D7%A2%D7%99%D7%9D-%D7%91%D7%97%D7%A7%D7%99%D7%A8%D7%94/%D7%9C%D7%95%D7%93/) (a page of the IDF's missing-soldiers branch). | Not Israeli-held on 15 May; taken on 11 July 1948. Keep. |
| Ramla (Ramle) | `(-3, 21)` (`settlement_hexes.yaml:13`); `A` (`control_hexes_1948_49.yaml:158`). | `A` | IDF: "במבצע דני השתלט צה"ל על הערים לוד ורמלה, על שדה התעופה הראשי ועל תחנת הרכבת של לוד) וכן על מספר רב של כפרים בסמוך להן, בחבל מודיעין ובאזור הפרוזדור לירושלים." (the stray bracket is the page's) [IDF, 9.7.1948 מבצע דני](https://www.idf.il/%D7%90%D7%AA%D7%A8%D7%99-%D7%99%D7%97%D7%99%D7%93%D7%95%D7%AA/80-%D7%A9%D7%A0%D7%94-%D7%9C%D7%A4%D7%9C%D7%9E-%D7%97/%D7%AA%D7%90%D7%A8%D7%99%D7%9B%D7%95%D7%9F/%D7%99%D7%95%D7%9C%D7%99-%D7%A1%D7%A4%D7%98%D7%9E%D7%91%D7%A8/971948-%D7%AA%D7%97%D7%99%D7%9C%D7%AA-%D7%A7%D7%A8%D7%91%D7%95%D7%AA-%D7%A2%D7%A9%D7%A8%D7%AA-%D7%94%D7%99%D7%9E%D7%99%D7%9D-%D7%9E%D7%91%D7%A6%D7%A2-%D7%93%D7%A0%D7%99/); the same page dates Dani "בין ה-10-18 ליולי". Knesset, PDF p. 5: "בחזית המרכזית נכבשו במבצע "דני" רמלה ולוד (עם נמל התעופה החיוני) מידי הלגיון הערבי." [Knesset](https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf). | Not Israeli-held on 15 May; taken in Operation Dani, July 1948. Keep. |
| Gaza | `(-11, 27)` (`settlement_hexes.yaml:14`); `-`, no colour (`control_hexes_1948_49.yaml:214`); `E` from the first truce. The opening south is left uncoloured on purpose: the sources disagree (`control_1948_49.geojson:40531`, `unsourced_south_opening`). | `-` | Palmach Heritage, "Battling the Egyptian Invaders – 15.5.-10.6.1948": "The Egyptian army invaded on the 15th of May and advanced as far as Gaza." [Palmach](https://www.palmach.org.il/en/history/database/?itemId=8474). | Not Israeli-held on 15 May; the Egyptian army reached it from 15 May. Keep. Do not say the opening map shows Egypt: it shows no colour. The sentence does not say Gaza was reached on 15 May itself; the Phase 6 note records 15 or 19 May as a conflict (`research_notes/GDD phase 6/settlement-targets.md:323`). |
| Beersheba | `(-9, 31)` (`settlement_hexes.yaml:15`); `-`, no colour (`control_hexes_1948_49.yaml:268`); `M` (several) from the first truce, Israeli from the `october` period. | `-` | IDF: "המשימה של כיבוש באר שבע, שהייתה מרכז לוגיסטי עבור הכוחות המצריים, הוטלה על חטיבת "הנגב" של הפלמ"ח, בפיקודו של נחום שריג." [IDF, 21.10.1948 חטיבת הנגב כובשת את באר שבע](https://www.idf.il/%D7%90%D7%AA%D7%A8%D7%99-%D7%99%D7%97%D7%99%D7%93%D7%95%D7%AA/80-%D7%A9%D7%A0%D7%94-%D7%9C%D7%A4%D7%9C%D7%9E-%D7%97/%D7%AA%D7%90%D7%A8%D7%99%D7%9B%D7%95%D7%9F/%D7%90%D7%95%D7%A7%D7%98%D7%95%D7%91%D7%A8-%D7%93%D7%A6%D7%9E%D7%91%D7%A8/21101948-%D7%97%D7%98%D7%99%D7%91%D7%AA-%D7%94%D7%A0%D7%92%D7%91-%D7%9B%D7%95%D7%91%D7%A9%D7%AA-%D7%90%D7%AA-%D7%91%D7%90%D7%A8-%D7%A9%D7%91%D7%A2/). For May: "It was occupied by the Egyptian army from May 1948 until October 1948 when it was captured by the Israel Defense Forces and part of the Arab population was expelled." [Beersheba, en.wiki, revision 1380259379](https://en.wikipedia.org/wiki/Beersheba). | Not Israeli-held on 15 May; taken on 21 October 1948. Keep. No opened source names the holder on 15 May itself, so say "the other side", not "Egypt", for that day. |

How the quotations were read: the en.wiki and Palmach pages were downloaded as HTML and the sentence was found in the
page text; the three IDF pages were read in a browser (they refuse scripted downloads); the Knesset PDF was read with
`pypdf`, and its line breaks and doubled spaces are joined here. The en.wiki sentences are given without their
footnote markers.

## Corrections to the 19 October draft

The first draft of this note (19 October, written without network access) was checked line by line.

| Draft claim | Result on 2026-10-21 |
|---|---|
| Acre quote, Operation Ben-Ami | Verbatim on the page. Kept. |
| Nazareth quote, Operation Dekel, "On the evening of 16 July, Nazareth surrendered to the Israelis…" | Verbatim (the draft cut the sentence; the full sentence is quoted above). Its paraphrase "the Arab Liberation Army retreated as the town fell" is close to the page's next sentence: "The Arab Liberation Army forces in the village under the command of Fawzi al-Qawuqji retreated to the mountains in the north." The Knesset summary is added as the Israeli official source. |
| Lod quote, IDF | Verbatim. Kept. |
| Ramla quote, IDF Operation Dani, "במבצע דני השתלט צה"ל על הערים לוד ורמלה" | Verbatim but a fragment; replaced by the full sentence. The draft's "10-18 July" matches the page. |
| Gaza quote, Palmach Heritage | Verbatim. Kept. |
| Beersheba, IDF, "הייתה מרכז לוגיסטי עבור הכוחות המצריים" | Verbatim but a fragment; replaced by the full sentence. |
| Hexes `(7, 6)`, `(7, 10)`, `(-3, 21)`, `(-11, 27)`, `(-9, 31)`, `(-3, 19)` and their opening codes | All match main. |
| "The map metadata says Acre changed hands during the opening period and surrendered on the night of 17/18 May" | Matches `control_1948_49.geojson:99-109`. Added: Acre's point is `I` on the 9 July polygon; the `c` is a per-hex grey for the opening period only. |
| Jaffa: hex `(-3, 19)` is `I`, and "the existing Phase 6 research records Haganah occupation on 14 May" | Both true: `control_hexes_1948_49.yaml:138` and `research_notes/GDD phase 6/settlement-targets.md:72` ("On 14 May Haganah units, with token Irgun forces, occupied Jaffa without resistance (p. 219)", citing Morris) and `:318`. Added: Jaffa shares its hex with Tel Aviv (`settlement_hexes.yaml:5-6`), so the hex code alone proves little; the town's own point is also `I`, and the closure check anchors Jaffa as Israeli from 15 May (`tools/control_digitise.py:276`). Another repo note dates Jaffa's fall to 13 May from en.wiki (`research_notes/Map borders/control-15-may-1948.md:106`); either day is before the start. |
| Nazareth's holder | The Phase 6 note says "Arab-held (Arab Liberation Army)" (`research_notes/GDD phase 6/settlement-targets.md:320`). en.wiki's Nazareth article says the ALA came later: "Nazareth itself was not a field of battle during the 1948 War, which began on 15 May, before the first truce on 11 June, although some of the villagers had joined the loosely organized peasant military and paramilitary forces, and troops from the Arab Liberation Army (ALA) had entered Nazareth on 9 July." [Nazareth, en.wiki, revision 1380255348](https://en.wikipedia.org/wiki/Nazareth). The map agrees in shape: `A` in the opening, `V` (ALA) from the first truce (`control_hexes_1948_49.yaml:72`). So "the other side", not "ALA", for 15 May. |

## Add or drop

**Drop: none.** Each of the six was on the non-Israeli side on 15 May 1948 by the opened sources.

**Add: none proposed.**

- Jaffa, the seventh `arab_town` in `content/map/settlements_1948.yaml` (line 38), was Israeli-held before the start
  (above). Not an addition.
- The list can only name rows of `settlement_hexes.yaml`: the loader rejects any other id
  (`sim/Statecraft.Sim/ContentLoader.cs:1245-1246`). Of its 25 rows, the six above and Jaffa are the `arab_town`
  rows; the rest are Jewish towns, cities and settlements (Jerusalem's hex is `m`, no-man's land). Arab-held places
  that `docs/locations.md` names, such as Ramallah, Nablus, Jenin, Tulkarm, Hebron, East Jerusalem, Isdud and
  Al-Majdal (`docs/locations.md:43-51`), have no settlement row, so adding them would first need new map rows and
  the same place-by-place check. `docs/locations.md:79` keeps the curated-target decision separate from map
  inclusion. Not proposed here.

## Recommended final list

Keep the draft list unchanged: `acre`, `nazareth`, `lod`, `ramla`, `gaza`, `beersheba`.

This means "on the non-Israeli side on 15 May 1948". It does not assign one Arab army to all six, and for Nazareth,
Gaza and Beersheba it names no holder for 15 May itself. Keep the list and this note marked draft until a person has
checked every claim and quotation against the linked pages.

## For the owner (not decided here)

1. **Acre falls in the first days.** It surrendered on the night of 17/18 May, inside the opening period (15 May to
   11 June). With R-MOVE-005 (`docs/rules/mvp.md:171`) the player may not enter it until Phase 6, though Israel held
   it from 18 May. Keeping it is true to 15 May; dropping it is true to the rest of the period. The owner picks.
2. **Lod and Ramla are one hex** (`(-3, 21)`). Listing both blocks the same hex once; it is harmless, and keeps both
   names on record.
