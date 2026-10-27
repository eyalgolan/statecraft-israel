### Fixed

- The Hebrew city panel no longer shows "a winery" and "vineyards" in English inside a Hebrew line (#1781). The
  `winery` project kind gets a `name_he` like its three siblings, and the resource it requires is named by a
  `city.resource.<id>` UI string in both languages. The resource's name lives in the strings rather than in
  `content/`, because a resource is a record nested inside a city and the translation contract binds only a
  `<file>#<list>.<id>.<field>` key (`HebrewOverlay.Bindings`). Both wordings are `agent-checked`, for the native read
  under #564.
