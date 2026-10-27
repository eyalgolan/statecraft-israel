# Local Palestinian Arab forces: row proposals and exact gaps

**Status: draft research proposal; AI-read. Refs #1243.** A person must check the original
sources, every claim and every proposed row before any factual or sensitivity approval.
This note is not importable content. It adds no unit, type, strength, position or game rule.

## Scope and source baseline

The [ready-now proposal in #1243](https://github.com/eyalgolan/statecraft-israel/issues/1243)
allows a draft row proposal or an exact gap for each force in the earlier research. Code and
content wait for the separate owner gates on sensitivity, dated entry and art. The GDD's
[Population and units](../../docs/GDD.md#population-and-units) includes Palestinian militia;
it leaves equipment, movement, strength and behavior open. This note sets none of those values.

Read against main `fa35e557d9f45119a075a26e93092a7e2bdb59a1`, 21 October 2026:

- [Arab-forces research](arab-forces-1948.md), Conventions and sections 7 and 10.
  Its `[V]` and `[S, carried]` labels describe that note's earlier research, including snippets.
  They are not a new opening of those sources or a person's factual approval here.
- [Unit row schema](../../content/map/units_1948.yaml) and [Map units](../../docs/units.md).
  The side `local_arab_forces` is recognized but has no row. Strength is one sourced number
  with `as_of` and sources, never a range. R-UNIT-006 currently uses a strength dated
  15 May 1948; the issue leaves later entry for the owner. No 15 May strength is established
  by this proposal. A formation's own name, echelon and dated position must also be supported.
- [Content policy](../../docs/CONTENT_POLICY.md), especially sources, AI drafts and human sign-off.
  Israeli official sources and mainstream Israeli historiography come first. The older
  multi-perspective research remains available for a person to compare; this note does not
  turn its conflicting figures into a selected content value.

Four research groups below cover the four entries in the earlier note's section 7 table.
They are not a decision to create four simulation units. In particular, a command headquarters
is not automatically a separate combat formation, and a combined estimate is not two town strengths.

## Source access on 21 October 2026

| Key | Source and relevant passage | What was actually read for this proposal |
|---|---|---|
| S1 | [Ben-Gurion Archive, diary, 31 May 1948](https://bengurionarchive.bgu.ac.il/en/node/5151), English translation, paragraph beginning “In the north they bombed villages” | Opened and read by the AI on 21 October 2026. The translated entry attributes an estimate of 2,000–3,000 fighters in Ramle and Lod to James / Michael Ben-Gal. This is a combined estimate for 31 May, not one value for either town on 15 May. No person has checked it for this proposal. |
| S2 | [Munayyer, with Khalidi's introduction, *The Fall of Lydda*](https://www.palquest.org/sites/default/files/The_Fall_of_Lydda.pdf), pp. 81, 88 and 97 note 18 as cited in the earlier research | The linked PDF returned HTTP 403 on 21 October 2026. Its text was not read here. The cited page claims below are carried as verification tasks, not new checked facts. |
| S3 | [David Tal, *War in Palestine, 1948*](https://books.google.com/books?id=Vs2PAgAAQBAJ), pp. 213, 226–227, 233 and 238 as cited in the earlier research | The earlier note labels these passages `[S, carried]` and says the book was not re-read there. No original passage was opened for this proposal. These pages remain a gap, not a checked citation. |
| S4 | [Abdullah al-Tall, *Karithat Filastin*](https://archive.org/details/elshandawily9575), pp. 161–162 and 359 as cited in the earlier research | The archive item was not accessible through the reading tool on 21 October 2026. No OCR passage was read here. The earlier note also says the demolition-unit passage's page was not read. |
| S5 | [O'Ballance, *The Arab-Israeli War 1948*](https://archive.org/details/in.ernet.dli.2015.51540), p. 137 as cited in the earlier research | Not opened again for this proposal. The earlier note's reading is not promoted to a current check or human approval. A person still needs the dated formation passage and the preferred Israeli-source comparison. |

Only S1 has a successful current source reading in this proposal. Failed or unattempted
readings above have no new factual `checked` claim. There are no quoted historical speeches.

## 1. Lydda–Ramla command of the Army of the Holy War

- **Place/anchor gap:** Section 7 of the earlier note associates this command with the Ramla
  area and a Ras al-Ayn incident. It cites S2 p. 97 note 18 for an end on 2 June, and S3
  pp. 226–227 and 238 for the late-May command relationship. These passages were not read
  here. Verify the formation's identity, whether the command represents a combat unit at all,
  its actual dated position and the start of any anchor window. An end date alone supplies no start.
- **Strength gap:** No strength for this command dated 15 May is supplied by the earlier note.
  S1's combined Lod/Ramle estimate must not be assigned to this command as its own strength.
  `strength.value`, `strength.as_of` and a usable strength source are therefore not proposed.
- **Source status:** S2 blocked; S3 carried and unverified. S1 is a dated combined estimate only.
  Needed before a row: a directly read identity/location passage, a dated window and the single
  sourced strength required by the loader; compare an Israeli official or mainstream source.
- **Sensitivity:** Lydda and Ramla are explicitly named in the earlier note's Conventions
  sensitivity list. Preserve the owner sensitivity gate for local forces. This proposal describes
  no sensitive incident and makes no judgment about residents.

## 2. Lydda and Ramla garrisons

- **Place/anchor gap:** The earlier note's table cites S2 p. 81 for garrisons at Lydda and Ramla
  from 15 May to July. That passage was not read here. Verify the named formations and the
  relationship between volunteers and the small Legion contingent before deciding row identity
  or side; do not duplicate a Legion unit as a local force. “July” supplies no exact end day.
  No coordinates are proposed: the note's town points identify places, not unit positions.
- **Strength gap:** S1 supplies a combined 2,000–3,000 estimate on 31 May. Keep its range and
  date; neither select an endpoint or midpoint nor divide it between Lod and Ramla. S2 p. 88's
  sorties, reported by the earlier note for 5 May, are outside the game start and are not a
  dated 15 May garrison strength. No individual row strength can be filled from these inputs.
- **Source status:** S1 was AI-read on 21 October 2026; S2 remains blocked. The source table
  separates the accessible later estimate from the still-unread anchor and formation evidence.
  A later-entry proposal requires the owner's dated-entry ruling and an exact, suitable strength.
- **Sensitivity:** Both towns are explicitly in the Conventions sensitivity list. Combatants and
  residents remain distinct. No resident count, civilian token or settlement-control change follows
  from identifying a garrison.

## 3. Jerusalem's Old City and Abu Tor detachments

- **Place/anchor gap:** Section 7 attributes an Old City force, an Abu Tor detachment and a
  demolition unit to the Army of the Holy War. Its dated claims rely on S4 pp. 161–162 and 359,
  with S3 p. 213 carried for local commanders. The earlier note's “from about 17 May” is not
  an exact start day. Its first-truce headquarters is not proof of a combat formation at one
  coordinate. The demolition-unit passage has no read page. Verify each identity and dated
  location separately; do not merge headquarters, detachment and demolition unit by assumption.
- **Strength gap:** Neither the earlier note nor an original passage read here supplies a
  15 May strength for these formations. An early-May mixed ALA/Jordanian/Palestinian arrival
  mentioned elsewhere in section 7 is not an independently dated strength for this proposed row.
- **Source status:** S4 inaccessible in this attempt; S3 still carried. Original text, page/date
  resolution and an Israeli-source comparison remain open. No anchor window or usable row is supplied.
- **Sensitivity:** The Conventions explicitly name the Jewish Quarter, not every place in
  Jerusalem. Old City and Abu Tor are not listed verbatim there; this is not an exemption from
  the owner's local-forces sensitivity review. Keep those place distinctions visible to that reviewer.

## 4. Palestinian battalions associated with the Iraqi force

- **Place/anchor gap:** The earlier table places these formations in the western/Tulkarm sector
  and describes their raising mostly during the truce, citing S5 p. 137 and S3 p. 233.
  Neither original passage was read here. Resolve the battalions' own names, side and echelon,
  their relationship to Iraqi formations, a dated start and a specific sourced position. A sector
  name is not a formation coordinate; “mostly during the truce” is not an exact date.
- **Strength gap:** No dated 15 May number for these battalions is supplied. National irregular
  totals in section 7, or later aggregates, must not be assigned to one Tulkarm formation.
  There is no proposed `strength.value`, `strength.as_of` or start count.
- **Source status:** S5 not reopened; S3 carried. These are exact research gaps rather than a
  content-ready row. Original formation/date evidence and the preferred Israeli-source check are needed.
- **Sensitivity:** Tulkarm is not named in the earlier note's Conventions sensitivity list.
  The owner review required for all these local-forces rows still applies; this note supplies no approval.

## Row-shaped handoff

This table maps the proposal to the existing fields without inserting placeholders into the content pack.
An omitted required value means the row is not ready; it does not mean zero, a default or an empty formation.

| Existing field | Proposal boundary |
|---|---|
| `id`, `name`, `number`, `echelon` | No new ids or accepted formation names selected. Resolve original formation identity and grouping above first. |
| `service`, `side` | The intended research category is army / `local_arab_forces`, the side already recognized by the schema. Confirm each formation belongs to it rather than the Legion, ALA or Iraqi row before import. |
| `anchors[].place`, `from`, `to`, `since` | No executable anchor proposed. Preserve the stated uncertainty, source date and undated ends; resolve each group's gaps rather than converting “about”, “late May” or “July” into a day. |
| `anchors[].lon`, `lat`, `place_hex`, `toward` | Omitted. Locality coordinates in the old research are approximate reference points, not validated unit deployment; do not infer a neighbouring hex or offset. |
| `strength.value`, `as_of`, `sources` | Omitted for every proposed row. The one newly read combined estimate is a range for 31 May. It supplies neither a single value nor the 15 May date currently required. |
| `sources` | Source-access ledger above plus the original passages still to read. A URL or old `[V]` label alone is not a fresh verification. |
| `status`, Hebrew name and its review flag | This research note remains draft. No content status or new Hebrew display name is introduced. |

## What the owner and later implementation still decide

The proposal has no prerequisite, as #1243 states. Importing any row does: the owner reviews
sensitivity, decides dated entry where no 15 May strength exists, and selects appropriate art
(the issue points to C09 as a candidate, not an approval). The local-forces unit type and all its
balance values remain outside this document. No token, combatant, resident state or objective is added.
The later code/content PR has its own red-first tests, golden update, release gate and visual checks;
none of those gates is claimed by this research-only handoff.

## Human review checklist

- [ ] Open S1 and check the translated passage against the diary; confirm the estimate's attribution,
  combined scope and 31 May date. Do not relabel it 15 May or pick a number from its range.
- [ ] Read S2's original pp. 81, 88 and 97 note 18; record which formation, place and date each supports.
- [ ] Read the original S3/S4/S5 passages and compare Israeli official or mainstream historiography.
  Record failed access and conflicting dates instead of filling a gap from another formation.
- [ ] Decide which named formations deserve separate rows; avoid counting command, garrison,
  attached Legion troops and other battalions twice.
- [ ] Resolve exact anchor windows and unit-position sources; do not treat reference town coordinates
  as a survey of troop deployments.
- [ ] Obtain a suitable single dated strength, or keep the row absent. Request the owner's dated-entry
  decision before any later formation enters the simulation.
- [ ] Obtain the owner's named sensitivity review; preserve the distinction between combatants and
  residents. Check Hebrew names separately if any are later proposed.
- [ ] Leave the draft and all content review fields unchanged until the person actually reviews and
  records their own decision. This agent has made no human attestation.
