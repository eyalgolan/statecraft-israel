# Steamworks integration: design (owner request, 1 November 2026)

- **Date:** 1 November 2026. **Status: DECIDED (design); not built.** The owner answered Q1-Q10 on 1 November 2026
  (#2150; section 6 quotes the chosen option labels). Nothing here has been bought, signed up for, uploaded or configured
  on Steamworks.
- **Request (owner, verbatim):** "in parralel to everything else, I've downloaded the steamworks sdk as we will need to
  upload the game there in the future. therefore in parralel to everything else create a plan using the superpowers for
  the steamworks integration work and create issues for this."
- **Builds on:** `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md` (the desktop plan, #482) and
  the owner's answers in `docs/oss/DECISIONS.md:43-60`. This document only covers what is new or changed since then; it
  does not repeat that spec's research. Read against `origin/main` at `7e6957a59` (1 November 2026).
- **Plan:** `docs/superpowers/plans/2026-11-01-steamworks-integration.md`. **Tracking:** epic #2149 (linked from
  the plan's PR); roadmap tracker #54.
- Unity was not run for this document. Steam facts not re-read today are marked **re-check** and carry the date they were
  last read.

## 1. Fixed facts this design must respect

| Fact | Source |
| --- | --- |
| The game is free, forever: "there is not going to be a paid release" (22 Oct); "there is always going to be free - even when we release to steam", "the game is never going to charge any money. ever", "the game is never going to have ads" (23 Oct) | `docs/oss/DECISIONS.md:15`, `:49-50` |
| So on Steam: a free product, no microtransactions, no DLC for money, no ads, no in-game store | follows from the row above |
| The Steamworks account is opened as an individual; opening it, the fee and the tax and bank forms are the owner's | `docs/oss/DECISIONS.md:53` (O5) |
| Wrapper: Steamworks.NET | `docs/oss/DECISIONS.md:59` (O11) |
| Platforms: Windows, macOS and Linux, all built on the owner's Mac (Unity Personal cannot run on CI machines); Windows and Linux on Mono, macOS on IL2CPP | `docs/oss/DECISIONS.md:48`, `:55-56` (O1, O7, O8) |
| Bundle ID `io.github.eyalgolan.statecraftisrael` (O6), already set | `docs/oss/DECISIONS.md:54`; `unity/ProjectSettings/ProjectSettings.asset:174` |
| The MVP is web only; desktop players and Steam come after it | `docs/oss/DECISIONS.md:48` (20 Oct ruling, #814); `docs/mvp-checklist.md:178`; `docs/RELEASING.md:5-6` |
| Desktop players still need signing and notarisation (macOS, owner's Apple credentials), a true Universal build and LZ4HC/size checks | issue #905 |
| The Steam trailer comes from the real build | `docs/oss/DECISIONS.md:84` |
| Build records live in a separate repository the owner creates | `docs/oss/DECISIONS.md:60` (O12) |
| Code licence: PolyForm Noncommercial 1.0.0 (the owner may still grant permissions); Tal Heuberger's 146 original model files are CC BY-NC-ND 4.0 | `docs/oss/DECISIONS.md:14`; `LICENSING.md:16` |
| Three sound cues ship under "written permission from ElevenLabs for publishing these files in this repo" | `docs/oss/DECISIONS.md:88` |
| Unity EditMode tests are a local gate, never CI | `CONTRIBUTING.md:114` |
| Agents never spend money, never enter or hold credentials, never run SDK binaries | owner standing rules for this project |

## 2. What the repository does today (desktop and Steam)

- **Players.** `Build.Mac` (IL2CPP, `unity/Assets/Editor/Build.cs:258-269`), `Build.LinuxMono` and `Build.WindowsMono`
  (`Build.cs:272-289`) all call `Run`, which builds with `BuildOptions.None` (`Build.cs:301-318`). No scripting define is
  set: `scriptingDefineSymbols: {}` (`ProjectSettings.asset:832`).
- **Release script.** `tools/release_build.py` builds the three players on the Mac under the machine lock, runs the golden
  check on the macOS player, archives each with the notices and writes a build record (`tools/release_build.py:1-50`,
  platform table `:102-109`). It never uploads (`:42-46`). `.github/workflows/player-golden.yml:1-30` runs the Windows
  and Linux players' golden check on GitHub's machines with no Unity licence and no secret.
- **Identity.** `companyName: Statecraft`, `productName: Statecraft Israel` (`ProjectSettings.asset:15-16`),
  `bundleVersion: 0.3.2` (`:150`).
- **Saves.** One autosave, `autosave.txt`, written through a `.tmp` and replaced (`unity/Assets/Scripts/SaveStore.cs:11-15`,
  `:32`). On desktop it lives in `Application.persistentDataPath/saves` (`SaveStore.cs:24-25`). The save is the seed and
  the command log, and the sim is bit-identical across .NET, Mono, IL2CPP and WebGL (`docs/determinism.md`), so a save
  made on one OS loads on another.
- **Settings.** `settings.txt` in `persistentDataPath/settings` (`unity/Assets/Scripts/SettingsStore.cs:24-25`), one
  `key=value` per line; the UI language is the `lang` key (`unity/Assets/Scripts/UI/SettingsPresenter.cs:21`).
- **Language.** English unless Hebrew is asked for: a `-lang he` command-line flag on desktop, else the stored choice, else
  English (`unity/Assets/Scripts/UI/UiLanguage.cs:27-37`; read at `unity/Assets/Scripts/UI/GameRoot.cs:168`). Event
  text is English only until #55; the advisor clips have English voice only (`unity/Assets/StreamingAssets/advisors/*.en.wav`).
- **Objectives.** Four MVP objectives, `railway`, `burma_road`, `yishuvim`, `absorb_immigrants`
  (`content/objectives.yaml:6`, `:18`, `:31`, `:45`), read with `Objectives.Status` (`sim/Statecraft.Sim/Objectives.cs:36-38`);
  the game ends with one of the `GameOverCodes` (`sim/Statecraft.Sim/GameState.cs:11-22`), `mvp_ended` for reaching the
  last turn. The Unity client sees each change through `SimBridge.Changed` (`unity/Assets/Scripts/UI/SimBridge.cs:44`).
- **Quit.** No quit control in the UI: the only `Application.Quit` calls are the golden check and the profiler
  (`unity/Assets/Scripts/Boot/GameBoot.cs:54`, `:65`; `unity/Assets/Scripts/Map/FrameProfile.cs:129`).
- **Keyboard.** Tab and Shift+Tab move UI focus (`unity/Assets/Scripts/UI/NavigationGuard.cs:9`;
  `docs/superpowers/plans/2026-10-14-a11y-keyboard-map.md:26`). Shift+Tab is also the Steam overlay's default key.
- **Compile check outside Unity.** `scripts/unity_compile/Game.csproj:9` compiles every `.cs` under
  `unity/Assets/Scripts/`, with Unity's define list (`scripts/unity_compile/Common.props:22`).
- **Store preparation already on `main`:** the Coming Soon kit and text (`docs/steam/coming-soon-kit.md`,
  `docs/steam/store-page-draft.md`; #424), a draft Content Survey (`docs/steam/coming-soon-kit.md:288`), the store-page
  checklist and screenshot preflight (`docs/RELEASING.md:356-412`, `tools/store_screenshots.py`; #45), the AI disclosure
  generator (`scripts/oss/ai_disclosure.py`; `ASSETS.yaml:7`).

## 3. The SDK the owner downloaded

`~/Downloads/steamworks_sdk_165.zip`, 25,023,007 bytes, SHA-256
`8c42792e09100988e31e3dc069de2eb1bc60702a0445bb37298ba0c54067c202`. Unzipped outside the repository and inspected as
text only; no binary in it was run.

- `sdk/Readme.txt`: "Copyright (c) 1996-2026, Valve Corporation, All rights reserved"; v1.65 dated 23 July 2026. v1.65
  removes `ISteamUtils::IsRunningOnSteamDeck()` and adds `IsRunningOnSteamHardware()`, `GetSteamHardwareDefaultConfig()`
  and `IsRunningUnderProton()`. The zip carries no licence text; the terms are the Steamworks SDK Access Agreement, which
  the 14 Oct spec quotes (section 5.2: source "solely to develop the Licensee Software"; only `redistributable_bin` may be
  distributed, with the game, in object code).
- `sdk/public/steam/`: the C++ headers and `steam_api.json`.
- `sdk/redistributable_bin/`: `win64/steam_api64.dll`, `osx/libsteam_api.dylib`, `linux64/libsteam_api.so` (plus
  32-bit, arm64 and Android builds).
- `sdk/tools/ContentBuilder/`: `builder/`, `builder_osx/steamcmd.sh`, `builder_linux/`, the sample scripts
  `scripts/app_build_1000.vdf`, `depot_build_1001.vdf`, `depot_build_1002.vdf`, `simple_app_build.vdf`, and
  `run_build.bat`, whose sample passes the account password on the command line. This project never does that: the
  owner types it into steamcmd's own prompt, once, and Steam Guard confirms the machine.
- Also `sdk/tools/ContentServer`, `goldmaster`, `codesigning`, `linux` and `sdk/steamworksexample` (not needed).

**What the project uses from it:** only ContentBuilder (`steamcmd`), on the owner's Mac, kept outside every checkout. The
runtime library reaches the game through Steamworks.NET's package (section 4.2), not from this zip.

Steamworks.NET (MIT): release `2025.165.0`, published 29 October 2026, tag commit
`ea51b029a5dbcb47a99d3c1fac930b811672ed32`; its `com.rlabrecque.steamworks.net/package.json` says version `2025.165.0`,
licence MIT; its `Plugins/` folder carries Valve's `steam_api64.dll`, `steam_api.bundle` and `libsteam_api.so` (GitHub API,
read 1 November 2026). It matches SDK 1.65.

## 4. Design

### 4.1 Shape: one seam, one optional assembly

- **`Statecraft.Game` stays Steam-free.** A small seam, `unity/Assets/Scripts/Platform/`, holds the interface
  `IPlatformServices` and a static `PlatformServices.Current` that defaults to a no-op implementation. Everything the game
  asks of a store goes through it. Pure logic that decides *what* to do (which achievements are earned, whether a Steam
  build may call Steam at start) lives here too, so EditMode and the dotnet compile check test it with no Steam.
- **`Statecraft.Steam` is a separate assembly** in `unity/Assets/Steam/` (outside `Assets/Scripts/`, so
  `scripts/unity_compile/Game.csproj:9` never compiles it), with `"defineConstraints": ["STEAM"]` and Standalone plus Editor
  platforms. Without the `STEAM` define Unity does not compile it at all, so the WebGL build, itch.io, the nightly and every
  contributor's editor are unchanged. With it, a `RuntimeInitializeOnLoadMethod` bootstrap starts Steam, sets
  `PlatformServices.Current`, pumps `SteamAPI.RunCallbacks()` each frame and calls `SteamAPI.Shutdown()` on quit.
- **The `STEAM` define is set only by the build**, never committed: `Build.Mac`, `Build.LinuxMono` and `Build.WindowsMono`
  take a `-steam` flag, and `Run` (`Build.cs:301-318`) passes it as `BuildPlayerOptions.extraScriptingDefines`, which
  applies to that player build only and never writes `ProjectSettings.asset` (review of #2148, C9).
- **Valve's native library is excluded at build time from non-Steam players.** Steamworks.NET's managed assembly compiles
  into every desktop player (its asmdef has no define constraint), but a non-Steam build excludes its native plugins with
  `PluginImporter.SetIncludeInBuildDelegate`, so there is nothing to strip afterwards and a later macOS signature is never
  broken by a deletion (review C6). The release script still refuses an archive that lists one, as a backstop.
- **A golden check never calls Steam.** With `-golden` on the command line the Steam bootstrap calls neither
  `RestartAppIfNecessary` nor `Init`. Valve: `RestartAppIfNecessary` returns true, and the game must quit, whenever the
  game was not started by the Steam client and no `steam_appid.txt` is present (steam_api page, read 1 Nov 2026). Without
  this rule, a Steam player could never run the player golden check (review C1).
- **The sim is untouched.** Steam reads the game state; it never feeds the sim. No golden hash moves.

### 4.2 The SDK and the licence (Q2: "Never commit; UPM wrapper (Recommended)")

- Never commit the SDK, its headers, its tools or any Valve binary. The scratch unzip lives outside the repository.
- Steamworks.NET comes in through UPM as a git dependency pinned to the tag *and* checked by the commit in
  `unity/Packages/packages-lock.json`, like `com.ivanmurzak.unity.mcp` (`unity/Packages/manifest.json:3`). UPM downloads it
  into `unity/Library/PackageCache` (ignored, `.gitignore:46`), so Valve's redistributable reaches a build without ever
  sitting in this repository.
- Only Steam builds may carry `steam_api*`: the release script deletes the Steamworks.NET native plugins from a non-Steam
  player folder before archiving and refuses an archive that still lists one (GitHub Releases and itch.io stay free of
  Valve files).
- `THIRD-PARTY-NOTICES.md` "Builds" (`THIRD-PARTY-NOTICES.md:44-52`) gains Steamworks.NET (MIT, "Copyright (c) 2013-2022
  Riley Labrecque", its LICENSE.md at the pinned commit), whose managed assembly is in every desktop build, and a line that
  Steam builds also carry Valve's `steam_api` redistributable under the Steamworks SDK Access Agreement.
  `LICENSING.md` needs no row: nothing Valve's is in the tree.

### 4.3 Steam builds and channels

- A Steam build is a separate flavour: `uv run tools/release_build.py --steam ...` builds with `-steam` and writes plain
  content folders `steam/<platform>/` for ContentBuilder instead of archives. GitHub and itch.io keep today's archives.
- With `STEAM`, the bootstrap calls `SteamAPI.RestartAppIfNecessary(appId)` first, so a Steam build started outside the
  Steam client relaunches through Steam; a failed `SteamAPI.Init` logs one line and the game runs on with the no-op
  services (a Steam build must never refuse to start because Steam is slow).
- The app ID and depot IDs are public numbers, committed in `steam/steam.json` once the owner has them; `null` until
  then, and every Steam step refuses while they are `null`. `steam_appid.txt` is a development aid only and is never
  shipped (the release script refuses a Steam folder containing it). Before an app ID exists, a maintainer can exercise
  the Steam path with a `--dev --steam` player and a local `steam_appid.txt` holding Valve's test app 480 beside it,
  with the Steam client running; that file is never committed and never reaches a release folder.
- **macOS Steam builds** need the entitlements `com.apple.security.cs.disable-library-validation` (to load the Steamworks
  library and the overlay) and `com.apple.security.cs.allow-dyld-environment-variables` (to inject the overlay), and never
  the App Sandbox (Valve, Platforms page, read 1 Nov 2026). The signing step of #905 adds them for Steam builds only, and
  the owner ticks "App Bundles Are Notarized" in Steamworks. Notarisation stays required for Steam: "Steam will require
  all new macOS Applications to be 64-bit and notarized by Apple" (same page).
- **Depots:** one per OS (Windows x64, macOS Universal, Linux x64), each with its OS set in App Admin (owner).
- **Branches (Q7: "Also nightlies").** A release tag `vX.Y.Z` goes to a password-protected `beta` branch, and every
  published nightly also goes to a password-protected `nightly` branch, each with `SetLive`. The owner tests a `beta` build
  and sets it live on `default` by hand in App Admin; Valve does not allow scripting the default branch (14 Oct spec
  section 5.4). A nightly never touches `beta` or `default`.
- **Nightly (Q7).** Today the nightly builds and publishes the web only (`docs/RELEASING.md:192-224`). A new last stage,
  `steam`, runs only after the web nightly is published. It builds the three Steam players with
  `release_build.py --steam --nightly-version X.Y.Z` (the nightly's stamped, uncommitted version; `tools/nightly.py:397-406`),
  checks the macOS golden, stages them and starts the CI upload to `nightly`. A failure there is logged as `steam=failed` on
  the night's LOG line and notified; it never unpublishes or refuses the web nightly. It adds three desktop builds to every
  night, so its time is measured on the first runs and capped like the other stages.

#### 4.3.1 The upload runs in GitHub Actions (Q8: "GitHub Actions")

Unity still builds only on the owner's Mac (O7: a Personal licence cannot run on CI), so CI uploads the players the Mac
built; it never builds them.

1. **Hand-over.** `tools/steam_upload.py stage --out <release folder> --tag <tag>` (on the owner's Mac, by the release
   steps or the nightly) zips each `steam/<platform>/` as `steam-<tag>-<platform>.zip`, writes `steam-record.json` (tag,
   commit, target branch, each zip's SHA-256 and size) and attaches them to a **draft** release named `steam-<tag>` that is
   never published. Draft releases are visible only to people with write access, so Valve's library never becomes a
   public download. It then runs `gh workflow run steam-upload.yml -f tag=<tag> -f branch=<beta|nightly>` with the owner's
   own `gh` login.
2. **`.github/workflows/steam-upload.yml`** runs only on `workflow_dispatch`, never on a pull request or a fork. The job
   uses the environment `steam`, whose secrets GitHub gives only to runs from `main`. It has
   `permissions: contents: write`, needed to read and then delete the draft; confirm before wiring that the default token
   lists draft releases (**re-check**, GitHub docs). Its steps:
   1. Download the draft's assets and check every SHA-256 against `steam-record.json`.
   2. Refuse a `null` ID in `steam/steam.json`, a `branch` other than `beta` or `nightly`, a record whose branch differs
      from the input, and a record whose commit is not on `main`.
   3. Render the VDFs with `steam_upload.py render`.
   4. Install steamcmd from Valve's official Linux download (URL **re-check**), and log the SHA-256 of what it fetched.
   5. Write the login cache from the secret into steamcmd's config folder, run
      `steamcmd +@NoPromptForPassword 1 +login "$STEAM_BUILDER_USERNAME" +run_app_build <vdf> +quit`, and delete the cache
      in an `always()` step.
   6. On success, delete the draft release.
   Every action is pinned to a full SHA (`scripts/oss/pin_actions.py`). No step prints a secret: no `set -x`, and the
   secrets are masked.
3. **Steam Guard in CI.** steamcmd keeps a login token after one interactive login with Steam Guard. The job restores that
   cache, so it never needs the password or a Steam Guard code. When the token expires or Valve rejects the machine, the job
   fails at login with no prompt, and the owner re-provisions the secret. If a cached login turns out not to transfer to
   GitHub's machines (**re-check** with Valve's SteamCMD page before relying on it), the fallback is the builder account's
   Steam Guard mobile-authenticator secret stored as a third secret, used to generate the code in the job. Choosing it is a
   new owner decision.
4. **What the owner sets up (agents never create, read or print any of it).**
   1. Create the builder account with only the upload permissions.
   2. On the owner's own machine, run steamcmd once interactively from a new temporary folder: `+login <builder account>`
      with the password and the Steam Guard code, then `+quit`. Use a temporary folder because agents work on that Mac
      too, and a cached login left there would let any process upload without a password (review C2).
   3. In the repository's settings, create the environment `steam` with deployment branches limited to `main`.
   4. Add two environment secrets: `STEAM_BUILDER_USERNAME`, and `STEAM_CONFIG_VDF`, the base64 of steamcmd's
      `config/config.vdf` from step 2. Pipe it straight in, for example
      `base64 < <steamcmd folder>/config/config.vdf | gh secret set STEAM_CONFIG_VDF --env steam`, so it is never shown.
      The exact config path depends on where steamcmd was installed (**re-check**). Then delete the temporary steamcmd
      folder, so no cached builder login stays on the machine.
   5. Re-run steps 2 and 4 whenever the job reports a login failure.

The local `--upload` mode with a TTY check in the first draft of this design is dropped: CI is the only upload path.

### 4.4 Features

| Feature | Design | Owner question |
| --- | --- | --- |
| Init and shutdown | Section 4.1; guarded; WebGL and non-Steam builds unaffected | Q1, Q3 |
| Languages | Valve lists Hebrew only under "Game Support Only": "These languages are not supported in Steamworks APIs", with no API code (Languages page, read 1 Nov 2026). So the store lists English (interface, subtitles, full audio) and Hebrew (interface) as a game-support-only language; achievement names and descriptions are English only; and the game never reads Steam's language (it could never be Hebrew). The in-game choice and `-lang` stay as they are (`UiLanguage.cs:27-37`). The first draft's Steam-language seeding is dropped (review C3) | none |
| Steam Cloud | Auto-Cloud (App Admin, no code) for `saves/autosave.txt` only; settings stay per machine (quality and UI scale depend on the screen) | Q6 |
| Achievements | Five, read from the game state after each change: one per objective (the four ids above) and one for reaching the last turn (`mvp_ended`). Unlocked through `SteamUserStats.SetAchievement` and `StoreStats`; never from the sim | Q5 |
| Overlay | Works with no code (macOS: the entitlements in 4.3). Shift+Tab is the overlay's default and the game's focus-back key: documented on the store page and in the test checklist; an alternative in-game key is a UX follow-up for the keyboard map, not part of this plan | none |
| Steam Deck and controllers | No controller support and no Deck claim at launch; one smoke test on a Deck before release. With a native Linux depot, a Deck runs the Linux build (in the Steam Linux Runtime), not the Windows build under Proton, so the test is of the Linux depot (review C4) | Q10 |
| Rich presence, stats, leaderboards, workshop, DRM, inventory, microtransactions | Not planned (YAGNI; microtransactions and paid items are ruled out) | none |
| Quit | A desktop "Quit" button (Settings and start screen), hidden on WebGL; Steam review and Deck need a way out of a full-screen game (14 Oct spec section 2.5) | none |

**Steam Cloud roots** (Unity's `persistentDataPath` page for 6000.3 and Valve's Steam Cloud page, both read 1 Nov 2026).
Unity: Windows `%userprofile%\AppData\LocalLow\<companyname>\<productname>`; macOS
`~/Library/Application Support/unity.company name.product name`, "but uses the Editor path if that directory already
exists"; Linux `$XDG_CONFIG_HOME/unity3d/Company/ProjectName`, `~/.config` by default. Valve's roots include
`WinAppDataLocalLow`, `MacAppSupport` and `LinuxHome`; there is no XDG config root (only `LinuxXdgDataHome`). So
Auto-Cloud is:
- the Root OS set to `[Cross-Platform]`, as Valve requires when using Root Overrides;
- root `WinAppDataLocalLow`, subdirectory `Statecraft/Statecraft Israel/saves`, pattern `autosave.txt`;
- an override for macOS to `MacAppSupport`, `unity.Statecraft.Statecraft Israel/saves`;
- an override for Linux to `LinuxHome`, `.config/unity3d/Statecraft/Statecraft Israel/saves`. This assumes the default
  `XDG_CONFIG_HOME`; a player who moved it is not synced.
- Valve: the "Byte quota per user" and "Number of files allowed per user" must be set before it works. One small file;
  1 MB and 2 files leave room.

Log `Application.persistentDataPath` from each first Steam player before configuring (the macOS editor-path caveat).
Renaming `companyName` or `productName` after launch strands both local saves and the cloud mapping, so they stay frozen.

### 4.5 Store page, content and privacy

- Reuse the kit (`docs/steam/coming-soon-kit.md`, `docs/steam/store-page-draft.md`); screenshots and the trailer come from
  the Steam release build, recorded with its commit and SHA-256 (`docs/RELEASING.md:356-412`). No new art pipeline.
- **Content Survey and age ratings:** the draft answers (`docs/steam/coming-soon-kit.md:288`) are refreshed against the
  first Steam build (war, terror, the Holocaust, real people, the generative-AI section from `scripts/oss/ai_disclosure.py`).
  Germany needs the survey's generated rating (14 Oct spec section 5.1). Only the owner submits.
- **Privacy:** the game collects nothing and has no account; Steam's own data is Valve's. A short privacy statement goes in
  `docs/steam/` for the store page's support fields.

### 4.6 Testing

- **EditMode, no Steam:** the seam's no-op default, the achievement rules (from real content and synthetic states), the
  start rule (no app ID or `-golden` never calls Steam), the quit button's visibility, and the build helpers (the extra
  defines and the native-plugin include rule). The Steam assembly itself has no logic to
  test beyond calling Steamworks.NET.
- **pytest (in CI):** `release_build.py --steam` folder layout and refusals, the native-plugin strip and refusal for
  non-Steam archives, `steam_upload.py` VDF rendering, staging records and their refusals, and the workflow's shape: it
  runs on `workflow_dispatch` only, uses the `steam` environment, pins every action, restricts `branch` to `beta` and
  `nightly`, and names the two Steam secrets in its login step only. `scripts/oss/tests/test_github_config.py`'s rule that
  workflows use no secret (`test_ci_uses_no_secret_but_the_read_only_token`) gets one named exception for this file. No
  test runs steamcmd.
- **Player golden:** every Steam content folder passes the same `-golden` check as today's archives, macOS on both
  slices (arm64, and x86_64 under Rosetta, as `player-golden.yml` does for the archives); `-golden` never calls Steam.
- **Manual, with the Steam client (owner, on the `beta` branch):** a checklist in `docs/steam/test-checklist.md`.

## 5. Owner-only actions (never an agent)

Steamworks signup as an individual (O5); the Steam Direct fee (US$100 per app, not recouped by a free game; **re-check**
before paying); identity, tax and bank forms; creating the app as a free product (gives the app ID and depot IDs); a
dedicated builder account with only upload permissions and its Steam Guard; the CI setup of section 4.3.1 item 4 (the
`steam` environment and its two secrets, and re-provisioning them); App Admin configuration (depots and their OS, packages
including Dev Comp, the `beta` and `nightly` branches and their passwords, Auto-Cloud, achievements and their icons,
supported languages); the store page, the Content Survey and the review submissions; setting a build live on `default`;
the Apple notarisation for the macOS depot (#905) and "App Bundles Are Notarized"; one launch option per OS
(`Statecraft.exe`, `Statecraft.app`, `Statecraft.x86_64`: the player names in `tools/release_build.py:102-109`); the
Steam Cloud quota and file count; system requirements; the release calendar: the 30-day wait after the fee, at least two
weeks of a public Coming Soon page, and separate store-page and build reviews of 3-5 business days each, with 7 asked
for (14 Oct spec section 5.1). Store art and achievement icons must not be adapted from Tal Heuberger's CC BY-NC-ND
models (no derivatives); their source gets an `ASSETS.yaml` row like any asset.

## 6. Owner answers (1 November 2026, #2150)

The owner chose one option per question; the quoted text is the option label as chosen. The questions, options and
reasoning are in #2150; the answers are recorded in `docs/oss/DECISIONS.md:98-113`.

| Question | Owner's choice | What it means here |
| --- | --- | --- |
| Q1 When does Steam code start? | "Inert repo work now (Recommended)" | Plan Tasks 1-9 may start now; builds and uploads wait for the MVP and #905 |
| Q2 How does the repository handle Valve's SDK? | "Never commit; UPM wrapper (Recommended)" | Section 4.2 |
| Q3 One desktop build or a Steam flavour? | "Separate --steam build (Recommended)" | Section 4.3 |
| Q4 Which OSes on the first Steam release? | "Win + Mac + Linux (Recommended)" | The first release waits for macOS notarisation (#905) |
| Q5 Achievements? | "Five (Recommended)" | Section 4.4 |
| Q6 Steam Cloud? | "Auto-Cloud autosave (Recommended)" | Section 4.4 |
| Q7 Which builds go to Steam? | "Also nightlies" | Release tags to `beta`, every published nightly to `nightly` (section 4.3) |
| Q8 Who uploads, from where? | "GitHub Actions" | Section 4.3.1; secrets created only by the owner |
| Q9 Non-commercial licences on Steam | "Treat free as non-commercial" | No written confirmations are sought before the first upload |
| Q10 Steam Deck and controllers? | "No claim + one smoke test (Recommended)" | Section 4.4 |

## 7. Risks

| Risk | Mitigation |
| --- | --- |
| The `STEAM` define leaks into a committed `ProjectSettings.asset` | It is passed only as `extraScriptingDefines`, which never writes the settings; `release_build.py` already restores `unity/ProjectSettings` after each run (`tools/release_build.py:24-26`); a pytest in CI reads the committed `ProjectSettings.asset` and fails on any `STEAM` define |
| `SteamAPI.Init` fails on a player's machine | Fail soft to the no-op services; the game never depends on Steam |
| Valve's library ends up in a GitHub or itch.io archive | The strip-and-refuse step in `release_build.py`, tested |
| A credential reaches the repository or an agent | No password parameter anywhere; the only credentials are the two environment secrets, which the owner creates and only the upload step reads |
| The Steam secrets are used by a run the owner did not intend | `workflow_dispatch` only (write access required); the `steam` environment releases its secrets only to `main`; never on pull requests or forks; the builder account can only upload, and only `beta` and `nightly` can be set live |
| The CI login stops working (token expired, machine rejected) | The job fails at login without prompting; the owner re-provisions the secret (4.3.1 item 4); the fallback is a new owner decision |
| Valve's library is exposed through the hand-over | A draft release that is never published, visible only to people with write access, and deleted after a successful upload |
| The nightly's Steam stage breaks the web nightly | It runs after the web publish and only logs `steam=failed` |
| A cloud save from a newer `nightly` or `beta` build is opened by an older `default` build | Today's loader already handles it: a content-hash change warns (`AGENTS.md:98`), and an unreadable autosave is set aside as `autosave.bad.txt` (`SaveStore.cs:54-65`) and a new game starts; the checklist covers it once |
| A cached builder login on the owner's Mac | Provisioned in a temporary steamcmd folder that is deleted afterwards (4.3.1 item 4) |
| Shift+Tab opens the overlay instead of moving focus | Documented; players can change the overlay key in Steam |
| Hebrew has no Steamworks API code | Listed as a game-support-only store language; achievements English only; no Steam-language logic in the game |
| A rename moves the save folder | `companyName` and `productName` frozen |
| Steamworks.NET 2025.165.0 is three days old on 1 Nov | Pin the exact commit; the first Steam build is weeks away, so any fix release can replace it then |
| Q9: the free Steam build is treated as non-commercial use of the CC BY-NC-ND models and the "in this repo" ElevenLabs permission | The owner's decision ("Treat free as non-commercial"), recorded in `docs/oss/DECISIONS.md:112`; not legal advice. The same reading already covers itch.io. Valve's distribution agreement also covers store and marketing use, so no store art or icon is adapted from the ND models. Code: contributions come in under MIT to the maintainer (`docs/oss/DECISIONS.md:19`), so the owner can grant Valve what the agreement needs |
