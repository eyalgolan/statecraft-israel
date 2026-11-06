### Changed

- GDD 0.3: a staffed field can now settle its month's Food into the national stock: 2, 3 or 5 times
  `field_food_per_turn` by its farmer's tier, once per field and in field order, with a Why line for each. A field the
  enemy holds adds nothing but keeps its farmer and assignment, and once retaken it produces again from the next month
  with no new order (Fixes #2471). No game uses it yet, so play is unchanged.
