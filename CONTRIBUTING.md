# Contributing to Statecraft: Israel

Thanks for helping. This file covers how to get set up, the rules every change follows, and how pull requests are reviewed.

## Ways to help

- **Code:** the C# simulation (`sim/`), the Unity client (`unity/`), and the Python map and data tools (`tools/`).
- **History:** fact-checking, sourcing, and drafting new events, ministers and dates (`content/`).
- **Art:** 3D settlements, vegetation and units for the era-correct look. See the roadmap in the [README](README.md).
- **Design and writing:** event choices, balance, and the text the player reads.

If you're unsure where to start, open an issue first and describe what you want to do.

**Picking an issue.** Start with an open [`good first issue`](https://github.com/eyalgolan/statecraft-israel/issues?q=is%3Aopen+label%3A%22good+first+issue%22). If you read Hebrew, you can take one of the native-speaker review slices. Comment "Taking this" before you start. Issues with the label `ready for a contributor` come from the **Task** issue form: a goal, the files likely touched, the commands that must pass, the agreed sources, what needs the maintainer's sign-off, and whether Unity is needed. When every field is filled, you can finish the work without asking the maintainer. If a field is missing or unclear, ask in the issue before you start. The label `needs-unity` marks an issue you cannot finish without Unity (see "Without Unity" in [docs/dev-setup.md](docs/dev-setup.md)); `area: unity` only says the issue touches `unity/`.

## Licence of your contribution

There is no CLA and no sign-off. By opening a pull request you agree that your contribution is licensed under this repository's licences, as GitHub's Terms of Service (section D.6) also say:

- code under [MIT](LICENSE);
- `content/` under [CC BY-SA 4.0](LICENSES/CC-BY-SA-4.0.txt);
- other original material under [CC BY 4.0](LICENSES/CC-BY-4.0.txt).

[LICENSING.md](LICENSING.md) lists which licence covers each path. Only contribute work you have the right to license this way. If you add a third-party file, include its source and licence. Material from the Unity Asset Store is never accepted.

## Setup

Follow [docs/dev-setup.md](docs/dev-setup.md). In short:

- **Sim and tools only:** .NET SDK 10 and `uv`. Unity is not needed.
- **Unity work:** Unity 6000.3.25f1 (6.3 LTS) with Web Build Support, a free Unity account, and running `uv run tools/build_sim_for_unity.py` before you open `unity/`.

## Rules every change follows

1. **Game rules live in the sim, not in Unity.** Unity draws, reads input and calls sim helpers. A rule written in a presenter is a bug.
2. **Determinism.** The same seed and commands must give the same state hash.
   - If your change alters the simulation on purpose, the golden hashes will change.
   - Record the new values everywhere they are pinned (see [docs/determinism.md](docs/determinism.md)), with a one-line reason in its history table.
3. **Tests first.** Add or change a test that fails without your change, then make it pass. Run:
   - `dotnet test sim/Statecraft.sln`, which must pass with 0 warnings (warnings are errors);
   - the tools tests (they need the .NET SDK too: one runs the sim's release gate):
     `uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 --with pyproj==3.8.0 pytest tools/tests -q`;
   - the repository policy checks:
     `uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q` and `uv run scripts/oss/check_repo.py` (the command CI runs);
   - Unity EditMode tests, if you touched `unity/` (`-buildTarget OSXUniversal` keeps an earlier WebGL build from leaving the editor on the wrong platform):
     `"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log`. There must be 0 `warning CS` in the log.
4. **Text the player sees** has no internal ids, gives dates as "15 May 1948", and shows money as "IL 1.2m" or "$1.2m". The tests enforce this.
5. **Keep the pull request focused:** one change per PR, and no unrelated refactors.
6. **Changelog.** If you change `sim/`, `content/` or `unity/` (tests excepted), add a line under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md). CI's `Changelog` job checks it; the maintainer can add the `no-changelog` label to a change no player would notice.
7. **Rule IDs.** A rule that has an ID (`R-ECON-014`, defined in `docs/rules/`, see [docs/rules/README.md](docs/rules/README.md)) is cited by that ID in the test that checks it (`// R-ECON-014`), in YAML comments that depend on it, and in the PR body ("implements R-ECON-014"). `uv run scripts/oss/rule_ids.py` fails on a rule with no citing test and on a citation of an ID that does not exist.
8. **Provenance.** Every new image, video, font, audio, 3D-model or map-data file gets a row in [ASSETS.yaml](ASSETS.yaml) with its source, licence and status. CI's `Repo policy` job fails on a file with no row.
9. **Roadmap status.** If your PR finishes a Phase 1 PR or a whole phase of [#374](https://github.com/eyalgolan/statecraft-israel/issues/374), update the Roadmap table in [README.md](README.md#roadmap) and the same table in [README.he.md](README.he.md), and tick the item in #374 if you can (otherwise ask the maintainer in the PR). The Phase 1 count must match in both files; `test_readme.py` checks it.

## History and content rules

- **Adding an event?** Follow [docs/WRITING_EVENTS.md](docs/WRITING_EVENTS.md): every field, what the loader rejects, and how to run the tests.
- **Every fact needs a source.** Every event, minister, office term, date and number in `content/` carries `sources:` with a URL and the date you checked it. A claim without a source can't be merged.
- **No invented quotes.** Paraphrase, and never put words in a real person's mouth.
- **Framing.** The project tells the story from the Israeli perspective. Contested narratives are not shown as parallel accounts.
- **Sensitive events.** Events tagged `holocaust`, `terror`, `massacre` or `war-casualties` need the maintainer's sensitivity review before they can ship. A release build refuses to package until that review is done.
- **Reporting a mistake.** Use the **Historical accuracy** issue form; it asks for the source that shows the correct fact.

## AI-assisted contributions

AI-assisted work is welcome. Say so in the pull request: which parts, and how you checked them. You are responsible for every line you submit. For content, check every fact against its source yourself, and never take one from an AI tool unchecked.

## Working with an AI agent in Unity (Unity-MCP)

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) (Apache-2.0) is the project's standard way to let an AI agent (Claude Code, Cursor, Copilot and others) work in the Unity editor: read the Console, inspect scenes, run tests. It runs locally, with no account and no cloud.

- **Install:** nothing to do. The package (`com.ivanmurzak.unity.mcp` 0.93.3) and the OpenUPM registry it needs are already in `unity/Packages/manifest.json`. On the first open, the plugin downloads its NuGet DLLs from nuget.org into `Assets/Plugins/NuGet/` (git-ignored) and its local server, `gamedev-mcp-server`, from the plugin's GitHub releases into `Library/`. Then Unity compiles once more.
- **Open** Window → AI Game Developer — MCP.
- **Connection: Custom, held by the project.** `UnityMcpGuard` keeps the plugin in Custom (local) mode in every editor, so Cloud is disabled. If you pick Cloud in the window, the setting goes back to Custom when the plugin saves and nothing connects to the cloud, but the window can keep showing Cloud (with its sign-in section) and the local server stops: reopen the window and click Start. Never sign in: nothing in this project needs an ai-game.dev account. The plugin's own `UNITY_MCP_CONNECTION_MODE` variable would override this; the project doesn't support that. Your settings file, `unity/UserSettings/AI-Game-Developer-Config.json` (per user, git-ignored, holds your local token), may still say Cloud: the project's choice is applied in memory and never written there, so don't edit the file by hand.
- **Server URL:** leave it as the window shows it, with the transport `http`. The port is derived from your project's path, so every clone and worktree gets its own server. A second editor finds an already-running server on its port and attaches to it, so two projects never share one as long as their ports differ.
- **Authorization: token, held by the project.** The local server checks a Bearer token (your local token). Never choose "Account sign-in" (oauth): it makes the local server validate tokens online against ai-game.dev.
- **The local server starts with the editor.** If you don't use an agent, turn off *Keep server running* in the window.
- **Choose your agent** and click **Configure**, or click **Generate** to write skill files instead. Configure writes the server URL and the Bearer header into `unity/.mcp.json` (for Claude Code). Start your agent in `unity/`, or point it at that file. The files these steps write (`.mcp.json`, `unity/.claude/` and the other agent folders) are git-ignored; don't commit them.
- **The rules still apply:** no game rules in Unity (they live in the sim), tests first, sources for every fact in `content/`, and disclose the AI assistance in your pull request (see "Rules every change follows" and "AI-assisted contributions" above).
- **Security:** the local server lets the agent run editor actions, including editing files and executing code in the editor. It listens on loopback only and rejects browser pages from other sites, and the token keeps other local programs out. Keep it local: never expose the server's port (no port forwarding, tunnels or `0.0.0.0` binds), and stop the server when you're done.
- **Not connecting?** Connect only works in an interactive editor: in batch mode (tests and builds) the plugin is held offline on purpose, and its windows don't open. Check that the server shows Running on the port in the Server URL, then click Connect. The first attempt can fail while the server is still starting; the plugin retries. If the editor is stopped without quitting, `gamedev-mcp-server` can stay behind on the port: stop it with the window's Stop button or by its PID.
- **Batch mode and builds:** `UnityMcpGuard` (`unity/Assets/Editor/UnityMcpGuard.cs`) keeps the plugin from connecting, starting its server or opening a window in batch mode (tests and builds), and drops its assemblies from every player build. The browser build's golden hashes don't change. Run batch Unity with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (as every command in these docs does): the plugin treats `CI`, `GITHUB_ACTIONS` or `TF_BUILD` set to `true` in the environment as CI (every CI runner sets at least one of them, and so do some agent shells), and then it skips its NuGet download but still adds its defines, and a fresh clone fails to compile. The update popup is off for the team (`unity/ProjectSettings/AI-Game-Developer-UpdateSettings.asset`); version bumps come by pull request. On every Unity-MCP version bump, run the Unity EditMode tests in batch mode and paste the `UnityMcpGuardTests` result in the pull request.
- **Leave one change uncommitted:** the plugin adds `UNITY_MCP_READY;UNITY_MCP_DEPS_7` to the scripting defines in `unity/ProjectSettings/ProjectSettings.asset` once its DLLs are in place. Don't commit that change: on a fresh clone the defines would arrive before the DLLs, and the plugin wouldn't compile. A policy test refuses it. The post-build cleanup (`git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`) drops the defines too; the plugin adds them back on the next open, at the cost of one extra compile.
- **Per-user state the plugin keeps:** besides the settings file, it stores `Unity-MCP.MainWindow.Initialized` and `Unity_MCP_SelectedAiAgent` in this project's editor PlayerPrefs, and the server's PID and update-check state in EditorPrefs.

## Pull requests

- Base your branch on `main`. History on `main` is protected, so no force-pushes.
- In the description, say what changed and why. Paste the test commands you ran and their output; a description of the output doesn't count.
- The maintainer reviews every PR (`.github/CODEOWNERS`). Once the maintainer approves your latest commit and every check on it has passed, `.github/workflows/merge-approved.yml` squash-merges it within about ten minutes. A new push needs a new approval.

## What CI runs

Every pull request and every push to `main` runs `Sim (dotnet test)`, `Tools (pytest)`, `Repo policy`, and, on pull requests, `Changelog`. The `Changelog` job also runs `scripts/oss/reviewed_by_guard.py`: a commit with an AI co-author trailer may not add, change or remove a `reviewed_by` or `reviewed_on` line in `content/` (only a person signs a row off, #375). The jobs use no secrets, call no paid API, run no AI agent and push nothing.

**Unity EditMode tests are not in CI.** Running Unity in CI needs the maintainer's Unity account credentials stored as repository secrets. GitHub never gives secrets to pull requests from forks, so the job could not run on the pull requests that most need it, and those credentials would sit on machines that run outside code. So the Unity tests are a local gate: paste their result in your PR, and the maintainer re-runs them before merging any change under `unity/`. A future Unity job must run Unity with `-CI false -GITHUB_ACTIONS false -TF_BUILD false` (runners set `CI=true`, and GitHub Actions and Azure Pipelines also set `GITHUB_ACTIONS=true` or `TF_BUILD=true`; the Unity-MCP plugin treats any of them as CI, then skips its NuGet download and fails to compile), or commit the plugin's DLLs.

## Security

Never commit keys, tokens or `.env` files. Secret scanning and push protection are on. To report a vulnerability, use **Security → Report a vulnerability** on the repository page (GitHub private reporting); don't open a public issue. What is in scope and what to expect: [SECURITY.md](SECURITY.md). How the maintainer reviews outside pull requests without running their code: [docs/UNTRUSTED_PR_REVIEW.md](docs/UNTRUSTED_PR_REVIEW.md).

## Conduct

Be respectful. Argue about facts with sources, not with people. Political or personal attacks are not tolerated. The full [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) applies in every project space, including its section on historical and political discussion. The content rules, and how to dispute a fact, are in [docs/CONTENT_POLICY.md](docs/CONTENT_POLICY.md). A short sourcing checklist, including what to do when a source cannot be reached, is in [docs/SOURCING_FOR_CONTRIBUTORS.md](docs/SOURCING_FOR_CONTRIBUTORS.md).
