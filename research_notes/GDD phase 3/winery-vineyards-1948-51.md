# The winery's city: a dated 1948-51 source for its vineyards (research note, 2026-10-16)

Status: research only, for issue #737 (GDD Phase 3 PR 6, Task 6.0). Nothing here is in `content/`. The owner decided the
winery goes to Rishon LeZion "subject to Task 6.0 finding a dated 1948 to 1951 source for the town's vineyards" (plan,
Q-SPEC-1). Task 6.0's rule: "If no candidate has a source, stop and report; do not pick one."

## Result

**No town has a source read on the page that dates its vineyards to 1948-51. Following the plan's rule, this note picks
no city.** The negative is not conclusive: the most likely primary sources (the Palestine Post on JPress, the
co-operative's own reports, the 1949/50 agricultural census) were not reachable from this machine or not found
online (see "Not reached").

What was found:

1. **National, [V]:** the CBS gives Jewish farming's wine-grape crop for 1948/49 to 1950/51, with no breakdown by town.
2. **Zikhron Ya'akov, strongest lead [Sn]:** a letter of 30 March 1951 to the wine growers' co-operative's council,
   "signed by sixty members residing in Zichron Yaacov", quoted in a 1966 history (Viteles). It dates co-operative
   members (the vintners' society) residing in the town to 1951, but it was read only as search snippets, and it gives
   no area or tonnage.
3. **Rishon LeZion, weak lead [Sn]:** a 1950 book for general readers (Hoffman) has the line "In this case it is largely
   viniculture", then "Rishon le Zion is famous for its wine cellars". The snippet does not show what "this case"
   refers to, the book does not date its observation, and Encyclopaedia Judaica says citrus became the town's main
   branch (before the First World War). Read as search snippets only.
4. Rehovot, Petah Tikva, Hadera, Netanya and Rosh Pinna: nothing dated 1948-51 ties them to vineyards. Jerusalem,
   Tel Aviv, Haifa, Tiberias and Safed were not searched (none is in the plan's list of wine-growing candidates).

## How to read this note

- **[V]** read on the page named, on the date given. **[Sn]** read as a full-text search hit on archive.org (the
  `service_backend=fts` search, which returns a few lines around each match); the book is lending-only and its page was
  not opened, so the page number and the wider context are unknown. **[C]** two sources disagree. **[S]** secondary or
  undated. **[G]** gap: no source reached. **[U]** a lead found but not opened (blocked from this machine).
- Searches used the built-in web search and fetch (no Tavily key on this machine). No request carried an owner email or
  other identifier.

## National wine-grape figures [V]

CBS Statistical Abstracts on the CBS website, `https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/`, read
2026-10-16. All three years are Jewish agriculture.

| Year | Wine grapes, produced | Marketed | Source |
|---|---|---|---|
| 1948/49 | 7,200 t | 6,913 t | SA No. 2 (1950/51), Table 7, p. 44 (`shnaton1951_num2.pdf`) |
| 1949/50 | 7,400 t | 7,180 t | SA No. 2 (1950/51), Table 7, p. 44 |
| 1950/51 | 6,250 t | 6,081 t | SA No. 3 (1951/52), Table 8, p. 51 (`shnaton1952_num3.pdf`) |

SA No. 2, Tables 2-3, and SA No. 3, Table 3 (the area tables) have no split by town. So the CBS shows that Jewish
vineyards existed in all three years, but not where.

## Per town

### Zikhron Ya'akov

- **[Sn] Viteles, Harry, *A History of the Co-operative Movement in Israel* (London: Vallentine, Mitchell, 1966),
  chapter "Vintners of the wine cellars".** https://archive.org/details/historyofcoopera0000vite_b5e9 (lending only).
  Snippets read 2026-10-16:
  - "Letter to Vigneronne's council of 30 March, 1951, signed by sixty members residing in Zichron Yaacov".
  - "(above quoted letter of 30 March, 1951). 'Zichron Yaacov is now entitled to five places on the council.'"
  - The co-operative is the "Vigneronne Co-operative Society of Vintners of the Wine Cellars of Rishon Le Zion and
    Zichron Yaacov".
  This is the only dated 1948-51 item found that places wine growers in a town. It is a primary letter quoted in a
  secondary book, read only as snippets, and it gives members, not vineyard area or grapes delivered.
- **[S]** JTA, 14 August 1952, "Zichron Yaacov Colony Celebrates 70th Anniversary",
  https://www.jta.org/archive/zichron-yaacov-colony-celebrates-70th-anniversary-of-its-existence: wine cellars, no
  vineyard figures, and dated 1952 (outside the window).
- **[S]** Jewish Film Archive, "Seventy Years of Carmel Mizrachi Wineries", Carmel Newsreel II-110, 6 December 1952,
  https://jfc.org.il/en/news_journal/27647-2/91958-2/ (read 2026-10-16): "Workers harvest grapes at the Carmel Mizrahi
  vineyards"; the page lists the location as Zikhron Yaakov. Footage of a Zikhron vintage, but dated 1952, after the
  window.
- **[S]** Zeligs, Dorothy F., *The Story of Modern Israel for Young People* (New York: Bloch, 1950),
  https://archive.org/details/storyofmodernisr00zeli: a character says "Zichron Yakov is noted for its vineyards and olive
  orchards" [Sn]. A children's narrative, not evidence.
- Zikhron Ya'akov is one of the twelve MVP towns (`content/map/settlements_1948.yaml`, type `town`).

### Rishon LeZion

- **[Sn] Hoffman, Gail, *The Land and People of Israel* (1950),** https://archive.org/details/bwb_P2-AFF-053 (lending
  only). Snippet read 2026-10-16: "...activity of agriculture. In this case it is largely viniculture. Rishon le Zion is
  famous for its wine cellars." The snippet does not show what "this case" refers to, so it may not be about
  the town's own farming. A book for general readers; it does not say when it observed this.
- **[C]** Encyclopaedia Judaica, 2nd ed., "Rishon Le-Zion", on encyclopedia.com, read 2026-10-16: "Part of the vineyards
  were therefore replaced by almond plantations" (after 1882-89) and "Citrus groves became the principal farming branch"
  (no year; the article places it before the First World War). It says nothing on the town's farming in 1948-51. It sits
  against "largely viniculture".
- **[S]** Halperin, Haim, *Changing Patterns in Israel Agriculture* (1957),
  https://archive.org/details/changingpatterns0000haim: "groves and the vineyards whose produce is processed in the huge
  wine-cellars of Rishon-Lezion" and land set "aside for the cultivation of wine grapes by growers connected with the
  Rishon-Lezion cellars" [Sn]. It describes the region in the mid-1950s; the snippets do not name 1948-51 or the town's
  own vineyards.
- **[S]** Viteles (above): the vintners of Rishon and Rehovot "in 1939 delivered only 2,200 kantars to the Rishon wine
  cellars" [Sn]. Pre-1948.
- **[Sn]** The Rishon wine cellars are "famous" in 1950 (Hoffman, above), and sixty Zikhron members of the co-operative
  of the Rishon and Zikhron cellars wrote to its council in March 1951 (Viteles, above). That points to the cellars
  working in 1948-51, but no source read says so outright, and it is a fact about the winery, not about vineyards in the
  town.

### Rehovot, Petah Tikva, Hadera, Netanya, Rosh Pinna

Nothing dated 1948-51 ties these towns to vineyards or grape deliveries [G]. Viteles names the vintners "of the moshavot
Rishon Le-Zion, Rehovot, Petah Tikva..." as delivering to the Rishon cellar, without a date in the snippet [Sn].

## Not reached

- **[U]** JPress (National Library of Israel; the Palestine Post and the Hebrew press 1948-51): HTTP 403 from this
  machine. A search there for the vintage (בציר) of 1949-51 in Rishon and Zikhron is the most likely way to close this.
- **[U]** The full text of Viteles (1966): lending only on archive.org (401 on the text file). Borrowing it would show
  the 1951 letter in context and whether it gives areas or tonnage.
- **[U]** winesisrael.com, "Wine in modern Israel since 1948" (`/en/159/`) and `/en/4098/`; adammontefiore.com `?p=4479`
  and `?p=4917` (the latter reportedly says the last vineyard did not leave Rishon "until well into the 1970's"): 403.
  Secondary in any case.
- **[G]** CBS agricultural census 1949/50 by settlement, the Israel Government Year Books 5710-5712 (archive.org has only
  5719, 1958), and the co-operative's annual reports: not found online.
- Trove (bot block), Google Books (quota), hamichlol: blocked.

## Candidates for #737

| Town | Best dated item | Grade | Meets Task 6.0? |
|---|---|---|---|
| Zikhron Ya'akov | Letter, 30 March 1951, sixty co-operative members residing there (Viteles 1966); vintage film 6 Dec 1952 | [Sn]; [S] | No, not read on the page; members, not vineyards; film after the window |
| Rishon LeZion | Hoffman 1950, "largely viniculture" (antecedent unclear; undated) | [Sn]; [C] with EJ (weak: EJ is pre-1914) | No |
| Rehovot, Petah Tikva, Hadera, Netanya, Rosh Pinna | none | [G] | No |

**Recommendation:** pick no city now (Task 6.0's rule) and keep PR 6's code blocked. Zikhron Ya'akov is the closest to
passing: one borrowed page of Viteles, or a JPress hit on the 1949-51 vintage there, would settle it. If the owner wants
Rishon LeZion anyway, the winery could rest on the town's wine cellars rather than its vineyards (those too are only
[Sn] for 1948-51), but that changes the GDD's "a winery requires areas for grapes" and is the owner's call.
