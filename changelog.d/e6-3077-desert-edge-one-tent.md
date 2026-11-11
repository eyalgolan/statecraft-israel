### Fixed
- The hex tiles' desert edge draws each Bedouin tent once (Refs #3077): each tent was a row of two or three boxes, and
  since Tal Heuberger's tent model arrived each box drew a whole tent, so a camp of three tents showed up to nine
  overlapping ones. Each tent is now one tent model as long as the row was. Where the tile draws no models it is still
  the row of dark boxes, and while the props stream in (or if they fail to load) each tent is one box as long as the
  tent, like other props. By arithmetic from the model's triangle count, the tile stays inside its 25,000-triangle
  budget.
