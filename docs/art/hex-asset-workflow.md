# Building and hex asset workflow

Every new building is delivered as both an asset and a registered hex composition. A standalone model or a `TownLandmarks` mapping alone does not complete the hex deliverable. Use the existing renderer, prototype registry, board recipes and terrain-painting paths described in [the hex-tiles guide](../hex-tiles/README.md) and [the layers and team plan](../hex-tiles/CIV-LAYERS-AND-TEAM-PLAN.md).

## 1. Agree the design before modelling

Follow this sequence: **historical/reference research → concept image → owner design approval → low-poly 3D → integration and QA**.

Record the reference, intended place/period, concept version and approval link. A pleasing concept is not historical verification; source clearance is not art approval; passing tests is not visual acceptance. Keep these decisions separate. If a design or historical choice is unresolved, state the question instead of treating an assumption as approval.

Finish current assets before expanding the batch. Parallel hex-polish work belongs on an isolated branch/worktree, with its touched files and overlaps stated before editing.

## 2. Keep provenance and originals intact

For each reference and delivered asset, record the exact source, creator, licence or permission, checked date, and generation/edit provenance where applicable. Follow [CONTRIBUTING.md](../../CONTRIBUTING.md), [LICENSING.md](../../LICENSING.md), [the content policy](../CONTENT_POLICY.md) and the asset manifest requirements. Do not infer reuse rights from an image being accessible online.

Preserve original models, textures and concept images. Keep derived edits identifiable: record the source hash, derived hash, transformation/export settings and destination asset identifier. Do not overwrite an original with a decimation or describe an edited file as untouched. Follow repository storage rules for source masters; preservation does not mean committing prohibited or oversized files. Do not slim an approved original merely to satisfy an assumed budget—raise the budget or design question through the existing process.

## 3. Deliver a designed hex, not a model on a pedestal

Each building delivery includes:

- The real approved model and required texture/material assets, with provenance records.
- A registered composition with a stable identifier, using existing prototype APIs.
- Explicit pivot, scale, orientation, footprint and placement settings.
- Ground and yards painted through the shared terrain system, with coherent entrances, paths and transitions to neighbouring hexes.
- A reproducible one-hex preview and the intended board integration.

Do not add a duplicated tall pedestal below each building. Shape the composition around the shared ground. Aim for better-designed ground and edges, larger focal buildings, fewer incidental objects and less clutter, with a readable silhouette at gameplay scale. No universal scale multiplier or object-count target is implied: demonstrate the balance visually.

Larger buildings must still respect the hex inset, neighbouring bounds, reserved unit space, picking and readable labels. Validate actual transformed geometry, not only a guessed footprint. Use real models for visual review; if assets are unavailable, label a stand-in diagnostic clearly and do not present it as art acceptance.

## 4. Approve one pilot before broad rollout

Use Tiberias as the first comparison for the larger-building, lower-density treatment. Keep its existing model integration and improve the actual registered hex composition rather than starting a competing town asset.

Capture the current and proposed versions at matching camera, zoom, lighting and game state. Show the ground/edge design, building silhouette, unit clearance and neighbouring transitions. Include both a one-hex view and the actual-map gameplay view. Ask the art owner to approve the density and composition before repeating the treatment across the board.

The flatter renderer remains opt-in unless the maintainer explicitly decides otherwise. A composition approval is not approval to switch the default map. Grid-spacing changes and renderer-default changes are separate decisions.

## 5. Integrate through existing paths

Use existing boards, regional recipes and persistent overrides; make regeneration reproducible. Coordinate coordinates and scale with the grid-bake work rather than duplicating it. Keep presentation overrides separate from historical locations, control data and simulation rules.

Record whether the model is present in a one-hex preview, a full board, the actual game and supported player builds. These are distinct integration levels. Reuse existing streaming/fallback paths and verify cleanup when a board is removed. A successful one-hex stream does not establish full-board memory, loading or frame-time budgets.

## 6. Characters and animations

Use fixed character–weapon pairs and the corresponding armed idle, walking and attack clips on the existing rigs. This workflow does not request runtime weapon switching or a new equipment system. Keep tank work, walking clips and attack/gun clips coordinated through #1865, #1866 and #1867 rather than opening parallel implementation queues.

## 7. Show evidence at the scope actually tested

For each visual change, provide matching actual-map before/after captures at close, gameplay and overview scales where relevant. Record the tested commit, board, state, camera, zoom, lighting, platform and reproduction command. Keep annotations separate from the rendered evidence.

Run relevant registration, determinism, containment, clearance, placement and integration tests, plus required repository gates. Report counts and failures precisely. Distinguish focused checks from the full suite, Editor from Native/WebGL player checks, and a one-hex preview from full-map validation. State unrun checks and missing assets directly; do not lower an acceptance bar simply to obtain a pass.

Before marking the slice complete, confirm:

- [ ] Reference and rights evidence are recorded; design approval is linked separately.
- [ ] Originals are preserved and derivatives have traceable hashes/settings.
- [ ] The real building has a registered composition, shared ground and one-hex preview.
- [ ] Silhouette, density, ground/edges, bounds, unit space and labels have been inspected.
- [ ] Actual-map before/after evidence and honest test results are attached.
- [ ] Board edits regenerate correctly and existing claims/dependencies are respected.
- [ ] Any requested default-map rollout remains a separate explicit maintainer decision.

## 8. Coordinate small contributions

Check current issues, PRs and comments before starting. Claim one named prototype or small regional slice, identify touched files and link dependencies. Use #18 as the visual umbrella; #31 is historical/closed and #1288 owns its remaining whole-town work. Coordinate grid changes through #1183/#1796 and unresolved Caesarea research through #1874.

Maintain a compact board inventory: prototype/region, real-model coverage, missing composition or placement issue, source/approval status, current claim and linked PR. Prefer filling a documented gap to replacing already-covered towns. Do not assign another contributor, invent a deadline or infer broader rollout approval from a local review.
