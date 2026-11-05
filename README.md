# #2634: web save-failure toast, captured in a real WebGL build

- PR: #2634 (`claude/web-save-failure-toast-2632`), head `fa77df924d3da13e4296c60d40c045b25e88fad0`.
- Build: `Statecraft.Game.EditorTools.Build.Web` (`-buildTarget WebGL -compression disabled -out Builds/Web-disabled`),
  Unity 6000.3.25f1, after `tools/build_sim_for_unity.py`. Build time: 278 s (5 November 2026, 12:23-12:28 IST).
- Browser: Chrome for Testing (Playwright cache, `--headless=new`, web_check's flags), fresh profile per language,
  viewport 1920x1080 at DPR 1, screenshots by CDP `Page.captureScreenshot`. Served by `tools/web_check.py`'s server.

## Screenshots

| Language | File | Toast text seen |
|---|---|---|
| English (`?navlog`) | `toast-en.png` | "Golani Brigade holds a hex 10 km south-east of Tiberias" / "Your game could not be saved." |
| Hebrew (`?navlog&lang=he`) | `toast-he.png` | the Hold message (Golani Brigade, in Hebrew) / "לא ניתן היה לשמור את המשחק." (RTL, full stop at the left) |

`save.write_failed` at the head: `strings.en.yaml:761` "Your game could not be saved.";
`strings.he.yaml:901` "לא ניתן היה לשמור את המשחק.". The first toast line is the Hold order's own message; the
failure is appended to it, as `GameRoot.OnSavePersistenceResult` does (`toast.Append`).

## How the failure was forced

Steps on a fresh profile: boot, New game on the start screen, Skip the opening, End turn (it selects the first unit
that needs orders, Golani Brigade), then Hold on its card. Hold is a committed order, so `GameStart.Autosave` writes
the save and asks `AutosavePersistence.jslib` for the IndexedDB result.

1. Tried first: CDP `Storage.overrideQuotaForOrigin` with `quotaSize: 1`, set after boot and before New game. The write
   still succeeded (`AUTOSAVE OK`, no toast; `console-en-quota-attempt.txt`), so a tiny quota override does not fail
   IDBFS's put in this Chrome.
2. Used: `Page.addScriptToEvaluateOnNewDocument` wraps `IDBObjectStore.prototype.put`; once the rig sets
   `window.__statecraftFailPuts = true` (after boot, before New game) every put throws a `QuotaExceededError`
   DOMException. Emscripten's IDBFS `syncfs` then reports the error to Unity's autosync callback, which the jslib
   observes. The rig logs each forced put (`RIG forced IndexedDB put failure ...`). Script: `rig.py`.

Console lines (`console-en.txt`, `console-he.txt`), English run:

```
20.59s [log] AUTOSAVE PENDING 1 1948-05-15 turn 0 hash 39bca07b...
20.68s [log] RIG forced IndexedDB put failure on FILE_DATA key /idbfs/1d3aff39f23b633785840aea973d4eeb
20.68s [error] AUTOSAVE FAIL 1 1948-05-15 turn 0 hash 39bca07b...
20.68s [log] SAVE FAILURE SHOWN save.write_failed
```

The Hebrew run shows the same sequence (`AUTOSAVE PENDING` / `AUTOSAVE FAIL` / `SAVE FAILURE SHOWN save.write_failed`).

## Main's behaviour

Main was not built: on main the same IndexedDB failure only logged `AUTOSAVE FAIL` to the browser console and showed
the player nothing (#2632).

## Notes

- The opening's settings sync also failed while the failure was armed (`/idbfs/statecraft-settings/...`, before any
  save request); that is not a save request, so it shows no toast, as the PR intends.
- Recorded by an agent (Claude Code).
