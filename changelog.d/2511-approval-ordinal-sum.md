### Changed

- Approval (Refs #2511): each voter group's weights are now summed in a fixed (ordinal key) order, so the result no
  longer rests on the order a dictionary happens to return them in. Shipped content gives the same numbers as before.
