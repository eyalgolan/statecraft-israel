# Case study: from a vague water story to a self-contained one

An existing narrated promo video about water was used to test these rules. The findings are about the text, not about anyone. The point of the case study is the pattern: a script that sounds like a story but assumes the viewer already knows the situation.

## Earlier working transcript: version and date unverified

The transcript below came from the earlier drafting material. Its cut identifier and date have not been established. It is not the owner's promo 3 narration posted on 15 October 2026 in [#639](https://github.com/eyalgolan/statecraft-israel/issues/639#issuecomment-6191440479). The owner supplied bar cues at 0.667 seconds per bar, different wording, and no closing "Thank you." Do not use the timestamps below to judge that cut. The final cue sheet is still awaited.

## Earlier transcript (for the text exercise only)

> [00:01 to 00:24] The young Jewish state faced many problems. One of the most pressing was water. The lake was in the north. The thirst was everywhere else. Negba, 1948. The Egyptian army pushed north. Shells tore through the water tower, but Negba held.
>
> [00:41 to 00:43] In the camps, families waited their turn.
>
> [01:10 to 01:14] A lake carried south, then one drop at a time.
>
> [01:14 to 01:16] Water became a basis for peace.
>
> [01:16 to 01:18] When the rain ran short, we turned to the sea.
>
> [01:41] Thank you.

The earlier drafting exercise recorded checker findings for these supplied cues. The input table and its exact version are not preserved here, so the old error and warning totals are not reproducible evidence. In particular, its gaps and speaking-rate estimates do not establish a problem with the owner's cut.

## Jeremy's reading of the earlier transcript

Too vague. Not informative enough. Not enough story. Lines like "the lake was in the north, the thirst was everywhere else" assume the viewer already knows the situation, and the project cannot assume that.

This is Jeremy's analysis, not a quotation or attributed maintainer verdict. It informs the proposed rule: **explain first, then evoke** (`references/02-voice-tone-style.md`), and a script must pass the **cold-viewer test**.

## What the finished video carries besides the voice

The supplied script is the voice track only. The finished promo, "The Long Way South", carries more than the voice, so the voice file alone makes the story look vaguer than the video is. The maintainer described the other channels in #639 [M]. This description has not been checked against the video file.

| Channel | What it carries |
|---|---|
| Voice | The owner's separate narration in #639, from an ElevenLabs premade voice, cued by bars of 0.667 seconds. It is not the earlier transcript above. |
| On-screen captions | "Jerusalem, May 1948 / The pumps have stopped. Water is rationed."; "Kibbutz Negba, 1948 / In the Egyptian army's path north"; "About 150 shells hit its water tower. Negba held." |
| Decision cards, shown and not spoken | Operation Yoav, Hula, Water for Jordan, Jordan Canal 1953, Desalinating the Sea 2002 |
| Not voiced at all | the Declaration of Independence card |

What this changes:

- D3 and D4 below concern the earlier transcript alone. The owner supplied the exact captions above. For "about 150 shells", the maintainer identified Hebrew Wikipedia, "מגדל המים (נגבה)", citing Bar-Yosef, IDF publishing ([decision](https://github.com/eyalgolan/statecraft-israel/issues/639#issuecomment-6196244797)). Keep it [S] until the printed book is checked. This revision records the pointer, not a fresh source verification.
- The cards carry the real decisions, and nobody explains them. A viewer who knows nothing sees five card titles and hears none of them. That is the strongest reason for D9.
- A viewer with the sound off loses the voice and keeps the captions and cards. A viewer with the sound on and no time to read loses the captions. Each channel must carry what the viewer needs, and the voice must not depend on a card to be understood.
- A captions-and-cards table, with what each carries at each time, is the right unit for a script like this one. The shot table (`templates/script-template.md`) already has the columns for it.

## Diagnosis

| Id | The line | Why it fails for a cold viewer | Rule |
|---|---|---|---|
| D1 | "The young Jewish state faced many problems. One of the most pressing was water." | Generic opener. "Many" and "one of the most" give no scale. Nothing is learned. | Say what, how much, where, when. |
| D2 | "The lake was in the north. The thirst was everywhere else." | Which lake? Which north? Whose thirst, and where? A contrast line that works only if the viewer already knows the geography. It also overclaims ("everywhere"). | Explain first, then evoke. Name the place and give the plain fact the line compresses. |
| D3 | "Negba, 1948. The Egyptian army pushed north. Shells tore through the water tower, but Negba held." | Specific, but disconnected: what is Negba, why does its water tower matter here? Check the historical caption details separately. The exact shell count still needs the cited book; the transcript alone does not establish any of these claims. | One sentence that ties the event to the story, worded no more strongly than the source, or cut. |
| D4 | "In the camps, families waited their turn." | Which camps? Waiting for what? No source. | Define on first mention, with a source. |
| D5 | "A lake carried south, then one drop at a time." | Two separate developments compressed into a metaphor, neither named, neither dated. | Name the thing, give the date, then the image. |
| D6 | "Water became a basis for peace." | A slogan. Which peace, what water, how much? A loaded claim needs a source and care. | Name the agreement, the year, the amount. |
| D7 | "When the rain ran short, we turned to the sea." | Who is "we"? Desalination is never named. No date or scale. | Name it, date it, give a number. Keep the narrator's person consistent. |
| D8 | "Thank you." | Not a next step. | Verb plus a place that exists today. |
| D9 | The whole script | No premise (what is the viewer watching?), no decision, no consequence. The footage shows a real decision card that the voice never mentions. The earlier cue timings are unverified. Do not carry their gap or speaking-rate findings over to the owner's cut; assess timing when the final cue sheet is available. | Use the shot table. One story with a setting, a problem, a decision and a result. Picture and voice tell the same story. |

## What works and should be kept

- Concrete images. A real archival photograph shown first, credited.
- Short sentences.
- The theme. Water scarcity is a good fit for the game's own idea that every choice is paid for somewhere else.
- "One drop at a time" is a good line once the viewer knows what it refers to.

## A rewritten version

A teaching rewrite with sources re-opened on 17 October 2026. Only the Hula beat represents current game content. The later dates are explicitly historical context, beyond the May 1948 to January 1952 MVP. The rewrite does not promise later-era cards or implementation. Source checks are AI-assisted; footage, rights, native Hebrew captions and human factual review remain release work.

```
Title: Water: one game decision and its later historical context
Channel: example only; historical-context format needs owner approval before production
Length (seconds): 73
Language: en
Audience: strategy players and history-minded players
Build id or footage note: REPLACE with a real build and commit id for Hula; later beats use labelled historical graphics
```

| # | Time | Picture | On-screen text | Voice-over | Sources |
|---|---|---|---|---|---|
| 1 | 0-10 | Hula decision card, footage pending | 1951. The choice: continue drainage work in the demilitarised zone, or halt pending agreement. | In 1951, Israel's prime minister faces a decision. Continue drainage work near Lake Hula, or halt pending agreement? | F1, game:G1 |
| 2 | 10-21 | Historical map, labelled context beyond the current game | Later history, beyond the current game. Water from the Sea of Galilee to towns in the arid south. | The story continues beyond the current game. The National Water Carrier would move water from the Sea of Galilee to towns in the arid south. | F2 |
| 3 | 21-33 | Historical Carrier graphic, labelled context | 1964. The National Water Carrier. A route of about 130 kilometres. | In 1964, the National Water Carrier began operating. Its route carried water about 130 kilometres from the Sea of Galilee towards southern Israel. | F2, F3 |
| 4 | 33-49 | Treaty graphic, labelled historical context | 1994. Israel and Jordan agree to seek sources for an additional 50 million cubic metres of drinking water a year for Jordan. | The 1994 peace treaty also addressed water. Both countries would seek new sources of drinking water for Jordan. The target: an additional 50 million cubic metres each year. | F4 |
| 5 | 49-62 | Ashkelon historical graphic, labelled context | September 2000. A tender for a large seawater desalination plant at Ashkelon. | In September 2000, a tender was issued for a large seawater desalination plant at Ashkelon. The initial contract followed in November 2001. | F6 |
| 6 | 62-73 | Return to the Hula options and title card | 1951. Your choice: continue or halt. Statecraft: Israel. Free. Every historical fact has a source. | Back in 1951, you face the Hula decision. Statecraft: Israel is free, with a source for every historical fact. | game:G1, F7 |

### Fact sheet for the rewrite

[V] below means an AI-assisted source check on 17 October 2026, not human sign-off. The [source audit](../references/09-source-audit.md) records passages, corrections and remaining limits.

| Id | Claim | Kind | Source | Tag |
|---|---|---|---|---|
| F1 | Drainage-related work south of Lake Hula was in the demilitarised zone in 1951. | history | [FRUS 1951 vol. V, doc. 288](https://history.state.gov/historicaldocuments/frus1951v05/d288), telegram of 5 March and footnote 5 | [V] |
| F2 | The Carrier transported water from the Sea of Galilee in the north towards towns and settlements in the arid south. | history | [Mekorot, Our History](https://www.mekorot-int.com/our-history/), 1960s | [V] operator's account |
| F3 | The Carrier operated from 1964; its route was about 130 km. | history | [Mekorot visitor centre](https://www.mekorot-int.com/visitor-center/), Sapir pumps since 1964; Mekorot history, 1960s, 130 km | [V] operator's account |
| F4 | The 1994 treaty required joint efforts to find sources for an additional 50 million cubic metres a year for Jordan. | history | [Treaty, Annex II, Article I.3, Yale Avalon reproduction](https://avalon.law.yale.edu/20th_century/jordan_treaty_annex2.asp) | [V] primary treaty text |
| F6 | The Ashkelon tender was issued in September 2000, followed by the initial contract in November 2001. | history | [Dreizin, Tenne and Hoffman, 2007, Water Commission-hosted paper](https://www.gov.il/BlobFolder/generalpage/desalination-main/he/GRCE0035.pdf), PDF page 12 | [V] planners' account |
| F7 | The game is free; historical facts carry sources. | game | README.md, opening description and history section, at 46abd99c | [V] repository check |
| G1 | The prime minister's Hula event has continue and halt options. | game | content/events/hula_drainage.yaml at 46abd99c | [V] definition checked; recorded-build appearance pending |

### Notes on the rewrite

- The earlier 2002 tender claim was wrong. The paper distinguishes the September 2000 tender, November 2001 initial contract and April 2002 capacity increase. No unverified 2002 card or options appear in the rewrite.
- The earlier claim that roughly half of drinking water came from desalination in 2015 is removed. The inherited source did not establish a checked denominator and figure suitable for this script.
- The treaty line describes the joint additional-water commitment. It does not turn that clause into an unconditional Israeli transfer of 50 million cubic metres a year.
- Negba is not in the rewrite. The maintainer's Bar-Yosef book pointer for the shell figure remains unresolved; it is not cleared for reuse by this example.
- Historical graphics and footage need asset-specific rights checks before production. Timing is an estimate until a voice and cue sheet are tested.
