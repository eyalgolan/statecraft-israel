# Example: 46 seconds, one real decision

A worked example. It tells one real decision from the game in plain words, so a viewer who has never heard of the period understands what happened and what the choice is. Every claim about the game or the history points at a source id. It is a teaching example, not a script to publish: the footage and build id are placeholders.

```
Title: Example 46 second one-decision explainer
Channel: social (example only)
Length (seconds): 46
Language: en
Audience: strategy players (primary), history-minded players (secondary)
Build id or footage note: REPLACE with a real build and commit id
```

## Build and naming limit

The table uses the current English label, "International support" (#683), and the event effects at upstream 46abd99c. The internal effect key remains intl_standing. The content file establishes the decision and options; footage still needs a real build id and a check that the card appears in that build.

## Shot table

| # | Time | Picture | On-screen text | Voice-over | Sources |
|---|---|---|---|---|---|
| 1 | 0-5 | Real build: the Hula decision card | 1951. Israel's prime minister must decide. | It is 1951, and Israel's prime minister has a decision to make. | F1, game:G1 |
| 2 | 5-14 | Real build: map pan to northern Israel, then the card text | Drainage works near Lake Hula have reached a demilitarised zone on the Syrian border. | Israeli drainage works near Lake Hula, in northern Israel, have reached a demilitarised zone on the Syrian border. | F1, F2 |
| 3 | 14-22 | Card text | Syria has complained. The armistice commission's chairman asks Israel to stop. | Syria has complained. The armistice commission's chairman asks Israel to stop work until there is an agreement. | F1, F3 |
| 4 | 22-30 | Card options | Continue the works. Or halt until there is agreement. | You can continue and assert Israel's rights in the zone, or halt until there is agreement. | game:G2 |
| 5 | 30-40 | The top bar before and after, then the Why panel | Continue: security up, international support down. Halt: the reverse. | Continue, and security rises but international support falls. Halt, and it goes the other way. The Why panel shows what caused each change. | game:G3, game:G4 |
| 6 | 40-46 | Title card | Statecraft: Israel. Free. A source for every fact. | Statecraft: Israel is free, and every fact has a source. | F4 |

## Fact sheet for this example

| Id | Claim | Kind | Source | Tag |
|---|---|---|---|---|
| F1 | In 1951, drainage works south of Lake Hula, under the Palestine Land Development Company's concession, reached the demilitarised zone with Syria. | history | [FRUS 1951 vol. V, doc. 288](https://history.state.gov/historicaldocuments/frus1951v05/d288), paragraphs on the concession and works south of Lake Hula; see [source audit](../references/09-source-audit.md) | [V] AI-assisted source check, 17 October 2026 |
| F2 | Lake Hula is in the north of Israel. | history | [Israel Nature and Parks Authority, Hula Nature Reserve](https://en.parks.org.il/reserve-park/hula-nature-reserve/), historic lake and access; Upper Galilee context in its [regional guide](https://www.parks.org.il/palgey-maim/) | [V] AI-assisted source check, 17 October 2026 |
| F3 | Syria complained, and the chairman of the armistice commission asked that work stop pending agreement. | history | FRUS doc. 288, complaint of 21 February, stop-work requests and footnote 5 on the 7 March memorandum | [V] AI-assisted source check, 17 October 2026 |
| F4 | The game is free and every historical fact carries a source. | game | `README.md` tagline | [V] |
| G1 | The Hula event defines a decision for the prime minister. Its appearance in recorded footage still needs a build check. | game | `content/events/hula_drainage.yaml`, office: prime_minister, at 46abd99c | [V] file checked; build not recorded |
| G2 | The two options are "Continue the works and assert Israel's rights in the zone" and "Halt the works until there is agreement". | game | same file | [V] |
| G3 | The Why panel explains a turn's changes. | game | `README.md`, "What it looks like today" and current-build list | [V] |
| G4 | Continuing raises security and lowers international support. Halting lowers security and raises international support. | game | the options' effects in the same event file; confirm the numbers in the recorded build | [V] |

## What this example shows

- A viewer who knows nothing learns who (Israel's prime minister), when (1951), where (near Lake Hula, on the Syrian border), what (drainage works reached a demilitarised zone), the problem (Syria complained and the commission's chairman asked Israel to stop) and the choice, with what each option costs.
- Every line works with the sound off. The on-screen text carries the message.
- No adjective carries a claim. The "cost somewhere else" idea arrives as a visible trade-off in the top bar, not as a slogan.
- Picture and voice tell the same story: the card on screen is the decision the voice describes.
