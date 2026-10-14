# Building Statecraft: Israel

The README gives the short version. This page has the exact commands. The full developer setup, including the Unity
Hub install command, is in [`docs/dev-setup.md`](docs/dev-setup.md).

## Requirements

- Unity **6000.3.25f1** (6.3 LTS) with Web Build Support. The exact version and changeset are in
  [`unity/ProjectSettings/ProjectVersion.txt`](unity/ProjectSettings/ProjectVersion.txt); other versions are not
  tested. You need your own Unity licence, activated in Unity Hub (sign in, then Preferences → Licenses) before any
  batch-mode command; Unity Personal is free within Unity's terms.
- .NET SDK 10.0.401 and [uv](https://github.com/astral-sh/uv) 0.12.5.
- No Git LFS: a plain `git clone` is complete.

## Run locally

```bash
git clone https://github.com/eyalgolan/statecraft-israel.git && cd statecraft-israel
uv run tools/build_sim_for_unity.py              # the sim DLLs Unity loads (build output, not committed)
export UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"   # macOS path
# Windows: "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"; Linux: ~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity
"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
python3 -m http.server 8000 --directory unity/Builds/Web-disabled   # then open http://localhost:8000/
```

Desktop players (macOS IL2CPP; Linux and Windows on Mono, built from the Mac) and their build record come from
`uv run tools/release_build.py`; see "Desktop builds" in step 5 of [`docs/RELEASING.md`](docs/RELEASING.md).

A build rewrites some cached settings. Drop them afterwards with
`git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`.

## In the editor

Unity Hub → **Add** → select `unity/`. Run `uv run tools/build_sim_for_unity.py` again after every change under `sim/`.
`tools/build_sim_for_unity.sh` is a thin wrapper that runs the same script.

### First open in Unity

The game scene is `Assets/Map.unity`, and it opens automatically. If it doesn't (you see an empty "Untitled" scene), double-click `Map` in the Project window's Assets folder. Then press Play.

Troubleshooting:

- `error CS0246: The type or namespace name 'PlayerPrefsBool' could not be found`: the Unity-MCP package (`com.ivanmurzak.unity.mcp`) was installed without its OpenUPM registry (`https://package.openupm.com`), so its dependency `extensions.unity.playerprefsex` is missing. Restore `Packages/manifest.json` and `Packages/packages-lock.json` from git (`git checkout -- unity/Packages`); they already carry the package and the registry.
- About 2,000 `error CS0234` (for example `'Json' does not exist in the namespace 'System.Text'`) from `com.ivanmurzak.unity.mcp` on a first open, and a batch run that then hangs: Unity ran with `CI=true`, `GITHUB_ACTIONS=true` or `TF_BUILD=true` in its environment, so the plugin skipped its NuGet download but added its defines. Run `git checkout -- unity/ProjectSettings/ProjectSettings.asset`, then run Unity again with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (every batch command in these docs has them).
- Compile errors from a package that isn't in our `Packages/manifest.json`: remove that package from your local manifest or update it. Unity can't run while there is any compile error.
- macOS says "Unity is damaged and can't be opened. Move to Trash": this can happen when the editor is launched outside Unity Hub after Hub installed modules (such as Mac Build Support) inside the app. Click **Cancel**, never Move to Trash. Open the project through Unity Hub. If Hub refuses too, reinstall that editor version (6000.3.25f1) through Unity Hub.

## Working with an AI agent in Unity (Unity-MCP)

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) (Apache-2.0) is the project's standard way to let an AI agent (Claude Code, Cursor, Copilot and others) work in the Unity editor: read the Console, inspect scenes, run tests. It runs locally, with no account and no cloud.

- **Install:** nothing to do. The package (`com.ivanmurzak.unity.mcp` 0.93.3) and the OpenUPM registry it needs are already in `unity/Packages/manifest.json`. On the first open, the plugin downloads its NuGet DLLs from nuget.org into `Assets/Plugins/NuGet/` (git-ignored) and its local server, `gamedev-mcp-server`, from the plugin's GitHub releases into `Library/`. Then Unity compiles once more.
- **Open** Window → AI Game Developer — MCP.
- **Connection: Custom, held by the project.** `UnityMcpGuard` keeps the plugin in Custom (local) mode in every editor, so Cloud is disabled. If you pick Cloud in the window, the setting goes back to Custom when the plugin saves and nothing connects to the cloud, but the window can keep showing Cloud (with its sign-in section) and the local server stops: reopen the window and click Start. Never sign in: nothing in this project needs an ai-game.dev account. The plugin's own `UNITY_MCP_CONNECTION_MODE` variable would override this; the project doesn't support that. Your settings file, `unity/UserSettings/AI-Game-Developer-Config.json` (per user, git-ignored, holds your local token), may still say Cloud: the project's choice is applied in memory and never written there, so don't edit the file by hand.
- **Server URL:** leave it as the window shows it, with the transport `http`. The port is derived from your project's path, so every clone and worktree gets its own server. A second editor finds an already-running server on its port and attaches to it, so two projects never share one as long as their ports differ.
- **Authorization: token, held by the project.** The local server checks a Bearer token (your local token). Never choose "Account sign-in" (oauth): it makes the local server validate tokens online against ai-game.dev.
- **The local server starts with the editor.** If you don't use an agent, turn off *Keep server running* in the window.
- **Choose your agent** and click **Configure**, or click **Generate** to write skill files instead. Configure writes the server URL and the Bearer header into `unity/.mcp.json` (for Claude Code). Start your agent in `unity/`, or point it at that file. The files these steps write (`.mcp.json`, `unity/.claude/` and the other agent folders) are git-ignored; don't commit them.
- **The rules still apply:** no game rules in Unity (they live in the sim), tests first, sources for every fact in `content/`, and disclose the AI assistance in your pull request (see "Rules every change follows" and "AI-assisted contributions" in [CONTRIBUTING.md](CONTRIBUTING.md)).
- **Security:** the local server lets the agent run editor actions, including editing files and executing code in the editor. It listens on loopback only and rejects browser pages from other sites, and the token keeps other local programs out. Keep it local: never expose the server's port (no port forwarding, tunnels or `0.0.0.0` binds), and stop the server when you're done.
- **Not connecting?** Connect only works in an interactive editor: in batch mode (tests and builds) the plugin is held offline on purpose, and its windows don't open. Check that the server shows Running on the port in the Server URL, then click Connect. The first attempt can fail while the server is still starting; the plugin retries. If the editor is stopped without quitting, `gamedev-mcp-server` can stay behind on the port: stop it with the window's Stop button or by its PID.
- **Batch mode and builds:** `UnityMcpGuard` (`unity/Assets/Editor/UnityMcpGuard.cs`) keeps the plugin from connecting, starting its server or opening a window in batch mode (tests and builds), and drops its assemblies from every player build. The browser build's golden hashes don't change. Run batch Unity with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (as every command in these docs does): the plugin treats `CI`, `GITHUB_ACTIONS` or `TF_BUILD` set to `true` in the environment as CI (every CI runner sets at least one of them, and so do some agent shells), and then it skips its NuGet download but still adds its defines, and a fresh clone fails to compile. The update popup is off for the team (`unity/ProjectSettings/AI-Game-Developer-UpdateSettings.asset`); version bumps come by pull request. On every Unity-MCP version bump, run the Unity EditMode tests in batch mode and paste the `UnityMcpGuardTests` result in the pull request.
- **Leave one change uncommitted:** the plugin adds `UNITY_MCP_READY;UNITY_MCP_DEPS_7` to the scripting defines in `unity/ProjectSettings/ProjectSettings.asset` once its DLLs are in place. Don't commit that change: on a fresh clone the defines would arrive before the DLLs, and the plugin wouldn't compile. A policy test refuses it. The post-build cleanup (`git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`) drops the defines too; the plugin adds them back on the next open, at the cost of one extra compile.
- **Per-user state the plugin keeps:** besides the settings file, it stores `Unity-MCP.MainWindow.Initialized` and `Unity_MCP_SelectedAiAgent` in this project's editor PlayerPrefs, and the server's PID and update-check state in EditorPrefs.

## Tests

The test commands, and why Unity tests are not in CI, are in [`CONTRIBUTING.md`](CONTRIBUTING.md) ("Rules every change follows", "What CI runs"). The browser check of a build is `tools/web_check.py` ([`docs/m3-release.md`](docs/m3-release.md)).

## Repository layout

- `sim/`: the simulation, a pure C# library (netstandard2.1) with xUnit tests. No Unity dependency.
- `content/`: the game's data (YAML and JSON): people, offices, parties, events, map data. Every fact carries `sources:`.
- `unity/`: the Unity project, which presents the sim. No game rules live here.
- `tools/`: Python data tools (map and rainfall pipelines, browser check), run with `uv`.
- `scripts/oss/`: repository policy checks.
