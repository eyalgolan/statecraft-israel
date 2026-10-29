# Approved region design associations

Region review index: inclusive physical lines in `gdd03-regions-capitals-fields.yaml`. Each region range includes its definition, members, candidate hexes and ancillary rows. Shared material is reviewed once.

| Region | Ledger lines |
| --- | --- |
| galilee | 171–6232 |
| valleys | 6233–9833 |
| haifa_sharon | 9834–16918 |
| tel_aviv_jaffa | 16919–20479 |
| central_plain | 20480–26801 |
| jerusalem_hills | 26802–35423 |
| northern_hills | 35424–40855 |
| southern_coast | 40856–49639 |
| negev | 49640–51316 |
| Shared provenance, camp and loader context | 20–170 |
| Shared gaps, retained roster, counts and source-layer notes | 51317–51699 |

Owner decision, 27 Oct 2026, relayed by the coordinator: **APPROVED**: all six choices approved as proposed; the nine-region draft was separately approved by name on 27 October 2026 (owner ruling, 27 October 2026). This is approval of the supplied design content by name. Historical rows still need a person's check. SOURCED means the inherited narrow assertion has cited evidence; it does not mean a person has checked this adaptation.

| Decision | Exact owner label | Recorded value |
| --- | --- | --- |
| Tel Aviv–Jaffa capital | "Tel Aviv" | tel_aviv |
| Central Plain capital | "Rishon LeZion" | rishon_lezion |
| Valleys capital | "Tiberias" | tiberias |
| Kiryat Shmuel camp design region | "Haifa-Sharon" | haifa_sharon |
| Haifa-Sharon extent | "Keep as proposed" | Existing associations retained |
| Jerusalem extent | "Keep as proposed" | Existing associations retained |
| Opening fields, housing and Industry per region (#1691) | "Approve as proposed" | Each region's opening allocation rows; see [gdd03-region-allocation.md](gdd03-region-allocation.md) |

The evidence ledger is [gdd03-regions-capitals-fields.yaml](gdd03-regions-capitals-fields.yaml). It preserves all 326 member rows, shared candidate hexes, source citations, control objects, field/facility caveats and the retained roster from the approved revision. The design decisions above are separate from `capital_status`, `boundary_status`, `binding_status`, control and human-review fields. Safed, Haifa, Jerusalem, Nablus, Gaza and Beersheba retain their individual capital status; none is newly designated APPROVED.

The camp is `kiryat_shmuel_camp_haifa`. Only its design `region_binding` becomes `haifa_sharon`. Its footprint remains NEEDS-HUMAN-CHECK, coordinates/hex remain unknown, and opening control stays PARTIAL-EVIDENCE with a null value. `camp_80` and `shaar_haaliyah` retain their separate associations and facility caveats; neither substitutes for this camp or proves its control, operation or position.

## Schema adaptation and activation

The follow-up adaptation supplies `research_notes/gdd03-regions.schema-input.yaml`, mapping each region's id, capital, full member-id list, disclaimer, provenance, capital approval, binding status and opening control to the shared geography schema. All nine original `boundary_hexes` are null. The follow-up schema `member_hexes` lists are empty, an explicit absence of geometry. Candidate marker hexes are preserved only in the evidence ledger; they are not substituted for full extents.

**All nine rows fail the strict loader** at `player_regions[0].member_hexes` when submitted separately: `galilee`, `valleys`, `haifa_sharon`, `tel_aviv_jaffa`, `central_plain`, `jerusalem_hills`, `northern_hills`, `southern_coast`, `negev`. A combined submission stops at the first row. Later placement/identity/overlap checks are not reached; they are not claimed to pass. Haifa-Sharon's 52 and Jerusalem's 61 named associations do not resolve their boundaries or place bindings.

The follow-up adaptation supplies `content/regions.yaml` with an empty `player_regions` list for the existing exact `gdd-0.3` reader; legacy/gdd-0.1 do not read it. This evidence import supplies no runtime content. Regional activation remains off. No incomplete row is loaded, no validator is weakened, and no production initializer changes.

The follow-up `research_notes/gdd03-regions.projections.yaml` supplies only the existing Tel Aviv/Jaffa, Rishon/Rehovot and Tiberias named-place projections for acceptance tests. Their coordinates come from those members' existing candidate marker rows. These are test inputs outside runtime discovery, not substitutes for the nine full regions. Distinct co-located identities remain distinct.

## Remaining evidence and gates

The 385 control objects retain 33 SOURCED, 255 PARTIAL-EVIDENCE and 97 NEEDS-RESEARCH statuses. Every unconfirmed value stays null. Historical, shared-hex and facility control are not inferred from associations or member control. Same-day captures remain partial, including Tamra, Umm ez Zinat, Fajja and Mughar. The Old City ruling changes no extent or control row here.

No approved set of opening prepared-field coordinates/workability/control/staffing bindings is supplied. Existing future field markers remain evidence only, not opening slots. The per-region opening counts of prepared fields, housing projects (places) and Industry slots are recorded in each region's `opening_field_allocation`, `opening_housing_allocation` and `opening_industry_allocation` rows by owner ruling ("Approve as proposed", owner ruling, 27 October 2026, #1691); every row is PARTIAL-EVIDENCE, and source, method and limits are in [gdd03-region-allocation.md](gdd03-region-allocation.md). By that ruling the housing and Industry counts follow Jewish population, for this opening allocation only; no facility inventory is inferred from population. Field hexes stay empty and the farmer tier mix by region stays Deferred. No `gdd03-fields.yaml`, balance key, Amir number, army value or population is added, and nothing reaches the runtime catalogue. `A` remains Deferred and free civilians remain `34 - A` in the inherited ledger. Staffing belongs to #1717 and farming mechanics to their existing owners.

Historical row review, schema/runtime acceptance, all managed and Unity gates, and legacy/gdd-0.1 byte identity remain PENDING. Test source exercises the shared strict loader and existing golden manifest; it is not evidence of a run. No human review sign-off is supplied by this document or by design approval.

## Public source index and evidence conventions

Research dated 27 Oct 2026; sources below are inherited research, not independently fetched for this import. Stable `gdd03-NNNN` identifiers preserve the numeric join. Each `sources[].evidence: embedded_public_citations` record retains its `research_item`, research status, qualifications and original citation excerpts. Public URLs, revisions, dates, page/section locators and short quotations in those excerpts are the evidence locators. A source definition and its immediately following line fragments form one citation; repeated source aliases resolve within the same list. Every inherited citation fragment now carries an `at` locator into one of the public research notes listed below, using that note’s final physical line numbers.

`locator_status: repository_note` means the citation fragment is available at its `at` path and physical line in a public research note. All 2,310 fragments have such locators; none is marked unavailable. Public URLs already carried in the excerpts remain unchanged. The notes preserve source-access limits and open gaps: availability of a research finding does not make an inaccessible primary source available or change a factual status. Other repository paths refer to main at `307bef9fe7c82bf8fd186eae30f67ec33df1b17e`; embedded older revision/line labels remain historical provenance, not assertions that current line numbers match.

| Public reference | Date and locator | Evidence and limit |
| --- | --- | --- |
| [Administrative anchors and capitals](Map/regions-1948-opening-control-p1.md) | 27 Oct 2026; 19 research items; item headings and cited pages | Public AI research note; original findings and PARTIAL item statuses retained; later design approval is recorded above. |
| [Northern and coastal opening control](Map/regions-1948-opening-control-p2a.md) | 27 Oct 2026; 146 control items; exact `at` lines in ledger | Public AI research note; pinned revisions, quotations, source-access limits and open gaps retained. |
| [Central and southern opening control](Map/regions-1948-opening-control-p2b.md) | 27 Oct 2026; 181 control items including Kiryat Shmuel camp; exact `at` lines in ledger | Public AI research note; region-local source aliases, quotations, source-access limits and open gaps retained. |
| [Palestine Gazette 1552](https://commons.wikimedia.org/wiki/File:PalestineDistricts1947.pdf), [direct scan](https://upload.wikimedia.org/wikipedia/commons/9/95/PalestineDistricts1947.pdf) | 30 Jan 1947; pp.125–133; effective 1 Jan 1947 | Schedule of administrative divisions. Inherited reading: pp.132–133 page images; other pages text layer, with interleaved-column caveat. Design associations are not administrative boundaries. |
| [Village Statistics 1945](https://commons.wikimedia.org/wiki/File:Village_Statistics_1945_full.pdf), [direct scan](https://upload.wikimedia.org/wikipedia/commons/1/17/Village_Statistics_1945_full.pdf) | Apr 1945; Explanatory Note A ¶3, scan p.3; Haifa table serials 22/29, scan p.14; footnotes (d)/(m), scan p.16 | Short quote: “the cheflieu of each sub-district”. Footnote (d): “Qiryat Shmuel—160”. Locality membership does not locate the camp footprint. |
| [A Survey of Palestine I](https://www.bjpa.org/content/upload/bjpa/a_su/A%20SURVEY%20OF%20PALESTINE%20DEC%201945-JAN%201946%20VOL%20I.pdf) | Government Printer, 1946; ch.III ¶5 p.104 / PDF109; ch.IV ¶11 p.112 / PDF117; ch.VII ¶¶10–11 p.145 / PDF150 | Short quotes: “six administrative districts”; “Lydda, (headquarters Jaffa)”. Headquarters and chefs-lieux are evidenced; no dated act designating sub-district capitals was located. First two pages were reportedly image-read; p.145 text-layer only. |
| [William Fuller Collection GB165-0114](https://www.sant.ox.ac.uk/wp-content/uploads/2025/02/GB165-0114-William-Fuller-Collection.pdf) | Material Apr–May 1948; catalogue description of Lydda district commissionership; research 27 Oct 2026 | Short quote: “District Commissioner, Lydda (with headquarters in Jaffa), 1948”. Catalogue evidence, not the underlying papers. Supplements the Tel Aviv/Jaffa seat distinction. |
| `research_notes/Map borders/subdistrict-names-and-amendments-1947-48.md` and `content/map/subdistricts_1948.geojson` | Gazette 1415 headings; amendment check 1 Jan 1947–28 Apr 1948; source metadata / open questions | No amendment found in that checked interval. 29 Apr–14 May 1948 and the missing 1947 Hebrew schedule remain unavailable. Digitised lines have uncertainty; no conversion to runtime geometry. |
| `research_notes/Map borders/control-15-may-1948.md` | Sections 1, 3, 4 and 6; CIA maps and S/1025 ¶28 as cited there | Regional context only. Short quote, §1 item4: “The south cannot be coloured by side on 15 May”. No place-specific holder or camp perimeter follows. |
| [Morris, Key to Map 2](https://books.google.co.il/books?id=uM_kFX6edX8C&pg=PR16) | 2004; pp.xvi–xix; research 27 Oct 2026 | Abandonment dates and causes, not control. Body text unavailable in the inherited research; Khalidi and other book pages cited through secondary accounts still need direct human checking. |
| [Battles of Latrun, R-LAT](https://en.wikipedia.org/w/index.php?oldid=1370603666), [R-LAT-HE](https://he.wikipedia.org/w/index.php?oldid=43792411), [Operation Barak, R-BARAK](https://he.wikipedia.org/w/index.php?oldid=43021951) | Revisions named in ledger source definitions; research 27 Oct 2026; events 4–31 May 1948 | Resolves source aliases whose definitions give titles and revisions without URLs. Conflicting 14–18 May accounts remain partial. R-CIA resolves to the public control note above; its map was not re-read. |

Administrative-anchor exceptions carried from the dated research: Gazette p.128 places Mishmar HaEmek in Haifa SD, outside the Valleys anchor; p.132 places Hulda and Latrun in Ramle SD, outside the Jerusalem anchor; Ijzim's double Nazareth/Haifa tag conflicts with the Haifa-only Gazette placement on p.128. Ra'anana is listed in Jaffa SD on p.132 despite the digitised line's near-border placement. These are review flags, not edits to the approved memberships. The Jerusalem anchor covers Ramallah to the north; “West Jerusalem” is not a Mandate administrative unit. The two approved non-seat capitals remain design choices.

## Additional evidence retained without changing control

The following compact entries preserve evidence or qualifications absent from the ledger's research-item joins. Research date is 27 Oct 2026 throughout. Every listed control remains NEEDS-RESEARCH/null in the ledger; later events and undated statements do not settle 15 May 1948. Quotations are inherited excerpts and still require a person's check.

| Stable item / place | Public source, date, locator and short quote | Remaining gap |
| --- | --- | --- |
| gdd03-0261 / shaar_haaliyah | [Hebrew Wikipedia revision 42247530](https://he.wikipedia.org/w/index.php?oldid=42247530), history; event Mar 1949: “במרץ 1949 הוסב המחנה הצבאי הבריטי” | Conversion of St. Luke's military camp in 1949 does not establish the May 1948 holder or British enclave membership. |
| gdd03-0262 / camp_80 | [Hebrew Wikipedia revision 43463565](https://he.wikipedia.org/w/index.php?oldid=43463565), history; short date quote: “ב-6 באפריל 1948” | April attack and 13 July trainee arrival do not name the 15 May holder. |
| gdd03-0302 / al_safiriyya | [Morris p.xviii](https://books.google.co.il/books?id=uM_kFX6edX8C&pg=PR18), 2004: “220 Safiriya, al - Not known”; [English Wikipedia revision 1377920002](https://en.wikipedia.org/w/index.php?oldid=1377920002), lead: “on May 20, 1948” | Morris gives no abandonment date, Wikipedia gives 20 May, al-Aref via Khalidi gives April. The conflict cannot settle opening control. |
| gdd03-0377 / nilin; gdd03-0381 / rantis; gdd03-0383 / qibya | [Ni'lin](https://en.wikipedia.org/wiki/Ni%27lin), [Rantis](https://en.wikipedia.org/wiki/Rantis), [Qibya](https://en.wikipedia.org/wiki/Qibya), postwar sections; 1949 context: “Jordanian rule” | No dated 15 May source; postwar status cannot establish it. Iraqi/Legion deployment records unavailable. |
| gdd03-0452 / sur_bahir | [Sur Baher](https://en.wikipedia.org/wiki/Sur_Baher), Jordanian-era account, citing Dinstein & Tabory 1994 p.41: “occupied and later annexed by Jordan” | Undated. Ramat Rachel search snippet concerns 21–25 May; the attempted English battle page was unavailable. |
| gdd03-0480 / beit_hanina | `research_notes/Map borders/control-15-may-1948.md`, §3; nearby evacuations 17–18 May | Place-specific evidence unavailable. Atarot/Neve Yaakov context does not establish this village's holder. |
| gdd03-0490 / beit_iksa | [Beit Iksa](https://en.wikipedia.org/wiki/Beit_Iksa), Apr 1948 account citing Morris 1987 pp.114/158: “the Haganah entered the village destroying many buildings” | Whether troops remained after the raid is unavailable. |
| gdd03-0492 / beit_safafa | [Beit Safafa](https://en.wikipedia.org/wiki/Beit_Safafa), postwar account citing Klein 2007 p.70: “the village was divided in two” | No dated opening-control record. |
| gdd03-0522 / battir | [Battir](https://en.wikipedia.org/wiki/Battir), undated 1948 account citing FAZ, 21 Jun 2012: “thought Battir was still inhabited and gave up attacking” | A dated 1948 record is unavailable. |
| gdd03-0689 / ad_halom_bridge | [Jisr Isdud](https://en.wikipedia.org/wiki/Jisr_Isdud), 1948 account citing Dvir, Haaretz 10 Mar 2010: “again in 1948 by the Golani Brigade” | Inherited attribution marked questionable; no demolition day or opening holder. |
| gdd03-0690 / mefalsim; gdd03-0692 / mefalsim_field | `content/sites.yaml`, Mefalsim founding row, 12 Jun 1949; [Palmach item 8465](https://www.palmach.org.il/en/history/database/?itemId=8465), Nir Am HQ context | The later/current footprint is not the historical site. Place-specific 15 May evidence and the original site's coordinates are unavailable; nearby headquarters are not proof. |
| gdd03-0714 / beit_kama; gdd03-0716 / beit_kama_field; gdd03-0718 / reim; gdd03-0720 / reim_field; gdd03-0722 / gilat; gdd03-0724 / gilat_field | [Dupuy, Elusive Victory](https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf), 1978 pp.44/55/56; [Palmach item 6201](https://palmach.org.il/history/database/?itemId=6201); control note §1 item4. Short quote, p.44: “west and northwest of Beersheba”; p.56 refers to 14 May deployment. | Regional sources do not place forces on any of these later founding sites or field footprints. Each needs dated place-specific evidence. |
| gdd03-0726 / umm_rashrash | [Hamichlol](https://www.hamichlol.org.il/%D7%90%D7%95%D7%9D_%D7%A8%D7%A9%D7%A8%D7%A9), police post / 10 Mar 1949: “תחנת המשטרה הנטושה” | Later context only. An unfetched UN cablegram of 13 Mar 1949 and FRUS 1949 vol.VI d529 search snippet reportedly mention occupation / an entrenched Legion unit; full evidence unavailable. The abandoned/entrenched conflict is in March 1949, not May 1948. |

The remaining missing joins contain no dated place-specific opening evidence. Their short inherited finding is “none found”; public supporting evidence is unavailable, rather than inferred from the map codes. Galilee: gdd03-0043 (`beit_jann`), gdd03-0089 (`kiryat_shmona_field`), gdd03-0093 (`hula_field`). Valleys: gdd03-0111 (`ein_harod`), gdd03-0112 (`nahalal`). Haifa-Sharon: gdd03-0168 (`zikhron_yaakov`), gdd03-0187 (`anabta`), gdd03-0191 (`deir_al_ghusun`), gdd03-0195 (`attil`), gdd03-0197 (`shwiaka_tulkarm`), gdd03-0201 (`baqa_al_gharbiyye`), gdd03-0203 (`bala`), gdd03-0207 (`daliyat_al_karmel`), gdd03-0211 (`isfiya`), gdd03-0213 (`zeita_tulkarm`), gdd03-0221 (`kafr_zibad`), gdd03-0223 (`qaffin`), gdd03-0237 (`ilar`), gdd03-0241 (`kafr_thulth`), gdd03-0251 (`azzoun`), gdd03-0257 (`jatt`), gdd03-0259 (`irtah`). The two field footprints are undefined; ethnicity, founding dates and neighbouring places cannot supply the missing control. The undated Isfiya volunteer reference cannot settle it either.

The original owner selection and control-policy public records are unavailable. The dated design labels above are supplied provenance, not a public transcript or a historical source. Davar issues, the Yassky memorandum, direct S/1025 camp-perimeter reading, primary military diaries and inaccessible books remain human research tasks. No transcript is imported and no new historical sign-off is claimed.
