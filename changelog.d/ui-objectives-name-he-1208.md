### Fixed

- **The Objectives screen reads each objective's Hebrew name from `content/objectives.yaml` (#1208).** In Hebrew it shows
  `name_he`, otherwise `name`; the second copy of the names in the two strings files is gone, so a new objective needs no
  strings key. One wording changes on screen: the first objective now reads "היישוב החדש הראשון" (it was "יישוב חדש ראשון");
  the Hebrew is still an AI translation that waits for the native read on #564.
