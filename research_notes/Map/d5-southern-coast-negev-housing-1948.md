# D5: one opening housing place in Southern Coast and in the Negev (research note, 2026-11-01)

Web and repository research by a review agent (Claude Code), 1 November 2026, for owner decision D5 on #1691. A
person still needs to check the history, the sources and the quotations. Nothing here changes `content/`, `sim/` or
`unity/`.

Refs #1691.

## The question

Owner ruling, 1 November 2026, on the GDD 0.3 decisions ("Approve all six (Recommended)"): D5 option **B**, "One
opening housing place in each region for its farmer", applied only "after a person checks a sourced Jewish settlement
in each region on 15 May 1948" (`.superpowers/coordination/gdd03-amir-answers-draft.md`, D5 row, read 2026-11-01).
On who sources it, the owner chose "A review agent sources it (Recommended)". This note is that sourcing. The person's
check is still pending.

The background is the owner's 30 October farming ruling, "Yes, same region": a farmer lives in the region whose field
they work. The approved #1691 split gave Southern Coast and the Negev one opening field each and no housing
(`research_notes/gdd03-region-allocation.md`, "Approved split").

## Tags

As in the sibling notes (`research_notes/Era 1882-1948/settlements-pre-1948-negev.md`):

- **[V]** two or more independent sources agree, or one authoritative primary source.
- **[S]** one source. **[S, Wikipedia]** rests on Wikipedia alone, which is used only as a pointer.
- **[C]** the sources disagree. Both values are given.
- **[GAP]** not found.
- **computed here** marks arithmetic or a lookup done for this note. **our reading** marks an interpretation.

Every [V] and [S] below is an agent's reading on 2026-11-01. A person's check is pending for all of them.

## 1. How the game defines the two regions

The regions are design regions, anchored on whole Mandate sub-districts by the owner ruling of 30 October 2026, "C:
sub-district, then Amir adjusts" (`research_notes/gdd03-regions-capitals-fields.md`, "Schema adaptation and
activation"). They are not a claim that these were Israeli administrative regions.

| Region (id) | Approved anchor | Capital | Member hexes | Hex bounds (computed here) |
|---|---|---|---:|---|
| Southern Coastal Plain (`southern_coast`) | Gaza Sub-District, Gaza District | `gaza` (-11, 27) | 18 | q -15 to -6, r 23 to 31 |
| Negev (`negev`) | Beersheba Sub-District, Gaza District | `beersheba` (-9, 31) | 190 | q -20 to -4, r 27 to 56 |

- Anchors: `research_notes/gdd03-regions-capitals-fields.yaml`, `reference_subdistricts` of each region (Gaza and
  Beersheba). Member hexes: `research_notes/gdd03-regions.schema-input.yaml`, `member_hexes` of each region, generated
  by `tools/gdd03_region_hexes.py` from `content/map/subdistrict_hexes_1948.yaml` (sub-district ids 15 and 16). The
  hex counts match the regions note's "Southern Coast 18; Negev 190".
- The Southern Coast hexes, in full: (-15, 31), (-14, 30), (-13, 29), (-12, 28), (-11, 27), (-11, 28), (-10, 27),
  (-10, 28), (-9, 26), (-9, 27), (-8, 25), (-8, 26), (-7, 24), (-7, 25), (-7, 26), (-6, 23), (-6, 24), (-6, 25).
- Each hex takes the sub-district at its centre, so a hex can be up to half a hex (about 4 km) from the true line
  (`content/map/subdistrict_hexes_1948.yaml`, header).
- **The opening field.** Each region has one opening prepared field: `southern_coast_opening_field_allocation` and
  `negev_opening_field_allocation`, `slots: 1`, `farmer_tiers: Deferred`, with `hexes` and `prepared_at_opening`
  empty. Which hex the field is on waits for the #1690 workability rule (Amir Q36 on #1668)
  (`gdd03-regions-capitals-fields.yaml`; `gdd03-region-allocation.md`, "Deferred and open"). So no field hex is named
  yet, and the housing place below is chosen for the region, not for a field hex.
- PR #2013 (the GDD 0.3 docs, fetched as `pull/2013/head` on 2026-11-01) adds no other definition of the two regions:
  its `docs/` hits for "Negev" and "southern coastal" are the Nir Am water-line objective (`docs/gdd/corridors.md`) and
  location descriptions (`docs/locations.md`), not region bounds.

Note on names: the sibling Negev note uses the **present-day CBS** sub-districts, where Nir Am, Yad Mordechai, Dorot
and Ruhama are in the Ashkelon sub-district. The game uses the **1947 Mandate** sub-districts, where the same places
fall in Gaza or Beersheba. This note uses only the game's hexes.

## 2. Method

- Each settlement's 1948 point was looked up against the game's own hexes: the nearest land-hex centre in
  `content/map/hexes.json` (cosine-scaled longitude), then its sub-district in `subdistrict_hexes_1948.yaml` and its
  opening control code in `content/map/control_hexes_1948_49.yaml` (period `opening`, 15 May to 11 June 1948) **(computed
  here)**. The lookup reproduces every hex the content already stores: Negba (-7, 25), Nir Am (-9, 27), Yad Mordechai
  (-9, 26) and Revivim (-11, 34).
- Where a settlement moved after the war, the 1948 site is used when a source gives it.
- Presence in `content/` was checked with `git grep -i` over `content/` for each name (2026-11-01).
- Sources were fetched on 2026-11-01 with a generic User-Agent. The Jewish Telegraphic Agency (JTA) archive was
  searched through `https://www.jta.org/wp-json/wp/v2/archive` (date windows below) and the item text was searched for
  the exact name. That text is the JTA's machine reading of the printed bulletin; the print PDFs were not opened.
- Haim Gvati, *100 Years of Settlement*, vol. 2, Ben-Yehuda Project edition, https://benyehuda.org/read/14684 (fetched
  2026-11-01, 1,348,249 bytes, the same size the sibling notes record), was searched for each name.

## 3. Candidates in Southern Coast (Gaza Sub-District)

| Settlement | 1948 point | Hex | Opening code | In `content/` | 15 May 1948 |
|---|---|---|---|---|---|
| **Nir Am** | 31.5194, 34.5808 (`locations_1948.yaml`) | (-9, 27) | `I` in every period | yes, `locations_1948.yaml` `nir_am`, `must_have` marker | inhabited and held [V] |
| Negba | 31.6608, 34.6831 (`locations_1948.yaml`) | (-7, 25) | `I` opening, then `M`, then `E` to October 1948 | yes, `locations_1948.yaml` `negba` | inhabited and held [V] |
| Yad Mordechai | 31.5886, 34.5583 (`settlements_1948.yaml`) | (-9, 26) | `I` opening, then `E` to October 1948 | yes, `settlements_1948.yaml` | inhabited; evacuated 23 to 24 May 1948 [C on the day] |
| Dorot | 31.5067, 34.6451 (CBS) | (-9, 27), Nir Am's hex | as Nir Am | no | not researched further |
| Gvaram | 31.5915, 34.6122 (CBS) | (-8, 26) | not looked up | no | not researched further |
| Kfar Darom | 31.4014, 34.36 (he.wiki) | (-13, 29) | `-`, then `E` | no | inhabited; Egyptian attack 15 May (Gvati), evacuated 8 July 1948 |

Ruhama (CBS 31.4980, 34.7061) falls in hex (-8, 27), which is Beersheba Sub-District in the game, so it belongs to the
Negev region here **(computed here)**.

### Nir Am, the recommended place

- Founded January 1943. Gvati, appendix, kibbutzim: "ניר-עם דרום 12.1.43 האיחוד". [S] The sibling note gives he.wiki's
  21 January 1943 against Gvati's 12 January [C]; either way the kibbutz was five years old on 15 May 1948.
- Before the invasion. Gvati, narrative: "על־יד קיבוץ ניר־עם הוקם מרכז אספקה לנגב ושם התמקמה מפקדת חטיבת הנגב של
  הפלמ״ח". Palmach Heritage, "The Palmach and the Negev Settlements",
  https://www.palmach.org.il/en/history/database/?itemId=8465 (checked 2026-11-01): "The headquarters of the Negev
  Brigade and the 2nd Battalion were at Nir-Am". Gvati also names Nir Am among the older southern settlements with more
  members ("דורות, גת, ניר־עם, רוחמה"). [V] Two independent sources.
- Around 15 May. Palmach Heritage, "חטיבת הנגב (חטיבה 12)", https://www.palmach.org.il/history/database/?itemId=6190
  (checked 2026-11-01): "למחרת הפלישה של הצבא המצרי ולאחר שקיבוץ ניר-עם ספג הפגזה ארטילרית, עברה מפקדת החטיבה לקיבוץ
  דורות". The kibbutz was there and was shelled the day after the invasion. [S]
- After. JTA, 18 October 1948, https://www.jta.org/archive/israelis-warn-egyptians-against-use-of-gas-jews-capture-strategic-position-in-negev
  (checked 2026-11-01): the Israelis "reported the shelling of the Jewish settlements of Niram, Guaram and Mekoroth".
  [S] JTA web text.
- Not lost. Gvati's account of the settlements taken in the war names Atarot, Yad Mordechai, Gush Etzion and Nitzanim,
  not Nir Am ("רק ישובים ספורים נכבשו ... (עטרות, יד־מרדכי) ... (גוש עציון וניצנים)"). [S]
- Site. The content point (31.5194, 34.5808) is the CBS point of the present kibbutz (31.51966, 34.57977, sibling
  note). No source read here records a move. [GAP] for an explicit 1948 site, but no move is known.
- The game's own data. Hex (-9, 27) is `I` in every period of the control layer, and it is the Negev Brigade's anchor
  hex (`content/map/unit_anchor_hexes.yaml`, `negev`, `q: -9, r: 27`). The earlier opening-control research rated it
  SOURCED, holder Yishuv/Haganah (`research_notes/Map/regions-1948-opening-control-p2b.md`, gdd03-0687).

### Why not Negba or Yad Mordechai

- **Negba** was inhabited and held all war. JTA, 20 May 1948,
  https://www.jta.org/archive/haganah-breaks-through-encircled-old-city-of-jerusalem-captures-sarafand (checked
  2026-11-01): "The southern Jewish settlement of Negba was raided by planes this morning". Gvati dates the first
  Egyptian attack to 21 May and the second to 2 June. [V] It is a good second choice. **Our reading:** its hex (-7, 25)
  also holds the Iraq Suwaydan police fort, which the Egyptians held from May to 9 November 1948, and the control layer
  turns the hex `M` and then `E` from 11 June. A housing place there would sit in a hex the map shows as not Israeli for
  most of 1948.
- **Yad Mordechai** was inhabited on 15 May, but it was abandoned on 23 to 24 May 1948 and taken (Gvati: "לנטוש את
  המקום ... (עטרות, יד־מרדכי)"; the sibling note gives the [C] on the day). Its hex turns `E` from 11 June. **Our
  reading:** an opening housing place that the history empties in the second week is a poor fit.

## 4. Candidates in the Negev (Beersheba Sub-District)

| Settlement | 1948 point | Hex | Opening code | In `content/` | 15 May 1948 |
|---|---|---|---|---|---|
| **Revivim** | original mitzpe 31.0396, 34.7183 (he.wiki) | (-11, 34) | `-` (no colour) until the October period | yes, `settlements_1948.yaml`, `settlement_hexes.yaml` (-11, 34) | inhabited and held [V] |
| Nirim | Dangur security house 31.2428, 34.335 (he.wiki table) | (-14, 31) | `-` opening, then `I` | no | inhabited; attacked 15 May and held [V] |
| Mishmar HaNegev | 31.3628, 34.7178 (he.wiki table) | (-9, 29) | `I` in every period | no (a pinned point in `control_1948_49.geojson` only) | founded 1946 (sibling note); 15 May not researched here |
| Shoval | 31.4145, 34.7438 (CBS) | (-8, 28) | `I` in every period | no | founded 1946 (sibling note); 15 May not researched here |
| Hatzerim | 31.2401, 34.7134 (CBS) | (-10, 31) | `I` in every period | no | founded 1946 (sibling note); 15 May not researched here |
| Beit Kama, Re'im, Gilat | content points | (-8, 28), (-12, 29), (-10, 30) | | yes | founded 1949: not on 15 May 1948 |

The three content places founded in 1949 are `beit_kama`, `reim` and `gilat` in `content/sites.yaml` (Beit Kama:
`founded: 1949-04-18`). They cannot be the 15 May 1948 place.

### Revivim, the recommended place

- Founded 1943 as one of the three mitzpim. Gvati, appendix, kibbutzim: "רביבים נגב צפוני 20.7.43 הקיבוץ המאוחד". [S]
  The sibling note records the [C] on the day (Gvati 20 July, he.wiki 7 July, the founding scroll 30 August 1943).
- Before the invasion. Palmach Heritage, "הגדוד השמיני – הנגב הדרומי",
  https://palmach.org.il/history/database/?itemId=6201 (checked 2026-11-01), on February 1948: "פלוגה מוקטנת שנייה
  בפיקוד "גנדי" (יהושע ברנשטיין) תגברה את הישובים הדרומיים רביבים, חלוצה ועלומים". [S]
- Across 15 May. *Encyclopaedia Judaica* (2007), "Revivim", on the Jewish Virtual Library,
  https://www.jewishvirtuallibrary.org/revivim (checked 2026-11-01): in the war it resisted Bedouin attacks, was cut
  off by the invading Egyptian army and held out until Israeli forces broke through (the entry says December 1948).
  Paraphrased here; the page limits quotation. [S] It covers 15 May by its span, not by a dated line.
- After. JTA, 4 August 1948, https://www.jta.org/archive/britain-supplies-egyptian-army-in-palestine-with-military-equipment-violates-truce
  (checked 2026-11-01): "an assault on the heights overlooking the Jewish settlement of Revivim, in the Negev". [S] JTA
  web text.
- Not lost. Gvati's list of settlements taken in the war does not include it (section 3). [S]
- **Our reading:** a February 1948 reinforcement, an August 1948 attack on a still-Jewish settlement, and a reference
  work that says it held out from December 1947 to the end of the siege are three independent sources that bracket 15
  May 1948. Together they confirm Revivim was inhabited and held on that day. [V] No source read here names Revivim on
  15 May itself. [GAP] for a line dated that day (the Negev Brigade log or Ben-Gurion's diary were not read).
- **Site: the 1948 site is in the same hex.** The kibbutz moved after the war; its new cornerstone was laid on 1 June
  1949 (he.wiki `רביבים`, oldid 44107079: "לאחר המלחמה הקיבוץ עבר ממקומו המקורי אל האתר בו הוא נמצא היום"). [S,
  Wikipedia] he.wiki `מצפה רביבים`, oldid 43348736, places the original mitzpe at **31.03962, 34.71827** [S, Wikipedia].
  That point is 0.52 km from the content point (31.0436, 34.7211) and falls in the same hex, (-11, 34) **(computed
  here)**. This closes the open gap on the original site that `regions-1948-opening-control-p2b.md` (gdd03-0713)
  recorded.

### Why Revivim, and the two alternatives

- Revivim is the only place in `content/` inside the Negev region that existed on 15 May 1948, and it already has a
  map settlement row and hex. So it needs no new map asset.
- **The catch, for the owner:** Revivim's hex is uncoloured (`-`) in the control layer for every period until October
  1948 (`-----------IIIII`), because the source maps do not reach that far south
  (`content/map/control_1948_49.geojson`, the south's 15 May to 10 June note: "Revivim, south of the 60-km northing,
  stays without colour as in every 1948 period"). If housing needs friendly control of its hex at the start (Amir's
  Q37 on #1668), Revivim's hex does not have it in the data today. The sources above say Revivim was Jewish-held. The
  map data is silent; it does not say otherwise.
- **Alternative 1, Nirim**, has the best-dated evidence of any Negev settlement. Palmach Heritage, itemId=6201 (above):
  "ב-15 למאי 1948 הדפה נירים התקפה של הצבא המצרי". Gvati, narrative: the Egyptians attacked "קיבוץ נירים בדנגור" at the
  same time as Kfar Darom, which he dates "ב־15.5". JTA, 18 May 1948,
  https://www.jta.org/archive/control-center-of-jerusalem-tel-aviv-hospital-bombed-casualties-heavy (checked
  2026-11-01): "a two-day battle at the Nirim settlement was ended when the Jews counterattacked". [V] But Nirim is not
  in `content/` and would need a new place row. Its first site is also [C] between two he.wiki articles (sibling note).
  The 15 May battle was at Dangur (Gvati), but the hex (-14, 31) is from he.wiki's point for the 1947 security house. It
  is also uncoloured at the opening.
- **Alternative 2, Mishmar HaNegev**, sits in a hex that is `I` in every period, (-9, 29), and is a pinned blue
  point in the control data. Its 15 May 1948 presence was not researched for this note, and it has no place row in
  `content/`.

## 5. Recommendation

| Region | Housing place | Hex | Confirms 15 May 1948 | Key source |
|---|---|---|---|---|
| Southern Coast | **Nir Am** (`nir_am`) | (-9, 27) | yes [V] | Palmach Heritage itemId=6190 (shelled the day after the invasion; brigade HQ) with Gvati (supply centre and HQ) and Palmach itemId=8465 |
| Negev | **Revivim** (`revivim`) | (-11, 34) | yes [V] by bracketing sources; no line dated 15 May | *Encyclopaedia Judaica* "Revivim" (held out through the invasion), Palmach itemId=6201 (Feb 1948), JTA 4 Aug 1948 |

Both regions meet D5's condition: a source confirms a Jewish settlement in each on 15 May 1948. For the Negev, Nirim is
confirmed by a source dated 15 May (Palmach itemId=6201 and Gvati), even if the owner prefers Revivim as the place.

What this note does not decide:

- Whether the housing place must stand in the field's hex. The field hexes are still empty (#1690).
- Revivim's uncoloured opening hex (section 4) is the owner's call: keep Revivim and treat the hex as friendly for
  housing, or choose Nirim or Mishmar HaNegev with a new place row.
- Population: these are design places (3 per project in the #1691 split), not a claim about how many people lived
  there. The end-1944 Jewish population basis was 2,890 for Gaza Sub-District and 150 for Beersheba Sub-District
  (`gdd03-region-allocation.md`).

## 6. Gaps

- [GAP] A document dated 14 to 16 May 1948 naming Revivim (the Negev Brigade operations log, Ben-Gurion's diary).
- [GAP] An explicit statement that Nir Am did not move after 1948. None of the sources read records a move.
- [GAP] The JTA items were read as web text, not as the printed bulletin pages.
- Not read: Mishmar HaNegev, Shoval and Hatzerim on 15 May 1948; Dorot and Gvaram beyond their hexes.

## 7. Sources

Fetched and read on 2026-11-01 unless stated:

- Haim Gvati, *100 Years of Settlement*, vol. 2, Ben-Yehuda Project, https://benyehuda.org/read/14684 (appendix rows
  for Nir Am, Revivim, Nirim; narrative on the Negev in 1948).
- Palmach Heritage: https://www.palmach.org.il/en/history/database/?itemId=8465 ("The Palmach and the Negev
  Settlements"); https://www.palmach.org.il/history/database/?itemId=6190 ("חטיבת הנגב (חטיבה 12)");
  https://palmach.org.il/history/database/?itemId=6201 ("הגדוד השמיני – הנגב הדרומי").
- *Encyclopaedia Judaica* (2007), "Revivim", Jewish Virtual Library, https://www.jewishvirtuallibrary.org/revivim.
- JTA archive (web text): 18 May 1948 (Nirim), 20 May 1948 (Negba), 4 August 1948 (Revivim), 18 October 1948 (Niram),
  URLs above. Searched through the archive's REST API for "Revivim", "Nir Am", "Nir-Am", "Niram", "Negba" and "Nirim",
  15 April to 15 July 1948 and 29 November 1947 to 1 March 1949.
- he.wiki (pointer only): `רביבים` oldid 44107079; `מצפה רביבים` oldid 43348736.
- Repository: `research_notes/gdd03-regions-capitals-fields.{md,yaml}`, `research_notes/gdd03-regions.schema-input.yaml`,
  `research_notes/gdd03-region-allocation.md`, `research_notes/Era 1882-1948/settlements-pre-1948-negev.md`,
  `research_notes/Map/regions-1948-opening-control-p2b.md`, `content/map/{hexes.json,subdistrict_hexes_1948.yaml,control_hexes_1948_49.yaml,locations_1948.yaml,settlements_1948.yaml,settlement_hexes.yaml,unit_anchor_hexes.yaml}`,
  all at `origin/main` 0181a6465.
