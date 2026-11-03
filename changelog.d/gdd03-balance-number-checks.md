### Fixed

- Behind the scenes: under GDD 0.3 only, the balance file now refuses bad numbers at load and names the row. Every
  number must be at least 0, except the signed housing condition changes; counts and turns are whole, fractions are
  from 0 to 1, and percentage shares are from 0 to 100. The current game (gdd-0.1) is unchanged.
