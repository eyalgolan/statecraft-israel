- The HUD holds at the Settings screen's 125% and 150% UI scales (#40): below the smallest window the 100% layout is
  tested at (HudPresenter's new hud-tight, set from the panel's logical size; small phone windows reach it too) the
  bar's rows, the icon column and the panels tighten; on the tight three-row window the icon column becomes a row of the
  phone's 60 px discs under the bar (at least 24 px on screen down to 375x667) and a screen takes the window like a
  mobile modal, with the card's backdrop leaving the row's discs clear. The decision card starts right of the tight
  column; the info panel is a scrolling panel capped clear of the bar and the column, and on a phone at 125% or 150% it
  stands above the minimap at the window's width; the Cabinet's side column slims so
  the seats list keeps its room; and the small text keeps its 12 px on-screen floor at every size. The seven layout
  suites that failed at 125%/150% now run at all three scales.
- At 150% on short windows the Cabinet scroll shows seat cards before the house and legend; on phones at 150% the
  half circle fits its column. Desktop half circles and the 100%/125% Cabinet order keep their existing layout.
- The decision card's body scrolls as one (#1853): the text and the options with their effect lines sit in one scroll
  between the pinned head and the pinned foot, so Hide stays inside the card at 150%. A card that fits shows no scroll
  bar, Tab onto an option scrolls it into view, and a new card opens at the top of its text.
