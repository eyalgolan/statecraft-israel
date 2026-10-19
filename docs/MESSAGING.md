# Messaging and social copy proposals

Draft for maintainer review, checked on 19 October 2026 against main at `e1251750` and release `v0.1.2`. This proposes a reusable way to write public copy and supplies examples to review. It does not establish new channels, a posting schedule or an approved campaign.

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

## Initial claim register

| Claim or material | Evidence checked on 19 October 2026 | Safe use and remaining check |
| --- | --- | --- |
| Turn-based strategy about governing Israel, starting on 15 May 1948 | [README](../README.md), [content policy section 1](CONTENT_POLICY.md#1-the-perspective-is-israeli-and-it-says-so) | Use the stated player role and framing. Source any additional historical explanation separately. |
| How to play screen covers monthly turns, Attention, training and delegation | [v0.1.2 release notes](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.1.2), Added | Can describe this as part of v0.1.2; do not infer that all GDD systems are complete. |
| A turn report explains changed top-bar numbers and links to Why | [v0.1.2 release notes](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.1.2), Added | Useful player-facing release benefit. Verify an accompanying screenshot in that build. |
| Downloadable WebGL build | [v0.1.2 assets](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.1.2): `statecraft-israel-v0.1.2-web.zip` | This release lists one web archive. It does not establish a one-click hosted destination or desktop installers. |
| English and Hebrew interface language choice | [v0.1.2 release notes](https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.1.2), Settings | Notes explicitly say event cards remain English only. Avoid claiming the entire game is translated. |
| The full MVP design | [GDD](GDD.md), [README](../README.md) | Design scope and current implementation must be distinguished; no invented completion date. |
| Historical and sensitive content standards | [content policy](CONTENT_POLICY.md), especially sections 1 and 2 | Explain the rules. Do not claim a particular event exists or has passed review without checking its content record. |
| Screenshots, concept art and cinematic trailer | [press page source](../site/src/content/en/press.md), [asset provenance](../ASSETS.yaml), [licensing](../LICENSING.md) | Use only a verified capture as gameplay. Preserve concept/cinematic labels, provenance and credits; check the individual asset's terms. |

Recheck this register before reusing dated examples. A changelog item saying that a model or system is imported but not used does not support saying the player can interact with it.

## Existing copy to review next

The [English press factsheet](../site/src/content/en/press.md) describes building from source and says there is no hosted build. It omits the v0.1.2 release download. The release archive supports adding a download route; it does not resolve whether a hosted build exists. Ask the owner for the current preferred player destination before replacing the hosted-build statement.

Candidate English factsheet lines:

> **Status:** A playable prototype. Release notes and downloadable builds are available on GitHub Releases.
>
> **Platform:** Web browser (WebGL). Release v0.1.2 includes a downloadable web build.
>
> **Languages:** English and Hebrew interface options. Event cards are currently in English only.

Evidence: the register above. These are review candidates, not changes to the live site. Before applying them, check the current release, agree the destination and pair them with a native-reviewed Hebrew update. Preserve the rest of the factsheet's verified licence, credit and contact information.

The [Steam description draft](steam/store-page-draft.md) records its own 14 October baseline and open confirmations. Audit it against the chosen launch build before use. Do not convert its planned features into shipped claims or infer a live store page from a repository draft. The owner chooses the store status and launch promise.

## Proposed social copy

These are English drafts for review, not published posts. A channel's final length, destination and any Hebrew adaptation need a separate check.

### Release: help with the monthly loop

> Statecraft: Israel v0.1.2 adds a How to play screen covering monthly turns, Attention, training and delegation. A new turn report also explains changes to your top-bar numbers. Read the release notes and find the downloadable web build: https://github.com/eyalgolan/statecraft-israel/releases/tag/v0.1.2

Evidence: v0.1.2 Added entries and its web archive. Intended next step: inspect the release. Attach only a capture verified against this build; the archive link alone is not an instant-play button.

### Contribution: improve what the player reads

> Help make Statecraft: Israel clearer to play. Writing and design contributions can improve event choices and interface text. Historical claims need sources, and Hebrew wording needs native review. Start with the contribution guide and choose an open task: https://github.com/eyalgolan/statecraft-israel/blob/main/CONTRIBUTING.md

Evidence: [CONTRIBUTING](../CONTRIBUTING.md), Ways to help and Picking an issue; [content policy](CONTENT_POLICY.md). Check the task is still open before replacing this general destination with a specific issue. Do not imply a volunteer role has been assigned.

### Research: explain the standard

> Statecraft: Israel uses an Israeli perspective and requires sources for historical facts. Its content policy also sets review gates for sensitive events. Found a historical error? Share the specific claim and source through the repository's historical-error report: https://github.com/eyalgolan/statecraft-israel/issues/new?template=historical_accuracy.yml

Evidence: [content policy sections 1 and 2](CONTENT_POLICY.md), [historical-error form](../.github/ISSUE_TEMPLATE/historical_accuracy.yml). This explains the process without speculating about a critic's motives, promising coverage of every event, or treating an unreviewed draft as verified history.

## Use the examples, then improve the guidance

For the next update, choose one reader need, verify one useful change, draft the copy, and attach its claim evidence and destination. Review factual accuracy, actual availability, language, image provenance and the owner's outstanding decisions before posting. AI-assisted prose remains a draft; personal review declarations belong to the contributor.

Record accepted wording and its approving PR as examples. Add another channel format or a dedicated messaging skill only when real repeated work demonstrates the need. Keep [UX writing proposals](https://github.com/eyalgolan/statecraft-israel/pull/970) and event/video writing workflows focused on their own player or viewer tasks; public messaging should draw from verified work they produce.
