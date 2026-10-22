# Script template

Copy this file, fill in the header, and keep the shot table in this exact shape. `scripts/check_script.py` reads it.

```
Title:
Channel:
Length (seconds):
Language: en
Audience:
Build id or footage note:
```

## Shot table

Time is `start-end` in seconds. Voice-over is the spoken text, in the sentence form a person would say. On-screen text is exactly what appears. Sources are fact-sheet ids (F1, F2) and quote ids (Q1, Q2), separated by commas. Use `game:` for a claim about the build, with the commit id on the fact sheet.

| # | Time | Picture | On-screen text | Voice-over | Sources |
|---|---|---|---|---|---|
| 1 | 0-3 | | | | |
| 2 | 3-8 | | | | |
| 3 | 8-15 | | | | |

## Notes for the editor

- Music cues and where they start:
- Silence on purpose:
- Labels that must be on screen, and when:

## Captions

Written from this table into `<name>.en.vtt` and `<name>.he.vtt`. Every voice-over word and every on-screen word is in the captions.
