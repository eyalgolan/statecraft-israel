# The rationing history note for the economic defeat (research note, 2026-10-20)

Status: research only. Nothing here changes `content/`, the sim or the approved short defeat line. Refs #8 (the
bounded slice claimed in its comments on 20 October 2026) and Q-DEF-4 (b) (owner, 18 October 2026, #784: "the short
factual line per cause plus one sourced history note, rationing on 26 April 1949 and the four armistices of 1949,
naming no losses"; the Hebrew goes to native-speaker review). This note covers the rationing half only; the
war-defeat half waits for #749/#750. Product integration coordinates with #812 and is not implemented here.

## Conventions

As in the other sourcing notes: **[V]** verified in a primary or official record, or two independent sources that
agree; **[S]** read in one named source, nothing found against it; **[C]** sources disagree; **not reached** means the
source exists but could not be opened today. Quotations are verbatim from the named source, for checking, and none is
meant for game text. The player-facing draft names offices, not people (`docs/WRITING_EVENTS.md`). Check dates are
2026-10-20 unless an entry says otherwise.

## Sources read

| Source | What it is | Reached |
|---|---|---|
| JTA, 26 April 1949 | "Israeli Knesseth to Debate Rationing Program Today; Plan to Be Presented by Ben Gurion" (https://www.jta.org/archive/israeli-knesseth-to-debate-rationing-program-today-plan-to-be-presented-by-ben-gurion) | yes |
| JTA, original Daily News Bulletin, 26 April 1949, PDF page 3 | Dispatch datelined 25 April (http://pdfs.jta.org/1949/1949-04-26_094.pdf#page=3) | reached by Codex agent, 2026-10-19 UTC |
| he.wiki, "תקופת הצנע" | https://he.wikipedia.org/wiki/תקופת_הצנע (MediaWiki API, plaintext) | yes |
| Knesset 1, 25th sitting | Plenum protocol, 26 April 1949 (https://fs.knesset.gov.il/1/Plenum/1_ptm_250102.pdf) | **not reached** (the host geo-blocks this location; the 14 October 2026 read is recorded in `content/events/austerity_1949.yaml` and is used from there, tagged as such) |

Codex agent verification, 2026-10-19 UTC: the original JTA bulletin's publication date and dispatch dateline were checked separately. This agent check is not a person's EN review or a native-speaker HE review; the author's location-specific minutes-access report and human review queue below remain unchanged.

## What the sources establish

- **Presentation to the Knesset is documented on 26 April 1949; an operational start date is not established here.** he.wiki: the decision on a
  rationing policy was taken at the first elected government's first sitting on 26 April 1949, and later that day the
  Prime Minister announced it to the Knesset as a temporary arrangement forced by circumstances ("הודיע ... לכנסת על
  הנהגת המדיניות כהוראת שעה הנובעת מכורח המציאות") [S, he.wiki]. The 14 October read of the minutes records the plan
  presented to the plenum of 26 April, with saving foreign currency among its principles [V as recorded on
  2026-10-14]. The JTA item published on 26 April has the dispatch dateline "TEL AVIV, April 25" in the original
  bulletin (PDF page 3). Its "yesterday" places the cabinet discussion on 24 April, and its "tomorrow" places the
  Knesset presentation on 26 April [V, JTA; Codex agent check, 2026-10-19 UTC]. Publication, dispatch and cabinet
  discussion are separate dates; 26 April is the sourced public presentation date.
- **The scale the plan answered.** The 14 October read of the minutes records the Prime Minister's figures: more than
  210,000 arrivals since the state was founded, more than 102,000 of them since January 1949 [V as recorded on
  2026-10-14]. he.wiki gives the same rationale (mass immigration, no preparation, no foreign-currency reserves
  ("לא היו יתרות מטבע חוץ")) [S, he.wiki].
- **What the plan rationed.** JTA: "Most foodstuffs and essential consumers' goods will be rationed under this plan",
  clothing manufactured to regulation specifications, luxury production for the local market banned, tighter import
  regulation, and subsidies for cheap-goods industries; the aims were "to correct conditions caused here by
  scarcities" and, through the luxury-import ban, to "save foreign exchange" [V]. he.wiki adds the mechanism (each
  citizen assigned to a grocery, a personal points booklet) and the price rationale (a low price ceiling so everyone,
  above all the unestablished immigrants, could eat) [S, he.wiki].
- **How long it ran.** he.wiki dates the austerity regime 1949-1959, easing from late 1950 [S, he.wiki]. Neither JTA
  nor the recorded minutes give an end date; the regime's day-to-day start in the shops is not pinned by any of the
  three sources, and the note below deliberately makes no implementation-date claim.

## The precision the owner asked for

The approved short line stays untouched. The note ties the defeat to **26 April 1949 as the day the rationing plan
came before the Knesset** - a documented public presentation date, sourced as above. It does not say rationing began that
day, does not give loss figures, and changes no threshold, count or balance value.

## Draft history note (EN, for native-speaker review in HE)

> In the state's first year more than 210,000 immigrants arrived, and there were no foreign-currency reserves. On
> 26 April 1949 the government brought a strict rationing plan to the Knesset to save foreign currency and keep basic
> food within everyone's reach. The austerity regime it opened rationed food, clothing and other essentials; eased
> step by step, it stayed in force until 1959.

Every clause maps to a bullet in "What the sources establish" above. Offices are used where a person would be named;
the immigrant figures are the 14 October reading of the Knesset record and should be re-confirmed against the minutes
PDF when it is next reachable before the text ships.

## EN/HE review queue

- HE: the note needs a native-speaker translation and review before any integration (Q-DEF-4's standing gate).
- EN: a person's check of the draft against the minutes PDF (blocked today, see "Sources read") is the one open
  verification.

## Proposed integration point (for coordination with #812, not implemented here)

The short line ("Money and Food ran out") lives in `PlayerText.GameOver`; the end screen (#812) renders it. Proposal:
the game-over reason gains an optional companion `history_note` string, resolved the same way (one place for both
causes; the war-defeat note - the four armistices of 1949 - joins it when #749/#750 land), and the #812 end screen
shows it under the short line in a smaller, quieter style. That keeps one text owner for both causes, keeps the
approved short line byte-identical, and gives the HE review one file. A text fixture for the presentation pass would
assert only that the defeat path carries a non-empty note; the wording itself ships with the content change after the
owner and the HE reviewer pass on this draft.
