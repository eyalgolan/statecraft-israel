# WebGL read-only companion (B)

Refs #1119; draft #1230. The [implementation approval](https://github.com/eyalgolan/statecraft-israel/issues/1119#issuecomment-6360904442)
sets the scope: a visible read-only panel and the game's existing keyboard controls.
The contributor's real VoiceOver pass and a later owner decision on C remain required.

## What runs

`unity/Assets/Scripts/UI/AccessibilityCompanion.cs` reads the existing UI tree after
`Changed`, `Refused`, `TurnEnded`, click/submit/cancel/key, focus and text-change events.
Root geometry changes also request a read; that event does not propagate from descendants.
Two scheduler stages leave a UI style/layout pass before reading resolved visibility.
Requests coalesce to at most one publication per frame, and identical JSON is skipped.
Presenters and sim rules stay with the existing UI. The collector reads:

- The visible, uncovered event's title, body, office, context and ordered option buttons,
  including enabled state, previews and rejoin notes.
- The focused element's label, card title, tooltip or visible descendant text.
- The final visible toast and the uncovered turn report's title, introduction and rows.

`GameRoot.Begin` owns one collector after the presenters and startup notices, under
`UNITY_WEBGL && !UNITY_EDITOR`. `Shutdown` disposes it before rebuilding language UI
or disabling the root. Disposal cancels pending work, removes every subscription and
pushes one empty snapshot. New/load uses the existing bridge `Changed` path. The optional
JSON sink is the EditMode seam; the default transport outside WebGL is a no-op.

`AccessibilityCompanion.jslib` passes UTF-8 JSON one way to the template renderer.
The renderer mounts after `createUnityInstance` resolves; an early failed push retries
through the scheduler. No library, font, image or additional request is added.
The sync mechanism was kept after the new interleaved boot measurements below.

The renderer uses `createElement` and `textContent`, headings, paragraphs and an ordered
list. Text nodes retain direction marks and use `dir="auto"`; `lang` is limited to EN/HE.
Bad JSON or shape retains the last good panel. Only the pre-existing `role="status"`
paragraph is live, and only its text or turn changing updates it. Focus/status-only
pushes retain the event subtree, and publishing preserves `document.activeElement`.

## Layout and keyboard play

Above 700 CSS pixels, the game and companion occupy separate columns; the panel is
18–24 rem wide (26 vw between those bounds), with vertical scrolling. Below that width,
the game is above the panel. Its minimum height is the smaller of 1.5 times the viewport
width and the safe viewport height minus 3 rem. The companion targets 30 dvh with an
8 rem minimum. If those minimums do not fit, the inner `#player-regions` grid scrolls.
The outer grid keeps both regions clipped inside the hardware safe edges.

This corrects the cramped HUD in the first B phone captures. At 390×844 with 47px top
and 34px bottom insets, the canvas is 390×585; at 375×667 it is 375×538 and the inner
grid can scroll to the panel. The canvas's height preserves the existing portrait HUD
layout. The headed captures show the actual rescaled map, HUD and event card.

For the Statecraft template, `SafeArea.jslib` returns zero extra game insets because
the outer grid already owns them. Scrolling the canvas above the clipped edge must not
be mistaken for another notch. Other pages retain the canvas-relative/full-window fallback.

The [approved narrow exception](https://github.com/eyalgolan/statecraft-israel/issues/1119#issuecomment-6364098014)
adds `tabindex="-1"` to exactly `#companion` and `#player-regions`, excluding Chrome's
implicit scrolling boxes from sequential Tab navigation. There are no new controls,
key handlers, focus calls or game callbacks in the panel. A pointer click can give the
container DOM focus. In the actual EN/HE headed runs, the next Tab changed the game's
focused label, and subsequent keys answered the event and ended the turn while DOM
focus remained on that container. VoiceOver's reading cursor remains a manual check.

## Validation and source pin

The [audit](1119-b-audit.json) pins final runtime source
`aaa44832bf725afff012d1cf30eb3e06e732d3b2`, Unity 6000.3.25f1, SDK 10.0.401,
macOS 27.0.1 arm64 and Chrome for Testing 156.0.8078.4. It hashes 21 relevant source
files, including the template and all nine script `.jslib` bridges (ShareShot included),
and both complete 113-file players. Generated Unity settings were recorded and restored;
none is committed. The imported sim DLL remains pinned in the audit.

Baseline `f9d8371d693c5996399c13fe5513bd225ec2f472` is a plain merge integrating main
`a250e2954b31201a02fa0ee0cf9b74ee6e77c2ec`. This is the tested integration baseline.
Merge main again after maintainer-owned #1227 lands, then repeat affected gates.
Its latest integration remains open at `a12fd9cc3`; its branch was not changed here.
The original diagnostic's `3afb171` / `507348f` observations remain historical.

| Gzip file | Base bytes | B bytes | Change |
| --- | ---: | ---: | ---: |
| data | 25,696,308 | 25,695,678 | -630 |
| wasm | 14,343,950 | 14,343,739 | -211 |
| framework.js | 97,719 | 97,863 | +144 |

## Completed technical checks, 23 October

- Full managed check at `9fb249c8`: 323 policy, 2,443 sim and 1,525 tools passed;
  five tools skips and two passing subtests. Optional standalone Unity compile explicitly
  skipped in the clean validation checkout without a Library. The subsequent change adds
  only the two approved HTML attributes and the headed assertion; 91 focused tools/DOM
  checks pass on final `aaa44832`.
- Full unfiltered EditMode at `0b41a3a3`: 2,356 total, 2,352 passed, zero failed, four skipped;
  all four companion payload methods passed. Subsequent C# changes only reword the root
  geometry comment; executable C# and sim/content scope are unchanged. Final Native
  `ProjectSettingsTests` at `aaa44832`: six passed, zero failed/skipped. Both runs and the
  final WebGL build have zero CS warnings/errors.
- The four full-run skips are two Explicit capture/timing jobs and two optional-art
  comparisons without local sandbox/proto prefabs. None is waived or relabelled.
- Actual final-player headed EN/HE flows pass: new game, event, real budget refusal and
  End Turn at 1920×1080; 390×844 and 375×667 check safe bounds, scrolling, the overflowing
  event panel, click then Tab and subsequent keyboard event/End Turn. No sim state or
  game commands are injected. Matched base/PR 390×844 captures use the same 47/34px insets.
- All nine exact manifest goldens, smoke5, orders and ordinary New Game/End Turn checks
  pass in EN and HE on the final player. No golden/reference was re-recorded.

The first full Native XML/log remain preserved: one old template contract failed before
it was corrected for the approved game region; its save/no-alert assertions were retained.
The first fixed-coordinate start-screen timeout also remains preserved; the checker now
follows mirrored keyboard focus using ordinary Tab/Enter. The corrected EN/HE checks pass.
Old cramped B captures are retained separately from the corrected player.

Negative controls reproduce the missing renderer/payload, unsettled visibility, wrong
canvas click mapping and old inset handling. The new phone regressions fail at three
old layout sizes and after a scrolled canvas incorrectly gets 80px extra top padding;
all nine isolated geometry/bridge cases pass with the correction. Chrome's implicit Tab
stop is separately recorded in a synthetic renderer fixture, with the approved opt-out.
These technical records supply no speech or caption-panel acceptance.

## Interleaved load measurements

The [new review request](https://github.com/eyalgolan/statecraft-israel/pull/1230#issuecomment-6363640772)
was run as base, B, base, B: three fresh profiles per block, CPU 4, exclusive machine lock,
no Unity or SDK processes, and the other task's source copy finished before timing.

| Block | Player | Map median, ms | First-frame median, ms |
| --- | --- | ---: | ---: |
| 1 | Base | 4,344 | 16,043 |
| 2 | B | 4,216 | 15,297 |
| 3 | Base | 4,160 | 15,184 |
| 4 | B | 4,217 | 15,331 |

The previous separate-block +2,249ms first-frame gap did not recur: paired differences
are −746ms and +147ms. Pooled six-run medians are 4,198.5 → 4,216.5ms for map build and
15,229 → 15,314ms for first frame (+85ms). Both pairs pass the existing checker budget,
max(300ms, 50%). Raw samples and all four logs are in the review evidence and audit.
No cause is assigned to the old gap and no speculative scheduler change was made.

```sh
uv run tools/companion_check.py --build <B-player> --out <captures>
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build <base-or-B-player> --runs 3 --cpu 4
```

The contributor will run the [real diagnostic checklist](1119-webgl-accessibility.md#real-diagnostic-still-required)
manually at the Mac in fresh EN/HE windows, recording build, macOS, browser and VoiceOver
versions plus a caption-panel screenshot and notes for every step. Silence or an
unreachable control is a failure. Keep #1230 draft; C needs a later owner decision.
Both personal AI/licence boxes remain for the contributor. New-head CI approval is a
maintainer action and remains separate from these local technical results.
