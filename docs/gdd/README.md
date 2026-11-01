# GDD feature sections

The [GDD](../GDD.md) is the design of record. Map-production priorities and historical location records are in the
separate [Locations specification](../locations.md); the authoring workflow is in [Map editor](map-editor.md).
The terrain/tile/hex-type catalogue is frozen at the eight biomes for the MVP (owner, 23 October 2026, #1512), and the
approved biome-based movement framework is retained in [Military and combat](military-combat.md).
[The GDD](../GDD.md) owns scope/authority. Keep the existing one-file-per-feature structure. Each mechanic has one semantic owner and [balance.md](balance.md) is the sole numerical catalogue; elsewhere reference its keys.

For map work, read the location roster together with [Cities and absorption](cities.md), [Build yishuv](build-yishuv.md)
and [Corridors](corridors.md). [Settlement control](settlement-control.md) defines which places may become control targets;
a location's map priority does not add a city economy, replace a founding objective or select such a target. Existing
supported places remain supported even when additional authoring is optional.

| Owner | Responsibility |
| --- | --- |
| [Balance](balance.md) | Numerical values, provenance, start inventory |
| [Training](training.md) | Identity, retained qualifications/courses and conversion/demobilisation |
| [Cities](cities.md) | Per-city work, housing and worker-free projects |
| [Resources](cutover-resource-layer.md) | Retained pools, borrowing, imports and shortages |
| [Military combat](military-combat.md) | Movement, veterancy (combat experience under `gdd-0.3`, time served under `gdd-0.1`), forts, AI, truces and prisoners |
| [Settlement control](settlement-control.md) | Individual settlement control, annexation and civilian protections |
| [Build yishuv](build-yishuv.md) | Designated-site founding under fixed-time work rules |
| [Corridors](corridors.md) | Segment work/completion |
| [Objectives](objective-card.md) | Membership, cards and scoring |
| [Monthly turns](monthly-turns.md) | Calendar/windows |
| [Loop](loop-switch.md) | Sequence, determinism, saves/migration |
| [Government/events/end](ministers-events-end.md) | Integration, indicators and ending |
| [Map editor](map-editor.md) | Fixed camera and authored-map persistence/validation |

One file per feature in this folder, copied from [TEMPLATE.md](TEMPLATE.md). The shape (player story, rules, data touched,
done-when, out of scope, sources, open questions) is a starting point; the maintainers and the GDD writers own it. Change the
template and the linter together.
[Locations](../locations.md) owns names/facilities, [routes](../routes.md) geometry and [units](../units.md) artwork/placement evidence. [one-city.md](one-city.md)/[veteran-pioneer.md](veteran-pioneer.md) are compatibility handoffs; [example-turn-grid.md](example-turn-grid.md) is illustrative.

Phase-plan paths link to their full immutable originals and current owners rather than retaining active duplicate rule tables. [Existing rule IDs](../rules/mvp.md) remain a migration index; passing their tests does not establish revised-design implementation.

## Writing a feature section

Follow [TEMPLATE.md](TEMPLATE.md); change template/linter together. Keep all required sections and meaningful completion checks. Put open principles only in [the root register](../GDD.md#deferred-decisions), including the requested terrain placeholder.

`uv run scripts/oss/gdd_lint.py` fails a file when:

- a section is missing or empty, or "Done when" has no bullet;
- TBD, TODO, FIXME or ??? is left in the text, or a `<...>` placeholder from the template;
- a number in Rules or Done when has no unit (`3 turns`, `5%`, `IL 2m`; add `(unitless)` to a line where it truly has none);
- a line holding a year or a date has no `[S<n>]` source, or cites one that "Sources" does not define with a link or a path;
- a source's link has no checked date (`checked 2026-10-12`), as `docs/CONTENT_POLICY.md` asks.

HTML comments are not checked, so the template's comments can stay.

[example-turn-grid.md](example-turn-grid.md) passes. `TEMPLATE.md` and this README are not linted.
It runs in CI as one of the policy checker tests (`pytest scripts/oss/tests`) and in `scripts/check.sh`.
