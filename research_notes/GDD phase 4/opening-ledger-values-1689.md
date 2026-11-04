# The four deferred opening-ledger values: sourced draft for owner approval (#1689, research part of #2492)

Status: **draft for owner approval, research only**. An AI agent (Claude Code) opened every source below on 4 November 2026
and wrote the rows. Per `docs/CONTENT_POLICY.md` section 2 rule 6 and the owner's ruling on #2481, each row is recorded as
`checked by beriki770-ship-it, AI-assisted`, and the exact list of what was opened is in "What was opened". This note
changes nothing in `content/`, `sim/` or `unity/`, does not touch `Simulation.NewGame` or the Historical ledger fixture,
and does not change any `status`, `reviewed_by` or `reviewed_on`. It asks the owner to approve (or change) the proposals
in the last section before any wiring. The separate city-by-field proposal is #2493 and is not in this note.

## Question and result

`docs/rules/mvp.md` (R-POP-004, line 584) keeps four values Deferred: the work assignments of the employed share of each
census city, which census units are farmers, the formations' military qualification, and their combat experience. The
owner's ruling of 4 November 2026 (Q24, "do the recommended for all", recorded on
[#1689](https://github.com/eyalgolan/statecraft-israel/issues/1689#issuecomment-6623135448)) asks for a separate sourced
draft first. Result in one line each:

- **Employed assignments.** The CBS gives, for five of the twelve census cities, the working share, the unemployed share
  and the split by economic branch and occupation group, read on the scan. For the other seven only the national share
  exists ([D], a stand-in). No source allocates people to the game's job types: that conversion is the owner's, [GAP].
- **Farmer identities.** No source names which people, or which census unit, are farmers. What exists is a share: farmers
  were 0.8 to 2.4 per cent of the working Jewish population of the five CBS cities and 11.1 per cent (branch) or 12.4 per
  cent (occupation group) nationally. For the seven other cities there is no figure, [GAP].
- **Formation qualification and combat experience.** For all eight Israeli opening formations a dated source gives how the
  formation was raised (a Haganah Field Corps or Palmach origin) and, for seven of them, dated fights before 15 May 1948.
  For the Negev Brigade no dated fight before 15 May was found, [GAP]. No source states a "qualification" or "experience"
  in the game's terms, so every game value stays a proposal for the owner, and where the sources do not tell the
  formations apart the table says so instead of choosing.

## How to read this note

Tags: **[V]** opened and read by the agent on the day given, the quotation is verbatim from the page named; **[S]**
secondary (Wikipedia by revision, a lexicon or museum summary of a primary history); **[D]** derived from [V] figures by
the method stated; **[C]** two sources disagree and both are kept; **[GAP]** no source found. Every row says "a person's
check pending" through the tag `[V, agent read 2026-11-04; checked by beriki770-ship-it, AI-assisted]`, shortened below to
**[V*]** (the same wording for [S]: **[S*]**). The Hebrew quotations are the page's own text. One English quotation writes the page's em dash as `---`, and the diary quotation elides the archive's square-bracket glosses with `[...]`.

Games terms used here come from the repo, not the sources: `military.strength` untrained / trained / veteran (1, 2, 3) and
`military.combat_xp_full` 5 ticks (`content/gdd03-balance.yaml:355-366`), the civilian tier novice / trained / veteran
(R-POP-002, R-QUAL-008), and R-UNIT-006 ("Every IDF brigade is a `veteran_soldier`").

## 1. Employed work assignments

### 1a. The twelve census cities

The twelve are the cities of `research_notes/GDD phase 3/city-start-values-1948-cbs.md` ("Which cities"; census day 8
November 1948). The population and the five unemployed and working shares are that note's; the five shares were re-read on the scan today (Table 9, 1b), the populations were not re-opened (Statistical Abstract No. 14 Table 11 and No. 3 p. 133, second-hand from the city-start note, [S*]).

| City | Census pop. (8 Nov 1948) | Working share | Unemployed share | Employed (rounded to 100) | Tag |
|---|---|---|---|---|---|
| Jerusalem | 83,984 | 34.2% | 0.9% | 28,700 | [V*] shares, [D] count |
| Tel Aviv-Yafo | 248,459 | 42.6% | 0.8% | 105,800 | [V*] shares, [D] count |
| Haifa | 98,618 | 42.9% | 0.3% | 42,300 | [V*] shares, [D] count |
| Tiberias | 5,555 | 33.4% | 0.8% | 1,900 | [V*] shares, [D] count |
| Safed | 2,317 | 34.5% | 1.3% | 800 | [V*] shares, [D] count |
| Petah Tikva | 21,879 | 43.9% national | 0.6% national | 9,600 | [D] national stand-in, [GAP] city share |
| Rishon LeZion | 10,433 | 43.9% national | 0.6% national | 4,600 | [D] national stand-in, [GAP] city share |
| Rehovot | 12,522 | 43.9% national | 0.6% national | 5,500 | [D] national stand-in, [GAP] city share |
| Hadera | 11,811 | 43.9% national | 0.6% national | 5,200 | [D] national stand-in, [GAP] city share |
| Netanya | 11,589 | 43.9% national | 0.6% national | 5,100 | [D] national stand-in, [GAP] city share |
| Zikhron Ya'akov | 1,935 | 43.9% national | 0.6% national | 800 | [D] national stand-in, [GAP] city share |
| Rosh Pinna | about 350 (May 1948, Hebrew Wikipedia) | 43.9% national | 0.6% national | 200 (154 before rounding) | [D] national stand-in, [GAP] city share |

Derivation: employed = census population times the working share (both sexes), rounded to 100, the method of the city-start
note. Check on the five: 83,984 x 0.342 = 28,723; 248,459 x 0.426 = 105,844; 98,618 x 0.429 = 42,307; 5,555 x 0.334 =
1,855; 2,317 x 0.345 = 799. National: 314,781 / 716,678 = 43.9% working, 4,111 / 716,678 = 0.6% unemployed.

### 1b. Source rows

| Claim | Source and page | Printed text | Tag | Day read |
|---|---|---|---|---|
| Five cities' occupational status, both sexes: Jerusalem proprietors 7.2, co-ops 0.5, managerial 3.6, foremen 1.3, unpaid family 0.6, clerks and labourers 21.0; Tel Aviv 10.7, 0.7, 2.4, 2.0, 0.5, 26.3; Haifa 8.4, 0.8, 2.7, 1.4, 0.5, 29.1; Tiberias 8.2, 0.8, 2.5, 1.1, 0.5, 20.3; Safed 8.2, 0.7, 2.5, 0.8, 0.7, 21.6 (sums 34.2, 42.6, 42.9, 33.4, 34.5) | CBS, Statistical Abstract of Israel No. 1 (1949/50), Table 9 "Jewish population, by occupational status and sex (%)", printed p. 26 (PDF p. 49), `https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1950_num1.pdf`; page image viewed | the six rows under "Working Population", column "Both Sexes", for each city, as listed; "Unemployed" row: Tel Aviv 0.8, Jerusalem 0.9, Haifa 0.3, Tiberias 0.8, Safed 1.3 | [V*] | 2026-11-04 |
| The table counts Jews only | same page | title "JEWISH POPULATION, by OCCUPATIONAL STATUS and SEX (%)"; the city populations in 1a include non-Jews, so the derived counts are high where non-Jews were many (Haifa, Tel Aviv-Yafo) | [V*] for the title, [D] for the caveat | 2026-11-04 |
| National, 8 Nov 1948: 716,678 Jews, working population 314,781, unemployed 4,111 | CBS, Statistical Abstract No. 2 (1950/51), Table 9 "Jewish population, by occupational status and sex", printed p. 24 (PDF p. 54), `https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1951_num2.pdf`; page image viewed | "Total Population 716,678", "Total Working Population 314,781", "Unemployed 4,111"; its own "Not stated" row is 30,668 inside the working population (the six city rows of No. 1 have no such row) | [V*] | 2026-11-04 |
| Jobs by economic branch, five cities, both sexes (below) | CBS, Statistical Abstract No. 1, Table 7 "Jewish working population, by economic branch and sex (in percentages)", printed p. 24 (PDF p. 47); page image viewed | rows as in the table below | [V*] | 2026-11-04 |
| Jobs by occupation group: Agriculture, both sexes: Tel Aviv 1.2, Jerusalem 1.3, Tiberias 2.3, Haifa 1.1, Safed 2.4 | same, Table 8 "by occupation group and sex", printed p. 25 (PDF p. 48); page image viewed | "Agriculture ... 1.2 ... 1.3" (Tel Aviv, Jerusalem), "2.3 ... 1.1" (Tiberias, Haifa), "2.4" (Safed) | [V*] | 2026-11-04 |
| National jobs by branch, 8 Nov 1948 (counts, below) | CBS, Statistical Abstract No. 2, Table 5 "Jewish working population, by economic branch and sex", printed p. 19 (PDF p. 49); text layer read, total checked (the nine rows sum to 314,781) | "All Branches 314,781", "Agriculture 34,847", "Industry and Crafts 85,785" and the rest as below | [V*] | 2026-11-04 |
| Soldiers and new immigrants in the national figure | Statistical Abstract No. 2, English text, as quoted in the city-start note (PDF p. 169) | not re-opened today: the city-start note's own record (read 2026-10-15) says mobilised soldiers were classified by their civilian occupation, and most new immigrants still in immigrant centres as unemployed or in an unstated branch | [S*] (second-hand, from the repo's note) | 2026-11-04 |

Jobs by economic branch, both sexes, per cent of the working population (Table 7; the five cities) and of the national
working population including "not stated" (Table 5, derived by dividing each count by 314,781):

| Branch | Tel Aviv | Jerusalem | Haifa | Tiberias | Safed | National (counts) |
|---|---|---|---|---|---|---|
| Agriculture | 0.9 | 1.0 | 0.8 | 2.1 | 2.4 | 11.1 (34,847) |
| Citrus and plantation | 0.2 | 0.1 | 0.1 | 0.1 | 0.1 | 1.0 (3,278) |
| Industry and crafts | 36.6 | 26.9 | 31.3 | 23.3 | 27.2 | 27.3 (85,785) |
| Building | 5.5 | 4.7 | 7.7 | 9.3 | 12.3 | 5.2 (16,390) |
| Transport and communications | 6.7 | 5.0 | 11.0 | 8.0 | 6.9 | 5.3 (16,740) |
| Trade and finance | 19.0 | 15.7 | 18.7 | 15.2 | 13.9 | 11.5 (36,355) |
| Personal services | 9.8 | 9.3 | 9.2 | 12.4 | 10.0 | 8.4 (26,468) |
| Public services | 21.3 | 37.3 | 21.2 | 29.6 | 27.2 | 17.2 (54,123) |
| Not stated | none | none | none | none | none | 13.0 (40,795) |

The national column is not comparable row by row with the city columns: the city tables have no "not stated" row and were
taken from a sample of about 10 per cent for Jerusalem, Tel Aviv and Haifa (the section note, per the city-start note),
and all of it is provisional.

### 1c. What the sources do not give

| Item | Result | Tag |
|---|---|---|
| A city share for the seven non-CBS-table cities | Tables 7, 8 and 9 of No. 1 name five cities only. Statistical Abstract No. 2 Table 7 (PDF p. 39, "Population, by locality") gives populations, not occupations | [GAP] |
| Allocation of the employed to the game's assignment types (what each person does in the game) | The sources give economic branch and occupation group, not the game's job types; mapping branch to assignment is a game conversion | [GAP] (owner) |
| Job slots (capacity) | The city-start note records no source for slots; with unemployment near 1 per cent only "slots were at least the employed count" follows | [GAP] |
| Conflict | The national share (43.9%) and the five cities' shares (33.4% to 42.9%) differ by 1.0 to 10.5 points (Tiberias 33.4% is the furthest below); none is dropped, the national value is only a stand-in | [C] kept |

## 2. Farmer identities

### 2a. Evidence

| Claim | Source and page | Printed text | Tag | Day read |
|---|---|---|---|---|
| Nationally on 8 Nov 1948, 34,847 of 314,781 working Jews were in agriculture by branch (11.1%), 38,881 by occupation group (12.4%) | CBS, Statistical Abstract No. 2, Tables 5 and 6, printed p. 19 (PDF p. 49) | "Agriculture 11,103 / 23,744 / 34,847" (Table 5, females, males, both) and "Agriculture 11,650 / 27,231 / 38,881" (Table 6); the text layer misreads "11,650" as "11^650", so the female figure is [S*] and the both-sexes totals are used | [V*] totals | 2026-11-04 |
| The status table (not the branch table) has a separate count of members of co-operatives and kibbutzim; the two tables are different classifications and the sources do not say how they overlap | same, Table 9, printed p. 24 (PDF p. 54) | "Members of Co-operatives and Kibbutzim 16,258 / 23,798 / 40,056" | [V*] | 2026-11-04 |
| In five cities agriculture was 0.8% to 2.4% of the working Jews (branch table) and 1.1% to 2.4% (occupation table) | Statistical Abstract No. 1, Tables 7 and 8, printed pp. 24, 25 (PDF pp. 47, 48) | see 1b | [V*] | 2026-11-04 |
| Derived farmers in those five cities (employed from 1a times the agriculture share): Jerusalem 287 to 373, Tel Aviv 953 to 1,270, Haifa 338 to 465, Tiberias 39 to 43, Safed 19, together about 1,640 to 2,170, or 4.7% to 5.6% of the national 34,847 to 38,881 | the rows above | not applicable | [D] | 2026-11-04 |
| Which people or units were farmers | no census table seen names a person, a unit or a household; the cities' shares are percentages of a working population | none | [GAP] | 2026-11-04 |
| Farm share for Petah Tikva, Rishon LeZion, Rehovot, Hadera, Netanya, Zikhron Ya'akov, Rosh Pinna | not in Tables 7, 8, 9 (five cities only) or Table 5 and 6 (national); the agricultural census that Statistical Abstract No. 1 (PDF p. 139) says "is now being carried out" has no results in either abstract, and by its own text covered Galilee Arab villages and the Sharon | none | [GAP] | 2026-11-04 |

Statistical Abstract No. 1 (PDF p. 139, the explanatory notes) says "A general agricultural census is now being carried out
within the framework of the world agricultural census initiated by the United Nations Food and Agriculture Organization
(FAO). During the winter of 1949/50, it was held in Arab villages in Galilee, and during the summer in the Jewish and Arab
villages of the Sharon." [V*] That census covers villages, not the twelve cities' farmers, and its results were not found in
either abstract, so it closes nothing here.

### 2b. What this does and does not support

- Supported: the census cities were not where most 1948 farmers were. The five CBS cities together held about
  5 to 6 per cent (4.7% to 5.6%, pairing branch with branch and occupation with occupation) of the national agricultural workers [D], so a Historical start that makes farmers a large share of a city's
  units would contradict the CBS tables for those five cities.
- Not supported: any statement that a given census unit is a farmer, or that a given city has a given number. The
  owner's approved 1:2:1 novice / trained / veteran ratio per city (R-POP-004) is not evidence about who farms, and this
  note does not infer an identity from it or from a city's distance to a field (the issue forbids both).
- Which cities supply the farmer of which field is #2493, not this note.

## 3. Formation qualification and combat experience

### 3a. The opening formations

R-UNIT-006 starts one unit per `content/map/units_1948.yaml` row that is not hidden, is army, has a 15 May 1948 strength
and an anchor window covering 15 May. Read from the file today, the Israeli rows that meet it are the eight below.
Not in the opening inventory: the 7th Brigade (arrives 24 May, R-UNIT-009), Yiftach (first anchor 11 July), the 8th and
Oded brigades (hidden), the squadron and the two ships (no strength). The strength is the row's `strength.value`, whose
source is Ben-Gurion's diary for 15 May 1948 (see 3b) except for Harel and Negev, read from "Palmach – about 6,000, of whom
1,300 are in Harel [...], 1,300 in the Negev".

| Formation (id) | Strength 15 May 1948 | How it was raised | Tag |
|---|---|---|---|
| 1st Golani (`golani`) | 4,095 | Haganah Field Corps (HISH) battalions of the Jezreel and Jordan valleys, formed in the second half of February 1948 | [S*] |
| 2nd Carmeli (`carmeli`) | 2,289 | Field Corps and Guard Corps battalions of Haifa and the Zevulun district, formed late February 1948 | [S*] |
| 3rd Alexandroni (`alexandroni`) | 3,588 | Field Corps battalions between Zikhron Ya'akov and Ramat Gan, formed at the start of the war | [S*] |
| 4th Kiryati (`kiryati`) | 2,504 | Tel Aviv Field Corps companies, formed 8 February 1948 | [S*] |
| 5th Givati (`givati`) | 3,229 | Field Corps battalions of Tel Aviv and the south, formed November 1947 | [S*] |
| 6th Etzioni (`etzioni`) | 3,166 | Field Corps and Guard Corps of Jerusalem, formed on the eve of the war | [S*] |
| 10th Harel (`harel`) | 1,300 | Palmach battalions in the Jerusalem hills, brigade formed 16 April 1948 | [S*] |
| 12th Negev (`negev`) | 1,300 | Palmach 2nd and 8th battalions, brigade formed March 1948 | [S*] |

### 3b. Evidence rows

All Hebrew is the page's own wording. "HISH" is the Hebrew abbreviation `חי"ש` (Haganah Field Corps), "HIM" `חי"ם` (Guard
Corps); the lexicon uses both without defining them in the entries read, and en.wikipedia's Haganah article names "the
Field Corps, Guard Corps, and the Palmach strike force" (revision 1381789595).

| Formation | Claim | Source | Printed text | Tag | Day read |
|---|---|---|---|---|---|
| all | On 15 May 1948 the brigades' strengths and the staff total | Ben-Gurion's diary, 15 May 1948, the Ben-Gurion Archive's English transcription, item 221910, via the Wayback copy `https://web.archive.org/web/20240203024717/https://bengurionarchive.bgu.ac.il/en/node/8733` | "Palmach – about 6,000, of whom 1,300 are in Harel [...], 1,300 in the Negev, 1,200 in the north, 1,000 women; Golani has 3,573 + 522 paid recruits; Carmeli 2,289; Alexandroni 3,588; Kiriati 2,504; Givati 3,229; Etsiyyoni 3,166 – the total for all brigades is about 24,400. In addition: Training Department 398, Artillery Corps 659, Air Force 675, Engineering Corps 150, Military Police 168, Transportation 1,097, Total [for] divisions and services 4,161; [new recruits] in processing 1,719, overall total 30,573." (the square-bracket glosses are the archive's) | [V*] | 2026-11-04 |
| Golani | Fresh recruits on the day | same page | "Golani [Brigade] enlisted 400 [troops] over the course of a week, Palmach [Haganah strike force] has 300 for the Negev." | [V*] | 2026-11-04 |
| Golani | Formed 2nd half of February 1948; battalions; first jobs; fights before 15 May | Haganah Heritage Organization, lexicon, "Golani" (brigade 1), `https://www.hahagana.org.il/lexicon/lexiconpage/?itemId=47295` | "חטיבה 1. חטיבת חי"ש שהוקמה במחצית השנייה של פברואר 1948"; "בתחילה פעלו כוחות גולני בהבטחת התחבורה ובפשיטות גמול וסייעו להגנת היישובים"; "כוחות מגולני תיגברו את משמר העמק, שהותקפה בליל 5/4 באפריל 1948"; "נכנס גדוד ברק של החטיבה למערכה רצופה על טבריה, שהחלה ב-8 באפריל והוכרעה ב- 18 באפריל"; "לקראת סוף אפריל - ראשית מאי 1948 תפסו כוחות החטיבה את בנייני המשטרה בצמח וגשר"; "לקראת הפלישה נכנעה בית-שאן, לאחר שכותרה על-ידי יחידות גדוד 1 מגולני" | [V*] | 2026-11-04 |
| Golani | Four battalions at the start (the museum's order of battle gives five, 11 to 15) | same lexicon page: "בראשית דרכה היו בחטיבה 4 גדודים"; Haganah Museum, the brigades' order-of-battle page, `https://hahagana.org.il/story/chapters/?itemId=48280` | lists "חטיבה 1 גולני", battalions 11 Alon, 12 Barak, 13 Gideon, 14 Dror, 15 Goren (split from 14) | [C] kept, [V*] both | 2026-11-04 |
| Carmeli | Formed late Feb 1948, three battalions first; reprisals, an ambush on 17 March, support at Mishmar HaEmek and Ramat Yohanan in April, the battle of Haifa from 22 April, Operation Ben-Ami from mid-May | Haganah Heritage Organization, lexicon, "Carmeli" (brigade 2), `https://hahagana.org.il/lexicon/lexiconpage/?itemId=47620` | "חטיבת חי"ש, שהוקמה בסוף פברואר 1948"; "ב-17.3 הושם בקרית-מוצקין, בשיתוף כוח פלמ"ח, מארב לשיירת תחמושת ערבית מלבנון, והיא הושמדה"; "פלוגה מהחטיבה השתתפה בהתקפת-הנגד באזור משמר-העמק ואילו ברמת-יוחנן הכריעו כוחות כרמלי את הקרב"; "למחרת החטיבה תקפה במורד מהדר-הכרמל לעבר הנמל"; "במבצע בן-עמי באמצע מאי הועבר כוח בים לנהריה"; "ב-14.5 הגיעו כוחות לכל יישובי הגליל המערבי, לבד מיחיעם" | [V*] | 2026-11-04 |
| Alexandroni | Formed at the start of the war from Field Corps battalions; Arab-held road fights; fights before 15 May | same site, lexicon, brigade 3, `https://www.hahagana.org.il/lexicon/lexiconpage/?itemId=47121` | "הוקמה בתחילת מלחמת העצמאות מגדודי החי"ש של ההגנה בנפות שבין זכרון-יעקב לרמת- גן-גבעתיים"; "ב-5.3.1948 פינו כוחות החטיבה את מחצבות מגדל-צדק"; "ב-9.5 תקפו את כפר קרע בפתח עמק עירון"; "בדרום השתתפה החטיבה, למעט גדוד 31, במבצע חמץ"; "ב-13.5 כבשו כוחותיה את כפר-סבא הערבית, והדפו את תגבורות צבא ההצלה והלגיון. למחרת תקפו את טירה, מצפון לרמת-הכובש, אך נכשלו." | [V*] | 2026-11-04 |
| Alexandroni | Formation date: the repo row says "November 1947 (the Haganah's decision of 7 November)"; the lexicon says "at the start of the war" with no date | `content/map/units_1948.yaml` row `alexandroni`, `formed`; the lexicon page above | not re-derived here | [C] kept, owner to see both | 2026-11-04 |
| Kiryati | Formed 8 February 1948 from Tel Aviv Field Corps companies, 2,504 serving "on the eve of the state"; fought in Operation Hametz and received Jaffa's surrender on 13 May | he.wikipedia, "חטיבת קרייתי", revision 44030601 | "החטיבה הוקמה בתל אביב ב-8 בפברואר 1948 על בסיס פלוגות חי"ש"; "מספר המשרתים בחטיבה ערב הקמת המדינה היה 2,504"; "ב-13 במאי 1948 קיבל לידיו מפקד החטיבה, מיכאל ("ג'יימס") בן-גל, את כתב הכניעה של ערביי יפו" | [S*] | 2026-11-04 |
| Kiryati | Failed assault at Tel al-Rish during Operation Hametz (25 to 30 April) | en.wikipedia, "Operation Hametz", revision 1370632906 | "The Kiryati Brigade failed in its assault on the southern Jaffa suburb of Tel al-Rish." | [S*] | 2026-11-04 |
| Kiryati | The February 1948 decision to set it up, after incidents on the Tel Aviv-Jaffa border | Haganah Heritage Organization, lexicon, "Givati" (brigade 5), `https://hahagana.org.il/lexicon/lexiconpage/?itemId=47227` | "גרמו בפברואר 1948 להחלטה להקים בעיר את חטיבת קרייתי" | [V*] | 2026-11-04 |
| Kiryati | A Haganah Heritage lexicon entry of its own | searched `hahagana.org.il` with the brigade's Hebrew name; the entry was not found | none | [GAP] for a primary-institution entry | 2026-11-04 |
| Givati | Formed November 1947; raids in January 1948; Operation Nachshon raid on the Hasan Salama headquarters (April); Operation Barak, a bridge blown 11 to 12 May | Haganah Heritage Organization, lexicon, brigade 5, `https://hahagana.org.il/lexicon/lexiconpage/?itemId=47227` | "חטיבת חי"ש שהוקמה בנובמבר 1947 במרחבי תל-אביב והדרום"; "פשיטה על הכפר ערב-סוכריר, ממערב ליבנה, בליל 25/24 בינואר 1948"; "לקראת המבצע פשט כוח מהחטיבה על מפקדת חסן סלאמה, ליד באר-יעקב, השמידה וגרם לאויב אבדות כבדות"; "בעת המבצע פוצץ בליל 12/11 במאי גשר נחל האלה" | [V*] | 2026-11-04 |
| Givati | Formation date: November 1947 in the lexicon, December 1947 in `content/map/units_1948.yaml` (`formed`) | the lexicon page above; the units file row `givati` | "חטיבת חי"ש שהוקמה בנובמבר 1947"; `formed: December 1947` | [C] kept | 2026-11-04 |
| Givati | A fifth battalion formed on 15 May; 200 fighters given to the Negev Brigade and 200 to Jerusalem | same page | "ב-15.5.1948 הוקם גדוד חמישי"; "גבעתי הפרישה 200 מלוחמיה לחטיבת הנגב ו-200 נוספים נשלחו לירושלים" | [V*] | 2026-11-04 |
| Etzioni | Formed on the eve of the war; Jerusalem, Etzion Bloc, potash works; three Field Corps and four Guard Corps battalions; fights before 15 May; Etzion Bloc fell 13-14 May | same site, lexicon, brigade 6, `https://www.hahagana.org.il/lexicon/lexiconpage/?itemId=47926` | "חטיבת חי"ש, שהוקמה ערב מלחמת העצמאות"; "הקימה שלושה גדודי חי"ש, "מוריה", "מכמש" ו-"בית חורון", וארבעה גדודי חי"ם"; "כוחות החטיבה השתתפו במבצע נחשון, בעיקר בהגנת היישובים ממערב לירושלים ובכפר קסטל"; "ב-12 במאי פתח כוח ירדני בהתקפה על יישובי גוש עציון"; "יישובי הגוש הוכרעו ואנשיהם הלכו בשבי בבוקר ה-14 במאי" | [V*] | 2026-11-04 |
| Harel | Formed 16 April 1948; Operations Harel (15 to 21 April), Yevusi (22 April to 1 May, Nebi Samuel failed, San Simon succeeded), Maccabi (1 to 16 May); its own training staff | he.wikipedia, "חטיבת הראל", revision 43805226 | "כחטיבה נוסדה ב-16 באפריל 1948, ולמפקדה הראשון מונה יצחק רבין"; "בימים 15-21 באפריל פעלה החטיבה במבצע הראל"; "ספגה החטיבה אבדות קשות בקרב נבי סמואל, שנכשל ובקרב סן סימון, שהצליח"; "במבצע מכבי (א' וב'), שנערך בימים 1–16 במאי 1948, טיהרה החטיבה את הפרוזדור"; "המדריכים החטיבתיים שהיו מסופחים למטה השגיחו על אימוניהם של הגדודים" | [S*] | 2026-11-04 |
| Harel | Losses: 33 killed at Nebi Samuel, 19 at Katamon (en); 34 and 18 (Palmach memorial site, as en.wikipedia quotes it) | en.wikipedia, "Harel Brigade", revision 1318672396 | "sustained thirty-three killed in the battle for Nebi Samuel and nineteen dead in the Katamon neighborhood"; "Thirty-four were killed at Nabi Samuel and eighteen in Katamon." | [C] kept, [S*] both | 2026-11-04 |
| Harel | The 5th battalion trained and fought where it was based until the end of March 1948 | he.wikipedia, "חטיבת הראל", revision 43805226 | "הגדוד התאמן ולחם במקומות מושביו עד סוף מרץ 1948" | [S*] | 2026-11-04 |
| Negev | Formed March 1948 from the Palmach 2nd and 8th battalions, four battalions later; commander Sarig | he.wikipedia, "חטיבת הנגב", revision 44106930; en.wikipedia, "Negev Brigade", revision 1366467719 | he: "החטיבה הוקמה על בסיס שני גדודי פלמ"ח: הגדוד השני בפיקודו של משה נצר, והגדוד השמיני בפיקודו של חיים בר-לב"; en: "The brigade was founded in March 1948 with two battalions, the 2nd and 8th." | [S*] | 2026-11-04 |
| Negev | Origin: the 2nd and 8th Palmach battalions (both Wikipedias) against the museum's list of battalions 7, 8 and 9, with the 2nd listed under Yiftach | he.wikipedia and en.wikipedia as above; `https://hahagana.org.il/story/chapters/?itemId=48280` | museum: "חטיבה 12  פלמ"ח הנגב", "הגדוד השביעי "באר שבע"", "הגדוד השמיני "הנגב הדרומי"", "הגדוד התשיעי גדוד הפשיטה הממוכן" and, under Yiftach, "הגדוד השני "הנגב" (היה מסופח לחטיבת הנגב רוב המלחמה)" | [C] kept | 2026-11-04 |
| Negev | Dated fights before 15 May 1948 | the two articles above, the Ben-Gurion diary page, the Knesset "Independence War" PDF cited on the row (opened, no such date), he.wikipedia's article read through for earlier dates (it says the Egyptian army reached Negev settlements on 15 May, so not before), the Palmach site's organisation page `https://www.palmach.org.il/history/database/?itemId=5523` | the dated engagements in them (Operation Pharaoh 25-26 May, Palesheth 3 June) are after 15 May | [GAP] | 2026-11-04 |
| Palmach (Harel, Negev) | The Palmach was founded 14 May 1941; members "received specialist training in guerrilla tactics and sabotage" | en.wikipedia, "Haganah", revision 1381789595 | "On May 14, 1941, the Haganah created the Palmach (an acronym for Plugot Mahatz---strike companies), an elite commando section, in preparation against the possibility of a British withdrawal and Axis invasion of Palestine. Its members, young men and women, received specialist training in guerrilla tactics and sabotage." | [S*] | 2026-11-04 |
| Palmach (Harel, Negev) | Palmach size in May 1948: three readings kept side by side | en.wikipedia, "Palmach", revision 1381789687; the Ben-Gurion diary above | en: "In May 1948 the Palmach had 2,200 permanently mobilised members." and "A different source puts the size of the Palmach as 3,000 at the end of November 1947, and, following the mobilization of 3,000 reserves, five battalions were formed by May 1948, consisting of 5,000 fighters of whom 1,200 were women."; diary: "Palmach – about 6,000" | [C] kept, none chosen | 2026-11-04 |
| Palmach battalions | The Palmach moved to battalion organisation in the first half of 1945 and kept four battalions until the war | Palmach House (palmach.org.il), "Palmach staff / organisation" page, `https://www.palmach.org.il/history/database/?itemId=5523` | "ההחלטה יושמה, בהדרגה, במחצית הראשונה של שנת 1945; הוקמו ארבעה גדודים"; "המבנה האירגוני של ארבעה גדודים התקיים בפלמ"ח עד לראשית מלחמת העצמאות" | [V*] | 2026-11-04 |
| Earlier service | About 30,000 Palestinian Jews served in the British Army in the Second World War; the Jewish Brigade had 5,000 | en.wikipedia, "Haganah", revision 1381789595 | "All in all, some 30,000 Palestinian Jews served in the British army during the war." and "The Jewish Brigade Group consisted of 5,000 soldiers" | [S*] | 2026-11-04 |
| Earlier service | How many of each brigade's 15 May men had served in the British Army or in earlier Haganah courses | none found in the pages above | none | [GAP] | 2026-11-04 |
| Training | Staff and recruit numbers outside the brigades on 15 May | Ben-Gurion's diary, as above | "Training Department 398"; "[new recruits] in processing 1,719" | [V*] | 2026-11-04 |

### 3c. What the evidence separates and what it does not

Historical evidence, kept apart from any game value:

- **Raised from the Field Corps (part of the Haganah organised in territorial battalions):** Golani, Carmeli,
  Alexandroni, Kiryati, Givati, Etzioni. **Raised from the Palmach strike force, trained from 1941 and organised in
  battalions from 1945:** Harel, Negev. [S*, V*]
- **Fought before 15 May 1948 with a dated source:** Golani (Tiberias, Mishmar HaEmek, Tsemach), Carmeli (Haifa, Ramat
  Yohanan, Ben-Ami), Alexandroni (Migdal Zedek, Kafr Qara, Kfar Saba, Hametz), Kiryati (Hametz, Jaffa), Givati (Nachshon,
  Barak), Etzioni (Nachshon, Qastel, Etzion Bloc), Harel (Harel, Yevusi, Maccabi). **Negev: none dated, [GAP].**
- **Brigades were new on the day.** The lexicon dates Golani and Carmeli to late February 1948, he.wikipedia dates Kiryati (8 February) and Harel (16 April), and
  the diary records 400 Golani recruits "over the course of a week", so a brigade's men are not all veterans of one
  unit. The sources do not give how many men had earlier training or British service. [GAP]

No source states a rank order of experience or a level of qualification. A statement such as "Palmach brigades were more
experienced than Field Corps brigades" is not made by any row above and is not made here.

## Owner-approval table

Everything below is a **proposal**. Each row separates what the sources support (left) from the game value the owner could
approve (right). Nothing is written to `content/` or the ledger until approved, and no default stands in for a [GAP].

| # | Category | Supported by sources | Proposal for approval | Open |
|---|---|---|---|---|
| 1 | Employed share, Jerusalem, Tel Aviv-Yafo, Haifa, Tiberias, Safed | Working share 34.2 / 42.6 / 42.9 / 33.4 / 34.5%, [V*] | Employed units = census units times the working share (1a column "Employed") | Whether the Jewish-only share applies to all residents (the table counts Jews only) |
| 2 | Employed share, the other seven cities | Only the national 43.9%, [D] | Option: the national share, labelled a stand-in (the city-start note's design default), or no employment for these cities | [GAP] for each city; owner to approve the stand-in or leave them without employment |
| 3 | What the employed do in the game | Economic branch and occupation group percentages for five cities, national counts, [V*] | Allocate by branch only for the five cities if the owner maps branches to assignments | [GAP] for the mapping and for the seven cities; no job-slot source |
| 4 | Farmer identities | A share of the working Jews: 0.8 to 2.4% in five cities, 11.1 to 12.4% nationally, [V*], derived counts of 19 to 1,270 per city, [D] | None for identities; an option for the owner: treat the derived city counts as an upper bound if a count is wanted | [GAP] for which units; [GAP] for the seven other cities; no inference from 1:2:1 or distance |
| 5 | Qualification, the six Field Corps brigades | Raised from Field Corps battalions between November 1947 and February 1948; dated pre-15-May fights for each, [S*, V*] | One qualification for all six, or a split by the owner; the evidence does not rank them | The owner decides the level on the game's scale; training before 1948 is [GAP] |
| 6 | Qualification, Harel and Negev | Palmach, trained from 1941, [S*]; battalions since 1945, [V*] | Same, owner's choice; evidence supports "Palmach origin" only | Palmach size conflict [C] (2,200 / 5,000 / about 6,000) |
| 7 | Combat experience, seven formations with dated fights | Dated engagements before 15 May, 3b | Owner decides ticks of the 5; the sources give engagements, not a count of engagements per brigade | A tick per engagement is a game rule, not a source |
| 8 | Combat experience, Negev | No dated fight before 15 May | Cannot be derived from sources | [GAP] |

Owner approval is requested before any `Simulation.NewGame` wiring or any change to the Historical ledger fixture.

## What was tried and failed

- `www.izkor.gov.il` (Harel memorial rock) returned HTTP 403 to a plain fetch on 4 November; the Izkor rows the units file
  cites were therefore not re-read, and no Izkor claim is made in this note.
- `moreshet.netanya.muni.il` (the two Alexandroni pages the units file cites) timed out twice; Alexandroni rests on the
  Haganah Heritage lexicon and museum pages only.
- The Haganah Heritage lexicon has no entry for Kiryati that a search reached; Kiryati rests on he.wikipedia (by revision),
  en.wikipedia's Operation Hametz article and the Givati entry.
- A search for a CBS or archive table of occupations for the seven other cities returned none; the results listed
  Wikipedia mirrors and unrelated pages.
- No source on British Army service by brigade, or on courses taken before 1948, was found.

## What was opened

On 4 November 2026 the agent opened: the CBS Statistical Abstract No. 1 PDF, Tables 7, 8, 9 as page images (PDF pp. 47,
48, 49) and No. 2, Table 9 as an image (PDF p. 54) and Tables 5 and 6 (PDF p. 49, text layer and image rendered); the
Ben-Gurion Archive's 15 May 1948 diary page (Wayback copy); Haganah Heritage Organization pages itemId 47295, 47620,
47227, 47926, 47121, 48280 and 49622; the Palmach House page itemId 5523; en.wikipedia revisions 1381789595 (Haganah),
1381789687 (Palmach), 1370632906 (Operation Hametz), 1318672396 (Harel Brigade), 1366467719 (Negev Brigade), 1368286375
(Operation Nachshon); he.wikipedia revisions 44030601, 43805226, 44106930. A person has not yet opened the same pages.
