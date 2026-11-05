### Fixed

- A hand-edited translation of the interface is now checked when the content loads in the editor, on the command line
  and before every player build, as the translation editor already checks a save (Fixes #2598). A string the English
  table does not have, such as a misspelt key, or a string whose placeholders differ from the English one, is refused
  with the file and the key, so a typo can no longer leave a line in English unnoticed or drop a number from it. Hebrew
  is unchanged.
