# Decision-card video: illustration loops and outcome clips

On some decision cards, an **Illustration** switch over the photograph's top right corner plays a short, silent,
AI-generated loop in the 16:9 media header. Owner rulings of 2026-10-13 cover it (`reports/Video at decision
moments.md`, the "Full plan"; plan: `docs/superpowers/plans/2026-10-13-decision-videos.md`).

## Rules

- **The archival photograph comes first.** The card always opens on the photograph. The switch shows only for an event
  that has both a photograph and a clip, and only while the photograph is shown. A clip is never the only picture of a
  real event (`EventImageCatalog`; GDD: authentic images for history).
- **The clip is labelled in the card, not in the file.**
  - While it plays, "AI-generated illustration" replaces the photograph's credit, and the clip's own alt text becomes
    the header's tooltip.
  - The files carry **no burned-in caption**. Owner ruling of 14 October 2026, relayed by the coordinator, after
    watching the three sensitive clips: "the 3 ilustration clips are great but no need to say there were created by
    ai". Until then each file carried a small "AI-generated · נוצר בבינה מלאכותית" label; every clip was re-encoded
    from its promo shot without it, and nothing else changed. The Dreamina shots keep Dreamina's own small "AI" badge
    in the top left corner, which is part of the shot: owner ruling "Keep the badge", 2026-10-14 (`docs/oss/DECISIONS.md`
    G5). The caption ruling is G4.
  - The provenance stays: every row of the mapping and every clip's `ASSETS.yaml` row says `ai_generated: true`, and
    `docs/ai-disclosure.md` lists them.
- **Silent.** The clips have no audio stream (`tools/encode_event_videos.py` refuses one).
- **Opt-in.** A card's loop plays only after the player presses the switch, so it needs no separate pause button and no
  reduced-motion setting. Pressing the switch again stops it. (The outcome clips below play at End turn without a
  switch, once, for 3.5 s: a click, Enter or Space skips one, and Esc skips them all.)
  - The choice holds for the session: later cards open on their clip until the player switches back.
  - It is not saved, and every launch starts on the photograph.
- **Deport never gets any video** (GDD owner ruling 1). `EventVideoMapping.Problems` refuses a row for an event with a
  deport option.
- **No recognisable real person** in any clip. Every frame sheet was checked before the clip was committed.
- **Sensitive events.** A clip on an event with a sensitive tag is `sensitive: true`. A `-shareable` build refuses it
  until the owner sets `sensitivity_reviewed: true` (`Build.ShareableRefusal`). That applies to `ten_days` (J09) and
  `operation_yoav` (W04a), and to the `operation_horev` outcome clip (A05). The owner reviewed all three on 14 October
  2026 (relayed by the coordinator), so a shareable build may carry them.
- **Rights:** owner ruling D-V (`docs/oss/DECISIONS.md`, merged in #449), "no need to check, just use them". Every clip has an
  `ASSETS.yaml` row with `ai_generated: true`, the generator and the date, and those rows feed `docs/ai-disclosure.md`.

## Outcome clips at End turn (phase 3)

When a turn runs, every event the player answered with its **historical** option, if the turn applied the order and the
event has an outcome clip, plays that clip once, in the middle of the screen, before the game goes on (`OutcomePresenter`).

- **Only for an order.** A clip plays only for the historical option the player chose. A delegated answer gets none,
  and so does a counterfactual option (there is no footage of what did not happen). `EventVideoMapping.OutcomeProblems`
  refuses a row for a non-historical option, and any row that involves Deport.
- **3.5 s, silent.** The caption is worded as the order, "Your order: …", because the clip illustrates the order and
  does not show its outcome.
  - A click, Enter or Space (or a gamepad's Submit) skips the clip. Esc (or Cancel) skips all of the turn's clips.
  - A screen open at End turn (Cabinet, Budget, Defence, Why) closes when the clip starts. The clip is drawn over
    everything but the toast, and while it plays the decision card, the top bar and the panels wait, so no order can be
    given under it.
  - It has its own player, separate from the card's loop.
  - The keyboard focus goes to the clip while it plays, then back to the open decision card, or to where it was.
- **The steadiest window.** AI shots often morph in their first frames, so each clip keeps the steadiest 3.5 s of its
  shot, not the first 3.5 s. "Steadiest" means the least frame-to-frame change, and above all no sudden jump in it. The
  search is `tools/encode_event_videos.py --steadiest`, and the row records `start_frame` and `frames` so the cut
  reproduces. The 60 fps Dreamina files repeat frames, so they are decimated first (`decimate: true`).
- **The clips:**
  - maabarot_policy / maabarot: A08, a transit camp;
  - hula_drainage / continue_works: W08, the drained valley;
  - operation_horev / launch: A05, jeeps and an armoured carrier crossing the Negev. This one is sensitive; the owner
    reviewed it on 14 October 2026. Its vehicles (jeeps, a tracked carrier) match the 8th Brigade's tanks,
    jeeps and APCs (en.wikipedia "8th Armored Brigade (Israel)", checked 2026-10-14), and it shows no flag or marking.
- **Dropped in review (2026-10-14):** first_truce / accept (A02, the Burma Road convoy of June 1948, a poor picture for
  a truce whose terms bar bringing in men and materiel) and law_of_return / enact (K04b, not in the plan's list and with
  no recorded ruling). A replacement needs the owner's word before anything new is generated.

## Where things are

| What | Where |
|---|---|
| The mapping: event (and, for an outcome, option and window), file, promo shot and its SHA-256, generator, date, alt text, sensitivity flags | `unity/Assets/Resources/EventVideos/event-videos.yaml` |
| The clips (960x540 H.264, about 0.4 to 1.5 MB each) | `unity/Assets/StreamingAssets/video/<event>.mp4`, and `<event>-outcome.mp4` for the outcome clips |
| The encode, pinned (no burned-in caption) | `tools/encode_event_videos.py` |
| The player: a lazy VideoPlayer playing a URL into a 960x540 RenderTexture; one for the card's loop and one for the outcome clips | `CardVideo` |
| The outcome clips at End turn | `OutcomePresenter` |
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
