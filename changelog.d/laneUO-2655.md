### Changed
- The nightly's EditMode run leaves out the two tests that compare against gitignored local art
  (`HexModelMeshTests.TownAliasesFaceAsTheirSandboxPrefabs`, `HexUnitTests.CollectionFiguresFaceAsTheirProtoFigures`,
  now in a `LocalOnly` category), so its skip count is only the `[Explicit]` tests (Refs #2655). A local run still
  runs them.
