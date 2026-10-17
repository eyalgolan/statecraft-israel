---
name: statecraft-video-script
description: 'Write, review and source scripts for Statecraft: Israel video and narration (trailers, teasers, site and social clips, captions, voice briefs). Use when drafting a video script, a voice-over, on-screen text or captions, checking a script for voice, claims and quotes, or preparing an AI voice brief and its disclosure.'
argument-hint: 'draft script / review script / voice brief / captions / quote source sheet'
user-invocable: true
disable-model-invocation: false
---

# Statecraft video script

Version 0.4, local draft revision for issue #639. Maintainer decisions on location, narration and captions are recorded in the references. Additional recommendations remain proposals. The current design of record is `docs/GDD.md` v0.2; do not present later-era research as playable MVP content.

This skill extends `statecraft-writing-voice` to spoken and edited video. It keeps the same voice (direct, grounded, sober) and the same rules (every fact sourced, no invented quotes, the Israeli framing in `docs/CONTENT_POLICY.md` section 1). It adds what a script needs that a decision card does not: a viewer who may be watching with no sound, a voice that is heard once and cannot be re-read, picture and words that must carry different information, and captions in two languages.

## When to use

- drafting or rewriting a trailer, teaser, site or social clip, or any voice-over
- writing on-screen text and its captions
- reviewing a script someone else (or an AI tool) produced
- preparing a voice brief for a human or synthetic narrator
- building a quote source sheet for a video that already exists

## The rules that do not bend

1. Every historical fact, number, date, name and quote in a script has a source id in the shot table. No source id, no line.
2. No invented quotes. The sheet distinguishes original-language quotations, excerpts, named official translation excerpts and paraphrases. A narrator never speaks as a real, named person.
3. The framing is Israeli and the video says so where it matters. It is not neutral and it does not present contested narratives as parallel accounts. Soft wording that blurs who decided what is not the same as framing, and it is not allowed.
4. Say what the video is. Concept art, AI footage and AI voice are labelled as such. Gameplay footage is from the real build, with the build id recorded.
5. A video makes sense with the sound off, and every spoken or on-screen word is in the captions.
6. A cold viewer can follow it. Assume the viewer knows nothing about the period, the place or the people. Every line says what happened, where, when and why it matters, in words a stranger understands. Explain first, then evoke. No riddle, aphorism or contrast line ("the lake was in the north, the thirst was everywhere else") stands in for information.
7. It is a story, not a topic. A viewer can retell it in two sentences: who faced what problem, what was decided or done, and what it cost or changed. Picture and voice tell the same story.

## Workflow

1. **Brief.** Fill in `templates/script-brief.md`: channel, audience segment, one message, one thing the viewer should do next, length, languages.
2. **Facts first.** List every claim in `templates/fact-sheet.md` with a source and a check date before any line is written.
3. **Draft** in the shot table (`templates/script-template.md`): picture, on-screen text, voice-over, source ids. Use `references/04-storytelling.md` for structure and `references/02-voice-tone-style.md` for the voice.
4. **Check.** Run `scripts/check_script.py` on the file. Fix every flag or write down why it stays. Then run the cold-viewer test: read each voice-over line to someone with no background and ask what they learned. If the answer is "nothing", rewrite it.
5. **Review** against `templates/review-checklist.md`.
6. **Voice and disclosure.** If any audio is synthetic, fill in `templates/voice-brief.md` and the `ASSETS.yaml` row (`references/06-ai-voice-music-and-disclosure.md`).
7. **Captions and Hebrew.** Write captions in English and Hebrew (`references/07-captions-accessibility-hebrew.md`). A native reader signs off the Hebrew.
8. **Record** the script, the fact sheet and the build id next to the final cut.

## References

- [Audience and purpose](references/01-audience-and-purpose.md)
- [Voice, tone and style](references/02-voice-tone-style.md)
- [Language and terminology](references/03-language-terminology.md)
- [Storytelling](references/04-storytelling.md)
- [Claims, quotes and sources](references/05-claims-quotes-and-sources.md)
- [AI voice, music and disclosure](references/06-ai-voice-music-and-disclosure.md)
- [Captions, accessibility and Hebrew](references/07-captions-accessibility-hebrew.md)
- [Channels and formats](references/08-channels-and-formats.md)
- Existing project rules: `docs/WRITING_GUIDE.md`, `docs/CONTENT_POLICY.md`, `.github/skills/statecraft-writing-voice/`

## Templates and tools

- `templates/script-brief.md`, `script-template.md`, `fact-sheet.md`, `quote-source-sheet.md`, `voice-brief.md`, `pronunciation-list.md`, `review-checklist.md`
- `scripts/check_script.py`: flags generic and vague phrasing, ambiguous first references, lines with no concrete detail, dashes, long spoken sentences, unsourced lines and quotes, empty rows, silent gaps and timing problems
- `prompts/draft-script.prompt.md` and `prompts/review-script.prompt.md`: the same rules, written for an AI assistant
- `examples/example-46s-one-decision.md`: a worked example that uses only claims the repo already sources
- `examples/case-study-before-after.md`: a vague script, why it fails, and a self-contained rewrite

Source-check results and remaining production limits: [source audit](references/09-source-audit.md).
