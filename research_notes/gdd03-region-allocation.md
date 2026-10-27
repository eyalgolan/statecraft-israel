# Opening fields, housing and Industry by region (#1691)

Owner decision, 27 October 2026, exact label **"Approve as proposed"**: the per-region split of the opening prepared fields, permanent housing projects (with their places) and Industry slots below, for GDD 0.3 (#1691). Industry follows population. The farmer tier mix per region stays Deferred. Every row is PARTIAL-EVIDENCE.

These are design values derived from one named historical statistic by a stated rule. They are not a claim that the Yishuv had exactly this many "fields", "housing projects" or "Industry slots" in a region on 15 May 1948. The rows live in the evidence ledger [gdd03-regions-capitals-fields.yaml](gdd03-regions-capitals-fields.yaml), in each region's `<region>_opening_field_allocation`, `<region>_opening_housing_allocation` and `<region>_opening_industry_allocation` rows. Regional activation stays off: nothing here reaches the runtime catalogue, the loader or any shipped era.

## Approved split

| Region (id) | Mandate sub-districts (approved anchor) | Fields | Housing projects (places) | Industry | Evidence |
| --- | --- | ---: | ---: | ---: | --- |
| Galilee and Hula (`galilee`) | Acre, Safad | 2 | 1 (3) | 0 | PARTIAL-EVIDENCE |
| Galilee Valleys (`valleys`) | Nazareth, Tiberias, Beisan | 6 | 1 (3) | 0 | PARTIAL-EVIDENCE |
| Haifa, Carmel and Sharon (`haifa_sharon`) | Haifa, Tulkarm | 6 | 5 (15) | 2 | PARTIAL-EVIDENCE |
| Tel Aviv and Jaffa (`tel_aviv_jaffa`) | Jaffa | 2 | 12 (36) | 4 | PARTIAL-EVIDENCE |
| Central Plain (`central_plain`) | Ramle | 2 | 1 (3) | 0 | PARTIAL-EVIDENCE |
| Jerusalem Corridor and Southern Hills (`jerusalem_hills`) | Jerusalem, Ramallah, Hebron | 0 | 4 (12) | 2 | PARTIAL-EVIDENCE |
| Northern Central Hills (`northern_hills`) | Nablus, Jenin | 0 | 0 (0) | 0 | PARTIAL-EVIDENCE |
| Southern Coastal Plain (`southern_coast`) | Gaza | 1 | 0 (0) | 0 | PARTIAL-EVIDENCE |
| Negev (`negev`) | Beersheba | 1 | 0 (0) | 0 | PARTIAL-EVIDENCE (weakest) |
| Total | | 20 | 24 (72) | 8 | |

The totals equal the fixed national values in `content/gdd03-balance.yaml`: `start.prepared_fields` 20, `start.permanent_housing_projects` 24, `start.housing_capacity` 72 and `start.industry_slots` 8. `start.farmers` stays 5 / 10 / 5 (novice / trained / veteran) nationally; its split by region is Deferred for every region.

## Source

S1. Government of Palestine, Department of Statistics, *Village Statistics, 1945* (Jerusalem, April 1945).

- Locator: "Summary for Palestine", as at 1.4.45, scan PDF page 4 (land by category and owner; population by religion; by sub-district). Explanatory Note, scan PDF page 3 (method, categories, source records).
- URL: https://commons.wikimedia.org/wiki/File:Village_Statistics_1945_full.pdf (direct scan https://upload.wikimedia.org/wikipedia/commons/1/17/Village_Statistics_1945_full.pdf), 52,198,759 bytes, SHA-256 `b265a1dab532486945f3840bb435bc9f2465188abd41173bf4af37de95ece51e`. Checked 2026-10-27.
- Licence: public domain (Government of Palestine, 1945). The approved regions ledger already cites this file for sub-district membership.
- Reading: numbers were read from the rendered page image, not from the OCR text layer. Only the Jewish columns are used.

What S1 measures (Explanatory Note, scan PDF page 3):

- Part "B", Areas and Ownership, paras 1-2: areas and ownership are "extracted from the Tax Distribution Lists, prepared under the provisions of the Rural Property Tax Ordinance, 1942, ... and the Commuted Tithe records for Beersheba Sub-District". Holdings are divided into Arabs, Jews, Public and Others. Categories are tax categories by land quality: 1-2 citrus, 3 bananas, 4 built-on, 5-8 irrigated land and plantations, 9-15 ground crop land, 16 uncultivable.
- Part "A", Population, paras 2, 5 and 6: end-1944 estimates, described as "rough estimates"; the Jewish figures were corrected with Jewish Agency and Food Controller data; sub-district figures are more reliable than locality figures.

## Inputs (S1, Summary for Palestine, Jewish columns)

"Cultivable" here is categories 1-2 Citrus + 3 Bananas + 5-8 Plantations + 9-13 Taxable cereal + 14-15 Taxable cereal. Excluded: urban areas, category 4 built-on area and category 16 uncultivable. Land is in dunums; population is Jews, end-1944 estimate.

| Sub-district | Citrus | Bananas | Plantations | Cereal 9-13 | Cereal 14-15 | Cultivable total | Jewish population |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Acre | 197 | 12 | 1,382 | 7,494 | 346 | 9,431 | 2,950 |
| Safad | 66 | 0 | 20,504 | 42,948 | 27,488 | 91,006 | 6,700 |
| Nazareth | 430 | 0 | 5,008 | 119,181 | 0 | 124,619 | 7,600 |
| Tiberias | 1,243 | 615 | 6,995 | 106,045 | 20,921 | 135,819 | 13,100 |
| Beisan | 1,221 | 155 | 7,679 | 103,200 | 4,498 | 116,753 | 7,000 |
| Haifa | 18,475 | 71 | 29,267 | 188,586 | 9,983 | 246,382 | 104,510 |
| Tulkarm | 31,859 | 131 | 1,991 | 91,890 | 0 | 125,871 | 14,900 |
| Jaffa | 41,192 | 37 | 5,137 | 40,244 | 380 | 86,990 | 264,100 |
| Ramle | 40,153 | 57 | 8,769 | 50,849 | 507 | 100,335 | 29,420 |
| Jerusalem | 0 | 0 | 2,333 | 9,333 | 1,787 | 13,453 | 100,200 |
| Ramallah | 0 | 0 | 0 | 45 | 22 | 67 | 0 |
| Hebron | 0 | 0 | 474 | 2,496 | 1,790 | 4,760 | 80 |
| Nablus | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Jenin | 0 | 0 | 0 | 4,173 | 0 | 4,173 | 0 |
| Gaza | 4,892 | 1 | 4,628 | 37,474 | 117 | 47,112 | 2,890 |
| Beersheba | 0 | 0 | 0 | 65,151 | 0 | 65,151 | 150 |
| Palestine | 139,728 | 1,079 | 94,167 | 869,109 | 67,839 | 1,171,922 | 553,600 |

Transcription check: every column sums exactly to the printed Grand Total for Palestine (139,728 / 1,079 / 94,167 / 869,109 / 67,839 / 553,600), and the district subtotals were checked against their printed TOTAL rows (for example Galilee District plantations 41,568, cereal 9-13 378,868, cereal 14-15 53,253).

## Method

Largest-remainder (Hamilton) apportionment. For each column, quota = national total x region basis / national basis. Each region gets the integer part of its quota; the seats left over go one each to the largest fractional remainders. Ties would be broken by region id; none occurred. The integers always sum exactly to the fixed totals.

- Fields (20 seats): basis = Jewish cultivable dunums.
- Housing projects (24 seats): basis = Jewish population, end-1944.
- Industry slots (8 seats): basis = Jewish population, end-1944. No Jewish industrial statistic by town or sub-district was reached (gap G3), so population is used as a labelled proxy. This is the owner's ruling ("Industry follows population").
- Places: 3 per project. The catalogue has no places-per-project key; 72 / 24 = 3 is implied by `start.housing_capacity` (dependency H1).

### Fields (basis 1,171,922 dunums)

| Region | Basis | Quota | Floor | Remainder | Fields |
| --- | ---: | ---: | ---: | ---: | ---: |
| galilee | 100,437 | 1.7141 | 1 | 0.7141 (2nd) | 2 |
| valleys | 377,191 | 6.4371 | 6 | 0.4371 (5th) | 6 |
| haifa_sharon | 372,253 | 6.3529 | 6 | 0.3529 | 6 |
| tel_aviv_jaffa | 86,990 | 1.4846 | 1 | 0.4846 (4th) | 2 |
| central_plain | 100,335 | 1.7123 | 1 | 0.7123 (3rd) | 2 |
| jerusalem_hills | 18,280 | 0.3120 | 0 | 0.3120 | 0 |
| northern_hills | 4,173 | 0.0712 | 0 | 0.0712 | 0 |
| southern_coast | 47,112 | 0.8040 | 0 | 0.8040 (1st) | 1 |
| negev | 65,151 | 1.1119 | 1 | 0.1119 | 1 |
| Total | 1,171,922 | 20 | 16 | 4 seats | 20 |

Close calls: the 4th remainder seat went to tel_aviv_jaffa (0.4846) over valleys (0.4371); galilee (0.7141) and central_plain (0.7123) differ by 0.0018.

### Housing projects (basis 553,600 people)

| Region | Basis | Quota | Floor | Remainder | Projects | Places |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| galilee | 9,650 | 0.4184 | 0 | 0.4184 (2nd) | 1 | 3 |
| valleys | 27,700 | 1.2009 | 1 | 0.2009 | 1 | 3 |
| haifa_sharon | 119,410 | 5.1767 | 5 | 0.1767 | 5 | 15 |
| tel_aviv_jaffa | 264,100 | 11.4494 | 11 | 0.4494 (1st) | 12 | 36 |
| central_plain | 29,420 | 1.2754 | 1 | 0.2754 | 1 | 3 |
| jerusalem_hills | 100,280 | 4.3474 | 4 | 0.3474 (3rd, no seat) | 4 | 12 |
| northern_hills | 0 | 0 | 0 | 0 | 0 | 0 |
| southern_coast | 2,890 | 0.1253 | 0 | 0.1253 | 0 | 0 |
| negev | 150 | 0.0065 | 0 | 0.0065 | 0 | 0 |
| Total | 553,600 | 24 | 22 | 2 seats | 24 | 72 |

If places were apportioned on their own (72 seats, same basis), the result would be galilee 1, valleys 4, haifa_sharon 16, tel_aviv_jaffa 34, central_plain 4, jerusalem_hills 13, others 0, which does not equal 3 x projects (dependency H1).

### Industry slots (basis 553,600 people)

| Region | Quota | Floor | Remainder | Slots |
| --- | ---: | ---: | ---: | ---: |
| galilee | 0.1395 | 0 | 0.1395 | 0 |
| valleys | 0.4003 | 0 | 0.4003 (5th) | 0 |
| haifa_sharon | 1.7256 | 1 | 0.7256 (2nd) | 2 |
| tel_aviv_jaffa | 3.8165 | 3 | 0.8165 (1st) | 4 |
| central_plain | 0.4251 | 0 | 0.4251 (4th) | 0 |
| jerusalem_hills | 1.4491 | 1 | 0.4491 (3rd) | 2 |
| northern_hills | 0 | 0 | 0 | 0 |
| southern_coast | 0.0418 | 0 | 0.0418 | 0 |
| negev | 0.0022 | 0 | 0.0022 | 0 |
| Total | 8 | 5 | 3 seats | 8 |

Close call: the 3rd remainder seat went to jerusalem_hills (0.4491) over central_plain (0.4251) and valleys (0.4003).

### Reproduction

Run with `python3 -I`; it asserts the printed totals and prints every quota above.

```python
from fractions import Fraction as F
SD = {  # sub-district: (citrus, bananas, plantations, cereal 9-13, cereal 14-15, jewish population)
 "Acre":(197,12,1382,7494,346,2950), "Safad":(66,0,20504,42948,27488,6700),
 "Nazareth":(430,0,5008,119181,0,7600), "Tiberias":(1243,615,6995,106045,20921,13100),
 "Beisan":(1221,155,7679,103200,4498,7000), "Haifa":(18475,71,29267,188586,9983,104510),
 "Tulkarm":(31859,131,1991,91890,0,14900), "Jaffa":(41192,37,5137,40244,380,264100),
 "Ramle":(40153,57,8769,50849,507,29420), "Jerusalem":(0,0,2333,9333,1787,100200),
 "Ramallah":(0,0,0,45,22,0), "Hebron":(0,0,474,2496,1790,80), "Nablus":(0,0,0,0,0,0),
 "Jenin":(0,0,0,4173,0,0), "Gaza":(4892,1,4628,37474,117,2890), "Beersheba":(0,0,0,65151,0,150)}
PRINTED = [139728, 1079, 94167, 869109, 67839, 553600]
REG = [("galilee",["Acre","Safad"]),("valleys",["Nazareth","Tiberias","Beisan"]),
 ("haifa_sharon",["Haifa","Tulkarm"]),("tel_aviv_jaffa",["Jaffa"]),("central_plain",["Ramle"]),
 ("jerusalem_hills",["Jerusalem","Ramallah","Hebron"]),("northern_hills",["Nablus","Jenin"]),
 ("southern_coast",["Gaza"]),("negev",["Beersheba"])]
for i, printed in enumerate(PRINTED):
    assert sum(v[i] for v in SD.values()) == printed
def hamilton(w, total):
    W = sum(w.values()); q = {k: F(v * total, W) for k, v in w.items()}; a = {k: int(x) for k, x in q.items()}
    for k in sorted(q, key=lambda k: (-(q[k] - a[k]), k))[:total - sum(a.values())]: a[k] += 1
    return q, a
land = {r: sum(sum(SD[s][:5]) for s in ss) for r, ss in REG}
pop = {r: sum(SD[s][5] for s in ss) for r, ss in REG}
for name, w, total in [("fields", land, 20), ("housing projects", pop, 24), ("industry slots", pop, 8)]:
    q, a = hamilton(w, total)
    print(name, {r: (round(float(q[r]), 4), a[r]) for r, _ in REG})
```

## Why every row is PARTIAL-EVIDENCE

The statistic is official and reconciles exactly, but it measures something close to, not the same as, the game quantity, and at a different date.

- L1, ownership, not cultivation. The categories classify land Jews owned by fiscal quality. They do not say the land was farmed by a Jewish settlement in May 1948; owned land could be leased, fallow or reserve.
- L2, date. Land is as at 1 April 1945 and population is the end-1944 estimate, three years before 15 May 1948. Land purchases, settlements founded after April 1945 and population movement up to May 1948 are not reflected; their size was not researched.
- L3, control on 15 May 1948. The statistic ignores who held the land on the opening day. Opening control stays with the per-place evidence in the ledger; nothing here infers control.
- L4, region extent. The approved regions are anchored on whole sub-districts, and this split uses each region's approved `reference_subdistricts` exactly. The approved member rows with a Village Statistics listing all fall inside their region's sub-districts except one ambiguous locality (`ijzim`, listed under both Nazareth and Haifa); 55 members carry no sub-district listing, so their match was not checked. Where a region boundary departs from the sub-district line, the statistic cannot follow it without the per-village tables (gap G4).

Row notes:

- negev is the weakest row. Beersheba's land figure comes from the Commuted Tithe records, not the Tax Distribution Lists (S1, part B), and all 65,151 Jewish dunums are classed as cereal 9-13, a coarser record than the other rows. Its one field rests on that figure (quota 1.11). With the Beersheba basis removed (national basis 1,106,771) the same rule gives galilee 2, valleys 7, haifa_sharon 7, tel_aviv_jaffa 1, central_plain 2, jerusalem_hills 0, northern_hills 0, southern_coast 1, negev 0. The owner approved the split with the Negev field.
- tel_aviv_jaffa: 264,100 is the Jewish population of Jaffa Sub-District; its two fields are mostly citrus (41,192 dunums).
- jerusalem_hills: quota 0.31 fields gives 0. Housing and Industry follow end-1944 population only; which holdings were still held on 15 May 1948 is not assessed (L3).
- northern_hills: no Jewish population in S1; 4,173 dunums of Jewish-owned cereal land in Jenin Sub-District give quota 0.07.
- haifa_sharon: the reception camp binds here in the approved ledger; no extra housing is added for it (reception beds are separate catalogue keys).

## Deferred and open

- Farmer tier mix by region (gap G1): no source gives novice / trained / veteran farmers by region; Deferred for every region. The national 5 / 10 / 5 is unchanged.
- Which hexes are the fields: each field row keeps `hexes` and `prepared_at_opening` empty. Placement waits for Amir's Q36 on #1668 (what makes a starting field workable).
- Q37 (Amir, #1668): friendly control of an empty hex. Regions with fields but uncertain opening control (Negev, Southern Coast) depend on it.
- Housing region: southern_coast and negev get fields but no housing. Whether farmers must be housed in their own region is a design question, not a historical one.
- H1: places per starting housing project (3 is implied by 72 / 24, not a catalogue key).
- Jerusalem: whether its opening-day situation should change its Industry was not researched.
- Runtime: the regions schema, loader and regional pools carry no start allocation. These ledger rows are the source for that later work, once region member hexes and #1690 initialization exist.

## Gaps and sources not reached

- G1: farmer tier mix by region; no source.
- G2: a May 1948 (not April 1945) Jewish cultivated-area statistic by sub-district. Candidates not reached in free full text: Jewish Agency Agricultural Settlement Department reports 1947-48, Statistical Abstract of Israel 1949/50, Survey of Palestine Supplement (1947).
- G3: Jewish industrial establishments or employment by town or sub-district. S2, *A Survey of Palestine* (prepared December 1945 and January 1946 for the Anglo-American Committee of Inquiry), Vol. II, ch. XIV, Table 3 "Ownership of industry in Palestine (as found at the census of industry, 1943)", PDF page 41, printed p.567 (https://www.bjpa.org/content/upload/bjpa/a_su/A%20SURVEY%20OF%20PALESTINE%20DEC%201945-JAN%201946%20VOL%20II.pdf), gives the 1943 census only nationally (Jewish establishments 1,907; persons engaged 37,773). A text search of Vol. I, ch. XIII (https://www.bjpa.org/content/upload/bjpa/a_su/A%20SURVEY%20OF%20PALESTINE%20DEC%201945-JAN%201946%20VOL%20I.pdf) found no industry table by town or district. Both checked 2026-10-27; neither is cited for any number above.
- G4: per-village Jewish land, needed only where a region boundary departs from the sub-district line. The tables exist in S1 (scan pages 5-34) but were not transcribed.
- G5: places per housing project (H1); design, not history.

The Wikipedia article "Village Statistics, 1945" appeared in search results and was not used as a source.

## How this was made

AI-assisted research, 27 October 2026. An AI agent read S1 from the page images and reconciled every column to the printed totals; a second agent re-read the population and cereal 14-15 Jewish columns from the page images before this note was written. No person has checked these readings yet; historical review stays NEEDS-HUMAN-CHECK, as for the rest of the ledger.
