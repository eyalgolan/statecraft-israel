# Desktop builds and the release pipeline (issue #54)

- **Date:** 14 October 2026.
- **Status: PROPOSED.** Every purchase, account, signature and store action below is the owner's, and none has been
  taken. Section 9 lists the decisions as questions. On 14 October 2026 the owner answered all of them except O6 (the
  bundle ID), O9 and O11; the answers are under each question and in `docs/oss/DECISIONS.md`. Sections 1 to 8 are the
  plan as written before those answers. Where an answer differs from the plan's recommendation, the answer wins.
- **Read against:** `origin/main` at `e77f402` (14 October 2026). Claims about the repository cite the file. PRs #465 (the
  1948 recording) and #466 (outcome clips) were open on that date and are described from their PR text and diff.
- **Scope:** a plan only. No code, settings, content or golden changes. Unity was not run for this document.
- **Issue:** [#54](https://github.com/eyalgolan/statecraft-israel/issues/54) (roadmap tracker). Related: #424 (Steam Coming
  Soon page), #45 (Steam readiness and trailer), #39 (audio), #40 (settings), #44 (low-end).

**How to read it.** Section 1 is what the repository does today. Sections 2 to 6 cover the builds, signing, Steam and CI,
each separating "today" from "proposed". Section 7 is the phased plan, section 8 the risks, section 9 the owner decisions,
section 10 the facts that could not be verified, and section 11 the sources with the date each was read.

**Sources.** Every price, term and limit below comes from an official page read on 14 October 2026 (section 11). Where a
page could not be read, or did not say what was needed, the fact is marked **unverified** and listed in section 10.
Prices are as published on that date and can change; re-check them before paying anything.

---

## Summary for the owner

1. **The smallest useful step costs nothing.** `Build.Mac` already makes a macOS IL2CPP player
   (`unity/Assets/Editor/Build.cs`), and it passed the golden check at M3 (`docs/determinism.md`). An unsigned Mac build for
   a few named testers needs a packaging script, a desktop quit control and a size and boot record, not a purchase. Testers
   open it once through System Settings > Privacy & Security > "Open Anyway" (Apple's Gatekeeper page).
2. **Windows needs either a Windows PC or the Mono backend.** Unity cannot build an IL2CPP Windows player on a Mac ("to build
   an IL2CPP Player for a particular target platform you must build from an Editor running on the same platform", Unity IL2CPP
   overview). A Mono Windows player can be built on the Mac; the sim must already match bit for bit on Mono
   (`docs/determinism.md`), but no Windows player has ever been checked.
3. **Mac on Steam needs the Apple Developer Program.** Valve's Platforms page says Steam requires new macOS applications to be
   64-bit and notarized by Apple. Notarization requires a Developer ID certificate, which comes with the program (99 USD per
   membership year). A Windows-only Steam release needs no Apple account.
4. **Windows signing is optional for a first release and does not remove SmartScreen warnings by itself.** Microsoft's own
   pages say a new signed file still shows an "unrecognized" prompt until reputation builds, that EV certificates no longer
   bypass SmartScreen, and that Artifact Signing (formerly Trusted Signing, 9.99 USD a month) is open to individual developers
   only in the US and Canada.
5. **Steam needs no Steamworks code for a first release.** Valve: "The Steamworks SDK is only required to upload your content
   to Steam, everything else provided through the SDK is optional." Cloud saves can come later through Steam Auto-Cloud,
   which needs no code. Achievements need a wrapper; both MIT wrappers are maintained.
6. **Recommendation on CI: keep Unity builds local.** Unity's documentation says a Personal licence can be activated only
   through the Unity Hub, not from the command line; the repository's own reasons for keeping Unity out of CI still hold
   (`CONTRIBUTING.md`, decision D9). Build on the owner's Mac under `tools/machine_lock.py`, and record commit, Unity version,
   date and SHA-256 per artifact.
7. **The name (D8) comes before anything public on Steam.** It also sets the save folder on desktop (Unity derives
   `persistentDataPath` from the company and product names), so renaming after players have saves would strand them.

---

## 1. What the repository does today

| Area | Today | Where |
| --- | --- | --- |
| Shipped builds | WebGL only, zipped by `scripts/oss/package_web.sh` for GitHub Releases and itch.io | `docs/RELEASING.md` steps 5-7 |
| Mac build | `Build.Mac` builds `BuildTarget.StandaloneOSX` to `Builds/Mac/Statecraft.app`, honours `-shareable`, writes `event-photos.json` beside the app, and takes `-ffp-contract-off` to stop Apple clang fusing multiply-adds | `unity/Assets/Editor/Build.cs`; command in `docs/dev-setup.md` |
| Windows build | None. No `Build.Windows`, and `docs/dev-setup.md` installs only the `webgl` and `mac-il2cpp` modules | `unity/Assets/Editor/Build.cs`, `docs/dev-setup.md` |
| Mac architecture | Not set by the build script. Unity reads it from `OSXStandalone.UserBuildSettings.architecture`, a per-user setting, so the output depends on the machine's editor state | `Build.Mac`; Unity Scripting API |
| Scripting backend | IL2CPP for Standalone and WebGL (`ProjectSetup.Apply`; `scriptingBackend: Standalone: 1`) | `unity/Assets/Editor/ProjectSetup.cs`, `unity/ProjectSettings/ProjectSettings.asset` |
| Graphics APIs | No list for Standalone in `m_BuildTargetGraphicsAPIs`, so the Standalone player uses Unity's automatic choice | `unity/ProjectSettings/ProjectSettings.asset` |
| Quality | Two URP tiers: `Mobile` (web) and `PC` (desktop: four soft shadow cascades, SSAO off). Standalone defaults to `PC` (`m_PerPlatformDefaultQuality: Standalone: 1`). The frame-time fallback (`QualityFallback.Wanted`) runs on WebGL only | `unity/Assets/Editor/QualityTiers.cs`, `unity/ProjectSettings/QualitySettings.asset`, `unity/Assets/Scripts/Map/QualityFallback.cs` |
| Input | Input System package 1.20.0 (`activeInputHandler: 1`); mouse and keyboard only | `unity/Packages/manifest.json`, `unity/Assets/Scripts/Map/CameraController.cs` |
| Window | Full-screen window by default (`fullscreenMode: 1`), 1024x768 default size, not resizable, `runInBackground: 0`. No quit control and no display settings in the UI | `ProjectSettings.asset`; `unity/Assets/Scripts/UI/` |
| Identity | `companyName: Statecraft`, `productName: Statecraft Israel`, `bundleVersion: 0.1.0`, and the Standalone bundle identifier still the URP template's `com.Unity-Technologies.com.unity.template.urp-blank` | `ProjectSettings.asset` |
| Save path | WebGL: the fixed `/idbfs/statecraft-saves`. Desktop: `Application.persistentDataPath/saves/autosave.txt` | `unity/Assets/Scripts/SaveStore.cs` |
| Content loading | WebGL fetches `StreamingAssets` over HTTP; desktop reads `StreamingAssets/content` and `StreamingAssets/map` from disk (`GameInputs.FromDisk`) | `unity/Assets/Scripts/Boot/GameBoot.cs`, `unity/Assets/Scripts/Paths.cs` |
| Video | 11 silent decision-card clips in `StreamingAssets/video` (9.5 MB, H.264 960x540), played by `CardVideo` from `Application.streamingAssetsPath + "/video/" + file` | `unity/Assets/Scripts/UI/CardVideo.cs`, `ASSETS.yaml` |
| Audio (PR #465, open) | One WAV in `StreamingAssets/audio`, fetched with `UnityWebRequestMultimedia`; the code prefixes `file://` when the path has no scheme, so it is written for desktop as well | `gh pr diff 465` |
| Outcome clips (PR #466, open) | Five more clips through the same `CardVideo.Url` | `gh pr diff 466` |
| Release gate | `scripts/oss/release_gate.sh` on the merged `main`, then the build with `-shareable`, then `package_web.sh` (notices, the sensitivity gate in `tools/package_release.py`, SHA-256) | `docs/RELEASING.md` |
| Build record | The zip's SHA-256 is printed by `package_web.sh` and pasted into the GitHub Release notes; the tag fixes the commit; screenshots must record "the build commit and zip SHA-256 with the capture date" | `docs/RELEASING.md` steps 5-6 and the store-page checklist |
| Unity runs | Every Unity run on the owner's machine goes through `uv run tools/machine_lock.py --shared --wait-unity -- ...` | `AGENTS.md`, `tools/machine_lock.py` |
| CI | `Sim (dotnet test)`, `Tools (pytest)`, `Repo policy`, `Changelog`. No Unity job; EditMode tests are a local gate (decision D9) | `.github/workflows/ci.yml`, `CONTRIBUTING.md` |

**Determinism, as recorded.** The goldens were verified on .NET, the editor's Mono, a macOS IL2CPP player (Apple clang,
`-ffp-contract=off` not needed) and the WebGL player. The last Mac player check (M3, `a099655`) printed the old 1952 hash
`73590e32`; the issue #2 content change moved it to `62f8ec61`, and `docs/determinism.md` says the Mac revalidation is
"still needed". No Windows player, Mono or IL2CPP, has been checked.

**WebGL numbers the repository records** (the baseline for section 2.9):

| Measure | Value | Where |
| --- | --- | --- |
| Uncompressed WebGL build | 77.1 MB on disk (visual v1, before the clips) | `docs/visual-v1.md` |
| Brotli build folder, what a browser downloads | 23,954,695 bytes after issue #44 (`fb31ce1` era) | `docs/m3-release.md` |
| Boot, `--cpu 1` | `map_build_ms` about 430-510, `first_frame_ms` about 3,450-3,850 | `docs/visual-v1.md`, `docs/m3-release.md` |
| Boot budget | Task 1 baseline plus max(300 ms, 50%): 743 / 5,321 ms at `--cpu 1`, 2,720 / 10,445 ms at `--cpu 4` | `docs/visual-v1.md` |
| Mac player frame time | 1.02 ms (gameplay03) and 2.72 ms (diorama02), all features on | `docs/visual-v1.md` "Final macOS player" |

No Mac player size or Mac boot time is recorded.

---

## 2. Mac and Windows builds

### 2.1 Targets and architecture

**Proposed.**
- **macOS: Universal (Apple silicon + Intel 64-bit).** Unity's macOS requirements page notes that Apple is deprecating
  Rosetta and recommends building "for Apple silicon or create a Universal binary instead of an Intel 64-bit build". A
  Universal build covers both and avoids a second depot. `Build.Mac` should set
  `OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64ARM64` itself, so the result no longer depends on the
  per-user editor setting, and log the architecture it built.
- **Windows: x64 only.** Unity also lists 32-bit and Arm64. x64 is the one to test first; Arm64 can follow if testers ask.
- **Linux: not proposed.** Steam Deck runs the Windows build through Proton (section 5.7). A native Linux player is a later
  question.

Minimum OS: Unity 6.3 supports macOS Monterey 12 and later and Windows 10 21H1 (build 19043) or newer (Unity requirements
pages). These are Unity's floors; the store page should list what was actually tested.

### 2.2 Scripting backend and determinism

The sim must give the same state bit for bit on .NET, Mono and IL2CPP (`docs/determinism.md`). That rule makes either
backend acceptable in principle; what matters is that each shipped player passes `-golden`.

| Platform | Backend | Build machine | Determinism check |
| --- | --- | --- | --- |
| macOS | IL2CPP (as today) | The owner's Mac | `"<app>/Contents/MacOS/Statecraft Israel" -batchmode -nographics -golden -logFile <absolute path>`; must print both `GOLDEN OK` lines with the current hashes and exit 0. Re-run it once on an Intel Mac or under Rosetta, since a Universal app has two native slices |
| Windows, option A | Mono | The owner's Mac (needs the Windows Build Support (Mono) module; the module id was not checked, **unverified**) | The same flags on a Windows machine. The editor's Mono already passes the goldens; a Windows Mono player has not been run |
| Windows, option B | IL2CPP | A Windows PC with Visual Studio 2019 C++ tools or later and Windows SDK 10.0.19041.0+ (Unity Windows requirements) | The same. The C++ compiler differs from Apple clang, so a golden failure would be the first sign of a floating-point difference; `Build.Mac`'s `-ffp-contract-off` switch has no Windows counterpart yet |

**Proposed:** start Windows with option A (Mono, built on the Mac), because it needs no new machine or licence, and decide
on IL2CPP once a Windows PC or a Windows tester is available (decision O8). The golden check is the gate either way: a
Windows player that does not print both `GOLDEN OK` lines does not ship.

Running a built player needs no Unity licence, so the `-golden` check of a player could later run on a GitHub-hosted Windows
or macOS runner even while builds stay local (section 6.3). GitHub runners have no GPU listed in their specification, so
only the `-batchmode -nographics` checks fit there.

### 2.3 Graphics APIs

Today the Standalone player uses Unity's automatic graphics API (no Standalone row in `m_BuildTargetGraphicsAPIs`). Unity's
documentation: with "Auto Graphics API" on, "the Player build includes a set of built-in graphics APIs and uses the
appropriate one at runtime". macOS players use Metal (Unity macOS requirements: "Unity supported macOS devices support the
Metal graphics API"). Windows devices support "DX10, DX11, DX12, and Vulkan capable GPUs" (Unity Windows requirements).

**Proposed.**
- **macOS:** keep Auto (Metal). The visual v1 profiles already ran on the Mac player.
- **Windows:** keep Auto for the first tester build, and record which API each tester's player picked (the player log
  names it). If a shader breaks on one API, pin the list (for example Direct3D 11 first) rather than chase it during a
  release. The custom shaders (`StatecraftTerrain.shadergraph`, the kit shader, `TiltShift`) have only been seen on Metal
  and WebGL 2, so Windows is the first time they meet Direct3D or Vulkan.
- Read `Player.log` for `Shader error` on every platform, as the visual v1 checks already do for builds.

### 2.4 Quality tiers

The desktop player runs the `PC` tier: four soft cascades, SSAO off, bloom off (`QualityTiers.Apply`,
`docs/visual-v1.md` "Tier decisions"). The WebGL frame-time fallback, which lowers render scale and drops the tilt-shift
when frames are slow, is WebGL-only (`QualityFallback.Wanted`). A desktop player on a weak integrated GPU therefore has no
automatic way down.

**Proposed:** for testers, ship `PC` as is and ask for the `-vprofile` summary lines on their machines. Before a public
release, either enable the fallback on desktop or add a quality choice to the settings screen (#40, #44). This is a decision
for those issues, not a release blocker for testers.

### 2.5 Input and window

- **Input** is mouse and keyboard through the Input System; the same code runs on desktop. Nothing changes for a desktop
  tester build.
- **Quit.** The web build never needed one. On macOS Cmd+Q works through the system menu; a Windows full-screen window has
  only Alt+F4. **Proposed:** a "Quit" item on the start screen and in the in-game menu, shown only when
  `Application.platform` is a desktop player, calling `Application.Quit()`.
- **Window.** The player opens as a full-screen window at the desktop resolution and is not resizable. **Proposed:** keep that
  default, and add windowed/full-screen and resolution to the settings screen (#40).
- **Background.** `runInBackground` is off, so the player pauses when it loses focus. That is right for players; the profile
  scripts already work around it (`docs/dev-setup.md`).

### 2.6 Save path

Today `SaveStore.DirectoryFor` uses `Application.persistentDataPath/saves` on desktop. Unity's Scripting API gives the
folders:
- **Windows player:** `%userprofile%\AppData\LocalLow\<companyname>\<productname>`, so today
  `...\LocalLow\Statecraft\Statecraft Israel\saves\autosave.txt`.
- **macOS player:** `~/Library/Application Support/unity.<company name>.<product name>`, so today
  `~/Library/Application Support/unity.Statecraft.Statecraft Israel/saves/autosave.txt`.

Consequences:
1. **The name decides the folder.** If D8 changes the product or company name after testers have saves, the new build will
   not see them. **Proposed:** settle D8, or at least freeze `companyName` and `productName` for desktop builds, before the
   first tester build, and add a one-time migration only if a rename is unavoidable.
2. **Steam Auto-Cloud can sync this folder with no code** (section 5.3). Valve's root list includes `WinAppDataLocalLow`
   and `MacAppSupport`, and cross-platform sync needs one root plus root overrides for the other OS (Steam Cloud page).
3. The save is small text (the seed and the command log), and `WriteAutosave` already writes a `.tmp` and then replaces, so
   a sync never sees half a file unless the game is killed mid-write.

### 2.7 StreamingAssets: video and audio (#465, #466)

Unity copies `StreamingAssets` "without modification into the Player output", and on desktop
`Application.streamingAssetsPath` "is a directory accessible using regular file system APIs"; only Android and the Web
return a URL (Unity StreamingAssets page). So:

- **Content and the heightmap** already load from disk on desktop (`GameInputs.FromDisk`). No change.
- **Video.** `CardVideo` hands `VideoPlayer.url` a plain path on desktop. The clips are H.264 MP4. Unity points to each
  platform's own decoder list for codec support (Windows: Microsoft's "Supported Media Formats"), and the WebGL page lists
  `.mp4` among supported formats. The clips have only been played on WebGL and in the editor. **Proposed check per
  platform:** open a decision card with the Illustration switch on and look for `VIDEO playing` (not `VIDEO failed`) in
  `Player.log`. If a Windows machine cannot decode them, the card falls back to its photograph after 8 s by design
  (`PrepareTimeoutMs`), so a failure degrades, it does not crash.
- **Audio (#465).** The recording is fetched with `UnityWebRequestMultimedia` from `"file://" + path` on desktop. Two
  things to test on the first desktop builds, not assumed broken: a macOS path with a space (`Statecraft Israel.app`) and a
  Windows path with a drive letter (`file://C:/...` versus `file:///C:/...`). Both are visible in the log as
  `RECORDING playing` or an error.
- **Size.** The clips (9.5 MB, more with #466) and the WAV (2.7 MB per #465) are copied uncompressed into every desktop
  build; that is small next to the rest.
- **Disclosure.** The clips are AI-generated (`ASSETS.yaml`, decision D-V) and ship in every desktop build, so they belong
  in the Steam AI disclosure (section 5.5).

### 2.8 Identity fields

Signing, notarization and Steam all key on the application's identity:
- **Bundle identifier.** The Standalone value is still the URP template's. Apple registers an app by its bundle ID (Unity's
  macOS code signing page), and the template ID is not the project's. **Proposed:** a reverse-DNS ID the owner controls,
  chosen with D8 (decision O6). Changing it does not move the Mac save folder (Unity builds that path from the company and
  product names), but it should still be fixed once.
- **Version.** `bundleVersion` and `SimInfo.Version` already agree (`test_versions_agree`). Steam has its own build IDs;
  the release record (section 6.5) maps one to the other.

### 2.9 Proposed build-size and boot targets

Nothing is measured yet for desktop size or desktop boot, so these are proposals to replace with the first measured build.

**Size.**
- **Measure** the folder or `.app` on disk, and the zip, for each platform, and print them in the build log the way
  `Build.Run` already prints "MB on disk".
- **Compression.** `Build.Run` passes `BuildOptions.None`. Unity: "On Windows, Mac, Linux Standalone ... there's no default
  compression", and LZ4HC "produces better results for release builds". **Proposed:** LZ4HC for release builds, after
  checking that it does not slow the boot past the target below.
- **Provisional ceilings:** Windows x64 zip at most 150 MB; macOS Universal zip at most 200 MB (two native slices). The
  reasoning: the WebGL build is 77.1 MB uncompressed with one copy of the code as wasm; a desktop build carries the same data
  plus Unity's native player, and a Universal app carries the native code twice. The ceilings are set loose enough that the
  first build should fit and tight enough that an accidental duplicate texture set or an unstripped debug build would not.
  After the first measured build, **proposed** rule: the baseline is that build, and a release that grows a platform by
  more than 10% must say why in its PR, like the boot budget rule.

**Boot.**
- **Measure** with the desktop players' existing `-bootlog` flag (`docs/dev-setup.md`), three cold starts, median, under
  `tools/machine_lock.py --exclusive --wait-unity`. `tools/headed.py boot` is browser-only and does not apply.
- **Provisional targets:** on the development Mac, `first_frame` no slower than the WebGL `--cpu 1` median (about 3.5 s).
  A desktop player skips the download and the wasm compile and reads files from disk, so it should be faster; if it is not,
  that is worth knowing. On a low-end Windows reference machine (to be named in #44), at most 10 s, the WebGL `--cpu 4`
  budget. Then the repository's existing rule: baseline plus max(300 ms, 50%).

---

## 3. Mac signing and notarization (OWNER purchase and decision)

Nothing in this section has been or will be done by an agent: no account, no enrolment, no certificate, no upload to Apple.

### 3.1 What players see without it

- Unity "adds a code signature to every macOS build it produces" (Unity macOS code signing page), but it is not a Developer
  ID signature, and the app is not notarized.
- Apple's Gatekeeper page: "By default, macOS Catalina and later also requires software to be notarized". For an app that
  is not notarized macOS shows an alert that it cannot check the app or verify the developer. A user who trusts the source
  can open System Settings > Privacy & Security, click "Open Anyway", then confirm; the app is then saved as an exception.
  If the user's setting allows only App Store apps, macOS will not open it.
- **For testers** this is workable with a short instruction sheet. **For the public** it reads as a warning about malware,
  and Apple's page tells users to "Check the App Store for an updated version or search for an alternative app". It is not
  a basis for a public Mac release.
- **On Steam it is not allowed.** Valve: "Starting October 14th, 2019 Steam will require all new macOS Applications to be
  64-bit and notarized by Apple." (Steamworks Platforms page.)

### 3.2 What the Apple Developer Program requires

- **Cost:** "The Apple Developer Program is 99 USD per membership year. Prices may vary by region and are listed in local
  currency during the enrollment process." Fee waivers exist for nonprofits, accredited educational institutions and
  government entities (Apple enrolment page); an individual game developer is not among them.
- **Enrolment as an individual:** an Apple Account with two-factor authentication, the legal age of majority, and the legal
  name on the account. **As an organisation:** a legal entity (no DBAs or trade names), a D-U-N-S Number, a work email on the
  organisation's domain and a public website (Apple enrolment page).
- **Certificate:** a "Developer ID Application" certificate. Apple's notarization page requires a Developer ID certificate
  ("Don't use a Mac Distribution, ad hoc, Apple Developer, or local development certificate").
- **Notarization requirements** (Apple, "Notarizing macOS software before distribution"): code-sign every executable;
  enable the Hardened Runtime; include a secure timestamp; do not include `com.apple.security.get-task-allow` set to true;
  link against the macOS 10.9 SDK or later. The notary service "usually takes less than an hour". Since 1 November 2023 the
  notary service no longer accepts `altool`; use `notarytool`.
- **Hardened Runtime** "disallow[s] certain less common capabilities, like just-in-time (JIT) compilation"; an app that needs
  one adds an entitlement for it, and "you add entitlements only to executables" (Apple, Hardened Runtime).
- **Entitlements for this project.** Unity's page gives a minimum set for a Unity app with the Hardened Runtime:
  `com.apple.security.cs.disable-library-validation` and `com.apple.security.cs.disable-executable-page-protection`.
  Valve's Platforms page asks Steam builds for `com.apple.security.cs.disable-library-validation` (to load the Steamworks
  and overlay libraries) and `com.apple.security.cs.allow-dyld-environment-variables` (for the overlay), and says Steam is
  not compatible with `com.apple.security.app-sandbox`. The final list is a test result, not a guess: sign, notarize, run
  the golden check and the Steam overlay, and drop any entitlement the app does not need.
- **Stapling.** `xcrun stapler staple "<App>.app"` attaches the ticket so the check also works offline. A ZIP cannot be
  stapled; staple the app, then zip it again (Apple, "Customizing the notarization workflow"; Unity notarization page).
- **Credentials.** `notarytool` takes an app-specific password (the Apple Account uses 2FA) or an App Store Connect API
  key, stored once with `xcrun notarytool store-credentials` (Apple). On this project they would live in the owner's
  login keychain, never in the repository or in an agent's context.

### 3.3 What a signed pipeline would add (outline, proposed)

After `Build.Mac` and the golden check, a local script would run, in order: write the entitlements file; `codesign
--timestamp --options runtime --entitlements ... --sign "Developer ID Application: ..."` (Unity's page warns that `--deep`
can mis-sign nested code, so sign nested binaries first); `ditto -c -k --keepParent` to zip; `xcrun notarytool submit
--wait`; `xcrun stapler staple`; zip again; SHA-256. Each step's output is kept in the build record. This waits for
decision O3.

---

## 4. Windows code signing (OWNER purchase and decision)

### 4.1 What players see without it

Microsoft's SmartScreen reputation page:
- **No signature:** "Windows protected your PC"; the user must choose "Run anyway". "Enterprise policy can prevent
  continuation entirely."
- **Valid OV or EV certificate:** still a warning ("app flagged as unrecognized until reputation accumulates"), but the
  verified publisher name is shown.
- "EV certificates no longer bypass SmartScreen." Reputation "can take several weeks and hundreds of clean installs from a
  wide audience". Signing every release with the same identity lets reputation carry over; unsigned files start from zero
  each version.
- On Windows 11, Smart App Control "will block execution of unsigned files unless the file has a positive reputation".

Whether a game installed by the Steam client meets SmartScreen at all was not found in Microsoft's or Valve's pages read
(**unverified**). Valve's Platforms page states no Windows signing requirement.

### 4.2 Options, with costs as published

| Option | Cost (as published) | Who can use it | Notes |
| --- | --- | --- | --- |
| None | 0 | Anyone | Fine for named testers with an instruction sheet; poor for a public download |
| Azure Artifact Signing (formerly Trusted Signing; the old URLs redirect to the new name) | Basic 9.99 USD a month with 5,000 signatures; Premium 99.99 USD a month; 0.005 USD per signature over quota (Azure pricing page and the Azure retail prices API, still listed as "Trusted Signing" there) | Public Trust: individuals in the US or Canada only. Organisations: the quickstart lists the US, Canada, the EU, the UK, Australia, New Zealand, Japan, South Korea, Singapore, Switzerland, Norway and Israel; Microsoft's "Code signing options" page lists only the US, Canada, the EU and the UK. The two pages disagree (**unverified** which is current) | No hardware token; works from CI; no EV certificates "and there's no plan to issue EV certificates in the future" (Artifact Signing FAQ) |
| OV certificate from a CA (for example DigiCert, Sectigo, GlobalSign) | "Typically $150-300/year depending on the CA" (Microsoft's range, not a CA quote) | Worldwide | Since June 2023 the private key must be on an HSM or hardware token; identity validation takes several business days |
| EV certificate | "$400+/year" (Microsoft's range) | Worldwide | Same SmartScreen behaviour as OV since 2024; "no longer justified" for SmartScreen alone |
| SignPath Foundation | Free for qualifying open-source projects | Its terms require an OSI licence for all components and "no proprietary, non open-source component", and every signed binary must come from a verified automated build of the repository | A Unity player contains Unity's proprietary runtime, and this plan keeps builds local; so the project likely does not qualify (**unverified**; it would need SignPath's answer) |
| Microsoft Store (MSIX) | Free; Microsoft re-signs | Worldwide | No SmartScreen warnings for Store installs. A separate store and packaging; not proposed now |

**Proposed:** no Windows signing for tester builds or a Steam-only Windows release; revisit before any public direct
download (decision O4). If the owner is an individual outside the US and Canada, Artifact Signing is closed and the choice
is an OV certificate or nothing.

---

## 5. Steam

Every step on Steamworks is the owner's: signup, the fee, tax and bank forms, the Content Survey, the store page and every
build upload and "set live". #424 already lists these as owner steps.

### 5.1 Accounts, fees and timing (as published)

- **Steam Direct fee:** "$100.00 USD fee for each product". "This fee is not refundable, but will be recoupable in the
  payment made after your product has at least $1,000.00 USD Adjusted Gross Revenue." It cannot be paid from the Steam
  Wallet (Steam Direct page; Steam Direct Fee page). A free game never recoups it.
- **Paperwork:** bank, tax and identity verification, which "takes between 15-30 days" (Steam Direct page). The bank account
  holder's name must match the legal identification.
- **Before the first release:** a 30-day wait after paying the fee, and a public Coming Soon page for at least two weeks
  (Steam Direct page).
- **Review:** store page and build each "typically take 3-5 business days"; Valve asks for at least 7 business days. "Your
  product must successfully launch in all supported operating systems listed on the store page." (Review Process page.)
- **Revenue share.** Valve's public pages read for this plan do not state the base share; it is in the Steam Distribution
  Agreement, visible after login. The public 30 November 2018 announcement says that for revenue from 1 October 2018 the
  share moves to "75%/25% on earnings beyond $10M" and "80%/20% on earnings beyond $50M". The base rate below $10M is
  **unverified** here.
- **Germany:** since 15 November 2024 Steam hides games without a German age rating; Valve issues one from the Content
  Survey, or a real USK rating can be entered (Steam, "Age Ratings Mandatory in Germany"). Issue #54's "USK check for
  Holocaust content" is therefore a Content Survey answer first; a separate USK submission is optional.

### 5.2 The Steamworks SDK and the wrapper choice

**What the SDK licence allows.** The Steamworks SDK Access Agreement grants a licence to "use and locally reproduce the SDK
in source code form, solely to develop the Licensee Software", and to "reproduce and distribute the part of the SDK
provided inside the folder named redistributable_bin ... along with the Licensee Software in object code form". It does not
grant publishing the SDK's source in a public repository. **Proposed:** never commit Valve's SDK headers or tools to this
MIT repository; take the native library through the wrapper's package, pinned to a tag, the way `com.ivanmurzak.unity.mcp`
comes in through `unity/Packages/manifest.json`; and keep all Steam code behind a scripting define so the WebGL build and
forks compile without it. Whether the redistributable binaries that Steamworks.NET's own repository carries may also sit in
this repository's tree is a licence question for the owner, not assumed here.

| | Steamworks.NET | Facepunch.Steamworks |
| --- | --- | --- |
| Licence | MIT (LICENSE file, "Copyright (c) 2013-2022 Riley Labrecque") | MIT (LICENSE file, "Copyright (c) 2016 Facepunch Studios LTD") |
| Style | "designed to be as close as possible to the original C++ API", so Valve's documentation applies directly | An idiomatic C# layer ("SteamClient.SteamId", events, async call results) |
| Platforms (README) | "fully supports Windows (32 and 64 bit), OSX, and Linux"; "building against Steamworks SDK 1.65" | Windows, Linux, macOS, Unity, Unity IL2CPP all ticked |
| Unity install | Unity Package Manager git URL with a release tag | A DLL build; Unity support claimed |
| Latest release | 2025.164.1, 2 August 2026; last commit 7 August 2026 ("Update to Steamworks 1.65[a]") | 2.5.2, 23 April 2026; commits as recent as 13 October 2026 |
| Open issues | 173 | 216 |
| Native library | Ships Valve's `steam_api` binaries in its package folder | Still needs Valve's `steam_api` library (the README's "no native requirements apart from Steam") |

(GitHub repository metadata and READMEs, read 14 October 2026.)

**Proposed:** none for the first Steam release (section 5.3). When a feature needs one, **Steamworks.NET**: it maps one to
one onto Valve's documentation, which keeps reviews simple, and it installs through UPM by tag. Facepunch is equally
licensed and active; the choice is decision O11.

### 5.3 Minimum feature set

| | Free release | Paid release |
| --- | --- | --- |
| Steamworks API in the game | None. Upload only needs the SDK's ContentBuilder | None required. Valve: everything beyond upload "is optional" |
| Store setup | "This is a free product" at app creation; a hidden free package (Free To Play page) | Pricing per currency |
| Steam DRM wrapper | Optional. Valve: "not an anti-piracy solution"; it "protects against extremely casual piracy" | Optional; for an MIT game whose source is public it adds little |
| Cloud saves | Optional. Steam Auto-Cloud: set a byte quota and file count, then a root (`WinAppDataLocalLow` / `MacAppSupport` + the save subfolder); "without writing code" (Steam Cloud page) | Same |
| Achievements | Optional; need the wrapper and Steamworks App Admin setup | Same; players of paid games tend to expect them, but nothing requires them |
| Steam Deck review | Optional; Valve reviews on request (section 5.7) | Same |
| Content Survey | Required, all three sections, before review | Same |
| Licence questions | D-D (start-screen painting), D-P (portraits) and the #465 recording's terms still apply, since Valve's rules forbid "Content you don't own or have adequate rights to" | As free, plus D-D's own recommendation that a paid release needs confirmed commercial rights or a replacement image, and #465's note that the Israel State Archives' English terms say "except commercial" while the Hebrew terms say "including commercial" |

**Proposed:** the first Steam release ships with no Steamworks API calls. Auto-Cloud can be switched on later from App Admin
without a new build, if the save folder is fixed first (section 2.6). Achievements wait until the GDD game has goals worth
marking.

### 5.4 Depots, branches and the upload flow (outline)

- **Depots.** One depot per OS: a Windows depot and a macOS depot, each with its OS set, one architecture setting ("leave it
  set to [All Architectures]" with one depot per OS), and both added to the store package and to the Dev Comp package or
  testers will not receive them (Depots page; Testing On Steam page; the upload page's "My Mac and/or Linux Builds aren't
  installing any files").
- **Launch options.** Prefer the app bundle on macOS ("the app bundle format should be preferred", upload page).
- **Branches.** `default` is what customers get; a password-protected `beta` branch for testers. Valve: "you can not set the
  build live on the default branch automatically. You must do that manually in App Admin." A beta branch can be set live
  from the build script (`SetLive`) (Branches page; upload page).
- **Upload.** `steamcmd +login <builder account> +run_app_build app_build_<appid>.vdf +quit` with an app build script and a
  depot build script per depot. Valve recommends "a dedicated build account" with only the "Edit App Metadata" and
  "Publish App Changes To Steam" permissions, and documents a Steam Guard step for machines that log in by script
  (upload page). Testers outside Steamworks get Steam Playtest or release-override keys
  (Testing On Steam page).
- **Where it runs.** On the owner's Mac, by the owner, after the local release script (section 6.4). The builder account's
  credentials never enter the repository or a CI secret while builds stay local.
- **What gets uploaded** is the same folder the release record hashes, so the store build and the GitHub Release can be
  matched by SHA-256.

### 5.5 The AI-content disclosure

Steam's Content Survey section 3 covers "content that ships with your game, and is consumed by players ... artwork, sound,
narrative, localization", and not efficiency tools; "Pre-Generated" content is reviewed like any other content, and
"Live-Generated" content needs guardrails (Content Survey page). The disclosure is shown on the store page.

How it maps to the repository:
- `ASSETS.yaml` and `ASSETS.portraits.yaml` mark AI output with `ai_generated: true`; `scripts/oss/ai_disclosure.py`
  writes `docs/ai-disclosure.md` from them (`docs/RELEASING.md` store-page checklist). The draft wording, and what each
  statement rests on, is in `docs/steam/store-page-draft.md` section 4 (#424).
- **In a desktop build** the AI content is: the start-screen painting (`declaration-hall.jpg`, row `owner-verify`, D-D
  open), the decision-card clips (11 today, 16 if #466 merges, rows citing D-V). The WebGL tab icon is not in a desktop build.
- **Live generation:** none ("Nothing is generated live while you play", `docs/ai-disclosure.md`). Keep the statement only
  while it is true.
- **Gap, proposed for a later issue:** `ai_disclosure.py` lists every manifest row, not what a given build carries. A
  `--build <folder>` mode that keeps only rows whose files are inside the build (and, for the packed assets, the build's
  own manifest, as `event-photos.json` does for photographs) would make the survey answer match the shipped build exactly.
- The store-page media (capsules from `promo/final/01`, any cinematic teaser) are disclosed separately, as #424's draft
  already does.

### 5.6 The store page and the trailer

The page text, media placement and open questions are in `docs/steam/store-page-draft.md` (#424). The trailer from the
real build is the pending part of #45. Two points from this plan feed them:
- The page may list only platforms whose builds pass review ("must successfully launch in all supported operating
  systems listed"), so it should name Mac only once the Mac build is notarized.
- Screenshots and the trailer should come from the desktop release build, recorded with that build's commit and SHA-256,
  as `docs/RELEASING.md` already asks for the zip.

### 5.7 Steam Deck

Valve reviews games on Deck on request and publishes "Verified", "Playable", "Unsupported" or "Unknown"; the review does
not decide availability. Verified needs full controller support with matching glyphs, a supported resolution (1280x800
preferred) and text at least 9 px high at 1280x800. Valve also notes that "some media codecs used for video or audio" are
unsupported (Steam Deck compatibility page). This game is mouse and keyboard only, so Verified would need controller
support first. **Proposed:** no Deck claim at launch; test the Windows build under Proton once, including the H.264 clips,
before deciding (decision O10).

---

## 6. CI or a local release script

### 6.1 What GitHub Actions offers this public repository

- "Use of the standard GitHub-hosted runners is free and unlimited on public repositories." Larger runners are always
  charged (GitHub runner reference; Actions billing page).
- Standard runners for public repositories: Linux and Windows x64 with 4 CPUs, 16 GB RAM, 14 GB SSD; macOS arm64
  (`macos-latest`, `macos-14/15/26`) with 3 M1 CPUs, 7 GB RAM, 14 GB SSD; macOS Intel with 4 CPUs, 14 GB RAM, 14 GB SSD.
- Limits: 6 hours per job; on the Free plan 20 concurrent jobs, of which at most 5 macOS (Actions limits page).
- Secrets: "With the exception of GITHUB_TOKEN, secrets are not passed to the runner when a workflow is triggered from a
  forked repository" (Events that trigger workflows). A Unity job could not run on outside contributors' PRs, as
  `CONTRIBUTING.md` already says.
- 14 GB of disk must hold the Unity editor with the target modules, the `Library/` import cache and the build output. The
  editor's installed size was not measured for this plan (**unverified** whether it fits).

### 6.2 GameCI and the Unity licence

- **GameCI** (`game-ci/unity-builder` v6.0.0, 10 September 2026, MIT; `game-ci/unity-test-runner` v4.3.2, 9 September 2026,
  MIT) runs Unity in Docker on Linux and Windows or on a macOS host. Its builder page: "Many Mono targets can be
  cross-compiled - for example, a Linux runner can produce a Windows Mono player - while targets that need a platform SDK
  or native IL2CPP toolchain require a compatible host." The macOS IL2CPP player therefore needs a macOS runner.
- **GameCI's Personal-licence method:** activate in the Unity Hub on your own machine, take the `.ulf` file, and store it
  with the Unity email and password as the secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`. Plus or Pro uses
  `UNITY_SERIAL` instead. GameCI's FAQ answers "Can I use a free Unity license to build my project?" with "Yes".
- **What Unity says.** Unity's manual: "For Unity Personal, the Unity Hub is the only method for activating and returning
  licences"; the command-line procedure "do[es]n't apply to Unity Personal"; manual activation "isn't supported for ... Unity
  licenses under a Personal plan". The Unity Editor Software Terms (last updated 30 June 2026) section 2.8 let a seat
  install the editor on "a primary and a secondary computer or operating system ... only for use by a single person", one
  instance at a time per seat, and say that third-party providers may not run the editor "on ... the cloud or a remote
  server" except when acting on your behalf as an Authorized User. Section 2.5: "You may not use Unity Build Server with
  the Unity Personal tier." None of the pages read addresses a Personal `.ulf` reused on CI virtual machines directly; the
  documented route for headless activation is a Pro seat. **This plan does not read the terms as permitting it, and does not
  read them as forbidding it; the owner decides (O7), ideally after asking Unity.**
- **Pro, if wanted:** "from $2,310.00 /yr" per seat (Unity plans page; the Pro page shows "$210.00/mo per seat"). Unity
  Pro is "required for businesses with revenue or funding greater than $200K USD in the last 12 months"; Personal is for
  individuals whose amount "generated in connection with their use of Unity is less than $200K USD" (Unity plans page).
- **Secrets that a Unity CI job would need:** `UNITY_LICENSE` or `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`; for
  signing, the Apple Developer ID certificate and a notarytool key or app-specific password, and for Windows an Artifact
  Signing identity or certificate; for Steam, the builder account and its Steam Guard token. Every one of these would sit
  on GitHub's machines, and the owner's Unity password would be among them.

### 6.3 Recommendation

**Keep Unity builds, signing and Steam uploads on the owner's Mac, through a local release script under
`tools/machine_lock.py`.** Reasons:
1. Unity documents Hub-only activation for Personal, and the terms limit a seat to two computers; CI under Personal rests on
   a route Unity does not document.
2. The project already decided Unity stays out of CI (D9), for reasons that still hold: fork PRs get no secrets, and the
   credentials would sit on machines that run outside code (`CONTRIBUTING.md`).
3. The Mac build needs a macOS host anyway, and the owner's Mac is already the reference machine for goldens and profiles.
4. It adds no recurring cost and no new account.

**What CI can still do without Unity (proposed, optional):** a `workflow_dispatch` job that downloads a release's player
zips from the GitHub Release and runs `-batchmode -nographics -golden` on `windows-latest` and `macos-latest`. Running a
built player needs no Unity licence and no secret. It gives an independent golden check on clean Windows and macOS
machines, which the owner's Mac cannot.

**When to revisit:** if the project moves to Unity Pro, or if a second maintainer needs to cut releases.

### 6.4 The local release script (proposed outline)

A new `tools/release_desktop.py` (or a shell script beside `package_web.sh`), run by the owner on the merged `main` after
`bash scripts/oss/release_gate.sh` prints `RELEASE GATE OK`:

1. Refuse unless on `main`, the tree is clean, and `HEAD` is the tag's commit.
2. For each platform, under `uv run tools/machine_lock.py --shared --wait-unity -- "$UNITY" -batchmode -CI false
   -GITHUB_ACTIONS false -TF_BUILD false -quit ... -executeMethod Statecraft.Game.EditorTools.Build.<Mac|Windows> -shareable
   -logFile <absolute path>`; then `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`.
3. Run the player's `-golden` check (Mac locally; Windows on a Windows machine or the optional CI job) and require both
   `GOLDEN OK` lines.
4. Package like `package_web.sh`: copy `LICENSE`, `NOTICE`, `THIRD-PARTY-NOTICES.md`, `OFL.txt` and `LICENSES/` next to
   the player; run the sensitivity gate of `tools/package_release.py` on the build's `StreamingAssets/content` and its
   `event-photos.json` (the packager needs a desktop mode: today it refuses any folder without `index.html`); zip; list the
   zip and require the notices.
5. If signing is decided: sign and notarize (section 3.3) or Authenticode-sign (section 4), before the final zip and hash.
6. Write the build record (6.5).
7. Steam upload stays a separate, manual owner command.

### 6.5 The build record

`docs/RELEASING.md` already wants the commit (the tag), the zip's SHA-256 (in the release notes) and, for store media, the
capture date. **Proposed:** one record per release with, for every artifact:

| Field | Example source |
| --- | --- |
| Version and tag | `bundleVersion`, `SimInfo.Version`, `git describe` |
| Commit | `git rev-parse HEAD` |
| Date (UTC) | the script's clock at packaging |
| Unity version and changeset | `unity/ProjectSettings/ProjectVersion.txt` and the editor log |
| Platform, architecture, backend | the build call |
| Artifact file name, size, SHA-256 | `shasum -a 256` |
| Golden lines | the player's `GOLDEN OK 1950 ...` and `GOLDEN OK 1952 ...` |
| Signing | none, or the identity name and the notarization submission id |
| Steam build ID | added by the owner after upload |

**Where:** written as `build-record.txt` (plain text, one block per artifact), attached to the GitHub Release beside the
zips, and pasted into the release notes, which is where the SHA-256 goes today. Committing a copy under `docs/releases/` in
the next version-bump PR would make the history greppable without trusting the Releases page; that is decision O12. This
plan does not assume that Unity produces byte-identical output from the same commit twice; that has not been tested here.
The record identifies a build; it does not promise to reproduce one.

---

## 7. Phased plan

Each phase is shippable on its own and needs nothing from a later one.

### Phase 0: preparation (code and docs, no purchase)

- `Build.Mac` sets the Universal architecture itself and logs it; release builds use LZ4HC; the build log prints size.
- A desktop "Quit" control (2.5).
- Re-run the Mac `-golden` check against the current hashes and record it in `docs/determinism.md` (it is listed as still
  needed).
- Desktop checks for #465 and #466 on the Mac player: `VIDEO playing`, `RECORDING playing`.
- `tools/package_release.py` learns a desktop folder; the build record (6.5).
- Desktop boot measurement with `-bootlog` (2.9).
- **Owner:** freeze `companyName`/`productName` for desktop or settle D8 (2.6); choose a bundle ID (O6).

### Phase 1: unsigned Mac build for named testers

- The release script makes a Universal `.app`, zipped with notices, gated with `-shareable`, hashed and recorded.
- A one-page tester note: how to "Open Anyway" (Apple's steps), where the save lives, how to send `Player.log`.
- Collect from testers: macOS version, chip, whether the clips and the recording play, `-vprofile` summaries.
- **Owner:** decide how testers get it (O9): a GitHub pre-release is public; private links are not.
- **Cost:** none.

### Phase 2: Windows x64

- `Build.Windows` (Mono from the Mac, or IL2CPP on a Windows PC: O8), the same packaging and record.
- The `-golden` check on a real Windows machine (a tester's, the owner's, or the optional CI job in 6.3).
- Testers must choose "Run anyway" at SmartScreen's "Windows protected your PC"; the note says so.
- First look at Direct3D and Vulkan with the custom shaders (2.3).
- **Owner:** O8; a Windows machine or tester.
- **Cost:** none, unless a Windows PC is bought.

### Phase 3: signing (only once O3 or O4 says yes)

- Mac: Developer ID signing, Hardened Runtime, notarization, stapling (3.2, 3.3).
- Windows: the chosen route (4.2).
- **Owner:** the Apple Developer Program (99 USD a year) and/or a Windows signing option; keeps the credentials.

### Phase 4: Steam

- Steamworks signup, fee, tax and bank (owner); the Coming Soon page from #424; two weeks visible and 30 days after the fee.
- Depots for Windows (and macOS only after Phase 3's notarization), a `beta` branch for testers, steamcmd upload by the
  owner, review (plan 7 business days).
- Content Survey answers from the shipped build (5.5), which also gives the German rating.
- **Owner:** all Steamworks actions; free or paid (O2); D8 settled; D-D and D-P settled for what ships.
- **Cost:** 100 USD per app, not recouped by a free game.

### Phase 5: optional Steam features

- Auto-Cloud for saves (configuration only).
- Achievements through Steamworks.NET, behind a define.
- A Deck review after controller support exists.

---

## 8. Risks

| Risk | Effect | Mitigation |
| --- | --- | --- |
| A Windows player fails the golden check | Saves and replays would not match across platforms | The `-golden` gate per artifact; IL2CPP and Mono tried separately; investigate before shipping, never re-pin the goldens for a platform |
| The Mac golden has not been rechecked since the issue #2 content change | A Mac build could ship unverified | Phase 0 re-runs it |
| D8 renames the game after testers have saves | Saves are stranded (2.6) | Freeze the names before Phase 1, or ship a migration |
| Custom shaders misbehave on Direct3D or Vulkan | Visual bugs on Windows only | Record the API per tester; pin the API list if needed |
| H.264 clips do not decode on some Windows or Proton setups | The clip is skipped after 8 s | Degrades by design; check logs; re-encode only if a real failure appears |
| `file://` paths with spaces or drive letters (#465) | The recording does not play on desktop | Test in Phase 1 and 2; fix in its own PR if seen |
| Unsigned builds alarm testers | Lost testers | A clear note; small named group; signing in Phase 3 if the public needs it |
| Notarization rejects the app (entitlements, unsigned nested binaries) | Mac on Steam blocked | Follow Apple's notary log; drop `--deep`; sign nested code first |
| Steam review finds a listed OS that does not launch | Release delayed | List only tested platforms |
| Licences of shipped assets for a paid release (D-D, D-P, the #465 recording) | A paid release blocked or a takedown | Settle them before Phase 4 if paid |
| Unity licence terms around CI | Licence breach | Builds stay local (6.3) |
| The 14 GB runner disk is too small (if CI is chosen later) | Failed CI builds | Measure before committing to CI |
| Credentials leak (Apple, signing, Steam builder) | Account abuse | Owner keychain only; never in the repository, CI or an agent's context |

---

## 9. Owner decisions

Each is a question with its options and what each costs. Recommended defaults are marked; the owner decides.

**O1. Which platforms at the first public release?**
- **Decided (owner, 2026-10-14), with O7:** "ok lets do it - windows, mac and linux". Windows, macOS and Linux, built on the Mac (O7).
- (a) Windows only. Cost: no Apple account; the Mac audience waits.
- (b) Windows and macOS. Cost: the Apple Developer Program (99 USD a year) and notarization work; required for Mac on Steam.
- (c) Also native Linux. Cost: another build and test target; Deck already runs Windows builds through Proton.
- Recommended: (a) for the first Steam release, with unsigned Mac builds to testers in parallel; (b) once O3 is yes.

**O2. Free or paid on Steam?**
- **Decided (owner, 2026-10-14):** "free". (a).
- (a) Free. Cost: the 100 USD Steam Direct fee is never recouped; D-D's paid-release condition does not apply; the
  question of the #465 recording's commercial terms is softer but not gone.
- (b) Paid. Cost: pricing work; D-D needs commercial rights or a new painting; the #465 recording's terms ("except
  commercial" in English, "including commercial" in Hebrew) need an answer; the fee is recouped after 1,000 USD.
- Recorded context: README.md and TRADEMARK.md say the maintainer intends to sell the game; #424 treated the first release
  as free.

**O3. Join the Apple Developer Program?**
- **Decided (owner, 2026-10-14):** "we have apple development". The owner already has an account, and only the owner acts on it.
- (a) Yes, as an individual. Cost: 99 USD a year (local currency may differ); 2FA Apple Account in the legal name; the
  owner keeps the Developer ID certificate.
- (b) Yes, as an organisation. Cost: as (a), plus a legal entity, a D-U-N-S Number and a public website.
- (c) No. Cost: Mac only as unsigned tester builds; no Mac on Steam.

**O4. Sign Windows builds, and how?**
- **Decided (owner, 2026-10-14):** "no windows signing for now". (a).
- (a) Not for now. Cost: "Windows protected your PC" on direct downloads; nothing extra on Steam is required by Valve.
- (b) Azure Artifact Signing. Cost: 9.99 USD a month (Basic); individuals only in the US or Canada.
- (c) An OV certificate. Cost: about 150-300 USD a year (Microsoft's range) and a hardware token or cloud HSM.
- (d) EV. Cost: 400 USD a year or more; no SmartScreen advantage over OV since 2024.
- Recommended: (a) until a public direct download is planned.

**O5. Open a Steamworks account, when, and as whom?**
- **Decided (owner, 2026-10-14):** "lets open the steamworks account as an individual". (a), an owner action.
- (a) Now, as an individual (#424's research assumed a sole proprietorship). Cost: 100 USD, the tax and bank forms, 15-30
  days of verification; the 30-day clock starts with the fee.
- (b) Later, after Phase 2. Cost: the Coming Soon page waits.
- (c) As a company. Cost: a company and its bank account first.

**O6. The name (D8) and the identity fields.**
- **Decided (owner, 2026-10-17):** "O6 bundle ID: io.github.eyalgolan.statecraftisrael". D8 was decided by the owner on 2026-10-14: "keep the games name as is".
- (a) Settle D8 before the first tester build and set `productName`, `companyName` and the bundle ID once. Cost: the
  decision now; no migration later.
- (b) Ship testers under the current names and rename later. Cost: a save migration, or testers lose saves.
- Recommended: (a), or at least freeze the desktop names now.

**O7. CI or local builds?**
- **Decided (owner, 2026-10-14):** (a), local builds on the Mac. The owner first chose cloud builds; Unity's pages then ruled that out for a Personal licence (see O7 in `docs/oss/DECISIONS.md` for the quotes, including Editor Software Terms §2.5), and the owner chose "Build on this Mac". GitHub runners run only the licence-free player golden check (6.3).
- (a) Local on the owner's Mac (recommended). Cost: the owner's machine time; the release depends on that Mac.
- (b) GitHub Actions with GameCI and a Personal `.ulf`. Cost: no money; a route Unity does not document for Personal;
  the owner's Unity password in GitHub secrets; no runs on fork PRs.
- (c) GitHub Actions with a Unity Pro seat. Cost: from 2,310 USD a year; the 14 GB disk limit still to be measured.
- (d) A self-hosted runner on the owner's Mac. Cost: a runner process on the personal machine, which GitHub would let
  workflow code reach; the Unity licence question of (b) partly remains.
- Add-on, any option: the licence-free player golden job (6.3). Cost: none.

**O8. Windows backend?**
- **Linux backend, decided (owner, 2026-10-14):** "sounds good", to keeping Mono for the Linux player (`Build.LinuxMono`, #542).
- **Decided (owner, 2026-10-14):** first "use the best windows build type" (b), then, with O7's local builds, (a) Mono built on the Mac. IL2CPP would need a Windows PC.
- (a) Mono, built on the Mac. Cost: the Windows Build Support (Mono) module; a Windows machine only to run the check.
- (b) IL2CPP, built on a Windows PC. Cost: a Windows machine with Visual Studio C++ tools; a second build host.

**O9. How do testers get unsigned builds?**
- **Decided (owner, 2026-10-14):** "lets go with github pre-release". (a).
- (a) A GitHub pre-release. Cost: public to anyone; every `docs/RELEASING.md` gate applies (`-shareable`, the sensitivity
  review, notices).
- (b) Private links to named testers. Cost: manual; the same gates still apply, since the build leaves the machine.

**O10. Steam features at launch?**
- **Decided (owner, 2026-10-14):** "steam features at luanch are fully working 1948-1956 mvp that looks like demo2", then "1956 was meant loosely and launch stays at 1948–51". The launch is a fully working MVP from 15 May 1948 to January 1952 that looks like demo2, with no Steam-only feature named. The GDD MVP and the 1952-56 pause are unchanged.
- (a) None (recommended). Cost: none.
- (b) Auto-Cloud. Cost: configuration only; the save folder must be final.
- (c) Achievements. Cost: a wrapper, design work, a define per build.
- (d) A Deck review. Cost: controller support first, or accept "Playable"/"Unsupported".

**O11. Which Steamworks wrapper, when one is needed?**
- **Decided (owner, 2026-10-14):** "lets go with Steamworks.NET". (a).
- (a) Steamworks.NET (recommended): MIT, one-to-one with Valve's API, UPM install.
- (b) Facepunch.Steamworks: MIT, idiomatic C#.

**O12. Where do build records live?**
- **Decided (owner, 2026-10-14):** "build records will leave in a seperate repo". A separate repository, neither (a) nor (b).
- (a) The GitHub Release only (asset and notes). Cost: none; depends on the Releases page.
- (b) Also committed under `docs/releases/`. Cost: one file per release in the version-bump PR.

The existing open lines in `docs/oss/DECISIONS.md` also block any release through the gate: D-D, D-P, D13, D-A and D-R (D8 was decided on 2026-10-14).

---

## 10. Facts not verified

| Fact | Why it is unverified |
| --- | --- |
| Steam's base revenue share below 10M USD | Not stated on the public pages read; it is in the Steam Distribution Agreement behind login. Only the 2018 tiers (75/25 above 10M USD, 80/20 above 50M USD) were read |
| Whether Unity's terms permit a Personal `.ulf` on CI machines | Unity's docs and terms do not address it directly (6.2) |
| Whether Unity plus modules, `Library/` and the output fit a 14 GB runner disk | Not measured |
| The Unity Hub module id for Windows Build Support (Mono) on macOS | Not checked; `docs/dev-setup.md` lists only `webgl` and `mac-il2cpp` |
| Artifact Signing's organisation countries | Microsoft's quickstart and its "Code signing options" page list different countries |
| OV and EV prices | Microsoft's published ranges; no CA price page was read |
| Whether Steam-installed Windows games meet SmartScreen | Not found in Microsoft's or Valve's pages read |
| SignPath Foundation eligibility for a Unity game | Inferred from its terms; not asked |
| H.264 playback under Proton on Steam Deck | Valve says some codecs are unsupported, without a list |
| Desktop build size and boot time | Never measured; section 2.9's numbers are proposals |
| Unity build reproducibility (byte-identical rebuilds) | Not tested in this repository |
| GameCI's macOS deployment page | `https://game.ci/docs/github/deployment/macos` redirected to the GameCI home page; its content was not read |
| Steam's stated OS floors | Valve's Platforms page still names OS X 10.11 and Windows 7, which may be out of date; Unity's floors (macOS 12, Windows 10 21H1) are higher |

---

## 11. Sources (all read 14 October 2026)

**Unity**
- macOS build settings reference: https://docs.unity3d.com/6000.3/Documentation/Manual/macosbuildsettings.html
- macOS requirements and compatibility: https://docs.unity3d.com/6000.3/Documentation/Manual/macos-requirements-and-compatibility.html
- Code sign your application (macOS): https://docs.unity3d.com/6000.3/Documentation/Manual/macoscodesigning.html
- Notarize with Xcode and command-line tools: https://docs.unity3d.com/6000.3/Documentation/Manual/macosnotarizationxcode.html
- `OSXStandalone.UserBuildSettings.architecture`: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/OSXStandalone.UserBuildSettings-architecture.html
- Windows requirements and compatibility: https://docs.unity3d.com/6000.3/Documentation/Manual/windows-requirements-and-compatibility.html
- Windows build settings reference: https://docs.unity3d.com/6000.3/Documentation/Manual/WindowsStandaloneBinaries.html
- Windows Player: IL2CPP scripting backend: https://docs.unity3d.com/6000.3/Documentation/Manual/WindowsPlayerIL2CPPScriptingBackend.html
- Windows Player settings: https://docs.unity3d.com/6000.3/Documentation/Manual/playersettings-windows.html
- IL2CPP overview: https://docs.unity3d.com/6000.3/Documentation/Manual/il2cpp-introduction.html
- Graphics API support: https://docs.unity3d.com/6000.3/Documentation/Manual/GraphicsAPIs.html
- Configure graphics APIs: https://docs.unity3d.com/6000.3/Documentation/Manual/configure-graphicsAPIs.html
- Include additional files in a build (StreamingAssets): https://docs.unity3d.com/6000.3/Documentation/Manual/StreamingAssets.html
- `Application.persistentDataPath`: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html
- Video file compatibility with target platforms: https://docs.unity3d.com/6000.3/Documentation/Manual/video-sources-compatibility-target-platforms.html
- Video playback in Web: https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-video.html
- Licensing overview: https://docs.unity3d.com/6000.3/Documentation/Manual/LicenseOverview.html
- License activation methods: https://docs.unity3d.com/6000.3/Documentation/Manual/LicenseActivationMethods.html
- Manage your license through the command line: https://docs.unity3d.com/6000.3/Documentation/Manual/ManagingYourUnityLicense.html
- Introduction to manual license activation: https://docs.unity3d.com/6000.3/Documentation/Manual/ManualActivationGuide.html
- Unity Editor Software Terms (last updated 30 June 2026): https://unity.com/legal/editor-terms-of-service/software
- Unity plans and pricing: https://unity.com/products (requested as https://unity.com/pricing)
- Unity Personal: https://unity.com/products/unity-personal
- Unity Pro: https://unity.com/products/unity-pro

**Apple**
- Apple Developer Program: https://developer.apple.com/programs/
- Enrollment: https://developer.apple.com/programs/enroll/
- What's included: https://developer.apple.com/programs/whats-included/
- Notarizing macOS software before distribution: https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution
- Customizing the notarization workflow: https://developer.apple.com/documentation/security/customizing-the-notarization-workflow
- Hardened Runtime: https://developer.apple.com/documentation/security/hardened-runtime
- Building a universal macOS binary: https://developer.apple.com/documentation/apple-silicon/building-a-universal-macos-binary
- Safely open apps on your Mac (Gatekeeper): https://support.apple.com/en-us/102445
- Gatekeeper and runtime protection (Platform Security): https://support.apple.com/guide/security/gatekeeper-and-runtime-protection-sec5599b66df/web

(The Apple developer documentation pages were read through their JSON data endpoints under
`https://developer.apple.com/tutorials/data/documentation/...`, which serve the same text.)

**Microsoft**
- SmartScreen reputation for Windows app developers: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation
- Code signing options for Windows app developers: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options
- What is Artifact Signing?: https://learn.microsoft.com/en-us/azure/artifact-signing/overview (https://learn.microsoft.com/en-us/azure/trusted-signing/overview redirects here)
- Artifact Signing quickstart (prerequisites): https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart
- Artifact Signing FAQ: https://learn.microsoft.com/en-us/azure/artifact-signing/faq
- Artifact Signing pricing: https://azure.microsoft.com/en-us/pricing/details/artifact-signing/ (prices render by script; the figures were read from the page's embedded data and from the Azure Retail Prices API, https://prices.azure.com/api/retail/prices, product "Trusted Signing")

**Valve (Steamworks)**
- Steam Direct: https://partner.steamgames.com/steamdirect
- Steam Direct Fee: https://partner.steamgames.com/doc/gettingstarted/appfee
- Steamworks SDK: https://partner.steamgames.com/doc/sdk
- Steamworks SDK Access Agreement: https://partner.steamgames.com/documentation/sdk_access_agreement
- Uploading to Steam (SteamPipe, steamcmd): https://partner.steamgames.com/doc/sdk/uploading
- Branches (Betas): https://partner.steamgames.com/doc/store/application/branches
- Depots: https://partner.steamgames.com/doc/store/application/depots
- Platforms: https://partner.steamgames.com/doc/store/application/platforms
- Steam Cloud: https://partner.steamgames.com/doc/features/cloud
- Stats and Achievements: https://partner.steamgames.com/doc/features/achievements
- Steam DRM: https://partner.steamgames.com/doc/features/drm
- Free To Play Games: https://partner.steamgames.com/doc/store/freetoplay
- Content Survey: https://partner.steamgames.com/doc/gettingstarted/contentsurvey
- Age Ratings Mandatory in Germany: https://partner.steamgames.com/doc/gettingstarted/contentsurvey/germany
- Review Process: https://partner.steamgames.com/doc/store/review_process
- Testing On Steam: https://partner.steamgames.com/doc/store/testing
- Steam Deck and Steam Machine Compatibility Review: https://partner.steamgames.com/doc/steamhardware/compat (requested as https://partner.steamgames.com/doc/steamdeck/compat)
- New Revenue Share Tiers and other updates to the Steam Distribution Agreement (30 November 2018): https://steamcommunity.com/groups/steamworks/announcements/detail/1697191267930157838

**Steamworks wrappers**
- Steamworks.NET: https://github.com/rlabrecque/Steamworks.NET (README, LICENSE, release 2025.164.1)
- Facepunch.Steamworks: https://github.com/Facepunch/Facepunch.Steamworks (README, LICENSE, release 2.5.2)

**GameCI**
- Activation: https://game.ci/docs/github/activation
- Builder: https://game.ci/docs/github/builder
- Test runner: https://game.ci/docs/github/test-runner
- Getting started: https://game.ci/docs/github/getting-started
- FAQ: https://game.ci/docs/faq
- Repositories: https://github.com/game-ci/unity-builder, https://github.com/game-ci/unity-test-runner (MIT; release dates from GitHub)

**GitHub**
- GitHub-hosted runners reference: https://docs.github.com/en/actions/reference/runners/github-hosted-runners
- GitHub Actions billing: https://docs.github.com/en/billing/concepts/product-billing/github-actions
- Actions limits: https://docs.github.com/en/actions/reference/limits
- Secrets: https://docs.github.com/en/actions/concepts/security/secrets
- Events that trigger workflows (fork secrets): https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows

**SignPath Foundation**
- https://signpath.org/ and its terms, https://signpath.org/terms.html

**Repository files cited:** `AGENTS.md`, `CONTRIBUTING.md`, `LICENSING.md`, `TRADEMARK.md`, `ASSETS.yaml`,
`docs/RELEASING.md`, `docs/dev-setup.md`, `docs/determinism.md`, `docs/visual-v1.md`, `docs/m3-release.md`,
`docs/ai-disclosure.md`, `docs/oss/DECISIONS.md`, `docs/steam/store-page-draft.md`, `unity/Assets/Editor/Build.cs`,
`unity/Assets/Editor/ProjectSetup.cs`, `unity/Assets/Editor/QualityTiers.cs`, `unity/Assets/Scripts/SaveStore.cs`,
`unity/Assets/Scripts/UI/CardVideo.cs`, `unity/Assets/Scripts/Boot/GameBoot.cs`, `unity/Assets/Scripts/Map/QualityFallback.cs`,
`unity/ProjectSettings/ProjectSettings.asset`, `unity/ProjectSettings/QualitySettings.asset`, `unity/Packages/manifest.json`,
`tools/package_release.py`, `tools/machine_lock.py`, `scripts/oss/package_web.sh`; PRs #465 and #466; issues #54, #424, #45.
