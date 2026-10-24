# M3 content research: 1950–1951 facts (extension to 31 Dec 1951)

Researched 2026-10-10. All URLs were checked on that date unless noted.

**Conventions**
- **[V]** means verified on the cited page, or extracted directly from the CBS PDF text.
- **[S]** means single source or secondary only. Treat it as "probably", not "fact-checked".
- **[C]** means sources conflict. Every version is given, each with its source.
- **[GAP]** means not found.
- Ids in `backticks` follow the existing `content/*.yaml` ids. New ids are proposals only.
- No quotes are invented. The few short quotes below are copied from the cited page.

**Access notes**
- knesset.gov.il (both the history pages and main.knesset.gov.il) sits behind a bot wall and returned an empty JS shell. Knesset-sourced facts below are therefore taken through Wikipedia (en and he), IDI and JVL, which cite the Knesset site.
- CBS PDFs could only be read by fetching them and then extracting the raw text streams locally. The CBS numbers below come from those extracts.

---

## 1. Governments in office, 1950–51

### 1a. First Government (`first_government`): end
- **Coalition (unchanged until the end):** Mapai, United Religious Front, Progressive, Sephardim and Oriental Communities, Democratic List of Nazareth. [V] https://en.wikipedia.org/wiki/First_government_of_Israel
- **Ministers:** No changes to ministers before the end are recorded on the en.wiki page. The lineup is the same as in `people.yaml` (Ben-Gurion PM+Defence; Kaplan Finance+Trade & Industry; Sharett FA; Shapira Health+Immigration+Interior; Rosen Justice; Golda Meir Labour; Sheetrit Police; Maimon Religions+War Victims; Remez Transportation; Levin Welfare; Dov Yosef Agriculture+Rationing & Supply; Shazar Education). [V] same page.
- **Resignation:** Ben-Gurion resigned on **15 Oct 1950**. [V] en.wiki First government; https://en.wikipedia.org/wiki/1950_in_Israel. [C] Some pages (e.g. the en.wiki Mizrachi party article) give **16 Oct 1950**.
- **End date:** [C] The en.wiki infobox gives **30 Oct 1950** as the dissolution date. The successor government was sworn in on **1 Nov 1950**. The Rationing ministry article ends Dov Yosef's term on **1 Nov 1950**.
  - Recommendation: First Government `to: 1950-11-01`, using the existing handover convention `[from, to)`.
- **Causes of the October 1950 crisis:**
  - **Supply and Rationing.** Ben-Gurion wanted to close the Ministry of Supply and Rationing. The en.wiki Rationing-minister page says the government's collapse was "partially due to an internal crisis in Mapai over the ministry's liquidation". [V] https://en.wikipedia.org/wiki/Rationing_and_Supply_Minister_of_Israel
  - **Trade and Industry.** Ben-Gurion wanted a businessman (Ya'akov Geri, not an MK) as Minister of Trade and Industry, taking that portfolio from Kaplan. Under the law of the time, adding a minister required the government to resign and win a new confidence vote. The law was amended afterwards. [V] he.wiki First Knesset https://he.wikipedia.org/wiki/הכנסת_הראשונה
  - **United Religious Front demands.** The URF demanded the ministry's closure and the businessman appointment. [S, uncited on page] https://en.wikipedia.org/wiki/United_Religious_Front
  - **Education in the immigrant camps.** This was a background cause (see the Frumkin crisis below). [V] en.wiki First government.
  - **Geri as a minor factor.** A contemporary JTA-wire piece calls Geri's appointment "one of the minor factors" in the crisis, and says religious circles wanted one of their own in the post. [S] Detroit Jewish News, 17 Nov 1950: https://digital.bentley.umich.edu/djnews/djn.1950.11.17.001/20/download_text
- **Sequence 15 Oct – 1 Nov 1950** [V] https://en.wikipedia.org/wiki/1950_in_Israel:
  - 15 Oct: Ben-Gurion resigns. Weizmann asks him to form a new government.
  - 17 Oct: Ben-Gurion presents a minority government, which fails to win Knesset approval. he.wiki Second Government adds that his Mapai + Sephardim attempt had only 52 supporters.
  - 19 Oct: Weizmann asks **Pinchas Rosen** (Progressive) to form a government.
  - 29 Oct: Rosen returns the mandate. he.wiki says Rosen rebuilt the same coalition and Ben-Gurion then presented it.
  - 1 Nov: The Second Government is approved **69–42, 2 abstentions**.

### 1b. The January 1950 immigrant-camp education crisis (Frumkin Commission)
- **Background:** Under the "uniform education" in immigrant camps, children were placed under the Culture Department, so parents had no choice of school stream. Religious Yemenite parents reported coercion, including the cutting of sidelocks. MK David-Zvi Pinkas (Mizrachi) filed a parliamentary question in **December 1949**. [V] https://he.wikipedia.org/wiki/ועדת_פרומקין
- **Commission:** Appointed in January 1950 by decision of Ben-Gurion.
  - [S] One search snippet gives **17 Jan 1950** as the appointment date, but no page carrying it could be confirmed.
  - [V] he.wiki gives the first session as **25 Jan (1950)**.
- **Members** [V] he.wiki:
  - Judge Gad Frumkin (chair)
  - Yitzhak Ben-Zvi (Mapai MK)
  - Avraham Elmaliah (Sephardim MK)
  - Rabbi Avraham Haim Shag (URF MK)
  - Rabbi Kalman Kahana (URF MK)
- **Report submitted:** **9 May 1950**. [V] he.wiki
- **Findings** [V] he.wiki:
  - There was no government intention of anti-religious coercion.
  - Handing camp education to the Culture Department was a serious error.
  - Sidelock-cutting was a practice, not isolated cases.
  - Direct responsibility lay with department head Nahum Levin. Education Minister Shazar was not held personally responsible.
  - en.wiki (Gad Frumkin) summarises that the uniform system "resulted in the harsh infringement of the new immigrants' freedom of religion". https://en.wikipedia.org/wiki/Gad_Frumkin
- **Consequences:**
  - Camp education was moved to the Education Ministry's education department. Officials were reassigned. [V] he.wiki
  - Shazar was replaced by Remez when the Second Government formed. [V] he.wiki says "replaced". [C] en.wiki Gad Frumkin says the findings "led to the resignation of Education Minister Zalman Shazar and the fall of Israel's first government".
- **March 1950:** The URF threatened to leave the coalition over religious services in the camps, and Mapai failed to bring Mapam into the coalition. [V, undated within the month] https://en.wikipedia.org/wiki/1950_in_Israel
- **29 Nov 1950:** A new Mapai–religious dispute broke out over education in the camps. This one led to the Feb 1951 fall. [V] same page.

### 1c. Second Government (proposed id `second_government`)
- **Dates:**
  - `from: 1950-11-01` [V] en.wiki and he.wiki.
  - Resigned on **14 Feb 1951** and continued as a transitional government until **8 Oct 1951**. [V] https://en.wikipedia.org/wiki/Second_government_of_Israel ; https://he.wikipedia.org/wiki/ממשלת_ישראל_השנייה
  - Recommendation: `to: 1951-10-08`, and add a flag for "transitional from 14 Feb 1951".
- **Coalition:** Mapai, United Religious Front, Progressive Party, Sephardim and Oriental Communities, Democratic List of Nazareth. That is the same as the First Government, with **73 MKs**. [V] en.wiki and he.wiki
- **Investiture vote:** 69–42–2. [V] 1950 in Israel
- **Ministers.** All terms run 1 Nov 1950 → 8 Oct 1951 unless noted. Sources: en.wiki and he.wiki Second Government.

| Person (id) | Office(s) | Party (list) | Dates / notes |
|---|---|---|---|
| David Ben-Gurion (`ben_gurion`) | PM; Defence | Mapai | Also **Education and Culture from 19 May 1951** after Remez's death |
| Eliezer Kaplan (`kaplan`) | Finance | Mapai | Lost Trade and Industry |
| Moshe Sharett (`sharett`) | Foreign Affairs | Mapai | |
| Haim-Moshe Shapira (`shapira`) | Health; Immigration; Interior | United Religious Front | [S] en.wiki lists all three. The he.wiki extract did not clearly show Interior and Immigration. |
| Pinchas Rosen (`rosen`) | Justice | Progressive | |
| Golda Meir (`golda_meir`) | Labour (and Social Insurance) | Mapai | |
| Bechor-Shalom Sheetrit (`sheetrit`) | Police | Sephardim and Oriental Communities | |
| Yehuda Leib Maimon (`maimon`) | Religions; War Victims | URF | [C] Resignation date unclear, see 1d |
| Yitzhak-Meir Levin (`levin`) | Welfare | URF | |
| David Remez (`remez`) | **Education and Culture** | Mapai | 1 Nov 1950 → **19 May 1951** (died in office) |
| Dov Yosef (`dov_yosef`) | **Transportation** | Mapai | Moved from Agriculture and Rationing. The Rationing ministry was abolished. |
| Pinhas Lavon (new `lavon`) | **Agriculture** | Mapai | |
| Ya'akov Geri (new `geri`) | **Trade and Industry** | [C] en.wiki: no party, "not a Knesset member". he.wiki lists him under Mapai. | Not an MK |
| Yosef Efrati (new) | Deputy Minister of Agriculture | Mapai | **26 Feb 1951** → 8 Oct 1951 (he.wiki) |
| Reuven Shari (new) | Deputy Minister of Transportation | Mapai | **1 Apr 1951** → 8 Oct 1951 (he.wiki) |

- **Zalman Shazar** (`shazar`): the education term ends **1 Nov 1950**. He was not in the Second Government. [V]
- **Dov Yosef:** the Agriculture and Rationing terms end **1 Nov 1950**. [V] Rationing-minister page.

### 1d. February 1951 fall of the Second Government
- **Resignation:** **14 Feb 1951**. [V] en.wiki and he.wiki Second Government; he.wiki First Knesset.
- **Formal cause:** The Knesset rejected Education Minister David Remez's proposals on the registration of schoolchildren (in the immigrant camps). The dispute was over whether the Histadrut's labour stream could run religious education for immigrant children. [V] he.wiki and en.wiki Second Government.
- **Vote count:** [S] The only figure found is a contemporary JTA-wire report of a **49–42** vote "on the question of the education of children in immigrant camps", with Mizrachi voting against Mapai together with Mapam, Herut and others. https://digital.bentley.umich.edu/djnews/djn.1951.02.23.001/4 . Not cross-checked against Knesset minutes.
- **Maimon:** [C]
  - he.wiki: Minister Maimon resigned after the Knesset vote, and Ben-Gurion treated that as a loss of confidence.
  - en.wiki "1951 in Israel" lists Maimon resigning on **4 Jan 1951** "over the education crisis in the ma'abarot" (no footnote).
- **Informal cause** [V] he.wiki Second Government:
  - The **14 Nov 1950 local elections** saw Mapai fall and the General Zionists win roughly a quarter of the vote. https://en.wikipedia.org/wiki/1950_in_Israel
  - Ben-Gurion read this as a sign that the Knesset no longer reflected public opinion, and preferred early elections. Sharett's diary is cited for this.
  - he.wiki on austerity gives the margin as General Zionists about 3% behind Mapai. https://he.wikipedia.org/wiki/תקופת_הצנע

### 1e. Third Government (proposed id `third_government`)
- **Dates:** `from: 1951-10-08`, ends 24 Dec 1952 (resigned 19 Dec 1952). It runs past the M3 window. [V] https://en.wikipedia.org/wiki/Third_government_of_Israel ; https://he.wikipedia.org/wiki/ממשלת_ישראל_השלישית
- **Investiture vote:** **56–40**. [S] he.wiki, citing Haaretz 9 Oct 1951.
- **Coalition (65 seats per he.wiki):**
  - Mapai 45
  - Hapoel HaMizrachi 8
  - Agudat Yisrael 3
  - Democratic List for Israeli Arabs 3
  - Mizrachi 2
  - Poalei Agudat Yisrael 2
  - Progress and Work 1
  - Agriculture and Development 1
  - The three minority lists were affiliated with Mapai and held no ministers.
  - **Out of the coalition:** Progressive Party and General Zionists.
- **Ministers on 8 Oct 1951.** Only one change falls within 1951 (Kahana, 4 Nov 1951).

| Person (id) | Office(s) | Party | Notes |
|---|---|---|---|
| David Ben-Gurion | PM; Defence | Mapai | |
| Eliezer Kaplan | Finance; also Deputy PM | Mapai | Finance until 25 Jun 1952; died 13 Jul 1952 (outside window) |
| Levi Eshkol (new `eshkol`) | Agriculture | Mapai | Until 25 Jun 1952 |
| Moshe Sharett | Foreign Affairs | Mapai | |
| Haim-Moshe Shapira | Interior; Religions | Hapoel HaMizrachi | Party changed: the URF dissolved in 1951 |
| Ben-Zion Dinur (new `dinur`) | Education and Culture | Non-MK; former Mapai MK | |
| Golda Meir | Labour | Mapai | |
| Bechor-Shalom Sheetrit | Police | [C] en.wiki 3rd Government says **Mapai**. He was elected in 1949 for the Sephardim list. | Check whether he moved to Mapai for the 1951 election |
| Dov Yosef | **Trade and Industry** and **Justice** | Mapai | Justice until 25 Jun 1952 (en.wiki). he.wiki lists only Trade and Industry clearly. |
| Yosef Burg (new `burg`) | [C] **Health** per en.wiki 3rd Government and en.wiki Yosef Burg (https://en.wikipedia.org/wiki/Yosef_Burg). he.wiki's table shows him as Post. | Hapoel HaMizrachi | The Postal ministry went to Nurock only on 3 Nov 1952 (https://en.wikipedia.org/wiki/Mordechai_Nurock), so Health is the likely 1951 portfolio. |
| David-Zvi Pinkas (new `pinkas`) | Transportation | Mizrachi | Died 14 Aug 1952 |
| Yitzhak-Meir Levin | Welfare | Agudat Yisrael | Left Sep 1952 |
| Peretz Naftali (new) | Minister without Portfolio | Mapai | Until 25 Jun 1952 |
| Kalman Kahana (new) | Deputy Minister of Education | Poalei Agudat Yisrael | From **4 Nov 1951** (he.wiki) |

- **Leaving office on 8 Oct 1951 (not in the Third Government):** Pinchas Rosen (Progressive, now in opposition), Pinhas Lavon, Ya'akov Geri, Maimon.
  - Lavon later became Minister without Portfolio from 17 Aug 1952 (outside window).
  - Remez's terms end with his death on 19 May 1951. Ben-Gurion holds Education 19 May → 8 Oct 1951.
- **Proposed new office ids:**
  - `education_minister` already exists.
  - Deputy ministers would need new office ids, or can be skipped.
  - `postal_minister` is not needed before Nov 1952.
  - `rationing_minister` ends 1 Nov 1950. The offices file should mark it defunct from that date.

---

## 2. Knesset

- **First Knesset (`first_knesset`) end:**
  - It continued until the Second Knesset convened. Term: 14 Feb 1949 → **20 Aug 1951**. [V] he.wiki https://he.wikipedia.org/wiki/הכנסת_הראשונה
  - **Self-dissolution, not the Feb 1951 resignation, set the election.** The government resigned on 14 Feb 1951. In **April 1951** the Knesset passed the **Transition to the Second Knesset Law 5711-1951**, the **Second Knesset Elections Law**, and a law fixing the election day as **30 Jul 1951**. [V] he.wiki First Knesset (month only).
  - [S] One page (hamichlol.org.il, "חוק יסוד: הכנסת") dates the Transition Law to **12 Apr 1951**, with the election laws about a week later: https://www.hamichlol.org.il/חוק_יסוד:_הכנסת
  - IDI classes 1951 as a dissolution "by a Law to Dissolve the Knesset". https://www.idi.org.il/articles/38887
  - **Reason:** The Feb 1951 education crisis (see 1d) and Ben-Gurion's choice of elections over a new coalition. [V] he.wiki; IDI 1951 page.
  - So if the game needs a "dissolution" event, the trigger is **14 Feb 1951** (government resignation) and the law is **April 1951**. "February 1951 dissolution" is imprecise.
- **Speaker (First and Second Knessets):** **Yosef Sprinzak** (Mapai), 14 Feb 1949 → 28 Jan 1959, continuously. [V] https://en.wikipedia.org/wiki/Yosef_Sprinzak ; he.wiki First and Second Knesset.
  - Deputy speakers of the First Knesset included Yosef Burg (URF) and Nahum Nir (Mapam). [V] he.wiki.
- **Election:** **30 Jul 1951**. [V] https://en.wikipedia.org/wiki/1951_Israeli_legislative_election ; https://en.idi.org.il/israeli-elections-and-parties/elections/1951/ ; https://www.jewishvirtuallibrary.org/elections-to-the-2nd-knesset-july-1951
  - Eligible voters: **924,885**
  - Turnout: **75.1%**
  - Total votes: 695,007
  - Valid votes: **687,492**
  - Threshold: **1%** (6,874 votes per JVL)
  - Votes per seat: 5,692 (JVL)
- **Seats.** Total **120**. Identical in en.wiki, IDI and JVL. [V]

| Party | Votes | % | Seats | id |
|---|---|---|---|---|
| Mapai | 256,456 | 37.3 | **45** | `mapai` |
| General Zionists | 111,394 | 16.2 | **20** | `general_zionists` |
| Mapam | 86,095 | 12.5 | **15** | `mapam` |
| Hapoel HaMizrachi | 46,347 | 6.8 | **8** | `hapoel_hamizrachi` |
| Herut | 45,651 | 6.6 | **8** | `herut` |
| Maki | 27,334 | 4.0 | **5** | `maki` |
| Progressive Party | 22,171 | 3.2 | **4** | `progressive` |
| Democratic List for Israeli Arabs | 16,370 | 2.4 | **3** | new `democratic_list_israeli_arabs` |
| Agudat Yisrael | 13,799 | 2.0 | **3** | `agudat_yisrael` |
| Sephardim and Oriental Communities | 12,002 | 1.8 | **2** | `sephardim` |
| Poalei Agudat Yisrael | 11,194 | 1.6 | **2** | `poalei_agudat_yisrael` |
| Mizrachi | 10,383 | 1.5 | **2** | `mizrachi` |
| Progress and Work | 8,067 | 1.2 | **1** | new `progress_and_work` |
| Yemenite Association | 7,965 | 1.2 | **1** | `yemenite_association` |
| Agriculture and Development | 7,851 | 1.1 | **1** | new `agriculture_and_development` |
| Sepharadim–Ashkenazim Unity | 4,038 | 0.6 | 0 | |
| For New Immigrants and Freed Soldiers | 375 | 0.05 | 0 | |

- **Notes on the results:**
  - The URF did not run as one list. It split into four lists: Hapoel HaMizrachi, Mizrachi, Agudat Yisrael and Poalei Agudat Yisrael, with 15 seats combined (IDI).
  - The three minority lists were Mapai-affiliated (JVL footnote).
  - he.wiki Second Knesset shows a "composition" (General Zionists 23, Mapai 47 …) that reflects **later mergers and splits**, not the election result. Use the table above for the convening date.
- **Convened:** **20 Aug 1951** (sworn in). [S] he.wiki https://he.wikipedia.org/wiki/הכנסת_השנייה . Not confirmed elsewhere.
- **Opposition leader in the game sense:** en.wiki infoboxes give **Meir Ya'ari** (Mapam) for the Second Government, and Peretz Bernstein (General Zionists) for the Third. [S]

---

## 3. President

- **Chaim Weizmann** was in office throughout 1950–51. His first term was tied to the First Knesset's term.
- **Re-elected 19 Nov 1951** by the Second Knesset, **85 for, 11 against, 3 blank**, with 21 MKs not voting. He was the only candidate. [V] https://en.wikipedia.org/wiki/1951_Israeli_presidential_election ; https://en.wikipedia.org/wiki/1951_in_Israel
- **Sworn in at his Rehovot residence on 25 Nov 1951.** [S] Search summary attributing the date to the IDI presidential-elections article https://en.idi.org.il/articles/34583 . Not confirmed by direct read.
- **Why a new election:** The early dissolution of the First Knesset forced it. [V] en.wiki 1951 presidential election.
- Weizmann died 9 Nov 1952 (outside window). [V] en.wiki Third Government infobox.

---

## 4. Aliyah

### 4a. Annual immigration (CBS)

| Period | Total | Asia | Africa | Europe | America & Oceania | Unknown |
|---|---|---|---|---|---|---|
| 1950 | **170,563** | 57,565 | 26,162 | 81,195 | 1,954 | 3,687 |
| 1951 | **175,279** | 103,396 | 20,382 | 47,074 | 1,286 | 3,141 |
| (1952, for reference) | 24,610 | 6,867 | 10,286 | 6,232 | 950 | 275 |

- **Source:** CBS table reproduced at https://jewishvirtuallibrary.org/jsource/Immigration/Immigration_by_Period_and_Continent.html (CBS source line http://www1.cbs.gov.il/hodaot2015n/21_15_129t2.pdf). [V]
- **Parsing warning:** The JVL table is right-to-left and the row label sits *after* the data. A WebFetch summariser mis-shifted the rows, giving "1950 = 175,279". The figures above come from parsing the raw HTML. The row sums check exactly (57,565+26,162+81,195+1,954+3,687 = 170,563, and so on), and they match the existing 1948 and 1949 rows in `aliyah.yaml`.
- **Totals cross-checks:**
  - https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year : 1950 = 170,563; 1951 = 175,279. [V]
  - CBS, *Immigration to Israel 2002–2006* (Publication 1365, 2009), Table 1 (1948–2006): 1950 = 170,215 Jews … total **170,563**; 1951 total **175,279**. [V] https://www.cbs.gov.il/he/publications/DocLib/2009/1365/pdf/e_print.pdf
- **1948–51 first wave:** **687,624** total. 48% Europe, 34% Asia, 14% Africa. Two-thirds of the Europeans came from Poland and Romania. Iraq supplied 52% of the Asian arrivals. [V] same CBS publication.

### 4b. Immigrants 15 May 1948 – 1951 by country of birth (CBS)
- **Source:** Publication 1365, the period table. The 1948–51 column was extracted from the PDF text. [V, parsed]
- Annual 1950 vs 1951 splits: now found in the CBS Statistical Abstracts of the time, see "Annual 1950 and 1951" below. Morocco, Tunisia and Algeria are only given together there, and Egypt and Sudan not at all.
- **Figures:**
  - Iraq **123,371**
  - Romania **117,950**
  - Poland **106,414**
  - Yemen **48,315**
  - Bulgaria **37,260**
  - Turkey **34,547**
  - Libya **30,972**
  - Morocco **28,263**
  - Iran **21,910**
  - Czechoslovakia **18,788** (parse slightly ambiguous)
  - Egypt and Sudan **16,028**
  - Hungary **14,324**
  - Tunisia **13,293**
  - Germany **8,210**
  - Algeria **3,810**
- **Cross-checks:**
  - These match a Jewish Agency archive summary found in search: Iraq 123.3k, Rumania 118.0k, Poland 106.4k, Yemen and Aden 48.3k, Morocco/Tunisia/Algeria 45.4k. Morocco + Tunisia + Algeria = 45,366 ✓.
  - The page itself (https://archive.jewishagency.org/society-and-politics/content/36566) now returns 404.

#### Annual 1950 and 1951 by country of birth (CBS Statistical Abstracts, issue #1419)
Read 2026-10-23 from the scanned page images of the two PDFs by Claude Code (an AI), not yet by a person; every column below was checked against the printed subtotals.
- **1950:** CBS, *Statistical Abstract of Israel* No. 2 (1950/51), Table 6 "Jewish Immigrants, by Country of Origin or Birth (1919 – III 1951)", printed p. 27 (PDF p. 57), column "1950". https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1951_num2.pdf [V]
- **1951:** CBS, *Statistical Abstract of Israel* No. 3 (1951/52), Table 6 "Jewish Immigrants, by Country of Origin (1919 – III 1952)", printed p. 26 (PDF p. 55), column "1951 I–XII". The same table's column "15 V 1948 – 31 XII 1951" is given for comparison with Publication 1365. https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1952_num3.pdf [V]

| Country of birth (as printed) | 1950, Jewish immigrants (SA 2) | 1951, Jewish immigrants (SA 3) | 15 V 48 – 31 XII 51, Jewish immigrants (SA 3) | 1948–51, all immigrants (Pub. 1365, above) |
|---|---|---|---|---|
| **All countries** | **169,405** | **173,901** | 684,275 | 687,624 |
| Asia | 57,760 | 103,327 | 237,378 | |
| Iraq | 32,453 | 89,088 | 123,265 | 123,371 |
| Iran | 10,519 | 9,444 | 21,784 | 21,910 |
| Yemen | 8,650 | 698 | 45,279 (Yemen and Aden) | 48,315 (Yemen, including the former Aden: Pub. 1365 Table 5 note 7, printed p. 39) |
| Aden | 2,692 (Aden and other Asia) | 239 | see Yemen | |
| Turkey | 2,439 | 1,198 | 34,294 | 34,547 |
| Syria and Lebanon | 1,007 | 316 | 2,898 | |
| Other Asia | see Aden | 2,344 | 9,858 | |
| Africa | 25,820 | 20,124 | 93,295 | |
| Morocco, Algeria, Tunisia (one row) | 9,567 | 11,369 | 45,111 | 45,366 (28,263 + 13,293 + 3,810) |
| Libya | 9,189 | 6,570 | 31,175 | 30,972 |
| Union of South Africa | 183 | 43 | 621 | |
| Other Africa (no Egypt or Sudan row is printed; that they fall here is our reading, not stated) | 6,881 | 2,142 | 16,388 | Egypt and Sudan 16,028 |
| Europe | 83,337 | 49,535 | 331,299 | |
| Romania | 46,178 | 40,208 | 117,660 | 117,950 |
| Poland | 26,499 | 3,505 | 106,136 | 106,414 |
| Hungary | 2,721 | 1,273 | 14,301 | 14,324 |
| U.S.S.R. | 2,286 | 566 | 6,201 | |
| Bulgaria | 960 | 1,161 | 37,210 | 37,260 |
| Germany | 932 | 463 | 8,150 | 8,210 |
| Czechoslovakia | 632 | 295 | 18,731 | 18,788 (parse slightly ambiguous, see the list above) |
| Other Europe (the 9 smaller rows) | 3,129 | 2,064 | | |
| America and Oceania | 1,006 | 671 | 3,516 | |
| Not stated | 1,482 | 244 | 18,787 | |

- **Checksums** (our arithmetic on the printed rows):
  - 1950: 57,760 + 25,820 + 83,337 + 1,006 + 1,482 = 169,405, the printed total. Asia, Africa, Europe and America each equal the sum of their country rows.
  - 1951: 103,327 + 20,124 + 49,535 + 671 + 244 = 173,901, the printed total; the continents also equal their country rows. "Other Europe" above is our sum of Latvia/Lithuania, Yugoslavia, Greece, Austria, United Kingdom, Netherlands, France, Italy and "other countries", which the table prints one by one.
- **Against the totals in 4a** [C]:
  - The annual totals of 169,405 and 173,901 leave out the travellers permitted to settle in 1950 and 1951: SA 2 Table 2 "Jewish Immigrants", note 1 (printed p. 25), and SA 3 Table 3, note 1 (printed p. 25), both say "except those permitted to stay in 1950 and 1951"; SA 2 Table 2 gives 169,405 for 1950 (its 1951 row covers January to March only) and SA 3 Table 3 gives 169,405 and 173,901. Table 6 itself says so only indirectly (its footnote 4 includes such travellers for 1948 and 1949 only). CBS *Statistical Abstract* No. 14 (1963), Table 3 "Immigrants and Tourists Settling, by Year (Jews)", printed p. 109 (PDF p. 134), adds them: **1950 = 170,249** (of which 844 tourists settling) and **1951 = 175,095** (of which 1,194). 169,405 + 844 = 170,249 and 173,901 + 1,194 = 175,095. https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf [V]
  - Publication 1365 Table 1 (printed p. 30) gives each year for all immigrants and for Jews: 1950 = 170,563 all, 170,215 Jews; 1951 = 175,279 all, 175,129 Jews. The 4a totals are the all-immigrant figures, so they do not compare with the Jewish-only tables here. Against Publication 1365's Jewish figures, SA 14 is 34 higher for 1950 (170,249 against 170,215) and 34 lower for 1951 (175,095 against 175,129); the volumes read do not explain this. [C]
  - The 4a continent rows are all-immigrant figures too (they sum to 170,563), while SA 2 counts Jewish immigrants only, so the two series should not be compared row by row or mixed.
  - The 1948–51 period column: SA 3's 684,275 counts Jewish immigrants and Publication 1365 Table 5's 687,624 counts all immigrants, so their 3,349 gap is not like for like. Against Publication 1365's Jewish rows in Table 1 (101,819 + 239,576 + 170,215 + 175,129 = 686,739), SA 3 is 2,464 lower. Most country rows differ by under 300, and Yemen by 3,036 (45,279 for Yemen and Aden in SA 3, against 48,315 in Publication 1365). [C]
- **Definitions:**
  - Country of birth for these years; the tables use nationality only for 1919–34 and for the 1946 visa-less immigrants (footnote 1 of both tables).
  - SA No. 2 Tables 2 and 6 and SA No. 3 Tables 3 and 6 have no "potential immigrants" category (the rest of each volume was not searched for one). Publication 1365 Table 1 has that column, but it is empty for 1950 and 1951 (it starts in 1969); its 170,215 for 1950 is the Jews column.
- **Not in these tables** [GAP]:
  - Morocco, Tunisia and Algeria separately for 1950 or 1951 (one combined row in SA 2, SA 3 and SA 4).
  - Egypt and Sudan for 1950 or 1951 (no row of their own; presumably inside "other countries" of Africa, not stated).
  - Aden separately for 1950 (one cell with "other countries" of Asia).
  - SA No. 4 (1952/53) Table 5 gives only the whole period 15 V 1948 – 31 XII 1952 and the year 1952, not 1950 or 1951 (printed p. 28). SA No. 14 Table 4 gives continents only. A later CBS series by year and country (for example a special-series volume on immigration 1948–72) was not found online.

### 4c. Operations
- **On Wings of Eagles (Yemen and Aden):**
  - 16 Dec 1948 → **24 Sep 1950**, when the last two planes landed at Lydda with 177 passengers.
  - Total about **49,000**. About 430 flights, transport cost over US$4 million.
  - Source: Schechtman 1952 via https://en.wikipedia.org/wiki/Operation_On_Wings_of_Eagles [V]
  - [C] The stage figures on that page (about 7,000 + about 30,000 + an unquantified 1950 stage) don't reconcile with 49,000.
  - The 1950 timeline says the operation brought "roughly 50,000". https://en.wikipedia.org/wiki/1950_in_Israel
  - CBS: Yemen-born 1948–51 = 48,315.
- **Ezra and Nehemiah (Iraq)** [V] https://en.wikipedia.org/wiki/Operation_Ezra_and_Nehemiah :
  - **March 1950:** Iraq passes the one-year **Denaturalisation Law**. Jews may leave if they renounce citizenship. The window expires March 1951 (Meir-Glitzenstein 2004, p. 206).
  - **Registration:** about 50,000 in the first month, about 90,000 within two months [S, uncited]. By Sep 1950, about 70,000 registered but only about 10,000 had left (Meir-Glitzenstein p. 204).
  - **10 Mar 1951:** A new Iraqi law freezes the assets of Jews who renounced citizenship. 64,000 were still waiting (Meir-Glitzenstein p. 206).
  - **Airlift dates:** [C]
    - en.wiki 1950 timeline: "18 May 1950: Operation Ezra and Nehemiah begins".
    - The operation article: flights via Cyprus began "mid-May 1951" [uncited], later direct Baghdad → Lod. It ended "early 1952" [citation needed], leaving about 6,000 Jews.
    - **Interpretation:** emigration flights ran from May 1950, and the mass airlift came in 1951. The game should treat **1951** as the peak year. The CBS Asia figure for 1951 (103,396) supports this.
  - **Total:** 120,000–130,000 (Pasachoff and Littman; Szulc).
  - **Cost:** about US$4m, largely paid by the JDC.
  - **Carriers:** Near East Transport Company (founded by James Wooten, who had run the Yemen airlift) and El Al.
  - A 1951 Israeli Foreign Ministry letter to the UNCCP says about **104,000** registered "during the year", with over 40,000 already arrived [S, snippet]. https://www.un.org/unispal/document/auto-insert-211728
  - **Baghdad bombings, Apr 1950 – Jun 1951** (Israeli historians' framing). https://en.wikipedia.org/wiki/1950–1951_Baghdad_bombings
    - Five attacks; 3–4 Jews killed on 14 Jan 1951.
    - **Moshe Gat** (Israeli historian, *The Jewish Exodus from Iraq 1948–1951*, 1997) argues the emissaries did not plant the bombs and suspects Istiqlal militants.
    - **Esther Meir-Glitzenstein** calls the Iraqi charges "groundless" and shows mass registration preceded most bombings.
    - A **1960 secret Israeli inquiry** appointed by Ben-Gurion found no proof that any Jewish body threw the bombs.
    - The contrary view is Avi Shlaim (2023).
    - Recommend game text: "bombings whose perpetrators remain disputed; Israeli inquiries found no Israeli involvement."
- **Romania:**
  - About 115,000 had emigrated to Israel by Dec 1951 [S] (en.wiki Romanian Jews in Israel, via search). CBS 1948–51 Romania-born = 117,950.
  - **Pauker–Rubin understanding:** about 4,000 per month [S]. https://en.wikipedia.org/wiki/Jewish_emigration_from_Romania
  - **Exit closure:** [C] "orders of 1951" (1959 Detroit Jewish News) vs. "drastically reduced by 1952" (Ioanid via Wilson Center). https://www.wilsoncenter.org/event/book-launch-the-ransom-the-jews-the-story-the-extraordinary-secret-bargain-between-romania-and
  - The AJYB 1952 estimate has Romania's Jewish population falling from 335,000 (1950) to 280,000 (mid-1951). https://www.bjpa.org/content/upload/bjpa/1952/1952worldpop.pdf [S]
- **Poland:** CBS 1948–51 Poland-born = 106,414. Annual: **26,499 in 1950** (SA No. 2, Table 6, p. 27) and **3,505 in 1951** (SA No. 3, Table 6, p. 26), without travellers permitted to settle; see 4b. [V] Exits were encouraged roughly 1949–51 [S].
- **North Africa and Libya:**
  - CBS 1948–51: Morocco 28,263, Tunisia 13,293, Algeria 3,810, Libya 30,972.
  - **Selective immigration:** In **November 1951** the Jewish Agency (via the Coordinating Committee) adopted selection guidelines for North African aliyah: young, healthy, working-age people. This held at least to 1956. [S] Secondary sources only: Avi Picard, *Olim Bemesorah* (2013); Daniel Ben Simon; jewishrefugees.org.uk https://www.jewishrefugees.org.uk/2013/10/israels-painful-selection-of-moroccan.html . Disputed by Shifra Shvarts. Primary document not seen.

### 4d. Population (CBS Statistical Abstract 2023, table 2.1 and table 2.2, thousands)

| Date | Total | Jews (and Others) | Arabs | Muslims | Christians | Druze |
|---|---|---|---|---|---|---|
| End 1949 (existing) | 1,173.9 | 1,013.9 | 160.0 | 111.5 | 34.0 | 14.5 |
| **End 1950** | **1,370.1** | **1,203.0** | **167.1** | 116.1 | 36.0 | 15.0 |
| **End 1951** | **1,577.8** | **1,404.4** | **173.4** | 118.9 | 39.0 | 15.5 |
| Avg 1950 | 1,266.8 | 1,103.0 | 163.8 | | | |
| Avg 1951 | 1,494.3 | 1,324.0 | 170.3 | | | |

- **Sources:** https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_01.pdf (by population group) and https://www.cbs.gov.il/he/publications/doclib/2023/2.shnatonpopulation/st02_02.pdf (by religion). [V, parsed]
- The end-1949 row reproduces the value already in `population.yaml` (1,013.9k), which validates the parse.
- **For `population.yaml`:** `1950-12-31 jews: 1203000`, `1951-12-31 jews: 1404400`.
- **Natural increase:** [GAP] The CBS "sources of population growth" table was not located (tried st02_04/05, both 404).
  - **Derived, not a CBS figure:** The Jewish increase of 189.1k in 1950 against 170.6k immigrants leaves about 18.5k for natural increase net of emigration and adjustments. The 201.4k increase in 1951 against 175.3k immigrants leaves about 26.1k. Use only as a calibration hint.
  - he.wiki ma'abarot gives population growth rates of 28.3% (1948–49), 16.7% and 15.2% for the following years [S].

---

## 5. Ma'abarot (transit camps)

- **Origin:**
  - The idea is credited to **Levi Eshkol** (head of the Jewish Agency Settlement Department). He proposed replacing the immigrant camps (*machanot olim*) in **March 1950** (Hacohen 2003, p. 151).
  - The word "ma'abarot" first appears in Eshkol's letter of **21 Apr 1950** (he.wiki).
  - The government and Jewish Agency decision is dated "May 1950" in the en.wiki 1950 timeline (undated within the month). [V] https://en.wikipedia.org/wiki/Ma'abarot ; https://he.wikipedia.org/wiki/מעברה
- **Key design difference from the camps:** The camps fed and housed immigrants free. In the ma'abarot, residents had to earn their own living. [V] he.wiki
- **First ma'abara:** **Kesalon**, Jerusalem Hills, **May 1950**, 150 families (Allweil 2012; *Davar* 23 May 1950). [V]
- **Population and count over time:**
  - **April 1950:** about 100,000 immigrants in temporary camps (en.wiki 1950 timeline, undated). [S]
  - **July 1950:** Giora Yoseftal moved 70,000 immigrants into ma'abarot (Roby 2015). [V]
  - **End 1950:** **62 ma'abarot housing 93,000**, plus 40,000 in other temporary camps. [S] en.wiki 1950 timeline, no footnote.
  - **End 1951 (the usual "peak" anchor):** [C]
    - About **227,000 in 123 or 125 ma'abarot**, about one-sixth of the population (Lipshitz 1998; Roby 2015; Kachanski 1986).
    - vs. **127 ma'abarot / 250,000** (Swirski 2002, in the en.wiki lead).
    - he.wiki: "over 220,000" within two years; 98 + 31 = about 129 ma'abarot in total.
  - **January 1952:** 157,140 residents (he.wiki). [S] Note the conflict with the 227,000 end-1951 figure. It may reflect a different count (e.g. registered vs. including batei olim). Unresolved.
  - **1952:** 172,500 in 111 tent camps plus 38,544 in wooden shacks (Roby 2015). [S]
- **Conditions (citable):**
  - Infant mortality 157 per 1,000 (Davidovich and Shvarts 2004; Hacohen 2003).
  - Worst-camp unemployment 90–96% (Roby 2015).
  - Migdal Gad: two faucets for about 1,000 people (Shapira 2014).
- **Winter 1950–51 flooding and storms** (researched 2026-10-23 for #1420; every JTA page below was opened that day, its date is the page's own bulletin date, and "today"/"yesterday"/"last night" in a quote count from the dispatch, so the event day is not fixed more closely than that; [V] here means an agent read the page, and a person's check is pending):
  - **Before the rains: the shelter campaign, kept separate.** en.wiki 1950: the **6 Nov 1950** "Shelter campaign" appealed to the public to house immigrant children for the winter. [S] The JTA bulletin of 6 Dec 1950 describes "the special campaign now being conducted by the Israel Government and the Jewish Agency among the urban population to secure shelter for children" (our reading: the same campaign; the bulletin gives no start date). [V] https://www.jta.org/archive/rains-flood-refugee-camps-in-israel-government-seeks-shelter-for-6000-children
  - **Winter preparations, November 1950** (government and army; in the sources read here they come before the first storm report, 6 Dec 1950: our reading):
    - JTA 1 Nov 1950: the Knesset Finance Committee voted "500,000 Israel pounds for the preparation of 'work villages' for immigrants for the coming winter", plus 500,000 pounds for the Welfare Ministry's care of immigrants. [V] https://www.jta.org/archive/israel-parliamentary-committee-votes-3000000-for-plans-to-absorb-immigrants
    - JTA 22 Nov 1950: "The Israel Army will take over some 40 work villages and semi-permanent camps on December 1 for a period of four months"; the army would run transport, keep the roads open and give medical and child-welfare help; "some 1,500 children will be wintered in Army camps"; "some 200 physicians have been called up from the reserves". [V] https://www.jta.org/archive/israel-army-to-take-over-immigrant-work-camps-for-four-winter-months-calls-up-doctors
    - JTA 24 Nov 1950: at a Cabinet meeting "Labor Minister Golda Myerson was today given complete charge of the government's program in behalf of immigrants living in work villages and semi-permanent camps"; "Three thousand new wooden huts must be built before the winter sets in, the Cabinet decided." [V] https://www.jta.org/archive/golda-myerson-named-to-head-government-program-for-immigrants-in-work-camps
    - Maariv (Arik Bender, 15 Apr 2020, a retrospective): the army operation was called "מבצע מעברות", and Chief of Staff Yadin wrote in an order of the day of **17 Nov 1950** that "הממשלה החליטה להטיל על צה"ל להטות שכם למשימה הגדולה הזאת" (the government decided to charge the IDF with shouldering this great task). [S] The order itself was not reached. https://www.maariv.co.il/news/military/article-759860
  - **The storms, as reported:**
    - JTA 6 Dec 1950: "The first intense rainfall yesterday has affected adversely many immigrant camps in various parts of the country"; "Despite specially-fitted double flaps, many tents were flooded". The Jewish Agency said "At least 6,000 children of school age need homes during the winter", and Social Welfare Minister Rabbi I. M. Lewin appealed to the public to take them in. No camp is named. [V] (link above)
    - JTA 8 Jan 1951: "The long-awaited rains came down in torrents in Israel last night"; they "brought terrible hardships to newcomers living in tents and huts in 'work villages,' particularly in the Jerusalem and Rehovot sectors. Tents collapsed and roofs of huts were blown away in a number of immigrant villages in the north. Police and army units were summoned to several villages to aid the immigrants." [V] https://www.jta.org/archive/heavy-rains-save-crops-in-israel-immigrants-in-work-villages-suffer-from-inundation
    - Maariv 2020 (above): "גשמי הזעף" flooded the immigrant camps and ma'abarot "בראשית שנות ה־50" (in the early 1950s), and the press of the time ran pictures of soldiers pulling immigrants out of the floods and the mud. It gives no day and no camp for the floods. [S]
  - **Other measures during the winter:**
    - JTA 14 Dec 1950: the Finance Committee voted 500,000 pounds "for use in work settlements known as 'Maabarot'". [V] https://www.jta.org/archive/israel-parliamentary-committee-votes-1400000-for-use-in-work-settlements
    - JTA 29 Dec 1950: the Vatican's representative put a convent in the Haifa district at the authorities' disposal "for the use of 200 immigrant children" for four months. [V] https://www.jta.org/archive/vatican-offers-convent-in-haifa-as-winter-shelter-for-jewish-immigrant-children
    - JTA 2 Jan 1951: the Jewish Agency's plan was partly in effect; "Some 4,500 canvas huts have been erected in various work centers as have been 5,000 tin structures." [V] https://www.jta.org/archive/jewish-agency-begins-work-on-improving-conditions-in-immigrant-work-centers
    - JTA 2 Mar 1951: OSE took "Over 300 sick and undernourished immigrant children" out of the camps' huts and tents for "the entire winter period", with government and Jewish Agency support. [V] https://www.jta.org/archive/ose-in-israel-provides-shelter-for-300-sick-children-from-immigrant-games
  - **[GAP] Named camps, casualties and totals for winter 1950–51.** No source read names a camp hit in December 1950 or January 1951 (only "the Jerusalem and Rehovot sectors" and "the north"). None gives deaths, a count of flooded tents or of evacuees for that winter. Nothing is extrapolated. The Knesset record, *Davar* and the *Palestine Post* (NLI, HTTP 403) and the IDF Archives were not reached.
  - **[C] Snow, and the Ein Shemer collapse.** Rossetto (*Quest* 4, 2012, in a paragraph citing Hacohen, *Immigrants in Turmoil*, pp. 129-161) says "The 1950-1951's winter was particularly severe, with heavy snow falls across the country". [S] https://www.quest-cdecjournal.it/space-of-transit-place-of-memory-maabarah-and-literary-landscapes-of-arab-jews/ Maariv 2020 dates the collapse of the dining-hall roof at the Ein Shemer immigrant camp under snow, four killed and five hurt, to "6 בפברואר 1951". [S] The JTA bulletin of **8 Feb 1950** reports a collapse that may be the same event (our reading), in the February 1950 snowstorm (four dead at the Ein Shemer camp under snow; JTA says a mess-hall wall and "many injured", Maariv a dining-hall roof and five hurt): "Four Jews were killed and many injured last night when a wall of a mess hall at the Ein Shemer immigrant camp for Yemenites collapsed under the weight of snow." [V] https://www.jta.org/archive/snow-since-in-jerusalum-broken-crops-affected-damages-reach-millions-of-dollars No JTA report of snow in winter 1950–51 was found. Both versions are kept; the note does not decide whether these are one event or two, or which year.
  - **Not this winter (kept apart):** heavy rains flooded immigrant camps in **December 1949** (JTA 27 Dec 1949, https://www.jta.org/archive/heavy-rains-flood-immigrant-camps-in-israel-children-contract-pneumonia-aid-urged) [V], in winter 1949–50. The great storm of **December 1951** (JTA 26 Dec 1951, "Israel's 11-day Storm Ends; Heavy Damages Reported", https://www.jta.org/archive/israels-11-day-storm-ends-heavy-damages-reported) is winter 1951–52 [V]. Maariv 2020 quotes a Knesset debate of 20 Dec 1951 on the ma'abarot in that month [S]. Neither belongs to winter 1950–51. (JTA: titles and dates only.)
- **End:** Most residents were rehoused 1957–60. The last ma'abara (Hatzrot Yosef) was dismantled 20 Jan 2004 (he.wiki). en.wiki says "around 1963".

---

## 6. Austerity (tzena)

- **Start and ministry:**
  - Rationing was decided **26 Apr 1949**, the same day Ben-Gurion's Knesset statement presented it as temporary. [V] https://he.wikipedia.org/wiki/תקופת_הצנע
  - Ministry of Supply and Rationing: Dov Yosef, **26 Apr 1949 → 1 Nov 1950**. [V] https://en.wikipedia.org/wiki/Rationing_and_Supply_Minister_of_Israel
  - Note that `people.yaml` currently starts `rationing_minister` on 1949-03-10. The en.wiki ministry page says the ministry was created on 26 Apr 1949. [C] Flag for the M1 owner.
- **Ration:**
  - en.wiki: about **1,600 kcal/day**, monthly coupons worth IL 6, modelled on the UK wartime diet (Seidman 2008). [V] https://en.wikipedia.org/wiki/Austerity_in_Israel
  - he.wiki daily list: 360 g bread, 58 g sugar, 200 g cheese …; meat 750 g a month.
- **1950 tightening:**
  - **31 Jul 1950** (en.wiki) / "July 1950" (he.wiki): rationing was extended to **clothing and footwear**. [V]
  - **Aug 1950:** Retailers' strike (1–14 Aug), which became a general strike against austerity on 7 Aug.
  - **8 Aug 1950:** A no-confidence motion on rationing failed, **57–36**. [V] en.wiki 1950 in Israel. he.wiki also mentions a public review committee.
- **Black market:**
  - By Feb 1950, about **5,000 police files** had been opened (Mordechai Naor, via he.wiki). [S]
  - **Sep 1950:** The **Office for Fighting the Black Market** was created (en.wiki, Seidman). [V]
  - **3 Oct 1950:** Ben-Gurion launched an anti-black-market campaign with a personal public appeal (en.wiki 1950). [V]
  - **Mar 1951:** On the black market the gold sovereign fell from IL 30 to IL 20, and the government moved to ban gold trading. [V] JTA, 27 Mar 1951 https://www.jta.org/archive/israel-to-prohibit-trading-in-gold-measure-taken-to-check-inflation-in-country
- **Late 1950 easing:**
  - The **14 Nov 1950** local elections (General Zionists strong) showed Mapai it had to ease austerity.
  - The Supply ministry was "symbolically" abolished and its powers spread to other ministries. [V] he.wiki tzena
  - **Note:** rationing itself continued, with Dov Yosef back in charge as Trade and Industry minister from 8 Oct 1951.
- **Repressed inflation:**
  - Official consumer price index: up only about **10%** from end-1948 to end-1951. The money supply rose about **140%** over the same period, and Michaely estimates free-market prices rose about 30 points more.
  - Black-market activity peaked at the 1952 devaluation.
  - [V] Michael Michaely, *Foreign Trade Regimes and Economic Development: Israel* (NBER, 1975), ch. 5, pp. 133–134. https://www.nber.org/system/files/chapters/c4560/c4560.pdf
- **New Economic Policy:** **1952, outside the window.** Announced **13 Feb 1952** [C: 14 Feb in the NBER devaluation study; 13 Feb in a 1952 pamphlet] https://www.marxists.org/archive/vilenska/1952/new-economic-plan.pdf .
  - It brought devaluation from IL 1 = $2.80 to multiple rates ($1.40 / $1.00), and gradual decontrol.
  - Michaely Table 5-1 shows the formal rate jumping from IL 0.36/$ (1950–51) to 0.70 (1952 average). [V]
  - A Jerusalem Post archive item gives the confidence vote on the NEP as 55–44 [S].
  - **Fiscal tightening is visible from April 1951:** public-sector excess demand fell from about 20% of GNP (1949–50) to about 11% (1951), and money growth almost stopped from Q3 1951 (Michaely Table 5-2 and text). [V]
  - **Use for the game:** a "pre-NEP tightening" in late 1951 is historical; the NEP itself is a 1952 event.

---

## 7. Key laws, 1950–51 (and 1949 carry-over)

| Law | Date | Notes | Source |
|---|---|---|---|
| Compulsory Education Law 5709-1949 | Passed **12 Sep 1949** [S]; [C] another source says August 1949. In force 18 Sep 1949. | Free compulsory schooling. Ages [C] 5–13 / 5–14 / 5–15 depending on source. Parents choose a recognised stream ("trend"). | https://israeled.org/compulsory-education-law ; https://www.jewishvirtuallibrary.org/principal-laws-relating-to-education-in-israel ; en.wiki First Gov (Louvish: 5–14) |
| Jerusalem resolution | **23 Jan 1950** | Knesset declares Jerusalem the capital | en.wiki 1950 (Gilbert) |
| Absentees' Property Law 5710-1950 | **14 Mar 1950** [V date in en.wiki 1950 timeline; the UN working paper also references 14 Mar 1950] | Vests absentee property in a Custodian. "Absentee" is defined from 29 Nov 1947. | https://en.wikipedia.org/wiki/1950_in_Israel ; UNCCP https://www.un.org/unispal/?p=211848 ; text https://www.adalah.org/uploads/oldfiles/Public/files/Discriminatory-Laws-Database/English/04-Absentees-Property-Law-1950.pdf |
| Development Authority (Transfer of Property) Law 5710-1950 | [C] In force **9 Aug 1950** (UN working paper) vs "July 1950" (Forman and Kedar, via a partisan explainer) | Lets the Custodian sell to the Development Authority, which can sell to the State and the JNF | UNCCP paper above |
| Harari Decision | **13 Jun 1950** | Constitution to be built chapter by chapter as Basic Laws. Vote 50–30, URF abstaining (Kraines 1957). | en.wiki URF; he.wiki First Knesset |
| Law of Return 5710-1950 | **5 Jul 1950**, unanimous (Herzl's yahrzeit) | "Every Jew has the right to come to this country as an oleh". Exclusions for activity against the Jewish people or danger to public health and security (later amendments added more). | https://en.wikipedia.org/wiki/Law_of_Return |
| Nazis and Nazi Collaborators (Punishment) Law | **1 Aug 1950** | Allows capital punishment | en.wiki 1950; he.wiki First Knesset |
| 1950/51 state budget | **29 Jun 1950** | **IL 60 million** (about US$168m), +30%. After a 3-month provisional budget (ordinary IL 10.601m + development IL 20m). | en.wiki 1950; JTA-wire via Bentley |
| Women's Equal Rights Law 5711-1951 | [S] **17 Jul 1951** (date not confirmed by any page read; only "1951") | Formal equality for women | https://en.wikipedia.org/wiki/Women%27s_Equal_Rights_Law_of_Israel_(1951) |
| Hours of Work and Rest Law; MKs' Immunity Law; Civil Defence Law | 1951 (exact dates not taken) | | he.wiki First Knesset |
| Transition to the Second Knesset Law; Second Knesset Elections Law; election-day law | April 1951 (Transition Law 12 Apr per hamichlol [S]) | Dissolution mechanism | §2 above |
| Israel Bonds authorising vote | **Feb 1951** (Knesset voted to launch the first issue) | | https://en.wikipedia.org/wiki/Israel_Bonds (citing JTA "Facts About Israel Bonds") |

- **Yeshiva deferments (not a law):**
  - **About 400** yeshiva students exempted under the Ben-Gurion arrangement. [C] Dated 1948 or 1949 depending on source.
  - [S] Jüdische Allgemeine reports a 1951 Ben-Gurion letter formalising the exemption under section 12 of the Defence Service Law.
  - https://en.wikipedia.org/wiki/Haredi_conscription_in_Israel ; https://www.juedische-allgemeine.de/religion/beten-oder-schiessen/

---

## 8. Security, 1950–51

- **Chief of Staff:** **Yigael Yadin** (`yadin`), 9 Nov 1949 → resigned **7 Dec 1952** over defence-budget cuts. The cabinet accepted his resignation and named Maklef on 4 Dec 1952. [V] https://www.jta.org/archive/israel-army-chief-of-staff-resigns-makleff-named-new-head ; https://en.wikipedia.org/wiki/Mordechai_Maklef . In office for the whole window.
- **Defence Minister:** Ben-Gurion throughout. [V] cabinet tables above.
- **Infiltration (Israeli historians' framing):**
  - **Benny Morris, *Israel's Border Wars 1949–1956* (1993)** is the standard reference.
    - Most crossings were economic or social: refugees returning to homes, fields and relatives. A minority were armed theft or murder.
    - Between 2,700 and 5,000 infiltrators were killed in 1949–56, most of them in 1949–51 (pp. 215–216).
    - [V via] https://en.wikipedia.org/wiki/Reprisal_operations
  - **Israeli deaths from infiltration by year** (Morris figures as reported by Algemeiner): **1950: 19**, **1951: 48** (1949: 22; 1952: 42). [S] https://www.algemeiner.com/2023/11/13/attacks-from-gaza-were-common-from-1948-to-1956-heres-how-they-were-stopped/
  - Brecher, *Israel's Foreign Policy* p. 229: **137 Israeli "casualties" in 1951** (likely killed + wounded). [S] https://cojs.org/1951-attacks-by-arab-infiltrators-fedayeen/
  - Morris: 57 Israelis (mostly civilians) killed by infiltrators from the West Bank and Jordan, Jun 1949 – end 1952 (en.wiki Reprisal operations). [V]
  - [C] The figures differ by front and definition. Use 1950 ≈ 19 killed and 1951 ≈ 48 killed as the "killed" series; use 137 only as "casualties".
- **Notable incidents:**
  - **17 Mar 1950:** Shepherd killed (Gilad Sharon).
  - **31 Mar 1950:** Negev ambush; 3 soldiers and 2 civilians killed (Morris p. 168).
  - **23 Aug 1950:** Woman killed in a Yarkona orange grove.
  - **29 Nov – 2 Dec 1950:** Jordan blocks the Eilat road at km 78; Israel clears it on 2 Dec.
  - **6 Feb 1951:** IDF raid on Sharafat; 9 killed (Morris p. 194).
  - **20 Oct 1951:** 7th Brigade companies raid the eastern suburbs of Gaza (Morris p. 202).
  - **4 Dec 1951:** Woman abducted and murdered in Jerusalem.
  - **Sources:** https://en.wikipedia.org/wiki/1950_in_Israel and https://en.wikipedia.org/wiki/1951_in_Israel . [V, footnotes as listed]
- **Egypt:**
  - The Tiran and Sanafir islands were taken by Egypt on **24–25 Feb 1950**. [V] en.wiki 1950
  - The **Tripartite Declaration** (US/UK/France) was issued **25 May 1950**. [V] en.wiki 1950
- **Hula drainage dispute (Syria):**
  - Works by the Palestine Land Development Company began about **14 Feb 1951** on the west bank of the Jordan, south of Lake Hula, partly inside the DMZ. Israel relied on the 1914 Ottoman concession, which passed to PLDC in 1934.
  - Syria complained to the MAC on **21 Feb 1951**. General Riley's 7 Mar memo recommended that work stop pending agreement.
  - [V] FRUS 1951 v.V doc 288 (5 Mar 1951) https://history.state.gov/historicaldocuments/frus1951v05/d288
  - Israel resumed the works despite Syrian troop concentrations [V] JTA 27 Mar 1951 https://www.jta.org/archive/israel-resumes-drainage-of-huleh-swamps-syria-concentrates-troops-situation-tense
  - Riley allowed work to continue on undisputed land in early June 1951 [S].
  - **Israeli framing:** normal civilian development and restoration of life in the DMZ; Israel holds the concession.
- **Al-Hamma incident, 4 Apr 1951:**
  - An Israeli patrol in police guise, sent to assert Israeli sovereignty in the southern DMZ, was fired on near al-Hamma (Hamat Gader). **7 IDF soldiers were killed.**
  - **5 Apr 1951:** The IAF bombed the al-Hamma police post and Syrian positions. Israel also razed remaining houses in DMZ Arab villages, whose residents (about 2,200) it wanted moved.
  - **Israeli version:** Syrian troops entered al-Hamma, disarmed local police and fired on the Israeli police patrol.
  - [V] https://en.wikipedia.org/wiki/Al-Hamma_incident (Morris 1993 pp. 362–363; UN S/2099)
- **Tel Mutila, 2–6 May 1951:**
  - Syrian forces and irregulars seized Tel el-Mutilla and Khirbet Abu Zeid, about 1 km west of the DMZ (north of the Sea of Galilee). Golani units recaptured them after five days.
  - **41 IDF killed** [S, Hebrew memorial article https://www.inn.co.il/news/219362 ; ru-wiki also gives 70 wounded].
  - Israel's 2 May statement to the Security Council (read by Eban) called it "unprovoked aggression". [V] https://www.jewishvirtuallibrary.org/statement-to-the-security-council-by-ambassador-eban-on-syrian-incursions
  - The UNSC called for a cease-fire on 8 May [S, FRUS editorial note https://history.state.gov/historicaldocuments/frus1951v05/d330].
- **UNSC Resolution 93, 18 May 1951:**
  - Adopted 10–0–1 (USSR abstained).
  - Found that Israel's 5 Apr air action violated the cease-fire.
  - Per FRUS it also called for halting drainage on Arab-owned DMZ land and the return of displaced DMZ Arabs. [V for date, vote and air-action finding] https://en.wikipedia.org/wiki/United_Nations_Security_Council_Resolution_93
  - [S] for the drainage and return clauses: https://history.state.gov/historicaldocuments/frus1951v05/d416
- **Armistice commissions:**
  - **Jordan:** The MAC was frozen after km 78; Jordan refused MAC talks until the route was settled (Dec 1950). [V] en.wiki 1950
  - **Syria:** Sovereignty in the DMZ was the core issue (Riley: neither party has sovereignty). [V] FRUS d288
  - **Egypt:** The Feb 1950 Auja agreement and the Gaza Strip neutral-zone note [V en.wiki 1950].

---

## 9. Economy anchors

- **GNP:**
  - National-accounts data begin only in 1950. Real GNP grew **about 25%, 1950→1951**. [V] Michaely 1975, p. 134.
  - Halevi's EH.net summary: real GNP grew more than 11% a year on average, 1950–65. https://eh.net/encyclopedia/a-brief-economic-history-of-modern-israel/ [V]
  - [S, unverified] A search snippet attributed to an NBER table (source: CBS Statistical Abstract) gives GNP at current prices of IL **460m (1950)** and **IL 700m (1951)**; in 1955 IL, 1,096m and 1,435m (+30.9%); per capita 865 → 960. Not read directly. The +30.9% also conflicts with Michaely's "about 25%".
  - **National income (Kaplan, Mar 1951):** "the state's national income of 300,000,000 pounds". The four-month budget equalled 16–17% of it. [V] JTA 27 Mar 1951 (link above). Note that this conflicts with the IL 460m GNP snippet: a different concept and Kaplan's own estimate.
- **Budgets:**
  - FY 1950/51 (Apr–Mar): **IL 60m** approved 29 Jun 1950 (en.wiki 1950). [V]
  - Joint Development Authority budget: IL 65m (Aug 1950, JTA wire) [S].
  - FY 1951/52 was passed in instalments: a four-month budget in Mar 1951 and **IL 27.4m for three months** in Sep 1951, excluding the secret defence budget. Revenue Apr–Aug 1951 was 66% above estimate. [S] JTA wire via https://digital.bentley.umich.edu/djnews/djn.1951.09.28.001/74
  - [GAP] No full-year 1951/52 total was found.
- **Fiscal and monetary** (Michaely Table 5-2; IL millions; budget years) [V]:

| Year | Internal debt rise | External debt rise | Total | Excess demand % GNP | Money supply % | Credit to public % |
|---|---|---|---|---|---|---|
| 1950 | 65.4 | 22.6 | 88.0 | 19.2 | 35.4 | 42.3 |
| 1951 | 33.0 | 43.1 | 76.1 | 10.9 | 27.2 | 28.9 |

- **Exchange rate** (Michaely Table 5-1, IL per $): formal rate 0.36 in 1950 and 1951 (IL 1 = $2.80). Effective export/import rates were 0.39/0.40 (1950) and 0.41/0.40 (1951). [V]
- **Inflation:**
  - Official CPI about +10% over end-1948 to end-1951 (repressed; see §6). [V]
  - MFA via JVL: inflation "had risen from 14% the previous year to 66%" in 1952, i.e. **1951 ≈ 14%**. [S] https://jewishvirtuallibrary.org/the-rise-and-fall-of-israeli-inflation
  - A new cost-of-living index base was set at Sep 1951 = 100 [S] (JTA).
- **Foreign-currency crisis, autumn 1951** [V] FRUS 1951 v.V doc 513, Amb. Davis, **19 Oct 1951** https://history.state.gov/historicaldocuments/frus1951v05/d513 :
  - Drought forced higher wheat imports.
  - Immigration in the first six months "greatly exceeded" plan, mainly from Iraq and Romania.
  - Bond cash receipts were only **$60m against $80m pledged**.
  - About **$45m** of short-term commercial credit matured Oct–Dec.
  - Israel asked for about **$15m** in immediate US aid.
  - en.wiki Austerity: foreign banks and oil companies refused to expand credit at the end of 1951. [V]
  - Earlier, on **11 Feb 1950**, Britain released £15m of frozen Mandate-era Israeli sterling assets. [V] en.wiki 1950
- **Israel Bonds:**
  - Ben-Gurion convened US Jewish leaders at the King David Hotel in **Sep 1950** ("Billion Dollar Conference", **3 Sep 1950** per en.wiki 1950).
  - The Knesset authorised the issue in **Feb 1951**.
  - **Launch: 10 May 1951**, Madison Square Garden. The rally raised $35m (JTA 11 May 1951).
  - First-year sales were over **$52m** against a $25m goal. The 3.5% "Independence Issue".
  - [V] https://en.wikipedia.org/wiki/Israel_Bonds ; https://www.jewishvirtuallibrary.org/state-of-israel-bonds
  - [C] en.wiki "1950 in Israel" says "6 Sep 1950: the first State of Israel Bond issue is launched in the United States". This conflicts with the Bonds article and JVL, which say May 1951. Treat **May 1951** as the launch.
- **Unemployment** (issue #1422; read 2026-10-23 from the page images by Claude Code, an AI, not yet by a person):
  - **Falk Institute estimate, Jews, annual averages** [V]: Avner Hovne, *The Labor Force in Israel* (Maurice Falk Institute for Economic Research in Israel, Jerusalem, 1961; preface dated May 1961), Table 7 "Estimates of Unemployment, Jews: 1949–1959", Part A, printed p. 27 (PDF p. 43); the same figures are in the Summary Table, Part B "Jews", printed p. 13 (PDF p. 29). https://en.falk.huji.ac.il/sites/default/files/falk/files/laborhovne.pdf

    | Year | Registered unemployed, daily average (thousands) | Estimated unemployed, labour-force definition (thousands) | Per cent of civilian labour force | Civilian labour force, Jews (thousands, Summary Table B) |
    |---|---|---|---|---|
    | 1949 | 6.4 | 29.6 | **9.5** | 310.0 |
    | 1950 | 5.9 | 28.2 | **6.8** | 412.5 |
    | 1951 | 6.3 | 29.3 | **5.8** | 505.8 |

    - **What it is:** an estimate, not a count. Column 2 is computed from column 1 with a regression Hovne fitted on the 1954–58 weeks for which both the labour exchanges and the Labour Force Survey exist: Y = 11.7 + 2.8X (Table 7 sources). The survey only began in June 1954 (p. 26), so for 1949–51 every figure in columns 2 and 3 is a back-projection. Our check: 11.7 + 2.8 × 6.4 = 29.6, × 5.9 = 28.2, × 6.3 = 29.3; and 29.6 / 310.0 = 9.5%, 28.2 / 412.5 = 6.8%, 29.3 / 505.8 = 5.8%.
    - **Definition and population:** the labour-force definition is a person aged 14 or over who did not work one hour in the survey week and was actively seeking work (p. 26). Jews only. Annual averages. The regression input (column 1) counts people aged 18 and over seeking wage or salaried work through the General Labour Exchanges, on a single day (p. 26).
    - **Jews and non-Jews together** (Summary Table, Part A, printed p. 12, PDF p. 28): 1949 **32.6 thousand, 9.5%**; 1950 **31.0 thousand, 6.9%**; 1951 **33.2 thousand, 6.1%** of a civilian labour force of 342.9, 450.1 and 545.0 thousand. The non-Jewish part excludes the Negev Bedouin, about 13,000–14,000 people (footnote 1, p. 13). [V]
    - Hovne's text (p. 28) sums it up as "very high in 1949 (9.5 per cent), decreased to about 7 per cent for 1950 through 1952"; that sentence describes Part A. The Jews-only column gives 6.8% and 5.8% for 1950 and 1951.
  - **Registered unemployed, the official count** [V]: CBS, *Statistical Abstract of Israel* No. 10 (1958/59), Table 14 "Daily Average of Unemployed, by Sex, Month, Occupation, District and Sub-District (1949–1958)", printed p. 308 (PDF p. 349), the source Hovne cites for column 1. The English title is the one printed on the continuation page (printed p. 309, PDF p. 350); page 308 itself is headed in Hebrew only (ממוצע יומי של דורשי עבודה, work seekers). https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1959_num10.pdf
    - Yearly daily average: **1949 6,351; 1950 5,908; 1951 6,337**.
    - By sex: 1949 men 5,556, women 795; 1951 men 5,277, women 1,060; 1950 not given ("..").
    - Monthly range: 1949 from 1,081 (January) to 10,007 (December); 1950 from 9,822 (January) down to 4,463 (September) and 4,032 (October; SA No. 4 prints 4,932, see below); 1951 from 4,980 (March) to 9,682 (December).
    - Scope (footnote 1): up to the end of 1953, registrations at the labour exchanges affiliated to the "Centre of Labour Exchanges". The table does not say Jews only; Hovne uses it as his Jewish series.
  - **Earlier print of the same series** [C]: CBS, *Statistical Abstract of Israel* No. 4 (1952/53), Table 5 "Daily Average of Unemployed, by Months (1949–1952)", printed p. 83 (PDF p. 97). https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1953_num4.pdf
    - Yearly average 1949 **6,850**, 1950 **5,907**, 1951 **6,337**.
    - It adds a rate: "Unemployed as percentage of total wage earners", 1949 **2.8**, 1950 **2.2**, 1951 **2.0**. This is registered unemployment against wage earners, not the labour-force rate above; the two must not be mixed.
    - Differences from SA No. 10: June 1950 is 4,609 (SA 10: 4,809) and October 1950 is 4,932 (SA 10: 4,032). Our arithmetic: SA 4's twelve 1950 months average 5,908 and SA 10's average 5,850, so SA 4's months fit the yearly figure both volumes print. For 1949 the twelve months average 6,351 in both volumes, so SA 4's 6,850 does not match its own months. Which print is right was not settled from a third source.
  - **ILO, via a JTA brief dated 19 September 1951:** Israel was one of five countries "to show an increase in unemployment in June of this year as compared with June, 1951", a 25 percent increase. [S] https://jta.org/archive/israel-was-one-of-five-countries-to-show . The brief's date and its "June, 1951" cannot both be right, so which two Junes the ILO compared is not settled. For comparison only (our arithmetic): the CBS daily average for June 1951, 5,765, is 25% above SA No. 4's June 1950 (4,609) and 20% above SA No. 10's (4,809).
  - **Ma'abarot, kept apart from the national series:** worst-case unemployment of 90–96% (Roby). This is a figure for the transit camps, not a national rate, and is not comparable with any of the series above.
  - Still missing [GAP]: a 1949–51 labour-force-survey estimate made at the time (the survey only began in June 1954; Hovne's figures above are back-projections), and the registered series for non-Jews.

---

## 10. Candidate events for 1950–51

Each candidate gives dates, the real choice faced, the historical option taken, the alternatives that were discussed, and sources. Effects are left to design.

1. **`frumkin_commission` – Camp-education crisis (Jan–May 1950).** Office: prime_minister.
   - **Choice:** Religious parties allege anti-religious coercion of Yemenite children in the camps. Ignore the charges, or appoint an inquiry and transfer camp education?
   - **Historical:** A commission under Judge Frumkin was appointed in Jan 1950 and reported on 9 May 1950. Camp education moved to the Education Ministry and officials were reassigned. The government did not formally adopt the report.
   - **Alternatives raised at the time:** URF threats to leave the coalition (March 1950).
   - **Sources:** he.wiki Frumkin Commission; en.wiki Gad Frumkin; en.wiki 1950.
2. **`ministry_supply_crisis` – Abolishing the Ministry of Supply and Rationing (Oct 1950).** Office: prime_minister.
   - **Choice:** Keep Dov Yosef's ministry and the coalition as it is, or restructure: close the ministry and bring in a non-party businessman (Geri) for Trade and Industry, at the cost of resigning and rebuilding the coalition.
   - **Historical:** Resigned 15 Oct 1950. A minority government failed on 17 Oct. Rosen's attempt (19–29 Oct) failed. Same coalition re-formed on 1 Nov 1950 (69–42). The ministry was abolished, Yosef moved to Transportation and Geri took Trade and Industry.
   - **Sources:** en.wiki 1950; en.wiki Rationing minister; he.wiki First Knesset.
3. **`law_of_return` – Law of Return (5 Jul 1950).** Office: prime_minister.
   - **Choice:** An unrestricted right of every Jew to immigrate, vs. regulated or selective entry.
   - **Historical:** Passed unanimously.
   - **Sources:** en.wiki Law of Return.
   - **Note:** Not a contested vote. Better treated as a scripted milestone than a dilemma.
4. **`iraqi_exodus` – Iraqi Jews (Mar 1950 – 1951).** Office: prime_minister / immigration_minister.
   - **Choice:** After Iraq's March 1950 denaturalisation law, bring all registrants fast despite ma'abarot overload and the forex crisis, or pace arrivals to absorption capacity.
   - **Historical:** The Zionist movement initially tried to slow registration (Meir-Glitzenstein). After the March 1951 asset freeze, Israel ran the mass airlift through 1951, and about 120–130k arrived by early 1952.
   - **Sources:** en.wiki Ezra and Nehemiah; Meir-Glitzenstein (Iyunim 40, https://in.bgu.ac.il/bgi/iyunim/40/Meir-Glitzenstein.pdf); FRUS d513 (Iraqi inflow exceeding plan).
   - **Caution:** No primary Israeli cabinet decision was seen. Keep the event text general.
5. **`maabarot_policy` – Replace immigrant camps with ma'abarot (Mar–May 1950).** Office: prime_minister (in practice the Jewish Agency, Eshkol).
   - **Choice:** Keep free-board camps, or move to self-supporting tent and tin camps near towns and work.
   - **Historical:** Ma'abarot from May 1950. About 227,000 residents by end-1951.
   - **Sources:** en.wiki and he.wiki Ma'abarot.
6. **`clothing_rationing_1950` – Tighten austerity (Jul–Aug 1950).** Office: rationing_minister.
   - **Choice:** Extend rationing to clothing and footwear and fight the black market, or relax.
   - **Historical:** Extended on 31 Jul 1950. Merchants' and general strikes followed (1–14 Aug). The no-confidence motion failed 57–36 on 8 Aug. The anti-black-market office was set up in Sep 1950.
   - **Sources:** en.wiki and he.wiki austerity; en.wiki 1950.
   - **Note:** This continues the existing `austerity_1949`.
7. **`education_registration_1951` – Religious stream in the camps and the government's fall (Nov 1950 – Feb 1951).** Office: prime_minister / education_minister.
   - **Choice:** Accept the religious parties' demand to keep immigrant children in the religious stream (no Histadrut religious stream), or press Remez's registration plan and risk defeat.
   - **Historical:** The Knesset rejected Remez's proposal. The government resigned on 14 Feb 1951 and Ben-Gurion chose early elections (laws of April 1951, election 30 Jul 1951) over a new coalition.
   - **Sources:** en.wiki and he.wiki Second Government; he.wiki First Knesset; JTA wire (49–42).
8. **`israel_bonds` – Launch Israel Bonds (Sep 1950 → May 1951).** Office: finance_minister / prime_minister.
   - **Choice:** Rely on UJA donations and loans, or sell sovereign bonds to diaspora Jews (risking competition with UJA fundraising).
   - **Historical:** King David conference Sep 1950, Knesset approval Feb 1951, launch 10 May 1951. Over $52m sold in 1951.
   - **Sources:** en.wiki Israel Bonds; JVL.
9. **`hula_drainage` / `al_hamma` – DMZ with Syria (Feb–May 1951).** Office: prime_minister / defense_minister.
   - **Choice:** Continue Hula drainage and assert sovereignty in the DMZ despite the MAC chairman's requests to stop, or halt pending agreement.
   - **Historical:** Continued. Patrol to al-Hamma, 7 killed (4 Apr). Air strike (5 Apr). Tel Mutila battle (2–6 May, 41 killed). UNSC Res. 93 (18 May) criticised the air action. Drainage continued on undisputed land.
   - **Sources:** FRUS d288; en.wiki Al-Hamma incident; JVL Eban statement; en.wiki UNSC 93.
10. **`reparations_claim` – Reparations from Germany (Mar 1951 → Jan 1952).** Office: foreign_minister / prime_minister.
    - **Choice:** Claim compensation from Germany (and later negotiate directly), or refuse any dealings on moral grounds.
    - **Historical:**
      - **12 Mar 1951:** Sharett's note to the four occupying powers claims **$1.5bn**, based on absorbing 500,000 survivors at $3,000 each. [C] en.wiki 1951 timeline: 13 Mar.
      - **27 Sep 1951:** Adenauer's Bundestag declaration offering negotiations.
      - **Oct 1951:** Claims Conference founded.
      - **Within the window, the decision is only the claim note.** The Knesset debate and vote to authorise direct talks were **7–9 Jan 1952 (61–50, 6 abstentions; Begin-led riot on 7 Jan)**, outside the window. Signing in Luxembourg was on 10 Sep 1952.
    - **Sources:** https://en.wikipedia.org/wiki/Reparations_Agreement_between_Israel_and_the_Federal_Republic_of_Germany ; https://www.jewishvirtuallibrary.org/german-holocaust-reparations ; https://www.juedische-allgemeine.de/politik/bruecke-in-die-zukunft-2 ; he.wiki Second Knesset (vote).
- **Extra candidates, if needed:**
  - `selective_aliyah_1951` (Nov 1951 North Africa selection guidelines, [S] secondary only).
  - `yeshiva_deferment` (about 400 exemptions; 1951 formal letter [S]).
  - `forex_crisis_1951` (Oct 1951 US aid request, FRUS d513). This is the strongest-sourced late-1951 economic event.
  - `local_elections_1950` (14 Nov 1950; General Zionists surge).

---

## Gaps and open conflicts (summary)
- **Knesset primary site:** Blocked, so no Knesset-site confirmation. Every government and Knesset date comes from en.wiki, he.wiki, IDI or JVL.
- **Government dates:**
  - First Government end: 30 Oct vs 1 Nov 1950.
  - Ben-Gurion's resignation: 15 vs 16 Oct 1950.
  - Maimon's resignation: 4 Jan 1951 vs post-vote Feb 1951.
- **Third Government parties and portfolios:** Burg Health vs Post. Sheetrit's party in the Third Government (Mapai vs Sephardim).
- **Knesset session dates:**
  - Exact day of the April 1951 dissolution laws (12 Apr from a single source).
  - Second Knesset convening, 20 Aug 1951, from a single source.
- **Aliyah:**
  - No country-by-year (1950 vs 1951) immigration split. Only 1948–51 country totals.
  - Romania and Poland annual figures missing.
- **Demography and ma'abarot:**
  - No natural-increase figure from CBS.
  - Ma'abarot end-1951 count and population conflict (123/125 and 227k vs 127 and 250k). The January 1952 figure of 157k conflicts as well.
- **Economy:**
  - No unemployment rate.
  - GNP in IL is an unverified snippet; Kaplan's IL 300m "national income" is a different concept.
  - No full-year FY1951/52 budget.
- **Security:** Infiltration counts per year come from secondary reports of Morris and Brecher. The Tel Mutila 41-killed figure is from an Israeli memorial article and ru-wiki only.


## Addendum (2026-10-10): Third Government portfolios, Weizmann term start

### Third Government (approved 8 Oct 1951, PM David Ben-Gurion; resigned 19 Dec 1952 per Wikipedia, JVL says 22 Dec; replaced 24 Dec 1952)
Sources: https://en.wikipedia.org/wiki/Third_government_of_Israel ; https://he.wikipedia.org/wiki/ממשלת_ישראל_השלישית ; https://www.jewishvirtuallibrary.org/third-government-of-israel (JVL lists no parties). Knesset site (main.knesset.gov.il govId=3) returned no usable content.

(a) Minister of Immigration (Aliyah): NOT FOUND. No source (en/he Wikipedia, JVL) lists an Immigration portfolio or minister in the Third Government. Tag: not found. (In the Second Government, Immigration sat with Haim-Moshe Shapira, see Q2.) Implication: no Immigration minister existed in Third Gov per all available lists; treat as handled by the Jewish Agency / PM's office until verified.

(b) Minister of War Victims: NOT FOUND in the Third Government (no source lists it). Tag: not found. (In the Second Government it was paired with Religions under Yehuda Leib Maimon.)

(c) Minister of Health: Yosef Burg, Hapoel HaMizrachi, 8 Oct 1951 - 24 Dec 1952. Tag: verified (en Wikipedia Yosef_Burg, he Wikipedia יוסף_בורג, JVL, en Third Gov page; four sources agree).
 Resolution of the Health/Posts conflict: Hebrew sources say Burg became Minister of Posts on 24 Dec 1952 (Fourth Government, to 1 Jul 1958), not in the Third. So "Burg = Posts" is the later portfolio; "Burg = Health" is correct for the Third Government. https://he.wikipedia.org/wiki/יוסף_בורג ; https://en.wikipedia.org/wiki/Yosef_Burg
 Posts in Third Government: Mordechai Nurock (Nurok), Mizrachi, Minister of Posts from 3 Nov 1952 to 24 Dec 1952 (first ever holder; appointed to the seat vacated after Agudat Yisrael left the coalition in Sept 1952). Tag: verified for the 3 Nov 1952 date (en + he Wikipedia Nurock pages; he page Posts-minister article says Posts had been inside the Transport ministry, split off after Pinkas died 14 Aug 1952). Holder of Posts between 8 Oct 1951 and 3 Nov 1952: not found / no separate minister (postal powers were held by the postal director, under Transport). JVL and en Third Gov page list Nurock as Posts without a date, which is simplified. https://he.wikipedia.org/wiki/מרדכי_נורוק ; https://he.wikipedia.org/wiki/שר_הדואר

(d) Bechor-Shalom Sheetrit: Minister of Police, party Mapai, whole term 8 Oct 1951 - 24 Dec 1952. Tag: verified (en Wikipedia, he Wikipedia, JVL agree on portfolio). Note: in the Second Government (1950-51) Sheetrit was listed under "Sephardim and Oriental Communities" (en Wikipedia Second_government_of_Israel); in the Third, both Wikipedias list Mapai.

Full minister list (en Wikipedia unless noted):
- David Ben-Gurion, Mapai: PM and Defense; also Transport from 14 Aug 1952 (after Pinkas died)
- Eliezer Kaplan, Mapai: Deputy PM and Finance, 8 Oct 1951 - 25 Jun 1952; Minister without portfolio 25 Jun - 13 Jul 1952 (died 13 Jul 1952)
- Levi Eshkol, Mapai: Agriculture (and Development) 8 Oct 1951 - 25 Jun 1952; Finance 25 Jun 1952 - end
- Peretz Naftali, Mapai: Minister without portfolio 8 Oct 1951 - 25 Jun 1952; Agriculture 25 Jun 1952 - end
- Pinhas Lavon, Mapai: Minister without portfolio 17 Aug - 24 Dec 1952
- Ben-Zion Dinur, Mapai (he Wikipedia; en says not an MK in 2nd Knesset): Education and Culture
- Moshe Sharett, Mapai: Foreign Affairs
- Yosef Burg, Hapoel HaMizrachi: Health
- Haim-Moshe Shapira, Hapoel HaMizrachi: Interior and Religions
- Dov Yosef, Mapai: Trade and Industry; Justice until 25 Jun 1952 (en Wikipedia: Justice 8 Oct 1951 - 25 Jun 1952 then Haim Cohn, not an MK, 25 Jun - 24 Dec 1952; he Wikipedia says Yosef took Justice from 25 Jun 1952 - CONFLICTING on who held Justice after June 1952; JVL lists Yosef as Trade and Industry and Justice)
- Golda Meir, Mapai: Labour
- Bechor-Shalom Sheetrit, Mapai: Police
- Mordechai Nurock, Mizrachi: Posts from 3 Nov 1952
- David-Zvi Pinkas, Mizrachi: Transport 8 Oct 1951 - 14 Aug 1952 (died)
- Yitzhak Meir Levin, Agudat Yisrael: Welfare, to 18 Sep 1952 (en/he differ: 18 vs 23 Sep 1952 for Agudat Yisrael leaving coalition)
- Deputies (en): Yosef Efrati (Mapai) Agriculture; Kalman Kahana (Poalei Agudat Yisrael) Education and Culture
Cabinet size: sources conflict (13 vs 15). Tag for the list overall: verified on core portfolios; dates of mid-term changes single source (Wikipedia) with the conflicts noted above. Knesset vote: 56-40 (he Wikipedia, single source).

### Q2. Second Government (1 Nov 1950 - 8 Oct 1951, Ben-Gurion)
Source: https://en.wikipedia.org/wiki/Second_government_of_Israel (single source, Knesset site not reachable). Tag: single source.
- Immigration: no separate minister; held by Haim-Moshe Shapira (United Religious Front) together with Health and Interior, whole term (no separate dates).
- War Victims: Yehuda Leib Maimon (United Religious Front), as "Religions and War Victims", whole term.
- Others: Lavon (Agriculture, Mapai), Remez (Education, 1 Nov 1950 - 19 May 1951, then Ben-Gurion), Sharett (Foreign), Kaplan (Finance), Rosen (Justice, Progressive), Meir (Labour), Sheetrit (Police), Geri (Trade and Industry), Dov Yosef (Transport), Levin (Welfare). Government resigned 14 Feb 1951.

### Q3. Weizmann first term start
- Knesset election: 16 Feb 1949 (83 votes to Klausner's 15, 114 of 120 voting). Source: https://en.wikipedia.org/wiki/1949_Israeli_presidential_election (cites Wechsler 2024, Middle Eastern Studies 60(2)). Tag: verified (en Wikipedia election page + web search summary). Note en Wikipedia Chaim_Weizmann is internally inconsistent (16 vs 17 Feb for election).
- Declared president / term start: 17 Feb 1949; infobox term runs to 9 Nov 1952 (death). https://en.wikipedia.org/wiki/Chaim_Weizmann ; 1949 election page ("declared president on 17 February 1949").
- Swearing-in: 17 Feb 1949 per Weizmann Institute (https://weizmann.ac.il/WeizmannCompass/node/914) and IFCJ (https://www.ifcj.org/news/fellowship-blog/the-first-president-of-israel-2); Israel MFA page (https://embassies.gov.il/MFA/AboutIsrael/state/Pages/Chaim%20Weizmann.aspx) says swearing-in in Jerusalem on 16 Feb. Tag: conflicting, both versions given; 17 Feb is the better supported (2 vs 1), so election 16 Feb / oath and term start 17 Feb is the working answer.

## Addendum 2 (2026-10-10): rationing responsibility Nov 1950 – Oct 1951

Method note: Knesset site and the Weitz PDF (in.bgu.ac.il redirects), SSRN (403), Tablet (403), Seidman and Mevaker PDFs (unreadable) were not directly readable. Items below marked "search summary" come from web-search summaries of those pages, not from my reading of the page itself.

### A1. Which ministry took over rationing/supply on abolition
- Ministry of Supply and Rationing abolished 1 Nov 1950 (second government formed without it). Decision to close it made Oct 1950 (Ben-Gurion resigned 15 Oct 1950 partly over this). Established Apr 1949 (26 Apr 1949 per en Wikipedia). Dov Yosef was its only minister. Tag: verified (he + en Wikipedia).
  - https://he.wikipedia.org/wiki/משרד_האספקה_והקיצוב ("ב-1 בנובמבר 1950 הוקמה ממשלה חדשה ללא משרד האספקה והקיצוב"; "השר היחיד")
  - https://en.wikipedia.org/wiki/Rationing_and_Supply_Minister_of_Israel (Yosef 26 Apr 1949 - 1 Nov 1950)
- Functions were SPLIT, no single successor: he Wikipedia says portions transferred to the Ministry of Commerce and Industry (Ya'akov Geri) and the Ministry of Agriculture (Pinhas Lavon). Tag: single source read directly (he Wikipedia); the same split (Agriculture + Commerce and Industry) is repeated in a search summary of en Wikipedia text, not independently confirmed on the page I fetched (the en Rationing page itself names no successor).
- Weight toward Agriculture: Weitz, "Iyunim BeTkumat Israel" 24 (2014), pp. 332-362 (https://in.bgu.ac.il/bgi/iyunim/24/Yechiam-Weitz.pdf, search summary only): Lavon was named Minister of Agriculture (and Food) in Nov 1950 and his ministry received most of the functions of the abolished ministry. Schiffman and Goldstein, "Ambition Meets Reality: Pinhas Lavon's Animal-protein Plan under Austerity and Rationing in Israel, 1950-1951" (SSRN 6351358, https://papers.ssrn.com/sol3/papers.cfm?abstract_id=6351358, search summary only): Lavon replaced Yosef as the minister responsible for food supply, austerity and rationing. Tag: single source each (secondary, unread directly); consistent with each other.
- he Wikipedia Lavon page (https://he.wikipedia.org/wiki/פנחס_לבון): appointed "תחילה כשר החקלאות והמזון" (Agriculture and Food) in 1950. Page does not mention rationing. Tag: single source, supports the Agriculture-and-Food title only.
- Mevaker (State Comptroller) report 1951-2, Trade ministry chapter (https://library.mevaker.gov.il/sites/DigitalLibrary/Documents/1950-2008/1951-2/1951-2-215-Mischar.pdf): per search summary only, in Nov 1950 the Trade and Industry ministry was reorganized and a Commodities and Stock division (אגף הסחורות והמלאי) and other departments of the former Supply and Rationing ministry were attached to it. NOT read directly (PDF unreadable). Tag: single source, unverified by me.
- Named rationing commissioner (ממונה/נציב קיצוב) for this period: NOT FOUND. he Wikipedia mentions only a queue committee (summer 1949) and a Rationing Committee (Aug 1950, recommendations adopted Sep 1950). A "Office for Fighting the Black Market" was set up Sep 1950 (en Wikipedia Austerity in Israel), not a rationing commissioner.
- Net answer: Agriculture (Lavon) held the main food supply/rationing responsibility, Trade and Industry (Geri) took supply-side departments (commodities and stock). Tag: conflicting/partial across sources; no source gives an explicit "who was the rationing minister" line. Confidence moderate for Lavon as the political face, low for the precise split.

### A2. Dates and ministers to 8 Oct 1951
- Transfer date: 1 Nov 1950 (verified, as above).
- Ya'akov Geri, Trade and Industry (Mapai, not an MK): 1 Nov 1950 - 8 Oct 1951 (he Wikipedia Geri page https://he.wikipedia.org/wiki/יעקב_גרי and Trade ministry page https://he.wikipedia.org/wiki/משרד_התעשייה_והמסחר). Tag: verified (two he Wikipedia pages). Geri page does not mention rationing.
- Pinhas Lavon, Agriculture: 1 Nov 1950 - 8 Oct 1951 (he Wikipedia Lavon page; en Wikipedia gives "1950-1951"). Tag: verified for dates. Note a minor internal inconsistency on he page body ("1951-1952") vs infobox; infobox is right (Eshkol took Agriculture 8 Oct 1951).
- Weitz (search summary): Lavon announced resignation in Jan 1951 over disagreements with Dov Yosef, "Minister of Supply and Rationing", Ben-Gurion cancelled it. This is odd (Yosef was Transport minister from 1 Nov 1950); do not use without reading the article. Tag: single source, suspect.
- Dov Yosef Nov 1950 - Oct 1951: Minister of Transport, 1 Nov 1950 - 8 Oct 1951 (he Wikipedia https://he.wikipedia.org/wiki/דב_יוסף; en Wikipedia https://en.wikipedia.org/wiki/Dov_Yosef: moved to transportation in the new government). Tag: verified. He was NOT in charge of rationing in this window.
- Date conflict on abolition: he Wikipedia Dov Yosef body text says the ministry was abolished "בשנת 1951" while its infobox and the ministry page say 1 Nov 1950; ynet (https://www.ynet.co.il/articles/0,7340,L-4301054,00.html, search summary) says Mar 1950 for Yosef's removal. Tag: conflicting; 1 Nov 1950 is best supported (Knesset-based table, two Wikipedias, Mevaker mention of Nov 1950).

### A3. Dov Yosef as Minister of Trade and Industry and rationing from 8 Oct 1951
- Dov Yosef became Minister of Trade and Industry (and Justice) on 8 Oct 1951 in the Third Government, to 24 Dec 1952 for Trade and Industry. Tag: verified (he Wikipedia Trade ministry page and he Dov Yosef page; en Dov Yosef page "appointed as both Minister of Justice and Minister of Trade and Industry").
- That he was RESPONSIBLE FOR RATIONING from 8 Oct 1951: NOT FOUND. No page I could read (he/en Wikipedia Dov Yosef, Trade ministry, Third/Second government, Austerity in Israel, Rationing and Supply Minister page) says so. Do not state it as fact. A defensible inference only: rationing/supply functions sat partly in the Trade and Industry ministry from Nov 1950 (A1), so Yosef inherited that ministry's remit; but this is inference, not a source. Also unverified: who held the agriculture-side food supply after Lavon (Levi Eshkol, Agriculture, from 8 Oct 1951).
- Update (issue #2, 2026-10-11): now sourced; see rationing-responsibility.md.
- Suggested next sources if needed: Knesset plenary records 1951-52 (fs.knesset.gov.il/2/Plenum/) for "שר המסחר והתעשייה" answering rationing queries; Orit Rozin, "Rationing and Austerity" works; Israel State Archives.
