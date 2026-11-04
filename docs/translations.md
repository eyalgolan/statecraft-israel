# The translation contract

How a language other than English is stored in `translations/<lang>/` (#1618; owner, 24 October 2026: option (a), "verify
it doesnt cause any bugs, degregations or gaps"). English stays the source of record in `content/` and
`unity/Assets/UI/strings.en.yaml`. This page fixes the files, their keys and their order, so the tools, the sim and Unity
read and write them the same way.

**Status (steps 1 and 2 of #1618):** the contract and its tests, and the transport that brings `translations/` to the
sim, the CLI and every player (below, "Loading"). The game still shows Hebrew from the inline `*_he` fields and
`strings.he.yaml`; nothing has moved. `Translations.InlineHebrew` (sim) reads today's inline Hebrew as these files,
and the tests prove every value comes back unchanged. `tools/translate.py` (#1604) has the same reader,
`inline_hebrew()`, and the same key quoting (`_yaml_key`); `tools/tests/test_translate.py` holds it to the same fixture
bytes, so the tool and the game write one format.

## The files

Each language has a folder named by its code (`he`, `ro`, `pt-BR`: lowercase ISO 639, then optional `-` subtags). Each of
the three files may be missing, which means the language has none of that kind.

| File | Key | Entry |
|---|---|---|
| `ui.yaml` | the key in `strings.en.yaml` (`hud.turn`) | the text |
| `events.yaml` | the event id (`altalena`) | `title`, `text`, `options` (option id: text, in the event's order), and `review` |
| `content.yaml` | a content key (below) | `text`, `review`, and for a language other than Hebrew `sources` |

```yaml
# events.yaml
altalena:
  title: "האלטלנה"
  text: "... {defense_minister}."
  options:
    enforce: "..."
    compromise: "..."
  review: "agent-checked"
```

`review` is `agent-checked` until a native speaker has read the text, and is left out after that (#564). It is the only
value it may have.
An AI-assisted contributor may also do that read and remove the line, when the pull request discloses the AI help and
names the strings read (the owner, 4 November 2026: "AI-assisted can clear it"; [comment](https://github.com/eyalgolan/statecraft-israel/pull/2481#issuecomment-6623108295)).

## Content keys

`<file>#<path>.<field>`, for example `people.yaml#people.ben_gurion.name` or
`map/units_1948.yaml#units.golani.anchors.0.place`.

- `<file>` is relative to `content/`.
- Each step of `<path>` is a mapping key, or, in a list, the item's `id`. An item with no text `id` is named by its index,
  so reordering such a list moves its translations to other items.
- `<field>` is the English field the text translates. `title_f` is the feminine form of `title` (offices.yaml's
  `title_he_f`).
- A key must name exactly one record, and that record must have the English field as text. `Translations.Resolve` refuses
  anything else.

A content field is keyed when its file has a `<field>_he` (or `<field>_he_f`) beside a text `<field>` in a record of
that shape. Every record of the same shape gets a key, whether or not it has its translation yet; that is the list
`tools/translate.py` (#1604) shows.

## What stays on the English record

These fields are facts about the record or its Hebrew, so they are never keys and stay in `content/`:
`*_he_sources`, `biography_sources.name_he`, `title_he_form`, `title_he_from`, `title_he_until`, `sources`, `status`,
`reviewed_by` and `reviewed_on`. A Hebrew entry may not carry `sources`; another language may carry its own.

## Not keyed yet

`content/map/subdistricts_1948.geojson` (`name_he`, `district_he`) and `content/map/control_1948_49.geojson`
(`legend_he`) hold Hebrew map labels that their digitisers write as a sourced set, next to the Arabic
(`tools/borders_digitise.py`, `tools/control_digitise.py`). The sub-districts' Hebrew stays in place, a documented
exception (owner, 25 October 2026, on #1618: "Keep in place, document it"), because it is sourced together with the
Arabic names to the Palestine Gazette, No. 1415. The front-line legends of `control_1948_49.geojson` wait for the
owner's decision. `TranslationsTests` names both files, and fails if any other non-YAML content file has Hebrew.

## Sign-offs and the review queue

A signed row's sign-off (`reviewed_by`, `reviewed_on`) covers its Hebrew wherever it lives: inline, or as the row's
entry in `he/events.yaml` (an event, by its id) or `he/content.yaml` (a record, by its keys). Hebrew moved with the
same decoded text keeps the sign-off (owner, 25 October 2026, on #1618: "Yes, carry them over"). The inline YAML and
`translations/he/` quote differently (a folded `>-` scalar becomes a double-quoted one), so "byte-identical moved text"
is read as the same decoded text: every character the same, direction marks, nikkud and maqaf included. `scripts/oss/reviewed_by_guard.py`
holds the line. A commit with an AI co-author trailer may take a signed row's Hebrew lines out of `content/` when the
row's Hebrew, read from both places, is the same before and after. Any other change to that Hebrew, in either place,
is an edit of the signed row: a person must sign the row again, or an AI-assisted contributor may sign it again
when the pull request's "AI assistance" section discloses the AI help, names the check and says which sources were
opened and what was checked (owner, 4 November 2026: "Yes, it counts"); CI checks only that the section names a check,
and the reviewer reads the rest (`docs/CONTENT_POLICY.md`). A commit with an AI co-author trailer never removes the
sign-off.

`tools/review_queue.py` lists an entry's `review: agent-checked` just as it lists the inline `*_he_review` flag, with
the English record's row and sources (an event's `text_he_sources`, else its own `sources`).

A change to `translations/` is a change players get, so it needs a changelog entry (`scripts/oss/changelog_check.py`;
`translations/README.md` excepted). `tools/package_release.py` refuses a build whose `StreamingAssets/translations`
files and `bundle.json`'s packed texts differ, so the release gate checks the same translations every player reads.

## Order and format

A file lists its entries in this order: the events by `from` date, then id; the UI strings in `strings.en.yaml`'s order,
with any others after them by key; the content fields by file (`content/*.yaml`, then `content/map/*.yaml`, each in
ordinal order) and then by their position in the file.

The body (everything after the header comment) is fixed byte for byte: block mappings, two-space indents, every
string double-quoted the way JSON writes it (`json.dumps(..., ensure_ascii=False)`), and a key plain only when it is
letters, digits, `_`, `.` and `-` and no YAML reader would type it: not a value the YAML 1.2 core schema reads as null, a
boolean or a number (`null`, `~`, `true`, `1948`, `1e3`, `.inf`), and not a YAML 1.1 boolean (`y`, `yes`, `on`, `off` and
their capitalised forms). Any other key is double-quoted (`Translations.Key()`, `tools/translate.py` `_yaml_key`). The header comment belongs to the tool that writes the file.

## What the parser checks, and what it leaves to the content rules

`Translations.Parse` checks the shape: the known fields only, keys of the right form, no key twice, every text a
non-empty string, `review` only `agent-checked`, and `sources` as a list of `{ url, checked }`. The rules about the
text itself (an event is all or none, placeholders as in the English, no stray `{` or `}`, Hebrew letters, no direction
marks, the UI's approved wording) are the content rules. They run on the text where it is used, as they do today.

## A UI language names itself

`ui.yaml` carries `ui.lang`, and its value is the language's own code — `ui.lang: "ro"` in `translations/ro/`. It is
not a label. The player reads this key to decide which language it is showing: the Hebrew face on the UI root, and the
branches in `EventCardPresenter` and `SharePresenter`. So a file that says the wrong thing here does not fail, it
shows one language's words laid out as another's.

A language offered to the player must therefore pass `Translations.CheckDeclaredLanguage`, which refuses a `ui.yaml`
with no `ui.lang` and one whose `ui.lang` names a different language from its directory. The second is the case worth
the check: a `ui.yaml` copied from another language with its code left behind passes every shape check in `Parse`.
`Parse` itself does not require the key, because it is also used to read a file's shape in isolation — the check is a
separate call, made where a language is adopted.

## Loading

`ContentLoader.Load` reads `translations/` with the content (#1618, step 2) and keeps each language in
`Content.Translations`, for the language picker of #1606 and the later steps of #1618. Nothing shows them yet.

- **Where:** the `translations/` beside a folder named `content` (`TranslationLoader.Beside`): the repository's in the
  editor and the CLI, `StreamingAssets/translations` in a desktop player. A content folder with another name, such as a
  test fixture, has none. The CLI's `--translations DIR` names another folder; `--preview` does not take it.
- **Players:** before every player build `ContentSync` copies `translations/` to `StreamingAssets/translations`, and
  `ContentManifest` lists the files in `manifest.json` and packs their texts into `bundle.json` beside the content's, so
  the web boot makes no extra request. `ContentManifest.Verify` checks the lists, the packed texts and the copy (byte for
  byte) against the source, and `ContentSync` then loads the copy as a player would and the source as the editor does
  (`ContentManifest.CheckLoads`), so a broken translation fails the build rather than the player's boot. `Copy` marks the
  translations folder it makes (`.contentmanifest-copy`) and refuses to replace one it did not make.
- **The folder:** a language folder holds only `ui.yaml`, `events.yaml` and `content.yaml`. Any other file or folder there,
  or a folder that is not a language code, is refused rather than skipped. Files at the top (this folder's `README.md`)
  are not languages, and hidden names (`.DS_Store`) are ignored.
- **The checks:** each language is parsed, must name itself (`CheckDeclaredLanguage`), may not translate the advisors or
  How to play unless it is Hebrew, translates only events that exist (an event a ruleset leaves out counts) and every
  option of each, and keys only fields that `Fields` offers. A read failure is reported with its file.
- **A stale translation:** when an event or a record changes so that a translation no longer matches it, the load is
  refused (owner, 1 November 2026: "Refuse + say how to fix (Recommended)"), so nothing stale ships. The error says how to
  unblock without a translator: update the entry, or delete it from the language's file, and that text shows in English.
  Not for `he/`, which must equal the inline Hebrew.
- **Hebrew:** a `translations/he/` that is present must match the inline Hebrew of the events and the content exactly:
  the same events, texts, options and review flags, and the same fields and flags, none missing and none extra.
  `he/ui.yaml` is not compared with `strings.he.yaml` yet; that comes with the UI move (step 4).
- **Files a player does not ship:** `map/localities_1948.yaml` is a tool input; players get its baked TSV. A key into it
  is checked where the file can be read (the editor, the CLI and the build) and skipped in a player.
- **The hash:** translations are display text, so they stay outside `Content.Hash`: saves, the goldens and replay hashes
  are the same with or without them.

The UI strings still come from `strings.en.yaml` and `strings.he.yaml`; which catalogue the UI shows is #1606's
(`GameRoot.Strings()`).

## The implementations and their tests

- `sim/Statecraft.Sim/Translations.cs`: keys, parsing, writing, `Resolve` and `InlineHebrew`.
- `sim/Statecraft.Sim/TranslationLoader.cs`: finds, lists and checks `translations/` ("Loading", above);
  `TranslationLoaderTests` covers it, and `GameInputsTests` (Unity EditMode) the copy, the bundle and the web boot.
- `sim/Statecraft.Sim/HebrewOverlay.cs`: puts a Hebrew set on the sim's typed `*He` properties. Unity reads the Hebrew of
  `map/localities_1948.yaml`, `map/settlements_1948.yaml` and `map/units_1948.yaml` itself
  (`HebrewOverlay.UnityReads`).
- `sim/Statecraft.Sim.Tests/translations_fixture/`: a small checkout with the edge cases and the bodies an
  implementation must write for it. `TranslationsTests` checks the sim's and `tools/tests/test_translate.py` checks
  `tools/translate.py`'s (`inline_hebrew()`), so the tool and the game write one format.

`TranslationsTests` also checks the real content:
- every Hebrew value writes and reads back byte for byte;
- every content key resolves to the one record whose Hebrew it is;
- the overlay holds exactly what the loader reads, and clearing the Hebrew and applying the set brings it back;
- every `*He` property of a content type is covered;
- no key names a retained field;
- the localities bake's Hebrew all comes from keyed fields.
