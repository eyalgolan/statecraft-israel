# The railway and yishuvim cards: a search for day-of photographs (research note, 2026-10-17)

Status: research only, for the owner's choice (Q-CARD-1). Nothing here is in `content/` or `unity/`. Refs #791 (GDD
Phase 4 PR 7, Task 7.0) and #782 (the Phase 4 owner answers). Companion to
`burma-road-photographs.md` (#835, not yet on main), which covers the third card.

Q-CARD-1's recommended answer in the [Phase 4 plan](../../docs/superpowers/plans/2026-10-15-gdd-phase-4.md) (line 177)
says: "Railway: a Boris Carmi "Israel Railways" frame (Meitar Collection, NLI, 1949, CC BY 4.0), with the alt text
"Israel Railways, 1949", not tied to the day. Yishuvim: Phase 1's Mefalsim photograph." The objective-cards note
([objective-cards.md](../GDD%20objectives/objective-cards.md), section 3) found the Carmi series and refused the two
photographs of 20 September 1949 it found (PD-Israel only; GPO). It did not record a search of the Meitar Collection
for a frame of the day itself, and nobody has looked for photographs of the other three yishuv sites (Q-SITE-1: Beit
Kama, Re'im, Gilat). This note does both.

**Result: not found, for both cards.** No CC BY 4.0, CC0 or public-domain-with-a-US-basis photograph of the 20
September 1949 rail opening is on Commons, and the Meitar Collection has no founding photograph of Beit Kama, Re'im or
Gilat. Q-CARD-1's recommendation stands as written: the undated Carmi frame for the railway, Mefalsim for the yishuvim.

How it was done:
- Read on 17 October 2026 through the Commons API: full-text searches of file pages and, for every hit, its page
  source (licence template, photographer, `{{taken on}}` date, NLI title, VRTS ticket) and the image info (size, SHA-1).
- Railway, inside the Meitar Collection: `רכבת`, `"Israel Railways"`, `קטר`, `train`, `"תחנת הרכבת"`, `"ראש העין"`,
  `מסילה`, `"תל אביב צפון"`, `"חידוש קו רכבת"` (181 distinct files), then
  `"רכבת ישראל"` and `insource:"1950-02" רכבת` to recount the series below. Across Commons: `insource:"1949-09-20"` (135
  files) and every file in Category:History of Israel Railways, Category:Bnei Brak train station (the 1949 Tel Aviv
  North station), Category:Yarkon Railway and Category:East Railway, one level down (809 files).
- Yishuvim, inside the Meitar Collection: `גילת`, `"בית קמה"`, `רעים`, `Gilat`, `"Beit Kama"`, `"Re'im"`, `מפלסים`
  (62 files).
- The rule is the event-photograph rule in docs/CONTENT_POLICY.md §4 and docs/event-images.md: public domain or CC0
  with a valid US basis, or CC BY 4.0 with a credit line; GPO, PD-Israel-only and share-alike files are refused.
- The NLI record pages (nli.org.il) return 403 from the machine used for the search, so NLI titles and dates are as copied onto
  the Commons pages. A full-text search can miss a frame whose page has none of the words above, so "not found" means
  "not found by these searches".

## The railway card (20 September 1949)

No Meitar frame has a date in September 1949 or a title about the Haifa to Tel Aviv line, Rosh HaAyin's reversal or
Tel Aviv North. What the Meitar Collection does have from the MVP years, all CC BY 4.0, VRTS ticket 2021092910005788:

| Series | NLI date | NLI title (as on Commons) | Why it is not the card's event |
|---|---|---|---|
| Boris Carmi, 20 frames | 1949 | "רכבת ישראל." (Israel Railways) | The recommended series. No place or day. |
| Benno Rothenberg, 45 frames | "1947 or 1949" (`-\|1947\|1949`) | "חידוש קו רכבת לירושלים לראשונה לאחר מלחמת העצמאות ובפעם הראשונה כרכבת ישראלית" (the Jerusalem line renewed for the first time after the war, the first time as an Israeli railway) | The Jerusalem line, not Haifa to Tel Aviv. The NLI date is open. |
| Boris Carmi, 10 frames | February 1950 | "שר התחבורה דוד רמז חונך את קו הרכבת הסדיר לירושלים" (the transport minister David Remez opens the regular line to Jerusalem; 5 frames), and 5 more of the same ceremony and "the first train to Jerusalem" under shorter titles | The Jerusalem line, after the card's date. |
| Boris Carmi, 3 frames | 1950 | "פורטרט של יו"ר הכנסת הראשון, יוסף שפרינצק בעת טקס של רכבת ישראל" (a portrait of the first Knesset Speaker, Yosef Sprinzak, at an Israel Railways ceremony) | A portrait of a real person, so PD or CC0 only under the ruling; the ceremony is not named. |
| Boris Carmi, 5 frames | 1951 | Level crossings in Tel Aviv, a passing train stopping the traffic | After the card's date; a crossing, not the line's opening. |

The two recommended Carmi frames were re-read today and are unchanged since the objective-cards note:
- A conductor with a flag beside the coaches: 997009453600005171, 2000 x 1975, SHA-1
  3cd1c8ad099c2976db9c49054cce0442be57b38b, `{{cc-by-4.0|...Meitar Collection/license credit line...}}`,
  `{{PermissionTicket|id=2021092910005788}}`.
  https://commons.wikimedia.org/wiki/File:Israel_Railways_(997009453600005171.jpg
- A tender lettered "מדינת ישראל" over "P R": 997009453599305171, 1975 x 2000, SHA-1
  b5b546c2a4cf358b29bd47802ecf71f70483d8ac, same licence and ticket.
  https://commons.wikimedia.org/wiki/File:Israel_Railways_(997009453599305171.jpg

Outside the Meitar Collection, every railway file dated 1948 to 1951 is refused or off the subject:
- "Tel Aviv North Railway Station.jpg" (`{{Taken on|1949-09-20}}`, Ernest Talhoffer) and "The Haifa train leaving for
  Tel Aviv from Atlit railway station 1949 D846-029.jpg" (GPO): already refused in the objective-cards note. They are
  the only two files on Commons dated to the day.
- "IL steam engine.jpg" (4 January 1949): `{{PD-Israel}}` only, already refused.
- "Israel Railways P-Class loco.jpg" and "The american made Beldwin tank engine locomotive 18.09.1950 D535-121.jpg"
  (18 September 1950): GPO, `{{PD-Israel}}`.
- "Contract coast line building 1950.jpg" (21 August 1950, Israel Railway Museum archive): `{{cc-by-sa-4.0}}`, share-alike.
  It is a scanned contract for the coastal line, not a photograph.
- "PikiWiki Israel 19322 Transport in Israel.jpg" (about 1949): `{{PD-Israel}}` only.
- Three PikiWiki files of Tel Aviv's old station (107599, 107600, 108414; "1940 to 1949"), `{{PD-author|Israel
  Preker}}`: a release by the author, but the description is about the old Tel Aviv station's buildings, which this
  event renamed Tel Aviv South. Not the new line, so not a pick.

## The yishuvim card (Beit Kama, Re'im, Gilat; Mefalsim)

- **Mefalsim:** 11 Boris Carmi frames, NLI date June 1949, NLI title "ימיו הראשונים של קיבוץ מפלסים." (the first days
  of kibbutz Mefalsim), CC BY 4.0. That is the series Phase 1 cleared; docs/event-images.md says its photograph comes
  in Phase 1 PR 7 (#380).
- **Beit Kama:** 33 Benno Rothenberg frames, `{{taken on|1950-11-15}}`, NLI title "מחנה עבודה של מע"צ" (a Public Works
  Department labour camp). A road works camp, a year and a half after the founding (18 April 1949), not the kibbutz.
- **Re'im:** 6 Benno Rothenberg frames, `{{taken on|1949-12-02}}`, NLI titles about soldiers at "משלט הגופרית" (the
  sulphur outpost): guard posts, a food queue, a tent, games beside a wall of soldiers' graffiti. A military outpost,
  not the kibbutz founded on 20 October 1949. Small (about 650 x 680 px).
- **Gilat:** no Meitar frame found.

So the yishuvim card has no founding photograph of the last required site (Gilat, 4 December 1949, the reference date
Q-OBJ-2 recommends). The Mefalsim frames remain the only founding photographs among the four.

## What this leaves open for Task 7.0

- The three cards' texts ("Write each card's account from sources already found") are not started here. They go into
  `content/objectives.yaml` and `content/events/`, which PR 7 itself writes once #782 (Q-OBJ-1, Q-RAIL-2), #785 and #786
  are settled. The Burma Road and railway sources are JPress pages, which return 403 from this machine, and the plan
  keeps the rule that a human reads each cited page before a card is `fact-checked`.
- The yishuvim card's sources come from PR 1 (#785), which is not on main.
