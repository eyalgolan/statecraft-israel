# translations_fixture

A small checkout for the translation contract (#1618, [docs/translations.md](../../../docs/translations.md)): `content/`
and `ui/` hold inline Hebrew with the cases the contract must handle, and `expected/he/` holds the bodies (the files
without their header comment) an implementation must write for it. `TranslationsTests` (sim) checks the sim writes
exactly these bytes; once #1604 has merged, `tools/tests/test_translate.py` checks `tools/translate.py` does too, so the
tool and the game stay one contract.

The cases: events ordered by date, then id (`alpha` before `vote` on the same day; `quiet` has no Hebrew); a review flag
and none; text with `'`, `"` and `\`; an office's feminine title (`title_f`) beside its retained sources and date window;
a person whose `name_he` source sits in `biography_sources` (a list, so not a field); a record keyed by a mapping key
(`regions.north`); a list item with no id (keyed by its index); an English value that is not text (skipped); a Hebrew
field with no English (skipped); UI strings in strings.en.yaml's order with one only in Hebrew last, and a separator
whose spaces count.
