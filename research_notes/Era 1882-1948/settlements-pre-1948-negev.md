# Jewish localities founded in the Negev before 15 May 1948 (research note, 2026-10-23)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`.
Refs #97, its Negev region. The dated settlements file and `tools/settlement_hexes.py` stay with
#61, and rows with a rename or an abandonment date need the owner's sensitivity review before they
enter the game; none is proposed here.

## How it was done

- Every page was fetched on 23 October 2026 with the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`. A search tool
  was used only to find pages; every page cited was then fetched and read directly.
- **Founding year and coordinates (official).** Central Bureau of Statistics, localities file 2024,
  https://www.cbs.gov.il/he/publications/doclib/2019/ishuvim/bycode2024.xlsx (checked 2026-10-23),
  columns `שנת ייסוד` and `קואורדינטות`. The twelve-digit coordinate is read as an Israel
  Transverse Mercator easting and northing of six digits each and converted with `pyproj`
  (EPSG:2039 to EPSG:4326), the same reading `settlements-1949-51.md` uses. That reading is the
  earlier note's, not stated in the file, so every lat/lon below is **computed here** from the CBS
  string rather than printed by CBS. Unlike the earlier note, this one reports no residuals against
  independent coordinates: none of the articles used here carries its own lat/lon, they all draw
  the CBS bot template, so there is no pair to validate the reading against.
- **Founding dates.** Haim Gvati, *100 Years of Settlement*, vol. 2, appendix, in the Ben-Yehuda
  Project edition, https://benyehuda.org/read/14684 (checked 2026-10-23). Columns: type of
  settlement, name, geographic region, date of founding, affiliation, origin of the name. As
  `settlements-1949-51.md` records, the column does not say whether the date is the day of going
  onto the land or a later recognition, and it sometimes differs from other sources by a day or
  more.
- **Hebrew Wikipedia** was read through the MediaWiki API as the wikitext of the current revision,
  cited by `oldid`. It is a lead and a cross-check, not an authority; facts resting on it alone are
  marked [S, Wikipedia].
- **The Palmach history** (palmach.org.il), **the Haganah heritage site** (hahagana.org.il), **the
  CET history** (lib.cet.ac.il), **the Council for Conservation of Heritage Sites** (shimur.org)
  and **Amudanan** (amudanan.co.il) were each fetched and read for the 1947 points, which are
  largely absent from CBS and from Gvati's appendix. Each citation gives its URL and check date.
- Hebrew dates were converted with the `convertdate` library. The Hebrew date is kept and the
  civil date is marked **computed here**. The method was checked against the four dates the
  articles convert themselves (Nir Am, Sa'ad, Gvim, Be'erot Yitzhak); all four reproduce exactly.

**Tags**, as in `settlements-1949-51.md`:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source.
  CBS is that primary source for a founding year; its coordinates are **computed here** (see above).
- **[S] single source**: one source, or several that plainly copy one another. A fact resting only
  on Hebrew Wikipedia is **[S, Wikipedia]**.
- **[C] conflicting**: sources disagree. Both values are given, each with its source, and
  **our reading** says which to use and why.
- **[GAP]** not found.
- **computed here** is arithmetic or a conversion done for this note, not a printed figure.
- **our reading** marks an interpretation, and is not attributed to the source.

Every page cited was opened and read, including each Wikipedia revision at the `oldid` given.

## What counts as the Negev here

The boundary is not clean and the sources do not agree on one, so this note states the rule it used
rather than implying there is a settled answer.

Included: every Jewish locality founded before 15 May 1948 that CBS places in the **Be'er Sheva
sub-district**, or that stood there and was later abandoned or moved; the **eleven points of
October 1946 as a set**; the other localities the Palmach history counts in pre-war Negev
settlement (six kibbutzim of 1941 to 1944, three mitzpim of 1943 and seven points of 1947),
https://www.palmach.org.il/history/database/?itemId=8464 (checked 2026-10-23); and **Brur Hayil**
(April 1948), which Gvati places in the western Negev.

Excluded, as belonging to a neighbouring region note rather than this one: Negba (CBS 1939), Gat
(CBS 1941), Nitzanim (CBS 1943) and Kfar Menachem (CBS 1937). All are in the Ashkelon sub-district
and in the southern coastal plain or the Judean foothills, and none is in the Palmach list.
**Our reading:** they belong with Lachish and the coastal plain, not the Negev.

Two of the eleven points sit awkwardly on that line and are kept in because the operation defines
the set, not geography: **Gal-On**, which Gvati's appendix puts in **Lachish**, and **Kedma**, which
went up from Kfar Menachem. Both are flagged in their rows.

## 1. The three mitzpim of 1943

Three mitzpim, research and experiment stations, were set up in the arid Negev in 1943: Gvulot,
Revivim and Beit Eshel. [V] The Palmach history lists the same three
(https://www.palmach.org.il/history/database/?itemId=8464, checked 2026-10-23), and the Gvulot and
Revivim articles both say "משלושת המצפים".

### Gvulot
- CBS: locality code **352**, `שנת ייסוד` **1943**, **31.21084, 34.46549**, Be'er Sheva
  sub-district. [V]
- Gvati, kibbutzim section: `גְּבוּלוֹת | הנגב הצפוני | 12.5.43 | הקיבוץ הארצי`. [S]
- he.wiki `גבולות`, oldid **43752363**: "עלה לקרקע ב-12 במאי 1943 כתחנת ניסיון חקלאית (מצפה) -
  הראשון משלושת המצפים בנגב". [S, Wikipedia] Gvati and he.wiki agree on **12 May 1943**, so the date is [V].
- **[C] within the same he.wiki article.** A later paragraph says "הקיבוץ הוקם בשנת 1946 על ידי חברי
  גרעין אלמגור". Both readings are in the article; the 1946 sentence is about the founding group of
  the present kibbutz, not the mitzpe. **Our reading:** 12 May 1943 is the date for the mitzpe and
  the one to use; the article does not reconcile the two itself.
- Relocation: "לאחר מלחמת העצמאות הועתק היישוב למקומו הנוכחי, כקילומטר צפונית מזרחית למצפה". [S, Wikipedia]
  No day given. [GAP]
- Departure base for Urim and Nirim in October 1946. [S, Wikipedia]

### Revivim
- CBS: code **354**, `שנת ייסוד` **1943**, **31.04397, 34.72178**, Be'er Sheva. [V]
- Gvati: `רְבִיבִים | נגב צפוני | 20.7.43 | הקיבוץ המאוחד`. [S]
- he.wiki `רביבים`, oldid **43556273**: "הקיבוץ עלה לקרקע ב-7 ביולי 1943, בשם תל צופים כאחד משלושת
  המצפים". [S, Wikipedia]
- **Rename: founded as תל צופים (Tel Tzofim).** [S, Wikipedia]
- **[C] on the day:** Gvati 20 July 1943, he.wiki 7 July 1943, and the founding scroll quoted in
  the article, dated "כ"ט במנחם-אב" 5703, a Monday, which is **30 August 1943 (computed here)**;
  the scroll's own "יום שני" matches that weekday. **Our reading:** no source here says which of
  the three marks the ascent; use July 1943, which two of the three give, and record all three
  days.
- Relocation: "לאחר המלחמה הקיבוץ עבר ממקומו המקורי אל האתר בו הוא נמצא היום. אבן הפינה של היישוב
  החדש הונחה ב-1 ביוני 1949". [S, Wikipedia]

### Beit Eshel
- **Not in the CBS localities file.** No row matches `בית אשל` in the 1,490 rows. [V, checked]
  **Our reading:** it is absent because it was not re-established on its site; the article's
  `סוג יישוב=יישוב לשעבר` and `יישובים יורשים=באר שבע` point the same way.
- **No row in Gvati's appendix** either. [V, checked]
- he.wiki `בית אשל`, oldid **42228944**. Infobox: `סוג יישוב=יישוב לשעבר`, `תאריך ייסוד=1943`,
  `סיבת נטישה=מלחמת העצמאות`, `תאריך נטישה=אוקטובר 1948`, `יישובים יורשים=באר שבע`. [S, Wikipedia]
  Its coordinates are written out in the article's map template, not drawn from the CBS bot
  template: `|מפה={{מפת מיקום|מדינה=באר שבע|שם=בית אשל|אורך=34.813272|רוחב=31.233472|...}}`, so
  **31.233472, 34.813272** is the article's own figure. [S, Wikipedia]
- Body: "היישוב עלה לקרקע באוגוסט 1943, כ-2 ק"מ מזרחית לבאר שבע". [S, Wikipedia] **Month only, no day.** [GAP]
- **[C] on the abandonment date.** The infobox says `תאריך נטישה = אוקטובר 1948`. The body says the
  place held out "עד כיבוש באר שבע על ידי צה"ל" in October 1948 and that the members decided only
  afterwards to move to the Jezreel valley and found Hayogev. Gvati's narrative agrees with the
  body: the defenders "החזיקו מעמד עד סוף המלחמה, ורק אחריה התיישבו בעמק במושב היוגב". **[C] on
  Hayogev's own date**, within Gvati: his narrative puts it in "חודש אב (יולי 1949)" and his
  appendix row reads `היוגב | עמק יזרעאל | 1.8.49`. Both are given. **Our reading:** the infobox date is the
  month Be'er Sheva was taken, not
  the month the site was given up, and the two are being conflated. Both values are given; neither
  is used as the abandonment date here.
- Unlike the other two mitzpim, its people wanted a moshav: "בניגוד לקיבוצים רביבים וגבולות, תושבי
  בית אשל שאפו להקים במקום מושב עובדים". [S, Wikipedia]

## 2. The six kibbutzim of 1941 to 1944

These are in the Ashkelon sub-district by CBS; the Palmach history counts them as the first stage
of Negev settlement. Be'erot Yitzhak has its own section below.

### Ruhama
- CBS: code **362**, `שנת ייסוד` **1944**, **31.49799, 34.70613**, Ashkelon. [V]
- Gvati: `רוּחָמָה | הנגב הצפוני | 19.3.44 | הקיבוץ הארצי`. [S]
- he.wiki `רוחמה`, oldid **43556283**: "היה זה היישוב היהודי הראשון בנגב בעת החדשה", of the 1911
  venture. [S, Wikipedia] **Rename:** "תחילה נקרא היישוב ג'ממה, כשם הכפר הערבי הסמוך", later changed to
  Ruhama. [S, Wikipedia]
- **Three abandonments before the present kibbutz**, all in the article: in the First World War
  ("היישוב ננטש ונבזז על ידי בדווים תושבי הסביבה"), in 1929, and a third time in the 1936 to 1939
  disturbances
  ("נעזבה רוחמה בשלישית; בתיה נהרסו ומטעיה נעקרו"). [S, Wikipedia] No day for any of the three. [GAP]
- **[C] on the founding of the present kibbutz.** The article gives both: "ב-1 בדצמבר 1943 עלו חברי
  השומר הצעיר, חידשו את הבאר והחלו להקים גדר ואת מבני הקיבוץ, טקס העלייה הרשמית לקרקע התבצע במרץ
  1944 ומהווה את ראשיתו של היישוב הנוכחי". [S, Wikipedia] So 1 December 1943 is the arrival and March 1944 the
  official ascent. CBS's 1944 and Gvati's 19 March 1944 both fall on the second. **Our reading:** Gvati's 19
  March 1944 is the best day-level value, and the
  December 1943 arrival is a distinct event rather than a competing date for the same one.
- The article also records that the neighbouring Arab village of al-Jammama, whose name Ruhama
  first carried, "ננטש במלחמת העצמאות". [S, Wikipedia] No day. [GAP]
- Departure base for Mishmar HaNegev and Shoval in October 1946. [S, Wikipedia]

### Dorot
- CBS: code **336**, `שנת ייסוד` **1941**, **31.50672, 34.64506**, Ashkelon. [V]
- Gvati: `דוֹרוֹת | דרום | 18.12.41 | האיחוד`. [S]
- he.wiki `דורות`, oldid **43555697**: "דורות היה הקיבוץ הראשון בנגב בהיווסדו בשנת 1941", and
  "היישוב הוקם בחנוכה, שנת ה'תש"ב (1941) על קרקעות שנרכשו מתושבי הכפר הסמוך הוג'". [S, Wikipedia]
- **Day: Gvati only (18 December 1941).** he.wiki gives the festival, not a civil day. Hanukkah
  5702 ran **15 to 22 December 1941 (computed here)** and Gvati's 18 December falls inside it, so
  the two sources agree: [S] for the exact day, which is Gvati's alone, and [V] for December 1941.

### Nir Am
- CBS: code **348**, `שנת ייסוד` **1943**, **31.51966, 34.57977**, Ashkelon. [V]
- Gvati: `נִיר-עָם | דרום | 12.1.43 | האיחוד`. [S]
- he.wiki `ניר עם`, oldid **44033578**: "הקיבוץ הוקם בט"ו בשבט ה'תש"ג (21 בינואר 1943)". [S, Wikipedia]
- **[C] on the day: Gvati 12 January 1943 against he.wiki 21 January 1943.** Both are January 1943.
  The he.wiki value is tied to a Hebrew date the article converts itself; Gvati's is a bare column
  value. Both are given.
- The article notes the Nir Am wells fed the 1947 water line to the eleven points: "קידוחים באזור
  ניר עם שימשו מקור לקו המים שהונח בשנת 1947 וסיפק מים ל-11 הנקודות בנגב". [S, Wikipedia]

### Gevaram
- CBS: code **342**, `שנת ייסוד` **1942**, **31.59149, 34.61218**, Ashkelon. [V]
- Gvati: `גְּבַרְעָם | דרום | 28.8.42 | הקיבוץ המאוחד`. [S]
- he.wiki `גברעם`, oldid **43729063**, image caption: "יום העלייה לקרקע - קיבוץ גברעם, 28 באוגוסט
  1942 (במועד זה עדיין לא היה שם לקיבוץ)". [S, Wikipedia] **Gvati and he.wiki agree to the day**, so
  28 August 1942 is [V].
- **Renames, and the locality had no name at founding.** The article says the founding core was
  called "מחר", that in early 1945 most members wanted "משואות" while the JNF names committee
  proposed "בית השקמה", and that גברעם was chosen in the end. [S, Wikipedia]
- Moved to its permanent site in 1945; in the war it was on the front line and its women and
  children were evacuated to Jaffa. [S, Wikipedia]

### Yad Mordechai
- CBS: code **358**, `שנת ייסוד` **1943**, **31.58896, 34.55842**, Ashkelon. [V]
- Gvati: `יַד מָרְדְכַי | דרום | 5.12.43 | הקיבוץ הארצי`. [S]
- he.wiki `יד מרדכי`, oldid **43009815**: "קיבוץ יד-מרדכי עלה להתיישבות בדצמבר 1943".
  [S, Wikipedia] Month only in the article; Gvati gives 5 December 1943. December 1943 is [V].
- **Rename with a date:** "באפריל 1946 הוחלף שם הקיבוץ מ'מצפה הים' ל'יד מרדכי'", after Mordechai
  Anielewicz. [S, Wikipedia]
- **Evacuated in the war and returned.** The article describes six days of attacks "החל מ-18 במאי
  ועד ל-23 במאי" and the decision to abandon; Gvati's narrative puts the evacuation at "אור ליום
  24.5". [C] Both are given. Soldiers reached the freed kibbutz on 5 November 1948 and the members
  returned on 6 November. [S, Wikipedia]

## 3. The eleven points, the night of 5 to 6 October 1946

he.wiki `11 הנקודות`, oldid **43974883**, opens: "11 הנקודות הן אחד-עשר יישובים יהודיים בנגב
הצפוני, אשר הוקמו במבצע התיישבות גדול במוצאי יום הכיפורים ה'תש"ז", on "הלילה שבין 5 ל-6 באוקטובר
1946", at the initiative of the Jewish Agency. [S, Wikipedia]

**A correction to my claim comment on #97**, which dated the operation 11 October 1946. The "11"
is the count of points **and a Hebrew date**, but not an October one: the operation went up on
**11 Tishrei 5707**, which is 6 October 1946 (computed here; Yom Kippur, 10 Tishrei, was
5 October). he.wiki `י"א בתשרי`, oldid **44002865**, records it under that day: "ה'תש"ז - 11
יישובים חדשים עולים על הקרקע בנגב, מהלך לילה אחד, במבצע שמכונה '11 הנקודות בנגב'". [S, Wikipedia]
The operation is also written "י"א הנקודות", with the Hebrew numeral, as the Mishmar HaNegev
article has it. **Our reading:** "11 October" is most likely 11 Tishrei read as an October day,
and that is how the error in my claim comment arose. No source read here gives 11 October: the article dates
it to the night of 5 to 6 October 1946, and
Gvati's appendix independently gives **6.10.46** for eight of the eleven. Nothing read for this note
supports 11 October.

The article's own list of eleven, with the site name it gives in brackets and the base each party
left from. [S, Wikipedia] In the table below, the CBS code, year and coordinate are [V] (CBS is the primary
source for those), and the Gvati date column is [S] on its own but [V] for the eight localities
where it agrees with the article.

| Locality | site name | departure base | CBS code | CBS year | CBS lat, lon | Gvati date |
|---|---|---|---|---|---|---|
| Urim | Grein | Gvulot | 403 | 1946 | 31.30419, 34.52263 | 6.10.46 |
| Be'eri | Nakhabir | Be'erot Yitzhak | 399 | 1946 | 31.42385, 34.49086 | 6.10.46 |
| Gal-On | Khirbet Musajat | Gat | 393 | 1946 | 31.63377, 34.84758 | 6.10.46 |
| Hatzerim | Qalta | Beit Eshel | 397 | 1946 | 31.24010, 34.71344 | 6.10.46 |
| Kfar Darom | Deir al-Balah | Be'erot Yitzhak | not in file | n/a | n/a | no row |
| Mishmar HaNegev | Bir Mansura | Ruhama | 395 | 1946 | 31.36351, 34.71701 | 6.10.46 |
| Nevatim | on Madsus land | Beit Eshel | 396 | 1946 | 31.22316, 34.88160 | 6.10.46 |
| Nirim | on Dangur land | Gvulot | 602 | **1949** | 31.33597, 34.39494 | 6.10.46 |
| Kedma | Tel Turmus | Kfar Menachem | 392 | **blank** | 31.70096, 34.77501 | no row |
| Shoval | Bir Zabala | Ruhama | 394 | 1946 | 31.41449, 34.74384 | 6.10.46 |
| Tkuma | Sumra | Be'erot Yitzhak | 665 | **1949** | 31.45092, 34.57933 | 30.5.49 |

The article also gives the mnemonic "קח משכנתא נג"ב" for the eleven names. [S, Wikipedia]

**The CBS coordinates above are the present localities.** Several of the eleven moved after the war,
so for those the CBS point is not the October 1946 point. Where an article gives the original site
explicitly it is recorded below; otherwise the original position is [GAP].

### The ones where CBS disagrees with 1946

- **Nirim.** CBS `שנת ייסוד` **1949**; Gvati and he.wiki **6 October 1946**. [C] he.wiki `נירים`,
  oldid **43556145**: "יצאו חברי נירים במוצאי יום הכיפורים תש"ז, 6 באוקטובר 1946, במסגרת עליית 11
  הנקודות", and "בגלל תקלה לוגיסטית, ההתיישבות לא הייתה במקום המתוכנן", the planned point being
  reached only months later, at **Dangur**. [S, Wikipedia] After the war, "בתחילת 1949 עבר הקיבוץ למקומו
  הנוכחי ובשטחו המקורי הוקם הקיבוץ ניר יצחק". [S, Wikipedia] **Our reading:** CBS's 1949 is the present site
  and the 1946 date is the Dangur point; they are not in conflict about the same thing. The battle
  of Nirim is dated "בוקר ה־15 במאי 1948" in the article. [S, Wikipedia]
  - **Where the first site was.** he.wiki places it "ליד קיבוץ סופה של היום" and says Nir Yitzhak
    was founded on Nirim's original ground; Gvati's narrative puts Nir Yitzhak at Dangur. These
    agree: Nir Yitzhak and Sufa are about 1.5 km apart (1.48 km, computed here from the two CBS
    points). CBS Nir Yitzhak is code **402**, `שנת ייסוד` 1949, **31.23768, 34.35765**; CBS Sufa is
    code **1238**, **31.23732, 34.34215**. [V] for the area; the exact 1946 coordinate is [GAP].
- **Tkuma.** CBS **1949**, Gvati **30.5.49**, he.wiki **the night of 5 to 6 October 1946**. [C], and
  he.wiki `תקומה`, oldid **42996459**, explains the mechanism: founded as a kibbutz at Sumra,
  "בשדות חוות יזרעם, מול קיבוץ בארי" [S, Wikipedia], then
  "בתאריך 30 במאי 1949 ... עברו תושבי תקומה אל אזור אדמות הכפר הערבי אל-מוחרקה, שתושביו גורשו על ידי
  כוחות של חטיבת הנגב", whose residents, the article says, were **expelled** by forces of the
  Negev Brigade. [S, Wikipedia] **Rename: founded as סומרה**, renamed Tkuma within days. [S, Wikipedia] It also
  changed type, founded as a kibbutz and now a moshav, which is why Gvati's row is in the moshavim
  section. **Our reading:** 1946 is the founding and 1949 the move; they are two events, not two readings
    of one.
  - The 1949 move is onto the land of an Arab village whose residents the article says were expelled. That is
    recorded here as the source states it and nothing is proposed about it; it is the owner's
    sensitivity review.
- **Kedma.** CBS `שנת ייסוד` is **blank**, and the CBS row is the present youth village, not the
  kibbutz. Gvati's appendix has **no row**; his narrative mentions "קדמה, גלאון, דברת שעלו בשנת
  1946". [S] he.wiki `קדמה`, oldid **43293211**: "קֶדְמָה התקיימה כקיבוץ בין השנים 1946–1962", and
  "מאז 1979 נמצא במקום כפר נוער טיפולי". [S, Wikipedia]
  - The year range quoted above is printed in the article with an en dash, which is kept because
    it is inside a quotation.
  - **An impossible date in the source.** The article's founding sentence reads "במוצאי יום כיפור,
    10.6.1946 יצאו חברי קדמה מכפר מנחם". [S, Wikipedia] Motza'ei Yom Kippur 5707 and 10 June 1946 cannot both
    be right, and the `11 הנקודות` article dates the operation to 5 to 6 October 1946. The numeral
    is reported as written. **Our reading:** the day-month digits are transposed or wrong and the
    intended date is the October ascent, but no page read here says so, so no corrected date is
    given.
  - End: "במאי 1962 עזבו אחרוני קדמה את המקום". [S, Wikipedia] The article carries
    `קטגוריה:יישובים ישראליים שננטשו`.

### The rest of the eleven

- **Urim.** he.wiki `אורים`, oldid **44069164**: "קבוצת אורים הוקמה במוצאי יום כיפור ה'תש"ז, 1946".
  [S, Wikipedia] **Three sites, two with explicit coordinates in the article's own footnotes:** first at Grein
  near Gvulot, `31.185812, 34.485823`, where a marker stone now stands; then "לנקודה הסמוכה לצאלים",
  `31.184178, 34.499766`, where a half-ruined security house stands; then "בשנת 1948 עברו למקום הקבע
  במשטרת עימארה". [S, Wikipedia] The CBS point **31.30419, 34.52263** is the third.
- **Be'eri.** he.wiki `בארי (קיבוץ)`, oldid **44086869**: "בארי הוקם במוצאי יום כיפור ה'תש"ז, 1946,
  במסגרת 11 הנקודות". [S, Wikipedia] Year only in the article; Gvati gives 6.10.46. Relocation: "לאחר קום
  המדינה הועתק הקיבוץ שלושה קילומטר דרום־מזרחה". [S, Wikipedia] No day. [GAP]
- **Gal-On.** he.wiki `גלאון`, oldid **43897874**: "במוצאי יום הכיפורים ה'תש"ז, 5-6 באוקטובר 1946,
  עלה על הקרקע ליד קיבוץ גת, במסגרת 11 הנקודות בנגב". [S, Wikipedia] This is the one article that prints the
  two-day form. Gvati puts it in **Lachish**, not the Negev. Not evacuated: it repelled an Egyptian
  attack "ב-14 ביולי 1948". [S, Wikipedia]
- **Hatzerim.** he.wiki `חצרים`, oldid **43555845**: "הקיבוץ הוקם במוצאי יום כיפור בשנת 1946 על
  אדמות קלטה כחלק מ־11 הנקודות". [S, Wikipedia] No move recorded.
- **Mishmar HaNegev.** he.wiki `משמר הנגב`, oldid **44065142**: "שהוקם כחלק ממבצע י"א הנקודות
  במוצאי יום כיפור תש"ז, 6 באוקטובר 1946", and "לאחר שנה הועתק הקיבוץ למקומו הנוכחי על גבעה שכנה
  כ-500 מטר צפונה", the first site now called "הגבעה הראשונה". [S, Wikipedia]
- **Nevatim.** he.wiki `נבטים (מושב)`, oldid **44032217**: "מושב נבטים נוסד ב-6 באוקטובר 1946,
  במוצאי יום כיפור, במבצע 11 הנקודות על ידי עולים יוצאי עיראק". [S, Wikipedia] Moved "כ-2 קילומטר" to its
  permanent site; no date for the move. [GAP] Note the bare title `נבטים` is the article on the
  Nabataeans, not the locality.
- **Shoval.** he.wiki `שובל (קיבוץ)`, oldid **44069091**: "נוסד ב-6 באוקטובר 1946", and "עלה לקרקע
  באדמות קרן קיימת לישראל ב'ביר זבאלה' במוצאי יום כיפור, 6 באוקטובר 1946". [S, Wikipedia] Gvati gives the
  same day, so the date is [V]. **Intended name:**
  the founders wanted "אילת"; the settling institutions chose Shoval. [S, Wikipedia]
- **Kfar Darom.** **Not in the CBS file and no Gvati appendix row.** [V, checked] he.wiki
  `כפר דרום`, oldid **42998094**: "במוצאי יום הכיפורים תש"ז -1946 יושב המקום, על ידי גרעין של הקיבוץ
  הדתי", and `תאריך נטישה = 18 באוגוסט 2005`. Its coordinates are in a map template,
  `| מפה = {{מיקום מפורט בישראל|שם=כפר דרום|רוחב=31.401389|אורך=34.36|...}}`, giving **31.401389,
  34.36**; the longitude carries only two decimals, so it is good to about 500 m. [S, Wikipedia] **[C] on the
  first Egyptian attack.** he.wiki: "ב-13 במאי 1948, יום לפני הכרזת העצמאות",
  calling it the first locality the Egyptian army attacked. Gvati's narrative gives "ב־15.5 תקפו
  יחידות צבא מצרים את כפר־דרום", and adds that it had already faced irregulars of the Muslim
  Brotherhood before that. Both dates are given; the two may be describing the regular army's
  first attack and an earlier one. Evacuated on the night of **8 July 1948**. [S, Wikipedia]
  - **[C] on the hour of the fall.** Gvati: the defenders left at night "ולמחרת נכבש הישוב
    על־ידי המצרים". he.wiki: "בבוקר 8 ביולי החל הצבא המצרי בהתקפה על היישוב ... וגילה כי היישוב
    ריק", that is the same morning. Both given.
  - Gvati lists it among the localities permanently lost in the war, and its people founded **Bnei
    Darom**: his appendix row reads `בְּנֵי-דָרוֹם | דרום | 23.3.49 | הפועל המזרחי | נוסד בידי חברי
    כפר-דרום שננטש במלחמת השחרור ונקרא על שמו`. [S]
  - The 1970 re-establishment and the 2005 evacuation are outside this note's window and are
    recorded only so the sequence is not mistaken for a continuous settlement.

## 4. 1947 and 1948

### Sa'ad
- CBS: code **419**, `שנת ייסוד` **1947**, **31.47068, 34.53445**, Be'er Sheva. [V]
- Gvati: `סַעַד | נגב מערבי | 30.6.47 | הפועל המזרחי`. [S]
- he.wiki `סעד`, oldid **42997897**: "היישוב עלה על הקרקע בי"ב בתמוז ה'תש"ז (30 ביוני 1947), במלאת
  שנה ל'שבת השחורה'". [S, Wikipedia] **All three agree, and Gvati and he.wiki agree to the day.**
- **Rename:** "השם שנבחר לקיבוץ על ידי מייסדיו היה 'עלומים', אולם בהחלטת המוסדות המיישבים שונה השם
  ל'סעד'". [S, Wikipedia]
- Relocation after the war: "העבירו חברי הקיבוץ את יישובם כקילומטר מזרחה". [S, Wikipedia] No day. [GAP]

### Tze'elim
- CBS: code **413**, `שנת ייסוד` **1947**, **31.20370, 34.53417**, Be'er Sheva. [V]
- Gvati: `צֶאֱלִים | הנגב המערבי | 7.2.47 | האיחוד`. [S]
- he.wiki `צאלים`, oldid **43801220**: "הקיבוץ נוסד בינואר 1947". [S, Wikipedia]
- **[C]: Gvati 7 February 1947 against he.wiki January 1947.** The Haganah heritage page
  independently gives 7 February 1947 for "מבטחים, שורשים ועלומים"
  (https://hahagana.org.il/sites/sites/sitepage/?itemId=48443, checked 2026-10-23), and שורשים is
  this kibbutz under its first name. **Our reading:** 7 February 1947. That also explains why
  Mivtahim shares the date: three points went up that night, not one recorded twice.
- **Rename:** "שם הקיבוץ בנגב היה בהתחלה 'שרשים'. לאחר זמן קצר נאלצו לשנות את השם לצאלים".
  [S, Wikipedia] The Haganah page's "שורשים" for the 7 February point is the same name, so the
  rename itself is [V].
- **A full change of population:** "לאחר המלחמה עברו במרץ 1949 כל
  חברי צאלים אל הקיבוץ כפר החורש שליד נצרת", and a group of Holocaust survivors from eastern Europe
  took their place. [S, Wikipedia]

### Gvim
- CBS: code **424**, `שנת ייסוד` **1947**, **31.50689, 34.59654**, Ashkelon. [V]
- Gvati: `גֵבִים | דרום | 28.8.47 | האיחוד`. [S]
- he.wiki `גבים`, oldid **44057295**: "קיבוץ גבים הוקם בי"ב אלול תש"ז, 28 באוגוסט 1947". [S, Wikipedia]
  **Gvati and he.wiki agree to the day.**
- **Rename:** "שמו המקורי של הקיבוץ היה שדה עקיבא ... לאחר זמן מה שונה שמו לגבים".
  [S, Wikipedia] The Haganah page independently calls it "קיבוץ גבים (אז שדה-עקיבא)", so the
  rename is [V].
- **An error in a source, reported rather than dropped.** he.wiki `בתי הביטחון בנגב הצפוני`, oldid
  **42567087**, gives Gvim's coordinate as `31.204928, 34.455571`. That is the same pair it gives
  for Gvulot and lies 36.07 km from the CBS Gvim point (computed here, WGS84 geodesic). It does not match the
  CBS point and the two cannot both be Gvim.

### Mivtahim, the 1947 kibbutz
- he.wiki `מבטחים (מושב)`, oldid **42987731**: "היישוב מבטחים הוקם לראשונה ב-7 בפברואר 1947 כקיבוץ
  של העובד הציוני". [S, Wikipedia] The Haganah page gives the same 7 February 1947 for
  "מבטחים", so the founding date is [V]. Abandoned: "עם תום מלחמת העצמאות החליטו חברי מבטחים לפנות את המקום" and
  joined the founders of the new Nitzanim. [S, Wikipedia] No day for the evacuation. [GAP]
- **The present moshav is not the same founding.** CBS code **829**, `שנת ייסוד` **1950**, **31.24040,
  34.40787**; Gvati, moshavim section, `מִבְטָחִים | הנגב המערבי | 17.12.50`; he.wiki "המושב מבטחים
  הוקם מחדש ב-1950 על ידי עולים מכורדיסטן ליד היישוב הישן הנטוש". The 1950 year is [V], CBS,
  Gvati and he.wiki agreeing; the detail that the settlers came from Kurdistan is
  [S, Wikipedia]. Only the 1947 kibbutz is in
  this note's window.

### Brur Hayil
- CBS: code **428**, `שנת ייסוד` **1948**, **31.55787, 34.64673**, Ashkelon. [V]
- Gvati: `בְרוֹר חַיִל | הנגב המערבי | 18.4.48 | האיחוד`. [S] His narrative adds it was built
  "בשיטה של חומה ומגדל" beside the Arab village of Burayr.
- he.wiki `ברור חיל`, oldid **43890748**: "באפריל 1948 הוקם משלט סמוך לכפר ברייר בנגב המערבי על ידי
  הפלמ"ח ותושבים מהאזור", and it was "אחד משני היישובים היחידים שהוקמו בין החלטת החלוקה להכרזת
  העצמאות". [S, Wikipedia] **Our reading, computed here from this note's own dates:** it is the
  only locality in this note founded between the Partition Resolution and 15 May 1948.
- Relocation: "בינואר 1950 עברו החברים ממיקומו הראשוני של היישוב לנקודה החדשה". [S, Wikipedia]

### Ramat HaNegev (Ofakim)
Not in the CBS file and no Gvati appendix row: it stood south of Be'er Sheva and was given up, like
Beit Eshel. [V, checked]
- Palmach history, https://palmach.org.il/history/database/?itemId=7973 (checked 2026-10-23):
  "קיבוץ-משלט. נוסד בנובמבר 1947 נוסד ב- 19 בנובמבר 1947 בידי 40 חברים", from the
  "הכשרת משמרות - מסדה ('אופקים')" training group. [S] Note that on this page **"אופקים" names the
  training group, not the locality**. The Haganah heritage page uses it as the locality's name,
  calling the last two points "חלוצה ואופקים"
  (https://hahagana.org.il/sites/sites/sitepage/?itemId=48443, checked 2026-10-23). [S] Both
  readings are recorded; which name a row should carry is not proposed here.
- End: "במארס 1949 התפרק היישוב וחלק מעוזביו הצטרפו לקבוצת משמרות". [S] The CET history agrees it
  was given up: "רמת הנגב (ממזרח לצאלים) נעזבה על-ידי תושביה"
  (https://www.lib.cet.ac.il/pages/item.asp?item=13006&source=647, checked 2026-10-23). Two sources
  agree it was abandoned, so that is [V]; the March 1949 month is [S].
- Location: "ממזרח לצאלים", east of Tze'elim, per CET. Coordinate: [GAP].

### Halutza
Not in the CBS file under this name and no Gvati appendix row. [V, checked] It moved east in 1949
and became Mashabim, later Mashabei Sadeh.
- Founding: the Council for Conservation of Heritage Sites sign text,
  https://shimur.org/signs/קיבוץ-חלוצה/ (checked 2026-10-23): "בלילה שבין 19-18 בנובמבר 1947 עלתה
  כאן להתיישבות חבורה מתוך הכשרת 'הנוער העובד' בהרצליה", ten days before the partition vote. [S]
  Amudanan gives "ב-19.11.1947" for the same event,
  https://amudanan.co.il/w/P414031 (checked 2026-10-23). [S] The two agree, the night spanning both
  days, so **18 to 19 November 1947** is [V].
- Original site: **31.11656, 34.65272**, per Amudanan. [S] It is not a CBS point.
- The move: the Palmach page dates the enlarged group's move to the new point at Mashabim to
  "ב- 19 בנובמבר 1949", https://palmach.org.il/history/database/?itemId=7968 (checked 2026-10-23).
  [S]

### Alumim at Hazla
This is the locality that section 6 of the earlier draft of this note could not account for. It was
a separate point, not Sa'ad under another name.
- The Haganah heritage page gives three points going up on 7 February 1947: "ב-7 בפברואר 47' חוזקה
  שרשת הישובים בנגב הצפוני עם עלייתם לקרקע של המושבים מבטחים, שורשים ועלומים"
  (https://hahagana.org.il/sites/sites/sitepage/?itemId=48443, checked 2026-10-23). [S] שורשים is
  Tze'elim under its first name, so the three are Mivtahim, Tze'elim and Alumim.
- Amudanan, "עלומים הישנה (חזע'לה)", https://amudanan.co.il/w/P645633 (checked 2026-10-23):
  "ניסיון התיישבות שנכשל בראשית 1947 (פברואר)", at **31.14698, 34.71052**, settled by a group from
  the Alumim kibbutz "(והיום הוא הקיבוץ הדתי סעד בנגב המערבי)", and "לאחר זמן קצר ניטש המקום". [S]
  Two independent sources give February 1947, so the founding month is [V].
- **[C] on when it was abandoned.** Amudanan says soon after it went up; the CET history lists it
  among the post-war departures, "קבוצת עלומים בחזלה (מדרום לבאר-שבע) עזבה את המקום ומקצת מחבריה
  הצטרפו לקיבוץ סעד". Both are given; neither is used as the date.
- **This resolves the gap the earlier draft recorded.** The `11 הנקודות` article was right that a
  third February 1947 point called Alumim existed. The `צאלים` and `סעד` articles are describing
  the *group's* name, which is a different thing from this point. The 1966 kibbutz Alumim is a
  third, later use of the name.

## 5. Be'erot Yitzhak, the original Negev site

The CBS row is the post-war locality in the centre of the country, so the Negev site is recorded
separately here.

- CBS: code **450**, `שנת ייסוד` **1948**, **32.04080, 34.91041**, **Petah Tikva sub-district**. [V]
  That is not the Negev site.
- **No Gvati appendix row** for either site. [V, checked] His narrative covers the attack: the
  Egyptians tried to take it "ב-15.7" and "המשק ההרוס נשאר בידי כוחותינו", and its members later
  moved to Wilhelma. [S]
- he.wiki `בארות יצחק`, oldid **43784200**: "בכ"ב בשבט תש"ג (28 בינואר 1943) עלתה קבוצת חלוץ של 12
  חברים והתחילה לבנות את הקיבוץ, כחמישה קילומטר מדרום-מזרח לעזה", with an explicit coordinate
  footnote for the first site, `31.46528, 34.50703` (given as 31 27 54.99 N, 34 30 25.3 E;
  converted to decimal here). [S, Wikipedia]
- **Rename:** the group was "רמת השומרון" at Pardes Hanna before the name Be'erot Yitzhak.
  [S, Wikipedia]
- Destroyed "בח' בתמוז תש"ח (15 ביולי 1948)", with 17 defenders killed and the farm "נהרס לחלוטין",
  and the decision was "שלא לחזור" to the site. [S, Wikipedia] The members moved to Wilhelma and "בשנת 1952
  עברו חברי הקיבוץ צפונה מווילהלמה והקימו את בארות יצחק החדשה". [S, Wikipedia]
- **[C] on the re-establishment year: CBS 1948 against he.wiki 1952.** Both refer to the centre of
  the country, not the Negev. **Our reading:** use **1952** for the present locality. The article
  describes a staged move, temporary residence at Wilhelma first and the move north to the present
  site in 1952, and CBS records one founding year per locality, which here most likely marks the
  arrival at Wilhelma rather than the present site. CBS is the primary source for a founding year,
  so this is the one place in the note where it is not followed, and the reason is given.
- It was the departure base for three of the eleven points, Kfar Darom, Be'eri and Tkuma. [S, Wikipedia]

## 6. Checked and excluded

- **Alumim.** CBS code **1146**, `שנת ייסוד` **1966**; Gvati `עֲלוּמִים | הנגב המערבי | 1966`;
  he.wiki `עלומים (קיבוץ)`, oldid **43556197**, "המתיישבים הראשונים עלו לקרקע באלול תשכ"ו, ספטמבר
  1966". [V] All three agree on 1966. **It does not belong in this window.** It stands near Be'erot
  Yitzhak's original site, 1.56 km from it (computed here); the `11 הנקודות` article puts Be'erot
  Yitzhak "ליד עלומים של היום" and the Alumim article puts the old water tower "בשדות שבסמוך
  לקיבוץ", so the sources say near, not on. [S, Wikipedia]
- **The 1966 Alumim is a different place from the 1947 Alumim at Hazla**, which is in section 4.
  The name is used three times over: the training group that became Sa'ad, the failed 1947 point at
  Hazla, and this kibbutz of 1966.
- **Be'er Sheva.** CBS code **9000**, `שנת ייסוד` **1948**, Be'er Sheva sub-district,
  **31.25382, 34.78497** (computed here). [V] It is the only CBS row in the Be'er Sheva
  sub-district with a founding year of 1948 or earlier that this note does not cover, and the
  inclusion rule above would otherwise catch it. **Our reading:** CBS's 1948 marks the city's
  re-establishment as an Israeli locality after it was taken in October 1948, which is after this
  note's window and is a different kind of event from a settlement going up on the land. It is
  recorded here so its absence is deliberate rather than an oversight. Beit Eshel stood about
  2 km east of it.
- **Sufa.** he.wiki `כרונולוגיית ההתיישבות היהודית בארץ ישראל בעת החדשה`, oldid **43572242**, lists Sufa under
1947. The article
  `סופה (קיבוץ)`, oldid **43488608**, says it began as a Nahal outpost "בשנת 1974", and Gvati's
  appendix gives `סוּפָה | פתחת רפיח | 1975`. [C] on the year, 1974 against 1975, but both are far
  outside this window. **Our reading:** the chronology's 1947 entry is an error; Sufa is not
  added.

## 7. Gaps

- The October 1946 position of Be'eri, Hatzerim, Shoval, Mishmar HaNegev and Nevatim, beyond the
  direction and rough distance the articles give. [GAP] The localities with an explicit original
  coordinate in a source read here are Urim (two sites), Beit Eshel, Kfar Darom, Be'erot Yitzhak,
  Halutza and Alumim at Hazla. Nirim is **not** among them: its article places the first site near
  today's Sufa but gives no point, so its 1946 coordinate is [GAP], as section 3 says.
- A day for several of the moves: Gvulot, Be'eri, Nevatim, Sa'ad, Mivtahim's evacuation. [GAP]
- Beit Eshel's founding day within August 1943. [GAP]
- A coordinate for Ramat HaNegev. [GAP] CET places it east of Tze'elim; no source read here gives
  a point.
- The day within March 1949 that Ramat HaNegev disbanded, and the day Alumim at Hazla was given up.
  [GAP]
- Coordinates were taken from CBS, from Amudanan, and from coordinates written into the articles
  themselves. None was measured off a map, and no hex was computed here, since
  `tools/settlement_hexes.py` stays with #61.

## 8. Sources not reached, and what was not read

- **The Knesset's HTML sites** are behind a bot wall: `main.knesset.gov.il` and
  `www.knesset.gov.il` return HTTP 247 and a JavaScript challenge. **Its file server and OData
  service are not blocked**: `fs.knesset.gov.il` serves Divrei HaKnesset PDFs with real text
  layers and `knesset.gov.il/Odata/ParliamentInfo.svc` answers, both checked 2026-10-23, and that
  is the route `events-1952-56.md` uses. Nothing in this note rests on either, but the earlier
  blanket statement that the whole domain is walled was wrong and is corrected here.
- **No contemporaneous newspaper was read.** Several facts cited above rest on newspaper footnotes
  inside the Wikipedia articles (על המשמר, דבר, הצופה, מעריב), and those scans were not opened. A
  fact of that kind is [S, Wikipedia] here even where the article footnotes a newspaper, because
  the newspaper itself was not read.
- **No archive material, no Gvati volume in print and no scholarship** was opened. Gvati is read
  only in the Ben-Yehuda Project transcription of the appendix and its narrative chapters.
- **No map was measured.** Every coordinate comes from CBS or from a figure written in a source;
  none was taken off a map, and no hex was computed, since `tools/settlement_hexes.py` stays
  with #61.
