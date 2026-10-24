# The Yishuv-era policies and indicators: sourced basis and dates, 1882-1948

Researched 24 October 2026. Research note for issue #92 (Yishuv 13, the research part only, opened by the owner on 24
October 2026). Research only: nothing here is drafted for the game, no file in `content/` is changed, and the model
records stay paused until after the MVP. The policy values themselves (levels, defaults, effects) are design, not
research, and this note proposes none.

## Purpose

Give each policy and indicator in the design's table
([section 5.1 of the Yishuv design](../../docs/superpowers/specs/2026-10-10-era-yishuv-design.md), "New, windowed
`to: 1948-05-15`") a sourced historical basis and sourced dates: `land_purchase`, `hebrew_labour`,
`pioneer_selection`, `aliyah_bet`, `havlagah`, `cooperation_with_britain`, `hebrew_schools`, `voluntary_levy`,
`settlement_drive`, and the 1926-27 crisis behind `employment`, which the design marks *to verify*. The design's
`national_authority` indicator is not a separate section here; the friction it names (levy yield, dissidents) is in
sections 4, 6 and 8.

## Conventions

- [V] read this session in a primary document or a scholarly book, on the page image or in the text layer of an open
  scan (all listed under Sources). Where only the text layer was read, the entry says so. Sums were redone.
- [S] secondary only: Wikipedia, or an institutional reference page (USHMM, Jewish Virtual Library, a museum page).
  The page is named. A [S] value is a lead, not a settled fact.
- [C] two sources disagree. Every value is given, and the preferred one, if any, is named.
- [GAP] not found in any source read. No value is filled in by inference.
- [FTS] read only as a snippet in the Internet Archive full-text search, as defined in the national-funds note; a lead, not a value.
- **Derived** marks a number this note computed. The arithmetic is shown next to it.
- **My reading** marks an interpretation the source does not state itself.
- "p." is the printed page number; "PDF p." is the position in the file when it differs. Quotes are copied from the
  cited page. Where a printed dash between years was turned into a hyphen (repository policy), the quote says "dash
  adapted".
- Web pages were read through the fetch tool, which returns an extract made by a small model, not the raw page. Every
  quotation from a web page is therefore marked "as extracted" and needs a person's check against the live page. The
  scans (Survey, Mandate reports, Peel Report, Shafir) were read as text and, where marked "image", as page images.
- **Every page opened for this note was opened by an agent on 24 October 2026; a person's check is pending.**

## Sources

| Key | Source | Where read |
|---|---|---|
| Survey I, II | *A Survey of Palestine*, prepared December 1945 to January 1946 for the Anglo-American Committee of Inquiry, Vol. I and Vol. II (Government of Palestine). Printed page = PDF page minus 5 in Vol. I; the offset in Vol. II changes by chapter, so both numbers are given | https://www.bjpa.org/content/upload/bjpa/a_su/A%20SURVEY%20OF%20PALESTINE%20DEC%201945-JAN%201946%20VOL%20I.pdf and the same with `VOL%20II` |
| AR 1927, 1936, 1937, 1938 | *Report by His Majesty's Government ... to the Council of the League of Nations on the Administration of Palestine and Trans-Jordan* for the year named | https://archive.org/details/report-admin-palestine-1927 (and `-1936`, `-1937`, `-1938`); text from the `_djvu.txt` file of each |
| Peel | *Palestine Royal Commission Report*, Cmd. 5479 (July 1937) | https://unispal.un.org/pdfs/Cmd5479.pdf (PDF p. = printed p. + 11 in the chapters read) |
| Shafir | Gershon Shafir, *Land, Labor and the Origins of the Israeli-Palestinian Conflict, 1882-1914* (Cambridge University Press 1989; University of California Press paperback 1996). Pages as printed ("Page 60") in a third-party scan of the 1996 paperback | https://yplus.ps/wp-content/uploads/2021/01/Shafir-Gershon-Land-Labor-and-the-Origins-of-the-Israeli-Palestinian-Conflict-1882-1914.pdf |
| White Paper 1939 | the Statement of Policy of May 1939 (Cmd. 6019, as the Survey itself names it), read in two places: Survey I, Annexure II, pages 97-98 (PDF p. 102-103), and the Avalon Project text | https://avalon.law.yale.edu/20th_century/brwh1939.asp |
| USHMM Aliyah Bet | United States Holocaust Memorial Museum, Holocaust Encyclopedia, "Aliyah Bet" | https://encyclopedia.ushmm.org/content/en/article/aliyah-bet |
| JVL Aliyah Bet | Jewish Virtual Library, "Immigration to Israel: Aliyah Bet (1939 - 1948)" | https://www.jewishvirtuallibrary.org/aliyah-bet-1939-1948 |
| Daat Irgun | Daat (Herzog College) English pages "The Illegal Immigration of the Irgun" and "Restraint and Retaliation" (no author or date line); Yehuda Lapidut, "The Black Sabbath" (Lapidut was an Irgun member; the page cites the Jabotinsky Institute) | https://daat.ac.il/etzel/english/ac04.htm ; https://daat.ac.il/etzel/english/ac03.htm ; https://daat.ac.il/DAAT/english/history/lapidot/16.htm |
| en.wiki Aliyah Bet | English Wikipedia, "Aliyah Bet" (current revision, read 24 October 2026) | https://en.wikipedia.org/wiki/Aliyah_Bet |
| he.wiki Haapala | Hebrew Wikipedia, "ההעפלה" (first 100,000 characters only) | https://he.wikipedia.org/wiki/ההעפלה |
| en.wiki Havlagah | English Wikipedia, "Havlagah" (tagged "needs more citations") | https://en.wikipedia.org/wiki/Havlagah |
| en.wiki Tower and Stockade | English Wikipedia, "Tower and Stockade" | https://en.wikipedia.org/wiki/Tower_and_Stockade |
| JVL Tel Amal | Jewish Virtual Library, "Tel Amal Stockade & Tower Museum" (text "Copyright 2000 Gems in Israel", no author) | https://www.jewishvirtuallibrary.org/jsource/History/stockade1.html |
| en.wiki Community ransom | English Wikipedia, "Community ransom" (Kofer HaYishuv) | https://en.wikipedia.org/wiki/Community_ransom |
| Srugim | Yair Klein, "היום בהיסטוריה", Srugim, 24 July 2022 | https://www.srugim.co.il/693584-היום-בהיסטוריה-כופר-הישוב-גיוס-ההמוני |

Opened and not usable: the National Library of Israel page "Illegal Immigration" (HTTP 403), the Jewish Agency's
Ben-Gurion archive page on the White Paper motto (redirect, then 404), the Jewish Virtual Library "He-Halutz" page
(navigation only, no article text), the Avalon 1930 White Paper address I guessed (404).

Not reachable or not opened, so cited nowhere as read: Dalia Ofer, *Escaping the Holocaust* (Oxford 1990; the search
returned only catalogue pages), Yehuda Bauer or Halamish on Aliyah Bet, Barnett 1993 (the book behind the English
Wikipedia Kofer date), Kessler 2023 (behind the Havlagah page), the Palestine Gazette, and the Hope-Simpson Report of
1930.

## Already on main (not repeated)

- Jewish population, immigration by period and Jewish emigration by period 1920 to 14 May 1948:
  [`population-1882-1948.md`](population-1882-1948.md), sections 1g, 4 and 5b. Its Bachi series gives Jewish emigration
  at 6,218 a year in 1926-27 (12,436 over the two years, derived there) against 2,094 a year in 1924-25.
- Jewish National Fund land held by year (196,700 dunums in 1927, 463,500 in 1939, 758,200 in 1944), JNF land spending,
  the Jewish Agency and Keren Hayesod budgets and the first dating of Kofer HaYishuv:
  [`national-funds-1882-1948.md`](national-funds-1882-1948.md), sections 1, 2 and 3.
- Founding dates of the tower-and-stockade settlements and of the eleven points of 5 to 6 October 1946:
  [`settlements-pre-1948-valleys.md`](settlements-pre-1948-valleys.md),
  [`settlements-pre-1948-galilee.md`](settlements-pre-1948-galilee.md) and
  [`settlements-pre-1948-negev.md`](settlements-pre-1948-negev.md) (section 3 there).
- Office holders for the offices the design links to each policy (`ja_settlement_head`, `ja_labour_head`,
  `ja_immigration_head`, `haganah_command_head`, `ja_treasurer`): [`offices-1882-1948.md`](offices-1882-1948.md).

---

## 1. `employment` and the crisis of 1926-27

The design's wording is "the 1926-27 crisis, when emigration passed immigration (*to verify*)". It is verified for
1927 by the Mandate's own report, and it is a one-year event, not a two-year one.

| Item | Value | Tag | Source |
|---|---|---|---|
| Cause given by the Administration | "Financial depression contracted considerably the economic capacity of the country to absorb new arrivals" | [V] image | AR 1927, p. 67, "XIV.-Immigration and Labour" |
| What was allowed in 1927 | certificates "only to the wives and young children of residents, to children entering schools in Palestine, and to men of means"; as labour immigrants "only 250 men and 250 women, all young relatives of residents" | [V] image | AR 1927, pp. 67-68 |
| The crossing | the limitation "caused women immigrants to outnumber the men, and also, for the first time since 1920, emigrants to outnumber immigrants" | [V] image | AR 1927, p. 68 |
| Immigrants 1926, all races / Jews | 13,910 / 13,081 | [V] image | AR 1927, p. 68 |
| Immigrants 1927, all races / Jews | 3,595 / 2,713 (including 1,220 travellers allowed to settle, of whom 705 Jews) | [V] image | AR 1927, p. 68 |
| Emigrants 1927 | 2,274 residents from before 1 July 1920 (610 Jews) and 4,704 settlers since (4,431 Jews, 233 Christians, 40 Moslems) | [V] image | AR 1927, p. 68 |
| Emigrants 1926 | 1,694 residents from before 1 July 1920 (413 Jews) and 7,735 settlers since (6,952 Jews, 635 Christians, 148 Moslems) | [V] image | AR 1927, p. 68 |
| Jewish emigrants, derived | 1927: 610 + 4,431 = 5,041, against 2,713 Jewish immigrants. 1926: 413 + 6,952 = 7,365, against 13,081 | derived | the two rows above |
| Of the Jewish emigrants of 1927 | "4,431 were post-war settlers, including about 2,500 who had arrived in Palestine less than two years previously and for the most part returned to Russia and Poland"; "many of the younger men sailed to America and various British dominions" | [V] image | AR 1927, p. 68 |
| Jewish unemployment | "affected a population of not less than 5,000" (end of the year relief works began to shrink the numbers "and the exodus of workers also had its effect") | [V] image / text | AR 1927, p. 68 (the figure) and p. 69 (the continuation, text layer) |
| Seasonal non-Jewish unemployment | 1,500 to 2,000 | [V] image | AR 1927, p. 68 |
| Government relief works | an accelerated public-works programme "at an aggregate cost of £P.66,000", which "afforded employment for 840 Jews for an average period of four months" | [V] text | AR 1927, p. 94 (the page number is the OCR's header; the page image was not viewed) |
| Jewish Agency relief | "an average of £P.7,000 monthly in unemployment relief to workless Jews throughout the year. No unemployed Jew became a direct charge on public funds" | [V] text | AR 1927, p. 96 (as above) |
| Jewish labour force | "26,500 Jewish working men and women in Palestine; 70 per cent. are members of the General Federation"; building-trade wages down "as much as 30 per cent." | [V] text | AR 1927, p. 69 |
| Bachi, yearly net emigration | 1924-25: 2,094; 1926-27: 6,218; 1928-31: 1,575 | second-hand [V]: read in the population note, not re-opened | population note, section 5b |

- **The Survey's own table has the same two years with OCR slips.** Survey I, Chapter VII, Table 1 (p. 185, PDF p. 190)
  reads 1926 as "18,910" and 1927 as "8,595" in its text layer. The Jews, Christians and Moslems columns of the same
  rows sum to 13,910 (13,081 + 611 + 218) and 3,595 (2,713 + 758 + 124), which are the figures AR 1927 prints. The
  text layer is wrong there; the Survey's Jewish columns (13,081 and 2,713) agree with the Mandate report.
- **The lead-up** is the wave of 1924 and 1925. Same table, Jews: 12,856 (1924), 33,801 (1925), 13,081 (1926), 2,713
  (1927), 2,178 (1928) [V, text layer].
- **[C] The scale of the unemployment.** AR 1927 says "not less than 5,000" Jews unemployed out of 26,500 workers
  (derived: about 19 per cent of that workforce, if all 5,000 were workers). No second count was found. [GAP] for a
  peak month, and for the Jewish Agency's or the Histadrut's own count.
- **My reading, for the design:** "emigration passed immigration" is true of the calendar year 1927 for Jews and for all
  races, and of 1926 only in the sense that Jewish emigration was high (7,365 against 13,081). The two-year average of
  Bachi (6,218 a year against 15,794 Jewish immigrants over the two years, derived) does not show emigration above
  immigration for 1926-27 as a pair. Bachi's own ratio for 1926-27 is 73.6 emigrants per 100 immigrants; the 78.7 that 12,436 over 15,794 gives (derived) differs because Bachi's immigrant base is not the Mandate's registered count, which this note did not check.

## 2. `land_purchase`

| Item | Value | Tag | Source |
|---|---|---|---|
| JNF land, 1927 to 1944 | 196,700 (1927), 369,800 (1936), 463,500 (1939), 758,200 (1944) dunums | [V] (Survey I p. 376 re-checked by the independent reviewer; the 1936 figure is misread "1986" by the text layer) | national-funds note, section 2a (Survey I, Table 4, p. 376) |
| White Paper of 1939, land | "the High Commissioner will be given general powers to prohibit and regulate transfers of land. These powers will date from the publication of this statement of policy and the High Commissioner will retain them throughout the transitional period"; it also says "no restriction has been imposed hitherto on the transfer of land from Arabs to Jews" | [V] | Survey I, Annexure II, para 16, p. 98 (PDF p. 103) |
| Statement of policy issued | 17 May 1939 | [V] | Survey I, chronology, p. 52 (PDF p. 57) |
| Order in Council | the Palestine (Amendment) Order-in-Council of 25 May 1939 added Article 16D: the High Commissioner "may make regulations prohibiting, restricting or regulating transfers of land"; regulations may take effect "from any date not being earlier than the 18th day of May, 1939" | [V] | Survey I, ch. VIII, paras 88, p. 260-261 (PDF p. 265-266) |
| The Land Transfers Regulations | made in February 1940, published 28 February 1940 | [V] | Survey I, ch. VIII para 89, p. 261; chronology p. 58 (PDF p. 63) |
| The three zones | Zone A: transfer "save to a Palestinian Arab is prohibited" (with listed exceptions); Zone B: transfer by a Palestinian Arab "save to a Palestinian Arab" prohibited unless the High Commissioner approves at his "unfettered discretion"; a "free" zone with no restriction | [V] | Survey I, ch. VIII para 89, p. 261 (PDF p. 266) |
| Zone areas | A 16,680, B 8,348, free 1,292 square kilometres, total 26,320 (16,680 + 8,348 + 1,292 = 26,320, derived) | [V] | Survey I, ch. VIII para 90, p. 261 |
| Reaction | a general Jewish strike on 29 February 1940, followed by a week of demonstrations that "were organized by the Histadruth and received the support of all Jewish parties, except the Revisionists who stood aside" | [V] | Survey I, chronology, pp. 58-59 (PDF p. 63-64) |

Transfers from Arabs to non-Arabs that the High Commissioner approved, in dunums, by year from February to February
(Survey I, ch. VIII para 92, p. 263, PDF p. 268; [V] text layer, both rows sum to the printed totals):

| Zone | 1940-41 | 1941-42 | 1942-43 | 1943-44 | 1944-45 | 1945-46 (first half) | Total |
|---|---|---|---|---|---|---|---|
| A | 318 | 333 | 890 | 600 | 215 | 158 | 2,514 |
| B | 4,494 | 5,860 | 293 | 45 | 34 | 151 | 10,877 |

- Check, derived: row A 318 + 333 + 890 + 600 + 215 + 158 = 2,514; row B 4,494 + 5,860 + 293 + 45 + 34 + 151 = 10,877.
- In zone A, of the 2,514 dunums: 946 were exchanges, 924 sales "necessary for the consolidation of existing Jewish
  holdings", 604 transactions begun before the Regulations, 40 to a cemetery board and the Latin Patriarchate (paras
  93, p. 263-264). In zone B, of the 10,877: 1,430 exchanges, 3,101 consolidation sales, 6,346 earlier transactions
  (para 94, p. 264).
- Registrations at the Land Registries (approved transfers plus court processes) are a second table on the same
  page, zone A: 5,641, 8,539, 7,868, 790 and 832 dunums for 1941-42 to the first half of 1945-46; zone B: 710, 729, 334,
  884 and nil. The Survey says the two tables differ because court processes fall outside the Regulations and
  approved transfers are not all completed.
- **Before 1940 there was no legal brake, so there is no "policy level" to source** for 1882 to 1939. What the pace of
  purchase was is the JNF series on main and the land-held table. [GAP] for private-purchase flows by year, for the
  Land Transfer Ordinance of 1920 and its 1933 amendment (named in Survey I, ch. VIII, but not read for this note), and
  for the Hope-Simpson Report's land findings.
- [GAP] JNF land in 1948 (also a gap in the national-funds note; open under #91).

## 3. `settlement_drive`

| Item | Value | Tag | Source |
|---|---|---|---|
| First tower-and-stockade settlement | Tel Amal, 10 December 1936. The English Wikipedia list puts Kfar Hittim (7 December 1936) first but notes it "Doesn't use traditional Tower and Stockade method" | [S] | JVL Tel Amal ("This is an exact replica of the first Stockade and Tower settlement, Tel Amal, which saw its beginnings on December 10, 1936", as extracted); en.wiki Tower and Stockade (same date, as extracted) |
| Count | "57 settlements established between 1936-1939" | [S] | JVL Tel Amal (as extracted); en.wiki Tower and Stockade: "57 were constructed between the last days of 1936 and October 1939", citing Rotbard (2003, p. 57) |
| [C] the count and the end date | the English Wikipedia list itself runs to She'ar Yashuv in February 1940 and has 55 entries by the extractor's count; a search snippet said a 1986 Israeli state medal refers to 55 settlements | [C] [S] | en.wiki Tower and Stockade (as extracted); the medal claim is from a web-search snippet, not opened |
| Contemporary Mandate count, 1937 | "Seventeen new agricultural settlements located in the Beisan sub-district, the Jordan Valley and the Maritime Plain were established by the Jewish Agency in 1937 on Jewish National Fund lands", covering "35,000 dunums, of which 20,000 dunums were placed under cultivation", planned "to accommodate 1,200 families" and holding "about 1,340 persons" | [V] text | AR 1937, pp. 51-52 (the OCR splits the passage at the page break) |
| Contemporary Mandate count, 1938 | "Nine workers' settlements were established in 1938, making a total of 29 new agricultural settlements established since January, 1936. Most of these settlements were established in the northern district" | [V] text | AR 1938, p. 135 (the OCR's header; page image not viewed) |
| Method and permission | built in one night, on JNF land, on the Ottoman rule that "buildings erected in one night didn't require a building permit"; "the British generally gave their tacit accord" | [S] | en.wiki Tower and Stockade (as extracted; the legal claim is the page's, not checked against a statute) |
| Cost and who paid | [GAP] no source read says | | |
| The eleven points | the night of 5 to 6 October 1946, after the Morrison-Grady plan would have put the Negev in an Arab state | [S] | web-search summary of en.wiki "11 points in the Negev" (page not opened); the dating is worked through in the Negev settlements note |

- **[C] Mandate counts against the later count.** The Mandate reports count 29 new agricultural settlements for January 1936 to
  December 1938 and 17 settlements in 1937 alone; the "57" is a count of tower-and-stockade settlements to 1939. The two
  are not the same population (the Report calls only the nine of 1938 workers' settlements, and says 29 "new agricultural settlements" since January 1936), so they are not in conflict;
  but nothing read reconciles them either. The design should not take 57 as a Mandate figure.
- The settlement notes on main carry the day-level founding dates from Gvati and the Central Bureau of Statistics. They
  are not repeated here. They mark some founding dates as [C] already.
- **Link to security.** The design says `settlement_drive` raises `security`. The sourced hook is the Survey's account
  that the "events of 1936 brought into being a new police formation", the Jewish Settlement Police (section 6 below).

## 4. `voluntary_levy` (Kofer HaYishuv and the emergency taxes)

The national-funds note holds the figures (section 3 there). This section adds the date disagreement and the contemporary
wording.

| Item | Value | Tag | Source |
|---|---|---|---|
| Date the Vaad Leumi acted | "בכ"ה בתמוז תרצ"ח (24.7.38) החליטו חברי הוועד הלאומי מכל המפלגות על הקמת קופה ארצית למימון הגנת היישוב" (as extracted) | [S] | Srugim, Yair Klein, 24 July 2022 |
| [C] the same, in English | "formally introduced on 24 June 1938" | [C] [S] | en.wiki Community ransom (as extracted), citing Barnett 1993, p. 304 (book not opened) |
| Contemporary description | the Redemption Fund (Kofer Hayishuv), "which was instituted under the auspices of the Vaad Leumi to provide funds to assist in the care of Jewish outlying settlements"; collections "were actively opposed by the Revisionists, on the grounds that members of their organization had not been allowed to co-operate in the measures taken for the defence of Jewish settlements" | [V] text | AR 1938, p. 33, para 50 |
| Who proposed the name | David Remez | [S] | Srugim (as extracted) |
| How it was raised | indirect taxes "on imported goods, cigarettes, oil, wine, radios and public transport" (Srugim, as extracted); "a tax on luxuries, cigarettes, restaurant bills and so on" (en.wiki, as extracted) | [S] | both |
| Voluntary or a levy | en.wiki: "a tax imposed by the Jewish National Council" and "formally introduced on 24 June 1938" (as extracted); Srugim: "המיִסים הוטלו בצורה עקיפה על סחורות מיובאות", receipts "ניתנו בבולים שהודבקו או כתווים שקיבל העוסק בפנקסים", and of the separate "כופר נפש" project, "אחד הגילויים המרגשים להתנדבות היישוב היהודי לשלם מס מרצון" (as extracted; the voluntary phrase is about that project, and the page is a popular article). The two pages thus call it a tax, collected indirectly, with voluntary payment stressed on the Hebrew side | [S] | en.wiki Community ransom; Srugim (both as extracted, 24 October 2026). [GAP] for a primary description |
| Emergency tax and mobilisation appeal | Srugim says that "during and after the Second World War direct taxes were imposed on the Jewish community", named the emergency tax and the mobilisation and rescue campaign | [S] | Srugim (as extracted). The 1940 and 1942 years come from he.wiki, in the national-funds note, section 3 |
| Income | 152,000 £P (5699, 1938/39) and 57,000 £P (5700, 1939/40) | [FTS] (a search-index snippet only; see the national-funds note) | national-funds note, section 3 |

- **My reading:** 24 July 1938 is the better-supported date (a Hebrew source naming the Hebrew date and the decision-maker,
  and the existing note's Davar report of 25 July). The English Wikipedia's 24 June may be a one-month slip, but that is a
  guess, not a finding. Both values stay.
- [GAP] primary text of the Vaad Leumi's decision, the Mandate's own mention of the 1940 and 1942 direct taxes, and any
  yield after 5700 (open under #91).

## 5. `hebrew_labour`

Neutral wording: the sources describe an aim, a measured outcome, and the objections of the people affected. They are
quoted as they stand.

| Item | Value | Tag | Source |
|---|---|---|---|
| Origin of the slogan | in 1905 workers set up the Hapoel Hatzair party; "the 'conquest of labor' (kibush haavoda)" carries "at least three meanings": self-conquest, class conflict over wages, and the third, "the 'taking away of the work in the moshavot from the Arab workers and transferring it into the hands of the Jewish workers'" | [V] | Shafir, p. 60 |
| The masthead | Hapoel Hatzair's periodical carried the line "A necessary condition for the realization of Zionism is the conquest of all branches of work in Eretz Israel by Jews"; a later page of the same book (PDF p. 170) says the slogan was modified in October 1908, so this is the earlier wording (the later wording was not copied) | [V] | Shafir, p. 60 |
| Shafir's verdict for 1905-14 | "all skilled labor in the plantations was monopolized by Jewish workers"; the second question, whether the workers won wages for a European standard of living, "must be negative" | [V] | Shafir, p. 72 |
| The Histadrut | formed in 1920 | [V] text | Survey II, ch. XVII, p. 759 (PDF p. 235): "before the Histadruth was formed in 1920" |
| Size, 1927 | "26,500 Jewish working men and women in Palestine; 70 per cent. are members of the General Federation" | [V] text | AR 1927, p. 69 |
| Size, 1 January 1945 | 143,000 adults and 8,500 youths, "75% of all Jewish wage and salary earners", whom the Survey estimates at 152,000 | [V] text | Survey II, ch. XVII para 39, p. 757 (PDF p. 233) |
| Who stayed outside | the Revisionists' National Labour Organization, the Mizrahi workers' organisation and the Agudat Israel workers' organisation "have formed their own independent labour unions" | [V] text | Survey II, ch. XVII para 40, p. 757 |
| Orange groves, 1935 | "In February, 1935, an enquiry showed that in the Jewish-owned orange groves 40 per cent. of the labour was Jewish and 60 per cent. Arab. The position is now reversed and 60 per cent. of the labour is Jewish and 40 per cent. Arab" (Peel, 1937) | [V] image | Peel, ch. IX para 62, p. 240 (PDF p. 251) |
| Same paragraph | "the Jews, in their policy of creating an agricultural Jewish population, have restricted the employment of Arab labour on lands held by them" | [V] image | Peel, ch. IX para 62, p. 240 |
| Picketing | "a movement to intimidate those Jewish farmers who employ Arab labour. Picketing in the post-war settlements in the Sharon was on such a scale as to require legislation - the Prevention of Intimidation Ordinance of 1927 (amended in 1936)" | [V] text | Peel, ch. IX para 62, p. 241 (PDF p. 252) |
| Peel's effect finding | "Though this policy has not increased the number of landless Arabs, it has reduced the field of employment for labourers and the means of livelihood of those cultivators who depend on work outside their holdings to obtain an adequate income." | [V] text | Peel, ch. IX para 62, p. 241 |
| Listed as an Arab grievance | "the refusal of Jews to employ Arab labourers" | [V] text | Peel, ch. XIX para 8, p. 365 (PDF p. 376) |
| Royal Commission's finding | "Owing to Jewish development and enterprise the employment of Arab labour has increased in urban areas, particularly in the ports" | [V] text | Peel, ch. V para 32, p. 129 (PDF p. 140) |
| Histadrut and Arab workers | "The Histadruth's attempt to form an Arab labour organization under its own direction has, on the whole, worsened rather than improved relations"; the Palestine Labour League "has now declined in influence and membership" | [V] text | Survey II, ch. XVII para 71, pp. 766-767 (PDF p. 242-243; the quoted sentence is on p. 767) |
| Government public works | the Government set in 1933 that "from 30 to 33 per cent. of the labour employed on public works should be Jewish", calculated on wages; the Jewish share of public revenue was accepted at 37 per cent and of the population at about 18 per cent | [V] text | Peel, ch. XIII paras 7-8, p. 320 (PDF p. 331). The Survey (II, ch. XVII para 100, p. 774, PDF p. 250) says the 1942-45 Jewish share was "still less than the 30-33 percent proposed by Government for 1933/34" |

- **What the sources do and do not give.** They give the aim (Shafir, from the Hapoel Hatzair line), a measured result for
  one sector in one year pair (oranges, 1935 and 1937), the organisation's size, and Mandate and Royal Commission
  statements. [GAP] a Histadrut or Jewish Agency policy text from the 1920s or 1930s, any series of Jewish-labour share
  in agriculture by year, and Shafir's coverage of 1914 to 1948 (the book ends in 1914). Hope-Simpson (1930) and Peel's
  Chapter X were not read for this section.
- [C] none found on the dates; the sources agree on 1905 and 1920.

## 6. `havlagah`

Neutral wording: a policy of restraint by the Yishuv's institutions, and a split with the groups that rejected it.

| Item | Value | Tag | Source |
|---|---|---|---|
| What it was | "the strategic policy of the Yishuv during the 1936-1939 Arab revolt"; adopted by the Haganah "as an official operational strategy" | [S] | en.wiki Havlagah (as extracted; the page is tagged "needs more citations"; its own sources are Kessler 2023 and a 2001 newspaper item, not opened) |
| The Jewish Agency's policy | "The Jewish Agency adopted a passive defence policy, based on 'havlaga' (restraint)"; the Irgun held that attack was the best defence | [S] | Daat Irgun, "Restraint and Retaliation" (as extracted; no author) |
| Contemporary British description, 1938 | "During the first five months of the year the Jews showed restraint and there were few acts of retaliation, but from the end of June the Jewish attitude in all sections of the community underwent a change" after the conviction of a Revisionist youth, Shlomo Ben Yoseph, "who had fired on an Arab bus"; the death sentence "was carried out on 29th June" | [V] text | Survey I, chronology 1938, p. 45 (PDF p. 50) |
| Same passage | "On 6th and 25th July bomb explosions in the Arab fruit market at Haifa caused the death of 74 Arabs and injury to 129 others" | [V] text | Survey I, p. 45 |
| [C] the Haifa market of 6 July 1938 | one Irgun page says the 6 July bomb "killed 21 Arabs and injured more than 50"; the Survey's 74 dead is for the bombs of 6 and 25 July together | [C] [S] | Daat Irgun (as extracted) against Survey I, p. 45 |
| AR 1937 and 1938 | "as in 1937, there were isolated incidents of Jewish reprisals" in 1938; the AR 1938 Jewish affairs part says the Revisionists showed "no improvement" | [V] text | AR 1938, p. 6, para 10; AR 1938, p. 33, para 50 |
| Royal Commission, 1937 | "throughout the whole series of outbreaks, and under very great provocation, they have shown a notable capacity for discipline and self-restraint" | [V] text | Peel, ch. V, p. 121 (PDF p. 132) |
| Irgun break, 1937 | Jabotinsky's cable of 30 April 1937, "if the riots are renewed and there is a tendency to attack Jews as well, do not hold back", is called "the sign that 'havlaga', the policy of restraint, was over"; 14 November 1937 is the day on which, the page says, the Irgun's history marks the end of havlagah; the Tehomi group rejoined the Haganah in May 1937 | [S] | Daat Irgun, "Restraint and Retaliation" (as extracted) |
| [C] when the Irgun broke it | July 1937 and 1938 were each named by a search summary; the Daat page gives 30 April and 14 November 1937 | [C] [S] | not opened (search snippets); Daat page as above |
| The Jewish Settlement Police, from 1936 | "The events of 1936 brought into being a new police formation ... the Jewish Settlement Police"; the scheme of supernumerary police ran "until the 6th December, 1938"; "the high water mark of active paid supernumeraries of classes A, B and C was reached in 1937 when the number employed totalled 2,700"; category A was paid half by the Government and half by the Jewish Agency | [V] text | Survey II, ch. on public security, paras 17-18, p. 590 (PDF p. 64) |
| The Haganah in the Survey's words | "At no time, however, has there been official recognition of this illegal organization", though "selected units were provided and trained by British officers", "such as Major (later Major-General) Wingate's special night squads" | [V] text | Survey II, p. 600 (PDF p. 74) |
| End of the policy window | the White Paper of 17 May 1939 | [V] | Survey I, p. 52 |

- **My reading:** the design's "1936-39" is the contemporary British frame; the Yishuv's own institutions did not all drop
  restraint in 1939 (a stricter reading would split the policy from the groups that left it in 1937-38). The Survey's
  1938 text puts a visible change at the end of June 1938. The start month of 1936 is not in any source read:
  [GAP] for the first public statement of the policy (the Arab revolt began in April 1936 by the general account; no
  page was read that states it).
- [GAP] a primary Yishuv-side text (a Jewish Agency or Haganah order or speech) for the policy; Kessler 2023 and Slutsky
  were not opened.

## 7. `cooperation_with_britain` (1939 and 1945)

| Item | Value | Tag | Source |
|---|---|---|---|
| Sept 1939: the appeal | after the outbreak of war the "Jewish Agency issued an appeal calling on all Jews in Palestine to close their ranks and offer their full assistance to Great Britain" | [V] text | Survey I, chronology, p. 56 (PDF p. 61) |
| Sept 1939: the register | "A Jewish national register of volunteers was opened and over 134,000 Jews between the ages of 18 and 40 enrolled for public service" | [V] text | Survey I, p. 57 (PDF p. 62) |
| Sept 1939: the condition | volunteers offered "with the qualification that they be recruited as Jews in recognised Jewish units for service in Palestine. The offer, with its qualification, was not accepted" | [V] text | Survey I, p. 57 |
| Enlistment through the Agency | "The Jewish recruits were enlisted through the Jewish Agency, but this arrangement was temporarily dropped when the Agency objected to the employment of Jews on military duties outside the Middle East and to the formation of mixed units of Arabs and Jews" | [V] text | Survey I, p. 57 |
| 1939: the quota | on 13 July 1939 the quota for October 1939 to March 1940 "would be suspended", and a 24-hour Jewish general strike followed | [V] text | Survey I, p. 56 |
| Feb 1940: the land regulations | after the Land Transfers Regulations "the opposition of Jewish leaders to recruitment of Jews in the mixed Arab/Jew pioneer corps units hardened and the demand for a Jewish Legion ... became stronger" | [V] text | Survey I, p. 59 (PDF p. 64) |
| Sept 1940: Jewish companies | "The intention to create separate Arab and Jewish companies of a British infantry regiment (the Buffs) ... was announced. Recruits for the Jewish companies came forward immediately" | [V] text | Survey I, p. 60 (PDF p. 65) |
| Sept 1944: the Brigade | "The decision of His Majesty's Government to form a Jewish Brigade Group, announced in September" (the year is the Survey's 1944 section) | [V] text | Survey I, p. 74 (PDF p. 79) |
| Late 1944: Jewish leaders and the Irgun and Stern group | "pressure was brought to bear on the terrorist groups and the Revisionist party by the Jewish leaders through use of the Hagana; a number of Revisionists were abducted, and towards the end of the year the terrorist activities of the Stern group and the Irgun were curbed by threat of open warfare" | [V] text | Survey I, p. 71 (PDF p. 76) |
| Oct 1945: the break | on 31 October 1945 "a concerted series of attacks was made by armed Jews on the Palestine railway system"; the Survey says they "were probably the combined work of the Hagana, Irgun and Stern Group" | [V] text | Survey I, p. 76 (PDF p. 81) |
| Oct 1945: the movement | the British seized "the agreement between the Haganah, Irgun and Lehi" among Jewish Agency documents on 29 June 1946; the page does not date the agreement or the movement | [S] | Daat, Lapidut, "The Black Sabbath" (page re-read by the reviewer; no 1945 date on it) |
| Ben-Gurion's slogan | [GAP] the Jewish Agency archive page named in a search result could not be opened; the exact words and date are not confirmed | [GAP] | |

- The "1939 and 1945" pair in the design is supported as two turns: cooperation proclaimed in September 1939 (with a
  condition the Government refused), and a coordinated resistance from October 1945. The 1945 hinge date in this note's
  sources is 31 October 1945 (Survey) (the founding date of the joint Resistance Movement is [GAP]; no page read dates it).
- [GAP] the primary text of the October 1945 agreement, the date the Jewish Resistance Movement ended (the search
  summaries gave July and August 1946), and Ben-Gurion's slogan.

## 8. `aliyah_bet`

The owner's frame is: from 1934, the 1942-44 pause, the 1945-48 surge.

| Item | Value | Tag | Source |
|---|---|---|---|
| First large sea voyage | 1934, about 350 passengers on a hired ship (Vallos or Velos), "without the permission of the heads of the Jewish Agency"; the Haganah helped them ashore | [S] | Daat Irgun, "The Illegal Immigration of the Irgun" (as extracted) |
| Landing date | "the boat reached the shore on August 25" | [S] | same page |
| [C] organiser | "on the initiative of activists of the Hehalutz movement in Poland" (Daat page) against "organised by the Hakibbutz Hameuchad movement, together with the Haganah" (he.wiki Haapala, as extracted) | [C] [S] | the two pages |
| [C] date and port | August 1934 (Daat); a search-result snippet of a Hebrew page said July 1934 and another genealogy page said sailing from Varna on 6 September 1934 | [C] [S] | snippets only, not opened |
| The Mossad LeAliyah Bet | "established by the Jewish leadership in Palestine"; began coordinating in 1938 | [S] | USHMM Aliyah Bet (as extracted) |
| Voyages, 1937 to 1944 | "62 such voyages were carried out from 1937 to 1944" | [S] | USHMM Aliyah Bet (as extracted) |
| [C] voyages in all | "142 voyages by 120 ships" (en.wiki, 1934-48, as extracted); "141 voyages", 98 of them by the Mossad and the Haganah (he.wiki, as extracted) | [C] [S] | the two Wikipedias |
| British count of illegal entry, to April 1939 | "of the order of 30,000 to 40,000" Jewish illegal immigrants since 1920; "The Jewish Agency indeed add 37,000" | [V] text | Survey I, ch. VIII para 54, p. 210 (PDF p. 215) |
| The same, April 1939 to 1945 | "of the order of 20,000 to 25,000"; total in the population "between 50,000 and 60,000"; recorded in 1945: "370" | [V] text | Survey I, para 54, p. 210 |
| White Paper rule | illegal immigrants "who ... may succeed in coming into the country and cannot be deported will be deducted from the yearly quotas" | [V] | Survey I, Annexure II, para 14(4), p. 98 |
| Pre-war peak, 1939 | arrivals of 5 February 1939 (the "Artemisia") and "over 1,700 more" in the five weeks after 25 March 1939 | [V] text | Survey I, p. 51 (PDF p. 56) |
| Account, 1 April 1939 to 31 March 1944 | legal 31,221; illegal 19,965; total 51,186 against 75,000; balance 23,814 | [V] image | Survey I, ch. VII para 38, p. 180 (PDF p. 185) |
| Account, to 31 December 1945 | legal 54,727; illegal 20,304 (provisional); total 75,031; "the provision for Jewish immigration under the White Paper was exhausted by the end of 1945" | [V] text; sum redone | Survey I, ch. VII para 45, p. 183 (PDF p. 188) |
| Check, derived | 31,221 + 19,965 = 51,186; 75,000 - 51,186 = 23,814; 54,727 + 20,304 = 75,031 | derived | |

**The 1942-44 pause.** What the sources show:

- No source read states a "pause". What they show is a thin flow and the sinking of ships.
  - 1939-40: ships still arrived; on 1 and 3 November 1940 the "Pacific" and "Milos" were intercepted with 1,771 on board
    and the Government announced on 20 November that they, and later arrivals, would be deported; the "Patria" was
    scuttled in Haifa on 25 November 1940 "with loss of life to 252 Jewish illegal immigrants and British police
    personnel"; 793 arrived on the "Darien" in March 1941 and were detained in Palestine [V text, Survey I, pp. 60-61,
    PDF p. 65-66]. The 1,350 deportees held in Mauritius "were permitted to return to Palestine" in September 1945 [V
    text, Survey I, p. 80 (PDF p. 85) and p. 81].
  - Late 1942: the British Government authorised visas for Jews arriving in Turkey; "6,027 immigrants (4,912 adults and
    1,115 children) received facilities to proceed to Palestine as immigrants" [V image, Survey I, p. 180, para 36].
  - [S] the "Struma" (sank February 1942, between 767 and 791 dead by one count) and the "Mefkure" (sunk in August 1944)
    are named by en.wiki Aliyah Bet (as extracted; the figures cite Aroni 2007, no page).
  - [S] he.wiki Haapala (as extracted): "about 17 thousand immigrants in 25 ships" by sea in the war years, and 23
    ships with about 14,000 at the start of the war (1939). That page's own sources were not in the part read.
  - USHMM says Aliyah Bet "continued, but at a slower pace and under more dangerous conditions, with hundreds of lives
    lost at sea" [S, as extracted].
- [GAP] by-year sea arrivals for 1942, 1943 and 1944. The Survey's Table 1 (p. 185) has Jewish immigrant totals by year
  but its 1942 to 1944 rows do not sum in the text layer, so they are not used here.
- [C] the legal-immigration table of the Survey against the Jewish count: the Survey's account is of the Government's
  count; the Jewish Agency's own count is only "37,000" for the stock at April 1939.

**The 1945-48 surge.**

| Item | Value | Tag | Source |
|---|---|---|---|
| Revival | "A revival of the illegal immigration traffic began in the autumn" of 1945; "the Jewish official organizations in Palestine and the Jewish public as a whole" determined "to promote it by all means in their power" | [V] text | Survey I, chronology, p. 81 (PDF p. 86) |
| 1945 to 1948, ships and people | "Sixty-six illegal immigration sailings carrying 70,000 immigrants were organized during these years" | [S] | JVL Aliyah Bet (as extracted; the page names no source) |
| [C] the same | "well over 100,000 people had taken this route, including more than 70,000 Holocaust survivors" by 1948 (USHMM); about 84,000 in all and more than 70,000 on Palyam-commanded ships (he.wiki); "approximately 110,000" for 1939-48 (JVL) | [C] [S] | the three pages (all as extracted) |
| Interception | "over 90 percent of the ships were intercepted by the British navy" | [S] | USHMM Aliyah Bet (as extracted) |
| Cyprus | "over 50,000 Jewish refugees in internment camps on Cyprus" by 1948 (USHMM); "over 52,000" sent to the camps (he.wiki) | [C] [S] | the two pages (as extracted) |
| Voyage of 1947 | "In 1947, 4,500 immigrants on the Exodus were sent back to Europe by the mandatory government" | [S] | JVL Aliyah Bet (as extracted) |

- **My reading:** the design can take three dated turns from sourced material: 1934 (first large sea voyage; [C] on who ran
  it), 1939 (the White Paper's quota and the deduction rule; the Survey's counts), and autumn 1945 (the revival). The
  quantities for 1945-48 are all [S] and differ by up to 14,000 (70,000 against 84,000). [GAP] a scholarly count
  (Halamish, Ofer, Bauer) for any period.

## 9. `pioneer_selection`

| Item | Value | Tag | Source |
|---|---|---|---|
| Category C, labour | immigration of "persons who have a definite prospect of employment in Palestine", proved by an employer's guarantee of "a minimum period of one year"; "the Executive of the Jewish Agency is a kind of conglomerate employer of labour" | [V] text | Survey I, ch. VII para 17, p. 171 (PDF p. 176) |
| The labour schedule | "prepared once in six months" laying down "the maximum number of persons by sex, trades, industries and callings"; the High Commissioner approved it | [V] text | Survey I, para 18, p. 171 |
| The Agency's share | "The greater part of the schedule was in fact assigned to the Jewish Agency", with "special conditions ... concerned, for the most part, with the ages, sex and conjugal condition of the immigrants and their degree of skill, training and competence"; a small departmental reserve was kept | [V] text | Survey I, para 19, pp. 171-172 (PDF p. 176-177) |
| The selection step | certificates "issued in instalments to the Jewish Agency who then allocated them to the several Zionist Palestine offices in the world to be granted to selected immigrants" | [V] text | Survey I, para 20, p. 172 (PDF p. 177) |
| The size | set by the High Commissioner after "the request by the Jewish Agency" and the immigration authority's own survey, "a rough survey of Arab unemployment", and the White Paper of 1930 as read by MacDonald's letter of 13 February 1931 | [V] text | Survey I, para 20, p. 172 |
| Labour schedules, certificates for the Jewish Agency and departmental reserve | April-September 1933: 5,500; October 1933-March 1934: 5,500; April-September 1934: 6,200; October 1934-March 1935: 7,500; April-September 1935: 8,000; October 1935-March 1936: 3,250; April-September 1936: 4,500; October 1936-March 1937: 1,800 | [V] text | AR 1936, p. 76, para 16 (the OCR's header; page image not viewed) |
| Conditions applied in 1936 | by age: 4,390 at 18-35, 340 at 18-45, 1,570 "any age above 18" | [V] text | AR 1936, p. 76, para 17 |
| The 1936 cut | the schedule due in 1936 was "fixed at 1,800 certificates" after the Government decided to "take a conservative view of the economic absorptive capacity"; the Arab Higher Committee cited it on 13 November 1936 | [V] text | AR 1936, pp. 35-36, paras 71-72 (the 13 November letter runs across the page break; OCR headers, page image not viewed) |
| White Paper of 1939 | a quota of 10,000 a year for five years plus 25,000 refugees, "special consideration being given to refugee children and dependants" | [V] | Survey I, Annexure II, para 14(1), p. 97 |
| Categories | A capitalists (£P1,000 in the 1930s), B maintenance assured, B(ii) religious occupation, B(iii) students, C labour, D dependants | [V] text | Survey I, ch. VII paras 13-21, pp. 170-172; the £1,000 figure also in AR 1936, p. 35 |

- **The "pioneer" part is not in the Mandate documents.** They say "selected immigrants" and give age, sex, skill and
  training conditions; they do not name the pioneer movements. [GAP] the Zionist side: Hehalutz's role, hachshara
  (agricultural training) as a route to a certificate, and the Palestine Offices' selection rules. The Jewish Virtual
  Library page for He-Halutz had no article text and a search for the Encyclopaedia Judaica entry returned only
  memoir fragments, none opened.
- The design's phrase "share of labour certificates kept for trained pioneers, against family and rescue cases" has a
  sourced frame in three parts: Category C (labour) against Category D (dependants) and the 25,000 refugees of 1939.
  The *share* of C that went to pioneers, as opposed to other labour immigrants, is [GAP].

## 10. `hebrew_schools`

| Item | Value | Tag | Source |
|---|---|---|---|
| Before 1914 | Jewish bodies (the Hilfsverein, the Alliance Israelite, the Anglo-Jewish Association) used the languages of their countries; "in all 'settlement' schools, of which most were maintained by the Jewish Colonisation Association (PICA), Hebrew was the medium of instruction" | [V] text | Survey II, ch. XVI para 36, p. 635 (PDF p. 109) |
| 1914 | "the Zionist Education Council (Va'ad ha-Hinnukh) was formed, and took over the control of 12 schools. This number had increased by 1918 to 40" | [V] text | Survey II, para 36, p. 635 |
| Dual system | "Since 1920 a dual system of national education has gradually developed, formed on a linguistic and racial basis" | [V] text | Survey II, para 38, p. 636 (PDF p. 110) |
| Government support, 1926 | the schools were "at first private and unassisted from public funds", then "per capita grants amounting altogether to about £P.2,000 a year"; "In 1926, however, Government formulated the principle that a larger sum might properly be assigned to these schools"; the sum was set at £P.20,000 a year for five years, as a block grant to the Palestine Zionist Executive after per-capita grants | [V] text | Survey II, paras 52-53, p. 644 (PDF p. 118) |
| Pupils, 1925/26 | 17,174 Jewish pupils against 19,737 in Government schools | [V] text | Survey II, para 53, p. 644 |
| AR 1927 grant | "a grant of £20,000 to the schools of the Jewish Agency for the scholastic year 1927-28" | [V] text | AR 1927, p. 94 |
| Devolution | "devolved as from the beginning of the school year 1932/33 upon the Jewish Community of Palestine"; the Va'ad Leumi took the system over "in October, 1932" | [V] text | Survey II, para 79, p. 655 (PDF p. 129) and para 98, p. 670 (PDF p. 144) |
| Education Code | laid down by the Va'ad Leumi in 1933, revised in 1940 | [V] text | Survey II, para 98, p. 670 |
| Three trends | General, Mizrahi, Labour: July 1944, 39,673 (54 per cent), 16,731 (23) and 16,729 (23); total 73,133 | [V] text; sums redone | Survey II, para 99, p. 670 |
| Scale, 1943/44 | the Va'ad Leumi department controlled "483 schools with over 73,000 pupils, being roughly three-quarters of all Jewish pupils in that year" | [V] text | Survey II, para 80, p. 655 |
| Whole Jewish school population, July 1944 | Hebrew public elementary 61,938; private elementary and secondary 36,053; total 97,991; plus 1,504 Jewish pupils in Arab private schools = 99,495 | [V] text; sums redone | Survey II, Table 1, p. 637 (PDF p. 111) |
| Royal Commission, 1937 | "Practically every Jewish boy and girl attends a primary school"; "In more than two cases out of three, the school, whether primary or secondary, is a Jewish school, the instruction is in Hebrew" | [V] text | Peel, ch. V para 12, p. 118 (PDF p. 129) |

- Check, derived: 39,673 + 16,731 + 16,729 = 73,133 and 61,938 + 36,053 = 97,991, both as printed; the text layer misreads
  3 as 8 in both rows (39,678 and 36,058), and the page images (read at 300 dpi) sum exactly. 97,991 + 1,504 = 99,495.
- [GAP] the Hebrew-language decision of the Ottoman period (the language dispute of 1913-14) and the Mandate's 1922
  recognition of Hebrew as an official language, neither read for this note; the Peel Report mentions an official-
  language grievance (Chapter XIX, para 8) but that passage does not date the decision.
- [GAP] pupil series by year for 1920 to 1940 (only 1925/26 and July 1944 were read).

## 11. What the design can take, and what it cannot yet

| Design item | Sourced dates and anchors (this note) | Still open |
|---|---|---|
| `employment` | the 1927 reversal (AR 1927 p. 68); unemployment of at least 5,000 | a monthly peak; the Jewish Agency count |
| `land_purchase` | 17 May 1939; 25 May 1939; 28 February 1940; zones; approved dunums by year | private purchase by year; Land Transfer Ordinance of 1920 |
| `hebrew_labour` | 1905 slogan; 1920 Histadrut; 1935 and 1937 orange-grove shares; the 1927 picketing ordinance | a policy text; a share series; 1914-48 scholarship |
| `pioneer_selection` | Category C, six-month schedules; 1933-37 schedule sizes | the pioneer share; Hehalutz's role |
| `aliyah_bet` | 1934; 1938 (Mossad); White Paper deduction; account to 1944 and 1945; autumn 1945 | by-year 1942-44 arrivals; one scholarly 1945-48 count |
| `havlagah` | 1936 Settlement Police; 29 June 1938 hinge; 1937 Irgun break (secondary) | the 1936 start text; Yishuv-side primary |
| `cooperation_with_britain` | September 1939 appeal and 134,000 volunteers; September 1944 Brigade; October 1945 | the slogan; the October 1945 agreement |
| `hebrew_schools` | 1914 and 1918 school counts; 1932 devolution; July 1944 pupils | pupil series by year |
| `voluntary_levy` | 24 July 1938 [C] 24 June; the 1938 report's wording | the 1940 and 1942 taxes in a primary source |
| `settlement_drive` | 10 December 1936; 17 settlements in 1937; 29 new agricultural settlements since January 1936 (AR 1938) | the 57 against 55 count; cost |

## 12. Gaps for a person

1. Page images for the entries marked "text" (the Survey and Mandate reports were read as text layers; only AR 1927 p.
   68, Peel p. 240 and Survey I p. 180 were checked on the page image). The Survey's OCR mistook 3 for 8 in two places
   (section 1).
2. The Wikipedia, Jewish Virtual Library, USHMM, Daat and Srugim pages were read as extracts; every quotation marked
   "as extracted" needs a check against the live page.
3. Scholarly books not opened: Halamish, Ofer, Bauer (Aliyah Bet); Kessler (havlagah); Barnett (Kofer date); Rotbard
   (the count of 57); Slutsky (the Haganah and the levies).
4. The Ben-Gurion slogan of September 1939, the October 1945 Resistance Movement agreement, and the Hehalutz selection
   rules have no source read.
5. Hope-Simpson (1930) and Peel's Chapter X were not read; they are the likely primary sources for land and Hebrew
   labour.
