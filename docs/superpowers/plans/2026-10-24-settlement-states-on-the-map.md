# Settlement-control states on the map (#803). Plan

> **For the implementer:** the owner answered both questions on 24 October 2026 ("Owner decisions" below). Start after
> Civ movement U1 merges (see "Overlap with work in flight").
> Red-first tests, one focused Unity PR. No new sim rule, no annex state until #799 defines it.

**Status (24 October 2026):** decided; implementation waits for Civ movement U1. Read against `origin/main` at
`ef62e9f1c` (#1517 merged: a move that ends on a target captures it at once; the answer still runs at End turn). No open or merged PR implements #803.

**Design record:** [docs/GDD.md](../../GDD.md) "After capturing an enemy city or village";
[docs/gdd/settlement-control.md](../../gdd/settlement-control.md) lines 29 and 32; the Phase 6 plan's PR 11
([2026-10-15-gdd-phase-6.md](2026-10-15-gdd-phase-6.md), "PR 11"); rules R-CTRL-002 to R-CTRL-021 in
[docs/rules/mvp.md](../../rules/mvp.md).

## What the owner has already decided

| Ruling | Words | Where |
|---|---|---|
| Q-UI-3 (a) | "A marker per state, with the name kept" (not (b): a depopulated place keeps its houses; not (c): no extra event-and-date tooltip) | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676), options in [the drafts](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6231809370) |
| Q-UI-4 (a) | `DisplacedByPlayer` "Shown on the International Support tooltip as a cause" | same |
| Q-CAP-2 (b) | `DisplacedByPlayer` counts places, not people (R-CTRL-019) | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249810256) |
| Q-GATE-1 (c) | "The release gate covers events, the target rows, the two dialogs' strings (EN and HE), and the map states' labels and tooltips" | same comment |
| Q-DEP-4 (a) | No razing: a depopulated place keeps its buildings (R-CTRL-021) | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676) |
| Q-PUP-1 (c) | No puppet regime in the MVP, so no puppet marker | [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249846499) |

## The states on main today

| Sim record (`GameState.Settlements[id]`) | Rule | Key stem |
|---|---|---|
| `State == SettlementControl.AwaitingChoice` | captured, answer not given (R-CTRL-004) | `awaiting_choice` |
| `State == SettlementControl.Occupied`, `Turns` left | occupied (R-CTRL-011, R-CTRL-012) | `occupied` |
| `State == SettlementControl.AwaitingEnd` | occupation period over, waits for #799 (R-CTRL-012) | `awaiting_end` |
| `Depopulated == true` (owner and state empty) | Deport (R-CTRL-020) | `depopulated` |

No record means the place is as it started. The six targets (jaffa, acre, nazareth, lod, ramla, beersheba) are rows of
`content/map/settlements_1948.yaml`, so each has its own name pill in `MapLabelsPresenter`'s game settlements
(`slot-<id>`, `label-<id>`). Lod and Ramla share hex (-3,21).

## Settled by this plan

The owner can overrule any of these in review.

- **No marker for a target the enemy still holds.** #803's issue lists captured, occupied, puppet, annexed and
  depopulated; the front-line colours already show who holds a town. Only the four records above get a marker.
- **The annexed marker comes with whichever of #799 and #803 lands second**: one map key and one panel key per language
  and one line in the view mapping (Task 2). Neither PR waits for the other.
- **Wording is settled by the owner's sensitivity review, not a separate question.** Q-GATE-1 (c) already puts every word
  below under the owner's review in both languages before a shareable build, so the plan proposes the words (Task 2) and
  the review approves or edits them.
- **The count line sits above "Main causes this turn:"**, not under it, because it is not a cause of this turn. The
  existing Deport drag line (R-CTRL-018, "(the places depopulated by Deport)") stays a turn cause as it is.
- **The release gate (R-GATE-002) covers the capture dialog's 17 `capture.*` keys too**, as Q-GATE-1 (c) says. They are
  on main today with only a comment asking for review. Heads-up: from the merge, a shareable build is refused until the
  owner has reviewed those lines and #803's in both languages.

## Owner decisions

**Then decided (owner, 24 Oct 2026, "1521: 1. a 2.a"; [#803](https://github.com/eyalgolan/statecraft-israel/issues/803#issuecomment-6374861047)):**

- **Question 1 (a), the map look:** a short word tag under the town's name, such as "Occupied" or "Depopulated", in one
  quiet colour for every state, so it reads without colour and does not look like a reward or an alarm. The name stays.
  Not (b) a symbol per state, not (c) a recoloured border.
- **Question 2 (a), the turns left:** an occupation's turns left show only in the side panel when the town is selected
  ("Nazareth: occupied, 4 turns left"). The map tag just says "Occupied" and never carries a number.

## Overlap with work in flight

- **Civ movement U1 to U5 (`civ/u1-u5-movement-ui`, not yet a PR):** changes `SimBridge.State` to the turn's preview (so an
  instant capture shows before End turn), adds route turn numbers to `MapLabelsPresenter` (`RouteTurnClass`,
  `SyncRouteLabels`), and edits `GameRoot.cs` and `InfoPanelPresenter.cs`. #803 edits the same three files. **#803 starts
  after U1 merges**, or rebases onto it: the state badge is a sibling in a settlement's slot, the route numbers are their
  own slots, and both must survive. With U1, reading `bridge.State` on `bridge.Changed` shows a capture the moment the
  move ends.
- **U6 (Codex, the capture dialog after a capturing move):** `CaptureDialogPresenter` only; no shared file. Task 1's gate
  covers its `capture.*` keys; if U6 adds a key, it joins the manifest.
- **#799 (annex or withdraw):** see "Settled" above; #799 adds no Unity string.
- **Advisors S1 (`feat/advisors-s1`):** new files only (`AdvisorCatalog`, `AdvisorSettings`, `AdvisorSignals`); reads
  `bridge.State` like everyone else. No overlap.
- **#1305 (enemy captures of Israeli towns):** a future record for an Israeli town captured by the enemy would need its own
  marker; not in #803.

## Global constraints

- The sim is the only source of truth. Unity maps an existing record to words; it adds no control rule or transition, and
  never derives a state from the units on the hex.
- No marker changes terrain, control overlays, settlement geometry, the state hash or the content hash. Expected golden
  impact: none; stop if any golden moves.
- A depopulated place keeps its houses (R-CTRL-021). The count wording says places, never people (R-CTRL-019).
- Every new key exists in both languages with the same placeholders. Hebrew is a draft until a native reviewer approves it
  (`docs/WRITING_UI.md`, `docs/WRITING_UI_HE.md`, `docs/UX_GLOSSARY.yaml`).
- The legacy loop has no settlement records and renders exactly as today.
- `changelog.d/803.md`. No em dashes. They/them for people.

## Files

| File | Change |
|---|---|
| `unity/Assets/Scripts/UI/SettlementControlView.cs` (new) | Pure mapping from a record to a key stem and the map and panel words |
| `unity/Assets/Scripts/UI/MapLabelsPresenter.cs` | One hidden badge per game settlement; `SetSettlementControl` |
| `unity/Assets/Scripts/UI/GameRoot.cs` | Initial call and a named `bridge.Changed` handler, undone in `Shutdown` |
| `unity/Assets/Scripts/UI/InfoPanelPresenter.cs` | One state line per settlement on the selected hex (`RenderLines`) |
| `unity/Assets/Scripts/UI/HudModel.cs`, `MeterTip.cs` | The lasting count line on International Support |
| `unity/Assets/UI/Statecraft.uss` | One quiet tag style under the name, the same colour for every state |
| `unity/Assets/UI/strings.en.yaml`, `strings.he.yaml` | The new keys |
| `unity/Assets/UI/settlement-control-strings-review.yaml` (new) | R-GATE-002's reviewed snapshot, `status: pending` |
| `unity/Assets/Scripts/UI/UiPaths.cs` | The manifest's path |
| `unity/Assets/Editor/SettlementControlStringReview.cs` (new), `Build.cs` | Parse, compare, refuse a shareable build |
| `docs/rules/mvp.md` | R-GATE-002 |
| EditMode tests: `SettlementControlViewTests.cs` (new), `MapLabelTests.cs`, `InfoPanelLayoutTests.cs`, `MeterTipTests.cs`, `BuildGateTests.cs`, `EventVideoTests.cs`, `OutcomeClipTests.cs`, `HebrewUiTests.cs` | |

Unity creates the `.meta` files; never hand-write a GUID.

## Task 1: R-GATE-002 first

- [ ] `docs/rules/mvp.md`: "R-GATE-002: A shareable build is refused unless every settlement-control string (the capture
  and end-of-occupation dialogs, the map-state labels, the info-panel state lines and the International Support count
  line), in English and Hebrew, matches `settlement-control-strings-review.yaml` and that file's status is
  `sensitivity-reviewed` (Q-GATE-1 (c), owner, 18 October 2026)."
- [ ] Red test in `BuildGateTests` (note `UiText.Parse`'s second argument is the file name used in errors):

```csharp
[Test]
public void AShareableBuildWaitsForTheSettlementControlStringsReview() // R-GATE-002
{
    var en = UiText.Parse("map.control.depopulated: Depopulated\n", "strings.en.yaml");
    var he = UiText.Parse("map.control.depopulated: מרוקן מתושביו\n", "strings.he.yaml");
    var review = SettlementControlStringReview.Parse(
        "status: pending\nstrings:\n  - { key: map.control.depopulated, en: Depopulated, he: מרוקן מתושביו }\n", "fixture");
    StringAssert.Contains("sensitivity review", SettlementControlStringReview.Refusal(review, en, he));
    review.Status = Statecraft.Sim.ReleaseGate.ReviewedStatus;
    Assert.IsNull(SettlementControlStringReview.Refusal(review, en, he));
    var changed = UiText.Parse("map.control.depopulated: Empty\n", "strings.en.yaml");
    StringAssert.Contains("map.control.depopulated", SettlementControlStringReview.Refusal(review, changed, he));
}
```

- [ ] `Refusal` also refuses: an empty list, a repeated key, an unknown status, a key missing from either strings file,
  and a missing or unreadable manifest (fail closed, as `EventImageRefusal` does).
- [ ] `Build.ShareableRefusal` gains a parameter for the loaded review (or its refusal) and appends it; `RefuseUnlessShareable`
  loads the shipped manifest and both strings files. Update the reflection argument arrays in `BuildGateTests`,
  `EventVideoTests` and `OutcomeClipTests` with a reviewed fixture so their tests keep testing only their own gate.
  `GateRefusal` without `-shareable` stays null.
- [ ] The manifest lists the 17 `capture.*` keys and every key from Tasks 2 and 5, `status: pending`. An AI-assisted
  commit never sets `sensitivity-reviewed`.

## Task 2: The view mapping and the proposed words

- [ ] Proposed English, for the owner's review (Hebrew drafts beside them in `strings.he.yaml`, marked
  `# SENSITIVE agent draft for #803: native and owner review pending`):

```yaml
map.control.awaiting_choice: "Decision pending"
map.control.occupied: "Occupied"
map.control.awaiting_end: "Occupation period over"
map.control.depopulated: "Depopulated"
info.control.awaiting_choice: "{0}: captured, decision pending"
info.control.occupied: "{0}: occupied, {1} turns left"
info.control.awaiting_end: "{0}: occupation period over, decision pending"
info.control.depopulated: "{0}: residents displaced, place depopulated"
hud.standing_displaced_places: "Places depopulated by Deport: {0}"
```

  The `map.control.*` words take no placeholder: the turns left are in the panel line only (Question 2 (a)).
- [ ] Red tests in the new `SettlementControlViewTests` (with an `En` helper as `CaptureDialogTests` has):

```csharp
[TestCase(SettlementControl.AwaitingChoice, false, "awaiting_choice")]
[TestCase(SettlementControl.Occupied, false, "occupied")]
[TestCase(SettlementControl.AwaitingEnd, false, "awaiting_end")]
[TestCase("", true, "depopulated")]
public void EveryRecordOnMainHasOneView(string state, bool depopulated, string expected)
{
    var owner = depopulated ? "" : "israel";
    var record = new SettlementControl(owner, state, depopulated: depopulated, turns: state == SettlementControl.Occupied ? 4 : 0);
    Assert.AreEqual(expected, SettlementControlView.Key(record));
}

[Test]
public void NoRecordOrAnUnknownStateHasNoView()
{
    Assert.IsNull(SettlementControlView.Key(null));
    Assert.IsNull(SettlementControlView.Key(new SettlementControl("israel", "something_new")));
}

[Test]
public void TheOccupiedPanelLineCarriesTheTurnsLeft()
{
    var occupied = new SettlementControl("israel", SettlementControl.Occupied, turns: 4);
    Assert.AreEqual(En["map.control.occupied"], SettlementControlView.MapText(En, occupied));
    Assert.AreEqual(En.Format("info.control.occupied", "Lydda", 4), SettlementControlView.PanelText(En, "Lydda", occupied));
}
```

- [ ] A parity test: every new key is in both files with the same placeholders, each Hebrew value has Hebrew letters, and
  no `map.control.*` value has a placeholder in either language.

## Task 3: Map badges that keep the name

- [ ] Interface: `public const string ControlBadgeClass = "map-label-control";` (one class: no per-state style)
  and `public void SetSettlementControl(IReadOnlyDictionary<string, SettlementControl> control)`.
- [ ] Red test in `MapLabelTests` with the file's `HdPanel` harness (a bare `UiTestKit` root has no `map-labels`
  element), disposed as the file's other tests do:

```csharp
[UnityTest]
public IEnumerator EachStateHasOneBadgeAndTheNameStays()
{
    var hd = new HdPanel(cam);
    var labels = new MapLabelsPresenter(hd.Root, map, cam);
    try
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var names = labels.Labels.ToDictionary(x => x.name, x => x.text);
        var badges = new List<Label>();
        labels.SetSettlementControl(new Dictionary<string, SettlementControl>
        {
            ["acre"] = new SettlementControl("israel", SettlementControl.AwaitingChoice),
            ["nazareth"] = new SettlementControl("israel", SettlementControl.Occupied, turns: 4),
            ["lod"] = new SettlementControl("israel", SettlementControl.AwaitingEnd),
            ["ramla"] = new SettlementControl(depopulated: true),
        });
        yield return null;
        foreach (var (id, key) in new[] { ("acre", "awaiting_choice"), ("nazareth", "occupied"), ("lod", "awaiting_end"), ("ramla", "depopulated") })
        {
            var slot = labels.Slots.Single(x => x.name == "slot-" + id);
            Assert.AreEqual(names["label-" + id], slot.Q<Label>("label-" + id).text, id + ": the name is kept");
            var badge = slot.Q<Label>(className: MapLabelsPresenter.ControlBadgeClass);
            Assert.AreEqual(text["map.control." + key], badge.text, id);
            badges.Add(badge);
        }
        Assert.AreEqual(1, badges.Select(x => x.resolvedStyle.color).Distinct().Count(), "one quiet colour for every state");
        var jaffa = labels.Slots.Single(x => x.name == "slot-jaffa").Q<Label>(className: MapLabelsPresenter.ControlBadgeClass);
        Assert.AreEqual(DisplayStyle.None, jaffa.style.display.value, "no record, no badge");
    }
    finally { labels.Dispose(); hd.Dispose(); }
}
```

- [ ] `ClearingARecordHidesItsBadgeAndKeepsTheSameNameLabel`: hold the name `Label`, set one record, then an empty map;
  the same object remains and the badge is hidden.
- [ ] Layout test at 1920x1080, 1280x720 and 390x844, English and Hebrew: each visible badge stays inside its slot's bounds
  and the window, sits under its own name (its top at or below the name's bottom), and does not overlap another visible
  name (Lod and Ramla included).
- [ ] Implement one hidden badge per game settlement at construction (not for map locations, built sites or the 1948
  localities), as a sibling of the name in its slot, placed under the name, hidden with `style.display = DisplayStyle.None`.
  One USS rule styles every state alike. `SetSettlementControl` swaps text, toggles display and sets `geometryDirty` only
  when something changed; it never calls `container.Clear` or rebuilds a name.

## Task 4: The panel names each place's state

- [ ] Red test in `InfoPanelLayoutTests` with its `Rendered` harness:

```csharp
[UnityTest]
public IEnumerator ASharedHexNamesEachPlacesStateSeparately()
{
    var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
    var lod = C.SettlementHexes.Single(x => x.Id == "lod");
    Assert.IsTrue(C.SettlementHexes.Any(x => x.Id == "ramla" && x.Q == lod.Q && x.R == lod.R), "Lod and Ramla share a hex");
    g.State.Settlements["lod"] = new SettlementControl("israel", SettlementControl.Occupied, turns: 3);
    g.State.Settlements["ramla"] = new SettlementControl(depopulated: true);
    yield return Rendered(1280, 720, g, (root, info) => info.Show(lod.Q, lod.R), root =>
    {
        var line = root.Q<Label>("info-settlements").text;
        StringAssert.Contains(Text.Format("info.control.occupied", "Lydda", 3), line);
        StringAssert.Contains(Text.Format("info.control.depopulated", "Ramle"), line);
    });
}
```

- [ ] The same in Hebrew with the file's Hebrew harness; all four states in the panel's existing text-fit test.
- [ ] In `RenderLines`, after `info.owned`, append `SettlementControlView.PanelText(text, TownName(id), record)` for each
  settlement on the hex that has a view, in `Content.SettlementHexes` order. Not while a unit card owns the line.

## Task 5: Keep the map in step, and the count line

- [ ] `GameRoot`: call `labels.SetSettlementControl(bridge.State.Settlements)` once, and register a named
  `Action showSettlementControl` on `bridge.Changed`, removed through `Undo` as `showUnits` is. Keep it separate from
  `RenderPlacementsIfChanged`. Test (`GameSceneTests`, which opens the real scene): replace a record, raise `Changed`,
  the badge changes without a new name label; remove it, the badge hides. With U1 merged, add: a move that ends on
  Nazareth shows its badge in the same click, before End turn.
- [ ] `HudModel.PersistentCauses(UiText text, GameState state, string meter)`: one line,
  `text.Format("hud.standing_displaced_places", state.DisplacedByPlayer)`, only for `standing` and only when the count is
  above 0. `MeterTip.Lines` puts it after the meter's words and before the `hud.causes` heading, so `HudModel.MaxCauses`
  never drops it. Red tests in `MeterTipTests` (its `En` helper): the line for 2 places, none for `cohesion` or 0 places;
  a rendered English and Hebrew tooltip shows it on a turn with no Deport cause in `Session.Last`.
- [ ] `changelog.d/803.md` under `### Added`.

## Visual evidence

Before and after on a `captures/pr-<n>` branch, English and Hebrew, same save, camera, zoom and window size, for:
awaiting choice, occupied, awaiting end, depopulated (map tag and panel), and the International Support tooltip on the
Deport turn and one turn later. 1920x1080 for comparison, 390x844 for layout. Label the changed areas.

## Validation

- [ ] `uv run tools/build_sim_for_unity.py`; the focused EditMode filter
  `SettlementControlViewTests;MapLabelTests;InfoPanelLayoutTests;MeterTipTests;BuildGateTests;EventVideoTests;OutcomeClipTests;HebrewUiTests`
  red first, then green, through `tools/machine_lock.py --shared --wait-unity` with
  `export UNITY_MCP_SERVER_PATH=<existing empty file>`; never `-nographics`, never `-quit` with `-runTests`; XML total > 0.
- [ ] Then the full EditMode run on the PR merged with current origin/main, 0 NEW failures;
  `git checkout -- unity/ProjectSettings unity/Assets/Settings` after each run.
- [ ] `dotnet test sim/Statecraft.sln`, the tools and policy suites, `uv run scripts/oss/rule_ids.py`,
  `bash scripts/check.sh origin/main` ending "ALL CHECKS PASSED". No golden moves.

## Done when

- The four states on main each have one marker that keeps the name, and a per-place panel line.
- The turns left show only in the panel line; the map tag never carries a number.
- The International Support tooltip always shows the places count while it is above 0.
- R-GATE-002 refuses a pending, missing or changed string in a shareable build, for the capture dialog's lines and #803's.
- The owner has reviewed the exact English and Hebrew lines that ship; a native reviewer has approved the Hebrew.
- Before and after screenshots are on the PR; full EditMode and `scripts/check.sh` pass.
