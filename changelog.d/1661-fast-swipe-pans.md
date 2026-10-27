### Fixed

- A very fast one-finger swipe no longer selects a hex (#1661 item 4; owner's ruling of 26 October 2026). A finger is
  sampled once a frame, so a press, a long travel and a lift inside one input update left the press and the sample at
  the same point: the slop saw nothing move, however far the finger went, and the swipe came back as a tap at the
  release point. The press now also carries the finger's travel within the frame, so the slop sees it and the gesture
  is a drag. The mouse's right button is stepped from its action callbacks and reports no travel, so right-click
  ordering is unchanged.
