### Fixed

- End turn is lighter again: the automated player's staging for operation cards searched the whole map once for every
  candidate hex (over 1,000 searches a game) and now searches once per unit. The moves it picks are the same.
