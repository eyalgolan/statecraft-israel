# Currencies 1948-93: the changes, their instruments and their conversion factors

Researched 2026-10-24, for issue #66 (research part only). Every page named below was opened by an AI agent on that
date ("agent read 2026-10-24; a person's check pending"). No game code, no `content/` file and no `currency.yaml` goes
with this note: the chapters design (§11, "Money") keeps the era-aware currency paused until after the MVP.

**Purpose.** Chapters design §11 says `content/currency.yaml` will hold "dated currencies, each with a display prefix, a
cumulative factor from the sim's internal unit, and sources", and that "which currencies and which dates (the
pound/lira, the shekel, the new shekel) is content to source". This note sources them from 1948, as the owner's
comment on #66 asks. For each change it gives the date, the instrument that made it, and the factor the instrument
states. Pre-1948 currencies are in `Era 1882-1948/national-funds-1882-1948.md` §5 and are not repeated.

**Conventions** (the same as the funds note and `Era 1957-77/economy-1957-77.md`)
- **[V]** verified on a page that was opened, in the instrument itself or in the cited official page. Every statute
  below was read on a rendered image of the page (the text layers of these scans are reversed Hebrew), and the quoted
  words were copied from that image.
- **[S]** single source, or a secondary source, or a summary that was not opened directly.
- **[C]** sources conflict. Every version is given.
- **[GAP]** not found. No value is filled in by inference.
- **Derived** marks arithmetic done here on the cited factors; the arithmetic is shown.
- **Our reading** marks an interpretation the source does not state.
- "S.H." is *Sefer HaChukim* (the Book of Laws), "O.R." is *Iton Rishmi* (Official Gazette of 1948), "K.T." is *Kovetz
  HaTakanot* (the Book of Regulations), "H.H." is *Hatza'ot Hok* (the bill series). Dates are as the page prints them;
  Hebrew dates are followed by the Gregorian date the page gives.

## Sources opened

The Knesset's own server (`fs.knesset.gov.il`, `main.knesset.gov.il`) answered the AWS box with an HTML block page
or a redirect to `knesset.gov.il/maintenance-page-geo`, so no Knesset PDF was read from the live site. Four of them
exist in the Internet Archive as byte copies of the same files; those copies were downloaded and each page was read
as an image. The Bank of Israel (BoI) pages returned a browser check to scripts; their Wayback copies were read.

| Key | Source | Where read |
|---|---|---|
| CO48 | *Pkudat HaMatbea* (Currency Ordinance), No. 19 of 5708-1948, O.R. No. 15, Supplement A, 12 Av 5708 (17.8.1948), p. 39. Signed 11 Av 5708 (16 August 1948) by Ben-Gurion, Felix Rosenblit (Rosenblueth) and Eliezer Kaplan. One page, read as an image | `fs.knesset.gov.il/0/law/0_lsr_312686.pdf`, read as the Wayback copy http://web.archive.org/web/20260927180638id_/https://fs.knesset.gov.il/0/law/0_lsr_312686.pdf |
| L52 | *Hok LeHachlafat Shitrei Bank UMilve Hova* (Law for the Exchange of Bank Notes and Compulsory Loan), 5712-1952, S.H. 105, 29 Av 5712 (20.8.1952), pp. 272-275 (printed as law No. 52 of that issue). Passed 21 Av 5712 (12 August 1952); bill H.H. 122, 1 Tammuz 5712 (24.6.52), p. 254. Signed Ben-Gurion, Eshkol, Sprinzak (acting President) | `fs.knesset.gov.il/2/law/2_lsr_209527.PDF`, Wayback copy http://web.archive.org/web/20230610115956id_/http://fs.knesset.gov.il//2/law/2_lsr_209527.PDF |
| L80 | *Hok Matbea HaSheqel* (Sheqel Currency Law), 5740-1980, S.H. 970, 8 Sivan 5740 (23.5.1980), pp. 128-130. Passed 27 Iyar 5740 (13 May 1980); bill H.H. 1437, p. 120. Signed Begin, Navon, Hurvitz | `fs.knesset.gov.il/9/law/9_lsr_208942.pdf`, Wayback copy http://web.archive.org/web/20240205053538id_/https://fs.knesset.gov.il/9/law/9_lsr_208942.pdf |
| L85 | *Hok Matbea HaSheqel HeHadash* (New Sheqel Currency Law), 5745-1985, S.H. 1157, 3.9.1985, p. 216 (the footnote's citation). Four pages. Signed Peres, Modai, Herzog | The Bank of Israel's copy of the law: https://www.boi.org.il/media/l5abksks/newshekellaw1985.pdf (the page footer says the document was prepared for the Bank by Hotsaat HaHalacha). The Knesset copy named in the issue (`11_lsr_210449.PDF`) returned the block page and its Wayback URL gave 404 |
| A59 | *Hok LeTikun Pkudat HaMatbea* (Law to Amend the Currency Ordinance), 5719-1959, S.H. 289, 10 Av 5719 (14.8.1959), p. 193 (the page footer prints "ספר החוקים 289"). Passed 29 Tammuz 5719 (4 August 1959); bill H.H. 371, p. 140. Signed Ben-Gurion, Eshkol, Ben-Zvi. One page, read as an image | `fs.knesset.gov.il/3/law/3_lsr_208936.pdf`, Wayback copy http://web.archive.org/web/20260928193601id_/https://fs.knesset.gov.il/3/law/3_lsr_208936.pdf |
| KT4095 | *Takanot Sha'at Hirum (Matbea HaSheqel)* (Emergency Regulations, Sheqel Currency), 5740-1980, K.T. 4095, 7 Adar 5740 (24.2.1980), pp. 1080-1081, signed by the Minister of Finance Hurvitz on 5 Adar 5740 (22 February 1980). A Ministry of Justice scan, five PDF pages, pp. 1080-1081 are PDF pp. 2-3 | https://olaw.org.il/takanot/takanot-4095.pdf |
| Wikisource | Hebrew Wikisource, "פקודת המטבע", revision 2992879: the Ordinance with its amendments marked in the text, and the Knesset database's amendment list (database id 2000879). A volunteer transcription | https://he.wikisource.org/w/index.php?title=פקודת_המטבע&oldid=2992879 |
| BoI sys | BoI, "The Israeli Currency System" (Wayback copy of 14 Feb 2026) | https://web.archive.org/web/20260214233524/https://www.boi.org.il/en/economic-roles/coins/currency-system/ |
| BoI APB | BoI, "Anglo Palestine Bank Series" (Wayback copy of 10 Apr 2026; page last updated 21/05/2023 as printed) | https://web.archive.org/web/20260410160014/https://boi.org.il/en/economic-roles/banknotes-and-coins/anglo-palestine-bank-series/ |
| BoI BL | BoI, "Bank Leumi Le-Israel Series" (Wayback copy of 15 Aug 2026; last updated 21/05/2023) | https://web.archive.org/web/20260815120948/https://boi.org.il/en/economic-roles/banknotes-and-coins/bank-leumi-le-israel-series/ |
| BoI 25m | BoI, "25 mil coin - the State of Israel's first coin" (Wayback copy of 21 Jan 2026) | https://web.archive.org/web/20260121004016/https://boi.org.il/en/economic-roles/coins/25-mil-coin-the-state-of-israels-first-coin/ |
| BoI ag | BoI, "Agora and Pound Series" (Wayback copy of 16 Apr 2026) | https://web.archive.org/web/20260416135447/https://www.boi.org.il/en/economic-roles/coins/agora-and-pound-series/ |
| BoI sh | BoI, "Sheqel Series" (Wayback copy of 12 Jun 2026) | https://web.archive.org/web/20260612170121/https://www.boi.org.il/en/economic-roles/banknotes-and-coins/sheqel-series/ |
| BoI nag | BoI, "New Agora and Sheqel Series" (Wayback copy of 25 Jan 2026) | https://web.archive.org/web/20260125065054/https://boi.org.il/en/economic-roles/coins/new-agora-and-sheqel-series/ |
| BoI NIS | BoI, "First Series of the New Sheqel" (Wayback copy of 2 Sep 2026) | https://web.archive.org/web/20260902090932/https://boi.org.il/en/economic-roles/banknotes-and-coins/first-series-of-the-new-sheqel/ |
| BoI AR00 | BoI Foreign Currency Department, *Annual Report 2000*, Appendix 2.4 "Foreign-exchange policy 1948-1988", printed p. 64 (PDF p. 16 of the chapter file; the same page the 1977-93 economy note cites as S8) | https://boi.org.il/boi_files/OldBankIsraelAnnualReport/Bank%20of%20Israel%20Annual%20Report%20-%202000/3/Part%202%20Developments%20in%20the%20Foreign%20Currency%20Market%20in%202000/ch-e2.pdf |
| en.wiki pound | English Wikipedia, "Israeli pound", revision 1351681459 (29 April 2026). A lead, not a source | https://en.wikipedia.org/w/index.php?title=Israeli_pound&oldid=1351681459 |
| en.wiki pruta | English Wikipedia, "Israeli pruta" (the version served on 2026-10-24, no revision id read). A lead | https://en.wikipedia.org/wiki/Israeli_pruta |
| he.wiki pound, he.wiki agora | Hebrew Wikipedia, "לירה ישראלית" and "אגורה (מטבע)" (versions served on 2026-10-24). Leads | https://he.wikipedia.org/wiki/לירה_ישראלית , https://he.wikipedia.org/wiki/אגורה_(מטבע) |

Opened and not cited for a fact: the BoI Currency Department's 2008 annual review (`boi.org.il/media/1jwct0iy/skira08.pdf`),
which has no dated history of these changes.

Not opened, so cited nowhere as read: the Knesset database page for the Ordinance (id 2000879, block page), the Knesset
PDFs of the 1960 and 1969 amendments (no snapshot in the Internet Archive), the *Reshumot* issues themselves, the
emergency regulations of 1952 and 1984, and the BoI orders under the Bank of Israel Law that cancelled coins.

---

## 1. The changes, in order

| # | Date | Change | Instrument | Factor stated by the instrument | Tag |
|---|---|---|---|---|---|
| 1 | 17 Aug 1948 (in force right after midnight at the start of 12 Av 5708) | The unit of currency is "the Israeli pound", divided into 1,000 mils. Every earlier reference to the Palestine pound ("pound", "£P (E.I.)", "Eretz-Israel pound") now means the Israeli pound | CO48 ss. 1, 2, 8 | 1 Israeli pound = 1 Palestine pound (s. 2, by reading all references as the same unit); 1 pound = 1,000 mils (s. 1) | [V] |
| 2 | 17 Aug 1948 | Anglo-Palestine Bank notes (printed "Palestine Pound") are legal tender | CO48 s. 3 (under the Bank Notes Ordinance, 5708-1948) | none: a legal-tender status | [V] |
| 3 | to 15 Sep 1948 | Palestine Currency Board notes stay legal tender "up to and including" 11 Elul 5708 (15 Sep 1948), "at the rate of one Palestine pound (E.I.) for every Israeli pound"; after that day they are foreign currency | CO48 ss. 4, 5 | **1 : 1, the par at the 1948 boundary** | [V] |
| 4 | 17 Aug 1948 | Mandate coins of 1927 stay legal tender: each mil on them counts as 1/1,000 of an Israeli pound | CO48 s. 6 | 1 mil = 1/1,000 pound | [V] |
| 5 | 24 Dec 1948 [S] | The thousandth part of the pound is called the prutah, in place of the mil | not found | 1 pound = 1,000 prutot | [GAP] for the instrument; [S] for the date (BoI sys); see §3 |
| 6 | 9 Jun 1952 (notes); exchange to 23 Jun 1952 | New notes in "Israeli pounds or prutot", issued by Bank Leumi, replace the Anglo-Palestine Bank notes. A 10 per cent compulsory loan is taken on the notes of 5, 10 and 50 pounds and on bank balances | First the Emergency Regulations (Exchange of Bank Notes and Compulsory Loan), 5712-1952 (K.T. 277, 15 Sivan 5712 = 9.6.52, p. 1028, cited by L52, not read); then L52, which replaced them (s. 17) | **Amount on the new note in Israeli pounds or prutot equals the amount on the old note in Eretz-Israel pounds or mils: 1 : 1** (L52 s. 3(a)). The 10 per cent is a loan, not a conversion discount (L52 ss. 4, 5, 8) | [V] |
| 7 | 20 Aug 1952 (published) | In any law, "mil" is read as "prutah" unless the text says mil and not prutah. L52 s. 13 was repealed from 1 Jan 1960 by A59 s. 3 (the new s. 4(2) of the Ordinance) and s. 4 | L52 s. 13; A59 ss. 3, 4 | 1 mil = 1 prutah | [V] |
| 8 | 1 Jan 1960 | The pound is divided into 100 agorot in place of 1,000 prutot (the words "divided into a thousand mils" are deleted from s. 1) | A59 ss. 1, 2, 4: commences 1 Tevet 5720 (1 January 1960); BoI ag and BoI sys give the same start | 1 pound = 100 agorot (s. 1 adds "החלק המאה של הלירה יהיה אגורה"); a sum "in mils or prutot" is read as agorot equal to **one tenth** of the sum of prutot or mils (s. 2), so 1 agora = 10 prutot | [V] |
| 9 | 4 Jun 1969 | The Knesset passes an amendment (No. 4) making "the sheqel" the unit, the agora its hundredth part. It never took effect | Amendment No. 4 to the Currency Ordinance, 5729-1969, S.H. 5729, p. 136 (not opened) | none | [S]; see §4 |
| 10 | 22 Feb 1980 (the law's commencement, L80 s. 12; the regulations, signed that day, carry no commencement provision, and BoI sh gives 22 Feb as the day the sheqel was "declared legal tender" [S]); notes in circulation 24 Feb 1980 | The sheqel replaces the Israeli pound; the "new agora" is its hundredth part. The Emergency Regulations of 22 Feb 1980 make it the unit and treat any "pound" as ten new agorot; they also repeal the Currency Ordinance (reg. 8) | KT4095 regs. 1, 2, 8 (signed 22 Feb 1980, printed 24 Feb 1980); then L80 ss. 1, 2, 12, which repeals the regulations (s. 10(b)) | **1 sheqel = 10 Israeli pounds** (L80 s. 2(a); KT4095 reg. 2(a): a pound is ten new agorot); **1 new agora = 10 agorot** (L80 s. 2(b); KT4095 reg. 2(b)) | [V] |
| 11 | 30 Sep 1980 | The "fixed day" for cheques (L80 s. 4); BoI AR00 p. 64 says prices and exchange rates were quoted in sheqalim from this date | L80 s. 4; BoI AR00 | none | [V] |
| 12 | 31 Mar 1984 | The pound ceases to be legal tender | BoI AR00 p. 64 and BoI ag; the regulations fixing the day under L80 s. 6 were not found | none | [S] for the date; [GAP] for the instrument |
| 13 | 4 Sep 1985 | The new sheqel (NIS) replaces the sheqel; its hundredth part is the agora | L85 ss. 1, 2, 11 | **1 new sheqel = 1,000 sheqalim** (s. 2(a)); **1 agora = 10 sheqalim** (s. 2(b)) | [V] |
| 14 | 1 Jan 1986 | The "fixed day" (L85 s. 4(a)); from it prices are shown and records kept in new sheqalim (L85 s. 5, our reading of its margin heading and text); BoI AR00 p. 64 says prices and exchange rates quoted in new sheqalim | L85 ss. 4(a), 5; BoI AR00 | none | [V] |
| 15 | 4 Sep 1986 | The sheqel ceases to be legal tender | L85 s. 3(b) (one year from commencement); BoI sh and BoI AR00 give the date | none | [V] for the one-year rule; the 4 Sep 1986 date is BoI [S] |

## 2. What each instrument says on the page

### 2.1 The Currency Ordinance, 17 August 1948 (CO48) [V]

- s. 1: "יחידת המטבע תהא הלירה הישראלית, מחולקת לאלף מילים." (the unit of currency shall be the Israeli pound, divided
  into a thousand mils).
- s. 2: wherever "פונט או פונט פלשתינאי (א"י) או לירה ארץ-ישראלית או לירה א"י או לא"י או לירה" is said, written or
  hinted, in the past or the future, for any purpose, it is treated as meaning the Israeli pound, unless the
  Ordinance says otherwise. This is the legal statement that the pound did not change unit at the boundary.
- s. 3: notes issued under the Bank Notes Ordinance, 5708-1948, are legal tender for any amount and any purpose.
- s. 4: notes issued by the Palestine Currency Board remain legal tender "עד יום י"א אלול תש"ח (15 בספטמבר 1948) ועד
  בכלל", in payment of any amount and for any purpose "בשער של פונט פלשתינאי (א"י) אחד לכל לירה ישראלית אחת".
- s. 5: right after that day the Board's notes cease to be legal tender and count "as foreign currency for all
  purposes".
- s. 6: the King's Order in Council on Palestine coins, 1927, ceases to be in force where the Ordinance applies, but the
  coins issued under it stay legal tender "as if each mil of the amount in mils printed on each of them is a thousandth
  part of an Israeli pound".
- s. 8: the Ordinance is in force "מיד לאחר חצות, אור ליום י"ב באב תש"ח (17 באוגוסט 1948)". It was signed on 11 Av 5708
  (16 August 1948) and printed in O.R. No. 15 (Supplement A) dated 12 Av 5708 (17.8.1948).
- Page number: the issue's page header, the 1952 law's footnote 3 and the 1980 law's footnote 5 all give **p. 39**.
  Wikisource gives the same ("ע״ר תש״ח, תוס׳ א, 39").
- BoI APB says "On August 17 the government passed a law declaring the notes legal tender, and they were put into
  circulation on the following day." That matches s. 3 and s. 8 of the Ordinance: the law is the Ordinance of 16 to 17
  August, the circulation date is 18 August 1948.
- Our reading: the funds note's `[V]` for the end of Board legal tender (15 September 1948, from Berlin pp. 62, 67) is
  confirmed by the Ordinance itself, s. 4.

### 2.2 The 1952 exchange: the closed [GAP] (L52) [V]

The funds note (`Era 1882-1948/national-funds-1882-1948.md`, §5, "The par the design asks about") said no source it
had opened states an exchange rate for 1952 and that a primary statement of the par was [GAP]. The statement is in the
law that regulated the exchange.

- s. 1 defines the two kinds of note. "שטר בנק ישן": an Anglo-Palestine Bank note, "בערבון מוגבל" (limited), "בלירות
  ארץ ישראליות או במילים" (denominated in Eretz-Israel pounds or mils). "שטר בנק חדש": a note of Bank Leumi LeIsrael,
  "בלירות ישראליות או בפרוטות" (in Israeli pounds or prutot).
- s. 2: despite s. 3 of the Currency Ordinance, an old note "לא תהיה עוד הצעת שטר בנק ישן הצעת מטבע חוקית" (an old bank note is no
  longer legal tender).
- s. 3(a): the Issue Department of Bank Leumi exchanges new notes for old notes, "והסכום הנקוב בשטרי הבנק החדשים
  בלירות ישראליות או בפרוטות יהא שווה לסכום של לירות ארץ ישראליות או מילים הנקוב בשטרי הבנק הישנים שנמסרו לה". In
  English: the sum on the new notes in Israeli pounds or prutot is equal to the sum in Eretz-Israel pounds or mils on
  the old notes handed in. **Par, 1 : 1, for pounds and for the thousandths.**
- s. 3(b): the exchange runs "עד יום ל' בסיון תשי"ב (23 ביוני 1952)". This is BoI APB's cancellation date for the
  Anglo-Palestine notes (23 June 1952), now with its legal basis. The Minister of Finance may extend it for people
  abroad or prevented (s. 3(b)(1), (2)).
- ss. 4 and 5: the compulsory loan. Whoever hands in an old note of 5, 10 or 50 Eretz-Israel pounds gets a new note and
  is lent to the State an amount equal to ten per cent of the face value of the old note; a bank balance of 50
  pounds or more at close of business on 15 Sivan 5712 (8 June 1952) carries a loan of ten per cent of the credit.
  Section 15 prints the next day, "ט"ז בסיון תשי"ב (9 ביוני 1952)" (16 Sivan = 9 June), so it agrees with ss. 4 and 5.
  Only the K.T. 277 footnote on p. 274 pairs 15 Sivan with 9.6.52. BoI BL gives the issue date of the new notes as
  9 June 1952.
- s. 8: the loan bears interest of four per cent a year and is repayable by the Government in instalments from 11
  Tammuz 5715 (1 July 1955) up to 13 Tammuz 5726 (1 July 1966), the dates the page prints. (The loan's terms are not
  needed for the currency's par; they are here so nobody reads the 10 per cent as a rate of conversion.)
- s. 13: "כל מקום, מחוץ לחוק זה, שבו מדובר או ידובר במיל, לכל מטרה שהיא, ייחשב כאילו מדובר בפרוטה, אם לא נאמר
  במפורש שהכוונה היא למיל ולא לפרוטה." (Wherever outside this law a mil is or will be mentioned, it is read as a
  prutah.) A59 s. 3 and s. 4 repealed this section from 1 January 1960.
- s. 17: the law takes the place of the Emergency Regulations (Exchange of Bank Notes and Compulsory Loan), 5712-1952.
  K.T. 277 of 15 Sivan 5712 (9.6.52), p. 1028, is the citation in footnote 2; K.T. 260 of 19 Nisan 5712 (14.4.52), p.
  754, in footnote 1, is for the Emergency Regulations (Compulsory Payments), 5712-1952 (not read).
- The issue was published as S.H. 105 of 29 Av 5712 (20.8.1952); the Knesset passed the law on 21 Av 5712 (12 August
  1952) (the double-star footnote on p. 272). No commencement section appears on the four pages. Our reading: it took
  effect on publication, with s. 18 treating earlier acts under the regulations as lawful.
- **[C] with a retail source.** The Israel Mint Corporation's coin page for the 10-pound note (`en.israelmint.com/
  default/6619520010.html`) was not opened (HTTP 403), but a search summary of it said each Anglo-Palestine pound was
  swapped for a new pound "valued at only 900 prutah", with 10 per cent passed to the Ministry of Finance. That is the
  loan of ss. 4 and 5 described as a rate. The statute says the exchange is 1 : 1 and the 10 per cent is a loan. The
  summary is [S] and unverified; the statute is [V].
- BoI BL says: "The new money was introduced in June 1952, along with a 10 percent compulsory loan levied on cash
  holdings and current accounts." Agrees.

### 2.3 The Sheqel Currency Law, 1980 (L80) [V]

- s. 1: "(א) יחידת המטבע תהיה השקל. (ב) חלק המאה של השקל יהיה האגורה החדשה."
- s. 2: "(א) שקל אחד יהיה שווה ערך לעשר לירות ישראליות, ויבוא במקומן. (ב) אגורה חדשה אחת תהיה שוות ערך לעשר
  אגורות, ותבוא במקומן." (c) A sum said "in pounds or agorot" anywhere is read as if an equal-value sum "בשקלים או באגורות
  חדשות" (in sheqalim or new agorot) were stated (the page's s. 2(c)), and (d) a payment ending in less than half a new agora is dropped, half or more is
  rounded to a whole new agora.
- s. 3: coins issued in Israel before the commencement day, of 25, 5 or 1 agora or an amount in prutot, are no longer legal tender; the
  Bank of Israel exchanges them for twenty years (s. 3(b)). BoI ag gives "February 22, 1980" as the cancellation date
  for the 1, 5 and 25 agorot coins and March 31, 1984 for the 10 agorot coin, which is consistent with s. 3 not naming
  the 10 agorot coin. (Our reading of the first line of s. 3(a), which is dense on the image.)
- s. 4: "היום הקובע" is 20 Tishri 5741 (30 September 1980). Until that day a bank is not obliged to honour a cheque
  drawn in sheqalim; after it, not one drawn in pounds and agorot unless it was handed in for collection before the
  day (our reading of s. 4(c) and (d)). This is the 30 September 1980 of BoI AR00.
- s. 6: until a day the Minister of Finance fixes, whoever hands a bank notes and coins in pounds and agorot
  (cancelled coins included) is entitled to sheqel notes and coins for them, free of charge, and every bank must
  exchange them. The section does not say that the pound notes stay legal tender. The day was fixed by regulation; the regulation was not found (§5). BoI AR00 puts the
  end at 31 March 1984.
- s. 10: "(א) פקודת המטבע, תש"ח-1948 - בטלה. (ב) תקנות שעת חירום (מטבע השקל), תש"ם-1980 - בטלות." Footnote 5 gives
  O.R. 5708, Supplement A, p. 39; footnote 6 gives K.T. 5740, p. 1080.
- s. 12: "תחילתו של חוק זה ביום ה' באדר תש"ם (22 בפברואר 1980)". It was passed on 13 May 1980 and commences nearly
  three months earlier. The Emergency Regulations (Sheqel Currency), signed the same day, covered the interval (§2.3a).
- The BoI sheqel page agrees: "On February 22, 1980 the sheqel was declared legal tender, and the first notes went into
  circulation on February 24, 1980"; BoI nag: "With the introduction of the sheqel on February 24, 1980 ... One new
  agora was equal to ten (old) agorot."
- Wikisource's amendment list prints K.T. 5740 as p. **1081**; the law's own footnote prints **1080**. Resolved by
  KT4095: the regulations begin on p. 1080 and end, with the signature, on p. 1081, so both citations point at the
  same instrument (§2.3a).

### 2.3a The Emergency Regulations (Sheqel Currency), 1980 (KT4095) [V]

K.T. 4095 of 7 Adar 5740 (24.2.1980), pp. 1080-1081, "made under s. 9(a) of the Law and Administration Ordinance,
5708-1948"; signed by Finance Minister Hurvitz, "ה' באדר תש"ם (22 בפברואר 1980)", the day the law later made its
commencement.
- reg. 1: the unit of currency is the sheqel; its hundredth part is the new agora.
- reg. 2(a): "לירה ישראלית, לירה או ל"י, יראו כאילו נאמר בו עשר אגורות חדשות": one
  pound is ten new agorot, a tenth of a sheqel. Reg. 2(b) reads a sum in agorot as new agorot equal to a tenth of it.
- reg. 3: coins issued before the regulations' commencement (their own wording; they give no date for it) in 25, 5 or
  1 agora and in prutot cease to be legal tender;
  reg. 3(b) lets the Bank of Israel exchange them for twenty years.
- reg. 5: until a day the Minister fixes, whoever hands in notes and coins in pounds and agorot is entitled to sheqel
  ones free of charge. Reg. 8: "פקודת המטבע, תש"ח-1948 - בטלה". Reg. 4 sets the "fixed day" for cheques as the day
  the regulations commence.
- The regulations end at reg. 9 with the signature of 22 February 1980 and have no commencement provision; K.T. 4095 is
  dated 24 February 1980. That they took effect on 22 February is our reading, matching BoI sh ("On February 22, 1980
  the sheqel was declared legal tender") [S]. So, on that reading, the Emergency Regulations, not the law of 13 May, are
  what put the sheqel into force, and the law of 13 May 1980 restates them from the same date (our reading). The
  regulations repeal the Ordinance (reg. 8); L80 s. 10(a) repeats the repeal. The regulation fixing the end of the pound's legal tender (31 March 1984) is a later
  instrument and was not found.

### 2.4 The New Sheqel Currency Law, 1985 (L85) [V]

Read from the Bank of Israel's copy, a four-page file; the Knesset copy named in the issue could not be fetched.

- Title and publication: "חוק מטבע השקל החדש, התשמ"ה-1985", footnote 1: "ס"ח 1157, התשמ"ה (3.9.1985), עמ' 216."
- s. 1: "(א) המטבע של ישראל יהיה שקל חדש. (ב) החלק המאה של השקל החדש יהיה האגורה."
- s. 2: "(א) שקל חדש אחד יהיה שווה ערך לאלף שקלים ויבוא במקומם. (ב) אגורה אחת תהיה שוות ערך לעשרה שקלים ותבוא
  במקומם." (c) a sum in pounds or sheqalim is read as if an equal-value sum in new sheqalim were stated.
- s. 3: on commencement, coins of one new agora and coins and notes of 1 sheqel and of 5 sheqalim cease to be legal
  tender; other coins and notes that were legal tender continue so for one year from commencement (s. 3(b)), and the
  Bank of Israel exchanges them for two years (s. 3(d), our reading).
- s. 4: the "fixed day" is "כ' בטבת התשמ"ו (1 בינואר 1986)". The Treasury Minister may fix a later day by a notice
  in *Reshumot* (s. 4(b)).
- s. 9: the Sheqel Currency Law 5740-1980 is repealed; s. 10 amends the Bank of Israel Law's definition of "currency";
  s. 11: "תחילתו של חוק זה ביום י"ח באלול התשמ"ה (4 בספטמבר 1985)".
- Derived: the old sheqel's one-year continuation from 4 September 1985 ends 4 September 1986, which is BoI sh's
  "Cancellation date September 4, 1986" for the old sheqel notes and BoI AR00's "on September 4, 1986, the sheqel
  ceased to be legal tender". The three sources agree.
- BoI NIS: "On September 4, 1985 the new sheqel, equal to 1,000 old sheqalim, became the currency of Israel." It also
  names "the Sheqel Currency Law, 5729-1969" as the law whose purpose (to keep the ancient name) the name "sheqel"
  follows; see §4.

## 3. Mils and prutot: the 24 December 1948 date and the sources that disagree

| Source | What it says about the thousandth of the pound |
|---|---|
| CO48 (primary) | s. 1 (17 Aug 1948) calls it a **mil**: "divided into a thousand mils". The word prutah does not appear on the page |
| BoI sys | Two lines: "From August 17, 1948: One Israeli Pound = 1000 mils" and "From December 24, 1948: One Israeli Pound = 1000 Prutot" |
| BoI 25m | A coin of "25 mil" was issued first (6 April 1949), "the term 'mil' was carried over from the British Mandate"; "the first coin in the new series, a 50 pruta coin, was only entered into circulation on May 11, 1949" |
| L52 s. 13 (primary) | Printed 20 Aug 1952: a mil is read as a prutah. The same law names the new notes "בלירות ישראליות או בפרוטות" |
| BoI BL | The 1952 notes carry "500 Prutah", but the 1948 notes of the Anglo-Palestine Bank carry "500 Mils" (BoI APB) |
| en.wiki pound | Infobox: mil as the subunit 1951-52 and pruta 1952-60 (the page cites nothing for it) |
| he.wiki pound | Infobox: prutah 1950-60; coin table: first prutah coins dated 4 Jan 1950 and 25 Oct 1950 |
| en.wiki pruta | "introduced shortly after the establishment of the state"; no date |

- **[C]** on when the name changed: 24 December 1948 (BoI sys), the coin issue of 11 May 1949 (BoI 25m), 1950
  (he.wiki), 1952 (en.wiki pound, and L52 s. 13 as the first statute found that equates them). No instrument was found
  for the 24 December 1948 date: **[GAP]**. Our reading: the Ordinance (17 August 1948) still said "mils"; the law
  that first treats the two as one, among the documents read, is L52 s. 13; the 1948 date has no instrument in hand.
- The factor does not depend on the name: a thousandth part either way. 1 pound = 1,000 mils = 1,000 prutot [V] (CO48 s.
  1 and L52 s. 3(a)).
- A59 s. 2 is the source of that wording: it adds s. 2(b) to the Ordinance, "כל מקום בו נאמר ... סכום כלשהו במילים או
  בפרוטות, יראו כאילו נאמר בו סכום אגורות השווה לחלק העשירי של סכום הפרוטות או המילים" [V]. The Wikisource text
  matches it.

## 4. The 1969 law and the 1959 and 1960 amendments

- **1959 amendment (the agora)** [V], A59, read on the page: "חוק לתיקון פקודת המטבע, תשי"ט-1959", S.H. 289, 10 Av
  5719 (14.8.1959), p. 193, passed 29 Tammuz 5719 (4 August 1959). Section 1 deletes "מחולקת לאלף מילים" from s. 1 of
  the Ordinance and adds "(ב) החלק המאה של הלירה יהיה אגורה". Section 2 adds s. 2(b), the one-tenth rule (§1, row 8).
  Section 3 adds ss. 3 (forged coins) and 4 (repeals the Currency Notes Ordinance, "פקודת שטרי הכסף", and s.
  13 of the 1952 law). Section 4: commencement "ביום א' בטבת תש"ך (1 בינואר 1960)". BoI ag gives the practical start
  and the coin issue dates (1 agora 12 May 1960, 10 agorot 6 May 1960, 25 agorot 17 May 1960, 5 agorot 20 October
  1960, as the page prints them). Wikisource's "ס״ח תשי״ט, 193" is this page.
- **1960 amendment** [S]: a Knesset-site search result named a file `4_lsr_208937.pdf` as "Law to Amend the Currency
  Ordinance (No. 2), 5720-1960" (Wikisource flags the "No. 2" as wrong and says No. 3), rounding at half an agora; S.H.
  5720, p. 46 in the Wikisource list. The file has no Internet Archive snapshot and was not opened.
- **1969 amendment (No. 4)** [S]: BoI sh says "On June 4, 1969 the Knesset passed a law providing for the sheqel to
  become the currency of Israel at a date to be determined on the recommendation of the Governor of the Bank of
  Israel"; BoI NIS calls it "the Sheqel Currency Law, 5729-1969". Wikisource says it was to commence on a date the
  Knesset Finance Committee would fix on the Governor's proposal, "this decision was never taken and amendment No. 4
  never took effect", and that the Sheqel Currency Law of 13 May 1980 replaced the Ordinance. The text was not opened.
- Our reading: the sheqel was therefore never in force from 1969; the instrument that made it the currency is L80,
  from 22 February 1980.

## 5. Cumulative factors

All derived from the factors stated in §1, with the arithmetic shown. The sim's internal unit and any display rule are
the owner's decision (chapters design §11, D5); this table only supplies the sourced multipliers.

| From | To | Factor | Derived from |
|---|---|---|---|
| 1 Palestine pound (to 15 Sep 1948) | Israeli pound | 1 | CO48 s. 4, [V] |
| 1 Anglo-Palestine "pound" note (to 23 Jun 1952) | 1 Bank Leumi Israeli pound note | 1 | L52 s. 3(a), [V] |
| 1 Israeli pound | prutot (to 1960) | 1,000 | CO48 s. 1, L52 s. 3(a) |
| 1 Israeli pound | agorot (from 1960) | 100 | A59 s. 1, [V]; BoI sys, BoI ag |
| 1 prutah | agorot | 0.1 (1 agora = 10 prutot) | A59 s. 2, [V]; 100 / 1,000 |
| 1 sheqel (24 Feb 1980) | Israeli pounds | 10 | L80 s. 2(a), [V] |
| 1 new agora (1980) | agorot of 1960 to 1980 | 10 | L80 s. 2(b), [V]; consistent with 100 new agorot = 1 sheqel = 1,000 agorot |
| 1 new sheqel (4 Sep 1985) | sheqalim | 1,000 | L85 s. 2(a), [V] |
| 1 new sheqel | Israeli pounds | **10,000** | Derived: 1,000 sheqalim x 10 pounds |
| 1 Israeli pound | new sheqalim | **0.0001** | Derived: 1 / 10,000 |
| 1 agora (1985) | sheqalim | 10 | L85 s. 2(b), [V]; also BoI sys |
| 1 Palestine pound of 1948 | new sheqalim | 0.0001 | Derived: par to the Israeli pound, then x 0.0001 |

The two intermediate factors that no single instrument states are the ones a `currency.yaml` would carry: pound to
sheqel is 0.1 (1980) and sheqel to new sheqel is 0.001 (1985), so a pre-1980 pound amount is divided by 10,000 to
display it in new sheqalim. That is arithmetic here, not a figure printed on a page.

## 6. Exchange rates are not conversion factors

The factors above change the unit's name and size; they are not exchange rates. The sterling link, the dollar quote
and the devaluations are in `Era 1957-77/economy-1957-77.md` §3. BoI AR00 p. 64 says: "After the establishment of the
State of Israel its currency continued to be linked to the pound sterling, as it had been during the British Mandate,
but in practice exchange rates were also quoted in US dollars. From January 1954 the new official rate of the Israeli
pound was quoted in US dollars and not in pounds sterling." [V] en.wiki pound's "£1 stg at par (1952-1954)" has no
citation and was not checked here: [S].

## 7. Agreement with the other notes in the repository

- `Era 1977-93/economy-1977-93.md` §4 lists the same 1980 to 1986 dates from BoI sh and BoI AR00 p. 64 ("S7", "S8").
  Every one of them is now also traced to the law: 22 and 24 February 1980 (L80 s. 12, BoI sh), 30 September 1980 (L80 s.
  4, the cheque fixed day; "prices and exchange rates" is BoI AR00's wording), 4 September 1985 and 1 January 1986
  (L85 ss. 11, 4(a)), and 4 September 1986 (L85 s. 3(b), derived). The exception is
  31 March 1984 (§1, row 12), which still has no instrument.
- `Era 1882-1948/national-funds-1882-1948.md` §5: its row "Anglo-Palestine Bank notes, denominated in Palestine pounds,
  legal tender in Israel: law of 17 August 1948" is CO48, No. 19 of 5708-1948; its "[GAP]" on the par of the 1952
  change closes with L52 s. 3(a) (§2.2 above).
- `Era 1977-93/events-1977-82.md` B12a says the sheqel was "declared legal tender" on 22 February 1980 (BoI sh); L80 s.
  12 makes that day the commencement.

## 8. Gaps for a person

1. **The instrument for 24 December 1948** (mil to prutah). BoI sys is the only source of the date. A person with the
   Reshumot index could look for an order or notice of December 1948 or early 1949.
2. **The 1960 and 1969 amendments** (S.H. 5720 p. 46; S.H. 5729 p. 136; the 1969 one never took effect per
   Wikisource): their Knesset PDFs have no Internet Archive snapshot and the live server blocks this box. The 1959
   amendment, which made the agora, is read (A59).
3. **The 1952 emergency regulations** (K.T. 277, p. 1028) and the regulation that fixed the end of the pound's legal
   tender under L80 s. 6 and reg. 5 (31 March 1984), with any Treasury notice. The 1980 regulations are read (KT4095).
4. **The Knesset copy of the 1985 law** (`11_lsr_210449.PDF`). The text read is the Bank of Israel's copy; a person
   should compare it with S.H. 1157 p. 216.
5. **The Israel Mint "900 prutah" claim** (§2.2) was only seen in a search summary. L52 s. 15 shows how a 90 per cent
   cash and 10 per cent loan-claim split reached the holder: a person holding money for another is discharged even
   if he gave "90 אחוז מן הסכום במזומנים בצירוף 10 אחוזים מאותו סכום בזכות למילווה". Our reading: the claim describes
   the loan, not an exchange rate; a person who can open the Mint page should read what it cites.
6. **How the 10 per cent loan reached the holder of an old note.** L52 ss. 4, 6 say it was withheld as a loan: the
   new note's amount was the par amount, with the loan retained. Our reading, supported by s. 15 (a holder of money
   for another is discharged if he handed over 90 per cent in cash plus a 10 per cent claim on the loan).
