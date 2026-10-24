# H5 WebGL accessibility diagnostic and design proposal

Refs #1119. Working draft; real screen-reader diagnostic remains incomplete.
The owner selected the read-only companion on 23 October 2026 (see the dated update below).
The original observations and proposal below are historical. The [B implementation](1119-read-only-companion.md)
is documented separately; no completed real screen-reader test or acceptance is claimed.

## Evidence and limits

Source audit: `3afb171f3d852665cd5d22b646a03e64ed1428fc`.
Runtime: existing local player `507348f9573b1e3bd44c9b65b7649f0939b0dfd8`,
all 110 built files checked against its SHA-256 receipt before running.
The source revision includes newer main changes than this player; no runtime claim for unbuilt main.
Browser: Google Chrome for Testing 156.0.8078.4, headless, macOS 27.0.1 arm64,
1280 × 720 CSS viewport. Fresh temporary browser profile for each language.

Seven of the nine audited source blobs match runtime `507348f` exactly. GameRoot and ScreenNav differ; new end-screen behavior in `3afb171` is source-only evidence.

The English and Hebrew start screens visibly contain recording and New Game controls.
Both DOM snapshots instead contain only an unnamed canvas with `tabindex=-1` among
the queried canvas/control/role/live-region elements. Chrome's four-node AX tree
contains a named RootWebArea, an ignored node, generic container, and unnamed Canvas;
it contains no New Game or recording button. These are observations of browser
semantics, not a transcript of assistive-technology speech.

**Initial VoiceOver status, 21 October 2026: NOT RUN.** At that point native-control
inventory reported the Mac locked and automatic unlock unsuccessful; manual unlock
was requested. That lock was subsequently resolved, as recorded below. Do not convert
these DOM/AX observations or the synthetic fixture into a VoiceOver pass.
A separate ordinary-input EN run recorded New Game, skipping the first-turn guide, the One Army event card, Tab visibly focusing its first option without choosing, Escape returning to Decision waiting, and one End Turn showing the turn report. All six DOM/AX snapshots still contain four AX nodes and an unnamed canvas. The report contains real result text visually but no corresponding AX text. No refusal was triggered.

### Native attempt follow-up, recorded 22 October 2026

The [21 October follow-up record](https://github.com/eyalgolan/statecraft-israel/pull/1230#issuecomment-6309041725)
reports an actual attempt after the contributor unlocked the Mac. This used the same
`507348f9573b1e3bd44c9b65b7649f0939b0dfd8` player, with all 110 files reverified,
on macOS 27.0.1 (26A434), ordinary Chrome 155.0.8059.85 in a fresh Guest window,
and VoiceOver 10 (993). This browser session is separate from the headless DOM/AX
observations above.

Native Settings showed VoiceOver off, then on; its caption panel was already enabled.
Retrieving VoiceOver output timed out after 2532.8984 seconds. No speech or caption
transcript was obtained, and only the English loading screen was observed: no ready-game
speech observation or completed fresh EN/HE scenarios. This is an **incomplete attempt**,
with no inference of silence or accessibility success. VoiceOver was restored to its
original off state, the Guest window closed, and the owned local server stopped; all
three cleanup actions were verified. No product source changed.

Unlock is no longer the outstanding requirement. Actual reader output and completion
of the real diagnostic below remain required.

Focus name, event narration, refusal narration, and turn-result narration require the
real diagnostic below before this issue can be completed.

### Owner decision, recorded 23 October 2026

The owner selected **B: read-only companion**, recorded in [#1119](https://github.com/eyalgolan/statecraft-israel/issues/1119#issuecomment-6359490836)
and [#1230](https://github.com/eyalgolan/statecraft-israel/pull/1230#issuecomment-6359490537).
The approved next milestone is an HTML panel beside the game that mirrors the current
event title, text and choices, the current focus and the status. Play continues through
the existing game keyboard controls. Whether to proceed to an operable companion (C)
is decided after a real VoiceOver test of B. The design-choice gate for B is resolved.
The [implementation and current audit](1119-read-only-companion.md) are separate from
this historical proposal; the real diagnostic remains incomplete.

The integration audit below remains pinned to `3afb171` and runtime `507348f`. Main
`460a8500088331dc35f3da662b0f1316619e0928` also contains `UI/ShareShot.jslib`,
which was absent from that audit. The new [B audit receipt](1119-b-audit.json) pins the
tested source and player, including ShareShot and every other script `.jslib`. The
newer bridge and template are not covered by the old runtime observations below.

## Verified code integration points

Paths and symbols verified in the named source tree, not inferred from old audit line numbers:

| Source | Actual behavior | Consequence for a future bridge |
| --- | --- | --- |
| `unity/Assets/WebGLTemplates/Statecraft/index.html`, body and `start` | Canvas has no accessible name; loading/error are plain divs; successful loader promise discards its instance | Add a separate semantic root and capture the instance only if the owner chooses callbacks; keep boot failures accessible too |
| `UI/GameRoot.cs`, `Begin`, `SwitchLanguage`, `Shutdown`, `OnDisable` | Begin shuts down prior presenters; language switch rebuilds over the same game and queued orders | Root lifetime needs a new generation even when session identity stays the same; dispose old bindings before rebuilding |
| `UI/EventCardPresenter.cs`, `Render`, `FocusCard`, `Choose` | Formats real localized title/body/office/variant/objective; rebuilds option buttons; focuses card before choices; Queue remains the command path | Read final presented strings and actual enabled state after rendering; do not duplicate localization, pricing, or select the first option by default |
| `UI/SimBridge.cs`, `Queue`, `EndTurn`, `Replace` | Queue can raise Refused without Changed; EndTurn raises Changed before TurnEnded; Replace drops queued orders | Do not subscribe only to Changed; distinguish queued choices from completed turns; replace clears old DOM state |
| `UI/ToastPresenter.cs`, `Show`, `Append`, `Refuse`, `Clear` | Combines localized refusal lines, save warnings, and turn rejections; clears on Changed | Prefer the final presented message as the announcement source so appended save warnings and localized prefixes are retained |
| `UI/ScreenNav.cs`, `DisableCovered` | Start, event, screens, report, outcome and end-screen classes determine enabled panels | Mirror effective visibility/enabled state, not only local style; covered controls must disappear from DOM navigation too |
| `UI/MapFocusPresenter.cs`, `Pick`, `OnKey` | Enter/Space use the camera look-at point and the existing map-pick callback | A future map action must call the same path; naming a canvas alone does not make hex navigation accessible |
| `UI/HandCursor.jslib`, `UI/SafeArea.jslib` | Existing small `.jslib` functions use Module.canvas and document DOM | The transport pattern exists; there is no existing semantic accessibility mirror in these files |

`UI/` above is `unity/Assets/Scripts/UI/`. Nine original source blobs and their hashes
are preserved privately with the source receipt. This audit does not change any source file.

## Alternatives originally presented for owner choice

| Alternative | Benefit | Cost / unresolved requirement |
| --- | --- | --- |
| Name canvas and announce current focus/status only | Smallest notification patch; existing keyboard behavior retained | Event browsing and independent control discovery remain missing; cannot claim screen-reader operability |
| Read-only DOM companion with event body, ordered options, focus summary and status | Simple transport; screen reader can browse real text without every frame being spoken | Does not provide action parity; Unity keyboard capture and assistive-navigation interaction still require real testing |
| Opt-in DOM companion with native action controls | Can expose discovery, labels, disabled state, and operable event/turn actions | Requires explicit focus ownership, input-capture switching, callback validation and broader parity tests |

Original recommendation, before the 23 October decision: prove read-only
semantic output first, then select the smallest operable subset explicitly. A mirror
that only speaks is not a complete accessible game. Do not give the entire page
`role=application` or promise map/action parity from an output fixture.

The original choices were notification-only as a clearly limited intermediate
milestone, read-only companion as a diagnostic milestone, or an operable
subset (start/continue, event choices, End Turn, report close) with an explicit input
and focus contract. Map navigation, other panels, and media controls need a coverage
inventory; they are not silently covered by that subset. The owner selected B above.
#1116/#890/#1011 claims remain intact.

## Proposed payload and transport contract

This is the original proposal, not B's implemented protocol. The [23 October implementation approval](https://github.com/eyalgolan/statecraft-israel/issues/1119#issuecomment-6360904442)
replaced it with a single read of the UI tree, no generations or sequence numbers,
and disposal that cancels scheduled work and clears the panel. B has no action callbacks.

After approval, C# can serialize a versioned plain-text snapshot; one guarded WebGL
`.jslib` entry decodes the UTF-8 string and passes it to a root-scoped DOM renderer.
No per-frame update or simulation rule change. Publish after presenters settle or
from narrow presenter hooks, with tests proving their ordering. A root-level Changed
handler alone can read presenters too early and miss Refused and non-bridge notices.

```json
{
  "version": 1,
  "rootGeneration": 12,
  "sessionGeneration": 4,
  "sequence": 39,
  "language": "he",
  "screen": "event",
  "focus": {"id": "event-card", "label": "<final localized card title>", "role": "group"},
  "event": {"id": "<actual id>", "title": "<final localized title>",
    "body": "<final localized body>", "options": [
      {"id": "<actual option id>", "label": "<presented label including cost>", "enabled": true}]},
  "status": {"id": "root12-message8", "kind": "refusal", "text": "<presented toast>"}
}
```

These are schema placeholders, not historical content or agreed new player strings.
Separate root generation covers language rebuilds; session generation covers new/load;
sequence rejects old updates. IDs are tokens, never HTML selectors or executable names.
Reject unknown versions, stale generations, malformed fields and non-finite counters
before any DOM mutation. Size limits in the isolated fixture are defensive test values,
not a product truncation policy: an implementation must define limits from actual
content and report oversize payloads without silently dropping event text.

The WebGL-only guard must leave desktop/editor behavior unchanged. Transport errors
must not throw through a command or alter deterministic state. Scope DOM and instance
references to this player; handle initialization, loader failure, disabled bridge,
multiple roots, disposal and repeated mounting. Unity's documented SendMessage accepts
only a GameObject method with zero arguments or one string/number; ordinary presenter
methods are not directly callable from it. Any future action receiver therefore needs
an explicitly owned scene component, narrow allowlisted actions and validation of
generation, current event/option and enabled state, through the existing command path.
The current template does not retain `unityInstance`; callback operability is unproved.

## Focus, reading order and announcements

Keep the semantic companion outside the canvas and reachable in normal DOM reading
order: heading/screen, current focus summary, event title, context/body, options in
source order, current status/result. Use ordinary headings/paragraphs/lists; do not hide
the companion with `display:none`, `hidden`, `visibility:hidden` or `aria-hidden`.
The owner selected a visible companion beside the game on 23 October. Its reading
position must also be understandable to sighted keyboard users; review the presentation
with matched screenshots and the real reader diagnostic.

Notification-only mode never moves document focus when UITK focus or a status changes.
Status uses a pre-existing `role=status` region and plain text. Announce discrete
refusal/result events, not every Changed render, map hover or resource tick. Preserve
distinct repeated refusals via message identity and separated DOM updates; verify
actual speech rather than relying on mutation timing. Do not focus status regions.
Cancel pending callbacks when a newer message or generation supersedes them.

If native DOM actions are approved, define a single active input owner: DOM navigation
must not simultaneously trigger Unity Tab/Enter/Space. Suspend Unity page keyboard
capture when the companion owns input and restore it on an explicit return-to-game
action. Prototype and test that with the pinned engine before choosing the API.
Synchronize corresponding UITK and DOM focus without feedback loops or auto-selecting
an event option; opening/closing/covering screens must preserve the same enabled gates
and expected return target. Do not put DOM nodes inside an aria-hidden ancestor or
leave a focusable control for a covered UITK element.

Set language/direction from the current UI language; retain final per-field fallback
text. Mixed-direction fields need an explicit per-field language/direction policy,
not an assumption that every Hebrew-mode field is Hebrew. Render all content and
error/status strings with `textContent`; do not use innerHTML, eval or inline handlers.

## Bounded feasibility evidence

[`h5-fixture/index.html`](h5-fixture/index.html) is a standalone **synthetic** output fixture under documentation, outside the
Unity project and WebGL build. No .jslib, C# transport, callback action or product entry
point is installed. The original task-local diagnostic recorded browser semantics separately
from its fixture assertions; [the sanitized records](h5-fixture/evidence.json) preserve the
DOM/AX data, action sequence, source hashes and original screenshot hashes. The original
screenshots remain diagnostic evidence; they are not shipped player assets. The first run passed 17 assertions, including literal
markup handling, preserved option order, malformed/old-session/duplicate/schema
rejection, language/direction, focus retention, repeated text repopulation, and pending
callback cancellation on teardown/new session. Repopulating a status node is not
proof that a screen reader announces it twice. The fixture's single epoch deliberately
tests lifetime behavior, not the full proposed two-generation production protocol.

Remaining implementation evidence after design approval: native/Unity transport tests,
no-op behavior outside WebGL, actual serialization/string fallback and exact presenter
ordering, keyboard capture handoff, event coverage and repeat mount/unmount, no stale
loaded-session or language payloads, real VoiceOver/browser tests, ordinary keyboard
regressions, then existing deterministic/native/WebGL gates. No implementation estimate
or complete accessibility claim follows from the fixture.

To reproduce the isolated output checks from the repository root:

```sh
python3 -m http.server 8891 --bind 127.0.0.1 --directory docs/diagnostics/h5-fixture
```

Open `http://127.0.0.1:8891/verify.html` and press **Run isolated checks**.
The verifier adds coverage for late disposed-root updates, superseded announcements
and duplicate mounts, and checks atomic rejection against the entire prior DOM.
It closes each synthetic root after testing and can be run again on the same page.
This is not connected to the game and does not exercise a Unity runtime or screen reader.

## Real diagnostic still required

The contributor runs this manually at the Mac, with VoiceOver's caption panel visible
and a caption-panel screenshot plus notes at every step. Do not automate VoiceOver.
In a fresh isolated browser session, record macOS version/build, VoiceOver version/build, browser version
and exact player source; enable VoiceOver with visible native controls and retain observed transcript
or equivalent actual output. In fresh EN and HE profiles check: discover and name New
Game; focus through real controls; read the current event's complete title/body and
ordered choices; trigger a real unavailable action and record refusal; finish a turn
and read result; close/open screens; start a new session and switch language. Record
what is silent or unreachable as a failure, never fabricate the missing spoken text.
The earlier lock is resolved; obtaining actual reader output and finishing these
scenarios remain outstanding. Neither source inspection nor Chrome AX substitutes.

## Primary references checked 21 October 2026

- [Unity 6000.3: call JavaScript from C#](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-js-to-unity.html): `.jslib` scope and UTF-8 string transport.
- [Unity 6000.3: call C# from JavaScript](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-unity-to-js.html): GameObject/SendMessage restrictions.
- [WAI-ARIA status role](https://www.w3.org/TR/wai-aria-1.2/#status) and [ARIA22 technique](https://www.w3.org/WAI/WCAG21/Techniques/aria/ARIA22): polite status semantics and avoiding unsolicited focus changes.
- [WAI-ARIA APG keyboard interface](https://www.w3.org/WAI/ARIA/apg/practices/keyboard-interface/): explicit keyboard behavior, tab sequence and focus management.

Design recommendations are inferences from the audited code and these references.
