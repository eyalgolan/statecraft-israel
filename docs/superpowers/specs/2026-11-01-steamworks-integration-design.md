# Steamworks integration: design (owner request, 1 November 2026)

- **Date:** 1 November 2026. **Status: PROPOSED.** The open choices are owner questions (section 6), each with a
  recommendation. Nothing here has been bought, signed up for, uploaded or configured on Steamworks.
- **Request (owner, verbatim):** "in parralel to everything else, I've downloaded the steamworks sdk as we will need to
  upload the game there in the future. therefore in parralel to everything else create a plan using the superpowers for
  the steamworks integration work and create issues for this."
- **Builds on:** `docs/superpowers/specs/2026-10-14-desktop-builds-and-release-pipeline.md` (the desktop plan, #482) and
  the owner's answers in `docs/oss/DECISIONS.md:43-60`. This document only covers what is new or changed since then; it
  does not repeat that spec's research. Read against `origin/main` at `7e6957a59` (1 November 2026).
- **Plan:** `docs/superpowers/plans/2026-11-01-steamworks-integration.md`. **Tracking:** the Steam epic issue (linked from
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
  asks of a store goes through it. Pure logic that decides *what* to report (which achievements are earned, which UI
  language a Steam language means) lives here too, so EditMode and the dotnet compile check test it with no Steam.
- **`Statecraft.Steam` is a separate assembly** in `unity/Assets/Steam/` (outside `Assets/Scripts/`, so
  `scripts/unity_compile/Game.csproj:9` never compiles it), with `"defineConstraints": ["STEAM"]` and Standalone plus Editor
  platforms. Without the `STEAM` define Unity does not compile it at all, so the WebGL build, itch.io, the nightly and every
  contributor's editor are unchanged. With it, a `RuntimeInitializeOnLoadMethod` bootstrap starts Steam, sets
  `PlatformServices.Current`, pumps `SteamAPI.RunCallbacks()` each frame and calls `SteamAPI.Shutdown()` on quit.
- **The `STEAM` define is set only by the build**, never committed: `Build.Mac`, `Build.LinuxMono` and `Build.WindowsMono`
  take a `-steam` flag that adds it for the build and restores the committed (empty) list after, the way `StandaloneMono`
  restores the backend (`Build.cs:279-289`).
- **The sim is untouched.** Steam reads the game state; it never feeds the sim. No golden hash moves.

### 4.2 The SDK and the licence (recommended answer to Q2)

- Never commit the SDK, its headers, its tools or any Valve binary. The scratch unzip lives outside the repository.
- Steamworks.NET comes in through UPM as a git dependency pinned to the tag *and* checked by the commit in
  `unity/Packages/packages-lock.json`, like `com.ivanmurzak.unity.mcp` (`unity/Packages/manifest.json:3`). UPM downloads it
  into `unity/Library/PackageCache` (ignored, `.gitignore:46`), so Valve's redistributable reaches a build without ever
  sitting in this repository.
- Only Steam builds may carry `steam_api*`: the release script deletes the Steamworks.NET native plugins from a non-Steam
  player folder before archiving and refuses an archive that still lists one (GitHub Releases and itch.io stay free of
  Valve files).
- `THIRD-PARTY-NOTICES.md` "Builds" (`THIRD-PARTY-NOTICES.md:44-52`) gains Steamworks.NET (MIT, its copyright line) and a
  line that Steam builds carry Valve's `steam_api` redistributable under the Steamworks SDK Access Agreement.
  `LICENSING.md` needs no row: nothing Valve's is in the tree.

### 4.3 Steam builds and channels

- A Steam build is a separate flavour: `uv run tools/release_build.py --steam ...` builds with `-steam` and writes plain
  content folders `steam/<platform>/` for ContentBuilder instead of archives. GitHub and itch.io keep today's archives.
- With `STEAM`, the bootstrap calls `SteamAPI.RestartAppIfNecessary(appId)` first, so a Steam build started outside the
  Steam client relaunches through Steam; a failed `SteamAPI.Init` logs one line and the game runs on with the no-op
  services (a Steam build must never refuse to start because Steam is slow).
- The app ID and depot IDs are public numbers, committed in `steam/steam.json` once the owner has them; `null` until
  then, and every Steam step refuses while they are `null`. `steam_appid.txt` is a development aid only and is never
  shipped (the release script refuses a Steam folder containing it).
- **Depots:** one per OS (Windows x64, macOS Universal, Linux x64), each with its OS set in App Admin (owner).
- **Branches (recommended answer to Q7):** a release tag `vX.Y.Z` is uploaded to a password-protected `beta` branch with
  `SetLive`; the owner tests it and sets it live on `default` by hand in App Admin (Valve does not allow scripting the
  default branch; 14 Oct spec section 5.4). Nightlies are not uploaded.
- **Upload (recommended answer to Q8):** `tools/steam_upload.py` renders the VDF scripts from templates in `steam/` and the
  IDs, then, only when the owner runs it with `--upload` from an interactive terminal, runs the owner's own `steamcmd` with
  `+login <builder account> +run_app_build <vdf> +quit`. It never takes, stores or prints a password; steamcmd asks for it
  and for Steam Guard itself. It refuses without a TTY, so no agent or CI job can start an upload.

### 4.4 Features

| Feature | Design | Owner question |
| --- | --- | --- |
| Init and shutdown | Section 4.1; guarded; WebGL and non-Steam builds unaffected | Q3 |
| Languages | Store: English (interface, subtitles, full audio) and Hebrew (interface) if Steamworks lists Hebrew (**re-check**; if it does not, the store lists English and the description says the game has a Hebrew interface). In game: on the first start of a Steam build with no stored `lang`, `SteamApps.GetCurrentGameLanguage()` picks the UI language (`"hebrew"` to `he`, anything else to `en`); a stored choice or `-lang` always wins | none |
| Steam Cloud | Auto-Cloud (App Admin, no code) for `saves/autosave.txt` only; settings stay per machine (quality and UI scale depend on the screen) | Q6 |
| Achievements | Five, read from the game state after each change: one per objective (the four ids above) and one for reaching the last turn (`mvp_ended`). Unlocked through `SteamUserStats.SetAchievement` and `StoreStats`; never from the sim | Q5 |
| Overlay | Works with no code. Shift+Tab is the overlay's default and the game's focus-back key: documented on the store page and in the test checklist; no rebinding | none |
| Steam Deck and controllers | No controller support and no Deck claim at launch; one smoke test of the Linux build on a Deck or under Proton before release | Q10 |
| Rich presence, stats, leaderboards, workshop, DRM, inventory, microtransactions | Not planned (YAGNI; microtransactions and paid items are ruled out) | none |
| Quit | A desktop "Quit" button (Settings and start screen), hidden on WebGL; Steam review and Deck need a way out of a full-screen game (14 Oct spec section 2.5) | none |

**Steam Cloud roots (re-check on the Steam Cloud page before configuring).** Unity's `persistentDataPath`:
Windows `%userprofile%\AppData\LocalLow\Statecraft\Statecraft Israel`, macOS
`~/Library/Application Support/unity.Statecraft.Statecraft Israel` (14 Oct spec section 2.6), Linux
`~/.config/unity3d/Statecraft/Statecraft Israel` (Unity Scripting API, **re-check**). Auto-Cloud: root
`WinAppDataLocalLow`, subdirectory `Statecraft/Statecraft Israel/saves`, pattern `autosave.txt`, with root overrides for
macOS (`MacAppSupport`, `unity.Statecraft.Statecraft Israel/saves`) and Linux (the root Valve lists for `~/.config`;
**re-check** its exact name). Renaming `companyName` or `productName` after launch strands both local saves and the cloud
mapping, so they stay frozen.

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
  language mapping, the quit button's visibility, and the build define helper. The Steam assembly itself has no logic to
  test beyond calling Steamworks.NET.
- **pytest (in CI):** `release_build.py --steam` folder layout and refusals, the native-plugin strip and refusal for
  non-Steam archives, `steam_upload.py` VDF rendering, the `null`-ID refusal and the no-TTY refusal. No test runs steamcmd.
- **Player golden:** every Steam content folder passes the same `-golden` check as today's archives; Steam init failing
  (no client on the runner) must not change it.
- **Manual, with the Steam client (owner, on a `beta` branch):** a checklist in `docs/steam/test-checklist.md`.

## 5. Owner-only actions (never an agent)

Steamworks signup as an individual (O5); the Steam Direct fee (US$100 per app, not recouped by a free game; **re-check**
before paying); identity, tax and bank forms; creating the app as a free product (gives the app ID and depot IDs); a
dedicated builder account with only upload permissions and its Steam Guard; App Admin configuration (depots and their OS,
packages including Dev Comp, the `beta` branch and its password, Auto-Cloud, achievements and their icons, supported
languages); the store page, the Content Survey and the review submissions; setting a build live on `default`; the Apple
notarisation for the macOS depot (#905); asking licensors for written confirmations (Q9).

## 6. Owner questions

Each has options and a recommendation; the plan assumes the recommendation until the owner answers, and every task that
depends on an answer names it.

- **Q1 When does Steam code start?** (a) Now, in parallel: only the inert repository work (the seam, the `STEAM`-gated
  assembly, the scripts, the docs), none of which changes the web build; builds and uploads still wait for the MVP and
  #905. (b) Everything after the MVP, as `docs/mvp-checklist.md:178` says today. **Recommended: (a)**, which matches "in
  parallel to everything else".
- **Q2 How does the repository handle Valve's SDK?** (a) Section 4.2: never commit the SDK or a Valve binary; Steamworks.NET
  by UPM pinned to `2025.165.0` and its commit; Valve's redistributable only in Steam builds. (b) Vendor Steamworks.NET
  into `unity/Packages/` (commits Valve's binaries to a public repository; the SDK agreement does not clearly allow it).
  (c) No wrapper: copy the redistributable from the owner's SDK zip at build time and write our own bindings (more code
  to own). **Recommended: (a).**
- **Q3 One desktop build or a Steam flavour?** (a) A separate Steam flavour (`--steam`), as in 4.3; GitHub and itch.io
  archives carry no Steam code or Valve file. (b) One build everywhere with Steam always compiled in and failing soft off
  Steam (simpler, but every GitHub archive ships Valve's library and cannot use `RestartAppIfNecessary`).
  **Recommended: (a).**
- **Q4 Which OSes on the first Steam release?** (a) Windows, macOS and Linux together (O1), so the release waits for macOS
  notarisation (#905). (b) Windows and Linux first, macOS when notarised. **Recommended: (a)**, falling back to (b) only if
  notarisation is the last thing blocking a ready release. Valve requires every listed OS to launch.
- **Q5 Achievements?** (a) None at launch (O10 named no Steam-only feature). (b) Five: the four objectives and finishing
  the campaign. (c) More (for example early completions, no defeat). **Recommended: (b)**: small, read from existing
  state, no sim change; (c) can follow once the GDD's balance settles. Each needs a name, a description in both languages
  and two 256x256 icons (owner approves the art).
- **Q6 Steam Cloud?** (a) None. (b) Auto-Cloud for the autosave only (no code). (c) Auto-Cloud for the autosave and
  settings. (d) The `ISteamRemoteStorage` API. **Recommended: (b).**
- **Q7 Which builds go to Steam?** (a) Release tags to a password `beta` branch, the owner sets `default` live by hand;
  nightlies are not uploaded. (b) Also every nightly to a `nightly` branch (needs the builder login cached on the Mac for
  unattended runs). **Recommended: (a)**; revisit (b) after launch.
- **Q8 Who uploads, from where?** (a) The owner, on their Mac, with `tools/steam_upload.py --upload` (builds are made there
  anyway, O7). (b) A GitHub Actions job with owner-created secrets (the builder login would sit on GitHub's machines).
  **Recommended: (a).**
- **Q9 Non-commercial licences on Steam.** Steam is a commercial storefront even for a free game. Tal Heuberger's models
  are CC BY-NC-ND 4.0 (`LICENSING.md:16`, 196 rows in `ASSETS.yaml`); the ElevenLabs permission covers "publishing these
  files in this repo" (`docs/oss/DECISIONS.md:88`); the 1948 recording's English terms say "except commercial" (14 Oct
  spec section 5.3). (a) The owner asks Tal and checks the ElevenLabs permission for written confirmation that the free
  Steam build is covered, recorded in `docs/oss/DECISIONS.md` before the first upload. (b) Treat a free Steam listing as
  non-commercial use without asking. **Recommended: (a)** (not legal advice).
- **Q10 Steam Deck and controllers?** (a) No claim and no controller work at launch; one Deck or Proton smoke test.
  (b) Controller support and a Deck Verified review before launch (a separate project). **Recommended: (a).**

## 7. Risks

| Risk | Mitigation |
| --- | --- |
| The `STEAM` define leaks into a committed `ProjectSettings.asset` | The build restores it in `finally`; `release_build.py` already restores `unity/ProjectSettings` after each run (`tools/release_build.py:24-26`); an EditMode test pins `scriptingDefineSymbols` empty |
| `SteamAPI.Init` fails on a player's machine | Fail soft to the no-op services; the game never depends on Steam |
| Valve's library ends up in a GitHub or itch.io archive | The strip-and-refuse step in `release_build.py`, tested |
| A credential reaches the repository, CI or an agent | No password parameter anywhere; `--upload` needs a TTY; steamcmd's own prompt and Steam Guard |
| Shift+Tab opens the overlay instead of moving focus | Documented; players can change the overlay key in Steam |
| Hebrew is not a Steam store language | The store lists English and says the interface is also in Hebrew |
| A rename moves the save folder | `companyName` and `productName` frozen |
| Steamworks.NET 2025.165.0 is three days old on 1 Nov | Pin the exact commit; the first Steam build is weeks away, so any fix release can replace it then |
