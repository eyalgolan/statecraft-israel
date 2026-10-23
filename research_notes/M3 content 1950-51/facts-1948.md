# M3 content research: decision events, 15 May 1948 to 24 Feb 1949

Researched 2026-10-10. All URLs were checked on that date.

**Purpose.** The game has no events between `start: 1948-05-15` and `armistice_egypt` (`from: 1949-02-24`), which leaves about 40 weekly turns (rung 5) with no decision. These notes collect sourced candidates for that period.

**Conventions** (the same as `facts.md`)
- **[V]** means verified on the cited page. Where two independent pages agree, both are given.
- **[S]** means single source, or a search-result summary that was not opened directly. Treat it as "probably".
- **[C]** means sources conflict. Every version is given, each with its source.
- **[GAP]** means not found.
- No quotes are invented. The few short quotes below are copied from the cited page.

**Schema reminders** (read from `content/` and `sim/Statecraft.Sim/EventRules.cs`)
- **Effect targets:** only the indicators `morale`, `food_supply`, `housing`, `security` and `intl_standing`, plus `treasury` and `fx_reserves`.
- **Policies:** only `rationing`, `housing_construction`, `border_settlement` and `maabarot`. None fits these events, so the options below use effects and `set_rung` only.
- **Rungs:** `set_rung` takes 1–5. 5 is full war (weekly turns) and 4 is prolonged conflict (monthly turns). A truce mapped to rung 4 would also cut the number of empty weekly turns.
- **Sensitive tags:** `holocaust`, `terror`, `massacre` and `war-casualties`. A sensitive event cannot have a positive `treasury` or `fx_reserves` delta.
- **Hard anchors** must have a historical option, and an event may have at most one historical option.
- **Offices in 1948:** Ben-Gurion holds both `prime_minister` and `defense_minister` (people.yaml). Sharett is `foreign_minister`, Kaplan `finance_minister`, Gruenbaum `interior_minister`, Shapira `immigration_minister` + `health_minister`, Rosen `justice_minister`, Bechor-Shalom Sheetrit `police_minister` + `minority_affairs_minister`. Dori is `chief_of_staff`, but Yadin acted for him in 1948 (see 3b).

**Source caveat (important).** The owner's preferred historians (Shapira, Morris's military narrative, Gilbert, Gelber, Bar-Zohar, official IDF history) were not read directly: the books are not online. The facts below come from:
- Israeli state sources: the MoD/IDF Archives, the Ben-Gurion Archive diaries at BGU, and the CBS;
- the Jewish Virtual Library (JVL), whose war pages give the IDF as their source;
- US FRUS documents and UN documents;
- en and he Wikipedia.

Where a fact traces to one of the named historians (Bar-Zohar via Lavid, for example), it is marked. A historian review is still needed before `status: historian-reviewed`.

---

## 1. First truce: accept or refuse (June 1948)

- **Security Council Resolution 50** was adopted on **29 May 1948** (S/801). [V] https://en.wikipedia.org/wiki/United_Nations_Security_Council_Resolution_50
  - It called for a four-week cease-fire.
  - It barred bringing fighting men or war material into Palestine and the Arab states.
  - The mediator was to supervise, and Chapter VII was raised if the terms were violated.
  - No vote was taken on the resolution as a whole; it was adopted in parts.
- **Israel's acceptance was sent to the UN on 10 June 1948.** [V] https://www.jewishvirtuallibrary.org/israel-accepts-the-truce-letter-from-ambassador-eban-to-the-secretary-general-s-834
  - Aubrey Eban sent the letter to the Secretary-General (S/834), conveying Shertok's reply to Bernadotte.
  - Israel was prepared, "if the other side accepts likewise", to cease fire from 11 June. The letter says it "attaches no conditions to this decision", but then lists reservations: limits on immigration of men of military age, food supply to Jerusalem, and blockades.
- **The truce took effect on 11 June 1948 and lasted 28 days.** [V] https://www.jewishvirtuallibrary.org/the-first-truce-1948 (source given: IDF); https://en.wikipedia.org/wiki/1948_Arab–Israeli_War
- **End date** [C]:
  - **8 July**: en.wiki 1948 war (contents, "First truce: 11 June – 8 July 1948"), and the Bernadotte article.
  - **9 July**: JVL/IDF ("ended Friday, July 9, 1948") and he.wiki (11.6–9.7).
  - Both sources agree that Egypt renewed fighting on 8 July, one day before the truce was due to expire. [V] en.wiki 1948 war.
- **What Israel did during the truce.** The IDF reorganised and trained, and materiel arrived from Europe, especially Czechoslovakia. [V] JVL/IDF page above.
- **Bernadotte's first proposals.**
  - He submitted them secretly on **28 June 1948**. [S] https://en.wikipedia.org/wiki/Bernadotte_plan
  - Terms: Galilee to Israel, the Negev to the Arabs, Jerusalem under UN authority. [V] JVL/IDF page.
  - **Israel rejected the proposals but agreed to extend the truce by a month. The Arab states rejected both.** [V] en.wiki 1948 war.
  - **Shertok's reply on the proposals is dated 5 July 1948.** [V, agent read 2026-10-23; a person's check pending] It is a letter from Shertok, as Minister for Foreign Affairs of the Provisional Government, to Bernadotte. Two sources give the date (A/648 and FRUS d436). S/870 carries the same text without a date:
    - UN doc A/648 (the Mediator's progress report, 16 Sept 1948), Part One, para 9: "The Provisional Government of Israel, in a letter dated 5 July 1948, objected to the deviations from the General Assembly resolution of 29 November 1947, and particularly to the suggestions concerning the regulation of immigration and the status of Jerusalem. They offered no counter-suggestions but urged a reconsideration of my "whole approach to the problem"." Annex I, item 4 prints the full text under the heading "LETTER DATED 5 JULY 1948 ADDRESSED TO THE UNITED NATIONS MEDIATOR BY THE MINISTER FOR FOREIGN AFFAIRS OF THE PROVISIONAL GOVERNMENT OF ISRAEL". Its item 5 is Bernadotte's reply of 6 July, which opens "I have studied carefully the observations on my suggestions set forth in your letter to me of 5 July 1948". UNISPAL's retyped PDF of A/648 (https://unispal.un.org/pdfs/AB14D4AAFC4E1BB985256204004F55FA.pdf) has para 9 on PDF p. 7 and items 4-5 on PDF pp. 22-23. These are PDF page numbers, not the printed GAOR page numbers.
    - UN doc S/870 (dated 8 July 1948, 4 pp.): Eban's letter of 7 July 1948 to the Secretary-General conveys "the text of the reply given by the Foreign Minister of Israel to the suggestions presented by Count Bernadotte" (p. 1). The text runs from p. 1 to p. 4 and ends: "may cause you to reconsider your whole approach to the problem." S/870 itself does not print the reply's date. https://digitallibrary.un.org/record/470511
    - FRUS 1948 v5p2 d436, Editorial Note: "Israeli Foreign Minister Shertok's formal reply to Count Bernadotte, dated July 5, was in the form of observations on the latter's suggestions. The observations were negative in character." https://history.state.gov/historicaldocuments/frus1948v05p2/d436
    - A/648 (Annex I, item 4) opens the reply by referring to suggestions sent "under cover of your letter of 27 June"; S/870 (p. 1) has "June 27". The 28 June line above ([S]) is left as it is. The two transcriptions differ in small wording, for example "noted with surprise" (S/870) against "has noted with surprise" (A/648). The substance is the same.
  - **Shertok's reply on extending the truce is a separate text.** [V, agent read 2026-10-23; a person's check pending] UN doc S/872 (dated 8 July 1948, 2 pp.) is Bernadotte's telegram giving the "text of Jewish reply to proposals of Mediator handed to Mediator by Shertok Thursday afternoon, 7 July" (p. 1). Its point 1: "The Provisional Government of Israel agrees to a prolongation of the Truce for a period of thirty days from 6 a.m. G.M.T. on Friday, July 9th 1948". https://digitallibrary.un.org/record/470613
    - [C] The telegram's own day and date disagree. 7 July 1948 was a Wednesday; the Thursday was 8 July (by the calendar, and consistent with the telegram's own "Friday, July 9th"). The source does not say which is right.
  - The press trace stays: a contemporary report (NY Herald Tribune, 7 July 1948) says Israel would accept a longer truce and turn down the plan. [S] https://cojs.org/jews_to_accept_longer_truce-_bar_peace_plan-_ny_herald_tribune-_july_7-_1948/
- **The internal Israeli debate on accepting** is a [GAP]. I found no online source naming ministers for and against. Checked again 2026-10-23 [agent read; a person's check pending]: S/870, S/872, A/648 (Part One and Annex I) and FRUS d435/d436 give the Provisional Government's position (in Shertok's letter) but name no minister for or against inside the cabinet. FRUS d435 (Jessup, 6 July 1948) reports Eban relaying "telegrams he had received from Shertok" on "late Friday afternoon" (presumably 2 July, the Friday before by the calendar). That is the government line, not a cabinet debate. https://history.state.gov/historicaldocuments/frus1948v05p2/d435
  - Yadin is quoted, via the Jewish Press, as calling the truce "dew falling from the heavens". [S, secondary, not used as a quote] https://jewishpress.com/shavuot-2/
- **Proposed event** `first_truce`:
  - **Window:** `from: 1948-06-01`, `to: 1948-06-11`. Anchor: hard. Office: `prime_minister`.
  - **accept** (historical): `set_rung: 4`; `security +0.05` (time to rearm and reorganise); `intl_standing +0.03`.
  - **refuse** (keep fighting): `intl_standing -0.08`; `morale -0.03`, because the army was short of arms and Resolution 50 threatened Chapter VII. The second effect is a design inference, not a sourced fact.
  - Do not invent a named "opponent" option; no Israeli voice for refusing was found. The Bernadotte plan of 28 June could be its own short event (reject the plan, historical; or open talks on it).
  - Tags: none.

## 2. The Altalena (20–22 June 1948)

- **20 June:** The ship reached Kfar Vitkin in the late afternoon. The same day the government met in Tel Aviv and resolved to empower the army to use force if necessary. [V] https://en.wikipedia.org/wiki/Altalena_Affair
  - 1948-in-Israel adds that the government demanded all weapons be handed over and that "The Irgun refuses to comply". [V] https://en.wikipedia.org/wiki/1948_in_Israel
- **The actual choice.**
  - Begin's first request, that 20% of the arms go to the Irgun's Jerusalem battalion, was agreed.
  - His second request, that the rest go to the IDF to equip Irgun battalions, was rejected as an "army within an army". [V] Altalena Affair.
- **21 June:**
  - Brigade commander Dan Even issued an ultimatum at Kfar Vitkin. [V]
  - Fighting broke out in the evening.
  - The ship sailed for Tel Aviv at 21:35 and ran aground off Tel Aviv around midnight.
- **22 June:** Ben-Gurion ordered the ship shelled, around 16:00. Yitzhak Rabin commanded the forces on shore. A ceasefire followed that evening. [V]
- **Casualties:** 16 Irgun and 3 IDF dead. [V] (both pages agree)
- **Aftermath:**
  - More than 200 Irgun members were arrested. Five commanders were held until 27 August 1948. [V]
  - The Irgun units in the IDF were disbanded and their men dispersed across other units. [V]
- **Cabinet objections or resignations:** [GAP]. The en.wiki page names none.
- **Proposed event** `altalena`:
  - **Window:** `from: 1948-06-20`, `to: 1948-06-22`. Anchor: hard. Office: `defense_minister` (or `prime_minister`; both are Ben-Gurion).
  - **enforce** (historical): order the IDF to take the arms. `security +0.05` (one army); `morale -0.05` (Jews killed by Jews).
  - **compromise**: Begin's second request, the arms to Irgun battalions inside the IDF. This was actually put on the table and rejected. `security -0.08` (a separate army inside the IDF); `morale +0.02`.
  - Tag: `war-casualties`. 19 Jewish dead in a clash between Jews, so it needs a sensitivity review. There are no money effects.

## 3. The IDF founding order and absorbing the Irgun, Lehi and Palmach

### 3a. Founding order
- **Date of the order** [C]:
  - Ben-Gurion signed the order on **26 May 1948**. [V] MoD/IDF Archives https://archives.mod.gov.il/sites/English/Exhibitions/David_Ben_Gurion_and_the_IDF/Pages/The-establishment-of-the-IDF.aspx and https://en.wikipedia.org/wiki/1948_in_Israel
  - JVL says it was "issued May 28, 1948". https://jewishvirtuallibrary.org/the-founding-of-the-israel-defense-forces
  - Both agree it was published in the Official Gazette No. 3 of **31 May 1948**. en.wiki IDF says the cabinet made it legal on 31 May. [V]
- **The prohibition.** The order banned "any armed force other than the Israel Defense Forces". [V, JVL wording]
- **First swearing-in:** **27 June 1948**, for the senior commanders at Ramat Gan. Ben-Gurion read the oath. Dori was absent through illness. [V] MoD Archives; corroborated by https://martinkramer.org/2020/02/06/swearing-allegiance-to-israel/
- **Irgun–government agreement** to dissolve the Irgun and fold it into the IDF: **1 June 1948**. [S] en.wiki 1948 in Israel. Jerusalem was excluded (Jerusalem was not yet under Israeli sovereignty). [V] JVL founding page.

### 3b. The "Generals' Revolt" (June–July 1948, inside the first truce)
- **Galili.** Ben-Gurion told Galili on 26 April that his post was abolished, and sent the formal letter on 3 May. On 6 May there was a 12-hour ultimatum from the Haganah leadership, which ended in a compromise. [V] https://en.wikipedia.org/wiki/The_Generals%27_Revolt
- **June-July 1948, step by step.** Dates below come from a protocol of the Committee of Five and from the Palmach Association's history site, not from the en.wiki page (which gives none). Source tags: [V, agent read 2026-10-23; a person's check pending] for the committee's own protocol and Sharett's published record of it; [S, agent read 2026-10-23; a person's check pending] for the rest.
  - **Ben-Gurion's proposed appointments: 29 June 1948.** The General Staff had proposed four fronts and their commanders on 24 June (en.wiki, citing Peri 1983, p. 298; the Palmach site, https://palmach.org.il/history/database/?itemId=5268, gives 24 June too). The Palmach site says Ben-Gurion's counter-plan "הוצגה ב- 29 ביוני לפני סגן הרמטכ"ל וראש אגף המבצעים" (was presented on 29 June to the deputy chief of staff and the head of operations). [S] https://palmach.org.il/history/database/?itemId=5268. The site's heading calls it "משבר המינויים" and places it in June 1948; he.wiki "מרד הגנרלים (1948)" (oldid 42740886) says "מרד שני אירע ביולי 1948" (the second revolt happened in July 1948) and gives no day. [S] See the [C] list below.
  - **The officers' resignations: 1 July 1948.** Palmach site: "ב- 1 ביולי 1948 התפטרו שלושה ראשי אגפים במטכ"ל (ידין, איילון ובן חור)... איתם התפטר גם ... ישראל גלילי" (on 1 July 1948 three General Staff branch heads, Yadin, Ayalon and Ben Hur, resigned; Galili resigned with them). [S] https://palmach.org.il/history/database/?itemId=5270. he.wiki "דוד בן-גוריון" (oldid 44100366) also says "ב־1 ביולי" four resignation letters reached him, from Yadin, Galili, Ayalon and Eliyahu Cohen [S]. The committee's protocol is consistent with it, if the matter was the resignations (it does not say): on the evening of 3 July Yadin told the committee his request to be released from his post was not caused by one matter alone, but by "ענין אחד, שקרה אתמול שלשום" (a matter that happened the day before yesterday). [V] Anita Shapira, "מפיטורי הרמ"א עד פירוק הפלמ"ח" (as hosted by Matach, kotar.cet.ac.il), protocol of the Committee of Five, session 2, 3.7.1948 evening, https://kotar.cet.ac.il/kotarapp/index/Chapter.aspx?nBookID=43787201&nTocEntryID=43893962 (only the opening of each session is online, cut off at "..."; the quotes from it keep its words, with the OCR's spacing before commas removed).
  - **The Committee of Five set up: 2 July 1948.** Palmach site: at the Provisional Government's session of 2 July 1948 it was decided to set up a committee of five ministers, one for each coalition party, chaired by Interior Minister Yitzhak Gruenbaum, to report within days because the truce was ending on 9 July. [S] https://palmach.org.il/history/database/?itemId=5270. he.wiki "ועדת החמישה" (oldid 44061692) says only "ביולי 1948"; its members were Gruenbaum, Zisling, Sharett (Shertok), Rosen and Shapira. [S] The first sitting was 3 July: Gruenbaum opened with "הממשלה בחרה בוועדת חמישה אשר צריכה בראש וראשונה לעמוד על זה איך בתנאים אלה שנוצרו, מה יש לעשות, כדי שנוכל לנהל את המלחמה כמו שצריך לנהלה" (the government chose a committee of five which must first of all establish what is to be done in these conditions, so that we can run the war as it should be run). [V] protocol, session 1, 3.7.1948, https://kotar.cet.ac.il/kotarapp/index/Chapter.aspx?nBookID=43787201&nTocEntryID=43893961
  - **Sittings: 3 to 6 July 1948, seven sessions.** Protocol headings: 1 (3.7, Tel Aviv), 2 (3.7 evening), 3 (4.7, noon), 4 (4.7, evening), 5 (5.7), 6 (6.7) and 7 (6.7, evening). [V] kotar IDs 43893961 to 43893967 on the same book. Sharett's published record gives the same dates and seven sessions: "31. ישיבות ועדת החמישה, תל-אביב - 3-6/7/1948" in *דבר דבור 1948*. [V] https://www.sharett.org.il/cgi-webaxy/sal/sal.pl?lang=he&ID=880900_sharett_new&act=show&dbid=bookfiles&dataid=3262
  - **Its report: no date and no text found.** On 6 July Gruenbaum told the committee "בהיותנו בגמר עבודתנו לניסוח מסקנות הוועדה" (as we were finishing drafting the committee's conclusions) before reading Ben-Gurion's letter [V] session 7. The editors of Sharett's book say of the conclusions: "נוסח המסקנות אינו בידינו" (the text of the conclusions is not in our hands), and quote Shapira's summary that it recommended a war cabinet and a director-general for army affairs between the minister and the chief of staff. [V] Sharett source above. So the report existed in draft by the evening of 6 July at the latest, and was shelved. [GAP] for the day it was finished or handed over.
  - **Ben-Gurion's announced resignation: 6 July 1948.** Palmach site: "ב- 6 ביולי 1948 התפטר דוד בן-גוריון מתפקידיו כראש הממשלה ושר הביטחון" (on 6 July 1948 Ben-Gurion resigned as prime minister and defence minister), before the committee's last sitting. [S] https://palmach.org.il/history/database/?itemId=5271. His letter is read out in session 7 of 6 July: "הדיון שנתקיים בוועדת החמישה וההצעות שהוכנסו על-ידי חברים אחדים הוציאו אותי ממשרד הביטחון ומאת הממשלה הזמנית" (the discussion in the Committee of Five and the proposals put in by some members have taken me out of the Defence Ministry and the Provisional Government). [V] protocol, session 7, https://kotar.cet.ac.il/kotarapp/index/Chapter.aspx?nBookID=43787201&nTocEntryID=43893967. At the opening of session 6 (6 July) Gruenbaum read out an undated letter from Ben-Gurion saying that five days of committee discussion were not helping the arrangements needed "לקראת גמר ההפוגה" (before the end of the truce). [V]
  - **Ben-Gurion's withdrawal of his resignation: 6 July 1948, evening (session 7).** Sharett's record: at the 7th session, after a short discussion "אמר היו״ר גרינבוים שב״ג הודיע שהוא חוזר בו ומוכן לשוב ולנהל את העניינים. יחד איתו תתקיים ועדה של שלושה: ב"ג, קפלן וגרינבוים" (the chairman said Ben-Gurion had announced he was withdrawing and was ready to return to running affairs; a committee of three, Ben-Gurion, Kaplan and Gruenbaum, would sit with him). [V] Sharett source above, and its note 12: "ב״ג היה חולה בימים 6-11/7/1948" (Ben-Gurion was ill 6 to 11 July 1948).
  - **The government shelves the committee's conclusions: 7 July 1948.** he.wiki "דוד בן-גוריון": "ב־7 ביולי החליטה הממשלה לגנוז את מסקנות הוועדה" (on 7 July the government decided to shelve the committee's conclusions). [S]
  - **The government accepts the rest of Ben-Gurion's demands: 11 July 1948.** Palmach site: "בישיבת הממשלה ב- 11 ביולי התקבלו שאר דרישותיו של בן-גוריון" (at the government session of 11 July the rest of his demands were accepted); it adds that Yigal Allon was named commander of Operation Dani while his appointment as front commander was put off. [S] https://palmach.org.il/history/database/?itemId=5271. Each of these three is a separate act with its own date; no source opened dates the withdrawal itself to 7 or 11 July.
  - **Galili's removal: [GAP], no day found.** Palmach site: Galili was ready to resign "ובלבד שבן-גוריון ישוב למישרתו" (so long as Ben-Gurion returned to office), and "בפרשה זו הצליח בן-גוריון לתמרן את גלילי החוצה" (in this affair Ben-Gurion managed to manoeuvre Galili out), with no date. [S] https://palmach.org.il/history/database/?itemId=5271. Sharett's record has Galili present at the first six sessions (note 2), and on 5 July Sharett floated him, hypothetically, as director-general of the Defence Ministry (note 5 says who the two director-generals would be). [V] No source opened gives the day he left his post. The 3 May and 14 May dates in `research_notes/Era 1882-1948/offices-1882-1948.md` §5.1 are the May episode, a different one.
  - **[C] Chronology and content.** (a) en.wiki says "With eight days until the end of the first truce, Ben-Gurion announced his resignation", and the truce ended on 9 July (Palmach site), which would put it on 1 July; the Palmach site and the protocol put it on 6 July, three days before. en.wiki https://en.wikipedia.org/w/index.php?title=The_Generals%27_Revolt&oldid=1361272051 [S]. (b) The en.wiki lead says Galili was "finally unseated by Ben-Gurion in June"; the he.wiki article puts the second revolt in July; the Palmach site heads the crisis "June 1948" yet dates the resignations 1 July. (c) en.wiki says Ben-Gurion accepted the committee "on condition that Galili was dismissed", and that "Galili was removed from all positions of influence"; Peres writes that the ministers "recommended too that Galili be restored to his old role as head of the national command". Shimon Peres, *Ben-Gurion: A Political Life* (2011), ch. 7, "just days before the truce was due to end", https://publicism.info/biography/ben-gurion/8.html [S]; he.wiki "דוד בן-גוריון" says the report "הקים מחדש את תפקיד הרמ"א, בשם המנהל הכללי לענייני הצבא" (re-established the post of head of the National Command under the name director-general for army affairs) [S]; Shapira's summary in Sharett's book speaks of a director-general between the minister and the chief of staff [V]. Whether that is the old post or a new one is not settled by these sources. (d) Peres has Yadin brokering the compromise at Ben-Gurion's house and Allon "given command of the key Jerusalem front"; the Palmach site says Allon's front command was put off. Not settled here.
- This is a good real PM dilemma, but the page itself frames it as partly a purge of Mapam officers. Treat it as needing historian review.

### 3c. Palmach staff disbanded
- **Order and effective date.** The Chief of Staff ordered the dissolution on **29 Oct 1948**, effective **7 Nov 1948**. [V] JVL founding page. The 7 Nov date is also given by https://www.jewishvirtuallibrary.org/the-palmach and by https://he.wikipedia.org/wiki/פלמ"ח (infobox: 15.5.1941–7.11.1948).
- **Earlier announcement:** Ben-Gurion told Palmach commanders on **14 Sept 1948** that the separate command would end. [S, search-result summary only; source pages were not opened]
- **Year** [C]: en.wiki IDF says Ben-Gurion disbanded the Palmach staff "in 1949". The Israeli sources say 7 Nov 1948. **Use 7 Nov 1948.**
- **Reasons.** he.wiki gives Ben-Gurion's statism (one army) and, in the view of some, a political aim against Mapam. Nahal was set up in August 1948 as a concession. [V]
- **Proposed event** `palmach_staff`:
  - **Window:** `from: 1948-10-29`, `to: 1948-11-07`. Anchor: soft. Office: `defense_minister`.
  - **disband** (historical): `security +0.03`; `morale -0.02` (the Mapam/kibbutz constituency).
  - **keep the separate staff**: `security -0.03`.
  - Tags: none.

### 3d. Absorbing Lehi and the Jerusalem Irgun
See section 6.

## 4. The Ten Days and Operation Dani (8/9–18 July 1948)

- **Start date** [C]: en.wiki 1948 war gives "8–18 July"; the Dani page and he.wiki give 9 July.
- **Operation Dani** ran **9–19 July 1948**. [V] https://en.wikipedia.org/wiki/Operation_Danny
  - **Commander:** Yigal Allon, with Yitzhak Rabin as his deputy (the en.wiki wording).
  - **Forces:** about 6,000 men from the Yiftach, Harel and 8th Armoured brigades, plus battalions of Kiryati and Alexandroni.
  - **Aim:** secure the Tel Aviv–Jerusalem corridor.
- **Lydda airport** was taken on **10 July**. [V] en.wiki 1948 in Israel
- **Towns** [V] en.wiki 1948 war; Dani page:
  - Lydda fell on 11 July.
  - Ramle surrendered on 12 July, after the Arab Legion withdrew.
- **Latrun** was attacked on 15–16 July and again on 18 July. Both attempts failed. [V] en.wiki 1948 war; the Dani page says Phase 2 failed.
- **Palmach dead:** 91 recorded, 44 of them at Khirbet Kurikur on 18 July. [V] Dani page
- **Same window:**
  - Operation Dekel took Nazareth on **16 July**. [V]
  - **Operation Kedem**, the Old City attempt, failed on the night of 16–17 July. [V] https://en.wikipedia.org/wiki/Operation_Kedem
- **Sensitivity.** Both the en.wiki Dani page and Morris's published work document the flight and expulsion of 50,000–70,000 inhabitants of Lydda and Ramle. Under the owner's rule ("contested narratives not shown") the event text should stay military: the airport, the Jerusalem road and Latrun. It must not make claims about civilians either way. Flag it for sensitivity review: leaving it out is an editorial choice that reviewers should approve explicitly.
- **Proposed event** `ten_days`:
  - **Window:** `from: 1948-07-08`, `to: 1948-07-10`. Anchor: hard. Office: `defense_minister`.
  - Choice: where to put the main effort when the truce ends.
  - **central_front** (historical): Dani, the Lydda/Ramle/Jerusalem corridor. `set_rung: 5`; `security +0.08`.
  - **southern_front**: concentrate against Egypt first. `set_rung: 5`; `security +0.04`.
    - This is a design alternative. I found no online source for an explicit cabinet debate in July. Ben-Gurion's diary of 11 July records Yadin asking where forces should go after Lydda and Ramle fell. [S] https://bengurionarchive.bgu.ac.il/en/node/9706
  - Tag: `war-casualties`. No money effects.

## 5. The second truce (from 18 July 1948)

- **Security Council Resolution 54** was adopted on **15 July 1948** (S/902). [V] https://en.wikipedia.org/wiki/United_Nations_Security_Council_Resolution_54
  - It found a threat to the peace under Article 39 and ordered a cease-fire within three days. It ordered an immediate, unconditional cease-fire in Jerusalem.
  - Vote: 7–1 (Syria) with 3 abstentions.
- **The truce took effect at 19:00 on 18 July 1948** and lasted until 15 October. [V] en.wiki 1948 war; he.wiki gives 18.7–15.10.
- **Israel's acceptance was cabled on 16 July 1948.** [V, agent read 2026-10-23; a person's check pending] UN doc S/903 (dated 16 July 1948, 1 p.), "Cablegram dated 16 July 1948 from the Foreign Minister of the Provisional Government of Israel to the Secretary-General in reply to the resolution (document S/902) [...]", https://digitallibrary.un.org/record/470029. Shertok transmits a resolution of the Provisional Government, which reads in part: "The Provisional Government of Israel, having taken note of the resolution of the Security Council of July 15th 1948, decided to comply with Council's request for resumption of truce in Palestine and for immediate unconditional cease fire in Jerusalem." The acceptance was conditional on the other side: "The Provisional Government is ready to issue the necessary orders to both effects as soon as it is notified that all Arab Governments and authorities concerned have likewise accepted truce in Palestine and immediate cease fire in Jerusalem and have issued orders for these arrangements to take effect at respective times laid down in resolution", and it "accepts the resumption of truce in accordance with its declared attitude as to conditions to be observed." The cable does not date the government's own decision: it says only that the resolution was adopted "after its consideration" of the Council's resolution, so the decision falls between the Council's vote of 15 July and the cable of 16 July (our reading), and the day itself is [GAP]. The same cable is in FRUS 1948 v5p2 d468's footnote (https://history.state.gov/historicaldocuments/frus1948v05p2/d468), which gives its printed text as SC, 3rd yr., Supplement for July 1948, p. 77 (not read).
  - **The cease-fire order.** [V, agent read 2026-10-23; a person's check pending] UN doc S/907 (dated 19 July 1948), p. 1: the Mediator's cablegram of 16 July set the cease-fire for "3.00 p.m. G.M.T., Sunday, 18 July 1948". S/907 p. 2, reply (d) Israel, signed Moshe Shertok, undated in the document: "View shortness time and absent reply regarding Arab decision, orders have been issued all our commanders cease fire today 7.00 p.m. Israeli Time corresponding hours fixed by you and resume firing only if other side continues." https://digitallibrary.un.org/record/647734. The reply itself says the order went out "absent reply regarding Arab decision", although S/903 had made the Arab acceptance a condition. The document does not print the reply's date; its "today 7.00 p.m. Israeli Time" is the Mediator's 3 p.m. GMT (Israel was on double summer time, GMT+4, that summer, per the IANA tz database as cited in `research_notes/M4 war mode/war-1948.md`; 19:00 on 18 July, line above), so it was probably sent on 18 July (our reading, not stated in the source).
  - **Cabinet positions and internal debate:** [GAP]. S/903 quotes the government's resolution but records no vote or positions. The Ben-Gurion Archive diary page for 16 July 1948 was not found on 2026-10-23 (https://bengurionarchive.bgu.ac.il). The cabinet minutes were not reached.
- **Proposed event** `second_truce`:
  - **Window:** `from: 1948-07-15`, `to: 1948-07-18`. Anchor: hard. Office: `prime_minister`.
  - **comply** (historical): `set_rung: 4`; `intl_standing +0.03`.
  - **defy**: keep attacking Latrun. `intl_standing -0.10` (the Article 39 threat); `security +0.02`.
  - Tags: none.
- **Companion events in the second truce** (the long lull, which is the period that most needs content):
  - **Jerusalem military government, 2 Aug 1948.** The government rejected Bernadotte's suggestion to demilitarise Jerusalem and declared the Jerusalem area under its control to be Israel-occupied territory. Dov Yosef (Dov Joseph) was appointed Military Governor (Official Gazette No. 12). [V] https://www.jewishvirtuallibrary.org/jerusalem-declared-israel-occupied-city-government-proclamation and https://en.wikipedia.org/wiki/Dov_Yosef
    - Office: `prime_minister`. **accept_demilitarisation** vs **military_government** (historical).
  - **The Israeli pound, 17 Aug 1948.** [V] en.wiki 1948 in Israel ("replaces the Palestine pound").
    - The Bank Notes Ordinance took effect on 17 Aug, with Anglo-Palestine Bank notes. [S] search summaries of https://www.jhom.com/topics/money/anglo_palestine.htm and the UN CCP paper.
    - [C] The UN CCP paper dates the new unit to 16 Aug, with old notes exchangeable 17 Aug–17 Sept at par with sterling.
    - Office: `finance_minister`. A plausible decision, but I found no sourced alternative. [GAP] on a debate.
  - **Area of Jurisdiction and Powers Ordinance, 22 Sept 1948.** It applied Israeli law to areas taken since the war began. [S] en.wiki 1948 in Israel. Office: `justice_minister` or `prime_minister`.
  - **Ben-Gurion's Latrun proposal, 26 Sept 1948.** See section 13a. This is the best "real PM decision" in the lull.

## 6. Bernadotte's assassination (17 Sept 1948): Lehi outlawed, the Jerusalem Irgun disbanded

- **The assassination:** **17 Sept 1948**, in Jerusalem (Katamon), by Lehi. [V] https://en.wikipedia.org/wiki/Folke_Bernadotte; en.wiki 1948 in Israel. (A footnote on en.wiki Lehi gives 16 Sept, which is an outlier.)
- **Bernadotte's second plan** was completed on **16 Sept 1948**, the day before. [V] en.wiki 1948 war; [S] the Bernadotte plan page.
- **Lehi arrests:** about 200 Lehi members were arrested. Yellin-Mor and Shmuelevitz were arrested on 29 Sept 1948. [V] https://en.wikipedia.org/wiki/Lehi_(militant_group)
- **Lehi in Jerusalem** was dissolved by government order, per JVL "until September 17, 1948". [V] JVL founding page.
- **Prevention of Terrorism Ordinance No. 33, 5708-1948**, dated **23 Sept 1948**. [V] https://en.wikipedia.org/wiki/Prevention_of_Terror_Ordinance; also https://www.jewishvirtuallibrary.org/jsource/Terrorism/terrorord.html and https://palquest.org/en/node/34119 (search summaries).
  - [C] en.wiki Lehi says it was passed "three days after", and a photo caption dates the terrorist designation to 20 Sept.
  - A UN cable of 21 Sept 1948 already reports emergency regulations issued by the Defence Minister.
  - Reading: emergency regulations came around 20 Sept and the ordinance on 23 Sept. Mark [C].
- **Jerusalem Irgun ultimatum:** issued by the IDF on **20 Sept 1948 at 12:00**, with 24 hours to disband, hand over arms and enlist. [S] daat.ac.il reproduces the text: https://daat.ac.il/DAAT/english/history/lapidot/36.htm
  - The US Consul reported on 22 Sept that the Irgun had agreed the previous day (21 Sept). [S] https://history.state.gov/historicaldocuments/frus1948v05p2/d609
  - JVL says the Irgun battalions in Jerusalem were "disbanded on September 20, 1948". [V]
  - [C] Formal Irgun dissolution: 12 Jan 1949 (en.wiki Irgun) vs 1 Sept 1948 (Britannica, search summary).
- **Sentences and amnesty.** Yellin-Mor and Shmuelevitz were sentenced on 10 Feb 1949 (8 and 5 years) and released. The general amnesty is dated and named below; the two men's own release day is a separate question. Tags: [V, agent read 2026-10-23; a person's check pending] for the Provisional State Council protocol, [S, agent read 2026-10-23; a person's check pending] for the rest ("last" sitting rests on Tidhar, below).
  - **The instrument: "פקודת חנינה כללית, תש״ט–1949" (General Amnesty Ordinance, 5709-1949), an ordinance of the Provisional State Council,** Ordinance No. 60 of 5709-1949, Official Gazette (ע״ר) 5709, supplement A, p. 173, dated "י״א בשבט תש״ט (10 בפברואר 1949)" and signed by Ben-Gurion (Prime Minister and Defence Minister), Rosenblit (Justice) and Shitrit (Police). [S] the text on Hebrew Wikisource (oldid 2936978, a copy of the law database); the Gazette page itself was not reached. Section 1: "כל אדם שביום הינתן תוקף לפקודה זו נמצא במאסר או במעצר, ישוחרר, אלא אם נמצא במאסר או במעצר מפני שנידון או נאשם ברצח או בעבירה אחרת שהעונש המקסימלי שהחוק קובע לה הוא עונש מוות או מאסר עולם" (anyone in prison or detention on the day this ordinance takes effect shall be released, unless held for having been sentenced or accused of murder or another offence whose maximum penalty is death or life imprisonment). Section 5: it takes effect "מיום קבלתה על ידי מועצת־המדינה הזמנית" (from its adoption by the Provisional State Council). Section 2 also stops proceedings for offences committed before 10 February 1949, with the same exceptions. The ordinance does not name Lehi.
  - **The date it was adopted: 10 February 1949,** at the Provisional State Council's 40th and last sitting, Tel Aviv, agenda item ג(1) (contents: p. 14). The chairman, Y. Sprinzak, records the vote: "אישרה פה אחד את פקודת החנינה הכללית" (the Council approved the General Amnesty Ordinance unanimously), and the protocol has "הפקודה נתקבלה פה אחד". [V] https://fs.knesset.gov.il/0/Plenum/0_ptm_250008.pdf, read from the Wayback copy of 29 March 2019. The ordinance text is in the protocol's annex, p. 24, which was not read; the PDF's text layer runs the two columns together, so the speeches were read only in part. In them Ben-Gurion gives counts of prisoners and detainees the ordinance would and would not reach, and a speaker raises members of an organisation declared terrorist under the Prevention of Terror law. No passage read names Lehi or either man.
  - **Contemporary account tying it to the two men:** Tidhar's *Encyclopedia of the Founders and Builders of Israel*, vol. 3, p. 1530 (1949): the Council "אישרה בישיבתה האחרונה את הצעת הממשלה הזמנית לתת חנינה" on 10.2.49 "לכל העצורים והיושבים במאסר... על עבירות ופשעים פוליטיים ופליליים כאחד" (approved at its last sitting the Provisional Government's proposal to grant amnesty to all detainees and prisoners for political and criminal offences alike); the court sentenced the two the same day and offered release on a declaration and police supervision, "שני הנאשמים סירבו... אך שוחררו בתוקף החנינה הכללית" (both refused, but were released under the general amnesty). [S] https://www.tidhar.tourolib.org/tidhar/view/3/1530. Tidhar also reports a government plan, before the vote, for the President to pardon the Lehi detainees after his election, which a hunger strike hastened. [S]
  - **When the two men were freed [C].** he.wiki "נתן ילין-מור" (oldid 44030100): "הוא שוחרר לאחר יומיים במסגרת החנינה הכללית" (released two days later), which would be about 12 February if counted from the sentence (the note's own arithmetic; the article gives no start point or date). [S] The article's footnote is a Herut item of 13 February 1949, headed "שוחררו פרידמן ילין, שמואלביץ וכל עצורי לח"י" (Friedman-Yellin, Shmuelevitz and all the Lehi detainees released); that item was not opened. The Government Press Office photograph on Wikimedia Commons ("Mati Sh 1949.jpg", GPO caption cut off in the original) shows them leaving Acre prison "following the general amnesty", with the file's date field reading 20 February 1949 (a metadata date, not a stated event date). [S] https://commons.wikimedia.org/wiki/File:Mati_Sh_1949.jpg. The Lehi association's party page says Yellin-Mor "שוחרר מיד" (was released at once) after the elections and the other detainees were freed "בעקבות חוק החנינה הכללית". [S] The sources do not say whether these are one release or several. No Gazette notice or prison record of the release day was found.
  - **en.wiki's inconsistency, as it stands.** en.wiki "Lehi (militant group)" (oldid 1381410332) says both that "just before the first Israeli elections in January 1949, the government granted all Lehi prisoners amnesty under a state general order of pardon" and that "the State (Temporary) Council soon announced a general amnesty" after the 10 February sentences. The elections were on 25 January (Tidhar, p. 1530), and the ordinance is dated 10 February, so the "before the elections" wording matches no instrument found. [C] en.wiki, which also gives no instrument or date. The Lehi association page names only "החנינה הכללית" and "חוק החנינה הכללית" and gives no date for the amnesty. [S] https://lehi.org.il/he/%d7%99%d7%95%d7%a0%d7%99-1948-1951/
- **Proposed event** `bernadotte_assassination`:
  - **Window:** `from: 1948-09-17`, `to: 1948-09-23`. Anchor: hard. Office: `prime_minister` (the emergency regulations were the Defence Minister's, also Ben-Gurion).
  - **outlaw** (historical): outlaw Lehi, arrest its members, give the Jerusalem Irgun an ultimatum. `intl_standing +0.03` (partial recovery); `security +0.02`.
  - **investigate_only**: no general crackdown. `intl_standing -0.10`.
  - The `intl_standing` hit from the killing itself should apply to both options, or sit in the event text.
  - Tag: `terror`. The Israeli government's own ordinance treated it as terrorism, so the tag matches the Israeli framing. No money effects.

## 7. Operation Yoav (15–22 Oct 1948)

- **Dates:** 15–22 Oct 1948. [V] https://en.wikipedia.org/wiki/Operation_Yoav; the start date is also given by en.wiki 1948 in Israel.
- **Cabinet approval:** approved at the **Cabinet session of 6 Oct 1948**. [V] Yoav page
- **Trigger:** an Israeli supply convoy was fired on as it passed Egyptian positions, on **14 Oct**. [V]
- **Commander:** Yigal Allon (Southern Front). [V]
- **Beersheba** was taken on **21 Oct**, the Negev Brigade. [V] (two pages)
- **Third cease-fire** took effect on **22 Oct**. [V] en.wiki 1948 war
- **Debate.** At the end, Allon asked to push into the Hebron hills and the Jericho valley. Ben-Gurion refused, "mainly due to fears that this would lead to British involvement". [V] Yoav page. That is a real, sourced alternative.
- **Background.** On 26 Sept the cabinet had rejected Ben-Gurion's central-front proposal (section 13a), so the southern offensive was the alternative that was approved. That sequence is my inference from the two dates; I found no page stating the causal link [S].
- **Proposed events** `operation_yoav` (cabinet, 6 Oct) and an optional follow-up, `yoav_hebron` (late Oct):
  - **operation_yoav:** **launch** (historical): `set_rung: 5`; `security +0.08`. **wait**: `security -0.05`. The Negev convoys stay blocked; that is a design effect.
  - **yoav_hebron:** **stop** (historical) vs **push into the Hebron hills**: `intl_standing -0.08`, plus British-intervention risk.
  - Tag: `war-casualties`.

## 8. Operation Hiram (late Oct 1948)

- **Dates** [C]:
  - 29–31 Oct 1948, "launched on the night of 28–29 October", about 60 hours. [V] https://en.wikipedia.org/wiki/Operation_Hiram
  - 28–31 Oct per PalQuest and the original order dated 28 Oct. [S]
  - en.wiki 1948 war says "launched on 24 October". This is an outlier; do not use it.
- **Trigger.** On 22 Oct the Arab Liberation Army attacked the Sheikh Abd position above Kibbutz Manara, in breach of the truce, and sniped at Manara on 24–25 Oct. Qawuqji refused to withdraw, and Israel told the UN it felt free to act. [V] Hiram page
- **Commander:** Moshe Carmel. [V]
- **Outcome:** the Upper Galilee and a strip of southern Lebanon taken; the ALA driven into Lebanon. [V]
- **Ceasefire:** 31 Oct, 11:00. [V]
- **Alternative actually raised.** Ben-Gurion declined to authorise an advance further into Lebanon (the page says towards Beirut), fearing international condemnation. [V] Hiram page
- **Sensitivity.** The en.wiki page also carries population claims (the over-50,000 figure) and a cabinet quote of 26 Sept. Both are contested material under the owner's rules. Keep the text military.
- **Proposed event** `operation_hiram`:
  - **Window:** `from: 1948-10-24`, `to: 1948-10-29`. Anchor: soft. Office: `defense_minister`.
  - **launch** (historical): `security +0.08` (north); `set_rung: 5`.
  - **protest_to_un**: `security -0.04`.
  - Optional second choice, **advance into Lebanon**: `intl_standing -0.08`.
  - Tag: `war-casualties`.

## 9. Operation Horev, the British ultimatum and the RAF clash (Dec 1948 – 7 Jan 1949)

- **Start date** [C]:
  - **22 Dec 1948.** [V] https://en.wikipedia.org/wiki/Operation_Horev; JVL https://www.jewishvirtuallibrary.org/jsource/History/horev.html (search summary)
  - **27 Dec.** en.wiki 1948 in Israel. This is the date of the al-Auja attack.
  - **Use 22 Dec.**
- **End:** 7 Jan 1949. [V]
- **Commander:** Yigal Allon, with five brigades. [V]
- **Into Sinai.** The Negev Brigade crossed the border on the night of 28 Dec and reached the outskirts of El-Arish airfield by 30 Dec. [V] Horev page
- **UN cease-fire:** the Security Council called for one on 29 Dec 1948. [V] Horev page
- **US pressure.** The State Department's message of 30 Dec was read to Ben-Gurion by McDonald. [V] FRUS 1949 vol. VI doc 341 (McDonald to the Secretary of State, 1 Jan 1949, 09:00) https://history.state.gov/historicaldocuments/frus1949v06/d341
  - In it, Ben-Gurion says Israeli forces "have already received orders to return to the Negev frontier".
  - He also says Britain "threatens to take action against us under 1936 Treaty with Egypt".
  - So the British threat came under the 1936 Anglo-Egyptian treaty and was known by 31 Dec/1 Jan. The exact date of the British note is a [GAP].
- **Withdrawal order.** Israeli forces were ordered out of Sinai with total withdrawal by **2 Jan 1949**. [S] FRUS 1949 doc 371 summary: https://history.state.gov/historicaldocuments/frus1949v06/d371
  - A further attempt at Rafah on 3 Jan was halted by Ben-Gurion. [S, weaker source]
- **RAF clash: date** [C]:
  - **7 Jan 1949**: JVL (IAF page: "five RAF planes in two dogfights on January 7, 1949", https://jewishvirtuallibrary.org/israel-air-force-in-the-war-of-independence), en.wiki 1949 in Israel and en.wiki 1948 war.
  - **6 Jan**: en.wiki Horev.
  - **Use 7 Jan** (three sources).
- **RAF clash: losses** [C]: five RAF aircraft (four Spitfires, then a Tempest). Two pilots killed and two taken prisoner, per en.wiki Horev and 1949 in Israel.
  - The ASN narrative says ground fire downed one Spitfire. [S] https://asn.flightsafety.org/wikibase/121188
- **Aftermath:**
  - Ben-Gurion ordered the wrecks moved into Israeli territory. [V] en.wiki 1948 war
  - Britain blocked aviation fuel shipments to Israel in January 1949. [V] en.wiki 1948 war
  - Israel accepted the cease-fire on **7 Jan 1949**. [V] Horev page
  - Egypt announced on **6 Jan** that it would negotiate. [V] Horev page
  - Rhodes talks opened on **13 Jan 1949**. [V] he.wiki elections page
- **Debated alternative.** Allon's forces could have cut off the Egyptian army from El-Arish, and the withdrawal ended that chance. This is documented in Avnery's commentary [S] and implied in the Horev page.
- **Proposed events**:
  - **`operation_horev`.** Window: `from: 1948-12-20`, `to: 1948-12-22`. Anchor: hard. Office: `defense_minister`. **launch** (historical): `set_rung: 5`; `security +0.06`.
  - **`sinai_withdrawal`.** Window: `from: 1948-12-31`, `to: 1949-01-02`. Anchor: hard. Office: `prime_minister`.
    - **withdraw** (historical): `intl_standing +0.05`; `security -0.02`.
    - **hold El-Arish**: `intl_standing -0.15`; British relations hit (`uk` standing in model.yaml, if events can target it later); `security +0.05`.
  - **`raf_clash`.** Window: 7 Jan 1949. Probably text-only, or a choice between **accept the ceasefire** (historical, `set_rung: 4`) and **fight on**.
  - Tag for `operation_horev`: `war-casualties`.

## 10. The population registration of 8 Nov 1948

- **Date** [C]: **8 Nov 1948** per CBS (already `population.yaml`, fact-checked), en.wiki elections and he.wiki elections. en.wiki 1948 in Israel gives "8 or 11".
- **How it was done.** The whole country was under a seven-hour curfew, from 17:00 to midnight, while enumerators went door to door. [V] https://he.wikipedia.org/wiki/הבחירות_לאספה_המכוננת
- **Purpose:** to build the voters' roll for the Constituent Assembly. [V] en.wiki and he.wiki elections pages.
- **Eligible voters** [C]: 506,507 (JCPA/JCFA, https://www.jcfa.org/wp-content/uploads/art/knesset1.htm) vs 506,567 (he.wiki).
- **Fit as a decision: weak.**
  - No debated alternative was found [GAP]. The responsible office and minister are also a [GAP]; it was probably the Interior Ministry (Gruenbaum), but that is unverified.
  - **Recommendation:** fold it into the elections event (section 11) as the preparatory step, or show it as a notification. Do not make it a standalone choice: an invented "skip the census" option would have no historical basis. This keeps the calibration anchor untouched.

## 11. Elections to the Constituent Assembly (25 Jan 1949)

- **Original deadline.** The Declaration of Independence required elected institutions by **1 Oct 1948**. [V] he.wiki elections; en.wiki https://en.wikipedia.org/wiki/1949_Israeli_Constituent_Assembly_election
- **Postponements.** The vote was postponed twice because of the war. [V] (both pages)
  - IDI: scheduled for October 1948, postponed three months. [S] https://en.idi.org.il/israeli-elections-and-parties/elections/1949/
  - **Who sets the date.** [V, agent read 2026-10-23; a person's check pending] The Constituent Assembly Elections Ordinance, 5709-1948 (Official Gazette 5709 p. 52, signed 18 Nov 1948), s. 1: "הבחירות לאסיפה המכוננת של מדינת ישראל תיערכנה במועד שתחליט עליו מועצת המדינה הזמנית; המועד יפורסם בעתון הרשמי לפחות 54 יום לפני יום הבחירות". Claude's translation: *the elections shall be held on the date the Provisional State Council decides; the date shall be published in the Official Gazette at least 54 days before election day*. Section 43 forbids the Central Elections Committee to postpone the election date. Text read on Hebrew Wikisource, https://he.wikisource.org/wiki/פקודת_הבחירות_לאסיפה_המכוננת. Its scan link, https://fs.knesset.gov.il/0/law/0_lsr_312706.pdf, was geo-blocked (an HTTP 303 to an HTML page) on 2026-10-23.
  - **First postponement, October to mid-November 1948.** [S, agent read 2026-10-23; a person's check pending] JTA Daily Bulletin, 9 Sept 1948: "The Israeli elections, originally scheduled for October, will now be held in the middle of November, a spokesman for the Israeli mission here announced late today. He said a telegram to the mission from Tel Aviv blamed technical difficulties for the postponement." https://www.jta.org/archive/u-s-de-jure-recognition-of-israel-depends-on-israeli-elections-marshal-declares. This is a contemporary press report of an announcement in Washington; it does not say which body decided or on what day. **Deciding body and date: [GAP].** The Provisional State Council's sittings of Aug-Sept 1948 (Knesset data via oknesset `kns_plmsessionitem`) list no elections item. The protocols themselves (fs.knesset.gov.il) were geo-blocked.
  - [C] **The original date.** JTA of 10 Aug 1948 (link in the wartime-debate bullet below) says the elections "will be held in the late autumn, if fighting does not resume", and quotes a critic on "the scheduled elections next month" (September, by our reading). The 9 Sept item says "originally scheduled for October" and "It was Israel's original intention to hold the elections on October 1, the date set by the United Nations." Each version is given here with its source; none is chosen.
  - **In between.** [S, agent read 2026-10-23; a person's check pending] JTA, 14 Nov 1948: "The first national election in Israel will be held between Jan, 10 and 15, according to present plans of the government as outlined today by Nahum Rafalkes-Mir [sic; Nir], vice-chairman of the State Council." https://www.jta.org/archive/israeli-national-elections-to-be-held-in-middle-of-january-official-announces. This states a plan, not a decision.
  - **Date fixed for 25 Jan 1949 by the "State Council" (JTA's wording; the second postponement, by our reading).** [S, agent read 2026-10-23; a person's check pending] JTA Daily Bulletin, 28 Nov 1948: "The Israeli State Council unanimously decided last night to hold national elections Jan. 25. The decision was taken after Interior Minister Itzhak Gruenbaum revealed that on the basis of the completed population census, a total of 477,000 residents will be entitled to cast their ballots [...]". https://www.jta.org/archive/israeli-elections-to-be-held-jan-25-census-shows-477000-eligible-to-vote
    - **The day.** The JTA item does not print the day of its own dispatch, so "last night" does not fix it. The Council's 28th sitting was on 25 Nov 1948 (Knesset data: PlenumSessionID 489504, "ישיבת מליאה בתאריך 25/11/1948", https://production.oknesset.org/pipelines/data/plenum/kns_plenumsession/kns_plenumsession.csv). The sitting before it was on 18 Nov, and none followed until 2 Dec. The same JTA item reports a closed night session on Negev policy, and the 25 Nov agenda has a "דיון מדיני" (political debate) item. So 25 Nov 1948 is the only candidate sitting (our reading), but its agenda lists no elections item, so the **decision date stays [GAP]** until the protocol is read. The protocol was not read: fs.knesset.gov.il was geo-blocked.
  - **How this maps onto "postponed twice".** The two-postponement count (the pages above) fits October → mid-November → 25 Jan. The 14 Nov "Jan 10-15" plan sits between the two and was not a separate decision in any source read.
- **Size of the assembly.** On **5 Nov 1948** the Provisional State Council set it at **120 seats**, kept nationwide PR, and left the electoral system to the constitution. [V] (both)
- **The vote:** **25 Jan 1949** (24 Tevet 5709), a rest day. [V]
  - Turnout: 86.9% (en.wiki), about 440,095 voters (he.wiki).
  - About 1,000 polling stations; 21 lists. [V] en.wiki
  - Results [V] en.wiki: Mapai 46, Mapam 19, URF 16, Herut 14, General Zionists 7, Progressives 5, Sephardim 4, Maki 4.
  - Soldiers voted at the front. [V, he.wiki photo captions only]
- **Timing.** The vote fell after the 7 Jan cease-fire on the Egyptian front and 12 days after Rhodes opened. The campaign was largely about continuing the war or seeking armistices. [V] he.wiki; JCPA
- **Debate over holding elections in wartime:** no source read gives a debate with named sides. The cabinet and Council protocols were not reached (geo-blocked). What contemporary reports give:
  - **Reasons given for the first postponement** [S, agent read 2026-10-23; a person's check pending] (JTA, 9 Sept 1948, link above). The mission's spokesman "called these difficulties the strife-caused shortage of manpower to conduct an election, the drafting of Israel’s now [sic; new] constitution, and the scarcity of paper and printing equipment for ballots."
  - **An outside accusation** [S, agent read 2026-10-23; a person's check pending] (JTA, 10 Aug 1948). Konrad Bercovici, vice-chairman of the American League for a Free Palestine, told a New York press conference that the Provisional Government "is preparing steps to perpetuate itself even, if necessary, by suspending the scheduled elections next month." https://www.jta.org/archive/israel-state-council-begins-preparations-for-elections-to-constituent-assembly. This is one critic's claim, reported as such, and not evidence of a cabinet position.
  - **Debate inside the government or the Council:** [GAP].
  - **Recommendation:** model the decision as **hold on 25 Jan** (historical) vs **postpone again until the armistices**. The second option is a plausible design alternative given the two real postponements, but no named advocate exists, so the text must not attribute it to anyone.
  - Effects: hold gives `morale +0.03`, `intl_standing +0.03`; postpone gives `intl_standing -0.05`.
  - Office: `prime_minister` (or `interior_minister`, unverified). Anchor: hard.
  - **Window:** `from: 1948-11-05`, `to: 1949-01-25`.
- **Related:** the Knesset's first sitting on 14 Feb 1949, and Weizmann elected president on 16 Feb 1949, 83–15 over Joseph Klausner. [V] en.wiki 1949 in Israel. These are already partly covered in people.yaml.

## 12. Operation Magic Carpet (Yemen)

- **Start** [C]:
  - **16 Dec 1948**, the first flight: 50 orphans out of Aden on a DC-3. [V] https://en.wikipedia.org/wiki/Operation_Magic_Carpet_(Yemen)
  - JDC: "December 1948–September 1950". [S] https://archives.jdc.org/historic-sound-recording-from-1949-operation-magic-carpet/
  - en.wiki Yemenite Jews in Israel: "June 1949". [C] That is the start of the main wave.
- **Phases** [V] Magic Carpet page:
  - Stage 1, Dec 1948 to Mar/Apr 1949: about 7,000 people (about 4,500 by end-Feb 1949).
  - Stage 2, Jul–Nov 1949: about 30,000.
  - The operation ended on 24 Sept 1950. Total about 49,000 [C: 45,000–49,000 across sources].
- **Who decided.**
  - The Israeli government made 3,500 visas available. The JDC ran the airlift from Aden (Hashed camp). [V] Magic Carpet page
  - "David Ben-Gurion was reluctant at first but eventually agreed". [S, en.wiki only; uncited claim on that page]
- **Fit for this window:** small (stage 1). It fits better as a 1949 event (stage 2, July 1949), which is outside this window.
  - If used here: window `from: 1948-12-01`, `to: 1948-12-16`; office `immigration_minister` (Shapira).
  - **airlift** (historical): `housing -0.01`, `morale +0.02`.
  - **wait for the sea route**: Suez was closed to Israel-bound ships [V], so `morale -0.02`.

## 13. Other PM decisions in the window

### 13a. Ben-Gurion's proposal to attack Latrun (26 Sept 1948): strong candidate
- **Ben-Gurion's diary, 26 Sept 1948.** He proposed to the cabinet to attack and take Latrun. The proposal was rejected. [V primary] https://bengurionarchive.bgu.ac.il/en/node/6251
- **The vote as the diary records it: 7–5.**
  - **Against (7):** Rosen (Rosenblüth), Moshe Shapira, Levin, Bernstein, Kaplan, Remez, and "Sharett (Sheetrit?)"; the diary page has a question mark here.
  - **For (5):** Fishman (Maimon), Gruenbaum, Zisling, Bentov, and Ben-Gurion.
- [C] Lior Lavid (BGU, citing Bar-Zohar) describes the proposal as clearing the road from Latrun to Jericho and the Dead Sea, rejected "by a majority of one", according to Ben-Gurion. https://in.bgu.ac.il/bgi/israelis/DocLib/Pages/2012/ליאור-לביד.pdf (extracted text partly garbled).
  - Bar-Zohar (via Lavid) says Ben-Gurion kept the vote quiet for years so as not to embarrass the ministers who opposed it.
- **Ben-Gurion's diary, 29 Sept.** He told the General Staff that an attack was "possible but not at this time". [S] https://bengurionarchive.bgu.ac.il/en/node/12491
- **Proposed event** `latrun_proposal`:
  - **Window:** `from: 1948-09-24`, `to: 1948-09-26`. Anchor: hard. Office: `prime_minister`.
  - **Mechanic:** the player-PM proposes, and the cabinet's historical decision is "reject". Options are **bring to cabinet and accept the vote** (historical) or **press the offensive**: `security +0.05`, `intl_standing -0.10`, coalition-stability cost (the ministers who voted against).
  - **Avoid** the 26 Sept "Galilee clean of Arabs" quote on en.wiki Hiram: it is contested, and the owner's rules exclude it.

### 13b. Jerusalem front
- **2 Aug 1948:** military government in Jerusalem. See section 5.
- **30 Nov 1948:** the Dayan–Abdullah el-Tell "complete and sincere cease-fire" agreement for the Jerusalem area, signed at Government House with UN observers present. [V] FRUS 1948 vol. V pt 2, editorial note: https://history.state.gov/historicaldocuments/frus1948v05p2/d800
  - Talks began on 28 Nov. [S] UN press release
  - On 23 Dec, el-Tell apparently received new orders and avoided further talks. [S] FRUS d844
  - **Event:** **sign** (historical; `security +0.03` for Jerusalem) vs **hold out for the Old City / Latrun road**. Office: `prime_minister`.
- **Operation Kedem (16–17 July):** the failed Old City attempt. Good event text, not a strong choice.

### 13c. Etzion bloc and Old City POWs
- **Release in March 1949:** 320 prisoners from the Etzion settlements, 85 of them women. [V] https://en.wikipedia.org/wiki/Kfar_Etzion_massacre (no exact day)
  - [C] A footnote on the same page gives 670 prisoners in total.
  - [S] The last Old City POWs returned on 3 March 1949.
  - Partial releases followed the 30 Nov 1948 agreement. [S]
- Outside the window (after 24 Feb 1949) and with no decision found. Not recommended. The page title also makes it a `massacre`-tagged topic.

### 13d. First US loan: Export-Import Bank
- **Application:** Israel applied for $100m on **2 June 1948**. [S] FRUS summary (search result)
- **Earmark, Jan 1949:** $100m earmarked for Israel: $35m for agriculture authorised, plus $65m available to 31 Dec 1949 for communications, transport, manufacturing, housing and public works, at 3.5%.
  - [S] The Federal Reserve press release is dated **19 Jan 1949**: https://fraser.stlouisfed.org/files/docs/historical/martin/12_03_19490119.pdf (the PDF could not be text-extracted; the facts are from the search summary).
  - [V] FRUS 1949 doc 956 refers to the $100m "earmarked for Israel last January". https://history.state.gov/historicaldocuments/frus1949v06/d956
  - [C] A later State Department memo says "spring of 1949". A 1952 JTA piece garbles the tranches. https://jta.org/archive/israel-starts-payments-on-american-export-import-bank-loan
- **Fit:** a finance_minister event. **accept the conditional US credit** (historical; `fx_reserves +35` for the first tranche, or the per-year share).
  - Alternative: rely on the UJA and diaspora only. No sourced debate; [GAP].
  - Not sensitive, so positive fx is allowed.

### 13e. Israeli pound (16/17 Aug 1948)
See section 5. Better as a notification than a choice ([GAP] on alternatives).

### 13f. UN membership bid
- **Applied:** 29 Nov 1948. [S] NZ parliamentary paper (search summary)
- **Outcome** [C]: the Security Council vote on 17 Dec 1948 failed at 5–1–5 (Jewish Press) vs "decided to defer" (NZ paper).
- **Later:** admitted after Security Council Resolution 69 (4 Mar 1949) and GA Resolution 273 (May 1949), both outside the window.
- Weak as a choice; the alternative would be to wait.

### 13g. The Generals' Revolt (June–July 1948)
See section 3b. A real PM crisis: Ben-Gurion threatened to resign during the first truce. It needs historian review because of the Mapam framing.

---

## Ranking for implementation (strength of sourcing × quality of the decision)

| # | Event | Window | Sourcing | Decision quality |
|---|---|---|---|---|
| 1 | `first_truce` | 1–11 Jun 1948 | V (UN, Eban letter, IDF/JVL); end date C (8/9 Jul) | High |
| 2 | `altalena` | 20–22 Jun 1948 | V (two pages agree) | High; real compromise option |
| 3 | `bernadotte_assassination` | 17–23 Sep 1948 | V; ordinance and ultimatum dates C (20 vs 23 Sep) | High |
| 4 | `latrun_proposal` | 26 Sep 1948 | V primary (BG diary); vote count C (7–5 vs majority of one) | High; real cabinet vote |
| 5 | `operation_yoav` | 6–15 Oct 1948 | V (cabinet 6 Oct) | Medium-high; real Hebron alternative |
| 6 | `sinai_withdrawal` (Horev) | 31 Dec 1948 – 2 Jan 1949 | V (FRUS d341); British note date GAP; RAF date C (6/7 Jan) | High |
| 7 | `second_truce` | 15–18 Jul 1948 | V (Res 54); Israeli acceptance GAP | Medium |
| 8 | `ten_days` | 8–10 Jul 1948 | V; alternative weak (S) | Medium; sensitive |
| 9 | `operation_hiram` | 24–29 Oct 1948 | V; start date C (28/29 Oct) | Medium; sensitive |
| 10 | `elections_1949` | 5 Nov 1948 – 25 Jan 1949 | V facts; wartime debate GAP | Medium |
| 11 | `jerusalem_military_government` | 2 Aug 1948 | V | Medium |
| 12 | `palmach_staff` | 29 Oct – 7 Nov 1948 | V (7 Nov); C (1948 vs 1949 on en.wiki IDF) | Medium |
| 13 | `jerusalem_ceasefire` (Dayan–el-Tell) | 28–30 Nov 1948 | V (FRUS) | Medium-low |
| 14 | `exim_loan` | Jan 1949 | S/C | Medium-low |
| 15 | Magic Carpet stage 1 | 16 Dec 1948 | V (en.wiki), C on start | Low here; better in mid-1949 |
| 16 | Census | 8 Nov 1948 | V | Not a decision; fold into elections |
| 17 | Israeli pound | 16/17 Aug 1948 | V/C | Notification |

**Coverage of the 40 empty turns** (in weekly turns, unless a truce event moves the game to rung 4):
- June: 1, 2, 3g
- July: 8, 7
- August: 11, 17
- September: 3, 4
- October: 5, 9, 12
- November: 16, 10, 13
- December: 15, 6
- January: 6 (RAF clash), 14, 10
- February: the existing armistice event

Gaps remain in the second half of August and in early December.

## Open gaps to close with print sources (historian review)
- Names of Israeli ministers or generals who argued for refusing the first truce, if any. Sources: Gelber, Bar-Zohar.
- The date and terms of Israel's acceptance of the second truce.
- The exact date of the British note under the 1936 treaty (late Dec 1948).
- Generals' Revolt: the day the Committee of Five's report was finished, its text, and the day Galili left his post (section 3b). The other steps are dated from the committee's protocol and the Palmach site.
- Who ran the 8 Nov registration (which ministry), and whether there was any debate.
- Any recorded debate on holding the elections in wartime, and the dates of the two postponement decisions.
- Lehi amnesty: dated and named in section 6 (General Amnesty Ordinance, adopted 10 Feb 1949). Still open: the Gazette page itself, the ordinance text in the protocol's annex, and the day the two leaders were freed ([C], about 12 or 20 Feb).
- The Altalena cabinet: whether any minister objected on 20 June or resigned afterwards.
