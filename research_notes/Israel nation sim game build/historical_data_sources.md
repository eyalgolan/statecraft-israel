# Historical Data Sources for the Yishuv and Israel (1880s–2026): Calibration Catalog for the Government-Sim Game

Researcher notes. Research date: 2026-10-09. Every figure has an inline source. Where sources disagree, I give the range. Items I could not verify this session are listed under each section's **Gaps** and should not be used as hard calibration targets until someone checks them.

---

## 1. Population (Ottoman/Mandate Palestine and Israel)

### Takeaway
The population baselines are well documented: 1878 Ottoman census, 1922 and 1931 Mandate censuses, the 1945 Survey of Palestine, then the CBS annual series. Israel had 10.178M people at the end of 2025 and about 10.305M by September 2026. The Jewish population of 1914 is genuinely contested, with estimates from 59k to 94k.

### Cited Findings
**Pre-state (Ottoman/Mandate)**
- 1878 Ottoman census: 403,795 Muslims, 43,659 Christians, 15,011 Jews who were Ottoman citizens, plus an estimated 5–10k foreign-born Jews. Total up to about 472k. McCarthy's 1880–81 registration gives about 399k Muslims, 14.7k Jews and 42.9k Christians (456,929 total). About 24,000 Jews lived there in 1882. — [Wikipedia: Demographic history of Palestine](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region)) (citing Mendel 2014, Dowty 2012, McCarthy)
- **1914 (contested):** McCarthy gives about 797k total (657k Muslim, 81k Christian, 59k Jews). DellaPergola/Bachi gives 689k total (525k Muslim, 94k Jews, 70k Christian). Another estimate puts Jews at 85k in 1914, falling to 56k in 1916–19 because of WWI expulsions and deaths. **Range for Jews: 59k–94k.** — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))
- 1920 British Interim Report: "hardly 700,000" in total, about 76k Jews and 77k Christians. — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))
- **1922 Mandate census:** 589,177 Muslims, 73,024 Christians, 83,790 Jews, 757,182 total. — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))
- **1931 Mandate census:** 759,717 Muslims, 91,398 Christians, 174,610 Jews, 1,035,821 total. The Wikipedia page has one table that swaps the Jewish and Christian figures, so check against the original census. — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))
- **1945 (Survey of Palestine):** 1,061,270 Muslims, 135,550 Christians, 553,600 Jews, 1,764,520 total. — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))
- 1947 (DellaPergola): about 1.181M Muslims, 143k Christians, 630k Jews, about 1.97M total. UNSCOP in 1948 gave about 1.9M, roughly 68% Arab and 32% Jewish. — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))
- Contested topic for game narrative: Arab in-migration during the Mandate. Bachi estimated net Arab migration of 40–42k for 1922–45. For 1922–31, estimates of the share of Arab growth due to immigration range from 7% to 38.7%. British reports attributed most Arab growth to natural increase. — [same](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region))

**State of Israel**
- End of 2025 (CBS): 10.178M total. 7.771M (76.3%) "Jews and others", 2.147M (21.1%) Arabs, 0.26M (2.6%) foreign nationals. Growth in 2025 was about 112k (1.1%): natural increase of about 132k (182k births, 50k deaths) and net migration of about −20k. — [Ratopati summarising CBS](https://english.ratopati.com/story/44341/israel-starts-2026-with-more-than-10-million-people); [JPost](https://www.jpost.com/israel-news/article-908103)
- 69,300 Israelis left in 2025 and about 19,000 returned. — [Ynet](https://www.ynetnews.com/article/1655f65xp)
- Pre-Rosh Hashanah 2026 (CBS): about 10.305M total, about 124k growth (1.2%), 7.828M "Jews and others" and 2.173M Arabs. — [The Media Line / CBS](https://themedialine.org/headlines/cbs-israels-population-approaches-10-million-ahead-of-jewish-new-year)
- The total population first topped 10M around April 2025. — [Jewish News](https://www.jewishnews.co.uk/topic/central-bureau-of-statistics-cbs/)
- Definition caveat: CBS's "Jews and others" includes non-Arab Christians and people not classified by religion, many of them FSU immigrants under the Law of Return. Model the game's "Jewish" population on this category, or split out "others". — [CBS reporting via search summary](https://themedialine.org/headlines/cbs-israels-population-approaches-10-million-ahead-of-jewish-new-year)

### Inferences
- Calibration anchors for the population model: Jews about 24k in 1882 → 84k in 1922 → 175k in 1931 → 554k in 1945 → about 630–650k in 1948. This implies very high immigration-driven growth in the 1930s (the Fifth Aliyah) and lets the game check its immigration and natural-increase parameters.
- In 2024–25, net migration turned negative. A game model for 2023+ needs an emigration term sensitive to war and politics, not only aliyah.

### Gaps
- I did not fetch the 1945 Village Statistics (Hadawi/Mandate) directly. The Wikipedia figures come from the Survey of Palestine.
- I could not retrieve a full CBS annual table (Jews/Arabs by year, 1948–2025) in this session. The Statistical Abstract of Israel, chapter 2 ("Population"), table 2.1 is the canonical source. It is published annually at cbs.gov.il.
- The Jewish Virtual Library vital statistics page returned no content when fetched.

---

## 2. Aliyah Waves (numbers by wave/year, origins)

### Takeaway
There is a full CBS annual immigration series for 1948–2025, mirrored on JVL, totalling about 3.52M. The pre-state waves are known only as rounded estimates. The biggest spikes were 1949 (240–250k), 1990 (199.5k) and 1991 (176k), with a war-driven bump in 2022 (74.4k).

### Cited Findings
**Pre-state waves (rounded estimates)**
- First Aliyah (1882–1903): about 35,000. — [Wikipedia: Aliyah](https://en.wikipedia.org/wiki/Aliyah)
- Second Aliyah (1904–1914): 35–40k. — [same](https://en.wikipedia.org/wiki/Aliyah)
- Third Aliyah (1919–1923): about 40k. — [same](https://en.wikipedia.org/wiki/Aliyah)
- Fourth Aliyah (1924–1929): 82k. — [same](https://en.wikipedia.org/wiki/Aliyah)
- Fifth Aliyah (1929–1939): about 250k, of whom 174k arrived in 1933–36. — [same](https://en.wikipedia.org/wiki/Aliyah)
- Aliyah Bet (illegal immigration, 1934–48; some sources date it from 1933): about 110k. — [same](https://en.wikipedia.org/wiki/Aliyah)

**Annual series, 1948–2025** (CBS, as reproduced by [JVL "Total Immigration to Israel by Year"](https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year))
- 1948–57: 101,828; 239,954; 170,563; 175,279; 24,610; 11,575; 18,491; 37,528; 56,330; 72,634
- 1958–67: 27,290; 23,988; 24,692; 47,735; 61,533; 64,489; 55,036; 31,115; 15,957; 14,469
- 1968–77: 20,703; 38,111; 36,750; 41,930; 55,888; 54,886; 31,979; 20,028; 19,754; 21,429
- 1978–87: 26,394; 37,222; 20,428; 12,599; 13,723; 16,906; 19,981; 10,642; 9,505; 12,965
- 1988–97: 13,034; 24,050; 199,516; 176,100; 77,057; 76,805; 79,844; 76,361; 70,919; 66,221
- 1998–2007: 56,730; 76,766; 60,201; 43,473; 33,570; 23,273; 20,899; 21,183; 19,269; 18,131
- 2008–17: 13,701; 14,575; 16,635; 16,893; 16,560; 16,929; 24,120; 27,908; 25,977; 28,598
- 2018–24: 30,300; 34,000; 19,713; 25,497; 74,400; 46,033; 32,161
- 2025: 22,522. Total 1948–2024: 3,494,321. Total 1948–2025: 3,516,843.
- Discrepancy: 1949 is 239,954 in the JVL table but 249,954 in the Wikipedia Aliyah article. The Wikipedia article says "more than 680,000" arrived in 1948–51, and its 1948–53 table totals 723,090. — [Wikipedia](https://en.wikipedia.org/wiki/Aliyah) vs [JVL](https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year)

**Specific operations and waves**
- 1970s Soviet aliyah: about 163k Soviet Jews came to Israel, mostly in 1969–73. Another source gives about 164k for 1970–88, out of about 291k emigrants in total. — [Wikipedia: 1970s Soviet Union aliyah](https://en.wikipedia.org/wiki/1970s_Soviet_Union_aliyah); [JPR archive](https://archive.jpr.org.uk/download?id=3233)
- 1990s FSU wave: of about 1.6M Jews and relatives who left the (former) USSR in 1989–2006, about 979k (61%) went to Israel. Other sources say "about 800k in the 1990s" or "over a million from the 1970s to the 2000s". — [Wikipedia: 1990s post-Soviet aliyah](https://en.wikipedia.org/wiki/1990s_post-Soviet_aliyah); [JPost](https://jpost.com/opinion/article-750015)
- Operation Moses (1984–85, via Sudan): estimates range from 6,500 to 8,000. — [Wikipedia: Operation Moses](https://en.wikipedia.org/wiki/Operation_Moses); [ReformJudaism.org](https://reformjudaism.org/history-jewish-immigration-israel-aliyah)
- Operation Solomon (24–25 May 1991): 14,325 Jews in about 36 hours on 34 aircraft. A contemporary 1991 newspaper gave 14,087. — [UJS](https://www.ujs.org.uk/operation_solomon); [Detroit Jewish News 1991](https://digital.bentley.umich.edu/djnews/djn.1991.05.31.001/25); [JPost](https://www.jpost.com/aliyah/article-775954)
- Post-2022 wave: 74,400 arrived in 2022, after which numbers declined. Russia arrivals were about 19.5k in 2024 and about 8.3k in 2025 (−57%). Ukraine arrivals were about 980 in 2024 and 805 in Jan–Nov 2025. France rose from about 2,200 to about 3,300 in 2025 (its 2015 peak was about 7,900). US arrivals in 2025 were about 3.5–4.15k depending on the source. UK arrivals were 840 (+19%). Western aliyah more than doubled, from 4,570 to 9,256 in Jan–Nov. The 2024 total is reported as 31–32.8k depending on the source. — [JNS](https://www.jns.org/analysis/western-aliyah-rises-even-as-overall-immigration-falls); [Times of Israel FR](https://fr.timesofisrael.com/lalyah-en-provenance-de-france-en-hausse-de-45/); [Jewish News UK](https://www.jewishnews.co.uk/uk-aliyah-up-by-almost-20-in-2025-new-figures-show/)
- Retention caveat: a sizeable share of the post-2022 Russian olim took citizenship and then moved on to Europe or back to Russia. — [David Fishman Substack](https://davidfishman.substack.com/p/the-war-in-ukraine-jewish-news-45)

### Inferences
- The JVL/CBS annual series can drive an immigration "event" model directly. Key shocks to reproduce are 1948–51, the 1955–57 North Africa wave, the 1969–73 and 1990–91 Soviet waves, the 2022 Ukraine/Russia wave, and the post-2023 decline with rising emigration.

### Gaps
- I did not pull numbers for Operation Magic Carpet (Yemen, about 49k is the commonly cited figure, unverified) or Ezra and Nehemiah (Iraq, about 120–130k commonly cited, unverified).
- I did not obtain a CBS origin-by-continent breakdown per year. It is CBS Statistical Abstract chapter 4 ("Immigration"), not verified this session.

---

## 3. Economy (GDP, inflation, defense, US aid, reparations, budget)

### Takeaway
Maddison (CC BY 4.0) is the best long-run GDP/capita source, covering years up to 2022. Bank of Israel and CBS cover modern detail. Headline calibration targets: inflation of about 450% in 1984, the stabilisation plan of 1 July 1985, defense at about 30% of GDP in the mid-1970s, defense at 8.8% of GDP in 2024 (SIPRI), US aid of $158B cumulative nominal (CRS), and German reparations of DM 3B to Israel (1952).

### Cited Findings
- Maddison Project Database 2023 covers GDP per capita and population for 169 countries up to 2022. Its license is CC-BY-4.0 (DataverseNL, DOI 10.34894/INZBF2). The authors ask that the original papers be cited when showing graphs or analysing subsets of fewer than 12 countries. — [B2FIND record](https://b2find.eudat.eu/dataset/5af947f7-57b4-5a20-a87b-76c0d9ee9300); [QoG datafinder](https://datafinder.qog.gu.se/dataset/mad)
- The R package `maddison` is CC0 but contains the older 2018 version of the data. — [CRAN vignette](https://cran.r-project.org/web/packages/maddison/vignettes/maddison_project_database.html)
- Inflation: the years after 1973 were a "lost decade". The bank-stock crisis hit in 1983. Annual inflation was about 450% in 1984 and was projected to exceed 1,000% in 1985. The Peres government's stabilisation plan (Moda'i and Bruno) began on **1 July 1985**. Its measures were spending and deficit cuts, a wage pact with the Histadrut, temporary price controls, devaluation then a fixed exchange rate, and limits on BoI deficit monetisation. Inflation fell below 20% within two years. — [Wikipedia: 1985 Israel Economic Stabilization Plan](https://en.wikipedia.org/wiki/1985_Israel_Economic_Stabilization_Plan)
- **Defense spending as % of GDP (contested for the 1970s):** about 8.5% in 1950. About 30% in 1975 (Rivlin, Cambridge). JPC gives averages of 21% for 1968–72 and 26% for 1974–81. JCPA claims a 35% peak after 1973. MERIP says about 37% of the *government budget* in 1971–80, which is not comparable to a GDP share. The share fell to about 8% by 2009. — [Breaking Defense / SIPRI](https://breakingdefense.com/2025/04/israeli-defense-spending-jumped-in-2024-part-of-overall-rise-in-middle-east-sipri/); [Cambridge (Rivlin)](https://www.cambridge.org/core/product/14ED6D804849AB551B053EFB8A418B1D); [JPC](https://www.jewishpolicycenter.org/2019/01/11/israel-americas-ally-by-the-numbers/); [JCPA](https://jcpa.org/?p=19433); [MERIP](https://www.merip.org/1980/11/challenge-from-israels-military.md)
- 2024 (SIPRI): spending rose 65% to $46.5B, or 8.8% of GDP, the largest annual jump since 1967 and second-highest burden worldwide. A later SIPRI listing shows about 7.8% for 2025. The low of the past 25 years was about 4.4% in 2022 (World Bank data). — [Breaking Defense](https://breakingdefense.com/2025/04/israeli-defense-spending-jumped-in-2024-part-of-overall-rise-in-middle-east-sipri/); [Statistics of the World](https://statisticsoftheworld.com/country/israel/military-spending)
- **US aid:** $158B cumulative (current dollars) in bilateral aid plus missile-defense funding. This makes Israel the largest cumulative recipient since WWII (CRS RL33222). The 2014 CRS edition gave $121B. CFR's inflation-adjusted figure is over $300B. The 2019–2028 MOU is $38B ($33B FMF plus $5B missile defense). — [CRS RL33222](https://www.congress.gov/crs_external_products/RL/PDF/RL33222/RL33222.39.pdf); [EveryCRSReport 2014](https://www.everycrsreport.com/files/20140411_RL33222_f9d4688164adbb5262f549cbda2faa9b31e184f2.html); [CBS12 / CFR](https://cbs12.com/news/nation-world/fact-check-team-how-much-aid-has-the-us-given-israel-council-foreign-relations-united-states)
- **Reparations (Luxembourg Agreement):** signed 10 Sept 1952, in force 27 Mar 1953. DM 3B to Israel plus DM 450M to the Claims Conference, paid as goods and services over about 12–14 years. The NLI blog says 3.5B marks, which is likely a conflation. Total German payments to survivors and programs are over $70B equivalent. — [NLI blog](https://blog.nli.org.il/en/?p=95369); [en-academic (Wikipedia mirror)](https://en-academic.com/dic.nsf/enwiki/460146); [JTA](https://www.jta.org/?p=445799)

### Inferences
- For the game's economy model, use Maddison GDP/capita (with free commercial use under CC BY plus attribution) as the long-run spine to 2022. Splice in CBS or World Bank figures for 2023–25.
- Defense burden is a key state variable. Suggested path: about 8–9% (1950s) → 20–30% (1968–81) → about 5–6% (2000s–2010s) → about 4.4% (2022) → 8.8% (2024). The 1970s peak should be a range, not a point.

### Gaps
- I did not verify specific Bank of Israel series URLs, CBS budget-by-ministry tables, or the annual CPI series in this session. Ministry of Finance budget documents and the BoI annual report are the expected sources.
- Monthly hyperinflation peak and the shekel redenominations (IL→shekel 1980, →NIS 1985): not verified here.
- I have no Maddison Israel values in hand. They need a download.

---

## 4. Elections (Knesset 1949–2025, Yishuv Assembly)

### Takeaway
No single open machine-readable dataset of all Knesset results since 1949 turned up. JVL has per-election pages with votes and seats for the 1st to 25th Knessets. The Central Elections Committee (gov.il) has official recent results. The 26th Knesset election is scheduled for **27 Oct 2026** (after this research date).

### Cited Findings
- JVL per-election pages give votes and seats by party. Examples: 1949 Mapai 46, Mapam 19, United Religious Front 16, 434,684 valid votes, 1% threshold. 1951 Mapai 45 (37.3%). 1955 Mapai 40, Herut 15. 1977 Likud 43 (33.4%) vs Alignment 32. 1988 Likud 40, Alignment 39. 2003 Likud 38. 2006 Kadima 29. 2013 Likud-Beiteinu 31, Yesh Atid 19. 2022 Likud 32. No party has ever won 61 seats. — [JVL electoral history index](https://www.jewishvirtuallibrary.org/israeli-electoral-history); [1st Knesset](https://www.jewishvirtuallibrary.org/jsource/Politics/knesset49.html)
- Official Central Elections Committee results pages exist for recent Knessets. — [gov.il results](https://www.gov.il/apps/elections/eng/list/Results_eng.html); [19th Knesset](https://www.gov.il/apps/elections/elections-knesset-19/eng/list/Results_eng.html)
- Polling-station-level data for the 16th to 25th Knessets is available from renavon.com (login required). — [renavon](https://renavon.com/data/pdm/bechirot)
- IDI publishes election analyses, e.g. Arab vs Jewish party vote 1999–2021, rather than a full downloadable dataset. — [JVL citing IDI](https://www.jewishvirtuallibrary.org/arab-vote-in-knesset-elections)
- 2026: the 25th Knesset was dissolved on 17 July 2026 (62–0), and the election is on 27 Oct 2026. It is the first on-schedule election since 1988, after five elections between April 2019 and November 2022. — [Wikipedia: 2026 Israeli legislative election](https://en.wikipedia.org/wiki/2026_Israeli_legislative_election); [Israel Hayom](https://www.israelhayom.com/2026/07/12/israels-next-official-election-date-set)
- Yishuv Assembly of Representatives: elections on 19 Apr 1920, 6 Dec 1925, 5 Jan 1931 and 2 Aug 1944. In 1920 Ahdut HaAvoda was the largest faction with 70 of 314 seats, and the Haredim/Agudat Yisrael boycotted. In 1944 some Sephardi groups boycotted, and Hatzohar (Revisionists) boycotted from 1944 on. — [Wikipedia: Assembly of Representatives](https://en.wikipedia.org/wiki/Assembly_of_Representatives_(Mandatory_Palestine))

### Inferences
- The game will probably need a hand-built CSV (Knesset number, date, party, votes, seats, coalition membership) scraped from JVL and cross-checked against the Knesset/CEC. Election results are facts, so copyright risk is low, but scraped page layouts should not be redistributed verbatim.

### Gaps
- I did not locate the knesset.gov.il historical results URL or any official coalition/government composition dataset. The Knesset site has lists of "Governments of Israel", but this is unverified.
- Seat/vote results for the 1925, 1931 and 1944 Assembly elections were not found.
- The ParlGov and Döring-Manow datasets (which do contain Israel cabinets and elections under CC licenses) were not checked this session.

---

## 5. Security (casualties per war and terror waves)

### Takeaway
Israeli figures are fairly consistent per war. Cumulative "fallen" totals differ by cutoff and definition. Palestinian and Arab casualty figures, especially for the 2023–25 Gaza war, are highly contested and should be presented as ranges with attribution.

### Cited Findings (Israeli deaths, military/civilian) — [Wikipedia: Israeli casualties of war](https://en.wikipedia.org/wiki/Israeli_casualties_of_war) unless noted
- 1920 Nebi Musa riots 5. 1921 Jaffa riots 47. 1929 riots 133. 1936–39 Arab revolt 415+ civilians. 1947–48 civil war 895 military plus about 408 civilians.
- 1948 war: about 6,400 (4,000 military, 2,400 civilian), the split from Garfinkle, *Politics and Society in Modern Israel* (2000), p. 61, as cited on English Wikipedia ("Israeli casualties of war"; book not opened). JVL's IDF table gives only 6,373 killed for 1947–49. — [JVL casualty table](https://www.jewishvirtuallibrary.org/jsource/History/casualty_table.html)
- 1956 Sinai: 172. 1967: 776 military plus 20 civilian. War of Attrition: 1,424 military plus 227 civilian.
- **1973: 2,656** (Wikipedia) vs **2,688** (JVL IDF table). This is a minor discrepancy, probably a different cutoff. — [JVL](https://www.jewishvirtuallibrary.org/jsource/History/casualty_table.html)
- 1982–85 Lebanon: 657 military plus 10 civilian (Wikipedia) vs 1,216 for 1982–85 in the JVL table, which likely counts all causes. The 1985–2000 security zone saw 256 military deaths.
- First Intifada: 60 military plus 100 civilian. Second Intifada (2000–08): 332 security personnel plus 731 civilians = 1,063. The MFA/ADL figure for Sept 2000–Aug 2010 is 1,194.
- 2006 Lebanon: 121 military plus 44 civilian. 2008–09 Gaza: 13. 2014 Gaza: 67 plus 5. 2021: 1 plus 10.
- Gaza war (as of early 2025): 829 military and police plus 888 civilians, including 700+ civilians on 7 Oct. Hezbollah front: 87 plus 46.
- 7 October 2023 death toll: about 1,200 (AFP tally 1,210; other reports 1,221). — [Search summary of news reports](https://citizen.digital/article/death-toll-from-gaza-war-surpasses-70000-health-ministry-n373768)
- The IDF published a summary of casualties over two years of war. In 2025, 151 soldiers died, 88 of them in operations. — [IDF](https://www.idf.il/en/mini-sites/idf-press-releases-israel-at-war/january-25-pr/summary-of-idf-casualties-over-the-past-two-years); [Yahoo/News](https://www.yahoo.com/news/articles/israel-says-151-soldiers-lost-180656403.html)
- Cumulative totals: 20,093 soldiers killed 1948–97 (MFA). 22,684 fallen since 1860 plus 3,971 terror victims (Yom HaZikaron 2010). The official figure for Yom HaZikaron 2021 is 23,928. The JVL table total of 23,320 does not match the sum of its rows. — [Wikipedia](https://en.wikipedia.org/wiki/Israeli_casualties_of_war); [JVL](https://www.jewishvirtuallibrary.org/total-casualties-arab-israeli-conflict)
- **Gaza Palestinian deaths (contested):** the Gaza MoH reported about 70,100 by late Nov 2025 and over 73,000 by June 2026, including about 1,000 killed after the 10 Oct 2025 ceasefire. The MoH does not distinguish combatants from civilians. The UN treats its figures as reliable, while Israel has disputed them. The Lancet (2025) estimated 75,200 violent deaths up to 5 Jan 2025 plus 16,300 indirect deaths. That estimate was challenged by demographers, including DellaPergola and Zlochin, as inflated. A speculative 2024 Lancet letter put deaths at "up to 186,000" including indirect causes. — [CTV](https://www.ctvnews.ca/world/article/death-toll-from-gaza-war-surpasses-70000-health-ministry/); [OPB](https://www.opb.org/article/2026/06/18/over-1000-people-killed-during-gaza-ceasefire-palestinian-authorities-say/); [Common Dreams on Lancet](https://www.commondreams.org/news/lancet-study-gaza-death-toll); [Ground News on critiques](https://ground.news/article/lancet-gaza-death-toll-study-faces-bias-claims_c32b10)
- Several rows in the Wikipedia casualty article carry "citation needed" tags, and the article is flagged for cleanup. — [Wikipedia](https://en.wikipedia.org/wiki/Israeli_casualties_of_war)

### Inferences
- For the game, use per-war Israeli deaths as calibration targets for the "war event" severity model. Present enemy and Palestinian casualties as ranges with attribution (Israeli source / Gaza MoH / academic), never as single numbers.

### Gaps
- I did not directly access the B'Tselem statistics database or the OCHA casualty dataset (OCHA "Data on casualties", 2008–present). Both are known to exist, but their URLs and licenses were not verified this session.
- Arab state casualties per war were not collected.

---

## 6. Other Domains (health, education, infrastructure, tourism, foreign relations)

### Takeaway
Tourism and Abraham Accords dates are clear. Health indicators are best taken from the UN WPP / World Bank / OWID series rather than secondary sites. Education and infrastructure dates were not researched in depth.

### Cited Findings
- **Tourism:** a record 4.55M arrivals in 2019 (4,551,600, +10% over 4.12M in 2018). About 3M in 2023. 961k in 2024. About 1.3M in 2025 (Tourism Ministry preliminary; top markets US about 400k, France 159k, UK 95k). — [ITN 2019](https://itn.co.il/news/updates/israel-sees-all-time-record-in-tourist-arrivals-in-2019); [JNS 2025](https://www.jns.org/1-3-million-tourists-visited-israel-in-2025/); [JPost](https://jpost.com/israel-news/article-881915)
- **Health:** infant mortality fell sevenfold between 1955 and 2000 and stood at 5.4 per 1,000 in 2000 (MoH study). The 2024 figure is about 2.7–2.9 per 1,000 (UN IGME / WPP 2024). Life expectancy in 2024 was about 83.2 (women 85.4, men 81.1; UN Population Division). — [Haifa/BIU CRIS](https://cris.biu.ac.il/en/publications/infant-mortality-in-israel-during-1950-2000-rates-causes-demograp/); [FRED World Bank IMR series](https://fred.stlouisfed.org/data/SPDYNIMRTINISR); [The Global Economy](https://www.theglobaleconomy.com/Israel/life_expectancy_female/)
- **Abraham Accords:** UAE and Bahrain signed on 15 Sept 2020. Morocco established ties in Dec 2020. Sudan signed the declaration in Jan 2021, but formal relations have stalled. Kazakhstan joined on 6 Nov 2025. Saudi Arabia is the leading candidate but has not joined. — [MEI Backgrounder 2025](https://mei.edu/wp-content/uploads/2026/01/Abraham-Accords_Backgrounder_2025.pdf); [Heritage](https://www.heritage.org/abraham-accords/key-developments)
- Count of countries recognising Israel: a July 2026 headline says 164 of 193 UN members. This is unverified in the body text. — [Jewish Link](https://jewishlink.news/israel-recognition-164-countries-down-29-to-go/)

### Gaps
- I did not verify these with cited sources this session: university founding dates (Technion, Hebrew University, Weizmann, TAU, BGU, Bar-Ilan, Haifa), the National Water Carrier opening (commonly 1964), electrification (Rutenberg concession, 1920s), rail and road milestones, the Egypt (1979) and Jordan (1994) treaties, and a full dated table of diplomatic relations. They need a follow-up pass. Wikidata (CC0) is suitable for founding dates and diplomatic-relations properties.
- I found no reliable 1950 life expectancy figure.

---

## 7. Open APIs, Machine-Readable Sources and Licensing for Commercial Game Use

### Takeaway
CBS data is explicitly licensed for commercial use with attribution. Maddison is CC BY 4.0. Most Israeli official statistics are therefore usable in a commercial game, provided the required credit line is included and logos and personal data are avoided.

### Cited Findings
- **CBS Open License:** a worldwide, royalty-free, perpetual, non-exclusive license allowing commercial and non-commercial use, including copying, adapting and publishing in any medium. Attribution is required in this form: "The Central Bureau of Statistics (CBS), [file/product], [(date of access)], available online: [link], copied and distributed as is, according to the CBS license at the website [terms link]". Attribution is *not* required when extracted data does not preserve the originality of the source's selection or arrangement. Exclusions: personal data, logos, third-party material, authored working papers, photos, trademarks, and site code. Users may not misrepresent the data or imply endorsement. Data is provided "AS IS". The terms can change, and the version in force at the time of access governs. — [CBS Open License](https://www.cbs.gov.il/en/Pages/Enduser-license.aspx)
- **CBS API:** a central database plus SDMX time series. — [CBS API interface](https://www.cbs.gov.il/en/Pages/API-interface.aspx). Unofficial MCP wrappers exist (e.g. "israel-statistics-mcp"). Their MIT licenses cover the code, not the data. — [civictech directory](https://directory.civictech.guide/listing/israel-statistics-mcp)
- **Maddison 2023:** CC-BY-4.0, DOI 10.34894/INZBF2. — [B2FIND](https://b2find.eudat.eu/dataset/5af947f7-57b4-5a20-a87b-76c0d9ee9300)
- **Our World in Data:** indicator metadata is available via an API at api.ourworldindata.org, and OWID republishes Maddison and other series. — [OWID indicator metadata](https://api.ourworldindata.org/v1/indicators/1295094.metadata.json)
- **World Bank WDI series** (e.g. IMR SP.DYN.IMRT.IN) are mirrored on FRED. — [FRED](https://fred.stlouisfed.org/data/SPDYNIMRTINISR)
- **US aid:** CRS reports are US government works (public domain). — [CRS RL33222](https://www.congress.gov/crs_external_products/RL/PDF/RL33222/RL33222.39.pdf)

### Inferences
- Recommended stack: CBS (SDMX API) for population, migration, CPI and national accounts from 1948; Maddison for GDP/capita back to 1870s–1920s Palestine (if covered) or as Israel-only from 1950; SIPRI for defense from 1949 (SIPRI's terms need checking); World Bank/OWID for health; Wikidata (CC0) for dates. JVL is good as a cross-check, but it is a secondary aggregator with mixed-provenance tables and an inconsistent casualty total, so don't redistribute its tables verbatim.

### Gaps
- Not verified this session: the specific licenses of the World Bank (believed CC BY 4.0), OWID (believed CC BY 4.0 for its own work, with third-party data under original terms), SIPRI Milex (believed free for non-commercial use with attribution, so commercial use may need permission), data.gov.il (believed to use an open license), and the Bank of Israel series database terms. These should be confirmed before shipping.
- B'Tselem and OCHA data terms were not checked.
