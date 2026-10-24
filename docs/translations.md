# The translation contract

How a language other than English is stored in `translations/<lang>/` (#1618; owner, 24 October 2026: option (a), "verify
it doesnt cause any bugs, degregations or gaps"). English stays the source of record in `content/` and
`unity/Assets/UI/strings.en.yaml`. This page fixes the files, their keys and their order, so the tools, the sim and Unity
read and write them the same way.

**Status (step 1 of #1618):** the contract and its tests only. The game still reads Hebrew from the inline `*_he` fields
and `strings.he.yaml`; nothing has moved. `Translations.InlineHebrew` (sim) reads today's inline Hebrew as these files,
and the tests prove every value comes back unchanged. The tools' side (`tools/translate.py`, from #1604) joins the same
fixture check once #1604 has merged.

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
(`tools/borders_digitise.py`, `tools/control_digitise.py`). Whether they stay in place or get keys is the owner's
decision on #1618; until then `TranslationsTests` names them, and fails if any other non-YAML content file has Hebrew.

## Order and format

A file lists its entries in this order: the events by `from` date, then id; the UI strings in `strings.en.yaml`'s order,
with any others after them by key; the content fields by file (`content/*.yaml`, then `content/map/*.yaml`, each in
ordinal order) and then by their position in the file.

The body (everything after the header comment) is fixed byte for byte: block mappings, two-space indents, every
string double-quoted the way JSON writes it (`json.dumps(..., ensure_ascii=False)`), and a key plain only when it is
letters, digits, `_`, `.` and `-`. The header comment belongs to the tool that writes the file.

## What the parser checks, and what it leaves to the content rules

`Translations.Parse` checks the shape: the known fields only, keys of the right form, no key twice, every text a
non-empty string, `review` only `agent-checked`, and `sources` as a list of `{ url, checked }`. The rules about the
text itself (an event is all or none, placeholders as in the English, no stray `{` or `}`, Hebrew letters, no direction
marks, the UI's approved wording) are the content rules. They run on the text where it is used, as they do today.

## The implementations and their tests

- `sim/Statecraft.Sim/Translations.cs`: keys, parsing, writing, `Resolve` and `InlineHebrew`.
- `sim/Statecraft.Sim/HebrewOverlay.cs`: puts a Hebrew set on the sim's typed `*He` properties. Unity reads the Hebrew of
  `map/localities_1948.yaml`, `map/settlements_1948.yaml` and `map/units_1948.yaml` itself
  (`HebrewOverlay.UnityReads`).
- `sim/Statecraft.Sim.Tests/translations_fixture/`: a small checkout with the edge cases and the bodies an
  implementation must write for it. `TranslationsTests` checks the sim's; `tools/translate.py` (#1604) gets the same
  check, so the tool and the game write one format.

`TranslationsTests` also checks the real content:
- every Hebrew value writes and reads back byte for byte;
- every content key resolves to the one record whose Hebrew it is;
- the overlay holds exactly what the loader reads, and clearing the Hebrew and applying the set brings it back;
- every `*He` property of a content type is covered;
- no key names a retained field;
- the localities bake's Hebrew all comes from keyed fields.
