# translations/

Translations of the game, written by `tools/translate.py` ([tools/README.md](../tools/README.md)). The game loads and
checks these files with the content ([docs/translations.md](../docs/translations.md), "Loading"); a language is offered
to players through the start-of-game language picker and the Settings menu (#1606). English stays in `content/` and
`unity/Assets/UI/`. Hebrew is moving here one file at a time (#1618): `he/ui.yaml` holds the Hebrew UI strings (step 4),
and the events' and content's Hebrew stay inline in `content/` until steps 5 and 6. `he/ui.yaml` keeps its comments (the
native reads and rulings), so the tool edits it line by line rather than rewriting it.

Each language has a folder named by its code (`ro`, `fr`, `pt-BR`), with up to three files:

| File | Holds | Keyed by |
|---|---|---|
| `events.yaml` | each event's `title`, `text` and `options` (by option id), and `review` | the event id in `content/events/` |
| `ui.yaml` | the UI strings, except the advisors (`advisor.*`) and their encyclopedia (`encyclopedia.*`, which replaced How to play), which stay English or Hebrew only for now (owner, 24 October 2026, #1604) | the key in `unity/Assets/UI/strings.en.yaml` |
| `content.yaml` | every content field that has a Hebrew form (`name_he`, `title_he_f`, `sector_he` ...), as `text` and `review` | `<file under content/>#<record path>.<field>`, for example `people.yaml#people.ben_gurion.name` |

The rules are the Hebrew ones ([docs/WRITING_EVENTS.md](../docs/WRITING_EVENTS.md)): an event is all or none, placeholders
match the English, one line each, no direction marks, and `review: agent-checked` until a native speaker, or an
AI-assisted contributor who discloses it, has read the text (#564; owner, 4 November 2026: "AI-assisted can clear it").
The tool rewrites these files whole, so only their header comment is kept.

`ro/` is an example of the layout: `ui.yaml` holds one string (`ui.lang`, the language code) and `events.yaml` the
`first_truce` event. Start a language with `uv run tools/translate.py`, then "To" > "New language…".
