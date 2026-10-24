- The translation contract for `translations/<lang>/` (#1618, step 1): the sim can read, write and resolve the three
  files (`Translations`), and put a Hebrew set on its typed records (`HebrewOverlay`). Nothing moves yet: the game still
  reads the inline Hebrew, and tests prove every Hebrew value comes back unchanged through the contract
  (`docs/translations.md`).
