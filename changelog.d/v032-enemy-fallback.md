### Fixed

- An enemy token whose side figure fails to load (a missing or damaged Collection bundle) is now drawn anyway, on its
  plinth under its side's flag with the plain built-in rifleman, at the same anchor and figure count, and the failure is
  logged once per side (owner, 31 October 2026: "Yes, fallback in v0.3.2"; the only exception to #740's "no placeholder
  art"). Before, no enemy unit appeared for the whole session (seen on a CrazyGames-hosted v0.3.1). While a figure is
  still loading the token waits as before; loaded figures and Israeli tokens are unchanged.
