- Map editor (gdd-0.3 authoring envelope, Refs #1721 slice A): given the loaded content, the editor's diagnostics now
  check corridor segments, force anchors, settlement-control targets and objective bindings, in place of their "adapter
  unavailable" rows. The overlay's location moves apply to the targets and objective sites; corridors and anchors are
  checked against the map. A broken binding is a blocking row that names its record (for example `targets[<id>]`);
  nothing is moved or rerouted to fix it. Regions, fields, facilities and the base manifest stay unavailable, so
  canonical export stays refused.
