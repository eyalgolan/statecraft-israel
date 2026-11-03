### Changed

- `gdd-0.3` fields, in test fixtures only: each of the 20 authored starting fields takes one farmer who lives in its
  region, only while it is Israeli-held, and yields 2, 3 or 5 times `field_food_per_turn` Food a turn by the farmer's
  tier in the month's snapshot; an unworked field yields nothing (owner, 3 November 2026, "One farmer per field
  (Recommended)", #1694). Nothing in a game changes yet: `gdd-0.1` keeps its fields with no workers.
- `gdd-0.3` job fixture: a free civilian no longer needs completed reception turns to take a job, since the MVP has no
  reception-bed layer (owner, 1 November 2026).
- The `field.slots` note in `content/gdd03-balance.yaml` now records the ruling, so the content hash changes and older
  saves load with the "content changed" notice.
