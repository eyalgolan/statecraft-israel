# Village flour mills in 1948, for the `village_mill` hex tile (#2269, research part)

Status: **draft, needs a person to check**. This is research only. An AI agent opened every source marked "agent read" below on
3 November 2026 and recorded the day. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check every claim
against its source before use. This note does not change `content/`, `sim/` or `unity/`, and it does not choose where the tile
goes: placement follows in a separate PR, and the owner decides it.

## Question and result

#2269 asks which villages in the game's area had a flour mill in 1948, what kind, how each matches `content/map/localities_1948.yaml`
or `settlements_1948.yaml`, and the mill's own point where a source gives one. The tile is
`unity/Assets/Scripts/Map/HexTiles/Prototypes/VillageMill.cs`.

**What closed (four villages that have a row, one that has none).** Two kinds of source were opened:

1. The 1940s 1:20,000 Survey of Palestine sheets, read as images from Palestine Open Maps tiles. They print the word "Mill" (or
   "Mills") at a point. Four places carry it: Ghuweir Abu Shusha (three labels, one marked "Disused"), Beisan (one), Kabri (one) and
   Jarisha (one, "Mills"). These are the nearest thing to a 1948 source found. The tile set does not name its edition (see "What was
   tried and failed"), so "1940s" is the most this note can say.
2. The Palestine Exploration Fund *Survey of Western Palestine, Memoirs* (survey of the 1870s), page images on archive.org. It names a
   mill at Zuq et Tahta, at Kh. Abu Shusheh, at Beisan, at Kabri (a quotation from Guerin) and at Jerisheh. These are 1870s facts, not
   1948 facts.

Where both agree (Beisan, Kabri, Ghuweir Abu Shusha, Jarisha) the place had a mill in the 1870s and a mill is printed on the 1940s
sheet. Zuq et Tahtani has the 1870s mill but its 1940s sheet prints none (see the table).

**What the sources do not say:** the kind of mill. No label or page read says water, animal or motor for the 1940s mills. The 1870s
text says water-driven for Kabri ("to turn mills"); for Beisan it mentions "the aqueducts leading to" the mills. Threshing floors,
terraces and sycamore figs: nothing read says they stood at these places ([GAP], the tile can stay mill-only as #2269 allows).

## Answer table

Tags: V (always written in full, as in the rows) means opened and read by the agent; [S] secondary; [C] conflicting; [GAP] open.
Map labels are read from tiles `https://palopenmaps.org/tiles/pal20k-1940s/16/{x}/{y}.jpg`, joined 3 by 3 around the tile named.
Coordinates read off a map label are the label's position, good to about 50 m at best, not a surveyed mill point.

| Claim | Source and page or URL | Printed text | Tag | Day read |
|---|---|---|---|---|
| Ghuweir Abu Shusha (`ghuwayr_abu_shusha`, row at `localities_1948.yaml`, hex (12, 8)) has three "Mill" labels beside the village | Palestine Open Maps tile set above, z16 tiles centred on x 39232, y 26429 | "Mill" at 35.50998 E, 32.85262 N; "Mill" at 35.50831 E, 32.85001 N (south of the village, beside "El Qabu"); and "...al Mill (Disused)" at 35.50436 E, 32.85437 N (west, beside "Ein el Bassa"). The first word of the third label is cut by the montage edge, so only "Mill (Disused)" is certain | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The same place in the 1870s: Kh. Abu Shusheh, "round a mill" | *Survey of Western Palestine, Memoirs*, vol. I, p. 360 (archive.org `surveyofwesternp01conduoft`, page image n424 viewed) | "Kh. Abu Shusheh (Q g).-A few wretched hovels, all built of basalt, round a mill. It contains about 20 Moslems." (page prints "Shusheh"; the match of this Kh. Abu Shusheh to the 1948 village is by name and by the Tiberias district; the grid square was not checked against the sheet) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| The same place in the 1870s, second entry: Kh. Abu Shusheh, "ruined mills in the valley below" | *Memoirs*, vol. I, p. 396 (archive.org `surveyofwesternp01conduoft`, page image n462 viewed) | "Kh. Abu Shûsheh (O g).-There are modern ruins in this village, and a number of ruined mills in the valley below. No ancient remains were observed." (the page prints a long dash after the full stop; it prints the grid as "(O g)" while the p. 360 entry prints "(Q g)" in the text layer and is hard to read on the scan, so the match of the two entries to each other and to the sheet is by name, not by grid) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Beisan (`beit_shean`, hex (9, 14)) has a "Mill" label south of the town | tile set above, z16 centred x 39231, y 26506 | "Mill" at about 35.50601 E, 32.49548 N, south-south-east of the built-up area and east of the road; the sheet also prints "Milh" west of the town, a place name, not read as a mill | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Beisan in the 1870s: thirteen mills, ten in working order, with aqueducts | *Memoirs*, vol. II, p. 113 (`surveyofwesternp02conduoft`, page image n136 viewed) | "Thirteen mills are marked on the plan, of which ten are in working order. These, with the aqueducts leading to them, are of Arabic workmanship." | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Kabri (`al_kabri`, hex (9, 6)) has a "Mill" label south-west of the village | tile set above, z16 centred x 39167, y 26394 | "Mill" at about 35.14715 E, 33.01107 N, near the cemetery mark south-west of "El Kabiri" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Kabri in the 1870s: a spring whose water "runs off in a cascade to turn mills" | *Memoirs*, vol. I, p. 146 (`surveyofwesternp01conduoft`, page image n178 viewed), a passage the *Memoirs* quote from Guerin | "from there, by an opening made expressly, the water runs off in a cascade to turn mills and water gardens." The *Memoirs* print "El Kabry" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Zuq et Tahtani (`al_zuq_al_tahtani`, hex (16, 2)) had a mill in the 1870s | *Memoirs*, vol. I, p. 90 (archive.org `surveyofwesternp01conduoft`, page image n108 viewed) | "Zuk et Tahta (R c).-Stone and mud village, with ruined Arab houses on north side, and a mill; contains about 100 Moslems; situated on the Huleh Plain" | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Zuq et Tahtani: no "Mill" label on the 1940s sheet in the 3 by 3 tiles read | tile set above, z16 centred x 39248, y 26351 (village at 35.6011 E, 33.215 N) | the tiles print "Ez Zuq et Tahtani", "Ein el Balad", "Ein es Safsaf", "Ein Abdu", "El Khirba", "Beit Hillel"; no "Mill" among them. Not a statement that none stood: the area read is about 1.5 km across | [V, agent read 2026-11-03; a person's check pending] for the labels listed | 2026-11-03 |
| Jarisha has a "Mills" label beside the village (no row for Jarisha in `localities_1948.yaml` or `settlements_1948.yaml`; a search of both files for "jarisha" and "jerisheh" returned nothing) | tile set above, z16 centred x 39104, y 26593 | "Mills" at about 34.80735 E, 32.09637 N, at the crossing next to "Jarisha". Palestine Open Maps gazetteer point for Jarisha 34.808425 E, 32.095865 N [S] | [V, agent read 2026-11-03; a person's check pending] for the label | 2026-11-03 |
| Jerisheh in the 1870s: "a well (Sakia) and a mill" | *Memoirs*, vol. II, p. 251 (`surveyofwesternp02conduoft`; text layer read, header "TOPOGRAPHY. 251", page image not viewed) | "Jerisheh (H p). - A very small mud village, with olives and a palm. It has a well (Sakia) and a mill." (the text layer spaces and OCR-marks the line) | [V, agent read 2026-11-03; a person's check pending] | 2026-11-03 |
| Tahunet Abu Nur, "a modern flour-mill on the river", the Zerka (Crocodile) river | *Memoirs*, vol. II, p. 34 (text layer read, header "34 THE SURVEY OF WESTERN PALESTINE", page image not viewed) | "Tahunet Abu Nur (Ik). - A modern flour-mill on the river"; no game row sits on that spot, and the Zerka sheet read shows "Dhahrat et Tahuna" ("Tahuna": mill) as a ridge name, not a mill | [V, agent read 2026-11-03; a person's check pending] for the text; [GAP] for a 1948 mill and for the row | 2026-11-03 |

## Disagreements

None between the sources read. One difference of time, not of fact: the 1870s pages and the 1940s sheet are about seventy years apart,
so "had a mill in the 1870s" does not mean "had one in 1948". Only a printed "Mill" on the 1940s sheet is read as a 1948-era mill,
and "Disused" on one Abu Shusha label means that mill may not have worked in 1948.

## Search log

| Search | Where and when | Result |
|---|---|---|
| "mill" in the Palestine Open Maps gazetteer | `https://palopenmaps.org/api/localities`, parsed 2026-11-03, regex on every name and field | 0 features named mill, tahun or tahan (the gazetteer lists places, not map labels) |
| "mill" across the three PEF *Memoirs* text layers | archive.org `surveyofwesternp01conduoft`, `...p02conduoft` read in full 2026-11-03; vol. III (`...p03conduoft`) did not download in time and was not read | 73 lines in vol. I and 42 in vol. II contain "mill" (counts of lines, many are millstones or place names); only the entries in the table were followed up |
| "mill" in `content/`, `research_notes/`, `docs/` | grep in the worktree 2026-11-03 | `content/map/localities_1948.yaml` and `settlements_1948.yaml`: 0 lines with "mill" or "tahun" |

## What was tried and failed

- **Tile edition.** The tile set `pal20k-1940s` does not state its edition; `https://palopenmaps.org/api/sheets` (read 2026-11-03) lists
  290 sheets for the `pal20k1940` layer, dated 1929 to 1954 (1948: 25 sheets, 1949: 14, 1942: 57, 1931: 12; 144 with no year). For the
  sheets over the five mill points it lists: 19-21 Beisan, 1942, 1949 and one with no year; 20-29 Metulla (Zuq et Tahtani), 1949 and two
  with no year; 13-16 Petah Tiqva (Jarisha), 1942, 1948 and one with no year; 19-25 Esh Shuna (Ghuweir Abu Shusha) and 15-26 Nahariya
  (Kabri), no year. So 1948 or 1949 printings exist for some of these places, and the tiles may be one of them. The edition of the
  tiles stays a [GAP]; the 1940s date is a statement about the tile set's name, not a printed date.
- **Spots read with no mill found:** Mirr (el Mirr, 34.9156 E, 32.112 N), the Tantura village tiles, and the Zerka river mouth near
  Kabara. The PEF names mills at "el Mir" and at Ras el 'Ain (Rosh HaAyin). The tiles centred on the Rosh HaAyin point
  (34.956 E, 32.093 N and 34.976 E, 32.096 N) did not show the springs, so Ras el 'Ain's mills were not found: [GAP].
- **Safed.** The *Memoirs* name many mills on Wady et Tawahin near Safed; one tile set around 35.51 E, 32.97 N showed none and was not
  the wadi. [GAP].
- **Jewish villages.** No source for a Jewish village's mill (steam or motor) was found or searched beyond the above. [GAP].
- **Kind of mill in 1948, threshing floors, terraces, sycamore figs:** [GAP] (see the result).
- No Hebrew or Israeli archive was tried, and no Wikipedia article was used.

## Open gaps

- A text source for a 1948 mill, its kind and its owner, at any of the four villages with a row.
- Ras el 'Ain, Safed's wadi, el Mirr, Tahunet Abu Nur's village, the Jewish villages' mills.
- Edition of the map tiles.
