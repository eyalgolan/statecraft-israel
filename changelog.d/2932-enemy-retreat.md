### Fixed

- An enemy unit beaten back in combat no longer retreats onto an empty Israeli town or city. It obeys the same limit
  as an enemy move, and with no other hex to go to it surrenders. Before, under gdd-0.1 such a retreat onto a city
  ended the game at once with "The enemy holds a city". A save in which that retreat happened replays differently
  (Fixes #2932).
