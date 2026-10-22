---
description: Review a Statecraft: Israel video script, voice-over or caption file against the project rules.
---

You are reviewing a video script for Statecraft: Israel. Follow the `statecraft-video-script` skill. Do not rewrite the script unless asked. Report.

Check, and report each as pass, fail or not applicable, with the line and the reason:

1. **Sources.** Does every date, number, name and quote have a fact-sheet or quote-sheet id? Open the sources you can. Is any claim [S], [C] or [GAP] stated as plain fact?
2. **Quotes.** Is each quote verbatim, an excerpt or a paraphrase, and does the sheet say which? Does any ellipsis change the meaning? Is any private thought or feeling attributed to a real person?
3. **The game.** Does each game claim match the recorded build or the README and GDD? Is anything planned described as playable?
4. **Voice.** Does it sound like a briefing and not a trailer? Flag phrases from the "fails review" list, verdict words, stacked triples, stacked questions, long spoken sentences, mixed person or tense.
   - **Cold viewer.** Read each voice-over line as someone with no background. Flag every line that assumes knowledge ("the lake", "the camps", an aphorism that compresses a fact the viewer was never given) and every line that teaches nothing.
   - **Story.** Is there a setting, a problem, a decision and a result? Can it be retold in two sentences? Do picture and voice tell the same story? Are there unexplained gaps or empty cues?
5. **Framing and sensitivity.** Is the Israeli framing stated and not softened? Are war, terror and the Holocaust handled through facts and archival material only? Is a sensitive event flagged for owner review?
6. **Honesty.** Are concept art, AI footage and AI voice labelled on screen? Is there a real build id? Does the call to action exist today?
7. **Sound and access.** Does the key message survive with the sound off? Is every spoken and on-screen word in the captions, in both languages?
8. **Voice and disclosure.** If any audio is synthetic: is the voice a real person's likeness, is it disclosed, is there an `ASSETS.yaml` row, are README and Steam text in step, is the pronunciation list signed off?
9. **Hebrew.** Is it native and not translated line by line? Is there a named native reviewer and a date?

Run `scripts/check_script.py` and include its output. End with a short verdict: ready, ready after named fixes, or not ready, and the three most important fixes.
