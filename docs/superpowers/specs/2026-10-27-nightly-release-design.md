# Nightly release: design

Status: Approved by the owner on 27 October 2026 ("Approve as written"). Built from docs/superpowers/plans/2026-10-27-nightly-release-plan.md.

Amended by owner rulings, 28 Oct 2026: "the nighttly release needs to be vx.y.z +0.0.1 every time";
"Mark each Latest (Recommended)"; "Stamp the build only (Recommended)". These replace the original nightly naming
and Latest policy. The owner-run release steps in docs/RELEASING.md remain as written.

## Intent

The owner, 27 October 2026: "we need a nightly build process that releases a version at a specific time every day. build that process and make sure it doesnt introduce any bugs, degregations or gaps." Asked who it is for and when, the owner chose "Public: website + itch" and "03:00 (Recommended)" (Israel time).

So every night at 03:00 the newest `main` becomes the public game: the website's `/demo/` and the itch.io page. Because players get it with nobody watching, the process must refuse to publish anything worse than what is already live. A skipped night is acceptable; a broken public build is not.

## Goals

1. At 03:00 Asia/Jerusalem, build the current `origin/main` and, only if every gate passes, publish it to GitHub Releases (which updates the website demo through `site.yml`) and to itch.io.
2. Gate each nightly at least as strictly as a normal release (docs/RELEASING.md steps 4-5 and 7), plus a no-regression comparison against the build that is live now.
3. Leave a clear record each night: published, skipped (no change), or refused (which gate, with the log), in a place the coordinator reads.
4. Never disturb the coordinator lanes or the Codex root: take the Unity and WebGL slots like any other job.

## Non-goals

- No desktop players in the nightly (the MVP release is web only, owner ruling 20 Oct 2026, #814). The Mac player can be added later.
- No change to the owner-run release process in docs/RELEASING.md; automated nightlies also use vX.Y.Z.
- No new gameplay or UI.

## Anti-goals (a failure even if it "works")

- Publishing a nightly that fails to boot, regresses load time, moves the goldens without a recorded reason, or contains an unreviewed sensitive item.
- Publishing over a working public build when any gate could not run (missing disk, Unity busy for hours, screen-dependent check that could not execute). Unknown counts as fail.
- A nightly that is not a normal release marked "Latest" on GitHub (owner ruling, 28 Oct 2026), or that leaves the website demo pointing at a broken zip.
- Storing or printing the itch.io key or a GitHub token anywhere.

## Constraints (verified 27 Oct 2026)

- Unity is not in CI (AGENTS.md); WebGL builds run only on the owner's Mac with `tools/release_build.py`. The nightly therefore runs on this Mac, from a dedicated worktree, under `launchd`.
- `scripts/oss/release_gate.sh` refuses to run except on the merged `main` and refuses while an owner decision is open, an asset is `owner-verify`/`licence: unknown`, the sensitivity gate refuses content, or the history scan has an open secret-shape row. The nightly runs it unchanged; if it refuses, the night is REFUSED (this is intended: it is the same bar as a release).
- `site.yml` builds `/demo/` from the newest release in `gh release list` that has a `*-web-pages.zip`, but it has NO release trigger: it runs on a push to `main` touching its paths, or on `workflow_dispatch`. The nightly therefore starts the deploy itself (`gh workflow run site.yml`) and waits for it. As of 28 Oct 2026 v0.3.1 is Latest; the next nightly v0.3.2 becomes a normal Latest release under the amended owner policy, and the site serves its web zip.
- itch.io: `butler` is installed (`~/.local/bin/butler`) with saved credentials. docs/m3-release.md:330-332 says the itch upload is an owner step "never automated"; the owner's 27 Oct choice replaces that, and the doc is updated in the same PR.
- `tools/web_check.py` runs Chrome in `--headless=new`, so it can run while the screen is locked; `tools/headed.py` checks cannot (a locked console stalls headed boots). The nightly uses only headless checks plus the boot/size measurements that do not need a visible window.
- Disk: WebGL builds need >= 40 GB free at launch (the coordinator's disk rule). Below that the night is REFUSED, never "build anyway".
- Hard rules apply: never spend money, no force-push, no history rewrite, merges only via merge.sh (the nightly never merges anything).

## How it works (proposal)

Nightly job, `tools/nightly.py`, started by a `launchd` agent at 03:00:

1. **Preflight.** `git fetch`; if `origin/main` equals the commit of the last published nightly, record SKIPPED (no change) and stop. Check disk >= 40 GB, no `EXCLUSIVE-WINDOW` file, Unity licence available. Fail closed.
2. **Prepare** a clean dedicated worktree at `origin/main` (reused nightly, `git reset --hard origin/main` inside that worktree only, never in shared checkouts).
3. **Gates before building:** `bash scripts/check.sh` (with the Unity compile actually running), `bash scripts/oss/release_gate.sh` must print `RELEASE GATE OK`, sim tests under he_IL and en_US.
4. **Unity:** warm-up, FULL EditMode (total > 0, 0 failures), through `unity_slots.py --webgl` and `machine_lock`, with the settings churn restored afterwards.
5. **Build and package** exactly as docs/RELEASING.md step 5 (gzip, `--startscreen` passes, `tools/package_release.py`), both the `-web.zip` (itch, GitHub) and the `-web-pages.zip` (site demo). The build carries X.Y.Z in `bundleVersion` and the in-game label. Choose the numerically highest published release tag matching `^v[0-9]+\.[0-9]+\.[0-9]+$` and add one to its patch component; ignore drafts, `-nightly` and other tags. No matching release refuses. With today's v0.3.1 and v0.3.1-nightly.20261028, choose v0.3.2. Leading-zero components are numeric: v0.03.1 ties with v0.3.1, and the next version is normalized to v0.3.2. Read every release page through the existing gh runner. Before checking availability, delete a stranded draft only if its tag matches the saved `open_draft`, then clear that field. Under `--no-publish`, leave it alone and refuse the collision. Refuse every remaining release or local/remote git tag of that name; never overwrite it. Stamp only in the nightly worktree AFTER `release_gate.sh` passed on the clean tree, never commit it (owner rulings, 28 Oct 2026). The no-change skip still compares the saved published SHA, independent of its tag.
6. **No-regression checks:**
   - `web_check.py` smoke (`golden&smoke=5`, `smoke-orders`) must pass on the built player.
   - **Sizes** (owner, 27 Oct: "Budget in the PR (Recommended)"): refuse if data.gz + wasm.gz + framework grow more than 2% over the previous published nightly, unless a PR merged since then records the size increase in its body (the asset load-gate measurements); then the new size becomes the baseline.
   - **Boot time** (owner: "Sizes + headless boot (Recommended)"): a new headless boot-time measurement (Chrome `--headless=new`, same flags every night, 3 runs, median) with its own baseline from the first nightly; refuse a slowdown beyond a bound measured from 5 baseline nights' spread (recorded in the nightly log, not invented). The visible-window `tools/headed.py boot` stays a PR gate.
   - **Goldens:** a nightly never moves goldens; golden moves arrive only through reviewed PRs (their docs/determinism.md rows are checked at PR time), so the nightly only requires the golden tests in the FULL run to pass (builder's call).
7. **Publish** only if every step passed, in this order so nothing public changes until both stores are verified:
   a. `gh release create vX.Y.Z --draft --latest` with the web zips and generated notes (commits since the previous nightly), without `--prerelease`; verify `isDraft: true` and `isPrerelease: false`, download the draft's assets and diff them against the local zips.
      Immediately before create, check the tag again (no release, no local or remote git tag), then record and save `open_draft`. A release made since preflight is refused and not deleted. A create that fails after leaving a draft has that draft cleaned; rollback deletes only this run's draft while it is still a draft; a saved owned draft remains recognizable on a rerun. Ownership is by tag, so a same-tag draft created by someone else in the second between the check and create would be treated as this run's; release-ID ownership and a persisted Latest-recovery retry are deferred to #1917.
   b. `butler push` the plain gzip `-web.zip` contents (the itch file per docs/RELEASING.md step 5 and docs/m3-release.md:347-350) to `eyalgolan/statecraft-israel:html5 --userversion X.Y.Z`, then `butler status` must show the new build live.
   c. Publish the draft (`gh release edit vX.Y.Z --draft=false --latest`), verify `isDraft: false`, `isPrerelease: false`, Latest and the tag's commit, then `gh workflow run site.yml` and wait for it to succeed; fetch `https://eyalgolan.github.io/statecraft-israel/demo/` and check it serves the new version.
      Immediately before the public edit, read the current Latest through gh and record its tag (or no previous Latest) in `latest-before-publish.txt`, separately from the previous nightly asset baseline.
   d. On a publication or verification failure after (b): push the previous nightly's (or release's) web build back to itch, delete only this run's unpublished draft, and record REFUSED. On ANY failure after the public edit, including verification, saves, interrupts and site checks, and only while the failed release is still Latest, restore the saved previous Latest with `gh release edit <previous> --latest` and verify it; a Latest set by someone else in the meantime is left alone and reported. Record the prior tag and recovery result in the REFUSED line and `latest-rollback.log`; if there was no previous Latest, say so and remove the failed release's Latest mark. Recovery failures explicitly request manual repair. Preserve published releases and tags. If (c)'s site deploy fails after the release is public, re-run `site.yml` once; if it still fails, retain the new itch build and public release, mark the site pending, restore the previous Latest, and record REFUSED with an alert. Pages switches deployments only on success.
8. **Record** one line per night (PUBLISHED / SKIPPED / REFUSED + gate + log path) in `~/israel-game/.superpowers/nightly/LOG.md` and the HANDOFF; on REFUSED send a desktop notification.
9. **Retention:** no deletion of published nightly releases or tags (deleting public tags is avoided under the no-history-rewrite rule; builder's call). Revisit if the list becomes unwieldy.
10. **itch save safety first** (owner, 27 Oct: "Automated save check (Recommended)"): before nightly itch pushes are enabled, build an automated two-upload save test against itch (save in build A, push build B, reload the itch page, confirm the save survives; docs/m3-release.md:354-388). Until it passes, the nightly publishes to GitHub and the site only, and skips itch with a logged reason.
11. **Machine:** the Mac stays plugged in and awake (owner, 27 Oct: "Mac stays awake"); no wake schedule. If the job does not start by 03:30 (asleep, logged out), the next run reports the missed night.

## Left to the builder

- Exact structure of `tools/nightly.py`, its tests (pytest, with fakes for git/gh/butler/Unity), and the `launchd` plist.
- Where the noise bound for sizes comes from (reuse the asset load gate's definition; do not invent a number).

## Decisions recorded

Owner, 27 Oct 2026: "Public: website + itch"; "03:00 (Recommended)"; "Automated save check (Recommended)"; "Sizes + headless boot (Recommended)"; "Budget in the PR (Recommended)"; "Mac stays awake".

Coordinator's calls (owner to check): the nightly triggers `site.yml` itself; itch gets the plain `-web.zip`; draft-first publish order with rollback; per-night `bundleVersion` stamp after the gate; no deletion of nightlies; goldens gated only by the FULL run.

## Docs to update in the same PR

docs/RELEASING.md (a Nightly section) and docs/m3-release.md:330-332 (the itch upload is now automated for nightlies, per the owner's 27 Oct choice).
