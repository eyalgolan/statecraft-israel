### Fixed

- An exchange card no longer fires for a side whose stored count of prisoners or held soldiers is 0.
- The loader now refuses `mvp.cover_months` above 120 and `mvp.field_food_per_turn` above 1,000, so a content edit can't
  wrap the Food stock.
