# The first designated site, and the Yeruham photographs (research note, 2026-10-14)

Status: research only, for the owner's choice. Nothing here is in `content/`, `sim/` or `unity/`. Refs #378 (GDD
Phase 1, PR 5, Task 5.0), #27, and ruling 4 of the Phase 1 plan
(`docs/superpowers/plans/2026-10-12-gdd-phase-1.md`, "PR 5").

The question: which settlement founded in 1948-51 should be the first designated site for "Build yishuv"
(`docs/GDD.md`, "Pioneer work"), and are Werner Braun's 1951 photographs of Yeruham on Wikimedia Commons public
domain as their tag says?

This note builds on section 4 of `objective-cards.md` (13 October 2026), which recommended Tifrah. It re-checks that
work, finds a second dated source, and changes two of its findings (marked "Change from the earlier note").

## How it was done

- Every page was fetched on 14 October 2026 with `curl` and the User-Agent
  `statecraft-israel-research/1.0 (+https://github.com/eyalgolan/statecraft-israel)`. No request carried a personal
  identifier. A few pages were found through a web search tool; each was then fetched and read directly.
- Wikipedia, Commons, Wikidata and Hebrew Wikisource were read through the MediaWiki API (wikitext of the current
  revision). Wikipedia is used as a lead. Where a fact rests on Wikipedia alone it is tagged single source and says so.
- Hebrew dates were converted with the `convertdate` library.
- Hexes were computed with the repo's own rule (`tools/hexgrid.py`, `settlement_hex`) against
  `content/map/hexes.json`, `settlement_hexes.yaml`, `localities_1948.tsv` and `armistice_hexes_1949.yaml`.

Tags, one per fact:
- **[V] verified**: two or more independent sources agree, or one authoritative primary source (a statute, a file
  page's own licence data).
- **[S] single source**: one source, or several that plainly copy one another.
- **[C] conflicting**: sources disagree. The conflict and the sources on each side are stated.

### Sources not reached

- **NLI historical press (JPress), `www.nli.org.il/he/newspapers/...`**: HTTP 403 with a "Checking your browser"
  page, for `curl` and for the fetch tool. Tried: Davar, 11 October 1949, article 40
  (`https://www.nli.org.il/he/newspapers/dav/1949/10/11/01/article/40`), the article Hebrew Wikipedia cites for
  Tifrah. The Wayback Machine holds the page shell (snapshot of 8 July 2023) with the headline but not the article
  text, which loads by script. So **no contemporaneous newspaper was read for any candidate**. This is the main gap.
- **Israel State Archives**, `https://catalog.archives.gov.il/site/chapter/great-settlement-1948-1952/`: HTTP 403.
- **WIPO Lex** copy of the Copyright Act 2007, `https://www.wipo.int/edocs/lexdocs/laws/en/il/il033en.pdf`: returns
  an HTML page, not the PDF. The Act was read in Hebrew on Wikisource instead (below).
- **JTA archive**: reached (`https://www.jta.org/?s=...`), but searches for Yeruham, Yerucham, Tel Yeruham, Kfar
  Yeruham, Tifrah, Tifrach and Mefalsim returned no report of any of the three foundings.
- Not tried: Central Bureau of Statistics locality files, Knesset records, Central Zionist Archives and KKL archive
  catalogues (beyond the one CZA exhibition page below), the Jewish Agency Settlement Department's own records. None
  of the facts below rests on them.
- Books cited by Wikipedia and not read: Binyamin Kluger, *Givat Shaul and its neighbourhoods*, p. 353 (Tifrah);
  Shimshon Livni in *The Land of the Negev* (Ministry of Defense Publishing, 1994), p. 588 (Yeruham).

## Summary

| | Mefalsim | Tifrah | Yeruham |
|---|---|---|---|
| Type | Kibbutz | Moshav | Ma'abara (transit camp), later a town |
| Founding date | 12 June 1949 **[V]** | 27 September or 18 October 1949, or 1950 **[C]** | 9 or 11 January 1951 **[C]** |
| Founders | A garin of the Dror youth movement from Latin America **[V]** | Immigrants, Poalei Agudat Israel **[V]**; origin **[C]** | Set up by the Jewish Agency for immigrants from Romania **[V]** |
| Photograph inside the policy | Boris Carmi, June 1949, CC BY 4.0 **[V]** | Benno Rothenberg, 2 December 1949, CC BY 4.0 **[V]** | None from 1951 (section 3) |
| Hex (q, r) | (-10, 27): straddles the Gaza armistice line | (-10, 30): clear | (-10, 35): clear |
| Plan criteria met (Task 5.0) | 1 with a caveat, 2, 3, 4 | 1, 3, 4; fails 2 | 1, 4 with a note; fails 2 and 3 |

**Recommendation: Mefalsim, 12 June 1949.** It is the only candidate whose founding day is agreed by independent
sources and which also has a founding-month photograph under a licence the content policy accepts. It carries three
points the owner must see before accepting it: its hex, its 1950 move, and its association with 7 October 2023
(section 1). If any of those rules it out, no candidate meets all four criteria and the plan says to stop.

## 1. Mefalsim (recommended)

### Facts

- **Founding date: 12 June 1949. [V]** Three sources agree:
  - Haim Gvati, *100 Years of Settlement*, vol. 2, appendix "List of all Jewish rural settlements in Israel", which
    has a founding-date column. Row: Mefalsim, South, 12.6.49, HaIhud. Read in the Ben-Yehuda Project edition:
    https://benyehuda.org/read/14684 (checked 14 October 2026). The same volume's text lists, among the
    foundings of spring 1949, a kibbutz set up in the Negev by the Mefalsim garin of HaKibbutz HaMeuhad. The
    edition's print publication details were not on the page I read.
  - Kibbutz Story, "Mefalsim, the first Argentine kibbutz": an advance party went to the site on 8 June 1949 and the
    official ceremony of going onto the land was held on 12 June 1949. It carries an item captioned as the day of
    going up to Mefalsim, 12.6.1949, credited to the kibbutz's 50th-anniversary bulletin.
    https://www.kibbutz-story.com/post/mefalsim (checked 14 October 2026).
  - "Israel Revealed to the Eye" (Yad Ben-Zvi), Kibbutz Mefalsim page: the ceremony of going onto Jewish National
    Fund land was held on 12 June 1949.
    https://israelalbum.org.il/mercazim/%D7%A7%D7%99%D7%91%D7%95%D7%A5-%D7%9E%D7%A4%D7%9C%D7%A1%D7%99%D7%9D/
    (checked 14 October 2026).
  - Independence: Gvati's list is a separate compilation. The other two both draw on the kibbutz's own archive, so
    count them as one line of evidence. Two independent lines agree. No 1949 newspaper was read (JPress, above).
  - Year only, consistent: English and Hebrew Wikipedia ("Mefalsim", "מפלסים"), the Sha'ar HaNegev Regional Council
    (https://www.sng.org.il/350/), the Central Zionist Archives exhibition page
    (http://www.zionistarchives.org.il/events/South/Pages/Haotef8.aspx), Wikidata Q2890190. All checked 14 October
    2026.
- **The advance party arrived on 8 June 1949. [S]** Kibbutz Story only. If the card wants "the day the first people
  arrived" instead of the ceremony, this is the date, and it has one source.
- **Founding group. [V]** Graduates of the Dror Zionist-socialist youth movement from Latin America, mainly Argentina
  and Uruguay, who had trained abroad: Yad Ben-Zvi page, CZA page, Kibbutz Story, both Wikipedias. Kibbutz Story adds
  64 founding members, with a minority from Brazil and Chile **[S]**.
- **Movement. [C, minor]** Gvati's narrative and the Yad Ben-Zvi page: HaKibbutz HaMeuhad at founding. Gvati's list
  gives "HaIhud", the later affiliation. Yad Ben-Zvi: the kibbutz left HaKibbutz HaMeuhad in 1951; Hebrew Wikipedia
  says May 1951. Use HaKibbutz HaMeuhad for 1949.
- **The Speaker of the Knesset, Yosef Sprinzak, attended the ceremony. [S]** Kibbutz Story. It also prints words it
  attributes to him. Do not quote them: the source is secondary and the wording is unverified.
- **Location today. [C, minor]** Hebrew Wikipedia: 31.50200 N, 34.56193 E. English Wikipedia: 31.50194 N, 34.56444 E.
  Wikidata: 31.50190 N, 34.56440 E. The spread is about 240 m. All are Wikipedia-family values: single source in
  kind. Either maps to the same hex.
- **The 1949 site was different. [V for the move, location not found]** The kibbutz was first set up on a kurkar hill
  facing the Gaza Strip border and moved about a year later (1950) to its present site because of direct fire from
  Gaza: Yad Ben-Zvi page, CZA page, Hebrew Wikipedia (which names Beit Hanoun as the direction of fire and says a
  firing range and an IDF post later stood on the first site). **I found no coordinates for the 1949 site.** The
  coordinates above are the 1950 site.
- **Photograph. [V]** Boris Carmi, NLI title "ימיו הראשונים של קיבוץ מפלסים" (the first days of kibbutz Mefalsim),
  dated June 1949. Read through the Commons API on 14 October 2026:
  - https://commons.wikimedia.org/wiki/File:Mefalsim_(997009452698205171.jpg
  - `{{cc-by-4.0}}` with the Meitar Collection credit line, VRTS ticket 2021092910005788, NLI ARC. 4* 2069 / 32475,
    1528 x 1566 px, SHA-1 f4d82b8f08f810081145eec13d24ac09f672873f.
  - This is the same file and data the earlier note recorded. The month matches the founding month.

### Fit with the game design

- The GDD's "Build yishuv" is done by a Halutz (pioneer) unit at a designated spot. Mefalsim was founded by a trained
  pioneer garin that went onto bare land on one dated day. That is the action the mechanic models. Yeruham was a
  camp the Jewish Agency set up for new immigrants; Tifrah was an immigrants' moshav.
- A day-level date is what the objective card needs for "completed on / historically" (Phase 1 plan, PR 6 and PR 7).
- June 1949 is 13 turns after 15 May 1948, early in a 48-turn game, so the timing bonus (D-B2) is reachable.

### What the owner must see first

1. **The hex straddles the armistice line.** Mefalsim maps to hex (-10, 27). That hex is land, and
   `settlement_hexes.yaml` puts no 1948 settlement in it, so the plan's validation as written passes. But
   `armistice_hexes_1949.yaml` lists it as a line hex of the Egypt-Israel line in the Gaza area, its centre
   (34.519 E, 31.519 N) lies west of the kibbutz toward Beit Hanoun, and `localities_1948.tsv` places four Gaza Strip
   villages in it (Jabaliya, Beit Hanun, Beit Lahiya, Nazla). The kibbutz itself is inside the line, about 1.2 km
   from it (Hebrew Wikipedia) or 1.3 km (regional council) **[C, minor]**. Whether a site may sit on a line hex is a
   design call the plan does not cover.
2. **The move.** The site the player builds is named Mefalsim, but the 1949 point and the present kibbutz are
   different places, probably in the same hex. A card that says "founded 12 June 1949" is accurate; a map pin at the
   present coordinates is the 1950 site.
3. **7 October 2023.** Mefalsim was attacked that day; Hebrew Wikipedia describes the kibbutz's defence. The earlier
   note raised this too. It is a sensitivity call for the owner, not a sourcing problem.
4. **Depopulated localities (criterion 4).** Neither Wikipedia article names an Arab locality at either site, and the
   English Wikipedia articles on the nearest depopulated villages (Najd, Dimra, Huj) do not mention Mefalsim. By the
   coordinates in those articles they are about 6 km away. This is absence of evidence in Wikipedia, not a gazetteer
   check. **[S]**

## 2. The alternatives

### Tifrah

- **Founding date. [C]**
  - 27 September 1949, the eve of 5 Tishrei 5710: Hebrew Wikipedia "תפרח"
    (https://he.wikipedia.org/wiki/%D7%AA%D7%A4%D7%A8%D7%97, revision of 10 October 2026), citing Kluger, p. 353 (not
    read). 5 Tishrei 5710 is 28 September 1949, so "eve of" fits the evening of the 27th.
  - 18 October 1949: Gvati's list (URL above), row Tifrah, Northern Negev, 18.10.49, Poalei Agudat Israel.
  - 1950: English Wikipedia "Tifrah" (https://en.wikipedia.org/wiki/Tifrah), text and infobox.
  - A Davar report of 11 October 1949 is cited by Hebrew Wikipedia for the moshav's members joining the Histadrut.
    If it is about the moshav already on its land, it sits better with September than with 18 October. I could read
    only its headline (JPress, above), so this is not evidence yet.
  - **Change from the earlier note:** it had one source for 27 September and asked for a second. The second source
    found gives a different day. Criterion 2 is not met.
- **Founders. [V] for the movement, [C] for origin.** Poalei Agudat Israel: Gvati, both Wikipedias. Origin: Holocaust
  survivors from Romania and Hungary, with a group from Morocco joining in 1954 (Hebrew Wikipedia); Hungary,
  Czechoslovakia, Transylvania and North Africa (English Wikipedia).
- **Location. [S, Wikipedia]** 31.32540 N, 34.67818 E (Hebrew Wikipedia); 31.32639 N, 34.67667 E (English). Hex
  (-10, 30): land, no 1948 settlement, no 1948 locality, not a line hex.
- **Photograph. [V]** Benno Rothenberg, "הקמת מושב תפרח בנגב" (the founding of moshav Tifrah in the Negev), taken
  2 December 1949. `{{cc-by-4.0}}`, VRTS ticket 2021092910005788, 653 x 681 px, SHA-1
  e592a5bea461b5e1c36b81de9d332e9a3bfe5149. https://commons.wikimedia.org/wiki/File:Tifrah_997008137656705171.jpg
  (Commons API, 14 October 2026). Small for a card header. Its date is after both candidate founding days.
- Verdict: the cleanest map position and no sensitivity found, but the founding day is open. It becomes the best
  choice if the Davar article or the Kluger page settles the day.

### Yeruham (the plan's lead)

- **Founding date. [C]** Two days apart.
  - 9 January 1951 (2 Shevat 5711): the heritage plaque "The first ma'abara", Yeruham Local Council and the Council
    for Conservation of Heritage Sites, 2016, which says the Tel Yeruham ma'abara was inaugurated that day
    (https://shimur.org/signs/%D7%94%D7%9E%D7%A2%D7%91%D7%A8%D7%94-%D7%94%D7%A8%D7%90%D7%A9%D7%95%D7%A0%D7%94/); and
    English Wikipedia "Yeruham", citing Livni 1994, p. 588 (not read). The plaque's two calendars agree: 9 January
    1951 is 2 Shevat 5711.
  - 11 January 1951 (4 Shevat 5711): Hebrew Wikipedia "ירוחם", with no footnote on the date; repeated in the same
    words by https://maabarot-story.org/maabarot/%D7%9E%D7%A2%D7%91%D7%A8%D7%AA-%D7%99%D7%A8%D7%95%D7%97%D7%9D/ and
    by Srugim (https://www.srugim.co.il/634849). These three read as one source.
  - Srugim adds "Thursday ... (4.1.1951)". 4 January 1951 was 26 Tevet. 4 Shevat 5711 was Thursday 11 January, so
    its Gregorian date is a slip. **Correction to the earlier note:** it attributed "4.1.1951" to a Shimur
    presentation seen in a search summary. The Shimur plaque page, read today, says 9 January; "4.1.1951" is in
    Srugim.
  - The sources may describe two events (an inauguration on the 9th, people settling on the 11th). Nothing I read
    says so. A January 1951 newspaper would settle it; none was reached.
- **Founders. [V]** A ma'abara for immigrants from Romania, set up on the Jewish Agency's initiative: Hebrew
  Wikipedia and the plaque (which adds the Ministry of Labour and Solel Boneh). Hebrew Wikipedia names Amnon Zair of
  kibbutz Gan Shmuel as the man who set it up **[S]**. First called Tel Yeruham, then Kfar Yeruham. The ma'abara was
  dismantled in 1959 and the name became Yeruham in 1962 (Hebrew Wikipedia, maabarot-story) **[S]**.
- **Location. [S, Wikipedia]** 30.98796 N, 34.92900 E (Hebrew Wikipedia); 30.98861 N, 34.92806 E (English). Hex
  (-10, 35): land, desert, no 1948 settlement or locality, not a line hex.
- **Site history, for criterion 4. [S]** Hebrew Wikipedia: the ma'abara was set up in an area where Bedouin of the
  al-Azazme moved, some of whose people now live in Rakhma, an unrecognised village near Yeruham. No depopulated
  village is named. The owner should see this line before a Yeruham card is written.
- **Photographs:** none from 1951 inside the content policy (section 3). Meitar photographs of Yeruham start in
  1953.
- Verdict: honest check of the plan's lean. Yeruham is a well-known name and a clean hex, but it fails criterion 2 by
  two days and criterion 3 outright, and it was a transit camp set up by an agency, not a yishuv built by pioneers.
  It is the weakest fit of the three for "Build yishuv".

### Others, briefly

Gvati's list also dates Nahal Oz to 25.7.51 and Yotvata to 31.10.51. Hebrew Wikipedia gives 26 July 1951 and
30 October 1951 (per the earlier note), so each is off by a day **[C]**. Their Meitar photographs carry an NLI date
of "1950", before either founding (earlier note). Not pursued.

Gvati's list is worth knowing for #27 and #384: it gives a day for most kibbutzim and moshavim. It is one secondary
compilation and, as Tifrah, Nahal Oz and Yotvata show, it does not always match other sources.

## 3. Ruling 4: Werner Braun's 1951 Yeruham photographs

### What the licence data says

Three files, read through the Commons API on 14 October 2026:

| File | Date field | SHA-1 |
|---|---|---|
| [128639 yeruham PikiWiki Israel.png](https://commons.wikimedia.org/wiki/File:128639_yeruham_PikiWiki_Israel.png) | June 1951 | a7285b97b62a6fbcf23856786b581f1538ee7a5d |
| [129769 yeruham PikiWiki Israel.png](https://commons.wikimedia.org/wiki/File:129769_yeruham_PikiWiki_Israel.png) | July 1951 | 360fa9402c69d1207d01c4cfda9f0ad975bf40b6 |
| [129770 yeruham PikiWiki Israel.png](https://commons.wikimedia.org/wiki/File:129770_yeruham_PikiWiki_Israel.png) | July 1951 | ab90836dafd395be306c1cdff546789f30fef063 |

For each file **[V, the file page itself]**:
- Author field: ורנר בראון (Werner Braun). Uploader named in the description: ישראל פרקר (Israel Preker). Source of
  the photograph: "ארכיון ירוחם - דרך ארכיון קק"ל" (the Yeruham archive, by way of the KKL archive). Uploaded by the
  bot account Pikiwikisrael on 12 April and 14 May 2025.
- Permission: `{{PD-author|Israel Preker}}`. The template's text is that the work has been released into the public
  domain by its author, the named person, worldwide.
- Machine-readable metadata: License "pd", "Public domain", Copyrighted "False". Structured data: copyright licence
  (P275) "released into the public domain by the copyright holder" (Q98592850); copyright status (P6216)
  "copyrighted, dedicated to the public domain by copyright holder" (Q88088423). A bot added these from the
  template.
- The original file names are "enhanced_...png": these are processed copies, not scans as archived.
- The PikiWiki page for 128639 (https://www.pikiwiki.org.il/image/view/128639) shows the same photographer,
  uploader and source, and "licence type: public domain".

Two more files in the same batch (129771 and 129772) are from 1968, by other authors, with the same tag.

### Is it consistent? No.

- **The tag names the wrong person.** `PD-author` says the author released the work, and names Israel Preker. The
  page's own author field names Werner Braun. Nothing on the Commons or PikiWiki pages says Preker holds the
  copyright, or that Braun, his estate or the KKL released anything. **[V]**
- **Where the tag comes from.** PikiWiki's "free use" page (https://www.pikiwiki.org.il/content/23, read 14 October
  2026) describes the upload form's "public domain" option as covering two cases: the uploader owns the copyright and
  donates the picture, or the picture was already in the public domain to the best of the uploader's knowledge. The
  same page tells contributors that under the Copyright Act 2007 photographs taken fifty or more years ago are in the
  public domain. The site's terms (https://www.pikiwiki.org.il/content/9) have uploaders confirm they own the
  copyright, hold a licence, or that the content is public domain, and put responsibility for the content on the
  uploader. **[V, the project's own pages]**
- So the likely meaning of the tag is "old enough to be public domain in Israel", passed into a Commons template that
  says something else ("released by its author"). That is an inference from the project's rules. The pages do not
  state which case the uploader ticked. **[inferred]**

### Israeli law

- Copyright Act 2007, section 78(i): for a photograph made before the Act came into force, the identity of its
  author and its term stay under the previous law, section 21 of the Copyright Act 1911. Read in Hebrew on
  Wikisource (https://he.wikisource.org/wiki/%D7%97%D7%95%D7%A7_%D7%96%D7%9B%D7%95%D7%AA_%D7%99%D7%95%D7%A6%D7%A8%D7%99%D7%9D,
  revision of 1 September 2026). Section 77: the Act came into force six months after publication; Commons gives
  25 May 2008. **[V, statute text; Wikisource is a transcription, the official PDF on fs.knesset.gov.il was not
  fetched]**
- Copyright Act 1911, section 21: copyright in a photograph lasts fifty years from the making of the original
  negative, and the owner of the negative when it was made is deemed the author.
  https://www.legislation.gov.uk/ukpga/Geo5/1-2/46/enacted (read 14 October 2026). **[V]**
- So a negative made in June or July 1951 left copyright in Israel in 2001, or on 1 January 2002 on the Commons
  reading (`{{PD-Israel}}`: 1 January of the 51st year;
  https://commons.wikimedia.org/wiki/Commons:Copyright_rules_by_territory/Israel). Either way, expired in Israel.
  This assumes the photographs were taken in 1951 as the pages say; the dates are the uploader's. **[V on the rule,
  S on the dates]**
- **Change from the earlier note:** it looked to sections 34-35 of the 2007 Act for who owned the copyright. Section
  78(e) says sections 33 to 36 do not apply to works made before the Act; the 1911 Act does. Under its section 21 the
  "author" is whoever owned the negative, and under section 5(1)(a) a photograph ordered and paid for belongs to the
  person who ordered it unless agreed otherwise. Guy Raz ("Zionism in pictures: 120 years of photography at the
  Jewish National Fund", *Et-mol* 274, Yad Ben-Zvi, https://files.ybz.org.il/periodicals/Etmol/274/274.12.pdf) lists
  Braun among the KKL information departments' main photographers in 1948-1973, and the files came from the KKL
  archive. So the first owner may have been the KKL, not Braun. Which it was is **not established**. It does not
  change the term.

### United States

- The URAA restored US copyright in foreign works that, on the restoration date, were not public domain in their
  source country through expiry of term and were public domain in the US for failure of formalities or lack of
  eligibility. Restoration date for Berne and WTO members: 1 January 1996. A restored work gets the rest of the term
  it would have had: generally 95 years from first publication for works published before 1978. US Copyright Office,
  Circular 38B, https://www.copyright.gov/circs/circ38b.pdf (read 14 October 2026). **[V]**
- Israel's URAA date is 1 January 1996 (Commons territory page, above). **[S]**
- On 1 January 1996 a 1951 photograph was still in copyright in Israel (to 2001). If the other conditions hold, its
  US copyright was restored and would run to the end of 2046 if it was published in 1951. Whether and when these
  three photographs were first published, and where, I do not know. **[inferred; the publication facts are missing]**
- Werner Braun lived 12 June 1918 to 25 December 2018 (Wikidata Q18120775; English Wikipedia), so no life-based term
  has run out anywhere. **[S, Wikimedia]**

### Answer for the owner

- The tag "PD-author" is not supported by the pages it sits on. Read literally, it is wrong: the named person is the
  uploader, not the photographer.
- The photographs are very likely out of copyright in Israel, by age. That is the same position as a PD-Israel file.
- Nothing found gives them a US public-domain basis, and the URAA rules point the other way. This is not certain: it
  depends on publication facts nobody has recorded, and this note is not legal advice.
- Under `docs/CONTENT_POLICY.md` section 4 and `docs/event-images.md` (PD-Israel-only files are refused), the three
  files do not qualify as they stand. A written release from whoever holds the rights (the KKL archive first, since
  the files came from it; otherwise the Braun estate) would change that.
- This matches the earlier note's conclusion and adds the PikiWiki terms and the statute texts behind it.

## 4. Where this differs from the Phase 1 plan, PR 5

- The plan leans to Yeruham (ruling 4 assumes it is a candidate). Yeruham fails Task 5.0's criteria 2 and 3.
- Task 5.0 says a founding date "to the day from two independent sources (for example the Hebrew or English
  Wikipedia article and a ... page)". For Tifrah and Yeruham the two Wikipedias disagree with each other or with a
  third source. Only Mefalsim passes.
- Task 5.1's validation ("hex is land; hex owns no 1948 settlement") passes for Mefalsim but does not catch that its
  hex is an armistice line hex with Gaza Strip localities in it. If the owner picks Mefalsim, the plan needs a ruling
  on that, or a test that says it is allowed.
- Task 5.1's `founded` range (1948-05-15 to 1951-12-31) holds for all three.
- If Mefalsim is chosen, `content/sites.yaml` could cite the Gvati list and the Yad Ben-Zvi page for `founded:
  1949-06-12`. Both were read today.

## 5. Gazetteer check of the small villages near Mefalsim (16 October 2026, #654)

Section 1 left question 7 as "absence of evidence in Wikipedia, not a gazetteer check": places under 1,000
people near Mefalsim were not checked against a gazetteer. This section closes that gap for both sites.

### How it was done

Three sweeps, all read on 16 October 2026:

1. The en.wikipedia geosearch API, 10 km around 31.5019 N, 34.5644 E (the 1950 site), which returns every
   geo-tagged article in the radius; then a second 10 km sweep centred on 31.53 N, 34.52 E, about 5 km from
   the 1950 site toward Beit Hanoun - the direction Hebrew Wikipedia gives for the 1949 site's exposure to
   fire - to cover the site whose coordinates were never found.
2. *Village Statistics 1945*, the Mandate government's official land and population survey: the full Gaza
   Sub-District table (Table I, p. 45-46), read in the scan's text at archive.org, identifier
   `palestine-village-statistics-1945` (the Facts and Figures No. 34 edition with explanatory notes by Sami
   Hadawi, PLO Research Center, Beirut, September 1970, reproducing the Mandate tables). **[V]** This is the
   gazetteer the population threshold itself comes from.
3. The Survey of Palestine 1:100,000, Gaza, Sheet 9, second edition 1942 (GSGS 4079), read as a scan at
   maps.mapywig.org. The area from Gaza city east to Bureir and from Barbara south to Tell Abu Hureira was
   inspected; every named place in it was compared with the Village Statistics table. **[V]**

Distances use each village's en.wikipedia/Wikidata coordinates, recomputed by haversine against both centres.

### Result

Seven depopulated villages lie within 10 km of the 1950 site; the offset sweep covers six of them and adds
one more. 1945 populations are from Village Statistics 1945, Gaza Sub-District Table I **[V]**; the
settlement-on-land column is from the en.wikipedia articles and their citation chains (Village Statistics
1945; Khalidi 1992, quoted but not read page by page) **[S]**.

| Village | VS 1945 name, population | From the 1950 site | From the offset centre | Settlement named on its land | Names Mefalsim? |
|---|---|---|---|---|---|
| Huj | Hoj and Dorot: 810 Arabs, 230 Jews (counted together) | 5.6 km | 9.9 km | Havat Shikmim, on village lands | No |
| al-Muharraqa | Muharraqa, El: 580 | 5.9 km | 11.2 km | Yakhini (1950) | No |
| Najd | Najd: 620 | 6.3 km | 7.8 km | Sderot, Or HaNer | No |
| Dimra | Dimra: 520 | 6.3 km | 5.5 km | Erez | No |
| Dayr Sunayd | Deir Suneid: 730 | 8.1 km | 6.0 km | None; "the village's lands left undeveloped" | No |
| Kawfakha | Kaufakha: 500 | 9.6 km | 14.6 km | Nir Akiva (1953), southwest of the village site | No |
| Simsim | Sumsum: 1,290 Arabs, 70 Jews | 8.3 km | 9.2 km | Gvar'am (1942), on land traditionally belonging to the village | No |
| Bayt Jirja | Beit Jirja: 940 | 11.4 km | 9.96 km | None named in its article | No |

Simsim at 1,360 is over the 1,000 threshold (it is already in `map/localities_1948.yaml` as Sumsum). The
under-1,000 set is therefore seven villages: the four checked for #653, **Dayr Sunayd and Kawfakha, new this
pass**, and **Bayt Jirja**, which is outside 10 km of the 1950 site but inside the offset sweep, so it
belongs to the 1949 site's coverage. None of the seven names Mefalsim.

Completeness:

- The offset sweep (31.53 N, 34.52 E, 10 km) returns exactly Dimra, Dayr Sunayd, Najd, Hiribya, Simsim,
  Huj and Bayt Jirja among depopulated villages; Hiribya (2,240 Arabs and 60 Jews in VS 1945, at 8.8 km) is
  over the threshold. **[S]** (geosearch result; VS 1945 population)
- Every other Gaza Sub-District place under 1,000 in Village Statistics 1945 is farther than 10 km from the
  1950 site. Spot-checked coordinates: Hulayqat 13.7 km, al-Khisas 16.3 km, Hatta 23.6 km, Ibdis 23.3 km; the
  rest are the Julis-Iraq Suweidan-Qastina cluster, farther northeast. **[S]** (en.wikipedia coordinates)
- The 1942 sheet shows no other named village inside the sweep. Its remaining labels there are ruins ("Kh."),
  shrines ("Sh."), tells and bedouin land tracts ("Baikat ..."), none of which appear as villages in Village
  Statistics 1945. **[V]**
- Bedouin localities: Palestine Open Maps (secondary) marks three depopulated Bedouin localities within
  10 km of the 1950 site - Gatatweh (Tayaha, about 6.8 km), Urour (about 8.9 km) and Rawashdeh (about
  9.7 km), distances from its coordinates. **[S]** They fall in the Beersheba Sub-District, which Village
  Statistics 1945 counts only by tribe: its tribal table has no per-locality figures, and the Tayaha Tribes
  row lists the sub-tribes Qatatiwa, Rawashida and 'Urar among others. **[V]** The under-1,000 test therefore
  cannot be applied to them; the three names match the tribal-tract labels seen on the 1942 sheet. This is a
  gap the population threshold does not close. **[G, for the per-locality populations]**
- The 1949 site: still no coordinates **[G]** (searched: en.wikipedia, he.wikipedia, both geosearch sweeps).
  Hebrew Wikipedia describes only a kurkar hill facing the Gaza border, under fire from Beit Hanoun, later a
  firing range and an IDF post. The offset sweep stands in for it: with Bayt Jirja added, the 1942 sheet
  still shows no named village southeast of Beit Hanun toward the border, so the result holds for any
  plausible 1949 position.
- Nothing stands at Mefalsim's own position on the 1942 sheet: the land southeast of Beit Hanun between Najd
  and the border is open country with contour lines and land-tract labels only. **[V]**

Answer to #654: no gazetteer source places Mefalsim on any depopulated village's land. The nearest
settlement-on-village-land facts are Yakhini on al-Muharraqa's land (1950) and Or HaNer on Najd's (1957);
Mefalsim is on none. Whether the `first_new_yishuv.yaml` source comment should now say the under-1,000 check
is done is the content owner's edit, not this note's.

### Sources not reached

- Khalidi, *All That Remains* (1992): not read page by page. His 1992 site descriptions reached through the
  en.wikipedia articles that quote them. **[S]**
- Palestine Remembered: not used, per #654 ("lead only").
- The 1955 revision of the Gaza sheet (GSGS 4645), ANU openresearch, hdl.handle.net/1885/199041: the 1942
  edition was read instead, because it shows the pre-1948 village pattern.

## Open questions for the owner

1. Mefalsim despite the line hex, the 1950 move and the 7 October 2023 association? Or wait for a newspaper check
   that could settle Tifrah's day?
2. If Mefalsim: is the card's date the ceremony (12 June 1949, verified) or the advance party (8 June 1949, one
   source)?
3. Ruling 4: refuse the three Braun photographs as tagged, or ask the KKL archive for a written release?
4. Someone on an Israeli network or with a browser should read Davar for 12-14 June 1949 (Mefalsim), late September
   to mid October 1949 including 11 October (Tifrah) and 9-12 January 1951 (Yeruham) on the NLI press site. That
   would turn the single-source items here into primary-source ones.
