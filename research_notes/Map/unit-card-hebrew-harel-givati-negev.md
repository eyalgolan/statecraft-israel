# Unit cards in Hebrew: Harel, Givati, Negev (research note, 2026-10-19)

Status: an AI draft for issue #558 (owner's scoped slice, 19 October 2026). Claude prepared it. Every Hebrew text below
is `agent-checked`: an AI agent wrote it and compared each proper name against the cited page, and no person has read
it yet. A native Hebrew reader (Beri, `beriki770-ship-it`) and the normal native-speaker review (#564) must confirm
every row before any of it goes into `content/`. Nothing in `content/`, `unity/`, the sim, a content hash or a review
flag was changed. Carmeli is out of scope (#1090 changes its anchors).

Read against `main` at `9a69d5b4`: `unity/Assets/Scripts/UI/UnitCard.cs` and `content/map/units_1948.yaml`.

## What the card inserts from English data

`UnitCard.For` builds the card body from Hebrew templates (`unity/Assets/UI/strings.he.yaml`) but fills them with
English values from the unit's row:

| Template key | Hebrew template on main | English value it inserts |
|---|---|---|
| `unit.position` / `unit.position_plain` | `מיקום: {0}, מאז {1}` / `מיקום: {0}` | the anchor's `place` ({0}) and `since` ({1}) |
| `unit.formed` | `הוקמה: {0}` | the unit's `formed` |
| `unit.commander` | `מפקד ראשון: {0}` | the unit's `first_commander` |
| (none, a line of its own) | | the unit's `sector` |

The owner's comment names three of these: anchor place, first commander and sector (section 1). `since` and `formed`
are English dates inside the same lines; they are given in section 2 so the later schema PR can decide whether they
need Hebrew fields too. The subtitle's `number` ("5th Brigade") is also English; it is listed in section 2 as well.
The proposals are written to read correctly inside the Hebrew templates above (the place after `מיקום:`, the date
after `מאז`), and none of the English values carries a placeholder, so there is none to keep.

## 1. The three fields the owner named

Status of every row: `agent-checked`.

### Givati (`givati`)

| Field | Current English | Proposed Hebrew | Source for each proper name | Notes |
|---|---|---|---|---|
| anchor `place` | Gedera | גדרה | גדרה: CBS locality list, code 2550 [1]; Haganah lexicon, "Givati" [2] | |
| `first_commander` | Shimon Avidan | שמעון אבידן | Haganah lexicon, "Givati": "בפיקוד מפקד החטיבה שמעון אבידן" [2] | The same page also writes "שמעון קוך- אבידן" (his earlier surname). The card keeps the name the English uses. |
| `sector` | Moved to the south as the war entered its second stage, concentrated mainly around Gedera, Gan Yavne and Be'er Tuvia. | עברה לדרום עם כניסת המלחמה לשלבה השני, והתרכזה בעיקר באזור גדרה, גן יבנה ובאר טוביה. | גדרה: CBS 2550 [1]. גן יבנה: CBS 166 [1]. באר טוביה: CBS 155 [1]; the Haganah lexicon writes "באר-טוביה" [2] | Feminine verbs: the subject is the brigade (חטיבה). The CBS spelling without the hyphen is today's official form. |

### Harel (`harel`)

| Field | Current English | Proposed Hebrew | Source for each proper name | Notes |
|---|---|---|---|---|
| anchor `place` | Sha'ar HaGai | שער הגיא | Ministry of Defence, Izkor, Harel memorial rock: "בין שער הגיא לסריס" [3] | |
| `first_commander` | Yitzhak Rabin | יצחק רבין | Ministry of Education, "Yitzhak Rabin": "הוא היה מפקד חטיבת הראל בחזית ירושלים" [4]; he.wiki "חטיבת הראל" (pointer only): "ולמפקדה הראשון מונה יצחק רבין" [5] | The Education page says he commanded Harel; that he was its *first* commander comes from he.wiki only (as the English row's own sources do). |
| `sector` | All units in the Jerusalem corridor and hills; in the first half of May 1948 it took the corridor and opened the road to Sha'ar HaGai. Later it fought in Operation Horev. | כל היחידות בפרוזדור ירושלים ובהרי ירושלים; במחצית הראשונה של מאי 1948 כבשה את הפרוזדור ופתחה את הדרך עד שער הגיא. בהמשך לחמה במבצע חורב. | פרוזדור ירושלים, הרי ירושלים, שער הגיא: Izkor [3]. מבצע חורב: he.wiki "מבצע חורב" (pointer only), which names the Harel Brigade among Horev's forces [6] | "the Jerusalem corridor and hills" is spelled out as "בפרוזדור ירושלים ובהרי ירושלים" (Izkor's two terms) rather than a shorter "ובהריו", for clarity. "to Sha'ar HaGai" as "עד שער הגיא", as he.wiki "חטיבת הראל" phrases it [5]. |

### Negev (`negev`)

| Field | Current English | Proposed Hebrew | Source for each proper name | Notes |
|---|---|---|---|---|
| anchor `place` | Nir Am (Mekorot camp) | ניר עם (מחנה מקורות) | ניר עם: CBS 348 [1] (also `content/map/locations_1948.yaml`). מחנה מקורות: he.wiki "חטיבת הנגב" (pointer only): "מטה החטיבה במחנה מקורות שליד ניר עם" [7] | מחנה מקורות is unresolved, see section 3. |
| `first_commander` | Nahum Sarig | נחום שריג | Haganah lexicon, "Givati": "מפקד חטיבת הנגב נחום שריג" [2]; he.wiki "חטיבת הנגב" (pointer only): "למפקד החטיבה התמנה נחום שריג" [7] | The lexicon names him as the brigade's commander, not as its *first*; that rests on he.wiki [7] (and the English row's own sources). |
| `sector` | The Negev, in two sectors divided by the Beersheba-Gaza road; brigade headquarters at the Mekorot camp near Nir Am. | הנגב, בשתי גזרות שכביש באר שבע–עזה מפריד ביניהן; מטה החטיבה במחנה מקורות שליד ניר עם. | הנגב: Haganah lexicon ("חטיבת הנגב") [2]. באר שבע: CBS 9000 [1]. עזה: he.wiki pointer; `content/map/settlements_1948.yaml` already writes עזה. ניר עם: CBS 348 [1]. מחנה מקורות: he.wiki only [7] | "גזרות" is the military word for sectors; he.wiki "חטיבת הנגב" uses "גזרה" in the same sense. The en dash in "באר שבע–עזה" follows the English hyphen; a reviewer may prefer a plain hyphen. |

## 2. The other English values in the same lines (for the schema PR)

Status: `agent-checked`. No proper names here, only dates and numbers.

| Unit | Field | Current English | Proposed Hebrew | Read inside the template | Notes |
|---|---|---|---|---|---|
| givati | `formed` | December 1947 | דצמבר 1947 | הוקמה: דצמבר 1947 | The Haganah lexicon says "שהוקמה בנובמבר 1947" [2]. This note does not change the English fact; a person should check the month (section 3). |
| givati | `number` | 5th Brigade | חטיבה 5 | | Haganah lexicon: "חטיבה 5" [2]. |
| harel | `formed` | 16 April 1948 | 16 באפריל 1948 | הוקמה: 16 באפריל 1948 | Izkor gives 16.4.1948 [3]. |
| harel | anchor `since` | the first half of May 1948 | המחצית הראשונה של מאי 1948 | מיקום: שער הגיא, מאז המחצית הראשונה של מאי 1948 | |
| harel | `number` | 10th Brigade (Palmach) | חטיבה 10 (פלמ"ח) | | Izkor: "חטיבת הראל של הפלמ"ח, חטיבה 10" [3]. |
| negev | `formed` | March 1948 | מרץ 1948 | הוקמה: מרץ 1948 | The Academy of the Hebrew Language spells the month מרס; מרץ is the common spelling. The reviewer picks one for all cards. |
| negev | anchor `since` | March 1948 | מרץ 1948 | מיקום: ניר עם (מחנה מקורות), מאז מרץ 1948 | Same spelling choice as above. |
| negev | `number` | 12th Brigade (Palmach) | חטיבה 12 (פלמ"ח) | | Same pattern as Harel; no Hebrew source for "12" was read for this note (section 3). |

## 3. Unresolved, and what a person must check

1. **מחנה מקורות** (the Mekorot camp near Nir Am): the only source found is he.wiki "חטיבת הנגב" [7]. No Palmach,
   Haganah, IDF or Ministry of Defence page was found that names it. The Palmach heritage database page the repo
   already cites for the brigade names (`palmach.org.il/history/database/?itemId=5523`) returned a server error (HTTP
   500) on 19 October 2026, so it could not be read.
2. **מבצע חורב**: the spelling is certain enough to use, but the only page read for it is he.wiki [6]. A heritage-site
   page would be a better source.
3. **עזה**: he.wiki only; the CBS list holds Israeli localities only.
4. **"First" commander, Harel and Negev**: the non-wiki pages read say Rabin [4] and Sarig [2] commanded their
   brigades, not that each was the first; that rests on he.wiki [5] [7] (and the English rows' own sources).
5. **Givati's founding month**: the Haganah lexicon says November 1947 [2]; the row says December 1947. Not changed here.
6. **Negev's number "חטיבה 12"**: no Hebrew page read for it.
7. For every row: a native reader confirms the wording reads naturally inside the card templates, and the #564 review
   confirms each Hebrew text before it goes into `content/` with `*_review: agent-checked`.

## Sources (read 2026-10-19)

1. Central Bureau of Statistics locality list, as published on data.gov.il ("רשימת ישובים בישראל - מתעדכן"), queried
   through its API: `https://data.gov.il/api/3/action/datastore_search?resource_id=5c78e9fa-c2e2-4771-93ff-7f400a12f7ba&q=<name>`.
   Codes: גדרה 2550, גן יבנה 166, באר טוביה 155, ניר עם 348, באר שבע 9000.
2. Organisation of Haganah Heritage, lexicon, "גבעתי" (Brigade 5): https://hahagana.org.il/lexicon/lexiconpage/?itemId=47227
3. Ministry of Defence, Izkor, "סלע הנצחה לחטיבת פלמ"ח-הראל": https://www.izkor.gov.il/en_5b66232e91a0aeffce2c5c9082b5de0c
4. Ministry of Education, Pop portal, "יצחק רבין": https://pop.education.gov.il/tchumey_daat/moledet_hevra_ezrahut/yesodi/noseem_nilmadim/itzhak_rabin/
5. he.wikipedia "חטיבת הראל" (pointer only): https://he.wikipedia.org/wiki/%D7%97%D7%98%D7%99%D7%91%D7%AA_%D7%94%D7%A8%D7%90%D7%9C
6. he.wikipedia "מבצע חורב" (pointer only): https://he.wikipedia.org/wiki/%D7%9E%D7%91%D7%A6%D7%A2_%D7%97%D7%95%D7%A8%D7%91
7. he.wikipedia "חטיבת הנגב" (pointer only): https://he.wikipedia.org/wiki/%D7%97%D7%98%D7%99%D7%91%D7%AA_%D7%94%D7%A0%D7%92%D7%91
