# Hebrew localisation guide for Statecraft: Israel (draft v0.5)

Status: DRAFT for discussion. Not approved by the maintainer. **New Hebrew recommendations need native review. Existing maintainer decisions, including gershayim in #630, remain adopted unless explicitly overruled.** Hebrew in this draft has been reviewed by a non-native reader and not finalised.
Date: 17 October 2026. Evidence: upstream/main 46abd99c, 271 keys in each language. Cited strings were reconciled with the snapshot; new city, training, pioneer and other added strings have not received a full native-language review.

Publication update, 18 October 2026: the examples below were audited at 46abd99c. Since that snapshot, #928 adopted a verb-free info.built date label after native review, and #930 recorded native reads of government, legislature and office names. Those accepted changes stand; the guide does not reopen their approval. This draft is based on db4350f8 and is not a full review of subsequent Hebrew changes.

Revision 19 October 2026: targeted implementation and terminology checks at upstream/main 9e571342 address the #970 review. The original snapshot count is dated, not a claim about every subsequent string or its native review.

This guide covers how Hebrew is written and displayed in the game and its surfaces. It builds on the English UI microcopy guide and the glossary. It does not set the prose rules for event cards; those remain in WRITING_GUIDE.md and WRITING_EVENTS.md. Event fields and unit-card names now use Hebrew where supplied, with a per-field English fallback (HE-12).

Revision 21 October 2026: the budget and map-hint quotations and Beri's agreement note were checked against upstream/main 001a59d0 (#1136, #1134). The original snapshot evidence remains dated above.

Revision 21 October 2026, localisation follow-up: event/unit field fallbacks and Cabinet government/legislature names were checked against upstream/main e123971c. Native review status remains separate from implementation.

## How to use this guide

- A writer or translator reads the rules and the checklist.
- A native reviewer works through the "For the native reviewer" lists, decides each open point, and signs off by name and date (HE-14).
- An AI agent reads the agent brief first. It must never finalise Hebrew and must flag every Hebrew string for native review.
- Every quoted string is a real string from `strings.he.yaml` or `strings.en.yaml`, or a real line from the sources named. Rewrites are labelled "candidate".

Rule status legend: **observed** (already the practice), **proposed** (new), **maintainer** (needs Eyal's decision), **native** (needs a native Hebrew reviewer), **code** (needs a code change).

## Checked Hebrew behavior (original snapshot plus dated updates)

- At 46abd99c, `strings.he.yaml` had 271 keys, the same keys and placeholders as `strings.en.yaml`, and repo tests enforce this (HE-01). Originally Hebrew was selected by hand with `?lang=he` on the web or `-lang he` on desktop (#193, #506). Since #1058 the Settings screen also offers English / עברית and applies the choice immediately. Eyal's 13 October status on #193 said the Hebrew font and right-to-left layout exist.
- Each text takes its direction from its own first letter, and a repo test checks it. Older test comments about English dates predate the localized date paths in #687 and #701.
- The map, top bar, Budget, Why and info panel dates now use localized date.* keys (#687, #701). PlayerText.Date remains English for other callers. Money and untranslated content still need separate review.
- Since #543 `UiText.Format` puts an invisible left-to-right mark (U+200E) on both sides of each value it inserts into a Hebrew pattern, unless the value already contains Hebrew. An English date, an amount or a minus sign then reads as one run.
- Event titles, bodies and option labels now use their Hebrew fields where supplied, with English fallback for each missing field (EventCardPresenter and ContentNames.Fill). UnitCard uses sourced Hebrew unit names and optional place, commander and sector fields where supplied, with English fallback per field. Native review remains pending on #564; displaying Hebrew does not establish approval. Since #558 the content files carry sourced Hebrew names for offices (with feminine forms where a woman held the office), parties, governments, legislatures and places. At 9e571342, Unity reads Hebrew names of places; #985 added people, office and party names in Cabinet refusal reasons, and #861 added ContentNames.Of for people, offices, parties, governments and legislature body_he in the Cabinet/Defence paths.

## Agent brief

Use when writing or reviewing Hebrew for Statecraft: Israel. Never present a proposal as an approved rule. Never finalise Hebrew; flag it for a native reviewer.

1. Hebrew mirrors English key for key, with the same placeholders (HE-01).
2. One Hebrew word per concept, from the glossary. Do not introduce a second word for a concept already in use (HE-02).
3. Do not use the masculine imperative or slash forms. Prefer an action noun, "יש ל...", a word ending in ך or ת, or a neutral first person (HE-03).
4. Write counts as "Label: N". Do not build sentences whose wording changes with the number (HE-04).
5. Full spelling (ktiv male) for modern text (HE-05).
6. Punctuation is ASCII, as in English. Acronyms take gershayim before the last letter. Use the typographic gershayim ״ (U+05F4) everywhere, in strings and in content (HE-06, settled in #630).
7. A prefix before a numeral or Latin letters takes a maqaf, per the Academy (HE-07).
8. Digits are Latin. Do not type dates or money into a string. Month names come from the `date.*` keys; money is not yet localised (HE-08).
9. Inserted values are wrapped in direction marks by the UI (#543). Static Hebrew text has no Latin letters, names included. Do not put a colon or bracket directly after a Latin word typed in a Hebrew sentence without checking the display. Preview with `?lang=he` (HE-09).
10. No left or right, above or below (HE-11).
11. Do not write a verb or pronoun after a placeholder in a form that only fits one gender, when the placeholder can be a body or a person of either gender (HE-15).
12. New Hebrew recommendations need native review. Preserve adopted decisions and record any explicit overrule (HE-14).

## Rules

### HE-01 Parity with English
Status: observed. Reason: a missing key throws an error and a mismatched placeholder shows the wrong value. At 46abd99c the two files had 271 keys each and no mismatch in key names or placeholder indexes (checked by script). The repo tests this: `test_hebrew_keys_match_english_and_every_value_is_nonempty` and `test_hebrew_keeps_complete_format_tokens_and_logical_text_order`, and the Unity test `InHebrewEveryStaticTextIsTheHebrewString`.
Check: the repo's tests; a pattern search over the strings files as a local convenience.

### HE-02 One Hebrew word per concept
Status: proposed, native. Reason: the same concept can have more than one Hebrew word, and in places one Hebrew word covers two English concepts (see the glossary). Apple's guidance asks for a common-terms list used consistently.

Settled by the owner in #550 (shown so nobody reopens them by mistake):
- Seats and members: a unit string by body, "מנדטים" for an elected body (`cabinet.seats`) and "חברים" for the appointed Provisional State Council (`cabinet.seats_appointed`). A repo test pins both. The older `cabinet.members_unit` ("חברים") is still in the file.
- Minister rating: "כישורים" (`card.competence`), beside "נאמנות" (`card.loyalty`). It was "כשירות" on 14 October.
- Ladder caption: "המצב הביטחוני" (`defence.section_ladder`). It was "דרגת ביטחון" on 14 October.
- Words for the resource layer, decided by the owner on 15 October: כסף, מזון, נשק and תמיכה בינלאומית, with לכידות חברתית for Social cohesion (`docs/gdd/cutover-resource-layer.md`). The current top-bar captions are כסף (נטו) and תמיכה בינלאומית; these are adopted names, not pending choices.
- Office, government and body names: sourced Hebrew forms are in the content since #558, from the Knesset's records where they give one (the `title_he`, `name_he` and `body_he` fields), and the UI now reads the Hebrew forms through ContentNames.Of where supplied (#861); English remains the fallback for a missing form.

Still open:
- The ladder's own words. "דרגה {0} מתוך 5" (`hud.level`) and "הורדת הדרגה" (`defence.lower`) use one word, and "רמת ביטחון {1} מתוך 5" (`unit.national`) uses another. The five rung lines (`defence.rung1` to `defence.rung5`) number the steps, and the Hebrew README says "סולם הסלמה". English says "Level" and "security level".
- Settlement and locality: "יישובים: {0}" (`info.owned`) and "יישובים ב-1948: {0}" (`info.localities`) use one word for two English concepts.
- Cabinet and government: "ממשלה" labels the Cabinet screen (`hud.nav.cabinet`) and is also the word for a government, where the English separates them. The content's government names (ממשלת ישראל הראשונה and so on) repeat the word.
- Terrain: "בקע" (`info.biome.valley`) for "Valley" and "עמק" (`info.biome.lowland`) for "Inland lowland". They may be swapped. "עמק" appears to be the usual word for a valley and "בקע" a rift or basin, which the reviewer should confirm.
For the native reviewer: choose the word for each open concept above and record it in the glossary's Hebrew column.

### HE-03 Form of address
Status: proposed, native; source read directly. Source: the Israeli National Digital Agency's guide to government language, section 1.3.3 (paraphrased; reuse terms not found). It says: do not address users only in the masculine singular; do not use slash or dot forms; first priority is a setting that adapts the form to the user; second, a second person without a gender marker (words ending in ך, ת or ה); then a neutral first person; then an action noun, an infinitive, or rephrasing such as "יש לשלוח".
- The strings already follow this apart from one masculine imperative: "הסתר" (`event.hide`). Candidate: "הסתרה" or "להסתיר".
- Examples using "יש ל..." include "יש ללחוץ על המפה, או לעבור אליה ב־Tab וללחוץ Enter, כדי לבדוק משושה; גרירה בלחצן הימני מזיזה את המפה" (`info.hint`). Action-noun labels include "הקמת מעברה ({0})" (`info.build_camp`), "הגשת התקציב" (`budget.submit`), "העלאת הדרגה" (`defence.raise`), "השמעת ההקלטה" (`start.listen`) and "איפוס" (`budget.reset`). One uses the ך ending: "{0} (מינוי שלך)" (`card.appointed_name`).
- Two forms the reviewer should look at: "טוען את ההקלטה…" (`start.loading`), a masculine present-tense form with the program as its implied subject; and "המשך" (`start.continue`), which reads as a noun and also matches a masculine imperative. The Hebrew README addresses the reader as "אתם" (plural), a different choice again.
- A player setting for the form of address is the guide's first priority. It is a design decision for the owner and is not proposed here.
For the native reviewer: confirm that "יש ל..." and action nouns are right for a game, rule on the two forms above, and choose between "הסתרה" and "להסתיר".

### HE-04 Counts and plurals
Status: observed in the Hebrew strings, maintainer for a mechanism. Reason: Hebrew has three plural categories for cardinal numbers (one, two, other; Unicode CLDR 48), and the game has no general way to choose a form by count. The Hebrew file already writes counts as "Label: N": "החלטות ממתינות נוספות: {0}" (`event.more`), which needs no plural form, and a repo test pins it as count-neutral. The top bar picks between two keys by count: "{0:0} נקודה" (`hud.say_point`) and "{0:0} נקודות" (`hud.say_points`). The objective early/late month messages also have singular variants selected in EventCardPresenter.ObjectiveLine. The city panel adds city.groups_one/city.groups and city.turns_one/city.turns (#869). These explicit selectors do not provide a general language-category mechanism; native review must assess their wording.
- Gendered numerals (the form of a number depends on the gender of the counted noun) are a separate question. No source was found, and the rule is not written here.
For the native reviewer: list any strings where a number appears next to a noun and say which form is needed, and say whether the two-key form of `hud.say_point` is enough for a digit numeral.

### HE-05 Spelling
Status: proposed, native. Reason: the Academy of the Hebrew Language calls full spelling (ktiv male) the main spelling of Hebrew today, and r12a's notes say it is the predominant approach in everyday use. Proper names often keep traditional spelling (the Academy's examples: יעקב, משה, ירושלים).
For the native reviewer: confirm full spelling for all UI text, and decide the spelling of historical names that content will need.

### HE-06 Punctuation
Status: gershayim adopted (maintainer, #630), subject to a native-reviewer overrule in #564. Other punctuation recommendations remain proposed for native review.
- Hebrew uses ASCII punctuation: full stop, comma, question mark, exclamation mark and colon work as in English, and the question mark is not mirrored (r12a).
- Acronyms take gershayim before the last letter, for example "הרמטכ״ל" and "האו״ם". The correct character is the typographic gershayim ״ (U+05F4); a straight quote is a common substitute because of keyboards, and r12a notes it sits too high (r12a). The Academy's own examples use the straight quote and do not discuss the typographic character. The strings checked use ״, including defence.command, defence.ceasefire, defence.ceasefire_stops, defence.ceasefire_starts and the newer cabinet.reason.is_chief_of_staff / cabinet.reason.not_career_officer (#985). The abbreviation mark geresh is the typographic ׳ (U+05F3), in "מס׳ 1" (`start.source`) and in `info.hex`. The gershayim choice ״ was adopted in #630 for strings and content. A native reviewer may overrule it in #564. Geresh and quotation-style recommendations still need native review.
- Quotation marks are not paired. In principle ” opens and closes and ’ nests, but straight quotes are common in practice (r12a). The strings contain no double quotation marks. The one string that put straight single quotes around a placeholder, `order.unknown`, no longer does: since #651 it reads "לא ניתן לזהות {0}" and shows no id (UI-07). No string quotes a value now. Choose one style before content needs it.
- #630 made the content match the strings. `content/offices.yaml` gives the Haganah post as רמטכ״ל ההגנה, and a scan of `content/parties.yaml` and `content/offices.yaml` found no straight quote inside a Hebrew word. The strings and the content now use ״ alike. The #630 description also lists a title and one person's name; those were not re-checked here.
For the native reviewer: record any overrule of the adopted ״ convention, and choose a quotation style.

### HE-07 Maqaf
Status: proposed, native. Reason: the Academy's punctuation rules use a maqaf when a prefix meets a numeral or Latin letters, for example "ה־12" and "ב־DNA". r12a says the maqaf is the proper character for a hyphen between compounds, that keyboards often lack it so a hyphen-minus is substituted, and that lines should not break next to it.
- Today: "יישובים ב-1948: {0}" (`info.localities`) uses a hyphen-minus. "מ־{0} עד {1}" (`why.turn`) uses a maqaf. The date pattern joins the prefix to the month name with no hyphen ("{0} ב{1} {2}", `date.day_month_year`), which is ordinary Hebrew for a prefix before a word.
- Code: a placeholder written straight after a prefix ("ב{0}") is joined to a Hebrew value ("בתל אביב"), and UiText.Format adds the maqaf when the value starts with a digit or a Latin letter ("ב־Haifa", for a name with no Hebrew form yet). Write "ב־{0}" only where the value is always a numeral or a date.
- Candidate: "יישובים ב־1948: {0}".
- A second place: the map legend joins two colour words with a hyphen-minus, "כתום-אדום" (`map.legend_held`, also `map.legend_armistice`). r12a's note on the hyphen between compounds points to the maqaf there too. Candidate: "כתום־אדום".
For the native reviewer: confirm the maqaf for prefixes before numerals and Latin letters, for the colour compound, and the numeral range style in `why.turn`.

### HE-08 Numbers, dates and money
Status: proposed, maintainer, code.
- Digits are Latin. A range written with a hyphen-minus reads left to right inside Hebrew text (r12a), while the Academy writes a numeric range right to left with an en dash, the first number at the right (פרקים 5 to 12, not 12 to 5). The two behave differently under the bidi algorithm, so avoid typed ranges, as the strings already do with "מ־{0} עד {1}" (`why.turn`), and test any range in Unity before adopting one.
- The map, top bar, Budget, Why and info panel use localized date.* keys via Control1948.DateText or LineDateText (#687, #701). PlayerText.Date itself remains English; remaining callers need individual checks. Money still uses PlayerText.Money and its currency prefix from content. UiText.Format wraps inserted values without Hebrew in U+200E marks, which addresses direction but does not translate the value.
- Currency needs a decision. The money format is "IL 1.2m". For the years the game covers, "IL" is, as far as is known, the Israeli pound (an inference, not checked in a source), not the new shekel, so the shekel sign ₪ that Hebrew UI guidance normally uses would be wrong. How the Israeli pound is written in Hebrew is a question for the native reviewer and Eyal.
For the native reviewer: confirm the twelve month names as the style the game should use, supply the Hebrew way to write the Israeli pound, and the format for large amounts ("1.2m"). The spoken form already says "מיליון" (`hud.say_million`).

### HE-09 Mixed direction
Status: proposed, native, test in Unity. Reason: Hebrew runs right to left and embedded Latin text and numbers run left to right. The base direction must be set, or runs are misordered (r12a); W3C and Unicode advise isolating each opposite-direction run and any value of unknown direction (W3C; r12a recommends the isolate characters over embeddings). Unity's advanced text generator supports Hebrew and a direction setting, but its manual does not mention isolates, so they must be tested in the game.
- To test: "לא ניתן היה להפעיל את Statecraft: {0}. יש לטעון את הדף מחדש." (`boot.failed`), where a colon follows a Latin word and a placeholder follows it.
- To test: "לחיצה, Enter או רווח לדילוג · Esc מדלג על הכול" (`outcome.skip`), which is new since 14 October.
- Settled since 14 October: the recording credit keeps no Latin name. It is "הקלטה: ראלף הלינגר, ארכיון המדינה" (`start.recording_credit`), as the Israel State Archives spell the name, and the Hebrew UI test rejects Latin letters in any static Hebrew text, names included.
- Already in place: each text takes its direction from its own first letter (`InHebrewEachTextTakesItsDirectionFromItsOwnWords`), and the Hebrew test bans the explicit embedding and override characters U+202A to U+202E. Since #543 every value inserted into a Hebrew pattern is wrapped in left-to-right marks (U+200E), and `HebrewValuesTests` renders the lines to check that a minus sign and an English date draw correctly and that the marks draw nothing. Those tests cover the inserted-value cases described; they do not establish correctness for every mixed-direction string. A Latin word typed inside a Hebrew sentence is not covered by the tests reviewed.
- How to see it: open the game with `?lang=he`.
- To test: "ספיטפייר LF Mk IXe" (`unit.aircraft.spitfire`).
- To test: in the game, the lines that receive an English date or amount, to confirm the marks do what the rendered test describes (HE-08).
For the native reviewer: say how each of these should read.
For the maintainer: the #543 check found that the Hebrew face honours U+200E and draws nothing for it. Whether it honours the isolate characters is not tested.

### HE-10 Latin names and terms inside Hebrew
Status: proposed, native. The product name appears in Latin inside a Hebrew sentence ("Statecraft" in `boot.failed`, which is not a static text), and aircraft and place names mix scripts. Since #558 the recording credit uses a transliterated name. Decide per category whether a name is transliterated or kept in Latin, and record it in the glossary.
For the native reviewer: decide for the product name, aircraft names (the strings already transliterate "Avia" as "אוויה"), and the names of places and people. The credit line is settled: "הקלטה: ראלף הלינגר, ארכיון המדינה" (`start.recording_credit`).

### HE-11 Layout and directional words
Status: observed in English, proposed for Hebrew. The settings.tiltshift string describes blur at the top and bottom of the map. This is a description of the effect, not a direction to a control; review it as an exception. Google advises against directional words because they fail accessibility and localisation, and in the mirrored layout "the button on the right" would be wrong. r12a lists punctuation that should not start or end a line (closing quotes and brackets, . , ; ! ? ־ %) and says a currency sign stays with its number; this is a layout property for engineering to check.
Check: a pattern search over the strings files.

### HE-12 Content text (events, units, names)
Status: implemented localisation, native review pending (#564). In the Hebrew UI, EventCardPresenter uses the Hebrew title, body and option labels where supplied through ContentNames.Fill; each missing field falls back to English. An alternate-timeline variant without Hebrew keeps its English variant text. UnitCard uses a sourced Hebrew unit name and optional Hebrew place, commander and sector text where supplied, otherwise the English value for that field. Names of offices, parties, governments, legislatures and places have sourced Hebrew forms in the content since #558, and Unity reads Hebrew names of places, Cabinet refusal names (#985), and the people, office, party, government and legislature forms handled by ContentNames.Of (#861). Since 15 October a Hebrew display name that an agent has checked against its sources and no person has read carries its own flag beside it, `name_he_review`, `title_he_review`, `title_he_f_review` or `body_he_review: agent-checked`. The native reviewer removes the line once they have read the name (CONTENT_POLICY section 2, rule 6; #564). Localised content keeps the same rules: every historical claim keeps its sources, no quote is invented, and Hebrew names of people and places are checked against a source and not guessed. Sensitive events need review by a native reader and the existing sensitivity review, in addition to the historian review.
For the maintainer: the remaining localisation scope and review ownership; the Hebrew fields and per-field English fallbacks above are already implemented.

### HE-13 Tone
Status: proposed, native. The project voice is sober, direct and institutional. Hebrew should match: no slang, no drama, no filler. The government guide's principle of personal but gender-neutral wording (HE-03) applies. No Hebrew-specific tone source has been found beyond that.
For the native reviewer: say what register suits a sober political strategy game, and name two or three sentences that sound right.

### HE-14 Review and sign-off
Status: proposed review process. New Hebrew recommendations need native review; adopted maintainer rules remain in effect unless explicitly overruled. Keep a review log with the date, the reviewer's name, the keys or rules reviewed, and the changes made. A reviewer's change is recorded as a decision with its reason, so later edits do not undo it.

### HE-15 Agreement with a placeholder
Status: #1136 applied Beri's native review from #564 to the budget strings after the interim neutral wording in #1081. A changing placeholder must not receive a fixed verb or pronoun that fits only one possible name. The current strings are "{0} מצביעה על התקציב, אך ההצבעה אינה מחייבת: לקואליציה יש {1} מתוך {2} {3}" (`budget.vote_advisory`), "{0} אינה מצביעה על התקציב" (`budget.no_vote`) and "מתייעצת עם {0} · {1} חברים · ההצבעות אינן מחייבות" (`cabinet.legislature_advisory`). Beri's note explains why the budget's feminine forms work: all three current legislature names are feminine. A masculine-named body would need new wording. Keep the native-reviewed wording for the current bodies; it is not a universal solution for every future name. ContentNames.Of reads legislature body_he (#861). A new grammatical-gender field remains a separate design decision.
For the native reviewer: review any new body name against these sentences and mark any other string whose verb or pronoun assumes the gender of a changing placeholder.

## Candidate fixes for the Hebrew file (all unapproved)

| Key | Today | Candidate | Rule |
|---|---|---|---|
| `event.hide` | הסתר | הסתרה, or להסתיר | HE-03 |
| `info.localities` | יישובים ב-1948: {0} | יישובים ב־1948: {0} | HE-07 |
| `map.legend_held` | כתום-אדום (in the line) | כתום־אדום | HE-07 |
| `unit.national` | רמת ביטחון {1} מתוך 5 | the same word as `hud.level` (דרגה) | HE-02 |
| `info.biome.valley`, `info.biome.lowland` | בקע, עמק | reviewer to say whether the two are swapped | HE-02 |
| `start.loading` | טוען את ההקלטה… | reviewer to rule on the masculine present form | HE-03 |
| `boot.failed` | ...את Statecraft: {0}. ... | check the display; isolate the Latin name | HE-09 |

Addressed by #1081 and removed from this table: the three legislature vote strings (HE-15). Settled by the owner since 14 October and also removed: `card.competence` (now כישורים), `defence.section_ladder` (now המצב הביטחוני) and the separate unit for the Provisional State Council (`cabinet.seats_appointed`).

## Review checklist

Run in order. A "no" is a finding, cited by rule ID.

1. Does the key exist in English with the same placeholders (HE-01)?
2. Does it use the glossary's Hebrew word for each concept (HE-02)?
3. No masculine imperative or slash form; action noun, "יש ל...", ך or ת endings, or a neutral first person (HE-03)?
4. Counts as "Label: N" (HE-04)?
5. Full spelling (HE-05)?
6. Gershayim as ״, one style across the file (HE-06)?
7. Maqaf before a numeral or Latin letters (HE-07)?
8. No typed date or money; flag any string that receives English-formatted values (HE-08)?
9. Latin text and unknown-direction values isolated, and the display checked (HE-09, HE-10)?
10. No directional words (HE-11)?
11. No verb or pronoun after a placeholder in a form that fits only one gender (HE-15)?
12. A native reviewer named and dated (HE-14)?

## Open decisions

For the maintainer:
- Review any remaining English date callers and decide the localized money format (HE-08). The main date surfaces listed above are already localized.
- Whether isolates are honoured by the text generator, and where the test lives (HE-09). Per-text direction is already tested.
- Hebrew for content, and its review (HE-12, #55).
- A player setting for the form of address (HE-03).
- Whether the two-key plural is enough, or a general mechanism is wanted (HE-04).
- Gender agreement once the Hebrew content names are read by the UI (HE-15).

For the native Hebrew reviewer: every "native" item above, the candidate fixes, and the register (HE-13).

## Evidence and limits

- Read directly by the author: the Israeli National Digital Agency guide to government language (section 1.3.3, slides 52 to 57); r12a "Modern Israeli Hebrew" orthography notes (updated 27 April 2026, CC BY 4.0); the W3C "Hebrew Script Resources" index (Group Note Draft, 20 March 2026); the real strings and code named above.
- Re-opened and checked by the author on 14 October 2026: the Academy of the Hebrew Language punctuation and spelling pages, the W3C inline bidi article, the Unity manual page, Unicode CLDR 48 plural rules, and the Google and Apple pages used here.
- Not found: a Hebrew-specific source on UI tone and register, gendered numeral agreement in UI, and an authoritative Knesset source for the Council and Knesset terms. The Microsoft Hebrew guide was excluded by choice.
- This 17 October revision adds no newly verified external source. It reconciles cited strings and current date-formatting paths at upstream/main 46abd99c. Earlier external-source checks retain their original dates. Gender agreement and terrain terminology remain questions for the native reviewer.
- The government guide is written for public digital services. It is a strong reference for Hebrew UI practice, not a rule for a game.
