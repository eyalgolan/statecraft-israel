- Map editor (gdd-0.3 authoring envelope, Refs #1721 slice A): given the loaded content, the editor's diagnostics now
  check corridor segments, force anchors, settlement-control targets and objective bindings against the map and the
  overlay's location moves, in place of their "adapter unavailable" rows. A broken binding is a blocking row that names
  its record (for example `targets[<id>]`); nothing is moved or rerouted to fix it. Regions, fields, facilities and the
  base manifest stay unavailable, so canonical export stays refused.
