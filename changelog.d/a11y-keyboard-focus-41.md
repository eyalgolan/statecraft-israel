### Fixed

- Keyboard: the Cabinet's seat and candidate lists no longer put their scroll bars in the Tab order, so Tab goes from
  the last row straight to the next control instead of stopping on a bar that cannot be used from the keyboard.
- Keyboard: a city or site panel press that leaves its own button gone or disabled no longer drops the focus. The
  Assign stepper's minus disables itself at zero, Build disables every Build in the part once one group is ordered,
  and Stop and a project's Cancel leave only a state line; in each case the focus now goes to the nearest control that
  can take it, so the following Tab carries on instead of starting again at the first screen.
