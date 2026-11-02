### Fixed

- Deactivating a transit camp retires its tent models without reparenting during the disable callback. Redrawing the inactive camp preserves streamed model meshes while releasing its own geometry and lease.
