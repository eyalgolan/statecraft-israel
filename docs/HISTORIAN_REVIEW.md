# Historian review

A review checklist and record format for [issue #29](https://github.com/eyalgolan/statecraft-israel/issues/29).
This guide prepares a human review; it does not certify any event or introduce a release gate.
Read [CONTENT_POLICY.md](CONTENT_POLICY.md) and [WRITING_EVENTS.md](WRITING_EVENTS.md) first.
A proposal for the M4 process (record files, status rules, the first batch) is in [historian-review-proposal.md](historian-review-proposal.md); it is not in force.

## Current behavior and the M4 target

The [design spec, section 5.2](superpowers/specs/2026-10-09-statecraft-israel-design.md#5-content-and-historical-accuracy-pipeline)
plans `draft` -> `fact-checked` -> `historian-reviewed`, then `sensitivity-reviewed` for sensitive tags.
Historian review is planned for M4 and is not yet a gate. The current code and policy take precedence over the planned pipeline:

- [ContentLoader.cs](../sim/Statecraft.Sim/ContentLoader.cs) accepts all four status values, but rejects unknown YAML keys.
  Beside `status`, every status row (an event, a person, a legislature, a government, an aliyah or population row, a
  site, and the map files' rows) takes a person's sign-off: `reviewed_by` (their GitHub handle) and `reviewed_on` (the
  day), both or neither (see "Sign the row off" below).
- [RealContentTests.cs](../sim/Statecraft.Sim.Tests/RealContentTests.cs) currently requires non-sensitive events to be exactly
  `fact-checked` and sensitive events to be exactly `sensitivity-reviewed`. Changing one to `historian-reviewed` fails that test.
- [ReleaseGate.cs](../sim/Statecraft.Sim/ReleaseGate.cs) checks sensitivity review, not historian review.
  A green release gate is not evidence that a historian checked the content.

Record an actual historian review now in the PR, with the sign-off and a pointer comment in the YAML as described below,
leaving `status` unchanged. Before changing a status, a separate implementation must update tests and decide how historian
and sensitivity sign-offs are preserved together. Do not weaken the sensitivity gate to make a historian status pass. This
guide defines no extra status values.

## Who reviews and what is being reviewed

The maintainer confirms a human reviewer with relevant historical or academic background, their area of expertise,
and the public name or handle they consent to use. Do not infer credentials from a username or label an AI check a historian review.
A reviewer can limit the review to particular claims or decline topics outside their expertise. The record names those limits.

Start with a small batch of non-sensitive events with retrievable sources. For each event, the PR lists its file, event id,
exact commit, current status and whether its tags require sensitivity review. No batch is accepted merely because its sources
list is nonempty. Do not put private contact details or unapproved biographical information in a public record.

Give the reviewer the exact YAML at that commit, the rendered title/body/options, a claim list and the source packet.
The packet identifies each claim's source URL, check date, and page/section or timestamp where it can be checked.
If a source is inaccessible, record the gap instead of claiming it was read. Short, exact quotes may be used for checking;
do not reproduce a book or source packet whose rights do not permit sharing it.

## Per-event checklist

- [ ] Dates, names, offices, places, amounts and numbers match the cited passages. Check the event window separately
      from the historical date: `from` can be a game turn before the event, and `to` is exclusive.
- [ ] The title, body and option text are supported by the sources and do not invent speech or put unsourced words in a person's mouth.
- [ ] The option marked `historical: true` matches the documented decision, not an invented alternative or a gameplay preference.
- [ ] Office placeholders resolve to the right public role across the window. Check office terms as well as the event source.
- [ ] Facts use the project's Israeli framing and source-selection rules in [CONTENT_POLICY.md, sections 1-2](CONTENT_POLICY.md).
      Where sources disagree, identify the exact disputed field and both passages; do not silently average or erase the conflict.
- [ ] Separate factual claims from design choices. `recommended`, indicator deltas and balance are not certified as historical
      measurements just because a historian reviewed the prose. Flag an implausible effect for design review rather than inventing evidence.
- [ ] Check the tags, civilian-harm framing and no-reward rules. Sensitive tags (`holocaust`, `terror`, `massacre`, `war-casualties`)
      still need the maintainer's separate sensitivity review. A historian cannot sign that off on the maintainer's behalf.
- [ ] List any omitted context that makes the text misleading, each correction needed, and any claim left outside the review scope.

## Record the result

The reviewer comments on the PR for each event, giving: file/id, reviewed commit, review date, agreed public identity and
relevant expertise, claim/source/page list, scope limits, outcome and corrections. Use `accepted`, `changes requested`, or
`unable to verify` as **record outcomes**, not YAML `status` values. An accepted result needs an actual affirmative human review.
A contributor's or AI's summary is not a substitute for the reviewer's record. GitHub code-owner approval is a different check.

### Sign the row off

**Changed on 15 October 2026.** Until then this guide said to record a review in comments only, "without adding unsupported
keys such as `reviewed_by` or `reviewed_on`". The [owner decision, 15 October 2026, #375](https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6184126823) reverses that: "Yes, humans only". A row a person has
reviewed now carries, beside its `status`:

- `reviewed_by`: the reviewer's GitHub handle (letters, digits and single hyphens, at least one letter), never an email or
  a display name;
- `reviewed_on`: the day of the review, exactly `YYYY-MM-DD`, not after the day of the commit that adds it.

Both or neither: the loader (and the map's readers in Unity, and `tools/routes_1948.py`) refuses one without the other, a
blank value, or a value of the wrong shape. **Only the reviewer writes these two lines, in a commit of their own.** An AI
agent never does, not even to copy a sign-off it was told about: `scripts/oss/reviewed_by_guard.py` fails a pull request
in which a commit with an AI co-author trailer adds, changes or removes either line, and it fails closed. A row with a
whole sign-off leaves the review queue (`tools/review_queue.py`, `docs/review-queue.md`).

**A signed row is closed to agents** (owner, 15 October 2026, on #629: "Block it in CI"). A sign-off vouches for the text
the reviewer read, so the same guard also fails a pull request in which a commit with an AI co-author trailer changes any
line of a row that carries a whole sign-off, any field, the `id` included, or adds a key inside it. It stays failed until
a person, in a later commit of their own in the same pull request, reads the row again and signs it again (a new
`reviewed_by` / `reviewed_on`) or removes its sign-off. An agent that needs to change a signed row proposes the change
in words, and a person makes it. Merging `main` into a branch does not count as an edit: the guard reads a merge commit's
combined diff, so lines it takes unchanged from `main` are not its own.

After the reviewer confirms acceptance, they add the two keys and a comment block next to `sources` that points to the
record. Replace every placeholder with facts from the real review; never commit this unfilled example as a sign-off:

```yaml
# Historian review record:
# Reviewer: <agreed public name/handle>; expertise: <confirmed relevant scope>
# Reviewed: <YYYY-MM-DD>; content commit: <full reviewed SHA>
# Record: <URL of the actual per-event review comment>
# Claims and sources: <fields checked, source URLs and page/section locators>
# Scope: <covered claims and any exclusions>; outcome: accepted
# Sensitivity: <separate maintainer record if applicable; otherwise not applicable>
# status remains at its existing value until the M4 implementation supports the transition.
# reviewed_by: <the reviewer's GitHub handle>    (the reviewer's own two lines, uncommented, beside status)
# reviewed_on: <YYYY-MM-DD>
```

The comments are not a machine-enforced certificate: anyone can edit them, and no test reads them. The reviewer's PR
comment is the record; the comments point to it, and `reviewed_by` and `reviewed_on` say who signed the row off and when.
An edit that adds only comments and these two keys still changes the content hash (`ContentLoader` hashes the text of every content file), so a save made
before it loads with the "different game content" warning. It changes no game state, so the golden hashes in
[determinism.md](determinism.md) stay the same. If they move, the edit changed more than comments: find out what before going on.

## Corrections, disagreements and changed content

For each disputed field, open or link a **Historical accuracy** issue with the current value, proposed correction and checkable
source passages. The maintainer chooses the game's value under [CONTENT_POLICY.md, section 3](CONTENT_POLICY.md#3-how-to-dispute-a-fact).
Keep the disagreement and rationale in that issue. A source conflict or unverified claim is not an accepted review.

After a correction, the reviewer checks the changed claims and records the new commit. Earlier acceptance stays attached to
its original revision; it does not silently cover later edits. Keep unrelated design changes out of the review PR.
Sensitive content that changes also returns to the maintainer for sensitivity review.

## Before the first reviewed batch lands

1. Confirm the reviewer and their scope, then collect the per-event records at exact commits.
2. Fix requested corrections, update each affected `sources` entry with its real check date, and get the changed claims rechecked.
3. Add the confirmed audit comments, keeping current `status` values and the separate sensitivity sign-off intact.
4. Run the sim, tools and repository-policy checks listed in [CONTRIBUTING.md](../CONTRIBUTING.md), including
   `dotnet test sim/Statecraft.sln`, `uv run scripts/oss/check_repo.py` and `git diff --check`. Content changes need a changelog line.
5. The maintainer reviews the PR. Only after a separate M4 status/test/gate implementation should a batch move to
   `status: historian-reviewed`. Guide-only work does not close #29: its first genuine reviewed batch remains outstanding.
