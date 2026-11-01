# Source check of the two Abdullah drafts, Q-NEW-1 and Q-NEW-3 (research note, AI read, 2026-10-26)

Status: research only, an agent's read. Nothing here changes `content/`; no card is added and no `checked:` is
stamped. Refs #807, slice 1 (the owner's 22 and 24 Oct "Current state" comments). The drafts are the two YAML files in
[the 18 Oct comment](https://github.com/eyalgolan/statecraft-israel/issues/807#issuecomment-6245703774):
`jordan_nonaggression_talks` (card 1) and `abdullah_assassination` (card 2). Every read below is tagged
`[V, agent read 26 Oct 2026; a person's check pending]`: a person still has to open each source and add their own date
before either card gets `status: fact-checked`. The owner's 24 Oct ask is kept in view: the handwritten-note
attribution, initialled principles versus a signed agreement, and the unresolved gaps stay explicit.

Plan reference: [Phase 7 plan](../../docs/superpowers/plans/2026-10-15-gdd-phase-7.md) (the Q-NEW-1 and Q-NEW-3 rows,
both decided as recommended, the second on 18 Oct as "News line, no options"). Background:
[candidate-events-1948-51.md](candidate-events-1948-51.md) section 2a and section 3, which this note re-checks claim by
claim and does not repeat.

## Conventions

- Every source below was opened on **26 Oct 2026**. Result column: **match** (the source prints what the draft says),
  **conflict** (the source prints something different, both given), **not covered** (nothing in the sources opened
  settles it).
- Quotation marks are verbatim from the text read, for a person to find. None is meant for event text.
- JTA dating follows the owner's 24 Oct rule (#1596): the archive page shows the bulletin date; the event date is the
  dateline on the item in the bulletin PDF, read on the page image (`pdftoppm`, 130 dpi crops).
- Requests used the User-Agent `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`; no
  header was changed to get past a block.

## Sources used and how far each was reached

| Source | How it was read | Day |
|---|---|---|
| FRUS 1950 V d364, d366, d370, d382, d399 | `history.state.gov` answered **403** to every request (six documents, one retry after 60 s, and a fetch tool; no header changed). Read instead in the Office of the Historian's own TEI source for the volume, `HistoryAtState/frus`, `volumes/frus1950v05.xml` (https://raw.githubusercontent.com/HistoryAtState/frus/6170e4b32c1224002db2fe0c5d8d910597cddc2e/volumes/frus1950v05.xml), the text of the same documents with their footnotes. The rendered web pages were **not** seen. | 26 Oct 2026 |
| FRUS 1951 V d561, d563 | Same, `volumes/frus1951v05.xml`. | 26 Oct 2026 |
| JTA Daily News Bulletin, 3 Mar 1950, p. 1 (http://pdfs.jta.org/1950/1950-03-03_043.pdf) | Page image, dateline and quote read. Archive page: https://www.jta.org/archive/u-n-expecis-speedy-completion-of-formalities-on-israel-trans-jordan-peace-pact | 26 Oct 2026 |
| JTA Daily News Bulletin, 23 Jul 1951, pp. 1 and 2 (http://pdfs.jta.org/1951/1951-07-23_142.pdf) | Page images, datelines and quotes read. Archive pages: https://www.jta.org/archive/sharett-sees-danger-for-israel-in-abdullahs-assassination-urges-increased-defense and https://www.jta.org/archive/unfortunate-repercussions-for-israel-seem-as-result-of-abdullahs-assassination | 26 Oct 2026 |
| `content/events/armistice_jordan.yaml`, `content/events/bernadotte_assassination.yaml` | Repo files on `origin/main` `1256d026`, for the `requires` and the tags. | 26 Oct 2026 |

No Wikipedia page was used. The Knesset protocols for February 1950 were not reached (the Knesset site is geo-blocked
from this machine; see [knesset-protocols-situation-gated-1948-51.md](knesset-protocols-situation-gated-1948-51.md)).

## Card 1: `jordan_nonaggression_talks`

Tally of the numbered rows below: 17 match, 1 conflict (1.16, the date), 1 not covered (1.17).

### The card text, sentence by sentence

All lines but the last two rest on FRUS 1950 V d364 (McDonald, Tel Aviv, 19 February 1950, "Shiloah with Kollek reported").
[V, agent read 26 Oct 2026; a person's check pending] for every line marked match.

| # | Draft says | The source prints | Result |
|---|---|---|---|
| 1.1 | "At a night meeting" | "At Israel-Transjordan meeting night February 17" | match |
| 1.2 | the King "agreed with Israel's negotiators that a full peace was not possible at that time" | "agreed between Shiloah and Samir and King that now impossible make peace because neither side can now [make] necessary concessions. As King put it, 'now is too near period of war.'" The three named are Shiloah (Israel), Samir (Jordan's side) and the King. | match. "Negotiators" is fair for Shiloah; "Israel's" is right, but the agreement was three-way, and the reason (neither side can concede) is not in the card. |
| 1.3 | "then proposed a five-year renewable non-aggression pact" | "Towards end of meeting ... King reappeared and proposed following: 1. Five year renewable non-aggression pact maintaining armistice lines." | match |
| 1.4 | "It would keep the armistice lines" | "maintaining armistice lines" | match |
| 1.5 | "allow free dealings between the two countries with a free zone at Haifa" | "2. Free intercourse between two countries with free zone at Haifa." | match (paraphrase). d370 (24 Feb) words it "grant of free zones for Jordan in Haifa". |
| 1.6 | "set up committees to study the remaining issues" | "3. Setting up study committees to examine specific issues possible basis peace." | match in substance. "Remaining" is the card's word; the source says "specific issues possible basis peace" (the telegram is compressed). |
| 1.7 | "give the UN mutual guarantees of free access to the Holy Places" | "4. Mutual guarantees to UN re maintenance freedom access and protection holy places." | match |
| 1.8 | "suggested an Israeli concession to Arab property owners and reserved his claim to an outlet to the sea" | "Suggested some Israel concession to Arab property owners and said he reserves for future consideration his claim to sea outlet." | match |
| 1.9 | "{prime_minister} must decide whether Israel accepts the proposal in principle" | d364: "Shiloah consulted Prime Minister and Foreign Minister. His impression is Israel Cabinet will accept King's proposals 'as basis talks'." d370: "Shiloah announced Israel cabinet agreement in principle King's five year nonaggression proposal." | match for the outcome (agreement in principle). **Not covered:** who decided (the source says the cabinet, after Shiloah consulted the Prime Minister and the Foreign Minister), and the cabinet's own minute; the decision rests on Shiloah's report to the US Ambassador. |

### The comments in the draft file and the other sources it cites

| # | Draft says | The source prints | Result |
|---|---|---|---|
| 1.10 | d364 header comment: the meeting of "the night of 17 February"; peace "now impossible" | dated Tel Aviv, February 19, 1950, 7 p. m., and "meeting night February 17"; "now impossible" | match |
| 1.11 | d364: "February 1 Shiloah consulted" reads as a garble | The text prints exactly "February 1 Shiloah consulted Prime Minister and Foreign Minister." No editor's note corrects it. | match (the printed text is as the draft says; the intended day is **not covered**) |
| 1.12 | d370 (McDonald, 26 February): on 24 February Shiloah announced "Israel cabinet agreement in principle" and both sides initialled an "agreed set of principles" | "At beginning 24th Shiloah announced Israel cabinet agreement in principle King's five year nonaggression proposal and delegation's authority to discuss details." and "Finally, both sides initialled 'agreed set of principles'." The meeting was "night February 24". | match. The announcement is Shiloah's, at the start of the meeting; the date the cabinet decided is **not covered** ("cabinet ... in principle" is all it says). |
| 1.13 | d370 editor's footnote: Israel and Jordan "had initialed a nonaggression pact the previous evening" | The footnote (Sharett's handwritten note, per telegram 140, not printed) reads: "informing that Israel and Jordan had initialed a nonaggression pact the previous evening". | match, and worth a person's eye: FRUS calls it initialled, not signed; the "agreed set of principles" (d370 body) and "pact" (footnote) are two descriptions of the same initialling. |
| 1.14 | d366 (Geneva, 21 February): Eban reports "five-year non-aggression pact as practical half-way step between armistice and peace" | "Eban and Rafael reported Jordan now proposes five-year non-aggression pact as practical half-way step between armistice and peace since peace treaty would place Jordan in embarrassing position vis-a-vis other Arab states." | match. Two Israelis reported, Eban and Rafael; the draft names one. Corrected 2026-10-27: the printed word is "vis-à-vis" with the accent (HistoryAtState TEI of d366, read again 2026-10-27); the quote above gave it as "vis-a-vis". |
| 1.15 | d382 (McDonald, 3 March): Jordan's draft of 28 February dropped "non-aggression pact", the five years and freedom of trade; deadlock | Jordan's draft "eliminated (1) 'non-aggression pact' and substituted 'modification of armistice'; (2) provision for five years agreement; (3) 'freedom of commerce and trade.'" The meeting of 28 February ended at midnight with Shiloah saying "no sense repeating ourselves" and leaving a note "explaining impasse"; "At conclusion King 'commanded' both parties to come back 'on Friday.'" | match. "Deadlock" is the draft's word for the printed "impasse"; the same telegram shows the King ordering the talks to go on. Corrected 2026-10-27: the note was proposed, not left. d382 prints: At midnight Shiloah said "no sense repeating ourselves and to prevent misunderstanding, I will leave note for King explaining impasse." To this Jordans strongly objected and instead decided to "wake His Majesty". So the quote above, "leaving a note", should read: Shiloah said he would "leave note for King explaining impasse"; the Jordanians "strongly objected and instead decided to 'wake His Majesty'" (HistoryAtState TEI of d382, read 2026-10-27). |
| 1.16 | the effect +0.02 on `intl_standing`: "the Conciliation Commission welcomed the reported agreement (JTA, 3 March 1950)" | JTA, bulletin of Friday 3 March 1950, p. 1, dateline "GENEVA, March 2. (JTA)": "A spokesman for the U.N. Palestine Conciliation Commission today stated: 'The whole Conciliation Commission is delighted with the Israel-Transjordan agreement.'" It also says both delegations "continued to issue formal denials regarding the reported non-aggression pact negotiations". (On the page image the word is printed "delighte" with a smudge; the archive page text reads "delighte". The word is "delighted" in the sentence's sense; a person should look at the page.) | **conflict on the date**, match on the quote: the spokesman said it as printed, but the event date is **2 March 1950** (dateline), not 3 March (the bulletin). The draft's "(JTA, 3 March 1950)" and its source comment should say "dateline 2 March". The agreement was "reported" and officially denied that day: the card's text does not claim a signed pact, which fits. The +0.02 stays a design value. |
| 1.17 | the other option's comment: "No record was found of anyone in the cabinet arguing for" a full-peace stance in February 1950 | **Not covered.** The documents opened here (d364, d366, d370, d382, d399) show no such argument; that is a negative about five documents, not a search of the volume. | not covered |

### Card 1's frame: `requires`, window, effects

| # | Draft says | Check | Result |
|---|---|---|---|
| 1.18 | `requires: [{ event: armistice_jordan, option: sign }]` | `content/events/armistice_jordan.yaml` on `origin/main` has an option `id: sign` (line 17), window `from: 1949-04-03` to `1950-01-01`. | match (repo) |
| 1.19 | window from 1950-02-15 to 1950-03-15 (owner ruling) | The meeting is 17 February (d364), the cabinet's agreement was announced on 24 February (d370), and the talks were adjourned on 7 March (d399, below). The window covers all of it. The draft's own point stands: the card reaches the player two days before the offer. | match with the owner's ruling; no change proposed |

### What the opened sources add for card 1, not in the draft

- **d399** (McDonald, 9 March 1950, Shiloah's report): a short meeting on 7 March "which agreed to 'adjourn' but 'not break off' negotiations"; Jordan's note verbale said "Jordan Government accepts King's plan as basis for settlement. Owing to rumors and lies circulating in Jordan it is decided not to press negotiations but to ask for adjournment." [V, agent read 26 Oct 2026; a person's check pending]. This is the last meeting read here and bears on card 2's "they are over" (row 2.4).
- d385 (Drew, Amman, 5 March 1950): the telegram reports that "Samir Pasha having failed form Cabinet had renounced task"; not used by either card. [V, agent read 26 Oct 2026; a person's check pending]

## Card 2: `abdullah_assassination`

Tally of the numbered rows below: 12 match, 2 conflict (2.8, the date; 2.10, in substance), 1 not covered (2.4).

### The card text, sentence by sentence

[V, agent read 26 Oct 2026; a person's check pending] for every line marked match, here and in the next table.

| # | Draft says | The source prints | Result |
|---|---|---|---|
| 2.1 | "assassinated at a mosque in Jerusalem around noon on 20 July" | FRUS 1951 V d561 (Drew, Amman, 20 July 1951, 1 p.m., telegram 26) footnote on telegram 25: "King Abdullah assassinated about noon July 20, at mosque Jerusalem." | match. The footnote quotes a telegram that is not printed; no time zone is given. |
| 2.2 | "a quarter of a mile from the Jewish quarters of the city" | JTA bulletin of Monday 23 July 1951, p. 1, dateline "HAIFA, July 22. (JTA)": "Drawing attention to the fact that Abdullah was assassinated only a quarter of a mile from the Jewish quarters of Jerusalem, the Foreign Minister said that the killing was 'not a simple murder, but a political assassination affecting the central authority' of the Jordan Government." | match. The "quarter of a mile" is JTA's narration of what Sharett pointed out, with no quote marks. The Hebrew "כ-400 מטר" is the owner-approved conversion (0.25 x 1,609.344 m = 402 m). |
| 2.3 | "His direct talks with Israel, among them his offer of a five-year non-aggression pact, had not produced a signed agreement" | d370: "both sides initialled 'agreed set of principles'", to be "finally signed at meeting few days after 28th". d382: impasse on 28 February. d399: adjourned on 7 March, no signature. JTA 23 Jul 1951, p. 2: "nothing concrete emerged". | match for "not signed": nothing opened shows a signature. Two readings of "nothing concrete" are in the sources: see 2.4 and row 2.10. |
| 2.4 | "and with his death they are over" | **Not covered by any source.** d399 shows the talks adjourned, not ended, on 7 March 1950, with Jordan's note saying it "accepts King's plan as basis for settlement" and hoping to resume. FRUS 1951 V d563 says Israel "loses by Abdullah's death". The JTA bulletin of 23 July 1951 quotes the Herut leader: "Abdullah's disappearance from the political scene has prevented the conclusion of a peace treaty consecrating Israel's forfeit of Jerusalem, Hebron and other places sacred to the Jewish people". | not covered. The sentence is the owner's framing; the sources support "no agreement was reached" and, in 1951, show others reading the death as an end to a possible treaty. The last meeting read here (d399) is March 1950, so the sources opened do not show what happened to the talks between then and July 1951. Added 2026-10-27: d385 (Amman, 5 March 1950), which was opened, reports a Jordanian Cabinet member's admiration for the King's "decision defer Israeli negotiations" and that the informant "referred to them as indefinitely postponed", adding "Presumably they will not in any case be resumed until after forthcoming elections". That supports "postponed, no agreement" as of March 1950; it does not mention the King's death, so it does not by itself cover "with his death they are over". |
| 2.5 | "A handwritten line on a US State Department memorandum of that day called him the most cooperative of the Arab leaders toward Israel" | d563, "Memorandum by ... McGhee to the Secretary of State, [Washington,] July 20, 1951", in the Israel paragraph: a footnote reads "At this point in the source text, the following sentence is inserted, presumably in McGhee's handwriting: 'Of all the Arab leaders, Abdullah was the most cooperative toward Israel.'" | match. Keep the attribution as printed: inserted, "presumably" in McGhee's hand. **Not covered:** when the line was written; the memorandum is dated 20 July, the insertion is not. |
| 2.6 | "{foreign_minister} called the killing a political assassination" | JTA (above): "not a simple murder, but a political assassination affecting the central authority" of the Jordan Government, in quote marks. | match |
| 2.7 | "and said Israel must increase its manpower and its defensive strength" | JTA: "he insisted, Israel must increase its manpower and its defensive strength." | match. JTA's paraphrase, not a direct quotation; the card uses no quote marks. |
| 2.8 | the Foreign Minister's remarks sit under the card's date of 20 July (`from: 1951-07-20`) | The rally was on **22 July 1951** (dateline "HAIFA, July 22"), two days after the killing; the bulletin is dated 23 July. | **conflict on the date.** The card states the reaction in one text with the 20 July event. The draft's source comment says "JTA, 23 July 1951" and should say dateline 22 July. The card text does not say when he spoke, so no wording change is forced; a person may want "two days later" or to leave it. |
| 2.9 | no assassin and no motive named | d563: "The inconclusive information available causes us to believe the assassination may have been inspired by the ex-Mufti of Jerusalem" and "more likely to have been the result of a vendetta". JTA, quoting Davar: "Abdullah's assassin was identified as a supporter of the ex-Mufti of Jerusalem". | match (the card names none; the sources do offer a guess, which the card rightly leaves out). |

### The comments in the draft file and the other sources it cites

| # | Draft says | The source prints | Result |
|---|---|---|---|
| 2.10 | JTA, 23 July 1951 (same bulletin), summarising the New York Times: "nothing concrete emerged" and "opposed by Abdullah's political leaders" | Bulletin p. 2, dateline "NEW YORK, July 22. (JTA)", sub-head "Details of Abdullah's Peace Talks with Israel Revealed": "He then proposed a five-year nonaggression pact between Amman and Tel Aviv ... Abdullah's political leaders objected to this formula. Conversations ... continued, but nothing concrete emerged. Politically, the project was too explosive." | **conflict in substance**, match on the words: the quoted words are as printed, but it conflicts with d370: the New York Times account (a press summary of unnamed reporting) leaves out the 24 February initialling of an "agreed set of principles" that the US Ambassador recorded. The card's "had not produced a signed agreement" is true under both. Added 2026-10-27: on the page image the printed words are "political Leaders" with a capital L (agent viewed the cropped page image of the 23 July 1951 bulletin, page 2, 2026-10-27); the quoted "political leaders" above is lower case. |
| 2.11 | "Foreign Minister ... 'not a simple murder, but a political assassination'" (comment) | as 2.2 and 2.6 | match |
| 2.12 | the card calls the US estimate "inconclusive" (comment) | as 2.9 | match |
| 2.13 | d561 "Drew (Amman), 20 July 1951" | printed "Amman, July 20, 1951, 1 p.m." | match |
| 2.14 | d382, 3 March 1950: "the talks deadlocked on 28 February 1950" | as 1.15 | match |
| 2.15 | tags `[security, terror]`, as `bernadotte_assassination` | `content/events/bernadotte_assassination.yaml` line 12: `tags: [security, terror]`. | match (repo); whether to tag is the maintainer's call, as the earlier note says |

## The 13 items in the draft's "What a person must check"

| Item | Answer from this read |
|---|---|
| 1 | Done as an agent's read, in this note. A person's check, with their own date, is still needed. |
| 2 | "February 1" is printed in d364 as the draft says (row 1.11). |
| 3 | d364 says the agreement was "between Shiloah and Samir and King". "Israel's negotiators" is fair for Shiloah; the card could say "the King agreed with Israel's envoy" (Shiloah) if a person wants it exact. Row 1.2. |
| 4 | Confirmed: d364 "Free intercourse between two countries with free zone at Haifa"; d370 "grant of free zones for Jordan in Haifa". Row 1.5. |
| 5 | Confirmed: d370 has "agreed set of principles" initialled on 24 February and a footnote calling it a "nonaggression pact" initialled; the New York Times summary in JTA says "nothing concrete emerged". "Not signed" holds under all of them; "with his death they are over" is not in any source (row 2.4). |
| 6 | Confirmed: "presumably in McGhee's handwriting", undated (row 2.5). |
| 7 | Confirmed: only "not a simple murder, but a political assassination affecting the central authority" is in quote marks; the manpower line is JTA's paraphrase; "a quarter of a mile" is JTA's narration. Add: the rally was 22 July (row 2.8). |
| 8 | The tags match the precedent (row 2.15). Not a source question. |
| 9 | Design values; not checkable against a source. |
| 10 | Not a source question. Not checked here (no build was run). |
| 11 | Not covered. Nothing in the documents read mentions the union of 24 April 1950; that is [candidate-events-1948-51.md](candidate-events-1948-51.md) section 2c. |
| 12 | The sources agree on the dates in the draft: meeting 17 February (d364), announcement 24 February (d370). Nothing here argues for moving the owner's 15 February window. |
| 13 | Still true: the Knesset protocols and the cabinet's own minute of 24 February 1950 were not reached (row 1.12). |

## Conflicts that need a specific decision

1. **Dates of the two JTA items** (rows 1.16 and 2.8). The draft's comments cite "JTA, 3 March 1950" and "JTA, 23 July 1951"; the datelines are **2 March** (Geneva) and **22 July** (Haifa; New York) by the 24 Oct dating rule. A comment fix only; no card text carries those dates.
2. **"With his death they are over"** (row 2.4) is not in any source opened, and d399 shows the talks adjourned, not ended, on 7 March 1950. The owner's ruling (#784, 18 Oct) says the card "closes the talks"; that is a game-design statement. Proposal for the person who checks: keep the sentence as the owner's framing and mark it in the card's comments as "owner ruling, not a source sentence", rather than change the text.
3. **Initialled principles versus "nothing concrete"** (rows 1.13 and 2.10). The card's "had not produced a signed agreement" is safe under both; a stronger sentence would not be.

## Not reached, and gaps

- `history.state.gov`: 403 on all six documents; the rendered pages were not seen (the text is from the publisher's GitHub TEI, which is the same text). A person opening the web pages should confirm the footnote text and the page's own "February 1".
- The Knesset protocols for February 1950 and the cabinet's own minute of 24 February 1950: not reached.
- What happened to the talks after 9 March 1950 (d399) up to July 1951: not re-read here; [candidate-events-1948-51.md](candidate-events-1948-51.md) section 2a cites FRUS 1950 V d434 and d456 for it, and those two were not re-opened in this pass.
- The Commons photograph and the Hebrew strings in the draft comment: out of scope (a language read by a native speaker is recorded on #807).
- The 22 Oct comment's wish for "the page/paragraph and their own date" per source: a person's date is still to be added.

## What a person must check next

1. Open `history.state.gov` d364, d370, d382, d399, d563 and compare the footnotes quoted in rows 1.11, 1.13 and 2.5 with the rendered pages.
2. Look at the page image of JTA 3 March 1950 (the smudged "delighte"), and confirm the datelines 2 March and 22 July.
3. Decide row 2.4 and conflict 3 above, then add `checked:` dates to the draft files in the PR that adds the cards.
