# UI microcopy guide for Statecraft: Israel (draft v0.5)

Status: DRAFT for discussion. Not approved by the maintainer or by a native Hebrew reviewer.
Date: 17 October 2026. Evidence: upstream/main 46abd99c, 271 keys in each language. This revision reconciles the cited examples and decisions with that snapshot. It is not a full review of every added string. Score, minimap tooltips, icon-only navigation and the new city, training and pioneer strings remain outside the detailed copy review.

Revision 19 October 2026: targeted corrections against upstream/main 9e571342 address the #970 review. The original 46abd99c snapshot and counts are historical evidence; this revision does not claim a full audit of all newer strings.

This guide covers short player-facing text: labels, buttons, status lines, toasts, refusals, errors, hints and empty states. It does not cover event card prose (`docs/WRITING_EVENTS.md`, `docs/WRITING_GUIDE.md`) or historical sourcing (`docs/CONTENT_POLICY.md`). Those stay as they are.

Revision 21 October 2026: map-hint quotations were checked against upstream/main 001a59d0 (#1134); this remains a targeted update.

## How to use this guide

- A writer reads the rules and the review checklist.
- A reviewer runs the checklist in order and cites rule IDs, for example "this breaks UI-05".
- An AI agent reads the agent brief first and the rules when it needs the reason.
- Every example is a real string from `strings.en.yaml` or `strings.he.yaml`. Rewrites are labelled "candidate" and are proposals only.

The check report numbers its groups with its own codes (UI-02, UI-03 and so on). They do not always match the rule IDs below, so each "Check:" line names the report group it means.

Rule status legend: **observed** (already the practice, now written down), **proposed** (new), **maintainer** (needs Eyal's decision), **native** (needs a native Hebrew reviewer).

## Agent brief

Use when writing or reviewing short player-facing text for Statecraft: Israel. Never present a proposal as an approved rule. Never finalise Hebrew; flag it for a native reviewer.

1. Plain, direct, sober. Name the thing and the number. No filler, no drama (UI-01).
2. Sentence case for phrases and sentences. Labels, button names included, are sentence case too: "End turn", "New game", "Public support" (UI-02, settled in #630).
3. "Label: value" for facts, a middle dot between facts on one line (UI-03).
4. Counts as "Label: N". No "(s)" (UI-04, proposed).
5. A refusal says what could not be done, the rule that blocked it, and what the player can do next (UI-05, proposed).
6. Never show a raw exception message, a file path or a content id. Convert the cause to player wording first (UI-07).
7. Avoid "valid", "invalid", "please", "sorry", "oops", "unable". No "we" (UI-06, UI-11; proposed).
8. A standalone message ends with a period. Labels, status lines and strings inserted into another string do not (UI-08, proposed).
9. Buttons and actions start with a verb and name the destination. No "click here" (UI-09).
10. An empty state tells the player what to do next (UI-10).
11. No left, right, above or below. The Hebrew layout mirrors (UI-12).
12. Dates, money and percentages are formatted by code, never typed into a string (UI-13).
13. Use the glossary's preferred terms. The UI says "Defence"; an office title keeps its official "Defense" (UI-14).
14. English and Hebrew keys and placeholders match exactly. The repo's tests already check this (UI-15).
15. Hebrew: maqaf before a numeral or Latin letters, one gershayim style, isolate Latin text and values of unknown direction. Gershayim is adopted in #630; the remaining proposals need native review. Preview with `?lang=he` (UI-16).

## Rules

Each rule has a reason, the evidence behind it, real examples, and how it is checked. "Source" names where the idea comes from.

### UI-01 Voice: plain, direct, sober
Status: observed. Reason: the project voice (`docs/WRITING_GUIDE.md`) is direct and concrete; Apple's writing guidance asks for one voice with tone adjusted to the situation, and a serious moment gets a plain, direct tone (Apple HIG, writing).
- Good: "Costs {0} a year in pay and lost work" (`defence.mob_cost`). "Nothing notable happened this turn." (`why.nothing`).
Check: manual.

### UI-02 Capitalisation: one style per element type
Status: adopted (maintainer, #630, merged 15 October 2026). Reason: Apple asks for one capitalisation style per element type, and Google says a button named in running text is written as it appears. Version 0.2 found six two-word labels in Title Case among sentence-case strings. #630 changed them.
- Sentence case now, everywhere: "Public support", "International support", "Foreign currency" (the top-bar captions), "New game" (`hud.new_game`, `start.new_game`), "End turn" (`hud.end_turn`). "Air Force" is a proper name and stays.
- The top-bar captions follow the content display names, with Money (net) explicitly identifying the net position ("International support", "Foreign currency" in `model.yaml`). The test is `HudTests.TopBarCaptionsAreTheContentNames`.
- A button named in running text is written as the button shows it: "Submitted: takes effect at End turn" (`budget.queued`).
- The owner's decision text for the resource layer writes "Public support" and "Social cohesion" in sentence case, which now matches the top bar.
- The Hebrew has no capitals, so the rule is for English only.
Check: a pattern search over the strings files: no hits expected, apart from "Air Force". A hit is a regression.

The glossary entry `label-capitalisation` records the same decision.

### UI-03 "Label: value", with a middle dot between facts
Status: observed. Reason: the file already does this, and it keeps lines scannable.
- "Party: {0}" (`card.party`). "Coalition: {0} of {1} {3} · stability {2}" (`cabinet.coalition`; the unit, "seats" or "members", is now its own string by body). "Money {0} · debt {1} · foreign currency {2}" (`budget.money`).
- A label should name what the number is. "Majority: {0} {1}" (`cabinet.majority`) is the number of seats a majority needs, but it reads like a holding next to "Coalition: {0} of {1}". Candidate with the same placeholders: "Needed for a majority: {0} {1}". Optional, and worth a cold-read test before it goes to the maintainer.
Check: manual.

### UI-04 Counts: write "Label: N"; no "(s)"
Status: observed in Hebrew, proposed for English. Reason: Google advises against optional plurals such as "key(s)" because they are ambiguous and translate badly, and Hebrew needs a separate form for exactly 2 (Unicode CLDR 48). A "Label: N" line needs no plural form in either language. The game has no general plural mechanism (`UiText` is a key to `string.Format` lookup), so this pattern is the way to avoid the problem. The repo's own Hebrew test pins `event.more` as "Label: N" and names the test "count neutral" (`test_hebrew_review_wording_is_count_neutral_and_uses_knesset_terms`), so the maintainer already treats this as the rule for Hebrew. Some messages already choose singular and plural keys explicitly (UI-17), including the city-panel group and turn counts added in #869.
- Weak: "{0} more decision(s) waiting" (`event.more`, English).
- Good: the Hebrew counterpart "החלטות ממתינות נוספות: {0}" (`event.more`, Hebrew), which is already "Label: N".
- Candidate English rewrite: "More decisions waiting: {0}".
Check: a pattern search over the strings files.

### UI-05 A refusal says what, why and what next
Status: proposed (observed in the save and start-up strings). Reason: NN/g and GOV.UK agree that an error message should name the specific problem and say how to fix it. Neither requires a separate "why", but in a game the reason is a rule the player can act on, so it is kept. Where nothing can be done, say what happens instead.
- Good: "The old saved game could not be removed; it will be replaced after your next turn." (`newgame.delete_failed`): what happened and what happens next.
- Good: "Statecraft could not start: {0}. Reload the page." (`boot.failed`): what happened and what to do.
- Good: "Your saved game could not be loaded ({0}). A new game has started." (`save.unreadable`).
- Mixed: "Not enough attention (need {0:0.0}, have {1:0.0})" (`order.no_attention`) gives the rule and the numbers but no next step. It is also shown after "Not done: " (`toast.refused`), so the player reads "Not done: Not enough attention (...)".
- Weak: "Your game could not be saved." (`save.write_failed`): what, but no why and no next step.
- Candidate rewrite for `order.no_attention`: "this order needs {0:0.0} Attention and {1:0.0} is left. Remove a queued order or end the turn." (check against the UI before use: the code has a withdraw command, but the screen that offers it was not checked).
Note: the Hebrew `order.no_attention` is pinned by name in `test_hebrew_review_wording_is_count_neutral_and_uses_knesset_terms`, so any change to it needs a test change in the same patch.
Check: manual; the checker lists every error string for review (report groups UI-05).

### UI-06 Words to avoid in system text
Status: proposed. Reason: GOV.UK lists "valid", "invalid", "please", "sorry", "oops", "forbidden" and "you forgot" among words to avoid, NN/g advises against "invalid", "illegal" and "incorrect", and Apple advises against "oops" and similar. The words sound like blame or hide the problem.
- Weak: "That order is not valid" (`order.invalid`): uses "valid", names no problem, gives no next step.
Check: a pattern search over the strings files: one hit today.

### UI-07 Raw errors never reach the player
Status: adopted for the cited order failures (maintainer, #630 and #676). Reason: raw exception messages and internal ids are developer text. CONTRIBUTING rule 4 requires player wording.
- `order.failed` reads "The turn could not run. Your orders were cleared. Give them again." The recovery step was adopted in #622 and implemented in #676. SimBridge clears the queue and logs the exception.
- `order.unknown` reads "Unknown {0}": the kind of thing only; the id is logged. Its Hebrew kind words need their own localization review.
- `save.reason_other` supplies the fixed reason "the file could not be read or replayed". Startup failures pass the fixed reason boot.reason_other ("its files failed to load"); the exception text stays in GameBoot.Error and the log (#705).
Check: inspect both the localized pattern and the value passed into it; a safe pattern can still receive raw developer text.

### UI-08 End punctuation
Status: proposed house rule, informed by current strings. Complete messages use periods; labels and short status fragments do not. The adopted multi-sentence order.failed message ends with a period even though it follows toast.refused.
- Standalone examples: "Nothing notable happened this turn." and "Your game could not be saved."
- Fragments: "Level {0} of 5" and "Submitted: takes effect at End turn".
- Exceptions for review: info.hint has no final period; some defence status lines read as sentences. Judge them by their role on screen rather than a stale count of punctuation in the file.
Check: manual, supported by the current string-pattern report.

### UI-09 Buttons and actions start with a verb and name the destination
Status: observed. Reason: Apple asks for a verb on buttons and says to name the destination instead of "click here". Google gives the same advice for links.
- "Hide" (`event.hide`), "Appoint to {0}" (`cabinet.pick`), "Place a transit camp" (`info.build_camp_plain`), "Open the Defence screen" (`unit.defence`), "Submit the budget" (`budget.submit`), "Play the recording" (`start.listen`).
Check: manual.

### UI-10 An empty state gives a next step
Status: observed. Reason: Apple says an empty state should say what to do next and should not carry essential information that disappears.
- Good: "Nothing has happened yet: end the turn to see its causes." (`why.empty`).
- Good: "No sourced front line here on this date" (`info.control_none`): says exactly what is missing and why, though it gives no next step.
Check: manual.

### UI-11 Second person for the player's own things; no "we", no "oops"
Status: observed. Reason: Apple advises using "your" sparingly and avoiding "we" because it is unclear who "we" is, especially in errors. Check pronouns in their full message context; counts change as strings are added.
- "{0} (your appointee)" (`card.appointed_name`), "Your saved game could not be loaded ({0})." (`save.unreadable`).
Check: a pattern search over the strings files.

### UI-12 No directional words
Status: observed. Reason: Google says directional words such as above, below and right-hand fail accessibility and localisation. The Hebrew layout mirrors, so "on the right" would be wrong in half the game. The current exception is settings.tiltshift, which describes blur at the top and bottom of the map. This describes the visual effect rather than directing the player to a control.
Check: a pattern search over the strings files: one hit, settings.tiltshift, reviewed as the descriptive exception above.

### UI-13 Formatting belongs to code
Status: observed. The strings header already records the date exceptions; the stale-header finding was fixed in #630 and subsequent date localization expanded them (#687, #701).
- The map's front-line date, top bar, Budget, Why and info panel dates use date.* via Control1948.DateText or LineDateText. English and Hebrew each supply month names and their own patterns.
- PlayerText.Date remains English. Check remaining callers individually; do not claim that all displayed dates are localized or that all still arrive in English.
- Money and percentages still use PlayerText formatting. Spoken meter tooltips expand million through hud.say_million; they do not establish a localized currency format.
- UiText.Format surrounds inserted values without Hebrew with U+200E marks. Formatting language and display direction are separate checks.
- A year in a citation or label, such as info.localities, is not a runtime date format.
Check: the repo's formatting tests and the actual caller for the screen being edited.

### UI-14 Use the glossary's terms
Status: proposed. Reason: terminology drifts between files and between English and Hebrew (see the glossary). A string should use the preferred term for its concept.
- Spelling: the UI is British ("Defence"), and an office keeps the official US spelling of its title ("Minister of Defense"). The maintainer wrote this rule in a comment in `content/offices.yaml` ("Spelling is deliberate, do not \"fix\" it"), so `defence.command` ("Minister of Defense {0} · Chief of Staff {1}") follows it. The check script skips "Minister of Defense" for that reason. The open point is only that the rule lives in a YAML comment, not in a style document.
- Resource and meter names: the owner decided on 15 October that the five resources are Money, Food, Arms, Social cohesion and International Support, with approval shown as Public support (`docs/gdd/cutover-resource-layer.md`). The current top bar uses Money (net), International support and Public support. Social cohesion is exposed for the GDD loop. These names are adopted; future screens must follow them. The glossary carries each name under `resources-and-meters`.
Check: a pattern search over the strings files; the glossary lists the terms.

### UI-15 English and Hebrew keys and placeholders match
Status: observed. Reason: a mismatch breaks the game or shows the wrong value. At 46abd99c there were 271 keys in each file and no mismatch in key names or placeholder indexes. The repo already tests this: `test_hebrew_keys_match_english_and_every_value_is_nonempty` and `test_hebrew_keeps_complete_format_tokens_and_logical_text_order` in `tools/tests/test_hebrew_ui.py`, and `EachEnglishStaticTextIsTheUxmlsOwnText` and `InHebrewEveryStaticTextIsTheHebrewString` in `HebrewUiTests.cs`. The local check adds nothing the tests do not cover, and is kept only as a convenience.
Check: a pattern search over the strings files and the repo's tests.

### UI-16 Hebrew details
Status: gershayim adopted (maintainer, #630); other language recommendations need native review. A native reviewer may overrule the gershayim choice through #564. Mixed-direction changes need a test in the game.
- Prefix before a numeral or Latin letters: the Academy of the Hebrew Language punctuation rules use the maqaf, as in "ה־12" or "ב־DNA". For example, the strings use a hyphen ("יישובים ב-1948: {0}", `info.localities`) and a maqaf in another ("מ־{0} עד {1}", `why.turn`).
- Gershayim in acronyms: the strings checked use the typographic ״ ("הרמטכ״ל", "האו״ם"), so the file is consistent. The Academy's published examples use a straight quote and say nothing about the typographic character. The owner chose ״ in #630. Apply that choice unless the native reviewer records an overrule in #564.
- Mixed direction: W3C and Unicode advise isolating each opposite-direction phrase, and any value of unknown direction, so punctuation and numbers do not jump to the wrong side. Since #543 the repo does this for inserted values: `UiText.Format` wraps each value that has no Hebrew in it in left-to-right marks (U+200E), and a rendered-pixel test in `HebrewValuesTests` checks that a minus sign and an English date draw correctly. The repo also sets each text's direction from its own first letter (`InHebrewEachTextTakesItsDirectionFromItsOwnWords`), and its Hebrew test rejects the explicit embedding and override characters U+202A to U+202E. Unity's manual does not mention isolates, so any other approach needs a test in the game. What the tests reviewed do not cover is a Latin word typed inside a Hebrew sentence, as opposed to an inserted value.
  - Three strings to test: "ספיטפייר LF Mk IXe" (`unit.aircraft.spitfire`), "לא ניתן היה להפעיל את Statecraft: {0}. יש לטעון את הדף מחדש." (`boot.failed`), where a colon follows a Latin word, and "לחיצה, Enter או רווח לדילוג · Esc מדלג על הכול" (`outcome.skip`).
  - Settled since 14 October: the recording credit no longer keeps a name in Latin letters. It is "הקלטה: ראלף הלינגר, ארכיון המדינה" (`start.recording_credit`), spelt as the Israel State Archives' own description spells it, and the Hebrew UI test now rejects Latin letters in any static Hebrew text, names included.
  - How to test: the original explicit-language path was pinned by `HebrewIsOnlyAskedForByHand`; since #1058 Settings also offers English / עברית. Test the current switch and the `?lang=he` path (#506).
- Address and register on buttons and instructions: the Israeli National Digital Agency's guide to government language (section 1.3.3, read by the author as rendered slides; reuse terms not found, paraphrased) says not to address users only in the masculine singular and not to use slash or dot forms. It prefers, in order: a setting that adapts the form to the user; a second person without a gender marker (words ending in ך, ת or ה, such as "שלך"); a neutral first person; then an action noun, an infinitive, or rephrasing such as "יש לשלוח". The strings already follow this apart from one masculine imperative: "הסתר" (`event.hide`). Examples using "יש ל..." include (`info.hint`, `newgame.confirm`, `why.empty`, `budget.unsaved`, `boot.failed`) and action labels use action nouns ("הקמת מעברה", "בניית דיור", "מינוי לתפקיד", "פתיחת מסך הביטחון"). Candidate: replace "הסתר" with "הסתרה" or "להסתיר". A setting that lets the player choose a form of address is the guide's first priority, but it is a design decision for the owner. The native reviewer confirms all of this.
Check: a pattern search over the strings files.

### UI-17 Plurals
Status: proposed, maintainer. Reason: English has two plural categories and Hebrew has three (one, two, other; Unicode CLDR 48). The game has no general plural mechanism, so UI-04 is the rule and new sentences whose wording changes with a number should be avoided. Existing precedents include city.groups_one/city.groups and city.turns_one/city.turns (#869), objective early/late month messages in EventCardPresenter.ObjectiveLine, and the top bar's spoken form, which picks between two keys by count, `hud.say_point` and `hud.say_points` (`HudModel.cs`, by `git grep`), in Hebrew "{0:0} נקודה" and "{0:0} נקודות". Two keys cover one and other. They have no key for exactly 2, which the Hebrew categories name; the native reviewer should say whether that matters for a digit numeral. A general mechanism is a code change in `unity/` and a proposal to the maintainer; these explicit selectors are precedents to assess before introducing a general mechanism.
Check: a pattern search over the strings files: one string today, `event.more` in English.

### UI-18 Text and accessibility
Status: proposed. Reason: WCAG 2.2 is a design target for the web, not a conformance claim for this game. The criteria that affect copy are: labels name their purpose, an error says what is wrong and how to fix it, colour is not the only signal, and abbreviations are explained. Contrast and size are design checks and belong to issue #41.
- Example: the map legend gives each colour with its meaning in words ("Held by Israel: blue · Held by Arab forces: orange-red · Contested or no-man's land: grey, dashed", `map.legend_held`), so the text carries the information and not the colour alone. Whether the map itself adds a second cue for blue and orange-red was not checked.
Check: manual.

## Examples bank

| Key | String | Verdict | Rule |
|---|---|---|---|
| `newgame.delete_failed` | The old saved game could not be removed; it will be replaced after your next turn. | Good | UI-05 |
| `boot.failed` | Statecraft could not start: {0}. Reload the page. | Good (the `{0}` is the fixed `boot.reason_other`, #705) | UI-05, UI-07 |
| `save.reason_other` | the file could not be read or replayed | Good, a fixed player reason | UI-07 |
| `why.empty` | Nothing has happened yet: end the turn to see its causes. | Good, empty state with a next step | UI-10 |
| `budget.queued` | Submitted: takes effect at End turn | Good, state and consequence | UI-03 |
| `budget.unsaved` | Changed: submit to put it to the vote | Good, state and next step | UI-05 |
| `event.more` (en) | {0} more decision(s) waiting | Weak | UI-04 |
| `order.invalid` | That order is not valid | Weak | UI-06, UI-07 |
| `order.failed` | The turn could not run. Your orders were cleared. Give them again. | Adopted recovery wording, #676 | UI-07 |
| `order.unknown` | Unknown {0} | Fixed in #630 (kind only) | UI-07 |
| `order.no_attention` | Not enough attention (need {0:0.0}, have {1:0.0}) | Mixed, no next step, repeats "Not" after "Not done:" | UI-05 |
| `save.write_failed` | Your game could not be saved. | Weak, no why or next step | UI-05 |
| `info.hint` | Click the map, or Tab to it and press Enter, to inspect a hex; right-drag to move the map | Exception to settle | UI-08 |
| `cabinet.majority` | Majority: {0} {1} | Reads as a holding; candidate "Needed for a majority: {0} {1}" | UI-03 |
| `hud.say_point` | {0:0} point | Good, the two-key plural (with `hud.say_points`) | UI-17 |
| `hud.caption.support` | Public support | Sentence case since #630 | UI-02 |
| `info.control_none` | No sourced front line here on this date | Good, says what is missing | UI-10 |
| `info.localities` (he) | יישובים ב-1948: {0} | Hyphen where the Academy points to the maqaf | UI-16 |

## Review checklist

Run in order. A "no" is a finding, cited by rule ID.

1. Does it say plainly what it means, with the number or the name (UI-01)?
2. Does it follow the capitalisation rule (UI-02): sentence case for every label, button names included?
3. Facts as "Label: value", joined with a middle dot (UI-03)?
4. If it contains a count, is it "Label: N" with no "(s)" (UI-04, UI-17)?
5. If it is a refusal or an error: does it say what could not be done, why, and what the player can do next (UI-05)?
6. Could it show a raw exception message, a path or a content id (UI-07)?
7. Does it avoid "valid", "invalid", "please", "sorry", "oops", "unable" and "we" (UI-06, UI-11)?
8. Does a standalone message end with a period, and a fragment or inserted string not (UI-08)?
9. Does an action start with a verb and name its destination (UI-09)?
10. If it is an empty state, does it give a next step (UI-10)?
11. No directional words (UI-12)?
12. Dates, money and percentages come from code (UI-13)?
13. Terms match the glossary (UI-14)?
14. Same keys and placeholders in Hebrew (UI-15)?
15. For Hebrew: maqaf, gershayim, Latin text isolated, native reviewer named (UI-16)?
16. Does it name a turn length or another value the GDD will change? If so, match the current monthly MVP rules and keep future design choices explicit.

## Open decisions

For the maintainer:
- Confirm the punctuation rule (UI-08). The capitalisation rule (UI-02, glossary `label-capitalisation`) is settled by #630.
- The remaining refusal rewrites, such as order.no_attention (UI-05). The order.failed recovery wording is already adopted and implemented (#676).
- Whether the two-key plural (`hud.say_point`, `hud.say_points`) is enough, or a general mechanism is wanted (UI-04, UI-17).
- Where the spelling rule is written: today only in a comment in `content/offices.yaml` (UI-14).
- Optional: the "Majority" label (UI-03).

For the native Hebrew reviewer: every point in UI-16, and the Hebrew of any candidate rewrite.

## Evidence and limits

- Real strings: upstream/main 46abd99c, 17 October 2026, 271 keys per language. GDD v0.2 defines the MVP from 15 May 1948 to 15 January 1952, with monthly turns. Cited string checks do not certify every code or wording claim.
- External sources: Google developer documentation style guide (UI elements, pluralization), Apple Human Interface Guidelines (writing), NN/g error message guidelines, GOV.UK error message component, Unicode CLDR 48 plural rules, W3C internationalisation articles, Unicode UAX 9, the Unity 6 manual on the advanced text generator, the Academy of the Hebrew Language punctuation rules, WCAG 2.2. All except WCAG 2.2 were re-opened and checked by the author on 14 October 2026, and the claims used here were confirmed. WCAG 2.2 was not re-opened and is used only as a design target. The Israeli National Digital Agency's guide to government language, section 1.3.3 (slides 52 to 57), was read directly by the author.
- Not covered: Microsoft's style guide (word list unreachable), strategy-game text patterns, image captions.
- The check script finds patterns. It does not decide whether a string is good.
