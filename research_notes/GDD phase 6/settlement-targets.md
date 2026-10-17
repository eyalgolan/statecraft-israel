# Phase 6 settlement-control candidates: research note

Research date: 18 October 2026. Research only. This note proposes no target list and selects no place; the owner
picks the list (Q-TGT-1, #783).

## Scope and cautions

This is the research note for PR 1 of the Phase 6 plan (`docs/superpowers/plans/2026-10-15-gdd-phase-6.md`, "PR 1:
the curated target list", #793). It covers the seven `arab_town` rows of `content/map/settlements_1948.yaml` that
Q-TGT-1 option (a) names: Jaffa, Acre, Nazareth, Lydda, Ramle, Gaza and Beersheba. Covering them does not answer
Q-TGT-1: options (b) and (c) remain open, and Q-TGT-1 and Q-TGT-3 are still unanswered on #783 as of this date. A
separate draft on #783 (17 October 2026) covers the 23 `arab_town` rows of `localities_1948.yaml` for operation and
date only, without residents. This note adds what the plan's PR 1 asks for on residents for the seven sim rows.

The plan asks for what mainstream Israeli historiography and Israeli official sources record, with conflicts kept
(`docs/CONTENT_POLICY.md` section 1 picks one value at content time; a research note keeps every version). The main
source here is Benny Morris, an Israeli historian; the Israeli official sources are the Knesset summary, the IDF
accounts and the IDF and Defence Establishment Archives. A Palestinian account (Munayyer, with Walid Khalidi's
introduction) is recorded where it was read, and labelled. A person must check each claim before any fact goes into
content.

Conventions:

- **[V]**: the cited passage or record was read on 18 October 2026.
- **[C]**: cited sources give different dates or descriptions; every version is kept.
- **[S]**: carried from an existing repo research note, which records its own check.
- **[W]**: the claim rests on Wikipedia alone.
- **[GAP]**: the evidence read does not establish the point.
- "Held" means military or governmental control.
- The 1945 figures are Village Statistics estimates for 1 April 1945, not 1948 resident counts. They come from the
  repo's `content/map/settlement_population_1945.yaml`, where every row is `owner-verify` and is read from Wikipedia's
  transcription, not the scan. They were re-read against that page on 18 October 2026 and match. Each is marked [W].

## Common sources

1. Benny Morris, *The Birth of the Palestinian Refugee Problem Revisited*, Cambridge University Press, 2004:
   pp. 211-221 (Jaffa), 228-232 (Acre), 418-421 (Nazareth), 423-435 (Lydda and Ramle), 466-467 (Beersheba, and the
   observer's description of Gaza) and 471-472 (Gaza in October 1948).
   [Cambridge record](https://www.cambridge.org/core/books/birth-of-the-palestinian-refugee-problem-revisited/8AE72A6813CEA7DDDE8F9386313F0D97).
   Page numbers were checked on 18 October 2026 against a full-text copy with the 2004 pagination. [V]
2. Knesset, "מלחמת העצמאות" (The War of Independence), narrative and table of operations,
   [PDF](https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf), accessed 18 October 2026. [V]
3. IDF account, "Operation Danny (July 9-18, 1948)", republished by the Jewish Virtual Library,
   [page](https://jewishvirtuallibrary.org/jsource/History/Danny.html), accessed 18 October 2026. [V]
4. IDF account, "Operation Yoav (October 15-22, 1948)", republished by the Jewish Virtual Library,
   [page](https://jewishvirtuallibrary.org/jsource/History/Yoav.html), accessed 18 October 2026. [V]
5. IDF and Defence Establishment Archives, "מבצע חורב - מבצע עין" (Operation Horev - Operation Ayin),
   [page](https://archives.mod.gov.il/FocusOn/Pages/050115.aspx), accessed 18 October 2026. [V]
6. United Nations, Egyptian-Israeli General Armistice Agreement, S/1264/Rev.1, 24 February 1949,
   [transcription](https://avalon.law.yale.edu/20th_century/arm01.asp), accessed 18 October 2026. [V]
7. Spiro Munayyer, with an introduction and notes by Walid Khalidi, "The Fall of Lydda", *Journal of Palestine
   Studies* 27(4), 1998, pp. 80-98, [PDF](https://www.palquest.org/sites/default/files/The_Fall_of_Lydda.pdf),
   accessed 18 October 2026. A Palestinian participant's account. [V]
8. Walid Khalidi, ed., *All That Remains*, Institute for Palestine Studies, 1992. Only the bibliographic record was
   reachable ([Google Books](https://books.google.com/books/about/All_that_Remains.html?id=P4VtAAAAMAAJ), accessed
   18 October 2026). It supports no claim below. [GAP]
9. Government of Palestine, *Village Statistics, April 1945*, as transcribed on
   [Wikipedia](https://en.wikipedia.org/wiki/Village_Statistics,_1945), re-read 18 October 2026. [W]
10. Repo notes carried as [S]: `research_notes/GDD phase 5/arab-forces-1948.md` (Dupuy, O'Ballance, Tal),
    `research_notes/M3 content 1950-51/facts-1948.md` and `research_notes/Map borders/control-15-may-1948.md`
    (en.wiki revisions).

## Jaffa (`jaffa`, hex `(-3, 19)`, the same hex as `tel_aviv`)

**Operation and control.** The partition plan made Jaffa an Arab enclave; some two thirds of its prewar population of
70,000-80,000 were still there in late April (Morris, p. 212). The Irgun attacked the Manshiya quarter on 25 April
and reached the sea on 27 April (pp. 212-213). British troops moved into the city on 28 April; after an agreement on
30 April the Irgun withdrew and British troops were left in control (p. 216). The Haganah's Operation Hametz, 28-30
April, took villages east of Jaffa and cut the town off from its hinterland toward Lydda and Ramle (pp. 217-218; the
Knesset table dates Hametz 27-30 April, "the encirclement of Jaffa"). Members of a Jaffa Arab Emergency Committee met
Kiryati officers on 12 and 13 May and signed an agreement; the Haganah commander's instructions to the population are
dated 13 May. On 14 May Haganah units, with token Irgun forces, occupied Jaffa without resistance (p. 219). [V]

[C] Date of the fall: Morris gives the agreement on 12-13 May and the occupation on 14 May; the repo's control note
carries 13 May from en.wiki's Operation Hametz page [S][W]; the Knesset summary says the city surrendered in May
without a day.

**Residents.** Morris attributes the flight before the occupation to the Irgun's three-day mortar barrage, the
departure of local leaders, the collapse of services and order, the isolation of the town after Hametz, and the
conduct of irregular forces in the town (pp. 213-218). By 8 May about 5,000 inhabitants remained (p. 218); after the
occupation a census registered about 4,100 (p. 219). The remaining inhabitants were screened and concentrated in one
or two areas behind barbed wire. Morris records looting by troops and civilians, brief forced unpaid labour, a rape,
and the bodies of 15 men found shot on 25 May, apparently after the occupation (p. 220). He dates the end of the
harassment and vandalism to August (p. 221). [V]

**Population baseline.** 94,310 in "Jaffa (Urban)" on 1 April 1945, of whom the table lists 28,000 as Jews; not a
1948 figure. [W]

**Existing cards.** No event card's text names Jaffa. `bernadotte_assassination.yaml` mentions Lehi detainees moved
to Jaffa in its comments, not the city's capture.

**Open points.** [GAP] The 13 May agreement itself was not read; its terms here are Morris's account. [GAP] Jaffa
falls before the game's 15 May 1948 start, and #569 makes it part of Tel Aviv-Yafo (plan, "Where things stand"), so
its start-of-game control is a question for the owner, not for this note.

## Acre (`acre`, hex `(7, 6)`)

**Operation and control.** The partition plan assigned Acre, a town of 12,000-15,000, to the Arab state (Morris,
p. 228). The British withdrew from the camps around the town in early May (pp. 229-230). Operation Ben-Ami began on
13 May; on 13-14 May Haganah forces took the villages and positions north and east of the town and surrounded it. The
militia commander Yunis Naf'a left by boat for Lebanon on 14 May; Mahmoud al-Saffuri, who then organised the
defence of the Old City, left with his men on 16 May. Carmeli forces attacked on the night of 16 May and on 17 May.
Around midnight, notables asked to surrender; Haganah troops entered early on 18 May. Morris writes that Acre fell on
17-18 May (pp. 230-231). [V]

[C] The Knesset table dates Ben-Ami 14-19 May; Morris dates its start to 13 May.

**Residents.** Before the assault, many inhabitants and Haifa refugees left for Lebanon; the British put the
population at about 8,000 on 5-7 May (p. 230). After the surrender about 5,000 remained, many of them not local:
refugees from Haifa and labourers from Syria, Egypt and Sudan (p. 231). A military administration reopened the water
mains and shops, imposed a nightly curfew and searched the houses for weapons and former irregulars. Morris states
that no expulsion orders were issued and no pressure was put on the townspeople to leave. From 25 May families living
outside the walls were moved into the Old City (p. 231). Before the First Truce ended, IDF Northern Front sought to
move the inhabitants to Jaffa or across the border; the minority-affairs minister Shitrit and the finance minister
Kaplan opposed it, and Ben-Gurion shelved the idea (pp. 231-232). [V]

**Population baseline.** 12,360 in "Acre (Urban)" on 1 April 1945; not a 1948 figure. [W]

**Existing cards.** No event in `content/events/` names Acre.

**Open point.** [GAP] The surrender spans the night of 17-18 May. If a data row needs one day, the owner chooses.

## Nazareth (`nazareth`, hex `(7, 10)`)

**Operation and control.** The Arab Liberation Army held Nazareth before Operation Dekel. On 16 July units of the
Golani and 7th brigades occupied the town; the ALA units left as the troops entered. That evening the remaining
notables and Haim Laskov, commander of Operation Dekel, signed an instrument of surrender (Morris, p. 419). The Knesset
summary says Operation Dekel took Lower Galilee, including Nazareth, and its table dates Dekel 9-18 July. [V]

**Residents.** Before the attack some inhabitants "with means" left (p. 418). Most stayed. On 17 July an expulsion
order was issued; Ben Dunkelman, whom Laskov had appointed military governor, refused to carry it out, and
Ben-Gurion, as defence minister, vetoed it (p. 419). Morris puts the town at about 15,000 inhabitants and 20,000
refugees in the days after the capture. In the following weeks refugees came back through the IDF lines in large
numbers, while Muslim families continued to leave (pp. 420-421). The mayor and much of the municipal staff stayed,
and a Minority Affairs Ministry official became military governor on 18 July (p. 420). [V]

**Population baseline.** 14,200 in "Nazareth (Urban)" on 1 April 1945; not a 1948 figure. [W]

**Existing cards.** No event in `content/events/` names Nazareth.

**Open point.** [GAP] The post-capture figures mix residents and refugees; they are not a resident count.

## Lydda (`lod`, hex `(-3, 21)`, the same hex as `ramla`)

**Operation and control.** Lydda was defended by local volunteers and, with Ramle, one Arab Legion company of about
120-150 men (Morris, p. 424). Operation Dani began on the night of 9 July (p. 425). The IDF account dates the
operation 9-18 July; the Knesset table 9-17 July, and its summary says Dani took Ramle and Lydda from the Arab Legion.
On the evening of 11 July, after the 89th Battalion's raid through the town, the Yiftah Brigade's 3rd Battalion
fought its way in and held the centre; the police fort held out until the morning of 13 July (pp. 426-427, 431-432).
No formal surrender was signed in Lydda (p. 427). [V]

[C] Capture date: Morris has the town entered on 11 July and the police fort taken on 13 July; Khalidi's introduction
gives 10-14 July for the conquest (Munayyer, p. 80) [V]; the repo's M3 note carries 11 July from en.wiki [S][W].

**Residents.** Air and artillery attacks on 10-11 July caused flight before the capture (pp. 425-426). On 12 July,
after Legion armoured cars entered the town, sniping broke out; Morris reports that the troops' fire killed some 250
Arabs, by a Yiftah report, including dozens of unarmed detainees in the Dahaimash mosque compound (pp. 427-428). That
afternoon Operation Dani headquarters ordered the inhabitants of Lydda expelled toward Beit Nabala (p. 429). Most
left on 13 July, on foot, toward Beit Nabala and Barfiliya (pp. 431-432). Morris writes that quite a few died on the
way from exhaustion, dehydration and disease. He cites a hearsay figure of 335 from Nimr al-Khatib and does not adopt
it (p. 433). About 100 people remained in Lydda; with Ramle, several hundred, mostly old and sick, and a few
Christians. Infiltration back raised the two towns' Arab population to about 2,000 by mid-October (p. 434). [V]

Munayyer's account and Khalidi's introduction also describe the fighting, the small Legion company, the killings of
12 July and the expulsion of most of the population; Khalidi's introduction puts the Legion company at 125
men. [V]

**Population baseline.** 16,780 in "Lydda (Urban)" on 1 April 1945; not a 1948 figure. Morris puts the two towns
together at 50,000-70,000 in July 1948, about 20,000 of them refugees (p. 425). [W] for the 1945 figure.

**Existing cards.** `ten_days.yaml` names "Lydda airport" and Operation Dani in its text. Its comment records the
owner ruling that the text describes the military operation only, and `docs/m3-rulings.md` says Lydda and Ramle
"appear as military events only".

**Open points.** [GAP] One capture day needs a choice between the versions above. [GAP] No supported count of deaths
on the march was found.

## Ramle (`ramla`, hex `(-3, 21)`, the same hex as `lod`)

**Operation and control.** Ramle was the other primary objective of the first phase of Operation Dani (IDF account).
The Arab Legion company withdrew from Ramle on the night of 11-12 July. A delegation of notables reached Yiftah
Brigade headquarters that night and signed an instrument of surrender the next morning; Kiryati's 42nd Battalion
entered Ramle later that morning, 12 July, and imposed a curfew (Morris, pp. 426-427). The IDF account says Kiryati
units took Ramle on 12 July after a brief engagement. Khalidi's introduction also has Ramle falling on 12 July
(Munayyer, p. 80). [V] The repo's M3 note carries the same date [S].

**Residents.** Flight from Ramle began with the air attacks of 10 July (p. 425). Morris writes that an order like the one for Lydda
was apparently sent to Kiryati on 12 July; a Kiryati cable of the next day ordered military-age men sent to a
prisoner-of-war camp and the old, women and children taken by vehicle to al-Qubab and across the lines (p. 429). From the afternoon of 12 July thousands left on foot and in trucks
and buses; the bulk left on 13 July, many taken to al-Qubab and walking on to Legion-held Latrun and Salbit
(pp. 430, 432). Morris describes the departures from both towns as an expulsion (pp. 431-432). For those who
remained, see Lydda above. [V]

**Population baseline.** 15,160 in "Ramle (Urban)" on 1 April 1945; not a 1948 figure. [W]

**Existing cards.** `ten_days.yaml` names Ramle in its comments and sources only; its text names Lydda airport. The M3
ruling above covers Ramle too.

**Open point.** [GAP] The sources read give no separate count for Ramle of those who remained.

## Gaza (`gaza`, hex `(-11, 27)`)

**Operation and control.** The Egyptian coastal force entered Gaza on 15 May 1948 (Dupuy) or was held up at its Gaza
headquarters until 19 May (Tal) [S][C]. Israeli forces did not take Gaza in 1948-1949: in October it was bombed from the air
and shelled from the sea, and its townspeople went to the dunes and came back a few days later, but "Gaza was not
overrun" (Morris, pp. 471-472).
Operation Horev ran from 22 December 1948 to 7 January 1949; the IDF entered Sinai and was ordered back across the
border when armistice talks opened (IDF Archives). The Knesset summary says the Egyptian forces in the Gaza Strip were
then in danger of being cut off, and Egypt agreed on 5 January 1949 to negotiate. The armistice of 24 February 1949
set a demarcation line in the Gaza-Rafah area (art. VI), not to be read as a political or territorial boundary
(art. V). [V]

**Residents.** Gaza's own population was not displaced by an Israeli capture; it received refugees. Morris quotes an
American observer who, about two months after the October fighting, put Gaza at about 25,000 original inhabitants and about 60,000 refugees
(p. 467). These are an observer's estimates, not a census. [V]

**Population baseline.** 34,250 in "Gaza (Urban)" on 1 April 1945; not a 1948 figure. [W]

**Existing cards.** No card presents Gaza as captured. `first_new_yishuv.yaml` names the Gaza Strip as a place
Mefalsim faced.

**Open point.** [GAP] Gaza did not change hands in 1947-1949, so there is no Israeli capture date or captured-resident
outcome for it.

## Beersheba (`beersheba`, hex `(-9, 31)`)

**Operation and control.** Beersheba was in Arab hands before the Egyptian inland column arrived in May. [C] The
dates differ: Egyptian advance guard 17 May and main body 20 May (Dupuy p. 58), entered 20 May (O'Ballance p. 93),
17 May (Tal) or 19 May (Morris) [S]. Israeli forces took the town on 21 October 1948 in Operation Yoav: the IDF
account says the garrison surrendered by 09:00 that morning; the Knesset summary gives 21 October, and its table
names the capture "Operation Moshe" within Yoav (15-22 October). Morris names the 8th Brigade's 89th Battalion and
the Negev Brigade's 7th and 9th battalions (p. 466). [V]

**Residents.** Wealthier families had left from April-May. Mass flight, mainly toward Hebron, began on 19 October
after air raids on the night of 18-19 October, and continued during and after the capture (Morris, p. 467). About
200 adult men and 150 women and children remained. Around 25 October the women and children, with several dozen old
and disabled men, were trucked to the Gaza lines and pushed across. Of the roughly 120 men kept for labour, some were
later sent to prisoner-of-war camps and others to Egyptian-held Majdal or Gaza. When Ben-Gurion visited on 30
October, the Arab inhabitants were gone (p. 467). [V]

**Population baseline.** 5,570 in the "Beersheba" row on 1 April 1945. The repo file calls the source row garbled
and 5,570 its most likely reading; it is unchecked against the scan. [W] [GAP]

**Existing cards.** `operation_yoav.yaml` records Beersheba's capture in its sourced comments; its text does not name
the town. `first_new_yishuv.yaml` mentions the Beersheba bedouin in a comment.

**Open point.** [GAP] The May change of control is 17, 19 or 20 May, depending on whether the advance guard or the
main body counts.

## Comparison table

| settlement | period | sources | confidence | open questions |
|---|---|---|---|---|
| Jaffa | Irgun attack from 25 April; British control 30 April to 13 May; agreement 12-13 May; Haganah occupation 14 May 1948 | Morris pp. 211-221; Knesset | High for 14 May and the residents' outcome | 13 or 14 May [C]; agreement text not read; before the 15 May start and part of Tel Aviv-Yafo (#569) [GAP] |
| Acre | Operation Ben-Ami from 13 May; surrender night of 17-18 May 1948 | Morris pp. 228-232; Knesset | High for the two-day range and the residents' outcome | One date if content needs it [GAP]; Ben-Ami start 13 or 14 May [C] |
| Nazareth | Operation Dekel; occupied and surrendered 16 July 1948 | Morris pp. 418-421; Knesset | High | Residents and refugees not separable [GAP] |
| Lydda | Operation Dani, 9-18 July (IDF) or 9-17 July (Knesset); entered 11 July, police fort 13 July | Morris pp. 423-435; Knesset; IDF Danny; Munayyer/Khalidi pp. 80-98 | High for the operation and the residents' outcome; medium for one capture day | Capture day [C]; deaths on the march [GAP] |
| Ramle | Operation Dani; surrendered and entered 12 July 1948 | Morris pp. 425-432; IDF Danny; Knesset; Munayyer/Khalidi p. 80 | High | Count of those who remained [GAP] |
| Gaza | Egyptian-held from 15 May (or 19 May) 1948 through the 1949 armistice; not taken by Israel | Phase 5 note (Dupuy, Tal); Morris pp. 467, 471-472; IDF Archives Horev; Knesset; 1949 armistice | High | No Israeli capture or captured-resident outcome [GAP] |
| Beersheba | Egyptian control from 17, 19 or 20 May; Israeli capture 21 October 1948 | Phase 5 note (Dupuy, O'Ballance, Tal); Knesset; IDF Yoav; Morris pp. 466-467 | High for 21 October and the residents' outcome; low for one May date | May date [C]; 1945 baseline scan [GAP] |

## Sources not reached or not usable

- Khalidi, *All That Remains*: only the bibliographic record was reachable. It supports no claim here. [GAP]
- The Government of Palestine's *Village Statistics, April 1945* scan was not checked; all seven baselines stay the
  repo's `owner-verify` transcription. [GAP]
- The 13 May 1948 Jaffa agreement and instructions were not read in the original. [GAP]
