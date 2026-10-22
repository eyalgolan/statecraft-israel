- The capture dialog, GDD Phase 6 PR 10 (#802): when a settlement the player has captured awaits a decision, a dialog in
  the decision card's place names it and lists the options the sim's `SettlementControl.CaptureOptions` offers, in the
  sim's order, each with a one-line plain description and the sim's preview of what it changes at once. Nothing is
  pre-selected, and there is no picture, sound or video. A choice queues `ResolveCapture` and takes effect at End turn;
  Hide or Escape leaves the decision waiting. Only Raid and withdraw is offered until Occupy and Deport are built. The
  dialog's wording waits for the owner's sensitivity review, and the Hebrew for a native speaker's read (#564). The
  shipped target list is still empty, so no game shows the dialog yet.
