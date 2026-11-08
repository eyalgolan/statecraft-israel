- A military operation card can now order sourced attacks as well as moves (`unit_orders` rows with `order: attack`),
  fought through the player's own attack under both rulesets; a refused action leaves the later ones to run, a
  player's order to a unit in an operation cancels that operation's remaining orders, and the sim lists every action an
  operation takes for the map and turn report to show (#2544, sim side; no card has an attack yet).
