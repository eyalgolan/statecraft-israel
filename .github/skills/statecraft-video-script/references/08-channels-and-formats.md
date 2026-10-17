# 08 Channels and formats

Status: DRAFT. Tags: [V] rechecked against the named source on 17 October 2026; see `09-source-audit.md`, [H] proposal, [Q] question for the maintainer. Social channels are not specified anywhere in the repo, so those rows are open.

| Channel | What the repo or platform says | What it means for a script | Status |
|---|---|---|---|
| **Steam store page** | Steam requires a trailer and recommends gameplay first, legible without sound. Its upload guidance prefers widescreen, high bitrate and H.264/AAC; use the current [Steamworks page](https://partner.steamgames.com/doc/store/trailer) for exact settings. A six-second micro-trailer is generated automatically from the first visible video; it cannot be customised. Custom poster images must be video frames; avoiding text is advice. | Lead with real gameplay; test without sound. Verify upload settings at export. | [V] |
| **Steam, this project's rule** | The trailer is made from the real build, which is stricter than Steam's advice (`docs/RELEASING.md`). The capture and draft cut are the pending part of #45. | No concept art or AI footage in the Steam trailer unless the owner amends the rule (#424 notes this for demo2). | [V] |
| **GitHub README hero** | A cinematic trailer, 1:23, with a caption saying it was made with AI video tools and AI music and is not in-game footage. A test enforces the disclosure (`docs/RELEASING.md`). | Cinematic is allowed here, labelled. The caption wording is fixed by the test. | [V] |
| **Website media page** | `site/public/media/` holds the trailer, the flythrough and English and Hebrew caption files. | Ship a `.vtt` in both languages with every video. | [V] |
| **Decision-card clips** | Silent AI-generated illustration loops and separate 3.5-second outcome clips, labelled in the UI, never for a Deport option (`docs/event-videos.md`). | No voice-over. No script beyond alt text and the mapping row. | [V] |
| **Social posts (LinkedIn, Slack, others)** | Not in the repo. | Assume sound off and autoplay until the maintainer says otherwise. Write the hook so it works as text and picture alone. | [H], [Q] which channels, which lengths |
| **Press kit** | A press downloads page is planned (#417). | A one-line description and honest labelling of every asset. | [V] |

## Length

No length is fixed by the repo. Starting points to test, not rules: 6 to 15 seconds for a sound-off hook, 30 to 60 seconds for an explainer, up to 90 seconds for a labelled cinematic. [H] Replace these with measured results after the first few videos.

## Labelling, by footage type

| Footage | Label |
|---|---|
| Real build | Build id and commit, as the README captions do ("Real gameplay, build 0.1.0, 2026-10-13, commit f93fd5e"). |
| Concept art | "Concept art (AI-generated)", "not the current build". |
| AI video or illustration | "AI-generated", and on a card "AI-generated illustration" while it plays. |
| Archival | Archive, date and credit. |

## Questions for the maintainer [Q]

1. Which social channels does the project post video to, and in which languages?
2. Is there a length or format the channels need?
3. Who approves a script before it is recorded, and who approves the cut?
