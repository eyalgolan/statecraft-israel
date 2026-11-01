# The territories, 1967-1977: military administration, the open bridges and the settlement decisions

Researched 1 November 2026 by an AI agent for issue #125 (1957-77 item 13, the research part only, opened by the
owner on 1 November 2026). Every source below was opened and read by the agent on 1 November 2026 unless a row says
otherwise; a person's check of each read is pending. Research only: no event YAML, no options, no card text, no
proposed tags and nothing under `content/`. How any of these events is shown, and which date a policy takes, stays
the design's call. The facts are recorded in neutral terms (design §7.2, D6; §10, D7 rule 4).

## Purpose

[The 1957-1977 design](../../docs/superpowers/specs/2026-10-10-era-1957-1977-design.md) §7.2 handles the territories
through the policies `territories_settlement` and `open_bridges`, the actor `territories_residents` and the Cabinet's
real decisions as events: the Allon Plan (July 1967), Kfar Etzion (September 1967), Hebron and Kiryat Arba, the Golan
settlements, Sebastia and Kadum (1974-75). §2.5 marks Gush Emunim's founding (February 1974) and Hebron, Kiryat Arba,
Ofra and the Golan settlements *to verify*. This note collects, for each of those decision points, the dated facts and
what a source says, and closes or narrows the [GAP]s the sibling notes left open on them.

Where a sibling note already sources a fact, this note points to it and does not repeat it:

- [events-1967-70.md](events-1967-70.md): B1 (the Allon Plan, FRUS), B3 (Kfar Etzion, JTA), B9 (Hebron, Passover 1968).
- [events-1970-77.md](events-1970-77.md): B14 (Sebastia and Kadum, 1975-76).
- [model-basis-1957-77.md](model-basis-1957-77.md): 1.5 (`open_bridges`, JTA August 1967 to July 1968 and FRUS 1972),
  1.6 (`territories_settlement`), 3.3 (`territories_residents`: military governors named on 8 June 1967).
- [west-bank-localities-1967-77.md](west-bank-localities-1967-77.md): the founding dates of Kfar Etzion, Kiryat Arba,
  Ofra, Qedumim and the other West Bank localities.
- [golan-localities-1967-77.md](golan-localities-1967-77.md) and
  [jordan-valley-localities-1967-77.md](jordan-valley-localities-1967-77.md): the Golan and Jordan Valley settlements.

## Conventions

The same as [events-1967-70.md](events-1967-70.md):

- **[V, agent read 2026-11-01; a person's check pending]** read on this day in a primary source (here: a UN document,
  read on the page image). Every [V] here is an agent read; a person's check is pending (CONTENT_POLICY §2 item 6).
- **[S]** single or secondary source only: a JTA bulletin, or Wikipedia as a pointer, opened on the page itself and
  cited by revision number.
- **[C]** sources conflict; every value is kept with its source.
- **[GAP]** not found. A gap stays a gap: no date, figure or name is filled in by inference. A search snippet is
  never a source.
- **Derived** marks a value I computed (a weekday, a day resolved from a dateline). Derived values are never [V].
- **JTA dates.** The archive page shows the bulletin date. The event date comes from the dateline on the item in the
  original bulletin PDF (`http://pdfs.jta.org/...`), read here by `tesseract` OCR of the page images rendered at
  450 dpi; "today", "yesterday" and "last night" are resolved from that dateline. Weekdays are computed with Python's
  `datetime`.
- **Hebrew** quotes are copied from the cited revision (its en dashes written as hyphens); the English in parentheses after each quote is my translation.
- People are named only where they have a Wikipedia article; others are given by their role.

## Sources opened on 1 November 2026

| Ref | Source | What was read |
|---|---|---|
| U1 | UN, Report of the Secretary-General under General Assembly resolution 2252 (ES-V) and Security Council resolution 237 (1967), A/6797-S/8158, 2 October 1967 (the file the UN server returns for A/6797 prints the symbol S/8158): https://documents.un.org/doc/undoc/gen/n67/223/32/pdf/n6722332.pdf | paras 86, 87, 91, 188-191; annex V (first Israeli aide-memoire on the West Bank, printed p. 81); annex VI (second aide-memoire, printed p. 84); quotes checked on the page images |
| U2 | UN, Report of the Special Committee to Investigate Israeli Practices, A/8089, 1970: https://documents.un.org/doc/undoc/gen/n70/235/42/pdf/n7023542.pdf | text searched for the proclamations; para 142 read |
| J1 | JTA, bulletin of 11 March 1970, `plans-to-build-jewish-suburb-next-to-hebron-assailed-by-left-praised-by-right`; PDF http://pdfs.jta.org/1970/1970-03-11_047.pdf p. 2 | dateline JERUSALEM, March 10 |
| J2 | JTA, bulletin of 27 March 1970, `cabinet-approves-decision-to-build-permanent-housing-for-250-jewish-families-in-hebron`; PDF http://pdfs.jta.org/1970/1970-03-27_059.pdf p. 3 | dateline JERUSALEM, March 26 |
| J3 | JTA, bulletin of 5 September 1973, `labor-party-secretariat-overwhelmingly-approves-dayan-plan`; PDF http://pdfs.jta.org/1973/1973-09-05_170.pdf pp. 1-2 | dateline TEL AVIV, Sept. 4 |
| J4 | JTA, bulletin of 20 December 1967, `amman-acknowledges-brisk-trade-between-both-banks-of-jordan-river`; PDF http://pdfs.jta.org/1967/1967-12-20_245.pdf p. 1 | dateline JERUSALEM, Dec. 19 |
| J5 | JTA, bulletin of 6 June 1974, `jewish-squatters-seek-settlement-in-nablus`; PDF http://pdfs.jta.org/1974/1974-06-06_109.pdf p. 3 | dateline JERUSALEM, June 5 |
| J6 | JTA, bulletin of 29 July 1974, `settlers-quit-illegal-settlement-to-avoid-confrontation-with-govt`; PDF http://pdfs.jta.org/1974/1974-07-29_144.pdf p. 1 | dateline TEL AVIV, July 28 |
| W1 | English Wikipedia, "Allon Plan", revision 1383083073: https://en.wikipedia.org/w/index.php?title=Allon_Plan&oldid=1383083073 | section on the 1967 plan |
| W2 | Hebrew Wikipedia, "תוכנית אלון", revision 44114699: https://he.wikipedia.org/w/index.php?oldid=44114699 | sections on submission and implementation |
| W3 | English Wikipedia, "Theodor Meron", revision 1381697889: https://en.wikipedia.org/w/index.php?title=Theodor_Meron&oldid=1381697889 | section on the 1967 legal opinion |
| W4 | English Wikipedia, "Kfar Etzion", revision 1378265794: https://en.wikipedia.org/w/index.php?title=Kfar_Etzion&oldid=1378265794 | section "New Kfar Etzion" |
| W5 | English Wikipedia, "Israeli Military Governorate", revision 1357126059: https://en.wikipedia.org/w/index.php?title=Israeli_Military_Governorate&oldid=1357126059 | the 1967 establishment |
| W6 | English Wikipedia, "Shlomo Gazit", revision 1371658102: https://en.wikipedia.org/w/index.php?title=Shlomo_Gazit&oldid=1371658102 | career, 1967-1974 |
| W7 | Hebrew Wikipedia, "מתאם פעולות הממשלה בשטחים", revision 43916836: https://he.wikipedia.org/w/index.php?oldid=43916836 | history section |
| W8 | Hebrew Wikipedia, "הגשרים הפתוחים", revision 43635154: https://he.wikipedia.org/w/index.php?oldid=43635154 | section on the policy |
| W9 | Hebrew Wikipedia, "מסמך גלילי", revision 43882294: https://he.wikipedia.org/w/index.php?oldid=43882294 | the approval |
| W10 | Hebrew Wikipedia, "גוש אמונים", revision 43834201: https://he.wikipedia.org/w/index.php?oldid=43834201 | founding and the 1974-75 attempts |
| W11 | English Wikipedia, "Gush Emunim", revision 1383144274: https://en.wikipedia.org/w/index.php?title=Gush_Emunim&oldid=1383144274 | founding and Sebastia |

All JTA archive pages are `https://www.jta.org/archive/<slug>`. Not reached: knesset.gov.il, gov.il and the Israel
State Archives (blocked from this server, as in the sibling notes), and the Wayback Machine (HTTP 429 on every request
on 1 November; see the search log). No Cabinet minutes, no Divrei HaKnesset and no book was opened.

## Summary table

| # | Decision point | Date(s) | Status of the key date |
|---|---|---|---|
| M1 | Military governors named | 8 June 1967 | cross-reference: model-basis-1957-77.md 3.3, [S] JTA |
| M2 | The law kept in force; courts reopened | 7 June 1967 (wiki); by 3 July (Nablus courts; year not printed, derived 1967) | [V, agent read 2026-11-01; a person's check pending] U1 for the courts and the law; [S] W5 for 7 June; [GAP] the proclamation's text |
| M3 | Coordination of policy in the territories | July 1967 to August 1968 | [S] W6, W7; [GAP] a primary record |
| O1 | Returns across the bridges | 18 July; 6, 12 and 18 August 1967 | [V, agent read 2026-11-01; a person's check pending] U1 |
| O2 | Trade across the river | June to December 1967 | [V, agent read 2026-11-01; a person's check pending] U1 (the summer position); [S] W8, J4; [GAP] the 7 September 1967 procedure's text |
| S1 | The Allon Plan before the Cabinet | 26 or 27 July; 30 July 1967 | [C] W1 against W2; [GAP] a primary record |
| S2 | Kfar Etzion: the legal advice | September 1967 | [S] W3, W4 (no date); [GAP] the memo's date and text |
| S3 | Hebron: the suburb plan and the 250 homes | 9 and 25 March 1970 | [S] J1, J2; [GAP] the Cabinet's own decision day |
| S4 | The Golan settlements | from 14 or 15 July 1967 | cross-reference: golan-localities-1967-77.md |
| S5 | The Galili document | 3 September 1973 | [S] J3, W9; [C] 78 or 79 votes |
| S6 | Gush Emunim founded | February 1974 | [S] W10, W11; [C] the place; [GAP] a contemporary source |
| S7 | The 1974 attempts near Nablus and at Sebastia | 5 June; 25 to 28 July 1974 | [S] J5, J6, W10 |
| S8 | Sebastia 1975 and Kadum | March to December 1975 | [S] W10, consistent with events-1970-77.md B14; Kadum: cross-reference |
| S9 | Ofra | April to June 1975 | cross-reference: west-bank-localities-1967-77.md row 10 |

---

## M. The military administration (`territories_residents`)

### M1. Military governors named (8 June 1967)

Cross-reference, not re-read: model-basis-1957-77.md 3.3 has the JTA item datelined JERUSALEM, June 8: the Government
"named today military governors for the west bank section of Jordan and for the Gaza Strip and Northern Sinai" [S].

### M2. The law kept in force; the courts reopened (June to July 1967)

- **What the wiki says.** W5: "General Chaim Herzog announced on 7 June 1967, that all previously existing laws would
  remain in force, save in cases where they conflicted with the rights of Israel as Occupying Power to ensure security
  for both its forces and public order." [S]
- **What Israel told the UN in the summer of 1967.** U1, annex VI (the second aide-memoire submitted to the Secretary-
  General's Special Representative by the Israel authorities on the West Bank), printed p. 84, under "Administration of
  Justice": "Local Courts, including District Courts, have been reactivated. They are functioning on the basis of laws in
  force before 5 June." [V, agent read 2026-11-01; a person's check pending] U1, annex V (the first aide-memoire),
  printed p. 81, under "Law": "Civil courts have resumed their work in Nablus on 3 July. They are adjucating under
  Jordanian law." (spelling as printed) [V, agent read 2026-11-01; a person's check pending]
- **What the UN Special Committee found in 1970.** U2, printed p. 60 (PDF page 60), para 142, under "Interference with
  the judicial system, including legal aid": "The Special Committee has examined the proclamations and orders
  promulgated by the occupation authorities in the occupied territories and finds that the Israeli authorities have
  seriously hampered the functioning of the court system by transferring the Court of Appeal in Jerusalem to Ramallah.
  This transfer provoked a reaction on the part of the judiciary that brought activities of the Court of Appeal to a
  standstill." (the text layer misprints "Ramallah" as "Ramsllah"; the page image reads "Ramallah") [V, agent read
  2026-11-01; a person's check pending] This is the Committee's finding, not Israel's statement.
- **Reading.** The Israeli statements in U1 agree with W5 that the law in force before the war was kept; they date the
  courts' reopening in Nablus to "3 July" (no year printed; 1967 is derived from the report's date, October 1967). Neither U1 nor any source read gives the proclamation itself.
- **Open.** [GAP] The text and number of the first proclamations of June 1967. U2 says the UN Special Committee "has
  examined the proclamations and orders promulgated by the occupation authorities" (para 142) but does not print them;
  searches in the log.

### M3. Coordinating policy in the territories (July 1967 to August 1968)

- W6: Gazit "was appointed by Defense Minister Moshe Dayan to be in charge of a committee tasked with running the
  political, security, and later economic affairs in the newly captured territories" in "the summer of 1967". Its next sentence, copied as printed: This group
  would later be renamed "The Unit for the Coordination of Operations in the Territories". [S]
- W7 gives the steps (my summary of the history section): a committee of directors-general to coordinate policy in the
  territories was set up in July 1967, with a reserve colonel (Col. Yehuda Nitzan) appointed its coordinator ("מרכז"; the word means coordinator here, not head) for some months; from 10 September 1967 to September
  1968 another colonel (Col. Dan Hiram) was coordinator of civil affairs; in August 1967 Gazit was named chair of the political-security
  coordination committee in the territories, and towards the end of 1968 (after Hiram's retirement) he was also appointed
  coordinator ("מרכז") of the directors-general committee and became the Coordinator of Government Activities in the Territories. Two sentences quoted: "ובדצמבר 1967 מונה לראש מחלקת ממשל צבאי וביטחון מרחבי במטכ"ל." (and in
  December 1967 he was named head of the General Staff's department of military government and regional security);
  "באוגוסט 1968 אוחדה יחידת מרכז ועדת המנכ"לים עם יחידת התיאום בראשות גזית." (in August 1968 the unit of the directors-general committee's coordinator was merged with the coordination
  unit headed by Gazit). [S]
- **[C]** On when Gazit's role began, W7 itself gives two months: the prose says he was named chair of the political-
  security coordination committee "באוגוסט 1967" (August 1967), while the same revision's table of coordinators gives
  Gazit "ספטמבר 1967 - 1974" (September 1967 to 1974) and Hiram "ספטמבר 1967 - ספטמבר 1968". Both kept; W6 says only
  "the summer of 1967".
- **Two steps, not a conflict.** W7 gives two separate steps in 1968: the merger of the two units in August 1968, and
  Gazit becoming coordinator of the directors-general committee "עם פרישת חירם לקראת סוף שנת 1968" (with Hiram's
  retirement towards the end of 1968). The table puts Hiram's term ending in September 1968. These can be consistent;
  W7 does not say whether the two steps are one event, so the order is recorded as the source gives it and not
  resolved. W6 gives no month.
- **Open.** [GAP] A primary record (the Government or Defence Ministry decision) for any of these steps.

---

## O. The open bridges (`open_bridges`)

The JTA evidence from August 1967 to July 1968 and the FRUS record of 1972 are in model-basis-1957-77.md 1.5 and are
not repeated. This section adds the UN record of the summer of 1967 and the dated steps the Hebrew Wikipedia gives.

### O1. Returns across the bridges (July to September 1967)

U1, paras 188-191, printed pp. 52-53, all [V, agent read 2026-11-01; a person's check pending]:

- 18 July 1967: "some fifty families had already returned to the West Bank, following an Israel Government decision to
  allow the return of special hardship cases on that date" (para 189).
- 6 August 1967: "during a meeting at the Allenby Bridge, in which representatives of the Israel Government, the ICRC,
  and the Jordan Red Crescent participated", the heading of the application forms was agreed (para 188).
- 12 August 1967: "The distribution of the application forms began on 12 August." The Israel Government later agreed
  "to postpone the deadline for the return operation until 31 August." (para 188)
- 18 August 1967: "The first displaced persons under this scheme crossed the Jordan river on 18 August." On 9 September
  the total returned was "14,150 persons" (Jordanian sources) or "14,056" (Israel sources) (para 190). **[C]** on the
  total, both kept.
- Israel "claimed that it had opened two bridges across the Jordan river to receive returnees at the rate of 3,000 a
  day" (para 191).

This agrees with model-basis-1957-77.md 1.5, which dates the first crossing to Friday 18 August 1967 from JTA.

### O2. Trade across the river (June to December 1967)

- **The summer position, from U1** [V, agent read 2026-11-01; a person's check pending]: after the war "Trade between
  the West Bank and the East Bank was suspended" (para 86, printed p. 30). The Israel Government said it had bought
  "West Bank agricultural surpluses formerly exported to East Jordan and to other Arab countries" (para 87). Israel
  sources told the Special Representative that "it was not to be expected that next season's agricultural production
  could be exported to the East Bank. Instead, the Israel market would have to be used as a basis for planning of West
  Bank agriculture." (para 91, printed p. 32)
- **The steps W8 gives** [S]: produce first crossed at a ford with the local military government's consent, approved
  afterwards by the command and the Defence Minister (my summary); "באוגוסט 1967 הוקמה נקודת ביקורת גבולות בגשר אלנבי." (in August 1967 a border control
  point was set up at the Allenby Bridge); "ב־7 בספטמבר 1967 הוציאה ועדת המנכ"לים נוהל רשמי למעבר הסחורות בין צדדי הירדן." (on 7 September 1967 the directors-general committee issued
  a formal procedure for the passage of goods between the two banks of the Jordan); "באוקטובר-נובמבר 1967 הוקמו גשרי ביילי בגשר אלנבי ובגשר אדם על ידי ירדן." (in October to
  November 1967 Bailey bridges were put up at the Allenby and Adam crossings by Jordan); "בדצמבר 1967 הותר היבוא מירדן בתקנות היבוא והמכס הישראליות." (in December
  1967 imports from Jordan were permitted in Israel's import and customs regulations). 7 September 1967 was a Thursday
  (derived).
- **Jordan's side** [S]: J4, dateline JERUSALEM, Dec. 19 (so 19 December 1967): "The Jordanian Government tacitly
  admitted today that a brisk trade is going on between Jordan and the Israel-occupied West Bank by announcing that a
  comission has been set up to supervise it." (spelling as printed)
- **[C]** U1's summer expectation (no exports to the East Bank next season) against the trade that W8, J4 and the JTA
  items in model-basis-1957-77.md 1.5 show from mid-August 1967. These are two moments, not two values for one fact; both
  are kept so the design can see the policy change.
- **Open.** [GAP] The text of the 7 September 1967 procedure and any Government decision that opens the bridges.
  model-basis-1957-77.md 1.5's reading stands: the earliest dated evidence of crossing is August 1967, and no source read
  places a start on 5 June 1967.

---

## S. The settlement decisions (`territories_settlement`)

### S1. The Allon Plan before the Cabinet (July 1967)

- W1: "On 27 July 1967, Allon presented the first version of his plan based on the Palestinian option, which included
  Palestinian autonomy in the West Bank." "A vast majority of the ministers rejected the plan when it was brought before
  the plenary session of the government on 30 July." [S]
- W2: "תוכנית אלון הוגשה לממשלה ב-26 ביולי 1967, כחודש לאחר מלחמת ששת הימים, תחת הכותרת "עתיד השטחים המשוחררים בארץ-ישראל המערבית ודרכי הטיפול בבעיית הפליטים הערביים"." (the Allon Plan was submitted to the Government on 26 July 1967, about a month after the war, under
  the title "The future of the liberated territories in western Eretz Israel and ways of handling the Arab refugee
  problem"); "תוכנית אלון לא התקבלה רשמית על ידי ממשלת אשכול ולא על ידי הממשלות שקמו אחריה. נערכו עליה דיונים, אך היא לא עמדה מעולם להצבעה בממשלה." (the plan was not formally adopted by the Eshkol government nor by the governments
  after it. It was discussed, but it never came to a vote in the Government). [S]
- **[C]** The day: 26 July 1967 (W2, "submitted") against 27 July 1967 (W1, "presented"); 26 July 1967 was a
  Wednesday and 27 July a Thursday (derived). The vote: a rejection by most ministers on 30 July (W1) against "never came
  to a vote" (W2). All kept. The design's §2.5 has W1's dates.
- The 1968 form of the plan is in FRUS (events-1967-70.md B1).
- **Open.** [GAP] A primary record of 26, 27 or 30 July 1967. The JTA searches of July to September 1967 found nothing
  (log, and events-1967-70.md B1).

### S2. Kfar Etzion: the legal advice (September 1967)

- The resettlement itself (27 September 1967) and the Cabinet decision ([C] 24 or 25 September 1967) are in
  events-1967-70.md B3.
- W3: "Meron, as legal counsel to the Israeli Foreign Ministry, wrote a secret memo for Prime Minister Levi Eshkol, who
  was considering re-establishing the Jewish settlement of Kfar Etzion"; "Meron's memo concluded that creating this new
  settlement in the Occupied Territories would be a violation of the Fourth Geneva Convention. Eshkol created the
  settlement anyway." [S]
- W4: "The Israeli cabinet decided to re-establish the settlement of Kfar Etzion despite receiving legal advice that
  establishing such settlements in occupied territory would be illegal under the Fourth Geneva Convention." [S]
- **Open.** [GAP] The memo's date and text. Neither revision gives a date. The page that published it (a 2008 blog post
  at southjerusalem.com, found in the Wayback Machine's capture index) could not be read: the Wayback Machine answered
  HTTP 429 and the live address redirects to another site (log). No other source for the memo was opened.

### S3. Hebron: the suburb plan and the 250 homes (March 1970)

- J1, dateline JERUSALEM, March 10, so the Knesset sitting was 9 March 1970 (derived; a Monday): "Plans to build a
  Jewish suburb next to the West Bank Arab town of Hebron were disclosed by Deputy Premier Yigal Allon in the Knesset
  yesterday." Allon said a master plan "was being drawn up by a committee of experts" and would not be ready "for a year
  of possibly longer" (as printed). He also announced "plans were under way for eight new settlements in the occupied
  territories, four of them in the Golan Heights, two on the West Bank and two in the Sinai Peninsula", and the item
  says "Twenty-two Jewish settlements have been set up in those areas since the June, 1967 war, most of them of the Nahal
  or para-military type". [S]
- J2, dateline JERUSALEM, March 26, so the Knesset vote was 25 March 1970 (derived; a Wednesday): "A Cabinet decision
  to build permanent housing for 250 Jewish families in the West Bank town of Hebron was approved by a large majority in
  the Knesset yesterday." "only the two Communist factions and Haolam Hazeh opposed it"; Mapam and the State List
  abstained. [S]
- The 1968 arrival is in events-1967-70.md B9 (April 1968); west-bank-localities-1967-77.md (Qiryat Arba row) has
  both April 1968 and the start of building at Kiryat Arba in the week of 15 September 1970 (JTA).
- **Open.** [GAP] The day of the Cabinet's own decision on the 250 homes (J2 gives only the Knesset's approval) and its
  text.

### S4. The Golan settlements

Cross-reference only: golan-localities-1967-77.md has the founding dates, the first being Merom Golan ([C] 14 or 15 July
1967). J1 adds that in March 1970 four of the eight planned new settlements were on the Golan Heights (S3).

### S5. The Galili document (September 1973)

- J3, dateline TEL AVIV, Sept. 4, so the vote was on 3 September 1973 (derived; a Monday): "The Labor Party Secretariat
  gave overwhelming approval yesterday to a series of proposals by Defense Minister Moshe Dayan that will open the way to
  private land purchases by Jews in the administered territories, increase Jewish settlement in the territories and
  reduce the Arab labor force presently working in Israel." "The various facets of the plan originated with Dayan and were drafted into
  document form by Minister-Without-Portfolio Israel Galili" (archive page text; our OCR garbled it, the sentence is legible on the rendered p. 2, where a stray line crosses part of it). "The document was approved a week ago by the
  Party's Executive"; "Approval was by 78 votes. There were several abstentions but no hands were raised against it." [S]
- W9: "התוכנית הובאה לאישור מזכירות מפלגת העבודה ב־3 בספטמבר 1973 כחלק ממצע הבחירות שלה." (the plan was brought for the approval of the Labor Party secretariat on 3 September 1973 as part
  of its election platform); "בעד התוכנית הצביעו 79 מחברי המזכירות, רבים נעדרו, בהם ח״כ לובה אליאב שהביע התנגדות למסמך." (79 members of the secretariat voted for the plan; many were absent,
  among them Arie Lova Eliav, who opposed the document). [S]
- **[C]** 78 votes (J3) against 79 (W9). The day agrees: 3 September 1973.
- **Open.** [GAP] The document's text. W9 links a copy on the Israel State Archives site, which is not reachable from
  this server.

### S6. Gush Emunim founded (February 1974)

- W10: "התנועה קמה רשמית מספר חודשים לאחר מלחמת יום הכיפורים, ב-7 בפברואר 1974 (ט"ו בשבט תשל"ד), בכינוס יסוד שנערך בביתו של הרב יואל בן נון באלון שבות." (the movement was founded formally some months after the Yom Kippur War, on 7 February 1974 (15
  Shevat 5734), at a founding meeting held in the home of Rabbi Yoel Bin-Nun at Alon Shvut). [S]
- W11: "Gush Emunim was founded by students of Zvi Yehuda Kook in February 1974 in the living room of Haim Drukman".
  [S]
- **[C]** The month agrees (February 1974, as the design's §2.5 has it); the day is W10's alone; the place differs
  (Bin-Nun's home at Alon Shvut, W10, against Drukman's living room, W11). Both kept. 7 February 1974 was a Thursday
  (derived); the Hebrew date is W10's and was not converted here.
- **Open.** [GAP] A contemporary source for the founding. The JTA searches of January to June 1974 found no item on it
  (log).

### S7. The 1974 attempts: near Nablus (June) and Sebastia (July)

- **June.** J5, dateline JERUSALEM, June 5 (so 5 June 1974): "Large contingents of Israeli troops surrounded a site
  near the Arab town of Nablus on the West Bank today in an effort to persuade a group of Jewish squatters to depart
  peacefully." "The incident occurred on the seventh anniversary of the 1967 Six-Day War". [S] W10: "ניסיון ההתנחלות הראשון של "גרעין אלון מורה" נעשה בט"ו בסיוון תשל"ד (5 ביוני 1974), יחד עם הרב צבי יהודה הכהן קוק בחווארה (ארבעה קילומטרים מדרום לשכם) שם הכריזו על הקמת 'אלון מורה'." (the
  first settling attempt of the Elon Moreh group was made on 15 Sivan 5734 (5 June 1974), with Rabbi Zvi Yehuda Kook,
  at Hawara, four kilometres south of Nablus, where they declared the founding of "Elon Moreh"). [S] The day agrees.
- **July.** W10: "כחודש לאחר מכן, ב-25 ביולי, עלו פעילי גוש אמונים וחברי-כנסת (בהם שרון ואהוד אולמרט, לימים שניהם יכהנו כראשי ממשלת ישראל), לתחנת הרכבת הנטושה סבסטיה." (about a month later, on 25 July, Gush Emunim activists and Knesset members (among them Sharon and Ehud Olmert, both later prime ministers of Israel) went up to the
  abandoned Sebastia railway station). [S] J6, dateline TEL AVIV, July 28 (so 28 July 1974, a Sunday, derived): "The 150
  settlers who sought to establish a settlement at Sebastia near Nablus on the West Bank averted a showdown with the
  government tonight by deciding to leave their encampment a few minutes before a government deadline." Peres had told
  "the 35 families, in line with a Cabinet decision Friday" (Friday 26 July 1974, derived) that they would be removed
  otherwise; "no government action was taken because of, first, the Sabbath, and then Tisha B'av." The item also says
  the area is "some eight miles northwest of Hebron", which does not fit its own "Sebastia near Nablus"; kept as
  printed. [S]
- **[C]** How the July attempt ended: the settlers decided to leave a few minutes before the deadline, their spokesman
  saying "they were not leaving of their own free will but that they did not want a confrontation with the Israel
  Defense Force" (J6), against
  "removed by force by the IDF" after a four-day ultimatum (W10, my translation of a later sentence in the same paragraph).
  Both kept.

### S8. Sebastia in 1975, and Kadum

- W10 lists further attempts at Sebastia "ניסיונות נוספים להתיישב בסבסטיה נעשו במהלך 1975 (ב-5 וב-19 במרץ וב-25 בנובמבר), ובכולם פונו המתיישבים ביום שבו הגיעו למקום." (further attempts to settle at Sebastia were made during 1975, on
  5 and 19 March and on 25 November, and in all of them the settlers were removed on the day they arrived). [S]
- W10 lists the 25 November 1975 attempt (removed the same day) separately from the mass ascent of 30 November 1975
  and the compromise of 8 December (later sentences of the same section, my summary). This is consistent with
  events-1970-77.md B14, which has the arrival "Sunday" (30 November 1975) from JTA and leaves the compromise day as 8 or
  9 December; west-bank-localities-1967-77.md (Qedumim row) reads the dateline "Dec. 8" on the PDF. B14 keeps its own
  [C] on the month (a JTA item of 6 January 1976 says the compromise was reached "in November"); this note did not
  reopen the JTA items.
- Kadum: cross-reference only (events-1970-77.md B14; west-bank-localities-1967-77.md Qedumim row: the move to the Kadum
  camp on 8 December 1975 [S]; the Cabinet statement of 9 to 10 May 1976 is in events-1970-77.md B14 only: the
  11 May 1976 bulletin, "decision of May 9").

### S9. Ofra (1975)

Cross-reference only: west-bank-localities-1967-77.md row 10 (night of 20 April 1975 from Hebrew Wikipedia, derived;
a UN record of a radio report of 7 May; JTA of 11 June 1975).

---

## Search log

All searches were run on 1 November 2026. JTA: the archive's WordPress API,
`https://www.jta.org/wp-json/wp/v2/archive?search=<query>&after=<from>T00:00:00&before=<to>T23:59:59&per_page=20`; the
count is the `x-wp-total` header; only the first page of 20 titles was seen.

| Query | Range | Count | Result |
|---|---|---|---|
| `Allon plan` | 1967-07-15 to 1967-09-30 | 15 | titles read; none on a plan |
| `Allon` | 1967-07-20 to 1967-08-10 | 23 | first 20 titles read; none on a plan or a Cabinet vote |
| `Herzog` | 1967-06-07 to 1967-06-25 | 4 | titles read; the one on the territories (9 June 1967) is already in model-basis-1957-77.md 3.3 |
| `administered territories` | 1967-06-01 to 1968-12-31 | 352 | not pursued (too broad) |
| `bridges` | 1967-09-01 to 1967-12-31 | 95 | first 20 titles read; J4 opened |
| `Jordan produce` | 1967-09-01 to 1967-09-20 | 0 | none |
| `Hebron` | 1970-01-15 to 1970-04-30 | 81 | first 20 titles read (April 1970); none opened |
| `Hebron settlement` | 1970-02-15 to 1970-04-10 | 8 | J1 and J2 opened; the 8 April item (Eban) title only |
| `Kiryat Arba` | 1970-01-01 to 1970-12-31 | 31 | first 20 titles read; the 16 September item is already in west-bank-localities-1967-77.md |
| `Galili` | 1973-08-15 to 1973-09-20 | 54 | first 20 titles read; none on the document |
| `Labor Party territories` | 1973-08-25 to 1973-09-15 | 7 | J3 opened |
| `Dayan Labor platform` | 1973-08-20 to 1973-09-15 | 4 | titles read; J3 among them |
| `Gush Emunim` | 1974-01-01 to 1974-06-30 | 2 | titles read (11 April, 11 January); neither on a founding |
| `Emunim` | 1974-01-01 to 1974-04-30 | 11 | titles read; none on a founding |
| `Gush Emunim` | 1974-07-01 to 1974-12-31 | 4 | titles read (October 1974 attempts); not opened |
| `Nablus settlers` | 1974-06-01 to 1974-06-15 | 1 | J5 opened |
| `Sebastia` | 1974-07-20 to 1974-08-05 | 7 | J6 opened; the other titles (29 July, 1, 2 and 5 August) not opened |

Other searches:

- UN A/8089 (U2), the text layer searched for `proclamation`, `7 June 1967`, `military governor`, `Allenby` and `open
  bridge`: `proclamation` occurs in para 142 (read) and in an annex on Jerusalem; no proclamation text is printed.
- UN A/6797-S/8158 (U1), the text layer searched for `proclamation`: 0 hits; `bridge`: the hits read are in O1.
- Wayback Machine capture index (`web.archive.org/cdx/search/cdx`, url `southjerusalem.com*`, filtered on `meron`):
  captures of `southjerusalem.com/2008/09/kfar-etzion-the-meron-opinion-and-the-illegality-of-settlement/` from 2008 to
  2024. Fetching the capture of 7 December 2023 returned HTTP 429 six times; the live address returned HTTP 307 to
  `https://haimwatzman.com`, whose matching path did not answer. Not read.
- Wikipedia (by the revisions above): English "Allenby Bridge" (revision 1350584658) and "Coordinator of Government
  Activities in the Territories" (revision 1375106211) were opened and give no date used here.

## Still open

- M2: the text and number of the first proclamations of June 1967.
- M3: a primary record of the coordination bodies of 1967-68.
- O2: the text of the 7 September 1967 procedure; any Government decision that opens the bridges.
- S1: a primary record of the Allon Plan's submission and the Cabinet's 30 July 1967 discussion.
- S2: the date and text of the legal opinion on Kfar Etzion.
- S3: the day and text of the Cabinet decision on the 250 homes in Hebron.
- S5: the Galili document's text.
- S6: a contemporary source for Gush Emunim's founding.
