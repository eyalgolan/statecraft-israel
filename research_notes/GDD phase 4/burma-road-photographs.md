# The Burma Road card: photographs (research note, 2026-10-17)

Status: research only, for the owner's choice (Q-CARD-1). Nothing here is in `content/` or `unity/`. Refs #791 (GDD
Phase 4 PR 7, Task 7.0) and #782 (the Phase 4 owner answers).

Task 7.0 of the [Phase 4 plan](../../docs/superpowers/plans/2026-10-15-gdd-phase-4.md) asks: "Search the Meitar
Collection for a CC BY 4.0 photograph of the Burma Road or the Road of Valour. Record what is found or not found".
Q-CARD-1's recommended answer says: "Burma Road: Task 7.0 searches the Meitar Collection; if no CC BY 4.0 or
public-domain photograph is found, no photograph." Its reason: "No Burma Road photograph has been searched for."

**Result: found.** The Meitar Collection on Commons has 33 CC BY 4.0 frames that the National Library of Israel (NLI)
dates to 7 December 1948 and titles as the Road of Valour's opening ceremony, the card's own event. It also has 60
frames of the Burma Road track from June and July 1948. The railway and yishuvim photographs are not covered
here: Q-CARD-1 already names them (objective-cards note).

How it was done:
- Read on 17 October 2026 through the Commons API: full-text searches of file pages (`intitle:"Burma road"`,
  `intitle:"דרך בורמה"`, `"Road of Valour"`, `"Road of Valor"`, `"כביש הגבורה"`, and inside the Meitar Collection
  category `"Burma"`, `"Road of"`, `Shaar Hagai`, `Latrun road`), the members of Category:Road of Heroism, and for
  each candidate its page source: the licence template, photographer, NLI title and date, NLI archive number, VRTS
  ticket, size, original URL and SHA-1.
- I looked at every frame dated 7 December 1948 or June/July 1948 at 400 px to say what it shows.
- The rule is the event-photograph rule in docs/CONTENT_POLICY.md §4 and docs/event-images.md: public domain or CC0
  with a valid US basis, or CC BY 4.0 with a credit line. GPO, PD-Israel-only, PD-India-only and share-alike files
  are refused, and portraits of real people must be PD or CC0.
- The NLI record pages (nli.org.il) return 403 from the machine I worked on, so the NLI titles and dates below are as
  copied onto the Commons pages. NLI dates are sometimes wrong (docs/event-images.md, the Altalena example).
- [C] marks a conflict between sources.

## Summary

| Use | Pick | Shows | NLI date | Shape |
|---|---|---|---|---|
| The card as decided (G1: the Road of Valour opens, 7 December 1948) | Rothenberg 997008137268105171 | Cars and a bus on the new road beside a stone verge, people on foot beside it | 7 Dec 1948 | 1370 x 924, crops to 16:9 |
| Alternate, same day | Rothenberg 997008137267905171 | A bus and a car queued on the road, people on foot (a rougher, scratched frame) | 7 Dec 1948 | 1370 x 908 |
| If the owner wants the track the player builds (Q-COR-1 option a) | Rothenberg 997008136513105171 | An improvised armoured truck climbing a steep, rocky track, men behind it | June 1948 | 1346 x 924 |
| Alternate, track | Rothenberg 997008136513305171 | From a vehicle: a convoy winding down a valley track | June 1948 | 1354 x 900 |

All four are CC BY 4.0, Benno Rothenberg, Meitar Collection, VRTS ticket 2021092910005788. None is a portrait.
Credit: "Benno Rothenberg / Meitar Collection / National Library of Israel / The Pritzker Family National Photography
Collection · CC BY 4.0 · graded and cropped".

## The Road of Valour's opening, 7 December 1948

Commons Category:Road of Heroism holds 33 Meitar frames, all `{{taken on|1948-12-07}}`, all
`{{cc-by-4.0|1={{Meitar Collection/license credit line|...}}}}` with `{{PermissionTicket|id=2021092910005788}}`: 29 by
Benno Rothenberg (NLI ARC. 4* 2068) and 4 by Boris Carmi (ARC. 4* 2069). The NLI title on the frames I opened is "טקס
לזכר הרוגי השיירות ופתיחת כביש הגבורה" (a ceremony in memory of those killed in the convoys, and the opening of the
Road of Valour). The frames show a flag-lined ground with a platform and loudspeakers, a crowd, soldiers on parade,
a bulldozer, 25-pounder guns, armoured cars, Ben-Gurion speaking and laying a stone, and traffic on the new road.

Picks (the road itself, no single person as the subject):
- **Pick: cars and a bus on the new road.** Rothenberg, NLI title "טקס לזכר הרוגי השיירות ופתיחת כביש הגבורה.",
  ARC. 4* 2068 / 13178.
  https://commons.wikimedia.org/wiki/File:Meitar_Collection_(997008137268105171.jpg, original
  https://upload.wikimedia.org/wikipedia/commons/b/bb/Meitar_Collection_%28997008137268105171.jpg (1370 x 924, SHA-1
  55b3ea19042035fbd45812ffc6576029b3d171dc).
- **Alternate: a bus and a car queued on the road, people on foot; the negative has heavy dust and scratches.** Rothenberg, NLI title "הקהל בטקס לזכר הרוגי השיירות ופתיחת כביש
  הגבורה." (the crowd at the ceremony), ARC. 4* 2068 / 13180.
  https://commons.wikimedia.org/wiki/File:Meitar_Collection_(997008137267905171.jpg, original
  https://upload.wikimedia.org/wikipedia/commons/3/30/Meitar_Collection_%28997008137267905171.jpg (1370 x 908, SHA-1
  7b3d603c729a88753160a92d0f1017e08009d478).
- Square Carmi frames of the same day, usable but they lose more in a 16:9 crop: the ceremonial gate with soldiers
  marching through it (997009322908305171, SHA-1 5f9bd2ef42552f996398eda9462c9c56b1f56c80) and people walking up the
  road (997009322908405171, SHA-1 d4254ac3e6c99f73d4c635d5cf2bf956143e4dc2), both 1322 x 1314, credit "Boris Carmi /
  Meitar Collection / National Library of Israel / The Pritzker Family National Photography Collection".

Not picked: the 16 frames titled "David Ben-Gurion (…)" on Commons. Several are close shots of him, and portraits of
real people stay PD or CC0 under the ruling. The wide ones (a speech to the crowd) are event photographs, but the
road picks above say "the road opened" more directly.

Alt text: "The opening of the Road of Valour, December 1948". It names only the month, as docs/event-images.md asks
when the day rests on a library date.

What the frames add to the date question **[C]**: the corridors note (D3, row 4b) and docs/gdd/corridors.md [S4] have 7 December 1948 (Hayom, 8
December 1948; HE Wikipedia) against 12 December 1948 (two English books, snippets only). The NLI dates both
photographers' series to 7 December 1948, which agrees with Hayom. It does not settle the conflict: a library date is
not a primary source.

The NLI caption on the frames also says "הכביש שחיבר בין רמלה לצומת נחשון (כביש 44)" (the road that linked Ramla
and Nahshon junction, Route 44). The plan (line 31) describes the Road of Valour as running by Nahshon and Shimshon
junctions to Sha'ar HaGai **[C]**. The caption most likely describes the later Route 44: the corridors note (row 4c,
D4) has the Ramla-Nahshon link opening separately in September 1950. The alt text above avoids the route, so the
card does not depend on either.

## The Burma Road track, June and July 1948

- Rothenberg, 55 frames, NLI date June 1948, NLI titles mainly "סלילת דרך בורמה לירושלים" (building the Burma Road to
  Jerusalem) and "סלילת דרך בורמה לירושלים הנצורה בזמן מלחמת העצמאות" (… to besieged Jerusalem during the War of
  Independence). They show jeep and truck convoys on dirt tracks, armoured trucks among boulders, drivers at the
  wheel, a Jerusalem signpost, and convoys loading under a shed, soldiers resting and a boy driving; 16 frames carry the title "פריצת
  דרך בורמה" (breaking through the Burma Road). Category:Burma road (Israel); all carry `{{cc-by-4.0|...}}` and the VRTS ticket.
  - **Pick: an improvised armoured truck (firing slits, number 834) on a steep, rocky track, men behind it.** ARC. 4* 2068 / 12865.
    https://commons.wikimedia.org/wiki/File:1948_Arab-Israeli_War_(997008136513105171).jpg (1346 x 924, SHA-1
    cef6bf096af3245f57e06a6ad419f69a1e1b880f).
  - **Alternate: a convoy winding down the valley, seen from a vehicle.** ARC. 4* 2068 / 12863.
    https://commons.wikimedia.org/wiki/File:1948_Arab-Israeli_War_(997008136513305171).jpg (1354 x 900, SHA-1
    8636f409256a9d6ee43a962879dc424146339777).
  - Also good: armoured trucks among boulders, NLI title "סלילת דרך בורמה לירושלים הנצורה בזמן מלחמת העצמאות",
    ARC. 4* 2068 / 12932, 997008136337705171 (1362 x 941, SHA-1 70661b06a7475d9bdec7130b731d03f052d144db).
- Boris Carmi, 5 frames, NLI date July 1948, NLI title "פריצת הדרך לירושלים, סלילת דרך בורמה." (breaking through the
  road to Jerusalem, building the Burma Road): a car on a dirt track (997009322901305171, SHA-1
  125f999475d728dcab6d5d80c402ec2021bafd4e), trucks among rocks (997009322901105171, SHA-1
  86a316c19ddaac88bb8bd7f0b1bd6fa11f1315b5), a truck beside a water pipeline, a cleared slope, the road above a
  valley in fog. All square (about 1314 x 1314).
- Rothenberg, 7 more frames titled "Burma road (…)" on Commons, NLI dates January 1950 and June-August 1950, under
  "War memorials" and "Memorials". They are after the opening and show the road's memorials, so they are not picks.

Alt text for a track picture: "Building the Burma Road to Jerusalem, June 1948" (the NLI's month). It shows the track,
not 7 December 1948, so the card text would need to say what the picture shows.

These files carry the Commons category "Meitar Collection (review needed)". That is a Commons sorting backlog, not a
licence review (docs/event-images.md, correction of 2026-10-13); the licence template and the VRTS ticket are on
each page.

## Refused under the policy (so nobody searches for them again)

- "Burma Road 1948.jpg", "Burma Road convoy.jpg", "Driving through Burma Road. 1948 (00071706).jpg", "Burma Road
  II1948.jpg" and "חטיבת הראל הגדוד הרביעי הפורצים דרך בורמה 2 - …-151742.jpg": `{{PD-Israel}}` only (the "Burma
  Road II1948" page adds a CC0 tag from the uploader, for an unknown author).
- "COLONEL MICKEY MARCUS (STONE) WITH HIS A.D.C. MAJOR ALEX BROIDO DURING THE "BURMA ROAD CAMPAIGN.D359-083.jpg": GPO,
  PD-Israel.
- "115534 jerusalem burma road PikiWiki Israel.jpg": Hans Pinn, 1948, `{{PD-author|Israel Preker}}`: the release is
  by the PikiWiki uploader, not by the photographer, so it gives no valid basis.
- "דרך בורמה.png": a 1949 encyclopaedia picture marked `{{PD-old-70}}`, with a Commons deletion request open since 12
  October 2026 ("Bogus licenses").
- "Mule section on parade at Burma Road opening ceremony (D401-089).jpg" and "Mule section.jpeg" (Category:Road of
  Heroism, dated 7 December 1948): GPO / National Photo Collection, public domain under PD-Israel only. They show the
  card's own day, so they are the likeliest to be proposed again.
- "PikiWiki Israel 8698 relief of burma road in rabin park.jpg" (2010) and the other PikiWiki files in Category:Burma
  road (Israel): CC BY 2.5, not CC BY 4.0, and modern.
- "HaGvura01.jpg": a 2008 photograph of the memorial figures by the road, GFDL / CC BY-SA (share-alike), and not of
  the period.
- Every other `intitle:"Burma road"` hit is the wartime Burma Road in Burma and China, or places in Britain, Canada
  and the United States.

## Open for the owner

1. Q-CARD-1: the 7 December 1948 road frame (pick) or a June 1948 track frame. The first matches the card's event and
   date (G1); the second matches the track the player builds under Q-COR-1's recommended option (a).
2. Sensitivity: none of the four picks shows the dead, combat or a named depopulated place. The June 1948 track pick
   shows an improvised armoured truck, military hardware in wartime, which the owner may want to see first. The ceremony was also a
   memorial for the convoys' dead (the NLI title), so the card text should not describe it as a celebration only.
   Whether to set `sensitive: true` in `event-images.yaml` is the owner's call.
