- City projects in the info panel: on a hex a city can build on, the panel names the city that builds (the player picks
  when several are in reach) and offers Build for each `projects.yaml` kind with its cost and turns, or shows the sim's
  own refusal in English or Hebrew; a project there shows its turns left and Cancel, or its finishing day. The turns are
  the sim's forecast at the kind's minister multiplier (`Projects.CityTurnsToBuild`). The sim adds `Projects.CitiesFor`
  (cities in reach, by the one range check) and `Projects.CheckCityReason` (structured refusals, a locked kind included;
  `CheckCity`'s English and order unchanged), and the kinds gain draft Hebrew names for native review (#564; refs #1122).
- On a city's own hex, on a stacked bar or a short window, the project part folds behind one "City projects (N)"
  toggle that opens and closes the list, so the panel stays clear of the left column (owner ruling on #1150).
