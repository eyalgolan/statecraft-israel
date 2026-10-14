# Decision-card illustration loops

On some decision cards, an **Illustration** switch over the photograph's top right corner plays a short, silent,
AI-generated loop in the 16:9 media header. Owner rulings of 2026-10-13 cover it (`reports/Video at decision
moments.md`, the "Full plan"; plan: `docs/superpowers/plans/2026-10-13-decision-videos.md`).

## Rules

- **The archival photograph comes first.** The card always opens on the photograph. The switch shows only for an event
  that has both a photograph and a clip, and only while the photograph is shown. A clip is never the only picture of a
  real event (`EventImageCatalog`; GDD: authentic images for history).
- **The clip is labelled twice.**
  - While it plays, "AI-generated illustration" replaces the photograph's credit, and the clip's own alt text becomes
    the header's tooltip.
  - The file carries a small burned-in label, "AI-generated · נוצר בבינה מלאכותית", so it says what it is even when it
    is downloaded on its own. The Dreamina shots also keep Dreamina's own "AI" badge (D-D).
- **Silent.** The clips have no audio stream (`tools/encode_event_videos.py` refuses one).
- **Opt-in.** Nothing plays until the player presses the switch, so the loop needs no separate pause button and no
  reduced-motion setting. Pressing the switch again stops it.
  - The choice holds for the session: later cards open on their clip until the player switches back.
  - It is not saved, and every launch starts on the photograph.
- **Deport never gets any video** (GDD owner ruling 1). `EventVideoMapping.Problems` refuses a row for an event with a
  deport option.
- **No recognisable real person** in any clip. Every frame sheet was checked before the clip was committed.
- **Sensitive events.** A clip on an event with a sensitive tag is `sensitive: true`. A `-shareable` build refuses it
  until the owner sets `sensitivity_reviewed: true` (`Build.ShareableRefusal`). Today that applies to `ten_days` (J09)
  and `operation_yoav` (W04a).
- **Rights:** owner ruling D-V (`docs/oss/DECISIONS.md`, merged in #449), "no need to check, just use them". Every clip has an
  `ASSETS.yaml` row with `ai_generated: true`, the generator and the date, and those rows feed `docs/ai-disclosure.md`.

## Where things are

| What | Where |
|---|---|
| The mapping: event, file, promo shot and its SHA-256, generator, date, alt text, sensitivity flags | `unity/Assets/Resources/EventVideos/event-videos.yaml` |
| The clips (960x540 H.264, about 0.4 to 1.5 MB each) | `unity/Assets/StreamingAssets/video/<event>.mp4` |
| The encode, pinned, and the burned-in label | `tools/encode_event_videos.py`, `tools/event_videos/ai-bug.png` |
| The player: one lazy VideoPlayer playing a URL into one 960x540 RenderTexture | `CardVideo` |
| The switch | `EventCardPresenter.ApplyIllustration` |

The mapping is **not game content**: the sim never reads it, and the goldens and the content hash cannot move for it
(`EventVideoTests.TheMappingIsNotGameContent`).

## Colour

In the game a clip looks slightly less saturated than its file: on the WebGL build, measured on the first-truce clip,
the mean HSV saturation was 71 in game and 83 in the file, and contrast was the same. This is the known colour-space
effect of Unity's Web video path: `Video.js` copies each frame into a texture and converts it for the project's Linear
colour space. It is accepted as is.

## Off in automated runs

Video is off in the smoke run, the golden check, `?shot`, the frame and pixel-error profiles, `?units` and
`?borderproof` (`CardVideo.ForcedOff`). In those runs the card has no switch at all, so captures and checks never depend
on decoding.

## When a clip fails

On the Web, a clip plays only from a URL in StreamingAssets, fetched when the player presses the switch and never at
boot. If it errors, or is not ready within 8 s, the card logs `VIDEO failed <url>: <why>`, puts the photograph and its
credit back, and drops the switch for that event for the rest of the session.

## Adding a clip

1. The event must have an archival photograph (`docs/event-images.md`), a shot with no recognisable real person, and no
   Deport option.
2. Add a row to `event-videos.yaml`, then run `uv run tools/encode_event_videos.py --root <checkout with promo/>
   --only <event>`.
3. Add its `ASSETS.yaml` row, then regenerate `docs/ai-disclosure.md` with `uv run scripts/oss/ai_disclosure.py
   --output docs/ai-disclosure.md`.
