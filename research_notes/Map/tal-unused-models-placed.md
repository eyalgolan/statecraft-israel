# Tal Heuberger's Dakota, four props and local irregular: the sources of their placements

Sources for the placements of Tal Heuberger's last six unused generated models (owner, 2 November 2026: "make sure all of tal's new models are used"). The owner's rulings of 2 November 2026 picked each placement:

- the Egyptian Dakota C-47: "Over Tel Aviv, 15 May-11 Jun (Recommended)" ("A slow loop over Tel Aviv on those turns, like the Spitfire's loop, with the Egyptian flag; purely visual");
- the four props (09 well, 10 sandbags, 11 crates, 12 wall): "Place all four now (Recommended)" ("Wire each at its sourced place and dates, with the sources in the data rows marked agent-read; load measured; screenshots for you");
- the local Arab irregular: "Use it instead of C09".

**Status: agent-read, a person's check pending.** An AI agent read every source below on 2 November 2026: the cited revision of each wiki page (through the MediaWiki API) or the archive's web text. No person has checked them yet (`docs/CONTENT_POLICY.md`, section 2, rule 6). Each data row in `SpecialBuildingModels.cs` and `SeaAirCollection.Dakota.cs` says so. "[S]" marks a claim that rests on a source a wiki footnote names, which the agent did not open. The full research draft, with the candidates not taken, is the coordinator's sourcing note of 2 November 2026 (its rows D-a to D-h, S-a to S-e, C-a to C-c, W-a to W-d, L-a to L-c and I-a to I-d); the rows used here are quoted below.

## Dakota C-47 (`generated-1948-16-egyptian-dakota-c47`), in flight by Tel Aviv, 15 May to 11 June 1948

- he.wiki "הפצצת תל אביב במלחמת העצמאות", revision 43856594 (https://he.wikipedia.org/w/index.php?oldid=43856594), agent-read 2026-11-02; the paragraph has no footnote of its own: "תקופת ההפצצות הראשונה נמשכה מ-15 במאי ועד כניסת ההפוגה הראשונה לתוקפה ב-11 ביוני. שיטת התקיפה המצרית הייתה בדרך כלל גיחה של שני מטוסי הפצצה, בדרך כלל מטוסי דקוטה, שלוו במטוס קרב אחד או שניים מדגם ספיטפייר." (the first bombing period ran from 15 May until the first truce took effect on 11 June; the usual Egyptian attack was two bombers, usually Dakotas, escorted by one or two Spitfires.)
- en.wiki "Modi Alon", revision 1375428559 (https://en.wikipedia.org/w/index.php?oldid=1375428559), agent-read 2026-11-02 [S for the books it cites]: "when a pair of Egyptian C-47s and their two Spitfire escorts returned for their 16th bombing of Tel Aviv ... Alon evaded the Spitfires and then pursued and shot down both bombers".
- he.wiki "מודי אלון", revision 43744773 (https://he.wikipedia.org/w/index.php?oldid=43744773), agent-read 2026-11-02: "ב-3 ביוני המריא אלון ממחנה עקרון (תל נוף) והפיל באמצעות ה"מסרשמיט" שלו זוג מטוסי DC-3 דקוטה מצריים שהפציצו את תל אביב".
- JTA archive, "Egyptian force surrounded; Tel Aviv and Rishon l'Zion raided by Arab bombers" (https://www.jta.org/archive/egyptian-force-surrounded-tel-aviv-and-rishon-lzion-raided-by-arab-bombers), web text agent-read 2026-11-02 (the bulletin image, and so its dateline, unread): "Two Egyptian bombers which today raided Tel Aviv and its northern suburbs were shot down by intercepting Jewish fighters". It disagrees with the wikis on the second aircraft ("forced the second down near Rehovoth"); the day and the place agree.
- Its size: the C-47's wingspan, "|span ft=95" "|span in=6" (95 ft 6 in, 29.11 m), en.wiki "Douglas C-47 Skytrain", Specifications (C-47B-DK), revision 1383378030 (https://en.wikipedia.org/w/index.php?oldid=1383378030), agent-read 2026-11-02.

**What the game draws.** The 15 May 1948 turn is the only monthly turn in the window. The aircraft is drawn as the Spitfire is (#1932, #2014): in flight only, at the unit tokens' aircraft scale (420x, so 12.2 km across) and altitude (4,200 m over the highest ground under its loop), flying the Spitfire's ellipse once a minute, with Egypt's flag floating over its tail and no pole (the owner's ruling on #2260, "Smaller flag, no pole"). Its loop's centre is the nearest to Tel Aviv's settlement point whose whole swept box is over the board and clear of every unit token in the window and every map label and town (`SeaAirCollection.DakotaSpot`). It cannot be over the city itself: Kiryati's token stands on Tel Aviv from 15 May 1948 (`content/map/units_1948.yaml`), and at 420x the aircraft's box would cover it and the city's name. On the map that centre is 22 km due west of Tel Aviv's point, over the sea: the loop's eastern end is 17 km out, and the aircraft's nearest reach (half its 12.2 km span's diagonal, 8.6 km) comes to about 8 km from the city's point, just off its shore (measured by `SeaAirCollectionTests.TheDakotaLoopsJustOffTelAvivInsideTheBoardClearOfTokensAndLabels`, 2 November 2026). At the opening zoom (90 km) it reads as flying off Tel Aviv's coast. The owner reviewed this on #2274 (2 November 2026): "Off the coast is fine (Recommended)".

**What a person needs to check:** the IAF site article that he.wiki's footnote names (9529-55420) or Yofe and Nyveen p. 94, for the 3 June kill; whether the model, labelled Egyptian with unverified paint (`docs/vehicle-review-2026-10-22.md`), reads as an Egyptian bomber.

## The props' size and spot (owner on #2274, 2 November 2026)

"Bigger, closer (Recommended)": "Draw these four at about 3x so they read at close zoom, and let them stand at the town's edge right by the sourced spot." The four props are drawn at three times the one world scale of the placed models (`SpecialBuildingModels.PropScale`, in each row), and each may stand inside a town's kit footprint, clear of the town's building models, the tokens in towns, every settlement's and locality's name point, the routes, the other tokens and the water (`Want.TownEdge`).

## Prop 10, sandbags (`generated-1948-10-low-sandbag-position`): the Tel Aviv shore, the Altalena's turn only

- he.wiki "אלטלנה", revision 44137735 (https://he.wikipedia.org/w/index.php?oldid=44137735), agent-read 2026-11-02, footnote "nakdimon2016" (Shlomo Nakdimon, 2016) [S]: "אש שנורתה מתוך עמדת שקי החול שלמרגלות מטה הפלמ"ח אילצה את הסירה לחזור" (fire from the sandbag position at the foot of the Palmach headquarters forced the boat back), the night of 21-22 June 1948.
- The window is the Altalena card's own (`content/events/altalena.yaml`, from 15 June to 23 June 1948), the window `SeaAirCollection` draws the Altalena off Tel Aviv in; a test holds the two equal.

**What the game draws.** One sandbag post on the beach of the Tel Aviv coast point that the Altalena lies off (`SeaCraft.Coasts`, `tel_aviv`), just inland of the water's edge, at the first spot along the shore from that point that is clear (`SpecialBuildingModels.ShoreSpot`; see above). Its size is a DESIGN VALUE, a 4 m post, drawn at three times the world scale. The Altalena affair is tagged `war-casualties` on its card: the prop is a sandbag post only, with no figures, blood or casualties (CONTENT_POLICY section 4).

**What a person needs to check:** Nakdimon 2016 (the page is not printed in he.wiki) or a primary record such as HaMashkif, 23 June 1948, for the sandbag position.

## Prop 11, crates (`generated-1948-11-supply-crate-stack`): the Haifa port quay, 1948

- Wikimedia Commons copy of Nationaal Archief photo 255-1124, revision 970193372 (https://commons.wikimedia.org/w/index.php?oldid=970193372, CC0), wikitext agent-read 2026-11-02; archive record https://www.nationaalarchief.nl/onderzoeken/fotocollectie/aed57008-d0b4-102d-bcf8-003048976d84 (its metadata did not render in the fetch): "Het transportschip Bulgaria met emigranten in de haven van Haifa terwijl men wacht op het ontschepen, met op de kade kisten en kratten en een mobiel toilet" / "Datum : 1948" / "Locatie : Haifa, Israël" (crates and boxes on the quay).

**What the game draws.** One crate stack on the shore of Haifa's coast point (`SeaCraft.Coasts`, `haifa`, the port the immigrant steamer lies off), on the 1948 turns (15 May to 15 December 1948): the source gives the year only. Its size is a DESIGN VALUE, a 3 m stack, drawn at three times the world scale.

**What a person needs to check:** view the photo and confirm the quay crates are stacked wooden crates.

## Prop 09, well (`generated-1948-09-stone-village-well`): Abraham's Well, Beersheba, every date

- he.wiki "באר אברהם", revision 44049275 (https://he.wikipedia.org/w/index.php?oldid=44049275), agent-read 2026-11-02; the 1948 sentence has no footnote: "בשנת 1948, עם כיבוש באר שבע במסגרת מבצע יואב, הושארה באר אברהם כשטח ציבורי ארכאולוגי"; the well is "מדופנת עד לקרקעיתה באבנים מסותתות", 3.75 m across. Its point, 34.79307778 E, 31.237225 N, is he.wiki's and en.wiki's.
- en.wiki "Abraham's Well", revision 1374743581 (https://en.wikipedia.org/w/index.php?oldid=1374743581), agent-read 2026-11-02: Robinson 1838, "circular, and stoned up with solid masonry"; also "In 1897, a local sheikh built a house above the well".

**What the game draws.** The well, 3.75 m across (sourced) and drawn at three times the world scale, at every date, on the first clear ground round its point (`SpecialBuildingModels.SeededSpot`).

**What a person needs to check:** a dated description or photo of the well in 1948-51, to see whether its head was open or under the 1897 house.

## Prop 12, low wall (`generated-1948-12-low-stone-wall-module`): the Jaffa-Jerusalem road near Abu Ghosh, 1948

- Wikimedia Commons copy of Nationaal Archief photo 255-0876 (Van de Poll), revision 1225901219 (https://commons.wikimedia.org/w/index.php?oldid=1225901219), wikitext agent-read 2026-11-02; archive record https://www.nationaalarchief.nl/onderzoeken/fotocollectie/aed4b5dc-d0b4-102d-bcf8-003048976d84: "Terrassen op een heuvel, met op de voorgrond een stenen muurtje langs een weg; het dorp op de top is vermoedelijk Abu Ghosh" / "Reportage / Serie : Israël 1948-1949: onderweg naar Jeruzalem" (a low stone wall along a road; the village is probably Abu Ghosh; on the way to Jerusalem).
- Abu Ghosh is not a content place. Its point, 35.11083 E, 31.80472 N, is en.wiki "Abu Ghosh", revision 1381085214 (https://en.wikipedia.org/w/index.php?oldid=1381085214), agent-read 2026-11-02.

**What the game draws.** One run of the wall module beside route `jaffa_jerusalem_road` (`content/map/routes_1948.yaml`), just off its corridor at the road point nearest Abu Ghosh's, on the side facing it where clear, its long side along the road (`SpecialBuildingModels.RoadSpot`), on the 1948 turns: the source gives the year only. Its length is a DESIGN VALUE, an 8 m run, drawn at three times the world scale.

**What a person needs to check:** view the photo: is the wall low dry stone, and is the road the Jaffa-Jerusalem road? The record itself says only "probably" Abu Ghosh.

## The local Arab irregular (`figure-local-arab-irregular-1948-v1`): the local forces' figure, no row

The owner's 2 November 2026 ruling "Use it instead of C09" replaces the 1 November 2026 ruling on #1243, "Map figure: C09 after photo check (Recommended)". C09 was never mapped to the local forces in the code; the irregular now is (`UnitFigures.BySide`). The other 1 November rulings on #1243 still stand: "Starting strength on 15 May: No row until sourced (Recommended)" and "Sensitivity review: Once rows exist (Recommended)". So no local-forces row is added, no token of that side is drawn, and the figure is never streamed until a sourced row exists. Presence sources for a future row are in the coordinator's note (rows I-a to I-d: he.wiki "מבצע דני", revision 43258522; he.wiki "צבא הג'יהאד הקדוש", revision 43311377); none gives a sourced 15 May strength.
