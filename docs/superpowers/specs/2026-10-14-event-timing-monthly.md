# Event timing under monthly turns (2026-10-14)

- **Status:** analysis and proposal for the cutover PR (#382). No content, sim or golden change is made here. The window
  edits below are proposals for the owner to accept or refuse; the cutover PR makes them.
- **Read against:** `origin/main` at `37fb6843` (14 October 2026).
- **Context:** the impact plan's §3, cutover step 2 ("Re-time the 31 event windows"), and its risk "Re-timed windows read
  wrong historically" ([impact plan](2026-10-12-gdd-v0.1-impact-and-plan.md)). Monthly turns: [monthly-turns.md](../../gdd/monthly-turns.md).

## Summary for the owner

- **All 31 events in `content/events/` were run on monthly turns** (method below). **29 fire. 2 never fire:**
  `sinai_withdrawal` and `rhodes_talks`. Both need `operation_horev` to have been answered. On monthly turns, Horev's card
  arrives on 15 January 1949, after both of their windows have closed.
- **Every 1949–1951 event is fine.** Monthly turns bring each card closer to its sourced date than today's quarterly
  turns do. For example, the Jordan armistice arrives 12 days after the signing instead of 54, and the Law of Return
  10 days after the vote instead of 53.
- **The war period reads wrong.** Of the 15 cards in 1948 and January 1949, 13 now arrive after the historical decision
  they ask the player to make. `WarEvents1948Tests` enforces "the card arrives before the historical decision" and fails
  on all 13. The worst cases:
  - `operation_horev` proposes the offensive on 15 January 1949, after it ended on 7 January.
  - `operation_hiram` asks whether to launch on 15 November, after the operation ended on 31 October.
  - `elections_1949` says "the vote is set for 25 January" on 15 February.
  - `altalena` comes on 15 July, three weeks after the shelling. It arrives together with the Ten Days and the Second
    Truce.
  - `ten_days` asks where the main effort should go when the fighting resumes, on 15 July. That is a week into the
    fighting, and the same card batch brings the truce that ends it.
- **Six `from` edits fix six of these, and through them the two broken events** (§4). They are proposed below and were
  tested on the sim. They leave **7 cards late**, and no window edit can fix those on the 15th-of-the-month grid: no turn
  date falls between the fact a card states and the decision it asks for. These need an owner call (§4, Q1).
- **Counts for the current content on monthly turns:**
  - 18 fine: 9 on time, and 9 late "on or after" by design but better than today;
  - 6 need a window edit;
  - 5 need an owner call;
  - 2 are broken.

## 1. How events fire

`EventSystem.Trigger` (`sim/Statecraft.Sim/EventSystem.cs`) runs at the end of every `Simulation.Step`, after the date
moves:

- **Which events are eligible.** An unfired event is eligible when its window `[from, to)` overlaps the turn just played
  (`[turnStart, s.Date)`), or contains `s.Date`. Its `requires` must also be met (an option already chosen), and its
  `conditions` too (no real event has any).
- **What the player sees.** The card reaches the player at `s.Date`, the end of that turn, and they answer it during the
  next turn. A card that fires after its window has ended (a "late fire") fills office names at the last day in the
  window (`FillDate`). The event card shows no date of its own: the player reads the turn date on the HUD.
- **The cap.** Every eligible hard anchor fires. At most 3 soft events fire per turn, in `from` then id order. The rest
  wait while their window lasts.
- **The resulting rule:** **a card fires on the first turn date on or after `from`**, whatever the window's length. No
  window is too short to be seen.
- **The exception is a `requires` chain.** The parent is answered during the turn after its card, so a follower can fire
  no earlier than one turn after the parent's card. It fires only if its window still overlaps that turn. A follower
  whose window ends within a month of the parent's card date vanishes.

**Turn lengths.**
- **Today (legacy loop):** `SecuritySystem.TurnLengthFor(c, date, rung)` reads `turn_lengths`. The real
  `content/rules.yaml` does not set it, so the default applies: a week on rung 5, a month on rung 4, a quarter on rungs
  1–3. The game starts on rung 5 (`start_rung`). The windows were tuned for those weekly war turns, so each card opens "on
  the turn date before" the decision (determinism.md, row "playtest 1 E fix 1"; the comments in each file).
- **Under monthly turns:** the cutover sets `turn_lengths: [month, month, month, month, month]`, as `MvpSlice` already
  does. The game then runs on the 15th of each month: 15 June 1948, 15 July 1948, and so on, to 15 January 1952 (44
  turns). There is one era (`founding`, to 1957), so no turn is adjusted before then. The rung no longer changes the turn
  length. The first truce's "do not fix to 4" comment and the "monthly grid of the second truce" timing notes become moot.
- **The grid can still move.** D-E5 is open: the MVP may move to a grid on the 1st. §5 compares the two grids.

## 2. Method

A throwaway program, outside the repo and not committed, played three runs from the golden seed in Historical mode to
1 January 1952 and logged the turn on which each event fired:

1. **legacy:** the real content and `HistoricalAI`. This is the golden run behind `Hash1952`, ending on 27 February 1952
   at turn 43.
2. **monthly-legacy:** the real content with only `turn_lengths` set to all months, and `HistoricalAI`.
3. **mvp:** `MvpSlice.Load` (the loop switch on, monthly turns) and `ReferencePlayer`, extended past `MvpGolden.Until` to
   1952.

Runs 2 and 3 fire every event on the same turn, so the GDD loop does not change event timing. A fourth run applied the
§3 edits in memory: the content loaded, all 31 events fired, and no order was refused for Attention in any run.

A small Python model of the trigger rule reproduced run 3 event for event. It was then used for the grid on the 1st (§5).

## 3. Every event

**How to read the tables.**
- *Window* gives the first and last day the window covers (`to` is exclusive in the YAML, so the last day is `to` minus one).
- *Today* is the card date in the legacy golden run, with its turn number.
- *Monthly* is the card date on the monthly grid, with the current windows.
- *Sourced* is the historical decision or fact the card leads to, taken from the file's own comments and `sources:`. For
  the 1948 events it is also `DecidedBy` in `WarEvents1948Tests`.
- *Δ* is the monthly card date minus the sourced date. A minus sign means the card comes first.
- *Proposed* is the card date after the edit in the last column, checked on the sim.

**Verdicts.**
- *On time:* the card arrives on or before the decision.
- *Late, as today:* a card that fires "on or after" a dated fact (the armistice files say so in their comments). It is
  later than the fact, but earlier than today.
- *Edit:* a window edit fixes it.
- *Owner:* no turn date falls between the fact the card states and the decision it asks for.
- *Broken:* the event never fires.

### 3.1 The war period, 15 May 1948 to January 1949

| # | Event | Window (anchor; requires) | Today | Monthly | Sourced | Δ | Verdict | Proposed |
|---|---|---|---|---|---|---|---|---|
| 1 | `idf_founding`, One Army | 22–31 May 1948 (hard) | 22 May 1948 (t1) | 15 June 1948 (t1) | order signed 26 May 1948 | +20 d | Edit | `from: 1948-05-15`: card at the game's start, 15 May (t0) |
| 2 | `first_truce`, The First Truce | 1–10 June 1948 (hard) | 5 June 1948 (t3) | 15 June 1948 (t1) | Res. 50 of 29 May; acceptance 10 June; in force 11 June | +5 d | Owner | none on this grid (Q1) |
| 3 | `altalena`, The Altalena | 19–22 June 1948 (hard) | 19 June 1948 (t5) | 15 July 1948 (t2) | arrival and the government's decision 20 June; shelling 22 June | +25 d | Edit, needs a source | `from: 1948-06-15`: 15 June (t1), if a source shows the ship was expected by then (Q2) |
| 4 | `ten_days`, The Ten Days | 3–10 July 1948 (hard; `first_truce: accept`) | 3 July 1948 (t7) | 15 July 1948 (t2) | truce ends and Dani begins 9 July | +6 d | Owner | none while `requires` stays (Q3) |
| 5 | `second_truce`, The Second Truce | 15–18 July 1948 (hard) | 17 July 1948 (t9) | 15 July 1948 (t2) | Res. 54 of 15 July; in force 18 July | −3 d | On time | — |
| 6 | `jerusalem_military_government`, Jerusalem | 17 July – 2 August 1948 (hard) | 17 July 1948 (t9) | 15 August 1948 (t3) | rejection 28–29 July; proclamation 2 August | +13 d | Edit | `from: 1948-07-15`: 15 July (t2) |
| 7 | `bernadotte_assassination`, The Mediator Assassinated | 17–23 September 1948 (hard) | 17 September 1948 (t11) | 15 October 1948 (t5) | killed 17 September; ultimatum and regulations 20 September | +25 d | Owner | none on this grid (Q1) |
| 8 | `latrun_proposal`, Latrun | 17–26 September 1948 (hard; `second_truce: comply`) | 17 September 1948 (t11) | 15 October 1948 (t5) | cabinet vote 26 September | +19 d | Edit | `from: 1948-09-15`: 15 September (t4) |
| 9 | `operation_yoav`, Operation Yoav | 15–22 October 1948 (hard) | 17 October 1948 (t12) | 15 October 1948 (t5) | convoy 15 October; Hebron refused, cease-fire 22 October | −7 d | On time | — |
| 10 | `operation_hiram`, Operation Hiram | 24–28 October 1948 (soft) | 24 October 1948 (t13) | 15 November 1948 (t6) | ALA attack 22 October; launched night of 28–29 October; over 31 October | +18 d | Owner | none on this grid (Q1) |
| 11 | `jerusalem_ceasefire`, Cease-fire in Jerusalem | 28–30 November 1948 (hard; `jerusalem_military_government: military_government`) | 28 November 1948 (t18) | 15 December 1948 (t7) | talks 28 November; signed 30 November | +15 d | Owner | none on this grid (Q1) |
| 12 | `operation_horev`, Operation Horev | 19–22 December 1948 (hard) | 19 December 1948 (t21) | 15 January 1949 (t8) | launched 22 December; ended 7 January 1949 | +24 d | Edit | `from: 1948-12-15`: 15 December (t7) |
| 13 | `sinai_withdrawal`, The British Ultimatum | 31 December 1948 – 2 January 1949 (hard; `operation_horev: launch`) | 2 January 1949 (t23) | **never** | facts 28–30 December; orders 1 January; withdrawal by 2 January | — | Broken | fixed by #12: 15 January 1949 (t8), +14 d (Q1) |
| 14 | `rhodes_talks`, Talks at Rhodes | 9–12 January 1949 (hard; `operation_horev: launch`) | 9 January 1949 (t24) | **never** | acceptance 6 January; talks open 13 January | — | Broken | fixed by #12: 15 January 1949 (t8), +2 d (Q1) |
| 15 | `elections_1949`, Electing the Constituent Assembly | 17–25 January 1949 (hard) | 23 January 1949 (t26) | 15 February 1949 (t9) | vote 25 January (date fixed 25 November 1948) | +21 d | Edit | `from: 1949-01-15`: 15 January (t8) |

### 3.2 1949, after the war

| # | Event | Window (anchor) | Today | Monthly | Sourced | Δ | Verdict |
|---|---|---|---|---|---|---|---|
| 16 | `armistice_egypt`, Armistice with Egypt | 24 February – 31 December 1949 (hard) | 27 February 1949 (t31) | 15 March 1949 (t10) | signed 24 February 1949 | +19 d | Late, as today |
| 17 | `armistice_lebanon`, Armistice with Lebanon | 23 March – 31 December 1949 (hard) | 27 May 1949 (t32) | 15 April 1949 (t11) | signed 23 March 1949 | +23 d | Late, as today (was +65 d) |
| 18 | `armistice_jordan`, Armistice with Jordan | 3 April – 31 December 1949 (hard) | 27 May 1949 (t32) | 15 April 1949 (t11) | signed 3 April 1949 | +12 d | Late, as today (was +54 d) |
| 19 | `armistice_syria`, Armistice with Syria | 20 July – 31 December 1949 (hard) | 27 August 1949 (t33) | 15 August 1949 (t15) | signed 20 July 1949 | +26 d | Late, as today (was +38 d) |
| 20 | `austerity_1949`, Austerity | 26 April – 31 December 1949 (hard) | 27 May 1949 (t32) | 15 May 1949 (t12) | plan before the Knesset 26 April 1949 | +19 d | Late, as today (was +31 d) |

### 3.3 1950 and 1951

| # | Event | Window (anchor) | Today | Monthly | Sourced | Δ | Verdict |
|---|---|---|---|---|---|---|---|
| 21 | `frumkin_commission`, Education in the Immigrant Camps | 1 January – 8 May 1950 (soft) | 27 February 1950 (t35) | 15 January 1950 (t20) | committee appointed by 19 January; first session 25 January 1950 | −4 d | On time |
| 22 | `maabarot_policy`, Transit Camps | 1 April – 31 May 1950 (hard) | 27 May 1950 (t36) | 15 April 1950 (t23) | Eshkol's proposal in April; the word ma'abarot 21 April; Kesalon in May 1950 | about 0 | On time |
| 23 | `law_of_return`, The Law of Return | 5 July 1950, one day (hard) | 27 August 1950 (t37) | 15 July 1950 (t26) | passed 5 July 1950 | +10 d | Late, as today (was +53 d) |
| 24 | `clothing_rationing_1950`, Rationing Clothes and Shoes | 1 July – 31 August 1950 (hard) | 27 August 1950 (t37) | 15 July 1950 (t26) | extended 31 July 1950 | −16 d | On time |
| 25 | `ministry_supply_crisis`, Closing the Ministry of Rationing | 1–31 October 1950 (hard) | 27 November 1950 (t38) | 15 October 1950 (t29) | Ben-Gurion resigned 15 October; new vote 1 November 1950 | 0 d | On time |
| 26 | `education_registration_1951`, Schools in the Transit Camps | 29 November 1950 – 13 February 1951 (hard) | 27 February 1951 (t39) | 15 December 1950 (t31) | dispute from 29 November 1950; government resigned 14 February 1951 | −61 d | On time |
| 27 | `reparations_claim`, A Claim on Germany | 1–12 March 1951 (hard) | 27 May 1951 (t40) | 15 March 1951 (t34) | note of 12 March 1951 | +3 d | Late, as today (was +76 d) |
| 28 | `hula_drainage`, Draining the Hula | 7 March – 17 May 1951 (soft) | 27 May 1951 (t40) | 15 March 1951 (t34) | Riley's request 7 March 1951 | +8 d after the trigger | On time |
| 29 | `iraqi_exodus`, The Jews of Iraq | 10 March – 30 June 1951 (soft) | 27 August 1951 (t41) | 15 March 1951 (t34) | asset-freeze law 10 March 1951 | +5 d after the trigger | On time |
| 30 | `israel_bonds`, Israel Bonds | 10 May 1951, one day (hard) | 27 May 1951 (t40) | 15 May 1951 (t36) | launch 10 May 1951 | +5 d | Late, as today |
| 31 | `forex_crisis_1951`, Foreign Currency Runs Out | 19 October – 31 December 1951 (soft) | 27 November 1951 (t42) | 15 November 1951 (t42) | Davis's report 19 October 1951 | +27 d | Late, as today |

**Notes on the rows.**
- **#1.** On 15 May the IDF does not yet exist under that name: the order created it. The card's opening clause ("Armed
  forces are being brought into the new Israel Defense Forces") needs a wording check if the window moves.
- **#3.** The file's sources date the ship's arrival (20 June) but not its sailing, or when its arrival was known. Moving
  the card to 15 June states "is due at Kfar Vitkin" five days ahead. That needs a source, or the window stays and the
  card is accepted as late (Q2).
- **#4 and #5.** The two cards come in the same batch. `ten_days` sets rung 5 and complying with `second_truce` sets
  rung 4, so the order of the answers decides the rung. Pending order (`from`, then id) puts the truce last, which is the
  historical order.
- **#7, #8 and #9.** The current windows put all three cards on 15 October, the day the truce broke down. The Latrun
  card ("break the truce") then arrives with Yoav. The #8 edit separates them.
- **#13 and #14.** With the #12 edit, Horev is answered during the turn from 15 December to 15 January. Both followers'
  windows overlap that turn, so both fire on 15 January 1949, together with `elections_1949` (3 hard cards).
- **#16 to #20, #23, #27, #30 and #31.** These cards fire "on or after" a dated fact. Several ask the player to decide
  something that has already happened ("whether Israel signs", "the Knesset is ready to vote", "it launches on 10 May").
  This is today's design, and monthly turns shorten the delay in every case.
- **The cap and Attention.** No turn has more than two soft events (15 March 1951), so the cap of 3 never binds. Some
  turns bring 3 hard cards: 15 July 1948, 15 October 1948, 15 January 1949 after the edits, and 15 March 1951 with two
  soft ones. `HistoricalAI` had no order refused in any run.

## 4. Proposed content edits and owner questions

**Edits, all tested together on the sim.** Each moves the window's `from` to the 15th on or before the decision, so the
card opens on the turn before it. That is the same convention as today, re-based from weekly turns to monthly ones. No
`to` changes. Office coverage for the new windows passes the loader.

1. `idf_founding.yaml`: `from: 1948-05-22` → `from: 1948-05-15`. Also check the text's opening clause (§3 note on #1).
2. `altalena.yaml`: `from: 1948-06-19` → `from: 1948-06-15`, **only with a source** for the ship being expected by
   15 June (Q2).
3. `jerusalem_military_government.yaml`: `from: 1948-07-17` → `from: 1948-07-15`.
4. `latrun_proposal.yaml`: `from: 1948-09-17` → `from: 1948-09-15`.
5. `operation_horev.yaml`: `from: 1948-12-19` → `from: 1948-12-15`. This also makes `sinai_withdrawal` and
   `rhodes_talks` fire.
6. `elections_1949.yaml`: `from: 1949-01-17` → `from: 1949-01-15`.
7. **Comments in every 1948 file** that say "the turn date before … (game timing)", "the monthly grid of the second
   truce", or (in `first_truce.yaml`) "do not fix it to 4": rewrite them for monthly turns.
8. **Tests:**
   - `WarEvents1948Tests`: re-base it on monthly turns.
   - Its `LateByDesign` map: widen it, or replace it, by the answer to Q1.
   - A new test for the cutover: every event fires in the historical monthly run. Today `RealContentTests` checks only
     1950–51, and `WarEvents1948Tests` only the war cards. A check like this would have caught the two broken events.

**Owner questions.**

- **Q1. Seven cards cannot be placed between their fact and their decision on the 15th grid.** They are `first_truce`,
  `ten_days`, `bernadotte_assassination`, `operation_hiram`, `jerusalem_ceasefire`, `sinai_withdrawal` and
  `rhodes_talks`; each card would have to state a fact before it happened. Options:
  - (a) **Accept them as late by design** (+2 to +25 days) and list them in `LateByDesign`. This is the cheapest, but
    several then ask about something already done, such as signing a cease-fire signed two weeks earlier, or launching
    Hiram after it ended.
  - (b) **Rewrite those texts** as decisions taken during, or after, the fact. Yoav's card already does this. For example,
    Hiram could become "how far to go" and the Jerusalem card "whether to keep the cease-fire". The text must stay
    sourced.
  - (c) **A sim change:** run `Trigger` once more right after the turn's commands, so that a follower fires in the same
    turn its parent is answered. This helps only the chained cards (#4, #13 and #14), and it moves every golden.

  **Recommendation:** (a) for `first_truce` (+5 d) and `rhodes_talks` (+2 d); (b) for the rest.
- **Q2. Altalena.** Is there a source for when the ship's arrival was expected? If not, should the card stay at 15 July
  (+25 d, after the shelling) with its text reworked as the aftermath, the Irgun units in the IDF dispersed?
- **Q3. Ten Days.** On the 15th grid the card cannot come before 15 July while it `requires` the first truce. The parent
  is answered during the turn that ends on 15 July. Options:
  - (a) drop `requires`, open on 15 June and keep the text: the truce is in force and "due to end on 9 July". But a
    player who refused the first truce would then read about a truce that never happened.
  - (b) keep `requires` and rewrite the card as a decision during the fighting.
- **Q4. Gaps between events.** The m3 ruling allows "at most a 4-turn gap with no events" in the historical run
  ([m3-rulings](../../m3-rulings.md)). It was written for quarterly turns, where 4 turns meant a year. On monthly turns:
  - June to October 1951 (turns 37 to 41) is a 5-turn gap, which breaks the ruling as worded;
  - September to December 1949 is exactly 4 turns.

  Restate the rule in months, or add a sourced 1951 event? (`WarEvents1948Tests.MaxEmptyTurns = 4` covers only the war
  period, and it holds: the longest gap there is 1 turn.)

## 5. The grid on the 1st (D-E5)

D-E5 may move the MVP to turns on the 1st of each month. The same model on that grid, with the current windows:
- all 31 events fire: the Horev chain survives, because Horev comes on 1 January 1949 and its followers on 1 February;
- but 13 of the 15 war cards still come after their decision.

**Placing cards between their fact and their decision.** Of the 15 war cards, these have a turn date between the fact
the card states and the decision it asks for:

| Grid | Cards that can be placed | Count |
|---|---|---|
| 15th | `idf_founding`, `altalena` (with a source), `second_truce`, `jerusalem_military_government`, `latrun_proposal`, `operation_yoav`, `operation_horev`, `elections_1949` | 8 |
| 1st | `first_truce`, `ten_days`, `jerusalem_military_government`, `latrun_proposal`, `operation_horev`, `sinai_withdrawal`, `elections_1949` | 7 |

**Neither grid can place** `bernadotte_assassination`, `operation_hiram`, `jerusalem_ceasefire` or `rhodes_talks`. On the
1st grid, `second_truce` and `operation_yoav`, both on time today on the 15th grid, would arrive after the truce took
effect and after the Yoav cease-fire.

**Recommendation:** for event timing alone, keep the 15th grid, and end the MVP on 15 January 1952. The edits in §4 are
written for the 15th grid. If D-E5 picks the 1st, they must be redone.
