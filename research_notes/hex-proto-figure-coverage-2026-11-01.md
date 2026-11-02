# Hex-tile proto hooks for five civilian figures: what draws them

Read 1 November 2026 against main
[`a256c49ce`](https://github.com/eyalgolan/statecraft-israel/tree/a256c49ce7ac0db6516c31bbe8538ba4cf94b315); every
link below is pinned to that commit. A static read of the source and the tracked tree by an agent; a person's review
is pending. Refs [#1955](https://github.com/eyalgolan/statecraft-israel/issues/1955), its slice of five civilian
figures. This note re-does, with our own read, the audit Avl proposed in #1958.

## The five hooks

Two hex-tile prototypes ask for these figures, each with `StandIn.Figure` as the fallback:

| Hook on the hex tile | Where it is asked for | Collection model of the same name | Tracked `-stream` prefab | `HexModels.FigureAliases` | `HexProps.Ids` |
|---|---|---|---|---|---|
| `proto:figure-elder-immigrant-v1` | [`MaabaraCamp.People`][people], [placement loop][people-place] | in the People group | yes | no | no |
| `proto:figure-immigrant-woman-v1` | same | in the People group | yes | no | no |
| `proto:figure-immigrant-child-v1` | same | in the People group | yes | no | no |
| `proto:figure-yemenite-immigrant-v1` | same | in the People group | yes | no | no |
| `proto:figure-kibbutz-woman-v1` | [`KibbutzNegev.Woman`][woman], [placement][woman-place] | in the People group | yes | no | no |

- **Collection model.** Each name without `proto:` is in [`CollectionCatalog`'s People group][catalog]. The
  [prefab-path rule][prefab-path] gives `Assets/Art/Collection/<id>/<id>-stream.prefab` for them, and `git ls-files`
  lists all five of those prefabs (with `-near` and `-far` beside them).
- **No hex route to it.** [`CollectionAlias`][alias-route] streams a hex id only if it is a town id or is in
  [`FigureAliases`][aliases]; it does not strip `proto:` and look the rest up in the catalog. None of the five is in
  `FigureAliases` (its ten rows are the C-series figures, the sheep and the donkey), and none is in
  [`HexProps.Ids`][props], which lists only `tiles:` props. So [`HexCollectionFigures.Streams`][streams] is false for all
  five.
- **The same models are used elsewhere.** The main map's [`CivilianPeople`][civilian] layer already places all five
  Collection models (immigrants at the transit camps, the Yemenite immigrant in the Magic Carpet window, the kibbutz woman
  at every kibbutz). That layer asks the Collection by model id; it does not touch the hex-tile prototypes. The gap in
  this note is only the hex-tile path.

## What a hex board draws for them

1. [`PlaceByHeight` and `Place`][placement] can refuse a piece (outside the hex, on water, or too steep) before it is
   added, so a hook in the source is not proof that a figure is placed. `MaabaraCamp` is
   [not placed on the 1948 board by any recipe][maabara-scope]; it is a painter tile for 1950 on.
2. In the [board's emit loop][emit], a placed piece whose id does not stream goes to [`HexModels.Prefab`][lookup]. For
   these ids that tries the editor-only Collection alias, hex prop and house lookups (all miss), then
   `Resources.Load<GameObject>("SettlementPrototypes/<name>")` ([`PathOf`][lookup]).
3. No tracked file has `SettlementPrototypes` in its path, and `.gitignore` ignores
   `unity/Assets/Art/Settlements/Prototype/Resources/`. So a checkout of the repository has no such prefab; a machine
   with local, untracked files may. This read did not inspect local files or a built player.
4. When that load returns null, the loop draws [`StandInParts(StandIn.Figure)`][figure-fallback]: Unity's built-in
   `Capsule.fbx`. That is what the code falls back to, not an observation of a running board.

## What remains open

For each of the five hooks, both routes to the shipped Collection model are missing: there is no `FigureAliases` row
and no `HexProps` entry, so the hex tile keeps the Resources-or-capsule fallback above.

Closing that needs a decision this note does not make: an exact alias per figure (and at which level, near or far), or
another reviewed mapping. Each needs approval and then #1955's own checks: streaming after the first frame, a missing
bundle, board cleanup, one-hex and board captures, clearance and load measurements. The
[adapter tests][adapter-tests] exercise the current alias table, failure and teardown; they say nothing about these five
ids. No Editor, EditMode, player or capture run was made for this note. Nothing outside this note changes; the town
models (#1288) and motion work (#1866, #1900) are not touched.

[people]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Prototypes/MaabaraCamp.cs#L43-L47
[people-place]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Prototypes/MaabaraCamp.cs#L119-L125
[maabara-scope]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Prototypes/MaabaraCamp.cs#L6-L8
[woman]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Prototypes/KibbutzNegev.cs#L43-L46
[woman-place]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Prototypes/KibbutzNegev.cs#L131
[catalog]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs#L89-L101
[prefab-path]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/Collection/CollectionCatalog.cs#L212-L216
[aliases]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L100-L115
[alias-route]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L143-L160
[lookup]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L192-L214
[figure-fallback]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexModels.cs#L357-L373
[props]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexProps.cs#L25-L37
[streams]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexCollectionFigures.cs#L25-L28
[emit]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexBoardBuilder.cs#L433-L460
[placement]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/HexTiles/Core/HexTileContext.cs#L128-L168
[civilian]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Scripts/Map/Collection/CivilianPeople.cs#L13-L57
[adapter-tests]: https://github.com/eyalgolan/statecraft-israel/blob/a256c49ce7ac0db6516c31bbe8538ba4cf94b315/unity/Assets/Tests/EditMode/HexCollectionFiguresTests.cs#L35-L78
