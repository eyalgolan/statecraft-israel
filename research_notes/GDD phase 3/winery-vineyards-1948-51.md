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

## Second source check for issue #1928 (agent read 2026-10-31; a person's check pending)

This section adds to the note above and does not replace it. An AI agent ran it from a machine with no library access. It
is not the person's check that issue #1928 asks for: no page of Hoffman, Halperin or Viteles was opened (all three are
lending-only on archive.org, and the full text returns HTTP 401 without a borrowing account). What follows is what could
be read without one.

### What was read

- **[V, agent read 2026-10-31; a person's check pending]** JTA Daily News Bulletin, 27 July 1950, page 5 of
  `http://pdfs.jta.org/1950/1950-07-27_143.pdf` (page image read; the dateline on the item is "NEW YORK, July 26. (JTA)").
  Headline "ISRAEL'S WINE EXPORT TO UNITED STATES WILL BE GREATLY INCREASED, JEWISH AGENCY SAYS". Printed text: "35,000
  cases, representing a retail value of $900,000, are to be shipped from the famous Rishon Wine Cellars to the Israel
  Middle East Corporation in New York" and "an increase of approximately $700,000 over wine exports to the U.S. during
  1949." Web copy: `https://www.jta.org/archive/israels-wine-export-to-united-states-will-be-greatly-increased-jewish-agency-says`.
  This dates the cellars' activity to 1949-50. It says nothing about vineyards or where the grapes grew.
- **[Sn, agent read 2026-10-31; a person's check pending]** Hoffman (1950), the antecedent of "this case". The archive.org
  full-text search returns about one line around each match and up to five matches per book. Overlapping lines from
  seven searches (for example `"first wave of early pioneers"`, `"wave of early pioneers, is another of"`, `"chief
  activity of agriculture"`, `"largely viniculture"`, `"vaulted caverns"`) join into one passage, in the 1950 scan
  `bwb_P2-AFF-053`: "Rishon le Zion (First to Zion), also started by the first wave of early pioneers, is another of
  the towns with industrial plants supplementing the chief activity of agriculture. In this case it is largely
  viniculture. Rishon le Zion is famous for its wine cellars. Here, in great vaulted caverns, carefully constructed below
  the grape-pressing rooms, are 105 giant tanks with glazed interiors". So "this case" is Rishon LeZion's own chief
  agricultural activity, and the earlier worry that it might refer to another town is answered. The 1950 scan's index
  (also a snippet) lists "Rishon le Zion, 109". The same sentences come back from a second scan, `landpeopleofisra0000gail`
  (archive.org dates it 1955), so the passage was not written for the 1948-51 window and is undated inside it. The page
  was not opened: the order of the lines rests on their overlap, and the page number is the index's, not read on a page.
- **[Sn, agent read 2026-10-31; a person's check pending]** Halperin (1957), the same technique on
  `changingpatterns0000haim`: "vineyards whose produce is processed in the huge
  wine-cellars of Rishon-Lezion" and land "set aside for the cultivation of wine grapes by growers connected with the
  Rishon- Lezion cellars" (the scan prints "Rishon- Lezion" with a break). The lines around these sentences do not name
  the district they describe, so the snippets do not show that it is the town's own land. The sentences tie the
  vineyards and the growers to the cellars, not to the town. The second sentence reads in full in the scan: "In recent
  years special areas have been set aside for the cul- tivation of wine grapes by growers connected with the Rishon-
  Lezion cellars." The only dating in it is "In recent years", which counted back from 1957 is the closest thing to a
  date in the passage; the person checking should look on the page for a year. Re-read 2026-10-31 on the same
  archive.org search.
- **[S, agent read 2026-10-31; a person's check pending]** English Wikipedia, "Carmel Winery",
  `https://en.wikipedia.org/w/index.php?title=Carmel_Winery&oldid=1328303280` (revision of 19 December 2025). It says
  of Edmond de Rothschild: "His first vineyards were planted near Rishon LeZion", with French rootstock imported in 1882
  and a large wine cellar begun in Rishon LeZion, and that the Rishon LeZion Winery was built in 1890. After the First
  World War "Many of the vineyards were uprooted and replanted with citrus trees"; "during the Second World War, the
  industry began to grow again". For today it says the Central Coastal Plain and the Judean Lowlands region "supplies the Rishon
  LeZion Winery". Nothing on it dates a Rishon vineyard to 1948-51. In the raw wikitext of this revision the History and Wineries
  sections (and the Overview and Vineyards sections) carry a "section unsourced" tag dated December 2023, which fits the
  [S] grade. The resource comment in `content/cities.yaml` says
  "Carmel Winery's first vineyards"; the article says Rothschild's first vineyards.
- **JTA search, none found.** Run 2026-10-31 on `https://www.jta.org/wp-json/wp/v2/archive?search=<term>&after=1948-05-14&before=1951-12-31`,
  with the terms `Rishon`, `Rishon-le-Zion`, `Rishon Lezion`, `Rishon LeZion`, `Rishon le Zion`, `Rishon wine cellars`,
  `vineyards Israel` and `vintage Israel wine`, up to three pages of 100 each. The searches returned 398 distinct items.
  The text of 11 contains "Rishon" (4 June 1948 to 5 December 1951); none of the 11 contains "vineyard", "grape",
  "vintage" or "viticult". One of them is the 27 July 1950 item above (the only mention of the cellars). The search
  matches loosely, so the 398 is what came back, not a count of everything JTA holds.

### Claim to evidence

| Claim in the vineyard resource or this note | Evidence read | Dated 1948-51? | Grade |
|---|---|---|---|
| Rishon LeZion had wine cellars in 1948-51 | JTA 27 July 1950 (page image) | Yes: the bulletin is 27 July 1950 and the dateline 26 July 1950 | [V, agent read 2026-10-31; a person's check pending] |
| The cellars shipped wine to the US in 1949-50 | Same item: 35,000 cases, $900,000 retail, about $700,000 up on 1949 | Yes (announcement, not a delivery record) | [V, agent read 2026-10-31; a person's check pending] |
| The town's chief agricultural activity is largely viniculture | Hoffman 1950, one sentence stitched from search lines | No: a 1950 book, present tense, same text in a 1955 scan | [Sn] |
| Vineyards connected to the cellars | Halperin 1957 | No: 1957, district not named | [Sn, agent read 2026-10-31; a person's check pending] |
| Rothschild's first vineyards were planted near the town | Wikipedia, oldid 1328303280 | No: 1880s | [S, agent read 2026-10-31; a person's check pending] |
| Vineyards stayed in the town after the First World War | Encyclopaedia Judaica (above): vineyards partly replaced, citrus principal | No: earlier than 1914 | [C] with Hoffman |
| The town's own vineyards existed in 1948-51 | Nothing read dates it | Not found | [G] |

Winery activity is a separate claim from vineyards in the town, and grapes brought from elsewhere are a third. The
sources read support the first (1950, [V]) and say the second only in an undated present tense. None supports the third
or rules it out.

### Conclusion

The current claim is **not supported** by anything read that is dated to 1948-51. What is missing is a page, from 1948-51
or close to it, that places vineyards in or beside Rishon LeZion (the Palestine Post or Hebrew press on JPress, the
co-operative's reports, the 1949/50 agricultural census by settlement, or the context of Hoffman p. 109 opened in the
book). A person with a borrowing account opening Hoffman p. 109, Halperin and Viteles is the next step.

Proposed wording for a person to review (nothing has been changed in `content/`): "Rishon LeZion's wine cellars were in
operation and exporting in 1950 (JTA, 27 July 1950). A 1950 book calls the town's farming largely viniculture, but it is
undated within 1948-51 and no source read dates the town's own vineyards to 1948-51." The `to_verify` line in
`content/cities.yaml` stays as it is until a person has done the check; this agent read narrows nothing.

Not reached again: JPress and the Israeli archives (as above; not retried, to keep headers unchanged). The Viteles
snippets were not re-read for this check.

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
