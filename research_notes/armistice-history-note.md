# The armistice history note for the war-loss defeat (research note, 2026-10-22)

Status: draft; AI-read; every row below is `to_verify`. A person must open every cited page and check every line
before any text from this note goes into `content/`, `PlayerText.cs` or the end screen.

Researched 2026-10-22 by an AI agent for issue #1306, the research slice only. Refs #812 (the end screen, which shows
the note), #8 (the war loss, R-END-003, on main since #1246), #564 (the Hebrew review queue) and the owner's ruling
Q-DEF-4 (b) of 18 October 2026 on #784: "the short factual line per cause plus one sourced history note (rationing on
26 April 1949; the four armistices of 1949), naming no losses". The rationing half is
`research_notes/rationing-history-note-8.md` (#1151); this note is the armistice half, in the same shape. It changes no
content file, no code, no rule, no value and no golden. The short line "The enemy holds a city"
(`sim/Statecraft.Sim/PlayerText.cs:27`) stays as it is.

## Conventions

As in the rationing note: **[V]** verified in a primary or official record, or in two independent sources that agree;
**[S]** read in one named source, nothing found against it; **[C]** sources disagree; **not reached** means the source
exists but could not be opened today. Quotations are verbatim from the named source, for checking. The only quotation
proposed for game text is a phrase of the UN agreement texts themselves, not words of a person (`docs/CONTENT_POLICY.md`
§2.2). The draft names no person and no office is needed (`docs/WRITING_EVENTS.md`). Every source below was opened on
2026-10-22 (`checked: 2026-10-22`) unless the entry says otherwise. "Agent-read" means an AI agent extracted the text
(HTML stripped, or the PDF's text layer read with pypdf) and searched it; no person has read these pages for this note.

## Sources read

| Id | Source | What it is | Reached |
|---|---|---|---|
| UNTS | United Nations Treaty Series, vol. 42 (1949), Nos. 654-657 (https://treaties.un.org/doc/publication/UNTS/Volume%2042/v42.pdf) | The registered English texts. Printed page = PDF page minus 10 | yes, text layer (pypdf), agent-read |
| AV1 | Avalon Project, "Egyptian-Israeli General Armistice Agreement, February 24, 1949" (https://avalon.law.yale.edu/20th_century/arm01.asp) | Transcription of U.N. doc. S/1264/Rev.1 (its note 1) | yes, agent-read |
| AV2 | Avalon Project, "Lebanese-Israeli General Armistice Agreement, March 23, 1949" (https://avalon.law.yale.edu/20th_century/arm02.asp) | Transcription of U.N. doc. S/1296/Rev.1 | yes, agent-read |
| AV3 | Avalon Project, "Jordanian-Israeli General Armistice Agreement, April 3, 1949" (https://avalon.law.yale.edu/20th_century/arm03.asp) | Transcription of U.N. doc. S/1302/Rev.1 | yes, agent-read |
| AV4 | Avalon Project, "Israeli-Syrian General Armistice Agreement, July 20, 1949" (https://avalon.law.yale.edu/20th_century/arm04.asp) | Transcription of U.N. doc. S/1353/Rev.1 | yes, agent-read |
| OB | Edgar O'Ballance, *The Arab-Israeli War 1948* (Faber and Faber, 1956), chapter X "The Final Actions", pp. 204-206 (OCR text: https://archive.org/download/in.ernet.dli.2015.51540/2015.51540.The-Arab-Israeli-War-1948_djvu.txt) | A secondary history, the same scan `research_notes/GDD phase 5/enemy-script-after-first-truce-1270.md` uses | yes, OCR text, agent-read. The OCR prints "204" and "206" as page footers; the page between them has no number in the OCR, so "p. 205" below is the agent's inference |
| HEW | he.wiki, "הסכמי שביתת הנשק בתום מלחמת העצמאות" (https://he.wikipedia.org/wiki/הסכמי_שביתת_הנשק_בתום_מלחמת_העצמאות, MediaWiki API, plaintext) | Hebrew terminology only (the same page the four `armistice_*` events cite for the reviewer); dates cross-checked, not used as a source | yes, agent-read |
| UNISPAL | The UNISPAL PDFs the four `armistice_*` events cite (`content/events/armistice_*.yaml`, `checked: 2026-10-14`) | The UN document copies | **not reopened today**; recorded here from the event files |

Reused, not re-researched: `research_notes/Map borders/armistice-lines-1949.md` (the UN document symbols, UNTS
numbers and pages, read 2026-10-11; the same numbers were found again today), `research_notes/GDD phase
1/armistice-card-images-1949.md` (photographs only; nothing in it is needed for the note's text) and the four events
`content/events/armistice_egypt.yaml`, `armistice_lebanon.yaml`, `armistice_jordan.yaml`, `armistice_syria.yaml`
(their `from:` dates, 1949-02-24, 1949-03-23, 1949-04-03 and 1949-07-20, agree with every row below).

## The four agreements: date and place

Each date is the agreement's own closing line ("DONE at ...") in UNTS, matched by the Avalon transcription and by the
UNTS contents list. Each agreement also says it "is not subject to ratification and shall come into force immediately
upon being signed" (Egypt and Jordan art. XII.1, Lebanon and Syria art. VIII.1; UNTS pp. 268, 296, 318, 338; AV1-AV4),
so the signing date is also the date it came into force.

| # | Agreement | Signed (verbatim, UNTS) | UNTS | Avalon | O'Ballance | Status |
|---|---|---|---|---|---|---|
| A1 | Egypt-Israel | "DONE at Rhodes, Island of Rhodes, Greece, on the twenty-fourth of February nineteen forty-nine." | No. 654; contents p. iv: "Signed at Rhodes, on 24 February 1949"; closing line p. 270 (the text layer splits this line out of order, PDF lines around "DONE at Rhodes, Island of") | AV1, same words | p. 204: "signed between Israel and Egypt at Rhodes on 24th February 1949" | [V] `to_verify` |
| A2 | Lebanon-Israel | "DONE at Ras En Naqoura on the twenty-third of March nineteen forty-nine" | No. 655; contents p. vi: "Signed at Ras En Naqoura, on 23 March 1949"; closing line p. 298 | AV2, same words | p. 205: "concluded on March 23rd. This was signed at Ras El Naqura" | [V] `to_verify` |
| A3 | Jordan-Israel | "DONE at Rhodes, Island of Rhodes, Greece, on the third of April one thousand nine hundred and forty-nine" | No. 656; contents p. vi: "Signed at Rhodes, on 3 April, 1949"; closing line p. 320 | AV3, same words | p. 205: "On April 3rd the armistice between Israel and Jordan was signed" | [V] `to_verify` |
| A4 | Syria-Israel | "DONE at Hill 232 near Mahanayim on the twentieth of July nineteen forty-nine" | No. 657; contents p. vi: "Signed at Hill 232, near Mahanayim, on 20 July 1949"; closing line p. 340 | AV4, same words | p. 205: "Eventually, on July 20th, these were concluded" | [V] `to_verify` |
| A5 | Registration | All four: "The registration took place on 6 October 1949" | UNTS pp. 251, 287, 303, 327 (each agreement's title page) | - | - | [S] `to_verify` (background only, not for the note) |

HEW gives the same four dates (24 February, 23 March, 3 April, 20 July 1949). It is not counted as a source.

## What the texts say about what an armistice was

These rows back the second sentence of the draft. Each is a verbatim phrase of the agreement texts.

| # | Claim | Source | Status |
|---|---|---|---|
| B1 | Each preamble recalls the Security Council resolution of 16 November 1948, which called on the parties to negotiate an armistice "to facilitate the transition from the present truce to permanent peace in Palestine". The phrase is the Council's call, recited in each preamble, not a separate aim of each agreement | UNTS pp. 252, 288, 304, 328 (preambles); AV1-AV4, same words in all four | [V] `to_verify` |
| B2 | Each says it was "negotiated and concluded in pursuance of the resolution of the Security Council of 16 November 1948" | Egypt and Jordan art. XII.2, Lebanon and Syria art. VIII.2 (AV1-AV4); UNTS same pages as the ratification clause | [V] `to_verify` |
| B3 | Egypt: "The Armistice Demarcation Line is not to be construed in any sense as a political or territorial boundary, and is delineated without prejudice to rights, claims and positions of either Party to the Armistice as regards ultimate settlement of the Palestine question." | Egypt art. V.2, UNTS p. 256; AV1 | [V] `to_verify` |
| B4 | Jordan: "The Armistice Demarcation Lines defined in articles V and VI of this Agreement are agreed upon by the Parties without prejudice to future territorial settlements or boundary lines or to claims of either Party relating thereto." | Jordan art. VI.9, UNTS p. 312; AV3 | [V] `to_verify` |
| B5 | Lebanon: "The Armistice Demarcation Line shall follow the international boundary between the Lebanon and Palestine." | Lebanon art. V.1, AV2 (read today); UNTS p. 292 (the text layer breaks "bound-ary" across a line) | [V] `to_verify` |
| B6 | Each says "no provision of this Agreement shall in any way prejudice the rights, claims and positions of either Party hereto in the ultimate peaceful settlement of the Palestine question" | Egypt art. XI (AV1; UNTS p. 268); Lebanon, Jordan and Syria art. II.2 (AV2-AV4; UNTS pp. 290, 306, 330) | [V] `to_verify` |

What the agreements are **not**, per their own texts: the line clauses above are the evidence. The draft says only
"armistices, not peace treaties", which B1 (a transition towards "permanent peace") supports; it does not claim
anything about borders.

## Draft history note (EN, for the owner and a person's fact-check)

> In history, Israel signed four armistice agreements in 1949: with Egypt on 24 February, Lebanon on 23 March,
> Jordan on 3 April and Syria on 20 July. They were armistices, not peace treaties, meant in the words of the
> Security Council resolution they answered "to facilitate the transition from the present truce to permanent peace".

How each clause maps: the four dates are A1-A4; "armistices, not peace treaties" and the quoted phrase are B1 (the
quote is cut before "in Palestine" for length; a person should decide whether to keep the full phrase). It names no
person and no office, gives no loss figure, no casualty count and no place lost or held, and it does not say who won.

This starts from the owner-approved recommendation for (b) on #784 (comment 6231605979, the Q-DEF-4 draft: "In
history, the war ended with four armistice agreements: Egypt on 24 February, Lebanon on 23 March, Jordan on 3 April
and Syria on 20 July 1949"). One change, for a person to accept or undo: "the war ended with" became "Israel signed",
because the sources read here date the signings, not the end of the war (see open question 1). If the owner prefers
his approved sentence, the dates in it are all [V] above.

## Draft HE line (agent-checked, for the native-speaker queue on #564)

> בהיסטוריה, ישראל חתמה ב-1949 על ארבעה הסכמי שביתת נשק: עם מצרים ב-24 בפברואר, עם לבנון ב-23 במרץ, עם ירדן ב-3
> באפריל ועם סוריה ב-20 ביולי. אלה היו הסכמי שביתת נשק ולא הסכמי שלום, ונועדו, כלשון החלטת מועצת הביטחון שעליה
> ענו, להקל על המעבר מן ההפוגה הנוכחית לשלום של קבע.

`text_he_review: agent-checked`. The term "הסכמי שביתת הנשק" follows HEW and the four `armistice_*` events. The
quoted phrase is the agent's own translation of the English UN text, not an official Hebrew version, so the HE draft
gives it without quotation marks; the reviewer should say whether an accepted Hebrew rendering exists. This line goes
to #564 only after the owner approves the EN wording, as the Q-DEF-4 draft said for the other defeat texts.

## What a person still has to check

1. Open UNTS vol. 42 and read the four closing lines (pp. 270, 298, 320, 340) and the four preambles (pp. 252, 288,
   304, 328) against rows A1-A4 and B1. The page numbers are the agent's reading of the PDF (printed page = PDF page
   minus 10).
2. Open O'Ballance pp. 204-206 in the scan and confirm the page break the OCR does not number (p. 205).
3. Read Egypt art. XI (UNTS p. 268, B6) and Lebanon art. V.1 (UNTS p. 292, B5).
4. Re-open the four UNISPAL PDFs the events cite (not reopened today) and confirm they carry the same dates.
5. Native-speaker read of the HE line (#564), including the translated phrase.

## Open questions for the owner

1. **"The war ended" or "Israel signed".** HEW says the fighting ended on 10 March 1949 and treats the Syria agreement
   of 20 July as the formal end; O'Ballance says only that by 20 July "armistice agreements were in force along all the
   frontiers" (p. 205). Neither is a primary record of an end date. The draft avoids the claim; the owner decides.
2. **Iraq.** Iraq signed no armistice: O'Ballance p. 206 says the Iraqi forces "refused to enter into negotiations",
   HEW says Iraq "did not take part in the talks" ("לא השתתפה בשיחות"), and the Jordan agreement has "The Hashemite Jordan Kingdom accepts responsibility for all Iraqi
   forces in Palestine" (art. VI.7, UNTS p. 312). The note says "four armistice agreements", which is true; whether it
   should mention Iraq is the owner's call.
3. **The quoted phrase.** Keep it, cut it, or give it in full with "in Palestine"? Without it the note is the first
   sentence alone, which already meets Q-DEF-4 (b).
