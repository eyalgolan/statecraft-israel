# Historian review: proposed process

**PROPOSED.** Nothing on this page is in force. It is a proposal for [issue #29](https://github.com/eyalgolan/statecraft-israel/issues/29)
and needs the owner's decisions in [section 6](#6-owner-decisions) before any of it is built. Until then the process is
the one in [HISTORIAN_REVIEW.md](HISTORIAN_REVIEW.md): reviews are recorded in PR comments and YAML comments, and no
event's `status` changes.

## 0. Where this starts from

The [design spec, section 5.2](superpowers/specs/2026-10-09-statecraft-israel-design.md#5-content-and-historical-accuracy-pipeline):

> - `draft` → `fact-checked` → `historian-reviewed`, then `sensitivity-reviewed` for sensitive tags.
> - **Release builds include only fully reviewed content.**

and section 5.4: "A pro-Israeli historian advisory board does the final review."

What the code does today:

- `ContentLoader.cs` accepts the four status values and rejects any other key on an event, so a review cannot be
  recorded as a new YAML field without a schema change.
- `RealContentTests.EveryHistoricalRecordIsSourcedAndFactChecked` requires every non-sensitive event to be exactly
  `fact-checked` and every sensitive one to be exactly `sensitivity-reviewed`. Setting `historian-reviewed` fails it.
- `ReleaseGate.cs`, `Statecraft.Cli --release-gate`, the `-shareable` build, `tools/package_release.py` and
  `scripts/oss/release_gate.sh` check sensitivity review only.
- Content today: 31 events, 24 `fact-checked`, 7 `sensitivity-reviewed`, none `historian-reviewed`.

The M4 plan sets the bar ([D5](superpowers/plans/2026-10-10-m4-plan.md#owner-decisions)): "The review guide merged, at
least one named reviewer, and recorded reviews for at least the war events (1948 operations and Suez) and the 1952–56
decision scenes. Requiring every event to be `historian-reviewed` in shareable builds is N9, in M5". Its review row
says to start "with the 1948–51 events that M4 does not change: not the five war events (D4), the armistice and
reparations events (N5) or the 1951 education crisis (#7)". This proposal follows both.

## 1. Who qualifies

No names here. The owner chooses reviewers (section 6). A reviewer should meet all of the following:

- **Field.** Academic training in history, or a closely related field with published historical work: the history of
  Israel and the Yishuv, military history, political or diplomatic history of the Middle East, or the economic and
  social history of immigration and absorption.
- **Period.** Their own work covers 1948–56, or a part of it that matches the events they review. A reviewer of the war
  events knows the 1948 operations; a reviewer of the austerity and ma'abarot events knows the absorption years.
- **Sources.** Reads Hebrew. Many of the cards now cite Knesset minutes, Ben-Gurion's diary and other Hebrew primary
  records, and the reviewer has to read them in the original.
- **Track record.** At least one of: peer-reviewed articles or a monograph on the period; a doctorate on it; a
  professional role in an archive that holds the period's records. A popular-history byline alone is not enough, and a
  GitHub label or a self-description is never taken as a credential.
- **Independence.** Has not drafted, sourced or fact-checked the events they review. Is not paid by, and does not
  advise, a party, campaign or organisation with a stake in how a card reads.
- **Conflicts of interest**, declared in writing before the first review and kept with the reviewer record (section 3):
  a family tie to a person the game shows; a current role in a body whose own history is under review (for example an
  official history department reviewing its own operations); a commercial interest in the game; a public dispute with
  an author the card cites. A declared conflict does not disqualify by itself. The maintainer decides, and the
  reviewer leaves the affected events to someone else when the maintainer asks.

**Balance.** The framing is fixed by [CONTENT_POLICY.md, section 1](CONTENT_POLICY.md#1-the-perspective-is-israeli-and-it-says-so):
facts come from Israeli official and mainstream Israeli sources, and contested ranges and opposing-narrative panels are
not shown. Within that, the panel should not be one person or one school. Aim for reviewers from more than one
institution and more than one sub-field (military, political, social and economic history), so that a card on the
ma'abarot is not checked only by a military historian, and so that a disputed point in the mainstream literature is
seen by someone who knows both sides of that debate. Reviewers are not asked to supply an opposing narrative, and the
game will not show one.

What a reviewer does not do: sign off the sensitivity review (that stays with the maintainer), certify game-balance
values, or re-open the framing.

## 2. What a reviewer receives

### 2.1 The packet

One Markdown file per event, produced from a fixed commit, plus the event's YAML at that commit. Markdown because a
reviewer should not need git or YAML to read it. Each packet holds:

1. **Header:** event id, file, full commit SHA, the date the packet was made, current `status`, tags, and whether the
   tags need sensitivity review (`holocaust`, `terror`, `massacre`, `war-casualties`).
2. **Dates:** the event window as `from` and `to`, in words ("shown from 15 October 1948; gone by 23 October 1948"),
   with the note that `from` can be a game turn before the event and `to` is exclusive.
3. **Text as the player sees it:** title, body and every option, with each office placeholder resolved to the person
   who held it on `from` (from `people.yaml`), and a line if the holder changes inside the window.
4. **Options:** for each, its text, whether it is marked `historical: true`, whether it is `recommended`, and its
   effects in plain words, for example "Security +0.025, International support −0.08 (indicators run 0 to 1)" or
   "sets the war-escalation rung to 5". Names come from `content/model.yaml`. The packet says that these numbers are
   design values and are not under review.
5. **Requirements:** any `requires` line, in words (for example "only after `second_truce` was resolved with `comply`").
6. **Sources:** every entry with its URL, the `checked` date and its age in days, and the YAML comment that sits above
   it, if any (these comments often name the page, the session or the document symbol).
7. **The file's own notes:** every YAML comment in the event file, verbatim. They hold dated claims and the gaps the
   sourcing found ("when 25 January was fixed is a gap").
8. **Open questions and flagged contradictions** for this event, from a hand-kept list (section 2.3), each with its
   issue link and state: open, fixed (with the PR), or waiting for the maintainer.
9. **A claim list to fill in:** one row per checkable claim (a date, a number, a name, an office, an act), each with
   the field it comes from and an empty verdict column. The person who makes the packet drafts the list; the reviewer
   may add, split or strike claims.

### 2.2 The script

Extend `scripts/event_timeline.py`, which already loads every event, its era, anchor, status and tags. Add:

```
uv run scripts/event_timeline.py --packet <id> [<id> ...] [--out DIR]
```

It writes `<DIR>/<id>.md` for each id (default directory: `build/review-packets/`, not committed). It needs, beyond
what the script reads now: `content/people.yaml` and `content/offices.yaml` to resolve placeholders,
`content/model.yaml` for indicator and policy names, the raw file lines for YAML comments (PyYAML drops comments),
`source_report.py`'s age calculation (import it rather than copy it), and the open-questions file. It reads only and
writes only into `--out`. A test under `scripts/oss/tests/` builds a packet for one real event and checks that every
section is present and that no placeholder is left unresolved.

Not proposed here: a PDF, or any network fetch. The reviewer opens the source URLs themselves.

### 2.3 Open questions file

`docs/reviews/historian/open-questions.yaml`, kept by hand, one entry per event and field:

```yaml
- event: education_registration_1951
  field: from
  question: "1950-11-29 rests on en.wiki '1950 in Israel' only; the card's plenum sources do not give it."
  link: https://github.com/eyalgolan/statecraft-israel/issues/457#issuecomment-6160597428
  state: waiting-for-maintainer   # open | fixed | waiting-for-maintainer
  fixed_by: null                  # PR URL when fixed
```

It is seeded from the contradictions flagged on #442 and #457 (section 5 lists them) and updated by the PR that fixes
each one. It lives outside `content/`, so editing it changes no content hash and no golden.

## 3. How a review is recorded

### 3.1 Files

All under `docs/reviews/historian/`, outside `content/` for the same reason as above. Under the folder rule in
[LICENSING.md](../LICENSING.md) that text would be CC BY 4.0; the reviewer has to agree to that (section 6).

- `reviewers.yaml`: one entry per reviewer. `id` (a stable short id such as `r1`), `public_name` (or `null` if the
  reviewer asked not to be named), `role` (`historian`), `expertise` (the scope the maintainer confirmed),
  `conflicts` (the declaration, or "none declared"), `since`. No email, phone or address: contact details stay out of
  the repository.
- `<event_id>.yaml`: one file per event, holding every review of it, newest last. A file per event keeps each review
  next to nothing else, gives a clean diff and lets a second reviewer add a record without touching the first.

### 3.2 Fields of one review

```yaml
event: rhodes_talks
reviews:
  - reviewer: r1
    reviewed_on: 2026-11-20
    commit: <full SHA the packet was made from>
    fingerprint: <sha256 of the reviewed fields, section 3.4>
    scope: "All claims in text and options; not the effects"
    claims:
      - claim: "Egypt and Israel had both accepted the cease-fire by 6 January 1949"
        field: text
        sources: ["https://history.state.gov/historicaldocuments/frus1949v06/d362"]
        verdict: supported        # supported | supported-after-change | not-supported | unable-to-verify | out-of-scope
        comment: ""
    requested_changes:
      - field: text
        change: "..."
        resolved_by: <PR URL or null>
    outcome: accepted             # accepted | changes-requested | unable-to-verify
    dissent: []                   # section 4
    signoff: <URL of the reviewer's own sign-off comment>
    accepted_by_maintainer: <PR URL that merged this record>
```

`outcome: accepted` means every in-scope claim is `supported` or `supported-after-change`, every requested change has
`resolved_by`, and `dissent` is empty or the owner has ruled that it does not block (section 6, decision 8).

### 3.3 Sign-off

The sign-off is the reviewer's own act. The reviewer posts it from their own account as a comment on the PR that adds
the record: "I reviewed `<event>` at `<SHA>`; outcome `<outcome>`; the record in this PR is mine." The `signoff` field
holds that comment's URL. A reviewer without a GitHub account sends the same statement to the maintainer, who commits
it with a note of how and when it arrived and keeps the original. The claim list may be drafted by a contributor or an
AI agent; the verdicts, comments and sign-off may not. A code-owner approval is not a historian sign-off.

### 3.4 The fingerprint and `status: historian-reviewed`

The fingerprint is a SHA-256 over a canonical form (sorted keys, LF line ends) of the fields a historian checks:
`title`, `text`, `from`, `to`, `office`, `tags`, `requires`, each option's `id`, `text` and `historical`, the list of
source URLs, and the person each placeholder resolves to across the window. It leaves out `effects`, `set_rung`,
`recommended`, `anchor`, `checked` dates and YAML comments, which are design values or bookkeeping. The packet script
prints it, so the packet and the record carry the same value.

An event may carry `status: historian-reviewed` only when all of these hold:

1. it is not sensitive (sensitive events stay at `sensitivity-reviewed`; see below);
2. its file in `docs/reviews/historian/` has at least one review with `outcome: accepted`, a `signoff` URL, and an
   `accepted_by_maintainer` PR;
3. that review's `fingerprint` equals the fingerprint of the event as it is now;
4. the reviewer is in `reviewers.yaml`.

A sensitive event has one status field and needs `sensitivity-reviewed` for the existing gate. Rather than a fifth
status or a second YAML field (both schema changes), its historian review is shown by the same file and the same
fingerprint check, and its status stays `sensitivity-reviewed`. The owner may prefer the spec's order, where
sensitivity review comes after historian review and therefore implies it; then the seven events already at
`sensitivity-reviewed` would be out of step until reviewed (section 6, decision 7).

**How it is checked.** A new `scripts/oss/historian_reviews.py`, run by `check_repo.py` in CI:

- fails if an event is `historian-reviewed` and conditions 1–4 do not hold;
- fails if a record's YAML is malformed, names an unknown reviewer or event, or lacks `signoff`;
- prints, as information, every sensitive event and whether a matching accepted review exists.

`RealContentTests` changes to allow `historian-reviewed` for non-sensitive events. The release gate gets one more step
in `release_gate.sh` only when the owner decides that review gates release (section 6, decision 6); until then the CI
check keeps the status honest and nothing more.

**When the text changes later.** Any edit that changes the fingerprint (new wording, a moved date, a changed office or
holder, a new or removed source) makes the review stale. The CI check then fails, so the PR that makes the edit must
do one of two things: set the event back to `fact-checked`, or add a new accepted review at the new fingerprint. Old
records stay in the file; they apply to the commit they name and to nothing later. Effect and balance changes do not
invalidate the review, but the packet for any later review lists them. For a sensitive event the M4 plan's D4(d) rule
still applies on top: a change to the options or effects of a signed-off war event resets it to `fact-checked` for the
owner's sensitivity review.

## 4. Disagreements

**Reviewer against the sources.** The reviewer says a cited source is wrong or does not support the claim and gives a
checkable passage. That becomes a **Historical accuracy** issue under
[CONTENT_POLICY.md, section 3](CONTENT_POLICY.md#3-how-to-dispute-a-fact). The maintainer picks the game's one value by
the section 1 rule and records why. If the value changes, the reviewer rechecks the changed claim
(`supported-after-change`).

**Reviewer against the Israeli framing.** Section 1 of the content policy is not up for review. A comment that the
card takes the Israeli view, uses Israeli terms or leaves out an opposing narrative is recorded as `out-of-scope`, does
not block, and is not answered as a dispute. The line is between framing and fact: "the term is one-sided" is framing;
"this date is wrong", "this number is not in the source" and "this omission makes the sentence false" are facts and are
in scope. The rules in section 2 of the policy (sourcing, no invented quotes, no reward for atrocities) are in scope:
a reviewer who finds a breach reports it.

**Between reviewers.** Both records stay in the event file. The event cannot reach `historian-reviewed` while any
in-scope claim has a `not-supported` verdict from a reviewer that is not resolved. The maintainer may change the
value, cut the claim from the card, or ask a further reviewer; the further review is added, not substituted.

**Reviewer against the maintainer.** On a fact, the maintainer's decision under section 3 of the policy closes the
dispute for the game. If the reviewer still disagrees, the record keeps it:

```yaml
    dissent:
      - claim: "..."
        reviewer_position: "..."
        maintainer_ruling: <issue URL>
```

Whether a recorded dissent blocks `historian-reviewed` is decision 8 below. This proposal recommends that it does: a
status that says a historian checked the card should not stand over that historian's written objection. On design
values (effects, which option is `recommended`), the reviewer may flag a value as implausible; the maintainer decides,
and the flag goes in the PR, not in the review record.

**Escalation.** Issue, then the maintainer. There is no outside body. The owner's decision is final for the game; a
dissent stays public in the record unless the reviewer asked not to be named, in which case it is kept under their
reviewer id.

## 5. The first batch

Chosen from the events the M4 plan says to start with (1948–51, not changed by war mode, actor standing or the
coalition work), ranked by risk: a sensitive tag, a contradiction already flagged, contested history, or thin
sourcing. Eleven events.

| # | Event | Why |
|---|---|---|
| 1 | `bernadotte_assassination` | The only sensitive event in the eligible set (`terror`). Names Lehi as the killers and sets the government's answer to the Irgun battalions in Jerusalem. Four sources. |
| 2 | `frumkin_commission` | Coercion of Yemenite children in the camp schools, a contested chapter. Its option text was flagged on #442 (the move was between departments of the Education Ministry) and fixed in #469; the reviewer checks the fix. |
| 3 | `rhodes_talks` | Flagged on #457 (item 10): Israel's acceptance was notified by 6 January 1949, and the cease-fire took effect on 7 January. Fixed in #470; the reviewer checks the new wording against S/1187 and FRUS. |
| 4 | `elections_1949` | Flagged on #442: the 120 seats came on 18 November 1948, and 25 January was fixed on 25 November (Harpaz). Comment fixes in #469. Many numbers in one card. |
| 5 | `clothing_rationing_1950` | Flagged on #442: the 57–36 vote of 8 August 1950 was for the coalition's motion, made a confidence question, not a defeated no-confidence motion. Fixed in #469. |
| 6 | `iraqi_exodus` | A number (some 64,000 waiting) and the asset freeze, in a period whose history is debated. The reviewer checks for an omission that makes the card misleading. |
| 7 | `maabarot_policy` | The transit camps and Eshkol's proposal: central to the absorption years and contested in Israeli social history. |
| 8 | `jerusalem_ceasefire` | One source only. Names Dayan and Abdullah el-Tell and the 28 November 1948 date. |
| 9 | `hula_drainage` | The demilitarised zone with Syria and "Israel's rights in the zone", a legal claim. Two sources. |
| 10 | `latrun_proposal` | A cabinet vote with three ministers named as opposed through placeholders; the reviewer checks who held each office on 17 September 1948. Two sources. |
| 11 | `jerusalem_military_government` | Bernadotte's proposals, demilitarisation and Dov Yosef's appointment, with a window that closes on 3 August 1948. |

**Held for the second batch** (higher risk, but the M4 plan expects them to change, so a review now would go stale):

- The five war events under D4: `altalena`, `ten_days`, `operation_yoav` (convoy date fixed to 15 October in #448),
  `operation_hiram`, `operation_horev`. D5 requires their review in M4, after war mode lands.
- `sinai_withdrawal`: tied to Horev and likely touched by war mode.
- `reparations_claim` (`holocaust`) and the four `armistice_*` events: N5 changes how they act.
- `education_registration_1951`: #7 changes it, and its `from` date (#457 item 9) waits for the maintainer.

**Open items the packets must show** (from #457, waiting for the maintainer): item 2 (the Provisional Government from
14 or 15 May 1948; no event in the first batch opens that early) and item 8 (Ben-Gurion as Education Minister,
19 May to 8 October 1951, which bears on `{education_minister}` in that window). Items 5–7 (Second Knesset factions)
are in #475 and do not reach any first-batch card text.

## 6. Owner decisions

Each of these is for the owner. Nothing above is built until they are made.

1. **Recruitment.** Who approaches reviewers, and how (the "help wanted" label on #29 alone, or direct approaches by
   the owner). How many for M4: one, as D5 sets, or two so that no card rests on one person.
2. **Payment.** Volunteer or an honorarium, and from what budget. If paid, the independence rule in section 1 needs a
   line saying the fee does not depend on the outcome.
3. **Naming and credit.** Whether reviewers appear by name on the contributors page (the `historian` category since
   #430) and in the game's credits, by handle, or not at all; and the wording beside the name, for example "reviewed
   these events against their sources", so that credit is not read as endorsement of the game's framing.
4. **Licence of review records.** Whether review text is committed under CC BY 4.0 (the `docs/` rule) and how the
   reviewer agrees to that, or kept out of the repository with only a link.
5. **Timeline.** When the first batch goes out, and whether to keep the M4 plan's order (non-changing events first) or
   send the war events earlier and accept a re-review after war mode.
6. **Whether review gates release.** Proposed: the CI consistency check from the start; a release-gate step for D5's
   M4 bar; every event `historian-reviewed` only at N9 in M5.
7. **Status for sensitive events.** Keep `sensitivity-reviewed` and show the historian review through the record
   (proposed), or follow the spec's order so sensitivity review implies historian review.
8. **Whether a recorded dissent blocks `historian-reviewed`.** Proposed: it does.
9. **Reviewers per sensitive event.** One, or two for events tagged `holocaust`, `terror`, `massacre` or
   `war-casualties`.
