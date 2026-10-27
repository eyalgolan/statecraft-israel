# Slice 2 roads: new evidence beyond the G2 note (research note, AI draft, 2026-10-22)

Status: **AI draft, not checked by a person.** Research only: nothing here changes `content/`, no card is drafted,
and no basis in [roads-checked.md](roads-checked.md) changes. Refs #957 (Task 13 of the alternate-timeline plan,
`docs/superpowers/plans/2026-10-17-alternate-timelines.md`), #949, #939.

## Who did this and how

- **AI assistance.** An OpenAI Codex worker drafted a first version on 18 October 2026. On 22 October 2026 Claude
  (Anthropic) opened every cited source again, matched every quotation against the saved text with a script, read the
  scanned Knesset pages as images, and rewrote the note. A person has not checked it.
- **How the sources were read.** Each page was fetched with a plain GET and a generic browser User-Agent; no request
  carried a personal identifier. Every URL below was opened on **2026-10-22** unless the entry says otherwise.
  Hebrew excerpts are given in Hebrew. The English after them is Claude's translation, outside the quotation marks,
  and is not a quotation.
- **Conventions** follow [roads-checked.md](roads-checked.md): quotation marks around a source excerpt mean it is
  verbatim and under 15 words; `proposed`, `debated` and `design` mean what that note's "Conventions" section says.
  Anything marked *inference* is Claude's reading, not a statement in the source.

## Scope

[roads-checked.md](roads-checked.md) is the G2 record for every road, signed off by the owner on 19 October 2026
(#949). This note does not replace it. It records only what is new since then for the roads Task 13 names:

| Road | Slice | Basis in roads-checked.md | What this note adds |
|---|---|---|---|
| `clothing_rationing_1950/relax` (road 8) | 2 | `debated` (owner ruling, 18 Oct, #949: "the basis stays `debated`") | The 8 August 1950 Knesset motions and votes, which the G2 pass could not open |
| `austerity_1949/decline` (road 6) | 2 | `design` | No change; the 26 April 1949 minutes now open |
| `reparations_claim/refuse` (road 26) | 2, sensitive (`holocaust`); #938 is closed | `design` | The Foreign Affairs and Defense Committee record of 13 March 1951; no change |
| `jerusalem_military_government/accept_demilitarisation` (road 18) | 1 (#939) | `debated` | One more FRUS document; listed here only for the slice-1 drafts |

Under Q-ALT-3 (a) only `proposed` and `debated` roads get a follow-up card. On the G2 record that leaves
`clothing_rationing_1950/relax` as the one slice-2 road that passes.

## `clothing_rationing_1950/relax` (road 8)

- Event: `content/events/clothing_rationing_1950.yaml`, window 1950-07-01 to 1950-09-01; historical option `extend`.
  The option reads "Ease rationing and let imports fill the shops".
- **New: the Knesset motions of 8 August 1950.** The sitting that closed the rationing debate (Divrei HaKnesset,
  176th sitting of the First Knesset, 8 August 1950, pp. 2503-2505,
  https://fs.knesset.gov.il/1/Plenum/1_ptm_250242.pdf). roads-checked.md lists this PDF as unreachable; it opened as
  a real PDF on 2026-10-22, and pp. 2503-2505 were read from page images.
  - Herut's motion, p. 2504: "מציעה לדחות את הודעתו של שר־האספקה והקיצוב ואת הצו האחרון לקיצוב בהלבשה והנעלה"
    (Claude's translation: *proposes to reject the statement of the Minister of Supply and Rationing and the latest
    order on clothing and footwear rationing*).
  - The General Zionists' motion (a), p. 2504: a parliamentary committee to draw up a fair distribution scheme, after
    which "ייקבע צו קיצוב מתאים" (*a suitable rationing order will be set*), "אם יהיה צורך בו" (*if one is needed*).
  - The Sephardim list's motion, p. 2504: "שכל שיטות הפיקוח והקיצוב יבוטלו בהדרגה עם הטבת המצב" (*that all
    methods of control and rationing be abolished gradually as the situation improves*).
  - Mapam's motion, p. 2503, asks for amendments to the order and for "ביטול גמור של יבוא מצרכים בלתי־חיוניים"
    (*the complete abolition of imports of non-essential goods*): the opposite of "let imports fill the shops".
  - The votes, p. 2505: Herut 11 for, 66 against; General Zionists 8 for, 56 against; Mapam 17 for, 63 against;
    Maki 2 for, 72 against; the Sephardim list 2 for, 56 against; the coalition's motion 57 for, 36 against,
    carried. The Prime Minister made it a question of confidence: "הממשלה עושה מזה שאלת אמון" (*the government makes
    this a question of confidence*). The 57-36 count matches the JTA report already cited in roads-checked.md.
- **New: JTA, 8 August 1950** (https://www.jta.org/archive/israel-cabinet-acts-on-rationing-protests-appoints-non-party-committee).
  The cabinet named a non-party committee to "study and report on effects of clothing and footwear rationing". At a
  protest meeting called by the Merchants Association, speakers for the Religious Bloc said its members in parliament
  "will not vote for the rationing law in its present form". This was said at a rally, not in the Knesset.
- **New: JTA, 4 August 1950** (https://www.jta.org/archive/israel-merchants-threaten-to-keep-stores-closed-for-month-protest-new-rationing).
  After meeting the Prime Minister, the Tel Aviv merchants said they would keep their shops shut another month "to
  force reconsideration of the edict". A trade body, as roads-checked.md already notes for its 2 August report.
- **What this means for the basis (for the owner; not decided here).** Herut, a Knesset faction, moved on the floor to
  reject the clothing order, and the motion was voted down. That is a named body on record proposing the core of
  `relax` on the day of the vote. Whether it lifts road 8 from `debated` to `proposed` is the owner's call: the owner
  ruled on 18 October that the basis stays `debated`, before these minutes were read.
- **Still a gap.** No motion or speech found on 8 August asks for imports to fill the shops; Mapam's motion asks the
  reverse. The G2 note's caveat stands: only rejecting or easing the order is on record, not the imports clause.
  The 7 August sitting (https://fs.knesset.gov.il/1/Plenum/1_ptm_250241.pdf, 175th sitting) also opened but was not
  read in this pass.
- **Expected consequences.** None new that qualify. The ministers' case in the JTA report of 8 August ("There is only
  one alternative," the Minister of Labour said, between absorbing immigrants and living "in luxury") argues for the
  historical option; it is not an official's expectation about relaxing the order. *Inference:* the minister's
  speech on pp. 2503-2504 may hold one, but this pass read only the motions and votes.

## `austerity_1949/decline` (road 6)

- No change to `design`. No Israeli body was found proposing or debating declining the April 1949 programme.
- The 26 April 1949 sitting (25th sitting of the First Knesset, https://fs.knesset.gov.il/1/Plenum/1_ptm_250102.pdf),
  unreachable in the G2 pass, opened on 2026-10-22. A text search of it found the Minister of Supply and Rationing
  presenting the plan and procedural requests from the General Zionists and Herut about how the debate would run; no
  motion to decline was found. A full read is still open.
- JTA, 13 April 1950 (https://www.jta.org/archive/austerity-in-israel-should-not-be-regarded-as-permanent-minister-joseph-says-in-n-y):
  the minister in charge said in New York the programme "must not be regarded as a permanent state of affairs". This
  is a year after the event's window and speaks to easing it later, not to declining it.
- Dropped: the draft's use of the National Library of Israel blog post on ration queues (https://blog.nli.org.il/en/hoi_rations/)
  to date a General Zionist anti-rationing poster to 1951. The page returned HTTP 403 on 2026-10-22, so the claim
  could not be checked.

## `reparations_claim/refuse` (road 26)

- No change to `design`. Nobody was found proposing to refuse the claim in March 1951. The opposition argued for a
  different and larger claim.
- Yaakov Sharett (ed.), *The Reparations Controversy* (De Gruyter, 2011), English translation of the Foreign Affairs
  and Defense Committee, 13 March 1951 (https://tile.loc.gov/storage-services/master/gdc/gdcebookspublic/20/20/71/94/06/2020719406/2020719406.pdf):
  - The Foreign Minister, p. 50: "We submitted the second note yesterday", made as "a definitive claim".
  - Herut's member, p. 54: "The demand can only be one: to restore the material property"; pp. 54-55, Israel
    "should demand $6 billion".
- FRUS 1951 V, document 296 (https://history.state.gov/historicaldocuments/frus1951v05/d296), 19 March 1951, footnote
  on Israel's note of 12 March: Israel asked US support "for its claim of $1,500,000,000 in reparations".
- The Knesset sitting of 13 March 1951 (https://fs.knesset.gov.il/1/Plenum/1_ptm_250159.pdf) opened on 2026-10-22
  but was not read.

## For the slice-1 drafts: `jerusalem_military_government/accept_demilitarisation` (road 18)

Not a slice-2 road (Q-ALT-13 (a), #939). One document found here is not in roads-checked.md:

- FRUS 1948 V-2, document 494 (https://history.state.gov/historicaldocuments/frus1948v05p2/d494), New York, 29 July
  1948: two letters from Israel's representative at the UN. Letter of 26 July: "In accepting the principle of a
  demilitarized Jerusalem", the government objected to the access terms. Letter of 29 July: the government "reaffirms
  its readiness to approve the principle of demilitarization"; the enclosed cable from the Foreign Minister rejects
  the Mediator's particular plan. This fits the G2 basis (`debated`): the principle was accepted and the scheme
  refused.

## Not read, and gaps

- `[GAP]` The imports clause of `clothing_rationing_1950/relax` has no source (see road 8).
- Opened on 2026-10-22 but not read in this pass: `1_ptm_250241` (7 August 1950), `1_ptm_250159` (13 March 1951),
  and `0_ptm_249980` (Provisional State Council, cited for road 18 in roads-checked.md).
- Not reachable: https://blog.nli.org.il/en/hoi_rations/ (HTTP 403).
