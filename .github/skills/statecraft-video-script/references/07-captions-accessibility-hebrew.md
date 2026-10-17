# 07 Captions, accessibility and Hebrew

Status: DRAFT. External Steam and W3C passages rechecked 17 October 2026; see `09-source-audit.md`. Tags: [V] opened and read on 15 October 2026 (source given), [H] proposal, [A] assumption to test.

## Sound off is the default case

Steamworks says a customer may watch a trailer with no audio and that the team should consider "if they can learn about it without audio" (https://partner.steamgames.com/doc/store/trailer). [V] A script is written so that the picture, the on-screen text and the captions carry the message, and the voice adds to it. The narration is never the only place a key fact appears.

## What the standard asks

- **Captions for prerecorded audio** (WCAG 2.2, success criterion 1.2.2, Level A). Captions cover dialogue, who is speaking, and meaningful non-speech sound such as music. https://www.w3.org/WAI/WCAG22/Understanding/captions-prerecorded.html [V]
- **Audio description for prerecorded video** (1.2.5, Level AA): a spoken description of important visual content. The same page notes that if all the important information in the video track is already conveyed in the audio track, no extra description is needed. https://www.w3.org/WAI/WCAG22/Understanding/audio-description-prerecorded.html [V]

What this means for a script: write the narration so it carries the important information that the picture shows, and put every on-screen word in the captions. If the finished audio carries all important visual information, the W3C guidance says additional audio description is unnecessary. Check that condition against the final edit. [H, a design choice, not a legal reading]

## The existing pattern

The trailer's caption files already note "[Music]" and repeat the on-screen text, in English and Hebrew (`site/public/media/promo-trailer.en.vtt` and `.he.vtt`). Keep that pattern, and for a narrated video add the spoken words, with the speaker named. [V]

## Caption files

- One file per language and per video: `<name>.en.vtt` and `<name>.he.vtt`.
- Cue text: spoken words first, then `[Music]` or `[No sound]` where it matters, then `On screen:` for text shown.
- Two lines at most per cue. Choose a line length after testing readability in the target player. No numerical line-length standard is adopted here.
- For a translated quote, the caption names the translator or translating institution and the translation year, matching the source notes. Record a missing year as a gap. The card carries no translator line (#648).

## Hebrew

- **Write Hebrew natively.** A Hebrew script is not a translation of the English one. It is written for a Hebrew ear, with Hebrew rhythm, and checked by a native reader.
- Use the sourced Hebrew names and titles in `content/`, the project's Hebrew terms, and the single gershayim `״` (U+05F4). [V, PR #630]
- Follow the Hebrew rules proposed in #622, including gender agreement for office titles. [H]
- Government and civic Hebrew should be plain. The National Digital Agency's language guide is the reference used for the Hebrew UI guide proposed in #622 (https://harhayeda.gov.il/guides-digital-transformation/language-characterization-guide/). [V]
- Right-to-left display: check Latin names and numerals in the player and add direction marks only where the display breaks. [V pattern, PR #543]
- A native reader signs off the Hebrew captions and any Hebrew voice, with name and date.

## Steam and several languages

Steam lets a trailer be shown by language, and the base video should be the English trailer when there are several (https://partner.steamgames.com/doc/store/trailer, Advanced Settings). [V] Plan English first, then a Hebrew cut if the maintainer wants one.

## Checklist

- Every spoken and on-screen word is in the captions.
- Music and meaningful sound are noted.
- The key message survives with the sound off.
- Hebrew captions exist and a named native reader has signed them off.
- Direction marks checked in the real player.
