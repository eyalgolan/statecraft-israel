# translations/

Translations into languages other than English and Hebrew, written by `tools/translate.py` ([tools/README.md](../tools/README.md)).
The game does not read these files yet: loading them needs a change to the sim loader, the content schemas and
`unity/Assets/Scripts/UI/UiLanguage.cs`, and a language is offered to players through the start-of-game language
picker and the Settings menu (#1606). English and Hebrew stay where they are, in `content/` and `unity/Assets/UI/`.

Each language has a folder named by its code (`ro`, `fr`, `pt-BR`), with up to three files:

| File | Holds | Keyed by |
|---|---|---|
| `events.yaml` | each event's `title`, `text` and `options` (by option id), and `review` | the event id in `content/events/` |
| `ui.yaml` | the UI strings, except the advisors (`advisor.*`) and How to play (`how_to_play.*`), which stay English or Hebrew only for now (owner, 24 October 2026, #1604) | the key in `unity/Assets/UI/strings.en.yaml` |
| `content.yaml` | every content field that has a Hebrew form (`name_he`, `title_he_f`, `sector_he` ...), as `text` and `review` | `<file under content/>#<record path>.<field>`, for example `people.yaml#people.ben_gurion.name` |

The rules are the Hebrew ones ([docs/WRITING_EVENTS.md](../docs/WRITING_EVENTS.md)): an event is all or none, placeholders
match the English, one line each, no direction marks, and `review: agent-checked` until a native speaker has read the
text (#564). The tool rewrites these files whole, so only their header comment is kept.

`ro/` is an example of the layout: `ui.yaml` holds one string (`ui.lang`, the language code) and `events.yaml` the
`first_truce` event. Start a language with `uv run tools/translate.py`, then "To" > "New language…".
