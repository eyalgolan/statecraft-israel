# Generated 1948 characters

This character-only change is independent of the vehicle and environment asset branches. Seven existing generated characters retain their imported GUIDs, skin weights, 512px colour maps, static far derivatives, and portable per-character provenance. No new generation, paid rigging, simulation change or historical source edit is included.

## Campaign and review placement

| Character | Placement |
| --- | --- |
| Syrian infantry | Existing Syrian formation only |
| ALA infantry | Character-review stage; original C09 retains campaign ALA role |
| Iraqi infantry | Existing Iraqi formation only |
| Egyptian infantry | Character-review stage; campaign representation pending |
| Lebanese regular | Character-review stage; existing Lebanese row has no anchors |
| Bedouin civilian | Character-review stage; no combat assignment |
| Local irregular recreation | Character-review stage; original local figure retained |

The original Arab Legion remains C08. Original C09 retains its existing ALA campaign mapping. The new ALA candidate is review-only; art creates no deployment row. Existing Israeli figures and vehicle identities are unchanged. This slice does not edit CivilianPeople, the original C04/C06 materials or any historical anchor data. The new Bedouin civilian remains review-only, so it adds no campaign civilian spawn. Collection runtime counts are 58 models, 24 People and 24 Animated.

Egypt's existing simulation force has an authoritative hex, but presentation also uses the historical anchor row. Its Isdud forward and Majdal rear bases intentionally overlap from 11 June to 8 July 1948 (the existing sourced `content/map/units_1948.yaml` row, owner ruling #860). `UnitRoster.OnMap` also governs the legacy chronological roster and `UnitPlacer`; activating a side mapping would violate their existing one-token-per-row invariant. This change preserves the data and invariant and uses the explicitly authorized character-review scene. It does not select a base, invent a deployment or remove the simulation force.

## Play the review scene

Open `unity/Assets/Generated1948CharacterReview.unity` and press Play, or choose **Statecraft → Character review → Play seven generated characters**. The saved scene has its own camera and directional light, and seven explicit model/material/idle/walk references. Its display coordinates have no relationship to a historical map. No game boot, simulation or campaign orders run in it.

The stage shows all seven characters. Select a character or return to all seven, switch idle/walk in place, pause/resume, turn the camera, inspect the rear and zoom. **Game angle** uses the existing CameraRig 50-degree pitch, 35-degree field of view and perspective projection on this isolated stage. **Detail** uses a lower 12-degree orthographic view for feet and clothing. Neither stage view is claimed to reproduce campaign lighting, terrain or post-processing. Walk playback is confined to this art-review stage; runtime campaign prefabs still use the one-state idle controller. The scene is not added to the campaign's boot/build scene list and does not enlarge its initial download.

## Sources and import

`Generated1948Characters.Run` imports only these seven IDs. Original provider skin weights are preserved; C09 idle/walk animation was retargeted locally using rest-relative rotations and target bone lengths. The original fifty #507/#336 slim-converter inputs remain unchanged; the three independently generated runtime infantry use their own export pipeline. Near models preserve source triangles under the 8,000 cap; far figures are at most 1,500 triangles. Original masters and private references stay outside git. ASSETS.yaml records only this category's new files and derived evidence.

## Evidence and validation

The unchanged earlier character sheets are isolated actual Unity asset renders, labeled **not gameplay**: [idle/walk](images/generated-1948-character-idle-walk-review.jpg), [far review](images/generated-1948-character-far-review.jpg). Superseded ALA replacement experiment (not the current mapping): [close](images/generated-1948-ala20-comparison.jpg), [context](images/generated-1948-ala60-comparison.jpg), with [camera/state record](generated-1948-character-evidence.json). C09 is now restored, so these comparisons do not represent the final campaign change.

The prior combined integration passed 165 targeted EditMode tests, including 14 actual Animator/rig tests across seven characters. Those results predate this separate branch's new review scene; they do not claim that its scene test or playback has passed. The character-only branch passed 174 selected EditMode tests with no failures or skips, including the new scene and all 14 rig tests. The strengthened final scene test passed again after stage-only grounding was corrected for non-unit renderer scale. It checks all seven rigs after idle/walk against the real floor surface and independently computed renderer world bounds. An earlier bake-only assertion shared the original wrong scale assumption; it is not used as final floor-contact evidence. A one-pose no-shadow/zero-bias probe ruled out lighting as the main cause. [Final actual scene proof](images/generated-1948-character-review-scene-proof.jpg) and [scale/bounds record](generated-1948-character-grounding.json) show the planted boot meeting the floor; the other boot is raised in the walk pose. Original lights, global render settings, rig assets and campaign code were unchanged by this fix. The earlier64 development frames are not claimed as final contact evidence.

The collection-bundle build failed on unchanged base and the combined branch with identical installed Unity Collections CS7036 errors (NativeList.cs lines 850/864). [Baseline comparison](generated-1948-build-baseline.json) records the editor/package/source identity. No package file is patched, no release gate is bypassed, and a successful player/WebGL build is not claimed.


## Current independent checks

The .NET simulation check passed 2,423 tests with 0 warnings, 0 failures and 0 skips (parent-run shared unchanged sim/content). Full tools passed 1,500 tests, 5 skips and 2 subtests in 219.41 seconds; two native-GDAL modules, `test_dem_to_heightmap.py` and `test_precip_clip.py`, were excluded because GDAL is unavailable in this environment. This branch's slim-converter and policy test run passed 328 tests; repository policy also passed over the staged character-only files. The compile-only check uses the pinned Unity references and passed with 0 warnings and 0 errors. No successful player/WebGL build is claimed.

## Draft review follow-up

C09 campaign regression was reproduced with a failing test, then restored to the existing ALA formation. The final affected Unity run passed135 tests with zero C# warnings/errors. The new ALA rig stays available on the review stage and remains catalogued; it does not replace C09. Original C09 files and import metadata are byte-identical to the branch base.

The license grant and derivative-import policy for these new models remain unresolved against draft #1177 and #1273. No license or import settings were changed in this follow-up. [Audit](generated-1948-review-audit.json) records exact provenance checks, current compression/LOD settings and the decision boundary. GitHub CI runs for all three category PRs are awaiting maintainer approval and have started no jobs; this is not a failing-test result.
