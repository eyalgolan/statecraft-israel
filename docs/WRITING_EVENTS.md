# Writing an event

An event is one YAML file in `content/events/`. The game shows it as a card with two or more options; each option
moves some numbers. This guide takes you from an empty file to a passing test run. Everything here was checked
against the loader (`sim/Statecraft.Sim/ContentLoader.cs`, `EventRules.cs`, `Content.cs`, `ReleaseGate.cs`); if the
code and this page disagree, the code wins and the page has a bug (please open an issue).

Read [docs/CONTENT_POLICY.md](CONTENT_POLICY.md) first. It is the contract for sources, quotes and sensitive
topics. The worked example below is [content/events/hula_drainage.yaml](../content/events/hula_drainage.yaml).

## The shortest path

1. Find your sources and write down, for each fact, a URL and the date you opened it.
2. Copy `hula_drainage.yaml` to `content/events/<your_id>.yaml` and edit it. The file name is free, but use the id.
3. Run `dotnet test sim/Statecraft.sln` (see "Running the tests").
4. Add a line to `CHANGELOG.md` under `## [Unreleased]`, and open a pull request.

## Fields

The loader is strict. An unknown key is an error, so a typo such as `titel:` fails loudly instead of being ignored.
A key written with no value (`tags:`) is an error for lists.

Editor help: the first line of every event file, `people.yaml`, `offices.yaml` and `parties.yaml` is a
`# yaml-language-server: $schema=...` comment. With the YAML extension (Red Hat YAML, in VS Code and others) a wrong
field name or a missing `sources:` gets a red line as you type. The schemas are in `content/schema/`. They are written
by hand, and `SchemaTests` keeps them equal to the loader types. If you add a field to a content type, add it to the
schema too, or that test fails. Keep the comment when you copy a file.

| Field | Required | What it is |
|---|---|---|
| `id` | yes | Unique across all events. Lowercase letters, digits and underscores, starting with a letter. |
| `title` | yes | The card title. May contain office placeholders. |
| `text` | yes | The card body. May contain office placeholders. |
| `anchor` | no (default `soft`) | `hard` or `soft`. See below. |
| `tags` | no | A list of free-form labels. Four are sensitive. See below. |
| `office` | no | The office id of the minister the event goes to when the player does not answer it. |
| `from` | yes | First day of the window, `YYYY-MM-DD`. |
| `to` | no | Day the window ends (exclusive). Must be after `from`. Leave it out for an open window. |
| `conditions` | no | Indicator bounds that must hold when it fires. See below. |
| `requires` | no | Fires only after another event was resolved with a given option. See below. |
| `objective` | no | Makes the event the completion card of an objective in `objectives.yaml`, which must name this event as its `card`. It fires only once the objective is completed (GDD loop only), and has exactly one option. See `docs/gdd/objective-card.md`. |
| `options` | yes | One or more options. See below. |
| `sources` | no by the loader, yes by the rules | A list of `{ url, checked }`. See below. |
| `status` | no (default `draft`) | The review status. See below. |

### id, title, text and placeholders

Never type a real person's name into `title`, `text` or an option's `text`. Write an office id in braces, such as
`{prime_minister}`, and the game replaces it with whoever holds that office on the day the event fires (and with the
player's own appointment, if the player changed the cabinet). In the example, `{prime_minister} must decide.` becomes
the name of the Prime Minister on the day. Office ids are in `content/offices.yaml`; who held them is in
`content/people.yaml`.

Rules the loader enforces:

- Placeholders may use only lowercase letters, digits and underscores, and must name a real office.
- Any other `{` or `}` is an error (no braces for emphasis).
- The office must have a holder on **every day of the window** (`from` up to `to`). If the office is vacant for even
  a day, or the window is open-ended and the last holder's term ends, loading fails and the message gives the date.
- If the window has already closed when the event reaches the player (it can fire late), the names are filled in for
  the last day of the window.

Write plain description. Do not put words in anyone's mouth: no invented quotes
([CONTENT_POLICY.md](CONTENT_POLICY.md), section 2).

### anchor

- `soft`: an event that may or may not fire. At most 3 non-hard events fire per turn (the earliest `from` first, then
  by id), so a soft event can wait for a later turn inside its window.
- `hard`: an event that must happen. Every eligible hard event fires. It needs one option marked `historical: true`;
  in Historical mode, if the player leaves it unanswered it resolves to that option.

### tags and the sensitive review

Tags are free-form, but these four are sensitive (checked without regard to case): `holocaust`, `terror`, `massacre`,
`war-casualties`. An event with any of them:

- cannot give a positive delta to any resource in any option: `treasury`, `fx_reserves`, `food_supply`, `morale`
  (standing for Social cohesion) or `intl_standing` (standing for International Support). The loader refuses: it would
  reward the atrocity. Owner decision of 15 October 2026 (Q-EFX-2, "Extend it"); until then the rule covered money only;
- needs `status: sensitivity-reviewed` before any build is shared. That status is set by the maintainer, not by you.
  Submit it as `fact-checked`. `Statecraft.Cli --release-gate`, the `-shareable` build and `tools/package_release.py`
  refuse to release while any sensitive event is not `sensitivity-reviewed`, and a test requires every sensitive event
  in `content/` to carry exactly that status once reviewed.

The other tag used so far is `security`. It has no rule attached.

### office

The office id (for example `defense_minister`) of the minister who answers the event if the player does not. The
minister picks the option marked `recommended` with a probability equal to their competence divided by 10, and
otherwise picks at random. With no `office`, a middling competence of 5 is used. An unknown office is an error.

### from and to

The window is half-open: the event can fire from `from` up to, not including, `to`. It fires on the first turn that
overlaps the window. Turns are a month long and end on the 15th (15 June 1948, 15 July 1948, and so on, to the game's
end on 15 January 1952), so a window shorter than a turn can still fire, late. For a decision card, open the window on
the 15th on or before the historical decision, so the card reaches the player on the turn before it (R-TURN-002;
`EventRetimingTests` pins each card's turn). Put a comment above `from` saying why you chose the date, as the example
does. The loader only requires `from`, and that `to` is later than `from`.

### conditions

Optional bounds on indicators (`morale`, `food_supply`, `housing`, `security`, `intl_standing`, the ids in
`content/model.yaml`), each a value from 0 to 1:

```yaml
conditions:
  - { indicator: morale, max: 0.4 }
```

`min` and `max` are both optional, and `min` must not exceed `max`. Not every event needs this; the example has none.

### requires

```yaml
requires: { event: other_event_id, option: other_option_id }
```

The event waits until `other_event_id` was resolved (chosen or delegated) with `other_option_id`. Both must exist, an
event cannot require itself, and requirements cannot form a cycle.

### options

Each option has:

| Field | Required | What it is |
|---|---|---|
| `id` | yes | Unique within the event. Same shape as an event id. |
| `text` | yes in practice | What the player reads on the button. May use placeholders. |
| `historical` | no (default false) | `true` on the option that matches what really happened. At most one per event. |
| `recommended` | no (default false) | The option a competent minister picks when the event is delegated. |
| `effects` | no | A list of `{ target, delta }`. |
| `set_rung` | no | Sets the security rung, 1 to 5. |
| `set_policy` | no | A map of policy id to a level from 0 to 1. |

An event needs at least one option. `historical` is a statement of fact and needs a source; `recommended` is a game
design choice and does not.

### effects

`target` is one of:

- an indicator id from `content/model.yaml`: `morale`, `food_supply`, `housing`, `security`, `intl_standing`. These
  live between 0 and 1, so a delta of `0.03` is three points of the scale. Keep deltas small, as the example does;
- `treasury` or `fx_reserves`, in game money.

`delta` must be a finite number. Any other target is an error. Policy ids (`rationing`, ...) are not effect targets;
use `set_policy` for those.

### sources

```yaml
sources:
  - { url: "https://history.state.gov/historicaldocuments/frus1951v05/d288", checked: 2026-10-10 }
```

The loader reads `sources` but does not check it. The tests do: every event in `content/` must have at least one,
each URL must start with `https://`, and `checked` must be on or after 2026-10-09 (the date of the content check).
Use the day you actually opened the page. A source must support the claim it is attached to
([CONTENT_POLICY.md](CONTENT_POLICY.md), section 2). Paraphrase; do not quote unless you give the quote verbatim.

### status

One of `draft`, `fact-checked`, `historian-reviewed`, `sensitivity-reviewed`; anything else is an error.

- `draft`: a work in progress. A pull request for `content/` should not stay at this.
- `fact-checked`: a person checked every claim against its sources. This is what you submit. The content tests
  require `fact-checked` on every non-sensitive event in `content/`.
- `historian-reviewed`: planned for M4, not a gate yet. How a review is done and recorded until then:
  [HISTORIAN_REVIEW.md](HISTORIAN_REVIEW.md).
- `sensitivity-reviewed`: set by the maintainer for events with a sensitive tag (see above).

If AI helped draft the text, you still check every claim yourself before using `fact-checked`
([CONTENT_POLICY.md](CONTENT_POLICY.md), section 2, rule 6).

Hebrew display names in the other content files (`name_he`, `title_he`, `title_he_f`, `body_he`) have a separate
flag: `*_review: agent-checked` while only an AI agent has checked the name against its sources. A native speaker
removes the line once they have reviewed the name (#564); a blank value is an error, not a review. It does not change
the row's `status`.

### reviewed_by and reviewed_on

A person's sign-off beside `status`: `reviewed_by` is the reviewer's GitHub handle (never an email), `reviewed_on` the
day, `YYYY-MM-DD`. Both or neither; the loader refuses a blank or malformed value. **Only the reviewer writes them**, in a
commit of their own: never add, change or remove them yourself on someone's behalf, and an AI agent never does
(`scripts/oss/reviewed_by_guard.py` fails such a pull request; owner, 15 October 2026, #375). An agent may not edit any
field of a signed row either, until a person signs it again ("Block it in CI", owner, 15 October 2026). See
[HISTORIAN_REVIEW.md](HISTORIAN_REVIEW.md#sign-the-row-off).

## What the loader rejects

Errors are printed as `file: field: message`. These are the messages you are most likely to see:

| Message contains | Cause |
|---|---|
| `Property 'x' not found on type 'Statecraft.Sim.GameEvent'` | A key the loader does not know (usually a typo). Not a field of the event. |
| `must be lowercase letters, digits and underscores, starting with a letter` | An event or option `id` of the wrong shape (for example `Continue-Works`). |
| `duplicate event id` | Another file already uses this `id`. |
| `duplicate option id` | Two options share an `id`. |
| `unknown anchor` | `anchor` is not `hard` or `soft`. |
| `unknown status` | `status` is not one of the four values. |
| `must be after from` | `to` is on or before `from`. |
| `unknown office '` | `office` is not an office id. |
| `unknown office placeholder` | `{something}` is not an office id. |
| `stray '{' or '}' outside an {office_id} placeholder` | A brace that is not a placeholder. |
| `has no holder on` | A placeholder office is vacant on a day inside the window. |
| `unknown indicator` | A `conditions` entry names an indicator that does not exist. |
| `at least one option required` | `options` is empty. |
| `at most one option can be historical` | Two options have `historical: true`. |
| `hard anchor needs a historical option` | `anchor: hard` and no option is `historical`. |
| `unknown target` | An effect `target` is neither an indicator nor `treasury` or `fx_reserves`. |
| `sensitive events cannot reward` | A sensitive-tagged event with a positive `treasury`, `fx_reserves`, `food_supply`, `morale` or `intl_standing` delta. |
| `must be 1-5` | `set_rung` outside 1 to 5. |
| `unknown policy` | `set_policy` names a policy that does not exist. |
| `unknown objective` | `objective` names an objective that is not in `objectives.yaml`. |
| `has another card` | `objective` names an objective whose `card` is a different event. |
| `an objective's card has exactly one option` | An objective's card has more than one option. |
| `must be a list` | A list field (such as `tags:`) written with no value. |
| `an event cannot require itself` | `requires.event` is the event's own id. |
| `requirements form a cycle` | Event A needs B and B needs A, directly or through others. |
| `unknown event '` | `requires.event` names an event that does not exist. |

A full message looks like this (an effect target misspelled `securty`):

```
Content error: events/hula_drainage.yaml: events[hula_drainage].options[continue_works].effects.securty: unknown target
```

## Running the tests

From the repository root, with the .NET SDK installed ([dev-setup.md](dev-setup.md)):

```
dotnet test sim/Statecraft.sln
```

This loads all of `content/`, so any error above shows up here, and it must finish with 0 warnings. To run only the
checks on real content: `dotnet test sim/Statecraft.sln --filter RealContentTests`. To check the sensitive-review
gate by hand:

```
dotnet run --project sim/Statecraft.Cli -- --release-gate
```

The Python checks (`uv run --with pytest==9.1.1 --with pyyaml==6.0.3 pytest scripts/oss/tests -q`, and
`uv run scripts/oss/check_repo.py`) cover repository policy. They include a test that keeps this guide's field table
in line with the loader.

## A golden-hash change

`docs/determinism.md` pins two hashes of complete playthroughs (the "golden runs", to 1950-01-01 and to 1952-01-01).
They are computed from the real `content/`, so **an event whose window falls inside a golden run changes the hash**,
and the determinism tests then fail with the old and new value. That is expected, not a bug in your event. An event
that opens after 1952-01-01 does not move them.

How to tell:

- The failing test is `GoldenPlaythroughHashIsFixed` or `GoldenPlaythroughTo1952HashIsFixed` (in `DeterminismTests`),
  it shows two 64-character hashes, and nothing else fails: your event moved the golden. Say so in your pull request, and do not edit the hash constants yourself.
- If other tests fail, or a hash moves when you changed no event window, effect or option, that is a real problem;
  fix it first.

The maintainer rebaselines: they update `GoldenRun.cs`, `DeterminismTests.cs` and `docs/determinism.md` together, and add
a row to its "Rebaseline history". Contributors do not.

## Checklist

- [ ] `id` unique and lowercase; `title` and `text` use `{office}` placeholders, no hard-coded names
- [ ] `from` has a comment explaining the date; `to` is after it
- [ ] every fact has a `https://` source with the day you checked it
- [ ] at most one `historical` option; `hard` events have one
- [ ] no invented quotes; sensitive tags only where they apply, `status: fact-checked`
- [ ] `dotnet test sim/Statecraft.sln` passes
- [ ] a line in `CHANGELOG.md`

### Historical-only titles

An office with `historical_only: true` records a dated title without adding a playable Cabinet seat.
Its terms in `people.yaml` can be used in text placeholders such as `{deputy_title}` when the
event window has a holder throughout. It cannot own a policy, effect, budget line or an event's
`office:` delegation. Use the responsible senior minister for those gameplay fields. Existing
offices default to `historical_only: false`. A historical-only title ignores player appointments,
including entries carried in an older save.

### Dated offices

`from` (the day an office was established) and `until` (the day it was abolished) limit when an office is a Cabinet seat
and can be filled. Terms in `people.yaml` must fall between them. A budget line may run before `from`: the Budget
screen then names the date the ministry begins instead of calling it vacant.

### Budget lines owned by office and date

A line in `rules.yaml` `budget_start` is keyed by its own id. With no `budget_owners` entry, the office with that id owns it
for the whole era (and must not be abolished). A line can have a dated list of owners instead:

```yaml
budget_start:
  prime_minister: 0.15       # the Immigration line; its id stays as it was, the owner changes
budget_owners:
  prime_minister:
    - { from: 1948-05-15, office: immigration_minister }
    - { from: 1951-10-08, office: prime_minister }
```

The first owner starts on the first day of the era (no gap). Each `from` is inside the era and after the one before. Each
owner's office must exist from its `from` to the next `from` (or the end of the era), and one office may own only one line on
any day. The loader refuses anything else. `Offices.BudgetOwner(content, line, date)` and `Offices.BudgetLineOf(content,
office, date)` are the only readers. Lines that change across an era boundary are the carry rules' job (#59).
