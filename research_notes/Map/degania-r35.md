# Degania Alef's R35: the sources of the placed tank

Sources for Tal Heuberger's Syrian Renault R35 at Degania Alef (`SpecialBuildingModels`, key `degania_r35`; owner, 27 October 2026, "Degania landmark (Recommended)").

## The tank stopped at Degania Alef, 20 May 1948

- Jewish Virtual Library, credited to the IDF, already cited by `content/events/degania_defence.yaml` (checked 2026-10-24): the dawn attack on Degania A and Degania B on 20 May, and "an R35 that broke through the fence and was stopped (it stands there today)".
- English Wikipedia, "Renault R35", section on Syria and Lebanon (https://en.wikipedia.org/wiki/Renault_R35, checked 2026-10-28):
  - "The R35 saw combat in Syrian hands when five R35s took part in an unsuccessful Syrian Army attack on the Jewish kibbutz Degania Alef in the Galilee on 20 May 1948, during the Battles of the Kinarot Valley."
  - "One of the disabled R35s remains near the kibbutz today as a memorial of the 1947–1949 Palestine war."
  - Photo caption: "A knocked-out Syrian R-35 at Degania Alef".

## Its length

- English Wikipedia, "Renault R35", infobox, Length: "4.02 m (13 ft 2 in)" (checked 2026-10-28). This is the infobox's overall length of the tank; the game sizes the model by its long side seen from above (`SpecialBuildingModels.RealLengthM`), at the one world scale of the placed models.

## Its point

The model's centre stands on Degania Alef's drawn kit bounds widened by one model-clearance on each side (one clearance beyond a side, up to the square root of two clearances beyond a corner), on the side facing the nearest mapped transport segment. The bounds come from the kit meshes, so they are the same with the kits hidden. If no route-facing spot is available (no kit footprint or drawn houses, no mapped route, or every candidate taken), it logs a warning and uses the stable seeded search around the settlement anchor (`content/map/settlements_1948.yaml`, `degania_alef`, lon 35.5747, lat 32.7081, from en.wiki Degania Alef). It is a schematic placement at the kibbutz, not a surveyed position of the tank or of the fence.
