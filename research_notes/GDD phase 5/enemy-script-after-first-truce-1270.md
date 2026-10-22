# The Arab forces' positions from 8 July 1948 to the armistices: anchors for the enemy script

Status: draft; AI-read; every anchor is `to_verify`. A person must open every cited page and check every line before
any row of `content/map/units_1948.yaml` changes.

Researched 2026-10-21 by an AI agent for issue #1270, the research slice only. Every URL below was opened on that day
unless the entry says otherwise. Feeds R-ENEMY-002 (#1263, the script machinery, not merged when this was written). This
note changes no content file and no code. It answers no owner question: the proposed windows in each table are agent
proposals, and the questions in section 9 are for the owner and the reviewer.

## Purpose

After #1263, each Arab force in Historical mode marches toward its first 11 June 1948 anchor that cites at least two
sources other than Wikipedia and Wikidata, until the first truce ends on 8 July 1948. After that it falls back to the
simple goals. #1270 asks for the script to go on: each force marches to its next sourced position on its sourced date,
through the Ten Days (8 to 18 July 1948), the second truce (18 July to 15 October 1948) and the autumn and winter
operations, up to its front's armistice (Egypt 24 February, Lebanon 23 March, Jordan 3 April, Syria 20 July 1949; the
same dates as `end_basis: front_armistice` in `units_1948.yaml`).

This note gives, for each of the six forces the issue names (the Egyptian, Iraqi, Syrian and Lebanese armies, the Arab
Legion and the ALA), the places a source puts the force at after 8 July 1948, with the date, the page and the exact
words. Where no source places a force, section 9 says so, and the table has no row for it.

## Conventions

- **Anchor ids** (E1, L1, ...) are this note's, for review only. They are not content ids.
- **Window** is the agent's proposal in the format of `units_1948.yaml` (`from` inclusive, `to` exclusive). `from` is
  the first date a cited source puts the force at the place. `to` is the date a cited source moves the force or ends
  the position (`end_basis: source`), or the front's armistice where no source gives an end (`front_armistice`).
  "precision: month" marks a window edge the source gives only as a month or as "early" or "late" in a month.
- **Non-wiki sources** counts the independent sources other than Wikipedia and Wikidata that support the place in the
  window, so the reviewer can see which anchors meet #1263's two-source rule. Two passages of one book count once.
- **Quotes** are exact, copied from the text the agent read. Words split by a line break in the scan are joined.
  `[...]` marks a cut, and `[...]` or a bracketed role replaces most commanders' names. Commanders are named by role
  only in this note's own text (the convention of `arab-forces-1948.md`); a few quotes keep a name where cutting it
  would break the sentence.
- **O'Ballance's text is the Internet Archive's OCR.** Obvious OCR errors are corrected in the quote and listed in the
  quote's line ("OCR: ..."). His page numbers come from the OCR's page footers; where a footer is missing, the page was
  counted from the footers around it, and the quote says so.
- **Dupuy's pages** are the printed page numbers of the Stanford scan (PDF page = printed page minus 40).
- **Source order** follows `docs/CONTENT_POLICY.md` §1: Israeli official sources and mainstream Israeli
  historiography first. In this session the Israeli sources reached were the Ben-Gurion diary (the Ben-Gurion Archive's
  English transcription) and the armistice agreements Israel signed. Lorch was not reached (section 8). Most anchors
  therefore rest on Dupuy and O'Ballance, which `units_1948.yaml` already cites, and on the armistice texts.
- **Sensitive events** near these positions (Lydda and Ramle in July 1948, the Faluja civilians after February 1949)
  are not described here, as in `arab-forces-1948.md`.
- **Confidence** (1 to 10) is the agent's, that a person who opens the source finds the claim as written. It is not a
  person's check.

## Sources read

| Short name | What it is | URL opened | How it was read |
|---|---|---|---|
| Dupuy | Trevor N. Dupuy, *Elusive Victory: The Arab-Israeli Wars, 1947-1974* (Harper and Row, 1978), chapters 5 to 10, pp. 41 to 104 | https://web.stanford.edu/group/tomzgroup/pmwiki/uploads/1793-1978-Dupuy-a-CLH.pdf | Text extracted from the PDF (pypdf); pp. 73 to 104 read in full. The scan ends at p. 104 (31 October 1948), so Dupuy covers nothing after Operation Hiram here. PDF page 62 (printed p. 102) has no text layer |
| O'Ballance | Edgar O'Ballance, *The Arab-Israeli War 1948* (Faber and Faber, 1956), Digital Library of India scan | https://archive.org/details/in.ernet.dli.2015.51540 (text: https://archive.org/download/in.ernet.dli.2015.51540/2015.51540.The-Arab-Israeli-War-1948_djvu.txt) | The archive's OCR text; chapters VII to X, pp. 141 to 206, read in full or by search |
| Armistice EG | Egyptian-Israeli General Armistice Agreement, Rhodes, 24 February 1949 (UN doc. S/1264/Corr.1), the Avalon Project's transcription | https://avalon.law.yale.edu/20th_century/arm01.asp | Full text |
| Armistice LB | Lebanese-Israeli General Armistice Agreement, Ras en-Naqoura, 23 March 1949 | https://avalon.law.yale.edu/20th_century/arm02.asp | Full text |
| Armistice JO | Jordanian-Israeli General Armistice Agreement, Rhodes, 3 April 1949 | https://avalon.law.yale.edu/20th_century/arm03.asp | Full text |
| Armistice SY | Israeli-Syrian General Armistice Agreement, 20 July 1949 | https://avalon.law.yale.edu/20th_century/arm04.asp | Full text |
| BG diary 21 Oct | Ben-Gurion's diary, 21 October 1948, the Ben-Gurion Archive's English transcription (item 224891) | https://bengurionarchive.bgu.ac.il/en/node/7290 | The page's text. The Hebrew original was not seen |
| BG diary 31 Oct | Ben-Gurion's diary, 31 October 1948 (item 225117) | https://bengurionarchive.bgu.ac.il/en/node/12338 | The page's text. The Hebrew original was not seen |
| en.wiki Iraq Suwaydan | Iraq Suwaydan, the page's wikitext | https://en.wikipedia.org/wiki/Iraq_Suwaydan | `action=raw`; used only for a date conflict (C3) |

The armistice texts are the four agreements Israel signed; Avalon transcribes the UN documents. A person may prefer to
cite the UN's own copies (UN Treaty Series, or https://peacemaker.un.org); those were found by search, not opened.

## 1. Egyptian Expeditionary Force (`egyptian_expeditionary_force`)

Today's script ends at Isdud (the forward base) and Majdal (the rear base), 11 June to 8 July 1948.

| Id | Place | Window (proposal) | end_basis | Evidence | Non-wiki | Confidence |
|---|---|---|---|---|---|---|
| E1 | Isdud | 1948-07-08 to 1948-10-27 | source: evacuated 27 Oct (Dupuy p. 99; O'Ballance p. 181) | Q-E1, Q-E2, Q-E5, Q-E9, Q-E10 | 2 | 8 |
| E2 | Majdal | 1948-07-08 to 1948-11-05 | source: abandoned 5 Nov (Dupuy p. 99; O'Ballance p. 181) | Q-E1, Q-E2, Q-E5, Q-E9, Q-E10 | 2 | 8 |
| E3 | Faluja (from late October the "Faluja pocket") | 1948-07-08 to 1949-02-26 (or 1949-02-24, C5) | source: the armistice's withdrawal (Armistice EG, Art. III) | Q-E1, Q-E2, Q-E5, Q-E6, Q-E7, Q-E8, Q-E11, Q-E16 | 4 | 8 |
| E4 | Beersheba | 1948-07-08 to 1948-10-21 | source: surrendered 21 Oct (Dupuy p. 98; O'Ballance p. 180; BG diary 21 Oct) | Q-E1, Q-E2, Q-E5, Q-E7, Q-E8 | 3 | 8 |
| E5 | Hebron and Bethlehem (Egyptian and volunteer battalions) | 1948-07-08 to 1949-02-24 | front_armistice (egypt); the armistice leaves these positions to a later agreement (Art. VI.4, C6) | Q-E1, Q-E5, Q-E7, Q-E12, Q-E17 | 3 | 6 |
| E6 | El Auja and the road north to Asluj | 1948-10-21 to 1948-12-27 (`from` C7) | source: El Auja and Asluj taken 27 Dec (O'Ballance p. 200) | Q-E5, Q-E10, Q-E13, Q-E14 | 2 for the place, 1 for the end date | 6 |
| E7 | Gaza and Rafah (the coastal strip; division headquarters, C4) | 1948-07-08 to 1949-02-24 | front_armistice (egypt); the strip stays Egyptian under the armistice (Art. VI.1) | Q-E1, Q-E2, Q-E3, Q-E4, Q-E7, Q-E10, Q-E14, Q-E15, Q-E17 | 3 | 8 |

Quotes:

- **Q-E1** Dupuy p. 80, the forces when the first truce expired: "The Egyptian forces in this region had been augmented
  during June and early July, and totalled four brigades when the first truce expired. One of these, with headquarters
  at Gaza, occupied the coastal strip from the border to Majdal; another—the 2nd Brigade, [...] with headquarters at
  Majdal—was responsible for the security of the Ashdod area; a third—the 4th Brigade, [...] with headquarters at
  Faluja—occupied the axis from Majdal through Beit Gubrin to Hebron; the fourth brigade—consisting mostly of [...]
  Volunteers—held Beersheba and the area north to Hebron and Bethlehem."
- **Q-E2** O'Ballance pp. 173 to 174, before the October fighting: "These were: first the 'coastal strip' from Rafah, on
  the frontier, through Gaza, northwards to Isdud, along the road and the railways [...]. Second there was the 'inland
  strip', running also roughly from south to north, from El Auja, through Beersheba and Hebron, to Bethlehem, along the
  inland road. Joining these two strips together was the third, which was a 'cross-country strip', running from west
  to east along the road from Majdal to Beit Jibrin, through Faluja."
- **Q-E3** O'Ballance p. 174: "Gaza was the headquarters of this expeditionary force, and most of its bases and supply
  centres were around that city or to the south of it."
- **Q-E4** Dupuy p. 91: "It was organized into nine infantry battalions, which in turn were combined in three brigades,
  all under the control of a division headquarters at Rafah."
- **Q-E5** Dupuy pp. 91 to 92: "By early October the Egyptian expeditionary force was deployed as follows: one brigade
  group was in the Ashdod-Majdal area, with headquarters at Majdal; another brigade group was deployed on the
  Majdal-Beit Gubrin corridor, with headquarters at Faluja; the third brigade group, consisting of only two battalions,
  and with headquarters at Gaza, was deployed in the Rafah area. To the east a battalion of about 1,000 Egyptians and
  Volunteers held the area between Beit Gubrin and Bethlehem. A similar battalion was in the Hebron-Beersheba-el Auja
  corridor."
- **Q-E6** Dupuy p. 97, after the Huleiqat breakthrough (about 20 October): "[...] left an Egyptian force of about 4,000
  men, including about 1,000 in Iraq el Manshiyya, approximately a brigade, exposed and partially isolated in the
  vicinity of Faluja."
- **Q-E7** Dupuy p. 98: "[...] surrendered at 9:15 in the morning." (Beersheba, 21 October.) And: "Less than a brigade
  was with the division headquarters in the Rafah-Gaza area. A brigade was isolated in and around Faluja.
  Approximately two battalions, mostly [...] Volunteers, were isolated in the Hebron-Jerusalem area."
- **Q-E8** O'Ballance pp. 179 to 180: "[...] became known as the 'Faluja pocket'. It was held mainly by Sudanese troops, of which
  there were about 3,500 [...]." And: "Beersheba was garrisoned by about 500 men [...]" (OCR: "mart"), "[...] surrendered
  about dawn on the 21st."
- **Q-E9** Dupuy p. 99: "[...] on October 27 the Egyptians evacuated Ashdod, pulling their troops back as rapidly as
  possible along the threatened beach road. On November 5, Majdal also was abandoned, giving the Israelis full control
  of the coast as far south as Beit Hanun."
- **Q-E10** O'Ballance p. 181 (footer missing; counted from pp. 180 and 182): "Isdud was evacuated on October 27th and
  on November 5th Majdal was also abandoned without a shot being fired."
- **Q-E11** O'Ballance p. 183: "Although completely surrounded, the Faluja pocket continued to hold out defiantly, but
  its perimeter was gradually curtailed. After some fighting, on November 8th, its eastern bastion, Suweidan,
  surrendered [...]."
- **Q-E12** O'Ballance p. 180: "A considerable number of Egyptians from various units and in various stages of
  demoralization remained in the Hebron area for some months, cut off from the possibility of retreat to Egypt and
  they never engaged in further hostilities."
- **Q-E13** O'Ballance p. 183: "At the end of the fighting, the Egyptian position was like a U-shaped piece of
  territory, with the left, northern tip about six miles north of Gaza, running from there to Rafah, on to Asluj and
  then northwards again almost to Beersheba."
- **Q-E14** O'Ballance p. 200: "Realizing the acuteness of their position, the Egyptian defenders withdrew into the
  desert in the early hours of the 27th. [...] the Israelis on the 27th, the same day that El Auja, farther to the
  south, also fell." And: "[...] Gaza, Kfar Yunis and Rafah became cut off from one another." (OCR: footer "800".)
- **Q-E15** O'Ballance p. 202: "On 7th January 1949, just as the Israeli troops were completing their preparations
  for mounting the attack on Rafah, Egypt asked for an armistice which was granted immediately. The fighting ceased
  almost at once [...]." (OCR: "Rafeh".)
- **Q-E16** Armistice EG, Art. III.1 and III.2: "[...] the Egyptian Military Forces in the AL FALUJA area shall be
  withdrawn." "This withdrawal shall begin on the day after that which follows the signing of this Agreement, at 0500
  hours GMT, and shall be beyond the Egypt-Palestine frontier." (Signed 24 February 1949, so the withdrawal begins on
  26 February: the agent's arithmetic.) BG diary 31 Oct quotes an Iraqi cable of 30 October: "the Egyptian forces in
  Iraq-Manshiyya and al-Faluja and Iraq-Suweidan are surrounded and have no contact with the other forces."
- **Q-E17** Armistice EG, Art. VI.1: "In the GAZA-RAFAH area the Armistice Demarcation Line shall be as delineated
  [...]". Art. VI.4: "In the BETHLEHEM-HEBRON area, wherever positions are held by Egyptian forces, the provisions of
  this Agreement shall apply to the forces of both Parties in each such locality, except that the demarcation of the
  Armistice Line and reciprocal arrangements for withdrawal and reduction of forces shall be undertaken in such manner
  as may be decided by the Parties, at such time as an Armistice Agreement may be concluded covering military forces
  in that area other than those of the Parties to this Agreement [...]." Art. VIII.1: "The area comprising the village
  of El Auja and vicinity [...] shall be demilitarized [...]."

BG diary 21 Oct supports E4 and the October retreat: "[...] we've succeeded in conquering Beersheva [...]" and
"There's news from the Egyptian front that the forces are retreating southward. It seems they're about to leave Ashdod,
Majdal, Faluja, Iraq-Suweidan, and elsewhere." (The diary's apostrophes are typographic.)

## 2. Arab Legion (`arab_legion`)

Today's script ends at Latrun, 11 June to 8 July 1948.

| Id | Place | Window (proposal) | end_basis | Evidence | Non-wiki | Confidence |
|---|---|---|---|---|---|---|
| L1 | Latrun | 1948-07-08 to 1949-04-03 | front_armistice (jordan); no source moves the Legion from Latrun | Q-L1, Q-L2, Q-L3, Q-L4, Q-L5 | 2 | 8 |
| L2 | Jerusalem (the Old City and around it) | 1948-07-08 to 1949-04-03 | front_armistice (jordan) | Q-L6, Q-L7, Q-L10 | 2 | 6 |
| L3 | Hebron (the column sent south in late October) | 1948-10-21 (precision: month, "late October", C8) to 1949-04-03 | front_armistice (jordan); no source moves the column | Q-L8, Q-L9 | 2 | 6 |

Quotes:

- **Q-L1** Dupuy p. 75, when the first truce expired: "Two Arab Legion infantry battalions, with armor and artillery
  support, were concentrated in the area north and east of Latrun."
- **Q-L2** Dupuy p. 78, 15 to 18 July: "The Israelis were unable to make any progress against the two battalions of
  disciplined Arab Legionnaires holding Latrun [...]." And: "[...] the Legion concentrated on the defense of Latrun and
  there it was completely successful."
- **Q-L3** O'Ballance p. 145: "At Latrun, he still retained his men in the strong positions around the police post and
  the monastery." (He = the Legion's commander.)
- **Q-L4** Dupuy p. 87, the Ten Days' assessment: "They had failed, however, in repeated efforts to eject the Arab
  Legion from its coastal plain outpost at Latrun [...]."
- **Q-L5** Dupuy p. 88, the second truce: "The most serious action on this part of the front was the Arab Legion's
  destruction of the Latrun pumping station on the water pipeline to Jerusalem." O'Ballance p. 167: "On August 12th
  the pumping station at Latrun was blown up [...]." (OCR: "August 1 2th".)
- **Q-L6** Dupuy p. 79: "On July 16 his Arab Legion forces began to shell the northern and northeastern sections of the
  New City, and probing attacks penetrated several sections of the Jewish defenses. [...] a number of the houses near
  the Damascus Gate were seized by the Legion."
- **Q-L7** Dupuy p. 100: "[...] commanding the Arab Legion forces in and around old Jerusalem, [...] on November 30 the
  two commanders agreed to a "sincere" cease fire, which went into effect on December 1st." O'Ballance p. 193: "[...] on
  November 30th a cease-fire was agreed upon in the Jerusalem area [...]."
- **Q-L8** Dupuy p. 98: "Alarmed by this development, in late October [the Legion's commander] sent a combined arms
  force—about a battalion in strength—down to Hebron [...]." Dupuy p. 99: "Seven kilometers east of Beit Gubrin the
  Israelis encountered the Arab Legion column which [the Legion's commander] had sent southward to the Hebron area. A
  short battle took place, in which the Arab Legion held its ground."
- **Q-L9** O'Ballance p. 181: "[...] he formed a mixed column of armoured cars and lorried infantry, which passed
  round to the east of Jerusalem and moved down to the Hebron district to do what it could to save the situation."
  And p. 181: "[...] it bumped into the Arab Legion armoured column at a point about 7 kilometres to the east of Beit
  Jibrin."
- **Q-L10** O'Ballance p. 205 (the Jordan armistice): "[...] except in the Jerusalem district, where the existing status
  quo was accepted."

## 3. Syrian Army (`syrian_army`)

Today's script ends at the Mishmar HaYarden bridgehead, 11 June to 8 July 1948.

| Id | Place | Window (proposal) | end_basis | Evidence | Non-wiki | Confidence |
|---|---|---|---|---|---|---|
| S1 | The bridgehead at Mishmar HaYarden | 1948-07-08 to 1949-07-20 | front_armistice (syria); the armistice makes the area a demilitarized zone (C9) | Q-S1 to Q-S6 | 3 | 9 |

Quotes:

- **Q-S1** Dupuy p. 83, before Operation Brosh: "The Syrian bridgehead at Mishmar Hayarden was held by one infantry
  brigade supported by tanks and artillery. A second Syrian brigade was on the heights overlooking the east bank of the
  Jordan River."
- **Q-S2** O'Ballance p. 150: "The Syrian bridgehead at Mishmar Hayarden was heavily fortified and the troops there
  increased and reorganized into two brigades [...]. In all, there were about 2,500 men in and around the bridgehead
  position."
- **Q-S3** Dupuy p. 84: "By noon the Syrians had regained all of their earlier possessions west of the river [...]." And:
  "Operation Brosh had failed completely; a stalemate settled around the Syrian bridgehead and Mishmar Hayarden."
- **Q-S4** O'Ballance pp. 153 to 154: "By July 18th, the end of this phase of the war, the Syrians were still firmly in
  possession of their bridgehead at Mishmar Hayarden [...]."
- **Q-S5** Dupuy p. 104, the end of Operation Hiram (31 October): "[...] the Syrians pulled back from their outposts, but
  continued to hold their position in Mishmar Hayarden itself. This strongly-fortified position was closely contained
  by the Israelis but not attacked." And: "[...] the Syrians retained only their tiny bridgehead just west of the Jordan
  at Mishmar Hayarden."
- **Q-S6** O'Ballance p. 205: "Eventually, on July 20th, these were concluded and all Syrian troops were withdrawn from
  Israeli territory." Armistice SY, Art. V.3: "The Armistice Demarcation Line shall follow a line midway between the
  existing truce lines [...]." Art. V.5(a): "[...] the area between the Armistice Demarcation Line and the boundary
  [...] shall be established as a Demilitarized Zone from which the armed forces of both Parties shall be totally
  excluded [...]."

## 4. Iraqi expeditionary force (`iraqi_expeditionary_force`)

Today's script ends at Jenin, 11 June to 8 July 1948.

| Id | Place | Window (proposal) | end_basis | Evidence | Non-wiki | Confidence |
|---|---|---|---|---|---|---|
| I1 | The triangle Jenin-Tulkarm-Nablus (Jenin as today's hex) | 1948-07-08 to 1949-04-03 | front_armistice (jordan); Jordan signed for the Iraqi sector and its forces replace Iraq's (C10) | Q-I1 to Q-I4 | 3 | 8 |

Quotes:

- **Q-I1** Dupuy p. 83, before the Ten Days: "The Iraqis, two brigades with some armored support, were concentrated in
  the Samarian triangle, Jenin-Tulkarm-Nablus."
- **Q-I2** O'Ballance p. 198, Operation Horev (from 22 December 1948): "As a token of goodwill, the Iraqis roused
  themselves and shelled a few Israeli villages which were near their positions."
- **Q-I3** Armistice JO, Art. VI.1: "It is agreed that the forces of the Hashemite Jordan Kingdom shall replace the
  forces of Iraq in the sector now held by the latter forces [...]." Art. VI.3: the new line in the Iraqi sector is
  taken up in stages, "within five weeks", "within seven weeks" and "(c) In all other areas of the Iraqi sector:
  within fifteen weeks of the date on which this Armistice Agreement is signed." Art. VI.7: "The Hashemite Jordan
  Kingdom accepts responsibility for all Iraqi forces in Palestine."
- **Q-I4** O'Ballance p. 205: "In this agreement, Jordan took over the Iraqi-held Samarian Triangle [...]. All these moves
  were completed by July 17th." And p. 206: "[...] as the Arab Legion moved into the positions occupied by them this in practice
  did not amount to much. Sadly disillusioned, they then withdrew through Jordan to make their way back to their
  country." (Fifteen weeks after 3 April 1949 is 17 July 1949, the agent's arithmetic: the two agree.)

## 5. Lebanese Army (`lebanese_army`)

The row has no 11 June anchor with two sources today, so it has no script under #1263.

| Id | Place | Window (proposal) | end_basis | Evidence | Non-wiki | Confidence |
|---|---|---|---|---|---|---|
| B1 | The line from Rosh HaNikra to al-Malikiyya (al-Malikiyya as the hex, C11) | 1948-07-08 to 1948-10-31 | source: the Lebanese footholds lost in Operation Hiram (Dupuy p. 104; O'Ballance pp. 190 to 191; BG diary 31 Oct) | Q-B1 to Q-B5 | 3 | 6 |

After 31 October 1948 no source puts the Lebanese Army in Palestine; the armistice line is the international boundary
(Q-B5). The row would have no anchor from 31 October to 23 March 1949, so the simple goals would take over (Q-ENEMY-1 in #1263
default), unless the owner rules the force leaves the map.

Quotes:

- **Q-B1** Dupuy p. 83: "The Lebanese Army held a line from Rosh Hanikra, on the coast, to the vicinity of Malkiya. The
  Israelis rightly suspected that the Lebanese would seek to avoid involvement in any operations to the east or south."
- **Q-B2** O'Ballance p. 150, the Syrian front in July: "[...] the road from Rosh Pina, northwards to the Jewish
  settlements, was under shell fire from the Syrians in the east and from the Lebanese from the west. The Israelis
  anticipated that the Syrians would launch an attack, with the object of linking up with both Kaukji, in central
  Galilee, and the Lebanese, at Malikiya."
- **Q-B3** Dupuy p. 104: "Carmel himself, with his 7th Brigade column, moved northeastward from Sasa, capturing first the
  village of Saliha and then Malkiya [...]." And: "[...] the Lebanese had lost their few small footholds in Galilee
  [...]." (Carmel, the Northern Front's commander, is named in the quote as the book names him.)
- **Q-B4** O'Ballance p. 190: "[...] first taking the village of Saliha and then occupying Malikiya, the frontier post."
  O'Ballance p. 191: "[...] the Israelis crossed the Lebanese border and occupied a stretch of country from the Litani
  River in the north to Malikiya in the south. The Lebanese offered little resistance." BG diary 31 Oct: "This morning
  at 5-6 we arrived and conquered Malkiyya." (C12.)
- **Q-B5** Armistice LB, Art. V.1: "The Armistice Demarcation Line shall follow the international boundary between the
  Lebanon and Palestine." (Avalon prints "and- Palestine".) Art. V.3: "Withdrawal of forces to the Armistice Demarcation Line and their reduction to
  defensive strength in accordance with the preceding paragraph shall be completed within ten days of the signing of
  this Agreement."

## 6. Arab Liberation Army (`arab_liberation_army`)

The row has no 11 June anchor with two sources today, so it has no script under #1263.

| Id | Place | Window (proposal) | end_basis | Evidence | Non-wiki | Confidence |
|---|---|---|---|---|---|---|
| A1 | Nazareth, the ALA's headquarters | 1948-07-08 to 1948-07-16 (C13 for an earlier `from`) | source: Nazareth surrendered on 16 July (Dupuy p. 86; O'Ballance p. 158) | Q-A1 to Q-A4 | 2 | 8 |
| A2 | Upper Galilee: Sasa and Jish, with Tarshiha (Sasa as the hex) | 1948-10-22 to 1948-10-31 (gap C14) | source: the ALA driven out of Galilee in Operation Hiram (Dupuy p. 104; O'Ballance p. 191; BG diary 31 Oct) | Q-A5 to Q-A9 | 3 | 7 |

The ALA is not a state and signed no armistice. After 31 October 1948 no source puts it in Palestine.

Quotes:

- **Q-A1** Dupuy p. 84: "At the time of the first truce the northern units of Kaukji's ALA held most of west central
  Galilee from Nazareth north to the Lebanese border." Dupuy p. 83: "At this time the ALA in northern Palestine
  consisted of some 2,200 troops, including 600 men in a regular Syrian battalion."
- **Q-A2** O'Ballance p. 155: "In central Galilee, Kaukji was still in command of the Arab Liberation Army, which had
  been reorganized and reduced in strength to about 2,000. It was now under Lebanese command and was supplied and
  controlled from that country. [...] He had set up his headquarters in Nazareth, and he dominated the mountainous area
  around."
- **Q-A3** Dupuy p. 86: "[...] shortly before 6 p.m. the leading elements of the Israeli units entered Nazareth. At 6:15
  the city surrendered, and Operation Dekel had come to a successful conclusion. Kaukji and the remnants of his troops
  were in flight to the northeast [...]." (Dupuy dates the day as 16 July: "by dawn on July 16 Laskov's advance units
  had reached Zipori".)
- **Q-A4** O'Ballance p. 158: "[...] by nightfall most of Nazareth was in Israeli possession. [...] just before dawn he and
  numbers of his men slipped away northwards over the hills." O'Ballance's date is on p. 157: "[...] on the 16th, after being ambushed badly [...]" (OCR: "qp"), the day his
  column took Saffuriya and then pushed on to Nazareth.
- **Q-A5** O'Ballance p. 185: "After losing Lower Galilee, including Nazareth, to the Israelis in the 'Ten Days'
  Offensive', Kaukji had withdrawn northwards into Upper Galilee, where he occupied a rectangle of the mountainous area
  there, roughly fifteen by twenty miles square. The north side of this rectangle rested on the Lebanese frontier."
  (OCR: "fiftecfl".)
- **Q-A6** Dupuy p. 100: "After his disastrous defeat at Nazareth in July, [the ALA's commander] had regrouped his
  shattered forces in Lebanon, and in the small strip of Palestine occupied by Lebanese forces. The strength of his
  revived army was about 3,100 men [...]." And: "On October 22, with Lebanese approval, [he] began a new offensive—really
  a raid—south into Galilee. His first objective was the settlement of Manara in the Huleh Valley [...]."
- **Q-A7** Dupuy p. 101: "The brigade under his personal direction had just isolated Manara. The 2nd Yarmuk Brigade was
  located a few miles west in the vicinity of Sasa and Gish. The 3rd Yarmuk Brigade was in the vicinity of Tarshia. The
  1st Yarmuk Brigade was isolated from the others, and was operating in the hills south of the Acre-Safad road."
  O'Ballance p. 184: "On October 22nd Kaukji moved out against the Israeli settlement of Manara [...]."
- **Q-A8** Dupuy p. 104: "Thus at about dawn on the 31st, Operation Hiram drew to a close. The ALA had been totally
  defeated and its remnants either captured or driven out of Galilee." O'Ballance p. 191: "Operation 'Hiram' drew to a
  close about six o'clock on the morning of October 31st [...]. All Galilee had been cleared, Kaukji had been driven out,
  the Arab Liberation Army had been broken up [...]."
- **Q-A9** BG diary 31 Oct: "We captured about 200 POWs, a few hundred were killed, a majority of Qawuqji's army fled in
  disarray." And, quoting the Iraqi cable of 30 October: "[...] the Jews attacked the liberation forces [the Arab
  Liberation Army] in the northern territory and forced them to retreat [...]."

## 7. Conflicts and points for the owner

- **C1, the second truce's hour.** Dupuy p. 86: 7:00 p.m. on 18 July; O'Ballance p. 165: 5.30 p.m. (local time). The game
  goes by day, so both give 18 July; R-ENEMY-006 (#1263) already uses 18 July.
- **C2, the Faluja pocket's strength.** About 4,000 (Dupuy p. 97) or about 3,500 Sudanese troops (O'Ballance p. 180). The
  rows give 15 May strengths only, so this matters only if a later strength is ever wanted.
- **C3, Iraq Suwaydan.** Surrendered on 8 November (O'Ballance p. 183) or fell on 9 November 1948 (en.wiki Iraq
  Suwaydan, citing Khalidi, not opened). No anchor depends on it: the fort is inside E3's pocket.
- **C4, the Egyptian headquarters.** Gaza (O'Ballance p. 174) or a division headquarters at Rafah from
  September-October (Dupuy p. 91; p. 98: "the division headquarters in the Rafah-Gaza area"). E7 joins them as one
  coastal anchor; the owner may want two.
- **C5, the end of the Faluja window.** The armistice was signed on 24 February 1949 (the row's `front_armistice`), but
  the withdrawal from the pocket begins on 26 February (Art. III.2). A `to` of 1949-02-26 would be `end_basis: source`;
  1949-02-24 would follow the file's front rule. With monthly turns both fall in the 15 February 1949 turn.
- **C6, the Egyptians at Hebron and Bethlehem after 24 February 1949.** The Egyptian armistice leaves them to a later
  agreement (Art. VI.4), and O'Ballance has them in the Hebron area "for some months". No source read gives the day
  they left. E5 proposes to end at the Egyptian armistice, by the file's front rule.
- **C7, El Auja and Asluj before 21 October.** Dupuy p. 92 puts a battalion in the Hebron-Beersheba-el Auja corridor in
  early October; Beersheba fell on 21 October. E6 starts on 21 October because Beersheba (E4) is the inland strip's
  anchor until then; a person may prefer an earlier `from`.
- **C8, the Legion's column to Hebron.** Both sources give "late October" with no day. 1948-10-21 is the agent's
  placeholder (the day Beersheba fell, the event O'Ballance ties it to); it must be read as precision: month.
- **C9, Syria after the armistice.** The armistice demilitarizes the bridgehead area; its withdrawal schedule (annex
  II) was not in the Avalon text read, so no withdrawal day is sourced. S1 ends at the signing.
- **C10, the Iraqi front's end.** Jordan signed for the Iraqi sector on 3 April 1949, but the Iraqi forces left in stages
  to about 17 July 1949 (Q-I3, Q-I4). I1 ends on 3 April by the file's front rule (`front: jordan`).
- **C11, who held al-Malikiyya in July 1948.** Carried from `arab-forces-1948.md` §8, item 13: the Lebanese Army, the
  ALA or the Syrians, depending on the source. Dupuy and O'Ballance both put the Lebanese there in July. B1 uses that
  reading and does not settle the conflict.
- **C12, the day al-Malikiyya fell.** The diary entry is dated 31 October and says "this morning"; Dupuy and O'Ballance
  put the capture before the end of Hiram at dawn on 31 October. A person should check the diary's Hebrew page for
  which morning it means. B1's and A2's `to` is 31 October either way.
- **C13, the ALA at Nazareth from 11 June.** Dupuy p. 84 puts the ALA at Nazareth "at the time of the first truce", and
  O'Ballance p. 155 has the headquarters there in July. If a person accepts that as an 11 June position, A1 could open
  on 1948-06-11, and the ALA would get a script under #1263's two-source rule. That changes the 15 June turn and every
  golden, so it is the owner's call, not this slice's.
- **C14, the ALA from 16 July to 22 October.** No source read gives a dated place for the ALA in the second truce:
  O'Ballance p. 185 and Dupuy p. 100 say only that it had withdrawn to Upper Galilee and Lebanon. A2 opens on 22 October,
  the first dated position.

## 8. Searched, not reached

- **Netanel Lorch, *The Edge of the Sword* (1961).** The Internet Archive item (`edgeofsword00lorc`) is access-restricted,
  and its full-text search returned "Item not available" on 2026-10-21. This is the first Israeli historiography the
  rows should cite (`units_1948.yaml` header; CONTENT_POLICY §1). A person with the book should check every anchor
  against it.
- **The IDF's official history** (the General Staff's history branch, *Toldot Milhemet HaKomemiyut*, 1959) and the
  IDF, Ministry of Defence, Knesset and State Archives sites: a site-restricted search found no page on these
  operations.
- **Ben-Gurion's diary beyond 21 and 31 October 1948.** Only those two entries were opened. A search of the archive
  for July 1948 (Nazareth, Latrun), December 1948 (Horev) and March to April 1949 (the Iraqi handover) is the cheapest
  next step for an Israeli primary source per anchor.
- **Morris, *1948*, Tal, *War in Palestine 1948*, Pollack:** not opened in this session.
- **Dupuy after 31 October 1948:** the Stanford scan ends at p. 104, so Horev, the Faluja withdrawal and the armistices
  rest on O'Ballance and the armistice texts alone.

## 9. Open questions for a person

1. Check every quote above against the page image (O'Ballance is OCR, Dupuy is an extracted text layer).
2. C5: does E3 end on 24 or 26 February 1949?
3. C13: should the ALA get a Nazareth anchor from 11 June 1948 (a script under #1263, every golden moves)?
4. B1 and A2 end on 31 October 1948 with nothing after. Should the Lebanese and ALA rows leave the map from then on, or
   fall back to the simple goals until the armistice, as #1270's text implies ("simple goals take over only where no
   sourced position exists")?
5. C4: one coastal Egyptian anchor (Gaza and Rafah) or two?
6. The new places (Faluja, Beersheba, Hebron and Bethlehem, El Auja, Gaza and Rafah, Jerusalem, Sasa) need coordinates
   by the file's rule and hexes in `unit_anchor_hexes.yaml` (rerun `tools/unit_anchor_hexes.py`). That belongs to the
   rows slice.
7. Several windows begin or end inside the second truce (18 July to 15 October 1948). Under R-ENEMY-006 (#1263) a scripted
   march still runs on a truce turn and only attacks are blocked. The owner may want marches inside the truce checked
   against the cease-fire too.

## 10. Considered, not proposed

- **The Legion at Lydda and Ramle (to 11 or 12 July 1948).** O'Ballance p. 145 gives the Legion "about a hundred all told"
  there, and says the commander withdrew them to Beit Nabala; Dupuy p. 76 has "small Legion contingents". The towns
  were held mainly by local forces (`local_arab_forces`, not in #1270's list), and the dates conflict (Dupuy p. 77:
  Lydda surrendered on the morning after the 10th, Ramle on 12 July; O'Ballance p. 147: Lydda on the 12th). It is also
  a sensitive area. No anchor is proposed.
- **Ramallah as the Legion's headquarters after 8 July.** Only O'Ballance p. 143 ("Ramallah, the main Arab supply base
  and headquarters") in the text read. The current row already moved the token from Ramallah to the front.
- **The Arab Legion in the Iraqi sector from April 1949** (Q-I3): after the Jordanian armistice, so outside the
  script's windows.
