# Nir Am's Negev water line, 1947 to 24 February 1949: sources, card text and gaps (research note, AI draft, 2026-10-20)

Status: **AI draft, not checked by a person.** Research only: nothing here changes `content/`, `sim/` or `unity/`, and
no `checked:` date is given, because only Claude read the sources. Refs #1006 (the research slice offered on
19 October 2026) and #791 (objective 3, which this part belongs to). The last section lists what a person has to check.

## Scope

The owner's ruling (#1006, 18 October 2026, "Part of objective 3"): the Negev water line that began at Nir Am must
stay in Israeli hands until the armistice with Egypt (24 February 1949). The card tells the pipeline's 1947–48 history
from sources, and its points are a labelled design value for the balance pass (#813). This note gives the source/claim
table, a draft card, the gaps and a review checklist. It chooses no points, adds no rule, state or `ever_lost` flag,
does not make Nir Am a settlement-control target and leaves `content/map/locations_1948.yaml` (`nir_am`, on main as
draft) alone. It is written so whoever takes #791 can fold it into objective 3's cards.

## 1. Source/claim table

Tiers: **[V]** a contemporary document read here; **[S]** a secondary source read here; **[L]** a lead seen only in
a search result, not read. Claude read every source on 2026-10-20. The "Checked by a person" column is left empty on
purpose: a person fills it in with the date they read the source.

| # | Claim | Sources | Tier | Agreement and caveats | Checked by a person |
|---|---|---|---|---|---|
| 1 | Wells at Nir Am (and Gvar'am) were the source of the first Negev water line | he.wiki "ניר עם" ("קידוחים באזור ניר עם שימשו מקור לקו המים שהונח בשנת 1947", citing *Mishmar*, 10 April 1947, "צנור המים בנגב"); he.wiki "מקורות" (Mekorot drilled about 15 wells around Gvar'am and Nir Am; no footnote); Encyclopaedia Judaica, 2nd ed., "Nir Am" (Orni and Gilboa, via encyclopedia.com: groundwater found soon after 1943 "supported a pipeline to Negev outposts in 1947") | [S] | Agree | |
| 2 | The line was laid in 1947 | he.wiki "ניר עם" and EJ (1947); Palmach Heritage, "הלחימה להגנת קו המים בנגב" (laying began at the end of 1946 and ended in summer 1947) | [S] | Agree on 1947 for the line. Israel Hayom (search result only) gives January 1947 for the start [L]. No inauguration day was found | |
| 3 | It ran in two arms from Nir Am, east and west, to the Negev settlements, among them the "11 points" of October 1946 | Palmach Heritage ("שתי זרועות צינור מניר עם"; the eastern arm to Mishmar HaNegev; the western arm through the Be'eri–Tzalim section); he.wiki "מקורות" (pipes to the 11 points); he.wiki "מוזיאון המים והביטחון בנגב" (water from Nir Am to the 11 points) | [S] | Agree. No source read lists every settlement on each arm. A US community paper (search result only) says eight settlements west and six east [L] | |
| 4 | Simcha Blass of Mekorot planned it; the 6-inch pipes came from London | he.wiki "מוזיאון המים והביטחון בנגב" (laid to Blass's plan; pipe sections bought in Britain); he.wiki "מקורות" (6-inch pipes from London, thick-walled; no footnote) | [S] | The London fire-service story appears only in search results [L]. Not needed on the card | |
| 5 | The Mandate government stopped the line's completion for a time in 1947 | JTA, "Palestine Government Planning Water Restrictions to Throttle Jewish Development", 25 May 1947: "the Jews were forbidden to complete their water pipeline project for bringing water to new settlements in the Negev" | [V] (a wire report) | One source. Context only | |
| 6 | From December 1947 the line was cut many times, and the settlements faced a water shortage | JTA, "Settlement Sends out S.o.s.; Arabs Kill Jewish Policemen Rushing to Aid", 17 December 1947: the pipeline "serving the recently-established 14 colonies in the desert" "was cut in a number of places last night", Haganah squads went out to repair it, "the colonists are threatened with a severe water famine"; Palmach Heritage (sabotage on the western arm along its whole length; repairs often slower than the damage; rationing at Nirim) | [V] + [S] | Agree. The JTA scan text has gaps (OCR "##"). Who cut it: Palmach Heritage says Bedouin; he.wiki "מקורות" says Bedouin and Arab residents who did not want the pipes through their land; JTA's December reports blame "Arab" bands. **The card does not name who cut it** (see the checklist) | |
| 7 | Guarding the line cost lives: two Jews were killed fighting off an attack on guards near Amara (Imara) | JTA, "Killed As Haganah Seeks to Uncover Headquarters of Band Terrorizing Old City", 29 December 1947 ("late yesterday"; British troops then arrested the Jewish guards); Palmach Heritage (26 December 1947: a squad guarding welders opposite the Imara police station was attacked; the British loaded 3 dead and 2 wounded) | [V] + [S] | **The two disagree** on the day (28 vs 26 December, if JTA's "yesterday" is the 28th) and the dead (2 vs 3). Both accounts end with British troops arresting or disarming the guards, so it is probably one incident with different dates and tolls (Palmach's 3 killed and 2 wounded are the men the British loaded onto their vehicle); it may still be two. No number goes on the card | |
| 8 | Palmach patrols, with armoured trucks from 28 December 1947 (the "Parpar"), guarded it; welders from the settlements repaired it under escort | Palmach Heritage | [S] | One source (the Palmach veterans' association) | |
| 9 | By February 1948 parts of the line no longer reached the far settlements | Ben-Gurion Archive, diary 8 February 1948 (archive's English text): "The water only reaches as far as Mishmar HaNegev because [the pipeline] near Beersheva was damaged" and "the army isn't allowing repairs"; "The people are confined to [protecting] the pipeline, the convoys"; Palmach Heritage (from Nir Am to Mishmar HaNegev the eastern arm ran among friendly Bedouin of the al-Huzayyil tribe with few faults; the section ending at Hatzerim, which came close to Beersheba, was in effect abandoned; Hatzerim, Beit Eshel and Nevatim relied on local water; Mivtahim and Nirim at the western end went short) | [V] + [S] | Agree. "The army" in the diary is presumably the British army; the entry does not say | |
| 10 | Sabotage fell in the following months, after Khirbet al-Far was taken (17 January 1948) and the Imara police post was taken (May 1948) | Palmach Heritage: in mid-January 1948 a patrol found 1,700 m of pipe taken from the line and buried at Khirbet al-Far; "at dawn on 17.1.48" the Western Negev company, reinforced from Be'eri, attacked and took the village, "in response to the dismantling of the pipes" blew up several houses and a water well, and the residents "fled and did not return" ("נמלטו ללא שוב"). On "2 May" the Mandate police closed the Imara station (the Arab policemen left with their weapons) and the Western Negev company took it; Kibbutz Urim later settled there. The page: "as a result of the flight of the residents of Khirbet al-Far and the capture of the Imara police station, sabotage on the line fell in the following months" | [S] | One source; the year of "2 May" is implied, not stated. **Sensitive:** houses and a well destroyed in reprisal and the residents' flight, told only by the attacking side's veterans' site | |
| 11 | Egyptian artillery shelled Negev settlements' underground pipelines on 20–21 May 1948 | JTA, "State of Emergency Decreed in Israel...", 21 May 1948: "In an apparent effort to cut off the vital water supply, the Egyptians poured 1,000 shells into subterranean pipelines throughout the night and this morning" | [V] (a wire report) | Does not name Nir Am's line or a place. Context only | |
| 12 | In the 1948 war Nir Am was the Negev Brigade's headquarters and the supply centre of the cut-off southern settlements | en.wiki "Nir Am" (citing the Palmach archive's home page); EJ "Nir Am" ("headquarters, hospital, and supply center" for settlements "cut off from the rest of Israel for several months"); `research_notes/Map/unit-card-hebrew-harel-givati-negev.md` (he.wiki "חטיבת הנגב": HQ at the Mekorot camp near Nir Am, from March 1948) | [S] | Agree | |
| 13 | Nir Am itself was not taken | he.wiki "מוזיאון המים והביטחון בנגב" (Nir Am withstood the Egyptian invasion and was a main defence point; no footnote) | [S] | One unfootnoted line. No source read says the Egyptian army reached the wells | |
| 14 | The Israel–Egypt armistice was signed at Rhodes on 24 February 1949, in force on signing; Israeli forces withdrew from Beit Hanun to north of the line | Avalon Project, "Egyptian-Israeli General Armistice Agreement" (Art. XII.1 "shall come into force immediately upon being signed"; "DONE at Rhodes ... on the twenty-fourth of February nineteen forty-nine"; Art. VI.2: Beit Hanun "from which Israeli forces shall be withdrawn to north of the Armistice Demarcation Line"), https://avalon.law.yale.edu/20th_century/arm01.asp | [V] | The date is already on main (`armistice_egypt`). Beit Hanun lies next to Nir Am | |

Sources:
- Palmach Heritage (Dor HaPalmach association), "הלחימה להגנת קו המים בנגב",
  https://www.palmach.org.il/history/database/?itemId=8471 . No author or footnotes on the page. `docs/locations.md`
  already cites it as [L9]. Its table of contents names "הלחימה בפולש המצרי" and "עמידת הנגב במצור", but no text
  behind those titles was shown.
- JTA archive (daily bulletins): https://www.jta.org/archive/palestine-government-planning-water-restrictions-to-throttle-jewish-development
  (25 May 1947); https://www.jta.org/archive/settlement-sends-out-s-o-s-arabs-kill-jewish-policemen-rushing-to-aid
  (17 December 1947); https://www.jta.org/archive/killed-as-haganah-seeks-to-uncover-headquarters-of-band-terrorizing-old-city
  (29 December 1947); https://www.jta.org/archive/state-of-emergency-decreed-in-israel-jewish-planes-attack-arab-troop-concentrations
  (21 May 1948). Dates are the pages' `datePublished`.
- Ben-Gurion Archive, diary 8 February 1948, https://bengurionarchive.bgu.ac.il/en/node/10411 (only the archive's
  English text was shown; the place of writing is not given on the page).
- he.wiki "ניר עם", https://he.wikipedia.org/wiki/ניר_עם ; he.wiki "מקורות", https://he.wikipedia.org/wiki/מקורות ;
  he.wiki "מוזיאון המים והביטחון בנגב", https://he.wikipedia.org/wiki/מוזיאון_המים_והביטחון_בנגב
- en.wiki "Nir Am", https://en.wikipedia.org/wiki/Nir_Am
- Encyclopaedia Judaica, 2nd ed., "Nir Am", https://encyclopedia.com/religion/encyclopedias-almanacs-transcripts-and-maps/nir-am
- Avalon Project, Egypt–Israel armistice, https://avalon.law.yale.edu/20th_century/arm01.asp

Not reached: the KKL-JNF water history page (`hanof.kkl.org.il`, 403); the Detroit Jewish News of 19 December 1947
("Arabs hit Negev pipeline") and the Detroit Jewish Chronicle of September 1947 ("Water Piped to the Negev") on the
Bentley library site (403) [L]; *Mishmar* of 10 April 1947 and *Davar* on JPress (not tried; NLI pages return 403 from
this machine); Mekorot's own history pages and the Water and Security Museum (no page found); the IDF and Defence
Establishment Archives (no page on the line found).

## 2. What the sources do not show (gaps)

1. **The line between July 1948 and February 1949.** No source read says what state the line was in during the
   siege (a search result, not read, dates the Egyptian cut between the Negev and the coastal plain to July 1948 [L]), after Operation Yoav (October 1948) or at the armistice. Palmach
   Heritage's sections on the Egyptian invasion and the siege had no text. So nothing read supports "the line stayed in
   Israeli hands to 24 February 1949" as a fact about the whole line.
2. **Continuous control is contradicted for the far ends.** Rows 6 and 9: by early 1948 the western arm was cut along
   its length, and the far end of the eastern arm (towards Hatzerim and Beersheba) was in effect abandoned; the
   Nir Am to Mishmar HaNegev section had few faults. The line as built was not held whole even
   before the invasion. What the sources do support is that the source end, Nir Am, was held and was the brigade's
   base (rows 12, 13), and that the line was defended and repaired (rows 7, 8).
3. **The route.** No source read gives the arms' routes or a list of the settlements on each arm, so the line cannot
   yet be drawn on the hex map. Only Nir Am's own point (`nir_am`, q -9, r 27, on main) is placed.
4. **No day for the card.** No inauguration or completion day was found (row 2). The owner's end date, 24 February
   1949, is the armistice, not an event on the line. Whether the card carries 24 February 1949 or a date in the line's
   1947–48 history is a question for #791 or the owner.
5. **Who cut the line**, and the Amara toll (rows 6, 7): the sources disagree, and the attribution is sensitive.
6. **No picture** was searched for in this slice. The offer does not ask for one; #791's photograph research (#835,
   #836) did not cover the line.

What this means for the mechanic (for #791 to decide, not done here): the sources can carry "Nir Am held until the
armistice"; they cannot yet carry "the whole line held". A condition on the Nir Am hex matches the evidence; a
condition on the line's route needs gap 3 closed first.

## 3. Draft card text (EN)

```yaml
title: The Negev water line
text: 'In 1947 pipes were laid from wells at Nir Am to the new Jewish settlements in the Negev, in two arms, east and west. From December 1947 the line was cut again and again. Palmach patrols guarded it, and welders from the settlements mended it under escort. In the 1948 war Nir Am became the headquarters of the Negev Brigade and the supply base of the cut-off southern settlements.'
```

Clause by clause: wells at Nir Am, 1947 (rows 1, 2); new settlements, two arms (row 3); cut again and again from
December 1947 (row 6); Palmach patrols, welders under escort (row 8); headquarters and supply base (row 12). Left out
on purpose: who cut the line (row 6), casualty numbers (row 7), Blass and the London pipes (row 4, weakly sourced),
and any claim that the line was held to the armistice (section 2, gaps 1 and 2).

No Hebrew draft: the earlier research notes give none, and the event YAMLs carry none. The Hebrew text is for the
native reviewer (#564).

## 4. For #791

- This is objective 3's water part. Its card would sit beside the railway, Burma Road and yishuv cards in #791's set.
- Its sources and the person's check follow the same rule as #791's cards: a person reads each source before the card
  is `fact-checked` (#791: "a person reads each cited JPress page before a card is `fact-checked`").
- Open for #791 or the owner: the card's date (gap 4), and whether the condition is the Nir Am hex or the line
  (section 2). The points stay a labelled design value for #813.

## What a person must check

1. **Palmach Heritage page** (itemId 8471): the arms, the Be'eri–Tzalim section, 26 and 28 December 1947, "2 May",
   the rationing; and whether its "Egyptian invader" and "siege" sections have text that closes gap 1.
2. **JTA, 17 December 1947**: the "14 colonies" and the cut, against the scanned bulletin (the OCR has gaps).
3. **JTA, 29 December 1947 vs Palmach Heritage, 26 December**: one incident or two, the day and the dead.
4. **Ben-Gurion's diary, 8 February 1948**: the Hebrew original, and who "the army" is.
5. ***Mishmar*, 10 April 1947, "צנור המים בנגב"** (he.wiki's source for 1947) on JPress, and a day for the line's
   completion or opening if one exists.
6. **The line after July 1948**: any source on its state in the siege, after Yoav, and at 24 February 1949 (Negev
   Brigade histories, Mekorot, the Water and Security Museum, the IDF Archives).
7. **Nir Am not taken** (row 13): a footnoted source.
8. **Who cut the line and the wording**: a sensitivity reviewer decides whether the card may name Bedouin, Arab bands
   or neither, and checks rows 7 and 10 (the dead; the attack on Khirbet al-Far, the houses and well blown up and
   the residents' flight, dated 17 January 1948).
9. **The card text** in section 3, clause by clause against the table, then the Hebrew by a native reviewer.
10. Fill in the "Checked by a person" column with the date each source was read.

Only after these may the sources go into `content/` with `checked:` dates.
