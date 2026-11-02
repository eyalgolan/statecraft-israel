- Translations travel with the content (#1618, step 2; designed by Orel Moshe Weinstock, @moshewe, in #1884): the sim,
  the CLI (`--translations DIR`) and every player load `translations/` and check each language against the content it
  translates, and the web boot gets them in the same `bundle.json` as the content, with no extra request. A
  `translations/he/` must match the inline Hebrew of the events and content, which is still what the game shows.
  Translations stay outside the content hash, so saves and goldens do not move (`docs/translations.md`, "Loading").
