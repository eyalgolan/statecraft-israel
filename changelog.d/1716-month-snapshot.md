- Refs #1716: in the gdd-0.3 synthetic fixtures, the month's work and course snapshot reflects this turn's orders. A job
  change made this turn is in it, the same job again changes nothing (state and hash), and a novice released from work
  this turn can start a course and is on the course, off work, in the same snapshot. When a course end releases people
  stays open; nothing in the game calls this, and legacy/gdd-0.1 behaviour and content are unchanged.
