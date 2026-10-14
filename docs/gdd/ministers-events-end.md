# Ministers, events and the end of the MVP

## Player story

As a player of the MVP, I want the game to run its events and my chosen ministers through to an end screen on 1 January 1952 [S2], so that a full game resolves into a score and its historical completion cards (GDD "MVP scope" and "Historical events on objective completion" [S1]).

## Rules

- The MVP runs 44 turns (monthly), from 15 May 1948 to 1 January 1952 (D-B1 [S2]). The GDD text still says "approximately 48 turns" [S1]; the owner settled 44 turns [S2].
- Each completed objective triggers a historical event card with an authentic image, a short historical account and the actual date of the event, and the card compares the player's completion date with the historical date (GDD [S1]).
- A completed objective scores 100 points (D-B6 [S2]). Finishing the war earlier than its historical end adds 5 points per month, capped at 60 points (D-B2 [S2]).
- The war-survival objective's historical reference is the 1949 armistice: Egypt signed on 24 February 1949, Lebanon on 23 March 1949, Jordan on 3 April 1949 and Syria on 20 July 1949 [S3]. Which of these dates is the card's reference date is an owner decision.
- Ministers reuse the existing Cabinet system [S2]; minister selection is a planned mechanic the GDD leaves open [S1].

## Data touched

- `objectives.yaml`: completion rules and card references.
- `events/`: the MVP's event set (#135 research).
- `content/governments.yaml`, `offices.yaml`, `people.yaml`: minister candidates.
- The end screen and score display (Unity).

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.

## Out of scope

- Event frequency, windows and caps (D-H1), event effect targets (D-H2) and minister roles and effects (D-H3): all open.
- The objectives panel UI and the score's presentation in Phase 1.
- The exact completion conditions of each objective (GDD "MVP completion rules" [S1]): open.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] The 1949 armistice layer, content/map/armistice_hexes_1949.yaml (signing dates per agreement)

## Open questions

- Which armistice date is the war-survival card's reference date: 24 February 1949 (first) or 20 July 1949 (last) [S3].
- Minister roles, candidates and effects (D-H3); event frequency and caps (D-H1); event effects on units, cities and projects (D-H2).
- Bonus amounts, the timing formula's remaining parts, defeat conditions and whether all three objectives are required to finish (GDD "MVP completion rules" [S1]).
- The GDD's "approximately 48 turns" [S1] against the decided 44 turns [S2]: a one-line GDD fix for the owner.
