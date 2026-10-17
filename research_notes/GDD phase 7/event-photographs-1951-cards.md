# Three 1951 cards without a photograph: a search of the Meitar Collection (research note, 2026-10-17)

Status: research only, for the maintainer's choice. Nothing here is in `content/`, `unity/` or `event-images.yaml`.
Refs #807 (GDD Phase 7 PR 3: "a photograph per card where one is usable").

[docs/event-images.md](../../docs/event-images.md) lists seven events that still show the minister's portrait because
no free photograph that fits them was found. This note covers the three from 1951. The other four (`rhodes_talks`,
`armistice_lebanon`, `armistice_syria`, `sinai_withdrawal`) are outside it.

It follows
[portrait-fallback-event-images.md](../GDD%20phase%201/portrait-fallback-event-images.md) (#763, searched 16 October),
which reached these three cards by keyword searches and category walks. That note found no qualifying photograph from
any of the three windows: its education frame (the Shobaki ma'abara kindergarten, Herzliya, PikiWiki) is on subject but
its rights are unconfirmed; its forex frames (the shopkeepers' strike of August 1950, the Purim "white market, black
market" costume of 24 March 1951, the Allenby Street austerity sign of December 1949) are context from before the
crisis; and it found nothing for the reparations claim. This note adds a month-by-month sweep of the Meitar
Collection over each window, which finds in-window CC BY 4.0 frames for two of the three cards.

| Event | Window (spec rows; the YAML `to` is the next day) | Card date under monthly turns ([#567 table](../../docs/superpowers/specs/2026-10-14-event-timing-monthly.md), rows 26, 27, 31) |
|---|---|---|
| `education_registration_1951`, Schools in the Transit Camps | 29 November 1950 to 13 February 1951 | 15 December 1950 |
| `reparations_claim`, A Claim on Germany (tag `holocaust`) | 1 to 12 March 1951 | 15 March 1951 |
| `forex_crisis_1951`, Foreign Currency Runs Out | 19 October to 31 December 1951 | 15 November 1951 |

**Result:**
- **Schools in the Transit Camps: found.** A Benno Rothenberg frame taken on 15 December 1950, the card's own date
  under monthly turns: children in a ring among the tents of the Farradiyya (Parod) ma'abara.
- **Foreign Currency Runs Out: found, but not of the event.** Nothing shows the crisis itself. Two Meitar series from
  the window can illustrate it: a Tel Aviv commercial bank at work (November 1951) and the Finance Ministry's
  department board (December 1951).
- **A Claim on Germany: not found.** Nothing from 1 to 12 March 1951 fits. The one fallback is a building, dated January
  1952. Recommendation: keep the portrait.

## How it was done

- Read on 17 October 2026 through the Wikimedia Commons API. For every hit: the file page's source (the
  `{{cc-by-4.0|...}}` template, the photographer, the `{{taken on}}` or month date, the NLI title, the VRTS ticket) and
  the image info (size, SHA-1). The picks below were also looked at, as thumbnails.
- Every Meitar Collection file with each window's months on its page: full-text searches `"Meitar Collection"
  "<Month> <Year>"` for November 1950 (188 files), December 1950 (171), January 1951 (153), February 1951 (76), March
  1951 (170), October 1951 (121), November 1951 (186) and December 1951 (163): 1,228 files, all listed and read by NLI
  title.
- Keyword searches inside the Meitar Collection: `שילומים` (reparations), `גרמניה 1951` (Germany), `"משרד החוץ"` (the
  Foreign Ministry), `מטבע חוץ` (foreign currency), `חינוך מעברה` (ma'abara education), `"חינוך העולים"` (immigrant
  education), `"Schools Israel" 1950` and `1951`, `"Ma'barot" school`.
- Across Commons: `reparations Israel 1951`, `Wiedergutmachung Israel 1951`, `Sharett 1951`, `insource:"1951-03"
  Israel`, `Israel wheat ship 1951`, `צנע 1951` (austerity), `transit camp school 1950`, `מעברה בית ספר`.
- The rule is the event-photograph rule in docs/CONTENT_POLICY.md section 4 and docs/event-images.md: public domain or
  CC0 with a valid US basis, or CC BY 4.0 with a credit line. GPO, PD-Israel-only and share-alike files are refused.
  Images of real people stay PD or CC0; CC BY 4.0 covers event photographs, not portraits.
- The NLI record pages (nli.org.il) return 403 from the machine used, so NLI titles and dates are as copied onto the
  Commons pages. A full-text search can miss a frame whose page has none of the words searched, so "not found" means
  "not found by these searches".

All the Meitar files below are CC BY 4.0, VRTS ticket 2021092910005788, credit line "Benno Rothenberg / Meitar
Collection / National Library of Israel / The Pritzker Family National Photography Collection". Some carry the
"Meitar Collection (review needed)" category; docs/event-images.md (2026-10-13 correction) records that this is a
Commons sorting backlog, not a licence review.

## Schools in the Transit Camps (`education_registration_1951`)

**The series:** 59 Benno Rothenberg frames, `{{taken on|1950-12-15}}`, NLI title "חיילי חיל הים מסייעים להקים מעברה
בפראדייה." (navy soldiers help set up a ma'abara at Farradiyya), Commons category "Farradiyya (Ma'abara)". It shows the
tent camp, children, sailors at work, a clinic queue, and evening scenes in the tents. No frame shows a school or a
class, and the NLI title says nothing about schooling. The card's dispute was about schooling for children in these
camps, so the camp and its children are the card's setting, not a record of the dispute.

**Recommended frame:** children dancing in a ring on open ground, with the rows of tents and the hill behind them.
Seen from a distance, some children facing the camera: no single child is the subject, so it is not a portrait.
- 997008136555005171, 1273 x 1281, SHA-1 63cde352dcff5c18608a715ec32eb10c6e515fb2.
  https://commons.wikimedia.org/wiki/File:Parod_(997008136555005171).jpg
- Suggested alt text: "Children in a ring among the tents of a transit camp in northern Israel, December 1950." (The
  Commons page places it at Farradiyya, by Parod; it does not say "Galilee".)
- The original is square, so a 16:9 crop keeps a band about 56% of its height. The band should hold both the ring of
  children and the tents (roughly the middle of the frame). The crop is checked on the contact sheet when the picture
  is made.
- Against the Shobaki kindergarten frame of the earlier note: that one shows camp schooling itself, so it is the
  closer subject, but it cannot ship until Beit Rishonim confirms the rights. This one can ship under the ruling as it
  stands. If the Shobaki rights are confirmed, the maintainer can choose between them.
- Suggested `sensitive: true`: the children can be recognised, and the card is about the immigrant-camp schooling
  dispute. The owner should see the picture before a shareable build carries it.

Not used:
- 997008136555305171 (the same day): three small children at a tent marked with chalk drawings, menorahs among them.
  A strong picture, but it is a close view of three recognisable children, near a portrait.
- "הצבא מגיע לסייע בבניית המעברה בחוליקת" (the army helps build the ma'abara at Huliqat), 21 frames, 24 November 1950:
  five days before the window opens.
- "הועד למען החייל מחלק נעליים לילדי מעברה" (the Soldiers' Welfare Committee hands out shoes to ma'abara children), 9
  frames, 25 February 1951: twelve days after the window closes.
- "חוג תיאטרון של בי"ס תיכון" (a high-school drama club), 9 frames, February 1951: a school, but not a camp school,
  and several frames are close-ups of one girl.
- Later frames of camp children's education (Nahal soldiers teaching children in a ma'abara near Beersheba, 7 frames,
  18 October 1951; a school in Bat Yam, November 1951): outside the window.

## Foreign Currency Runs Out (`forex_crisis_1951`)

No frame from 19 October to 31 December 1951 shows the crisis, the wheat imports or the request to Washington. Two
series inside the window can stand for the finance minister's problem, in the way Q-CARD-1
([Phase 4 plan](../../docs/superpowers/plans/2026-10-15-gdd-phase-4.md), line 177) lets an undated "Israel
Railways" frame stand for the railway card:

1. **A commercial bank at work** ("בנק מסחרי בעת העבודה"), Tel Aviv, NLI date November 1951 (the card's month), 5
   frames. Two show the banking hall: clerks at desks with ledgers, seen across the room.
   - 997008137028605171, 1338 x 1338, SHA-1 06cf266d3f6cab1441d8cb4d03d8b9c6b6a4351d.
     https://commons.wikimedia.org/wiki/File:Tel_Aviv-Yafo_997008137028605171.jpg
   - 997008137028705171, 1257 x 1346, SHA-1 b224e872dc651db7862526bbfd4e58e8f8d28617.
     https://commons.wikimedia.org/wiki/File:Tel_Aviv-Yafo_997008137028705171.jpg
   - The other three frames are a manager at his desk (a near-portrait) and two closer views of one clerk.
   - Suggested alt text: "A bank's hall in Tel Aviv, November 1951." The bank is not named on the page, so the alt text
     does not name it.
2. **The Finance Ministry's department board** ("כרזה המפרטת את המחלקות השונות במשרד האוצר"), Jerusalem, NLI date
   December 1951, 1 frame. Text only, with no people. It is upright (1305 x 1897), so a 16:9 header would show a few
   lines of the board.
   - 997008136199705171, SHA-1 347832ba1d6594c8955374fc27c3d23dbaaebdf3.
     https://commons.wikimedia.org/wiki/File:Jerusalem_(997008136199705171).jpg

Recommendation: the bank hall (997008137028605171), as the closer fit to a card about money running out. The choice
is the maintainer's: neither picture is of the event.

Not used: the Kishon port works (33 frames, November 1951; the Haifa port extension, not the imports) and "ספינה
מועמסת בנמל חיפה" (a ship being loaded at Haifa port, January 1951, before the window).

## A Claim on Germany (`reparations_claim`)

Nothing in the window fits. The Meitar Collection's 170 files of March 1951 are Purim, a visiting American
philanthropist, the Soldiers' Welfare Committee, a doctor in the Rehovot ma'abara and a Gadna march. None is about
Germany, the Foreign Ministry or the note of 12 March. No Meitar page found has "שילומים" or "גרמניה" from 1951. Across
Commons, the March 1951 files found that are not Meitar files are GPO photographs (photographers Fritz Cohen and Hans
Pinn, D-numbers), which the owner ruling refuses. That includes the one of Moshe Sharett, which is also a portrait.

The only fallback found is a building, not the event:
- "מבנה של משרד החוץ בקריה בתל אביב" (the Foreign Ministry's building in the Kirya, Tel Aviv), NLI date January 1952,
  10 months after the card. 997008137484905171, 1914 x 1305, SHA-1 245dc756fb57583b8e06c03bf4ee10ddba23b9f0.
  https://commons.wikimedia.org/wiki/File:The_Kirya_997008137484905171.jpg
- Caution: the Kirya was set up in Sarona, the former German Templer colony
  ([Sarona](https://en.wikipedia.org/wiki/Sarona_%28colony%29), checked 2026-10-17), and this house looks like one of its
  stone houses (the Commons page does not say whether it is). On a card about a claim on Germany, a Templer house may
  read as a comment the card does not make. That is the owner's call under the card's
  `holocaust` tag, and the tag already makes any picture `sensitive: true`.

Recommendation: keep the foreign minister's portrait on this card, as now. Also checked and not used: the Foreign
Ministry's entrance plaque in Jerusalem and its archive room (3 frames, December 1951), both nine months after the card.

## What this leaves for the maintainer

- Adding a picture is the docs/event-images.md procedure: the `event-images.yaml` row, `tools/event_images.py`,
  `ASSETS.yaml` (`status: owner-verify`) and `NOTICE`, then `EventImageImport.Run` in Unity and the EditMode tests.
  That is a Unity step, so it is not done here.
- docs/event-images.md's list of portrait-only events changes only when a picture is actually added.
