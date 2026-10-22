- Answering a capture, GDD Phase 6 PR 5 (#797; R-CTRL-007 to R-CTRL-010): a new `ResolveCapture(settlement, outcome)`
  order, one kind for every outcome at `attention_cost.resolve_capture` 0 (a design value proposed on #797). The sim's
  `SettlementControl.CaptureOptions` lists Raid and withdraw, Occupy and Deport in that fixed order, with nothing
  pre-selected and a preview worked out on a copy of the state. Occupy and Deport are refused as not built yet. Raid
  moves the player's units off the place to the first free neighbouring hex and puts it back under enemy control, with
  its residents unchanged. `HistoricalAI` and the reference player answer every capture with Raid, and a capture still
  waiting at the game's end counts as Raid. No golden moves, and none moved when #793 filled the target list with six
  rows.
