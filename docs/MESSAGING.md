# Messaging and social copy proposals

Draft public copy for maintainer review, checked on 23 October 2026 against main at `36ddb0a1` and the published `v0.2.0` prerelease. This proposes a reusable way to write public copy and supplies examples to review. It does not establish new channels, a posting schedule or an approved campaign. Evidence below is repository and release-note inspection, not a fresh playthrough or native Hebrew review.

## Start with the audience's next step

| Reader | Question to answer | Useful next step |
| --- | --- | --- |
| Prospective player | What do I do in this game, and what can I access today? | A verified playable destination or clearly labelled download instructions |
| Existing player | What changed in this release, and how does it help me? | The exact release notes and download |
| Contributor | What concrete work can I help with? | An existing issue or the contribution guide |
| Researcher or historically interested reader | How are facts, framing and sensitive events handled? | The content policy and historical-error reporting route |

Lead with one useful point. Give a concrete next step. Do not turn a small update into a claim that the whole design is complete.

## A claim needs a scope and a source

Before publishing, record the claim, its evidence, the version or date checked, and the destination the reader will reach. Use these distinctions in prose:

- **Released:** verified in the named release. Link that release rather than assuming current main and the release are identical.
- **Implemented on main:** present in the repository, but not yet verified in a named release. Say that it is in development.
- **Planned:** a design or roadmap item. Use future or conditional wording, without adding a delivery date.
- **Concept:** an illustration or cinematic, not evidence of gameplay. Keep its label and attribution with it.

Historical claims still follow the [content policy](CONTENT_POLICY.md) and [sourcing guidance](SOURCING_FOR_CONTRIBUTORS.md). Distinguish simulation values and design assumptions from historical facts. The project's perspective is Israeli; do not advertise it as a neutral viewpoint. Describe its sourcing and review standards without promising that every draft has already passed review.

## Current claim register

| Claim or material | Evidence checked on 23 October 2026 | Safe use and remaining check |
| --- | --- | --- |
| Turn-based strategy about governing Israel, starting on 15 May 1948 | [README](../README.md), [content policy section 1](CONTENT_POLICY.md#1-the-perspective-is-israeli-and-it-says-so) | Use the stated player role and framing. Source any additional historical explanation separately. |
| How to play explains worker-order timing and training terms | [v0.2.0 release notes](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.2.0), Fixed | Worker changes take effect at End turn. English defines halutzim and Hakhshara; the changed Hebrew remains pending native review. |
| The Timeline compares resolved choices with the historical record; the turn report lists enemy orders | [v0.2.0 release notes](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.2.0), Added | Describe these specific additions. Do not imply that every planned alternate-history branch is playable. |
| Downloadable WebGL prerelease | [v0.2.0 assets](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.2.0): `statecraft-israel-v0.2.0-web.zip` and `statecraft-israel-v0.2.0-web-pages.zip` | These are web archives, not desktop installers. v0.2.0 is the newest published GitHub release found in this check; the v0.2.1 Git tag alone does not establish a published release. |
| Hosted browser-play destination | [README](../README.md), [Play page source](../site/src/content/en/play.md) and [press page source](../site/src/content/en/press.md) link [itch.io](https://eyalgolan.itch.io/statecraft-israel) | Use the repository's chosen destination. Its current hosted version and runtime were not verified in this pass; do not equate it with a release tag without checking. |
| English and Hebrew text, with field-level English fallback | [v0.2.0 release notes](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.2.0), Timeline and unit-card entries; [README](../README.md), What works now | Hebrew event text is available, including existing titles and answers in Timeline. Missing Hebrew fields can still show English. The release notes explicitly retain English unit-card body fields; do not promise a fully translated or fully native-reviewed game. |
| Current project and code description | [README](../README.md), [LICENSING](../LICENSING.md), [owner decision in #1371](https://github.com/eyalgolan/statecraft-israel/pull/1371), [D1 in DECISIONS](oss/DECISIONS.md) | Current main calls the game free and the code available for noncommercial use. In D1, the owner states that the game will stay free, including on Steam, and will not charge money or carry ads. Use the current licence page for details, not a blanket open-source claim. Earlier releases retain their recorded terms; do not apply main's new terms retroactively. |
| The full MVP design | [GDD](GDD.md), [README](../README.md) | Design scope and current implementation must be distinguished; no invented completion date. |
| Historical and sensitive content standards | [content policy](CONTENT_POLICY.md), especially sections 1 and 2 | Explain the rules. Do not claim a particular event exists or has passed review without checking its content record. |
| Screenshots, concept art and cinematic trailer | [press page source](../site/src/content/en/press.md), [asset provenance](../ASSETS.yaml), [licensing](../LICENSING.md) | Use only a verified capture as gameplay. Preserve concept/cinematic labels, provenance and credits; check the individual asset's terms. |

Recheck this register before reusing dated examples. A changelog item saying that a model or system is imported but not used does not support saying the player can interact with it.

## Existing copy to review next

The [English press factsheet](../site/src/content/en/press.md) and [Play page](../site/src/content/en/play.md) now link to itch.io. The earlier hosted-link gap is closed. The factsheet still describes Hebrew only as an interface in progress; a focused follow-up can explain the available text and fallback without implying native approval.

Candidate English factsheet lines:

> **Status:** An early playable prototype. Play in your browser on itch.io, or find release notes and downloadable web builds on GitHub Releases.
>
> **Platform:** Web browser (WebGL).
>
> **Languages:** English and Hebrew interface and event text. Missing Hebrew fields fall back to English. Native Hebrew review is ongoing.

Evidence: the register above. These are review candidates, not changes to the live site. Before applying them, check the hosted build, pair the English with Hebrew, retain any native-review flags and follow the current [#564 review process](https://github.com/eyalgolan/statecraft-israel/issues/564). Supply matched before/after website captures. Preserve the factsheet's licence, credit and contact information, checking each against its current source.

The [Steam description draft](steam/store-page-draft.md) records its own 14 October baseline and open confirmations. Audit it against the chosen launch build before use. Do not convert its planned features into shipped claims or infer a live store page from a repository draft. The owner chooses the store status and launch promise.

## Proposed social copy

These are English drafts for review, not published posts. A channel's final length, destination and any Hebrew adaptation need a separate check.

### Release: understand your choices and the next turn

> Statecraft: Israel v0.2.0 adds a Timeline comparing your choices with the historical record. After End turn, the turn report lists enemy movements, attacks and fortifications. How to play also clarifies when worker changes take effect and explains the training terms. Read the prerelease notes and download the web build: https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.2.0

Evidence: v0.2.0 Added entries for Timeline and enemy orders, Fixed entry for How to play, and its web archives. Intended next step: inspect the prerelease. This is draft copy for review, not a published announcement. Attach only a capture verified against this build; the archive link alone is not an instant-play button. A separate play-now post may use itch.io after its current build has been checked.

### Contribution: improve what the player reads

> Help make Statecraft: Israel clearer to play. Writing and design contributions can improve event choices and interface text. Historical claims need sources, and Hebrew wording needs native review. Start with the contribution guide and choose an open task: https://github.com/eyalgolan/statecraft-israel/blob/main/CONTRIBUTING.md

Evidence: [CONTRIBUTING](../CONTRIBUTING.md), Ways to help and Picking an issue; [content policy](CONTENT_POLICY.md). Check the task is still open before replacing this general destination with a specific issue. Do not imply a volunteer role has been assigned.

### Research: explain the standard

> Statecraft: Israel uses an Israeli perspective and requires sources for historical facts. Its content policy also sets review gates for sensitive events. Found a historical error? Share the specific claim and source through the repository's historical-error report: https://github.com/eyalgolan/statecraft-israel/issues/new?template=historical_accuracy.yml

Evidence: [content policy sections 1 and 2](CONTENT_POLICY.md), [historical-error form](../.github/ISSUE_TEMPLATE/historical_accuracy.yml). This explains the process without speculating about a critic's motives, promising coverage of every event, or treating an unreviewed draft as verified history.

## Use the examples, then improve the guidance

For the next update, choose one reader need, verify one useful change, draft the copy, and attach its claim evidence and destination. Review factual accuracy, actual availability, language, image provenance and the owner's outstanding decisions before posting. AI-assisted prose remains a draft; personal review declarations belong to the contributor.

Record accepted wording and its approving PR as examples. Add another channel format or a dedicated messaging skill only when real repeated work demonstrates the need. The [glossary](UX_GLOSSARY.md), [UI guide](WRITING_UI.md) and [Hebrew guide](WRITING_UI_HE.md) bind new player-facing strings under the [22 October owner ruling](https://github.com/eyalgolan/statecraft-israel/issues/622#issuecomment-6350231000). Existing strings change in focused bilingual PRs, with the applicable guide rule named and Hebrew sent to #564. This adoption does not approve the public-copy drafts here or remove native-review flags. Public messaging should draw from verified work in the UI, event and video workflows.
