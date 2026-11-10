### Fixed
- The hex tiles' desert edge draws each Bedouin tent once (Refs #3077): each tent was a row of two or three boxes, and
  since Tal Heuberger's tent model arrived each box drew a whole tent, so a camp of three tents showed up to nine
  overlapping ones. Each tent is now one tent model as long as the row was; the row of dark boxes stays only where the
  model does not draw. The tile stays inside its 25,000-triangle budget.
