- Refs #1716: in the gdd-0.3 synthetic fixtures, the month's work and course snapshot reflects this turn's orders. A job
  change made this turn is in it, the same job again changes nothing (state and hash), and a novice released from work
  this turn can start a course and is on the course, off work, in the same snapshot. Q43(b)/(c) (when a course end or
  a cancel releases people) were answered on 1 November 2026 (#1668 Q43(b)/(c), https://github.com/eyalgolan/statecraft-israel/pull/1668#issuecomment-6553884547);
  building that release is #1716's follow-up, so the fixture still refuses those cases as Deferred. Nothing in the game calls this, and legacy/gdd-0.1 behaviour and content are unchanged.
