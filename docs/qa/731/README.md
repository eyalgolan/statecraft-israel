# Food and Arms HUD capture evidence

Refs #731, the owner-released Food/Arms HUD and buy-Arms slice. These are actual WebGL screenshots captured on 19 October 2026, not generated mockups. The capture build was made with source commit `95e5afd33058345290a11ac7171cd852a76e6b29` checked out. It includes the HUD integration fixes and current main c5771d9e. It precedes this evidence-only refresh; later commits' gate results belong in the PR with their exact SHA.

| State | English | Hebrew |
|---|---|---|
| Initial HUD, pending decision hidden without answering it | [HUD](hud-en.jpg) | [HUD](hud-he.jpg) |
| Defence scrolled to Arms, one purchase queued for End turn | [Queued Arms](queued-arms-en.jpg) | [Queued Arms](queued-arms-he.jpg) |

The initial HUD shows Food and Arms alongside the existing meters. In the queued images, stock remains 12, the quantity is 1, Attention left is 2.0, the pending line appears and withdrawal is enabled. Price and Attention are the real sim helpers' answers at that state; the UI does not choose a new price or compute an independent rule.

The same browser session in each language also withdrew the order (stock still 12, Attention restored to 4.0, pending line gone), queued it again and pressed End turn. The real console emitted `TURN 1 1948-06-15`; agent image inspection then found Arms 13, delta +1 and the turn-report cause `Bought 1 Arms, cost IL 1.0m`. The extra PNG/JPEG captures and complete successful-run console are retained in the local evidence archive. Those turn-report captures are not added here because their background includes an existing event-card photograph outside this focused visual evidence.

Capture method:

```sh
uv run tools/machine_lock.py --shared --wait-unity -- "$UNITY" \
  -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit \
  -projectPath unity -buildTarget WebGL \
  -executeMethod Statecraft.Game.EditorTools.Build.Web \
  -compression disabled -out Builds/731-food-arms-c577-preview -logFile "$BUILD_LOG"
```

`tools/headed.py`'s `Browser` drove the built game with `golden&navlog&lang=en` / `he`, at a fixed 1920x1080 CSS viewport and DPR 1, in temporary profiles deleted on exit. Its existing `keep_awake` option prevented background/covered-window throttling. All eight current manifest golden lines were awaited separately from this fresh game's seed. The UI path hides the initial card, opens Defence, scrolls the real panel, then uses its quantity-1 purchase, withdrawal and End turn buttons. No game state was injected and no product diagnostic hook was added.

The committed JPEGs are unedited native `Page.captureScreenshot` results at quality 90. Adjacent native PNG originals remain local; PNG and JPEG were separate capture calls, not claimed to be identical frames. [captures.json](captures.json) records each committed JPEG's SHA-256, size, capture time and adjacent PNG hash. These are agent-inspected captures, not a human visual acceptance test or a native Hebrew approval. New Hebrew remains agent-checked; existing sim cause messages retain their current English fallback. Safari, Windows and screen-reader checks are separate.

## Attribution and licence

Credit **Statecraft: Israel contributors**. The four JPEGs are **CC BY-SA 4.0** because they include D13's derived map bakes, following the existing screenshot ruling recorded in [ASSETS.yaml](../../../ASSETS.yaml). The screenshots are real game renders; visible models from The 1948 Collection retain their existing Tal Heuberger / Tripo Studio / CC0 disclosure. Fonts retain their existing EB Garamond and Frank Ruhl Libre OFL credits. See [LICENSING.md](../../../LICENSING.md) and [NOTICE](../../../NOTICE) for the full inherited records. Capturing the game does not independently verify its historical claims or change any source-review flag.

Required Copernicus notice:

> produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved
