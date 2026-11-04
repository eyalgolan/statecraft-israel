- Map editor (gdd-0.3 authoring envelope, Refs #1721 slice B): the envelope now carries visual and label offsets, each
  a location's axial hex plus whole metres east and north of the hex centre (Q59). An offset whose point leaves its own
  4.4 km hex gives a warning naming the row, which does not block export (Q58); the point is kept as written. Offsets
  inside their hex save and reload byte for byte.
