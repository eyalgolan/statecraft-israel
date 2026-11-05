### Fixed

- gdd-0.3 basic training (synthetic fixture, no game caller yet): a military camp that stops operating during a course no
  longer stops the sim with a "Deferred" error. Its next course turn cancels every course at that camp at once, as a
  cancel does: the people stay in place as civilians without a job, Arms the course issued go back to the spare pool,
  Arms a recruit already carried stay with them, and Money and debt are not refunded (27M-9, R-TRAIN-002; Fixes #2463).
  gdd-0.1 and the goldens are unchanged.
