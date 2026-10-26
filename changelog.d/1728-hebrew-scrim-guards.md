### Changed

- Two EditMode guards for the Hebrew decision-card path (#1728): answering one card, and answering two in sequence,
  must leave the backdrop gone, `event-open` off the root, and the info panel's area pickable. The existing coverage
  only collapsed the card with Hide, and only in English. No player code changes.
