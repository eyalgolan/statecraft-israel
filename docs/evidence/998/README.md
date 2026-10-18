# Map hint evidence (#998)

The [owner's option (c) ruling](https://github.com/eyalgolan/statecraft-israel/issues/998#issuecomment-6251812500) approves explaining the existing right-drag control in English and Hebrew. This change updates the two hints; it does not change the controls.

| Language | Before | After |
| --- | --- | --- |
| English, 1280x720 | ![English before](en-before.png) | ![English after](en-after.png) |
| Hebrew, 390x844 | ![Hebrew before](he-before.png) | ![Hebrew after](he-after.png) |

These are crops of actual Unity EditMode UI renders made on 18 October 2026. The capture used the real Game.uxml, PanelSettings, fonts, HudPresenter, InfoPanelPresenter and UiDirection, with a new historical session. It rendered the UI into a RenderTexture against a black background, without the world scene. The same temporary capture test produced both sets; it was removed from Assets before the final test run. These screenshots are not WebGL captures.

## Regression and layout results

Before changing the strings, both new `TheMapHintExplainsInspectionAndRightDrag` cases failed against the original English and Hebrew hints. The temporary capture case passed: 3 total, 1 passed, 2 failed.

After changing the strings, the focused Unity EditMode run passed all 50 cases, including the capture case. After removing the capture helper, the final run passed all 49 retained cases, with zero C# compiler warnings or errors:

```text
Filter: InfoPanelLayoutTests;UiFoundationTests;CameraInputTests;MapPressTests;GameSceneTests.TheGameStartsInTheSceneOnceTheInputsAreLoaded
test-run: total="49" passed="49" failed="0" skipped="0" result="Passed"
```

The hint layout test now covers English and Hebrew at 1920x1080, 1280x720 and 390x844. It checks that the text fits, the empty panel is one line, the panel stays inside the window, and it does not overlap the minimap.

## Other validation

```text
dotnet test sim/Statecraft.sln -c Release --nologo -warnaserror
Passed! - Failed: 0, Passed: 1846, Skipped: 0, Total: 1846

pytest tools/tests/test_hebrew_ui.py -q
11 passed

uv run scripts/oss/check_repo.py
OK: checks secrets, emails, home-paths, conflict-markers, sizes, symlinks, required, decisions, assets

uv run scripts/oss/gdd_lint.py
OK: every GDD section has its parts, a done-when, units, sources and no TBD

uv run scripts/oss/rule_ids.py
OK: every defined rule ID has a citing test and every cited ID is defined

pytest scripts/oss/tests -q
4 failed, 300 passed

uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q
ERROR tools/tests/test_machine_lock.py: ModuleNotFoundError: No module named 'fcntl'
ERROR tools/tests/test_unity_postmerge.py: ModuleNotFoundError: No module named 'fcntl'
2 errors during collection
```

The policy test failures are local Windows setup limitations: `test_absolute_symlink_fails` cannot create its fixture without symlink privileges (WinError 1314); three `test_release.py` cases invoke `python3`, which resolves to the Windows Store alias. The full tools suite cannot collect its two POSIX locking modules on Windows because `fcntl` is unavailable. Their implementation is unchanged. Full Unity tests and a WebGL build were not run for this change. Sim and content did not change; golden hashes are unchanged. The Hebrew wording remains an AI draft for native review; no human review status was added.
