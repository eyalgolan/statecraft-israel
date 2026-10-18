# GDD feature sections

The [GDD](../GDD.md) is the design of record. Map-production priorities and historical location records are in the
separate [Locations specification](../locations.md); the authoring workflow is in [Map editor](map-editor.md).
The terrain/tile/hex-type catalogue remains **TBD in the GDD**, while the approved biome-based movement framework
is retained in [Military and combat](military-combat.md).

For map work, read the location roster together with [Cities and absorption](cities.md), [Build yishuv](build-yishuv.md)
and [Corridors](corridors.md). [Settlement control](settlement-control.md) defines which places may become control targets;
a location's map priority does not add a city economy, replace a founding objective or select such a target. Existing
supported places remain supported even when additional authoring is optional.

One file per feature in this folder, copied from [TEMPLATE.md](TEMPLATE.md). The shape (player story, rules, data touched,
done-when, out of scope, sources, open questions) is a starting point; the maintainers and the GDD writers own it. Change the
template and the linter together.

`uv run scripts/oss/gdd_lint.py` fails a file when:

- a section is missing or empty, or "Done when" has no bullet;
- TBD, TODO, FIXME or ??? is left in the text, or a `<...>` placeholder from the template;
- a number in Rules or Done when has no unit (`3 turns`, `5%`, `IL 2m`; add `(unitless)` to a line where it truly has none);
- a line holding a year or a date has no `[S<n>]` source, or cites one that "Sources" does not define with a link or a path;
- a source's link has no checked date (`checked 2026-10-12`), as `docs/CONTENT_POLICY.md` asks.

HTML comments are not checked, so the template's comments can stay.

[example-turn-grid.md](example-turn-grid.md) passes. `TEMPLATE.md` and this README are not linted.
It runs in CI as one of the policy checker tests (`pytest scripts/oss/tests`) and in `scripts/check.sh`.
