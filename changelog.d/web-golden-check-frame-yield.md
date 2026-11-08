### Fixed

- Behind the scenes: the web build's golden check now waits a frame before each golden, so the browser can free each
  run's memory. The 8 November nightly's check ran out of memory at the 2 GB cap because all 11 goldens played in one
  frame.
