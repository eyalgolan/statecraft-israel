# Steamworks Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the owner release the free game on Steam for Windows, macOS and Linux, with Steam started only in Steam builds, five achievements, Auto-Cloud saves, English and Hebrew, and an owner-run SteamPipe upload, without changing the web build.

**Architecture:** A Steam-free seam in `Statecraft.Game` (`unity/Assets/Scripts/Platform/`) holds the interface and all logic worth testing; a thin `Statecraft.Steam` assembly (`unity/Assets/Steam/`, compiled only under the `STEAM` define, which only `-steam` builds set) adapts it to Steamworks.NET. `tools/release_build.py --steam` makes ContentBuilder folders, and `tools/steam_upload.py` renders the VDF scripts and, run by the owner in a terminal, calls the owner's `steamcmd`.

**Tech Stack:** Unity 6000.3.25f1 (C#, UI Toolkit, NUnit EditMode), Steamworks.NET 2025.165.0 via UPM (MIT; Steamworks SDK 1.65), Python 3.11+ with `uv` and pytest, SteamPipe ContentBuilder (owner's machine only).

**Spec:** `docs/superpowers/specs/2026-11-01-steamworks-integration-design.md` (owner questions Q1-Q10 in its section 6), on top of `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md`.

## Global Constraints

- Free forever: no price, no microtransactions, no paid DLC, no ads, no in-game store (`docs/oss/DECISIONS.md:15`, `:49-50`).
- Never commit the Steamworks SDK, its headers or tools, or any Valve binary (`steam_api*`, `steamcmd`); never commit `steam_appid.txt`.
- No agent and no CI job spends money, creates an account, holds or types a credential, or runs `steamcmd` or any SDK binary. Every Steamworks action in "Owner-only actions" is the owner's.
- Steamworks.NET is pinned to `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.165.0`, commit `ea51b029a5dbcb47a99d3c1fac930b811672ed32`.
- `STEAM` is never in the committed `scriptingDefineSymbols`; only `-steam` builds add it, and they restore it in `finally`.
- The sim (`sim/`) and the golden hashes do not change. Steam only reads game state.
- `companyName: Statecraft` and `productName: Statecraft Israel` stay frozen (they name the save folder and the Auto-Cloud paths).
- Unity runs only through `uv run tools/machine_lock.py --shared --wait-unity -- ...`, with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (`CONTRIBUTING.md:61-63`). EditMode is a local gate (`CONTRIBUTING.md:114`).
- Player-visible strings in both `unity/Assets/UI/strings.en.yaml` and `strings.he.yaml`. Changelog fragment `changelog.d/<branch>.md` for any change under `unity/` (`AGENTS.md:60`).
- No resource name or description uses an em dash; use a hyphen.
- Which tasks may start now depends on Q1 (recommended: Tasks 1-9 now; Task 10 after the MVP and #905).

## Review Focus

1. **A Steam build started without the Steam client, or with Steam offline.** The player expects the game to start (relaunched through Steam, or playing on with no achievements), never a crash or a black screen. Task 6 tests `SteamStartRule.Decide` for each outcome, including no app ID.
2. **A non-Steam desktop archive (GitHub, itch.io) containing Valve's library.** Expected: refused before archiving. Task 7 tests the refusal.
3. **A player who chose a language before, now on Steam in another language.** Expected: their stored choice wins; Steam's language only seeds a first start. Task 3 tests stored-vs-platform precedence.
4. **A save loaded from a save made on another OS through Steam Cloud, or an older achievement already unlocked.** Expected: the game loads it (deterministic sim) and re-reports nothing twice. Task 2 tests that the reporter unlocks only newly earned achievements, including after a load of an already-finished game.
5. **An objective added to `content/objectives.yaml` later.** Expected: a test fails until it has an achievement mapping (or is listed as deliberately unmapped). Task 2 pins it.

---

## File structure

| Path | Responsibility | Task |
| --- | --- | --- |
| `unity/Assets/Scripts/Platform/IPlatformServices.cs` | The interface the game asks a store for | 1 |
| `unity/Assets/Scripts/Platform/PlatformServices.cs` | `PlatformServices.Current` and the no-op `NullPlatformServices` | 1 |
| `unity/Assets/Scripts/Platform/AchievementRules.cs` | Which achievement API names a `GameState` has earned | 2 |
| `unity/Assets/Scripts/Platform/AchievementReporter.cs` | Watches `SimBridge.Changed`, unlocks only what is new | 2 |
| `unity/Assets/Scripts/Platform/PlatformLanguage.cs` | Steam language name to UI language | 3 |
| `unity/Assets/Scripts/Platform/SteamStartRule.cs` | Whether a Steam build quits, runs with Steam or runs without it | 6 |
| `unity/Assets/Scripts/UI/UiLanguage.cs` | Gains the platform language as the last fallback | 3 |
| `unity/Assets/Scripts/UI/QuitButton.cs` | Desktop-only Quit on the start screen and in Settings | 4 |
| `unity/Packages/manifest.json`, `packages-lock.json` | The pinned Steamworks.NET dependency | 5 |
| `unity/ProjectSettings/SteamworksNETSettings.json` | Turns off the package's own define management | 5 |
| `unity/Assets/Steam/Statecraft.Steam.asmdef` | `defineConstraints: ["STEAM"]` | 6 |
| `unity/Assets/Steam/SteamBootstrap.cs` | Init, restart, callbacks, shutdown | 6 |
| `unity/Assets/Steam/SteamPlatformServices.cs`, `SteamAppId.cs` | `IPlatformServices` over Steamworks.NET; the app ID (`0` until known) | 6 |
| `unity/Assets/Editor/Build.cs` | `-steam` flag, `Build.WithSteamDefine` | 6 |
| `scripts/unity_compile/Steam.csproj`, `compile.sh` | Compile the Steam assembly outside Unity when the package is cached | 6 |
| `tools/release_build.py` | `--steam` folders; strip and refuse Valve files in other archives | 7 |
| `steam/steam.json`, `steam/app_build.vdf.in`, `steam/depot_build.vdf.in` | IDs (null until the owner has them) and VDF templates | 8 |
| `tools/steam_upload.py` | Render VDFs; owner-only `--upload` | 8 |
| `docs/RELEASING.md`, `docs/steam/README.md`, `docs/steam/test-checklist.md`, `docs/steam/privacy.md` | Owner runbook, manual checks, privacy statement | 9 |

---

### Task 1: The platform seam

**Files:**
- Create: `unity/Assets/Scripts/Platform/IPlatformServices.cs`, `unity/Assets/Scripts/Platform/PlatformServices.cs`
- Test: `unity/Assets/Tests/EditMode/PlatformServicesTests.cs`

**Interfaces:**
- Produces:
  - `public interface IPlatformServices { string Name { get; } string? UiLanguage { get; } void Unlock(IReadOnlyCollection<string> achievementIds); }` in namespace `Statecraft.Game`.
  - `public static class PlatformServices { public static IPlatformServices Current { get; set; } }`, never null (setting null restores the default).
  - `public sealed class NullPlatformServices : IPlatformServices { public static readonly NullPlatformServices Instance; }`: `Name == "none"`, `UiLanguage == null`, `Unlock` does nothing.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void The_default_is_the_no_op_services()
{ Assert.AreSame(NullPlatformServices.Instance, PlatformServices.Current); Assert.AreEqual("none", PlatformServices.Current.Name); Assert.IsNull(PlatformServices.Current.UiLanguage); }
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

### Task 3: Steam's language seeds the first start

**Files:**
- Create: `unity/Assets/Scripts/Platform/PlatformLanguage.cs`
- Modify: `unity/Assets/Scripts/UI/UiLanguage.cs:27-37` and `:48-49`
- Test: `unity/Assets/Tests/EditMode/PlatformLanguageTests.cs` (and the existing `UiLanguage` tests keep passing)

**Interfaces:**
- Produces:
  - `public static class PlatformLanguage { public static string? FromSteam(string? steamLanguage); }`: `"hebrew"` to `UiLanguage.Hebrew`, `"english"` to `UiLanguage.English`, anything else or null to null.
  - `UiLanguage.AtStart(string? stored, string? url, string[] args, bool webgl, string? platform = null)`: precedence flag (`-lang` / `?lang=`) > stored > `platform` > English. The parameterless-platform overload at `:48-49` passes `PlatformServices.Current.UiLanguage`.

- [ ] **Step 1: Write the failing tests**

```csharp
[TestCase("hebrew", "he")] [TestCase("english", "en")] [TestCase("german", null)] [TestCase(null, null)]
public void Steam_language_names_map_to_ui_languages(string? steam, string? ui) => Assert.AreEqual(ui, PlatformLanguage.FromSteam(steam));
[Test] public void A_stored_choice_beats_the_platform_language()
{ Assert.AreEqual("en", UiLanguage.AtStart("en", null, new string[0], false, platform: "he")); }
[Test] public void The_platform_language_applies_only_with_no_stored_choice()
{ Assert.AreEqual("he", UiLanguage.AtStart(null, null, new string[0], false, platform: "he")); }
[Test] public void The_command_line_flag_beats_everything()
{ Assert.AreEqual("en", UiLanguage.AtStart("he", null, new[] { "-lang", "en" }, false, platform: "he")); }
```

- [ ] **Step 2: Run them to see them fail.** Expected: `PlatformLanguage` not found.
- [ ] **Step 3: Implement.** Keep the existing four-argument behaviour identical when `platform` is null.
- [ ] **Step 4: Run them to see them pass**, plus every existing test whose name contains `Language`.
- [ ] **Step 5: Commit** `UI: a store's language seeds the first start; a stored choice still wins (Refs #2149)`.

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
- Modify: `unity/Packages/manifest.json` (add `"com.rlabrecque.steamworks.net": "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.165.0"`), `unity/Packages/packages-lock.json` (written by Unity; its `hash` must be `ea51b029a5dbcb47a99d3c1fac930b811672ed32`), `.gitignore` (`unity/steam_appid.txt`), `THIRD-PARTY-NOTICES.md:44-52` (Steamworks.NET MIT with "Copyright (c) 2013-2022 Riley Labrecque"; Steam builds carry Valve's `steam_api` redistributable under the Steamworks SDK Access Agreement)
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
    assert not [n for n in names if re.search(r"(^|/)(lib)?steam_api(64)?\.(dll|so|dylib|lib|bundle)|steam_appid\.txt|steamcmd", n)]
def test_the_package_may_not_manage_define_symbols():
    assert json.loads((REPO / "unity/ProjectSettings/SteamworksNETSettings.json").read_text()) == {"<key Unity writes>": False}
```
Replace `<key Unity writes>` with the key the settings page actually writes (read the file after toggling; do not guess).

- [ ] **Step 2: Run them to see them fail:** `uv run --with pytest==9.1.1 pytest scripts/oss/tests/test_steam_package.py -q`. Expected: 3 failed, 1 passed (the no-binary test passes already).
- [ ] **Step 3: Add the dependency, open the project once in Unity under the machine lock so it resolves the package and writes the lock, toggle the setting, add the ignore line and notices.**
- [ ] **Step 4: Verify:** the tests pass; `git status --short unity/ProjectSettings/ProjectSettings.asset` is empty after a second editor open; `git ls-files | grep -i steam_api` prints nothing; the full EditMode suite passes; a WebGL build (`Build.Web`) is the same size within 1% (the package excludes WebGL).
- [ ] **Step 5: Commit** `Unity: Steamworks.NET 2025.165.0, pinned by tag and commit (Refs #2149)`.

### Task 6: The Steam assembly and `-steam` builds (Q3)

**Files:**
- Create: `unity/Assets/Scripts/Platform/SteamStartRule.cs`, `unity/Assets/Steam/Statecraft.Steam.asmdef` (`references: ["Statecraft.Game", "com.rlabrecque.steamworks.net"]`, `includePlatforms: ["Editor", "LinuxStandalone64", "macOSStandalone", "WindowsStandalone64"]`, `defineConstraints: ["STEAM"]`), `unity/Assets/Steam/SteamBootstrap.cs`, `unity/Assets/Steam/SteamPlatformServices.cs`, `unity/Assets/Steam/SteamAppId.cs`, `scripts/unity_compile/Steam.csproj`
- Modify: `unity/Assets/Editor/Build.cs:258-289` (`Mac`, `StandaloneMono`), `scripts/unity_compile/compile.sh` (also compile `Steam.csproj` with `STEAM` when `unity/Library/PackageCache/com.rlabrecque.steamworks.net@*` exists)
- Test: `unity/Assets/Tests/EditMode/SteamBuildTests.cs`

**Interfaces:**
- Consumes: `IPlatformServices`, `PlatformServices.Current` (Task 1); `PlatformLanguage.FromSteam` (Task 3); Steamworks.NET (Task 5).
- Produces:
  - `Build.WithSteamDefine(string defines, bool steam) -> string` (pure: adds or removes `STEAM` in a `;` list, keeps the rest and their order). `Build.Mac`, `LinuxMono`, `WindowsMono` read `-steam`, set the Standalone defines through it before `Run`, and restore the committed value in `finally`.
  - `public static class SteamStartRule { public static SteamStart Decide(uint appId, Func<bool> restartNeeded, Func<bool> init); }` with `enum SteamStart { Quit, RunWithSteam, RunWithoutSteam }`, in `unity/Assets/Scripts/Platform/SteamStartRule.cs` so EditMode reaches it without `STEAM`. `appId == 0` gives `RunWithoutSteam` without calling either function; else `restartNeeded()` true gives `Quit` (Steam relaunches the game); else `init()` true gives `RunWithSteam`; else `RunWithoutSteam` (one `Debug.LogWarning`, no-op services).
  - `SteamAppId.Value` (`uint`, `unity/Assets/Steam/SteamAppId.cs`), committed as `0` until the owner's app ID is known, then committed with it. Task 8's pytest pins it equal to `steam/steam.json`'s `app_id` (or `0` when that is `null`). A `-steam -shareable` build refuses while it is `0`; a `-steam` dev build is allowed and runs without Steam.
  - `SteamPlatformServices`: `Name == "steam"`; `UiLanguage == PlatformLanguage.FromSteam(SteamApps.GetCurrentGameLanguage())`; `Unlock` calls `SteamUserStats.SetAchievement` per id, then `SteamUserStats.StoreStats()` once.

- [ ] **Step 1: Write the failing tests**

```csharp
[TestCase("", true, "STEAM")] [TestCase("A;B", true, "A;B;STEAM")] [TestCase("A;STEAM;B", false, "A;B")] [TestCase("STEAM", true, "STEAM")]
public void The_steam_define_is_added_or_removed(string before, bool steam, string after) => Assert.AreEqual(after, Build.WithSteamDefine(before, steam));
[Test] public void The_committed_project_never_defines_steam()
{ StringAssert.DoesNotContain("STEAM", PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Replace("STEAMWORKS_NET", "")); }
[TestCase(true, true, SteamStart.Quit)] [TestCase(true, false, SteamStart.Quit)] [TestCase(false, true, SteamStart.RunWithSteam)] [TestCase(false, false, SteamStart.RunWithoutSteam)]
public void The_start_rule_never_blocks_the_game(bool restart, bool init, SteamStart expected) => Assert.AreEqual(expected, SteamStartRule.Decide(480, () => restart, () => init));
[Test] public void With_no_app_id_steam_is_never_called()
{ Assert.AreEqual(SteamStart.RunWithoutSteam, SteamStartRule.Decide(0, () => throw new AssertionException("restart called"), () => throw new AssertionException("init called"))); }
```
(`Build` is in the editor assembly; reach it the way `BuildGateTests` reaches editor types, `unity/Assets/Tests/EditMode/BuildGateTests.cs:15-18`.)

- [ ] **Step 2: Run them to see them fail.**
- [ ] **Step 3: Implement.** `SteamBootstrap` is a `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` that, outside the editor, calls `SteamStartRule.Decide(SteamAppId.Value, () => SteamAPI.RestartAppIfNecessary(new AppId_t(SteamAppId.Value)), SteamAPI.Init)`; on `Quit` it calls `Application.Quit()`; on `RunWithSteam` it sets `PlatformServices.Current` and creates one hidden `DontDestroyOnLoad` object that calls `SteamAPI.RunCallbacks()` in `Update` and `SteamAPI.Shutdown()` in `OnApplicationQuit`. In the editor it never initialises Steam. The golden check path (`unity/Assets/Scripts/Boot/GameBoot.cs:54`, `:65`) must behave identically with and without `STEAM`.
- [ ] **Step 4: Verify:** the tests pass; `scripts/unity_compile/compile.sh "<Unity.app>/Contents"` compiles `Steam.csproj` cleanly; `uv run tools/release_build.py --platform mac --dev --steam` (maintainer, under the machine lock) builds, its player starts and logs `RunWithoutSteam` (app ID `0`) and passes `-golden`; afterwards `git status --short unity/ProjectSettings` is empty.
- [ ] **Step 5: Commit** `Steam: an optional assembly started only in -steam builds (Refs #2149)`.

### Task 7: `release_build.py --steam` and clean non-Steam archives

**Files:**
- Modify: `tools/release_build.py` (argument parser `:129-139`, `build_command` `:240-246`, archiving), `docs/RELEASING.md:102-127`
- Test: `tools/tests/test_release_build.py`

**Interfaces:**
- Consumes: `-steam` (Task 6).
- Produces:
  - `--steam`: passes `-steam` to every build; writes `<out>/steam/<platform>/` (the player folder plus the notices, no archive) and records each folder's file list and SHA-256 in `build-record.json` under `"steam": true`. With `--dev` the record says `"dev": true` and `tools/steam_upload.py` refuses it.
  - `VALVE_FILES = re.compile(r"(^|/)(lib)?steam_api(64)?\.(dll|so|dylib|bundle)$|(^|/)steam_appid\.txt$")`.
  - `strip_valve_files(folder: Path) -> list[str]` removes matches from a non-Steam player folder before archiving (with their `.meta`-less plugin folders); `refuse_valve_files(names: list[str]) -> str | None` names any match in a finished non-Steam archive; and for `--steam` refuses a folder holding `steam_appid.txt`.

- [ ] **Step 1: Write the failing tests**: `test_a_non_steam_archive_never_carries_valve_files` (a fake player folder with `Statecraft_Data/Plugins/steam_api64.dll` is stripped, and a listing with it is refused by name); `test_steam_folders_are_written_not_archived` (dry-run prints `-steam` per platform and `steam/windows`, `steam/mac`, `steam/linux`); `test_a_steam_folder_with_steam_appid_txt_is_refused`; `test_a_dev_steam_build_is_recorded_as_dev`.
- [ ] **Step 2: Run them to see them fail:** `uv run --with pytest==9.1.1 pytest tools/tests/test_release_build.py -q -k "steam or valve"`. Expected: 4 failed.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run the whole file, then `scripts/check.sh origin/main`.** Expected: all pass; the existing archive tests unchanged.
- [ ] **Step 5: Commit** `Release: Steam content folders; GitHub archives never carry Valve files (Refs #2149)`.

### Task 8: VDF templates and the owner's upload tool (Q7, Q8)

**Files:**
- Create: `steam/steam.json` (`{"app_id": null, "depots": {"windows": null, "mac": null, "linux": null}, "beta_branch": "beta"}`), `steam/app_build.vdf.in`, `steam/depot_build.vdf.in` (from the SDK sample structure, rewritten here: no comment or field copied verbatim beyond the key names SteamPipe requires), `tools/steam_upload.py`
- Test: `tools/tests/test_steam_upload.py`

**Interfaces:**
- Consumes: `<out>/steam/<platform>/` and `build-record.json` (Task 7).
- Produces:
  - `render(config: dict, out: Path, tag: str, commit: str) -> list[Path]`: writes `app_build_<appid>.vdf` (`"AppID"`, `"Desc" "vX.Y.Z <commit8>"`, `"ContentRoot"` = `<out>/steam`, `"BuildOutput"` = `<out>/steam-output`, `"SetLive"` = `beta_branch`, `"Preview" "0"`, one entry per depot) and one `depot_build_<depotid>.vdf` per platform mapping `<platform>/*` recursively to `.`.
  - `refuse(config) -> str | None`: names every `null` ID; `--upload` also refuses when `sys.stdin.isatty()` is false, when `--steamcmd` is missing or inside any checkout, when the build record's tree was dirty, and when the record says `"dev": true`.
  - CLI: `uv run tools/steam_upload.py --out <release folder> [--upload --steamcmd <path to steamcmd.sh> --account <builder account name>]`. Without `--upload` it renders and prints the exact command for the owner. With it, it runs `steamcmd +login <account> +run_app_build <vdf> +quit` with inherited stdin, so steamcmd asks for the password and Steam Guard itself. No option or environment variable takes a password.

- [ ] **Step 1: Write the failing tests**: `test_null_ids_refuse_by_name`; `test_the_game_app_id_matches_steam_json` (parses `SteamAppId.cs` for its `Value` and compares with `app_id`, `0` for `null`); `test_a_dev_record_is_refused`; `test_render_matches_a_golden_vdf` (fixed IDs 1000/1001/1002/1003, checked against `tools/tests/data/steam/app_build_1000.vdf`); `test_upload_refuses_without_a_terminal` (stdin not a TTY); `test_no_password_option_exists` (`--help` text has no `password`, and the argument parser has no option containing it); `test_the_command_never_runs_in_tests` (subprocess is monkeypatched to fail the test if called).
- [ ] **Step 2: Run them to see them fail:** `uv run --with pytest==9.1.1 pytest tools/tests/test_steam_upload.py -q`.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run them, then `scripts/check.sh origin/main`.** Expected: pass. `scripts/oss/check_repo.py` accepts the new `steam/` folder (add a `LICENSING.md` code-row path if it asks).
- [ ] **Step 5: Commit** `Steam: VDF templates and an upload tool only the owner can run (Refs #2149)`.

### Task 9: Docs: the owner's runbook, the checklist, privacy

**Files:**
- Create: `docs/steam/README.md` (the owner's runbook: every "Owner-only action" below in order, with where each value goes; the App Admin settings: depots per OS, packages including Dev Comp, the `beta` branch, supported languages per spec 4.4, the five achievements with their API names from Task 2, English and Hebrew names and descriptions drafted from `content/objectives.yaml` for the owner's approval, and the Auto-Cloud roots from spec 4.4, each marked "re-check on the Steam Cloud page"), `docs/steam/test-checklist.md` (Task 10's manual checks), `docs/steam/privacy.md` (the game collects nothing; no account; Steam's data is Valve's)
- Modify: `docs/RELEASING.md` (a "Steam" section after the desktop builds, `:102-127`: `release_build.py --steam`, the golden check, `steam_upload.py`, the owner's upload and `default` set-live), `docs/steam/coming-soon-kit.md:288` (the Content Survey draft: re-checked against the Steam build, states free with no in-game purchases), `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md` (a one-line pointer at 5.3 to the new spec)
- Test: `uv run scripts/oss/md_links.py` and `scripts/check.sh origin/main`

- [ ] **Step 1: Write the three new files and the edits.** Cite file and line for every claim about the code; mark each Steam fact with the date read.
- [ ] **Step 2: Verify:** `scripts/check.sh origin/main` passes; every link resolves; `grep -n -i -E 'password|secret' docs/steam/*.md` shows only instructions that the owner types them into steamcmd.
- [ ] **Step 3: Commit** `Docs: the Steam runbook, the client checklist and the privacy statement (Refs #2149)`.

### Task 10: The first Steam beta build (after the MVP and #905; owner plus maintainer)

**Files:** none in the repository beyond the build record (separate repository, O12) and any fixes the checklist finds (each its own PR).

- [ ] **Step 1 (maintainer):** `uv run tools/release_build.py --platform all --tag vX.Y.Z --steam` on the merged `main`. Expected: three `steam/<platform>` folders, the macOS golden `GOLDEN OK` twice, the record written.
- [ ] **Step 2 (maintainer):** the Windows and Linux golden checks on the Steam folders (zip them only for `player-golden.yml`, or run `tools/player_golden_check.py` on each). Expected: `GOLDEN OK` with the manifest hashes; Steam init absent, `RunWithoutSteam` logged.
- [ ] **Step 3 (owner):** `uv run tools/steam_upload.py --out <folder> --upload --steamcmd <path> --account <builder>`; steamcmd prompts. Expected: `Successfully finished AppID <id> build` and the build on `beta`.
- [ ] **Step 4 (owner, Steam client on each OS that ships):** run `docs/steam/test-checklist.md`: installs and starts from Steam; started outside Steam it relaunches through Steam; overlay opens (Shift+Tab) and the game's Tab focus still works with the overlay key changed; Quit works; a fresh install in Hebrew Steam starts in Hebrew and a stored English choice stays English; each achievement unlocks once (a test account or `Steam > Settings` reset); a save made on one OS continues on another through Auto-Cloud; the decision-card clips and the 1948 recording play (`VIDEO playing`, `RECORDING playing` in `Player.log`); Linux build on a Steam Deck or under Proton starts and plays a turn with mouse and touch.
- [ ] **Step 5 (owner):** set the build live on `default` in App Admin when, and only when, the store page, the Content Survey and Valve's review are done.

## Owner-only actions (never an agent)

In order; each is its own line in the owner-only issue.
1. Answer Q1-Q10 (spec section 6).
2. Open the Steamworks account as an individual (O5); identity, tax and bank forms.
3. Pay the Steam Direct fee (US$100 per app as read on 14 Oct 2026; re-check before paying). Not recouped by a free game.
4. Create the app as a free product; note the app ID and the three depot IDs, and commit them in `steam/steam.json` (or give them to the coordinator to commit; they are public numbers).
5. Create a dedicated builder account with only the upload permissions (14 Oct spec 5.4); log in once with steamcmd on the Mac and confirm Steam Guard. The password never leaves the owner.
6. App Admin: depots and OS, packages (store and Dev Comp), the `beta` branch and its password, supported languages, Auto-Cloud (from `docs/steam/README.md`), the five achievements and their icons, then publish the App Admin changes.
7. Written confirmations for Q9 (Tal Heuberger's models; the ElevenLabs permission's scope), recorded in `docs/oss/DECISIONS.md`.
8. The Coming Soon page from the kit (#424), the Content Survey, review submissions.
9. macOS signing and notarisation with the Apple account (#905).
10. Every upload (Task 10 step 3) and every set-live on `default`.

## Issue map

| Issue | Tasks |
| --- | --- |
| #2149 Epic: "Steam: release the game on Steam (Steamworks integration)" | all; links #54, #905, #45, #424 |
| #2151 Owner-only: account, fee, app, builder account, App Admin, store | Owner-only actions 2-10 |
| #2150 Owner decisions Q1-Q10 | Owner-only action 1 |
| #2152 Platform seam, achievements, language | Tasks 1-3 |
| #2153 Desktop Quit button | Task 4 |
| #2154 Steamworks.NET and the Steam assembly | Tasks 5-6 |
| #2155 Steam content folders and the upload tool | Tasks 7-8 |
| #2156 Steam docs: runbook, checklist, privacy | Task 9 |
| #2157 First Steam beta build and the client checklist | Task 10 |
