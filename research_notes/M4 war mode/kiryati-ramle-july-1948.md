# Kiryati Brigade at Ramle, July 1948 (#2135, #2087)

Status: **draft, needs a person to check**. This is research only. An AI agent found and read the sources below on 1
November 2026. `docs/CONTENT_POLICY.md` section 2, rule 6, still requires a person to check every claim against its
source before use. This note does not change `content/`, `unity/` or code.

Tags: [S] a secondary source, [C] a conflict between sources, [V] the agent opened the page and read the quoted text
itself. "A person's check pending" means no person has checked it yet.

## Question and result

The owner's ruling on #2087 (1 November 2026) gives Operation Dani's unit order as "Kiryati to Ramle (Recommended)". The
31 October rule ("Sourced anchors only") needs a sourced, dated position for it. The proposed anchor is Kiryati at
Ramle from 12 to 20 July 1948, and until now it rested only on Wikipedia (the en.wiki "Operation Dani" table and the
he.wiki "מבצע דני" article). The question is whether a source other than Wikipedia puts the Kiryati Brigade, or named
Kiryati battalions, at Ramle on a day between 12 and 20 July 1948.

**Yes, for 12 July, at the level of "Kiryati units".** The Israel Defense Forces' own account of Operation Dani says:
"On July 12, Kiryati units took Ramle, which surrendered after a brief engagement." It does not name a battalion, and
it does not say how long the units stayed.

- **Best source:** the IDF account, republished by the Jewish Virtual Library (finding 1). It is the same IDF series
  that already sources `operation_yoav.yaml`.
- **The date:** Ben-Gurion's diary for 12 July 1948 records that Ramle's population announced it would sign a
  surrender at ten that morning (finding 2). It names no unit.
- **The battalion:** a government memorial page (Izkor) appears to name the 42nd Battalion in western Ramle in July
  1948, but the agent could not open it (finding 3). The 42nd Battalion therefore still rests on he.wiki alone.
- **The anchor's end (20 July):** no source read here says when the Kiryati units left Ramle. The end is the
  operation's end, the same rule as Yiftach's Lydda anchor, not a sourced departure.

## Findings

### 1. IDF account of Operation Dani: "Kiryati units took Ramle" on 12 July 1948

[V, agent read 2026-11-01; a person's check pending]

- Source: Israel Defense Forces, "The War of Independence: Operation Danny", republished by the Jewish Virtual Library,
  https://jewishvirtuallibrary.org/jsource/History/Danny.html, checked 2026-11-01. The page credits its text as
  "Sources: Israel Defense Forces" (with a link to idf.il) and gives no author or page number.
- Exact text: "On July 12, Kiryati units took Ramle, which surrendered after a brief engagement."
- The same page gives the order of battle: the operation used "the Yiftach Brigade, the new 8th Armored Brigade [...]
  and two battalions from the Kiryati and Alexandroni Brigades". This matches the Dani row of
  `research_notes/M4 war mode/war-1948.md`.
- **Precision:** the source says "Kiryati units", not "the Kiryati Brigade" and not a battalion number. Read with the
  order of battle on the same page, it was a battalion-level detachment of the brigade. The brigade token in the game
  stands for that detachment, as the Golani token does at Julis.
- Agreement: the en.wiki "Operation Dani" table ("Ramle / 12 July 1948 / Arab Legion withdrew / Kiryati Brigade") gives
  the same day and brigade. No conflict on the day.

### 2. Ben-Gurion's diary, 12 July 1948: Ramle's surrender that morning (no unit named)

[V, agent read 2026-11-01; a person's check pending]

- Source: David Ben-Gurion's diary, Monday 12 July 1948, the Ben-Gurion Archive's English transcription, item 225414.
  The live page (https://bengurionarchive.bgu.ac.il/en/node/9409) refused connections on 2026-11-01, so the Wayback
  copy was read: https://web.archive.org/web/20260118233403/https://bengurionarchive.bgu.ac.il/en/node/9409, checked
  2026-11-01.
- Exact text: "In the morning it was announced: Lod has surrendered, that is, the population, but the [Jordanian] army
  continued to fight the police. Ramle (the population) also announced that at ten they would sign a surrender, but
  it's still not clear what the army is doing."
- Use: this is a primary source for the day (12 July) of Ramle's surrender. It does not place Kiryati, so it supports
  the anchor's `from` date, not the unit.

### 3. Izkor: memorial to the 42nd Battalion of the Kiryati Brigade (located, not read)

[S, not opened; a person's check pending]

- Source: Ministry of Defence, Izkor, "אנדרטה לחללי גדוד 42 מחטיבת קרייתי במלחמת העצמאות",
  https://www.izkor.gov.il/en_fa46c51af7b40424bb29dac10daa62dc. On 2026-11-01 the live page returned HTTP 403
  (Incapsula), and its Wayback copy (20250324190121) holds only the page shell, not the text.
- A web search summary of the page says the 42nd Battalion fought on reaching western Ramle in July 1948, during
  Operation Dani, and that a memorial in Ramle names 35 of the battalion's dead. The agent did not read this text on the
  page itself, so it is **not** usable as a source yet. A person with a browser can open it and quote it.
- If confirmed, it would support the he.wiki sentence that a force of the 42nd Battalion took Sakhanat Fanus on the
  morning of 12 July. It gives "July 1948", not a day.

### 4. Not found or not opened

- Elhanan Oren, *BaDerekh el HaIr: Mivtza Dani* (Maarachot, 1976): no online copy found. It remains the standard
  reference for the battalion-level detail.
- The Haganah Heritage lexicon: no Kiryati or Dani entry turned up in the search, and none was read.
- The Palmach site's Dani account was not located, so its wording was not read. A search summary attributed to it a
  surrender time of 10:00 on **11** July; that is unverified, and it conflicts with the diary's 12 July (finding 2).
  If a person confirms it, record it as [C] against the diary.
- Benny Morris and Netanel Lorch were not opened.

## Recommendation for the anchor

- `from: '1948-07-12'`: sourced. The IDF account (finding 1) and the diary (finding 2) both give 12 July.
- The unit: sourced as "Kiryati units" (finding 1). Cite the IDF account first, en.wiki second. Say in the note that it
  was a detachment of the brigade and that the battalion number (42nd) comes from he.wiki only, until Izkor or Oren is
  read.
- `to: '1948-07-20'`, `end_basis: source`, "Operation Dani ended on 19 July 1948": this is the operation's end, the same
  as Yiftach's Lydda anchor. No source read gives the Kiryati units' departure. The anchor note should say so.
- Suggested `sources:` lines for the anchor, both "a person's check pending":
  - `{url: 'https://jewishvirtuallibrary.org/jsource/History/Danny.html', checked: '2026-11-01'}` with the comment
    `# IDF account of Operation Danny, republished by the Jewish Virtual Library: "On July 12, Kiryati units took Ramle, which surrendered after a brief engagement."`
  - `{url: 'https://web.archive.org/web/20260118233403/https://bengurionarchive.bgu.ac.il/en/node/9409', checked: '2026-11-01'}`
    with the comment `# Ben-Gurion's diary, 12 July 1948: "Ramle (the population) also announced that at ten they would sign a surrender".`

## Sensitivity

`docs/m3-release.md` records `ten_days` with the tags `security` and `war-casualties`, and says Lydda and Ramle are shown
as a military operation only: leaving out the population's flight and expulsion is an editorial choice that needs
explicit approval. Under `docs/CONTENT_POLICY.md` section 2, rule 1, a card with a sensitive tag must be
`sensitivity-reviewed` before a shareable build.

The sources chosen for the anchor are the IDF's military account and the diary's line on the surrender. Neither is
tied to a killing or an expulsion. Sources that place Kiryati in Ramle through the detention or removal of residents
were not used as the position anchor. The anchor adds a unit position only; it adds no text about the population.

## Sources

- Israel Defense Forces, "The War of Independence: Operation Danny", Jewish Virtual Library,
  https://jewishvirtuallibrary.org/jsource/History/Danny.html. [V, agent read 2026-11-01; a person's check pending]
- David Ben-Gurion, diary, 12 July 1948, Ben-Gurion Archive English transcription, item 225414, Wayback copy
  https://web.archive.org/web/20260118233403/https://bengurionarchive.bgu.ac.il/en/node/9409. [V, agent read
  2026-11-01; a person's check pending]
- Izkor, memorial to the 42nd Battalion, Kiryati Brigade,
  https://www.izkor.gov.il/en_fa46c51af7b40424bb29dac10daa62dc. [S, not opened (HTTP 403); a person's check pending]
- en.wiki, "Operation Dani", the table of communities captured, https://en.wikipedia.org/wiki/Operation_Dani. [S,
  agent read 2026-11-01 for #2087; a person's check pending]
- he.wiki, "מבצע דני", https://he.wikipedia.org/wiki/%D7%9E%D7%91%D7%A6%D7%A2_%D7%93%D7%A0%D7%99. [S, agent read
  2026-11-01 for #2087; a person's check pending]
