### Fixed
- Content loader: a map entry written with no value (`map_actions: { housing: }`, an empty `budget_owners` line) now
  fails with the loader's "empty entry" message naming the file and field, not a `NullReferenceException` (#2029).
