# Source check of `displacement-cards.md` (#801, note merged in #1332)

**Agent read, 26 October 2026. A person's check is still pending on every row.** This note re-opens every source that
[`displacement-cards.md`](displacement-cards.md) links and checks each sentence that note quotes against what the
source prints. It changes no `checked:` date, `status` or `reviewed_by`, and it edits neither that note nor `content/`.
It decides no owner question; the reads are an agent's and use the tag `[V, agent read 26 Oct; a person's check
pending]` (CONTENT_POLICY §2 item 6). Wikipedia pages are `[S]`, read by `oldid`.

## How the sources were read

- he.wiki: the MediaWiki API (`action=parse&oldid=<id>`), HTML turned to plain text, on 26 October 2026. The ten
  revision ids in the checked note were each returned with the same `revid`, so the permanent links are the pages read.
  Footnotes were read from the same revision's reference list.
- Jewish Virtual Library pages (IDF accounts, each ending "Sources: Israel Defense Forces"), the IDF and Defence
  Establishment Archives page and the Knesset PDF link: fetched with Python `urllib` and a browser User-Agent on
  26 October 2026; HTTP 200 for all four. Correction 2026-10-27: not every page read returned 200; the Ben-Ami page in A6 (`jewishvirtuallibrary.org/jsource/History/Ben-Ami.html`) returned HTTP 410.
- Method: each quoted sentence was searched as a whole in the page text, first after collapsing only whitespace
  (so dashes, the maqaf "־", gershayim and quotation marks had to agree as printed), and every one was found. Rows below give
  the printed text so a person can repeat the search on the permanent link.
- Not read: the books behind the he.wiki footnotes (Morris, Gelber, Segev, Regev, Abbasi). The footnote rows record
  what each he.wiki reference list prints, not what the book says.
- Owner quotes from GitHub (#783, #793, #1219, #1327) were read through `gh api` on 26 October 2026.

## Result in one paragraph

All 28 quoted source sentences (Hebrew and English, listed in the tables) match the printed text word for word. The footnote rows match the
reference lists. Four wording points are for the note's author or the checking person (rows M1 to M4 below); none
changes a quote. The Knesset PDF still cannot be read from this box, so nothing is checked against it.
Corrected 2026-10-27: all 28 sentences match for the quoted fragments only, and there are five wording points (rows M1 to M5), not four; M5 records that the Lydda dispute passage on the page is longer than the note describes. A review agent's check (agent read 2026-10-27; a person's check pending): the Knesset PDF does load from outside this box (9 pages), and its page 8 row for Ben-Ami reads "שחרור הגליל המערבי" with no Acre, which agrees with O7 below. From this box the Knesset URL answered HTTP 303 with a 48-byte body on 2026-10-27 (not the PDF, and not the HTML page K1 describes), so that PDF check is not ours. Date corrected 2026-10-27: the review agent's PDF check was done on 26 Oct 2026, not 27 Oct ("agent read 2026-10-27" above should read "agent read 2026-10-26").

## 1. Acre

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| A1 | HW-ACRE, the surrender sentence behind Line A | he.wiki [המצור על עכו (1948)](https://he.wikipedia.org/w/index.php?oldid=42972755) | "ב־18 במאי לפנות בוקר חתמו חברי מועצת העיר על כתב כניעה, הורו על הפסקת הלחימה ופתחו את שערי העיר העתיקה." | match. The sentence has no year; 1948 comes from the article title. | [S] | 26 Oct 2026 |
| A2 | Line B | same | "בימים הראשונים לאחר הכיבוש, נמשכה יציאת התושבים ללבנון ולגליל המרכזי, אולם לא הוצאו פקודות גירוש ולא הופעל לחץ על התושבים לעזוב." | match | [S] | 26 Oct 2026 |
| A3 | context: typhus | same | "ב-5 במאי פרצה בעיר מגפת טיפוס, שהייתה גורם נוסף לבריחת התושבים מן העיר." | match | [S] | 26 Oct 2026 |
| A4 | context: who remained | same | "בעיר נשארו 5,000 עד 6,000 איש, רובם תושבים מקומיים." | match. The "[C]" in the note (Morris: many not local) is not in any linked page; it is in #793 ("about 5,000, many not local; no expulsion order, Morris"), where it matches. | [S] | 26 Oct 2026 |
| A5 | note on the wider area | he.wiki [מבצע בן עמי](https://he.wikipedia.org/w/index.php?oldid=42972754) | "חטיבת כרמלי לא הצטוותה לגרש את האוכלוסייה הערבית האזרחית מן האזור, אך בעקבות המבצע נמלטו ממנו כמעט כל תושביו הערבים." | match | [S] | 26 Oct 2026 |
| A6 | Line A (official): none found; JVL has no Ben-Ami page | `jewishvirtuallibrary.org/jsource/History/Ben-Ami.html` | HTTP 410 (Gone) | match: the page is still gone | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| A7 | footnote under the surrender sentence (table in "Sources") | HW-ACRE reference list | the sentence is followed by two references: Mustafa Abbasi, "נפילת עכו במלחמת העצמאות" (palestine-studies.org, in English), and "כיבוש עכו - 16-17/5/48" (Haganah heritage site) | match | [S] | 26 Oct 2026 |
| A8 | footnote under Line B | HW-ACRE reference list | "בני מוריס, לידתה של בעיית הפליטים הפלסטינים 1947–1949, עמ' 153." | match (the paragraph's one footnote, at its end) | [S] | 26 Oct 2026 |

## 2. Nazareth

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| N1 | Line A: "Nazareth surrendered on 16 July 1948, in Operation Dekel", on "the eve of 16 July" | JVL, ["Operation Dekel (July 1948)"](https://jewishvirtuallibrary.org/jsource/History/Dekel.html), IDF account | "The city surrendered on the eve of 16 July." The page ends "Sources: Israel Defense Forces". | match for the sentence. The "[C]" in the note (evening of 15 or 16 July) stands: the page does not say which. | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| N2 | the forces set out on the night of 15-16 July | he.wiki [מבצע דקל](https://he.wikipedia.org/w/index.php?oldid=42975037), section "כיבוש נצרת" | "בליל 15–16 ביולי 1948 יצאו כוחות חטיבה 7 וגדוד 21 משפרעם לעבר נצרת." | match | [S] | 26 Oct 2026 |
| N3 | surrender at 22:00, after an entry at 17:40 (described in the note's [C]) | same section | "בשעה 17:40 נכנסו כוחות צה"ל לנצרת ללא קרב ותפסו את בית המשטרה." and "בשעה 22:00 נחתם הסכם הכניעה בין נציגי התושבים לנציגי צה"ל." | match; the 17:40 sentence is not quoted in the note, only the hour | [S] | 26 Oct 2026 |
| N4 | the order behind it | same | "בנוסף, דוד בן-גוריון הורה במפורש להניח לתושבים להישאר." | match | [S] | 26 Oct 2026 |
| N5 | Line B | he.wiki [נצרת](https://he.wikipedia.org/w/index.php?oldid=44022843) | "למרות הקרבות שניטשו בעיר ובסביבתה בעת כיבושה, נשארו תושבי העיר במקומותיהם." | match | [S] | 26 Oct 2026 |
| N6 | footnote under Line B | HW-NAZ reference list | "בני מוריס, לידתה של בעיית הפליטים הפלסטינים 1947–1949, עמוד 271." | match | [S] | 26 Oct 2026 |
| N7 | surrender sentence carries no footnote | HW-DEKEL | the paragraph holding the 22:00 sentence has no reference | match | [S] | 26 Oct 2026 |

## 3. Lydda

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| L1 | Line A, the Archives sentence | IDF and Defence Establishment Archives, [Yitzhak Rabin exhibition](https://archives.mod.gov.il/sites/English/Exhibitions/Pages/Yitzhak-Rabin.aspx) | "In July 1948, with the renewal of warfare following the first truce, Rabin was appointed operations officer in Operation Danny, capturing the cities of Lydda and Ramle, and in September 1948 operations officer in operations "Yoav," ..." | match; the note's quote stops at "Ramle," and marks the cut with "..." | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| L2 | Line A, the IDF Danny account | JVL, ["Operation Danny (July 9-18, 1948)"](https://jewishvirtuallibrary.org/jsource/History/Danny.html), IDF account | "The first phase was to occupy the areas of Lod and Ramle;" The page gives no day for Lydda (it says "within a day and a half" of the advance, without a date). Added 2026-10-27: that phrase is about the airport and a dozen villages, not about Lydda itself ("Thus, within a day and a half, the largest airport in the Middle East and a dozen key villages had fallen to the IDF"). | match; "no day for Lydda" also confirmed | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| L3 | the day | he.wiki [מבצע דני](https://he.wikipedia.org/w/index.php?oldid=43258522) | "בלילה שבין 12 ל-13 ביולי חמקו הלוחמים שבבניין המשטרה והושלם כיבוש לוד." The same article has the entry attempt on 11 July: "ב-11 ביולי 1948 ניסו שתי פלוגות גדוד הפלמ"ח השלישי ... לפרוץ ללוד ממזרח אך נבלמו". | match; the [C] with 11 July stands | [S] | 26 Oct 2026 |
| L4 | Line B | he.wiki [לוד](https://he.wikipedia.org/w/index.php?oldid=43942324) | "לפני הלחימה, במהלכה ואחריה גורשו באיומים כמעט כל התושבים הערבים מהעיר, לפי פקודה שנתן קצין המבצעים של מבצע דני, יצחק רבין:" then a quoted order | match | [S] | 26 Oct 2026 |
| L5 | context: those who stayed | HW-DANI | "בעיר נותרו רק כמה מאות תושבים, רובם נוצרים, שבחרו להישאר." | match as printed (see M1: the sentence does not name the city) | [S] | 26 Oct 2026 |
| L6 | the historians' dispute, Kadish and Sela | HW-LOD | "רק לאחר תקריות הירי ב־12 ביולי "הוחלט במטה 'דני' שיש להפעיל לחץ על האוכלוסייה לעזוב"." and, just before, "כמקובל בפעולות צבאיות נגד יישובים ערביים, איפשר צה"ל ואף עודד בריחת תושבים בשלבי הכיתור והלחימה" | match. The same passage has Morris's view ("... לזרוע בהלה בקרב אזרחיהן ולגרום להם לנוס על נפשם ..."), Gelber's ("גם לדעת יואב גלבר העדויות אינן מצביעות על מדיניות של גירוש מכוון") and Bar-On's, as the note describes them. Corrected 2026-10-27: match for the quoted fragments only, not a certificate that the note's summary of the passage is complete; the same section continues past Bar-On (see M5). | [S] | 26 Oct 2026 |
| L7 | HW-DANI uses "המפונים" and records the instruction sent that night | HW-DANI | "ההנחיות הללו נשלחו לאלון באותו ערב ..." and "המפונים הורשו לקחת את כל רכושם הנייד, מלבד רכב ממונע." | match for "המפונים"; see M2 for "that night" | [S] | 26 Oct 2026 |
| L8 | not proposed: casualty and count figures, 2026 documents | HW-LOD, HW-DANI | HW-LOD: "נהרגו שמונה לוחמים עבריים ו-250 ערבים", "בין 400 ל-1,700 בני-אדם", and "בשנת 2026 נחשפו מסמכים ופרוטוקולים חסויים מהתקופה ..."; HW-DANI: "תוך יומיים פונו 30,000 - 45,000 תושבים." | match: the figures and the 2026 passage are on the pages, as the note says; none is used. Added 2026-10-27: the 2026 passage is longer than the fragment quoted here (see M5). | [S] | 26 Oct 2026 |
| L9 | footnote table: Lydda B | HW-LOD reference list | the paragraph's references in order: "תום שגב, שיעור היסטוריה | מי יזם את גירוש ערביי רמלה ולוד - בן-גוריון או רבין?, באתר הארץ, 27 בנובמבר 2009; עופר רגב, כיבוש לוד ועלילות הטבח, באתר מידה, 8 ביולי 2015", then Morris p. 274, Bar-On (Cathedra 99), Gelber pp. 246-247, the Palmach site | match for Segev and Regev as the first footnote. The note's table lists only the first; Morris p. 274 is on the dispute sentence, which the note describes without a page. | [S] | 26 Oct 2026 |

## 4. Ramle

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| R1 | Line A | JVL, Danny account | "On July 12, Kiryati units took Ramle, which surrendered after a brief engagement." | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| R2 | Line B | he.wiki [רמלה](https://he.wikipedia.org/w/index.php?oldid=44080267) | "לאחר כניעת העיר, בעקבות תקריות ירי בלוד, הוצאה הוראה ממטה מבצע דני לגרש את תושבי רמלה, למעט הגברים הצעירים, מזרחה לכיוון עמדות הלגיון בהרים." | match | [S] | 26 Oct 2026 |
| R3 | footnote under R2 | HW-RAMLA reference list | "יואב גלבר, קוממיות ונכבה: ישראל, הפלסטינים ומדינות ערב, 1948, אור יהודה: דביר, תשס"ד 2004, עמ' 246." | match | [S] | 26 Oct 2026 |
| R4 | context: the buses to al-Qubab | HW-RAMLA | "תושבי רמלה הוסעו באוטובוסים ומשאיות על ידי חיילי חטיבת קרייתי לאל קובאב (כיום כפר בן-נון) ומשם המשיכו ברגל." | match | [S] | 26 Oct 2026 |
| R5 | footnote under R4 | HW-RAMLA reference list | first reference after the sentence: "בני מוריס, ... עמ' 280." (then Friedman, *Iyunim* 42, 2025, pp. 183-184; Morris p. 283) | match | [S] | 26 Oct 2026 |
| R6 | context: residents left after the war | HW-RAMLA | "ברמלה נותרו לאחר אירועי מלחמת העצמאות כ-920 תושבים ערבים, רובם נוצרים." | match | [S] | 26 Oct 2026 |
| R7 | [C]: most had decided to leave | HW-RAMLA | "אלא שבאותו לילה כבר החליטו רוב התושבים לצאת מהעיר ולא להישאר תחת שלטון יהודי." | match | [S] | 26 Oct 2026 |
| R8 | [C]: the government did not know | HW-RAMLA | "ממשלת ישראל לא ידעה על הוראות הגירוש." | match | [S] | 26 Oct 2026 |
| R9 | footnote under R7 and R8 | HW-RAMLA reference list | first reference after each: "בני מוריס, ... עמ' 278–279." | match | [S] | 26 Oct 2026 |
| R10 | al-Qubab today: "כיום משמר איילון" (HW-DANI), "כיום כפר בן-נון" (HW-RAMLA) | HW-DANI, HW-RAMLA | HW-DANI: "מטה מבצע דני גייס 50 משאיות שהובילו את התושבים עד אל קובאב (כיום משמר איילון)"; HW-RAMLA as R4 | match: the two pages do differ, as the note says | [S] | 26 Oct 2026 |

## 5. Beersheba

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| B1 | Line A, the surrender | JVL, ["Operation Yoav"](https://jewishvirtuallibrary.org/jsource/History/Yoav.html), IDF account | "After fierce fighting, the 500-strong Egyptian garrison broke and, by 09:00 hrs that morning, Beersheba - capital of the Negev had surrendered to Israeli forces." | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| B2 | the morning is 21 October | same | "At 4:00 on the morning of 21 October, the IDF moved to capture Beersheba." | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| B3 | Line B and B' | he.wiki [באר שבע](https://he.wikipedia.org/w/index.php?oldid=44069386) | "רוב תושבי העיר נמלטו מזרחה, לעבר הרי חברון, ומספר ימים לאחר הכיבוש גורשו כמה מאות התושבים הנותרים לרצועת עזה." | match. As the note says, the first clause alone ("fled") is B'; "גורשו" for the remainder is in the second clause. | [S] | 26 Oct 2026 |
| B4 | footnote under B3 | HW-B7 reference list | "בני מוריס, לידתה של בעיית הפליטים הפלסטינים 1947–1949, עמ' 294." | match | [S] | 26 Oct 2026 |
| B5 | [C] time of day | HW-B7 | "נכבשה העיר בשעות הצהריים של 21 באוקטובר 1948" against B1's "by 09:00 hrs that morning" | match: the conflict is real | [S] | 26 Oct 2026 |

## 6. Jaffa

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| J1 | Line A/B | he.wiki [מבצע חמץ](https://he.wikipedia.org/w/index.php?oldid=43483130) | "בעקבות התקפת האצ"ל, גברה מאוד הנטישה של תושבי יפו והכפרים, עד אשר העיר נכנעה ב-13 במאי 1948." | match. No footnote on the sentence, as the note says. | [S] | 26 Oct 2026 |
| J2 | Line A/B | he.wiki [יפו](https://he.wikipedia.org/w/index.php?oldid=43979954) | "כך החלה הנטישה של תושבי יפו אשר נמשכה עד ל-13 במאי 1948 עם כניעת האוכלוסייה הערבית." | match. No footnote on the sentence. | [S] | 26 Oct 2026 |

## 7. Pages that could not be read

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| K1 | the Knesset PDF "answered with an HTML page instead of the PDF" | [IndependenceWar.pdf](https://m.knesset.gov.il/About/Occasion/Documents/IndependenceWar.pdf) | HTTP 200, an HTML body of 131,618 bytes, whose visible text reads "האתר אינו זמין כעת מהמיקום שלך ... The site is temporarily inaccessible from your location" | match: still a location block (Knesset geo-block) from this box, recorded not worked around. Nothing in the note's claims about the PDF (Ben-Ami without Acre; official lines for Nazareth, Ramla, Lod, Beersheba) can be checked here. Added 2026-10-27: a review agent's check (agent read 2026-10-27; a person's check pending) found that the PDF does load from outside this box (9 pages), so the block is on this box's side; see the result paragraph. Date corrected 2026-10-27: that agent read was on 26 Oct 2026, not 27 Oct. | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |

## 8. Owner quotes the note relies on (GitHub)

| id | claim in the note | source | printed text | match / conflict / not covered | tag | day read |
|---|---|---|---|---|---|---|
| O1 | Q-CARD-1 (c) wording | [#783 draft comment](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6231809370) | option "(c) A card stating the in-game consequence, with one sourced line of history." | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| O2 | the "Needs research" line | same | "the one sourced line for each target, from Israeli official or mainstream Israeli historiography, one value per fact." | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| O3 | "Accept all 16 drafts", Q-CARD-1 (c), Q-CARD-2 (a), Q-CARD-3 (a), Q-SENS-1b (a) | [#783, 18 Oct decision](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676) | "'Accept all 16 drafts': every remaining Phase 6 pick is decided as drafted" and the list "... Q-CARD-1 (c), Q-CARD-2 (a), Q-CARD-3 (a), Q-SENS-1b (a) ..." | match. Added 2026-10-27: on the page "Accept all 16 drafts" is in double quotes inside the coordinator's comment ("Owner decision (18 Oct 2026, coordinator session)"), so it is the coordinator's record of the owner's decision, not the owner's own words. | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| O4 | the he.wiki ruling is about Hebrew names only | [#1219, 21 Oct](https://github.com/eyalgolan/statecraft-israel/issues/1219#issuecomment-6318062081) | "on item 3 (the five villages without a Hebrew name): besides he.wiki, `name_he_sources` may cite **moreshet-map.org.il** and **the Israel Antiquities Authority**, but not Zochrot." | match (the printed text has bold markers around the two names) | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| O5 | the word is "displaced" | [#1327, 22 Oct](https://github.com/eyalgolan/statecraft-israel/pull/1327#issuecomment-6338595230) | "Wording: the word is **"displaced"**, as your sim line has it ("Its residents are displaced and the place is depopulated")." | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| O6 | Lydda, Ramle, Beersheba default "fled"; Acre "stayed"; Jaffa what-if | [#793, 18 Oct rulings](https://github.com/eyalgolan/statecraft-israel/issues/793) | "Default 'fled'; Morris isn't enough"; "Count as stayed"; "Only in a what-if"; "it surrendered on 13 May 1948"; the official sources consulted "do not describe the residents" | match | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |
| O7 | the reviewer's report on the Knesset PDF | [#1332 review, 22 Oct](https://github.com/eyalgolan/statecraft-israel/pull/1332#issuecomment-6338613561) | "the Knesset PDF does load as a PDF from another machine (9 pages, readable text), with official lines for Nazareth (Dekel), Ramla and Lod (Dani) and Beersheba (21 October). Its table lists Ben-Ami as the Western Galilee, 14-19 May 1948, without naming Acre." | match for what the report says; the PDF itself is not covered (K1) | [V, agent read 26 Oct; a person's check pending] | 26 Oct 2026 |

## Points for the note's author or the checking person

- **M1. Lydda context line, city unnamed.** L5's sentence ("בעיר נותרו רק כמה מאות תושבים ...") sits in he.wiki's
  section "ההתקוממות בלוד ופינוי לוד ורמלה", after the instructions for both cities and before the sentence on Ramle's
  surrender, so it reads as Lydda; the sentence itself does not name the city. A person may want to read it in the
  section, not alone.
- **M2. "that night" in the Lydda note.** The note says HW-DANI "records the instruction sent to the commander that
  night". The page prints "נשלחו לאלון באותו ערב" (that evening), to Alon, and for both cities.
- **M3. Acre line A.** "Acre surrendered on 18 May 1948" is the sentence's "18 במאי" plus the year from the article
  title; the sentence says the council signed a surrender document and opened the old city's gates.
- **M4. Lydda footnote row.** The note's table lists Segev and Regev for Lydda B; that is the first reference. The same
  paragraph also cites Morris p. 274 (the dispute sentence), Bar-On and Gelber pp. 246-247, which bears on the
  "Morris isn't enough" question 2 (the Lydda line B itself is not on Morris).
- **M5. The Lydda dispute is longer on the page (added 2026-10-27).** On he.wiki Lod, revision 43942324 (https://he.wikipedia.org/w/index.php?oldid=43942324), the section "כיבוש לוד במלחמת העצמאות" continues after the Bar-On and casualty sentences (L6, L8) with a further paragraph: "בשנת 2026 נחשפו מסמכים ופרוטוקולים חסויים מהתקופה, ובהם עדויות למקרים רבים של גירוש יזום ומכוון תוך שימוש באלימות לצורך הטלת אימה." The paragraph's footnote, placed at the end of the Makleff quotation, is Adam Raz, Haaretz, 26 February 2026. It is followed by Makleff's testimony at the Leis trial, as the page prints it: "היו פעולות בהן השמידו את האויב הפוטנציאלי, כלומר אזרחים. לדוגמה: בספסף, ג'יש, עילבון, לוד, רמלה ובדרום בקנה מידה יותר גדול. הכוונה הייתה לגרש. ..." (the quotation continues on the page: "אי אפשר לגרש 114 אלף איש שגרו (בגליל) בלי טרור, והיה צריך להיות אלמנט של טרור התחלתי כדי שילכו"). The source note listed only Morris, Kadish and Sela, Gelber and Bar-On and recorded the 2026 passage as "not read, not used" (`research_notes/GDD phase 6/displacement-cards.md:195`), so L6 certifies the quoted fragments, not the note's summary. A person should read the whole passage before questions 2 or 3 go to the owner. Name corrected 2026-10-27: "the Leis trial" above should read "the Lahis trial"; the page has "משפטו של שמואל להיס" (Shmuel Lahis). [S]
- **Not checked:** the Morris, Gelber, Segev, Regev and Abbasi pages behind the footnotes, and the Knesset PDF (K1).
