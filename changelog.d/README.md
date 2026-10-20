# Changelog fragments

A pull request that changes the game (`sim/`, `content/`, `unity/`) can add its changelog entry here as a new file
instead of editing [CHANGELOG.md](../CHANGELOG.md). Two pull requests never add the same file, so they never conflict.

- Name the file after the branch or the issue: `changelog.d/<branch-or-issue>.md`, for example `changelog.d/1116.md`.
- Write each entry as a bullet (`- `) in the voice of the lines under `## [Unreleased]`; wrap long lines with two
  spaces, as there. Put `### Fixed` (or `### Changed`, `### Removed`) above entries that are not additions; without a
  heading an entry goes under Added.
- Add a new file; editing or deleting an existing one does not count. CI's `Changelog` job accepts a new file with at
  least one entry, or a line under `## [Unreleased]` in CHANGELOG.md.

At a release, `uv run scripts/oss/changelog_fold.py` moves every entry into CHANGELOG.md, in filename order, and
deletes the files ([docs/RELEASING.md](../docs/RELEASING.md), step 2). This README stays.
