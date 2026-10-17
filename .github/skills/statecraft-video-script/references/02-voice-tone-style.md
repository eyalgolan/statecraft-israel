# 02 Voice, tone and style

Status: DRAFT. Builds on `docs/WRITING_GUIDE.md` sections 2 to 4 and `.github/skills/statecraft-writing-voice/`. Tags: [V] already a project rule, [H] proposal for the maintainer to confirm, [A] working assumption to test on the first recording.

## The voice in one line

A calm, well-informed cabinet secretary reading the prime minister the briefing. [H]

This fits the game's own loop, where reports arrive from ministries and only the most important reach the prime minister's desk (`docs/superpowers/specs/2026-10-09-statecraft-israel-design.md`, the Briefing step). It gives the narrator a reason to be sober, specific and brief, and a reason not to sell.

## What carries over from the writing guide [V]

Direct. Grounded. Historically specific. Politically literate. Sober but readable. Short, concrete sentences. Verbs that say what changed. Institutions named, not just the mood. No inflated heroics, no generic nationalism, no moralising summaries, no AI filler.

## The apparent conflict, and how it resolves

The writing-voice skill asks whether a line sounds like "a decision card, not a TED Talk or a film trailer". A video script is allowed to borrow a trailer's picture, pace and music. It is not allowed to borrow a trailer's adjectives. Cinematic means composition and rhythm. It never means "epic", "destiny" or "the world held its breath".

## Rules for spoken lines

1. **One idea per sentence.** The viewer hears it once. Flag a sentence over 18 words. [A]
2. **New information goes at the end** of the sentence, where the ear lands.
3. **Concrete nouns.** An office, a place, an amount, a date. "The Treasury" beats "the authorities".
4. **Active verbs.** "The cabinet divided." not "A division was seen within the cabinet."
5. **Person and tense.** Second person present for the player's role: "You are the prime minister." Third person past for history: "The cabinet divided over the truce." Do not mix them inside one sentence.
6. **Questions only when the question is the player's real decision.** The store text closes on one: "What comes first?" Do not stack questions.
7. **Numbers and dates.** On screen: `15 May 1948`, `IL 1.2m`. Spoken: written out the way a person says them. [V for the on-screen formats, WRITING_GUIDE section 5]
8. **British spelling**, matching the UI. Official titles keep their official spelling (for example the Minister of Defense). [V per `content/offices.yaml` comment; confirm]
9. **No stacked triples.** "Courage, sacrifice and hope" is a rhythm, not a fact.
10. **Silence is allowed.** Music is not a substitute for a missing fact.

## Explain first, then evoke

The project cannot assume the viewer knows the situation. A line that works only for someone who already knows the story is a line for the team, not for the audience. Rules:

1. **Plain fact before image.** A contrast, a metaphor or an aphorism may follow the plain fact it compresses. It never replaces it. "The lake was in the north. The thirst was everywhere else." is a good line for a viewer who already knows which lake, which north and whose thirst. For everyone else it says nothing.
2. **Name the thing at first mention.** "The lake", "the camps", "the sea" and "the north" each need a name or a place: the Sea of Galilee, the ma'abarot (the transit camps), the Mediterranean, northern Israel.
3. **Give scale.** How much, how many, how long, how far. "Many problems" and "one of the most pressing" are not information.
4. **Answer who, what, where, when, and why it matters** inside the video, in the order a stranger needs them.
5. **No slogan as a conclusion.** "Water became a basis for peace" asks the viewer to accept a claim. Name the agreement, the year and the amount, and let them draw it.
6. **The cold-viewer test.** Read each voice-over line to someone with no background and ask what they learned. If the answer is "nothing" or "I would have to look it up", rewrite the line.

| Vague | Self-contained (each still needs a source row) |
|---|---|
| "The lake was in the north. The thirst was everywhere else." | "The Sea of Galilee, a freshwater lake, is in northern Israel. Central Israel is the most populated part of the country, and southern Israel is arid." |
| "In the camps, families waited their turn." | "In the ma'abarot, the transit camps built for new immigrants, families queued for water." (needs a source) |
| "Water became a basis for peace." | "Under the 1994 peace treaty with Jordan, Israel agreed to send Jordan 50 million cubic metres of water a year." |
| "When the rain ran short, we turned to the sea." | "After a long drought, Israel tendered its first large seawater desalination plant." (needs date and place) |

## Emotion

Let the fact carry it. One human-scale detail per script is enough (a number of people, a place, a month), stated plainly. Never put a feeling into the mouth of a real person. Never tell the viewer what to feel.

## Before and after

| Generic | Grounded |
|---|---|
| "In a pivotal moment that would change the destiny of a nation, one leader faced an impossible choice." | "You are the prime minister on 15 May 1948. Money, arms and food never cover everything. What comes first?" (the game's own text) |
| "Experience the epic story of Israel's birth like never before." | "Turn by turn, from 15 May 1948 to the end of 1951. Real ministers, real dates, a source for every fact." |
| "Against all odds, a young nation rose." | Replace the verdict with the fact that earns it: the office, the date, the amount. If there is no fact, there is no line. |
| "The world watched in horror as..." | State what happened, when, as reported by which source. |

Every line in the right-hand column that is not the game's own text still needs a source row before it is used.

## Sensitive subjects

War, terror and the Holocaust are handled as history, through facts, dates, official figures and archival material, as the Steam draft already promises ("without graphic violence"). Specifically:

- no music sting, slow motion or zoom on a casualty
- no imagery of the camps and no Nazi symbols [V, `docs/steam/coming-soon-kit.md`]
- no video of any kind for a Deport option [V, `docs/event-videos.md`]
- do not turn suffering into a gameplay abstraction [V, WRITING_GUIDE section 4]
- the Israeli framing is stated, not softened and not made into a slogan

## Humour, slang, hype

None by default. A script that needs a joke to hold attention has a missing fact.

## Words and phrases that fail review

The checker flags these. They are the habits of generic scripts and of AI drafts: "in a world", "pivotal", "unprecedented" (without a source), "destiny", "forever changed", "against all odds", "hang in the balance", "like never before", "epic", "untold story", "journey", "tapestry", "rapidly evolving", "it is important to note", "game-changer", "buckle up", "dive into".

## Hebrew

Write Hebrew natively. Do not translate the English line by line. See `07-captions-accessibility-hebrew.md`.
