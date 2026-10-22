---
description: Draft a Statecraft: Israel video script or voice-over from a brief, with a fact sheet first.
---

You are drafting a video script for Statecraft: Israel. Follow the `statecraft-video-script` skill and the project rules in `docs/WRITING_GUIDE.md` and `docs/CONTENT_POLICY.md`.

Input: a filled-in `templates/script-brief.md`. If any field is missing, ask for it before writing.

Do this in order and show each step:

1. **Fact sheet.** List every claim the script will make in the `templates/fact-sheet.md` shape. Give each a source and URL you have opened, or mark it [S], [C] or [GAP]. Do not invent a source. Do not include a claim you cannot source. List game claims against the recorded build or the README and GDD.
2. **Quotes.** If the brief wants a quotation, fill `templates/quote-source-sheet.md` from the primary text. If you cannot find the primary text, say so and leave the quote out. Never write words for a real person.
3. **Draft** the shot table in `templates/script-template.md` form: picture, on-screen text, voice-over, source ids. Rules:
   - the narrator is a calm, well-informed cabinet secretary reading a briefing, never a trailer voice;
   - a stranger can follow every line: name every place, group and term at first mention, give scale in numbers, and put the plain fact before any image, metaphor or contrast line;
   - it is a story, not a topic: setting, problem, decision, result, retellable in two sentences, and the picture and the voice tell the same story;
   - one idea per spoken sentence, at most 18 words;
   - the hook works with the sound off;
   - second person present for the player's role, third person past for history;
   - no verdict words, no stacked triples, no stacked questions, no phrase from the "fails review" list;
   - one real event at most, dated and sourced;
   - label concept art, AI footage and AI voice on screen;
   - the call to action refers only to something that exists today;
   - no dashes.
4. **Captions.** Write the English caption cues. For Hebrew, write a native Hebrew version, not a translation, and mark it for native review.
5. **Self-check.** Run `scripts/check_script.py` on the draft and fix every error. Explain any warning you keep.

Return the fact sheet, the quote sheet if any, the shot table, the captions and the checker output. Say plainly which claims are still unverified.
