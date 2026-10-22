# Phase 6 displacement follow-up cards: sourced draft material

**DRAFT. Agent-researched (Claude Code, for Beri), 22 October 2026. A person must check every source line below, and
the owner must review every line for sensitivity, before any of it goes into `content/`.** Nothing here is a card,
and nothing here decides an owner question. This note is the research slice of #801 (Phase 6 plan, PR 9). The content
PR waits on #800 (Deport, draft #1327), which sets when a card fires.

## What the cards have to do

- **Q-CARD-1 (c)** (owner, 18 October 2026, "Accept all 16 drafts", #783): "a card stating the in-game consequence,
  with one sourced line of history". The draft the owner accepted
  ([#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6231809370), Q-CARD-1, "Needs
  research") asks for "the one sourced line for each target, from Israeli official or mainstream Israeli
  historiography, one value per fact".
- **Q-CARD-2 (a)**: `ten_days` stays a military card. Only the new cards state displacement.
- **Q-CARD-3 (a)**: no image on any `displacement` card. Deport never gets a video (plan, constraint 10).
- **Q-SENS-1b (a)**: abandoned Arab housing is not named in Phase 6 text.
- **D-F1**: cards "state displacement factually, from sourced mainstream Israeli historiography (CONTENT_POLICY §1)".
- **Constraint 11** and `docs/WRITING_EVENTS.md`: every place, date and number sourced with a `checked:` date; no
  invented quotes; office placeholders, no names in card text; plain wording with no euphemism, melodrama, softening
  or loaded adjectives; dates as "15 May 1948". The tag is `displacement`; no option may raise any resource or score
  (R-EFX-007), and the release gate refuses the card until it is `sensitivity-reviewed` (R-GATE-001).
- **Targets** (`content/settlement_targets.yaml`, owner rulings of 19 and 21 October 2026, #793): `acre`, `nazareth`,
  `lod`, `ramla` and `beersheba` can be captured in play; `jaffa` only after an alternate-timeline card returns it to
  Arab control.
- **Residents' outcome on capture** (owner, 18 October 2026, #793): Nazareth and Acre "stayed"; Lydda, Ramle and the
  last residents of Beersheba take the default "fled" ("Default 'fled'; Morris isn't enough"); the official Israeli
  sources consulted then did not describe the residents.

## Sources and how they were read

The standard the owner accepted for these cards is Q-CARD-1's: "from Israeli official or mainstream Israeli
historiography" (#783, quoted above). This note reads Israeli official records, sources the repo already cites, and
he.wiki. Whether he.wiki meets that standard for a line about the residents is not settled; it is the owner's question 2
below. The owner's he.wiki ruling of 21 October 2026
([#1219](https://github.com/eyalgolan/statecraft-israel/issues/1219#issuecomment-6318062081)) does not settle it: it is
about sources for Hebrew names only ("on item 3 (the five villages without a Hebrew name): besides he.wiki,
`name_he_sources` may cite moreshet-map.org.il and the Israel Antiquities Authority, but not Zochrot").

No line here quotes Morris directly, but several of the he.wiki sentences behind Line B are themselves footnoted to
Morris, *The Birth of the Palestinian Refugee Problem* (Hebrew edition, "לידתה של בעיית הפליטים הפלסטינים
1947–1949"), in the revisions cited. The table below gives, line by line, what each he.wiki sentence cites, so the owner
can weigh question 2 against the "Morris isn't enough" ruling (#793). Where he.wiki reports Morris's view in its own
text, that is said under the target.

| line | he.wiki sentence | footnote in the cited revision |
|---|---|---|
| Acre A | HW-ACRE, "ב־18 במאי לפנות בוקר חתמו..." | Mustafa Abbasi (מוסטפא עבאסי), "נפילת עכו במלחמת העצמאות", palestine-studies.org (in English); and the Haganah heritage site, "כיבוש עכו - 16-17/5/48" (hahagana.org.il) |
| Acre B | HW-ACRE, "בימים הראשונים לאחר הכיבוש..." | Morris, p. 153 (the paragraph's one footnote, at its end) |
| Nazareth B | HW-NAZ, "למרות הקרבות..." | Morris, p. 271 (the paragraph's one footnote, at its end) |
| Nazareth B (surrender) | HW-DEKEL, "בשעה 22:00 נחתם הסכם הכניעה..." | none in that paragraph |
| Lydda B | HW-LOD, "לפני הלחימה, במהלכה ואחריה..." | Tom Segev, *Haaretz*, 27 November 2009; Ofer Regev, *Mida*, 8 July 2015 |
| Ramle B | HW-RAMLA, "לאחר כניעת העיר, בעקבות תקריות ירי בלוד..." | Yoav Gelber, *Komemiyut ve-Nakba* (Dvir, 2004), p. 246 |
| Ramle, context (the buses to al-Qubab) | HW-RAMLA, "תושבי רמלה הוסעו..." | Morris, p. 280 |
| Ramle, [C] (the government did not know; most decided to leave) | HW-RAMLA, "ממשלת ישראל לא ידעה...", "אלא שבאותו לילה..." | Morris, pp. 278–279 |
| Beersheba B and B' | HW-B7, "רוב תושבי העיר נמלטו מזרחה..." | Morris, p. 294 |
| Jaffa | HW-HAMETZ, HW-JAFFA | none on these sentences |

Every source below was opened on 22 October 2026; that is the `checked:` date the content PR would carry, after a
person's own check. he.wiki pages were read through the MediaWiki API as plain text; the
revision read is given as a permanent link, because the pages change. Supporting sentences are copied verbatim from
that revision.

| key | source | revision or note |
|---|---|---|
| HW-DANI | he.wiki, [מבצע דני](https://he.wikipedia.org/wiki/מבצע_דני) | [oldid 43258522](https://he.wikipedia.org/w/index.php?oldid=43258522) |
| HW-LOD | he.wiki, [לוד](https://he.wikipedia.org/wiki/לוד) | [oldid 43942324](https://he.wikipedia.org/w/index.php?oldid=43942324) |
| HW-RAMLA | he.wiki, [רמלה](https://he.wikipedia.org/wiki/רמלה) | [oldid 44080267](https://he.wikipedia.org/w/index.php?oldid=44080267) |
| HW-DEKEL | he.wiki, [מבצע דקל](https://he.wikipedia.org/wiki/מבצע_דקל) | [oldid 42975037](https://he.wikipedia.org/w/index.php?oldid=42975037) |
| HW-NAZ | he.wiki, [נצרת](https://he.wikipedia.org/wiki/נצרת) | [oldid 44022843](https://he.wikipedia.org/w/index.php?oldid=44022843) |
| HW-ACRE | he.wiki, [המצור על עכו (1948)](https://he.wikipedia.org/wiki/המצור_על_עכו_(1948)) | [oldid 42972755](https://he.wikipedia.org/w/index.php?oldid=42972755) |
| HW-BENAMI | he.wiki, [מבצע בן עמי](https://he.wikipedia.org/wiki/מבצע_בן_עמי) | [oldid 42972754](https://he.wikipedia.org/w/index.php?oldid=42972754) |
| HW-B7 | he.wiki, [באר שבע](https://he.wikipedia.org/wiki/באר_שבע) | [oldid 44069386](https://he.wikipedia.org/w/index.php?oldid=44069386) |
| HW-HAMETZ | he.wiki, [מבצע חמץ](https://he.wikipedia.org/wiki/מבצע_חמץ) | [oldid 43483130](https://he.wikipedia.org/w/index.php?oldid=43483130) |
| HW-JAFFA | he.wiki, [יפו](https://he.wikipedia.org/wiki/יפו) | [oldid 43979954](https://he.wikipedia.org/w/index.php?oldid=43979954) |
| IDF-DANI | IDF account, "Operation Danny", republished by the Jewish Virtual Library, [page](https://jewishvirtuallibrary.org/jsource/History/Danny.html) | already cited in `content/settlement_targets.yaml` |
| IDF-DEKEL | IDF account, "Operation Dekel (July 9-18, 1948)", Jewish Virtual Library, [page](https://jewishvirtuallibrary.org/jsource/History/Dekel.html) | the page credits "Israel Defense Forces" |
| IDF-YOAV | IDF account, "Operation Yoav", Jewish Virtual Library, [page](https://jewishvirtuallibrary.org/jsource/History/Yoav.html) | already cited in `content/settlement_targets.yaml` |
| ARCH-RABIN | IDF and Defence Establishment Archives, Yitzhak Rabin exhibition, [page](https://archives.mod.gov.il/sites/English/Exhibitions/Pages/Yitzhak-Rabin.aspx) | already cited in `content/settlement_targets.yaml` |

Not usable from this box: the Knesset summary
([PDF](https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf), cited in `settlement_targets.yaml`)
answered with an HTML page instead of the PDF on 22 October 2026 (twice), so no sentence from it is quoted here. Its
content is carried in `research_notes/GDD phase 6/settlement-targets.md` (read 18 October 2026), which records that it
"says nothing about any town's residents". The PR's reviewer reports
([#1332, 22 October 2026](https://github.com/eyalgolan/statecraft-israel/pull/1332#issuecomment-6338613561)) that it
does load as a PDF from another machine (9 pages, readable text), with official lines for Nazareth (Dekel), Ramla and
Lod (Dani) and Beersheba (21 October), and that its table lists Ben-Ami as the Western Galilee, 14-19 May 1948,
without naming Acre. That is the reviewer's report, not read here; a person quotes from the PDF. moreshet-map.org.il
and the IAA were not needed for any line below; no line rests on them.

## How each target is laid out

For each target: the card frame (the in-game consequence), then the candidate history lines. **Line A** is military
only, from an Israeli official account. **Line B** states what happened to the residents, from he.wiki. Under D-F1 the
card must be able to state displacement, so A alone may not meet it; that choice, and the wording of B, is the
owner's. Every line has its source key and the verbatim supporting sentence. English lines are drafts; Hebrew card
text is not drafted here (it goes on #564 for a native read once the English is approved).

### The card frame, the same for every target

The in-game half of the card restates what the sim does, in the words
[#1327](https://github.com/eyalgolan/statecraft-israel/pull/1327) proposes, in its PR description, for the cause line ("{place}:
Deport. Its residents are displaced and the place is depopulated"), and the costs the dialog shows. It needs no
source, because it describes play, not history. It must not say or imply that the player's Deport is what happened in
1948 (Q-CARD-1 context: a Deport in play "may happen at a place, on a date, or in a way that differs from the place's
history"). Draft shape, for the owner to edit:

> {place}'s residents have been displaced, and the place is depopulated. No city project or corridor can be started
> there. In 1948: *[the place's history line]*

The word is "displaced" (owner, 22 October 2026, on
[#1327](https://github.com/eyalgolan/statecraft-israel/pull/1327#issuecomment-6338595230)). The second sentence follows
#1327's draft rule R-CTRL-020 (at 4d2bd462): no city project may be started on the place's hex and no corridor segment
through it (new starts only), the player's units cannot enter it, and it is never captured again. If #1327's rule
changes, this sentence follows it. The values and the permanent costs come from #1327 and the dialog (#802), not from
this note.

## Acre (`acre`)

**Game outcome on capture:** "stayed" (owner, 18 October 2026).

- **Line A (official):** none found. The IDF account pages reachable from here have no Ben-Ami page (JVL
  `Ben-Ami.html` returned HTTP 410), and the Knesset PDF was not reachable; per the reviewer's report (above), its table
  lists Ben-Ami without naming Acre. [GAP]
- **Line A (he.wiki, military):** "Acre surrendered on 18 May 1948."
  - HW-ACRE: "ב־18 במאי לפנות בוקר חתמו חברי מועצת העיר על כתב כניעה, הורו על הפסקת הלחימה ופתחו את שערי העיר
    העתיקה."
- **Line B (residents):** "In the first days after Acre surrendered on 18 May 1948 (date from Line A), residents kept leaving for
  Lebanon and central Galilee, but no expulsion orders were issued and no pressure was put on them to leave."
  - HW-ACRE: "בימים הראשונים לאחר הכיבוש, נמשכה יציאת התושבים ללבנון ולגליל המרכזי, אולם לא הוצאו פקודות גירוש ולא
    הופעל לחץ על התושבים לעזוב."
  - Context, not in the line: HW-ACRE on the flight before the capture, "ב-5 במאי פרצה בעיר מגפת טיפוס, שהייתה גורם
    נוסף לבריחת התושבים מן העיר.", and on those who remained, "בעיר נשארו 5,000 עד 6,000 איש, רובם תושבים מקומיים."
- **[C]** HW-ACRE says those who remained were "mostly local residents" (our translation of "רובם תושבים מקומיים");
  Morris, as carried in
  `settlement-targets.md`, says many were not local. The owner already ruled Acre "stayed" on that record. Line B
  carries no figure; whether the 5,000 to 6,000 figure appears at all is the owner's pick (Q-CAP-2 (b) shows no count
  in play).
- **Note:** HW-BENAMI says of the wider operation area (Western Galilee, not the town): "חטיבת כרמלי לא הצטוותה לגרש
  את האוכלוסייה הערבית האזרחית מן האזור, אך בעקבות המבצע נמלטו ממנו כמעט כל תושביו הערבים." It is about the
  villages of the region, so it is not proposed for the Acre card.

## Nazareth (`nazareth`)

**Game outcome on capture:** "stayed" (owner, 18 October 2026).

- **Line A (official, military):** "Nazareth surrendered on 16 July 1948, in Operation Dekel."
  - IDF-DEKEL: "The city surrendered on the eve of 16 July."
  - [C] "the eve of 16 July" can be read as the evening of 16 July or of 15 July. HW-DEKEL has the forces set out on
    the night of 15-16 July ("בליל 15–16 ביולי 1948 יצאו כוחות חטיבה 7 וגדוד 21 משפרעם לעבר נצרת.") and the surrender
    at 22:00 after the entry at 17:40, which fits 16 July; `settlement-targets.md` carries 16 July. A person confirms.
- **Line B (residents):** "When Nazareth was taken on 16 July 1948, in Operation Dekel, its residents stayed." (date
  [C], see Line A)
  - HW-NAZ: "למרות הקרבות שניטשו בעיר ובסביבתה בעת כיבושה, נשארו תושבי העיר במקומותיהם."
  - The day is IDF-DEKEL's (above). HW-DEKEL, section "כיבוש נצרת", agrees on the evening surrender without restating
    the day: "בשעה 22:00 נחתם הסכם הכניעה בין נציגי התושבים לנציגי צה"ל."
  - HW-DEKEL, on the order behind it: "בנוסף, דוד בן-גוריון הורה במפורש להניח לתושבים להישאר." The card text names
    no person (WRITING_EVENTS). If the owner wants this fact on the card, it reads through an office placeholder,
    which a person picks, and it is not a quote.
- **Note:** Nazareth is the one target where the record is that the residents were not displaced. A Deport card here
  would state, in its history line, that this did not happen in 1948. Whether that line belongs on a Deport card at
  all is an owner question (below).

## Lydda (`lod`)

**Game outcome on capture:** default "fled" (owner, 18 October 2026: "Default 'fled'; Morris isn't enough").

- **Line A (official, military):** "In July 1948 Operation Dani took the cities of Lydda and Ramle."
  - ARCH-RABIN: "In July 1948, with the renewal of warfare following the first truce, Rabin was appointed operations
    officer in Operation Danny, capturing the cities of Lydda and Ramle, ..."
  - IDF-DANI: "The first phase was to occupy the areas of Lod and Ramle; ..." It gives no day for Lydda.
  - Day, HW-DANI: "בלילה שבין 12 ל-13 ביולי חמקו הלוחמים שבבניין המשטרה והושלם כיבוש לוד." [C] with 11 July (the town
    entered) in `settlement-targets.md`.
- **Line B (residents):** "Before, during and after the fighting for Lydda in July 1948, in Operation Dani, almost
  all of its Arab residents were expelled under threat, on an order from the operation's operations officer."
  - HW-LOD: "לפני הלחימה, במהלכה ואחריה גורשו באיומים כמעט כל התושבים הערבים מהעיר, לפי פקודה שנתן קצין המבצעים של
    מבצע דני, יצחק רבין:" (the sentence goes on to quote the order; the quote is not proposed for the card).
  - Context, not in the line: HW-DANI, "בעיר נותרו רק כמה מאות תושבים, רובם נוצרים, שבחרו להישאר."
- **[C] in the same source.** HW-LOD records the historians' disagreement right after that sentence: Morris's view
  that the attacks were meant from the start to make the civilians flee; Alon Kadish and Avraham Sela's view that the
  IDF "allowed and even encouraged" (our translation of "איפשר צה"ל ואף עודד") flight during the fighting but did not plan to expel all the residents, and that
  pressure to leave was decided only after the shooting of 12 July; Yoav Gelber's view that the evidence does not show
  a policy of deliberate expulsion; and Mordechai Bar-On's criticism of Morris. Verbatim, for Kadish and Sela: "רק
  לאחר תקריות הירי ב־12 ביולי "הוחלט במטה 'דני' שיש להפעיל לחץ על האוכלוסייה לעזוב"." HW-DANI, for its part, calls
  the people who left "המפונים" and records the instruction sent to the commander that night. Line B uses the source's
  verb ("גורשו באיומים"; our translation, "expelled under threat") and states the order, not the intent behind it, on which the
  historians differ. The office ("operations officer") stands in for the name; no person is named on the card.
- **Owner ruling to reconcile:** the game's "fled" default for Lydda rests on "Morris isn't enough" and on the official
  sources saying nothing about residents. HW-LOD is a different source from Morris, but it reports the same events and
  cites the same debate. Whether Line B may stand on he.wiki is the owner's call. If not, the card has Line A only, and
  D-F1's "state displacement" is not met for Lydda.
- **Not proposed:** HW-LOD and HW-DANI give casualty figures for 12 July and a count of people who left. No figure is
  proposed for any card (the issue does not ask for one; Q-CAP-2 (b); CONTENT_POLICY §1 shows no contested ranges).
  HW-LOD also reports documents revealed in 2026; not read, not used.

## Ramle (`ramla`)

**Game outcome on capture:** default "fled" (owner, 18 October 2026).

- **Line A (official, military):** "Ramle surrendered on 12 July 1948, in Operation Dani."
  - IDF-DANI: "On July 12, Kiryati units took Ramle, which surrendered after a brief engagement."
- **Line B (residents):** "After Ramle surrendered on 12 July 1948 (date from Line A), following shooting incidents
  in Lydda, Operation Dani's headquarters ordered its residents, except the young men, expelled east toward the Arab
  Legion's positions in the hills."
  - HW-RAMLA: "לאחר כניעת העיר, בעקבות תקריות ירי בלוד, הוצאה הוראה ממטה מבצע דני לגרש את תושבי רמלה, למעט הגברים
    הצעירים, מזרחה לכיוון עמדות הלגיון בהרים." (footnoted to Gelber, p. 246; a person may want to read that page
    directly)
  - Context, not in the line (one line of history, Q-CARD-1 (c); footnoted to Morris, p. 280): HW-RAMLA, "תושבי רמלה הוסעו באוטובוסים ומשאיות על ידי חיילי חטיבת קרייתי לאל קובאב (כיום כפר בן-נון) ומשם
    המשיכו ברגל."
  - Context, not in the line (a post-war figure, not a 12 July one): HW-RAMLA, "ברמלה נותרו לאחר אירועי מלחמת
    העצמאות כ-920 תושבים ערבים, רובם נוצרים."
- **[C] in the same source.** HW-RAMLA also records that the government did not know of the expulsion order, that
  instructions not to force women, the old and children to leave went out on the night of 12-13 July, and that by
  then most residents had decided to leave, for reasons it lists. Verbatim: "אלא שבאותו לילה כבר החליטו רוב התושבים
  לצאת מהעיר ולא להישאר תחת שלטון יהודי." And: "ממשלת ישראל לא ידעה על הוראות הגירוש." (both footnoted to Morris,
  pp. 278–279). Line B states the order, and the shooting incidents it followed, as the source does. Whether the card also says that most residents had decided to leave is the
  owner's call.
- **[C] place name.** HW-DANI gives al-Qubab as "כיום משמר איילון"; HW-RAMLA gives "כיום כפר בן-נון". Line B names
  no modern place, so the conflict does not reach the card.
- **Owner ruling to reconcile:** as for Lydda.

## Beersheba (`beersheba`)

**Game outcome on capture:** default "fled", including the last residents (owner, 18 October 2026).

- **Line A (official, military):** "Beersheba surrendered on the morning of 21 October 1948, in Operation Yoav."
  - IDF-YOAV: "After fierce fighting, the 500-strong Egyptian garrison broke and, by 09:00 hrs that morning,
    Beersheba - capital of the Negev had surrendered to Israeli forces." (The morning is that of 21 October: "At 4:00
    on the morning of 21 October, the IDF moved to capture Beersheba.")
- **Line B (residents):** "Most of Beersheba's residents fled east toward the Hebron hills, and a few days after the
  town was taken on 21 October 1948, the residents who remained were expelled to the Gaza Strip."
- **Line B' (keeps the ruling's "fled"):** "Most of Beersheba's residents fled east toward the Hebron hills." This
  is HW-B7's first clause only. Nothing read here says the last residents fled, so B' must stop there. HW-B7's
  sentence, behind both B and B', is footnoted to Morris, p. 294.
  - HW-B7: "רוב תושבי העיר נמלטו מזרחה, לעבר הרי חברון, ומספר ימים לאחר הכיבוש גורשו כמה מאות התושבים הנותרים
    לרצועת עזה."
- **Owner ruling to reconcile:** the ruling sets "fled" for the last residents too. HW-B7 says they were expelled
  ("גורשו"). A card that follows HW-B7 departs from the game's default for those few hundred; a card that keeps "fled"
  for them has no source here.
- **[C] time of day.** HW-B7's sentence just before reads "נכבשה העיר בשעות הצהריים של 21 באוקטובר 1948"; IDF-YOAV
  says the garrison had surrendered by 09:00. Line A uses "morning" from the IDF account; no line needs the hour.

## Jaffa (`jaffa`, what-if only)

**Game outcome:** Jaffa is a target only after an alternate-timeline card returns it to Arab control (owner,
18 October 2026, "Only in a what-if"). A Deport there is wholly counterfactual, so the history line can only say what
happened to the residents before Israel took the city.

- **Line A/B (residents before the surrender):** "Many of Jaffa's residents left after the Irgun attack, until the
  city surrendered on 13 May 1948."
  - HW-HAMETZ: "בעקבות התקפת האצ"ל, גברה מאוד הנטישה של תושבי יפו והכפרים, עד אשר העיר נכנעה ב-13 במאי 1948."
  - HW-JAFFA: "כך החלה הנטישה של תושבי יפו אשר נמשכה עד ל-13 במאי 1948 עם כניעת האוכלוסייה הערבית."
- **[C] date.** he.wiki gives 13 May (the surrender); `settlement-targets.md` carries Morris's 14 May for the Haganah's
  entry and the Knesset's "in May" with no day. The owner's #793 ruling already says "it surrendered on 13 May 1948".
- **[GAP]** Neither sentence gives a share of the residents, so the line says "many", from "גברה מאוד הנטישה" (the
  leaving grew greatly).

## Summary table

| target | game outcome | Line A (military) source | Line B (residents) source | open |
|---|---|---|---|---|
| `acre` | stayed | HW-ACRE (no official page reached) | HW-ACRE (Morris p. 153) | figure or no figure; local or not [C] |
| `nazareth` | stayed | IDF-DEKEL | HW-NAZ (Morris p. 271), HW-DEKEL | does a "they stayed" line belong on a Deport card; "eve of" [C] |
| `lod` | fled (default) | ARCH-RABIN, IDF-DANI; day HW-DANI | HW-LOD (Segev; Regev), HW-DANI | he.wiki vs "Morris isn't enough"; intent disputed [C]; capture day [C] |
| `ramla` | fled (default) | IDF-DANI | HW-RAMLA (Gelber p. 246) | he.wiki vs "Morris isn't enough"; which verb [C] |
| `beersheba` | fled (default) | IDF-YOAV | HW-B7 (Morris p. 294; B, or B' to keep "fled") | HW-B7 says the last residents were expelled |
| `jaffa` | what-if | none | HW-HAMETZ, HW-JAFFA | no share given [GAP]; 13 or 14 May [C] |

## Questions for the owner (none decided here)

1. **Which line goes on the card: A (military only), B (residents), or both?** D-F1 asks the cards to state
   displacement; A alone does not. Q-CARD-1 (c) says one line of history.
2. **Lydda, Ramle and Beersheba: may Line B stand on he.wiki?** The game's "fled" default rests on "Morris isn't
   enough" and on official sources that say nothing about residents. he.wiki says the residents of Lydda were
   "גורשו", gives an order from the operation's headquarters for Ramle, and says Beersheba's last residents were
   "גורשו", while recording, for Lydda and Ramle, the historians who dispute deliberate expulsion. he.wiki's own
   footnotes differ: Lydda's sentence cites Segev and Regev, Ramle's order cites Gelber, and Beersheba's sentence cites
   Morris (table under "Sources"). If he.wiki is not enough either, these three cards carry Line A only.
3. **The verb.** The drafts use the sources' own verbs: "expelled under threat" (Lydda; our translation of
   "גורשו באיומים"), an order to expel (Ramle),
   "fled" and "expelled" (Beersheba). Plain and not softened is the policy; the final word is the owner's
   sensitivity call.
4. **Nazareth and Acre, where the residents stayed.** A Deport card's history line would say that in 1948 the
   residents stayed (Nazareth) or that no expulsion order was issued (Acre). Keep that line, or use Line A there?
5. **Fled places and Deport.** If residents "fled" on capture at Lydda, Ramle and Beersheba, does #800 still offer
   Deport there, and so does each of them need a card? This is #800's rule, not this note's.
6. **Figures.** No line carries a population, displacement or casualty figure. The sources' counts (Acre 5,000 to
   6,000 remaining; "a few hundred" in Lydda; about 920 in Ramle after the war; "a few hundred" in Beersheba) are
   recorded as context only, for the owner to add if wanted.

## For the person who checks the sources

- Open each he.wiki permanent link and find the quoted sentence; the current page may differ.
- The Knesset summary was not reachable from this box; the reviewer reports it loads elsewhere (see "Sources"). Please
  read it for anything it says about Ben-Ami and Acre, and for its official lines on the other towns.
- Ramle's Line B rests on Gelber, *Komemiyut ve-Nakba* (2004), p. 246, through he.wiki; that page is worth reading
  directly.
- moreshet-map.org.il and the IAA were not consulted for these towns; nothing rests on them.
- The Hebrew sentences are copied, not translated; the English lines are drafts and need a read against them.
