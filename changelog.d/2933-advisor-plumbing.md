- Advisors: groundwork for the new-player course (#2933): a `turn_at_least` condition, `lapse_after` and `fallback` fields on
  a message, and a `course_open` signal set when the opening chain opens by itself. No message uses them yet, so nothing
  changes on screen.
- Advisors: at help level off, experienced or warnings the opening chain no longer opens by itself on a first game. It
  counts as seen, so it does not come later, and the new-player course does not open. The encyclopedia still reopens it.
