### Fixed

- The `Assets/Editor/Generated1948Review` folder (#1754) had the same GUID as `Generated1948VehicleReview` (#1378), so Unity reported a GUID conflict and rewrote the `.meta` on every import. It now has its own GUID. Nothing refers to the old one, and players see no change.
