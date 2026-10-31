# Five civilian proto hooks: static coverage audit

Checked 28 October 2026 against main
[`457c270cbd019bd89340d6c73c383b1c3f6aac55`](https://github.com/eyalgolan/statecraft-israel/tree/457c270cbd019bd89340d6c73c383b1c3f6aac55).
Status: agent-checked source audit; a person's review is pending. Refs [#1955](https://github.com/eyalgolan/statecraft-israel/issues/1955),
the five-figure slice [claimed here](https://github.com/eyalgolan/statecraft-israel/issues/1955#issuecomment-6475598371).

## The five hooks

These are authored prototype references, not observations from a running board. The four `MaabaraCamp.People` entries
are passed to `PlaceByHeight` by its [placement loop][people-place]; `KibbutzNegev.Woman` has its own [placement call][woman-place].
Each call specifies `StandIn.Figure`.

| Proto ID | Definition | Collection catalog member after removing `proto:` | Current Collection alias |
|---|---|---|---|
| `proto:figure-elder-immigrant-v1` | [`MaabaraCamp.People[0]`][people] | Present | Absent |
| `proto:figure-immigrant-woman-v1` | [`MaabaraCamp.People[1]`][people] | Present | Absent |
| `proto:figure-immigrant-child-v1` | [`MaabaraCamp.People[2]`][people] | Present | Absent |
| `proto:figure-yemenite-immigrant-v1` | [`MaabaraCamp.People[3]`][people] | Present | Absent |
| `proto:figure-kibbutz-woman-v1` | [`KibbutzNegev.Woman`][woman] | Present | Absent |

All five corresponding IDs are in [`CollectionCatalog`'s People group][catalog]. None is in
[`HexModels.FigureAliases`][aliases], and none matches the [`HexProps` list][props]. Catalog membership alone does not
connect a `proto:` hook: [`CollectionAlias`][alias-route] recognizes the existing town and figure mappings, without a
general rule that removes `proto:` and looks up a catalog member. No new mapping or detail choice is approved here.

The catalog's [prefab-path rule][prefab-path] names `Assets/Art/Collection/<id>/<id>-stream.prefab` for these members.
The pinned tree tracks each of those five prefabs. That establishes source paths, not a successful bundle build,
download, licence review or player load. Existing files and mappings are unchanged.

## Conditional fallback path

1. [`PlaceByHeight` and `Place`][placement] can reject a piece for bounds, water or slope before adding a placement.
   A source reference therefore does not prove that the figure is drawn. `MaabaraCamp` also [says it is not placed on
   the 1948 board by a recipe][maabara-scope].
2. For an admitted piece in the default renderer path, [`HexCollectionFigures.Streams`][streams] does not recognize
   these five IDs: they have neither a Collection alias nor a `HexProps` entry. [`Emit`][emit] then uses the ordinary
   prefab lookup.
3. [`HexModels.Prefab` and `PathOf`][lookup] try `Resources.Load<GameObject>` at
   `SettlementPrototypes/<id without proto:>`. The pinned tracked tree contains no matching asset under any nested
   `Resources/SettlementPrototypes/` folder for these five names. This does not inspect ignored local files or a built player.
4. **If that lookup returns null**, [`Emit`][emit] uses [`StandInParts(Figure)`][figure-fallback], whose mesh is
   `Capsule.fbx`. This is the conditional code fallback, not evidence that a player displayed a capsule.

## What remains open

For each of these five hooks, the recorded gap is the absent Collection alias; the fallback above is retained.
The existing [adapter tests][adapter-tests] exercise the current alias table, failure and teardown. Reading those tests
does not establish coverage or a passing runtime result for these five unaliased hooks.

This audit changes no model, alias, controller, catalog, stream API, prototype, unit definition or Jerusalem variant.
It does not take the town-model work in #1288 or the animation and motion work in #1866/#1900. Other #1955 hooks remain
outside this slice. Any new exact alias or reuse mapping still needs its approval and implementation validation.

No Editor, EditMode, player, missing-bundle, board-cleanup, capture, clearance or load measurement was run for this audit.
The one-hex/board evidence and first-frame/load requirements of #1955 remain unfinished. The doc-only source comparison
does not close that issue.

## AI assistance

Codex compared the pinned source files and tracked paths and drafted this note. This is an agent's static read;
it supplies no personal declaration, human sign-off or visual acceptance.

[people]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Prototypes/MaabaraCamp.cs#L43-L47
[people-place]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Prototypes/MaabaraCamp.cs#L119-L125
[woman]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Prototypes/KibbutzNegev.cs#L43-L46
[woman-place]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Prototypes/KibbutzNegev.cs#L130-L132
[catalog]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs#L86-L98
[aliases]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L100-L115
[props]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexProps.cs#L25-L42
[alias-route]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L124-L141
[prefab-path]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs#L205-L210
[placement]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexTileContext.cs#L128-L168
[maabara-scope]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Prototypes/MaabaraCamp.cs#L6-L8
[streams]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexCollectionFigures.cs#L18-L28
[emit]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexBoardBuilder.cs#L433-L458
[lookup]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L173-L195
[figure-fallback]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L334-L353
[adapter-tests]: https://github.com/eyalgolan/statecraft-israel/blob/457c270cbd019bd89340d6c73c383b1c3f6aac55/unity/Assets/Tests/EditMode/HexCollectionFiguresTests.cs#L35-L77
