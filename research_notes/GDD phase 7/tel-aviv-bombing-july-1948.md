# The Egyptian bombing of Tel Aviv in July 1948: which aircraft, and who fought them (research note, 2026-11-05)

Status: **AI draft, not checked by a person.** Research only: it backs a looks-only map change (the Dakotas and Tel Aviv's
air defence on the 15 July 1948 turn) and changes nothing in `content/` or `sim/`. This note was read and written by an AI
agent. A person should open each source and confirm what it supports; `reviewed_by` and `reviewed_on` stay empty.

## Question

The owner asked: "we will need an animation of the 101 attacking the dakotas", then chose "(a) Add the Dakotas to the 15
July 1948 turn, the second bombing period, with 101 Squadron's fighters attacking them over Tel Aviv. Both sides are
historically present that turn." Two facts need a source before that is drawn: which aircraft bombed Tel Aviv in the second
bombing period, 8 to 17 July 1948, and whether 101 Squadron intercepted them over Tel Aviv then.

## Short answer

- **The second bombing period, 8 to 17 July 1948, is sourced** (he.wiki, row S1). The game's monthly turn of 15 July 1948
  falls inside it.
- **Dakotas in that period: [S], and [C] inside one article.** en.wiki "1948 Arab–Israeli War" (citing Morris 2008, pp.
  294-295) has Egyptian raids on Tel Aviv "by Dakota aircraft accompanied by a Spitfire fighter escort" after the Israeli B-17
  raid of 15 July, and "One Egyptian Dakota was lost" (row S2). The same article says, in its 3 June paragraph, that there
  were "no more raids by bombers for the rest of the war" (row S3). he.wiki's 13 July paragraph has two bombers and two
  fighters but does not name the bombers' type (row S4). Morris itself was not opened (see "Not read").
- **One Egyptian Dakota shot down by anti-aircraft fire over Tel Aviv on 17 July 1948: [S]**, JTA 19 July 1948, already in
  the repo (row S5).
- **101 Squadron intercepting the Dakotas over Tel Aviv in July: [GAP].** No source read says so. en.wiki says only that
  Israeli fighters "on occasion intercepted Egyptian aircraft" in the Ten Days, with no place, day or type (row S6). 101
  Squadron was flying the Avia S-199 then, and was the IAF's fighter squadron (rows S7, S8), so the type is right for the
  turn; the interception itself is not sourced. The only sourced 101 fight with the Dakotas is Modi Alon's of 3 June 1948
  (row S9).
- **A 101 Squadron shoot-down of a Dakota in July: none found.** The only July Dakota loss sourced is the anti-aircraft
  kill of 17 July (row S5); "One Egyptian Dakota was lost" (row S2) does not say how.

## What was built from this (owner's answer "do 1+2")

The owner was offered two options and answered "do 1+2":

1. On the 15 July 1948 turn the Dakotas bomb Tel Aviv and 101 Squadron's S-199s attack them **with hits but no 101
   shoot-down**, shown as an illustration of that turn's air defence (rows S6 to S8; the interception itself is [GAP]).
2. **One Dakota goes down to anti-aircraft fire**, the sourced 17 July 1948 event (row S5): flak bursts near Tel Aviv, then
   the Dakota smoking and falling out of the loop. Where it fell is not sourced; the fall's path is a design value.

## Conventions

- Every source below was opened on **2026-11-05** unless the row says otherwise.
- Tags as in `research_notes/GDD phase 7/squadron-101-dates-1948.md`: **[V]** two or more independent sources agree, or one
  authoritative primary source; **[S]** one source, or several that copy one another; **[C]** sources disagree, every
  version kept; **[GAP]** nothing reachable settles it.
- Wikipedia is a pointer, read at the revision given (`oldid`). Quotation marks are verbatim.

## Sources

| Short name | What it is | Where read |
| --- | --- | --- |
| he.wiki bombing | הפצצת תל אביב במלחמת העצמאות | https://he.wikipedia.org/w/index.php?oldid=43856594 |
| en.wiki war | 1948 Arab–Israeli War | https://en.wikipedia.org/w/index.php?oldid=1375001749 |
| he.wiki Tel Aviv | תל אביב במלחמת העצמאות | https://he.wikipedia.org/w/index.php?oldid=43556405 |
| JTA 19 July 1948 | JTA Daily News Bulletin, as read for the repo note | `research_notes/GDD phase 7/egyptian-air-bases-and-raids-1948-1872.md`, row R11 (read 2026-10-28; not re-opened here) |
| he.wiki 101 | טייסת 101 | https://he.wikipedia.org/w/index.php?oldid=44131461 |
| en.wiki 101 | 101 Squadron (Israel) | https://en.wikipedia.org/w/index.php?oldid=1379560417 |
| 101squadron.com | Fan site, the S-199 page | https://101squadron.com/101/s-199s.html |
| Repo note 101 | 101 Squadron's 1948 dates | `research_notes/GDD phase 7/squadron-101-dates-1948.md` |

## Findings

| Row | Claim | Source and exact words | Tag |
| --- | --- | --- | --- |
| S1 | The second bombing period ran 8 to 17 July 1948; the last bombing of Tel Aviv was on 17 July. | he.wiki bombing, section "תקופת ההפצצות השנייה": "תקופת ההפצצות השנייה על תל אביב התרחשה במהלך קרבות עשרת הימים (8 עד 17 ביולי)"; "ב-17 ביולי הופצצה תל אביב בפעם האחרונה בשתי גיחות מצריות" (no footnote on either sentence) | [S] |
| S2 | After the Israeli B-17 raid of 15 July, Egypt raided Tel Aviv with Dakotas escorted by a Spitfire; one Dakota was lost. | en.wiki war, section on the Ten Days' air operations, footnoted to Morris, 2008, pp. 294-295: "The Egyptians responded with raids on Tel Aviv by Dakota aircraft accompanied by a Spitfire fighter escort, killing at least 15 people. One Egyptian Dakota was lost." | [S] (a pointer to Morris, not read) |
| S3 | No bomber raids on Tel Aviv after 3 June. | en.wiki war, 3 June paragraph: "Although Tel Aviv would see additional raids by fighter aircraft, there would be no more raids by bombers for the rest of the war." he.wiki Tel Aviv calls the 3 June raid "המתקפה האווירית האחרונה של הצבא המצרי על תל אביב" (footnote: "תולדות מלחמת הקוממיות, 232"), then adds "גם בעת קרבות עשרת הימים הופצצה העיר מספר פעמים". | [C] with S2 and S4 |
| S4 | On 13 July two bombers and two fighters bombed Tel Aviv; the bombers' type is not named. The usual Egyptian sortie was two bombers, usually Dakotas, with one or two Spitfires. | he.wiki bombing: "ביום שלישי, 13 ביולי, ספגה תל אביב הפצצה כבדה על ידי שני מפציצים ושני מטוסי קרב מצרים"; and, for the war as a whole, "שיטת התקיפה המצרית הייתה בדרך כלל גיחה של שני מטוסי הפצצה, בדרך כלל מטוסי דקוטה, שלוו במטוס קרב אחד או שניים מדגם ספיטפייר." | [S] |
| S5 | On 17 July Tel Aviv had six Egyptian air raids; one Egyptian Dakota was shot down by anti-aircraft fire. | JTA 19 July 1948, bulletin image PDF p. 3, dateline "TEL AVIV, July 18. (JTA)", as recorded in row R11 of the repo note: "Six Jews were killed and at least 20 wounded yesterday when Tel Aviv was subjected to six Egyptian air raids"; the same row records the Dakota "shot down by anti-aircraft fire". | [S]; agent-read 2026-10-28, a person's check pending |
| S6 | Israeli fighters sometimes intercepted Egyptian aircraft in the Ten Days. | en.wiki war, same section as S2 (same footnote): "In addition, Israeli Air Force fighter aircraft flew ground support missions and on occasion intercepted Egyptian aircraft." No place, day or aircraft type. | [S] for "on occasion"; [GAP] for an interception over Tel Aviv |
| S7 | 101 Squadron flew the Avia S-199 in July 1948, few of them serviceable. | 101squadron.com: "By the time fighting intensified on July 9, 101 Squadron had achieved a 1:1 pilot to plane ratio on paper, although the number of serviceable S-199s was almost always limited to four or fewer." Repo note 101: the S-199 from 20 May to mid-October 1948. | [S] |
| S8 | On 8 July Modi Alon, flying from Herzliya, shot down an Egyptian Spitfire on the way back from a strike in the Negev, not a Dakota and not over Tel Aviv. | he.wiki 101: "ב-8 ביולי המריא אלון משדה התעופה בהרצליה כדי לתקוף טור משוריין מצרי בביר עסלוג' שבנגב. בדרכו חזרה הבחין המבנה שבו טס בשני מטוסי ספיטפייר מצריים, ואלון הפיל אחד מהם." | [S] |
| S9 | The only sourced 101 Squadron fight with Egyptian Dakotas: 3 June 1948, Modi Alon shot down two over Tel Aviv. | en.wiki 101: "Modi Alon, after taking off from Herzliya Airport, shot down a pair of Royal Egyptian Air Force C-47s which had just bombed Tel Aviv." he.wiki bombing: "מטוס קרב ישראלי שהוטס על ידי הטייס מודי אלון המריא והצליח להפיל שני מטוסי דקוטה מצרים ששימשו כמפציצים." | [V] (two articles, each footnoted; already used by `SeaAirCollection.Dakota.cs`) |

## Disagreements kept side by side

| Point | Version 1 | Version 2 |
| --- | --- | --- |
| Bombers over Tel Aviv after 3 June | No more bomber raids (en.wiki war, 3 June paragraph; he.wiki Tel Aviv, "the last air attack", citing תולדות מלחמת הקוממיות p. 232) | Dakota raids after 15 July, one Dakota lost (en.wiki war, Ten Days section, citing Morris 2008); "two bombers" on 13 July (he.wiki bombing); a Dakota shot down by anti-aircraft fire on 17 July (JTA 19 July 1948) |

## Not read

- **Morris, *1948: A History of the First Arab-Israeli War* (2008), pp. 294-295**: the source of row S2. archive.org holds
  it only as a lending copy (`1948historyoffir00morr`); it was not opened. A person should read those two pages.
- **The Davar article of 14 July 1948** that he.wiki cites for 13 July ("התקפת טרור קשה על תל אביב"): the NLI newspaper
  site answered with a browser check and could not be read from this machine.
- **Scanned aviation histories** (Cull et al., *Spitfires over Israel*; the Helion Middle East@War volumes): third-party
  scans were not downloaded. A person with the books could settle rows S3 and S6.
