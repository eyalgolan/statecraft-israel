# Steamworks Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the owner release the free game on Steam for Windows, macOS and Linux, with Steam started only in Steam builds, five achievements, Auto-Cloud saves, English (and Hebrew as a game-support-only store language), release tags on a `beta` branch and every nightly on a `nightly` branch, uploaded by a GitHub Actions job with owner-created secrets, without changing the web build.

**Architecture:** A Steam-free seam in `Statecraft.Game` (`unity/Assets/Scripts/Platform/`) holds the interface and all logic worth testing; a thin `Statecraft.Steam` assembly (`unity/Assets/Steam/`, compiled only under the `STEAM` define, which only `-steam` builds set) adapts it to Steamworks.NET. `tools/release_build.py --steam` makes ContentBuilder folders on the owner's Mac (Unity never runs in CI, O7); `tools/steam_upload.py stage` hands them to `.github/workflows/steam-upload.yml` through a never-published draft release, and that job renders the VDF scripts and runs steamcmd with the owner's cached builder login.

**Tech Stack:** Unity 6000.3.25f1 (C#, UI Toolkit, NUnit EditMode), Steamworks.NET 2025.165.0 via UPM (MIT; Steamworks SDK 1.65), Python 3.11+ with `uv` and pytest, SteamPipe steamcmd (in the GitHub Actions upload job only), GitHub Actions environments and secrets.

**Spec:** `docs/superpowers/specs/2026-11-01-steamworks-integration-design.md` (the owner's answers to Q1-Q10, 1 Nov 2026, in its section 6; #2150), on top of `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md`.

## Global Constraints

- Free forever: no price, no microtransactions, no paid DLC, no ads, no in-game store (`docs/oss/DECISIONS.md:15`, `:49-50`).
- Never commit the Steamworks SDK, its headers or tools, or any Valve binary (`steam_api*`, `steamcmd`); never commit `steam_appid.txt`.
- No agent spends money, creates an account or a secret, holds, reads, types or prints a credential, or runs `steamcmd` or any SDK binary. The only place steamcmd runs is `steam-upload.yml` on GitHub's machines, with the two secrets the owner created (Q8: "GitHub Actions"). Every Steamworks action in "Owner-only actions" is the owner's.
- Steamworks.NET is pinned to `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.165.0`, commit `ea51b029a5dbcb47a99d3c1fac930b811672ed32`.
- `STEAM` is never in the committed `scriptingDefineSymbols`; only `-steam` builds add it, and they restore it in `finally`.
- The sim (`sim/`) and the golden hashes do not change. Steam only reads game state.
- `companyName: Statecraft` and `productName: Statecraft Israel` stay frozen (they name the save folder and the Auto-Cloud paths).
- Unity runs only through `uv run tools/machine_lock.py --shared --wait-unity -- ...`, with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (`CONTRIBUTING.md:61-63`). EditMode is a local gate (`CONTRIBUTING.md:114`).
- Player-visible strings in both `unity/Assets/UI/strings.en.yaml` and `strings.he.yaml`. Changelog fragment `changelog.d/<branch>.md` for any change under `unity/` (`AGENTS.md:60`).
- No resource name or description uses an em dash; use a hyphen.
- Q1: "Inert repo work now (Recommended)": Tasks 1-9 may start now; Task 10 waits for the MVP and #905. Q9: "Treat free as non-commercial" (recorded in `docs/oss/DECISIONS.md`).

## Review Focus

1. **A Steam build started without the Steam client, or with Steam offline.** The player expects the game to start (relaunched through Steam, or playing on with no achievements), never a crash or a black screen. Task 6 tests `SteamStartRule.Decide` for each outcome, including no app ID.
2. **A non-Steam desktop archive (GitHub, itch.io) containing Valve's library, including the macOS `steam_api.bundle` directory.** Expected: excluded at build time (Task 6) and refused before archiving (Task 7, all three platforms' paths).
3. **A Steam player run with `-golden` (the player golden check, on GitHub's machines or the Mac).** Expected: it never calls Steam, so it never quits to relaunch through Steam, and prints both `GOLDEN OK` lines. Task 6 tests the start rule with `golden: true`.
4. **A save loaded from a save made on another OS through Steam Cloud, or an older achievement already unlocked.** Expected: the game loads it (deterministic sim) and re-reports nothing twice. Task 2 tests that the reporter unlocks only newly earned achievements, including after a load of an already-finished game.
5. **A Steam upload started by something other than the owner's own release or nightly (a pull request, a fork, a branch other than `main`).** Expected: the job never gets the secrets. Task 8 pins the workflow's triggers, environment and secret use.
6. **An objective added to `content/objectives.yaml` later.** Expected: a test fails until it has an achievement mapping. Task 2 pins it.

---

## File structure

| Path | Responsibility | Task |
| --- | --- | --- |
| `unity/Assets/Scripts/Platform/IPlatformServices.cs` | The interface the game asks a store for | 1 |
| `unity/Assets/Scripts/Platform/PlatformServices.cs` | `PlatformServices.Current` and the no-op `NullPlatformServices` | 1 |
| `unity/Assets/Scripts/Platform/AchievementRules.cs` | Which achievement API names a `GameState` has earned | 2 |
| `unity/Assets/Scripts/Platform/AchievementReporter.cs` | Watches `SimBridge.Changed`, unlocks only what is new | 2 |
| `unity/Assets/Scripts/Platform/SteamStartRule.cs` | Whether a Steam build quits, runs with Steam or runs without it (never with `-golden`) | 6 |
| `unity/Assets/Scripts/UI/QuitButton.cs` | Desktop-only Quit on the start screen and in Settings | 4 |
| `unity/Packages/manifest.json`, `packages-lock.json` | The pinned Steamworks.NET dependency | 5 |
| `unity/ProjectSettings/SteamworksNETSettings.json` | Turns off the package's own define management | 5 |
| `unity/Assets/Steam/Statecraft.Steam.asmdef` | `defineConstraints: ["STEAM"]` | 6 |
| `unity/Assets/Steam/SteamBootstrap.cs` | Init, restart, callbacks, shutdown | 6 |
| `unity/Assets/Steam/SteamPlatformServices.cs`, `SteamAppId.cs` | `IPlatformServices` over Steamworks.NET; the app ID (`0` until known) | 6 |
| `unity/Assets/Editor/Build.cs` | `-steam` flag as `extraScriptingDefines`; Valve's native plugins excluded from non-Steam players | 6 |
| `scripts/unity_compile/Steam.csproj`, `compile.sh` | Compile the Steam assembly outside Unity when the package is cached | 6 |
| `tools/release_build.py` | `--steam` folders, `--nightly-version`; strip and refuse Valve files in other archives | 7 |
| `tools/nightly.py`, `docs/RELEASING.md` ("Nightly builds") | The nightly's last stage, `steam` | 7 |
| `steam/steam.json`, `steam/app_build.vdf.in`, `steam/depot_build.vdf.in` | IDs (null until the owner has them) and VDF templates | 8 |
| `tools/steam_upload.py` | `render` (VDFs) and `stage` (draft release and dispatch) | 8 |
| `.github/workflows/steam-upload.yml`, `scripts/oss/tests/test_github_config.py` | The upload job and its pinned shape | 8 |
| `docs/RELEASING.md`, `docs/steam/README.md`, `docs/steam/test-checklist.md`, `docs/steam/privacy.md` | Owner runbook, manual checks, privacy statement | 9 |

---

### Task 1: The platform seam

**Files:**
- Create: `unity/Assets/Scripts/Platform/IPlatformServices.cs`, `unity/Assets/Scripts/Platform/PlatformServices.cs`
- Test: `unity/Assets/Tests/EditMode/PlatformServicesTests.cs`

**Interfaces:**
- Produces:
  - `public interface IPlatformServices { string Name { get; } void Unlock(IReadOnlyCollection<string> achievementIds); }` in namespace `Statecraft.Game`.
  - `public static class PlatformServices { public static IPlatformServices Current { get; set; } }`, never null (setting null restores the default).
  - `public sealed class NullPlatformServices : IPlatformServices { public static readonly NullPlatformServices Instance; }`: `Name == "none"`, `Unlock` does nothing.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void The_default_is_the_no_op_services()
{ Assert.AreSame(NullPlatformServices.Instance, PlatformServices.Current); Assert.AreEqual("none", PlatformServices.Current.Name); }
[Test] public void Setting_null_restores_the_default()
{ PlatformServices.Current = new FakePlatform(); PlatformServices.Current = null!; Assert.AreSame(NullPlatformServices.Instance, PlatformServices.Current); }
[Test] public void The_no_op_unlock_accepts_anything()
{ Assert.DoesNotThrow(() => NullPlatformServices.Instance.Unlock(new[] { "OBJ_RAILWAY" })); }
```
`FakePlatform` (in the test file) records every `Unlock` call; `[TearDown]` resets `PlatformServices.Current = null!`. Task 2 reuses it (make it `internal`).

- [ ] **Step 2: Run them to see them fail**

Run (machine lock, `CONTRIBUTING.md:61-63`, plus `-testFilter PlatformServicesTests`). Expected: compile error, `PlatformServices` not found. `scripts/unity_compile/compile.sh` shows the same error without Unity.

- [ ] **Step 3: Implement the three types**

- [ ] **Step 4: Run the tests to see them pass.** Expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add unity/Assets/Scripts/Platform unity/Assets/Tests/EditMode/PlatformServicesTests.cs* changelog.d/
git commit -m "Platform: a store-neutral services seam, a no-op by default (Refs #2149)"
```

### Task 2: Achievement rules and reporter (Q5)

**Files:**
- Create: `unity/Assets/Scripts/Platform/AchievementRules.cs`, `unity/Assets/Scripts/Platform/AchievementReporter.cs`
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs` (beside the `EndScreenPresenter` at `:420`, own an `AchievementReporter`)
- Test: `unity/Assets/Tests/EditMode/AchievementRulesTests.cs`

**Interfaces:**
- Consumes: `IPlatformServices`, `PlatformServices.Current` (Task 1); the public fields `GameState.Objectives` (`SortedDictionary<string, DateTime>`, `sim/Statecraft.Sim/GameState.cs:105`) and `GameState.GameOver` (`:85`), `GameOverCodes.MvpEnded` (`:16`); `SimBridge.Changed` and `SimBridge.State` (`unity/Assets/Scripts/UI/SimBridge.cs:24`, `:44`).
- Produces:
  - `public static class AchievementRules { public static readonly IReadOnlyDictionary<string, string> ForObjective; public const string CampaignDone = "CAMPAIGN_DONE"; public static IReadOnlyList<string> Earned(GameState s); }`: `Earned` returns API names sorted ordinally.
  - `ForObjective` is exactly `railway -> OBJ_RAILWAY`, `burma_road -> OBJ_BURMA_ROAD`, `yishuvim -> OBJ_YISHUVIM`, `absorb_immigrants -> OBJ_ABSORB_IMMIGRANTS`. These API names are what the owner enters in App Admin (Task 9 lists them).
  - `public sealed class AchievementReporter : IDisposable { public AchievementReporter(SimBridge bridge, Func<IPlatformServices> services); }`: reports on construction and on each `Changed`, passing only ids not reported before in this session.

- [ ] **Step 1: Write the failing tests**

```csharp
static readonly Content C = ContentLoader.Load(Paths.Content);
[Test] public void Every_objective_in_content_has_an_achievement()
{ CollectionAssert.AreEquivalent(C.Objectives.Select(o => o.Id), AchievementRules.ForObjective.Keys); }
[Test] public void A_new_game_has_earned_nothing()
{ CollectionAssert.IsEmpty(AchievementRules.Earned(GameSession.New(C, GoldenRun.Seed, Mode.Historical).State)); }
[Test] public void A_completed_objective_earns_its_achievement()
{ var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State; s.Objectives["railway"] = new DateTime(1949, 3, 15);
  CollectionAssert.AreEqual(new[] { "OBJ_RAILWAY" }, AchievementRules.Earned(s)); }
[Test] public void Reaching_the_last_turn_earns_the_campaign_but_a_defeat_does_not()
{ var s = GameSession.New(C, GoldenRun.Seed, Mode.Historical).State; s.GameOver = GameOverCodes.MvpEnded;
  CollectionAssert.Contains(AchievementRules.Earned(s), AchievementRules.CampaignDone);
  s.GameOver = GameOverCodes.DefeatWar; CollectionAssert.DoesNotContain(AchievementRules.Earned(s), AchievementRules.CampaignDone); }
[Test] public void The_reporter_unlocks_each_achievement_once_even_over_a_loaded_finished_game()
{ /* bridge over a state with railway done and GameOver = MvpEnded; FakePlatform from Task 1;
     construct the reporter, raise Changed twice; assert FakePlatform saw exactly one Unlock of
     {"OBJ_RAILWAY","CAMPAIGN_DONE"} and no further calls. */ }
```
The reporter test builds its `SimBridge` the way `EndScreenTests` does (`unity/Assets/Tests/EditMode/EndScreenTests.cs:23-40`).

- [ ] **Step 2: Run them to see them fail** (EditMode, `-testFilter AchievementRulesTests`). Expected: `AchievementRules` not found.

- [ ] **Step 3: Implement `Earned` and the reporter; wire it in `GameRoot`**

- [ ] **Step 4: Run them to see them pass**, then the full EditMode suite. Expected: all pass; the golden tests unchanged.

- [ ] **Step 5: Commit** `Platform: five achievements read from the game state (Refs #2149)`.

### Task 3: Dropped

The first draft let Steam's language seed the first start. Valve lists Hebrew only under "Game Support Only": "These
languages are not supported in Steamworks APIs" (Languages page, read 1 Nov 2026), so `GetCurrentGameLanguage()` can never
return Hebrew, and the task had nothing to do (review of #2148, C3). The in-game choice and `-lang` stay as they are.
The number is kept so the issue map and the later tasks keep their numbers.

### Task 4: Desktop Quit

**Files:**
- Create: `unity/Assets/Scripts/UI/QuitButton.cs`
- Modify: `unity/Assets/UI/Game.uxml` (a `start-quit` button in `start-panel`, `:597`, and a `settings-quit` button in the Settings screen, `:437`), `unity/Assets/Scripts/UI/StartScreenPresenter.cs`, `unity/Assets/Scripts/UI/SettingsPresenter.cs`, `unity/Assets/UI/strings.en.yaml` and `strings.he.yaml` (`ui.quit`: "Quit" / "יציאה")
- Test: `unity/Assets/Tests/EditMode/QuitButtonTests.cs`

**Interfaces:**
- Produces: `public static class QuitButton { public static bool Shown(RuntimePlatform p); public static void Bind(Button b, RuntimePlatform p, Action quit); }`. `Shown` is false for `WebGLPlayer`, true for `WindowsPlayer`, `OSXPlayer`, `LinuxPlayer` and the editors. `Bind` hides the button when not shown and calls `quit` on click; the presenters pass `() => Application.Quit()`.

- [ ] **Step 1: Write the failing tests**: `Shown` for the five platforms above; on `Game.uxml` through `UiTestKit`, the start screen and Settings each carry exactly one quit button, hidden when bound for `WebGLPlayer` and calling the injected action when clicked on `WindowsPlayer`; `ui.quit` exists in both string files.
- [ ] **Step 2: Run them to see them fail.**
- [ ] **Step 3: Implement**, following the Settings button styling already in `Game.uxml:437-446`. Keyboard: the buttons join the existing Tab order; no new shortcut.
- [ ] **Step 4: Run them to see them pass**, then the full EditMode suite (layout tests in RTL included).
- [ ] **Step 5: Commit** `UI: a Quit button on desktop players, hidden in the browser (Refs #2149)`.

### Task 5: Steamworks.NET, pinned (Q2)

**Files:**
- Modify: `unity/Packages/manifest.json` (add `"com.rlabrecque.steamworks.net": "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.165.0"`), `unity/Packages/packages-lock.json` (written by Unity; its `hash` must be `ea51b029a5dbcb47a99d3c1fac930b811672ed32`), `.gitignore` (`unity/steam_appid.txt`), `THIRD-PARTY-NOTICES.md:44-52` (Steamworks.NET MIT with "Copyright (c) 2013-2022 Riley Labrecque", from its `LICENSE.md` at the pinned commit; its managed assembly is in every desktop build; Steam builds also carry Valve's `steam_api` redistributable under the Steamworks SDK Access Agreement)
- Create: `unity/ProjectSettings/SteamworksNETSettings.json` (made by Project Settings > Steamworks.NET, "manage define symbols" off: the package's `RedistInstall` otherwise adds `STEAMWORKS_NET` to `ProjectSettings.asset` whenever the editor opens on a Standalone target, which `CONTRIBUTING.md:61-63` always uses)
- Test: `scripts/oss/tests/test_steam_package.py` (runs in CI's `Tools`/policy pytest)

**Interfaces:**
- Produces: the `com.rlabrecque.steamworks.net` assembly (Editor, Windows, macOS, Linux; never WebGL, per its asmdef) for Task 6.

- [ ] **Step 1: Write the failing tests**

```python
def test_steamworks_net_is_pinned_to_the_reviewed_tag():
    deps = json.loads((REPO / "unity/Packages/manifest.json").read_text())["dependencies"]
    assert deps["com.rlabrecque.steamworks.net"].endswith("#2025.165.0")
def test_the_lock_holds_the_reviewed_commit():
    lock = json.loads((REPO / "unity/Packages/packages-lock.json").read_text())["dependencies"]
    assert lock["com.rlabrecque.steamworks.net"]["hash"] == "ea51b029a5dbcb47a99d3c1fac930b811672ed32"
def test_no_valve_binary_or_appid_file_is_tracked():
    names = subprocess.run(["git", "ls-files"], cwd=REPO, capture_output=True, text=True, check=True).stdout.split()
    assert not [n for n in names if re.search(r"(^|/)(lib)?steam_api(64)?\.(dll|so|dylib|lib)$|(^|/)steam_api\.bundle(/|$)|steam_appid\.txt|steamcmd", n)]
def test_the_committed_project_never_defines_steam():
    lines = (REPO / "unity/ProjectSettings/ProjectSettings.asset").read_text().splitlines()
    k = next(i for i, l in enumerate(lines) if l.startswith("  scriptingDefineSymbols:"))
    block = [lines[k]] + [l for l in itertools.takewhile(lambda l: l.startswith("    "), lines[k + 1:])]
    assert not any(re.search(r"(^|[;: ])STEAM(;|$)", l) for l in block), block
def test_the_package_may_not_manage_define_symbols():
    assert json.loads((REPO / "unity/ProjectSettings/SteamworksNETSettings.json").read_text()) == {"<key Unity writes>": False}
```
Replace `<key Unity writes>` with the key the settings page actually writes (read the file after toggling; do not guess).

- [ ] **Step 2: Run them to see them fail:** `uv run --with pytest==9.1.1 pytest scripts/oss/tests/test_steam_package.py -q`. Expected: 3 failed, 2 passed (the no-binary and committed-defines tests pass already and stay as guards).
- [ ] **Step 3: Add the dependency, open the project once in Unity under the machine lock so it resolves the package and writes the lock, toggle the setting, add the ignore line and notices.**
- [ ] **Step 4: Verify:** the tests pass; `git status --short unity/ProjectSettings/ProjectSettings.asset` is empty after a second editor open; `git ls-files | grep -i steam_api` prints nothing; the full EditMode suite passes; a WebGL build made with `-compression disabled` contains no Steamworks code: `grep -a -c -i steamworks <build>/Build/*` prints 0 for every file (the package's asmdef excludes WebGL; this checks it).
- [ ] **Step 5: Commit** `Unity: Steamworks.NET 2025.165.0, pinned by tag and commit (Refs #2149)`.

### Task 6: The Steam assembly and `-steam` builds (Q3)

**Files:**
- Create: `unity/Assets/Scripts/Platform/SteamStartRule.cs`, `unity/Assets/Steam/Statecraft.Steam.asmdef` (`references: ["Statecraft.Game", "com.rlabrecque.steamworks.net"]`, `includePlatforms: ["Editor", "LinuxStandalone64", "macOSStandalone", "WindowsStandalone64"]`, `defineConstraints: ["STEAM"]`), `unity/Assets/Steam/SteamBootstrap.cs`, `unity/Assets/Steam/SteamPlatformServices.cs`, `unity/Assets/Steam/SteamAppId.cs`, `scripts/unity_compile/Steam.csproj`
- Modify: `unity/Assets/Editor/Build.cs` (`Mac` `:258-269`, `StandaloneMono` `:279-289`, `Run` `:301-318`), `scripts/unity_compile/compile.sh` (also compile `Steam.csproj` with `STEAM` when `unity/Library/PackageCache/com.rlabrecque.steamworks.net@*` exists; like the rest of `compile.sh`, this is a local check: CI has no `unity/Library`, `scripts/check.sh` prints SKIPPED there)
- Test: `unity/Assets/Tests/EditMode/SteamBuildTests.cs`

**Interfaces:**
- Consumes: `IPlatformServices`, `PlatformServices.Current` (Task 1); `GoldenCheck.Requested()` (`unity/Assets/Scripts/Boot/GoldenCheck.cs:16`, used at `GameBoot.cs:65`); Steamworks.NET (Task 5).
- Produces:
  - `Build.ExtraDefines(bool steam) -> string[]`: `["STEAM"]` or empty. `Run` gains a `bool steam` and passes it as `BuildPlayerOptions.extraScriptingDefines`; `Mac`, `LinuxMono` and `WindowsMono` read `-steam`. Nothing writes `PlayerSettings` defines, so there is nothing to restore.
  - `Build.IncludeNativePlugin(string assetPath, bool steam) -> bool`: false only for a path under `Packages/com.rlabrecque.steamworks.net/Plugins/` when `steam` is false; registered with `PluginImporter.SetIncludeInBuildDelegate` for each such plugin before `BuildPlayer` and cleared after. The Steam path is never stripped after the build, so a later macOS signature (#905) is never broken.
  - `public static class SteamStartRule { public static SteamStart Decide(uint appId, bool golden, Func<bool> restartNeeded, Func<bool> init); }` with `enum SteamStart { Quit, RunWithSteam, RunWithoutSteam }`. `appId == 0` or `golden` gives `RunWithoutSteam` without calling either function. Otherwise `restartNeeded()` true gives `Quit`: Valve's `RestartAppIfNecessary` is true when the game was not started by Steam and no `steam_appid.txt` is present, and Steam relaunches it. Otherwise `init()` true gives `RunWithSteam`, else `RunWithoutSteam` (one `Debug.LogWarning`, no-op services).
  - `SteamAppId.Value` (`uint`, `unity/Assets/Steam/SteamAppId.cs`), committed as `0` until the owner's app ID is known, then committed with it. Task 8's pytest pins it equal to `steam/steam.json`'s `app_id` (or `0` when that is `null`). A `-steam -shareable` build refuses while it is `0`; a `-steam` dev build is allowed.
  - `SteamPlatformServices`: `Name == "steam"`; `Unlock` calls `SteamUserStats.SetAchievement` per id, then `SteamUserStats.StoreStats()` once.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void Only_a_steam_build_gets_the_define()
{ CollectionAssert.AreEqual(new[] { "STEAM" }, Build.ExtraDefines(true)); CollectionAssert.IsEmpty(Build.ExtraDefines(false)); }
[TestCase("Packages/com.rlabrecque.steamworks.net/Plugins/x86_64/steam_api64.dll", false, false)]
[TestCase("Packages/com.rlabrecque.steamworks.net/Plugins/steam_api.bundle", false, false)]
[TestCase("Packages/com.rlabrecque.steamworks.net/Plugins/steam_api.bundle", true, true)]
[TestCase("Assets/Plugins/Other.dll", false, true)]
public void Valve_native_plugins_ship_only_in_steam_builds(string path, bool steam, bool included) => Assert.AreEqual(included, Build.IncludeNativePlugin(path, steam));
[TestCase(true, true, SteamStart.Quit)] [TestCase(true, false, SteamStart.Quit)] [TestCase(false, true, SteamStart.RunWithSteam)] [TestCase(false, false, SteamStart.RunWithoutSteam)]
public void The_start_rule_never_blocks_the_game(bool restart, bool init, SteamStart expected) => Assert.AreEqual(expected, SteamStartRule.Decide(480, false, () => restart, () => init));
[TestCase(0u, false)] [TestCase(480u, true)]
public void With_no_app_id_or_a_golden_run_steam_is_never_called(uint appId, bool golden)
{ Assert.AreEqual(SteamStart.RunWithoutSteam, SteamStartRule.Decide(appId, golden, () => throw new AssertionException("restart called"), () => throw new AssertionException("init called"))); }
```
(`Build` is in the editor assembly; reach it the way `BuildGateTests` reaches editor types, `unity/Assets/Tests/EditMode/BuildGateTests.cs:15-18`. Read the real plugin paths from the package once it is resolved and use them in the cases.)

- [ ] **Step 2: Run them to see them fail.**
- [ ] **Step 3: Implement.**
  - `SteamBootstrap` is a `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` that, outside the editor, calls `SteamStartRule.Decide(SteamAppId.Value, GoldenCheck.Requested(), () => SteamAPI.RestartAppIfNecessary(new AppId_t(SteamAppId.Value)), SteamAPI.Init)`.
  - On `Quit` it calls `Application.Quit()`. That takes effect at the end of the frame, so check that the relaunch path writes no autosave and no settings.
  - On `RunWithSteam` it sets `PlatformServices.Current` and creates one hidden `DontDestroyOnLoad` object that calls `SteamAPI.RunCallbacks()` in `Update` and `SteamAPI.Shutdown()` in `OnApplicationQuit`.
  - In the editor it never initialises Steam.
- [ ] **Step 4: Verify (maintainer, under the machine lock):**
  - The tests pass.
  - `scripts/unity_compile/compile.sh "<Unity.app>/Contents"` compiles `Steam.csproj` cleanly.
  - `uv run tools/release_build.py --platform mac --dev --steam` builds a player that passes `-golden` and logs `RunWithoutSteam`.
  - The Steam path before an app ID exists: put a local `steam_appid.txt` containing `480` beside that dev player (never committed; the release script refuses it in a Steam folder) and start it with the Steam client running. It logs `RunWithSteam` and the overlay opens.
  - A non-Steam `--dev` build's `.app` and folders contain no `steam_api*`.
  - Afterwards `git status --short unity/ProjectSettings` is empty.
- [ ] **Step 5: Commit** `Steam: an optional assembly started only in -steam builds (Refs #2149)`.

### Task 7: `release_build.py --steam`, clean non-Steam archives, and the nightly's Steam stage (Q7)

**Files:**
- Modify: `tools/release_build.py` (argument parser `:129-139`, `build_command` `:240-246`, archiving), `tools/nightly.py` (a last stage after the web publish; the stamp helper `:397-406`), `docs/RELEASING.md:102-127` and "Nightly builds" (`:192-224`)
- Test: `tools/tests/test_release_build.py`, `tools/tests/test_nightly.py`

**Interfaces:**
- Consumes: `-steam` (Task 6).
- Produces:
  - `--steam`: passes `-steam` to every build; writes `<out>/steam/<platform>/` (the player folder plus the notices, no archive) and records each folder's file list and SHA-256 in `build-record.json` under `"steam": true`. With `--dev` the record says `"dev": true` and `tools/steam_upload.py` refuses it.
  - `VALVE_FILES = re.compile(r"(^|/)(lib)?steam_api(64)?\.(dll|so|dylib)$|(^|/)steam_api\.bundle(/|$)|(^|/)steam_appid\.txt$")`: the macOS plugin is a bundle directory, so a zip lists `.../steam_api.bundle/` and files inside it.
  - `refuse_valve_files(names: list[str]) -> str | None` names any match in a finished non-Steam archive (a backstop: Task 6 already excludes the plugins at build time, so nothing is deleted after the build and a macOS signature is never broken); for `--steam` it refuses a folder holding `steam_appid.txt`.
  - `--nightly-version X.Y.Z` (only with `--steam`): accepts a tree whose only change is the nightly's `bundleVersion` stamp (`tools/nightly.py:397-406`) to `X.Y.Z`, and the nightly's tag in place of `v<bundleVersion>`; records `"channel": "nightly"`. Without it a Steam build records `"channel": "beta"`.
  - `tools/nightly.py`: a stage `steam`, run only after the web nightly is published. It runs `release_build.py --platform all --steam --tag <tonight> --nightly-version <X.Y.Z> --out <run folder>/steam-release`, then `steam_upload.py stage` (Task 8). On any failure it appends `steam=failed` (and the log folder) to the night's LOG.md line and notifies, and it never refuses, unpublishes or rolls back the web nightly. A `--no-steam` flag skips it, and the stage is skipped while `steam/steam.json` still has a `null` ID.

- [ ] **Step 1: Write the failing tests**: `test_a_non_steam_archive_never_carries_valve_files` (listings with `Statecraft_Data/Plugins/x86_64/steam_api64.dll`, `Statecraft.app/Contents/PlugIns/steam_api.bundle/` and `Statecraft.app/Contents/PlugIns/steam_api.bundle/Contents/MacOS/libsteam_api.dylib` are each refused by name); `test_steam_folders_are_written_not_archived` (dry-run prints `-steam` per platform and `steam/windows`, `steam/mac`, `steam/linux`); `test_a_steam_folder_with_steam_appid_txt_is_refused`; `test_a_dev_steam_build_is_recorded_as_dev`; `test_nightly_version_accepts_only_the_bundle_version_stamp` (any other dirty line still refuses); in `test_nightly.py`: `test_the_steam_stage_runs_only_after_publish`, `test_a_steam_failure_leaves_the_web_nightly_published` (fake runner fails the stage; the LOG line ends `steam=failed`, no rollback call), `test_the_steam_stage_is_skipped_while_ids_are_null`.
- [ ] **Step 2: Run them to see them fail:** `uv run --with pytest==9.1.1 pytest tools/tests/test_release_build.py tools/tests/test_nightly.py -q -k "steam or valve or nightly_version"`. Expected: 8 failed.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run the whole file, then `scripts/check.sh origin/main`.** Expected: all pass; the existing archive tests unchanged.
- [ ] **Step 5: Commit** `Release: Steam content folders and the nightly's Steam stage; GitHub archives refuse Valve files (Refs #2149)`.

### Task 8: VDF templates, the hand-over and the CI upload job (Q7, Q8)

**Files:**
- Create: `steam/steam.json` (`{"app_id": null, "depots": {"windows": null, "mac": null, "linux": null}, "branches": ["beta", "nightly"]}`), `steam/app_build.vdf.in`, `steam/depot_build.vdf.in` (written for this project from SteamPipe's documented keys, not copied from the SDK samples), `tools/steam_upload.py`, `.github/workflows/steam-upload.yml`
- Modify: `scripts/oss/tests/test_github_config.py` (one named exception in `test_ci_uses_no_secret_but_the_read_only_token` for `steam-upload.yml`, plus the tests below)
- Test: `tools/tests/test_steam_upload.py`, `scripts/oss/tests/test_github_config.py`

**Interfaces:**
- Consumes: `<out>/steam/<platform>/` and `build-record.json` with `"channel"` (Task 7).
- Produces:
  - `render(config: dict, root: Path, tag: str, commit: str, branch: str) -> list[Path]`: writes `app_build_<appid>.vdf` (`"AppID"`, `"Desc" "<tag> <commit8>"`, `"ContentRoot"` = `<root>`, `"BuildOutput"` = `<root>/../steam-output`, `"SetLive"` = `branch`, `"Preview" "0"`, one entry per depot) and one `depot_build_<depotid>.vdf` per platform mapping `<platform>/*` recursively to `.`. `branch` must be in `config["branches"]`; `default` is never accepted.
  - `refuse(config: dict, record: dict | None = None) -> str | None`: names every `null` ID; with a record, refuses a dirty tree, `"dev": true`, and a channel not in `branches`.
  - `stage(out: Path, tag: str, gh: Callable) -> None` (the owner's machine): zips each `steam/<platform>/` to `steam-<tag>-<platform>.zip`, writes `steam-record.json` (`tag`, `commit`, `branch` from the record's channel, each zip's `sha256` and `size`), creates the draft release `steam-<tag>` (`draft: true`, never published; an existing draft of that name is replaced only if it is a draft), uploads the assets, then runs `gh workflow run steam-upload.yml -f tag=<tag> -f branch=<branch>`.
  - CLI: `uv run tools/steam_upload.py render --root <dir> --tag <tag> --commit <sha> --branch <beta|nightly>` and `uv run tools/steam_upload.py stage --out <release folder> --tag <tag>`. Neither takes, reads or prints a credential, and neither runs steamcmd.
  - `steam-upload.yml`: `on: workflow_dispatch` with inputs `tag` (checked against `^v[0-9]+\.[0-9]+\.[0-9]+$` before use) and `branch` (`type: choice`, `beta` or `nightly`); top-level `permissions: {}`; one job `upload` on `ubuntu-latest` with `environment: steam` and `permissions: contents: write`. Its steps:
    1. Pinned checkout of `main` with no persisted credentials.
    2. Download the `steam-<tag>` draft's assets with `gh` and check them against `steam-record.json`.
    3. Refuse when `record.branch != inputs.branch` or `record.commit` is not an ancestor of `origin/main`.
    4. Run `steam_upload.py render`.
    5. Download steamcmd from Valve's official Linux URL and print its SHA-256.
    6. Write `STEAM_CONFIG_VDF` (base64-decoded) to steamcmd's `config/config.vdf` with mode 600.
    7. Run `steamcmd +@NoPromptForPassword 1 +login "$STEAM_BUILDER_USERNAME" +run_app_build "<vdf>" +quit` and fail unless its output says the build succeeded.
    8. An `if: always()` step deletes `config.vdf` and the temporary folder.
    9. On success, delete the draft release.
  - Only steps 6 and 7 reference the secrets, through `env:`, never on the command line; there is no `set -x`.

- [ ] **Step 1: Write the failing tests.**
  - `tools/tests/test_steam_upload.py`:
    - `test_null_ids_refuse_by_name`
    - `test_the_game_app_id_matches_steam_json`: parses `SteamAppId.cs` for its `Value` and compares it with `app_id`, `0` for `null`.
    - `test_a_dev_or_dirty_record_is_refused`
    - `test_render_matches_a_golden_vdf`: fixed IDs 1000/1001/1002/1003, branch `nightly`, checked against `tools/tests/data/steam/app_build_1000.vdf`.
    - `test_default_branch_is_never_rendered`
    - `test_stage_writes_hashes_and_only_a_draft`: `gh` is faked; every release call carries `draft: true` and no call publishes.
    - `test_stage_dispatches_with_the_records_branch`
    - `test_stage_keeps_the_executable_bits`: the Linux `Statecraft.x86_64` and the macOS `Contents/MacOS/*` keep mode `0o755` through the zip, because steamcmd uploads from a Linux runner.
    - `test_no_option_takes_a_credential`: the argument parser has no option containing `password`, `token`, `secret` or `vdf`.
    - `test_steamcmd_is_never_run_here`: `subprocess` is monkeypatched so any call naming `steamcmd` fails the test.
  - `scripts/oss/tests/test_github_config.py`:
    - `test_steam_upload_runs_only_on_dispatch`: no `push`, `pull_request`, `pull_request_target` or `release` trigger.
    - `test_steam_upload_uses_the_steam_environment_and_minimal_permissions`
    - `test_steam_upload_branch_input_is_beta_or_nightly`
    - `test_steam_secrets_appear_only_in_the_login_steps`
    - `test_the_secret_exception_names_only_steam_upload`
    - Existing tests already require every action to be pinned to a full SHA (`test_every_action_is_pinned_to_a_full_sha`).
- [ ] **Step 2: Run them to see them fail:** `uv run --with pytest==9.1.1 pytest tools/tests/test_steam_upload.py -q` and `uv run --with pytest==9.1.1 --with pyyaml==6.0.3 pytest scripts/oss/tests/test_github_config.py -q`.
- [ ] **Step 3: Confirm the one open GitHub fact before wiring:** that a workflow's default token with `contents: write` can list and download a draft release's assets (GitHub's REST docs for releases; record the page and date in the workflow's header comment). If it cannot, stop and bring the alternative (a short-lived workflow artifact uploaded by a second dispatch-only job) to the coordinator; do not invent a workaround.
- [ ] **Step 4: Implement.** Keep the workflow's header comment in the style of `player-golden.yml:1-30` (trust model, what it never does).
- [ ] **Step 5: Run them, then `scripts/check.sh origin/main`.** Expected: pass. `scripts/oss/check_repo.py` accepts the new `steam/` folder (add a `LICENSING.md` code-row path if it asks), and `uv run scripts/oss/pin_actions.py` reports nothing to pin.
- [ ] **Step 6: Commit** `Steam: VDF templates, a draft-release hand-over and the CI upload job (Refs #2149)`.

### Task 9: Docs: the owner's runbook, the checklist, privacy

**Files:**
- Create: `docs/steam/README.md` (the owner's runbook: every "Owner-only action" below in order, with where each value goes; the CI setup the owner does (spec 4.3.1 item 4: the `steam` environment limited to `main`, the secrets `STEAM_BUILDER_USERNAME` and `STEAM_CONFIG_VDF` piped in with `gh secret set --env steam`, and re-provisioning after a login failure); the App Admin settings: depots per OS, packages including Dev Comp, the `beta` and `nightly` branches with passwords, supported languages per spec 4.4 (English; Hebrew as game-support-only), one launch option per OS (`Statecraft.exe`, `Statecraft.app`, `Statecraft.x86_64`, `tools/release_build.py:102-109`), "App Bundles Are Notarized", system requirements, the five achievements with their API names from Task 2 and English names and descriptions drafted from `content/objectives.yaml` for the owner's approval, their icons' source (never adapted from the CC BY-NC-ND models; an `ASSETS.yaml` row each), Auto-Cloud exactly as spec 4.4 (Root OS `[Cross-Platform]`, the three roots, quota 1 MB and 2 files) with the date read, and the release calendar from spec section 5), `docs/steam/test-checklist.md` (Task 10's manual checks), `docs/steam/privacy.md` (the game collects nothing; no account; Steam's data is Valve's)
- Modify: `docs/RELEASING.md` (a "Steam" section after the desktop builds, `:102-127`: `release_build.py --steam`, the golden check, `steam_upload.py stage`, the CI job, and the owner's `default` set-live; and the nightly's `steam` stage in "Nightly builds"), `docs/steam/coming-soon-kit.md:288` (the Content Survey draft: re-checked against the Steam build, states free with no in-game purchases), `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md` (a one-line pointer at 5.3 to the new spec)
- Test: `uv run scripts/oss/md_links.py` and `scripts/check.sh origin/main`

- [ ] **Step 1: Write the three new files and the edits.** Cite file and line for every claim about the code; mark each Steam fact with the date read.
- [ ] **Step 2: Verify:** `scripts/check.sh origin/main` passes; every link resolves; `grep -n -i -E 'password|secret' docs/steam/*.md` shows only instructions for the owner, and no value.
- [ ] **Step 3: Commit** `Docs: the Steam runbook, the client checklist and the privacy statement (Refs #2149)`.

### Task 10: The first Steam builds on `beta` and `nightly` (after the MVP and #905; owner plus maintainer)

**Files:** none in the repository beyond the build record (separate repository, O12) and any fixes the checklist finds (each its own PR).

- [ ] **Step 1 (maintainer):** `uv run tools/release_build.py --platform all --tag vX.Y.Z --steam` on the merged `main`. Expected: three `steam/<platform>` folders, the macOS golden `GOLDEN OK` twice, the record written.
- [ ] **Step 2 (maintainer):** the golden checks on every Steam folder: Windows and Linux with `tools/player_golden_check.py` (or `player-golden.yml` on zips of them), macOS on both slices (arm64, and `arch -x86_64` under Rosetta, as `player-golden.yml` does). Expected: `GOLDEN OK` with the manifest hashes and `RunWithoutSteam` logged (the `-golden` rule, Task 6).
- [ ] **Step 3 (maintainer, after the owner's CI setup in #2151):** `uv run tools/steam_upload.py stage --out <folder> --tag vX.Y.Z`. Expected: a draft `steam-vX.Y.Z` with three zips and `steam-record.json`, and a `steam-upload.yml` run that logs in without a prompt, ends with SteamPipe's success line, sets the build live on `beta` and deletes the draft. A login failure means the owner re-provisions the secret; nobody else touches it.
- [ ] **Step 3b (maintainer):** the next published nightly runs its `steam` stage. Expected: the night's LOG line has no `steam=failed`, and a build is live on `nightly`.
- [ ] **Step 4 (owner, Steam client on each OS that ships, on `beta`):** run `docs/steam/test-checklist.md`: installs and starts from Steam; started outside Steam it relaunches through Steam; overlay opens (Shift+Tab) and the game's Tab focus still works with the overlay key changed; Quit works; Hebrew chosen in Settings survives a restart and an update; each achievement unlocks once (a test account or `Steam > Settings` reset); a save made on one OS continues on another through Auto-Cloud; the decision-card clips and the 1948 recording play (`VIDEO playing`, `RECORDING playing` in `Player.log`); the Linux depot on a Steam Deck (it runs natively in the Steam Linux Runtime, not under Proton) starts and plays a turn with the touchscreen and trackpads; on the notarised macOS build the overlay opens (the two entitlements of spec 4.3).
- [ ] **Step 5 (owner):** set the build live on `default` in App Admin when, and only when, the store page, the Content Survey and Valve's review are done.

## Owner-only actions (never an agent)

In order; each is its own line in the owner-only issue.
1. ~~Answer Q1-Q10~~ Done 1 Nov 2026 (#2150; spec section 6).
2. Open the Steamworks account as an individual (O5); identity, tax and bank forms.
3. Pay the Steam Direct fee (US$100 per app as read on 14 Oct 2026; re-check before paying). Not recouped by a free game.
4. Create the app as a free product; note the app ID and the three depot IDs, and commit them in `steam/steam.json` (or give them to the coordinator to commit; they are public numbers).
5. Create a dedicated builder account with only the upload permissions (14 Oct spec 5.4); log in once with steamcmd on the owner's own machine and confirm Steam Guard. The password never leaves the owner.
6. CI setup (spec 4.3.1 item 4): the repository environment `steam`, deployment branches `main` only; its secrets `STEAM_BUILDER_USERNAME` and `STEAM_CONFIG_VDF` (base64 of steamcmd's `config/config.vdf` from step 5, piped into `gh secret set STEAM_CONFIG_VDF --env steam` so it is never displayed); re-provision after a login failure.
7. App Admin: depots and OS, packages (store and Dev Comp), the `beta` and `nightly` branches and their passwords, supported languages, Auto-Cloud (from `docs/steam/README.md`), the five achievements and their icons, then publish the App Admin changes.
8. The Coming Soon page from the kit (#424), the Content Survey, review submissions.
9. macOS signing and notarisation with the Apple account (#905).
10. Every set-live on `default` (uploads to `beta` and `nightly` run in CI with the owner's secrets).

## Issue map

| Issue | Tasks |
| --- | --- |
| #2149 Epic: "Steam: release the game on Steam (Steamworks integration)" | all; links #54, #905, #45, #424 |
| #2151 Owner-only: account, fee, app, builder account, CI secrets, App Admin, store | Owner-only actions 2-10 |
| #2150 Owner decisions Q1-Q10 (answered, closed) | Owner-only action 1 |
| #2152 Platform seam and achievements | Tasks 1-2 (Task 3 dropped) |
| #2153 Desktop Quit button | Task 4 |
| #2154 Steamworks.NET and the Steam assembly | Tasks 5-6 |
| #2155 Steam content folders, the nightly stage, the hand-over and the CI upload job | Tasks 7-8 |
| #2156 Steam docs: runbook, checklist, privacy | Task 9 |
| #2157 First Steam builds on `beta` and `nightly`, and the client checklist | Task 10 |
