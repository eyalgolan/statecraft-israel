# Alternate timelines: follow-up cards for the roads not taken

## Player story

As a player who picks an option that did not happen, I want the game to bring the dilemmas contemporaries expected from that road, bend the later historical cards to my course and fold back into the record at the real outside events, so that my choice matters and the history stays sourced (the owner's request and answers [S1][S2]; the plan [S3]).

## Rules

- The mechanism runs in Historical mode, the only startable mode; the player's explicit answer is never overridden there (`Q-ALT-1` [S2]).
- An event has diverged when its chosen option, picked by the player or delegated to a minister, is not its historical option. Nothing new is stored: it is read from the chosen options (`Q-ALT-2`, `Q-ALT-11` [S2]).
- An event's `requires` is a list, and every entry must hold; an entry `{ event, diverged: true }` holds for any option but the historical one (`R-ALT-001`, `R-ALT-002` [S3]).
- Each option that did not happen records who proposed it: `proposed` or `debated` (a body or office in words, a date, sources) or `design` (nobody is recorded proposing it) (`R-ALT-003`, `R-ALT-004` [S3]). An option nobody proposed keeps its `design` label and gets no follow-up card (`Q-ALT-12` [S2]).
- A consequence card follows only an option contemporaries are recorded proposing or debating, the Ferguson test (`Q-ALT-3` [S2]). It is soft, has no historical option, carries its parent's sensitive tags and lies at most 2 cards (unitless) deep from the record, a labelled design value (`Q-ALT-5` [S2]).
- Consequence cards take a turn's soft slots before the other soft events, after an early armistice (`Q-ALT-14` [S2]).
- Windows stay absolute dates; there is no "N turns after" form (`Q-ALT-4` [S2]).
- A consequence option may `rejoin` the record: later cards that need the parent's historical option fire again if their windows are still open, and the timeline keeps the road (`Q-ALT-6` [S2]).
- Real outside events that happened regardless may carry variant text for a course off the record; their options and effects never change (`R-ALT-010` [S3]).
- A consequence card names the road it follows and the record beside it; "Not adopted, proposed by" is shown after the choice, never on the button before it (`Q-ALT-7` [S2]). A timeline screen shows the player's course beside the record (`Q-ALT-8` [S2]).
- A shareable build refuses while any consequence card or variant lacks a person's sign-off (`reviewed_by`) (`Q-ALT-9` [S2]).
- Adding the `alternative` record, or the list form of `requires` with the same meaning, keeps a card's review status (`Q-ALT-10` [S2]).
- The rules take the ID area `R-ALT` (`Q-ALT-15` [S2]).
- The first content slice takes the roads with the strongest records, subject to the fact-check: defying the second truce, pushing to Hebron in Yoav, holding toward El-Arish, accepting Jerusalem's demilitarisation, and advancing into Lebanon in Hiram (`Q-ALT-13` [S2]). Every card is drafted by agents, edited by the owner and built only after sign-off [S4].

## Data touched

- `content/events/*.yaml`: `requires` as a list; `alternative` on options; `kind`; `rejoin`; `variants`.
- `content/schema/event.schema.json`.
- `sim/Statecraft.Sim`: `Content.cs`, `EventRules.cs`, `EventSystem.cs`, a new `Timeline.cs`, `ReleaseGate.cs`; `Statecraft.Cli` (`--release-gate`, `--timeline-report`). No `GameState` field and no save change.
- `unity/`: the event card banner and a timeline screen.
- `research_notes/Alternate timelines/roads-checked.md`, the fact-checked roads and the premise table.

## Done when

- `dotnet test sim/Statecraft.sln` passes with the R-ALT tests, and the historical run raises no consequence card.
- `hash1950`, `hash1952`, `mvp-end` and `smoke5` do not move in any mechanism task (checked by a real update run).
- Every consequence card in `content/` fires on its road in a test run, and `Statecraft.Cli --release-gate` refuses while one is unsigned.

## Out of scope

- A stored drift number, per-front drift, OR or NOT requirements, relative windows, delayed effects and procedural text.
- Follow-up cards for the armistice refusals: a refused armistice returns under `R-WAR-006` (Phase 5).
- Any change to the effects of the Egypt armistice refusal (#937) or the reparations refusal (#938).

## Sources

- [S1] The owner's widened scope for #939 (the request is quoted in [S3]), <https://github.com/eyalgolan/statecraft-israel/issues/939#issuecomment-6238281824>, checked 2026-10-17
- [S2] The owner's answers, "Accept all recommendations" for `Q-ALT-1` to `Q-ALT-15`, <https://github.com/eyalgolan/statecraft-israel/issues/945#issuecomment-6239061172>, checked 2026-10-17
- [S3] The design spec and plan, `docs/superpowers/plans/2026-10-17-alternate-timelines.md`
- [S4] The owner's review process for #783, <https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6231517653>, checked 2026-10-17

## Open questions

- None. Each card's text and effects are ruled one by one on its slice's issue.
