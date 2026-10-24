# Glossary (draft v0.5)

Source of truth: `UX_GLOSSARY.yaml`. This Markdown is its readable view; update the YAML first and keep both views in sync. DRAFT for review. Adopted decisions are identified individually; other recommendations await maintainer or native Hebrew review. Snapshot date 2026-10-17.

Entries: 6 contested, 8 decision-needed, 3 pending-gdd, 27 preferred, 6 use-with-caution (50 concepts, 6 conventions).

Revision 19 October 2026: targeted terminology and implementation checks against upstream/main 9e571342 address the #970 review. The original snapshot date remains above; this is not a full review of every new string.

Evidence limits: the original 46abd99c snapshot checker matched 289 cited forms; nine references were outside its supported inputs or used summary notation. Those totals do not include the new rows checked directly against 9e571342 in this revision. Provisional-state-council, truce, yishuv and unit still have no Hebrew candidate here. The new in-use pioneer terms are recorded without native approval.

Revision 21 October 2026: budget wording and map hints match upstream/main 001a59d0 (#1136, #1134). The native agreement note records the current bodies and its limit; other snapshot evidence retains its original date.

Revision 21 October 2026, localisation follow-up: the Cabinet reads government and legislature Hebrew names through ContentNames.Of (upstream/main e123971c), with English fallback for a missing name.

## At a glance

| Concept | Status | English | Hebrew | Decision owner |
|---|---|---|---|---|
| security-level | contested | security level | open | Eyal and the native reviewer |
| attention | decision-needed | Attention at the start of a label, attention inside a sentence (sentence case, as in the strings) | open | Eyal |
| turn | preferred | turn | תור | Eyal |
| movement-points | decision-needed | movement points | נקודות תנועה | Eyal and the native reviewer |
| end-turn | preferred | End turn (the button), 'end the turn' (the action) | סיום התור (the button); 'לסיים את התור' in sentences | Eyal |
| decision | preferred | decision | החלטה | Eyal, for the plural form |
| event | preferred | event (data and history), 'event card' (the card the player sees) | open | Eyal |
| why-panel | preferred | Why? (the button), 'Why did this happen?' (the screen title), the 'Why?' panel (prose) | למה? (the button), 'למה זה קרה?' (the screen title) | Eyal, for the alt text |
| coalition | preferred | coalition | קואליציה | Eyal |
| seat-and-member | preferred | seats (for counts), members (for people in a body) | מנדטים (an elected body), חברים (the appointed Provisional State Council) | Closed by Eyal (#550); native reviewer to confirm |
| provisional-state-council | decision-needed | Provisional State Council | open | Native reviewer; Eyal for #55 |
| competence | preferred | competence | כישורים | Closed by Eyal (#550); native reviewer to confirm |
| ceasefire | decision-needed | ceasefire | הפסקת אש | Eyal (sourcing) |
| truce | decision-needed | truce (in the event names) | open | Eyal (sourcing) and the native reviewer |
| armistice | preferred | armistice | שביתת נשק | Eyal |
| reserve-call-up | preferred | reserve call-up | גיוס מילואים | Eyal |
| transit-camp | preferred | transit camp; 'ma'abarot' in prose, with the English at first mention | מעברה | Eyal |
| settlement | contested | settlement | open | Native reviewer |
| locality | contested | locality | open | Native reviewer |
| yishuv | decision-needed | open | open | Eyal |
| hex | use-with-caution | hex, defined at first use | open | Eyal |
| unit | pending-gdd | open | open | Eyal |
| draft-marker | preferred | Draft | טיוטה | Eyal |
| ai-generated | preferred | AI-generated [illustration \| concept art] | בינה מלאכותית | Eyal |
| defence-screen-and-office | preferred | 'Defence' for the game's own screen, button and prose; the official office and ministry titles as Israel writes them ('Minister of Defense'), as content/offices.yaml records | שר הביטחון (minister), ביטחון (the screen and button); the ministry's name for the reviewer to confirm | Eyal for where the spelling rule is documented; gershayim adopted in #630, subject to native-reviewer overrule in #564 |
| social-cohesion | preferred | Social cohesion | לכידות חברתית | Eyal |
| international-support | preferred | International support | תמיכה בינלאומית | Eyal and the native reviewer |
| pioneer | decision-needed | halutz (plural halutzim), the current UI wording | open | Eyal |
| hakhshara | decision-needed | Hakhshara, the current UI wording | הכשרה | Eyal |
| resources-and-meters | pending-gdd | open | open | Eyal |
| treasury-and-money | preferred | Money; Money (net) for the top-bar net position | כסף, for the GDD resource (the owner's word) | Eyal; native reviewer for the Hebrew |
| foreign-currency | preferred | foreign currency | מטבע חוץ | Eyal |
| reserves | use-with-caution | reserves, for the military reserve only | מילואים, for the military reserve | Eyal |
| budget | preferred | budget | תקציב | Eyal; native reviewer for the spelling |
| legislature-vote | preferred | 'must approve', 'votes on it, but its vote does not bind', 'does not vote' | open | Native reviewer; Eyal for any key change |
| stability | preferred | stability | יציבות | Eyal; native reviewer for the Hebrew |
| majority-and-opposition | use-with-caution | majority; opposition | open | Eyal; native reviewer |
| cabinet-and-government | contested | Cabinet for the screen and the group; the government's name for the period | open | Native reviewer |
| office-ministry-minister | preferred | office for the post, ministry for the department, 'Minister of ...' for the title as Israel writes it | open | Eyal |
| chief-of-staff | use-with-caution | 'Chief of Staff' in running UI text; the full title where the office itself is named | הרמטכ״ל | Eyal |
| public-support | preferred | Public support | open | Eyal |
| order | contested | order | open | Eyal; native reviewer |
| era-and-chapter | contested | open | open | Eyal |
| policy | preferred | policy | מדיניות | Eyal |
| effects-and-situations | use-with-caution | 'Ongoing effects' and 'Situations', as in the Why panel | open | Eyal; native reviewer |
| aliyah-and-immigration | preferred | immigration | עלייה | Eyal |
| mandate-sub-district | preferred | Mandate sub-districts | נפות המנדט | Eyal |
| terrain | use-with-caution | terrain | open | Native reviewer |
| front-line-and-control | preferred | the labels the strings use: 'Held by Israel', 'Held by Arab forces', 'Contested', "No-man's land", 'Israeli side', 'Arab side', 'Demilitarized zone' | open | Eyal; historian review for new labels |
| city | pending-gdd | city | open | Eyal |

## Conventions

### spelling (preferred)

The rule exists but only as a comment in content/offices.yaml: office titles keep Israel's official English ('Ministry of Defense', 'Minister of Defense') because they are proper nouns, and the UI is otherwise British English, so the screen and its button say 'Defence'. The comment says not to 'fix' it (playtest 1, item 8). No document in docs/ or CONTRIBUTING.md states it, no reference dictionary is named, and docs/GDD.md mixes both spellings ('defence' in the soldier actions table, 'defense' under settlement control).

Recommendation (see status): Move the rule from the offices.yaml comment into docs/WRITING_GUIDE.md so it covers every surface, name one reference dictionary for the British spelling, and say which spelling the GDD and docs follow. Pattern from the Google developer documentation style guide and the GOV.UK style guide.

Decision owner: Eyal: where the rule is documented and the dictionary; the rule itself is already his

### transliteration (decision-needed)

Hebrew terms in English text are transliterated in several ways: ma'abarot (README), Halutz and Hakhshara (GDD), yishuv and yishuvim (GDD), Knesset. No scheme is written down.

Recommendation (see status): Pick one scheme, list each term once in this glossary, define each at first use, and keep the Hebrew spelling beside it in the glossary.

Decision owner: Eyal

### button-names (preferred)

A named button is written in running text as the button shows it, in sentence case since #630 ('takes effect at End turn', 'Press New game again'), while the action described in general is lowercase ('end the turn to see its causes'). This follows the Google rule that a UI label is written as it appears.

Recommendation (see status): Keep it as the rule. New buttons follow it.

Decision owner: Eyal

### counts (preferred)

Counts are written 'Label: N' where the sentence would otherwise change with N. The Hebrew 'event.more' already does this; the English still says 'decision(s)'.

Recommendation (see status): Adopt now for both languages; a real plural mechanism is a separate proposal.

Decision owner: Eyal

### dates (preferred)

English dates are written '15 May 1948' (CONTRIBUTING rule 4). Since #550 the strings files hold the twelve month names and two patterns (date.day_month_year, date.month_year), with Hebrew versions ('{0} ב{1} {2}', so the prefix ב joins the month name with no hyphen). The map's control captions use them ('9 July 1948' and '9 ביולי 1948'). The map, top bar, Budget, Why and info panel dates now use the language's date keys (#687, #701). PlayerText.Date itself remains English, so other callers still need checking. Since #543 the UI puts invisible left-to-right marks round every value it inserts into a Hebrew line, so an English date or amount reads as one run and a minus sign stays on the correct side; the text itself is still English.

Recommendation (see status): Preserve the adopted localized date paths; check any remaining caller individually. Money localization remains open.

Decision owner: Eyal

### label-capitalisation (preferred)

Settled by the maintainer in #630 (merged 15 October 2026): every static label is sentence case, button names included ('New game', 'End turn', 'Public support', 'International support', 'Foreign currency'). The top-bar captions are the content display names as written (the test is HudTests.TopBarCaptionsAreTheContentNames). 'DRAFT' on a decision card stays in capitals. The Hebrew has no capitals, so the rule is for English only.

Recommendation (see status): Keep it as the rule. A new label, caption or button follows it, and so does text that names one.

Decision owner: Eyal

## Concepts

### security-level (contested)

Category: game-system. Scope: ui, docs, marketing, code.

**Definition.** The five-step scale of the country's security posture, from 1 (routine threat) to 5 (full war), that the player orders up or down on the Defence screen. At level 5 a countdown to a UN-imposed ceasefire runs.

**English.** Preferred: security level.
Avoid: rung (in player text and prose; it stays in code and content/rules.yaml).

| In use | Where |
|---|---|
| Level {0} of 5 | strings.en.yaml hud.level |
| Security level | strings.en.yaml defence.section_ladder |
| Lower the level | strings.en.yaml defence.lower |
| Raise the level | strings.en.yaml defence.raise |
| security level {1} of 5 | strings.en.yaml unit.national |
| level {0} to {1} | strings.en.yaml defence.order |
| five-level escalation ladder | README.md |
| start_rung | content/rules.yaml |

**Hebrew.** Preferred: open.
Options: דרגה; רמת ביטחון; רמת כוננות (Eyal's suggestion on #193, if the ladder is read as an alert level).

| In use | Where |
|---|---|
| דרגה {0} מתוך 5 | strings.he.yaml hud.level |
| המצב הביטחוני | strings.he.yaml defence.section_ladder |
| הורדת הדרגה | strings.he.yaml defence.lower |
| העלאת הדרגה | strings.he.yaml defence.raise |
| רמת ביטחון {1} מתוך 5 | strings.he.yaml unit.national |
| מדרגה {0} לדרגה {1} | strings.he.yaml defence.order |
| סולם הסלמה | README.he.md |

**Recommendation (see status).**
- English: Say 'security level' in full where the player first meets it (the Defence screen caption and the HUD), then 'level' alone in the same view. Keep 'escalation ladder' for prose that describes the system, as the README does. Never 'rung' in player text.
- Hebrew: Choose one Hebrew word for 'level' and use it in every key. Which word is for the native reviewer; the options are listed. Since 14 October the section caption reads 'המצב הביטחוני' (the security situation) while the English caption still reads 'Security level', so the two languages no longer name the same thing there; confirm that this is intended.
- Reasons: (1) Two of the three English UI forms already say 'security level'. (2) 'Level 5 of 5' on its own does not say what the level measures. (3) 'Rung' appears only in code and rules. (4) The Hebrew uses 'דרגה' (hud.level, defence.lower and defence.raise), 'רמת ביטחון' (unit.national) and 'המצב הביטחוני' (the caption) for one scale, and the README adds 'סולם הסלמה'.

**Decision owner.** Eyal and the native reviewer.

**Pending GDD.** The GDD plan (section 5, D-C4) proposes that the level become derived from the war state. The wording may change with it.

### attention (decision-needed)

Category: game-system. Scope: ui, docs, code.

**Definition.** The per-turn allowance that limits how many orders the player can give. Each kind of order costs some Attention, and an order is refused when there is not enough left.

**English.** Preferred: Attention at the start of a label, attention inside a sentence (sentence case, as in the strings).

| In use | Where |
|---|---|
| Attention {0:0.0} of {1:0.0} | strings.en.yaml hud.attention |
| Not enough attention (need {0:0.0}, have {1:0.0}) | strings.en.yaml order.no_attention |
| 4 Attention per turn | issue #42 body |
| attention_per_turn: 4 | content/rules.yaml |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| תשומת לב {0:0.0} מתוך {1:0.0} | strings.he.yaml hud.attention |
| אין מספיק תשומת לב | strings.he.yaml order.no_attention |

**Recommendation (see status).**
- English: Keep sentence case everywhere, in prose as well as in the UI, so one rule covers both, and define the term once at first use in each document. The alternative, a capital for the named resource in prose only, would split the rule between UI and docs.
- Hebrew: No Hebrew change proposed. A native check of 'תשומת לב' as the name of a resource is advisable.
- Reasons: (1) No document gives a player-facing definition. (2) One rule for UI and docs is simpler to apply and to check, and the strings already follow sentence case.

**Decision owner.** Eyal.

**Pending GDD.** Kept by the owner (decision D-C5, 12 October). Its role in the GDD core loop is listed as open in docs/GDD.md.

### turn (preferred)

Category: game-system. Scope: ui, docs, marketing, code.

**Definition.** One step of the game. The current MVP uses monthly turns on every security rung, from 15 May 1948 to 15 January 1952 (docs/GDD.md and content/rules.yaml). Legacy turn-length strings remain in the files; their presence does not describe the current MVP cadence.

**English.** Preferred: turn.

| In use | Where |
|---|---|
| Turn {0} | strings.en.yaml hud.turn |
| turns of a quarter / month / week | strings.en.yaml defence.turn_quarter, defence.turn_month, defence.turn_week |
| Each turn advances history by one month | docs/GDD.md |
| turn_lengths: [month, month, month, month, month] | content/rules.yaml |

**Hebrew.** Preferred: תור.

| In use | Where |
|---|---|
| תור {0} | strings.he.yaml hud.turn |
| תורות של רבעון / חודש / שבוע | strings.he.yaml defence.turn_* |
| {0} תורות | strings.he.yaml city.turns (#869) |

**Recommendation (see status).**
- English: Use turn. When duration matters, say one month for the current MVP. Do not infer a quarter or week from unused legacy strings.
- Hebrew: The plural of תור is תורות everywhere (city.turns, defence.turn_*; owner, 24 October 2026: "plural of תור is תורות").
- Reasons: (1) The current rules set all five turn lengths to month.

**Decision owner.** Eyal.

### movement-points (decision-needed)

Category: game-system. Scope: ui, docs, code.

**Definition.** The number of points a military unit can spend entering hexes during one turn.

**English.** Preferred: movement points.

| In use | Where |
|---|---|
| Movement points: {0} of {1} | strings.en.yaml unit.points |

**Hebrew.** Preferred: נקודות תנועה.

| In use | Where |
|---|---|
| נקודות תנועה: {0} מתוך {1} | strings.he.yaml unit.points |

**Recommendation (see status).**
- English: Use movement points in the card and rules.
- Hebrew: The candidate נקודות תנועה needs a native review.
- Reasons: (1) The term is new on screen with Civ-style movement.

**Decision owner.** Eyal and the native reviewer.

### end-turn (preferred)

Category: game-system. Scope: ui, docs.

**Definition.** The button that submits the queued orders and runs the turn.

**English.** Preferred: End turn (the button), 'end the turn' (the action).

| In use | Where |
|---|---|
| End turn | strings.en.yaml hud.end_turn |
| takes effect at End turn | strings.en.yaml budget.queued |
| end the turn to see its causes | strings.en.yaml why.empty |

**Hebrew.** Preferred: סיום התור (the button); 'לסיים את התור' in sentences.

| In use | Where |
|---|---|
| סיום התור | strings.he.yaml hud.end_turn |
| ייכנס לתוקף בסיום התור | strings.he.yaml budget.queued |
| יש לסיים את התור כדי לראות את הסיבות | strings.he.yaml why.empty |

**Recommendation (see status).**
- English: Keep the pattern: capitalised for the button, lowercase for the action. Record it as the rule (conventions: button-names).
- Hebrew: The button label 'סיום התור' (added in #550) and the sentences already use the same words. The reviewer confirms.
- Reasons: (1) Matches the Google rule that a UI label is written as it appears.

**Decision owner.** Eyal.

### decision (preferred)

Category: game-system. Scope: ui, docs, marketing.

**Definition.** A card that asks the player to choose between options at a historical moment.

**English.** Preferred: decision.

| In use | Where |
|---|---|
| Decision waiting | strings.en.yaml event.reopen |
| {0} more decision(s) waiting | strings.en.yaml event.more |
| face real decisions at their real dates | README.md |

**Hebrew.** Preferred: החלטה.

| In use | Where |
|---|---|
| החלטה ממתינה | strings.he.yaml event.reopen |
| החלטות ממתינות נוספות: {0} | strings.he.yaml event.more |
| מכריעים בהחלטות אמיתיות | README.he.md |

**Recommendation (see status).**
- English: Say 'decision' when the player must choose, and 'event' for what happens in history (see 'event'). Replace 'decision(s)' with the Hebrew shape: 'More decisions waiting: {0}'.
- Hebrew: No change.
- Reasons: (1) Google advises against '(s)'. (2) The Hebrew string already uses the 'Label: N' shape and needs no plural form.

**Decision owner.** Eyal, for the plural form.

### event (preferred)

Category: game-system. Scope: docs, events, code.

**Definition.** Something that happens in the game's history, written up as a content record with a date window, options and sources.

**English.** Preferred: event (data and history), 'event card' (the card the player sees).

| In use | Where |
|---|---|
| Events | strings.en.yaml why.events |
| event card | docs/WRITING_GUIDE.md (event card usage) |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| אירועים | strings.he.yaml why.events |

**Recommendation (see status).**
- English: Use 'event' for the historical happening and the data, 'decision' for what the player answers.
- Hebrew: No change proposed.
- Reasons: (1) Keeps the two words from being used for each other.

**Decision owner.** Eyal.

### why-panel (preferred)

Category: game-system. Scope: ui, docs, marketing.

**Definition.** The screen that explains what caused the changes in the last turn.

**English.** Preferred: Why? (the button), 'Why did this happen?' (the screen title), the 'Why?' panel (prose).
Avoid: 'Why panel' without the question mark.

| In use | Where |
|---|---|
| Why? | strings.en.yaml hud.nav.why |
| Why did this happen? | strings.en.yaml why.title |
| "Why?" panel | README.md |
| Why panel | README.md image alt text |

**Hebrew.** Preferred: למה? (the button), 'למה זה קרה?' (the screen title).

| In use | Where |
|---|---|
| למה? | strings.he.yaml hud.nav.why |
| למה זה קרה? | strings.he.yaml why.title |
| לוח 'למה זה קרה?' | README.he.md |

**Recommendation (see status).**
- English: Keep the names the strings now give. In prose, write the 'Why?' panel, with the question mark, as most of the README does; the README's image alt text still says 'Why panel'.
- Hebrew: No change proposed; the button and the title were added in #550.
- Reasons: (1) The English and Hebrew button and title are parallel (a short form on the button, the full question as the title). (2) The question mark matches what the screen does, which is answer a question.

**Decision owner.** Eyal, for the alt text.

### coalition (preferred)

Category: politics. Scope: ui, docs, marketing.

**Definition.** The parties that support the government, counted in seats.

**English.** Preferred: coalition.

| In use | Where |
|---|---|
| Coalition: {0} of {1} {3} · stability {2} | strings.en.yaml cabinet.coalition |
| inside a real coalition | README.md |

**Hebrew.** Preferred: קואליציה.

| In use | Where |
|---|---|
| קואליציה: {0} מתוך {1} {3} · יציבות {2} | strings.he.yaml cabinet.coalition |
| בתוך קואליציה אמיתית | README.he.md |

**Recommendation (see status).**
- English: No change to the word.
- Hebrew: No change proposed. The unit after the number is now chosen by the body (see 'seat-and-member').
- Reasons: (1) Consistent in both languages.

**Decision owner.** Eyal.

**Notes.** The same string is shown while the Provisional State Council sits, which is an appointed body (Eyal on #209), so 'coalition' there needs its own look.

### seat-and-member (preferred)

Category: politics. Scope: ui, docs.

**Definition.** A seat is one place in a legislature, used when counting a party or coalition. A member is a person who holds one. In Hebrew the unit of a count depends on the body: seats of an elected body, members of the appointed one.

**English.** Preferred: seats (for counts), members (for people in a body).

| In use | Where |
|---|---|
| seats | strings.en.yaml cabinet.seats |
| seats | strings.en.yaml cabinet.seats_appointed |
| {1} members | strings.en.yaml cabinet.legislature |
| members | strings.en.yaml cabinet.members_unit |

**Hebrew.** Preferred: מנדטים (an elected body), חברים (the appointed Provisional State Council).

| In use | Where |
|---|---|
| מנדטים | strings.he.yaml cabinet.seats |
| חברים | strings.he.yaml cabinet.seats_appointed |
| חברים | strings.he.yaml cabinet.legislature, cabinet.members_unit |

**Recommendation (see status).**
- English: No change; 'seats' and 'members' already work for both bodies.
- Hebrew: Closed in #550: the unit is its own string ('cabinet.seats' or 'cabinet.seats_appointed'), chosen by whether the legislature has an election date in content/legislatures.yaml (CabinetPresenter.SeatUnit), and no template hard-codes either word. The native reviewer can still confirm the two words, and a Knesset source for them is still missing.
- Reasons: (1) English needs no split, Hebrew does. (2) A new body is handled by its election date, so no string changes.

**Decision owner.** Closed by Eyal (#550); native reviewer to confirm.

### provisional-state-council (decision-needed)

Category: politics. Scope: ui, docs, events.

**Definition.** The appointed body the Cabinet screen shows from 14 May 1948 to 14 February 1949 (the dates Eyal gives on #209). The screen draws it as a half circle of seats.

**English.** Preferred: Provisional State Council.

| In use | Where |
|---|---|
| the Provisional State Council as a half circle of seats | README.md image alt text |
| body: Provisional State Council | content/legislatures.yaml |
| body: Constituent Assembly (First Knesset) | content/legislatures.yaml |

**Hebrew.** Preferred: open.

**Recommendation (see status).**
- English: No change.
- Hebrew: Use the sourced name carried in content (#558). The Cabinet now reads legislature BodyHe through ContentNames.Of, showing the Hebrew name in the Hebrew UI where supplied and English otherwise. Keep any outstanding native review on #564 separate from that implementation; displaying a name does not establish its approval.
- Reasons: (1) CabinetPresenter uses ContentNames.Of for the legislature name; it is no longer an unread content field.

**Decision owner.** Native reviewer; Eyal for #55.

### competence (preferred)

Category: game-system. Scope: ui, docs.

**Definition.** A rating shown on every minister's card for how well they run their office, next to loyalty. Under the GDD plan it would scale the matching system for that office (D-H3, open).

**English.** Preferred: competence.

| In use | Where |
|---|---|
| Competence | strings.en.yaml card.competence |
| competence {2}, loyalty {3} | strings.en.yaml cabinet.person |

**Hebrew.** Preferred: כישורים.
Avoid: כשירות (replaced in #550; reads as fitness or qualification, Eyal on #193).
Options: יכולת (the other candidate Eyal named).

| In use | Where |
|---|---|
| כישורים | strings.he.yaml card.competence |
| כישורים {2}, נאמנות {3} | strings.he.yaml cabinet.person |
| נאמנות | strings.he.yaml card.loyalty |

**Recommendation (see status).**
- English: No change.
- Hebrew: Closed in #550: 'כישורים'. The native reviewer can still confirm it against 'יכולת'.
- Reasons: (1) Eyal's own note on #193, applied in #550.

**Decision owner.** Closed by Eyal (#550); native reviewer to confirm.

**Pending GDD.** Minister roles are GDD Phase 7 (D-H3).

### ceasefire (decision-needed)

Category: history-term. Scope: ui, events, docs.

**Definition.** In the game rules: the stop to full war that the UN imposes after a countdown at security level 5. In event titles: the name of a specific cessation of fighting (for example in Jerusalem).

**English.** Preferred: ceasefire.
Avoid: cease-fire (hyphenated) in new text.

| In use | Where |
|---|---|
| the UN imposes a ceasefire in {0} weeks | strings.en.yaml defence.ceasefire |
| Cease-fire in Jerusalem | content/events/jerusalem_ceasefire.yaml title |

**Hebrew.** Preferred: הפסקת אש.

| In use | Where |
|---|---|
| הפסקת אש של האו״ם | strings.he.yaml defence.ceasefire_starts |

**Recommendation (see status).**
- English: Use 'ceasefire' as one word in UI and prose. An event title that follows a source's own spelling keeps it, so decide whether 'Cease-fire in Jerusalem' follows a source or should be aligned.
- Hebrew: No change proposed.
- Reasons: (1) The strings and the event title disagree on the hyphen.

**Decision owner.** Eyal (sourcing).

### truce (decision-needed)

Category: history-term. Scope: events, docs, marketing.

**Definition.** The word the event titles use for two pauses in the 1948 fighting ('The First Truce', 'The Second Truce'). Not the same word as ceasefire or armistice.

**English.** Preferred: truce (in the event names).
Avoid: using it for ceasefire or armistice.

| In use | Where |
|---|---|
| The First Truce | content/events/first_truce.yaml title |
| The Second Truce | content/events/second_truce.yaml title |
| the First Truce | README.md |

**Hebrew.** Preferred: open.

**Recommendation (see status).**
- English: Keep 'truce', 'ceasefire' and 'armistice' as three different words.
- Hebrew: No Hebrew term for 'truce' appears in the strings. The reviewer supplies it.
- Reasons: (1) The repo uses each word for a different thing.

**Decision owner.** Eyal (sourcing) and the native reviewer.

### armistice (preferred)

Category: history-term. Scope: ui, events, docs.

**Definition.** The 1949 agreements and the lines they set. Used in the event titles for four countries, and in map text.

**English.** Preferred: armistice.

| In use | Where |
|---|---|
| Armistice with Egypt / Jordan / Lebanon / Syria | content/events/armistice_*.yaml title |
| On the {0} · {1}, {2} | strings.en.yaml info.armistice_line |

**Hebrew.** Preferred: שביתת נשק.

| In use | Where |
|---|---|
| קו שביתת נשק | strings.he.yaml map.layer_front_no_data |

**Recommendation (see status).**
- English: No change.
- Hebrew: No change proposed.
- Reasons: (1) Consistent in both languages.

**Decision owner.** Eyal.

### reserve-call-up (preferred)

Category: game-system. Scope: ui, docs.

**Definition.** The share of reserves the player calls up, which costs pay and lost work and has a minimum for each security level.

**English.** Preferred: reserve call-up.

| In use | Where |
|---|---|
| Reserve call-up | strings.en.yaml defence.section_callup, unit.national |
| {0} of reserves called up | strings.en.yaml defence.mob |

**Hebrew.** Preferred: גיוס מילואים.

| In use | Where |
|---|---|
| גיוס מילואים | strings.he.yaml defence.section_callup, unit.national |

**Recommendation (see status).**
- English: No change.
- Hebrew: No change proposed.
- Reasons: (1) Consistent in both languages.

**Decision owner.** Eyal.

**Pending GDD.** The GDD plan proposes replacing the call-up with recruits once Phase 5 lands (D-C4).

### transit-camp (preferred)

Category: place. Scope: ui, docs, marketing.

**Definition.** A camp for new immigrants. The player can place one on the map.

**English.** Preferred: transit camp; 'ma'abarot' in prose, with the English at first mention.

| In use | Where |
|---|---|
| Place a transit camp ({0}) | strings.en.yaml info.build_camp |
| the ma'abarot transit camps | README.md, site press page |

**Hebrew.** Preferred: מעברה.

| In use | Where |
|---|---|
| הקמת מעברה ({0}) | strings.he.yaml info.build_camp |

**Recommendation (see status).**
- English: UI says 'transit camp'. Prose says 'the ma'abarot (transit camps)' the first time, then either word. Decide the transliteration once (conventions: transliteration).
- Hebrew: No change proposed.
- Reasons: (1) That is what the README already does.

**Decision owner.** Eyal.

### settlement (contested)

Category: place. Scope: ui, docs.

**Definition.** A place the player owns on the map and builds in.

**English.** Preferred: settlement.

| In use | Where |
|---|---|
| Settlements: {0} | strings.en.yaml info.owned |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| יישובים: {0} | strings.he.yaml info.owned |

**Recommendation (see status).**
- English: No change.
- Hebrew: Check with the reviewer: the Hebrew uses the same word for 'settlement' and for '1948 locality' (see 'locality').
- Reasons: (1) English separates two concepts that Hebrew merges.

**Decision owner.** Native reviewer.

### locality (contested)

Category: place. Scope: ui, docs.

**Definition.** A town or village that existed in 1948 and appears on the map with its 1948 name.

**English.** Preferred: locality.

| In use | Where |
|---|---|
| 1948 localities: {0} | strings.en.yaml info.localities |
| Mixed locality | strings.en.yaml map.mixed |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| יישובים ב-1948: {0} | strings.he.yaml info.localities |
| יישוב מעורב | strings.he.yaml map.mixed |

**Recommendation (see status).**
- English: No change.
- Hebrew: Same point as 'settlement': one Hebrew word covers two English concepts. Also see the maqaf point on 'ב-1948' (Academy of the Hebrew Language punctuation rules).
- Reasons: (1) The Hebrew strings use 'יישוב' for both.

**Decision owner.** Native reviewer.

### yishuv (decision-needed)

Category: gdd-term. Scope: docs, ui, marketing.

**Definition.** Two senses are in use. In the GDD, 'yishuv' is a new settlement the player founds at a designated site. In the repo's era design (issues #81 to #98), 'the Yishuv' is the pre-state community and the era 1882 to 1948.

**English.** Preferred: open.

| In use | Where |
|---|---|
| Build yishuv | docs/GDD.md |
| establish new yishuvim | docs/GDD.md |
| Yishuv 1 ... Yishuv 19 (era work) | issue titles #81 to #98 |
| A designated site for a new yishuv | strings.en.yaml site.available (#869) |
| Build a yishuv with {0}: {1}, {2} | strings.en.yaml site.build (#869) |

**Hebrew.** Preferred: open.

**Recommendation (see status).**
- English: The GDD (v0.2) and the UI (#869: site.available, site.build) call a place the player founds a 'yishuv' (plural 'yishuvim'). 'The Yishuv', with a capital Y, is the pre-state community (docs/CONTENT_POLICY.md, section 1). Keep the capital to tell the two apart. Whether the UI should say 'settlement' instead is the owner's call.
- Hebrew: The reviewer decides how 'יישוב' is used for each sense.
- Reasons: (1) The design of record and the implemented UI agree on the lowercase term. (2) Capitalization distinguishes the place from the pre-state community.

**Decision owner.** Eyal.

### hex (use-with-caution)

Category: game-system. Scope: ui, docs.

**Definition.** One cell of the map grid. The player can click it to inspect it.

**English.** Preferred: hex, defined at first use.

| In use | Where |
|---|---|
| Click the map, or Tab to it and press Enter, to inspect a hex; right-drag to move the map | strings.en.yaml info.hint |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| יש ללחוץ על המפה, או לעבור אליה ב־Tab וללחוץ Enter, כדי לבדוק משושה; גרירה בלחצן הימני מזיזה את המפה | strings.he.yaml info.hint |

**Recommendation (see status).**
- English: Keep, but write the first mention so a player who has never met a hex map understands it.
- Hebrew: No change proposed.
- Reasons: (1) The Google guide treats unexplained jargon as a risk; the word is only used on the map hint.

**Decision owner.** Eyal.

### unit (pending-gdd)

Category: gdd-term. Scope: ui, docs, marketing, code.

**Definition.** Currently a token on the map for a brigade, squadron or ship. In the GDD it also names population roles (refugee, civilian, craftsman, pioneer, recruit and others).

**English.** Preferred: open.

| In use | Where |
|---|---|
| unit.* (service, position, formed, commander) | strings.en.yaml |
| brigade tokens | README.md |
| Population and units | docs/GDD.md (section heading) |

**Hebrew.** Preferred: open.

**Recommendation (see status).**
- English: Wait for the owner's formation-scale decision (D-A1) before fixing this entry.
- Hebrew: Nothing proposed.
- Reasons: (1) One word currently means two things (map token and person role).

**Decision owner.** Eyal.

**Pending GDD.** D-A1 and the Arab forces as units (#227 and the units doc).

### draft-marker (preferred)

Category: content-label. Scope: ui, docs.

**Definition.** The label shown on a unit card when its facts have not been checked by a historian.

**English.** Preferred: Draft.

| In use | Where |
|---|---|
| Draft: not yet checked by a historian | strings.en.yaml unit.draft |
| Draft: from Wikipedia only, not yet checked against a published history | strings.en.yaml unit.draft_wikipedia |
| DRAFT | strings.en.yaml event.draft |

**Hebrew.** Preferred: טיוטה.

| In use | Where |
|---|---|
| טיוטה: טרם נבדק בידי היסטוריון | strings.he.yaml unit.draft |
| טיוטה | strings.he.yaml event.draft |

**Recommendation (see status).**
- English: No change to the word. Two writings are live: 'Draft:' with a reason on a unit card, and 'DRAFT' in capitals as the mark on a decision card (the Hebrew uses one writing for both). Decide whether the capitals are a deliberate style for a mark.
- Hebrew: No change proposed.
- Reasons: (1) Honest, specific, and matches the project's sourcing rules. (2) One term in two typographic forms is a small drift that a rule can settle.

**Decision owner.** Eyal.

### ai-generated (preferred)

Category: content-label. Scope: ui, marketing, docs.

**Definition.** The label for any image, video or sound made with AI tools.

**English.** Preferred: AI-generated [illustration | concept art].
Avoid: presenting concept art or the trailer as gameplay.

| In use | Where |
|---|---|
| AI-generated illustration | strings.en.yaml event.ai_label |
| AI-generated concept art | README.md |
| made with AI video tools ... it is not in-game footage | README.md, site press page |
| a short, silent AI-generated loop, and a few historical orders show a short AI-generated clip at End turn, both labelled as such | README.md |
| no caption is burned into the video itself | docs/steam/store-page-draft.md |
| with no burned-in caption (owner ruling of 2026-10-14: no need to say they were created by AI) | docs/ai-disclosure.md |

**Hebrew.** Preferred: בינה מלאכותית.

| In use | Where |
|---|---|
| איור שנוצר בבינה מלאכותית | strings.he.yaml event.ai_label |

**Recommendation (see status).**
- English: Keep the label wording. The statements now agree: on 14 October the owner removed the caption burned into the clips, and the store-page draft says so while saying the game's UI labels each clip 'AI-generated illustration' while it plays, which is the string `event.ai_label`. The README says the loops and End turn clips are 'labelled as such', which the UI label satisfies. The 'not gameplay' wording stays beside any concept art or trailer.
- Hebrew: No change proposed. The Hebrew README makes the same 'labelled' statement and the Hebrew label exists.
- Reasons: (1) The burned-in caption and the in-game label are two different things, and the documents now separate them. (2) The ruling is quoted in docs/ai-disclosure.md.

**Decision owner.** Eyal.

### defence-screen-and-office (preferred)

Category: politics. Scope: ui, docs, events, code.

**Definition.** The security screen, and the minister and ministry in charge of defence.

**English.** Preferred: 'Defence' for the game's own screen, button and prose; the official office and ministry titles as Israel writes them ('Minister of Defense'), as content/offices.yaml records.

| In use | Where |
|---|---|
| Defence | strings.en.yaml hud.nav.defence |
| Open the Defence screen | strings.en.yaml unit.defence |
| Minister of Defense {0} · Chief of Staff {1} | strings.en.yaml defence.command |
| Spelling is deliberate, do not "fix" it (playtest 1, item 8) | content/offices.yaml comment |
| id: defense_minister, title: Minister of Defense | content/offices.yaml |
| the screen and its button say "Defence". | content/offices.yaml comment |
| {defense_minister} | content/events/altalena.yaml (placeholder) |
| for defence | docs/GDD.md (soldier actions table) |
| defense responsibilities | docs/GDD.md (settlement control) |

**Hebrew.** Preferred: שר הביטחון (minister), ביטחון (the screen and button); the ministry's name for the reviewer to confirm.

| In use | Where |
|---|---|
| ביטחון | strings.he.yaml hud.nav.defence |
| שר הביטחון {0} · הרמטכ״ל {1} | strings.he.yaml defence.command |

**Recommendation (see status).**
- English: The rule is already the owner's and the strings follow it. Write it down outside a YAML comment (conventions: spelling), and decide what the GDD, which mixes both spellings, follows. A placeholder such as {defense_minister} is an office id and a code question, not player text.
- Hebrew: Gershayim ״ is adopted by the maintainer in #630 for strings and content. A native reviewer may overrule it through #564; adoption does not wait for that review.
- Reasons: (1) The strings and offices.yaml agree; only the GDD and the placeholder differ. (2) The comment says the title is a proper noun, so it keeps its official spelling.

**Decision owner.** Eyal for where the spelling rule is documented; gershayim adopted in #630, subject to native-reviewer overrule in #564.

### social-cohesion (preferred)

Category: gdd-term. Scope: docs, ui, events.

**Definition.** The national indicator morale, displayed as Social cohesion on its zero-to-one simulation scale and the UI percentage scale. HudModel exposes its meter in the GDD loop. It is distinct from Public support, which is approval.

**English.** Preferred: Social cohesion.

| In use | Where |
|---|---|
| Social cohesion | docs/GDD.md |
| Social cohesion is the `morale` indicator | docs/gdd/cutover-resource-layer.md |

**Hebrew.** Preferred: לכידות חברתית.

| In use | Where |
|---|---|
| לכידות חברתית | docs/GDD.md (given in the table) |

**Recommendation (see status).**
- English: Use 'Social cohesion', as the owner's decision text writes it. The capitalisation question is the label rule (conventions: label-capitalisation): the GDD itself capitalises 'International Support' but not 'Social cohesion'.
- Hebrew: As the owner wrote it.
- Reasons: (1) The owner has named it and tied it to an indicator. (2) It is a different measure from Public support, which the same decision names as approval.

**Decision owner.** Eyal.

**Notes.** The earlier question, whether the top bar's Public support is the GDD's social cohesion, is answered by the cutover page: approval appears as Public support, and Social cohesion is the morale indicator. They are two measures.

### international-support (preferred)

Category: game-system. Scope: docs, ui, events, code.

**Definition.** The national indicator intl_standing, displayed as International support. The English and Hebrew captions were aligned in #683; the name is decided.

**English.** Preferred: International support.

| In use | Where |
|---|---|
| International support | strings.en.yaml hud.caption.standing |
| id: intl_standing, base: 0.50, name: International support | content/model.yaml |
| International Support | docs/GDD.md |
| International Support is the `intl_standing` indicator | docs/gdd/cutover-resource-layer.md |

**Hebrew.** Preferred: תמיכה בינלאומית.

| In use | Where |
|---|---|
| תמיכה בינלאומית | strings.he.yaml hud.caption.standing |
| The Hebrew words are כסף, מזון, נשק and תמיכה בינלאומית, with Social cohesion staying לכידות חברתית | docs/gdd/cutover-resource-layer.md |

**Recommendation (see status).**
- English: Use 'International support' in sentence case. The internal id intl_standing does not appear in player text.
- Hebrew: Use תמיכה בינלאומית, the adopted caption. Native review is tracked in #564.
- Reasons: (1) The owner decision and current captions agree. (2) Naming is settled independently of future balance changes.

**Decision owner.** Eyal and the native reviewer.

### pioneer (decision-needed)

Category: gdd-term. Scope: docs, ui, marketing.

**Definition.** A civilian who has done pioneer training and builds roads, railways, fields and new settlements at designated sites.

**English.** Preferred: halutz (plural halutzim), the current UI wording.

| In use | Where |
|---|---|
| Halutz or pioneer | docs/GDD.md population table |
| Veteran pioneer | docs/GDD.md population table |
| train in a Hakhshara (הכשרה) and become a Halutz | docs/GDD.md |
| Train halutzim: 1 group, {0}, {1} | strings.en.yaml city.train_halutz (#869) |
| Halutzim | strings.en.yaml city.track_halutz (#869) |
| halutz group {0} | strings.en.yaml site.group (#869) |
| No halutz group is free. Train one in a city with a Hakhshara. | strings.en.yaml site.no_halutz (#869) |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| הכשרת חלוצים: קבוצה אחת, {0}, {1} | strings.he.yaml city.train_halutz (#869) |
| חלוצים | strings.he.yaml city.track_halutz (#869) |
| קבוצת חלוצים {0} | strings.he.yaml site.group (#869) |
| אין קבוצת חלוצים פנויה. אפשר להכשיר קבוצה בעיר שיש בה הכשרה. | strings.he.yaml site.no_halutz (#869) |

**Recommendation (see status).**
- English: The UI says 'halutz' / 'halutzim' and 'Hakhshara' (#869), as the GDD does. Proposal for the owner: define each once at first use (for example in a tooltip), or switch the UI to 'pioneer' / 'pioneer training'. Either way it is a string change the owner approves.
- Hebrew: The UI already uses חלוצים and קבוצת חלוצים. A native reader reviews the terminology; this draft does not approve or replace it.
- Reasons: (1) The GDD uses both words for the same unit.

**Decision owner.** Eyal.

### hakhshara (decision-needed)

Category: gdd-term. Scope: docs, ui.

**Definition.** The pioneer training a civilian does to become a halutz.

**English.** Preferred: Hakhshara, the current UI wording.

| In use | Where |
|---|---|
| train in a Hakhshara | docs/GDD.md |
| No halutz group is free. Train one in a city with a Hakhshara. | strings.en.yaml site.no_halutz (#869) |

**Hebrew.** Preferred: הכשרה.

| In use | Where |
|---|---|
| Hakhshara (הכשרה) | docs/GDD.md |
| אין קבוצת חלוצים פנויה. אפשר להכשיר קבוצה בעיר שיש בה הכשרה. | strings.he.yaml site.no_halutz (#869) |

**Recommendation (see status).**
- English: The UI says 'halutz' / 'halutzim' and 'Hakhshara' (#869), as the GDD does. Proposal for the owner: define each once at first use (for example in a tooltip), or switch the UI to 'pioneer' / 'pioneer training'. Either way it is a string change the owner approves.
- Hebrew: The word is given in the GDD; the reviewer confirms it for the UI.
- Reasons: (1) The GDD introduces the Hebrew word in brackets after the transliteration.

**Decision owner.** Eyal.

### resources-and-meters (pending-gdd)

Category: game-system. Scope: ui, docs, events, code.

**Definition.** The quantities the player watches and spends. GDD v0.2 names Money, Food, Arms, Social cohesion and International Support. Current UI captions include Money (net), International support and Public support; HudModel also supplies Social cohesion in the GDD loop. A design target is not proof that every resource screen is implemented.

**English.** Preferred: open.

| In use | Where |
|---|---|
| Money (net) | strings.en.yaml hud.caption.treasury |
| Immigration | strings.en.yaml hud.caption.immigration |
| Security | strings.en.yaml hud.caption.security |
| Public support | strings.en.yaml hud.caption.support |
| International support | strings.en.yaml hud.caption.standing |
| Foreign currency | strings.en.yaml hud.caption.fx |
| \| Money \| Funds development | docs/GDD.md (resource table) |
| \| Arms \| Available military equipment | docs/GDD.md (resource table) |
| \| Food \| Available food supply | docs/GDD.md (resource table) |
| Monthly turns for the real game, and the five resources | README.md (roadmap, Phase 2) |
| the existing meters stay | docs/gdd/cutover-resource-layer.md |
| approval appears as Public support | docs/gdd/cutover-resource-layer.md |
| Food and Arms are national pools | docs/gdd/cutover-resource-layer.md |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| כסף (נטו) | strings.he.yaml hud.caption.treasury |
| עלייה | strings.he.yaml hud.caption.immigration |
| ביטחון | strings.he.yaml hud.caption.security |
| תמיכה ציבורית | strings.he.yaml hud.caption.support |
| תמיכה בינלאומית | strings.he.yaml hud.caption.standing |
| מטבע חוץ | strings.he.yaml hud.caption.fx |
| The Hebrew words are כסף, מזון, נשק and תמיכה בינלאומית, with Social cohesion staying לכידות חברתית | docs/gdd/cutover-resource-layer.md |

**Recommendation (see status).**
- English: Use the adopted resource names and sentence case. Money (net) labels the top-bar net position; the budget uses Money. International support is already adopted in the UI and content. Check the recorded build before describing planned Food or Arms interfaces.
- Hebrew: The current captions are כסף (נטו) and תמיכה בינלאומית. The adopted resource terms remain כסף, מזון, נשק, לכידות חברתית and תמיכה בינלאומית; native review remains separate.
- Reasons: (1) The caption decisions are implemented. (2) Different monetary quantities need precise labels.

**Decision owner.** Eyal.

**Pending GDD.** Use GDD v0.2 for remaining implementation and balance decisions; do not infer feature completion from labels.

### treasury-and-money (preferred)

Category: game-system. Scope: ui, docs, events, code.

**Definition.** Money is the player name for the treasury resource, in IL millions. The top-bar Money (net) meter subtracts debt; the budget reports Money, debt and foreign currency separately (HudModel and budget.money).

**English.** Preferred: Money; Money (net) for the top-bar net position.

| In use | Where |
|---|---|
| Money {0} · debt {1} · foreign currency {2} | strings.en.yaml budget.money |
| id: treasury, name: Money, prefix: 'IL ' | content/model.yaml |
| id: debt, name: Debt, prefix: 'IL ' | content/model.yaml |
| treasury and debt in IL millions | content/rules.yaml comment |
| \| Money \| Funds development | docs/GDD.md (resource table) |
| Money is the Treasury, in IL millions | docs/gdd/cutover-resource-layer.md |

**Hebrew.** Preferred: כסף, for the GDD resource (the owner's word).

| In use | Where |
|---|---|
| כסף {0} · חוב {1} · מטבע חוץ {2} | strings.he.yaml budget.money |
| The Hebrew words are כסף, מזון, נשק and תמיכה בינלאומית, with Social cohesion staying לכידות חברתית | docs/gdd/cutover-resource-layer.md |

**Recommendation (see status).**
- English: Use Money for the resource and preserve Money (net) when naming the top-bar meter. Treasury remains an internal id; it is not an alternative caption. Retain the currency format supplied by the build.
- Hebrew: Use כסף for the budget and כסף (נטו) for the net meter, matching the strings. The localized currency format still needs a decision.
- Reasons: (1) The UI and model now use Money. (2) The net meter and the cash amount are different quantities.

**Decision owner.** Eyal; native reviewer for the Hebrew.

### foreign-currency (preferred)

Category: game-system. Scope: ui, events, code.

**Definition.** The state's stock of foreign currency, held in US dollar millions. When it is exhausted, food imports are cut and the food supply falls.

**English.** Preferred: foreign currency.
Avoid: fx and forex in player text; reserves for this meaning (see 'reserves').

| In use | Where |
|---|---|
| Foreign currency | strings.en.yaml hud.caption.fx |
| foreign currency {2} | strings.en.yaml budget.money |
| id: fx_reserves, name: Foreign currency, prefix: '$' | content/model.yaml |
| fx_start: 40 | content/rules.yaml |
| While reserves are exhausted, food imports are cut | content/rules.yaml comment |
| Debt and foreign currency stay internal, under Money | docs/gdd/cutover-resource-layer.md |

**Hebrew.** Preferred: מטבע חוץ.

| In use | Where |
|---|---|
| מטבע חוץ | strings.he.yaml hud.caption.fx |
| מטבע חוץ {2} | strings.he.yaml budget.money |

**Recommendation (see status).**
- English: Say 'foreign currency' in player text. Capitalisation follows the label rule (conventions: label-capitalisation): the caption and the content name are both 'Foreign currency'. The owner decided on 15 October that debt and foreign currency stay internal under Money, at least through the cutover, and are hidden if they confuse, so the top-bar caption may go.
- Hebrew: No change proposed. The reviewer confirms 'מטבע חוץ'.
- Reasons: (1) The UI uses it in two places and the Hebrew matches. (2) 'Reserves' already means the military reserve in the UI.

**Decision owner.** Eyal.

### reserves (use-with-caution)

Category: game-system. Scope: ui, code, docs.

**Definition.** A word with two meanings in the project: the military reserve the player calls up, and, in code and comments, the foreign-currency stock.

**English.** Preferred: reserves, for the military reserve only.
Avoid: reserves for the currency in player text.

| In use | Where |
|---|---|
| {0} of reserves called up | strings.en.yaml defence.mob |
| Reserve call-up | strings.en.yaml defence.section_callup |
| id: fx_reserves | content/model.yaml |
| set so reserves run out in the last quarter of 1951 | content/rules.yaml comment |

**Hebrew.** Preferred: מילואים, for the military reserve.

| In use | Where |
|---|---|
| גיוס מילואים | strings.he.yaml defence.section_callup |

**Recommendation (see status).**
- English: In player text, use 'reserves' for the military reserve and 'foreign currency' for the money. The ids and comments can keep 'reserves' for the currency.
- Hebrew: The strings already use different words for the two (מילואים and מטבע חוץ), so no change is proposed.
- Reasons: (1) A player who reads 'reserves' in the Budget flow line could take it for either.

**Decision owner.** Eyal.

### budget (preferred)

Category: game-system. Scope: ui, docs, events.

**Definition.** The player's split of spending across ministries, one line per office. The player changes the shares, submits the budget at End turn, and where a legislature must approve it, the submission goes to a vote.

**English.** Preferred: budget.

| In use | Where |
|---|---|
| Budget | strings.en.yaml budget.title |
| Submit the budget | strings.en.yaml budget.submit |
| Changed: submit to put it to the vote | strings.en.yaml budget.unsaved |
| Submitted: takes effect at End turn | strings.en.yaml budget.queued |
| Taxes {0}, spending {1} and reserve call-up {2} a year | strings.en.yaml budget.flow |
| spending effect {0} | strings.en.yaml budget.factor |
| No defence line in the budget | strings.en.yaml defence.budget_none |
| The Immigration line. Its id stays `prime_minister` | content/rules.yaml comment |

**Hebrew.** Preferred: תקציב.

| In use | Where |
|---|---|
| תקציב | strings.he.yaml budget.title |
| הגשת התקציב | strings.he.yaml budget.submit |
| מסים {0}, הוצאות {1} וגיוס מילואים {2} בשנה | strings.he.yaml budget.flow |
| אין בתקציב סעיף ביטחון | strings.he.yaml defence.budget_none |

**Recommendation (see status).**
- English: No change. One note for later: the budget says 'Submitted', and the other player orders say 'Ordered' (see 'order'). That may be deliberate, because a budget can go to a vote.
- Hebrew: The reviewer checks the spelling of 'מסים' (the Hebrew guide asks for full spelling).
- Reasons: (1) The strings use the same word for the screen, the button and the line. (2) The Immigration line's id differs from the office that owns it, so any text that shows ids would mislead. Rule 4 already bans ids in player text.

**Decision owner.** Eyal; native reviewer for the spelling.

### legislature-vote (preferred)

Category: politics. Scope: ui, docs, events.

**Definition.** The step where a legislature takes part in the budget. It must approve it, or it votes and the vote does not bind, or it does not vote at all. Each legislature in content has a flag for this.

**English.** Preferred: 'must approve', 'votes on it, but its vote does not bind', 'does not vote'.

| In use | Where |
|---|---|
| The {0} must approve it: the coalition holds {1} of {2} {3} | strings.en.yaml budget.vote_needed |
| The {0} votes on it, but its vote does not bind: the coalition holds {1} of {2} {3} | strings.en.yaml budget.vote_advisory |
| The {0} does not vote on the budget | strings.en.yaml budget.no_vote |
| Answerable to the {0} · {1} members | strings.en.yaml cabinet.legislature |
| Consults the {0} · {1} members · its votes do not bind | strings.en.yaml cabinet.legislature_advisory |
| budget_approval: true | content/legislatures.yaml |
| body: Provisional State Council | content/legislatures.yaml |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| נדרש אישור של {0}: לקואליציה יש {1} מתוך {2} {3} | strings.he.yaml budget.vote_needed |
| {0} מצביעה על התקציב, אך ההצבעה אינה מחייבת: לקואליציה יש {1} מתוך {2} {3} | strings.he.yaml budget.vote_advisory |
| {0} אינה מצביעה על התקציב | strings.he.yaml budget.no_vote |
| אחראית בפני {0} · {1} חברים | strings.he.yaml cabinet.legislature |
| מתייעצת עם {0} · {1} חברים · ההצבעות אינן מחייבות | strings.he.yaml cabinet.legislature_advisory |
| body_he: "מועצת המדינה הזמנית" | content/legislatures.yaml |
| body_he: "הכנסת השנייה" | content/legislatures.yaml |

**Recommendation (see status).**
- English: No change. The wording states the rule and the coalition's seats without comment.
- Hebrew: After the interim neutral wording in #1081, #1136 applied Beri's native review from #564 to the budget strings. The current feminine forms work because all three legislature names are feminine; a masculine-named body would need new wording. Keep these native-reviewed strings for the current bodies. ContentNames.Of reads BodyHe (#861). Any new agreement or gender-field proposal still needs native review and an owner decision.
- Reasons: (1) Beri's native note on #564 supports the feminine forms for the three current bodies, not every future body name. (2) #1136 applied that review; record the current wording and its limit rather than reopening the earlier finding.

**Decision owner.** Native reviewer; Eyal for any key change.

### stability (preferred)

Category: politics. Scope: ui, events, code.

**Definition.** A coalition indicator shown beside its seat count. It falls when the player replaces a minister, by more when the replacement is from another party, and, with the coalition's majority, it decides whether a vote passes.

**English.** Preferred: stability.

| In use | Where |
|---|---|
| appoint_stability_cost_other_party: 0.06 | content/rules.yaml |
| Coalition stability lost by replacing a minister (Cabinet.Appoint) | content/rules.yaml comment |
| No majority: fails. Majority with stability >= 0.5: passes. Otherwise passes with probability 2 * stability. | (git grep) |
| coalition stability {PlayerText.PercentChange | (git grep) |

**Hebrew.** Preferred: יציבות.

**Recommendation (see status).**
- English: Use 'coalition stability' in text the player reads. One finding: the Why panel's cause line is composed in C# ('coalition stability', 'resents the reshuffle'), not in the strings files, so the Hebrew UI cannot cover it yet (see conventions: dates).
- Hebrew: The reviewer supplies the term; 'יציבות' is the plain word, but whether it carries the coalition sense is not for this file to say.
- Reasons: (1) The strings that show it also show the seat count, so the player sees both. (2) The in-code wording is player-facing and lives outside the localisation files.

**Decision owner.** Eyal; native reviewer for the Hebrew.

### majority-and-opposition (use-with-caution)

Category: politics. Scope: ui, code.

**Definition.** Majority is the number of seats that carries a vote: the body's size halved, rounded down, plus one. Opposition is the seats of every bloc outside the coalition.

**English.** Preferred: majority; opposition.

| In use | Where |
|---|---|
| Majority: {0} {1} | strings.en.yaml cabinet.majority |
| Opposition: {0} {1} | strings.en.yaml cabinet.opposition |
| Majority => size / 2 + 1 | (git grep) |
| blocks.Where(b => !b.Coalition).Sum(b => b.Seats) | (git grep) |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| רוב: {0} {1} | strings.he.yaml cabinet.majority |
| אופוזיציה: {0} {1} | strings.he.yaml cabinet.opposition |

**Recommendation (see status).**
- English: 'Majority: {0} {1}' reads as a holding, but it is the threshold. A drop-in with the same two placeholders is 'Needed for a majority: {0} {1}'. Optional, and the first thing to take to a cold-read test.
- Hebrew: The same ambiguity may apply to 'רוב'. The reviewer decides.
- Reasons: (1) The HUD shows the coalition's seats on the line above, so a number after 'Majority' can be misread as the coalition's own total. (2) 'Opposition' needs no change.

**Decision owner.** Eyal; native reviewer.

### cabinet-and-government (contested)

Category: politics. Scope: ui, docs, events, code.

**Definition.** 'Cabinet' is the screen and the group of ministers the player appoints. A 'government' in content is a numbered period with its own coalition (Provisional, First, and so on).

**English.** Preferred: Cabinet for the screen and the group; the government's name for the period.

| In use | Where |
|---|---|
| Cabinet | strings.en.yaml hud.nav.cabinet, why.cabinet |
| name: Provisional Government | content/governments.yaml |
| name: First Government | content/governments.yaml |
| The Cabinet with appointments | README.md (roadmap, Phase 1) |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| ממשלה | strings.he.yaml hud.nav.cabinet, why.cabinet |
| name_he: "הממשלה הזמנית" | content/governments.yaml |
| name_he: "ממשלת ישראל הראשונה" | content/governments.yaml |

**Recommendation (see status).**
- English: No change to the English; it separates the two words.
- Hebrew: The Hebrew uses 'ממשלה' for the Cabinet screen, and 'ממשלה' is also the word for a government. Since #558 the content carries sourced Hebrew names for the governments ("הממשלה הזמנית", "ממשלת ישראל הראשונה"), so a period name and the screen label share the word. The Cabinet now displays the government name through ContentNames.Of, using NameHe in the Hebrew UI where supplied and English otherwise. The native reviewer decides whether the two senses need distinct labels; implementation does not settle that terminology choice.
- Reasons: (1) The English separates the two senses and the Hebrew uses one word for both.

**Decision owner.** Native reviewer.

### office-ministry-minister (preferred)

Category: politics. Scope: ui, events, docs, code.

**Definition.** An office is a post the player can fill: a minister's portfolio, the Prime Minister, the Chief of Staff. A ministry is the department a minister runs. Office titles are proper nouns.

**English.** Preferred: office for the post, ministry for the department, 'Minister of ...' for the title as Israel writes it.

| In use | Where |
|---|---|
| Appoint to {0} | strings.en.yaml cabinet.pick |
| no ministry until {0} | strings.en.yaml budget.not_established |
| vacant | strings.en.yaml budget.vacant |
| {0} (your appointee) | strings.en.yaml card.appointed_name |
| id: prime_minister, title: Prime Minister | content/offices.yaml |
| id: interior_minister, title: Minister of Internal Affairs | content/offices.yaml |
| The English titles stay the player's default | content/offices.yaml comment |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| מינוי לתפקיד {0} | strings.he.yaml cabinet.pick |
| אין משרד עד {0} | strings.he.yaml budget.not_established |
| פנוי | strings.he.yaml budget.vacant |
| {0} (מינוי שלך) | strings.he.yaml card.appointed_name |
| title_he: "שר הפנים" | content/offices.yaml |
| title_he_f: "שרת החוץ" | content/offices.yaml |
| המשרה {0} בוטלה ב־{1} | strings.he.yaml cabinet.reason.abolished |

**Recommendation (see status).**
- English: Use 'office' for the post and 'ministry' for the department. Hebrew uses תפקיד and משרד in the earlier strings, and the Cabinet refusal reasons also use משרה (#985); a native reader should assess the variants.
- Hebrew: The native reviewer assesses תפקיד and משרה for office, alongside משרד for ministry.
- Reasons: (1) New Cabinet refusal strings introduce another Hebrew word for office; consistency is a review question, not an established fact.

**Decision owner.** Eyal.

### chief-of-staff (use-with-caution)

Category: politics. Scope: ui, events, docs.

**Definition.** The head of the armed forces' general staff. The office title in content is longer than the label the Defence screen uses.

**English.** Preferred: 'Chief of Staff' in running UI text; the full title where the office itself is named.

| In use | Where |
|---|---|
| Minister of Defense {0} · Chief of Staff {1} | strings.en.yaml defence.command |
| id: chief_of_staff, title: IDF Chief of the General Staff | content/offices.yaml |
| id: haganah_chief_of_staff, title: Haganah Chief of the General Staff | content/offices.yaml |

**Hebrew.** Preferred: הרמטכ״ל.

| In use | Where |
|---|---|
| שר הביטחון {0} · הרמטכ״ל {1} | strings.he.yaml defence.command |
| title_he: "ראש המטה הכללי" | content/offices.yaml |
| title_he: 'רמטכ״ל ההגנה' | content/offices.yaml |

**Recommendation (see status).**
- English: Keep the short form on the Defence screen. Use the content title when an event names the office, so the Haganah and the IDF titles stay distinct.
- Hebrew: Three Hebrew forms exist. The Defence screen says 'הרמטכ״ל', the content titles the IDF post 'ראש המטה הכללי', and the Haganah post 'רמטכ״ל ההגנה'. Since #630 the gershayim is the typographic ״ in the strings and in the content, so the earlier mismatch is closed. The comment in offices.yaml lists two open owner questions, one of them whether the IDF title adds 'של צה״ל'.
- Reasons: (1) The content already separates the Haganah and the IDF post, and the UI label does not.

**Decision owner.** Eyal.

### public-support (preferred)

Category: game-system. Scope: ui, code.

**Definition.** A top-bar meter. The code computes it as approval across voter groups, each weighing morale, security, housing and food by its own weights. The owner decided on 15 October that approval appears as Public support.

**English.** Preferred: Public support.
Options: Public support; Public support; Approval.

| In use | Where |
|---|---|
| Public support | strings.en.yaml hud.caption.support |
| Support = Points(EffectsSystem.Approval(c, s) | (git grep) |
| id: veterans, weights: { morale: 0.6, security: 0.4 } | content/model.yaml |
| approval appears as Public support | docs/gdd/cutover-resource-layer.md |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| תמיכה ציבורית | strings.he.yaml hud.caption.support |

**Recommendation (see status).**
- English: Use 'Public support' for the meter and approval for the code concept. The top-bar caption is already sentence case, as adopted in #630.
- Hebrew: The reviewer checks 'תמיכה ציבורית' against the owner's 'תמיכה בינלאומית', since the two share a word.
- Reasons: (1) The owner has named it and separated it from Social cohesion. (2) With International Support, the game will have two meters with 'support' in the name; the Hebrew shares 'תמיכה' for the same reason.

**Decision owner.** Eyal.

### order (contested)

Category: game-system. Scope: ui, docs, code.

**Definition.** An action the player submits, such as appointing a minister, changing policy, submitting a budget or assigning pioneer work. Attention costs depend on the command; some unit orders cost zero (content/rules.yaml).

**English.** Preferred: order.

| In use | Where |
|---|---|
| Ordered: {0} to {1}, at End turn | strings.en.yaml cabinet.queued |
| Ordered: {0}, at End turn | strings.en.yaml info.queued |
| Ordered: level {0} to {1} | strings.en.yaml defence.order |
| That order is not valid | strings.en.yaml order.invalid |
| Your order: {0} | strings.en.yaml outcome.order |
| Submitted: takes effect at End turn | strings.en.yaml budget.queued |
| pioneer_work: 0 | content/rules.yaml |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| ניתנה פקודה: {0}, בסיום התור | strings.he.yaml info.queued |
| הפקודה שלך: {0} | strings.he.yaml outcome.order |
| הפקודה אינה תקפה | strings.he.yaml order.invalid |
| הוגש: ייכנס לתוקף בסיום התור | strings.he.yaml budget.queued |
| הוזמנו {0} | strings.he.yaml city.train_ordered (#869) |
| הוזמן | strings.he.yaml site.ordered (#869) |

**Recommendation (see status).**
- English: Use 'order' for the player's actions and say what was ordered. The budget says 'Submitted' for the same kind of step; that can stay if the vote is the reason (see 'budget').
- Hebrew: 'פקודה' is a command word. The reviewer decides whether it suits civil actions such as appointing a minister, or whether another word is better there. The city and site panels also use הוזמנו / הוזמן; the native reviewer should consider these alongside פקודה.
- Reasons: (1) The same End turn mechanic reads as 'Ordered' in some strings and 'Submitted' in one. (2) Order costs vary, including zero-cost unit orders; wording must not imply every order spends attention.

**Decision owner.** Eyal; native reviewer.

### era-and-chapter (contested)

Category: game-system. Scope: ui, docs, code.

**Definition.** A span of history that a content set covers. The Why panel calls it a Chapter; the rules and the roadmap say era.

**English.** Preferred: open.
Options: Chapter; Era.

| In use | Where |
|---|---|
| Chapter | strings.en.yaml why.era |
| The first owner starts on the era's first day | content/rules.yaml comment |
| Later eras | README.md (roadmap) |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| פרק | strings.he.yaml why.era |
| תקופות מאוחרות | README.he.md (roadmap) |

**Recommendation (see status).**
- English: Pick one word for the span the player is playing. If both stay, define them: a chapter is the part the player plays now, and an era is the longer period.
- Hebrew: The Hebrew separates them already: 'פרק' in the UI and 'תקופות' in the roadmap.
- Reasons: (1) The UI and the project documents use different words for what looks like one idea.

**Decision owner.** Eyal.

### policy (preferred)

Category: game-system. Scope: ui, events, code.

**Definition.** A setting the player can change that has standing effects, such as rationing, housing construction or transit camps. Changing one is an order and costs attention.

**English.** Preferred: policy.

| In use | Where |
|---|---|
| Policies | strings.en.yaml why.policy |
| id: rationing, default: 0.0, office: rationing_minister, name: Rationing | content/model.yaml |
| name: Housing construction | content/model.yaml |
| name: Transit camps (ma'abarot) | content/model.yaml |

**Hebrew.** Preferred: מדיניות.

| In use | Where |
|---|---|
| מדיניות | strings.he.yaml why.policy |

**Recommendation (see status).**
- English: No change. One finding for the transliteration convention: the content name is 'Transit camps (ma'abarot)', which gives the Hebrew word in brackets, while the strings say 'transit camp'.
- Hebrew: No change proposed.
- Reasons: (1) The word is used the same way in the UI and in content.

**Decision owner.** Eyal.

### effects-and-situations (use-with-caution)

Category: game-system. Scope: ui, code.

**Definition.** In content/model.yaml, an effect is a standing link from a policy or indicator to a target indicator, and a situation is a named crisis that switches on below one level and off above another. The Why panel has a section for each.

**English.** Preferred: 'Ongoing effects' and 'Situations', as in the Why panel.

| In use | Where |
|---|---|
| Ongoing effects | strings.en.yaml why.effects |
| Situations | strings.en.yaml why.situations |
| id: food_crisis, indicator: food_supply, on_below: 0.30, off_above: 0.45, name: Food crisis | content/model.yaml |
| id: rationing_feeds, source: rationing, target: food_supply, k: 0.40, inertia: 2, office: rationing_minister, name: Rations share out food | content/model.yaml |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| השפעות מתמשכות | strings.he.yaml why.effects |
| מצבים | strings.he.yaml why.situations |

**Recommendation (see status).**
- English: The two labels are generic. 'Situations' in particular could mean any state of the game. Add one line under each heading, or test a plainer word such as 'Crises' only if every situation is a crisis, which the files read here do not show.
- Hebrew: 'מצבים' is broad for the same reason. The reviewer decides.
- Reasons: (1) The model defines a situation by thresholds, but the heading does not tell the player that.

**Decision owner.** Eyal; native reviewer.

### aliyah-and-immigration (preferred)

Category: history-term. Scope: ui, events, docs, code.

**Definition.** Jewish immigration to Israel. English text says 'immigration'; Hebrew says 'עלייה'; 'aliyah' appears in a file name, a party id and a code comment about a ministry's name.

**English.** Preferred: immigration.

| In use | Where |
|---|---|
| Immigration | strings.en.yaml hud.caption.immigration |
| Immigration and population | strings.en.yaml why.population |
| immigration during 1948-1951 | docs/GDD.md (scope) |
| id: immigration_minister, title: Minister of Immigration | content/offices.yaml |
| The Ministry of Aliyah (immigration_minister) ends on 8 October 1951 | content/rules.yaml comment |
| CBS 1948 immigration series (aliyah.yaml) | content/rules.yaml comment |

**Hebrew.** Preferred: עלייה.

| In use | Where |
|---|---|
| עלייה | strings.he.yaml hud.caption.immigration |
| עלייה ואוכלוסייה | strings.he.yaml why.population |
| title_he: "שר העלייה" | content/offices.yaml |

**Recommendation (see status).**
- English: Keep 'immigration' for the HUD and prose, and use 'aliyah' where a source names it. The sourcing question is answered: a comment in rules.yaml calls the ministry 'the Ministry of Aliyah', and since #558 offices.yaml gives its Hebrew title as 'שר העלייה' from the Knesset's records, with a comment that the English titles stay the player's default. So 'Minister of Immigration' is the English rendering of a sourced Hebrew title.
- Hebrew: No change proposed. The reviewer confirms 'עלייה' for the meter.
- Reasons: (1) The Hebrew title and the source comment agree on Aliyah; the English title is the default rendering.

**Decision owner.** Eyal.

### mandate-sub-district (preferred)

Category: place. Scope: ui, docs.

**Definition.** The British Mandate's administrative areas, as drawn in 1947, which the map text uses to name regions.

**English.** Preferred: Mandate sub-districts.

| In use | Where |
|---|---|
| {0}, {1} (Mandate sub-districts, 1947) | strings.en.yaml info.region |
| Mandate sub-districts | strings.en.yaml map.layer_front_no_data |

**Hebrew.** Preferred: נפות המנדט.

| In use | Where |
|---|---|
| נפות המנדט | strings.he.yaml info.region, map.layer_front_no_data |

**Recommendation (see status).**
- English: No change. Spell 'sub-district' with a hyphen everywhere.
- Hebrew: The reviewer confirms 'נפות' as the term.
- Reasons: (1) Both languages use one term in both strings.

**Decision owner.** Eyal.

### terrain (use-with-caution)

Category: game-system. Scope: ui.

**Definition.** The terrain type of a hex on the map: sea, lake, coastal plain, hills, mountains, valley, inland lowland, desert. The keys call it a biome.

**English.** Preferred: terrain.

| In use | Where |
|---|---|
| Coastal plain | strings.en.yaml info.biome.coastal_plain |
| Valley | strings.en.yaml info.biome.valley |
| Inland lowland | strings.en.yaml info.biome.lowland |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| בקע | strings.he.yaml info.biome.valley |
| עמק | strings.he.yaml info.biome.lowland |

**Recommendation (see status).**
- English: No change. Keep 'biome' as the code word only.
- Hebrew: Question for the reviewer: 'Valley' is 'בקע' and 'Inland lowland' is 'עמק'. 'עמק' appears to be the usual word for a valley, and 'בקע' (as in 'בקעת הירדן') a rift or broad basin. If that is right, the two may be swapped or need different English.
- Reasons: (1) The English pair and the Hebrew pair may not match one to one.

**Decision owner.** Native reviewer.

### front-line-and-control (preferred)

Category: history-term. Scope: ui, docs.

**Definition.** Who held an area on a date, as the map's control layer shows it. With the armistice lines, the map also names the two sides and the demilitarized zone.

**English.** Preferred: the labels the strings use: 'Held by Israel', 'Held by Arab forces', 'Contested', "No-man's land", 'Israeli side', 'Arab side', 'Demilitarized zone'.

| In use | Where |
|---|---|
| Held by Israel | strings.en.yaml info.control_israel |
| Held by Arab forces | strings.en.yaml info.control_arab |
| the Arab Liberation Army | strings.en.yaml control.holder_ala |
| Demilitarized zone | strings.en.yaml info.control_dmz |
| Front lines, {0}, approximate (Israel State Archives) | strings.en.yaml map.caption_held |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| בשליטת ישראל | strings.he.yaml info.control_israel |
| בשליטת כוחות ערביים | strings.he.yaml info.control_arab |
| צבא ההצלה | strings.he.yaml control.holder_ala |
| אזור מפורז | strings.he.yaml info.control_dmz |

**Recommendation (see status).**
- English: No change. The captions follow the project's framing (docs/CONTENT_POLICY.md, section 1) and cite their source. Keep it that way, and send any new label for a side or a holder through the content review, as the policy requires for sensitive wording. The unit names in the GDD ('Arab Legionnaire', 'Palestinian militia') are more specific than the UI's 'Arab forces'; the two should not contradict each other.
- Hebrew: The English names 'the Arab Liberation Army' and the Hebrew gives 'צבא ההצלה'. The reviewer confirms that this is the standard Hebrew name.
- Reasons: (1) The labels name the holder and the date, not a judgement. (2) The map caption states that the lines are approximate.

**Decision owner.** Eyal; historian review for new labels.

### city (pending-gdd)

Category: gdd-term. Scope: ui, docs.

**Definition.** A large settlement the game models in detail: housing, jobs and population. content/cities.yaml contains twelve cities at 9e571342 (#821); Tel Aviv was the earlier Phase 1 slice, not the full current list.

**English.** Preferred: city.

| In use | Where |
|---|---|
| Cities | strings.en.yaml why.city |
| one city (Tel Aviv), training, one new settlement and one objective card | README.md (roadmap, Phase 1) |

**Hebrew.** Preferred: open.

| In use | Where |
|---|---|
| ערים | strings.he.yaml why.city |

**Recommendation (see status).**
- English: Keep 'city' for the places modeled in detail. The GDD and site UI call a new place the player founds a 'yishuv'; see that entry before proposing a different term.
- Hebrew: The reviewer confirms 'עיר' against 'יישוב'.
- Reasons: (1) The README separates a city from a new settlement.

**Decision owner.** Eyal.

**Pending GDD.** Further city design and terminology proposals remain owner-gated; the twelve-city content and city panel already exist.
