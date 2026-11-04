# Census-city host for each opening field, a proposal for the owner (#2493, parent #1717)

Status: **research-only proposal, for the owner to approve**. Nothing here is in `content/`, `sim/` or `unity/`, and no row is
final until the owner approves it (section "Owner approval table"). The author is AI-assisted (Claude Code). On 4 November 2026
the assistant opened every source marked [V] or [S] below and recorded the day. The submitting contributor, beriki770-ship-it,
stands behind that check as described in `CONTRIBUTING.md` ("AI-assisted contributors may do these person-only steps too");
each [V] and [S] row therefore reads **checked by beriki770-ship-it, AI-assisted**, 4 November 2026. Section "What was opened
and checked" says exactly what that covers and what it does not.

## Question and result

The owner's rule is that a farmer lives in the region the field is in (`docs/GDD.md:373`, Q27: "farmers live in the region they
farm"). The Historical start's census units that are farmers are Deferred (`docs/rules/mvp.md`, R-POP-004 and the paragraph
after it, lines 588-589), and the owner chose the recommended option ("do the recommended for all"), which reads: "a source-backed city-by-field proposal for owner approval" (Q26, 4 November
2026, [recorded on #1717](https://github.com/eyalgolan/statecraft-israel/issues/1717#issuecomment-6623136302)). This note takes
the 20 `field_hexes` of `content/regions.yaml` as they stand and proposes one of the 12 census cities of `content/cities.yaml`
for each, from the cities whose `member_locations` sit in the field's own region (the same test R-POP-004 uses for a census
record's region).

**What the result is, in four lines.**

- **Two fields have no eligible census city at all.** Southern Coast (F19, Nir Am) and the Negev (F20, Mishmar HaNegev) hold none
  of the 12 census cities in `member_locations`, so the same-region rule cannot be met from the census cities. Both rows are
  [GAP] and the owner has to choose how (section "Decision A: the two regions with no census city").
- **Three fields have a census city on the field itself** or a source that ties the city to the place: F09 (Hadera), F12 (Zikhron Ya'akov), F15 (Petah Tikva). Two more have a founding or administrative source: F10 (Binyamina to
  Zikhron Ya'akov) and F16 (Kfar Saba to Petah Tikva).
- **Six Valleys fields (F03 to F08) have exactly one eligible city, Tiberias.** The choice is forced by the rule, not by evidence.
  Two of the six (F07 Ashdot Ya'akov, F08 Degania Alef) are at least in Tiberias's Mandate sub-district; the other four are not.
- **Seven fields have several eligible cities and no source that picks one**: F01, F02, F11, F13, F14, F17, F18. Each is [GAP] and lists
  the plausible choices. Where a default is offered (F01, F02, F11, F18) it is labelled as a contributor inference, never as evidence; F13, F14 and F17 have none.

## How to read this note

- **[V]** the assistant opened the source and read the printed text on a page image (all [V] here are pages of *Village
  Statistics 1945*). **[S]** secondary: English Wikipedia, read through the Wikipedia API at the revision given. **[GAP]** no
  source read supports the placement. Nothing is invented: a figure is printed as the page prints it.
- **Evidence class** (column "Class" in the field table), kept apart as the issue asks:
  **H** historical evidence of cultivation or settlement ties between the place and the city;
  **A** administrative only (the Mandate sub-district a place was listed under): says nothing about who farmed;
  **R** forced by the rule (one eligible city): not evidence about the place at all;
  **D** distance or map reading: **not evidence**, shown only because the owner may want to know it;
  **I** contributor inference, offered for the owner to accept or refuse.
- **Distance is never used to choose a city.** Where a recommendation rests on D or I it says so in the row.
- **What the 1945 figures are.** *Village Statistics 1945* is the Palestine Government's land and population table, which
  Sami Hadawi reproduced (PLO Research Center, Beirut, 1970). Its foreword says the table gives "the ownership holdings of the
  land as between Arabs, Jews, Public and Others as on 1 April 1945", and also calls the reproduced material official "despite
  its inaccuracies". Table I is population and total land by owner. **Table II is "Cultivable Land (Areas in Dunums)"**: citrus
  and bananas, plantations and irrigable land, and cereal land, each by owner. These are registered ownership and a fiscal
  land classification on 1 April 1945. They are not what was sown on 15 May 1948, and not who worked it. The "Urban" line of a
  town is blank in Table II for Haifa, Tel Aviv, Safad and Tiberias, so a town's figure sits on its "Rural" line, which may share
  the line with a neighbour (Tiberias shares it with Haz Zor'im). 1 dunum = 1,000 m².
- **Sources for the places.** The hex notes come from `content/regions.yaml` (lines 64-66, 149-155, 233-239, 308-310, 370-372,
  704-705, 997-998). The sources opened are in "Sources".

## Inventory: the 20 fields and the census cities eligible in each region

Census cities by region (`content/regions.yaml` `member_locations` crossed with `content/cities.yaml`, 4 November 2026, main at
`aabbbc0a1`, the tip when this note was written; main has moved since). "No field" means the city is eligible somewhere but no field in its region asks for it.

| Region | Fields | Eligible census cities |
|---|---|---|
| Galilee (`galilee`) | F01 (9,5); F02 (14,7) | Safed; Rosh Pinna |
| Valleys (`valleys`) | F03 (7,11); F04 (7,12); F05 (8,13); F06 (9,13); F07 (11,12); F08 (12,11) | Tiberias |
| Haifa and the Sharon (`haifa_sharon`) | F09 (1,15); F10 (2,14); F11 (2,15); F12 (3,13); F13 (6,10); F14 (6,12) | Haifa; Zikhron Ya'akov; Hadera; Netanya (Netanya: no field) |
| Tel Aviv and Jaffa (`tel_aviv_jaffa`) | F15 (-2,21); F16 (-1,20) | Tel Aviv; Petah Tikva |
| Central plain (`central_plain`) | F17 (-5,25); F18 (-4,25) | Rishon LeZion; Rehovot |
| Southern Coast (`southern_coast`) | F19 (-11,31) | **none** |
| Negev (`negev`) | F20 (-10,33) | **none** |
| Jerusalem hills, Northern hills | no fields | (Jerusalem is eligible; no field) |

Field numbers F01 to F20 are this note's labels, in the file's region order. The hex is `{ q, r }` as in the file.

## Evidence: what the pages print

All Hadawi pages are in the PDF at `https://archive.org/download/palestine-village-statistics-1945/Village%20Statistics%201945.pdf`
(item `https://archive.org/details/palestine-village-statistics-1945`); the PDF page and the printed page are the same number.
Each row below was read on the page image on 4 November 2026. Columns of Table II: citrus and bananas / plantations and
irrigable / cereal / total, **Jewish-owned dunums**.

| Place (line) | Page | Printed (Jews) | Tag |
|---|---|---|---|
| Rosh Pinna, Table I (Safad Sub-District) | 71 | population Jews 340; land Arabs 801, Jews 6,847, Public 91, total 7,739 | [V] |
| Rosh Pinna, Table II | 120 | plantations 195; cereal 5,195; **total 5,390** (Arabs total 523; all 5,913) | [V] |
| Safad (Urban), Table I | 71 | population Arabs 9,530, Jews 2,400, total 11,930; land Arabs 998, Jews 153, Public 278, total 1,429 | [V] |
| Safad (Rural), Table I | 71 | land Arabs 2,820, Jews 103, Public 79, total 3,002 | [V] |
| Safad (Urban) and Safad (Rural), Table II | 120 | Urban line blank; Rural: **total 19** Jewish (Arabs 2,002; all 2,025) | [V] |
| Tiberias (Urban), Table I (Tiberias Sub-District) | 73 | population Arabs 5,310, Jews 6,000, total 11,310; land Arabs 552, Jews 1,864, Public 689, total 3,105 | [V] |
| Tiberias (Urban) and "Tiberias (Rural) and Haz Zor'im", Table II | 123 | Urban line blank; Rural and Haz Zor'im: citrus none; plantations 14; cereal 5,158; **total 5,172** | [V] |
| Hadera (Rural), Table II | 90 | citrus 7,150; plantations 1,482; cereal 6,394; **total 15,026** (Hadera (Urban) line blank) | [V] |
| Hadera (Rural), Table I | 48 | land Jews 18,000 (Arabs 117, Public 814, total 18,931) | [V] |
| Zikhron Ya'aqov, Table II | 92 | citrus 107; plantations 3,906; cereal 3,499; **total 7,512** | [V] |
| Binyamina, Table II | 89 | citrus 1,439; plantations 3,570; cereal 3,894; **total 8,903** | [V] |
| Pardes Hanna, Table II | 91 | citrus 5,288; plantations 119; cereal 11,913; **total 17,320** | [V] |
| Mesheq Yagur, Table II | 91 | citrus 68; plantations 873; cereal 2,858; **total 3,799** | [V] |
| Mishmar ha 'Emeq, Table II | 91 | citrus 36; plantations 225; cereal 2,696; **total 2,957** | [V] |
| Haifa (Urban), Table I | 48 | population Arabs 62,800, Jews 75,500, total 138,300; land Jews 27,623 (Table II line blank, p. 90) | [V] |
| Petah Tiqva (Rural), Table II (Jaffa Sub-District) | 96 | citrus 13,749; plantations 2,213; cereal 2,004; **total 17,966** (Urban line blank) | [V] |
| Petah Tiqva (Urban), Table I | 53 | population Arabs 150, Jews 17,100, total 17,250; land Jews 4,011 | [V] |
| Kefar Sava (Urban) and (Rural), Table I | 52 | Urban population Jews 4,320; land Jews 1,239 (Public 153). Rural land Jews 5,012 (Public 198) | [V] |
| Kefar Sava (Rural), Table II | 96 | citrus 2,922; plantations 69; cereal 1,166; **total 4,157** | [V] |
| Tel Aviv (Urban), Table I and II | 53, 97 | population Jews 166,000; land Jews 9,101; Table II line blank | [V] |
| Rehovot (Rural), Table II (Ramle Sub-District) | 117 | citrus 9,935; plantations 1,821; cereal 744; **total 12,500** | [V] |
| Rishon le Zion (Rural), Table II | 117 | citrus 7,833; plantations 2,500; cereal 1,844; **total 12,177** | [V] |
| Natanya (Rural), Table II (Tulkarm Sub-District) | 127 | citrus 3,557; plantations 408; cereal 1,976; **total 5,941** | [V] |
| Giv'at Brenner, Table II (Ramle Sub-District) | 115 | citrus 1,416; plantations 16; cereal 979; **total 2,411** | [V] |
| Hulda, Table II (Ramle Sub-District) | 115 | plantations 569; cereal 1,726; **total 2,295** | [V] |
| Ashdot Ya'aqov, Deganiya "A", Table I (Tiberias Sub-District) | 72 | Ashdot Ya'aqov: population Jews 1,020, land Jews 6,343. Deganiya "A": population Jews 290, land Jews 1,118 | [V] |
| Haifa Sub-District pages (Hadera, Binyamina, Pardes Hanna, Zikhron, Yagur, Mishmar ha 'Emeq) | 48, 89-92 | all six are printed under "Haifa Sub-District" | [V] |

How the list reads for the choices: Rosh Pinna's Jewish cultivable land (5,390 dunums) against Safad's (19, with a blank urban
line) is the one large gap between two cities in the same region. Hadera, Zikhron Ya'akov, Petah Tikva, Rehovot and Rishon
all have thousands of dunums, and Haifa and Tel Aviv have none printed in Table II.

## The 20 rows

Checked date for every row: 2026-11-04. Quotations are verbatim (Wikipedia revisions in "Sources"; Hadawi printed text is the
row from the table above). "W" codes are listed under Sources.

| Field and hex | Region | Proposed census city | Source | Exact supporting quotation | Class | Tag | Reasoning |
|---|---|---|---|---|---|---|---|
| F01 (9,5), coastal plain, on Nahariya next to Hanita (`regions.yaml:65`) | Galilee | **Rosh Pinna** (a recommendation; Safed is the other eligible city) | W6 (Nahariya), W7 (Hanita); Hadawi pp. 40, 71, 120 | W6: "The city was founded in 1935 by Jewish refugees fleeing Nazi Germany." W7: "Located in the western Galilee approximately 15 kilometres northeast of Nahariya". Hadawi: Rosh Pinna Jewish cultivable 5,390; Safad (Rural) 19, Safad (Urban) blank | I | [GAP] | No source read ties Rosh Pinna or Safed to cultivation at Nahariya or Hanita. Hanita and Nahariya are listed in Acre Sub-District (p. 40, p. 41), and neither candidate is. The only evidence is regional: Rosh Pinna's Jewish farmland in 1945 is 5,390 dunums, Safad's 19. That is a reason to recommend Rosh Pinna, not a link to this place. Plausible same-region choices: Rosh Pinna, Safed. |
| F02 (14,7), lowland, next to Rosh Pinna (`regions.yaml:66`) | Galilee | **Rosh Pinna** (a recommendation; Safed is the other eligible city) | W1; Hadawi pp. 71, 120 | W1: "Rosh Pinna was one of the first modern Jewish agricultural settlements in the history of the Land of Israel, then part of the Ottoman Empire (Turkey)." Hadawi p. 120: Rosh Pinna Jewish cultivable total 5,390 | I | [GAP] | No source read ties F02's farmers to Rosh Pinna. The hex note says "next to Rosh Pinna" (`regions.yaml:66`), a map reading, which is distance and is not used as evidence. What the sources show is Rosh Pinna's own farming (founded 1878 and 1882, 5,390 Jewish cultivable dunums on 1 April 1945 against 340 residents, p. 71), and that does not say who worked this field. The pick is a contributor inference, labelled I, as for F01. Plausible same-region choices: Rosh Pinna, Safed. |
| F03 (7,11), coastal plain, on Nahalal (`regions.yaml:150`) | Valleys | **Tiberias** (the only eligible city) | `content/regions.yaml` member list | no source read links Tiberias to Nahalal: W8 says Nahalal "is a moshav in northern Israel" in the Jezreel Valley Regional Council | R | [GAP] | The rule forces Tiberias. Nahalal is listed under Nazareth Sub-District (p. 62), not Tiberias's. Tiberias itself has 5,172 Jewish cultivable dunums on the line it shares with Haz Zor'im (p. 123), so it is a farming town on paper, but nothing connects those dunums to Nahalal. |
| F04 (7,12), lowland, next to Nahalal and Mishmar HaEmek (`regions.yaml:151`) | Valleys | **Tiberias** (only eligible) | as F03 | as F03 | R | [GAP] | As F03. Mishmar ha 'Emeq is printed under Haifa Sub-District (p. 48), so neither named place is in Tiberias's. |
| F05 (8,13), lowland, on Ein Harod (`regions.yaml:152`) | Valleys | **Tiberias** (only eligible) | W9 (Ein Harod) | W9: "Ein Harod (Hebrew: עֵין חֲרוֹד) was a kibbutz in northern Israel near Mount Gilboa." | R | [GAP] | As F03. Ein Harod is printed under Beisan Sub-District (p. 43). Nothing read ties it to Tiberias. |
| F06 (9,13), lowland, next to Ein Harod (`regions.yaml:153`) | Valleys | **Tiberias** (only eligible) | as F05 | as F05 | R | [GAP] | As F05. |
| F07 (11,12), valley, on Ashdot Ya'akov and Gesher (`regions.yaml:154`) | Valleys | **Tiberias** | Hadawi p. 72; W11 | Hadawi p. 72: "Ashdot Ya'aqov" is a row of the "Tiberias Sub-District — Table I" page, population Jews 1,020. W11: "Originally founded in 1924 by a kvutza of Hashomer members from Latvia on the land which is today Gesher, it moved to its current location between 1933 and 1935." | A | [V] [S] | Ashdot Ya'akov is listed in Tiberias Sub-District (p. 72). That is an administrative tie, not a cultivation one. Gesher is under Beisan (p. 43). Tiberias is also the only eligible city. |
| F08 (12,11), valley, on Degania Alef (`regions.yaml:155`) | Valleys | **Tiberias** | Hadawi p. 72; W10 | Hadawi p. 72: "Deganiya "A"" is a row of the "Tiberias Sub-District — Table I" page, population Jews 290, land Jews 1,118. W10: "The Jewish community (kvutza) was founded in 1910, making it the earliest Labor Zionist farming commune in Palestine." | A | [V] [S] | As F07: Degania Alef is in Tiberias Sub-District (administrative tie) and Tiberias is the only eligible city. |
| F09 (1,15), coastal plain, on Hadera (`regions.yaml:234`) | Haifa and the Sharon | **Hadera** | W2; Hadawi pp. 90, 48 | W2: "Hadera was established in 1891 as a farming colony by members of the proto-Zionist group Hovevei Zion, from Lithuania and Latvia." Hadawi p. 90: Hadera (Rural) Jewish cultivable total 15,026 (citrus 7,150) | H | [V] [S] | The field is on Hadera, a farming colony, with 15,026 Jewish cultivable dunums in 1945, the most of the four eligible cities. |
| F10 (2,14), coastal plain, on Binyamina (`regions.yaml:235`) | Haifa and the Sharon | **Zikhron Ya'akov** | W3 (Binyamina); Hadawi p. 89 | W3: "Binyamina was founded on PICA land by members of the Third Aliyah and people from the neighboring Zikhron Ya'akov." Hadawi p. 89: Binyamina Jewish cultivable total 8,903 | H | [S] [V] | One source names Zikhron Ya'akov as a source of Binyamina's settlers. It is one sentence in one secondary source (single-source). |
| F11 (2,15), coastal plain, on Pardes Hanna (`regions.yaml:236`) | Haifa and the Sharon | **owner picks: Haifa, Hadera, Zikhron Ya'akov or Netanya; no source separates them** (if a default is wanted: Hadera) | W4 (Pardes Hanna-Karkur); Hadawi p. 91, 48 | W4: "Pardes Hanna was founded in 1929 by Palestine Jewish Colonisation Association, just east of Karkur." Hadawi p. 91: Pardes Hanna Jewish cultivable total 17,320 (citrus 5,288) | I | [GAP] | Pardes Hanna is farmland on paper (17,320 dunums), but nothing read ties it to Hadera, Zikhron Ya'akov, Haifa or Netanya. All of those towns except Netanya are in Haifa Sub-District with it. Haifa is not excluded: it is in the region's inventory, and no rule or source read rules it out (its Table II line is blank, so it has no size to compare). If the owner wants a default: Hadera, which has the larger Jewish cultivable total of Hadera and Zikhron Ya'akov (15,026 against 7,512), the two with a printed total; Netanya was not compared. That is a size reading, labelled I. Plausible choices: Haifa, Hadera, Zikhron Ya'akov, Netanya. |
| F12 (3,13), coastal plain, on Zikhron Ya'akov (`regions.yaml:237`) | Haifa and the Sharon | **Zikhron Ya'akov** | W5; Hadawi p. 92 | W5: "Zikhron was one of the first Jewish agricultural towns to come under the wing of the Baron (along with Rishon LeZion and Rosh Pinna)". Hadawi p. 92: Zikhron Ya'aqov Jewish cultivable total 7,512 | H | [V] [S] | The field is on Zikhron Ya'akov, an agricultural moshava, with 7,512 Jewish cultivable dunums (plantations 3,906) in 1945. |
| F13 (6,10), coastal plain, on Yagur (`regions.yaml:238`) | Haifa and the Sharon | **no default: owner picks** (Haifa, Hadera, Zikhron Ya'akov or Netanya) | W12 (Yagur); Hadawi pp. 48, 91 | W12: "Located on the northeastern slopes of Mount Carmel, about 9 km southeast of Haifa". Hadawi p. 91: Mesheq Yagur Jewish cultivable total 3,799 | D | [GAP] | The only link read is distance (D), which is not evidence. Yagur is in Haifa Sub-District, but so are Hadera and Zikhron Ya'akov, so that does not pick Haifa. No source says Yagur's farmers belonged to Haifa. No default is offered, because the only reading that points at Haifa is distance. Plausible choices: Haifa, Hadera, Zikhron Ya'akov, Netanya. Haifa has no cultivable land printed (Table II). |
| F14 (6,12), coastal plain, on Mishmar HaEmek (`regions.yaml:239`) | Haifa and the Sharon | **no default: owner picks** (Haifa, Hadera, Zikhron Ya'akov or Netanya) | W13 (Mishmar HaEmek); Hadawi pp. 48, 91 | W13: "It was the first Jewish settlement in the southern part of the Jezreel Valley, built as part of Jewish National Fund efforts to settle the valley." Hadawi p. 91: Mishmar ha 'Emeq Jewish cultivable total 2,957 | I | [GAP] | Nothing read ties Mishmar HaEmek to any of the four cities. Its Sub-District is Haifa (shared with Hadera and Zikhron Ya'akov). No default is offered. The same limits as F13. Plausible choices: Haifa, Hadera, Zikhron Ya'akov, Netanya. |
| F15 (-2,21), coastal plain, on Petah Tikva (`regions.yaml:309`) | Tel Aviv and Jaffa | **Petah Tikva** | W14; Hadawi pp. 96, 53 | W14: "Petah Tikva, a center of citrus farming, was considered by both the British government and the Jaffa Electric Company as a potentially important consumer of electricity for irrigation." Hadawi p. 96: Petah Tiqva (Rural) Jewish cultivable total 17,966 (citrus 13,749) | H | [V] [S] | The field is on Petah Tikva, a citrus-farming town with 17,966 Jewish cultivable dunums in 1945. Tel Aviv's Table II line is blank (p. 97). |
| F16 (-1,20), coastal plain, on Kfar Saba (`regions.yaml:310`) | Tel Aviv and Jaffa | **Petah Tikva** | W15 (Kfar Saba); Hadawi pp. 52, 96 | W15: "The situation was defused when reinforcements from Petah Tikva arrived, and a peace was negotiated." and "Baruch Amrami, who transferred the administration of the Kfar Saba settlement from Petah Tikva to a local committee" and "It took another two years for other families to return (mostly from Petah Tikva)." | H | [S] [V] | Three statements in one article tie Kfar Saba's early years to Petah Tikva (defence in 1910, administration until a local committee in the 1920s, families returning after 1914-18). They concern the 1910s and 1920s, not 1948. Kefar Sava is in Jaffa Sub-District with both Petah Tikva and Tel Aviv (pp. 52-53), so that does not separate them. |
| F17 (-5,25), coastal plain, on Givat Brenner (`regions.yaml:371`) | Central plain | **no default: owner picks** (Rehovot or Rishon LeZion) | W16 (Givat Brenner); Hadawi pp. 115, 117 | W16: "Located around two kilometres (one mile) south of Rehovot". Hadawi p. 115: Giv'at Brenner Jewish cultivable total 2,411 | D | [GAP] | The only link read is distance (D), not evidence. W16 also says the land "had been purchased by Moshe Smilansky from the Arab landholders of Aqir and Zarnuqa"; no read source ties Smilansky's purchase to either city. Rehovot and Rishon LeZion are both in Ramle Sub-District with Giv'at Brenner. No default is offered, because the only reading that points at Rehovot is distance. Plausible choices: Rehovot, Rishon LeZion. |
| F18 (-4,25), coastal plain, on Hulda (`regions.yaml:372`) | Central plain | **owner picks: Rishon LeZion or Rehovot; no source separates them** (if a default is wanted: Rishon LeZion) | W17 (Hulda); Hadawi pp. 115, 117 | W17: "In 1905, the Anglo Palestine Bank purchased 2,000 dunams (2.0 km2) of land from the Saidun tribe for a Jewish settlement near the Jaffa–Jerusalem railway." Hadawi p. 115: Hulda Jewish cultivable total 2,295 | I | [GAP] | Nothing read ties the Hulda farm to Rehovot or Rishon LeZion; both are in Ramle Sub-District with it. The default is only so that F17 and F18 do not both go to one city (I, no historical or map basis). Plausible choices: Rishon LeZion, Rehovot. |
| F19 (-11,31), coastal plain, on Nir Am (`regions.yaml:705`) | Southern Coast | **none eligible** | `content/regions.yaml` member list; W18 (Nir Am) | W18: "Nir Am was established on 19 August 1943 by immigrants from Bessarabia who were members of the Gordonia youth movement" | R | [GAP] | Southern Coast holds none of the 12 census cities (see Decision A). Nir Am was a kibbutz founded 1943, and the owner already chose it as the region's D5 housing place (1 November 2026, #2113). It is not a census city. Options in Decision A. |
| F20 (-10,33), lowland, on Mishmar HaNegev (`regions.yaml:998`) | Negev | **none eligible** | `content/regions.yaml` member list; W19 (Mishmar HaNegev) | W19: "Mishmar HaNegev was founded in 1946 as one of the 11 points in the Negev settlements." | R | [GAP] | The Negev holds none of the 12 census cities. The hex note says "hex by nearest centre, no roster record" (`regions.yaml:998`), so even the place the hex stands on is a map reading. Options in Decision A. |

Counts: 20 rows. Proposed with evidence ([V] or [S], class H): F09, F10, F12, F15, F16 (5). Proposed on an administrative tie
only (class A): F07, F08 (2). Forced by the single eligible city (class R, [GAP] for the place): F03, F04, F05, F06 (4). Several
eligible cities and no source: F01, F02, F11, F13, F14, F17, F18 (7, [GAP]; a default only for F01, F02, F11, F18, labelled I; none for F13, F14, F17).
No eligible city: F19, F20 (2, [GAP]).

## Decision A: the two regions with no census city

The rule is "Yes, same region" (owner, 30 October 2026, quoted in #1717). R-POP-004 gives a census record the region whose
`member_locations` hold its city, so a region with no census city has no census records and no farmers could live in it.
The owner's options, with a recommendation:

1. **(Recommended) Keep the rule and name a host for these two fields that is not a census city.** The natural candidate is the
   region's D5 housing place, already approved on 1 November 2026: Nir Am for Southern Coast, Nirim for the Negev
   (#1717 body, D5: "Nirim, add it to the map (Recommended)"; #2113 not opened further). That needs a new kind of host in the ledger and a ruling on it. Reason for recommending it:
   it keeps the same-region rule and uses places the owner has already approved; the cost is that the farmers are not in a
   census city.
2. Add census-city values for a city in each region (Gaza, Beersheba). No Jewish census value for either exists in
   `content/cities.yaml`, and this note read no source for one, so it would be a new research item.
3. Let these two fields' farmers live in the nearest region's census city. This breaks "Yes, same region", and the issue forbids
   using nearest-city distance as a rule.
4. Drop the two fields from the 20. That changes the 2/6/6/2/2/0/0/1/1 split the owner approved (#1691 Q4), so it is the
   owner's call only.

## Sources (opened 2026-11-04 unless said otherwise)

- **H**: Sami Hadawi, *Village Statistics 1945*, PLO Research Center, Beirut, 1970; archive.org item
  `palestine-village-statistics-1945`, PDF pages cited in the tables above, page images viewed. Foreword (PDF p. 7) read as OCR text.
- **W1** en.wikipedia "Rosh Pinna", revision 1380257114. **W2** "Hadera", revision 1380232481. **W3** "Binyamina", revision
  1380231354 (title "Binyamina-Giv'at Ada"). **W4** "Pardes Hanna-Karkur", revision 1380231851. **W5** "Zikhron Ya'akov", revision
  1383265371. **W6** "Nahariya", revision 1380729065. **W7** "Hanita", revision 1374531326. **W8** "Nahalal", revision 1382836303.
  **W9** "Ein Harod", revision 1383054029. **W10** "Degania Alef", revision 1374529245. **W11** "Ashdot Ya'akov", revision
  1377925273. **W12** "Yagur", revision 1374538048. **W13** "Mishmar HaEmek", revision 1378097624. **W14** "Petah Tikva", revision
  1380221503. **W15** "Kfar Saba", revision 1380222750. **W16** "Givat Brenner", revision 1378101176. **W17** "Hulda, Israel",
  revision 1377268762. **W18** "Nir Am", revision 1380468384. **W19** "Mishmar HaNegev", revision 1375580227. All read through
  `https://en.wikipedia.org/w/api.php` (plain-text extract) on 4 November 2026.
- Repo files read: `content/regions.yaml`, `content/cities.yaml`, `docs/GDD.md:373`, `docs/rules/mvp.md` (R-POP-004 and the
  paragraph after it), `CONTRIBUTING.md`.

## What was tried and failed, and what stays [GAP]

- **The Israeli archives and the maps the issue lists** (State Archives, Haganah archive, settlement archives, CBS settlement
  histories, period maps) were not opened in this pass. The one printed primary source opened was the 1945 Government table.
  The seven "no source separates them" rows, and the place-specific links for F01, F02 and F03 to F06, may be closable there: a
  settlement history or a Mandate local-council record that says which town a kibbutz's or moshav's farmers traded, banked or
  were registered with. This note does not claim they cannot be closed.
- **No source read for 15 May 1948 itself.** The 1945 table (1 April 1945) is about 37 months before the opening date, and all "connection" quotations
  are from Wikipedia, which cites sources this box did not open. Single-source rows are marked in the reasoning (F10, F16), and their ties are to the 1910s to 1920s (F16) and to Binyamina's founding in 1922 (F10), not to 1948.
- **Wikipedia is a pointer, not the source.** Every [S] row should be read as "Wikipedia says", and the 1910s and 1920s ties for
  F16 say nothing about 1948.
- **Hadawi's Table II is a fiscal classification of 1 April 1945**, so a town's "Jewish cultivable total" shows land held, not
  who farmed it in May 1948.
- **The hex notes in `regions.yaml` were taken as given.** Every "on Hadera", "on Petah Tikva" style reading in the class H rows (F09, F12, F15) rests on those notes. This note did not re-check which hex each place stands on, and F20's
  hex is by the file's own words a "nearest centre" reading.

## Owner approval table

Request: please approve, change or refuse each row below. No `content/` or ledger wiring should start before that. "Evidence"
is the class and tag from the rows above.

| Field | Hex | Region | Proposed city | Evidence | Owner: approve / change |
|---|---|---|---|---|---|
| F01 | (9,5) | Galilee | owner picks: Rosh Pinna (recommended on regional evidence) or Safed | I, [GAP] | |
| F02 | (14,7) | Galilee | owner picks: Rosh Pinna (inference, no source ties it to the field) or Safed | I, [GAP] | |
| F03 | (7,11) | Valleys | Tiberias (forced) | R, [GAP] | |
| F04 | (7,12) | Valleys | Tiberias (forced) | R, [GAP] | |
| F05 | (8,13) | Valleys | Tiberias (forced) | R, [GAP] | |
| F06 | (9,13) | Valleys | Tiberias (forced) | R, [GAP] | |
| F07 | (11,12) | Valleys | Tiberias | A, [V] [S] | |
| F08 | (12,11) | Valleys | Tiberias | A, [V] [S] | |
| F09 | (1,15) | Haifa and the Sharon | Hadera | H, [V] [S] | |
| F10 | (2,14) | Haifa and the Sharon | Zikhron Ya'akov | H, [S] [V] | |
| F11 | (2,15) | Haifa and the Sharon | owner picks: Haifa, Hadera, Zikhron Ya'akov or Netanya (default if wanted: Hadera) | I, [GAP] | |
| F12 | (3,13) | Haifa and the Sharon | Zikhron Ya'akov | H, [V] [S] | |
| F13 | (6,10) | Haifa and the Sharon | owner picks: Haifa, Hadera, Zikhron Ya'akov or Netanya (no default) | D, [GAP] | |
| F14 | (6,12) | Haifa and the Sharon | owner picks: Haifa, Hadera, Zikhron Ya'akov or Netanya (no default) | I, [GAP] | |
| F15 | (-2,21) | Tel Aviv and Jaffa | Petah Tikva | H, [V] [S] | |
| F16 | (-1,20) | Tel Aviv and Jaffa | Petah Tikva | H, [S] [V] | |
| F17 | (-5,25) | Central plain | owner picks: Rehovot or Rishon LeZion (no default) | D, [GAP] | |
| F18 | (-4,25) | Central plain | owner picks: Rishon LeZion or Rehovot (default if wanted: Rishon LeZion) | I, [GAP] | |
| F19 | (-11,31) | Southern Coast | none eligible: Decision A | R, [GAP] | |
| F20 | (-10,33) | Negev | none eligible: Decision A | R, [GAP] | |

Also asked: **Decision A** (above, recommended option 1).

## What was opened and checked (AI assistance)

Assistant: Claude Code, 4 November 2026. Opened and read: 19 English Wikipedia articles through the API at the revisions in
"Sources" (plain-text extract, searched for the quoted sentences); 24 page images of *Village Statistics 1945* (PDF pages 40, 41, 43, 48,
52, 53, 62, 68, 71, 72, 73, 76, plus the Table II pages 89, 90, 91, 92, 96, 97, 115, 117, 118, 120, 123, 127; pages were rotated where the
table is printed sideways), each row in the evidence table read on the image; the Foreword in OCR text.
Not opened: any Israeli archive, map, CBS settlement history or Hebrew source, and the Wikipedia citations behind each article.
Not checked: the hex positions in `content/regions.yaml`, and no figure in this note was recomputed from `content/`. The
submitting contributor stands behind these opens and reads as `CONTRIBUTING.md` describes, and the tag for every [V] and [S]
above is **checked by beriki770-ship-it, AI-assisted**. The check on the 20 field hexes themselves (`regions.yaml:17-20`,
"a person's check pending") is a different item and is not closed by this note.
