### Changed

- A shareable build now also waits for a person's check of a starting unit's field that was sourced after the unit's
  own check: each fact-checked row in `units_1948.yaml` records the day of that check (`checked_on`), and the release
  gate names the row and the field. Today that holds back the order targets of six brigades (#2722).
