- GDD 0.3 starting population (Refs #1725, Refs #1706): battles now run on the inactive dark start's Historical ledger.
  Each unit of strength one of the player's formations loses removes one of its soldier records as a death, the lowest
  id first, noted with the battle's date and hex; a trained formation is covered too. The battle itself, its draw and
  its report are the aggregate start's, and a surrender's losses are written before its soldiers are captured. A
  surrender's report counts the soldiers actually captured, and a formation's records beyond its strength go home as
  Send home sends them. No new-game wiring, so there is no change in the game.
