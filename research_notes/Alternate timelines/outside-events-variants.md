# The outside events on the roads not taken: variant, suppress or replace (research note, AI draft, 2026-10-22)

Status: **AI draft, not checked by a person.** Research only: nothing here changes `content/`, and no variant text,
card or effect is drafted. Every row is a proposal for the owner. Refs #958 (Task 14 of the alternate-timeline plan,
`docs/superpowers/plans/2026-10-17-alternate-timelines.md`), #949, #939.

## Who did this and how

- **AI assistance.** An OpenAI Codex worker drafted a first version on 18 October 2026. On 22 October 2026 Claude
  (Anthropic) opened every cited source again, matched every quotation against the saved text with a script, read the
  scanned UN report S/1071 as page images, corrected what did not hold (listed at the end), and rewrote the note. A
  person has not checked it.
- **How the sources were read.** Each page was fetched with a plain GET and a generic browser User-Agent; no request
  carried a personal identifier. Every URL below was opened on **2026-10-22**. Hebrew excerpts are given in Hebrew;
  the English after them is Claude's translation, outside the quotation marks, and is not a quotation.
- **Conventions** follow [roads-checked.md](roads-checked.md): quotation marks around a source excerpt mean it is
  verbatim and under 15 words; quotation marks around an event file's text quote the repo. *Inference* marks Claude's
  reading, not a statement in a source.

## What Task 14 asks, and what this note adds

Task 14 treats a later card in one of three ways (plan §A.4.4 and Task 14):

- **Variant:** the card still comes, with text that fits the player's road; its options and effects stay.
- **Suppress:** the card's premise fails on the road, so it gains a `requires` entry on the historical option.
- **Replace:** a `kind: consequence` card takes its place, built only from what a party threatened or promised, or
  what officials recorded that they feared or expected (plan, Global Constraints, "Sources").

The premise table in [roads-checked.md](roads-checked.md) (G2, owner sign-off 19 October 2026, #949) already gives a
proposal for each card on `main` whose text a road contradicts. This note goes through the outside events (the
UN, the armistice talks, the powers, the Arab states) and adds: the sources for each proposal, rows the premise table
does not have (outside reactions that are not yet a card, the Jordan armistice, the January 1949 election), and the
places where the Codex draft and the premise table disagree.

Two limits apply to every row:

- **The Ferguson gate.** Under Q-ALT-3 (a) only a `proposed` or `debated` road gets consequence cards. Most roads
  below are `design` (roads-checked.md, summary table), so a "replace" there cannot become a card unless the owner
  rules otherwise. The basis column shows each road's G2 basis.
- **Structure is not a forecast.** A source that shows a historical event depended on something the player undid can
  support a suppression. It cannot supply replacement text; only a recorded threat, promise, fear or expectation can.

## The rows

| Card (file) | Road (G2 basis) | Premise table (G2) | This note | Evidence |
|---|---|---|---|---|
| none on `main`: the UN's answer to a refused truce | `first_truce/refuse` (design) | n/a | Replace candidate; fails the gate | Resolution 50, 29 May 1948: "with a view to action under Chapter VII" (https://avalon.law.yale.edu/20th_century/scres050.asp). FRUS 1948 V-2 d428, 1 July 1948: "Israel could be almost fatally injured by the application of economic sanctions" (https://history.state.gov/historicaldocuments/frus1948v05p2/d428). As roads-checked.md road 10 notes, d428 is dated during the truce and covers a breach or a resumed war, not a refusal on 1 June. |
| `jerusalem_military_government.yaml:4` | `first_truce/refuse` (design) | variant | Variant; its text is a design assumption | d428 (1 July 1948, during the first truce) records the Mediator's plan to fill Jerusalem with a "1,000 man United Nations armed guard". FRUS d494 (29 July 1948) has Israel's representative at the UN placing the demilitarisation talks under the Council's resolution of 15 July (https://history.state.gov/historicaldocuments/frus1948v05p2/d494). No source says what would become of the proposals if Israel had refused the first truce. |
| none on `main`: the UN's answer to a defied second truce | `second_truce/defy` (design) | n/a | Replace candidate; fails the gate | Resolution 54, 15 July 1948 (FRUS d468, https://history.state.gov/historicaldocuments/frus1948v05p2/d468): non-compliance shows "a breach of the peace" calling for "further action under Chapter VII". CIA estimate ORE 38-48, 27 July 1948 (FRUS d486, https://history.state.gov/historicaldocuments/frus1948v05p2/d486): if the truce collapses, the Mediator "will recommend sanctions against Arabs, or Jews, or both". d428 as above. |
| `operation_hiram.yaml:4` ("In breach of the truce, the Arab Liberation Army attacked") | `second_truce/defy` (design) | replace | Agree: no truce to breach. Suppress; the only sourced replacement is the UN row above, which fails the gate | The card's own source, the Acting Mediator's report on the truce in the Lebanese sector (S/1071, November 1948, https://documents.un.org/api/symbol/access?s=S/1071&l=en&t=pdf, read as page images), reports the October fighting under its title, "observance of the truce in the Lebanese sector". No source found forecasts a northern operation on this road. Whether a separate northern-war card is wanted is the owner's call. |
| `operation_yoav.yaml:4` ("the offensive the cabinet approved on 6 October is under way") | `second_truce/defy` (design); `latrun_proposal/press_offensive` (debated) | variant (both) | Agree: variant; its text is a design assumption | Both roads end the lull the card assumes. The card's source S/1042 is the Acting Mediator's report of 18 October 1948 on the Negev fighting (https://documents.un.org/api/symbol/access?s=S/1042&l=en&t=pdf), not a forecast. Resolution 54 (above) records the UN's stated response to fighting, not whether or when Yoav would happen. |
| `operation_hiram.yaml:4` | `latrun_proposal/press_offensive` (debated) | variant | Agree: variant (Israel would already have broken the truce) | S/1071 and Resolution 54, as above. No source forecasts the Arab Liberation Army attack after an Israeli September offensive; keeping the card is a design assumption. |
| `armistice_jordan.yaml:4` ("Negotiators have reached an armistice agreement with Jordan.") | `latrun_proposal/press_offensive` (debated); `operation_yoav/push_to_hebron` (design) | no row | Not in the premise table. No change is supported; keeping the card as is, or a variant, is a design assumption | IDF History Department study of Mount Hebron, p. 24, quoting the Prime Minister's war diary: "מלחמה על חברון היא מלחמה על הלגיון, והוא ירחיב את המלחמה" (*a war on Hebron is a war on the Legion, and it will widen the war*) (https://www.idf.il/media/rccdyzpl/%D7%94%D7%A8-%D7%97%D7%91%D7%A8%D7%95%D7%9F-%D7%91%D7%9E%D7%9C%D7%97%D7%9E%D7%AA-%D7%94%D7%A2%D7%A6%D7%9E%D7%90%D7%95%D7%AA-%D7%90%D7%97%D7%A8%D7%99-%D7%A2%D7%99%D7%9E%D7%95%D7%93-1.pdf; already in roads-checked.md road 25). A wider war with the Legion was feared; no source says what that would do to the 1949 talks (agreement text: https://avalon.law.yale.edu/20th_century/arm03.asp). |
| `armistice_lebanon.yaml:4` ("Negotiators have reached an armistice agreement with Lebanon.") | `operation_hiram/advance_into_lebanon` (design) | no conflict | Agree: no conflict; do not suppress | FRUS 1949 VI d372, 10 January 1949, Israel's Special Representative in Washington: if Lebanon "were sincerely prepared to sit down to serious armistice and peace negotiations", "Israel would with draw [sic] its forces from the Lebanon" (https://history.state.gov/historicaldocuments/frus1949v06/d372). This concerns the historical incursion. *Inference:* the same offer could stand after a deeper advance, so later talks stay possible. Agreement text: https://avalon.law.yale.edu/20th_century/arm02.asp. |
| `rhodes_talks.yaml:4` | `operation_horev/hold` (design) | no conflict (already requires `operation_horev/launch`) | Agree | Checked on `main`: `rhodes_talks.yaml` has `requires: [{ event: operation_horev, option: launch }]`. FRUS d362 records both governments' "unconditional acceptance of a proposal providing for a cease-fire" before the talks (https://history.state.gov/historicaldocuments/frus1949v06/d362). No source says what anyone expected if Israel had not launched Horev. |
| `armistice_egypt.yaml:4` ("Talks in Rhodes have produced an armistice agreement with Egypt.") | `operation_horev/hold` (design) | variant | Agree: variant; its text is a design assumption | FRUS d483 (editorial note): Israel and Egypt signed at Rhodes on 24 February 1949 (https://history.state.gov/historicaldocuments/frus1949v06/d483); agreement text https://avalon.law.yale.edu/20th_century/arm01.asp. No source forecasts an Egyptian armistice, its venue or its date without Horev. |
| `rhodes_talks.yaml:4` ("The talks are to open at Rhodes.") | `sinai_withdrawal/hold_el_arish` (proposed; slice 1) | variant | **Disagrees:** the Codex draft proposes suppress and replace. For the owner | FRUS d373, 10 January 1949, the Egyptian Ambassador in Washington: Egypt's delegates "would not leave Egypt until all Israeli forces had left" Egyptian territory (https://history.state.gov/historicaldocuments/frus1949v06/d373). This is new; roads-checked.md road 29 has the Michelson study's condition but not Egypt's own words. The expected consequences are already in road 29: the British note under the 1936 treaty (d341, 1 January 1949), the US warning (d354, 5 January 1949), and the British order on Israeli aircraft (Hansard, https://api.parliament.uk/historic-hansard/commons/1949/may/11/israel-attacked-british-aircraft). A replacement card on this road passes the gate. |
| `armistice_egypt.yaml:4` | `sinai_withdrawal/hold_el_arish` (proposed; slice 1) | variant | **Disagrees:** the Codex draft proposes suppress until a `rejoin` (pulling the force back) | d373 as above; FRUS d425 (25 January 1949): the cease-fire agreement was "formally approved by the Delegations of Egypt and Israel at Rhodes" (https://history.state.gov/historicaldocuments/frus1949v06/d425); d483 as above. *Inference:* if Egypt's delegates stay home until the force leaves, the Rhodes agreement waits on the withdrawal. When a rejoin would bring the card back, and with what window, is the owner's call (Task 6, `rejoin`). |
| `armistice_egypt.yaml:4` | `rhodes_talks/decline` (design) | variant | **Disagrees:** the Codex draft proposes suppress, with no replacement found | d425 and d483 as above: the agreement was negotiated and signed by the delegations at Rhodes. No source records what anyone expected if Israel sent no delegation, so any substitute (another venue, an imposed settlement, renewed fighting) would be invented. |
| `armistice_jordan.yaml`, `armistice_lebanon.yaml`, `armistice_syria.yaml` | `rhodes_talks/decline` (design) | no row | No change is supported | FRUS d440, 1 February 1949: Israel accepted Bunche's invitation to talks with Transjordan, Syria, Lebanon and Iraq, "provided: (a) that Egyptian negotiations be concluded first" (https://history.state.gov/historicaldocuments/frus1949v06/d440). *Inference:* this ties the order of the later talks to the Egyptian ones, but no source says what follows if the Egyptian talks never start. |
| `hula_drainage.yaml:4` ("reach into the demilitarised zone with Syria ... the chairman of the armistice commission") | `armistice_syria/hold_out` (design) | suppress | Agree: suppress; no replacement found | The signed Israel-Syria agreement, 20 July 1949, art. V (a): the area "shall be established as a Demilitarized Zone" (https://avalon.law.yale.edu/20th_century/arm04.asp). Bunche's draft article of 8 June 1949 says the same (FRUS 1949 VI d735, https://history.state.gov/historicaldocuments/frus1949v06/d735). FRUS 1951 V d288 (5 March 1951) shows the 1951 dispute running through that zone (https://history.state.gov/historicaldocuments/frus1951v05/d288). No source gives a regime for the works without an armistice. |
| none: later outside events after refusing a signed armistice | `armistice_egypt`, `armistice_jordan`, `armistice_lebanon` `/hold_out` (design) | no conflict | Agree | Resolution 62 (https://avalon.law.yale.edu/20th_century/scres062.asp) and the agreement texts say what signing created, not what refusing would bring (as roads-checked.md roads 2 to 4 found). |
| `elections_1949.yaml:4` ("Postponed because of the war, the vote is set for 25 January.") | any road that keeps heavy fighting going into January 1949 | no row | Not in the premise table. No change is supported; delaying the vote on any road would be a design assumption | The Knesset's newsletter on its history: the "War of Independence necessitated a delay" in the constitution timetable (https://main.knesset.gov.il/EN/activity/ForeignAffairs/Documents/KnessetIntAffNewsletter2.pdf). Harpaz, *Kesher* 61, PDF p. 3: the government's date of 25 November 1948 was put off "בשל התמשכות הקרבות" (*because of the prolonged fighting*), mainly in the south (https://dacenter.tau.ac.il/sites/abraham.tau.ac.il/files/media_server/daniel%20abraham/kesher/61/08_Kesher61_Harpaz.pdf). No source gives a rule for when fighting would postpone the vote again, or a new date. |

`jerusalem_military_government/accept_demilitarisation` (debated; slice 1) changes no outside event. Its premise-table
rows concern `bernadotte_assassination.yaml` and `jerusalem_ceasefire.yaml`, which are outside this note.

## Questions for the owner (from the rows above; not decided here)

1. `sinai_withdrawal/hold_el_arish`: should `rhodes_talks` and `armistice_egypt` get a variant (the premise table)
   or be suppressed until the force pulls back (the Codex draft)? If suppressed, the British and US pressure in
   roads-checked.md road 29 is the sourced material for a replacement, and the road passes the gate.
2. `rhodes_talks/decline`: variant (the premise table) or suppress `armistice_egypt` with no replacement?
3. `second_truce/defy`: suppress `operation_hiram` with nothing in its place, since the UN-response replacement fails
   the gate?
4. `armistice_syria/hold_out`: suppress `hula_drainage` with nothing in its place?
5. `latrun_proposal/press_offensive` and `operation_yoav/push_to_hebron`: leave `armistice_jordan` as it is?
6. `elections_1949`: leave it on every road?

## What the 22 October check changed in the Codex draft

- **Lebanon (FRUS d372).** The draft's quotation "Israel would withdraw its forces from the Lebanon" is not verbatim:
  the document reads "with draw". Quoted as printed, with [sic], and the condition attached to it added.
- **S/1042.** The draft called it "Israel's reply concerning the October fighting". It is the Acting Mediator's report
  of 18 October 1948 on the fighting in the Negev.
- **Jerusalem row.** The draft cited A/648 through FRUS d601. That document is an extract of the Mediator's
  conclusions of September 1948 and does not cover the July demilitarisation proposals. Replaced with d428 and d494.
- **Hula row.** The draft quoted FRUS d735 for the demilitarised zone. That is Bunche's draft article of 8 June 1949,
  not the signed agreement; the quotation now comes from the agreement of 20 July 1949, with d735 kept as the draft.
- **Election row.** The draft said the war had "already forced two postponements". The sources show the
  constitution timetable slipping past 1 October 1948 and the 25 November 1948 election date being put off; "two
  postponements" was the draft's count, so the row now says what each source says.
- **Hebron quotation.** The draft gave an English line, "will widen the war wherever it is", as a quotation. The
  source is Hebrew; the Hebrew is now quoted, with the translation outside the quotation marks.
- **The gate and the premise table.** The draft did not say which roads pass the Ferguson gate, or where it disagreed
  with the G2 premise table. Both are now in the table.
- **Dropped:** the draft's self-scoring section. Its "replacement supported" on the `hold_el_arish` rows now says
  where the consequences come from (road 29 of the G2 note).

## Not read, and gaps

- S/1071 was read from page images, pages 2 and 3 only. Its exact day in November 1948 was not legible enough to give.
- `[GAP]` No source found for what any party expected after: a refused first truce (for the Jerusalem proposals), a
  declined Rhodes delegation, a Syrian hold-out (for the Hula works), or continued fighting into January 1949 (for the
  election).
