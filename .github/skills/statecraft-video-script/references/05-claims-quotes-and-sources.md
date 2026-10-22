# 05 Claims, quotes and sources

Status: DRAFT. Tags: [V] already a project rule, [H] proposal.

## One rule

A script line with a fact, a number, a date, a name or a quote carries a source id in the shot table. If it has none, it is not ready. This is the same standard as event content: "If a sentence is not directly supported by a source, it is not ready for the repo" (`docs/WRITING_GUIDE.md` section 7). [V]

## Two kinds of claim

| Kind | Source | Example |
|---|---|---|
| **History** | An external source with a URL and a check date, tagged as the research notes tag them: [V] opened and read, [S] single or secondary source, [C] sources conflict, [GAP] not found. | A date, an office, a number, a decision. |
| **The game** | The repo and the recorded build: the commit id, the README "What works now" list, the GDD marking. Planned features are labelled "in development". | "You appoint real ministers." "The Why? panel explains each change." |

A game claim that is only planned is never spoken as if it were playable. [V, the Steam draft keeps "in the build today" and "in development for launch" apart]

## The fact sheet

Every claim goes on `templates/fact-sheet.md` before a line is written: id, claim in one sentence, source, URL, checked date, tag, who checked. The shot table refers to the id. A conflict stays a conflict: say "sources differ" or leave the number out.

## Quotes

The sheet distinguishes these classes:

- **Verbatim.** The exact words, in the original language, with the translation if any.
- **Excerpt.** Verbatim, with an ellipsis where text is cut. The ellipsis must not change the meaning. The sheet keeps the full sentence.
- **Verbatim excerpt of a named official translation (original Hebrew).** Exact wording from that translation, not the speaker's original English. Name the institution or translator and record the translation source and year where supplied.
- **Paraphrase.** Not in quotation marks. Not attributed with "said" unless the source supports it.

For every quote on screen or in voice-over, `templates/quote-source-sheet.md` records: the exact text as shown, the speaker, the role they held at the time, the date, the occasion or document, the original language, the translator and the translation source, the URL, the check date, and a status. A quote card shows who, when and where, not only a name and a title. Under the [maintainer decision on #648](https://github.com/eyalgolan/statecraft-israel/pull/648#issuecomment-6195693081), translators are recorded in the source notes and captions, not on the cards. The source notes and captions name the translator or translating institution. Proposal [H]: also include the translation year there; record an unknown year as a gap rather than inventing it.

Do not shorten a sentence so that it says something the whole sentence does not. Do not merge two statements into one quote. Do not use a quote from a secondary source without finding the primary text.

A set of quotes from several leaders is an editorial choice. Record why each one is there.

## Worked cases: the retained v1 trailer

These record editorial decisions, not new primary-source verification. See [the source-sheet PR](https://github.com/eyalgolan/statecraft-israel/pull/648) and [the closed trailer revision](https://github.com/eyalgolan/statecraft-israel/pull/650).

- **Begin:** the source sheet records a three-part source line, while the v1 card retains two parts. The owner decided on 16 October 2026 to keep v1. Preserve the full source sentence, the difference and the owner decision in the notes. Do not silently describe the card as the complete source line or change the card to the rejected v2 wording.
- **The Altneuland motto:** the retained card reads "If you will it, it is no dream." The source sheet records that this rendering has no named published translator established there. The owner kept it in v1. Record that limitation and decision; do not substitute the proposed Levensohn wording or invent a translator attribution. That proposal was superseded.

An owner decision to retain a card does not verify its wording against a source or close a translation gap.

## Real people

- No invented speech and no private thoughts for a real person. [V]
- No AI likeness of a real person is presented as that person. The repo already bars the AI likenesses in the promo set from the game (`docs/CONTENT_POLICY.md` section 4, as cited in #45). [V]
- An archival recording is used only with its archive, date and rights recorded, as for the Declaration recording (`docs/declaration-recording.md`). [V]

## Numbers

Round only when the source rounds. Give a range or "sources differ" for a contested figure (the war research notes already do this). Casualty figures carry their source and their date. [V pattern]

## Sensitive events

An event tagged sensitive in the game stays tagged in a script about it. A script about one is reviewed by the owner before release, as sensitive clips are (`docs/event-videos.md`). [V]

## Records

Keep the script, the fact sheet, the quote sheet and the build id next to the final cut. Anyone should be able to check a line in a minute.
