## What this changes

<!-- One or two sentences. Link the issue: "Fixes #123" only if this PR completes the issue, otherwise "Refs #123". -->

## AI assistance

<!-- One line: the tool, and what it did. Example: "Claude Code drafted the test; I checked each fact against its source." Write "None" if no AI tool was used. -->

## Evidence

<!-- Paste the commands you ran and the end of their output. Not a description of the output. `scripts/check.sh` runs everything CI runs. -->

```
```

### Visual evidence (if applicable)

<!-- For any change a player can see (models, the map, characters, the UI or the website), matched before/after screenshots are required in the PR description; see CONTRIBUTING.md, "Pull requests". Attach images to this PR, label the changed area and important side effects, and keep state/camera/zoom matched (resolution/lighting when relevant). For changes with no visible effect, explain why screenshots are not applicable; add a short video when motion helps review. Prefer reviewer-accessible GitHub attachments over private Library links or review-only files in the repo. -->

## Checklist

- [ ] Tests cover the change, and they fail without it.
- [ ] Test count did not go down, or this description explains why.
- [ ] The goldens (`Hash1950`, `Hash1952`, smoke) did not move, or this description says why they moved and they were re-pinned in `sim/Statecraft.Sim.Tests/golden/manifest.txt` with `STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln` (`docs/determinism.md`).
- [ ] `CHANGELOG.md` has a line under `## [Unreleased]` if this changes `sim/`, `content/` or `unity/`.
- [ ] Every fact added or changed in `content/` has `sources:` with a checked date (`docs/CONTENT_POLICY.md`).
- [ ] No quote is attributed to a real person unless it is sourced verbatim.
- [ ] Every new image, video, font, audio or map-data file has an `ASSETS.yaml` row.
- [ ] If this finishes a Phase 1 PR or a phase of #374, the Roadmap table is updated in `README.md` and `README.he.md` (`CONTRIBUTING.md`, rule 9).
- [ ] If `unity/` changed: Unity edit-mode tests ran locally, and their result is pasted above (CI does not run them).
- [ ] Nothing in the diff contains a credential, a Unity Cloud id or personal data.
- [ ] AI-assisted parts are disclosed under "AI assistance", and I can defend every line (`CONTRIBUTING.md`).
- [ ] I contribute this under the repository's licences for the paths it changes (`LICENSING.md`).
