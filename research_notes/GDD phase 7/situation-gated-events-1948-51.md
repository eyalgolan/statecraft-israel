# Situation-gated candidate events 1948-51: low social cohesion, low international support (research note, 2026-10-16)

Status: research only. Nothing here changes `content/`. Refs #807 (GDD Phase 7 PR 3, the research half) and #387. The
cards themselves wait on the owner's answers in #784 (Q-EVT-5 decides the size of the set); this note only lists
sourced candidates and recommends.

## Scope

The [Phase 7 plan](../../docs/superpowers/plans/2026-10-15-gdd-phase-7.md) gives PR 3 four parts: the #135 cards, the
first unit grant, "situation-gated events" and the gap rule. The #135 cards and the grant are sourced already
([candidate-events-1948-51.md](candidate-events-1948-51.md), #472; [gdd-phase-7-sourcing.md](../gdd-phase-7-sourcing.md)
for Q-NEW-1, Q-NEW-2 and Q-HFX-4). This note covers the other two:

1. **Situation-gated candidates.** Q-NAT-3 (decided, Phase 2 plan) made "Low social cohesion" (`morale`) and "Low
   international support" (`intl_standing`) situations, and the Phase 7 plan says "PR 3 may add events gated on them,
   and nothing else". This note lists dated moments from 15 May 1948 to 15 January 1952 where Israel faced a cohesion
   strain or a drop in international support **and** a recorded Israeli response exists, checked against the events on
   `main` (32 files in `content/events/`; the plan counts 31).
2. **The 15 May to 15 November 1951 stretch.** `EventRetimingTests.TheLimitsAreSixMonthsAndTwoAtWar` pins the
   longest peacetime stretch without a card at exactly 6 months, "15 May to 15 November 1951" (`israel_bonds` to
   `forex_crisis_1951`), so the gap rule (R-TURN-003) holds today with no margin. Section 3 lists the sourced
   candidates inside that stretch.

One point for whoever writes the cards: the gap test runs the historical run. A card gated on a situation only counts
for R-TURN-003 if the historical run is in that situation on that turn. This note did not check whether it is.

## Conventions

- Every URL below was opened on **2026-10-16** (the check date for all of them). Two research helpers read the pages
  first; every quotation tagged [V] below was then re-fetched and matched verbatim in the page text by the note's
  author, the same day. JTA bulletin dates are the pages' own `datePublished`. The Suez complaint sources added for issue #1426 (section 1d) were read on **2026-10-23** by an AI agent; a person's check is pending. The seamen's strike sources added for issue #1425 (section 2a) were read on **2026-10-23** by an AI agent; a person's check is pending.
- Tags, as in the #135 note:
  - **[V] verified**: read in a primary or official record (FRUS, UN resolution text) or in the period press (JTA
    Daily News Bulletin), quoted verbatim.
  - **[S] single source**: read in one secondary source only (named).
  - **lead**: seen only as a search-result summary, or the page was not opened. Not a fact.
  - **not reached**: the source exists but could not be opened from this machine.
- Quotation marks are verbatim from the source named, for the reader to check. None is meant for event text. Real
  people are named in citations; event text would use office placeholders (`docs/WRITING_EVENTS.md`).
- Turns are monthly, from the 15th. "The 15 May 1949 turn" means the turn that starts on that day.
  A card itself reaches the player on the first 15th on or after its `from` (section 3): a card whose window opens on
  28 May 1949 reaches the player on 15 June 1949.
- Requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`.
  No Tavily key on this machine: searches used the built-in web search; Wikipedia only as a pointer.

## Sources used

Primary and official:

| Short name | What it is | URL |
|---|---|---|
| FRUS 1949 VI d705 | Acting Secretary of State to the Embassy in Israel, 28 May 1949: the President's note to the Prime Minister | https://history.state.gov/historicaldocuments/frus1949v06/d705 |
| FRUS 1949 VI d706 | McDonald (Tel Aviv), 29 May 1949: the Prime Minister's first response | https://history.state.gov/historicaldocuments/frus1949v06/d706 |
| FRUS 1949 VI d736 | "The Government of Israel to the Government of the United States", 8 June 1949 | https://history.state.gov/historicaldocuments/frus1949v06/d736 |
| FRUS 1949 VI d884 | Porter (Lausanne), 3 August 1949: the PCC meets the Israeli delegation | https://history.state.gov/historicaldocuments/frus1949v06/d884 |
| FRUS 1950 V d309 | Secretary of State to the Embassy in Israel, 4 January 1950, on Jerusalem | https://history.state.gov/historicaldocuments/frus1950v05/d309 |
| UNGA 303 (IV) | General Assembly resolution 303 (IV), 9 December 1949, "Palestine: question of an international regime for the Jerusalem area" (UN PDF, text layer) | https://documents.un.org/api/symbol/access?s=A/RES/303(IV)&l=en&t=pdf |
| UNSC 93 (1951) | Security Council resolution 93, 18 May 1951 (UN compilation PDF, text layer) | https://documents.un.org/api/symbol/access?s=S/RES/93(1951)&l=en&t=pdf |
| UNSC 95 (1951) | Security Council resolution 95, 1 September 1951 (UN compilation PDF, text layer; its page images re-read 2026-10-23 for #1426) | https://documents.un.org/api/symbol/access?s=S/RES/95(1951)&l=en&t=pdf |
| S/2241 | Israel's complaint: letter dated 11 July 1951 from the permanent representative of Israel to the President of the Security Council, as reprinted in the Council's Official Records (printed pp. 9-10); read as page images 2026-10-23 (#1426) | https://digitallibrary.un.org/record/480045 (scan: https://digitallibrary.un.org/record/480045/files/S_2241-EN.pdf) |
| S/PV.549 to S/PV.558 | Security Council Official Records, Sixth Year, 549th to 558th meetings (26 July to 1 September 1951); cover pages and agenda read 2026-10-23 (#1426) | https://documents.un.org/api/symbol/access?s=S/PV.549&l=en&t=pdf (same pattern for 550 to 558) |

Period press (JTA Daily News Bulletin, by bulletin date):

| Short name | URL |
|---|---|
| JTA 14 Dec 1949 | https://www.jta.org/archive/israel-premier-and-parliament-to-move-to-jerusalem-hen-gurion-reads-declaration |
| JTA 26 Jan 1950 | https://www.jta.org/archive/jerusalem-is-capital-not-only-of-israel-but-of-jewish-nation-ben-gurion-declares |
| JTA 30 Mar 1950 | https://www.jta.org/archive/ben-gurion-addresses-parliament-on-motives-for-proclaiming-jerusalem-israels-capital |
| JTA 19 Jun 1950 | https://www.jta.org/archive/sharett-greets-tri-partite-statement-on-near-east-arms-policy |
| JTA 9 Oct 1950 | https://www.jta.org/archive/major-drive-against-black-market-on-in-israel-ben-gurion-gets-extraordinary-powers |
| JTA 5 Jan 1951 | https://www.jta.org/archive/new-crisis-in-israel-cabinet-orthodox-minister-resigns-two-more-may-quit-government |
| JTA 15 May 1951 | https://www.jta.org/archive/israel-determined-to-remove-arab-forces-from-demilitarized-zone-sharett-says |
| JTA 25 May 1951 | https://jta.org/1951/05/25/archive/hearings-on-jerusalem-bomb-plot-opens-court-orders-leaders-held-another-wee |
| JTA 1 Jun 1951 | https://jta.org/1951/06/01/archive/six-members-of-brit-kanaim-released-five-others-transferred-to-jerusalem-jail |
| JTA 6 Jun 1951 | https://www.jta.org/archive/sixteen-suspected-members-of-brit-kanaim-released-only-13-men-will-face-trial |
| JTA 13 Jun 1951 | https://www.jta.org/archive/ben-gurion-blames-u-s-for-united-nations-order-to-cease-operations-on-huleh |
| JTA 1 Aug 1951 | https://www.jta.org/archive/mapai-leads-in-israeli-elections-general-zionists-score-high-in-tel-aviv-haifa |
| JTA 10 Aug 1951 | https://www.jta.org/archive/distribution-of-seats-in-israel-parliament-announced-knesset-to-meet-within-ten-days |
| JTA 28 Nov 1951 | https://www.jta.org/archive/striking-israeli-seamen-clash-with-police-100-strikers-arrested |
| JTA 29 Nov 1951 | https://www.jta.org/1951/11/29/archive/striking-seamen-storm-haifa-jail-seek-to-release-sailors |
| JTA 4 Dec 1951 | https://www.jta.org/archive/israel-seamens-strike-enters-fourth-week-settlement-sought |
| JTA 17 Dec 1951 | https://www.jta.org/1951/12/17/archive/scores-of-police-israeli-seamen-injured-in-clashes-in-haifa |
| JTA 24 Dec 1951 | https://www.jta.org/archive/flood-endangers-new-settlements-in-israel-govt-mobilizes-aid |

## 1. Low international support

### 1a. The United States note, 28 May 1949, and Israel's reply, 8 June 1949 (strongest candidate)

Turn: 15 May 1949 (note, first response and reply all fall in it). No event on `main` covers it; `sinai_withdrawal`
(January 1949) is a separate episode of US pressure.

- **The note.** The Acting Secretary of State sends, for the President, a note to be delivered to the Prime Minister.
  [V] FRUS 1949 VI d705: the US "has supported the position that Israel should be expected to offer territorial
  compensation for any territorial acquisition which it expects to effect beyond the boundaries set forth in the res of
  the GA of Nov 29"; if Israel holds its course, "the US Govt will regretfully be forced to the conclusion that a
  revision of its attitude toward Israel has become unavoidable."
- **The first response, 29 May.** [V] FRUS 1949 VI d706, the Prime Minister to the US Ambassador, "off the record": "US
  is powerful and we are weak; we could be destroyed; but we do not intend to commit suicide by accepting November 29
  settlement".
- **The reply, 8 June.** [V] FRUS 1949 VI d736: "The principle of territorial compensation, related to the 1947 award,
  is one which the Government of Israel cannot accept." On refugees: "It is inconceivable that the Government of Israel
  should find itself able to undertake in one and the same breath the absorption of mass Jewish immigration and the
  reintegration of returning Arab refugees."
- **The later offer, 3 August 1949** (turn 15 July 1949). [V] FRUS 1949 VI d884, the PCC meets the Israeli delegation
  at Lausanne: "approximate figure Israeli Government had in mind was 100,000"; the delegation head "desired make
  statement that 100,000 represented heavy tax on Israel."
- **What Israel decided.** On the record: refuse territorial compensation and mass return (8 June), then a limited
  offer at Lausanne (3 August). The options a card could offer are those two lines plus accepting the US terms. No
  cabinet minutes were read; the offer's first notice to the PCC (28 July, per a FRUS document header seen by a helper)
  is a **lead**.
- **Sensitivity:** refugees (CONTENT_POLICY). The documents' own framing of the 1948 exodus is contested; card text
  should keep to the diplomatic facts.

### 1b. The General Assembly's Jerusalem resolution, 9 December 1949, and the move to Jerusalem

Turn: 15 November 1949 (resolution and the Knesset statement); the US response falls in the 15 December 1949 turn. No
event on `main` covers it (`jerusalem_military_government` is July 1948).

- **The resolution.** [V] UNGA 303 (IV), adopted at the "275th plenary meeting", "9 December 1949"; it restates that
  the City of Jerusalem "shall be established as a corpus separatum under a special international regime". The vote
  count was not on the text layer read and is not given here.
- **Israel's response.** [V] JTA 14 Dec 1949 (the Prime Minister in the Knesset, "today"): the decision "is utterly
  incapable of implementation"; the government and the Knesset are to move to Jerusalem; the Knesset voted 59 to 27 to
  postpone a general debate. Whether the session was on 13 or 14 December is not settled here: the Knesset protocol
  was **not reached** (fs.knesset.gov.il and the Knesset OData return "temporarily inaccessible from your location"
  from this machine).
- **What it cost.** [V] FRUS 1950 V d309 (4 January 1950): "US continues to support principle internationalization of
  Jerusalem", and "Dept does not desire Emb Tel Aviv to conduct official business in Jerusalem with Israeli Central
  Govt officials who may move there."
- **Follow-through.** [V] JTA 26 Jan 1950: the government "will spend more than $1,000,000" by 1 April on moving the
  Knesset and the offices. [V] JTA 30 Mar 1950: the government made Jerusalem the capital "not only on a de jure basis
  but also de facto". The Knesset declaration of 23 January 1950 and its vote are a **lead** only (search summary).
- **Options on the record:** accept the regime, or reject it and move the offices (what Israel did). Sensitivity: low;
  Holy Places wording needs care.

### 1c. Security Council resolution 93, 18 May 1951 (the Hula and the demilitarised zone), inside the 1951 stretch

Turn: 15 May 1951. **Overlaps `hula_drainage`**, whose window is 7 March to 18 May 1951: resolution 93 falls on the
window's last day. A card here would be the arc's second step, or folded into `hula_drainage`; the owner decides.

- **The resolution.** [V] UNSC 93 (1951), "Resolution of 18 May 1951": it finds that "Aerial action taken by the
  forces of the Government of Israel on 5 April 1951" violated the cease-fire, "calls upon the Government of Israel to
  comply" with the Chief of Staff's and the Mixed Armistice Commission's requests on the drainage works, and decides
  that Arab civilians "removed from the demilitarized zone by the Government of Israel should be permitted to return
  forthwith". The adoption line after the text reads "Adopted at the 547th meeting by 10 votes to none, with 1
  abstention (Union of Soviet Socialist Republics)". (The "545th meeting" line printed just above the heading belongs to
  the resolution before it; the compilation prints each adoption line after its text.)
- **Before it.** [V] JTA 15 May 1951: the Acting Prime Minister told the Knesset that Israel "has accepted the
  cease-fire order".
- **After it.** [V] JTA 13 Jun 1951: the Prime Minister said the US "bears the greatest part of the responsibility" for
  the resolution and that "the work would go on". A dated decision to suspend or resume the works between 18 May and
  13 June was **not found**.
- **Sensitivity:** war casualties in the April fighting; Arab civilians removed from the zone.

### 1d. Also checked, weaker

- **The Tripartite Declaration, 25 May 1950** (turn 15 May 1950). [V] JTA 19 Jun 1950: the Foreign Minister welcomed
  it but Israel "is not completely satisfied" with its arms section. A statement, not a decision; the declaration's
  own text was not opened. Mostly support up, not down. Not recommended.
- **Korea, June to July 1950.** The cabinet's decision to back the Security Council (said to be 2 July 1950) is a
  **lead** only: the pages carrying it (an NLI blog, a Detroit Jewish News scan) returned 403. Support up, not down.
- **Security Council resolution 95, 1 September 1951** (turn 15 August 1951, inside the 1951 stretch). Three dated
  steps, kept apart (all read 2026-10-23 for #1426 as page images; [V, agent read; a person's check pending]):
  1. **The complaint, S/2241.** A letter "dated 11 July 1951" from Abba Eban, "Permanent Representative of Israel to
     the United Nations", to the President of the Council, asking "that the following item be placed on the agenda of
     the Security Council for urgent discussion"; the item is "Restrictions imposed by Egypt on the passage of ships
     through the Suez Canal." Under the heading, the reprint carries "[Original text: English]" and "[12 July 1951]".
     The UN Digital Library record (480045) gives the letter date as 1951-07-11 and the document date as 1951-07-12. So:
     written and dated **11 July 1951**; issued as S/2241 under **12 July 1951**. Neither source uses the word
     "circulated" or records the day of receipt: whether 12 July is the day it was circulated is **not found**. The scan
     is the Official Records reprint (printed pp. 9-10), not the mimeographed original; resolution 95's own footnote
     cites it as "Sixth Year, Supplement for July, August and September 1951, document S/2241".
  2. **The Council's meetings.** The item, "(a) Restrictions imposed by Egypt on the passage of ships through the
     Suez Canal (S/2241)", is on the agenda of the 549th meeting, **26 July 1951**; at that meeting the Council invited
     the representatives of Israel, Egypt and Iraq to take part without vote (UNSC 95 compilation, "Decision"). The
     meeting records that discuss it are the 549th (26 July), 550th and 551st (both 1 August), 552nd and 553rd (both 16
     August), 555th (27 August), 556th (29 August) and 558th (1 September). The 554th (23 August) and 557th (31 August)
     records hold only an official communiqué; their text layer does not mention Suez.
  3. **The resolution, 1 September 1951.** [V] UNSC 95 (1951) finds Egypt's practice "an abuse of the exercise of the
     right of visit, search and seizure" and "Calls upon Egypt to terminate the restrictions". The compilation heads it
     "[S/2322]". The scan (printed p. 11) reads "Adopted at the 558th meeting by 8 votes to none, with 3 abstentions
     (China, India, Union of Soviet Socialist Republics)"; the text layer's "668th" is an OCR error. The 558th meeting
     record (1 September 1951) has the same count: "The draft resolution was adopted by 8 votes to none, with 3
     abstentions", China, India and the Soviet Union abstaining.

  It answers Israel's own complaint, so it is support **up**. A fit for a card on its own merits, not for a "low
  support" gate.

## 2. Low social cohesion

### 2a. The seamen's strike, November and December 1951 (strongest candidate)

Turn: 15 November 1951 (the same turn as `forex_crisis_1951`; just **after** the 1951 stretch). No event on `main`.

- [V] JTA 28 Nov 1951: "More than 100 crewmen of a strikebound vessel in Haifa were arrested"; the Defense Ministry sent
  "draft notices" to striking seamen.
- [V] JTA 29 Nov 1951: the Prime Minister in the Knesset called the strike "a criminal attempt by enemies of the state
  to sabotage the nation's vital arteries" and said "there was not and there is not a strike"; the seamen's union was in
  a dispute with the Histadrut.
- [V] JTA 4 Dec 1951: "The strike is in its fourth week"; talks with the Histadrut were reopening.
- [V] JTA 17 Dec 1951: "Thirty police and scores of strikers were injured", the worst violence of a strike "now in its
  second month".
- **Start, Monday 12 November 1951** (issue #1425; read 2026-10-23 from the scans by Claude Code, an AI, not yet by a
  person):
  - [V] JTA Daily News Bulletin, 13 Nov 1951 (Vol. XVIII No. 219), p. 3, "350 Israel Seamen Halt Work in Dispute with
    Histadrut", dateline "HAIFA, Nov. 12": "Some 350 Israeli seamen today 'resigned' from their jobs in a dispute
    between the seamen's union and the Histadrut. As a result, three Israeli vessels are tied up in the harbor here."
    The union wanted its own hiring offices; the Histadrut called the "resignations" unjustified.
    http://pdfs.jta.org/1951/1951-11-13_219.pdf
  - [V] *The Jerusalem Post*, Tuesday 13 Nov 1951, p. 1, "350 Sailors Quit, Eight Israel Ships Tied Up", dateline
    "HAIFA, Monday": "About 350 sailors signed off eight Israel ships in Haifa and Tel Aviv Ports today in accordance
    with a decision taken at the Seamen's conference here yesterday." The crew of the s.s. Elath walked off that
    afternoon; "the rest of the seamen are to go ashore tomorrow morning" (13 November). Shoham said the 24-hour notice
    broke a labour agreement requiring one month's notice.
    https://archive.org/details/per_jerusalem-post_the-jerusalem-post_1951-11-13_27_7407
  - The ship count differs: three vessels tied up in Haifa (JTA) against crews signing off eight ships in Haifa and Tel
    Aviv (Post). This may be a difference of scope (ships already idle on 12 November against crews that resigned) rather
    than a conflict; not settled.
  - The form was a collective resignation, not a declared strike; the Post of 14 Nov (p. 1) quotes the Communications
    Minister, D. Z. Pinkas, that "they could not be considered strikers, but merely people who have resigned from their
    jobs". JTA calls it a strike from 15 Nov on. The [S] he.wiki article (שביתת הימאים (1951)) has the resignations on
    13 November and an open strike call only after 21 November, citing the Hebrew press, which is not readable here.
  - Checks: 12 Nov to 4 Dec is the 23rd day, inside the fourth week, as JTA says on 4 Dec; 12 Nov to 17 Dec is the 36th
    day, so "second month" on 17 Dec also fits. (Our arithmetic.)
- **End, return to work voted 23, 24 or 25 December 1951** ([C], below):
  - [V] JTA Daily News Bulletin, 26 Dec 1951 (No. 248), p. 2, "Israeli Seamen End Strike; First Vessel Leaves for
    Trieste", dateline "TEL AVIV, Dec. 25": "A general meeting of Israel's striking seamen today ratified decisions of
    their representative group to call off the strike and return to work immediately. The vote was 400-3." Terms as
    reported: the strikers return to work; the Histadrut "will press for release from military service of strikers
    conscripted when they quit their jobs"; all questions in dispute go to the Histadrut Council, whose decisions the
    seamen accept; 120 seamen to return within days, the rest to wait for ships now abroad.
    http://pdfs.jta.org/1951/1951-12-26_248.pdf
  - [C] he.wiki gives the general meeting as **24 December** and the vote as unanimous (JTA: 400-3), citing *HaBoker*, 25 Dec 1951, "הימאים הפסיקו את
    שביתתם" (not readable here: NLI returns 403). A cable written on 25 December may report a meeting held the night
    before; that is our reading, not a source. JTA of 27 Dec 1951 says the seamen "formally ended their strike against the Histadrut yesterday"; the item has no dateline, so the day it means is not shown. https://www.jta.org/archive/israeli-seamen-returning-to-ships-jobs-provided-for-all
  - [C] [S] The Israel Democracy Institute's survey "כאן שביתה! על תרבות השביתות הישראלית" gives the end as "23 בדצמבר 1951" and the length as 43 days (12 November to 23 December is 42 days counting both ends; the source does not say how it counts), citing the Knesset's "הכנסת השנייה" page. https://www.idi.org.il/parliaments/11092/11096. The day stays open between 23, 24 and 25 December.
- **Parties, ports, ships and the path to a settlement, as the sources give them:**
  - The Seamen's Association (its secretariat elected at the seamen's convention in early November) against the
    Histadrut (secretary-general Mordecai Namir) and the shipping companies Shoham and M. Dizengoff (*Jerusalem Post* 13 and 14 Nov;
    JTA 21 Nov). The disputed points as JTA gives them: the union's own hiring offices (JTA 13 Nov) and autonomy within the
    Histadrut (JTA 21 Nov); the right to strike in foreign ports is the account of the Histadrut's representative in the United States
    of the seamen's demands (JTA 21 Dec).
  - Ports: Haifa and Tel Aviv; abroad, New York and Baltimore, where 59 strikers on the *Yafo* and the *Haifa* were held
    on Ellis Island and deported to Israel (JTA 21 Dec 1951, https://www.jta.org/archive/striking-israeli-seamen-deported-from-u-s-to-israel-on-greek-ship ).
    Ships named: *Elath*, *Akko*, *Meir Dizengoff* (Post 13-14 Nov), *Kedma* (JTA 21 Nov), *Negba* (JTA 29 Nov).
  - Path to a settlement (no third-party mediator is named in the reports read): Pinkas convened officials on 14 Nov to end the tie-up (JTA 15 Nov 1951, No. 221, p. 3,
    http://pdfs.jta.org/1951/1951-11-15_221.pdf ); the Histadrut was reported to be considering a separate seamen's union while other vessels prepared to
    leave with volunteer crews it provided (JTA 21 Nov, https://www.jta.org/archive/striking-israeli-seamen-agree-to-sail-ships-with-jews-from-libya );
    a "new approach for an agreement" with the Histadrut was reported on 4 December, with negotiations that "now appear near" (JTA 4 Dec); the end came through an agreement with the
    organising committee of the new Histadrut-sponsored union (JTA 26 Dec).
  - [S] he.wiki also records an earlier, separate dispute on the *Negba* that began in Marseille in June 1951 and spread
    to other Shoham ships in July 1951; it is not part of the November-December dispute and is not a start date for it.
- **Options on the record:** the government's line was police action, call-up notices and foreign crews; a negotiated
  settlement through the Histadrut was the other course being tried. Sensitivity: low; keep to the labour dispute.

### 2b. The religious underground's plot and the arrests, May and June 1951 (inside the 1951 stretch; sensitive)

Turn: 15 May 1951. No event on `main`.

- [V] JTA 25 May 1951: a government spokesman on "the underground movement which planned to blow up the Parliament
  building"; "the Cabinet today appointed a three-man committee" of ministers to look into the conditions of detention.
  The link to the bill on women's national service is the bulletin's account of the charges, paraphrased here.
- [V] JTA 1 Jun 1951: six of those arrested "were ordered released".
- [V] JTA 6 Jun 1951: the Acting Prime Minister announced in the Knesset "the release of the last 16 of 56 persons
  detained"; 13 would face trial. The Knesset set up a committee on "police brutality" charges and passed a motion
  against "religious fanatics".
- The exact day of the arrests (about mid-May 1951) is a **lead**; the trial's outcome (1952) is outside the window
  and was not checked.
- **Sensitivity: high.** A plot against the Knesset would carry the `terror` tag (maintainer's call). Card text should
  keep to the plot as charged, the arrests and the government's inquiry; no individuals named.

### 2c. The religious bloc's crisis over the immigrant camps, January 1951

Turn: 15 December 1950. **Overlaps** `education_registration_1951` (from 29 November 1950), which already carries the
camp-education dispute; `frumkin_commission` (1950) is the earlier step of the same arc.

- [V] JTA 5 Jan 1951: the Minister of Religious Affairs "submitted his resignation after a stormy Cabinet discussion on
  the situation in the immigrant camps".
- The government's fall on 14 February 1951 and the Knesset's dissolution are [S] only (Wikipedia, "First Knesset";
  the Israel Democracy Institute's 1951 election page names "serious coalition crises over issues of religion and
  education"). The Knesset record was **not reached**.
- Recommendation: a source for widening the existing education card, not a new one.

### 2d. Also checked, weaker

- **The black-market drive, October 1950** (turn 15 September 1950). [V] JTA 9 Oct 1950: the cabinet gave the Prime
  Minister powers for police and troops to carry out "large-scale searches". Overlaps `ministry_supply_crisis`
  (October 1950).
- **The Second Knesset election, end of July 1951** (turn 15 July 1951, inside the 1951 stretch). [V] JTA 1 Aug 1951
  (the vote "yesterday", counted from the bulletin's dateline; the 30 July date usually given was not read on a primary
  page here; the General Zionists had "campaigned on a program of free enterprise") and JTA 10 Aug 1951
  (Mapai "45 seats", the General Zionists "20 deputies"). Not a cohesion event, but the one sourced civic anchor in the
  stretch; `elections_1949` is the precedent for an election card.
- **The winter flood, December 1951** (turn 15 December 1951). [V] JTA 24 Dec 1951: the cabinet approved "temporary
  wooden housing" for evacuees from immigrant work villages. Disaster relief, not strain.
- **Protests in the immigrant camps, and against austerity, 1949-51:** no dated protest with a recorded government
  response was found on a page that could be opened. **not found**, not "did not happen".

## 3. The 15 May to 15 November 1951 stretch

The gap test (`EventRetimingTests`) dates a card by the first state it has fired in (`CardDate`). That is the **first
15th on or after the event's `from`**: `israel_bonds` (from 10 May 1951) reaches the player on 15 May 1951, and
`forex_crisis_1951` (from 19 October 1951) on 15 November 1951. So a candidate dated after 15 May 1951 reaches the
player a month after the turn it falls in, and it does split the stretch.

Sourced candidates inside it, by turn:

| Turn | Candidate | Kind | Sources | Card reaches the player | Stretch becomes | Note |
|---|---|---|---|---|---|---|
| 15 May 1951 | Resolution 93 and Israel's response (1c) | Low international support | UNSC 93 (1951); JTA 15 May, 13 Jun 1951 | 15 June 1951 (dated 18 May) | 1 + 5 months | Overlaps `hula_drainage` (ends 18 May) |
| 15 May 1951 | The religious underground's plot (2b) | Low cohesion; sensitive | JTA 25 May, 1 Jun, 6 Jun 1951 | 15 June 1951, if the window opens after 15 May | 1 + 5 months | `terror` tag likely |
| 15 July 1951 | The Second Knesset election (2d) | Civic anchor, not gated | JTA 1 Aug, 10 Aug 1951 | 15 August 1951 (end of July) | 3 + 3 months | |
| 15 August 1951 | Resolution 95 on the Suez Canal (1d) | Support up | UNSC 95 (1951); S/2241; S/PV.549-558 | 15 September 1951 (dated 1 September) | 4 + 2 months | Complaint S/2241 dated 11 July 1951 (issued 12 July); on the agenda 26 July |

The two May 1951 candidates would shorten the stretch to 5 months if their window opens after 15 May; a window opening
on 15 May itself would share the turn on which `israel_bonds` already fires. A card at the end of July 1951 splits it
into 3 and 3 months; one dated 1 September 1951 into 4 and 2. In each case only if the card fires in the historical run
(see Scope).

## Not reached, and gaps

- **Knesset protocols and the Knesset OData:** "temporarily inaccessible from your location" from this machine. Not
  read: the December 1949 Jerusalem debate, the 23 January 1950 declaration, the February 1951 vote, the November 1951
  strike debate. A reader in Israel should check these.
- **JPress / NLI:** 403 here, as before. The NLI blog on Israel and Korea was not opened.
- **Detroit Jewish News scans (Bentley Library):** 403. Two leads rest on them (the Korea cabinet decision, the
  February 1951 vote).
- **FRUS 1951 V** on the Hula after resolution 93: not opened (the site rate-limited a helper).
- The day of the May 1951 arrests: not found.

## Summary

| Candidate | Anchor | Situation | Fits a card | Main sources | Sensitivity |
|---|---|---|---|---|---|
| US note and Israel's reply | 28 May and 8 June 1949 | Low international support | Yes: a recorded Israeli decision with real options | FRUS 1949 VI d705, d706, d736, d884 | Refugees |
| Jerusalem resolution and the move | 9 Dec 1949 to Jan 1950 | Low international support | Yes: the government's decision to move is on record | UNGA 303 (IV); JTA 14 Dec 1949, 26 Jan, 30 Mar 1950; FRUS 1950 V d309 | Low |
| Resolution 93 | 18 May 1951 | Low international support | Maybe: overlaps `hula_drainage` | UNSC 93 (1951); JTA 15 May, 13 Jun 1951 | Casualties, civilians |
| Tripartite Declaration | 25 May 1950 | (support up) | No: a statement, not a decision | JTA 19 Jun 1950 | None |
| Korea | July 1950 | (support up) | Not sourced (lead) | none read | None |
| Resolution 95 (Suez) | 1 Sep 1951 | (support up) | Maybe, not as a gated card | UNSC 95 (1951) | None |
| Seamen's strike | Nov to Dec 1951 | Low social cohesion | Yes: police action and call-ups on record | JTA 28, 29 Nov, 4, 17 Dec 1951 | Low |
| Religious underground's plot | May to June 1951 | Low social cohesion | Maybe: sensitive | JTA 25 May, 1, 6 Jun 1951 | `terror` likely |
| Religious bloc's camp crisis | Jan 1951 | Low social cohesion | No: widen the existing education card | JTA 5 Jan 1951 | None |
| Black-market drive | Oct 1950 | Low social cohesion | No: overlaps `ministry_supply_crisis` | JTA 9 Oct 1950 | None |
| Second Knesset election | end of Jul 1951 | none | Maybe, as an anchor | JTA 1, 10 Aug 1951 | None |
| Winter flood | Dec 1951 | none | No: relief, not strain | JTA 24 Dec 1951 | None |

Recommendation (the owner decides under Q-EVT-5): the US note (1a) and the Jerusalem move (1b) for "Low international
support", the seamen's strike (2a) for "Low social cohesion". Each has a dated Israeli decision on a primary page and no
overlap with the events on `main`.
