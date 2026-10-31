- Translations travel with the content (#1618, step 2): the sim, the CLI (`--translations DIR`) and every player read
  `translations/`, checked against the content it translates, and the web boot gets it in the same bundle as the
  content, with no extra request. A `translations/he/` must match the inline Hebrew, which is still what the game shows.
  Translations stay outside the content hash, so saves and goldens do not move (`docs/translations.md`, "Loading").
