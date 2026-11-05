### Fixed
- The content loader now refuses a site whose kinds hold `yishuv` together with `military_camp` or `hakhshara`,
  naming `sites.yaml` and the row's `kinds` (Fixes #2599). Such a site would share one built record, so building the
  yishuv would also count as a built camp for its city. No shipped site has both kinds, so the game is unchanged.
