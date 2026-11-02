### Added

- GDD 0.3 arms workshops in the sim (Refs #1701): under the gdd-0.3 ruleset only, a city can build an arms workshop (IL 15m,
  3 turns), staff it with one person without a job, and each staffed workshop pays for a cycle at the monthly production
  step (IL 5m for an ordinary worker, borrowing a short Treasury but never past the debt ceiling) and makes 1 Arms every 3
  staffed months, starting the next cycle by itself. The turn-report panel names the workshops' Arms and ceiling
  pauses, one line a city for each, with a count for two or more. Staffing can take its worker from a city's automatic jobs, and a staffed worker cannot be sent to training.
  The current game (gdd-0.1) is unchanged, though `projects.yaml` gained
  the gdd-0.3 row, so older saves show the "different game content" notice. The Hebrew workshop name and turn-report lines
  are agent drafts awaiting a native read. The hex panel order and the b21 map model come later.
