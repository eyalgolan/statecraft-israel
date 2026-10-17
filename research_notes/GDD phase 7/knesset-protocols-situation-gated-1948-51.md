# The Knesset protocols behind the situation-gated candidates, 1949-51 (research note, AI draft, 2026-10-17)

Status: **AI draft, not checked by a person.** Research only: nothing here changes `content/`, and no card is drafted.
Refs #807 (GDD Phase 7 PR 3, the research half) and #387. The cards and the size of the set wait on the owner's
answers in #784 (Q-EVT-5, Q-NEW-1 to Q-NEW-5).

## Scope

[situation-gated-events-1948-51.md](situation-gated-events-1948-51.md) (#829) left the Knesset protocols unread: the
Knesset sites were "temporarily inaccessible" from the machine used, and its "Not reached, and gaps" section listed the
sessions to check. This note reads those sessions, plus two that sit next to them:

| #829 candidate | Session(s) read here |
|---|---|
| 1b, the Jerusalem resolution and the move | 5 December 1949 (93rd), 13 December 1949 (96th), 23 January 1950 (108th) |
| 1c, resolution 93 and the Hula | 14 May 1951 (251st) |
| 2a, the seamen's strike | 20 November 1951 (21st, Second Knesset), 28 November 1951 (25th, Second Knesset) |
| 2c, the religious bloc's camp crisis | 13 February 1951 (226th), 14 February 1951 (227th) |

It does not change #829's recommendation (1a, 1b and 2a). It moves several #829 facts from a lead, a single source or
the JTA bulletin's account onto the Knesset's own record, and it corrects two of them (section 5).

## How the protocols were reached

- The Knesset sites (`knesset.gov.il`, `main.knesset.gov.il`, `fs.knesset.gov.il` and the OData service) still answer
  with a redirect to `https://www.knesset.gov.il/maintenance-page-geo` from the machine used for this note (an AWS
  server outside Israel), on 17 October 2026.
- **Finding the files.** The Knesset's plenum index (the `KNS_PlenumSession` and `KNS_DocumentPlenumSession` tables of
  its OData service) is mirrored by the Open Knesset project as CSV files:
  `https://production.oknesset.org/pipelines/data/plenum/kns_plenumsession/kns_plenumsession.csv` and
  `https://production.oknesset.org/pipelines/data/plenum/kns_documentplenumsession/kns_documentplenumsession.csv`.
  They give each session's number and date and the `fs.knesset.gov.il` path of its protocol (a TIF scan, a PDF and a
  DOC per session). The mirror was used only to find file names and to list which sessions exist; every fact below
  comes from a protocol page.
- **Reading the files.** The Knesset's own PDFs were opened as Internet Archive (Wayback Machine) captures of their
  `fs.knesset.gov.il` addresses, the `id_` form, which returns the stored file unchanged. The capture list came from the
  Wayback CDX API (`url=fs.knesset.gov.il/1/Plenum/`, `matchType=prefix`, `filter=statuscode:200`, and the same for
  `/2/Plenum/`).
- **The text.** The PDFs are scans with a text layer (OCR). The text was taken out with PyMuPDF. In that layer the
  order of words inside a line is often scrambled (right-to-left text with punctuation moved to the wrong end), and
  some letters are misread (`שביתר` for `שביתה`, `אחר` for `אחד`). Hebrew quoted below is a run of words that reads in
  order on one line or across two adjacent lines of that layer; it has **not** been compared with the page image.
  Page numbers are PDF pages (1-based), not the printed page numbers of Divrei HaKnesset.
- Claude (Anthropic) found, opened and read these files on 17 October 2026, as an AI draft for Beri. That date is an
  access date, not a check: no person has read these pages for this note, so no `checked:` date is given anywhere
  below (CONTENT_POLICY section 2, rules 6 and 7).

## Sources used

| Short name | Session (index number, date, place on the title line) | Knesset file | Wayback capture read |
|---|---|---|---|
| K1 s93 | 93rd sitting, First Knesset, Monday 5 December 1949, Tel Aviv | `https://fs.knesset.gov.il/1/Plenum/1_ptm_250119.pdf` | https://web.archive.org/web/20170113165824id_/https://fs.knesset.gov.il/1/Plenum/1_ptm_250119.pdf |
| K1 s96 | 96th sitting, First Knesset, Tuesday 13 December 1949, 17.02, Tel Aviv | `https://fs.knesset.gov.il/1/Plenum/1_ptm_250215.pdf` | https://web.archive.org/web/20170113171816id_/https://fs.knesset.gov.il/1/Plenum/1_ptm_250215.pdf |
| K1 s108 | 108th sitting, First Knesset, Monday 23 January 1950, Jewish Agency building, Jerusalem | `https://fs.knesset.gov.il/1/Plenum/1_ptm_250127.pdf` | https://web.archive.org/web/20230410004401id_/http://fs.knesset.gov.il//1/Plenum/1_ptm_250127.pdf |
| K1 s226 | 226th sitting, First Knesset, Tuesday 13 February 1951, Jerusalem | `https://fs.knesset.gov.il/1/Plenum/1_ptm_250265.pdf` | https://web.archive.org/web/20190718101502id_/https://fs.knesset.gov.il/1/Plenum/1_ptm_250265.pdf |
| K1 s227 | 227th sitting, First Knesset, Wednesday 14 February 1951, Jerusalem | `https://fs.knesset.gov.il/1/Plenum/1_ptm_250266.pdf` | https://web.archive.org/web/20221209075541id_/https://fs.knesset.gov.il/1/Plenum/1_ptm_250266.pdf |
| K1 s251 | 251st sitting, First Knesset, Monday 14 May 1951, Jerusalem | `https://fs.knesset.gov.il/1/Plenum/1_ptm_250094.pdf` | https://web.archive.org/web/20260106111045id_/https://fs.knesset.gov.il/1/Plenum/1_ptm_250094.pdf |
| K2 s21 | 21st sitting, Second Knesset, Tuesday 20 November 1951, Jerusalem | `https://fs.knesset.gov.il/2/Plenum/2_ptm_250286.pdf` | https://web.archive.org/web/20221213141157id_/https://fs.knesset.gov.il/2/Plenum/2_ptm_250286.pdf |
| K2 s25 | 25th sitting, Second Knesset, Wednesday 28 November 1951, Jerusalem | `https://fs.knesset.gov.il/2/Plenum/2_ptm_250623.pdf` | https://web.archive.org/web/20240622223530id_/https://fs.knesset.gov.il/2/Plenum/2_ptm_250623.pdf |

Two earlier captures were cut off at 1 MB and would not open (`1_ptm_250127.pdf` of 17 September 2019 and
`2_ptm_250623.pdf` of 4 December 2022); the later captures in the table are complete.

## 1. Jerusalem: 5 and 13 December 1949, 23 January 1950 (#829 section 1b)

- **5 December 1949, before the General Assembly vote.** K1 s93, PDF p. 1, item a: "הודעת ראש-הממשלה בדבר ירושלים
  והמקומות הקדושים" (the Prime Minister's statement on Jerusalem and the Holy Places), Tel Aviv. In it the government
  holds that the 29 November [1947] decision on Jerusalem is "בטלה ומבוטלת" (null and void). The debate on the
  statement is item b of the same sitting (PDF p. 2).
- **13 December 1949, the answer to resolution 303 (IV).** K1 s96, PDF p. 1, the agenda title "העברת הכנסת והממשלה
  לירושלים" (moving the Knesset and the government to Jerusalem). The Prime Minister refers back to his statement of a
  week before, and says of the General Assembly's decision: "החלטה זו לא ניתנת לביצוע בשום אופן" (this decision cannot
  be carried out in any way). He then proposes that the Knesset return to Jerusalem.
  - **The date.** The title line reads Tuesday, 22 Kislev 5710, 13 December 1949, 17.02, Knesset building, Tel Aviv.
    The Knesset index has no sitting between 7 and 13 December 1949; the next sitting (97th) was on 14 December. So the
    statement JTA reported on 14 December 1949 ("today") was made on **13 December**. #829 left this open.
  - **The vote.** PDF p. 2. The vote was on Yosef Sapir's (General Zionists) motion to hold a debate on the statement;
    the Prime Minister had asked that the parties make statements and that the foreign-policy debate be held later. The
    first count was 27 for the motion and 59 against. A Herut member said 37 had voted for it, not 27; the Speaker had
    two members count again, and the recount was **39 for, 60 against**: "ההצעה לא נתקבלה" (the motion was not
    carried). On PDF p. 7 the Speaker comes back to the miscount on a written complaint from the same member.
  - **The move.** PDF p. 7, the Speaker, closing: the Knesset's sittings after Hanukkah "יתקיימו מחדש בירושלים" (will be
    held again in Jerusalem), with applause. The sitting closed at 18.25.
- **23 January 1950, the declaration.** K1 s108, title line: Monday, 5 Shevat 5710, 23 January 1950, 16.15, the Jewish
  Agency building, Jerusalem. So by this date the Knesset was sitting in Jerusalem.
  - Item c, "הצהרת הכנסת בדבר ירושלים" (the Knesset's declaration on Jerusalem), PDF p. 16. The committees chose a
    declaration rather than a law (the Herut faction had tabled a bill). The declaration's text (PDF p. 17) recalls that
    "הכנסת החליטה ביום כ"ב בכסלו תש"י" (the Knesset decided on 22 Kislev 5710), dated 13 December 1949 in brackets, to
    return to Jerusalem, and asks that the government precinct be built in Jerusalem.
  - **The vote,** PDF p. 20. Law against declaration: 15 for a law, 45 for a declaration. After a dispute over the
    order of voting, a second vote: a law 15 for and 47 against, not carried; the declaration 45 for and 15 against,
    carried. The declaration in the committee's wording is printed again on PDF p. 29.
  - #829 had the declaration of 23 January 1950 as a **lead** (a search summary). On this reading it took place, and
    its date, place and vote are on the protocol's pages.

## 2. The government's fall over camp education, 13 and 14 February 1951 (#829 section 2c)

- **The debate.** K1 s226 (title line: Tuesday, 7 Adar I 5711, 13 February 1951, Jerusalem) and K1 s227 (Wednesday,
  8 Adar I 5711, 14 February 1951, Jerusalem) continue the debate on the Education Minister's statement on "החינוך
  במחנות העולים ובמעברות" (education in the immigrant camps and the transit camps). The summary on K1 s227 PDF p. 22
  dates the first day of the debate, sitting 225, to 12 February 1951.
- **The motions.** K1 s227 PDF p. 16: the General Zionists tabled two motions, to be voted on apart. The first reads
  "הכנסת דוחה את הודעת" [הממשלה] (the Knesset rejects the government's statement) as the Education Minister gave it;
  the second asks the government to bring, within a month, a bill that ends the party "streams" in education. The
  summary on PDF p. 22 lists motions from Mapam, the United Religious Front, Herut, the General Zionists, WIZO and the
  Progressives, and one from Yohanan Bader (Herut) in his own name.
- **The Prime Minister's warning.** PDF p. 19, before the votes: in the text layer he announces "in the name of the
  government" that adopting Bader's motion (that the Knesset "heard with satisfaction" the minister's statement and
  moves on, which he calls insincere) would be read as no confidence, and rejecting it as confidence. The lines of
  that sentence are out of order in the text layer, and Bader's motion is not among the three counts below, so how the
  warning bears on the vote that carried needs the page image.
- **The votes,** PDF p. 19: Mapam's motion 36 for, 43 against, 17 abstaining, not carried; Herut's motion 22 for,
  61 against, not carried; **the General Zionists' motion 49 for, 42 against, 3 abstaining, carried**. The summary on
  PDF p. 22: "הוחלט: לקבל את הצעת סיעת הציונים הכלליים" (decided: to adopt the General Zionists' motion).
- **The resignation,** PDF p. 19, item 6, "התפטרות הממשלה" (the government's resignation): the Prime Minister says the
  government, "in line with my earlier statement", sees this as "הבעת אי-אמון ומתפטרת" (an expression of no
  confidence, and resigns), and that he will go to the President that
  day. The Progressives and the United Religious Front then make statements; the Progressives say
  they abstained, rather than vote against the government, because of the foreign and currency situation.
- **What this changes in #829.** 2c had the government's fall as [S] only (Wikipedia, the Israel Democracy
  Institute). On this reading the fall is on the protocol page, and the date is **14 February 1951**, the 227th
  sitting. Which of the General Zionists' two motions the vote of 49 to 42 was on is not clear from the text layer.
  #829's recommendation stands, and on `main` it is mostly done already: `education_registration_1951` has
  `from: 1950-11-29` and `to: 1951-02-14` (exclusive, `docs/WRITING_EVENTS.md`), cites the 227th sitting
  (`1_ptm_250266.pdf`) and the 224th (`1_ptm_250264.pdf`) in its `sources:`, and its comment records the resignation on
  14 February 1951. What this note adds is the vote count and the 226th sitting (`1_ptm_250265.pdf`), which the card
  does not cite.
- **An index slip.** The Open Knesset mirror dates the 226th sitting to 12 February 1951 (the same day as the 225th);
  the protocol's title line and the summary on K1 s227 PDF p. 22 date it to 13 February. The protocol is followed here.

## 3. The Hula and the cease-fire, 14 May 1951 (#829 section 1c)

- K1 s251 (title line: Monday, 8 Iyar 5711, 14 May 1951, Jerusalem), item b, PDF p. 4: "הודעת שר-החוץ על הסכסוך עם
  סוריה" (the Foreign Minister's statement on the conflict with Syria). The speaker line is the Foreign Minister,
  M. Sharett.
- The statement (PDF pp. 4-5) is about a Security Council decision, adopted unanimously after the Council discussed
  Israel's complaint on 8 May 1951, that calls on the parties to stop the fighting. That is a cease-fire call made
  before resolution 93, not resolution 93 itself (18 May); its resolution number was not checked here. The statement
  says the Israeli and Syrian amendments to the draft were withdrawn before the vote.
- **Israel's line,** PDF p. 5: the government "קיבלה על עצמה לקיים ולשמור את הפסקת האש" (undertook to keep the
  cease-fire), on the stated assumption that the other side does the same, and the work on the Hula drainage plan
  "נמשכת כסדרה" (goes on as planned).
- **What this adds to #829.** 1c quotes JTA 15 May 1951, under "Before it": the "Acting Prime Minister" told the
  Knesset that Israel "has accepted the cease-fire order". This is the same statement. The protocol dates it to
  **14 May**, labels the speaker Foreign Minister (whether he was also acting Prime Minister that day was not checked
  here), and shows that the cease-fire it concerns is the Council's call that followed its 8 May discussion. The
  decision between 18 May and 13 June that #829 did not find is still not found: the sittings of
  15 May and 6 June 1951 have no Wayback capture (section 6).
- **Sensitivity:** as in #829 (the April fighting; the Arab civilians removed from the zone).

## 4. The seamen's strike, 20 and 28 November 1951 (#829 section 2a)

- **Already under way on 20 November.** K2 s21 (Tuesday, 21 Heshvan 5712, 20 November 1951, Jerusalem), PDF p. 7: a
  member, in another debate, sets aside "השאלות האקטואליות" (the current questions) about Ashed (?) "או לשביתת הימאים"
  (or the seamen's strike). This puts the strike before 20 November; #829 placed its start in "about the second week
  of November".
- **A count of days (a lead).** K2 s25 (28 November), PDF p. 8: the General Zionists' Y. Rokah, moving his motion,
  says "היום הוא היום השמונה-עשר" (today is the eighteenth day), and another speaker on PDF p. 9 also counts eighteen
  days. On PDF p. 8 a third remark has "מחר ימלאו שמונה-עשר ימים" (tomorrow it will be eighteen days), and the same
  page speaks of the last 17 days. Together that would put the start at about 11 or 12 November 1951. These are members'
  remarks, not a record of the start, so they stay a lead; the start day is not found on a record.
- **The Knesset's motions, 28 November.** K2 s25 (Wednesday, 29 Heshvan 5712, 28 November 1951, 11.10, Jerusalem),
  PDF p. 8: "נעבור להצעות לסדר-היום בענין שביתת הימאים" (we move to the motions for the agenda on the seamen's
  strike); one motion's title (PDF p. 11) speaks of the events in Haifa port over the resignation of the Hebrew seamen.
- **The Prime Minister's line,** PDF p. 10, in exchanges with Mapam and Maki members:
  - "לא היתה ולא קיימת כל שביתה" (there was no strike and there is none); a "criminal attempt" (`נסיון פושע`) for
    political reasons to sabotage "the most vital artery" of the state.
  - The men in the merchant fleet had been deferred from army service because their work was vital; having left it,
    they must serve: "והם יגויסו וככל חייבי גיוס ישרתו שנתיים בצבא" (they will be called up and, like everyone liable,
    serve two years in the army).
  - The ships will keep sailing "by all the means we have", and the government will change nothing in what it has done
    so far.
- **The Minister of Transport,** PDF p. 11: the men are not state employees, and the Histadrut sent other workers in
  their place.
- **The vote,** PDF p. 11: the motion to hold a debate was carried unanimously (the text layer reads `פה אחר`, an OCR
  slip for `פה אחד`); the government and the movers had both agreed to it.
- **What this changes in #829.** JTA 29 Nov 1951 has the Prime Minister in the Knesset calling the strike "a criminal
  attempt by enemies of the state" and saying "there was not and there is not a strike". The protocol of the day
  before has the same points in Hebrew, and adds the call-up line, which #829 had only from JTA 28 Nov ("draft
  notices"). The debate that was agreed on, and the strike's end, are not in the sittings read here.

## 5. Corrections to #829

| #829 says | The protocol says | Where |
|---|---|---|
| 1b: the Knesset session on the Jerusalem resolution was on "13 or 14 December" 1949 | 13 December 1949 (96th sitting, Tel Aviv) | K1 s96, title line |
| 1b: "the Knesset voted 59 to 27 to postpone a general debate" (JTA) | apparently the same vote (the counts and the day match), recorded as a motion **to hold** a debate; 27 to 59 was the first count, and after a member said 37 had voted for it, the recount was 39 for and 60 against | K1 s96, PDF pp. 2, 7 |

And three additions: the declaration of 23 January 1950 (a lead in #829) and the government's fall on 14 February 1951
([S] in #829) are on the protocol pages (sections 1 and 2), and the Hula statement JTA reported on 15 May 1951 is dated
14 May, by the speaker labelled Foreign Minister, on the cease-fire call after 8 May (section 3).

## 6. Not reached

- The **Knesset sites themselves**: geo-blocked from this machine (see above). No page here was read on
  `knesset.gov.il`.
- **No Wayback capture** of the protocol (PDF or DOC) for: 14 December 1949 (97th sitting, `1_ptm_250031.pdf`),
  15 May 1951 (252nd, `1_ptm_250080.pdf`), 6 June 1951 (262nd, `1_ptm_250082.pdf`, where JTA 6 Jun 1951 places the
  release of the last detainees of the religious underground), and 26 November, 27 November and 17 December 1951 (the
  Second Knesset's 23rd, 24th and 32nd, `2_ptm_250622.pdf`, `2_ptm_250389.pdf`, `2_ptm_250391.pdf`).
- **The 8 June 1949 reply to the United States** (#829 section 1a): no Knesset sitting was looked for; the reply is a
  note between governments and is already on FRUS pages.
- **The TIF scans and the printed page numbers** were not opened.

## What a person must check

Nothing above has been read by a person. Before any of it goes into `content/` `sources:` with a `checked:` date, a
person reads the page **image** (the Knesset's PDF or TIF, ideally from `fs.knesset.gov.il` itself, from Israel) and
confirms:

1. **K1 s96 (13 December 1949):** the title line date; the quoted sentence "החלטה זו לא ניתנת לביצוע בשום אופן"; the two
   counts on Sapir's motion (27/59, then 39/60) and that the motion was to hold a debate; the Speaker's closing line on
   returning to Jerusalem after Hanukkah.
2. **K1 s93 (5 December 1949):** that the "null and void" sentence refers to the 29 November 1947 decision on Jerusalem.
3. **K1 s108 (23 January 1950):** the place (the Jewish Agency building, Jerusalem); the declaration's wording; both
   votes (15 to 45; then 15 to 47 on a law and 45 to 15 on the declaration).
4. **K1 s227 (14 February 1951):** which General Zionists motion was put to the vote, the three counts (36/43/17,
   22/61, 49/42/3), the Prime Minister's sentence on confidence and on resigning, and the name at the end of his
   warning (Bader or another).
5. **K1 s226:** the date, 13 February 1951 (the mirror says 12 February).
6. **K1 s251 (14 May 1951):** the speaker (Foreign Minister M. Sharett), the 8 May reference and which Security
   Council resolution it is, and the two quoted phrases.
7. **K2 s25 (28 November 1951):** the Prime Minister's quoted phrases, the call-up sentence, the unanimous vote, the
   titles of the agenda motions, and the day counts on PDF pp. 8-9 ("the eighteenth day",
   "tomorrow eighteen days", "the last 17 days") and who made them.
8. **K2 s21 (20 November 1951):** the passing mention of the seamen's strike, and what "Ashed" stands for.
9. The sittings in section 6 that have no capture.

## Summary

| #829 candidate | What the protocols add | Sessions | Effect on #829's recommendation |
|---|---|---|---|
| 1b, Jerusalem | Statement and decision on 13 Dec 1949 (not 14); the vote on holding a debate was 39 to 60 after a recount; the 23 Jan 1950 declaration passed 45 to 15 | K1 s93, s96, s108 | None: still recommended |
| 1c, resolution 93 | The 14 May statement was the Foreign Minister's, on the cease-fire call after 8 May; drainage "goes on as planned" | K1 s251 | None: still "maybe", overlaps `hula_drainage` |
| 2a, seamen's strike | Under way by 20 Nov 1951; on 28 Nov the Prime Minister said there was no strike and the men would be called up; debate agreed unanimously | K2 s21, s25 | None: still recommended |
| 2c, camp crisis | The government fell on 14 Feb 1951 on a General Zionists motion, 49 to 42 with 3 abstaining | K1 s226, s227 | None: a source for `education_registration_1951`, which already cites the 227th sitting on `main` |
