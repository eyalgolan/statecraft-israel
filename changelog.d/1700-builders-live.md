### Added

- GDD 0.3 builders on city projects in the sim (Refs #1700, slice A): under the gdd-0.3 ruleset only, a city project
  being built needs one builder, a person of its city without a job, to make progress, at the kind's existing turns.
  Starting a project takes a free person as its builder automatically when the city has one; otherwise the project waits
  until the player assigns one with the new `build` role. A builder holds no job, cannot be trained or put to work in a
  job or an arms workshop, and is freed when the project is built or cancelled. Extra Money never speeds a project. The Why
  screen says when a builder starts and why a project waits; its Hebrew lines are agent drafts awaiting a native read.
  The current game (gdd-0.1) is unchanged. The per-city start limit and the hex panel come later.
