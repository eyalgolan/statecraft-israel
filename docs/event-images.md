# Event-card photographs

The event card's header picture, as in the promo (`promo/final/03-gameplay-1936.png`, `05-event-transit-camps.png`), is a
real archival photograph of the event, never an AI image. 24 of the 32 events in `content/events/` have one. Seven
others show the responsible minister's portrait in the card's head, as before: `rhodes_talks`, `armistice_lebanon`,
`armistice_syria`, `sinai_withdrawal`, `education_registration_1951`, `reparations_claim` and `forex_crisis_1951`. No
free photograph that fits them was found (for `sinai_withdrawal`, see the decisions below). The eighth, the GDD objective
card `first_new_yishuv`, has no `office`, so it shows no picture at all until its photograph (Boris Carmi, June 1949, CC BY
4.0) comes in GDD Phase 1 PR 7 (#380).

Owner ruling 2026-10-11 (summarised; recorded in `docs/CONTENT_POLICY.md` section 4):
- Images of real people stay public domain or CC0, with one narrow exception: archival event photographs (not
  portraits) under CC BY 4.0, with a credit line.
- Photos are shown as authentic black and white, with a subtle warm grade and the gold frame. There is no AI
  colourising and no change beyond grade, crop and resize. The adaptation is recorded as "graded and cropped".
- GPO photographs are refused, as are files whose only licence is PD-Israel or PD-India, and share-alike files.
- Each photograph gets a sensitivity review before a shareable build may carry it.

## Where things are

| What | Where |
|---|---|
| The pictures, at most 960 x 540 JPEG (16:9, the centred card's media header; `idf_founding` is 640 x 360, as its original is small), each under 450 KB | `unity/Assets/UI/EventImages/<event>.jpg` |
| The mapping: event, Commons page, original URL and SHA-1, licence, artist, credit, alt text, crop, sensitivity flags | `unity/Assets/UI/EventImages/event-images.yaml` |
| The catalog the game loads (`Resources.Load`, so there is no scene field) | `unity/Assets/Resources/EventImageCatalog.asset` |
| Provenance rows, one per file (`status: verified` once the owner signs off) | `ASSETS.yaml` |
| CC BY 4.0 credits | `NOTICE`, and under the picture on the card |
| The grade and crop | `tools/event_images.py` |

The mapping is **not game content**. The event YAMLs feed the content hash and save compatibility, so nothing about
pictures may go into `content/`. `EventImageMappingTests.TheMappingIsNotGameContent` guards this, and the golden hashes
and `SaveGameTests` stay unchanged.

## The card

The card opens in the middle of the screen, up to 1040 px wide, over a dim backdrop (decision cards, 2026-10-13). When the
event has a picture, it fills the card's 16:9 media header (960x540 at the 1080p reference) in a gold frame, as in the
promo. Its credit line runs as a
small italic caption strip along the picture's bottom edge, so the credit is always visible but costs the card no
height. The minister's portrait is hidden, and the office line still names the minister. The alt text is the picture's
`tooltip`, and `EventCardPresenter.PhotoAlt` exposes it.

The header is 16:9 whenever the card has the height for it. The photograph needs a band at least as tall as its credit line plus 48 px of picture
(`EventCardPresenter.MinPhotoHeight`). The credit strip never shrinks (`flex-shrink: 0`), so the credit that CC BY 4.0
requires is never cut.
- On every window the picture gives way first when the card is taller than the room under the bar: the header gets
  shorter (the picture is cropped, never squeezed), so the text and the options always show whole. If the band would
  fall below that height, the whole photograph is dropped: picture, credit and tooltip, and only then may the body
  scroll. (The old 200 px band in the right-hand column had to let the body scroll first on short windows, or an
  ultrawide 2560x1080 window never showed a photograph; the 16:9 header has the room to give.)
- When the window grows again, a dropped photograph comes back without waiting for the next event.
- A dropped photograph does not bring back the minister's portrait, which is taller still. The office line still names
  the minister.
- On a short window the card may show the photograph for one frame before it drops.

`UiLayoutTests.EveryEventCardFitsTheDecisionLayer` (every HUD window shape, the two- and three-row bars included),
`AShortWindowShowsThePhotographWholeOrDropsIt` (1280x600 and 2560x1080) and `ADroppedPhotographReturnsWhenTheWindowGrows`
cover this. The decision layer under the top bar bounds the card's height.

## Adding or replacing a picture

1. Find a file on Wikimedia Commons that is public domain, CC0 or CC BY 4.0. Never use share-alike files. Never use
   GPO, PD-Israel-only or PD-India files: the URAA may have restored their US copyright (decision D-P).
2. Read the licence, the licence templates and the artist on the file page. For a Meitar Collection file, the credit
   is the licensor's credit line, which the page's `{{cc-by-4.0|1=...}}` template expands to, for example "Benno
   Rothenberg / Meitar Collection / National Library of Israel / The Pritzker Family National Photography Collection".
3. Download the original once, into an empty folder outside the repo, as `<event>.jpg`. It is untrusted data.
4. Add the row to `event-images.yaml`:
   - The credit ends with " · CC BY 4.0 · graded and cropped" (or "CC0" / "public domain").
   - The alt text says what the picture shows. A date in it is the event's own date, or only a month or a year:
     library dates are sometimes wrong (the NLI dates the Altalena shelling 20 June; it was 22 June).
   - Set `sensitive: true` for an event with a sensitive tag, or for any picture the owner must see before release.
5. Make the JPG with `uv run --with pillow==12.3.0 --with pyyaml==6.0.3 python -I tools/event_images.py --originals DIR
   --only <event>`. It refuses a file whose SHA-1 does not match the row.
6. Add the `ASSETS.yaml` row: glob, licence, source (the Commons page), `status: owner-verify`, artist, credit,
   `adapted: "graded and cropped"`, and a note. For a CC BY file, add the credit to `NOTICE`.
7. Run `Statecraft.Game.EditorTools.EventImageImport.Run` in Unity, then the EditMode tests.

## Owner steps

- **Sensitivity review.** Six pictures are marked `sensitive: true`: `altalena`, `ten_days`, `operation_yoav`,
  `operation_hiram`, `operation_horev` and `bernadotte_assassination`.
  - A `-shareable` build (`Build.Web`/`Build.Mac`, the release flow in `docs/RELEASING.md`) refuses until each one is
    `sensitivity_reviewed: true` and `EventImageImport.Run` has rebuilt the catalog.
  - The gate fails closed: a catalog that exists but does not load is a refusal, and the gate also reads
    `event-images.yaml` and refuses when it lists an unreviewed sensitive picture or does not match the catalog.
  - Dev builds still show them, so the owner can review them in the game.
  - Marking a picture reviewed is the owner's step only: set `sensitivity_reviewed: true` in `event-images.yaml`, run
    `EventImageImport.Run`, and commit both.
  - Every build writes `event-photos.json` into its folder (for the Mac app, beside it): each picture it carries, and
    whether it is sensitive and reviewed, and whether the build was `-shareable`. The pictures are packed into Unity's
    data files, so `tools/package_release.py` (run by `scripts/oss/package_web.sh`) reads that file. It refuses a build
    folder without it, a build not made with `-shareable`, and a build carrying an unreviewed sensitive picture.
- **Decisions, 2026-10-11.** The controller ruled on the pictures under the owner's delegation, and the pictures
  below follow those rulings. The owner then signed off all six sensitive pictures, as shipped (contact sheet below),
  in session on 2026-10-11, relayed by the controller: "Approve all 6". Their rows say `sensitivity_reviewed: true`,
  with that note, and the `-shareable` build passes the gate.
  - **Scope.** Only the sensitive pictures need the sign-off, not all of them. The gate is unchanged.
  - **Approved as they are:** `operation_hiram` (a mine sweep), `operation_horev` (armour on the road, 22 December,
    in the window) and `bernadotte_assassination` (officials at the airfield, nothing graphic).
  - **Replaced, so the card does not show the outcome before the choice, or a near-portrait:**
    - `altalena`: the Altalena off the Tel Aviv shore, seen from a seafront café before the shelling (NLI: "a moment
      before the shelling … the Altalena approaches the Tel Aviv shore", 20 June 1948; Benno Rothenberg). It replaces
      the burning ship. No Kfar Vitkin frame was found on Commons under a usable licence: the Meitar frames are all at
      Tel Aviv, and the others found are GPO, PD-Israel or CC BY-SA.
    - `operation_yoav`: officers over a map on a jeep as forces move south before the operation (NLI, 13 October 1948;
      Benno Rothenberg). It replaces Beersheba on the day it was taken.
    - `ten_days`: two soldiers keeping watch from an armoured car during the capture of Lydda airport (NLI, 10 July
      1948, in the window; Boris Carmi). The airport itself is not in the frame. It replaces the near-portrait of one
      named woman, which the portrait rule does not allow under CC BY. The 82nd Battalion series of the same day is
      tagged "Meitar Collection (review needed)" on Commons and was not used. (Correction, 2026-10-13: that tag is a
      Commons sorting backlog, not a licence review. 8 of the 20 Meitar photos we ship carry it; all 20 have the same
      CC BY 4.0 licence and VRTS ticket 2021092910005788, with no deletion request.)
    - All three are CC BY 4.0 (Meitar Collection, VRTS ticket 2021092910005788), read through the Commons API, with the
      SHA-1 recorded. Contact sheet: `.superpowers/handover-2026-10-11/promo/eventimg-images/new-picks.png`.
  - **Dropped:** `sinai_withdrawal` now shows the prime minister's portrait. Its window is 31 December 1948 to 3 January
    1949, and no free photograph of the event from that window was found. The Meitar pictures of the RAF wreck are from
    7 January. The Sinai and El-Arish frames on Commons are GPO or PD-Israel. The NLI Bitmuna "Operation Horev" scans
    are tagged only `PD-old`, with no author and no US basis.
- **Provenance.** The licence review re-read all 25 original rows through the Commons API (SHA-1, licence, credit), and
  the three new rows were checked the same way. With the owner's sign-off of 2026-10-11, all 24 rows say
  `status: verified`. A new or replaced picture starts again at `owner-verify`, which `check_repo.py --release` refuses.

## Merge notes

- **promo-screens** (not merged here; checked against `origin/promo-screens` a4fa752): two files conflict.
  - `GameRoot.cs`, at the presenters' construction. Keep both new arguments:
    `new EventCardPresenter(root, bridge, text, portraits, EventImageCatalog.Load())` and
    `new DefencePresenter(root, bridge, text, portraits)`.
  - `Statecraft.uss`, after the Cabinet rules: `main`'s candidate-column and portrait-grid rules (`#cabinet-pick`,
    `#cabinet-seats`) against promo-screens' wide-screen rules (`.screen-wide`, `.wide-screen-open`, `.section-title`).
    Keep both blocks. The event-photo rules are not part of the conflict.
  - `UiLayoutTests.cs`, `Game.uxml` and `HudTests.cs` merge on their own. Rerun the layout tests on the merged tree.
- **perf-fix**: `HudIdleTests` builds `EventCardPresenter(..., null)`, with no photographs. Once both branches are on
  `main`, pass the real `EventImageCatalog`, so the idle check covers a photograph card.
