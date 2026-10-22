# 03 Language and terminology

Status: DRAFT. Tags: [V] already a repo rule or usage, [H] proposal, [Q] open question for the maintainer.

## Principle

A script introduces no term the project does not already use. If a script needs a new term, raise it as a question before the script is written, not after. The terms below are a starter list; the fuller list is the glossary proposed in #622.

## Dates and money

- On screen and in captions: `15 May 1948`; `IL 1.2m` or `$1.2m` [V, WRITING_GUIDE section 5].
- Spoken: written out as a person says it ("the fifteenth of May, nineteen forty-eight"). [H]
- `IL` is read as Israeli pounds in the glossary draft. [Q] confirm before a script says it aloud.
- A date range in captions uses "to", not a dash: `15 May 1948 to the end of 1951`.

## People and offices

- Name the person in full at first mention and give the office they held on that date. Offices follow `content/offices.yaml`.
- Never state an office, a name or a date from memory. It goes on the fact sheet with a source.
- A narrator never speaks as a real person (see `06`).

## Game terms (name them as the recorded build shows them)

Footage shows one build at one commit. The words a script uses for buttons, panels and numbers are the words on screen in that build, not the words in an older document. Check the label in the footage at the recorded commit. Examples of terms in use today: the "Why?" panel, the Cabinet, the Budget screen, the event card, the escalation ladder, the ma'abarot. UI labels are in sentence case ("End turn", "New game") since PR #630. [V]

The game design of record is `docs/GDD.md`. Where the GDD and the build use different names (for example for the top-bar resources), the script follows the build in footage and the GDD for features still in development. [H]

## Contested or open terms

- "The 1948 war" is the README's term. The GDD says "Liberation War". The store draft keeps "the 1948 war" and marks the choice [confirm]. Until the maintainer decides, scripts use "the 1948 war". [V, `docs/steam/store-page-draft.md`]
- For how to name parties, populations and events, follow `docs/CONTENT_POLICY.md`. Do not invent a euphemism and do not import a term from another source without a question.

## Hebrew and transliteration

- Use the project's spelling of transliterated terms: Ben-Gurion, ma'abarot, yishuv and yishuvim, halutz (a pioneer), Knesset. [V, README and store draft]
- First mention in English gives a short plain gloss: "the ma'abarot, the transit camps". [V pattern, README]
- In Hebrew, use the sourced Hebrew names in `content/` (`title_he`, `title_he_f`, `name_he`, `body_he`). Acronyms and abbreviations take one gershayim, U+05F4, as decided in PR #630. [V]
- Latin names and numerals inside Hebrew text: check how they render and add direction marks only where the display breaks (the UI does this for inserted values, PR #543). [V]

## Pronunciation

Every non-English name that will be spoken goes in `templates/pronunciation-list.md` with a native speaker's confirmation and a date. No synthetic or human voice records a name that is not on a signed-off list.

## Forbidden patterns (all channels)

- an invented quote or a private thought attributed to a real person [V]
- a claim with no source [V]
- "unprecedented", "historic" or "pivotal" as a substitute for a fact [V]
- moral language that turns policy into a character judgment [V, WRITING_GUIDE section 6]
