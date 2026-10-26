# Writing guide for Statecraft: Israel

This guide is the repo’s practical writing standard for event copy, historical cards, and narrative text. It complements the stricter rules in `docs/CONTENT_POLICY.md` and the YAML schema in `docs/WRITING_EVENTS.md`.

## 1. Purpose

The writing should make the player feel the weight of governing Israel in a contested historical moment, without drifting into melodrama, invented speech, or unsupported certainty.

The standard is writing that helps the player understand what happened, what was at stake, and what choice was before them.

## 2. Core voice

The project voice is:

- direct
- grounded
- historically specific
- politically literate
- sober but readable

Use short, concrete sentences when possible. Favor verbs that tell the player what changed, not abstract historical summary.

Good examples:

- “The cabinet divided over the truce terms.”
- “The budget tightened as the ma'abarot swelled.”
- “The government faced a fresh wave of security pressure.”

## 3. Tone guidelines

### Preferred tone

- credible
- tense but precise
- matter-of-fact when discussing state-building
- clear about trade-offs and consequences

### Avoid

- inflated heroics
- generic nationalism without action
- vague political theater
- AI-flavored filler
- moralizing summaries that flatten complexity

## 4. Historical framing

The project deliberately tells the story from the Israeli perspective, as set out in `docs/CONTENT_POLICY.md`, section 1. That choice is settled and is not reopened in event copy. It is not a reason to write with soft certainty or to hide the state’s calculations and constraints. Explain them from the frame the game is built around.

Still, the writing must stay honest:

- name the institutions, not just the mood
- identify the real conflict or pressure
- connect the action to a sourceable fact
- avoid turning suffering into a gameplay abstraction

## 5. Event writing rules

When drafting an event card:

- explain the immediate problem and the decision before the player
- keep the sentence length readable in UI space
- write in plain, historical English
- avoid invented speeches or private thoughts
- use placeholders like `{prime_minister}` for office-based naming
- write dates in the project’s format, such as `15 May 1948`
- show money as `IL 1.2m` or `$1.2m`, and never show internal ids to the player

A strong event card usually does three things:

1. names the problem or situation
2. shows the source of pressure or stakes
3. frames the player’s decision

## 6. Forbidden patterns

Do not write:

- invented direct quotes
- “this unprecedented moment” without evidence
- generic AI filler or explanatory padding
- claims that are not tied to a real source
- emotionally inflated language that obscures the historical fact
- moral language that turns policy into a character judgment

## 7. Source-backed writing standard

If a sentence is not directly supported by a source, it is not ready for the repo.

The real standard is not “interesting” but “supported, readable, and historically grounded.”

## 8. Good historical writing checklist

Before finalizing a draft, ask:

- Does the text reflect the actual period and power structure?
- Is the decision understandable to a player without prior knowledge?
- Does the wording stay within established project framing?
- Does it fit the event’s field data and source list?
- Is it concise and legible in the game UI?
- Does it avoid invented quotes or inflated language?
- Is `status` still `draft` until a person has checked every claim against its source?

## 9. Place names

The Arabic place names of 1948 in English text follow *Village Statistics, 1945*, as the map labels already do
(`content/map/localities_1948.yaml`, `name_basis: vs1945`): **Beit**, not "Bayt"; **Deir**, not "Dayr". Owner's ruling,
25 October 2026 (#1586).

Six things the rule does not reach:

- **A quoted source form keeps the spelling its source prints** ("keep quotes as printed", same ruling). That covers
  `listed_as` values, the armistice line names in `content/map/armistice_hexes_1949.yaml`, and any wording inside
  quotation marks. `docs/routes.md` quoting the Burma Road's start as "just east of Dayr Muhaysin" is the pattern: the
  phrase is the English Wikipedia article's, so it keeps that article's spelling even though our own prose says Deir.
- **Kafr and Kfar stay distinct.** Kafr is the Arabic word and Kfar the Hebrew one; they are two words, not two
  spellings of one, and `name_basis` already records which is which.
- **A URL is never respelled.** A Wikipedia article title belongs to the article, so a waypoint spelled "Beit Jiz" may
  still cite `.../wiki/Bayt_Jiz`.
- **A record id is not display text.** `bayt_mahsir` keeps its id while its `name` reads "Beit Mahsir"; renaming ids
  moves every reference and the content hash for no player-visible gain.
- **Today's name of a living place is not a 1945 form.** It is the present-day official name with its own source, so
  `today.name` reads "Bayt Jann" beside `name` "Beit Jann".
- **Hebrew terms follow section 10, not this rule.** ma'abarot, yishuv and halutz are Hebrew, not Arabic place names;
  their spelling is section 10 below.

## 10. Hebrew terms in English text

**ma'abara**, **ma'abarot** — with a straight apostrophe, never "maabara" or "maabarot". Owner's ruling, 26 October
2026 (#1760), for docs, content and the UI alike. Give the English at the first mention, as the glossary's
*transit camp* entry sets out: "the ma'abarot (transit camps)", then either word.

The owner has ruled on ma'abara and ma'abarot only. The spellings below are **observed in the game, not ruled**:
they are what the text already uses, recorded here so a reader has one place to look, and a straggler is not a second
scheme. **yishuv** and **yishuvim**, with no variant spelling at all; **halutz** and **halutzim**, against 5 "haluz";
**aliyah**, against 5 "aliya"; **hakhshara** with *kh*, against 10 "hachshara", two of them in `content/sites.yaml`;
and **Knesset**, against 6 "Knesseth".

The straggler counts are from main on 26 October 2026, over `docs/`, `research_notes/`, `content/`, `README.md`,
`README.he.md`, `CONTRIBUTING.md` and `unity/Assets/UI/strings.en.yaml`, matching `.md`, `.yaml`, `.yml`, `.tsv`,
`.json` and `.txt` only, case-insensitively, with a word boundary either side so that "aliya" does not count the
"aliyah" that contains it. They are the counts that matter to the rule, and they reproduce exactly.

Three things the rule does not reach, for the same reasons as section 9:

- **An identifier is not display text.** An apostrophe cannot go in an id, a YAML key, a filename or a C# name, so
  the event `maabarot_policy`, the policy key `maabarot`, `content/events/maabarot_policy.yaml` and
  `MaabaraCamp.cs` keep their spelling. `content/model.yaml` is the pattern: `id: maabarot` beside
  `name: Transit camps (ma'abarot)`. Renaming them would move every reference and the content hash for no
  player-visible gain: the event id `maabarot_policy` is named by three goldens, and the policy key `maabarot` has a
  line in all nine.
- **A quoted source form keeps the spelling its source prints**, as in section 9. `research_notes/GDD phase 3/locked-project-kinds.md`
  quoting "The first maabara opened in Ksalon" keeps the source's spelling.
- **A URL is never respelled.**

